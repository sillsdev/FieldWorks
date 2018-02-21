// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.Windows.Forms;

namespace SIL.FieldWorks.Common.Widgets
{
	/// <summary>
	/// We need to subclass TreeView in order to override IsInputChar(), otherwise
	/// TreeView will not try to handle TAB keys (cf. LT-2190).
	/// </summary>
	internal class FwTreeView : TreeView
	{
		/// <summary />
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + ". ******");
			base.Dispose(disposing);
		}

		/// <summary>
		/// We need to be able to handle the TAB key.
		/// Requires IsInputKey() == true.
		/// </summary>
		protected override bool IsInputChar(char charCode)
		{
			return charCode == '\t' || base.IsInputChar(charCode);
		}

		/// <summary>
		/// We need to be able to handle the TAB key. IsInputKey() must be true
		/// for IsInputChar() to be called.
		/// </summary>
		protected override bool IsInputKey(Keys keyData)
		{
			if (keyData == Keys.Tab || keyData == (Keys.Tab | Keys.Shift))
			{
				return true;
			}
			return base.IsInputKey(keyData);
		}

		protected override void WndProc(ref Message m)
		{
			// don't try to handle WM_CHAR in TreeView
			// it causes an annoying beep LT-16007
			const int wmCharMsg = 258;
			if (m.Msg == wmCharMsg)
			{
				return;
			}
			base.WndProc(ref m);
		}
	}
}