using System;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for DrawRetroMenuHelper.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DrawStandardMenuHelper : IMenuDrawingHelper
	{
		#region Private Attributes
		// Some pre-defined buffer values for putting space between
		// icon, menutext, seperator text, and submenu arrow indicators
		private const int SEPERATOR_HEIGHT = 8;
		private const int SBORDER_WIDTH = 1;
		private const int BORDER_SIZE = SBORDER_WIDTH * 2;
		private const int SBUFFER_WIDTH = 5;
		private const int LBUFFER_WIDTH = 15;

		// Gap between the very left edge of the menu and the beginning of the menu text.
		private int TEXT_LEFT = SystemInformation.SmallIconSize.Width + 10;

		// Gap between menu text and shortcut text
		private const int SHORTCUT_GAP_WIDTH = 20;
		private int IMAGE_WIDTH = SystemInformation.SmallIconSize.Width;
		private int IMAGE_HEIGHT = SystemInformation.SmallIconSize.Height;

		// holds the local instances of the MenuItem and Graphics
		// objects passed in through the Constructor
		private MenuItem m_menuItem;
		private Graphics m_graphics;
		private ImageList m_imageList;
		private readonly Font m_fntMenu = SystemInformation.MenuFont;
		private readonly SolidBrush m_brush = new SolidBrush(Color.Empty);
		private readonly Pen m_pen = new Pen(Color.Empty, 1);
		#endregion

		#region Disposable stuff
		#if DEBUG
		/// <summary/>
		~DrawStandardMenuHelper()
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
		internal bool HasShortcut
		{
			get {return (m_menuItem.ShowShortcut && m_menuItem.Shortcut != Shortcut.None);}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether or not the <c>MenuItem</c> is a seperator.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal bool IsSeperator
		{
			get {return (m_menuItem.Text == "-");}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether or not the <c>MenuItem</c> is a top-level menu that
		/// is sited directly on a <c>MainMenu</c> control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal bool IsTopLevel
		{
			get {return (m_menuItem.Parent is MainMenu);}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value that formats the <c>MenuItem</c> and is the shortcut selection as a
		/// displayable text string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal string ShortcutText
		{
			get
			{
				if (m_menuItem.ShowShortcut == true && m_menuItem.Shortcut != Shortcut.None)
				{
					Keys keys = (Keys)m_menuItem.Shortcut;

					return Convert.ToChar(Keys.Tab) +
						System.ComponentModel.TypeDescriptor.GetConverter(
						keys.GetType()).ConvertToString(keys);
				}

				return null;
			}
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
			// If the menu is a seperator, then return a fixed height otherwise calculate the
			// menu size based on the system font and smalliconsize calculations (with some
			// added buffer values)
			if (m_menuItem.Text == "-")
				return SEPERATOR_HEIGHT;
			else
			{
				// Depending on which is longer, set the menu height to either the icon, or the
				// system menu font
				if (m_fntMenu.Height > SystemInformation.SmallIconSize.Height)
					return m_fntMenu.Height + BORDER_SIZE;
				else
					return SystemInformation.SmallIconSize.Height + BORDER_SIZE;
			}
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
			// Prepare string formatting used for rendering menu caption
			using (StringFormat sf = new StringFormat())
			{
				sf.HotkeyPrefix = System.Drawing.Text.HotkeyPrefix.Show;

				// Set the menu width by measuring the string, icon and buffer spaces
				int menuWidth = (int)m_graphics.MeasureString(m_menuItem.Text,
					m_fntMenu, 1000, sf).Width;

				int shortcutWidth = (int)m_graphics.MeasureString(ShortcutText,
					m_fntMenu, 1000, sf).Width;

				// if a top-level menu, no image support
				if (IsTopLevel)
					return menuWidth;
				else
					return TEXT_LEFT + menuWidth + SHORTCUT_GAP_WIDTH + shortcutWidth;
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
			bool selected = (state & DrawItemState.Selected) > 0;

			DrawBackground(bounds, selected);

			if (IsSeperator)
				DrawSeperator(bounds);
			else
			{
				DrawMenuText(bounds, selected);

				if (m_menuItem.Checked)
					DrawCheckBox(bounds);
				else if (m_imageList != null && imageIndex > -1)
					DrawImage(m_imageList.Images[imageIndex], bounds);
			}
		}

		#endregion

		#region Private Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Draws the <c>MenuItem</c> background.
		/// </summary>
		/// <param name="bounds">a <c>Rectangle</c> that holds the painting canvas boundaries
		/// </param>
		/// <param name="selected">True/False if the menu item is currently selected</param>
		/// ------------------------------------------------------------------------------------
		private void DrawBackground(Rectangle bounds, bool selected)
		{
			// If selected then paint the menu as highlighted, otherwise use the default menu
			// brush.
			m_graphics.FillRectangle(selected ? SystemBrushes.Highlight : SystemBrushes.Menu,
				bounds);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Draws a menu seperator.
		/// </summary>
		/// <param name="bounds">a <c>Rectangle</c> that holds the drawing canvas boundaries.
		/// </param>
		/// ------------------------------------------------------------------------------------
		private void DrawSeperator(Rectangle bounds)
		{
			m_pen.Color = SystemColors.ControlDark;

			// calculate seperator boundaries
			int xLeft = bounds.Left + TEXT_LEFT;
			int xRight = xLeft + bounds.Width;
			int yCenter = bounds.Top  + (bounds.Height / 2);

			// draw a seperator line and return
			m_graphics.DrawLine(m_pen, xLeft, yCenter, xRight, yCenter);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Draws the text for an ownerdrawn <c>MenuItem</c>.
		/// </summary>
		/// <param name="bounds">A <c>Rectangle</c> that holds the drawing area boundaries
		/// </param>
		/// <param name="selected">True/False whether the menu item is currently selected.
		/// </param>
		/// ------------------------------------------------------------------------------------
		private void DrawMenuText(Rectangle bounds, bool selected)
		{
			if (m_menuItem.Enabled == false)
				m_brush.Color = SystemColors.GrayText;
			else
				m_brush.Color = (selected ? SystemColors.HighlightText : SystemColors.MenuText);

			// Draw the menu text
			using (StringFormat sf = new StringFormat(StringFormat.GenericTypographic))
			{
				sf.HotkeyPrefix = System.Drawing.Text.HotkeyPrefix.Show;
				sf.Alignment = StringAlignment.Near;

				m_graphics.DrawString(m_menuItem.Text, m_fntMenu, m_brush, bounds.Left +
					TEXT_LEFT, bounds.Top + ((bounds.Height - m_fntMenu.Height) / 2), sf);

				// If the menu has a shortcut, then also  draw the shortcut right aligned.
				if (!IsTopLevel || !HasShortcut)
				{
					sf.Alignment = StringAlignment.Far;
					m_graphics.DrawString(ShortcutText, m_fntMenu, m_brush,
						bounds.Width - LBUFFER_WIDTH,
						bounds.Top + ((bounds.Height - m_fntMenu.Height) / 2), sf);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Draws a checked item next to a <c>MenuItem</c>.
		/// </summary>
		/// <param name="bounds">a <c>Rectangle</c> that identifies the drawing area boundaries
		/// </param>
		/// ------------------------------------------------------------------------------------
		private void DrawCheckBox(Rectangle bounds)
		{
			Rectangle rc = new Rectangle(bounds.Left + SBORDER_WIDTH, bounds.Top +
				((bounds.Height - IMAGE_HEIGHT) / 2), IMAGE_WIDTH, IMAGE_HEIGHT);

			// Fill the check box area.
			m_brush.Color = Color.FromArgb(75, Color.White);
			m_graphics.FillRectangle(m_brush, rc);

			// Draw the check mark.
			using (StringFormat sf = new StringFormat(StringFormat.GenericTypographic))
			{
				sf.Alignment = StringAlignment.Center;
				sf.LineAlignment = StringAlignment.Center;
				m_brush.Color =
					(m_menuItem.Enabled ? SystemColors.ControlText : SystemColors.GrayText);
				m_graphics.DrawString(MenuExtender.CheckMarkGlyph, MenuExtender.CheckFont,
					m_brush, rc, sf);

				// Draw the dark lines to give the indented 3D look.
				rc.Width--;
				rc.Height--;
				m_pen.Color = SystemColors.ControlDark;
				m_graphics.DrawRectangle(m_pen, rc);

				// Draw the light lines to give the indented 3D look.
				m_pen.Color = SystemColors.ControlLightLight;
				m_graphics.DrawLine(m_pen, rc.Right, rc.Y, rc.Right, rc.Bottom);
				m_graphics.DrawLine(m_pen, rc.X, rc.Bottom, rc.Right, rc.Bottom);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Draws a provided image onto the <c>MenuItem</c>.
		/// </summary>
		/// <param name="menuImage">an <c>Image</c> to paint on the menu</param>
		/// <param name="bounds">a <c>Rectangle</c> that holds the drawing space boundaries
		/// </param>
		/// ------------------------------------------------------------------------------------
		private void DrawImage(Image menuImage, Rectangle bounds)
		{
			// If the menu item is enabled, then draw the image normally otherwise draw it as
			// disabled.
			if (m_menuItem.Enabled == true)
			{
				m_graphics.DrawImage(menuImage, bounds.Left + SBORDER_WIDTH,
					bounds.Top + ((bounds.Height - IMAGE_HEIGHT) / 2), IMAGE_WIDTH,
					IMAGE_HEIGHT);
			}
			else
			{
				ControlPaint.DrawImageDisabled(m_graphics, menuImage,
					bounds.Left + SBORDER_WIDTH,
					bounds.Top + ((bounds.Height - IMAGE_HEIGHT) / 2), SystemColors.Menu);
			}
		}

		#endregion
	}
}
