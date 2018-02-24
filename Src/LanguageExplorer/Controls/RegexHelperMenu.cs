// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.FwCoreDlgs.Controls;

namespace LanguageExplorer.Controls
{
	/// <summary>
	/// Context menu to help build regular expressions.  To be used in Find dialog boxes with regex support.
	/// </summary>
	public class RegexHelperMenu : HelperMenu
	{
		private bool m_isFind;

		/// <summary>
		/// Constructor for Regex Helper Context Menu, assumes it is for a search
		/// </summary>
		/// <param name="textbox">the textbox to insert regex characters into</param>
		/// <param name="helpTopicProvider">usually IHelpTopicProvider.App</param>
		public RegexHelperMenu(FwTextBox textbox, IHelpTopicProvider helpTopicProvider) : this(textbox, helpTopicProvider, true)
		{
		}

		/// <summary>
		/// Constructor for the Regex Helper Context Menu
		/// </summary>
		/// <param name="textbox">the textbox to insert regex characters into</param>
		/// <param name="helpTopicProvider">usually IHelpTopicProvider.App</param>
		/// <param name="isFind">True if the menu is for searching, false if it is for replacing (shows the $n options)</param>
		public RegexHelperMenu(FwTextBox textbox, IHelpTopicProvider helpTopicProvider, bool isFind) : base(textbox, helpTopicProvider)
		{
			m_isFind = isFind;
			Init();
		}

		/// <summary>
		/// Initializes the menu for either "Find" or "Replace"
		/// </summary>
		private void Init()
		{
			if (m_isFind)
			{
				MenuItems.Add(string.Format(FwCoreDlgs.ksREBeginLine, "^"), caret);
				MenuItems.Add(string.Format(FwCoreDlgs.ksREEndLine, "$"), dollarSign);
				MenuItems.Add(string.Format(FwCoreDlgs.ksREAnyChar, "."), dot);
				MenuItems.Add(string.Format(FwCoreDlgs.ksRECharFromSet, "[]"), charClass);
				MenuItems.Add(string.Format(FwCoreDlgs.ksRECharNotFromSet, "[^]"), invCharClass);
				MenuItems.Add(string.Format(FwCoreDlgs.ksREAlternation, "|"), pipe);
				MenuItems.Add(string.Format(FwCoreDlgs.ksREGrouping, "()"), parens);
				MenuItems.Add(string.Format(FwCoreDlgs.ksREQuoteMeta, "\\"), backslash);
				MenuItems.Add("-");
				MenuItems.Add(string.Format(FwCoreDlgs.ksREZeroOrMore, "*"), star);
				MenuItems.Add(string.Format(FwCoreDlgs.ksREOneOrMore, "+"), plus);
				MenuItems.Add(string.Format(FwCoreDlgs.ksREOptional, "?"), questionMark);
				MenuItems.Add(string.Format(FwCoreDlgs.ksRENTimes, "{n}"), curlyBrackets);
				MenuItems.Add("-");
				MenuItems.Add(string.Format(FwCoreDlgs.ksREWordChar, "\\w"), word);
				MenuItems.Add(string.Format(FwCoreDlgs.ksRENonwordChar, "\\W"), nonWord);
				MenuItems.Add(string.Format(FwCoreDlgs.ksRESpaceChar, "\\s"), space);
				MenuItems.Add(string.Format(FwCoreDlgs.ksRENonspaceChar, "\\S"), nonSpace);
				MenuItems.Add(string.Format(FwCoreDlgs.ksREDigitChar, "\\d"), digit);
				MenuItems.Add(string.Format(FwCoreDlgs.ksRENondigitChar, "\\D"), nonDigit);
				MenuItems.Add(string.Format(FwCoreDlgs.ksREBoundaryChar, "\\b"), boundary);
				MenuItems.Add(string.Format(FwCoreDlgs.ksRENonboundaryChar, "\\B"), nonBoundary);
				MenuItems.Add("-");
				MenuItems.Add(string.Format(FwCoreDlgs.ksREFirstCapture, "\\1"), findCapture1);
				MenuItems.Add(string.Format(FwCoreDlgs.ksRESecondCapture, "\\2"), findCapture2);
				MenuItems.Add(string.Format(FwCoreDlgs.ksREThirdCapture, "\\3"), findCapture3);
				MenuItems.Add(string.Format(FwCoreDlgs.ksRENthCapture, "\\n"), findCapturen);
			}
			else
			{
				MenuItems.Add(string.Format(FwCoreDlgs.ksREFirstCapture, "$1"), replaceCapture1);
				MenuItems.Add(string.Format(FwCoreDlgs.ksRESecondCapture, "$2"), replaceCapture2);
				MenuItems.Add(string.Format(FwCoreDlgs.ksREThirdCapture, "$3"), replaceCapture3);
				MenuItems.Add(string.Format(FwCoreDlgs.ksRENthCapture, "$n"), replaceCapturen);
			}
			MenuItems.Add("-");
			MenuItems.Add(FwCoreDlgs.ksREHelp, showHelp);
		}

