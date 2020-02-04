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
        /// <summary>
        /// Событие выброса ошибки из модели данных
        /// </summary>
        event Action<string> ThrowException;
        /// <summary>
        /// Событие остановки оптимизации
        /// </summary>
        event Action OptimisationStoped;
        /// <summary>
        /// Событие обновление прогресс бара из модели данных
        /// </summary>
        event Action<string, double> PBUpdate;
        #endregion

        #region Methods
        /// <summary>
        /// Метод загружающий ранее созраненные результаты оптимизаций
        /// </summary>
        /// <param name="optimisationName">Имя требуемого отчета</param>
        void LoadSavedOptimisation(string optimisationName);
        /// <summary>
        /// Метод изменяющий ранее выбранный терминалл
        /// </summary>
        /// <param name="terminalName">ID запрашиваемого терминала</param>
        /// <returns></returns>
        bool ChangeTerminal(string terminalName);
        /// <summary>
        /// Метод смены оптимизатора
        /// </summary>
        /// <param name="optimiserName">Имя оптимизатора</param>
        /// <param name="terminalName">Имя терминала</param>
        /// <returns></returns>
        bool ChangeOptimiser(string optimiserName, string terminalName = null);
        /// <summary>
        /// Запуск оптимизации
        /// </summary>
        /// <param name="optimiserInputData">Взодные данные для запуска оптимизаций</param>
        /// <param name="IsAppend">Признак дополнить ли существующие выгрузки (если существуют) или же перезаписать их</param>
        /// <param name="dirPrefix">Префикс директории с оптимизациями</param>
        void StartOptimisation(OptimiserInputData optimiserInputData, bool IsAppend, string dirPrefix);
        /// <summary>
        /// Остановка оптимизации извне (пользователем)
        /// </summary>
        void StopOptimisation();
        /// <summary>
        /// Получение параметров робота
        /// </summary>
        /// <param name="botName">Имя эксперта</param>
        /// <param name="isUpdate">Признак нужно ли обновлять файл с параметрами перед его чтением</param>
        /// <returns>Список параметров</returns>
        IEnumerable<ParamsItem> GetBotParams(string botName, bool isUpdate);
        /// <summary>
        /// Созранение в (*.csv) файл выбранных оптимизаций
        /// </summary>
        /// <param name="pathToSavingFile">Путь к созраняемому файлу</param>
        void SaveToCSVSelectedOptimisations(string pathToSavingFile);
        /// <summary>
        /// Созранение в (*csv) файл оптимизаций з переданную дату
        /// </summary>
        /// <param name="dateBorders">Границы дат</param>
        /// <param name="pathToSavingFile">Путь к созраняемому файлу</param>
        void SaveToCSVOptimisations(DateBorders dateBorders, string pathToSavingFile);
        /// <summary>
        /// Запуск процесса тестирования
        /// </summary>
        /// <param name="optimiserInputData">Список паарметров настройки тестера</param>
        void StartTest(OptimiserInputData optimiserInputData);
        /// <summary>
        /// Запуск процесса сортировки результатов
        /// </summary>
        /// <param name="borders">Границы дат</param>
        /// <param name="sortingFlags">Массив имен параметров для сортировки</param>
        void SortResults(DateBorders borders, IEnumerable<SortBy> sortingFlags);
        /// <summary>
        /// Фильтрация результатов оптимизации
        /// </summary>
        /// <param name="borders">Границы дат</param>
        /// <param name="compareData">Флаги фильтрации данных</param>
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
        History, // Историческая
        Forward // Форвардная
    }

}
