// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: SelPositionInfo.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Diagnostics;
using System.Drawing;
using SIL.Utils;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Common.RootSites
{
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// Class to represent the position of a selection at the top left of a root site,
	/// so it can be restored, typically after some operation in the other pane.
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	public class SelPositionInfo
	{
		#region Member variables
		private SimpleRootSite m_rootSite;	// The root site we are working with.
		private Rectangle m_rcPrimaryOld;	// The position of the top-left selection to be restored.
		private IVwSelection m_sel;		// the selection.
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Construct and save info about selection at top left of rootSite.
		/// </summary>
		/// <param name="rootSite">rootSite</param>
		/// ------------------------------------------------------------------------------------
		public SelPositionInfo(SimpleRootSite rootSite)
		{
			m_rootSite = rootSite;
			if (rootSite == null)
				return;

			int xdLeft = m_rootSite.ClientRectangle.X;
			int ydTop = m_rootSite.ClientRectangle.Y;

			Rectangle rcSrcRoot, rcDstRoot;
			m_rootSite.GetCoordRects(out rcSrcRoot, out rcDstRoot);
			m_sel = m_rootSite.RootBox.MakeSelAt(xdLeft + 1,ydTop + 1, rcSrcRoot, rcDstRoot, false);
			if (m_sel != null)
			{
				bool fEndBeforeAnchor;
				m_rootSite.SelectionRectangle(m_sel, out m_rcPrimaryOld, out fEndBeforeAnchor);
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Scroll rootSite so the selection we made in the constructor is at the same position it was
		/// when we were constructed, if possible.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public void Restore()
		{
			if (m_sel != null)
			{
				// We were able to get a selection...before we use it, make sure it wasn't
				// spoiled by the changes we made.
				if (m_sel.IsValid)
				{
					Rectangle rcPrimary;
					bool fEndBeforeAnchor;
					m_rootSite.SelectionRectangle(m_sel, out rcPrimary, out fEndBeforeAnchor);
					if (rcPrimary.Top != m_rcPrimaryOld.Top)
						m_rootSite.ScrollDown(rcPrimary.Top - m_rcPrimaryOld.Top);
				}
			}
		}
	}
}
