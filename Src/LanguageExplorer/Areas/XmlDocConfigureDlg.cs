// Copyright (c) 2007-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

#if DEBUG
// <remarks>
// Uncomment the #define if you want to see the "Restore Defaults" and "Set/Clear All" buttons.
// (This affects only DEBUG builds.)
// </remarks>
//#define DEBUG_TEST
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Controls.XMLViews;
using LanguageExplorer.DictionaryConfiguration;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FwCoreDlgControls;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainImpl;
using SIL.LCModel.DomainServices;
using SIL.Windows.Forms;
using SIL.Xml;
using static System.Char;

namespace LanguageExplorer.Areas
{
	/// <summary>
	/// XmlDocConfigureDlg is used to configure parts of a jtview layout, for instance, as used
	/// for the Dictionary view in Flex.  It builds a tree view from the XML &lt;part&gt; nodes
	/// contained within &lt;layout&gt; nodes, following the path of layouts called by parts called
	/// by those "part ref" nodes.
	/// The first such document handled is a dictionary in the Language Explorer program.  There
	/// are a few traces of that in the code below, but most of the code should be rather generic.
	/// If any additional documents are configured with this tool, it may be worth try to generalize
	/// some of these fine points, possibly by adding more configuration control attributes like the
	/// hideConfig="true" attributes added to a couple of part refs just to make the node tree look
	/// nicer to the users.
	/// </summary>
	internal partial class XmlDocConfigureDlg : Form, ILayoutConverter
	{
		XElement m_configurationParameters;
		string m_defaultRootLayoutName;
		const string sdefaultStemBasedLayout = "publishStem";
		IFwMetaDataCache m_mdc;
		LcmStyleSheet m_styleSheet;
		IFwMainWnd m_mainWindow;
		private IPropertyTable m_propertyTable;
		private IPublisher m_publisher;
		string m_sLayoutPropertyName;
		Inventory m_layouts;
		Inventory m_parts;
		LayoutTreeNode m_current;
		List<StyleComboItem> m_rgCharStyles;
		List<StyleComboItem> m_rgParaStyles;
		// The original distance from the bottom of panel1 to the top of m_lblContext.
		int m_dyContextOffset;
		// The original distance from the top of m_lblContext to the bottom of the dialog.
		private int m_dyOriginalTopOfContext;
		private string m_helpTopicID; // should we store the helpID or the configObject
		bool m_fDeleteCustomFiles;
		string m_configObjectName;
		string m_configNotLocalizedObjectName;
		/// <summary>
		/// Store nodes that need to be persisted as part of a new copy of a whole layout,
		/// but which don't directly (or indirectly) appear in configuration.
		/// </summary>
		readonly List<XElement> m_rgxnNewLayoutNodes = new List<XElement>();

		private readonly Guid m_unspecComplexFormType;
		private readonly Guid m_unspecVariantType;

		private List<ICmPossibility> m_rgRelationTypes;
		private List<ICmPossibility> m_rgComplexFormTypes;
		private List<ICmPossibility> m_rgVariantTypes;

		/// <summary>
		/// True if a MasterRefresh must be done when the dialog closes (even if the user cancels!)
		/// This gets set if we change the global homograph configuration while running the dialog.
		/// </summary>
		public bool MasterRefreshRequired { get; set; }

		/// <summary>
		/// Label for top of Complex Entry Type list (and top of Minor Entry Type list in Root-based views).
		/// </summary>
		private readonly string m_noComplexEntryTypeLabel;

		/// <summary>
		/// Label for top of Variant Type list and Minor Entry Type list.
		/// </summary>
		private readonly string m_noVariantTypeLabel;

		/// <summary>
		/// Toggles true to set, false to clear.
		/// </summary>
		private bool m_fValueForSetAll = true;

		#region Constructor, Initialization, etc.

		/// <summary>
		/// Convert a distance that is right at 96 dpi to the current screen dpi
		/// </summary>
		private int From96dpiY(int input)
		{
			using (var g = CreateGraphics())
			{
				return (int)Math.Round(input*g.DpiY/96.0);
			}
		}

		/// <summary>
		/// Convert a distance that is right at 96 dpi to the current screen dpi
		/// </summary>
		private int From96dpiX(int input)
		{
			using (var g = CreateGraphics())
			{
				return (int)Math.Round(input * g.DpiX / 96.0);
			}
		}

		public XmlDocConfigureDlg()
		{
			InitializeComponent();

			m_lvItems.SelectedIndexChanged += OnListItemsSelectedIndexChanged;
			m_lvItems.ItemCheck += OnListItemsItemCheck;
			m_btnMoveItemDown.Click += OnMoveItemDownClick;
			m_btnMoveItemUp.Click += OnMoveItemUpClick;

			// Relocate the label and link for parent nodes.
			m_cfgParentNode.Visible = false;
			m_cfgParentNode.Location = new Point(From96dpiX(9), From96dpiY(57));
			m_cfgParentNode.ConfigureNowClicked += m_lnkConfigureNow_LinkClicked;

			// Relocate the controls for Sense nodes.
			m_chkShowSingleGramInfoFirst.Location = new Point(From96dpiX(10), From96dpiY(104));
			m_cfgSenses.Location = new Point(From96dpiX(10),
				m_chkShowSingleGramInfoFirst.Location.Y + m_chkShowSingleGramInfoFirst.Height + From96dpiY(4));
			m_cfgSenses.SensesBtnClicked += m_cfgSenses_SensesBtnClicked;
			m_cfgSenses.DisplaySenseInParaChecked += m_displaySenseInParaChecked;
			m_cfgSenses.Initialize();

			// "showasindentedpara"
			m_chkComplexFormsAsParagraphs.Location = m_chkShowSingleGramInfoFirst.Location;

			m_tvParts.AfterCheck += m_tvParts_AfterCheck;
			m_tbBefore.LostFocus += m_tbBefore_LostFocus;
			m_tbBetween.LostFocus += m_tbBetween_LostFocus;
			m_tbAfter.LostFocus += m_tbAfter_LostFocus;
			m_tbBefore.GotFocus += m_tbBefore_GotFocus;
			m_tbBetween.GotFocus += m_tbBetween_GotFocus;
			m_tbAfter.GotFocus += m_tbAfter_GotFocus;
			m_tbBefore.TextChanged += m_tb_TextChanged;
			m_tbBetween.TextChanged += m_tb_TextChanged;
			m_tbAfter.TextChanged += m_tb_TextChanged;

			m_unspecComplexFormType = XmlViewsUtils.GetGuidForUnspecifiedComplexFormType();
			m_unspecVariantType = XmlViewsUtils.GetGuidForUnspecifiedVariantType();

			m_noComplexEntryTypeLabel = "<" + LanguageExplorerResources.ksNoComplexFormType + ">";
			m_noVariantTypeLabel = "<" + LanguageExplorerResources.ksNoVariantType + ">";
			// This functionality is too difficult to get working on a limited basis (affecting
			// only the current general layout), so it's being disabled here and moved elsewhere.
			// See LT-6984.
			m_btnRestoreDefaults.Visible = false;
			m_btnRestoreDefaults.Enabled = false;
			// We need to show/hide the style controls based on whether we're showing as a paragraph
			// (where that control is valid).
			m_chkComplexFormsAsParagraphs.CheckedChanged += m_chkComplexFormsAsParagraphs_CheckedChanged;
			//m_cfgSenses.VisibleChanged += m_cfgSenses_VisibleChanged;
#if DEBUG
#if DEBUG_TEST
			m_btnRestoreDefaults.Visible = true;	// but it can be useful when testing...
			m_btnRestoreDefaults.Enabled = true;
			m_btnSetAll.Visible = true;
			m_btnSetAll.Enabled = true;
// ReSharper disable LocalizableElement
			m_btnSetAll.Text = "DEBUG: Set All";
// ReSharper restore LocalizableElement
#endif
#endif
		}

		/// <summary>
		/// Initialize the dialog after creating it.
		/// </summary>
		public void SetConfigDlgInfo(XElement configurationParameters, LcmCache cache, LcmStyleSheet styleSheet, IFwMainWnd mainWindow, IPropertyTable propertyTable, IPublisher publisher, string sLayoutPropertyName)
		{
			CheckDisposed();
			m_configurationParameters = configurationParameters;
			var labelKey = XmlUtils.GetOptionalAttributeValue(configurationParameters, "viewTypeLabelKey");
			if (!string.IsNullOrEmpty(labelKey))
			{
				var sLabel = string.Empty;
				switch (labelKey)
				{
					case "ksDictionaryView":
						sLabel = AreaResources.ksDictionaryView;
						break;
				}
				if (!string.IsNullOrEmpty(sLabel))
				{
					m_lblViewType.Text = sLabel;
				}
			}
			Cache = cache;
			m_mdc = Cache.DomainDataByFlid.MetaDataCache;
			m_styleSheet = styleSheet;
			m_mainWindow = mainWindow;
			m_propertyTable = propertyTable;
			m_publisher = publisher;
			m_sLayoutPropertyName = sLayoutPropertyName;
			m_layouts = Inventory.GetInventory("layouts", cache.ProjectId.Name);
			m_parts = Inventory.GetInventory("parts", cache.ProjectId.Name);
			m_configObjectName = StringTable.Table.LocalizeAttributeValue(XmlUtils.GetOptionalAttributeValue(configurationParameters, "configureObjectName", string.Empty));
			m_configNotLocalizedObjectName = XmlUtils.GetOptionalAttributeValue(configurationParameters, "configureObjectName", "");
			Text = string.Format(Text, m_configObjectName);
			m_defaultRootLayoutName = XmlUtils.GetOptionalAttributeValue(configurationParameters, "layout");
			string sLayoutType;
			m_propertyTable.TryGetValue(m_sLayoutPropertyName, out sLayoutType);
			if (string.IsNullOrEmpty(sLayoutType))
			{
				sLayoutType = m_defaultRootLayoutName;
			}

			var configureLayouts = XmlUtils.FindElement(m_configurationParameters, "configureLayouts");
			LegacyConfigurationUtils.BuildTreeFromLayoutAndParts(configureLayouts, this);
			SetSelectedDictionaryTypeItem(sLayoutType);

			// Restore the location and size from last time we called this dialog.
			Point dlgLocation;
			Size dlgSize;
			if (m_propertyTable.TryGetValue("XmlDocConfigureDlg_Location", out dlgLocation) && m_propertyTable.TryGetValue("XmlDocConfigureDlg_Size", out dlgSize))
			{
				var rect = new Rectangle(dlgLocation, dlgSize);
				ScreenHelper.EnsureVisibleRect(ref rect);
				DesktopBounds = rect;
				StartPosition = FormStartPosition.Manual;
			}

			// Make a help topic ID
			m_helpTopicID = generateChooserHelpTopicID(m_configNotLocalizedObjectName);

			// Load the lists for the styles combo boxes.
			SetStylesLists();
		}

		private void SetSelectedDictionaryTypeItem(string sLayoutType)
		{
			var idx = -1;
			for (var i = 0; i < m_cbDictType.Items.Count; ++i)
			{
				var item = m_cbDictType.Items[i] as LayoutTypeComboItem;
				if (item != null && item.LayoutName == sLayoutType)
				{
					idx = i;
					break;
				}
			}

			if (idx < 0)
			{
				idx = 0;
			}
			m_cbDictType.SelectedIndex = idx;
		}

		private void SetStylesLists()
		{
			if (m_rgCharStyles == null)
			{
				m_rgCharStyles = new List<StyleComboItem>();
			}
			else
			{
				m_rgCharStyles.Clear();
			}

			if (m_rgParaStyles == null)
			{
				m_rgParaStyles = new List<StyleComboItem>();
			}
			else
			{
				m_rgParaStyles.Clear();
			}
			m_rgCharStyles.Add(new StyleComboItem(null));
			// Per comments at end of LT-10950, we don't ever want 'none' as an option for paragraph style.
			//m_rgParaStyles.Add(new StyleComboItem(null));
			foreach (var sty in m_styleSheet.Styles)
			{
				if (sty.IsCharacterStyle)
				{
					m_rgCharStyles.Add(new StyleComboItem(sty));
				}
				else if (sty.IsParagraphStyle)
				{
					m_rgParaStyles.Add(new StyleComboItem(sty));
				}
			}
			m_rgCharStyles.Sort();
			m_rgParaStyles.Sort();
		}

		public void BuildRelationTypeList(LayoutTreeNode ltn)
		{
			// Get the canonical list from the project.
			if (m_rgRelationTypes == null)
			{
				m_rgRelationTypes = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.ToList();
				m_rgRelationTypes.Sort(ComparePossibilitiesByName);
			}
			// Add any new types to our ordered list (or fill in an empty list).
			var setSortedGuids = new HashSet<GuidAndSubClass>();
			foreach (var lri in ltn.RelTypeList)
			{
				setSortedGuids.Add(new GuidAndSubClass(lri.ItemGuid, lri.SubClass));
			}
			foreach (var poss in m_rgRelationTypes)
			{
				var lrt = (ILexRefType)poss;
				if (ltn.LexRelType == "sense")
				{
					// skip entry relations when looking at a sense configuration
					if (lrt.MappingType == (int)LexRefTypeTags.MappingTypes.kmtEntryAsymmetricPair ||
						lrt.MappingType == (int)LexRefTypeTags.MappingTypes.kmtEntryCollection ||
						lrt.MappingType == (int)LexRefTypeTags.MappingTypes.kmtEntryPair ||
						lrt.MappingType == (int)LexRefTypeTags.MappingTypes.kmtEntrySequence ||
						lrt.MappingType == (int)LexRefTypeTags.MappingTypes.kmtEntryTree)
					{
						continue;
					}
				}
				else
				{
					// skip sense relations when looking at an entry configuration
					if (lrt.MappingType == (int)LexRefTypeTags.MappingTypes.kmtSenseAsymmetricPair ||
						lrt.MappingType == (int)LexRefTypeTags.MappingTypes.kmtSenseCollection ||
						lrt.MappingType == (int)LexRefTypeTags.MappingTypes.kmtSensePair ||
						lrt.MappingType == (int)LexRefTypeTags.MappingTypes.kmtSenseSequence ||
						lrt.MappingType == (int)LexRefTypeTags.MappingTypes.kmtSenseTree)
					{
						continue;
					}
				}
				var gsc = new GuidAndSubClass(poss.Guid, TypeSubClass.Normal);
				if (lrt.MappingType == (int)LexRefTypeTags.MappingTypes.kmtEntryAsymmetricPair ||
					lrt.MappingType == (int)LexRefTypeTags.MappingTypes.kmtEntryOrSenseAsymmetricPair ||
					lrt.MappingType == (int)LexRefTypeTags.MappingTypes.kmtSenseAsymmetricPair ||
					lrt.MappingType == (int)LexRefTypeTags.MappingTypes.kmtEntryTree ||
					lrt.MappingType == (int)LexRefTypeTags.MappingTypes.kmtEntryOrSenseTree ||
					lrt.MappingType == (int)LexRefTypeTags.MappingTypes.kmtSenseTree)
				{
					gsc.SubClass = TypeSubClass.Forward;
				}
				if (!setSortedGuids.Contains(gsc))
				{
					var lri = new LexReferenceInfo(true, poss.Guid)
						{
							SubClass = gsc.SubClass
						};
					ltn.RelTypeList.Add(lri);
				}
				if (gsc.SubClass == TypeSubClass.Forward)
				{
					gsc.SubClass = TypeSubClass.Reverse;
					if (!setSortedGuids.Contains(gsc))
					{
						var lri = new LexReferenceInfo(true, poss.Guid)
							{
								SubClass = gsc.SubClass
							};
						ltn.RelTypeList.Add(lri);
					}
				}
			}
			// Remove any obsolete types from our ordered list.
			var mapGuidType = new Dictionary<GuidAndSubClass, ILexRefType>();
			foreach (var poss in m_rgRelationTypes)
			{
				var lrt = (ILexRefType)poss;
				var gsc = new GuidAndSubClass(lrt.Guid, TypeSubClass.Normal);
				if (lrt.MappingType == (int)LexRefTypeTags.MappingTypes.kmtEntryAsymmetricPair ||
					lrt.MappingType == (int)LexRefTypeTags.MappingTypes.kmtEntryOrSenseAsymmetricPair ||
					lrt.MappingType == (int)LexRefTypeTags.MappingTypes.kmtSenseAsymmetricPair ||
					lrt.MappingType == (int)LexRefTypeTags.MappingTypes.kmtEntryTree ||
					lrt.MappingType == (int)LexRefTypeTags.MappingTypes.kmtEntryOrSenseTree ||
					lrt.MappingType == (int)LexRefTypeTags.MappingTypes.kmtSenseTree)
				{
					gsc.SubClass = TypeSubClass.Forward;
				}
				mapGuidType.Add(gsc, lrt);
				if (gsc.SubClass == TypeSubClass.Forward)
				{
					var gsc2 = new GuidAndSubClass(lrt.Guid, TypeSubClass.Reverse);
					mapGuidType.Add(gsc2, lrt);
				}
			}
			var obsoleteItems = ltn.RelTypeList.Where(lri => !mapGuidType.ContainsKey(new GuidAndSubClass(lri.ItemGuid, lri.SubClass))).ToList();
			foreach (var lri in obsoleteItems)
			{
				ltn.RelTypeList.Remove(lri);
			}
			// Add the names to the items in the ordered list.
			foreach (var lri in ltn.RelTypeList)
			{
				var lrt = mapGuidType[new GuidAndSubClass(lri.ItemGuid, lri.SubClass)];
				lri.Name = lri.SubClass == TypeSubClass.Reverse ? lrt.ReverseName.BestAnalysisVernacularAlternative.Text : lrt.Name.BestAnalysisVernacularAlternative.Text;
			}
		}

