using System;
using System.Collections.Generic;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;

namespace SIL.FieldWorks.TE
{
	/// <summary>
	/// This class is responsible to save selection information before some operation
	/// (currently typically a KeyPress) which might result in an ordinary string property being
	/// replaced by a prompt. The replacement will typically destroy the selection.
	/// This code saves the information needed to restore a selection in the prompt
	/// after the operation.
	/// Typical usage:
	/// using new PromptSelectionRestorer(rootbox)
	///		base.OnKeyPress();
	/// </summary>
	public class PromptSelectionRestorer : IDisposable
	{
		int ihvoRoot;
		int cvsli;
		SelLevInfo[] rgvsli;
		private IVwRootBox m_rootbox;
		int cpropPrevious;
		ITsTextProps ttp;
		int ws;
		/// <summary>
		/// Make one.
		/// </summary>
		public PromptSelectionRestorer(IVwRootBox rootbox)
		{
			m_rootbox = rootbox;
			if (rootbox == null)
				return;
			// Save information about the old selection.
			IVwSelection oldsel = rootbox.Selection;
			// Dummy variables
			int tagTextProp;
			int ichAnchor;
			int ichEnd;
			int ihvoEnd;
			bool fAssocPrev;
			if (oldsel != null && oldsel.CLevels(false) > 0)
			{
				cvsli = oldsel.CLevels(false);
				cvsli--; // CLevels includes the string property itself, but AllTextSelInfo doesn't need it.
				rgvsli = SelLevInfo.AllTextSelInfo(oldsel, cvsli,
												   out ihvoRoot, out tagTextProp, out cpropPrevious, out ichAnchor, out ichEnd,
												   out ws, out fAssocPrev, out ihvoEnd, out ttp);
			}

		}

		#region IDisposable Members

		/// <summary>
		/// We consciously don't do the usual destructor stuff. There's nothing useful to do unless properly
		/// disposed.
		/// </summary>
		public void Dispose()
		{
			if (m_rootbox != null && m_rootbox.Selection == null && rgvsli != null)
			{
				// The likely reason for this is that the backspace deleted the last character in a prompt and we
				// re-inserted the prompt. Try to re-select the whole prompt. Don't worry if this fails.
				try
				{
					m_rootbox.MakeTextSelection(ihvoRoot, cvsli, rgvsli,
											  SimpleRootSite.kTagUserPrompt, cpropPrevious, 0, 0,
											  ws, false, -1, ttp, true);
				}
				catch (Exception)
				{
				}
			}
		}

		#endregion
	}
}
