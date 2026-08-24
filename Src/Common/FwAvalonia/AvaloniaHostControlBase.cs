// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Avalonia.Win32.Interoperability;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwAvalonia.Seams;

namespace SIL.FieldWorks.Common.FwAvalonia
{
	/// <summary>
	/// Reusable WinForms host for an Avalonia detail view. Owns Avalonia bootstrap, the
	/// <see cref="WinFormsAvaloniaControlHost"/>, the companion-control strip, the WinForms/Avalonia
	/// directional-key interop, focus-safe content swapping, context menus, and the message/clear
	/// states. Detail-specific projection (building the detail view) belongs to the derived class
	/// via <see cref="SetHostContent"/>.
	/// </summary>
	public abstract class AvaloniaHostControlBase : System.Windows.Forms.UserControl
	{
		/// <summary>The Avalonia content host. Protected so derived detail hosts can set content directly.</summary>
		protected readonly WinFormsAvaloniaControlHost Host;
		private readonly Panel _companionStrip;

		/// <summary>Raised after a hosted detail view reports an edit completed (wired by the derived host).</summary>
		public event EventHandler DetailEditCompleted;

		protected AvaloniaHostControlBase()
		{
			FwAvaloniaRuntime.EnsureInitialized();

			Dock = DockStyle.Fill;
			TabStop = true;

			// The host claims the arrow keys for the hosted Avalonia control so WinForms does not consume them as
			// control navigation before the hosted content sees them (Enter stays with WinForms in a pane).
			Host = new InputKeyClaimingAvaloniaHost
			{
				Dock = DockStyle.Fill,
				Name = "AvaloniaHost",
				AccessibleName = FwAvaloniaStrings.AvaloniaHostName
			};
			// Headless (test) platform: no Win32 top-level exists, so make the host's HWND reparent a
			// deliberate no-op. The Avalonia content still constructs and lays out off-screen. No-op (and
			// thus identical) on the real Win32 platform.
			FwAvaloniaPlatform.GuardHeadlessEmbed(Host);

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

		protected void RaiseDetailEditCompleted() => DetailEditCompleted?.Invoke(this, EventArgs.Empty);

		/// <summary>Swaps the hosted Avalonia content and shows the control.</summary>
		protected void SetHostContent(Avalonia.Controls.Control content)
		{
			Host.Content = content;
			Show();
		}

		/// <summary>The current Avalonia content, or null.</summary>
		protected Avalonia.Controls.Control CurrentContent => Host.Content as Avalonia.Controls.Control;

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

		/// <summary>
		/// Shows a context menu of 'items'.
		/// </summary>
		/// <param name="anchor">The control to anchor the menu to.</param>
		/// <param name="atPointer">True: Open at the pointer location.
		///                         False: Open under the anchor control.</param>
		public void ShowContextMenu(IReadOnlyList<DetailMenuItem> items,
			Avalonia.Controls.Control anchor, bool atPointer)
		{
			var target = anchor ?? Host.Content as Avalonia.Controls.Control;
			DetailMenuFlyout.Show(items, target, atPointer);
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
