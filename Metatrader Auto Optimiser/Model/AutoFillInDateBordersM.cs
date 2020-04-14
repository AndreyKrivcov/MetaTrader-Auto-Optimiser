using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Metatrader_Auto_Optimiser.Model
{
    class AutoFillInDateBordersCreator
    {
        private static IAutoFillInDateBordersM instance;
        public static IAutoFillInDateBordersM Model 
        { 
            get 
            {
                if (instance == null)
                    instance = new AutoFillInDateBordersM();

                return instance;
            } 
        }
    }
    interface IAutoFillInDateBordersM
    {
        event Action<List<KeyValuePair<OptimisationType, DateTime[]>>> DateBorders;
        void Calculate(DateTime From, DateTime Till, uint history, uint forward);
    }
    class AutoFillInDateBordersM : IAutoFillInDateBordersM
    {
        public event Action<List<KeyValuePair<OptimisationType, DateTime[]>>> DateBorders;

        public void Calculate(DateTime From, DateTime Till, uint history, uint forward)
        {
            List<KeyValuePair<OptimisationType, DateTime[]>> data = new List<KeyValuePair<OptimisationType, DateTime[]>>();
            DateBorders?.Invoke(data);
            throw new NotImplementedException();
        }
    }

    class StepItem
    {
        public StepItem(OptimisationType type) 
        {
            Type = type;
        }
        public OptimisationType Type { get; }
        public uint Value { get; set; }
    }
}
