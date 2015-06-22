// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// Original author: MarkS 2010-11-12 MainWindowDelegateTests.cs
using System;
using System.IO;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.Utils;

namespace SIL.FieldWorks.Common.Framework
{
	/// <summary/>
	public class DummyMainWindowDelegateCallbacks : IMainWindowDelegateCallbacks
	{
		#region IMainWindowDelegateCallbacks implementation
		/// <summary/>
		public void OnStylesRenamedOrDeleted()
		{
			throw new NotImplementedException();
		}

		/// <summary/>
		public bool PopulateParaStyleListOverride()
		{
			throw new NotImplementedException();
		}

		/// <summary/>
		public FdoCache Cache { get; set; }

		/// <summary/>
		public FwCoreDlgControls.StyleComboListHelper ParaStyleListHelper {
			get {
				throw new NotImplementedException();
			}
		}

		/// <summary/>
		public FwCoreDlgControls.StyleComboListHelper CharStyleListHelper {
			get {
				throw new NotImplementedException();
			}
		}

		/// <summary/>
		public IRootSite ActiveView {
			get {
				throw new NotImplementedException();
			}
		}

		/// <summary/>
		public ComboBox WritingSystemSelector {
			get {
				throw new NotImplementedException();
			}
		}

		/// <summary/>
		public FwEditingHelper FwEditingHelper {
			get {
				throw new NotImplementedException();
			}
		}

		/// <summary/>
		public EditingHelper EditingHelper {
			get {
				throw new NotImplementedException();
			}
		}

		/// <summary/>
		public FwStyleSheet StyleSheet {
			get {
				throw new NotImplementedException();
			}
		}

		/// <summary/>
		public FwStyleSheet ActiveStyleSheet {
			get {
				throw new NotImplementedException();
			}
		}

		/// <summary/>
		public int StyleSheetOwningFlid {
			get {
				throw new NotImplementedException();
			}
		}

		/// <summary/>
		public int HvoAppRootObject {
			get {
				throw new NotImplementedException();
			}
		}

		/// <summary/>
		public int MaxStyleLevelToShow {
			get {
				throw new NotImplementedException();
			}
		}

		/// <summary/>
		public bool ShowTEStylesComboInStylesDialog {
			get {
				throw new NotImplementedException();
			}
		}

		/// <summary/>
		public bool CanSelectParagraphBackgroundColor {
			get {
				throw new NotImplementedException();
			}
		}
		#endregion

		#region IWin32Window implementation
		/// <summary/>
		public IntPtr Handle {
			get {
				throw new NotImplementedException();
			}
		}
		#endregion
	}

	/// <summary/>
	public class DummyFwApp : FwApp
	{
		/// <summary/>
		public DummyFwApp() : base(null, null)
		{
		}

		#region abstract members of SIL.FieldWorks.Common.Framework.FwApp
		/// <summary/>
		public override bool InitCacheForApp(IThreadedProgress progressDlg)
		{
			throw new System.NotImplementedException();
		}

		/// <summary/>
		public override string ProductExecutableFile
		{
			get {
				throw new System.NotImplementedException();
			}
		}

		/// <summary/>
		public override string ApplicationName
		{
			get {
				return FwUtils.FwUtils.ksFlexAppName;
			}
		}

		/// <summary/>
		public override Form NewMainAppWnd(IProgress progressDlg, bool fNewCache,
			Form wndCopyFrom, bool fOpeningNewProject)
		{
			throw new System.NotImplementedException();
		}
		#endregion
	}

	/// <summary/>
	[TestFixture]
	public class MainWindowDelegateTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private string m_projectName;
		private string m_pathExtension;
		private string m_expectedPathDesktop;
		private string m_expectedPathTmp;
		private MockFileOS m_fileOs;

		/// <summary/>
		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();

			m_fileOs = new MockFileOS();
			FileUtils.Manager.SetFileAdapter(m_fileOs);

			m_projectName = Cache.ProjectId.Handle;
			m_pathExtension = ".desktop";
			var pathDesktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
			var pathTmp = Path.GetTempPath();
			m_expectedPathDesktop = Path.Combine(pathDesktop, m_projectName + m_pathExtension);
			m_expectedPathTmp = Path.Combine(pathTmp, m_projectName + m_pathExtension);

			// Create these directories in mock filesystem
			m_fileOs.ExistingDirectories.Add(pathDesktop);
			m_fileOs.ExistingDirectories.Add(pathTmp.TrimEnd(
				new char[] {Path.DirectorySeparatorChar}));
		}
	}
}
