// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ToolsOptionsDialogTests.cs
// Responsibility: TE Team

using System;
using System.Windows.Forms;

using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using NUnit.Framework;
using SIL.FieldWorks.Test.TestUtils;
using SIL.Utils;
using XCore;
using SIL.CoreImpl;
using System.Collections.Generic;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.TE
{
	#region DummyApp
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyApp : FwApp
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DummyApp() : base(null, null)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new instance of the main window
		/// </summary>
		/// <param name="progressDlg">The progress dialog to use, if needed (can be null).</param>
		/// <param name="fNewCache">Flag indicating whether one-time, application-specific
		/// initialization should be done for this m_cache.</param>
		/// <param name="wndCopyFrom">Must be null for creating the original app window.
		/// Otherwise, a reference to the main window whose settings we are copying.</param>
		/// <param name="fOpeningNewProject"><c>true</c> if opening a brand spankin' new
		/// project</param>
		/// <returns>
		/// New instance of main window if successfull; otherwise <c>null</c>
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override Form NewMainAppWnd(IProgress progressDlg, bool fNewCache,
			Form wndCopyFrom, bool fOpeningNewProject)
		{
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The name of the sample DB for the app.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string SampleDatabase
		{
			get
			{
				CheckDisposed();
				return string.Empty;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Throws an exception
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ProductExecutableFile
		{
			get { throw new NotImplementedException(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the application.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ApplicationName
		{
			get { return "Dummy app"; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Provides a hook for initializing the cache in application-specific ways.
		/// </summary>
		/// <param name="progressDlg">The progress dialog.</param>
		/// <returns>True if the initialization was successful, false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public override bool InitCacheForApp(IThreadedProgress progressDlg)
		{
			throw new NotImplementedException();
		}
	}
	#endregion

	#region DummyWritingSystemManager class
	/// <summary>
	///
	/// </summary>
	public class DummyWritingSystemManager : IWritingSystemManager
	{
		Dictionary<string, IWritingSystem> m_mapIdWs = new Dictionary<string, IWritingSystem>();
		private IWritingSystem m_userWrSys;

		/// <summary>
		/// Constructor
		/// </summary>
		public DummyWritingSystemManager()
		{
		}

		#region IWritingSystemManager Members

		/// <summary></summary>
		public bool CanSave(IWritingSystem ws, out string path)
		{
			throw new NotImplementedException();
		}

		/// <summary></summary>
		public System.Collections.Generic.IEnumerable<IWritingSystem> CheckForNewerGlobalWritingSystems()
		{
			throw new NotImplementedException();
		}

		/// <summary></summary>
		public IWritingSystem Create(LanguageSubtag languageSubtag, ScriptSubtag scriptSubtag, RegionSubtag regionSubtag, VariantSubtag variantSubtag)
		{
			throw new NotImplementedException();
		}

		/// <summary></summary>
		public IWritingSystem Create(string identifier)
		{
			throw new NotImplementedException();
		}

		/// <summary></summary>
		public IWritingSystem CreateFrom(IWritingSystem ws)
		{
			throw new NotImplementedException();
		}

		/// <summary></summary>
		public bool Exists(string identifier)
		{
			IWritingSystem wsT;
			return m_mapIdWs.TryGetValue(identifier, out wsT);
		}

		/// <summary></summary>
		public bool Exists(int handle)
		{
			throw new NotImplementedException();
		}

		/// <summary></summary>
		public IWritingSystem Get(string identifier)
		{
			IWritingSystem wsT;
			if (m_mapIdWs.TryGetValue(identifier, out wsT))
				return wsT;
			wsT = new DummyWritingSystem(this, identifier);
			m_mapIdWs.Add(identifier, wsT);
			return wsT;
		}

		/// <summary></summary>
		public IWritingSystem Get(int handle)
		{
			throw new NotImplementedException();
		}

		/// <summary></summary>
		public bool GetOrSet(string identifier, out IWritingSystem ws)
		{
			throw new NotImplementedException();
		}

		/// <summary></summary>
		public System.Collections.Generic.IEnumerable<IWritingSystem> GlobalWritingSystems
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public System.Collections.Generic.IEnumerable<IWritingSystem> LocalWritingSystems
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public void Replace(IWritingSystem ws)
		{
			throw new NotImplementedException();
		}

		/// <summary></summary>
		public void Save()
		{
			throw new NotImplementedException();
		}

		/// <summary></summary>
		public IWritingSystem Set(string identifier)
		{
			throw new NotImplementedException();
		}

		/// <summary></summary>
		public void Set(IWritingSystem ws)
		{
			throw new NotImplementedException();
		}

		/// <summary></summary>
		public bool TryGet(string identifier, out IWritingSystem ws)
		{
			throw new NotImplementedException();
		}

		/// <summary></summary>
		public bool TryGetOrSet(string identifier, out IWritingSystem ws)
		{
			throw new NotImplementedException();
		}

		/// <summary></summary>
		public IWritingSystem UserWritingSystem
		{
			get
			{
				if (m_userWrSys == null)
					m_userWrSys = Get(Options.UserInterfaceWritingSystem);
				return m_userWrSys;
			}
			set
			{
				m_userWrSys = value;
			}
		}

		/// <summary></summary>
		public string LocalStoreFolder
		{
			set { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public string GetValidLangTagForNewLang(string langName)
		{
			throw new NotImplementedException();
		}
		#endregion

		#region ILgWritingSystemFactory Members

		/// <summary></summary>
		public string GetStrFromWs(int wsId)
		{
			throw new NotImplementedException();
		}

		/// <summary></summary>
		public void GetWritingSystems(SIL.FieldWorks.Common.COMInterfaces.ArrayPtr rgws, int cws)
		{
			throw new NotImplementedException();
		}

		/// <summary></summary>
		public int GetWsFromStr(string bstr)
		{
			throw new NotImplementedException();
		}

		/// <summary></summary>
		public int NumberOfWs
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public int UserWs
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public SIL.FieldWorks.Common.COMInterfaces.ILgCharacterPropertyEngine get_CharPropEngine(int ws)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		///
		/// </summary>
		public SIL.FieldWorks.Common.COMInterfaces.ILgWritingSystem get_Engine(string bstrIcuLocale)
		{
			throw new NotImplementedException();
		}

		/// <summary></summary>
		public SIL.FieldWorks.Common.COMInterfaces.ILgWritingSystem get_EngineOrNull(int ws)
		{
			throw new NotImplementedException();
		}

		/// <summary></summary>
		public SIL.FieldWorks.Common.COMInterfaces.IRenderEngine get_Renderer(int ws, SIL.FieldWorks.Common.COMInterfaces.IVwGraphics _vg)
		{
			throw new NotImplementedException();
		}

		/// <summary></summary>
		public SIL.FieldWorks.Common.COMInterfaces.IRenderEngine get_RendererFromChrp(IVwGraphics vg, ref SIL.FieldWorks.Common.COMInterfaces.LgCharRenderProps _chrp)
		{
			throw new NotImplementedException();
		}
		#endregion
	}
	#endregion

	#region DummyWritingSystem class
	/// <summary>
	///
	/// </summary>
	public class DummyWritingSystem : IWritingSystem
	{
		static int s_hvoGen = 987650001;

		private IWritingSystemManager m_wsManager;
		private string m_id;
		private int m_hvo;

		/// <summary></summary>
		public DummyWritingSystem(IWritingSystemManager manager, string identifier)
		{
			m_wsManager = manager;
			m_id = identifier;
			m_hvo = s_hvoGen++;
		}

		#region IWritingSystem Members

		/// <summary></summary>
		public string Abbreviation
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public Palaso.WritingSystems.Collation.ICollator Collator
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public void Copy(IWritingSystem source)
		{
			throw new NotImplementedException();
		}

		/// <summary></summary>
		public string DisplayLabel
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public string IcuLocale
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public string RFC5646
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public bool IsGraphiteEnabled
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public LanguageSubtag LanguageSubtag
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public string LegacyMapping
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public bool MarkedForDeletion
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public string MatchedPairs
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public bool Modified
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public string PunctuationPatterns
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public string QuotationMarks
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public RegionSubtag RegionSubtag
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public ScriptSubtag ScriptSubtag
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public string SortRules
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public Palaso.WritingSystems.WritingSystemDefinition.SortRulesType SortUsing
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public string ValidChars
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public bool ValidateCollationRules(out string message)
		{
			throw new NotImplementedException();
		}

		/// <summary></summary>
		public VariantSubtag VariantSubtag
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public void WriteLdml(System.Xml.XmlWriter writer)
		{
			throw new NotImplementedException();
		}

		/// <summary></summary>
		public IWritingSystemManager WritingSystemManager
		{
			get { throw new NotImplementedException(); }
		}

		#endregion

		#region ILgWritingSystem Members

		/// <summary></summary>
		public SIL.FieldWorks.Common.COMInterfaces.ILgCharacterPropertyEngine CharPropEngine
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public int CurrentLCID { get; set; }

		/// <summary></summary>
		public string DefaultFontFeatures { get; set; }

		/// <summary></summary>
		public string DefaultFontName
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public int Handle
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public string ISO3
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public string Id
		{
			get { return m_id; }
		}

		/// <summary></summary>
		public void InterpretChrp(ref SIL.FieldWorks.Common.COMInterfaces.LgCharRenderProps _chrp)
		{
			throw new NotImplementedException();
		}

		/// <summary></summary>
		public string Keyboard
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public int LCID
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public string LanguageName
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public bool RightToLeftScript
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public string SpellCheckingId
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		/// <summary></summary>
		public SIL.FieldWorks.Common.COMInterfaces.IRenderEngine get_Renderer(SIL.FieldWorks.Common.COMInterfaces.IVwGraphics _vg)
		{
			throw new NotImplementedException();
		}
		#endregion
	}
	#endregion

	#region ToolsOptionsDlgDummy
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// A dialog window for testing ToolsOptions dialog.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ToolsOptionsDlgDummy : ToolsOptionsDialog
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ToolsOptionsDlgDummy"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ToolsOptionsDlgDummy(IApp app, IHelpTopicProvider helpTopicProvider, IWritingSystemManager wsManager) :
			base(app, helpTopicProvider, wsManager)
		{
		}

		/// <summary></summary>
		public CheckBox CheckPromptEmptyParas
		{
			get
			{
				CheckDisposed();
				return m_chkPromptEmptyParas;
			}
		}
		/// <summary></summary>
		public CheckBox CheckMarkerlessFootnoteIcons
		{
			get
			{
				CheckDisposed();
				return m_chkMarkerlessFootnoteIcons;
			}
		}
		/// <summary></summary>
		public CheckBox CheckSynchFootnoteScroll
		{
			get
			{
				CheckDisposed();
				return m_chkSynchFootnoteScroll;
			}
		}
		/// <summary></summary>
		public Button OKButton
		{
			get
			{
				CheckDisposed();
				return btnOK;
			}
		}
		/// <summary></summary>
		public RadioButton RadioBasic
		{
			get
			{
				CheckDisposed();
				return rdoBasicStyles;
			}
		}
		/// <summary></summary>
		public RadioButton RadioAll
		{
			get
			{
				CheckDisposed();
				return rdoAllStyles;
			}
		}
		/// <summary></summary>
		public RadioButton RadioCustom
		{
			get
			{
				CheckDisposed();
				return rdoCustomList;
			}
		}
		/// <summary></summary>
		public CheckBox CheckUserDefined
		{
			get
			{
				CheckDisposed();
				return chkShowUserDefined;
			}
		}
		/// <summary></summary>
		public ComboBox ComboStyleLevel
		{
			get
			{
				CheckDisposed();
				return cboStyleLevel;
			}
		}
		/// <summary></summary>
		public void ClickOK()
		{
			CheckDisposed();

			btnOK_Click(null, null);
		}
	}
	#endregion

	/// <summary>
	/// Summary description for ToolsOptionsDialogTests.
	/// </summary>
	[TestFixture]
	public class ToolsOptionsDialogTests: BaseTest
	{
		private DummyApp m_app;
		private DummyWritingSystemManager m_wsManager;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public void Setup()
		{
			m_app = new DummyApp();
			m_wsManager = new DummyWritingSystemManager();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tears the down.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public void TearDown()
		{
			m_app.Dispose();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the draft view options portion of the tools dialog
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DraftViewOptionsTest()
		{
			// set the registry settings before creating the dialog
			Options.ShowMarkerlessIconsSetting = true;
			Options.FootnoteSynchronousScrollingSetting = false;
			Options.ShowEmptyParagraphPromptsSetting = true;

			// create a dialog and make sure that the dialog is initialized properly
			using (ToolsOptionsDlgDummy dlg = new ToolsOptionsDlgDummy(m_app, m_app, m_wsManager))
			{
				Assert.IsTrue(dlg.CheckMarkerlessFootnoteIcons.Checked);
				Assert.IsFalse(dlg.CheckSynchFootnoteScroll.Checked);
				Assert.IsTrue(dlg.CheckPromptEmptyParas.Checked);
				//Assert.IsFalse(dlg.CheckShowStyles.Checked);

				// set the registry settings before creating the dialog again
				Options.ShowMarkerlessIconsSetting = false;
				Options.FootnoteSynchronousScrollingSetting = true;
				Options.ShowEmptyParagraphPromptsSetting = false;
			}

			// check the new dialog values
			using (ToolsOptionsDlgDummy dlg = new ToolsOptionsDlgDummy(m_app, m_app, m_wsManager))
			{
				Assert.IsFalse(dlg.CheckMarkerlessFootnoteIcons.Checked);
				Assert.IsTrue(dlg.CheckSynchFootnoteScroll.Checked);
				Assert.IsFalse(dlg.CheckPromptEmptyParas.Checked);
				//Assert.IsTrue(dlg.CheckShowStyles.Checked);

				// set the items in the dialog and then click OK to make sure they get saved correctly
				dlg.CheckMarkerlessFootnoteIcons.Checked = true;
				dlg.CheckSynchFootnoteScroll.Checked = true;
				dlg.CheckPromptEmptyParas.Checked = false;
				//dlg.CheckShowStyles.Checked = false;
				dlg.ClickOK();

				Assert.IsTrue(Options.ShowMarkerlessIconsSetting);
				Assert.IsTrue(Options.FootnoteSynchronousScrollingSetting);
				Assert.IsFalse(Options.ShowEmptyParagraphPromptsSetting);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the style options portion of the tools/options dialog
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void StyleOptionsTest()
		{
			// Make sure all of the settings are the default values in the registry
			Options.ShowTheseStylesSetting = Options.ShowTheseStyles.All;
			Options.ShowStyleLevelSetting = Options.StyleLevel.Basic;
			Options.ShowUserDefinedStylesSetting = true;

			using (ToolsOptionsDlgDummy dlg = new ToolsOptionsDlgDummy(m_app, m_app, m_wsManager))
			{
				// Make sure that all of the settings were initialized correctly.
				Assert.IsTrue(dlg.RadioAll.Checked, "All radio button should be checked");
				Assert.AreEqual(0, dlg.ComboStyleLevel.SelectedIndex);
				Assert.IsTrue(dlg.CheckUserDefined.Checked,
					"Show user defined styles checkbox should be checked");

				// Now, set a different set of values to the registry and see if the dialog gets them correctly
				Options.ShowTheseStylesSetting = Options.ShowTheseStyles.Basic;
				Options.ShowStyleLevelSetting = Options.StyleLevel.Expert;
				Options.ShowUserDefinedStylesSetting = false;
			}

			using (ToolsOptionsDlgDummy dlg = new ToolsOptionsDlgDummy(m_app, m_app, m_wsManager))
			{
				// Make sure that all of the settings were initialized correctly.
				Assert.IsTrue(dlg.RadioBasic.Checked, "Basic radio button should be checked");
				Assert.AreEqual(3, dlg.ComboStyleLevel.SelectedIndex);
				Assert.IsFalse(dlg.CheckUserDefined.Checked,
					"Show user defined styles checkbox should NOT be checked");

				// change the values in the window and then make sure they get saved correctly.
				dlg.RadioCustom.Checked = true;
				dlg.ComboStyleLevel.SelectedIndex = 2;
				dlg.CheckUserDefined.Checked = true;
				dlg.ClickOK();

				// check the registry values to make sure they got set correctly.
				Assert.AreEqual(Options.ShowTheseStyles.Custom, Options.ShowTheseStylesSetting);
				Assert.AreEqual(Options.StyleLevel.Advanced, Options.ShowStyleLevelSetting);
				Assert.IsTrue(Options.ShowUserDefinedStylesSetting,
					"Show User defined styles checkbox was not saved to the registry correctly");
			}
		}
	}
}
