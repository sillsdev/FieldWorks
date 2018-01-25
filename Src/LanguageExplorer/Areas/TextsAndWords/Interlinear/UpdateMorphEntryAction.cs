// Copyright (c) 2006-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary />
	internal class UpdateMorphEntryAction : UndoActionBase
	{
		private SandboxBase m_sandbox;
		private int m_hvoMorph;
		private ISilDataAccess m_sda;
		private int m_morphFormNew;
		private int m_morphFormOld;
		private readonly int[] m_tags = { SandboxBase.ktagSbMorphForm, SandboxBase.ktagSbMorphGloss, SandboxBase.ktagSbNamedObjGuess, SandboxBase.ktagSbMorphPos };
		private readonly int[] m_oldVals;
		private readonly int[] m_newVals;
		private int m_originalAnalysis;

		public UpdateMorphEntryAction(SandboxBase sandbox, int hvoMorph)
		{
			m_oldVals = new int[m_tags.Length];
			m_newVals = new int[m_tags.Length];
			m_sandbox = sandbox;
			m_sda = m_sandbox.Caches.DataAccess;
			m_originalAnalysis = m_sandbox.Analysis;
			m_hvoMorph = hvoMorph;
			for (var i = 0; i < m_tags.Length; i++)
			{
				m_oldVals[i] = m_sda.get_ObjectProp(m_hvoMorph, m_tags[i]);
			}
		}

		public void GetNewVals()
		{
			for (var i = 0; i < m_tags.Length; i++)
			{
				m_newVals[i] = m_sda.get_ObjectProp(m_hvoMorph, m_tags[i]);
			}
		}
		/// <summary>
		/// Reverses (or "undoes") an action.
		/// </summary>
		public override bool Undo()
		{
			SetVals(m_oldVals);
			return true;
		}

		private void SetVals(System.Collections.Generic.IReadOnlyList<int> vals)
		{
			// If things have moved on, don't mess with it. These changes are no longer relevant.
			if (m_sandbox.IsDisposed || m_sandbox.Analysis != m_originalAnalysis)
			{
				return;
			}
			for (var i = 0; i < m_tags.Length; i++)
			{
				if (m_oldVals[i] == m_newVals[i])
				{
					continue;
				}
				m_sda.SetObjProp(m_hvoMorph, m_tags[i], vals[i]);
				m_sda.PropChanged(m_sandbox.RootBox, (int)PropChangeType.kpctNotifyAll, m_hvoMorph, m_tags[i], 0, 1, 1);
			}
		}

		/// <summary>
		/// Reapplies (or "redoes") an action.
		/// </summary>
		public override bool Redo()
		{
			SetVals(m_newVals);
			return true;
		}

		/// <summary>
		/// This is rather dubious. This change is NOT in itself a change to FieldWorks data.
		/// However, currently (Mar 17 2011) the whole undo bundle is discarded if none of the
		/// changes is a data change, and we need it to be put into the stack long enough so
		/// that it can get merged with the change it modifies. If we allow no-data-change UOWs
		/// to get put in the stack normally, we can remove this override.
		/// </summary>
		public override bool IsDataChange => true;
	}
}