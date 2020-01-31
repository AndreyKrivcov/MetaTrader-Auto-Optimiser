using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Metatrader_Auto_Optimiser.Model.DirectoryManagers;
using Metatrader_Auto_Optimiser.Model.FileReaders;
using Metatrader_Auto_Optimiser.Model.Terminal;
using Metatrader_Auto_Optimiser.View_Model;
using ReportManager;

namespace Metatrader_Auto_Optimiser.Model.OptimisationManagers.SimpleForvard
{
    /// <summary>
    /// Optimiser creator
    /// </summary>
    class SimpleOptimiserManagerCreator : OptimiserCreator
    {
        /// <summary>
        /// Creator constructor
        /// </summary>
        /// <param name="workingDirectory">working directory</param>
        public SimpleOptimiserManagerCreator(WorkingDirectory workingDirectory) : base(Name)
        {
            this.workingDirectory = workingDirectory;
        }
        /// <summary>
        /// Wirking directory manager
        /// </summary>
        private readonly WorkingDirectory workingDirectory;
        /// <summary>
        /// Create method
        /// </summary>
        /// <param name="terminalManager">Selected terminal</param>
        /// <returns></returns>
        public override IOptimiser Create(TerminalManager terminalManager)
        {
            return new Manager(workingDirectory)
            {
                TerminalManager = terminalManager
            };
        }
        /// <summary>
        /// Optimiser name
        /// </summary>
        public new static string Name => "Simple forward optimiser";
    }

    class SimpleOptimiserM
    {
        private static SimpleOptimiserM instance;
        SimpleOptimiserM() { }
        public static SimpleOptimiserM Instance()
        {
            if (instance == null)
                instance = new SimpleOptimiserM();

            return instance;
        }

        public bool IsTickTest { get; set; } = true;
        public bool ReplaceDates { get; set; } = false;
        public bool IsDifferentShiftForTicks { get; set; } = false;

        public ObservableCollection<ComissionKeeper> NewShiftAndComission = new ObservableCollection<ComissionKeeper>();
    }

    class SimpleOptimiserVM : INotifyPropertyChanged
    {
        public SimpleOptimiserVM()
        {
            Add = new RelayCommand((object o) =>
            {
                if (NewShiftAndComission.Any(x => x.Name == ShiftAndComissionName))
                    return;

                ComissionKeeper _item = new ComissionKeeper(ShiftAndComissionName, ShiftAndComission, (ComissionKeeper item) =>
                {
                    NewShiftAndComission.Remove(item);
                });

                NewShiftAndComission.Add(_item);
            });
        }
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly SimpleOptimiserM model = SimpleOptimiserM.Instance();

        public bool IsTickTest
        {
            get => model.IsTickTest;
            set => model.IsTickTest = value;
        }
        public bool ReplaceDates
        {
            get => model.ReplaceDates;
            set => model.ReplaceDates = value;
        }

        public bool IsDifferentShiftForTicks
        {
            get => model.IsDifferentShiftForTicks;
            set => model.IsDifferentShiftForTicks = value;
        }

        public string ShiftAndComissionName { get; set; }
        public double ShiftAndComission { get; set; } = 0;

        public ObservableCollection<ComissionKeeper> NewShiftAndComission => model.NewShiftAndComission;

        public ICommand Add { get; }
    }

    class ComissionKeeper
    {
        public ComissionKeeper(string Name, double Value, Action<ComissionKeeper> action)
        {
            this.Name = Name;
            this.Value = Value;
            Delete = new RelayCommand((object o) =>
            {
                action(this);
            });
        }

        public string Name { get; }
        public double Value { get; }
        public ICommand Delete { get; }
    }

    class Manager : IOptimiser
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="workingDirectory"></param>
        public Manager(WorkingDirectory workingDirectory)
        {
            this.workingDirectory = workingDirectory;
        }
        /// <summary>
        /// Wirking directory manager
        /// </summary>
        private readonly WorkingDirectory workingDirectory;

        #region Terminal manager
        /// <summary>
        /// Terminal keeper
        /// </summary>
        private TerminalManager _terminal = null;
        /// <summary>
        /// Terminal getter/setter
        /// </summary>
        public TerminalManager TerminalManager
        {
            get => _terminal;
            set
            {
                if (value == null)
                    return;
                if (IsOptimisationInProcess)
                    Stop();

                if (_terminal != null && _terminal.IsActive)
                {
                    _terminal.Close();
                    _terminal.WaitForStop();
                }
                _terminal = value;
            }
        }
        #endregion

        /// <summary>
        /// Optimisation process status
        /// </summary>
        public bool IsOptimisationInProcess { get; protected set; } = false;

        /// <summary>
        /// Optimiser name
        /// </summary>
        public string Name => SimpleOptimiserManagerCreator.Name;

