using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Metatrader_Auto_Optimiser.Model.DirectoryManagers;
using Metatrader_Auto_Optimiser.Model.FileReaders;
using Metatrader_Auto_Optimiser.Model.OptimisationManagers;
using Metatrader_Auto_Optimiser.Model.Terminal;
using Metatrader_Auto_Optimiser.View_Model;
using ReportManager;

namespace Metatrader_Auto_Optimiser.Model
{
    /// <summary>
    /// Статическая фабрика модели
    /// </summary>
    class MainModelCreator
    {
        /// <summary>
        /// Статический конструктор
        /// </summary>
        public static IMainModel Model => new MainModel();
    }

    /// <summary>
    /// Модель данных основного окна
    /// </summary>
    class MainModel : IMainModel
    {
        /// <summary>
        /// Деструктор
        /// </summary>
        ~MainModel()
        {
            if (Optimiser.IsOptimisationInProcess)
                StopOptimisation();

            Optimiser.ProcessStatus -= Optimiser_ProcessStatus;
            Optimiser.OptimisationProcessFinished -= Optimiser_OptimisationProcessFinished;
        }

        /// <summary>
        /// Диспатчер основного окна
        /// </summary>
        private readonly System.Windows.Threading.Dispatcher dispatcher =
            System.Windows.Application.Current.Dispatcher;

        /// <summary>
        /// Коллбек события обновления прогресс бара
        /// </summary>
        /// <param name="arg1">Статус</param>
        /// <param name="arg2">Прогресс</param>
        private void Optimiser_ProcessStatus(string arg1, double arg2)
        {
            // Переадресация вызова аналогичному событию модели
            PBUpdate(arg1, arg2);
        }
        /// <summary>
        /// Коллбек окончания процеса оптимизации
        /// </summary>
        /// <param name="obj">Оптимизатор</param>
        private void Optimiser_OptimisationProcessFinished(IOptimiser optimiser)
        {
            DirectoryInfo cachDir = optimiser.TerminalManager.TerminalChangeableDirectory
                                                             .GetDirectory("Tester")
                                                             .GetDirectory("cache", true);

            workingDirectory.Tester.GetDirectory("cache", true).GetFiles().ToList()
                     .ForEach(x =>
                     {
                         string path = Path.Combine(cachDir.FullName, x.Name);
                         if (!File.Exists(path))
                             x.MoveTo(path);
                     });

            SaveOptimisations(optimiser);
            if (LoadingOptimisationTougle)
                LoadSavedOptimisation(optimiser.OptimiserWorkingDirectory);
            OptimisationStoped();
            Optimiser.ClearOptimiser();
        }
        /// <summary>
        /// Метод созраняющий оптимизации в локальной директории
        /// </summary>
        /// <param name="optimiser">Оптимизатор</param>
        private void SaveOptimisations(IOptimiser optimiser)
        {
            // Проверка на существование имени директории с оптимизациями
            if (string.IsNullOrEmpty(optimiser.OptimiserWorkingDirectory) ||
                string.IsNullOrWhiteSpace(optimiser.OptimiserWorkingDirectory))
            {
                return;
            }

            // Создаем массивы с результатами оптимизации
            List<OptimisationResult> AllOptimisationResults = new List<OptimisationResult>();
            List<OptimisationResult> ForwardOptimisations = new List<OptimisationResult>();
            List<OptimisationResult> HistoryOptimisations = new List<OptimisationResult>();

            // Получаем список всех файлов с расширением (*.xml) в локальной директории с оптимизациями
            List<FileInfo> files = Directory.GetFiles(optimiser.OptimiserWorkingDirectory, "*.xml").Select(x => new FileInfo(x)).ToList();

            // Вложенная функция заполняющая созданные выше массивы ранее созраненными результатами оптимизации 
            // Работает лишь если мы дополняем существующие, в случае когда мы перезаписываем старые результаты - не вызывается
            bool FillIn(List<OptimisationResult> results, IEnumerable<DateBorders> currentBorders, string fileName)
            {
                // Заполнение массива и получение имени эксперкта, депозита, валюты депозита и плеча
                results.AddRange(GetItems(files.Find(x => x.Name == fileName),
                    out string expert, out double deposit, out string currency, out int laverage));

                // Проверка имени эксперта, депозита, валюты депозита и плеча 
                // на соответствие заданным при старте текущей оптимищации оптимизации
                if (expert != optimiser.PathToBot || deposit != optimiser.Balance ||
                   currency != optimiser.Currency || laverage != optimiser.Laverage)
                {
                    System.Windows.MessageBox.Show("Can`t append data into files with different optimiser settings (path to bot / balance / currency / laverage)");
                    return false;
                }

                // Отчищаем из старых массивов те резальтаты что соответствующи датам уже имеющимся в новой оптимизации
                foreach (var item in currentBorders)
                {
                    results.RemoveAll(x => x.report.DateBorders == item);
                }

                return true;
            }

            // Задаем шаг обновления Прогресс бара
            double step = 100.0 / 4;

            // Если существуют все 3 файла с оптимизациями в локальной директории с оптимизаациями 
            // заполняем их через описанную выше локальную функцию
            if (files.Any(x => x.Name == "Report.xml") &&
                files.Any(x => x.Name == "History.xml") &&
                files.Any(x => x.Name == "Forward.xml"))
            {
                PBUpdate("Reading files", step);
                if (!FillIn(AllOptimisationResults, optimiser.AllOptimisationResults.Select(x => x.report.DateBorders).Distinct(), "Report.xml"))
                    return;
                if (!FillIn(ForwardOptimisations, optimiser.ForwardOptimisations.Select(x => x.report.DateBorders).Distinct(), "Forward.xml"))
                    return;
                if (!FillIn(HistoryOptimisations, optimiser.HistoryOptimisations.Select(x => x.report.DateBorders).Distinct(), "History.xml"))
                    return;
            }
            else
                PBUpdate("Saving files", step);
            // Учаляем все (*xml) файлы из локальной директории с файлами оптимизациями
            files.ForEach(x => x.Delete());

            // Копируем в локальные копии результатов оптимизации - все прооптимизированные результаты из оптимизатора
            AllOptimisationResults.AddRange(optimiser.AllOptimisationResults);
            HistoryOptimisations.AddRange(optimiser.HistoryOptimisations);
            ForwardOptimisations.AddRange(optimiser.ForwardOptimisations);

            // Локальная функция созраняющая результаты оптимизации в файл
            void WriteFile(List<OptimisationResult> results, string fileName)
            {
                results.ReportWriter(optimiser.PathToBot, optimiser.Currency, optimiser.Balance, optimiser.Laverage,
                                                    Path.Combine(optimiser.OptimiserWorkingDirectory, fileName));
            }

            PBUpdate("Save All optimisations", step * 2);

            // Проверяем список всех проведенных оптимизаций и созраняем их если таковые были найдены
            if (AllOptimisationResults.Count > 0)
                WriteFile(AllOptimisationResults, "Report.xml");
            else
            {
                System.Windows.MessageBox.Show("There are no optimisation data to save");
                return;
            }

            // Создаем пустую запись оптимизации - нужна для того что бы что то бы созранить что то в файл
            var emptyItem = new OptimisationResult
            {
                report = new ReportManager.ReportItem
                {
                    BotParams = new Dictionary<string, string>(),
                    DateBorders = new DateBorders(DTExtention.UnixEpoch, DTExtention.UnixEpoch.AddDays(1)),
                    OptimisationCoefficients = new Coefficients
                    {
                        TradingDays = new Dictionary<DayOfWeek, DailyData>
                                {
                                    {DayOfWeek.Monday, new DailyData() },
                                    {DayOfWeek.Tuesday, new DailyData() },
                                    {DayOfWeek.Wednesday, new DailyData() },
                                    {DayOfWeek.Thursday, new DailyData() },
                                    {DayOfWeek.Friday, new DailyData() }
                                }
                    }
                }
            };

            // Созраняем пустую строку оптимизации если не было не одной оптимизации
            if (HistoryOptimisations.Count == 0)
                HistoryOptimisations.Add(emptyItem);
            if (ForwardOptimisations.Count == 0)
                ForwardOptimisations.Add(emptyItem);

            // Созраняем в файл рещультаты тестов форвардных и исторических
            PBUpdate("Save History tests", step * 3);
            WriteFile(HistoryOptimisations, "History.xml");

            PBUpdate("Save Forward tests", step * 4);
            WriteFile(ForwardOptimisations, "Forward.xml");

        }

