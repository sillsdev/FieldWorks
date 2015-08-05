using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.DomainServices;
using XCore;
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	public class AddNewSenseDlg : Form, IFWDisposable
	{
		private const string s_helpTopic = "khtpAddNewSense";
		private System.Windows.Forms.HelpProvider helpProvider;

		#region Data members

		private bool m_skipCheck = false;
		private IHelpTopicProvider m_helpTopicProvider;
		private FdoCache m_cache;
		private ILexEntry m_le;
		private int m_newSenseID = 0;


		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button m_btnOK;
		private System.Windows.Forms.Button m_btnCancel;
		private SIL.FieldWorks.Common.Widgets.FwTextBox m_fwtbCitationForm;
		private SIL.FieldWorks.Common.Widgets.FwTextBox m_fwtbGloss;
		private System.Windows.Forms.Label label2;
		private SIL.FieldWorks.LexText.Controls.MSAGroupBox m_msaGroupBox;
		private System.Windows.Forms.Button buttonHelp;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		#endregion Data members

		#region Properties

#if false
		private string Gloss
		{
			get { return m_txtGloss.Text.Trim(); }
			set { m_txtGloss.Text = value.Trim(); }
		}

		public MsaType MsaType
		{
			get
			{
				CheckDisposed();
				/* MSAOVERHAUL
				if (m_msaSelector != null)
					return m_msaSelector.MSAType;
				else
					return MsaType.kStem;
					*/
				return MsaType.kStem;
			}
			set
			{
				CheckDisposed();
				/* MSAOVERHAUL
				if (m_msaSelector != null)
					m_msaSelector.MSAType = value;
					*/

			}
		}
		public FDO.Ling.MoInflAffixSlot Slot
		{
			get
			{
				CheckDisposed();
				/* MSAOVERHAUL
				if (m_msaSelector != null)
					return m_msaSelector.Slot;
				else
					return null;
					*/
				return null;
			}
			set
			{
				CheckDisposed();
				/* MSAOVERHAUL
				if (m_msaSelector != null)
					m_msaSelector.Slot = value;
					*/
			}
		}
#endif

		#endregion Properties

		private AddNewSenseDlg()
		{
			BasicInit();

			// Figure out where to locate the dlg.
			/*
			object obj = SettingsKey.GetValue("InsertX");
			if (obj != null)
			{
				int x = (int)obj;
				int y = (int)SettingsKey.GetValue("InsertY");
				int width = (int)SettingsKey.GetValue("InsertWidth", Width);
				int height = (int)SettingsKey.GetValue("InsertHeight", Height);
				Rectangle rect = new Rectangle(x, y, width, height);
				ScreenUtils.EnsureVisibleRect(ref rect);
				DesktopBounds = rect;
				StartPosition = FormStartPosition.Manual;
			}*/
		}

		private void BasicInit()
		{
			if (m_fwtbCitationForm != null)
				return;
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			AccessibleName = GetType().Name;

			m_le = null;
		}

		public AddNewSenseDlg(IHelpTopicProvider helpTopicProvider)
		{
			BasicInit();

			m_helpTopicProvider = helpTopicProvider;
			helpProvider = new HelpProvider();
			helpProvider.HelpNamespace = m_helpTopicProvider.HelpFile;
			helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(s_helpTopic));
			helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
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

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing )
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				if (components != null)
				{
					components.Dispose();
				}

			}
			m_le = null;
			m_cache = null;

			base.Dispose( disposing );
		}

		/// <summary>
		/// This sets the original citation form into the dialog.
		/// </summary>
		/// <param name="tssCitationForm"></param>
		/// <param name="le"></param>
		/// <param name="mediator"></param>
		/// <param name="propertyTable"></param>
		public void SetDlgInfo(ITsString tssCitationForm, ILexEntry le, Mediator mediator, IPropertyTable propertyTable)
		{
			CheckDisposed();

			Debug.Assert(tssCitationForm != null);
			Debug.Assert(le != null);

			m_le = le;
			m_cache = le.Cache;

			IWritingSystemContainer wsContainer = m_cache.ServiceLocator.WritingSystems;
			IWritingSystem defVernWs = wsContainer.DefaultVernacularWritingSystem;
			IWritingSystem defAnalWs = wsContainer.DefaultAnalysisWritingSystem;
			m_fwtbCitationForm.Font = new Font(defVernWs.DefaultFontName, 10);
			m_fwtbGloss.Font = new Font(defAnalWs.DefaultFontName, 10);
			var stylesheet = FontHeightAdjuster.StyleSheetFromPropertyTable(propertyTable);
			// Set writing system factory and code for the two edit boxes.
			m_fwtbCitationForm.WritingSystemFactory = m_cache.WritingSystemFactory;
			m_fwtbCitationForm.WritingSystemCode = defVernWs.Handle;
			m_fwtbCitationForm.StyleSheet = stylesheet;
			m_fwtbCitationForm.AdjustStringHeight = false;
			m_fwtbGloss.WritingSystemFactory = m_cache.WritingSystemFactory;
			m_fwtbGloss.WritingSystemCode = defAnalWs.Handle;
			m_fwtbGloss.StyleSheet = stylesheet;
			m_fwtbGloss.AdjustStringHeight = false;
			m_fwtbCitationForm.Tss = tssCitationForm;
			m_fwtbGloss.Text = String.Empty;
			m_fwtbCitationForm.HasBorder = false;

			m_msaGroupBox.Initialize(m_cache, mediator, propertyTable, this, new SandboxGenericMSA());

			// get the current morph type from the lexical entry.
			IMoMorphType mmt;
			foreach (var mf in le.AlternateFormsOS)
			{
				mmt = mf.MorphTypeRA;
				if (mmt != null)
				{
					m_msaGroupBox.MorphTypePreference = mmt;
					break; // Assume the first allomorph's type is good enough.
				}
			}

			m_skipCheck = true;
			m_skipCheck = false;

			// Adjust sizes of the two FwTextBoxes if needed, and adjust the locations for the
			// controls below them.  Do the same for the MSAGroupBox.
			AdjustHeightAndPositions(m_fwtbCitationForm);
			AdjustHeightAndPositions(m_fwtbGloss);
			AdjustHeightAndPositions(m_msaGroupBox);
		}

		private void AdjustHeightAndPositions(FwTextBox fwtb)
		{
			int oldHeight = fwtb.Height;
			int newHeight = Math.Max(oldHeight, fwtb.PreferredHeight);
			int delta = newHeight - oldHeight;
			if (delta > 0)
			{
				fwtb.Height = newHeight;
				FontHeightAdjuster.GrowDialogAndAdjustControls(this, delta, fwtb);
			}
		}

		private void AdjustHeightAndPositions(MSAGroupBox msagb)
		{
			int oldHeight = msagb.Height;
			int newHeight = Math.Max(oldHeight, msagb.PreferredHeight);
			int delta = newHeight - oldHeight;
			if (delta > 0)
			{
				msagb.AdjustInternalControlsAndGrow();
				Debug.Assert(msagb.Height == msagb.PreferredHeight);
				FontHeightAdjuster.GrowDialogAndAdjustControls(this, delta, msagb);
			}
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AddNewSenseDlg));
			this.label1 = new System.Windows.Forms.Label();
			this.m_btnOK = new System.Windows.Forms.Button();
			this.m_btnCancel = new System.Windows.Forms.Button();
			this.m_fwtbCitationForm = new SIL.FieldWorks.Common.Widgets.FwTextBox();
			this.m_fwtbGloss = new SIL.FieldWorks.Common.Widgets.FwTextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.m_msaGroupBox = new SIL.FieldWorks.LexText.Controls.MSAGroupBox();
			this.buttonHelp = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.m_fwtbCitationForm)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_fwtbGloss)).BeginInit();
			this.SuspendLayout();
			//
			// label1
			//
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			//
			// m_btnOK
			//
			this.m_btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			resources.ApplyResources(this.m_btnOK, "m_btnOK");
			this.m_btnOK.Name = "m_btnOK";
			//
			// m_btnCancel
			//
			this.m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(this.m_btnCancel, "m_btnCancel");
			this.m_btnCancel.Name = "m_btnCancel";
			//
			// m_fwtbCitationForm
			//
			this.m_fwtbCitationForm.AdjustStringHeight = true;
			this.m_fwtbCitationForm.BackColor = System.Drawing.SystemColors.Control;
			this.m_fwtbCitationForm.controlID = null;
			resources.ApplyResources(this.m_fwtbCitationForm, "m_fwtbCitationForm");
			this.m_fwtbCitationForm.Name = "m_fwtbCitationForm";
			this.m_fwtbCitationForm.SelectionLength = 0;
			this.m_fwtbCitationForm.SelectionStart = 0;
			// Bad idea! Causes a crash (see FWR-2528).
			// WritingSystemCode gets set appropriately in SetDlgInfo().
			//this.m_fwtbCitationForm.WritingSystemCode = 1;
			//
			// m_fwtbGloss
			//
			this.m_fwtbGloss.AdjustStringHeight = true;
			this.m_fwtbGloss.BackColor = System.Drawing.SystemColors.Window;
			this.m_fwtbGloss.controlID = null;
			resources.ApplyResources(this.m_fwtbGloss, "m_fwtbGloss");
			this.m_fwtbGloss.Name = "m_fwtbGloss";
			this.m_fwtbGloss.SelectionLength = 0;
			this.m_fwtbGloss.SelectionStart = 0;
			// Bad idea! Causes a crash (see FWR-2528).
			// WritingSystemCode gets set appropriately in SetDlgInfo().
			//this.m_fwtbGloss.WritingSystemCode = 1;
			//
			// label2
			//
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			//
			// m_msaGroupBox
			//
			resources.ApplyResources(this.m_msaGroupBox, "m_msaGroupBox");
			this.m_msaGroupBox.MSAType = SIL.FieldWorks.FDO.MsaType.kNotSet;
			this.m_msaGroupBox.Name = "m_msaGroupBox";
			this.m_msaGroupBox.Slot = null;
			//
			// buttonHelp
			//
			resources.ApplyResources(this.buttonHelp, "buttonHelp");
			this.buttonHelp.Name = "buttonHelp";
			this.buttonHelp.Click += new System.EventHandler(this.buttonHelp_Click);
			//
			// AddNewSenseDlg
			//
			this.AcceptButton = this.m_btnOK;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.m_btnCancel;
			this.ControlBox = false;
			this.Controls.Add(this.label2);
			this.Controls.Add(this.m_fwtbGloss);
			this.Controls.Add(this.m_fwtbCitationForm);
			this.Controls.Add(this.m_btnCancel);
			this.Controls.Add(this.m_btnOK);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.buttonHelp);
			this.Controls.Add(this.m_msaGroupBox);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Name = "AddNewSenseDlg";
			this.ShowInTaskbar = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.Closing += new System.ComponentModel.CancelEventHandler(this.AddNewSenseDlg_Closing);
			((System.ComponentModel.ISupportInitialize)(this.m_fwtbCitationForm)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_fwtbGloss)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion

		/// <summary>
		/// Retrieve the data from the dialog.
		/// </summary>
		public void GetDlgInfo(out int newSenseID)
		{
			CheckDisposed();

			newSenseID = m_newSenseID;
		}

		private void AddNewSenseDlg_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			switch (DialogResult)
			{
				default:
				{
					Debug.Assert(false, "Unexpected DialogResult.");
					break;
				}
				case DialogResult.Cancel:
				{
					break;
				}
				case DialogResult.OK:
				{
					if (m_fwtbGloss.Text == String.Empty)
					{
						e.Cancel = true;
						MessageBox.Show(this, LexTextControls.ksFillInGloss,
							LexTextControls.ksMissingInformation,
							MessageBoxButtons.OK, MessageBoxIcon.Information);
						return;
					}

					using (new WaitCursor(this))
					{
						m_cache.DomainDataByFlid.BeginUndoTask(LexTextControls.ksUndoCreateNewSense,
							LexTextControls.ksRedoCreateNewSense);

						var lsNew = m_cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
						m_le.SensesOS.Add(lsNew);
						int defAnalWs = m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle;
						lsNew.Gloss.set_String(defAnalWs, m_cache.TsStrFactory.MakeString(m_fwtbGloss.Text, defAnalWs));

						lsNew.SandboxMSA = m_msaGroupBox.SandboxMSA;
						m_newSenseID = lsNew.Hvo;

						m_cache.DomainDataByFlid.EndUndoTask();
					}
					break;
				}
			}
		}

		private void buttonHelp_Click(object sender, System.EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, s_helpTopic);
		}
	}
}
