using Metatrader_Auto_Optimiser.Model;
using Metatrader_Auto_Optimiser.Model.FileReaders;
using Metatrader_Auto_Optimiser.Model.Terminal;
using ReportManager;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

using Metatrader_Auto_Optimiser.View;

namespace Metatrader_Auto_Optimiser.View_Model
{
    /// <summary>
    /// View model
    /// </summary>
    class AutoOptimiserVM : INotifyPropertyChanged
    {
        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        public AutoOptimiserVM()
        {
            // Инстанцируем оптимизатор стоящий первым в списке оптимизаторов
            if (!model.ChangeOptimiser(Optimisers.ElementAt(SelectedOptimiserIndex), Terminals.ElementAt(SelectedTerminalIndex)))
            {
                System.Windows.MessageBox.Show("Can`t instance optimiser !");
                throw new Exception("Can`t instance optimiser !");
            }

            // Устанавливаем вариации настроек тестера/оптимизатора на главной вкладке
            OptimiserSettings = new List<OptimiserSetting>
                                {
                                    new OptimiserSetting("Available experts", model.Optimiser.TerminalManager.Experts, (string botName)=>
                                    { SetBotParams(botName,false); }),
                                    new OptimiserSetting("Execution Mode", GetEnumNames<ENUM_ExecutionDelay>()),
                                    new OptimiserSetting("Deposit", new []{ "1000", "3000", "5000", "10000", "25000", "50000", "100000" }),
                                    new OptimiserSetting("Currency", new []{ "RUR", "USD", "EUR", "GBP", "CHF"}),
                                    new OptimiserSetting("Optimisation mode", GetEnumNames<ENUM_OptimisationMode>()),
                                    new OptimiserSetting("Laverage", new []{"1", "5", "50", "100", "500", "1000"}),
                                    new OptimiserSetting("TF", GetEnumNames<ENUM_Timeframes>()),
                                    new OptimiserSetting("Optimisation model", GetEnumNames<ENUM_Model>())
                                };
            // Устанавливаем пустые (изменятся при загрузке результатов оптимизации) настройки тестера / оптимиазтора на вкладке с результатами оптимизации
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
            // Подписка на события модели данных
            model.OptimisationStoped += Model_OptimisationStoped;
            model.PBUpdate += Model_PBUpdate;
            model.ThrowException += Model_ThrowException;
            model.PropertyChanged += Model_PropertyChanged;

            // Заполняем настройки для первого робота из списка
            var settings = OptimiserSettings.Find(x => x.Name == "Available experts");
            SetBotParams(settings.SelectedParam, false);

            #region Subwindows
            autoFillInDateBorders = new SubFormKeeper(() => { return new AutoFillInDateBorders(); });
            AutoFillInDateBordersCreator.Model.DateBorders += Model_DateBorders;
            #endregion

            // Заполняем коллбеки графического интерфейса
            #region Fill in commands
            // Коллбек кнопок добавления сортировщика
            AddSorter = new RelayCommand((object o) => _AddSorter(false));
            AddSorter_Results = new RelayCommand((object o) => _AddSorter(true));

            // Коллбек кнопок добавления фильтров
            AddFilter = new RelayCommand((object o) => _AddFilter(false));
            AddFilter_Result = new RelayCommand((object o) => _AddFilter(true));

            // Коллбек кнопки запуска / остановки процесса оптимизации
            StartStopOptimisation = new RelayCommand(_StartStopOptimisation);

            // Список параметров для ComboBox с выбором типа оптимизации
            DateBorderTypes = GetEnumNames<OptimisationType>();
            // Коллбек добавления границы дат оптимизации
            AddDateBorder = new RelayCommand(_AddDateBorder);

            // Коллбек кнопки загрузки результатов оптимизации
            LoadResults = new RelayCommand((object o) => model.LoadSavedOptimisation(SelectedOptimisation));

            // Коллбек кнопки вызова графического интерфейса оптимизатора
            ShowOptimiserGUI = new RelayCommand((object o) =>
            {
                try { model.Optimiser.LoadSettingsWindow(); }
                catch (Exception e) { System.Windows.MessageBox.Show(e.Message); }
            });

            // Коллбек кнопки сортировка загруженных результатов оптимизации
            SortResults = new RelayCommand(_SortResults);
            // Коллбек кнопки фильтрации загруженных результатов оптимизации
            FilterResults = new RelayCommand(_FilterResults);
            // Коллбек запуска теста по событию двойного клика на таблице с оптимизациями
            StartTestReport = new RelayCommand((object o) =>
            {
                _StartTest(AllOptimisations, SelecterReportItem);
            });
            // Коллбек старта теста по событию двойного клика на таблице с историческими тестами
            StartTestHistory = new RelayCommand((object o) =>
            {
                _StartTest(model.HistoryOptimisations, SelectedHistoryItem);
            });
            // Коллбек старта теста по событию двойного клика на таблице с форвардными тестами
            StartTestForward = new RelayCommand((object o) =>
            {
                _StartTest(model.ForwardOptimisations, SelectedForwardItem);
            });
            // Коллебук созранения в (*.scv) файл результатов оптимизации
            SaveToCsv = new RelayCommand(_SaveToCsv);

            // Коллбек нажатия на кнопку обновления (*set) файла с настройками выбранного робота
            UpdateSetFile = new RelayCommand((object o) =>
            {
                System.Threading.Tasks.Task.Run(() =>
                {
                    SetBotParams(OptimiserSettings.First(x => x.Name == "Available experts").SelectedParam, true);
                });
            });
            // Коллбек нажатия на кнопку созранения или же напротив загрузки файла с результатами выбора форвардных и исторических диаппазонов
            SaveOrLoadDates = new RelayCommand(_SaveOrLoadDates);

            // Коллбек нажатия кнопки автоматического формирования дат оптимизаций
            AutosetDateBorder = new RelayCommand((object o) =>
            {
                autoFillInDateBorders.Open();
            });

            // Коллбек отчистки границ дат 
            ClearDateBorders = new RelayCommand((object o) =>
            {
                DateBorders.Clear();
            });

            // Коллбек нажатия кнопки отчистки результатов оптимизации из памяти
            ClearLoadedResults = new RelayCommand((object o) => { model.ClearResults(); });
            #endregion
        }

