﻿using System;
using System.Collections.Generic;
using System.Windows.Input;
using Metatrader_Auto_Optimiser.Model;

namespace Metatrader_Auto_Optimiser.View_Model
{
    class AutoFillInDateBordersVM
    {
        public AutoFillInDateBordersVM()
        {
            Set = new RelayCommand(Calculate);
        }
        private readonly IAutoFillInDateBordersM model = AutoFillInDateBordersCreator.Model;
        public List<StepItem> Steps { get; } = new List<StepItem>
        {
            new StepItem(OptimisationType.History) { Value = 360},
            new StepItem(OptimisationType.Forward) { Value = 90}
        };
        private void Calculate(object o)
        {
            try
            {
                model.Calculate(From, Till,
                                Steps.Find(x => x.Type == OptimisationType.History).Value,
                                Steps.Find(x => x.Type == OptimisationType.Forward).Value);
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message);
            }
        }
        public DateTime From { get; set; } = DateTime.Now;
        public DateTime Till { get; set; } = DateTime.Now;

        public ICommand Set { get; }
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
