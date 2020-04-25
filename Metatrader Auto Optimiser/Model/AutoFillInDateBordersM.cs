using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Metatrader_Auto_Optimiser.Model
{
    class AutoFillInDateBordersCreator
    {
        public static IAutoFillInDateBordersM Model => AutoFillInDateBordersM.Instance();
    }
    interface IAutoFillInDateBordersM
    {
        event Action<List<KeyValuePair<OptimisationType, DateTime[]>>> DateBorders;
        void Calculate(DateTime From, DateTime Till, uint history, uint forward);
    }
    class AutoFillInDateBordersM : IAutoFillInDateBordersM
    {
        private AutoFillInDateBordersM() { }

        private static AutoFillInDateBordersM instance;

        public static AutoFillInDateBordersM Instance()
        {
            if (instance == null)
                instance = new AutoFillInDateBordersM();

            return instance;
        }

        public event Action<List<KeyValuePair<OptimisationType, DateTime[]>>> DateBorders;

        public void Calculate(DateTime From, DateTime Till, uint history, uint forward)
        {
            if (From >= Till)
                throw new ArgumentException("Date From must be less then date Till");

            List<KeyValuePair<OptimisationType, DateTime[]>> data = new List<KeyValuePair<OptimisationType, DateTime[]>>();

            OptimisationType type = OptimisationType.History;

            DateTime _history = From;
            DateTime _forward = From.AddDays(history + 1);

            DateTime CalcEndDate()
            {
                return type == OptimisationType.History ? _history.AddDays(history) : _forward.AddDays(forward);
            }

            while (CalcEndDate() <= Till)
            {
                DateTime from = type == OptimisationType.History ? _history : _forward;
                data.Add(new KeyValuePair<OptimisationType, DateTime[]>(type, new DateTime[2] { from, CalcEndDate() }));

                if (type == OptimisationType.History)
                    _history = _history.AddDays(forward + 1);
                else
                    _forward = _forward.AddDays(forward + 1);

                type = type == OptimisationType.History ? OptimisationType.Forward : OptimisationType.History;
            }

            if (data.Count == 0)
                throw new ArgumentException("Can`t create any date borders with setted In sample (History) step");

            DateBorders?.Invoke(data);
        }
    }
}
