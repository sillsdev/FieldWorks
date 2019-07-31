// Copyright (c) 2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SIL.InstallValidator
{
	public class InstallValidator
	{
		// ENHANCE (Hasso) 2019.07: look for extra files?
		// ENHANCE (Hasso) 2019.07: option to catalog files that have already been installed (for testing against other upgrade scenarios)
		public static void Main(string[] args)
		{
			if (args.Length == 0 || !File.Exists(args[0]))
			{
				// The user double-clicked (poor user won't see this), ran w/o args, or ran w/ invalid args.
				Console.WriteLine("SIL Installation Validator");
				Console.WriteLine("Copyright (c) 2019 SIL International");
				Console.WriteLine(string.Empty);
				Console.WriteLine("This program may be installed in the same directory as another SIL program,");
				Console.WriteLine("but users should not use it. Its purpose is to help verify that the program");
				Console.WriteLine("was installed correctly.");
				Console.WriteLine("Usage:");
				Console.WriteLine(" - Drop installerTestMetadata.csv on this exe to generate a report");
				Console.WriteLine(" - InstallValidator.exe installerTestMetadata.csv [alternate report location]");
				Console.WriteLine("   (for unit tests)");
				return;
			}

			var inFile = args[0];
			var logFile = SafeGetAt(args, 1);

			using (var expected = new StreamReader(inFile))
			{
				var line = expected.ReadLine(); // first line is either headers (tests) or app version info (production)
				logFile = logFile ?? GenerateOutputNameFromInput(line);
				using (var actual = new StreamWriter(logFile))
				{
					actual.WriteLine($"Installation report for: {line}");
					actual.WriteLine(
						"File, Result, Expected Version, Actual Version, Expected Date, Actual Date Modified (UTC)");
					while ((line = expected.ReadLine()) != null)
					{
						var info = line.Split(',');
						if (info.Length < 2)
						{
							actual.WriteLine($"short line, {line}");
							continue;
						}

						var filePath = info[0].Trim();
						actual.Write(filePath);
						var fullPath = Path.Combine(Directory.GetCurrentDirectory(), filePath);
						if (!File.Exists(fullPath))
						{
							actual.WriteLine(", is missing");
							continue;
						}

						var expectedMd5 = info[1].Trim();
						var actualMd5 = ComputeMd5Sum(fullPath);
						if (string.Equals(expectedMd5, actualMd5))
						{
							actual.WriteLine(", was installed correctly");
							continue;
						}

						var expectedVersion = SafeGetAt(info, 2);
						var actualVersion = FileVersionInfo.GetVersionInfo(fullPath).FileVersion;
						var expectedDate = SafeGetAt(info, 3);
						var actualDate = File.GetLastWriteTimeUtc(fullPath);
						actual.WriteLine(
							$", incorrect file is present, {expectedVersion}, {actualVersion}, {expectedDate}, {actualDate:yyyy-MM-dd HH:mm:ss}");
					}
				}
			}

			// If we ran the program by dropping installerTestMetadata.csv, open the report using the default program
			if (args.Length == 1)
			{
				Process.Start(logFile);
			}
		}

		private static string SafeGetAt(string[] arr, int index)
		{
			return arr.Length > index ? arr[index].Trim() : null;
		}

		private static string GenerateOutputNameFromInput(string firstLine)
		{
			var versionPart = firstLine.Split(' ').Last();
			return Path.Combine(Path.GetTempPath(), $"FlexInstallationReport.{versionPart}.csv");
		}

		private static readonly HashAlgorithm Hasher = HashAlgorithm.Create("MD5");

		public static string ComputeMd5Sum(string filename)
		{
			byte[] checksumBytes;
			using (var file = File.OpenRead(filename))
			{
				checksumBytes = Hasher.ComputeHash(file);
			}
			var bldr = new StringBuilder();
			foreach (var b in checksumBytes)
			{
				bldr.AppendFormat("{0:x2}", b);
			}

			return bldr.ToString();
		}
	}
}
