// Copyright (c) 2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.TestUtilities;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Tests for WavConverter Class
	/// </summary>
	class WavConverterTests
	{
		private readonly string _goodWavFile = Path.Combine(FwDirectoryFinder.SourceDirectory, "Common/FwUtils/FwUtilsTests/TestData/WavFiles/abu2.wav");
		private readonly string _badWavFile = Path.Combine(FwDirectoryFinder.SourceDirectory, "Common/FwUtils/FwUtilsTests/TestData/WavFiles/bad.wav");

		/// <summary>
		/// Tests that the ReadWavFile method works as expected
		/// </summary>
		[Test]
		public void ReadWavFile_ConvertSingleFile()
		{
			byte[] result = WavConverter.ReadWavFile(_goodWavFile);
			Assert.IsNotEmpty(result, "ReadWavFile did not read the bytes of a file into a byte array.");
		}

		/// <summary>
		/// Tests that a path that leads to nowhere will not get converted
		/// </summary>
		[Test]
		public void ReadWavFile_FilePathDoesNotExist()
		{
			using (var tempDirPath = new TemporaryFolder(Path.GetRandomFileName()))
			{
				string filePath = tempDirPath.Combine(tempDirPath.Path, "TestData/123.wav");
				Assert.Throws<DirectoryNotFoundException>(() => WavConverter.ReadWavFile(filePath), "DirectoryNotFoundException was not thrown.");
			}
		}

		/// <summary>
		/// Tests that a file with an extension besides .wav will not get converted
		/// </summary>
		[Test]
		public void ReadWavFile_WrongExtension()
		{
			Assert.That(() => WavConverter.ReadWavFile(Path.GetTempFileName()),
				Throws.TypeOf<Exception>().With.Message.EqualTo("SaveFile is not a .wav file."), "ReadWavFile didn't break when the SaveFile was not a .wav file.");
		}

		/// <summary>
		/// Tests that the SaveBytes method works as expected
		/// </summary>
		[Test]
		public void SaveBytes_SaveSingleFile()
		{
			using (var tempDirPath = new TemporaryFolder(Path.GetRandomFileName()))
			{
				byte[] bytes = { 177, 209, 137, 61, 204, 127, 103, 88 };
				string fakeFile = tempDirPath.Combine(tempDirPath.Path, "abu3.mp3");
				WavConverter.SaveBytes(fakeFile, bytes);
				Assert.IsTrue(File.Exists(fakeFile), "SaveFile did not successfully save the bytes into a file.");
			}
		}

		/// <summary>
		/// Tests that SaveBytes will change the extension of the destination to .mp3
		/// </summary>
		[Test]
		public void SaveBytes_WrongExtension()
		{
			using (var tempDirPath = new TemporaryFolder(Path.GetRandomFileName()))
			{
				byte[] bytes = { 177, 209, 137, 61, 204, 127, 103, 88 };
				string fakeFile = tempDirPath.Combine(tempDirPath.Path, "abu2.abc");
				WavConverter.SaveBytes(fakeFile, bytes);
				Assert.IsTrue(File.Exists(Path.ChangeExtension(fakeFile, ".mp3")), "SaveBytes did not change the extension of the SaveFile to .mp3.");
			}
		}

		/// <summary>
		/// Tests the WavToMp3 method which in essence tests all of the functionality of the WavConverter class
		/// </summary>
		[Test]
		public void WavToMp3_ConvertAndSaveSingleFiles()
		{
			using (var tempDirPath = new TemporaryFolder(Path.GetRandomFileName()))
			{
				string file = Path.ChangeExtension(Path.GetRandomFileName(), ".mp3");
				string destination = tempDirPath.Combine(tempDirPath.Path, file);
				WavConverter.WavToMp3(_goodWavFile, destination);
				Assert.IsTrue(File.Exists(destination), "WavConverter did not successfully convert the wav file and save it as an mp3 file.");
			}
		}

		/// <summary>
		/// Tests the WavToMp3 method which in essence tests all of the functionality of the WavConverter class
		/// </summary>
		[Test]
		[Platform(Exclude="Linux", Reason = "The message reporting happens only on Windows")]
		public void WavToMp3_ConvertAndSave_ReportsUnsupportedWav()
		{
			var messageBoxAdapter = new MockMessageBox(DialogResult.OK);
			MessageBoxUtils.Manager.SetMessageBoxAdapter(messageBoxAdapter);
			using (var tempDirPath = new TemporaryFolder(Path.GetRandomFileName()))
			{
				var file = Path.ChangeExtension(Path.GetRandomFileName(), ".mp3");
				var destination = tempDirPath.Combine(tempDirPath.Path, file);
				Assert.DoesNotThrow(()=>WavConverter.WavToMp3(_badWavFile, destination));
				Assert.IsFalse(File.Exists(destination), "WavConverter should not have created an mp3 file.");
				Assert.AreEqual(1, messageBoxAdapter.MessagesDisplayed.Count);
				Assert.That(messageBoxAdapter.MessagesDisplayed[0],
					Does.StartWith(string.Format(FwUtilsStrings.ConvertBytesToMp3_BadWavFile, file, string.Empty, string.Empty)));
			}
		}

		/// <summary>
		/// Tests that the destination folder is created if it does not exist
		/// </summary>
		[Test]
		public void WavToMp3_NonExistentFolder()
		{
			using (var tempDirPath = new TemporaryFolder(Path.GetRandomFileName()))
			{
				string newDestination = tempDirPath.Combine(tempDirPath.Path, "New/new/abu2.mp3");
				string directory = tempDirPath.Combine(tempDirPath.Path, "New");
				WavConverter.WavToMp3(_goodWavFile, newDestination);
				Assert.IsTrue(Directory.Exists(directory), "SaveBytes did not create the previously nonexistent folder.");
			}
		}

		/// <summary>
		/// Tests that WavToMp3 throws an exception if the source is null
		/// </summary>
		[Test]
		public void WavToMp3_NullSource()
		{
			using (var tempDirPath = new TemporaryFolder(Path.GetRandomFileName()))
			{
				string file = Path.ChangeExtension(Path.GetRandomFileName(), ".mp3");
				string destination = tempDirPath.Combine(tempDirPath.Path, file);
				Assert.Throws<ArgumentNullException>(() => WavConverter.WavToMp3(null, destination), "WavToMp3 did not fail when it was given a null SourceFilePath.");
			}
		}

		/// <summary>
		/// Tests that WavToMp3 throws an exception if the destination is null
		/// </summary>
		[Test]
		public void WavToMp3_NullDestination()
		{
			using (var tempDirPath = new TemporaryFolder(Path.GetRandomFileName()))
			{
				string file = Path.ChangeExtension(Path.GetRandomFileName(), ".mp3");
				string destination = tempDirPath.Combine(tempDirPath.Path, file);
				Assert.Throws<ArgumentNullException>(() => WavConverter.WavToMp3(destination, null), "WavToMp3 did not fail when it was given a null DestinationFilePath.");
			}
		}

		/// <summary>
		/// Tests that WavToMp3 throws an exception if the source file does not exist
		/// </summary>
		[Test]
		public void WavToMp3_SourceDoesNotExist()
		{
			using (var tempDirPath = new TemporaryFolder(Path.GetRandomFileName()))
			{
				string file = Path.ChangeExtension(Path.GetRandomFileName(), ".mp3");
				string destination = tempDirPath.Combine(tempDirPath.Path, file);
				var ex = Assert.Throws<Exception>(() => WavConverter.WavToMp3(Path.Combine(_goodWavFile, "abcde.wav"), destination));
				Assert.IsTrue(ex.Message.Equals("The source file path is invalid."), "WavToMp3 does not fail when it was given a nonexistent source.");
			}
		}

		/// <summary>
		/// Tests that WavToMp3 throws an exception if the source file is not a .wav file
		/// </summary>
		[Test]
		public void WavToMp3_WrongExtension()
		{
			using (var tempDirPath = new TemporaryFolder(Path.GetRandomFileName()))
			{
				string file = Path.ChangeExtension(Path.GetRandomFileName(), ".mp3");
				string destination = tempDirPath.Combine(tempDirPath.Path, file);
				WavConverter.WavToMp3(_goodWavFile, destination);
				var ex = Assert.Throws<Exception>(() => WavConverter.WavToMp3(destination, destination));
				Assert.IsTrue(ex.Message.Equals("Source file is not a .wav file."), "WavToMp3 did not fail when the source was not a .wav file.");
			}
		}

		/// <summary>
		/// Tests that AlreadyExists returns the DoesNotExist if the destination file does not exist
		/// </summary>
		[Test]
		public void AlreadyExists_DoesNotExist()
		{
			using (var tempDirPath = new TemporaryFolder(Path.GetRandomFileName()))
			{
				string file = Path.ChangeExtension(Path.GetRandomFileName(), ".mp3");
				string destination = tempDirPath.Combine(tempDirPath.Path, file);
				Assert.IsTrue(WavConverter.AlreadyExists(_goodWavFile, destination) == SaveFile.DoesNotExist, "AlreadyExists did not recognize that the destination does not already exist.");
			}
		}

		/// <summary>
		/// Tests that AlreadyExists returns Identical Exists if the destination file exists and has
		/// the contents of the converted .wav file
		/// </summary>
		[Test]
		public void AlreadyExists_IdenticalExists()
		{
			using (var tempDirPath = new TemporaryFolder(Path.GetRandomFileName()))
			{
				string file = Path.ChangeExtension(Path.GetRandomFileName(), ".mp3");
				string destination = tempDirPath.Combine(tempDirPath.Path, file);
				WavConverter.WavToMp3(_goodWavFile, destination);
				Assert.IsTrue(WavConverter.AlreadyExists(_goodWavFile, destination) == SaveFile.IdenticalExists, "AlreadyExists did not recognize that the converted file already exists.");
			}
		}

		/// <summary>
		/// Tests that AlreadyExists returns NotidenticalExists if the destination file exists but
		/// does not have the contents of the converted .wav file
		/// </summary>
		[Test]
		public void AlreadyExists_NonIdenticalExists()
		{
			using (var tempDirPath = new TemporaryFolder(Path.GetRandomFileName()))
			{
				byte[] bytes = { 177, 209, 137, 61, 204, 127, 103, 88 };
				string fakeFile = tempDirPath.Combine(tempDirPath.Path, "abu2.mp3");
				WavConverter.SaveBytes(fakeFile, bytes);
				Assert.IsTrue(WavConverter.AlreadyExists(_goodWavFile, fakeFile) == SaveFile.NotIdenticalExists, "AlreadyExists did not recognize that the destination exists but is not the converted version of the source.");
			}
		}

		private sealed class MockMessageBox : IMessageBox
		{
			private DialogResult _mockResult;
			public List<string> MessagesDisplayed { get; }

			public MockMessageBox(DialogResult mockResult)
			{
				_mockResult = mockResult;
				MessagesDisplayed = new List<string>();
			}

			public DialogResult Show(IWin32Window owner, string text, string caption, MessageBoxButtons buttons,
				MessageBoxIcon icon)
			{
				MessagesDisplayed.Add(text);
				return _mockResult;
			}

			public DialogResult Show(IWin32Window owner, string text, string caption, MessageBoxButtons buttons,
				MessageBoxIcon icon, MessageBoxDefaultButton defaultButton, MessageBoxOptions options,
				string helpFilePath, HelpNavigator navigator, object param)
			{
				MessagesDisplayed.Add(text);
				return _mockResult;
			}
		}
	}
}