// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// §19d: the picture-properties dialog body — a pure-code <see cref="UserControl"/> (no XAML compiler
	/// dependency, like the FwAvalonia owned controls) bound to <see cref="PicturePropertiesDialogViewModel"/>.
	/// Lays out caption / description / license / creator entries, the chosen-image row with a "Choose
	/// image…" button (which calls back to the host's file picker through <see cref="ChooseImageRequested"/>),
	/// and OK/Cancel wired to the kit's commands. Hosted in a WinForms-owned modal via
	/// <c>AvaloniaDialogHost.ShowModal</c> during coexistence.
	/// </summary>
	public sealed class PicturePropertiesDialogView : UserControl
	{
		/// <summary>
		/// Raised when the user clicks "Choose image…". The launcher subscribes and runs the host's
		/// Avalonia file picker, then calls <see cref="PicturePropertiesDialogViewModel.SetImageFile"/>.
		/// </summary>
		public event EventHandler ChooseImageRequested;

		private readonly PicturePropertiesDialogViewModel _viewModel;
		private readonly TextBlock _imageDisplay;

		public PicturePropertiesDialogView(PicturePropertiesDialogViewModel viewModel)
		{
			_viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
			DialogThemeBootstrap.Apply(this);
			DataContext = viewModel;
			AutomationProperties.SetAutomationId(this, "PicturePropertiesDialog");

			var root = new StackPanel { Spacing = 8, MinWidth = 360 };

			root.Children.Add(Label(FwAvaloniaDialogsStrings.PicturePropertiesImageLabel));
			_imageDisplay = new TextBlock
			{
				Text = viewModel.ImageFileDisplay,
				Foreground = Brushes.Gray,
				VerticalAlignment = VerticalAlignment.Center
			};
			AutomationProperties.SetAutomationId(_imageDisplay, "PicturePropertiesDialog.ImageFile");
			var chooseButton = new Button { Content = FwAvaloniaDialogsStrings.PicturePropertiesChooseImage };
			AutomationProperties.SetAutomationId(chooseButton, "PicturePropertiesDialog.ChooseImage");
			chooseButton.Click += (s, e) => ChooseImageRequested?.Invoke(this, EventArgs.Empty);
			var imageRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
			imageRow.Children.Add(_imageDisplay);
			imageRow.Children.Add(chooseButton);
			root.Children.Add(imageRow);

			root.Children.Add(Label(FwAvaloniaDialogsStrings.PicturePropertiesCaptionLabel));
			root.Children.Add(Entry("PicturePropertiesDialog.Caption", () => _viewModel.Caption,
				v => _viewModel.Caption = v));

			root.Children.Add(Label(FwAvaloniaDialogsStrings.PicturePropertiesDescriptionLabel));
			root.Children.Add(Entry("PicturePropertiesDialog.Description", () => _viewModel.Description,
				v => _viewModel.Description = v));

			root.Children.Add(Label(FwAvaloniaDialogsStrings.PicturePropertiesLicenseLabel));
			root.Children.Add(Entry("PicturePropertiesDialog.License", () => _viewModel.License,
				v => _viewModel.License = v));

			root.Children.Add(Label(FwAvaloniaDialogsStrings.PicturePropertiesCreatorLabel));
			root.Children.Add(Entry("PicturePropertiesDialog.Creator", () => _viewModel.Creator,
				v => _viewModel.Creator = v));

			var buttons = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 8,
				HorizontalAlignment = HorizontalAlignment.Right
			};
			var ok = new Button { Content = viewModel.OkCaption, Command = viewModel.OkCommand, IsDefault = true };
			AutomationProperties.SetAutomationId(ok, "PicturePropertiesDialog.Ok");
			var cancel = new Button { Content = FwAvaloniaDialogsStrings.Cancel, Command = viewModel.CancelCommand, IsCancel = true };
			AutomationProperties.SetAutomationId(cancel, "PicturePropertiesDialog.Cancel");
			buttons.Children.Add(ok);
			buttons.Children.Add(cancel);
			root.Children.Add(buttons);

			// Keep the displayed file in sync when the VM's file changes (after the picker returns).
			viewModel.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName == nameof(PicturePropertiesDialogViewModel.ImageFileDisplay))
					_imageDisplay.Text = _viewModel.ImageFileDisplay;
			};

			Content = root;
		}

		private static TextBlock Label(string text) => new TextBlock { Text = text };

		private static TextBox Entry(string automationId, Func<string> get, Action<string> set)
		{
			var box = new TextBox { Text = get() ?? string.Empty };
			AutomationProperties.SetAutomationId(box, automationId);
			box.TextChanged += (s, e) => set(box.Text ?? string.Empty);
			return box;
		}
	}
}
