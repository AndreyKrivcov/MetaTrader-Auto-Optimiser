using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Metatrader_Auto_Optimiser.Model.DirectoryManagers;

namespace Metatrader_Auto_Optimiser.Model.Terminal
{
    /// <summary>
    /// Менеджер терминалла
    /// </summary>
    class TerminalManager
    {
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="TerminalChangeableDirectory">
        /// Путь к директориии с изменяемыми файлами (та что в AppData)
        /// </param>
        public TerminalManager(DirectoryInfo TerminalChangeableDirectory) :
            this(TerminalChangeableDirectory, new DirectoryInfo(File.ReadAllText(TerminalChangeableDirectory.GetFiles().First(x => x.Name == "origin.txt").FullName)), false)
        {
        }
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="TerminalChangeableDirectory">
        /// Путь к директориии с изменяемыми файлами
        /// </param>
        /// <param name="TerminalInstallationDirectory">
        /// Путь к папке и терминалом
        /// </param>
        public TerminalManager(DirectoryInfo TerminalChangeableDirectory, DirectoryInfo TerminalInstallationDirectory, bool isPortable)
        {
            this.TerminalInstallationDirectory = TerminalInstallationDirectory;
            this.TerminalChangeableDirectory = TerminalChangeableDirectory;

            TerminalID = TerminalChangeableDirectory.Name;

            CheckDirectories();

            Process.Exited += Process_Exited;

            Portable = isPortable;
        }
        /// <summary>
        /// Деструктор
        /// </summary>
        ~TerminalManager()
        {
            Close();
            Process.Exited -= Process_Exited;
        }
        /// <summary>
        /// Процесс запуска терминалла
        /// </summary>
        private readonly System.Diagnostics.Process Process = new System.Diagnostics.Process();
        /// <summary>
        /// Событие завершения запущенного процесса
        /// </summary>
        public event Action<TerminalManager> TerminalClosed;

        #region Terminal start Arguments
        /// <summary>
        /// Login для старта - флаг /Login
        /// </summary>
        public uint? Login { get; set; } = null;
        /// <summary>
        /// запуск платформы под определенным профилем. 
        /// Профиль должен быть заранее создан и находится в папке /profiles/charts/ торговой платформы
        /// </summary>
        public string Profile { get; set; } = null;
        /// <summary>
        /// Конфиг файл ввиде объекта /Config
        /// </summary>
        public Config Config { get; set; } = null;
        /// <summary>
        /// Флаг запуска терминалла в режиме /portable
        /// </summary>
        private bool _portable;
        public bool Portable
        {
            get => _portable;
            set
            {
                _portable = value;
                if (value && !TerminalInstallationDirectory.GetDirectories().Any(x => x.Name == "MQL5"))
                {
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized;

                    if (Run())
                    {
                        System.Threading.Thread.Sleep(1000);
                        Close();
                    }
                    WaitForStop();
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
                }
            }
        }
        /// <summary>
        /// стиль окна запускаемого процесса
        /// </summary>
        public System.Diagnostics.ProcessWindowStyle WindowStyle { get; set; } = System.Diagnostics.ProcessWindowStyle.Normal;
        #endregion

        #region Terminal directories
        /// <summary>
        /// Путь к папке где установлен терминалл
        /// </summary>
        public DirectoryInfo TerminalInstallationDirectory { get; }
        /// <summary>
        /// Путь к папке терминалла с изменяемыми файлами
        /// </summary>
        public DirectoryInfo TerminalChangeableDirectory { get; }
        /// <summary>
        /// Путь к папке MQL5
        /// </summary>
        public DirectoryInfo MQL5Directory => (Portable ? TerminalInstallationDirectory : TerminalChangeableDirectory).GetDirectory("MQL5");
        #endregion

        /// <summary>
        /// ID терминалла - имя папки вдиректории AppData
        /// </summary>
        public string TerminalID { get; }
        /// <summary>
        /// Признат того запущен ли терминалл в данный момент или же нет
        /// </summary>
        public bool IsActive => Process.StartInfo.FileName != "" && !Process.HasExited;// (Process.StartInfo.FileName=="" ? false : !Process.HasExited);
        /// <summary>
        /// Process Id
        /// </summary>
        public int ProcessID => Process.Id;