        #region Report getters
        /// <summary>
        /// Raeport for all optimisation periods
        /// </summary>
        public List<OptimisationResult> AllOptimisationResults { get; } = new List<OptimisationResult>();
        /// <summary>
        /// Forward tests
        /// </summary>
        public List<OptimisationResult> ForwardOptimisations { get; } = new List<OptimisationResult>();
        /// <summary>
        /// History tests
        /// </summary>
        public List<OptimisationResult> HistoryOptimisations { get; } = new List<OptimisationResult>();
        /// <summary>
        /// Currency
        /// </summary>
        public string Currency { get; protected set; } = null;
        /// <summary>
        /// Balance
        /// </summary>
        public double Balance { get; protected set; }
        /// <summary>
        /// Laverage
        /// </summary>
        public int Laverage { get; protected set; }
        /// <summary>
        /// Path tor bot
        /// </summary>
        public string PathToBot { get; protected set; } = null;
        /// <summary>
        /// Working directory
        /// </summary>
        public string OptimiserWorkingDirectory { get; protected set; }
        #endregion
        /// <summary>
        /// Event that notifis for finishing optimisation process
        /// </summary>
        public event Action<IOptimiser> OptimisationProcessFinished;
        /// <summary>
        /// Event that notifis GUI for optimisation process progress
        /// </summary>
        public event Action<string, double> ProcessStatus;

        /// <summary>
        /// Clear optimisation manager
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

        readonly SimpleOptimiserM optimiserSettings = SimpleOptimiserM.Instance();
        System.Windows.Window settingsGUI = null;

        /// <summary>
        /// Open optimisation manager GUI dialog
        /// </summary>
        public virtual void LoadSettingsWindow()
        {
            if (settingsGUI != null)
                return;

            settingsGUI = new SimpleOptimiserSettings();
            settingsGUI.Closed += (object sender, EventArgs e) => { settingsGUI = null; };
            settingsGUI.Show();
        }

