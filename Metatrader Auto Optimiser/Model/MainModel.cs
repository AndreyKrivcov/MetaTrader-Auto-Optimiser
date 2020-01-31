using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Metatrader_Auto_Optimiser.Model.DirectoryManagers;
using Metatrader_Auto_Optimiser.Model.FileReaders;
using Metatrader_Auto_Optimiser.Model.OptimisationManagers;
using Metatrader_Auto_Optimiser.Model.Terminal;
using ReportManager;

namespace Metatrader_Auto_Optimiser.Model
{
    /// <summary>
    /// Model creator
    /// </summary>
    class MainModelCreator
    {
        /// <summary>
        /// Static constructor
        /// </summary>
        public static IMainModel Model => new MainModel();
    }

    /// <summary>
    /// Main window model
    /// </summary>
    class MainModel : IMainModel
    {
        public MainModel()
        {
            optimiserCreators = new List<OptimiserCreator>
                                {
                                    new OptimisationManagers.SimpleForvard.SimpleOptimiserManagerCreator(workingDirectory)
                                };
            Optimiser = optimiserCreators[0].Create(new TerminalManager(terminalDirectory.Terminals.ElementAt(0)));
            Optimiser.ProcessStatus += Optimiser_ProcessStatus;
            Optimiser.OptimisationProcessFinished += Optimiser_OptimisationProcessFinished;
        }

        /// <summary>
        /// Destructor
        /// </summary>
        ~MainModel()
        {
            if (Optimiser.IsOptimisationInProcess)
                StopOptimisation();

            Optimiser.ProcessStatus -= Optimiser_ProcessStatus;
            Optimiser.OptimisationProcessFinished -= Optimiser_OptimisationProcessFinished;
        }

