// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: PageSetupDlg.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Controls;
using SIL.Utils;
using SIL.FieldWorks.Common.Framework;
using XCore;
using SIL.FieldWorks.Common.RootSites;

namespace SIL.FieldWorks.Common.PrintLayout
{
	#region Enumerations
	/// <summary>Enumeration containing possible setting conflict errors in the Page Setup
	/// dialog</summary>
	public enum PageSetupErrorType
	{
		/// <summary>All settings are valid on Page Setup dialog</summary>
		NoError,
		/// <summary>The publication page size is bigger than the paper size.</summary>
		PubPageTooBig,
		/// <summary>The Top/Bottom Margins are bigger than the publication page size.</summary>
		VerticalMarginsTooBig,
		/// <summary>The Inside/Outside Margins are bigger than the publication page size.</summary>
		HorizontalMarginsTooBig,
		/// <summary>The gutter is so big that the page won't fit on the paper.</summary>
		GutterTooBig,
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for PageSetupDlg.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class PageSetupDlg : Form, IFWDisposable, IPageSetupDialog
	{
		#region Constants
		private const int ktabMargins = 0;
		private const int ktabPaper = 1;
		private const int ktabText = 2;
		private const int kFootnoteSepWidthNone = 0;
		private const int kFootnoteSepWidthThird = 1;
		private const int kFootnoteSepWidthFull = 2;
		/// <summary>Conversion factor for hundredths of an inch to millipoints.</summary>
		private const int kCentiInchToMilliPoints = 720;
		private const int kDefaultBaseCharSize = 11;
		private const int kDefaultLineSpacing = 13;
		#endregion

		#region Member variables
		private bool m_AdjustingPaperSize;
		private bool m_fBookFoldCurrent;
		private bool m_fChangingIndex;
		private bool m_fAllowNonStandardChoicesCheckBoxAvailable = true;
		/// <summary>Flag to save paper size (if changed)</summary>
		private bool m_fSavePaperSize;
		/// <summary>Flag to save base font and line height sizes (if changed)</summary>
		protected bool m_fSaveBaseFontAndLineSizes = false;
		/// <summary>Flag to save margins (if changed)</summary>
		private bool m_fSaveMargins = false;

		private IApp m_app;

		/// <summary>Leading factor used to scale line spacing to keep it in synch with font size.
		/// Default value of 1.2 times the base character size</summary>
		private decimal m_leadingFactor;
		/// <summary>Major object that owns the current publication and a collection of
		/// header/footer sets</summary>
		private ICmMajorObject m_pubOwner;
		/// <summary>current publication</summary>
		private IPublication m_publication;
		/// <summary>current page layout</summary>
		private IPubPageLayout m_pgLayout;
		private IPubDivision m_division;
		/// <summary>Callbacks to get application-specific settings, e.g. header/footer sets</summary>
		private IPageSetupCallbacks m_callbacks;
		/// <summary>Flag that prevents the base font size and line spacing from being set
		/// because of an assignment</summary>
		private bool m_fChangingTextSize = false;
		/// <summary>Flag that indictes both font size and line spacing are being set to default
		/// values, so do not adjust line spacing to maintain leading.</summary>
		protected bool m_fSetFontSizeAndLineSpacing = false;
		/// <summary>Combo box containing the list of possible paper sizes</summary>
		protected FwOverrideComboBox cbPaperSize;
		/// <summary>up-down measure control for the top margin</summary>
		protected SIL.FieldWorks.FwCoreDlgControls.UpDownMeasureControl m_udmTop;
		/// <summary>up-down measure control for the inside margin</summary>
		protected SIL.FieldWorks.FwCoreDlgControls.UpDownMeasureControl m_udmLeft;
		/// <summary>up-down measure control for the bottom margin</summary>
		protected SIL.FieldWorks.FwCoreDlgControls.UpDownMeasureControl m_udmBottom;
		/// <summary>up-down measure control for the outside margin</summary>
		protected SIL.FieldWorks.FwCoreDlgControls.UpDownMeasureControl m_udmRight;
		/// <summary>up-down measure control for the gutter</summary>
		protected SIL.FieldWorks.FwCoreDlgControls.UpDownMeasureControl m_udmGutter;
		/// <summary>current paper width</summary>
		protected SIL.FieldWorks.FwCoreDlgControls.UpDownMeasureControl m_udmPaperWidth;
		/// <summary>current paper height</summary>
		protected SIL.FieldWorks.FwCoreDlgControls.UpDownMeasureControl m_udmPaperHeight;
		/// <summary>Number of columns in the layout</summary>
		private int m_numColumns;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		private System.Windows.Forms.TabPage tabLayout;
		private System.Windows.Forms.Label label16;
		private System.Windows.Forms.Label label17;
		private System.Windows.Forms.Label label22;
		private System.Windows.Forms.Label label25;
		private System.Windows.Forms.CheckBox m_ckbDiffFirstHF;
		private System.Windows.Forms.CheckBox m_ckbDiffEvenHF;
		private FwOverrideComboBox m_cbBookStart;
		private FwOverrideComboBox m_cboSeparator;
		private PictureBox pbDraft;
		private PictureBox pbBookFold;
		private TabControl tabControl1;
		private RadioButton rdoDoubleSide;
		private RadioButton rdoSingleSide;
		private Label label8;
		private Label label9;
		private Label label28;
		/// <summary>combobox which contains the list of possible publication page sizes</summary>
		protected ComboBox cboPubPageSize;
		private Panel pnlDraftOptions;
		private Panel pnlBookFoldOptions;
		private FwOverrideComboBox fwOverrideComboBox1;
		private Label label7;
		private SIL.FieldWorks.FwCoreDlgControls.UpDownMeasureControl m_udmFooter;
		private SIL.FieldWorks.FwCoreDlgControls.UpDownMeasureControl m_udmHeader;
		private Label label1;
		private Button btnHeaderFooter;
		private Label m_lblLineSpacing;
		/// <summary>numeric up-down control for setting the line spacing</summary>
		protected NumericUpDown m_nudLineSpacing;
		private Label label18;
		/// <summary>numeric up-down control for setting the base character size</summary>
		protected NumericUpDown m_nudBaseCharSize;
		/// <summary>Check box that allows the user to make choices that don't follow iPub standards.</summary>
		protected CheckBox m_chkNonStdChoices;
		private Label label23;
		private Label label33;
		private Label lblTwoCol;
		private Label lblOneCol;
		private Button btnOneColumn;
		private Panel pnlOneColumn;
		private Panel pnlTwoColumn;
		private Button btnTwoColumn;
		private Button m_btnOK;
		private Panel panelPreviewPage;
		private Panel panelPreviewMargins;
		private Label label29;
		private Label label30;
		private Panel pnlPreviewContainer;
		private Panel panelPreviewContainerMargins;
		private Panel panelPreviewContainerText;
		private Panel panelPreviewText;
		private Label label31;
		private Label label32;
		private Panel panel3;

		private IHelpTopicProvider m_helpTopicProvider;
		#endregion

		#region Constructor/destructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Page Setup Dialog
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public PageSetupDlg()
		{
			InitializeComponent();

			// Default to book start of New Page
			m_cbBookStart.SelectedIndex = 1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:PageSetupDlg"/> class.
		/// </summary>
		/// <param name="pgLayout">The page layout.</param>
		/// <param name="pubOwner">The CmMajorObject that owns the publication.</param>
		/// <param name="publication">The publication.</param>
		/// <param name="division">The division. The NumColumns in the division should be
		/// set before calling this dialog.</param>
		/// <param name="callbacks">The callbacks used to get application-specific settings.</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// <param name="app">The app</param>
		/// <param name="pubPageSizes">The page sizes available for publication.</param>
		/// ------------------------------------------------------------------------------------
		public PageSetupDlg(IPubPageLayout pgLayout, ICmMajorObject pubOwner,
			IPublication publication, IPubDivision division, IPageSetupCallbacks callbacks,
			IHelpTopicProvider helpTopicProvider, IApp app, List<PubPageInfo> pubPageSizes) : this()
		{
			m_pubOwner = pubOwner;
			m_publication = publication;
			m_division = division;
			m_pgLayout = pgLayout;
			m_callbacks = callbacks;
			m_helpTopicProvider = helpTopicProvider;
			m_app = app;

			InitializePaperSizes();

			MsrSysType units = app.MeasurementSystem;
			m_udmTop.MeasureType = units;
			m_udmLeft.MeasureType = units;
			m_udmBottom.MeasureType = units;
			m_udmRight.MeasureType = units;
			m_udmGutter.MeasureType = units;
			m_udmPaperWidth.MeasureType = units;
			m_udmPaperWidth.UseVariablePrecision = true;
			m_udmPaperHeight.MeasureType = units;
			m_udmPaperHeight.UseVariablePrecision = true;
			m_udmHeader.MeasureType = units;
			m_udmFooter.MeasureType = units;

			m_udmTop.MeasureValue = pgLayout.MarginTop;
			m_udmLeft.MeasureValue = pgLayout.MarginInside;
			m_udmBottom.MeasureValue = pgLayout.MarginBottom;
			m_udmRight.MeasureValue = pgLayout.MarginOutside;
			m_udmGutter.MeasureValue = publication.GutterMargin;

			//REVIEW: Do we need to call UpdateMarginControls?
			m_numColumns = m_division.NumColumns;

			m_fBookFoldCurrent = publication.IsLandscape;

			//if (m_fBookFoldCurrent)
			//{
			//    pbBookFold.Image = ResourceHelper.BookFoldSelectedIcon;
			//    pbDraft.Image = ResourceHelper.PortraitIcon;
			//    pnlDraftOptions.Visible = false;
			//    pnlBookFoldOptions.Visible = true;

			//}
			//else // Draft
			//{
			//    pbBookFold.Image = ResourceHelper.BookFoldIcon;
			//    pbDraft.Image = ResourceHelper.PortraitSelectedIcon;
			//    pnlDraftOptions.Visible = true;
			//    pnlBookFoldOptions.Visible = false;
			//}

			//rdoSingleSide.Checked = true;
			//rdoDoubleSide.Checked = false;

			if (pubPageSizes != null && pubPageSizes.Count > 0)
			{
				cboPubPageSize.Items.Clear();
				foreach (PubPageInfo pubPgInfo in pubPageSizes)
				{
					cboPubPageSize.Items.Add(pubPgInfo);
					if (publication.PageHeight == pubPgInfo.Height &&
						publication.PageWidth == pubPgInfo.Width)
					{
						cboPubPageSize.SelectedItem = pubPgInfo;
					}
				}
			}
			else
				cboPubPageSize.SelectedIndex = 0;

			int mptPaperHeight = publication.PaperHeight;
			int mptPaperWidth = publication.PaperWidth;
			if (mptPaperHeight == 0)
			{
				m_fSavePaperSize = false;
				Debug.Assert(mptPaperWidth == 0);
				PrinterUtils.GetDefaultPaperSizeInMp(out mptPaperHeight, out mptPaperWidth);
			}
			else
				m_fSavePaperSize = true;

			// Find the paper size in the combo box.
			foreach (PaperSize size in cbPaperSize.Items)
			{
				if (size.Height * kCentiInchToMilliPoints == mptPaperHeight &&
					size.Width * kCentiInchToMilliPoints == mptPaperWidth)
				{
					cbPaperSize.SelectedItem = size;
				}
			}

			m_udmPaperWidth.MeasureValue = mptPaperWidth;
			m_udmPaperHeight.MeasureValue = mptPaperHeight;
			AdjustPaperSize(mptPaperWidth, mptPaperHeight);

			foreach (PubPageInfo pubPgInfo in cboPubPageSize.Items)
			{
				if (pubPgInfo.Height == m_publication.PageHeight &&
					pubPgInfo.Width == m_publication.PageWidth)
				{
					cboPubPageSize.SelectedItem = pubPgInfo;
					break;
				}
			}

			m_cbBookStart.SelectedIndex = (int)m_division.StartAt;
			m_ckbDiffEvenHF.Checked = m_division.DifferentEvenHF;
			m_ckbDiffFirstHF.Checked = m_division.DifferentFirstHF;
			m_udmHeader.MeasureValue = pgLayout.PosHeader;
			m_udmFooter.MeasureValue = pgLayout.PosFooter;
			switch (publication.FootnoteSepWidth)
			{
				case 0:
					m_cboSeparator.SelectedIndex = kFootnoteSepWidthNone;
					break;
				case 333:
					m_cboSeparator.SelectedIndex = kFootnoteSepWidthThird;
					break;
				case 1000:
					m_cboSeparator.SelectedIndex = kFootnoteSepWidthFull;
					break;
				default:
					Debug.Assert(false, "non-default footnote seperator width");
					m_cboSeparator.SelectedIndex = -1;
					break;
			}

			m_fChangingIndex = false;

			// Initialize dialog settings from the publication.
			NumberOfColumns = m_division.NumColumns;
			UpdateColumnButtonStates();

			SetFontSizeAndLineSpacing = true;
			if (PublicationUsesNormalStyle)
			{
				SetDefaultBaseFontAndLineSizes();
			}
			else
			{
				// In the unlikely event that the publication has a default value for one but not
				// both of these values, set the leading to the default percentage and set the
				// implicit value based on the explicit one.

				// Use the absolute value because we only support "exact" line spacing
				decimal mptPubBaseLineSpacing = (decimal)Math.Abs(publication.BaseLineSpacing);
				m_leadingFactor = (publication.BaseFontSize == 0 || publication.BaseLineSpacing == 0) ?
					StandardLeadingFactor : mptPubBaseLineSpacing / publication.BaseFontSize;

				SetBaseCharacterSize(publication.BaseFontSize);
				SetBaseLineSpacing(mptPubBaseLineSpacing);
				if (publication.BaseFontSize == 0)
					SetBaseCharacterSize(BaseLineSpacing / m_leadingFactor);
				else if (publication.BaseLineSpacing == 0)
					SetBaseLineSpacing(BaseCharacterSize * m_leadingFactor);

				m_fSaveBaseFontAndLineSizes = true;
			}
			SetFontSizeAndLineSpacing = false;

			m_chkNonStdChoices.Checked = !FollowsStandardSettings;
			UpdateTextSizeCtlStatus();
		}

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
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"></param>
		/// ------------------------------------------------------------------------------------
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
			}
			base.Dispose( disposing );
		}
		#endregion

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.Windows.Forms.TabPage tabMargins;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PageSetupDlg));
			System.Windows.Forms.Label label20;
			System.Windows.Forms.Label label19;
			System.Windows.Forms.Label label14;
			System.Windows.Forms.Label label6;
			System.Windows.Forms.Label label4;
			System.Windows.Forms.Label label3;
			System.Windows.Forms.Label label2;
			System.Windows.Forms.TabPage tabPage;
			System.Windows.Forms.Label label12;
			System.Windows.Forms.Label label13;
			System.Windows.Forms.Label label11;
			System.Windows.Forms.Label label10;
			System.Windows.Forms.Label label27;
			System.Windows.Forms.Label label26;
			System.Windows.Forms.Label label24;
			System.Windows.Forms.Label label21;
			System.Windows.Forms.Label label15;
			System.Windows.Forms.Button m_btnCancel;
			System.Windows.Forms.Button m_btnHelp;
			System.Windows.Forms.Label label5;
			this.m_udmFooter = new SIL.FieldWorks.FwCoreDlgControls.UpDownMeasureControl();
			this.m_udmHeader = new SIL.FieldWorks.FwCoreDlgControls.UpDownMeasureControl();
			this.m_udmGutter = new SIL.FieldWorks.FwCoreDlgControls.UpDownMeasureControl();
			this.m_udmRight = new SIL.FieldWorks.FwCoreDlgControls.UpDownMeasureControl();
			this.m_udmBottom = new SIL.FieldWorks.FwCoreDlgControls.UpDownMeasureControl();
			this.m_udmLeft = new SIL.FieldWorks.FwCoreDlgControls.UpDownMeasureControl();
			this.m_udmTop = new SIL.FieldWorks.FwCoreDlgControls.UpDownMeasureControl();
			this.panelPreviewContainerMargins = new System.Windows.Forms.Panel();
			this.panelPreviewMargins = new System.Windows.Forms.Panel();
			this.pnlPreviewContainer = new System.Windows.Forms.Panel();
			this.panelPreviewPage = new System.Windows.Forms.Panel();
			this.label29 = new System.Windows.Forms.Label();
			this.label30 = new System.Windows.Forms.Label();
			this.pnlDraftOptions = new System.Windows.Forms.Panel();
			this.rdoSingleSide = new System.Windows.Forms.RadioButton();
			this.rdoDoubleSide = new System.Windows.Forms.RadioButton();
			this.pnlTwoColumn = new System.Windows.Forms.Panel();
			this.btnTwoColumn = new System.Windows.Forms.Button();
			this.pnlOneColumn = new System.Windows.Forms.Panel();
			this.btnOneColumn = new System.Windows.Forms.Button();
			this.label23 = new System.Windows.Forms.Label();
			this.lblTwoCol = new System.Windows.Forms.Label();
			this.label33 = new System.Windows.Forms.Label();
			this.lblOneCol = new System.Windows.Forms.Label();
			this.pbDraft = new System.Windows.Forms.PictureBox();
			this.pnlBookFoldOptions = new System.Windows.Forms.Panel();
			this.fwOverrideComboBox1 = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.label7 = new System.Windows.Forms.Label();
			this.cboPubPageSize = new System.Windows.Forms.ComboBox();
			this.pbBookFold = new System.Windows.Forms.PictureBox();
			this.label9 = new System.Windows.Forms.Label();
			this.label28 = new System.Windows.Forms.Label();
			this.m_udmPaperHeight = new SIL.FieldWorks.FwCoreDlgControls.UpDownMeasureControl();
			this.label8 = new System.Windows.Forms.Label();
			this.m_udmPaperWidth = new SIL.FieldWorks.FwCoreDlgControls.UpDownMeasureControl();
			this.cbPaperSize = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.label16 = new System.Windows.Forms.Label();
			this.m_btnOK = new System.Windows.Forms.Button();
			this.tabControl1 = new System.Windows.Forms.TabControl();
			this.tabLayout = new System.Windows.Forms.TabPage();
			this.panelPreviewContainerText = new System.Windows.Forms.Panel();
			this.panelPreviewText = new System.Windows.Forms.Panel();
			this.label31 = new System.Windows.Forms.Label();
			this.label32 = new System.Windows.Forms.Label();
			this.m_lblLineSpacing = new System.Windows.Forms.Label();
			this.m_nudLineSpacing = new System.Windows.Forms.NumericUpDown();
			this.label18 = new System.Windows.Forms.Label();
			this.m_nudBaseCharSize = new System.Windows.Forms.NumericUpDown();
			this.m_chkNonStdChoices = new System.Windows.Forms.CheckBox();
			this.label1 = new System.Windows.Forms.Label();
			this.btnHeaderFooter = new System.Windows.Forms.Button();
			this.m_cboSeparator = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.m_ckbDiffFirstHF = new System.Windows.Forms.CheckBox();
			this.m_ckbDiffEvenHF = new System.Windows.Forms.CheckBox();
			this.m_cbBookStart = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.label25 = new System.Windows.Forms.Label();
			this.label22 = new System.Windows.Forms.Label();
			this.label17 = new System.Windows.Forms.Label();
			this.panel3 = new System.Windows.Forms.Panel();
			tabMargins = new System.Windows.Forms.TabPage();
			label20 = new System.Windows.Forms.Label();
			label19 = new System.Windows.Forms.Label();
			label14 = new System.Windows.Forms.Label();
			label6 = new System.Windows.Forms.Label();
			label4 = new System.Windows.Forms.Label();
			label3 = new System.Windows.Forms.Label();
			label2 = new System.Windows.Forms.Label();
			tabPage = new System.Windows.Forms.TabPage();
			label12 = new System.Windows.Forms.Label();
			label13 = new System.Windows.Forms.Label();
			label11 = new System.Windows.Forms.Label();
			label10 = new System.Windows.Forms.Label();
			label27 = new System.Windows.Forms.Label();
			label26 = new System.Windows.Forms.Label();
			label24 = new System.Windows.Forms.Label();
			label21 = new System.Windows.Forms.Label();
			label15 = new System.Windows.Forms.Label();
			m_btnCancel = new System.Windows.Forms.Button();
			m_btnHelp = new System.Windows.Forms.Button();
			label5 = new System.Windows.Forms.Label();
			tabMargins.SuspendLayout();
			this.panelPreviewContainerMargins.SuspendLayout();
			tabPage.SuspendLayout();
			this.pnlPreviewContainer.SuspendLayout();
			this.pnlDraftOptions.SuspendLayout();
			this.pnlTwoColumn.SuspendLayout();
			this.pnlOneColumn.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.pbDraft)).BeginInit();
			this.pnlBookFoldOptions.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.pbBookFold)).BeginInit();
			this.tabControl1.SuspendLayout();
			this.tabLayout.SuspendLayout();
			this.panelPreviewContainerText.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_nudLineSpacing)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_nudBaseCharSize)).BeginInit();
			this.SuspendLayout();
			//
			// tabMargins
			//
			tabMargins.Controls.Add(this.m_udmFooter);
			tabMargins.Controls.Add(this.m_udmHeader);
			tabMargins.Controls.Add(label20);
			tabMargins.Controls.Add(label19);
			tabMargins.Controls.Add(this.m_udmGutter);
			tabMargins.Controls.Add(label14);
			tabMargins.Controls.Add(this.m_udmRight);
			tabMargins.Controls.Add(this.m_udmBottom);
			tabMargins.Controls.Add(this.m_udmLeft);
			tabMargins.Controls.Add(this.m_udmTop);
			tabMargins.Controls.Add(label6);
			tabMargins.Controls.Add(label4);
			tabMargins.Controls.Add(label3);
			tabMargins.Controls.Add(label2);
			tabMargins.Controls.Add(this.panelPreviewContainerMargins);
			resources.ApplyResources(tabMargins, "tabMargins");
			tabMargins.Name = "tabMargins";
			tabMargins.UseVisualStyleBackColor = true;
			//
			// m_udmFooter
			//
			this.m_udmFooter.DisplayAbsoluteValues = false;
			resources.ApplyResources(this.m_udmFooter, "m_udmFooter");
			this.m_udmFooter.MeasureIncrementFactor = ((uint)(1u));
			this.m_udmFooter.MeasureMax = 720000;
			this.m_udmFooter.MeasureMin = 0;
			this.m_udmFooter.MeasureType = SIL.FieldWorks.Common.FwUtils.MsrSysType.Inch;
			this.m_udmFooter.MeasureValue = 36720;
			this.m_udmFooter.Name = "m_udmFooter";
			this.m_udmFooter.TextChanged += new System.EventHandler(this.m_udmFooter_TextChanged);
			//
			// m_udmHeader
			//
			this.m_udmHeader.DisplayAbsoluteValues = false;
			resources.ApplyResources(this.m_udmHeader, "m_udmHeader");
			this.m_udmHeader.MeasureIncrementFactor = ((uint)(1u));
			this.m_udmHeader.MeasureMax = 720000;
			this.m_udmHeader.MeasureMin = 0;
			this.m_udmHeader.MeasureType = SIL.FieldWorks.Common.FwUtils.MsrSysType.Inch;
			this.m_udmHeader.MeasureValue = 36720;
			this.m_udmHeader.Name = "m_udmHeader";
			this.m_udmHeader.TextChanged += new System.EventHandler(this.m_udmHeader_TextChanged);
			//
			// label20
			//
			resources.ApplyResources(label20, "label20");
			label20.Name = "label20";
			//
			// label19
			//
			resources.ApplyResources(label19, "label19");
			label19.Name = "label19";
			//
			// m_udmGutter
			//
			this.m_udmGutter.DisplayAbsoluteValues = false;
			resources.ApplyResources(this.m_udmGutter, "m_udmGutter");
			this.m_udmGutter.MeasureIncrementFactor = ((uint)(1u));
			this.m_udmGutter.MeasureMax = 144000;
			this.m_udmGutter.MeasureMin = 0;
			this.m_udmGutter.MeasureType = SIL.FieldWorks.Common.FwUtils.MsrSysType.Inch;
			this.m_udmGutter.MeasureValue = 0;
			this.m_udmGutter.Name = "m_udmGutter";
			this.m_udmGutter.TextChanged += new System.EventHandler(this.m_udmGutter_TextChanged);
			//
			// label14
			//
			resources.ApplyResources(label14, "label14");
			label14.Name = "label14";
			//
			// m_udmRight
			//
			this.m_udmRight.DisplayAbsoluteValues = false;
			resources.ApplyResources(this.m_udmRight, "m_udmRight");
			this.m_udmRight.MeasureIncrementFactor = ((uint)(1u));
			this.m_udmRight.MeasureMax = 720000;
			this.m_udmRight.MeasureMin = 0;
			this.m_udmRight.MeasureType = SIL.FieldWorks.Common.FwUtils.MsrSysType.Inch;
			this.m_udmRight.MeasureValue = 70560;
			this.m_udmRight.Name = "m_udmRight";
			this.m_udmRight.TextChanged += new System.EventHandler(this.m_udmRight_TextChanged);
			//
			// m_udmBottom
			//
			this.m_udmBottom.DisplayAbsoluteValues = false;
			resources.ApplyResources(this.m_udmBottom, "m_udmBottom");
			this.m_udmBottom.MeasureIncrementFactor = ((uint)(1u));
			this.m_udmBottom.MeasureMax = 720000;
			this.m_udmBottom.MeasureMin = 0;
			this.m_udmBottom.MeasureType = SIL.FieldWorks.Common.FwUtils.MsrSysType.Inch;
			this.m_udmBottom.MeasureValue = 70560;
			this.m_udmBottom.Name = "m_udmBottom";
			this.m_udmBottom.TextChanged += new System.EventHandler(this.m_udmBottom_TextChanged);
			this.m_udmBottom.Click += new System.EventHandler(this.m_udmBottom_Click);
			//
			// m_udmLeft
			//
			this.m_udmLeft.DisplayAbsoluteValues = false;
			resources.ApplyResources(this.m_udmLeft, "m_udmLeft");
			this.m_udmLeft.MeasureIncrementFactor = ((uint)(1u));
			this.m_udmLeft.MeasureMax = 720000;
			this.m_udmLeft.MeasureMin = 0;
			this.m_udmLeft.MeasureType = SIL.FieldWorks.Common.FwUtils.MsrSysType.Inch;
			this.m_udmLeft.MeasureValue = 70560;
			this.m_udmLeft.Name = "m_udmLeft";
			this.m_udmLeft.TextChanged += new System.EventHandler(this.m_udmLeft_TextChanged);
			//
			// m_udmTop
			//
			this.m_udmTop.DisplayAbsoluteValues = false;
			resources.ApplyResources(this.m_udmTop, "m_udmTop");
			this.m_udmTop.MeasureIncrementFactor = ((uint)(1u));
			this.m_udmTop.MeasureMax = 720000;
			this.m_udmTop.MeasureMin = 0;
			this.m_udmTop.MeasureType = SIL.FieldWorks.Common.FwUtils.MsrSysType.Inch;
			this.m_udmTop.MeasureValue = 70560;
			this.m_udmTop.Name = "m_udmTop";
			this.m_udmTop.TextChanged += new System.EventHandler(this.m_udmTop_TextChanged);
			//
			// label6
			//
			resources.ApplyResources(label6, "label6");
			label6.Name = "label6";
			//
			// label4
			//
			resources.ApplyResources(label4, "label4");
			label4.Name = "label4";
			//
			// label3
			//
			resources.ApplyResources(label3, "label3");
			label3.Name = "label3";
			//
			// label2
			//
			resources.ApplyResources(label2, "label2");
			label2.Name = "label2";
			//
			// panelPreviewContainerMargins
			//
			this.panelPreviewContainerMargins.Controls.Add(this.panelPreviewMargins);
			resources.ApplyResources(this.panelPreviewContainerMargins, "panelPreviewContainerMargins");
			this.panelPreviewContainerMargins.Name = "panelPreviewContainerMargins";
			//
			// panelPreviewMargins
			//
			this.panelPreviewMargins.BackColor = System.Drawing.SystemColors.Window;
			this.panelPreviewMargins.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			resources.ApplyResources(this.panelPreviewMargins, "panelPreviewMargins");
			this.panelPreviewMargins.Name = "panelPreviewMargins";
			this.panelPreviewMargins.Paint += new System.Windows.Forms.PaintEventHandler(this.panelPreview_Paint);
			//
			// tabPage
			//
			tabPage.Controls.Add(this.pnlPreviewContainer);
			tabPage.Controls.Add(this.label29);
			tabPage.Controls.Add(this.label30);
			tabPage.Controls.Add(this.pnlDraftOptions);
			tabPage.Controls.Add(this.pnlTwoColumn);
			tabPage.Controls.Add(this.pnlOneColumn);
			tabPage.Controls.Add(this.label23);
			tabPage.Controls.Add(this.lblTwoCol);
			tabPage.Controls.Add(this.label33);
			tabPage.Controls.Add(this.lblOneCol);
			tabPage.Controls.Add(this.pbDraft);
			tabPage.Controls.Add(this.pnlBookFoldOptions);
			tabPage.Controls.Add(this.cboPubPageSize);
			tabPage.Controls.Add(this.pbBookFold);
			tabPage.Controls.Add(this.label9);
			tabPage.Controls.Add(label12);
			tabPage.Controls.Add(this.label28);
			tabPage.Controls.Add(this.m_udmPaperHeight);
			tabPage.Controls.Add(this.label8);
			tabPage.Controls.Add(label13);
			tabPage.Controls.Add(this.m_udmPaperWidth);
			tabPage.Controls.Add(this.cbPaperSize);
			tabPage.Controls.Add(label11);
			tabPage.Controls.Add(label10);
			tabPage.Controls.Add(this.label16);
			resources.ApplyResources(tabPage, "tabPage");
			tabPage.Name = "tabPage";
			tabPage.UseVisualStyleBackColor = true;
			//
			// pnlPreviewContainer
			//
			resources.ApplyResources(this.pnlPreviewContainer, "pnlPreviewContainer");
			this.pnlPreviewContainer.Controls.Add(this.panelPreviewPage);
			this.pnlPreviewContainer.Name = "pnlPreviewContainer";
			//
			// panelPreviewPage
			//
			this.panelPreviewPage.BackColor = System.Drawing.SystemColors.Window;
			this.panelPreviewPage.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			resources.ApplyResources(this.panelPreviewPage, "panelPreviewPage");
			this.panelPreviewPage.Name = "panelPreviewPage";
			this.panelPreviewPage.Paint += new System.Windows.Forms.PaintEventHandler(this.panelPreview_Paint);
			//
			// label29
			//
			resources.ApplyResources(this.label29, "label29");
			this.label29.Name = "label29";
			//
			// label30
			//
			resources.ApplyResources(this.label30, "label30");
			this.label30.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.label30.Name = "label30";
			//
			// pnlDraftOptions
			//
			this.pnlDraftOptions.Controls.Add(this.rdoSingleSide);
			this.pnlDraftOptions.Controls.Add(this.rdoDoubleSide);
			resources.ApplyResources(this.pnlDraftOptions, "pnlDraftOptions");
			this.pnlDraftOptions.Name = "pnlDraftOptions";
			//
			// rdoSingleSide
			//
			resources.ApplyResources(this.rdoSingleSide, "rdoSingleSide");
			this.rdoSingleSide.Name = "rdoSingleSide";
			this.rdoSingleSide.TabStop = true;
			this.rdoSingleSide.UseVisualStyleBackColor = true;
			//
			// rdoDoubleSide
			//
			resources.ApplyResources(this.rdoDoubleSide, "rdoDoubleSide");
			this.rdoDoubleSide.Name = "rdoDoubleSide";
			this.rdoDoubleSide.TabStop = true;
			this.rdoDoubleSide.UseVisualStyleBackColor = true;
			//
			// pnlTwoColumn
			//
			this.pnlTwoColumn.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.pnlTwoColumn.Controls.Add(this.btnTwoColumn);
			resources.ApplyResources(this.pnlTwoColumn, "pnlTwoColumn");
			this.pnlTwoColumn.Name = "pnlTwoColumn";
			this.pnlTwoColumn.TabStop = true;
			//
			// btnTwoColumn
			//
			this.btnTwoColumn.BackColor = System.Drawing.Color.Transparent;
			this.btnTwoColumn.FlatAppearance.BorderColor = System.Drawing.Color.White;
			this.btnTwoColumn.FlatAppearance.BorderSize = 0;
			resources.ApplyResources(this.btnTwoColumn, "btnTwoColumn");
			this.btnTwoColumn.ForeColor = System.Drawing.Color.Transparent;
			this.btnTwoColumn.Name = "btnTwoColumn";
			this.btnTwoColumn.UseVisualStyleBackColor = false;
			this.btnTwoColumn.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.btnTwoColumn_PreviewKeyDown);
			this.btnTwoColumn.Click += new System.EventHandler(this.ColumnButton_Click);
			//
			// pnlOneColumn
			//
			this.pnlOneColumn.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.pnlOneColumn.Controls.Add(this.btnOneColumn);
			resources.ApplyResources(this.pnlOneColumn, "pnlOneColumn");
			this.pnlOneColumn.Name = "pnlOneColumn";
			this.pnlOneColumn.TabStop = true;
			//
			// btnOneColumn
			//
			this.btnOneColumn.BackColor = System.Drawing.Color.Transparent;
			this.btnOneColumn.FlatAppearance.BorderColor = System.Drawing.Color.White;
			this.btnOneColumn.FlatAppearance.BorderSize = 0;
			resources.ApplyResources(this.btnOneColumn, "btnOneColumn");
			this.btnOneColumn.ForeColor = System.Drawing.Color.Transparent;
			this.btnOneColumn.Name = "btnOneColumn";
			this.btnOneColumn.UseVisualStyleBackColor = false;
			this.btnOneColumn.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.btnOneColumn_PreviewKeyDown);
			this.btnOneColumn.Click += new System.EventHandler(this.ColumnButton_Click);
			//
			// label23
			//
			resources.ApplyResources(this.label23, "label23");
			this.label23.Name = "label23";
			//
			// lblTwoCol
			//
			resources.ApplyResources(this.lblTwoCol, "lblTwoCol");
			this.lblTwoCol.Name = "lblTwoCol";
			//
			// label33
			//
			resources.ApplyResources(this.label33, "label33");
			this.label33.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.label33.Name = "label33";
			//
			// lblOneCol
			//
			resources.ApplyResources(this.lblOneCol, "lblOneCol");
			this.lblOneCol.Name = "lblOneCol";
			//
			// pbDraft
			//
			this.pbDraft.BackColor = System.Drawing.Color.White;
			this.pbDraft.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			resources.ApplyResources(this.pbDraft, "pbDraft");
			this.pbDraft.Name = "pbDraft";
			this.pbDraft.TabStop = false;
			this.pbDraft.Click += new System.EventHandler(this.pbDraft_Click);
			//
			// pnlBookFoldOptions
			//
			this.pnlBookFoldOptions.Controls.Add(this.fwOverrideComboBox1);
			this.pnlBookFoldOptions.Controls.Add(this.label7);
			resources.ApplyResources(this.pnlBookFoldOptions, "pnlBookFoldOptions");
			this.pnlBookFoldOptions.Name = "pnlBookFoldOptions";
			//
			// fwOverrideComboBox1
			//
			this.fwOverrideComboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.fwOverrideComboBox1, "fwOverrideComboBox1");
			this.fwOverrideComboBox1.Items.AddRange(new object[] {
			resources.GetString("fwOverrideComboBox1.Items"),
			resources.GetString("fwOverrideComboBox1.Items1"),
			resources.GetString("fwOverrideComboBox1.Items2"),
			resources.GetString("fwOverrideComboBox1.Items3"),
			resources.GetString("fwOverrideComboBox1.Items4"),
			resources.GetString("fwOverrideComboBox1.Items5"),
			resources.GetString("fwOverrideComboBox1.Items6"),
			resources.GetString("fwOverrideComboBox1.Items7"),
			resources.GetString("fwOverrideComboBox1.Items8"),
			resources.GetString("fwOverrideComboBox1.Items9"),
			resources.GetString("fwOverrideComboBox1.Items10")});
			this.fwOverrideComboBox1.Name = "fwOverrideComboBox1";
			this.fwOverrideComboBox1.Sorted = true;
			//
			// label7
			//
			resources.ApplyResources(this.label7, "label7");
			this.label7.Name = "label7";
			//
			// cboPubPageSize
			//
			this.cboPubPageSize.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cboPubPageSize.FormattingEnabled = true;
			this.cboPubPageSize.Items.AddRange(new object[] {
			resources.GetString("cboPubPageSize.Items"),
			resources.GetString("cboPubPageSize.Items1"),
			resources.GetString("cboPubPageSize.Items2")});
			resources.ApplyResources(this.cboPubPageSize, "cboPubPageSize");
			this.cboPubPageSize.Name = "cboPubPageSize";
			this.cboPubPageSize.SelectedIndexChanged += new System.EventHandler(this.cboPubPageSize_SelectedIndexChanged);
			//
			// pbBookFold
			//
			this.pbBookFold.BackColor = System.Drawing.Color.White;
			this.pbBookFold.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			resources.ApplyResources(this.pbBookFold, "pbBookFold");
			this.pbBookFold.Name = "pbBookFold";
			this.pbBookFold.TabStop = false;
			this.pbBookFold.Click += new System.EventHandler(this.pbBookFold_Click);
			//
			// label9
			//
			resources.ApplyResources(this.label9, "label9");
			this.label9.Name = "label9";
			//
			// label12
			//
			resources.ApplyResources(label12, "label12");
			label12.Name = "label12";
			//
			// label28
			//
			resources.ApplyResources(this.label28, "label28");
			this.label28.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.label28.Name = "label28";
			//
			// m_udmPaperHeight
			//
			this.m_udmPaperHeight.DisplayAbsoluteValues = false;
			resources.ApplyResources(this.m_udmPaperHeight, "m_udmPaperHeight");
			this.m_udmPaperHeight.MeasureIncrementFactor = ((uint)(1u));
			this.m_udmPaperHeight.MeasureMax = 3600000;
			this.m_udmPaperHeight.MeasureMin = 7200;
			this.m_udmPaperHeight.MeasureType = SIL.FieldWorks.Common.FwUtils.MsrSysType.Inch;
			this.m_udmPaperHeight.MeasureValue = 790560;
			this.m_udmPaperHeight.Name = "m_udmPaperHeight";
			this.m_udmPaperHeight.Changed += new System.EventHandler(this.m_udmPaperWidthOrHeight_Changed);
			//
			// label8
			//
			resources.ApplyResources(this.label8, "label8");
			this.label8.Name = "label8";
			//
			// label13
			//
			resources.ApplyResources(label13, "label13");
			label13.Name = "label13";
			//
			// m_udmPaperWidth
			//
			this.m_udmPaperWidth.DisplayAbsoluteValues = false;
			resources.ApplyResources(this.m_udmPaperWidth, "m_udmPaperWidth");
			this.m_udmPaperWidth.MeasureIncrementFactor = ((uint)(1u));
			this.m_udmPaperWidth.MeasureMax = 3600000;
			this.m_udmPaperWidth.MeasureMin = 7200;
			this.m_udmPaperWidth.MeasureType = SIL.FieldWorks.Common.FwUtils.MsrSysType.Inch;
			this.m_udmPaperWidth.MeasureValue = 612000;
			this.m_udmPaperWidth.Name = "m_udmPaperWidth";
			this.m_udmPaperWidth.Changed += new System.EventHandler(this.m_udmPaperWidthOrHeight_Changed);
			//
			// cbPaperSize
			//
			this.cbPaperSize.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.cbPaperSize, "cbPaperSize");
			this.cbPaperSize.Name = "cbPaperSize";
			this.cbPaperSize.SelectedIndexChanged += new System.EventHandler(this.cbPaperSize_SelectedIndexChanged);
			//
			// label11
			//
			resources.ApplyResources(label11, "label11");
			label11.Name = "label11";
			//
			// label10
			//
			resources.ApplyResources(label10, "label10");
			label10.Name = "label10";
			//
			// label16
			//
			resources.ApplyResources(this.label16, "label16");
			this.label16.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.label16.Name = "label16";
			//
			// label27
			//
			resources.ApplyResources(label27, "label27");
			label27.Name = "label27";
			//
			// label26
			//
			resources.ApplyResources(label26, "label26");
			label26.Name = "label26";
			//
			// label24
			//
			resources.ApplyResources(label24, "label24");
			label24.Name = "label24";
			//
			// label21
			//
			resources.ApplyResources(label21, "label21");
			label21.Name = "label21";
			//
			// label15
			//
			resources.ApplyResources(label15, "label15");
			label15.Name = "label15";
			//
			// m_btnCancel
			//
			resources.ApplyResources(m_btnCancel, "m_btnCancel");
			m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			m_btnCancel.Name = "m_btnCancel";
			//
			// m_btnHelp
			//
			resources.ApplyResources(m_btnHelp, "m_btnHelp");
			m_btnHelp.Name = "m_btnHelp";
			m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// label5
			//
			resources.ApplyResources(label5, "label5");
			label5.Name = "label5";
			//
			// m_btnOK
			//
			resources.ApplyResources(this.m_btnOK, "m_btnOK");
			this.m_btnOK.Name = "m_btnOK";
			this.m_btnOK.Click += new System.EventHandler(this.m_btnOK_Click);
			//
			// tabControl1
			//
			resources.ApplyResources(this.tabControl1, "tabControl1");
			this.tabControl1.Controls.Add(tabPage);
			this.tabControl1.Controls.Add(tabMargins);
			this.tabControl1.Controls.Add(this.tabLayout);
			this.tabControl1.Name = "tabControl1";
			this.tabControl1.SelectedIndex = 0;
			//
			// tabLayout
			//
			this.tabLayout.Controls.Add(this.panelPreviewContainerText);
			this.tabLayout.Controls.Add(this.label31);
			this.tabLayout.Controls.Add(this.label32);
			this.tabLayout.Controls.Add(label21);
			this.tabLayout.Controls.Add(label15);
			this.tabLayout.Controls.Add(this.m_lblLineSpacing);
			this.tabLayout.Controls.Add(this.m_nudLineSpacing);
			this.tabLayout.Controls.Add(this.label18);
			this.tabLayout.Controls.Add(this.m_nudBaseCharSize);
			this.tabLayout.Controls.Add(this.m_chkNonStdChoices);
			this.tabLayout.Controls.Add(label5);
			this.tabLayout.Controls.Add(this.label1);
			this.tabLayout.Controls.Add(this.btnHeaderFooter);
			this.tabLayout.Controls.Add(this.m_cboSeparator);
			this.tabLayout.Controls.Add(label27);
			this.tabLayout.Controls.Add(this.m_ckbDiffFirstHF);
			this.tabLayout.Controls.Add(this.m_ckbDiffEvenHF);
			this.tabLayout.Controls.Add(this.m_cbBookStart);
			this.tabLayout.Controls.Add(label26);
			this.tabLayout.Controls.Add(this.label25);
			this.tabLayout.Controls.Add(label24);
			this.tabLayout.Controls.Add(this.label22);
			this.tabLayout.Controls.Add(this.label17);
			resources.ApplyResources(this.tabLayout, "tabLayout");
			this.tabLayout.Name = "tabLayout";
			this.tabLayout.UseVisualStyleBackColor = true;
			//
			// panelPreviewContainerText
			//
			resources.ApplyResources(this.panelPreviewContainerText, "panelPreviewContainerText");
			this.panelPreviewContainerText.Controls.Add(this.panelPreviewText);
			this.panelPreviewContainerText.Name = "panelPreviewContainerText";
			//
			// panelPreviewText
			//
			this.panelPreviewText.BackColor = System.Drawing.SystemColors.Window;
			this.panelPreviewText.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			resources.ApplyResources(this.panelPreviewText, "panelPreviewText");
			this.panelPreviewText.Name = "panelPreviewText";
			this.panelPreviewText.Paint += new System.Windows.Forms.PaintEventHandler(this.panelPreview_Paint);
			//
			// label31
			//
			resources.ApplyResources(this.label31, "label31");
			this.label31.Name = "label31";
			//
			// label32
			//
			resources.ApplyResources(this.label32, "label32");
			this.label32.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.label32.Name = "label32";
			//
			// m_lblLineSpacing
			//
			resources.ApplyResources(this.m_lblLineSpacing, "m_lblLineSpacing");
			this.m_lblLineSpacing.Name = "m_lblLineSpacing";
			//
			// m_nudLineSpacing
			//
			resources.ApplyResources(this.m_nudLineSpacing, "m_nudLineSpacing");
			this.m_nudLineSpacing.Maximum = new decimal(new int[] {
			120,
			0,
			0,
			0});
			this.m_nudLineSpacing.Minimum = new decimal(new int[] {
			4,
			0,
			0,
			0});
			this.m_nudLineSpacing.Name = "m_nudLineSpacing";
			this.m_nudLineSpacing.Value = new decimal(new int[] {
			4,
			0,
			0,
			0});
			this.m_nudLineSpacing.ValueChanged += new System.EventHandler(this.m_nudLineSpacing_ValueChanged);
			//
			// label18
			//
			resources.ApplyResources(this.label18, "label18");
			this.label18.Name = "label18";
			//
			// m_nudBaseCharSize
			//
			resources.ApplyResources(this.m_nudBaseCharSize, "m_nudBaseCharSize");
			this.m_nudBaseCharSize.Maximum = new decimal(new int[] {
			120,
			0,
			0,
			0});
			this.m_nudBaseCharSize.Minimum = new decimal(new int[] {
			4,
			0,
			0,
			0});
			this.m_nudBaseCharSize.Name = "m_nudBaseCharSize";
			this.m_nudBaseCharSize.Value = new decimal(new int[] {
			4,
			0,
			0,
			0});
			this.m_nudBaseCharSize.ValueChanged += new System.EventHandler(this.m_nudBaseCharSize_ValueChanged);
			//
			// m_chkNonStdChoices
			//
			resources.ApplyResources(this.m_chkNonStdChoices, "m_chkNonStdChoices");
			this.m_chkNonStdChoices.Checked = true;
			this.m_chkNonStdChoices.CheckState = System.Windows.Forms.CheckState.Checked;
			this.m_chkNonStdChoices.Name = "m_chkNonStdChoices";
			this.m_chkNonStdChoices.UseVisualStyleBackColor = true;
			this.m_chkNonStdChoices.CheckedChanged += new System.EventHandler(this.m_chkNonStdChoices_CheckedChanged);
			//
			// label1
			//
			resources.ApplyResources(this.label1, "label1");
			this.label1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.label1.Name = "label1";
			//
			// btnHeaderFooter
			//
			resources.ApplyResources(this.btnHeaderFooter, "btnHeaderFooter");
			this.btnHeaderFooter.Name = "btnHeaderFooter";
			this.btnHeaderFooter.UseVisualStyleBackColor = true;
			this.btnHeaderFooter.Click += new System.EventHandler(this.btnHeaderFooter_Click);
			//
			// m_cboSeparator
			//
			this.m_cboSeparator.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.m_cboSeparator, "m_cboSeparator");
			this.m_cboSeparator.Items.AddRange(new object[] {
			resources.GetString("m_cboSeparator.Items"),
			resources.GetString("m_cboSeparator.Items1"),
			resources.GetString("m_cboSeparator.Items2")});
			this.m_cboSeparator.Name = "m_cboSeparator";
			//
			// m_ckbDiffFirstHF
			//
			resources.ApplyResources(this.m_ckbDiffFirstHF, "m_ckbDiffFirstHF");
			this.m_ckbDiffFirstHF.Name = "m_ckbDiffFirstHF";
			//
			// m_ckbDiffEvenHF
			//
			resources.ApplyResources(this.m_ckbDiffEvenHF, "m_ckbDiffEvenHF");
			this.m_ckbDiffEvenHF.Name = "m_ckbDiffEvenHF";
			//
			// m_cbBookStart
			//
			this.m_cbBookStart.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.m_cbBookStart, "m_cbBookStart");
			this.m_cbBookStart.Items.AddRange(new object[] {
			resources.GetString("m_cbBookStart.Items"),
			resources.GetString("m_cbBookStart.Items1"),
			resources.GetString("m_cbBookStart.Items2")});
			this.m_cbBookStart.Name = "m_cbBookStart";
			this.m_cbBookStart.Sorted = true;
			//
			// label25
			//
			resources.ApplyResources(this.label25, "label25");
			this.label25.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.label25.Name = "label25";
			//
			// label22
			//
			resources.ApplyResources(this.label22, "label22");
			this.label22.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.label22.Name = "label22";
			//
			// label17
			//
			resources.ApplyResources(this.label17, "label17");
			this.label17.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.label17.Name = "label17";
			//
			// panel3
			//
			this.panel3.BackColor = System.Drawing.SystemColors.Window;
			this.panel3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			resources.ApplyResources(this.panel3, "panel3");
			this.panel3.Name = "panel3";
			//
			// PageSetupDlg
			//
			this.AcceptButton = this.m_btnOK;
			resources.ApplyResources(this, "$this");
			this.CancelButton = m_btnCancel;
			this.ControlBox = false;
			this.Controls.Add(m_btnHelp);
			this.Controls.Add(m_btnCancel);
			this.Controls.Add(this.m_btnOK);
			this.Controls.Add(this.tabControl1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "PageSetupDlg";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			tabMargins.ResumeLayout(false);
			tabMargins.PerformLayout();
			this.panelPreviewContainerMargins.ResumeLayout(false);
			tabPage.ResumeLayout(false);
			tabPage.PerformLayout();
			this.pnlPreviewContainer.ResumeLayout(false);
			this.pnlDraftOptions.ResumeLayout(false);
			this.pnlDraftOptions.PerformLayout();
			this.pnlTwoColumn.ResumeLayout(false);
			this.pnlOneColumn.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.pbDraft)).EndInit();
			this.pnlBookFoldOptions.ResumeLayout(false);
			this.pnlBookFoldOptions.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.pbBookFold)).EndInit();
			this.tabControl1.ResumeLayout(false);
			this.tabLayout.ResumeLayout(false);
			this.tabLayout.PerformLayout();
			this.panelPreviewContainerText.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_nudLineSpacing)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_nudBaseCharSize)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion

		#region Event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the pbDraft control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void pbDraft_Click(object sender, System.EventArgs e)
		{
			pbDraft.Image = ResourceHelper.PortraitSelectedIcon;
			pbBookFold.Image = ResourceHelper.BookFoldIcon;
			m_fBookFoldCurrent = false;
			pnlDraftOptions.Visible = true;
			pnlBookFoldOptions.Visible = false;
			if (m_udmPaperHeight.MeasureValue < m_udmPaperWidth.MeasureValue)
			{
				int mpt = m_udmPaperHeight.MeasureValue;
				m_udmPaperHeight.MeasureValue = m_udmPaperWidth.MeasureValue;
				m_udmPaperWidth.MeasureValue = mpt;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the pbBookFold control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void pbBookFold_Click(object sender, System.EventArgs e)
		{
			// selecting Landscape
			pbDraft.Image = ResourceHelper.PortraitIcon;
			pbBookFold.Image = ResourceHelper.BookFoldSelectedIcon;
			m_fBookFoldCurrent = true;
			pnlDraftOptions.Visible = false;
			pnlBookFoldOptions.Visible = true;
			if (m_udmPaperHeight.MeasureValue > m_udmPaperWidth.MeasureValue)
			{
				int mpt = m_udmPaperHeight.MeasureValue;
				m_udmPaperHeight.MeasureValue = m_udmPaperWidth.MeasureValue;
				m_udmPaperWidth.MeasureValue = mpt;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Show help for this dialog.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_btnHelp_Click(object sender, System.EventArgs e)
		{
			string helpTopicKey;

			switch (tabControl1.SelectedIndex)
			{
				case ktabMargins:
					helpTopicKey = "khtpPageSetup_Margins";
					break;
				case ktabPaper:
					helpTopicKey = "khtpPageSetup_Paper";
					break;
				case ktabText:
					helpTopicKey = "khtpPageSetup_Text";
					break;
				default:
					helpTopicKey = "khtpNoHelpTopic";
					break;
			}
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, helpTopicKey);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the paper size is changed.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void cbPaperSize_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			if (!m_AdjustingPaperSize)
			{
				m_fChangingIndex = true;
				PaperSize paperSize = (PaperSize)cbPaperSize.SelectedItem;
				if (paperSize.PaperName != ResourceHelper.GetResourceString("kstidPaperSizeCustom"))
				{
					// Only change the width and height to the selected paper size if it is not
					// custom. Custom will keep the current height and width but the user can
					// change the values.
					m_udmPaperWidth.MeasureValue = paperSize.Width * kCentiInchToMilliPoints;
					m_udmPaperHeight.MeasureValue = paperSize.Height * kCentiInchToMilliPoints;
				}
				m_fSavePaperSize = true;

				AdjustPaperSizePreview();

				m_fChangingIndex = false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adjusts the paper size preview panel.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void AdjustPaperSizePreview()
		{
			// Adjust the preview panel size
			panelPreviewPage.Width = m_udmPaperWidth.MeasureValue * panelPreviewPage.Height / m_udmPaperHeight.MeasureValue;
			if (panelPreviewPage.Width > pnlPreviewContainer.Width)
			{
				// If the width gets too wide to show and still keep the proportions correct, then adjust the height
				panelPreviewPage.Width = pnlPreviewContainer.Width;
				panelPreviewPage.Height = m_udmPaperHeight.MeasureValue * panelPreviewPage.Width / m_udmPaperWidth.MeasureValue;
				panelPreviewPage.Top = (pnlPreviewContainer.Height - panelPreviewPage.Height) / 2;
			}
			panelPreviewPage.Left = (pnlPreviewContainer.Width - panelPreviewPage.Width) / 2;
			panelPreviewPage.Invalidate();
			panelPreviewMargins.Bounds = panelPreviewPage.Bounds;
			panelPreviewText.Bounds = panelPreviewPage.Bounds;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the paper width is changed
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_udmPaperWidthOrHeight_Changed(object sender, System.EventArgs e)
		{
			if (!m_fChangingIndex)
			{
				AdjustPaperSize(m_udmPaperWidth.MeasureValue, m_udmPaperHeight.MeasureValue);
				m_fSavePaperSize = true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the SelectedIndexChanged event of the cboPubPageSize control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void cboPubPageSize_SelectedIndexChanged(object sender, EventArgs e)
		{
			//if (m_fIsTrialPub)
			//{
			//    switch (((ComboBox)sender).SelectedIndex)
			//    {
			//        case 0: // Small Bible
			//            m_pageSize = PubPageSizeType.SmallBible;
			//            break;
			//        case 1: // Large Bible
			//            m_pageSize = PubPageSizeType.LargeBible;
			//            break;
			//        default:
			//            break;
			//    }
			//}
			//else
			//{
			//    switch (((ComboBox)sender).SelectedIndex)
			//    {
			//        case 0: // Full page
			//            m_pageSize = PubPageSizeType.FullPage;
			//            break;
			//        case 1: // Small Bible
			//            m_pageSize = PubPageSizeType.SmallBible;
			//            break;
			//        case 2: // Large Bible
			//            m_pageSize = PubPageSizeType.LargeBible;
			//            break;
			//        default:
			//            break;
			//    }
			//}
			//m_chkNonStdChoices.Enabled = m_pageSize != PubPageSizeType.FullPage;
			UpdateMarginControls();
			UpdateBaseSizeControlsMinAndMax();
			ScaleLineSpacing();
			UpdateTextSizeCtlStatus();
			panelPreviewPage.Invalidate();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the ValueChanged event of the m_nudBaseCharSize control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_nudBaseCharSize_ValueChanged(object sender, EventArgs e)
		{
			ScaleLineSpacing();
			m_fSaveBaseFontAndLineSizes = true;
			UpdateMarginControls();
			panelPreviewText.Invalidate();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the ValueChanged event of the m_nudLineSpacing control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_nudLineSpacing_ValueChanged(object sender, EventArgs e)
		{
			ScaleFontSize();
			m_fSaveBaseFontAndLineSizes = true;
			UpdateMarginControls();
			panelPreviewText.Invalidate();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the CheckedChanged event of the m_chkNonStdChoices control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_chkNonStdChoices_CheckedChanged(object sender, EventArgs e)
		{
			UpdateBaseSizeControlsMinAndMax();
			if (!m_chkNonStdChoices.Checked)
				ScaleLineSpacing();
			UpdateTextSizeCtlStatus();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the min and max values for the base font size and line spacing controls.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void UpdateBaseSizeControlsMinAndMax()
		{
			m_nudBaseCharSize.Minimum = 4;
			m_nudBaseCharSize.Maximum = 100;
			if (!m_chkNonStdChoices.Checked)
			{
				m_nudLineSpacing.Value = SnapValueToRange(
					m_nudBaseCharSize.Minimum * StandardLeadingFactor,
					m_nudBaseCharSize.Maximum * StandardLeadingFactor, m_nudLineSpacing.Value);
				m_nudLineSpacing.Minimum = m_nudBaseCharSize.Minimum * StandardLeadingFactor;
			}
			else
				m_nudLineSpacing.Minimum = m_nudBaseCharSize.Minimum;
			m_nudLineSpacing.Maximum = m_nudBaseCharSize.Maximum * StandardLeadingFactor;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the One/Two Column controls.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void ColumnButton_Click(object sender, EventArgs e)
		{
			SetNumberOfColumns(sender == btnOneColumn ? 1 : 2);
			panelPreviewPage.Invalidate();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the m_btnOK control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_btnOK_Click(object sender, EventArgs e)
		{
			PageSetupErrorType error = ValidateDialogSettings();
			if (error != PageSetupErrorType.NoError)
			{
				DisplayErrorMessage(error);
			}
			else
			{
				DialogResult = DialogResult.OK;
				SaveDialogSettings();
				Close();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the TextChanged event of the m_udmGutter control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the
		/// event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_udmGutter_TextChanged(object sender, EventArgs e)
		{
			panelPreviewMargins.Invalidate();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the TextChanged event of the m_udmLeft control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_udmLeft_TextChanged(object sender, EventArgs e)
		{
			m_fSaveMargins = true;
			panelPreviewMargins.Invalidate();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the TextChanged event of the m_udmTop control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_udmTop_TextChanged(object sender, EventArgs e)
		{
			m_fSaveMargins = true;
			Debug.Assert(m_udmHeader.MeasureMin <= m_udmTop.MeasureMin);
			m_udmHeader.MeasureValue = Math.Min(m_udmHeader.MeasureValue, m_udmTop.MeasureValue);
			panelPreviewMargins.Invalidate();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the TextChanged event of the m_udmBottom control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_udmBottom_TextChanged(object sender, EventArgs e)
		{
			m_fSaveMargins = true;
			Debug.Assert(m_udmFooter.MeasureMin <= m_udmBottom.MeasureMin);
			m_udmFooter.MeasureValue = Math.Min(m_udmFooter.MeasureValue, m_udmBottom.MeasureValue);
			panelPreviewMargins.Invalidate();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the TextChanged event of the m_udmRight control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_udmRight_TextChanged(object sender, EventArgs e)
		{
			m_fSaveMargins = true;
			panelPreviewMargins.Invalidate();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the m_udmBottom control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_udmBottom_Click(object sender, EventArgs e)
		{
			m_fSaveMargins = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the TextChanged event of the m_udmHeader control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_udmHeader_TextChanged(object sender, EventArgs e)
		{
			m_fSaveMargins = true;
			Debug.Assert(m_udmHeader.MeasureMax <= m_udmTop.MeasureMax);
			m_udmTop.MeasureValue = Math.Max(m_udmHeader.MeasureValue, m_udmTop.MeasureValue);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the TextChanged event of the m_udmFooter control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_udmFooter_TextChanged(object sender, EventArgs e)
		{
			m_fSaveMargins = true;
			Debug.Assert(m_udmFooter.MeasureMax <= m_udmBottom.MeasureMax);
			m_udmBottom.MeasureValue = Math.Max(m_udmFooter.MeasureValue, m_udmBottom.MeasureValue);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the btnHeaderFooter control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void btnHeaderFooter_Click(object sender, EventArgs e)
		{
			using (HeaderFooterSetupDlg dlg = new HeaderFooterSetupDlg(m_publication.Cache,
				m_publication, m_helpTopicProvider, m_callbacks.FactoryHeaderFooterSetNames,
				m_pubOwner))
			{
				dlg.ShowDialog();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the PreviewKeyDown event of the btnOneColumn control. Used to treat a
		/// right-arrow as a click on the other column button.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.PreviewKeyDownEventArgs"/>
		/// instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void btnOneColumn_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
		{
			switch (e.KeyCode)
			{
				case Keys.Right:
					btnTwoColumn.PerformClick();
					btnTwoColumn.Focus();
					break;
				case Keys.Enter:
					m_btnOK.Focus();
					break;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the PreviewKeyDown event of the btnTwoColumn control. Used to treat a
		/// left-arrow as a click on the other column button.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.PreviewKeyDownEventArgs"/>
		/// instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void btnTwoColumn_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
		{
			switch (e.KeyCode)
			{
				case Keys.Left:
					btnOneColumn.PerformClick();
					btnOneColumn.Focus();
					break;
				case Keys.Enter:
					m_btnOK.Focus();
					break;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Paint event of the Preview panel controls.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.PaintEventArgs"/> instance
		/// containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void panelPreview_Paint(object sender, PaintEventArgs e)
		{
			Panel panel = (Panel)sender;
			int panelClientWidth = panel.ClientRectangle.Width;
			int panelClientHeight = panel.ClientRectangle.Height;

			Graphics g = e.Graphics;

			if (!PubPageFits)
			{
				using (Pen pen = new Pen(Color.Red, 2))
				{
					g.DrawLine(pen, 0, 0, panelClientWidth, panelClientHeight);
					g.DrawLine(pen, 0, panelClientHeight, panelClientWidth, 0);
					return;
				}
			}

			int leftMargin = m_udmLeft.MeasureValue * panelClientWidth / m_udmPaperWidth.MeasureValue;
			int topMargin = m_udmTop.MeasureValue * panelClientHeight / m_udmPaperHeight.MeasureValue;
			int rightMargin = m_udmRight.MeasureValue * panelClientWidth / m_udmPaperWidth.MeasureValue;
			int bottomMargin = m_udmBottom.MeasureValue * panelClientHeight / m_udmPaperHeight.MeasureValue;
			int gutter = m_udmGutter.MeasureValue * panelClientWidth / m_udmPaperWidth.MeasureValue;

			Rectangle rectPubPage;
			using (Pen dashedPen = new Pen(SystemColors.GrayText))
			{
				dashedPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;

				if (IsFullPage)
				{
					rectPubPage = new Rectangle(0, 0, panelClientWidth, panelClientHeight);
					if (gutter > 0)
					{
						switch (m_publication.BindingEdge)
						{
							case BindingSide.Left:
								rectPubPage.X += gutter;
								rectPubPage.Width -= gutter;
								g.DrawLine(dashedPen, rectPubPage.X, 0, rectPubPage.X, rectPubPage.Height);
								break;
							case BindingSide.Right:
								rectPubPage.Width -= gutter;
								g.DrawLine(dashedPen, rectPubPage.Width, 0, rectPubPage.Width, rectPubPage.Height);
								break;
							case BindingSide.Top:
								// TODO: Deal with top-bound gutter
								break;
						}
					}
				}
				else
				{
					int prevPubWidth = panelClientWidth * SelectedPubPage.Width / m_udmPaperWidth.MeasureValue;
					int prevPubHeight = panelClientHeight * SelectedPubPage.Height / m_udmPaperHeight.MeasureValue;
					int prevPubX = 2;
					int prevPubY = 2;
					switch (m_publication.BindingEdge)
					{
						case BindingSide.Left:
							prevPubX = Math.Max(prevPubX, gutter); break;
						case BindingSide.Right: // no-op
							break;
						case BindingSide.Top:
							// TODO: Deal with top-bound gutter
							//prevPubY = ;
							break;
					}
					rectPubPage = new Rectangle(prevPubX, prevPubY, prevPubWidth, prevPubHeight);
					g.DrawRectangle(dashedPen, rectPubPage);
				}
			}

			// draw lines representing the text
			Rectangle rectPrintable = rectPubPage;
			rectPrintable.X += leftMargin;
			rectPrintable.Y += topMargin;
			rectPrintable.Width -= leftMargin + rightMargin;
			rectPrintable.Height -= topMargin + bottomMargin;

			// The smaller the font size, the lighter we draw the lines representing the text
			int darkness = Math.Max((15 - (int)m_nudBaseCharSize.Value) * 15, 0);
			using (SolidBrush lineBrush = new SolidBrush(Color.FromArgb(darkness, darkness, darkness)))
			using (SolidBrush backgroundBrush = new SolidBrush(panel.BackColor))
			{
				int totalLineHeight = Math.Max((int)Math.Round((m_nudLineSpacing.Value * 1000 * panelClientHeight) / m_udmPaperHeight.MeasureValue), 2);
				int lineThickness = Math.Min((int)Math.Round(totalLineHeight / m_leadingFactor), totalLineHeight - 1);
				int spacingThickness = totalLineHeight - lineThickness;

				Rectangle lineRect = rectPrintable;
				lineRect.Height = lineThickness;
				int i = 0;
				//Font sampleFont = new Font("Times New Roman",
				//    (int)Math.Max((m_nudBaseCharSize.Value * 1000 * panelClientWidth) / m_udmPaperWidth.MeasureValue, 1));
				while (lineRect.Bottom < rectPrintable.Bottom)
				{
					Rectangle drawRect = lineRect;
					lineRect.Y += (spacingThickness + lineThickness);
					// Every seventh line gets indented.
					if (i % 6 == 0 && lineRect.Bottom < rectPrintable.Bottom)
					{
						if (m_publication.IsLeftBound)
							drawRect.X += 5;
						drawRect.Width -= 5;
					}
					//g.DrawString("QWERTY QWERTY QWERTY QWERTY QWERTY QWERTY QWERTY QWERTY ", sampleFont, lineBrush, drawRect);
					g.FillRectangle(lineBrush, drawRect);

					if (m_numColumns == 2)
					{
						drawRect.X = lineRect.X + lineRect.Width / 2 - 2;
						drawRect.Width = 4;
						if (i % 9 == 2 && lineRect.Bottom < rectPrintable.Bottom)
						{
							if (!m_publication.IsLeftBound)
								drawRect.X -= 5;
							drawRect.Width += 5;
						}
						g.FillRectangle(backgroundBrush, drawRect);
					}

					i++;
				}
			}
		}
		#endregion

		#region Protected properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the publication uses the font size and line spacing
		/// from the normal style or overrides these values.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool PublicationUsesNormalStyle
		{
			get
			{
				return (m_publication.BaseFontSize <= 0 ||
					Math.Abs(m_publication.BaseLineSpacing) < m_publication.BaseFontSize);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the selected publication page size info.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected PubPageInfo SelectedPubPage
		{
			get { return (PubPageInfo)cboPubPageSize.SelectedItem; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance is a two-column print layout.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool IsTwoColumnPrintLayout
		{
			get
			{
				Debug.Assert(m_numColumns == 1 || m_numColumns == 2,
					"Unexpected number of columns: " + m_numColumns);
				return m_numColumns == 2;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the standard leading factor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual decimal StandardLeadingFactor
		{
			get { return (decimal)1.2; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the the standard line spacing based on the current base character size.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual decimal StandardLineSpacingForBaseCharSize
		{
			get { return m_nudBaseCharSize.Value * StandardLeadingFactor; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the standard base char size based on the current line spacing value.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected virtual decimal StandardBaseCharSizeForLineSpacing
		{
			get { return m_nudLineSpacing.Value / StandardLeadingFactor; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the settings are within IPUB standards.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual bool FollowsStandardSettings
		{
			get
			{
				// Check if standard leading is maintained to within 1pt
				return Approx((int)(m_nudBaseCharSize.Value * StandardLeadingFactor),
							(int)m_nudLineSpacing.Value, 1);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the publication page fits on the selected paper.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool PubPageFits
		{
			get
			{
				PubPageInfo pubSize = (PubPageInfo)cboPubPageSize.SelectedItem;

				// Make sure the paper isn't too small for the publication page.
				if (pubSize.Height > 0)
				{
					Debug.Assert(pubSize.Width > 0);
					// Validate the publication page size to determine if it would not fit either
					// with portrait or landscape layout.
					if ((pubSize.Height > m_udmPaperHeight.MeasureValue ||
						pubSize.Width > m_udmPaperWidth.MeasureValue) &&
						(pubSize.Width > m_udmPaperHeight.MeasureValue ||
						pubSize.Height > m_udmPaperWidth.MeasureValue))
					{
						return false;
					}
				}
				return true;
			}
		}
		#endregion

		#region Public properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the current publication is for full page.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsFullPage
		{
			get { return SelectedPubPage == null ? false : SelectedPubPage.Height == 0; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the current settings result in an effective page
		/// size which matches a specific page size being displayed in the list of page sizes.
		/// This will return true if "Full Page" (page size with Height and Width both 0) is not
		/// selected in the list of page sizes OR if Full Page is selected and the select paper
		/// size matches the page size of one of the explicit page sizes in the list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsSpecialPageSize
		{
			get
			{
				if (!IsFullPage)
					return true;

				foreach (PubPageInfo pageInfo in cboPubPageSize.Items)
				{
					if (m_udmPaperHeight.MeasureValue == pageInfo.Height &&
						m_udmPaperWidth.MeasureValue == pageInfo.Width)
					{
						return true;
					}
				}
				return false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the maximum number of columns (1 or 2).
		/// </summary>
		/// <exception cref="T:ArgumentOutOfRangeException">thrown if value other than 1 or 2 is
		/// passed</exception>
		/// ------------------------------------------------------------------------------------
		public int MaxNumberOfColumns
		{
			set
			{
				CheckDisposed();
				if (value > 2 || value < 1)
					throw new ArgumentOutOfRangeException("MaxNumberOfColumns must be either 1 or 2");
				if (value == 1)
					SetNumberOfColumns(1);
				pnlOneColumn.Enabled = pnlTwoColumn.Enabled = btnOneColumn.Enabled =
					btnTwoColumn.Enabled = lblOneCol.Enabled = lblTwoCol.Enabled = (value == 2);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the index of the selected Publication Page size.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int PubPageSizeSelectedIndex
		{
			get { return cboPubPageSize.SelectedIndex; }
			set { cboPubPageSize.SelectedIndex = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of items in the Publication Page Size combo box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int PubPageSizeItems
		{
			get { return cboPubPageSize.Items.Count; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets a value indicating whether Publication Page Size combo box is enabled.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsPubPageSizeComboBoxEnabled
		{
			set { cboPubPageSize.Enabled = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the size of the base character in millipoints.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int BaseCharacterSize
		{
			get { return (int)(m_nudBaseCharSize.Value * 1000); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the base (normal) line spacing in millipoints.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int BaseLineSpacing
		{
			get { return (int)(m_nudLineSpacing.Value * 1000); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the default size of the base character and line spacing in millipoints. This is
		/// used when the current publication does not specify specific values (i.e., its
		/// BaseFontSize and BaseLineSpacing == 0).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SetDefaultBaseFontAndLineSizes()
		{
			// We always want the default to fall within the standards, so we clear this
			// checkbox. That should adjust the min/max values so that if the normal defaults
			// fall outside the range, they will be snapped back.
			// ENHANCE: This is really a kludge, because it works simply because there is only
			// one valid value for standard 1-column layout (in TE). So the snapping logic
			// always takes us to the correct "default". If multiple valid values existed, this
			// would only work if the correct default were the one closest to the normal default.
			// Since we really want to try to encapsulate the parameters that constrain this bit
			// of business logic in the publications XML file, we shouldn't rely on this kludge.
			// We really need to change our callback to be able to give us a good default based
			// on our current column count, page size, etc. It's just a bit tricky because so
			// many things can be defaulted that it's hard to get the order right.
			m_chkNonStdChoices.Checked = m_fAllowNonStandardChoicesCheckBoxAvailable;

			if (m_callbacks == null)
			{
				// Explicit values were not specified by the publication (or invalid values were specified),
				// so for this publication we will not allow the user to change text scaling values unless
				// the application has provided callbacks to give us valid defaults.
				m_nudBaseCharSize.Visible = m_nudLineSpacing.Visible = m_chkNonStdChoices.Visible = false;
				// Initialize non-standard leading to 20% of the base character size if we don't have
				// an actual real ratio to use.
				m_leadingFactor = StandardLeadingFactor;
			}
			else
			{
				decimal mptDefBaseCharSize = m_callbacks.NormalFontSize;
				decimal mptDefLineSpacing = m_callbacks.NormalLineHeight;
				Debug.Assert(mptDefBaseCharSize != 0 && mptDefLineSpacing != 0,
					"Default BaseCharacterSize and BaseLineSpacing should be non-zero: " +
					"BaseCharacterSize (" + mptDefBaseCharSize + "), BaseLineSpacing (" +
					mptDefLineSpacing + ")");
				m_fSetFontSizeAndLineSpacing = true; // don't recalculate one setting based on the other
				SetBaseCharacterSize(mptDefBaseCharSize);
				SetBaseLineSpacing(Math.Abs(mptDefLineSpacing)); // Only support exact line spacing
				m_leadingFactor = m_nudLineSpacing.Value / m_nudBaseCharSize.Value;
				if (m_fAllowNonStandardChoicesCheckBoxAvailable)
				{
					m_chkNonStdChoices.Visible = true;
					m_chkNonStdChoices.Checked = !FollowsStandardSettings;
				}
				m_fSetFontSizeAndLineSpacing = false;
			}
			m_fSaveBaseFontAndLineSizes = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets a value indicating whether "line spacing" control and label are visible.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsLineSpacingVisible
		{
			set { m_nudLineSpacing.Visible = m_lblLineSpacing.Visible = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the number of columns.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected int NumberOfColumns
		{
			get { return m_numColumns; }
			set { SetNumberOfColumns(value); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets a value indicating whether both font size and line spacing are being set to
		/// fixed values and we do not want to recalculate either to maintain a default leading.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool SetFontSizeAndLineSpacing
		{
			set { m_fSetFontSizeAndLineSpacing = value; }
		}
		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets a value indicating whether the "Allow Non-standard Choices" checkbox is visible.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void HideAllowNonStandardChoicesOption()
		{
			m_chkNonStdChoices.Visible = false;
			m_fAllowNonStandardChoicesCheckBoxAvailable = false;
			m_chkNonStdChoices.Checked = true;
		}
		#endregion

		#region Misc. methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the size of the base character, snapping it to be within the minimum/maximum
		/// range.
		/// </summary>
		/// <param name="value">The value, in millipoints.</param>
		/// ------------------------------------------------------------------------------------
		private void SetBaseCharacterSize(decimal value)
		{
			m_nudBaseCharSize.Value = SnapValueToRange(m_nudBaseCharSize.Minimum,
				m_nudBaseCharSize.Maximum, value / 1000);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the base (normal) line spacing, snapping it to be within the minimum/maximum
		/// range.
		/// </summary>
		/// <param name="value">The value, in millipoints.</param>
		/// ------------------------------------------------------------------------------------
		private void SetBaseLineSpacing(decimal value)
		{
			m_nudLineSpacing.Value = SnapValueToRange(m_nudLineSpacing.Minimum,
				m_nudLineSpacing.Maximum, value / 1000);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a list of default paper sizes for a FieldWorks application.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitializePaperSizes()
		{
			cbPaperSize.DisplayMember = "PaperName";
			AddPaperSize(ResourceHelper.GetResourceString("kstidPaperSizeA4"), 827, 1169);
			AddPaperSize(ResourceHelper.GetResourceString("kstidPaperSizeLetter"), 850, 1100);
			AddPaperSize(ResourceHelper.GetResourceString("kstidPaperSizeA5"), 583, 827);
			AddPaperSize(ResourceHelper.GetResourceString("kstidPaperSizeLegalStd"), 850, 1400);
			AddPaperSize(ResourceHelper.GetResourceString("kstidPaperSizeLegalPhil"), 850, 1300);
			AddPaperSize(ResourceHelper.GetResourceString("kstidPaperSizeCustom"), 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the size of the paper.
		/// </summary>
		/// <param name="paperName">Name of the paper.</param>
		/// <param name="width">The width of the paper.</param>
		/// <param name="height">The height of the paper.</param>
		/// ------------------------------------------------------------------------------------
		private void AddPaperSize(string paperName, int width, int height)
		{
			Debug.Assert(paperName != null, "Undefined paper name in AddPaperSize");
			cbPaperSize.Items.Add(new PaperSize(paperName, width, height));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Validates the dialog settings.
		/// </summary>
		/// <returns>an error code for the settings in the page setup dialog</returns>
		/// ------------------------------------------------------------------------------------
		protected virtual PageSetupErrorType ValidateDialogSettings()
		{
			if (!PubPageFits)
				return PageSetupErrorType.PubPageTooBig;

			// Make sure margins + gutter are not too big for that paper
			if (!IsFullPage)
			{
				PubPageInfo pubSize = (PubPageInfo)cboPubPageSize.SelectedItem;

				if (m_publication.BindingEdge == BindingSide.Top)
				{
					if (m_udmGutter.MeasureValue + pubSize.Height > m_udmPaperHeight.MeasureValue)
						return PageSetupErrorType.GutterTooBig;
				}
				else
				{
					if (m_udmGutter.MeasureValue + pubSize.Width > m_udmPaperWidth.MeasureValue)
						return PageSetupErrorType.GutterTooBig;
				}
			}
			int mptTotalMarginY = m_udmTop.MeasureValue + m_udmBottom.MeasureValue;
			int mptTotalMarginX = m_udmLeft.MeasureValue + m_udmRight.MeasureValue;
			if (IsFullPage)
			{
				if (m_publication.BindingEdge == BindingSide.Top)
					mptTotalMarginY += m_udmGutter.MeasureValue;
				else
					mptTotalMarginX += m_udmGutter.MeasureValue;
			}
			if (m_udmPaperHeight.MeasureValue < mptTotalMarginY + 50 * kCentiInchToMilliPoints)
				return PageSetupErrorType.VerticalMarginsTooBig;

			if(m_udmPaperWidth.MeasureValue < mptTotalMarginX + 50 * kCentiInchToMilliPoints)
				return PageSetupErrorType.HorizontalMarginsTooBig;

			return PageSetupErrorType.NoError;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ensures that the given value falls within the specified range. If the value is below
		/// the minimum, we return the minimum. If it is above the maximum, we return the
		/// maximum. Otherwise, we just return the given value.
		/// </summary>
		/// <param name="min">The minimum value allowed.</param>
		/// <param name="max">The maximum value allowed.</param>
		/// <param name="value">The value.</param>
		/// <returns>value which is  modified, if needed, to be within the range.</returns>
		/// ------------------------------------------------------------------------------------
		protected decimal SnapValueToRange(decimal min, decimal max, decimal value)
		{
			if (value < min)
				return min;
			else if (value > max)
				return max;
			else
				return value;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Displays the error message.
		/// </summary>
		/// <param name="error">An enumeration value which represents an error.</param>
		/// ------------------------------------------------------------------------------------
		private void DisplayErrorMessage(PageSetupErrorType error)
		{
			string msg = null;
			int tabToFocus = 0;
			switch (error)
			{
				case PageSetupErrorType.HorizontalMarginsTooBig:
					if (m_publication.BindingEdge == BindingSide.Top)
						msg = ResourceHelper.GetResourceString("kstidHorizontalMarginsTooBig");
					else
						msg = ResourceHelper.GetResourceString("kstidHorizontalMarginsAndGutterTooBig");
					tabToFocus = 1;
					break;
				case PageSetupErrorType.VerticalMarginsTooBig:
					if (m_publication.BindingEdge == BindingSide.Top)
						msg = ResourceHelper.GetResourceString("kstidVerticalMarginsAndGutterTooBig");
					else
						msg = ResourceHelper.GetResourceString("kstidVerticalMarginsTooBig");
					tabToFocus = 1;
					break;
				case PageSetupErrorType.PubPageTooBig:
					msg = ResourceHelper.GetResourceString("kstidPubPageTooBig");
					tabToFocus = 0;
					break;
				case PageSetupErrorType.GutterTooBig:
					msg = ResourceHelper.GetResourceString("kstidGutterTooBig");
					tabToFocus = tabControl1.SelectedIndex;
					if (tabToFocus == 2)
						tabToFocus = 1;
					break;
				default:
					break;
			}
			if (msg != null)
				MessageBox.Show(this, msg, m_app.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			tabControl1.SelectedIndex = tabToFocus;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns true if the given size is within 5000 millipoints of the standard.
		/// </summary>
		/// <param name="mptSize">Actual size in millipoints.</param>
		/// <param name="mptStd">Standard size in millipoints.</param>
		/// <returns><c>true</c> if the actual size is within 5 pts of the standard;
		/// otherwise, <c>false</c></returns>
		/// ------------------------------------------------------------------------------------
		private bool Approx(int mptSize, int mptStd)
		{
			return Approx(mptSize, mptStd, 5000);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns true if the given size is within the standard.
		/// </summary>
		/// <param name="size">Actual size.</param>
		/// <param name="std">Standard size.</param>
		/// <param name="threshold">The threshold.</param>
		/// <returns><c>true</c> if the actual size is within the threshold from the standard;
		/// otherwise, <c>false</c></returns>
		/// ------------------------------------------------------------------------------------
		private bool Approx(int size, int std, int threshold)
		{
			return (size >= std - threshold) && (size <= std + threshold);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adjust the paper orientation to portrait or landscape to match the paper
		/// width and paper height. And adjust the paper size selection to match
		/// the paper width and paper height.
		/// </summary>
		/// <param name="mptWidth">current width of the paper in millipoints</param>
		/// <param name="mptHeight">current height of the paper in millipoints</param>
		/// ------------------------------------------------------------------------------------
		private void AdjustPaperSize(int mptWidth, int mptHeight)
		{
			m_AdjustingPaperSize = true;
			//if (mptWidth < mptHeight)
			//{
			//    m_fBookFoldCurrent = false;
			//    pbLandscape.Visible = true;
			//    pbBookFold.Visible = false;
			//    pbDraft.Visible = false;
			//}
			//else
			//{
			//    m_fBookFoldCurrent = true;
			//    pbLandscape.Visible = false;
			//    pbBookFold.Visible = true;
			//    pbDraft.Visible = true;
			//}

			bool fFoundPaperSize = false;
			// For each paper size in the dictionary...
			foreach (PaperSize paperSize in cbPaperSize.Items)
			{
				// determine if the selected paper size matches a defined papersize for
				// portrait or landscape layout.
				if ((Approx(mptWidth, paperSize.Width * kCentiInchToMilliPoints) &&
					Approx(mptHeight, paperSize.Height * kCentiInchToMilliPoints)) ||
					(Approx(mptWidth, paperSize.Height * kCentiInchToMilliPoints) &&
					Approx(mptHeight, paperSize.Width * kCentiInchToMilliPoints)))
				{
					cbPaperSize.SelectedItem = paperSize;
					fFoundPaperSize = true;
					break;
				}
			}

			if (!fFoundPaperSize)
			{
				// We didn't find a matching papersize, so set the type of paper to custom.
				string strCustomPaperSize = ResourceHelper.GetResourceString("kstidPaperSizeCustom");
				cbPaperSize.SelectedIndex = cbPaperSize.FindStringExact(strCustomPaperSize);
			}

			AdjustPaperSizePreview();

			m_AdjustingPaperSize = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the margin controls. The current implementation does what TE needs. If this
		/// proves useless for other apps, this logic should be moved into the TE-specific
		/// subclass.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void UpdateMarginControls()
		{
			if (IsFullPage)
			{
				m_udmTop.Enabled = m_udmBottom.Enabled = m_udmLeft.Enabled = m_udmRight.Enabled =
					true;
				m_udmHeader.MeasureMax = m_udmFooter.MeasureMax = 720000;
			}
			else
			{
				m_udmTop.Enabled = m_udmBottom.Enabled = m_udmLeft.Enabled = m_udmRight.Enabled =
					false;
				m_udmHeader.MeasureMax = m_udmTop.MeasureValue;
				m_udmFooter.MeasureMax = m_udmBottom.MeasureValue;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the number of columns.
		/// </summary>
		/// <param name="numberOfColumns">The number of columns for division.</param>
		/// ------------------------------------------------------------------------------------
		private void SetNumberOfColumns(int numberOfColumns)
		{
			Debug.Assert(numberOfColumns == 1 || numberOfColumns == 2,
				"Unexpected number of columns: " + numberOfColumns);
			m_numColumns = numberOfColumns;
			UpdateColumnButtonStates();
			UpdateBaseSizeControlsMinAndMax();
			ScaleLineSpacing();
			UpdateMarginControls();
			UpdateTextSizeCtlStatus();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the column button images to reflect the number of columns.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void UpdateColumnButtonStates()
		{
			switch (m_numColumns)
			{
				case 1:
					btnOneColumn.Image = ResourceHelper.OneColumnSelectedIcon;
					pnlOneColumn.TabStop = btnOneColumn.TabStop = true;
					btnTwoColumn.Image = ResourceHelper.TwoColumnIcon;
					pnlTwoColumn.TabStop = btnTwoColumn.TabStop = false;
					break;
				case 2:
				default:
					btnTwoColumn.Image = ResourceHelper.TwoColumnSelectedIcon;
					pnlTwoColumn.TabStop = btnTwoColumn.TabStop = true;
					btnOneColumn.Image = ResourceHelper.OneColumnIcon;
					pnlOneColumn.TabStop = btnOneColumn.TabStop = false;
					break;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the base line spacing to maintain scaling consistent with character size.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ScaleLineSpacing()
		{
			if (m_fChangingTextSize || m_fSetFontSizeAndLineSpacing)
				return;
			m_fChangingTextSize = true;

			if (!m_chkNonStdChoices.Checked) // following the standard
			{
				m_nudLineSpacing.Value = StandardLineSpacingForBaseCharSize;
			}
			else // not following the standard
			{
				decimal proposedNewLineSpacingValue = m_nudBaseCharSize.Value * m_leadingFactor;
				// Recalculate line spacing to maintain current non-standard leading.
				if (proposedNewLineSpacingValue >= m_nudLineSpacing.Minimum &&
					proposedNewLineSpacingValue <= m_nudLineSpacing.Maximum)
				{
					m_nudLineSpacing.Value = proposedNewLineSpacingValue;
				}
				else
				{
					// If the font size change would force the line spacing too high or low
					// (which only happens during dialog initialization) then stop trying
					// to maintain consistent leading. Recalc the leading percentage instead.
					m_leadingFactor = m_nudLineSpacing.Value / m_nudBaseCharSize.Value;
				}
			}
			m_fChangingTextSize = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the base font size to keep it scaled consistently with the line spacing.
		/// </summary>
		/// <remarks>REVIEW: This seems to be app-specific with the use of PubPageSizeType</remarks>
		/// ------------------------------------------------------------------------------------
		private void ScaleFontSize()
		{
			if (m_fChangingTextSize || m_fSetFontSizeAndLineSpacing)
				return;
			m_fChangingTextSize = true;

			if (!m_chkNonStdChoices.Checked) // following the standard
			{
				m_nudBaseCharSize.Value = StandardBaseCharSizeForLineSpacing;
			}
			else // not following the standard, so don't scale font size at all (just adjust leading %)
			{
				// When allowing Non-Standard Choices the Line Spacing should never
				// be less than the Base Character Size, so adjust the font size down too.
				if (m_nudLineSpacing.Value < m_nudBaseCharSize.Value)
					m_nudBaseCharSize.Value = m_nudLineSpacing.Value;

				m_leadingFactor = m_nudLineSpacing.Value / m_nudBaseCharSize.Value;
			}

			m_fChangingTextSize = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the enabled status of the text size controls.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void UpdateTextSizeCtlStatus()
		{
			// Note: For Scripture publications, there is only one valid base character size and
			// line spacing with single-column print layout according to IPUB standards.
			m_nudBaseCharSize.Enabled = m_nudLineSpacing.Enabled =
				(m_nudBaseCharSize.Maximum != m_nudBaseCharSize.Minimum) ||
				m_chkNonStdChoices.Checked || !IsSpecialPageSize;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Saves the settings on the dialog in the database.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SaveDialogSettings()
		{
			if (m_fSavePaperSize)
			{
				m_publication.PaperHeight = m_udmPaperHeight.MeasureValue;
				m_publication.PaperWidth = m_udmPaperWidth.MeasureValue;
			}

			m_publication.PageHeight = ((PubPageInfo)cboPubPageSize.SelectedItem).Height;
			m_publication.PageWidth = ((PubPageInfo)cboPubPageSize.SelectedItem).Width;
			m_division.StartAt = (DivisionStartOption)m_cbBookStart.SelectedIndex;
			m_division.DifferentEvenHF = m_ckbDiffEvenHF.Checked;
			m_division.DifferentFirstHF = m_ckbDiffFirstHF.Checked;
			m_division.NumColumns = m_numColumns;

			switch (m_cboSeparator.SelectedIndex)
			{
				case kFootnoteSepWidthNone:
					m_publication.FootnoteSepWidth = 0; break;
				case kFootnoteSepWidthThird:
					m_publication.FootnoteSepWidth = 333; break;
				case kFootnoteSepWidthFull:
					m_publication.FootnoteSepWidth = 1000; break;
			}

			// REVIEW: currently returns whether it is book fold, not necessarily landscape.
			m_publication.IsLandscape = m_fBookFoldCurrent;

			if (m_fSaveBaseFontAndLineSizes)
			{
				m_publication.BaseFontSize = (int)m_nudBaseCharSize.Value * 1000;
				m_publication.BaseLineSpacing = -(int)m_nudLineSpacing.Value * 1000;
			}

			if (m_fSaveMargins)
			{
				m_pgLayout.MarginTop = m_udmTop.MeasureValue;
				m_pgLayout.MarginBottom = m_udmBottom.MeasureValue;
				m_pgLayout.MarginInside = m_udmLeft.MeasureValue;
				m_pgLayout.MarginOutside = m_udmRight.MeasureValue;
				m_pgLayout.PosHeader = m_udmHeader.MeasureValue;
				m_pgLayout.PosFooter = m_udmFooter.MeasureValue;
				m_publication.GutterMargin = m_udmGutter.MeasureValue;
			}
		}
		#endregion
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Any application that displays the Page Setup dialog needs to implement this interface
	/// to allow the dialog to know about application-specific settings.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IPageSetupCallbacks
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a list of header/footer set names that cannot be deleted.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		List<string> FactoryHeaderFooterSetNames {get;}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the size of the normal font in millipoints.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		int NormalFontSize {get;}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the height of the normal line in millipoints.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		int NormalLineHeight {get;}
	}
}
