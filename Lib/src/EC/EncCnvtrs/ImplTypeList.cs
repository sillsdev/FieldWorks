using System;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Diagnostics;               // for Debug.Assert

namespace SilEncConverters40
{
	/// <summary>
	/// Summary description for ImplTypeList.
	/// </summary>
	public class ImplTypeList : System.Windows.Forms.Form
	{
		private System.Windows.Forms.ListBox listBoxImplTypes;
		private System.Windows.Forms.Label labelStatic;
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.Button buttonAdd;
		private HelpProvider helpProvider;
		private TableLayoutPanel tableLayoutPanel;
		/// <summary>
		/// Dialog box with a list of available 'self-configuring' IEncConverter implementation types
		/// </summary>
		private System.ComponentModel.Container components = null;

		// it would perhaps be more useful to have this class itself determine the list, but that would
		//  require iterating thru the registry again and since we already do that elsewhere, ...
		// (okay, I'm trying to save 3mS)
		public ImplTypeList(ICollection aDisplayNames)
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			// put the display names in a list box for the user to choose.
			foreach(string strDisplayName in aDisplayNames)
			{
				this.listBoxImplTypes.Items.Add(strDisplayName);
			}

			// disable the add button (until an implementation type is selected)
			this.buttonAdd.Enabled = false;
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ImplTypeList));
			this.listBoxImplTypes = new System.Windows.Forms.ListBox();
			this.labelStatic = new System.Windows.Forms.Label();
			this.buttonAdd = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.helpProvider = new System.Windows.Forms.HelpProvider();
			this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.tableLayoutPanel.SuspendLayout();
			this.SuspendLayout();
			//
			// listBoxImplTypes
			//
			this.listBoxImplTypes.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.tableLayoutPanel.SetColumnSpan(this.listBoxImplTypes, 2);
			this.helpProvider.SetHelpString(this.listBoxImplTypes, "This list displays all of the available transduction engine types currently insta" +
					"lled");
			this.listBoxImplTypes.Location = new System.Drawing.Point(3, 26);
			this.listBoxImplTypes.Name = "listBoxImplTypes";
			this.helpProvider.SetShowHelp(this.listBoxImplTypes, true);
			this.listBoxImplTypes.Size = new System.Drawing.Size(262, 225);
			this.listBoxImplTypes.Sorted = true;
			this.listBoxImplTypes.TabIndex = 0;
			this.listBoxImplTypes.SelectedIndexChanged += new System.EventHandler(this.listBoxImplTypes_SelectedIndexChanged);
			this.listBoxImplTypes.DoubleClick += new System.EventHandler(this.listBoxImplTypes_DoubleClick);
			//
			// labelStatic
			//
			this.tableLayoutPanel.SetColumnSpan(this.labelStatic, 2);
			this.labelStatic.Location = new System.Drawing.Point(3, 0);
			this.labelStatic.Name = "labelStatic";
			this.labelStatic.Size = new System.Drawing.Size(256, 23);
			this.labelStatic.TabIndex = 1;
			this.labelStatic.Text = "Select an implementation type and click Add:";
			//
			// buttonAdd
			//
			this.buttonAdd.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.helpProvider.SetHelpString(this.buttonAdd, "Click this button to add an existing map or create a new converter based on the s" +
					"elected transduction type");
			this.buttonAdd.Location = new System.Drawing.Point(56, 257);
			this.buttonAdd.Name = "buttonAdd";
			this.helpProvider.SetShowHelp(this.buttonAdd, true);
			this.buttonAdd.Size = new System.Drawing.Size(75, 23);
			this.buttonAdd.TabIndex = 2;
			this.buttonAdd.Text = "&Add";
			this.buttonAdd.Click += new System.EventHandler(this.buttonAdd_Click);
			//
			// buttonCancel
			//
			this.buttonCancel.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.helpProvider.SetHelpString(this.buttonCancel, "Click this button to cancel this dialog");
			this.buttonCancel.Location = new System.Drawing.Point(137, 257);
			this.buttonCancel.Name = "buttonCancel";
			this.helpProvider.SetShowHelp(this.buttonCancel, true);
			this.buttonCancel.Size = new System.Drawing.Size(75, 23);
			this.buttonCancel.TabIndex = 3;
			this.buttonCancel.Text = "&Cancel";
			this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
			//
			// tableLayoutPanel
			//
			this.tableLayoutPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.tableLayoutPanel.ColumnCount = 2;
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.tableLayoutPanel.Controls.Add(this.labelStatic, 0, 0);
			this.tableLayoutPanel.Controls.Add(this.buttonCancel, 1, 2);
			this.tableLayoutPanel.Controls.Add(this.listBoxImplTypes, 0, 1);
			this.tableLayoutPanel.Controls.Add(this.buttonAdd, 0, 2);
			this.tableLayoutPanel.Location = new System.Drawing.Point(12, 12);
			this.tableLayoutPanel.Name = "tableLayoutPanel";
			this.tableLayoutPanel.RowCount = 3;
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel.Size = new System.Drawing.Size(268, 283);
			this.tableLayoutPanel.TabIndex = 4;
			//
			// ImplTypeList
			//
			this.AcceptButton = this.buttonAdd;
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.CancelButton = this.buttonCancel;
			this.ClientSize = new System.Drawing.Size(292, 307);
			this.Controls.Add(this.tableLayoutPanel);
			this.HelpButton = true;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(270, 133);
			this.Name = "ImplTypeList";
			this.Text = "Choose a Transduction Engine";
			this.tableLayoutPanel.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		private string	m_strDisplayNameChoosen;

		public string	SelectedDisplayName
		{
			get	{ return m_strDisplayNameChoosen; }
		}

		private void buttonAdd_Click(object sender, System.EventArgs e)
		{
			Debug.Assert(this.listBoxImplTypes.SelectedItem != null);
			m_strDisplayNameChoosen = (string)this.listBoxImplTypes.SelectedItem;
			this.DialogResult = DialogResult.OK;
			this.Close();
		}

		private void listBoxImplTypes_SelectedIndexChanged(object sender, EventArgs e)
		{
			this.buttonAdd.Enabled = (this.listBoxImplTypes.SelectedItem != null);
		}

		private void buttonCancel_Click(object sender, System.EventArgs e)
		{
			m_strDisplayNameChoosen = null;
			this.DialogResult = DialogResult.Cancel;
			this.Close();
		}

		private void listBoxImplTypes_DoubleClick(object sender, EventArgs e)
		{
			if (this.listBoxImplTypes.SelectedIndex >= 0)
			{
				m_strDisplayNameChoosen = (string)this.listBoxImplTypes.SelectedItem;
				this.DialogResult = DialogResult.OK;
				this.Close();
			}
		}
	}
}
