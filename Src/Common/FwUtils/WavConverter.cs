// Copyright (c) 2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System;
using System.IO;
using System.Security.Cryptography;
using System.Diagnostics;
using NAudio.Lame;
using NAudio.Wave;
using SIL.PlatformUtilities;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// DoesNotExist: The destination file does not exist
	/// NotIdenticalExists: A file with the same name exists
	///  but it has the wrong contents
	/// IdenticalExists: A file with the same name exists
	///  and it has the right contents
	/// </summary>
	public enum SaveFile
	{
		DoesNotExist,
		NotIdenticalExists,
		IdenticalExists
	}

	/// <summary>
	/// Converts a .wav file into an .mp3 file
	/// </summary>
	public static class WavConverter
	{
		/// <summary>
		/// Performs the .wav to .mp3 conversion
		/// If Windows it uses NAudio and LAME to perform the conversion
		/// If Linux it uses LAME through the terminal
		/// </summary>
		/// <param name="sourceFilePath"> Path to wav file </param>
		/// <param name="destinationFilePath"> Path to where the converted file should be saved </param>
		public static void WavToMp3(string sourceFilePath, string destinationFilePath)
		{
			if (string.IsNullOrEmpty(sourceFilePath))
				throw new ArgumentNullException(sourceFilePath);
			if (string.IsNullOrEmpty(destinationFilePath))
				throw new ArgumentNullException(destinationFilePath);
			if (!Path.GetExtension(sourceFilePath).Equals(".wav"))
				throw new Exception("Source file is not a .wav file.");
			if (!File.Exists(sourceFilePath))
				throw new Exception("The source file path is invalid.");

			byte[] wavBytes = ReadWavFile(sourceFilePath);
			byte[] wavHash = MD5.Create().ComputeHash(wavBytes);
			SaveBytes(Path.ChangeExtension(destinationFilePath, ".txt"), wavHash);
			if (Platform.IsWindows)
				ConvertBytesToMp3_Windows(wavBytes, destinationFilePath);
			else
				ConvertWavToMp3Linux(sourceFilePath, destinationFilePath);
		}

		private static void ConvertBytesToMp3_Windows(byte[] wavBytes, string destinationFilePath)
		{
			using (var outputStream = new MemoryStream())
			using (var inputStream = new MemoryStream(wavBytes))
			using (var fileReader = new WaveFileReader(inputStream))
			using (var fileWriter = new LameMP3FileWriter(outputStream, fileReader.WaveFormat, 128))
			{
				fileReader.CopyTo(fileWriter);
				var mp3Bytes = outputStream.ToArray();
				SaveBytes(destinationFilePath, mp3Bytes);
			}
		}

		private static void ConvertWavToMp3Linux(string sourceFilePath, string destinationFilePath)
		{
			// NAudio doesn't work on Linux so the conversion is run through LAME on the terminal
			using (var process = new Process())
			{
				var startInfo = new ProcessStartInfo
				{
					WindowStyle = ProcessWindowStyle.Hidden,
					FileName = "/bin/bash",
					Arguments = string.Format("-c 'lame -h {0} {1}'", sourceFilePath,
						destinationFilePath),
					UseShellExecute = false,
					CreateNoWindow = true
				};
				process.StartInfo = startInfo;
				process.EnableRaisingEvents = true;
				process.Start();
				process.WaitForExit();
			}
		}

		/// <summary>
		/// Takes a file path and returns a byte array containing the contents of that file
		/// </summary>
		/// <param name="path"> SaveFile path </param>
		/// <returns> Contents of the file in a byte array </returns>
		internal static byte[] ReadWavFile(string path)
		{
			byte[] wavFile;
			if (!Path.GetExtension(path).ToLower().Equals(".wav"))
			{
				throw new Exception("SaveFile is not a .wav file.");
			}
			wavFile = File.ReadAllBytes(path);
			return wavFile;
		}

		/// <summary>
		/// Saves the converted byte array/file to the specified file name with an .mp3 extension
		/// </summary>
		/// <param name="destinationPath"> Name of the mp3 file </param>
		/// <param name="bytes"> Contents of the mp3 file </param>
		internal static void SaveBytes(string destinationPath, byte[] bytes)
		{
			string folderPath = Path.GetDirectoryName(destinationPath);
			if (!Directory.Exists(folderPath))
				Directory.CreateDirectory(folderPath);
			if (!Path.GetExtension(destinationPath).Equals(".mp3") && !Path.GetExtension(destinationPath).Equals(".txt"))
				destinationPath = Path.ChangeExtension(destinationPath, ".mp3");
			if (File.Exists(Path.GetFileName(destinationPath)))
				throw new Exception("The conversion failed because the destination file path already exists.");
			File.WriteAllBytes(destinationPath, bytes);
		}

		/// <summary>
		/// Compares the hash of the .wav file to a hash in a text file with the same name as the destination file.
		/// The text file contains the hash of the .wav file that was converted to make the .mp3 file of the
		/// same name. The hashes are compared to determine if the .wav file has already been converted or if the
		/// file merely has the same name but different contents.
		/// </summary>
		/// <param name="sourcePath"> Path to .wav file </param>
		/// <param name="destinationPath"> Path to where the converted file should be saved </param>
		/// <returns></returns>
		public static SaveFile AlreadyExists(string sourcePath, string destinationPath)
		{
			string txtPath = Path.ChangeExtension(destinationPath, ".txt");
			if (!Path.GetExtension(destinationPath).Equals(".mp3"))
				destinationPath = Path.ChangeExtension(destinationPath, ".mp3");
			if (!File.Exists(destinationPath))
			{
				return SaveFile.DoesNotExist;
			}

			byte[] currentWav = MD5.Create().ComputeHash(File.ReadAllBytes(sourcePath));
			byte[] originalWav = { };
			if (File.Exists(txtPath))
				originalWav = File.ReadAllBytes(txtPath);

			if (currentWav.Length != originalWav.Length)
				return SaveFile.NotIdenticalExists;

			int i = -1;
			do
			{
				i++;
			} while (i < currentWav.Length && currentWav[i] == originalWav[i]);
			if (i == currentWav.Length)
				return SaveFile.IdenticalExists;
			return SaveFile.NotIdenticalExists;
		}
	}
}