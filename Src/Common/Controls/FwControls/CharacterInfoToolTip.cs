using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Media;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;
using System.Drawing.Drawing2D;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.CoreImpl;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Provides an owner-drawn class that displays Unicode values for a text string and the
	/// ICU character name when the text string is a single character.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class CharacterInfoToolTip : ToolTip
	{
		/// <summary>
		/// Handler for the UnicodeValueTextConstructed event.
		/// </summary>
		/// <param name="sender">Tooltip firing the event</param>
		/// <param name="ctrl">Control for which the tooltip is being shown.</param>
		/// <param name="text">String containing comma-delimited Unicode values.</param>
		public delegate void UnicodeValueTextConstructedHandler(object sender, Control ctrl, ref string text);

		/// <summary>Event fired after the Unicode value string is constructed by the tooltip,
		/// but before it's displayed. This gives subscribers to the event an option to mofify
		/// the text before the tooltip is shown.</summary>
		public event UnicodeValueTextConstructedHandler UnicodeValueTextConstructed;

		private Control m_ctrl;
		private Font m_fntChar = null;
		private Font m_fntTitle;
		private Font m_fntText;
		private string m_text;
		private Rectangle m_rcText;
		private Rectangle m_rcTitle;
		private bool m_showMissingGlyphIcon = false;
		private Image m_missingGlyphIcon = Properties.Resources.kimidMissingGlyph;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="CharacterInfoToolTip"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public CharacterInfoToolTip()
		{
			m_fntText = SystemFonts.IconTitleFont;
			m_fntTitle = new Font(m_fntText, FontStyle.Bold);

			ReshowDelay = 1000;
			AutoPopDelay = 5000;
			OwnerDraw = true;
			Popup += HandlePopup;
			Draw += HandleDraw;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up the title font we created.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (disposing && m_fntTitle != null)
			{
				m_fntTitle.Dispose();
				m_fntTitle = null;
				m_fntText = null;
			}

			base.Dispose(disposing);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the control over which the tooltip is being displayed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Control Control
		{
			get { return m_ctrl; }
			set { m_ctrl = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the character font.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Font CharacterFont
		{
			get { return m_fntChar; }
			set { m_fntChar = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows tooltip for the specified character.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Show(string chr)
		{
			Show(m_ctrl, chr, m_fntChar);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows tooltip for the specified character at the specified coordinate relative
		/// to the grid control specified at construction.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Show(string chr, Font fnt)
		{
			Show(m_ctrl, chr, fnt);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows tooltip for the specified control and character.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Show(Control ctrl, string chr)
		{
			Show(ctrl, chr, m_fntChar);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows tooltip for the specified character at the specified coordinate relative
		/// to the grid control specified at construction.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Show(Control ctrl, string chr, Font fnt)
		{
			m_ctrl = ctrl;

			if (m_ctrl == null)
			{
				SystemSounds.Beep.Play();
				return;
			}

			if (m_fntChar == null)
				m_fntChar = m_ctrl.Font;

			Hide();

			if (!string.IsNullOrEmpty(chr))
			{
				BuildToolTipContent(chr);
				SetToolTip(m_ctrl, m_text);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Hides this instance.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Hide()
		{
			if (m_ctrl != null)
			{
				SetToolTip(m_ctrl, null);
				Hide(m_ctrl);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Builds the content of the tool tip.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void BuildToolTipContent(string chr)
		{
			m_text = (chr.Length == 1 ?
				Properties.Resources.kstidChrGridCodepoint :
				Properties.Resources.kstidChrGridCodepoints);

			// Get the string containing the character codepoints.
			m_text = string.Format(m_text, StringUtils.CharacterCodepoints(chr));

			if (UnicodeValueTextConstructed != null)
				UnicodeValueTextConstructed(this, m_ctrl, ref m_text);

			string name = Icu.GetPrettyICUCharName(chr);

			// Get the name of the character if its length is 1.
			if (!string.IsNullOrEmpty(name))
			{
				name = string.Format(Properties.Resources.kstidChrGridName, name);
				m_text += (Environment.NewLine + name);
			}

			m_showMissingGlyphIcon = !Win32.AreCharGlyphsInFont(chr, m_fntChar);

			// If the glyphs for the codepoints are not present in the character
			// grid's font, then use a heading telling the user that.
			ToolTipTitle = (m_showMissingGlyphIcon ?
				Properties.Resources.kstidChrGridMissingGlyphHdg :
				Properties.Resources.kstidChrGridNormalHdg);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the popup.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void HandlePopup(object sender, PopupEventArgs e)
		{
			using (Graphics g = Graphics.FromHwnd(e.AssociatedWindow.Handle))
			{
				Size sz1 = TextRenderer.MeasureText(g, ToolTipTitle, m_fntTitle);
				Size sz2 = TextRenderer.MeasureText(g, m_text, m_fntText);

				m_rcTitle = new Rectangle(10, 10, sz1.Width, sz1.Height);
				m_rcText = new Rectangle(10, m_rcTitle.Bottom + 15, sz2.Width, sz2.Height);

				if (m_showMissingGlyphIcon)
				{
					m_rcTitle.X += (m_missingGlyphIcon.Width + 5);
					sz1.Width += (m_missingGlyphIcon.Width + 5);
					sz1.Height = Math.Max(sz1.Height, m_missingGlyphIcon.Height);
				}

				sz1.Width = Math.Max(sz1.Width, sz2.Width) + 20;
				sz1.Height += (sz2.Height + 35);
				e.ToolTipSize = sz1;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the draw.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void HandleDraw(object sender, DrawToolTipEventArgs e)
		{
			e.DrawBackground();
			e.DrawBorder();

			Form frm = m_ctrl.FindForm();
			TextFormatFlags flags = (frm.RightToLeft == RightToLeft.Yes ?
				TextFormatFlags.RightToLeft : TextFormatFlags.Left) |
				TextFormatFlags.VerticalCenter;

			TextRenderer.DrawText(e.Graphics, ToolTipTitle, m_fntTitle,
				m_rcTitle, SystemColors.InfoText, flags);

			TextRenderer.DrawText(e.Graphics, m_text, m_fntText, m_rcText,
				SystemColors.InfoText, flags);

			// Draw the icon
			if (m_showMissingGlyphIcon)
			{
				Point pt = m_rcTitle.Location;
				pt.X -= (m_missingGlyphIcon.Width + 5);
				if (m_missingGlyphIcon.Height > m_rcTitle.Height)
					pt.Y -= (int)((m_missingGlyphIcon.Height - m_rcTitle.Height) / 2);

				e.Graphics.DrawImageUnscaled(m_missingGlyphIcon, pt);
			}

			// Draw a line separating the title from the text below it.
			Point pt1 = new Point(e.Bounds.X + 7, m_rcTitle.Bottom + 7);
			Point pt2 = new Point(e.Bounds.Right - 5, m_rcTitle.Bottom + 7);

			using (LinearGradientBrush br = new LinearGradientBrush(pt1, pt2,
				SystemColors.InfoText, SystemColors.Info))
			{
				e.Graphics.DrawLine(new Pen(br, 1), pt1, pt2);
			}
		}
	}
}
