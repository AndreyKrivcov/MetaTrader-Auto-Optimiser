using ReportManager;
using System;

namespace Metatrader_Auto_Optimiser.Model.OptimisationManagers.DoubleFiltered
{
    class Settings_VM
    {
        private readonly Settings_M model = Settings_M.Instance();

        public bool IsTickTest
        {
            get => model.IsTickTest;
            set => model.IsTickTest = value;
        }
        public string[] SourtingFlags { get; } = Enum.GetNames(typeof(SortBy));
        public string SelectedFlag
        {
            get => model.SecondSorter.ToString();
            set => model.SecondSorter = (SortBy)Enum.Parse(typeof(SortBy), value);
        }
    }

    class Settings_M
    {
        private static Settings_M instance;
        private Settings_M() { }
        public static Settings_M Instance()
        {
            if (instance == null)
                instance = new Settings_M();
            return instance;
        }

        public bool IsTickTest { get; set; } = true;
        public SortBy SecondSorter { get; set; } = (SortBy)Enum.Parse(typeof(SortBy), Enum.GetNames(typeof(SortBy))[0]);

    }
}
