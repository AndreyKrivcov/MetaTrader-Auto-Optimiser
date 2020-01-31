using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Metatrader_Auto_Optimiser.Model.DirectoryManagers
{
    class TerminalDirectory
    {
        public TerminalDirectory() :
            this(new DirectoryInfo(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData)).
                GetDirectory("MetaQuotes").GetDirectory("Terminal"))
        {
        }

        public TerminalDirectory(DirectoryInfo path)
        {
            pathToTerminal = path;
            Common = path.GetDirectory("Common");
            Community = path.GetDirectory("Community");
        }

        private readonly DirectoryInfo pathToTerminal;

        public IEnumerable<DirectoryInfo> Terminals
        {
            get
            {
                bool Comparer(DirectoryInfo dir)
                {
                    string pathToOrigin = Path.Combine(dir.FullName, "origin.txt");
                    if (!File.Exists(pathToOrigin))
                        return false;
                    if (!File.Exists(Path.Combine(File.ReadAllText(pathToOrigin), "terminal64.exe")))
                        return false;
                    return true;

                }

                return pathToTerminal.GetDirectories().Where(x => Comparer(x));
            }
        }
        public DirectoryInfo Common { get; }
        public DirectoryInfo Community { get; }
    }
}
