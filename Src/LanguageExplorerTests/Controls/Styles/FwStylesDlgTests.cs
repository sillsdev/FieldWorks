<<<<<<< HEAD:Src/LanguageExplorerTests/Controls/Styles/FwStylesDlgTests.cs
// Copyright (c) 2006-2020 SIL International
||||||| f013144d5:Src/FwCoreDlgs/FwCoreDlgsTests/FwStylesDlgTests.cs
// Copyright (c) 2006-2017 SIL International
=======
// Copyright (c) 2006-2021 SIL International
>>>>>>> develop:Src/FwCoreDlgs/FwCoreDlgsTests/FwStylesDlgTests.cs
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Reflection;
<<<<<<< HEAD:Src/LanguageExplorerTests/Controls/Styles/FwStylesDlgTests.cs
using LanguageExplorer.Controls.Styles;
||||||| f013144d5:Src/FwCoreDlgs/FwCoreDlgsTests/FwStylesDlgTests.cs
=======
using System.Windows.Forms;
>>>>>>> develop:Src/FwCoreDlgs/FwCoreDlgsTests/FwStylesDlgTests.cs
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
<<<<<<< HEAD:Src/LanguageExplorerTests/Controls/Styles/FwStylesDlgTests.cs
||||||| f013144d5:Src/FwCoreDlgs/FwCoreDlgsTests/FwStylesDlgTests.cs
using SIL.FieldWorks.FwCoreDlgControls;
=======
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.FwCoreDlgControls;
using SIL.LCModel;
>>>>>>> develop:Src/FwCoreDlgs/FwCoreDlgsTests/FwStylesDlgTests.cs
using SIL.LCModel.DomainServices;

