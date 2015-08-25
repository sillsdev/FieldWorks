// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: PicturePropertiesDialog.cs
// Responsibility: TeTeam
using System;
using System.Drawing;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Resources;
using SIL.Utils;
using SIL.Utils.FileDialog;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Dialog for editing picture properties
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class PicturePropertiesDialog : Form, IFWDisposable
	{
		#region Member variables
		private const string s_helpTopic = "khtpPictureProperties";

		private IContainer components;
		private Image m_currentImage;
		private string m_filePath;
		private PictureBox m_picPreview;
		private Label lblFilename;
		private readonly FdoCache m_cache;
		private readonly ICmPicture m_initialPicture;
		private readonly IHelpTopicProvider m_helpTopicProvider;
		private readonly IApp m_app;
		private readonly HelpProvider helpProvider;
		private readonly int m_captionWs;

		private Button m_btnHelp;
		private FwTextBox m_txtCaption;
		private LabeledMultiStringControl m_lmscCaption;
		private ToolTip m_tooltip;
		private Panel panelBottom;
		private GroupBox m_grpFileLocOptions;
		private RadioButton m_rbMove;
		private RadioButton m_rbCopy;
		private RadioButton m_rbLeave;
		private TextBox m_txtDestination;
		private Label m_lblDestination;
		private Button m_btnBrowseDest;
		private Panel panelFileName;
		private FwPanel pnlCaption;
		private Panel panel1;
		private FwPanel pnlPicture;

		private static FileLocationChoice s_defaultFileLocChoiceForSession = FileLocationChoice.Copy;
		private static string s_sExternalLinkDestinationDir;

		private String s_defaultPicturesFolder;
		#endregion

		#region Construction, initialization, and disposal
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:PicturePropertiesDialog"/> class.
		/// </summary>
		/// <param name="cache">The FdoCache to use</param>
		/// <param name="initialPicture">The CmPicture object to set all of the dialog
		/// properties to, or null to edit a new picture</param>
		/// <param name="helpTopicProvider">typically IHelpTopicProvider.App</param>
		/// <param name="app">The application</param>
		/// ------------------------------------------------------------------------------------
		public PicturePropertiesDialog(FdoCache cache, ICmPicture initialPicture,
			IHelpTopicProvider helpTopicProvider, IApp app)
			: this(cache, initialPicture, helpTopicProvider, app, false)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:PicturePropertiesDialog"/> class.
		/// </summary>
		/// <param name="cache">The FdoCache to use</param>
		/// <param name="initialPicture">The CmPicture object to set all of the dialog
		/// properties to, or null to edit a new picture</param>
		/// <param name="helpTopicProvider">typically IHelpTopicProvider.App</param>
		/// <param name="app">The application</param>
		/// <param name="fAnalysis">true to use analysis writign system for caption</param>
		/// ------------------------------------------------------------------------------------
		public PicturePropertiesDialog(FdoCache cache, ICmPicture initialPicture,
			IHelpTopicProvider helpTopicProvider, IApp app, bool fAnalysis)
		{
			// ReSharper disable LocalizableElement
			if (cache == null)
				throw(new ArgumentNullException("cache", "The FdoCache cannot be null"));
			// ReSharper restore LocalizableElement

			Logger.WriteEvent("Opening 'Picture Properties' dialog");

			m_cache = cache;
			m_initialPicture = initialPicture;
			m_helpTopicProvider = helpTopicProvider;
			m_app = app;
			m_captionWs = fAnalysis
				? m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle
				: m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle;

			InitializeComponent();
			AccessibleName = GetType().Name;

			if (m_helpTopicProvider != null) // Could be null during tests
			{
				helpProvider = new HelpProvider();
				helpProvider.HelpNamespace = FwDirectoryFinder.CodeDirectory +
					m_helpTopicProvider.GetHelpString("UserHelpFile");
				helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(s_helpTopic));
				helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
			}
		}

		/// <summary>
		/// Convert the text box for the caption to a multilingual string control.
		/// </summary>
		public void UseMultiStringCaption(FdoCache cache, int wsMagic, IVwStylesheet stylesheet)
		{
			m_lmscCaption = new LabeledMultiStringControl(cache, wsMagic, stylesheet);
			m_txtCaption.Hide();
			m_lmscCaption.Location = m_txtCaption.Location;
			m_lmscCaption.Width = m_txtCaption.Width;
			m_lmscCaption.Anchor = m_txtCaption.Anchor;
			m_lmscCaption.AccessibleName = m_txtCaption.AccessibleName;
			m_lmscCaption.Dock = DockStyle.Fill;

			// Grow the dialog and move all lower controls down to make room.
			pnlCaption.Controls.Remove(m_txtCaption);
			m_lmscCaption.TabIndex = m_txtCaption.TabIndex;	// assume the same tab order as the 'designed' control
			pnlCaption.Controls.Add(m_lmscCaption);
		}

		/// <summary>
		/// Set the multilingual caption into the dialog control.
		/// </summary>
		public void SetMultilingualCaptionValues(IMultiAccessorBase caption)
		{
			if (m_lmscCaption == null)
				return;
			var cws = m_lmscCaption.NumberOfWritingSystems;
			for (var i = 0; i < cws; i++)
			{
				var curWs = m_lmscCaption.Ws(i);
				if (curWs <= 0)
					continue;
				int actualWs;
				ITsString tssStr;
				if (!caption.TryWs(curWs, out actualWs, out tssStr))
					continue;
				m_lmscCaption.SetValue(curWs, tssStr);
			}
		}

		/// <summary>
		/// Store the results of any editing into the actual data.
		/// </summary>
		public void GetMultilingualCaptionValues(IMultiAccessorBase caption)
		{
			if (m_lmscCaption == null)
				return;
			var cws = m_lmscCaption.NumberOfWritingSystems;
			for (var i = 0; i < cws; i++)
			{
				var curWs = m_lmscCaption.Ws(i);
				caption.set_String(curWs, m_lmscCaption.Value(curWs));
			}
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the dialog (and let the user select a picture)
		/// </summary>
		/// <returns>True if initialization succeeded, false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public bool Initialize()
		{
			CheckDisposed();

			ILgWritingSystemFactory wsf = m_cache.LanguageWritingSystemFactoryAccessor;
			m_txtCaption.WritingSystemFactory = wsf;
			m_txtCaption.WritingSystemCode = m_captionWs;

			s_defaultPicturesFolder = Path.Combine(m_cache.LanguageProject.LinkedFilesRootDir, "Pictures");
			try
			{
				if (!Directory.Exists(m_cache.LanguageProject.LinkedFilesRootDir))
					Directory.CreateDirectory(m_cache.LanguageProject.LinkedFilesRootDir);
				if (!Directory.Exists(s_defaultPicturesFolder))
					Directory.CreateDirectory(s_defaultPicturesFolder);
			}
			catch (Exception e)
			{
				var errorMsg = string.Format("Error creating one of the following directories '{0}' or '{1}'",
											 m_cache.LanguageProject.LinkedFilesRootDir, s_defaultPicturesFolder);
				Logger.WriteEvent(errorMsg);
				Logger.WriteError(e);
				MessageBoxUtils.Show(errorMsg);
			}

			m_txtDestination.Text = s_sExternalLinkDestinationDir ?? s_defaultPicturesFolder;

			if (m_initialPicture != null)
			{
				ITsString tss = m_initialPicture.Caption.get_String(m_captionWs);
				m_txtCaption.Tss = tss.Length == 0 ? MakeEmptyCaptionString() : tss;

				if (m_initialPicture.PictureFileRA == null)
					m_filePath = String.Empty;
				else
				{
					m_filePath = m_initialPicture.PictureFileRA.AbsoluteInternalPath;
					if (m_filePath == StringServices.EmptyFileName)
						m_filePath = String.Empty;
				}

				if (FileUtils.TrySimilarFileExists(m_filePath, out m_filePath))
					m_currentImage = Image.FromFile(m_filePath);
				else
				{
					// use an image that indicates the image file could not be opened.
					m_currentImage = Common.RootSites.SimpleRootSite.ImageNotFoundX;
				}
				UpdatePicInformation();
				m_rbLeave.Checked = true;
				return true;
			}

			m_txtCaption.Tss = MakeEmptyCaptionString();

			// if the user isn't editing an existing picture, then go ahead and bring up
			// the file chooser
			DialogResult result = ShowChoosePictureDlg();
			if (result == DialogResult.Cancel)
			{
				// use an image that indicates the we don't have an image
				Debug.Assert(m_currentImage == null);
				m_currentImage = SIL.FieldWorks.Common.RootSites.SimpleRootSite.ImageNotFoundX;
				UpdatePicInformation();
			}
			ApplyDefaultFileLocationChoice();
			return (result == DialogResult.OK);
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

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.
		/// </param>
		/// -----------------------------------------------------------------------------------
		protected override void Dispose( bool disposing )
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
				if (components != null)
					components.Dispose();

				if (m_currentImage != null)
					m_currentImage.Dispose();
			}
			m_currentImage = null;
			base.Dispose( disposing );
		}
		#endregion

		#region Windows Form Designer generated code
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.Windows.Forms.Button m_btnOK;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PicturePropertiesDialog));
			System.Windows.Forms.Button btnCancel;
			System.Windows.Forms.Button btnChooseFile;
			System.Windows.Forms.Label lblCaption;
			System.Windows.Forms.Label label2;
			System.Windows.Forms.Label label3;
			this.m_btnHelp = new System.Windows.Forms.Button();
			this.m_picPreview = new System.Windows.Forms.PictureBox();
			this.lblFilename = new System.Windows.Forms.Label();
			this.m_txtCaption = new SIL.FieldWorks.Common.Widgets.FwTextBox();
			this.m_tooltip = new System.Windows.Forms.ToolTip(this.components);
			this.panelBottom = new System.Windows.Forms.Panel();
			this.pnlPicture = new SIL.FieldWorks.Common.Controls.FwPanel();
			this.pnlCaption = new SIL.FieldWorks.Common.Controls.FwPanel();
			this.m_grpFileLocOptions = new System.Windows.Forms.GroupBox();
			this.m_btnBrowseDest = new System.Windows.Forms.Button();
			this.m_txtDestination = new System.Windows.Forms.TextBox();
			this.m_lblDestination = new System.Windows.Forms.Label();
			this.m_rbLeave = new System.Windows.Forms.RadioButton();
			this.m_rbMove = new System.Windows.Forms.RadioButton();
			this.m_rbCopy = new System.Windows.Forms.RadioButton();
			this.panelFileName = new System.Windows.Forms.Panel();
			this.panel1 = new System.Windows.Forms.Panel();
			m_btnOK = new System.Windows.Forms.Button();
			btnCancel = new System.Windows.Forms.Button();
			btnChooseFile = new System.Windows.Forms.Button();
			lblCaption = new System.Windows.Forms.Label();
			label2 = new System.Windows.Forms.Label();
			label3 = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.m_picPreview)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_txtCaption)).BeginInit();
			this.panelBottom.SuspendLayout();
			this.pnlPicture.SuspendLayout();
			this.pnlCaption.SuspendLayout();
			this.m_grpFileLocOptions.SuspendLayout();
			this.panelFileName.SuspendLayout();
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			//
			// m_btnOK
			//
			resources.ApplyResources(m_btnOK, "m_btnOK");
			m_btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			m_btnOK.Name = "m_btnOK";
			//
			// btnCancel
			//
			resources.ApplyResources(btnCancel, "btnCancel");
			btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			btnCancel.Name = "btnCancel";
			//
			// btnChooseFile
			//
			resources.ApplyResources(btnChooseFile, "btnChooseFile");
			btnChooseFile.Name = "btnChooseFile";
			btnChooseFile.Click += new System.EventHandler(this.m_btnChooseFile_Click);
			//
			// lblCaption
			//
			resources.ApplyResources(lblCaption, "lblCaption");
			lblCaption.Name = "lblCaption";
			//
			// label2
			//
			resources.ApplyResources(label2, "label2");
			label2.Name = "label2";
			//
			// label3
			//
			resources.ApplyResources(label3, "label3");
			label3.Name = "label3";
			//
			// m_btnHelp
			//
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			this.m_btnHelp.Name = "m_btnHelp";
			this.m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// m_picPreview
			//
			resources.ApplyResources(this.m_picPreview, "m_picPreview");
			this.m_picPreview.Name = "m_picPreview";
			this.m_picPreview.TabStop = false;
			this.m_picPreview.ClientSizeChanged += new System.EventHandler(this.m_picPreview_ClientSizeChanged);
			//
			// lblFilename
			//
			resources.ApplyResources(this.lblFilename, "lblFilename");
			this.lblFilename.Name = "lblFilename";
			this.lblFilename.Paint += new System.Windows.Forms.PaintEventHandler(this.lblFilename_Paint);
			this.lblFilename.MouseEnter += new System.EventHandler(this.lblFilename_MouseEnter);
			//
			// m_txtCaption
			//
			this.m_txtCaption.AcceptsReturn = false;
			this.m_txtCaption.AdjustStringHeight = true;
			this.m_txtCaption.BackColor = System.Drawing.SystemColors.Window;
			this.m_txtCaption.controlID = null;
			resources.ApplyResources(this.m_txtCaption, "m_txtCaption");
			this.m_txtCaption.HasBorder = true;
			this.m_txtCaption.Name = "m_txtCaption";
			this.m_txtCaption.SuppressEnter = false;
			this.m_txtCaption.WordWrap = true;
			//
			// panelBottom
			//
			resources.ApplyResources(this.panelBottom, "panelBottom");
			this.panelBottom.Controls.Add(this.pnlPicture);
			this.panelBottom.Controls.Add(this.pnlCaption);
			this.panelBottom.Controls.Add(btnChooseFile);
			this.panelBottom.Controls.Add(lblCaption);
			this.panelBottom.Controls.Add(label3);
			this.panelBottom.ForeColor = System.Drawing.SystemColors.ControlText;
			this.panelBottom.Name = "panelBottom";
			//
			// pnlPicture
			//
			resources.ApplyResources(this.pnlPicture, "pnlPicture");
			this.pnlPicture.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.pnlPicture.ClipTextForChildControls = true;
			this.pnlPicture.ControlReceivingFocusOnMnemonic = null;
			this.pnlPicture.Controls.Add(this.m_picPreview);
			this.pnlPicture.DoubleBuffered = true;
			this.pnlPicture.MnemonicGeneratesClick = false;
			this.pnlPicture.Name = "pnlPicture";
			this.pnlPicture.PaintExplorerBarBackground = false;
			//
			// pnlCaption
			//
			resources.ApplyResources(this.pnlCaption, "pnlCaption");
			this.pnlCaption.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.pnlCaption.ClipTextForChildControls = true;
			this.pnlCaption.ControlReceivingFocusOnMnemonic = null;
			this.pnlCaption.Controls.Add(this.m_txtCaption);
			this.pnlCaption.DoubleBuffered = true;
			this.pnlCaption.MnemonicGeneratesClick = false;
			this.pnlCaption.Name = "pnlCaption";
			this.pnlCaption.PaintExplorerBarBackground = false;
			//
			// m_grpFileLocOptions
			//
			resources.ApplyResources(this.m_grpFileLocOptions, "m_grpFileLocOptions");
			this.m_grpFileLocOptions.Controls.Add(this.m_btnBrowseDest);
			this.m_grpFileLocOptions.Controls.Add(this.m_txtDestination);
			this.m_grpFileLocOptions.Controls.Add(this.m_lblDestination);
			this.m_grpFileLocOptions.Controls.Add(this.m_rbLeave);
			this.m_grpFileLocOptions.Controls.Add(this.m_rbMove);
			this.m_grpFileLocOptions.Controls.Add(this.m_rbCopy);
			this.m_grpFileLocOptions.Name = "m_grpFileLocOptions";
			this.m_grpFileLocOptions.TabStop = false;
			//
			// m_btnBrowseDest
			//
			resources.ApplyResources(this.m_btnBrowseDest, "m_btnBrowseDest");
			this.m_btnBrowseDest.Name = "m_btnBrowseDest";
			this.m_btnBrowseDest.UseVisualStyleBackColor = true;
			this.m_btnBrowseDest.Click += new System.EventHandler(this.m_btnBrowseDest_Click);
			//
			// m_txtDestination
			//
			resources.ApplyResources(this.m_txtDestination, "m_txtDestination");
			this.m_txtDestination.Name = "m_txtDestination";
			//
			// m_lblDestination
			//
			resources.ApplyResources(this.m_lblDestination, "m_lblDestination");
			this.m_lblDestination.Name = "m_lblDestination";
			//
			// m_rbLeave
			//
			resources.ApplyResources(this.m_rbLeave, "m_rbLeave");
			this.m_rbLeave.Name = "m_rbLeave";
			this.m_rbLeave.UseVisualStyleBackColor = true;
			this.m_rbLeave.CheckedChanged += new System.EventHandler(this.HandleLocationCheckedChanged);
			//
			// m_rbMove
			//
			resources.ApplyResources(this.m_rbMove, "m_rbMove");
			this.m_rbMove.Name = "m_rbMove";
			this.m_rbMove.UseVisualStyleBackColor = true;
			this.m_rbMove.CheckedChanged += new System.EventHandler(this.HandleLocationCheckedChanged);
			//
			// m_rbCopy
			//
			resources.ApplyResources(this.m_rbCopy, "m_rbCopy");
			this.m_rbCopy.Checked = true;
			this.m_rbCopy.Name = "m_rbCopy";
			this.m_rbCopy.TabStop = true;
			this.m_rbCopy.UseVisualStyleBackColor = true;
			this.m_rbCopy.CheckedChanged += new System.EventHandler(this.HandleLocationCheckedChanged);
			//
			// panelFileName
			//
			resources.ApplyResources(this.panelFileName, "panelFileName");
			this.panelFileName.Controls.Add(this.lblFilename);
			this.panelFileName.Controls.Add(label2);
			this.panelFileName.ForeColor = System.Drawing.SystemColors.ControlText;
			this.panelFileName.Name = "panelFileName";
			//
			// panel1
			//
			resources.ApplyResources(this.panel1, "panel1");
			this.panel1.Controls.Add(btnCancel);
			this.panel1.Controls.Add(this.m_btnHelp);
			this.panel1.Controls.Add(m_btnOK);
			this.panel1.Name = "panel1";
			//
			// PicturePropertiesDialog
			//
			this.AcceptButton = m_btnOK;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = btnCancel;
			this.Controls.Add(this.panelBottom);
			this.Controls.Add(this.m_grpFileLocOptions);
			this.Controls.Add(this.panelFileName);
			this.Controls.Add(this.panel1);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "PicturePropertiesDialog";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			((System.ComponentModel.ISupportInitialize)(this.m_picPreview)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_txtCaption)).EndInit();
			this.panelBottom.ResumeLayout(false);
			this.panelBottom.PerformLayout();
			this.pnlPicture.ResumeLayout(false);
			this.pnlCaption.ResumeLayout(false);
			this.m_grpFileLocOptions.ResumeLayout(false);
			this.m_grpFileLocOptions.PerformLayout();
			this.panelFileName.ResumeLayout(false);
			this.panelFileName.PerformLayout();
			this.panel1.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		#region Public properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the path to the file the user chose (if the user chose to move or copy a file
		/// to the "internal" folder (i.e., the LinkedFiles folder), this will be the path of
		/// the file in the new location.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string CurrentFile
		{
			get
			{
				CheckDisposed();
				return m_filePath;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the caption the user gave the file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITsString Caption
		{
			get
			{
				CheckDisposed();
				return m_txtCaption.Tss;
			}
		}
		#endregion

		#region Event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the ClientSizeChanged event of the m_picPreview control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_picPreview_ClientSizeChanged(object sender, EventArgs e)
		{
			UpdatePicInformation();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle event when user clicks on button to choose an image file.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_btnChooseFile_Click(object sender, EventArgs e)
		{
			ShowChoosePictureDlg();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Show help for this dialog
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, s_helpTopic);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the m_btnBrowseDest control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_btnBrowseDest_Click(object sender, EventArgs e)
		{
			Logger.WriteEvent("Browsing for destination folder in 'Picture Properties' dialog");
			using (var dlg = new FolderBrowserDialogAdapter())
			{
				dlg.SelectedPath = m_txtDestination.Text;
				dlg.Description = String.Format(FwCoreDlgs.kstidSelectLinkedFilesSubFolder,
					s_defaultPicturesFolder);
				dlg.ShowNewFolderButton = true;

				if (dlg.ShowDialog() == DialogResult.OK)
				{
					if (ValidateDestinationFolder(dlg.SelectedPath))
						m_txtDestination.Text = dlg.SelectedPath;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Log a message when the dialog is closed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnClosed(EventArgs e)
		{
			Logger.WriteEvent("Closing 'Picture Properties' dialog with result " + DialogResult);

			if (DialogResult == DialogResult.OK)
			{
				string action = (m_initialPicture == null ? "Creating" : "Changing");
				Logger.WriteEvent(string.Format("{0} Picture Properties: file: {1}, {2} caption",
					action, m_filePath, m_txtCaption.Text.Length > 0 ? "with" : "no"));
			}

			base.OnClosed(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Form.Closing"/> event.
		/// </summary>
		/// <param name="e">A <see cref="T:System.ComponentModel.CancelEventArgs"/> that
		/// contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnClosing(CancelEventArgs e)
		{
			if (DialogResult == DialogResult.OK && m_grpFileLocOptions.Visible)
			{
				if (!m_rbLeave.Checked && !ValidateDestinationFolder(m_txtDestination.Text))
					e.Cancel = true;
				else
					ApplyFileLocationOptions();
			}

			base.OnClosing(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Draw the file name with EllipsisPath trimming.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void lblFilename_Paint(object sender, PaintEventArgs e)
		{
			TextFormatFlags flags = TextFormatFlags.VerticalCenter |
				TextFormatFlags.PathEllipsis | TextFormatFlags.SingleLine;

			e.Graphics.FillRectangle(SystemBrushes.Control, lblFilename.ClientRectangle);
			TextRenderer.DrawText(e.Graphics, lblFilename.Text, lblFilename.Font,
				lblFilename.ClientRectangle, lblFilename.ForeColor, flags);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void lblFilename_MouseEnter(object sender, EventArgs e)
		{
			Size szPreferred = TextRenderer.MeasureText(lblFilename.Text, lblFilename.Font);
			m_tooltip.SetToolTip(lblFilename,
				(lblFilename.Width < szPreferred.Width + 8 ? lblFilename.Text : null));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes sure the destination text box and chooser button are disabled when the
		/// user chooses to leave the picture in its original folder.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void HandleLocationCheckedChanged(object sender, EventArgs e)
		{
			m_txtDestination.Enabled = (sender != m_rbLeave);
			m_btnBrowseDest.Enabled = m_txtDestination.Enabled;
			m_lblDestination.Enabled = m_txtDestination.Enabled;
		}

		#endregion

		#region Private methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the user clicked the OK button and the File Location Options panel is
		/// visible.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ApplyFileLocationOptions()
		{
			FileLocationChoice fileLocChoice;

			if (m_rbCopy.Checked)
				fileLocChoice = FileLocationChoice.Copy;
			else
			{
				fileLocChoice = (m_rbMove.Checked ?
					FileLocationChoice.Move : FileLocationChoice.Leave);
			}

			// If this dialog is being displayed for the purpose of inserting a new
			// picture or changing which picture is being displayed, remember the user's
			// copy/move/leave choice
			if (m_initialPicture == null || m_initialPicture.PictureFileRA.AbsoluteInternalPath != m_filePath)
				s_defaultFileLocChoiceForSession = fileLocChoice;

			m_picPreview.Image = null;
			m_currentImage.Dispose();
			m_currentImage = null;

			m_filePath = MoveOrCopyFilesDlg.PerformMoveCopyOrLeaveFile(m_filePath,
				m_txtDestination.Text, fileLocChoice);
			if (MoveOrCopyFilesDlg.FileIsInExternalLinksFolder(m_filePath, m_txtDestination.Text))
				s_sExternalLinkDestinationDir = m_txtDestination.Text;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Validates the proposed destination folder.
		/// </summary>
		/// <param name="proposedDestFolder">The proposed destination folder path.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private bool ValidateDestinationFolder(string proposedDestFolder)
		{
			if (!IsFolderInLinkedFilesFolder(proposedDestFolder))
			{
				MessageBoxUtils.Show(this, String.Format(FwCoreDlgs.kstidDestFolderMustBeInLinkedFiles,
					s_defaultPicturesFolder), m_app.ApplicationName, MessageBoxButtons.OK);
				return false;
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Applies the default file location choice.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ApplyDefaultFileLocationChoice()
		{
			// We can only leave the file where it is if we're working locally. If we don't copy
			// (or move) to the project folder other users can't see it.
			m_rbLeave.Enabled = true;
			switch (s_defaultFileLocChoiceForSession)
			{
				case FileLocationChoice.Copy:
					m_rbCopy.Checked = true;
					break;
				case FileLocationChoice.Move:
					m_rbMove.Checked = true;
					break;
				case FileLocationChoice.Leave:
					m_rbLeave.Checked = true;
					break;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Show the file chooser dialog for opening an image file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private DialogResult ShowChoosePictureDlg()
		{
			DialogResult dialogResult = DialogResult.None;
			using (var dlg = new OpenFileDialogAdapter())
			{
				dlg.InitialDirectory = (m_grpFileLocOptions.Visible) ? m_txtDestination.Text :
					s_defaultPicturesFolder;
				dlg.Filter = ResourceHelper.BuildFileFilter(FileFilterType.AllImage, FileFilterType.AllFiles);
				dlg.FilterIndex = 1;
				dlg.Title = FwCoreDlgs.kstidInsertPictureChooseFileCaption;
				dlg.RestoreDirectory = true;
				dlg.CheckFileExists = true;
				dlg.CheckPathExists = true;

				while (dialogResult != DialogResult.OK && dialogResult != DialogResult.Cancel)
				{
					dialogResult = dlg.ShowDialog(m_app == null ? null : m_app.ActiveMainWindow);
					if (dialogResult == DialogResult.OK)
					{
						string file = dlg.FileName;
						if (String.IsNullOrEmpty(file))
							return DialogResult.Cancel;
						Image image;
						try
						{
							image = Image.FromFile(FileUtils.ActualFilePath(file));
						}
						catch (OutOfMemoryException) // unsupported image format
						{
							MessageBoxUtils.Show(FwCoreDlgs.kstidInsertPictureReadError,
								FwCoreDlgs.kstidInsertPictureReadErrorCaption);
							dialogResult = DialogResult.None;
							continue;
						}
						m_filePath = file;
						m_currentImage = image;
						UpdatePicInformation();
						if (m_grpFileLocOptions.Visible)
							ApplyDefaultFileLocationChoice();
					}
				}
			}
			return dialogResult;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes an empty caption string.
		/// </summary>
		/// <returns>An empty caption string with the correct writing system</returns>
		/// ------------------------------------------------------------------------------------
		private ITsString MakeEmptyCaptionString()
		{
			ITsStrFactory factory = TsStrFactoryClass.Create();
			return factory.MakeString(string.Empty, m_captionWs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the information in the dialog for the current image
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void UpdatePicInformation()
		{
			if (m_currentImage != null)
			{
				// update the image
				int newWidth;
				int newHeight;
				float ratio = (float)m_currentImage.Height / m_currentImage.Width;

				if ((int)(m_picPreview.Width * ratio) < m_picPreview.Height)
				{
					newWidth = m_picPreview.Width;
					newHeight = (int)(newWidth * ratio);
				}
				else
				{
					newHeight = m_picPreview.Height;
					newWidth = (int)(newHeight * (1f / ratio));
				}

				m_picPreview.Image = new Bitmap(m_currentImage, newWidth, newHeight);
			}

			// Add "(not found)" if the original file isn't available
			string tmpOriginalPath = m_filePath;
			if (String.IsNullOrEmpty(tmpOriginalPath))
				m_grpFileLocOptions.Visible = false;
			else
			{
				if (!File.Exists(tmpOriginalPath))
					tmpOriginalPath = tmpOriginalPath.Normalize();
				if (!File.Exists(tmpOriginalPath))
					tmpOriginalPath = tmpOriginalPath.Normalize(System.Text.NormalizationForm.FormD);
				if (!File.Exists(tmpOriginalPath))
				{
					tmpOriginalPath =
						string.Format(FwCoreDlgs.kstidPictureUnavailable, tmpOriginalPath.Normalize());
					m_grpFileLocOptions.Visible = false;
				}
				else
				{
					m_grpFileLocOptions.Visible = !FileIsInLinkedFilesFolder(tmpOriginalPath);
				}
			}

			// update the path
			lblFilename.Text = tmpOriginalPath;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether file is in the LinkedFiles folder.
		/// </summary>
		/// <param name="sFilePath">The file path.</param>
		/// ------------------------------------------------------------------------------------
		private bool FileIsInLinkedFilesFolder(string sFilePath)
		{
			return MoveOrCopyFilesDlg.FileIsInExternalLinksFolder(sFilePath,
				s_defaultPicturesFolder);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether given folder is in LinkedFiles folder.
		/// </summary>
		/// <param name="sFolder">The full path of the folder to check.</param>
		/// ------------------------------------------------------------------------------------
		private bool IsFolderInLinkedFilesFolder(string sFolder)
		{
			if (!Directory.Exists(sFolder))
				return false;
			sFolder = sFolder.ToLowerInvariant().TrimEnd(Path.DirectorySeparatorChar);
			string sExtLinksRoot = s_defaultPicturesFolder;
			sExtLinksRoot = sExtLinksRoot.ToLowerInvariant().TrimEnd(Path.DirectorySeparatorChar);
			// Check whether the file is found within the directory.  If so, just return.
			if (sFolder.StartsWith(sExtLinksRoot))
			{
				int cchDir = sExtLinksRoot.Length;
				return (sFolder.Length == cchDir ||
					(sFolder.Length > cchDir && sFolder[cchDir] == Path.DirectorySeparatorChar));
			}
			return false;
		}
		#endregion
	}
}
