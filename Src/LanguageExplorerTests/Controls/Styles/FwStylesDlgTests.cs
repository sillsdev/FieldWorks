// Copyright (c) 2006-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Reflection;
using LanguageExplorer.Controls.Styles;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FwCoreDlgs.Controls;
using SIL.LCModel.DomainServices;

namespace LanguageExplorerTests.Controls.Styles
{
	/// <summary />
	[TestFixture]
	public class FwStylesDlgTests
	{
		#region Dummy FwStylesDlg class
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
		#endregion

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
	}
}