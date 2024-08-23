using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Etax_Api
{
    public static class Function
    {
        public static void DeleteDirectory(string startLocation)
        {
            foreach (var directory in Directory.GetDirectories(startLocation))
            {
                DeleteDirectory(directory);
                if (Directory.GetFiles(directory).Length == 0 && Directory.GetDirectories(directory).Length == 0)
                {
                    Directory.Delete(directory, false);
                }
            }
        }

        public static void DeleteFile(string filePath)
        {
            DateTime now = DateTime.Now;
            string[] files = Directory.GetFiles(filePath, "*", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                FileInfo fi = new FileInfo(file);
                if (fi.CreationTime.AddDays(1) < now)
                    System.IO.File.Delete(file);
            }
        }
    }
}
