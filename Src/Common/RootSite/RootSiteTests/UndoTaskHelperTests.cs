// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: UndoTaskHelperTests.cs
// Responsibility: TE Team
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Common.RootSites
{
	#region DummyUndoTaskHelper class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DummyUndoTaskHelper: UndoTaskHelper
	{
		public static bool m_fUndoTaskEnded = false;
		public static bool m_fRollbackCalled = false;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Start the undo task
		/// </summary>
		/// <param name="rootSite">The view</param>
		/// <param name="stid">String resource id used for Undo/Redo labels</param>
		/// -----------------------------------------------------------------------------------
		public DummyUndoTaskHelper(RootSite rootSite, string stid)
			: base(rootSite, stid)
		{
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Start the undo task
		/// </summary>
		/// <param name="rootSite">The view</param>
		/// <param name="stid">String resource id used for Undo/Redo labels</param>
		/// <param name="fSavePoint"><c>true</c> to set a save point</param>
		/// -----------------------------------------------------------------------------------
		public DummyUndoTaskHelper(RootSite rootSite, string stid, bool fSavePoint)
			: base(rootSite, stid, fSavePoint)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// End the undo task and call commit on root site
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void EndTheUndoTask()
		{
			base.EndTheUndoTask();
			m_fUndoTaskEnded = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Rollback to the save point
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void Rollback()
		{
			base.Rollback();
			m_fRollbackCalled = true;
		}
	}
	#endregion

	#region UndoSelectionAction
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Undo action for setting the selection
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyUndoAction: UndoActionBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DummyUndoAction()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns <c>true</c> so that the ActionHandler will think something usefull is
		/// going on.
		/// </summary>
		/// <returns>always <c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		public override bool IsDataChange()
		{
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reverses (or "un-does") an action.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool Undo(bool fRefreshPending)
		{
			// Do nothing
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Re-applies (or "re-does") an action.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool Redo(bool fRefreshPending)
		{
			// Do nothing
			return true;
		}
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests the UndoTaskHelperTests class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class UndoTaskHelperTests : RootsiteBasicViewTestsBaseRealCache
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize test
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void Initialize()
		{
			CheckDisposed();
			base.Initialize();

			DummyUndoTaskHelper.m_fUndoTaskEnded = false;
			DummyUndoTaskHelper.m_fRollbackCalled = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that UndoTaskHelper begins and ends a undo task
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BeginAndEndUndoTask()
		{
			CheckDisposed();

			ShowForm();
			// we need a selection
			m_basicView.RootBox.MakeSimpleSel(true, true, false, true);

			// this should begin an outer undo task, so we will have only one undoable task!
			using(new UndoTaskHelper(m_basicView, "kstidUndoStyleChanges"))
			{
				m_fdoCache.BeginUndoTask("", "");
				StStyle style = new StStyle();
				m_fdoCache.LangProject.StylesOC.Add(style);
				m_fdoCache.EndUndoTask();

				m_fdoCache.BeginUndoTask("", "");
				style = new StStyle();
				m_fdoCache.LangProject.StylesOC.Add(style);
				m_fdoCache.EndUndoTask();
			}
			int nUndoTasks = 0;
			while(m_fdoCache.Undo())
			{
				nUndoTasks++;
			}
			Assert.AreEqual(1, nUndoTasks);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that Dispose gets called after we get an unhandled exception
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EndUndoCalledAfterUnhandledException()
		{
			CheckDisposed();

			ShowForm();
			// we need a selection
			m_basicView.RootBox.MakeSimpleSel(true, true, false, true);

			try
			{
				using(DummyUndoTaskHelper helper = new DummyUndoTaskHelper(m_basicView,
					"kstidUndoStyleChanges"))
				{
					throw new Exception(); // this throws us out of the using statement
				}
			}
			catch
			{
				// just catch the exception so that we can test if undo task was ended
			}

			Assert.IsTrue(DummyUndoTaskHelper.m_fUndoTaskEnded);
			Assert.IsFalse(DummyUndoTaskHelper.m_fRollbackCalled);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that Dispose gets not called after handled exception
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EndUndoNotCalledAfterHandledException()
		{
			CheckDisposed();

			ShowForm();
			// we need a selection
			m_basicView.RootBox.MakeSimpleSel(true, true, false, true);

			try
			{
				using(DummyUndoTaskHelper helper = new DummyUndoTaskHelper(m_basicView,
						  "kstidUndoStyleChanges", true))
				{
					try
					{
						throw new Exception();
					}
					catch(Exception)
					{
						helper.EndUndoTask = false;
						throw; // this throws us out of the using statement
					}
				}
			}
			catch
			{
				// just catch the exception so that we can test if undo task was ended
			}

			Assert.IsFalse(DummyUndoTaskHelper.m_fUndoTaskEnded);
			Assert.IsTrue(DummyUndoTaskHelper.m_fRollbackCalled);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to supress subtasks by setting the action handler to null inside of
		/// an outer undo task
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SuppressSubTasks()
		{
			CheckDisposed();

			ShowForm();
			// we need a selection
			m_basicView.RootBox.MakeSimpleSel(true, true, false, true);

			using(UndoTaskHelper helper = new UndoTaskHelper(m_basicView,
					  "kstidUndoStyleChanges"))
			{
				CheckActionHandlerIsNull(false);
				using (new SuppressSubTasks(m_fdoCache))
					CheckActionHandlerIsNull(true);
			}
			CheckActionHandlerIsNull(false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the action handler and check to see if it is null or not.
		/// </summary>
		/// <param name="checkForNull"></param>
		/// ------------------------------------------------------------------------------------
		private void CheckActionHandlerIsNull(bool checkForNull)
		{
			IActionHandler ah = m_basicView.RootBox.DataAccess.GetActionHandler();
			try
			{
				if (checkForNull)
					Assert.IsNull(ah);
				else
					Assert.IsNotNull(ah);
			}
			finally
			{
				if (ah != null)
					Marshal.ReleaseComObject(ah);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that a save point gets set and rolled back after exception
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AutomaticRollbackAfterException()
		{
			CheckDisposed();

			ShowForm();
			// we need a selection
			m_basicView.RootBox.MakeSimpleSel(true, true, false, true);

			try
			{
				using(DummyUndoTaskHelper helper = new DummyUndoTaskHelper(m_basicView,
							"kstidUndoStyleChanges", true))
				{
					try
					{
						throw new Exception();
					}
					catch(Exception)
					{
						helper.EndUndoTask = false;
						throw; // this throws us out of the using statement
					}
				}
			}
			catch
			{
				// just catch the exception so that we can test if undo task was ended
			}

			Assert.IsFalse(DummyUndoTaskHelper.m_fUndoTaskEnded);
			Assert.IsTrue(DummyUndoTaskHelper.m_fRollbackCalled);

			// This re-runs the test to make sure that the undo task was ended properly
			DummyUndoTaskHelper.m_fUndoTaskEnded = false;
			DummyUndoTaskHelper.m_fRollbackCalled = false;
			try
			{
				using(DummyUndoTaskHelper helper = new DummyUndoTaskHelper(m_basicView,
							"kstidUndoStyleChanges", true))
				{
					try
					{
						throw new Exception();
					}
					catch(Exception)
					{
						helper.EndUndoTask = false;
						throw; // this throws us out of the using statement
					}
				}
			}
			catch
			{
				// just catch the exception so that we can test if undo task was ended
			}
			Assert.IsFalse(DummyUndoTaskHelper.m_fUndoTaskEnded);
			Assert.IsTrue(DummyUndoTaskHelper.m_fRollbackCalled);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that a save point gets set and not rolled back when no exception happens
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NoRollbackAfterNoException()
		{
			CheckDisposed();

			ShowForm();
			// we need a selection
			m_basicView.RootBox.MakeSimpleSel(true, true, false, true);

			using(DummyUndoTaskHelper helper = new DummyUndoTaskHelper(m_basicView,
						"kstidUndoStyleChanges"))
			{
			}

			Assert.IsTrue(DummyUndoTaskHelper.m_fUndoTaskEnded);
			Assert.IsFalse(DummyUndoTaskHelper.m_fRollbackCalled);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that there are no actions left after we rolled back
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NoActionsLeftAfterException()
		{
			CheckDisposed();

			ShowForm();
			// we need a selection
			m_basicView.RootBox.MakeSimpleSel(true, true, false, true);

			IActionHandler acth = m_basicView.Cache.ActionHandlerAccessor;
			Assert.AreEqual(0, acth.UndoableActionCount, "Undoable actions after start");

			int undoableActionCountInUndoTask = 0;
			try
			{
				using(DummyUndoTaskHelper helper = new DummyUndoTaskHelper(m_basicView,
						  "kstidUndoStyleChanges"))
				{
					try
					{
						undoableActionCountInUndoTask = acth.UndoableActionCount;
						throw new Exception();
					}
					catch(Exception)
					{
						helper.EndUndoTask = false;
						throw; // this throws us out of the using statement
					}
				}
			}
			catch
			{
				// just catch the exception so that we can test if undo task was ended
			}
			Assert.AreEqual(1, undoableActionCountInUndoTask,
				"No undoable actions in undo task");
			Assert.AreEqual(0, acth.UndoableActionCount,
				"Undoable actions after getting exception");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that UndoSelectionActions get created
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreatedUndoSelectionActions()
		{
			CheckDisposed();

			ShowForm();
			// we need a selection - set to start
			m_basicView.RootBox.MakeSimpleSel(true, true, false, true);

			IActionHandler acth = m_basicView.Cache.ActionHandlerAccessor;
			Assert.AreEqual(0, acth.UndoableActionCount, "Undoable actions at start");

			using(UndoTaskHelper helper = new UndoTaskHelper(m_basicView,
					  "kstidUndoStyleChanges"))
			{
				Assert.AreEqual(1, acth.UndoableActionCount,
					"Wrong number of undoable actions after beginning task");
				acth.AddAction(new DummyUndoAction());
				Assert.AreEqual(2, acth.UndoableActionCount,
					"Wrong number of undoable actions after adding a task");
			}
			Assert.AreEqual(3, acth.UndoableActionCount,
				"Wrong number of undoable actions after ending task");
		}
	}
}