        #region Directory managers
        /// <summary>
        /// Менеджер директорий терминалов
        /// </summary>
        private readonly TerminalDirectory terminalDirectory = new TerminalDirectory();
        /// <summary>
        /// Менеджер локальной рабочей директории
        /// </summary>
        private readonly WorkingDirectory workingDirectory = new WorkingDirectory();
        #endregion

        /// <summary>
        /// Список фабрик оптимизаторов
        /// </summary>
        private readonly List<OptimiserCreator> optimiserCreators = Optimisers.Creators;

        #region Getters
        /// <summary>
        /// Выбранный оптимизатор
        /// </summary>
        public IOptimiser Optimiser { get; private set; }
        /// <summary>
        /// Список имен терминалов (их ID)
        /// </summary>
        public IEnumerable<string> TerminalNames => terminalDirectory.Terminals.Select(x => x.Name);
        /// <summary>
        /// Список имен оптимиазторов
        /// </summary>
        public IEnumerable<string> OptimisatorNames => optimiserCreators.Select(x => x.Name);
        /// <summary>
        /// Список имен произведенных оптимизаций
        /// </summary>
        public IEnumerable<string> SavedOptimisations => workingDirectory.Reports.GetDirectories().Select(x => x.Name);
        /// <summary>
        /// Результаты всех произведенных оптимизаций
        /// </summary>
        public ReportData AllOptimisationResults { get; private set; } = new ReportData
        {
            AllOptimisationResults = new Dictionary<DateBorders, List<OptimisationResult>>()
        };
        /// <summary>
        /// Результаты форвардных тестов
        /// </summary>
        public ObservableCollection<View_Model.ReportItem> ForwardOptimisations { get; } = new ObservableCollection<View_Model.ReportItem>();
        /// <summary>
        /// Результаты исторических тестов
        /// </summary>
        public ObservableCollection<View_Model.ReportItem> HistoryOptimisations { get; } = new ObservableCollection<View_Model.ReportItem>();

