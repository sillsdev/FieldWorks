// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using Avalonia.Win32.Interoperability;
using SIL.FieldWorks.Common.FwAvalonia.Region;

namespace SIL.FieldWorks.Common.FwAvalonia.Poc
{
	/// <summary>
	/// WinForms wrapper that hosts the Avalonia lexical-edit POC slice inside the product app via
	/// <see cref="WinFormsAvaloniaControlHost"/>. This is the in-process net48 path used by the
	/// feature-flagged RecordEditView integration.
	/// </summary>
	public sealed class PocWinFormsHostControl : System.Windows.Forms.UserControl
	{
		private static readonly object s_initGate = new object();
		private static bool s_isAvaloniaInitialized;

		private readonly WinFormsAvaloniaControlHost _host;
		private readonly Panel _companionStrip;

		public PocWinFormsHostControl()
		{
			EnsureAvaloniaInitialized();

			Name = "PocWinFormsHostControl";
			AccessibleName = "RecordEditView.AvaloniaPoc";
			AccessibleDescription = FwAvaloniaStrings.AvaloniaHostName;
			Dock = DockStyle.Fill;
			TabStop = true;

			_host = new WinFormsAvaloniaControlHost
			{
				Dock = DockStyle.Fill,
				Name = "AvaloniaHost",
				AccessibleName = FwAvaloniaStrings.AvaloniaHostName
			};

			// Hybrid companion lane: designated WinForms-only legacy slices (e.g. the Chorus
			// Messages notes bar) stack in this strip above the Avalonia surface. Collapsed and
			// invisible until the host supplies controls via SetCompanionControls.
			_companionStrip = new Panel
			{
				Dock = DockStyle.Top,
				Name = "CompanionStrip",
				AccessibleName = "RecordEditView.AvaloniaPoc.CompanionStrip",
				Visible = false,
				Height = 0,
				TabStop = false
			};

			Controls.Add(_host);
			// Added after the fill-docked host so WinForms lays the strip out first (docking
			// processes the collection from the highest index down): the strip claims the top
			// edge, the Avalonia host fills the remainder.
			Controls.Add(_companionStrip);
			Clear();
		}

		/// <summary>
		/// Hybrid companion lane: hosts externally created WinForms controls in the strip docked
		/// above the Avalonia surface. Clears any previous companions and re-adds the given ones in
		/// order (first item topmost). The strip only parents the controls — it never disposes
		/// them; lifetime stays with the caller. Pass null or empty to clear and collapse the strip.
		/// </summary>
		public void SetCompanionControls(System.Collections.Generic.IReadOnlyList<Control> controls)
		{
			for (var i = _companionStrip.Controls.Count - 1; i >= 0; i--)
			{
				var existing = _companionStrip.Controls[i];
				existing.SizeChanged -= OnCompanionControlSizeChanged;
				_companionStrip.Controls.RemoveAt(i); // never dispose: the caller owns it
			}

			if (controls != null)
			{
				// Top-docked children stack from the END of the collection, so add in reverse to
				// keep the caller's order (first control visually topmost).
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

		/// <summary>
		/// SetCompanionControls promises companion lifetime stays with the caller, but
		/// <see cref="System.Windows.Forms.Control.Dispose(bool)"/> disposes parented children —
		/// so detach the companions (and their SizeChanged hooks) BEFORE base disposal runs.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			if (disposing && _companionStrip != null)
			{
				for (var i = _companionStrip.Controls.Count - 1; i >= 0; i--)
				{
					var companion = _companionStrip.Controls[i];
					companion.SizeChanged -= OnCompanionControlSizeChanged;
					_companionStrip.Controls.RemoveAt(i); // never dispose: the caller owns it
				}
			}
			base.Dispose(disposing);
		}

		// The strip auto-sizes to its stacked children and collapses (hidden, zero-height) when empty.
		private void UpdateCompanionStripHeight()
		{
			var height = 0;
			foreach (Control child in _companionStrip.Controls)
				height += child.Height;
			_companionStrip.Height = height;
			_companionStrip.Visible = height > 0;
		}

		/// <summary>
		/// Raised after a region edit commits or cancels, so the product host can re-resolve and
		/// re-show the region from current domain state (tasks 6.8/6.10).
		/// </summary>
		public event EventHandler RegionEditCompleted;

		/// <summary>
		/// Displays a typed-definition-backed region model in the Avalonia surface (task 4.8). This is the
		/// product render path (the lossy preview-DTO rendering lives in the preview-only POC types).
		/// With an <paramref name="editContext"/> the region is editable through the fenced LCModel
		/// session (tasks 6.8/6.10); without one it is read-only display.
		/// </summary>
		public void ShowRegion(LexicalEditRegionModel region, IRegionEditContext editContext = null,
			Action<string> writingSystemFocused = null,
			Func<string, bool?> getExpansionState = null,
			Action<string, bool> expansionChanged = null,
			Action<RegionMenuRequest> menuRequested = null,
			Action<RegionLinkRequest> linkRequested = null)
		{
			if (region == null) throw new ArgumentNullException(nameof(region));
			var view = new LexicalEditRegionView(region, editContext, writingSystemFocused,
				getExpansionState, expansionChanged, menuRequested, linkRequested);
			view.EditCompleted += (s, e) => RegionEditCompleted?.Invoke(this, EventArgs.Empty);

			// Focus continuity (14.4): re-shows replace the whole view, so carry the focused
			// editor (by stable automation id) and caret across the swap — otherwise every
			// auto-commit re-show would dump keyboard focus.
			var focusMemento = RegionFocusMemory.Capture(_host.Content as Avalonia.Controls.Control);
			_host.Content = view;
			if (focusMemento != null)
			{
				Avalonia.Threading.Dispatcher.UIThread.Post(
					() => RegionFocusMemory.TryRestore(view, focusMemento),
					Avalonia.Threading.DispatcherPriority.Input);
			}
			Show();
		}

		/// <summary>
		/// 15.1: shows host-resolved context-menu items as a native Avalonia flyout at the current
		/// pointer position over the displayed region.
		/// </summary>
		public void ShowContextMenu(System.Collections.Generic.IReadOnlyList<RegionMenuItem> items)
		{
			if (_host.Content is Avalonia.Controls.Control target)
				RegionMenuFlyout.Show(items, target);
		}

		/// <summary>Displays a simple placeholder message instead of a slice.</summary>
		public void ShowMessage(string message)
		{
			_host.Content = new Avalonia.Controls.TextBlock { Text = message ?? string.Empty };
			Show();
		}

		/// <summary>Clears the current slice and shows a minimal placeholder.</summary>
		public void Clear()
		{
			ShowMessage(FwAvaloniaStrings.NoEntrySelected);
		}

		private static void EnsureAvaloniaInitialized()
		{
			if (s_isAvaloniaInitialized)
				return;

			lock (s_initGate)
			{
				if (s_isAvaloniaInitialized)
					return;

				// 16.1 crash guard: MicroCom proxy finalizers post their native Release through the
				// SynchronizationContext captured at proxy creation; install the finalizer-safe
				// wrapper FIRST so every proxy captures it instead of the raw WinForms context
				// (whose Post terminates the process once the marshaling window is gone).
				Seams.FinalizerSafeSynchronizationContext.InstallOnCurrentThread();
				PocAvaloniaHost.BuildAvaloniaApp().SetupWithoutStarting();
				s_isAvaloniaInitialized = true;
			}
		}
	}
}