		public void BuildEntryTypeList(LayoutTreeNode ltn, string parentLayoutName)
		{
			// Add any new types to our ordered list (or fill in an empty list).
			var setGuidsFromXml = new HashSet<Guid>(ltn.EntryTypeList.Select(info => info.ItemGuid));
			Dictionary<Guid, ICmPossibility> mapGuidType;
			int index;
			switch (ltn.EntryType)
			{
				case "complex":
					// Get the canonical list from the project if needed.
					if (m_rgComplexFormTypes == null)
					{
						m_rgComplexFormTypes = GetSortedFlattenedComplexFormTypeList();
					}

					mapGuidType = m_rgComplexFormTypes.ToDictionary(poss => poss.Guid);
					foreach (var info in m_rgComplexFormTypes.Where(poss => !setGuidsFromXml.Contains(poss.Guid)).Select(poss => new ItemTypeInfo(true, poss.Guid)))
					{
						ltn.EntryTypeList.Add(info);
					}
					if (!ListContainsGuid(ltn.EntryTypeList, m_unspecComplexFormType, out index))
					{
						var unspecified = new ItemTypeInfo(true, m_unspecComplexFormType);
						ltn.EntryTypeList.Insert(0, unspecified);
						index = 0;
					}
					ltn.EntryTypeList[index].Name = m_noComplexEntryTypeLabel;
					break;
				case "variant":
					// Get the canonical list from the project if needed.
					if (m_rgVariantTypes == null)
					{
						m_rgVariantTypes = GetSortedFlattenedVariantTypeList();
					}

					mapGuidType = m_rgVariantTypes.ToDictionary(poss => poss.Guid);
					foreach (var info in m_rgVariantTypes.Where(poss => !setGuidsFromXml.Contains(poss.Guid)).Select(poss => new ItemTypeInfo(true, poss.Guid)))
					{
						ltn.EntryTypeList.Add(info);
					}
					if (!ListContainsGuid(ltn.EntryTypeList, m_unspecVariantType, out index))
					{
						var unspecified = new ItemTypeInfo(true, m_unspecVariantType);
						ltn.EntryTypeList.Insert(0, unspecified);
						index = 0;
					}
					ltn.EntryTypeList[index].Name = m_noVariantTypeLabel;
					break;
				//case "minor":
				default:
					// Should be 'minor', but treat any unknown entrytype as 'minor'
					var fstemBased = parentLayoutName.Contains(sdefaultStemBasedLayout);
					// Get the canonical Variant Type list from the project if needed.
					if (m_rgVariantTypes == null)
					{
						m_rgVariantTypes = GetSortedFlattenedVariantTypeList();
					}

					mapGuidType = m_rgVariantTypes.ToDictionary(poss => poss.Guid);
					// Root-based views have Complex Forms as Minor Entries too.
					if (!fstemBased)
					{
						// Get the canonical Complex Form Type list from the project if needed.
						if (m_rgComplexFormTypes == null)
						{
							m_rgComplexFormTypes = GetSortedFlattenedComplexFormTypeList();
						}
						// Add them to the map
						foreach (var poss in m_rgComplexFormTypes)
						{
							mapGuidType.Add(poss.Guid, poss);
						}
					}

					// Now make sure the LayoutTreeNode has the right entries
					foreach (var info in mapGuidType.Where(kvp => !setGuidsFromXml.Contains(kvp.Key)).Select(kvp => new ItemTypeInfo(true, kvp.Key)))
					{
						ltn.EntryTypeList.Add(info);
					}
					AddUnspecifiedTypes(fstemBased, fstemBased ? 0 : m_rgVariantTypes.Count, ltn);
					break;
			}
			// Remove any obsolete types from our ordered list.
			var obsoleteItems = ltn.EntryTypeList.Where(info => !IsUnspecifiedPossibility(info) && !mapGuidType.ContainsKey(info.ItemGuid)).ToList();
			foreach (var info in obsoleteItems)
			{
				ltn.EntryTypeList.Remove(info);
			}
			// Add the names to the items in the ordered list.
			foreach (var info in ltn.EntryTypeList)
			{
				if (IsUnspecifiedPossibility(info))
				{
					continue;
				}
				var poss = mapGuidType[info.ItemGuid];
				info.Name = poss.Name.BestAnalysisVernacularAlternative.Text;
			}
		}

		public void LogConversionError(string errorLog)
		{
			Debug.Fail(errorLog);
		}

		/// <summary>
		/// Searches a list of ItemTypeInfo items for a specific Guid (usually representing
		/// an unspecified type 'possibility'. Out variable gives the index at which the Guid is
		/// found in the list, or -1 if not found.
		/// </summary>
		private static bool ListContainsGuid(IList<ItemTypeInfo> itemTypeList, Guid searchGuid, out int index)
		{
			var ffound = false;
			index = -1;
			for (var i = 0; i < itemTypeList.Count; i++)
			{
				var info = itemTypeList[i];
				if (info.ItemGuid != searchGuid)
				{
					continue;
				}
				ffound = true;
				index = i;
				break;
			}
			return ffound;
		}

		private List<ICmPossibility> GetSortedFlattenedVariantTypeList()
		{
			var result = Cache.LangProject.LexDbOA.VariantEntryTypesOA.ReallyReallyAllPossibilities.ToList();
			result.Sort(ComparePossibilitiesByName);
			return result;
		}

		private List<ICmPossibility> GetSortedFlattenedComplexFormTypeList()
		{
			var result = Cache.LangProject.LexDbOA.ComplexEntryTypesOA.ReallyReallyAllPossibilities.ToList();
			result.Sort(ComparePossibilitiesByName);
			return result;
		}

		private void AddUnspecifiedTypes(bool fstemBased, int cvarTypes, LayoutTreeNode ltn)
		{
			int index;
			if (!fstemBased)
			{
				if (!ListContainsGuid(ltn.EntryTypeList, m_unspecComplexFormType, out index))
				{
					var unspecified = new ItemTypeInfo(true, m_unspecComplexFormType);
					ltn.EntryTypeList.Insert(cvarTypes, unspecified);
					index = cvarTypes;
				}
				ltn.EntryTypeList[index].Name = m_noComplexEntryTypeLabel;
			}
			if (!ListContainsGuid(ltn.EntryTypeList, m_unspecVariantType, out index))
			{
				var unspecified = new ItemTypeInfo(true, m_unspecVariantType);
				ltn.EntryTypeList.Insert(0, unspecified);
				index = 0;
			}
			ltn.EntryTypeList[index].Name = m_noVariantTypeLabel;
		}
		#endregion // Constructor, Initialization, etc.

		/// <summary>
		/// Overridden to defeat the standard .NET behavior of adjusting size by
		/// screen resolution. That is bad for this dialog because we remember the size,
		/// and if we remember the enlarged size, it just keeps growing.
		/// If we defeat it, it may look a bit small the first time at high resolution,
		/// but at least it will stay the size the user sets.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnLoad(EventArgs e)
		{
			var size = Size;
			base.OnLoad(e);
// ReSharper disable RedundantCheckBeforeAssignment
			if (Size != size)
			{
				Size = size;
			}
// ReSharper restore RedundantCheckBeforeAssignment
		}

		/// <summary>
		/// Save the location and size for next time.
		/// </summary>
		protected override void OnClosing(CancelEventArgs e)
		{
			if (m_propertyTable != null)
			{
				m_propertyTable.SetProperty("XmlDocConfigureDlg_Location", Location, true, false);
				m_propertyTable.SetProperty("XmlDocConfigureDlg_Size", Size, true, false);
			}
			base.OnClosing(e);
		}

		#region Dialog Event Handlers
		// ReSharper disable InconsistentNaming

		/// <summary>
		/// Users want to see visible spaces.  The only way to do this is to select everything,
		/// and show the selection when the focus leaves for elsewhere.
		/// </summary>
		private void m_tbBefore_LostFocus(object sender, EventArgs e)
		{
			m_tbBefore.SelectAll();
		}

		private void m_tbBetween_LostFocus(object sender, EventArgs e)
		{
			m_tbBetween.SelectAll();
		}

		private void m_tbAfter_LostFocus(object sender, EventArgs e)
		{
			m_tbAfter.SelectAll();
		}

		/// <summary>
		/// When the focus returns, it's probably more useful to select at the end rather
		/// than selecting everything.
		/// </summary>
		private void m_tbBefore_GotFocus(object sender, EventArgs e)
		{
			m_tbBefore.Select(m_tbBefore.Text.Length, 0);
		}

		private void m_tbBetween_GotFocus(object sender, EventArgs e)
		{
			m_tbBetween.Select(m_tbBetween.Text.Length, 0);
		}

		private void m_tbAfter_GotFocus(object sender, EventArgs e)
		{
			m_tbAfter.Select(m_tbAfter.Text.Length, 0);
		}

