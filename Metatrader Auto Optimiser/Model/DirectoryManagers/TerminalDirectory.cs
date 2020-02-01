using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Metatrader_Auto_Optimiser.Model.DirectoryManagers
{
    /// <summary>
    /// Объект описывающий структуру директорий тертинала
    /// </summary>
    class TerminalDirectory
    {
        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        public TerminalDirectory() :
            this(new DirectoryInfo(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData)).
                GetDirectory("MetaQuotes").GetDirectory("Terminal"))
        {
        }

        /// <summary>
        /// Параметризированный конструктор
        /// </summary>
        /// <param name="path">Путь к директории Terminal в которой содержатся изменяемые файлы всех терминалов</param>
        public TerminalDirectory(DirectoryInfo path)
        {
            pathToTerminal = path;
            Common = path.GetDirectory("Common");
            Community = path.GetDirectory("Community");
        }

        /// <summary>
        /// Путь к директории Terminal в которой содержатся изменяемые файлы всех терминалов
        /// </summary>
        private readonly DirectoryInfo pathToTerminal;

        /// <summary>
        /// Список путей к директориям конкретных терминалов
        /// </summary>
        public IEnumerable<DirectoryInfo> Terminals
        {
            get
            {
                // Влаженная функция отличающая директории терминалов от прочих 
                bool Comparer(DirectoryInfo dir)
                {
                    string pathToOrigin = Path.Combine(dir.FullName, "origin.txt");
                    // Проверяется наличие файла с описанием пути к исполняемому файлу терминала
                    if (!File.Exists(pathToOrigin))
                        return false;
                    // Проверяется наличие исполняемого файла терминала
                    if (!File.Exists(Path.Combine(File.ReadAllText(pathToOrigin), "terminal64.exe")))
                        return false;

                    return true;
                }

                // Поиск директорий терминалов
                return pathToTerminal.GetDirectories().Where(x => Comparer(x));
            }
        }
        /// <summary>
        /// Директория общих файлов терминалов  
        /// </summary>
        public DirectoryInfo Common { get; }
        /// <summary>
        /// Директория Terminal/Comunity
        /// </summary>
        public DirectoryInfo Community { get; }
    }
}
