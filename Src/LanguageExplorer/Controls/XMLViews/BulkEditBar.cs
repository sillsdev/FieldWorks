// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Xml.Linq;
using ECInterfaces;
using LanguageExplorer.Areas;
using LanguageExplorer.Filters;
using LanguageExplorer.Impls;
using LanguageExplorer.LcmUi;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.FwCoreDlgs.Controls;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Application.ApplicationServices;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.SpellChecking;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.Xml;
using SilEncConverters40;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary />
	internal class BulkEditBar : UserControl, IPropertyTableProvider, IPublisherProvider, ISubscriberProvider
	{
		/// <summary />
		protected VSTabControl m_operationsTabControl;
		internal VSTabControl OperationsTabControl => m_operationsTabControl;
		/// <summary>
		/// the target combo box curently in use.
		/// </summary>
		protected internal FwOverrideComboBox CurrentTargetCombo { get; set; }
		/// <summary> indicates that we've either finished setting up or restoring bulk edit bar tab during initialization. </summary>
		protected internal bool m_setupOrRestoredBulkEditBarTab;
		/// <summary />
		protected FwOverrideComboBox m_listChoiceTargetCombo;
		internal FwOverrideComboBox ListChoiceTargetCombo => m_listChoiceTargetCombo;
		internal FwOverrideComboBox ListChoiceChangeToCombo { get; private set; }
		/// <summary />
		protected Control m_listChoiceControl;
		internal Control ListChoiceControl => m_listChoiceControl;
		private IContainer components;
		internal XElement ConfigurationNode { get; }
		/// <summary>
		/// Browse viewer
		/// </summary>
		protected BrowseViewer m_bv;
		/// <summary />
		protected BulkEditItem[] m_beItems;
		LcmCache m_cache;
		// object selected in browse view and possibly being edited; we track this
		// so we can commit changes to it when the current index changes.
		int m_hvoSelected;
		private ImageList m_imageList16x14;
		/// <summary />
		protected int m_itemIndex = -1;
		/// <summary>
		/// lets clients know when the target combo has changed (eg. so they
		/// can know if the ExpectedListItemsClass should change.)
		/// </summary>
		public event TargetColumnChangedHandler TargetComboSelectedIndexChanged;

		internal IVwPattern Pattern { get; set; }
		internal ITsString TssReplace { get; private set; }
		private Label m_bulkEditIcon;   // placeholder for button below.
		private Button m_bulkEditIconButton;
		private Button m_closeButton;
		private Button m_previewButton;
		private Button m_suggestButton; // for List Choice when Semantic Domains is active
		private Button m_ApplyButton;
		private Button m_helpButton;
		private Label label4;
		private Label label5;
		private Label label6;
		private Label label7;
		private Label label8;
		private Label label9;
		private Label label12;
		/// <summary />
		protected TabPage m_deleteTab;
		private Label label13;
		internal FwOverrideComboBox BulkCopySourceCombo { get; private set; }
		internal FwOverrideComboBox BulkCopyTargetCombo { get; private set; }
		internal FwOverrideComboBox ClickCopyTargetCombo { get; private set; }
		private Button m_transduceSetupButton;
		internal FwOverrideComboBox TransduceSourceCombo { get; private set; }
		internal FwOverrideComboBox TransduceTargetCombo { get; private set; }
		internal FwOverrideComboBox TransduceProcessorCombo { get; private set; }
		private FwLabel m_findReplaceSummaryLabel;
		private Button m_findReplaceSetupButton;
		internal FwOverrideComboBox FindReplaceTargetCombo { get; private set; }
		internal FwOverrideComboBox DeleteWhatCombo { get; private set; }
		private GroupBox groupBox1;
		internal RadioButton ClickCopyWordButton { get; private set; }
		internal RadioButton ClickCopyReorderButton { get; private set; }
		private GroupBox groupBox2;
		internal RadioButton ClickCopyAppendButton { get; private set; }
		internal RadioButton ClickCopyOverwriteButton { get; private set; }
		internal FwTextBox ClickCopySepBox { get; private set; }
		internal NonEmptyTargetControl BcNonEmptyTargetControl { get; }
		internal NonEmptyTargetControl TrdNonEmptyTargetControl { get; }
		private bool m_previewOn;
		private readonly string m_originalApplyText = XMLViewsStrings.ksApply;
		private static readonly string[] s_labels = {
									   XMLViewsStrings.ksListChoiceDesc,
									   XMLViewsStrings.ksBulkCopyDesc,
									   XMLViewsStrings.ksClickCopyDesc,
									   XMLViewsStrings.ksProcessDesc,
									   XMLViewsStrings.ksBulkReplaceDesc,
									   XMLViewsStrings.ksDeleteDesc
								   };
		// This set is used in figuring which items to enable when deleting (specifically senses, at present).
		// Contains the Ids of things in ItemsToChange(false).
		private ISet<int> m_items;
		// These variables are used in computing whether a ClickCopy target should actually
		// be changed.  (This feature is used by Wordform Bulk Edit.)
		private string m_sClickEditIf;
		private bool m_fClickEditIfNot;
		private MethodInfo m_miClickEditIf;
		private int m_wsClickEditIf;
		private XElement m_enableBulkEditTabsNode;
		// These variables are used in computing whether a row can be deleted in Bulk Delete.
		// (This feature is used in Wordform Bulk Edit.)
		private string m_sBulkDeleteIfZero;
		/// <summary>
		/// the classes of the objects that have rows which are bulkeditable
		/// </summary>
		private List<int> m_bulkEditListItemsClasses = new List<int>();
		/// <summary>
		/// the fields that own ghost objects in our browse view.
		/// </summary>
		private readonly List<GhostParentHelper> m_bulkEditListItemsGhostFields = new List<GhostParentHelper>();
		private ImageList m_imageList16x16;
		private PropertyInfo m_piBulkDeleteIfZero;
		private GhostParentHelper m_ghostParentHelper;
		private int m_expectedListItemsClassId;

		/// <summary>
		/// Create one
		/// </summary>
		/// <param name="bv">The BrowseViewer that it is part of.</param>
		/// <param name="spec">The parameters element of the BV, containing the
		/// 'columns' elements that specify the BE bar (among other things).</param>
		/// <param name="flexComponentParameters"></param>
		/// <param name="cache">The cache.</param>
		public BulkEditBar(BrowseViewer bv, XElement spec, FlexComponentParameters flexComponentParameters, LcmCache cache)
			: this()
		{
			PropertyTable = flexComponentParameters.PropertyTable;
			Publisher = flexComponentParameters.Publisher;
			Subscriber = flexComponentParameters.Subscriber;
			m_bv = bv;
			m_bv.FilterChanged += BrowseViewFilterChanged;
			m_bv.RefreshCompleted += BrowseViewSorterChanged;
			m_cache = cache;
			ConfigurationNode = spec;
			// (EricP) we should probably try find someway to get these classes from the RecordList/List
			var bulkEditListItemsClassesValue = XmlUtils.GetMandatoryAttributeValue(spec, "bulkEditListItemsClasses");
			var bulkEditListItemsClasses = bulkEditListItemsClassesValue.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (var className in bulkEditListItemsClasses)
			{
				var classId = cache.MetaDataCacheAccessor.GetClassId(className);
				m_bulkEditListItemsClasses.Add((int)classId);
			}
			// get any fields that have ghosts we may want to edit (see also "ghostListField" in columnSpecs)
			var bulkEditListItemsGhostFieldsValue = XmlUtils.GetOptionalAttributeValue(spec, "bulkEditListItemsGhostFields");
			if (!string.IsNullOrEmpty(bulkEditListItemsGhostFieldsValue))
			{
				var bulkEditListItemsGhostFields = bulkEditListItemsGhostFieldsValue.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (var classAndField in bulkEditListItemsGhostFields)
				{
					m_bulkEditListItemsGhostFields.Add(GhostParentHelper.Create(m_cache.ServiceLocator, classAndField));
				}
			}
			MakeItems();
			m_sBulkDeleteIfZero = XmlUtils.GetOptionalAttributeValue(spec, "bulkDeleteIfZero");
			AccessibilityObject.Name = @"BulkEditBar";
			m_listChoiceTargetCombo.SelectedIndexChanged += m_listChoiceTargetCombo_SelectedIndexChanged;
			// Finish init of the FwTextBox
			ClickCopySepBox.WritingSystemFactory = m_cache.WritingSystemFactory;
			ClickCopySepBox.WritingSystemCode = m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle;
			ClickCopySepBox.Text = " "; // default (maybe should persist?)
			ClickCopySepBox.GotFocus += m_clickCopySepBox_GotFocus;
			// Add NonBlankTargetControl as needed.
			BcNonEmptyTargetControl = new NonEmptyTargetControl
			{
				WritingSystemFactory = m_cache.WritingSystemFactory,
				Separator = " ",
				Location = new Point(170, 50),
				Name = "m_bcNonEmptyTargetControl"
			};
			// persist?
			// Set WritingSystemCode when destination field is set
			// Its size should be correctly preset.
			// todo: give it a tab stop.
			BulkCopyTab.Controls.Add(BcNonEmptyTargetControl);
			BulkCopyTargetCombo.SelectedIndexChanged += m_bulkCopyTargetCombo_SelectedIndexChanged;
			// And for the transduce tab...  (Process Tab)
			TrdNonEmptyTargetControl = new NonEmptyTargetControl
			{
				WritingSystemFactory = m_cache.WritingSystemFactory,
				Separator = " ",
				Location = new Point(170, 50),
				Name = "m_trdNonEmptyTargetControl"
			};
			// persist?
			// Set WritingSystemCode when destination field is set
			// Its size should be correctly preset.
			// todo: give it a tab stop.
			TransduceTab.Controls.Add(TrdNonEmptyTargetControl);
			TransduceTargetCombo.SelectedIndexChanged += m_transduceTargetCombo_SelectedIndexChanged;
			m_enableBulkEditTabsNode = XmlUtils.FindElement(spec, "enableBulkEditTabs");
			if (m_enableBulkEditTabsNode != null)
			{
				BulkCopyTab.Enabled = XmlUtils.GetOptionalBooleanAttributeValue(m_enableBulkEditTabsNode, "enableBEBulkCopy", true);
				ClickCopyTab.Enabled = XmlUtils.GetOptionalBooleanAttributeValue(m_enableBulkEditTabsNode, "enableBEClickCopy", true);
				TransduceTab.Enabled = XmlUtils.GetOptionalBooleanAttributeValue(m_enableBulkEditTabsNode, "enableBEProcess", true);
				FindReplaceTab.Enabled = XmlUtils.GetOptionalBooleanAttributeValue(m_enableBulkEditTabsNode, "enableBEFindReplace", true);
				m_deleteTab.Enabled = XmlUtils.GetOptionalBooleanAttributeValue(m_enableBulkEditTabsNode, "enableBEOther", true);
			}
			else
			{
				BulkCopyTab.Enabled = ClickCopyTab.Enabled = TransduceTab.Enabled = FindReplaceTab.Enabled = m_deleteTab.Enabled = true;
			}
			m_operationsTabControl.SelectedIndexChanged += m_operationsTabControl_SelectedIndexChanged;
			m_operationsTabControl.Deselecting += m_operationsTabControl_Deselecting;
			BulkEditTabPageSettings.TrySwitchToLastSavedTab(this);
			// events like SelectedIndexChanged do not fire until after initialization,
			// so do it explicitly here now.
			m_operationsTabControl_SelectedIndexChanged(null, new EventArgs());
			m_setupOrRestoredBulkEditBarTab = true;
			m_previewButton.Click += m_previewButton_Click;
			m_ApplyButton.Click += m_ApplyButton_Click;
			m_closeButton.Click += m_closeButton_Click;
			m_findReplaceSummaryLabel.WritingSystemFactory = m_cache.WritingSystemFactory;
		}

		internal void SetupNonserializableObjects(IVwPattern pattern)
		{
			Pattern = pattern;
			TssReplace = Pattern.ReplaceWith;
			UpdateFindReplaceSummary();
			EnablePreviewApplyForFindReplace();
		}

		private void BrowseViewSorterChanged(object sender, EventArgs e)
		{
			ResumeRecordListRowChanges();
		}

		private void BrowseViewFilterChanged(object sender, FilterChangeEventArgs e)
		{
			ResumeRecordListRowChanges();
		}

		private void m_operationsTabControl_Deselecting(object sender, TabControlCancelEventArgs e)
		{
			// try to save the settings from the currently selected tab before switching contexts.
			SaveSettings();
			if (m_operationsTabControl.SelectedTab != ClickCopyTab)
			{
				return;
			}
			// switching from click copy, so commit any pending changes.
			CommitClickChanges(this, EventArgs.Empty);
			SetEditColumn(-1);
			// ClickCopy will setup this up again.
			((XmlBrowseView)m_bv.BrowseView).ClickCopy -= xbv_ClickCopy;
		}

		/// <summary />
		public BulkEditBar()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
			m_ApplyButton.Text = m_originalApplyText;
			// replace the bulkEditIconButton with just its icon, until the button actually does something.
			m_bulkEditIcon = new Label
			{
				ForeColor = m_bulkEditIconButton.ForeColor,
				Name = "bulkEditIconLabel",
				ImageList = m_bulkEditIconButton.ImageList,
				ImageIndex = m_bulkEditIconButton.ImageIndex,
				Size = m_bulkEditIconButton.Size,
				Location = m_bulkEditIconButton.Location
			};
			m_bulkEditIconButton.Visible = false;
			m_operationsTabControl.SelectedTab.Controls.Remove(m_bulkEditIconButton);
			m_operationsTabControl.SelectedTab.Controls.Add(m_bulkEditIcon);
			m_bulkEditIconButton = null;
			m_findReplaceSummaryLabel = new FwLabel();
			FindReplaceTab.Controls.Add(m_findReplaceSummaryLabel);
			m_findReplaceSummaryLabel.Location = new Point(275, 72);
			m_findReplaceSummaryLabel.Size = new Size(215, 56);
			m_findReplaceSummaryLabel.TabIndex = 17;
			m_findReplaceSummaryLabel.Name = "m_findReplaceSummaryLabel";
			m_findReplaceSummaryLabel.TextAlign = ContentAlignment.TopLeft;
			m_findReplaceSummaryLabel.BackColor = SystemColors.Control;
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
				m_listChoiceTargetCombo.SelectedIndexChanged -= m_listChoiceTargetCombo_SelectedIndexChanged;
				m_previewButton.Click -= m_previewButton_Click;
				m_ApplyButton.Click -= m_ApplyButton_Click;
				m_closeButton.Click -= m_closeButton_Click;
				m_operationsTabControl.Deselecting -= m_operationsTabControl_Deselecting;
				m_operationsTabControl.SelectedIndexChanged -= m_operationsTabControl_SelectedIndexChanged;
				DisposeBulkEditItems();
			}
			m_beItems = null;
			m_bv = null;
			m_cache = null;

			base.Dispose(disposing);
		}

		internal void SetupApplyPreviewButtons(bool showApplyButton, bool showPreviewButton)
		{
			m_ApplyButton.Visible = showApplyButton;
			m_previewButton.Visible = showPreviewButton;
		}

		internal void SetupApplyPreviewButtons(bool showApplyButton, bool showPreviewButton, Cursor defaultCursorursor)
		{
			SetupApplyPreviewButtons(showApplyButton, showPreviewButton);
			m_bv.BrowseView.EditingHelper.DefaultCursor = defaultCursorursor;
		}

		internal static GhostParentHelper GetGhostHelper(ILcmServiceLocator locator, XElement colSpec)
		{
			var classDotField = XmlUtils.GetOptionalAttributeValue(colSpec, "ghostListField");
			return classDotField == null ? null : GhostParentHelper.Create(locator, classDotField);
		}

		private void DisposeBulkEditItems()
		{
			if (m_beItems == null)
			{
				return;
			}
			foreach (var bei in m_beItems)
			{
				if (bei == null)
				{
					continue;
				}
				bei.BulkEditControl.ValueChanged -= besc_ValueChanged;
				bei.Dispose();
			}
			m_beItems = null;
		}

		/// <summary>
		/// Update when the user reconfigures the set of columns.
		/// </summary>
		public void UpdateColumnList()
		{
			// ClickCopy.ClickCopyTabPageSettings/SaveSettings()/CommitClickChanges()
			// could possibly change m_hvoSelected when we're not ready, so save current.
			// see comment on LT-4768 below.
			var oldSelected = m_hvoSelected;
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

		/// <summary />
		private void populateListChoiceTargetCombo()
		{
			m_listChoiceTargetCombo.ClearItems();
			m_listChoiceTargetCombo.Text = string.Empty;
			// Here we figure which columns we can bulk edit.
			var icol = -1; // will increment at start of loop.
			foreach (var colSpec in m_bv.ColumnSpecs)
			{
				icol++;
				if (m_beItems[icol] != null)
				{
					var label = StringTable.Table.LocalizeAttributeValue(XmlUtils.GetOptionalAttributeValue(colSpec, "label", null)) ?? XmlUtils.GetMandatoryAttributeValue(colSpec, "label");
					m_listChoiceTargetCombo.Items.Add(new TargetFieldItem(label, icol));
				}
			}
		}

		/// <summary>
		/// Makes the items.
		/// </summary>
		public void MakeItems()
		{
			if (m_beItems != null)
			{
				return; // already made.
			}
			m_beItems = new BulkEditItem[m_bv.ColumnSpecs.Count];
			// Here we figure which columns we can bulk edit.
			var icol = -1; // will increment at start of loop.
			foreach (var colSpec in m_bv.ColumnSpecs)
			{
				icol++;
				var bei = MakeItem(colSpec);
				if (bei != null)
				{
					m_beItems[icol] = bei;
				}
			}
			m_itemIndex = -1;
		}

		private int GetFlidFromClassDotName(XElement node, string attrName)
		{
			return GetFlidFromClassDotName(m_cache, node, attrName);
		}

		/// <summary>
		/// Given a string that is supposed to identify a particular list, return the flid.
		/// </summary>
		internal static int GetFlidFromClassDotName(LcmCache cache, XElement node, string attrName)
		{
			return GetFlidFromClassDotName(cache, XmlUtils.GetMandatoryAttributeValue(node, attrName));
		}

		private static int GetFlidFromClassDotName(LcmCache cache, string descriptor)
		{
			var parts = descriptor.Trim().Split('.');
			if (parts.Length != 2)
			{
				throw new FwConfigurationException("atomicFlatListItem field must be class.field");
			}
			try
			{
				var flid = cache.DomainDataByFlid.MetaDataCache.GetFieldId(parts[0], parts[1], true);
				return flid;
			}
			catch (Exception e)
			{
				throw new FwConfigurationException("Don't recognize atomicFlatListItem field " + descriptor, e);
			}
		}

		/// <summary>
		/// Extracts the class.property information for a CmPossibilityList from a columnSpec.
		/// </summary>
		internal static void GetListInfo(XElement colSpec, out string owningClass, out string property)
		{
			GetPathInfoFromColumnSpec(colSpec, "list", "LangProject", out owningClass, out property);
		}

		/// <summary />
		/// <param name="node"></param>
		/// <param name="attrName"></param>
		/// <param name="defaultOwningClass">if only the property is specified,
		/// we'll use this as the default class for that property.</param>
		/// <param name="owningClass"></param>
		/// <param name="property"></param>
		private static void GetPathInfoFromColumnSpec(XElement node, string attrName, string defaultOwningClass, out string owningClass, out string property)
		{
			var listpath = XmlUtils.GetMandatoryAttributeValue(node, attrName);
			var parts = listpath.Trim().Split('.');
			if (parts.Length > 1)
			{
				if (parts.Length != 2)
				{
					throw new FwConfigurationException("List id must not have more than two parts " + listpath);
				}
				owningClass = parts[0];
				property = parts[1];
			}
			else
			{
				owningClass = defaultOwningClass;
				property = parts[0];
			}
		}

		private ICmPossibilityList GetNamedList(XElement node, string attrName)
		{
			return GetNamedList(m_cache, node, attrName);
		}

		/// <summary>
		/// Return Hvo of a named list. If the list does not yet exist, then
		/// this will return zero.
		/// </summary>
		/// <returns>Hvo or 0</returns>
		private int GetNamedListHvo(XElement node, string attrName)
		{
			var possList = GetNamedList(node, attrName);
			return possList?.Hvo ?? 0;
		}

		/// <summary>
		/// Return Hvo of a named list. If the list does not yet exist, then
		/// this will return SpecialHVOValues.kHvoUninitializedObject.
		/// </summary>
		/// <returns>Hvo or 0</returns>
		internal static int GetNamedListHvo(LcmCache cache, XElement node, string attrName)
		{
			var possList = GetNamedList(cache, node, attrName);
			return possList?.Hvo ?? (int)SpecialHVOValues.kHvoUninitializedObject;
		}

		/// <summary>
		/// Get a specification of a list from the specified attribute of the specified node,
		/// and return the indicated list.
		/// The descriptor is the name of one of the lists that is an attribute of
		/// LangProject, or else LexDb. followed by the name of a list that
		/// is an attribute of that class.
		/// </summary>
		/// <returns>An ICmPossibilityList (which may be null).</returns>
		internal static ICmPossibilityList GetNamedList(LcmCache cache, XElement node, string attrName)
		{
			string owningClass;
			string property;
			GetPathInfoFromColumnSpec(node, attrName, "LangProject", out owningClass, out property);
			var key = property;
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
						if (!string.IsNullOrEmpty(property))
						{
							var guidList = new Guid(property);
							return cache.ServiceLocator.GetInstance<ICmPossibilityListRepository>().GetObject(guidList);
						}
					}
					catch
					{
					}
					throw new FwConfigurationException($"List {owningClass}.{property} currently not supported.");
				default:
					throw new FwConfigurationException($"List {owningClass}.{property} currently not supported.");
			}
			var pi = owner.GetType().GetProperty(key + "OA");
			if (pi == null)
			{
				throw new FwConfigurationException($"List {owningClass}.{property} not in conceptual model.");
			}
			var result = pi.GetValue(owner, null);
			if (result != null && !(result is ICmPossibilityList))
			{
				throw new FwConfigurationException($"Specified property ({owningClass}.{property}) does not return a possibility list, but a {result.GetType().ToString()}.");
			}
			return (ICmPossibilityList)result;
		}

		internal static string GetColumnLabel(XElement colSpec)
		{
			var colName = StringTable.Table.LocalizeAttributeValue(XmlUtils.GetOptionalAttributeValue(colSpec, "label", null)) ??
						  XmlUtils.GetMandatoryAttributeValue(colSpec, "label");
			return colName;
		}
		/// <summary>
		/// Make an item
		/// </summary>
		protected virtual BulkEditItem MakeItem(XElement colSpec)
		{
			var beSpec = XmlUtils.GetOptionalAttributeValue(colSpec, "bulkEdit", string.Empty);
			IBulkEditSpecControl besc;
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
					catch (Exception)
					{
						MessageBox.Show(XMLViewsStrings.ksBarElementFailed);
						return null;
					}
					break;
				case "phonemeFlatListItem":
					besc = new PhonologicalFeatureEditor(Publisher, Subscriber, colSpec);
					break;
				case "atomicFlatListItem":
					flid = GetFlidFromClassDotName(colSpec, "field");
					hvoList = GetNamedListHvo(colSpec, "list");
					var list = (ICmPossibilityList)m_cache.ServiceLocator.GetObject(hvoList);
					if (RequiresDialogChooser(list))
					{
						besc = new ComplexListChooserBEditControl(m_cache, PropertyTable, colSpec);
						break;
					}
					ws = WritingSystemServices.GetWritingSystem(m_cache, FwUtils.ConvertElement(colSpec), null, WritingSystemServices.kwsAnal).Handle;
					besc = new FlatListChooserBEditControl(flid, hvoList, ws, false);
					break;
				case "morphTypeListItem":
					flid = GetFlidFromClassDotName(colSpec, "field");
					flidSub = GetFlidFromClassDotName(colSpec, "subfield");
					hvoList = GetNamedListHvo(colSpec, "list");
					ws = WritingSystemServices.GetWritingSystem(m_cache, FwUtils.ConvertElement(colSpec), null, WritingSystemServices.kwsAnal).Handle;
					besc = new MorphTypeChooserBEditControl(flid, flidSub, hvoList, ws, m_bv);
					break;
				case "variantConditionListItem":
					besc = new VariantEntryTypesChooserBEditControl(m_cache, PropertyTable, colSpec);
					break;
				case "integer":
					flid = GetFlidFromClassDotName(colSpec, "field");
					var stringList = m_bv.BrowseView.GetStringList(colSpec);
					if (stringList != null)
					{
						besc = new IntChooserBEditControl(stringList, flid, XmlUtils.GetOptionalIntegerValue(colSpec, "defaultBulkEditChoice", 0));
					}
					else
					{
						items = XmlUtils.GetMandatoryAttributeValue(colSpec, "items");
						besc = new IntChooserBEditControl(items, flid);
					}
					break;
				case "integerOnSubfield":
					flid = GetFlidFromClassDotName(colSpec, "field");
					flidSub = GetFlidFromClassDotName(colSpec, "subfield");
					items = XmlUtils.GetMandatoryAttributeValue(colSpec, "items");
					besc = new IntOnSubfieldChooserBEditControl(items, flid, flidSub);
					break;
				case "booleanOnSubfield":
					flid = GetFlidFromClassDotName(colSpec, "field");
					flidSub = GetFlidFromClassDotName(colSpec, "subfield");
					items = XmlUtils.GetMandatoryAttributeValue(colSpec, "items");
					besc = new BoolOnSubfieldChooserBEditControl(items, flid, flidSub);
					break;
				case "boolean":
					flid = GetFlidFromClassDotName(colSpec, "field");
					items = XmlUtils.GetMandatoryAttributeValue(colSpec, "items");
					besc = new BooleanChooserBEditControl(items, flid);
					break;
				case "complexListMultiple":
					besc = new ComplexListChooserBEditControl(m_cache, PropertyTable, colSpec);
					break;
				case "semanticDomainListMultiple":
					besc = new SemanticDomainChooserBEditControl(m_cache, PropertyTable, this, colSpec);
					break;
				case "variantEntryTypes":
					besc = new VariantEntryTypesChooserBEditControl(m_cache, PropertyTable, colSpec);
					break;
				case "complexEntryTypes":
					besc = new ComplexListChooserBEditControl(m_cache, PropertyTable, colSpec);
					break;
				default:
					return null;
			}
			besc.Cache = m_bv.Cache;
			besc.DataAccess = m_bv.SpecialCache;
			besc.Stylesheet = m_bv.StyleSheet;
			if (besc.PropertyTable != PropertyTable)
			{
				besc.PropertyTable = PropertyTable;
			}
			(besc as IGhostable)?.InitForGhostItems(besc.Cache, colSpec);
			besc.ValueChanged += besc_ValueChanged;
			var bei = new BulkEditItem(besc);
			return bei;
		}

		/// <summary>
		/// Return true if the specified list requires us to use a chooser dialog rather than putting a simple combo box
		/// in the bulk edit bar. Currently we do this if the list is (actually, not just potentially) hierarchical,
		/// or if it has more than 25 items.
		/// Note that at least one unit test will break if this method causes the default Locations list to be treated
		/// as hierarchical.
		/// </summary>
		private bool RequiresDialogChooser(ICmPossibilityList list)
		{
			return list.PossibilitiesOS.Count > 25 || list.PossibilitiesOS.Any(item => item.SubPossibilitiesOS.Any());
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
			}
			else
			{
				m_ApplyButton.Enabled = false;
				m_previewButton.Enabled = false;
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
			this.OperationLabel = new System.Windows.Forms.Label();
			this.m_operationsTabControl = new LanguageExplorer.Controls.XMLViews.VSTabControl();
			this.ListChoiceTab = new System.Windows.Forms.TabPage();
			this.m_bulkEditIconButton = new System.Windows.Forms.Button();
			this.m_imageList16x16 = new System.Windows.Forms.ImageList(this.components);
			this.BulkEditOperationLabel = new System.Windows.Forms.Label();
			this.ListChoiceChangeToCombo = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.ChangeToLabel = new System.Windows.Forms.Label();
			this.m_listChoiceTargetCombo = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.TargetFieldLabel = new System.Windows.Forms.Label();
			this.m_closeButton = new System.Windows.Forms.Button();
			this.BulkCopyTab = new System.Windows.Forms.TabPage();
			this.BulkCopySourceCombo = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.label4 = new System.Windows.Forms.Label();
			this.BulkCopyTargetCombo = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.label5 = new System.Windows.Forms.Label();
			this.ClickCopyTab = new System.Windows.Forms.TabPage();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.ClickCopySepBox = new SIL.FieldWorks.FwCoreDlgs.Controls.FwTextBox();
			this.ClickCopyOverwriteButton = new System.Windows.Forms.RadioButton();
			this.ClickCopyAppendButton = new System.Windows.Forms.RadioButton();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.ClickCopyReorderButton = new System.Windows.Forms.RadioButton();
			this.ClickCopyWordButton = new System.Windows.Forms.RadioButton();
			this.ClickCopyTargetCombo = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.label6 = new System.Windows.Forms.Label();
			this.TransduceTab = new System.Windows.Forms.TabPage();
			this.m_transduceSetupButton = new System.Windows.Forms.Button();
			this.TransduceSourceCombo = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.label7 = new System.Windows.Forms.Label();
			this.TransduceTargetCombo = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.label8 = new System.Windows.Forms.Label();
			this.TransduceProcessorCombo = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.label9 = new System.Windows.Forms.Label();
			this.FindReplaceTab = new System.Windows.Forms.TabPage();
			this.m_findReplaceSetupButton = new System.Windows.Forms.Button();
			this.FindReplaceTargetCombo = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.label12 = new System.Windows.Forms.Label();
			this.m_deleteTab = new System.Windows.Forms.TabPage();
			this.label13 = new System.Windows.Forms.Label();
			this.DeleteWhatCombo = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.m_imageList16x14 = new System.Windows.Forms.ImageList(this.components);
			this.m_helpButton = new System.Windows.Forms.Button();
			this.m_previewButton = new System.Windows.Forms.Button();
			this.m_ApplyButton = new System.Windows.Forms.Button();
			this.m_operationsTabControl.SuspendLayout();
			this.ListChoiceTab.SuspendLayout();
			this.BulkCopyTab.SuspendLayout();
			this.ClickCopyTab.SuspendLayout();
			this.groupBox2.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.ClickCopySepBox)).BeginInit();
			this.groupBox1.SuspendLayout();
			this.TransduceTab.SuspendLayout();
			this.FindReplaceTab.SuspendLayout();
			this.m_deleteTab.SuspendLayout();
			this.SuspendLayout();
			//
			// m_operationLabel
			//
			resources.ApplyResources(this.OperationLabel, "m_operationLabel");
			this.OperationLabel.BackColor = System.Drawing.Color.Transparent;
			this.OperationLabel.Name = "m_operationLabel";
			//
			// m_operationsTabControl
			//
			resources.ApplyResources(this.m_operationsTabControl, "m_operationsTabControl");
			this.m_operationsTabControl.Controls.Add(this.ListChoiceTab);
			this.m_operationsTabControl.Controls.Add(this.BulkCopyTab);
			this.m_operationsTabControl.Controls.Add(this.ClickCopyTab);
			this.m_operationsTabControl.Controls.Add(this.TransduceTab);
			this.m_operationsTabControl.Controls.Add(this.FindReplaceTab);
			this.m_operationsTabControl.Controls.Add(this.m_deleteTab);
			this.m_operationsTabControl.Name = "m_operationsTabControl";
			this.m_operationsTabControl.SelectedIndex = 0;
			//
			// m_listChoiceTab
			//
			this.ListChoiceTab.Controls.Add(this.m_bulkEditIconButton);
			this.ListChoiceTab.Controls.Add(this.OperationLabel);
			this.ListChoiceTab.Controls.Add(this.BulkEditOperationLabel);
			this.ListChoiceTab.Controls.Add(this.ListChoiceChangeToCombo);
			this.ListChoiceTab.Controls.Add(this.ChangeToLabel);
			this.ListChoiceTab.Controls.Add(this.m_listChoiceTargetCombo);
			this.ListChoiceTab.Controls.Add(this.TargetFieldLabel);
			this.ListChoiceTab.Controls.Add(this.m_closeButton);
			resources.ApplyResources(this.ListChoiceTab, "m_listChoiceTab");
			this.ListChoiceTab.Name = "m_listChoiceTab";
			this.ListChoiceTab.UseVisualStyleBackColor = true;
			this.ListChoiceTab.Enter += new System.EventHandler(this.m_listChoiceTab_Enter);
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
			resources.ApplyResources(this.BulkEditOperationLabel, "m_bulkEditOperationLabel");
			this.BulkEditOperationLabel.BackColor = System.Drawing.Color.Transparent;
			this.BulkEditOperationLabel.ForeColor = System.Drawing.Color.Blue;
			this.BulkEditOperationLabel.Name = "m_bulkEditOperationLabel";
			//
			// m_listChoiceChangeToCombo
			//
			this.ListChoiceChangeToCombo.AllowSpaceInEditBox = false;
			this.ListChoiceChangeToCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.ListChoiceChangeToCombo, "m_listChoiceChangeToCombo");
			this.ListChoiceChangeToCombo.Name = "m_listChoiceChangeToCombo";
			//
			// label3
			//
			resources.ApplyResources(this.ChangeToLabel, "label3");
			this.ChangeToLabel.Name = "label3";
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
			resources.ApplyResources(this.TargetFieldLabel, "label2");
			this.TargetFieldLabel.Name = "label2";
			//
			// m_closeButton
			//
			resources.ApplyResources(this.m_closeButton, "m_closeButton");
			this.m_closeButton.ForeColor = System.Drawing.SystemColors.Control;
			this.m_closeButton.Name = "m_closeButton";
			//
			// m_bulkCopyTab
			//
			this.BulkCopyTab.Controls.Add(this.BulkCopySourceCombo);
			this.BulkCopyTab.Controls.Add(this.label4);
			this.BulkCopyTab.Controls.Add(this.BulkCopyTargetCombo);
			this.BulkCopyTab.Controls.Add(this.label5);
			resources.ApplyResources(this.BulkCopyTab, "m_bulkCopyTab");
			this.BulkCopyTab.ForeColor = System.Drawing.SystemColors.ControlText;
			this.BulkCopyTab.Name = "m_bulkCopyTab";
			this.BulkCopyTab.UseVisualStyleBackColor = true;
			this.BulkCopyTab.Enter += new System.EventHandler(this.m_bulkCopyTab_Enter);
			//
			// m_bulkCopySourceCombo
			//
			this.BulkCopySourceCombo.AllowSpaceInEditBox = false;
			this.BulkCopySourceCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.BulkCopySourceCombo, "m_bulkCopySourceCombo");
			this.BulkCopySourceCombo.Name = "m_bulkCopySourceCombo";
			//
			// label4
			//
			resources.ApplyResources(this.label4, "label4");
			this.label4.Name = "label4";
			//
			// m_bulkCopyTargetCombo
			//
			this.BulkCopyTargetCombo.AllowSpaceInEditBox = false;
			this.BulkCopyTargetCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.BulkCopyTargetCombo, "m_bulkCopyTargetCombo");
			this.BulkCopyTargetCombo.Name = "m_bulkCopyTargetCombo";
			//
			// label5
			//
			resources.ApplyResources(this.label5, "label5");
			this.label5.Name = "label5";
			//
			// m_clickCopyTab
			//
			this.ClickCopyTab.Controls.Add(this.groupBox2);
			this.ClickCopyTab.Controls.Add(this.groupBox1);
			this.ClickCopyTab.Controls.Add(this.ClickCopyTargetCombo);
			this.ClickCopyTab.Controls.Add(this.label6);
			resources.ApplyResources(this.ClickCopyTab, "m_clickCopyTab");
			this.ClickCopyTab.Name = "m_clickCopyTab";
			this.ClickCopyTab.UseVisualStyleBackColor = true;
			this.ClickCopyTab.Enter += new System.EventHandler(this.m_clickCopyTab_Enter);
			//
			// groupBox2
			//
			this.groupBox2.Controls.Add(this.ClickCopySepBox);
			this.groupBox2.Controls.Add(this.ClickCopyOverwriteButton);
			this.groupBox2.Controls.Add(this.ClickCopyAppendButton);
			resources.ApplyResources(this.groupBox2, "groupBox2");
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.TabStop = false;
			//
			// m_clickCopySepBox
			//
			this.ClickCopySepBox.AdjustStringHeight = true;
			this.ClickCopySepBox.BackColor = System.Drawing.SystemColors.Window;
			this.ClickCopySepBox.controlID = null;
			resources.ApplyResources(this.ClickCopySepBox, "m_clickCopySepBox");
			this.ClickCopySepBox.HasBorder = true;
			this.ClickCopySepBox.Name = "m_clickCopySepBox";
			this.ClickCopySepBox.SelectionLength = 0;
			this.ClickCopySepBox.SelectionStart = 0;
			//
			// m_clickCopyOverwriteButton
			//
			resources.ApplyResources(this.ClickCopyOverwriteButton, "m_clickCopyOverwriteButton");
			this.ClickCopyOverwriteButton.Name = "m_clickCopyOverwriteButton";
			//
			// m_clickCopyAppendButton
			//
			resources.ApplyResources(this.ClickCopyAppendButton, "m_clickCopyAppendButton");
			this.ClickCopyAppendButton.Checked = true;
			this.ClickCopyAppendButton.Name = "m_clickCopyAppendButton";
			this.ClickCopyAppendButton.TabStop = true;
			//
			// groupBox1
			//
			this.groupBox1.Controls.Add(this.ClickCopyReorderButton);
			this.groupBox1.Controls.Add(this.ClickCopyWordButton);
			resources.ApplyResources(this.groupBox1, "groupBox1");
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.TabStop = false;
			//
			// m_clickCopyReorderButton
			//
			resources.ApplyResources(this.ClickCopyReorderButton, "m_clickCopyReorderButton");
			this.ClickCopyReorderButton.Name = "m_clickCopyReorderButton";
			//
			// m_clickCopyWordButton
			//
			resources.ApplyResources(this.ClickCopyWordButton, "m_clickCopyWordButton");
			this.ClickCopyWordButton.Checked = true;
			this.ClickCopyWordButton.Name = "m_clickCopyWordButton";
			this.ClickCopyWordButton.TabStop = true;
			//
			// m_clickCopyTargetCombo
			//
			this.ClickCopyTargetCombo.AllowSpaceInEditBox = false;
			this.ClickCopyTargetCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.ClickCopyTargetCombo, "m_clickCopyTargetCombo");
			this.ClickCopyTargetCombo.Name = "m_clickCopyTargetCombo";
			this.ClickCopyTargetCombo.SelectedIndexChanged += new System.EventHandler(this.m_clickCopyTargetCombo_SelectedIndexChanged);
			//
			// label6
			//
			resources.ApplyResources(this.label6, "label6");
			this.label6.Name = "label6";
			//
			// m_transduceTab
			//
			this.TransduceTab.Controls.Add(this.m_transduceSetupButton);
			this.TransduceTab.Controls.Add(this.TransduceSourceCombo);
			this.TransduceTab.Controls.Add(this.label7);
			this.TransduceTab.Controls.Add(this.TransduceTargetCombo);
			this.TransduceTab.Controls.Add(this.label8);
			this.TransduceTab.Controls.Add(this.TransduceProcessorCombo);
			this.TransduceTab.Controls.Add(this.label9);
			resources.ApplyResources(this.TransduceTab, "m_transduceTab");
			this.TransduceTab.Name = "m_transduceTab";
			this.TransduceTab.UseVisualStyleBackColor = true;
			this.TransduceTab.Enter += new System.EventHandler(this.m_transduceTab_Enter);
			//
			// m_transduceSetupButton
			//
			resources.ApplyResources(this.m_transduceSetupButton, "m_transduceSetupButton");
			this.m_transduceSetupButton.Name = "m_transduceSetupButton";
			this.m_transduceSetupButton.Click += new System.EventHandler(this.m_transduceSetupButton_Click);
			//
			// m_transduceSourceCombo
			//
			this.TransduceSourceCombo.AllowSpaceInEditBox = false;
			this.TransduceSourceCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.TransduceSourceCombo, "m_transduceSourceCombo");
			this.TransduceSourceCombo.Name = "m_transduceSourceCombo";
			//
			// label7
			//
			resources.ApplyResources(this.label7, "label7");
			this.label7.Name = "label7";
			//
			// m_transduceTargetCombo
			//
			this.TransduceTargetCombo.AllowSpaceInEditBox = false;
			this.TransduceTargetCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.TransduceTargetCombo, "m_transduceTargetCombo");
			this.TransduceTargetCombo.Name = "m_transduceTargetCombo";
			//
			// label8
			//
			resources.ApplyResources(this.label8, "label8");
			this.label8.Name = "label8";
			//
			// m_transduceProcessorCombo
			//
			this.TransduceProcessorCombo.AllowSpaceInEditBox = false;
			this.TransduceProcessorCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.TransduceProcessorCombo.Items.AddRange(new object[] {
			resources.GetString("m_transduceProcessorCombo.Items"),
			resources.GetString("m_transduceProcessorCombo.Items1")});
			resources.ApplyResources(this.TransduceProcessorCombo, "m_transduceProcessorCombo");
			this.TransduceProcessorCombo.Name = "m_transduceProcessorCombo";
			//
			// label9
			//
			resources.ApplyResources(this.label9, "label9");
			this.label9.Name = "label9";
			//
			// m_findReplaceTab
			//
			this.FindReplaceTab.Controls.Add(this.m_findReplaceSetupButton);
			this.FindReplaceTab.Controls.Add(this.FindReplaceTargetCombo);
			this.FindReplaceTab.Controls.Add(this.label12);
			resources.ApplyResources(this.FindReplaceTab, "m_findReplaceTab");
			this.FindReplaceTab.Name = "m_findReplaceTab";
			this.FindReplaceTab.UseVisualStyleBackColor = true;
			this.FindReplaceTab.Enter += new System.EventHandler(this.m_findReplaceTab_Enter);
			//
			// m_findReplaceSetupButton
			//
			resources.ApplyResources(this.m_findReplaceSetupButton, "m_findReplaceSetupButton");
			this.m_findReplaceSetupButton.Name = "m_findReplaceSetupButton";
			this.m_findReplaceSetupButton.Click += new System.EventHandler(this.m_findReplaceSetupButton_Click);
			//
			// m_findReplaceTargetCombo
			//
			this.FindReplaceTargetCombo.AllowSpaceInEditBox = false;
			this.FindReplaceTargetCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.FindReplaceTargetCombo, "m_findReplaceTargetCombo");
			this.FindReplaceTargetCombo.Name = "m_findReplaceTargetCombo";
			this.FindReplaceTargetCombo.SelectedIndexChanged += new System.EventHandler(this.m_findReplaceTargetCombo_SelectedIndexChanged);
			//
			// label12
			//
			resources.ApplyResources(this.label12, "label12");
			this.label12.Name = "label12";
			//
			// m_deleteTab
			//
			this.m_deleteTab.Controls.Add(this.label13);
			this.m_deleteTab.Controls.Add(this.DeleteWhatCombo);
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
			this.DeleteWhatCombo.AllowSpaceInEditBox = false;
			this.DeleteWhatCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.DeleteWhatCombo, "m_deleteWhatCombo");
			this.DeleteWhatCombo.Name = "m_deleteWhatCombo";
			this.DeleteWhatCombo.SelectedIndexChanged += new System.EventHandler(this.m_deleteWhatCombo_SelectedIndexChanged);
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
			this.ListChoiceTab.ResumeLayout(false);
			this.ListChoiceTab.PerformLayout();
			this.BulkCopyTab.ResumeLayout(false);
			this.BulkCopyTab.PerformLayout();
			this.ClickCopyTab.ResumeLayout(false);
			this.ClickCopyTab.PerformLayout();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.ClickCopySepBox)).EndInit();
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.TransduceTab.ResumeLayout(false);
			this.TransduceTab.PerformLayout();
			this.FindReplaceTab.ResumeLayout(false);
			this.FindReplaceTab.PerformLayout();
			this.m_deleteTab.ResumeLayout(false);
			this.m_deleteTab.PerformLayout();
			this.ResumeLayout(false);

		}
		#endregion

		private bool DeleteRowsItemSelected
		{
			get
			{
				if (m_operationsTabControl.SelectedTab != m_deleteTab)
				{
					return false;
				}
				return DeleteWhatCombo.SelectedItem is ListClassTargetFieldItem;
			}
		}

		private List<ListClassTargetFieldItem> ListItemsClassesInfo(HashSet<int> classes)
		{
			var targetClasses = new List<ListClassTargetFieldItem>();
			foreach (var classId in classes)
			{
				string pluralOfClass;
				// get plural form labels from AlternativeTitles
				XmlViewsUtils.TryFindPluralFormFromClassId(m_bv.SpecialCache.MetaDataCache, classId, out pluralOfClass);
				if (pluralOfClass.Length > 0)
				{
					targetClasses.Add(new ListClassTargetFieldItem(pluralOfClass + " (" + XMLViewsStrings.ksRows + ")", classId));
				}
			}
			return targetClasses;
		}

		internal void m_deleteWhatCombo_SelectedIndexChanged(object sender, EventArgs e)
		{
			var fWasPreviewOn = PreviewOn;
			if (fWasPreviewOn)
			{
				ClearPreview();
			}
			var selectedItem = DeleteWhatCombo.SelectedItem as FieldComboItem;
			// need to change target before we do anything else, because calculating
			// the items depends upon having the right sort of items listed.
			OnTargetComboItemChanged(selectedItem);
			var fWasShowEnabled = m_bv.BrowseView.Vc.ShowEnabled;
			if (DeleteRowsItemSelected)
			{
				// Delete rows
				OperationLabel.Text = s_labels[m_operationsTabControl.SelectedIndex];
				m_ApplyButton.Text = XMLViewsStrings.ksDelete;
				m_previewButton.Enabled = false;
				m_bv.BrowseView.Vc.ShowEnabled = true;
				SetEnabledForAllItems();
			}
			else
			{
				OperationLabel.Text = XMLViewsStrings.ksDeleteDesc2;
				m_previewButton.Enabled = true;
				m_ApplyButton.Text = m_originalApplyText;
				m_bv.BrowseView.Vc.ShowEnabled = false;
			}
			if (fWasPreviewOn || fWasShowEnabled != m_bv.BrowseView.Vc.ShowEnabled && m_bv.BrowseView.RootBox != null)
			{
				m_bv.BrowseView.RootBox.Reconstruct();
			}
		}

		internal void SetEnabledIfShowing()
		{
			if (!m_bv.BrowseView.Vc.ShowEnabled)
			{
				return;
			}
			SetEnabledForAllItems();
			m_bv.BrowseView.RootBox.Reconstruct();
		}

		private void SetEnabledForAllItems()
		{
			m_items = ItemsToChangeSet(false);
			UpdateCurrentGhostParentHelper(); // needed for AllowDeleteItem()
			foreach (var hvoItem in m_items)
			{
				// Use special SDA.
				m_bv.SpecialCache.SetInt(hvoItem, XMLViewsDataCache.ktagItemEnabled, AllowDeleteItem(hvoItem));
			}
		}

		internal void UpdateCheckedItems()
		{
			if (!m_bv.BrowseView.Vc.ShowEnabled || m_items != null && m_items.Any())
			{
				return;
			}
			// it's about time to try to load these items and their checked state.
			SetEnabledForAllItems();
			m_bv.BrowseView.RootBox?.Reconstruct();
		}

		/// <summary>
		/// The selected state of the specified item may be changing, update enable values as appropriate.
		/// </summary>
		internal void UpdateEnableItems(int hvoItem)
		{
			// Only relevant for the delete rows function.
			if (!DeleteRowsItemSelected)
			{
				return;
			}
			// currently only handle update enable for Sense objects.
			var clsid = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoItem).ClassID;
			if (clsid != LexSenseTags.kClassId)
			{
				return; // currently we only restrict senses.
			}
			var hvoOwner = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoItem).Owner.Hvo;
			var clsOwner = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoOwner).ClassID;
			if (clsOwner != LexEntryTags.kClassId)
			{
				return; // subsenses are always OK to delete, change can't have any effect.
			}
			var chvo = m_bv.SpecialCache.get_VecSize(hvoOwner, LexEntryTags.kflidSenses);
			if (chvo == 1)
			{
				return; // only sense, nothing can have changed.
			}
			UpdateCurrentGhostParentHelper();
			for (var i = 0; i < chvo; i++)
			{
				var hvoSense = m_bv.SpecialCache.get_VecItem(hvoOwner, LexEntryTags.kflidSenses, i);
				var wasEnabled = m_bv.SpecialCache.get_IntProp(hvoSense, XMLViewsDataCache.ktagItemEnabled);
				var enabled = AllowDeleteItem(hvoSense);
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
		private int AllowDeleteItem(int hvoItem)
		{
			var clsid = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoItem).ClassID;
			// we only want to disallow the first sense, if we're not dealing with ghostable targets.
			if (clsid == LexSenseTags.kClassId && m_ghostParentHelper == null)
			{
				var hvoOwner = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoItem).Owner.Hvo;
				var clsOwner = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoOwner).ClassID;
				if (clsOwner != LexEntryTags.kClassId)
				{
					return 1; // subsenses are always OK to delete.
				}
				var sda = m_cache.DomainDataByFlid;
				var chvo = sda.get_VecSize(hvoOwner, LexEntryTags.kflidSenses);
				if (chvo == 1)
				{
					return 0; // can't delete only sense.
				}
				for (var i = 0; i < chvo; i++)
				{
					var hvoSense = sda.get_VecItem(hvoOwner, LexEntryTags.kflidSenses, i);
					if (hvoSense == hvoItem)
					{
						// Senses other than the first are never disabled. Deleting the first sense
						// is disabled if all others are marked for deletion.
						if (i != 0)
						{
							return 1;
						}
						continue; // We're looking for another sense not marked for deletion
					}
					if (!m_items.Contains(hvoSense))
					{
						return 1; // there's a sense currently not even in the filter.
					}
					if (m_bv.GetCheckState(hvoSense) == 0)
					{
						return 1; // some other sense is not being deleted, ok to delete this.
					}
				}
				return 0; // this is the first sense and is the only one not marked for deletion.
			}
			return CanDeleteItemOfClassOrGhostOwner(hvoItem, clsid) ? 1 : 0;
		}

		private bool CanDeleteItemOfClassOrGhostOwner(int hvoItem, int clsid)
		{
			//allow deletion for the class we expect to be bulk editing.
			if (DomainObjectServices.IsSameOrSubclassOf(m_cache.DomainDataByFlid.MetaDataCache, clsid, m_expectedListItemsClassId))
			{
				return true;
			}
			// allow bulk delete if ghost child already exists, otherwise be disabled.
			// NOTE: in this case UpdateCurrentGhostParentHelper() needs to be called prior
			// to calling AllowDeleteItem().
			if (m_ghostParentHelper != null && m_ghostParentHelper.IsGhostOwnerClass(hvoItem) && !m_ghostParentHelper.IsGhostOwnerChildless(hvoItem))
			{
				// child exists, so allow bulk editing for child.
				return true;
			}
			return false;
		}

		/// <summary>
		/// needed for AllowDeleteItem().
		/// </summary>
		private GhostParentHelper UpdateCurrentGhostParentHelper()
		{
			m_ghostParentHelper = null;
			// see if the object is a ghost object owner.
			foreach (var helper in m_bulkEditListItemsGhostFields)
			{
				if (helper.TargetClass == m_expectedListItemsClassId)
				{
					m_ghostParentHelper = helper;
					break;
				}
			}
			return m_ghostParentHelper;
		}

		private void m_helpButton_Click(object sender, EventArgs e)
		{
			var helpTopic = string.Empty;
			switch (PropertyTable.GetValue<string>(AreaServices.ToolChoice))
			{
				case AreaServices.BulkEditEntriesOrSensesMachineName:
					helpTopic = "khtpBulkEditBarEntriesOrSenses";
					break;
				case AreaServices.ReversalBulkEditReversalEntriesMachineName:
					helpTopic = "khtpBulkEditBarReversalEntries";
					break;
				case AreaServices.BulkEditWordformsMachineName:
					helpTopic = "khtpBulkEditBarWordforms";
					break;
				case AreaServices.BulkEditPhonemesMachineName:
					helpTopic = "khtpBulkEditBarPhonemes";
					break;
			}
			if (helpTopic != string.Empty)
			{
				ShowHelp.ShowHelpTopic(PropertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider), helpTopic);
			}
		}

		/// <summary>
		/// Handles searching for Semantic Domain suggestions to put in the Preview
		/// </summary>
		protected void m_suggestButton_Click(object sender, EventArgs e)
		{
			HandlePreviewOrSuggestTask(DoSuggestTask);
		}

		private void DoSuggestTask(ProgressState state)
		{
			var newCol = -1;
			if (PreviewOn)
			{
				// Clear the previous preview
				DoClearPreviewTask(state);
			}
			PreviewOn = true;
			if (m_operationsTabControl.SelectedTab == ListChoiceTab)
			{
				if (m_itemIndex >= 0)
				{
					MakeSuggestions(state);
					newCol = m_itemIndex + 1;
				}
			}
			else
			{
				MessageBox.Show(this, XMLViewsStrings.ksSorryNoPreview, XMLViewsStrings.ksUnimplFeature);
				PreviewOn = false; // Didn't actually happen
			}
			if (newCol > 0)
			{
				// Use the BrowseViewer special 'decorator' SDA.
				m_bv.SpecialCache.SetInt(m_bv.RootObjectHvo, XMLViewsDataCache.ktagActiveColumn, newCol);
			}
		}

		private void MakeSuggestions(ProgressState state)
		{
			var bei = m_beItems[m_itemIndex];
			bei.BulkEditControl.MakeSuggestions(ItemsToChange(false), XMLViewsDataCache.ktagAlternateValue, XMLViewsDataCache.ktagItemEnabled, state);
		}

		private const int SUGGEST_BTN_YOFFSET = 30;

		internal void m_listChoiceTargetCombo_SelectedIndexChanged(object sender, EventArgs e)
		{
			var tfm = m_listChoiceTargetCombo.SelectedItem as TargetFieldItem;
			if (tfm == null)
			{
				// Show the dummy combo if this somehow happens, just to give the look and feel.
				ListChoiceChangeToCombo.Visible = true;
				return;
			}
			var index = tfm.ColumnIndex;
			ListChoiceTab.SuspendLayout();
			RemoveOldChoiceControl();
			var bei = m_beItems[index];
			ListChoiceChangeToCombo.Visible = false; // it's a placeholder
			ListChoiceChangeToCombo.Enabled = false;
			m_listChoiceControl = bei.BulkEditControl.Control;
			m_listChoiceControl.Location = ListChoiceChangeToCombo.Location;
			m_listChoiceControl.Size = ListChoiceChangeToCombo.Size;
			m_listChoiceControl.Anchor = ListChoiceChangeToCombo.Anchor;
			Debug.Assert(string.IsNullOrEmpty(m_listChoiceControl.Name), $"not sure if we want to permenently overwrite an existing name. was {m_listChoiceControl.Name}. currently used in BulkEditBarTests for Controls.Find");
			m_listChoiceControl.Name = "m_listChoiceControl";
			m_itemIndex = index;
			ListChoiceTab.Controls.Add(m_listChoiceControl);
			// If the new control has a Suggest button, add it.
			// If not, it was already removed in RemoveOldChoiceControl() above.
			CheckForAndSetupSuggestButton(bei);
			ListChoiceTab.ResumeLayout(false);
			ListChoiceTab.PerformLayout();
			EnablePreviewApplyForListChoice();
			OnTargetComboItemChanged(tfm);
		}

		private void CheckForAndSetupSuggestButton(BulkEditItem bei)
		{
			var button = bei.BulkEditControl.SuggestButton;
			if (button == null)
			{
				return;
			}
			m_suggestButton = button;
			ListChoiceTab.Controls.Add(m_suggestButton);
			m_suggestButton.Location = new Point(m_listChoiceControl.Location.X, m_listChoiceControl.Location.Y + SUGGEST_BTN_YOFFSET);
			m_suggestButton.Size = m_listChoiceControl.Size;
			m_suggestButton.Click += m_suggestButton_Click;
			m_suggestButton.Visible = true;
		}

		/// <summary>
		/// Remove old (bulk edit) choice control
		/// </summary>
		private void RemoveOldChoiceControl()
		{
			if (m_listChoiceControl == null)
			{
				return;
			}
			ListChoiceTab.Controls.Remove(m_listChoiceControl);
			m_listChoiceControl.Name = string.Empty; // no longer the current listChoiceControl
			m_listChoiceControl = null;
			if (m_suggestButton == null)
			{
				return;
			}
			m_suggestButton.Click -= m_suggestButton_Click;
			ListChoiceTab.Controls.Remove(m_suggestButton);
			m_suggestButton = null;
		}

		private bool PreviewOn
		{
			get { return m_previewOn; }
			set
			{
				m_previewOn = value;
				switch (m_previewOn)
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
		/// Handles a click on the Preview (or Clear [Preview]) button
		/// </summary>
		protected internal void m_previewButton_Click(object sender, EventArgs e)
		{
			if (PreviewOn)
			{
				HandlePreviewOrSuggestTask(DoClearPreviewTask);
			}
			else
			{
				HandlePreviewOrSuggestTask(DoPreviewTask);
			}
		}

		private void HandlePreviewOrSuggestTask(Action<ProgressState> previewOrSuggestTask)
		{
			// Fixes LT-8336 by making sure that a highlighted row that was visible before the change is
			// still visible after it too.
			using (new ReconstructPreservingBVScrollPosition(m_bv))
			{
				// Both of these need manual disposing.
				// 'using' with the ProgressState fixes LT-4186, since it forces the manual Dispose call,
				// which, in turn, clears the progress panel.
				using (var state = CreateSimpleProgressState())
				using (new WaitCursor(this))
				{
					previewOrSuggestTask(state);
				}
			} // Does RootBox.Reconstruct() here.
		}

		internal void LaunchPreview()
		{
			HandlePreviewOrSuggestTask(DoPreviewTask);
		}

		private void DoPreviewTask(ProgressState state)
		{
			var newCol = -1;
			if (PreviewOn)
			{
				// Clear the previous preview
				DoClearPreviewTask(state);
			}
			PreviewOn = true;

			if (m_operationsTabControl.SelectedTab == ListChoiceTab)
			{
				if (m_itemIndex >= 0)
				{
					ShowPreviewItems(state);
					newCol = m_itemIndex + 1;
				}
			}
			else if (m_operationsTabControl.SelectedTab == FindReplaceTab)
			{
				var method = MakeReplaceWithMethod(out newCol);
				if (method == null)
				{
					return;
				}
				method.FakeDoit(ItemsToChange(false), XMLViewsDataCache.ktagAlternateValue, XMLViewsDataCache.ktagItemEnabled, state);
			}
			else if (m_operationsTabControl.SelectedTab == BulkCopyTab)
			{
				var method = MakeBulkCopyMethod(out newCol);
				if (method == null)
				{
					return;
				}
				method.FakeDoit(ItemsToChange(false), XMLViewsDataCache.ktagAlternateValue, XMLViewsDataCache.ktagItemEnabled, state);
			}
			else if (m_operationsTabControl.SelectedTab == TransduceTab)
			{
				var method = MakeTransduceMethod(out newCol);
				if (method == null)
				{
					return;
				}
				method.FakeDoit(ItemsToChange(false), XMLViewsDataCache.ktagAlternateValue, XMLViewsDataCache.ktagItemEnabled, state);
			}
			else if (m_operationsTabControl.SelectedTab == m_deleteTab && !DeleteRowsItemSelected)
			{
				// clear a field
				if (DeleteWhatCombo.SelectedItem is TargetFieldItem)
				{
					var item = DeleteWhatCombo.SelectedItem as TargetFieldItem;
					var index = item.ColumnIndex;
					var bei = m_beItems[index];
					bei.BulkEditControl.SetClearField();
					bei.BulkEditControl.FakeDoit(ItemsToChange(false), XMLViewsDataCache.ktagAlternateValue, XMLViewsDataCache.ktagItemEnabled, state);
					newCol = index + 1;
				}
				else if (DeleteWhatCombo.SelectedItem is FieldComboItem)
				{
					var method = MakeClearMethod(out newCol);
					if (method == null)
					{
						return;
					}
					method.FakeDoit(ItemsToChange(false), XMLViewsDataCache.ktagAlternateValue, XMLViewsDataCache.ktagItemEnabled, state);
				}
			}
			else
			{
				MessageBox.Show(this, XMLViewsStrings.ksSorryNoPreview, XMLViewsStrings.ksUnimplFeature);
				PreviewOn = false; // Didn't actually happen
			}

			if (newCol != -1)
			{
				// Use the BrowseViewer special 'decorator' SDA.
				m_bv.SpecialCache.SetInt(m_bv.RootObjectHvo, XMLViewsDataCache.ktagActiveColumn, newCol);
			}
		}

		/// <summary/>
		protected virtual void ShowPreviewItems(ProgressState state)
		{
			var bei = m_beItems[m_itemIndex];
			bei.BulkEditControl.FakeDoit(ItemsToChange(false), XMLViewsDataCache.ktagAlternateValue, XMLViewsDataCache.ktagItemEnabled, state);
		}

		internal void ClearPreview()
		{
			HandlePreviewOrSuggestTask(DoClearPreviewTask);
		}

		private void DoClearPreviewTask(ProgressState state)
		{
			m_bv.SpecialCache.SetInt(m_bv.RootObjectHvo, XMLViewsDataCache.ktagActiveColumn, 0);
			PreviewOn = false;
		}

		/// <summary>
		/// Make a ReplaceWithMethod (used in preview and apply click methods for Replace tab).
		/// </summary>
		ReplaceWithMethod MakeReplaceWithMethod(out int newActiveColumn)
		{
			newActiveColumn = 0;  // in case we fail.
			var fci = FindReplaceTargetCombo.SelectedItem as FieldComboItem;
			if (fci == null)
			{
				MessageBox.Show(XMLViewsStrings.ksChooseEditTarget);
				return null;
			}
			newActiveColumn = fci.ColumnIndex + 1;
			return new ReplaceWithMethod(m_cache, m_bv.SpecialCache, fci.Accessor, m_bv.ColumnSpecs[fci.ColumnIndex], Pattern, TssReplace);
		}

		/// <summary>
		/// Make a ClearMethod (used in preview and apply click methods for Delete tab for fields).
		/// </summary>
		private ClearMethod MakeClearMethod(out int newActiveColumn)
		{
			newActiveColumn = 0;  // in case we fail.
			var fci = DeleteWhatCombo.SelectedItem as FieldComboItem;
			if (fci == null)
			{
				MessageBox.Show(XMLViewsStrings.ksChooseClearTarget);
				return null;
			}
			newActiveColumn = fci.ColumnIndex + 1;
			return new ClearMethod(m_cache, m_bv.SpecialCache, fci.Accessor, m_bv.ColumnSpecs[fci.ColumnIndex]);
		}

		/// <summary>
		/// Make a TransduceMethod (used in preview and apply click methods for Transduce tab).
		/// </summary>
		private TransduceMethod MakeTransduceMethod(out int newActiveColumn)
		{
			newActiveColumn = 0;  // in case we fail.
			var fci = TransduceTargetCombo.SelectedItem as FieldComboItem;
			if (fci == null)
			{
				MessageBox.Show(XMLViewsStrings.ksChooseDestination);
				return null;
			}
			var fciSrc = TransduceSourceCombo.SelectedItem as FieldComboItem;
			if (fciSrc == null)
			{
				MessageBox.Show(XMLViewsStrings.ksChooseSource);
				return null;
			}
			newActiveColumn = fci.ColumnIndex + 1;
			var convName = TransduceProcessorCombo.SelectedItem as string;
			if (convName == null)
			{
				MessageBox.Show(XMLViewsStrings.ksChooseTransducer, XMLViewsStrings.ksSelectProcess);
				return null;
			}
			IEncConverters encConverters;
			try
			{
				encConverters = new EncConverters();
			}
			catch (Exception e)
			{
				MessageBox.Show(string.Format(XMLViewsStrings.ksCannotAccessEC, e.Message));
				return null;
			}
			var converter = encConverters[convName];
			return new TransduceMethod(m_cache, m_bv.SpecialCache, fci.Accessor, m_bv.ColumnSpecs[fci.ColumnIndex], fciSrc.Accessor, converter,
				TrdNonEmptyTargetControl.TssSeparator, TrdNonEmptyTargetControl.NonEmptyMode);
		}

		/// <summary>
		/// Make a BulkCopyMethod (used in preview and apply click methods for Bulk Copy tab).
		/// </summary>
		private BulkCopyMethod MakeBulkCopyMethod(out int newActiveColumn)
		{
			newActiveColumn = 0;  // in case we fail.
			var fci = BulkCopyTargetCombo.SelectedItem as FieldComboItem;
			if (fci == null)
			{
				MessageBox.Show(XMLViewsStrings.ksChooseDestination);
				return null;
			}
			var fciSrc = BulkCopySourceCombo.SelectedItem as FieldComboItem;
			if (fciSrc == null)
			{
				MessageBox.Show(XMLViewsStrings.ksChooseSource);
				return null;
			}
			newActiveColumn = fci.ColumnIndex + 1;
			var srcCol = fciSrc.ColumnIndex + 1;
			if (newActiveColumn == srcCol)
			{
				MessageBox.Show(XMLViewsStrings.ksSrcDstMustDiffer);
				return null;
			}
			return new BulkCopyMethod(m_cache, m_bv.SpecialCache, fci.Accessor, m_bv.ColumnSpecs[fci.ColumnIndex], fciSrc.Accessor,
				BcNonEmptyTargetControl.TssSeparator, BcNonEmptyTargetControl.NonEmptyMode);
		}

		/// <summary>
		/// Return the items that should be changed by the current bulk-edit operation. If the flag is set,
		/// (which also means we will really do it), return only the ones the user has checked.
		/// Also in that case, we force a partial sort by homograph number for any lexical entries.
		/// This helps preserve the homograph numbers in the case where the change causes more than one item
		/// to move from one homograph set to another.
		/// </summary>
		protected internal IEnumerable<int> ItemsToChange(bool fOnlyIfSelected)
		{
			var result = ItemsToChangeSet(fOnlyIfSelected);
			if (!fOnlyIfSelected)
			{
				return result; // Unordered is fine for preview
			}
			var repo = m_cache.ServiceLocator.ObjectRepository;
			var objects = result.Select(hvo => repo.GetObject(hvo)).ToList();
			// The objective is to sort them so that we modify anything that affects a homograph number before we modify
			// anything that affects the HN of a homograph of the same entry. Thus, things like LexemeForm that affect
			// the owning entry are sorted by the owning entry.
			// Where entries have the same HN, we sort arbitrarily.
			// This means for example that when a bulk edit merges two groups of homographs, changing both, the resulting
			// order will assign 1 and 2 to the original HN1 items, then 3 and 4 to the original HN2 ones, and so forth.
			// It will be unpredictable which gets 1 and which gets 2 and so forth.
			// If this becomes an issue a smarter sort could be produced. It would be slighly more predictable if we just
			// compared HVO when HN is the same, or we could compare Headwords (before the change).
			objects.Sort((x, y) =>
			{
				var entry1 = x as ILexEntry;
				if (entry1 == null && x is IMoForm)
				{
					entry1 = x.Owner as ILexEntry;
				}
				var entry2 = y as ILexEntry;
				if (entry2 == null && y is IMoForm)
				{
					entry1 = y.Owner as ILexEntry;
				}
				if (entry1 == null)
				{
					return entry2 == null ? 0 : -1; // any entry is larger than a non-entry.
				}
				if (entry2 == null)
				{
					return 1;
				}
				return entry1.HomographNumber.CompareTo(entry2.HomographNumber);
			});
			return objects.Select(obj => obj.Hvo).ToList(); // probably counted at least twice and enumerated, so collection is likely more efficient.
		}

		internal ISet<int> ItemsToChangeSet(bool fOnlyIfSelected)
		{
			return new HashSet<int>(fOnlyIfSelected ? m_bv.CheckedItems : m_bv.AllItems);
		}

		/// <summary>
		/// Create a default progress state that we can update simply by setting PercentDone
		/// and calling Breath.
		/// Note that most of the methods for doing this are methods of FwMainWnd. But we can't use those
		/// because this project can't reference XWorks.
		/// Possibly all of them could be moved to the project that defines StatusBarProgressPanel?
		/// </summary>
		private ProgressState CreateSimpleProgressState()
		{
			var panel = PropertyTable.GetValue<StatusBarProgressPanel>("ProgressBar");
			return panel == null ? new NullProgressState() : new ProgressState(panel);
		}

		/// <summary>
		/// Called when [enable bulk edit buttons].
		/// </summary>
		public void OnEnableBulkEditButtons(object value)
		{
			var fEnable = (bool)value;
			m_ApplyButton.Enabled = fEnable;
			m_previewButton.Enabled = fEnable;
		}

		/// <summary />
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
				using (var state = CreateSimpleProgressState())
				using (new WaitCursor(this))
				{
					if (m_operationsTabControl.SelectedTab == ListChoiceTab)
					{
						if (m_itemIndex >= 0)
						{
							var bei = m_beItems[m_itemIndex];
							bei.BulkEditControl.DoIt(ItemsToChange(true), state);
							m_bv.RefreshDisplay();
						}
					}
					else if (m_operationsTabControl.SelectedTab == FindReplaceTab)
					{
						int newCol;
						var method = MakeReplaceWithMethod(out newCol);
						if (method == null)
						{
							return;
						}
						method.Doit(ItemsToChange(true), state);
						FixReplacedItems(method);
					}
					else if (m_operationsTabControl.SelectedTab == BulkCopyTab)
					{
						int newCol;
						var method = MakeBulkCopyMethod(out newCol);
						if (method == null)
						{
							return;
						}
						method.Doit(ItemsToChange(true), state);
						FixReplacedItems(method);
					}
					else if (m_operationsTabControl.SelectedTab == TransduceTab)
					{
						int newCol;
						var method = MakeTransduceMethod(out newCol);
						if (method == null)
						{
							return;
						}
						method.Doit(ItemsToChange(true), state);
						FixReplacedItems(method);
					}
					else if (m_operationsTabControl.SelectedTab == m_deleteTab)
					{
						if (DeleteRowsItemSelected)
						{
							DeleteSelectedObjects(state); // delete rows
						}
						else if (DeleteWhatCombo.SelectedItem is TargetFieldItem)
						{
							var item = DeleteWhatCombo.SelectedItem as TargetFieldItem;
							var index = item.ColumnIndex;
							var bei = m_beItems[index];
							bei.BulkEditControl.SetClearField();
							bei.BulkEditControl.DoIt(ItemsToChange(true), state);
						}
						else if (DeleteWhatCombo.SelectedItem is FieldComboItem)
						{
							int newCol;
							var method = MakeClearMethod(out newCol);
							if (method == null)
							{
								return;
							}
							method.Doit(ItemsToChange(true), state);
							FixReplacedItems(method);
						}

					}
					else
					{
						MessageBox.Show(this, XMLViewsStrings.ksSorryNoEdit, XMLViewsStrings.ksUnimplFeature);
					}
					// Turn off the preview (if any).
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
			var gro = doItObject as IGetReplacedObjects;
			var replacedObjects = gro?.ReplacedObjects;
			if (replacedObjects != null && replacedObjects.Count != 0)
			{
				m_bv.FixReplacedItems(replacedObjects);
			}
		}

		/// <summary>
		/// Delete ALL the checked objects!!
		/// </summary>
		private void DeleteSelectedObjects(ProgressState state)
		{
			var idsToDelete = new HashSet<int>();
			UpdateCurrentGhostParentHelper(); // needed for code below.
			foreach (var hvo in ItemsToChange(true))
			{
				if (m_bv.SpecialCache.get_IntProp(hvo, XMLViewsDataCache.ktagItemEnabled) == 0)
				{
					continue;
				}
				if (!VerifyRowDeleteAllowable(hvo))
				{
					continue;
				}
				//allow deletion for the class we expect to be bulk editing.
				var hvoToDelete = 0;
				if (DomainObjectServices.IsSameOrSubclassOf(m_cache.DomainDataByFlid.MetaDataCache, m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo).ClassID, m_expectedListItemsClassId))
				{
					hvoToDelete = hvo;
				}
				else if (m_ghostParentHelper != null)
				{
					hvoToDelete = m_ghostParentHelper.GetOwnerOfTargetProperty(hvo);
				}
				if (hvoToDelete != 0)
				{
					idsToDelete.Add(hvoToDelete);
				}
			}
			bool fUndo;
			if (!CheckMultiDeleteConditionsAndReport(idsToDelete, out fUndo))
			{
				return;
			}
			try
			{
				state.PercentDone = 10;
				state.Breath();
				m_bv.SetListModificationInProgress(true);
				var interval = Math.Min(100, Math.Max(idsToDelete.Count / 90, 1));
				var i = 0;
				UndoableUnitOfWorkHelper.Do(XMLViewsStrings.ksUndoBulkDelete, XMLViewsStrings.ksRedoBulkDelete, m_cache.ActionHandlerAccessor, () =>
				{
					foreach (var hvo in idsToDelete)
					{
						if ((i + 1) % interval == 0)
						{
							state.PercentDone = i * 90 / idsToDelete.Count + 10;
							state.Breath();
						}
						i++;
						ICmObject obj;
						if (m_cache.ServiceLocator.ObjectRepository.TryGetObject(hvo, out obj))
						{
							m_bv.SpecialCache.DeleteObj(hvo);
						}
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
		private bool CheckMultiDeleteConditionsAndReport(HashSet<int> idsToDelete, out bool fUndo)
		{
			var cOrphans = 0;
			fUndo = cOrphans == 0;
			var sMsg = XMLViewsStrings.ksConfirmDeleteMultiMsg;
			var sTitle = XMLViewsStrings.ksConfirmDeleteMulti;
			if (!fUndo)
			{
				sMsg = XMLViewsStrings.ksCannotUndoTooManyDeleted;
			}
			return MessageBox.Show(this, sMsg, sTitle, MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK;
		}

		private bool VerifyRowDeleteAllowable(int hvo)
		{
			if (string.IsNullOrEmpty(m_sBulkDeleteIfZero))
			{
				return true;
			}
			var co = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
			if (m_piBulkDeleteIfZero == null)
			{
				m_piBulkDeleteIfZero = co.GetType().GetProperty(m_sBulkDeleteIfZero);
			}
			if (m_piBulkDeleteIfZero != null)
			{
				var o = m_piBulkDeleteIfZero.GetValue(co, null);
				if (o.GetType() == typeof(int) && (int)o != 0)
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Gets an image of the blue arrow we use to separate actual cell contents from
		/// bulk edit preview.
		/// </summary>
		public Image PreviewArrow => m_imageList16x14.Images[0];

		/// <summary>
		/// Gets the preview arrow static.
		/// </summary>
		/// <value>The preview arrow static.</value>
		public static Image PreviewArrowStatic
		{
			get
			{
				using (var list = new ImageList())
				{
					var resources = new ComponentResourceManager(typeof(BulkEditBar));
					list.ImageStream = (ImageListStreamer)(resources.GetObject("m_imageList16x14.ImageStream"));
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
			ClickCopySepBox.SelectAll();
		}

		internal void InitFindReplaceTab()
		{
			InitStringCombo(FindReplaceTargetCombo, false);
			EnablePreviewApplyForFindReplace();
		}

		// Initialize the delete tab.
		internal void InitDeleteTab()
		{
			DeleteWhatCombo.SuspendLayout();
			DeleteWhatCombo.ClearItems();
			// Add the string related fields
			AddStringFieldItemsToCombo(DeleteWhatCombo, null, false);
			// Add support for deleting "rows" only for the classes
			// that have associated columns installed (LT-9128).
			var targetClassesNeeded = new HashSet<int>();
			// Always allow deleting the primary row (e.g. Entries)
			targetClassesNeeded.Add(m_bulkEditListItemsClasses[0]);
			// Go through each of the column-deletable string fields, and add rows to delete.
			foreach (FieldComboItem fci in DeleteWhatCombo.Items)
			{
				var targetClass = GetExpectedListItemsClassFromSelectedItem(fci);
				targetClassesNeeded.Add(targetClass);
			}
			// will increment at start of loop.
			var icol = -1;
			// Add all the List related fields
			foreach (var bei in m_beItems)
			{
				icol++;
				if (bei == null || !bei.BulkEditControl.CanClearField)
				{
					continue;
				}
				var colSpec = m_bv.ColumnSpecs[icol];
				var label = GetColumnLabel(colSpec);
				TargetFieldItem tfi = null;
				try
				{
					tfi = new TargetFieldItem(label, icol);
					// still want to allow deleting item rows, even if column is not deletable.
					var targetClass = GetExpectedListItemsClassFromSelectedItem(tfi);
					targetClassesNeeded.Add(targetClass);
					var allowBulkDelete = XmlUtils.GetOptionalBooleanAttributeValue(colSpec, "bulkDelete", true);
					if (!allowBulkDelete)
					{
						continue;
					}
					DeleteWhatCombo.Items.Add(tfi);
					tfi = null; // well be disposed by m_deleteWhatCombo
				}
				finally
				{
					tfi?.Dispose();
				}
			}
			foreach (var rootClassOption in ListItemsClassesInfo(targetClassesNeeded))
			{
				DeleteWhatCombo.Items.Add(rootClassOption);
			}

			// Default to deleting rows if that's all we have in the combo box list.
			DeleteWhatCombo.ResumeLayout();
			var enabled = m_deleteTab.Enabled;
			m_ApplyButton.Enabled = enabled;
		}

		private void EnablePreviewApplyForFindReplace()
		{
			var enabled = CanFindReplace();
			m_ApplyButton.Enabled = enabled;
			m_previewButton.Enabled = enabled;
		}

		private bool CanFindReplace()
		{
			if (!FindReplaceTab.Enabled)
			{
				return false;
			}
			if (Pattern == null)
			{
				return false;
			}
			// If matching writing systems it doesn't matter if find and replace are both empty.
			// That means match anything in the writing system.
			if (Pattern.MatchOldWritingSystem)
			{
				return true;
			}
			// Otherwise we can do it unless both are empty.
			var tssPattern = Pattern.Pattern;
			return tssPattern != null && tssPattern.Length != 0 || TssReplace != null && TssReplace.Length != 0;
		}

		internal void InitBulkCopyTab()
		{
			InitStringCombo(BulkCopySourceCombo, true);
			InitStringCombo(BulkCopyTargetCombo, false);
			EnablePreviewApplyForBulkCopy();
		}

		/// <summary>
		/// Set the enable state for the specified tab, based on the number of possible target columns
		/// (if zero, disable) and the setting specified in the enableBulkEditTabsNode in the specified attribute.
		/// Enhance JohnT: possibly there is no reason for the enableBulkEditTabsNode now that we're figuring
		/// out whether to enable based on the number of target columns?
		/// </summary>
		private void SetTabEnabled(TabPage page, int targetColumnCount, string enableAttrName)
		{
			page.Enabled = (m_enableBulkEditTabsNode == null || XmlUtils.GetOptionalBooleanAttributeValue(m_enableBulkEditTabsNode, enableAttrName, true)) && targetColumnCount > 0;
		}
		/// <summary>
		/// Initialize the List Choice tab.
		/// </summary>
		internal void InitListChoiceTab()
		{
			populateListChoiceTargetCombo();
			SetEnabledForListChoiceTab();
		}

		private void SetEnabledForListChoiceTab()
		{
			SetTabEnabled(ListChoiceTab, m_listChoiceTargetCombo.Items.Count, "enableBEListChoice");
		}

		private void EnablePreviewApplyForBulkCopy()
		{
			var enabled = BulkCopyTab.Enabled;
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
		internal void InitClickCopyTab()
		{
			InitStringCombo(ClickCopyTargetCombo, false);
			m_bv.BrowseView.EditingHelper.ReadOnlyTextCursor = Cursors.Hand;
			var xbv = m_bv.BrowseView as XmlBrowseView;
			// The -= helps make sure we never have more than one. Apparently multiple
			// copies of the same event handler are possible with derived event handlers.
			xbv.ClickCopy -= xbv_ClickCopy;
			xbv.ClickCopy += xbv_ClickCopy;
			m_clickCopyTargetCombo_SelectedIndexChanged(this, new System.EventArgs());
			var enabled = ClickCopyTab.Enabled;
			var selectedItem = ClickCopyTargetCombo.SelectedItem as FieldComboItem;
			if (selectedItem == null)
			{
				// The user has removed all possible targets from the browse view columns.  Choose
				// a plausible writing system code for the text box, and disable the preview and
				// apply buttons since there's nothing for them to work with.  (See LT-5129.)
				ClickCopySepBox.WritingSystemCode = m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle;
				enabled = false;
			}
			else
			{
				ClickCopySepBox.WritingSystemCode = selectedItem.Accessor.WritingSystem;
				// Looks like a no-op, but will cause it to be in the correct writing system.
				ClickCopySepBox.Text = ClickCopySepBox.Text;
			}
			// Note: Don't access BrowseView.SelectedObject here,
			// since it may be invalid/out-of-range (thus crash),
			// pending OnRecordNavigation changes from ReloadList
			// triggered by the m_clickCopyTargetCombo_SelectedIndexChanged().
			m_ApplyButton.Enabled = enabled;
			m_previewButton.Enabled = enabled;
		}

		/// <summary>
		/// Initialize the transduce tab.
		/// </summary>
		internal void InitTransduce()
		{
			InitConverterCombo();
			// Load the source and destination combos.
			InitStringCombo(TransduceSourceCombo, true);
			InitStringCombo(TransduceTargetCombo, false);
			EnablePreviewApplyForTransduce();
		}

		private void EnablePreviewApplyForTransduce()
		{
			var enabled = TransduceTab.Enabled;
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
				var selectedItem = TransduceProcessorCombo.SelectedItem as string;
				var encConverters = new EncConverters();
				TransduceProcessorCombo.ClearItems();
				foreach (string convName in encConverters.Keys)
				{
					var conv = encConverters[convName];
					// Only Unicode-to-Unicode converters are relevant.
					if (conv.ConversionType == ECInterfaces.ConvType.Unicode_to_Unicode || conv.ConversionType == ECInterfaces.ConvType.Unicode_to_from_Unicode)
					{
						TransduceProcessorCombo.Items.Add(convName);
					}
				}
				if (!string.IsNullOrEmpty(selectedItem))
				{
					TransduceProcessorCombo.SelectedItem = selectedItem; // preserve selection if possible
				}
				else if (TransduceProcessorCombo.Items.Count > 0)
				{
					TransduceProcessorCombo.SelectedIndex = 0;
				}
			}
			catch (Exception e)
			{
				MessageBox.Show(string.Format(XMLViewsStrings.ksCannotAccessEC, e.Message));
			}
		}

		// The currently active bulk edit item (if any).
		internal BulkEditItem CurrentItem
		{
			get
			{
				if (m_beItems == null)
				{
					return null;
				}
				if (m_itemIndex < 0 || m_itemIndex >= m_beItems.Length)
				{
					return null;
				}
				return m_beItems[m_itemIndex];
			}
		}

		/// <summary>
		/// Enables the preview apply for list choice.
		/// </summary>
		internal void EnablePreviewApplyForListChoice()
		{
			if (m_beItems == null || m_itemIndex < 0 || m_itemIndex >= m_beItems.Length || m_listChoiceControl == null)
			{
				// Things haven't been initialized enough yet.  Disable the buttons and quit.
				m_ApplyButton.Enabled = false;
				m_previewButton.Enabled = false;
				return;
			}
			// The assumption is that this method is only called when switching to the List Choice tab
			// therefore if the selected item in m_fieldCombo is ComplexListChooserBEditControl
			// we need to disable these buttons when in this state.
			if (m_beItems[m_itemIndex] != null && m_beItems[m_itemIndex].BulkEditControl is ComplexListChooserBEditControl)
			{
				if ((m_beItems[m_itemIndex].BulkEditControl as ComplexListChooserBEditControl).ChosenObjects.Any())
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
			if (!m_listChoiceControl.Text.Equals(string.Empty))
			{
				m_ApplyButton.Enabled = true;
				m_previewButton.Enabled = true;
				return;
			}
			m_ApplyButton.Enabled = false;
			m_previewButton.Enabled = false;
		}

		private void m_operationsTabControl_SelectedIndexChanged(object sender, EventArgs e)
		{
			OperationLabel.Text = s_labels[m_operationsTabControl.SelectedIndex];
			BulkEditTabPageSettings.InitializeSelectedTab(this);
			var fReconstucted = false;
			var fWasShowEnabled = m_bv.BrowseView.Vc.ShowEnabled;
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
			if (!fReconstucted && fWasShowEnabled != m_bv.BrowseView.Vc.ShowEnabled && m_bv.BrowseView.RootBox != null)
			{
				m_bv.BrowseView.RootBox.Reconstruct();
			}
			var selectedTab = m_operationsTabControl.SelectedTab;
			selectedTab.Controls.Add(BulkEditOperationLabel);
			selectedTab.Controls.Add(OperationLabel);
			if (m_bulkEditIcon != null)
			{
				selectedTab.Controls.Add(m_bulkEditIcon);
			}
			if (m_bulkEditIconButton != null)
			{
				selectedTab.Controls.Add(m_bulkEditIconButton);
			}
		}

		/// <summary />
		protected internal virtual void SaveSettings()
		{
			// ClickCopy.ClickCopyTabPageSettings/SaveSettings()/CommitClickChanges()
			// could possibly change m_hvoSelected when we're not ready, so save current.
			// see comment on LT-4768 in UpdateColumnList.
			var oldSelected = m_hvoSelected;
			BulkEditTabPageSettings.CaptureSettingsForCurrentTab(this);
			m_hvoSelected = oldSelected;
		}

		private void m_findReplaceSetupButton_Click(object sender, System.EventArgs e)
		{
			// Ensure that the find and replace strings have the correct writing system.
			var ws = -50;
			try
			{
				// Find the writing system for the selected column (see LT-5491).
				var fci = FindReplaceTargetCombo.SelectedItem as FieldComboItem;
				if (fci == null)
				{
					MessageBox.Show(XMLViewsStrings.ksChooseEditTarget);
					return;
				}
				var xnField = m_bv.ColumnSpecs[fci.ColumnIndex];
				var sWs = XmlViewsUtils.FindWsParam(xnField);
				if (string.IsNullOrEmpty(sWs))
				{
					// It's likely a custom field with a ws selector in the field metadata.
					var sTransduce = XmlUtils.GetOptionalAttributeValue(xnField, "transduce");
					if (!string.IsNullOrEmpty(sTransduce))
					{
						var parts = sTransduce.Split('.');
						if (parts.Length == 2)
						{
							var className = parts[0];
							var fieldName = parts[1];
							var mdc = m_cache.DomainDataByFlid.MetaDataCache;
							try
							{
								var clid = mdc.GetClassId(className);
								var flid = mdc.GetFieldId2(clid, fieldName, true);
								ws = FieldReadWriter.GetWsFromMetaData(0, flid, m_cache);
								if (ws == 0)
								{
									ws = m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle;
								}
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
			{
				ws = m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle;
			}
			if (TssReplace == null)
			{
				TssReplace = TsStringUtils.EmptyString(ws);
			}
			else
			{
				// If we have a replacement TsString, but no pattern, keep the text but
				// no properties.
				if (Pattern == null)
				{
					TssReplace = TsStringUtils.MakeString(TssReplace.Text, ws);
				}
				else if (!Pattern.MatchOldWritingSystem)
				{
					// We have both a string and a pattern. We want to clear writing system information
					// on the string unless we are matching on WS. But we don't want to clear any style info.
					var bldr = TssReplace.GetBldr();
					bldr.SetIntPropValues(0, bldr.Length, (int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, ws);
					TssReplace = bldr.GetString();
				}
			}
			if (Pattern != null)
			{
				if (Pattern.Pattern == null)
				{
					Pattern.Pattern = TsStringUtils.EmptyString(ws);
				}
				else if (!Pattern.MatchOldWritingSystem)
				{
					// Enforce the expected writing system; but don't clear styles.
					var bldr = Pattern.Pattern.GetBldr();
					bldr.SetIntPropValues(0, bldr.Length, (int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, ws);
					Pattern.Pattern = bldr.GetString();
				}
			}
			using (var findDlg = new FwFindReplaceDlg())
			{
				//Change the Title from "Find and Replace" to "Bulk Replace Setup"
				findDlg.Text = string.Format(XMLViewsStrings.khtpBulkReplaceTitle);
				var app = PropertyTable.GetValue<IApp>(LanguageExplorerConstants.App);
				findDlg.SetDialogValues(m_cache, Pattern, m_bv.BrowseView.StyleSheet, FindForm(), PropertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider), app);
				findDlg.RestoreAndPersistSettingsIn(PropertyTable);
				// Set this AFTER it has the correct WSF!
				findDlg.ReplaceText = TssReplace;
				if (findDlg.ShowDialog(this) == DialogResult.OK)
				{
					TssReplace = findDlg.ResultReplaceText;
					UpdateFindReplaceSummary();
					EnablePreviewApplyForFindReplace();
				}
			}
		}

		private static string GetString(ITsString tss)
		{
			return tss == null ? string.Empty : tss.Text;
		}

		private static System.Text.RegularExpressions.Regex s_regexFormatItem = new System.Text.RegularExpressions.Regex("{[0-9]+}");
		private void UpdateFindReplaceSummary()
		{
			if (Pattern?.Pattern == null || Pattern.Pattern.Length <= 0)
			{
				return;
			}
			if (m_findReplaceSummaryLabel.StyleSheet == null)
			{
				return;
			}
			if (m_findReplaceSummaryLabel.WritingSystemFactory == null)
			{
				return;
			}
			m_findReplaceSummaryLabel.BackColor = SystemColors.Control;
			var wsArgs = TsStringUtils.GetWsAtOffset(TssReplace, 0);
			var bldr = TsStringUtils.MakeIncStrBldr();
			bldr.SetIntPropValues((int)FwTextPropType.ktptFontSize, (int)FwTextPropVar.ktpvMilliPoint, 16000);
			bldr.SetIntPropValues((int)FwTextPropType.ktptBold, (int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
			// Simulate string.Format(XMLViewsStrings.ksReplaceXWithY, <pattern>, <replace>) to build a TsString that
			// properly displays everything.
			foreach (var piece in ExtractFormattingPieces(XMLViewsStrings.ksReplaceXWithY))
			{
				if (s_regexFormatItem.IsMatch(piece))
				{
					bldr.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, wsArgs);
					bldr.Append(piece == "{0}" ? GetString(Pattern.Pattern) : TssReplace.Text);
				}
				else
				{
					bldr.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, m_cache.DefaultUserWs);
					bldr.Append(piece);
				}
			}
			m_findReplaceSummaryLabel.Tss = bldr.GetString();
		}

		/// <summary>
		/// Split the string into pieces separated by the embedded "format items".  The input string is
		/// a format specifier like those used by string.Format().  Since TsString doesn't have a compatible
		/// Format method, and since the provided arguments that match the "format items" may need a different
		/// writing system than the literal characters in the string, we return a list of pieces of the input
		/// string.  These pieces include all of the characters of the original string in order, including a
		/// separate piece for each "format item".  For example, consider the input "This {0} is {1}!"  The
		/// method would break this into five pieces: "This ", "{0}", " is ", "{1}", and "!".  The caller
		/// is expected to know what to do with each piece to put together the desired TsString.
		/// </summary>
		/// <remarks>
		/// TODO: think about whether this might be a useful utility method of general import, and if so move
		/// it (and the static Regex variable) to the appropriate utility class.
		/// </remarks>
		private static List<string> ExtractFormattingPieces(string fmt)
		{
			var pieces = new List<string>();
			var idx = 0;
			foreach (System.Text.RegularExpressions.Match match in s_regexFormatItem.Matches(fmt))
			{
				if (match.Index > idx)
				{
					pieces.Add(fmt.Substring(idx, match.Index - idx));
				}
				pieces.Add(fmt.Substring(match.Index, match.Length));
				idx = match.Index + match.Length;
			}
			if (idx < fmt.Length)
			{
				pieces.Add(fmt.Substring(idx));
			}
			return pieces;
		}

		/// <summary>
		/// Initialize a combo box with the columns that can be sources or targets for string manipulations.
		/// If possible select an item with the same name.
		/// If not select the first item if any.
		/// If none set the Text to 'no choices'.
		/// </summary>
		protected void InitStringCombo(FwOverrideComboBox combo, bool fIsSourceCombo)
		{
			var selectedItem = combo.SelectedItem as FieldComboItem;
			combo.ClearItems();
			AddStringFieldItemsToCombo(combo, selectedItem, fIsSourceCombo);
			if (combo.Items.Count == 0)
			{
				combo.Text = XMLViewsStrings.ksNoChoices;
			}
			// NOTE: Don't re-set the item here, let BulkEditBarTabSettings do it.
		}

		private void AddStringFieldItemsToCombo(ComboBox combo, FieldComboItem selectedItem, bool fIsSourceCombo)
		{
			var icol = -1;
			foreach (var node in m_bv.ColumnSpecs)
			{
				icol++;
				FieldReadWriter accessor = null;
				var optionLabel = GetColumnLabel(node);
				try
				{
					if (fIsSourceCombo)
					{
						accessor = new ManyOnePathSortItemReadWriter(m_cache, node, m_bv, PropertyTable.GetValue<IApp>(LanguageExplorerConstants.App));
					}
					else if (!IsColumnWsBothVernacularAndAnalysis(node))
					{
						accessor = FieldReadWriter.Create(node, m_cache, m_bv.RootObjectHvo);
					}
					if (accessor == null)
					{
						continue;
					}
					// Use the decorated data access - see FWR-376.
					accessor.DataAccess = m_bv.SpecialCache;
				}
				catch
				{
					Debug.Fail(string.Format("There was an error creating Delete combo item for column ({0})", selectedItem.ColumnIndex), optionLabel);
					// skip buggy column
					continue;
				}
				var item = new FieldComboItem(optionLabel, icol, accessor);
				combo.Items.Add(item);
				if (selectedItem != null && selectedItem.ToString() == item.ToString())
				{
					var newSelection = item;
				}
			}
		}

		/// <summary/>
		/// <returns>true if the ws attribute for the column indicates both vernacular and analysis writing systems, false otherwise</returns>
		private static bool IsColumnWsBothVernacularAndAnalysis(XElement node)
		{
			var wsAttributeValue = XmlUtils.GetOptionalAttributeValue(node, "ws", null);
			if (wsAttributeValue == null)
			{
				return false;
			}
			var magicWsId = WritingSystemServices.GetMagicWsIdFromName(wsAttributeValue.Substring("$ws=".Length));
			switch (magicWsId)
			{
				case WritingSystemServices.kwsAnalVerns:
				case WritingSystemServices.kwsFirstAnalOrVern:
				case WritingSystemServices.kwsFirstVernOrAnal:
					return true;
				default:
					return false;
			}
		}

		private void xbv_ClickCopy(object sender, ClickCopyEventArgs e)
		{
			var selectedItem = ClickCopyTargetCombo.SelectedItem as FieldComboItem;
			if (selectedItem == null)
			{
				MessageBox.Show(XMLViewsStrings.ksChooseClickTarget);
				return;
			}
			// Check whether this item is really editable.
			if (!string.IsNullOrEmpty(m_sClickEditIf) && m_wsClickEditIf != 0)
			{
				var co = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(e.Hvo);
				if (m_miClickEditIf == null)
				{
					m_miClickEditIf = co.GetType().GetMethod(m_sClickEditIf);
				}
				if (m_miClickEditIf != null)
				{
					var o = m_miClickEditIf.Invoke(co, new object[] { m_wsClickEditIf });
					if (o.GetType() == typeof(bool))
					{
						var fAllowEdit = m_fClickEditIfNot ? !(bool)o : (bool)o;
						if (!fAllowEdit)
						{
							return;
						}
					}
				}
			}
			var tssOld = selectedItem.Accessor.CurrentValue(e.Hvo);
			var tssNew = e.Word;
			if (ClickCopyReorderButton.Checked)
			{
				tssNew = e.Source;
				if (e.IchStartWord > 0)
				{
					var tsb = tssNew.GetBldr();
					var tsbStart = tssNew.GetBldr();
					tsbStart.Replace(e.IchStartWord, tsbStart.Length, string.Empty, null);
					tsb.Replace(0, e.IchStartWord, string.Empty, null);
					tsb.Replace(tsb.Length, tsb.Length, ", ", null);
					tsb.ReplaceTsString(tsb.Length, tsb.Length, tsbStart.GetString());
					tssNew = tsb.GetString();
				}
			}
			if (tssOld != null && tssOld.Length > 0 && ClickCopyAppendButton.Checked)
			{
				var tsb = tssOld.GetBldr();
				var ich = tsb.Length;
				tsb.ReplaceTsString(ich, ich, ClickCopySepBox.Tss);
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
			try
			{
				var prevEC = TransduceProcessorCombo.Text;
				var app = PropertyTable.GetValue<IApp>(LanguageExplorerConstants.App);
				using (var dlg = new AddCnvtrDlg(PropertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider), app, null, TransduceProcessorCombo.Text, null, true))
				{
					dlg.ShowDialog();
					// Reload the converter list in the combo to reflect the changes.
					InitConverterCombo();
					// Either select the new one or select the old one
					if (dlg.DialogResult == DialogResult.OK && !string.IsNullOrEmpty(dlg.SelectedConverter))
					{
						TransduceProcessorCombo.SelectedItem = dlg.SelectedConverter;
					}
					else if (TransduceProcessorCombo.Items.Count > 0)
					{
						TransduceProcessorCombo.SelectedItem = prevEC; // preserve selection if possible
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(string.Format(XMLViewsStrings.ksCannotAccessEC, ex.Message));
			}
		}

		internal void m_clickCopyTargetCombo_SelectedIndexChanged(object sender, EventArgs e)
		{
			var selectedItem = ClickCopyTargetCombo.SelectedItem as FieldComboItem;
			var column = -1;
			if (selectedItem != null)
			{
				column = selectedItem.ColumnIndex;
				var spec = m_bv.ColumnSpecs[column];
				m_sClickEditIf = XmlUtils.GetOptionalAttributeValue(spec, "editif");
				m_wsClickEditIf = 0;
				if (!string.IsNullOrEmpty(m_sClickEditIf))
				{
					if (m_sClickEditIf[0] == '!')
					{
						m_fClickEditIfNot = true;
						m_sClickEditIf = m_sClickEditIf.Substring(1);
					}
					var sWs = StringServices.GetWsSpecWithoutPrefix(FwUtils.ConvertElement(spec));
					if (sWs != null)
					{
						m_wsClickEditIf = XmlViewsUtils.GetWsFromString(sWs, m_cache);
					}
				}
				OnTargetComboItemChanged(selectedItem);
			}
			SetEditColumn(column);
		}

		/// <summary>
		/// the expected record list's list items class id according to the
		/// selected target combo field. <c>0</c> if one hasn't been determined (yet).
		/// </summary>
		public int ExpectedListItemsClassId
		{
			get
			{
				if (m_expectedListItemsClassId == 0)
				{
					if (CurrentTargetCombo?.SelectedItem != null)
					{
						m_expectedListItemsClassId = GetExpectedListItemsClassFromSelectedItem(CurrentTargetCombo.SelectedItem as FieldComboItem);
					}
				}
				return m_expectedListItemsClassId;
			}
		}

		/// <summary>
		/// Get the list choice tab page
		/// </summary>
		public TabPage ListChoiceTab { get; private set; }

		/// <summary>
		/// Get the bulk edit operation label
		/// </summary>
		public Label BulkEditOperationLabel { get; private set; }

		/// <summary>
		/// Get label3 (the change to field)
		/// </summary>
		public Label ChangeToLabel { get; private set; }

		/// <summary>
		/// Get label2 (the target field)
		/// </summary>
		public Label TargetFieldLabel { get; private set; }

		/// <summary>
		/// Get the operation label
		/// </summary>
		public Label OperationLabel { get; private set; }

		/// <summary>
		/// Get the list choice target combo
		/// </summary>
		public FwOverrideComboBox TargetCombo => m_listChoiceTargetCombo;

		/// <summary>
		/// Get the bulk copy tab page
		/// </summary>
		public TabPage BulkCopyTab { get; private set; }

		/// <summary>
		/// Get the click copy tab page
		/// </summary>
		public TabPage ClickCopyTab { get; private set; }

		/// <summary>
		/// Get the transduce tab page
		/// </summary>
		public TabPage TransduceTab { get; private set; }

		/// <summary>
		/// Get the find/replace tab page
		/// </summary>
		public TabPage FindReplaceTab { get; private set; }

		/// <summary>
		/// Get the delete tab page
		/// </summary>
		public TabPage DeleteTab => m_deleteTab;

		/// <summary>
		/// triggers TargetComboSelectedIndexChanged to tell delegates that TargetComboItem has changed.
		/// </summary>
		public void OnTargetComboSelectedIndexChanged()
		{
			if (CurrentTargetCombo != null)
			{
				OnTargetComboItemChanged(CurrentTargetCombo.SelectedItem as FieldComboItem);
			}
		}

		private void OnTargetComboItemChanged(FieldComboItem selectedItem)
		{
			if (TargetComboSelectedIndexChanged == null)
			{
				return;
			}
			int flid;
			m_expectedListItemsClassId = GetExpectedListItemsClassAndTargetFieldFromSelectedItem(selectedItem, out flid);
			if (m_expectedListItemsClassId != 0)
			{
				using (var targetFieldItem = new TargetFieldItem(selectedItem.ToString(), selectedItem.ColumnIndex, m_expectedListItemsClassId, flid))
				{
					var args = new TargetColumnChangedEventArgs(targetFieldItem);
					//REFACTOR: the BrowseView should not know about BulkEdit - They appear to be too highly coupled.
					m_bv.BulkEditTargetComboSelectedIndexChanged(args); // may set ForceReload flag on args.
					TargetComboSelectedIndexChanged?.Invoke(this, args);
				}
			}
		}

		private int GetExpectedListItemsClassFromSelectedItem(FieldComboItem selectedItem)
		{
			int dummy;
			return GetExpectedListItemsClassAndTargetFieldFromSelectedItem(selectedItem, out dummy);
		}

		private int GetExpectedListItemsClassAndTargetFieldFromSelectedItem(FieldComboItem selectedItem, out int field)
		{
			field = 0; // default
			if (selectedItem == null)
			{
				return 0;
			}
			var listItemsClassId = 0;
			// if we're only expecting this bulk edit to be used with one list items class, use that one.
			if (m_bulkEditListItemsClasses.Count == 1)
			{
				listItemsClassId = m_bulkEditListItemsClasses[0];
			}
			else
			{
				// figure out the class of the expected list items for the corresponding bulk edit item
				// and make that our target class.
				if (selectedItem is TargetFieldItem && ((TargetFieldItem)selectedItem).ExpectedListItemsClass != 0)
				{
					var targetFieldItem = (TargetFieldItem)selectedItem;
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
						var beItem = m_beItems[selectedItem.ColumnIndex];
						field = beItem.BulkEditControl.FieldPath[0];
					}
					listItemsClassId = m_cache.DomainDataByFlid.MetaDataCache.GetOwnClsId(field);
				}
			}
			return listItemsClassId;
		}

		private void SetEditColumn(int icol)
		{
			if (m_bv.BrowseView.Vc.OverrideAllowEditColumn != icol)
			{
				m_bv.BrowseView.Vc.OverrideAllowEditColumn = icol;
				m_bv.BrowseView.RootBox?.Reconstruct();
			}
			if (icol >= 0)
			{
				m_bv.BrowseView.EditingHelper.ReadOnlyTextCursor = Cursors.Hand;
				m_bv.BrowseView.LostFocus -= CommitClickChanges;  // make sure we don't install more than one.
				m_bv.BrowseView.LostFocus += CommitClickChanges;
				m_bv.BrowseView.SelectedIndexChanged -= CommitClickChanges; // don't install more than one
				m_bv.BrowseView.SelectedIndexChanged += CommitClickChanges;
			}
			else
			{
				m_bv.BrowseView.EditingHelper.ReadOnlyTextCursor = null;
				m_bv.BrowseView.LostFocus -= CommitClickChanges;
				m_bv.BrowseView.SelectedIndexChanged -= CommitClickChanges;
			}
		}

		/// <summary>
		/// When we switch rows or lose focus in the main browse view, commit any changes
		/// made in click copy mode.
		/// TODO (DamienD): I don't think this is used anymore, so it can probably be removed.
		/// </summary>
		internal void CommitClickChanges(object sender, EventArgs e)
		{
			// only appropriate in context of clickCopyTab.
			if (m_operationsTabControl.SelectedTab != ClickCopyTab)
			{
				// LT-9033: we can crash if this gets called in the context of
				// a non-clickCopy tab after removing a column that click copy
				// was using as a target field.
				Debug.Fail("CommitClickChanges should only be fired in context of Click Copy tab, not " + m_operationsTabControl.SelectedTab.ToString());
				return;
			}
			var selectedItem = ClickCopyTargetCombo.SelectedItem as FieldComboItem;
			if (selectedItem != null)
			{
				var spec = m_bv.ColumnSpecs[selectedItem.ColumnIndex];
				if (m_hvoSelected != 0)
				{
					CommitChanges(m_hvoSelected, XmlUtils.GetOptionalAttributeValue(spec, "commitChanges"), m_cache, selectedItem.Accessor.WritingSystem);
				}
			}
			m_hvoSelected = m_bv.BrowseView.SelectedObject;
		}

		/// <summary>
		/// Commit changes for the current hvo if it has a commit changes handler specified.
		/// </summary>
		internal static void CommitChanges(int hvo, string commitChanges, LcmCache cache, int ws)
		{
			if (string.IsNullOrEmpty(commitChanges) || cache.ActionHandlerAccessor == null || cache.ActionHandlerAccessor.IsUndoOrRedoInProgress)
			{
				return;
			}
			var cmo = cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
			var mi = cmo.GetType().GetMethod(commitChanges, new[] { typeof(int) });
			if (mi == null)
			{
				throw new FwConfigurationException("Method " + commitChanges + " not found on class " + cmo.GetType().Name);
			}
			mi.Invoke(cmo, new object[] { ws });
		}

		internal void m_bulkCopyTargetCombo_SelectedIndexChanged(object sender, EventArgs e)
		{
			var fci = (FieldComboItem)BulkCopyTargetCombo.SelectedItem;
			if (fci.Accessor.WritingSystem != BcNonEmptyTargetControl.WritingSystemCode)
			{
				BcNonEmptyTargetControl.WritingSystemCode = fci.Accessor.WritingSystem;
				// This causes it to keep the same string but convert to the new WS.
				BcNonEmptyTargetControl.Separator = BcNonEmptyTargetControl.Separator;
			}
			OnTargetComboItemChanged(fci);
		}

		internal void m_transduceTargetCombo_SelectedIndexChanged(object sender, EventArgs e)
		{
			var fci = (FieldComboItem)TransduceTargetCombo.SelectedItem;
			if (fci.Accessor.WritingSystem != TrdNonEmptyTargetControl.WritingSystemCode)
			{
				TrdNonEmptyTargetControl.WritingSystemCode = fci.Accessor.WritingSystem;
				// This causes it to keep the same string but convert to the new WS.
				TrdNonEmptyTargetControl.Separator = TrdNonEmptyTargetControl.Separator;
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
			foreach (var item in m_beItems)
			{
				if (item?.BulkEditControl != null)
				{
					item.BulkEditControl.Stylesheet = value;
				}
			}
			m_findReplaceSummaryLabel.StyleSheet = value;
		}

		internal void m_findReplaceTargetCombo_SelectedIndexChanged(object sender, EventArgs e)
		{
			var selectedItem = FindReplaceTargetCombo.SelectedItem as FieldComboItem;
			OnTargetComboItemChanged(selectedItem);
		}

		#region Implementation of IPropertyTableProvider

		/// <summary>
		/// Placement in the IPropertyTableProvider interface lets FwApp call IPropertyTable.DoStuff.
		/// </summary>
		public IPropertyTable PropertyTable { get; }

		#endregion

		/// <summary />
		public IPublisher Publisher { get; }

		/// <summary />
		public ISubscriber Subscriber { get; }

		/// <summary>
		/// keeps track of a row selection (for DeleteTab)
		/// as opposed to deleting a column.
		/// </summary>
		private sealed class ListClassTargetFieldItem : TargetFieldItem
		{
			internal ListClassTargetFieldItem(string label, int classId)
				: base(label, -1, classId)
			{
			}
		}

		/// <summary>
		/// This is a base class for several ways of faking and actually making a change.
		/// </summary>
		private abstract class DoItMethod : IGetReplacedObjects
		{
			protected FieldReadWriter m_accessor; // typically the destination accessor, sometimes also the source.
			private ISilDataAccess m_sda;
			private LcmCache m_cache;
			private XElement m_nodeSpec; // specification node for the column
			private string m_sEditIf;
			private bool m_fEditIfNot;
			private MethodInfo m_miEditIf;
			private int m_wsEditIf;

			protected DoItMethod(LcmCache cache, ISilDataAccessManaged sda, FieldReadWriter accessor, XElement spec)
			{
				m_cache = cache;
				m_accessor = accessor;
				m_sda = sda;
				m_nodeSpec = spec;
				m_sEditIf = XmlUtils.GetOptionalAttributeValue(spec, "editif");
				if (string.IsNullOrEmpty(m_sEditIf))
				{
					return;
				}
				if (m_sEditIf[0] == '!')
				{
					m_fEditIfNot = true;
					m_sEditIf = m_sEditIf.Substring(1);
				}
				var sWs = StringServices.GetWsSpecWithoutPrefix(XmlUtils.GetOptionalAttributeValue(spec, "ws"));
				if (sWs != null)
				{
					m_wsEditIf = XmlViewsUtils.GetWsFromString(sWs, m_cache);
				}
			}

			/// <summary>
			/// Fake doing the change by setting the specified property to the appropriate value
			/// for each item in the set. Disable items that can't be set.
			/// </summary>
			public void FakeDoit(IEnumerable<int> itemsToChange, int tagMadeUpFieldIdentifier, int tagEnable, ProgressState state)
			{
				var i = 0;
				// Report progress 50 times or every 100 items, whichever is more (but no more than once per item!)
				var interval = Math.Min(100, Math.Max(itemsToChange.Count() / 50, 1));
				foreach (var hvo in itemsToChange)
				{
					i++;
					if (i % interval == 0)
					{
						state.PercentDone = i * 100 / itemsToChange.Count();
						state.Breath();
					}
					var fEnable = OkToChange(hvo);
					if (fEnable)
					{
						m_sda.SetString(hvo, tagMadeUpFieldIdentifier, NewValue(hvo));
					}
					m_sda.SetInt(hvo, tagEnable, (fEnable ? 1 : 0));
				}
			}

			public void Doit(IEnumerable<int> itemsToChange, ProgressState state)
			{
				m_sda.BeginUndoTask(XMLViewsStrings.ksUndoBulkEdit, XMLViewsStrings.ksRedoBulkEdit);
				var commitChanges = XmlUtils.GetOptionalAttributeValue(m_nodeSpec, "commitChanges");
				var i = 0;
				// Report progress 50 times or every 100 items, whichever is more (but no more than once per item!)
				var interval = Math.Min(100, Math.Max(itemsToChange.Count() / 50, 1));
				foreach (var hvo in itemsToChange)
				{
					i++;
					if (i % interval == 0)
					{
						state.PercentDone = i * 100 / itemsToChange.Count();
						state.Breath();
					}
					Doit(hvo);
					CommitChanges(hvo, commitChanges, m_cache, m_accessor.WritingSystem);
				}
				m_sda.EndUndoTask();
			}

			/// <summary>
			/// Make the change. Usually you can just override NewValue and/or OkToChange.
			/// </summary>
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
				if (string.IsNullOrEmpty(m_sEditIf) || m_wsEditIf == 0)
				{
					return true;
				}
				var co = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
				if (m_miEditIf == null)
				{
					m_miEditIf = co.GetType().GetMethod(m_sEditIf);
				}
				if (m_miEditIf == null)
				{
					return true;
				}
				var o = m_miEditIf.Invoke(co, new object[] { m_wsEditIf });
				if (o.GetType() != typeof(bool))
				{
					return true;
				}
				return m_fEditIfNot ? !(bool)o : (bool)o;
			}

			protected abstract ITsString NewValue(int hvo);

			#region IGetReplacedObjects Members

			public Dictionary<int, int> ReplacedObjects { get; } = new Dictionary<int, int>();

			#endregion
		}

		/// <summary>
		/// Implement (Fake)DoIt by replacing the current value with an empty string.
		/// </summary>
		private sealed class ClearMethod : DoItMethod
		{
			private readonly ITsString m_newValue;

			internal ClearMethod(LcmCache cache, ISilDataAccessManaged sda, FieldReadWriter accessor, XElement spec)
				: base(cache, sda, accessor, spec)
			{
				m_newValue = TsStringUtils.EmptyString(accessor.WritingSystem);
			}

			/// <summary>
			/// We can do a replace if the current value is not empty.
			/// </summary>
			protected override bool OkToChange(int hvo)
			{
				if (!base.OkToChange(hvo))
				{
					return false;
				}
				var tss = OldValue(hvo);
				return (tss != null && tss.Length != 0);
			}

			/// <summary>
			/// Actually produce the replacement string.
			/// </summary>
			protected override ITsString NewValue(int hvo)
			{
				return m_newValue;
			}
		}

		/// <summary>
		/// Implement FakeDoIt by replacing whatever matches the pattern with the result.
		/// </summary>
		private sealed class ReplaceWithMethod : DoItMethod
		{
			private IVwPattern m_pattern;
			private ITsString m_replacement;
			private IVwTxtSrcInit m_textSourceInit;
			private IVwTextSource m_ts;

			internal ReplaceWithMethod(LcmCache cache, ISilDataAccessManaged sda, FieldReadWriter accessor, XElement spec, IVwPattern pattern, ITsString replacement)
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
			protected override bool OkToChange(int hvo)
			{
				if (!base.OkToChange(hvo))
				{
					return false;
				}
				var tss = OldValue(hvo) ?? TsStringUtils.EmptyString(m_accessor.WritingSystem);
				m_textSourceInit.SetString(tss);
				int ichMin, ichLim;
				m_pattern.FindIn(m_ts, 0, tss.Length, true, out ichMin, out ichLim, null);
				return ichMin >= 0;
			}

			/// <summary>
			/// Actually produce the replacement string.
			/// </summary>
			protected override ITsString NewValue(int hvo)
			{
				var tss = OldValue(hvo) ?? TsStringUtils.EmptyString(m_accessor.WritingSystem);
				m_textSourceInit.SetString(tss);
				var ichStartSearch = 0;
				ITsStrBldr tsb = null;
				var delta = 0; // Amount added to length of string (negative if shorter).
				var cch = tss.Length;
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
				var ichLimLastMatch = -1;
				for (; ichStartSearch <= cch;)
				{
					int ichMin, ichLim;
					m_pattern.FindIn(m_ts, ichStartSearch, cch, true, out ichMin, out ichLim, null);
					if (ichMin < 0)
					{
						break;
					}
					if (ichLim == ichLimLastMatch)
					{
						ichStartSearch = ichLim + 1;
						continue;
					}
					ichLimLastMatch = ichLim;
					if (tsb == null)
					{
						tsb = tss.GetBldr();
					}
					var tssRep = m_pattern.ReplacementText;
					tsb.ReplaceTsString(ichMin + delta, ichLim + delta, tssRep);
					delta += tssRep.Length - (ichLim - ichMin);
					ichStartSearch = ichLim;
				}
				return tsb?.GetString().get_NormalizedForm(FwNormalizationMode.knmNFD);
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
				{
					return;
				}
				var tss = NewValue(hvo);
				if (tss != null)
				{
					SetNewValue(hvo, tss);
				}
			}
		}

		/// <summary>
		/// Handle BulkEditBar's "Transduce" tab action to either make a real change, or give a preview of a change.
		/// </summary>
		private sealed class TransduceMethod : DoItMethod
		{
			private FieldReadWriter m_srcAccessor;
			private IEncConverter m_converter;
			private bool m_fFailed;
			private ITsString m_tssSep;
			private NonEmptyTargetOptions m_options;

			internal TransduceMethod(LcmCache cache, ISilDataAccessManaged sda, FieldReadWriter dstAccessor, XElement spec, FieldReadWriter srcAccessor, IEncConverter converter, ITsString tssSep, NonEmptyTargetOptions options)
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
			protected override bool OkToChange(int hvo)
			{
				if (!base.OkToChange(hvo))
				{
					return false;
				}
				if (m_options == NonEmptyTargetOptions.DoNothing)
				{
					var tssOld = OldValue(hvo);
					if (tssOld != null && tssOld.Length != 0)
					{
						return false;
					}
				}
				var tssSrc = m_srcAccessor.CurrentValue(hvo);
				return tssSrc != null && !m_srcAccessor.CurrentValue(hvo).Equals(NewValue(hvo));
			}


			protected override ITsString NewValue(int hvo)
			{
				var tssSrc = m_srcAccessor.CurrentValue(hvo) ?? TsStringUtils.EmptyString(m_accessor.WritingSystem);
				if (m_fFailed) // once we've had a failure don't try any more this pass.
				{
					return tssSrc;
				}
				var old = tssSrc.Text;
				var converted = string.Empty;
				if (!string.IsNullOrEmpty(old))
				{
					try
					{
						converted = m_converter.Convert(old);
					}
					catch (Exception except)
					{
						MessageBox.Show(string.Format(XMLViewsStrings.ksErrorProcessing, old, except.Message));
						m_fFailed = true;
						return tssSrc;
					}
				}
				var tssNew = TsStringUtils.MakeString(converted, m_accessor.WritingSystem);
				if (m_options != NonEmptyTargetOptions.Append)
				{
					return tssNew;
				}
				var tssOld = OldValue(hvo);
				if (tssOld == null || tssOld.Length == 0)
				{
					return tssNew;
				}
				var bldr = tssOld.GetBldr();
				bldr.ReplaceTsString(bldr.Length, bldr.Length, m_tssSep);
				bldr.ReplaceTsString(bldr.Length, bldr.Length, tssNew);
				return bldr.GetString();
			}
		}

		private sealed class BulkCopyMethod : DoItMethod
		{
			private FieldReadWriter m_srcAccessor;
			private ITsString m_tssSep;
			private NonEmptyTargetOptions m_options;

			public BulkCopyMethod(LcmCache cache, ISilDataAccessManaged sda, FieldReadWriter dstAccessor, XElement spec, FieldReadWriter srcAccessor, ITsString tssSep, NonEmptyTargetOptions options)
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
			protected override bool OkToChange(int hvo)
			{
				if (!base.OkToChange(hvo))
				{
					return false;
				}
				var tssOld = OldValue(hvo);
				if (m_options == NonEmptyTargetOptions.DoNothing && tssOld != null && tssOld.Length != 0)
				{
					// Don't want to modify existing data.
					return false;
				}
				string sOld = null;
				if (tssOld != null)
				{
					sOld = tssOld.Text;
				}
				var tssNew = NewValue(hvo);
				string sNew = null;
				if (tssNew != null)
				{
					sNew = tssNew.Text;
				}
				if (string.IsNullOrEmpty(sOld) && string.IsNullOrEmpty(sNew))
				{
					// They're really the same, regardless of properties.
					return false;
				}
				if (sOld != sNew)
				{
					return true;
				}
				return !tssNew.Equals(tssOld);
			}


			protected override ITsString NewValue(int hvo)
			{
				var tssNew = m_srcAccessor.CurrentValue(hvo) ?? TsStringUtils.EmptyString(m_accessor.WritingSystem);
				if (m_options != NonEmptyTargetOptions.Append)
				{
					return tssNew;
				}
				var tssOld = OldValue(hvo);
				if (tssOld == null || tssOld.Length == 0)
				{
					return tssNew;
				}
				var bldr = tssOld.GetBldr();
				bldr.ReplaceTsString(bldr.Length, bldr.Length, m_tssSep);
				bldr.ReplaceTsString(bldr.Length, bldr.Length, tssNew);
				return bldr.GetString();
			}
		}

		private class IntChooserBEditControl : BulkEditSpecControl, IGetReplacedObjects
		{
			#region IBulkEditSpecControl Members
			protected int m_flid;
			protected ComboBox m_combo;

			/// <summary>
			/// Initialized with a string like "0:no;1:yes".
			/// </summary>
			public IntChooserBEditControl(string itemList, int flid)
			{
				m_flid = flid;
				m_combo = new ComboBox
				{
					DropDownStyle = ComboBoxStyle.DropDownList
				};
				foreach (var pair in itemList.Split(';'))
				{
					var vals = pair.Trim().Split(':');
					if (vals.Length != 2)
					{
						throw new Exception("IntChooserBEditControl values must look like n:name");
					}
					var val = int.Parse(vals[0]);
					m_combo.Items.Add(new IntComboItem(vals[1].Trim(), val));
				}
				if (m_combo.Items.Count == 0)
				{
					throw new Exception("IntChooserBEditControl created with zero items");
				}
				m_combo.SelectedIndex = 0;
				m_combo.SelectedIndexChanged += m_combo_SelectedIndexChanged;
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
				m_combo = new ComboBox
				{
					DropDownStyle = ComboBoxStyle.DropDownList
				};
				var index = 0;
				foreach (var name in itemList)
				{
					m_combo.Items.Add(new IntComboItem(name, index++));
				}
				if (m_combo.Items.Count == 0)
				{
					throw new Exception("IntChooserBEditControl created with zero items");
				}
				m_combo.SelectedIndex = Math.Max(0, Math.Min(m_combo.Items.Count - 1, initialIndexToSelect));
				m_combo.SelectedIndexChanged += m_combo_SelectedIndexChanged;
			}

			private void m_combo_SelectedIndexChanged(object sender, EventArgs e)
			{
				// Tell the parent control that we may have changed the selected item so it can
				// enable or disable the Apply and Preview buttons based on the selection.
				var index = m_combo.SelectedIndex;
				// we need a dummy hvo value to pass since IntChooserBEditControl
				var hvo = 0;
				//displays 'yes' or 'no' which have no hvo
				OnValueChanged(sender, new FwObjectSelectionEventArgs(hvo, index));
			}

			public override Control Control => m_combo;

			public override void DoIt(IEnumerable<int> itemsToChange, ProgressState state)
			{
				var itemsToChangeAsList = new List<int>(itemsToChange);
				Cache.DomainDataByFlid.BeginUndoTask(XMLViewsStrings.ksUndoBulkEdit, XMLViewsStrings.ksRedoBulkEdit);
				var sda = Cache.DomainDataByFlid;
				ReplacedObjects.Clear();
				var val = (m_combo.SelectedItem as IntComboItem).Value;
				var i = 0;
				// Report progress 50 times or every 100 items, whichever is more (but no more than once per item!)
				var interval = Math.Min(100, Math.Max(itemsToChangeAsList.Count / 50, 1));
				var mdcManaged = Cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();
				foreach (var hvoItem in itemsToChangeAsList)
				{
					i++;
					if (i % interval == 0)
					{
						state.PercentDone = i * 100 / itemsToChangeAsList.Count;
						state.Breath();
					}
					// If the field is on an owned object that might not exist, we don't want to create
					// that owned object just because we're changing the values involved.
					// (See FWR-3199 for an example of such a situation.)
					var clid = m_sda.get_IntProp(hvoItem, CmObjectTags.kflidClass);
					var flids = mdcManaged.GetFields(clid, true, (int)CellarPropertyTypeFilter.All);
					if (!flids.Contains(m_flid))
					{
						continue;
					}
					int valOld;
					if (TryGetOriginalListValue(sda, hvoItem, out valOld) && valOld == val)
					{
						continue;
					}
					UpdateListItemToNewValue(sda, hvoItem, val, valOld);
				}
				// Enhance JohnT: maybe eventually we will want to make a more general mechanism for doing this,
				// e.g., specify a method to call using the XML configuration for the column?
				// Note that resetting them all at the end of the edit is much more efficient than
				// adding an override for each word, which rewrites the exceptions file each time.
				// (This code runs before the code at the end of the UOW which also tries to update the spelling
				// status. That code will find it is already correct and not write the exc file.)
				if (m_flid == WfiWordformTags.kflidSpellingStatus)
				{
					WfiWordformServices.ConformOneSpellingDictToWordforms(Cache.DefaultVernWs, Cache);
				}
				Cache.DomainDataByFlid.EndUndoTask();
			}

			protected override void UpdateListItemToNewValue(ISilDataAccess sda, int hvoItem, int newVal, int oldVal)
			{
				var hvoOwningInt = hvoItem;
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
				var type = m_sda.MetaDataCache.GetFieldType(m_flid);
				if (type == (int)CellarPropertyType.Boolean)
				{
					sda.SetBoolean(hvoOwner, m_flid, newVal != 0);
				}
				else
				{
					sda.SetInt(hvoOwner, m_flid, newVal);
				}
			}

			private void FixSpellingStatus(int hvoItem, int val)
			{
				var defVernWS = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle;
				var tss = Cache.DomainDataByFlid.get_MultiStringAlt(hvoItem, WfiWordformTags.kflidForm, defVernWS);
				if (tss == null || tss.Length == 0)
				{
					return; // probably can't happen?
				}
				SpellingHelper.SetSpellingStatus(tss.Text, defVernWS, Cache.WritingSystemFactory, (val == (int)SpellingStatusStates.correct));
			}

			public override void FakeDoit(IEnumerable<int> itemsToChange, int tagMadeUpFieldIdentifier, int tagEnabled, ProgressState state)
			{
				var itemsToChangeAsList = new List<int>(itemsToChange);
				var val = ((IntComboItem)m_combo.SelectedItem).Value;
				var tssVal = TsStringUtils.MakeString(m_combo.SelectedItem.ToString(), Cache.ServiceLocator.WritingSystemManager.UserWs);
				var i = 0;
				// Report progress 50 times or every 100 items, whichever is more
				// (but no more than once per item!)
				var interval = Math.Min(100, Math.Max(itemsToChangeAsList.Count / 50, 1));
				var mdcManaged = Cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();
				var type = m_sda.MetaDataCache.GetFieldType(m_flid);
				foreach (var hvoItem in itemsToChangeAsList)
				{
					i++;
					if (i % interval == 0)
					{
						state.PercentDone = i * 100 / itemsToChangeAsList.Count;
						state.Breath();
					}
					bool fEnable;
					// If the field is on an owned object that might not exist, the hvoItem might
					// refer to the owner, which is likely of a different class.  In such cases,
					// we don't want to try getting the field value, since that produces a pretty
					// green dialog box for the user.  See FWR-3199.
					var clid = m_sda.get_IntProp(hvoItem, CmObjectTags.kflidClass);
					var flids = mdcManaged.GetFields(clid, true, (int)CellarPropertyTypeFilter.All);
					if (!flids.Contains(m_flid))
					{
						fEnable = false;
					}
					else if (type == (int)CellarPropertyType.Boolean)
					{
						fEnable = m_sda.get_BooleanProp(hvoItem, m_flid) != (val != 0);
					}
					else
					{
						fEnable = m_sda.get_IntProp(hvoItem, m_flid) != val;
					}
					if (fEnable)
					{
						m_sda.SetString(hvoItem, tagMadeUpFieldIdentifier, tssVal);
					}
					m_sda.SetInt(hvoItem, tagEnabled, (fEnable ? 1 : 0));
				}
			}

			protected override bool TryGetOriginalListValue(ISilDataAccess sda, int hvoItem, out int value)
			{
				value = int.MinValue;
				var hvoOwningInt = hvoItem;
				if (m_ghostParentHelper != null)
				{
					hvoOwningInt = m_ghostParentHelper.GetOwnerOfTargetProperty(hvoItem);
				}
				if (hvoOwningInt == 0)
				{
					return false;
				}
				value = GetBasicPropertyValue(sda, hvoOwningInt);
				return true;
			}

			protected virtual int GetBasicPropertyValue(ISilDataAccess sda, int hvoOwner)
			{
				var type = m_sda.MetaDataCache.GetFieldType(m_flid);
				if (type == (int)CellarPropertyType.Boolean)
				{
					return sda.get_BooleanProp(hvoOwner, m_flid) ? 1 : 0;
				}
				return sda.get_IntProp(hvoOwner, m_flid);
			}

			public override bool CanClearField => false;

			public override void SetClearField()
			{
				throw new Exception("The IntChooserBEditControl.SetClearField() method is not implemented.");
			}

			public override List<int> FieldPath => new List<int>(new int[] { m_flid });

			#endregion

			#region IGetReplacedObjects Members

			/// <summary>
			/// Objects get replaced here when dummies are changed to real.
			/// </summary>
			public Dictionary<int, int> ReplacedObjects { get; } = new Dictionary<int, int>();

			#endregion
		}

		private sealed class BooleanChooserBEditControl : IntChooserBEditControl
		{
			internal BooleanChooserBEditControl(string itemList, int flid)
				: base(itemList, flid)
			{ }

			protected override int GetBasicPropertyValue(ISilDataAccess sda, int hvoOwner)
			{
				return Convert.ToInt32(IntBoolPropertyConverter.GetBoolean(m_sda, hvoOwner, m_flid));
			}

			protected override void SetBasicPropertyValue(ISilDataAccess sda, int newVal, int hvoOwner)
			{
				Debug.Assert(newVal == 0 || newVal == 1, $"Expected value {newVal} to be boolean.");
				IntBoolPropertyConverter.SetValueFromBoolean(m_sda, hvoOwner, m_flid, Convert.ToBoolean(newVal));
			}
		}

		private class IntOnSubfieldChooserBEditControl : IntChooserBEditControl
		{
			protected int m_subFieldId;
			/// <summary>
			/// Initialized with a string like "0:no;1:yes".
			/// </summary>
			public IntOnSubfieldChooserBEditControl(string itemList, int mainFieldId, int subFieldId)
				: base(itemList, mainFieldId)
			{
				m_subFieldId = subFieldId;
			}

			public override List<int> FieldPath
			{
				get
				{
					if (m_subFieldId == LexEntryRefTags.kflidHideMinorEntry)
					{
						return new List<int>(new[] { m_subFieldId });
					}
					var fieldPath = base.FieldPath;
					fieldPath.Add(m_subFieldId);
					return fieldPath;
				}

			}

			public override void FakeDoit(IEnumerable<int> itemsToChange, int tagMadeUpFieldIdentifier, int tagEnabled, ProgressState state)
			{
				var itemsToChangeAsList = new List<int>(itemsToChange);
				var val = (m_combo.SelectedItem as IntComboItem).Value;
				var tssVal = TsStringUtils.MakeString(m_combo.SelectedItem.ToString(), Cache.DefaultUserWs);
				var i = 0;
				// Report progress 50 times or every 100 items, whichever is more
				// (but no more than once per item!)
				var interval = Math.Min(100, Math.Max(itemsToChangeAsList.Count / 50, 1));
				if (m_subFieldId == LexEntryRefTags.kflidHideMinorEntry)
				{
					// we present this to the user as "Show" instead of "Hide"
					val = val == 0 ? 1 : 0;
					foreach (var hvoItem in itemsToChangeAsList)
					{
						i++;
						if (i % interval == 0)
						{
							state.PercentDone = i * 100 / itemsToChangeAsList.Count;
							state.Breath();
						}
						Debug.Assert(m_sda.get_IntProp(hvoItem, CmObjectTags.kflidClass) == LexEntryRefTags.kClassId);
						var valOld = m_sda.get_IntProp(hvoItem, m_subFieldId);
						var fEnable = valOld != val;
						if (fEnable)
						{
							m_sda.SetString(hvoItem, tagMadeUpFieldIdentifier, tssVal);
						}
						m_sda.SetInt(hvoItem, tagEnabled, (fEnable ? 1 : 0));
					}
				}
				else
				{
					foreach (var hvoItem in itemsToChangeAsList)
					{
						i++;
						if (i % interval == 0)
						{
							state.PercentDone = i * 100 / itemsToChangeAsList.Count;
							state.Breath();
						}
						var hvoField = m_sda.get_ObjectProp(hvoItem, m_flid);
						if (hvoField == 0)
						{
							continue;
						}
						var valOld = GetValueOfField(m_sda, hvoField);
						var fEnable = valOld != val;
						if (fEnable)
						{
							m_sda.SetString(hvoItem, tagMadeUpFieldIdentifier, tssVal);
						}
						m_sda.SetInt(hvoItem, tagEnabled, (fEnable ? 1 : 0));
					}
				}
			}

			internal virtual int GetValueOfField(ISilDataAccess sda, int hvoField)
			{
				return sda.get_IntProp(hvoField, m_subFieldId);
			}

			public override void DoIt(IEnumerable<int> itemsToChange, ProgressState state)
			{
				var itemsToChangeAsList = new List<int>(itemsToChange);
				Cache.DomainDataByFlid.BeginUndoTask(XMLViewsStrings.ksUndoBulkEdit, XMLViewsStrings.ksRedoBulkEdit);
				var sda = Cache.DomainDataByFlid;
				var val = (m_combo.SelectedItem as IntComboItem).Value;
				var i = 0;
				// Report progress 50 times or every 100 items, whichever is more (but no more than once per item!)
				var interval = Math.Min(100, Math.Max(itemsToChangeAsList.Count / 50, 1));
				if (m_subFieldId == LexEntryRefTags.kflidHideMinorEntry)
				{
					// we present this to the user as "Show" instead of "Hide"
					val = val == 0 ? 1 : 0;
					foreach (var hvoItem in itemsToChangeAsList)
					{
						i++;
						if (i % interval == 0)
						{
							state.PercentDone = i * 100 / itemsToChangeAsList.Count;
							state.Breath();
						}
						Debug.Assert(m_sda.get_IntProp(hvoItem, CmObjectTags.kflidClass) == LexEntryRefTags.kClassId);
						var valOld = m_sda.get_IntProp(hvoItem, m_subFieldId);
						if (valOld != val)
						{
							sda.SetInt(hvoItem, m_subFieldId, val);
						}
					}
				}
				else
				{
					foreach (var hvoItem in itemsToChangeAsList)
					{
						i++;
						if (i % interval == 0)
						{
							state.PercentDone = i * 100 / itemsToChangeAsList.Count;
							state.Breath();
						}
						var hvoField = sda.get_ObjectProp(hvoItem, m_flid);
						if (hvoField == 0)
						{
							continue;
						}
						var valOld = GetValueOfField(sda, hvoField);
						if (valOld == val)
						{
							continue;
						}
						SetValueOfField(sda, hvoField, val);
					}
				}
				Cache.DomainDataByFlid.EndUndoTask();
			}

			internal virtual void SetValueOfField(ISilDataAccess sda, int hvoField, int val)
			{
				sda.SetInt(hvoField, m_subFieldId, val);
			}
		}

		/// <summary>
		/// This adapts IntOnSubfield chooser to use 0 for a boolean false and 1 for a boolean true
		/// </summary>
		private sealed class BoolOnSubfieldChooserBEditControl : IntOnSubfieldChooserBEditControl
		{
			public BoolOnSubfieldChooserBEditControl(string itemList, int flid, int flidSub) : base(itemList, flid, flidSub)
			{
				if (m_combo.Items.Count != 2)
				{
					throw new ArgumentException("BoolOnSubfieldChooserBEditControl must be created with a two-item list of options");
				}
			}

			internal override int GetValueOfField(ISilDataAccess sda, int hvoField)
			{
				return sda.get_BooleanProp(hvoField, m_subFieldId) ? 1 : 0;
			}

			internal override void SetValueOfField(ISilDataAccess sda, int hvoField, int val)
			{
				sda.SetBoolean(hvoField, m_subFieldId, val == 1);
			}
		}

#if RANDYTODO
	// TODO: Why does this class need to exist? It adds nothing to the superclass.
	// TODO: Jason Naylor's Gerrit comment: "I saw some comments in an earlier file about a hack to handle
	// TODO: Variants separately from ComplexForms, I wonder if this class is involved in that hack?"
#endif
		private sealed class VariantEntryTypesChooserBEditControl : ComplexListChooserBEditControl
		{
			internal VariantEntryTypesChooserBEditControl(LcmCache cache, IPropertyTable propertyTable, XElement colSpec)
				: base(cache, propertyTable, colSpec)
			{
			}
		}

		/// <summary>
		/// Allows a generic way to access a string in a browse view column cell.
		/// (Currently used for Source combo items)
		/// </summary>
		private sealed class ManyOnePathSortItemReadWriter : FieldReadWriter, IDisposable
		{
			private LcmCache m_cache;
			private XElement m_colSpec;
			private BrowseViewer m_bv;
			private IStringFinder m_finder;
			private IApp m_app;

			internal ManyOnePathSortItemReadWriter(LcmCache cache, XElement colSpec, BrowseViewer bv, IApp app)
				: base(cache.DomainDataByFlid)
			{
				m_cache = cache;
				m_colSpec = colSpec;
				m_bv = bv;
				m_app = app;
				EnsureFinder();
			}

			#region Disposable stuff

			/// <summary/>
			~ManyOnePathSortItemReadWriter()
			{
				Dispose(false);
			}

			/// <summary/>
			private bool IsDisposed { get; set; }

			/// <summary/>
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			/// <summary/>
			private void Dispose(bool fDisposing)
			{
				Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType() + " *******");
				if (IsDisposed)
				{
					// No need to run it more than once.
					return;
				}

				if (fDisposing)
				{
					// dispose managed and unmanaged objects
					(m_finder as IDisposable)?.Dispose();
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
				return new ManyOnePathSortItem(hvo, null, null);    // just return an item based on the the RootObject
			}

			private void EnsureFinder()
			{
				if (m_finder == null)
				{
					m_finder = LayoutFinder.CreateFinder(m_cache, m_colSpec, m_bv.BrowseView.Vc, m_app);
				}
			}

			/// <summary>
			/// Get's the string associated with the given hvo for a particular column.
			/// </summary>
			public override ITsString CurrentValue(int hvo)
			{
				return m_finder.Key(GetManyOnePathSortItem(hvo));
			}

			/// <summary>
			/// NOTE: ManyOnePathSortItemReadWriter is currently read-only.
			/// </summary>
			public override void SetNewValue(int hvo, ITsString tss)
			{
				throw new NotSupportedException();
			}

			/// <summary>
			///
			/// </summary>
			public override int WritingSystem
			{
				get { throw new NotSupportedException(); }
			}
		}

		/// <summary>
		/// This class implements setting an atomic reference property to a value chosen from a flat list,
		/// by means of a combo box showing the items in the list.
		/// </summary>
		private class FlatListChooserBEditControl : IBulkEditSpecControl, IGhostable, IDisposable
		{
			protected XMLViewsDataCache m_sda;
			protected FwComboBox m_combo;
			private int m_ws;
			private int m_hvoList;
			private bool m_useAbbr;
			protected int m_flidAtomicProp;
			private IVwStylesheet m_stylesheet;
			private GhostParentHelper m_ghostParentHelper;

			public Button SuggestButton => null;

			public event FwSelectionChangedEventHandler ValueChanged;

			public FlatListChooserBEditControl(int flidAtomicProp, int hvoList, int ws, bool useAbbr)
			{
				m_ws = ws;
				m_hvoList = hvoList;
				m_useAbbr = useAbbr;
				m_flidAtomicProp = flidAtomicProp;
			}

			#region IBulkEditSpecControl Members

			/// <summary>
			/// Get/Set the property table.
			/// </summary>
			public IPropertyTable PropertyTable { get; set; }

			public Control Control
			{
				get
				{
					if (m_combo == null)
					{
						FillComboBox();
					}
					return m_combo;
				}
			}

			public LcmCache Cache { get; set; }

			/// <summary>
			/// The special cache that can handle the preview and check-box properties.
			/// </summary>
			public XMLViewsDataCache DataAccess
			{
				get
				{
					if (m_sda == null)
					{
						throw new InvalidOperationException("Must set the special cache of a BulkEditSpecControl");
					}
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
					{
						m_combo.StyleSheet = value;
					}
				}
			}

			public virtual void DoIt(IEnumerable<int> itemsToChange, ProgressState state)
			{
				UndoableUnitOfWorkHelper.Do(XMLViewsStrings.ksUndoBulkEdit, XMLViewsStrings.ksRedoBulkEdit, Cache.ActionHandlerAccessor, () =>
				{
					var sda = Cache.DomainDataByFlid;
					var item = m_combo.SelectedItem as HvoTssComboItem;
					if (item == null)
					{
						return;
					}
					var hvoSel = item.Hvo;
					var mdcManaged = Cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();
					var i = 0;
					// Report progress 50 times or every 100 items, whichever is more (but no more than once per item!)
					var interval = Math.Min(100, Math.Max(itemsToChange.Count() / 50, 1));
					foreach (var hvoItem in itemsToChange)
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
						var clid = m_sda.get_IntProp(hvoItem, CmObjectTags.kflidClass);
						var flids = mdcManaged.GetFields(clid, true, (int)CellarPropertyTypeFilter.All);
						if (!flids.Contains(m_flidAtomicProp))
						{
							continue;
						}
						var hvoOld = GetOriginalListValue(sda, hvoItem);
						if (hvoOld == hvoSel)
						{
							continue;
						}
						UpdateListItemToNewValue(sda, hvoItem, hvoSel, hvoOld);
					}
				});
			}

			/// <summary />
			protected virtual void UpdateListItemToNewValue(ISilDataAccess sda, int hvoItem, int hvoSel, int hvoOld)
			{
				var hvoOwningAtomic = hvoItem;
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
			/// modified. For ones that can, it should set the string property tagMadeUpFieldIdentifier
			/// to the value to show in the 'modified' fields.
			/// </summary>
			public virtual void FakeDoit(IEnumerable<int> itemsToChange, int tagMadeUpFieldIdentifier, int tagEnabled, ProgressState state)
			{
				var sda = Cache.DomainDataByFlid;
				var item = m_combo.SelectedItem as HvoTssComboItem;
				if (item == null)
				{
					return;
				}
				var hvoSel = item.Hvo;
				var mdcManaged = Cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();
				var i = 0;
				// Report progress 50 times or every 100 items, whichever is more
				// (but no more than once per item!)
				var interval = Math.Min(100, Math.Max(itemsToChange.Count() / 50, 1));
				foreach (var hvoItem in itemsToChange)
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
					var clid = m_sda.get_IntProp(hvoItem, CmObjectTags.kflidClass);
					var flids = mdcManaged.GetFields(clid, true, (int)CellarPropertyTypeFilter.All);
					if (!flids.Contains(m_flidAtomicProp))
					{
						fEnable = false;
					}
					else
					{
						var hvoOld = GetOriginalListValue(sda, hvoItem);
						fEnable = hvoOld != hvoSel;
					}
					if (fEnable)
					{
						m_sda.SetString(hvoItem, tagMadeUpFieldIdentifier, item.AsTss);
					}
					m_sda.SetInt(hvoItem, tagEnabled, (fEnable ? 1 : 0));
				}
			}

			/// <summary>
			/// It's possible that hvoItem will be an owner of a ghost (ie. an absent source object for m_flidAtomicProp).
			/// In that case, the cache should return 0.
			/// </summary>
			private int GetOriginalListValue(ISilDataAccess sda, int hvoItem)
			{
				return sda.get_ObjectProp(hvoItem, m_flidAtomicProp);
			}

			/// <summary>
			/// Used by SemanticDomainChooserBEditControl to make suggestions and then call FakeDoIt
			/// </summary>
			public void MakeSuggestions(IEnumerable<int> itemsToChange, int tagMadeUpFieldIdentifier, int tagEnabled, ProgressState state)
			{
				throw new NotSupportedException();
			}

			#endregion

			protected virtual void FillComboBox()
			{
				m_combo = new FwComboBox
				{
					DropDownStyle = ComboBoxStyle.DropDownList,
					WritingSystemFactory = Cache.WritingSystemFactory,
					WritingSystemCode = m_ws,
					StyleSheet = m_stylesheet
				};
				var labeledList = GetLabeledList();
				// if the possibilities list IsSorted (misnomer: needs to be sorted), do that now.
				if (labeledList.Count > 1) // could be zero if list non-existant, if 1 don't need to sort either!
				{
					if (Cache.ServiceLocator.GetInstance<ICmPossibilityListRepository>().GetObject(m_hvoList).IsSorted)
					{
						labeledList.Sort();
					}
				}
				// now add list to combo box in that order.
				foreach (var labelItem in labeledList)
				{
					m_combo.Items.Add(new HvoTssComboItem(labelItem.Hvo, labelItem.TssLabel));
				}
				// Don't allow <Not Sure> for MorphType selection.  See FWR-1632.
				if (m_hvoList != Cache.LangProject.LexDbOA.MorphTypesOA.Hvo)
				{
					m_combo.Items.Add(new HvoTssComboItem(0, TsStringUtils.MakeString(XMLViewsStrings.ksNotSure, Cache.WritingSystemFactory.UserWs)));
				}
				m_combo.SelectedIndexChanged += m_combo_SelectedIndexChanged;
			}

			private List<HvoLabelItem> GetLabeledList()
			{
				var tagName = m_useAbbr ? CmPossibilityTags.kflidAbbreviation : CmPossibilityTags.kflidName;
				var chvo = m_hvoList > 0 ? Cache.DomainDataByFlid.get_VecSize(m_hvoList, CmPossibilityListTags.kflidPossibilities) : 0;
				var al = new List<HvoLabelItem>(chvo);
				for (var i = 0; i < chvo; i++)
				{
					var hvoChild = Cache.DomainDataByFlid.get_VecItem(m_hvoList, CmPossibilityListTags.kflidPossibilities, i);
					al.Add(new HvoLabelItem(hvoChild, GetItemLabel(hvoChild, tagName)));
				}
				return al;
			}

			/// <summary>
			/// Gets the item label.
			/// </summary>
			private ITsString GetItemLabel(int hvoChild, int tagName)
			{
				// Try getting the label with the user writing system.
				var tssLabel = Cache.DomainDataByFlid.get_MultiStringAlt(hvoChild, tagName, m_ws);
				// If that doesn't work, try using the default user writing system.
				if (string.IsNullOrEmpty(tssLabel?.Text))
				{
					tssLabel = Cache.DomainDataByFlid.get_MultiStringAlt(hvoChild, tagName, Cache.ServiceLocator.WritingSystemManager.UserWs);
				}
				// If that doesn't work, then fallback to the whatever the cache considers
				// to be the fallback writing system (probably english).
				if (string.IsNullOrEmpty(tssLabel?.Text))
				{
					tssLabel = Cache.DomainDataByFlid.get_MultiStringAlt(hvoChild, tagName, WritingSystemServices.FallbackUserWs(Cache));
				}
				return tssLabel;
			}

			/// <summary>
			/// Handles the SelectedIndexChanged event of the m_combo control.
			/// </summary>
			private void m_combo_SelectedIndexChanged(object sender, EventArgs e)
			{
				// Tell the parent control that we may have changed the selected item so it can
				// enable or disable the Apply and Preview buttons based on the selection.
				if (ValueChanged == null)
				{
					return;
				}
				var index = m_combo.SelectedIndex;
				var htci = m_combo.SelectedItem as HvoTssComboItem;
				var hvo = htci?.Hvo ?? 0;
				ValueChanged(sender, new FwObjectSelectionEventArgs(hvo, index));
			}

			/// <summary>
			/// This type of editor can always select null.
			/// </summary>
			public bool CanClearField => true;

			/// <summary>
			/// And does it by choosing the final, 'Not sure' item.
			/// </summary>
			public void SetClearField()
			{
				if (m_combo == null)
				{
					FillComboBox();
				}
				m_combo.SelectedIndex = m_combo.Items.Count - 1;
			}

			public virtual List<int> FieldPath => new List<int>(new int[] { m_flidAtomicProp });

			#region Disposable stuff

			/// <summary/>
			~FlatListChooserBEditControl()
			{
				Dispose(false);
			}

			/// <summary />
			private bool IsDisposed { get; set; }

			/// <summary />
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			/// <summary />
			protected virtual void Dispose(bool fDisposing)
			{
				Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
				if (IsDisposed)
				{
					// No need to run it more than once.
					return;
				}

				if (fDisposing && !IsDisposed)
				{
					// dispose managed and unmanaged objects
					m_combo?.Dispose();
				}
				m_combo = null;
				IsDisposed = true;
			}
			#endregion

			#region IGhostable Members

			public void InitForGhostItems(LcmCache cache, XElement colSpec)
			{
				m_ghostParentHelper = GetGhostHelper(cache.ServiceLocator, colSpec);
			}

			#endregion
		}

		/// <summary>
		/// This class implements setting the MorphType of the MoForm belonging to the LexemeForm
		/// field of a LexEntry.
		/// </summary>
		private sealed class MorphTypeChooserBEditControl : FlatListChooserBEditControl
		{
			private int m_flidParent;
			private BrowseViewer m_containingViewer;

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
					var fieldPath = base.FieldPath;
					fieldPath.Insert(0, m_flidParent);
					return fieldPath;
				}
			}

			public override void DoIt(IEnumerable<int> itemsToChange, ProgressState state)
			{
				UndoableUnitOfWorkHelper.Do(XMLViewsStrings.ksUndoBulkEdit, XMLViewsStrings.ksRedoBulkEdit, Cache.ActionHandlerAccessor, () =>
				{
					var sda = Cache.DomainDataByFlid;
					var item = m_combo.SelectedItem as HvoTssComboItem;
					if (item == null)
					{
						return;
					}
					var hvoSelMorphType = item.Hvo;
					var fSelAffix = false;
					if (hvoSelMorphType != 0)
					{
						fSelAffix = MorphServices.IsAffixType(Cache, hvoSelMorphType);
					}
					var fAnyFundamentalChanges = false;
					// Preliminary check and warning if changing fundamental type.
					foreach (var hvoLexEntry in itemsToChange)
					{
						var hvoLexemeForm = sda.get_ObjectProp(hvoLexEntry, m_flidParent);
						if (hvoLexemeForm == 0)
						{
							continue;
						}
						var hvoMorphType = sda.get_ObjectProp(hvoLexemeForm, m_flidAtomicProp);
						if (hvoMorphType == 0)
						{
							continue;
						}
						var fAffix = MorphServices.IsAffixType(Cache, hvoMorphType);
						if (fAffix == fSelAffix || hvoSelMorphType == 0)
						{
							continue;
						}
						var msg = string.Format(XMLViewsStrings.ksMorphTypeChangesSlow, fAffix ? XMLViewsStrings.ksAffixes : XMLViewsStrings.ksStems, fAffix ? XMLViewsStrings.ksStems : XMLViewsStrings.ksAffixes);
						if (MessageBox.Show(m_combo, msg, XMLViewsStrings.ksChangingMorphType, MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK)
						{
							return;
						}
						fAnyFundamentalChanges = true;
						break; // user OKd it, no need to check further.
					}
					if (fAnyFundamentalChanges)
					{
						m_containingViewer.SetListModificationInProgress(true);
					}
					try
					{
						// Report progress 50 times or every 100 items, whichever is more
						// (but no more than once per item!)
						var idsToDel = new HashSet<int>();
						var newForms = new Dictionary<IMoForm, ILexEntry>();
						var interval = Math.Min(80, Math.Max(itemsToChange.Count() / 50, 1));
						var i = 0;
						var rgmsaOld = new List<IMoMorphSynAnalysis>();
						foreach (var hvoLexEntry in itemsToChange)
						{
							// Guess we're 80% done when through all but deleting leftover objects and moving
							// new MoForms to LexemeForm slot.
							if ((i + 1) % interval == 0)
							{
								state.PercentDone = i * 80 / itemsToChange.Count();
								state.Breath();
							}
							i++;
							var hvoLexemeForm = sda.get_ObjectProp(hvoLexEntry, m_flidParent);
							if (hvoLexemeForm == 0)
							{
								continue;
							}
							var hvoMorphType = sda.get_ObjectProp(hvoLexemeForm, m_flidAtomicProp);
							if (hvoMorphType == 0)
							{
								continue;
							}
							var fAffix = MorphServices.IsAffixType(Cache, hvoMorphType);
							var stemAlloFactory = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>();
							var afxAlloFactory = Cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>();
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
								var entry = Cache.ServiceLocator.GetInstance<ILexEntryRepository>().GetObject(hvoLexEntry);
								var affix = Cache.ServiceLocator.GetInstance<IMoAffixAllomorphRepository>().GetObject(hvoLexemeForm);
								var stem = stemAlloFactory.Create();
								rgmsaOld.Clear();
								rgmsaOld.AddRange(entry.MorphoSyntaxAnalysesOC.Where(msa => !(msa is IMoStemMsa)));
								entry.ReplaceObsoleteMsas(rgmsaOld);
								SwapFormValues(entry, affix, stem, hvoSelMorphType, idsToDel);
								foreach (var env in affix.PhoneEnvRC)
								{
									stem.PhoneEnvRC.Add(env);
								}
								newForms[stem] = entry;
							}
							else
							{
								// Changing from stem to affix, need a new allomorph object.
								var entry = Cache.ServiceLocator.GetInstance<ILexEntryRepository>().GetObject(hvoLexEntry);
								var stem = Cache.ServiceLocator.GetInstance<IMoStemAllomorphRepository>().GetObject(hvoLexemeForm);
								var affix = afxAlloFactory.Create();
								rgmsaOld.Clear();
								rgmsaOld.AddRange(entry.MorphoSyntaxAnalysesOC.OfType<IMoStemMsa>().Cast<IMoMorphSynAnalysis>());
								entry.ReplaceObsoleteMsas(rgmsaOld);
								SwapFormValues(entry, stem, affix, hvoSelMorphType, idsToDel);
								foreach (var env in stem.PhoneEnvRC)
								{
									affix.PhoneEnvRC.Add(env);
								}
								newForms[affix] = entry;
							}
						}
						if (fAnyFundamentalChanges)
						{
							foreach (var hvo in idsToDel)
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
						{
							m_containingViewer.SetListModificationInProgress(false);
						}
					}
				});
			}
			// Swap values of various attributes between an existing form that is a LexemeForm and
			// a newly created one. Includes adding the new one to the alternate forms of the entry, and
			// the id of the old one to a map of things to delete.
			private void SwapFormValues(ILexEntry entry, IMoForm origForm, IMoForm newForm, int typeHvo, HashSet<int> idsToDel)
			{
				entry.AlternateFormsOS.Add(newForm);
				origForm.SwapReferences(newForm);
				var muaOrigForm = origForm.Form;
				var muaNewForm = newForm.Form;
				muaNewForm.MergeAlternatives(muaOrigForm);
				newForm.MorphTypeRA = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(typeHvo);
				idsToDel.Add(origForm.Hvo);
			}

			public override void FakeDoit(IEnumerable<int> itemsToChange, int tagMadeUpFieldIdentifier, int tagEnabled, ProgressState state)
			{
				var sda = Cache.DomainDataByFlid;
				var item = m_combo.SelectedItem as HvoTssComboItem;
				if (item == null)
				{
					return;
				}
				var hvoSelMorphType = item.Hvo;
				// Report progress 50 times or every 100 items, whichever is more
				// (but no more than once per item!)
				var interval = Math.Min(100, Math.Max(itemsToChange.Count() / 50, 1));
				var i = 0;
				foreach (var hvoLexEntry in itemsToChange)
				{
					if ((i + 1) % interval == 0)
					{
						state.PercentDone = i * 100 / itemsToChange.Count();
						state.Breath();
					}
					var hvoLexemeForm = sda.get_ObjectProp(hvoLexEntry, m_flidParent);
					if (hvoLexemeForm == 0)
					{
						continue;
					}
					var hvoMorphType = sda.get_ObjectProp(hvoLexemeForm, m_flidAtomicProp);
					if (hvoMorphType == 0)
					{
						continue;
					}
					// Per LT-5305, OK to switch types.
					//bool fEnable = fAffix == fSelAffix && hvoMorphType != hvoSelMorphType;
					var fEnable = hvoMorphType != hvoSelMorphType;
					if (fEnable)
					{
						m_sda.SetString(hvoLexEntry, tagMadeUpFieldIdentifier, item.AsTss);
					}
					m_sda.SetInt(hvoLexEntry, tagEnabled, (fEnable ? 1 : 0));
					i++;
				}
			}

			#endregion
		}

		/// <summary>
		/// This class allows, for example, the morph type items to be sorted alphabetically in the
		/// listbox by providing the IComparable interface.
		/// </summary>
		private sealed class HvoLabelItem : IComparable
		{
			private readonly string m_sortString;

			public HvoLabelItem(int hvoChild, ITsString tssLabel)
			{
				Hvo = hvoChild;
				TssLabel = tssLabel;
				m_sortString = tssLabel.Text;
			}

			public int Hvo { get; }

			public ITsString TssLabel { get; }

			public override string ToString()
			{
				return m_sortString;
			}

			#region IComparable Members

			public int CompareTo(object obj)
			{
				return m_sortString?.CompareTo(obj.ToString()) ?? string.Empty.CompareTo(obj.ToString());
			}

			#endregion
		}
	}
}