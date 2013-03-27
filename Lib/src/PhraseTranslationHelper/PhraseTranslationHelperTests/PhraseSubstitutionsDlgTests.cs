// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: PhraseSubstitutionsDlgTests.cs
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
using NUnit.Framework;

namespace SILUBS.PhraseTranslationHelper
{
	#region class DummyPhraseSubstitutionsDlg
	internal class DummyPhraseSubstitutionsDlg : PhraseSubstitutionsDlg
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DummyPhraseSubstitutionsDlg"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal DummyPhraseSubstitutionsDlg() : base(new Substitution[] { }, new string[] { }, 0)
		{
			TextControl = new TextBox();
		}

		internal TextBox FauxEditedTextControl
		{
			get { return TextControl; }
		}

		internal void ChangeSuffix(string suffix)
		{
			m_txtMatchSuffix.Text = suffix;
		}

		internal void ChangePrefix(string prefix)
		{
			m_txtMatchPrefix.Text = prefix;
		}

		internal void ChangeMatchCount(int c)
		{
			UpdateMatchCount(c);
		}

		public string[] CallGetMatchGroups(string expression)
		{
			return GetMatchGroups(expression);
		}

		public void ChangeMatchGroup(string s)
		{
			if (s == string.Empty)
				s = m_sRemoveItem;
			UpdateMatchGroup(s);
		}
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="Unit test - m_dlg gets disposed in Teardown(); m_textBox is a reference")]
	public class PhraseSubstitutionsDlgTests
	{
		#region Data Members
		private DummyPhraseSubstitutionsDlg m_dlg;
		private TextBox m_textBox;
		#endregion

		#region Setup and Teardown
		[SetUp]
		public void Setup()
		{
			m_dlg = new DummyPhraseSubstitutionsDlg();
			m_textBox = m_dlg.FauxEditedTextControl;
		}

		[TearDown]
		public void Teardown()
		{
			m_dlg.Dispose();
		}
		#endregion

		#region MatchCount tests
		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetExistingMatchCountValue and UpdateMatchCount methods when the text box
		/// is empty - should do nothing.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void MatchCount_EmptyTextBox()
		{
			Assert.AreEqual(1, m_dlg.ExistingMatchCountValue);
			m_dlg.ChangeMatchCount(2);
			Assert.AreEqual(string.Empty, m_textBox.Text);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetExistingMatchCountValue and UpdateMatchCount methods when the selection
		/// precedes all the text in the text box - should do nothing.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void MatchCount_IpPrecedingText()
		{
			m_textBox.Text = "abc";
			m_textBox.SelectionStart = 0;
			m_textBox.SelectionLength = 0;
			Assert.AreEqual(1, m_dlg.ExistingMatchCountValue);
			m_dlg.ChangeMatchCount(2);
			Assert.AreEqual("abc", m_textBox.Text);
			Assert.AreEqual(0, m_textBox.SelectionStart);
			Assert.AreEqual(0, m_textBox.SelectionLength);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetExistingMatchCountValue and UpdateMatchCount methods when the selection
		/// follows all the text in the text box, and there is no explicit number of occurrences
		/// specified.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void MatchCount_NoExistingMatchCount_IpAtEnd()
		{
			m_textBox.Text = "abc";
			m_textBox.SelectionStart = 3;
			m_textBox.SelectionLength = 0;
			Assert.AreEqual(1, m_dlg.ExistingMatchCountValue);
			m_dlg.ChangeMatchCount(2);
			Assert.AreEqual("abc{1,2}", m_textBox.Text);
			Assert.AreEqual("{1,2}", m_textBox.SelectedText);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetExistingMatchCountValue and UpdateMatchCount methods when the all the
		/// text in the text box is selected, and there is no explicit number of occurrences
		/// specified.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void MatchCount_AllTextSelected_NoExistingMatchCount()
		{
			m_textBox.Text = "abc";
			m_textBox.SelectionStart = 0;
			m_textBox.SelectionLength = 3;
			Assert.AreEqual(1, m_dlg.ExistingMatchCountValue);
			m_dlg.ChangeMatchCount(2);
			Assert.AreEqual("(abc){1,2}", m_textBox.Text);
			Assert.AreEqual("{1,2}", m_textBox.SelectedText);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetExistingMatchCountValue and UpdateMatchCount methods when there is an
		/// explicit number of occurrences specified for the final character in the text, and
		/// the entire expression indicating the number of matches is selected.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void MatchCount_FinalExistingMatchCountExpressionSelected_NoGroup_DecrementTo1()
		{
			m_textBox.Text = "abc{1,2}";
			m_textBox.SelectionStart = 3;
			m_textBox.SelectionLength = 5;
			Assert.AreEqual(2, m_dlg.ExistingMatchCountValue);
			m_dlg.ChangeMatchCount(1);
			Assert.AreEqual("abc", m_textBox.Text);
			Assert.AreEqual(3, m_textBox.SelectionStart);
			Assert.AreEqual(0, m_textBox.SelectionLength);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetExistingMatchCountValue and UpdateMatchCount methods when there is an
		/// explicit number of occurrences specified for the final character in the text, and
		/// the entire expression indicating the number of matches is selected.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void MatchCount_FinalExistingMatchCountExpressionSelected_NoGroup_IncrementTo3()
		{
			m_textBox.Text = "abc{1,2}";
			m_textBox.SelectionStart = 3;
			m_textBox.SelectionLength = 5;
			Assert.AreEqual(2, m_dlg.ExistingMatchCountValue);
			m_dlg.ChangeMatchCount(3);
			Assert.AreEqual("abc{1,3}", m_textBox.Text);
			Assert.AreEqual("{1,3}", m_textBox.SelectedText);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetExistingMatchCountValue and UpdateMatchCount methods when the entire
		/// text is grouped but there is no explicit number of occurrences specified for it, and
		/// the entire text is selected.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void MatchCount_EntireTextGroupSelected_AddMatchCount()
		{
			m_textBox.Text = "(abc)";
			m_textBox.SelectionStart = 0;
			m_textBox.SelectionLength = m_textBox.Text.Length;
			Assert.AreEqual(1, m_dlg.ExistingMatchCountValue);
			m_dlg.ChangeMatchCount(2);
			Assert.AreEqual("(abc){1,2}", m_textBox.Text);
			Assert.AreEqual("{1,2}", m_textBox.SelectedText);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetExistingMatchCountValue and UpdateMatchCount methods when there is an
		/// explicit number of occurrences specified for the entire range of characters in the
		/// text, and the entire expression indicating the number of matches is selected.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void MatchCount_FinalExistingMatchCountExpressionSelected_Group_IncrementTo3()
		{
			m_textBox.Text = "(abc){1,2}";
			m_textBox.SelectionStart = 5;
			m_textBox.SelectionLength = 5;
			Assert.AreEqual(2, m_dlg.ExistingMatchCountValue);
			m_dlg.ChangeMatchCount(3);
			Assert.AreEqual("(abc){1,3}", m_textBox.Text);
			Assert.AreEqual("{1,3}", m_textBox.SelectedText);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetExistingMatchCountValue and UpdateMatchCount methods when there is an
		/// explicit number of occurrences specified for the entire range of characters in the
		/// text, and just the numerals indicating the number of matches is selected.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void MatchCount_FinalExistingMatchCountNumberSelected_Group_IncrementTo3()
		{
			m_textBox.Text = "(abc){1,2}";
			m_textBox.SelectionStart = 6;
			m_textBox.SelectionLength = 3;
			Assert.AreEqual(2, m_dlg.ExistingMatchCountValue);
			m_dlg.ChangeMatchCount(3);
			Assert.AreEqual("(abc){1,3}", m_textBox.Text);
			Assert.AreEqual("{1,3}", m_textBox.SelectedText);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetExistingMatchCountValue and UpdateMatchCount methods when there is an
		/// explicit number of occurrences specified for the entire range of characters in the
		/// text, and the entire text (including the match expression) is selected.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void MatchCount_EntireTextSelected_FinalExistingMatchCount_Group_IncrementTo3()
		{
			m_textBox.Text = "(abc){1,2}";
			m_textBox.SelectionStart = 0;
			m_textBox.SelectionLength = m_textBox.Text.Length;
			Assert.AreEqual(2, m_dlg.ExistingMatchCountValue);
			m_dlg.ChangeMatchCount(3);
			Assert.AreEqual("(abc){1,3}", m_textBox.Text);
			Assert.AreEqual("{1,3}", m_textBox.SelectedText);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetExistingMatchCountValue and UpdateMatchCount methods when there is an
		/// explicit number of occurrences specified for the entire range of characters in the
		/// text, and the text (but not the match expression) is selected.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void MatchCount_TextGroupSelected_FinalExistingMatchCount_IncrementTo3()
		{
			m_textBox.Text = "(abc){1,2}";
			m_textBox.SelectionStart = 0;
			m_textBox.SelectionLength = 5;
			Assert.AreEqual(2, m_dlg.ExistingMatchCountValue);
			m_dlg.ChangeMatchCount(3);
			Assert.AreEqual("(abc){1,3}", m_textBox.Text);
			Assert.AreEqual("{1,3}", m_textBox.SelectedText);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetExistingMatchCountValue and UpdateMatchCount methods when there is an
		/// explicit number of occurrences specified for the entire range of characters in the
		/// text, and the text of the group (but not the parentheses that enclose the group) is
		/// selected.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void MatchCount_TextInGroupSelected_FinalExistingMatchCount_IncrementTo3()
		{
			m_textBox.Text = "(abc){1,2}";
			m_textBox.SelectionStart = 1;
			m_textBox.SelectionLength = 3;
			Assert.AreEqual(2, m_dlg.ExistingMatchCountValue);
			m_dlg.ChangeMatchCount(3);
			Assert.AreEqual("(abc){1,3}", m_textBox.Text);
			Assert.AreEqual("{1,3}", m_textBox.SelectedText);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetExistingMatchCountValue and UpdateMatchCount methods when there are two
		/// places where an explicit number of occurrences are specified in the text, and the
		/// insertion point is at the end of the string, following the second one.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void MatchCount_EarlierExistingMatchCount_IpAtEnd_ChangeExistingMatchCount()
		{
			m_textBox.Text = "abc{1,2}def{1,3}";
			m_textBox.SelectionStart = m_textBox.Text.Length;
			m_textBox.SelectionLength = 0;
			Assert.AreEqual(3, m_dlg.ExistingMatchCountValue);
			m_dlg.ChangeMatchCount(6);
			Assert.AreEqual("abc{1,2}def{1,6}", m_textBox.Text);
			Assert.AreEqual("{1,6}", m_textBox.SelectedText);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetExistingMatchCountValue and UpdateMatchCount methods when there is an
		/// earlier place in the text where an explicit number of occurrences is specified, and
		/// the insertion point is at the end of the string.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void MatchCount_EarlierExistingMatchCount_IpAtEnd_AddMatchCount()
		{
			m_textBox.Text = "abc{1,2}def";
			m_textBox.SelectionStart = m_textBox.Text.Length;
			m_textBox.SelectionLength = 0;
			Assert.AreEqual(1, m_dlg.ExistingMatchCountValue);
			m_dlg.ChangeMatchCount(2);
			Assert.AreEqual("abc{1,2}def{1,2}", m_textBox.Text);
			Assert.AreEqual("{1,2}", m_textBox.SelectedText);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetExistingMatchCountValue and UpdateMatchCount methods when there are two
		/// places where an explicit number of occurrences are specified in the text, and the
		/// insertion point is in the text between them.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void MatchCount_EarlierAndLaterExistingMatchCounts_IpInMiddle_AddMatchCount()
		{
			m_textBox.Text = "abc{1,2}def{1,3}";
			m_textBox.SelectionStart = 9;
			m_textBox.SelectionLength = 0;
			Assert.AreEqual(1, m_dlg.ExistingMatchCountValue);
			m_dlg.ChangeMatchCount(2);
			Assert.AreEqual("abc{1,2}d{1,2}ef{1,3}", m_textBox.Text);
			Assert.AreEqual("{1,2}", m_textBox.SelectedText);
			Assert.AreEqual(9, m_textBox.SelectionStart);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetExistingMatchCountValue and UpdateMatchCount methods when there are two
		/// places where an explicit number of occurrences are specified in the text, and the
		/// insertion point is before the number in first one.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void MatchCount_LaterExistingMatchCount_IpBeforeNumberInExistingMatchCount()
		{
			m_textBox.Text = "abc{1,2}def{1,3}";
			m_textBox.SelectionStart = 4;
			m_textBox.SelectionLength = 0;
			Assert.AreEqual(2, m_dlg.ExistingMatchCountValue);
			m_dlg.ChangeMatchCount(3);
			Assert.AreEqual("abc{1,3}def{1,3}", m_textBox.Text);
			Assert.AreEqual("{1,3}", m_textBox.SelectedText);
			Assert.AreEqual(3, m_textBox.SelectionStart);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetExistingMatchCountValue and UpdateMatchCount methods when there are two
		/// places where an explicit number of occurrences are specified in the text, and the
		/// character to which the first one applies is selected.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void MatchCount_LaterExistingMatchCount_CharacterOfExistingMatchCountSelected()
		{
			m_textBox.Text = "abc{1,2}def{1,3}";
			m_textBox.SelectionStart = 3;
			m_textBox.SelectionLength = 1;
			Assert.AreEqual(2, m_dlg.ExistingMatchCountValue);
			m_dlg.ChangeMatchCount(3);
			Assert.AreEqual("abc{1,3}def{1,3}", m_textBox.Text);
			Assert.AreEqual("{1,3}", m_textBox.SelectedText);
			Assert.AreEqual(3, m_textBox.SelectionStart);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetExistingMatchCountValue and UpdateMatchCount methods when there are two
		/// places where an explicit number of occurrences are specified in the text, and the
		/// character and match count expression of the first one is selected.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void MatchCount_LaterExistingMatchCount_CharacterAndExistingMatchCountSelected()
		{
			m_textBox.Text = "abc{1,2}def{1,3}";
			m_textBox.SelectionStart = 3;
			m_textBox.SelectionLength = 6;
			Assert.AreEqual(2, m_dlg.ExistingMatchCountValue);
			m_dlg.ChangeMatchCount(3);
			Assert.AreEqual("abc{1,3}def{1,3}", m_textBox.Text);
			Assert.AreEqual("{1,3}", m_textBox.SelectedText);
			Assert.AreEqual(3, m_textBox.SelectionStart);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetExistingMatchCountValue and UpdateMatchCount methods when there are two
		/// places where an explicit number of occurrences are specified in the text, and the
		/// entire text is selected.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void MatchCount_TwoExistingMatchCounts_EntireTextSelected()
		{
			m_textBox.Text = "abc{1,2}def{1,3}";
			m_textBox.SelectionStart = 0;
			m_textBox.SelectionLength = m_textBox.Text.Length;
			Assert.AreEqual(1, m_dlg.ExistingMatchCountValue);
			m_dlg.ChangeMatchCount(2);
			Assert.AreEqual("(abc{1,2}def{1,3}){1,2}", m_textBox.Text);
			Assert.AreEqual("{1,2}", m_textBox.SelectedText);
			Assert.AreEqual(m_textBox.Text.Length - m_textBox.SelectionLength, m_textBox.SelectionStart);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetExistingMatchCountValue and UpdateMatchCount methods when the selection
		/// follows all the text in the text box, and there is an explicit range specified that
		/// does not start at 1.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void MatchCount_ExistingMatchCountStartsAt2_IpAtEnd_DecrementTo99()
		{
			m_textBox.Text = "abc{2,100}";
			m_textBox.SelectionStart = m_textBox.Text.Length;
			m_textBox.SelectionLength = 0;
			Assert.AreEqual(100, m_dlg.ExistingMatchCountValue);
			m_dlg.ChangeMatchCount(99);
			Assert.AreEqual("abc{2,99}", m_textBox.Text);
			Assert.AreEqual("{2,99}", m_textBox.SelectedText);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetExistingMatchCountValue and UpdateMatchCount methods when there is an
		/// explicit absolute match count specified and we increment it -- should convert it to
		/// a range.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void MatchCount_ConvertExistingAbsoluteMatchCountToRange()
		{
			m_textBox.Text = "abc{2}";
			m_textBox.SelectionStart = 3;
			m_textBox.SelectionLength = 3;
			Assert.AreEqual(2, m_dlg.ExistingMatchCountValue);
			m_dlg.ChangeMatchCount(3);
			Assert.AreEqual("abc{1,3}", m_textBox.Text);
			Assert.AreEqual("{1,3}", m_textBox.SelectedText);
		}
		#endregion

		#region Prefix and Suffix tests
		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting and setting prefix when the text box is initially empty.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void Prefix_EmptyTextBox()
		{
			Assert.AreEqual(string.Empty, m_dlg.ExistingPrefix);
			m_dlg.ChangePrefix(@"pre");
			Assert.AreEqual(@"\bpre", m_textBox.Text);
			Assert.AreEqual(@"pre", m_textBox.SelectedText);
			Assert.AreEqual(@"pre", m_dlg.ExistingPrefix);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests starting a new (single-character) prefix at the end of some existing text in
		/// the text box.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void Prefix_StartNewPrefixAtEndOfExistingText()
		{
			m_textBox.Text = @"Some words ";
			m_textBox.SelectionStart = m_textBox.Text.Length;
			m_textBox.SelectionLength = 0;
			Assert.AreEqual(string.Empty, m_dlg.ExistingPrefix);
			m_dlg.ChangePrefix(@"p");
			Assert.AreEqual(@"Some words \bp", m_textBox.Text);
			Assert.AreEqual(@"p", m_textBox.SelectedText);
			Assert.AreEqual(@"p", m_dlg.ExistingPrefix);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting and setting suffix when the text box is initially empty.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void Suffix_EmptyTextBox()
		{
			Assert.AreEqual(string.Empty, m_dlg.ExistingSuffix);
			m_dlg.ChangeSuffix(@"suf");
			Assert.AreEqual(@"suf\b", m_textBox.Text);
			Assert.AreEqual(@"suf", m_textBox.SelectedText);
			Assert.AreEqual(@"suf", m_dlg.ExistingSuffix);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting and setting prefix when the text box initially consists of nothing but
		/// a prefix, and the insertion point is at the start.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void Prefix_EntireTextBoxIsPrefix_IpAtStart()
		{
			m_textBox.Text = @"\bpre";
			m_textBox.SelectionStart = 0;
			m_textBox.SelectionLength = 0;
			Assert.AreEqual(@"pre", m_dlg.ExistingPrefix);
			m_dlg.ChangePrefix(@"ante");
			Assert.AreEqual(@"\bante", m_textBox.Text);
			Assert.AreEqual(@"ante", m_textBox.SelectedText);
			Assert.AreEqual(@"ante", m_dlg.ExistingPrefix);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting and setting suffix when the text box initially consists of nothing but
		/// a suffix, and the insertion point is at the start.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void Suffix_EntireTextBoxIsPrefix_IpAtStart()
		{
			m_textBox.Text = @"suf\b";
			m_textBox.SelectionStart = 0;
			m_textBox.SelectionLength = 0;
			Assert.AreEqual(@"suf", m_dlg.ExistingSuffix);
			m_dlg.ChangeSuffix(@"post");
			Assert.AreEqual(@"post\b", m_textBox.Text);
			Assert.AreEqual(@"post", m_textBox.SelectedText);
			Assert.AreEqual(@"post", m_dlg.ExistingSuffix);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting and setting prefix when the text box initially consists of nothing but
		/// a prefix, and the insertion point is at the end.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void Prefix_EntireTextBoxIsPrefix_IpAtEnd()
		{
			m_textBox.Text = @"\bpre";
			m_textBox.SelectionStart = m_textBox.Text.Length;
			m_textBox.SelectionLength = 0;
			Assert.AreEqual(@"pre", m_dlg.ExistingPrefix);
			m_dlg.ChangePrefix(@"ante");
			Assert.AreEqual(@"\bante", m_textBox.Text);
			Assert.AreEqual(@"ante", m_textBox.SelectedText);
			Assert.AreEqual(@"ante", m_dlg.ExistingPrefix);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting and setting suffix when the text box initially consists of nothing but
		/// a suffix, and the insertion point is at the end.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void Suffix_EntireTextBoxIsPrefix_IpAtEnd()
		{
			m_textBox.Text = @"suf\b";
			m_textBox.SelectionStart = m_textBox.Text.Length;
			m_textBox.SelectionLength = 0;
			Assert.AreEqual(@"suf", m_dlg.ExistingSuffix);
			m_dlg.ChangeSuffix(@"post");
			Assert.AreEqual(@"post\b", m_textBox.Text);
			Assert.AreEqual(@"post", m_textBox.SelectedText);
			Assert.AreEqual(@"post", m_dlg.ExistingSuffix);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting and setting prefix when the text box initially consists of nothing but
		/// a prefix, and the entire text box text is selected.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void Prefix_EntireTextBoxIsPrefix_EntireTextSelected()
		{
			m_textBox.Text = @"\bpre";
			m_textBox.SelectionStart = 0;
			m_textBox.SelectionLength = m_textBox.Text.Length;
			Assert.AreEqual(@"pre", m_dlg.ExistingPrefix);
			m_dlg.ChangePrefix(@"ante");
			Assert.AreEqual(@"\bante", m_textBox.Text);
			Assert.AreEqual(@"ante", m_textBox.SelectedText);
			Assert.AreEqual(@"ante", m_dlg.ExistingPrefix);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting and setting suffix when the text box initially consists of nothing but
		/// a suffix, and the entire text box text is selected.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void Suffix_EntireTextBoxIsPrefix_EntireTextSelected()
		{
			m_textBox.Text = @"suf\b";
			m_textBox.SelectionStart = 0;
			m_textBox.SelectionLength = m_textBox.Text.Length;
			Assert.AreEqual(@"suf", m_dlg.ExistingSuffix);
			m_dlg.ChangeSuffix(@"post");
			Assert.AreEqual(@"post\b", m_textBox.Text);
			Assert.AreEqual(@"post", m_textBox.SelectedText);
			Assert.AreEqual(@"post", m_dlg.ExistingSuffix);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting and setting prefix when the text box initially consists of multiple
		/// words including a prefix (which is not the last thing in the text), and the insertion
		/// point is at the end.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void Prefix_EarlierPrefix_InsertNewPrefixAtEnd()
		{
			m_textBox.Text = @"ed\b \bpre(\w+) thing ";
			m_textBox.SelectionStart = m_textBox.Text.Length;
			m_textBox.SelectionLength = 0;
			Assert.AreEqual(string.Empty, m_dlg.ExistingPrefix);
			m_dlg.ChangePrefix(@"ante");
			Assert.AreEqual(@"ed\b \bpre(\w+) thing \bante", m_textBox.Text);
			Assert.AreEqual(@"ante", m_textBox.SelectedText);
			Assert.AreEqual(@"ante", m_dlg.ExistingPrefix);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting and setting suffix when the text box initially consists of multiple
		/// words including a suffix (which is not the last thing in the text), and the insertion
		/// point is at the end.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void Suffix_EarlierSuffix_InsertNewSuffixAtEnd()
		{
			m_textBox.Text = @"ed\b \bpre(\w+) thing ";
			m_textBox.SelectionStart = m_textBox.Text.Length;
			m_textBox.SelectionLength = 0;
			Assert.AreEqual(string.Empty, m_dlg.ExistingSuffix);
			m_dlg.ChangeSuffix(@"post");
			Assert.AreEqual(@"ed\b \bpre(\w+) thing post\b", m_textBox.Text);
			Assert.AreEqual(@"post", m_textBox.SelectedText);
			Assert.AreEqual(@"post", m_dlg.ExistingSuffix);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting and setting prefix when the text box initially consists of multiple
		/// words including a prefix (which is not the first thing in the text), and the insertion
		/// point is at the beginning.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void Prefix_LaterPrefix_InsertNewPrefixAtBeginning()
		{
			m_textBox.Text = @" ed\b \bpre(\w+) thing";
			m_textBox.SelectionStart = 0;
			m_textBox.SelectionLength = 0;
			Assert.AreEqual(string.Empty, m_dlg.ExistingPrefix);
			m_dlg.ChangePrefix(@"ante");
			Assert.AreEqual(@"\bante ed\b \bpre(\w+) thing", m_textBox.Text);
			Assert.AreEqual(@"ante", m_textBox.SelectedText);
			Assert.AreEqual(@"ante", m_dlg.ExistingPrefix);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting and setting suffix when the text box initially consists of multiple
		/// words including a suffix (which is not the first thing in the text), and the insertion
		/// point is at the beginning.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void Suffix_LaterSuffix_InsertNewSuffixAtBeginning()
		{
			m_textBox.Text = @" \bpre(\w+) ed\b thing";
			m_textBox.SelectionStart = 0;
			m_textBox.SelectionLength = 0;
			Assert.AreEqual(string.Empty, m_dlg.ExistingSuffix);
			m_dlg.ChangeSuffix(@"post");
			Assert.AreEqual(@"post\b \bpre(\w+) ed\b thing", m_textBox.Text);
			Assert.AreEqual(@"post", m_textBox.SelectedText);
			Assert.AreEqual(@"post", m_dlg.ExistingSuffix);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting and changing an existing prefix when the text box initially consists of
		/// multiple prefixes. The middle prefix is selected and replaced by newly entered text.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void Prefix_ReplacePrefixInMiddle_EntirePrefixSelected()
		{
			m_textBox.Text = @"\bpre(\w+) \bmid(\w+) \blast";
			m_textBox.SelectionStart = 11;
			m_textBox.SelectionLength = 5;
			Assert.AreEqual("mid", m_dlg.ExistingPrefix);
			m_dlg.ChangePrefix(@"midd");
			Assert.AreEqual(@"\bpre(\w+) \bmidd(\w+) \blast", m_textBox.Text);
			m_dlg.ChangePrefix(@"middl");
			Assert.AreEqual(@"\bpre(\w+) \bmiddl(\w+) \blast", m_textBox.Text);
			m_dlg.ChangePrefix(@"middle");
			Assert.AreEqual(@"\bpre(\w+) \bmiddle(\w+) \blast", m_textBox.Text);
			Assert.AreEqual(@"middle", m_textBox.SelectedText);
			Assert.AreEqual(@"middle", m_dlg.ExistingPrefix);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting and changing an existing suffix when the text box initially consists of
		/// multiple suffixes. The middle suffix is selected and replaced by newly entered text.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void Suffix_ReplaceSuffixInMiddle_EntireSuffixSelected()
		{
			m_textBox.Text = @"post\b(\w+) (\w+)mid\b (\w+)last\b";
			m_textBox.SelectionStart = 17;
			m_textBox.SelectionLength = 5;
			Assert.AreEqual("mid", m_dlg.ExistingSuffix);
			m_dlg.ChangeSuffix(@"midd");
			Assert.AreEqual(@"post\b(\w+) (\w+)midd\b (\w+)last\b", m_textBox.Text);
			m_dlg.ChangeSuffix(@"middl");
			Assert.AreEqual(@"post\b(\w+) (\w+)middl\b (\w+)last\b", m_textBox.Text);
			m_dlg.ChangeSuffix(@"middle");
			Assert.AreEqual(@"post\b(\w+) (\w+)middle\b (\w+)last\b", m_textBox.Text);
			Assert.AreEqual(@"middle", m_textBox.SelectedText);
			Assert.AreEqual(@"middle", m_dlg.ExistingSuffix);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests removing an existing prefix.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void Prefix_RemovePrefix()
		{
			m_textBox.Text = @"Good \bpre";
			m_textBox.SelectionStart = 7;
			m_textBox.SelectionLength = 3;
			Assert.AreEqual("pre", m_dlg.ExistingPrefix);
			m_dlg.ChangePrefix(@"pr");
			Assert.AreEqual(@"Good \bpr", m_textBox.Text);
			m_dlg.ChangePrefix(@"p");
			Assert.AreEqual(@"Good \bp", m_textBox.Text);
			m_dlg.ChangePrefix(string.Empty);
			Assert.AreEqual(@"Good ", m_textBox.Text);
			Assert.AreEqual(string.Empty, m_textBox.SelectedText);
			Assert.AreEqual(5, m_textBox.SelectionStart);
			Assert.AreEqual(string.Empty, m_dlg.ExistingPrefix);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests removing an existing suffix.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void Suffix_RemoveSuffix()
		{
			m_textBox.Text = @"ed\b here";
			m_textBox.SelectionStart = 0;
			m_textBox.SelectionLength = 2;
			Assert.AreEqual("ed", m_dlg.ExistingSuffix);
			m_dlg.ChangeSuffix(@"e");
			Assert.AreEqual(@"e\b here", m_textBox.Text);
			m_dlg.ChangeSuffix(string.Empty);
			Assert.AreEqual(@" here", m_textBox.Text);
			Assert.AreEqual(string.Empty, m_textBox.SelectedText);
			Assert.AreEqual(0, m_textBox.SelectionStart);
			Assert.AreEqual(string.Empty, m_dlg.ExistingSuffix);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests entering a space in the prefix text box when an existing prefix is selected.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void Prefix_EnterWhitespaceAsPrefix()
		{
			m_textBox.Text = @"Good \bpre";
			m_textBox.SelectionStart = 7;
			m_textBox.SelectionLength = 3;
			Assert.AreEqual("pre", m_dlg.ExistingPrefix);
			m_dlg.ChangePrefix(@" ");
			Assert.AreEqual(@"Good ", m_textBox.Text);
			Assert.AreEqual(string.Empty, m_textBox.SelectedText);
			Assert.AreEqual(5, m_textBox.SelectionStart);
			Assert.AreEqual(string.Empty, m_dlg.ExistingPrefix);
		}
		#endregion

		#region GetMatchGroups tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to get the match groups when input is an empty string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetMatchGroups_EmptyMatchExpr()
		{
			string[] matches = m_dlg.CallGetMatchGroups(string.Empty);
			Assert.AreEqual(1, matches.Length);
			Assert.AreEqual(PhraseSubstitutionsDlg.kEntireMatch, matches[0]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to get the match groups when input is a string with no match groups.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetMatchGroups_NoMatchGroupsInExpr()
		{
			string[] matches = m_dlg.CallGetMatchGroups("No match groups here, folks!");
			Assert.AreEqual(1, matches.Length);
			Assert.AreEqual(PhraseSubstitutionsDlg.kEntireMatch, matches[0]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to get the match groups when input is a string with no match groups
		/// but has literal parentheses.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetMatchGroups_NoMatchGroupsInExpr_LiteralParens()
		{
			string[] matches = m_dlg.CallGetMatchGroups(@"No \(match groups\) here, folks!");
			Assert.AreEqual(1, matches.Length);
			Assert.AreEqual(PhraseSubstitutionsDlg.kEntireMatch, matches[0]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to get the match groups when input consists of a single match group.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetMatchGroups_EntireExprIsAGroup()
		{
			string[] matches = m_dlg.CallGetMatchGroups("(1 match group here)");
			Assert.AreEqual(2, matches.Length);
			Assert.AreEqual(PhraseSubstitutionsDlg.kEntireMatch, matches[0]);
			Assert.AreEqual("1", matches[1]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to get the match groups when input is a string with 1 un-named match
		/// group.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetMatchGroups_ExprContainsOneGroup_UnNamed()
		{
			string[] matches = m_dlg.CallGetMatchGroups("before (1 match group here) after");
			Assert.AreEqual(2, matches.Length);
			Assert.AreEqual(PhraseSubstitutionsDlg.kEntireMatch, matches[0]);
			Assert.AreEqual("1", matches[1]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to get the match groups when input is a string with 1 named match
		/// group.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetMatchGroups_ExprContainsOneGroup_Named()
		{
			string[] matches = m_dlg.CallGetMatchGroups("before (?<grp1>1 match group here) after");
			Assert.AreEqual(2, matches.Length);
			Assert.AreEqual(PhraseSubstitutionsDlg.kEntireMatch, matches[0]);
			Assert.AreEqual("grp1", matches[1]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to get the match groups when input is a string with two
		/// un-nested match groups.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetMatchGroups_ExprContainsTwoGroups_UnNested_UnNamed()
		{
			string[] matches = m_dlg.CallGetMatchGroups(@"before (\S+) verb(\bed) after");
			Assert.AreEqual(3, matches.Length);
			Assert.AreEqual(PhraseSubstitutionsDlg.kEntireMatch, matches[0]);
			Assert.AreEqual("1", matches[1]);
			Assert.AreEqual("2", matches[2]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to get the match groups when input is a string with two
		/// un-nested match groups.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetMatchGroups_ExprContainsTwoGroups_UnNested_Named()
		{
			string[] matches = m_dlg.CallGetMatchGroups(@"before (?'word'\S+) verb(?<edSuffix>\bed) after");
			Assert.AreEqual(3, matches.Length);
			Assert.AreEqual(PhraseSubstitutionsDlg.kEntireMatch, matches[0]);
			Assert.AreEqual("word", matches[1]);
			Assert.AreEqual("edSuffix", matches[2]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to get the match groups when input is a string with a match group
		/// nested inside another.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetMatchGroups_ExprContainsTwoGroups_NestedNamedAndUnNamed()
		{
			string[] matches = m_dlg.CallGetMatchGroups(@"before (?<namedGroup>why (\S+) not\?) after");
			Assert.AreEqual(3, matches.Length);
			Assert.AreEqual(PhraseSubstitutionsDlg.kEntireMatch, matches[0]);
			Assert.AreEqual("1", matches[1]);
			Assert.AreEqual("namedGroup", matches[2]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to get the match groups when input is a string that cannot
		/// be interpreted as a legal regular expression.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetMatchGroups_ExprInvalid()
		{
			Assert.AreEqual(0, m_dlg.CallGetMatchGroups(@"before (?<named group>why (\S+) not\?) after").Length);
		}
		#endregion

		#region MatchGroup tests
		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ExistingMatchGroup when the text box is empty.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void MatchGroup_GetExisting_EmptyTextBox()
		{
			Assert.AreEqual(string.Empty, m_dlg.ExistingMatchGroup);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a numeric match group in an empty text box.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void MatchGroup_InsertNumeric_EmptyTextBox()
		{
			m_dlg.ChangeMatchGroup("2");
			Assert.AreEqual("$2", m_textBox.Text);
			Assert.AreEqual("2", m_dlg.ExistingMatchGroup);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a named match group in an empty text box.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void MatchGroup_InsertNamed_EmptyTextBox()
		{
			m_dlg.ChangeMatchGroup("willy");
			Assert.AreEqual("${willy}", m_textBox.Text);
			Assert.AreEqual("willy", m_dlg.ExistingMatchGroup);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a match group corresponding to the entire match in an empty text box.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void MatchGroup_InsertEntireMatch_EmptyTextBox()
		{
			m_dlg.ChangeMatchGroup(PhraseSubstitutionsDlg.kEntireMatch);
			Assert.AreEqual("$&", m_textBox.Text);
			Assert.AreEqual("&", m_textBox.SelectedText);
			Assert.AreEqual(PhraseSubstitutionsDlg.kEntireMatch, m_dlg.ExistingMatchGroup);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ExistingMatchGroup and UpdateMatchGroup methods when the selection
		/// precedes all the text in the text box - should do nothing.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void MatchGroup_IpPrecedingText_InsertNumericMatchGroup()
		{
			m_textBox.Text = "abc";
			m_textBox.SelectionStart = 0;
			m_textBox.SelectionLength = 0;
			Assert.AreEqual(string.Empty, m_dlg.ExistingMatchGroup);
			m_dlg.ChangeMatchGroup("2");
			Assert.AreEqual("$2abc", m_textBox.Text);
			Assert.AreEqual("2", m_textBox.SelectedText);
			Assert.AreEqual(1, m_textBox.SelectionStart);
			Assert.AreEqual("2", m_dlg.ExistingMatchGroup);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ExistingMatchGroup and UpdateMatchGroup methods when the selection
		/// follows all the text in the text box, and there is no explicit number of occurrences
		/// specified.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void MatchGroup_NoExistingMatchGroup_IpAtEnd_InsertNumericMatchGroup()
		{
			m_textBox.Text = "abc";
			m_textBox.SelectionStart = 3;
			m_textBox.SelectionLength = 0;
			Assert.AreEqual(string.Empty, m_dlg.ExistingMatchGroup);
			m_dlg.ChangeMatchGroup("2");
			Assert.AreEqual("abc$2", m_textBox.Text);
			Assert.AreEqual("2", m_textBox.SelectedText);
			Assert.AreEqual("2", m_dlg.ExistingMatchGroup);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ExistingMatchGroup method when the text box contains some text followed by
		/// a substitution expression for group 0 (which is synonymous with $& - substitute
		/// entire match), and the insertion point is at the end. Then remove that group
		/// substitution expression.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void MatchGroup_GetAndRemoveExisting_IpAfterGroupZero()
		{
			m_textBox.Text = "abc $0";
			m_textBox.SelectionStart = m_textBox.Text.Length;
			m_textBox.SelectionLength = 0;
			Assert.AreEqual(PhraseSubstitutionsDlg.kEntireMatch, m_dlg.ExistingMatchGroup);
			m_dlg.ChangeMatchGroup(string.Empty);
			Assert.AreEqual("abc ", m_textBox.Text);
			Assert.AreEqual(4, m_textBox.SelectionStart);
			Assert.AreEqual(0, m_textBox.SelectionLength);
			Assert.AreEqual(string.Empty, m_dlg.ExistingMatchGroup);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ExistingMatchGroup method when the text box contains a named substitution
		/// expression, and the insertion point is at the end. Then remove that group
		/// substitution expression.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void MatchGroup_GetAndRemoveExisting_IpAfterNamedGroup()
		{
			m_textBox.Text = "${yu}";
			m_textBox.SelectionStart = m_textBox.Text.Length;
			m_textBox.SelectionLength = 0;
			Assert.AreEqual("yu", m_dlg.ExistingMatchGroup);
			m_dlg.ChangeMatchGroup(string.Empty);
			Assert.AreEqual("", m_textBox.Text);
			Assert.AreEqual(0, m_textBox.SelectionStart);
			Assert.AreEqual(0, m_textBox.SelectionLength);
			Assert.AreEqual(string.Empty, m_dlg.ExistingMatchGroup);
		}
		#endregion
	}
}