		/// <summary>
		/// Certain characters are not allowed in XML, even escaped, including 0x0 through 0x1F. Prevent these characters from being entered.
		/// (actually, tabs and linebreaks are in that range and legal, but they cause other problems, so we remove them, too)
		/// </summary>
		private static void m_tb_TextChanged(object sender, EventArgs e)
		{
			const string illegalChars = "[\u0000-\u001F]";
			var tb = (TextBox)sender;
			if (!Regex.IsMatch(tb.Text, illegalChars))
			{
				return;
			}
			tb.Text = Regex.Replace(tb.Text, illegalChars, string.Empty);
			MessageBox.Show(AreaResources.ksIllegalXmlChars, LanguageExplorerResources.ksWarning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
		}

		private void m_cbDictType_SelectedIndexChanged(object sender, EventArgs e)
		{
			var rgltn = ((LayoutTypeComboItem)m_cbDictType.SelectedItem).TreeNodes;
			m_tvParts.Nodes.Clear();
			m_tvParts.Nodes.AddRange(rgltn.ToArray());
			if (m_tvParts.Nodes.Count > 0)
			{
				m_tvParts.SelectedNode = m_tvParts.Nodes[0];
			}
		}

		/// <summary>
		/// Set the active node, based on a string generated by XmlVc.NodeIdentifier,
		/// which makes 3-part strings class:layoutName:partRef.
		/// </summary>
		internal void SetActiveNode(string nodePath)
		{
			if (string.IsNullOrEmpty(nodePath))
			{
				return; // has been crashing with no check
			}
			var idParts = nodePath.Split(':');
			if (idParts.Length != 4)
			{
				return; // throw? 0 at least is plausible
			}
			var className = idParts[0];
			var layoutName = idParts[1];
			var partRef = idParts[2];
			foreach (LayoutTreeNode ltn in m_tvParts.Nodes)
			{
				SetActiveNode(ltn, className, layoutName, partRef);
			}
		}

		private void SetActiveNode(LayoutTreeNode ltn, string className, string layoutName, string partRef)
		{
			if (XmlUtils.GetOptionalAttributeValue(ltn.Configuration, "ref") == partRef
				&& ltn.ParentLayout != null // is this ever possible?
				&& XmlUtils.GetOptionalAttributeValue(ltn.ParentLayout, "class") == className
				&& XmlUtils.GetOptionalAttributeValue(ltn.ParentLayout, "name") == layoutName)
			{
				m_tvParts.SelectedNode = ltn;
				return;
			}

			foreach (LayoutTreeNode ltnChild in ltn.Nodes)
			{
				SetActiveNode(ltnChild, className, layoutName, partRef);
			}
		}

		private void m_btnMoveUp_Click(object sender, EventArgs e)
		{
			StoreNodeData(m_current);	// Ensure duplicate has current data.
			var ltn = (LayoutTreeNode)m_current.Clone();
			var idx = m_current.Index;
			var tnParent = m_current.Parent;
			var old = m_current;
			tnParent.Nodes.Insert(idx - 1, ltn);
			m_tvParts.SelectedNode = ltn;
			// It's important to remove the old one AFTER we insert AND SELECT the new one.
			// otherwise another node is temporarily selected which may somehow result in it
			// being expanded (LT-10337). I chose to user Remove rather than RemoveAt
			// just to remove all doubt about what position it ends up after any side effects of
			// adding the new one, though I think it would work.
			tnParent.Nodes.Remove(old);
		}

		private void m_btnMoveDown_Click(object sender, EventArgs e)
		{
			StoreNodeData(m_current);	// Ensure duplicate has current data.
			var ltn = (LayoutTreeNode)m_current.Clone();
			var idx = m_current.Index;
			var tnParent = m_current.Parent;
			var old = m_current;
			tnParent.Nodes.Insert(idx + 2, ltn); // after the following node, since this one is not yet deleted.
			m_tvParts.SelectedNode = ltn;
			// It's important to remove the old one AFTER we insert AND SELECT the new one.
			// otherwise another node is temporarily selected which may somehow result in it
			// being expanded (LT-10337). I chose to user Remove rather than RemoveAt
			// just to remove all doubt about what position it ends up after any side effects of
			// adding the new one, though I think it would work.
			tnParent.Nodes.Remove(old);
		}

		private void OnDuplicateClick(object sender, EventArgs e)
		{
			Debug.Assert(!m_current.IsTopLevel);
			StoreNodeData(m_current);	// Ensure duplicate has current data.
			LayoutTreeNode ltnDup;

			// Generate a unique label to identify this as the n'th duplicate in the list.
			var rgsLabels = new List<string>();
			string sBaseLabel = null;
			if (m_current.Level == 0)
			{
				var nodes = ((LayoutTypeComboItem)m_cbDictType.SelectedItem).TreeNodes;
				sBaseLabel = GetBaseLabel(nodes, rgsLabels);
			}
			else
			{
				sBaseLabel = GetBaseLabel(m_current.Parent.Nodes, rgsLabels);
			}

			if (sBaseLabel == null)
			{
				sBaseLabel = m_current.Label;
			}
			var cDup = 1;
			var sLabel = $"{sBaseLabel} ({cDup})";
			while (rgsLabels.Contains(sLabel))
			{
				++cDup;
				sLabel = $"{sBaseLabel} ({cDup})";
			}
			if (string.IsNullOrEmpty(m_current.Param))
			{
				Debug.Assert(m_current.Nodes.Count == 0);
				ltnDup = m_current.CreateCopy();
			}
			else
			{
				ltnDup = DuplicateLayoutSubtree(cDup);
				if (ltnDup == null)
				{
					return;
				}
			}
			ltnDup.Label = sLabel;		// sets Text as well.
			var sDup = ltnDup.DupString;
			sDup = string.IsNullOrEmpty(sDup) ? cDup.ToString() : $"{sDup}-{cDup}";
			ltnDup.DupString = sDup;
			if (m_current.Level == 0)
			{
				var nodes = ((LayoutTypeComboItem)m_cbDictType.SelectedItem).TreeNodes;
				var idx = nodes.IndexOf(m_current);
				nodes.Insert(idx + 1, ltnDup);
				m_tvParts.Nodes.Insert(idx + 1, ltnDup);
			}
			else
			{
				var idx = m_current.Index;
				m_current.Parent.Nodes.Insert(idx + 1, ltnDup);
			}
			ltnDup.Checked = m_current.Checked; // remember this before m_current changes
			m_tvParts.SelectedNode = ltnDup;
		}

		private string GetBaseLabel(IList nodes, List<string> rgsLabels)
		{
			string sBaseLabel = null;
			for (var i = 0; i < nodes.Count; ++i)
			{
				var ltn = (LayoutTreeNode)nodes[i];
				if (ltn.Configuration.Element("ref") == m_current.Configuration.Element("ref") &&
					ltn.LayoutName == m_current.LayoutName &&
					ltn.PartName == m_current.PartName)
				{
					rgsLabels.Add(ltn.Label);
					if (!ltn.IsDuplicate)
					{
						sBaseLabel = ltn.Label;
					}
				}
			}
			return sBaseLabel;
		}

		private LayoutTreeNode DuplicateLayoutSubtree(int iDup)
		{
			var sDupKey = $"{iDup:D2}";

			var suffixCode = $"{LayoutKeyUtils.kcMarkNodeCopy}{sDupKey}";
			var sRef = XmlUtils.GetOptionalAttributeValue(m_current.Configuration, "ref");
			var xnPart = m_parts.GetElement("part", new[] {$"{m_current.ClassName}-Jt-{sRef}"});
			if (xnPart == null)
			{
				return null;		// shouldn't happen.
			}

			var duplicates = new List<XElement>();
			ProcessPartChildrenForDuplication(m_current.ClassName, m_current.Configuration, xnPart.Elements(), suffixCode, duplicates);
			foreach (var xn in duplicates)
			{
				m_layouts.AddNodeToInventory(xn);
			}

			var ltnDup = m_current.CreateCopy();
			AdjustAttributeValue(ltnDup.Configuration, "param", suffixCode);
			ltnDup.Param = AdjustLayoutName(ltnDup.Param, suffixCode);
			if (ltnDup.RelTypeList != null && ltnDup.RelTypeList.Count > 0)
			{
				var repoLexRefType = Cache.ServiceLocator.GetInstance<ILexRefTypeRepository>();
				foreach (var lri in ltnDup.RelTypeList)
				{
					var lrt = repoLexRefType.GetObject(lri.ItemGuid);
					lri.Name = lri.SubClass == TypeSubClass.Reverse ? lrt.ReverseName.BestAnalysisVernacularAlternative.Text : lrt.Name.BestAnalysisVernacularAlternative.Text;
				}
			}
			if (ltnDup.EntryTypeList != null && ltnDup.EntryTypeList.Count > 0)
			{
				var repoPoss = Cache.ServiceLocator.GetInstance<ICmPossibilityRepository>();
				foreach (var info in ltnDup.EntryTypeList)
				{
					if (IsUnspecifiedPossibility(info)) // Not 'real' CmPossibility items
					{
						info.Name = GetUnspecifiedItemName(info);
						continue;
					}
					var poss = repoPoss.GetObject(info.ItemGuid);
					info.Name = poss.Name.BestAnalysisVernacularAlternative.Text;
				}
			}

			if (duplicates.Count > 0)
			{
				LegacyConfigurationUtils.AddChildNodes(duplicates[0], ltnDup, 0, this);
			}
			ltnDup.IsNew = true;
			MarkLayoutTreeNodesAsNew(ltnDup.Nodes.OfType<LayoutTreeNode>());

			return ltnDup;
		}

		private bool IsUnspecifiedPossibility(ItemTypeInfo info)
		{
			return info.ItemGuid == m_unspecComplexFormType || info.ItemGuid == m_unspecVariantType;
		}

		private string GetUnspecifiedItemName(ItemTypeInfo info)
		{
			return info.ItemGuid == m_unspecComplexFormType ? m_noComplexEntryTypeLabel : m_noVariantTypeLabel;
		}

		/// <summary>
		/// Processes any part children for duplication purposes, does nothing on an empty list of elements
		/// </summary>
		private void ProcessPartChildrenForDuplication(string className, XElement xnCaller, IEnumerable<XElement> elements, string suffixCode, List<XElement> duplicates)
		{
			foreach (var xn in elements)
			{
				if (xn.Name.LocalName == "obj" || xn.Name.LocalName == "seq" || xn.Name.LocalName == "objlocal")
				{
					ProcessPartChildForDuplication(className, xnCaller, xn, suffixCode, duplicates);
				}
				else
				{
					ProcessPartChildrenForDuplication(className, xnCaller, xn.Elements(), suffixCode, duplicates);
				}
			}
		}

		private void ProcessPartChildForDuplication(string className, XElement xnCaller, XElement xnField, string suffixCode, List<XElement> duplicates)
		{
			var sLayoutName = XmlUtils.GetMandatoryAttributeValue(xnCaller, "param");
			var fRecurse = XmlUtils.GetOptionalBooleanAttributeValue(xnCaller, "recurseConfig", true);
			if (!fRecurse)
			{
				// We don't want to recurse forever just because senses have subsenses, which
				// can have subsenses, which can ...
				// Or because entries have subentries (in root type layouts)...
				return;
			}
			var sField = XmlUtils.GetMandatoryAttributeValue(xnField, "field");
			var clidDst = 0;
			string sClass = null;
			string sTargetClasses = null;
			try
			{
				// Failure should be fairly unusual, but, for example, part MoForm-Jt-FormEnvPub attempts to display
				// the property PhoneEnv inside an if that checks that the MoForm is one of the subclasses that has
				// the PhoneEnv property. MoForm itself does not.
				var mdc = Cache.GetManagedMetaDataCache();
				if (!mdc.FieldExists(className, sField, true))
				{
					return;
				}
				var flid = Cache.DomainDataByFlid.MetaDataCache.GetFieldId(className, sField, true);
				var type = (CellarPropertyType)Cache.DomainDataByFlid.MetaDataCache.GetFieldType(flid);
				Debug.Assert(type >= CellarPropertyType.MinObj);
				if (type >= CellarPropertyType.MinObj)
				{
					sTargetClasses = XmlUtils.GetOptionalAttributeValue(xnField, "targetclasses");
					clidDst = m_mdc.GetDstClsId(flid);
					sClass = clidDst == 0 ? XmlUtils.GetOptionalAttributeValue(xnField, "targetclass") : m_mdc.GetClassName(clidDst);
					if (clidDst == StParaTags.kClassId)
					{
						var sClassT = XmlUtils.GetOptionalAttributeValue(xnField, "targetclass");
						if (!string.IsNullOrEmpty(sClassT))
						{
							sClass = sClassT;
						}
					}
				}
			}
			catch
			{
				return;
			}
			if (clidDst == MoFormTags.kClassId && !sLayoutName.StartsWith("publi"))
				return;	// ignore the layouts used by the LexEntry-Jt-Headword part.
			if (string.IsNullOrEmpty(sLayoutName) || string.IsNullOrEmpty(sClass))
			{
				return;
			}

			if (sTargetClasses == null)
			{
				sTargetClasses = sClass;
			}
			var rgsClasses = sTargetClasses.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
			XElement xnLayout = null;
			if (rgsClasses.Length > 0)
			{
				xnLayout = m_layouts.GetElement("layout", new[] { rgsClasses[0], "jtview", sLayoutName, null });
			}

			if (xnLayout != null)
			{
				var cNodes = xnLayout.Elements().Count();
				DuplicateLayout(xnLayout.Clone(), suffixCode, duplicates);
				var fRepeatedConfig = XmlUtils.GetOptionalBooleanAttributeValue(xnField, "repeatedConfig", false);
				if (fRepeatedConfig)
				{
					return;		// repeats an earlier part element (probably as a result of <if>s)
				}
				for (var i = 1; i < rgsClasses.Length; i++)
				{
					var xnMergedLayout = m_layouts.GetElement("layout", new[] { rgsClasses[i], "jtview", sLayoutName, null });
					if (xnMergedLayout != null && xnMergedLayout.Elements().Count() == cNodes)
					{
						DuplicateLayout(xnMergedLayout.Clone(), suffixCode, duplicates);
					}
				}
			}
			else
			{
				// The "layout" in a part node can actually refer directly to another part, so check
				// for that possibility.
				var subPart = m_parts.GetElement("part", new[] { rgsClasses[0] + "-Jt-" + sLayoutName });
				if (subPart == null && !sLayoutName.EndsWith("-en"))
				{
					// Complain if we can't find either a layout or a part, and the name isn't tagged
					// for a writing system.  (We check only for English, being lazy.)
					var msg = $"Missing jtview layout for class=\"{rgsClasses[0]}\" name=\"{sLayoutName}\"";
					Debug.Assert(xnLayout != null, msg);
				}
			}
		}

		/// <summary>
		/// Fix the cloned layout for use as a duplicate.
		/// </summary>
		/// <param name="xnLayout">an unmodified clone of the original node</param>
		/// <param name="suffixCode">tag to append to the name attribute, and param attributes of child nodes</param>
		/// <param name="duplicates">list to add the modified layout to</param>
		private void DuplicateLayout(XElement xnLayout, string suffixCode, List<XElement> duplicates)
		{
			// It is important for at least one caller that the FIRST node added to duplicates is the copy of xnLayout.
			duplicates.Add(xnLayout);
			AdjustAttributeValue(xnLayout, "name", suffixCode);
			var className = XmlUtils.GetMandatoryAttributeValue(xnLayout, "class");

			foreach (var partref in xnLayout.Elements())
			{
				if (partref.Name.LocalName == "part")
				{
					var param = XmlUtils.GetOptionalAttributeValue(partref, "param");
					if (string.IsNullOrEmpty(param))
					{
						continue;
					}
					var sRef = XmlUtils.GetMandatoryAttributeValue(partref, "ref");
					var xnPart = m_parts.GetElement("part", new[] {$"{className}-Jt-{sRef}"});
					if (xnPart == null)
					{
						Debug.Assert(xnPart != null, $@"XMLDocConfigure::DuplicateLayout - Failed to process {className}-Jt-{sRef} likely an invalid layout.");
					}
					else
					{
						ProcessPartChildrenForDuplication(className, partref, xnPart.Elements(), suffixCode, duplicates);
					}
					AdjustAttributeValue(partref, "param", suffixCode);
				}
				else if (partref.Name == "sublayout")
				{
					var sublayout = XmlUtils.GetMandatoryAttributeValue(partref, "name");
					var xnSublayout = m_layouts.GetElement("layout", new[] {className, "jtview", sublayout, null});
					DuplicateLayout(xnSublayout.Clone(), suffixCode, duplicates);
					AdjustAttributeValue(partref, "name", suffixCode);
				}
			}
		}

		private static void AdjustAttributeValue(XElement node, string sAttrName, string suffixCode)
		{
			var xa = node.HasAttributes ? node.Attribute(sAttrName) : null;
			Debug.Assert(xa != null);
			xa.Value = AdjustLayoutName(xa.Value, suffixCode);
		}

		private static string AdjustLayoutName(string sName, string suffixCode)
		{
			Debug.Assert(!string.IsNullOrEmpty(sName));
			var cTag = suffixCode[0];
			int idx;
			if (cTag == LayoutKeyUtils.kcMarkNodeCopy)
			{
				var idx0 = suffixCode.IndexOf(LayoutKeyUtils.kcMarkLayoutCopy);
				if (idx0 < 0)
				{
					idx0 = 0;
				}
				idx = sName.IndexOf(cTag, idx0);
			}
			else
			{
				idx = sName.IndexOf(cTag);
			}

			if (idx > 0)
			{
				sName = sName.Remove(idx);
			}
			return $"{sName}{suffixCode}";
		}

		private void m_btnRemove_Click(object sender, EventArgs e)
		{
			Debug.Assert(m_current.IsDuplicate);
			// REVIEW:  Should we have a user prompt?
			if (m_current.Level == 0)
			{
				var nodes = ((LayoutTypeComboItem)m_cbDictType.SelectedItem).TreeNodes;
				nodes.Remove(m_current);
				m_tvParts.Nodes.Remove(m_current);
			}
			else
			{
				m_current.Parent.Nodes.Remove(m_current);
			}
		}

		private void m_btnRestoreDefaults_Click(object sender, EventArgs e)
		{
			using (new WaitCursor(this))
			{
				LayoutCache.InitializePartInventories(null, m_propertyTable.GetValue<IApp>("App").ApplicationName, Cache.ProjectId.ProjectFolder);
				var layouts = Inventory.GetInventory("layouts", null);
				var parts = Inventory.GetInventory("parts", null);
				//preserve layouts which are marked as copies
				var layoutTypes = m_layouts.GetLayoutTypes();
				var layoutCopies = m_layouts.GetElements("//layout[contains(@name, '#')]");
				foreach (var type in layoutTypes)
				{
					layouts.AddLayoutTypeToInventory(type);
				}
				foreach (var layoutCopy in layoutCopies)
				{
					var copy = layoutCopy;
					layouts.AddNodeToInventory(copy);
				}
				m_layouts = layouts;
				m_parts = parts;
				// recreate the layout trees.
				var selItem = m_cbDictType.SelectedItem as LayoutTypeComboItem;
				var selLabel = selItem?.Label;
				var selLayout = selItem?.LayoutName;
				m_cbDictType.Items.Clear();
				m_tvParts.Nodes.Clear();
				var configureLayouts = XmlUtils.FindElement(m_configurationParameters, "configureLayouts");
				LegacyConfigurationUtils.BuildTreeFromLayoutAndParts(configureLayouts, this);
				if (selItem != null)
				{
					// try to select the same view.
					foreach (var item in m_cbDictType.Items)
					{
						var ltci = item as LayoutTypeComboItem;
						if (ltci == null || ltci.Label != selLabel || ltci.LayoutName != selLayout)
						{
							continue;
						}
						m_cbDictType.SelectedItem = ltci;
						break;
					}
				}
				m_fDeleteCustomFiles = true;
			}
		}

		private void m_btnOK_Click(object sender, EventArgs e)
		{
			if (IsDirty())
			{
				m_propertyTable.SetProperty(m_sLayoutPropertyName, ((LayoutTypeComboItem)m_cbDictType.SelectedItem).LayoutName, SettingsGroup.LocalSettings, true, true);
				SaveModifiedLayouts();
				DialogResult = DialogResult.OK;
			}
			else
			{
				DialogResult = DialogResult.Cancel;
			}
			Close();
		}

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"), m_helpTopicID);
		}

		/// <summary>
		/// Fire up the Styles dialog, and if necessary, reload the related combobox.
		/// </summary>
		private void m_btnStyles_Click(object sender, EventArgs e)
		{
			HandleStylesBtn(m_cbCharStyle, null, string.Empty);
		}

		/// <summary>
		/// Fire up the Styles dialog, and if necessary, reload the related combobox.
		/// </summary>
		private void m_btnBeforeStyles_Click(object sender, EventArgs e)
		{
			HandleStylesBtn(m_cbBeforeStyle, null, string.Empty);
		}

		/// <summary>
		/// Returns the name of the style the user would like to select (null or empty if canceled).
		/// </summary>
		private void HandleStylesBtn(ComboBox combo, Action fixCombo, string defaultStyle)
		{
			FwStylesDlg.RunStylesDialogForCombo(combo,
				() =>
				{
					// Reload the lists for the styles combo boxes, and redisplay the controls.
					SetStylesLists();
					if (m_btnStyles.Visible)
						DisplayStyleControls(m_current.AllParentsChecked);
					if (m_btnBeforeStyles.Visible)
						DisplayBeforeStyleControls(m_current.AllParentsChecked);
					fixCombo?.Invoke();
				},
				defaultStyle, m_styleSheet,
#if RANDYTODO
				m_mainWindow != null ? m_mainWindow.MaxStyleLevelToShow : 0,
				m_mainWindow != null ? m_mainWindow.HvoAppRootObject : 0,
#else
				0, 0,
#endif
				Cache, this, m_propertyTable.GetValue<IApp>("App"),
				m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"),
				(new FlexStylesXmlAccessor(Cache.LanguageProject.LexDbOA)).SetPropsToFactorySettings);
		}

