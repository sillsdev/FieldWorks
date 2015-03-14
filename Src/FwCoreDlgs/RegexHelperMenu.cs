using System;
using System.Diagnostics.CodeAnalysis;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.Common.FwUtils;
using XCore;

namespace SIL.FieldWorks.FwCoreDlgs
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
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="Added to MenuItems collection and disposed there.")]
		private void Init()
		{
			if (m_isFind)
			{
				MenuItems.Add(String.Format(FwCoreDlgs.ksREBeginLine, "^"), new EventHandler(caret));
				MenuItems.Add(String.Format(FwCoreDlgs.ksREEndLine, "$"), new EventHandler(dollarSign));
				MenuItems.Add(String.Format(FwCoreDlgs.ksREAnyChar, "."), new EventHandler(dot));
				MenuItems.Add(String.Format(FwCoreDlgs.ksRECharFromSet, "[]"), new EventHandler(charClass));
				MenuItems.Add(String.Format(FwCoreDlgs.ksRECharNotFromSet, "[^]"), new EventHandler(invCharClass));
				MenuItems.Add(String.Format(FwCoreDlgs.ksREAlternation, "|"), new EventHandler(pipe));
				MenuItems.Add(String.Format(FwCoreDlgs.ksREGrouping, "()"), new EventHandler(parens));
				MenuItems.Add(String.Format(FwCoreDlgs.ksREQuoteMeta, "\\"), new EventHandler(backslash));
				MenuItems.Add("-");
				MenuItems.Add(String.Format(FwCoreDlgs.ksREZeroOrMore, "*"), new EventHandler(star));
				MenuItems.Add(String.Format(FwCoreDlgs.ksREOneOrMore, "+"), new EventHandler(plus));
				MenuItems.Add(String.Format(FwCoreDlgs.ksREOptional, "?"), new EventHandler(questionMark));
				MenuItems.Add(String.Format(FwCoreDlgs.ksRENTimes, "{n}"), new EventHandler(curlyBrackets));
				MenuItems.Add("-");
				MenuItems.Add(String.Format(FwCoreDlgs.ksREWordChar, "\\w"), new EventHandler(word));
				MenuItems.Add(String.Format(FwCoreDlgs.ksRENonwordChar, "\\W"), new EventHandler(nonWord));
				MenuItems.Add(String.Format(FwCoreDlgs.ksRESpaceChar, "\\s"), new EventHandler(space));
				MenuItems.Add(String.Format(FwCoreDlgs.ksRENonspaceChar, "\\S"), new EventHandler(nonSpace));
				MenuItems.Add(String.Format(FwCoreDlgs.ksREDigitChar, "\\d"), new EventHandler(digit));
				MenuItems.Add(String.Format(FwCoreDlgs.ksRENondigitChar, "\\D"), new EventHandler(nonDigit));
				MenuItems.Add(String.Format(FwCoreDlgs.ksREBoundaryChar, "\\b"), new EventHandler(boundary));
				MenuItems.Add(String.Format(FwCoreDlgs.ksRENonboundaryChar, "\\B"), new EventHandler(nonBoundary));
				MenuItems.Add("-");
				MenuItems.Add(String.Format(FwCoreDlgs.ksREFirstCapture, "\\1"), new EventHandler(findCapture1));
				MenuItems.Add(String.Format(FwCoreDlgs.ksRESecondCapture, "\\2"), new EventHandler(findCapture2));
				MenuItems.Add(String.Format(FwCoreDlgs.ksREThirdCapture, "\\3"), new EventHandler(findCapture3));
				MenuItems.Add(String.Format(FwCoreDlgs.ksRENthCapture, "\\n"), new EventHandler(findCapturen));
			}
			else
			{
				MenuItems.Add(String.Format(FwCoreDlgs.ksREFirstCapture, "$1"), new EventHandler(replaceCapture1));
				MenuItems.Add(String.Format(FwCoreDlgs.ksRESecondCapture, "$2"), new EventHandler(replaceCapture2));
				MenuItems.Add(String.Format(FwCoreDlgs.ksREThirdCapture, "$3"), new EventHandler(replaceCapture3));
				MenuItems.Add(String.Format(FwCoreDlgs.ksRENthCapture, "$n"), new EventHandler(replaceCapturen));
			}
			MenuItems.Add("-");
			MenuItems.Add(FwCoreDlgs.ksREHelp, new EventHandler(showHelp));
		}

		// Text insertion methods

		// This one will enclose the highlighted text with parenthesis and add the specified text afterward
		private void groupRegexText(string text)
		{
			int selLen = m_textbox.SelectionLength;

			if(selLen > 0)
			{
				GroupText("(", ")");
				int selStart = m_textbox.SelectionStart + 1;
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
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
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
