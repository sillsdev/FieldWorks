// --------------------------------------------------------------------------------------------
// <copyright from='2002' to='2002' company='SIL International'>
//	Copyright (c) 2002, SIL International. All Rights Reserved.
// </copyright>
//
// File: WizardDialog.cs
// Responsibility: DavidO
// Last reviewed:
//
// Implementation of WizardDialog class from which .Net FieldWorks wizards should be
// derived.
// --------------------------------------------------------------------------------------------
using System;
using System.Resources;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.Utils;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Drawing;


namespace SIL.FieldWorks.Common.Controls
{
	internal interface IWizardPaintPanSteps
	{
		string[] StepNames { get; }
		int LastStepNumber { get; }
		int CurrentStepNumber{ get; }
		Font StepsFont { get; }
		Color TextColor { get; }
		Panel PanSteps { get; }
	}

	/// <summary>
	/// Summary description for WizardDialog.
	/// </summary>
	public class WizardDialog : Form, IFWDisposable, IWizardPaintPanSteps
	{
		#region Data Members
		/// <summary>Space between the steps panel and the steps tab control.</summary>
		protected const int kTabStepsPanStepsPadding = 5;
		/// <summary>Space between the right edge of the steps label and the form.</summary>
#if !__MonoCS__
		protected const int kdxpStepsLabelRightPadding = 9;
#else
		protected const int kdxpStepsLabelRightPadding = 12;	// FWNX-514. (could keep the two the same maybe)
#endif
		/// <summary>Space between the bottom edge of the steps label and the form.</summary>
		protected const int kdypStepsLabelBottomPadding = 54;
		/// <summary>Space between the bottom edge of the buttons and the form.</summary>
		protected const int kdypButtonsBottomPadding = 7;
		/// <summary>Space between the right edge of the help button and the form.</summary>
		protected const int kdxpHelpButtonPadding = 7;
		/// <summary>Space between the cancel and help buttons.</summary>
		protected const int kdxpCancelHelpButtonGap = 5;
		/// <summary>Space between the next and cancel buttons.</summary>
		protected const int kdxpNextCancelButtonGap = 10;
		/// <summary>This is the height difference between the steps panel and the
		/// tab control.</summary>
		protected const int kdypTabPanelHeightDiff = 24;
		/// <summary>This is the height difference between the tab control and the form.</summary>
		protected const int kdypFormTabHeightDiff = 52;
		/// <summary></summary>
		protected const int kdxpStepListSpacing = 8;
		/// <summary></summary>
		protected const int kdypStepListSpacing = 10;
		/// <summary></summary>
		protected const int kdxpStepSquareWidth = 14;
		/// <summary></summary>
		protected const int kdypStepSquareHeight = 14;
		/// <summary></summary>
		protected static readonly Color kclrPendingStep = Color.LightGray;
		/// <summary></summary>
		protected static readonly Color kclrCompletedStep = Color.Gray;
		/// <summary></summary>
		protected static readonly Color kclrCurrentStep = Color.LightGreen;
		/// <summary></summary>
		protected static readonly Color kclrLastStep = Color.Red;
		/// <summary></summary>
		protected int m_CurrentStepNumber = 0;

		private String[] m_StepNames;
		private int m_LastStepNumber;
		private Font m_StepsFont = SystemInformation.MenuFont;
		private Color m_StepTextColor = Color.White;
		private string m_NextText;
		private string m_FinishText;
		private string m_StepIndicatorFormat;
		/// <summary></summary>
		protected string m_helpTopicID;

		/// <summary></summary>
		protected System.Windows.Forms.Panel panSteps;
		/// <summary></summary>
		protected System.Windows.Forms.Label lblSteps;
		/// <summary></summary>
		protected System.Windows.Forms.Button m_btnBack;
		/// <summary></summary>
		protected System.Windows.Forms.Button m_btnCancel;
		/// <summary></summary>
		protected System.Windows.Forms.Button m_btnNext;
		/// <summary></summary>
		protected System.Windows.Forms.Button m_btnHelp;
		/// <summary></summary>
		protected System.Windows.Forms.TabControl tabSteps;
		private System.Windows.Forms.HelpProvider helpProvider2;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		#endregion

		#region Construction, Initialization, Disposal

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Constructor
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public WizardDialog()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			AccessibleName = GetType().Name;

