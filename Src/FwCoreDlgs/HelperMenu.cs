// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;

using SIL.Utils;
using SIL.FieldWorks.Common.Widgets;
using XCore;

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
		/// <param name="helpTopicProvider">usually IHelpTopicProvider.App</param>
		protected HelperMenu(FwTextBox textbox, IHelpTopicProvider helpTopicProvider)
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

		private bool m_isDisposed;

		/// <summary>
		///
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
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
		protected void InsertText(string text)
		{
			InsertText(text, true);
		}

		/// <summary>
		/// Insert text.  Assumes that if selected text is not replaced, the new text should be inserted at the left
		/// of the selection.
		/// </summary>
		/// <param name="text">Text to insert</param>
		/// <param name="replaceSelection">Determines if selected text should be replaced or not</param>
		protected void InsertText(string text, bool replaceSelection)
		{
			InsertText(text, replaceSelection, false);
		}

		/// <summary>
		/// Insert text.
		/// </summary>
		/// <param name="text">Text to insert</param>
		/// <param name="replaceSelection">Determines if selected text should be replaced or not</param>
		/// <param name="insertAtRight">If true and replaceSelection is false, the inserted text will be inserted at the right
		/// boundary of the selection instead of the left boundary.</param>
		protected void InsertText(string text, bool replaceSelection, bool insertAtRight)
		{
			int selLen = m_textbox.SelectionLength;
			int selStart = m_textbox.SelectionStart;
			int newSelStart = selStart + text.Length;
			const int newSelLength = 0;
			var bldr = m_textbox.Tss.GetBldr();

			if (replaceSelection)
			{
				if (selLen > 0)
					m_textbox.Text = m_textbox.Text.Remove(selStart, selLen);

				bldr.Replace(selStart, selStart, text, null);
				m_textbox.Tss = bldr.GetString();
			}
			else
			{
				if (insertAtRight)
				{
					bldr.Replace(selStart + selLen, selStart + selLen, text, null);
					m_textbox.Tss = bldr.GetString();
				}
				else
				{
					bldr.Replace(selStart, selStart, text, null);
					m_textbox.Tss = bldr.GetString();
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
		protected void GroupText(string leftText, string rightText)
		{
			GroupText(leftText, rightText, false);
		}

		/// <summary>
		/// Attempts to group text.
		/// </summary>
		/// <param name="leftText">Text to insert before the highlighted region</param>
		/// <param name="rightText">Text to insert after the highlighted region</param>
		/// <param name="requireSel">Determines if a selection is required or not.  If a selection is required but none
		/// is present, a warning message will be shown and the text will not be altered.</param>
		protected void GroupText(string leftText, string rightText, bool requireSel)
		{
			int selLen = m_textbox.SelectionLength;
			int selStart = m_textbox.SelectionStart;
			var bldr = m_textbox.Tss.GetBldr();

			if (selLen > 0)
			{
				bldr.Replace(selStart + selLen, selStart + selLen, rightText, null); // do this BEFORE inserting leftText and changing the position
				bldr.Replace(selStart, selStart, leftText, null);
				m_textbox.Tss = bldr.GetString();

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

				bldr.Replace(selStart, selStart, leftText + rightText, null);
				m_textbox.Tss = bldr.GetString();
				m_textbox.Focus();
				m_textbox.Select(selStart + leftText.Length, 0);
			}

			m_textbox.Refresh();
		}
	}
}