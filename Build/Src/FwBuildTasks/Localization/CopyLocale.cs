// Copyright (c) 2024 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;


namespace SIL.FieldWorks.Build.Tasks.Localization
{
	public class CopyLocale : Task
	{
		[Required]
		public string SourceL10n { get; set; }

		[Required]
		public string DestL10n { get; set; }

		[Required]
		public string LcmDir { get; set; }

		public override bool Execute()
		{
			var srcLangCode = Path.GetFileName(SourceL10n);
			var destLangCode = Path.GetFileName(DestL10n);
			if (!Directory.Exists(SourceL10n))
			{
				Log.LogError($"Source directory '{SourceL10n}' does not exist.");
				return false;
			}
			if (Directory.Exists(DestL10n))
			{
				Log.LogError($"Destination directory '{DestL10n}' already exists.");
				return false;
			}
			// Create the destination directory
			Directory.CreateDirectory(DestL10n);

			// Get the files in the source directory and copy to the destination directory
			CopyDirectory(SourceL10n, DestL10n, true);

			NormalizeLocales.RenameLocaleFiles(DestL10n, srcLangCode, destLangCode);
			// Get the files in the source directory and copy to the destination directory
			foreach (var file in Directory.GetFiles(LcmDir, "*.resx", SearchOption.AllDirectories))
			{
				var relativePath = GetRelativePath(LcmDir, file);
				Log.LogMessage(MessageImportance.Normal, "CopyLocale: relpath - " + relativePath);
				var newFileName = Path.GetFileNameWithoutExtension(file) + $".{destLangCode}.resx";
				var newFilePath = Path.Combine(DestL10n, Path.Combine("Src", Path.GetDirectoryName(relativePath)));

				// Create the directory for the new file if it doesn't exist
				Directory.CreateDirectory(newFilePath);

				Log.LogMessage(MessageImportance.Normal, $"CopyLocale: {newFilePath}, {newFileName}");
				// Copy the file to the new location
				File.Move(file, Path.Combine(newFilePath, newFileName));
			}

			return true;
		}

		static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
		{
			// From: https://learn.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories
			// Get information about the source directory
			var dir = new DirectoryInfo(sourceDir);

			// Check if the source directory exists
			if (!dir.Exists)
				throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

			// Cache directories before we start copying
			DirectoryInfo[] dirs = dir.GetDirectories();

			// Create the destination directory
			Directory.CreateDirectory(destinationDir);

			// Get the files in the source directory and copy to the destination directory
			foreach (FileInfo file in dir.GetFiles())
			{
				string targetFilePath = Path.Combine(destinationDir, file.Name);
				file.CopyTo(targetFilePath);
			}

			// If recursive and copying subdirectories, recursively call this method
			if (recursive)
			{
				foreach (DirectoryInfo subDir in dirs)
				{
					string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
					CopyDirectory(subDir.FullName, newDestinationDir, true);
				}
			}
		}

		static string GetRelativePath(string baseDir, string filePath)
		{
			Uri baseUri = new Uri(baseDir);
			Uri fileUri = new Uri(filePath);
			return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fileUri).ToString().Replace('/', Path.DirectorySeparatorChar));
		}
	}
}
