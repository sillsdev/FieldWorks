// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using Avalonia.Win32.Interoperability;

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

		public PocWinFormsHostControl()
		{
			EnsureAvaloniaInitialized();

			Name = "PocWinFormsHostControl";
			AccessibleName = "RecordEditView.AvaloniaPoc";
			Dock = DockStyle.Fill;
			TabStop = true;

			_host = new WinFormsAvaloniaControlHost
			{
				Dock = DockStyle.Fill,
				Name = "AvaloniaHost",
				AccessibleName = "Avalonia Host"
			};

			Controls.Add(_host);
			Clear();
		}

		/// <summary>Displays the given lexical-entry DTO in the Avalonia POC slice.</summary>
		public void ShowEntry(PocEntryDto entry)
		{
			if (entry == null) throw new ArgumentNullException(nameof(entry));
			_host.Content = new PocLexEntrySlice(entry);
			Show();
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
			ShowMessage("No lexical entry selected.");
		}

		private static void EnsureAvaloniaInitialized()
		{
			if (s_isAvaloniaInitialized)
				return;

			lock (s_initGate)
			{
				if (s_isAvaloniaInitialized)
					return;

				PocAvaloniaHost.BuildAvaloniaApp().SetupWithoutStarting();
				s_isAvaloniaInitialized = true;
			}
		}
	}
}
