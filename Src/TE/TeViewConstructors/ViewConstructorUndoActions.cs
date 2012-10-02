// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ViewConstructorUndoActions.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ReplacePromptUndoAction : UndoActionBase
	{
		private int m_paraHvo;
		private int m_updatePropHvo;
		private Set<int> m_updatedPrompts;
		private FdoCache m_cache;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new instance of the <see cref="ReplacePromptUndoAction"/> object.
		/// </summary>
		/// <param name="updatePropHvo"></param>
		/// <param name="cache"></param>
		/// <param name="updatedPrompts"></param>
		/// ------------------------------------------------------------------------------------
		public ReplacePromptUndoAction(int updatePropHvo, FdoCache cache, Set<int> updatedPrompts)
		{
			m_updatePropHvo = updatePropHvo;
			m_cache = cache;
			m_updatedPrompts = updatedPrompts;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the paragraph hvo.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int ParaHvo
		{
			set { m_paraHvo = value; }
		}

		#region Overrides of UndoActionBase
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reverses (or "un-does") an action.
		/// </summary>
		/// <param name="fRefreshPending"></param>
		/// ------------------------------------------------------------------------------------
		public override bool Undo(bool fRefreshPending)
		{
			Debug.Assert(m_paraHvo != 0);

			m_updatedPrompts.Remove(m_updatePropHvo);
			// Do a fake propchange to update the prompt
			m_cache.PropChanged(null, PropChangeType.kpctNotifyAll, m_paraHvo,
				(int)StPara.StParaTags.kflidStyleRules, 0, 1, 1);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Re-applies (or "re-does") an action.
		/// </summary>
		/// <param name="fRefreshPending"></param>
		/// ------------------------------------------------------------------------------------
		public override bool Redo(bool fRefreshPending)
		{
			return true; // always works
		}
		#endregion

	}
}
