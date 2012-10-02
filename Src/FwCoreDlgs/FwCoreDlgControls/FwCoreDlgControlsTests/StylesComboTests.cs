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
// File: StylesComboTests.cs
// Responsibility: TeTeam: (DavidO)
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;
using System.Collections;
using System.Diagnostics;
using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.FwCoreDlgControls
{
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for StylesComboTests.
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	[TestFixture]
	public class StylesComboTests : InMemoryFdoTestBase
	{
		private ComboBox m_stylesComboBox;
		private StyleComboListHelper m_styleListHelper;
		private ILangProject m_lp;
		private FwStyleSheet m_styleSheet;
		private FdoCache m_fdoCache;
		private const string kStyleName = "Words Of Christ";

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_stylesComboBox != null)
					m_stylesComboBox.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_stylesComboBox = null;
			m_styleListHelper = null;
			m_styleSheet = null;
			m_lp = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		#region Test setup and tear-down
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Init for a test.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void Initialize()
		{
			CheckDisposed();

			base.Initialize();

			m_fdoCache = Cache;

			// Setup the stylesheet.
			IStStyle captionStyle = AddTestStyle("Caption", ContextValues.Internal,
				StructureValues.Body, FunctionValues.Prose, false,
				Cache.LangProject.StylesOC);
			m_styleSheet = new FwStyleSheet();
			((FwStyleSheet)m_styleSheet).Init(m_fdoCache, m_lp.Hvo,
				(int)LangProject.LangProjectTags.kflidStyles);

			Debug.Assert(m_stylesComboBox == null, "m_stylesComboBox is not null.");
			//if (m_stylesComboBox != null)
			//	m_stylesComboBox.Dispose();
			m_stylesComboBox = new ComboBox();
			m_styleListHelper = new StyleComboListHelper(m_stylesComboBox);

			// Set the options to display all of the styles
			m_styleListHelper.MaxStyleLevel = int.MaxValue;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up for a test.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void Exit()
		{
			CheckDisposed();
			base.Exit();

			m_lp = null;
			m_styleSheet = null;
			m_stylesComboBox.Dispose();
			m_stylesComboBox = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called to make the test data for the tests
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			base.CreateTestData();

			m_lp = Cache.LangProject;

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
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that setting the SelectedStyleName property sets the InUse flag of the style
		/// and that the style is still shown in the list when we limit styles shown.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerifyStylesSetToInUseInCombo()
		{
			CheckDisposed();

			// Set the combobox to show only character styles
			m_styleListHelper.ShowOnlyStylesOfType = StyleType.kstCharacter;
			m_styleListHelper.MaxStyleLevel = 5; // show all styles

			// Initialize the combo box.
			m_styleListHelper.AddStyles(m_styleSheet);
			m_styleListHelper.MaxStyleLevel = 1;
			Assert.IsTrue(m_stylesComboBox.Items.Count > 0, "Oops! Everything got excluded.");

			// Trying to select the style should cause it to be set to "InUse" and it should be
			// added to the list.
			m_styleListHelper.SelectedStyleName = kStyleName;
			StyleListItem item = m_styleListHelper.SelectedStyle;

			// Assume if we have a style item the item was in the list
			Assert.IsNotNull(item);
			Assert.AreEqual(kStyleName, item.Name);
			Assert.IsTrue(item.StyleInfo.RealStyle.InUse);
			Assert.AreEqual(-2, item.UserLevel);
			Assert.AreEqual(-2, item.StyleInfo.UserLevel);
		}

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
			CheckDisposed();

			// Initialize the combo box.
			m_styleListHelper.AddStyles(m_styleSheet);

			// Get the count of styles in the DB.
			int styleCountExpected = m_lp.StylesOC.Count;

			// Verify that all the styles that are in the DB are in the combo box list.
			int i;
			foreach (IStStyle style in m_lp.StylesOC)
			{
				if (style.Context == ContextValues.Internal)
				{
					styleCountExpected--;
					continue; // skip internal styles which won't be in menu.
				}
				i = m_stylesComboBox.FindStringExact(style.Name);
				Assert.IsTrue(i > -1);
				StyleListItem comboItem = (StyleListItem)m_stylesComboBox.Items[i];
				Assert.AreEqual(style.Type, comboItem.Type);
				Assert.IsFalse(comboItem.IsDefaultParaCharsStyle,
					"Style is Default Paragraph Characters, but should not be");
			}

			// Now check for the Default Paragraph Characters psuedo-style style.
			i = m_stylesComboBox.FindStringExact(FdoResources.DefaultParaCharsStyleName);
			Assert.IsTrue(i > -1);
			styleCountExpected++; // Add one for this psuedo-style
			Assert.AreEqual(StyleType.kstCharacter,
				((StyleListItem)m_stylesComboBox.Items[i]).Type);
			Assert.IsTrue(((StyleListItem)m_stylesComboBox.Items[i]).IsDefaultParaCharsStyle,
				"Style is not Default Paragraph Characters, but should be");

			Assert.AreEqual(styleCountExpected, m_stylesComboBox.Items.Count);
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
			CheckDisposed();

			// Set the combobox to show only character styles and make sure the only styles
			// showing are, indeed, character styles.
			m_styleListHelper.ShowOnlyStylesOfType = StyleType.kstCharacter;

			// Initialize the combo box.
			m_styleListHelper.AddStyles(m_styleSheet);

			Assert.IsTrue(m_stylesComboBox.Items.Count > 0, "Oops! Everything got excluded.");
			foreach (StyleListItem style in m_stylesComboBox.Items)
			{
				if (style.Name != "Default Paragraph Characters")
					Assert.AreEqual(StyleType.kstCharacter, style.Type,
						"Should have only found character styles in Combo box, but others were found.");
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
			CheckDisposed();

			// Set the combobox to show only paragraph styles and make sure the only styles
			// showing are, indeed, paragraph styles.
			m_styleListHelper.ShowOnlyStylesOfType = StyleType.kstParagraph;

			// Initialize the combo box.
			m_styleListHelper.AddStyles(m_styleSheet);

			Assert.IsTrue(m_stylesComboBox.Items.Count > 0, "Oops! Everything got excluded.");
			foreach (StyleListItem style in m_stylesComboBox.Items)
			{
				if (style.Name != "Default Paragraph Characters")
					Assert.AreEqual(StyleType.kstParagraph, style.Type,
						"Should have only found paragraph styles in Combo box, but others were found.");
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
			CheckDisposed();

			// Styles whose context are Title or Note should show up in the combo box.
			m_styleListHelper.IncludeStylesWithContext.Add(ContextValues.Title);
			m_styleListHelper.IncludeStylesWithContext.Add(ContextValues.Note);

			// Initialize the combo box.
			m_styleListHelper.AddStyles(m_styleSheet);

			Assert.IsTrue(m_stylesComboBox.Items.Count > 0, "Oops! Everything got excluded.");
			foreach (StyleListItem style in m_stylesComboBox.Items)
			{
				if (style.Name != "Default Paragraph Characters")
					Assert.IsTrue(style.Context == ContextValues.Title ||
						style.Context == ContextValues.Note,
						"Only Title or Note styles should have been found.");
			}

			// Change the list of included styles to only include Internal Mappable styles.
			// Then refresh the list and verify the list's contents.
			m_styleListHelper.IncludeStylesWithContext.Clear();
			m_styleListHelper.IncludeStylesWithContext.Add(ContextValues.InternalMappable);
			m_styleListHelper.Refresh();

			Assert.IsTrue(m_stylesComboBox.Items.Count > 0, "Oops! Everything got excluded.");
			foreach (StyleListItem style in m_stylesComboBox.Items)
			{
				if (style.Name != "Default Paragraph Characters")
					Assert.AreEqual(ContextValues.InternalMappable, style.Context,
						"Only InternalMappable styles should have been found.");
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
			CheckDisposed();

			m_styleListHelper.ShowOnlyStylesOfType = StyleType.kstCharacter;
			m_styleListHelper.IncludeStylesWithContext.Add(ContextValues.Title);

			// Initialize the combo box.
			m_styleListHelper.AddStyles(m_styleSheet);

			Assert.IsTrue(m_stylesComboBox.Items.Count > 0, "Oops! Everything got excluded.");
			foreach (StyleListItem style in m_stylesComboBox.Items)
			{
				Assert.IsTrue(style.Context == ContextValues.Title ||
					style.Type == StyleType.kstCharacter,
					"Only Title or character styles should have been found.");
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
			CheckDisposed();

			// Styles whose function is Chapter or Verse should not show up in the
			// combo box.
			m_styleListHelper.ExcludeStylesWithFunction.Add(FunctionValues.Chapter);
			m_styleListHelper.ExcludeStylesWithFunction.Add(FunctionValues.Verse);

			// Initialize the combo box.
			m_styleListHelper.AddStyles(m_styleSheet);

			Assert.IsTrue(m_stylesComboBox.Items.Count > 0, "Oops! Everything got excluded.");
			foreach (StyleListItem style in m_stylesComboBox.Items)
			{
				Assert.IsTrue(style.Function != FunctionValues.Chapter,
					"Chapter style should not have been found.");
				Assert.IsTrue(style.Function != FunctionValues.Verse,
					"Verse style should not have been found.");
			}

			// Change the list of excluded styles to only exclude Text styles.
			// Then refresh the list and verify the list's contents.
			m_styleListHelper.ExcludeStylesWithFunction.Clear();
			m_styleListHelper.ExcludeStylesWithFunction.Add(FunctionValues.List);
			m_styleListHelper.Refresh();

			Assert.IsTrue(m_stylesComboBox.Items.Count > 0, "Oops! Everything got excluded.");
			foreach (StyleListItem style in m_stylesComboBox.Items)
			{
				Assert.IsTrue(style.Function != FunctionValues.List,
					"Prose style " + style.Name + " should not have been found.");
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
			CheckDisposed();

			// Styles whose context are Title or Intro should not show up in the
			// combo box.
			m_styleListHelper.ExcludeStylesWithContext.Add(ContextValues.Title);
			m_styleListHelper.ExcludeStylesWithContext.Add(ContextValues.Intro);

			// Initialize the combo box.
			m_styleListHelper.AddStyles(m_styleSheet);

			Assert.IsTrue(m_stylesComboBox.Items.Count > 0, "Oops! Everything got excluded.");
			foreach (StyleListItem style in m_stylesComboBox.Items)
			{
				Assert.IsTrue(style.Context != ContextValues.Title,
					"Title style should not have been found.");
				Assert.IsTrue(style.Context != ContextValues.Intro,
					"Intro style should not have been found.");
			}

			// Change the list of excluded styles to only exclude Text styles.
			// Then refresh the list and verify the list's contents.
			m_styleListHelper.ExcludeStylesWithContext.Clear();
			m_styleListHelper.ExcludeStylesWithContext.Add(ContextValues.Text);
			m_styleListHelper.Refresh();

			Assert.IsTrue(m_stylesComboBox.Items.Count > 0, "Oops! Everything got excluded.");
			foreach (StyleListItem style in m_stylesComboBox.Items)
			{
				Assert.IsTrue(style.Context != ContextValues.Text,
					"Text style should not have been found.");
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
			CheckDisposed();

			m_styleListHelper.ExcludeStylesWithContext.Add(ContextValues.General);

			// Initialize the combo box.
			m_styleListHelper.AddStyles(m_styleSheet);

			Assert.IsTrue(m_stylesComboBox.Items.Count > 0, "Oops! Everything got excluded.");
			foreach (StyleListItem style in m_stylesComboBox.Items)
			{
				if (style.Name != "Default Paragraph Characters")
					Assert.IsTrue(style.Context != ContextValues.General,
						"General style should not have been found.");
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
			CheckDisposed();

			// Styles with a Text context shouldn't show up in the combo box.
			m_styleListHelper.ExcludeStylesWithContext.Add(ContextValues.Text);

			// Also, show only character styles.
			m_styleListHelper.ShowOnlyStylesOfType = StyleType.kstCharacter;

			// Initialize the combo box.
			m_styleListHelper.AddStyles(m_styleSheet);

			Assert.IsTrue(m_stylesComboBox.Items.Count > 0, "Oops! Everything got excluded.");
			foreach (StyleListItem style in m_stylesComboBox.Items)
			{
				Assert.AreEqual(StyleType.kstCharacter, style.Type,
					"Should have only found character styles in combo box, but others were found.");
				Assert.IsTrue(style.Context != ContextValues.Text,
					"Text style should not have been found.");
			}

			// Now show only paragraph styles.
			m_styleListHelper.ShowOnlyStylesOfType = StyleType.kstParagraph;
			m_styleListHelper.Refresh();

			Assert.IsTrue(m_stylesComboBox.Items.Count > 0, "Oops! Everything got excluded.");
			foreach (StyleListItem style in m_stylesComboBox.Items)
			{
				if (style.Name != "Default Paragraph Characters")
				{
					Assert.AreEqual(StyleType.kstParagraph, style.Type,
						"Should have only found character styles in Combo box, but others were found.");
					Assert.IsTrue(style.Context != ContextValues.Text,
						"Text style should not have been found.");
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
			CheckDisposed();

			// set up for basic styles and make sure all the styles are only basic.
			m_styleListHelper.AddStyles(m_styleSheet);
			m_styleListHelper.MaxStyleLevel = 0;
			m_styleListHelper.Refresh();
			foreach (StyleListItem style in m_stylesComboBox.Items)
				Assert.IsTrue(style.UserLevel <= 0, "Non-basic style was added in basic mode");

			// setup for custom styles and make sure the appropriate styles are present.
			m_styleListHelper.AddStyles(m_styleSheet);
			m_styleListHelper.MaxStyleLevel = 2;
			m_styleListHelper.Refresh();
			foreach (StyleListItem style in m_stylesComboBox.Items)
				Assert.IsTrue(style.UserLevel <= 2, "Non-custom style was added in basic mode");
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
			Assert.AreEqual("Caption", m_stylesComboBox.Text);
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
			Assert.AreEqual("", m_stylesComboBox.Text);
			ICollection afterStyles = (ICollection)ReflectionHelper.GetProperty(m_styleListHelper, "Items");
			Assert.AreEqual(beforeStyles.Count, afterStyles.Count, "Selected styles should not change");
		}
	}
}
