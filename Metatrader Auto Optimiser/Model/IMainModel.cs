using Metatrader_Auto_Optimiser.Model.FileReaders;
using Metatrader_Auto_Optimiser.Model.OptimisationManagers;
using ReportManager;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Metatrader_Auto_Optimiser.Model
{
    /// <summary>
    /// Интерфейс можели данных основного окна оптимизатора
    /// </summary>
    interface IMainModel : INotifyPropertyChanged
    {
        #region Getters
        /// <summary>
        /// Выбранный оптимизатор
        /// </summary>
        IOptimiser Optimiser { get; }
        /// <summary>
        /// Список имен терминалов установленных на компьютере
        /// </summary>
        IEnumerable<string> TerminalNames { get; }
        /// <summary>
        /// Список имен оптимизаторов доступных для использования
        /// </summary>
        IEnumerable<string> OptimisatorNames { get; }
        /// <summary>
        /// Список имен директорий с созраненными оптимизациями (Data/Reperts/*)
        /// </summary>
        IEnumerable<string> SavedOptimisations { get; }
        /// <summary>
        /// Структура со всеми прозодами резултьтатов оптимизаций
        /// </summary>
        ReportData AllOptimisationResults { get; }
        /// <summary>
        /// Форвардные тесты
        /// </summary>
        List<OptimisationResult> ForwardOptimisations { get; }
        /// <summary>
        /// Исторические тесты
        /// </summary>
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
        bool ChangeOptimiser(string optimiserName, string terminalName = null);
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

    /// <summary>
    /// Структура описывающая результаты оптимизации
    /// </summary>
    struct ReportData
    {
        /// <summary>
        /// Словарь с прозодами оптимизаций
        /// key - диаппазон дат
        /// value - список прозодов оптимизаций за заданный диаппазон
        /// </summary>
        public Dictionary<DateBorders, List<OptimisationResult>> AllOptimisationResults;
        /// <summary>
        /// Эксперт и валюта
        /// </summary>
        public string Expert, Currency;
        /// <summary>
        /// Депозит
        /// </summary>
        public double Deposit;
        /// <summary>
        /// Кредитное плечо
        /// </summary>
        public int Laverage;
    }

    /// <summary>
    /// тип оптимизации
    /// </summary>
    enum OptimisationType
    {
        History,
        Forward
    }

}
