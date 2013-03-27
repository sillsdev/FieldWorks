using System;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using ICSharpCode.SharpZipLib.Zip;

namespace FwBuildTasks
{
	/// <summary>
	/// Unzip the specified archive file into the specified directory.
	/// </summary>
	/// <remarks>
	/// This code is a simplified rip-off of the code used in unpacking backup
	/// files in FieldWorks.
	/// </remarks>
	public class Unzip : Task
	{
		public override bool Execute()
		{
			using (var zipIn = new ZipInputStream(File.OpenRead(ZipFilename)))
			{
				ZipEntry entry;
				while ((entry = zipIn.GetNextEntry()) != null)
				{
					var fileName = Path.GetFileName(entry.Name);
					if (String.IsNullOrEmpty(fileName) || fileName.EndsWith("/"))
						continue;
					string destDir;
					var filePath = Path.GetDirectoryName(entry.Name);
					if (string.IsNullOrEmpty(filePath))
						destDir = ToDir;
					else
						destDir = Path.Combine(ToDir, filePath);
					UnzipFileToFolder(zipIn, fileName, entry.Size, destDir, entry.DateTime);
				}
			}
			return true;
		}

		[Required]
		public string ZipFilename { get; set; }

		[Required]
		public string ToDir { get; set; }

		/// <summary>
		/// Unzip a single file into the given directory.
		/// </summary>
		private void UnzipFileToFolder(ZipInputStream zipIn, string fileName,
			long fileSize, string destinationDir, DateTime fileDateTime)
		{
			var newFileName = Path.Combine(destinationDir, fileName);
			//Make sure the directory exists where we are going to create the file.
			Directory.CreateDirectory(Directory.GetParent(newFileName).ToString());
			if (File.Exists(newFileName))
			{
				if ((File.GetAttributes(newFileName) & FileAttributes.ReadOnly) != 0)
					File.SetAttributes(newFileName, FileAttributes.Normal);
				// Do NOT delete it here. File.Create will successfully overwrite it in cases where
				// we may not have permission to delete it, for example, because the OS thinks another
				// process is using it.
				//FileUtils.Delete(newFileName);
			}
			FileStream streamWriter = null;

			try
			{
				try
				{
					streamWriter = File.Create(newFileName);
				}
				catch (Exception)
				{
					GC.Collect();
					GC.WaitForFullGCComplete();
				}
				if (streamWriter == null)
				{
					try
					{
						streamWriter = File.Create(newFileName);
					}
					catch (Exception)
					{
						Log.LogError("Error unzipping {0}.", newFileName);
						return;
					}
				}
				byte[] data = new byte[fileSize];
				while (true)
				{
					fileSize = zipIn.Read(data, 0, data.Length);
					if (fileSize > 0)
						streamWriter.Write(data, 0, (int)fileSize);
					else
						break;
				}
				streamWriter.Close();
			}
			finally
			{
				if (streamWriter != null)
					streamWriter.Dispose();
			}
			File.SetLastWriteTime(newFileName, fileDateTime);
		}
	}
}
