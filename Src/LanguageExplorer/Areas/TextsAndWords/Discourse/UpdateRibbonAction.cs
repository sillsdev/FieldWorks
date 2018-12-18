// Copyright (c) 2008-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Areas.TextsAndWords.Discourse
{
	/// <summary>
	/// Update the ribbon during Undo or Redo of annotating something.
	/// Note that we create TWO of these, one that is the first action in the group, and one that is the last.
	/// The first is for Undo, and updates the ribbon to the appropriate state for when the action is undone.
	/// (It needs to be first so it will be the last thing undone.)
	/// The last is for Redo, and updates the ribbon after the task is redone (needs to be last so it is the
	/// last thing redone).
	/// </summary>
	internal class UpdateRibbonAction : IUndoAction
	{
		private readonly ConstituentChartLogic m_logic;
		private readonly bool m_fForRedo;

		public UpdateRibbonAction(ConstituentChartLogic logic, bool fForRedo)
		{
			m_logic = logic;
			m_fForRedo = fForRedo;
		}

		public void UpdateUnchartedWordforms()
		{
			m_logic.Ribbon.CacheRibbonItems(m_logic.NextUnchartedInput(ConstituentChartLogic.kMaxRibbonContext).ToList()); // now handles PropChanged???
			m_logic.Ribbon.SelectFirstOccurence();
		}

		#region IUndoAction Members

		public void Commit()
		{
		}

		public bool IsDataChange => false;

		public bool IsRedoable => true;

		public bool Redo()
		{
			if (m_fForRedo)
			{
				UpdateUnchartedWordforms();
			}
			return true;
		}

		public bool SuppressNotification
		{
			set { }
		}

		public bool Undo()
		{
			if (!m_fForRedo)
			{
				UpdateUnchartedWordforms();
			}
			return true;
		}

		#endregion
	}
}