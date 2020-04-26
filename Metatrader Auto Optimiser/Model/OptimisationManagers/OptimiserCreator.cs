
using System.Collections.Generic;

namespace Metatrader_Auto_Optimiser.Model.OptimisationManagers
{    
    /// <summary>
    /// Абстрактный класс фабрики оптимизаторов
    /// </summary>
    abstract class OptimiserCreator
    {
        /// <summary>
        /// Конструкор класса
        /// </summary>
        /// <param name="Name"></param>
        public OptimiserCreator(string Name)
        {
            this.Name = Name;
        }
        /// <summary>
        /// Абстрактный метод порождающий выбранный тип оптимизатора
        /// </summary>
        /// <param name="terminalManager">Выбранный терминалл</param>
        /// <returns>Оптимизатор</returns>
        public abstract IOptimiser Create(Terminal.TerminalManager terminalManager);
        /// <summary>
        /// Имя выбранного оптимизатора
        /// </summary>
        public string Name { get; }
    }

    class Optimisers
    {
        public static new List<OptimiserCreator> Creators => new List<OptimiserCreator>
        {
            new SimpleForvard.SimpleOptimiserManagerCreator(),
            new DoubleFiltered.DoubleFilterOptimiserCreator()
        };
    }
}