        #endregion

        #region Events
        /// <summary>
        /// Событие переадресации текста ошибки в графический интерфейс
        /// </summary>
        public event Action<string> ThrowException;
        /// <summary>
        /// Событие завершения процесса оптимизации
        /// </summary>
        public event Action OptimisationStoped;
        /// <summary>
        /// Событие обновления прогресс бара
        /// </summary>
        public event Action<string, double> PBUpdate;
        /// <summary>
        /// Событие изменения какого либо из свойств ViewModel 
        /// и его обработчики
        /// </summary>
        #region PropertyChanged Event
        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// Обработчик события PropertyChanged
        /// </summary>
        /// <param name="propertyName">Имя обновляемой переменной</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #endregion

        #region Methods
        /// <summary>
        /// Метод смены оптимизатора
        /// </summary>
        /// <param name="optimiserName">Имя выбранного оптимизатора</param>
        /// <returns>true в слечае успеха</returns>
        public bool ChangeOptimiser(string optimiserName, string terminalName)
        {
            // Еслизапущен процесс оптимизации или же запущен терминалл - то не чего не делаем
            if (Optimiser != null &&
                (Optimiser.IsOptimisationInProcess || Optimiser.TerminalManager.IsActive))
            {
                return false;
            }

            // Если текущий оптимизатор не пуст
            if (Optimiser != null)
            {
                // Отписываемся от событий прошлого оптимизатора
                Optimiser.ProcessStatus -= Optimiser_ProcessStatus;
                Optimiser.OptimisationProcessFinished -= Optimiser_OptimisationProcessFinished;
                Optimiser.ClearOptimiser();
                Optimiser.CloseSettingsWindow();

                // Создаем новый оптимизатор и копируем старый менеджер терминалла
                Optimiser = optimiserCreators.First(x => x.Name == optimiserName).Create(Optimiser.TerminalManager);
            }
            else
            {
                try
                {
                    // Создаем новый оптимизатор и создаем менеджер терминалла
                    Optimiser = optimiserCreators.First(x => x.Name == optimiserName)
                                                 .Create(new TerminalManager(terminalDirectory.Terminals.First(x => x.Name == terminalName)));
                }
                catch (Exception e)
                {
                    // В случае ошибки - переадресоввываем её текст в графический интерфейс
                    ThrowException(e.Message);
                    return false;
                }
            }

            // Подписываемся на события нового оптимизатора
            Optimiser.ProcessStatus += Optimiser_ProcessStatus;
            Optimiser.OptimisationProcessFinished += Optimiser_OptimisationProcessFinished;
            return true;
        }
        /// <summary>
        /// Метод изменяющий терминалл
        /// </summary>
        /// <param name="terminalName">Имя нового терминалла</param>
        /// <returns>true в случае успеха</returns>
        public bool ChangeTerminal(string terminalName)
        {
            if (Optimiser.IsOptimisationInProcess || Optimiser.TerminalManager.IsActive)
                return false;
            try
            {
                Optimiser.TerminalManager = new TerminalManager(terminalDirectory.Terminals.First(x => x.Name == terminalName));
            }
            catch (Exception e)
            {
                ThrowException(e.Message);
                return false;
            }

            return true;
        }
        /// <summary>
        /// Получение параметров для выбранного эксперта
        /// </summary>
        /// <param name="botName">Имя эксперта</param>
        /// <param name="terminalName">Имя терминалла</param>
        /// <returns>Параметры эксперта</returns>
        public IEnumerable<ParamsItem> GetBotParams(string botName, bool isUpdate)
        {
            if (botName == null)
                return null;

            FileInfo setFile = new FileInfo(Path.Combine(Optimiser
                                           .TerminalManager
                                           .TerminalChangeableDirectory
                                           .GetDirectory("MQL5")
                                           .GetDirectory("Profiles")
                                           .GetDirectory("Tester")
                                           .FullName, $"{Path.GetFileNameWithoutExtension(botName)}.set"));


            try
            {
                if (isUpdate)
                {
                    if (Optimiser.TerminalManager.IsActive)
                    {
                        ThrowException("Wating for closing terminal");
                        Optimiser.TerminalManager.WaitForStop();
                    }
                    if (setFile.Exists)
                        setFile.Delete();

                    FileInfo iniFile = terminalDirectory.Terminals
                                                        .First(x => x.Name == Optimiser.TerminalManager.TerminalID)
                                                        .GetDirectory("config")
                                                        .GetFiles("common.ini").First();

                    Config config = new Config(iniFile.FullName);

                    config = config.DublicateFile(Path.Combine(workingDirectory.WDRoot.FullName, $"{Optimiser.TerminalManager.TerminalID}.ini"));

                    config.Tester.Expert = botName;
                    config.Tester.FromDate = DateTime.Now;
                    config.Tester.ToDate = config.Tester.FromDate.Value.AddDays(-1);
                    config.Tester.Optimization = ENUM_OptimisationMode.Disabled;
                    config.Tester.Model = ENUM_Model.OHLC_1_minute;
                    config.Tester.Period = ENUM_Timeframes.D1;
                    config.Tester.ShutdownTerminal = true;
                    config.Tester.UseCloud = false;
                    config.Tester.Visual = false;

                    Optimiser.TerminalManager.WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized;
                    Optimiser.TerminalManager.Config = config;

                    if (Optimiser.TerminalManager.Run())
                        Optimiser.TerminalManager.WaitForStop();

                    if (!File.Exists(setFile.FullName))
                        return null;

                    SetFileManager setFileManager = new SetFileManager(setFile.FullName, false);
                    return setFileManager.Params;
                }
                else
                {
                    if (!setFile.Exists)
                        return GetBotParams(botName, true);

                    SetFileManager setFileManager = new SetFileManager(setFile.FullName, false);
                    if (setFileManager.Params.Count == 0)
                        return GetBotParams(botName, true);

                    return setFileManager.Params;
                }
            }
            catch (Exception e)
            {
                ThrowException(e.Message);
                return null;
            }
        }
        /// <summary>
        /// Загрузка оптимизаций из файлов
        /// </summary>
        /// <param name="optimisationName">Имя директории с оптимизациями</param>
        public async void LoadSavedOptimisation(string optimisationName)
        {
            double step = 100.0 / 4.0;
            // Выбор директории с оптимизациями
            DirectoryInfo selectedDir = workingDirectory.Reports.GetDirectory(optimisationName);
            // Проверка директории на существование
            #region Check
            if (selectedDir == null)
            {
                ThrowException("Can`t get directory");
                return;
            }
            #endregion

            // Отчистка ранее созраненных результатов
            ClearOptimisationFields();

            // ЗАпуск вторичного потока с загрузкой результатов из файлов
            await Task.Run(() =>
            {
                try
                {
                    PBUpdate("Getting files", step);
                    // Получаем список файлов с расширением (*xml)
                    FileInfo[] files = selectedDir.GetFiles("*.xml");

                    // Если существуют все 3 файла - загружаем оптимизации
                    if (files.Any(x => x.Name == "Report.xml") &&
                       files.Any(x => x.Name == "History.xml") &&
                       files.Any(x => x.Name == "Forward.xml"))
                    {
                        // Создаем новый отчет всех прозведенных оптимизаций 
                        ReportData reportData = new ReportData
                        {
                            AllOptimisationResults = new Dictionary<DateBorders, List<OptimisationResult>>()
                        };

                        PBUpdate("Results.xml", step * 2);
                        // Читаем файл со всеми произведенными оптимизациями и занвсим результаты в соответствующие переменные
                        List<OptimisationResult> report = GetItems(files.First(x => x.Name == "Report.xml"),
                                                                   out string expert, out double deposit,
                                                                   out string currency, out int laverage);
                        reportData.Expert = expert;
                        reportData.Deposit = deposit;
                        reportData.Currency = currency;
                        reportData.Laverage = laverage;

                        #region Check
                        // Локальная функция проверяющая верность настроек тестера для всех файлов
                        void CompareTestersettings()
                        {
                            if (expert != reportData.Expert)
                                throw new Exception("Experts are different");
                            if (deposit != reportData.Deposit)
                                throw new Exception("Deposits are different");
                            if (currency != reportData.Currency)
                                throw new Exception("Currencies are different");
                            if (laverage != reportData.Laverage)
                                throw new Exception("Lavreges are different");
                        }

                        // Проверяем существуют ли записи в файле с настройками оптимизаций 
                        if (report.Count == 0)
                            throw new Exception("File 'Report.xml' is empty or can`t be read");
                        #endregion

                        // Добавляем разбитую на даты историю оптимизаций из файла
                        List<DateBorders> dates = report.Select(x => x.report.DateBorders).Distinct().ToList();
                        foreach (var item in dates)
                        {
                            reportData.AllOptimisationResults.Add(item,
                                new List<OptimisationResult>(report.Where(x => x.report.DateBorders == item)));
                        }
                        // Созраняем прочтенную историю всех произведенных оптимизаций
                        AllOptimisationResults = reportData;

                        // СОзраняем историю исторических тестов и производим проверку на корректность настроек тестера
                        PBUpdate("History.xml", step * 3);
                        dispatcher.Invoke(() => GetItems(files.First(x => x.Name == "History.xml"),
                                 out expert, out deposit,
                                 out currency, out laverage).OrderBy(x => x.report.DateBorders)
                                 .ToList().ForEach(x => HistoryOptimisations.Add(x)));
                        #region Check
                        CompareTestersettings();
                        #endregion

                        // СОзраняем историю форвардных тестов и производим проверку на корректность настроек тестера
                        PBUpdate("Forward.xml", step * 4);
                        dispatcher.Invoke(() => GetItems(files.First(x => x.Name == "Forward.xml"),
                                 out expert, out deposit,
                                 out currency, out laverage).OrderBy(x => x.report.DateBorders)
                                 .ToList().ForEach(x => ForwardOptimisations.Add(x)));
                        #region Check
                        CompareTestersettings();
                        if (HistoryOptimisations.Count == 0)
                            throw new Exception("File 'History.xml' is empty or can`t be read");
                        if (ForwardOptimisations.Count == 0)
                            throw new Exception("File 'Forward.xml' is empty or can`t be read");
                        /*
                        foreach (var item in ForwardOptimisations)
                        {
                            if (!HistoryOptimisations.Contains(item) && item.report.BotParams.Count > 0)
                                throw new Exception("Can`t find forward item in History Optimisations array");
                        }*/
                        #endregion
                    }
                }
                catch (Exception e)
                {
                    // В случае ошибки все чистим
                    ClearOptimisationFields();

                    ThrowException(e.Message);
                }
            });

            PBUpdate(null, 0);

            // Информируем графику о произведенной перезаписи результатов оптимизации
            OnPropertyChanged("AllOptimisationResults");
        }
        /// <summary>
        /// Clear fields with optimisations
        /// </summary>
        void ClearOptimisationFields()
        {
            if (HistoryOptimisations.Count > 0)
                dispatcher.Invoke(() => HistoryOptimisations.Clear());
            if (ForwardOptimisations.Count > 0)
                dispatcher.Invoke(() => ForwardOptimisations.Clear());
            if (AllOptimisationResults.AllOptimisationResults.Count > 0)
            {
                AllOptimisationResults.AllOptimisationResults.Clear();
                AllOptimisationResults = new ReportData
                {
                    AllOptimisationResults = new Dictionary<DateBorders, List<OptimisationResult>>()
                };
            }

            GC.Collect();
        }
        public void ClearResults()
        {
            ClearOptimisationFields();
            OnPropertyChanged("AllOptimisationResults");
            OnPropertyChanged("ClearResults");
        }
        /// <summary>
        /// Чтение файла с результатами оптимизаций
        /// </summary>
        /// <param name="file">Файл</param>
        /// <param name="expert">Имя жксперта</param>
        /// <param name="deposit">Депозит</param>
        /// <param name="currensy">Валюта депозита</param>
        /// <param name="laverage">Кредитное плечо</param>
        /// <returns>Список результатов оптимизации</returns>
        private List<OptimisationResult> GetItems(FileInfo file, out string expert,
                                          out double deposit, out string currensy, out int laverage)
        {
            List<OptimisationResult> ans = new List<OptimisationResult>();
            using (ReportReader reader = new ReportReader(file.FullName))
            {
                expert = reader.RelativePathToBot;
                deposit = reader.Balance;
                currensy = reader.Currency;
                laverage = reader.Laverage;

                while (reader.Read())
                {
                    if (reader.ReportItem.HasValue)
                        ans.Add(reader.ReportItem.Value);
                }
            }

            return ans;
        }
        /// <summary>
        /// Созраняем отчет оптимизаций за выбранные даты в (*.csv) файл
        /// </summary>
        /// <param name="dateBorders">Выбранные временные границы</param>
        /// <param name="pathToSavingFile">Путь к созраняемому файлу</param>
        public void SaveToCSVOptimisations(DateBorders dateBorders, string pathToSavingFile)
        {
            List<View_Model.ReportItem> results = new List<View_Model.ReportItem>();
            if (dateBorders != null)
                results.AddRange(AllOptimisationResults.AllOptimisationResults[dateBorders].Select(x => (View_Model.ReportItem)x));

            CreateCsv(results, Path.Combine(Path.GetDirectoryName(pathToSavingFile), $"{Path.GetFileNameWithoutExtension(pathToSavingFile)}.csv"), true);
        }
        /// <summary>
        /// Созранение исторических и форвардных тестов в файл
        /// </summary>
        /// <param name="pathToSavingFile">Путь к сохраняемому файлу</param>
        public void SaveToCSVSelectedOptimisations(string pathToSavingFile)
        {
            string file_name = Path.GetFileNameWithoutExtension(pathToSavingFile);
            string path = Path.GetDirectoryName(pathToSavingFile);

            CreateCsv(HistoryOptimisations, Path.Combine(path, $"History_{file_name}.csv"), false);
            CreateCsv(ForwardOptimisations, Path.Combine(path, $"Forward_{file_name}.csv"), true);
        }
        /// <summary>
        /// Запуск оптимизаций
        /// </summary>
        /// <param name="optimiserInputData">Входные данные для оптимизатора</param>
        /// <param name="isAppend">Флаг - нужно ли дополнять файл ?</param>
        /// <param name="dirPrefix">Префикс директории</param>
        public async void StartOptimisation(OptimiserInputData optimiserInputData, bool isAppend, string dirPrefix, List<string> assets)
        {

            if (assets.Count == 0)
            {
                ThrowException("Fill in asset name");
                OnPropertyChanged("ResumeEnablingTogle");
                return;
            }


            await Task.Run(() =>
            {
                try
                {
                    if (optimiserInputData.OptimisationMode == ENUM_OptimisationMode.Disabled &&
                       assets.Count > 1)
                    {
                        throw new Exception("For test there mast be selected only one asset");
                    }
                    StopOptimisationTougle = false;

                    bool doWhile()
                    {
                        if (assets.Count == 0)
                            return false;
                        if(StopOptimisationTougle)
                        {
                            LoadingOptimisationTougle = true;
                            OnPropertyChanged("ResumeEnablingTogle");

                            return false;
                        }

                        optimiserInputData.Symb = assets.First();
                        LoadingOptimisationTougle = assets.Count == 1;

                        assets.Remove(assets.First());

                        return true;
                    }

                    while (doWhile())
                        StartOptimisation(optimiserInputData, isAppend, dirPrefix);
                }
                catch (Exception e)
                {
                    LoadingOptimisationTougle = true;
                    OnPropertyChanged("ResumeEnablingTogle");
                    ThrowException?.Invoke(e.Message);
                }
            });
        }
        private void StartOptimisation(OptimiserInputData optimiserInputData, bool isAppend, string dirPrefix)
        {
            if ((optimiserInputData.HistoryBorders.Count == 0 && optimiserInputData.ForwardBorders.Count == 0))
            {
                ThrowException("Fill in date borders");
                OnPropertyChanged("ResumeEnablingTogle");
                return;
            }

            if (Optimiser.TerminalManager.IsActive)
            {
                ThrowException("Terminal already running");
                return;
            }

            if (optimiserInputData.OptimisationMode == ENUM_OptimisationMode.Disabled)
            {
                StartTest(optimiserInputData);
                return;
            }

            if (!isAppend)
            {
                var dir = workingDirectory.GetOptimisationDirectory(optimiserInputData.Symb,
                                                          Path.GetFileNameWithoutExtension(optimiserInputData.RelativePathToBot),
                                                          dirPrefix, Optimiser.Name);
                List<FileInfo> data = dir.GetFiles().ToList();
                data.ForEach(x => x.Delete());
                List<DirectoryInfo> dirData = dir.GetDirectories().ToList();
                dirData.ForEach(x => x.Delete());
            }

            Optimiser.CloseSettingsWindow();

            try
            {
                DirectoryInfo cachDir = Optimiser.TerminalManager.TerminalChangeableDirectory
                                                         .GetDirectory("Tester")
                                                         .GetDirectory("cache", true);
                DirectoryInfo cacheCopy = workingDirectory.Tester.GetDirectory("cache", true);
                cacheCopy.GetFiles().ToList().ForEach(x => x.Delete());
                cachDir.GetFiles().ToList()
                       .ForEach(x => x.MoveTo(Path.Combine(cacheCopy.FullName, x.Name)));

                ClearResults();
                Optimiser.ClearOptimiser();

                int ind = optimiserInputData.BotParams.FindIndex(x => x.Variable == Fixed_Input_Settings.Params[InputParamName.CloseTerminalFromBot]);
                if (ind > -1)
                {
                    var item = optimiserInputData.BotParams[ind];
                    item.Value = "true";
                    optimiserInputData.BotParams[ind] = item;
                }
                var botParams = optimiserInputData.BotParams.ToList(); // clone expert settings

                Optimiser.Start(optimiserInputData,
                    Path.Combine(terminalDirectory.Common.FullName,
                    $"{Path.GetFileNameWithoutExtension(optimiserInputData.RelativePathToBot)}_Report.xml"), dirPrefix);

                SetFileManager fileManager = new SetFileManager(
                    Path.Combine(workingDirectory.GetOptimisationDirectory(optimiserInputData.Symb,
                                                                           Path.GetFileNameWithoutExtension(optimiserInputData.RelativePathToBot),
                                                                           dirPrefix, Optimiser.Name).FullName, "OptimisationSettings.set"), true)
                {
                    Params = botParams
                };
                fileManager.SaveParams();

                Optimiser.TerminalManager.WaitForStop();
            }
            catch (Exception e)
            {
                Optimiser.Stop();
                throw e;
            }
        }
        public bool LoadingOptimisationTougle { get; private set; } = true;
        /// <summary>
        /// Запуск тестов
        /// </summary>
        /// <param name="optimiserInputData">Взодные данные для тестера</param>
        public async void StartTest(OptimiserInputData optimiserInputData)
        {
            // Проверка не запущен ли терминалл
            if (Optimiser.TerminalManager.IsActive)
            {
                ThrowException("Terminal already running");
                return;
            }

            // Задаем диаппазон дат
            #region From/Forward/To
            DateTime Forward = new DateTime();
            DateTime ToDate = Forward;
            DateTime FromDate = Forward;

            // Проверка на количество переданных дат. Максимум одна историческая и одна форвардная
            if (optimiserInputData.HistoryBorders.Count > 1 ||
                optimiserInputData.ForwardBorders.Count > 1)
            {
                ThrowException("For test there mast be from 1 to 2 date borders");
                OnPropertyChanged("ResumeEnablingTogle");
                return;
            }

            // Если передана и историческая и форвардная даты
            if (optimiserInputData.HistoryBorders.Count == 1 &&
                optimiserInputData.ForwardBorders.Count == 1)
            {
                // Тестируем на корректность заданного промежутка
                DateBorders _Forward = optimiserInputData.ForwardBorders[0];
                DateBorders _History = optimiserInputData.HistoryBorders[0];

                if (_History > _Forward)
                {
                    ThrowException("History optimisation mast be less than Forward");
                    OnPropertyChanged("ResumeEnablingTogle");
                    return;
                }

                // Запоминаем даты
                Forward = _Forward.From;
                FromDate = _History.From;
                ToDate = (_History.Till < _Forward.Till ? _Forward.Till : _History.Till);
            }
            else // Если передана лишь форвардная или же лишь историческая дата
            {
                // Созраняем их и считаем что это была историческая дата (даже если была передана форвардная)
                if (optimiserInputData.HistoryBorders.Count > 0)
                {
                    FromDate = optimiserInputData.HistoryBorders[0].From;
                    ToDate = optimiserInputData.HistoryBorders[0].Till;
                }
                else
                {
                    FromDate = optimiserInputData.ForwardBorders[0].From;
                    ToDate = optimiserInputData.ForwardBorders[0].Till;
                }
            }
            #endregion

            PBUpdate("Start test", 100);

            // Запукаем тест во вторичном потоке
            await Task.Run(() =>
            {
                try
                {
                    // Создаем файл с настройкамит эксперта
                    #region Create (*.set) file
                    FileInfo file = new FileInfo(Path.Combine(Optimiser
                                                    .TerminalManager
                                                    .TerminalChangeableDirectory
                                                    .GetDirectory("MQL5")
                                                    .GetDirectory("Profiles")
                                                    .GetDirectory("Tester")
                                                    .FullName, $"{Path.GetFileNameWithoutExtension(optimiserInputData.RelativePathToBot)}.set"));

                    List<ParamsItem> botParams = new List<ParamsItem>(GetBotParams(optimiserInputData.RelativePathToBot, false));

                    // Заполняем настройки эксперта теми что были введены в графическом интерфейсе
                    for (int i = 0; i < optimiserInputData.BotParams.Count; i++)
                    {
                        var item = optimiserInputData.BotParams[i];

                        int ind = botParams.FindIndex(x => x.Variable == item.Variable);
                        if (ind != -1)
                        {
                            var param = botParams[ind];
                            param.Value = item.Value;
                            botParams[ind] = param;
                        }
                    }

                    // Созраянем настройки в файл
                    SetFileManager setFile = new SetFileManager(file.FullName, false)
                    {
                        Params = botParams
                    };
                    setFile.SaveParams();
                    #endregion

                    // Создаем конфиг терминала
                    #region Create config file
                    Config config = new Config(Optimiser.TerminalManager
                                                        .TerminalChangeableDirectory
                                                        .GetDirectory("config")
                                                        .GetFiles("common.ini")
                                                        .First().FullName);
                    config = config.DublicateFile(Path.Combine(workingDirectory.WDRoot.FullName, $"{Optimiser.TerminalManager.TerminalID}.ini"));

                    config.Tester.Currency = optimiserInputData.Currency;
                    config.Tester.Deposit = optimiserInputData.Balance;
                    config.Tester.ExecutionMode = optimiserInputData.ExecutionDelay;
                    config.Tester.Expert = optimiserInputData.RelativePathToBot;
                    config.Tester.ExpertParameters = setFile.FileInfo.Name;
                    config.Tester.ForwardMode = (Forward == new DateTime() ? ENUM_ForvardMode.Disabled : ENUM_ForvardMode.Custom);
                    if (config.Tester.ForwardMode == ENUM_ForvardMode.Custom)
                        config.Tester.ForwardDate = Forward;
                    else
                        config.DeleteKey(ENUM_SectionType.Tester, "ForwardDate");
                    config.Tester.FromDate = FromDate;
                    config.Tester.ToDate = ToDate;
                    config.Tester.Leverage = $"1:{optimiserInputData.Laverage}";
                    config.Tester.Model = optimiserInputData.Model;
                    config.Tester.Optimization = ENUM_OptimisationMode.Disabled;
                    config.Tester.Period = optimiserInputData.TF;
                    config.Tester.ShutdownTerminal = false;
                    config.Tester.Symbol = optimiserInputData.Symb;
                    config.Tester.Visual = false;
                    #endregion

                    // Конфигурируем терминалл и запускаем его
                    Optimiser.TerminalManager.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
                    Optimiser.TerminalManager.Config = config;
                    Optimiser.TerminalManager.Run();

                    // Ожидаем закрытие терминала
                    Optimiser.TerminalManager.WaitForStop();
                }
                catch (Exception e)
                {
                    ThrowException(e.Message);
                }

                OnPropertyChanged("StopTest");
            });
        }
        /// <summary>
        /// Завершаем оптимизацию извне оптимизатора
        /// </summary>
        public void StopOptimisation()
        {
            StopOptimisationTougle = true;
            Optimiser.Stop();
        }

