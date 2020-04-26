using Metatrader_Auto_Optimiser.Model.DirectoryManagers;
using Metatrader_Auto_Optimiser.Model.FileReaders;
using Metatrader_Auto_Optimiser.Model.Terminal;
using ReportManager;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Metatrader_Auto_Optimiser.Model.OptimisationManagers.DoubleFiltered
{
    class DoubleFilterOptimiserCreator : OptimiserCreator
    {
        public DoubleFilterOptimiserCreator() : base("Double Filtered Optimiser")
        { }
        public override IOptimiser Create(TerminalManager terminalManager)
        {
            return new Manager(Name) { TerminalManager = terminalManager };
        }
    }

    class Manager : IOptimiser
    {
        public Manager(string name)
        {
            Name = name;
            SubFormKeeper = new View_Model.SubFormKeeper(() => { return new Settings(); });
        }
        ~Manager() { SubFormKeeper.Close(); }

        #region Terminal manager
        /// <summary>
        /// Хранитель терминалла
        /// </summary>
        private TerminalManager _terminal = null;
        /// <summary>
        /// Геттер / Сеттер для добавления терминалла в оптимизатор
        /// </summary>
        public TerminalManager TerminalManager
        {
            get => _terminal;
            set
            {
                // Если добавляентя null - не чего не делаем
                if (value == null)
                    return;
                // Если сейчас оптимизация в процессе - то останавливаем ее перед добавлением нового терминала
                if (IsOptimisationInProcess)
                    Stop();

                // Если заданный ранее терминалл открыт - закрываем его перед заменой параметра
                if (_terminal != null && _terminal.IsActive)
                {
                    _terminal.Close();
                    _terminal.WaitForStop();
                }
                // ЗАменяем параметр
                _terminal = value;
            }
        }
        #endregion

        private readonly WorkingDirectory workingDirectory = new WorkingDirectory();

        public bool IsOptimisationInProcess { get; protected set; } = false;

        public string Name { get; }

        #region Report getters
        /// <summary>
        /// Отчет оптимизаций для всех указанных исторических временных промежутков
        /// </summary>
        public List<OptimisationResult> AllOptimisationResults { get; } = new List<OptimisationResult>();
        /// <summary>
        /// Форвард тесты
        /// </summary>
        public List<OptimisationResult> ForwardOptimisations { get; } = new List<OptimisationResult>();
        /// <summary>
        /// Исторические тесты
        /// </summary>
        public List<OptimisationResult> HistoryOptimisations { get; } = new List<OptimisationResult>();
        /// <summary>
        /// Валюта
        /// </summary>
        public string Currency { get; protected set; } = null;
        /// <summary>
        /// Баланс
        /// </summary>
        public double Balance { get; protected set; }
        /// <summary>
        /// Кредитное плечо
        /// </summary>
        public int Laverage { get; protected set; }
        /// <summary>
        /// Путь к роботу относительно директории с экспертами
        /// </summary>
        public string PathToBot { get; protected set; } = null;
        /// <summary>
        /// Путь к рабочей директории с изменяемыми файтами
        /// </summary>
        public string OptimiserWorkingDirectory { get; protected set; } = null;
        #endregion

        public event Action<IOptimiser> OptimisationProcessFinished;
        public event Action<string, double> ProcessStatus;

        /// <summary>
        /// Отчистка менеджера оптимизаций
        /// </summary>
        public virtual void ClearOptimiser()
        {
            if (TerminalManager.IsActive || IsOptimisationInProcess)
            {
                throw new Exception("Can`t cleat optimiserbecouse of terminal is active or optimisation is in process");
            }

            AllOptimisationResults.Clear();
            ForwardOptimisations.Clear();
            HistoryOptimisations.Clear();

            Currency = null;
            PathToBot = null;
            OptimiserWorkingDirectory = null;
            Balance = 0;
            Laverage = 0;
        }

        #region Subwindow

        private readonly View_Model.SubFormKeeper SubFormKeeper;
        private readonly Settings_M Settings = Settings_M.Instance();

        public void LoadSettingsWindow()
        {
            SubFormKeeper.Open();
        }

        #endregion
        public void Start(OptimiserInputData optimiserInputData, string PathToResultsFile, string dirPrefix)
        {
            if (IsOptimisationInProcess || TerminalManager.IsActive)
                throw new Exception("Optimisation already in process or terminal is busy");
            // Устанавливает статус оптимизации и переключатель
            ProcessStatus("Start optimisation", 0);
            IsOptimisationInProcess = true;

            // Проверить доступность папки к результатом оптимизации
            if (string.IsNullOrEmpty(PathToResultsFile) ||
               string.IsNullOrWhiteSpace(PathToResultsFile))
            {
                throw new ArgumentException("Path to results file is null or empty");
            }

            // Проверить количество границ исторических оптимизаций
            if (optimiserInputData.HistoryBorders.Count == 0)
                throw new ArgumentException("There are no history optimisations date borders");
            // Проверить количество границ исторических оптимизаций
            if (optimiserInputData.ForwardBorders.Count == 0)
                throw new ArgumentException("There are no forward optimisations date borders");

            // Проверить флаги сортировки
            if (optimiserInputData.SortingFlags.Count() == 0)
                throw new ArgumentException("There are no sorting params");

            // Установка рабочей директории оптимизатора
            OptimiserWorkingDirectory = workingDirectory.GetOptimisationDirectory(optimiserInputData.Symb,
                Path.GetFileNameWithoutExtension(optimiserInputData.RelativePathToBot), dirPrefix, Name).FullName;

            // Уладение существующего файла с резальтатами оптимизаций
            if (File.Exists(PathToResultsFile))
                File.Delete(PathToResultsFile);

            // Установка настроек оптимизатора
            PathToBot = optimiserInputData.RelativePathToBot;
            Currency = optimiserInputData.Currency;
            Balance = optimiserInputData.Balance;
            Laverage = optimiserInputData.Laverage;

            // Создание (*set) файла и созранение в него настроек робота
            string setFileName = CommonMethods.SaveExpertSettings(Path.GetFileNameWithoutExtension(optimiserInputData.RelativePathToBot),
                                             optimiserInputData.BotParams, TerminalManager.TerminalChangeableDirectory);

            // Выбор шага для прогресс бара
            double step = 100.0 / optimiserInputData.HistoryBorders.Count;
            // Счетчик итераций прогресс бара
            int i = 1;

            // Config
            Config configFile = CommonMethods.GetConfig(optimiserInputData,
                                                        setFileName,
                                                        optimiserInputData.HistoryBorders[0],
                                                        TerminalManager.TerminalChangeableDirectory,
                                                        Path.Combine(workingDirectory.WDRoot.FullName, $"{TerminalManager.TerminalID}.ini"));

            Dictionary<DateBorders, DateBorders> historyToForwardBorders =
                DateBorders.CompareHistoryToForward(optimiserInputData.HistoryBorders,
                                                    optimiserInputData.ForwardBorders);

            Dictionary<DateBorders, KeyValuePair<DateBorders, IEnumerable<ParamsItem>>> TestInputData =
                new Dictionary<DateBorders, KeyValuePair<DateBorders, IEnumerable<ParamsItem>>>();

            foreach (var item in optimiserInputData.HistoryBorders)
            {
                // Выходим из метода если вдруг остановили оптимизацию извне
                if (!IsOptimisationInProcess)
                    return;

                // Обновляем прогресс бар
                ProcessStatus("Optimisation", step * i++);

                configFile.Tester.FromDate = item.From;
                configFile.Tester.ToDate = item.Till;

                // Run
                if (!CommonMethods.RunTester(configFile, TerminalManager))
                    continue;

                // Чтение файла с отчетом и удаление его после прочтения
                List<OptimisationResult> results = new List<OptimisationResult>();
                if (!CommonMethods.ReadFile(results, PathToResultsFile, Currency, Balance, Laverage, PathToBot))
                    continue;
                // Устанавливаем для всех прочитанных результатов единые границы дат оптимизаций
                results.ForEach(x =>
                {
                    x.report.DateBorders = item;
                    AllOptimisationResults.Add(x);
                });

                var test_data = FilterResults(results, historyToForwardBorders[item],
                                              optimiserInputData.CompareData,
                                              optimiserInputData.SortingFlags);
                if (test_data.HasValue)
                    TestInputData.Add(item, test_data.Value);
            }

            // Выбор шага для прогресс бара
            step = 100.0 / TestInputData.Count;
            // Счетчик итераций прогресс бара
            i = 1;
            // Обновляем прогресс бар
            ProcessStatus("Tests", 0);

            configFile.Tester.Optimization = ENUM_OptimisationMode.Disabled;
            configFile.Tester.Model =
                (Settings.IsTickTest ? ENUM_Model.Every_tick_based_on_real_ticks : ENUM_Model.OHLC_1_minute);
            foreach (var item in TestInputData)
            {
                // Выходим из метода если вдруг остановили оптимизацию извне
                if (!IsOptimisationInProcess)
                    return;

                // Обновляем прогресс бар
                ProcessStatus("Tests", step * i++);

                Test(item.Key, item.Value.Key, item.Value.Value, configFile, PathToResultsFile);
            }

            // Переключение статуса оптимизаций на завершенный и вызов соответствующего события
            IsOptimisationInProcess = false;
            OptimisationProcessFinished(this);
        }

        private KeyValuePair<DateBorders, IEnumerable<ParamsItem>>? FilterResults(List<OptimisationResult> results,
                                                                          DateBorders borders,
                                                                          IDictionary<SortBy, KeyValuePair<CompareType, double>> CompareData,
                                                                          IEnumerable<SortBy> SortingFlags)
        {
            // Фильтрация
            results = results.FiltreOptimisations(CompareData).ToList();
            if (results.Count == 0)
                return null;
            // Первая сортировка
            results = results.SortOptimisations(OrderBy.Descending,
                                                SortingFlags).ToList();

            // Выбор 100 лучших результатов и повторная сортировка
            results = results.GetRange(0, Math.Min(100, results.Count));
            results.SortOptimisations(OrderBy.Descending, new[] { Settings.SecondSorter });

            // Подготовка параметров и возвращение результатов
            return new KeyValuePair<DateBorders, IEnumerable<ParamsItem>>(borders,
                results.First().report.BotParams
                .Select(x => new ParamsItem
                {
                    Variable = x.Key,
                    Value = x.Value
                }));
        }

        private void Test(DateBorders border_history, DateBorders border_forward,
                          IEnumerable<ParamsItem> botParams, Config config,
                          string pathToReportFile)
        {
            // History test
            CommonMethods.SaveExpertSettings(Path.GetFileNameWithoutExtension(config.Tester.Expert),
                botParams.ToList(), TerminalManager.TerminalChangeableDirectory);

            List<OptimisationResult> data = new List<OptimisationResult>();

            void tester(List<OptimisationResult> collection, DateBorders borders)
            {
                config.Tester.FromDate = borders.From;
                config.Tester.ToDate = borders.Till;

                if (CommonMethods.RunTester(config, TerminalManager) &&
                    CommonMethods.ReadFile(data, pathToReportFile, Currency, Balance, Laverage, PathToBot))
                {
                    var item = data.First();
                    if (item.report.BotParams.Count == 0)
                        item.report.BotParams = botParams.ToDictionary(x => x.Variable, x => x.Value);
                    collection.Add(item);
                }

                data.Clear();
            }

            tester(HistoryOptimisations, border_history);
            tester(ForwardOptimisations, border_forward);
        }

        /// <summary>
        /// Остановка текущего процесса оптимизации и отчистка оптимизатора
        /// </summary>
        public virtual void Stop()
        {
            ProcessStatus("Stoped", 100);

            if (TerminalManager.IsActive)
            {
                TerminalManager.Close();
                TerminalManager.WaitForStop();
            }

            IsOptimisationInProcess = false;
            ClearOptimiser();
            OptimisationProcessFinished(this);
        }

        public void CloseSettingsWindow()
        {
            SubFormKeeper.Close();
        }
    }
}
