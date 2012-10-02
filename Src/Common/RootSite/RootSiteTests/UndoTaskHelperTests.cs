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
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Infrastructure;

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
		public static bool m_fRollbackAction = true;
		public static bool m_fRollbackCalled = false;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Start the undo task
		/// </summary>
		/// <param name="actionHandler">The action handler.</param>
		/// <param name="rootSite">The view</param>
		/// <param name="stid">String resource id used for Undo/Redo labels</param>
		/// ------------------------------------------------------------------------------------
		public DummyUndoTaskHelper(IActionHandler actionHandler, RootSite rootSite, string stid)
			: base(actionHandler, rootSite, stid)
		{
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Start the undo task
		/// </summary>
		/// <param name="rootSite">The view</param>
		/// <param name="stid">String resource id used for Undo/Redo labels</param>
		/// -----------------------------------------------------------------------------------
		public DummyUndoTaskHelper(RootSite rootSite, string stid) : base(rootSite, stid)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// End the undo task and call commit on root site
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void EndUndoTask()
		{
			base.EndUndoTask();
			m_fRollbackAction = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Rollback to the save point
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void RollBackChanges()
		{
			base.RollBackChanges();
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
		/// Returns <c>true</c> so that the ActionHandler will think something usefull is
		/// going on.
		/// </summary>
		/// <returns>always <c>true</c></returns>
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
			// Do nothing
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Re-applies (or "re-does") an action.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool Redo()
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
	public class UndoTaskHelperTests : RootsiteDummyViewTestsBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize test
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();

			DummyUndoTaskHelper.m_fRollbackAction = true;
			DummyUndoTaskHelper.m_fRollbackCalled = false;

			m_actionHandler.EndUndoTask();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that UndoTaskHelper begins and ends a undo task
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BeginAndEndUndoTask()
		{
			ShowForm();
			// we need a selection
			m_basicView.RootBox.MakeSimpleSel(true, true, false, true);

			// this should begin an outer undo task, so we will have only one undoable task!
			using(UndoTaskHelper helper = new UndoTaskHelper(m_actionHandler, m_basicView, "kstidUndoStyleChanges"))
			{
				IScrBook book = Cache.ServiceLocator.GetInstance<IScrBookFactory>().Create(6);
				Cache.LanguageProject.TranslatedScriptureOA.ScriptureBooksOS.Add(book);

				book = Cache.ServiceLocator.GetInstance<IScrBookFactory>().Create(7);
				Cache.LanguageProject.TranslatedScriptureOA.ScriptureBooksOS.Add(book);
				helper.RollBack = false;
			}
			int nUndoTasks = 0;
			while (m_actionHandler.CanUndo())
			{
				Assert.AreEqual(UndoResult.kuresSuccess, m_actionHandler.Undo());
				nUndoTasks++;
			}
			Assert.AreEqual(1, nUndoTasks);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that Dispose gets called after we get an unhandled exception and that the
		/// action is rolled back.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EndUndoCalledAfterUnhandledException()
		{
			ShowForm();
			// we need a selection
			m_basicView.RootBox.MakeSimpleSel(true, true, false, true);

			try
			{
				using(DummyUndoTaskHelper helper = new DummyUndoTaskHelper(m_actionHandler,
					m_basicView, "kstidUndoStyleChanges"))
				{
					throw new Exception(); // this throws us out of the using statement
				}
			}
			catch
			{
				// just catch the exception so that we can test if undo task was ended
			}

			Assert.IsTrue(DummyUndoTaskHelper.m_fRollbackAction);
			Assert.IsTrue(DummyUndoTaskHelper.m_fRollbackCalled);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that Dispose gets not called after handled exception
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EndUndoNotCalledAfterHandledException()
		{
			ShowForm();
			// we need a selection
			m_basicView.RootBox.MakeSimpleSel(true, true, false, true);

			try
			{
				using (DummyUndoTaskHelper helper = new DummyUndoTaskHelper(m_basicView,
					"kstidUndoStyleChanges"))
				{
					throw new Exception();
				}
			}
			catch
			{
				// just catch the exception so that we can test if undo task was ended
			}

			Assert.IsTrue(DummyUndoTaskHelper.m_fRollbackAction);
			Assert.IsTrue(DummyUndoTaskHelper.m_fRollbackCalled);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that a save point gets set and rolled back after exception
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AutomaticRollbackAfterException()
		{
			ShowForm();
			// we need a selection
			m_basicView.RootBox.MakeSimpleSel(true, true, false, true);

			try
			{
				using (DummyUndoTaskHelper helper = new DummyUndoTaskHelper(m_basicView,
					"kstidUndoStyleChanges"))
				{
					throw new Exception();
				}
			}
			catch
			{
				// just catch the exception so that we can test if undo task was ended
			}

			Assert.IsTrue(DummyUndoTaskHelper.m_fRollbackAction);
			Assert.IsTrue(DummyUndoTaskHelper.m_fRollbackCalled);

			// This re-runs the test to make sure that the undo task was ended properly
			DummyUndoTaskHelper.m_fRollbackAction = true;
			DummyUndoTaskHelper.m_fRollbackCalled = false;
			try
			{
				using(DummyUndoTaskHelper helper = new DummyUndoTaskHelper(m_basicView,
					"kstidUndoStyleChanges"))
				{
					throw new Exception();
				}
			}
			catch
			{
				// just catch the exception so that we can test if undo task was ended
			}
			Assert.IsTrue(DummyUndoTaskHelper.m_fRollbackAction);
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
			ShowForm();
			// we need a selection
			m_basicView.RootBox.MakeSimpleSel(true, true, false, true);

			using(DummyUndoTaskHelper helper = new DummyUndoTaskHelper(m_actionHandler,
				m_basicView, "kstidUndoStyleChanges"))
			{
				// we have to explicitly indicate that the action not be rolled back at the end
				// of the statements
				helper.RollBack = false;
			}

			Assert.IsFalse(DummyUndoTaskHelper.m_fRollbackAction);
			Assert.IsFalse(DummyUndoTaskHelper.m_fRollbackCalled);
		}
	}
}