        /// <summary>
        /// Start optimisation process
        /// </summary>
        /// <param name="optimiserInputData">optimiser and bot settings</param>
        /// <param name="PathToResultsFile">path to file with report</param>
        public virtual void Start(OptimiserInputData optimiserInputData,
                                  string PathToResultsFile, string dirPrefix)
        {
            OptimiserWorkingDirectory = null;
            // check if history data and sorting flags availability and optimisation and terminal manager status
            if (IsOptimisationInProcess || TerminalManager.IsActive)
                return;

            // set progress ant togle
            ProcessStatus("Start optimisation", 0);
            IsOptimisationInProcess = true;

            // check path to result directory availability
            if (string.IsNullOrEmpty(PathToResultsFile) ||
               string.IsNullOrWhiteSpace(PathToResultsFile))
            {
                throw new ArgumentException("Path to results file is null or empty");
            }

            // check history dates availability
            if (optimiserInputData.HistoryBorders.Count == 0)
                throw new ArgumentException("There are no history optimisations date borders");

            // check sorting flags availability
            if (optimiserInputData.SortingFlags.Count() == 0)
                throw new ArgumentException("There are no sorting params");

            // Set working directory name
            OptimiserWorkingDirectory = workingDirectory.GetOptimisationDirectory(optimiserInputData.Symb,
                Path.GetFileNameWithoutExtension(optimiserInputData.RelativePathToBot), dirPrefix, Name).FullName;

            // Remove existing report file in Common directory
            if (File.Exists(PathToResultsFile))
                File.Delete(PathToResultsFile);

            // set optimiser settings
            PathToBot = optimiserInputData.RelativePathToBot;
            Currency = optimiserInputData.Currency;
            Balance = optimiserInputData.Balance;
            Laverage = optimiserInputData.Laverage;

            // Get (*.set) file path
            string setFile = new FileInfo(Path.Combine(TerminalManager.TerminalChangeableDirectory
                             .GetDirectory("MQL5")
                             .GetDirectory("Profiles")
                             .GetDirectory("Tester")
                             .FullName,
                             $"{Path.GetFileNameWithoutExtension(optimiserInputData.RelativePathToBot)}.set"))
                             .FullName;

            // step for progress bar
            double step = 100.0 / optimiserInputData.HistoryBorders.Count;
            // progress bar iterretion
            int i = 1;

            // creating (*set) file and save it with vew params
            #region Create (*.set) file
            SetFileManager setFileManager = new SetFileManager(setFile, true)
            {
                Params = optimiserInputData.BotParams
            };
            setFileManager.SaveParams();
            #endregion

            // Match setted and real history borders 
            Dictionary<DateBorders, DateBorders> borders = new Dictionary<DateBorders, DateBorders>();

            // foreach loop by date time borders
            foreach (var item in optimiserInputData.HistoryBorders)
            {
                // update progress
                ProcessStatus("Optimisation", step * i);
                i++;

                // configure terminal
                TerminalManager.Config = GetConfig(optimiserInputData, setFileManager, item);
                // run terminal with optimisation process and wait for closing
                if (TerminalManager.Run())
                {
                    // Wait for stop optimisation process
                    TerminalManager.WaitForStop();

                    // Read file with report and delete it after reading
                    List<OptimisationResult> results = new List<OptimisationResult>();
                    FillInData(results, PathToResultsFile);
                    // continue if can`t find any report information
                    if (results.Count == 0)
                        continue;

                    if (results.Select(x => x.report.DateBorders).Distinct().Count() > 1)
                        throw new Exception("There are more than one date borders inside report file");

                    // Add report data
                    if (!optimiserSettings.ReplaceDates)
                        AllOptimisationResults.AddRange(results);
                    else
                    {
                        results.ForEach(x =>
                        {
                            x.report.DateBorders = item;
                            AllOptimisationResults.Add(x);
                        });
                    }
                    // Fill in conparation dictionary
                    borders.Add(item, results[0].report.DateBorders);
                }
            }

            // Set status and start testing on ticks
            ProcessStatus("Start tests", 0);
            Tests(borders, optimiserInputData, setFile, PathToResultsFile);

            // Set togle and call event
            IsOptimisationInProcess = false;
            OptimisationProcessFinished(this);
        }
        /// <summary>
        /// Start forward and history tests
        /// </summary>
        /// <param name="HistoryToRealHistory">Maching given history data with real tests data</param>
        /// <param name="optimiserInputData">optimiser and bot settings</param>
        /// <param name="setFile">set file name</param>
        /// <param name="pathToFile">path to file with results</param>
        protected void Tests(Dictionary<DateBorders, DateBorders> HistoryToRealHistory,
                             OptimiserInputData optimiserInputData,
                             string setFile, string pathToFile)
        {
            // Math historydate border with forward
            Dictionary<DateBorders, DateBorders> HistoryToForward =
                DateBorders.CompareHistoryToForward(optimiserInputData.HistoryBorders, optimiserInputData.ForwardBorders);
            optimiserInputData.HistoryBorders.ForEach(x =>
            {
                if(!HistoryToRealHistory.ContainsKey(x))
                    HistoryToForward.Remove(x);
            });

            // internal method that start test
            bool Test(DateBorders Border, List<OptimisationResult> optimisationResults, List<OptimisationResult> results)
            {
                if (Border == null)
                    return false;

                // filter optimisations data
                if (optimiserInputData.CompareData != null &&
                    optimiserInputData.CompareData.Count > 0)
                {
                    optimisationResults = optimisationResults.FiltreOptimisations(optimiserInputData.CompareData).ToList();
                }
                // sort optimisations data or return if data are absent
                if (optimisationResults != null && optimisationResults.Count > 0)
                    optimisationResults = optimisationResults.SortOptimisations(OrderBy.Descending, optimiserInputData.SortingFlags).ToList();
                else
                    return false;

                // Get best result
                OptimisationResult result = optimisationResults.First();

                // Set bot params for the test
                for (int i = 0; i < optimiserInputData.BotParams.Count; i++)
                {
                    // Select bot param
                    var paramItem = optimiserInputData.BotParams[i];

                    // Set param value if this param contanes amoung bot params from report file item
                    if (result.report.BotParams.ContainsKey(paramItem.Variable))
                    {
                        var param = result.report.BotParams[paramItem.Variable];
                        paramItem.Value = param;

                        if (optimiserSettings.IsTickTest && optimiserSettings.IsDifferentShiftForTicks &&
                            optimiserSettings.NewShiftAndComission.Any(x => x.Name == paramItem.Variable))
                        {
                            paramItem.Value = optimiserSettings.NewShiftAndComission.First(x => x.Name == paramItem.Variable).Value.ToString();
                        }

                        optimiserInputData.BotParams[i] = paramItem;
                    }
                }

                // Save botparams before lanch terminal
                SetFileManager setFileManager = new SetFileManager(setFile, false)
                {
                    Params = optimiserInputData.BotParams
                };
                setFileManager.SaveParams();

                // Get and correct config file
                Config config = GetConfig(optimiserInputData, setFileManager, Border);
                if (optimiserSettings.IsTickTest)
                    config.Tester.Model = ENUM_Model.Every_tick_based_on_real_ticks;
                config.Tester.Optimization = ENUM_OptimisationMode.Disabled;

                // Configure terminal and run it
                TerminalManager.Config = config;
                if (TerminalManager.Run())
                    TerminalManager.WaitForStop();

                return true;
            }
            // internal method get optimisation surult for the selected date
            List<OptimisationResult> GetOptimisationResult(DateBorders settedHistoryBorder)
            {
                List<OptimisationResult> optimisationResults =
                    new List<OptimisationResult>
                    (
                     AllOptimisationResults
                    .Where(x =>
                    {
                        return x.report.DateBorders == (optimiserSettings.ReplaceDates ?
                                                        settedHistoryBorder :
                                                        HistoryToRealHistory[settedHistoryBorder]);
                    }));

                if (optimisationResults.Count == 0)
                    throw new Exception($"Can`t get optimisation results for the date diapossine {settedHistoryBorder}");

                return optimisationResults;
            }
            // internal method run test loop for the given results

            void RunTestLoop(List<OptimisationResult> results, bool isForward)
            {
                int n = 1;
                double step = 100.0 / HistoryToForward.Count;

                bool isSaveData = false;
                foreach (var item in HistoryToForward)
                {
                    ProcessStatus((isForward ? "Forward tests" : "History tests"), step * n);
                    n++;

                    List<OptimisationResult> optimisationResults = GetOptimisationResult(item.Key);
                    bool success = Test((isForward ? item.Value : item.Key), optimisationResults, results);
                    if (success && !isSaveData)
                        isSaveData = true;
                    if (success && optimiserSettings.ReplaceDates)
                    {
                        List<OptimisationResult> _results = new List<OptimisationResult>();
                        FillInData(_results, pathToFile);
                        if (_results.Count > 0)
                        {
                            var data = _results[0];
                            data.report.DateBorders = (isForward ? item.Value : item.Key);
                            results.Add(data);
                        }
                    }
                }

                if (isSaveData && !optimiserSettings.ReplaceDates)
                {
                    FillInData(results, pathToFile);
                }
            }

            // Run test loop forhistory ans forward test pass
            RunTestLoop(HistoryOptimisations, false);
            RunTestLoop(ForwardOptimisations, true);
        }
        /// <summary>
        /// Generate config file foroptimisation
        /// </summary>
        /// <param name="optimiserInputData">Optimiser and bot settings</param>
        /// <param name="setFileManager">set file manager</param>
        /// <param name="dateBorders">selected date borders</param>
        /// <returns></returns>
        protected Config GetConfig(OptimiserInputData optimiserInputData, SetFileManager setFileManager, DateBorders dateBorders)
        {
            Config config = new Config(TerminalManager.TerminalChangeableDirectory
                                                              .GetDirectory("config")
                                                              .GetFiles("common.ini")
                                                              .First().FullName);
            config = config.DublicateFile(Path.Combine(workingDirectory.WDRoot.FullName, $"{TerminalManager.TerminalID}.ini"));

            config.Tester.Currency = optimiserInputData.Currency;
            config.Tester.Deposit = optimiserInputData.Balance;
            config.Tester.ExecutionMode = optimiserInputData.ExecutionDelay;
            config.Tester.Expert = optimiserInputData.RelativePathToBot;
            config.Tester.ExpertParameters = setFileManager.FileInfo.Name;
            config.DeleteKey(ENUM_SectionType.Tester, "ForwardDate");
            config.Tester.ForwardMode = ENUM_ForvardMode.Disabled;
            config.Tester.FromDate = dateBorders.From;
            config.Tester.Leverage = $"1:{optimiserInputData.Laverage}";
            config.Tester.Model = optimiserInputData.Model;
            config.Tester.Optimization = optimiserInputData.OptimisationMode;
            config.Tester.OptimizationCriterion = ENUM_OptimisationCriteria.Balance__Profit_factor;
            config.Tester.Period = optimiserInputData.TF;
            config.DeleteKey(ENUM_SectionType.Tester, "Report");
            config.Tester.ShutdownTerminal = true;
            config.Tester.Symbol = optimiserInputData.Symb;
            config.Tester.ToDate = dateBorders.Till;

            return config;
        }
        /// <summary>
        /// Read optimisation report and save in into the given list
        /// </summary>
        /// <param name="data">Optimisation data keeper</param>
        /// <param name="pathToReportFile">path to optimisation report file</param>
        protected void FillInData(List<OptimisationResult> data, string pathToReportFile)
        {
            if (!File.Exists(pathToReportFile))
                return;

            using (ReportReader reader = new ReportReader(pathToReportFile))
            {
                if (reader.Currency != Currency)
                    throw new Exception("Currency is different");
                if (reader.Balance != Balance)
                    throw new Exception("Balance is different");
                if (reader.Laverage != Laverage)
                    throw new Exception("Laverage is different");
                if (reader.RelativePathToBot != PathToBot)
                    throw new Exception("Path to bot is different");

                while (reader.Read())
                {
                    if (reader.ReportItem.HasValue)
                        data.Add(reader.ReportItem.Value);
                }
            }

            File.Delete(pathToReportFile);
        }
        /// <summary>
        /// Stop current process and remove all progress
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
    }
}
