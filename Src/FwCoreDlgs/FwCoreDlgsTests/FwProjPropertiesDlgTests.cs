// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TestFwProjPropertiesDlg.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

using NMock;
using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Test.TestUtils;


namespace SIL.FieldWorks.FwCoreDlgs
{
	#region DummyFwProjPropertiesDlg Class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyFwProjPropertiesDlg : FwProjPropertiesDlg
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:DummyFwProjPropertiesDlg"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="tool">The tool.</param>
		/// <param name="strmLog">The STRM log.</param>
		/// <param name="hvoProj">The hvo proj.</param>
		/// <param name="hvoRoot">The hvo root.</param>
		/// <param name="wsUser">The ws user.</param>
		/// ------------------------------------------------------------------------------------
		public DummyFwProjPropertiesDlg(FdoCache cache, IFwTool tool, IStream strmLog,
			int hvoProj, int hvoRoot, int wsUser)
			: base(cache, null, tool, null, strmLog, hvoProj, hvoRoot, wsUser, null)
		{
		}

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Button OkButton
		{
			get
			{
				CheckDisposed();
				return m_btnOK;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public CheckedListBox VernWsList
		{
			get
			{
				CheckDisposed();
				return m_lstVernWs;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public CheckedListBox AnalWsList
		{
			get
			{
				CheckDisposed();
				return m_lstAnalWs;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Button VernWsMoveDownButton
		{
			get
			{
				CheckDisposed();
				return m_btnVernMoveDown;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Button VernWsMoveUpButton
		{
			get
			{
				CheckDisposed();
				return m_btnVernMoveUp;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Button VernWsDeleteButton
		{
			get
			{
				CheckDisposed();
				return m_btnDelVernWs;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Button VernWsModifyButton
		{
			get
			{
				CheckDisposed();
				return m_btnModifyVernWs;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Button AnalWsMoveDownButton
		{
			get
			{
				CheckDisposed();
				return m_btnAnalMoveDown;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Button AnalWsMoveUpButton
		{
			get
			{
				CheckDisposed();
				return m_btnAnalMoveUp;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Button AnalWsDeleteButton
		{
			get
			{
				CheckDisposed();
				return m_btnDelAnalWs;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Button AnalWsModifyButton
		{
			get
			{
				CheckDisposed();
				return m_btnModifyAnalWs;
			}
		}
		#endregion

		#region Button press simulations
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SimulateOKButtonPress()
		{
			CheckDisposed();

			m_btnOK_Click(null, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SimulateVernDownButtonPress()
		{
			CheckDisposed();

			m_btnVernMoveDown_Click(null, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SimulateVernUpButtonPress()
		{
			CheckDisposed();

			m_btnVernMoveUp_Click(null, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SimulateAnalDownButtonPress()
		{
			CheckDisposed();

			m_btnAnalMoveDown_Click(null, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SimulateAnalUpButtonPress()
		{
			CheckDisposed();

			m_btnAnalMoveUp_Click(null, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SimulateAnalDeletePress()
		{
			CheckDisposed();

			m_btnDelAnalWs_Click(null, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SimulateVernDeletePress()
		{
			CheckDisposed();

			m_btnDelVernWs_Click(null, null);
		}
		#endregion

		#region Other simulations
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SimulateAnalAddingWs(ILgWritingSystem ws)
		{
			CheckDisposed();

			AddWsToList(ws, m_lstAnalWs);
			UpdateButtons(m_lstAnalWs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SimulateVernAddingWs(ILgWritingSystem ws)
		{
			CheckDisposed();

			AddWsToList(ws, m_lstVernWs);
			UpdateButtons(m_lstVernWs);
		}
		#endregion

		#region Overridden methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Brings the up english warning MSG.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected override DialogResult BringUpEnglishWarningMsg()
		{
			return DialogResult.Yes;
		}
		#endregion
	}

	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for TestFwProjPropertiesDlg.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FwProjPropertiesDlgTests: BaseTest
	{
		private InMemoryFdoCache m_inMemoryCache;
		private DummyFwProjPropertiesDlg m_dlg;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ILangProject LangProj
		{
			get {return m_inMemoryCache.Cache.LangProject;}
		}

		#region Setup Helpers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="hvos"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private void AddWritingSystems(int[] hvos)
		{
			string langId = string.Empty;
			string langName = string.Empty;

			foreach (int hvo in hvos)
			{
				if (hvo == InMemoryFdoCache.s_wsHvos.En)
				{
					langId = "en";
					langName = "English";
				}
				else if (hvo == InMemoryFdoCache.s_wsHvos.Es)
				{
					langId = "es";
					langName = "Spanish";
				}
				else if (hvo == InMemoryFdoCache.s_wsHvos.Fr)
				{
					langId = "Fr";
					langName = "French";
				}
				else if (hvo == InMemoryFdoCache.s_wsHvos.Ipa)
				{
					langId = "en__IPA";
					langName = "English IPA";
				}
				LgWritingSystem ws = new LgWritingSystem(m_inMemoryCache.Cache, hvo);
				ws.ICULocale = langId;
				m_inMemoryCache.Cache.SetMultiUnicodeAlt(hvo,
					(int)LgWritingSystem.LgWritingSystemTags.kflidName, InMemoryFdoCache.s_wsHvos.En,
					langName);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetupVernWss()
		{
			// Setup so the CurVernWssRS returns the proper sequence.
			int[] hvos = new int[] {(int)InMemoryFdoCache.s_wsHvos.En, (int)InMemoryFdoCache.s_wsHvos.Fr};
			AddWritingSystems(hvos);

			m_inMemoryCache.CacheAccessor.CacheVecProp(m_inMemoryCache.Cache.LangProject.Hvo,
				(int)LangProject.LangProjectTags.kflidCurVernWss,
				hvos, hvos.Length);

			// Setup so the VernWssRC returns the proper collection.
			hvos = new int[] {(int)InMemoryFdoCache.s_wsHvos.En, (int)InMemoryFdoCache.s_wsHvos.Fr,
								 (int)InMemoryFdoCache.s_wsHvos.Ipa, (int)InMemoryFdoCache.s_wsHvos.Es};
			AddWritingSystems(hvos);
			m_inMemoryCache.CacheAccessor.CacheVecProp(m_inMemoryCache.Cache.LangProject.Hvo,
				(int)LangProject.LangProjectTags.kflidVernWss,
				hvos, hvos.Length);

			m_dlg = new DummyFwProjPropertiesDlg(m_inMemoryCache.Cache, null, null, 0, 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetupAnalysisWss()
		{
			// Setup so the CurAnalysisWssRS returns the proper sequence.
			int[] hvos = new int[] {(int)InMemoryFdoCache.s_wsHvos.Es, (int)InMemoryFdoCache.s_wsHvos.Ipa};
			AddWritingSystems(hvos);
			m_inMemoryCache.CacheAccessor.CacheVecProp(m_inMemoryCache.Cache.LangProject.Hvo,
				(int)LangProject.LangProjectTags.kflidCurAnalysisWss,
				hvos, hvos.Length);

			// Setup so the AnalysisWssRC returns the proper collection.
			hvos = new int[] {(int)InMemoryFdoCache.s_wsHvos.Es, (int)InMemoryFdoCache.s_wsHvos.Ipa,
								 (int)InMemoryFdoCache.s_wsHvos.En, (int)InMemoryFdoCache.s_wsHvos.Fr};
			AddWritingSystems(hvos);
			m_inMemoryCache.CacheAccessor.CacheVecProp(m_inMemoryCache.Cache.LangProject.Hvo,
				(int)LangProject.LangProjectTags.kflidAnalysisWss,
				hvos, hvos.Length);

			m_dlg = new DummyFwProjPropertiesDlg(m_inMemoryCache.Cache, null, null, 0, 0, 0);
		}

		#endregion

		#region Test Setup and Tear-Down
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Create a new draft view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[SetUp]
		public void Init()
		{
			m_inMemoryCache = InMemoryFdoCache.CreateInMemoryFdoCache();
			m_inMemoryCache.InitializeLangProject();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public void Teardown()
		{
			m_inMemoryCache.Dispose();
		}
		#endregion

		#region Helper Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies the writing system order.
		/// </summary>
		/// <param name="list">The list.</param>
		/// <param name="wsnames">The wsnames.</param>
		/// ------------------------------------------------------------------------------------
		private void VerifyWritingSystemOrder(CheckedListBox list, string[] wsnames)
		{
			Assert.AreEqual(wsnames.Length, list.Items.Count,
				"Number of writing systems in list is incorrect.");

			for (int i = 0; i < wsnames.Length; i++)
				Assert.AreEqual(wsnames[i], list.Items[i].ToString());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="list"></param>
		/// <param name="wsnames"></param>
		/// <param name="shouldBeChecked"></param>
		/// ------------------------------------------------------------------------------------
		private void VerifyCheckedWritingSystems(CheckedListBox list, string[] wsnames,
			bool shouldBeChecked)
		{
			if (shouldBeChecked)
			{
				Assert.AreEqual(wsnames.Length, list.CheckedItems.Count,
					"Number of checked writing systems is incorrect.");
			}
			else
			{
				Assert.AreEqual(wsnames.Length, list.Items.Count - list.CheckedItems.Count,
					"Number of unchecked writing systems is incorrect.");
			}

			foreach (string name in wsnames)
			{
				bool found = false;

				foreach (LgWritingSystem ws in list.CheckedItems)
				{
					if (name == ws.ToString())
					{
						found = true;
						break;
					}
				}

				if (shouldBeChecked)
					Assert.IsTrue(found, name + " not found in checked items list.");
				else
					Assert.IsFalse(found, name + " found in checked items list.");
			}
		}

		#endregion

		#region Tests
		// See comment on AnalysisWsListAdd
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VernacularWsListContent()
		{
			// Setup so the order in the Analysis WS list should be: English, French,
			// English IPA and Spanish.
			SetupVernWss();

			Assert.AreEqual(0, m_dlg.VernWsList.SelectedIndex, "First item is not selected.");

			VerifyWritingSystemOrder(m_dlg.VernWsList,
				new string[] { "English", "French", "English IPA", "Spanish" });

			// Verify that the correct items are checked.
			VerifyCheckedWritingSystems(m_dlg.VernWsList,
				new string[] { "English", "French" }, true);

			// Verify that the correct items are not checked.
			VerifyCheckedWritingSystems(m_dlg.VernWsList,
				new string[] { "English IPA", "Spanish" }, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VernacularWsListMoveContentDown()
		{
			// Setup so the order in the Analysis WS list should be: English, French,
			// English IPA and Spanish.
			SetupVernWss();

			// Set the selected item to "French"
			m_dlg.VernWsList.SelectedIndex = 1;
			m_dlg.SimulateVernDownButtonPress();

			VerifyWritingSystemOrder(m_dlg.VernWsList,
				new string[] { "English", "English IPA", "French", "Spanish" });

			Assert.AreEqual(2, m_dlg.VernWsList.SelectedIndex);

			// Verify that the correct items are checked.
			VerifyCheckedWritingSystems(m_dlg.VernWsList,
				new string[] { "English", "French" }, true);

			// Verify that the correct items are not checked.
			VerifyCheckedWritingSystems(m_dlg.VernWsList,
				new string[] { "English IPA", "Spanish" }, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VernacularWsListSelectionAtBottom()
		{
			SetupVernWss();

			m_dlg.VernWsList.SelectedIndex = m_dlg.VernWsList.Items.Count - 2;
			Assert.IsTrue(m_dlg.VernWsMoveDownButton.Enabled);
			m_dlg.VernWsList.SelectedIndex = m_dlg.VernWsList.Items.Count - 1;
			Assert.IsFalse(m_dlg.VernWsMoveDownButton.Enabled);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VernacularWsListSelectionAtTop()
		{
			SetupVernWss();

			m_dlg.VernWsList.SelectedIndex = 1;
			Assert.IsTrue(m_dlg.VernWsMoveUpButton.Enabled);
			m_dlg.VernWsList.SelectedIndex = 0;
			Assert.IsFalse(m_dlg.VernWsMoveUpButton.Enabled);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VernacularWsListMoveContentUp()
		{
			// Setup so the order in the Analysis WS list should be: English, French,
			// English IPA and Spanish.
			SetupVernWss();

			// Set the selected item to "French"
			m_dlg.VernWsList.SelectedIndex = 1;
			m_dlg.SimulateVernUpButtonPress();

			VerifyWritingSystemOrder(m_dlg.VernWsList,
				new string[] { "French", "English", "English IPA", "Spanish" });

			Assert.AreEqual(0, m_dlg.VernWsList.SelectedIndex);

			// Verify that the correct items are checked.
			VerifyCheckedWritingSystems(m_dlg.VernWsList,
				new string[] { "English", "French" }, true);

			// Verify that the correct items are not checked.
			VerifyCheckedWritingSystems(m_dlg.VernWsList,
				new string[] { "English IPA", "Spanish" }, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VernacularWsListDelete()
		{
			// Setup so the order in the Vernacular WS list should be: English, French,
			// English IPA and Spanish.
			SetupVernWss();

			// Set the selected item to "French"
			m_dlg.VernWsList.SelectedIndex = 1;
			m_dlg.SimulateVernDeletePress();

			VerifyWritingSystemOrder(m_dlg.VernWsList,
				new string[] { "English", "English IPA", "Spanish" });

			Assert.AreEqual(1, m_dlg.VernWsList.SelectedIndex);

			// Verify that the correct items are checked.
			VerifyCheckedWritingSystems(m_dlg.VernWsList,
				new string[] { "English" }, true);

			// Verify that the correct items are not checked.
			VerifyCheckedWritingSystems(m_dlg.VernWsList,
				new string[] { "English IPA", "Spanish" }, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VernacularWsListDeleteAllWs()
		{
			// Setup so the order in the Vernacular WS list should be: English, French,
			// English IPA and Spanish.
			SetupVernWss();

			// delete all the items in the list
			m_dlg.VernWsList.SelectedIndex = 0;
			m_dlg.SimulateVernDeletePress();
			m_dlg.SimulateVernDeletePress();
			m_dlg.SimulateVernDeletePress();
			m_dlg.SimulateVernDeletePress();

			Assert.AreEqual(-1, m_dlg.VernWsList.SelectedIndex);
			Assert.AreEqual(0, m_dlg.VernWsList.Items.Count);
			Assert.IsFalse(m_dlg.OkButton.Enabled);
			Assert.IsFalse(m_dlg.VernWsDeleteButton.Enabled);
			Assert.IsFalse(m_dlg.VernWsModifyButton.Enabled);
			Assert.IsFalse(m_dlg.VernWsMoveUpButton.Enabled);
			Assert.IsFalse(m_dlg.VernWsMoveDownButton.Enabled);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VernacularWsListAdd()
		{
			m_inMemoryCache.InitializeWritingSystemEncodings();
			// Setup so the order in the Analysis WS list should be: English, French,
			// English IPA and Spanish.
			SetupVernWss();

			// Disable all these buttons so we can verify that they will be enabled after
			// adding a new writing system.
			m_dlg.OkButton.Enabled = false;
			m_dlg.VernWsDeleteButton.Enabled = false;
			m_dlg.VernWsModifyButton.Enabled = false;
			m_dlg.VernWsMoveUpButton.Enabled = false;
			m_dlg.VernWsMoveDownButton.Enabled = false;

			// Add a new writing system to the cache.
			int newWsHvo = m_inMemoryCache.SetupWs("no");
			LgWritingSystem ws = m_inMemoryCache.CreateWritingSystem(m_inMemoryCache.Cache,
				newWsHvo, "no", new int[] { (int)InMemoryFdoCache.s_wsHvos.En }, new string[] { "new one" });

			m_dlg.SimulateVernAddingWs(ws);

			VerifyWritingSystemOrder(m_dlg.VernWsList,
				new string[] { "English", "French", "English IPA", "Spanish", "new one" });

			// Verify that the new writing system is the selected one.
			Assert.AreEqual(ws, (LgWritingSystem)m_dlg.VernWsList.SelectedItem,
				"New writing system is not selected.");

			// Verify that the correct items are checked.
			VerifyCheckedWritingSystems(m_dlg.VernWsList,
				new string[] { "English", "French", "new one" }, true);

			// Verify that the correct items are not checked.
			VerifyCheckedWritingSystems(m_dlg.VernWsList,
				new string[] { "English IPA", "Spanish" }, false);

			Assert.IsTrue(m_dlg.OkButton.Enabled);
			Assert.IsTrue(m_dlg.VernWsDeleteButton.Enabled);
			Assert.IsTrue(m_dlg.VernWsModifyButton.Enabled);
			Assert.IsTrue(m_dlg.VernWsMoveUpButton.Enabled);
			Assert.IsTrue(m_dlg.VernWsMoveDownButton.Enabled);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AnalysisWsListContent()
		{
			// Setup so the order in the Analysis WS list should be: Spanish, English IPA,
			// English and French.
			SetupAnalysisWss();

			Assert.AreEqual(0, m_dlg.AnalWsList.SelectedIndex, "First item is not selected.");

			VerifyWritingSystemOrder(m_dlg.AnalWsList,
				new string[] { "Spanish", "English IPA", "English", "French" });

			// Verify that the correct items are checked.
			VerifyCheckedWritingSystems(m_dlg.AnalWsList,
				new string[] { "Spanish", "English IPA" }, true);

			// Verify that the correct items are not checked.
			VerifyCheckedWritingSystems(m_dlg.AnalWsList,
				new string[] { "English", "French" }, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AnalysisWsListMoveContentDown()
		{
			// Setup so the order in the Analysis WS list should be: Spanish, English IPA,
			// English and French.
			SetupAnalysisWss();

			// Set the selected item to "English IPA"
			m_dlg.AnalWsList.SelectedIndex = 1;
			m_dlg.SimulateAnalDownButtonPress();

			VerifyWritingSystemOrder(m_dlg.AnalWsList,
				new string[] { "Spanish", "English", "English IPA", "French" });

			Assert.AreEqual(2, m_dlg.AnalWsList.SelectedIndex);

			// Verify that the correct items are checked.
			VerifyCheckedWritingSystems(m_dlg.AnalWsList,
				new string[] { "Spanish", "English IPA" }, true);

			// Verify that the correct items are not checked.
			VerifyCheckedWritingSystems(m_dlg.AnalWsList,
				new string[] { "English", "French" }, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AnalysisWsListMoveContentUp()
		{
			// Setup so the order in the Analysis WS list should be: Spanish, English IPA,
			// English and French.
			SetupAnalysisWss();

			// Set the selected item to "English IPA"
			m_dlg.AnalWsList.SelectedIndex = 1;
			m_dlg.SimulateAnalUpButtonPress();

			VerifyWritingSystemOrder(m_dlg.AnalWsList,
				new string[] { "English IPA", "Spanish", "English", "French" });

			Assert.AreEqual(0, m_dlg.AnalWsList.SelectedIndex);

			// Verify that the correct items are checked.
			VerifyCheckedWritingSystems(m_dlg.AnalWsList,
				new string[] { "Spanish", "English IPA" }, true);

			// Verify that the correct items are not checked.
			VerifyCheckedWritingSystems(m_dlg.AnalWsList,
				new string[] { "English", "French" }, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AnalysisWsListSelectionAtBottom()
		{
			SetupAnalysisWss();

			m_dlg.AnalWsList.SelectedIndex = m_dlg.AnalWsList.Items.Count - 2;
			Assert.IsTrue(m_dlg.AnalWsMoveDownButton.Enabled);
			m_dlg.AnalWsList.SelectedIndex = m_dlg.AnalWsList.Items.Count - 1;
			Assert.IsFalse(m_dlg.AnalWsMoveDownButton.Enabled);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AnalysisWsListSelectionAtTop()
		{
			SetupAnalysisWss();

			m_dlg.AnalWsList.SelectedIndex = 1;
			Assert.IsTrue(m_dlg.AnalWsMoveUpButton.Enabled);
			m_dlg.AnalWsList.SelectedIndex = 0;
			Assert.IsFalse(m_dlg.AnalWsMoveUpButton.Enabled);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AnalysisWsListDelete()
		{
			// Setup so the order in the Analysis WS list should be: Spanish, English IPA,
			// English and French.
			SetupAnalysisWss();

			// Set the selected item to "English IPA"
			m_dlg.AnalWsList.SelectedIndex = 1;
			m_dlg.SimulateAnalDeletePress();

			VerifyWritingSystemOrder(m_dlg.AnalWsList,
				new string[] { "Spanish", "English", "French" });

			Assert.AreEqual(1, m_dlg.AnalWsList.SelectedIndex);

			// Verify that the correct items are checked.
			VerifyCheckedWritingSystems(m_dlg.AnalWsList,
				new string[] { "Spanish" }, true);

			// Verify that the correct items are not checked.
			VerifyCheckedWritingSystems(m_dlg.AnalWsList,
				new string[] { "English", "French" }, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AnalysisWsListDeleteAllWs()
		{
			// Setup so the order in the Analysis WS list should be: Spanish, English IPA,
			// English and French.
			SetupAnalysisWss();

			// Delete all the items in the list
			m_dlg.AnalWsList.SelectedIndex = 0;
			m_dlg.SimulateAnalDeletePress();
			m_dlg.SimulateAnalDeletePress();
			m_dlg.SimulateAnalDeletePress();
			m_dlg.SimulateAnalDeletePress();

			Assert.AreEqual(-1, m_dlg.AnalWsList.SelectedIndex);
			Assert.AreEqual(0, m_dlg.AnalWsList.Items.Count);
			Assert.IsFalse(m_dlg.OkButton.Enabled);
			Assert.IsFalse(m_dlg.AnalWsDeleteButton.Enabled);
			Assert.IsFalse(m_dlg.AnalWsModifyButton.Enabled);
			Assert.IsFalse(m_dlg.AnalWsMoveUpButton.Enabled);
			Assert.IsFalse(m_dlg.AnalWsMoveDownButton.Enabled);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AnalysisWsListAdd()
		{
			// Setup so the order in the Analysis WS list should be: Spanish, English IPA,
			// English and French.
			SetupAnalysisWss();

			// Disable all these buttons so we can verify that they will be enabled after
			// adding a new writing system.
			m_dlg.OkButton.Enabled = false;
			m_dlg.AnalWsDeleteButton.Enabled = false;
			m_dlg.AnalWsModifyButton.Enabled = false;
			m_dlg.AnalWsMoveUpButton.Enabled = false;
			m_dlg.AnalWsMoveDownButton.Enabled = false;

			// Add a new writing system to the cache.
			int newWsHvo = m_inMemoryCache.SetupWs("no");
			LgWritingSystem ws = m_inMemoryCache.CreateWritingSystem(m_inMemoryCache.Cache,
				newWsHvo, "no", new int[] { (int)InMemoryFdoCache.s_wsHvos.En }, new string[] { "new one" });

			m_dlg.SimulateAnalAddingWs(ws);

			VerifyWritingSystemOrder(m_dlg.AnalWsList,
				new string[] { "Spanish", "English IPA", "English", "French", "new one" });

			// Verify that the new writing system is the selected one.
			Assert.AreEqual(ws, (LgWritingSystem)m_dlg.AnalWsList.SelectedItem,
				"New writing system is not selected.");

			// Verify that the correct items are checked.
			VerifyCheckedWritingSystems(m_dlg.AnalWsList,
				new string[] { "Spanish", "English IPA", "new one" }, true);

			// Verify that the correct items are not checked.
			VerifyCheckedWritingSystems(m_dlg.AnalWsList,
				new string[] { "English", "French" }, false);

			Assert.IsTrue(m_dlg.OkButton.Enabled);
			Assert.IsTrue(m_dlg.AnalWsDeleteButton.Enabled);
			Assert.IsTrue(m_dlg.AnalWsModifyButton.Enabled);
			Assert.IsTrue(m_dlg.AnalWsMoveUpButton.Enabled);
			Assert.IsTrue(m_dlg.AnalWsMoveDownButton.Enabled);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SavingVernWritingSystems()
		{
			// Setup so the order in the Analysis WS list should be: English, French,
			// English IPA and Spanish.
			SetupVernWss();

			// Uncheck French and check Spanish and IPA.
			m_dlg.VernWsList.SetItemChecked(1, false);
			m_dlg.VernWsList.SetItemChecked(2, true);
			m_dlg.VernWsList.SetItemChecked(3, true);

			// Move Spanish to the top of the list.
			m_dlg.VernWsList.SelectedIndex = 3;
			m_dlg.SimulateVernUpButtonPress();
			m_dlg.SimulateVernUpButtonPress();
			m_dlg.SimulateVernUpButtonPress();

			// Simulate Pressing the OK button to save the changes.
			m_dlg.SimulateOKButtonPress();

			// Make sure there are the correct number of writing systems in the cache.
			Assert.AreEqual(3, LangProj.CurVernWssRS.Count);
			Assert.AreEqual(4, LangProj.VernWssRC.Count);

			// Make sure the list of current writing systems in the cache is in the proper order.
			Assert.AreEqual("Spanish", LangProj.CurVernWssRS[0].ToString());
			Assert.AreEqual("English", LangProj.CurVernWssRS[1].ToString());
			Assert.AreEqual("English IPA", LangProj.CurVernWssRS[2].ToString());

			// Verify the list of cached writing systems.
			foreach (ILgWritingSystem ws in LangProj.VernWssRC)
			{
				Assert.IsTrue(ws.ToString() == "French" || ws.ToString() == "English" ||
					ws.ToString() == "Spanish" || ws.ToString() == "English IPA");
			}
		}

		#endregion
	}
}
