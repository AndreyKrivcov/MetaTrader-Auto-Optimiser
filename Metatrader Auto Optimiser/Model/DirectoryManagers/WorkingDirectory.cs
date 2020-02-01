using System.IO;

namespace Metatrader_Auto_Optimiser.Model.DirectoryManagers
{
    /// <summary>
    /// Объект описывающий подконтрольную директорию Data с изменяемыми файлами авто оптимизатора.
    /// </summary>
    class WorkingDirectory
    {
        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        public WorkingDirectory()
        {
            // Создание корневой директории с изменяемыми файлами
            WDRoot = new DirectoryInfo("Data");
            if (!WDRoot.Exists)
                WDRoot.Create();
            // Создание вложенной вложенной директории с отчетами оптимизаций
            Reports = WDRoot.GetDirectory("Reports", true);
        }
        /// <summary>
        /// Вложенная директория с отчетами оптимизаций
        /// </summary>
        public DirectoryInfo Reports { get; }
        /// <summary>
        /// Корневая директория с изменяемыми файлами и папками
        /// </summary>
        public DirectoryInfo WDRoot { get; }

        /// <summary>
        /// Получение или создание (если не была создана ранее) директории вложенной в директорию Reports.
        /// Полученная директория хранит в себе результаты конкретного прохода оптимизаций.
        /// </summary>
        /// <param name="Symbol">Символ на котором проводилась оптимизация</param>
        /// <param name="ExpertName">Имя робота</param>
        /// <param name="DirectoryPrefix">Префикс добавляемый к имени директории</param>
        /// <param name="OptimiserName">Наименование использованного оптимизатора</param>
        /// <returns>
        /// Путь к директории  с результатами оптимизации.
        /// Имя директории строится пледующим образом:
        /// {DirectoryPrefix} {OptimiserName} {ExpertName} {Symbol}
        /// </returns>
        public DirectoryInfo GetOptimisationDirectory(string Symbol, string ExpertName,
                                                      string DirectoryPrefix, string OptimiserName)
        {
            return Reports.GetDirectory($"{DirectoryPrefix} {OptimiserName} {ExpertName} {Symbol}", true);
        }

        /// <summary>
        /// Путь к директории Data/Tester 
        /// Нужна для временного перемещения файлов из одноименной директории терминала
        /// </summary>
        public DirectoryInfo Tester => WDRoot.GetDirectory("Tester", true);

    }
}
