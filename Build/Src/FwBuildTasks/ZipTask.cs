using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using ICSharpCode.SharpZipLib.Zip;

namespace FwBuildTasks
{
	/// <summary>
	/// Zip the specified file(s) into the specified path(s).
	/// </summary>
	public class Zip : Task
	{
		[Required]
		public string Source { get; set; }

		[Required]
		public string Destination { get; set; }

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

			var inputPaths = Source.Split(new[] { ';' });

			if (Destination.EndsWith(".zip"))
				CompressFilesToOneZipFile(inputPaths, Destination);
			else
				CompressFilesToMultipleZipFiles(inputPaths, Destination);

			return true;
		}

		private void CompressFilesToOneZipFile(ICollection<string> inputPaths, string zipFilePath)
		{
			Log.LogMessage(MessageImportance.Normal, "Zipping " + inputPaths.Count + " files to zip file " + zipFilePath);

			using (var fsOut = File.Create(zipFilePath)) // Overwrites previous file
			{
				using (var zipStream = new ZipOutputStream(fsOut))
				{
					foreach (var inputPath in inputPaths)
					{
						zipStream.SetLevel(9); // Highest level of compression

						var inputFileInfo = new FileInfo(inputPath);

						var newEntry = new ZipEntry(inputFileInfo.Name) { DateTime = inputFileInfo.CreationTime };
						zipStream.PutNextEntry(newEntry);

						var buffer = new byte[4096];
						using (var streamReader = File.OpenRead(inputPath))
						{
							ICSharpCode.SharpZipLib.Core.StreamUtils.Copy(streamReader, zipStream, buffer);
						}

						zipStream.CloseEntry();
					}
					zipStream.IsStreamOwner = true;
					zipStream.Close();
				}
			}
		}

		private void CompressFilesToMultipleZipFiles(ICollection<string> inputPaths, string outputFolder)
		{
			Log.LogMessage(MessageImportance.Normal, "Zipping " + inputPaths.Count + " files to zip files in folder " + outputFolder);

			// Create the output folder if it does not exist:
			if (!Directory.Exists(outputFolder))
				Directory.CreateDirectory(outputFolder);

			foreach (var inputPath in inputPaths)
			{
				// Form output zip file full path:
				var outputPath = Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(inputPath) + ".zip");

				using (var fsOut = File.Create(outputPath)) // Overwrites previous file
				{
					using (var zipStream = new ZipOutputStream(fsOut))
					{
						zipStream.SetLevel(9); // Highest level of compression

						var inputFileInfo = new FileInfo(inputPath);

						var newEntry = new ZipEntry(inputFileInfo.Name) { DateTime = inputFileInfo.CreationTime };
						zipStream.PutNextEntry(newEntry);

						var buffer = new byte[4096];
						using (var streamReader = File.OpenRead(inputPath))
						{
							ICSharpCode.SharpZipLib.Core.StreamUtils.Copy(streamReader, zipStream, buffer);
						}

						zipStream.CloseEntry();
						zipStream.IsStreamOwner = true;
						zipStream.Close();
					}
				}
			}
		}
	}
}
