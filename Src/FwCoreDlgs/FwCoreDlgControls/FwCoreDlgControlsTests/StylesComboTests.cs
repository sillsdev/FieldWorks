// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: StylesComboTests.cs
// Responsibility: TeTeam: (DavidO)
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System.Collections;
using System.Windows.Forms;
using System.Diagnostics;
using NUnit.Framework;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Utils;
using SIL.LCModel;
using SIL.LCModel.DomainServices;

namespace SIL.FieldWorks.FwCoreDlgControls
{
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for StylesComboTests.
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	[TestFixture]
	public class StylesComboTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private ComboBox m_stylesComboBox;
		private StyleComboListHelper m_styleListHelper;
		private ILangProject m_lp;
		private LcmStyleSheet m_styleSheet;
		private const string kStyleName = "Words Of Christ";

		#region Test setup and tear-down

		protected override void CreateTestData()
		{
			m_lp = Cache.LanguageProject;

			AddTestStyle("Normal", ContextValues.Internal, StructureValues.Undefined,
				FunctionValues.Prose, false, m_lp.StylesOC);
			AddTestStyle("Paragraph", ContextValues.Text, StructureValues.Body,
				FunctionValues.Prose, false, m_lp.StylesOC);
			AddTestStyle("Section Head", ContextValues.Text, StructureValues.Heading,
				FunctionValues.Prose, false, m_lp.StylesOC);
			AddTestStyle("Verse Number", ContextValues.Text, StructureValues.Body,
				FunctionValues.Verse, true, m_lp.StylesOC);
			AddTestStyle("Title Main", ContextValues.Title, StructureValues.Body,
				FunctionValues.Prose, false, m_lp.StylesOC);
			AddTestStyle("Note General Paragraph", ContextValues.Note, StructureValues.Undefined,
				FunctionValues.Prose, false, m_lp.StylesOC);
			AddTestStyle("Note Marker", ContextValues.Internal, StructureValues.Undefined,
				FunctionValues.Prose, true, m_lp.StylesOC);
			AddTestStyle(kStyleName, ContextValues.Text, StructureValues.Body,
				FunctionValues.Prose, true, 2, m_lp.StylesOC);

			// Setup the stylesheet.
			var captionStyle = AddTestStyle("Caption", ContextValues.Internal,
				StructureValues.Body, FunctionValues.Prose, false,
				m_lp.StylesOC);
			m_styleSheet = new LcmStyleSheet();
			m_styleSheet.Init(Cache, m_lp.Hvo,
				LangProjectTags.kflidStyles);

			Debug.Assert(m_stylesComboBox == null, "m_stylesComboBox is not null.");
			//if (m_stylesComboBox != null)
			//	m_stylesComboBox.Dispose();
			m_stylesComboBox = new ComboBox();
			m_styleListHelper = new StyleComboListHelper(m_stylesComboBox);

			// Set the options to display all of the styles
			m_styleListHelper.MaxStyleLevel = int.MaxValue;
		}

