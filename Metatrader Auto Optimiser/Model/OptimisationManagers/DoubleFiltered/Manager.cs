using Metatrader_Auto_Optimiser.Model.FileReaders;
using Metatrader_Auto_Optimiser.Model.Terminal;
using ReportManager;
using System;
using System.Collections.Generic;

namespace Metatrader_Auto_Optimiser.Model.OptimisationManagers.DoubleFiltered
{
    class DoubleFilterOptimiserCreator : OptimiserCreator
    {
        public DoubleFilterOptimiserCreator() : base("Double Filtered Optimiser")
        { }
        public override IOptimiser Create(TerminalManager terminalManager)
        {
            return new Manager(Name);
        }
    }

    class Manager : IOptimiser
    {
        public Manager(string name)
        {
            Name = name;
            SubFormKeeper = new View_Model.SubFormKeeper(_Window_creator);
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
        public string OptimiserWorkingDirectory { get; protected set; }
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
        private System.Windows.Window _Window_creator()
        {
            throw new NotImplementedException();
        }

        public void LoadSettingsWindow()
        {
            SubFormKeeper.Open();
        }

        #endregion
        public void Start(OptimiserInputData optimiserInputData, string PathToResultsFile, string dirPrifix)
        {
            throw new NotImplementedException();
        }

        private void Test(List<KeyValuePair<DateBorders, List<ParamsItem>>> borders, OptimiserInputData data, List<OptimisationResult> results)
        {
            throw new NotImplementedException();
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
