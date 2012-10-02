using System;
using System.Drawing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Diagnostics;
using System.Xml;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.LangProj;
using SIL.Utils;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Summary description for AddCustomFieldDlg.
	/// </summary>
	public class AddCustomFieldDlg : Form, IFWDisposable
	{
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ComboBox cbCreateIn;
		private System.Windows.Forms.Label labelCustomFieldsList;
		private System.Windows.Forms.Button buttonAdd;
		private System.Windows.Forms.Button buttonDelete;
		private System.Windows.Forms.RichTextBox rtbDescription;
		private System.Windows.Forms.ComboBox cbWritingSystem;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.Label lblName;
		private System.Windows.Forms.Label lblDescription;
		private System.Windows.Forms.Label lblWritingSystems;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;


		// variables for managing the dlg
		private XCore.Mediator m_mediator = null;	// local mediator

		Inventory m_layouts;
		private Dictionary<int, ModifiedLabel> m_dictModLabels = new Dictionary<int, ModifiedLabel>();

		private FDWrapper m_fdwCurrentField = null;

		//FLAGS
		private bool m_fOpeningDialog = true; //when opening the dialog we do not want to take any action
		//for events like TextChange or SelectedIndexChanged on controls

		private bool m_deletingCustomField = false; //use to return from
		//cbWritingSystem_SelectedIndexChanged
		//when user is deleting a Custom field and the index is changed to 0


		private FdoCache m_cache = null;
		private System.Windows.Forms.TextBox CustomFieldName;
		private List<FDWrapper> m_customFields;
		private System.Windows.Forms.Button buttonHelp;	// list of current custom fields [db and mem]

		//private int modifyingIndex = -1;
		private bool m_addingField = false;  //use this to keep track of add operations
		private bool m_modifyingField = false;  //use this to keep track of add operations
		private bool m_listViewItemChanging = false; //track when in listViewCustomFields_ItemSelectionChanged
		private bool m_creatingNewCustomField = false; //track when we are creating a new custom field
		private bool m_NoCustomFields = false; //keep track of when there are no CustomFields yet


		private Control[] m_settingControls; // Array of setting controls

		private const string s_helpTopic = "khtpCustomFields";
		private System.Windows.Forms.HelpProvider helpProvider;

		private ListView listViewCustomFields;
		private ColumnHeader columnHeader1;
		private ColumnHeader columnHeader2;

		// The following define which classes can have custom fields.
		private IdAndString m_entry = new IdAndString(LexEntry.kclsidLexEntry, xWorksStrings.Entry);
		private IdAndString m_sense = new IdAndString(LexSense.kclsidLexSense, xWorksStrings.Sense);
		private IdAndString m_allomorph = new IdAndString(MoForm.kclsidMoForm, xWorksStrings.Allomorph);
		private IdAndString m_exampleSent = new IdAndString(LexExampleSentence.kclsidLexExampleSentence, xWorksStrings.ExampleSentence);

		#region Private methods for hiding and enabling Settings controls


		/// <summary>
		///
		/// </summary>
		/// <param name="index"></param>
		private void SetControlsToSelectedCustomField(FDWrapper fdw)
		{
			CustomFieldName.Text = fdw.Fd.Userlabel;
			rtbDescription.Text = fdw.Fd.HelpString;

			SelectWSfromInt(fdw.Fd.WsSelector);

			SelectLocationfromInt(fdw.Fd.Class);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="index"></param>
		private void SetControlsToCustomFieldValuesInt(int index)
		{
			CustomFieldName.Text = m_customFields[index].Fd.Userlabel;
			rtbDescription.Text = m_customFields[index].Fd.HelpString;

			SelectWSfromInt(m_customFields[index].Fd.WsSelector);

			SelectLocationfromInt(m_customFields[index].Fd.Class);
		}


		private void EnableSettingsControls(bool enable)
		{
			foreach (Control c in m_settingControls)
			{
				if (c == cbWritingSystem || c == cbCreateIn)
					c.Enabled = enable;
				//Custom Field Name and Description should be enabled by default
			}
		}

		private void EnableSettingsControlsAllSame(bool enable)
		{
			foreach (Control c in m_settingControls)
			{
				//if (c == CustomFieldName || c == lblName)
				//    continue;	// always enabled.
				if (c.Enabled != enable)
					c.Enabled = enable;
			}
		}


		#endregion

		public AddCustomFieldDlg(XCore.Mediator mediator, int defaultClid)
		{
			// create member variables
			m_mediator = mediator;
			m_cache = (FDO.FdoCache)m_mediator.PropertyTable.GetValue("cache");
			m_layouts = Inventory.GetInventory("layouts", m_cache.DatabaseName);
			m_customFields = new List<FDWrapper>();

			InitializeComponent();		// form required method

			//lbCustomFields.Hide();
			//listViewCustomFields.Hide(); //for now hide this control until I figure out
									//how to convert everything from lbCustomFields
									//to this control type.

			this.labelCustomFieldsList.Tag = this.labelCustomFieldsList.Text;	// Localizes Tag!

			this.helpProvider = new System.Windows.Forms.HelpProvider();
			this.helpProvider.HelpNamespace = FwApp.App.HelpFile;
			this.helpProvider.SetHelpKeyword(this, FwApp.App.GetHelpString(s_helpTopic, 0));
			this.helpProvider.SetHelpNavigator(this, System.Windows.Forms.HelpNavigator.Topic);
			this.helpProvider.SetShowHelp(this, true);

			// Initialize array of setting controls
			m_settingControls = new Control[8]
						{
							lblName,
							CustomFieldName,
							lblDescription,
							rtbDescription,
							lblWritingSystems,
							cbWritingSystem,
							label1,
							cbCreateIn
						};


			SetReadyToAddOrDelete();    //get the buttons in their initial proper state that
										//we want when we start up this dialog
										// this also calls EnableSettingsControlsNew(false);

			LoadDBCustomFields();			// get custom fields from DB

			// Initialize the Writing Systems combo box.  This must be initialized before setting the
			// selected item in cbCreateIn.
			cbWritingSystem.Items.Add(new IdAndString(LangProject.kwsAnal, xWorksStrings.FirstAnalysisWs));
			cbWritingSystem.Items.Add(new IdAndString(LangProject.kwsVern, xWorksStrings.FirstVernacularWs));
			cbWritingSystem.Items.Add(new IdAndString(LangProject.kwsAnals, xWorksStrings.AllAnalysisWs));
			cbWritingSystem.Items.Add(new IdAndString(LangProject.kwsVerns, xWorksStrings.AllVernacularWs));
			cbWritingSystem.Items.Add(new IdAndString(LangProject.kwsAnalVerns, xWorksStrings.AllAnalysisVernacularWs));
			cbWritingSystem.Items.Add(new IdAndString(LangProject.kwsVernAnals, xWorksStrings.AllVernacularAnalysisWs));
			cbWritingSystem.SelectedIndex = 0;



			// initialize the 'Create in' combo box with the names and class id's
			cbCreateIn.Items.Add(m_entry);
			cbCreateIn.Items.Add(m_sense);
			cbCreateIn.Items.Add(m_exampleSent);
			cbCreateIn.Items.Add(m_allomorph);

			// Set the default item in the class combo box. (cf. LT-4404)
			switch (defaultClid)
			{
				case MoForm.kclsidMoForm:
					cbCreateIn.SelectedItem = m_allomorph;
					break;
				case LexExampleSentence.kclsidLexExampleSentence:
					cbCreateIn.SelectedItem = m_exampleSent;
					break;
				case LexSense.kclsidLexSense:
					cbCreateIn.SelectedItem = m_sense;
					break;
				case LexEntry.kclsidLexEntry:
				default:
					cbCreateIn.SelectedItem = m_entry;
					break;
			}

			UpdateCustomFieldsListView();   //load the items from m_customfields into the ListView

			buttonAdd.Select();

			//if there is at least one existing Custom field then set the controls
			//to the settings it has.
			if (listViewCustomFields.Items.Count > 0)
			{
				m_NoCustomFields = false;
				listViewCustomFields.Items[0].Selected = true;
			}
			else
			//********
			//I need to handle the situation where there are no custom fields in
			//existance yet. After discussion with Susanna we decided to open the dialog
			  //with the CustomFieldName and Description controls disabled.
			{
				SetStateNoCustomFields();
				m_fOpeningDialog = false;
				return;
			}

			//when opening the dialog we do not want to take any action
			//for events like TextChange or SelectedIndexChanged on controls
			//Once the this method is completed we can set this flag to false.
			m_fOpeningDialog = false;

			m_modifyingField = true; //Initially the state is to be modifying
								//the current field
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
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AddCustomFieldDlg));
			this.label1 = new System.Windows.Forms.Label();
			this.cbCreateIn = new System.Windows.Forms.ComboBox();
			this.labelCustomFieldsList = new System.Windows.Forms.Label();
			this.buttonAdd = new System.Windows.Forms.Button();
			this.buttonDelete = new System.Windows.Forms.Button();
			this.lblName = new System.Windows.Forms.Label();
			this.lblDescription = new System.Windows.Forms.Label();
			this.CustomFieldName = new System.Windows.Forms.TextBox();
			this.rtbDescription = new System.Windows.Forms.RichTextBox();
			this.lblWritingSystems = new System.Windows.Forms.Label();
			this.cbWritingSystem = new System.Windows.Forms.ComboBox();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.buttonOK = new System.Windows.Forms.Button();
			this.buttonHelp = new System.Windows.Forms.Button();
			this.listViewCustomFields = new System.Windows.Forms.ListView();
			this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
			this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			//
			// label1
			//
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			//
			// cbCreateIn
			//
			this.cbCreateIn.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.cbCreateIn, "cbCreateIn");
			this.cbCreateIn.Name = "cbCreateIn";
			this.cbCreateIn.SelectedIndexChanged += new System.EventHandler(this.cbCreateIn_SelectedIndexChanged);
			//
			// labelCustomFieldsList
			//
			resources.ApplyResources(this.labelCustomFieldsList, "labelCustomFieldsList");
			this.labelCustomFieldsList.Name = "labelCustomFieldsList";
			this.labelCustomFieldsList.Tag = "&Custom Fields:";
			//
			// buttonAdd
			//
			resources.ApplyResources(this.buttonAdd, "buttonAdd");
			this.buttonAdd.Name = "buttonAdd";
			this.buttonAdd.Click += new System.EventHandler(this.buttonAdd_Click);
			//
			// buttonDelete
			//
			resources.ApplyResources(this.buttonDelete, "buttonDelete");
			this.buttonDelete.Name = "buttonDelete";
			this.buttonDelete.Click += new System.EventHandler(this.buttonDelete_Click);
			//
			// lblName
			//
			resources.ApplyResources(this.lblName, "lblName");
			this.lblName.Name = "lblName";
			//
			// lblDescription
			//
			resources.ApplyResources(this.lblDescription, "lblDescription");
			this.lblDescription.Name = "lblDescription";
			//
			// CustomFieldName
			//
			resources.ApplyResources(this.CustomFieldName, "CustomFieldName");
			this.CustomFieldName.Name = "CustomFieldName";
			this.CustomFieldName.TextChanged += new System.EventHandler(this.CustomFieldName_TextChanged);
			//
			// rtbDescription
			//
			resources.ApplyResources(this.rtbDescription, "rtbDescription");
			this.rtbDescription.Name = "rtbDescription";
			this.rtbDescription.TextChanged += new System.EventHandler(this.rtbDescription_TextChanged);
			//
			// lblWritingSystems
			//
			resources.ApplyResources(this.lblWritingSystems, "lblWritingSystems");
			this.lblWritingSystems.Name = "lblWritingSystems";
			//
			// cbWritingSystem
			//
			this.cbWritingSystem.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.cbWritingSystem, "cbWritingSystem");
			this.cbWritingSystem.Name = "cbWritingSystem";
			this.cbWritingSystem.SelectedIndexChanged += new System.EventHandler(this.cbWritingSystem_SelectedIndexChanged);
			//
			// groupBox1
			//
			this.groupBox1.Controls.Add(this.label1);
			this.groupBox1.Controls.Add(this.lblName);
			this.groupBox1.Controls.Add(this.lblWritingSystems);
			this.groupBox1.Controls.Add(this.lblDescription);
			this.groupBox1.Controls.Add(this.CustomFieldName);
			this.groupBox1.Controls.Add(this.rtbDescription);
			this.groupBox1.Controls.Add(this.cbWritingSystem);
			this.groupBox1.Controls.Add(this.cbCreateIn);
			resources.ApplyResources(this.groupBox1, "groupBox1");
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.TabStop = false;
			//
			// buttonCancel
			//
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(this.buttonCancel, "buttonCancel");
			this.buttonCancel.Name = "buttonCancel";
			//
			// buttonOK
			//
			resources.ApplyResources(this.buttonOK, "buttonOK");
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
			//
			// buttonHelp
			//
			resources.ApplyResources(this.buttonHelp, "buttonHelp");
			this.buttonHelp.Name = "buttonHelp";
			this.buttonHelp.Click += new System.EventHandler(this.buttonHelp_Click);
			//
			// listViewCustomFields
			//
			this.listViewCustomFields.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
			this.columnHeader1,
			this.columnHeader2});
			this.listViewCustomFields.FullRowSelect = true;
			this.listViewCustomFields.HideSelection = false;
			this.listViewCustomFields.Items.AddRange(new System.Windows.Forms.ListViewItem[] {
			((System.Windows.Forms.ListViewItem)(resources.GetObject("listViewCustomFields.Items")))});
			resources.ApplyResources(this.listViewCustomFields, "listViewCustomFields");
			this.listViewCustomFields.MultiSelect = false;
			this.listViewCustomFields.Name = "listViewCustomFields";
			this.listViewCustomFields.UseCompatibleStateImageBehavior = false;
			this.listViewCustomFields.View = System.Windows.Forms.View.Details;
			this.listViewCustomFields.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.listViewCustomFields_ItemSelectionChanged);
			//
			// columnHeader1
			//
			resources.ApplyResources(this.columnHeader1, "columnHeader1");
			//
			// columnHeader2
			//
			resources.ApplyResources(this.columnHeader2, "columnHeader2");
			//
			// AddCustomFieldDlg
			//
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.buttonCancel;
			this.Controls.Add(this.listViewCustomFields);
			this.Controls.Add(this.buttonHelp);
			this.Controls.Add(this.buttonOK);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.labelCustomFieldsList);
			this.Controls.Add(this.buttonDelete);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.buttonAdd);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "AddCustomFieldDlg";
			this.Tag = "";
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		#region Button processing code

		private void buttonDelete_Click(object sender, System.EventArgs e)
		{
			//we need to make sure that a Custom field is actually selected
			//if we are going to allow the user to delete one.
			//Probably we should put up a dialog box telling the user to select one.
			if (listViewCustomFields.SelectedItems.Count == 0)
			{
				MessageBox.Show(this, xWorksStrings.FirstSelectItemToDelete,
					xWorksStrings.SelectCustomField, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			FDWrapper wrapper = listViewCustomFields.SelectedItems[0].Tag as FDWrapper;
			if (wrapper == null)
				return;

			FieldDescription fd = wrapper.Fd;
			if (fd == null)
				return; // probably can't happen.
			if (!fd.IsInstalled)
			{
				// One we just created, clobber it with no fuss.
				m_customFields.Remove((FDWrapper)listViewCustomFields.SelectedItems[0].Tag);

				//Rick Modifications
				m_deletingCustomField = true;
				//listViewCustomFields.Items.RemoveByKey(listViewCustomFields.FocusedItem.Text);
				//listViewCustomFields.Refresh();

				UpdateCustomFieldsListView();

				//if there is at least one existing Custom field then set the controls
				//to the settings it has.
				if (listViewCustomFields.Items.Count > 0)
				{
					listViewCustomFields.Items[0].Selected = true;
				}
				else
				//********
				//I need to handle the situation where there are no more custom fields.
				//After discussion with Susanna we decided to open the dialog
				//with the CustomFieldName and Description controls disabled.
				// Also the delete button is disabled too.
				{
					SetStateNoCustomFields();
				}
				m_deletingCustomField = false;
				//Rick Modifications

				return;
			}
			string userName = CustomFieldName.Text;
			int clsid = (cbCreateIn.SelectedItem as IdAndString).Id;
			string className = m_cache.MetaDataCacheAccessor.GetClassName((uint)clsid);
			string fieldName = fd.Name;
			int count = 0;
			if (fieldName != null && fieldName != "")
			{
				string sql;
				// This is the only non-multi type currently supported.
				if (fd.Type != (int)CellarModuleDefns.kcptString)
					sql = string.Format("select count(*) from {0}_{1} where Txt is not null and Txt != ''",
						className, fieldName);
				else
					sql = string.Format("select count(*) from {0} where {1} is not null and {1} != ''",
						className, fieldName);
				DbOps.ReadOneIntFromCommand(m_cache, sql, null, out count);
			}
			string sUserLabel = fd.Userlabel;
			if (m_dictModLabels.ContainsKey(fd.Id))
				sUserLabel = m_dictModLabels[fd.Id].OldLabel;
			List<XmlNode> xnlLayouts = FindAffectedLayouts(sUserLabel, fd.Name, className);
			string message;
			if (count != 0 && xnlLayouts.Count != 0)
			{
				message = String.Format(xWorksStrings.DeletingFieldCannotBeUndone0Items1Views,
					count, xnlLayouts.Count, userName);
			}
			else if (xnlLayouts.Count != 0)
			{
				message = String.Format(xWorksStrings.DeletingFieldCannotBeUndone0Views,
					xnlLayouts.Count, userName);
			}
			else if (count != 0)
			{
				message = String.Format(xWorksStrings.DeletingFieldCannotBeUndone0Items,
					count, userName);
			}
			else
			{
				message = String.Format(xWorksStrings.DeletingFieldCannotBeUndone,
					userName);
			}
			if (MessageBox.Show(this, message, xWorksStrings.ReallyDeleteField, MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK)
				return;
			fd.MarkForDeletion = true;

			//Rick Modifications
			m_deletingCustomField = true;
			//listViewCustomFields.Items.RemoveByKey(listViewCustomFields.FocusedItem.Text);
			//listViewCustomFields.Refresh();
			UpdateCustomFieldsListView();
			//if there is at least one existing Custom field then set the controls
			//to the settings it has.
			if (listViewCustomFields.Items.Count > 0)
			{
				listViewCustomFields.Items[0].Selected = true;
			}
			else
			//********
			//I need to handle the situation where there are no more custom fields.
			//After discussion with Susanna we decided to open the dialog
			//with the CustomFieldName and Description controls disabled.
			// Also the delete button is disabled too.
			{
				SetStateNoCustomFields();
			}
			m_deletingCustomField = false;
			//Rick Modifications

			if (m_dictModLabels.ContainsKey(fd.Id))
			{
				fd.Userlabel = sUserLabel;		// layout to delete is using the old label.
				m_dictModLabels.Remove(fd.Id);
			}

		}

		/// <summary>
		/// This scans through the list of configured layouts for the given class, and returns a
		/// list of layout names which display the given custom field (as defined by its UserLabel).
		/// </summary>
		/// <param name="sFieldLabel"></param>
		/// <param name="sName"></param>
		/// <param name="sClassName"></param>
		/// <returns></returns>
		private List<XmlNode> FindAffectedLayouts(string sFieldLabel, string sName, string sClassName)
		{
			List<XmlNode> xnlResults = new List<XmlNode>();
			XmlNodeList xnlLayouts = m_layouts.GetElements("layout", new string[] {sClassName});
			foreach (XmlNode xnLayout in xnlLayouts)
			{
				XmlNodeList xnl = xnLayout.SelectNodes("descendant::part[@ref=\"$child\" or @ref=\"Custom\"]");
				foreach (XmlNode xn in xnl)
				{
					string sRef = XmlUtils.GetOptionalAttributeValue(xn, "ref");
					if (sRef == "$child")
					{
						string sLabel = XmlUtils.GetOptionalAttributeValue(xn, "label");
						if (sLabel == sFieldLabel)
						{
							xnlResults.Add(xnLayout);
							break;
						}
					}
					else if (sRef == "Custom")
					{
						string sParam = XmlUtils.GetOptionalAttributeValue(xn, "param");
						if (sParam == sName)
						{
							xnlResults.Add(xnLayout);
							break;
						}
					}
				}
			}

			return xnlResults;
		}

		private void buttonAdd_Click(object sender, System.EventArgs e)
		{
			if (m_NoCustomFields == true)
			{
				m_NoCustomFields = false;
				listViewCustomFields.Enabled = true;
				m_addingField = true;
				buttonDelete.Enabled = true;
			}

			if (m_modifyingField == true || m_addingField == true)
			{
				// create new custom field
				// and add it to the list
				FDWrapper fdw = CreateNewCustomField();

				m_modifyingField = false;
				m_addingField = true;

				CustomFieldName.Focus();  //we want focus on the new CustomFieldName.Text

				//also enable the controls so that the
				//user can change the Location and WritingSystem
				EnableSettingsControlsAllSame(true);

				return;
			}

			//If the user was in the process of adding a new Custom
			//field then we need to complete the saving of it
			if (m_addingField == true)
			{
				//maybe just save the changes
				//CompleteAddition();
			}

			m_addingField = true;
		}



		private void buttonOK_Click(object sender, System.EventArgs e)
		{

			foreach (FDWrapper fdw in m_customFields)
			{
				if (CheckInvalidCustomField(fdw))
					return;
			}

			// save any new or modified custom field(s)
			bool changed = false;
			changed |= AdjustLayoutsForNewLabels();
			changed |= UpdateLayoutsForDeletions();
			changed |= SaveCustomFieldsToDB();
			if (changed)	// only fire the 'big gun' if something has actually changed
				m_mediator.BroadcastMessage("MasterRefresh", null);
			DialogResult = DialogResult.OK;
			this.Close();
		}


		#endregion

		#region Create In control event processing

		private void cbCreateIn_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			//when opening the dialog we do not want to take any action
			if (m_fOpeningDialog == true)
				return;

			//if we are in the process of switching items don't
			//save anyting since we just want to chage the Text in
			//this control
			if (m_listViewItemChanging == true)
			{
				FDWrapper fdw = listViewCustomFields.SelectedItems[0].Tag as FDWrapper;
				SelectLocationfromInt(fdw.Fd.Class);
				return;
			}

			if (m_modifyingField == true)
			{
				//we should never see this happen because when we are modifying
				//a CustomField this control should be disabled
				return;
			}

			//we only want to save the Sense/Entry selection of the user
			//when we are in the process of adding a new Custom Field
			if (m_addingField == false)
				return;

			if (cbCreateIn.SelectedItem == null)
				return;


			if (m_addingField == true)
			{
				// m_fdwCurrentField is the current field
				// I hope this updates it in listViewCustomFields
				// and m_customfields
				int classId = (cbCreateIn.SelectedItem as IdAndString).Id;


				if (m_fdwCurrentField != null)
				{
					m_fdwCurrentField.Fd.Class = classId;
				}

				int selectedIndex = listViewCustomFields.SelectedIndices.Count == 0 ? -1 : listViewCustomFields.SelectedIndices[0];
				UpdateCustomFieldsListView();
				// refreshed custom field list view, so we need to reselect the
				// newly added custom field.
				if (selectedIndex != -1)
					listViewCustomFields.Items[selectedIndex].Selected = true;
			}
		}


		#endregion

		#region Custom Fields Listbox event processing

		private void lbCustomFields_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			//when opening the dialog we do not want to take any action
			if (m_fOpeningDialog == true)
				return;

			//If the user was in the process of adding a new Custom
			//field then we need to complete the saving of it
			if (m_addingField == true)
			{
				//maybe just save the changes and return;
				//CompleteAddition();
				//m_addingField = false;
				//return; do we return at this point or do something else.
			}
		}


		#endregion

		/// <summary>
		/// Use the FieldDescription class to update all of the custom
		/// fields and save it locally in the m_CustomFields arraylist.
		/// </summary>
		/// <returns>number of custom fields</returns>
		private int LoadDBCustomFields()
		{
			m_customFields.Clear();
			FieldDescription.ClearDataAbout(m_cache);
			foreach (FieldDescription fd in FieldDescription.FieldDescriptors(m_cache))
			{
				if (fd.IsCustomField && fd.Class > 4999 && fd.Class < 6000)
					// As per LT-7462, limit displayed fields to ones with classes
					// from module 5 (5000-5999) - GordonM
					m_customFields.Add(new FDWrapper(fd, true));
			}
			return m_customFields.Count;
		}

		/// <summary>
		/// Update the ListView with the custom fields.
		/// </summary>
		/// <returns>number of items in the ListView.</returns>
		private int UpdateCustomFieldsListView()
		{
			listViewCustomFields.SuspendLayout();
			listViewCustomFields.Items.Clear();

			//load all the custom fields into the Custom Fields List
			foreach (FDWrapper fdw in m_customFields)
			{
				//I better leave this in for the case a field was
				//marked for deletion already
				if (!fdw.Fd.MarkForDeletion)
				{
					ListViewItem lvi = new ListViewItem(fdw.Fd.Userlabel);
					lvi.Tag = fdw;
					lvi.SubItems.Add(GetClassName(fdw.Fd.Class));
					listViewCustomFields.Items.Add(lvi);
				}
			}

			listViewCustomFields.ResumeLayout(true);

			return listViewCustomFields.Items.Count;
		}

		private string GetClassName(int clid)
		{
			if (clid == m_entry.Id)
				return m_entry.Name;
			else if (clid == m_sense.Id)
				return m_sense.Name;
			else if (clid == m_allomorph.Id)
				return m_allomorph.Name;
			else if (clid == m_exampleSent.Id)
				return m_exampleSent.Name;
			else
				return "???";	// not valid for custom field
		}

		/// <summary>
		/// Select the correct entry for the passed in writing system value.
		/// </summary>
		/// <param name="ws">ws to select</param>
		private void SelectWSfromInt(int ws)
		{
			foreach(IdAndString ids in cbWritingSystem.Items)
			{
				if (ids.Id == ws)
				{
					cbWritingSystem.SelectedItem = ids;
					break;
				}
			}
		}

		/// <summary>
		/// Select the correct Location for the passed in Entry/Sense system value.
		/// </summary>
		/// <param name="ws">ws to select</param>
		private void SelectLocationfromInt(int location)
		{
			if (location == m_entry.Id)
			{
				cbCreateIn.SelectedItem = m_entry;
			}
			else if (location == m_sense.Id)
			{
				cbCreateIn.SelectedItem = m_sense;
			}
			else if (location == m_allomorph.Id)
			{
				cbCreateIn.SelectedItem = m_allomorph;
			}
			else if (location == m_exampleSent.Id)
			{
				cbCreateIn.SelectedItem = m_exampleSent;
			}
			else
			{
				cbCreateIn.SelectedItem = null;
			}
		}



		private void SwitchDialogtoSelectedCustomField()
		{
			m_listViewItemChanging = true;

			//I think at this point all the changes are already saved
			//so I do not think I need to do anything
			//except I need to say what the current item is which the user just selected
			//m_fdwCurrentField = listViewCustomFields.FocusedItem.Tag as FDWrapper;

			m_fdwCurrentField = listViewCustomFields.SelectedItems[0].Tag as FDWrapper;

			//show the values that are in the item the user just selected
			SetControlsToSelectedCustomField(listViewCustomFields.SelectedItems[0].Tag as FDWrapper);

			EnableSettingsControls(false);//do not allow the user the change
			//the value for writingSystem or Sense/Entry

			//once we have

			m_listViewItemChanging = false;

		}




		/// <summary>
		/// Create a new Custom field and insert it in
		/// m_customFields and listViewCustomFields
		/// </summary>
		/// <returns>true if a field was saved</returns>
		private FDWrapper CreateNewCustomField()
		{
			m_creatingNewCustomField = true;
			FDWrapper fdw = null;

			// create new custom field
			FieldDescription fd = new FieldDescription(m_cache);
			if (fd != null)
			{
				fd.Userlabel = "New Custom Field";
				fd.HelpString = "";
				//set the writting system to whatever was last selected in the control
				if (cbWritingSystem.Enabled || fd.Type == 0)
				{
					fd.WsSelector = (cbWritingSystem.SelectedItem as IdAndString).Id;

					if (fd.WsSelector == LangProject.kwsAnal ||
						fd.WsSelector == LangProject.kwsVern)
						fd.Type = (int)CellarModuleDefns.kcptString;
					else
						fd.Type = (int)CellarModuleDefns.kcptMultiUnicode;
				}
				// set the Entry or Sense of the new custom field to whatever was
				// last selecte in the control.
				fd.Class = (cbCreateIn.SelectedItem as IdAndString).Id;
				fdw = new FDWrapper(fd, false);
			}
			m_customFields.Add(fdw); //add this new Custom Field to the list

			//now we need to add it to the listViewBox.
			listViewCustomFields.SuspendLayout();
			ListViewItem lvi = new ListViewItem(fdw.Fd.Userlabel);
			lvi.Tag = fdw;
			lvi.Selected = true;
			lvi.SubItems.Add(GetClassName(fdw.Fd.Class));
			listViewCustomFields.Items.Add(lvi);
			listViewCustomFields.ResumeLayout(true);

			CustomFieldName.Text = "New Custom Field";
			rtbDescription.Text = "";

			//now this is the current field
			m_fdwCurrentField = fdw;

			m_creatingNewCustomField = false;
			return fdw;
		}




		/// <summary>
		/// Now go through the list of custom fields and allow the data to be updated
		/// in the DB, or added in the case of new fields.  Assign the 'Name' field
		/// before saving, using a 'safe' and uniquie flavor of the 'Userlabel' field.
		/// </summary>
		/// <returns>true if it was successfull</returns>
		private bool SaveCustomFieldsToDB()
		{
			bool didUpdate = false;	// will only be true if one of the fields has been changed
//			bool error = false;
			foreach (FDWrapper fdw in m_customFields)
			{
				try
				{
//					if (fdw.IsFromDb == false)	// just mem copy can set the name field
//					{
//						// TODO : come up with unique algorythm for name field
//						Random randObj = new Random();
//						fdw.Fd.Name = "asdasdfasdf"+randObj.Next().ToString();
//					}
					// If this is a new record, the 'Name' will get created in the
					// FieldDescription UpdateDatabase method.
					if (fdw.Fd.IsDirty)
					{
						fdw.Fd.UpdateDatabase();
						didUpdate = true;
					}
				}
				catch
				{
//					error = true;
				}
			}
			if (didUpdate)
			{
				// Update MDC.
				m_cache.MetaDataCacheAccessor.Reload(m_cache.DatabaseAccessor, true);
				FieldDescription.ClearDataAbout(m_cache);
			}
			return didUpdate;
		}

		/// <summary>
		/// If any configured layouts use a deleted custom field, remove all references to the
		/// deleted custom field.  Otherwise, bad things happen (LT-5781).
		/// </summary>
		private bool UpdateLayoutsForDeletions()
		{
			bool didUpdate = false;
			foreach (FDWrapper fdw in m_customFields)
			{
				try
				{
					FieldDescription fd = fdw.Fd;
					if (fd.IsCustomField && fd.MarkForDeletion)
					{
						string className = m_cache.MetaDataCacheAccessor.GetClassName((uint)fd.Class);
						List<XmlNode> xnlLayouts = FindAffectedLayouts(fd.Userlabel, fd.Name, className);
						foreach (XmlNode xnLayout in xnlLayouts)
						{
							DeleteMatchingDescendants(xnLayout, fd);
							m_layouts.PersistOverrideElement(xnLayout);
							didUpdate = true;
						}
					}
				}
				catch
				{
				}
			}
			return didUpdate;
		}

		private void DeleteMatchingDescendants(XmlNode xnLayout, FieldDescription fd)
		{
			List<XmlNode> rgxn = new List<XmlNode>();

			foreach (XmlNode xn in xnLayout.ChildNodes)
			{
				string sRef = XmlUtils.GetOptionalAttributeValue(xn, "ref");
				if (sRef == "$child")
				{
					string sLabel = XmlUtils.GetOptionalAttributeValue(xn, "label");
					if (sLabel == fd.Userlabel)
						rgxn.Add(xn);
					else
						DeleteMatchingDescendants(xn, fd);		// recurse!
				}
				else if (sRef == "Custom")
				{
					string sParam = XmlUtils.GetOptionalAttributeValue(xn, "param");
					if (sParam == fd.Name)
						rgxn.Add(xn);
				}
			}

			foreach (XmlNode xn in rgxn)
				xnLayout.RemoveChild(xn);
		}




		/// <summary>
		/// Check to see if the user label field is nonempty and unique.  If not show a message box.
		/// </summary>
		/// <returns>true if invalid, false otherwise.</returns>
		private bool CheckInvalidCustomField(FDWrapper fdwToCheck)
	   {
			string Fieldname = fdwToCheck.Fd.Userlabel;
			string FieldName = Fieldname.TrimEnd(); //we don't allow a name of only spaces

			if (FieldName.Length == 0)
			{

				MessageBox.Show(xWorksStrings.FieldNameShouldNotBeEmpty,
						xWorksStrings.EmptyFieldName, System.Windows.Forms.MessageBoxButtons.OK);
			   return true;
			}

			Fieldname = fdwToCheck.Fd.Userlabel;
			foreach (FDWrapper fdw in m_customFields)
				if (fdwToCheck != fdw && fdw.Fd.Userlabel == FieldName
					&& fdwToCheck.Fd.Class == fdw.Fd.Class )
				{
					string sClassName = GetClassName(fdw.Fd.Class);
					string str1 = string.Format(xWorksStrings.AlreadyFieldWithThisLabel, sClassName, FieldName);
					MessageBox.Show(str1,
							xWorksStrings.LabelAlreadyExists, System.Windows.Forms.MessageBoxButtons.OK);
					return true;

				}

				return false;

		}













		private void SaveModifiedLabelIfNeeded(FieldDescription fd)
		{
			string sNewLabel = CustomFieldName.Text;
			if (fd.Userlabel != sNewLabel)
			{
				if (m_dictModLabels.ContainsKey(fd.Id))
				{
					m_dictModLabels[fd.Id].NewLabel = sNewLabel;
				}
				else
				{
					m_dictModLabels.Add(fd.Id, new ModifiedLabel(fd, sNewLabel, m_cache));
				}
			}
		}

		/// <summary>
		/// Find any layout which use a custom field whose label has been modified, and fix it.
		/// </summary>
		private bool AdjustLayoutsForNewLabels()
		{
			bool didUpdate = false;
			foreach (ModifiedLabel mod in m_dictModLabels.Values)
			{
				if (mod.OldLabel != mod.NewLabel)	// maybe the user changed his mind?
				{
					List<XmlNode> xnlLayouts = FindAffectedLayouts(mod.OldLabel, null, mod.ClassName);
					foreach (XmlNode xnLayout in xnlLayouts)
					{
						FixLayoutPartLabels(xnLayout, mod.OldLabel, mod.NewLabel);
						m_layouts.PersistOverrideElement(xnLayout);
						didUpdate = true;
					}
				}
			}
			return didUpdate;
		}

		private void FixLayoutPartLabels(XmlNode xnLayout, string sOldLabel, string sNewLabel)
		{
			foreach (XmlNode xn in xnLayout.ChildNodes)
			{
				if (XmlUtils.GetOptionalAttributeValue(xn, "ref") == "$child")
				{
					for (int i = 0; i < xn.Attributes.Count; ++i)
					{
						XmlAttribute xa = xn.Attributes[i];
						if (xa.Name == "label" && xa.Value == sOldLabel)
						{
							xa.Value = sNewLabel;
							break;
						}
					}
				}
				else
				{
					FixLayoutPartLabels(xn, sOldLabel, sNewLabel);		// recurse!
				}
			}
		}



		/// <summary>
		/// Disables all the buttons related to the custom field settings controls.
		/// </summary>
		private void DisableAll()
		{
			//modifyingIndex = -1;
			buttonAdd.Enabled = false;

			buttonDelete.Enabled = false;
			EnableSettingsControls(false);
		}





		/// <summary>
		/// We want to call this on Startup
		/// Puts the dialog in the state add a new custom field or delete the selected custom field.
		/// </summary>
		private void SetReadyToAddOrDelete()
		{
			m_addingField = false;
			buttonDelete.Enabled = true;
			buttonAdd.Enabled = true;
			EnableSettingsControls(false); // disable controls until user clicks Add.
		}

		/// <summary>
		/// Set the dialog to the proper state if there are no Custom fields displaying
		/// Note there can be some in m_customFields which the user has markedForDeletion
		///
		/// </summary>
		private void SetStateNoCustomFields()
		{
			m_NoCustomFields = true;
			EnableSettingsControlsAllSame(false);
			buttonDelete.Enabled = false;
			buttonAdd.Enabled = true;
			listViewCustomFields.Enabled = false;
			CustomFieldName.Text = "";
			rtbDescription.Text = "";
		}

		private void buttonHelp_Click(object sender, System.EventArgs e)
		{
			ShowHelp.ShowHelpTopic(FwApp.App, s_helpTopic);
		}

		#region Helper classes
		/// <summary>
		/// Local class for storing an Int value and String value together
		/// </summary>
		public class IdAndString
		{
			public IdAndString(int id,  string name )
			{
				m_id = id;
				m_name = name;
			}
			public override string ToString() { return m_name;}
			// read only properties
			public int Id { get { return m_id; } }
			public string Name { get { return m_name; } }
			// private variables
			private int m_id;
			private string m_name;
		}

		/// <summary>
		/// This class is a wrapper class for containing the FieldDescription
		/// and the source of it : mem or DB.  This class is added to the LB
		/// of custom fields.
		/// </summary>
		public class FDWrapper
		{
			public FDWrapper(FieldDescription fd, bool db)
			{
				m_fd = fd;
				m_fromDB = db;
			}
			public override string ToString()
			{
				return m_fd.Userlabel == null ? "" : m_fd.Userlabel;
			}
			// read only properties
			public FieldDescription Fd { get { return m_fd; } }
			public bool IsFromDb { get { return m_fromDB;} }
			// private variables
			private bool m_fromDB;
			private FieldDescription m_fd;
		}

		/// <summary>
		/// This class saves a relationship between old and new UserLabel values for a custom
		/// field.
		/// </summary>
		public class ModifiedLabel
		{
			private string m_sOldLabel;
			private string m_sNewLabel;
			private string m_sClass;

			public ModifiedLabel(FieldDescription fd, string sNewLabel, FdoCache cache)
			{
				m_sOldLabel = fd.Userlabel;
				m_sNewLabel = sNewLabel;
				m_sClass = cache.MetaDataCacheAccessor.GetClassName((uint)fd.Class);
			}
			/// <summary>
			/// Get the class for the custom field.
			/// </summary>
			public string ClassName
			{
				get { return m_sClass; }
			}
			/// <summary>
			/// Get the old label for the custom field.
			/// </summary>
			public string OldLabel
			{
				get { return m_sOldLabel; }
			}
			/// <summary>
			/// Get or set the new label for the custom field.
			/// </summary>
			public string NewLabel
			{
				get { return m_sNewLabel; }
				set { m_sNewLabel = value; }
			}
		}
		#endregion

		private void CustomFieldName_TextChanged(object sender, EventArgs e)
		{
			//when opening the dialog we do not want to take any action
			if (m_fOpeningDialog == true)
				return;



			//when no Custom Field exists yet exit this event handler if the text changes
			if (m_NoCustomFields == true)
				return;

			//we are in the process of deleting a Custom field and only want to
			//set the value of this control to another Custom field or and empty string
			if (m_deletingCustomField == true)
				return;

			//if we are in the process of switching items don't
			//save anyting since we just want to chage the Text in
			//this control
			if (m_listViewItemChanging == true)
				return;

			//when we are creating a New Custom Field we need to set the text of this field
			//and when we do that we do not want any other actions happening
			if (m_creatingNewCustomField == true)
				return;

			if (m_modifyingField == true)
			{
				//let's save the changes as we go along
				SaveModifiedLabelIfNeeded(m_fdwCurrentField.Fd);
				m_fdwCurrentField.Fd.Userlabel = CustomFieldName.Text;
				if (listViewCustomFields.SelectedItems.Count == 0)
				//then the user has not yet selected any items in the listView
				//therefore we assume the item that is being edited is the first item
				{
					listViewCustomFields.Items[0].Text = CustomFieldName.Text;
				}
				else //we can reset the text item of the item that is focus
				{
					listViewCustomFields.SelectedItems[0].Text = CustomFieldName.Text;
				}
				return;
			}

			if (m_addingField == true)
			{
				//let's save the changes as we go along
				m_fdwCurrentField.Fd.Userlabel = CustomFieldName.Text;

				//I am assuming from what I have seen that there will not be any
				//FocusedItem and that the new Custom field is the last Item
				listViewCustomFields.Items[listViewCustomFields.Items.Count-1].Text = CustomFieldName.Text;

				return;
			}

		}


		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void rtbDescription_TextChanged(object sender, EventArgs e)
		{
			if (rtbDescription.Text.Length > 100)
			{
				string message1 = String.Format("The description is limited to 100 characters.");
				MessageBox.Show(this, message1, "Limit on Description", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				rtbDescription.Text = rtbDescription.Text.Substring(0, 100);
			}
			//when opening the dialog we do not want to take any action
			if (m_fOpeningDialog == true)
				return;

			//when no Custom Field exists yet exit this event handler if the text changes
			if (m_NoCustomFields == true)
				return;


			//we are in the process of deleting a Custom field and only want to
			//set the value of this control to another Custom field or and empty string
			if (m_deletingCustomField == true)
				return;

			//if we are in the process of switching items don't
			//save anyting since we just want to chage the Text in
			//this control
			if (m_listViewItemChanging == true)
				return;

			//when we are creating a New Custom Field we need to set the text of this field
			//and when we do that we do not want any other actions happening
			if (m_creatingNewCustomField == true)
				return;

			if (m_modifyingField == true)
			{
				//let's save the changes as we go along
				m_fdwCurrentField.Fd.HelpString = rtbDescription.Text;
				return;
			}

			if (m_addingField == true)
			{
				//let's save the changes as we go along
				m_fdwCurrentField.Fd.HelpString = rtbDescription.Text;
				return;
			}

			if (m_addingField == false)
				return;


		}




		private void cbWritingSystem_SelectedIndexChanged(object sender, EventArgs e)
		{
			//when opening the dialog we do not want to take any action
			if (m_fOpeningDialog == true)
				return;

			//if we are in the process of switching items don't
			//save anything but the Writing System needs to
			//be set to the one of the ListView.Item the user just selected
			if (m_listViewItemChanging == true)
			{
				FDWrapper fdw = listViewCustomFields.SelectedItems[0].Tag as FDWrapper;
				SelectWSfromInt(fdw.Fd.WsSelector);
				return;
			}


			if (m_modifyingField == true)
			{
				//we should never see this happen because when we are modifying
				//a CustomField this control should be disabled
				return;
			}


			//we only want to save the Writing System selection of the user
			//when we are in the process of adding a new Custom Field
			if (m_addingField == false)
				return;

			if (m_addingField == true)
			{
				// m_fdwCurrentField is the current field
				// I hope this updates it in listViewCustomFields
				// and m_customfields
				m_fdwCurrentField.Fd.WsSelector = (cbWritingSystem.SelectedItem as IdAndString).Id;

				if (m_fdwCurrentField.Fd.WsSelector == LangProject.kwsAnal ||
							m_fdwCurrentField.Fd.WsSelector == LangProject.kwsVern)
					m_fdwCurrentField.Fd.Type = (int)CellarModuleDefns.kcptString;
				else
					m_fdwCurrentField.Fd.Type = (int)CellarModuleDefns.kcptMultiUnicode;
			}
		}



		private void listViewCustomFields_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
		{

			if (listViewCustomFields.SelectedItems.Count == 0)
				//we need to exit when this is called when it really should not be called yet
				//for some reason when the user clicks on another item it sometimes does not
				//immediately put that item in focus.
				return;

			//when no Custom Field exists yet exit this event handler until the user
			//creates a new Custom Field
			if (m_NoCustomFields == true)
				return;

			//when opening the dialog we do not want to take any action
			if (m_fOpeningDialog == true)
				return;

			if (m_fdwCurrentField == listViewCustomFields.SelectedItems[0].Tag)
				return;

			if (m_addingField == true)
			{

				//assume all changes have been saved to the current item
				//change the state to Modifying
				m_addingField = false;
				m_modifyingField = true;

				SwitchDialogtoSelectedCustomField();

				return;
			}

			if (m_modifyingField == true)
			{
				SwitchDialogtoSelectedCustomField();

				return;
			}

		}



	}
}

