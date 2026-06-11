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

	/// <summary>
	/// Editor-selection seam in front of <c>SliceFactory</c> (task 3.3). Editor keys resolve to a legacy
	/// slice handler now and to Avalonia editors later, without the caller knowing which adapter answers.
	/// </summary>
	public interface ILexicalEditorRegistry
	{
		/// <summary>Registers a handler token for an editor key.</summary>
		void Register(string editorKey, object handler);

		/// <summary>Resolves the handler for an editor key, or the fallback handler if none is registered.</summary>
		object Resolve(string editorKey);

		/// <summary>Whether an editor key has a registered handler.</summary>
		bool IsRegistered(string editorKey);
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

	/// <summary>Property/state store seam over the xCore PropertyTable (task 3.1).</summary>
	public interface IPropertyStateStore
	{
		bool TryGet<T>(string key, out T value);

		void Set<T>(string key, T value);

		bool Remove(string key);
	}

	/// <summary>Command-bridge seam over the xCore mediator (task 3.1). Routes command ids to handlers.</summary>
	public interface IXCoreCommandBridge
	{
		/// <summary>Whether a command id can currently be executed.</summary>
		bool CanExecute(string commandId);

		/// <summary>Executes a command id; returns true if a handler accepted it.</summary>
		bool Execute(string commandId, object argument = null);
	}

	/// <summary>Record navigation context seam (task 3.1). Exposes the current record and movement.</summary>
	public interface IRecordNavigationContext
	{
		object CurrentRecord { get; }

		bool MoveNext();

		bool MovePrevious();
	}
}
