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
    /// Создатель оптимизаторов
    /// </summary>
    class SimpleOptimiserManagerCreator : OptimiserCreator
    {
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="workingDirectory">Класс описывающий структуру рабочей директории</param>
        public SimpleOptimiserManagerCreator() : base(Name)
        { }
        /// <summary>
        /// Метод порождающий оптимизатор
        /// </summary>
        /// <param name="terminalManager">Выбранный терминалл</param>
        /// <returns>Оптимизатор</returns>
        public override IOptimiser Create(TerminalManager terminalManager)
        {
            return new Manager(new DirectoryManagers.WorkingDirectory())
            {
                TerminalManager = terminalManager
            };
        }
        /// <summary>
        /// Имя оптимизатора
        /// </summary>
        public new static string Name => "Simple forward optimiser";
    }

    /// <summary>
    /// Single tone Класс - модель в структуре MVVM для графического интерфейса данного оптимизатора
    /// </summary>
    class SimpleOptimiserM
    {
        /// <summary>
        /// Статическое поле храняще инстанцированный объект данного класса
        /// </summary>
        private static SimpleOptimiserM instance;
        /// <summary>
        /// Делаем конструктор приватным что бы нельзя было до него достучаться извне класса
        /// </summary>
        SimpleOptimiserM() { }
        /// <summary>
        /// Статический инстанцирующий метод
        /// </summary>
        /// <returns></returns>
        public static SimpleOptimiserM Instance()
        {
            // Если не было еще не одного инстанцирования - то инстанцируем
            if (instance == null)
                instance = new SimpleOptimiserM();

            return instance;
        }

        /// <summary>
        /// Настройка - тестировать на тиках ?
        /// </summary>
        public bool IsTickTest { get; set; } = true;
        /// <summary>
        /// Настройка - Заменять реальные даты прозодов оптимизации на указанные в настройках ?
        /// </summary>
        public bool ReplaceDates { get; set; } = false;
        /// <summary>
        /// Настройка - Использовать ли иной сдвиг для тикового теста ?
        /// </summary>
        public bool IsDifferentShiftForTicks { get; set; } = false;
        /// <summary>
        /// Новые параметры для сдвигов и комиссий
        /// </summary>
        public ObservableCollection<ComissionKeeper> NewShiftAndComission = new ObservableCollection<ComissionKeeper>();
    }

    /// <summary>
    /// Класс - View Model в структуре MVVM для графического интерфейса данного оптимизатора
    /// </summary>
    class SimpleOptimiserVM : INotifyPropertyChanged
    {
        /// <summary>
        /// Конструктор
        /// </summary>
        public SimpleOptimiserVM()
        {
            // Коллбек на кнопку добавление иного проскальзования и комиссии
            Add = new RelayCommand((object o) =>
            {
                // Если уже добавлено жто имя параметра, то не чего не делаем
                if (NewShiftAndComission.Any(x => x.Name == ShiftAndComissionName))
                    return;

                // Создаем экземпляр объекта описывающего заменяемый параметр и передаем коллбек для удаления заменяемого параметра из списка
                ComissionKeeper _item = new ComissionKeeper(ShiftAndComissionName, ShiftAndComission, (ComissionKeeper item) =>
                {
                    NewShiftAndComission.Remove(item);
                });

                // Добавлем новый параметр
                NewShiftAndComission.Add(_item);
            });
        }
        /// <summary>
        /// Событие изменения какого либо из свойств
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// Инстанцируем модель данных
        /// </summary>
        private readonly SimpleOptimiserM model = SimpleOptimiserM.Instance();

        /// <summary>
        /// Геттер / сеттер для параметра IsTickTest в моделе данных
        /// </summary>
        public bool IsTickTest
        {
            get => model.IsTickTest;
            set => model.IsTickTest = value;
        }
        /// <summary>
        /// Геттер / сеттер для параметра ReplaceDates в моделе данных
        /// </summary>
        public bool ReplaceDates
        {
            get => model.ReplaceDates;
            set => model.ReplaceDates = value;
        }
        /// <summary>
        /// Геттер / сеттер для параметра IsDifferentShiftForTicks в моделе данных
        /// </summary
        public bool IsDifferentShiftForTicks
        {
            get => model.IsDifferentShiftForTicks;
            set => model.IsDifferentShiftForTicks = value;
        }

        /// <summary>
        /// Геттер / Сеттер для имени сдвига цены / комиссии 
        /// </summary>
        public string ShiftAndComissionName { get; set; }
        /// <summary>
        /// Геттер / Сеттер для нового параметрао сдвига цены / комиссии 
        /// </summary>
        public double ShiftAndComission { get; set; } = 0;
        /// <summary>
        /// Список новых сдвигов и комиссий
        /// </summary>
        public ObservableCollection<ComissionKeeper> NewShiftAndComission => model.NewShiftAndComission;
        /// <summary>
        /// Коллбек добавления нового сдвига / комиссии
        /// </summary>
        public ICommand Add { get; }
    }

    /// <summary>
    /// Класс - хранитель конкретной выбранной для замены параметров комиссии или же проскальзования
    /// </summary>
    class ComissionKeeper
    {
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="Name">Имя параметра</param>
        /// <param name="Value">Значение параметра</param>
        /// <param name="action">Коллбек удаления данного эллемента из списка</param>
        public ComissionKeeper(string Name, double Value, Action<ComissionKeeper> action)
        {
            this.Name = Name;
            this.Value = Value;

            // Добавление коллбека
            Delete = new RelayCommand((object o) =>
            {
                action(this);
            });
        }

        /// <summary>
        /// Имя параметра
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// Значение параметра
        /// </summary>
        public double Value { get; }
        /// <summary>
        /// Коллбек удаления параметра из списка параметров предоставленных на замену во время тикового теста
        /// </summary>
        public ICommand Delete { get; }
    }

    /// <summary>
    /// Класс оптимизатора
    /// </summary>
    class Manager : IOptimiser
    {
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="workingDirectory">Менеджер рабочих директорий</param>
        public Manager(WorkingDirectory workingDirectory)
        {
            this.workingDirectory = workingDirectory;
        }
        ~Manager()
        {
            CloseSettingsWindow();
        }
        /// <summary>
        /// Менеджер рабочих директорий
        /// </summary>
        private readonly WorkingDirectory workingDirectory;

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

        /// <summary>
        /// Статус процесса оптимизаци
        /// </summary>
        public bool IsOptimisationInProcess { get; protected set; } = false;

        /// <summary>
        /// Имя оптимизаторы
        /// </summary>
        public string Name => SimpleOptimiserManagerCreator.Name;

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
        public string OptimiserWorkingDirectory { get; protected set; }
        #endregion
        /// <summary>
        /// Событие окончания процесса оптимизации
        /// </summary>
        public event Action<IOptimiser> OptimisationProcessFinished;
        /// <summary>
        /// Событие изменения прогресс бара в основном графическом изтерфейсе из оптимизатора
        /// </summary>
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

        /// <summary>
        /// Класс Модель менеджера оптимизаций 
        /// </summary>
        readonly SimpleOptimiserM optimiserSettings = SimpleOptimiserM.Instance();
        /// <summary>
        /// Окно с настройкамит оптимизатора
        /// </summary>
        System.Windows.Window settingsGUI = null;

        /// <summary>
        /// Метод открывающий окно с настройками оптимизатора
        /// </summary>
        public virtual void LoadSettingsWindow()
        {
            if (settingsGUI != null)
                return;

            settingsGUI = new SimpleOptimiserSettings();
            settingsGUI.Closed += (object sender, EventArgs e) => { settingsGUI = null; };
            settingsGUI.Show();
        }

        public void CloseSettingsWindow()
        {
            if (settingsGUI != null)
                settingsGUI.Close();
        }

        /// <summary>
        /// Запуск процесса оптимизации
        /// </summary>
        /// <param name="optimiserInputData">Настройки оптимизатора и робота</param>
        /// <param name="PathToResultsFile">Путь к файлу с отчетом</param>
        public virtual void Start(OptimiserInputData optimiserInputData,
                                  string PathToResultsFile, string dirPrefix)
        {
            OptimiserWorkingDirectory = null;

            // Проверить Проверить доступность терминала для запуска оптимизации
            if (IsOptimisationInProcess || TerminalManager.IsActive)
                return;

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

            // Получение пути к файлу (*.set) с параметрами эксперта
            string setFile = new FileInfo(Path.Combine(TerminalManager.TerminalChangeableDirectory
                             .GetDirectory("MQL5")
                             .GetDirectory("Profiles")
                             .GetDirectory("Tester")
                             .FullName,
                             $"{Path.GetFileNameWithoutExtension(optimiserInputData.RelativePathToBot)}.set"))
                             .FullName;

            // Выбор шага для прогресс бара
            double step = 100.0 / optimiserInputData.HistoryBorders.Count;
            // Счетчик итераций прогресс бара
            int i = 1;

            // Создание (*set) файла и созранение в него настроек робота
            #region Create (*.set) file
            SetFileManager setFileManager = new SetFileManager(setFile, true)
            {
                Params = optimiserInputData.BotParams
            };
            setFileManager.SaveParams();
            #endregion

            // Сопоставление установленных и реальных границ дат 
            Dictionary<DateBorders, DateBorders> borders = new Dictionary<DateBorders, DateBorders>();

            // Цикл по историческим датам оптимизаций
            foreach (var item in optimiserInputData.HistoryBorders)
            {
                if (!IsOptimisationInProcess)
                    return;

                // Обновление прогресс бара
                ProcessStatus("Optimisation", step * i);
                i++;

                // Конфигурирование терминала перед запуском
                TerminalManager.Config = GetConfig(optimiserInputData, setFileManager, item);
                if (optimiserInputData.BotParams.Any(x => x.Variable == Fixed_Input_Settings.Params[InputParamName.CloseTerminalFromBot]))
                    TerminalManager.Config.Tester.ShutdownTerminal = false;
                // Запуск терминала и ожидание завершения его работы
                if (TerminalManager.Run())
                {
                    // ожидание завершения работы терминала
                    TerminalManager.WaitForStop();

                    // Чтение файла с отчетом и удаление его после прочтения
                    List<OptimisationResult> results = new List<OptimisationResult>();
                    FillInData(results, PathToResultsFile);
                    // Если не был сформирован файл, то переходим к следующей итеррации
                    if (results.Count == 0)
                        continue;
                    // Если в отчете с результатами оптимизаций было прооптимизировано разное количество временных интервалов 
                    // хотя был запуск лишь для одного временного интервала - то выкидываем ошибку
                    if (results.Select(x => x.report.DateBorders).Distinct().Count() > 1)
                    {
                        //  throw new Exception("There are more than one date borders inside report file");

                        for (int c = 0; c < results.Count; c++)
                        {
                            var result_item = results[c];
                            result_item.report.DateBorders = item;
                            results[c] = result_item;
                        }
                    }

                    // Добавление отчета
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
                    // Заполнение словаря сопоставлений дат
                    borders.Add(item, results[0].report.DateBorders);
                }
            }

            // Установка статуса и сброс прогресс бара, затем запуск тестов
            ProcessStatus("Start tests", 0);
            Tests(borders, optimiserInputData, setFile, PathToResultsFile);

            // Переключение статуса оптимизаций на завершенный и вызов соответствующего события
            IsOptimisationInProcess = false;
            OptimisationProcessFinished(this);
        }
        /// <summary>
        /// Запуск форвардных и исторических тестов
        /// </summary>
        /// <param name="HistoryToRealHistory">
        /// Сопоставление переданных исторических дат тем что были получены путем оптимизаций по переданным датам
        /// </param>
        /// <param name="optimiserInputData">Настройки оптимизатора и робота</param>
        /// <param name="setFile">Имя (*set) файла с настройками</param>
        /// <param name="pathToFile">Путь к файлу с результатами оптимиазций</param>
        protected void Tests(Dictionary<DateBorders, DateBorders> HistoryToRealHistory,
                             OptimiserInputData optimiserInputData,
                             string setFile, string pathToFile)
        {
            // Сопоставление исторических границ - форвардным
            Dictionary<DateBorders, DateBorders> HistoryToForward =
                DateBorders.CompareHistoryToForward(optimiserInputData.HistoryBorders, optimiserInputData.ForwardBorders);
            optimiserInputData.HistoryBorders.ForEach(x =>
            {
                if (!HistoryToRealHistory.ContainsKey(x))
                    HistoryToForward.Remove(x);
            });

            // Вложенная функция запускающая тесты
            bool Test(DateBorders Border, List<OptimisationResult> optimisationResults, List<OptimisationResult> results, out Dictionary<string, string> botParams)
            {
                botParams = new Dictionary<string, string>();
                if (Border == null)
                    return false;

                // Фильтрация данных оптимизации
                if (optimiserInputData.CompareData != null &&
                    optimiserInputData.CompareData.Count > 0)
                {
                    optimisationResults = optimisationResults.FiltreOptimisations(optimiserInputData.CompareData).ToList();
                }
                // Сортировка данных оптимизации и возврат из метода если они пусты (могут все отсеяться на этапе фильтрации)
                if (optimisationResults != null && optimisationResults.Count > 0)
                    optimisationResults = optimisationResults.SortOptimisations(OrderBy.Descending, optimiserInputData.SortingFlags).ToList();
                else
                    return false;

                // Получение лучшего результата оптимизации
                OptimisationResult result = optimisationResults.First();
                botParams = result.report.BotParams;

                // Установка параметров робота для тестов
                for (int i = 0; i < optimiserInputData.BotParams.Count; i++)
                {

                    if (!IsOptimisationInProcess)
                        return false;
                    // Выбор параметра робота
                    var paramItem = optimiserInputData.BotParams[i];

                    // Установка значения параметра в случае если таковой присутствует в файле с отчетом оптимизации
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

                // Сохранение параметров роботов перед тем как запустиь оптимизации
                SetFileManager setFileManager = new SetFileManager(setFile, false)
                {
                    Params = optimiserInputData.BotParams
                };
                setFileManager.SaveParams();

                // Получение корректного файла конфигураций
                Config config = GetConfig(optimiserInputData, setFileManager, Border);
                if (optimiserSettings.IsTickTest)
                    config.Tester.Model = ENUM_Model.Every_tick_based_on_real_ticks;
                config.Tester.Optimization = ENUM_OptimisationMode.Disabled;

                // Конфигурация терминала и запуск
                TerminalManager.Config = config;
                if (TerminalManager.Run())
                    TerminalManager.WaitForStop();

                return true;
            }
            // Вложенная функция получения результатов оптимизации для выбранного диаппазона дат
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
            // Вложенная функция - запускает цикл тестов
            void RunTestLoop(List<OptimisationResult> results, bool isForward)
            {
                int n = 1;
                double step = 100.0 / HistoryToForward.Count;

                foreach (var item in HistoryToForward)
                {

                    if (!IsOptimisationInProcess)
                        return;
                    ProcessStatus((isForward ? "Forward tests" : "History tests"), step * n);
                    n++;

                    List<OptimisationResult> optimisationResults = GetOptimisationResult(item.Key);

                    if (!Test((isForward ? item.Value : item.Key), optimisationResults, results, out Dictionary<string, string> botParams))
                        continue;

                    List<OptimisationResult> _results = new List<OptimisationResult>();
                    FillInData(_results, pathToFile);

                    if (_results.Count == 0)
                        continue;
                    var data = _results[0];
                    // В новом варианте выгрузке результатов оптимизации может не быть параметров робота, 
                    // по этому они задаются здесь из тех что были отобраны 
                    if (data.report.BotParams.Count == 0)
                        data.report.BotParams = botParams;

                    if (optimiserSettings.ReplaceDates)
                        data.report.DateBorders = (isForward ? item.Value : item.Key);
                    results.Add(data);
                }
            }

            // Запуск тестов для исторических и форвардных проходов
            RunTestLoop(HistoryOptimisations, false);
            RunTestLoop(ForwardOptimisations, true);
        }
        /// <summary>
        /// Генерация файла конфигурации терминала
        /// </summary>
        /// <param name="optimiserInputData">Настройки оптимизатора</param>
        /// <param name="setFileManager">Менеджер (*.set) файлов</param>
        /// <param name="dateBorders">Выбранный диаппазон дат</param>
        /// <returns>Сконфигурированный конфигурационный файл</returns>
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
        /// Прочтение отчета оптимизаций и сохранение его в переданный массив
        /// </summary>
        /// <param name="data">Хранитель результатов оптимизаций</param>
        /// <param name="pathToReportFile">Путь к файлу с отчетом</param>
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
    }
}
