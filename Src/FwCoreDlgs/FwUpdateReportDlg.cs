// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FwUpdateReportDlg.cs
// Responsibility: TE Team

using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Text;
using System.Windows.Forms;

using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.Utils;
using SIL.Utils.FileDialog;
using XCore;
using System.Diagnostics.CodeAnalysis;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Base class for displaying somewhat "techie" information to report on changes that were
	/// applied automatically to a project but which the user might need to review.
	/// </summary>
	/// <remarks>This is intended to be an abstract base class, but making it abstract messes up
	/// the Designer in the derived classes. You should override HelpTopicKey</remarks>
	/// ----------------------------------------------------------------------------------------
	public partial class FwUpdateReportDlg : Form, IFWDisposable
	{
		#region Private members
		private Font m_SansSerifFont;
		private Single m_PageMargin = 75.0f;
		private IHelpTopicProvider m_helpTopicProvider;
		private int m_currentLine;
		private string m_warningText;
		private int m_currentPage;
		private bool m_repeatTitleOnEveryPage = false;
		private bool m_repeatColumnHeaderOnEveryPage = true;
		/// <summary></summary>
		protected SaveFileDialogAdapter saveFileDialog;
		#endregion

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for FwUpdateReportDlg
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwUpdateReportDlg()
		{
			InitializeComponent();
			saveFileDialog = new SaveFileDialogAdapter();
			saveFileDialog.DefaultExt = "txt";
			saveFileDialog.SupportMultiDottedExtensions = true;
			saveFileDialog.Filter = FileUtils.FileDialogFilterCaseInsensitiveCombinations(FwCoreDlgs.TextFileFilter);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for FwUpdateReportDlg
		/// </summary>
		/// <param name="itemsToReport">List of items to report in the list.</param>
		/// <param name="projectName">Name of the project.</param>
		/// <param name="helpTopicProvider">context sensitive help</param>
		/// ------------------------------------------------------------------------------------
		public FwUpdateReportDlg(List<string> itemsToReport, string projectName,
			IHelpTopicProvider helpTopicProvider) : this()
		{
			lblProjectName.Text = String.Format(lblProjectName.Text, projectName);

			m_helpTopicProvider = helpTopicProvider;

			// show list of names
			foreach (string item in itemsToReport)
				lvItems.Items.Add(item);

			TopMost = true;
		}
		#endregion

		#region Virtual members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the help topic key.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual string HelpTopicKey
		{
			get { throw new NotImplementedException("Derived classes must override this (see remark at start of class)."); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether to repeat the title on every page when
		/// printing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool RepeatTitleOnEveryPage
		{
			set { m_repeatTitleOnEveryPage = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether to repeat the title on every page when
		/// printing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool RepeatColumnHeaderOnEveryPage
		{
			set { m_repeatColumnHeaderOnEveryPage = value; }
		}
		#endregion

		#region IFwDisposable implementation
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CheckDisposed()
		{
			if (IsDisposed)
			{
				throw new ObjectDisposedException(
					string.Format("'{0}' in use after being disposed.", GetType().Name));
			}
		}
		#endregion

		#region Event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Form.Load"/> event.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			using (Graphics gr = Graphics.FromHwnd(Handle))
			{
				SizeF stringSize = gr.MeasureString(lblWarning.Text, lblWarning.Font, lblWarning.Width);
				if (stringSize.Height > lblWarning.Height)
				{
					Height += ((int)Math.Ceiling(stringSize.Height) - lblWarning.Height);
					MinimumSize = Size;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the SizeChanged event of the lvItems control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void lvItems_SizeChanged(object sender, EventArgs e)
		{
			columnHeader1.Width = lvItems.ClientSize.Width - 1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle click on the "Print Report..." button.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void btnPrintRpt_Click(object sender, EventArgs e)
		{
			TopMost = false;
			printDocument.DocumentName = Text;
			printDialog1.Document = printDocument;

			DialogResult result = printDialog1.ShowDialog();
			if (result == DialogResult.OK)
			{
				printDocument.PrinterSettings = printDialog1.PrinterSettings;
				printDocument.DefaultPageSettings = printDialog1.Document.DefaultPageSettings;

				printDocument.DefaultPageSettings.Margins =
					new Margins((int)m_PageMargin, (int)m_PageMargin, (int)m_PageMargin, (int)m_PageMargin);
				if (printDocument.DefaultPageSettings.PrintableArea.Width < 300 ||
					printDocument.DefaultPageSettings.PrintableArea.Height < 350)
				{
					MessageBox.Show(Properties.Resources.kstidPageTooSmall);
					return;
				}

				printDocument.OriginAtMargins = true;

				try
				{
					printDocument.Print();
				}
				catch (Exception exception)
				{
					TopMost = true;

					string errorMsg = String.Format(
						ResourceHelper.GetResourceString("kstidPrintingException"), exception.Message);

					MessageBox.Show(this, errorMsg, Text, MessageBoxButtons.OK,
						MessageBoxIcon.Error);
					TopMost = false;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle click on the "Save Report..." button.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void btnSaveRpt_Click(object sender, EventArgs e)
		{
			TopMost = false;
			saveFileDialog.FileName = Text;
			saveFileDialog.OverwritePrompt = true;
			saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
			DialogResult result = saveFileDialog.ShowDialog();
			if (result == DialogResult.OK)
			{
				using (FileStream file = new FileStream(saveFileDialog.FileName, FileMode.Create))
				{
					// Output warning message
					byte[] byteArray = StringToByteArray(Text + Environment.NewLine +
					lblProjectName.Text + Environment.NewLine + Environment.NewLine +
					lblWarning.Text + Environment.NewLine + Environment.NewLine);
					file.Write(byteArray, 0, byteArray.Length);

					// Output column heading
					byteArray = StringToByteArray(lvItems.Columns[0].Text + ":" + Environment.NewLine);
					file.Write(byteArray, 0, byteArray.Length);

					// Output list of report items
					foreach (ListViewItem item in lvItems.Items)
					{
						byteArray = StringToByteArray(" " + item.Text + Environment.NewLine);
						file.Write(byteArray, 0, byteArray.Length);
					}

					// Close file
					file.Flush();
					file.Close();
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Convert string to byte array.
		/// </summary>
		/// <param name="stringToConvert">string to convert to a byte array</param>
		/// ------------------------------------------------------------------------------------
		private byte[] StringToByteArray(string stringToConvert)
		{
			UTF8Encoding utf8encoding = new UTF8Encoding();
			return utf8encoding.GetBytes(stringToConvert);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle click on the "Help" button.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void btnHelp_Click(object sender, EventArgs e)
		{
			TopMost = false;
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, HelpTopicKey);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle BeginPrint event.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "FontFamily.Families contains references")]
		private void printDocument_BeginPrint(object sender, PrintEventArgs e)
		{
			if (m_SansSerifFont == null)
			{
				// get a sans-serif font
				m_SansSerifFont = new Font(FontFamily.GenericSansSerif, 12, FontStyle.Regular, GraphicsUnit.Point);
				{
					foreach (FontFamily family in FontFamily.Families)
					{
						string familyName = family.Name.ToLower();
						if (familyName.Contains("arial") || familyName.Contains("tahoma") || familyName.Contains("calibri"))
						{
							m_SansSerifFont = new Font(family, 12, FontStyle.Regular, GraphicsUnit.Point);
							continue;
						}
					}
				}
			}

			// set the page margins
			m_currentLine = 0;
			m_currentPage = 1;
			m_warningText = lblWarning.Text;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle PrintPage event.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void printDocument_PrintPage(object sender, PrintPageEventArgs e)
		{
			Single currentX = 0;
			Single currentY = 0;
			Single pageWidth = e.MarginBounds.Width;
			Single pageHeight = e.MarginBounds.Height;

			using (Font sanSerifBoldFont = new Font(m_SansSerifFont, FontStyle.Bold))
			{
				SizeF layoutSize = new SizeF(pageWidth, pageHeight - sanSerifBoldFont.Height * 2); // Allow room for page number

				int charactersFitted;
				int linesFilled;

				if (m_currentPage == 1 || m_repeatTitleOnEveryPage)
				{
					// title
					SizeF stringSize = e.Graphics.MeasureString(Text, sanSerifBoldFont, layoutSize,
						new StringFormat(), out charactersFitted, out linesFilled);
					Single dx = pageWidth / 2 - (stringSize.Width / 2);
					e.Graphics.DrawString(Text, sanSerifBoldFont, Brushes.Black, dx, currentY);

					layoutSize.Height -= stringSize.Height;
					currentY += stringSize.Height;

					stringSize = e.Graphics.MeasureString(lblProjectName.Text, sanSerifBoldFont, layoutSize,
						new StringFormat(), out charactersFitted, out linesFilled);
					dx = pageWidth / 2 - (stringSize.Width / 2);
					e.Graphics.DrawString(lblProjectName.Text, sanSerifBoldFont, Brushes.Black, dx, currentY);

					layoutSize.Height -= stringSize.Height + sanSerifBoldFont.Height;
					currentY += stringSize.Height + sanSerifBoldFont.Height;
				}

				// Warning goes on page 1, but if it's too long to fit, it could spill over onto subsequent page(s)
				if (!String.IsNullOrEmpty(m_warningText))
				{
					// print the warning label
					using (var stringFormat = new StringFormat())
					{
						SizeF stringSize = e.Graphics.MeasureString(m_warningText, m_SansSerifFont, layoutSize,
						stringFormat, out charactersFitted, out linesFilled);

						RectangleF drawRect = new RectangleF(currentX, currentY, layoutSize.Width, layoutSize.Height);
						e.Graphics.DrawString(m_warningText, m_SansSerifFont, Brushes.Black, drawRect,
						stringFormat);

						m_warningText = m_warningText.Substring(charactersFitted);

						// Skip a line
						layoutSize.Height -= stringSize.Height + m_SansSerifFont.Height;
						currentY += stringSize.Height + m_SansSerifFont.Height;
					}
				}

				if (layoutSize.Height > (sanSerifBoldFont.Height * 2))
				{
					// print column heading (repeat at start of each page, if requested)
					if (m_currentPage == 1 || m_repeatColumnHeaderOnEveryPage)
					{
						e.Graphics.DrawString(lvItems.Columns[0].Text + ":", sanSerifBoldFont, Brushes.Black, currentX, currentY);
						layoutSize.Height -= sanSerifBoldFont.Height;
						currentY += sanSerifBoldFont.Height;
					}

					currentX += 25; // indent 1/4 inch

					// print report items
					while (layoutSize.Height > (m_SansSerifFont.Height) && m_currentLine < lvItems.Items.Count)
					{
						ListViewItem item = lvItems.Items[m_currentLine++];
						e.Graphics.DrawString(item.Text, m_SansSerifFont, Brushes.Black, currentX, currentY);
						layoutSize.Height -= m_SansSerifFont.Height;
						currentY += m_SansSerifFont.Height;
					}
				}
				e.HasMorePages = !String.IsNullOrEmpty(m_warningText) || m_currentLine < lvItems.Items.Count;

				// Print the page number if there's going to be more than one page
				if (m_currentPage > 1 || e.HasMorePages)
				{
					currentY = pageHeight - m_SansSerifFont.Height;
					string pageNumber = m_currentPage.ToString();
					SizeF stringSize = e.Graphics.MeasureString(pageNumber, m_SansSerifFont, layoutSize);
					currentX = pageWidth / 2 - (stringSize.Width / 2);
					e.Graphics.DrawString(pageNumber, m_SansSerifFont, Brushes.Black, currentX, currentY);
				}

				m_currentPage++;
			}
		}
		#endregion
	}
}