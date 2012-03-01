// Copyright (c) 2010, SIL International. All Rights Reserved.
//
// Distributable under the terms of either the Common Public License or the
// GNU Lesser General Public License, as specified in the LICENSING.txt file.
//
// Original author: MarkS 2010-11-12 MainWindowDelegateTests.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Resources;
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
		public override bool InitCacheForApp(IProgress progressDlg)
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

		#region CreateShortcut tests
		/// <summary>
		/// If no launcher was created yet. One is created.
		/// </summary>
		[Test]
		[Platform(Include="Linux")]
		public void CreateShortcut_CreateProjectLauncher_NotExist_Created()
		{
			Assert.That(FileUtils.DirectoryExists(
				Path.GetDirectoryName(m_expectedPathDesktop)), Is.True,
				"Unit test error. Trying to create a launcher in a nonexistent directory.");
			Assert.That(FileUtils.FileExists(m_expectedPathDesktop), Is.False,
				"Launcher shouldn't exist yet.");
			AssertCreateProjectLauncherWorks(m_expectedPathDesktop);
		}

		/// <summary>
		/// If a launcher already exists on desktop, create another one of similar name.
		/// Don't overwrite existing one or do nothing, since the project_name.desktop file
		/// may not actually be a FW launcher; also, doing nothing could be confusing to a user.
		/// </summary>
		[Test]
		[Platform(Include="Linux")]
		public void CreateShortcut_CreateProjectLauncher_AlreadyExists_AnotherCreated()
		{
			m_fileOs.AddExistingFile(m_expectedPathDesktop);
			var tail = "-2";
			var expectedPath = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
				m_projectName + tail + m_pathExtension);

			Assert.That(FileUtils.FileExists(expectedPath), Is.False,
				"Launcher shouldn't exist yet.");

			AssertCreateProjectLauncherWorks(expectedPath);
		}

		/// <summary>
		/// If no Desktop directory already exists, a "Create Shortcut on Desktop" menu
		/// item doesn't seem like the right thing to create it.
		/// </summary>
		[Test]
		[Platform(Include="Linux")]
		public void CreateShortcut_inNonExistentDirectory_notCreatedAndNoThrow()
		{
			var nonexistentDir = "/nonexistent";
			var path = Path.Combine(nonexistentDir, m_projectName + m_pathExtension);

			Assert.That(FileUtils.DirectoryExists(nonexistentDir), Is.False,
				"Unit test error. Should be using a nonexistent directory.");

			using (var app = new DummyFwApp())
			{
				var dummyWindow = new DummyMainWindowDelegateCallbacks();
				dummyWindow.Cache = Cache;

				var window = new MainWindowDelegate(dummyWindow);
				window.App = app;

				Assert.DoesNotThrow(() => {
					window.CreateShortcut(nonexistentDir);
				});
			}

			Assert.That(FileUtils.DirectoryExists(nonexistentDir), Is.False,
				"Nonexistent directory should not have been made to hold launcher.");
		}

		/// <summary>
		/// .desktop file should not begin with a BOM or it doesn't work in Gnome.
		/// </summary>
		[Test]
		[Platform(Include="Linux")]
		public void CreateProjectLauncher_noBOM()
		{
			BinaryReader reader = null;
			try
			{
				// MockFileOS doesn't introduce the BOM like the real thing in mono, so
				// use the real thing for this test.
				FileUtils.Manager.Reset();

				Assert.That(FileUtils.FileExists(m_expectedPathTmp), Is.False,
					"Launcher shouldn't exist yet.");
				AssertCreateProjectLauncherWorks(m_expectedPathTmp);

				Assert.That(FileUtils.EncodingFromBOM(m_expectedPathTmp),
					Is.EqualTo(Encoding.ASCII),
					"Desktop launcher should not begin with a Unicode BOM.");
			}
			finally
			{
				if (reader != null)
					reader.Close();
				FileUtils.Delete(m_expectedPathTmp);
			}
		}

		/// <summary>
		/// .desktop file needs to be chmod +x
		/// </summary>
		[Test]
		[Platform(Include="Linux")]
		public void CreateProjectLauncher_isExecutable()
		{
			try
			{
				// Use a real file for test since using a system call
				FileUtils.Manager.Reset();

				Assert.That(FileUtils.FileExists(m_expectedPathTmp), Is.False,
					"Launcher shouldn't exist yet.");
				AssertCreateProjectLauncherWorks(m_expectedPathTmp);

				Assert.That(FileUtils.IsExecutable(m_expectedPathTmp), Is.True,
					"Launcher needs to be +x executable.");
			}
			finally
			{
				FileUtils.Delete(m_expectedPathTmp);
			}
		}

		/// <summary>
		/// Helper method for CreateProjectLauncher tests.
		/// </summary>
		private void AssertCreateProjectLauncherWorks(string expectedPath)
		{
			using (var app = new DummyFwApp())
			{
				var dummyWindow = new DummyMainWindowDelegateCallbacks();
				dummyWindow.Cache = Cache;

				var window = new MainWindowDelegate(dummyWindow);
				window.App = app;

				window.CreateShortcut(Path.GetDirectoryName(expectedPath));

				Assert.That(FileUtils.FileExists(expectedPath), Is.True,
					String.Format("Expected file does not exist: {0}", expectedPath));

				string actualLauncherData;
				byte[] launcherBuffer;
				using (var launcher = FileUtils.OpenStreamForRead(expectedPath))
				{
					launcherBuffer = new byte[launcher.Length];
					launcher.Read(launcherBuffer, 0, launcherBuffer.Length);
				}
				var enc = new UTF8Encoding(false);
				actualLauncherData = enc.GetString(launcherBuffer);

				var description = ResourceHelper.FormatResourceString(
					"kstidCreateShortcutLinkDescription", Cache.ProjectId.UiName,
					app.ApplicationName);
				string expectedLauncherData = String.Format(
					"[Desktop Entry]{1}" +
					"Version=1.0{1}" +
					"Terminal=false{1}" +
					"Exec=fieldworks-flex -db \"{0}\" -s \"\"{1}" +
					"Icon=fieldworks-flex{1}" +
					"Type=Application{1}" +
					"Name={0}{1}" +
					"Comment=" + description + "{1}",
					m_projectName, Environment.NewLine);
				Assert.That(actualLauncherData, Is.EqualTo(expectedLauncherData));
			}
		}
		#endregion // CreateShortcut tests
	}
}