        /// <summary>
        /// Деструктор
        /// </summary>
        ~AutoOptimiserVM()
        {
            // Отписка от событий модели данных
            model.OptimisationStoped -= Model_OptimisationStoped;
            model.PBUpdate -= Model_PBUpdate;
            model.ThrowException -= Model_ThrowException;
            model.PropertyChanged -= Model_PropertyChanged;

            AutoFillInDateBordersCreator.Model.DateBorders -= Model_DateBorders;
            autoFillInDateBorders.Close();
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

        //Коллбек события изменения какого либо свойства модели данных и какиз либо событий модели данных 
        private void Model_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Завершился тест или же нужно возобновить доступность кнопок заблокированных при старте оптимизации или же теста
            if (e.PropertyName == "StopTest" ||
                e.PropertyName == "ResumeEnablingTogle")
            {
                // переключатель доступности кнопок = true
                EnableMainTogles = true;
                // Скидываем статус и прогресс
                Status = "";
                Progress = 0;

                // Уведомляем графику о произошедших изменениях
                dispatcher.Invoke(() =>
                {
                    OnPropertyChanged("EnableMainTogles");
                    OnPropertyChanged("Status");
                    OnPropertyChanged("Progress");
                });
            }

            if (e.PropertyName == "ClearResults")
            {
                OptimisationResult result = new OptimisationResult();

                FillInDataForReportItem(result);
            }

            // Изменился список пройденных оптимизационных проходов
            if (e.PropertyName == "AllOptimisationResults")
            {
                dispatcher.Invoke(() =>
                {
                    // Отчищаем ранее сохраненные проходы оптимизаций и добавляем новые
                    ReportDateBorders.Clear();
                    foreach (var item in model.AllOptimisationResults.AllOptimisationResults.Keys)
                    {
                        ReportDateBorders.Add(item);
                    }

                    // Выбираем самую первую дату
                    SelectedReportDateBorder = 0;

                    // Заполняем фиксированные настройки тестера в соответствии с настройками выгруженных результатов
                    ReplaceBotFixedParam("Expert", model.AllOptimisationResults.Expert);
                    ReplaceBotFixedParam("Deposit", model.AllOptimisationResults.Deposit.ToString());
                    ReplaceBotFixedParam("Currency", model.AllOptimisationResults.Currency);
                    ReplaceBotFixedParam("Laverage", model.AllOptimisationResults.Laverage.ToString());
                    OnPropertyChanged("OptimiserSettingsForResults_fixed");
                });
            }

            // Либо фильтрация либо сортировка проходов оптимизации
            if (e.PropertyName == "SortedResults" ||
                e.PropertyName == "FilteredResults")
            {
                dispatcher.Invoke(() =>
                {
                    SelectedReportDateBorder = SelectedReportDateBorder;
                });
            }

            // Созранен (*.csv) файл с результатами оптимизации / тестов
            if (e.PropertyName == "CSV")
            {
                System.Windows.MessageBox.Show("(*.csv) File saved");
            }
        }
        /// <summary>
        /// Обновление статуса и прогресс бара из события модели
        /// </summary>
        /// <param name="status">новый статус</param>
        /// <param name="value">новоз значение </param>
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
        /// Отображение текста ошибок из модели данных
        /// </summary>
        /// <param name="e">Текст ошибки</param>
        private void Model_ThrowException(string e)
        {
            System.Windows.MessageBox.Show(e);
        }
        /// <summary>
        /// Коллбек вызываемый после завершения процесса оптимизации
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
        /// Модель данных
        /// </summary>
        private readonly IMainModel model = MainModelCreator.Model;
        /// <summary>
        /// Диспатчер основного окна
        /// </summary>
        private readonly System.Windows.Threading.Dispatcher dispatcher =
            System.Windows.Application.Current.Dispatcher;