		/// <summary>
		///
		/// </summary>
		public override void TestTearDown()
		{
			m_lp = null;
			m_styleSheet = null;
			m_stylesComboBox.Dispose();
			m_stylesComboBox = null;
			m_styleListHelper.Dispose();
			m_styleListHelper = null;

			base.TestTearDown();
		}


		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verify that the Styles combo box on the format toolbar contains the correct style
		/// names and types. Should be displaying all the styles, except the internal and
		/// internal mappable ones. Should also have the default para chars psuedo-style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerifyAllStylesInCombo()
		{
			// Initialize the combo box.
			m_styleListHelper.AddStyles(m_styleSheet);

			// Get the count of styles in the DB.
			int styleCountExpected = m_lp.StylesOC.Count;

			// Verify that all the styles that are in the DB are in the combo box list.
			int i;
			foreach (var style in m_lp.StylesOC)
			{
				if (style.Context == ContextValues.Internal)
				{
					styleCountExpected--;
					continue; // skip internal styles which won't be in menu.
				}
				i = m_stylesComboBox.FindStringExact(style.Name);
				Assert.That(i > -1, Is.True);
				StyleListItem comboItem = (StyleListItem)m_stylesComboBox.Items[i];
				Assert.That(comboItem.Type, Is.EqualTo(style.Type));
				Assert.That(comboItem.IsDefaultParaCharsStyle, Is.False, "Style is Default Paragraph Characters, but should not be");
			}

			// Now check for the Default Paragraph Characters psuedo-style style.
			i = m_stylesComboBox.FindStringExact(StyleUtils.DefaultParaCharsStyleName);
			Assert.That(i > -1, Is.True);
			styleCountExpected++; // Add one for this psuedo-style
			Assert.That(((StyleListItem)m_stylesComboBox.Items[i]).Type, Is.EqualTo(StyleType.kstCharacter));
			Assert.That(((StyleListItem)m_stylesComboBox.Items[i]).IsDefaultParaCharsStyle, Is.True, "Style is not Default Paragraph Characters, but should be");

			Assert.That(m_stylesComboBox.Items.Count, Is.EqualTo(styleCountExpected));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that setting the ShowOnlyStylesOfType to character styles only shows the
		/// character styles in the combo.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerifyOnlyCharStylesInCombo()
		{
			// Set the combobox to show only character styles and make sure the only styles
			// showing are, indeed, character styles.
			m_styleListHelper.ShowOnlyStylesOfType = StyleType.kstCharacter;

			// Initialize the combo box.
			m_styleListHelper.AddStyles(m_styleSheet);

			Assert.That(m_stylesComboBox.Items.Count > 0, Is.True, "Oops! Everything got excluded.");
			foreach (StyleListItem style in m_stylesComboBox.Items)
			{
				if (style.Name != "Default Paragraph Characters")
					Assert.That(style.Type, Is.EqualTo(StyleType.kstCharacter), "Should have only found character styles in Combo box, but others were found.");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that setting the ShowOnlyStylesOfType to paragraph styles only shows the
		/// paragraph styles in the combo.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerifyOnlyParaStylesInCombo()
		{
			// Set the combobox to show only paragraph styles and make sure the only styles
			// showing are, indeed, paragraph styles.
			m_styleListHelper.ShowOnlyStylesOfType = StyleType.kstParagraph;

			// Initialize the combo box.
			m_styleListHelper.AddStyles(m_styleSheet);

			Assert.That(m_stylesComboBox.Items.Count > 0, Is.True, "Oops! Everything got excluded.");
			foreach (StyleListItem style in m_stylesComboBox.Items)
			{
				if (style.Name != "Default Paragraph Characters")
					Assert.That(style.Type, Is.EqualTo(StyleType.kstParagraph), "Should have only found paragraph styles in Combo box, but others were found.");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that setting the IncludeStylesWithContext property only includes certain styles
		/// in the combo box when there is no filter
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerifyIncludedContexts()
		{
			// Styles whose context are Title or Note should show up in the combo box.
			m_styleListHelper.IncludeStylesWithContext.Add(ContextValues.Title);
			m_styleListHelper.IncludeStylesWithContext.Add(ContextValues.Note);

			// Initialize the combo box.
			m_styleListHelper.AddStyles(m_styleSheet);

			Assert.That(m_stylesComboBox.Items.Count > 0, Is.True, "Oops! Everything got excluded.");
			foreach (StyleListItem style in m_stylesComboBox.Items)
			{
				if (style.Name != "Default Paragraph Characters")
					Assert.That(style.Context == ContextValues.Title ||
						style.Context == ContextValues.Note, Is.True, "Only Title or Note styles should have been found.");
			}

			// Change the list of included styles to only include Internal Mappable styles.
			// Then refresh the list and verify the list's contents.
			m_styleListHelper.IncludeStylesWithContext.Clear();
			m_styleListHelper.IncludeStylesWithContext.Add(ContextValues.InternalMappable);
			m_styleListHelper.Refresh();

			Assert.That(m_stylesComboBox.Items.Count > 0, Is.True, "Oops! Everything got excluded.");
			foreach (StyleListItem style in m_stylesComboBox.Items)
			{
				if (style.Name != "Default Paragraph Characters")
					Assert.That(style.Context, Is.EqualTo(ContextValues.InternalMappable), "Only InternalMappable styles should have been found.");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that setting the IncludeStylesWithContext property forces styles to be included
		/// even if a filter is set when a filter is set
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerifyIncludedContextsWithFilter()
		{
			m_styleListHelper.ShowOnlyStylesOfType = StyleType.kstCharacter;
			m_styleListHelper.IncludeStylesWithContext.Add(ContextValues.Title);

			// Initialize the combo box.
			m_styleListHelper.AddStyles(m_styleSheet);

			Assert.That(m_stylesComboBox.Items.Count > 0, Is.True, "Oops! Everything got excluded.");
			foreach (StyleListItem style in m_stylesComboBox.Items)
			{
				Assert.That(style.Context == ContextValues.Title ||
					style.Type == StyleType.kstCharacter, Is.True, "Only Title or character styles should have been found.");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the ExcludeStylesWithFunction property omits certain styles from the
		/// combo box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerifyExcludedStyleFunctions()
		{
			// Styles whose function is Chapter or Verse should not show up in the
			// combo box.
			m_styleListHelper.ExcludeStylesWithFunction.Add(FunctionValues.Chapter);
			m_styleListHelper.ExcludeStylesWithFunction.Add(FunctionValues.Verse);

			// Initialize the combo box.
			m_styleListHelper.AddStyles(m_styleSheet);

			Assert.That(m_stylesComboBox.Items.Count > 0, Is.True, "Oops! Everything got excluded.");
			foreach (StyleListItem style in m_stylesComboBox.Items)
			{
				Assert.That(style.Function != FunctionValues.Chapter, Is.True, "Chapter style should not have been found.");
				Assert.That(style.Function != FunctionValues.Verse, Is.True, "Verse style should not have been found.");
			}

			// Change the list of excluded styles to only exclude Text styles.
			// Then refresh the list and verify the list's contents.
			m_styleListHelper.ExcludeStylesWithFunction.Clear();
			m_styleListHelper.ExcludeStylesWithFunction.Add(FunctionValues.List);
			m_styleListHelper.Refresh();

			Assert.That(m_stylesComboBox.Items.Count > 0, Is.True, "Oops! Everything got excluded.");
			foreach (StyleListItem style in m_stylesComboBox.Items)
			{
				Assert.That(style.Function != FunctionValues.List, Is.True, "Prose style " + style.Name + " should not have been found.");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the ExcludeStylesWithContext property omits certain styles from the combo
		/// box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerifyExcludedContexts()
		{
			// Styles whose context are Title or Intro should not show up in the
			// combo box.
			m_styleListHelper.ExcludeStylesWithContext.Add(ContextValues.Title);
			m_styleListHelper.ExcludeStylesWithContext.Add(ContextValues.Intro);

			// Initialize the combo box.
			m_styleListHelper.AddStyles(m_styleSheet);

			Assert.That(m_stylesComboBox.Items.Count > 0, Is.True, "Oops! Everything got excluded.");
			foreach (StyleListItem style in m_stylesComboBox.Items)
			{
				Assert.That(style.Context != ContextValues.Title, Is.True, "Title style should not have been found.");
				Assert.That(style.Context != ContextValues.Intro, Is.True, "Intro style should not have been found.");
			}

			// Change the list of excluded styles to only exclude Text styles.
			// Then refresh the list and verify the list's contents.
			m_styleListHelper.ExcludeStylesWithContext.Clear();
			m_styleListHelper.ExcludeStylesWithContext.Add(ContextValues.Text);
			m_styleListHelper.Refresh();

			Assert.That(m_stylesComboBox.Items.Count > 0, Is.True, "Oops! Everything got excluded.");
			foreach (StyleListItem style in m_stylesComboBox.Items)
			{
				Assert.That(style.Context != ContextValues.Text, Is.True, "Text style should not have been found.");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Styles with a General context shouldn't show up in the combo box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerifyExcludedContexts_General()
		{
			m_styleListHelper.ExcludeStylesWithContext.Add(ContextValues.General);

			// Initialize the combo box.
			m_styleListHelper.AddStyles(m_styleSheet);

			Assert.That(m_stylesComboBox.Items.Count > 0, Is.True, "Oops! Everything got excluded.");
			foreach (StyleListItem style in m_stylesComboBox.Items)
			{
				if (style.Name != "Default Paragraph Characters")
					Assert.That(style.Context != ContextValues.General, Is.True, "General style should not have been found.");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test with a combination of excluded context and only a particular style type.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerifyShowTypeAndExcludedContext()
		{
			// Styles with a Text context shouldn't show up in the combo box.
			m_styleListHelper.ExcludeStylesWithContext.Add(ContextValues.Text);

			// Also, show only character styles.
			m_styleListHelper.ShowOnlyStylesOfType = StyleType.kstCharacter;

			// Initialize the combo box.
			m_styleListHelper.AddStyles(m_styleSheet);

			Assert.That(m_stylesComboBox.Items.Count > 0, Is.True, "Oops! Everything got excluded.");
			foreach (StyleListItem style in m_stylesComboBox.Items)
			{
				Assert.That(style.Type, Is.EqualTo(StyleType.kstCharacter), "Should have only found character styles in combo box, but others were found.");
				Assert.That(style.Context != ContextValues.Text, Is.True, "Text style should not have been found.");
			}

			// Now show only paragraph styles.
			m_styleListHelper.ShowOnlyStylesOfType = StyleType.kstParagraph;
			m_styleListHelper.Refresh();

			Assert.That(m_stylesComboBox.Items.Count > 0, Is.True, "Oops! Everything got excluded.");
			foreach (StyleListItem style in m_stylesComboBox.Items)
			{
				if (style.Name != "Default Paragraph Characters")
				{
					Assert.That(style.Type, Is.EqualTo(StyleType.kstParagraph), "Should have only found character styles in Combo box, but others were found.");
					Assert.That(style.Context != ContextValues.Text, Is.True, "Text style should not have been found.");
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verify that the style filter works correctly.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerifyStyleLevelFilter()
		{
			// set up for basic styles and make sure all the styles are only basic.
			m_styleListHelper.AddStyles(m_styleSheet);
			m_styleListHelper.MaxStyleLevel = 0;
			m_styleListHelper.Refresh();
			foreach (StyleListItem style in m_stylesComboBox.Items)
				Assert.That(style.UserLevel <= 0, Is.True, "Non-basic style was added in basic mode");

			// setup for custom styles and make sure the appropriate styles are present.
			m_styleListHelper.AddStyles(m_styleSheet);
			m_styleListHelper.MaxStyleLevel = 2;
			m_styleListHelper.Refresh();
			foreach (StyleListItem style in m_stylesComboBox.Items)
				Assert.That(style.UserLevel <= 2, Is.True, "Non-custom style was added in basic mode");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that an excluded style can be included in the combobox (when, for example,
		/// the insertion point is in an internal-style paragraph).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerifySettingExcludedStyle()
		{
			// Initialize the combo box.
			m_styleListHelper.AddStyles(m_styleSheet);
			m_styleListHelper.SelectedStyleName = "Caption";
			Assert.That(m_stylesComboBox.Text, Is.EqualTo("Caption"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that Normal style will not be displayed in the style combo box and that the
		/// displayed styles will not be changed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerifySettingNormalStyle()
		{
			// Initialize the combo box.
			m_styleListHelper.AddStyles(m_styleSheet);
			m_styleListHelper.SelectedStyleName = "Normal";
			ICollection beforeStyles = (ICollection)ReflectionHelper.GetProperty(m_styleListHelper, "Items");
			Assert.That(m_stylesComboBox.Text, Is.EqualTo(""));
			ICollection afterStyles = (ICollection)ReflectionHelper.GetProperty(m_styleListHelper, "Items");
			Assert.That(afterStyles.Count, Is.EqualTo(beforeStyles.Count), "Selected styles should not change");
		}
	}
}
