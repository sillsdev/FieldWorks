// Copyright (c) 2015-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using System.IO.Compression;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace FwBuildTasks
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
			using (ZipArchive archive = ZipFile.OpenRead(ZipFilename))
			{
				foreach (ZipArchiveEntry entry in archive.Entries)
				{
					string filePath = Path.Combine(ToDir, entry.FullName);
					string dirPath = Path.GetDirectoryName(filePath);
					if (dirPath != null && !Directory.Exists(dirPath))
						Directory.CreateDirectory(dirPath);
					entry.ExtractToFile(filePath, true);
				}
			}
			return true;
		}
	}
}
