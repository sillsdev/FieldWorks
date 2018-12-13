// Copyright (c) 2013-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.KernelInterfaces;

namespace SIL.FieldWorks.Common.RootSites.SimpleRootSiteTests
{
	public class SimpleActionHandler: IActionHandler
	{
		private List<List<IUndoAction>> m_UndoTasks = new List<List<IUndoAction>>();
		private List<IUndoAction> m_OpenTask;

		public IVwRootBox RootBox { get; set; }

		#region IActionHandler implementation

		public void BeginUndoTask(string bstrUndo, string bstrRedo)
		{
			m_OpenTask = new List<IUndoAction>();
		}

		public void EndUndoTask()
		{
			m_UndoTasks.Add(m_OpenTask);
			m_OpenTask = null;
		}

		public void ContinueUndoTask()
		{
			if (m_OpenTask == null && m_UndoTasks.Count > 0)
			{
				m_OpenTask = m_UndoTasks[m_UndoTasks.Count - 1];
				m_UndoTasks.Remove(m_OpenTask);
			}
			if (m_OpenTask == null)
			{
				BeginUndoTask(null, null);
			}
		}

		public void EndOuterUndoTask()
		{
			throw new NotSupportedException();
		}

		public void BreakUndoTask(string bstrUndo, string bstrRedo)
		{
			EndUndoTask();
			BeginUndoTask(bstrUndo, bstrRedo);
		}

		public void BeginNonUndoableTask()
		{
			throw new NotSupportedException();
		}

		public void EndNonUndoableTask()
		{
			throw new NotSupportedException();
		}

		public void CreateMarkIfNeeded(bool fCreateMark)
		{
			throw new NotSupportedException();
		}

		public void StartSeq(string bstrUndo, string bstrRedo, IUndoAction _uact)
		{
			throw new NotSupportedException();
		}

		public void AddAction(IUndoAction act)
		{
			if (m_OpenTask == null)
			{
				return;
			}
			var cacheAction = act as CacheUndoAction;
			if (cacheAction != null)
			{
				cacheAction.RootBox = RootBox;
			}
			m_OpenTask.Add(act);
		}

		public string GetUndoText()
		{
			throw new NotSupportedException();
		}

		public string GetUndoTextN(int iAct)
		{
			throw new NotSupportedException();
		}

		public string GetRedoText()
		{
			throw new NotSupportedException();
		}

		public string GetRedoTextN(int iAct)
		{
			throw new NotSupportedException();
		}

		public bool CanUndo()
		{
			return m_UndoTasks.Count > 0 || (m_OpenTask != null && m_OpenTask.Count > 0);
		}

		public bool CanRedo()
		{
			return false;
		}

		public UndoResult Undo()
		{
			if (m_UndoTasks.Count <= 0)
			{
				throw new ApplicationException("No undo tasks");
			}
			var actions = m_UndoTasks[m_UndoTasks.Count - 1];
			m_UndoTasks.Remove(actions);
			var ok = true;
			foreach (var action in actions)
			{
				ok = action.Undo();
			}
			return ok ? UndoResult.kuresSuccess : UndoResult.kuresFailed;
		}

		public UndoResult Redo()
		{
			throw new NotSupportedException();
		}

		public void Rollback(int nDepth)
		{
			if (m_OpenTask == null)
			{
				throw new ApplicationException("No open undo task");
			}
			foreach (var action in m_OpenTask)
			{
				action.Undo();
			}
			m_OpenTask = null;
		}

		public void Commit()
		{
			throw new NotSupportedException();
		}

		public void Close()
		{
			m_UndoTasks.Clear();
		}

		public int Mark()
		{
			throw new NotSupportedException();
		}

		public bool CollapseToMark(int hMark, string bstrUndo, string bstrRedo)
		{
			throw new NotSupportedException();
		}

		public void DiscardToMark(int hMark)
		{
			throw new NotSupportedException();
		}

		public bool get_TasksSinceMark(bool fUndo)
		{
			return true;
		}

		public int CurrentDepth => m_OpenTask == null ? 0 : 1;

		public int TopMarkHandle
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		public int UndoableActionCount => m_UndoTasks[m_UndoTasks.Count - 1].Count;

		public int UndoableSequenceCount
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		public int RedoableSequenceCount
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		public bool IsUndoOrRedoInProgress
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		public bool SuppressSelections
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		#endregion
	}
}