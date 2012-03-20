// --------------------------------------------------------------------------------------------
// <copyright from='2011' to='2011' company='SIL International'>
// 	Copyright (c) 2011, SIL International. All Rights Reserved.
//
// 	Distributable under the terms of either the Common Public License or the
// 	GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
// --------------------------------------------------------------------------------------------
#if __MonoCS__
using System;
using System.IO;
using Gtk;
using NUnit.Framework;
using SIL.Utils.FileDialog;
using SIL.Utils.FileDialog.Linux;

namespace SIL.Utils
{
	[TestFixture]
	public class OpenFileDialogLinuxTests
	{
		private class DummyOpenFileDialogLinux: OpenFileDialogLinux
		{
			public DummyOpenFileDialogLinux()
			{
			}

			public string CalGetCurrentFileName(string fileName)
			{
				return GetCurrentFileName(fileName);
			}

			public FileChooserDialog CallApplyFilter()
			{
				var dlg = CreateFileChooserDialog();
				ApplyFilter(dlg);
				return dlg;
			}
		}

		private class DummyFile: IDisposable
		{
			private string m_FileName;

			public DummyFile(string fileName)
			{
				m_FileName = Path.Combine(Path.GetTempPath(), fileName);
				var stream = File.Create(m_FileName);
				stream.Dispose();
			}

			~DummyFile()
			{
				Dispose(false);
			}

			/// <summary/>
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			/// <summary/>
			protected virtual void Dispose(bool fDisposing)
			{
				System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType() + ". *******");
				if (fDisposing && m_FileName != null)
				{
					File.Delete(m_FileName);
				}
				m_FileName = null;
			}

			public string FileName { get { return Path.GetFileName(m_FileName); }}
			public string Directory { get { return Path.GetDirectoryName(m_FileName); }}
			public string FullPath { get { return m_FileName; }}
			public string FullPathNoExtension { get { return Path.Combine(Directory, Path.GetFileNameWithoutExtension(m_FileName)); } }
		}

		private DummyFile m_File;

		[TestFixtureSetUp]
		public void FixtureSetUp()
		{
			Application.Init();
			m_File = new DummyFile("AddExtension_ExtensionIncluded.txt");
		}

		[TestFixtureTearDown]
		public void FixtureTearDown()
		{
			m_File.Dispose();
		}

		[Test]
		public void AddExtension_ExtensionIncluded()
		{
			using (var dlg = new DummyOpenFileDialogLinux())
			{
				dlg.AddExtension = true;
				Assert.AreEqual(m_File.FullPath, dlg.CalGetCurrentFileName(m_File.FullPath));
			}
		}

		[Test]
		public void AddExtension_CheckFileExists()
		{
			using (var dlg = new DummyOpenFileDialogLinux())
			{
				dlg.AddExtension = true;
				dlg.CheckFileExists = true;
				dlg.DefaultExt = "foo";
				dlg.Filter = "Text files|*.txt";
				dlg.FilterIndex = 1;
				Assert.AreEqual(m_File.FullPath, dlg.CalGetCurrentFileName(m_File.FullPathNoExtension));
			}
		}

		[Test]
		public void AddExtension_CheckFileExists_DifferentFilter()
		{
			using (var dlg = new DummyOpenFileDialogLinux())
			{
				dlg.AddExtension = true;
				dlg.CheckFileExists = true;
				dlg.DefaultExt = "foo";
				dlg.Filter = "Other files|*.bla|Text files|*.txt";
				dlg.FilterIndex = 1;
				Assert.AreEqual(m_File.FullPathNoExtension, dlg.CalGetCurrentFileName(m_File.FullPathNoExtension));
			}
		}

		[Test]
		public void AddExtension_CheckFileExists_MultipleExtensions()
		{
			using (var dlg = new DummyOpenFileDialogLinux())
			{
				dlg.AddExtension = true;
				dlg.CheckFileExists = true;
				dlg.DefaultExt = "foo";
				dlg.Filter = "Other files|*.bla|Text files|*.foo;*.txt";
				dlg.FilterIndex = 2;
				Assert.AreEqual(m_File.FullPath, dlg.CalGetCurrentFileName(m_File.FullPathNoExtension));
			}
		}

		[Test]
		public void AddExtension_CheckFileExists_NoFilter()
		{
			using (var dlg = new DummyOpenFileDialogLinux())
			{
				dlg.AddExtension = true;
				dlg.CheckFileExists = true;
				dlg.DefaultExt = "txt";
				Assert.AreEqual(m_File.FullPath, dlg.CalGetCurrentFileName(m_File.FullPathNoExtension));
			}
		}

		[Test]
		public void AddExtension_NoCheckFileExists_NoFilter()
		{
			using (var dlg = new DummyOpenFileDialogLinux())
			{
				dlg.AddExtension = true;
				dlg.CheckFileExists = false;
				dlg.DefaultExt = "foo";
				Assert.AreEqual(Path.ChangeExtension(m_File.FullPath, "foo"),
					dlg.CalGetCurrentFileName(m_File.FullPathNoExtension));
			}
		}

		[Test]
		public void AddExtension_NoCheckFileExists_WithFilter()
		{
			using (var dlg = new DummyOpenFileDialogLinux())
			{
				dlg.AddExtension = true;
				dlg.CheckFileExists = false;
				dlg.DefaultExt = "foo";
				dlg.Filter = "Other files|*.bla|Text files|*.abc;*.txt";
				dlg.FilterIndex = 2;
				Assert.AreEqual(Path.ChangeExtension(m_File.FullPath, "abc"),
					dlg.CalGetCurrentFileName(m_File.FullPathNoExtension));
			}
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void Filter_Null()
		{
			using (var dlg = new DummyOpenFileDialogLinux())
			{
				dlg.Filter = null;
			}
		}

		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void Filter_Illegal()
		{
			using (var dlg = new DummyOpenFileDialogLinux())
			{
				dlg.Filter = "All files (*.*)";
			}
		}

		private FileFilterInfo CreateFilterInfo(string extension)
		{
			var info = new FileFilterInfo();
			info.Filename = "abc" + extension;
			info.DisplayName = info.Filename;
			info.Contains = FileFilterFlags.Filename | FileFilterFlags.DisplayName;
			return info;
		}

		[Test]
		public void FilterSeparatedBySpaces_FWNX840()
		{
			using (var dlg = new DummyOpenFileDialogLinux())
			{
				dlg.AddExtension = true;
				dlg.CheckFileExists = false;
				dlg.DefaultExt = "foo";
				dlg.Filter = "Other files|*.bla|Text files|*.abc; *.txt";
				dlg.FilterIndex = 2;
				using (var chooserDlg = dlg.CallApplyFilter())
				{
					var filter = chooserDlg.Filter;
					Assert.AreEqual("Text files", filter.Name);
					Assert.IsTrue(filter.Filter(CreateFilterInfo(".abc")));
					Assert.IsTrue(filter.Filter(CreateFilterInfo(".txt")));
					Assert.IsFalse(filter.Filter(CreateFilterInfo(".bla")));
				}
			}
		}

		[Test]
		public void OpenFile()
		{
			using (var dlg = new DummyOpenFileDialogLinux())
			{
				dlg.FileName = m_File.FullPath;
				using (var stream = dlg.OpenFile())
					Assert.NotNull(stream);
			}
		}
	}
}
#endif
