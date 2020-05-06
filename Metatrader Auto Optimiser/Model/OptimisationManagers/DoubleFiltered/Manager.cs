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

    class Manager : ManagerBase
    {
        public Manager(string name) : base(name)
        {
            SubFormKeeper = new View_Model.SubFormKeeper(() => { return new Settings(); });
        }
        ~Manager() { SubFormKeeper.Close(); }

        private readonly WorkingDirectory workingDirectory = new WorkingDirectory();

        #region Subwindow

        private readonly View_Model.SubFormKeeper SubFormKeeper;
        private readonly Settings_M Settings = Settings_M.Instance();

        public override void LoadSettingsWindow()
        {
            SubFormKeeper.Open();
        }

        #endregion
        public override void Start(OptimiserInputData optimiserInputData, string PathToResultsFile, string dirPrefix)
        {
            if (IsOptimisationInProcess || TerminalManager.IsActive)
                throw new Exception("Optimisation already in process or terminal is busy");
            // Устанавливает статус оптимизации и переключатель
            OnProcessStatus("Start optimisation", 0);
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
                OnProcessStatus("Optimisation", step * i++);

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
            OnProcessStatus("Tests", 0);

            configFile.Tester.Optimization = ENUM_OptimisationMode.Disabled;
            configFile.Tester.ShutdownTerminal = true;
            configFile.Tester.Model =
                (Settings.IsTickTest ? ENUM_Model.Every_tick_based_on_real_ticks : ENUM_Model.OHLC_1_minute);
            foreach (var item in TestInputData)
            {
                // Выходим из метода если вдруг остановили оптимизацию извне
                if (!IsOptimisationInProcess)
                    return;

                // Обновляем прогресс бар
                OnProcessStatus("Tests", step * i++);

                Test(item.Key, item.Value.Key, item.Value.Value, configFile, PathToResultsFile);
            }

            CommonMethods.SaveExpertSettings(Path.GetFileNameWithoutExtension(optimiserInputData.RelativePathToBot),
                                             optimiserInputData.BotParams, TerminalManager.TerminalChangeableDirectory);

            // Переключение статуса оптимизаций на завершенный и вызов соответствующего события
            IsOptimisationInProcess = false;
            OnOptimisationProcessFinished(this);
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
            results = results.SortOptimisations(OrderBy.Descending, new[] { Settings.SecondSorter }).ToList();

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
                if (borders == null)
                    return;

                config.Tester.FromDate = borders.From;
                config.Tester.ToDate = borders.Till;

                if (CommonMethods.RunTester(config, TerminalManager) &&
                    CommonMethods.ReadFile(data, pathToReportFile, Currency, Balance, Laverage, PathToBot))
                {
                    var item = data.First();
                    if (item.report.BotParams.Count == 0)
                        item.report.BotParams = botParams.ToDictionary(x => x.Variable, x => x.Value);
                    item.report.DateBorders = borders;
                    collection.Add(item);
                }

                data.Clear();
            }

            tester(HistoryOptimisations, border_history);
            tester(ForwardOptimisations, border_forward);
        }

        public override void CloseSettingsWindow()
        {
            SubFormKeeper.Close();
        }
    }
}
