using System.IO;
using UnityEngine;

namespace Seb
{

	public static class IOHelper
	{

		public static string EnsureUniqueFileName(string originalPath)
		{
			string originalFileName = Path.GetFileName(originalPath);
			string originalFileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalPath);
			string extension = Path.GetExtension(originalPath);
			string uniquePath = originalPath;

			int duplicates = 0;

			while (File.Exists(uniquePath))
			{
				duplicates++;
				string uniqueFileName = $"{originalFileNameWithoutExtension}_{duplicates}{extension}";
				uniquePath = originalPath.Replace(originalFileName, uniqueFileName);
			}

			return uniquePath;
		}

		public static string EnsureUniqueDirectoryName(string originalPath)
		{
			string uniquePath = originalPath;
			int duplicates = 0;

			while (Directory.Exists(uniquePath))
			{
				duplicates++;
				uniquePath = originalPath + "_" + duplicates;
			}

			return uniquePath;
		}

		// https://learn.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories
		public static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
		{
			var dir = new DirectoryInfo(sourceDir);

			if (!dir.Exists)
				throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

			DirectoryInfo[] dirs = dir.GetDirectories();

			Directory.CreateDirectory(destinationDir);

			foreach (FileInfo file in dir.GetFiles())
			{
				string targetFilePath = Path.Combine(destinationDir, file.Name);
				file.CopyTo(targetFilePath);
			}

			if (recursive)
			{
				foreach (DirectoryInfo subDir in dirs)
				{
					string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
					CopyDirectory(subDir.FullName, newDestinationDir, true);
				}
			}
		}

		public static void SaveBytesToFile(string path, string fileName, byte[] data, bool log = false)
		{
			string fullPath = Path.Combine(path, fileName + ".bytes");

			using (BinaryWriter writer = new BinaryWriter(File.Open(fullPath, FileMode.Create)))
			{
				writer.Write(data);
			}

			if (log)
			{
				Debug.Log("Saved data to: " + fullPath);
			}
		}

		public static void SaveTextToFile(string path, string fileName, string fileExtension, string data, bool log = false)
		{
			if (fileExtension[0] != '.')
			{
				fileExtension = "." + fileExtension;
			}

			string fullPath = Path.Combine(path, fileName + fileExtension);

			SaveTextToFile(fullPath, data, log);
		}


		public static void SaveTextToFile(string fullPath, string data, bool log = false)
		{

			using (var writer = new StreamWriter(File.Open(fullPath, FileMode.Create)))
			{
				writer.Write(data);
			}

			if (log)
			{
				Debug.Log("Saved data to: " + fullPath);
			}
		}
	}
}