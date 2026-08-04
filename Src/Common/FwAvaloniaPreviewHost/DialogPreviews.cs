// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Avalonia.Automation;
using Avalonia.Controls;
using FwAvaloniaDialogs;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Preview;
using SIL.FieldWorks.Common.FwAvalonia.PreviewHost;

// Previewable converted dialogs: one entry per surface a developer needs to look at while tuning its
// layout, selected with `--module <id>`. Registered here rather than in FwAvaloniaDialogs so no product
// assembly references preview-only code. Adding a surface is one window class plus one attribute; see
// Docs/migration/adjust-the-layout.md.
[assembly: FwPreviewModule(
	"create-feature",
	"Create New Feature",
	typeof(CreateFeatureDialogPreviewWindow))]
[assembly: FwPreviewModule(
	"message-box",
	"Message Box",
	typeof(MessageBoxPreviewWindow))]

namespace SIL.FieldWorks.Common.FwAvalonia.PreviewHost
{
	/// <summary>
	/// Base window for previewing one converted dialog body at the client size its launcher gives it, with
	/// the same compact density the runtime applies. <c>AvaloniaDialogHost.ShowModal</c> normally applies
	/// that density and owns the window; there is no modal host here, so it is applied directly — without
	/// it the preview renders at roomy Fluent defaults and misleads.
	///
	/// The title bar, icon, and close button belong to the WinForms host form at runtime and are therefore
	/// not represented here; this shows the dialog BODY at its real size.
	/// </summary>
	public abstract class DialogPreviewWindow : Window
	{
		protected DialogPreviewWindow(Control dialogBody, int clientWidth, int clientHeight, string automationId)
		{
			CompactDialogStyles.Apply(dialogBody);
			Width = clientWidth;
			Height = clientHeight;
			Content = dialogBody;
			AutomationProperties.SetAutomationId(this, automationId);
		}
	}

	/// <summary>
	/// The create-new-feature dialog (name + abbreviation over OK/Cancel). Size mirrors
	/// <c>LcmCreateFeatureLauncher</c>'s DialogWidth/DialogHeight; keep them in step so what the preview
	/// shows is what the dialog ships as.
	/// </summary>
	public sealed class CreateFeatureDialogPreviewWindow : DialogPreviewWindow
	{
		public CreateFeatureDialogPreviewWindow()
			: base(new CreateFeatureDialogView { DataContext = new CreateFeatureDialogViewModel() },
				340, 200, "CreateFeatureDialogPreviewWindow")
		{
		}
	}

	/// <summary>
	/// The reusable message/confirmation dialog, shown with a Yes/No warning — the shape most confirmation
	/// call sites use. Size matches what its own headless tests realize it at.
	/// </summary>
	public sealed class MessageBoxPreviewWindow : DialogPreviewWindow
	{
		public MessageBoxPreviewWindow()
			: base(Build(), 360, 160, "MessageBoxPreviewWindow")
		{
		}

		private static Control Build()
		{
			var viewModel = new MessageBoxViewModel(
				"This entry has senses that will also be deleted. Do you want to continue?",
				FwMessageBoxButtons.YesNo,
				FwMessageBoxIcon.Warning);
			return new MessageBoxView { DataContext = viewModel };
		}
	}
}
