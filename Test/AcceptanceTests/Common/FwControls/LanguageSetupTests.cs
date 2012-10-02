// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: LanguageSetup.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.Common.Controls;

namespace ATFwControls
{
	#region DummyLangSetup Class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DummyLangSetup : LanguageSetup
	{
		public bool m_searchTookPlace = false;
		public string m_origEthCode;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DummyLangSetup"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public DummyLangSetup()
		{
			m_testing = true;
			m_origEthCode = EthnologueCode;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Reset()
		{
			CheckDisposed();

			lvFindResult.Items.Clear();
			cboLookup.SelectedIndex = 0;
			lblOtherNamesList.Text = string.Empty;
			EthnologueCode = m_origEthCode;
			LanguageName = string.Empty;
			m_lastSearchText = string.Empty;
			txtFindPattern.Text = string.Empty;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public TextBox SearchForTextBox
		{
			get
			{
				CheckDisposed();
				return txtFindPattern;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ListView LvResults
		{
			get
			{
				CheckDisposed();
				return lvFindResult;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Label OtherNames
		{
			get
			{
				CheckDisposed();
				return lblOtherNamesList;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ComboBox SearchByCombo
		{
			get
			{
				CheckDisposed();
				return cboLookup;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="searchText"></param>
		/// ------------------------------------------------------------------------------------
		protected override void LoadList(string searchText)
		{
			ListViewItem lvi;

			lvi = new ListViewItem(new string[] {"English", "USA", "ENU"});
			lvFindResult.Items.Add(lvi);
			lvi = new ListViewItem(new string[] {"French", "France", "FRA"});
			lvFindResult.Items.Add(lvi);
			lvi = new ListViewItem(new string[] {"English", "UK", "ENK"});
			lvFindResult.Items.Add(lvi);
			lvi = new ListViewItem(new string[] {"French", "Senagal", "FRS"});
			lvFindResult.Items.Add(lvi);

			// When we're here, it must mean the base class invoked the call to search
			// the DB. Set this flag so tests can have a way to see if a search happened.
			m_searchTookPlace = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void LoadOtherNames()
		{
			lblOtherNamesList.Text = "Gumbo, Numbo, Wumbo";
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SimulateFindButtonClick()
		{
			CheckDisposed();

			btnFind_Click(null, null);
		}
	}

	#endregion

	#region Test Fixture
	/// ------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ------------------------------------------------------------------------------------
	[TestFixture]
	public class LanguageSetupTests
	{
		private DummyLangSetup m_langSetupCtrl;
		private Form m_form;

		#region Setup and Tear Down
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public void FixtureInit()
		{
			m_form = new Form();
			m_form.Width = 1;
			m_form.Height = 1;
			m_langSetupCtrl = new DummyLangSetup();
			m_form.Controls.Add(m_langSetupCtrl);
			m_form.Show();
			m_form.Hide();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureTearDown]
		public void FixtureCleanUp()
		{
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[SetUp]
		public void Init()
		{
			m_langSetupCtrl.Reset();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[TearDown]
		public void CleanUp()
		{
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the use of the listview to select a current language
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CurrentInfoSetTest()
		{
			// Make an initial search.
			m_langSetupCtrl.SearchForTextBox.Text = "Mumbo-Jumbo";
			m_langSetupCtrl.SimulateFindButtonClick();

			// Select the third item in the list.
			m_langSetupCtrl.LvResults.Items[2].Selected = true;

			Assert.AreEqual("English", m_langSetupCtrl.LanguageName);
			Assert.AreEqual("ENK", m_langSetupCtrl.EthnologueCode);
			Assert.AreEqual("Gumbo, Numbo, Wumbo", m_langSetupCtrl.OtherNames.Text);

			// Verify that choosing a list item when the current language name is
			// blank, will set the current language to the one in the list view item.
			m_langSetupCtrl.LvResults.Items[1].Selected = true;
			m_langSetupCtrl.LanguageName = string.Empty;
			m_langSetupCtrl.LvResults.Items[2].Selected = true;
			Assert.AreEqual("English", m_langSetupCtrl.LanguageName);

			// Verify that choosing a list item when the current language name is the
			// same as the one chosen but differs by case causes the case of the chosen
			// language to revert to match the selected language name.
			m_langSetupCtrl.LvResults.Items[1].Selected = true;
			m_langSetupCtrl.LanguageName = "eNgLiSh";
			m_langSetupCtrl.LvResults.Items[2].Selected = true;

			Assert.AreEqual("English", m_langSetupCtrl.LanguageName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the text being searched for is not placed in the current language text
		/// box. This should happen only when the search is not by language name (i.e. the
		/// search by combo. box's index > 0).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CurrentLangNameNotSet1()
		{
			// Make sure that when searching by country, the text being searched for
			// doesn't get put in the current langauge name.
			m_langSetupCtrl.SearchByCombo.SelectedIndex = 1;
			m_langSetupCtrl.SearchForTextBox.Text = "The Land of Mumbo-Jumbo";
			m_langSetupCtrl.SimulateFindButtonClick();
			Assert.AreEqual(string.Empty, m_langSetupCtrl.LanguageName);

			Init();

			// Make sure that when searching by ethnologue code, the text being searched for
			// doesn't get put in the current langauge name.
			m_langSetupCtrl.SearchByCombo.SelectedIndex = 2;
			m_langSetupCtrl.SearchForTextBox.Text = "Ethnologue Code MUJ";
			m_langSetupCtrl.SimulateFindButtonClick();
			Assert.AreEqual(string.Empty, m_langSetupCtrl.LanguageName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the text being searched for is not placed in the current language text
		/// box when searching by language name and when there is already a current language
		/// name specified.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CurrentLangNameNotSet2()
		{
			m_langSetupCtrl.LanguageName = "Exisiting Mumbo-Jumbo";
			m_langSetupCtrl.SearchForTextBox.Text = "Mumbo-Jumbo";
			m_langSetupCtrl.SimulateFindButtonClick();
			Assert.AreEqual("Exisiting Mumbo-Jumbo", m_langSetupCtrl.LanguageName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the text being searched for is placed in the current language text box.
		/// This should happen only when the search is by language name (i.e. the search by
		/// combo. box's index = 0).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CurrentLangNameSet()
		{
			m_langSetupCtrl.SearchForTextBox.Text = "Mumbo-Jumbo";
			m_langSetupCtrl.SimulateFindButtonClick();
			Assert.AreEqual("Mumbo-Jumbo", m_langSetupCtrl.LanguageName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerifyEthnoAndOtherNamesContentWhenReset()
		{
			m_langSetupCtrl.EthnologueCode = "JUNK CODE";

			// Make sure the other names for the ethnologue code get displayed.
			Assert.AreEqual("Gumbo, Numbo, Wumbo", m_langSetupCtrl.OtherNames.Text);

			// Reset the ethnologueCode.
			m_langSetupCtrl.EthnologueCode = string.Empty;

			// Make sure the ethnologue code field is "Unknown" or something like that.
			Assert.AreEqual(m_langSetupCtrl.m_origEthCode, m_langSetupCtrl.EthnologueCode);

			// Make sure the other names for the ethnologue code gets cleared.
			Assert.AreEqual(string.Empty, m_langSetupCtrl.OtherNames.Text);

			m_langSetupCtrl.EthnologueCode = "JUNK CODE";
			m_langSetupCtrl.EthnologueCode = null;

			// Make sure the ethnologue code field is "Unknown" or something like that.
			Assert.AreEqual(m_langSetupCtrl.m_origEthCode, m_langSetupCtrl.EthnologueCode);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerifySearchIsNotPerformed1()
		{
			// Make an initial search.
			m_langSetupCtrl.SearchForTextBox.Text = "Mumbo-Jumbo";
			m_langSetupCtrl.SimulateFindButtonClick();

			// Change the search text so the text changed event is fired, then put the text back
			// before the timer expires. Make sure a second search isn't made since the search
			// for text is the same as the last text searched for.
			m_langSetupCtrl.m_searchTookPlace = false;
			m_langSetupCtrl.SearchForTextBox.Text = "Mumbo";
			m_langSetupCtrl.SearchForTextBox.Text = "Mumbo-Jumbo";
			m_langSetupCtrl.SimulateFindButtonClick();

			Assert.IsFalse(m_langSetupCtrl.m_searchTookPlace,
				"Search happened but shouldn't have.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerifySearchIsNotPerformed2()
		{
			// Make an initial search.
			m_langSetupCtrl.SearchForTextBox.Text = "Mumbo-Jumbo";
			m_langSetupCtrl.SimulateFindButtonClick();

			// Change the search text to nothing and make sure no search is performed.
			m_langSetupCtrl.m_searchTookPlace = false;
			m_langSetupCtrl.SearchForTextBox.Text = string.Empty;
			m_langSetupCtrl.SimulateFindButtonClick();

			Assert.IsFalse(m_langSetupCtrl.m_searchTookPlace,
				"Search happened but shouldn't have.");
		}
	}

	#endregion
}