        #region Status and progress
        /// <summary>
        /// Статус
        /// </summary>
        public string Status { get; set; }
        /// <summary>
        /// Прогресс бар
        /// </summary>
        public double Progress { get; set; } = 0;
        #endregion

        /// <summary>
        /// Если этот переключатель = false, то наиболее важные поля - недоступны
        /// </summary>
        public bool EnableMainTogles { get; private set; } = true;

        #region Bot params
        /// <summary>
        /// Коллбек заполняющий параметры робота и отображающий их
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
        /// Настройки оптимизатора
        /// </summary>
        public List<OptimiserSetting> OptimiserSettings { get; }
        public ICommand UpdateSetFile { get; }
        /// <summary>
        /// Имя актива выбранного для тестов / оптимизации
        /// </summary>
        public string AssetName { get; set; }
        #endregion

        #region Sorter ans Filter in Settings
        /// <summary>
        /// Список параметров сортировки данных
        /// </summary>
        public IEnumerable<string> SortBy => GetEnumNames<SortBy>();

        #region Sorter
        /// <summary>
        /// Выбранный параметр сортировки для вкладки с настройками
        /// </summary>
        public int SelectedSorter { get; set; } = 0;
        /// <summary>
        /// Выбранный параметр сортировки для вкладки с результатами оптимизации
        /// </summary>
        public int SelectedSorter_Results { get; set; } = 0;
        /// <summary>
        /// Отобранные параметры сортировки
        /// </summary>
        public ObservableCollection<SorterItem> SorterItems { get; } = new ObservableCollection<SorterItem>();
        /// <summary>
        /// Коллбек добавления сортировщика к коллекции SorterItems
        /// </summary>
        /// <param name="isResults">Переключатель типа вкладок</param>
        private void _AddSorter(bool isResults)
        {
            SortBy value = GetEnum<SortBy>(SortBy.ElementAt((isResults ? SelectedSorter_Results : SelectedSorter)));
            if (!SorterItems.Any(x => x.Sorter == value))
                SorterItems.Add(new SorterItem(value, _DeleteSorter));
        }
        /// <summary>
        /// Коллбек удаления выбранного способа сортировки из SorterItems
        /// </summary>
        /// <param name="item">Удаляемый эллемент</param>
        private void _DeleteSorter(object item)
        {
            SorterItems.Remove((SorterItem)item);
        }
        /// <summary>
        /// Коллбек добавления сортировки из основной вкладке (с настройками)
        /// </summary>
        public ICommand AddSorter { get; }
        /// <summary>
        ///Коллбек добавления сортировщика из вкладки с результатами оптимизации
        /// </summary>
        public ICommand AddSorter_Results { get; }
        #endregion

        #region Filter
        /// <summary>
        /// Выбранный фильтр данных на вкладки Settings
        /// </summary>
        public int SelectedFilter { get; set; } = 0;
        /// <summary>
        /// Выбранный фильтр на вкладке Results
        /// </summary>
        public int SelectedFilter_Result { get; set; } = 0;

        /// <summary>
        /// Типы сопостовления данных фильтра
        /// </summary>
        public IEnumerable<string> CompareBy => GetEnumNames<CompareType>();
        /// <summary>
        /// Выбранный тип сопостовления на вкладке Settings
        /// </summary>
        public int SelectedComparer { get; set; } = 0;
        /// <summary>
        /// Выбранный тип сопоставления на вкладке Results
        /// </summary>
        public int SelectedComparer_Result { get; set; } = 0;
        /// <summary>
        /// Выбранное значение сопостовления на вкладке Settings
        /// </summary>
        public double ComparerBorder { get; set; } = 0;
        /// <summary>
        /// Выбранное значение сопостовления на вкладк Results
        /// </summary>
        public double ComparerBorder_Result { get; set; } = 0;
        /// <summary>
        ///Выбранные фильтры
        /// </summary>
        public ObservableCollection<FilterItem> FilterItems { get; } = new ObservableCollection<FilterItem>();
        /// <summary>
        /// Коллбек удаления выбранного фильтра
        /// </summary>
        /// <param name="item">Удаляемый фильтр</param>
        private void _DeleteFilter(object item)
        {
            FilterItems.Remove((FilterItem)item);
        }
        /// <summary>
        /// Коллбек добавления нового фильтра
        /// </summary>
        /// <param name="isResult">Тип вкладки</param>
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
        /// Коллбек добавления фильтра на вкладке Settings
        /// </summary>
        public ICommand AddFilter { get; }
        /// <summary>
        /// Коллбек добавления фильтра на вкладке Results
        /// </summary>
        public ICommand AddFilter_Result { get; }
        #endregion
        #endregion

