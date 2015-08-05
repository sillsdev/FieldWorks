using System;
using System.Diagnostics;
using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.FdoUi.Dialogs
{
	/// <summary>
	/// This dialog window allows for a user to confirm deleting an object, or cancel the deletion.
	/// </summary>
	public class ConfirmDeleteObjectDlg : Form, IFWDisposable
	{
		private Label label1;
		private Label label2;
		private PictureBox pictureBox1;
		private Button m_deleteButton;
		private Button m_cancelButton;
		private readonly FwTextBox m_descriptionBox3;
		private readonly FwTextBox m_descriptionBox4;
		protected FdoCache m_cache;
		private Panel panel1;
		private Panel panel2;
		private Button buttonHelp;
		private string s_helpTopic;
		private HelpProvider helpProvider;
		private readonly IHelpTopicProvider m_helpTopicProvider;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		private ConfirmDeleteObjectDlg()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			AccessibleName = "ConfirmDeleteObjectDlg";

			this.pictureBox1.Image = System.Drawing.SystemIcons.Exclamation.ToBitmap();

			//
			// m_descriptionBox3
			//
			this.m_descriptionBox3 = new FwTextBox();
			this.m_descriptionBox3.AdjustStringHeight = true;
			this.m_descriptionBox3.WordWrap = true;
			this.m_descriptionBox3.controlID = null;
			this.m_descriptionBox3.Name = "m_descriptionBox3";
			this.m_descriptionBox3.Enabled = false;
			this.m_descriptionBox3.TabStop = false;
			this.m_descriptionBox3.AccessibleName = "FwTextBox";
			this.m_descriptionBox3.BackColor = System.Drawing.SystemColors.Control; // not implemented: System.Drawing.Color.Transparent;
			this.m_descriptionBox3.HasBorder = false;
			this.m_descriptionBox3.Location = new System.Drawing.Point(5, 5);
			this.m_descriptionBox3.Size = new System.Drawing.Size(304, 184);
			this.m_descriptionBox3.TabIndex = 0;
			this.m_descriptionBox3.TabStop = false;
			this.m_descriptionBox3.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_descriptionBox3.Visible = true;

			panel1.Controls.Add(m_descriptionBox3);

			//
			// m_descriptionBox3
			//
			this.m_descriptionBox4 = new FwTextBox();
			this.m_descriptionBox4.AdjustStringHeight = true;
			this.m_descriptionBox4.WordWrap = true;
			this.m_descriptionBox4.controlID = null;
			this.m_descriptionBox4.Name = "m_descriptionBox4";
			this.m_descriptionBox4.Enabled = false;
			this.m_descriptionBox4.TabStop = false;
			this.m_descriptionBox4.AccessibleName = "FwTextBox";
			this.m_descriptionBox4.BackColor = System.Drawing.SystemColors.Control; // not implemented: System.Drawing.Color.Transparent;
			this.m_descriptionBox4.HasBorder = false;
			this.m_descriptionBox4.Location = new System.Drawing.Point(16,56);
			this.m_descriptionBox4.Size = new System.Drawing.Size(304, 184);
			this.m_descriptionBox4.TabIndex = 0;
			this.m_descriptionBox4.TabStop = false;
			this.m_descriptionBox4.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_descriptionBox4.Visible = true;

			panel2.Controls.Add(m_descriptionBox4);
		}

		public ConfirmDeleteObjectDlg(IHelpTopicProvider helpTopicProvider) : this()
		{
			m_helpTopicProvider = helpTopicProvider;
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

		public void SetDlgInfo(CmObjectUi obj, FdoCache cache, Mediator mediator, IPropertyTable propertyTable)
		{
			CheckDisposed();

			Debug.Assert(obj != null);
			Debug.Assert(obj.Object != null);

			SetDlgInfo(obj, cache, mediator, propertyTable, cache.TsStrFactory.MakeString(" ", cache.DefaultUserWs));
		}


		public void SetDlgInfo(CmObjectUi obj, FdoCache cache, Mediator mediator, IPropertyTable propertyTable, ITsString tssNote)
		{

			CheckDisposed();

			if (obj.Mediator == null)
			{
				obj.Mediator = mediator;
			}
			if (obj.PropTable == null)
			{
				obj.PropTable = propertyTable;
			}
			m_cache = cache;
			IVwStylesheet stylesheet = FontHeightAdjuster.StyleSheetFromPropertyTable(propertyTable);

			Debug.Assert(obj != null);
			Debug.Assert(obj.Object != null);

			Text = String.Format(FdoUiStrings.ksDeleteX, obj.DisplayNameOfClass);

			// Set the s_helpTopic based on the window title and rearrange the buttons if neccesary
			switch (obj.ClassName)
			{
				case "WfiWordform":
					s_helpTopic = "khtpDeleteWordform";
					break;
			}
			if (s_helpTopic != null)
			{
				buttonHelp.Visible = true;
				buttonHelp.Enabled = true;
				helpProvider = new HelpProvider();
				helpProvider.HelpNamespace = m_helpTopicProvider.HelpFile;
				helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(s_helpTopic));
				helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
			}
			else
			{
				m_deleteButton.Location = m_cancelButton.Location;
				m_cancelButton.Location = buttonHelp.Location;
			}

			//Use an FWTextBox so that strings of different writing systems will
			//be displayed with the correct stylesheet settings.
			int defUserWs = m_cache.ServiceLocator.WritingSystemManager.UserWs;
			m_descriptionBox3.WritingSystemFactory = m_cache.WritingSystemFactory;
			m_descriptionBox3.WritingSystemCode = defUserWs;
			m_descriptionBox3.StyleSheet = stylesheet;
			ITsIncStrBldr tisb3 = TsIncStrBldrClass.Create();
			tisb3.AppendTsString(obj.Object.DeletionTextTSS);
			m_descriptionBox3.Tss = tisb3.GetString();
			// Adjust the dialog size if needed to display the message (FWNX-857).
			int deltaY = GrowTextBox(panel1, m_descriptionBox3);
			panel2.Top += deltaY;

			m_descriptionBox4.WritingSystemFactory = m_cache.WritingSystemFactory;
			m_descriptionBox4.WritingSystemCode = defUserWs;
			m_descriptionBox4.StyleSheet = stylesheet;
			ITsIncStrBldr tisb4 = TsIncStrBldrClass.Create();
			tisb4.AppendTsString(tssNote); //this is the default for m_descriptionBox4
			m_descriptionBox4.Tss = tisb4.GetString();
			GrowTextBox(panel2, m_descriptionBox4);

			m_deleteButton.Enabled = obj.Object.CanDelete;
			label2.Visible = m_deleteButton.Enabled;
		}

		private int GrowTextBox(Panel panel, FwTextBox textBox)
		{
			int deltaY = textBox.PreferredHeight - textBox.Height;
			if (deltaY > 0)
			{
				panel.Height += deltaY;
				Height += deltaY;
				// Reinitialize the string.  Otherwise only the first line is displayed for some reason.
				textBox.Tss = textBox.Tss;
				return deltaY;
			}
			return 0;
		}

		/// <summary>
		/// Allow customizing the message in the top line of the dialog box.  It defaults to
		/// "You are deleting the following item:"
		/// </summary>
		public string TopMessage
		{
			get
			{
				CheckDisposed();
				return this.label1.Text;
			}
			set
			{
				CheckDisposed();
				this.label1.Text = value;
			}
		}

		/// <summary>
		/// Allow customizing the question at the bottom of the dialog box.  It defaults to
		/// "Do you want to continue with the deletion?"
		/// </summary>
		public string BottomQuestion
		{
			get
			{
				CheckDisposed();
				return this.label2.Text;
			}
			set
			{
				CheckDisposed();
				this.label2.Text = value;
			}
		}

		/// <summary>
		/// Allow customizing the top area of the body.
		/// </summary>
		public string TopBodyText
		{
			get
			{
				CheckDisposed();
				return this.m_descriptionBox3.Text;
			}
			set
			{
				CheckDisposed();
				this.m_descriptionBox3.Text = value;
			}
		}

		/// <summary>
		/// Allow customizing the bottom area of the body.
		/// </summary>
		public string BottomBodyText
		{
			get
			{
				CheckDisposed();
				return this.m_descriptionBox4.Text;
			}
			set
			{
				CheckDisposed();
				this.m_descriptionBox4.Text = value;
			}
		}

		/// <summary>
		/// Allow customizing the dialog window title.
		/// </summary>
		public string WindowTitle
		{
			get
			{
				CheckDisposed();
				return this.Text;
			}
			set
			{
				CheckDisposed();
				this.Text = value;
			}
		}

		/// <summary>
		/// The text shown on the delete/confirm button.
		/// </summary>
		public string DeleteButtonText
		{
			get
			{
				CheckDisposed();
				return m_deleteButton.Text;
			}
			set
			{
				CheckDisposed();
				m_deleteButton.Text = value;
			}
		}

		/// <summary>
		/// The text shown on the cancel/reject button.
		/// </summary>
		public string CancelButtonText
		{
			get
			{
				CheckDisposed();
				return m_cancelButton.Text;
			}
			set
			{
				CheckDisposed();
				m_cancelButton.Text = value;
			}
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConfirmDeleteObjectDlg));
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.m_deleteButton = new System.Windows.Forms.Button();
			this.m_cancelButton = new System.Windows.Forms.Button();
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.panel1 = new System.Windows.Forms.Panel();
			this.panel2 = new System.Windows.Forms.Panel();
			this.buttonHelp = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			this.SuspendLayout();
			//
			// label1
			//
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			//
			// label2
			//
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			//
			// m_deleteButton
			//
			resources.ApplyResources(this.m_deleteButton, "m_deleteButton");
			this.m_deleteButton.DialogResult = System.Windows.Forms.DialogResult.Yes;
			this.m_deleteButton.Name = "m_deleteButton";
			this.m_deleteButton.Leave += new System.EventHandler(this.m_deleteButton_Leave);
			//
			// m_cancelButton
			//
			resources.ApplyResources(this.m_cancelButton, "m_cancelButton");
			this.m_cancelButton.DialogResult = System.Windows.Forms.DialogResult.No;
			this.m_cancelButton.Name = "m_cancelButton";
			this.m_cancelButton.Leave += new System.EventHandler(this.m_cancelButton_Leave);
			//
			// pictureBox1
			//
			resources.ApplyResources(this.pictureBox1, "pictureBox1");
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.TabStop = false;
			//
			// panel1
			//
			resources.ApplyResources(this.panel1, "panel1");
			this.panel1.Name = "panel1";
			//
			// panel2
			//
			resources.ApplyResources(this.panel2, "panel2");
			this.panel2.Name = "panel2";
			//
			// buttonHelp
			//
			resources.ApplyResources(this.buttonHelp, "buttonHelp");
			this.buttonHelp.Name = "buttonHelp";
			this.buttonHelp.Click += new System.EventHandler(this.buttonHelp_Click);
			//
			// ConfirmDeleteObjectDlg
			//
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.m_cancelButton;
			this.CausesValidation = false;
			this.ControlBox = false;
			this.Controls.Add(this.panel2);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.buttonHelp);
			this.Controls.Add(this.pictureBox1);
			this.Controls.Add(this.m_cancelButton);
			this.Controls.Add(this.m_deleteButton);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ConfirmDeleteObjectDlg";
			this.ShowInTaskbar = false;
			this.Activated += new System.EventHandler(this.ConfirmDeleteObjectDlg_Activated);
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion

		private void ConfirmDeleteObjectDlg_Activated(object sender, System.EventArgs e)
		{
			m_deleteButton.TabStop = true;
			m_cancelButton.TabStop = true;
			m_cancelButton.Focus();
		}

		private void m_deleteButton_Leave(object sender, System.EventArgs e)
		{
			m_cancelButton.Focus();
		}

		private void m_cancelButton_Leave(object sender, System.EventArgs e)
		{
			m_deleteButton.Focus();
		}

		private void buttonHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, s_helpTopic);
		}
	}
}
