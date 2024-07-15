using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PurgeTempTest.Utils
{
	public static class FileUtils
	{
		public static void DeleteDirectoryWithRetry(string directoryPath, int maxRetries = 5, int delayMilliseconds = 500)
		{
			for (int i = 0; i < maxRetries; i++)
			{
				try
				{
					if (Directory.Exists(directoryPath))
					{
						Directory.Delete(directoryPath, true);
					}
					return;
				}
				catch (IOException)
				{
					Thread.Sleep(delayMilliseconds);
				}
				catch (UnauthorizedAccessException)
				{
					Thread.Sleep(delayMilliseconds);
				}
			}
		}

		public static string CreateFile(string folder, string fileName, string content)
		{
			string testFilePath = Path.Combine(folder, fileName);
			File.WriteAllText(testFilePath, content);
			return testFilePath;
		}

		public static int CountLines(string filePath, bool cleanup = false)
		{
			FileInfo fileInfo = new FileInfo(filePath);
			string tempFile = Path.Combine(fileInfo.Directory.FullName, "temp.txt");
			File.Copy(filePath, tempFile);
			int lineCount = 0;
			using (FileStream fs = new FileStream(tempFile, FileMode.Open, FileAccess.Read))
			{
				using (StreamReader sr = new StreamReader(fs))
				{
					string text = sr.ReadToEnd();
					string[] lines = text.Split('\r', '\n', StringSplitOptions.RemoveEmptyEntries);
					foreach (string line in lines)
					{
						if (line != "\r" && line != "\n" && line != "\r\n" && line != "")
						{
							lineCount++;
						}
					}
				}
			}
			if (cleanup)
			{
				File.Delete(tempFile);
			}
			return lineCount;
		}

	}
}
