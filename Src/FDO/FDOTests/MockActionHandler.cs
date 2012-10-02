using System;
using System.Collections.Generic;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;
using NUnit.Framework;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// <summary>
	/// This is a mock action handler designed to be inserted into a WriteProtectedDataAccess.
	/// It verifies
	/// (a) No updates are done before the first BeginUndoTask
	/// (b) No updates are done after the last EndUndoTask
	/// (c) While there may be nested BeginUndoTask/EndUndoTask, there is only one overall task.
	/// (d) No methods are called on the action handler except BeginUndoTask, EndUndoTask,
	/// and AddAction.
	/// It also saves any undo actions added before the first update method called on the cache
	/// and after the last, and the outermost BeginUndoTask strings.
	/// </summary>
	public class MockActionHandler : IActionHandler
	{
		List<IUndoAction> m_priorActions = new List<IUndoAction>();
		List<IUndoAction> m_followingActions = new List<IUndoAction>();
		WriteProtectedDataAccess m_sda;
		string m_UndoText;
		string m_RedoText;
		int m_depth;
		bool m_fGotFirstUpate = false; // set true when we get our first update through the cache.


		/// <summary>
		/// Make one.
		/// </summary>
		/// <param name="sda"></param>
		public MockActionHandler(WriteProtectedDataAccess sda)
		{
			m_sda = sda;
			m_sda.UpdatePerformed += new EventHandler(m_sda_UpdatePerformed);
		}

		void m_sda_UpdatePerformed(object sender, EventArgs e)
		{
			m_fGotFirstUpate = true;
			m_followingActions.Clear();
		}

		/// <summary>
		/// A common example of extra actions is surrounding ExtraPropChanged actions.
		/// Check that one occurred as expected.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ihvo"></param>
		/// <param name="chvoIns"></param>
		/// <param name="chvoDel"></param>
		public void VerifyExtraPropChanged(int hvo, int tag, int ihvo, int chvoIns, int chvoDel)
		{
			Assert.IsTrue(VerifyPropChanged(m_priorActions, hvo, tag, ihvo, chvoIns, chvoDel, false),
				"missing expected extra Undo prop changed");
			Assert.IsTrue(VerifyPropChanged(m_followingActions, hvo, tag, ihvo, chvoIns, chvoDel, true),
				"missing expected extra Redo prop changed");
		}

		bool VerifyPropChanged(List<IUndoAction> actions, int hvo, int tag, int ihvo, int chvoIns, int chvoDel, bool fForRedo)
		{
			foreach (IUndoAction action in actions)
			{
				ExtraPropChangedAction action1 = action as ExtraPropChangedAction;
				if (action1 != null && action1.SameInfo(hvo, tag, ihvo, chvoIns, chvoDel, fForRedo))
					return true;
			}
			return false;
		}

		/// <summary>
		/// Verify the expected outer undo/redo text.
		/// </summary>
		/// <param name="undoText"></param>
		/// <param name="redoText"></param>
		/// <param name="message"></param>
		public void VerifyUndoRedoText(string undoText, string redoText, string message)
		{
			Assert.AreEqual(undoText, m_UndoText, message);
			Assert.AreEqual(redoText, m_RedoText, message);
		}

		#region IActionHandler Members

		/// <summary>
		/// Record the action in the appropriate place.
		/// </summary>
		/// <param name="action"></param>
		public void AddAction(IUndoAction action)
		{
			if (m_fGotFirstUpate)
				m_followingActions.Add(action);
			else
				m_priorActions.Add(action);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="bstrUndo"></param>
		/// <param name="bstrRedo"></param>
		public void BeginUndoTask(string bstrUndo, string bstrRedo)
		{
			Assert.IsFalse(m_depth == 0 && m_UndoText != null, "Second outer undo task not expected");
			if (m_depth == 0)
			{
				m_UndoText = bstrUndo;
				m_RedoText = bstrRedo;
				m_sda.AllowWrites = true;
			}
			m_depth++;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="bstrUndo"></param>
		/// <param name="bstrRedo"></param>
		public void BreakUndoTask(string bstrUndo, string bstrRedo)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public bool CanRedo()
		{
			throw new Exception("The method or operation is not implemented.");
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public bool CanUndo()
		{
			throw new Exception("The method or operation is not implemented.");
		}

		/// <summary>
		///
		/// </summary>
		public void Close()
		{
			throw new Exception("The method or operation is not implemented.");
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hMark"></param>
		/// <param name="bstrUndo"></param>
		/// <param name="bstrRedo"></param>
		public void CollapseToMark(int hMark, string bstrUndo, string bstrRedo)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		/// <summary>
		///
		/// </summary>
		public void Commit()
		{
			throw new Exception("The method or operation is not implemented.");
		}

		/// <summary>
		///
		/// </summary>
		public void ContinueUndoTask()
		{
			throw new Exception("The method or operation is not implemented.");
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="fCreateMark"></param>
		public void CreateMarkIfNeeded(bool fCreateMark)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		/// <summary>
		///
		/// </summary>
		public int CurrentDepth
		{
			get { return m_depth; }
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hMark"></param>
		public void DiscardToMark(int hMark)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		/// <summary>
		///
		/// </summary>
		public void EndOuterUndoTask()
		{
			throw new Exception("The method or operation is not implemented.");
		}

		/// <summary>
		///
		/// </summary>
		public void EndUndoTask()
		{
			m_depth--;
			Assert.GreaterOrEqual(m_depth, 0, "Too many EndUndoTasks!");
			if (m_depth == 0)
				m_sda.AllowWrites = false; // no more after main transactions.
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public string GetRedoText()
		{
			throw new Exception("The method or operation is not implemented.");
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="iAct"></param>
		/// <returns></returns>
		public string GetRedoTextN(int iAct)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public string GetUndoText()
		{
			throw new Exception("The method or operation is not implemented.");
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="iAct"></param>
		/// <returns></returns>
		public string GetUndoTextN(int iAct)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		/// <summary>
		///
		/// </summary>
		public bool IsUndoOrRedoInProgress
		{
			get { throw new Exception("The method or operation is not implemented."); }
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public int Mark()
		{
			throw new Exception("The method or operation is not implemented.");
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public UndoResult Redo()
		{
			throw new Exception("The method or operation is not implemented.");
		}

		/// <summary>
		///
		/// </summary>
		public int RedoableSequenceCount
		{
			get { throw new Exception("The method or operation is not implemented."); }
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="nDepth"></param>
		public void Rollback(int nDepth)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="bstrUndo"></param>
		/// <param name="bstrRedo"></param>
		/// <param name="_uact"></param>
		public void StartSeq(string bstrUndo, string bstrRedo, IUndoAction _uact)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		/// <summary>
		///
		/// </summary>
		public int TopMarkHandle
		{
			get { throw new Exception("The method or operation is not implemented."); }
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public UndoResult Undo()
		{
			throw new Exception("The method or operation is not implemented.");
		}

		/// <summary>
		///
		/// </summary>
		public IUndoGrouper UndoGrouper
		{
			get
			{
				throw new Exception("The method or operation is not implemented.");
			}
			set
			{
				throw new Exception("The method or operation is not implemented.");
			}
		}

		/// <summary>
		///
		/// </summary>
		public int UndoableActionCount
		{
			get { throw new Exception("The method or operation is not implemented."); }
		}

		/// <summary>
		/// Somewhat spurious, but UndoRedoHelper wants to get it (though it does not appear to use it)
		/// </summary>
		public int UndoableSequenceCount
		{
			get { return 0; }
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="fUndo"></param>
		/// <returns></returns>
		public bool get_TasksSinceMark(bool fUndo)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		#endregion
	}
}