		// Text insertion methods

		// This one will enclose the highlighted text with parenthesis and add the specified text afterward
		private void groupRegexText(string text)
		{
			var selLen = m_textbox.SelectionLength;
			if(selLen > 0)
			{
				GroupText("(", ")");
				var selStart = m_textbox.SelectionStart + 1;
				m_textbox.Text = m_textbox.Text.Insert(selStart, text);
				m_textbox.Focus();
				m_textbox.Select(selStart + text.Length, 0);
			}
			else
			{
				InsertText(text);
			}
			m_textbox.Refresh();
		}

		// Event handlers
		private void backslash(object sender, EventArgs e)
		{
			InsertText(@"\");
		}

		private void dot(object sender, EventArgs e)
		{
			InsertText(".");
		}

		private void parens(object sender, EventArgs e)
		{
			GroupText("(", ")");
		}

		private void charClass(object sender, EventArgs e)
		{
			GroupText("[", "]");
		}

		private void invCharClass(object sender, EventArgs e)
		{
			GroupText("[^", "]");
		}

		private void star(object sender, EventArgs e)
		{
			groupRegexText("*");
		}

		private void plus(object sender, EventArgs e)
		{
			groupRegexText("+");
		}

		private void questionMark(object sender, EventArgs e)
		{
			groupRegexText("?");
		}

		private void caret(object sender, EventArgs e)
		{
			InsertText("^");
		}

		private void dollarSign(object sender, EventArgs e)
		{
			InsertText("$");
		}

		private void pipe(object sender, EventArgs e)
		{
			InsertText("|");
		}

		/// <summary>
		/// Curlies the brackets.
		/// </summary>
		private void curlyBrackets(object sender, EventArgs e)
		{
			groupRegexText("{}");
			// Need to move back one because in this case it makes sense to be inside the curly brackets
			m_textbox.Select(Math.Max(m_textbox.SelectionStart - 1, 0), 0);
		}

		private void boundary(object sender, EventArgs e)
		{
			InsertText(@"\b");
		}

		private void digit(object sender, EventArgs e)
		{
			InsertText(@"\d");
		}

		private void word(object sender, EventArgs e)
		{
			InsertText(@"\w");
		}

		private void nonWord(object sender, EventArgs e)
		{
			InsertText(@"\W");
		}

		private void space(object sender, EventArgs e)
		{
			InsertText(@"\s");
		}

		private void nonSpace(object sender, EventArgs e)
		{
			InsertText(@"\S");
		}

		private void nonDigit(object sender, EventArgs e)
		{
			InsertText(@"\D");
		}

		private void nonBoundary(object sender, EventArgs e)
		{
			InsertText(@"\B");
		}

		private void findCapture1(object sender, EventArgs e)
		{
			InsertText(@"\1");
		}

		private void findCapture2(object sender, EventArgs e)
		{
			InsertText(@"\2");
		}

		private void findCapture3(object sender, EventArgs e)
		{
			InsertText(@"\3");
		}

		private void findCapturen(object sender, EventArgs e)
		{
			InsertText(@"\");
		}

		private void replaceCapture1(object sender, EventArgs e)
		{
			InsertText(@"$1");
		}

		private void replaceCapture2(object sender, EventArgs e)
		{
			InsertText(@"$2");
		}

		private void replaceCapture3(object sender, EventArgs e)
		{
			InsertText(@"$3");
		}

		private void replaceCapturen(object sender, EventArgs e)
		{
			InsertText(@"$");
		}

		private void showHelp(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpRegexes");
		}
	}
}