        #region Start / Stop optimisation
        public string[] FileFillingType { get; } = new[] { "Rewrite", "Append" };
        /// <summary>
        /// Запуск оптимизации или теста (если режим оптимизации выключен)
        /// </summary>
        /// <param name="o"></param>
        private void _StartStopOptimisation(object o)
        {
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
        /// Коллбек для графического интерфейса - запуска оптимизации / теста
        /// </summary>
        public ICommand StartStopOptimisation { get; }
        #endregion

        #region Terminal names
        /// <summary>
        /// Список ID терминалов
        /// </summary>
        public IEnumerable<string> Terminals => model.TerminalNames;
        /// <summary>
        /// Выбранный индекс терминала
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
        /// Список имен оптимизаторов
        /// </summary>
        public IEnumerable<string> Optimisers => model.OptimisatorNames;
        /// <summary>
        /// Выбранный оптимизатор
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
                // Обновляем оптимизатор
                if (model.ChangeOptimiser(Optimisers.ElementAt(value)))
                    _selectedOptimiserIndex = value;
                else
                    System.Windows.MessageBox.Show("Can`t change optimiser");
            }
        }
        /// <summary>
        /// Коллбек показа графического интерфейса оптимизатора
        /// </summary>
        public ICommand ShowOptimiserGUI { get; }
        /// <summary>
        /// Режим типа записи в файл
        /// </summary>
        public string FileWritingMode { get; set; }
        /// <summary>
        /// Префикс директории
        /// </summary>
        public string DirPrefix { get; set; } = "";
        #endregion

        /// <summary>
        /// Хранитель параметров робота
        /// </summary>
        public ObservableCollection<BotParamsData> BotParams { get; } = new ObservableCollection<BotParamsData>();