			ResourceManager resources =
				new ResourceManager("SIL.FieldWorks.Common.Controls.FwControls",
				System.Reflection.Assembly.GetExecutingAssembly());

			m_helpTopicID = "khtpField-InterlinearSfmImportWizard-Step" + (m_CurrentStepNumber + 1.ToString()).TrimStart('0');
			m_NextText = resources.GetString("kstidWizForwardButtonText");
			m_FinishText = resources.GetString("kstidWizFinishButtonText");;
			m_StepIndicatorFormat = resources.GetString("kstidWizStepLabel");;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
				if (m_StepsFont != null)
					m_StepsFont.Dispose();
			}
			m_StepsFont = null;
			m_NextText = null;
			m_FinishText = null;
			m_StepIndicatorFormat = null;

			base.Dispose( disposing );
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

		#endregion

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WizardDialog));
			this.panSteps = new System.Windows.Forms.Panel();
			this.lblSteps = new System.Windows.Forms.Label();
			this.m_btnBack = new System.Windows.Forms.Button();
			this.m_btnCancel = new System.Windows.Forms.Button();
			this.m_btnNext = new System.Windows.Forms.Button();
			this.m_btnHelp = new System.Windows.Forms.Button();
			this.tabSteps = new System.Windows.Forms.TabControl();
			this.helpProvider2 = new System.Windows.Forms.HelpProvider();
			this.SuspendLayout();
			//
			// panSteps
			//
			this.panSteps.BackColor = System.Drawing.Color.Black;
			this.panSteps.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			resources.ApplyResources(this.panSteps, "panSteps");
			this.panSteps.Name = "panSteps";
			this.helpProvider2.SetShowHelp(this.panSteps, ((bool)(resources.GetObject("panSteps.ShowHelp"))));
			this.panSteps.Paint += new System.Windows.Forms.PaintEventHandler(this.panSteps_Paint);
			//
			// lblSteps
			//
			resources.ApplyResources(this.lblSteps, "lblSteps");
			this.lblSteps.Name = "lblSteps";
			this.helpProvider2.SetShowHelp(this.lblSteps, ((bool)(resources.GetObject("lblSteps.ShowHelp"))));
			//
			// m_btnBack
			//
			resources.ApplyResources(this.m_btnBack, "m_btnBack");
			this.m_btnBack.Name = "m_btnBack";
			this.helpProvider2.SetShowHelp(this.m_btnBack, ((bool)(resources.GetObject("m_btnBack.ShowHelp"))));
			this.m_btnBack.Click += new System.EventHandler(this.btnBack_Click);
			//
			// m_btnCancel
			//
			resources.ApplyResources(this.m_btnCancel, "m_btnCancel");
			this.m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btnCancel.Name = "m_btnCancel";
			this.helpProvider2.SetShowHelp(this.m_btnCancel, ((bool)(resources.GetObject("m_btnCancel.ShowHelp"))));
			this.m_btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
			//
			// m_btnNext
			//
			resources.ApplyResources(this.m_btnNext, "m_btnNext");
			this.m_btnNext.Name = "m_btnNext";
			this.helpProvider2.SetShowHelp(this.m_btnNext, ((bool)(resources.GetObject("m_btnNext.ShowHelp"))));
			this.m_btnNext.Click += new System.EventHandler(this.btnNext_Click);
			//
			// m_btnHelp
			//
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			this.m_btnHelp.Name = "m_btnHelp";
			this.helpProvider2.SetShowHelp(this.m_btnHelp, ((bool)(resources.GetObject("m_btnHelp.ShowHelp"))));
			this.m_btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
			//
			// tabSteps
			//
			resources.ApplyResources(this.tabSteps, "tabSteps");
			this.tabSteps.CausesValidation = false;
			this.tabSteps.Name = "tabSteps";
			this.tabSteps.SelectedIndex = 0;
			this.helpProvider2.SetShowHelp(this.tabSteps, ((bool)(resources.GetObject("tabSteps.ShowHelp"))));
			this.tabSteps.TabStop = false;
			this.tabSteps.SelectedIndexChanged += new System.EventHandler(this.tabSteps_SelectedIndexChanged);
			//
			// WizardDialog
			//
			this.AcceptButton = this.m_btnNext;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.m_btnCancel;
			this.Controls.Add(this.m_btnHelp);
			this.Controls.Add(this.m_btnBack);
			this.Controls.Add(this.m_btnCancel);
			this.Controls.Add(this.m_btnNext);
			this.Controls.Add(this.lblSteps);
			this.Controls.Add(this.tabSteps);
			this.Controls.Add(this.panSteps);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "WizardDialog";
			this.helpProvider2.SetShowHelp(this, ((bool)(resources.GetObject("$this.ShowHelp"))));
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		#region WizardDialog Properties

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the width of the steps panel.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Browsable(true)]
		[Localizable(true)]
		[Category("Layout")]
		public int StepPanelWidth
		{
			get
			{
				CheckDisposed();

				return panSteps.Width;
			}
			set
			{
				CheckDisposed();

				if (value > 0 && value < this.Width)
				{
					panSteps.Width = value;
					OnResize(null);
					panSteps.Invalidate();
				}
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the font for the text in the steps panel.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Browsable(true)]
		[Category("Appearance")]
		public Font StepTextFont
		{
			get
			{
				CheckDisposed();

				return m_StepsFont;
			}
			set
			{
				CheckDisposed();

				m_StepsFont = value;
				panSteps.Invalidate();
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the foreground color of the text in the steps panel.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Browsable(true)]
		[Category("Appearance")]
		public Color StepTextColor
		{
			get
			{
				CheckDisposed();

				return m_StepTextColor;
			}
			set
			{
				CheckDisposed();

				m_StepTextColor = value;
				panSteps.Invalidate();
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets the current step number. (Steps numbers are zero-based.)
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Browsable(false)]
		public int CurrentStepNumber
		{
			get
			{
				CheckDisposed();

				return m_CurrentStepNumber;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of the final step. (Steps numbers are zero-based.)
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Browsable(false)]
		public int LastStepNumber
		{
			get
			{
				CheckDisposed();

				return m_LastStepNumber;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the number of step pages needed for the wizard. This count must
		/// always be equal to or greater than the number of steps.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Browsable(true)]
		[Category("Misc")]
		public int StepPageCount
		{
			get
			{
				CheckDisposed();

				return tabSteps.TabCount;
			}
			set
			{
				CheckDisposed();

				// Make sure there are never fewer pages than there are steps.
				if (m_StepNames != null && value < m_StepNames.Length)
					value = m_StepNames.Length;

				// Do nothing if the number of pages didn't change or is negative.
				if (value == tabSteps.TabCount || value < 0)
					return;

				// Remove all the tabs if designer specified no steps.
				if (value == 0)
				{
					// Calling dispose on each page will make sure derived forms
					// being changed in design view will have the generated tab
					// page variables removed from the generated code. Trust me,
					// even though you may not understand what I just said -- DDO.
					foreach (TabPage page in tabSteps.TabPages)
						page.Dispose();

					tabSteps.TabPages.Clear();
					panSteps.Invalidate();
					return;
				}

				// Add more tabs when the current count is less than
				// the number of pages specified.
				if (value > tabSteps.TabCount)
				{
					for (int i = tabSteps.TabCount; i < value; i++)
					{
						TabPage newPage = new TabPage(String.Format(FwControls.kstidWizTabPage, i + 1));
						tabSteps.TabPages.Add(newPage);
					}
				}
				else
				{
					// Remove tabs until the count is the same as the number
					// of pages specified. Any controls placed on the tabs
					// being removed will be lost. (For commentary on why I
					// use dispose, see the comments earlier in this property
					// where value == 0).
					for (int i = tabSteps.TabCount - 1; i >= value; i--)
						tabSteps.TabPages[i].Dispose();
				}
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the array of strings used in the steps panel. The number of
		/// elements in this array determines the number of wizard steps.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Browsable(true)]
		[Category("Misc")]
		[Localizable(true)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		public String[] StepNames
		{
			get
			{
				CheckDisposed();

				return m_StepNames;
			}
			set
			{
				CheckDisposed();

				m_StepNames = value;
				m_LastStepNumber = (m_StepNames == null ? -1 : m_StepNames.Length - 1);
				UpdateStepLabel();
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the Enabled state of the Next button.
		/// </summary>
		/// <remarks>The button itself is private because making it protected causes
		/// Designer to attempt to re-locate it in the derived class, which ends up
		/// putting it in the wrong place.</remarks>
		/// -----------------------------------------------------------------------------------
		[Browsable(false)]
		[Localizable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		protected bool NextButtonEnabled
		{
			get{return m_btnNext.Enabled;}
			set {m_btnNext.Enabled = value;}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the Enabled state of the Back button.
		/// </summary>
		/// <remarks>The button itself is private because making it protected causes
		/// Designer to attempt to re-locate it in the derived class, which ends up
		/// putting it in the wrong place.</remarks>
		/// -----------------------------------------------------------------------------------
		[Browsable(false)]
		[Localizable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		protected bool BackButtonEnabled
		{
			get {return m_btnBack.Enabled;}
			set {m_btnBack.Enabled = value;}
		}
		#endregion

		#region WizardDialog Overrides

		/// -----------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			// If not in the design mode, then move the tab control up so the tabs disappear.
			// Do this because we really don't want the tab control to act like a tab control.
			// We just want the ability to have multiple pages without the ability to get to
			// those pages via the tabs across the top.
			if (!DesignMode)
			{
				if (tabSteps.TabCount > 0)
				{
					tabSteps.Top = -tabSteps.GetTabRect(0).Height;
					tabSteps.Height += tabSteps.GetTabRect(0).Height;
					m_CurrentStepNumber = 0;
					tabSteps.SelectedTab = tabSteps.TabPages[0];
					UpdateStepLabel();
				}

				m_btnBack.Enabled = false;
			}
		}

		/// <summary>
		/// This seemingly useless method is needed to get the Wizard buttons to display when
		/// using 120DPI fonts. Without a resize, the tab control covers up the buttons.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnActivated(EventArgs e)
		{
			base.OnActivated(e);
			this.OnResize(null);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// This will draw the the etched line that separates the dialog's buttons at the
		/// bottom from the rest of the dialog. It will also call the method to update the
		/// "Step x of n" label.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			// Draw the etched, horizontal line separating the wizard buttons
			// from the rest of the form.
			LineDrawing.DrawDialogControlSeparator(e.Graphics, ClientRectangle,
				lblSteps.Bottom + (m_btnHelp.Top - lblSteps.Bottom) / 2);

			UpdateStepLabel();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Position the controls according to the size of the form. We don't use the bottom
		/// anchor property since it really causes problems in derived classes. The buttons
		/// aren't anchored to any side for the same reason.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected override void OnResize(EventArgs e)
		{
			if (e != null)
				base.OnResize(e);

			this.SuspendLayout();

			tabSteps.Left = panSteps.Right + kTabStepsPanStepsPadding;
			tabSteps.Height = ClientSize.Height - kdypFormTabHeightDiff;
			tabSteps.Width = Width - (panSteps.Right + kTabStepsPanStepsPadding +
				panSteps.Left);

			panSteps.Height = tabSteps.Height - kdypTabPanelHeightDiff;
			lblSteps.Top = ClientSize.Height - (lblSteps.Height + kdypStepsLabelBottomPadding);

			m_btnHelp.Top = ClientSize.Height - (m_btnHelp.Height + kdypButtonsBottomPadding);
			m_btnHelp.Left = ClientSize.Width - (m_btnHelp.Width + kdxpHelpButtonPadding);
			m_btnCancel.Top = ClientSize.Height - (m_btnCancel.Height + kdypButtonsBottomPadding);
			m_btnCancel.Left = m_btnHelp.Left - (m_btnCancel.Width + kdxpCancelHelpButtonGap);
			m_btnNext.Top = ClientSize.Height - (m_btnNext.Height + kdypButtonsBottomPadding);
			m_btnNext.Left = m_btnCancel.Left - (m_btnNext.Width + kdxpNextCancelButtonGap);
			m_btnBack.Top = ClientSize.Height - (m_btnBack.Height + kdypButtonsBottomPadding);
			m_btnBack.Left = m_btnNext.Left - m_btnBack.Width;

			this.ResumeLayout(true);
		}

		#endregion

		#region WizardDialog Control Events
		/// -----------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void panSteps_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
		{
			PanStepsPaint(this, e);
		}

		#region IWizardPaintPanSteps implementation
		Font IWizardPaintPanSteps.StepsFont
		{
			get { return m_StepsFont; }
		}

		Color IWizardPaintPanSteps.TextColor
		{
			get { return m_StepTextColor; }
		}

		Panel IWizardPaintPanSteps.PanSteps
		{
			get { return panSteps; }
		}
		#endregion

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Paint method for steps. This is shared between WizardDialog and WizardStepPanel.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		internal static void PanStepsPaint(IWizardPaintPanSteps steps, PaintEventArgs e)
		{
			if (steps.StepNames == null || steps.LastStepNumber == -1)
				return;

			// This is the minimum height a single step text line will occupy.
			// (Note: this doesn't include the spacing between step text lines.)
			int dyStepHeight = Math.Max(steps.StepsFont.Height, kdypStepSquareHeight);

			// This is the rectangle for the text of step one. Each subsequent step's
			// text rectangle will be calculated by increasing the rectangle's Y
			// property accordingly. 3 is added as the number of horizontal pixels
			// between the text and the colored square.
			Rectangle rcText =
				new Rectangle(kdxpStepListSpacing + kdxpStepSquareWidth + 3,
				kdypStepListSpacing,
				steps.PanSteps.Width - (kdxpStepListSpacing + kdxpStepSquareWidth + 3),
				dyStepHeight);

			// This is the distance between the top of a step text's rectangle
			// and the top of the colored square's rectangle. However, if the step
			// text's rectangle is shorter than the height of the square, the
			// padding will be zero.
			int dySquarePadding = (dyStepHeight > kdypStepSquareHeight) ?
				(dyStepHeight - kdypStepSquareHeight) / 2 : 0;

			// This is the rectangle for step one's colored square.
			Rectangle rcSquare = new Rectangle(kdxpStepListSpacing,
				kdypStepListSpacing + dySquarePadding,
				kdxpStepSquareWidth, kdypStepSquareHeight);

			using (var sf = new StringFormat(StringFormat.GenericTypographic))
			{
				sf.Alignment = StringAlignment.Near;
				sf.LineAlignment = StringAlignment.Center;
				sf.Trimming = StringTrimming.EllipsisCharacter;

				// Calculate the horizontal position for the vertical connecting
				// line. (Subtracting 1 puts it just off center because the line
				// will be 2 pixels thick.)
				int xpConnectingLine = rcSquare.X + (kdxpStepSquareWidth / 2) - 1;

				// Create brushes for the colored squares and the step text.
				using (SolidBrush brSquare = new SolidBrush(kclrPendingStep),
					brText = new SolidBrush(steps.TextColor))
				{
					for (int i = 0; i <= steps.LastStepNumber; i++)
					{
						e.Graphics.DrawString(steps.StepNames[i], steps.StepsFont, brText, rcText, sf);
						rcText.Y += dyStepHeight + kdypStepListSpacing;

						// Determine what color the square should be.
						if (i == steps.LastStepNumber)
							brSquare.Color = kclrLastStep;
						else if (i == steps.CurrentStepNumber)
							brSquare.Color = kclrCurrentStep;
						else if (i < steps.CurrentStepNumber)
							brSquare.Color = kclrCompletedStep;
						else
							brSquare.Color = kclrPendingStep;

						// Draw the square next to the step text label.
						e.Graphics.FillRectangle(brSquare, rcSquare);
						rcSquare.Y += (dyStepHeight + kdypStepListSpacing);

						// Draw the vertical line connecting each step's square.
						if (i < steps.LastStepNumber)
						{
							using (var line = new LineDrawing(e.Graphics))
							{
								line.LineType = LineTypes.Solid;

								line.Draw(xpConnectingLine,
									rcSquare.Y - kdypStepListSpacing,
									xpConnectingLine, rcSquare.Y, kclrCompletedStep);

								line.Draw(xpConnectingLine + 1,
									rcSquare.Y - kdypStepListSpacing,
									xpConnectingLine + 1, rcSquare.Y, kclrPendingStep);
							}
						}
					}
				}
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Handle the Click event of the Back button
		/// </summary>
		/// <param name="sender">Ignored</param>
		/// <param name="e">Ignored</param>
		/// <remarks>This is <c>protected</c> for the purposes of exposing it in test code
		/// only. Production code can override the <see cref="OnBackButton"/> method if
		/// necessary</remarks>
		/// -----------------------------------------------------------------------------------
		protected void btnBack_Click(object sender, System.EventArgs e)
		{
			if (ValidToGoBackward())
			{
				m_CurrentStepNumber--;
				m_helpTopicID = "khtpField-InterlinearSfmImportWizard-Step" + (m_CurrentStepNumber + 1).ToString().TrimStart('0');
				this.OnBackButton();
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Handle the Click event of the Next button
		/// </summary>
		/// <param name="sender">Ignored</param>
		/// <param name="e">Ignored</param>
		/// <remarks>This is <c>protected</c> for the purposes of exposing it in test code
		/// only. Production code can override the <see cref="OnNextButton"/> method if
		/// necessary</remarks>
		/// -----------------------------------------------------------------------------------
		protected void btnNext_Click(object sender, System.EventArgs e)
		{
			if (!ValidToGoForward())
				return;

			if (m_CurrentStepNumber == m_LastStepNumber)
			{
				// Allow OnFinishButton() to change DialogResult (See TE-4237).
				this.DialogResult = DialogResult.OK;
				this.OnFinishButton();
				this.Visible = false;
			}
			else
			{
				m_CurrentStepNumber++;
				this.OnNextButton();
			}
			m_helpTopicID = "khtpField-InterlinearSfmImportWizard-Step" + (m_CurrentStepNumber + 1).ToString().TrimStart('0');
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Handle the Click event of the Cancel button
		/// </summary>
		/// <param name="sender">Ignored</param>
		/// <param name="e">Ignored</param>
		/// <remarks>This is <c>protected</c> for the purposes of exposing it in test code
		/// only. Production code can override the <see cref="OnCancelButton"/> method if
		/// necessary</remarks>
		/// -----------------------------------------------------------------------------------
		protected void btnCancel_Click(object sender, System.EventArgs e)
		{
			this.OnCancelButton();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Handle the Click event of the Help button
		/// </summary>
		/// <param name="sender">Ignored</param>
		/// <param name="e">Ignored</param>
		/// <remarks>This is <c>protected</c> for the purposes of exposing it in test code
		/// only. Production code can override the <see cref="OnHelpButton"/> method if
		/// necessary</remarks>
		/// -----------------------------------------------------------------------------------
		protected void btnHelp_Click(object sender, System.EventArgs e)
		{
			this.OnHelpButton();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void tabSteps_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			UpdateStepLabel();

			// Disable the back button if on the first or last step.
			m_btnBack.Enabled = (m_CurrentStepNumber > 0);

			// If on the last step, change the Next button's text to read 'Finish'
			// (or the localized equivalent).
			if (m_CurrentStepNumber == m_LastStepNumber)
			{
				m_btnNext.Text = m_FinishText;
//				this.helpProvider1.SetHelpString(NextHelpString"));

			}
			else
			{
				m_btnNext.Text = m_NextText;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Allows the inheritor to cause a click on the Next button to be ignored.
		/// </summary>
		/// <returns>A boolean representing whether or not it's valid to continue to
		/// the next step in the wizard.</returns>
		/// -----------------------------------------------------------------------------------
		protected virtual bool ValidToGoForward()
		{
			return true;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Allows the inheritor to cause a click on the Back button to be ignored.
		/// </summary>
		/// <returns>A boolean representing whether or not it's valid to return to the
		/// previous step in the wizard.</returns>
		/// -----------------------------------------------------------------------------------
		protected virtual bool ValidToGoBackward()
		{
			return true;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Subclass can override this method to do any actions necessary when wizard is
		/// finished.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected virtual void OnFinishButton()
		{
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected virtual void OnNextButton()
		{
			tabSteps.SelectedIndex++;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected virtual void OnBackButton()
		{
			tabSteps.SelectedIndex--;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected virtual void OnCancelButton()
		{
		}

		/// -----------------------------------------------------------------------------------
/// <summary>
///
/// </summary>
/// -----------------------------------------------------------------------------------
protected virtual void OnHelpButton()
{
}
		#endregion

		#region WizardDialog Helper Functions

		/// -----------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected void UpdateStepLabel()
		{
			lblSteps.Text = String.Format(m_StepIndicatorFormat,
				(m_CurrentStepNumber + 1).ToString(),
				(m_LastStepNumber + 1).ToString());

			lblSteps.Left = this.ClientSize.Width - (lblSteps.Width +
				(int)kdxpStepsLabelRightPadding);

			panSteps.Invalidate();
		}

		#endregion
	}
}
