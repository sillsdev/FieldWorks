// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace SIL.FieldWorks.Common.FwAvalonia
{
	/// <summary>
	/// Finishes an open input-method (IME) composition into the text box that owns it before a
	/// pointer press or navigation key moves focus away (LT-22887). Avalonia's Windows
	/// input-method bridge retargets itself to the newly focused box without completing the
	/// composition, so the finished text would otherwise be inserted, and saved, in whichever
	/// box receives focus.
	/// </summary>
	public static class ImeCompositionGuard
	{
		private static bool s_installed;

		/// <summary>
		/// Asks the input method to finish its open composition and deliver the text, as ordinary
		/// text input to the focused element, before returning. Defaults to the Windows IMM32
		/// request; a headless platform has no input method, so tests substitute a fake one.
		/// </summary>
		public static Action CompleteOpenComposition { get; set; }
			= Imm32.CompleteFocusedWindowComposition;

		/// <summary>
		/// Starts guarding every top-level root: windows, embedded hosts, dialogs, and popups.
		/// Idempotent.
		/// </summary>
		public static void Install()
		{
			if (s_installed)
				return;

			s_installed = true;
			InputElement.PointerPressedEvent.AddClassHandler<TopLevel>(OnPreviewPointerPressed,
				RoutingStrategies.Tunnel, handledEventsToo: true);
			InputElement.KeyDownEvent.AddClassHandler<TopLevel>(OnPreviewKeyDown,
				RoutingStrategies.Tunnel, handledEventsToo: true);
		}

		// The tunnel pass reaches the root before focus moves to the pressed element, so the box
		// that owns the composition still has focus when the input method delivers the text.
		private static void OnPreviewPointerPressed(TopLevel root, PointerPressedEventArgs e)
		{
			if (!(root.FocusManager?.GetFocusedElement() is TextBox focused))
				return;
			if (e.Source is Visual target && (target == focused || focused.IsVisualAncestorOf(target)))
				return;

			CompleteOpenComposition?.Invoke();
		}

		// A key the input method consumes arrives as Key.ImeProcessed. A navigation key that
		// reaches the editors instead can move focus, so the composition is finished first.
		private static void OnPreviewKeyDown(TopLevel root, KeyEventArgs e)
		{
			if (!IsNavigationKey(e.Key) || !(root.FocusManager?.GetFocusedElement() is TextBox))
				return;

			CompleteOpenComposition?.Invoke();
		}

		private static bool IsNavigationKey(Key key)
		{
			switch (key)
			{
				case Key.Tab:
				case Key.Up:
				case Key.Down:
				case Key.Left:
				case Key.Right:
					return true;
				default:
					return false;
			}
		}

		private static class Imm32
		{
			private const int GCS_COMPSTR = 0x0008;
			private const int NI_COMPOSITIONSTR = 0x0015;
			private const int CPS_COMPLETE = 0x0001;

			[DllImport("user32.dll")]
			private static extern IntPtr GetFocus();

			[DllImport("imm32.dll")]
			private static extern IntPtr ImmGetContext(IntPtr hWnd);

			[DllImport("imm32.dll")]
			[return: MarshalAs(UnmanagedType.Bool)]
			private static extern bool ImmReleaseContext(IntPtr hWnd, IntPtr hImc);

			[DllImport("imm32.dll")]
			private static extern int ImmGetCompositionStringW(IntPtr hImc, int index, IntPtr buffer,
				int bufferLength);

			[DllImport("imm32.dll")]
			[return: MarshalAs(UnmanagedType.Bool)]
			private static extern bool ImmNotifyIME(IntPtr hImc, int action, int index, int value);

			/// <summary>
			/// Completes the composition open in the input context of the window that has
			/// keyboard focus; a no-op when there is none.
			/// </summary>
			internal static void CompleteFocusedWindowComposition()
			{
				var hwnd = GetFocus();
				if (hwnd == IntPtr.Zero)
					return;

				var hImc = ImmGetContext(hwnd);
				if (hImc == IntPtr.Zero)
					return;

				try
				{
					if (ImmGetCompositionStringW(hImc, GCS_COMPSTR, IntPtr.Zero, 0) > 0)
						ImmNotifyIME(hImc, NI_COMPOSITIONSTR, CPS_COMPLETE, 0);
				}
				finally
				{
					ImmReleaseContext(hwnd, hImc);
				}
			}
		}
	}
}
