using System;
using System.Collections.Generic;
using Metatrader_Auto_Optimiser.Model.FileReaders;
using Metatrader_Auto_Optimiser.Model.Terminal;
using ReportManager;

namespace Metatrader_Auto_Optimiser.Model.OptimisationManagers
{
    /// <summary>
    /// Интерфейс пользовательского оптимизатора
    /// </summary>
    interface IOptimiser
    {
        /// <summary>
        /// Событие окончания оптимизационного процесса
        /// </summary>
        event Action<IOptimiser> OptimisationProcessFinished;
        /// <summary>
        /// Событие обновления Progress бара из оптимизатора
        /// </summary>
        event Action<string, double> ProcessStatus;

        /// <summary>
        /// Менеджер терминалов
        /// </summary>
        TerminalManager TerminalManager { get; set; }
        /// <summary>
        /// Признак запущен ли процесс оптимизации
        /// </summary>
        bool IsOptimisationInProcess { get; }
        /// <summary>
        /// Имя менеджера оптимизаций
        /// </summary>
        string Name { get; }

        #region Report getters
        /// <summary>
        /// Скисок всех исторических оптимизационных проходов
        /// </summary>
        List<OptimisationResult> AllOptimisationResults { get; }
        /// <summary>
        /// Список форвардных оптимизаций
        /// </summary>
        List<OptimisationResult> ForwardOptimisations { get; }
        /// <summary>
        /// Список исторических оптимизаций
        /// </summary>
        List<OptimisationResult> HistoryOptimisations { get; }

        /// <summary>
        /// Валюта указанная в тестере для выбранного символа
        /// </summary>
        string Currency { get; }
        /// <summary>
        /// Баланс указанный в тестере на момент начала оптимизации
        /// </summary>
        double Balance { get; }
        /// <summary>
        /// Кредитное плечо казанное в тестере
        /// </summary>
        int Laverage { get; }
        /// <summary>
        /// Путь к роботу для которого производилась оптимизация
        /// </summary>
        string PathToBot { get; }
        /// <summary>
        /// Рабочая директория оптимизатора (вложенная папка по пути Data/Reports)
        /// </summary>
        string OptimiserWorkingDirectory { get; }
        #endregion

        /// <summary>
        /// Запуск процесса оптимизации
        /// </summary>
        /// <param name="optimiserInputData">Входные параметры и настройка тестера</param>
        /// <param name="PathToResultsFile">Путь к файлу с результатами оптимизации</param>
        /// <param name="dirPrifix">Префикс директории с результатами оптимизации</param>
        void Start(OptimiserInputData optimiserInputData, string PathToResultsFile, string dirPrifix);
        /// <summary>
        /// /Остановка процесса оптимизации извне
        /// </summary>
        void Stop();
        /// <summary>
        /// Загрузка окна с настройками процесса оптимизации
        /// </summary>
        void LoadSettingsWindow();
        /// <summary>
        /// Отчистка оптимизатора
        /// </summary>
        void ClearOptimiser();
    }

    /// <summary>
    /// Структура с параметрами запуска оптимизаций
    /// </summary>
    struct OptimiserInputData
    {
        /// <summary>
        /// Режим работы оптимизатора
        /// </summary>
        public ENUM_OptimisationMode OptimisationMode;
        /// <summary>
        /// Таймфрейм
        /// </summary>
        public ENUM_Timeframes TF;
        /// <summary>
        /// Список параметров робота
        /// </summary>
        public List<ParamsItem> BotParams;
        /// <summary>
        /// Выбор модели генерации тиков во время оптимизации
        /// </summary>
        public ENUM_Model Model;
        /// <summary>
        /// Выбор режима исполнения (задержка)
        /// </summary>
        public ENUM_ExecutionDelay ExecutionDelay;
        /// <summary>
        /// Список исторических и форвардных границ оптимизации
        /// </summary>
        public List<DateBorders> HistoryBorders, ForwardBorders;
        /// <summary>
        /// Данные по которым фильтруются результаты оптимизации
        /// </summary>
        public IDictionary<SortBy, KeyValuePair<CompareType, double>> CompareData;
        /// <summary>
        /// Данные по которым сортируются результаты оптимизации
        /// </summary>
        public IEnumerable<SortBy> SortingFlags;
        /// <summary>
        /// Путь к роботу относительно директории "Experts"
        /// </summary>
        public string RelativePathToBot;
        /// <summary>
        /// Баланс на начало тестирования
        /// </summary>
        public double Balance;
        /// <summary>
        /// Валюта депозита
        /// </summary>
        public string Currency;
        /// <summary>
        /// Кредитное плечо
        /// </summary>
        public int Laverage;
        /// <summary>
        /// Выбранный актив
        /// </summary>
        public string Symb;
    }
}
