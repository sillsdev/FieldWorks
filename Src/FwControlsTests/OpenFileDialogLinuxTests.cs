// Copyright (c) 2011-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
#if __MonoCS__
using System;
using System.Diagnostics;
using System.IO;
using Gtk;
using NUnit.Framework;
using SIL.FieldWorks.Common.Controls.FileDialog.Linux;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary />
	[TestFixture]
	public class OpenFileDialogLinuxTests
	{
		private class DummyOpenFileDialogLinux: OpenFileDialogLinux
		{
			/// <summary />
			public string CalGetCurrentFileName(string fileName)
			{
				return GetCurrentFileName(fileName);
			}

			/// <summary />
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

			/// <summary />
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
				Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType() + ". *******");
				if (fDisposing && m_FileName != null)
				{
					File.Delete(m_FileName);
				}
				m_FileName = null;
			}

			/// <summary />
			public string FileName { get { return Path.GetFileName(m_FileName); }}
			/// <summary />
			public string Directory { get { return Path.GetDirectoryName(m_FileName); }}
			/// <summary />
			public string FullPath { get { return m_FileName; }}
			/// <summary />
			public string FullPathNoExtension { get { return Path.Combine(Directory, Path.GetFileNameWithoutExtension(m_FileName)); } }
		}

		private DummyFile m_File;

		/// <summary />
		[TestFixtureSetUp]
		public void FixtureSetUp()
		{
			Application.Init();
			m_File = new DummyFile("AddExtension_ExtensionIncluded.txt");
		}

		/// <summary />
		[TestFixtureTearDown]
		public void FixtureTearDown()
		{
			m_File.Dispose();
		}

		/// <summary />
		[Test]
		public void AddExtension_ExtensionIncluded()
		{
			using (var dlg = new DummyOpenFileDialogLinux())
			{
				dlg.AddExtension = true;
				Assert.AreEqual(m_File.FullPath, dlg.CalGetCurrentFileName(m_File.FullPath));
			}
		}

		/// <summary />
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

		/// <summary />
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

		/// <summary />
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

		/// <summary />
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

		/// <summary />
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

		/// <summary />
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

		/// <summary />
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void Filter_Null()
		{
			using (var dlg = new DummyOpenFileDialogLinux())
			{
				dlg.Filter = null;
			}
		}

		/// <summary />
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

		/// <summary />
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

		/// <summary />
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