		/// <summary>
		/// Move to the first child node when this "Configure Now" link text is clicked.
		/// </summary>
		private void m_lnkConfigureNow_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			Debug.Assert(m_current != null && m_current.Nodes.Count > 0);
			m_tvParts.SelectedNode = m_current.Nodes[0];
		}

		/// <summary>
		/// Don't allow the top-level nodes to be unchecked!  Also, tie the check boxes in
		/// the tree view to the "Display Data" checkbox if the user clicks on the tree view
		/// while that node is the current one displayed in detail.
		/// </summary>
		/// <remarks>LT-10472 says that not to restrict checkmarks for level 0.</remarks>
		void m_tvParts_AfterCheck(object sender, TreeViewEventArgs e)
		{
			if (e.Node == m_current)
			{
				m_chkDisplayData.Checked = e.Node.Checked;
				// Re-enable/disable the controls in the detail pane.
				if (m_current.IsTopLevel)
				{
					return;
				}
				if (m_current.Nodes.Count > 0)
				{
					DisplayDetailsForAParentNode(m_current.AllParentsChecked);
				}
				else if (m_current.UseParentConfig)
				{
					DisplayDetailsForRecursiveNode(m_current.AllParentsChecked);
				}
				else
				{
					DisplayDetailsForLeafNode(m_current.AllParentsChecked);
				}
				DisplayBeforeStyleControls(m_current.AllParentsChecked);
			}
			else
			{
				if (m_current.IsDescendedFrom(e.Node))
				{
					DisplayCurrentNodeDetails();
				}
				MakeParentParaIfDivInParaVisible((LayoutTreeNode)e.Node);
			}
		}

		/// <summary>
		/// Tie the "Display Data" checkbox to the corresponding checkbox in the treeview.
		/// </summary>
		private void m_chkDisplayData_CheckedChanged(object sender, EventArgs e)
		{
			if (m_current != null)
			{
				m_current.Checked = m_chkDisplayData.Checked;
			}
		}

		void m_chkComplexFormsAsParagraphs_CheckedChanged(object sender, EventArgs e)
		{
			if (m_current == null)
			{
				return; // Are complex forms to be shown in their own line?
			}
			var fChecked = m_chkComplexFormsAsParagraphs.Checked;
			m_current.ShowComplexFormPara = fChecked;
			ShowComplexFormRelatedControls(fChecked);
			if (!fChecked)
			{
				return; // All Senses must now be shown in separate paragraphs
			}
			var ltnParent = m_current.Parent as LayoutTreeNode;
			if (ltnParent != null)
			{
				ltnParent.ShowSenseAsPara = true;
			}
		}
		private ListViewContent m_listType = ListViewContent.Hidden;

		private void OnListItemsSelectedIndexChanged(object sender, EventArgs e)
		{
			var sic = m_lvItems.SelectedIndices;
			if (sic.Count != 1)
			{
				m_btnMoveItemUp.Enabled = false;
				m_btnMoveItemDown.Enabled = false;
				return;
			}
			switch (m_listType)
			{
				case ListViewContent.WritingSystems:
					m_btnMoveItemUp.Enabled = GetIndexToMoveWsUp(sic) > 0;
					m_btnMoveItemDown.Enabled = GetIndexToMoveWsDown(sic) < m_lvItems.Items.Count - 1;
					break;
				case ListViewContent.RelationTypes:
				case ListViewContent.ComplexFormTypes:
				case ListViewContent.VariantTypes:
				case ListViewContent.MinorEntryTypes:
					m_btnMoveItemUp.Enabled = GetIndexToMoveTypeUp(sic) > 0;
					m_btnMoveItemDown.Enabled = GetIndexToMoveTypeDown(sic) < m_lvItems.Items.Count - 1;
					break;
			}
		}

		void OnMoveItemUpClick(object sender, EventArgs e)
		{
			var sic = m_lvItems.SelectedIndices;
			int idx;
			switch (m_listType)
			{
				case ListViewContent.WritingSystems:
					idx = GetIndexToMoveWsUp(sic);
					MoveListItem(idx, Math.Max(idx - 1, 0));
					break;
				case ListViewContent.RelationTypes:
					idx = GetIndexToMoveTypeUp(sic);
					MoveListItem(idx, Math.Max(idx - 1, 0));
					MoveRelationTypeListItem(idx, Math.Max(idx - 1, 0));
					break;
				case ListViewContent.ComplexFormTypes:
				case ListViewContent.VariantTypes:
				case ListViewContent.MinorEntryTypes:
					idx = GetIndexToMoveTypeUp(sic);
					MoveListItem(idx, Math.Max(idx - 1, 0));
					MoveEntryTypeListItem(idx, Math.Max(idx - 1, 0));
					break;
			}
		}

		void OnMoveItemDownClick(object sender, EventArgs e)
		{
			var sic = m_lvItems.SelectedIndices;
			int idx;
			switch (m_listType)
			{
				case ListViewContent.WritingSystems:
					idx = GetIndexToMoveWsDown(sic);
					MoveListItem(idx, Math.Min(idx + 1, m_lvItems.Items.Count - 1));
					break;
				case ListViewContent.RelationTypes:
					idx = GetIndexToMoveTypeDown(sic);
					MoveListItem(idx, Math.Min(idx + 1, m_lvItems.Items.Count - 1));
					MoveRelationTypeListItem(idx, Math.Min(idx + 1, m_lvItems.Items.Count - 1));
					break;
				case ListViewContent.ComplexFormTypes:
				case ListViewContent.VariantTypes:
				case ListViewContent.MinorEntryTypes:
					idx = GetIndexToMoveTypeDown(sic);
					MoveListItem(idx, Math.Min(idx + 1, m_lvItems.Items.Count - 1));
					MoveEntryTypeListItem(idx, Math.Min(idx + 1, m_lvItems.Items.Count - 1));
					break;
			}
		}

		private int GetIndexToMoveWsUp(ListView.SelectedIndexCollection sic)
		{
			var idx = sic[0];
			if (idx == 0)
			{
				return 0;
			}
			var tagPrev = m_lvItems.Items[idx - 1].Tag;
			return tagPrev is ILgWritingSystem ? idx : 0;
		}

		private int GetIndexToMoveWsDown(ListView.SelectedIndexCollection sic)
		{
			var idxMax = m_lvItems.Items.Count - 1;
			var idx = sic[0];
			if (idx >= idxMax)
			{
				return idxMax;
			}
			var tagSel = m_lvItems.Items[idx].Tag;
			return tagSel is ILgWritingSystem ? idx : idxMax;
		}

		private int GetIndexToMoveTypeUp(ListView.SelectedIndexCollection sic)
		{
			var idxMax = m_lvItems.Items.Count - 1;
			var idx = sic[0];
			return idx >= idxMax ? idxMax : idx;
		}

		private int GetIndexToMoveTypeDown(ListView.SelectedIndexCollection sic)
		{
			var idxMax = m_lvItems.Items.Count - 1;
			var idx = sic[0];
			return idx >= idxMax ? idxMax : idx;
		}

		private void MoveRelationTypeListItem(int idx, int idxNew)
		{
			var info = m_current.RelTypeList[idx];
			m_current.RelTypeList.RemoveAt(idx);
			m_current.RelTypeList.Insert(idxNew, info);
		}

		private void MoveEntryTypeListItem(int idx, int idxNew)
		{
			var info = m_current.EntryTypeList[idx];
			m_current.EntryTypeList.RemoveAt(idx);
			m_current.EntryTypeList.Insert(idxNew, info);
		}

		void OnListItemsItemCheck(object sender, ItemCheckEventArgs e)
		{
			switch (m_listType)
			{
				case ListViewContent.WritingSystems:
					OnWritingSystemsItemCheck(e);
					break;
				case ListViewContent.RelationTypes:
				case ListViewContent.ComplexFormTypes:
				case ListViewContent.VariantTypes:
				case ListViewContent.MinorEntryTypes:
					OnItemTypeItemCheck(e);
					break;
			}
		}

		/// <summary>
		/// We are messing with the checkmarks while processing a checkmark change, and don't
		/// want the cascading recursive processing to occur.  Hence this variable...
		/// </summary>
		bool m_fItemCheck;

		/// <summary>
		/// We need to enforce two rules:
		/// 1. At least one item must always be checked.
		/// 2. If a magic ("Default Analysis" etc) item is checked, then it is the only one that can be checked.
		/// </summary>
		void OnWritingSystemsItemCheck(ItemCheckEventArgs e)
		{
			if (m_fItemCheck)
			{
				return;
			}
			try
			{
				m_fItemCheck = true;

				int cChecks;
				var lvic = m_lvItems.CheckedItems;
				if (lvic.Count == 0)
				{
					// I don't think this can happen, but just in case...
					if (e.NewValue == CheckState.Unchecked)
					{
						e.NewValue = CheckState.Checked;
					}
					return;
				}
				var lviEvent = m_lvItems.Items[e.Index];
				if (e.NewValue == CheckState.Checked)
				{
					cChecks = lvic.Count + 1;
					for (var i = lvic.Count - 1; i >= 0; --i)
					{
						if (lviEvent.Tag is ILgWritingSystem)
						{
							if (!(lvic[i].Tag is int))
							{
								continue;
							}
							lvic[i].Checked = false;	// Uncheck any magic ws items.
							--cChecks;
						}
						else
						{
							if (lvic[i].Index == e.Index)
							{
								continue;
							}
							lvic[i].Checked = false;	// uncheck any other items.
							--cChecks;
						}
					}
				}
				else
				{
					cChecks = lvic.Count - 1;
					// Can't uncheck the last remaining checked item! (rule 1 above)
					if (lvic.Count == 1 && lvic[0].Index == e.Index)
					{
						e.NewValue = CheckState.Checked;
						++cChecks;
					}
				}
				m_chkDisplayWsAbbrs.Enabled = cChecks > 1;
			}
			finally
			{
				m_fItemCheck = false;
			}
		}

		/// <summary>
		/// We need to enforce one rule: at least one item must always be checked.
		/// </summary>
		private void OnItemTypeItemCheck(ItemCheckEventArgs e)
		{
			if (m_fItemCheck)
			{
				return;
			}
			try
			{
				m_fItemCheck = true;
				int cChecks;
				var lvic = m_lvItems.CheckedItems;
				if (lvic.Count == 0)
				{
					// I don't think this can happen, but just in case...
					if (e.NewValue == CheckState.Unchecked)
					{
						e.NewValue = CheckState.Checked;
					}
				}
				else
				{
					if (e.NewValue == CheckState.Unchecked)
					{
						cChecks = lvic.Count - 1;
						// Can't uncheck the last remaining checked item! (only rule)
						if (lvic.Count == 1 && lvic[0].Index == e.Index)
						{
							e.NewValue = CheckState.Checked;
							++cChecks;
						}
					}
				}
				switch (m_listType)
				{
					case ListViewContent.RelationTypes:
						m_current.RelTypeList[e.Index].Enabled = (e.NewValue == CheckState.Checked);
						break;
					case ListViewContent.ComplexFormTypes:
					case ListViewContent.VariantTypes:
					case ListViewContent.MinorEntryTypes:
						m_current.EntryTypeList[e.Index].Enabled = (e.NewValue == CheckState.Checked);
						break;
				}
			}
			finally
			{
				m_fItemCheck = false;
			}
		}

		private void m_tvParts_AfterSelect(object sender, TreeViewEventArgs e)
		{
			// Save the data for the old node before displaying the data for the new node.
			if (m_current != null && !m_current.IsTopLevel)
			{
				StoreNodeData(m_current);
			}

			// Set up the dialog for editing the current node's layout.
			m_current = e.Node as LayoutTreeNode;
			DisplayCurrentNodeDetails();
		}

		private void DisplayCurrentNodeDetails()
		{
			var fEnabled = true;
			var sHeading = m_current.Text;
			var tnParent = m_current.Parent;
			if (tnParent != null)
			{
				fEnabled = m_current.AllParentsChecked;
				sHeading = string.Format(AreaResources.ksHierarchyLabel, tnParent.Text, sHeading);
			}
			// This is kind of kludgy but it's the best I can think of.
			var partName = m_current.PartName.ToLowerInvariant();
			m_linkConfigureHomograph.Visible = partName.Contains("headword") || partName.Contains("owneroutline");
			// Use the text box if the label is too wide. See LT-9281.
			m_lblPanel.Text = sHeading;
			m_tbPanelHeader.Text = sHeading;
			if (m_lblPanel.Location.X + m_lblPanel.PreferredWidth > panel1.ClientSize.Width - (panel1.Margin.Left + panel1.Margin.Right))
			{
				m_lblPanel.Enabled = false;
				m_lblPanel.Visible = false;
				m_tbPanelHeader.Text = tnParent != null ? string.Format(AreaResources.ksHierarchyLabel2, tnParent.Text, m_current.Text) : sHeading;
				m_tbPanelHeader.Enabled = true;
				m_tbPanelHeader.Visible = true;
			}
			else
			{
				m_lblPanel.Enabled = true;
				m_lblPanel.Visible = true;
				m_tbPanelHeader.Enabled = false;
				m_tbPanelHeader.Visible = false;
			}
			if (m_current.IsTopLevel)
			{
				DisplayDetailsForTopLevelNode();
				m_btnMoveUp.Enabled = false;
				m_btnMoveDown.Enabled = false;
				m_btnDuplicate.Enabled = false;
				m_btnRemove.Enabled = false;
			}
			else
			{
				// The "Display Data" control is common to all non-toplevel nodes.
				m_chkDisplayData.Enabled = !m_current.IsRequired && fEnabled;
				m_chkDisplayData.Checked = m_current.Checked;
				if (m_current.Nodes.Count > 0)
				{
					DisplayDetailsForAParentNode(fEnabled);
				}
				else
				{
					if (m_current.UseParentConfig)
					{
						DisplayDetailsForRecursiveNode(fEnabled);
					}
					else
					{
						DisplayDetailsForLeafNode(fEnabled);
					}
				}
				DisplayControlsForAnyNode(fEnabled);
				m_btnMoveUp.Enabled = EnableMoveUpButton();
				m_btnMoveDown.Enabled = EnableMoveDownButton();
				m_btnDuplicate.Enabled = true;
				m_btnRemove.Enabled = m_current.IsDuplicate;
			}
		}

		// ReSharper restore InconsistentNaming
		#endregion // Dialog Event Handlers

		#region Misc internal functions

		private void MoveListItem(int idx, int newIndex)
		{
			// This Select() call should be here, not after the
			// ListViewItem.set_Selected(bool) call near the end of the method,
			// in order to prevent focus from switching to m_cbCharStyle
			m_lvItems.Select();
			if (idx == newIndex)
			{
				return;
			}
			Debug.Assert((newIndex >= 0) && (newIndex < m_lvItems.Items.Count));
			var ltiSel = m_lvItems.Items[idx];
			var itemSelected = ltiSel.Selected;
			var lti = (ListViewItem) ltiSel.Clone();
			m_lvItems.BeginUpdate();
			m_lvItems.Items.RemoveAt(idx);
			m_lvItems.Items.Insert(newIndex, lti);
			if (itemSelected)
			{
				lti.Selected = true;
			}
			m_lvItems.EndUpdate();
		}

		/// <summary>
		/// Generates a possible help topic id from an identifying string, but does NOT check it for validity!
		/// </summary>
		/// <returns></returns>
		private string generateChooserHelpTopicID(string fromStr)
		{
			var candidateID = "khtpConfig";

			// Should we capitalize the next letter?
			var nextCapital = true;

			// Lets turn our field into a candidate help page!
			foreach (var ch in fromStr)
			{
				if (IsLetterOrDigit(ch)) // might we include numbers someday?
				{
					if (nextCapital)
					{
						candidateID += ToUpper(ch);
					}
					else
					{
						candidateID += ch;
					}
					nextCapital = false;
				}
				else // unrecognized character... exclude it
				{
					nextCapital = true; // next letter should be a capital
				}
			}

			return candidateID;
		}

