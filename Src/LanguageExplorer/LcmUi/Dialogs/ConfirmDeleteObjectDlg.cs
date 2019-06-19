// Copyright (c) 2004-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FwCoreDlgs.Controls;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;

namespace LanguageExplorer.LcmUi.Dialogs
{
	/// <summary>
	/// This dialog window allows for a user to confirm deleting an object, or cancel the deletion.
	/// </summary>
	public class ConfirmDeleteObjectDlg : Form
	{
		private Label label1;
		private Label label2;
		private PictureBox pictureBox1;
		private Button m_deleteButton;
		private Button m_cancelButton;
		private readonly FwTextBox m_descriptionBox3;
		private readonly FwTextBox m_descriptionBox4;
		protected LcmCache m_cache;
		private Panel panel1;
		private Panel panel2;
		private Button buttonHelp;
		private string s_helpTopic;
		private HelpProvider helpProvider;
		private readonly IHelpTopicProvider m_helpTopicProvider;
		private IContainer components = null;

		private ConfirmDeleteObjectDlg()
		{
			InitializeComponent();
			AccessibleName = "ConfirmDeleteObjectDlg";
			pictureBox1.Image = System.Drawing.SystemIcons.Exclamation.ToBitmap();
			m_descriptionBox3 = new FwTextBox
			{
				AdjustStringHeight = true,
				WordWrap = true,
				controlID = null,
				Name = "m_descriptionBox3",
				Enabled = false,
				TabStop = false,
				AccessibleName = "FwTextBox",
				BackColor = System.Drawing.SystemColors.Control,
				HasBorder = false,
				Location = new System.Drawing.Point(5, 5),
				Size = new System.Drawing.Size(304, 184),
				TabIndex = 0,
				Dock = DockStyle.Fill,
				Visible = true
			};
			panel1.Controls.Add(m_descriptionBox3);
			m_descriptionBox4 = new FwTextBox
			{
				AdjustStringHeight = true,
				WordWrap = true,
				controlID = null,
				Name = "m_descriptionBox4",
				Enabled = false,
				TabStop = false,
				AccessibleName = "FwTextBox",
				BackColor = System.Drawing.SystemColors.Control,
				HasBorder = false,
				Location = new System.Drawing.Point(16, 56),
				Size = new System.Drawing.Size(304, 184),
				TabIndex = 0,
				Dock = DockStyle.Fill,
				Visible = true
			};
			panel2.Controls.Add(m_descriptionBox4);
		}

		public ConfirmDeleteObjectDlg(IHelpTopicProvider helpTopicProvider) : this()
		{
			m_helpTopicProvider = helpTopicProvider;
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
			base.Dispose(disposing);
		}

		public void SetDlgInfo(CmObjectUi obj, LcmCache cache, IPropertyTable propertyTable)
		{
			Debug.Assert(obj != null);
			Debug.Assert(obj.MyCmObject != null);
			SetDlgInfo(obj, cache, propertyTable, TsStringUtils.MakeString(" ", cache.DefaultUserWs));
		}


		public void SetDlgInfo(CmObjectUi obj, LcmCache cache, IPropertyTable propertyTable, ITsString tssNote)
		{
			if (obj.PropertyTable == null)
			{
				obj.PropertyTable = propertyTable;
			}
			m_cache = cache;
			IVwStylesheet stylesheet = FwUtils.StyleSheetFromPropertyTable(propertyTable);
			Debug.Assert(obj != null);
			Debug.Assert(obj.MyCmObject != null);
			Text = string.Format(LcmUiStrings.ksDeleteX, obj.MyCmObject.DisplayNameOfClass(cache));
			// Set the s_helpTopic based on the window title and rearrange the buttons if necessary
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
				helpProvider = new HelpProvider { HelpNamespace = m_helpTopicProvider.HelpFile };
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
			var defUserWs = m_cache.ServiceLocator.WritingSystemManager.UserWs;
			m_descriptionBox3.WritingSystemFactory = m_cache.WritingSystemFactory;
			m_descriptionBox3.WritingSystemCode = defUserWs;
			m_descriptionBox3.StyleSheet = stylesheet;
			var tisb3 = TsStringUtils.MakeIncStrBldr();
			tisb3.AppendTsString(obj.MyCmObject.DeletionTextTSS);
			m_descriptionBox3.Tss = tisb3.GetString();
			// Adjust the dialog size if needed to display the message (FWNX-857).
			var deltaY = GrowTextBox(panel1, m_descriptionBox3);
			panel2.Top += deltaY;
			m_descriptionBox4.WritingSystemFactory = m_cache.WritingSystemFactory;
			m_descriptionBox4.WritingSystemCode = defUserWs;
			m_descriptionBox4.StyleSheet = stylesheet;
			var tisb4 = TsStringUtils.MakeIncStrBldr();
			tisb4.AppendTsString(tssNote); //this is the default for m_descriptionBox4
			m_descriptionBox4.Tss = tisb4.GetString();
			GrowTextBox(panel2, m_descriptionBox4);
			m_deleteButton.Enabled = obj.MyCmObject.CanDelete;
			label2.Visible = m_deleteButton.Enabled;
		}

		private int GrowTextBox(Panel panel, FwTextBox textBox)
		{
			var deltaY = textBox.PreferredHeight - textBox.Height;
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
				return label1.Text;
			}
			set
			{
				label1.Text = value;
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
				return label2.Text;
			}
			set
			{
				label2.Text = value;
			}
		}

		/// <summary>
		/// Allow customizing the top area of the body.
		/// </summary>
		public string TopBodyText
		{
			get
			{
				return m_descriptionBox3.Text;
			}
			set
			{
				m_descriptionBox3.Text = value;
			}
		}

		/// <summary>
		/// Allow customizing the bottom area of the body.
		/// </summary>
		public string BottomBodyText
		{
			get
			{
				return m_descriptionBox4.Text;
			}
			set
			{
				m_descriptionBox4.Text = value;
			}
		}

		/// <summary>
		/// Allow customizing the dialog window title.
		/// </summary>
		public string WindowTitle
		{
			get
			{
				return Text;
			}
			set
			{
				Text = value;
			}
		}

		/// <summary>
		/// The text shown on the delete/confirm button.
		/// </summary>
		public string DeleteButtonText
		{
			get
			{
				return m_deleteButton.Text;
			}
			set
			{
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
				return m_cancelButton.Text;
			}
			set
			{
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