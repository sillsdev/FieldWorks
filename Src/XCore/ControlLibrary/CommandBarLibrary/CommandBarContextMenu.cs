// ---------------------------------------------------------
// Windows Forms CommandBar Control
// Copyright (C) 2001-2003 Lutz Roeder. All rights reserved.
// http://www.aisto.com/roeder
// roeder@aisto.com
// ---------------------------------------------------------
namespace Reflector.UserInterface
{
	using System;
	using System.Drawing;
	using System.Drawing.Imaging;
	using System.ComponentModel;
	using System.Globalization;
	using System.Security.Permissions;
	using System.Windows.Forms;

	public class CommandBarContextMenu : ContextMenu
	{
		private CommandBarItemCollection items = new CommandBarItemCollection();
		private Font font = SystemInformation.MenuFont;
		private Menu selectedMenuItem = null;
		private bool mnemonics = true;

		public CommandBarItemCollection Items
		{
			get { return this.items; }
		}

		protected override void OnPopup(EventArgs e)
		{
			base.OnPopup(e);
			this.UpdateItems();
		}

		[SecurityPermission(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
		protected override bool ProcessCmdKey(ref Message message, Keys keyData)
		{
			CommandBarItem[] shortcutHits = this.items[keyData];
			if (shortcutHits.Length != 0)
			{
				CommandBarControl control = shortcutHits[0] as CommandBarControl;
				if (control != null)
				{
					control.PerformClick(EventArgs.Empty);
					return true;
				}
			}

			return base.ProcessCmdKey(ref message, keyData);
		}

		private void UpdateItems()
		{
			this.selectedMenuItem = null;

			this.MenuItems.Clear();

			Size imageSize = GetImageSize(this.items);
			foreach (CommandBarItem item in this.items)
			{
				this.MenuItems.Add(new MenuBarItem(item, imageSize, this.font, this.mnemonics));
			}
		}

		public Font Font
		{
			get { return this.font; }

			set
			{
				this.font = value;
				this.UpdateItems();
			}
		}

		internal bool Mnemonics
		{
			set { this.mnemonics = value; }
		}

		internal Menu SelectedMenuItem
		{
			set { this.selectedMenuItem = value; }
			get { return this.selectedMenuItem; }
		}

		private static Size GetImageSize(CommandBarItemCollection items)
		{
			Size imageSize = new Size(16, 16);
			for (int i = 0; i < items.Count; i++)
			{
				Image image = items[i].Image;
				if (image != null)
				{
					if (image.Width > imageSize.Width)
					{
						imageSize.Width = image.Width;
					}

					if (image.Height > imageSize.Height)
					{
						imageSize.Height = image.Height;
					}
				}
			}

			return imageSize;
		}

		private class MenuBarItem : MenuItem
		{
			private CommandBarItem item;
			private Size imageSize;
			private Font font;
			private bool mnemonics;

			public MenuBarItem(CommandBarItem item, Size imageSize, Font font, bool mnemonics)
			{
				this.item = item;
				this.imageSize = imageSize;
				this.font = font;
				this.mnemonics = mnemonics;

				this.UpdateItems();
			}

			protected override void OnPopup(EventArgs e)
			{
				CommandBarMenu menu = this.item as CommandBarMenu;
				if (menu != null)
				{
					menu.PerformDropDown(EventArgs.Empty);
				}

				base.OnPopup(e);
				this.UpdateItems();
			}

			private void UpdateItems()
			{
				this.OwnerDraw = true;

				CommandBarSeparator separator = this.item as CommandBarSeparator;
				if (separator != null)
				{
					this.Text = "-";
				}
				else
				{
					this.Text = (this.mnemonics) ? this.item.Text : this.item.Text.Replace("&", "");
				}

				CommandBarMenu menu = this.item as CommandBarMenu;
				if (menu != null)
				{
					this.MenuItems.Clear();

					Size imageSize = GetImageSize(menu.Items);

					int visibleItemCount = 0;
					foreach (CommandBarItem item in menu.Items)
					{
						this.MenuItems.Add(new MenuBarItem(item, imageSize, font, mnemonics));
						visibleItemCount += (item.IsVisible) ? 1 : 0;
					}

					this.Enabled = (visibleItemCount == 0) ? false : this.item.IsEnabled;
				}
				else
				{
					this.Enabled = this.item.IsEnabled;
				}

				this.Visible = this.item.IsVisible;
			}

			protected override void OnClick(EventArgs e)
			{
				base.OnClick(e);

				CommandBarControl control = this.item as CommandBarControl;
				if (control != null)
				{
					control.PerformClick(EventArgs.Empty);
				}
			}

			protected override void OnSelect(EventArgs e)
			{
				CommandBarContextMenu contextMenu = this.GetContextMenu() as CommandBarContextMenu;
				if (contextMenu == null)
				{
					throw new NotSupportedException();
				}

				contextMenu.SelectedMenuItem = this;
				base.OnSelect(e);
			}

			private bool IsFlatMenu
			{
				get
				{
					if (Environment.OSVersion.Version < new Version(5, 1, 0, 0))
					{
						return false;
					}

					int data = 0;
					NativeMethods.SystemParametersInfo(NativeMethods.SPI_GETFLATMENU, 0, ref data, 0);
					return (data != 0);
				}
			}

			protected override void OnMeasureItem(MeasureItemEventArgs e)
			{
				base.OnMeasureItem(e);
				Graphics graphics = e.Graphics;

				if (item is CommandBarSeparator)
				{
					e.ItemWidth = 0;
					e.ItemHeight = SystemInformation.MenuHeight / 2;
				}
				else
				{
					Size size = new Size(0, 0);
					size.Width += 3 + imageSize.Width + 3 + 3 + 1 + 3 + imageSize.Width + 3;
					size.Height += 3 + imageSize.Height + 3;

					string text = item.Text;

					CommandBarButtonBase buttonBase = item as CommandBarButtonBase;
					if ((buttonBase != null) && (buttonBase.Shortcut != Keys.None))
					{
						text += TypeDescriptor.GetConverter(typeof(Keys)).ConvertToString(null, CultureInfo.InvariantCulture, buttonBase.Shortcut);
					}

					using (TextGraphics textGraphics = new TextGraphics(graphics))
					{
						Size textSize = textGraphics.MeasureText(text, font);
						size.Width += textSize.Width;
						textSize.Height += 8;
						if (textSize.Height > size.Height)
						{
							size.Height = textSize.Height;
						}
					}

					e.ItemWidth = size.Width;
					e.ItemHeight = size.Height;
				}
			}

			protected override void OnDrawItem(DrawItemEventArgs e)
			{
				base.OnDrawItem(e);

				Graphics graphics = e.Graphics;
				Rectangle bounds = e.Bounds;

				bool selected = ((e.State & DrawItemState.Selected) != 0);
				bool disabled = ((e.State & DrawItemState.Disabled) != 0);

				if (item is CommandBarSeparator)
				{
					Rectangle r = new Rectangle(bounds.X, bounds.Y + (bounds.Height / 2), bounds.Width, bounds.Height);
					ControlPaint.DrawBorder3D(graphics, r, Border3DStyle.Etched, Border3DSide.Top);
				}
				else
				{
					this.DrawImage(graphics, bounds, selected, disabled);

					int width = 6 + imageSize.Width;
					this.DrawBackground(graphics, new Rectangle(bounds.X + width, bounds.Y, bounds.Width - width, bounds.Height), selected);

					using (TextGraphics textGraphics = new TextGraphics(graphics))
					{
						if ((this.Text != null) && (this.Text.Length != 0))
						{
							Size size = textGraphics.MeasureText(this.Text, font);
							Point point = new Point();
							point.X = bounds.X + 3 + imageSize.Width + 6;
							point.Y = bounds.Y + ((bounds.Height - size.Height) / 2);
							this.DrawText(textGraphics, this.Text, point, selected, disabled);
						}

						CommandBarButtonBase buttonBase = item as CommandBarButtonBase;
						if ((buttonBase != null) && (buttonBase.Shortcut != Keys.None))
						{
							string text = TypeDescriptor.GetConverter(typeof(Keys)).ConvertToString(null, CultureInfo.InvariantCulture, buttonBase.Shortcut);

							Size size = textGraphics.MeasureText(text, font);

							Point point = new Point();
							point.X = bounds.X + bounds.Width - 3 - imageSize.Width - 3 - size.Width;
							point.Y = bounds.Y + ((bounds.Height - size.Height) / 2);
							this.DrawText(textGraphics, text, point, selected, disabled);
						}
					}
				}
			}

			private void DrawBackground(Graphics graphics, Rectangle rectangle, bool selected)
			{
				Brush background = (selected) ? SystemBrushes.Highlight : SystemBrushes.Menu;
				graphics.FillRectangle(background, rectangle);
			}

			private void DrawCheck(Graphics graphics, Rectangle rectangle, Color color)
			{
				Bitmap bitmap = new Bitmap(rectangle.Width, rectangle.Height);

				Graphics bitmapGraphics = Graphics.FromImage(bitmap);
				ControlPaint.DrawMenuGlyph(bitmapGraphics, 0, 0, rectangle.Width, rectangle.Height, MenuGlyph.Checkmark);
				bitmapGraphics.Flush();

				bitmap.MakeTransparent(Color.White);

				ImageAttributes attributes = new ImageAttributes();
				ColorMap colorMap = new ColorMap();
				colorMap.OldColor = Color.Black;
				colorMap.NewColor = color;
				attributes.SetRemapTable(new ColorMap[] { colorMap });

				graphics.DrawImage(bitmap, rectangle, 0, 0, rectangle.Width, rectangle.Height, GraphicsUnit.Pixel, attributes);
			}

			private void DrawImage(Graphics graphics, Rectangle bounds, bool selected, bool disabled)
			{
				Rectangle rectangle = new Rectangle(bounds.X, bounds.Y, imageSize.Width + 6, bounds.Height);

				Size checkSize = SystemInformation.MenuCheckSize;
				Rectangle checkRectangle = new Rectangle(bounds.X + 1 + ((imageSize.Width + 6 - checkSize.Width) / 2), bounds.Y + ((bounds.Height - checkSize.Height) / 2), checkSize.Width, checkSize.Height);

				CommandBarCheckBox checkBox = item as CommandBarCheckBox;

				if (this.IsFlatMenu)
				{
					DrawBackground(graphics, rectangle, selected);

					if ((checkBox != null) && (checkBox.IsChecked))
					{
						int height = bounds.Height - 2;
						graphics.DrawRectangle(SystemPens.Highlight, new Rectangle(bounds.X + 1, bounds.Y + 1, imageSize.Width + 3, height - 1));
						graphics.FillRectangle(SystemBrushes.Menu, new Rectangle(bounds.X + 2, bounds.Y + 2, imageSize.Width + 2, height - 2));
					}

					Image image = item.Image;
					if (image != null)
					{
						Point point = new Point(bounds.X + 3, bounds.Y + ((bounds.Height - image.Height) / 2));
						NativeMethods.DrawImage(graphics, image, point, disabled);
					}
					else
					{
						if ((checkBox != null) && (checkBox.IsChecked))
						{
							Color color = (disabled ? SystemColors.GrayText : SystemColors.MenuText);
							this.DrawCheck(graphics, checkRectangle, color);
						}
					}
				}
				else
				{
					Image image = item.Image;
					if (image == null)
					{
						this.DrawBackground(graphics, rectangle, selected);

						if ((checkBox != null) && (checkBox.IsChecked))
						{
							Color color = (disabled ? (selected ? SystemColors.GrayText : SystemColors.ControlDark) : (selected ? SystemColors.HighlightText : SystemColors.MenuText));
							this.DrawCheck(graphics, checkRectangle, color);
						}
					}
					else
					{
						DrawBackground(graphics, rectangle, false);
						if ((checkBox != null) && (checkBox.IsChecked))
						{
							ControlPaint.DrawBorder3D(graphics, rectangle, Border3DStyle.SunkenOuter);
						}
						else if (selected)
						{
							ControlPaint.DrawBorder3D(graphics, rectangle, Border3DStyle.RaisedInner);
						}

						Point point = new Point(bounds.X + 3, bounds.Y + ((bounds.Height - image.Height) / 2));
						NativeMethods.DrawImage(graphics, image, point, disabled);
					}
				}
			}

			private void DrawText(TextGraphics textGraphics, string text, Point point, bool selected, bool disabled)
			{
				Color color = (disabled ? (selected ? SystemColors.GrayText : SystemColors.ControlDark) : (selected ? SystemColors.HighlightText : SystemColors.MenuText));

				if ((!this.IsFlatMenu) && (disabled) && (!selected))
				{
					textGraphics.DrawText(text, new Point(point.X + 1, point.Y + 1), font, SystemColors.ControlLightLight);
				}

				textGraphics.DrawText(text, point, font, color);
			}
		}
	}
}