        bool StopOptimisationTougle = false;

        /// <summary>
        /// Сортируем загруженный отчет
        /// </summary>
        /// <param name="borders">Выбранный диаппазон дат</param>
        /// <param name="sortingFlags">Выбранные параметры сортировки</param>
        public async void SortResults(DateBorders borders, IEnumerable<SortBy> sortingFlags)
        {
            void UpdatePB(string status, double progress)
            {
                if (Optimiser.IsOptimisationInProcess)
                    return;
                if (Optimiser.TerminalManager.IsActive)
                    return;
                PBUpdate(status, progress);
            }

            UpdatePB("Sorting results", 100);

            await Task.Run(() =>
            {
                AllOptimisationResults.AllOptimisationResults[borders] =
                    AllOptimisationResults.AllOptimisationResults[borders].SortOptimisations(OrderBy.Descending, sortingFlags).ToList();
                OnPropertyChanged("SortedResults");
            });

            UpdatePB(null, 0);
        }
        /// <summary>
        /// Фильтруем результаты оптимизаций
        /// </summary>
        /// <param name="borders">Выбранный диаппазон дат</param>
        /// <param name="compareData">Сопостовляемые флаги</param>
        public async void FilterResults(DateBorders borders, IDictionary<SortBy, KeyValuePair<CompareType, double>> compareData)
        {
            void UpdatePB(string status, double progress)
            {
                if (Optimiser.IsOptimisationInProcess)
                    return;
                if (Optimiser.TerminalManager.IsActive)
                    return;
                PBUpdate(status, progress);
            }

            UpdatePB("Filter Results", 100);

            await Task.Run(() =>
            {
                AllOptimisationResults.AllOptimisationResults[borders] =
                    AllOptimisationResults.AllOptimisationResults[borders].FiltreOptimisations(compareData).ToList();
                OnPropertyChanged("FilteredResults");
            });

            UpdatePB(null, 0);
        }
        /// <summary>
        /// Создаем (*csv) файл
        /// </summary>
        /// <param name="borders"></param>
        /// <param name="pathToFile"></param>
        private async void CreateCsv(IEnumerable<View_Model.ReportItem> results, string pathToFile, bool is_notify)
        {
            if (results.Count() > 0)
            {
                try
                {
                    using (StreamWriter writer = new StreamWriter(pathToFile))
                    {
                        string[] names = Enum.GetNames(typeof(SortBy));
                        string headders = $"From;To;{string.Join(";", names.Select(x => x.Replace("_", " ")))};";

                        await Task.Run(() =>
                        {
                            bool isFirst = true;

                            foreach (var item in results)
                            {
                                if (isFirst)
                                {
                                    isFirst = false;
                                    foreach (var param in ((OptimisationResult)item).report.BotParams)
                                    {
                                        headders += $"{param.Key};";
                                    }
                                    writer.WriteLine(headders);
                                }

                                string line = $"{((OptimisationResult)item).report.DateBorders.From.ToString("dd.MM.yyyy HH:mm:ss")};" +
                                             $"{((OptimisationResult)item).report.DateBorders.Till.ToString("dd.MM.yyyy HH:mm:ss")};";
                                foreach (var param in names)
                                {
                                    line += $"{((OptimisationResult)item).GetResult((SortBy)Enum.Parse(typeof(SortBy), param))};";
                                }

                                foreach (var param in ((OptimisationResult)item).report.BotParams)
                                {
                                    line += $"{param.Value};";
                                }

                                writer.WriteLine(line);
                            }
                        });
                    }

                    if (is_notify)
                        OnPropertyChanged("CSV");
                }
                catch (Exception e)
                {
                    ThrowException(e.Message);
                }
            }
        }

        public IEnumerable<ParamsItem> GetBotParamsFromOptimisationPass(string botName, string optimisationName)
        {
            var files = workingDirectory.Reports.GetDirectory(optimisationName)?.GetFiles();
            if (!files.Any(x => x.Name == "Forward.xml"))
                throw new Exception("Can`t find file named 'Forward.xml'");
            var forwardOptimisations = files.First(x => x.Name == "Forward.xml");
            if (!files.Any(x => x.Name == "OptimisationSettings.set"))
                throw new Exception("Can`t find 'OptimisationSettings.set' file");
            var setFile = files.First(x => x.Name == "OptimisationSettings.set");

            using (ReportReader reader = new ReportReader(forwardOptimisations.FullName))
            {
                if (reader.RelativePathToBot != botName)
                    throw new Exception($"Expected {reader.RelativePathToBot} expert, but selected {botName}");
            }

            SetFileManager setFileReader = new SetFileManager(setFile.FullName, false);

            return setFileReader.Params;
        }
        #endregion
    }


}
