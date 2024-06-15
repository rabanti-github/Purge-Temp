using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PurgeTempTest.Utils
{
    public class TempFolderFixture : IDisposable
    {
        public string TempFolderPath { get; private set; }

        public TempFolderFixture()
        {
            TempFolderPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(TempFolderPath);
        }

        public void Dispose()
        {
            if (Directory.Exists(TempFolderPath))
            {
                Directory.Delete(TempFolderPath, true);
            }
        }
    }
}
