// Copyright (c) 2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.IO;
using FwBuildTasks;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace SIL.FieldWorks.Build.Tasks
{
	/// <summary>
	/// Generate a CSV file containing metadata about the specified files (MD5, date modified, etc.)
	/// for use in testing installers and patchers
	/// </summary>
	public class LogMetadata : Task
	{
		[Required]
		public string[] Files { get; set; }

		public string PathPrefixToDrop { get; set; }

		[Required]
		public string LogFile { get; set; }

		public bool Overwrite { get; set; }

		public override bool Execute()
		{
			using (var writer = new StreamWriter(LogFile, !Overwrite))
			{
				writer.WriteLine("File,MD5,Version,Modified");
				var lengthToDrop = string.IsNullOrEmpty(PathPrefixToDrop) ? 0 : PathPrefixToDrop.Length;
				foreach (var file in Files)
				{
					var md5 = Md5Checksum.Compute(file);
					// Some files have commas in their versions. Replace with another character because our CSV reader is cheap.
					var version = FileVersionInfo.GetVersionInfo(file).FileVersion?.Replace(',', ';');
					var modified = File.GetLastWriteTimeUtc(file);
					writer.WriteLine($"{file.Substring(lengthToDrop)}, {md5}, {version}, {modified:yyyy-MM-dd HH:mm:ss}");
				}
				return true;
			}
		}
	}
}
