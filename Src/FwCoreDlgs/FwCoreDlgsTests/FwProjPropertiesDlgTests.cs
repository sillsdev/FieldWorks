// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: TestFwProjPropertiesDlg.cs
// Responsibility:

using System.Linq;
using System.Windows.Forms;

using NUnit.Framework;

using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;

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
		/// Initializes a new instance of the <see cref="DummyFwProjPropertiesDlg"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// ------------------------------------------------------------------------------------
		public DummyFwProjPropertiesDlg(FdoCache cache)
			: base(cache, null, null, null)
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

			SaveInternal();
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
		public void SimulateAnalAddingWs(CoreWritingSystemDefinition ws)
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
		public void SimulateVernAddingWs(CoreWritingSystemDefinition ws)
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
	public class FwProjPropertiesDlgTests: MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private DummyFwProjPropertiesDlg m_dlg;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ILangProject LangProj
		{
			get {return Cache.LanguageProject;}
		}

		/// <summary>
		///
		/// </summary>
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			CoreWritingSystemDefinition ws;
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("en-fonipa-x-etic", out ws);
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("es", out ws);
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("fr", out ws);
		}

		/// <summary/>
		[TearDown]
		public void TearDown()
		{
			if (m_dlg != null)
				m_dlg.Dispose();
			m_dlg = null;
		}

		#region Setup Helpers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetupVernWss()
		{
			IWritingSystemContainer wsContainer = Cache.ServiceLocator.WritingSystems;
			WritingSystemManager wsManager = Cache.ServiceLocator.WritingSystemManager;
			//wsContainer.VernacularWritingSystems.Clear();
			//wsContainer.CurrentVernacularWritingSystems.Clear();
			// Setup so the CurVernWssRS returns the proper sequence.
			wsContainer.VernacularWritingSystems.Add(wsManager.Get("en"));
			wsContainer.VernacularWritingSystems.Add(wsManager.Get("en-fonipa-x-etic"));
			wsContainer.VernacularWritingSystems.Add(wsManager.Get("fr"));
			wsContainer.VernacularWritingSystems.Add(wsManager.Get("es"));

			wsContainer.CurrentVernacularWritingSystems.Add(wsManager.Get("en"));

			if (m_dlg == null)
				m_dlg = new DummyFwProjPropertiesDlg(Cache);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetupAnalysisWss()
		{
			IWritingSystemContainer wsContainer = Cache.ServiceLocator.WritingSystems;
			WritingSystemManager wsManager = Cache.ServiceLocator.WritingSystemManager;
			//wsContainer.AnalysisWritingSystems.Clear();
			wsContainer.AnalysisWritingSystems.Add(wsManager.Get("en"));
			wsContainer.AnalysisWritingSystems.Add(wsManager.Get("en-fonipa-x-etic"));
			wsContainer.AnalysisWritingSystems.Add(wsManager.Get("fr"));
			wsContainer.AnalysisWritingSystems.Add(wsManager.Get("es"));

			wsContainer.CurrentAnalysisWritingSystems.Add(wsManager.Get("en-fonipa-x-etic"));

			if (m_dlg == null)
				m_dlg = new DummyFwProjPropertiesDlg(Cache);
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
		private static void VerifyWritingSystemOrder(CheckedListBox list, string[] wsnames)
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
		private static void VerifyCheckedWritingSystems(CheckedListBox list, string[] wsnames,
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
				bool found = list.CheckedItems.Cast<CoreWritingSystemDefinition>().Any(ws => name == ws.ToString());

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
			// Setup so the order in the Vernacular WS list should be: English, French,
			// English (IPA) and Spanish.
			SetupVernWss();
			m_dlg.WritingSystemsChanged();

			Assert.AreEqual(0, m_dlg.VernWsList.SelectedIndex, "First item is not selected.");

			VerifyWritingSystemOrder(m_dlg.VernWsList,
				new[] { "French", "English", "English (Phonetic)", "Spanish" });

			// Verify that the correct items are checked.
			VerifyCheckedWritingSystems(m_dlg.VernWsList,
				new[] { "English", "French" }, true);

			// Verify that the correct items are not checked.
			VerifyCheckedWritingSystems(m_dlg.VernWsList,
				new[] { "English (Phonetic)", "Spanish" }, false);
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
			// English (IPA) and Spanish.
			SetupVernWss();

			// Set the selected item to "English"
			m_dlg.VernWsList.SelectedIndex = 1;
			m_dlg.SimulateVernDownButtonPress();

			VerifyWritingSystemOrder(m_dlg.VernWsList,
				new[] { "French", "English (Phonetic)", "English", "Spanish"});

			Assert.AreEqual(2, m_dlg.VernWsList.SelectedIndex);

			// Verify that the correct items are checked.
			VerifyCheckedWritingSystems(m_dlg.VernWsList,
				new[] { "English", "French" }, true);

			// Verify that the correct items are not checked.
			VerifyCheckedWritingSystems(m_dlg.VernWsList,
				new[] { "English (Phonetic)", "Spanish" }, false);
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
			// English (IPA) and Spanish.
			SetupVernWss();

			// Set the selected item to "English"
			m_dlg.VernWsList.SelectedIndex = 1;
			m_dlg.SimulateVernUpButtonPress();

			VerifyWritingSystemOrder(m_dlg.VernWsList,
				new[] { "English", "French", "English (Phonetic)", "Spanish" });

			Assert.AreEqual(0, m_dlg.VernWsList.SelectedIndex);

			// Verify that the correct items are checked.
			VerifyCheckedWritingSystems(m_dlg.VernWsList,
				new[] { "English", "French" }, true);

			// Verify that the correct items are not checked.
			VerifyCheckedWritingSystems(m_dlg.VernWsList,
				new[] { "English (Phonetic)", "Spanish" }, false);
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
			// English (IPA) and Spanish.
			SetupVernWss();

			// Set the selected item to "English"
			m_dlg.VernWsList.SelectedIndex = 1;
			m_dlg.SimulateVernDeletePress();

			VerifyWritingSystemOrder(m_dlg.VernWsList,
				new[] { "French", "English (Phonetic)", "Spanish" });

			Assert.AreEqual(1, m_dlg.VernWsList.SelectedIndex);

			// Verify that the correct items are checked.
			VerifyCheckedWritingSystems(m_dlg.VernWsList,
				new[] { "French" }, true);

			// Verify that the correct items are not checked.
			VerifyCheckedWritingSystems(m_dlg.VernWsList,
				new[] { "English (Phonetic)", "Spanish" }, false);
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
			// English (IPA) and Spanish.
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
			// Setup so the order in the Analysis WS list should be: English, French,
			// English (IPA) and Spanish.
			SetupVernWss();

			// Disable all these buttons so we can verify that they will be enabled after
			// adding a new writing system.
			m_dlg.OkButton.Enabled = false;
			m_dlg.VernWsDeleteButton.Enabled = false;
			m_dlg.VernWsModifyButton.Enabled = false;
			m_dlg.VernWsMoveUpButton.Enabled = false;
			m_dlg.VernWsMoveDownButton.Enabled = false;

			// Add a new writing system to the cache.
			CoreWritingSystemDefinition ws = Cache.ServiceLocator.WritingSystemManager.Set("en-US");

			m_dlg.SimulateVernAddingWs(ws);

			VerifyWritingSystemOrder(m_dlg.VernWsList,
				new[] { "French", "English", "English (Phonetic)", "Spanish", "English (United States)"});

			// Verify that the new writing system is the selected one.
			Assert.AreEqual(ws, m_dlg.VernWsList.SelectedItem,
				"New writing system is not selected.");

			// Verify that the correct items are checked.
			VerifyCheckedWritingSystems(m_dlg.VernWsList,
				new[] { "English", "French", "English (United States)" }, true);

			// Verify that the correct items are not checked.
			VerifyCheckedWritingSystems(m_dlg.VernWsList,
				new[] { "English (Phonetic)", "Spanish" }, false);

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
			// Setup so the order in the Analysis WS list should be: Spanish, English (IPA),
			// English and French.
			SetupAnalysisWss();

			Assert.AreEqual(0, m_dlg.AnalWsList.SelectedIndex, "First item is not selected.");

			VerifyWritingSystemOrder(m_dlg.AnalWsList,
				new[] { "English", "English (Phonetic)", "French", "Spanish" });

			// Verify that the correct items are checked.
			VerifyCheckedWritingSystems(m_dlg.AnalWsList,
				new[] { "English (Phonetic)", "English" }, true);

			// Verify that the correct items are not checked.
			VerifyCheckedWritingSystems(m_dlg.AnalWsList,
				new[] { "Spanish", "French" }, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AnalysisWsListMoveContentDown()
		{
			// Setup so the order in the Analysis WS list should be: Spanish, English (IPA),
			// English and French.
			SetupAnalysisWss();

			// Set the selected item to "English (IPA)"
			m_dlg.AnalWsList.SelectedIndex = 1;
			m_dlg.SimulateAnalDownButtonPress();

			VerifyWritingSystemOrder(m_dlg.AnalWsList,
				new[] { "English", "French", "English (Phonetic)", "Spanish" });

			Assert.AreEqual(2, m_dlg.AnalWsList.SelectedIndex);

			// Verify that the correct items are checked.
			VerifyCheckedWritingSystems(m_dlg.AnalWsList,
				new[] { "English (Phonetic)", "English" }, true);

			// Verify that the correct items are not checked.
			VerifyCheckedWritingSystems(m_dlg.AnalWsList,
				new[] { "Spanish", "French" }, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AnalysisWsListMoveContentUp()
		{
			// Setup so the order in the Analysis WS list should be: Spanish, English (IPA),
			// English and French.
			SetupAnalysisWss();

			// Set the selected item to "English (IPA)"
			m_dlg.AnalWsList.SelectedIndex = 1;
			m_dlg.SimulateAnalUpButtonPress();

			VerifyWritingSystemOrder(m_dlg.AnalWsList,
				new[] { "English (Phonetic)", "English", "French", "Spanish" });

			Assert.AreEqual(0, m_dlg.AnalWsList.SelectedIndex);

			// Verify that the correct items are checked.
			VerifyCheckedWritingSystems(m_dlg.AnalWsList,
				new[] { "English (Phonetic)", "English" }, true);

			// Verify that the correct items are not checked.
			VerifyCheckedWritingSystems(m_dlg.AnalWsList,
				new[] { "spanish", "French" }, false);
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
			// Setup so the order in the Analysis WS list should be: Spanish, English (IPA),
			// English and French.
			SetupAnalysisWss();

			// Set the selected item to "English (IPA)"
			m_dlg.AnalWsList.SelectedIndex = 1;
			m_dlg.SimulateAnalDeletePress();

			VerifyWritingSystemOrder(m_dlg.AnalWsList,
				new[] { "English", "French", "Spanish" });

			Assert.AreEqual(1, m_dlg.AnalWsList.SelectedIndex);

			// Verify that the correct items are checked.
			VerifyCheckedWritingSystems(m_dlg.AnalWsList,
				new[] { "English" }, true);

			// Verify that the correct items are not checked.
			VerifyCheckedWritingSystems(m_dlg.AnalWsList,
				new[] { "Spanish", "French" }, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AnalysisWsListDeleteAllWs()
		{
			// Setup so the order in the Analysis WS list should be: Spanish, English (IPA),
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
			// Setup so the order in the Analysis WS list should be: Spanish, English (IPA),
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
			CoreWritingSystemDefinition ws = Cache.ServiceLocator.WritingSystemManager.Set("zh-CN");

			m_dlg.SimulateAnalAddingWs(ws);

			VerifyWritingSystemOrder(m_dlg.AnalWsList,
				new[] { "English", "English (Phonetic)", "French", "Spanish", "Chinese (Han (Simplified variant), China)" });

			// Verify that the new writing system is the selected one.
			Assert.AreEqual(ws, m_dlg.AnalWsList.SelectedItem, "New writing system is not selected.");

			// Verify that the correct items are checked.
			VerifyCheckedWritingSystems(m_dlg.AnalWsList,
				new[] { "English (Phonetic)", "Chinese (Han (Simplified variant), China)", "English" }, true);

			// Verify that the correct items are not checked.
			VerifyCheckedWritingSystems(m_dlg.AnalWsList,
				new[] { "Spanish", "French" }, false);

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
			// English (IPA) and Spanish.
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
			Assert.AreEqual(3, Cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Count);
			Assert.AreEqual(4, Cache.ServiceLocator.WritingSystems.VernacularWritingSystems.Count);

			// Make sure the list of current writing systems in the cache is in the proper order.
			Assert.AreEqual("Spanish", Cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems[0].ToString());
			Assert.AreEqual("French", Cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems[1].ToString());
			Assert.AreEqual("English (Phonetic)", Cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems[2].ToString());

			// Verify the list of cached writing systems.
			foreach (CoreWritingSystemDefinition ws in Cache.ServiceLocator.WritingSystems.VernacularWritingSystems)
			{
				Assert.IsTrue(ws.ToString() == "French" || ws.ToString() == "English" ||
					ws.ToString() == "Spanish" || ws.ToString() == "English (Phonetic)");
			}
		}

		#endregion
	}
}
