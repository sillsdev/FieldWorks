// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Windows.Forms;
using Avalonia.Win32.Interoperability;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.Seams;

namespace SIL.FieldWorks.Common.FwAvalonia
{
	/// <summary>
	/// WinForms wrapper that hosts the Avalonia lexical-edit region inside the product app via
	/// <see cref="WinFormsAvaloniaControlHost"/>. This is the in-process net48 path used by the
	/// feature-flagged RecordEditView integration.
	/// </summary>
	public sealed class LexicalEditHostControl : System.Windows.Forms.UserControl
	{
		private static readonly object s_initGate = new object();
		private static bool s_isAvaloniaInitialized;
		private static readonly TraceSwitch s_interopTrace =
			new TraceSwitch("FwAvaloniaHostInterop", "WinForms/Avalonia keyboard interop diagnostics");

		private readonly WinFormsAvaloniaControlHost _host;
		private readonly Panel _companionStrip;

		public LexicalEditHostControl()
		{
			EnsureAvaloniaInitialized();

			Name = "LexicalEditHostControl";
			AccessibleName = "RecordEditView.AvaloniaHost";
			AccessibleDescription = FwAvaloniaStrings.AvaloniaHostName;
			Dock = DockStyle.Fill;
			TabStop = true;

			_host = new WinFormsAvaloniaControlHost
			{
				Dock = DockStyle.Fill,
				Name = "AvaloniaHost",
				AccessibleName = FwAvaloniaStrings.AvaloniaHostName
			};
			_host.PreviewKeyDown += OnHostPreviewKeyDown;

			_companionStrip = new Panel
			{
				Dock = DockStyle.Top,
				Name = "CompanionStrip",
				AccessibleName = "RecordEditView.AvaloniaHost.CompanionStrip",
				Visible = false,
				Height = 0,
				TabStop = false
			};

			Controls.Add(_host);
			Controls.Add(_companionStrip);
			Clear();
		}

		private void LogInterop(string message)
		{
			if (s_interopTrace.TraceInfo)
				Trace.WriteLine("[LexicalEditHostControl] " + message);
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
			if (ShouldBypassWinFormsDirectionalKeyHandling(_host != null && _host.ContainsFocus, keyCode))
			{
				e.IsInputKey = true;
				LogInterop("PreviewKeyDown -> IsInputKey=true for " + ((Keys)keyCode));
			}
		}

		protected override bool IsInputKey(Keys keyData)
		{
			var keyCode = (int)(keyData & Keys.KeyCode);
			if (ShouldBypassWinFormsDirectionalKeyHandling(_host != null && _host.ContainsFocus, keyCode))
			{
				LogInterop("IsInputKey -> true for " + ((Keys)keyCode));
				return true;
			}

			return base.IsInputKey(keyData);
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			var keyCode = (int)(keyData & Keys.KeyCode);
			if (ShouldBypassWinFormsDirectionalKeyHandling(_host != null && _host.ContainsFocus, keyCode))
			{
				LogInterop("ProcessCmdKey bypass for " + ((Keys)keyCode)
					+ " while Avalonia host contains focus.");
				return false;
			}

			return base.ProcessCmdKey(ref msg, keyData);
		}

		public void SetCompanionControls(System.Collections.Generic.IReadOnlyList<Control> controls)
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
				if (_host != null)
					_host.PreviewKeyDown -= OnHostPreviewKeyDown;
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

		public event EventHandler RegionEditCompleted;

		public void ShowRegion(LexicalEditRegionModel region, IRegionEditContext editContext = null,
			Action<string> writingSystemFocused = null,
			Func<string, bool?> getExpansionState = null,
			Action<string, bool> expansionChanged = null,
			Action<RegionMenuRequest> menuRequested = null,
			Action<RegionLinkRequest> linkRequested = null,
			IFwClipboard clipboard = null)
		{
			if (region == null) throw new ArgumentNullException(nameof(region));
			var view = new LexicalEditRegionView(region, editContext, writingSystemFocused,
				getExpansionState, expansionChanged, menuRequested, linkRequested, clipboard);
			view.EditCompleted += (s, e) => RegionEditCompleted?.Invoke(this, EventArgs.Empty);

			var focusMemento = RegionFocusMemory.Capture(_host.Content as Avalonia.Controls.Control);
			if (focusMemento != null)
				RegionFocusMemory.TryRestoreScroll(view, focusMemento);
			_host.Content = view;
			if (!string.IsNullOrEmpty(focusMemento?.AutomationId))
			{
				Avalonia.Threading.Dispatcher.UIThread.Post(
					() => RegionFocusMemory.TryRestoreFocus(view, focusMemento),
					Avalonia.Threading.DispatcherPriority.Input);
			}
			Show();
		}

		public void ShowContextMenu(System.Collections.Generic.IReadOnlyList<RegionMenuItem> items)
		{
			if (_host.Content is Avalonia.Controls.Control target)
				RegionMenuFlyout.Show(items, target);
		}

		public void ShowMessage(string message)
		{
			_host.Content = new Avalonia.Controls.TextBlock { Text = message ?? string.Empty };
			Show();
		}

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

				Seams.FinalizerSafeSynchronizationContext.InstallOnCurrentThread();
				FwAvaloniaHost.BuildAvaloniaApp().SetupWithoutStarting();
				s_isAvaloniaInitialized = true;
			}
		}
	}
}
