// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace FwBuildTasks
{
	/// <summary>
	/// Zip the specified file(s) into the specified path(s).
	/// </summary>
	public class Zip : Task
	{
		[Required]
		public ITaskItem[] Source { get; set; }

		[Required]
		public string Destination { get; set; }

		/// <summary>
		/// Gets or sets the working directory for the zip file.
		/// </summary>
		/// <value>The working directory.</value>
		/// <remarks>
		/// The working directory is the base of the zip file.
		/// All files will be made relative from the working directory.
		/// </remarks>
		public string WorkingDirectory { get; set; }

		public override bool Execute()
		{
			/*
			If the Source is a single file path:
				1) If the Destination ends with ".zip", that will be the zip file used.
				2) If the Destination does not end with ".zip", it will be treated as a
					folder path; a zip file will be put in that folder, and the zip file
					name will match the Source file name, but with the extension changed
					to .zip.
			If the Source comprises multiple file paths (separated by semicolons):
				1) If the Destination ends with ".zip", that will be the zip file used;
					all the Source files will be zipped into the one zip file.
				2) If the Destination does not end with ".zip", it will be treated as a
					folder path; each source file will be zipped into its own zip file
					in that folder, and each zip file name will match its Source file
					name, but with the extension changed to .zip.
			*/
			if (Destination.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
				CompressFilesToOneZipFile();
			else
				CompressFilesToMultipleZipFiles();

			return true;
		}

		private void CompressFilesToOneZipFile()
		{
			Log.LogMessage(MessageImportance.Normal, "Zipping " + Source.Length + " files to zip file " + Destination);

			if (File.Exists(Destination))
				File.Delete(Destination);
			using (ZipArchive archive = ZipFile.Open(Destination, ZipArchiveMode.Create))
			{
				foreach (ITaskItem item in Source)
				{
					string inputPath = item.ItemSpec;
					var inputFileInfo = new FileInfo(inputPath);
					// clean up name
					string pathInArchive = !string.IsNullOrEmpty(WorkingDirectory) ? GetPath(inputFileInfo.FullName, WorkingDirectory)
						: Path.GetFileName(inputFileInfo.FullName);

					archive.CreateEntryFromFile(inputPath, pathInArchive);
				}
			}
		}

		private void CompressFilesToMultipleZipFiles()
		{
			Log.LogMessage(MessageImportance.Normal, "Zipping " + Source.Length + " files to zip files in folder " + Destination);

			// Create the output folder if it does not exist:
			if (!Directory.Exists(Destination))
				Directory.CreateDirectory(Destination);

			foreach (ITaskItem item in Source)
			{
				string inputPath = item.ItemSpec;

				// Form output zip file full path:
				string outputPath = Path.Combine(Destination, Path.GetFileNameWithoutExtension(inputPath) + ".zip");
				if (File.Exists(outputPath))
					File.Delete(outputPath);
				using (ZipArchive archive = ZipFile.Open(outputPath, ZipArchiveMode.Create))
				{
					var inputFileInfo = new FileInfo(inputPath);

					string pathInArchive = !string.IsNullOrEmpty(WorkingDirectory) ? GetPath(inputFileInfo.FullName, WorkingDirectory)
						: Path.GetFileName(inputFileInfo.FullName);

					archive.CreateEntryFromFile(inputPath, pathInArchive);
				}
			}
		}

		private static string GetPath(string originalPath, string rootDirectory)
		{
			var rootDirInfo = new DirectoryInfo(rootDirectory);

			var relativePath = new List<string>();
			string[] originalDirectories = originalPath.Split(Path.DirectorySeparatorChar);
			string[] rootDirectories = rootDirInfo.FullName.Split(Path.DirectorySeparatorChar);

			int length = Math.Min(originalDirectories.Length, rootDirectories.Length);

			int lastCommonRoot = -1;

			// find common root
			for (int x = 0; x < length; x++)
			{
				if (!string.Equals(originalDirectories[x], rootDirectories[x], StringComparison.OrdinalIgnoreCase))
					break;

				lastCommonRoot = x;
			}
			if (lastCommonRoot == -1)
				return originalPath;

			// add extra original directories
			for (int x = lastCommonRoot + 1; x < originalDirectories.Length; x++)
				relativePath.Add(originalDirectories[x]);

			return string.Join(Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture),
				relativePath.ToArray());
		}
	}
}
