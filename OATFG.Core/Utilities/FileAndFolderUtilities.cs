using System.Reflection;

namespace OATFG.Core.Utilities
{
    public static class FileAndFolderUtilities
    {
        public static bool CreateFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                return true;
            }

            if (Directory.Exists(path))
            {
                return true;
            }

            return false;
        }

        public static string GetExecutableFilePath()
        {
            string executableFilePath = Assembly.GetExecutingAssembly().Location;
            string getExecutableFilePath = Path.GetDirectoryName(executableFilePath);
            return getExecutableFilePath;
        }
    }
}