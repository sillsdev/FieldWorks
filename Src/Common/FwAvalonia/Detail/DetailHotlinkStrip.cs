// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace SIL.FieldWorks.Common.FwAvalonia.Detail
{
	/// <summary>
	/// A section header's hotlinks: one small tinted button per command, left to right in
	/// menu order. Only the leading links that fit the width the strip is given are
	/// drawn; the rest are laid out at zero size and leave the Tab order, so a narrow pane
	/// never shows half a link.
	/// </summary>
	internal sealed class DetailHotlinkStrip : Panel
	{
		/// <summary>
		/// Separates a row's automation id from a link's index in that link's automation id, e.g.
		/// "Senses/item0.Hotlinks.1".
		/// </summary>
		internal const string LinkIdSegment = ".Hotlinks.";

		private readonly string _rowAutomationId;

		// How many leading links the last measure found room for.
		private int _fittingCount;

		/// <summary>Creates an empty strip for the header row with
		/// <paramref name="rowAutomationId"/>.</summary>
		public DetailHotlinkStrip(string rowAutomationId)
		{
			_rowAutomationId = rowAutomationId ?? string.Empty;
			HorizontalAlignment = HorizontalAlignment.Right;
			AutomationProperties.SetAutomationId(this, _rowAutomationId + ".Hotlinks");
		}

		/// <summary>
		/// Replaces the links with one per command leaf of <paramref name="items"/>; separators
		/// and submenus are skipped. A disabled command's link is shown greyed and cannot be
		/// clicked. Null shows nothing.
		/// </summary>
		public void Show(IReadOnlyList<DetailMenuItem> items)
		{
			Children.Clear();
			if (items == null)
				return;
			foreach (var item in items.Where(i => !i.IsSeparator && i.Children.Count == 0))
				Children.Add(CreateLink(item, Children.Count));
		}

		/// <summary>Removes every link.</summary>
		public void Clear() => Children.Clear();

		private Button CreateLink(DetailMenuItem item, int index)
		{
			// The text block carries the colour itself: a button theme restyles its own
			// presenter on hover and when disabled, which would override a colour set there.
			var text = new TextBlock
			{
				Text = item.Label,
				Foreground = item.IsEnabled ? FwAvaloniaDensity.HotlinkButtonForeground : FwAvaloniaDensity.DisabledOptionBrush,
				FontSize = FwAvaloniaDensity.HotlinkFontSize,
				FontWeight = FontWeight.Normal
			};
			var link = new Button
			{
				Content = text,
				IsEnabled = item.IsEnabled,
				BorderThickness = new Thickness(0),
				Padding = FwAvaloniaDensity.HotlinkPadding,
				CornerRadius = FwAvaloniaDensity.HotlinkCornerRadius,
				MinHeight = 0,
				VerticalAlignment = VerticalAlignment.Center,
				Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
			};
			// The theme draws a disabled button with its resting fill, so only a command that
			// can run gets the tint; an unavailable one keeps the theme's neutral grey.
			if (item.IsEnabled)
			{
				link.Resources["ButtonDefaultBackground"] = FwAvaloniaDensity.HotlinkButtonBackground;
				link.Resources["ButtonDefaultPointeroverBackground"] = FwAvaloniaDensity.HotlinkButtonPointeroverBackground;
				link.Resources["ButtonDefaultPressedBackground"] = FwAvaloniaDensity.HotlinkButtonPressedBackground;
			}
			// The links are the keyboard's only way to these commands inside the view, so each
			// is a Tab stop, ordered with the header row they sit on (LT-22688).
			Avalonia.Input.KeyboardNavigation.SetTabIndex(link,
				Avalonia.Input.KeyboardNavigation.GetTabIndex(this));
			AutomationProperties.SetAutomationId(link, _rowAutomationId + LinkIdSegment + index);
			AutomationProperties.SetName(link, item.Label);
			var execute = item.Execute;
			if (execute != null)
				link.Click += (s, e) => execute();
			return link;
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			var gap = FwAvaloniaDensity.HotlinkGap;
			double width = 0, height = 0;
			_fittingCount = 0;
			var stillFits = true;
			foreach (var child in Children)
			{
				child.Measure(Size.Infinity);
				if (!stillFits)
					continue;
				var needed = (_fittingCount == 0 ? 0 : gap) + child.DesiredSize.Width;
				// The first link that does not fit ends the strip, so the links shown are always
				// a prefix of the menu.
				if (width + needed > availableSize.Width)
				{
					stillFits = false;
					continue;
				}
				width += needed;
				height = Math.Max(height, child.DesiredSize.Height);
				_fittingCount++;
			}
			return new Size(width, height);
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			var gap = FwAvaloniaDensity.HotlinkGap;
			double x = 0;
			for (var i = 0; i < Children.Count; i++)
			{
				var child = Children[i];
				// A link left out for want of room must not take Tab focus while unseen.
				Avalonia.Input.KeyboardNavigation.SetIsTabStop(child, i < _fittingCount);
				if (i >= _fittingCount)
				{
					child.Arrange(new Rect());
					continue;
				}
				child.Arrange(new Rect(x, 0, child.DesiredSize.Width, finalSize.Height));
				x += child.DesiredSize.Width + gap;
			}
			return finalSize;
		}
	}

	/// <summary>
	/// A section header with its hotlinks on the same line, right-aligned. The header keeps the
	/// width its text needs; the hotlinks get what is left, and the header takes back any
	/// width they do not use.
	/// </summary>
	internal sealed class DetailHeaderLine : Panel
	{
		private readonly Control _header;
		private readonly DetailHotlinkStrip _hotlinks;

		/// <summary>Lays out <paramref name="header"/> with <paramref name="hotlinks"/> to its
		/// right.</summary>
		public DetailHeaderLine(Control header, DetailHotlinkStrip hotlinks)
		{
			_header = header ?? throw new ArgumentNullException(nameof(header));
			_hotlinks = hotlinks ?? throw new ArgumentNullException(nameof(hotlinks));
			// The links sit level with the header text, whatever vertical margin the header
			// keeps, and clear of the row's right edge.
			_hotlinks.Margin = new Thickness(0, header.Margin.Top, FwAvaloniaDensity.TrailingGap.Right,
				header.Margin.Bottom);
			Children.Add(header);
			Children.Add(hotlinks);
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			_header.Measure(availableSize);
			var remaining = Math.Max(0, availableSize.Width - _header.DesiredSize.Width);
			_hotlinks.Measure(new Size(remaining, availableSize.Height));
			return new Size(_header.DesiredSize.Width + _hotlinks.DesiredSize.Width,
				Math.Max(_header.DesiredSize.Height, _hotlinks.DesiredSize.Height));
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			var hotlinksWidth = Math.Min(_hotlinks.DesiredSize.Width, finalSize.Width);
			_header.Arrange(new Rect(0, 0, finalSize.Width - hotlinksWidth, finalSize.Height));
			_hotlinks.Arrange(new Rect(finalSize.Width - hotlinksWidth, 0, hotlinksWidth, finalSize.Height));
			return finalSize;
		}
	}
}
