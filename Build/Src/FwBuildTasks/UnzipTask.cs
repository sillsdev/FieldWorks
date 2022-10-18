// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using System.IO.Compression;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace SIL.FieldWorks.Build.Tasks
{
	/// <summary>
	/// Unzip the specified archive file into the specified directory.
	/// </summary>
	public class Unzip : Task
	{
		[Required]
		public string ZipFilename { get; set; }

		[Required]
		public string ToDir { get; set; }

		public override bool Execute()
		{
			Log.LogMessage("Unzipping {0} to {1}.", ZipFilename, ToDir);
			using (var archive = ZipFile.OpenRead(ZipFilename))
			{
				// an empty name denotes a directory, which ZipArchiveEntry can't ExtractToFile.
				foreach (var entry in archive.Entries.Where(entry => !string.IsNullOrEmpty(entry.Name)))
				{
					var filePath = Path.Combine(ToDir, entry.FullName);
					var dirPath = Path.GetDirectoryName(filePath);
					if (dirPath != null && !Directory.Exists(dirPath))
						Directory.CreateDirectory(dirPath);
					entry.ExtractToFile(filePath, true);
					Log.LogMessage(MessageImportance.Low, "Extracting {0}.", filePath);
				}
			}
			return true;
		}
	}
}