/*
 *
 *

		public  string LongName
		{
			//TODO: make this "override" when we settle on a name for this kind of thing and make this on CmObject
			get
			{
				string pfx = "";
				IOleDbCommand odc = null;
				m_cache.DatabaseAccessor.CreateCommand(out odc);
				try
				{
					string sqlQuery = String.Format(
						"exec DisplayName_MSA \'<root><Obj Id=\"{0}\"/></root>\', 1", m_hvo);
					uint cbSpaceTaken;
					bool fMoreRows;
					bool fIsNull;
					Array rgchUsername = Array.CreateInstance(typeof(Char), 4000);
					using (ArrayPtr prgchUsername = MarshalEx.ArrayToNative(rgchUsername))
					{
						odc.ExecCommand(sqlQuery,
							(int)SqlStmtType.knSqlStmtStoredProcedure);
						odc.GetRowset(0);
						odc.NextRow(out fMoreRows);
						Debug.Assert(fMoreRows,
							"ID doesn't appear to be for a MoMorphSynAnalysis.");
						odc.GetColValue(3, prgchUsername, 4000, out cbSpaceTaken, out fIsNull, 0);
						byte[] rgbTemp = (byte[])MarshalEx.NativeToArray(prgchUsername, (int)cbSpaceTaken, typeof(byte));
						pfx = Encoding.Unicode.GetString(rgbTemp);
					}
				}
				finally
				{
					DbOps.ShutdownODC(ref odc);
				}
				return pfx;
			}
		}



 *
// ----------------------------------------------------------------------------------------------
	Strips all non alpha-numeric chars from the field name then checks to see if the name
	is used already in the Db.  If it is then a number is addeed to the end of the name.

	@param stu User name of field
	@param stuDbName Out Fixed name to be used for Db Field name
	@return true
// ----------------------------------------------------------------------------------------------
bool TlsOptDlg::FixDbName(StrUni stu, StrUni & stuDbName)
{
	// Strip all not alpha numeric chars to form name for Field$ table (stuDbName).
	wchar rgch[500];
	wchar * pch = rgch;
	stuDbName = stu;
	stuDbName.ToLowerInvariant();
	int cchDbN = stuDbName.Length();
	wchar ch;
	for (int ich = 0; ich < cchDbN; ++ich)
	{
		ch = stuDbName[ich];
		// We assume only ASCII.
		if (ch >= 'a' && ch <= 'z' || ch >= 0 && ch <= 9)
			*pch++ = ch;
	}
	*pch = '\0';
	stuDbName = rgch;

	// Check to make sure stuDbName is not already used, if it is the add a number to it.
	IOleDbCommandPtr qodc;
	ComBool fMoreRows;
	ComBool fIsNull;
	ULONG cbSpaceTaken;
	StrUni stuQuery;
	int ncnt = 0;
	StrUni stuName = stuDbName;
	int nFound;
	do
	{
		Vector<TlsObject> & vcdi = CustDefInVec();
		stuQuery.Format(L"if exists(select * from Field$ where "
			L"Name = '%s' and class in (%d", stuDbName.Chars(),vcdi[0].m_clsid);
		if (vcdi.Size()>1)
		{
			for (int iv = 1; iv < vcdi.Size(); ++iv)
			{
				stuQuery.FormatAppend(L",%d",vcdi[iv].m_clsid);
			}
		}
		stuQuery.Append(L")) (select 1) else (select 0)");

		AfLpInfo * plpi = m_qrmw->GetLpInfo();
		AssertPtr(plpi);
		AfDbInfoPtr qdbi = plpi->GetDbInfo();
		AssertPtr(qdbi);
		IOleDbEncapPtr qode;
		qdbi->GetDbAccess(&qode);

		CheckHr(qode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));

		CheckHr(qodc->GetColValue(1, reinterpret_cast<ULONG *>(&nFound),
			isizeof(nFound), &cbSpaceTaken, &fIsNull, 0));
		if (nFound)
		{
			// The field name is already in the list: make a name with a number appended.
			ncnt ++;
			stuDbName = stuName;
			stuDbName.FormatAppend(L"%d", ncnt);
		}
	} while (nFound);
	return true;
}

ALTER PROCEDURE GenCustomName
	@nvcName NVARCHAR(100) OUTPUT
AS BEGIN

	DECLARE
		@nCount INT,
		@nRowcount INT,
		@nWhatever INT

	SET @nvcName = SUBSTRING(@nvcName, 1, 120)
	SET @nCount = 1
	SELECT @nWhatever = [Id] FROM Field$ WHERE [Name] = @nvcName + CAST(@nCount AS VARCHAR(3))

	SET @nRowcount = @@ROWCOUNT
	WHILE @nRowcount != 0 BEGIN
		SET @nCount = @nCount + 1
		SELECT @nWhatever = [Id] FROM Field$ WHERE [Name] = @nvcName + CAST(@nCount AS VARCHAR(3))
		SET @nRowcount = @@ROWCOUNT
	END

	PRINT @nvcName + CAST(@nCount AS VARCHAR(3))
	--SET @nvcName = @nvcName + CAST(@nCount AS VARCHAR(3))
	SET @nvcName = @nvcName + CONVERT(VARCHAR(3), @nCount)
	RETURN @nvcName
END

declare @nvcName NVARCHAR(100)
set @nvcName = 'DateCreated'
exec GenCustomName @nvcName
print @nvcName



*/
