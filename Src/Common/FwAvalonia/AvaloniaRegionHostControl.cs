// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using Avalonia.Win32.Interoperability;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.Seams;

namespace SIL.FieldWorks.Common.FwAvalonia
{
	/// <summary>
	/// Reusable WinForms host for an Avalonia region surface (Stage 2.1: generalized out of
	/// <see cref="LexicalEditHostControl"/> so a second region host does not re-derive the in-process
	/// net48 plumbing). Owns Avalonia bootstrap, the <see cref="WinFormsAvaloniaControlHost"/>, the
	/// companion-control strip, the WinForms/Avalonia directional-key interop, focus-safe content
	/// swapping, context menus, and the message/clear states. Region-specific projection (building the
	/// region view) belongs to the derived class via <see cref="SetRegionContent"/>.
	/// </summary>
	public abstract class AvaloniaRegionHostControl : System.Windows.Forms.UserControl
	{
		private static readonly TraceSwitch s_interopTrace =
			new TraceSwitch("FwAvaloniaHostInterop", "WinForms/Avalonia keyboard interop diagnostics");

		/// <summary>The Avalonia content host. Protected so derived region hosts can set content directly.</summary>
		protected readonly WinFormsAvaloniaControlHost Host;
		private readonly Panel _companionStrip;

		/// <summary>Raised after a hosted region reports an edit completed (wired by the derived host).</summary>
		public event EventHandler RegionEditCompleted;

		protected AvaloniaRegionHostControl()
		{
			FwAvaloniaRuntime.EnsureInitialized();

			Dock = DockStyle.Fill;
			TabStop = true;

			Host = new WinFormsAvaloniaControlHost
			{
				Dock = DockStyle.Fill,
				Name = "AvaloniaHost",
				AccessibleName = FwAvaloniaStrings.AvaloniaHostName
			};
			Host.PreviewKeyDown += OnHostPreviewKeyDown;

			_companionStrip = new Panel
			{
				Dock = DockStyle.Top,
				Name = "CompanionStrip",
				AccessibleName = "RecordEditView.AvaloniaHost.CompanionStrip",
				Visible = false,
				Height = 0,
				TabStop = false
			};

			Controls.Add(Host);
			Controls.Add(_companionStrip);
			Clear();
		}

		protected void RaiseRegionEditCompleted() => RegionEditCompleted?.Invoke(this, EventArgs.Empty);

		/// <summary>Swaps the hosted Avalonia content and shows the control.</summary>
		protected void SetHostContent(Avalonia.Controls.Control content)
		{
			Host.Content = content;
			Show();
		}

		/// <summary>The current Avalonia content, or null.</summary>
		protected Avalonia.Controls.Control CurrentContent => Host.Content as Avalonia.Controls.Control;

		private void LogInterop(string message)
		{
			if (s_interopTrace.TraceInfo)
				Trace.WriteLine("[" + GetType().Name + "] " + message);
		}

		private static bool IsDirectionalKey(int keyCode)
		{
			switch (keyCode)
			{
				case 0x26:
				case 0x28:
				case 0x25:
				case 0x27:
					return true;
				default:
					return false;
			}
		}

		private static bool ShouldBypassWinFormsDirectionalKeyHandling(bool hostContainsFocus, int keyCode)
			=> hostContainsFocus && IsDirectionalKey(keyCode);

		private void OnHostPreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
		{
			var keyCode = (int)(e.KeyData & Keys.KeyCode);
			if (ShouldBypassWinFormsDirectionalKeyHandling(Host != null && Host.ContainsFocus, keyCode))
			{
				e.IsInputKey = true;
				LogInterop("PreviewKeyDown -> IsInputKey=true for " + ((Keys)keyCode));
			}
		}

		protected override bool IsInputKey(Keys keyData)
		{
			var keyCode = (int)(keyData & Keys.KeyCode);
			if (ShouldBypassWinFormsDirectionalKeyHandling(Host != null && Host.ContainsFocus, keyCode))
			{
				LogInterop("IsInputKey -> true for " + ((Keys)keyCode));
				return true;
			}

			return base.IsInputKey(keyData);
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			var keyCode = (int)(keyData & Keys.KeyCode);
			if (ShouldBypassWinFormsDirectionalKeyHandling(Host != null && Host.ContainsFocus, keyCode))
			{
				LogInterop("ProcessCmdKey bypass for " + ((Keys)keyCode)
					+ " while Avalonia host contains focus.");
				return false;
			}

			return base.ProcessCmdKey(ref msg, keyData);
		}

		public void SetCompanionControls(IReadOnlyList<Control> controls)
		{
			for (var i = _companionStrip.Controls.Count - 1; i >= 0; i--)
			{
				var existing = _companionStrip.Controls[i];
				existing.SizeChanged -= OnCompanionControlSizeChanged;
				_companionStrip.Controls.RemoveAt(i);
			}

			if (controls != null)
			{
				for (var i = controls.Count - 1; i >= 0; i--)
				{
					var control = controls[i];
					if (control == null)
						continue;
					control.Dock = DockStyle.Top;
					control.SizeChanged += OnCompanionControlSizeChanged;
					_companionStrip.Controls.Add(control);
				}
			}

			UpdateCompanionStripHeight();
		}

		private void OnCompanionControlSizeChanged(object sender, EventArgs e)
			=> UpdateCompanionStripHeight();

		protected override void Dispose(bool disposing)
		{
			if (disposing && _companionStrip != null)
			{
				if (Host != null)
					Host.PreviewKeyDown -= OnHostPreviewKeyDown;
				for (var i = _companionStrip.Controls.Count - 1; i >= 0; i--)
				{
					var companion = _companionStrip.Controls[i];
					companion.SizeChanged -= OnCompanionControlSizeChanged;
					_companionStrip.Controls.RemoveAt(i);
				}
			}
			base.Dispose(disposing);
		}

		private void UpdateCompanionStripHeight()
		{
			var height = 0;
			foreach (Control child in _companionStrip.Controls)
				height += child.Height;
			_companionStrip.Height = height;
			_companionStrip.Visible = height > 0;
		}

		public void ShowContextMenu(IReadOnlyList<RegionMenuItem> items)
		{
			if (Host.Content is Avalonia.Controls.Control target)
				RegionMenuFlyout.Show(items, target);
		}

		public void ShowMessage(string message)
		{
			Host.Content = new Avalonia.Controls.TextBlock { Text = message ?? string.Empty };
			Show();
		}

		public void Clear()
		{
			ShowMessage(FwAvaloniaStrings.NoEntrySelected);
		}
	}
}