        #region .ex5 files relative paths
        /// <summary>
        /// Список полных имен экспертов
        /// </summary>
        public List<string> Experts => GetEX5FilesR(MQL5Directory.GetDirectory("Experts"));
        /// <summary>
        /// Список полных имен индикаторов
        /// </summary>
        public List<string> Indicators => GetEX5FilesR(MQL5Directory.GetDirectory("Indicators"));
        /// <summary>
        /// Список полных имен скриптов
        /// </summary>
        public List<string> Scripts => GetEX5FilesR(MQL5Directory.GetDirectory("Scripts"));
        #endregion

        /// <summary>
        /// Запуск терминалла
        /// </summary>
        public bool Run()
        {
            if (IsActive)
                return false;
            // Задаем путь к терминаллу
            Process.StartInfo.FileName = Path.Combine(TerminalInstallationDirectory.FullName, "terminal64.exe");
            Process.StartInfo.WindowStyle = WindowStyle;
            // Задаем данные для запуска терминалла (если таковые были установлены)
            if (Config != null && File.Exists(Config.Path))
                Process.StartInfo.Arguments = $"/config:{Config.Path} ";
            if (Login.HasValue)
                Process.StartInfo.Arguments += $"/login:{Login.Value} ";
            if (Profile != null)
                Process.StartInfo.Arguments += $"/profile:{Profile} ";
            if (Portable)
                Process.StartInfo.Arguments += "/portable";

            // Уведомляем процесс о необходимости вызывать событие Exit после закрытия терминалла
            Process.EnableRaisingEvents = true;

            //Запускаем процесс и созраняем статус запуска в переменную IsActive
            return Process.Start();
        }
        /// <summary>
        /// Дождаться завержения работы терминалла
        /// </summary>
        public void WaitForStop()
        {
            if (IsActive)
                Process.WaitForExit();
        }
        /// <summary>
        /// Остановка процесса
        /// </summary>
        public void Close()
        {
            if (IsActive)
                Process.Kill();
        }
        /// <summary>
        /// Дождаться завержения работы терминалла определенное время
        /// </summary>
        public bool WaitForStop(int miliseconds)
        {
            if (IsActive)
                return Process.WaitForExit(miliseconds);
            return true;
        }
        /// <summary>
        /// Поиск файлов с расширением Ex5 
        /// Поиск выполняется рекурсивно - т.е. файлы ищатся в указанной папке и во всех вложенных папках
        /// </summary>
        /// <param name="path">Путь к папке с которой начинается поиск</param>
        /// <param name="RelativeDirectory">Указание папки относительно которой возвращается путь</param>
        /// <returns>Список путей к найденныхм файлам</returns>
        private List<string> GetEX5FilesR(DirectoryInfo path, string RelativeDirectory = null)
        {
            if (RelativeDirectory == null)
                RelativeDirectory = path.Name;
            string GetRelevantPath(string pathToFile)
            {
                string[] path_parts = pathToFile.Split('\\');
                int i = path_parts.ToList().IndexOf(RelativeDirectory) + 1;
                string ans = path_parts[i];

                for (i++; i < path_parts.Length; i++)
                {
                    ans = Path.Combine(ans, path_parts[i]);
                }

                return ans;
            }

            List<string> files = new List<string>();
            IEnumerable<DirectoryInfo> directories = path.GetDirectories();

            files.AddRange(path.GetFiles("*.ex5").Select(x => GetRelevantPath(x.FullName)));

            foreach (var item in directories)
                files.AddRange(GetEX5FilesR(item, RelativeDirectory));

            return files;
        }
        /// <summary>
        /// Событие закрытия терминалла
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Process_Exited(object sender, EventArgs e)
        {
            TerminalClosed?.Invoke(this);
        }
        /// <summary>
        /// Проверка переданного пути к терминаллу на корректность
        /// </summary>
        private void CheckDirectories()
        {
            if (!TerminalInstallationDirectory.Exists)
                throw new ArgumentException("PathToTerminalInstallationDirectory doesn`t exists");
            if (!TerminalChangeableDirectory.Exists)
                throw new ArgumentException("PathToTerminalChangeableDirectory doesn`t exists");
            if (!TerminalInstallationDirectory.GetFiles().Any(x => x.Name == "terminal64.exe"))
                throw new ArgumentException($"Can`t find terminal (terminal64.exe) in the instalation folder {TerminalInstallationDirectory.FullName}");
        }
    }
}