		/// <summary>
		/// The MoveUp button is enabled only when the current node is not the first child of
		/// its parent tree node, and the preceding node comes from the same actual layout.
		/// (Remember, the parts from a sublayout node appear as children of the the same tree
		/// node as any parts from the sublayout node appear as children of the the same tree
		/// node as any parts from the layout containing the sublayout.)
		/// </summary>
		/// <returns></returns>
		private bool EnableMoveUpButton()
		{
			if (m_current.Level == 0)
			{
				return false;
			}
			var idx = m_current.Index;
			if (idx <= 0)
			{
				return false;
			}
			var xnParent = m_current.HiddenNodeLayout ?? m_current.ParentLayout;
			var ltnPrev = (LayoutTreeNode)m_current.Parent.Nodes[idx-1];
			var xnPrevParent = ltnPrev.HiddenNodeLayout ?? ltnPrev.ParentLayout;
			return xnParent == xnPrevParent;
		}

		/// <summary>
		/// The MoveDown button is enabled only when the current node is not the last child of
		/// its parent tree node, and the following node comes from the same actual layout.
		/// (Remember, the parts from a sublayout node appear as children of the the same tree
		/// node as any parts from the layout containing the sublayout.)
		/// </summary>
		private bool EnableMoveDownButton()
		{
			if (m_current.Level == 0)
			{
				return false;
			}
			var idx = m_current.Index;
			if (idx >= m_current.Parent.Nodes.Count - 1)
			{
				return false;
			}
			var xnParent = m_current.HiddenNodeLayout ?? m_current.ParentLayout;
			var ltnNext = (LayoutTreeNode)m_current.Parent.Nodes[idx+1];
			var xnNextParent = ltnNext.HiddenNodeLayout ?? ltnNext.ParentLayout;
			return xnParent == xnNextParent;
		}

		private void StoreNodeData(LayoutTreeNode ltn)
		{
			ltn.ContentVisible = ltn.Checked;
			if (m_tbBefore.Visible && m_tbBefore.Enabled)
			{
				ltn.Before = m_tbBefore.Text;
			}
			else
			{
				ltn.Before = string.Empty; // if it's invisible don't let any non-empty value be saved.
			}

			if (m_tbBetween.Visible && m_tbBetween.Enabled)
			{
				ltn.Between = m_tbBetween.Text;
			}
			else
			{
				ltn.Between = string.Empty; // if it's invisible don't let any non-empty value be saved.
			}

			if (m_tbAfter.Visible && m_tbAfter.Enabled)
			{
				ltn.After = m_tbAfter.Text;
			}
			else
			{
				ltn.After = string.Empty; // if it's invisible don't let any non-empty value be saved.
			}

			if (m_chkDisplayWsAbbrs.Visible)
			{
				ltn.ShowWsLabels = m_chkDisplayWsAbbrs.Checked && m_chkDisplayWsAbbrs.Enabled;
			}

			if (m_cfgSenses.Visible)
			{
				StoreSenseConfigData(ltn);
			}

			if (m_chkShowSingleGramInfoFirst.Visible)
			{
				StoreGramInfoData(ltn);
			}

			if (m_chkComplexFormsAsParagraphs.Visible)
			{
				StoreComplexFormData(ltn);
			}
			if (m_cbCharStyle.Visible && m_cbCharStyle.Enabled)
			{
				var sci = m_cbCharStyle.SelectedItem as StyleComboItem;
				ltn.StyleName = sci?.Style != null ? sci.Style.Name : string.Empty;
			}
			if (m_cbBeforeStyle.Visible && m_cbBeforeStyle.Enabled)
			{
				var sci = m_cbBeforeStyle.SelectedItem as StyleComboItem;
				ltn.BeforeStyleName = sci?.Style != null ? sci.Style.Name : String.Empty;
			}

			if (m_lvItems.Visible && m_lvItems.Enabled)
			{
				ltn.WsLabel = GenerateWsLabelFromListView();
			}
			MakeParentParaIfDivInParaVisible(ltn);
		}

		private void MakeParentParaIfDivInParaVisible(LayoutTreeNode ltn)
		{
			if (!ltn.Checked || ltn.FlowType != "divInPara")
			{
				return;
			}
			var ltnParent = ltn.Parent as LayoutTreeNode;
			if (ltnParent == null)
			{
				return;
			}
			ltnParent.ShowSenseAsPara = true;
			if (ltnParent == m_current)
			{
				DisplayCurrentNodeDetails();
			}
		}

		private void MakeDivInParaChildNotVisible(LayoutTreeNode ltn, object sender)
		{
			// applies to Root dictionary ltn=Senses when ShowSenseAsPara=false
			var lts = sender as ConfigSenseLayout;
			if (lts == null)
			{
				return;
			}

			if (lts.DisplaySenseInPara)
			{
				return;
			}
			// find Stem: Main Entry-Senses-Visible Complex Forms ltn
			foreach (TreeNode n in ltn.Nodes) // iterate over child nodes
			{
				var tn = n as LayoutTreeNode;
				if (tn == null)
				{
					continue;
				}
				// meant for tn.Label == "Subentries" in root dictionary
				if (tn.Checked && tn.FlowType == "divInPara")
				{
					MessageBox.Show(AreaResources.ksRootSenseOnSubentriesGoneDlgText, AreaResources.ksRootSenseOnSubentriesGoneDlgLabel, MessageBoxButtons.OK, MessageBoxIcon.Information);
					tn.Checked = false;
					break;
				}
				// meant for tn.Label.Contains("Referenced Complex Form") in stem dictionary
				if (tn.Checked && tn.FlowType == "span" && tn.Label.Contains(AreaResources.ksReferencedComplexForm) && tn.ShowComplexFormPara)
				{
					tn.ShowComplexFormPara = false;
					break;
				}
			}
		}

		private string GenerateWsLabelFromListView()
		{
			var sbLabel = new StringBuilder();
			foreach (ListViewItem lvi in m_lvItems.CheckedItems)
			{
				var sWs = string.Empty;
				if (lvi.Tag is int)
				{
					var ws = (int)lvi.Tag;
					WritingSystemServices.SmartMagicWsToSimpleMagicWs(ws);
					sWs = WritingSystemServices.GetMagicWsNameFromId(ws);
				}
				else if (lvi.Tag is CoreWritingSystemDefinition)
				{
					sWs = ((CoreWritingSystemDefinition) lvi.Tag).Id;
				}

				if (sbLabel.Length > 0)
				{
					sbLabel.Append(",");
				}
				sbLabel.Append(sWs);
			}
			return sbLabel.ToString();
		}

		private void StoreSenseConfigData(LayoutTreeNode ltn)
		{
			if (m_cfgSenses.NumberStyleCombo.SelectedIndex == 0)
			{
				ltn.Number = string.Empty;
			}
			else
			{
				ltn.Number = m_cfgSenses.BeforeNumber + ((NumberingStyleComboItem)m_cfgSenses.NumberStyleCombo.SelectedItem).FormatString + m_cfgSenses.AfterNumber;
				ltn.NumStyle = GenerateNumStyleFromCheckBoxes();
				ltn.NumFont = m_cfgSenses.NumberFontCombo.SelectedItem.ToString();	// item is a string actually...
				if (ltn.NumFont == AreaResources.ksUnspecified)
				{
					ltn.NumFont = string.Empty;
				}
				ltn.NumberSingleSense = m_cfgSenses.NumberSingleSense;
			}
			ltn.ShowSingleGramInfoFirst = m_chkShowSingleGramInfoFirst.Checked;
			// Set the information on the child grammatical info node as well.
			foreach (TreeNode n in ltn.Nodes)
			{
				var tn = n as LayoutTreeNode;
				if (tn == null || !tn.ShowGramInfoConfig)
				{
					continue;
				}
				tn.ShowSingleGramInfoFirst = m_chkShowSingleGramInfoFirst.Checked;
				break;
			}
			ltn.ShowSenseAsPara = m_cfgSenses.DisplaySenseInPara;
			ltn.SenseParaStyle = m_cfgSenses.SenseParaStyle;
		}

		private void StoreGramInfoData(LayoutTreeNode ltn)
		{
			if (m_cfgSenses.Visible)
			{
				return;
			}
			ltn.ShowSingleGramInfoFirst = m_chkShowSingleGramInfoFirst.Checked;
			// Set the information on the parent sense node as well.
			var ltnParent = ltn.Parent as LayoutTreeNode;
			if (ltnParent != null && ltnParent.ShowSenseConfig)
			{
				ltnParent.ShowSingleGramInfoFirst = m_chkShowSingleGramInfoFirst.Checked;
			}
		}

		private void StoreComplexFormData(LayoutTreeNode ltn)
		{
			ltn.ShowComplexFormPara = m_chkComplexFormsAsParagraphs.Checked;
		}

		private string GenerateNumStyleFromCheckBoxes()
		{
			var sbNumStyle = new StringBuilder();
			switch (m_cfgSenses.BoldSenseNumber)
			{
				case CheckState.Checked:
					sbNumStyle.Append("bold");
					break;
				case CheckState.Unchecked:
					sbNumStyle.Append("-bold");
					break;
			}
			switch (m_cfgSenses.ItalicSenseNumber)
			{
				case CheckState.Checked:
					if (sbNumStyle.Length > 0)
						sbNumStyle.Append(" ");
					sbNumStyle.Append("italic");
					break;
				case CheckState.Unchecked:
					if (sbNumStyle.Length > 0)
						sbNumStyle.Append(" ");
					sbNumStyle.Append("-italic");
					break;
			}
			return sbNumStyle.ToString();
		}

		private void DisplayDetailsForTopLevelNode()
		{
			// Set up the details for top-level nodes.
			m_chkDisplayData.Checked = m_current.Checked;	// now layout attribute dependant
			m_chkDisplayData.Enabled = true;

			m_lblBefore.Visible = false;
			m_lblBetween.Visible = false;
			m_lblAfter.Visible = false;
			m_lblContext.Visible = false;
			m_tbBefore.Visible = false;
			m_tbBetween.Visible = false;
			m_tbAfter.Visible = false;
			m_chkDisplayWsAbbrs.Visible = false;

			HideSenseConfigControls();
			HideGramInfoConfigControls();
			HideComplexFormConfigControls();

			DisplayDetailsForAParentNode(true);	// A top-level node is also a parent node!
		}

		private void DisplayControlsForAnyNode(bool fEnabled)
		{
			if (m_current.ShowSenseConfig)
			{
				DisplaySenseConfigControls(fEnabled);
			}
			else
			{
				HideSenseConfigControls();
			}

			if (m_current.ShowGramInfoConfig)
			{
				DisplayGramInfoConfigControls(fEnabled);
			}
			else
			{
				HideGramInfoConfigControls();
			}

			if (m_current.ShowComplexFormParaConfig)
			{
				DisplayComplexFormConfigControls(fEnabled);
			}
			else
			{
				HideComplexFormConfigControls();
			}

			DisplayBeforeStyleControls(fEnabled);
		}

		private void DisplayDetailsForAParentNode(bool fEnabled)
		{
			// Set up the details for nodes with subitems.
			m_chkDisplayWsAbbrs.Visible = false;
			m_lblItemsList.Visible = false;
			m_lvItems.Visible = false;
			m_btnMoveItemUp.Visible = false;
			m_btnMoveItemDown.Visible = false;
			m_lblCharStyle.Visible = false;
			m_cbCharStyle.Visible = false;
			m_btnStyles.Visible = false;

			DisplayTypeControls(fEnabled);
			DisplayContextControls(fEnabled);
			DisplayStyleControls(fEnabled);

			var sMoreDetail = string.Format(AreaResources.ksCanBeConfiguredInMoreDetail, m_current.Text);
			m_cfgParentNode.Visible = true;
			m_cfgParentNode.SetDetails(sMoreDetail, m_chkDisplayData.Checked && fEnabled, true);
			if (!m_chkDisplayData.Checked || !fEnabled)
			{
				return;
			}
			m_current.Expand();
			foreach (LayoutTreeNode child in m_current.Nodes)
			{
				if (XmlUtils.GetOptionalBooleanAttributeValue(child.Configuration, "autoexp", false))
				{
					child.Expand();
				}
			}
		}

		private void DisplayDetailsForRecursiveNode(bool fEnabled)
		{
			m_lblItemsList.Visible = false;
			m_lvItems.Visible = false;
			m_btnMoveItemUp.Visible = false;
			m_btnMoveItemDown.Visible = false;
			m_lblCharStyle.Visible = false;
			m_cbCharStyle.Visible = false;
			m_btnStyles.Visible = false;
			m_chkDisplayWsAbbrs.Visible = false;

			DisplayContextControls(fEnabled);
			if (m_current.AllowDivParaStyle)
			{
				DisplayStyleControls(fEnabled);
			}

			var sMoreDetail = string.Format(LanguageExplorerResources.ksUsesTheSameConfigurationAs, m_current.Text, m_current.Configuration.Attribute("recurseConfigLabel") != null ? m_current.Configuration.Attribute("recurseConfigLabel").Value
																								  : m_current.Parent.Text);
			m_cfgParentNode.Visible = true;
			m_cfgParentNode.SetDetails(sMoreDetail, m_chkDisplayData.Checked && fEnabled, false);
		}

		private bool IsSubsenseNode => m_current?.Parent != null && ((LayoutTreeNode) m_current.Parent).ShowSenseConfig;

		private bool IsReversalLayout => m_defaultRootLayoutName != null && m_defaultRootLayoutName.Contains("Reversal");

		private void DisplaySenseConfigControls(bool fEnabled)
		{
			// Don't show the "Show single gram info first" checkbox control for the
			// subsense display or for a reversal layout.
			m_chkShowSingleGramInfoFirst.Visible = !IsReversalLayout;

			// Handle possibility of subsense node including above checkbox
			if (IsSubsenseNode)
			{
				HideSenseConfigControls();
			}
			else
			{
				ShowSenseConfigControls();
			}

			m_cfgSenses.DisplaySenseInPara = m_current.ShowSenseAsPara;
			// Otherwise, sometimes Context controls come up invisible when they shouldn't.
			m_current.After = m_current.After ?? string.Empty;
			m_current.Before = m_current.Before ?? string.Empty;
			m_current.Between = m_current.Between ?? string.Empty;
			DetermineStateOfContextControls();

			// Arrange placement of Surrounding Context control after making the above (perhaps) visible.
			PlaceContextControls(m_cfgSenses);
			string sBefore, sMark, sAfter;
			m_current.SplitNumberFormat(out sBefore, out sMark, out sAfter);
			if (m_current.Number == "")
			{
				m_cfgSenses.NumberStyleCombo.SelectedIndex = 0;
			}
			else if (sMark == "%O")
			{
				m_cfgSenses.NumberStyleCombo.SelectedIndex = 1;
			}
			else
			{
				m_cfgSenses.NumberStyleCombo.SelectedIndex = 2;
			}
			m_cfgSenses.BeforeNumber = sBefore;
			m_cfgSenses.AfterNumber = sAfter;
			SetNumStyleCheckStates();
			if (string.IsNullOrEmpty(m_current.NumFont))
			{
				m_cfgSenses.NumberFontCombo.SelectedIndex = 0;
				m_cfgSenses.NumberFontCombo.SelectedText = "";
			}
			else
			{
				for (var i = 0; i < m_cfgSenses.NumberFontCombo.Items.Count; ++i)
				{
					if (m_current.NumFont != m_cfgSenses.NumberFontCombo.Items[i].ToString())
					{
						continue;
					}
					m_cfgSenses.NumberFontCombo.SelectedIndex = i;
					break;
				}
			}
			m_cfgSenses.NumberSingleSense = m_current.NumberSingleSense;
			m_cfgSenses.FillStylesCombo(GetParaStyleComboItems);
			m_cfgSenses.SenseParaStyle = m_current.SenseParaStyle;

			m_chkShowSingleGramInfoFirst.Checked = m_current.ShowSingleGramInfoFirst;
			EnableSenseConfigControls(fEnabled);
		}

		private void DetermineStateOfContextControls()
		{
			var fRequisiteBoxChecked = IsSubsenseNode ?
				((LayoutTreeNode) m_current.Parent).ShowSenseAsPara :
				m_cfgSenses.DisplaySenseInPara;

			DisplayContextControls(!fRequisiteBoxChecked);
		}

		private void m_displaySenseInParaChecked(object sender, EventArgs e)
		{
			MakeDivInParaChildNotVisible(m_current, sender);
			DetermineStateOfContextControls();
		}

