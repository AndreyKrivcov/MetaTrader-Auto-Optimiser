using System.IO;

namespace Metatrader_Auto_Optimiser.Model.DirectoryManagers
{
    class WorkingDirectory
    {
        public WorkingDirectory()
        {
            WDRoot = new DirectoryInfo("Data");
            if (!WDRoot.Exists)
                WDRoot.Create();
            Reports = WDRoot.GetDirectory("Reports", true);
        }
        public DirectoryInfo Reports { get; }
        public DirectoryInfo WDRoot { get; }

        public DirectoryInfo GetOptimisationDirectory(string Symbol, string ExpertName,
                                                      string DirectoryPrefix, string OptimiserName)
        {
            return Reports.GetDirectory($"{DirectoryPrefix} {OptimiserName} {ExpertName} {Symbol}", true);
        }

        public DirectoryInfo Tester => WDRoot.GetDirectory("Tester", true);

    }
}