        #region DT border
        /// <summary>
        /// Коллекция дат оптимизации
        /// </summary>
        public ObservableCollection<DateBordersItem> DateBorders { get; } = new ObservableCollection<DateBordersItem>();
        /// <summary>
        /// Добавление границ дат
        /// </summary>
        public ICommand AddDateBorder { get; }
        /// <summary>
        /// Коллбек на загрузку или же созранение дат оптимизации
        /// </summary>
        /// <param name="o"></param>
        private void _SaveOrLoadDates(object o)
        {
            if (DateBorders.Count > 0)
                SaveDates();
            else
                LoadDates();
        }
        /// <summary>
        /// Коллбек созранения датоптимизации
        /// </summary>
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
        /// <summary>
        /// Коллбек загрузки дат оптимизации
        /// </summary>
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
        /// <summary>
        /// Коллбек созранения или же загрузки дат оптимизации
        /// </summary>
        public ICommand SaveOrLoadDates { get; }
        /// <summary>
        /// Дата начала периода
        /// </summary>
        public DateTime DateFrom { get; set; } = DateTime.Now;
        /// <summary>
        ///Дата окончания периода
        /// </summary>
        public DateTime DateTill { get; set; } = DateTime.Now;
        /// <summary>
        /// Тип диаппазона дат
        /// </summary>
        public IEnumerable<string> DateBorderTypes { get; }
        /// <summary>
        /// Выбранный тип диаппазона
        /// </summary>
        public int SelectedDateBorderType { get; set; } = 0;
        /// <summary>
        /// Коллбек добавления диаппазона дат
        /// </summary>
        /// <param name="o"></param>
        private void _AddDateBorder(object o)
        {
            _AddDateBorder(DateFrom, DateTill, GetEnum<OptimisationType>(DateBorderTypes.ElementAt(SelectedDateBorderType)));
        }
        void _AddDateBorder(DateTime From, DateTime Till, OptimisationType DateBorderType)
        {
            try
            {
                DateBorders border = new DateBorders(From, Till);
                if (!DateBorders.Where(x => x.BorderType == DateBorderType).Any(y => y.DateBorders == border))
                {
                    DateBorders.Add(new DateBordersItem(border, _DeleteDateBorder, DateBorderType));
                }
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message);
            }
        }
        /// <summary>
        /// Удаление выбранной границы дат
        /// </summary>
        /// <param name="item">Deleting item</param>
        private void _DeleteDateBorder(DateBordersItem item)
        {
            DateBorders.Remove(item);
        }

        public ICommand AutosetDateBorder { get; }
        private void Model_DateBorders(List<KeyValuePair<OptimisationType, DateTime[]>> obj)
        {
            DateBorders.Clear();
            foreach (var item in obj)
                _AddDateBorder(item.Value[0], item.Value[1], item.Key);
        }

        public ICommand ClearDateBorders { get; }

        private readonly SubFormKeeper autoFillInDateBorders;
        #endregion

        #region Results data
        /// <summary>
        /// Коллбек для кнопки загрузки диаппазона дат
        /// </summary>
        public ICommand LoadResults { get; }
        public ICommand ClearLoadedResults { get; }

        #region Selected saved optimisation
        /// <summary>
        /// Имя выбранного оптимизатора
        /// </summary>
        public string SelectedOptimisation { get; set; }
        /// <summary>
        /// Доступные оптимизаторы
        /// </summary>
        public IEnumerable<string> SelectedOptimisationNames => model.SavedOptimisations;
        #endregion

        /// <summary>
        /// Запуск теста по событию двойного клика на таблице с оптимизациями
        /// </summary>
        /// <param name="results">РЕзуьтаты оптимизаций</param>
        /// <param name="ind">Выбранный индекс</param>
        private void _StartTest(ObservableCollection<ReportItem> results, int ind)
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
                    BotParams = ((OptimisationResult)results[ind]).report.BotParams.Select(x => new ParamsItem { Variable = x.Key, Value = x.Value }).ToList()
                };

                model.StartTest(optimiserInputData);
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message);
            }
        }
        /// <summary>
        /// Запуск теста из таблицы с форвардными тестами
        /// </summary>
        public ICommand StartTestForward { get; }
        /// <summary>
        ///Запуск теста из таблицы с историческими тестами
        /// </summary>
        public ICommand StartTestHistory { get; }
        /// <summary>
        /// Запуск теста из таблицы с результатамиоптимизации
        /// </summary>
        public ICommand StartTestReport { get; }

        /// <summary>
        /// Созранение данных в (*.csv) файл
        /// </summary>
        /// <param name="o">Идентификтор файла (setting from view)</param>
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
        /// Сохранение данных в (*.csv) файл
        /// </summary>
        public ICommand SaveToCsv { get; }

        #region All optimisations table

        private void FillInDataForReportItem(OptimisationResult item)
        {
            FillInBotParams(item);
            FillInDailyPL(item);
            FillInMaxPLDD(item);
        }

        /// <summary>
        /// Заполнение параметров робота
        /// </summary>
        /// <param name="result">результаты</param>
        private void FillInBotParams(OptimisationResult result)
        {
            SelectedBotParams.Clear();

            if (result.report.BotParams != null)
            {
                foreach (var item in result.report.BotParams)
                    SelectedBotParams.Add(item);
            }

            ReplaceBotFixedParam("Symbol", result.report.Symbol);
            ReplaceBotFixedParam("TF", ((ENUM_Timeframes)result.report.TF).ToString());
            TestFrom = (result.report.DateBorders != null ? result.report.DateBorders.From : DateTime.Now);
            TestTill = (result.report.DateBorders != null ? result.report.DateBorders.Till : DateTime.Now);
            OnPropertyChanged("TestFrom");
            OnPropertyChanged("TestTill");
        }
        /// <summary>
        /// Заполнение параметров дневного PL
        /// </summary>
        /// <param name="result">results</param>
        private void FillInDailyPL(OptimisationResult result)
        {
            KeyValuePair<DayOfWeek, DailyPLItem> GetItem(DayOfWeek day)
            {
                if (result.report.OptimisationCoefficients.TradingDays != null)
                    return new KeyValuePair<DayOfWeek, DailyPLItem>(day, (result.report.OptimisationCoefficients.TradingDays.Count > 0 ?
                                                                new DailyPLItem(result.report.OptimisationCoefficients.TradingDays[day]) :
                                                                null));
                else
                    return new KeyValuePair<DayOfWeek, DailyPLItem>(day, null);
            }

            TradingDays[0] = GetItem(DayOfWeek.Monday);
            TradingDays[1] = GetItem(DayOfWeek.Thursday);
            TradingDays[2] = GetItem(DayOfWeek.Wednesday);
            TradingDays[3] = GetItem(DayOfWeek.Thursday);
            TradingDays[4] = GetItem(DayOfWeek.Friday);
            OnPropertyChanged("TradingDays");
        }
        /// <summary>
        /// Заполнение параметров дневного PL
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
        /// Хранитель параметров робота
        /// </summary>
        public ObservableCollection<KeyValuePair<string, string>> SelectedBotParams { get; } =
            new ObservableCollection<KeyValuePair<string, string>>();
        /// <summary>
        /// Хранитель PL по дням
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
        /// Хранитель PL по дням
        /// </summary>
        public ObservableCollection<KeyValuePair<string, KeyValuePair<string, string>>> MaxPLDD { get; } =
            new ObservableCollection<KeyValuePair<string, KeyValuePair<string, string>>>
        {
            new KeyValuePair<string, KeyValuePair<string,string>>("Value",new KeyValuePair<string,string>(null,null)),
            new KeyValuePair<string, KeyValuePair<string,string>>("Total trades",new KeyValuePair<string,string>(null,null)),
            new KeyValuePair<string, KeyValuePair<string,string>>("Consecutive trades",new KeyValuePair<string,string>(null,null))
        };
        /// <summary>
        /// Выбранный элемент отчета
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

                    FillInDataForReportItem(item);
                }
            }
        }
        /// <summary>
        /// Выбранный форвард проход
        /// </summary>
        private int _selectedForwardItem;
        public int SelectedForwardItem
        {
            get => _selectedForwardItem;
            set
            {
                _selectedForwardItem = value;
                if (value > -1)
                    FillInDataForReportItem(model.ForwardOptimisations[value]);
            }
        }
        /// <summary>
        /// Выбранный исторический проход
        /// </summary>
        private int _selectedHistoryItem;
        public int SelectedHistoryItem
        {
            get => _selectedHistoryItem;
            set
            {
                _selectedHistoryItem = value;
                if (value > -1)
                    FillInDataForReportItem(model.HistoryOptimisations[value]);
            }
        }

        /// <summary>
        /// Сортировка отчетов
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
        /// Фильтрация отчетов
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
        /// Выбранная граница дат для отчета
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
        /// Границы дат всех произведенных оптимизаций
        /// </summary>
        public ObservableCollection<DateBorders> ReportDateBorders { get; } = new ObservableCollection<DateBorders>();
        /// <summary>
        /// Выбранный отчет для "SelectedReportDateBorder"
        /// </summary>
        public ObservableCollection<ReportItem> AllOptimisations { get; } = new ObservableCollection<ReportItem>();
        #endregion

        #region Forward and History optimisations table
        /// <summary>
        /// Выбранные форвардные тесты
        /// </summary>
        public ObservableCollection<ReportItem> ForwardOptimisations => model.ForwardOptimisations;
        /// <summary>
        /// Выбранные исторические тесты
        /// </summary>
        public ObservableCollection<ReportItem> HistoryOptimisations => model.HistoryOptimisations;

        #endregion

        #region  OptimiserSettingsForResults
        /// <summary>
        /// Зафиксированные параметры тестера для результатов оптимизации
        /// </summary>
        public ObservableCollection<KeyValuePair<string, string>> OptimiserSettingsForResults_fixed { get; }
        /// <summary>
        /// Замена фиксированных параметров робота
        /// </summary>
        /// <param name="key">Имя параметра</param>
        /// <param name="value">Значение параметра</param>
        void ReplaceBotFixedParam(string key, string value)
        {
            KeyValuePair<string, string> item = OptimiserSettingsForResults_fixed.First(x => x.Key == key);
            int ind = OptimiserSettingsForResults_fixed.IndexOf(item);
            OptimiserSettingsForResults_fixed[ind] = new KeyValuePair<string, string>(key, value);
        }
        /// <summary>
        /// Изменяемые параметры робота для реста для вкладки с результатами
        /// </summary>
        public List<OptimiserSetting> OptimiserSettingsForResults_changing { get; }
        /// <summary>
        /// Дата начала теста
        /// </summary>
        public DateTime TestFrom { get; set; } = DateTime.Now;
        /// <summary>
        /// Дата завершения теста
        /// </summary>
        public DateTime TestTill { get; set; } = DateTime.Now;
        #endregion

        #endregion

        #region EnumToString StringToEnum
        /// <summary>
        /// Преобразование имени перечисления в строку
        /// </summary>
        /// <typeparam name="T">Тип перечисления</typeparam>
        /// <param name="param">Одно из значений перечисления</param>
        /// <returns>Строковое значение перечисления</returns>
        private T GetEnum<T>(string param)
        {
            return (T)Enum.Parse(typeof(T), param.Replace(" ", "_"));
        }
        /// <summary>
        /// Преобразование всех значений перечисления в коллекцию строк
        /// </summary>
        /// <typeparam name="T">Тип перечисления</typeparam>
        /// <returns>Список строкового значения перечислений</returns>
        private IEnumerable<string> GetEnumNames<T>()
        {
            return Enum.GetNames(typeof(T)).Select(x => x.Replace("_", " "));
        }
        #endregion
    }

    #region Entities for GUI
    class SubFormKeeper
    {
        public SubFormKeeper(Func<Window> createWindow, Action<Window> subscribe_events = null, Action<Window> unSubscribe_events = null)
        {
            creator = createWindow;
            Subscribe_events = subscribe_events;
            UnSubscribe_events = unSubscribe_events;
        }
        ~SubFormKeeper()
        {
            Close();
        }

        private readonly Func<Window> creator;
        private readonly Action<Window> Subscribe_events;
        private readonly Action<Window> UnSubscribe_events;

        private Window window = null;
        public void Open()
        {
            if (window == null)
            {
                window = creator();
                if (!window.IsActive)
                    window.Show();

                window.Closed += Window_Closed;
                Subscribe_events?.Invoke(window);
            }
            else
                window.Activate();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            window.Closed -= Window_Closed;
            UnSubscribe_events?.Invoke(window);
            window = null;
        }
        public void Close()
        {
            if (window != null)
                window.Close();
        }
    }

    /// <summary>
    /// Класс - обертка элемента отчета (для графического интервейса)
    /// </summary>
    class ReportItem
    {
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="item">Элемент</param>
        public ReportItem(OptimisationResult item)
        {
            result = item;
        }
        /// <summary>
        /// Эллемент отчета
        /// </summary>
        private readonly OptimisationResult result;

        public static implicit operator ReportItem(OptimisationResult item) { return new ReportItem(item); }
        public static implicit operator OptimisationResult(ReportItem data) { return data.result; }

        /// <summary>
        /// Дата "С"
        /// </summary>
        public DateTime From => result.report.DateBorders.From;
        /// <summary>
        /// Дата "По"
        /// </summary>
        public DateTime Till => result.report.DateBorders.Till;
        /// <summary>
        /// Показатель по которому производится сортировка
        /// </summary>
        public double SortBy => result.SortBy;
        /// <summary>
        /// Payoff
        /// </summary>
        public double Payoff => result.report.OptimisationCoefficients.Payoff;
        /// <summary>
        /// Profit factor
        /// </summary>
        public double ProfitFactor => result.report.OptimisationCoefficients.ProfitFactor;
        /// <summary>
        /// Average profit factor
        /// </summary>
        public double AverageProfitFactor => result.report.OptimisationCoefficients.AverageProfitFactor;
        /// <summary>
        /// Recovery factor
        /// </summary>
        public double RecoveryFactor => result.report.OptimisationCoefficients.RecoveryFactor;
        /// <summary>
        /// Average recovery factor
        /// </summary>
        public double AverageRecoveryFactor => result.report.OptimisationCoefficients.AverageRecoveryFactor;
        /// <summary>
        /// PL
        /// </summary>
        public double PL => result.report.OptimisationCoefficients.PL;
        /// <summary>
        /// DD
        /// </summary>
        public double DD => result.report.OptimisationCoefficients.DD;
        /// <summary>
        /// Altman Z score
        /// </summary>
        public double AltmanZScore => result.report.OptimisationCoefficients.AltmanZScore;
        /// <summary>
        /// Total trades
        /// </summary>
        public int TotalTrades => result.report.OptimisationCoefficients.TotalTrades;
        /// <summary>
        /// VaR 90
        /// </summary>
        public double VaR90 => result.report.OptimisationCoefficients.VaR.Q_90;
        /// <summary>
        /// VaR 95
        /// </summary>
        public double VaR95 => result.report.OptimisationCoefficients.VaR.Q_95;
        /// <summary>
        /// VaR 99
        /// </summary>
        public double VaR99 => result.report.OptimisationCoefficients.VaR.Q_99;
        /// <summary>
        /// Mx
        /// </summary>
        public double Mx => result.report.OptimisationCoefficients.VaR.Mx;
        /// <summary>
        /// Std
        /// </summary>
        public double Std => result.report.OptimisationCoefficients.VaR.Std;
        /// <summary>
        /// Custom coeffitiant
        /// </summary>
        public double CustomCoef => result.report.OptimisationCoefficients.Custom;
    }

    /// <summary>
    /// Класс - обертка, хрянящий границы дат (для графического интервейса)
    /// </summary>
    class DateBordersItem
    {
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="dateBorders">Диаппозон дат</param>
        /// <param name="delete">Коллбек удаляющзий текущий эллемент из списка</param>
        /// <param name="borderType">history/forward тип границы</param>
        public DateBordersItem(DateBorders dateBorders, Action<DateBordersItem> delete, OptimisationType borderType)
        {
            DateBorders = dateBorders;
            BorderType = borderType;
            Delete = new RelayCommand((object o) => delete(this));
        }
        /// <summary>
        /// Хранитель границ дат
        /// </summary>
        public DateBorders DateBorders { get; }
        /// <summary>
        /// Дата начала
        /// </summary>
        public DateTime From => DateBorders.From;
        /// <summary>
        /// Дата завершения
        /// </summary>
        public DateTime Till => DateBorders.Till;
        /// <summary>
        /// Тип оптимизации (исторический / форвардный)
        /// </summary>
        public OptimisationType BorderType { get; }
        /// <summary>
        /// Коллбек удаляющзий текущий элемент
        /// </summary>
        public ICommand Delete { get; }
    }

    /// <summary>
    /// Класс обертка для enum SortBy (для графического интервейса)
    /// </summary>
    class SorterItem
    {
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="sorter">Параметр сортировки</param>
        /// <param name="deleteItem">Коллбек удаления из списка</param>
        public SorterItem(SortBy sorter, Action<object> deleteItem)
        {
            Sorter = sorter;
            Delete = new RelayCommand((object o) => deleteItem(this));
        }
        /// <summary>
        /// Элемент сортировки
        /// </summary>
        public SortBy Sorter { get; }
        /// <summary>
        /// Коллбек удаления элемента
        /// </summary>
        public ICommand Delete { get; }
    }

    /// <summary>
    /// Класс обертка для enum SortBy и флагов CompareType (для графического интервейса)
    /// </summary>
    class FilterItem : SorterItem
    {
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="sorter">Элемент сортировки</param>
        /// <param name="deleteItem">Коллбек удаления</param>
        /// <param name="compareType">Способ сопоставления</param>
        /// <param name="border">Сопоставляемая величина</param>
        public FilterItem(SortBy sorter, Action<object> deleteItem,
                          CompareType compareType, double border) : base(sorter, deleteItem)
        {
            CompareType = compareType;
            Border = border;
        }
        /// <summary>
        /// Тип сопоставления
        /// </summary>
        public CompareType CompareType { get; }
        /// <summary>
        /// Сопоставляемое значение
        /// </summary>
        public double Border { get; }
    }

    /// <summary>
    /// Класс обертка для параметров робота (для графического интерфейса)
    /// </summary>
    class BotParamsData
    {
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="param">bot param item</param>
        public BotParamsData(ParamsItem param)
        {
            this.param = param;
        }
        /// <summary>
        /// Параметр робота
        /// </summary>
        private ParamsItem param;
        /// <summary>
        /// Геттер для параметров робота
        /// </summary>
        public ParamsItem Param => param;
        /// <summary>
        /// Признак оптимизируемого параметра
        /// </summary>
        public bool IsOptimize
        {
            get => param.IsOptimize;
            set => param.IsOptimize = value;
        }
        /// <summary>
        /// Имя параметра
        /// </summary>
        public string Vriable
        {
            get => param.Variable;
        }
        /// <summary>
        /// Значение параметра
        /// </summary>
        public string Value
        {
            get => param.Value;
            set => param.Value = value;
        }
        /// <summary>
        /// Начальное значение оптимизации 
        /// </summary>
        public string Start
        {
            get => param.Start;
            set => param.Start = value;
        }
        /// <summary>
        /// Шаг перебора оптимизацией
        /// </summary>
        public string Step
        {
            get => param.Step;
            set => param.Step = value;
        }
        /// <summary>
        /// Завершающее хначение оптимизации
        /// </summary>
        public string Stop
        {
            get => param.Stop;
            set => param.Stop = value;
        }
    }

    /// <summary>
    /// Класс обертка для значения дневного PL (для оптимизации)
    /// </summary>
    class DailyPLItem
    {
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="dailyData">daily pl data</param>
        public DailyPLItem(DailyData dailyData)
        {
            this.dailyData = dailyData;
        }

        /// <summary>
        /// структура дневного PL
        /// </summary>
        private readonly DailyData dailyData;
        /// <summary>
        /// Средняя дневная присыль
        /// </summary>
        public double DailyProfit => dailyData.Profit.Value;
        /// <summary>
        /// Количество трейдов за день
        /// </summary>
        public int DailyProfitTrades => dailyData.Profit.Trades;
        /// <summary>
        /// Средние убытки за день 
        /// </summary>
        public double DailyDD => dailyData.DD.Value;
        /// <summary>
        /// Количество убытков за день
        /// </summary>
        public int DailyDDTrades => dailyData.DD.Trades;
    }

    /// <summary>
    /// Класс обертка для изменяемых параметров оптимизатора
    /// </summary>
    class OptimiserSetting : INotifyPropertyChanged
    {
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="Name">Имя робота</param>
        /// <param name="Params">Диаппазон изменения параметра</param>
        /// <param name="SelectionChanged">Коллбек для выпадающего списка - изменение выбранного элемента</param>
        public OptimiserSetting(string Name, IEnumerable<string> Params, Action<string> SelectionChanged = null)
        {
            this.Name = Name;
            this.Params = Params;
            this.SelectionChanged = SelectionChanged;
        }
        /// <summary>
        /// Коллбек вызываемый при смены выбранного параметра
        /// </summary>
        private readonly Action<string> SelectionChanged;
        /// <summary>
        /// Имя параметра
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// Возможные значения для выбранного параметра
        /// </summary>
        public IEnumerable<string> Params { get; private set; }
        /// <summary>
        /// Установка возможных значений для выбранного параметра
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
        /// <summary>
        /// Выбранное значение параметра
        /// </summary>
        public string SelectedParam => (Params.Count() > 0 ? Params.ElementAt(SelectedIndex) : null);
        /// <summary>
        /// Индекс выбранного параметра
        /// </summary>
        private int _selectedIndex = 0;
        /// <summary>
        /// Событие обновления свойства
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// Геттер длявыбранного индекса
        /// </summary>
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
                // Асинхронный запуск события изменения выбранного ранее параметра
                SelectionChanged?.BeginInvoke(Params.Count() > 0 ? Params.ElementAt(value) : null, null, null);
            }
        }
        #endregion
    }
    #endregion
}
