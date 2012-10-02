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
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.FieldWorks.FDO.Infrastructure;

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
		private IVwRootBox m_rootbox;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new instance of the <see cref="ReplacePromptUndoAction"/> object.
		/// </summary>
		/// <param name="updatePropHvo"></param>
		/// <param name="rootbox"></param>
		/// <param name="updatedPrompts"></param>
		/// ------------------------------------------------------------------------------------
		public ReplacePromptUndoAction(int updatePropHvo, IVwRootBox rootbox, Set<int> updatedPrompts)
		{
			m_updatePropHvo = updatePropHvo;
			m_rootbox = rootbox;
			m_updatedPrompts = updatedPrompts;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the paragraph hvo.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int ParaHvo
		{
			get { return m_paraHvo; }
			set { m_paraHvo = value; }
		}

		#region Overrides of UndoActionBase
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Can't redo this - prop changes won't work right and selection is lost.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool IsRedoable
		{
			get { return false; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Will be only action in a UOW, so need to say it changed data.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool IsDataChange
		{
			get { return true; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reverses (or "un-does") an action.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool Undo()
		{
			Debug.Assert(m_paraHvo != 0);

			m_updatedPrompts.Remove(m_updatePropHvo);
			// Do a fake propchange to update the prompt
			m_rootbox.PropChanged(m_paraHvo, StParaTags.kflidStyleRules, 0, 1, 1);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Re-applies (or "re-does") an action.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool Redo()
		{
			return true;
		}
		#endregion

	}
}