        private void Optimiser_ProcessStatus(string arg1, double arg2)
        {
            PBUpdate(arg1, arg2);
        }
        /// <summary>
        /// Optimiser callback
        /// </summary>
        /// <param name="obj">Optimiser</param>
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
            LoadSavedOptimisation(optimiser.OptimiserWorkingDirectory);
            OptimisationStoped();
        }
        private void SaveOptimisations(IOptimiser optimiser)
        {
            if (string.IsNullOrEmpty(optimiser.OptimiserWorkingDirectory) ||
                string.IsNullOrWhiteSpace(optimiser.OptimiserWorkingDirectory))
            {
                return;
            }

            List<OptimisationResult> AllOptimisationResults = new List<OptimisationResult>();
            List<OptimisationResult> ForwardOptimisations = new List<OptimisationResult>();
            List<OptimisationResult> HistoryOptimisations = new List<OptimisationResult>();

            List<FileInfo> files = Directory.GetFiles(optimiser.OptimiserWorkingDirectory, "*.xml").Select(x => new FileInfo(x)).ToList();

            bool FillIn(List<OptimisationResult> results, IEnumerable<DateBorders> currentBorders, string fileName)
            {
                results.AddRange(GetItems(files.Find(x => x.Name == fileName),
                    out string expert, out double deposit, out string currency, out int laverage));

                if (expert != optimiser.PathToBot || deposit != optimiser.Balance ||
                   currency != optimiser.Currency || laverage != optimiser.Laverage)
                {
                    System.Windows.MessageBox.Show("Can`t append data into files with different optimiser settings (path to bot / balance / currency / laverage)");
                    return false;
                }

                foreach (var item in currentBorders)
                {
                    results.RemoveAll(x => x.report.DateBorders == item);
                }

                return true;
            }

            double step = 100.0 / 4;

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
            files.ForEach(x => x.Delete());

            AllOptimisationResults.AddRange(optimiser.AllOptimisationResults);
            HistoryOptimisations.AddRange(optimiser.HistoryOptimisations);
            ForwardOptimisations.AddRange(optimiser.ForwardOptimisations);

            void WriteFile(List<OptimisationResult> results, string fileName)
            {
                results.ReportWriter(optimiser.PathToBot, optimiser.Currency, optimiser.Balance, optimiser.Laverage,
                                                    Path.Combine(optimiser.OptimiserWorkingDirectory, fileName));
            }

            PBUpdate("Save All optimisations", step * 2);

            if (AllOptimisationResults.Count > 0)
                WriteFile(AllOptimisationResults, "Report.xml");
            else
            {
                System.Windows.MessageBox.Show("There are no optimisation data to save");
                return;
            }

            var emptyItem = new OptimisationResult
            {
                report = new ReportItem
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

            if (HistoryOptimisations.Count == 0)
                HistoryOptimisations.Add(emptyItem);
            if (ForwardOptimisations.Count == 0)
                ForwardOptimisations.Add(emptyItem);

            PBUpdate("Save History tests", step * 3);
            WriteFile(HistoryOptimisations, "History.xml");

            PBUpdate("Save Forward tests", step * 4);
            WriteFile(ForwardOptimisations, "Forward.xml");

        }

        #region Directory managers
        /// <summary>
        /// Terminal directory manager
        /// </summary>
        private readonly TerminalDirectory terminalDirectory = new TerminalDirectory();
        /// <summary>
        /// Current working directory manager
        /// </summary>
        private readonly WorkingDirectory workingDirectory = new WorkingDirectory();
        #endregion

        /// <summary>
        /// List of optimiser static fabrics
        /// </summary>
        private readonly List<OptimiserCreator> optimiserCreators;

        #region Getters
        /// <summary>
        /// Selected optimiser
        /// </summary>
        public IOptimiser Optimiser { get; private set; }
        /// <summary>
        /// Terminals list
        /// </summary>
        public IEnumerable<string> TerminalNames => terminalDirectory.Terminals.Select(x => x.Name);
        /// <summary>
        /// Optimisers list
        /// </summary>
        public IEnumerable<string> OptimisatorNames => optimiserCreators.Select(x => x.Name);
        /// <summary>
        /// List of optimisations directories
        /// </summary>
        public IEnumerable<string> SavedOptimisations => workingDirectory.Reports.GetDirectories().Select(x => x.Name);
        /// <summary>
        /// all optimisations results
        /// </summary>
        public ReportData AllOptimisationResults { get; private set; } = new ReportData
        {
            AllOptimisationResults = new Dictionary<DateBorders, List<OptimisationResult>>()
        };
        /// <summary>
        /// Forward optimisations
        /// </summary>
        public List<OptimisationResult> ForwardOptimisations { get; private set; } = new List<OptimisationResult>();
        /// <summary>
        /// History optimisations
        /// </summary>
        public List<OptimisationResult> HistoryOptimisations { get; private set; } = new List<OptimisationResult>();

        #endregion

        #region Events
        /// <summary>
        /// Exception events
        /// </summary>
        public event Action<string> ThrowException;
        /// <summary>
        /// End of optimisation events
        /// </summary>
        public event Action OptimisationStoped;
        /// <summary>
        /// Ptogress bar events
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
        /// Change optimiser manager
        /// </summary>
        /// <param name="optimiserName">optimiser manager name</param>
        /// <returns>true if sucsess</returns>
        public bool ChangeOptimiser(string optimiserName)
        {
            if (Optimiser.IsOptimisationInProcess || Optimiser.TerminalManager.IsActive)
                return false;
            Optimiser.ProcessStatus -= Optimiser_ProcessStatus;
            Optimiser.OptimisationProcessFinished -= Optimiser_OptimisationProcessFinished;

            Optimiser = optimiserCreators.First(x => x.Name == optimiserName).Create(Optimiser.TerminalManager);
            Optimiser.ProcessStatus += Optimiser_ProcessStatus;
            Optimiser.OptimisationProcessFinished += Optimiser_OptimisationProcessFinished;
            return true;
        }
        /// <summary>
        /// Change terminal manager
        /// </summary>
        /// <param name="terminalName">Terminal manager name</param>
        /// <returns>true if sucseed</returns>
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
        /// Get parametres for selected robot
        /// </summary>
        /// <param name="botName">Robot name</param>
        /// <param name="terminalName">Terminal name</param>
        /// <returns>Bot params</returns>
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
        /// Load optimisations from file
        /// </summary>
        /// <param name="optimisationName"></param>
        public async void LoadSavedOptimisation(string optimisationName)
        {
            double step = 100.0 / 4.0;
            DirectoryInfo selectedDir = workingDirectory.Reports.GetDirectory(optimisationName);
            #region Check
            if (selectedDir == null)
            {
                ThrowException("Can`t get directory");
                return;
            }
            #endregion

            AllOptimisationResults = new ReportData
            {
                AllOptimisationResults = new Dictionary<DateBorders, List<OptimisationResult>>()
            };
            HistoryOptimisations.Clear();
            ForwardOptimisations.Clear();

            await Task.Run(() =>
            {
                try
                {
                    PBUpdate("Getting files", step);
                    FileInfo[] files = selectedDir.GetFiles("*.xml");

                    if (files.Any(x => x.Name == "Report.xml") &&
                       files.Any(x => x.Name == "History.xml") &&
                       files.Any(x => x.Name == "Forward.xml"))
                    {
                        ReportData reportData = new ReportData
                        {
                            AllOptimisationResults = new Dictionary<DateBorders, List<OptimisationResult>>()
                        };

                        PBUpdate("Results.xml", step * 2);
                        List<OptimisationResult> report = GetItems(files.First(x => x.Name == "Report.xml"),
                                                                   out string expert, out double deposit,
                                                                   out string currency, out int laverage);
                        reportData.Expert = expert;
                        reportData.Deposit = deposit;
                        reportData.Currency = currency;
                        reportData.Laverage = laverage;

                        #region Check
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

                        if (report.Count == 0)
                            throw new Exception("File 'Report.xml' is empty or can`t be read");
                        #endregion

                        List<DateBorders> dates = report.Select(x => x.report.DateBorders).Distinct().ToList();
                        foreach (var item in dates)
                        {
                            reportData.AllOptimisationResults.Add(item,
                                new List<OptimisationResult>(report.Where(x => x.report.DateBorders == item)));
                        }

                        AllOptimisationResults = reportData;

                        PBUpdate("History.xml", step * 3);
                        HistoryOptimisations = GetItems(files.First(x => x.Name == "History.xml"),
                                                        out expert, out deposit,
                                                        out currency, out laverage).OrderBy(x => x.report.DateBorders).ToList();
                        #region Check
                        CompareTestersettings();
                        #endregion

                        PBUpdate("Forward.xml", step * 4);
                        ForwardOptimisations = GetItems(files.First(x => x.Name == "Forward.xml"),
                                                        out expert, out deposit,
                                                        out currency, out laverage).OrderBy(x => x.report.DateBorders).ToList();
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
                    HistoryOptimisations.Clear();
                    ForwardOptimisations.Clear();
                    AllOptimisationResults = new ReportData
                    {
                        AllOptimisationResults = new Dictionary<DateBorders, List<OptimisationResult>>()
                    };

                    ThrowException(e.Message);
                }
            });

            PBUpdate(null, 0);

            OnPropertyChanged("AllOptimisationResults");
            OnPropertyChanged("ForwardOptimisations");
            OnPropertyChanged("HistoryOptimisations");
        }
        /// <summary>
        /// Read file
        /// </summary>
        /// <param name="file"></param>
        /// <param name="expert"></param>
        /// <param name="deposit"></param>
        /// <param name="currensy"></param>
        /// <param name="laverage"></param>
        /// <returns></returns>
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
        /// Save reports to file
        /// </summary>
        /// <param name="dateBorders"></param>
        /// <param name="pathToSavingFile"></param>
        public void SaveToCSVOptimisations(DateBorders dateBorders, string pathToSavingFile)
        {
            CreateCsv(dateBorders, pathToSavingFile);
        }
        /// <summary>
        /// Save history tests to file
        /// </summary>
        /// <param name="pathToSavingFile"></param>
        public void SaveToCSVSelectedOptimisations(string pathToSavingFile)
        {
            CreateCsv(null, pathToSavingFile);
        }
        /// <summary>
        /// Startoptimisation
        /// </summary>
        /// <param name="optimiserInputData">optimiser inpit data</param>
        /// <param name="isAppend">flag is appent to the file</param>
        /// <param name="dirPrefix">directory prifix</param>
        public async void StartOptimisation(OptimiserInputData optimiserInputData, bool isAppend, string dirPrefix)
        {
            if (string.IsNullOrEmpty(optimiserInputData.Symb) ||
                string.IsNullOrWhiteSpace(optimiserInputData.Symb) ||
                (optimiserInputData.HistoryBorders.Count == 0 && optimiserInputData.ForwardBorders.Count == 0))
            {
                ThrowException("Fill in asset name and date borders");
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

            await Task.Run(() =>
            {
                try
                {
                    DirectoryInfo cachDir = Optimiser.TerminalManager.TerminalChangeableDirectory
                                                             .GetDirectory("Tester")
                                                             .GetDirectory("cache", true);
                    DirectoryInfo cacheCopy = workingDirectory.Tester.GetDirectory("cache", true);
                    cacheCopy.GetFiles().ToList().ForEach(x => x.Delete());
                    cachDir.GetFiles().ToList()
                           .ForEach(x => x.MoveTo(Path.Combine(cacheCopy.FullName, x.Name)));

                    Optimiser.ClearOptimiser();
                    Optimiser.Start(optimiserInputData,
                        Path.Combine(terminalDirectory.Common.FullName,
                        $"{Path.GetFileNameWithoutExtension(optimiserInputData.RelativePathToBot)}_Report.xml"), dirPrefix);
                }
                catch (Exception e)
                {
                    Optimiser.Stop();
                    ThrowException(e.Message);
                }
            });
        }
        /// <summary>
        /// Start test
        /// </summary>
        /// <param name="optimiserInputData">input data for tester/optimiser</param>
        public async void StartTest(OptimiserInputData optimiserInputData)
        {
            if (Optimiser.TerminalManager.IsActive)
            {
                ThrowException("Terminal already running");
                return;
            }

            #region From/Forward/To
            DateTime Forward = new DateTime();
            DateTime ToDate = Forward;
            DateTime FromDate = Forward;

            if (optimiserInputData.HistoryBorders.Count > 1 ||
                optimiserInputData.ForwardBorders.Count > 1)
            {
                ThrowException("For test there mast be from 1 to 2 date borders");
                OnPropertyChanged("ResumeEnablingTogle");
                return;
            }

            if (optimiserInputData.HistoryBorders.Count == 1 &&
                optimiserInputData.ForwardBorders.Count == 1)
            {
                DateBorders _Forward = optimiserInputData.ForwardBorders[0];
                DateBorders _History = optimiserInputData.HistoryBorders[0];

                if (_History > _Forward)
                {
                    ThrowException("History optimisation mast be less than Forward");
                    OnPropertyChanged("ResumeEnablingTogle");
                    return;
                }

                Forward = _Forward.From;
                FromDate = _History.From;
                ToDate = (_History.Till < _Forward.Till ? _Forward.Till : _History.Till);
            }
            else
            {
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

            await Task.Run(() =>
            {
                try
                {
                    #region Create (*.set) file
                    FileInfo file = new FileInfo(Path.Combine(Optimiser
                                                    .TerminalManager
                                                    .TerminalChangeableDirectory
                                                    .GetDirectory("MQL5")
                                                    .GetDirectory("Profiles")
                                                    .GetDirectory("Tester")
                                                    .FullName, $"{Path.GetFileNameWithoutExtension(optimiserInputData.RelativePathToBot)}.set"));

                    List<ParamsItem> botParams = new List<ParamsItem>(GetBotParams(optimiserInputData.RelativePathToBot, false));

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

                    SetFileManager setFile = new SetFileManager(file.FullName, false)
                    {
                        Params = botParams
                    };
                    setFile.SaveParams();
                    #endregion

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

                    Optimiser.TerminalManager.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
                    Optimiser.TerminalManager.Config = config;
                    Optimiser.TerminalManager.Run();
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
        /// Stop optimisation
        /// </summary>
        public void StopOptimisation()
        {
            Optimiser.Stop();
        }
        /// <summary>
        /// Sort loaded and selected report
        /// </summary>
        /// <param name="borders">selected date borders</param>
        /// <param name="sortingFlags">selected sorting flags</param>
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
        /// Filter optimisation results
        /// </summary>
        /// <param name="borders">selected date borders</param>
        /// <param name="compareData">compare flags</param>
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
        /// Create (*csv) file
        /// </summary>
        /// <param name="borders"></param>
        /// <param name="pathToFile"></param>
        private async void CreateCsv(DateBorders borders, string pathToFile)
        {
            List<OptimisationResult> results;
            if (borders != null)
                results = new List<OptimisationResult>(AllOptimisationResults.AllOptimisationResults[borders]);
            else
            {
                results = new List<OptimisationResult>(HistoryOptimisations);
                results.AddRange(ForwardOptimisations);
            }

            if (results.Count > 0)
            {
                try
                {
                    using (StreamWriter writer = new StreamWriter(pathToFile))
                    {
                        string headders = "From;Till;Payoff;Profit factor;Recovery factor;Total trades;PL;DD;Altman Z Score;" +
                                          "VaR 90;VaR 95;VaR 99;Mx;Std;Total profit;Total loose;Total profit trades;" +
                                          "Total loose trades;Consecutive wins;Consecutive loose;" +
                                          "Mn Profit;Mn loose;Mn profit trades;Mn loose trades;" +
                                          "Tu Profit;Tu loose;Tu profit trades;Tu loose trades;" +
                                          "We Profit;We loose;We profit trades;We loose trades;" +
                                          "Th Profit;Th loose;Th profit trades;Th loose trades;" +
                                          "Fr Profit;Fr loose;Fr profit trades;Fr loose trades;";

                        await Task.Run(() =>
                        {
                            bool isFirst = true;

                            foreach (var item in results)
                            {
                                if (isFirst)
                                {
                                    isFirst = false;
                                    foreach (var param in item.report.BotParams)
                                    {
                                        headders += $"{param.Key};";
                                    }
                                    writer.WriteLine(headders);
                                }

                                string line = $"{item.report.DateBorders.From.ToString("dd.MM.yyyy HH:mm:ss")};" +
                                              $"{item.report.DateBorders.Till.ToString("dd.MM.yyyy HH:mm:ss")};" +
                                              $"{item.report.OptimisationCoefficients.Payoff};" +
                                              $"{item.report.OptimisationCoefficients.ProfitFactor};" +
                                              $"{item.report.OptimisationCoefficients.RecoveryFactor};" +
                                              $"{item.report.OptimisationCoefficients.TotalTrades};" +
                                              $"{item.report.OptimisationCoefficients.PL};" +
                                              $"{item.report.OptimisationCoefficients.DD};" +
                                              $"{item.report.OptimisationCoefficients.AltmanZScore};" +
                                              $"{item.report.OptimisationCoefficients.VaR.Q_90};" +
                                              $"{item.report.OptimisationCoefficients.VaR.Q_95};" +
                                              $"{item.report.OptimisationCoefficients.VaR.Q_99};" +
                                              $"{item.report.OptimisationCoefficients.VaR.Mx};" +
                                              $"{item.report.OptimisationCoefficients.VaR.Std};" +
                                              $"{item.report.OptimisationCoefficients.MaxPLDD.Profit.Value};" +
                                              $"{item.report.OptimisationCoefficients.MaxPLDD.DD.Value};" +
                                              $"{item.report.OptimisationCoefficients.MaxPLDD.Profit.TotalTrades};" +
                                              $"{item.report.OptimisationCoefficients.MaxPLDD.DD.TotalTrades};" +
                                              $"{item.report.OptimisationCoefficients.MaxPLDD.Profit.ConsecutivesTrades};" +
                                              $"{item.report.OptimisationCoefficients.MaxPLDD.DD.ConsecutivesTrades};" +

                                              $"{item.report.OptimisationCoefficients.TradingDays[DayOfWeek.Monday].Profit.Value};" +
                                              $"{item.report.OptimisationCoefficients.TradingDays[DayOfWeek.Monday].DD.Value};" +
                                              $"{item.report.OptimisationCoefficients.TradingDays[DayOfWeek.Monday].Profit.Trades};" +
                                              $"{item.report.OptimisationCoefficients.TradingDays[DayOfWeek.Monday].DD.Trades};" +

                                              $"{item.report.OptimisationCoefficients.TradingDays[DayOfWeek.Tuesday].Profit.Value};" +
                                              $"{item.report.OptimisationCoefficients.TradingDays[DayOfWeek.Thursday].DD.Value};" +
                                              $"{item.report.OptimisationCoefficients.TradingDays[DayOfWeek.Thursday].Profit.Trades};" +
                                              $"{item.report.OptimisationCoefficients.TradingDays[DayOfWeek.Thursday].DD.Trades};" +

                                              $"{item.report.OptimisationCoefficients.TradingDays[DayOfWeek.Wednesday].Profit.Value};" +
                                              $"{item.report.OptimisationCoefficients.TradingDays[DayOfWeek.Wednesday].DD.Value};" +
                                              $"{item.report.OptimisationCoefficients.TradingDays[DayOfWeek.Wednesday].Profit.Trades};" +
                                              $"{item.report.OptimisationCoefficients.TradingDays[DayOfWeek.Wednesday].DD.Trades};" +

                                              $"{item.report.OptimisationCoefficients.TradingDays[DayOfWeek.Thursday].Profit.Value};" +
                                              $"{item.report.OptimisationCoefficients.TradingDays[DayOfWeek.Thursday].DD.Value};" +
                                              $"{item.report.OptimisationCoefficients.TradingDays[DayOfWeek.Thursday].Profit.Trades};" +
                                              $"{item.report.OptimisationCoefficients.TradingDays[DayOfWeek.Thursday].DD.Trades};" +

                                              $"{item.report.OptimisationCoefficients.TradingDays[DayOfWeek.Friday].Profit.Value};" +
                                              $"{item.report.OptimisationCoefficients.TradingDays[DayOfWeek.Friday].DD.Value};" +
                                              $"{item.report.OptimisationCoefficients.TradingDays[DayOfWeek.Friday].Profit.Trades};" +
                                              $"{item.report.OptimisationCoefficients.TradingDays[DayOfWeek.Friday].DD.Trades};";

                                foreach (var param in item.report.BotParams)
                                {
                                    line += $"{param.Value};";
                                }

                                writer.WriteLine(line);
                            }
                        });
                    }
                }
                catch (Exception e)
                {
                    ThrowException(e.Message);
                }
            }

            OnPropertyChanged("CSV");
        }
        #endregion
    }


}
