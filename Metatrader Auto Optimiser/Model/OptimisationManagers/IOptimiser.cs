using System;
using System.Collections.Generic;
using Metatrader_Auto_Optimiser.Model.FileReaders;
using Metatrader_Auto_Optimiser.Model.Terminal;
using ReportManager;

namespace Metatrader_Auto_Optimiser.Model.OptimisationManagers
{
    interface IOptimiser
    {
        event Action<IOptimiser> OptimisationProcessFinished;
        event Action<string, double> ProcessStatus; // for progress bar

        TerminalManager TerminalManager { get; set; }
        bool IsOptimisationInProcess { get; }
        string Name { get; }

        #region Report getters
        List<OptimisationResult> AllOptimisationResults { get; }
        List<OptimisationResult> ForwardOptimisations { get; }
        List<OptimisationResult> HistoryOptimisations { get; }

        string Currency { get; }
        double Balance { get; }
        int Laverage { get; }
        string PathToBot { get; }
        string OptimiserWorkingDirectory { get; }
        #endregion

        void Start(OptimiserInputData optimiserInputData, string PathToResultsFile, string dirPrifix);
        void Stop();
        void LoadSettingsWindow();
        void ClearOptimiser();
    }

    struct OptimiserInputData
    {
        public ENUM_OptimisationMode OptimisationMode;
        public ENUM_Timeframes TF;
        public List<ParamsItem> BotParams;
        public ENUM_Model Model;
        public ENUM_ExecutionDelay ExecutionDelay;
        //public Dictionary<DateBorders, OptimisationType> DateBorders;
        public List<DateBorders> HistoryBorders, ForwardBorders;
        public IDictionary<SortBy, KeyValuePair<CompareType, double>> CompareData;
        public IEnumerable<SortBy> SortingFlags;
        public string RelativePathToBot;
        public double Balance;
        public string Currency;
        public int Laverage;
        public string Symb;
    }
}
