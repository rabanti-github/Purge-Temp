using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PurgeTempTest.Utils
{
	public class TempFolderFixture : IDisposable
	{
		private readonly List<string> tempFolders = new List<string>();

		public string CreateUniqueTempFolder()
		{
			string tempFolderPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
			Directory.CreateDirectory(tempFolderPath);
			tempFolders.Add(tempFolderPath);
			return tempFolderPath;
		}

		public void Dispose()
		{
			foreach (var folder in tempFolders)
			{
				if (Directory.Exists(folder))
				{
					TestFileUtils.DeleteDirectoryWithRetry(folder);
				}
			}
		}
	}
}
