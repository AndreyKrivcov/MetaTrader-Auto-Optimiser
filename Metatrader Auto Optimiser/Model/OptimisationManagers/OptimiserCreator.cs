
using System.Collections.Generic;

namespace Metatrader_Auto_Optimiser.Model.OptimisationManagers
{    
    abstract class OptimiserCreator
    {
        public OptimiserCreator(string Name)
        {
            this.Name = Name;
        }
        public abstract IOptimiser Create(Terminal.TerminalManager terminalManager);
        public string Name { get; }
    }
}
