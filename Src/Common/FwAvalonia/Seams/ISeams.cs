// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.Common.FwAvalonia.Seams
{
	/// <summary>
	/// Fenced edit-session boundary (see avalonia-edit-sessions). The product implementation fences a
	/// real LCModel undo task; both the legacy adapter and the Avalonia editors drive commit/cancel
	/// through this contract.
	/// </summary>
	public interface IEditSession
	{
		bool IsOpen { get; }

		void Commit();

		void Cancel();
	}

	/// <summary>
	/// Refresh policy seam over the legacy DataTree <c>DoNotRefresh</c>/<c>RefreshListNeeded</c> gate
	/// (LT-22414). Lets refresh coordination be tested without a WinForms control (task 3.1, 3.2).
	/// </summary>
	public interface ILexicalRefreshCoordinator
	{
		/// <summary>Whether refreshes are currently suspended.</summary>
		bool IsSuspended { get; }

		/// <summary>Whether a refresh was requested while suspended and is still pending.</summary>
		bool RefreshPending { get; }

		/// <summary>Begins suspending refreshes (legacy <c>DoNotRefresh = true</c>).</summary>
		void BeginSuspend();

		/// <summary>
		/// Ends suspension (legacy <c>DoNotRefresh = false</c>). Returns true if a refresh was requested
		/// while suspended and should now run.
		/// </summary>
		bool EndSuspend();

		/// <summary>
		/// Requests a refresh. Returns true if the refresh should run immediately; false if it was
		/// suppressed because refreshes are suspended (and is now pending).
		/// </summary>
		bool RequestRefresh();
	}

	/// <summary>Thin UI-thread scheduling seam (see avalonia-ui-scheduler, task 3.7).</summary>
	public interface IUiScheduler
	{
		/// <summary>Whether the caller is on the UI thread.</summary>
		bool IsOnUiThread { get; }

		/// <summary>Posts work to run on the UI thread.</summary>
		void Post(Action action);
	}

	/// <summary>Region lifetime/disposal seam (see avalonia-lifetime, task 3.7).</summary>
	public interface IRegionLifetime : IDisposable
	{
		/// <summary>Whether the region has been disposed.</summary>
		bool IsDisposed { get; }

		/// <summary>Registers a disposable to be disposed once when the region is disposed.</summary>
		void Register(IDisposable disposable);
	}

	/// <summary>Command-bridge seam over the xCore mediator (task 3.1). Routes command ids to handlers.</summary>
	public interface IXCoreCommandBridge
	{
		/// <summary>Whether a command id can currently be executed.</summary>
		bool CanExecute(string commandId);

		/// <summary>Executes a command id; returns true if a handler accepted it.</summary>
		bool Execute(string commandId, object argument = null);
	}

	/// <summary>
	/// Record navigation context seam (tasks 3.1, 3.12): the bidirectional selection bridge over the
	/// xCore <c>RecordClerk</c>/<c>PropertyTable</c> "current record" bus. An Avalonia surface *follows*
	/// the bus through <see cref="CurrentRecordChanged"/>/<see cref="CurrentRecord"/> and *publishes* its
	/// own selection back through <see cref="PublishSelection"/> (and the movement methods), so legacy
	/// and Avalonia surfaces running concurrently stay on the same record. This bridge is coexistence
	/// infrastructure, not throwaway: the selection concept outlives WinForms.
	/// </summary>
	public interface IRecordNavigationContext
	{
		/// <summary>The record the bus currently considers selected (an <c>ICmObject</c> at the product edge).</summary>
		object CurrentRecord { get; }

		/// <summary>Raised after the bus broadcasts a new current record (follow direction).</summary>
		event EventHandler CurrentRecordChanged;

		/// <summary>Moves the bus selection to the next record, broadcasting to all surfaces.</summary>
		bool MoveNext();

		/// <summary>Moves the bus selection to the previous record, broadcasting to all surfaces.</summary>
		bool MovePrevious();

		/// <summary>
		/// Publishes this surface's selection back to the bus (publish direction). The key identifies the
		/// record (an hvo or <c>ICmObject</c> at the product edge). Returns false if the key is not
		/// understood or the record cannot be selected.
		/// </summary>
		bool PublishSelection(object recordKey);
	}
}
