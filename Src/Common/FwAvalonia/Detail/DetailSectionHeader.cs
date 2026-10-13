// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;

namespace SIL.FieldWorks.Common.FwAvalonia.Detail
{
	/// <summary>
	/// A collapsible section header: a flat button showing a +/- box before the section label.
	/// The box shows minus while the section is expanded and plus while it is collapsed. A
	/// header with no rows under it cannot toggle; it keeps the box's space but draws no box,
	/// so its label lines up with its siblings'. The owner applies the toggle on Click, which
	/// the ExpandCollapse automation pattern raises too, and then sets
	/// <see cref="IsExpanded"/>.
	/// </summary>
	public sealed class DetailSectionHeader : Button
	{
		private DetailSectionHeaderAutomationPeer _peer;
		private bool _isExpanded;

		/// <summary>Creates a header showing <paramref name="label"/>.</summary>
		public DetailSectionHeader(string label, bool isExpanded, bool canToggle)
		{
			CanToggle = canToggle;
			_isExpanded = isExpanded;
			Box = new DetailExpanderBox(isExpanded) { Opacity = canToggle ? 1 : 0 };
			Background = FwAvaloniaDensity.TransparentBrush;
			BorderThickness = new Thickness(0);
			Padding = new Thickness(0);
			// A section header hugs the left edge.
			HorizontalAlignment = HorizontalAlignment.Left;
			HorizontalContentAlignment = HorizontalAlignment.Left;
			var text = CreateLabel(label);
			text.VerticalAlignment = VerticalAlignment.Center;
			Content = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = FwAvaloniaDensity.TreeBoxGap,
				Children = { Box, text }
			};
		}

		/// <summary>
		/// The section header label text, in the section header size, colour and bold weight,
		/// for a header that cannot collapse as well as inside this one.
		/// </summary>
		public static TextBlock CreateLabel(string label) => new TextBlock
		{
			Text = label ?? string.Empty,
			FontSize = FwAvaloniaDensity.SectionHeaderFontSize,
			Foreground = FwAvaloniaDensity.SectionHeaderBrush,
			FontWeight = FontWeight.Bold
		};

		/// <summary>Themed as a plain <see cref="Button"/>.</summary>
		protected override Type StyleKeyOverride => typeof(Button);

		/// <summary>False when the header has no rows to show or hide.</summary>
		public bool CanToggle { get; }

		/// <summary>
		/// Whether the rows under the header are shown. Setting it redraws the +/- box and tells
		/// assistive technology the state changed; it does not show or hide any rows.
		/// </summary>
		public bool IsExpanded
		{
			get => _isExpanded;
			set
			{
				if (_isExpanded == value)
					return;
				var old = ExpandCollapseState;
				_isExpanded = value;
				Box.IsExpanded = value;
				_peer?.RaiseExpandCollapseStateChanged(old, ExpandCollapseState);
			}
		}

		/// <summary>The +/- box.</summary>
		public DetailExpanderBox Box { get; }

		private ExpandCollapseState ExpandCollapseState
			=> !CanToggle ? ExpandCollapseState.LeafNode
				: _isExpanded ? ExpandCollapseState.Expanded
				: ExpandCollapseState.Collapsed;

		/// <inheritdoc />
		protected override AutomationPeer OnCreateAutomationPeer()
			=> _peer = new DetailSectionHeaderAutomationPeer(this);

		private void RequestToggle() => OnClick();

		private sealed class DetailSectionHeaderAutomationPeer : ButtonAutomationPeer, IExpandCollapseProvider
		{
			public DetailSectionHeaderAutomationPeer(DetailSectionHeader owner)
				: base(owner)
			{
			}

			private DetailSectionHeader Header => (DetailSectionHeader)Owner;

			public ExpandCollapseState ExpandCollapseState => Header.ExpandCollapseState;

			public bool ShowsMenu => false;

			public void Expand()
			{
				if (Header.ExpandCollapseState == ExpandCollapseState.Collapsed)
					Header.RequestToggle();
			}

			public void Collapse()
			{
				if (Header.ExpandCollapseState == ExpandCollapseState.Expanded)
					Header.RequestToggle();
			}

			public void RaiseExpandCollapseStateChanged(ExpandCollapseState old, ExpandCollapseState current)
				=> RaisePropertyChangedEvent(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
					old, current);
		}
	}

	/// <summary>
	/// The square +/- box of a <see cref="DetailSectionHeader"/>: a thin outline holding a
	/// minus bar, plus a vertical bar while collapsed.
	/// </summary>
	public sealed class DetailExpanderBox : Border
	{
		private readonly Rectangle _verticalBar;

		/// <summary>Creates a box drawing a minus when <paramref name="isExpanded"/>, else a
		/// plus.</summary>
		public DetailExpanderBox(bool isExpanded)
		{
			Width = FwAvaloniaDensity.TreeBoxSize;
			Height = FwAvaloniaDensity.TreeBoxSize;
			BorderThickness = FwAvaloniaDensity.HairlineBorderThickness;
			BorderBrush = FwAvaloniaDensity.TreeBoxBorderBrush;
			Background = FwAvaloniaDensity.TreeBoxBackgroundBrush;
			VerticalAlignment = VerticalAlignment.Center;
			_verticalBar = Bar(FwAvaloniaDensity.TreeBoxGlyphThickness, FwAvaloniaDensity.TreeBoxGlyphLength);
			_verticalBar.IsVisible = !isExpanded;
			Child = new Panel
			{
				Children =
				{
					Bar(FwAvaloniaDensity.TreeBoxGlyphLength, FwAvaloniaDensity.TreeBoxGlyphThickness),
					_verticalBar
				}
			};
		}

		/// <summary>True when the box draws a minus, false when it draws a plus.</summary>
		public bool IsExpanded
		{
			get => !_verticalBar.IsVisible;
			set => _verticalBar.IsVisible = !value;
		}

		private static Rectangle Bar(double width, double height) => new Rectangle
		{
			Width = width,
			Height = height,
			Fill = FwAvaloniaDensity.TreeBoxGlyphBrush,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
	}
}
