using System;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Text;
using SIL.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DrawOfficeXPMenuHelper : IMenuDrawingHelper
	{
		#region Private Attributes
		private static int BITMAP_SIZE = 16;
		private static int STRIPE_WIDTH = BITMAP_SIZE + 8;
		private static int TEXT_LEFT = STRIPE_WIDTH + 6;
		private static int s_itemHeight;

		private MenuItem m_menuItem;
		private Graphics m_graphics;
		private ImageList m_imageList;

		private readonly SolidBrush m_brush = new SolidBrush(Color.Empty);
		private readonly Pen m_pen = new Pen(Color.Empty, 1);
		#endregion

		#region Disposable stuff
		#if DEBUG
		/// <summary/>
		~DrawOfficeXPMenuHelper()
		{
			Dispose(false);
		}
		#endif

		/// <summary/>
		public bool IsDisposed
		{
			get;
			private set;
		}

		/// <summary/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType() + ". ****** ");
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects
				m_brush.Dispose();
				m_pen.Dispose();
			}
			IsDisposed = true;
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicateing whether or not the <c>MenuItem</c> has a shortcut
		/// selected, and the shortcut has been selected for show.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool HasShortcut
		{
			get {return (m_menuItem.ShowShortcut && m_menuItem.Shortcut != Shortcut.None);}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether or not the <c>MenuItem</c> is a seperator.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsSeperator
		{
			get {return (m_menuItem.Text == "-");}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether or not the <c>MenuItem</c> is a top-level menu that
		/// is sited directly on a <c>MainMenu</c> control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsTopLevel
		{
			get {return (m_menuItem.Parent is MainMenu);}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value that formats the <c>MenuItem</c> and is the shortcut selection as a
		/// displayable text string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ShortcutText
		{
			get
			{
				if (m_menuItem.ShowShortcut == true && m_menuItem.Shortcut != Shortcut.None)
				{
					Keys keys = (Keys) m_menuItem.Shortcut;

					return Convert.ToChar(Keys.Tab) +
						System.ComponentModel.TypeDescriptor.GetConverter(
						keys.GetType()).ConvertToString(keys);
				}

				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private Rectangle GetMenuRect
		{
			get
			{
				if (m_menuItem.Parent == null)
					return Rectangle.Empty;

				Win32.RECT rc = new Win32.RECT();

				Win32.GetMenuItemRect(IntPtr.Zero, m_menuItem.Parent.Handle,
					(uint)m_menuItem.Index, ref rc);

				return Rectangle.FromLTRB(rc.left, rc.top, rc.right, rc.bottom);
			}
		}
		#endregion

		#region Helper Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the menu's image from the image list.
		/// </summary>
		/// <param name="imageIndex"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private Image MenuImage(int imageIndex)
		{
			if (m_imageList != null && imageIndex >= 0 && imageIndex < m_imageList.Images.Count)
				return m_imageList.Images[imageIndex];

			return null;
		}

		#endregion

		#region IMenuDrawingHelper Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		ImageList IMenuDrawingHelper.ImageList
		{
			get {return m_imageList;}
			set {m_imageList = value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		MenuItem IMenuDrawingHelper.MenuItem
		{
			set {m_menuItem = value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		Graphics IMenuDrawingHelper.Graphics
		{
			set	{m_graphics = value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Based on the menu item text, and the <c>SystemInformation.SmallIconSize,</c>
		/// performs a calculation to determine the correct <c>MenuItem</c> height.
		/// </summary>
		/// <returns>Returns an <c>int</c> value that contains the calculated height of the
		/// menu item.</returns>
		/// ------------------------------------------------------------------------------------
		int IMenuDrawingHelper.CalcHeight()
		{
			if (IsSeperator)
				return 4;

			s_itemHeight = SystemInformation.MenuHeight + 4;
			return s_itemHeight;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Based on the menu item text, and the <c>SystemInformation.SmallIconSize,</c>
		/// performs a calculation to determine the correct <c>MenuItem</c> width.
		/// </summary>
		/// <returns>Returns an <c>int</c> value that contains the calculated width of the
		/// menu item.</returns>
		/// ------------------------------------------------------------------------------------
		int IMenuDrawingHelper.CalcWidth()
		{
			if (IsSeperator)
				return 8;

			using (StringFormat sf = new StringFormat())
			{
				sf.HotkeyPrefix = System.Drawing.Text.HotkeyPrefix.Show;

				// Set the menu width by measuring the string, icon and buffer spaces
				int textWidth = (int)m_graphics.MeasureString(m_menuItem.Text,
					SystemInformation.MenuFont, 1000, sf).Width;

				if (IsTopLevel)
					return textWidth - 5;

				int shortcutWidth = 0;

				if (m_menuItem.Shortcut != Shortcut.None)
				{
					shortcutWidth = (int)m_graphics.MeasureString(ShortcutText,
						SystemInformation.MenuFont, 1000, sf).Width;
				}

				return Math.Max(160, textWidth + shortcutWidth + 50);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Draws a normal menu item including any related icons, checkboxes,
		/// menu text, shortcuts text, and parent/submenu arrows.
		/// </summary>
		/// <param name="bounds">a <c>Rectangle</c> that holds the drawing canvas boundaries
		/// </param>
		/// <param name="state">True/False if the menu item is currently selected</param>
		/// <param name="imageIndex">the image index of the menu icon to draw, defaults to -1
		/// </param>
		/// ------------------------------------------------------------------------------------
		void IMenuDrawingHelper.DrawMenu(Rectangle bounds, DrawItemState state, int imageIndex)
		{
			Image image = MenuImage(imageIndex);
			bool selected = (state & DrawItemState.Selected) > 0;

			if (m_menuItem is LabelMenuItem)
			{
				DrawLabelMenuItem(bounds, image);
				return;
			}

			DrawBackground(bounds, state, selected);

			if (image != null)
				DrawImage(bounds, image, selected, m_menuItem.Checked);
			else if (m_menuItem.Checked)
				DrawCheckmark(bounds, selected);

			if (!IsSeperator || m_menuItem is LabelMenuItem)
				DrawMenuText(bounds, state);
			else
				DrawSeparator(bounds);
		}

		#endregion

		#region Drawing Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Draws the background for a label menu.
		/// </summary>
		/// <param name="bounds"></param>
		/// <param name="image"></param>
		/// ------------------------------------------------------------------------------------
		private void DrawLabelMenuItem(Rectangle bounds, Image image)
		{
			// Draw label item's background
			m_brush.Color = ColorUtil.ControlColor;
			m_graphics.FillRectangle(m_brush, bounds);
			m_graphics.DrawLine(SystemPens.ControlDark, bounds.X, bounds.Bottom - 1,
				bounds.Right, bounds.Bottom - 1);

			using (StringFormat sf = new StringFormat())
			{
				sf.FormatFlags |= StringFormatFlags.DirectionRightToLeft;
				sf.Alignment = StringAlignment.Center;
				sf.LineAlignment = StringAlignment.Center;

				// Draw the label item's image if there is one.
				if (image != null)
					DrawImage(bounds, image, false, false);

				// Draw the label item's text
				using (Font font = new Font(SystemInformation.MenuFont, FontStyle.Bold))
				{
					m_brush.Color = SystemColors.MenuText;
					m_graphics.DrawString(m_menuItem.Text, font, m_brush, bounds, sf);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="bounds"></param>
		/// <param name="state"></param>
		/// <param name="selected"></param>
		/// ------------------------------------------------------------------------------------
		private void DrawBackground(Rectangle bounds, DrawItemState state, bool selected)
		{
			if ((!selected && (state & DrawItemState.HotLight) == 0))
			{
				DrawBgUnselectedNotHot(bounds);
			}
			else
			{
				if (IsTopLevel && selected)
					DrawBgSelectedTopLevel(bounds);
				else
				{
					if (m_menuItem.Enabled)
						DrawBgHotOrSelectedEnabled(bounds);
					else
						DrawBgHotOrSelectedDisabled(bounds);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Paints the menu item's background when it is enabled and hot (i.e. mouse is over
		/// it) or selected.
		/// </summary>
		/// <param name="bounds"></param>
		/// ------------------------------------------------------------------------------------
		private void DrawBgHotOrSelectedEnabled(Rectangle bounds)
		{
			m_brush.Color = ColorUtil.SelectionColor;
			m_pen.Color = ColorUtil.BorderColor;
			m_graphics.FillRectangle(m_brush, bounds);
			m_graphics.DrawRectangle(m_pen, bounds.X, bounds.Y,	bounds.Width - 1,
				bounds.Height - 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Paints the menu item's background when it is disabled and hot (i.e. mouse is over
		/// it) or selected.
		/// </summary>
		/// <remarks>The way the background is painted is different depending upon whether or
		/// not the menu item is selected via the keyboard or the mouse. When selected via the
		/// keyboard, the disabled item is painted with a border. If it's dragged over with the
		/// mouse, then no border is painted.</remarks>
		/// <param name="bounds"></param>
		/// ------------------------------------------------------------------------------------
		private void DrawBgHotOrSelectedDisabled(Rectangle bounds)
		{
			// Fill the entire menu item area (including the image stripe on the left) with
			// the background color.
			m_brush.Color = ColorUtil.BackgroundColor;
			m_graphics.FillRectangle(m_brush, bounds);

			// Determine whether or not the mouse pointer is over the menu item.
			if (GetMenuRect.Contains(Control.MousePosition))
			{
				// The mouse is over the menu item so don't draw the border, just fill in
				// the image stripe background color.
				bounds.Width = STRIPE_WIDTH;
				m_brush.Color = ColorUtil.ControlColor;
				m_graphics.FillRectangle(m_brush, bounds);
			}
			else
			{
				// The mouse is not over the menu item so we assume this item has been
				// selected via the keyboard. Therefore, draw the border around the menu item.
				m_pen.Color = ColorUtil.BorderColor;
				m_graphics.DrawRectangle(m_pen, bounds.X, bounds.Y,	bounds.Width - 1,
					bounds.Height - 1);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Draw the background for top level menu items.
		/// </summary>
		/// <param name="bounds"></param>
		/// ------------------------------------------------------------------------------------
		private void DrawBgSelectedTopLevel(Rectangle bounds)
		{
			bounds.Inflate(-1, 0);
			m_brush.Color = ColorUtil.ControlColor;
			m_graphics.FillRectangle(m_brush, bounds);
			ControlPaint.DrawBorder3D(m_graphics, bounds.Left, bounds.Top, bounds.Width,
				bounds.Height, Border3DStyle.Flat,
				Border3DSide.Top | Border3DSide.Left | Border3DSide.Right);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Paints the menu item's background when it's not selected and when it's not hot
		/// (i.e. the mouse isn't over it).
		/// </summary>
		/// <param name="bounds"></param>
		/// ------------------------------------------------------------------------------------
		private void DrawBgUnselectedNotHot(Rectangle bounds)
		{
			if (IsTopLevel)
				m_graphics.FillRectangle(SystemBrushes.Control, bounds);
			else
			{
				m_brush.Color = ColorUtil.BackgroundColor;
				m_graphics.FillRectangle(m_brush, bounds);
				m_brush.Color = ColorUtil.ControlColor;
				bounds.Width = STRIPE_WIDTH;
				m_graphics.FillRectangle(m_brush, bounds);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="bounds"></param>
		/// ------------------------------------------------------------------------------------
		public void DrawSeparator(Rectangle bounds)
		{
			int y = bounds.Y + bounds.Height / 2;
			m_graphics.DrawLine(SystemPens.ControlDark, bounds.X + TEXT_LEFT + 2, y,
				bounds.X + bounds.Width - 2, y);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="bounds"></param>
		/// <param name="selected"></param>
		/// ------------------------------------------------------------------------------------
		public void DrawCheckmark(Rectangle bounds, bool selected)
		{
			int checkTop = bounds.Top + (s_itemHeight - BITMAP_SIZE) / 2;
			int checkLeft = bounds.Left + (STRIPE_WIDTH - BITMAP_SIZE) / 2;
			Rectangle rc = new Rectangle(checkLeft, checkTop, BITMAP_SIZE, BITMAP_SIZE);

			DrawCheckedItemsBackground(bounds);

			// Draw the check mark.
			StringFormat sf = StringFormat.GenericTypographic;
			sf.Alignment = StringAlignment.Center;
			sf.LineAlignment = StringAlignment.Center;
			m_brush.Color = SystemColors.WindowText;
			m_graphics.DrawString(MenuExtender.CheckMarkGlyph, MenuExtender.CheckFont,
				m_brush, rc, sf);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="bounds"></param>
		/// <param name="image"></param>
		/// <param name="selected"></param>
		/// <param name="fChecked"></param>
		/// ------------------------------------------------------------------------------------
		private void DrawImage(Rectangle bounds, Image image, bool selected, bool fChecked)
		{
			int imgTop = bounds.Top + (s_itemHeight - BITMAP_SIZE) / 2;
			int imgLeft = bounds.Left + (STRIPE_WIDTH - BITMAP_SIZE) / 2;

			if (m_menuItem.Enabled)
			{
				if (fChecked)
					DrawCheckedItemsBackground(bounds);

				if (!selected)
					m_graphics.DrawImage(image, imgLeft, imgTop);
				else
				{
					// When the menu item is selected, first, draw a disabled version of
					// the image that serves as a shadow for the enabled version of the image.
					m_graphics.DrawImage(
						ColorUtil.MakeDisabledImage(image, ColorUtil.ControlColor, true),
							imgLeft + 1, imgTop + 1);

					// Now draw the image.
					m_graphics.DrawImage(image, imgLeft - 1, imgTop - 1);
				}
			}
			else
			{
				m_graphics.DrawImage(
					ColorUtil.MakeDisabledImage(image, ColorUtil.ControlColor, false),
					imgLeft, imgTop);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fill the background color of the check.
		/// </summary>
		/// <param name="bounds"></param>
		/// ------------------------------------------------------------------------------------
		private void DrawCheckedItemsBackground(Rectangle bounds)
		{
			int top = bounds.Top + (s_itemHeight - BITMAP_SIZE) / 2;
			int left = bounds.Left + (STRIPE_WIDTH - BITMAP_SIZE) / 2;
			Rectangle rc = new Rectangle(left - 3, top - 3, BITMAP_SIZE + 5, BITMAP_SIZE + 5);

			// Fill the background for the check mark (or checked image).
			m_brush.Color = ColorUtil.SelectionColor;
			m_graphics.FillRectangle(m_brush, rc);

			// Draw border around check mark (or checked image).
			m_pen.Color = ColorUtil.BorderColor;
			m_graphics.DrawRectangle(m_pen, rc);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="bounds"></param>
		/// <param name="state"></param>
		/// ------------------------------------------------------------------------------------
		private void DrawMenuText(Rectangle bounds, DrawItemState state)
		{
			using (StringFormat sf = new StringFormat())
			{
				sf.HotkeyPrefix = ((state & DrawItemState.NoAccelerator) > 0) ?
					HotkeyPrefix.Hide : HotkeyPrefix.Show;

				string menuText = m_menuItem.Text;

				if (IsTopLevel)
				{
					int index = menuText.IndexOf("&");
					if (index > -1)
						menuText = menuText.Remove(index, 1);
				}

				int topGap = (IsTopLevel ? 2 : 4);
				int y = bounds.Top + topGap;

				int textWidth =
					(int)(m_graphics.MeasureString(menuText, SystemInformation.MenuFont).Width);

				int x = IsTopLevel ?
					bounds.Left + (bounds.Width - textWidth) / 2 : bounds.X + TEXT_LEFT;

				m_brush.Color = (m_menuItem.Enabled ? SystemColors.MenuText : SystemColors.GrayText);
				m_graphics.DrawString(menuText, SystemInformation.MenuFont, m_brush, x, y, sf);

				if (!IsTopLevel)
				{
					sf.FormatFlags |= StringFormatFlags.DirectionRightToLeft;
					m_graphics.DrawString(ShortcutText, SystemInformation.MenuFont, m_brush,
						bounds.Width - 10 , bounds.Top + topGap, sf);
				}
			}
		}
		#endregion
	}
}
