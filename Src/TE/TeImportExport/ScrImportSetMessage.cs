// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ScrImportSetMessage.cs
// Responsibility: TomB
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Resources;

using SIL.Utils;
using SIL.FieldWorks.Common.Drawing;

namespace SIL.FieldWorks.TE
{
	/// <summary>
	/// Summary description for ScrImportSetMessage.
	/// </summary>
	public class ScrImportSetMessage : Form, IFWDisposable
	{
		/// <summary></summary>
		protected string m_HelpUrl;
		/// <summary></summary>
		protected string m_HelpTopic;
		private System.Windows.Forms.ListBox lstInvalidFiles;

		private bool m_fDisplaySetupOption = true;
		/// <summary></summary>
		public Label lblMsg;
		private Button btnRetry;
		/// <summary></summary>
		/// <remarks>protected because of testing</remarks>
		protected Button btnHelp;
		private Button btnSetup;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ScrImportSetMessage"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public ScrImportSetMessage()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
		}

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		#region Windows Form Designer generated code
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			System.Windows.Forms.Button btnCancel;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ScrImportSetMessage));
			this.lblMsg = new System.Windows.Forms.Label();
			this.btnRetry = new System.Windows.Forms.Button();
			this.btnHelp = new System.Windows.Forms.Button();
			this.lstInvalidFiles = new System.Windows.Forms.ListBox();
			this.btnSetup = new System.Windows.Forms.Button();
			btnCancel = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// btnCancel
			//
			resources.ApplyResources(btnCancel, "btnCancel");
			btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			btnCancel.Name = "btnCancel";
			//
			// lblMsg
			//
			resources.ApplyResources(this.lblMsg, "lblMsg");
			this.lblMsg.Name = "lblMsg";
			//
			// btnRetry
			//
			resources.ApplyResources(this.btnRetry, "btnRetry");
			this.btnRetry.DialogResult = System.Windows.Forms.DialogResult.Retry;
			this.btnRetry.Name = "btnRetry";
			//
			// btnHelp
			//
			resources.ApplyResources(this.btnHelp, "btnHelp");
			this.btnHelp.Name = "btnHelp";
			this.btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
			//
			// lstInvalidFiles
			//
			resources.ApplyResources(this.lstInvalidFiles, "lstInvalidFiles");
			this.lstInvalidFiles.BackColor = System.Drawing.SystemColors.Control;
			this.lstInvalidFiles.Name = "lstInvalidFiles";
			this.lstInvalidFiles.Sorted = true;
			//
			// btnSetup
			//
			resources.ApplyResources(this.btnSetup, "btnSetup");
			this.btnSetup.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnSetup.Name = "btnSetup";
			//
			// ScrImportSetMessage
			//
			this.AcceptButton = this.btnSetup;
			resources.ApplyResources(this, "$this");
			this.CancelButton = btnCancel;
			this.Controls.Add(this.btnSetup);
			this.Controls.Add(this.lstInvalidFiles);
			this.Controls.Add(this.btnHelp);
			this.Controls.Add(btnCancel);
			this.Controls.Add(this.btnRetry);
			this.Controls.Add(this.lblMsg);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ScrImportSetMessage";
			this.ShowInTaskbar = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		#region Public properties
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Set the main message text
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public string Message
		{
			set
			{
				CheckDisposed();

				lblMsg.Text = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the dialog's list of invalid (or inaccessible) file names.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string[] InvalidFiles
		{
			set
			{
				CheckDisposed();

				lstInvalidFiles.Items.Clear();
				if (value != null)
					lstInvalidFiles.Items.AddRange(value);
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Set the help URL
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public string HelpURL
		{
			set
			{
				CheckDisposed();

				m_HelpUrl = value;
				SetHelpBtnVisibility();
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Set the help topic
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public string HelpTopic
		{
			set
			{
				CheckDisposed();

				m_HelpTopic = value;
				SetHelpBtnVisibility();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set whether the Setup button is a valid option.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool DisplaySetupOption
		{
			set
			{
				CheckDisposed();

				m_fDisplaySetupOption = value;
			}
		}
		#endregion

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Upon loading this form, we need to show/hid certain buttons.
		/// </summary>
		/// <param name="e"></param>
		/// -----------------------------------------------------------------------------------
		protected override void OnLoad(System.EventArgs e)
		{
			btnSetup.Visible = m_fDisplaySetupOption;
			AcceptButton = m_fDisplaySetupOption ? btnSetup : btnRetry;
			SetHelpBtnVisibility();
		}

		#region Other private methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows help when Help button is pressed.
		/// </summary>
		/// <param name="sender">Object that sent this message</param>
		/// <param name="e">More information about the event</param>
		/// ------------------------------------------------------------------------------------
		private void btnHelp_Click(object sender, System.EventArgs e)
		{
			// We have to use a label because without it the help is always on top of the window
			Help.ShowHelp(new Label(), m_HelpUrl, m_HelpTopic);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Hides/shows help button based on whther or not help topic and URL are set.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetHelpBtnVisibility()
		{
			btnHelp.Visible = ((m_HelpTopic != null) && (m_HelpUrl != null));
		}

//		/// ------------------------------------------------------------------------------------
//		/// <summary>
//		/// Custom painting method to paint the label contants by assembling a string from
//		/// the resources. The string that is built is composed of a main string that contains
//		/// a placeholder for a variable. The variable is filled by a string which is displayed
//		/// as italic type.
//		/// </summary>
//		/// <param name="sender">Object that sent this message</param>
//		/// <param name="e">More information about the event, most importantly the Graphics
//		/// object that allows us to paint the label</param>
//		/// <remarks>
//		/// This method could be further abstracted to generalize it and make this (or similar)
//		/// functionality available to other dialogs. If this becomes necessary, it would
//		/// probably be best to make a custom label control to do this and use standard HTML
//		/// tags as the means by which formatted strings could be set. Then the painting method
//		/// could be extended to recognize any needed tags. For now, the code only recognizes
//		/// <I> and </I> (italics), and is further limited to expect them only at the beginning
//		/// of words. In a more generalized control, this limitation would probably need to be
//		/// removed.
//		/// </remarks>
//		/// ------------------------------------------------------------------------------------
//		private void lblLaunchMsg_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
//		{
//			string sFullLaunchMsg = lblLaunchMsg.Text;
//
//			// *******************************************************************************
//			// All code from here on is code that would be part of a generic formatted label
//			// control.
//			// *******************************************************************************
//
//			System.Drawing.Graphics g = e.Graphics;
//			Font fnt = lblLaunchMsg.Font;
//
//			// Expand the clip rectangle to be the entire rectangle of the label. Repainting
//			// the whole label is much easier than just trying to repaint the clipped part.
//			g.SetClip(lblLaunchMsg.ClientRectangle);
//
//			// rcOrig is the (newly set) clip rectangle. We need to remember it so we can
//			// use it as our basis for resetting the working rectangle (rc) each time we fill
//			// a line of text.
//			Rectangle rcOrig = new Rectangle((int)g.ClipBounds.X, (int)g.ClipBounds.Y,
//				(int)g.ClipBounds.Width, (int)g.ClipBounds.Height);
//			Rectangle rc = rcOrig;
//
//			// "Erase" the rectangle so we can repaint without getting debris.
//			g.FillRectangle(new SolidBrush(lblLaunchMsg.BackColor), rc);
//
//			// Split the label text into an array of words.
//			string[] words = sFullLaunchMsg.Split(' ');
//			string sWordExtraChars = string.Empty;
//
//			// See Petzold's Programming MS Windows with C#, page 408ff. for a full description
//			// of why we need to do this. It helps minimize the ugliness of extra spacing on
//			// "normal" display devices.
//			StringFormat sfGeneric = new StringFormat(StringFormat.GenericTypographic);
//			sfGeneric.Alignment = StringAlignment.Near;
//			sfGeneric.LineAlignment = StringAlignment.Near;
//			sfGeneric.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
//
//			// This is the main processing loop. We lay out the words on a line one at a time.
//			// When the next word to be laid out won't fit, we advance to the next line. Note
//			// that line height is effectively constant, since the same font size is used for
//			// all text. If this code is ever extended to allow different size fonts, it will
//			// be necessary to pre-determine the baseline of each line based on the largest font
//			// to be drawn on that line.
//			for (int i = 0; i < words.Length; i++)
//			{
//				// Switch to use the italic or non-italic font based on the <I> tag.
//				if (words[i].StartsWith("<I>"))
//				{
//					words[i] = words[i].Substring(3);
//					fnt = new Font(lblLaunchMsg.Font, System.Drawing.FontStyle.Italic);
//				}
//				else if (words[i].StartsWith("</I>"))
//				{
//					words[i] = words[i].Substring(4).Trim();
//					fnt = lblLaunchMsg.Font;
//				}
//
//				// If the tag occurs by itself, the "word" was all tag an is now empty, so fetch
//				// the next word.
//				if (words[i].Length == 0)
//					continue;
//
//				// Determine how long the word will be when drawn. For this purpose, we do not
//				// append a trailing space since we don't want a word to get wrapped simply
//				// because the width of the space would push it over the limit.
//				// Note: we really don't care about the maximum width, but in order to pass the
//				// StringFormat parameter, we have to supply one, so we multiply the full width
//				// of the label by 2 to make sure max width is big enough for us to detect
//				// overrun and do the needed line wrapping. If we used the real max width, the
//				// measurement of a long string would be exactly equal to rc.Width and we would
//				// not know if we had overrun.
//				System.Drawing.SizeF strSize = g.MeasureString(words[i], fnt, rcOrig.Width * 2,
//					sfGeneric);
//
//				// If the word is too long, advance to the next line...
//				if (rc.Width < (int)strSize.Width)
//				{
//					// ...but if it's so long that it wouldn't even fit on a full line, then
//					// just lay out as many characters as possible, and break in the middle of
//					// the word. For now, we're not attempting to hyphenate or anything. Mainly,
//					// this logic is intended to deal with localizations into languages that
//					// don't use spaces to separate words.
//					if (rcOrig.Width < (int)strSize.Width)
//					{
//						sWordExtraChars = string.Empty;
//						do
//						{
//							sWordExtraChars = words[i].Substring(words[i].Length - 1) +
//								sWordExtraChars;
//							words[i] = words[i].Substring(0, words[i].Length - 1);
//							strSize = g.MeasureString(words[i], fnt, rcOrig.Width * 2,
//								sfGeneric);
//						}
//						while (rc.Width < (int)strSize.Width);
//					}
//					else
//					{
//						// Reset the working rectangle so the next text we lay out will start
//						// at the left edge of the label and on the next line down.
//						rc.X = rcOrig.Left;
//						rc.Y += (int)strSize.Height;
//						rc.Height -= (int)strSize.Height;
//						rc.Width = rcOrig.Width;
//					}
//				}
//
//				// Lay out the word (or part of word) in the label. The space could get clipped,
//				// but that's okay. The above logic should ensure that the word itself will not
//				// get clipped by rc.
//				g.DrawString(words[i] + " ", fnt, new SolidBrush(lblLaunchMsg.ForeColor), rc,
//					sfGeneric);
//
//				// Change the size and position of the working rectangle to remove the part we
//				// just used.
//				strSize = g.MeasureString(words[i] + " ", fnt, rc.Width, sfGeneric);
//				rc.X += (int)strSize.Width;
//				rc.Width -= (int)strSize.Width;
//
//				// If we've had to break in the middle of a word, now reset the word to be the
//				// remaining part, and decrement the lopp index so we lay it out.
//				if (sWordExtraChars != string.Empty)
//				{
//					words[i--] = sWordExtraChars;
//					sWordExtraChars = string.Empty;
//				}
//			}
//		}
		#endregion
	}
}