		private void SetNumStyleCheckStates()
		{
			var csBold = CheckState.Indeterminate;
			var csItalic = CheckState.Indeterminate;
			var sStyle = m_current.NumStyle;
			if (!string.IsNullOrEmpty(sStyle))
			{
				sStyle = sStyle.ToLowerInvariant();
				if (sStyle.IndexOf("-bold") >= 0)
				{
					csBold = CheckState.Unchecked;
				}
				else if (sStyle.IndexOf("bold") >= 0)
				{
					csBold = CheckState.Checked;
				}

				if (sStyle.IndexOf("-italic") >= 0)
				{
					csItalic = CheckState.Unchecked;
				}
				else if (sStyle.IndexOf("italic") >= 0)
				{
					csItalic = CheckState.Checked;
				}
			}
			m_cfgSenses.BoldSenseNumber = csBold;
			m_cfgSenses.ItalicSenseNumber = csItalic;
		}

		private void HideSenseConfigControls()
		{
			m_cfgSenses.Visible = false;
			m_chkShowSingleGramInfoFirst.Visible = false;
			// Restore the original position of the surrounding context controls if
			// necessary.
			PlaceContextControls(null);
		}

		private void ShowSenseConfigControls()
		{
			m_cfgSenses.Visible = true;
			// These controls should never show when the sense config is, but the code that
			// displays them runs before the code that decides whether to display sense config.
			// The easiest thing is just to make sure that they are not showing when sense config is.
			// Sense config has its own control for the style of sense paragraphs if any.
			HideStyleControls();
		}

		/// <summary>
		/// Adjust the position of the context controls. If they need to go below a particular control,
		/// pass it as belowThis. To restore the original position pass null.
		/// </summary>
		private void PlaceContextControls(Control belowThis)
		{
			if (m_dyOriginalTopOfContext == 0)
			{
				// This method gets called very early in initialization; the first call is a good time to
				// record the original placement of things.
				// The 8 is rather arbitrary. It would be nice to take a distance from some pre-positioned control,
				// but it is too difficult to know which one with so many overlapping options.
				m_dyContextOffset = 8;
				m_dyOriginalTopOfContext = Height - m_lblContext.Top;
			}
			var desiredTop = belowThis?.Bottom + m_dyContextOffset ?? Height - m_dyOriginalTopOfContext;
			var diff = desiredTop - m_lblContext.Top;
			var contextControls = new Control[] {m_lblContext, m_lblBefore, m_lblBetween, m_lblAfter, m_tbBefore, m_tbBetween, m_tbAfter};
			// If we're putting it below something fixed, it should not move if the dialog resizes.
			// If we're putting it the original distance from the bottom, it should.
			foreach (var control in contextControls)
			{
				control.Anchor = AnchorStyles.Left | (belowThis == null ? AnchorStyles.Bottom : AnchorStyles.Top);
			}

			if (diff == 0)
			{
				return;
			}

			foreach (var control in contextControls)
			{
				MoveControlVertically(control, diff);
			}
		}

		private void DisplayGramInfoConfigControls(bool fEnabled)
		{
			m_chkShowSingleGramInfoFirst.Visible = true;
			m_chkShowSingleGramInfoFirst.Checked = m_current.ShowSingleGramInfoFirst;
			m_chkShowSingleGramInfoFirst.Enabled = fEnabled;
		}

		private void HideGramInfoConfigControls()
		{
			if (m_current.ShowSenseConfig)
			{
				return;
			}
			m_chkShowSingleGramInfoFirst.Visible = false;
		}

		private void DisplayComplexFormConfigControls(bool fEnabled)
		{
			m_chkComplexFormsAsParagraphs.Visible = true;
			m_chkComplexFormsAsParagraphs.Checked = m_current.ShowComplexFormPara;
			m_chkComplexFormsAsParagraphs.Enabled = fEnabled;
			if (fEnabled)
			{
				ShowComplexFormRelatedControls(m_current.ShowComplexFormPara);
			}
		}

		private void ShowComplexFormRelatedControls(bool fChecked)
		{
			m_btnStyles.Visible = fChecked;
			m_cbCharStyle.Visible = fChecked;
			m_lblCharStyle.Visible = fChecked;
			if (fChecked)
			{
				m_lblCharStyle.Text = AreaResources.ksParagraphStyle;
				// Default to the "Dictionary-Subentry" style.  See LT-11453.
				if (m_current != null && string.IsNullOrEmpty(m_current.StyleName))
				{
					m_current.StyleName = "Dictionary-Subentry";
				}
				SetParagraphStyles();
				// Get rid of the "none" option, if it exists.  See LT-11453.
				for (var i = 0; i < m_cbCharStyle.Items.Count; ++i)
				{
					var sci = m_cbCharStyle.Items[i] as StyleComboItem;
					if (sci == null || sci.Style != null)
					{
						continue;
					}
					m_cbCharStyle.Items.RemoveAt(i);
					break;
				}
			}
			m_lblContext.Visible = !fChecked;
			m_lblBefore.Visible = !fChecked;
			m_lblBetween.Visible = !fChecked;
			m_lblAfter.Visible = !fChecked;
			m_tbBefore.Visible = !fChecked;
			m_tbBetween.Visible = !fChecked;
			m_tbAfter.Visible = !fChecked;
		}

		private void HideComplexFormConfigControls()
		{
			m_chkComplexFormsAsParagraphs.Visible = false;
		}

		private static void MoveControlVertically(Control ctl, int dy)
		{
			ctl.Location = new Point(ctl.Location.X, ctl.Location.Y + dy);
		}

		private void EnableSenseConfigControls(bool fEnabled)
		{
			m_cfgSenses.Enabled = m_chkDisplayData.Checked && fEnabled;
			m_chkShowSingleGramInfoFirst.Enabled = m_chkDisplayData.Checked && fEnabled;
		}

		private void DisplayDetailsForLeafNode(bool fEnabled)
		{
			m_cfgParentNode.Visible = false;

			DisplayContextControls(fEnabled);
			DisplayWritingSystemControls(fEnabled);
			DisplayStyleControls(fEnabled);
			if (m_current.ShowSenseConfig)
			{
				DisplaySenseConfigControls(fEnabled);
			}
			else
			{
				HideSenseConfigControls();
			}
			m_chkDisplayWsAbbrs.Visible = m_lvItems.Visible;
			if (!m_chkDisplayWsAbbrs.Visible)
			{
				return;
			}
			if (m_lvItems.CheckedIndices.Count > 1)
			{
				m_chkDisplayWsAbbrs.Checked = m_current.ShowWsLabels;
				m_chkDisplayWsAbbrs.Enabled = m_chkDisplayData.Checked && fEnabled;
			}
			else
			{
				m_chkDisplayWsAbbrs.Checked = false;
				m_chkDisplayWsAbbrs.Enabled = false;
			}
		}

		private void DisplayWritingSystemControls(bool fEnabled)
		{
			// don't disable the display just because we don't have a WsLabel. If we have
			// a WsType, we should be able to construct a list and select a default. (LT-9862)
			if (string.IsNullOrEmpty(m_current.WsLabel) && string.IsNullOrEmpty(m_current.WsType))
			{
				m_lblItemsList.Visible = false;
				m_lvItems.Visible = false;
				m_btnMoveItemUp.Visible = false;
				m_btnMoveItemDown.Visible = false;
				m_listType = ListViewContent.Hidden;
				return;
			}
			m_listType = ListViewContent.WritingSystems;
			SetItemListLocations();
			m_lblItemsList.Visible = true;
			m_lblItemsList.Text = AreaResources.ksWritingSystems;
			m_lblItemsList.Enabled = m_chkDisplayData.Checked && fEnabled;
			InitializeWsListView();
			m_lvItems.Visible = true;
			m_lvItems.Enabled = m_chkDisplayData.Checked && fEnabled;
			m_btnMoveItemUp.Visible = true;
			m_btnMoveItemDown.Visible = true;
			if (m_lvItems.SelectedIndices.Count > 1)
			{
				m_btnMoveItemUp.Enabled = m_chkDisplayData.Checked && fEnabled;
				m_btnMoveItemDown.Enabled = m_chkDisplayData.Checked && fEnabled;
			}
			else
			{
				m_btnMoveItemUp.Enabled = false;
				m_btnMoveItemDown.Enabled = false;
			}
		}

		private void InitializeWsListView()
		{
			m_lvItems.Items.Clear();
			ListViewItem lvi;
			int wsDefault;
			var wsDefault2 = 0;
			switch (m_current.WsType)
			{
				case "analysis":
					lvi = new ListViewItem(LanguageExplorerResources.ksDefaultAnalysis);
					wsDefault = WritingSystemServices.kwsAnal;
					lvi.Tag = wsDefault;
					m_lvItems.Items.Add(lvi);
					foreach (var ws in Cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems)
					{
						m_lvItems.Items.Add(new ListViewItem(ws.DisplayLabel) {Tag = ws});
					}
					break;
				case "vernacular":
					lvi = new ListViewItem(LanguageExplorerResources.ksDefaultVernacular);
					wsDefault = WritingSystemServices.kwsVern;
					lvi.Tag = wsDefault;
					m_lvItems.Items.Add(lvi);
					foreach (var ws in Cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems)
					{
						m_lvItems.Items.Add(new ListViewItem(ws.DisplayLabel) {Tag = ws});
					}
					break;
				case "pronunciation":
					lvi = new ListViewItem(LanguageExplorerResources.ksDefaultPronunciation);
					wsDefault = WritingSystemServices.kwsPronunciation;
					lvi.Tag = wsDefault;
					m_lvItems.Items.Add(lvi);
					foreach (var ws in Cache.ServiceLocator.WritingSystems.CurrentPronunciationWritingSystems)
					{
						m_lvItems.Items.Add(new ListViewItem(ws.DisplayLabel) { Tag = ws });
					}
					break;
				case "reversal":
					lvi = new ListViewItem(LanguageExplorerResources.ksCurrentReversal);
					wsDefault = WritingSystemServices.kwsReversalIndex;
					lvi.Tag = wsDefault;
					m_lvItems.Items.Add(lvi);
					foreach (var ws in Cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems)
					{ m_lvItems.Items.Add(new ListViewItem(ws.DisplayLabel) {Tag = ws});}
					break;
				case "analysis vernacular":
					lvi = new ListViewItem(LanguageExplorerResources.ksDefaultAnalysis);
					wsDefault = WritingSystemServices.kwsAnal;
					lvi.Tag = wsDefault;
					m_lvItems.Items.Add(lvi);
					lvi = new ListViewItem(LanguageExplorerResources.ksDefaultVernacular);
					wsDefault2 = WritingSystemServices.kwsVern;
					lvi.Tag = wsDefault2;
					m_lvItems.Items.Add(lvi);
					foreach (var ws in Cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems)
					{
						m_lvItems.Items.Add(new ListViewItem(ws.DisplayLabel) {Tag = ws});
					}
					foreach (var ws in Cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems)
					{
						m_lvItems.Items.Add(new ListViewItem(ws.DisplayLabel) {Tag = ws});
					}
					break;
				default:	// "vernacular analysis"
					lvi = new ListViewItem(LanguageExplorerResources.ksDefaultVernacular);
					wsDefault = WritingSystemServices.kwsVern;
					lvi.Tag = wsDefault;
					m_lvItems.Items.Add(lvi);
					lvi = new ListViewItem(LanguageExplorerResources.ksDefaultAnalysis);
					wsDefault2 = WritingSystemServices.kwsAnal;
					lvi.Tag = wsDefault2;
					m_lvItems.Items.Add(lvi);
					foreach (var ws in Cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems)
					{
						m_lvItems.Items.Add(new ListViewItem(ws.DisplayLabel) {Tag = ws});
					}
					foreach (var ws in Cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems)
					{
						m_lvItems.Items.Add(new ListViewItem(ws.DisplayLabel) {Tag = ws});
					}
					break;
			}
			var wsId = WritingSystemServices.GetMagicWsIdFromName(m_current.WsLabel);
			if (wsId != 0)
			{
				wsId = WritingSystemServices.SmartMagicWsToSimpleMagicWs(wsId);
				SetDefaultWritingSystem(wsId);
			}
			else
			{
				// Handle non-Magic wss here, must be explicit ws tags.
				var rgws = m_current.WsLabel.Split(new[] { ',' });
				var indexTarget = 0;
				for (var i = 0; i < m_lvItems.Items.Count; ++i)
				{
					if (m_lvItems.Items[i].Tag is int)
					{
						continue;
					}
					indexTarget = i;
					break;
				}
				foreach (var sLabel in rgws)
				{
					var fChecked = false;
					for (var iws = 0; iws < m_lvItems.Items.Count; ++iws)
					{
						var ws = m_lvItems.Items[iws].Tag as CoreWritingSystemDefinition;
						if (ws == null || ws.Id != sLabel)
						{
							continue;
						}
						m_lvItems.Items[iws].Checked = true;
						MoveListItem(iws, indexTarget++);
						fChecked = true;
						break;
					}

					if (fChecked)
					{
						continue;
					}
					// Add this to the list of writing systems, since the user must have
					// wanted it at some time.
					CoreWritingSystemDefinition ws2;
					if (Cache.ServiceLocator.WritingSystemManager.TryGet(sLabel, out ws2))
					{
						m_lvItems.Items.Insert(indexTarget++, new ListViewItem(ws2.DisplayLabel) { Tag = ws2, Checked = true });
					}
				}
			}
			// if for some reason nothing was selected, try to select a default.
			if (m_lvItems.CheckedItems.Count == 0)
			{
				SelectDefaultWss(wsDefault, wsDefault2);
			}
			if (m_lvItems.CheckedItems.Count == 0)
			{
				SelectAllDefaultWritingSystems();
			}
		}

		private void SelectDefaultWss(int wsDefault, int wsDefault2)
		{
			if (wsDefault != 0)
			{
				SetDefaultWritingSystem(wsDefault);
			}
			if (wsDefault2 != 0)
			{
				SetDefaultWritingSystem(wsDefault2);
			}
		}

		private void SetDefaultWritingSystem(int wsWanted)
		{
			for (var i = 0; i < m_lvItems.Items.Count; ++i)
			{
				if (!(m_lvItems.Items[i].Tag is int))
				{
					continue;
				}
				var ws = (int)m_lvItems.Items[i].Tag;
				if (ws != wsWanted)
				{
					continue;
				}
				m_lvItems.Items[i].Checked = true;
				break;
			}
		}

		private void SelectAllDefaultWritingSystems()
		{
			for (var i = 0; i < m_lvItems.Items.Count; ++i)
			{
				if (m_lvItems.Items[i].Tag is int)
				{
					m_lvItems.Items[i].Checked = true;
				}
			}
		}

		private void DisplayTypeControls(bool fEnabled)
		{
			if (string.IsNullOrEmpty(m_current.LexRelType) && string.IsNullOrEmpty(m_current.EntryType))
			{
				m_lblItemsList.Visible = false;
				m_lvItems.Visible = false;
				m_btnMoveItemUp.Visible = false;
				m_btnMoveItemDown.Visible = false;
				m_listType = ListViewContent.Hidden;
				return;
			}
			SetItemListLocations();
			m_lblItemsList.Visible = true;
			m_lvItems.Visible = true;
			if (!string.IsNullOrEmpty(m_current.LexRelType))
			{
				InitializeRelationList();
			}
			else switch (m_current.EntryType)
			{
				case "complex":
					InitializeComplexFormTypeList();
					break;
				case "variant":
					InitializeVariantTypeList();
					break;
				default:
					InitializeMinorEntryTypeList();
					break;
			}
			m_btnMoveItemUp.Visible = true;
			m_btnMoveItemDown.Visible = true;
			m_lblItemsList.Enabled = fEnabled;
			m_lvItems.Enabled = fEnabled;
			m_btnMoveItemUp.Enabled = false;
			m_btnMoveItemDown.Enabled = false;
		}

