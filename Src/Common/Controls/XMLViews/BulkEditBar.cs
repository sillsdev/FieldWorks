using System;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using System.Linq;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application.ApplicationServices;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Filters;
using SIL.FieldWorks.Common.FwUtils;
using XCore;
using SIL.FieldWorks.FDO.Application;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.RootSites;
using SilEncConverters40;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// Summary description for BulkEditBar2.
	/// </summary>
	public class BulkEditBar : UserControl, IFWDisposable
	{

		private System.Windows.Forms.Label m_operationLabel;
		/// <summary>
		///
		/// </summary>
		protected VSTabControl m_operationsTabControl;
		/// <summary>
		/// the target combo box curently in use.
		/// </summary>
		protected FwOverrideComboBox m_currentTargetCombo;
		/// <summary> indicates that we've either finished seting up or restoring bulk edit bar tab during initialization. </summary>
		internal protected bool m_setupOrRestoredBulkEditBarTab = false;
		private System.Windows.Forms.TabPage m_listChoiceTab;
		private System.Windows.Forms.TabPage m_bulkCopyTab;
		private System.Windows.Forms.TabPage m_clickCopyTab;
		private System.Windows.Forms.TabPage m_transduceTab;
		private System.Windows.Forms.TabPage m_findReplaceTab;
		private System.Windows.Forms.Label label2;
		/// <summary> </summary>
		protected FwOverrideComboBox m_listChoiceTargetCombo;
		private System.Windows.Forms.Label label3;
		/// <summary>
		/// m_listChoiceChangeToCombo is a dummy control which allows the programmer to adjust the position etc
		/// in the VS designer.
		/// m_listChoiceControl  is the actual control which the user interacts with. This control varies based on the
		/// field which is in m_listChoiceTargetCombo
		/// </summary>
		private FwOverrideComboBox m_listChoiceChangeToCombo;
		private Control m_listChoiceControl;
		private System.ComponentModel.IContainer components;

		Mediator m_mediator;
		XmlNode m_configurationNode = null;
		BrowseViewer m_bv;
		/// <summary>
		///
		/// </summary>
		protected BulkEditItem[] m_beItems;
		FdoCache m_cache;
		const int m_colOffset = 1;
		// object selected in browse view and possibly being edited; we track this
		// so we can commit changes to it when the current index changes.
		int m_hvoSelected;

		private System.Windows.Forms.ImageList m_imageList16x14;
		/// <summary>
		///
		/// </summary>
		protected int m_itemIndex = -1;
		/// <summary>
		/// lets clients know when the target combo has changed (eg. so they
		/// can know if the ExpectedListItemsClass should change.)
		/// </summary>
		public event TargetColumnChangedHandler TargetComboSelectedIndexChanged;

		IVwPattern m_pattern; // pattern used for Find tab, contains find pattern
		ITsString m_tssReplace; // text to replace with.
		private System.Windows.Forms.Label m_bulkEditIcon;	// placeholder for button below.
		private System.Windows.Forms.Button m_bulkEditIconButton;
		private System.Windows.Forms.Label m_bulkEditOperationLabel;
		private System.Windows.Forms.Button m_closeButton;
		private System.Windows.Forms.Button m_previewButton;
		private System.Windows.Forms.Button m_ApplyButton;
		private System.Windows.Forms.Button m_helpButton;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.Label label12;
		private System.Windows.Forms.TabPage m_deleteTab;
		private System.Windows.Forms.Label label13;
		private FwOverrideComboBox m_bulkCopySourceCombo;
		private FwOverrideComboBox m_bulkCopyTargetCombo;
		private FwOverrideComboBox m_clickCopyTargetCombo;
		private System.Windows.Forms.Button m_transduceSetupButton;
		private FwOverrideComboBox m_transduceSourceCombo;
		private FwOverrideComboBox m_transduceTargetCombo;
		private FwOverrideComboBox m_transduceProcessorCombo;
		private System.Windows.Forms.Label m_findReplaceSummaryLabel;
		private System.Windows.Forms.Button m_findReplaceSetupButton;
		private FwOverrideComboBox m_findReplaceTargetCombo;
		private FwOverrideComboBox m_deleteWhatCombo;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.RadioButton m_clickCopyWordButton;
		private System.Windows.Forms.RadioButton m_clickCopyReorderButton;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.RadioButton m_clickCopyAppendButton;
		private System.Windows.Forms.RadioButton m_clickCopyOverwriteButton;
		private SIL.FieldWorks.Common.Widgets.FwTextBox m_clickCopySepBox; // index into m_beItems of current item, or -1 if none is active.
		private NonEmptyTargetControl m_bcNonEmptyTargetControl;
		private NonEmptyTargetControl m_trdNonEmptyTargetControl;

		private bool previewOn = false;

		string m_originalApplyText = XMLViewsStrings.ksApply;

		static string[] s_labels = {
									   XMLViewsStrings.ksListChoiceDesc,
									   XMLViewsStrings.ksBulkCopyDesc,
									   XMLViewsStrings.ksClickCopyDesc,
									   XMLViewsStrings.ksProcessDesc,
									   XMLViewsStrings.ksBulkReplaceDesc,
									   XMLViewsStrings.ksDeleteDesc
								   };

		// This set is used in figuring which items to enable when deleting (specifically senses, at present).
		// Contins the Ids of things in ItemsToChange(false).
		Set<int> m_items;

		// These variables are used in computing whether a ClickCopy target should actually
		// be changed.  (This feature is used by Wordform Bulk Edit.)
		string m_sClickEditIf = null;
		bool m_fClickEditIfNot = false;
		MethodInfo m_miClickEditIf = null;
		int m_wsClickEditIf = 0;
		XmlNode m_enableBulkEditTabsNode;

		// These variables are used in computing whether a row can be deleted in Bulk Delete.
		// (This feature is used in Wordform Bulk Edit.)
		string m_sBulkDeleteIfZero = null;
		/// <summary>
		/// the classes of the objects that have rows which are bulkeditable
		/// </summary>
		List<int> m_bulkEditListItemsClasses = new List<int>();
		/// <summary>
		/// the fields that own ghost objects in our browse view.
		/// </summary>
		List<GhostParentHelper> m_bulkEditListItemsGhostFields = new List<GhostParentHelper>();
		private ImageList m_imageList16x16;
		PropertyInfo m_piBulkDeleteIfZero = null;

		//public const int kDoitHeight = 20;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create one
		/// </summary>
		/// <param name="bv">The BrowseViewer that it is part of.</param>
		/// <param name="spec">The parameters element of the BV, containing the
		/// 'columns' elements that specify the BE bar (among other things).</param>
		/// <param name="mediator">The mediator.</param>
		/// <param name="cache">The cache.</param>
		/// ------------------------------------------------------------------------------------
		public BulkEditBar(BrowseViewer bv, XmlNode spec, Mediator mediator, FdoCache cache)
			: this()
		{
			m_mediator = mediator;
			m_bv = bv;
			m_bv.FilterChanged += BrowseViewFilterChanged;
			m_bv.RefreshCompleted += BrowseViewSorterChanged;
			m_cache = cache;
			m_configurationNode = spec;
			// (EricP) we should probably try find someway to get these classes from the RecordClerk/List
			string bulkEditListItemsClassesValue = XmlUtils.GetManditoryAttributeValue(spec, "bulkEditListItemsClasses");
			string[] bulkEditListItemsClasses = bulkEditListItemsClassesValue.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string className in bulkEditListItemsClasses)
			{
				int classId = cache.MetaDataCacheAccessor.GetClassId(className);
				m_bulkEditListItemsClasses.Add((int)classId);
			}
			// get any fields that have ghosts we may want to edit (see also "ghostListField" in columnSpecs)
			string bulkEditListItemsGhostFieldsValue = XmlUtils.GetOptionalAttributeValue(spec, "bulkEditListItemsGhostFields");
			if (!String.IsNullOrEmpty(bulkEditListItemsGhostFieldsValue))
			{
				string[] bulkEditListItemsGhostFields = bulkEditListItemsGhostFieldsValue.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (string classAndField in bulkEditListItemsGhostFields)
					m_bulkEditListItemsGhostFields.Add(GhostParentHelper.Create(m_cache.ServiceLocator, classAndField));
			}
			MakeItems();

			m_sBulkDeleteIfZero = XmlUtils.GetOptionalAttributeValue(spec, "bulkDeleteIfZero");
			this.AccessibilityObject.Name = "BulkEditBar";

			m_listChoiceTargetCombo.SelectedIndexChanged += new EventHandler(m_listChoiceTargetCombo_SelectedIndexChanged);

			// Finish init of the FwTextBox
			m_clickCopySepBox.WritingSystemFactory = m_cache.WritingSystemFactory;
			m_clickCopySepBox.WritingSystemCode = m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle;
			m_clickCopySepBox.Text = " "; // default (maybe should persist?)
			m_clickCopySepBox.GotFocus += new EventHandler(m_clickCopySepBox_GotFocus);

			// Add NonBlankTargetControl as needed.
			m_bcNonEmptyTargetControl = new NonEmptyTargetControl();
			m_bcNonEmptyTargetControl.WritingSystemFactory = m_cache.WritingSystemFactory;
			m_bcNonEmptyTargetControl.Separator = " "; // persist?
			// Set WritingSystemCode when destination field is set
			m_bcNonEmptyTargetControl.Location = new Point(170, 50);
			m_bcNonEmptyTargetControl.Name = "m_bcNonEmptyTargetControl";
			// Its size should be correctly preset.
			// todo: give it a tab stop.
			m_bulkCopyTab.Controls.Add(m_bcNonEmptyTargetControl);
			m_bulkCopyTargetCombo.SelectedIndexChanged += new EventHandler(m_bulkCopyTargetCombo_SelectedIndexChanged);

			// And for the transduce tab...  (Process Tab)
			m_trdNonEmptyTargetControl = new NonEmptyTargetControl();
			m_trdNonEmptyTargetControl.WritingSystemFactory = m_cache.WritingSystemFactory;
			m_trdNonEmptyTargetControl.Separator = " "; // persist?
			// Set WritingSystemCode when destination field is set
			m_trdNonEmptyTargetControl.Location = new Point(170, 50);
			m_trdNonEmptyTargetControl.Name = "m_trdNonEmptyTargetControl";
			// Its size should be correctly preset.
			// todo: give it a tab stop.
			m_transduceTab.Controls.Add(m_trdNonEmptyTargetControl);
			m_transduceTargetCombo.SelectedIndexChanged += new EventHandler(m_transduceTargetCombo_SelectedIndexChanged);
			m_enableBulkEditTabsNode = XmlUtils.FindNode(spec, "enableBulkEditTabs");
			if (m_enableBulkEditTabsNode != null)
			{
				m_bulkCopyTab.Enabled = XmlUtils.GetOptionalBooleanAttributeValue(m_enableBulkEditTabsNode, "enableBEBulkCopy", true);
				m_clickCopyTab.Enabled = XmlUtils.GetOptionalBooleanAttributeValue(m_enableBulkEditTabsNode, "enableBEClickCopy", true);
				m_transduceTab.Enabled = XmlUtils.GetOptionalBooleanAttributeValue(m_enableBulkEditTabsNode, "enableBEProcess", true);
				m_findReplaceTab.Enabled = XmlUtils.GetOptionalBooleanAttributeValue(m_enableBulkEditTabsNode, "enableBEFindReplace", true);
				m_deleteTab.Enabled = XmlUtils.GetOptionalBooleanAttributeValue(m_enableBulkEditTabsNode, "enableBEOther", true);
			}
			else
			{
				m_bulkCopyTab.Enabled =
					m_clickCopyTab.Enabled = m_transduceTab.Enabled =
					m_findReplaceTab.Enabled = m_deleteTab.Enabled = true;
			}

			m_operationsTabControl.SelectedIndexChanged += new EventHandler(m_operationsTabControl_SelectedIndexChanged);
			m_operationsTabControl.Deselecting += new TabControlCancelEventHandler(m_operationsTabControl_Deselecting);

			BulkCopyTabPageSettings.TrySwitchToLastSavedTab(this);
			// events like SelectedIndexChanged do not fire until after initialization,
			// so do it explicitly here now.
			m_operationsTabControl_SelectedIndexChanged(null, new EventArgs());
			m_setupOrRestoredBulkEditBarTab = true;

			m_previewButton.Click += new EventHandler(m_previewButton_Click);
			m_ApplyButton.Click += new EventHandler(m_ApplyButton_Click);
			m_closeButton.Click += new EventHandler(m_closeButton_Click);

		}

		private void BrowseViewSorterChanged(object sender, EventArgs e)
		{
			ResumeRecordListRowChanges();
		}

		void BrowseViewFilterChanged(object sender, FilterChangeEventArgs e)
		{
			ResumeRecordListRowChanges();
		}

		void m_operationsTabControl_Deselecting(object sender, TabControlCancelEventArgs e)
		{
			// try to save the settings from the currently selected tab before switching contexts.
			SaveSettings();
			if (m_operationsTabControl.SelectedTab == m_clickCopyTab)
			{
				// switching from click copy, so commit any pending changes.
				CommitClickChanges(this, EventArgs.Empty);
				SetEditColumn(-1);
			// ClickCopy will setup this up again.
			(m_bv.BrowseView as XmlBrowseView).ClickCopy -= new ClickCopyEventHandler(xbv_ClickCopy);
		}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:BulkEditBar"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public BulkEditBar()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
			this.m_ApplyButton.Text = m_originalApplyText;
			// replace the bulkEditIconButton with just its icon, until the button actually does something.
			m_bulkEditIcon = new Label();
			m_bulkEditIcon.ForeColor = this.m_bulkEditIconButton.ForeColor;
			m_bulkEditIcon.Name = "bulkEditIconLabel";
			m_bulkEditIcon.ImageList = this.m_bulkEditIconButton.ImageList;
			m_bulkEditIcon.ImageIndex = this.m_bulkEditIconButton.ImageIndex;
			m_bulkEditIcon.Size = this.m_bulkEditIconButton.Size;
			m_bulkEditIcon.Location = this.m_bulkEditIconButton.Location;
			m_bulkEditIconButton.Visible = false;
			m_operationsTabControl.SelectedTab.Controls.Remove(m_bulkEditIconButton);
			m_operationsTabControl.SelectedTab.Controls.Add(m_bulkEditIcon);
			m_bulkEditIconButton = null;
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

			if ( disposing )
			{
				if (components != null)
				{
					components.Dispose();
				}
				m_listChoiceTargetCombo.SelectedIndexChanged -= new EventHandler(m_listChoiceTargetCombo_SelectedIndexChanged);
				m_previewButton.Click -= new EventHandler(m_previewButton_Click);
				m_ApplyButton.Click -= new EventHandler(m_ApplyButton_Click);
				m_closeButton.Click -= new EventHandler(m_closeButton_Click);
				m_operationsTabControl.Deselecting -= new TabControlCancelEventHandler(m_operationsTabControl_Deselecting);
				m_operationsTabControl.SelectedIndexChanged -= new EventHandler(m_operationsTabControl_SelectedIndexChanged);
				DisposeBulkEditItems();
			}
			m_beItems = null;
			m_mediator = null;
			m_bv = null;
			m_cache = null;

			base.Dispose( disposing );
		}

		internal static GhostParentHelper GetGhostHelper(IFdoServiceLocator locator, XmlNode colSpec)
		{
			string classDotField = XmlUtils.GetOptionalAttributeValue(colSpec, "ghostListField");
			if (classDotField == null)
				return null;
			return GhostParentHelper.Create(locator, classDotField);
		}

		private void DisposeBulkEditItems()
		{
			if (m_beItems == null)
				return;

			foreach (BulkEditItem bei in m_beItems)
			{
				if (bei != null)
				{
					bei.BulkEditControl.ValueChanged -= new FwSelectionChangedEventHandler(besc_ValueChanged);
					bei.Dispose();
				}
			}
			m_beItems = null;
		}

		/// <summary>
		/// Update when the user reconfigures the set of columns.
		/// </summary>
		public void UpdateColumnList()
		{
			CheckDisposed();

			// ClickCopy.ClickCopyTabPageSettings/SaveSettings()/CommitClickChanges()
			// could possibly change m_hvoSelected when we're not ready, so save current.
			// see comment on LT-4768 below.
			int oldSelected = m_hvoSelected;
			RemoveOldChoiceControl();
			DisposeBulkEditItems();
			MakeItems();

			// One reason to to this is that it ensures that items in the combos have
			// valid column indexes (and are valid items). Also items for new columns
			// may need to be added to the combos.
			// Unfortunately during a Refresh the list of objects may not be valid,
			// which prevents the normal reload of the current object in the click
			// copy tab.
			// Since we aren't actually changing the tab here, it should be safe to
			// let that not get modified. (Thinking of changing this? See LT-4768 and
			// make sure you can Undo a click copy.)
			m_operationsTabControl_SelectedIndexChanged(this, new EventArgs());
			m_hvoSelected = oldSelected;
			ResumeRecordListRowChanges();
		}

		/// <summary>
		///
		/// </summary>
		private void populateListChoiceTargetCombo()
		{
			CheckDisposed();

			m_listChoiceTargetCombo.ClearItems();
			m_listChoiceTargetCombo.Text = "";

			StringTable tbl = null;
			if (m_mediator != null && m_mediator.HasStringTable)
				tbl = m_mediator.StringTbl;

			// Here we figure which columns we can bulk edit.
			int icol = -1; // will increment at start of loop.
			foreach (XmlNode colSpec in m_bv.ColumnSpecs)
			{
				icol++;
				if (m_beItems[icol] != null)
				{
					string label = XmlUtils.GetLocalizedAttributeValue(tbl, colSpec, "label", null);
					if (label == null)
						label = XmlUtils.GetManditoryAttributeValue(colSpec, "label");
					m_listChoiceTargetCombo.Items.Add(new TargetFieldItem(label, icol));
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes the items.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void MakeItems()
		{
			CheckDisposed();

			if (m_beItems != null)
				return; // already made.

			m_beItems = new BulkEditItem[m_bv.ColumnSpecs.Count];

			// Here we figure which columns we can bulk edit.
			int icol = -1; // will increment at start of loop.
			foreach (XmlNode colSpec in m_bv.ColumnSpecs)
			{
				icol++;
				BulkEditItem bei = MakeItem(colSpec);
				if (bei != null)
				{
					m_beItems[icol] = bei;
				}
			}
			m_itemIndex = -1;
		}

		int GetFlidFromClassDotName(XmlNode node, string attrName)
		{
			return BulkEditBar.GetFlidFromClassDotName(m_cache, node, attrName);
		}

		/// <summary>
		/// Given a string that is supposed to identify a particular list, return the flid.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="node"></param>
		/// <param name="attrName"></param>
		/// <returns></returns>
		internal static int GetFlidFromClassDotName(FdoCache cache, XmlNode node, string attrName)
		{
			string descriptor = XmlUtils.GetManditoryAttributeValue(node, attrName);
			return GetFlidFromClassDotName(cache, descriptor);
		}

		private static int GetFlidFromClassDotName(FdoCache cache, string descriptor)
		{
			string[] parts = descriptor.Trim().Split('.');
			if (parts.Length != 2)
				throw new ConfigurationException("atomicFlatListItem field must be class.field");
			try
			{
				int flid = cache.DomainDataByFlid.MetaDataCache.GetFieldId(parts[0], parts[1], true);
				return flid;
			}
			catch (Exception e)
			{
				throw new ConfigurationException("Don't recognize atomicFlatListItem field " + descriptor, e);
			}
		}

		/// <summary>
		/// Extracts the class.property information for a CmPossibilityList from a columnSpec.
		/// </summary>
		/// <param name="colSpec"></param>
		/// <param name="owningClass"></param>
		/// <param name="property"></param>
		internal static void GetListInfo(XmlNode colSpec, out string owningClass, out string property)
		{
			GetPathInfoFromColumnSpec(colSpec, "list", "LangProject", out owningClass, out property);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="node"></param>
		/// <param name="attrName"></param>
		/// <param name="defaultOwningClass">if only the property is specified,
		/// we'll use this as the default class for that property.</param>
		/// <param name="owningClass"></param>
		/// <param name="property"></param>
		private static void GetPathInfoFromColumnSpec(XmlNode node, string attrName, string defaultOwningClass,
			out string owningClass, out string property)
		{
			string listpath = XmlUtils.GetManditoryAttributeValue(node, attrName);
			string[] parts = listpath.Trim().Split('.');
			if (parts.Length > 1)
			{
				if (parts.Length != 2)
					throw new ConfigurationException("List id must not have more than two parts " + listpath);
				owningClass = parts[0];
				property = parts[1];
			}
			else
			{
				owningClass = defaultOwningClass;
				property = parts[0];
			}
		}

		ICmPossibilityList GetNamedList(XmlNode node, string attrName)
		{
			return GetNamedList(m_cache, node, attrName);
		}

		/// <summary>
		/// Return Hvo of a named list. If the list does not yet exist, then
		/// this will return zero.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="attrName"></param>
		/// <returns>Hvo or 0</returns>
		int GetNamedListHvo(XmlNode node, string attrName)
		{
			ICmPossibilityList possList = GetNamedList(node, attrName);
			return (possList == null) ? 0 : possList.Hvo;
		}

		/// <summary>
		/// Return Hvo of a named list. If the list does not yet exist, then
		/// this will return SpecialHVOValues.kHvoUninitializedObject.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="node"></param>
		/// <param name="attrName"></param>
		/// <returns>Hvo or 0</returns>
		internal static int GetNamedListHvo(FdoCache cache, XmlNode node, string attrName)
		{
			ICmPossibilityList possList = GetNamedList(cache, node, attrName);
			return (possList == null) ? (int)SpecialHVOValues.kHvoUninitializedObject : possList.Hvo;
		}

		/// <summary>
		/// Get a specification of a list from the specified attribute of the specified node,
		/// and return the indicated list.
		/// The descriptor is the name of one of the lists that is an attribute of
		/// LangProject, or else LexDb. followed by the name of a list that
		/// is an attribute of that class.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="node"></param>
		/// <param name="attrName"></param>
		/// <returns>An ICmPossibilityList (which may be null).</returns>
		internal static ICmPossibilityList GetNamedList(FdoCache cache, XmlNode node, string attrName)
		{
			string owningClass;
			string property;
			GetPathInfoFromColumnSpec(node, attrName, "LangProject", out owningClass, out property);
			string key = property;
			ICmObject owner;
			switch (owningClass)
			{
				case "LangProject":
					owner = cache.LanguageProject;
					break;
				case "LexDb":
					owner = cache.LanguageProject.LexDbOA;
					break;
				case "RnResearchNbk":
					owner = cache.LanguageProject.ResearchNotebookOA;
					break;
				case "unowned":
					try
					{
						if (!String.IsNullOrEmpty(property))
						{
							Guid guidList = new Guid(property);
							return cache.ServiceLocator.GetInstance<ICmPossibilityListRepository>().GetObject(guidList);
						}
					}
					catch
					{
					}
					throw new ConfigurationException(String.Format("List {0}.{1} currently not supported.", owningClass, property));
				default:
					throw new ConfigurationException(String.Format("List {0}.{1} currently not supported.", owningClass, property));
			}
			PropertyInfo pi = owner.GetType().GetProperty(key + "OA");
			if (pi == null)
				throw new ConfigurationException(String.Format("List {0}.{1} not in conceptual model.", owningClass, property));
			object result = pi.GetValue(owner, null);
			if (result != null && !(result is ICmPossibilityList))
				throw new ConfigurationException(
					String.Format("Specified property ({0}.{1}) does not return a possibility list, but a {2}.",
						owningClass, property, result.GetType().ToString()));
			return (ICmPossibilityList)result;
		}

		internal static string GetColumnLabel(Mediator mediator, XmlNode colSpec)
		{
			StringTable tbl = null;
			if (mediator != null && mediator.HasStringTable)
				tbl = mediator.StringTbl;
			string colName = XmlUtils.GetLocalizedAttributeValue(tbl, colSpec, "label", null);
			if (colName == null)
				colName = XmlUtils.GetManditoryAttributeValue(colSpec, "label");
			return colName;
		}

		BulkEditItem MakeItem(XmlNode colSpec)
		{
			string beSpec = XmlUtils.GetOptionalAttributeValue(colSpec, "bulkEdit", "");
			IBulkEditSpecControl besc = null;
			m_cache = m_bv.Cache; // Just in case not set yet.
			int flid;
			int hvoList;
			int ws;
			string items;
			int flidSub;

			switch (beSpec)
			{
				case "external":
					try
					{
						// NB: colSpec node must have a child node named dynamicloaderinfo.
						besc = (IBulkEditSpecControl)DynamicLoader.CreateObjectUsingLoaderNode(colSpec);
					}
					catch(Exception)
					{
						MessageBox.Show(XMLViewsStrings.ksBarElementFailed);
						return null;
					}
					break;
				case "atomicFlatListItem":
					flid = GetFlidFromClassDotName(colSpec, "field");
					hvoList = GetNamedListHvo(colSpec, "list");
					var list = (ICmPossibilityList) m_cache.ServiceLocator.GetObject(hvoList);
					if (RequiresDialogChooser(list))
					{
						besc = new ComplexListChooserBEditControl(m_cache, m_mediator, colSpec);
						break;
					}
					ws = WritingSystemServices.GetWritingSystem(m_cache, colSpec, null, WritingSystemServices.kwsAnal).Handle;
					besc = new FlatListChooserBEditControl(flid, hvoList, ws, false);
					break;
				case "morphTypeListItem":
					flid = GetFlidFromClassDotName(colSpec, "field");
					flidSub = GetFlidFromClassDotName(colSpec, "subfield");
					hvoList = GetNamedListHvo(colSpec, "list");
					ws = WritingSystemServices.GetWritingSystem(m_cache, colSpec, null, WritingSystemServices.kwsAnal).Handle;
					besc = new MorphTypeChooserBEditControl(flid, flidSub, hvoList, ws, m_bv);
					break;
				case "variantConditionListItem":
					flid = GetFlidFromClassDotName(colSpec, "field");
					hvoList = GetNamedListHvo(colSpec, "list");
					ws = WritingSystemServices.GetWritingSystem(m_cache, colSpec, null, WritingSystemServices.kwsAnal).Handle;
					besc = new VariantEntryTypesChooserBEditControl(m_cache, m_mediator, colSpec);
					break;
				case "integer":
					flid = GetFlidFromClassDotName(colSpec, "field");
					string[] stringList = m_bv.BrowseView.GetStringList(colSpec);
					if (stringList != null)
					besc = new IntChooserBEditControl(stringList, flid,
							XmlUtils.GetOptionalIntegerValue(colSpec, "defaultBulkEditChoice", 0));
					else
					{
						items = XmlUtils.GetManditoryAttributeValue(colSpec, "items");
						besc = new IntChooserBEditControl(items, flid);
					}
					break;
				case "integerOnSubfield":
					flid = GetFlidFromClassDotName(colSpec, "field");
					flidSub = GetFlidFromClassDotName(colSpec, "subfield");
					items = XmlUtils.GetManditoryAttributeValue(colSpec, "items");
					besc = new IntOnSubfieldChooserBEditControl(items, flid, flidSub);
					break;
				case "booleanOnSubfield":
					flid = GetFlidFromClassDotName(colSpec, "field");
					flidSub = GetFlidFromClassDotName(colSpec, "subfield");
					items = XmlUtils.GetManditoryAttributeValue(colSpec, "items");
					besc = new BoolOnSubfieldChooserBEditControl(items, flid, flidSub);
					break;
				case "boolean":
					flid = GetFlidFromClassDotName(colSpec, "field");
					items = XmlUtils.GetManditoryAttributeValue(colSpec, "items");
					besc = new BooleanChooserBEditControl(items, flid);
					break;
				case "complexListMultiple":
					besc = new ComplexListChooserBEditControl(m_cache, m_mediator, colSpec);
					break;
				case "variantEntryTypes":
					besc = new VariantEntryTypesChooserBEditControl(m_cache, m_mediator, colSpec);
					break;
				case "complexEntryTypes":
					besc = new ComplexListChooserBEditControl(m_cache, m_mediator, colSpec);
					break;
				default:
					return null;
			}
			besc.Cache = m_bv.Cache;
			besc.DataAccess = m_bv.SpecialCache;
			besc.Stylesheet = m_bv.StyleSheet;
			besc.Mediator = m_mediator;
			if (besc is IGhostable)
				(besc as IGhostable).InitForGhostItems(besc.Cache, colSpec);
			besc.ValueChanged += new FwSelectionChangedEventHandler(besc_ValueChanged);
			BulkEditItem bei = new BulkEditItem(besc);
			return bei;
		}

		/// <summary>
		/// Return true if the specified list requires us to use a chooser dialog rather than putting a simple combo box
		/// in the bulk edit bar. Currently we do this if the list is (actually, not just potentially) hierarchical,
		/// or if it has more than 25 items.
		/// Note that at least one unit test will break if this method causes the default Locations list to be treated
		/// as hierarchical.
		/// </summary>
		/// <param name="list"></param>
		/// <returns></returns>
		private bool RequiresDialogChooser(ICmPossibilityList list)
		{
			if (list.PossibilitiesOS.Count > 25)
				return true;
			foreach (var item in list.PossibilitiesOS)
			{
				if (item.SubPossibilitiesOS.Count > 0)
					return true;
			}
			return false;
		}




		/// <summary>
		/// The selected item may have changed in the "Change To" comboBox under
		/// the List Choice tab therefore
		/// enable or disable the Apply and Preview buttons based on the selection.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e">in some situations an item selected in the combo box has no hvo so the index of the item in the list is used.</param>
		void besc_ValueChanged(object sender, FwObjectSelectionEventArgs e)
		{

			if (e.Hvo != 0 || e.Index >= 0)
			{
				m_ApplyButton.Enabled = true;
				m_previewButton.Enabled = true;
				return;
			}
			else
			{
				m_ApplyButton.Enabled = false;
				m_previewButton.Enabled = false;
				return;
			}
		}

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BulkEditBar));
			this.m_operationLabel = new System.Windows.Forms.Label();
			this.m_operationsTabControl = new SIL.FieldWorks.Common.Widgets.VSTabControl();
			this.m_listChoiceTab = new System.Windows.Forms.TabPage();
			this.m_bulkEditIconButton = new System.Windows.Forms.Button();
			this.m_imageList16x16 = new System.Windows.Forms.ImageList(this.components);
			this.m_bulkEditOperationLabel = new System.Windows.Forms.Label();
			this.m_listChoiceChangeToCombo = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.label3 = new System.Windows.Forms.Label();
			this.m_listChoiceTargetCombo = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.label2 = new System.Windows.Forms.Label();
			this.m_closeButton = new System.Windows.Forms.Button();
			this.m_bulkCopyTab = new System.Windows.Forms.TabPage();
			this.m_bulkCopySourceCombo = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.label4 = new System.Windows.Forms.Label();
			this.m_bulkCopyTargetCombo = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.label5 = new System.Windows.Forms.Label();
			this.m_clickCopyTab = new System.Windows.Forms.TabPage();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.m_clickCopySepBox = new SIL.FieldWorks.Common.Widgets.FwTextBox();
			this.m_clickCopyOverwriteButton = new System.Windows.Forms.RadioButton();
			this.m_clickCopyAppendButton = new System.Windows.Forms.RadioButton();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.m_clickCopyReorderButton = new System.Windows.Forms.RadioButton();
			this.m_clickCopyWordButton = new System.Windows.Forms.RadioButton();
			this.m_clickCopyTargetCombo = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.label6 = new System.Windows.Forms.Label();
			this.m_transduceTab = new System.Windows.Forms.TabPage();
			this.m_transduceSetupButton = new System.Windows.Forms.Button();
			this.m_transduceSourceCombo = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.label7 = new System.Windows.Forms.Label();
			this.m_transduceTargetCombo = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.label8 = new System.Windows.Forms.Label();
			this.m_transduceProcessorCombo = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.label9 = new System.Windows.Forms.Label();
			this.m_findReplaceTab = new System.Windows.Forms.TabPage();
			this.m_findReplaceSummaryLabel = new System.Windows.Forms.Label();
			this.m_findReplaceSetupButton = new System.Windows.Forms.Button();
			this.m_findReplaceTargetCombo = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.label12 = new System.Windows.Forms.Label();
			this.m_deleteTab = new System.Windows.Forms.TabPage();
			this.label13 = new System.Windows.Forms.Label();
			this.m_deleteWhatCombo = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.m_imageList16x14 = new System.Windows.Forms.ImageList(this.components);
			this.m_helpButton = new System.Windows.Forms.Button();
			this.m_previewButton = new System.Windows.Forms.Button();
			this.m_ApplyButton = new System.Windows.Forms.Button();
			this.m_operationsTabControl.SuspendLayout();
			this.m_listChoiceTab.SuspendLayout();
			this.m_bulkCopyTab.SuspendLayout();
			this.m_clickCopyTab.SuspendLayout();
			this.groupBox2.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_clickCopySepBox)).BeginInit();
			this.groupBox1.SuspendLayout();
			this.m_transduceTab.SuspendLayout();
			this.m_findReplaceTab.SuspendLayout();
			this.m_deleteTab.SuspendLayout();
			this.SuspendLayout();
			//
			// m_operationLabel
			//
			resources.ApplyResources(this.m_operationLabel, "m_operationLabel");
			this.m_operationLabel.BackColor = System.Drawing.Color.Transparent;
			this.m_operationLabel.Name = "m_operationLabel";
			//
			// m_operationsTabControl
			//
			resources.ApplyResources(this.m_operationsTabControl, "m_operationsTabControl");
			this.m_operationsTabControl.Controls.Add(this.m_listChoiceTab);
			this.m_operationsTabControl.Controls.Add(this.m_bulkCopyTab);
			this.m_operationsTabControl.Controls.Add(this.m_clickCopyTab);
			this.m_operationsTabControl.Controls.Add(this.m_transduceTab);
			this.m_operationsTabControl.Controls.Add(this.m_findReplaceTab);
			this.m_operationsTabControl.Controls.Add(this.m_deleteTab);
			this.m_operationsTabControl.Name = "m_operationsTabControl";
			this.m_operationsTabControl.SelectedIndex = 0;
			//
			// m_listChoiceTab
			//
			this.m_listChoiceTab.Controls.Add(this.m_bulkEditIconButton);
			this.m_listChoiceTab.Controls.Add(this.m_operationLabel);
			this.m_listChoiceTab.Controls.Add(this.m_bulkEditOperationLabel);
			this.m_listChoiceTab.Controls.Add(this.m_listChoiceChangeToCombo);
			this.m_listChoiceTab.Controls.Add(this.label3);
			this.m_listChoiceTab.Controls.Add(this.m_listChoiceTargetCombo);
			this.m_listChoiceTab.Controls.Add(this.label2);
			this.m_listChoiceTab.Controls.Add(this.m_closeButton);
			resources.ApplyResources(this.m_listChoiceTab, "m_listChoiceTab");
			this.m_listChoiceTab.Name = "m_listChoiceTab";
			this.m_listChoiceTab.UseVisualStyleBackColor = true;
			this.m_listChoiceTab.Enter += new System.EventHandler(this.m_listChoiceTab_Enter);
			//
			// m_bulkEditIconButton
			//
			this.m_bulkEditIconButton.BackColor = System.Drawing.Color.Transparent;
			resources.ApplyResources(this.m_bulkEditIconButton, "m_bulkEditIconButton");
			this.m_bulkEditIconButton.ImageList = this.m_imageList16x16;
			this.m_bulkEditIconButton.Name = "m_bulkEditIconButton";
			this.m_bulkEditIconButton.UseVisualStyleBackColor = false;
			//
			// m_imageList16x16
			//
			this.m_imageList16x16.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_imageList16x16.ImageStream")));
			this.m_imageList16x16.TransparentColor = System.Drawing.Color.Fuchsia;
			this.m_imageList16x16.Images.SetKeyName(0, "BulkEditIcon.bmp");
			//
			// m_bulkEditOperationLabel
			//
			resources.ApplyResources(this.m_bulkEditOperationLabel, "m_bulkEditOperationLabel");
			this.m_bulkEditOperationLabel.BackColor = System.Drawing.Color.Transparent;
			this.m_bulkEditOperationLabel.ForeColor = System.Drawing.Color.Blue;
			this.m_bulkEditOperationLabel.Name = "m_bulkEditOperationLabel";
			//
			// m_listChoiceChangeToCombo
			//
			this.m_listChoiceChangeToCombo.AllowSpaceInEditBox = false;
			this.m_listChoiceChangeToCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.m_listChoiceChangeToCombo, "m_listChoiceChangeToCombo");
			this.m_listChoiceChangeToCombo.Name = "m_listChoiceChangeToCombo";
			//
			// label3
			//
			resources.ApplyResources(this.label3, "label3");
			this.label3.Name = "label3";
			//
			// m_listChoiceTargetCombo
			//
			this.m_listChoiceTargetCombo.AllowSpaceInEditBox = false;
			this.m_listChoiceTargetCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.m_listChoiceTargetCombo, "m_listChoiceTargetCombo");
			this.m_listChoiceTargetCombo.Name = "m_listChoiceTargetCombo";
			//
			// label2
			//
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			//
			// m_closeButton
			//
			resources.ApplyResources(this.m_closeButton, "m_closeButton");
			this.m_closeButton.ForeColor = System.Drawing.SystemColors.Control;
			this.m_closeButton.Name = "m_closeButton";
			//
			// m_bulkCopyTab
			//
			this.m_bulkCopyTab.Controls.Add(this.m_bulkCopySourceCombo);
			this.m_bulkCopyTab.Controls.Add(this.label4);
			this.m_bulkCopyTab.Controls.Add(this.m_bulkCopyTargetCombo);
			this.m_bulkCopyTab.Controls.Add(this.label5);
			resources.ApplyResources(this.m_bulkCopyTab, "m_bulkCopyTab");
			this.m_bulkCopyTab.ForeColor = System.Drawing.SystemColors.ControlText;
			this.m_bulkCopyTab.Name = "m_bulkCopyTab";
			this.m_bulkCopyTab.UseVisualStyleBackColor = true;
			this.m_bulkCopyTab.Enter += new System.EventHandler(this.m_bulkCopyTab_Enter);
			//
			// m_bulkCopySourceCombo
			//
			this.m_bulkCopySourceCombo.AllowSpaceInEditBox = false;
			this.m_bulkCopySourceCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.m_bulkCopySourceCombo, "m_bulkCopySourceCombo");
			this.m_bulkCopySourceCombo.Name = "m_bulkCopySourceCombo";
			//
			// label4
			//
			resources.ApplyResources(this.label4, "label4");
			this.label4.Name = "label4";
			//
			// m_bulkCopyTargetCombo
			//
			this.m_bulkCopyTargetCombo.AllowSpaceInEditBox = false;
			this.m_bulkCopyTargetCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.m_bulkCopyTargetCombo, "m_bulkCopyTargetCombo");
			this.m_bulkCopyTargetCombo.Name = "m_bulkCopyTargetCombo";
			//
			// label5
			//
			resources.ApplyResources(this.label5, "label5");
			this.label5.Name = "label5";
			//
			// m_clickCopyTab
			//
			this.m_clickCopyTab.Controls.Add(this.groupBox2);
			this.m_clickCopyTab.Controls.Add(this.groupBox1);
			this.m_clickCopyTab.Controls.Add(this.m_clickCopyTargetCombo);
			this.m_clickCopyTab.Controls.Add(this.label6);
			resources.ApplyResources(this.m_clickCopyTab, "m_clickCopyTab");
			this.m_clickCopyTab.Name = "m_clickCopyTab";
			this.m_clickCopyTab.UseVisualStyleBackColor = true;
			this.m_clickCopyTab.Enter += new System.EventHandler(this.m_clickCopyTab_Enter);
			//
			// groupBox2
			//
			this.groupBox2.Controls.Add(this.m_clickCopySepBox);
			this.groupBox2.Controls.Add(this.m_clickCopyOverwriteButton);
			this.groupBox2.Controls.Add(this.m_clickCopyAppendButton);
			resources.ApplyResources(this.groupBox2, "groupBox2");
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.TabStop = false;
			//
			// m_clickCopySepBox
			//
			this.m_clickCopySepBox.AdjustStringHeight = true;
			this.m_clickCopySepBox.BackColor = System.Drawing.SystemColors.Window;
			this.m_clickCopySepBox.controlID = null;
			resources.ApplyResources(this.m_clickCopySepBox, "m_clickCopySepBox");
			this.m_clickCopySepBox.HasBorder = true;
			this.m_clickCopySepBox.Name = "m_clickCopySepBox";
			this.m_clickCopySepBox.SelectionLength = 0;
			this.m_clickCopySepBox.SelectionStart = 0;
			//
			// m_clickCopyOverwriteButton
			//
			resources.ApplyResources(this.m_clickCopyOverwriteButton, "m_clickCopyOverwriteButton");
			this.m_clickCopyOverwriteButton.Name = "m_clickCopyOverwriteButton";
			//
			// m_clickCopyAppendButton
			//
			resources.ApplyResources(this.m_clickCopyAppendButton, "m_clickCopyAppendButton");
			this.m_clickCopyAppendButton.Checked = true;
			this.m_clickCopyAppendButton.Name = "m_clickCopyAppendButton";
			this.m_clickCopyAppendButton.TabStop = true;
			//
			// groupBox1
			//
			this.groupBox1.Controls.Add(this.m_clickCopyReorderButton);
			this.groupBox1.Controls.Add(this.m_clickCopyWordButton);
			resources.ApplyResources(this.groupBox1, "groupBox1");
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.TabStop = false;
			//
			// m_clickCopyReorderButton
			//
			resources.ApplyResources(this.m_clickCopyReorderButton, "m_clickCopyReorderButton");
			this.m_clickCopyReorderButton.Name = "m_clickCopyReorderButton";
			//
			// m_clickCopyWordButton
			//
			resources.ApplyResources(this.m_clickCopyWordButton, "m_clickCopyWordButton");
			this.m_clickCopyWordButton.Checked = true;
			this.m_clickCopyWordButton.Name = "m_clickCopyWordButton";
			this.m_clickCopyWordButton.TabStop = true;
			//
			// m_clickCopyTargetCombo
			//
			this.m_clickCopyTargetCombo.AllowSpaceInEditBox = false;
			this.m_clickCopyTargetCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.m_clickCopyTargetCombo, "m_clickCopyTargetCombo");
			this.m_clickCopyTargetCombo.Name = "m_clickCopyTargetCombo";
			this.m_clickCopyTargetCombo.SelectedIndexChanged += new System.EventHandler(this.m_clickCopyTargetCombo_SelectedIndexChanged);
			//
			// label6
			//
			resources.ApplyResources(this.label6, "label6");
			this.label6.Name = "label6";
			//
			// m_transduceTab
			//
			this.m_transduceTab.Controls.Add(this.m_transduceSetupButton);
			this.m_transduceTab.Controls.Add(this.m_transduceSourceCombo);
			this.m_transduceTab.Controls.Add(this.label7);
			this.m_transduceTab.Controls.Add(this.m_transduceTargetCombo);
			this.m_transduceTab.Controls.Add(this.label8);
			this.m_transduceTab.Controls.Add(this.m_transduceProcessorCombo);
			this.m_transduceTab.Controls.Add(this.label9);
			resources.ApplyResources(this.m_transduceTab, "m_transduceTab");
			this.m_transduceTab.Name = "m_transduceTab";
			this.m_transduceTab.UseVisualStyleBackColor = true;
			this.m_transduceTab.Enter += new System.EventHandler(this.m_transduceTab_Enter);
			//
			// m_transduceSetupButton
			//
			resources.ApplyResources(this.m_transduceSetupButton, "m_transduceSetupButton");
			this.m_transduceSetupButton.Name = "m_transduceSetupButton";
			this.m_transduceSetupButton.Click += new System.EventHandler(this.m_transduceSetupButton_Click);
			//
			// m_transduceSourceCombo
			//
			this.m_transduceSourceCombo.AllowSpaceInEditBox = false;
			this.m_transduceSourceCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.m_transduceSourceCombo, "m_transduceSourceCombo");
			this.m_transduceSourceCombo.Name = "m_transduceSourceCombo";
			//
			// label7
			//
			resources.ApplyResources(this.label7, "label7");
			this.label7.Name = "label7";
			//
			// m_transduceTargetCombo
			//
			this.m_transduceTargetCombo.AllowSpaceInEditBox = false;
			this.m_transduceTargetCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.m_transduceTargetCombo, "m_transduceTargetCombo");
			this.m_transduceTargetCombo.Name = "m_transduceTargetCombo";
			//
			// label8
			//
			resources.ApplyResources(this.label8, "label8");
			this.label8.Name = "label8";
			//
			// m_transduceProcessorCombo
			//
			this.m_transduceProcessorCombo.AllowSpaceInEditBox = false;
			this.m_transduceProcessorCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_transduceProcessorCombo.Items.AddRange(new object[] {
			resources.GetString("m_transduceProcessorCombo.Items"),
			resources.GetString("m_transduceProcessorCombo.Items1")});
			resources.ApplyResources(this.m_transduceProcessorCombo, "m_transduceProcessorCombo");
			this.m_transduceProcessorCombo.Name = "m_transduceProcessorCombo";
			//
			// label9
			//
			resources.ApplyResources(this.label9, "label9");
			this.label9.Name = "label9";
			//
			// m_findReplaceTab
			//
			this.m_findReplaceTab.Controls.Add(this.m_findReplaceSummaryLabel);
			this.m_findReplaceTab.Controls.Add(this.m_findReplaceSetupButton);
			this.m_findReplaceTab.Controls.Add(this.m_findReplaceTargetCombo);
			this.m_findReplaceTab.Controls.Add(this.label12);
			resources.ApplyResources(this.m_findReplaceTab, "m_findReplaceTab");
			this.m_findReplaceTab.Name = "m_findReplaceTab";
			this.m_findReplaceTab.UseVisualStyleBackColor = true;
			this.m_findReplaceTab.Enter += new System.EventHandler(this.m_findReplaceTab_Enter);
			//
			// m_findReplaceSummaryLabel
			//
			resources.ApplyResources(this.m_findReplaceSummaryLabel, "m_findReplaceSummaryLabel");
			this.m_findReplaceSummaryLabel.Name = "m_findReplaceSummaryLabel";
			//
			// m_findReplaceSetupButton
			//
			resources.ApplyResources(this.m_findReplaceSetupButton, "m_findReplaceSetupButton");
			this.m_findReplaceSetupButton.Name = "m_findReplaceSetupButton";
			this.m_findReplaceSetupButton.Click += new System.EventHandler(this.m_findReplaceSetupButton_Click);
			//
			// m_findReplaceTargetCombo
			//
			this.m_findReplaceTargetCombo.AllowSpaceInEditBox = false;
			this.m_findReplaceTargetCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.m_findReplaceTargetCombo, "m_findReplaceTargetCombo");
			this.m_findReplaceTargetCombo.Name = "m_findReplaceTargetCombo";
			this.m_findReplaceTargetCombo.SelectedIndexChanged += new System.EventHandler(this.m_findReplaceTargetCombo_SelectedIndexChanged);
			//
			// label12
			//
			resources.ApplyResources(this.label12, "label12");
			this.label12.Name = "label12";
			//
			// m_deleteTab
			//
			this.m_deleteTab.Controls.Add(this.label13);
			this.m_deleteTab.Controls.Add(this.m_deleteWhatCombo);
			resources.ApplyResources(this.m_deleteTab, "m_deleteTab");
			this.m_deleteTab.Name = "m_deleteTab";
			this.m_deleteTab.UseVisualStyleBackColor = true;
			this.m_deleteTab.Enter += new System.EventHandler(this.m_deleteTab_Enter);
			//
			// label13
			//
			resources.ApplyResources(this.label13, "label13");
			this.label13.Name = "label13";
			//
			// m_deleteWhatCombo
			//
			this.m_deleteWhatCombo.AllowSpaceInEditBox = false;
			this.m_deleteWhatCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.m_deleteWhatCombo, "m_deleteWhatCombo");
			this.m_deleteWhatCombo.Name = "m_deleteWhatCombo";
			this.m_deleteWhatCombo.SelectedIndexChanged += new System.EventHandler(this.m_deleteWhatCombo_SelectedIndexChanged);
			//
			// m_imageList16x14
			//
			this.m_imageList16x14.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_imageList16x14.ImageStream")));
			this.m_imageList16x14.TransparentColor = System.Drawing.Color.Magenta;
			this.m_imageList16x14.Images.SetKeyName(0, "");
			//
			// m_helpButton
			//
			this.m_helpButton.ForeColor = System.Drawing.SystemColors.ControlText;
			resources.ApplyResources(this.m_helpButton, "m_helpButton");
			this.m_helpButton.Name = "m_helpButton";
			this.m_helpButton.Click += new System.EventHandler(this.m_helpButton_Click);
			//
			// m_previewButton
			//
			this.m_previewButton.ForeColor = System.Drawing.SystemColors.ControlText;
			resources.ApplyResources(this.m_previewButton, "m_previewButton");
			this.m_previewButton.Name = "m_previewButton";
			//
			// m_ApplyButton
			//
			this.m_ApplyButton.ForeColor = System.Drawing.SystemColors.ControlText;
			resources.ApplyResources(this.m_ApplyButton, "m_ApplyButton");
			this.m_ApplyButton.Name = "m_ApplyButton";
			//
			// BulkEditBar
			//
			this.Controls.Add(this.m_ApplyButton);
			this.Controls.Add(this.m_previewButton);
			this.Controls.Add(this.m_helpButton);
			this.Controls.Add(this.m_operationsTabControl);
			this.ForeColor = System.Drawing.SystemColors.ControlText;
			this.Name = "BulkEditBar";
			resources.ApplyResources(this, "$this");
			this.m_operationsTabControl.ResumeLayout(false);
			this.m_listChoiceTab.ResumeLayout(false);
			this.m_listChoiceTab.PerformLayout();
			this.m_bulkCopyTab.ResumeLayout(false);
			this.m_bulkCopyTab.PerformLayout();
			this.m_clickCopyTab.ResumeLayout(false);
			this.m_clickCopyTab.PerformLayout();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_clickCopySepBox)).EndInit();
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.m_transduceTab.ResumeLayout(false);
			this.m_transduceTab.PerformLayout();
			this.m_findReplaceTab.ResumeLayout(false);
			this.m_findReplaceTab.PerformLayout();
			this.m_deleteTab.ResumeLayout(false);
			this.m_deleteTab.PerformLayout();
			this.ResumeLayout(false);

		}
		#endregion

		bool DeleteRowsItemSelected
		{
			get
			{
				if (m_operationsTabControl.SelectedTab != m_deleteTab)
					return false;
				return m_deleteWhatCombo.SelectedItem is ListClassTargetFieldItem;
			}
		}

		List<ListClassTargetFieldItem> ListItemsClassesInfo(Set<int> classes)
		{
			List<ListClassTargetFieldItem> targetClasses = new List<ListClassTargetFieldItem>();
			if (!m_mediator.HasStringTable)
				return targetClasses;
			StringTable tbl = m_mediator.StringTbl;
			foreach (int classId in classes)
			{
				string pluralOfClass;
				// get plural form labels from AlternativeTitles
				XmlViewsUtils.TryFindPluralFormFromClassId(m_bv.SpecialCache.MetaDataCache, tbl, classId, out pluralOfClass);
				if (pluralOfClass.Length > 0)
				{
					targetClasses.Add(new ListClassTargetFieldItem(pluralOfClass +
																   " (" + XMLViewsStrings.ksRows + ")", classId));
				}
			}
			return targetClasses;
		}

		void m_deleteWhatCombo_SelectedIndexChanged(object sender, EventArgs e)
		{
			var fWasPreviewOn = PreviewOn;
			if (fWasPreviewOn)
				ClearPreviewState();
			FieldComboItem selectedItem = m_deleteWhatCombo.SelectedItem as FieldComboItem;
			// need to change target before we do anything else, because calculating
			// the items depends upon having the right sort of items listed.
			OnTargetComboItemChanged(selectedItem);

			bool fWasShowEnabled = m_bv.BrowseView.Vc.ShowEnabled;
			if (DeleteRowsItemSelected)
			{
				// Delete rows
				m_operationLabel.Text = s_labels[m_operationsTabControl.SelectedIndex];
				m_ApplyButton.Text = XMLViewsStrings.ksDelete;
				m_previewButton.Enabled = false;
				m_bv.BrowseView.Vc.ShowEnabled = true;
				SetEnabledForAllItems();
			}
			else
			{
				m_operationLabel.Text = XMLViewsStrings.ksDeleteDesc2;
				m_previewButton.Enabled = true;
				m_ApplyButton.Text = m_originalApplyText;
				m_bv.BrowseView.Vc.ShowEnabled = false;
			}
			if (fWasPreviewOn || fWasShowEnabled != m_bv.BrowseView.Vc.ShowEnabled && m_bv.BrowseView.RootBox != null)
				m_bv.BrowseView.RootBox.Reconstruct();
		}

		internal void SetEnabledIfShowing()
		{
			CheckDisposed();

			if (!m_bv.BrowseView.Vc.ShowEnabled)
				return;
			SetEnabledForAllItems();
			m_bv.BrowseView.RootBox.Reconstruct();
		}

		private void SetEnabledForAllItems()
		{
			m_items = ItemsToChangeSet(false);
			// No.
			// IVwCacheDa cda = m_cache.VwCacheDaAccessor;
			UpdateCurrentGhostParentHelper(); // needed for AllowDeleteItem()
			foreach (int hvoItem in m_items)
				//cda.CacheIntProp(hvoItem, XMLViewsDataCache.ktagItemEnabled, AllowDeleteItem(hvoItem));
				// Use special SDA instead.
				m_bv.SpecialCache.SetInt(hvoItem, XMLViewsDataCache.ktagItemEnabled, AllowDeleteItem(hvoItem));
		}

		internal void UpdateCheckedItems()
		{
			if (m_bv.BrowseView.Vc.ShowEnabled && (m_items == null || m_items.Count == 0))
			{
				// it's about time to try to load these items and their checked state.
				SetEnabledForAllItems();
				if (m_bv.BrowseView.RootBox != null)
					m_bv.BrowseView.RootBox.Reconstruct();
			}
			//ResumeRecordListRowChanges();
		}

		/// <summary>
		/// The selected state of the specified item may be changing, update enable values as appropriate.
		/// </summary>
		/// <param name="hvoItem"></param>
		internal void UpdateEnableItems(int hvoItem)
		{
			CheckDisposed();

			// Only relevant for the delete rows function.
			if (!DeleteRowsItemSelected)
				return;

			// currently only handle update enable for Sense objects.
			int clsid = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoItem).ClassID;
			if (clsid != LexSenseTags.kClassId)
				return; // currently we only restrict senses.

			int hvoOwner = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoItem).Owner.Hvo;
			int clsOwner = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoOwner).ClassID;
			if (clsOwner != LexEntryTags.kClassId)
				return; // subsenses are always OK to delete, change can't have any effect.
			ISilDataAccess sda = m_bv.SpecialCache;
			int chvo = sda.get_VecSize(hvoOwner, LexEntryTags.kflidSenses);
			if (chvo == 1)
				return; // only sense, nothing can have changed.

			UpdateCurrentGhostParentHelper();
			for (int i = 0; i < chvo; i++)
			{
				int hvoSense = m_bv.SpecialCache.get_VecItem(hvoOwner, LexEntryTags.kflidSenses, i);
				int wasEnabled = m_bv.SpecialCache.get_IntProp(hvoSense, XMLViewsDataCache.ktagItemEnabled);
				int enabled = AllowDeleteItem(hvoSense);
				if (enabled != wasEnabled)
				{
					m_bv.SpecialCache.SetInt(hvoSense, XMLViewsDataCache.ktagItemEnabled, enabled);
				}
			}
		}

		/// <summary>
		/// Return 1 if the item is allowed to be deleted, 0 if not.
		/// NOTE: in some cases, UpdateCurrentGhostParentHelper() needs to be called prior
		/// to calling AllowDeleteItem.
		/// </summary>
		/// <param name="hvoItem"></param>
		/// <returns></returns>
		private int AllowDeleteItem(int hvoItem)
		{
			int clsid = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoItem).ClassID;
			// we only want to disallow the first sense, if we're not dealing with ghostable targets.
			if (clsid == LexSenseTags.kClassId && m_ghostParentHelper == null)
			{
				int hvoOwner = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoItem).Owner.Hvo;
				int clsOwner = (int)m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoOwner).ClassID;
				if (clsOwner != LexEntryTags.kClassId)
					return 1; // subsenses are always OK to delete.
				ISilDataAccess sda = m_cache.DomainDataByFlid;
				int chvo = sda.get_VecSize(hvoOwner, LexEntryTags.kflidSenses);
				if (chvo == 1)
					return 0; // can't delete only sense.
				for (int i = 0; i < chvo; i++)
				{
					int hvoSense = sda.get_VecItem(hvoOwner, LexEntryTags.kflidSenses, i);
					if (hvoSense == hvoItem)
					{
						// Senses other than the first are never disabled. Deleting the first sense
						// is disabled if all others are marked for deletion.
						if (i != 0)
							return 1;
						continue; // We're looking for another sense not marked for deletion
					}
					if (!m_items.Contains(hvoSense))
						return 1; // there's a sense currently not even in the filter.
					if (m_bv.GetCheckState(hvoSense) == 0)
						return 1; // some other sense is not being deleted, ok to delete this.
				}
				return 0; // this is the first sense and is the only one not marked for deletion.
			}
			else
			{
				return CanDeleteItemOfClassOrGhostOwner(hvoItem, (int)clsid) ? 1 : 0;
			}
		}

		private bool CanDeleteItemOfClassOrGhostOwner(int hvoItem, int clsid)
		{
			//allow deletion for the class we expect to be bulk editing.
			if (DomainObjectServices.IsSameOrSubclassOf(m_cache.DomainDataByFlid.MetaDataCache, clsid, m_expectedListItemsClassId))
				return true;

			// allow bulk delete if ghost child already exists, otherwise be disabled.
			// NOTE: in this case UpdateCurrentGhostParentHelper() needs to be called prior
			// to calling AllowDeleteItem().
			if (m_ghostParentHelper != null && m_ghostParentHelper.IsGhostOwnerClass(hvoItem) &&
				!m_ghostParentHelper.IsGhostOwnerChildless(hvoItem))
			{
				// child exists, so allow bulk editing for child.
				return true;
			}
			return false;
		}

		GhostParentHelper m_ghostParentHelper = null;
		/// <summary>
		/// needed for AllowDeleteItem().
		/// </summary>
		/// <returns></returns>
		private GhostParentHelper UpdateCurrentGhostParentHelper()
		{
			m_ghostParentHelper = null;
			// see if the object is a ghost object owner.
			foreach (GhostParentHelper helper in m_bulkEditListItemsGhostFields)
			{
				if (helper.TargetClass == m_expectedListItemsClassId)
				{
					m_ghostParentHelper = helper;
					break;
				}
			}
			return m_ghostParentHelper;
		}

		void m_helpButton_Click(object sender, EventArgs e)
		{
			string helpTopic = "";

			switch(m_mediator.PropertyTable.GetStringProperty("currentContentControl", null))
			{
				case "bulkEditEntriesOrSenses":
					helpTopic = "khtpBulkEditBarEntriesOrSenses";
					break;
				case "reversalToolBulkEditReversalEntries":
					helpTopic = "khtpBulkEditBarReversalEntries";
					break;
				case "toolBulkEditWordforms":
					helpTopic = "khtpBulkEditBarWordforms";
					break;
			}

			if(helpTopic != "")
				ShowHelp.ShowHelpTopic(m_mediator.HelpTopicProvider, helpTopic);
		}

		private void m_listChoiceTargetCombo_SelectedIndexChanged(object sender, EventArgs e)
		{
			TargetFieldItem tfm = m_listChoiceTargetCombo.SelectedItem as TargetFieldItem;
			if (tfm == null)
			{
				// Show the dummy combo if this somehow happens, just to give the look and feel.
				m_listChoiceChangeToCombo.Visible = true;
				return;
			}
			int index = tfm.ColumnIndex;
			m_listChoiceTab.SuspendLayout();
			RemoveOldChoiceControl();
			BulkEditItem bei = m_beItems[index];
			m_listChoiceChangeToCombo.Visible = false; // it's a placeholder
			m_listChoiceChangeToCombo.Enabled = false;

			m_listChoiceControl = bei.BulkEditControl.Control;
			m_listChoiceControl.Location = m_listChoiceChangeToCombo.Location;
			m_listChoiceControl.Size = m_listChoiceChangeToCombo.Size;
			m_listChoiceControl.Anchor = m_listChoiceChangeToCombo.Anchor;
			Debug.Assert(String.IsNullOrEmpty(m_listChoiceControl.Name),
				String.Format("not sure if we want to permenantly overwrite an existing name. was {0}. "
						+ "currently used in BulkEditBarTests for Controls.Find", m_listChoiceControl.Name));
			m_listChoiceControl.Name = "m_listChoiceControl";
			m_itemIndex = index;
			m_listChoiceTab.Controls.Add(m_listChoiceControl);
			m_listChoiceTab.ResumeLayout(false);
			m_listChoiceTab.PerformLayout();

			EnablePreviewApplyForListChoice();
			OnTargetComboItemChanged(tfm);
		}

		/// <summary>
		/// Remove old (bulk edit) choice control
		/// </summary>
		private void RemoveOldChoiceControl()
		{
			if (m_listChoiceControl != null)
			{
				m_listChoiceTab.Controls.Remove(m_listChoiceControl);
				m_listChoiceControl.Name = ""; // no longer the current listChoiceControl
				m_listChoiceControl = null;
			}
		}

		private bool PreviewOn
		{
			get { return previewOn; }
			set
			{
				previewOn = value;
				switch (previewOn)
				{
					case true:
						m_previewButton.Text = XMLViewsStrings.ksClear;
						break;
					case false:
						m_previewButton.Text = XMLViewsStrings.ksPreview;
						break;
				}
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected void m_previewButton_Click(object sender, EventArgs e)
		{
			int oldCol = m_bv.SpecialCache.get_IntProp(m_bv.RootObjectHvo, XMLViewsDataCache.ktagActiveColumn);
			int newCol = oldCol;
			// Fixes LT-8336 by making sure that a highlighted row that was visible before the change is
			// still visible after it too.
			using (new ReconstructPreservingBVScrollPosition(m_bv))
			{
				// Both of these need manual disposing.
				// 'using' with the ProgressState fixes LT-4186, since it forces the manual Dispose call,
				// which, in turn, clears the progress panel.
				using (ProgressState state = CreateSimpleProgressState(m_mediator))
				using (new SIL.Utils.WaitCursor(this))
				{
					if (previewOn)
					{
						// Clear the preview
						// No fake data caching.
						//m_bv.Cache.VwCacheDaAccessor.CacheIntProp(m_bv.RootObjectHvo, XmlBrowseViewVc.ktagActiveColumn, 0);
						// Use the BrowseViewer Decorator SDA instead.
						ClearPreviewState();
					}
					else
					{
						PreviewOn = true;
						if (m_operationsTabControl.SelectedTab == m_listChoiceTab)
						{
							if (m_itemIndex >= 0)
							{
								BulkEditItem bei = m_beItems[m_itemIndex];
								bei.BulkEditControl.FakeDoit(ItemsToChange(false), XMLViewsDataCache.ktagAlternateValue,
															 XMLViewsDataCache.ktagItemEnabled, state);
								newCol = m_itemIndex + 1;
							}
						}
						else if (m_operationsTabControl.SelectedTab == m_findReplaceTab)
						{
							ReplaceWithMethod method = MakeReplaceWithMethod(out newCol);
							if (method == null)
								return;
							method.FakeDoit(ItemsToChange(false), XMLViewsDataCache.ktagAlternateValue,
											XMLViewsDataCache.ktagItemEnabled, state);
						}
						else if (m_operationsTabControl.SelectedTab == m_bulkCopyTab)
						{
							BulkCopyMethod method = MakeBulkCopyMethod(out newCol);
							if (method == null)
								return;
							method.FakeDoit(ItemsToChange(false), XMLViewsDataCache.ktagAlternateValue,
											XMLViewsDataCache.ktagItemEnabled, state);
						}
						else if (m_operationsTabControl.SelectedTab == m_transduceTab)
						{
							TransduceMethod method = MakeTransduceMethod(out newCol);
							if (method == null)
								return;
							method.FakeDoit(ItemsToChange(false), XMLViewsDataCache.ktagAlternateValue,
											XMLViewsDataCache.ktagItemEnabled, state);
						}
						else if (m_operationsTabControl.SelectedTab == m_deleteTab && !DeleteRowsItemSelected)
						{
							// clear a field
							if (m_deleteWhatCombo.SelectedItem is TargetFieldItem)
							{
								TargetFieldItem item = m_deleteWhatCombo.SelectedItem as TargetFieldItem;
								int index = item.ColumnIndex;
								BulkEditItem bei = m_beItems[index];
								bei.BulkEditControl.SetClearField();
								bei.BulkEditControl.FakeDoit(ItemsToChange(false), XMLViewsDataCache.ktagAlternateValue,
															 XMLViewsDataCache.ktagItemEnabled, state);
								newCol = index + 1;
							}
							else if (m_deleteWhatCombo.SelectedItem is FieldComboItem)
							{
								ClearMethod method = MakeClearMethod(out newCol);
								if (method == null)
									return;
								method.FakeDoit(ItemsToChange(false), XMLViewsDataCache.ktagAlternateValue,
												XMLViewsDataCache.ktagItemEnabled, state);
							}
						}
						else
						{
							MessageBox.Show(this, XMLViewsStrings.ksSorryNoPreview, XMLViewsStrings.ksUnimplFeature);
							PreviewOn = false; // Didn't actually happen
						}

						if (oldCol != newCol)
						{
							// Don't store in the real cache (it has no IVwCacheDa interface anyway).
							// (sda as IVwCacheDa).CacheIntProp(m_bv.RootObjectHvo, XmlBrowseViewVc.ktagActiveColumn, newCol);
							// Use the BrowseViewer special 'decorator' SDA.
							m_bv.SpecialCache.SetInt(m_bv.RootObjectHvo, XMLViewsDataCache.ktagActiveColumn, newCol);
						}
					}
				}
			} // End using(ReconstructPreservingBVScrollPosition) [Does RootBox.Reconstruct() here.]
		}

		private void ClearPreviewState()
		{
			m_bv.SpecialCache.SetInt(m_bv.RootObjectHvo, XMLViewsDataCache.ktagActiveColumn, 0);
			PreviewOn = false;
		}

		/// <summary>
		/// Make a ReplaceWithMethod (used in preview and apply click methods for Replace tab).
		/// </summary>
		/// <param name="newCol">obtains the index of the active column.</param>
		/// <returns></returns>
		ReplaceWithMethod MakeReplaceWithMethod(out int newCol)
		{
			newCol = 0;  // in case we fail.
			FieldComboItem fci = m_findReplaceTargetCombo.SelectedItem as FieldComboItem;
			if (fci == null)
			{
				MessageBox.Show(XMLViewsStrings.ksChooseEditTarget);
				return null;
			}
			newCol = fci.ColumnIndex + 1;

			return new ReplaceWithMethod(m_cache, m_bv.SpecialCache, fci.Accessor,
				m_bv.ColumnSpecs[fci.ColumnIndex] as XmlNode, m_pattern, m_tssReplace);
		}

		/// <summary>
		/// Make a ClearMethod (used in preview and apply click methods for Delete tab for fields).
		/// </summary>
		/// <param name="newCol">obtains the index of the active column.</param>
		/// <returns></returns>
		ClearMethod MakeClearMethod(out int newCol)
		{
			newCol = 0;  // in case we fail.
			FieldComboItem fci = m_deleteWhatCombo.SelectedItem as FieldComboItem;
			if (fci == null)
			{
				MessageBox.Show(XMLViewsStrings.ksChooseClearTarget);
				return null;
			}
			newCol = fci.ColumnIndex + 1;

			return new ClearMethod(m_cache, m_bv.SpecialCache, fci.Accessor,
				m_bv.ColumnSpecs[fci.ColumnIndex] as XmlNode);
		}

		/// <summary>
		/// Make a TransduceMethod (used in preview and apply click methods for Transduce tab).
		/// </summary>
		/// <param name="newCol">obtains the index of the active column.</param>
		/// <returns></returns>
		TransduceMethod MakeTransduceMethod(out int newCol)
		{
			newCol = 0;  // in case we fail.
			FieldComboItem fci = m_transduceTargetCombo.SelectedItem as FieldComboItem;
			if (fci == null)
			{
				MessageBox.Show(XMLViewsStrings.ksChooseDestination);
				return null;
			}
			FieldComboItem fciSrc = m_transduceSourceCombo.SelectedItem as FieldComboItem;
			if (fciSrc == null)
			{
				MessageBox.Show(XMLViewsStrings.ksChooseSource);
				return null;
			}
			newCol = fci.ColumnIndex + 1;
//			int srcCol = fciSrc.ColumnIndex + 1;
			string convName = m_transduceProcessorCombo.SelectedItem as string;
			if (convName == null)
			{
				MessageBox.Show(XMLViewsStrings.ksChooseTransducer, XMLViewsStrings.ksSelectProcess);
				return null;
			}

			ECInterfaces.IEncConverters encConverters = null;
			try
			{
				encConverters = new EncConverters();
			}
			catch (Exception e)
			{
				MessageBox.Show(String.Format(XMLViewsStrings.ksCannotAccessEC, e.Message));
				return null;
			}
			ECInterfaces.IEncConverter converter = encConverters[convName];
			return new TransduceMethod(m_cache, m_bv.SpecialCache, fci.Accessor,
				m_bv.ColumnSpecs[fci.ColumnIndex] as XmlNode,
				fciSrc.Accessor, converter,
				m_trdNonEmptyTargetControl.TssSeparator, m_trdNonEmptyTargetControl.NonEmptyMode);
		}

		/// <summary>
		/// Make a BulkCopyMethod (used in preview and apply click methods for Bulk Copy tab).
		/// </summary>
		/// <param name="newCol">obtains the index of the active column.</param>
		/// <returns></returns>
		BulkCopyMethod MakeBulkCopyMethod(out int newCol)
		{
			newCol = 0;  // in case we fail.
			FieldComboItem fci = m_bulkCopyTargetCombo.SelectedItem as FieldComboItem;
			if (fci == null)
			{
				MessageBox.Show(XMLViewsStrings.ksChooseDestination);
				return null;
			}
			FieldComboItem fciSrc = m_bulkCopySourceCombo.SelectedItem as FieldComboItem;
			if (fciSrc == null)
			{
				MessageBox.Show(XMLViewsStrings.ksChooseSource);
				return null;
			}
			newCol = fci.ColumnIndex + 1;
			int srcCol = fciSrc.ColumnIndex + 1;
			if (newCol == srcCol)
			{
				MessageBox.Show(XMLViewsStrings.ksSrcDstMustDiffer);
				return null;
			}

			return new BulkCopyMethod(m_cache, m_bv.SpecialCache, fci.Accessor,
				m_bv.ColumnSpecs[fci.ColumnIndex] as XmlNode,
				fciSrc.Accessor,
				m_bcNonEmptyTargetControl.TssSeparator, m_bcNonEmptyTargetControl.NonEmptyMode);
		}

		/// <summary>
		/// Return the items that should be changed by the current bulk-edit operation. If the flag is set,
		/// (which also means we will really do it), return only the ones the user has checked.
		/// Also in that case, we force a partial sort by homograph number for any lexical entries.
		/// This helps preserve the homograph numbers in the case where the change causes more than one item
		/// to move from one homograph set to another.
		/// </summary>
		/// <param name="fOnlyIfSelected"></param>
		/// <returns></returns>
		internal IEnumerable<int> ItemsToChange(bool fOnlyIfSelected)
		{
			var result = ItemsToChangeSet(fOnlyIfSelected);
			if (!fOnlyIfSelected)
				return result; // Unordered is fine for preview
			var repo = m_cache.ServiceLocator.ObjectRepository;
			var objects = (from hvo in result select repo.GetObject(hvo)).ToList();
			// The objective is to sort them so that we modify anything that affects a homograph number before we modify
			// anything that affects the HN of a homograph of the same entry. Thus, things like LexemeForm that affect
			// the owning entry are sorted by the owning entry.
			// Where entries have the same HN, we sort arbitrarily.
			// This means for example that when a bulk edit merges two groups of homographs, changing both, the resulting
			// order will assign 1 and 2 to the original HN1 items, then 3 and 4 to the original HN2 ones, and so forth.
			// It will be unpredictable which gets 1 and which gets 2 and so forth.
			// If this becomes an issue a smarter sort could be produced. It would be slighly more predictable if we just
			// compared HVO when HN is the same, or we could compare Headwords (before the change).
			objects.Sort((x,y) =>
				{
					var entry1 = x as ILexEntry;
					if (entry1 == null && x is IMoForm)
						entry1 = x.Owner as ILexEntry;
					var entry2 = y as ILexEntry;
					if (entry2 == null && y is IMoForm)
						entry1 = y.Owner as ILexEntry;
					if (entry1 == null)
						return entry2 == null ? 0 : -1; // any entry is larger than a non-entry.
					if (entry2 == null)
						return 1;
					return entry1.HomographNumber.CompareTo(entry2.HomographNumber);
				});
			return (from obj in objects select obj.Hvo).ToList(); // probably counted at least twice and enumerated, so collection is likely more efficient.
		}

		internal Set<int> ItemsToChangeSet(bool fOnlyIfSelected)
		{
			CheckDisposed();

			Set<int> itemsToChange = new Set<int>();
			if (fOnlyIfSelected)
			{
				itemsToChange.AddRange(m_bv.CheckedItems);
			}
			else
			{
				itemsToChange.AddRange(m_bv.AllItems);
			}

			return itemsToChange;
		}

		/// <summary>
		/// Create a default progress state that we can update simply by setting PercentDone
		/// and calling Breath.
		/// Note that most of the methods for doing this are methods of FwXWindow. But we can't use those
		/// because this project can't reference XWorks.
		/// Possibly all of them could be moved to the project that defines StatusBarProgressPanel?
		/// </summary>
		/// <param name="mediator"></param>
		/// <returns></returns>
		ProgressState CreateSimpleProgressState(Mediator mediator)
		{
			if (mediator == null || mediator.PropertyTable == null)
				return new NullProgressState();//not ready to be doing progress bars

			StatusBarProgressPanel panel = mediator.PropertyTable.GetValue("ProgressBar") as StatusBarProgressPanel;
			if (panel == null)
				return new NullProgressState();//not ready to be doing progress bars

			return new ProgressState(panel);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when [enable bulk edit buttons].
		/// </summary>
		/// <param name="value">The value.</param>
		/// ------------------------------------------------------------------------------------
		public void OnEnableBulkEditButtons(object value)
		{
			bool fEnable = (bool)value;
			m_ApplyButton.Enabled = fEnable;
			m_previewButton.Enabled = fEnable;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected void m_ApplyButton_Click(object sender, EventArgs e)
		{

			//we don't want the RecordList to call ReloadList() on an apply, that would prevent the user from seeing the results if the items
			//which the filter might now exclude. So apply any pending row changes and suspend any other row replacement until further notice
			SuspendRecordlistRowChanges();
			using (new ReconstructPreservingBVScrollPosition(m_bv))
			{
				// Both of these need manual disposing.
				// 'using' with the ProgressState fixes LT-4186, since it forces the manual Dispose call,
				// which, in turn, clears the progress panel.
				using (ProgressState state = CreateSimpleProgressState(m_mediator))
				using (new SIL.Utils.WaitCursor(this))
				{
					try
					{
						if (m_operationsTabControl.SelectedTab == m_listChoiceTab)
						{
							if (m_itemIndex >= 0)
							{
								BulkEditItem bei = m_beItems[m_itemIndex];
								bei.BulkEditControl.DoIt(ItemsToChange(true), state);
								FixReplacedItems(bei.BulkEditControl);
							}
						}
						else if (m_operationsTabControl.SelectedTab == m_findReplaceTab)
						{
							int newCol;
							ReplaceWithMethod method = MakeReplaceWithMethod(out newCol);
							if (method == null)
								return;
							method.Doit(ItemsToChange(true), state);
							FixReplacedItems(method);
						}
						else if (m_operationsTabControl.SelectedTab == m_bulkCopyTab)
						{
							int newCol;
							BulkCopyMethod method = MakeBulkCopyMethod(out newCol);
							if (method == null)
								return;

							method.Doit(ItemsToChange(true), state);
							FixReplacedItems(method);
						}
						else if (m_operationsTabControl.SelectedTab == m_transduceTab)
						{
							int newCol;
							TransduceMethod method = MakeTransduceMethod(out newCol);
							if (method == null)
								return;
							method.Doit(ItemsToChange(true), state);
							FixReplacedItems(method);
						}
						else if (m_operationsTabControl.SelectedTab == m_deleteTab)
						{
							if (DeleteRowsItemSelected)
							{
								DeleteSelectedObjects(state); // delete rows
							}
							else if (m_deleteWhatCombo.SelectedItem is TargetFieldItem)
							{
								TargetFieldItem item = m_deleteWhatCombo.SelectedItem as TargetFieldItem;
								int index = item.ColumnIndex;
								BulkEditItem bei = m_beItems[index];
								bei.BulkEditControl.SetClearField();
								bei.BulkEditControl.DoIt(ItemsToChange(true), state);
							}
							else if (m_deleteWhatCombo.SelectedItem is FieldComboItem)
							{
								int newCol;
								ClearMethod method = MakeClearMethod(out newCol);
								if (method == null)
									return;
								method.Doit(ItemsToChange(true), state);
								FixReplacedItems(method);
							}

						}
						else
						{
							MessageBox.Show(this, XMLViewsStrings.ksSorryNoEdit, XMLViewsStrings.ksUnimplFeature);
						}
					}
					finally
					{
					}
					// Turn off the preview (if any).
				// Not used now.
				//m_bv.Cache.VwCacheDaAccessor.CacheIntProp(m_bv.RootObjectHvo, XmlBrowseViewVc.ktagActiveColumn, 0);
				// Use new SDA instead.
				m_bv.SpecialCache.SetInt(m_bv.RootObjectHvo, XMLViewsDataCache.ktagActiveColumn, 0);
				}
				PreviewOn = false;
			} // End using(ReconstructPreservingBVScrollPosition) [Does RootBox.Reconstruct() here.]
		}

		private void SuspendRecordlistRowChanges()
		{
			m_bv.SetListModificationInProgress(true);
		}
		/// <summary>
		/// Only use in BulkEditBar. This method should be private, the coupling with BrowseViewer is too high if
		/// the bulkedit code is refactored out of BrowseViewer make this private.
		/// </summary>
		internal void ResumeRecordListRowChanges()
		{
			m_bv.SetListModificationInProgress(false);
		}

		private void FixReplacedItems(object doItObject)
		{
			IGetReplacedObjects gro = doItObject as IGetReplacedObjects;
			if (gro != null)
			{
				Dictionary<int, int> replacedObjects = gro.ReplacedObjects;
				if (replacedObjects != null && replacedObjects.Count != 0)
				{
					m_bv.FixReplacedItems(replacedObjects);
				}
			}
		}

		/// <summary>
		/// Delete ALL the checked objects!!
		/// </summary>
		private void DeleteSelectedObjects(ProgressState state)
		{
			Set<int> idsToDelete = new Set<int>();
			UpdateCurrentGhostParentHelper(); // needed for code below.
			foreach (int hvo in ItemsToChange(true))
			{
				if (m_bv.SpecialCache.get_IntProp(hvo, XMLViewsDataCache.ktagItemEnabled) != 0)
				{
					if (VerifyRowDeleteAllowable(hvo))
					{
						//allow deletion for the class we expect to be bulk editing.
						int hvoToDelete = 0;
						if (DomainObjectServices.IsSameOrSubclassOf(m_cache.DomainDataByFlid.MetaDataCache,
							m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo).ClassID,
							m_expectedListItemsClassId))
							hvoToDelete = hvo;
						else if (m_ghostParentHelper != null)
							hvoToDelete = m_ghostParentHelper.GetOwnerOfTargetProperty(hvo);
						if (hvoToDelete != 0)
							idsToDelete.Add(hvoToDelete);
					}
				}
			}
			bool fUndo;
			if (!CheckMultiDeleteConditionsAndReport(idsToDelete, out fUndo))
				return;
			try
			{
				state.PercentDone = 10;
				state.Breath();
				m_bv.SetListModificationInProgress(true);
				int total = idsToDelete.Count;
				int interval = Math.Min(100, Math.Max(idsToDelete.Count / 90, 1));
				int i = 0;
				UndoableUnitOfWorkHelper.Do(XMLViewsStrings.ksUndoBulkDelete, XMLViewsStrings.ksRedoBulkDelete,
											m_cache.ActionHandlerAccessor,
											() =>
												{
													foreach (int hvo in idsToDelete)
													{
														if ((i + 1) % interval == 0)
														{
															state.PercentDone = i * 90 / idsToDelete.Count + 10;
															state.Breath();
														}
														i++;
														ICmObject obj;
														if (m_cache.ServiceLocator.ObjectRepository.TryGetObject(hvo, out obj))
															m_bv.SpecialCache.DeleteObj(hvo);
													}
													if (m_expectedListItemsClassId == LexEntryTags.kClassId ||
														m_expectedListItemsClassId == LexSenseTags.kClassId)
													{
#if WANTPPORT
														CmObject.DeleteOrphanedObjects(m_cache, fUndo, state);
#endif
													}
												});
				m_bv.SetListModificationInProgress(false);
				ResumeRecordListRowChanges(); // need to show the updated list of rows!
				state.PercentDone = 100;
				state.Breath();
			}
			finally
			{
				// need to recompute what needs to be enabled after the deletion.
				m_items.Clear();
			}
		}

		/// <summary>
		/// Check whether deleting these objects will be undoable.  In any case, pops up a message
		/// asking the user whether to proceed. The type of message depends somewhat on the situation.
		/// </summary>
		/// <returns>true, if okay to continue with delete</returns>
		private bool CheckMultiDeleteConditionsAndReport(Set<int> idsToDelete, out bool fUndo)
		{
			int cOrphans = 0;
			if (m_expectedListItemsClassId == LexEntryTags.kClassId ||
				m_expectedListItemsClassId == LexSenseTags.kClassId)
			{
#if WANTPPORT
				cOrphans = CmObject.CountOrphanedObjects(m_cache);
#endif
			}
			fUndo = cOrphans == 0;
			if (fUndo)
			{
#if WANTPPORT
				int iNextGroup = 0;
				DbOps.MakePartialIdList(ref iNextGroup, idsToDelete.ToArray());
				fUndo = iNextGroup == idsToDelete.Count;
#endif
			}
			string sMsg = XMLViewsStrings.ksConfirmDeleteMultiMsg;
			string sTitle = XMLViewsStrings.ksConfirmDeleteMulti;
			if (!fUndo)
			{
				sMsg = XMLViewsStrings.ksCannotUndoTooManyDeleted;
			}
			return MessageBox.Show(this, sMsg, sTitle, MessageBoxButtons.OKCancel, MessageBoxIcon.Warning)
					== DialogResult.OK;
		}

		private bool VerifyRowDeleteAllowable(int hvo)
		{
			if (!String.IsNullOrEmpty(m_sBulkDeleteIfZero))
			{
				var co = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
				if (m_piBulkDeleteIfZero == null)
					m_piBulkDeleteIfZero = co.GetType().GetProperty(m_sBulkDeleteIfZero);
				if (m_piBulkDeleteIfZero != null)
				{
					object o = m_piBulkDeleteIfZero.GetValue(co, null);
					if (o.GetType() == typeof(int) && (int)o != 0)
						return false;
				}
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an image of the blue arrow we use to separate actual cell contents from
		/// bulk edit preview.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Image PreviewArrow
		{
			get
			{
				CheckDisposed();
				return m_imageList16x14.Images[0];
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the preview arrow static.
		/// </summary>
		/// <value>The preview arrow static.</value>
		/// ------------------------------------------------------------------------------------
		public static Image PreviewArrowStatic
		{
			get
			{
				using (ImageList list = new ImageList())
				{
					System.ComponentModel.ComponentResourceManager resources =
					new System.ComponentModel.ComponentResourceManager(typeof(BulkEditBar));
					list.ImageStream = (System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_imageList16x14.ImageStream"));
					list.TransparentColor = Color.Magenta;
					list.Images.SetKeyName(0, "");
					return list.Images[0];
				}
			}
		}

		private void m_closeButton_Click(object sender, EventArgs e)
		{
			m_bv.HideBulkEdit();

		}

		private void m_clickCopySepBox_GotFocus(object sender, EventArgs e)
		{
			m_clickCopySepBox.SelectAll();
		}

		private void InitFindReplaceTab()
		{
			InitStringCombo(m_findReplaceTargetCombo, false);
			EnablePreviewApplyForFindReplace();
		}

		// Initialize the delete tab.
		private void InitDeleteTab()
		{
			m_deleteWhatCombo.SuspendLayout();
			m_deleteWhatCombo.ClearItems();
			// Add the string related fields
			AddStringFieldItemsToCombo(m_deleteWhatCombo, null, false);
			// Add support for deleting "rows" only for the classes
			// that have associated columns installed (LT-9128).
			Set<int> targetClassesNeeded = new Set<int>();
			// Always allow deleting the primary row (e.g. Entries)
			targetClassesNeeded.Add(m_bulkEditListItemsClasses[0]);
			// Go through each of the column-deletable string fields, and add rows to delete.
			foreach (FieldComboItem fci in m_deleteWhatCombo.Items)
			{
				int targetClass = GetExpectedListItemsClassFromSelectedItem(fci);
				targetClassesNeeded.Add(targetClass);
			}
			int icol = -1; // will increment at start of loop.
			// Add all the List related fields
			foreach (BulkEditItem bei in m_beItems)
			{
				icol++;
				if (bei != null && bei.BulkEditControl.CanClearField)
				{
					XmlNode colSpec = m_bv.ColumnSpecs[icol] as XmlNode;
					string label = GetColumnLabel(m_mediator, colSpec);
					TargetFieldItem tfi = null;
					try
					{
						tfi = new TargetFieldItem(label, icol);
						// still want to allow deleting item rows, even if column is not deletable.
						int targetClass = GetExpectedListItemsClassFromSelectedItem(tfi);
						targetClassesNeeded.Add(targetClass);
						bool allowBulkDelete = XmlUtils.GetOptionalBooleanAttributeValue(colSpec, "bulkDelete", true);
						if (!allowBulkDelete)
							continue;
						m_deleteWhatCombo.Items.Add(tfi);
						tfi = null; // well be disposed by m_deleteWhatCombo
					}
					finally
					{
						if (tfi != null)
							tfi.Dispose();
					}
				}
			}
			foreach (ListClassTargetFieldItem rootClassOption in ListItemsClassesInfo(targetClassesNeeded))
				m_deleteWhatCombo.Items.Add(rootClassOption);

			// Default to deleting rows if that's all we have in the combo box list.
			m_deleteWhatCombo.ResumeLayout();
			bool enabled = m_deleteTab.Enabled;
			m_ApplyButton.Enabled = enabled;
		}

		private void EnablePreviewApplyForFindReplace()
		{
			bool enabled = CanFindReplace();
			m_ApplyButton.Enabled = enabled;
			m_previewButton.Enabled = enabled;
		}

		private bool CanFindReplace()
		{
			if (!m_findReplaceTab.Enabled)
				return false;
			if (m_pattern == null)
				return false;
			// If matching writing systems it doesn't matter if find and replace are both empty.
			// That means match anything in the writing system.
			if (m_pattern.MatchOldWritingSystem)
				return true;
			// Otherwise we can do it unless both are empty.
			ITsString tssPattern = m_pattern.Pattern;
			if ((tssPattern == null || tssPattern.Length == 0) &&
				(m_tssReplace == null || m_tssReplace.Length == 0))
				return false;
			return true;
		}

		private void InitBulkCopyTab()
		{
			InitStringCombo(m_bulkCopySourceCombo, true);
			InitStringCombo(m_bulkCopyTargetCombo, false);
			EnablePreviewApplyForBulkCopy();
		}

		/// <summary>
		/// Set the enable state for the specified tab, based on the number of possible target columns
		/// (if zero, disable) and the setting specified in the enableBulkEditTabsNode in the specified attribute.
		/// Enhance JohnT: possibly there is no reason for the enableBulkEditTabsNode now that we're figuring
		/// out whether to enable based on the number of target columns?
		/// </summary>
		/// <param name="page"></param>
		/// <param name="targetColumnCount"></param>
		/// <param name="enableAttrName"></param>
		private void SetTabEnabled(TabPage page, int targetColumnCount, string enableAttrName)
		{
			if (m_enableBulkEditTabsNode != null
				&& !XmlUtils.GetOptionalBooleanAttributeValue(m_enableBulkEditTabsNode, enableAttrName, true))
			{
				page.Enabled = false;
			}
			else
			{
				page.Enabled = targetColumnCount > 0;
			}

		}
		/// <summary>
		/// Initialize the List Choice tab.
		/// </summary>
		private void InitListChoiceTab()
		{
			populateListChoiceTargetCombo();
			SetEnabledForListChoiceTab();
		}

		private void SetEnabledForListChoiceTab()
		{
			SetTabEnabled(m_listChoiceTab, m_listChoiceTargetCombo.Items.Count, "enableBEListChoice");
		}

		private void EnablePreviewApplyForBulkCopy()
		{
			bool enabled = m_bulkCopyTab.Enabled;
			// The following would also make sense. But I think it is better to enable the buttons
			// and put up an explanation if they can't be used. If we change it to the following,
			// we need event handlers to get this called when the relevant indexes change.
			//			bool enabled = m_bulkCopySourceCombo.SelectedIndex >= 0
			//				&& m_bulkCopyTargetCombo.SelectedIndex >= 0
			//				&& m_bulkCopyTargetCombo.SelectedIndex != m_bulkCopySourceCombo.SelectedIndex;
			m_ApplyButton.Enabled = enabled;
			m_previewButton.Enabled = enabled;
		}

		/// <summary>
		/// Initialize the Click Copy tab.
		/// </summary>
		private void InitClickCopyTab()
		{
			InitStringCombo(m_clickCopyTargetCombo, false);
			m_bv.BrowseView.EditingHelper.ReadOnlyTextCursor = Cursors.Hand;
			XmlBrowseView xbv = m_bv.BrowseView as XmlBrowseView;
			// The -= helps make sure we never have more than one. Apparently multiple
			// copies of the same event handler are possible with derived event handlers.
			xbv.ClickCopy -= new ClickCopyEventHandler(xbv_ClickCopy);
			xbv.ClickCopy += new ClickCopyEventHandler(xbv_ClickCopy);
			m_clickCopyTargetCombo_SelectedIndexChanged(this, new System.EventArgs());

			bool enabled = m_clickCopyTab.Enabled;
			FieldComboItem selectedItem = m_clickCopyTargetCombo.SelectedItem as FieldComboItem;
			if (selectedItem == null)
			{
				// The user has removed all possible targets from the browse view columns.  Choose
				// a plausible writing system code for the text box, and disable the preview and
				// apply buttons since there's nothing for them to work with.  (See LT-5129.)
				m_clickCopySepBox.WritingSystemCode = m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle;
				enabled = false;
			}
			else
			{
				m_clickCopySepBox.WritingSystemCode = selectedItem.Accessor.WritingSystem;
				// Looks like a no-op, but will cause it to be in the correct writing system.
				m_clickCopySepBox.Text = m_clickCopySepBox.Text;
			}
			// Note: Don't access BrowseView.SelectedObject here,
			// since it may be invalid/out-of-range (thus crash),
			// pending OnRecordNavigation changes from ReloadList
			// triggered by the m_clickCopyTargetCombo_SelectedIndexChanged().
			//m_hvoSelected = m_bv.BrowseView.SelectedObject;

			m_ApplyButton.Enabled = enabled;
			m_previewButton.Enabled = enabled;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the transduce tab.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitTransduce()
		{
			InitConverterCombo();
			// Load the source and destination combos.
			InitStringCombo(m_transduceSourceCombo, true);
			InitStringCombo(m_transduceTargetCombo, false);
			EnablePreviewApplyForTransduce();
		}

		private void EnablePreviewApplyForTransduce()
		{
			bool enabled = m_transduceTab.Enabled;
			// The following would also make sense. But I think it is better to enable the buttons
			// and put up an explanation if they can't be used. If we change it to the following,
			// we need event handlers to get this called when the relevant indexes change.
//			bool enabled = m_transduceProcessorCombo.SelectedIndex >= 0
//				&& m_transduceSourceCombo.SelectedIndex >= 0
//				&& m_transduceTargetCombo.SelectedIndex >= 0;
			m_ApplyButton.Enabled = enabled;
			m_previewButton.Enabled = enabled;
		}

		/// <summary>
		/// Load the available (relevant) converters. Adapted from
		/// WritingSystemPropertiesDialog.LoadAvailableConverters.
		/// </summary>
		private void InitConverterCombo()
		{
			try
			{
				string selectedItem = m_transduceProcessorCombo.SelectedItem as string;
				ECInterfaces.IEncConverters encConverters = new EncConverters();
				m_transduceProcessorCombo.ClearItems();
				foreach (string convName in encConverters.Keys)
				{
					ECInterfaces.IEncConverter conv = encConverters[convName];
					// Only Unicode-to-Unicode converters are relevant.
					if (conv.ConversionType == ECInterfaces.ConvType.Unicode_to_Unicode
						|| conv.ConversionType == ECInterfaces.ConvType.Unicode_to_from_Unicode)
						m_transduceProcessorCombo.Items.Add(convName);
				}
				if (!String.IsNullOrEmpty(selectedItem))
					m_transduceProcessorCombo.SelectedItem = selectedItem; // preserve selection if possible
				else if (m_transduceProcessorCombo.Items.Count > 0)
					m_transduceProcessorCombo.SelectedIndex = 0;
			}
			catch (Exception e)
			{
				MessageBox.Show(String.Format(XMLViewsStrings.ksCannotAccessEC, e.Message));
				return;
			}
		}

		// The currently active bulk edit item (if any).
		internal BulkEditItem CurrentItem
		{
			get
			{
				if (m_beItems == null)
					return null;
				if(m_itemIndex < 0 || m_itemIndex >= m_beItems.Length)
					return null;
				return m_beItems[m_itemIndex];
			}
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Enables the preview apply for list choice.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void EnablePreviewApplyForListChoice()
		{
			if (m_beItems == null ||
				m_itemIndex < 0 || m_itemIndex >= m_beItems.Length ||
				m_listChoiceControl == null)
			{
				// Things haven't been initialized enough yet.  Disable the buttons and quit.
				m_ApplyButton.Enabled = false;
				m_previewButton.Enabled = false;
				return;
			}
			// The assumption is that this method is only called when switching to the List Choice tab
			// therefore if the selected item in m_fieldCombo is ComplexListChooserBEditControl
			// we need to disable these buttons when in this state.
			if (m_beItems[m_itemIndex] != null &&
				m_beItems[m_itemIndex].BulkEditControl is ComplexListChooserBEditControl)
			{
				if ((m_beItems[m_itemIndex].BulkEditControl as ComplexListChooserBEditControl).ChosenObjects.Count() > 0)
				{
					m_ApplyButton.Enabled = true;
					m_previewButton.Enabled = true;
				}
				else
				{
					m_ApplyButton.Enabled = false;
					m_previewButton.Enabled = false;
				}
				// we want to return because the Text field has "Choose..."
				// which is not something we want to enable Apply/Preview to.
				return;
			}

			if (!m_listChoiceControl.Text.Equals(String.Empty))
			{
				m_ApplyButton.Enabled = true;
				m_previewButton.Enabled = true;
				return;
			}

			m_ApplyButton.Enabled = false;
			m_previewButton.Enabled = false;
		}

		/// <summary>
		///
		/// </summary>
		protected enum BulkEditBarTabs
		{
			/// <summary>  </summary>
			ListChoice = 0,
			/// <summary>  </summary>
			BulkCopy,
			/// <summary>  </summary>
			ClickCopy,
			/// <summary>  </summary>
			Process,
			/// <summary>  </summary>
			BulkReplace,
			/// <summary>  </summary>
			Delete,
		}

		/// <summary>
		/// This is the base class that knows common settings used in BulkEdit tabs.
		/// Used to persist those values.
		/// </summary>
		public class BulkEditTabPageSettings
		{
			#region Member variables
			/// <summary> the bulkEditBar we're getting or settings our values</summary>
			protected BulkEditBar m_bulkEditBar = null;

			string m_bulkEditBarTabName = "";
			string m_targetFieldName = "";

			#endregion Member variables

			/// <summary>
			///
			/// </summary>
			public BulkEditTabPageSettings()
			{
			}

			/// <summary>
			/// The tab we expect this class to help load and store its settings.
			/// </summary>
			protected virtual int ExpectedTab
			{
				get
				{
					// The base class, can be used with whatever tab.
					// subclasses should override this.
					return m_bulkEditBar.m_operationsTabControl.SelectedIndex;
				}
			}

			/// <summary>
			/// make sure bulkEditBar is in the expected tab state.
			/// </summary>
			protected virtual void CheckExpectedTab()
			{
				if (m_bulkEditBar == null)
					throw new ApplicationException("Expected settings to have initialized m_bulkEditBar.");
				if (m_bulkEditBar.m_operationsTabControl.SelectedIndex != (int)ExpectedTab)
					throw new ApplicationException("Expected bulkEditBar to be on tab " + (BulkEditBarTabs)ExpectedTab);
			}

			private bool InExpectedTab()
			{
				return m_bulkEditBar != null && m_bulkEditBar.m_operationsTabControl.SelectedIndex == (int)ExpectedTab;
			}

			internal bool AreLoaded
			{
				get
				{
					return this.GetType().Name != typeof(BulkEditTabPageSettings).Name &&
						m_bulkEditBar != null &&
						TabPageName.Length > 0;
				}
			}

			#region BulkEditBar helper methods

			/// <summary>
			/// Create BulkEditBarTabPage settings for the current tab,
			/// and save them to the property table.
			/// (only effective after initialization (i.e. m_setupOrRestoredBulkEditBarTab)
			/// To restore the settings, use TrySwitchToLastSavedTab() and/or
			/// followed by InitializeSelectedTab().
			/// </summary>
			/// <param name="bulkEditBar"></param>
			/// <returns></returns>
			static internal BulkEditTabPageSettings CaptureSettingsForCurrentTab(BulkEditBar bulkEditBar)
			{
				// don't capture bulk edit bar settings until we're finished with initialization.
				if (!bulkEditBar.m_setupOrRestoredBulkEditBarTab)
					return null;
				BulkEditTabPageSettings tabPageSettings = GetNewSettingsForSelectedTab(bulkEditBar);
				tabPageSettings.SaveSettings(bulkEditBar);
				return tabPageSettings;
			}

			static internal BulkEditTabPageSettings GetNewSettingsForSelectedTab(BulkEditBar bulkEditBar)
			{
				BulkEditTabPageSettings tabPageSettings = null;
				switch (bulkEditBar.m_operationsTabControl.SelectedIndex)
				{
					default:
						// by default, just save basic tab info.
						tabPageSettings = new BulkEditTabPageSettings();
						break;
					case (int)BulkEditBarTabs.ListChoice: // list
						tabPageSettings = new ListChoiceTabPageSettings();
						break;
					case (int)BulkEditBarTabs.BulkCopy: // bulk copy
						tabPageSettings = new BulkCopyTabPageSettings();
						break;
					case (int)BulkEditBarTabs.ClickCopy: // click copy
						tabPageSettings = new ClickCopyTabPageSettings();
						break;
					case (int)BulkEditBarTabs.Process: // transduce
						tabPageSettings = new ProcessTabPageSettings();
						break;
					case (int)BulkEditBarTabs.BulkReplace: // find/replace
						tabPageSettings = new BulkReplaceTabPageSettings();
						break;
					case (int)BulkEditBarTabs.Delete: // Delete.
						tabPageSettings = new DeleteTabPageSettings();
						break;
				}
				tabPageSettings.m_bulkEditBar = bulkEditBar;
				return tabPageSettings;
			}

			/// <summary>
			/// Restore last visited BulkEditBar tab index.
			/// After BulkEditBar finishes initializing its state and controls in that tab,
			/// finish restoring the settings in that tab with InitializeSelectedTab()
			/// </summary>
			/// <param name="bulkEditBar"></param>
			/// <returns></returns>
			static internal bool TrySwitchToLastSavedTab(BulkEditBar bulkEditBar)
			{
				BulkEditTabPageSettings settings;
				// first try to deserialize stored settings.
				settings = BulkEditTabPageSettings.DeserializeLastTabPageSettings(bulkEditBar);
				// get the name of the tab. if we can't get this, no point in continuing to use the settings,
				// because all other settings depend upon the tab.
				if (settings.AreLoaded)
				{
					bool fOk = true;
					// try switching to saved tab.
					try
					{
						BulkEditBarTabs tab = (BulkEditBarTabs)Enum.Parse(typeof(BulkEditBarTabs), settings.TabPageName);
						bulkEditBar.m_operationsTabControl.SelectedIndex = (int)tab;
					}
					catch
					{
						// something went wrong trying to restore tab, so assume we didn't switch to a saved one.
						fOk = false;
					}
					return fOk;
				}
				return false;
			}

			/// <summary>
			/// Try to restore settings for selected tab, otherwise use defaults.
			/// </summary>
			/// <param name="bulkEditBar"></param>
			/// <returns></returns>
			static internal void InitializeSelectedTab(BulkEditBar bulkEditBar)
			{
				BulkEditTabPageSettings tabPageSettings;
				if (TryGetSettingsForCurrentTabPage(bulkEditBar, out tabPageSettings))
				{
					// now that we've loaded/setup a tab, restore the settings for that tab.
					try
					{
						tabPageSettings.SetupBulkEditBarTab(bulkEditBar);
					}
					catch
					{
						// oh well, we tried, just continue with what we could setup, if anything.
					}
				}
				else
				{
					// we didn't restore saved settings, but we may want to initialize defaults instead.
					tabPageSettings = GetNewSettingsForSelectedTab(bulkEditBar);
					tabPageSettings.SetupBulkEditBarTab(bulkEditBar);
				}

				tabPageSettings.SetupApplyPreviewButtons();
			}

			private static bool TryGetSettingsForCurrentTabPage(BulkEditBar bulkEditBar, out BulkEditTabPageSettings tabPageSettings)
			{
				string currentTabSettingsKey = BuildCurrentTabSettingsKey(bulkEditBar);
				tabPageSettings = DeserializeTabPageSettings(bulkEditBar, currentTabSettingsKey);
				return tabPageSettings.AreLoaded;
			}

			/// <summary>
			/// Check that we've changed to BulkEditBar to ExpectedTab,
			/// and then set BulkEditBar to those tab settings
			/// </summary>
			protected virtual void SetupBulkEditBarTab(BulkEditBar bulkEditBar)
			{
				if (m_bulkEditBar == null || !this.AreLoaded)
					return;
				CheckExpectedTab();
				SetTargetCombo();
				// first load target field name. other settings may depend upon this.
				SetTargetField();
			}

			/// <summary>
			/// Update Preview/Clear and Apply Button states.
			/// </summary>
			protected virtual void SetupApplyPreviewButtons()
			{
				m_bulkEditBar.m_ApplyButton.Visible = true;
				m_bulkEditBar.m_previewButton.Visible = true;
				m_bulkEditBar.m_bv.BrowseView.EditingHelper.DefaultCursor = null;
			}

			/// <summary>
			///
			/// </summary>
			protected virtual void SetTargetField()
			{
				if (m_bulkEditBar.m_currentTargetCombo != null)
				{
					m_bulkEditBar.m_currentTargetCombo.Text = this.TargetFieldName;
					if (m_bulkEditBar.m_currentTargetCombo.SelectedIndex == -1)
					{
						// by default select the first item
						if (m_bulkEditBar.m_currentTargetCombo.Items.Count > 0)
							m_bulkEditBar.m_currentTargetCombo.SelectedIndex = 0;
					}
					if (!m_bulkEditBar.m_setupOrRestoredBulkEditBarTab)
					{
						// if we haven't already been setup, we should explicitly trigger to
						// say our target field has changed, in case the RecordList needs to
						// reload accordingly.
						InvokeTargetComboSelectedIndexChanged();
					}
				}
			}

			private void SetTargetCombo()
			{
				m_bulkEditBar.m_currentTargetCombo = this.TargetComboForTab;
			}

			/// <summary>
			/// the target combo for a particular tab page.
			/// </summary>
			protected virtual FwOverrideComboBox TargetComboForTab
			{
				get { return null; }
			}

			/// <summary>
			/// this is a hack that explictly triggers the currentTargetCombo.SelectedIndexChange delegates
			/// during initialization, since they do not fire automatically until after everything is setup.
			/// </summary>
			protected virtual void InvokeTargetComboSelectedIndexChanged()
			{
				// EricP. Couldn't figure out how to do this by reflection generally.
				// so override in each tab settings.
			}

			/// <summary>
			/// Construct the property table key based upon the tool using this BulkEditBar.
			/// </summary>
			/// <param name="bulkEditBar"></param>
			/// <returns></returns>
			private static string BuildLastTabSettingsKey(BulkEditBar bulkEditBar)
			{
#pragma warning disable 219
				Mediator mediator = bulkEditBar.m_mediator;
#pragma warning restore 219
				string toolId = GetBulkEditBarToolId(bulkEditBar);
				string property = String.Format("{0}_LastTabPageSettings", toolId);
				return property;
			}

			private static string BuildCurrentTabSettingsKey(BulkEditBar bulkEditBar)
			{
#pragma warning disable 219
				Mediator mediator = bulkEditBar.m_mediator;
#pragma warning restore 219
				string toolId = GetBulkEditBarToolId(bulkEditBar);
				string property = String.Format("{0}_{1}_TabPageSettings", toolId, GetCurrentTabPageName(bulkEditBar));
				return property;
			}


			/// <summary>
			///
			/// </summary>
			/// <param name="bulkEditBar"></param>
			/// <returns></returns>
			internal static string GetBulkEditBarToolId(BulkEditBar bulkEditBar)
			{
				XmlNode configurationNode = bulkEditBar.m_configurationNode;
				return XWindow.GetToolIdFromControlConfiguration(configurationNode);
			}

			/// <summary>
			/// Serialize the settings for a bulk edit bar tab, and store it in the property table.
			/// </summary>
			/// <param name="bulkEditBar"></param>
			protected virtual void SaveSettings(BulkEditBar bulkEditBar)
			{
				m_bulkEditBar = bulkEditBar;
				string settingsXml = SerializeSettings();
				Mediator mediator = bulkEditBar.m_mediator;
				// first store current tab settings in the property table.
				string currentTabSettingsKey = BuildCurrentTabSettingsKey(bulkEditBar);
				mediator.PropertyTable.SetProperty(currentTabSettingsKey, settingsXml, false, PropertyTable.SettingsGroup.LocalSettings);
				mediator.PropertyTable.SetPropertyPersistence(currentTabSettingsKey, true);
				// next store the *key* to the current tab settings in the property table.
				string lastTabSettingsKey = BuildLastTabSettingsKey(bulkEditBar);
				mediator.PropertyTable.SetProperty(lastTabSettingsKey, currentTabSettingsKey, false, PropertyTable.SettingsGroup.LocalSettings);
				mediator.PropertyTable.SetPropertyPersistence(lastTabSettingsKey, true);
			}

			private string SerializeSettings()
			{
				return XmlUtils.SerializeObjectToXmlString(this);
			}

			/// <summary>
			/// factory returning a tab settings object, if we found them in the property table.
			/// </summary>
			/// <param name="bulkEditBar"></param>
			/// <returns></returns>
			static private BulkEditTabPageSettings DeserializeLastTabPageSettings(BulkEditBar bulkEditBar)
			{
				Mediator mediator = bulkEditBar.m_mediator;
				string lastTabSettingsKey = BuildLastTabSettingsKey(bulkEditBar);
				// the value of LastTabSettings is the key to the tab settings in the property table.
				string tabSettingsKey = mediator.PropertyTable.GetStringProperty(lastTabSettingsKey, "", PropertyTable.SettingsGroup.LocalSettings);
				return DeserializeTabPageSettings(bulkEditBar, tabSettingsKey);
			}

			static private BulkEditTabPageSettings DeserializeTabPageSettings(BulkEditBar bulkEditBar, string tabSettingsKey)
			{
				Mediator mediator = bulkEditBar.m_mediator;
				string settingsXml = "";
				if (tabSettingsKey.Length > 0)
					settingsXml = mediator.PropertyTable.GetStringProperty(tabSettingsKey, "", PropertyTable.SettingsGroup.LocalSettings);
				BulkEditTabPageSettings restoredTabPageSettings = null;
				if (settingsXml.Length > 0)
				{
					// figure out type/class of object to deserialize from xml data.
					XmlDocument doc = new XmlDocument();
					doc.LoadXml(settingsXml);
					string className = doc.DocumentElement.Name;
					// get the type from the xml itself.
					Assembly assembly = Assembly.GetExecutingAssembly();
					// if we can find an existing class/type, we can try to deserialize to it.
					BulkEditTabPageSettings basicTabPageSettings = new BulkEditTabPageSettings();
					Type pgSettingsType = basicTabPageSettings.GetType();
					string baseClassTypeName = pgSettingsType.FullName.Split(new char[] { '+' })[0];
					Type targetType = assembly.GetType(baseClassTypeName + "+" + className, false);

					// deserialize
					restoredTabPageSettings = (BulkEditTabPageSettings)
						XmlUtils.DeserializeXmlString(settingsXml, targetType);
				}
				if (restoredTabPageSettings == null)
					restoredTabPageSettings = new BulkEditTabPageSettings();
				restoredTabPageSettings.m_bulkEditBar = bulkEditBar;
				return restoredTabPageSettings;
			}

			/// <summary>
			///
			/// </summary>
			/// <returns></returns>
			protected bool CanLoadFromBulkEditBar()
			{
				return InExpectedTab();
			}

			/// <summary>
			/// after deserializing, determine if the target combo was able to get
			/// set to the persisted value.
			/// </summary>
			/// <returns></returns>
			protected bool HasExpectedTargetSelected()
			{
				return m_bulkEditBar.m_currentTargetCombo.Text == this.TargetFieldName;
			}


			#endregion BulkEditBar helper methods


			#region Tab properties to serialize

			/// <summary>
			/// The current tab page. Typically this is used to set the bulk edit bar into the current
			/// tab before SetupBulkEditBarTab adjusts settings for that tab.
			/// </summary>
			public string TabPageName
			{
				get
				{
					if (String.IsNullOrEmpty(m_bulkEditBarTabName) && m_bulkEditBar != null)
					{
						string tabPageName = GetCurrentTabPageName(m_bulkEditBar);
						m_bulkEditBarTabName = tabPageName;
					}
					if (m_bulkEditBarTabName == null)
						m_bulkEditBarTabName = String.Empty;
					return m_bulkEditBarTabName;
				}
				set
				{
					m_bulkEditBarTabName = value;
				}
			}

			/// <summary>
			///
			/// </summary>
			/// <param name="bulkEditBar"></param>
			/// <returns></returns>
			protected static string GetCurrentTabPageName(BulkEditBar bulkEditBar)
			{
				int selectedTabIndex = bulkEditBar.m_operationsTabControl.SelectedIndex;
				BulkEditBarTabs tab = (BulkEditBarTabs)Enum.Parse(typeof(BulkEditBarTabs), selectedTabIndex.ToString());
				return tab.ToString();
			}


			/// <summary>
			/// The name of item selected in the Target Combo box.
			/// </summary>
			public string TargetFieldName
			{
				get
				{
					if (String.IsNullOrEmpty(m_targetFieldName) &&
						CanLoadFromBulkEditBar() &&
						m_bulkEditBar.m_currentTargetCombo != null)
					{
						m_targetFieldName = m_bulkEditBar.m_currentTargetCombo.Text;
					}
					if (m_targetFieldName == null)
						m_targetFieldName = String.Empty;
					return m_targetFieldName;
				}
				set
				{
					m_targetFieldName = value;
				}
			}

			#endregion Tab properties to serialize

		}   //END of class BulkEditTabPageSettings

		/// <summary>
		///
		/// </summary>
		public class ListChoiceTabPageSettings : BulkEditTabPageSettings
		{

			string m_changeTo = "";

			/// <summary>
			///
			/// </summary>
			protected override int ExpectedTab
			{
				get { return (int)BulkEditBarTabs.ListChoice; }
			}

			/// <summary>
			///
			/// </summary>
			public string ChangeTo
			{
				get
				{
					if (String.IsNullOrEmpty(m_changeTo) &&
						CanLoadFromBulkEditBar() &&
						m_bulkEditBar.m_listChoiceControl != null)
					{
						m_changeTo = m_bulkEditBar.m_listChoiceControl.Text;
					}
					if (m_changeTo == null)
						m_changeTo = String.Empty;
					return m_changeTo;
				}
				set { m_changeTo = value; }
			}

			/// <summary>
			///
			/// </summary>
			protected override void SetupBulkEditBarTab(BulkEditBar bulkEditBar)
			{
				// first initialize the controls, since otherwise, we overwrite our selection.
				// now we can setup the target field
				m_bulkEditBar.InitListChoiceTab();

				base.SetupBulkEditBarTab(bulkEditBar);
				if (m_bulkEditBar.m_listChoiceControl != null)
				{
					if (HasExpectedTargetSelected())
						m_bulkEditBar.m_listChoiceControl.Text = this.ChangeTo;
					if (m_bulkEditBar.CurrentItem.BulkEditControl is ITextChangedNotification)
						(m_bulkEditBar.CurrentItem.BulkEditControl as ITextChangedNotification).ControlTextChanged();
					else
					{
						// couldn't restore target selection, so revert to defaults.
							// (LT-9940 default is ChangeTo, not "")
							m_bulkEditBar.m_listChoiceControl.Text = this.ChangeTo;
					}
				}
				else
				{
					// at least show dummy control.
					m_bulkEditBar.m_listChoiceChangeToCombo.Visible = true;
				}
			}

			/// <summary>
			/// the target combo for a particular tab page.
			/// </summary>
			protected override FwOverrideComboBox TargetComboForTab
			{
				get { return m_bulkEditBar.m_listChoiceTargetCombo; }
			}

			/// <summary>
			/// this is a hack that explictly triggers the currentTargetCombo.SelectedIndexChange delegates
			/// during initialization, since they do not fire automatically until after everything is setup.
			/// </summary>
			protected override void InvokeTargetComboSelectedIndexChanged()
			{
				m_bulkEditBar.m_listChoiceTargetCombo_SelectedIndexChanged(this, EventArgs.Empty);
			}

			/// <summary>
			/// Update Preview/Clear and Apply Button states.
			/// </summary>
			protected override void SetupApplyPreviewButtons()
			{
				base.SetupApplyPreviewButtons();
				m_bulkEditBar.EnablePreviewApplyForListChoice();
			}
		}

		/// <summary>
		///
		/// </summary>
		public class BulkCopyTabPageSettings : BulkEditTabPageSettings
		{
			string m_sourceField = "";
			string m_nonEmptyTargetMode = "";
			string m_nonEmptyTargetSeparator = "";

			/// <summary>
			///
			/// </summary>
			protected override int ExpectedTab
			{
				get { return (int)BulkEditBarTabs.BulkCopy; }
			}

			/// <summary>
			///
			/// </summary>
			public string SourceField
			{
				get
				{
					if (String.IsNullOrEmpty(m_sourceField) &&
						CanLoadFromBulkEditBar() &&
						SourceCombo != null)
					{
						m_sourceField = SourceCombo.Text;
					}
					if (m_sourceField == null)
						m_sourceField = String.Empty;
					return m_sourceField;
				}
				set { m_sourceField = value; }
			}

			/// <summary>
			///
			/// </summary>
			protected virtual FwOverrideComboBox SourceCombo
			{
				get { return m_bulkEditBar.m_bulkCopySourceCombo; }
			}

			/// <summary>
			///
			/// </summary>
			protected virtual NonEmptyTargetControl NonEmptyTargetControl
			{
				get { return m_bulkEditBar.m_bcNonEmptyTargetControl; }
			}

			/// <summary>
			///
			/// </summary>
			public string NonEmptyTargetWriteMode
			{
				get
				{
					if (String.IsNullOrEmpty(m_nonEmptyTargetMode) && CanLoadFromBulkEditBar())
						m_nonEmptyTargetMode = NonEmptyTargetControl.NonEmptyMode.ToString();
					if (m_nonEmptyTargetMode == null)
						m_nonEmptyTargetMode = String.Empty;
					return m_nonEmptyTargetMode;
				}
				set { m_nonEmptyTargetMode = value; }
			}

			/// <summary>
			///
			/// </summary>
			public string NonEmptyTargetSeparator
			{
				get
				{
					if (String.IsNullOrEmpty(m_nonEmptyTargetSeparator) && CanLoadFromBulkEditBar())
						m_nonEmptyTargetSeparator = NonEmptyTargetControl.Separator;
					if (m_nonEmptyTargetSeparator == null)
						m_nonEmptyTargetSeparator = String.Empty;
					return m_nonEmptyTargetSeparator;
				}
				set { m_nonEmptyTargetSeparator = value; }
			}

			/// <summary>
			///
			/// </summary>
			protected override void SetupBulkEditBarTab(BulkEditBar bulkEditBar)
			{
				InitizializeTab(bulkEditBar);
				base.SetupBulkEditBarTab(bulkEditBar);
				if (SourceCombo != null)
				{
					SourceCombo.Text = this.SourceField;
					if (SourceCombo.SelectedIndex == -1)
					{
						// by default select the first item.
						if (SourceCombo.Items.Count > 0)
							SourceCombo.SelectedIndex = 0;
					}
				}
				NonEmptyTargetControl.NonEmptyMode =
					(NonEmptyTargetOptions)Enum.Parse(typeof(NonEmptyTargetOptions), this.NonEmptyTargetWriteMode);
				NonEmptyTargetControl.Separator = this.NonEmptyTargetSeparator;
			}

			/// <summary>
			/// the target combo for a particular tab page.
			/// </summary>
			protected override FwOverrideComboBox TargetComboForTab
			{
				get { return m_bulkEditBar.m_bulkCopyTargetCombo; }
			}

			/// <summary>
			/// this is a hack that explictly triggers the currentTargetCombo.SelectedIndexChange delegates
			/// during initialization, since they do not fire automatically until after everything is setup.
			/// </summary>
			protected override void InvokeTargetComboSelectedIndexChanged()
			{
				m_bulkEditBar.m_bulkCopyTargetCombo_SelectedIndexChanged(this, EventArgs.Empty);
			}

			/// <summary>
			///
			/// </summary>
			/// <param name="bulkEditBar"></param>
			protected virtual void InitizializeTab(BulkEditBar bulkEditBar)
			{
				bulkEditBar.InitBulkCopyTab();
			}

		}

		/// <summary>
		///
		/// </summary>
		public class ClickCopyTabPageSettings : BulkEditTabPageSettings
		{
			string m_copyMode = "";
			string m_nonEmptyTargetMode = "";
			string m_nonEmptyTargetSeparator = "";

			/// <summary>
			///
			/// </summary>
			protected override int ExpectedTab
			{
				get { return (int)BulkEditBarTabs.ClickCopy; }
			}

			/// <summary>
			/// the target combo for a particular tab page.
			/// </summary>
			protected override FwOverrideComboBox TargetComboForTab
			{
				get { return m_bulkEditBar.m_clickCopyTargetCombo; }
			}

			enum SourceCopyOptions
			{
				CopyWord = 0,
				StringReorderedAtClicked
			}

			/// <summary>
			///
			/// </summary>
			public string SourceCopyMode
			{
				get
				{
					if (String.IsNullOrEmpty(m_copyMode) && CanLoadFromBulkEditBar())
					{
						if (m_bulkEditBar.m_clickCopyWordButton.Checked)
							m_copyMode = SourceCopyOptions.CopyWord.ToString();
						else if (m_bulkEditBar.m_clickCopyReorderButton.Checked)
							m_copyMode = SourceCopyOptions.StringReorderedAtClicked.ToString();
					}
					if (m_copyMode == null)
						m_copyMode = String.Empty;
					return m_copyMode;
				}
				set { m_copyMode = value; }
			}

			/// <summary>
			///
			/// </summary>
			public string NonEmptyTargetWriteMode
			{
				get
				{
					if (String.IsNullOrEmpty(m_nonEmptyTargetMode) && CanLoadFromBulkEditBar())
					{
						if (m_bulkEditBar.m_clickCopyAppendButton.Checked)
							m_nonEmptyTargetMode = NonEmptyTargetOptions.Append.ToString();
						else if (m_bulkEditBar.m_clickCopyOverwriteButton.Checked)
							m_nonEmptyTargetMode = NonEmptyTargetOptions.Overwrite.ToString();
					}
					if (m_nonEmptyTargetMode == null)
						m_nonEmptyTargetMode = String.Empty;
					return m_nonEmptyTargetMode;
				}
				set { m_nonEmptyTargetMode = value; }
			}

			/// <summary>
			///
			/// </summary>
			public string NonEmptyTargetSeparator
			{
				get
				{
					if (String.IsNullOrEmpty(m_nonEmptyTargetSeparator) && CanLoadFromBulkEditBar())
						m_nonEmptyTargetSeparator = m_bulkEditBar.m_clickCopySepBox.Text;
					if (m_nonEmptyTargetSeparator == null)
						m_nonEmptyTargetSeparator = String.Empty;
					return m_nonEmptyTargetSeparator;
				}
				set { m_nonEmptyTargetSeparator = value; }
			}

			/// <summary>
			///
			/// </summary>
			protected override void SetupBulkEditBarTab(BulkEditBar bulkEditBar)
			{
				bulkEditBar.InitClickCopyTab();
				base.SetupBulkEditBarTab(bulkEditBar);
				SourceCopyOptions sourceCopyMode =
					(SourceCopyOptions)Enum.Parse(typeof(SourceCopyOptions), this.SourceCopyMode);
				switch (sourceCopyMode)
				{
					case SourceCopyOptions.StringReorderedAtClicked:
						m_bulkEditBar.m_clickCopyReorderButton.Checked = true;
						break;
					case SourceCopyOptions.CopyWord:
					default:
						m_bulkEditBar.m_clickCopyWordButton.Checked = true;
						break;
				}

				NonEmptyTargetOptions nonEmptyTargetMode =
					(NonEmptyTargetOptions)Enum.Parse(typeof(NonEmptyTargetOptions),
					this.NonEmptyTargetWriteMode);
				switch (nonEmptyTargetMode)
				{
					case NonEmptyTargetOptions.Overwrite:
						m_bulkEditBar.m_clickCopyOverwriteButton.Checked = true;
						break;
					case NonEmptyTargetOptions.Append:
					default:
						m_bulkEditBar.m_clickCopyAppendButton.Checked = true;
						break;
				}
				m_bulkEditBar.m_clickCopySepBox.Text = this.NonEmptyTargetSeparator;
			}

			/// <summary>
			/// this is a hack that explictly triggers the currentTargetCombo.SelectedIndexChange delegates
			/// during initialization, since they do not fire automatically until after everything is setup.
			/// </summary>
			protected override void InvokeTargetComboSelectedIndexChanged()
			{
				m_bulkEditBar.m_clickCopyTargetCombo_SelectedIndexChanged(this, EventArgs.Empty);
			}

			/// <summary>
			/// Update Preview/Clear and Apply Button states.
			/// </summary>
			protected override void SetupApplyPreviewButtons()
			{
				m_bulkEditBar.m_ApplyButton.Visible = false;
				m_bulkEditBar.m_previewButton.Visible = false;
			}

			/// <summary>
			/// when switching contexts, we should commit any pending click copy changes.
			/// </summary>
			/// <param name="bulkEditBar"></param>
			protected override void SaveSettings(BulkEditBar bulkEditBar)
			{
				// first commit any pending changes.
				// switching from click copy, so commit any pending changes.
				m_bulkEditBar.CommitClickChanges(this, EventArgs.Empty);
				base.SaveSettings(bulkEditBar);
			}
		}

		/// <summary>
		/// Same as BulkCopy except for the Process combo box and different controls.
		/// </summary>
		public class ProcessTabPageSettings : BulkCopyTabPageSettings
		{
			string m_process = "";

			/// <summary>
			///
			/// </summary>
			protected override int ExpectedTab
			{
				get { return (int)BulkEditBarTabs.Process; }
			}

			/// <summary>
			///
			/// </summary>
			protected override FwOverrideComboBox SourceCombo
			{
				get { return m_bulkEditBar.m_transduceSourceCombo; }
			}

			/// <summary>
			///
			/// </summary>
			protected override NonEmptyTargetControl NonEmptyTargetControl
			{
				get { return m_bulkEditBar.m_trdNonEmptyTargetControl; }
			}

			/// <summary>
			/// the target combo for a particular tab page.
			/// </summary>
			protected override FwOverrideComboBox TargetComboForTab
			{
				get { return m_bulkEditBar.m_transduceTargetCombo; }
			}

			/// <summary>
			///
			/// </summary>
			public string Process
			{
				get
				{
					if (String.IsNullOrEmpty(m_process) &&
						CanLoadFromBulkEditBar() &&
						m_bulkEditBar.m_transduceProcessorCombo != null)
					{
						m_process = m_bulkEditBar.m_transduceProcessorCombo.Text;
					}
					if (m_process == null)
						m_process = String.Empty;
					return m_process;
				}
				set { m_process = value; }
			}

			/// <summary>
			///
			/// </summary>
			protected override void SetupBulkEditBarTab(BulkEditBar bulkEditBar)
			{
				base.SetupBulkEditBarTab(bulkEditBar);

				// now handle the process combo.
				if (m_bulkEditBar.m_transduceProcessorCombo != null)
					m_bulkEditBar.m_transduceProcessorCombo.Text = this.Process;
			}

			/// <summary>
			/// this is a hack that explictly triggers the currentTargetCombo.SelectedIndexChange delegates
			/// during initialization, since they do not fire automatically until after everything is setup.
			/// </summary>
			protected override void InvokeTargetComboSelectedIndexChanged()
			{
				m_bulkEditBar.m_transduceTargetCombo_SelectedIndexChanged(this, EventArgs.Empty);
			}

			/// <summary>
			///
			/// </summary>
			/// <param name="bulkEditBar"></param>
			protected override void InitizializeTab(BulkEditBar bulkEditBar)
			{
				bulkEditBar.InitTransduce();
			}
		}

		/// <summary>
		/// this just saves the target field
		/// </summary>
		public class BulkReplaceTabPageSettings : BulkEditTabPageSettings
		{
			/// <summary>
			///
			/// </summary>
			/// <param name="bulkEditBar"></param>
			protected override void SaveSettings(BulkEditBar bulkEditBar)
			{
				base.SaveSettings(bulkEditBar);

				// now temporarily save some nonserializable objects so that they will
				// persist for the duration of the app, but not after closing the app.
				// 1) the Find & Replace pattern
				string keyFindPattern = BuildFindPatternKey(bulkEditBar);
				Mediator mediator = bulkEditBar.m_mediator;
				// store the Replace string into the Pattern
				m_bulkEditBar.m_pattern.ReplaceWith = m_bulkEditBar.m_tssReplace;
				VwPatternSerializableSettings patternSettings = new VwPatternSerializableSettings(m_bulkEditBar.m_pattern);
				string patternAsXml = XmlUtils.SerializeObjectToXmlString(patternSettings);
				mediator.PropertyTable.SetProperty(keyFindPattern, patternAsXml, false);
				mediator.PropertyTable.SetPropertyPersistence(keyFindPattern, true);
			}

			/// <summary>
			/// Check that we've changed to BulkEditBar to ExpectedTab,
			/// and then set BulkEditBar to those tab settings
			/// </summary>
			/// <param name="bulkEditBar"></param>
			protected override void SetupBulkEditBarTab(BulkEditBar bulkEditBar)
			{
				bulkEditBar.InitFindReplaceTab();
				base.SetupBulkEditBarTab(bulkEditBar);

				// now setup nonserializable objects
				bulkEditBar.m_pattern = this.Pattern;
				bulkEditBar.m_tssReplace = bulkEditBar.m_pattern.ReplaceWith;
				bulkEditBar.UpdateFindReplaceSummary();
				bulkEditBar.EnablePreviewApplyForFindReplace();
			}

			/// <summary>
			///
			/// </summary>
			protected override FwOverrideComboBox TargetComboForTab
			{
				get { return m_bulkEditBar.m_findReplaceTargetCombo; }
			}

			/// <summary>
			/// this is a hack that explictly triggers the currentTargetCombo.SelectedIndexChange delegates
			/// during initialization, since they do not fire automatically until after everything is setup.
			/// </summary>
			protected override void InvokeTargetComboSelectedIndexChanged()
			{
				m_bulkEditBar.m_findReplaceTargetCombo_SelectedIndexChanged(this, EventArgs.Empty);
			}

			private static string BuildFindPatternKey(BulkEditBar bulkEditBar)
			{
				string toolId = GetBulkEditBarToolId(bulkEditBar);
				string currentTabPageName = GetCurrentTabPageName(bulkEditBar);
				string keyFindPattern = String.Format("{0}_{1}_FindAndReplacePattern", toolId, currentTabPageName);
				return keyFindPattern;
			}

			#region NonSerializable properties
			IVwPattern m_pattern = null;
			internal IVwPattern Pattern
			{
				get
				{
					if (m_pattern == null && CanLoadFromBulkEditBar())
					{
						// first see if we can load the value from BulkEditBar
						if (m_bulkEditBar.m_pattern != null)
						{
							m_pattern = m_bulkEditBar.m_pattern;
						}
						else
						{
							// next see if we can restore the pattern from deserializing settings stored in the property table.
							string patternAsXml = m_bulkEditBar.m_mediator.PropertyTable.GetStringProperty(BuildFindPatternKey(m_bulkEditBar), null);

							VwPatternSerializableSettings settings = (VwPatternSerializableSettings)XmlUtils.DeserializeXmlString(patternAsXml,
								typeof(VwPatternSerializableSettings));
							if (settings != null)
								m_pattern = settings.NewPattern;
							if (m_pattern == null)
								m_pattern = VwPatternClass.Create();
						}
					}
					return m_pattern;
				}
			}

			#endregion NonSerializable properties


		}

		/// <summary>
		/// this just saves the target field
		/// </summary>
		public class DeleteTabPageSettings : BulkEditTabPageSettings
		{
			/// <summary>
			///
			/// </summary>
			protected override int ExpectedTab
			{
				get { return (int)BulkEditBarTabs.Delete; }
			}

			/// <summary>
			///
			/// </summary>
			protected override FwOverrideComboBox TargetComboForTab
			{
				get { return m_bulkEditBar.m_deleteWhatCombo; }
			}

			/// <summary>
			/// this is a hack that explictly triggers the currentTargetCombo.SelectedIndexChange delegates
			/// during initialization, since they do not fire automatically until after everything is setup.
			/// </summary>
			protected override void InvokeTargetComboSelectedIndexChanged()
			{
				m_bulkEditBar.m_deleteWhatCombo_SelectedIndexChanged(this, EventArgs.Empty);
			}

			/// <summary>
			///
			/// </summary>
			/// <param name="bulkEditBar"></param>
			protected override void SetupBulkEditBarTab(BulkEditBar bulkEditBar)
			{
				bulkEditBar.InitDeleteTab();
				base.SetupBulkEditBarTab(bulkEditBar);
			}
		}

		private void m_operationsTabControl_SelectedIndexChanged(object sender, EventArgs e)
		{
			m_operationLabel.Text = s_labels[m_operationsTabControl.SelectedIndex];
			BulkEditTabPageSettings.InitializeSelectedTab(this);
			bool fReconstucted = false;
			bool fWasShowEnabled = m_bv.BrowseView.Vc.ShowEnabled;
			if (m_operationsTabControl.SelectedIndex != (int)BulkEditBarTabs.Delete)
			{
				m_ApplyButton.Text = m_originalApplyText; // currently all other tabs except Delete use the default text.
				m_bv.BrowseView.Vc.ShowEnabled = false;
			}
			if (m_bv.SpecialCache.get_IntProp(m_bv.RootObjectHvo, XMLViewsDataCache.ktagActiveColumn) != 0)
			{
				// Don't do it this way.
				//m_cache.VwCacheDaAccessor.CacheIntProp(m_bv.RootObjectHvo, XmlBrowseViewVc.ktagActiveColumn, 0);
				// Use special SDA instead.
				m_bv.SpecialCache.SetInt(m_bv.RootObjectHvo, XMLViewsDataCache.ktagActiveColumn, 0);
				if (m_bv != null && m_bv.BrowseView != null && m_bv.BrowseView.RootBox != null)
				{
					m_bv.BrowseView.RootBox.Reconstruct();
					fReconstucted = true;
				}
			}
			// if show enabled changed and we haven't reconstructed yet, do it now.
			if (!fReconstucted &&
				fWasShowEnabled != m_bv.BrowseView.Vc.ShowEnabled && m_bv.BrowseView.RootBox != null)
			{
				m_bv.BrowseView.RootBox.Reconstruct();
			}
			TabPage selectedTab = m_operationsTabControl.SelectedTab;
			selectedTab.Controls.Add(m_bulkEditOperationLabel);
			selectedTab.Controls.Add(m_operationLabel);
			if (m_bulkEditIcon != null)
				selectedTab.Controls.Add(m_bulkEditIcon);
			if (m_bulkEditIconButton != null)
				selectedTab.Controls.Add(m_bulkEditIconButton);
		}

		/// <summary>
		///
		/// </summary>
		internal protected virtual void SaveSettings()
		{
			// ClickCopy.ClickCopyTabPageSettings/SaveSettings()/CommitClickChanges()
			// could possibly change m_hvoSelected when we're not ready, so save current.
			// see comment on LT-4768 in UpdateColumnList.
			int oldSelected = m_hvoSelected;
			BulkEditTabPageSettings.CaptureSettingsForCurrentTab(this);
			m_hvoSelected = oldSelected;
		}

		private void m_findReplaceSetupButton_Click(object sender, System.EventArgs e)
		{
			// Ensure that the find and replace strings have the correct writing system.
			int ws = -50;
			try
			{
				// Find the writing system for the selected column (see LT-5491).
				FieldComboItem fci = m_findReplaceTargetCombo.SelectedItem as FieldComboItem;
				if (fci == null)
				{
					MessageBox.Show(XMLViewsStrings.ksChooseEditTarget);
					return;
				}
				XmlNode xnField = m_bv.ColumnSpecs[fci.ColumnIndex];
				string sWs = XmlViewsUtils.FindWsParam(xnField);
				if (String.IsNullOrEmpty(sWs))
				{
					// It's likely a custom field with a ws selector in the field metadata.
					string sTransduce = XmlUtils.GetOptionalAttributeValue(xnField, "transduce");
					if (!String.IsNullOrEmpty(sTransduce))
					{
						string[] parts = sTransduce.Split('.');
						if (parts.Length == 2)
						{
							string className = parts[0];
							string fieldName = parts[1];
							IFwMetaDataCache mdc = m_cache.DomainDataByFlid.MetaDataCache;
							try
							{
								int clid = mdc.GetClassId(className);
								int flid = mdc.GetFieldId2(clid, fieldName, true);
								ws = FieldReadWriter.GetWsFromMetaData(0, flid, m_cache);
								if (ws == 0)
									ws = m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle;
							}
							catch
							{
							}
						}
					}
				}
				else if (!XmlViewsUtils.GetWsRequiresObject(sWs))
				{
					// Try to convert the ws parameter into an int.  Sometimes the parameter
					// cannot be interpreted without an object, such as when the ws is a magic
					// string that will change the actual ws depending on the contents of the
					// object.  In these cases, we give -50 as a known constant to check for.
					// This can possibly throw an exception, so we'll enclose it in a try block.
					ws = WritingSystemServices.InterpretWsLabel(m_cache, sWs, null, 0, 0, null);
				}
			}
			catch
			{
			}
			if (ws == -50)
				ws = m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle;
			ITsStrFactory tsf = m_cache.TsStrFactory;

			if (m_tssReplace == null)
				m_tssReplace = tsf.MakeString("", ws);
			else
			{
				// If we have a replacement TsString, but no pattern, keep the text but
				// no properties.
				if (m_pattern == null)
					m_tssReplace = tsf.MakeString(m_tssReplace.Text, ws);
				else if (!m_pattern.MatchOldWritingSystem)
				{
					// We have both a string and a pattern. We want to clear writing system information
					// on the string unless we are matching on WS. But we don't want to clear any style info.
					ITsStrBldr bldr = m_tssReplace.GetBldr();
					bldr.SetIntPropValues(0, bldr.Length, (int)FwTextPropType.ktptWs,
										  (int)FwTextPropVar.ktpvDefault, ws);
					m_tssReplace = bldr.GetString();
				}
			}
			if (m_pattern != null)
			{
				if (m_pattern.Pattern == null)
					m_pattern.Pattern = tsf.MakeString("", ws);
				else if (!m_pattern.MatchOldWritingSystem)
				{
					// Enforce the expected writing system; but don't clear styles.
					ITsStrBldr bldr = m_pattern.Pattern.GetBldr();
					bldr.SetIntPropValues(0, bldr.Length, (int)FwTextPropType.ktptWs,
										  (int)FwTextPropVar.ktpvDefault, ws);
					m_pattern.Pattern = bldr.GetString();
				}
			}

			using (FwFindReplaceDlg findDlg = new FwFindReplaceDlg())
			{
				//Change the Title from "Find and Replace" to "Bulk Replace Setup"
				findDlg.Text = String.Format(XMLViewsStrings.khtpBulkReplaceTitle);
				IApp app = (IApp)m_mediator.PropertyTable.GetValue("App");
				findDlg.SetDialogValues(m_cache, m_pattern, m_bv.BrowseView.StyleSheet,
					FindForm(), m_mediator.HelpTopicProvider, app);
				findDlg.RestoreAndPersistSettingsIn(m_mediator);
				// Set this AFTER it has the correct WSF!
				findDlg.ReplaceText = m_tssReplace;

				if (findDlg.ShowDialog(this) == DialogResult.OK)
				{
					m_tssReplace = findDlg.ResultReplaceText;
					UpdateFindReplaceSummary();
					EnablePreviewApplyForFindReplace();
				}
			}
		}

		string GetString(ITsString tss)
		{
			if (tss == null)
				return "";
			return tss.Text;
		}

		private void UpdateFindReplaceSummary()
		{
			// Enhance JohnT: use some sort of View-based label that can display Graphite etc.
			if (m_pattern != null && m_pattern.Pattern != null && m_pattern.Pattern.Length > 0)
			{
				m_findReplaceSummaryLabel.Text = String.Format(XMLViewsStrings.ksReplaceXWithY,
					GetString(m_pattern.Pattern), GetString(m_tssReplace));
			}
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize a combo box with the columns that can be sources or targets for string manipulations.
		/// If possible select an item with the same name.
		/// If not select the first item if any.
		/// If none set the Text to 'no choices'.
		/// </summary>
		/// <param name="combo">The combo.</param>
		/// <param name="fIsSourceCombo">if set to <c>true</c> [f is source combo].</param>
		/// ------------------------------------------------------------------------------------
		protected void InitStringCombo(FwOverrideComboBox combo, bool fIsSourceCombo)
		{
			FieldComboItem selectedItem = combo.SelectedItem as FieldComboItem;
			combo.ClearItems();
//			FieldComboItem newSelection = AddStringFieldItemsToCombo(combo, selectedItem, fIsSourceCombo);
			AddStringFieldItemsToCombo(combo, selectedItem, fIsSourceCombo);
			if (combo.Items.Count == 0)
			{
				combo.Text = XMLViewsStrings.ksNoChoices;
				return;
			}
			// NOTE: Don't re-set the item here, let BulkEditBarTabSettings do it.
			//if (newSelection == null)
			//    combo.SelectedIndex = 0;
			//else
			//    combo.SelectedItem = newSelection;
		}

		private FieldComboItem AddStringFieldItemsToCombo(ComboBox combo, FieldComboItem selectedItem, bool fIsSourceCombo)
		{
			FieldComboItem newSelection = null;
			int icol = -1;
			foreach (XmlNode node in m_bv.ColumnSpecs)
			{
				icol++;
				FieldReadWriter accessor = null;
				string optionLabel = GetColumnLabel(m_mediator, node);
				try
				{
					if (fIsSourceCombo)
						accessor = new ManyOnePathSortItemReadWriter(m_cache, node, m_bv, (IApp)m_mediator.PropertyTable.GetValue("App"));
					else
						accessor = FieldReadWriter.Create(node, m_cache, m_bv.RootObjectHvo);
					if (accessor == null)
						continue;
					// Use the decorated data access - see FWR-376.
					accessor.DataAccess = m_bv.SpecialCache;
				}
				catch
				{
					Debug.Fail(String.Format("There was an error creating Delete combo item for column ({0})"), optionLabel);
					// skip buggy column
					continue;
				}

				FieldComboItem item = new FieldComboItem(optionLabel, icol, accessor);
				combo.Items.Add(item);
				if (selectedItem != null && selectedItem.ToString() == item.ToString())
					newSelection = item;
			}
			return newSelection;
		}

		private void xbv_ClickCopy(object sender, ClickCopyEventArgs e)
		{
			FieldComboItem selectedItem = m_clickCopyTargetCombo.SelectedItem as FieldComboItem;
			if (selectedItem == null)
			{
				MessageBox.Show(XMLViewsStrings.ksChooseClickTarget);
				return;
			}
			// Check whether this item is really editable.
			if (!String.IsNullOrEmpty(m_sClickEditIf) && m_wsClickEditIf != 0)
			{
				var co = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(e.Hvo);
				if (m_miClickEditIf == null)
					m_miClickEditIf = co.GetType().GetMethod(m_sClickEditIf);
				if (m_miClickEditIf != null)
				{
					object o = m_miClickEditIf.Invoke(co, new object[] { m_wsClickEditIf });
					if (o.GetType() == typeof(bool))
					{
						bool fAllowEdit = m_fClickEditIfNot ? !(bool)o : (bool)o;
						if (!fAllowEdit)
							return;
					}
				}
			}
			ITsString tssOld = selectedItem.Accessor.CurrentValue(e.Hvo);
			ITsString tssNew = e.Word;
			if (m_clickCopyReorderButton.Checked)
			{
				tssNew = e.Source;
				if (e.IchStartWord > 0)
				{
					ITsStrBldr tsb = tssNew.GetBldr();
					ITsStrBldr tsbStart = tssNew.GetBldr();
					tsbStart.Replace(e.IchStartWord, tsbStart.Length, "", null);
					tsb.Replace(0, e.IchStartWord, "", null);
					tsb.Replace(tsb.Length, tsb.Length, ", ", null);
					tsb.ReplaceTsString(tsb.Length, tsb.Length, tsbStart.GetString());
					tssNew = tsb.GetString();
				}
			}
			if (tssOld != null && tssOld.Length > 0 && m_clickCopyAppendButton.Checked)
			{
				ITsStrBldr tsb = tssOld.GetBldr();
				int ich = tsb.Length;
				tsb.ReplaceTsString(ich, ich, m_clickCopySepBox.Tss);
				ich = tsb.Length; // typically ich+1, but normalization MIGHT interfere?
				tsb.ReplaceTsString(ich, ich, tssNew);
				tssNew = tsb.GetString();
			}
			m_cache.DomainDataByFlid.BeginUndoTask(XMLViewsStrings.ksUndoClickCopy, XMLViewsStrings.ksRedoClickCopy);
			selectedItem.Accessor.SetNewValue(e.Hvo, tssNew);
			m_cache.DomainDataByFlid.EndUndoTask();
		}

		private void m_transduceSetupButton_Click(object sender, System.EventArgs e)
		{
#pragma warning disable 219
			object selItem = m_transduceProcessorCombo.SelectedItem;
			string selName = (selItem == null) ? "" : selItem.ToString();
#pragma warning restore 219

			try
			{
				string prevEC = m_transduceProcessorCombo.Text;
				IApp app = (IApp)m_mediator.PropertyTable.GetValue("App");
				using (AddCnvtrDlg dlg = new AddCnvtrDlg(m_mediator.HelpTopicProvider, app, null,
					m_transduceProcessorCombo.Text, null, true))
				{
					dlg.ShowDialog();

					// Reload the converter list in the combo to reflect the changes.
					this.InitConverterCombo();

					// Either select the new one or select the old one
					if (dlg.DialogResult == DialogResult.OK && !String.IsNullOrEmpty(dlg.SelectedConverter))
						m_transduceProcessorCombo.SelectedItem = dlg.SelectedConverter;
					else if (m_transduceProcessorCombo.Items.Count > 0)
						m_transduceProcessorCombo.SelectedItem = prevEC; // preserve selection if possible
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(String.Format(XMLViewsStrings.ksCannotAccessEC, ex.Message));
				return;
			}
		}

		private void m_clickCopyTargetCombo_SelectedIndexChanged(object sender, EventArgs e)
		{
			FieldComboItem selectedItem = m_clickCopyTargetCombo.SelectedItem as FieldComboItem;
			int column = -1;
			if (selectedItem != null)
			{
				column = selectedItem.ColumnIndex;
				XmlNode spec = m_bv.ColumnSpecs[column] as XmlNode;
				m_sClickEditIf = XmlUtils.GetOptionalAttributeValue(spec, "editif");
				m_wsClickEditIf = 0;
				if (!String.IsNullOrEmpty(m_sClickEditIf))
				{
					if (m_sClickEditIf[0] == '!')
					{
						m_fClickEditIfNot = true;
						m_sClickEditIf = m_sClickEditIf.Substring(1);
					}
					string sWs = StringServices.GetWsSpecWithoutPrefix(spec);
					if (sWs != null)
						m_wsClickEditIf = XmlViewsUtils.GetWsFromString(sWs, m_cache);
				}
				OnTargetComboItemChanged(selectedItem);
			}
			SetEditColumn(column);
		}

		int m_expectedListItemsClassId = 0;
		/// <summary>
		/// the expected record clerk's list items class id according to the
		/// selected target combo field. <c>0</c> if one hasn't been determined (yet).
		/// </summary>
		public int ExpectedListItemsClassId
		{
			get
			{
				if (m_expectedListItemsClassId == 0)
				{
					if (m_currentTargetCombo != null && m_currentTargetCombo.SelectedItem != null)
						m_expectedListItemsClassId = GetExpectedListItemsClassFromSelectedItem(m_currentTargetCombo.SelectedItem as FieldComboItem);
				}
				return m_expectedListItemsClassId;
			}
		}

		/// <summary>
		/// triggers TargetComboSelectedIndexChanged to tell delegates that TargetComboItem has changed.
		/// </summary>
		public void OnTargetComboSelectedIndexChanged()
		{
			if (m_currentTargetCombo != null)
				OnTargetComboItemChanged(m_currentTargetCombo.SelectedItem as FieldComboItem);
		}

		private void OnTargetComboItemChanged(FieldComboItem selectedItem)
		{
			if (TargetComboSelectedIndexChanged == null)
				return;
			int flid;
			m_expectedListItemsClassId = GetExpectedListItemsClassAndTargetFieldFromSelectedItem(selectedItem, out flid);
			if (m_expectedListItemsClassId != 0)
			{
				using (var targetFieldItem = new TargetFieldItem(selectedItem.ToString(),
					selectedItem.ColumnIndex, m_expectedListItemsClassId, flid))
				{
					var args = new TargetColumnChangedEventArgs(targetFieldItem);
					//REFACTOR: the BrowseView should not know about BulkEdit - They appear to be too highly coupled.
					m_bv.BulkEditTargetComboSelectedIndexChanged(args); // may set ForceReload flag on args.
					if (TargetComboSelectedIndexChanged != null)
						TargetComboSelectedIndexChanged(this, args);
				}
			}
		}

		int GetExpectedListItemsClassFromSelectedItem(FieldComboItem selectedItem)
		{
			int field;
			return GetExpectedListItemsClassAndTargetFieldFromSelectedItem(selectedItem, out field);
		}

		int GetExpectedListItemsClassAndTargetFieldFromSelectedItem(FieldComboItem selectedItem, out int field)
		{
			field = 0; // default
			if (selectedItem == null)
				return 0;
			int listItemsClassId = 0;
			// if we're only expecting this bulk edit to be used with one list items class, use that one.
			if (m_bulkEditListItemsClasses.Count == 1)
			{
				listItemsClassId = m_bulkEditListItemsClasses[0];
			}
			else
			{
				// figure out the class of the expected list items for the corresponding bulk edit item
				// and make that our target class.
				if (selectedItem is TargetFieldItem &&
					(selectedItem as TargetFieldItem).ExpectedListItemsClass != 0)
				{
					var targetFieldItem = selectedItem as TargetFieldItem;
					listItemsClassId = targetFieldItem.ExpectedListItemsClass;
					field = targetFieldItem.TargetFlid;
				}
				else
				{
					// the expected class of the list items will be the source class
					// of the first field in the FieldPath;
					if (selectedItem.Accessor != null)
					{
						field = selectedItem.Accessor.FieldPath[0];
					}
					else
					{
						BulkEditItem beItem = m_beItems[selectedItem.ColumnIndex];
						field = beItem.BulkEditControl.FieldPath[0];
					}
					listItemsClassId = (int)m_cache.DomainDataByFlid.MetaDataCache.GetOwnClsId((int)field);
				}
			}
			return listItemsClassId;
		}

		void SetEditColumn(int icol)
		{
			if (m_bv.BrowseView.Vc.OverrideAllowEditColumn != icol)
			{
				m_bv.BrowseView.Vc.OverrideAllowEditColumn = icol;
				if (m_bv.BrowseView.RootBox != null)
					m_bv.BrowseView.RootBox.Reconstruct();
			}
			if (icol >= 0)
			{
				m_bv.BrowseView.EditingHelper.ReadOnlyTextCursor = Cursors.Hand;
				m_bv.BrowseView.LostFocus -= new EventHandler(CommitClickChanges);  // make sure we don't install more than one.
				m_bv.BrowseView.LostFocus += new EventHandler(CommitClickChanges);
				m_bv.BrowseView.SelectedIndexChanged -= new EventHandler(CommitClickChanges); // don't install more than one
				m_bv.BrowseView.SelectedIndexChanged +=new EventHandler(CommitClickChanges);
			}
			else
			{
				m_bv.BrowseView.EditingHelper.ReadOnlyTextCursor = null;
				m_bv.BrowseView.LostFocus -= new EventHandler(CommitClickChanges);
				m_bv.BrowseView.SelectedIndexChanged -=new EventHandler(CommitClickChanges);
			}
		}

		/// <summary>
		/// When we switch rows or lose focus in the main browse view, commit any changes
		/// made in click copy mode.
		/// TODO (DamienD): I don't think this is used anymore, so it can probably be removed.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		internal void CommitClickChanges(object sender, EventArgs e)
		{
			// only appropriate in context of clickCopyTab.
			if (m_operationsTabControl.SelectedTab != m_clickCopyTab)
			{
				// LT-9033: we can crash if this gets called in the context of
				// a non-clickCopy tab after removing a column that click copy
				// was using as a target field.
				Debug.Fail("CommitClickChanges should only be fired in context of Click Copy tab, not " +
						   m_operationsTabControl.SelectedTab.ToString());
				return;
			}
			FieldComboItem selectedItem = m_clickCopyTargetCombo.SelectedItem as FieldComboItem;
			if (selectedItem != null)
			{
				XmlNode spec = m_bv.ColumnSpecs[selectedItem.ColumnIndex] as XmlNode;
				if (m_hvoSelected != 0)
					CommitChanges(m_hvoSelected, XmlUtils.GetOptionalAttributeValue(spec, "commitChanges"),
						m_cache, selectedItem.Accessor.WritingSystem);
			}
			m_hvoSelected = m_bv.BrowseView.SelectedObject;
		}

		// Commit changes for the current hvo if it has a commit changes handler specified.
		internal static void CommitChanges(int hvo, string commitChanges, FdoCache cache, int ws)
		{
			if (commitChanges != null && commitChanges != "" &&
				cache.ActionHandlerAccessor != null && !cache.ActionHandlerAccessor.IsUndoOrRedoInProgress)
			{
				var cmo = cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
				var mi = cmo.GetType().GetMethod(commitChanges, new[] {typeof(int)});
				if (mi == null)
					throw new ConfigurationException("Method " +commitChanges +
						" not found on class " + cmo.GetType().Name);
				mi.Invoke(cmo, new object[] {ws});
			}
		}

		private void m_bulkCopyTargetCombo_SelectedIndexChanged(object sender, EventArgs e)
		{
			FieldComboItem fci = m_bulkCopyTargetCombo.SelectedItem as FieldComboItem;
			if (fci.Accessor.WritingSystem != m_bcNonEmptyTargetControl.WritingSystemCode)
			{
				m_bcNonEmptyTargetControl.WritingSystemCode = fci.Accessor.WritingSystem;
				// This causes it to keep the same string but convert to the new WS.
				m_bcNonEmptyTargetControl.Separator = m_bcNonEmptyTargetControl.Separator;
			}
			OnTargetComboItemChanged(fci);
		}

		private void m_transduceTargetCombo_SelectedIndexChanged(object sender, EventArgs e)
		{
			FieldComboItem fci = m_transduceTargetCombo.SelectedItem as FieldComboItem;
			if (fci.Accessor.WritingSystem != m_trdNonEmptyTargetControl.WritingSystemCode)
			{
				m_trdNonEmptyTargetControl.WritingSystemCode = fci.Accessor.WritingSystem;
				// This causes it to keep the same string but convert to the new WS.
				m_trdNonEmptyTargetControl.Separator = m_trdNonEmptyTargetControl.Separator;
			}
			OnTargetComboItemChanged(fci);
		}

		private void m_findReplaceTab_Enter(object sender, EventArgs e)
		{
			m_previewButton.SetBounds(500, 39, 75, 23);
			m_ApplyButton.SetBounds(500, 72, 75, 23);
			m_helpButton.SetBounds(500, 105, 75, 23);
		}

		private void m_listChoiceTab_Enter(object sender, EventArgs e)
		{
			m_previewButton.SetBounds(500, 39, 75, 23);
			m_ApplyButton.SetBounds(500, 72, 75, 23);
			m_helpButton.SetBounds(500, 105, 75, 23);
		}

		private void m_bulkCopyTab_Enter(object sender, EventArgs e)
		{
			m_previewButton.SetBounds(500, 39, 75, 23);
			m_ApplyButton.SetBounds(500, 72, 75, 23);
			m_helpButton.SetBounds(500, 105, 75, 23);
		}

		private void m_clickCopyTab_Enter(object sender, EventArgs e)
		{
			m_previewButton.SetBounds(590, 39, 75, 23);
			m_ApplyButton.SetBounds(590, 72, 75, 23);
			m_helpButton.SetBounds(590, 105, 75, 23);
		}

		private void m_transduceTab_Enter(object sender, EventArgs e)
		{
			m_previewButton.SetBounds(590, 39, 75, 23);
			m_ApplyButton.SetBounds(590, 72, 75, 23);
			m_helpButton.SetBounds(590, 105, 75, 23);
		}

		private void m_deleteTab_Enter(object sender, EventArgs e)
		{
			m_previewButton.SetBounds(500, 39, 75, 23);
			m_ApplyButton.SetBounds(500, 72, 75, 23);
			m_helpButton.SetBounds(500, 105, 75, 23);
		}

		internal void SetStyleSheet(IVwStylesheet value)
		{
			foreach(BulkEditItem item in m_beItems)
			{
				if (item != null && item.BulkEditControl != null)
					item.BulkEditControl.Stylesheet = value;
			}
		}

		private void m_findReplaceTargetCombo_SelectedIndexChanged(object sender, EventArgs e)
		{
			FieldComboItem selectedItem = m_findReplaceTargetCombo.SelectedItem as FieldComboItem;
			OnTargetComboItemChanged(selectedItem);
		}
	}

	/// <summary>
	/// Notify clients that a bulk edit target column has changed and readjust to
	/// new list items class if necessary.
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	public delegate void TargetColumnChangedHandler(object sender, TargetColumnChangedEventArgs e);

	/// <summary>
	///
	/// </summary>
	public class TargetColumnChangedEventArgs : EventArgs
	{
		private TargetFieldItem m_tfi;

		internal TargetColumnChangedEventArgs(TargetFieldItem selectedTargetFieldItem)
		{
			m_tfi = selectedTargetFieldItem;
		}

		/// <summary>
		/// Target column expects its base list to be this list items class
		/// </summary>
		public int ExpectedListItemsClass
		{
			get { return m_tfi.ExpectedListItemsClass; }
		}

		/// <summary>
		/// The field we want to bulk edit (or 0, if it doesn't matter).
		/// </summary>
		public int TargetFlid
		{
			get { return m_tfi.TargetFlid; }
		}

		internal int ColumnIndex
		{
			get { return m_tfi.ColumnIndex; }
		}

		/// <summary>
		/// True to force reload of list even if expected item class has not changed.
		/// </summary>
		public bool ForceReload { get; internal set; }
	}

	/// <summary>
	/// A trivial interface implmented by IBulkEditSpecControls which can replace objects in
	/// the underlying list.
	/// </summary>
	public interface IGetReplacedObjects
	{
		/// <summary>
		/// Get the dictionary which maps from replaced objects to replacements (HVOs/object IDs).
		/// </summary>
		Dictionary<int, int> ReplacedObjects { get; }
	}

	/// <summary>
	///
	/// </summary>
	public interface IGhostable
	{
		/// <summary>
		/// Initialize the class for handling ghost items and parents of ghost items.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="colSpec"></param>
		void InitForGhostItems(FdoCache cache, XmlNode colSpec);
	}

	/// <summary>
	/// This interface specifies a component that allows the user to enter specifications
	/// on how to change a column, and displays the current choice.
	/// </summary>
	public interface IBulkEditSpecControl
	{
		/// <summary>Get and set the mediator.</summary>
		Mediator Mediator { get; set; }
		/// <summary>Retrieve the control that does the work.</summary>
		Control Control { get; }
		/// <summary>Get or set the cache. Client promises to set this immediately after creation.</summary>
		FdoCache Cache { get; set; }
		/// <summary>
		/// The decorator cache that understands the special properties used to control the checkbox and preview.
		/// Client promises to set this immediately after creation.
		/// </summary>
		XMLViewsDataCache DataAccess { get; set; }
		/// <summary>
		/// Set a stylesheet. Should be done soon after creation, before reading Control.
		/// </summary>
		IVwStylesheet Stylesheet { set;}
		/// <summary>Invoked when the command is to be executed. The argument contains an array of
		/// the HVOs of the items to which the change should be done (those visible and checked).</summary>
		void DoIt(IEnumerable<int> itemsToChange, ProgressState state);
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is called when the preview button is clicked. The control is passed
		/// the list of currently active (filtered and checked) items. It should cache
		/// tagEnabled to zero for any objects that can't be
		/// modified. For ones that can, it should set the string property tagFakeFlid
		/// to the value to show in the 'modified' fields.
		/// </summary>
		/// <param name="itemsToChange">The items to change.</param>
		/// <param name="tagFakeFlid">The tag fake flid.</param>
		/// <param name="tagEnabled">The tag enabled.</param>
		/// <param name="state">The state.</param>
		/// ------------------------------------------------------------------------------------
		void FakeDoit(IEnumerable<int> itemsToChange, int tagFakeFlid, int tagEnabled, ProgressState state);

		/// <summary>
		/// True if the editor can set a value that will make the field 'clear'.
		/// </summary>
		bool CanClearField { get; }
		/// <summary>
		/// Select a value that will make the field 'clear' (used by BE Delete tab).
		/// </summary>
		void SetClearField();

		/// <summary>
		/// the field paths starting from the RootObject to the value/object that we're bulkEditing
		/// </summary>
		List<int> FieldPath { get; }

		/// <summary></summary>
		event FwSelectionChangedEventHandler ValueChanged;
	}

	/// <summary>
	/// An additional interface that may be implemented by an IBulkEditSpecControl if it needs to know
	/// when the text of its control has been set (typically by restoring from persistence).
	/// </summary>
	public interface ITextChangedNotification
	{
		/// <summary>
		/// Notifies that the control's text has changed.
		/// </summary>
		void ControlTextChanged();
	}

	/// <summary>
	/// A bulk edit item manages a control (typically a combo) for a column that can
	/// handle a list choice bulk edit operation. (The name reflects an original intent
	/// that it should handle any kind of bulk edit for its column.)
	/// </summary>
	public class BulkEditItem : IFWDisposable
	{
		IBulkEditSpecControl m_beCcontrol;

		/// <summary>
		///
		/// </summary>
		/// <param name="control"></param>
		public BulkEditItem(IBulkEditSpecControl control)
		{
			m_beCcontrol = control;
		}

		/// <summary>
		///
		/// </summary>
		public IBulkEditSpecControl BulkEditControl
		{
			get
			{
				CheckDisposed();
				return m_beCcontrol;
			}
		}

		#region IDisposable & Co. implementation
		// Region last reviewed: RandyR: Oct. 16, 2005.

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
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~BulkEditItem()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected virtual void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_beCcontrol != null && m_beCcontrol is IDisposable)
					(m_beCcontrol as IDisposable).Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_beCcontrol = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation
	}

	internal class TargetFieldItem : FieldComboItem
	{
		internal TargetFieldItem(string label, int columnIndex, int classId, int targetField)
			: this(label, columnIndex, classId)
		{
			TargetFlid = targetField;
		}
		internal TargetFieldItem(string label, int columnIndex, int classId)
			: this(label, columnIndex)
		{
			ExpectedListItemsClass = classId;
		}
		internal TargetFieldItem(string label, int columnIndex)
			: base(label, columnIndex, null)
		{
		}

		internal int ExpectedListItemsClass { get; private set; }

		/// <summary>
		/// The field we want to bulk edit (or 0, if it doesn't matter).
		/// </summary>
		internal int TargetFlid { get; set; }

	}

	/// <summary>
	/// keeps track of a row selection (for DeleteTab)
	/// as opposed to deleting a column.
	/// </summary>
	internal class ListClassTargetFieldItem : TargetFieldItem
	{
		internal ListClassTargetFieldItem(string label, int classId)
			: base(label, -1, classId)
		{
		}
	}

	/// <summary>
	/// This is a base class for several ways of faking and actually making a change.
	/// </summary>
	internal abstract class DoItMethod : IGetReplacedObjects
	{
		protected FieldReadWriter m_accessor; // typically the destination accessor, sometimes also the source.
		ISilDataAccess m_sda;
		FdoCache m_cache;
		XmlNode m_nodeSpec; // specification node for the column

		string m_sEditIf = null;
		bool m_fEditIfNot = false;
		MethodInfo m_miEditIf = null;
		int m_wsEditIf = 0;
		Dictionary<int, int> m_replacedObjects = new Dictionary<int, int>();

		public DoItMethod(FdoCache cache, ISilDataAccessManaged sda, FieldReadWriter accessor, XmlNode spec)
		{
			m_cache = cache;
			m_accessor = accessor;
			m_sda = sda;
			m_nodeSpec = spec;

			m_sEditIf = XmlUtils.GetOptionalAttributeValue(spec, "editif");
			if (!String.IsNullOrEmpty(m_sEditIf))
			{
				if (m_sEditIf[0] == '!')
				{
					m_fEditIfNot = true;
					m_sEditIf = m_sEditIf.Substring(1);
				}
				string sWs = StringServices.GetWsSpecWithoutPrefix(spec);
				if (sWs != null)
					m_wsEditIf = XmlViewsUtils.GetWsFromString(sWs, m_cache);
			}
		}

		internal bool IsMultilingual(int flid)
		{
			return IsMultilingual(flid, m_cache.DomainDataByFlid.MetaDataCache);
		}

		static internal bool IsMultilingual(int flid, IFwMetaDataCache mdc)
		{
			switch ((CellarPropertyType)(mdc.GetFieldType(flid) & (int)CellarPropertyTypeFilter.VirtualMask))
			{
			case CellarPropertyType.MultiString:
			case CellarPropertyType.MultiBigString:
			case CellarPropertyType.MultiUnicode:
			case CellarPropertyType.MultiBigUnicode:
				return true;
			default:
				return false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fake doing the change by setting the specified property to the appropriate value
		/// for each item in the set. Disable items that can't be set.
		/// </summary>
		/// <param name="itemsToChange">The items to change.</param>
		/// <param name="tagFakeFlid">The tag fake flid.</param>
		/// <param name="tagEnable">The tag enable.</param>
		/// <param name="state">The state.</param>
		/// ------------------------------------------------------------------------------------
		public void FakeDoit(IEnumerable<int> itemsToChange, int tagFakeFlid, int tagEnable, ProgressState state)
		{
			int i = 0;
			// Report progress 50 times or every 100 items, whichever is more (but no more than once per item!)
			int interval = Math.Min(100, Math.Max(itemsToChange.Count() / 50, 1));
			foreach (int hvo in itemsToChange)
			{
				i++;
				if (i % interval == 0)
				{
					state.PercentDone = i * 100 / itemsToChange.Count();
					state.Breath();
				}
				bool fEnable = OkToChange(hvo);
				if (fEnable)
					m_sda.SetString(hvo, tagFakeFlid, NewValue(hvo));
				m_sda.SetInt(hvo, tagEnable, (fEnable ? 1 : 0));
			}
		}

		public void Doit(IEnumerable<int> itemsToChange, ProgressState state)
		{
			m_sda.BeginUndoTask(XMLViewsStrings.ksUndoBulkEdit, XMLViewsStrings.ksRedoBulkEdit);
			string commitChanges = XmlUtils.GetOptionalAttributeValue(m_nodeSpec, "commitChanges");
			int i = 0;
			// Report progress 50 times or every 100 items, whichever is more (but no more than once per item!)
			int interval = Math.Min(100, Math.Max(itemsToChange.Count() / 50, 1));
			foreach (int hvo in itemsToChange)
			{
				i++;
				if (i % interval == 0)
				{
					state.PercentDone = i * 100 / itemsToChange.Count();
					state.Breath();
				}
				Doit(hvo);
				BulkEditBar.CommitChanges(hvo, commitChanges, m_cache, m_accessor.WritingSystem);
			}
			m_sda.EndUndoTask();
		}

		/// <summary>
		/// Make the change. Usually you can just override NewValue and/or OkToChange.
		/// </summary>
		/// <param name="hvo"></param>
		public virtual void Doit(int hvo)
		{
			if (OkToChange(hvo))
			{
				SetNewValue(hvo, NewValue(hvo));
			}
		}

		/// <summary>
		/// Get the old value, assuming it is some kind of string property
		/// </summary>
		/// <param name="hvo"></param>
		/// <returns></returns>
		protected ITsString OldValue(int hvo)
		{
			return m_accessor.CurrentValue(hvo);
		}

		protected void SetNewValue(int hvoItem, ITsString tss)
		{
			m_accessor.SetNewValue(hvoItem, tss);
		}

		protected virtual bool OkToChange(int hvo)
		{
			if (!String.IsNullOrEmpty(m_sEditIf) && m_wsEditIf != 0)
			{
				var co = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
				if (m_miEditIf == null)
					m_miEditIf = co.GetType().GetMethod(m_sEditIf);
				if (m_miEditIf != null)
				{
					object o = m_miEditIf.Invoke(co, new object[] { m_wsEditIf });
					if (o.GetType() == typeof(bool))
					{
						bool fAllowEdit = m_fEditIfNot ? !(bool)o : (bool)o;
						if (!fAllowEdit)
							return false;
					}
				}
			}
			return true;
		}

		protected abstract ITsString NewValue(int hvo);

		#region IGetReplacedObjects Members

		public Dictionary<int, int> ReplacedObjects
		{
			get { return m_replacedObjects; }
		}

		#endregion
	}

	internal class BulkCopyMethod : DoItMethod
	{
		FieldReadWriter m_srcAccessor;
		ITsString m_tssSep;
		NonEmptyTargetOptions m_options;

		public BulkCopyMethod(FdoCache cache, ISilDataAccessManaged sda, FieldReadWriter dstAccessor,
			XmlNode spec, FieldReadWriter srcAccessor, ITsString tssSep, NonEmptyTargetOptions options)
			: base(cache, sda, dstAccessor, spec)
		{
			m_srcAccessor = srcAccessor;
			m_tssSep = tssSep;
			m_options = options;
		}

		/// <summary>
		/// The preview looks neater if things that won't change are not shown as changing.
		/// So, only 'ok to change' if there is a difference.
		/// </summary>
		/// <param name="hvo"></param>
		/// <returns></returns>
		protected override bool OkToChange(int hvo)
		{
			if (!base.OkToChange(hvo))
				return false;
			ITsString tssOld = OldValue(hvo);
			if (m_options == NonEmptyTargetOptions.DoNothing &&
				tssOld != null && tssOld.Length != 0)
			{
				return false;		// Don't want to modify existing data.
			}
			string sOld = null;
			if (tssOld != null)
				sOld = tssOld.Text;
			ITsString tssNew = NewValue(hvo);
			string sNew = null;
			if (tssNew != null)
				sNew = tssNew.Text;
			if ((sOld == null || sOld.Length == 0) &&
				(sNew == null || sNew.Length == 0))
			{
				return false;		// They're really the same, regardless of properties.
			}
			if (sOld != sNew)
				return true;
			return !tssNew.Equals(tssOld);
		}


		protected override ITsString NewValue(int hvo)
		{
			ITsString tssNew = m_srcAccessor.CurrentValue(hvo);
			if (tssNew == null)
			{
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				tssNew = tsf.MakeString("", m_accessor.WritingSystem);
			}
			if (m_options == NonEmptyTargetOptions.Append)
			{
				ITsString tssOld = OldValue(hvo);
				if (tssOld != null && tssOld.Length != 0)
				{
					ITsStrBldr bldr = tssOld.GetBldr();
					bldr.ReplaceTsString(bldr.Length, bldr.Length, m_tssSep);
					bldr.ReplaceTsString(bldr.Length, bldr.Length, tssNew);
					tssNew = bldr.GetString();
				}
			}
			return tssNew;
		}
	}
	internal class TransduceMethod : DoItMethod
	{
		FieldReadWriter m_srcAccessor;
		ECInterfaces.IEncConverter m_converter;
		bool m_fFailed = false;
		ITsString m_tssSep;
		NonEmptyTargetOptions m_options;

		public TransduceMethod(FdoCache cache, ISilDataAccessManaged sda, FieldReadWriter dstAccessor, XmlNode spec, FieldReadWriter srcAccessor,
			ECInterfaces.IEncConverter converter, ITsString tssSep, NonEmptyTargetOptions options)
			: base(cache, sda, dstAccessor, spec)
		{
			m_srcAccessor = srcAccessor;
			m_converter = converter;
			m_tssSep = tssSep;
			m_options = options;
		}

		/// <summary>
		/// The preview looks neater if things that won't change are not shown as changing.
		/// So, only 'ok to change' if there is a difference.
		/// </summary>
		/// <param name="hvo"></param>
		/// <returns></returns>
		protected override bool OkToChange(int hvo)
		{
			if (!base.OkToChange(hvo))
				return false;
			if (m_options == NonEmptyTargetOptions.DoNothing)
			{
				ITsString tssOld = OldValue(hvo);
				if(tssOld != null && tssOld.Length != 0)
					return false;
			}
			ITsString tssSrc = m_srcAccessor.CurrentValue(hvo);
			return tssSrc != null && !m_srcAccessor.CurrentValue(hvo).Equals(NewValue(hvo));
		}


		protected override ITsString NewValue(int hvo)
		{
			ITsString tssSrc = m_srcAccessor.CurrentValue(hvo);
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			if (tssSrc == null)
			{
				tssSrc = tsf.MakeString("", m_accessor.WritingSystem);
			}
			if (m_fFailed) // once we've had a failure don't try any more this pass.
				return tssSrc;
			string old = tssSrc.Text;
			string converted = "";
			if (old != null && old.Length != 0)
			{
				try
				{
					converted = m_converter.Convert(old);
				}
				catch (Exception except)
				{
					MessageBox.Show(String.Format(XMLViewsStrings.ksErrorProcessing, old, except.Message));
					m_fFailed = true;
					return tssSrc;
				}
			}
			ITsString tssNew = tsf.MakeString(converted, m_accessor.WritingSystem);
			if (m_options == NonEmptyTargetOptions.Append)
			{
				ITsString tssOld = OldValue(hvo);
				if (tssOld != null && tssOld.Length != 0)
				{
					ITsStrBldr bldr = tssOld.GetBldr();
					bldr.ReplaceTsString(bldr.Length, bldr.Length, m_tssSep);
					bldr.ReplaceTsString(bldr.Length, bldr.Length, tssNew);
					tssNew = bldr.GetString();
				}
			}
			return tssNew;
		}
	}
	/// <summary>
	/// Implement FakeDoIt by replacing whatever matches the pattern with the result.
	/// </summary>
	internal class ReplaceWithMethod : DoItMethod
	{

		IVwPattern m_pattern;
		ITsString m_replacement;
		IVwTxtSrcInit m_textSourceInit;
		IVwTextSource m_ts;

		public ReplaceWithMethod(FdoCache cache, ISilDataAccessManaged sda, FieldReadWriter accessor, XmlNode spec, IVwPattern pattern, ITsString replacement)
			: base(cache, sda, accessor, spec)
		{
			m_pattern = pattern;
			m_replacement = replacement;
			m_pattern.ReplaceWith = m_replacement;
			m_textSourceInit = VwStringTextSourceClass.Create();
			m_ts = m_textSourceInit as IVwTextSource;
		}

		/// <summary>
		/// We can do a replace if the pattern matches.
		/// </summary>
		/// <param name="hvo"></param>
		/// <returns></returns>
		protected override bool OkToChange(int hvo)
		{
			if (!base.OkToChange(hvo))
				return false;
			ITsString tss = OldValue(hvo);
			if (tss == null)
			{
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				tss = tsf.MakeString("", m_accessor.WritingSystem);
			}
			m_textSourceInit.SetString(tss);
			int ichMin, ichLim;
			m_pattern.FindIn(m_ts, 0, tss.Length, true, out ichMin, out ichLim, null);
			return ichMin >= 0;
		}

		/// <summary>
		/// Actually produce the replacement string.
		/// </summary>
		/// <param name="hvo"></param>
		/// <returns></returns>
		protected override ITsString NewValue(int hvo)
		{
			ITsString tss = OldValue(hvo);
			if (tss == null)
			{
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				tss = tsf.MakeString("", m_accessor.WritingSystem);
			}
			m_textSourceInit.SetString(tss);
			int ichMin, ichLim;
			int ichStartSearch = 0;
			ITsStrBldr tsb = null;
			int delta = 0; // Amount added to length of string (negative if shorter).
			int cch = tss.Length;
			// Some tricky stuff going on here. We allow a match ONCE where ichStartSearch = cch,
			// because some patterns match an empty string, and (once) we want to allow that.
			// But we must not allow it repeatedly, because we can get an infinite sequence
			// of replacements if ichLim comes out of FindIn equal to ichStartSearch.
			// Also, normally we want a pattern which matched, say, chars 1 and 2 to also
			// be able to match chars 3 and 4. But we don't want one which matched 1 and 2
			// to also match the empty position between 2 and 3. Or, for example, ".*" to
			// match the whole input, and then again the empty string at the end.
			// To achieve this, we allow each match to start where the last one ended,
			// but a match of zero length that ends exactly where a previous match
			// ended is discarded.
			int ichLimLastMatch = -1;
			for ( ; ichStartSearch <= cch; )
			{
				m_pattern.FindIn(m_ts, ichStartSearch, cch, true, out ichMin, out ichLim, null);
				if (ichMin < 0)
					break;
				if (ichLim == ichLimLastMatch)
				{
					ichStartSearch = ichLim + 1;
					continue;
				}
				ichLimLastMatch = ichLim;
				if (tsb == null)
					tsb = tss.GetBldr();
				ITsString tssRep = m_pattern.ReplacementText;
				tsb.ReplaceTsString(ichMin + delta, ichLim + delta, tssRep);
				delta += tssRep.Length - (ichLim - ichMin);
				ichStartSearch = ichLim;
			}
			if (tsb == null)
				return null;
			return tsb.GetString().get_NormalizedForm(FwNormalizationMode.knmNFD);
		}

		/// <summary>
		/// This is very like the base Doit, but we can save a duplicate pattern search
		/// by calling the BASE version of OkToChange rather than our own version, which
		/// tests for at least one match. We DO need to call the base version, e.g., so
		/// we don't change wordforms which shouldn't change because they are in use.
		/// </summary>
		/// <param name="hvo"></param>
		public override void Doit(int hvo)
		{
			if (!base.OkToChange(hvo))
				return;
			ITsString tss = NewValue(hvo);
			if (tss != null)
				SetNewValue(hvo, tss);
		}
	}
	/// <summary>
	/// Implement (Fake)DoIt by replacing the current value with an empty string.
	/// </summary>
	internal class ClearMethod : DoItMethod
	{
		ITsString m_newValue;
		public ClearMethod(FdoCache cache, ISilDataAccessManaged sda, FieldReadWriter accessor, XmlNode spec)
			: base(cache, sda, accessor, spec)
		{
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			m_newValue = tsf.MakeString("", accessor.WritingSystem);
		}

		/// <summary>
		/// We can do a replace if the current value is not empty.
		/// </summary>
		/// <param name="hvo"></param>
		/// <returns></returns>
		protected override bool OkToChange(int hvo)
		{
			if (!base.OkToChange(hvo))
				return false;
			ITsString tss = OldValue(hvo);
			return (tss != null && tss.Length != 0);
		}

		/// <summary>
		/// Actually produce the replacement string.
		/// </summary>
		/// <param name="hvo"></param>
		/// <returns></returns>
		protected override ITsString NewValue(int hvo)
		{
			return m_newValue;
		}
	}

	/// <summary>
	/// Class representing the information we know about an item in a target/source field combo
	/// </summary>
	internal class FieldComboItem : IDisposable
	{
		private string m_name; // string to show in menu

		public FieldComboItem(string name, int icol, FieldReadWriter accessor)
		{
			m_name = name;
			ColumnIndex = icol;
			Accessor = accessor;
		}

		#region Disposable stuff
#if DEBUG
		/// <summary/>
		~FieldComboItem()
		{
			Dispose(false);
		}
#endif

		/// <summary/>
		public bool IsDisposed { get; private set; }

		/// <summary/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		protected virtual void Dispose(bool fDisposing)
		{
			Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType() + " *******");
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects
				var disposable = Accessor as IDisposable;
				if (disposable != null)
					disposable.Dispose();
			}
			IsDisposed = true;
		}
		#endregion

		public override string ToString()
		{
			return m_name;
		}

		/// <summary>
		/// Gets the column index to show preview.
		/// </summary>
		public int ColumnIndex { get; private set; }

		/// <summary>
		/// Gets the thing that can read/write strings.
		/// </summary>
		public FieldReadWriter Accessor { get; private set; }
	}

	class IntChooserBEditControl : BulkEditSpecControl, IGetReplacedObjects
	{
		#region IBulkEditSpecControl Members
		protected int m_flid;
		protected ComboBox m_combo;
		Dictionary<int, int> m_replacedObjects = new Dictionary<int, int>();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialized with a string like "0:no;1:yes".
		/// </summary>
		/// <param name="itemList">The item list.</param>
		/// <param name="flid">The flid.</param>
		/// ------------------------------------------------------------------------------------
		public IntChooserBEditControl(string itemList, int flid)
		{
			m_flid = flid;
			m_combo = new ComboBox();
			m_combo.DropDownStyle = ComboBoxStyle.DropDownList;
			foreach (string pair in itemList.Split(';'))
			{
				string[] vals = pair.Trim().Split(':');
				if (vals.Length != 2)
					throw new Exception("IntChooserBEditControl values must look like n:name");
				int val = Int32.Parse(vals[0]);
				m_combo.Items.Add(new IntComboItem(vals[1].Trim(), val));
			}
			if (m_combo.Items.Count == 0)
				throw new Exception("IntChooserBEditControl created with zero items");
			m_combo.SelectedIndex = 0;
			m_combo.SelectedIndexChanged +=new EventHandler(m_combo_SelectedIndexChanged);
		}

		/// <summary>
		/// Initialized with a list of strings; first signifies 0, next 1, etc.
		/// </summary>
		/// <param name="itemList"></param>
		/// <param name="flid"></param>
		/// <param name="initialIndexToSelect">Index of one of the items in the combo box, the most useful choice that should
		/// initially be selected. Comes from defaultBulkEditChoice attribute on [column] element in XML spec. Default 0.</param>
		public IntChooserBEditControl(string[] itemList, int flid, int initialIndexToSelect)
		{
			m_flid = flid;
			m_combo = new ComboBox();
			m_combo.DropDownStyle = ComboBoxStyle.DropDownList;
			int index = 0;
			foreach (string name in itemList)
			{
				m_combo.Items.Add(new IntComboItem(name, index++));
			}
			if (m_combo.Items.Count == 0)
				throw new Exception("IntChooserBEditControl created with zero items");
			m_combo.SelectedIndex = Math.Max(0, Math.Min(m_combo.Items.Count - 1, initialIndexToSelect));
			m_combo.SelectedIndexChanged += new EventHandler(m_combo_SelectedIndexChanged);
		}

		protected void m_combo_SelectedIndexChanged(object sender, EventArgs e)
		{
			// Tell the parent control that we may have changed the selected item so it can
			// enable or disable the Apply and Preview buttons based on the selection.
			int index = m_combo.SelectedIndex;
			int hvo = 0;  // we need a dummy hvo value to pass since IntChooserBEditControl
			//displays 'yes' or 'no' which have no hvo
			OnValueChanged(sender, new FwObjectSelectionEventArgs(hvo, index));
		}

		public override Control Control
		{
			get { return m_combo; }
		}

		public override void DoIt(IEnumerable<int> itemsToChange, ProgressState state)
		{
			m_cache.DomainDataByFlid.BeginUndoTask(XMLViewsStrings.ksUndoBulkEdit, XMLViewsStrings.ksRedoBulkEdit);
			ISilDataAccess sda = m_cache.DomainDataByFlid;
			m_replacedObjects.Clear();

			int val = (m_combo.SelectedItem as IntComboItem).Value;
			int i = 0;
			// Report progress 50 times or every 100 items, whichever is more (but no more than once per item!)
			int interval = Math.Min(100, Math.Max(itemsToChange.Count() / 50, 1));
			var mdcManaged = m_cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();
			foreach (int hvoItem in itemsToChange)
			{
				i++;
				if (i % interval == 0)
				{
					state.PercentDone = i * 100 / itemsToChange.Count();
					state.Breath();
				}
				// If the field is on an owned object that might not exist, we don't want to create
				// that owned object just because we're changing the values involved.
				// (See FWR-3199 for an example of such a situation.)
				int clid = m_sda.get_IntProp(hvoItem, CmObjectTags.kflidClass);
				var flids = mdcManaged.GetFields(clid, true, (int)CellarPropertyTypeFilter.All);
				if (!flids.Contains(m_flid))
					continue;
				int valOld;
				if (TryGetOriginalListValue(sda, hvoItem, out valOld) && valOld == val)
					continue;
				UpdateListItemToNewValue(sda, hvoItem, val, valOld);
				// Enhance JohnT: maybe eventually we will want to make a more general mechanism for doing this,
				// e.g., specify a method to call using the XML configuration for the column?
				if (m_flid == WfiWordformTags.kflidSpellingStatus)
					FixSpellingStatus(hvoItem, val);
			}
			m_cache.DomainDataByFlid.EndUndoTask();
		}

		protected override void UpdateListItemToNewValue(ISilDataAccess sda, int hvoItem, int newVal, int oldVal)
		{
			int hvoOwningInt = hvoItem;
			if (m_ghostParentHelper != null)
			{
				// it's possible that hvoItem is actually the owner of a object that needs to be created
				// for hvoSel to be set.
				hvoOwningInt = m_ghostParentHelper.FindOrCreateOwnerOfTargetProp(hvoItem, m_flid);
			}
			SetBasicPropertyValue(sda, newVal, hvoOwningInt);
		}

		protected virtual void SetBasicPropertyValue(ISilDataAccess sda, int newVal, int hvoOwner)
		{
			int type = m_sda.MetaDataCache.GetFieldType(m_flid);
			if (type == (int)CellarPropertyType.Boolean)
				sda.SetBoolean(hvoOwner, m_flid, newVal != 0);
			else
				sda.SetInt(hvoOwner, m_flid, newVal);
		}

		void FixSpellingStatus(int hvoItem, int val)
		{
			int defVernWS = m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle;
			ITsString tss = m_cache.DomainDataByFlid.get_MultiStringAlt(hvoItem,
				WfiWordformTags.kflidForm,
				defVernWS);
			if (tss == null || tss.Length == 0)
				return; // probably can't happen?
			EnchantHelper.SetSpellingStatus(tss.Text,
				defVernWS,
				m_cache.WritingSystemFactory,
				((int)val == (int)SpellingStatusStates.correct));
		}

		public override void FakeDoit(IEnumerable<int> itemsToChange, int tagFakeFlid, int tagEnabled, ProgressState state)
		{
			int val = ((IntComboItem) m_combo.SelectedItem).Value;
			ITsString tssVal = m_cache.TsStrFactory.MakeString(m_combo.SelectedItem.ToString(),
				m_cache.ServiceLocator.WritingSystemManager.UserWs);
			int i = 0;
			// Report progress 50 times or every 100 items, whichever is more
			// (but no more than once per item!)
			int interval = Math.Min(100, Math.Max(itemsToChange.Count() / 50, 1));
			var mdcManaged = m_cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();
			int type = m_sda.MetaDataCache.GetFieldType(m_flid);
			foreach (int hvoItem in itemsToChange)
			{
				i++;
				if (i % interval == 0)
				{
					state.PercentDone = i * 100 / itemsToChange.Count();
					state.Breath();
				}
				bool fEnable;
				// If the field is on an owned object that might not exist, the hvoItem might
				// refer to the owner, which is likely of a different class.  In such cases,
				// we don't want to try getting the field value, since that produces a pretty
				// green dialog box for the user.  See FWR-3199.
				int clid = m_sda.get_IntProp(hvoItem, CmObjectTags.kflidClass);
				var flids = mdcManaged.GetFields(clid, true, (int)CellarPropertyTypeFilter.All);
				if (!flids.Contains(m_flid))
					fEnable = false;
				else if (type == (int)CellarPropertyType.Boolean)
					fEnable = m_sda.get_BooleanProp(hvoItem, m_flid) != (val != 0);
				else
					fEnable = m_sda.get_IntProp(hvoItem, m_flid) != val;
				if (fEnable)
					m_sda.SetString(hvoItem, tagFakeFlid, tssVal);
				m_sda.SetInt(hvoItem, tagEnabled, (fEnable ? 1 : 0));
			}
		}

		protected override bool TryGetOriginalListValue(ISilDataAccess sda, int hvoItem, out int value)
		{
			value = Int32.MinValue;
			int hvoOwningInt = hvoItem;
			if (m_ghostParentHelper != null)
				hvoOwningInt = m_ghostParentHelper.GetOwnerOfTargetProperty(hvoItem);
			if (hvoOwningInt == 0)
				return false;
			value = GetBasicPropertyValue(sda, hvoOwningInt);
			return true;
		}

		protected virtual int GetBasicPropertyValue(ISilDataAccess sda, int hvoOwner)
		{
			int type = m_sda.MetaDataCache.GetFieldType(m_flid);
			if (type == (int)CellarPropertyType.Boolean)
				return sda.get_BooleanProp(hvoOwner, m_flid) ? 1 : 0;
			else
				return sda.get_IntProp(hvoOwner, m_flid);
		}

		public override bool CanClearField
		{
			get { return false; }
		}

		public override void SetClearField()
		{
			throw new Exception("The IntChooserBEditControl.SetClearField() method is not implemented.");
		}

		public override List<int> FieldPath
		{
			get { return new List<int>(new int[] { m_flid }); }
		}

		#endregion

		#region IGetReplacedObjects Members

		/// <summary>
		/// Objects get replaced here when dummies are changed to real.
		/// </summary>
		public Dictionary<int, int> ReplacedObjects
		{
			get { return m_replacedObjects; }
		}

		#endregion
	}

	class BooleanChooserBEditControl : IntChooserBEditControl
	{
		internal BooleanChooserBEditControl(string itemList, int flid)
			: base(itemList, flid)
		{}

		protected override int GetBasicPropertyValue(ISilDataAccess sda, int hvoOwner)
		{
			return Convert.ToInt32(IntBoolPropertyConverter.GetBoolean(m_sda, hvoOwner, m_flid));
		}

		protected override void SetBasicPropertyValue(ISilDataAccess sda, int newVal, int hvoOwner)
		{
			Debug.Assert(newVal == 0 || newVal == 1, String.Format("Expected value {0} to be boolean.", newVal));
			IntBoolPropertyConverter.SetValueFromBoolean(m_sda, hvoOwner, m_flid, Convert.ToBoolean(newVal));
		}
	}

	internal class IntOnSubfieldChooserBEditControl : IntChooserBEditControl
	{
		protected int m_flidSub;
		/// <summary>
		/// Initialized with a string like "0:no;1:yes".
		/// </summary>
		/// <param name="itemList"></param>
		/// <param name="flid">main field</param>
		/// <param name="flidSub">subfield</param>
		public IntOnSubfieldChooserBEditControl(string itemList, int flid, int flidSub)
			: base(itemList, flid)
		{
			m_flidSub = flidSub;
		}

		public override List<int> FieldPath
		{
			get
			{
				if (m_flidSub == LexEntryRefTags.kflidHideMinorEntry)
				{
					return new List<int>(new int[] { m_flidSub });
				}
				else
				{
					List<int> fieldPath = base.FieldPath;
					fieldPath.Add(m_flidSub);
					return fieldPath;
				}
			}

		}

		public override void FakeDoit(IEnumerable<int> itemsToChange, int tagFakeFlid, int tagEnabled, ProgressState state)
		{
			int val = (m_combo.SelectedItem as IntComboItem).Value;
			ITsString tssVal = TsStringUtils.MakeTss(m_combo.SelectedItem.ToString(), m_cache.DefaultUserWs);
			int i = 0;
			// Report progress 50 times or every 100 items, whichever is more
			// (but no more than once per item!)
			int interval = Math.Min(100, Math.Max(itemsToChange.Count() / 50, 1));
			if (m_flidSub == LexEntryRefTags.kflidHideMinorEntry)
			{
				// we present this to the user as "Show" instead of "Hide"
				if (val == 0)
					val = 1;
				else
					val = 0;
				foreach (int hvoItem in itemsToChange)
				{
					i++;
					if (i % interval == 0)
					{
						state.PercentDone = i * 100 / itemsToChange.Count();
						state.Breath();
					}
					Debug.Assert(m_sda.get_IntProp(hvoItem, CmObjectTags.kflidClass) == LexEntryRefTags.kClassId);
					int valOld = m_sda.get_IntProp(hvoItem, m_flidSub);
					bool fEnable = valOld != val;
					if (fEnable)
						m_sda.SetString(hvoItem, tagFakeFlid, tssVal);
					m_sda.SetInt(hvoItem, tagEnabled, (fEnable ? 1 : 0));
				}
			}
			else
			{
				foreach (int hvoItem in itemsToChange)
				{
					i++;
					if (i % interval == 0)
					{
						state.PercentDone = i * 100 / itemsToChange.Count();
						state.Breath();
					}
					int hvoField = m_sda.get_ObjectProp(hvoItem, m_flid);
					if (hvoField == 0)
						continue;
					int valOld = GetValueOfField(m_sda, hvoField);
					bool fEnable = valOld != val;
					if (fEnable)
						m_sda.SetString(hvoItem, tagFakeFlid, tssVal);
					m_sda.SetInt(hvoItem, tagEnabled, (fEnable ? 1 : 0));
				}
			}
		}

		internal virtual int GetValueOfField(ISilDataAccess sda, int hvoField)
		{
			return sda.get_IntProp(hvoField, m_flidSub);
		}

		public override void DoIt(IEnumerable<int> itemsToChange, ProgressState state)
		{
			m_cache.DomainDataByFlid.BeginUndoTask(XMLViewsStrings.ksUndoBulkEdit, XMLViewsStrings.ksRedoBulkEdit);
			ISilDataAccess sda = m_cache.DomainDataByFlid;

			int val = (m_combo.SelectedItem as IntComboItem).Value;
			int i = 0;
			// Report progress 50 times or every 100 items, whichever is more (but no more than once per item!)
			int interval = Math.Min(100, Math.Max(itemsToChange.Count() / 50, 1));
			if (m_flidSub == LexEntryRefTags.kflidHideMinorEntry)
			{
				// we present this to the user as "Show" instead of "Hide"
				if (val == 0)
					val = 1;
				else
					val = 0;
				foreach (int hvoItem in itemsToChange)
				{
					i++;
					if (i % interval == 0)
					{
						state.PercentDone = i * 100 / itemsToChange.Count();
						state.Breath();
					}
					Debug.Assert(m_sda.get_IntProp(hvoItem, CmObjectTags.kflidClass) == LexEntryRefTags.kClassId);
					int valOld = m_sda.get_IntProp(hvoItem, m_flidSub);
					if (valOld != val)
						sda.SetInt(hvoItem, m_flidSub, val);
				}
			}
			else
			{
				foreach (int hvoItem in itemsToChange)
				{
					i++;
					if (i % interval == 0)
					{
						state.PercentDone = i * 100 / itemsToChange.Count();
						state.Breath();
					}
					int hvoField = sda.get_ObjectProp(hvoItem, m_flid);
					if (hvoField == 0)
						continue;
					int valOld = GetValueOfField(sda, hvoField);
					if (valOld == val)
						continue;
					SetValueOfField(sda, hvoField, val);
				}
			}
			m_cache.DomainDataByFlid.EndUndoTask();
		}

		internal virtual void SetValueOfField(ISilDataAccess sda, int hvoField, int val)
		{
			sda.SetInt(hvoField, m_flidSub, val);
		}
	}

	/// <summary>
	/// This adapts IntOnSubfield chooser to use 0 for a boolean false and 1 for a boolean true
	/// </summary>
	internal class BoolOnSubfieldChooserBEditControl : IntOnSubfieldChooserBEditControl
	{
		public BoolOnSubfieldChooserBEditControl(string itemList, int flid, int flidSub) : base(itemList, flid, flidSub)
		{
			if (m_combo.Items.Count != 2)
				throw new ArgumentException("BoolOnSubfieldChooserBEditControl must be created with a two-item list of options");
		}

		internal override int GetValueOfField(ISilDataAccess sda, int hvoField)
		{
			return sda.get_BooleanProp(hvoField, m_flidSub) ? 1 : 0;
		}

		internal override void SetValueOfField(ISilDataAccess sda, int hvoField, int val)
		{
			sda.SetBoolean(hvoField, m_flidSub, val == 1);
		}
	}

	internal class IntComboItem
	{
		string m_name;
		int m_val;
		public IntComboItem(string name, int val)
		{
			m_name = name;
			m_val = val;
		}

		public int Value
		{
			get { return m_val; }
		}

		public override string ToString()
		{
			return m_name;
		}
	}

	abstract class BulkEditSpecControl : IBulkEditSpecControl, IGhostable
	{
		protected Mediator m_mediator;
		protected FdoCache m_cache;
		protected XMLViewsDataCache m_sda;
		protected GhostParentHelper m_ghostParentHelper;
		public event FwSelectionChangedEventHandler ValueChanged;

		#region IBulkEditSpecControl Members

		public Mediator Mediator
		{
			get { return m_mediator; }
			set { m_mediator = value; }
		}

		public FdoCache Cache
		{
			get { return m_cache; }
			set { m_cache = value; }
		}

		/// <summary>
		/// The special cache that can handle the preview and check-box properties.
		/// </summary>
		public XMLViewsDataCache DataAccess
		{
			get
			{
				if (m_sda == null)
					throw new InvalidOperationException("Must set the special cache of a BulkEditSpecControl");
				return m_sda;
			}
			set { m_sda = value; }
		}

		public virtual Control Control
		{
			get { throw new Exception("The method or operation is not implemented."); }
		}

		/// <summary>
		/// Required interface member not yet used.
		/// </summary>
		public virtual IVwStylesheet Stylesheet
		{
			set {  }
		}

		public virtual void DoIt(IEnumerable<int> itemsToChange, ProgressState state)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public virtual void FakeDoit(IEnumerable<int> itemsToChange, int tagFakeFlid, int tagEnabled, ProgressState state)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public virtual bool CanClearField
		{
			get { throw new Exception("The method or operation is not implemented."); }
		}

		public virtual void SetClearField()
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public virtual List<int> FieldPath
		{
			get { throw new Exception("The method or operation is not implemented."); }
		}

		protected void OnValueChanged(object sender, FwObjectSelectionEventArgs args)
		{
			if (ValueChanged != null)
				ValueChanged(sender, args);
		}

		#endregion

		#region IGhostable Members

		public virtual void InitForGhostItems(FdoCache cache, XmlNode colSpec)
		{
			m_ghostParentHelper = BulkEditBar.GetGhostHelper(cache.ServiceLocator, colSpec);
		}

		/// <summary>
		/// It's possible that hvoItem will be an owner of a ghost (ie. an absent source object for m_flidAtomicProp).
		/// In that case, the cache should return 0.
		/// </summary>
		/// <param name="sda"></param>
		/// <param name="hvoItem"></param>
		/// <param name="value">the original list value, if we could get at one.</param>
		/// <returns>false if we couldn't get the list value (e.g. if we need to create an item to get the value)</returns>
		protected virtual bool TryGetOriginalListValue(ISilDataAccess sda, int hvoItem, out int value)
		{
			value = Int32.MinValue;
			return false; // override
		}
		protected virtual void UpdateListItemToNewValue(ISilDataAccess sda, int hvoItem, int newVal, int oldVal)
		{
			return; // override
		}

		#endregion

		#region IBulkEditSpecControl Members



		#endregion
	}

	/// <summary>
	/// This class implements setting an atomic reference property to a value chosen from a flat list,
	/// by means of a combo box showing the items in the list.
	/// </summary>
	class FlatListChooserBEditControl : IBulkEditSpecControl, IGhostable, IDisposable
	{
		protected Mediator m_mediator;
		protected FdoCache m_cache;
		protected XMLViewsDataCache m_sda;
		protected FwComboBox m_combo;
		protected int m_ws;
		protected int m_hvoList;
		protected bool m_useAbbr;
		protected int m_flidAtomicProp;
		internal IVwStylesheet m_stylesheet;
		GhostParentHelper m_ghostParentHelper;

		public event FwSelectionChangedEventHandler ValueChanged;

		public FlatListChooserBEditControl(int flidAtomicProp, int hvoList, int ws, bool useAbbr)
		{
			m_ws = ws;
			m_hvoList = hvoList;
			m_useAbbr = useAbbr;
			m_flidAtomicProp = flidAtomicProp;
		}

		#region IBulkEditSpecControl Members

		public Mediator Mediator
		{
			get { return m_mediator; }
			set	{ m_mediator = value; }
		}

		public Control Control
		{
			get
			{
				if (m_combo == null)
					FillComboBox();
				return m_combo;
			}
		}

		public FdoCache Cache
		{
			get
			{
				return m_cache;
			}
			set
			{
				m_cache = value;
			}
		}

		/// <summary>
		/// The special cache that can handle the preview and check-box properties.
		/// </summary>
		public XMLViewsDataCache DataAccess
		{
			get
			{
				if (m_sda == null)
					throw new InvalidOperationException("Must set the special cache of a BulkEditSpecControl");
				return m_sda;
			}
			set { m_sda = value; }
		}
		public IVwStylesheet Stylesheet
		{
			set
			{
				m_stylesheet = value;
				if (m_combo != null)
					m_combo.StyleSheet = value;
			}
		}

		public virtual void DoIt(IEnumerable<int> itemsToChange, ProgressState state)
		{
			UndoableUnitOfWorkHelper.Do(XMLViewsStrings.ksUndoBulkEdit, XMLViewsStrings.ksRedoBulkEdit,
				m_cache.ActionHandlerAccessor, () =>
			{
				ISilDataAccess sda = m_cache.DomainDataByFlid;

				HvoTssComboItem item = m_combo.SelectedItem as HvoTssComboItem;
				if (item == null)
					return;
				int hvoSel = item.Hvo;
				var mdcManaged = m_cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();
				int i = 0;
				// Report progress 50 times or every 100 items, whichever is more (but no more than once per item!)
				int interval = Math.Min(100, Math.Max(itemsToChange.Count() / 50, 1));
				foreach (int hvoItem in itemsToChange)
				{
					i++;
					if (i % interval == 0)
					{
						state.PercentDone = i * 100 / itemsToChange.Count();
						state.Breath();
					}
					// If the field is on an owned object that might not exist, the hvoItem might
					// refer to the owner, which is likely of a different class.  In such cases,
					// we don't want to try getting the field value, since that produces a pretty
					// green dialog box for the user.  See FWR-3199.
					int clid = m_sda.get_IntProp(hvoItem, CmObjectTags.kflidClass);
					var flids = mdcManaged.GetFields(clid, true, (int)CellarPropertyTypeFilter.All);
					if (!flids.Contains(m_flidAtomicProp))
						continue;
					int hvoOld = GetOriginalListValue(sda, hvoItem);
					if (hvoOld == hvoSel)
						continue;
					UpdateListItemToNewValue(sda, hvoItem, hvoSel, hvoOld);
				}
			});
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="sda"></param>
		/// <param name="hvoSel"></param>
		/// <param name="hvoItem"></param>
		/// <param name="hvoOld"></param>
		protected virtual void UpdateListItemToNewValue(ISilDataAccess sda, int hvoItem, int hvoSel, int hvoOld)
		{
			int hvoOwningAtomic = hvoItem;
			if (hvoOld == 0 && m_ghostParentHelper != null)
			{
				// it's possible that hvoItem is actually the owner of a object that needs to be created
				// for hvoSel to be set.
				hvoOwningAtomic = m_ghostParentHelper.FindOrCreateOwnerOfTargetProp(hvoItem, m_flidAtomicProp);
			}
			sda.SetObjProp(hvoOwningAtomic, m_flidAtomicProp, hvoSel);
		}

		/// <summary>
		/// This is called when the preview button is clicked. The control is passed
		/// the list of currently active (filtered and checked) items. It should cache
		/// tagEnabled to zero for any objects that can't be
		/// modified. For ones that can, it should set the string property tagFakeFlid
		/// to the value to show in the 'modified' fields.
		/// </summary>
		/// <param name="itemsToChange">The items to change.</param>
		/// <param name="tagFakeFlid">The tag fake flid.</param>
		/// <param name="tagEnabled">The tag enabled.</param>
		/// <param name="state">The state.</param>
		/// ------------------------------------------------------------------------------------
		/// ------------------------------------------------------------------------------------
		public virtual void FakeDoit(IEnumerable<int> itemsToChange, int tagFakeFlid, int tagEnabled,
			ProgressState state)
		{
			ISilDataAccess sda = m_cache.DomainDataByFlid;
			HvoTssComboItem item = m_combo.SelectedItem as HvoTssComboItem;
			if (item == null)
				return;
			int hvoSel = item.Hvo;
			var mdcManaged = m_cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();
			int i = 0;
			// Report progress 50 times or every 100 items, whichever is more
			// (but no more than once per item!)
			int interval = Math.Min(100, Math.Max(itemsToChange.Count() / 50, 1));
			foreach(int hvoItem in itemsToChange)
			{
				i++;
				if (i % interval == 0)
				{
					state.PercentDone = i * 100 / itemsToChange.Count();
					state.Breath();
				}
				bool fEnable;
				// If the field is on an owned object that might not exist, the hvoItem might
				// refer to the owner, which is likely of a different class.  In such cases,
				// we don't want to try getting the field value, since that produces a pretty
				// green dialog box for the user.  See FWR-3199.
				int clid = m_sda.get_IntProp(hvoItem, CmObjectTags.kflidClass);
				var flids = mdcManaged.GetFields(clid, true, (int)CellarPropertyTypeFilter.All);
				if (!flids.Contains(m_flidAtomicProp))
				{
					fEnable = false;
				}
				else
				{
					int hvoOld = GetOriginalListValue(sda, hvoItem);
					fEnable = hvoOld != hvoSel;
				}
				if (fEnable)
					m_sda.SetString(hvoItem, tagFakeFlid, item.AsTss);
				m_sda.SetInt(hvoItem, tagEnabled, (fEnable ? 1 : 0));
			}
		}

		/// <summary>
		/// It's possible that hvoItem will be an owner of a ghost (ie. an absent source object for m_flidAtomicProp).
		/// In that case, the cache should return 0.
		/// </summary>
		/// <param name="sda"></param>
		/// <param name="hvoItem"></param>
		/// <returns></returns>
		private int GetOriginalListValue(ISilDataAccess sda, int hvoItem)
		{
			return sda.get_ObjectProp(hvoItem, m_flidAtomicProp);
		}

		#endregion

		protected virtual void FillComboBox()
		{
			m_combo = new FwComboBox();
			m_combo.DropDownStyle = ComboBoxStyle.DropDownList;
			m_combo.WritingSystemFactory = m_cache.WritingSystemFactory;
			m_combo.WritingSystemCode = m_ws;
			m_combo.StyleSheet = m_stylesheet;
			List<HvoLabelItem> al = GetLabeledList();
			// if the possibilities list IsSorted (misnomer: needs to be sorted), do that now.
			if (al.Count > 1) // could be zero if list non-existant, if 1 don't need to sort either!
			{
				if (m_cache.ServiceLocator.GetInstance<ICmPossibilityListRepository>().GetObject(m_hvoList).IsSorted)
					al.Sort();
			}
			// now add list to combo box in that order.
			for (int i = 0; i < al.Count; ++i)
			{
				HvoLabelItem hli = al[i];
				m_combo.Items.Add(new HvoTssComboItem(hli.Hvo, hli.TssLabel));
			}
			// Don't allow <Not Sure> for MorphType selection.  See FWR-1632.
			if (m_hvoList != m_cache.LangProject.LexDbOA.MorphTypesOA.Hvo)
				m_combo.Items.Add(new HvoTssComboItem(0, m_cache.TsStrFactory.MakeString(XMLViewsStrings.ksNotSure, m_cache.WritingSystemFactory.UserWs)));
			m_combo.SelectedIndexChanged += new EventHandler(m_combo_SelectedIndexChanged);
		}

		private List<HvoLabelItem> GetLabeledList()
		{
			int chvo;
			int tagName = m_useAbbr ?
							CmPossibilityTags.kflidAbbreviation :
							CmPossibilityTags.kflidName;
			int flidItems = CmPossibilityListTags.kflidPossibilities;
			if (m_hvoList > 0)
				chvo = m_cache.DomainDataByFlid.get_VecSize(m_hvoList, flidItems);
			else
				chvo = 0;
			List<HvoLabelItem> al = new List<HvoLabelItem>(chvo);
			for (int i = 0; i < chvo; i++)
			{
				int hvoChild = m_cache.DomainDataByFlid.get_VecItem(m_hvoList, flidItems, i);
				al.Add(new HvoLabelItem(hvoChild, GetItemLabel(hvoChild, tagName)));
			}
			return al;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the item label.
		/// </summary>
		/// <param name="hvoChild">The hvo child.</param>
		/// <param name="tagName">Name of the tag.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		internal ITsString GetItemLabel(int hvoChild, int tagName)
		{
			// Try getting the label with the user writing system.
			ITsString tssLabel =
				m_cache.DomainDataByFlid.get_MultiStringAlt(hvoChild, tagName, m_ws);

			// If that doesn't work, try using the default user writing system.
			if (tssLabel == null || string.IsNullOrEmpty(tssLabel.Text))
			{
				tssLabel = m_cache.DomainDataByFlid.get_MultiStringAlt(hvoChild,
					tagName, m_cache.ServiceLocator.WritingSystemManager.UserWs);
			}

			// If that doesn't work, then fallback to the whatever the cache considers
			// to be the fallback writing system (probably english).
			if (tssLabel == null || string.IsNullOrEmpty(tssLabel.Text))
			{
				tssLabel = m_cache.DomainDataByFlid.get_MultiStringAlt(hvoChild,
					tagName, WritingSystemServices.FallbackUserWs(m_cache));
			}

			return tssLabel;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the SelectedIndexChanged event of the m_combo control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected void m_combo_SelectedIndexChanged(object sender, EventArgs e)
		{
			// Tell the parent control that we may have changed the selected item so it can
			// enable or disable the Apply and Preview buttons based on the selection.
			if (ValueChanged != null)
			{
				int index = m_combo.SelectedIndex;
				HvoTssComboItem htci = m_combo.SelectedItem as HvoTssComboItem;
				int hvo = htci != null ? htci.Hvo : 0;
				ValueChanged(sender, new FwObjectSelectionEventArgs(hvo, index));
			}
		}

		/// <summary>
		/// This type of editor can always select null.
		/// </summary>
		public bool  CanClearField
		{
			get { return true; }
		}

		/// <summary>
		/// And does it by choosing the final, 'Not sure' item.
		/// </summary>
		public void  SetClearField()
		{
			if (m_combo == null)
				FillComboBox();
			m_combo.SelectedIndex = m_combo.Items.Count - 1;
		}

		public virtual List<int> FieldPath
		{
			get { return new List<int>(new int[] { m_flidAtomicProp }); }
		}

		#region Disposable stuff
		#if DEBUG
		/// <summary/>
		~FlatListChooserBEditControl()
		{
			System.Diagnostics.Debug.WriteLine("****** Missing Dispose() call for " + GetType().ToString() + " *******");
			Dispose(false);
		}
		#endif

		/// <summary/>
		public bool IsDisposed { get; private set; }

		/// <summary/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects
				if (m_combo != null)
					m_combo.Dispose();
			}
			m_combo = null;
			IsDisposed = true;
		}
		#endregion
			#region IGhostable Members

		public void InitForGhostItems(FdoCache cache, XmlNode colSpec)
		{
			m_ghostParentHelper = BulkEditBar.GetGhostHelper(cache.ServiceLocator, colSpec);
		}

		#endregion
	}



	/// <summary>
	/// This class implements setting a sequence reference property to a set of values chosen
	/// from a list.
	/// </summary>
	class ComplexListChooserBEditControl : IBulkEditSpecControl
	{
		protected Mediator m_mediator;
		protected FdoCache m_cache;
		private XMLViewsDataCache m_sda;
		protected Button m_launcher;
		protected int m_hvoList;
		protected int m_flid;
		string m_fieldName; // user-viewable name of field to display
		string m_displayNameProperty; // name of method to get what to display for each item.
		string m_displayWs; // key recognized by ObjectLabelCollection
		List<ICmObject> m_chosenObjs = new List<ICmObject>(0);
		bool m_fReplace = false; // true to replace rather than appending.
		bool m_fRemove = false; // true to remove selected items rather than append or replace
		private GhostParentHelper m_ghostParentHelper;

		public event FwSelectionChangedEventHandler ValueChanged;

		public ComplexListChooserBEditControl(FdoCache cache, Mediator mediator,  XmlNode colSpec)
			: this(BulkEditBar.GetFlidFromClassDotName(cache, colSpec, "field"),
			BulkEditBar.GetNamedListHvo(cache, colSpec, "list"),
			XmlUtils.GetOptionalAttributeValue(colSpec, "displayNameProperty", "ShortNameTSS"),
			BulkEditBar.GetColumnLabel(mediator, colSpec),
			XmlUtils.GetOptionalAttributeValue(colSpec, "displayWs", "best analorvern"),
			BulkEditBar.GetGhostHelper(cache.ServiceLocator, colSpec))
		{
		}

		/// <summary>
		/// C# equivalent of the VB RIGHT function
		/// </summary>
		public string Right(string original, int numberCharacters)
		{
			return original.Substring(original.Length - numberCharacters);
		}

		public ComplexListChooserBEditControl(int flid, int hvoList, string displayNameProperty,
			string fieldName, string displayWs, GhostParentHelper gph)
		{
			m_hvoList = hvoList;
			m_flid = flid;
			m_launcher = new Button();
			m_launcher.Text = XMLViewsStrings.ksChoose_;
			m_launcher.Click += new EventHandler(m_launcher_Click);
			m_displayNameProperty = displayNameProperty;
			m_fieldName = fieldName;
			m_displayWs = displayWs;
			m_ghostParentHelper = gph;
		}

		void m_launcher_Click(object sender, EventArgs e)
		{
			if (m_hvoList == (int)SpecialHVOValues.kHvoUninitializedObject)
				return; // Can't show a chooser for a non-existent list!
			// Show a wait cursor (LT-4673)
			using (new SIL.Utils.WaitCursor(this.Control))
			{
				var list = m_cache.ServiceLocator.GetInstance<ICmPossibilityListRepository>().GetObject(m_hvoList);
				List<int> candidates = new List<int>(list.PossibilitiesOS.ToHvoArray());
				XCore.PersistenceProvider persistProvider =
					new PersistenceProvider(m_mediator.PropertyTable);
				var labels = ObjectLabel.CreateObjectLabels(m_cache, list.PossibilitiesOS.Cast<ICmObject>(),
					m_displayNameProperty, m_displayWs);
				//m_cache.MetaDataCacheAccessor.GetFieldName((int)m_flid, out fieldName);
				using (ReallySimpleListChooser chooser = new ReallySimpleListChooser(persistProvider,
					labels, m_fieldName, m_cache, m_chosenObjs, m_mediator.HelpTopicProvider))
				{
					chooser.Atomic = Atomic;
					chooser.Cache = m_cache;
					chooser.SetObjectAndFlid(0, m_flid);
					chooser.ShowFuncButtons();
					if (Convert.ToInt16(Right(m_flid.ToString(), 3)) >= 500 && Convert.ToInt16(Right(m_flid.ToString(),3)) < 600)
						chooser.SetHelpTopic("khtpBulkEditCustomField");
					else
						chooser.SetHelpTopic("khtpBulkEdit" + m_fieldName.Replace(" ", ""));
					System.Windows.Forms.DialogResult res = chooser.ShowDialog((sender as Control).TopLevelControl);
					if (System.Windows.Forms.DialogResult.Cancel == res)
						return;
					m_chosenObjs = chooser.ChosenObjects.ToList();
					m_fReplace = chooser.ReplaceMode;
					m_fRemove = chooser.RemoveMode;

					// Tell the parent control that we may have changed the selected item so it can
					// enable or disable the Apply and Preview buttons based on the selection.
					// We are just checking here if any item was selected by the user in the dialog
					if (ValueChanged != null)
					{
						int hvo = 0;
						if (m_chosenObjs.Count > 0)
							hvo = m_chosenObjs[0].Hvo;
						ValueChanged(sender, new FwObjectSelectionEventArgs(hvo));
					}
				}
			}
		}

		#region IBulkEditSpecControl Members

		public Mediator Mediator
		{
			get { return m_mediator; }
			set { m_mediator = value; }
		}

		public Control Control
		{
			get
			{
				return m_launcher;
			}
		}

		public FdoCache Cache
		{
			get
			{
				return m_cache;
			}
			set
			{
				m_cache = value;
				Atomic = (CellarPropertyType)m_cache.MetaDataCacheAccessor.GetFieldType(m_flid) == CellarPropertyType.ReferenceAtom;
			}
		}

		private bool Atomic { get; set; }
		/// <summary>
		/// The special cache that can handle the preview and check-box properties.
		/// </summary>
		public XMLViewsDataCache DataAccess
		{
			get
			{
				if (m_sda == null)
					throw new InvalidOperationException("Must set the special cache of a BulkEditSpecControl");
				return m_sda;
			}
			set { m_sda = value; }
		}
		/// <summary>
		/// (By default, it is an empty list (int[0]), unless user has used the chooser to select items.)
		/// </summary>
		internal IEnumerable<ICmObject> ChosenObjects
		{
			get { return m_chosenObjs; }
			set { m_chosenObjs = value.ToList(); }
		}

		/// <summary>
		///
		/// </summary>
		internal bool ReplaceMode
		{
			get { return m_fReplace; }
			set { m_fReplace = value; }
		}

		/// <summary>
		/// required interface member not currently used.
		/// </summary>
		public IVwStylesheet Stylesheet
		{
			set {  }
		}

		public virtual void DoIt(IEnumerable<int> itemsToChange, ProgressState state)
		{
			UndoableUnitOfWorkHelper.Do(XMLViewsStrings.ksUndoBulkEdit, XMLViewsStrings.ksRedoBulkEdit,
				m_cache.ServiceLocator.GetInstance<IActionHandler>(), () =>
			{
				//ISilDataAccess sda = m_cache.DomainDataByFlid; // used DataAccess, is that okay?

				var chosenObjs = m_chosenObjs;
				int i = 0;
				// Report progress 50 times or every 100 items, whichever is more (but no more than once per item!)
				int interval = Math.Min(100, Math.Max(itemsToChange.Count() / 50, 1));
				foreach (int hvoItem in itemsToChange)
				{
					i++;
					if (i % interval == 0)
					{
						state.PercentDone = i * 100 / itemsToChange.Count();
						state.Breath();
					}
					if (DisableItem(hvoItem))
						continue;
					List<ICmObject> oldVals, newVal;
					ComputeValue(chosenObjs, hvoItem, out oldVals, out newVal);
					if (oldVals.SequenceEqual(newVal))
						continue;

					var newHvos = (from obj in newVal
								   select obj.Hvo).ToArray();
					var realTarget = hvoItem;
					if (m_ghostParentHelper != null)
					{
						realTarget = m_ghostParentHelper.FindOrCreateOwnerOfTargetProp(hvoItem, m_flid);
					}
					if (Atomic)
					{
						var newHvo = newHvos.Length > 0 ? newHvos[0] : 0;
						DataAccess.SetObjProp(realTarget, m_flid, newHvo);
					}
					else
					{
						DataAccess.Replace(realTarget, m_flid, 0, oldVals.Count, newHvos, newHvos.Length);
					}
				}
			});
		}

		public virtual void FakeDoit(IEnumerable<int> itemsToChange, int tagFakeFlid, int tagEnabled,
			ProgressState state)
		{
			var chosenObjs = m_chosenObjs;
			int i = 0;
			// Report progress 50 times or every 100 items, whichever is more (but no more than once per item!)
			int interval = Math.Min(100, Math.Max(itemsToChange.Count() / 50, 1));
			ITsString tssChosenVal = BuildValueString(chosenObjs);
			foreach (int hvoItem in itemsToChange)
			{
				i++;
				if (i % interval == 0)
				{
					state.PercentDone = i * 100 / itemsToChange.Count();
					state.Breath();
				}
				ITsString tssVal = tssChosenVal;
				List<ICmObject> oldVals, newVal;
				bool fEnable = false;
				if (!DisableItem(hvoItem))
				{
					ComputeValue(chosenObjs, hvoItem, out oldVals, out newVal);
					fEnable = !oldVals.SequenceEqual(newVal);
					if (fEnable)
					{
						if (newVal != chosenObjs)
							tssVal = BuildValueString(newVal);
						m_sda.SetString(hvoItem, tagFakeFlid, tssVal);
					}
				}
				m_sda.SetInt(hvoItem, tagEnabled, (fEnable ? 1 : 0));
			}
		}

		/// <summary>
		/// subclasses may override to determine if this hvoItem should be excluded.
		/// Basically a kludge to avoid the hassle of trying to figure a way to generate
		/// separate lists for variants/complex entry types since the target the same ListItemsClass (LexEntryRef).
		/// Currently EntriesOrChildClassesRecordList can only determine which virtual property
		/// to load based upon the target ListItemsClass (not a flid). And since we typically don't
		/// want the user to change variant types for complex entry refs (or vice-versa),
		/// we need someway to filter out items in the list based upon the selected column.
		/// </summary>
		/// <param name="hvoItem"></param>
		/// <returns></returns>
		protected virtual bool DisableItem(int hvoItem)
		{
			// by default we don't want to automatically exclude selected items from
			// bulk editing.
			return false;
		}

		List<ICmObject> GetOldVals(int hvoReal)
		{
			if (hvoReal == 0)
				return new List<ICmObject>();
			if (Atomic)
			{
				var result = new List<ICmObject>();
				int val = m_cache.DomainDataByFlid.get_ObjectProp(hvoReal, m_flid);
				if (val != 0)
					result.Add(m_cache.ServiceLocator.GetObject(val));
				return result;
			}
			return (from hvo in (m_cache.DomainDataByFlid as ISilDataAccessManaged).VecProp(hvoReal, m_flid)
					   select m_cache.ServiceLocator.GetObject(hvo)).ToList();
		}

		private int GetRealHvo(int hvoItem)
		{
			if (m_ghostParentHelper != null)
				return m_ghostParentHelper.GetOwnerOfTargetProperty(hvoItem);
			return hvoItem;
		}

		private void ComputeValue(List<ICmObject> chosenObjs, int hvoItem, out List<ICmObject> oldVals, out List<ICmObject> newVal)
		{
			// Check whether we can actually compute values for this item.  If not,
			// just return a pair of empty lists.  (See LT-11016 and LT-11357.)
			var hvoReal = GetRealHvo(hvoItem);
			if (hvoReal != 0)
			{
				var clidItem = m_cache.ServiceLocator.ObjectRepository.GetClsid(hvoReal);
				var clidField = m_cache.MetaDataCacheAccessor.GetOwnClsId(m_flid);
				if (clidItem != clidField)
				{
					var baseClid = m_cache.MetaDataCacheAccessor.GetBaseClsId(clidItem);
					while (baseClid != clidField && baseClid != 0)
						baseClid = m_cache.MetaDataCacheAccessor.GetBaseClsId(baseClid);
					if (baseClid != clidField)
					{
						oldVals = new List<ICmObject>();
						newVal = oldVals;
						return;
					}
				}
			}
			oldVals = GetOldVals(hvoReal);

			newVal = chosenObjs;
			if (m_fRemove)
			{
				newVal = oldVals; // by default no change in remove mode.
				if (oldVals.Count > 0)
				{
					var newValues = new List<ICmObject>(oldVals);
					foreach (ICmObject obj in chosenObjs)
					{
						if (newValues.Contains(obj))
							newValues.Remove(obj);
					}
					newVal = newValues;
				}
			}
			else if (!m_fReplace && oldVals.Count != 0)
			{
				// Need to handle as append.
				if (Atomic)
					newVal = oldVals; // can't append to non-empty atomic value
				else
				{
					var newValues = new List<ICmObject>(oldVals);
					foreach (ICmObject obj in chosenObjs)
					{
						if (!newValues.Contains(obj))
							newValues.Add(obj);
					}
					newVal = newValues;
				}
			}
		}

		private ITsString BuildValueString(List<ICmObject> chosenObjs)
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			ITsString sep = null; // also acts as first-time flag.
			foreach (ICmObject obj in chosenObjs)
			{
				PropertyInfo pi = obj.GetType().GetProperty(m_displayNameProperty);
				ITsString tss = (ITsString)pi.GetValue(obj, null);
				if (sep == null)
				{
					// first time create it
					sep = m_cache.TsStrFactory.MakeString(", ",
						 m_cache.ServiceLocator.WritingSystemManager.UserWs);
				}
				else
				{
					// subsequent times insert it.
					bldr.ReplaceTsString(bldr.Length, bldr.Length, sep);
				}
				bldr.ReplaceTsString(bldr.Length, bldr.Length, tss);
			}
			ITsString tssVal = bldr.Length > 0 ? bldr.GetString() :
				TsStringUtils.MakeTss("", m_cache.ServiceLocator.WritingSystemManager.UserWs);
			return tssVal;
		}
		#endregion

		/// <summary>
		/// This type of editor can always select null.
		/// </summary>
		public bool CanClearField
		{
			get { return true; }
		}

		/// <summary>
		/// And does it by setting the list to empty and using overwrite mode.
		/// </summary>
		public void SetClearField()
		{
			m_chosenObjs = new List<ICmObject>(0);
			m_fRemove = false;
			m_fReplace = true;
		}

		public List<int> FieldPath
		{
			get { return new List<int>(new int[] { m_flid }); }
		}

	}


	class VariantEntryTypesChooserBEditControl : ComplexListChooserBEditControl
	{
		internal VariantEntryTypesChooserBEditControl(FdoCache cache, Mediator mediator, XmlNode colSpec)
			: base(cache, mediator, colSpec)
		{
		}
	}

	class ComplexEntryTypesChooserBEditControl : ComplexListChooserBEditControl
	{
		Set<int> m_complexEntryRefs = null;

		internal ComplexEntryTypesChooserBEditControl(FdoCache cache, Mediator mediator, XmlNode colSpec)
			: base(cache, mediator, colSpec)
		{
		}

		/// <summary>
		/// kludge: filter to allow only complex entry references.
		/// </summary>
		/// <param name="hvoItem"></param>
		/// <returns></returns>
		protected override bool DisableItem(int hvoItem)
		{
			if (m_complexEntryRefs == null)
			{
				m_complexEntryRefs = new Set<int>();
				Dictionary<int, List<int>> dict = new Dictionary<int,List<int>>();
				// go through each list and add the values to our set.
				foreach (List<int> refs in dict.Values)
					m_complexEntryRefs.AddRange(refs);
			}
			return !m_complexEntryRefs.Contains(hvoItem);
		}

		public override void FakeDoit(IEnumerable<int> itemsToChange, int tagFakeFlid, int tagEnabled, ProgressState state)
		{
			m_complexEntryRefs = null;	// reset the filtered entry refs cache.
			base.FakeDoit(itemsToChange, tagFakeFlid, tagEnabled, state);
		}

		public override void DoIt(IEnumerable<int> itemsToChange, ProgressState state)
		{
			m_complexEntryRefs = null; // reset the filtered entry refs cache.
			base.DoIt(itemsToChange, state);
		}
	}

	/// <summary>
	/// This class allows, for example, the morph type items to be sorted alphabetically in the
	/// listbox by providing the IComparable interface.
	/// </summary>
	class HvoLabelItem : IComparable
	{
		int m_hvo;
		ITsString m_tssLabel;
		string m_sortString;

		public HvoLabelItem(int hvoChild, ITsString tssLabel)
		{
			m_hvo = hvoChild;
			m_tssLabel = tssLabel;
			m_sortString = tssLabel.Text;
		}

		public int Hvo
		{
			get { return m_hvo; }
		}

		public ITsString TssLabel
		{
			get { return m_tssLabel; }
		}

		public override string ToString()
		{
			return m_sortString;
		}

		#region IComparable Members

		public int CompareTo(object obj)
		{
			if (m_sortString != null)
				return m_sortString.CompareTo(obj.ToString());
			else
				return String.Empty.CompareTo(obj.ToString());
		}

		#endregion
	}
	/// <summary>
	/// This class implements setting the MorphType of the MoForm belonging to the LexemeForm
	/// field of a LexEntry.
	/// </summary>
	class MorphTypeChooserBEditControl : FlatListChooserBEditControl
	{
		protected int m_flidParent;
		BrowseViewer m_containingViewer;

		//int flidAtomicProp, int hvoList, int ws, bool useAbbr
		public MorphTypeChooserBEditControl(int flid, int subflid, int hvoList, int ws, BrowseViewer viewer)
			: base(subflid, hvoList, ws, false)
		{
			m_flidParent = flid;
			m_containingViewer = viewer;
		}

		#region IBulkEditSpecControl Members (overrides)

		public override List<int> FieldPath
		{
			get
			{
				List<int> fieldPath = base.FieldPath;
				fieldPath.Insert(0, m_flidParent);
				return fieldPath;
			}
		}

		public override void DoIt(IEnumerable<int> itemsToChange, ProgressState state)
		{
			UndoableUnitOfWorkHelper.Do(XMLViewsStrings.ksUndoBulkEdit, XMLViewsStrings.ksRedoBulkEdit, m_cache.ActionHandlerAccessor,
				() =>
					{
						ISilDataAccess sda = m_cache.DomainDataByFlid;

						HvoTssComboItem item = m_combo.SelectedItem as HvoTssComboItem;
						if (item == null)
							return;
						int hvoSelMorphType = item.Hvo;
						bool fSelAffix = false;
						if (hvoSelMorphType != 0)
							fSelAffix = MorphServices.IsAffixType(m_cache, hvoSelMorphType);
						bool fAnyFundamentalChanges = false;
						// Preliminary check and warning if changing fundamental type.
						foreach (int hvoLexEntry in itemsToChange)
						{
							int hvoLexemeForm = sda.get_ObjectProp(hvoLexEntry, m_flidParent);
							if (hvoLexemeForm == 0)
								continue;
							int hvoMorphType = sda.get_ObjectProp(hvoLexemeForm, m_flidAtomicProp);
							if (hvoMorphType == 0)
								continue;
							bool fAffix = MorphServices.IsAffixType(m_cache, hvoMorphType);
							if (fAffix != fSelAffix && hvoSelMorphType != 0)
							{
								string msg = String.Format(XMLViewsStrings.ksMorphTypeChangesSlow,
									(fAffix ? XMLViewsStrings.ksAffixes : XMLViewsStrings.ksStems),
									(fAffix ? XMLViewsStrings.ksStems : XMLViewsStrings.ksAffixes));
								if (MessageBox.Show(this.m_combo, msg, XMLViewsStrings.ksChangingMorphType,
									MessageBoxButtons.OKCancel,
									MessageBoxIcon.Warning) != DialogResult.OK)
								{
									return;
								}
								fAnyFundamentalChanges = true;
								break; // user OKd it, no need to check further.
							}
						}
						if (fAnyFundamentalChanges)
						{
							m_containingViewer.SetListModificationInProgress(true);
						}
						try
						{
							// Report progress 50 times or every 100 items, whichever is more
							// (but no more than once per item!)
							Set<int> idsToDel = new Set<int>();
							var newForms = new Dictionary<IMoForm, ILexEntry>();
							int interval = Math.Min(80, Math.Max(itemsToChange.Count()/50, 1));
							int i = 0;
							foreach (int hvoLexEntry in itemsToChange)
							{
								// Guess we're 80% done when through all but deleting leftover objects and moving
								// new MoForms to LexemeForm slot.
								if ((i + 1)%interval == 0)
								{
									state.PercentDone = i*80/itemsToChange.Count();
									state.Breath();
								}
								i++;
								int hvoLexemeForm = sda.get_ObjectProp(hvoLexEntry, m_flidParent);
								if (hvoLexemeForm == 0)
									continue;
								int hvoMorphType = sda.get_ObjectProp(hvoLexemeForm, m_flidAtomicProp);
								if (hvoMorphType == 0)
									continue;
								bool fAffix = MorphServices.IsAffixType(m_cache, hvoMorphType);
								var stemAlloFactory = m_cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>();
								var afxAlloFactory = m_cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>();
								if (fAffix == fSelAffix)
								{
									// Not changing C# type of allomorph object, just set the morph type.
									if (hvoMorphType != hvoSelMorphType)
									{
										sda.SetObjProp(hvoLexemeForm, m_flidAtomicProp, hvoSelMorphType);
									}
								}
								else if (fAffix)
								{
									// Changing from affix to stem, need a new allomorph object.
									var entry = m_cache.ServiceLocator.GetInstance<ILexEntryRepository>().GetObject(hvoLexEntry);
									var affix = m_cache.ServiceLocator.GetInstance<IMoAffixAllomorphRepository>().GetObject(hvoLexemeForm);
									var stem = stemAlloFactory.Create();
									SwapFormValues(entry, affix, stem, hvoSelMorphType, idsToDel);
									foreach (var env in affix.PhoneEnvRC)
										stem.PhoneEnvRC.Add(env);
									newForms[stem] = entry;
								}
								else
								{
									// Changing from stem to affix, need a new allomorph object.
									var entry = m_cache.ServiceLocator.GetInstance<ILexEntryRepository>().GetObject(hvoLexEntry);
									var stem = m_cache.ServiceLocator.GetInstance<IMoStemAllomorphRepository>().GetObject(hvoLexemeForm);
									var affix = afxAlloFactory.Create();
									SwapFormValues(entry, stem, affix, hvoSelMorphType, idsToDel);
									foreach (var env in stem.PhoneEnvRC)
										affix.PhoneEnvRC.Add(env);
									newForms[affix] = entry;
								}
							}
							if (fAnyFundamentalChanges)
							{
								foreach (int hvo in idsToDel)
								{
									sda.DeleteObj(hvo);
								}
								state.PercentDone = 90;
								state.Breath();
								foreach (var pair in newForms)
								{
									pair.Value.LexemeFormOA = pair.Key;
								}
								state.PercentDone = 100;
								state.Breath();
							}
						}
						finally
						{
							if (fAnyFundamentalChanges)
								m_containingViewer.SetListModificationInProgress(false);
						}
					});
		}
		// Swap values of various attributes between an existing form that is a LexemeForm and
		// a newly created one. Includes adding the new one to the alternate forms of the entry, and
		// the id of the old one to a map of things to delete.
		private void SwapFormValues(ILexEntry entry, IMoForm origForm, IMoForm newForm, int typeHvo, Set<int> idsToDel)
		{
			entry.AlternateFormsOS.Add(newForm);
			origForm.SwapReferences(newForm);
			var muaOrigForm = origForm.Form;
			var muaNewForm = newForm.Form;
			muaNewForm.MergeAlternatives(muaOrigForm);
			newForm.MorphTypeRA = m_cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(typeHvo);
			idsToDel.Add(origForm.Hvo);
		}

		public override void FakeDoit(IEnumerable<int> itemsToChange, int tagFakeFlid, int tagEnabled,
			ProgressState state)
		{
			ISilDataAccess sda = m_cache.DomainDataByFlid;
			HvoTssComboItem item = m_combo.SelectedItem as HvoTssComboItem;
			if (item == null)
				return;
			int hvoSelMorphType = item.Hvo;

			// Report progress 50 times or every 100 items, whichever is more
			// (but no more than once per item!)
			int interval = Math.Min(100, Math.Max(itemsToChange.Count() / 50, 1));
			int i = 0;
			foreach (int hvoLexEntry in itemsToChange)
			{
				if ((i + 1) % interval == 0)
				{
					state.PercentDone = i * 100 / itemsToChange.Count();
					state.Breath();
				}
				int hvoLexemeForm = sda.get_ObjectProp(hvoLexEntry, m_flidParent);
				if (hvoLexemeForm == 0)
					continue;
				int hvoMorphType = sda.get_ObjectProp(hvoLexemeForm, m_flidAtomicProp);
				if (hvoMorphType == 0)
					continue;
				bool fAffix = MorphServices.IsAffixType(m_cache, hvoMorphType);
				// Per LT-5305, OK to switch types.
				//bool fEnable = fAffix == fSelAffix && hvoMorphType != hvoSelMorphType;
				bool fEnable = hvoMorphType != hvoSelMorphType;
				if (fEnable)
					m_sda.SetString(hvoLexEntry, tagFakeFlid, item.AsTss);
				m_sda.SetInt(hvoLexEntry, tagEnabled, (fEnable ? 1 : 0));
				i++;
			}
		}

		#endregion
	}

	/// <summary>
	/// These classes are command objects representing the task of getting a string value
	/// from a browse column in a way specified by its node and writing a new string value
	/// back to the object. This is orthogonal to the several different transformations
	/// that can be performed on the strings so obtained. This abstract class also has the
	/// responsibility for deciding whether a field read/writer can be created for the column,
	/// and if so, for creating the appropriate subclass.
	/// </summary>
	internal abstract class FieldReadWriter : IGhostable
	{
		public abstract ITsString CurrentValue(int hvo);
		public abstract void SetNewValue(int hvo, ITsString tss);
		public abstract int WritingSystem { get; }

		protected ISilDataAccess m_sda;
		protected GhostParentHelper m_ghostParentHelper;

		public FieldReadWriter(ISilDataAccess sda)
		{
			m_sda = sda;
		}

		public ISilDataAccess DataAccess
		{
			get { return m_sda; }
			set { m_sda = value; }
		}

		// If ws is zero, determine a ws for the specified string field.
		static internal int GetWsFromMetaData(int wsIn, int flid, FdoCache cache)
		{
			if (wsIn != 0)
				return wsIn;
			IFwMetaDataCache mdc = cache.DomainDataByFlid.MetaDataCache;
			// ws not specified in the file; better be in metadata
			int ws = mdc.GetFieldWs(flid);
			if (ws == WritingSystemServices.kwsAnal || ws == WritingSystemServices.kwsAnals
				|| ws == WritingSystemServices.kwsAnalVerns)
			{
				return cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle;
			}
			else if (ws == WritingSystemServices.kwsVern || ws == WritingSystemServices.kwsVerns
				|| ws == WritingSystemServices.kwsVernAnals)
			{
				return cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle;
			}
			else return 0;
		}

		static public FieldReadWriter Create(XmlNode node, FdoCache cache)
		{
			return Create(node, cache, 0);
		}

		static public FieldReadWriter Create(XmlNode node, FdoCache cache, int hvoRootObj)
		{
			string transduceField = XmlUtils.GetOptionalAttributeValue(node, "transduce");
			if (string.IsNullOrEmpty(transduceField))
				return null;
			string[] parts = transduceField.Split('.');
			if (parts.Length != 2 && parts.Length != 3)
				return null;
			string className = parts[0];
			string fieldName = parts[1];

			IFwMetaDataCache mdc = cache.DomainDataByFlid.MetaDataCache;
			int clid = mdc.GetClassId(className);
			if (clid == 0)
				return null;
			int flid = mdc.GetFieldId2(clid, fieldName, true);
			int ws = WritingSystemServices.GetWritingSystem(cache, node, null, hvoRootObj, flid, 0).Handle;
			if (parts.Length == 2)
			{
				FieldReadWriter frw;
				// parts are divided into class.propname
				if (DoItMethod.IsMultilingual(flid, mdc))
					frw = new OwnMlPropReadWriter(cache, flid, ws);
				else
					frw = new OwnStringPropReadWriter(cache, flid, GetWsFromMetaData(ws, flid, cache));
				frw.InitForGhostItems(cache, node);
				return frw;
			}

			// parts.Length is 3. We have class.objectpropname.propname
			int clidDst = mdc.GetDstClsId(flid);
			int fieldType = mdc.GetFieldType(flid);
			int flid2 = mdc.GetFieldId2(clidDst, parts[2], true);
			int clidCreate = clidDst;	// default
			string createClassName = XmlUtils.GetOptionalAttributeValue(node, "transduceCreateClass");
			if (createClassName != null)
				clidCreate = mdc.GetClassId(createClassName);
			if (DoItMethod.IsMultilingual(flid2, mdc))
			{
				Debug.Assert(ws != 0);
				// If it's a multilingual field and we didn't get a writing system, we can't transduce this field.
				if (ws == 0)
					return null;
				if (fieldType == (int)CellarPropertyType.OwningAtomic)
					return new OwnAtomicMlPropReadWriter(cache, flid2, ws, flid, clidCreate);
				else if (fieldType == (int)CellarPropertyType.OwningSequence)
					return new OwnSeqMlPropReadWriter(cache, flid2, ws, flid, clidCreate);
				else
					return null; // can't handle yet
			}
			else
			{
				if (fieldType == (int)CellarPropertyType.OwningAtomic)
					return new OwnAtomicStringPropReadWriter(cache, flid2, ws, flid, clidCreate);
				else if (fieldType == (int)CellarPropertyType.OwningSequence)
					return new OwnSeqStringPropReadWriter(cache, flid2, ws, flid, clidCreate);
				else
					return null; // can't handle yet
			}

		}
		internal virtual List<int> FieldPath
		{
			get { return null; }
		}

		#region IGhostable Members

		public void InitForGhostItems(FdoCache cache, XmlNode colSpec)
		{
			m_ghostParentHelper = BulkEditBar.GetGhostHelper(cache.ServiceLocator, colSpec);
		}

		#endregion
	}

	/// <summary>
	/// Allows a generic way to access a string in a browse view column cell.
	/// (Currently used for Source combo items)
	/// </summary>
	internal class ManyOnePathSortItemReadWriter : FieldReadWriter, IDisposable
	{
		private FdoCache m_cache;
		private XmlNode m_colSpec;
		private BrowseViewer m_bv;
		private IStringFinder m_finder;
		private IApp m_app;

		public ManyOnePathSortItemReadWriter(FdoCache cache, XmlNode colSpec, BrowseViewer bv, IApp app)
			: base(cache.DomainDataByFlid)
		{
			m_cache = cache;
			m_colSpec = colSpec;
			m_bv = bv;
			m_app = app;
			EnsureFinder();
		}

		#region Disposable stuff
#if DEBUG
		/// <summary/>
		~ManyOnePathSortItemReadWriter()
		{
			Dispose(false);
		}
#endif

		/// <summary/>
		public bool IsDisposed { get; private set; }

		/// <summary/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		protected virtual void Dispose(bool fDisposing)
		{
			Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType() + " *******");
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects
				var disposable = m_finder as IDisposable;
				if (disposable != null)
					disposable.Dispose();
			}
			m_finder = null;
			m_cache = null;
			m_colSpec = null;
			m_bv = null;
			m_app = null;
			IsDisposed = true;
		}
		#endregion

		private IManyOnePathSortItem GetManyOnePathSortItem(int hvo)
		{
			return new ManyOnePathSortItem(hvo, null, null);	// just return an item based on the the RootObject
		}

		private void EnsureFinder()
		{
			if (m_finder == null)
				m_finder = LayoutFinder.CreateFinder(m_cache, m_colSpec, m_bv.BrowseView.Vc, m_app);
		}

		/// <summary>
		/// Get's the string associated with the given hvo for a particular column.
		/// </summary>
		/// <param name="hvo"></param>
		/// <returns></returns>
		public override ITsString CurrentValue(int hvo)
		{
			return m_finder.Key(this.GetManyOnePathSortItem(hvo));
		}

		/// <summary>
		/// NOTE: ManyOnePathSortItemReadWriter is currently read-only.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tss"></param>
		public override void SetNewValue(int hvo, ITsString tss)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		/// <summary>
		///
		/// </summary>
		public override int WritingSystem
		{
			get { throw new Exception("The method or operation is not implemented."); }
		}
	}

	/// <summary>
	/// FieldReadWriter for strings stored in (non-multilingual) props of the object itself.
	/// </summary>
	internal class OwnStringPropReadWriter : FieldReadWriter
	{
		protected int m_flid;
		protected int m_ws;
		protected FdoCache m_cache;

		public OwnStringPropReadWriter(FdoCache cache, int flid, int ws)
			: base(cache.MainCacheAccessor)
		{
			m_cache = cache;
			m_flid = flid;
			m_ws = ws;
		}

		internal override List<int> FieldPath
		{
			get { return new List<int>(new int[] {m_flid} ) ; }
		}

		public override ITsString CurrentValue(int hvo)
		{
			int hvoStringOwner = hvo;
			if (m_ghostParentHelper != null)
				hvoStringOwner = m_ghostParentHelper.GetOwnerOfTargetProperty(hvo);
			if (hvoStringOwner == 0)
				return null; // hasn't been created yet.
			return m_sda.get_StringProp(hvoStringOwner, m_flid);
		}

		public override void SetNewValue(int hvo, ITsString tss)
		{
			int hvoStringOwner = hvo;
			if (m_ghostParentHelper != null)
				hvoStringOwner = m_ghostParentHelper.FindOrCreateOwnerOfTargetProp(hvo, m_flid);
			SetStringValue(hvoStringOwner, tss);
		}

		protected virtual void SetStringValue(int hvoStringOwner, ITsString tss)
		{
			m_sda.SetString(hvoStringOwner, m_flid, tss);
		}

		public override int WritingSystem
		{
			get
			{
				return m_ws;
			}
		}

	}

	/// <summary>
	/// FieldReadWriter for strings stored in a non-multilingual string prop of an object
	/// owned in an atomic property of the base object.
	/// </summary>
	internal class OwnAtomicStringPropReadWriter : OwnStringPropReadWriter
	{
		int m_flidObj;
		int m_clid; // to create if missing

		public OwnAtomicStringPropReadWriter(FdoCache cache, int flidString, int ws, int flidObj, int clid)
			: base(cache, flidString, ws)
		{
			m_flidObj = flidObj;
			m_clid = clid;
		}

		internal override List<int> FieldPath
		{
			get
			{
				List<int> fieldPath = base.FieldPath;
				fieldPath.Insert(0, m_flidObj);
				return fieldPath;
			}
		}

		public override ITsString CurrentValue(int hvo)
		{
			return base.CurrentValue(m_sda.get_ObjectProp(hvo, m_flidObj));
		}

		public override void SetNewValue(int hvo, ITsString tss)
		{
			int ownedAtomicObj = m_sda.get_ObjectProp(hvo, m_flidObj);
			bool fHadObject = ownedAtomicObj != 0;
			if (!fHadObject)
			{
				if (m_clid == 0)
					return;
				ownedAtomicObj = m_sda.MakeNewObject(m_clid, hvo, m_flidObj, -2);
			}
			base.SetNewValue(ownedAtomicObj, tss);
		}
	}

	/// <summary>
	/// FieldReadWriter for strings stored in a non-multilingual string prop of the FIRST object
	/// owned in an sequence property of the base object.
	/// </summary>
	internal class OwnSeqStringPropReadWriter : OwnStringPropReadWriter
	{
		int m_flidObj;
		int m_clid; // to create if missing

		public OwnSeqStringPropReadWriter(FdoCache cache, int flidString, int ws, int flidObj, int clid)
			: base(cache, flidString, ws)
		{
			m_flidObj = flidObj;
			m_clid = clid;
		}

		internal override List<int> FieldPath
		{
			get
			{
				List<int> fieldPath = base.FieldPath;
				fieldPath.Insert(0, m_flidObj);
				return fieldPath;
			}
		}

		public override ITsString CurrentValue(int hvo)
		{
			if (m_sda.get_VecSize(hvo, m_flidObj) > 0)
				return base.CurrentValue(m_sda.get_VecItem(hvo, m_flidObj, 0));
			else
				return null;
		}

		public override void SetNewValue(int hvo, ITsString tss)
		{
			int firstSeqObj = 0;
			bool fHadOwningItem = m_sda.get_VecSize(hvo, m_flidObj) > 0;
			if (fHadOwningItem)
			{
				firstSeqObj = m_sda.get_VecItem(hvo, m_flidObj, 0);
			}
			else
			{
				// make first vector item if we know the class to base it on.
				if (m_clid == 0)
					return;
				firstSeqObj = m_sda.MakeNewObject(m_clid, hvo, m_flidObj, 0);
			}
			base.SetNewValue(firstSeqObj, tss);
		}
	}

	/// <summary>
	/// FieldReadWriter for strings stored in multilingual props of an object.
	/// </summary>
	internal class OwnMlPropReadWriter : OwnStringPropReadWriter
	{
		private bool m_fFieldAllowsMultipleRuns;
		public OwnMlPropReadWriter(FdoCache cache, int flid, int ws)
			: base(cache, flid, ws)
		{

			try
			{
				var fieldType = m_sda.MetaDataCache.GetFieldType(flid);
				m_fFieldAllowsMultipleRuns = fieldType == (int)CellarPropertyType.MultiString ||
											 fieldType == (int)CellarPropertyType.MultiBigString;
			}
			catch (KeyNotFoundException)
			{
				m_fFieldAllowsMultipleRuns = true; // Possibly a decorator field??
			}
		}

		public override ITsString CurrentValue(int hvo)
		{
			int hvoStringOwner = hvo;
			if (m_ghostParentHelper != null)
				hvoStringOwner = m_ghostParentHelper.GetOwnerOfTargetProperty(hvo);
			if (hvoStringOwner == 0)
				return null; // hasn't been created yet.
			return m_sda.get_MultiStringAlt(hvoStringOwner, m_flid, m_ws);
		}

		// In this subclass we're setting a multistring.
		protected override void SetStringValue(int hvoStringOwner, ITsString tss)
		{
			if (!m_fFieldAllowsMultipleRuns && tss.RunCount > 1)
			{
				// Illegally trying to store a multi-run TSS in a single-run field. This will fail.
				// Typically it's just that we tried to insert an English comma or similar.
				// Patch it up by making the whole string take on the properties of the first run.
				var bldr = tss.GetBldr();
				bldr.SetProperties(0, bldr.Length, tss.get_Properties(0));
				tss = bldr.GetString();
			}
			m_sda.SetMultiStringAlt(hvoStringOwner, m_flid, m_ws, tss);
		}
	}

	/// <summary>
	/// FieldReadWriter for strings stored in a multilingual prop of an object
	/// owned in an atomic property of the base object.
	/// </summary>
	internal class OwnAtomicMlPropReadWriter : OwnMlPropReadWriter
	{
		int m_flidObj;
		int m_clid; // to create if missing

		public OwnAtomicMlPropReadWriter(FdoCache cache, int flidString, int ws, int flidObj, int clid)
			: base(cache, flidString, ws)
		{
			m_flidObj = flidObj;
			m_clid = clid;
		}
		public override ITsString CurrentValue(int hvo)
		{
			return base.CurrentValue(m_sda.get_ObjectProp(hvo, m_flidObj));
		}

		internal override List<int> FieldPath
		{
			get
			{
				List<int> fieldPath = base.FieldPath;
				fieldPath.Insert(0, m_flidObj);
				return fieldPath;
			}
		}

		public override void SetNewValue(int hvo, ITsString tss)
		{
			int ownedAtomicObj = m_sda.get_ObjectProp(hvo, m_flidObj);
			bool fHadObject = ownedAtomicObj != 0;
			if (!fHadObject)
			{
				if (m_clid == 0)
					return;
				ownedAtomicObj = m_sda.MakeNewObject(m_clid, hvo, m_flidObj, -2);
			}
			base.SetNewValue(ownedAtomicObj, tss);
		}
	}

	/// <summary>
	/// FieldReadWriter for strings stored in a multilingual prop of the FIRST object
	/// owned in an sequence property of the base object.
	/// </summary>
	internal class OwnSeqMlPropReadWriter : OwnMlPropReadWriter
	{
		int m_flidObj;
		int m_clid; // to create if missing

		public OwnSeqMlPropReadWriter(FdoCache cache, int flidString, int ws, int flidObj, int clid)
			: base(cache, flidString, ws)
		{
			m_flidObj = flidObj;
			m_clid = clid;
		}

		internal override List<int> FieldPath
		{
			get
			{
				List<int> fieldPath = base.FieldPath;
				fieldPath.Insert(0, m_flidObj);
				return fieldPath;
			}
		}

		public override ITsString CurrentValue(int hvo)
		{
			if (m_sda.get_VecSize(hvo, m_flidObj) > 0)
				return base.CurrentValue(m_sda.get_VecItem(hvo, m_flidObj, 0));
			else
				return null;
		}

		public override void SetNewValue(int hvo, ITsString tss)
		{
			int firstSeqObj = 0;
			bool fHadOwningItem = m_sda.get_VecSize(hvo, m_flidObj) > 0;
			if (fHadOwningItem)
			{
				firstSeqObj = m_sda.get_VecItem(hvo, m_flidObj, 0);
			}
			else
			{
				// make first vector item if we know the class to base it on.
				if (m_clid == 0)
					return;
				firstSeqObj = m_sda.MakeNewObject(m_clid, hvo, m_flidObj, 0);
			}
			base.SetNewValue(firstSeqObj, tss);
		}
	}

}
