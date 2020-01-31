using Metatrader_Auto_Optimiser.Model;
using Metatrader_Auto_Optimiser.Model.FileReaders;
using Metatrader_Auto_Optimiser.Model.Terminal;
using ReportManager;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace Metatrader_Auto_Optimiser.View_Model
{
    /// <summary>
    /// View model
    /// </summary>
    class AutoOptimiserVM : INotifyPropertyChanged
    {
        /// <summary>
        /// VM constructor
        /// </summary>
        public AutoOptimiserVM()
        {
            OptimiserSettings = new List<OptimiserSetting>
                                {
                                    new OptimiserSetting("Available experts", model.Optimiser.TerminalManager.Experts, (string botName)=>
                                    { SetBotParams(botName,false); }),
                                    new OptimiserSetting("Execution Mode", GetEnumNames<ENUM_ExecutionDelay>()),
                                    new OptimiserSetting("Deposit", new []{ "3000", "5000", "10000", "25000", "50000", "100000" }),
                                    new OptimiserSetting("Currency", new []{ "RUR", "USD", "EUR", "GBP", "CHF"}),
                                    new OptimiserSetting("Optimisation mode", GetEnumNames<ENUM_OptimisationMode>()),
                                    new OptimiserSetting("Laverage", new []{"1", "5", "50", "100", "500"}),
                                    new OptimiserSetting("TF", GetEnumNames<ENUM_Timeframes>()),
                                    new OptimiserSetting("Optimisation model", GetEnumNames<ENUM_Model>())
                                };
            OptimiserSettingsForResults_fixed = new ObservableCollection<KeyValuePair<string, string>>
                                {
                                    new KeyValuePair<string, string>("Symbol", null ),
                                    new KeyValuePair<string, string>("Expert", null ),
                                    new KeyValuePair<string, string>("Deposit", null ),
                                    new KeyValuePair<string, string>("Currency", null ),
                                    new KeyValuePair<string, string>("Optimisation mode", ENUM_OptimisationMode.Disabled.ToString().Replace("_"," ") ),
                                    new KeyValuePair<string, string>("Laverage", null ),
                                    new KeyValuePair<string, string>("TF", null )
                                };
            OptimiserSettingsForResults_changing = new List<OptimiserSetting>
                                {
                                    new OptimiserSetting("Execution Mode", GetEnumNames<ENUM_ExecutionDelay>()),
                                    new OptimiserSetting("Optimisation model", GetEnumNames<ENUM_Model>())
                                };
            // subscribe to events from model
            model.OptimisationStoped += Model_OptimisationStoped;
            model.PBUpdate += Model_PBUpdate;
            model.ThrowException += Model_ThrowException;
            model.PropertyChanged += Model_PropertyChanged;

            // Fill in bot params forthe first bot
            var settings = OptimiserSettings.Find(x => x.Name == "Available experts");
            SetBotParams(settings.SelectedParam, false);

            #region Fill in commands
            AddSorter = new RelayCommand((object o) => _AddSorter(false));
            AddSorter_Results = new RelayCommand((object o) => _AddSorter(true));

            AddFilter = new RelayCommand((object o) => _AddFilter(false));
            AddFilter_Result = new RelayCommand((object o) => _AddFilter(true));

            StartStopOptimisation = new RelayCommand(_StartStopOptimisation);
            DateBorderTypes = GetEnumNames<OptimisationType>();

            AddDateBorder = new RelayCommand(_AddDateBorder);

            LoadResults = new RelayCommand((object o) => model.LoadSavedOptimisation(SelectedOptimisation));

            ShowOptimiserGUI = new RelayCommand((object o) =>
            {
                try { model.Optimiser.LoadSettingsWindow(); }
                catch (Exception e) { System.Windows.MessageBox.Show(e.Message); }
            });

            SortResults = new RelayCommand(_SortResults);
            FilterResults = new RelayCommand(_FilterResults);
            StartTestReport = new RelayCommand((object o) =>
            {
                _StartTest(model.AllOptimisationResults.AllOptimisationResults[ReportDateBorders[SelectedReportDateBorder]], SelecterReportItem);
            });
            StartTestHistory = new RelayCommand((object o) =>
            {
                _StartTest(model.HistoryOptimisations, SelectedHistoryItem);
            });
            StartTestForward = new RelayCommand((object o) =>
            {
                _StartTest(model.ForwardOptimisations, SelectedForwardItem);
            });
            SaveToCsv = new RelayCommand(_SaveToCsv);

            UpdateSetFile = new RelayCommand((object o) =>
            {
                System.Threading.Tasks.Task.Run(() =>
                {
                    SetBotParams(OptimiserSettings.First(x => x.Name == "Available experts").SelectedParam, true);
                });
            });
            SaveOrLoadDates = new RelayCommand(_SaveOrLoadDates);
            #endregion
        }
        /// <summary>
        /// Destructor
        /// </summary>
        ~AutoOptimiserVM()
        {
            // Unsubscribe bot events
            model.OptimisationStoped -= Model_OptimisationStoped;
            model.PBUpdate -= Model_PBUpdate;
            model.ThrowException -= Model_ThrowException;
            model.PropertyChanged -= Model_PropertyChanged;
        }

        #region Events and callbacks
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

        private void Model_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "StopTest" ||
                e.PropertyName == "ResumeEnablingTogle")
            {
                EnableMainTogles = true;
                Status = "";
                Progress = 0;
                dispatcher.Invoke(() =>
                {
                    OnPropertyChanged("EnableMainTogles");
                    OnPropertyChanged("Status");
                    OnPropertyChanged("Progress");
                });
            }

            if (e.PropertyName == "AllOptimisationResults")
            {
                dispatcher.Invoke(() =>
                {
                    ReportDateBorders.Clear();
                    foreach (var item in model.AllOptimisationResults.AllOptimisationResults.Keys)
                    {
                        ReportDateBorders.Add(item);
                    }

                    SelectedReportDateBorder = 0;

                    ReplaceBotFixedParam("Expert", model.AllOptimisationResults.Expert);
                    ReplaceBotFixedParam("Deposit", model.AllOptimisationResults.Deposit.ToString());
                    ReplaceBotFixedParam("Currency", model.AllOptimisationResults.Currency);
                    ReplaceBotFixedParam("Laverage", model.AllOptimisationResults.Laverage.ToString());
                    OnPropertyChanged("OptimiserSettingsForResults_fixed");
                });

                System.Windows.MessageBox.Show("Report params where updated");
            }

            if (e.PropertyName == "SortedResults" ||
                e.PropertyName == "FilteredResults")
            {
                dispatcher.Invoke(() =>
                {
                    SelectedReportDateBorder = SelectedReportDateBorder;
                });
            }

            if (e.PropertyName == "ForwardOptimisations")
            {
                dispatcher.Invoke(() =>
                {
                    ForwardOptimisations.Clear();
                    foreach (var item in model.ForwardOptimisations)
                    {
                        ForwardOptimisations.Add(new ReportItem(item));
                    }
                });
            }

            if (e.PropertyName == "HistoryOptimisations")
            {
                dispatcher.Invoke(() =>
                {
                    HistoryOptimisations.Clear();
                    foreach (var item in model.HistoryOptimisations)
                    {
                        HistoryOptimisations.Add(new ReportItem(item));
                    }
                });
            }

            if (e.PropertyName == "CSV")
            {
                System.Windows.MessageBox.Show("(*.csv) File saved");
            }
        }
        /// <summary>
        /// Update status and progress bar from model callback
        /// </summary>
        /// <param name="status">new status</param>
        /// <param name="value">new value</param>
        private void Model_PBUpdate(string status, double value)
        {
            Status = status;
            Progress = value;

            dispatcher.Invoke(() =>
            {
                OnPropertyChanged("Status");
                OnPropertyChanged("Progress");
            });
        }
        /// <summary>
        /// Display exceptions from model
        /// </summary>
        /// <param name="e">exception</param>
        private void Model_ThrowException(string e)
        {
            System.Windows.MessageBox.Show(e);
        }
        /// <summary>
        /// Callback that calls after finishing optimisation process
        /// </summary>
        private void Model_OptimisationStoped()
        {
            EnableMainTogles = true;
            Status = null;
            Progress = 0;
            dispatcher.Invoke(() =>
            {
                OnPropertyChanged("SelectedOptimisationNames");
                OnPropertyChanged("EnableMainTogles");
                OnPropertyChanged("Status");
                OnPropertyChanged("Progress");
            });

            System.Windows.MessageBox.Show("Optimisation finished");
        }

        #endregion

        /// <summary>
        /// Model keeper
        /// </summary>
        private readonly IMainModel model = MainModelCreator.Model;
        /// <summary>
        /// Main window dispatcher
        /// </summary>
        private readonly System.Windows.Threading.Dispatcher dispatcher =
            System.Windows.Application.Current.Dispatcher;

        #region Status and progress
        /// <summary>
        /// Progress bar status
        /// </summary>
        public string Status { get; set; }
        /// <summary>
        /// Progress bar progress scale
        /// </summary>
        public double Progress { get; set; } = 0;
        #endregion

        /// <summary>
        /// if this togle false - most important fields are disabling 
        /// </summary>
        public bool EnableMainTogles { get; private set; } = true;

        #region Bot params
        /// <summary>
        /// Callback that fill in bot params and display it
        /// </summary>
        /// <param name="botName"></param>
        private void SetBotParams(string botName, bool isUpdateSetFile)
        {
            dispatcher.Invoke(() =>
            {
                BotParams.Clear();
                OnPropertyChanged("BotParams");
                Status = "Update bot params";
                OnPropertyChanged("Status");
                Progress = 100;
                OnPropertyChanged("Progress");
            });

            IEnumerable<ParamsItem> items = model.GetBotParams(botName, isUpdateSetFile);

            dispatcher.Invoke(() =>
            {
                if (items != null)
                {
                    foreach (var item in items)
                    {
                        BotParams.Add(new BotParamsData(item));
                    }
                }

                OnPropertyChanged("BotParams");
                Status = null;
                OnPropertyChanged("Status");
                Progress = 0;
                OnPropertyChanged("Progress");
            });
        }
        /// <summary>
        /// Data with pobot params
        /// </summary>
        public List<OptimiserSetting> OptimiserSettings { get; }
        public ICommand UpdateSetFile { get; }
        /// <summary>
        /// Testing asset name
        /// </summary>
        public string AssetName { get; set; }
        #endregion

        #region Sorter ans Filter in Settings
        /// <summary>
        /// Sorterting methods getter
        /// </summary>
        public IEnumerable<string> SortBy => GetEnumNames<SortBy>();

        #region Sorter
        /// <summary>
        /// Selected sorter for Settings tab
        /// </summary>
        public int SelectedSorter { get; set; } = 0;
        /// <summary>
        /// Selected sorter for Results tab
        /// </summary>
        public int SelectedSorter_Results { get; set; } = 0;
        /// <summary>
        /// Selected sorting items
        /// </summary>
        public ObservableCollection<SorterItem> SorterItems { get; } = new ObservableCollection<SorterItem>();
        /// <summary>
        /// Callback for adding sorter to the SorterItems collecton
        /// </summary>
        /// <param name="isResults">tab type togle</param>
        private void _AddSorter(bool isResults)
        {
            SortBy value = GetEnum<SortBy>(SortBy.ElementAt((isResults ? SelectedSorter_Results : SelectedSorter)));
            if (!SorterItems.Any(x => x.Sorter == value))
                SorterItems.Add(new SorterItem(value, _DeleteSorter));
        }
        /// <summary>
        /// Delete selected sorter type from SorterItems
        /// </summary>
        /// <param name="item">deleting item</param>
        private void _DeleteSorter(object item)
        {
            SorterItems.Remove((SorterItem)item);
        }
        /// <summary>
        /// Command to add Sorter from settings tab
        /// </summary>
        public ICommand AddSorter { get; }
        /// <summary>
        /// Command to add sorter from Result tab
        /// </summary>
        public ICommand AddSorter_Results { get; }
        #endregion

        #region Filter
        /// <summary>
        /// Selected Filter index for Settings tab
        /// </summary>
        public int SelectedFilter { get; set; } = 0;
        /// <summary>
        /// Selected filter intex for Results tab
        /// </summary>
        public int SelectedFilter_Result { get; set; } = 0;

        /// <summary>
        /// Compare type array
        /// </summary>
        public IEnumerable<string> CompareBy => GetEnumNames<CompareType>();
        /// <summary>
        /// Selected compare type for Settings tab
        /// </summary>
        public int SelectedComparer { get; set; } = 0;
        /// <summary>
        /// Selected comparer for Results tab
        /// </summary>
        public int SelectedComparer_Result { get; set; } = 0;
        /// <summary>
        /// Selected compare border for Settings tab
        /// </summary>
        public double ComparerBorder { get; set; } = 0;
        /// <summary>
        /// Selected comparer for Results tab
        /// </summary>
        public double ComparerBorder_Result { get; set; } = 0;
        /// <summary>
        /// Selected filters with its params
        /// </summary>
        public ObservableCollection<FilterItem> FilterItems { get; } = new ObservableCollection<FilterItem>();
        /// <summary>
        /// Delete selected filter
        /// </summary>
        /// <param name="item">delitig filter</param>
        private void _DeleteFilter(object item)
        {
            FilterItems.Remove((FilterItem)item);
        }
        /// <summary>
        /// Add new filter
        /// </summary>
        /// <param name="isResult">main tabs togle</param>
        private void _AddFilter(bool isResult)
        {
            double value = (isResult ? ComparerBorder_Result : ComparerBorder);
            SortBy sortBy = GetEnum<SortBy>(SortBy.ElementAt((isResult ? SelectedFilter_Result : SelectedFilter)));
            CompareType compareType = GetEnum<CompareType>(CompareBy.ElementAt((isResult ? SelectedComparer_Result : SelectedComparer)));

            if (FilterItems.Any(x => x.Sorter == sortBy))
            {
                List<FilterItem> selectedItems = new List<FilterItem>(FilterItems.Where(x => x.Sorter == sortBy));

                foreach (var _item in selectedItems)
                {
                    _DeleteFilter(_item);
                    if ((compareType & _item.CompareType) == 0)
                    {
                        compareType |= _item.CompareType;
                    }
                }
            }

            if (!(compareType.HasFlag(CompareType.EqualTo) &&
               compareType.HasFlag(CompareType.GraterThan) &&
               compareType.HasFlag(CompareType.LessThan)))
            {
                FilterItem item = new FilterItem(sortBy, _DeleteFilter, compareType, value);
                FilterItems.Add(item);
            }
        }

        /// <summary>
        /// Add filter command keeper for Settings tab
        /// </summary>
        public ICommand AddFilter { get; }
        /// <summary>
        /// Add filter command keeper for Results tab
        /// </summary>
        public ICommand AddFilter_Result { get; }
        #endregion
        #endregion

        #region Start / Stop optimisation
        public string[] FileFillingType { get; } = new[] { "Rewrite", "Append" };
        /// <summary>
        /// Start optimisation or test (if optimisation is disabled by settings)
        /// </summary>
        /// <param name="o"></param>
        private void _StartStopOptimisation(object o)
        {
            OptimiserSetting setting = OptimiserSettings.Find(x => x.Name == "Optimisation mode");

            if (model.Optimiser.IsOptimisationInProcess)
            {
                model.StopOptimisation();
            }
            else
            {
                EnableMainTogles = false;
                OnPropertyChanged("EnableMainTogles");

                Model.OptimisationManagers.OptimiserInputData optimiserInputData = new Model.OptimisationManagers.OptimiserInputData
                {
                    Balance = Convert.ToDouble(OptimiserSettings.Find(x => x.Name == "Deposit").SelectedParam),
                    BotParams = BotParams?.Select(x => x.Param).ToList(),
                    CompareData = FilterItems.ToDictionary(x => x.Sorter, x => new KeyValuePair<CompareType, double>(x.CompareType, x.Border)),
                    Currency = OptimiserSettings.Find(x => x.Name == "Currency").SelectedParam,
                    ExecutionDelay = GetEnum<ENUM_ExecutionDelay>(OptimiserSettings.Find(x => x.Name == "Execution Mode").SelectedParam),
                    Laverage = Convert.ToInt32(OptimiserSettings.Find(x => x.Name == "Laverage").SelectedParam),
                    Model = GetEnum<ENUM_Model>(OptimiserSettings.Find(x => x.Name == "Optimisation model").SelectedParam),
                    OptimisationMode = GetEnum<ENUM_OptimisationMode>(OptimiserSettings.Find(x => x.Name == "Optimisation mode").SelectedParam),
                    RelativePathToBot = OptimiserSettings.Find(x => x.Name == "Available experts").SelectedParam,
                    Symb = AssetName,
                    TF = GetEnum<ENUM_Timeframes>(OptimiserSettings.Find(x => x.Name == "TF").SelectedParam),
                    HistoryBorders = (DateBorders.Any(x => x.BorderType == OptimisationType.History) ?
                                    DateBorders.Where(x => x.BorderType == OptimisationType.History)
                                    .Select(x => x.DateBorders).ToList() :
                                    new List<DateBorders>()),
                    ForwardBorders = (DateBorders.Any(x => x.BorderType == OptimisationType.Forward) ?
                                    DateBorders.Where(x => x.BorderType == OptimisationType.Forward)
                                    .Select(x => x.DateBorders).ToList() :
                                    new List<DateBorders>()),
                    SortingFlags = SorterItems.Select(x => x.Sorter)
                };

                model.StartOptimisation(optimiserInputData, FileWritingMode == "Append", DirPrefix);
            }
        }
        /// <summary>
        /// Start optimisation command
        /// </summary>
        public ICommand StartStopOptimisation { get; }
        #endregion

        #region Terminal names
        /// <summary>
        /// Detected terminals
        /// </summary>
        public IEnumerable<string> Terminals => model.TerminalNames;
        /// <summary>
        /// Selected terminal
        /// </summary>
        private int _selectedTerminalIndex = 0;
        public int SelectedTerminalIndex
        {
            get => _selectedTerminalIndex;
            set
            {
                if (value == -1)
                {
                    _selectedTerminalIndex = 0;
                    return;
                }
                if (model.ChangeTerminal(Terminals.ElementAt(value)))
                {
                    _selectedTerminalIndex = value;
                    OptimiserSettings.Find(x => x.Name == "Available experts").SetParams(model.Optimiser.TerminalManager.Experts);
                }
                else
                    System.Windows.MessageBox.Show("Can`t change terminal");
            }
        }
        #endregion

        #region Optimiser data
        /// <summary>
        /// Optimisers list
        /// </summary>
        public IEnumerable<string> Optimisers => model.OptimisatorNames;
        /// <summary>
        /// Selected optimiser
        /// </summary>
        public int _selectedOptimiserIndex = 0;
        public int SelectedOptimiserIndex
        {
            get => _selectedOptimiserIndex;
            set
            {
                if (value == -1)
                {
                    _selectedOptimiserIndex = 0;
                    return;
                }
                // Update optimiser
                if (model.ChangeOptimiser(Optimisers.ElementAt(value)))
                    _selectedOptimiserIndex = value;
                else
                    System.Windows.MessageBox.Show("Can`t change optimiser");
            }
        }
        /// <summary>
        /// Show optimiser GUI command
        /// </summary>
        public ICommand ShowOptimiserGUI { get; }
        /// <summary>
        /// File writing mode {Append or Rewrite}
        /// </summary>
        public string FileWritingMode { get; set; }
        /// <summary>
        /// Directory prefix
        /// </summary>
        public string DirPrefix { get; set; } = "";
        #endregion

        /// <summary>
        /// Bot params keeper
        /// </summary>
        public ObservableCollection<BotParamsData> BotParams { get; } = new ObservableCollection<BotParamsData>();

        #region DT border
        /// <summary>
        /// Optimisation date borders collection
        /// </summary>
        public ObservableCollection<DateBordersItem> DateBorders { get; } = new ObservableCollection<DateBordersItem>();
        /// <summary>
        /// Add date borders
        /// </summary>
        public ICommand AddDateBorder { get; }

        private void _SaveOrLoadDates(object o)
        {
            if (DateBorders.Count > 0)
                SaveDates();
            else
                LoadDates();
        }

        private void SaveDates()
        {
            using (System.Windows.Forms.SaveFileDialog sfd = new System.Windows.Forms.SaveFileDialog())
            {
                sfd.RestoreDirectory = true;

                if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    DTSourceManager.SaveBorders(DateBorders, sfd.FileName);
                }
            }
        }
        private void LoadDates()
        {
            try
            {
                using (System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog())
                {
                    ofd.RestoreDirectory = true;

                    if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        List<KeyValuePair<DateBorders, OptimisationType>> borders = DTSourceManager.GetBorders(ofd.FileName);
                        foreach (var item in borders)
                        {
                            DateBorders.Add(new DateBordersItem(item.Key, _DeleteDateBorder, item.Value));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message);
            }
        }

        public ICommand SaveOrLoadDates { get; }
        /// <summary>
        /// Start date
        /// </summary>
        public DateTime DateFrom { get; set; } = DateTime.Now;
        /// <summary>
        /// End date
        /// </summary>
        public DateTime DateTill { get; set; } = DateTime.Now;
        /// <summary>
        /// Date border types
        /// </summary>
        public IEnumerable<string> DateBorderTypes { get; }
        /// <summary>
        /// Selected date border
        /// </summary>
        public int SelectedDateBorderType { get; set; } = 0;
        /// <summary>
        /// Add dateborder callback
        /// </summary>
        /// <param name="o"></param>
        private void _AddDateBorder(object o)
        {
            try
            {
                DateBorders border = new DateBorders(DateFrom, DateTill);
                if (!DateBorders.Any(x => x.DateBorders == border))
                {
                    DateBorders.Add(new DateBordersItem(border, _DeleteDateBorder,
                        GetEnum<OptimisationType>(DateBorderTypes.ElementAt(SelectedDateBorderType))));
                }
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message);
            }
        }
        /// <summary>
        /// Delete selected date border
        /// </summary>
        /// <param name="item">Deleting item</param>
        private void _DeleteDateBorder(DateBordersItem item)
        {
            DateBorders.Remove(item);
        }
        #endregion

        #region Results data
        /// <summary>
        /// Load results command
        /// </summary>
        public ICommand LoadResults { get; }

        #region Selected saved optimisation
        /// <summary>
        /// Selected optimisation index
        /// </summary>
        public string SelectedOptimisation { get; set; }
        /// <summary>
        /// Selected optimisation names
        /// </summary>
        public IEnumerable<string> SelectedOptimisationNames => model.SavedOptimisations;
        #endregion

        /// <summary>
        /// Start test by double click event
        /// </summary>
        /// <param name="results">optimisation results</param>
        /// <param name="ind">selected item index</param>
        private void _StartTest(List<OptimisationResult> results, int ind)
        {
            try
            {
                Model.OptimisationManagers.OptimiserInputData optimiserInputData = new Model.OptimisationManagers.OptimiserInputData
                {
                    Balance = Convert.ToDouble(OptimiserSettingsForResults_fixed.First(x => x.Key == "Deposit").Value),
                    Currency = OptimiserSettingsForResults_fixed.First(x => x.Key == "Currency").Value,
                    ExecutionDelay = GetEnum<ENUM_ExecutionDelay>(OptimiserSettingsForResults_changing.First(x => x.Name == "Execution Mode").SelectedParam),
                    Laverage = Convert.ToInt32(OptimiserSettingsForResults_fixed.First(x => x.Key == "Laverage").Value),
                    Model = GetEnum<ENUM_Model>(OptimiserSettingsForResults_changing.First(x => x.Name == "Optimisation model").SelectedParam),
                    OptimisationMode = ENUM_OptimisationMode.Disabled,
                    RelativePathToBot = OptimiserSettingsForResults_fixed.First(x => x.Key == "Expert").Value,
                    ForwardBorders = new List<DateBorders>(),
                    HistoryBorders = new List<DateBorders> { new DateBorders(TestFrom, TestTill) },
                    Symb = OptimiserSettingsForResults_fixed.First(x => x.Key == "Symbol").Value,
                    TF = (ENUM_Timeframes)Enum.Parse(typeof(ENUM_Timeframes), OptimiserSettingsForResults_fixed.First(x => x.Key == "TF").Value),
                    SortingFlags = null,
                    CompareData = null,
                    BotParams = results[ind].report.BotParams.Select(x => new ParamsItem { Variable = x.Key, Value = x.Value }).ToList()
                };

                model.StartTest(optimiserInputData);
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message);
            }
        }
        /// <summary>
        /// Start test from forward table
        /// </summary>
        public ICommand StartTestForward { get; }
        /// <summary>
        /// Start test from history table
        /// </summary>
        public ICommand StartTestHistory { get; }
        /// <summary>
        /// Start test from report table
        /// </summary>
        public ICommand StartTestReport { get; }

        /// <summary>
        /// data into (*.csv) file
        /// </summary>
        /// <param name="o">file indicator (setting from view)</param>
        private void _SaveToCsv(object o)
        {
            using (System.Windows.Forms.SaveFileDialog sfd = new System.Windows.Forms.SaveFileDialog())
            {
                sfd.RestoreDirectory = true;

                if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if (o.ToString() == "Report")
                        model.SaveToCSVOptimisations(ReportDateBorders[SelectedReportDateBorder], sfd.FileName);
                    else
                        model.SaveToCSVSelectedOptimisations(sfd.FileName);
                }
            }
        }
        /// <summary>
        /// Save data into (*.csv) file command
        /// </summary>
        public ICommand SaveToCsv { get; }

        #region All optimisations table

        /// <summary>
        /// Filling bot params
        /// </summary>
        /// <param name="result">results</param>
        private void FillInBotParams(OptimisationResult result)
        {
            SelectedBotParams.Clear();
            foreach (var item in result.report.BotParams)
            {
                SelectedBotParams.Add(item);
            }

            ReplaceBotFixedParam("Symbol", result.report.Symbol);
            ReplaceBotFixedParam("TF", ((ENUM_Timeframes)result.report.TF).ToString());
            TestFrom = result.report.DateBorders.From;
            TestTill = result.report.DateBorders.Till;
            OnPropertyChanged("TestFrom");
            OnPropertyChanged("TestTill");
        }
        /// <summary>
        /// Fill in daily pl params
        /// </summary>
        /// <param name="result">results</param>
        private void FillInDailyPL(OptimisationResult result)
        {
            KeyValuePair<DayOfWeek, DailyPLItem> GetItem(DayOfWeek day)
            {
                return new KeyValuePair<DayOfWeek, DailyPLItem>(day, (result.report.OptimisationCoefficients.TradingDays.Count > 0 ?
                                                                new DailyPLItem(result.report.OptimisationCoefficients.TradingDays[day]) :
                                                                null));
            }

            TradingDays[0] = GetItem(DayOfWeek.Monday);
            TradingDays[1] = GetItem(DayOfWeek.Thursday);
            TradingDays[2] = GetItem(DayOfWeek.Wednesday);
            TradingDays[3] = GetItem(DayOfWeek.Thursday);
            TradingDays[4] = GetItem(DayOfWeek.Friday);
            OnPropertyChanged("TradingDays");
        }
        /// <summary>
        /// Fillin daily PL
        /// </summary>
        /// <param name="result">results</param>
        private void FillInMaxPLDD(OptimisationResult result)
        {
            KeyValuePair<string, KeyValuePair<string, string>> GetItem(string param)
            {
                KeyValuePair<string, string> value;
                switch (param)
                {
                    case "Value":
                        value = new KeyValuePair<string, string>(result.report.OptimisationCoefficients.MaxPLDD.Profit.Value.ToString(),
                                                                result.report.OptimisationCoefficients.MaxPLDD.DD.Value.ToString());
                        break;
                    case "Total trades":
                        value = new KeyValuePair<string, string>(result.report.OptimisationCoefficients.MaxPLDD.Profit.TotalTrades.ToString(),
                                                                result.report.OptimisationCoefficients.MaxPLDD.DD.TotalTrades.ToString());
                        break;
                    case "Consecutive trades":
                        value = new KeyValuePair<string, string>(result.report.OptimisationCoefficients.MaxPLDD.Profit.ConsecutivesTrades.ToString(),
                                                                result.report.OptimisationCoefficients.MaxPLDD.DD.ConsecutivesTrades.ToString());
                        break;
                    default:
                        value = new KeyValuePair<string, string>(null, null);
                        break;
                }

                return new KeyValuePair<string, KeyValuePair<string, string>>(param, value);
            }

            MaxPLDD[0] = GetItem(MaxPLDD[0].Key);
            MaxPLDD[1] = GetItem(MaxPLDD[1].Key);
            MaxPLDD[2] = GetItem(MaxPLDD[2].Key);
        }
        /// <summary>
        /// Bot params keeper
        /// </summary>
        public ObservableCollection<KeyValuePair<string, string>> SelectedBotParams { get; } =
            new ObservableCollection<KeyValuePair<string, string>>();
        /// <summary>
        /// Trading dais keeper
        /// </summary>
        public ObservableCollection<KeyValuePair<DayOfWeek, DailyPLItem>> TradingDays { get; } = new ObservableCollection<KeyValuePair<DayOfWeek, DailyPLItem>>
        {
            new KeyValuePair<DayOfWeek, DailyPLItem>(DayOfWeek.Monday, null ),
            new KeyValuePair<DayOfWeek, DailyPLItem>(DayOfWeek.Tuesday, null ),
            new KeyValuePair<DayOfWeek, DailyPLItem>(DayOfWeek.Wednesday, null ),
            new KeyValuePair<DayOfWeek, DailyPLItem>(DayOfWeek.Thursday, null ),
            new KeyValuePair<DayOfWeek, DailyPLItem>(DayOfWeek.Friday, null )
        };
        /// <summary>
        /// Dily PL keeper
        /// </summary>
        public ObservableCollection<KeyValuePair<string, KeyValuePair<string, string>>> MaxPLDD { get; } =
            new ObservableCollection<KeyValuePair<string, KeyValuePair<string, string>>>
        {
            new KeyValuePair<string, KeyValuePair<string,string>>("Value",new KeyValuePair<string,string>(null,null)),
            new KeyValuePair<string, KeyValuePair<string,string>>("Total trades",new KeyValuePair<string,string>(null,null)),
            new KeyValuePair<string, KeyValuePair<string,string>>("Consecutive trades",new KeyValuePair<string,string>(null,null))
        };
        /// <summary>
        /// Selected report item
        /// </summary>
        private int _selecterReportItem;
        public int SelecterReportItem
        {
            get => _selecterReportItem;
            set
            {
                _selecterReportItem = value;
                if (value > -1)
                {
                    OptimisationResult item = model.AllOptimisationResults
                        .AllOptimisationResults[ReportDateBorders[SelectedReportDateBorder]]
                        .ElementAt(value);

                    FillInBotParams(item);
                    FillInDailyPL(item);
                    FillInMaxPLDD(item);
                }
            }
        }
        /// <summary>
        /// Selected forward report
        /// </summary>
        private int _selectedForwardItem;
        public int SelectedForwardItem
        {
            get => _selectedForwardItem;
            set
            {
                _selectedForwardItem = value;
                if (value > -1)
                {
                    FillInBotParams(model.ForwardOptimisations[value]);
                    FillInDailyPL(model.ForwardOptimisations[value]);
                    FillInMaxPLDD(model.ForwardOptimisations[value]);
                }
            }
        }
        /// <summary>
        /// Selected hirtory report
        /// </summary>
        private int _selectedHistoryItem;
        public int SelectedHistoryItem
        {
            get => _selectedHistoryItem;
            set
            {
                _selectedHistoryItem = value;
                if (value > -1)
                {
                    FillInBotParams(model.HistoryOptimisations[value]);
                    FillInDailyPL(model.HistoryOptimisations[value]);
                    FillInMaxPLDD(model.HistoryOptimisations[value]);
                }
            }
        }

        /// <summary>
        /// Sort report
        /// </summary>
        /// <param name="o"></param>
        private void _SortResults(object o)
        {
            if (ReportDateBorders.Count == 0)
                return;
            IEnumerable<SortBy> sortFlags = SorterItems.Select(x => x.Sorter);
            if (sortFlags.Count() == 0)
                return;
            if (AllOptimisations.Count == 0)
                return;

            model.SortResults(ReportDateBorders[SelectedReportDateBorder], sortFlags);
        }
        public ICommand SortResults { get; }

        /// <summary>
        /// Filter report
        /// </summary>
        /// <param name="o"></param>
        private void _FilterResults(object o)
        {
            if (ReportDateBorders.Count == 0)
                return;

            IDictionary<SortBy, KeyValuePair<CompareType, double>> compareData =
                FilterItems.ToDictionary(x => x.Sorter, x => new KeyValuePair<CompareType, double>(x.CompareType, x.Border));

            if (compareData.Count() == 0)
                return;
            if (AllOptimisations.Count == 0)
                return;

            model.FilterResults(ReportDateBorders[SelectedReportDateBorder], compareData);
        }
        public ICommand FilterResults { get; }

        /// <summary>
        /// Selected date border for reports
        /// </summary>
        #region Selected optimisation date border index keeper
        private int _selectedReportDateBorder;
        public int SelectedReportDateBorder
        {
            get => _selectedReportDateBorder;
            set
            {
                AllOptimisations.Clear();
                if (value == -1)
                {
                    _selectedReportDateBorder = 0;
                    return;
                }
                _selectedReportDateBorder = value;
                if (ReportDateBorders.Count == 0)
                    return;

                List<OptimisationResult> collection = model.AllOptimisationResults.AllOptimisationResults[ReportDateBorders[value]];
                foreach (var item in collection)
                {
                    AllOptimisations.Add(new ReportItem(item));
                }
            }
        }
        #endregion

        /// <summary>
        /// date borders for All optimisations keeper
        /// </summary>
        public ObservableCollection<DateBorders> ReportDateBorders { get; } = new ObservableCollection<DateBorders>();
        /// <summary>
        /// Selected report for "SelectedReportDateBorder"
        /// </summary>
        public ObservableCollection<ReportItem> AllOptimisations { get; } = new ObservableCollection<ReportItem>();
        #endregion

        #region Forward and History optimisations table
        /// <summary>
        /// Selected forward optimisations
        /// </summary>
        public ObservableCollection<ReportItem> ForwardOptimisations { get; } = new ObservableCollection<ReportItem>();
        /// <summary>
        /// Selected history optimisations
        /// </summary>
        public ObservableCollection<ReportItem> HistoryOptimisations { get; } = new ObservableCollection<ReportItem>();

        #endregion

        #region  OptimiserSettingsForResults
        /// <summary>
        /// Fixed optimisation rusults
        /// </summary>
        public ObservableCollection<KeyValuePair<string, string>> OptimiserSettingsForResults_fixed { get; }
        /// <summary>
        /// Replace fixed bot params
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        void ReplaceBotFixedParam(string key, string value)
        {
            KeyValuePair<string, string> item = OptimiserSettingsForResults_fixed.First(x => x.Key == key);
            int ind = OptimiserSettingsForResults_fixed.IndexOf(item);
            OptimiserSettingsForResults_fixed[ind] = new KeyValuePair<string, string>(key, value);
        }
        /// <summary>
        /// Changing bot params for test from results tab
        /// </summary>
        public List<OptimiserSetting> OptimiserSettingsForResults_changing { get; }

        public DateTime TestFrom { get; set; } = DateTime.Now;
        public DateTime TestTill { get; set; } = DateTime.Now;
        #endregion

        #endregion

        #region EnumToString StringToEnum
        private T GetEnum<T>(string param)
        {
            return (T)Enum.Parse(typeof(T), param.Replace(" ", "_"));
        }
        private IEnumerable<string> GetEnumNames<T>()
        {
            return Enum.GetNames(typeof(T)).Select(x => x.Replace("_", " "));
        }
        #endregion
    }

    #region Entities for GUI

    /// <summary>
    /// Report item wrapper
    /// </summary>
    class ReportItem
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="item">report item</param>
        public ReportItem(OptimisationResult item)
        {
            result = item;
        }
        /// <summary>
        /// Report item
        /// </summary>
        private readonly OptimisationResult result;

        /// <summary>
        /// From date getter
        /// </summary>
        public DateTime From => result.report.DateBorders.From;
        /// <summary>
        /// Till date getter
        /// </summary>
        public DateTime Till => result.report.DateBorders.Till;
        /// <summary>
        /// Sort by getter
        /// </summary>
        public double SortBy => result.SortBy;
        /// <summary>
        /// Payoff getter
        /// </summary>
        public double Payoff => result.report.OptimisationCoefficients.Payoff;
        /// <summary>
        /// Profit facter getter
        /// </summary>
        public double ProfitFactor => result.report.OptimisationCoefficients.ProfitFactor;
        public double AverageProfitFactor => result.report.OptimisationCoefficients.AverageProfitFactor;
        /// <summary>
        /// Recovery factor getter
        /// </summary>
        public double RecoveryFactor => result.report.OptimisationCoefficients.RecoveryFactor;
        public double AverageRecoveryFactor => result.report.OptimisationCoefficients.AverageRecoveryFactor;
        /// <summary>
        /// PL  getter
        /// </summary>
        public double PL => result.report.OptimisationCoefficients.PL;
        /// <summary>
        /// DD getter 
        /// </summary>
        public double DD => result.report.OptimisationCoefficients.DD;
        /// <summary>
        /// Altman Z score getter
        /// </summary>
        public double AltmanZScore => result.report.OptimisationCoefficients.AltmanZScore;
        /// <summary>
        /// Total trades getter
        /// </summary>
        public int TotalTrades => result.report.OptimisationCoefficients.TotalTrades;
        /// <summary>
        /// VaR getter
        /// </summary>
        public double VaR90 => result.report.OptimisationCoefficients.VaR.Q_90;
        /// <summary>
        /// VaR getter
        /// </summary>
        public double VaR95 => result.report.OptimisationCoefficients.VaR.Q_95;
        /// <summary>
        /// VaR getter
        /// </summary>
        public double VaR99 => result.report.OptimisationCoefficients.VaR.Q_99;
        /// <summary>
        /// Mx getter
        /// </summary>
        public double Mx => result.report.OptimisationCoefficients.VaR.Mx;
        /// <summary>
        /// Std getter
        /// </summary>
        public double Std => result.report.OptimisationCoefficients.VaR.Std;

    }

    /// <summary>
    /// Date border keeper for GUI
    /// </summary>
    class DateBordersItem
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dateBorders">Dates diaposone</param>
        /// <param name="delete">callback that deletes current item</param>
        /// <param name="borderType">history/forward border type</param>
        public DateBordersItem(DateBorders dateBorders, Action<DateBordersItem> delete, OptimisationType borderType)
        {
            DateBorders = dateBorders;
            BorderType = borderType;
            Delete = new RelayCommand((object o) => delete(this));
        }
        /// <summary>
        /// Date borders keeper
        /// </summary>
        public DateBorders DateBorders { get; }
        /// <summary>
        /// Start date
        /// </summary>
        public DateTime From => DateBorders.From;
        /// <summary>
        /// Finish date
        /// </summary>
        public DateTime Till => DateBorders.Till;
        /// <summary>
        /// History of forward date type
        /// </summary>
        public OptimisationType BorderType { get; }
        /// <summary>
        /// Callback to delete current item
        /// </summary>
        public ICommand Delete { get; }
    }

    /// <summary>
    /// Data sorter item
    /// </summary>
    class SorterItem
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="sorter">Sorter type</param>
        /// <param name="deleteItem">Delete acurrent item</param>
        public SorterItem(SortBy sorter, Action<object> deleteItem)
        {
            Sorter = sorter;
            Delete = new RelayCommand((object o) => deleteItem(this));
        }
        /// <summary>
        /// Sorter item
        /// </summary>
        public SortBy Sorter { get; }
        /// <summary>
        /// Delete current item callback
        /// </summary>
        public ICommand Delete { get; }
    }

    /// <summary>
    /// Filter item
    /// </summary>
    class FilterItem : SorterItem
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="sorter">Sorterm item</param>
        /// <param name="deleteItem">Delete callback</param>
        /// <param name="compareType">Compare type</param>
        /// <param name="border">Data border</param>
        public FilterItem(SortBy sorter, Action<object> deleteItem,
                          CompareType compareType, double border) : base(sorter, deleteItem)
        {
            CompareType = compareType;
            Border = border;
        }
        /// <summary>
        /// Compare type
        /// </summary>
        public CompareType CompareType { get; }
        /// <summary>
        /// Data border
        /// </summary>
        public double Border { get; }
    }

    /// <summary>
    /// Bot param item for GUI
    /// </summary>
    class BotParamsData
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="param">bot param item</param>
        public BotParamsData(ParamsItem param)
        {
            this.param = param;
        }
        /// <summary>
        /// Bot param item
        /// </summary>
        private ParamsItem param;
        /// <summary>
        /// Bot param item getter
        /// </summary>
        public ParamsItem Param => param;
        /// <summary>
        /// IsOptimise property getter/setter
        /// </summary>
        public bool IsOptimize
        {
            get => param.IsOptimize;
            set => param.IsOptimize = value;
        }
        /// <summary>
        /// Bot param name getter
        /// </summary>
        public string Vriable
        {
            get => param.Variable;
        }
        /// <summary>
        /// Value property getter/setter
        /// </summary>
        public string Value
        {
            get => param.Value;
            set => param.Value = value;
        }
        /// <summary>
        /// Start property getter/setter
        /// </summary>
        public string Start
        {
            get => param.Start;
            set => param.Start = value;
        }
        /// <summary>
        /// Step property getter/setter
        /// </summary>
        public string Step
        {
            get => param.Step;
            set => param.Step = value;
        }
        /// <summary>
        /// Stop property getter/setter
        /// </summary>
        public string Stop
        {
            get => param.Stop;
            set => param.Stop = value;
        }
    }

    /// <summary>
    /// Daily PL item
    /// </summary>
    class DailyPLItem
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dailyData">daily pl data</param>
        public DailyPLItem(DailyData dailyData)
        {
            this.dailyData = dailyData;
        }

        /// <summary>
        /// Daily pl data
        /// </summary>
        private readonly DailyData dailyData;
        /// <summary>
        /// Daily profit param getter
        /// </summary>
        public double DailyProfit => dailyData.Profit.Value;
        /// <summary>
        /// Daily profit trades param getter
        /// </summary>
        public int DailyProfitTrades => dailyData.Profit.Trades;
        /// <summary>
        /// Daily DD param getter
        /// </summary>
        public double DailyDD => dailyData.DD.Value;
        /// <summary>
        /// Daily DD trades getter
        /// </summary>
        public int DailyDDTrades => dailyData.DD.Trades;
    }

    /// <summary>
    /// Wrapper for changeble bot param items
    /// </summary>
    class OptimiserSetting : INotifyPropertyChanged
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="Name">Bot name</param>
        /// <param name="Params">Bot param</param>
        /// <param name="SelectionChanged">Combobox selection changed</param>
        public OptimiserSetting(string Name, IEnumerable<string> Params, Action<string> SelectionChanged = null)
        {
            this.Name = Name;
            this.Params = Params;
            this.SelectionChanged = SelectionChanged;
        }
        /// <summary>
        /// Combobox selection changed keeper
        /// </summary>
        private readonly Action<string> SelectionChanged;
        /// <summary>
        /// Param name
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// Possiblevalues for the selected param
        /// </summary>
        public IEnumerable<string> Params { get; private set; }
        /// <summary>
        /// Set new param`s possiblevalues
        /// </summary>
        /// <param name="params"></param>
        public void SetParams(IEnumerable<string> @params)
        {
            Params = @params;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Params"));

            SelectedIndex = 0;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SelectedIndex"));
        }

        #region Selected value
        public string SelectedParam => (Params.Count() > 0 ? Params.ElementAt(SelectedIndex) : null);

        private int _selectedIndex = 0;

        public event PropertyChangedEventHandler PropertyChanged;

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (value == -1)
                {
                    _selectedIndex = 0;
                    return;
                }
                _selectedIndex = value;
                // Run selecteion changed callback in asyncron mode
                SelectionChanged?.BeginInvoke(Params.Count() > 0 ? Params.ElementAt(value) : null, null, null);
            }
        }
        #endregion
    }
    #endregion
}
