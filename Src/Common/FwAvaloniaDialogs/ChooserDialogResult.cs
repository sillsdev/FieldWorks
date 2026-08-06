// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// The LCModel-free result of the reusable Avalonia chooser dialog. <see cref="Accepted"/> is true only when
	/// the user closed via OK; <see cref="ChosenKeys"/> carries the guid-string keys of the picked options — 0 or 1
	/// for <see cref="ChooserSelectionMode.Single"/>, N for <see cref="ChooserSelectionMode.Multi"/>. An empty-string
	/// key (<see cref="ChooserDialogInput.EmptyKey"/>) means the "&lt;Empty&gt;" row, i.e. an atomic clear; the
	/// product edge maps the keys back to <c>ICmObject</c>s (empty key =&gt; none).
	/// </summary>
	public sealed class ChooserDialogResult
	{
		public ChooserDialogResult(bool accepted, IReadOnlyList<string> chosenKeys)
		{
			Accepted = accepted;
			ChosenKeys = chosenKeys ?? Array.Empty<string>();
		}

		/// <summary>True when the user closed via OK; false on Cancel/close (in which case <see cref="ChosenKeys"/> is empty).</summary>
		public bool Accepted { get; }

		/// <summary>The chosen option keys (guid strings; empty string == the "&lt;Empty&gt;" clear row).</summary>
		public IReadOnlyList<string> ChosenKeys { get; }

		/// <summary>A not-accepted (Cancel/closed) result carrying no chosen keys.</summary>
		public static ChooserDialogResult Cancelled => new ChooserDialogResult(false, Array.Empty<string>());
	}
}
