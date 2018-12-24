// Copyright (c) 2004-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FwCoreDlgs.Controls;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Controls.LexText
{
	/// <summary />
	public class AddNewSenseDlg : Form
	{
		private const string s_helpTopic = "khtpAddNewSense";
		private HelpProvider helpProvider;
		private bool m_skipCheck;
		private IHelpTopicProvider m_helpTopicProvider;
		private LcmCache m_cache;
		private ILexEntry m_le;
		private int m_newSenseID;
		private Label label1;
		private Button m_btnOK;
		private Button m_btnCancel;
		private FwTextBox m_fwtbCitationForm;
		private FwTextBox m_fwtbGloss;
		private Label label2;
		private MSAGroupBox m_msaGroupBox;
		private Button buttonHelp;
		private Container components = null;

		private AddNewSenseDlg()
		{
			BasicInit();
		}

		private void BasicInit()
		{
			if (m_fwtbCitationForm != null)
			{
				return;
			}
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
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				components?.Dispose();

			}
			m_le = null;
			m_cache = null;

			base.Dispose(disposing);
		}

		/// <summary>
		/// This sets the original citation form into the dialog.
		/// </summary>
		public void SetDlgInfo(ITsString tssCitationForm, ILexEntry le, IPropertyTable propertyTable, IPublisher publisher)
		{
			Guard.AgainstNull(tssCitationForm, nameof(tssCitationForm));
			Guard.AgainstNull(le, nameof(le));

			m_le = le;
			m_cache = le.Cache;
			var wsContainer = m_cache.ServiceLocator.WritingSystems;
			var defVernWs = wsContainer.DefaultVernacularWritingSystem;
			var defAnalWs = wsContainer.DefaultAnalysisWritingSystem;
			m_fwtbCitationForm.Font = new Font(defVernWs.DefaultFontName, 10);
			m_fwtbGloss.Font = new Font(defAnalWs.DefaultFontName, 10);
			var stylesheet = FwUtils.StyleSheetFromPropertyTable(propertyTable);
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
			m_fwtbGloss.Text = string.Empty;
			m_fwtbCitationForm.HasBorder = false;
			m_msaGroupBox.Initialize(m_cache, propertyTable, publisher, this, new SandboxGenericMSA());
			// get the current morph type from the lexical entry.
			foreach (var mf in le.AlternateFormsOS)
			{
				var mmt = mf.MorphTypeRA;
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
			var oldHeight = fwtb.Height;
			var newHeight = Math.Max(oldHeight, fwtb.PreferredHeight);
			var delta = newHeight - oldHeight;
			if (delta > 0)
			{
				fwtb.Height = newHeight;
				FontHeightAdjuster.GrowDialogAndAdjustControls(this, delta, fwtb);
			}
		}

		private void AdjustHeightAndPositions(MSAGroupBox msagb)
		{
			var oldHeight = msagb.Height;
			var newHeight = Math.Max(oldHeight, msagb.PreferredHeight);
			var delta = newHeight - oldHeight;
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
			this.m_fwtbCitationForm = new SIL.FieldWorks.FwCoreDlgs.Controls.FwTextBox();
			this.m_fwtbGloss = new SIL.FieldWorks.FwCoreDlgs.Controls.FwTextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.m_msaGroupBox = new LanguageExplorer.Controls.LexText.MSAGroupBox();
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
			this.m_msaGroupBox.MSAType = MsaType.kNotSet;
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
						if (m_fwtbGloss.Text == string.Empty)
						{
							e.Cancel = true;
							MessageBox.Show(this, LexTextControls.ksFillInGloss, LexTextControls.ksMissingInformation, MessageBoxButtons.OK, MessageBoxIcon.Information);
							return;
						}
						using (new WaitCursor(this))
						{
							m_cache.DomainDataByFlid.BeginUndoTask(LexTextControls.ksUndoCreateNewSense, LexTextControls.ksRedoCreateNewSense);
							var lsNew = m_cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
							m_le.SensesOS.Add(lsNew);
							var defAnalWs = m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle;
							lsNew.Gloss.set_String(defAnalWs, TsStringUtils.MakeString(m_fwtbGloss.Text, defAnalWs));
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