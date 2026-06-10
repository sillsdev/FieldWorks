// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Avalonia.Win32.Interoperability;
using SIL.FieldWorks.Common.FwAvalonia.Graphite;
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

		/// <summary>
		/// Displays a typed-definition-backed region model in the Avalonia surface (task 4.8). This is the
		/// product render path; it replaces <see cref="ShowEntry"/>, which renders the lossy preview DTO.
		/// Optional Graphite transition warnings (graphite-transition-support task 2.1) render as banners
		/// above the fields; <paramref name="switchToLegacy"/> is the whole-surface legacy-mode affordance.
		/// </summary>
		public void ShowRegion(
			LexicalEditRegionModel region,
			IReadOnlyList<GraphiteWsClassification> graphiteWarnings = null,
			Action switchToLegacy = null)
		{
			if (region == null) throw new ArgumentNullException(nameof(region));
			_host.Content = new LexicalEditRegionView(region, graphiteWarnings, switchToLegacy);
			Show();
		}

		/// <summary>
		/// Displays the given lexical-entry DTO in the Avalonia POC slice. Preview/sample only: the
		/// product route uses <see cref="ShowRegion"/> with a typed-definition-backed region model.
		/// </summary>
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
