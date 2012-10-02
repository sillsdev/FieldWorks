using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.FieldWorks.Common.Framework;
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Summary description for LexReferenceDetailsDlg.
	/// </summary>
	public class LexReferenceDetailsDlg : Form, IFWDisposable
	{
		private Label label1;
		private Label label3;
		private TextBox m_tbName;
		private TextBox m_tbComment;
		private Button m_btnOK;
		private Button m_btnCancel;
		private Label lblExplanation;
		private Button buttonHelp;

		private const string s_helpTopic = "khtpReferenceSetDetails";
		private HelpProvider helpProvider;
		private IHelpTopicProvider m_helpTopicProvider;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="LexReferenceDetailsDlg"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private LexReferenceDetailsDlg()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			AccessibleName = GetType().Name;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="LexReferenceDetailsDlg"/> class.
		/// </summary>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// ------------------------------------------------------------------------------------
		public LexReferenceDetailsDlg(IHelpTopicProvider helpTopicProvider) : this()
		{
			m_helpTopicProvider = helpTopicProvider;
			helpProvider = new HelpProvider();
			helpProvider.HelpNamespace = helpTopicProvider.HelpFile;
			helpProvider.SetHelpKeyword(this, helpTopicProvider.GetHelpString(s_helpTopic));
			helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
		}

		public string ReferenceName
		{
			get
			{
				CheckDisposed();
				return m_tbName.Text;
			}
			set
			{
				CheckDisposed();
				m_tbName.Text = value;
			}
		}

		public string ReferenceComment
		{
			get
			{
				CheckDisposed();
				return m_tbComment.Text;
			}
			set
			{
				CheckDisposed();
				m_tbComment.Text = value;
			}
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
		protected override void Dispose( bool disposing )
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

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LexReferenceDetailsDlg));
			this.m_tbName = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.m_tbComment = new System.Windows.Forms.TextBox();
			this.m_btnOK = new System.Windows.Forms.Button();
			this.m_btnCancel = new System.Windows.Forms.Button();
			this.lblExplanation = new System.Windows.Forms.Label();
			this.buttonHelp = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// m_tbName
			//
			resources.ApplyResources(this.m_tbName, "m_tbName");
			this.m_tbName.Name = "m_tbName";
			//
			// label1
			//
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			//
			// label3
			//
			resources.ApplyResources(this.label3, "label3");
			this.label3.Name = "label3";
			//
			// m_tbComment
			//
			resources.ApplyResources(this.m_tbComment, "m_tbComment");
			this.m_tbComment.Name = "m_tbComment";
			//
			// m_btnOK
			//
			resources.ApplyResources(this.m_btnOK, "m_btnOK");
			this.m_btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btnOK.Name = "m_btnOK";
			//
			// m_btnCancel
			//
			resources.ApplyResources(this.m_btnCancel, "m_btnCancel");
			this.m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btnCancel.Name = "m_btnCancel";
			//
			// lblExplanation
			//
			resources.ApplyResources(this.lblExplanation, "lblExplanation");
			this.lblExplanation.Name = "lblExplanation";
			//
			// buttonHelp
			//
			resources.ApplyResources(this.buttonHelp, "buttonHelp");
			this.buttonHelp.Name = "buttonHelp";
			this.buttonHelp.Click += new System.EventHandler(this.buttonHelp_Click);
			//
			// LexReferenceDetailsDlg
			//
			this.AcceptButton = this.m_btnOK;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.m_btnCancel;
			this.Controls.Add(this.buttonHelp);
			this.Controls.Add(this.lblExplanation);
			this.Controls.Add(this.m_btnCancel);
			this.Controls.Add(this.m_btnOK);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.m_tbComment);
			this.Controls.Add(this.m_tbName);
			this.Controls.Add(this.label1);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "LexReferenceDetailsDlg";
			this.MinimumSize = new System.Drawing.Size(320, 300);
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		private void buttonHelp_Click(object sender, System.EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, s_helpTopic);
		}
	}
}