		private void SetItemListLocations()
		{
			int y;
			if (!string.IsNullOrEmpty(m_current.WsLabel) || !String.IsNullOrEmpty(m_current.WsType))
			{
				y = m_chkDisplayData.Location.Y + m_chkDisplayData.Size.Height + 7;
			}
			else if (!string.IsNullOrEmpty(m_current.LexRelType))
			{
				y = m_cfgParentNode.Location.Y + m_cfgParentNode.Size.Height + 7;
			}
			else if (m_current.EntryType == "complex" && !m_current.AllowBeforeStyle)
			{
				y = m_chkComplexFormsAsParagraphs.Location.Y + m_chkComplexFormsAsParagraphs.Size.Height + 7;
			}
			else if (!string.IsNullOrEmpty(m_current.EntryType))
			{
				y = m_cfgParentNode.Location.Y + m_cfgParentNode.Size.Height + 7;
			}
			else
			{
				return;
			}

			m_lblItemsList.Location = new Point(m_lblItemsList.Location.X, y);
			y += m_lblItemsList.Size.Height + 7;
			m_lvItems.Location = new Point(m_lvItems.Location.X, y);
			y += 12;
			m_btnMoveItemUp.Location = new Point(m_btnMoveItemUp.Location.X, y);
			y += m_btnMoveItemUp.Size.Height + 6;
			m_btnMoveItemDown.Location = new Point(m_btnMoveItemDown.Location.X, y);
		}

		private void InitializeRelationList()
		{
			m_listType = ListViewContent.RelationTypes;
			m_lblItemsList.Text = LanguageExplorerResources.ksLexicalRelationTypes;
			m_lvItems.Items.Clear();
			foreach (var lvi in m_current.RelTypeList.Select(x => new ListViewItem(x.Name) {Tag = x, Checked = x.Enabled}))
			{
				m_lvItems.Items.Add(lvi);
			}
		}

		private void InitializeComplexFormTypeList()
		{
			m_listType = ListViewContent.ComplexFormTypes;
			m_lblItemsList.Text = LanguageExplorerResources.ksComplexFormTypes;
			m_lvItems.Items.Clear();
			foreach (var lvi in m_current.EntryTypeList.Select(x => new ListViewItem(x.Name) {Tag = x, Checked = x.Enabled}))
			{
				m_lvItems.Items.Add(lvi);
			}
		}

		private void InitializeVariantTypeList()
		{
			m_listType = ListViewContent.VariantTypes;
			m_lblItemsList.Text = LanguageExplorerResources.ksVariantTypes;
			m_lvItems.Items.Clear();
			foreach (var lvi in m_current.EntryTypeList.Select(x => new ListViewItem(x.Name) {Tag = x, Checked = x.Enabled}))
			{
				m_lvItems.Items.Add(lvi);
			}
		}

		private void InitializeMinorEntryTypeList()
		{
			m_listType = ListViewContent.MinorEntryTypes;
			m_lblItemsList.Text = LanguageExplorerResources.ksMinorEntryTypes;
			m_lvItems.Items.Clear();
			foreach (var lvi in m_current.EntryTypeList.Select(x => new ListViewItem(x.Name) {Tag = x, Checked = x.Enabled}))
			{
				m_lvItems.Items.Add(lvi);
			}
		}

		private static int ComparePossibilitiesByName(ICmPossibility x, ICmPossibility y)
		{
			if (x == null)
			{
				return y == null ? 0 : -1;
			}
			if (y == null)
			{
				return 1;
			}
			var xName = x.Name.BestAnalysisVernacularAlternative.Text;
			var yName = y.Name.BestAnalysisVernacularAlternative.Text;
			if (xName == null)
			{
				return yName == null ? 0 : -1;
			}
			return yName == null ? 1 : xName.CompareTo(yName);
		}

		private void DisplayStyleControls(bool fEnabled)
		{
			if (m_current.AllowCharStyle)
			{
				m_lblCharStyle.Text = LanguageExplorerResources.ksCharacterStyleForContent;
				SetCharacterStyles();
				m_lblCharStyle.Visible = true;
				m_cbCharStyle.Visible = true;
				m_btnStyles.Visible = true;
				m_lblCharStyle.Enabled = m_chkDisplayData.Checked && fEnabled;
				m_cbCharStyle.Enabled = m_chkDisplayData.Checked && fEnabled;
				m_btnStyles.Enabled = m_chkDisplayData.Checked && fEnabled;
			}
			else if (m_current.AllowParaStyle || m_current.AllowDivParaStyle)
			{
				m_lblCharStyle.Text = LanguageExplorerResources.ksParagraphStyleForContent;
				SetParagraphStyles();
				m_lblCharStyle.Visible = true;
				m_cbCharStyle.Visible = true;
				m_btnStyles.Visible = true;
				m_lblCharStyle.Enabled = m_chkDisplayData.Checked && fEnabled;
				m_cbCharStyle.Enabled = m_chkDisplayData.Checked && fEnabled;
				m_btnStyles.Enabled = m_chkDisplayData.Checked && fEnabled;
			}
			else
			{
				HideStyleControls();
			}
		}

		private void HideStyleControls()
		{
			m_lblCharStyle.Visible = false;
			m_cbCharStyle.Visible = false;
			m_btnStyles.Visible = false;
		}

		private void SetCharacterStyles()
		{
			m_cbCharStyle.Items.Clear();
			m_cbCharStyle.Items.AddRange(m_rgCharStyles.ToArray());
			for (var i = 0; i < m_rgCharStyles.Count; ++i)
			{
				if (m_rgCharStyles[i].Style == null || m_rgCharStyles[i].Style.Name != m_current.StyleName)
				{
					continue;
				}
				m_cbCharStyle.SelectedIndex = i;
				break;
			}
		}

		private void SetParagraphStyles()
		{
			m_cbCharStyle.Items.Clear();
			var paraStyleComboItems = GetParaStyleComboItems;
			m_cbCharStyle.Items.AddRange(paraStyleComboItems.ToArray());
			for (var i = 0; i < m_rgParaStyles.Count; ++i)
			{
				if (paraStyleComboItems[i].Style == null || paraStyleComboItems[i].Style.Name != m_current.StyleName)
				{
					continue;
				}
				m_cbCharStyle.SelectedIndex = i;
				break;
			}
		}

		private List<StyleComboItem> GetParaStyleComboItems
		{
			get
			{
				return m_current != null && m_current.PreventNullStyle
					? m_rgParaStyles.Where(item => item.Style != null).ToList()
					: m_rgParaStyles;
			}
		}

		private void DisplayBeforeStyleControls(bool fEnabled)
		{
			if (m_current.AllowBeforeStyle)
			{
				m_cbBeforeStyle.Items.Clear();
				var fParaStyles = m_current.FlowType == "div";
				if (fParaStyles)
				{
					m_lblBeforeStyle.Text = AreaResources.ksParagraphStyleForBefore;
					m_cbBeforeStyle.Items.AddRange(m_rgParaStyles.ToArray());
					for (var i = 0; i < m_rgParaStyles.Count; ++i)
					{
						if (m_rgParaStyles[i].Style != null && m_rgParaStyles[i].Style.Name == m_current.BeforeStyleName)
						{
							m_cbBeforeStyle.SelectedIndex = i;
							break;
						}
					}
				}
				else
				{
					m_lblBeforeStyle.Text = AreaResources.ksCharacterStyleForBefore;
					m_cbBeforeStyle.Items.AddRange(m_rgCharStyles.ToArray());
					for (var i = 0; i < m_rgCharStyles.Count; ++i)
					{
						if (m_rgCharStyles[i].Style != null && m_rgCharStyles[i].Style.Name == m_current.BeforeStyleName)
						{
							m_cbBeforeStyle.SelectedIndex = i;
							break;
						}
					}
				}
				m_lblBeforeStyle.Visible = true;
				m_cbBeforeStyle.Visible = true;
				m_btnBeforeStyles.Visible = true;
				m_lblBeforeStyle.Enabled = m_chkDisplayData.Checked && fEnabled;
				m_cbBeforeStyle.Enabled = m_chkDisplayData.Checked && fEnabled;
				m_btnBeforeStyles.Enabled = m_chkDisplayData.Checked && fEnabled;
			}
			else
			{
				m_lblBeforeStyle.Visible = false;
				m_cbBeforeStyle.Visible = false;
				m_btnBeforeStyles.Visible = false;
			}
		}

		private void DisplayContextControls(bool fEnabled)
		{
			if (m_current.Before == null && m_current.After == null && m_current.Between == null)
			{
				m_lblContext.Visible = false;
				m_lblBefore.Visible = false;
				m_tbBefore.Visible = false;
				m_lblAfter.Visible = false;
				m_tbAfter.Visible = false;
				m_lblBetween.Visible = false;
				m_tbBetween.Visible = false;
				return;
			}

			m_lblContext.Visible = true;
			m_lblContext.Enabled = m_chkDisplayData.Checked && fEnabled;
			if (m_current.Before != null)
			{
				m_lblBefore.Text = AreaResources.ksBefore;
				m_tbBefore.Text = m_current.Before;
				m_lblBefore.Visible = true;
				m_tbBefore.Visible = true;
				m_lblBefore.Enabled = m_chkDisplayData.Checked && fEnabled;
				m_tbBefore.Enabled = m_chkDisplayData.Checked && fEnabled;
			}
			else
			{
				m_tbBefore.Text = string.Empty;
				m_lblBefore.Visible = true;
				m_tbBefore.Visible = true;
				m_lblBefore.Enabled = false;
				m_tbBefore.Enabled = false;
			}

			if (m_current.After != null)
			{
				m_tbAfter.Text = m_current.After;
				m_lblAfter.Visible = true;
				m_tbAfter.Visible = true;
				m_lblAfter.Enabled = m_chkDisplayData.Checked && fEnabled;
				m_tbAfter.Enabled = m_chkDisplayData.Checked && fEnabled;
			}
			else
			{
				m_tbAfter.Text = string.Empty;
				m_lblAfter.Visible = true;
				m_tbAfter.Visible = true;
				m_lblAfter.Enabled = false;
				m_tbAfter.Enabled = false;
			}

			if (m_current.Between != null)
			{
				m_tbBetween.Text = m_current.Between;
				m_lblBetween.Visible = true;
				m_tbBetween.Visible = true;
				m_lblBetween.Enabled = m_chkDisplayData.Checked && fEnabled;
				m_tbBetween.Enabled = m_chkDisplayData.Checked && fEnabled;
			}
			else
			{
				m_tbBetween.Text = string.Empty;
				m_lblBetween.Visible = true;
				m_tbBetween.Visible = true;
				m_lblBetween.Enabled = false;
				m_tbBetween.Enabled = false;
			}
		}

		/// <summary>
		/// Return true if any changes have been made to the layout configuration.
		/// </summary>
		private bool IsDirty()
		{
			StoreNodeData(m_current);
			if (m_fDeleteCustomFiles)
			{
				return true;
			}
			var sOldRootLayout = m_propertyTable.GetValue<string>(m_sLayoutPropertyName);
			var sRootLayout = ((LayoutTypeComboItem)m_cbDictType.SelectedItem).LayoutName;
			if (sOldRootLayout != sRootLayout)
			{
				return true;
			}
			foreach (var item in m_cbDictType.Items)
			{
				var ltci = (LayoutTypeComboItem)item;
				foreach (var ltn in ltci.TreeNodes)
				{
					if (ltn.IsDirty())
					{
						return true;
					}
				}
			}
			return false;
		}

		private void SaveModifiedLayouts()
		{
			var rgxnLayouts = new List<XElement>();
			foreach (var item in m_cbDictType.Items)
			{
				var ltci = (LayoutTypeComboItem)item;
				foreach (var treeNode in ltci.TreeNodes)
				{
					treeNode.MakeSenseNumberFormatConsistent();
					treeNode.GetModifiedLayouts(rgxnLayouts, ltci.TreeNodes);
				}
				// update the inventory with the copied layout type.
				if (ltci.LayoutName.Contains(Inventory.kcMarkLayoutCopy))
				{
					m_layouts.AddLayoutTypeToInventory(ltci.LayoutTypeNode);
				}
			}
			if (m_fDeleteCustomFiles)
			{
				Debug.Assert(m_layouts.DatabaseName == null);
				m_layouts.DeleteUserOverrides(Cache.ProjectId.Name);
				m_parts.DeleteUserOverrides(Cache.ProjectId.Name);
				//Make sure to retain any data from user override files that were not deleted i.e. copies of dictionary views
				m_layouts.LoadUserOverrides(LayoutCache.LayoutVersionNumber, Cache.ProjectId.Name);
				m_parts.LoadUserOverrides(LayoutCache.LayoutVersionNumber, Cache.ProjectId.Name);

				Inventory.SetInventory("layouts", Cache.ProjectId.Name, m_layouts);
				Inventory.SetInventory("parts", Cache.ProjectId.Name, m_parts);
				Inventory.RemoveInventory("layouts", null);
				Inventory.RemoveInventory("parts", null);
			}

			foreach (var layout in rgxnLayouts)
			{
				m_layouts.PersistOverrideElement(layout);
			}

			var layoutsPersisted = new HashSet<XElement>(rgxnLayouts);
			foreach (var xnNewBase in m_rgxnNewLayoutNodes)
			{
				if (!layoutsPersisted.Contains(xnNewBase)) // don't need to persist a node that got into both lists twice
				{
					m_layouts.PersistOverrideElement(xnNewBase);
				}
			}
			m_rgxnNewLayoutNodes.Clear();
		}


		#endregion // Misc internal functions

		public void CheckDisposed()
		{
			if (IsDisposed)
			{
				throw new ObjectDisposedException($"'{GetType().Name}' in use after being disposed.");
			}
		}

		private void XmlDocConfigureDlg_FormClosed(object sender, FormClosedEventArgs e)
		{
			if (DialogResult == DialogResult.OK || !m_fDeleteCustomFiles)
			{
				return;
			}
			Inventory.RemoveInventory("layouts", null);
			Inventory.RemoveInventory("parts", null);
		}

		private void m_btnSetAll_Click(object sender, EventArgs e)
		{
			if (m_tvParts == null || m_tvParts.Nodes.Count == 0)
			{
				return;
			}

			foreach (TreeNode node in m_tvParts.Nodes)
			{
				CheckNodeAndChildren(node, m_fValueForSetAll);
			}
			m_fValueForSetAll = !m_fValueForSetAll;
			m_btnSetAll.Text = m_fValueForSetAll ? "DEBUG: Set All" : "DEBUG: Clear All";
		}

		private static void CheckNodeAndChildren(TreeNode node, bool val)
		{
			node.Checked = val;
			foreach (TreeNode tn in node.Nodes)
			{
				CheckNodeAndChildren(tn, val);
			}
		}

		private void m_cfgSenses_SensesBtnClicked(object sender, EventArgs e)
		{
			HandleStylesBtn(m_cfgSenses.SenseStyleCombo, () => m_cfgSenses.FillStylesCombo(m_rgParaStyles), m_current.StyleName);
		}

		#region Manage Views methods