namespace LanguageExplorerTests.Controls.Styles
{
<<<<<<< HEAD:Src/LanguageExplorerTests/Controls/Styles/FwStylesDlgTests.cs
	/// <summary />
||||||| f013144d5:Src/FwCoreDlgs/FwCoreDlgsTests/FwStylesDlgTests.cs
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
=======
	/// <summary/>
>>>>>>> develop:Src/FwCoreDlgs/FwCoreDlgsTests/FwStylesDlgTests.cs
	[TestFixture]
	public class FwStylesDlgTests : ScrInMemoryLcmTestBase
	{
<<<<<<< HEAD:Src/LanguageExplorerTests/Controls/Styles/FwStylesDlgTests.cs
||||||| f013144d5:Src/FwCoreDlgs/FwCoreDlgsTests/FwStylesDlgTests.cs
		#region Dummy FwStylesDlg class
		private class DummyFwStylesDlg : FwStylesDlg
		{
			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="T:DummyFwStylesDlg"/> class.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public DummyFwStylesDlg()
				: base(null, null, new LcmStyleSheet(), false, false, "TestDefault", 0,
				MsrSysType.Cm, string.Empty, string.Empty, 0, null, null)
			{
				m_generalTab.RenamedStyles = m_renamedStyles;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Calls the save renamed style.
			/// </summary>
			/// <param name="oldName">The old name.</param>
			/// <param name="newName">The new name.</param>
			/// --------------------------------------------------------------------------------
			public void CallSaveRenamedStyle(string oldName, string newName)
			{
				Type t = typeof(FwGeneralTab);

				MethodInfo methodInfo = t.GetMethod("SaveRenamedStyle",
					BindingFlags.NonPublic | BindingFlags.Instance);

				if (methodInfo != null)
					methodInfo.Invoke(m_generalTab, new object[] { oldName, newName });
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Calls the save deleted style.
			/// </summary>
			/// <param name="styleName">Name of the style.</param>
			/// --------------------------------------------------------------------------------
			public void CallSaveDeletedStyle(string styleName)
			{
				base.SaveDeletedStyle(styleName);
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets the deleted style names.
			/// </summary>
			/// <value>The deleted style names.</value>
			/// --------------------------------------------------------------------------------
			public ISet<string> DeletedStyleNames
			{
				get { return m_deletedStyleNames; }
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets the renamed style names.
			/// </summary>
			/// <value>The renamed style names.</value>
			/// --------------------------------------------------------------------------------
			public Dictionary<string, string> RenamedStyleNames
			{
				get { return m_renamedStyles; }
			}
		}
		#endregion

		/// ------------------------------------------------------------------------------------
=======
		#region Dummy FwStylesDlg class
		private class DummyFwStylesDlg : FwStylesDlg
		{
			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="T:DummyFwStylesDlg"/> class.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public DummyFwStylesDlg()
				: base(null, null, new LcmStyleSheet(), false, false, "TestDefault", 0,
				MsrSysType.Cm, string.Empty, string.Empty, 0, null, null)
			{
				m_generalTab.RenamedStyles = m_renamedStyles;
			}

			/// <inheritdoc/>
			public DummyFwStylesDlg(IVwRootSite rootSite, LcmCache cache, LcmStyleSheet styleSheet,
				bool defaultRightToLeft, bool showBiDiLabels, string normalStyleName,
				int customUserLevel, MsrSysType userMeasurementType, string paraStyleName,
				string charStyleName, int hvoRootObject, IApp app,
				IHelpTopicProvider helpTopicProvider)
				: base(rootSite, cache, styleSheet, defaultRightToLeft, showBiDiLabels,
					normalStyleName, customUserLevel, userMeasurementType, paraStyleName,
					charStyleName, hvoRootObject, app, helpTopicProvider)
			{
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Calls the save renamed style.
			/// </summary>
			/// <param name="oldName">The old name.</param>
			/// <param name="newName">The new name.</param>
			/// --------------------------------------------------------------------------------
			public void CallSaveRenamedStyle(string oldName, string newName)
			{
				Type t = typeof(FwGeneralTab);

				MethodInfo methodInfo = t.GetMethod("SaveRenamedStyle",
					BindingFlags.NonPublic | BindingFlags.Instance);

				if (methodInfo != null)
					methodInfo.Invoke(m_generalTab, new object[] { oldName, newName });
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Calls the save deleted style.
			/// </summary>
			/// <param name="styleName">Name of the style.</param>
			/// --------------------------------------------------------------------------------
			public void CallSaveDeletedStyle(string styleName)
			{
				SaveDeletedStyle(styleName);
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets the deleted style names.
			/// </summary>
			/// <value>The deleted style names.</value>
			/// --------------------------------------------------------------------------------
			public ISet<string> DeletedStyleNames
			{
				get { return m_deletedStyleNames; }
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets the renamed style names.
			/// </summary>
			/// <value>The renamed style names.</value>
			/// --------------------------------------------------------------------------------
			public Dictionary<string, string> RenamedStyleNames
			{
				get { return m_renamedStyles; }
			}

			/// <summary/>
			public TabControl TabControl => m_tabControl;

			/// <summary/>
			public TabPage TbFont => m_tbFont;

			/// <summary/>
			public TabPage TbGeneral => m_tbGeneral;
		}
		#endregion

		/// ------------------------------------------------------------------------------------
>>>>>>> develop:Src/FwCoreDlgs/FwCoreDlgsTests/FwStylesDlgTests.cs
		/// <summary>
		/// Tests renaming and deleting styles.
		/// </summary>
		[Test]
		public void RenameAndDeleteStyles()
		{
			using (var dlg = new DummyFwStylesDlg())
			{
				var deletedStyles = dlg.DeletedStyleNames;
				var renamedStyles = dlg.RenamedStyleNames;

				// Add a bunch of things to the deleted list
				dlg.CallSaveDeletedStyle("style 1");
				dlg.CallSaveDeletedStyle("out of style");
				dlg.CallSaveDeletedStyle("no style");
				dlg.CallSaveDeletedStyle("no style");

				// rename a style twice which should result in one entry
				// results in rename name 1 -> name 3
				dlg.CallSaveRenamedStyle("name 1", "name 2");
				dlg.CallSaveRenamedStyle("name 2", "name 3");

				// rename a style then back to the old name which should not result in an entry
				// no action
				dlg.CallSaveRenamedStyle("old style", "new style");
				dlg.CallSaveRenamedStyle("new style", "old style");

				// rename a style, then delete it
				// results in delete "deleted style"
				dlg.CallSaveRenamedStyle("deleted style", "new deleted style");
				dlg.CallSaveDeletedStyle("new deleted style");

				// just a basic rename
				// results in rename
				dlg.CallSaveRenamedStyle("my style", "your style");

				// delete a style, then rename another style to the deleted name
				// results in deletion of "my recurring style" and rename of "my funny style".
				dlg.CallSaveDeletedStyle("my recurring style");
				dlg.CallSaveRenamedStyle("my funny style", "my recurring style");

				// Check the deleted styles set
				Assert.AreEqual(5, deletedStyles.Count);
				Assert.IsTrue(deletedStyles.Contains("style 1"));
				Assert.IsTrue(deletedStyles.Contains("out of style"));
				Assert.IsTrue(deletedStyles.Contains("no style"));
				Assert.IsTrue(deletedStyles.Contains("deleted style"));
				Assert.IsTrue(deletedStyles.Contains("my recurring style"));

				// Check the renamed styles list
				Assert.AreEqual(3, renamedStyles.Count);
				Assert.AreEqual("name 1", renamedStyles["name 3"]);
				Assert.AreEqual("my style", renamedStyles["your style"]);
				Assert.AreEqual("my funny style", renamedStyles["my recurring style"]);
			}
		}
<<<<<<< HEAD:Src/LanguageExplorerTests/Controls/Styles/FwStylesDlgTests.cs

		private sealed class DummyFwStylesDlg : FwStylesDlg
		{
			/// <summary />
			internal DummyFwStylesDlg()
				: base(null, null, new LcmStyleSheet(), false, false, "TestDefault", MsrSysType.Cm, string.Empty, string.Empty, null, null)
			{
				m_generalTab.RenamedStyles = m_renamedStyles;
			}

			/// <summary>
			/// Calls the save renamed style.
			/// </summary>
			internal void CallSaveRenamedStyle(string oldName, string newName)
			{
				var t = typeof(FwGeneralTab);
				var methodInfo = t.GetMethod("SaveRenamedStyle", BindingFlags.NonPublic | BindingFlags.Instance);
				if (methodInfo != null)
				{
					methodInfo.Invoke(m_generalTab, new object[] { oldName, newName });
				}
			}

			/// <summary>
			/// Calls the save deleted style.
			/// </summary>
			internal void CallSaveDeletedStyle(string styleName)
			{
				SaveDeletedStyle(styleName);
			}

			/// <summary>
			/// Gets the deleted style names.
			/// </summary>
			internal ISet<string> DeletedStyleNames => m_deletedStyleNames;

			/// <summary>
			/// Gets the renamed style names.
			/// </summary>
			internal Dictionary<string, string> RenamedStyleNames => m_renamedStyles;
		}
||||||| f013144d5:Src/FwCoreDlgs/FwCoreDlgsTests/FwStylesDlgTests.cs
=======

		/// <summary>
		/// Tests that all tabs are hidden if no style is selected (if a placeholder style name is passed) (LT-20566),
		/// and that the correct tabs are shown for paragraph and character styles.
		/// </summary>
		[Platform(Exclude = "Linux", Reason = "seems to require extra setup on Linux")]
		[TestCase("<not a real style>", 1)]
		[TestCase("Default Paragraph Characters", 1)]
		[TestCase("Hyperlink", 2)]
		[TestCase("Normal", 5)]
		public void NoStyleSelected_AllTabsHidden(string selectedStyle, int visibleTabs)
		{
			var stylesheet = new LcmStyleSheet();
			stylesheet.Init(Cache, Cache.LangProject.Hvo, LangProjectTags.kflidStyles);

			var sut = new DummyFwStylesDlg(null, Cache, stylesheet, false, false, "Normal", 0, MsrSysType.Cm,
				selectedStyle, string.Empty, 0, null, null);

			Assert.That(sut.TabControl.TabCount, Is.EqualTo(visibleTabs));
			Assert.That(sut.TabControl.TabPages, Contains.Item(sut.TbGeneral), "The General tab should always be visible");
			if (visibleTabs > 1)
			{
				Assert.That(sut.TabControl.TabPages, Contains.Item(sut.TbFont), "The Font tab should be visible for both character and paragraph styles");
			}
		}
>>>>>>> develop:Src/FwCoreDlgs/FwCoreDlgsTests/FwStylesDlgTests.cs
	}
}