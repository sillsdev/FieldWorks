// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;

namespace SIL.FieldWorks.Common.FwAvalonia.Seams
{
	/// <summary>Which framework renders a lexical-edit surface.</summary>
	public enum LexicalEditSurfaceKind
	{
		/// <summary>The legacy WinForms DataTree/Slice surface.</summary>
		Legacy,

		/// <summary>The Avalonia surface.</summary>
		Avalonia
	}

	/// <summary>
	/// Host/surface contract for the Lexical Edit screen (task 3.5). Captures the responsibilities
	/// <c>RecordEditView</c> currently performs inline — surface initialization, showing a record,
	/// focus, context menus, and view replacement — so the active-host contract (task 3.10) can be
	/// stated and tested without reaching into WinForms internals. A surface is one renderable
	/// implementation (legacy DataTree or an Avalonia host); the host owns the surfaces and swaps
	/// between them under the app-wide UI mode.
	/// </summary>
	public interface ILexicalEditSurface
	{
		/// <summary>Which framework this surface is.</summary>
		LexicalEditSurfaceKind Kind { get; }

		/// <summary>Whether the surface has performed its (lazy) one-time initialization.</summary>
		bool IsInitialized { get; }

		/// <summary>
		/// Performs one-time initialization (control creation, persistence, menus, layout inventory).
		/// Idempotent. Per the active-host contract, the host must only initialize the surface it is
		/// about to make active, except for an explicitly approved baseline adapter.
		/// </summary>
		void EnsureInitialized();

		/// <summary>Shows the given record on this surface. The record is opaque to the contract.</summary>
		void ShowRecord(object record);

		/// <summary>Hides this surface (used when another surface becomes active).</summary>
		void Hide();

		/// <summary>Attempts to put input focus on this surface; returns whether focus was taken.</summary>
		bool TrySetFocus();

		/// <summary>Attempts to show a context menu for the given context; returns whether one was shown.</summary>
		bool TryShowContextMenu(object context);

		/// <summary>Called before the host replaces this surface with another (commit/flush hook).</summary>
		void PrepareToReplace();
	}

	/// <summary>
	/// The screen that owns lexical-edit surfaces and replaces the active one under the UI mode
	/// (task 3.5). Implemented by <c>RecordEditView</c> in the product app.
	/// </summary>
	public interface ILexicalEditHost
	{
		/// <summary>The currently active surface kind.</summary>
		LexicalEditSurfaceKind ActiveSurface { get; }

		/// <summary>The surfaces this host owns.</summary>
		IReadOnlyList<ILexicalEditSurface> Surfaces { get; }

		/// <summary>
		/// Makes the given surface active and shows the record on it, hiding the previously active
		/// surface. Honors the active-host contract: the inactive surface is not driven.
		/// </summary>
		void ReplaceSurface(LexicalEditSurfaceKind kind, object record);
	}
}
