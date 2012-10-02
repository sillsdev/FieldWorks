// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2002, SIL International. All Rights Reserved.
// <copyright from='2002' to='2002' company='SIL International'>
//		Copyright (c) 2002, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwUserProperties.cs
// Responsibility: TE Team
//
// <remarks>
// Heavily modified (January-July 2004) by Sarah Doorenbos (sarahdoorenbos@yahoo.com)
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Reflection;
using System.Resources;
using System.Windows.Forms;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Drawing;
using SIL.Utils;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using XCore;

namespace SIL.FieldWorks.FwCoreDlgs
{
	#region FwUserProperties implementation
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for FwUserProperties dialog box.
	/// JohnT: this dialog is only used by a menu command which is not currently configured in XML
	/// to show up anywhere, as far as I can tell. User Properties and logons was never fully
	/// implemented. As part of cleaning up obsolete ported code, I deleted a lot of SQL implementations.
	/// If needed, they can be found in the source tree...go for the version before my checkin on Nov 5 2010.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FwUserProperties : Form, IFWDisposable
	{
		#region FwUserProperties Data Members
		#region Designer Variables

		/// <summary></summary>
		protected System.Windows.Forms.ImageList FaceImages;
		/// <summary></summary>
		protected System.Windows.Forms.TabControl tabControl;
		/// <summary></summary>
		protected System.Windows.Forms.TabPage tabAccount;
		/// <summary></summary>
		protected System.Windows.Forms.TabPage tabFeatures;
		/// <summary></summary>
		protected System.Windows.Forms.TabPage tabDataAccess;
		/// <summary></summary>
		protected System.Windows.Forms.Label lblUserName;
		/// <summary></summary>
		protected System.Windows.Forms.ListView lvwUsers;
		/// <summary></summary>
		protected System.Windows.Forms.TextBox txtPassword;
		/// <summary></summary>
		protected System.Windows.Forms.TextBox txtConfirmPwd;
		/// <summary></summary>
		protected System.Windows.Forms.Label lblConfirmPwd1;
		/// <summary></summary>
		protected System.Windows.Forms.CheckBox optMustChangePwd;
		/// <summary></summary>
		protected System.Windows.Forms.Label lblAvailableFeatures;
		/// <summary></summary>
		protected FwOverrideComboBox cboApplication;
		/// <summary></summary>
		protected System.Windows.Forms.CheckedListBox clbFeatures;
		/// <summary></summary>
		protected System.Windows.Forms.GroupBox grpFeatureDescription;
		/// <summary></summary>
		protected System.Windows.Forms.TextBox txtUserDescription;
		/// <summary></summary>
		protected System.Windows.Forms.TextBox txtFeatureDescription;
		/// <summary></summary>
		protected System.Windows.Forms.Label lblAvailableData;
		/// <summary></summary>
		protected System.Windows.Forms.RadioButton optAllData;
		/// <summary></summary>
		protected System.Windows.Forms.RadioButton optJustThisData;
		/// <summary></summary>
		protected System.Windows.Forms.TreeView tvwDataAccess;
		/// <summary></summary>
		protected System.Windows.Forms.GroupBox grpDataDescription;
		/// <summary></summary>
		protected System.Windows.Forms.TextBox txtDataDescription;
		/// <summary></summary>
		protected System.Windows.Forms.ColumnHeader user;
		/// <summary></summary>
		protected System.Windows.Forms.CheckBox optMaintenanceAccess;
		/// <summary></summary>
		protected System.Windows.Forms.Label lblSelectFeatures;
		private System.ComponentModel.IContainer components;
		#endregion

		#region Other Member Variables
		/// <summary>Index of the tab for user properties account</summary>
		protected const int kAccountTab = 0;
		/// <summary>Index of the tab for user features</summary>
		protected const int kFeaturesTab = 1;
		/// <summary>Index of the tab for user properties account</summary>
		protected const int kDataAccessTab = 2;

		/// <summary></summary>
		protected FdoCache m_Cache;
		private bool m_cacheMadeLocally = false;
		private Feature[] m_Features;
		private int m_userLevel;
		private bool m_hasMaintenance;
		/// <summary></summary>
		protected Button btnDelete;
		private Label lblMaintenanceAccess;
		private Label lblPasswordGroup;
		private Button btnOk;
		private IHelpTopicProvider m_helpTopicProvider;

		#endregion  //end of Other Member Variables

		#endregion  //end of FwUserProperties Data Members

		#region FwUserProperties Construction and disposal
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the FwUserProperties class. For use in Designer.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwUserProperties()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			AccessibleName = GetType().Name;
			// Make these tags localizable.
			lblSelectFeatures.Tag = FwCoreDlgs.ksSelectAvailFeatsForX;
			lblAvailableFeatures.Tag = FwCoreDlgs.ksAvailFeatsForX;
			lblAvailableData.Tag = FwCoreDlgs.ksAvailDataForX;

			// ENHANCE TomB: Remove this line in the future to implement Data Access tab
			tabControl.TabPages.RemoveAt(2);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates and initializes a new instance of the FwUserProperties class. Accepts an
		/// FdoCache that encapsulates a DB connection.
		/// </summary>
		/// <param name="cache">Accessor for data cache and DB connection</param>
		/// <param name="features">A list of application features available to the user</param>
		/// ------------------------------------------------------------------------------------
		public FwUserProperties(FdoCache cache, Feature[] features): this()
		{
			if (cache == null)
				throw new Exception("Null Cache passed toFwUserProperties");
			m_Cache = cache;
			m_Features = features;

			Initialize();
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		///
		/// <param name="disposing">Indicates whether component is being disposed</param>
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
				if (m_cacheMadeLocally && m_Cache != null)
					m_Cache.Dispose();
				//if (visualStyleProvider != null) // No. It is needed in the base dispose call.
				//	visualStyleProvider.Dispose();
				//if (m_helpTopicProvider != null && (m_helpTopicProvider is IDisposable)) // No, since the client provides it.
				//	(m_helpTopicProvider as IDisposable).Dispose();
			}
			m_Cache = null;
			m_Features = null;
			//visualStyleProvider = null;
			m_helpTopicProvider = null;

			base.Dispose( disposing );
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the dialog properties object for dialogs that are created.
		/// </summary>
		/// <param name="helpTopicProvider"></param>
		/// ------------------------------------------------------------------------------------
		public void SetDialogProperties(IHelpTopicProvider helpTopicProvider)
		{
			CheckDisposed();

			m_helpTopicProvider = helpTopicProvider;
		}

		#endregion

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FwUserProperties));
			System.Windows.Forms.Button btnCancel;
			System.Windows.Forms.Label lblConfirmPwd2;
			System.Windows.Forms.Label lblPassword;
			System.Windows.Forms.Label lblDescription;
			System.Windows.Forms.Label lblName;
			System.Windows.Forms.Label lblApplication;
			System.Windows.Forms.Button btnModify;
			System.Windows.Forms.Label lblUsers;
			System.Windows.Forms.Button btnAdd;
			System.Windows.Forms.Button btnHelp;
			this.btnOk = new System.Windows.Forms.Button();
			this.lblMaintenanceAccess = new System.Windows.Forms.Label();
			this.lblPasswordGroup = new System.Windows.Forms.Label();
			this.FaceImages = new System.Windows.Forms.ImageList(this.components);
			this.tabControl = new System.Windows.Forms.TabControl();
			this.tabAccount = new System.Windows.Forms.TabPage();
			this.optMaintenanceAccess = new System.Windows.Forms.CheckBox();
			this.optMustChangePwd = new System.Windows.Forms.CheckBox();
			this.lblConfirmPwd1 = new System.Windows.Forms.Label();
			this.txtConfirmPwd = new System.Windows.Forms.TextBox();
			this.txtPassword = new System.Windows.Forms.TextBox();
			this.txtUserDescription = new System.Windows.Forms.TextBox();
			this.lblUserName = new System.Windows.Forms.Label();
			this.tabFeatures = new System.Windows.Forms.TabPage();
			this.grpFeatureDescription = new System.Windows.Forms.GroupBox();
			this.txtFeatureDescription = new System.Windows.Forms.TextBox();
			this.clbFeatures = new System.Windows.Forms.CheckedListBox();
			this.lblSelectFeatures = new System.Windows.Forms.Label();
			this.cboApplication = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.lblAvailableFeatures = new System.Windows.Forms.Label();
			this.tabDataAccess = new System.Windows.Forms.TabPage();
			this.grpDataDescription = new System.Windows.Forms.GroupBox();
			this.txtDataDescription = new System.Windows.Forms.TextBox();
			this.tvwDataAccess = new System.Windows.Forms.TreeView();
			this.optJustThisData = new System.Windows.Forms.RadioButton();
			this.optAllData = new System.Windows.Forms.RadioButton();
			this.lblAvailableData = new System.Windows.Forms.Label();
			this.lvwUsers = new System.Windows.Forms.ListView();
			this.user = new System.Windows.Forms.ColumnHeader();
			this.btnDelete = new System.Windows.Forms.Button();
			btnCancel = new System.Windows.Forms.Button();
			lblConfirmPwd2 = new System.Windows.Forms.Label();
			lblPassword = new System.Windows.Forms.Label();
			lblDescription = new System.Windows.Forms.Label();
			lblName = new System.Windows.Forms.Label();
			lblApplication = new System.Windows.Forms.Label();
			btnModify = new System.Windows.Forms.Button();
			lblUsers = new System.Windows.Forms.Label();
			btnAdd = new System.Windows.Forms.Button();
			btnHelp = new System.Windows.Forms.Button();
			this.tabControl.SuspendLayout();
			this.tabAccount.SuspendLayout();
			this.tabFeatures.SuspendLayout();
			this.grpFeatureDescription.SuspendLayout();
			this.tabDataAccess.SuspendLayout();
			this.grpDataDescription.SuspendLayout();
			this.SuspendLayout();
			//
			// btnOk
			//
			resources.ApplyResources(this.btnOk, "btnOk");
			this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnOk.Name = "btnOk";
			this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
			//
			// btnCancel
			//
			resources.ApplyResources(btnCancel, "btnCancel");
			btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			btnCancel.Name = "btnCancel";
			//
			// lblMaintenanceAccess
			//
			resources.ApplyResources(this.lblMaintenanceAccess, "lblMaintenanceAccess");
			this.lblMaintenanceAccess.Name = "lblMaintenanceAccess";
			//
			// lblConfirmPwd2
			//
			resources.ApplyResources(lblConfirmPwd2, "lblConfirmPwd2");
			lblConfirmPwd2.Name = "lblConfirmPwd2";
			//
			// lblPassword
			//
			resources.ApplyResources(lblPassword, "lblPassword");
			lblPassword.Name = "lblPassword";
			//
			// lblPasswordGroup
			//
			resources.ApplyResources(this.lblPasswordGroup, "lblPasswordGroup");
			this.lblPasswordGroup.Name = "lblPasswordGroup";
			//
			// lblDescription
			//
			resources.ApplyResources(lblDescription, "lblDescription");
			lblDescription.Name = "lblDescription";
			//
			// lblName
			//
			resources.ApplyResources(lblName, "lblName");
			lblName.Name = "lblName";
			//
			// lblApplication
			//
			resources.ApplyResources(lblApplication, "lblApplication");
			lblApplication.Name = "lblApplication";
			//
			// btnModify
			//
			resources.ApplyResources(btnModify, "btnModify");
			btnModify.Name = "btnModify";
			//
			// lblUsers
			//
			resources.ApplyResources(lblUsers, "lblUsers");
			lblUsers.Name = "lblUsers";
			//
			// btnAdd
			//
			resources.ApplyResources(btnAdd, "btnAdd");
			btnAdd.Name = "btnAdd";
			btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
			//
			// btnHelp
			//
			resources.ApplyResources(btnHelp, "btnHelp");
			btnHelp.Name = "btnHelp";
			btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
			//
			// FaceImages
			//
			this.FaceImages.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("FaceImages.ImageStream")));
			this.FaceImages.TransparentColor = System.Drawing.Color.Transparent;
			this.FaceImages.Images.SetKeyName(0, "");
			//
			// tabControl
			//
			this.tabControl.Controls.Add(this.tabAccount);
			this.tabControl.Controls.Add(this.tabFeatures);
			this.tabControl.Controls.Add(this.tabDataAccess);
			resources.ApplyResources(this.tabControl, "tabControl");
			this.tabControl.Name = "tabControl";
			this.tabControl.SelectedIndex = 0;
			//
			// tabAccount
			//
			resources.ApplyResources(this.tabAccount, "tabAccount");
			this.tabAccount.Controls.Add(this.optMaintenanceAccess);
			this.tabAccount.Controls.Add(this.lblMaintenanceAccess);
			this.tabAccount.Controls.Add(this.optMustChangePwd);
			this.tabAccount.Controls.Add(this.lblConfirmPwd1);
			this.tabAccount.Controls.Add(this.txtConfirmPwd);
			this.tabAccount.Controls.Add(lblConfirmPwd2);
			this.tabAccount.Controls.Add(this.txtPassword);
			this.tabAccount.Controls.Add(lblPassword);
			this.tabAccount.Controls.Add(this.lblPasswordGroup);
			this.tabAccount.Controls.Add(this.txtUserDescription);
			this.tabAccount.Controls.Add(this.lblUserName);
			this.tabAccount.Controls.Add(lblDescription);
			this.tabAccount.Controls.Add(lblName);
			this.tabAccount.Name = "tabAccount";
			this.tabAccount.Paint += new System.Windows.Forms.PaintEventHandler(this.tabAccount_Paint);
			//
			// optMaintenanceAccess
			//
			resources.ApplyResources(this.optMaintenanceAccess, "optMaintenanceAccess");
			this.optMaintenanceAccess.Name = "optMaintenanceAccess";
			//
			// optMustChangePwd
			//
			resources.ApplyResources(this.optMustChangePwd, "optMustChangePwd");
			this.optMustChangePwd.Name = "optMustChangePwd";
			//
			// lblConfirmPwd1
			//
			resources.ApplyResources(this.lblConfirmPwd1, "lblConfirmPwd1");
			this.lblConfirmPwd1.Name = "lblConfirmPwd1";
			//
			// txtConfirmPwd
			//
			resources.ApplyResources(this.txtConfirmPwd, "txtConfirmPwd");
			this.txtConfirmPwd.Name = "txtConfirmPwd";
			//
			// txtPassword
			//
			resources.ApplyResources(this.txtPassword, "txtPassword");
			this.txtPassword.Name = "txtPassword";
			//
			// txtUserDescription
			//
			this.txtUserDescription.AcceptsReturn = true;
			this.txtUserDescription.AllowDrop = true;
			resources.ApplyResources(this.txtUserDescription, "txtUserDescription");
			this.txtUserDescription.HideSelection = false;
			this.txtUserDescription.Name = "txtUserDescription";
			//
			// lblUserName
			//
			resources.ApplyResources(this.lblUserName, "lblUserName");
			this.lblUserName.Name = "lblUserName";
			this.lblUserName.Tag = "{0}";
			//
			// tabFeatures
			//
			resources.ApplyResources(this.tabFeatures, "tabFeatures");
			this.tabFeatures.Controls.Add(this.grpFeatureDescription);
			this.tabFeatures.Controls.Add(this.clbFeatures);
			this.tabFeatures.Controls.Add(this.lblSelectFeatures);
			this.tabFeatures.Controls.Add(this.cboApplication);
			this.tabFeatures.Controls.Add(lblApplication);
			this.tabFeatures.Controls.Add(this.lblAvailableFeatures);
			this.tabFeatures.Name = "tabFeatures";
			//
			// grpFeatureDescription
			//
			resources.ApplyResources(this.grpFeatureDescription, "grpFeatureDescription");
			this.grpFeatureDescription.Controls.Add(this.txtFeatureDescription);
			this.grpFeatureDescription.Name = "grpFeatureDescription";
			this.grpFeatureDescription.TabStop = false;
			//
			// txtFeatureDescription
			//
			resources.ApplyResources(this.txtFeatureDescription, "txtFeatureDescription");
			this.txtFeatureDescription.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.txtFeatureDescription.Name = "txtFeatureDescription";
			this.txtFeatureDescription.ReadOnly = true;
			this.txtFeatureDescription.TabStop = false;
			//
			// clbFeatures
			//
			resources.ApplyResources(this.clbFeatures, "clbFeatures");
			this.clbFeatures.CheckOnClick = true;
			this.clbFeatures.Name = "clbFeatures";
			//
			// lblSelectFeatures
			//
			resources.ApplyResources(this.lblSelectFeatures, "lblSelectFeatures");
			this.lblSelectFeatures.Name = "lblSelectFeatures";
			this.lblSelectFeatures.Tag = "Select the features to make available for {0}.";
			//
			// cboApplication
			//
			this.cboApplication.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
			this.cboApplication.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.cboApplication, "cboApplication");
			this.cboApplication.Name = "cboApplication";
			//
			// lblAvailableFeatures
			//
			resources.ApplyResources(this.lblAvailableFeatures, "lblAvailableFeatures");
			this.lblAvailableFeatures.Name = "lblAvailableFeatures";
			this.lblAvailableFeatures.Tag = "Available Features for {0}";
			//
			// tabDataAccess
			//
			resources.ApplyResources(this.tabDataAccess, "tabDataAccess");
			this.tabDataAccess.Controls.Add(btnModify);
			this.tabDataAccess.Controls.Add(this.grpDataDescription);
			this.tabDataAccess.Controls.Add(this.tvwDataAccess);
			this.tabDataAccess.Controls.Add(this.optJustThisData);
			this.tabDataAccess.Controls.Add(this.optAllData);
			this.tabDataAccess.Controls.Add(this.lblAvailableData);
			this.tabDataAccess.Name = "tabDataAccess";
			//
			// grpDataDescription
			//
			resources.ApplyResources(this.grpDataDescription, "grpDataDescription");
			this.grpDataDescription.Controls.Add(this.txtDataDescription);
			this.grpDataDescription.Name = "grpDataDescription";
			this.grpDataDescription.TabStop = false;
			//
			// txtDataDescription
			//
			resources.ApplyResources(this.txtDataDescription, "txtDataDescription");
			this.txtDataDescription.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.txtDataDescription.Name = "txtDataDescription";
			this.txtDataDescription.ReadOnly = true;
			this.txtDataDescription.TabStop = false;
			//
			// tvwDataAccess
			//
			resources.ApplyResources(this.tvwDataAccess, "tvwDataAccess");
			this.tvwDataAccess.ItemHeight = 16;
			this.tvwDataAccess.Name = "tvwDataAccess";
			//
			// optJustThisData
			//
			resources.ApplyResources(this.optJustThisData, "optJustThisData");
			this.optJustThisData.Name = "optJustThisData";
			//
			// optAllData
			//
			this.optAllData.Checked = true;
			resources.ApplyResources(this.optAllData, "optAllData");
			this.optAllData.Name = "optAllData";
			this.optAllData.TabStop = true;
			//
			// lblAvailableData
			//
			resources.ApplyResources(this.lblAvailableData, "lblAvailableData");
			this.lblAvailableData.Name = "lblAvailableData";
			this.lblAvailableData.Tag = "Available Data for {0}";
			//
			// lvwUsers
			//
			resources.ApplyResources(this.lvwUsers, "lvwUsers");
			this.lvwUsers.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
			this.user});
			this.lvwUsers.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
			this.lvwUsers.HideSelection = false;
			this.lvwUsers.LabelEdit = true;
			this.lvwUsers.MultiSelect = false;
			this.lvwUsers.Name = "lvwUsers";
			this.lvwUsers.SmallImageList = this.FaceImages;
			this.lvwUsers.Sorting = System.Windows.Forms.SortOrder.Ascending;
			this.lvwUsers.UseCompatibleStateImageBehavior = false;
			this.lvwUsers.View = System.Windows.Forms.View.Details;
			this.lvwUsers.SelectedIndexChanged += new System.EventHandler(this.lvwUsers_SelectedIndexChanged);
			this.lvwUsers.AfterLabelEdit += new System.Windows.Forms.LabelEditEventHandler(this.lvwUsers_AfterLabelEdit);
			this.lvwUsers.Leave += new System.EventHandler(this.lvwUsers_Leave);
			//
			// user
			//
			resources.ApplyResources(this.user, "user");
			//
			// btnDelete
			//
			resources.ApplyResources(this.btnDelete, "btnDelete");
			this.btnDelete.Name = "btnDelete";
			this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
			//
			// FwUserProperties
			//
			this.AcceptButton = this.btnOk;
			resources.ApplyResources(this, "$this");
			this.CancelButton = btnCancel;
			this.Controls.Add(btnHelp);
			this.Controls.Add(lblUsers);
			this.Controls.Add(this.lvwUsers);
			this.Controls.Add(this.btnDelete);
			this.Controls.Add(btnAdd);
			this.Controls.Add(this.tabControl);
			this.Controls.Add(btnCancel);
			this.Controls.Add(this.btnOk);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FwUserProperties";
			this.ShowInTaskbar = false;
			this.tabControl.ResumeLayout(false);
			this.tabAccount.ResumeLayout(false);
			this.tabAccount.PerformLayout();
			this.tabFeatures.ResumeLayout(false);
			this.tabFeatures.PerformLayout();
			this.grpFeatureDescription.ResumeLayout(false);
			this.grpFeatureDescription.PerformLayout();
			this.tabDataAccess.ResumeLayout(false);
			this.tabDataAccess.PerformLayout();
			this.grpDataDescription.ResumeLayout(false);
			this.grpDataDescription.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		#region Painting methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Paint an etched line to separate main controls from OK, Cancel, and Help buttons.
		/// </summary>
		/// <param name="e">Paint Event arguments</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			// Draw the etched, horizontal line separating the OK/Cancel/Help buttons
			LineDrawing.DrawDialogControlSeparator(e.Graphics, ClientRectangle,
				tabControl.Bottom + (btnOk.Top - tabControl.Bottom) / 4);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Paint some etched lines to separate groups of controls on the Account page.
		/// </summary>
		/// <param name="sender">Not used</param>
		/// <param name="e">Paint Event arguments</param>
		/// ------------------------------------------------------------------------------------
		private void tabAccount_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
		{
			// Draw the etched, horizontal line separating the Password controls
			LineDrawing.Draw(e.Graphics, lblPasswordGroup.Right,
				lblPasswordGroup.Top + lblPasswordGroup.Height / 2,
				txtUserDescription.Right - lblPasswordGroup.Right, LineTypes.Etched);

			// Draw the etched, horizontal line separating the Maintenance Access controls
			LineDrawing.Draw(e.Graphics, lblMaintenanceAccess.Right,
				lblMaintenanceAccess.Top + lblMaintenanceAccess.Height / 2,
				txtUserDescription.Right - lblMaintenanceAccess.Right, LineTypes.Etched);
		}
		#endregion

		#region Protected Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the FwUserProperties class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void Initialize()
		{
			//make sure accounts tab is on top
			tabControl.SelectedTab = tabControl.TabPages[0];

			//fill the list view (lvwUsers) with a list of users/configurations,
			//  and select the current user
			PopulateUserList();

			//
			PopulateFeaturesList();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Populates list of users and selects the current login in the list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void PopulateUserList()
		{
			Debug.Assert(m_Cache != null);

			//Load the list of users into lvwUsers.Items
			for (int i = 0; i < m_Cache.LanguageProject.UserAccountsOC.Count; i++)
			{
				//get the user config account from the database:
				//  first get the hvo of the UserConfigAcct (ucac) at the next position
				//  in the collection (UserAccountsOC) of ucac's in the language project,
				//  then use that hvo to create the "live" ucac from the database
				int hvo = m_Cache.LanguageProject.UserAccountsOC.ToHvoArray()[i];
				var ucacTemp = m_Cache.ServiceLocator.GetInstance<IUserConfigAcctRepository>().GetObject(hvo);

				//make a new list view item displaying the login name that corresponds with the
				//ucac we just made:
				//  first get the sid from the ucac, then turn it into a string, then use the
				//  string version of the sid to find the corresponding login in the syslogins
				//  table, then create the new list view item
				//String sSid = ToHexString(ucacTemp.Sid);
				ListViewItem lviTemp = new ListViewItem(GetLoginName(ucacTemp.Sid), 0);

				//attach the ucac as the tag of the new list view item
				lviTemp.Tag = ucacTemp;

				//add the list view item to the list view (lveUsers)
				lvwUsers.Items.Add(lviTemp);
			}

			//TODO:  Select the current user
			//Temporary: select the first user:
			//  irun'm confused...  it's not working...  is it because the form hasn't
			//  been drawn yet?  oh well, it probably doesn't matter much.
			//  at least it selects a user.  (irun think it selects the one that was most recently
			//  added to the db.
			if (lvwUsers.Items.Count > 0)
			{
				lvwUsers.Items[0].Selected = true;
				lvwUsers.Items[0].Focused = true;
				PopulateTabControl(lvwUsers.Items[0], lvwUsers.Items[0].Text);
			}
			else
				LoadEmptyData();

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Populates list of features for the selected application, checks the checkbox beside
		/// the features that are activated for the selected user
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void PopulateFeaturesList()
		{
			//TODO:  implement this function.

			//Temporary:
			clbFeatures.Items.AddRange(m_Features);
			//SIL.FieldWorks.TE.GetAppFeatures();
		}

		private void CreateNewAccounts()
		{
			//Concerning the tag of the list view item:
			//the tag will be null if the account is new
			//the tag will be the string "new" if it was just created (purposly not null for
			//		a brief moment because we don't want to add it to the db if the user hasn't
			//		had a chance to modify the name of the account yet.)
			//the tag will be a UserConfigAcct if the there is already an account associated
			//		with the list view item

			for (int i = 0; i < lvwUsers.Items.Count; i++)
			{
				if (lvwUsers.Items[i].Tag == null)
				{
					//create the new user
					CreateNewLoginAndUcac(lvwUsers.Items[i]);
				}
			}
		}

		private void CreateNewLoginAndUcac(ListViewItem lvi)
		{
			//create a new syslogin and get the sid
			byte[] rgbSid = AddLogin(lvi.Text, "");//TODO: fix the password so it's not "" (?)

			//create a new ucac and fill in its data
			if (rgbSid != null)
			{
				var ucacTemp = m_Cache.ServiceLocator.GetInstance<IUserConfigAcctFactory>().Create();
				m_Cache.LanguageProject.UserAccountsOC.Add(ucacTemp);
				ucacTemp.Sid = rgbSid;
				ucacTemp.UserLevel = m_userLevel;
				ucacTemp.HasMaintenance = m_hasMaintenance;
				lvi.Tag = ucacTemp;
			}
			else
			{
				lvwUsers.Items.Remove(lvi);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a new login to the syslogins table in the master db.
		/// </summary>
		/// <returns>The Sid of the login that was created.</returns>
		/// ------------------------------------------------------------------------------------
		protected byte[] AddLogin(string sLoginName, string sPassword)
		{

			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Transfer data from one login into another login (delete the old one) (irun.e. copy it).
		/// </summary>
		/// <returns>The Sid of the login that was created.</returns>
		/// ------------------------------------------------------------------------------------
		protected byte[] TransferData(string sOldLoginName, string sNewLoginName)
		{
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the Sid of the login.
		/// </summary>
		/// <returns>The Sid of the login.</returns>
		/// ------------------------------------------------------------------------------------
		protected byte[] GetSid(string sLoginName)
		{
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the login name of the sid.
		/// </summary>
		/// <returns>The login name of the sid.</returns>
		/// ------------------------------------------------------------------------------------
		protected string GetLoginName(byte[] rgbSid)
		{
			return null;
		}

		private string ToHexString(byte[] rgByte)
		{
			//convert from byte[] to a string of hex values
			String sResult = "0x";
			for (int i = 0; i < rgByte.GetLength(0); i++)
			{
				string sTemp = rgByte[i].ToString("X");

				//make sure that a leading 0 is not dropped for any of the bytes
				//  (irun.e.  if the byte is 00001000, then the hex value is 08, but the
				//   ToString("X") function only returns "8".  this if statement catches
				//   that and fixes it)
				if (sTemp.Length == 1)
					sTemp = "0" + sTemp;

				sResult += sTemp;
			}
			return sResult;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new login with sNewName and copies all info from old login, then
		/// deletes the old login from the syslogins table.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool RenameLogin(ListViewItem lvi, string sNewName)
		{
			//First, keep track of old login name
			string sOldName = GetLoginName(((IUserConfigAcct)(lvi.Tag)).Sid);

			//Next, I need to create a new login with a new Sid (because you can't have two
			//logins with the same Sid), but the same other info
			byte[] rgbNewSid = TransferData(sOldName, sNewName);
			if (rgbNewSid != null)  //make sure the copy worked.  Sid will be null if it didn't.
			{
				//Next, change the Sid of the Ucac in the tag of lvi to the new Sid
				//before irun can do that, however, the ucac irun'm modifying has to be connected to
				//		the fdo db
				var ucacTemp = lvi.Tag as IUserConfigAcct;
				ucacTemp.Sid = rgbNewSid;
				lvi.Tag = ucacTemp;
				return true;
			}
			return false;  //rename did not work.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deletes the login from the syslogins table in the master db.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool DropLogin(string sLoginName)
		{
			return true;
		}

		private void LoadEmptyData()
		{
			//TODO: fill in the rest of the tab control with empty values
			//  (use this method if there are no user accounts in the list view and you want
			//   the nothing to display)
			string sName = "";
			lblUserName.Text = string.Format("{0}", sName);

			//the Tag of lblAvailableFeatures and lblSelectFeatures is the string with a
			//place holder ("{0}") where the user's name will go.
			lblAvailableFeatures.Text =
				string.Format((string)(lblAvailableFeatures.Tag), sName);
			lblSelectFeatures.Text = string.Format((string)(lblSelectFeatures.Tag), sName);
		}

		private void PopulateTabControl(ListViewItem lvi, string sName)
		{
			//NOTE:  Concerning the tag of the list view item:
			//the tag will be null if the account is new
			//the tag will be the string "new" if it was just created (purposly not null for
			//		a brief moment because we don't want to add it to the db if the user hasn't
			//		had a chance to modify the name of the account yet.)
			//the tag will be a UserConfigAcct if the there is already an account associated
			//		with the list view item  (only when the tag is a ucac will the
			//		 account be in the database--until then, it's just a list view item.)

			//first create the account
			/*if (lvi.Tag == null)
			{
				CreateNewAccounts();
				lvwUsers.Sort();
			}
			else*/ if(lvi.Tag != null && !lvi.Tag.Equals(FwCoreDlgs.ksNew))
			{
				lblUserName.Text = string.Format("{0}", sName);

				//the Tag of lblAvailableFeatures and lblSelectFeatures is the string with a
				//place holder ("{0}") where the user's name will go.
				lblAvailableFeatures.Text =
					string.Format((string)(lblAvailableFeatures.Tag), sName);
				lblSelectFeatures.Text = string.Format((string)(lblSelectFeatures.Tag), sName);
				optMaintenanceAccess.Checked = ((IUserConfigAcct)(lvi.Tag)).HasMaintenance;

				//TODO:  fill in the rest of the data.  Some things don't currently have
				//places to be stored in the database yet, so fields will need to be added for
				//those things (such as the descriptions of features, and the password, etc.
			}
			else
			{
				LoadEmptyData();
			}
		}

		#endregion

		#region Handle Events

		private void btnAdd_Click(object sender, System.EventArgs e)
		{
			//create a new dialog to add a new user
			using (AddNewUserDlg addDlg = new AddNewUserDlg())
			{
				addDlg.SetDialogProperties(m_helpTopicProvider);

				//if the user clicks OK on the dlg to add a new user, then add a new
				//  list view item to the list view to store the values of the new user.
				if (addDlg.ShowDialog() == DialogResult.OK)
				{
					m_userLevel = addDlg.m_UserLevel;
					m_hasMaintenance = addDlg.m_HasMaintenance;
					ResourceManager resources = new ResourceManager(
						"SIL.FieldWorks.FwCoreDlgs.FwCoreDlgs", Assembly.GetExecutingAssembly());

					ListViewItem lviNewUser =
						new ListViewItem(resources.GetString("kstidNewUser"), 0);

					//NOTE:  Concerning the tag of the list view item:
					//the tag will be null if the account is new
					//the tag will be the string "new" if it was just created (purposly not null
					//		for a brief moment because we don't want to add it to the db if the
					//		user hasn't had a chance to modify the name of the account yet.)
					//the tag will be a UserConfigAcct if the there is already an account
					//		associated with the list view item  (only when the tag is a ucac will
					//		the account be in the database--until then, it's just a list view
					//		item.)
					lviNewUser.Tag = FwCoreDlgs.ksNew;
					lvwUsers.Items.Add(lviNewUser);

					//let the user change the name from the default
					lviNewUser.BeginEdit();
					lviNewUser.Tag = null;
				}
			}
		}

		private void lvwUsers_AfterLabelEdit(
			object sender, System.Windows.Forms.LabelEditEventArgs e)
		{
			//if the label is null, it's probably an accident.
			//plus, irun don't think we want null login names...
			if (e.Label == null)
			{
				e.CancelEdit = true;
			}
			else
			{
				ListViewItem lvi = lvwUsers.Items[e.Item];
				bool renameWorks = false;

				//NOTE:  Concerning the tag of the list view item:
				//the tag will be null if the account is new
				//the tag will be the string "new" if it was just created (purposly not null
				//		for a brief moment because we don't want to add it to the db if the
				//		user hasn't had a chance to modify the name of the account yet.)
				//the tag will be a UserConfigAcct if the there is already an account
				//		associated with the list view item  (only when the tag is a ucac will
				//		the account be in the database--until then, it's just a list view
				//		item.)
				if (lvi.Tag == null)
				{
					string sOldText = lvi.Text;
					lvi.Text = e.Label;
					CreateNewAccounts();
					PopulateTabControl(lvi, e.Label);
					lvi.Text = sOldText;
					lvwUsers.Sort();
				}
				else if (lvi.Tag != null && !(lvi.Tag.Equals(FwCoreDlgs.ksNew)))
				{
					//rename the account because the user typed a new name.  this requires
					//dropping the old login in syslogins and adding a new one
					//(keep the same sid)

					string sOldName = GetLoginName(((IUserConfigAcct)(lvi.Tag)).Sid);

					//only do the following if the text is different
					if (!sOldName.Equals(e.Label))
						renameWorks = RenameLogin(lvi, e.Label);
					if (!renameWorks)
						e.CancelEdit = true;
				}

				//when the user changes the configuration name,
				//load that configuration's info into the account and feature tabs
				if (lvwUsers.SelectedItems.Count > 0 && renameWorks)
				{
					PopulateTabControl(lvwUsers.SelectedItems[0], e.Label);
				}

			}//end else
		}

		private void lvwUsers_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			//when the user selects a different configuration,
			//check to see if there are any new accounts in the list view
			CreateNewAccounts();
			lvwUsers.Sort();

			//load the new account's info into the tab control
			if (lvwUsers.SelectedItems.Count > 0)
			{
				PopulateTabControl(lvwUsers.SelectedItems[0], lvwUsers.SelectedItems[0].Text);
			}
			//else, just keep the info from the previously selected configuration
		}

		private void btnDelete_Click(object sender, System.EventArgs e)
		{
			//TODO:  do we only want to allow the delete if the current user
			//has maintenance access?
			//Also, do we want to make sure there's always at least one account?
			//Also, probably don't let the user delete their own account

			if (lvwUsers.SelectedItems.Count > 0)
			{
				ListViewItem lviToDelete = lvwUsers.SelectedItems[0];
				int index = lviToDelete.Index;

				//Make sure they really want to delete the account.
				ResourceManager resources = new ResourceManager(
					"SIL.FieldWorks.FwCoreDlgs.FwCoreDlgs", Assembly.GetExecutingAssembly());
				DialogResult result = MessageBox.Show(
					string.Format(resources.GetString("kstidConfirmDelete"), lviToDelete.Text),
					resources.GetString("kstidDeleteLabel"), MessageBoxButtons.YesNo,
					MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2);

				//if they're sure they want to delete, then drop the login, and if that works,
				//  then remove the ucac from the UserAccountsOC in the language project
				if (result.Equals(DialogResult.Yes))
				{
					//drop the login
					bool dropped =
						DropLogin(GetLoginName(((IUserConfigAcct)(lviToDelete.Tag)).Sid));
					if (dropped)
					{
						//delete ucac from the UserAccountsOC in the language project
						m_Cache.LanguageProject.UserAccountsOC.Remove((IUserConfigAcct)lviToDelete.Tag);
						lvwUsers.Items.RemoveAt(index);

						//select a different config in the list view
						//NOTE:  we already deleted the one they wanted to get rid of, so the
						//count should be one less than it just was.
						if (lvwUsers.Items.Count != 0) //more than one item left
						{
							if (index == lvwUsers.Items.Count) //just deleted the last item
							{
								//select the item before the one that was just deleted
								lvwUsers.Items[index - 1].Selected = true;
								PopulateTabControl(
									lvwUsers.Items[index - 1], lvwUsers.Items[index - 1].Text);
							}
							else //item deleted was somewhere else
							{
								//select the item after the one that was just deleted so
								//it looks like the selection did not change
								lvwUsers.Items[index].Selected = true;
								PopulateTabControl(
									lvwUsers.Items[index], lvwUsers.Items[index].Text);
							}
						}
						else  //we deleted the last one, so no account is selected
							LoadEmptyData();
					}
				}
			}
		}

		private void btnOk_Click(object sender, System.EventArgs e)
		{
			//TODO:  do we want to make sure that any changes aren't actually made until
			//the user clicks ok?  currently the changes are all implemented before they click
			//ok.
		}

		private void lvwUsers_Leave(object sender, System.EventArgs e)
		{
			//when the user clicks somewhere else,
			//check to see if there are any new accounts in the list view
			CreateNewAccounts();
			lvwUsers.Sort();

			if (lvwUsers.SelectedItems.Count > 0)
			{
				PopulateTabControl(lvwUsers.SelectedItems[0], lvwUsers.SelectedItems[0].Text);
			}
			//else keep the info from the previously selected configuration
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Open the help window when the help button is pressed.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void btnHelp_Click(object sender, System.EventArgs e)
		{
			string helpTopicKey = null;

			// select help appropriate to the selected control tab
			switch (tabControl.SelectedIndex)
			{
				case kAccountTab:
					helpTopicKey = "khtpUserProperties_Account";
					break;
				case kFeaturesTab:
					helpTopicKey = "khtpUserProperties_Features";
					break;
				case kDataAccessTab:
					helpTopicKey = "khtpUserProperties_Features";
					break;
			}

			ShowHelp.ShowHelpTopic(m_helpTopicProvider, helpTopicKey);
		}

		#endregion //end of Handle Events

	}
	#endregion
}