		/// <summary>
		/// Call the Dictionary Configuration Manager dialog when this "Manage Views" link text is clicked.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void m_linkManageViews_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			var currentItem = m_cbDictType.SelectedItem as LayoutTypeComboItem;
			if (currentItem == null)
			{
				return;
			}
			var current = currentItem.LayoutTypeNode;
			var configViews = (m_cbDictType.Items.OfType<LayoutTypeComboItem>().Select(item => item.LayoutTypeNode)).ToList();
			using (var dlg = new DictionaryConfigMgrDlg(m_propertyTable, m_configObjectName, configViews, current))
			{
				dlg.Text = string.Format(dlg.Text, m_configObjectName);
				var presenter = dlg.Presenter;
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					ProcessXMLConfigChanges(presenter as IDictConfigManager);
				}
			}
		}

		private void ProcessXMLConfigChanges(IDictConfigManager presenter)
		{
			// presenter.NewConfigurationViews will give a list of copied views to create.
			var newViewsToCreate = presenter.NewConfigurationViews;
			if (newViewsToCreate != null)
			{
				CreateConfigurationCopies(newViewsToCreate);
			}

			// presenter.ConfigurationViewsToDelete will give a list of views to delete.
			var viewsToDelete = presenter.ConfigurationViewsToDelete;
			if (viewsToDelete != null)
			{
				DeleteUnwantedConfigurations(viewsToDelete);
			}

			// presenter.RenamedExistingViews will give a list of existing views whose
			//   display name has changed.
			var viewsToRename = presenter.RenamedExistingViews;
			if (viewsToRename != null)
			{
				RenameConfigurations(viewsToRename);
			}

			// presenter.FinalConfigurationView will give the unique code for the view
			//   that should now be active in this dialog.
			var newActiveConfig = presenter.FinalConfigurationView;
			if (newActiveConfig != null)
			{
				SetNewActiveConfiguration(newActiveConfig);
			}
		}
		// ReSharper restore InconsistentNaming

		private void CreateConfigurationCopies(IEnumerable<Tuple<string, string, string>> newViewsToCreate)
		{
			var mapLayoutToConfigBase = new Dictionary<string, XElement>();
			foreach (var xn in(m_cbDictType.Items.OfType<LayoutTypeComboItem>().Select(item => item.LayoutTypeNode)))
			{
				if (xn == null)
				{
					continue;
				}
				var sLayout = XmlUtils.GetMandatoryAttributeValue(xn, "layout");
				mapLayoutToConfigBase.Add(sLayout, xn);
			}
			foreach ( var viewSpec in newViewsToCreate)
			{
				var code = viewSpec.Item1;
				var baseLayout = viewSpec.Item2;
				var label = viewSpec.Item3;
				XElement xnBaseConfig;
				if (mapLayoutToConfigBase.TryGetValue(baseLayout, out xnBaseConfig))
				{
					CopyConfiguration(xnBaseConfig, code, label);
				}
			}
		}

		//
		// *** Configuration nodes look like this:
		//"<layoutType label=\"Lexeme-based (complex forms as main entries)\" layout=\"publishStem\">" +
		//    "<configure class=\"LexEntry\" label=\"Main Entry\" layout=\"publishStemEntry\"/>" +
		//    "<configure class=\"LexEntry\" label=\"Minor Entry\" layout=\"publishStemMinorEntry\"/>" +
		//"</layoutType>" +
		//
		private void CopyConfiguration(XElement xnBaseConfig, string code, string label)
		{
			var xnNewConfig = xnBaseConfig.Clone();
			//set the version number on the layoutType node to indicate user configured
// ReSharper disable PossibleNullReferenceException
// With any xml node that can possibly be copied here we have an owner and attributes
			var versionAtt = new XAttribute("version", LayoutCache.LayoutVersionNumber.ToString());
			xnNewConfig.Add(versionAtt);
// ReSharper restore PossibleNullReferenceException
			Debug.Assert(xnNewConfig.HasAttributes);
			var xaLabel = xnNewConfig.Attribute("label");
			xaLabel.Value = label;
			UpdateLayoutName(xnNewConfig, "layout", code);
			// make sure we copy the top-level node if it isn't directly involved in configuration.
			var firstChildElement = xnBaseConfig.Elements().First();
			var className = XmlUtils.GetMandatoryAttributeValue(firstChildElement, "class");
			var layoutNameChild = XmlUtils.GetMandatoryAttributeValue(firstChildElement, "layout");
			var layoutName = XmlUtils.GetMandatoryAttributeValue(xnBaseConfig, "layout");
			if (layoutName != layoutNameChild)
			{
				var xnBaseLayout = m_layouts.GetElement("layout", new[] {className, "jtview", layoutName, null});
				if (xnBaseLayout != null)
				{
					var xnNewBaseLayout = xnBaseLayout.Clone();
					Debug.Assert(xnNewBaseLayout.Elements().Count() == 1);
					UpdateLayoutName(xnNewBaseLayout, "name", code);
					UpdateLayoutName(xnNewBaseLayout.Elements().First(), "param", code);
					m_layouts.AddNodeToInventory(xnNewBaseLayout);
					m_rgxnNewLayoutNodes.Add(xnNewBaseLayout);
				}
			}
			MakeSuffixLayout(code, layoutName, className);
			foreach (var xn in xnNewConfig.Elements())
			{
				className = XmlUtils.GetMandatoryAttributeValue(xn, "class");
				layoutName = XmlUtils.GetMandatoryAttributeValue(xn, "layout");
				CopyAndRenameLayout(className, layoutName, "#" + code);
				UpdateLayoutName(xn, "layout", code);
				MakeSuffixLayout(code, layoutName, className);
			}
			var rgltn = LegacyConfigurationUtils.BuildLayoutTree(xnNewConfig, this);
			MarkLayoutTreeNodesAsNew(rgltn);
			m_cbDictType.Items.Add(new LayoutTypeComboItem(xnNewConfig, rgltn));
		}

		private string MakeSuffixLayout(string code, string layoutName, string className)
		{
			var layoutSuffix = XmlUtils.GetOptionalAttributeValue(m_configurationParameters, "layoutSuffix");
			if (string.IsNullOrEmpty(layoutSuffix))
			{
				return layoutSuffix;
			}
			// The view also requires a layout made by appending this suffix to the layout name. Make that also.
			var suffixLayoutName = layoutName + layoutSuffix;
			var xnSuffixLayout = m_layouts.GetElement("layout", new[] { className, "jtview", suffixLayoutName, null });
			if (xnSuffixLayout == null)
			{
				return layoutSuffix;
			}
			var newSuffixLayout = xnSuffixLayout.Clone();
			DuplicateLayout(newSuffixLayout, "#" + code, new List<XElement>());
			m_layouts.AddNodeToInventory(newSuffixLayout);
			m_rgxnNewLayoutNodes.Add(newSuffixLayout);
			return layoutSuffix;
		}

		private static void MarkLayoutTreeNodesAsNew(IEnumerable<LayoutTreeNode> rgltnStyle)
		{
			foreach (var ltn in rgltnStyle)
			{
				ltn.IsNew = true;
				MarkLayoutTreeNodesAsNew(ltn.Nodes.OfType<LayoutTreeNode>());
			}
		}

		private void CopyAndRenameLayout(string className, string layoutName, string suffixCode)
		{
			var xnLayout = m_layouts.GetElement("layout", new[] { className, "jtview", layoutName, null });
			if (xnLayout == null)
			{
				return;
			}
			var duplicates = new List<XElement>();
			DuplicateLayout(xnLayout.Clone(), suffixCode, duplicates);
			// This method is used to duplicate the layouts used by one of the top level nodes
			// of the configuration. I think most of the layout nodes get matched up to tree nodes
			// and persisted as a result of that. sublayout ones don't, so we need to record them
			// as extras in the loop below. Also, the root one does not become a node in the tree,
			// so we need to remember it explicitly. (The line immediately below was added to fix
			// LT-13425.)
			m_rgxnNewLayoutNodes.Add(duplicates[0]);
			foreach (var xn in duplicates)
			{
				m_layouts.AddNodeToInventory(xn);
				var nodes = xn.Elements().ToList();
				if (nodes.Count == 1 && nodes[0].Name.LocalName == "sublayout")
				{
					m_rgxnNewLayoutNodes.Add(xn);
				}
			}
		}

		private static void UpdateLayoutName(XElement node, string attrName, string code)
		{
			Debug.Assert(node.HasAttributes);
			var xaLayout = node.Attribute(attrName);
			var oldLayout = xaLayout.Value;
			var idx = oldLayout.IndexOf(Inventory.kcMarkLayoutCopy);
			if (idx > 0)
			{
				oldLayout = oldLayout.Remove(idx);
			}
			xaLayout.Value = $"{oldLayout}{Inventory.kcMarkLayoutCopy}{code}";
		}

		private void DeleteUnwantedConfigurations(IEnumerable<string> viewsToDelete)
		{
			var configDir = LcmFileHelper.GetConfigSettingsDir(Cache.ProjectId.ProjectFolder);
			// Load in existing combobox LayoutTypeComboItems
			var layoutMap = LoadLayoutMapFromComboBox();

			// Find Matches, delete layouts
			foreach (var viewId in viewsToDelete)
			{
				// Find match and delete
				LayoutTypeComboItem defunct;
				if (layoutMap.TryGetValue(viewId, out defunct))
				{
					layoutMap.Remove(viewId);
					var path = GetConfigFilePath(defunct.LayoutTypeNode, configDir);
					File.Delete(path);
				}
			}
			RewriteComboBoxItemsAfterDelete(layoutMap);
			m_layouts.Reload();
			m_parts.Reload();
		}

		private static string GetConfigFilePath(XElement xnConfig, string configDir)
		{
			var label = XmlUtils.GetMandatoryAttributeValue(xnConfig, "label");
			var className = XmlUtils.GetMandatoryAttributeValue(xnConfig.Elements().First(), "class");
			var name = $"{label}_{className}.fwlayout";
			return Path.Combine(configDir, name);
		}

		private void RewriteComboBoxItemsAfterDelete(Dictionary<string, LayoutTypeComboItem> layoutMap)
		{
			var oldSelItem = m_cbDictType.SelectedItem as LayoutTypeComboItem;
			Debug.Assert(oldSelItem != null, "Should have SOME selected item!");
			var key = oldSelItem.LayoutName;
			var idx = m_cbDictType.SelectedIndex;
			var delta = layoutMap.Count - m_cbDictType.Items.Count;
			idx += delta;
			m_cbDictType.BeginUpdate();
			m_cbDictType.Items.Clear();
			foreach (var item in layoutMap)
			{
				m_cbDictType.Items.Add(item.Value);
			}
			var newSelItem = FindMatchingLayoutInMap(key, layoutMap);
			if (newSelItem == null)
			{
				m_cbDictType.SelectedIndex = Math.Max(0, idx);
			}
			else
			{
				m_cbDictType.SelectedItem = newSelItem;
			}
			m_cbDictType.EndUpdate();
		}

		private void RenameConfigurations(IEnumerable<Tuple<string, string>> viewsToRename)
		{
			var configDir = LcmFileHelper.GetConfigSettingsDir(Cache.ProjectId.ProjectFolder);
			// Load in existing combobox LayoutTypeComboItems
			var layoutMap = LoadLayoutMapFromComboBox();

			// Find matches, change layout display name
			foreach (var viewNameSpec in viewsToRename)
			{
				var id = viewNameSpec.Item1;
				var newLabel = viewNameSpec.Item2;

				// Find match
				var layoutItem = FindMatchingLayoutInMap(id, layoutMap);
				var oldPath = GetConfigFilePath(layoutItem.LayoutTypeNode, configDir);
				XmlUtils.SetAttribute(layoutItem.LayoutTypeNode, "label", newLabel);
				layoutItem.Label = newLabel;
				var newPath = GetConfigFilePath(layoutItem.LayoutTypeNode, configDir);
				File.Move(oldPath, newPath);	// rename the file.
			}
			RewriteComboBoxItemsAfterRename(layoutMap);
		}

		private void RewriteComboBoxItemsAfterRename(Dictionary<string, LayoutTypeComboItem> layoutMap)
		{
			Debug.Assert(layoutMap.Count != m_cbDictType.Items.Count, "Rename shouldn't change number of items!");
			var idx = m_cbDictType.SelectedIndex;
			m_cbDictType.BeginUpdate();
			m_cbDictType.Items.Clear();
			foreach (var item in layoutMap)
			{
				m_cbDictType.Items.Add(item.Value);
			}
			m_cbDictType.SelectedIndex = idx;
			m_cbDictType.EndUpdate();
		}

		/// <summary>
		/// Gets the layout item matching a given code from the Manage Views dialog.
		/// </summary>
		/// <param name="layoutId">Layout code from DictionaryConfigMgrDlg</param>
		/// <param name="layoutMap">Map from LoadLayoutMapFromComboBox</param>
		/// <returns>null if not found</returns>
		private static LayoutTypeComboItem FindMatchingLayoutInMap(string layoutId,
			Dictionary<string,LayoutTypeComboItem> layoutMap)
		{
			var mapKey = layoutMap.Keys.FirstOrDefault(key => key.Contains(layoutId));
			if (mapKey == null)
			{
				return null; // safety feature; shouldn't happen
			}

			// Found match
			Debug.WriteLine("Found layout key: "+mapKey);
			LayoutTypeComboItem layoutItem;
			layoutMap.TryGetValue(mapKey, out layoutItem);
			return layoutItem;
		}

		private Dictionary<string, LayoutTypeComboItem> LoadLayoutMapFromComboBox()
		{
			var mapLayoutToConfigBase = new Dictionary<string, LayoutTypeComboItem>();
			foreach (var ltn in (m_cbDictType.Items.OfType<LayoutTypeComboItem>()))
			{
				var sLayout = XmlUtils.GetMandatoryAttributeValue(ltn.LayoutTypeNode, "layout");
				mapLayoutToConfigBase.Add(sLayout, ltn);
			}
			return mapLayoutToConfigBase;
		}

		private void SetNewActiveConfiguration(string newActiveConfig)
		{
			var layoutMap = LoadLayoutMapFromComboBox();
			// Find matching view
			var match = FindMatchingLayoutInMap(newActiveConfig, layoutMap);
			// Set combobox to that view
			m_cbDictType.SelectedItem = match;
		}

		#endregion

		private void m_linkConfigureHomograph_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			using (var dlg = new ConfigureHomographDlg())
			{
				var flexApp = m_mainWindow.PropertyTable.GetValue<IFlexApp>("App");
				dlg.SetupDialog(m_mainWindow.Cache.ServiceLocator.GetInstance<HomographConfiguration>(), m_mainWindow.Cache, m_mainWindow.PropertyTable.GetValue<LcmStyleSheet>("FlexStyleSheet"), flexApp, flexApp);
				dlg.StartPosition = FormStartPosition.CenterScreen;
				MasterRefreshRequired = dlg.ShowDialog((Form)m_mainWindow) == DialogResult.OK;
			}
		}

		#region ILayoutConverter methods
		public void AddDictionaryTypeItem(XElement layoutNode, List<LayoutTreeNode> oldNodes)
		{
			m_cbDictType.Items.Add(new LayoutTypeComboItem(layoutNode, oldNodes));
		}

		public IEnumerable<XElement> GetLayoutTypes()
		{
			return m_layouts.GetLayoutTypes();
		}

		public LcmCache Cache { get; private set; }

		public bool UseStringTable => true;

		public LayoutLevels LayoutLevels { get; } = new LayoutLevels();

		public void ExpandWsTaggedNodes(string sWsTag)
		{
			m_layouts.ExpandWsTaggedNodes(sWsTag);
		}

		public void SetOriginalIndexForNode(LayoutTreeNode mainLayoutNode)
		{
			mainLayoutNode.OriginalIndex = m_tvParts.Nodes.Count;
		}

		public XElement GetLayoutElement(string className, string layoutName)
		{
			return LegacyConfigurationUtils.GetLayoutElement(m_layouts, className, layoutName);
		}

		public XElement GetPartElement(string className, string sRef)
		{
			return LegacyConfigurationUtils.GetPartElement(m_parts, className, sRef);
		}
		#endregion

		private sealed class GuidAndSubClass
		{
			private Guid ItemGuid { get; set; }
			internal TypeSubClass SubClass { get; set; }

			internal GuidAndSubClass(Guid guid, TypeSubClass sub)
			{
				ItemGuid = guid;
				SubClass = sub;
			}

			/// <summary>
			/// Override.
			/// </summary>
			public override bool Equals(object obj)
			{
				if (!(obj is GuidAndSubClass))
				{
					return false;
				}
				var that = (GuidAndSubClass)obj;
				return ItemGuid == that.ItemGuid && SubClass == that.SubClass;
			}

			/// <summary>
			/// Override.
			/// </summary>
			public override int GetHashCode()
			{
				return ItemGuid.GetHashCode() + SubClass.GetHashCode();
			}
		}

		/// <summary>
		/// Distinguish the various uses of m_lvItems, m_btnMoveItemUp, and m_btnMoveItemDown.
		/// </summary>
		private enum ListViewContent
		{
			Hidden,
			WritingSystems,
			RelationTypes,
			ComplexFormTypes,
			VariantTypes,
			MinorEntryTypes
		}

		#region LayoutTypeComboItem class

		private sealed class LayoutTypeComboItem
		{
			internal LayoutTypeComboItem(XElement xnLayoutType, List<LayoutTreeNode> rgltn)
			{
				Label = XmlUtils.GetMandatoryAttributeValue(xnLayoutType, "label");
				LayoutName = XmlUtils.GetMandatoryAttributeValue(xnLayoutType, "layout");
				LayoutTypeNode = xnLayoutType;
				TreeNodes = rgltn;
			}

			internal string Label { get; set; }

			internal string LayoutName { get; }

			internal List<LayoutTreeNode> TreeNodes { get; }

			public override string ToString()
			{
				return Label;
			}

			internal XElement LayoutTypeNode { get; private set; }
		}
		#endregion
	}
}