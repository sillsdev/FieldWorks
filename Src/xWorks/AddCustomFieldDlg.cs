using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using System.IO;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;
using SIL.FieldWorks.FDO;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Summary description for AddCustomFieldDlg.
	/// </summary>
	public class AddCustomFieldDlg : Form, IFWDisposable
	{
		public enum LocationType
		{
			Lexicon,
			Notebook
		}

		private enum CustomFieldType
		{
			SingleLineText,
			MultiparagraphText,
			Number,
			Date,
			ListRefAtomic,
			ListRefCollection
		}

		private Label m_locationLabel;
		private ComboBox m_locationComboBox;
		private Label m_fieldsLabel;
		private Button m_addButton;
		private Button m_deleteButton;
		private RichTextBox m_descTextBox;
		private ComboBox m_wsComboBox;
		private GroupBox m_groupBox1;
		private Button m_cancelButton;
		private Button m_okButton;
		private Label m_nameLabel;
		private Label m_descLabel;
		private Label m_wsLabel;

		// variables for managing the dlg
		private readonly Mediator m_mediator;	// local mediator

		private readonly Inventory m_layouts;
		private readonly Dictionary<int, ModifiedLabel> m_dictModLabels = new Dictionary<int, ModifiedLabel>();

		private FDWrapper m_fdwCurrentField;

		private readonly FdoCache m_cache;
		private TextBox m_nameTextBox;
		private readonly List<FDWrapper> m_customFields;
		private Button m_helpButton;	// list of current custom fields [db and mem]

		private const string s_helpTopic = "khtpCustomFields";
		private readonly HelpProvider m_helpProvider;

		private ListView m_fieldsListView;
		private ColumnHeader columnHeader1;
		private ColumnHeader columnHeader2;
		private ColumnHeader columnHeader3;

		private Label m_typeLabel;
		private ComboBox m_typeComboBox;
		private ComboBox m_listComboBox;
		private Label m_listLabel;

		/// <summary>
		/// Provide access (via reflection) to this dialog for use by the
		/// Data Notebook standard format importer.
		/// </summary>
		public static void ShowNotebookCustomFieldDlg(Mediator mediator)
		{
			using (var dlg = new AddCustomFieldDlg(mediator, LocationType.Notebook))
			{
				dlg.ShowDialog();
			}
		}

		public AddCustomFieldDlg(Mediator mediator, LocationType locationType)
		{
			// create member variables
			m_mediator = mediator;
			m_cache = (FdoCache) m_mediator.PropertyTable.GetValue("cache");
			m_layouts = Inventory.GetInventory("layouts", m_cache.ProjectId.Name);

			InitializeComponent();		// form required method
			AccessibleName = GetType().Name;

			m_fieldsLabel.Tag = m_fieldsLabel.Text;	// Localizes Tag!

			m_helpProvider = new HelpProvider { HelpNamespace = mediator.HelpTopicProvider.HelpFile };
			m_helpProvider.SetHelpKeyword(this, mediator.HelpTopicProvider.GetHelpString(s_helpTopic));
			m_helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
			m_helpProvider.SetShowHelp(this, true);

			// initialize the 'Create in' combo box with the names and class id's
			switch (locationType)
			{
				case LocationType.Lexicon:
					m_locationComboBox.Items.Add(new IdAndString<int>(LexEntryTags.kClassId, xWorksStrings.Entry));
					m_locationComboBox.Items.Add(new IdAndString<int>(LexSenseTags.kClassId, xWorksStrings.Sense));
					m_locationComboBox.Items.Add(new IdAndString<int>(LexExampleSentenceTags.kClassId, xWorksStrings.ExampleSentence));
					m_locationComboBox.Items.Add(new IdAndString<int>(MoFormTags.kClassId, xWorksStrings.Allomorph));
					break;

				case LocationType.Notebook:
					m_locationComboBox.Items.Add(new IdAndString<int>(RnGenericRecTags.kClassId, xWorksStrings.ksRecord));
					break;
			}
			m_locationComboBox.SelectedIndex = 0;

			// get the custom fields
			FieldDescription.ClearDataAbout();
			m_customFields = (from fd in FieldDescription.FieldDescriptors(m_cache)
							  where fd.IsCustomField && GetItem(m_locationComboBox, fd.Class) != null
							  select new FDWrapper(fd, false)).ToList();

			PopulateWritingSystemsList();

			m_typeComboBox.Items.Add(new IdAndString<CustomFieldType>(CustomFieldType.SingleLineText, xWorksStrings.ksSingleLineText));
			m_typeComboBox.Items.Add(new IdAndString<CustomFieldType>(CustomFieldType.MultiparagraphText, xWorksStrings.kMultiparagraphText));
			m_typeComboBox.Items.Add(new IdAndString<CustomFieldType>(CustomFieldType.ListRefCollection, xWorksStrings.ksListRefCollection));
			m_typeComboBox.Items.Add(new IdAndString<CustomFieldType>(CustomFieldType.ListRefAtomic, xWorksStrings.ksListRefAtomic));
			m_typeComboBox.Items.Add(new IdAndString<CustomFieldType>(CustomFieldType.Date, xWorksStrings.ksDate));
			m_typeComboBox.Items.Add(new IdAndString<CustomFieldType>(CustomFieldType.Number, xWorksStrings.ksNumber));
			m_typeComboBox.SelectedIndex = 0;

			m_listComboBox.Items.AddRange(GetListsComboItems(m_cache, (XmlNode)m_mediator.PropertyTable.GetValue("WindowConfiguration")).ToArray());

			m_listComboBox.SelectedIndex = 0;

			UpdateCustomFieldsListView();   //load the items from m_customfields into the ListView

			//if there is at least one existing Custom field then set the controls
			//to the settings it has.
			if (m_fieldsListView.Items.Count > 0)
			{
				SetControlsForField((FDWrapper) m_fieldsListView.Items[0].Tag);
				m_fieldsListView.Items[0].Selected = true;
			}
			else
			//********
			//I need to handle the situation where there are no custom fields in
			//existance yet. After discussion with Susanna we decided to open the dialog
			  //with the CustomFieldName and Description controls disabled.
			{
				SetStateNoCustomFields();
			}
			m_addButton.Select();
		}
		/// <summary>
		/// This method will populate the WritingSystemsList based off of the selection in the Type ComboBox.
		/// </summary>
		private void PopulateWritingSystemsList()
		{
			// Initialize the Writing Systems combo box.  This must be initialized before setting the
			// selected item in cbCreateIn.
			m_wsComboBox.Items.Clear();
			m_wsComboBox.Items.Add(new IdAndString<int>(WritingSystemServices.kwsAnal, xWorksStrings.FirstAnalysisWs));
			m_wsComboBox.Items.Add(new IdAndString<int>(WritingSystemServices.kwsVern, xWorksStrings.FirstVernacularWs));
			if (m_typeComboBox.SelectedItem != null
				&& ((IdAndString<CustomFieldType>)m_typeComboBox.SelectedItem).Id == CustomFieldType.SingleLineText)
			{
				m_wsComboBox.Items.Add(new IdAndString<int>(WritingSystemServices.kwsAnals, xWorksStrings.AllAnalysisWs));
				m_wsComboBox.Items.Add(new IdAndString<int>(WritingSystemServices.kwsVerns, xWorksStrings.AllVernacularWs));
				m_wsComboBox.Items.Add(new IdAndString<int>(WritingSystemServices.kwsAnalVerns,
															xWorksStrings.AllAnalysisVernacularWs));
				m_wsComboBox.Items.Add(new IdAndString<int>(WritingSystemServices.kwsVernAnals,
															xWorksStrings.AllVernacularAnalysisWs));
			}
			m_wsComboBox.SelectedIndex = 0;
		}

		public static List<IdAndString<Guid>> GetListsComboItems(FdoCache cache, XmlNode windowConfiguration)
		{
			var result = new List<IdAndString<Guid>>();
			var clerks = new Dictionary<string, XmlNode>();
			foreach (
				XmlNode elt in
					windowConfiguration.SelectNodes("//item[@value='lists' or @value='grammar']/parameters/clerks/clerk"))
			{
				clerks[elt.Attributes["id"].Value] = elt;
			}
			// Key is a list we found, value is an index in result.
			var resultsByList = new Dictionary<ICmObject, int>();
			var isBTEVersion = Directory.Exists(DirectoryFinder.TeFolder);
			foreach (
				XmlNode elt in
					windowConfiguration.SelectNodes("//item[@value='lists' or @value='grammar']/parameters/tools/tool"))
			{
				var clerkNode = elt.SelectSingleNode("control/parameters//control/parameters[@clerk]");
				if (clerkNode == null)
					continue;
				var clerkId = clerkNode.Attributes["clerk"].Value;
				XmlNode clerk;
				if (!clerks.TryGetValue(clerkId, out clerk))
					continue;
				var recordList = clerk.SelectSingleNode("recordList");
				if (recordList == null)
					continue;
				var owner = recordList.Attributes["owner"].Value;
				var property = recordList.Attributes["property"].Value;
				var list = PossibilityRecordList.GetListFromOwnerAndProperty(cache, owner, property);
				if (list == null || !(list is ICmPossibilityList))
					continue; // some tools in the lists area are not actually lists.
				int oldIndex;
				if (resultsByList.TryGetValue(list, out oldIndex))
				{
					result[oldIndex] = new IdAndString<Guid>(list.Guid, list.ChooserNameTS.Text);
				}
				else
				{
					resultsByList[list] = result.Count;
					var label = elt.Attributes["label"].Value;
					if (isBTEVersion || label != "Scripture Note Categories")
						result.Add(new IdAndString<Guid>(list.Guid, label));
				}
			}
			result.Sort((x, y) => x.Name.CompareTo(y.Name));
			return result;
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
			}
			base.Dispose( disposing );
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
			var xnlResults = new List<XmlNode>();
			XmlNodeList xnlLayouts = m_layouts.GetElements("layout", new[] {sClassName});
			foreach (XmlNode xnLayout in xnlLayouts)
			{
				XmlNodeList xnl = xnLayout.SelectNodes("descendant::part[@ref=\"$child\" or @ref=\"Custom\"]");
				if (xnl != null)
				{
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
			}

			return xnlResults;
		}

		/// <summary>
		/// Update the ListView with the custom fields.
		/// </summary>
		/// <returns>number of items in the ListView.</returns>
		private void UpdateCustomFieldsListView()
		{
			m_fieldsListView.BeginUpdate();
			m_fieldsListView.Items.Clear();

			//load all the custom fields into the Custom Fields List
			foreach (FDWrapper fdw in m_customFields)
			{
				//I better leave this in for the case a field was
				//marked for deletion already
				if (!fdw.Fd.MarkForDeletion)
				{
					var lvi = new ListViewItem(fdw.Fd.Userlabel) {Tag = fdw};
					lvi.SubItems.Add(GetItem(m_locationComboBox, fdw.Fd.Class).Name);
					lvi.SubItems.Add(GetItem(m_typeComboBox, GetCustomFieldType(fdw.Fd)).Name);
					m_fieldsListView.Items.Add(lvi);
				}
			}

			m_fieldsListView.EndUpdate();
		}

		private static IdAndString<T> GetItem<T>(ComboBox combo, T id)
		{
			return combo.Items.Cast<IdAndString<T>>().FirstOrDefault(item => EqualityComparer<T>.Default.Equals(item.Id, id));
		}

		private ListViewItem CurrentFieldListViewItem
		{
			get
			{
				return m_fieldsListView.Items.Cast<ListViewItem>().FirstOrDefault(item => item.Tag == m_fdwCurrentField);
			}
		}

		private static CustomFieldType GetCustomFieldType(FieldDescription fd)
		{
			switch ((CellarPropertyType)fd.Type)
			{
				case CellarPropertyType.MultiUnicode:
				case CellarPropertyType.String:
					return CustomFieldType.SingleLineText;
				case CellarPropertyType.OwningAtomic:
					return CustomFieldType.MultiparagraphText;
				case CellarPropertyType.GenDate:
					return CustomFieldType.Date;
				case CellarPropertyType.Integer:
					return CustomFieldType.Number;
				case CellarPropertyType.ReferenceAtomic:
					return CustomFieldType.ListRefAtomic;
				case CellarPropertyType.ReferenceCollection:
					return CustomFieldType.ListRefCollection;
				default:
					return CustomFieldType.SingleLineText;
			}
		}

		private void SetFieldType(FieldDescription fd)
		{
			fd.DstCls = 0;
			fd.WsSelector = 0;
			fd.ListRootId = Guid.Empty;
			switch (((IdAndString<CustomFieldType>) m_typeComboBox.SelectedItem).Id)
			{
				case CustomFieldType.SingleLineText:
					//if there is no selected item (for whatever reason) rather than crash just pick the first one in the combo.
					int ws = m_wsComboBox.SelectedItem != null ? ((IdAndString<int>)m_wsComboBox.SelectedItem).Id
															   : ((IdAndString<int>)m_wsComboBox.Items[0]).Id;
					fd.Type = ws == WritingSystemServices.kwsAnal || ws == WritingSystemServices.kwsVern ?
						CellarPropertyType.String : CellarPropertyType.MultiUnicode;
					fd.WsSelector = ws;
					break;

				case CustomFieldType.MultiparagraphText:
					fd.Type = CellarPropertyType.OwningAtomic;
					fd.DstCls = StTextTags.kClassId;
					break;

				case CustomFieldType.Number:
					fd.Type = CellarPropertyType.Integer;
					break;

				case CustomFieldType.Date:
					fd.Type = CellarPropertyType.GenDate;
					break;

				case CustomFieldType.ListRefAtomic:
					fd.Type = CellarPropertyType.ReferenceAtomic;
					fd.DstCls = CmPossibilityTags.kClassId;
					fd.ListRootId = ((IdAndString<Guid>)m_listComboBox.SelectedItem).Id;
					break;

				case CustomFieldType.ListRefCollection:
					fd.Type = CellarPropertyType.ReferenceCollection;
					fd.DstCls = CmPossibilityTags.kClassId;
					fd.ListRootId = ((IdAndString<Guid>)m_listComboBox.SelectedItem).Id;
					break;
			}
		}

		/// <summary>
		/// Create a new Custom field and insert it in
		/// m_customFields and listViewCustomFields
		/// </summary>
		/// <returns>true if a field was saved</returns>
		private void CreateNewCustomField()
		{
			m_fdwCurrentField = null;
			var location = (IdAndString<int>) m_locationComboBox.SelectedItem;
			// create new custom field
			var fd = new FieldDescription(m_cache)
						{
							Userlabel = xWorksStrings.ksNewCustomField,
							HelpString = string.Empty,
							Class = location.Id
						};
			SetFieldType(fd);

			var fdw = new FDWrapper(fd, true);
			m_customFields.Add(fdw); //add this new Custom Field to the list

			//now we need to add it to the listViewBox.
			m_fieldsListView.BeginUpdate();
			var lvi = new ListViewItem(fdw.Fd.Userlabel) {Tag = fdw, Selected = true};
			lvi.SubItems.Add(location.Name);
			var type = (IdAndString<CustomFieldType>) m_typeComboBox.SelectedItem;
			lvi.SubItems.Add(type.Name);
			m_fieldsListView.Items.Add(lvi);
			m_fieldsListView.EndUpdate();

			m_nameTextBox.Text = xWorksStrings.ksNewCustomField;
			m_descTextBox.Text = string.Empty;

			//now this is the current field
			m_fdwCurrentField = fdw;
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

			NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
			{
				foreach (FDWrapper fdw in m_customFields)
				{
					// If this is a new record, the 'Name' will get created in the
					// FieldDescription UpdateCustomField() method.
					if (fdw.Fd.IsDirty)
					{
						fdw.Fd.UpdateCustomField();
						didUpdate = true;
					}
				}
			});
			if (didUpdate)
			{
				FieldDescription.ClearDataAbout();
			}
			return didUpdate;
		}

		/// <summary>
		/// If any configured layouts use a deleted custom field, remove all references to the
		/// deleted custom field.  Otherwise, bad things happen (LT-5781).
		/// Better also explicitly delete any cached references from vectors that point to this field.
		/// (LT-12251)
		/// </summary>
		private bool UpdateCacheAndLayoutsForDeletions()
		{
			var didUpdate = false;

			// Query syntax seemed clearer here somehow.
			var deletedFieldList = from fdw in m_customFields
					   where fdw.Fd.IsCustomField && fdw.Fd.MarkForDeletion
					   select fdw.Fd;
			foreach (var fd in deletedFieldList)
			{
				didUpdate = UpdateLayouts(fd);
				didUpdate |= UpdateCachedObjects(m_cache, fd);
			}
			return didUpdate;
		}

		public static bool UpdateCachedObjects(FdoCache cache, FieldDescription fd)
		{
			// We need to find every instance of a reference from this flid to that custom list and delete it!
			// I can't figure out any other way of ensuring that EnsureCompleteIncomingRefs doesn't try to refer
			// to a non-existent flid at some point.
			var owningListGuid = fd.ListRootId;
			if (owningListGuid == Guid.Empty)
				return false;

			// This is only a problem for fields referencing a custom list
			if (!IsCustomList(cache, owningListGuid))
				return false;
			bool fchanged;
			var type = fd.Type;
			var objRepo = cache.ServiceLocator.GetInstance<ICmObjectRepository>();
			var objClass = fd.Class;
			var flid = fd.Id;
			var ddbf = cache.DomainDataByFlid;

			switch (type)
			{
				case CellarPropertyType.ReferenceSequence: // drop through
				case CellarPropertyType.ReferenceCollection:
					// Handle multiple reference fields
					// Is there a way to do this in LINQ without repeating the get_VecSize call?
					var tupleList = new List<Tuple<int, int>>();
					tupleList.AddRange(
						from obj in objRepo.AllInstances(objClass)
							where ddbf.get_VecSize(obj.Hvo, flid) > 0
							select new Tuple<int, int> (obj.Hvo, ddbf.get_VecSize(obj.Hvo, flid)));

					NonUndoableUnitOfWorkHelper.Do(cache.ActionHandlerAccessor, () =>
					{
						foreach (var partResult in tupleList)
							ddbf.Replace(partResult.Item1, flid, 0, partResult.Item2, null, 0);
					});

					fchanged = tupleList.Any();
					break;
				case CellarPropertyType.ReferenceAtomic:
					// Handle atomic reference fields
					// If there's a value for (Hvo, flid), nullify it!
					var objsWithDataThisFlid = new List<int>();
					objsWithDataThisFlid.AddRange(
						from obj in objRepo.AllInstances(objClass)
							where ddbf.get_ObjectProp(obj.Hvo, flid) > 0
							select obj.Hvo);

					// Delete these references
					NonUndoableUnitOfWorkHelper.Do(cache.ActionHandlerAccessor, () =>
					{
						foreach (var hvo in objsWithDataThisFlid)
							ddbf.SetObjProp(hvo, flid, FdoCache.kNullHvo);
					});

					fchanged = objsWithDataThisFlid.Any();
					break;
				default:
					fchanged = false;
					break;
			}
			return fchanged;
		}

		private static bool IsCustomList(FdoCache cache, Guid owningListGuid)
		{
			// Custom lists are unowned.
			var list = cache.ServiceLocator.GetInstance<ICmPossibilityListRepository>().GetObject(owningListGuid);
			return list.Owner == null;
		}

		private bool UpdateLayouts(FieldDescription fd)
		{
			var className = m_cache.DomainDataByFlid.MetaDataCache.GetClassName(fd.Class);
			var xnlLayouts = FindAffectedLayouts(fd.Userlabel, fd.Name, className);
			foreach (var xnLayout in xnlLayouts)
			{
				DeleteMatchingDescendants(xnLayout, fd);
				m_layouts.PersistOverrideElement(xnLayout);
			}
			return xnlLayouts.Count > 0;
		}

		private static void DeleteMatchingDescendants(XmlNode xnLayout, FieldDescription fd)
		{
			var rgxn = new List<XmlNode>();

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
				else
				{
					DeleteMatchingDescendants(xn, fd);		// recurse!
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
			if (fdwToCheck.Fd.MarkForDeletion)
				return false;

			string fieldName = fdwToCheck.Fd.Userlabel.TrimEnd();
			if (fieldName.Length == 0)
			{
				MessageBox.Show(xWorksStrings.FieldNameShouldNotBeEmpty,
						xWorksStrings.EmptyFieldName, MessageBoxButtons.OK);
				return true;
			}

			foreach (FDWrapper fdw in m_customFields)
			{
				if (CheckForRegularFieldDuplicateName(fdw))
				{
					var sClassName = GetItem(m_locationComboBox, fdw.Fd.Class).Name;
					var str1 = string.Format(xWorksStrings.ksCustomFieldMatchesNonCustomField,
						sClassName, fieldName);
					MessageBox.Show(str1, xWorksStrings.LabelAlreadyExists, MessageBoxButtons.OK);
					m_nameTextBox.Select();  // we want focus on the new CustomFieldName.Text
					return true;
				}
				if (fdwToCheck != fdw && fdw.Fd.Userlabel == fieldName && fdwToCheck.Fd.Class == fdw.Fd.Class)
				{
					string sClassName = GetItem(m_locationComboBox, fdw.Fd.Class).Name;
					string str1 = string.Format(xWorksStrings.AlreadyFieldWithThisLabel, sClassName, fieldName);
					MessageBox.Show(str1, xWorksStrings.LabelAlreadyExists, MessageBoxButtons.OK);
					m_nameTextBox.Text = FindUniqueName(m_customFields, fdwToCheck);
					m_nameTextBox.Select();  // we want focus on the new CustomFieldName.Text
					return true;

				}
			}
			return false;
		}

		private bool CheckForRegularFieldDuplicateName(FDWrapper fdw)
		{
			// return false if Name is unique
			// If it already made it into the mdc we don't need to check again
			// because the Name won't change, even if the Userlabel does.
			if (fdw.Fd.IsInstalled)
				return false;
			// Name actually gets set later to whatever Userlabel is, so test Userlabel.
			try
			{
				var flid = m_cache.MetaDataCacheAccessor.GetFieldId2(fdw.Fd.Class, fdw.Fd.Userlabel, true);
			}
			catch (FDOInvalidFieldException e)
			{
				return false; // this is actually the 'good' case.
			}
			return true;
		}

		private static string FindUniqueName(IEnumerable<FDWrapper> allCustomFields,
			FDWrapper currentFdw)
		{
			// Handles case where user didn't change another default userlabel.
			var result = xWorksStrings.ksNewCustomField;
			var defaultLabel = result;
			var extraId = 0;
			while (!FieldNameIsUnique(result, allCustomFields, currentFdw))
			{
				extraId++;
				result = defaultLabel + extraId;
			}
			return result;
		}

		private static bool FieldNameIsUnique(string result,
			IEnumerable<FDWrapper> allCustomFields, FDWrapper currentFdw)
		{
			return allCustomFields.Where(fdw => fdw != currentFdw).All(
				fdw => fdw.Fd.Userlabel != result);
		}

		private void SaveModifiedLabelIfNeeded(FieldDescription fd)
		{
			string sNewLabel = m_nameTextBox.Text;
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

		private static void FixLayoutPartLabels(XmlNode xnLayout, string sOldLabel, string sNewLabel)
		{
			foreach (XmlNode xn in xnLayout.ChildNodes)
			{
				if (XmlUtils.GetOptionalAttributeValue(xn, "ref") == "$child")
				{
					foreach (XmlAttribute xa in xn.Attributes)
					{
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
		/// Set the dialog to the proper state if there are no Custom fields displaying
		/// Note there can be some in m_customFields which the user has markedForDeletion
		///
		/// </summary>
		private void SetStateNoCustomFields()
		{
			m_fdwCurrentField = null;

			m_deleteButton.Enabled = false;
			m_addButton.Enabled = true;
			m_fieldsListView.Enabled = false;

			m_nameTextBox.Enabled = false;
			m_locationComboBox.Enabled = false;
			m_descTextBox.Enabled = false;
			m_typeComboBox.Enabled = false;
			m_listComboBox.Enabled = false;
			m_wsComboBox.Enabled = false;

			m_nameTextBox.Text = string.Empty;
			m_descTextBox.Text = string.Empty;
		}

		private void EnableTypeControls()
		{
			CustomFieldType type = ((IdAndString<CustomFieldType>) m_typeComboBox.SelectedItem).Id;
			m_listComboBox.Enabled = m_fdwCurrentField.IsNew && (type == CustomFieldType.ListRefAtomic || type == CustomFieldType.ListRefCollection);
			if (m_listComboBox.Enabled && m_listComboBox.SelectedItem == null)
				m_listComboBox.SelectedIndex = 0;
			m_wsComboBox.Enabled = m_fdwCurrentField.IsNew && (type == CustomFieldType.SingleLineText || type == CustomFieldType.MultiparagraphText);
			if (m_wsComboBox.Enabled && m_wsComboBox.SelectedItem == null)
				m_wsComboBox.SelectedIndex = 0;
		}

		private void SetControlsForField(FDWrapper field)
		{
			m_fdwCurrentField = field;

			m_locationComboBox.Enabled = field.IsNew;
			m_typeComboBox.Enabled = field.IsNew;

			m_nameTextBox.Text = m_fdwCurrentField.Fd.Userlabel;
			m_locationComboBox.SelectedItem = GetItem(m_locationComboBox, m_fdwCurrentField.Fd.Class);
			m_descTextBox.Text = m_fdwCurrentField.Fd.HelpString;
			m_typeComboBox.SelectedItem = GetItem(m_typeComboBox, GetCustomFieldType(m_fdwCurrentField.Fd));
			m_listComboBox.SelectedItem = GetItem(m_listComboBox, m_fdwCurrentField.Fd.ListRootId);
			m_wsComboBox.SelectedItem = GetItem(m_wsComboBox, m_fdwCurrentField.Fd.WsSelector);

			EnableTypeControls();
		}

		#region Event handlers

		private void m_okButton_Click(object sender, EventArgs e)
		{
			if (m_customFields.Any(CheckInvalidCustomField))
				return;

			bool changed = false;
			using (new WaitCursor(this))
			{
				// save any new or modified custom field(s)
				changed |= AdjustLayoutsForNewLabels();
				changed |= UpdateCacheAndLayoutsForDeletions();
				changed |= SaveCustomFieldsToDB();
			}
			if (changed)	// only fire the 'big gun' if something has actually changed
				m_mediator.BroadcastMessage("MasterRefresh", null);
			DialogResult = DialogResult.OK;
		}

		private void m_addButton_Click(object sender, EventArgs e)
		{
			// First check that any previously added field has a valid name
			var cfields = m_fieldsListView.Items.Count;
			if (cfields > 0)
			{
				var fdw = (FDWrapper) m_fieldsListView.Items[cfields - 1].Tag;
				if (fdw.IsNew && CheckInvalidCustomField(fdw))
					return;
			}
			m_fieldsListView.Enabled = true;
			m_deleteButton.Enabled = true;

			m_nameTextBox.Enabled = true;
			m_locationComboBox.Enabled = true;
			m_descTextBox.Enabled = true;
			m_typeComboBox.Enabled = true;

			// create new custom field
			// and add it to the list
			CreateNewCustomField();

			EnableTypeControls();
			PopulateWritingSystemsList();
			m_nameTextBox.Select();  //we want focus on the new CustomFieldName.Text
		}

		private void m_deleteButton_Click(object sender, EventArgs e)
		{
			//we need to make sure that a Custom field is actually selected
			//if we are going to allow the user to delete one.
			//Probably we should put up a dialog box telling the user to select one.
			if (m_fieldsListView.SelectedItems.Count == 0)
			{
				MessageBox.Show(this, xWorksStrings.FirstSelectItemToDelete,
					xWorksStrings.SelectCustomField, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			var wrapper = (FDWrapper) m_fieldsListView.SelectedItems[0].Tag;
			FieldDescription fd = wrapper.Fd;
			if (!fd.IsInstalled)
			{
				// One we just created, clobber it with no fuss.
				m_customFields.Remove(wrapper);
			}
			else
			{
				string userName = m_nameTextBox.Text;
				int clsid = ((IdAndString<int>) m_locationComboBox.SelectedItem).Id;
				string className = m_cache.DomainDataByFlid.MetaDataCache.GetClassName(clsid);
				string sUserLabel = fd.Userlabel;
				int count = fd.DataOccurrenceCount;
				if (m_dictModLabels.ContainsKey(fd.Id))
					sUserLabel = m_dictModLabels[fd.Id].OldLabel;
				List<XmlNode> xnlLayouts = FindAffectedLayouts(sUserLabel, fd.Name, className);
				string message;
				if (count != 0 && xnlLayouts.Count != 0)
				{
					message = string.Format(xWorksStrings.DeletingFieldCannotBeUndone0Items1Views,
						count, xnlLayouts.Count, userName);
				}
				else if (xnlLayouts.Count != 0)
				{
					message = string.Format(xWorksStrings.DeletingFieldCannotBeUndone0Views,
						xnlLayouts.Count, userName);
				}
				else if (count != 0)
				{
					message = string.Format(xWorksStrings.DeletingFieldCannotBeUndone0Items,
						count, userName);
				}
				else
				{
					message = string.Format(xWorksStrings.DeletingFieldCannotBeUndone,
						userName);
				}
				if (MessageBox.Show(this, message, xWorksStrings.ReallyDeleteField, MessageBoxButtons.OKCancel,
					MessageBoxIcon.Warning) != DialogResult.OK)
				{
					return;
				}
				fd.MarkForDeletion = true;
				if (m_dictModLabels.ContainsKey(fd.Id))
				{
					fd.Userlabel = sUserLabel;		// layout to delete is using the old label.
					m_dictModLabels.Remove(fd.Id);
				}
			}

			UpdateCustomFieldsListView();
			//if there is at least one existing Custom field then set the controls
			//to the settings it has.  Otherwise, disable the CustomFieldName and
			//Description controls, as well as the Delete button.
			if (m_fieldsListView.Items.Count > 0)
				m_fieldsListView.Items[0].Selected = true;
			else
				SetStateNoCustomFields();
		}

		private void m_fieldsListView_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
		{
			if (m_fieldsListView.SelectedItems.Count > 0)
			{
				var selectedField = (FDWrapper) m_fieldsListView.SelectedItems[0].Tag;
				if (m_fdwCurrentField != null && m_fdwCurrentField != selectedField)
					SetControlsForField(selectedField);
			}
		}

		private void m_nameTextBox_TextChanged(object sender, EventArgs e)
		{
			if (m_fdwCurrentField != null)
			{
				if (!m_fdwCurrentField.IsNew)
					SaveModifiedLabelIfNeeded(m_fdwCurrentField.Fd);

				m_fdwCurrentField.Fd.Userlabel = m_nameTextBox.Text;
				CurrentFieldListViewItem.Text = m_nameTextBox.Text;
			}
		}

		private void m_locationComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (m_fdwCurrentField != null && m_fdwCurrentField.IsNew)
			{
				var classItem = (IdAndString<int>) m_locationComboBox.SelectedItem;
				m_fdwCurrentField.Fd.Class = classItem.Id;
				CurrentFieldListViewItem.SubItems[1].Text = classItem.Name;
			}
		}

		private void m_descTextBox_TextChanged(object sender, EventArgs e)
		{
			if (m_fdwCurrentField != null)
			{
				if (m_descTextBox.Text.Length > 100)
				{
					string message1 = String.Format("The description is limited to 100 characters.");
					MessageBox.Show(this, message1, "Limit on Description", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					m_descTextBox.Text = m_descTextBox.Text.Substring(0, 100);
				}

				//let's save the changes as we go along
				m_fdwCurrentField.Fd.HelpString = m_descTextBox.Text;
			}
		}

		private void m_typeComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (m_fdwCurrentField != null && m_fdwCurrentField.IsNew)
			{
				EnableTypeControls();
				SetFieldType(m_fdwCurrentField.Fd);
				PopulateWritingSystemsList();
				CurrentFieldListViewItem.SubItems[2].Text = ((IdAndString<CustomFieldType>) m_typeComboBox.SelectedItem).Name;
			}
		}

		private void m_listComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (m_fdwCurrentField != null && m_fdwCurrentField.IsNew &&
				m_listComboBox.SelectedItem != null)
			{
				var rootId = ((IdAndString<Guid>)m_listComboBox.SelectedItem).Id;
				ICmPossibilityList list;
				try
				{
					list = m_cache.ServiceLocator.GetObject(rootId) as ICmPossibilityList;
				}
				catch (KeyNotFoundException)
				{
					// Shouldn't happen, but... just being safe.
					// OTOH, what ought to happen if the list doesn't exist?!
					// Delete the offender!
					m_listComboBox.Items.Remove(m_listComboBox.SelectedItem);
					return;
				}
				if (list != null)
				{
					m_fdwCurrentField.Fd.ListRootId = rootId;
					m_fdwCurrentField.Fd.DstCls = list.ItemClsid;
				}
			}
		}

		private void m_wsComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			//we only want to save the Writing System selection of the user
			//when we are in the process of adding a new Custom Field, and when that field
			//has a writing system selector (the ComboBox is enabled).  See FWR-563.
			if (m_fdwCurrentField != null && m_fdwCurrentField.IsNew &&
				m_wsComboBox.SelectedItem != null)
			{
				int ws = ((IdAndString<int>) m_wsComboBox.SelectedItem).Id;
				//If the type is String we may want to change it to MultiUnicode depending on the writing system.
				//however in other cases (e.g. when MultiParagraph is the type and OwningAtomic is the Fd.Type)
				//we should leave this alone.
				if (m_fdwCurrentField.Fd.Type == CellarPropertyType.String)
				{
					m_fdwCurrentField.Fd.Type = (ws == WritingSystemServices.kwsAnal || ws == WritingSystemServices.kwsVern)
													? CellarPropertyType.String
													: CellarPropertyType.MultiUnicode;
				}
				m_fdwCurrentField.Fd.WsSelector = ws;
			}
		}

		private void m_helpButton_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_mediator.HelpTopicProvider, s_helpTopic);
		}

		#endregion

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AddCustomFieldDlg));
			this.m_locationLabel = new System.Windows.Forms.Label();
			this.m_locationComboBox = new System.Windows.Forms.ComboBox();
			this.m_fieldsLabel = new System.Windows.Forms.Label();
			this.m_addButton = new System.Windows.Forms.Button();
			this.m_deleteButton = new System.Windows.Forms.Button();
			this.m_nameLabel = new System.Windows.Forms.Label();
			this.m_descLabel = new System.Windows.Forms.Label();
			this.m_nameTextBox = new System.Windows.Forms.TextBox();
			this.m_descTextBox = new System.Windows.Forms.RichTextBox();
			this.m_wsLabel = new System.Windows.Forms.Label();
			this.m_wsComboBox = new System.Windows.Forms.ComboBox();
			this.m_groupBox1 = new System.Windows.Forms.GroupBox();
			this.m_listLabel = new System.Windows.Forms.Label();
			this.m_listComboBox = new System.Windows.Forms.ComboBox();
			this.m_typeLabel = new System.Windows.Forms.Label();
			this.m_typeComboBox = new System.Windows.Forms.ComboBox();
			this.m_cancelButton = new System.Windows.Forms.Button();
			this.m_okButton = new System.Windows.Forms.Button();
			this.m_helpButton = new System.Windows.Forms.Button();
			this.m_fieldsListView = new System.Windows.Forms.ListView();
			this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
			this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
			this.columnHeader3 = new System.Windows.Forms.ColumnHeader();
			this.m_groupBox1.SuspendLayout();
			this.SuspendLayout();
			//
			// m_locationLabel
			//
			resources.ApplyResources(this.m_locationLabel, "m_locationLabel");
			this.m_locationLabel.Name = "m_locationLabel";
			//
			// m_locationComboBox
			//
			this.m_locationComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.m_locationComboBox, "m_locationComboBox");
			this.m_locationComboBox.Name = "m_locationComboBox";
			this.m_locationComboBox.SelectedIndexChanged += new System.EventHandler(this.m_locationComboBox_SelectedIndexChanged);
			//
			// m_fieldsLabel
			//
			resources.ApplyResources(this.m_fieldsLabel, "m_fieldsLabel");
			this.m_fieldsLabel.Name = "m_fieldsLabel";
			this.m_fieldsLabel.Tag = "&Custom Fields:";
			//
			// m_addButton
			//
			resources.ApplyResources(this.m_addButton, "m_addButton");
			this.m_addButton.Name = "m_addButton";
			this.m_addButton.Click += new System.EventHandler(this.m_addButton_Click);
			//
			// m_deleteButton
			//
			resources.ApplyResources(this.m_deleteButton, "m_deleteButton");
			this.m_deleteButton.Name = "m_deleteButton";
			this.m_deleteButton.Click += new System.EventHandler(this.m_deleteButton_Click);
			//
			// m_nameLabel
			//
			resources.ApplyResources(this.m_nameLabel, "m_nameLabel");
			this.m_nameLabel.Name = "m_nameLabel";
			//
			// m_descLabel
			//
			resources.ApplyResources(this.m_descLabel, "m_descLabel");
			this.m_descLabel.Name = "m_descLabel";
			//
			// m_nameTextBox
			//
			resources.ApplyResources(this.m_nameTextBox, "m_nameTextBox");
			this.m_nameTextBox.Name = "m_nameTextBox";
			this.m_nameTextBox.TextChanged += new System.EventHandler(this.m_nameTextBox_TextChanged);
			//
			// m_descTextBox
			//
			resources.ApplyResources(this.m_descTextBox, "m_descTextBox");
			this.m_descTextBox.Name = "m_descTextBox";
			this.m_descTextBox.TextChanged += new System.EventHandler(this.m_descTextBox_TextChanged);
			//
			// m_wsLabel
			//
			resources.ApplyResources(this.m_wsLabel, "m_wsLabel");
			this.m_wsLabel.Name = "m_wsLabel";
			//
			// m_wsComboBox
			//
			this.m_wsComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.m_wsComboBox, "m_wsComboBox");
			this.m_wsComboBox.Name = "m_wsComboBox";
			this.m_wsComboBox.SelectedIndexChanged += new System.EventHandler(this.m_wsComboBox_SelectedIndexChanged);
			//
			// m_groupBox1
			//
			this.m_groupBox1.Controls.Add(this.m_listLabel);
			this.m_groupBox1.Controls.Add(this.m_listComboBox);
			this.m_groupBox1.Controls.Add(this.m_typeLabel);
			this.m_groupBox1.Controls.Add(this.m_typeComboBox);
			this.m_groupBox1.Controls.Add(this.m_locationLabel);
			this.m_groupBox1.Controls.Add(this.m_nameLabel);
			this.m_groupBox1.Controls.Add(this.m_wsLabel);
			this.m_groupBox1.Controls.Add(this.m_descLabel);
			this.m_groupBox1.Controls.Add(this.m_nameTextBox);
			this.m_groupBox1.Controls.Add(this.m_descTextBox);
			this.m_groupBox1.Controls.Add(this.m_wsComboBox);
			this.m_groupBox1.Controls.Add(this.m_locationComboBox);
			resources.ApplyResources(this.m_groupBox1, "m_groupBox1");
			this.m_groupBox1.Name = "m_groupBox1";
			this.m_groupBox1.TabStop = false;
			//
			// m_listLabel
			//
			resources.ApplyResources(this.m_listLabel, "m_listLabel");
			this.m_listLabel.Name = "m_listLabel";
			//
			// m_listComboBox
			//
			this.m_listComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_listComboBox.FormattingEnabled = true;
			resources.ApplyResources(this.m_listComboBox, "m_listComboBox");
			this.m_listComboBox.Name = "m_listComboBox";
			this.m_listComboBox.SelectedIndexChanged += new System.EventHandler(this.m_listComboBox_SelectedIndexChanged);
			//
			// m_typeLabel
			//
			resources.ApplyResources(this.m_typeLabel, "m_typeLabel");
			this.m_typeLabel.Name = "m_typeLabel";
			//
			// m_typeComboBox
			//
			this.m_typeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_typeComboBox.FormattingEnabled = true;
			resources.ApplyResources(this.m_typeComboBox, "m_typeComboBox");
			this.m_typeComboBox.Name = "m_typeComboBox";
			this.m_typeComboBox.SelectedIndexChanged += new System.EventHandler(this.m_typeComboBox_SelectedIndexChanged);
			//
			// m_cancelButton
			//
			this.m_cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(this.m_cancelButton, "m_cancelButton");
			this.m_cancelButton.Name = "m_cancelButton";
			//
			// m_okButton
			//
			resources.ApplyResources(this.m_okButton, "m_okButton");
			this.m_okButton.Name = "m_okButton";
			this.m_okButton.Click += new System.EventHandler(this.m_okButton_Click);
			//
			// m_helpButton
			//
			resources.ApplyResources(this.m_helpButton, "m_helpButton");
			this.m_helpButton.Name = "m_helpButton";
			this.m_helpButton.Click += new System.EventHandler(this.m_helpButton_Click);
			//
			// m_fieldsListView
			//
			this.m_fieldsListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
			this.columnHeader1,
			this.columnHeader2,
			this.columnHeader3});
			this.m_fieldsListView.FullRowSelect = true;
			this.m_fieldsListView.HideSelection = false;
			this.m_fieldsListView.Items.AddRange(new System.Windows.Forms.ListViewItem[] {
			((System.Windows.Forms.ListViewItem)(resources.GetObject("m_fieldsListView.Items")))});
			resources.ApplyResources(this.m_fieldsListView, "m_fieldsListView");
			this.m_fieldsListView.MultiSelect = false;
			this.m_fieldsListView.Name = "m_fieldsListView";
			this.m_fieldsListView.UseCompatibleStateImageBehavior = false;
			this.m_fieldsListView.View = System.Windows.Forms.View.Details;
			this.m_fieldsListView.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.m_fieldsListView_ItemSelectionChanged);
			//
			// columnHeader1
			//
			resources.ApplyResources(this.columnHeader1, "columnHeader1");
			//
			// columnHeader2
			//
			resources.ApplyResources(this.columnHeader2, "columnHeader2");
			//
			// columnHeader3
			//
			resources.ApplyResources(this.columnHeader3, "columnHeader3");
			//
			// AddCustomFieldDlg
			//
			this.AcceptButton = this.m_okButton;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.m_cancelButton;
			this.Controls.Add(this.m_fieldsListView);
			this.Controls.Add(this.m_helpButton);
			this.Controls.Add(this.m_okButton);
			this.Controls.Add(this.m_cancelButton);
			this.Controls.Add(this.m_fieldsLabel);
			this.Controls.Add(this.m_deleteButton);
			this.Controls.Add(this.m_groupBox1);
			this.Controls.Add(this.m_addButton);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "AddCustomFieldDlg";
			this.m_groupBox1.ResumeLayout(false);
			this.m_groupBox1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		#region Helper classes
		/// <summary>
		/// This class is a wrapper class for containing the FieldDescription
		/// and the source of it : mem or DB.  This class is added to the LB
		/// of custom fields.
		/// </summary>
		private class FDWrapper
		{
			public FDWrapper(FieldDescription fd, bool isNew)
			{
				Fd = fd;
				IsNew = isNew;
			}
			public override string ToString()
			{
				return Fd.Userlabel ?? "";
			}
			// read only properties
			public FieldDescription Fd { get; private set; }
			public bool IsNew { get; private set; }
		}

		/// <summary>
		/// This class saves a relationship between old and new UserLabel values for a custom
		/// field.
		/// </summary>
		private class ModifiedLabel
		{
			public ModifiedLabel(FieldDescription fd, string sNewLabel, FdoCache cache)
			{
				OldLabel = fd.Userlabel;
				NewLabel = sNewLabel;
				ClassName = cache.DomainDataByFlid.MetaDataCache.GetClassName(fd.Class);
			}
			/// <summary>
			/// Get the class for the custom field.
			/// </summary>
			public string ClassName { get; private set; }

			/// <summary>
			/// Get the old label for the custom field.
			/// </summary>
			public string OldLabel { get; private set; }

			/// <summary>
			/// Get or set the new label for the custom field.
			/// </summary>
			public string NewLabel { get; set; }
		}
		#endregion
	}

	/// <summary>
	/// Helper class for storing an Int value and String value together
	/// </summary>
	public class IdAndString<T>
	{
		public IdAndString(T id, string name)
		{
			Id = id;
			Name = name;
		}
		public override string ToString() { return Name; }
		// read only properties
		public T Id { get; private set; }
		public string Name { get; private set; }
	}
}
