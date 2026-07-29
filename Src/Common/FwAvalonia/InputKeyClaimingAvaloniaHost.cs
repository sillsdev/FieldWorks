// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.Windows.Forms;
using Avalonia.Win32.Interoperability;

namespace SIL.FieldWorks.Common.FwAvalonia
{
	/// <summary>
	/// The pure input-key-claiming decision, split out from <see cref="InputKeyClaimingAvaloniaHost"/> so
	/// it is unit-testable without a realized window — and without a test assembly needing to load the
	/// Avalonia interop base type the host derives from.
	/// </summary>
	public static class InputKeyClaimPolicy
	{
		/// <summary>
		/// Whether the host claims <paramref name="keyData"/> as an input key: the arrow keys always, Enter
		/// only when <paramref name="claimEnterKey"/> is set, and never unless the host holds focus.
		/// </summary>
		public static bool ShouldClaimKey(Keys keyData, bool hostContainsFocus, bool claimEnterKey)
		{
			if (!hostContainsFocus)
				return false;
			switch ((int)(keyData & Keys.KeyCode))
			{
				case 0x26: // Up
				case 0x28: // Down
				case 0x25: // Left
				case 0x27: // Right
					return true;
				case 0x0D: // Enter
					return claimEnterKey;
				default:
					return false;
			}
		}
	}

	/// <summary>
	/// A <see cref="WinFormsAvaloniaControlHost"/> that claims the keyboard-navigation keys the hosted
	/// Avalonia surface needs, so the WinForms parent (a detail pane, or a modal dialog form) does not
	/// consume Up/Down/Left/Right — and, when asked, Enter — as its own control-navigation / default-button
	/// handling before the Avalonia content sees them. Without this, WinForms eats the presses and hosted
	/// list/keyboard navigation does nothing. Keys are claimed only while this host holds focus, so they
	/// route normally when focus is elsewhere in the parent.
	/// </summary>
	public class InputKeyClaimingAvaloniaHost : WinFormsAvaloniaControlHost
	{
		private static readonly TraceSwitch s_interopTrace =
			new TraceSwitch("FwAvaloniaHostInterop", "WinForms/Avalonia keyboard interop diagnostics");

		private readonly bool _claimEnterKey;

		/// <summary>
		/// Creates the host, optionally claiming Enter in addition to the arrow keys.
		/// </summary>
		/// <param name="claimEnterKey">
		/// Also claim Enter as an input key. A dialog host needs this so Enter reaches the hosted content
		/// (e.g. commit-on-Enter in a search box) instead of activating the form's default button; a detail
		/// pane host leaves it false so Enter keeps its normal meaning there.
		/// </param>
		public InputKeyClaimingAvaloniaHost(bool claimEnterKey = false)
		{
			_claimEnterKey = claimEnterKey;
		}

		private bool ShouldClaim(Keys keyData) => InputKeyClaimPolicy.ShouldClaimKey(keyData, ContainsFocus, _claimEnterKey);

		private void LogInterop(string message)
		{
			if (s_interopTrace.TraceInfo)
				Trace.WriteLine("[" + GetType().Name + "] " + message);
		}

		protected override bool IsInputKey(Keys keyData)
		{
			if (ShouldClaim(keyData))
			{
				LogInterop("IsInputKey -> true for " + (keyData & Keys.KeyCode));
				return true;
			}
			return base.IsInputKey(keyData);
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (ShouldClaim(keyData))
			{
				LogInterop("ProcessCmdKey bypass for " + (keyData & Keys.KeyCode)
					+ " while the Avalonia host contains focus.");
				return false;
			}
			return base.ProcessCmdKey(ref msg, keyData);
		}
	}
}
