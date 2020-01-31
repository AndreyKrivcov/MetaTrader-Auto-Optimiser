using System.IO;

namespace Metatrader_Auto_Optimiser.Model.DirectoryManagers
{
    static class DirectoryInfoExtention
    {
        public static DirectoryInfo GetDirectory(this DirectoryInfo directory, string Name, bool createIfNotExists = false)
        {
            if (Name == null)
                return null;

            DirectoryInfo ans = new DirectoryInfo(Path.Combine(directory.FullName, Name));
            if (!ans.Exists)
            {
                if (!createIfNotExists)
                    return null;
                ans.Create();
            }
            return ans;
        }
    }
}
