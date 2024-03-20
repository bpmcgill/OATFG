using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OATFG.Core.Utilities
{
    public static class FileAndFolderUtilities
    {
        public static string GetExecutableFilePath()
        {
            string executableFilePath = Assembly.GetExecutingAssembly().Location;
            string getExecutableFilePath = Path.GetDirectoryName(executableFilePath);
            return getExecutableFilePath;
        }

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
    }

}
