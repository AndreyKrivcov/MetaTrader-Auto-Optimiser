using Metatrader_Auto_Optimiser.Model.FileReaders;
using Metatrader_Auto_Optimiser.Model.OptimisationManagers;
using ReportManager;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Metatrader_Auto_Optimiser.Model
{
    interface IMainModel : INotifyPropertyChanged
    {
        #region Getters
        IOptimiser Optimiser { get; }
        IEnumerable<string> TerminalNames { get; }
        IEnumerable<string> OptimisatorNames { get; }
        IEnumerable<string> SavedOptimisations { get; }
        ReportData AllOptimisationResults { get; }
        List<OptimisationResult> ForwardOptimisations { get; }
        List<OptimisationResult> HistoryOptimisations { get; }
        #endregion

        #region Events
        event Action<string> ThrowException;
        event Action OptimisationStoped;
        event Action<string, double> PBUpdate;
        #endregion

        #region Methods
        void LoadSavedOptimisation(string optimisationName);
        bool ChangeTerminal(string terminalName);
        bool ChangeOptimiser(string optimiserName);
        void StartOptimisation(OptimiserInputData optimiserInputData, bool IsAppend, string dirPrefix);
        void StopOptimisation();
        IEnumerable<ParamsItem> GetBotParams(string botName, bool isUpdate);
        void SaveToCSVSelectedOptimisations(string pathToSavingFile);
        void SaveToCSVOptimisations(DateBorders dateBorders, string pathToSavingFile);
        void StartTest(OptimiserInputData optimiserInputData);
        void SortResults(DateBorders borders, IEnumerable<SortBy> sortingFlags);
        void FilterResults(DateBorders borders, IDictionary<SortBy, KeyValuePair<CompareType, double>> compareData);
        #endregion
    }

    struct ReportData
    {
        public Dictionary<DateBorders, List<OptimisationResult>> AllOptimisationResults;
        public string Expert, Currency;
        public double Deposit;
        public int Laverage;
    }

    enum OptimisationType
    {
        History,
        Forward
    }

}
