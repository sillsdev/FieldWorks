// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: UnitOfWorkHelper.cs
// Responsibility: FW Team
// ---------------------------------------------------------------------------------------------
using System;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.Infrastructure
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Base class for UnitOfWorkHelpers
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public abstract class UnitOfWorkHelper : IFWDisposable
	{
		/// <summary>The undo stack that is handling the actions that will be created during
		/// the task</summary>
		protected readonly IActionHandler m_actionHandler;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected UnitOfWorkHelper(IActionHandler actionHandler)
		{
			if (actionHandler == null) throw new ArgumentNullException("actionHandler");

			m_actionHandler = actionHandler;
			RollBack = true;
		}

		#region IDisposable & Co. implementation

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~UnitOfWorkHelper()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		#region Implementation of IFWDisposable
		/// <summary>
		/// This method throws an ObjectDisposedException if IsDisposed returns
		///             true.  This is the case where a method or property in an object is being
		///             used but the object itself is no longer valid.
		///             This method should be added to all public properties and methods of this
		///             object and all other objects derived from it (extensive).
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException("'UndoableUnitOfWorkHelper' in use after being disposed.");
		}

		/// <summary>
		/// Add the public property for knowing if the object has been disposed of yet
		/// </summary>
		public bool IsDisposed { get; private set; }

		#endregion

		#region Implementation of IDisposable

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		/// <filterpriority>2</filterpriority>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the underlying issue.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected virtual void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Method can be called several times,
			// but the main code must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				if (RollBack)
					RollBackChanges();
				else
					EndUndoTask();
				DoStuffAfterEndingTask();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.

			IsDisposed = true;
		}
		#endregion

		#endregion IDisposable & Co. implementation

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ends the undo task and rolls back any changes that were made
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void RollBackChanges()
		{
			m_actionHandler.Rollback(0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ends the undo task when not rolling back the changes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected abstract void EndUndoTask();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Does anything needing to be done after ending the undo task (whether it rolled
		/// back or not).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void DoStuffAfterEndingTask()
		{
			// Default is to do nothing
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set whether to rollback the Unit Of Work or to commit it, when the object is disposed.
		/// </summary>
		/// <remarks>The defauilt is 'true', which will do the rollback on the UOW.</remarks>
		/// ------------------------------------------------------------------------------------
		public bool RollBack { private get; set; }
	}
}
