using System;
using System.Windows.Forms;

using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// Context menu to help build text expressions. Subclassed to provide regex and morpheme break building help
	/// </summary>
	public abstract class HelperMenu : ContextMenu, IFWDisposable
	{
		/// <summary>
		/// The textbox to insert text into
		/// </summary>
		protected FwTextBox m_textbox;
		/// <summary>
		/// For providing help
		/// </summary>
		protected IHelpTopicProvider m_helpTopicProvider;

		/// <summary>
		/// Constructor for Helper Context Menu.
		/// </summary>
		/// <param name="textbox">the textbox to insert regex characters into</param>
		/// <param name="helpTopicProvider">usually FwApp.App</param>
		public HelperMenu(FwTextBox textbox, IHelpTopicProvider helpTopicProvider)
		{
			m_helpTopicProvider = helpTopicProvider;
			m_textbox = textbox;
		}

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		///
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		private bool m_isDisposed = false;

		/// <summary>
		///
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			if (IsDisposed)
				return;

			if (disposing)
			{
			}

			base.Dispose(disposing);

			m_isDisposed = true;
		}

		/// <summary>
		/// Insert text.  Assumes that if text is selected it is meant to be replaced.
		/// </summary>
		/// <param name="text">Text to insert</param>
		protected void insertText(string text)
		{
			insertText(text, true);
		}

		/// <summary>
		/// Insert text.  Assumes that if selected text is not replaced, the new text should be inserted at the left
		/// of the selection.
		/// </summary>
		/// <param name="text">Text to insert</param>
		/// <param name="replaceSelection">Determines if selected text should be replaced or not</param>
		protected void insertText(string text, bool replaceSelection)
		{
			insertText(text, replaceSelection, false);
		}

		/// <summary>
		/// Insert text.
		/// </summary>
		/// <param name="text">Text to insert</param>
		/// <param name="replaceSelection">Determines if selected text should be replaced or not</param>
		/// <param name="insertAtRight">If true and replaceSelection is false, the inserted text will be inserted at the right
		/// boundary of the selection instead of the left boundary.</param>
		protected void insertText(string text, bool replaceSelection, bool insertAtRight)
		{
			int selLen = m_textbox.SelectionLength;
			int selStart = m_textbox.SelectionStart;
			int newSelStart = selStart + text.Length;
			int newSelLength = 0;

			if (replaceSelection)
			{
				if (selLen > 0)
					m_textbox.Text = m_textbox.Text.Remove(selStart, selLen);

				m_textbox.Text = m_textbox.Text.Insert(selStart, text);
			}
			else
			{
				if (insertAtRight)
				{
					m_textbox.Text = m_textbox.Text.Insert(selStart + selLen, text);
					selStart += selLen;
				}
				else
				{
					m_textbox.Text = m_textbox.Text.Insert(selStart, text);
					m_textbox.Select(selStart + text.Length, 0);
				}
			}

			m_textbox.Focus();
			// Do select AFTER focus, otherwise, focus changes selection to whole box.
			m_textbox.Select(newSelStart, newSelLength);
			m_textbox.Refresh();
		}

		/// <summary>
		/// Attempts to group text.  Defaults to not requiring any selected text
		/// </summary>
		/// <param name="leftText">Text to insert before the highlighted region</param>
		/// <param name="rightText">Text to insert after the highlighted region</param>
		protected void groupText(string leftText, string rightText)
		{
			groupText(leftText, rightText, false);
		}

		/// <summary>
		/// Attempts to group text.
		/// </summary>
		/// <param name="leftText">Text to insert before the highlighted region</param>
		/// <param name="rightText">Text to insert after the highlighted region</param>
		/// <param name="requireSel">Determines if a selection is required or not.  If a selection is required but none
		/// is present, a warning message will be shown and the text will not be altered.</param>
		protected void groupText(string leftText, string rightText, bool requireSel)
		{
			int selLen = m_textbox.SelectionLength;
			int selStart = m_textbox.SelectionStart;

			if (selLen > 0)
			{
				string newSel = leftText + m_textbox.Text.Substring(selStart, selLen) + rightText;
				m_textbox.Text = m_textbox.Text.Remove(selStart, selLen);
				m_textbox.Text = m_textbox.Text.Insert(selStart, newSel);
				m_textbox.Focus();
				m_textbox.Select(selStart + selLen + leftText.Length, 0);
			}
			else
			{
				if (requireSel)
				{
					MessageBox.Show(FwCoreDlgs.ksNeedTextSelection, FwCoreDlgs.ksError);
					return;
				}

				m_textbox.Text = m_textbox.Text.Insert(selStart, leftText + rightText);
				m_textbox.Focus();
				m_textbox.Select(selStart + leftText.Length, 0);
			}

			m_textbox.Refresh();
		}
	}
}
