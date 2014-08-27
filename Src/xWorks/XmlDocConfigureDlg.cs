// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: XmlDocConfigureDlg.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// Uncomment the #define if you want to see the "Restore Defaults" and "Set/Clear All" buttons.
// (This affects only DEBUG builds.)
// </remarks>
#define DEBUG_TEST

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.FwCoreDlgControls;
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.RootSites;
using XCore;

namespace SIL.FieldWorks.XWorks
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
	public partial class XmlDocConfigureDlg : Form, IFWDisposable, ILayoutConverter
	{
		XmlNode m_configurationParameters;
		string m_defaultRootLayoutName;
		const string sdefaultStemBasedLayout = "publishStem";
		FdoCache m_cache;
		IFwMetaDataCache m_mdc;
		FwStyleSheet m_styleSheet;
		IMainWindowDelegateCallbacks m_callbacks;
		Mediator m_mediator;
		string m_sLayoutPropertyName;
		StringTable m_stringTbl;
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
		/// <summary>
		/// Store nodes that need to be persisted as part of a new copy of a whole layout,
		/// but which don't directly (or indirectly) appear in configuration.
		/// </summary>
		readonly List<XmlNode> m_rgxnNewLayoutNodes = new List<XmlNode>();

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
		/// This class encapsulates a part (ref=) and its enclosing layout.
		/// </summary>
		internal class PartCaller
		{
			private readonly XmlNode m_xnPartRef;
			private readonly XmlNode m_xnLayout;
			private readonly bool m_fHidden;

			internal PartCaller(XmlNode layout, XmlNode partref)
			{
				m_xnLayout = layout;
				m_xnPartRef = partref;
				m_fHidden = XmlUtils.GetOptionalBooleanAttributeValue(partref, "hideConfig", false);
			}

			internal XmlNode PartRef
			{
				get { return m_xnPartRef; }
			}

			internal XmlNode Layout
			{
				get { return m_xnLayout; }
			}

			internal bool Hidden
			{
				get { return m_fHidden; }
			}
		}

		readonly LayoutLevels m_levels = new LayoutLevels();

		/// <summary>
		/// Toggles true to set, false to clear.
		/// </summary>
		private bool m_fValueForSetAll = true;

		#region Constructor, Initialization, etc.

		/// <summary>
		/// Convert a distance that is right at 96 dpi to the current screen dpi
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		int From96dpiY(int input)
		{
			using (var g = CreateGraphics())
			{
				return (int)Math.Round(input*g.DpiY/96.0);
			}
		}

		/// <summary>
		/// Convert a distance that is right at 96 dpi to the current screen dpi
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		int From96dpiX(int input)
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

			m_unspecComplexFormType = XmlViewsUtils.GetGuidForUnspecifiedComplexFormType();
			m_unspecVariantType = XmlViewsUtils.GetGuidForUnspecifiedVariantType();

			m_noComplexEntryTypeLabel = "<" + xWorksStrings.ksNoComplexFormType + ">";
			m_noVariantTypeLabel = "<" + xWorksStrings.ksNoVariantType + ">";
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

		//void m_cfgSenses_VisibleChanged(object sender, EventArgs e)
		//{
		//    // Seems at resolutions other than 96dpi this control changes its minimumSize
		//    // when it first becomes visible, so at that point we need to adjust it.
		//    m_cfgSenses.Height = m_cfgSenses.MinimumSize.Height;
		//}

		/// <summary>
		/// Initialize the dialog after creating it.
		/// </summary>
		public void SetConfigDlgInfo(XmlNode configurationParameters, FdoCache cache,
			FwStyleSheet styleSheet, IMainWindowDelegateCallbacks mainWindowDelegateCallbacks,
			Mediator mediator, string sLayoutPropertyName)
		{
			CheckDisposed();
			m_configurationParameters = configurationParameters;
			string labelKey = XmlUtils.GetAttributeValue(configurationParameters, "viewTypeLabelKey");
			if (!String.IsNullOrEmpty(labelKey))
			{
				string sLabel = xWorksStrings.ResourceManager.GetString(labelKey);
				if (!String.IsNullOrEmpty(sLabel))
					m_lblViewType.Text = sLabel;
			}
			m_cache = cache;
			m_mdc = m_cache.DomainDataByFlid.MetaDataCache;
			m_styleSheet = styleSheet;
			m_callbacks = mainWindowDelegateCallbacks;
			m_mediator = mediator;
			m_sLayoutPropertyName = sLayoutPropertyName;
			if (m_mediator != null && m_mediator.HasStringTable)
				m_stringTbl = m_mediator.StringTbl;
			m_layouts = Inventory.GetInventory("layouts", cache.ProjectId.Name);
			m_parts = Inventory.GetInventory("parts", cache.ProjectId.Name);
			m_configObjectName = XmlUtils.GetLocalizedAttributeValue(m_stringTbl,
				configurationParameters, "configureObjectName", "");
			Text = String.Format(Text, m_configObjectName);
			m_defaultRootLayoutName = XmlUtils.GetAttributeValue(configurationParameters, "layout");
			string sLayoutType = null;
			if (m_mediator != null && m_mediator.PropertyTable != null)
			{
				object objType = m_mediator.PropertyTable.GetValue(m_sLayoutPropertyName);
				if (objType != null)
					sLayoutType = (string)objType;
			}
			if (String.IsNullOrEmpty(sLayoutType))
				sLayoutType = m_defaultRootLayoutName;

			var configureLayouts = XmlUtils.FindNode(m_configurationParameters, "configureLayouts");
			LegacyConfigurationUtils.BuildTreeFromLayoutAndParts(configureLayouts, this);
			SetSelectedDictionaryTypeItem(sLayoutType);

			// Restore the location and size from last time we called this dialog.
			if (m_mediator != null && m_mediator.PropertyTable != null)
			{
				object locWnd = m_mediator.PropertyTable.GetValue("XmlDocConfigureDlg_Location");
				object szWnd = m_mediator.PropertyTable.GetValue("XmlDocConfigureDlg_Size");
				if (locWnd != null && szWnd != null)
				{
					Rectangle rect = new Rectangle((Point)locWnd, (Size)szWnd);
					ScreenUtils.EnsureVisibleRect(ref rect);
					DesktopBounds = rect;
					StartPosition = FormStartPosition.Manual;
				}
			}

			// Make a help topic ID
			m_helpTopicID = generateChooserHelpTopicID(m_configObjectName);

			// Load the lists for the styles combo boxes.
			SetStylesLists();
		}

		private void SetSelectedDictionaryTypeItem(string sLayoutType)
		{
			int idx = -1;
			for (int i = 0; i < m_cbDictType.Items.Count; ++i)
			{
				var item = m_cbDictType.Items[i] as LayoutTypeComboItem;
				if (item != null && item.LayoutName == sLayoutType)
				{
					idx = i;
					break;
				}
			}
			if (idx < 0)
				idx = 0;
			m_cbDictType.SelectedIndex = idx;
		}

		private void SetStylesLists()
		{
			if (m_rgCharStyles == null)
				m_rgCharStyles = new List<StyleComboItem>();
			else
				m_rgCharStyles.Clear();
			if (m_rgParaStyles == null)
				m_rgParaStyles = new List<StyleComboItem>();
			else
				m_rgParaStyles.Clear();
			m_rgCharStyles.Add(new StyleComboItem(null));
			// Per comments at end of LT-10950, we don't ever want 'none' as an option for paragraph style.
			//m_rgParaStyles.Add(new StyleComboItem(null));
			foreach (BaseStyleInfo sty in m_styleSheet.Styles)
			{
				if (sty.IsCharacterStyle)
					m_rgCharStyles.Add(new StyleComboItem(sty));
				else if (sty.IsParagraphStyle)
					m_rgParaStyles.Add(new StyleComboItem(sty));
			}
			m_rgCharStyles.Sort();
			m_rgParaStyles.Sort();
		}

		class GuidAndSubClass
		{
			private Guid ItemGuid { get; set; }
			internal LexReferenceInfo.TypeSubClass SubClass { get; set; }

			internal GuidAndSubClass(Guid guid, LexReferenceInfo.TypeSubClass sub)
			{
				ItemGuid = guid;
				SubClass = sub;
			}

			/// <summary>
			/// Override.
			/// </summary>
			public override bool Equals(object obj)
			{
				if (obj is GuidAndSubClass)
				{
					var that = (GuidAndSubClass)obj;
					return ItemGuid == that.ItemGuid && SubClass == that.SubClass;
				}
				return false;
			}

			/// <summary>
			/// Override.
			/// </summary>
			public override int GetHashCode()
			{
				return ItemGuid.GetHashCode() + SubClass.GetHashCode();
			}
		}

		public void BuildRelationTypeList(LayoutTreeNode ltn)
		{
			// Get the canonical list from the project.
			if (m_rgRelationTypes == null)
			{
				m_rgRelationTypes = m_cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.ToList();
				m_rgRelationTypes.Sort(ComparePossibilitiesByName);
			}
			// Add any new types to our ordered list (or fill in an empty list).
			var setSortedGuids = new Set<GuidAndSubClass>();
			foreach (var lri in ltn.RelTypeList)
				setSortedGuids.Add(new GuidAndSubClass(lri.ItemGuid, lri.SubClass));
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
				var gsc = new GuidAndSubClass(poss.Guid, LexReferenceInfo.TypeSubClass.Normal);
				if (lrt.MappingType == (int)LexRefTypeTags.MappingTypes.kmtEntryAsymmetricPair ||
					lrt.MappingType == (int)LexRefTypeTags.MappingTypes.kmtEntryOrSenseAsymmetricPair ||
					lrt.MappingType == (int)LexRefTypeTags.MappingTypes.kmtSenseAsymmetricPair ||
					lrt.MappingType == (int)LexRefTypeTags.MappingTypes.kmtEntryTree ||
					lrt.MappingType == (int)LexRefTypeTags.MappingTypes.kmtEntryOrSenseTree ||
					lrt.MappingType == (int)LexRefTypeTags.MappingTypes.kmtSenseTree)
				{
					gsc.SubClass = LexReferenceInfo.TypeSubClass.Forward;
				}
				if (!setSortedGuids.Contains(gsc))
				{
					var lri = new LexReferenceInfo(true, poss.Guid)
						{
							SubClass = gsc.SubClass
						};
					ltn.RelTypeList.Add(lri);
				}
				if (gsc.SubClass == LexReferenceInfo.TypeSubClass.Forward)
				{
					gsc.SubClass = LexReferenceInfo.TypeSubClass.Reverse;
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
				var gsc = new GuidAndSubClass(lrt.Guid, LexReferenceInfo.TypeSubClass.Normal);
				if (lrt.MappingType == (int)LexRefTypeTags.MappingTypes.kmtEntryAsymmetricPair ||
					lrt.MappingType == (int)LexRefTypeTags.MappingTypes.kmtEntryOrSenseAsymmetricPair ||
					lrt.MappingType == (int)LexRefTypeTags.MappingTypes.kmtSenseAsymmetricPair ||
					lrt.MappingType == (int)LexRefTypeTags.MappingTypes.kmtEntryTree ||
					lrt.MappingType == (int)LexRefTypeTags.MappingTypes.kmtEntryOrSenseTree ||
					lrt.MappingType == (int)LexRefTypeTags.MappingTypes.kmtSenseTree)
				{
					gsc.SubClass = LexReferenceInfo.TypeSubClass.Forward;
				}
				mapGuidType.Add(gsc, lrt);
				if (gsc.SubClass == LexReferenceInfo.TypeSubClass.Forward)
				{
					var gsc2 = new GuidAndSubClass(lrt.Guid, LexReferenceInfo.TypeSubClass.Reverse);
					mapGuidType.Add(gsc2, lrt);
				}
			}
			var obsoleteItems = ltn.RelTypeList.Where(
				lri => !mapGuidType.ContainsKey(new GuidAndSubClass(lri.ItemGuid, lri.SubClass))).ToList();
			foreach (var lri in obsoleteItems)
				ltn.RelTypeList.Remove(lri);
			// Add the names to the items in the ordered list.
			foreach (var lri in ltn.RelTypeList)
			{
				var lrt = mapGuidType[new GuidAndSubClass(lri.ItemGuid, lri.SubClass)];
				if (lri.SubClass == LexReferenceInfo.TypeSubClass.Reverse)
					lri.Name = lrt.ReverseName.BestAnalysisVernacularAlternative.Text;
				else
					lri.Name = lrt.Name.BestAnalysisVernacularAlternative.Text;
			}
		}

		public void BuildEntryTypeList(LayoutTreeNode ltn, string parentLayoutName)
		{
			// Add any new types to our ordered list (or fill in an empty list).
			var setGuidsFromXml = new Set<Guid>(ltn.EntryTypeList.Select(info => info.ItemGuid));
			Dictionary<Guid, ICmPossibility> mapGuidType;
			int index;
			switch (ltn.EntryType)
			{
				case "complex":
					// Get the canonical list from the project if needed.
					if (m_rgComplexFormTypes == null)
						m_rgComplexFormTypes = GetSortedFlattenedComplexFormTypeList();

					mapGuidType = m_rgComplexFormTypes.ToDictionary(poss => poss.Guid);
					foreach (var info in
						from poss in m_rgComplexFormTypes
						where !setGuidsFromXml.Contains(poss.Guid)
						select new ItemTypeInfo(true, poss.Guid))
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
						m_rgVariantTypes = GetSortedFlattenedVariantTypeList();

					mapGuidType = m_rgVariantTypes.ToDictionary(poss => poss.Guid);
					foreach (var info in
						from poss in m_rgVariantTypes
						where !setGuidsFromXml.Contains(poss.Guid)
						select new ItemTypeInfo(true, poss.Guid))
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
						m_rgVariantTypes = GetSortedFlattenedVariantTypeList();

					mapGuidType = m_rgVariantTypes.ToDictionary(poss => poss.Guid);
					// Root-based views have Complex Forms as Minor Entries too.
					if (!fstemBased)
					{
						// Get the canonical Complex Form Type list from the project if needed.
						if (m_rgComplexFormTypes == null)
							m_rgComplexFormTypes = GetSortedFlattenedComplexFormTypeList();
						// Add them to the map
						foreach (var poss in m_rgComplexFormTypes)
							mapGuidType.Add(poss.Guid, poss);
					}

					// Now make sure the LayoutTreeNode has the right entries
					foreach (var info in
						from kvp in mapGuidType
						where !setGuidsFromXml.Contains(kvp.Key)
						select new ItemTypeInfo(true, kvp.Key))
					{
						ltn.EntryTypeList.Add(info);
					}
					AddUnspecifiedTypes(fstemBased, fstemBased ? 0 : m_rgVariantTypes.Count, ltn);
					break;
			}
			// Remove any obsolete types from our ordered list.
			var obsoleteItems = ltn.EntryTypeList.Where(info => !IsUnspecifiedPossibility(info) &&
				!mapGuidType.ContainsKey(info.ItemGuid)).ToList();
			foreach (var info in obsoleteItems)
				ltn.EntryTypeList.Remove(info);
			// Add the names to the items in the ordered list.
			foreach (var info in ltn.EntryTypeList)
			{
				if (IsUnspecifiedPossibility(info))
					continue;
				var poss = mapGuidType[info.ItemGuid];
				info.Name = poss.Name.BestAnalysisVernacularAlternative.Text;
			}
		}

		/// <summary>
		/// Searches a list of ItemTypeInfo items for a specific Guid (usually representing
		/// an unspecified type 'possibility'. Out variable gives the index at which the Guid is
		/// found in the list, or -1 if not found.
		/// </summary>
		/// <param name="itemTypeList"></param>
		/// <param name="searchGuid"></param>
		/// <param name="index"></param>
		/// <returns>true if found, false if not found</returns>
		private static bool ListContainsGuid(IList<ItemTypeInfo> itemTypeList, Guid searchGuid, out int index)
		{
			var ffound = false;
			index = -1;
			for (var i = 0; i < itemTypeList.Count; i++)
			{
				var info = itemTypeList[i];
				if (info.ItemGuid != searchGuid)
					continue;
				ffound = true;
				index = i;
				break;
			}
			return ffound;
		}

		private List<ICmPossibility> GetSortedFlattenedVariantTypeList()
		{
			var result = FlattenPossibilityList(
				m_cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS);
			result.Sort(ComparePossibilitiesByName);
			return result;
		}

		private List<ICmPossibility> GetSortedFlattenedComplexFormTypeList()
		{
			var result = FlattenPossibilityList(
				m_cache.LangProject.LexDbOA.ComplexEntryTypesOA.PossibilitiesOS);
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

		internal static List<ICmPossibility> FlattenPossibilityList(IEnumerable<ICmPossibility> sequence)
		{
			var list = sequence.ToList();
			foreach (var poss in sequence)
			{
				// Need to get all nested items
				list.AddRange(FlattenPossibilityList(poss.SubPossibilitiesOS));
			}
			return list;
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
			Size size = Size;
			base.OnLoad(e);
// ReSharper disable RedundantCheckBeforeAssignment
			if (Size != size)
				Size = size;
// ReSharper restore RedundantCheckBeforeAssignment
			// Now that we can 'Manage Views', we want this combo box even if Notebook only has one.
			//if (m_cbDictType.Items.Count < 2)
			//{
			//    // If there's only one choice, then hide the relevant controls, move all the other
			//    // controls up to the vacated space, and shrink the overall dialog box.
			//    RemoveDictTypeComboBox();
			//}
		}

		/// <summary>
		/// Save the location and size for next time.
		/// </summary>
		protected override void OnClosing(CancelEventArgs e)
		{
			if (m_mediator != null)
			{
				m_mediator.PropertyTable.SetProperty("XmlDocConfigureDlg_Location", Location, false);
				m_mediator.PropertyTable.SetPropertyPersistence("XmlDocConfigureDlg_Location", true);
				m_mediator.PropertyTable.SetProperty("XmlDocConfigureDlg_Size", Size, false);
				m_mediator.PropertyTable.SetPropertyPersistence("XmlDocConfigureDlg_Size", true);
			}
			base.OnClosing(e);
		}

		#region Dialog Event Handlers
		// ReSharper disable InconsistentNaming

		/// <summary>
		/// Users want to see visible spaces.  The only way to do this is to select everything,
		/// and show the selection when the focus leaves for elsewhere.
		/// </summary>
		void m_tbBefore_LostFocus(object sender, EventArgs e)
		{
			m_tbBefore.SelectAll();
		}
		void m_tbBetween_LostFocus(object sender, EventArgs e)
		{
			m_tbBetween.SelectAll();
		}
		void m_tbAfter_LostFocus(object sender, EventArgs e)
		{
			m_tbAfter.SelectAll();
		}

		/// <summary>
		/// When the focus returns, it's probably more useful to select at the end rather
		/// than selecting everything.
		/// </summary>
		void m_tbBefore_GotFocus(object sender, EventArgs e)
		{
			m_tbBefore.Select(m_tbBefore.Text.Length, 0);
		}
		void m_tbBetween_GotFocus(object sender, EventArgs e)
		{
			m_tbBetween.Select(m_tbBetween.Text.Length, 0);
		}
		void m_tbAfter_GotFocus(object sender, EventArgs e)
		{
			m_tbAfter.Select(m_tbAfter.Text.Length, 0);
		}

		private void m_cbDictType_SelectedIndexChanged(object sender, EventArgs e)
		{
			List<LayoutTreeNode> rgltn = ((LayoutTypeComboItem)m_cbDictType.SelectedItem).TreeNodes;
			m_tvParts.Nodes.Clear();
			m_tvParts.Nodes.AddRange(rgltn.ToArray());
			if (m_tvParts.Nodes.Count > 0)
				m_tvParts.SelectedNode = m_tvParts.Nodes[0];
		}

		/// <summary>
		/// Set the active node, based on a string generated by XmlVc.NodeIdentifier,
		/// which makes 3-part strings class:layoutName:partRef.
		/// </summary>
		/// <param name="nodePath"></param>
		internal void SetActiveNode(string nodePath)
		{
			if (String.IsNullOrEmpty(nodePath)) return; // has been crashing with no check
			var idParts = nodePath.Split(':');
			if (idParts.Length != 4)
				return; // throw? 0 at least is plausible
			var className = idParts[0];
			var layoutName = idParts[1];
			var partRef = idParts[2];
			foreach (LayoutTreeNode ltn in m_tvParts.Nodes)
				SetActiveNode(ltn, className, layoutName, partRef);
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
				SetActiveNode(ltnChild, className, layoutName, partRef);
		}

		private void m_btnMoveUp_Click(object sender, EventArgs e)
		{
			StoreNodeData(m_current);	// Ensure duplicate has current data.
			LayoutTreeNode ltn = (LayoutTreeNode)m_current.Clone();
			int idx = m_current.Index;
			TreeNode tnParent = m_current.Parent;
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
			LayoutTreeNode ltn = (LayoutTreeNode)m_current.Clone();
			int idx = m_current.Index;
			TreeNode tnParent = m_current.Parent;
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
				sBaseLabel = m_current.Label;
			var cDup = 1;
			var sLabel = String.Format("{0} ({1})", sBaseLabel, cDup);
			while (rgsLabels.Contains(sLabel))
			{
				++cDup;
				sLabel = String.Format("{0} ({1})", sBaseLabel, cDup);
			}
			if (String.IsNullOrEmpty(m_current.Param))
			{
				Debug.Assert(m_current.Nodes.Count == 0);
				ltnDup = m_current.CreateCopy();
			}
			else
			{
				ltnDup = DuplicateLayoutSubtree(cDup);
				if (ltnDup == null)
					return;
			}
			ltnDup.Label = sLabel;		// sets Text as well.
			var sDup = ltnDup.DupString;
			sDup = String.IsNullOrEmpty(sDup) ? cDup.ToString() : String.Format("{0}-{1}", sDup, cDup);
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
				if (ltn.Configuration["ref"] == m_current.Configuration["ref"] &&
					ltn.LayoutName == m_current.LayoutName &&
					ltn.PartName == m_current.PartName)
				{
					rgsLabels.Add(ltn.Label);
					if (!ltn.IsDuplicate)
						sBaseLabel = ltn.Label;
				}
			}
			return sBaseLabel;
		}

		private LayoutTreeNode DuplicateLayoutSubtree(int iDup)
		{
			var sDupKey = String.Format("{0:D2}", iDup);

			var suffixCode = String.Format("{0}{1}", LayoutKeyUtils.kcMarkNodeCopy, sDupKey);
			var sRef = XmlUtils.GetOptionalAttributeValue(m_current.Configuration, "ref");
			var xnPart = m_parts.GetElement("part", new[] { String.Format("{0}-Jt-{1}", m_current.ClassName, sRef) });
			if (xnPart == null)
				return null;		// shouldn't happen.

			var duplicates = new List<XmlNode>();
			ProcessPartChildrenForDuplication(m_current.ClassName, m_current.Configuration,
				xnPart.ChildNodes, suffixCode, duplicates);
			foreach (var xn in duplicates)
				m_layouts.AddNodeToInventory(xn);

			var ltnDup = m_current.CreateCopy();
			AdjustAttributeValue(ltnDup.Configuration, "param", suffixCode);
			ltnDup.Param = AdjustLayoutName(ltnDup.Param, suffixCode);
			if (ltnDup.RelTypeList != null && ltnDup.RelTypeList.Count > 0)
			{
				var repoLexRefType = m_cache.ServiceLocator.GetInstance<ILexRefTypeRepository>();
				foreach (var lri in ltnDup.RelTypeList)
				{
					var lrt = repoLexRefType.GetObject(lri.ItemGuid);
					if (lri.SubClass == LexReferenceInfo.TypeSubClass.Reverse)
						lri.Name = lrt.ReverseName.BestAnalysisVernacularAlternative.Text;
					else
						lri.Name = lrt.Name.BestAnalysisVernacularAlternative.Text;
				}
			}
			if (ltnDup.EntryTypeList != null && ltnDup.EntryTypeList.Count > 0)
			{
				var repoPoss = m_cache.ServiceLocator.GetInstance<ICmPossibilityRepository>();
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
				LegacyConfigurationUtils.AddChildNodes(duplicates[0], ltnDup, 0, this);
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
		/// Processes any part children for duplication purposes, does nothing on empty xmlNodeLists
		/// </summary>
		/// <param name="className"></param>
		/// <param name="xnCaller"></param>
		/// <param name="xmlNodeList"></param>
		/// <param name="suffixCode"></param>
		/// <param name="duplicates"></param>
		private void ProcessPartChildrenForDuplication(string className, XmlNode xnCaller,
			XmlNodeList xmlNodeList, string suffixCode, List<XmlNode> duplicates)
		{
			foreach (XmlNode xn in xmlNodeList)
			{
				if (xn is XmlComment)
					continue;
				if (xn.Name == "obj" || xn.Name == "seq" || xn.Name == "objlocal")
				{
					ProcessPartChildForDuplication(className, xnCaller, xn, suffixCode, duplicates);
				}
				else
				{
					ProcessPartChildrenForDuplication(className, xnCaller, xn.ChildNodes, suffixCode, duplicates);
				}
			}
		}

		private void ProcessPartChildForDuplication(string className, XmlNode xnCaller,
			XmlNode xnField, string suffixCode, List<XmlNode> duplicates)
		{
			var sLayoutName = XmlUtils.GetManditoryAttributeValue(xnCaller, "param");
			var fRecurse = XmlUtils.GetOptionalBooleanAttributeValue(xnCaller, "recurseConfig", true);
			if (!fRecurse)
			{
				// We don't want to recurse forever just because senses have subsenses, which
				// can have subsenses, which can ...
				// Or because entries have subentries (in root type layouts)...
				return;
			}
			var sField = XmlUtils.GetManditoryAttributeValue(xnField, "field");
			var clidDst = 0;
			string sClass = null;
			string sTargetClasses = null;
			try
			{
				// Failure should be fairly unusual, but, for example, part MoForm-Jt-FormEnvPub attempts to display
				// the property PhoneEnv inside an if that checks that the MoForm is one of the subclasses that has
				// the PhoneEnv property. MoForm itself does not.
				var mdc = (FDO.Infrastructure.IFwMetaDataCacheManaged)m_cache.DomainDataByFlid.MetaDataCache;
				if (!mdc.FieldExists(className, sField, true))
					return;
				var flid = m_cache.DomainDataByFlid.MetaDataCache.GetFieldId(className, sField, true);
				var type = (CellarPropertyType)m_cache.DomainDataByFlid.MetaDataCache.GetFieldType(flid);
				Debug.Assert(type >= CellarPropertyType.MinObj);
				if (type >= CellarPropertyType.MinObj)
				{
					sTargetClasses = XmlUtils.GetOptionalAttributeValue(xnField, "targetclasses");
					clidDst = m_mdc.GetDstClsId(flid);
					if (clidDst == 0)
						sClass = XmlUtils.GetOptionalAttributeValue(xnField, "targetclass");
					else
						sClass = m_mdc.GetClassName(clidDst);
					if (clidDst == StParaTags.kClassId)
					{
						var sClassT = XmlUtils.GetOptionalAttributeValue(xnField, "targetclass");
						if (!String.IsNullOrEmpty(sClassT))
							sClass = sClassT;
					}
				}
			}
			catch
			{
				return;
			}
			if (clidDst == MoFormTags.kClassId && !sLayoutName.StartsWith("publi"))
				return;	// ignore the layouts used by the LexEntry-Jt-Headword part.
			if (String.IsNullOrEmpty(sLayoutName) || String.IsNullOrEmpty(sClass))
				return;
			if (sTargetClasses == null)
				sTargetClasses = sClass;
			var rgsClasses = sTargetClasses.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
			XmlNode xnLayout = null;
			if (rgsClasses.Length > 0)
				xnLayout = m_layouts.GetElement("layout", new[] { rgsClasses[0], "jtview", sLayoutName, null });

			if (xnLayout != null)
			{
				var cNodes = xnLayout.ChildNodes.Count;
				DuplicateLayout(xnLayout.Clone(), suffixCode, duplicates);
				var fRepeatedConfig = XmlUtils.GetOptionalBooleanAttributeValue(xnField, "repeatedConfig", false);
				if (fRepeatedConfig)
					return;		// repeats an earlier part element (probably as a result of <if>s)
				for (var i = 1; i < rgsClasses.Length; i++)
				{
					var xnMergedLayout = m_layouts.GetElement("layout", new[] { rgsClasses[i], "jtview", sLayoutName, null });
					if (xnMergedLayout != null && xnMergedLayout.ChildNodes.Count == cNodes)
						DuplicateLayout(xnMergedLayout.Clone(), suffixCode, duplicates);
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
					var msg = String.Format("Missing jtview layout for class=\"{0}\" name=\"{1}\"",
						rgsClasses[0], sLayoutName);
					Debug.Assert(xnLayout != null, msg);
					//Debug.WriteLine(msg);
				}
			}
		}

		/// <summary>
		/// Fix the cloned layout for use as a duplicate.
		/// </summary>
		/// <param name="xnLayout">an unmodified clone of the original node</param>
		/// <param name="suffixCode">tag to append to the name attribute, and param attributes of child nodes</param>
		/// <param name="duplicates">list to add the modified layout to</param>
		private void DuplicateLayout(XmlNode xnLayout, string suffixCode, List<XmlNode> duplicates)
		{
			// It is important for at least one caller that the FIRST node added to duplicates is the copy of xnLayout.
			duplicates.Add(xnLayout);
			AdjustAttributeValue(xnLayout, "name", suffixCode);
			var className = XmlUtils.GetManditoryAttributeValue(xnLayout, "class");

			foreach (XmlNode partref in xnLayout.ChildNodes)
			{
				if (partref.Name == "part")
				{
					var param = XmlUtils.GetOptionalAttributeValue(partref, "param");
					if (!String.IsNullOrEmpty(param))
					{
						var sRef = XmlUtils.GetManditoryAttributeValue(partref, "ref");
						var xnPart = m_parts.GetElement("part", new[] {String.Format("{0}-Jt-{1}", className, sRef)});
						if (xnPart == null)
						{
							Debug.Assert(xnPart != null, @"XMLDocConfigure::DuplicateLayout - Failed to process "
										+ String.Format("{0}-Jt-{1}", className, sRef)
										+ @" likely an invalid layout.");
						}
						else
						{
							ProcessPartChildrenForDuplication(className, partref, xnPart.ChildNodes, suffixCode, duplicates);
						}
						AdjustAttributeValue(partref, "param", suffixCode);
					}
				}
				else if (partref.Name == "sublayout")
				{
					var sublayout = XmlUtils.GetManditoryAttributeValue(partref, "name");
					var xnSublayout = m_layouts.GetElement("layout", new[] {className, "jtview", sublayout, null});
					DuplicateLayout(xnSublayout.Clone(), suffixCode, duplicates);
					AdjustAttributeValue(partref, "name", suffixCode);
				}
			}
		}

		private static void AdjustAttributeValue(XmlNode node, string sAttrName, string suffixCode)
		{
			var xa = node.Attributes != null ? node.Attributes[sAttrName] : null;
			Debug.Assert(xa != null);
			xa.Value = AdjustLayoutName(xa.Value, suffixCode);
		}

		private static string AdjustLayoutName(string sName, string suffixCode)
		{
			Debug.Assert(!String.IsNullOrEmpty(sName));
			var cTag = suffixCode[0];
			int idx;
			if (cTag == LayoutKeyUtils.kcMarkNodeCopy)
			{
				var idx0 = suffixCode.IndexOf(LayoutKeyUtils.kcMarkLayoutCopy);
				if (idx0 < 0)
					idx0 = 0;
				idx = sName.IndexOf(cTag, idx0);
			}
			else
			{
				idx = sName.IndexOf(cTag);
			}
			if (idx > 0)
				sName = sName.Remove(idx);
			return String.Format("{0}{1}", sName, suffixCode);
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

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		private void m_btnRestoreDefaults_Click(object sender, EventArgs e)
		{
			using (new WaitCursor(this))
			{
				LayoutCache.InitializePartInventories(null, (IApp)m_mediator.PropertyTable.GetValue("App"),
					m_cache.ProjectId.ProjectFolder);
				Inventory layouts = Inventory.GetInventory("layouts", null);
				Inventory parts = Inventory.GetInventory("parts", null);
				//preserve layouts which are marked as copies
				var layoutTypes = m_layouts.GetLayoutTypes();
				var layoutCopies = m_layouts.GetElements("//layout[contains(@name, '#')]");
				foreach (var type in layoutTypes)
				{
					layouts.AddLayoutTypeToInventory(type);
				}
				foreach (var layoutCopy in layoutCopies)
				{
					var copy = layoutCopy as XmlNode;
					layouts.AddNodeToInventory(copy);
				}
				m_layouts = layouts;
				m_parts = parts;
				// recreate the layout trees.
				var selItem = m_cbDictType.SelectedItem as LayoutTypeComboItem;
				var selLabel = selItem == null ? null : selItem.Label;
				var selLayout = selItem == null ? null : selItem.LayoutName;
				m_cbDictType.Items.Clear();
				m_tvParts.Nodes.Clear();
				var configureLayouts = XmlUtils.FindNode(m_configurationParameters, "configureLayouts");
				LegacyConfigurationUtils.BuildTreeFromLayoutAndParts(configureLayouts, this);
				if (selItem != null)
				{
					// try to select the same view.
					foreach (var item in m_cbDictType.Items)
					{
						var ltci = item as LayoutTypeComboItem;
						if (ltci != null && ltci.Label == selLabel && ltci.LayoutName == selLayout)
						{
							m_cbDictType.SelectedItem = ltci;
							break;
						}
					}
				}
				m_fDeleteCustomFiles = true;
			}
		}

		private void m_btnOK_Click(object sender, EventArgs e)
		{
			if (IsDirty())
			{
				m_mediator.PropertyTable.SetProperty(m_sLayoutPropertyName,
					((LayoutTypeComboItem)m_cbDictType.SelectedItem).LayoutName, true,
					PropertyTable.SettingsGroup.LocalSettings);
				m_mediator.PropertyTable.SetPropertyPersistence(m_sLayoutPropertyName, true,
					PropertyTable.SettingsGroup.LocalSettings);
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
			ShowHelp.ShowHelpTopic(m_mediator.HelpTopicProvider, m_helpTopicID);
		}

		/// <summary>
		/// Fire up the Styles dialog, and if necessary, reload the related combobox.
		/// </summary>
		private void m_btnStyles_Click(object sender, EventArgs e)
		{
			HandleStylesBtn(m_cbCharStyle, null, "");
		}

		/// <summary>
		/// Fire up the Styles dialog, and if necessary, reload the related combobox.
		/// </summary>
		private void m_btnBeforeStyles_Click(object sender, EventArgs e)
		{
			HandleStylesBtn(m_cbBeforeStyle, null, "");
		}

		/// <summary>
		/// Returns the name of the style the user would like to select (null or empty if canceled).
		/// </summary>
		private void HandleStylesBtn(ComboBox combo, Action fixCombo, string defaultStyle)
		{
			FwStylesDlg.RunStylesDialogForCombo(combo,
				() => {
					// Reload the lists for the styles combo boxes, and redisplay the controls.
					SetStylesLists();
					if (m_btnStyles.Visible)
						DisplayStyleControls(m_current.AllParentsChecked);
					if (m_btnBeforeStyles.Visible)
						DisplayBeforeStyleControls(m_current.AllParentsChecked);
					if (fixCombo != null)
						fixCombo();
				},
				defaultStyle, m_styleSheet,
				m_callbacks != null ? m_callbacks.MaxStyleLevelToShow : 0,
				m_callbacks != null ? m_callbacks.HvoAppRootObject : 0,
				m_cache, this, ((IApp)m_mediator.PropertyTable.GetValue("App")),
				m_mediator.HelpTopicProvider);
		}

		/// <summary>
		/// Move to the first child node when this "Configure Now" link text is clicked.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
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
				if (!m_current.IsTopLevel)
				{
					if (m_current.Nodes.Count > 0)
						DisplayDetailsForAParentNode(m_current.AllParentsChecked);
					else if (m_current.UseParentConfig)
						DisplayDetailsForRecursiveNode(m_current.AllParentsChecked);
					else
						DisplayDetailsForLeafNode(m_current.AllParentsChecked);
					DisplayBeforeStyleControls(m_current.AllParentsChecked);
				}
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
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void m_chkDisplayData_CheckedChanged(object sender, EventArgs e)
		{
			if (m_current != null)
				m_current.Checked = m_chkDisplayData.Checked;
		}

		void m_chkComplexFormsAsParagraphs_CheckedChanged(object sender, EventArgs e)
		{
			if (m_current != null)
			{   // Are complex forms to be shown in their own line?
				var fChecked = m_chkComplexFormsAsParagraphs.Checked;
				m_current.ShowComplexFormPara = fChecked;
				ShowComplexFormRelatedControls(fChecked);
				if (fChecked)
				{   // All Senses must now be shown in separate paragraphs
					var ltnParent = m_current.Parent as LayoutTreeNode;
					if (ltnParent != null)
					{
						ltnParent.ShowSenseAsPara = true;
					}
				}
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
		};
		private ListViewContent m_listType = ListViewContent.Hidden;

		void OnListItemsSelectedIndexChanged(object sender, EventArgs e)
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
			int idx = sic[0];
			if (idx == 0)
				return 0;
			object tagPrev = m_lvItems.Items[idx - 1].Tag;
			return tagPrev is ILgWritingSystem ? idx : 0;
		}

		private int GetIndexToMoveWsDown(ListView.SelectedIndexCollection sic)
		{
			int idxMax = m_lvItems.Items.Count - 1;
			int idx = sic[0];
			if (idx >= idxMax)
				return idxMax;
			object tagSel = m_lvItems.Items[idx].Tag;
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
				return;
			try
			{
				m_fItemCheck = true;

				int cChecks;
				ListView.CheckedListViewItemCollection lvic = m_lvItems.CheckedItems;
				if (lvic.Count == 0)
				{
					// I don't think this can happen, but just in case...
					if (e.NewValue == CheckState.Unchecked)
						e.NewValue = CheckState.Checked;
					return;
				}
				ListViewItem lviEvent = m_lvItems.Items[e.Index];
				if (e.NewValue == CheckState.Checked)
				{
					cChecks = lvic.Count + 1;
					for (int i = lvic.Count - 1; i >= 0; --i)
					{
						if (lviEvent.Tag is ILgWritingSystem)
						{
							if (lvic[i].Tag is int)
							{
								lvic[i].Checked = false;	// Uncheck any magic ws items.
								--cChecks;
							}
						}
						else
						{
							if (lvic[i].Index != e.Index)
							{
								lvic[i].Checked = false;	// uncheck any other items.
								--cChecks;
							}
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
		void OnItemTypeItemCheck(ItemCheckEventArgs e)
		{
			if (m_fItemCheck)
				return;
			try
			{
				m_fItemCheck = true;
				int cChecks;
				var lvic = m_lvItems.CheckedItems;
				if (lvic.Count == 0)
				{
					// I don't think this can happen, but just in case...
					if (e.NewValue == CheckState.Unchecked)
						e.NewValue = CheckState.Checked;
				}
				else
				{
					var lviEvent = m_lvItems.Items[e.Index];
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
				StoreNodeData(m_current);

			// Set up the dialog for editing the current node's layout.
			m_current = e.Node as LayoutTreeNode;
			DisplayCurrentNodeDetails();
		}

		private void DisplayCurrentNodeDetails()
		{
			bool fEnabled = true;
			string sHeading = m_current.Text;
			TreeNode tnParent = m_current.Parent;
			if (tnParent != null)
			{
				fEnabled = m_current.AllParentsChecked;
				sHeading = String.Format(xWorksStrings.ksHierarchyLabel, tnParent.Text, sHeading);
			}
			// This is kind of kludgy but it's the best I can think of.
			var partName = m_current.PartName.ToLowerInvariant();
			m_linkConfigureHomograph.Visible = partName.Contains("headword") || partName.Contains("owneroutline");
			// Use the text box if the label is too wide. See LT-9281.
			m_lblPanel.Text = sHeading;
			m_tbPanelHeader.Text = sHeading;
			if (m_lblPanel.Location.X + m_lblPanel.PreferredWidth >
				panel1.ClientSize.Width - (panel1.Margin.Left + panel1.Margin.Right))
			{
				m_lblPanel.Enabled = false;
				m_lblPanel.Visible = false;
				if (tnParent != null)
					m_tbPanelHeader.Text = String.Format(xWorksStrings.ksHierarchyLabel2, tnParent.Text, m_current.Text);
				else
					m_tbPanelHeader.Text = sHeading;
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
						DisplayDetailsForRecursiveNode(fEnabled);
					else
						DisplayDetailsForLeafNode(fEnabled);
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
				return;
			Debug.Assert((newIndex >= 0) && (newIndex < m_lvItems.Items.Count));
			var ltiSel = m_lvItems.Items[idx];
			var itemSelected = ltiSel.Selected;
			var lti = (ListViewItem) ltiSel.Clone();
			m_lvItems.BeginUpdate();
			m_lvItems.Items.RemoveAt(idx);
			m_lvItems.Items.Insert(newIndex, lti);
			if (itemSelected)
				lti.Selected = true;
			m_lvItems.EndUpdate();
		}

		/// <summary>
		/// Generates a possible help topic id from an identifying string, but does NOT check it for validity!
		/// </summary>
		/// <returns></returns>
		private string generateChooserHelpTopicID(string fromStr)
		{
			string candidateID = "khtpConfig";

			// Should we capitalize the next letter?
			bool nextCapital = true;

			// Lets turn our field into a candidate help page!
			foreach (char ch in fromStr)
			{
				if (Char.IsLetterOrDigit(ch)) // might we include numbers someday?
				{
					if (nextCapital)
						candidateID += Char.ToUpper(ch);
					else
						candidateID += ch;
					nextCapital = false;
				}
				else // unrecognized character... exclude it
					nextCapital = true; // next letter should be a capital
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
				return false;
			int idx = m_current.Index;
			if (idx <= 0)
				return false;
			XmlNode xnParent = m_current.HiddenNodeLayout;
			if (xnParent == null)
				xnParent = m_current.ParentLayout;
			LayoutTreeNode ltnPrev = (LayoutTreeNode)m_current.Parent.Nodes[idx-1];
			XmlNode xnPrevParent = ltnPrev.HiddenNodeLayout;
			if (xnPrevParent == null)
				xnPrevParent = ltnPrev.ParentLayout;
			return xnParent == xnPrevParent;
		}

		/// <summary>
		/// The MoveDown button is enabled only when the current node is not the last child of
		/// its parent tree node, and the following node comes from the same actual layout.
		/// (Remember, the parts from a sublayout node appear as children of the the same tree
		/// node as any parts from the layout containing the sublayout.)
		/// </summary>
		/// <returns></returns>
		private bool EnableMoveDownButton()
		{
			if (m_current.Level == 0)
				return false;
			int idx = m_current.Index;
			if (idx >= m_current.Parent.Nodes.Count - 1)
				return false;
			XmlNode xnParent = m_current.HiddenNodeLayout;
			if (xnParent == null)
				xnParent = m_current.ParentLayout;
			LayoutTreeNode ltnNext = (LayoutTreeNode)m_current.Parent.Nodes[idx+1];
			XmlNode xnNextParent = ltnNext.HiddenNodeLayout;
			if (xnNextParent == null)
				xnNextParent = ltnNext.ParentLayout;
			return xnParent == xnNextParent;
		}

		private void StoreNodeData(LayoutTreeNode ltn)
		{
			ltn.ContentVisible = ltn.Checked;
			if (m_tbBefore.Visible && m_tbBefore.Enabled)
				ltn.Before = m_tbBefore.Text;
			else
				ltn.Before = ""; // if it's invisible don't let any non-empty value be saved.
			if (m_tbBetween.Visible && m_tbBetween.Enabled)
				ltn.Between = m_tbBetween.Text;
			else
				ltn.Between = ""; // if it's invisible don't let any non-empty value be saved.
			if (m_tbAfter.Visible && m_tbAfter.Enabled)
				ltn.After = m_tbAfter.Text;
			else
				ltn.After = ""; // if it's invisible don't let any non-empty value be saved.
			if (m_chkDisplayWsAbbrs.Visible)
				ltn.ShowWsLabels = m_chkDisplayWsAbbrs.Checked && m_chkDisplayWsAbbrs.Enabled;
			if (m_cfgSenses.Visible)
				StoreSenseConfigData(ltn);
			if (m_chkShowSingleGramInfoFirst.Visible)
				StoreGramInfoData(ltn);
			if (m_chkComplexFormsAsParagraphs.Visible)
				StoreComplexFormData(ltn);
			if (m_cbCharStyle.Visible && m_cbCharStyle.Enabled)
			{
				var sci = m_cbCharStyle.SelectedItem as StyleComboItem;
				if (sci != null && sci.Style != null)
					ltn.StyleName = sci.Style.Name;
				else
					ltn.StyleName = String.Empty;
			}
			if (m_cbBeforeStyle.Visible && m_cbBeforeStyle.Enabled)
			{
				var sci = m_cbBeforeStyle.SelectedItem as StyleComboItem;
				if (sci != null && sci.Style != null)
					ltn.BeforeStyleName = sci.Style.Name;
				else
					ltn.BeforeStyleName = String.Empty;
			}
			if (m_lvItems.Visible && m_lvItems.Enabled)
				ltn.WsLabel = GenerateWsLabelFromListView();
			MakeParentParaIfDivInParaVisible(ltn);
		}

		private void MakeParentParaIfDivInParaVisible(LayoutTreeNode ltn)
		{
			if (ltn.Checked && ltn.FlowType == "divInPara")
			{
				var ltnParent = ltn.Parent as LayoutTreeNode;
				if (ltnParent != null)
				{
					ltnParent.ShowSenseAsPara = true;
					if (ltnParent == m_current)
					{
						DisplayCurrentNodeDetails();
					}
				}
			}
		}

		private void MakeDivInParaChildNotVisible(LayoutTreeNode ltn, object sender)
		{
			// applies to Root dictionary ltn=Senses when ShowSenseAsPara=false
			var lts = sender as ConfigSenseLayout;
			if (lts == null) return;
			if (lts.DisplaySenseInPara) return;
			// find Stem: Main Entry-Senses-Visible Complex Forms ltn
			foreach (TreeNode n in ltn.Nodes) // iterate over child nodes
			{
				LayoutTreeNode tn = n as LayoutTreeNode;
				if (tn != null)
				{
					// meant for tn.Label == "Subentries" in root dictionary
					if (tn.Checked && tn.FlowType == "divInPara")
					{
						var resources = new ComponentResourceManager(typeof(XmlDocConfigureDlg));

						MessageBox.Show(resources.GetString("k_RootSenseOnSubentriesGoneDlgText"),
							resources.GetString("k_RootSenseOnSubentriesGoneDlgLabel"),
							MessageBoxButtons.OK, MessageBoxIcon.Information);

						tn.Checked = false;
						break;
					}
					// meant for tn.Label.Contains("Referenced Complex Form") in stem dictionary
					if (tn.Checked && tn.FlowType == "span" &&
							 tn.Label.Contains(xWorksStrings.ksReferencedComplexForm) &&
							 tn.ShowComplexFormPara)
					{
						tn.ShowComplexFormPara = false;
						break;
					}
				}
			}
		}

		private string GenerateWsLabelFromListView()
		{
			var sbLabel = new StringBuilder();
			foreach (ListViewItem lvi in m_lvItems.CheckedItems)
			{
				string sWs = String.Empty;
				if (lvi.Tag is int)
				{
					int ws = (int)lvi.Tag;
					WritingSystemServices.SmartMagicWsToSimpleMagicWs(ws);
					sWs = WritingSystemServices.GetMagicWsNameFromId(ws);
				}
				else if (lvi.Tag is IWritingSystem)
				{
					sWs = ((IWritingSystem) lvi.Tag).Id;
				}
				if (sbLabel.Length > 0)
					sbLabel.Append(",");
				sbLabel.Append(sWs);
			}
			return sbLabel.ToString();
		}

		private void StoreSenseConfigData(LayoutTreeNode ltn)
		{
			if (m_cfgSenses.NumberStyleCombo.SelectedIndex == 0)
			{
				ltn.Number = "";
			}
			else
			{
				ltn.Number = m_cfgSenses.BeforeNumber +
					((NumberingStyleComboItem)m_cfgSenses.NumberStyleCombo.SelectedItem).FormatString +
					m_cfgSenses.AfterNumber;
				ltn.NumStyle = GenerateNumStyleFromCheckBoxes();
				ltn.NumFont = m_cfgSenses.NumberFontCombo.SelectedItem.ToString();	// item is a string actually...
				if (ltn.NumFont == xWorksStrings.ksUnspecified)
					ltn.NumFont = String.Empty;
				ltn.NumberSingleSense = m_cfgSenses.NumberSingleSense;
			}
			ltn.ShowSingleGramInfoFirst = m_chkShowSingleGramInfoFirst.Checked;
			// Set the information on the child grammatical info node as well.
			foreach (TreeNode n in ltn.Nodes)
			{
				LayoutTreeNode tn = n as LayoutTreeNode;
				if (tn != null && tn.ShowGramInfoConfig)
				{
					tn.ShowSingleGramInfoFirst = m_chkShowSingleGramInfoFirst.Checked;
					break;
				}
			}
			ltn.ShowSenseAsPara = m_cfgSenses.DisplaySenseInPara;
			ltn.SenseParaStyle = m_cfgSenses.SenseParaStyle;
			//ltn.SingleSenseStyle = m_cfgSenses.SingleSenseStyle;
		}

		private void StoreGramInfoData(LayoutTreeNode ltn)
		{
			if (m_cfgSenses.Visible)
				return;
			ltn.ShowSingleGramInfoFirst = m_chkShowSingleGramInfoFirst.Checked;
			// Set the information on the parent sense node as well.
			var ltnParent = ltn.Parent as LayoutTreeNode;
			if (ltnParent != null && ltnParent.ShowSenseConfig)
				ltnParent.ShowSingleGramInfoFirst = m_chkShowSingleGramInfoFirst.Checked;
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
				DisplaySenseConfigControls(fEnabled);
			else
				HideSenseConfigControls();

			if (m_current.ShowGramInfoConfig)
				DisplayGramInfoConfigControls(fEnabled);
			else
				HideGramInfoConfigControls();

			if (m_current.ShowComplexFormParaConfig)
				DisplayComplexFormConfigControls(fEnabled);
			else
				HideComplexFormConfigControls();

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

			string sMoreDetail = String.Format(xWorksStrings.ksCanBeConfiguredInMoreDetail,
				m_current.Text);
			m_cfgParentNode.Visible = true;
			m_cfgParentNode.SetDetails(sMoreDetail, m_chkDisplayData.Checked && fEnabled, true);
			if (m_chkDisplayData.Checked && fEnabled)
			{
				m_current.Expand();
				foreach (LayoutTreeNode child in m_current.Nodes)
				{
					if (XmlUtils.GetOptionalBooleanAttributeValue(child.Configuration, "autoexp", false))
					{
						child.Expand();
					}
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
				DisplayStyleControls(fEnabled);

			string sMoreDetail = String.Format(xWorksStrings.ksUsesTheSameConfigurationAs,
				m_current.Text, m_current.Configuration.Attributes["recurseConfigLabel"] != null ? m_current.Configuration.Attributes["recurseConfigLabel"].Value
																								  : m_current.Parent.Text);
			m_cfgParentNode.Visible = true;
			m_cfgParentNode.SetDetails(sMoreDetail, m_chkDisplayData.Checked && fEnabled, false);
		}

		private bool IsSubsenseNode
		{
			get
			{
				if (m_current == null || m_current.Parent == null)
					return false;
				return ((LayoutTreeNode) m_current.Parent).ShowSenseConfig;
			}
		}

		private bool IsReversalLayout
		{
			get
			{
				return m_defaultRootLayoutName != null && m_defaultRootLayoutName.Contains("Reversal");
			}
		}

		private void DisplaySenseConfigControls(bool fEnabled)
		{
			// Don't show the "Show single gram info first" checkbox control for the
			// subsense display or for a reversal layout.
			m_chkShowSingleGramInfoFirst.Visible = !IsReversalLayout;

			// Handle possibility of subsense node including above checkbox
			if (IsSubsenseNode)
				HideSenseConfigControls();
			else
				ShowSenseConfigControls();

			m_cfgSenses.DisplaySenseInPara = m_current.ShowSenseAsPara;
			// Otherwise, sometimes Context controls come up invisible when they shouldn't.
			m_current.After = m_current.After ?? "";
			m_current.Before = m_current.Before ?? "";
			m_current.Between = m_current.Between ?? "";
			DetermineStateOfContextControls();

			// Arrange placement of Surrounding Context control after making the above (perhaps) visible.
			PlaceContextControls(m_cfgSenses);
			string sBefore, sMark, sAfter;
			m_current.SplitNumberFormat(out sBefore, out sMark, out sAfter);
			if (m_current.Number == "")
				m_cfgSenses.NumberStyleCombo.SelectedIndex = 0;
			else if (sMark == "%O")
				m_cfgSenses.NumberStyleCombo.SelectedIndex = 1;
			else
				m_cfgSenses.NumberStyleCombo.SelectedIndex = 2;
			m_cfgSenses.BeforeNumber = sBefore;
			m_cfgSenses.AfterNumber = sAfter;
			SetNumStyleCheckStates();
			if (String.IsNullOrEmpty(m_current.NumFont))
			{
				m_cfgSenses.NumberFontCombo.SelectedIndex = 0;
				m_cfgSenses.NumberFontCombo.SelectedText = "";
			}
			else
			{
				for (var i = 0; i < m_cfgSenses.NumberFontCombo.Items.Count; ++i)
				{
					if (m_current.NumFont != m_cfgSenses.NumberFontCombo.Items[i].ToString())
						continue;
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
			CheckState csBold = CheckState.Indeterminate;
			CheckState csItalic = CheckState.Indeterminate;
			string sStyle = m_current.NumStyle;
			if (!String.IsNullOrEmpty(sStyle))
			{
				sStyle = sStyle.ToLowerInvariant();
				if (sStyle.IndexOf("-bold") >= 0)
					csBold = CheckState.Unchecked;
				else if (sStyle.IndexOf("bold") >= 0)
					csBold = CheckState.Checked;
				if (sStyle.IndexOf("-italic") >= 0)
					csItalic = CheckState.Unchecked;
				else if (sStyle.IndexOf("italic") >= 0)
					csItalic = CheckState.Checked;
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
				m_dyOriginalTopOfContext = this.Height - m_lblContext.Top;
			}
			int desiredTop = belowThis == null ? this.Height - m_dyOriginalTopOfContext : belowThis.Bottom + m_dyContextOffset;
			int diff = desiredTop - m_lblContext.Top;
			var contextControls = new Control[] {m_lblContext, m_lblBefore, m_lblBetween, m_lblAfter, m_tbBefore, m_tbBetween, m_tbAfter};
			// If we're putting it below something fixed, it should not move if the dialog resizes.
			// If we're putting it the original distance from the bottom, it should.
			foreach (var control in contextControls)
				control.Anchor = AnchorStyles.Left | (belowThis == null ? AnchorStyles.Bottom : AnchorStyles.Top);
			if (diff == 0)
				return;
			foreach (var control in contextControls)
				MoveControlVertically(control, diff);
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
				return;
			m_chkShowSingleGramInfoFirst.Visible = false;
		}

		private void DisplayComplexFormConfigControls(bool fEnabled)
		{
			m_chkComplexFormsAsParagraphs.Visible = true;
			m_chkComplexFormsAsParagraphs.Checked = m_current.ShowComplexFormPara;
			m_chkComplexFormsAsParagraphs.Enabled = fEnabled;
			if (fEnabled)
				ShowComplexFormRelatedControls(m_current.ShowComplexFormPara);
		}

		private void ShowComplexFormRelatedControls(bool fChecked)
		{
			m_btnStyles.Visible = fChecked;
			m_cbCharStyle.Visible = fChecked;
			m_lblCharStyle.Visible = fChecked;
			if (fChecked)
			{
				m_lblCharStyle.Text = xWorksStrings.ksParagraphStyle;
				// Default to the "Dictionary-Subentry" style.  See LT-11453.
				if (m_current != null && String.IsNullOrEmpty(m_current.StyleName))
					m_current.StyleName = "Dictionary-Subentry";
				SetParagraphStyles();
				// Get rid of the "none" option, if it exists.  See LT-11453.
				for (var i = 0; i < m_cbCharStyle.Items.Count; ++i)
				{
					var sci = m_cbCharStyle.Items[i] as StyleComboItem;
					if (sci == null || sci.Style != null)
						continue;
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
				DisplaySenseConfigControls(fEnabled);
			else
				HideSenseConfigControls();
			m_chkDisplayWsAbbrs.Visible = m_lvItems.Visible;
			if (m_chkDisplayWsAbbrs.Visible)
			{
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
			m_lblItemsList.Text = xWorksStrings.ksWritingSystems;
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
			int wsDefault2 = 0;
			switch (m_current.WsType)
			{
				case "analysis":
					lvi = new ListViewItem(xWorksStrings.ksDefaultAnalysis);
					wsDefault = WritingSystemServices.kwsAnal;
					lvi.Tag = wsDefault;
					m_lvItems.Items.Add(lvi);
					foreach (IWritingSystem ws in m_cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems)
						m_lvItems.Items.Add(new ListViewItem(ws.DisplayLabel) {Tag = ws});
					break;
				case "vernacular":
					lvi = new ListViewItem(xWorksStrings.ksDefaultVernacular);
					wsDefault = WritingSystemServices.kwsVern;
					lvi.Tag = wsDefault;
					m_lvItems.Items.Add(lvi);
					foreach (IWritingSystem ws in m_cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems)
						m_lvItems.Items.Add(new ListViewItem(ws.DisplayLabel) {Tag = ws});
					break;
				case "pronunciation":
					lvi = new ListViewItem(xWorksStrings.ksDefaultPronunciation);
					wsDefault = WritingSystemServices.kwsPronunciation;
					lvi.Tag = wsDefault;
					m_lvItems.Items.Add(lvi);
					foreach (IWritingSystem ws in m_cache.ServiceLocator.WritingSystems.CurrentPronunciationWritingSystems)
						m_lvItems.Items.Add(new ListViewItem(ws.DisplayLabel) { Tag = ws });
					break;
				case "reversal":
					lvi = new ListViewItem(xWorksStrings.ksCurrentReversal);
					wsDefault = WritingSystemServices.kwsReversalIndex;
					lvi.Tag = wsDefault;
					m_lvItems.Items.Add(lvi);
					foreach (IWritingSystem ws in m_cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems)
						m_lvItems.Items.Add(new ListViewItem(ws.DisplayLabel) {Tag = ws});
					break;
				case "analysis vernacular":
					lvi = new ListViewItem(xWorksStrings.ksDefaultAnalysis);
					wsDefault = WritingSystemServices.kwsAnal;
					lvi.Tag = wsDefault;
					m_lvItems.Items.Add(lvi);
					lvi = new ListViewItem(xWorksStrings.ksDefaultVernacular);
					wsDefault2 = WritingSystemServices.kwsVern;
					lvi.Tag = wsDefault2;
					m_lvItems.Items.Add(lvi);
					foreach (IWritingSystem ws in m_cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems)
						m_lvItems.Items.Add(new ListViewItem(ws.DisplayLabel) {Tag = ws});
					foreach (IWritingSystem ws in m_cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems)
						m_lvItems.Items.Add(new ListViewItem(ws.DisplayLabel) {Tag = ws});
					break;
				default:	// "vernacular analysis"
					lvi = new ListViewItem(xWorksStrings.ksDefaultVernacular);
					wsDefault = WritingSystemServices.kwsVern;
					lvi.Tag = wsDefault;
					m_lvItems.Items.Add(lvi);
					lvi = new ListViewItem(xWorksStrings.ksDefaultAnalysis);
					wsDefault2 = WritingSystemServices.kwsAnal;
					lvi.Tag = wsDefault2;
					m_lvItems.Items.Add(lvi);
					foreach (IWritingSystem ws in m_cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems)
						m_lvItems.Items.Add(new ListViewItem(ws.DisplayLabel) {Tag = ws});
					foreach (IWritingSystem ws in m_cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems)
						m_lvItems.Items.Add(new ListViewItem(ws.DisplayLabel) {Tag = ws});
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
				string[] rgws = m_current.WsLabel.Split(new[] { ',' });
				int indexTarget = 0;
				for (int i = 0; i < m_lvItems.Items.Count; ++i)
				{
					if (!(m_lvItems.Items[i].Tag is int))
					{
						indexTarget = i;
						break;
					}
				}
				for (int i = 0; i < rgws.Length; ++i)
				{
					string sLabel = rgws[i];
					bool fChecked = false;
					for (int iws = 0; iws < m_lvItems.Items.Count; ++iws)
					{
						var ws = m_lvItems.Items[iws].Tag as IWritingSystem;
						if (ws != null && ws.Id == sLabel)
						{
							m_lvItems.Items[iws].Checked = true;
							MoveListItem(iws, indexTarget++);
							fChecked = true;
							break;
						}
					}
					if (!fChecked)
					{
						// Add this to the list of writing systems, since the user must have
						// wanted it at some time.
						IWritingSystem ws;
						if (m_cache.ServiceLocator.WritingSystemManager.TryGet(sLabel, out ws))
							m_lvItems.Items.Insert(indexTarget++, new ListViewItem(ws.DisplayLabel) { Tag = ws, Checked = true });
					}
				}
			}
			// if for some reason nothing was selected, try to select a default.
			if (m_lvItems.CheckedItems.Count == 0)
				SelectDefaultWss(wsDefault, wsDefault2);
			if (m_lvItems.CheckedItems.Count == 0)
				SelectAllDefaultWritingSystems();
		}

		private void SelectDefaultWss(int wsDefault, int wsDefault2)
		{
			if (wsDefault != 0)
				SetDefaultWritingSystem(wsDefault);
			if (wsDefault2 != 0)
				SetDefaultWritingSystem(wsDefault2);
		}

		private void SetDefaultWritingSystem(int wsWanted)
		{
			for (int i = 0; i < m_lvItems.Items.Count; ++i)
			{
				if (m_lvItems.Items[i].Tag is int)
				{
					int ws = (int)m_lvItems.Items[i].Tag;
					if (ws == wsWanted)
					{
						m_lvItems.Items[i].Checked = true;
						break;
					}
				}
			}
		}

		private void SelectAllDefaultWritingSystems()
		{
			for (int i = 0; i < m_lvItems.Items.Count; ++i)
			{
				if (m_lvItems.Items[i].Tag is int)
					m_lvItems.Items[i].Checked = true;
			}
		}

		private void DisplayTypeControls(bool fEnabled)
		{
			if (String.IsNullOrEmpty(m_current.LexRelType) && String.IsNullOrEmpty(m_current.EntryType))
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
			if (!String.IsNullOrEmpty(m_current.LexRelType))
				InitializeRelationList();
			else if (m_current.EntryType == "complex")
				InitializeComplexFormTypeList();
			else if (m_current.EntryType == "variant")
				InitializeVariantTypeList();
			else
				InitializeMinorEntryTypeList();
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
			if (!String.IsNullOrEmpty(m_current.WsLabel) || !String.IsNullOrEmpty(m_current.WsType))
				y = m_chkDisplayData.Location.Y + m_chkDisplayData.Size.Height + 7;
			else if (!String.IsNullOrEmpty(m_current.LexRelType))
				y = m_cfgParentNode.Location.Y + m_cfgParentNode.Size.Height + 7;
			else if (m_current.EntryType == "complex" && !m_current.AllowBeforeStyle)
				y = m_chkComplexFormsAsParagraphs.Location.Y + m_chkComplexFormsAsParagraphs.Size.Height + 7;
			else if (!String.IsNullOrEmpty(m_current.EntryType))
				y = m_cfgParentNode.Location.Y + m_cfgParentNode.Size.Height + 7;
			else
				return;

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
			m_lblItemsList.Text = xWorksStrings.ksLexicalRelationTypes;
			m_lvItems.Items.Clear();
			foreach (var lvi in m_current.RelTypeList.Select(x => new ListViewItem(x.Name) {Tag = x, Checked = x.Enabled}))
				m_lvItems.Items.Add(lvi);
		}

		private void InitializeComplexFormTypeList()
		{
			m_listType = ListViewContent.ComplexFormTypes;
			m_lblItemsList.Text = xWorksStrings.ksComplexFormTypes;
			m_lvItems.Items.Clear();
			foreach (var lvi in m_current.EntryTypeList.Select(x => new ListViewItem(x.Name) { Tag = x, Checked = x.Enabled }))
				m_lvItems.Items.Add(lvi);
		}

		private void InitializeVariantTypeList()
		{
			m_listType = ListViewContent.VariantTypes;
			m_lblItemsList.Text = xWorksStrings.ksVariantTypes;
			m_lvItems.Items.Clear();
			foreach (var lvi in m_current.EntryTypeList.Select(x => new ListViewItem(x.Name) { Tag = x, Checked = x.Enabled }))
				m_lvItems.Items.Add(lvi);
		}

		private void InitializeMinorEntryTypeList()
		{
			m_listType = ListViewContent.MinorEntryTypes;
			m_lblItemsList.Text = xWorksStrings.ksMinorEntryTypes;
			m_lvItems.Items.Clear();
			foreach (var lvi in m_current.EntryTypeList.Select(x => new ListViewItem(x.Name) { Tag = x, Checked = x.Enabled }))
				m_lvItems.Items.Add(lvi);
		}

		private static int ComparePossibilitiesByName(ICmPossibility x, ICmPossibility y)
		{
			if (x == null)
				return y == null ? 0 : -1;
			if (y == null)
				return 1;
			var xName = x.Name.BestAnalysisVernacularAlternative.Text;
			var yName = y.Name.BestAnalysisVernacularAlternative.Text;
			if (xName == null)
				return yName == null ? 0 : -1;
			if (yName == null)
				return 1;
			return xName.CompareTo(yName);
		}

		private void DisplayStyleControls(bool fEnabled)
		{
			if (m_current.AllowCharStyle)
			{
				m_lblCharStyle.Text = xWorksStrings.ksCharacterStyleForContent;
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
				m_lblCharStyle.Text = xWorksStrings.ksParagraphStyleForContent;
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
			for (int i = 0; i < m_rgCharStyles.Count; ++i)
			{
				if (m_rgCharStyles[i].Style != null &&
					m_rgCharStyles[i].Style.Name == m_current.StyleName)
				{
					m_cbCharStyle.SelectedIndex = i;
					break;
				}
			}
		}

		private void SetParagraphStyles()
		{
			m_cbCharStyle.Items.Clear();
			var paraStyleComboItems = GetParaStyleComboItems;
			m_cbCharStyle.Items.AddRange(paraStyleComboItems.ToArray());
			for (int i = 0; i < m_rgParaStyles.Count; ++i)
			{
				if (paraStyleComboItems[i].Style != null &&
					paraStyleComboItems[i].Style.Name == m_current.StyleName)
				{
					m_cbCharStyle.SelectedIndex = i;
					break;
				}
			}
		}

		private List<StyleComboItem> GetParaStyleComboItems
		{
			get
			{
				if (m_current != null && m_current.PreventNullStyle)
					return m_rgParaStyles.Where(item => item.Style != null).ToList();
				else
					return m_rgParaStyles;
			}
		}

		private void DisplayBeforeStyleControls(bool fEnabled)
		{
			if (m_current.AllowBeforeStyle)
			{
				m_cbBeforeStyle.Items.Clear();
				bool fParaStyles = m_current.FlowType == "div";
				if (fParaStyles)
				{
					m_lblBeforeStyle.Text = xWorksStrings.ksParagraphStyleForBefore;
					m_cbBeforeStyle.Items.AddRange(m_rgParaStyles.ToArray());
					for (int i = 0; i < m_rgParaStyles.Count; ++i)
					{
						if (m_rgParaStyles[i].Style != null &&
							m_rgParaStyles[i].Style.Name == m_current.BeforeStyleName)
						{
							m_cbBeforeStyle.SelectedIndex = i;
							break;
						}
					}
				}
				else
				{
					m_lblBeforeStyle.Text = xWorksStrings.ksCharacterStyleForBefore;
					m_cbBeforeStyle.Items.AddRange(m_rgCharStyles.ToArray());
					for (int i = 0; i < m_rgCharStyles.Count; ++i)
					{
						if (m_rgCharStyles[i].Style != null &&
							m_rgCharStyles[i].Style.Name == m_current.BeforeStyleName)
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
				return;
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
				m_lblBefore.Text = xWorksStrings.ksBefore;
				m_tbBefore.Text = m_current.Before;
				m_lblBefore.Visible = true;
				m_tbBefore.Visible = true;
				m_lblBefore.Enabled = m_chkDisplayData.Checked && fEnabled;
				m_tbBefore.Enabled = m_chkDisplayData.Checked && fEnabled;
			}
			else
			{
				m_tbBefore.Text = "";
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
				m_tbAfter.Text = "";
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
			else //if (m_tbBefore.Visible && m_tbAfter.Visible)
			{
				m_tbBetween.Text = "";
				m_lblBetween.Visible = true;
				m_tbBetween.Visible = true;
				m_lblBetween.Enabled = false;
				m_tbBetween.Enabled = false;
			}
			//else
			//{
			//    m_lblBetween.Visible = false;
			//    m_tbBetween.Visible = false;
			//}
		}

		/// <summary>
		/// Return true if any changes have been made to the layout configuration.
		/// </summary>
		/// <returns></returns>
		private bool IsDirty()
		{
			StoreNodeData(m_current);
			if (m_fDeleteCustomFiles)
				return true;
			string sOldRootLayout = m_mediator.PropertyTable.GetStringProperty(m_sLayoutPropertyName, null);
			string sRootLayout = ((LayoutTypeComboItem)m_cbDictType.SelectedItem).LayoutName;
			if (sOldRootLayout != sRootLayout)
				return true;
			for (int ici = 0; ici < m_cbDictType.Items.Count; ++ici)
			{
				LayoutTypeComboItem ltci = (LayoutTypeComboItem)m_cbDictType.Items[ici];
				for (int itn = 0; itn < ltci.TreeNodes.Count; ++itn)
				{
					LayoutTreeNode ltn = ltci.TreeNodes[itn];
					if (ltn.IsDirty())
						return true;
				}
			}
			return false;
		}

		private void SaveModifiedLayouts()
		{
			List<XmlNode> rgxnLayouts = new List<XmlNode>();
			for (int ici = 0; ici < m_cbDictType.Items.Count; ++ici)
			{
				LayoutTypeComboItem ltci = (LayoutTypeComboItem)m_cbDictType.Items[ici];
				for (int itn = 0; itn < ltci.TreeNodes.Count; ++itn)
				{
					ltci.TreeNodes[itn].MakeSenseNumberFormatConsistent();
					ltci.TreeNodes[itn].GetModifiedLayouts(rgxnLayouts, ltci.TreeNodes);
				}
				// update the inventory with the copied layout type.
				if (ltci.LayoutName.Contains(Inventory.kcMarkLayoutCopy))
					m_layouts.AddLayoutTypeToInventory(ltci.LayoutTypeNode);
			}
			if (m_fDeleteCustomFiles)
			{
				Debug.Assert(m_layouts.DatabaseName == null);
				m_layouts.DeleteUserOverrides(m_cache.ProjectId.Name);
				m_parts.DeleteUserOverrides(m_cache.ProjectId.Name);
				//Make sure to retain any data from user override files that were not deleted i.e. copies of dictionary views
				m_layouts.LoadUserOverrides(LayoutCache.LayoutVersionNumber, m_cache.ProjectId.Name);
				m_parts.LoadUserOverrides(LayoutCache.LayoutVersionNumber, m_cache.ProjectId.Name);

				Inventory.SetInventory("layouts", m_cache.ProjectId.Name, m_layouts);
				Inventory.SetInventory("parts", m_cache.ProjectId.Name, m_parts);
				Inventory.RemoveInventory("layouts", null);
				Inventory.RemoveInventory("parts", null);
			}
			for (int i = 0; i < rgxnLayouts.Count; ++i)
				m_layouts.PersistOverrideElement(rgxnLayouts[i]);
			var layoutsPersisted = new HashSet<XmlNode>(rgxnLayouts);
			foreach (var xnNewBase in m_rgxnNewLayoutNodes)
				if (!layoutsPersisted.Contains(xnNewBase)) // don't need to persist a node that got into both lists twice
					m_layouts.PersistOverrideElement(xnNewBase);
			m_rgxnNewLayoutNodes.Clear();
		}


		#endregion // Misc internal functions

		#region IFWDisposable Members

		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		#endregion // IFWDisposable Members

		#region LayoutTreeNode class

		public class LayoutTreeNode : TreeNode
		{
			// These are basic values that we need to know for every node.
			// **Important Note**: In most cases, adding a member variable here means you need to add it to
			// CopyValuesTo() also, so that a duplicate node will end up with the right values.
			XmlNode m_xnConfig;
			string m_sLayoutName;
			string m_sPartName;
			string m_sClassName;
			string m_sLabel;
			string m_sVisibility;
			bool m_fContentVisible;
			bool m_fUseParentConfig;
			bool m_fShowSenseConfig;
			bool m_fShowGramInfoConfig;
			bool m_fShowComplexFormParaConfig;
			string m_sParam;
			string m_sFlowType;

			// These values depend on the particular node, and affect what is displayed in the
			// details pane.  If a string value is null, then the corresponding control (and
			// label) is not shown.
			string m_sBefore;
			string m_sAfter;
			string m_sSep;
			string m_sWsLabel;
			string m_sWsType;
			string m_sStyleName;
			string m_sBeforeStyleName;
			bool m_fAllowBeforeStyle;
			bool m_fAllowCharStyle;
			bool m_fAllowParaStyle; // allow style for StTxtPara and other para-level elements
			private bool m_fPreventNullStyle; // If true, (none) is not offered as a style choice.
			private bool m_fAllowDivParaStyle; // allow parastyle to be set for arbitrary div.
			string m_sNumber;
			string m_sNumStyle;
			bool m_fNumSingle;
			bool m_fSingleGramInfoFirst;
			bool m_fShowComplexFormPara;
			string m_sNumFont;
			bool m_fShowWsLabels;
			bool m_fSenseIsPara;
			string m_sSenseParaStyle;

			// These are used to trace creating, deleting, and moving nodes.
			bool m_fDuplicate;
			string m_sDup;
			int m_cSubnodes = -1;
			int m_idxOrig = -1;
			// **NB**: If you're planning to add a member variable here, see the Important Note above.

			XmlNode m_xnCallingLayout;
			XmlNode m_xnParentLayout;

			XmlNode m_xnHiddenNode;
			XmlNode m_xnHiddenParentLayout;

			/// <summary>
			/// This node is a hidden child that provides/receives the style name.
			/// </summary>
			XmlNode m_xnHiddenChild;
			XmlNode m_xnHiddenChildLayout;
			bool m_fStyleFromHiddenChild;
			bool m_fHiddenChildDirty;

			readonly List<LayoutTreeNode> m_rgltnMerged = new List<LayoutTreeNode>();

			public LayoutTreeNode(XmlNode config, StringTable stringTbl, string classParent)
			{
				m_xnConfig = config;
				m_sLabel = XmlUtils.GetLocalizedAttributeValue(stringTbl, config, "label", null);
				if (config.Name == "configure")
				{
					m_sClassName = XmlUtils.GetManditoryAttributeValue(config, "class");
					m_sLayoutName = XmlUtils.GetManditoryAttributeValue(config, "layout");
					m_sPartName = String.Empty;
					m_sVisibility = "required";
				}
				else if (config.Name == "part")
				{
					m_sClassName = classParent;
					string sRef = XmlUtils.GetManditoryAttributeValue(config, "ref");
					if (m_sLabel == null && stringTbl != null)
						m_sLabel = stringTbl.LocalizeAttributeValue(sRef);
					if (config.ParentNode != null && config.ParentNode.Name == "layout")
						m_sLayoutName = XmlUtils.GetManditoryAttributeValue(config.ParentNode, "name");
					else
						m_sLayoutName = String.Empty;
					m_sPartName = String.Format("{0}-Jt-{1}", classParent, sRef);
					m_sVisibility = XmlUtils.GetOptionalAttributeValue(config, "visibility", "always");
					m_fContentVisible = m_sVisibility.ToLowerInvariant() != "never";
					m_sParam = XmlUtils.GetOptionalAttributeValue(config, "param");

					m_sWsLabel = StringServices.GetWsSpecWithoutPrefix(config);
					m_sWsType = XmlUtils.GetOptionalAttributeValue(config, "wsType");
					if (m_sWsLabel != null && String.IsNullOrEmpty(m_sWsType))
					{
						// Try to calculate a WS type from the WS label.
						int ichVern = m_sWsLabel.ToLowerInvariant().IndexOf("vern");
						int ichAnal = m_sWsLabel.ToLowerInvariant().IndexOf("anal");
						int ichPronun = m_sWsLabel.ToLowerInvariant().IndexOf("pronun");
						int ichRevers = m_sWsLabel.ToLowerInvariant().IndexOf("revers");
						if (ichVern >= 0 && ichAnal >= 0 && ichVern > ichAnal)
							m_sWsType = "analysis vernacular";
						else if (ichVern >= 0 && ichAnal >= 0 && ichAnal > ichVern)
							m_sWsType = "vernacular analysis";
						else if (ichVern >= 0)
							m_sWsType = "vernacular";
						else if (ichAnal >= 0)
							m_sWsType = "analysis";
						else if (ichPronun >= 0)
							m_sWsType = "pronunciation";
						else if (ichRevers >= 0)
							m_sWsType = "reversal";
						else
						{
							m_sWsType = "vernacular analysis";	// who knows???
							string refValue = String.Empty;
							string wsValue = String.Empty;
							if (config.Attributes != null)
							{
								refValue = config.Attributes["ref"].Value;
								wsValue = config.Attributes["ws"].Value;
							}
							Debug.Fail(String.Format("This layout node ({0}) does not specify @wsType "
								+ "and we couldn't compute something reasonable from @ws='{1}' "
								+ "so we're setting @wsType to 'vernacular analysis'",
								refValue, wsValue));
						}
							// store the wsType attribute on the node, so that if 'ws' changes to something
							// specific, we still know what type of wss to provide options for in the m_lvWritingSystems.
							if (config.OwnerDocument != null)
							{
							XmlAttribute xa = config.OwnerDocument.CreateAttribute("wsType");
							xa.Value = m_sWsType;
								if (config.Attributes != null)
							config.Attributes.Append(xa);
						}
					}
					m_sWsType = StringServices.GetWsSpecWithoutPrefix(m_sWsType);
					if (m_sWsType != null && m_sWsLabel == null)
							m_sWsLabel = "";
					string sSep = null;
					// By default, if we have a ws type or ws label we should be able to show multiple wss,
					// and thus need a separator between them.
					if (!String.IsNullOrEmpty(m_sWsLabel) || !String.IsNullOrEmpty(m_sWsType))
						sSep = " ";
					m_sBeforeStyleName = XmlUtils.GetOptionalAttributeValue(config, "beforeStyle");
					m_fAllowBeforeStyle = !String.IsNullOrEmpty(m_sBeforeStyleName);
					m_sBefore = XmlUtils.GetOptionalAttributeValue(config, "before", "");
					m_sSep = XmlUtils.GetOptionalAttributeValue(config, "sep", sSep);
					m_sAfter = XmlUtils.GetOptionalAttributeValue(config, "after", " ");

					m_sStyleName = XmlUtils.GetOptionalAttributeValue(config, "style");
					m_sFlowType = XmlUtils.GetOptionalAttributeValue(config, "flowType", "span");
					if (m_sFlowType == "span")
					{
						m_fAllowCharStyle = !XmlUtils.GetOptionalBooleanAttributeValue(
							config, "disallowCharStyle", false);
						if (m_sBefore == null)
							m_sBefore = "";
						if (m_sAfter == null)
							m_sAfter = "";
					}
					// Special handling for div flow elements, which can contain a sequence of paragraphs.
					else if (m_sFlowType == "div")
					{
						m_fAllowParaStyle = m_sClassName == "StText";
						m_fAllowDivParaStyle = false;
						if (m_fAllowParaStyle)
						{
							// We'll be getting the style name from a child layout.
							Debug.Assert(String.IsNullOrEmpty(m_sStyleName));
						}
						else
						{
							m_sStyleName = XmlUtils.GetOptionalAttributeValue(config, "parastyle");
							m_fAllowDivParaStyle = !String.IsNullOrEmpty(m_sStyleName);
						}
						m_sSep = null;
						m_sAfter = null;
						// This is subtly differerent: in a div, we will hide or disable the before control (value null) unless the XML
						// explicitly calls for it by including the attribute. Above we provided an empty string default,
						// which enables the empty control.
						m_sBefore = XmlUtils.GetOptionalAttributeValue(config, "before");
					}
					else if (m_sFlowType == "para")
					{
						m_fAllowParaStyle = !String.IsNullOrEmpty(m_sStyleName);
					}
					else if (m_sFlowType == "divInPara")
					{
						m_sBefore = m_sAfter = m_sSep = null; // suppress the whole separators group since each item is a para
						m_sStyleName = XmlUtils.GetOptionalAttributeValue(config, "parastyle");
						m_fAllowDivParaStyle = !String.IsNullOrEmpty(m_sStyleName);
					}
					m_sSenseParaStyle = XmlUtils.GetOptionalAttributeValue(config, "parastyle");
					m_sNumber = XmlUtils.GetOptionalAttributeValue(config, "number");
					m_sNumStyle = XmlUtils.GetOptionalAttributeValue(config, "numstyle");
					m_fNumSingle = XmlUtils.GetOptionalBooleanAttributeValue(config, "numsingle", false);
					m_sNumFont = XmlUtils.GetOptionalAttributeValue(config, "numfont");
					m_fSingleGramInfoFirst = XmlUtils.GetOptionalBooleanAttributeValue(config, "singlegraminfofirst", false);
					if (m_sFlowType == "divInPara" && m_sParam != null && m_sParam.EndsWith("_AsPara"))
						m_fSenseIsPara = true;
					else
						m_fSenseIsPara = false;

					m_fPreventNullStyle = XmlUtils.GetOptionalBooleanAttributeValue(config, "preventnullstyle", false);
					m_fShowComplexFormPara = XmlUtils.GetOptionalBooleanAttributeValue(config, "showasindentedpara", false);
					if (m_fShowComplexFormPara)
						m_fAllowParaStyle = true;
					m_fShowWsLabels = XmlUtils.GetOptionalBooleanAttributeValue(config, "showLabels", false);
					m_sDup = XmlUtils.GetOptionalAttributeValue(config, "dup");
					m_fDuplicate = !String.IsNullOrEmpty(m_sDup);

					LexRelType = XmlUtils.GetOptionalAttributeValue(config, "lexreltype");
					if (!String.IsNullOrEmpty(LexRelType))
					{
						var sRelTypes = XmlUtils.GetOptionalAttributeValue(config, "reltypeseq");
						RelTypeList = LexReferenceInfo.CreateListFromStorageString(sRelTypes);
					}
					EntryType = XmlUtils.GetOptionalAttributeValue(config, "entrytype");
					if (!String.IsNullOrEmpty(EntryType))
					{
						var sTypeseq = XmlUtils.GetOptionalAttributeValue(config, "entrytypeseq");
						EntryTypeList = ItemTypeInfo.CreateListFromStorageString(sTypeseq);
					}
				}
				Checked = m_sVisibility.ToLowerInvariant() != "never";
				Text = m_sLabel;
				Name = String.Format("{0}/{1}/{2}", m_sClassName, m_sLayoutName, m_sPartName);
			}

			public LayoutTreeNode()
			{
			}

			/// <summary>
			/// Copies the tree node and the entire subtree rooted at this tree node.
			/// This is a partial copy that copies references to objects rather than
			/// cloning the objects themselves (other than the tree nodes of the subtree).
			/// This is good for moving a tree node up and down, but not so good for
			/// duplicating the tree node.  In the latter case, at least the configuration
			/// XML nodes must be cloned, and the layouts those point to, and new names
			/// generated for the duplicates.  Otherwise, editing the subnodes of the
			/// duplicated node ends up editing the configuration of the subnodes of the
			/// original node.
			/// </summary>
			public override object Clone()
			{
				var ltn = (LayoutTreeNode)base.Clone();
				CopyValuesTo(ltn);
				return ltn;
			}

			private void CopyValuesTo(LayoutTreeNode ltn)
			{
				// Review: might be difficult to keep this up-to-date! How do we know we've got
				// everything in here that needs to be here?! --gjm
				ltn.m_cSubnodes = m_cSubnodes;
				ltn.m_fAllowBeforeStyle = m_fAllowBeforeStyle;
				ltn.m_fAllowCharStyle = m_fAllowCharStyle;
				ltn.m_fAllowDivParaStyle = m_fAllowDivParaStyle;
				ltn.m_fAllowParaStyle = m_fAllowParaStyle;
				ltn.m_fContentVisible = m_fContentVisible;
				ltn.m_fDuplicate = m_fDuplicate;
				//ltn.m_fHiddenChildDirty = m_fHiddenChildDirty;
				ltn.m_fNumSingle = m_fNumSingle;
				//ltn.m_fPreventNullStyle = m_fPreventNullStyle;
				ltn.m_fSenseIsPara = m_fSenseIsPara;
				ltn.m_fShowComplexFormPara = m_fShowComplexFormPara;
				ltn.m_fShowComplexFormParaConfig = m_fShowComplexFormParaConfig;
				ltn.m_fShowGramInfoConfig = m_fShowGramInfoConfig;
				ltn.m_fShowSenseConfig = m_fShowSenseConfig;
				ltn.m_fShowWsLabels = m_fShowWsLabels;
				ltn.m_fSingleGramInfoFirst = m_fSingleGramInfoFirst;
				ltn.m_fStyleFromHiddenChild = m_fStyleFromHiddenChild;
				ltn.m_fUseParentConfig = m_fUseParentConfig;
				ltn.m_idxOrig = m_idxOrig;
				//ltn.m_rgltnMerged = m_rgltnMerged;
				ltn.m_sAfter = m_sAfter;
				ltn.m_sBefore = m_sBefore;
				ltn.m_sBeforeStyleName = m_sBeforeStyleName;
				ltn.m_sClassName = m_sClassName;
				ltn.m_sDup = m_sDup;
				ltn.m_sFlowType = m_sFlowType;
				ltn.m_sLabel = m_sLabel;
				ltn.m_sLayoutName = m_sLayoutName;
				ltn.m_sNumber = m_sNumber;
				ltn.m_sNumFont = m_sNumFont;
				ltn.m_sNumStyle = m_sNumStyle;
				ltn.m_sParam = m_sParam;
				ltn.m_sPartName = m_sPartName;
				ltn.m_sSenseParaStyle = m_sSenseParaStyle;
				ltn.m_sSep = m_sSep;
				ltn.m_sStyleName = m_sStyleName;
				ltn.m_sVisibility = m_sVisibility;
				ltn.m_sWsLabel = m_sWsLabel;
				ltn.m_sWsType = m_sWsType;
				ltn.m_xnCallingLayout = m_xnCallingLayout;
				ltn.m_xnConfig = m_xnConfig;
				ltn.m_xnHiddenChild = m_xnHiddenChild;
				ltn.m_xnHiddenChildLayout = m_xnHiddenChildLayout;
				ltn.m_xnHiddenNode = m_xnHiddenNode;
				ltn.m_xnHiddenParentLayout = m_xnHiddenParentLayout;
				ltn.m_xnParentLayout = m_xnParentLayout;
				ltn.LexRelType = LexRelType;
				ltn.RelTypeList = RelTypeList;
				ltn.EntryType = EntryType;
				ltn.EntryTypeList = EntryTypeList;
			}

			internal LayoutTreeNode CreateCopy()
			{
				var ltn = new LayoutTreeNode();
				CopyValuesTo(ltn);
				ltn.m_xnConfig = m_xnConfig.Clone();
				ltn.IsDuplicate = true;
				ltn.m_cSubnodes = 0;
				if (ltn.RelTypeList != null && ltn.RelTypeList.Count > 0)
				{
					ltn.RelTypeList = LexReferenceInfo.CreateListFromStorageString(ltn.LexRelTypeSequence);
				}
				if (ltn.EntryTypeList != null && ltn.EntryTypeList.Count > 0)
				{
					ltn.EntryTypeList = ItemTypeInfo.CreateListFromStorageString(ltn.EntryTypeSequence);
				}
				return ltn;
			}

			public bool AllParentsChecked
			{
				get
				{
					TreeNode tn = Parent;
					while (tn != null)
					{
						if (!tn.Checked)
							return false;
						tn = tn.Parent;
					}
					return true;
				}
			}

			public bool IsDescendedFrom(TreeNode tnPossibleAncestor)
			{
				TreeNode tn = Parent;
				while (tn != null)
				{
					if (tn == tnPossibleAncestor)
						return true;
					tn = tn.Parent;
				}
				return false;
			}

			public bool IsTopLevel
			{
				get { return m_xnConfig.Name == "configure" && Level == 0; }
			}

			public XmlNode Configuration
			{
				get { return m_xnConfig; }
			}

			public string LayoutName
			{
				get { return m_sLayoutName; }
			}

			public string PartName
			{
				get { return m_sPartName; }
			}

			public string ClassName
			{
				get { return m_sClassName; }
				internal set { m_sClassName = value; }
			}

			public string FlowType
			{
				get { return m_sFlowType; }
			}

			public string Label
			{
				get { return m_sLabel; }
				set
				{
					m_sLabel = value;
					Text = m_sLabel;
				}
			}

			public bool UseParentConfig
			{
				get { return m_fUseParentConfig; }
				set { m_fUseParentConfig = value; }
			}

			public bool ShowSenseConfig
			{
				get { return m_fShowSenseConfig; }
				set { m_fShowSenseConfig = value; }
			}

			public string LexRelType { get; private set; }
			public List<LexReferenceInfo> RelTypeList { get; private set; }

			public string LexRelTypeSequence
			{
				get
				{
					if (RelTypeList == null)
						return null;
					var bldr = new StringBuilder();
					for (var i = 0; i < RelTypeList.Count; ++i)
					{
						if (i > 0)
							bldr.Append(",");
						bldr.Append(RelTypeList[i].StorageString);
					}
					return bldr.ToString();
				}
			}

			public string EntryType { get; private set; }
			public List<ItemTypeInfo> EntryTypeList { get; private set; }

			public string EntryTypeSequence
			{
				get
				{
					if (EntryTypeList == null)
						return null;
					var bldr = new StringBuilder();
					for (var i = 0; i < EntryTypeList.Count; ++i)
					{
						if (i > 0)
							bldr.Append(",");
						bldr.Append(EntryTypeList[i].StorageString);
					}
					return bldr.ToString();
				}
			}

			public bool ShowGramInfoConfig
			{
				get { return m_fShowGramInfoConfig; }
				set { m_fShowGramInfoConfig = value; }
			}

			public bool ShowComplexFormParaConfig
			{
				get { return m_fShowComplexFormParaConfig; }
				set { m_fShowComplexFormParaConfig = value; }
			}

			public bool ShowSenseAsPara
			{
				get { return m_fSenseIsPara; }
				set { m_fSenseIsPara = value; }
			}

			public string SenseParaStyle
			{
				get { return m_sSenseParaStyle; }
				set { m_sSenseParaStyle = value; }
			}

			public bool ContentVisible
			{
				get { return m_fContentVisible; }
				set { m_fContentVisible = value; }
			}

			public string BeforeStyleName
			{
				get { return m_sBeforeStyleName; }
				set { m_sBeforeStyleName = value; }
			}

			public string Before
			{
				get { return m_sBefore; }
				set { m_sBefore = value; }
			}

			public string After
			{
				get { return m_sAfter; }
				set { m_sAfter = value; }
			}

			public string Between
			{
				get { return m_sSep; }
				set { m_sSep = value; }
			}

			public string WsLabel
			{
				get { return m_sWsLabel; }
				set
				{
					var temp = value;
					if (!NewValueIsCompatibleWithMagic(m_sWsLabel, temp))
					{
						m_sWsLabel = temp;
					}
				}
			}

			private static bool NewValueIsCompatibleWithMagic(string possibleMagicLabel, string newValue)
			{
				if (String.IsNullOrEmpty(possibleMagicLabel) || String.IsNullOrEmpty(newValue))
					return false;

				var magicId = WritingSystemServices.GetMagicWsIdFromName(possibleMagicLabel);
				if (magicId == 0)
					return false;

				var newId = WritingSystemServices.GetMagicWsIdFromName(newValue);
				if (newId == 0)
					return false;

				return newId == WritingSystemServices.SmartMagicWsToSimpleMagicWs(magicId);
			}

			public string WsType
			{
				get { return m_sWsType; }
				set { m_sWsType = value; }
			}

			public string StyleName
			{
				get { return m_sStyleName; }
				set { m_sStyleName = value; }
			}

			/// <summary>
			/// True if (none) should be suppressed from the style list.
			/// Currently this is obsolete. There is code to implement it for main paragraph styles;
			/// but we do not create 'none' as an option for m_rgParaStyles at all (see commented-out code linked to LT-10950).
			/// It has never been implemented for character or 'before' styles.
			/// </summary>
			public bool PreventNullStyle
			{
				get { return m_fPreventNullStyle; }
			}

			public bool AllowCharStyle
			{
				get { return m_fAllowCharStyle; }
			}

			public bool AllowBeforeStyle
			{
				get { return m_fAllowBeforeStyle; }
			}

			public bool AllowParaStyle
			{
				get { return m_fAllowParaStyle; }
			}

			public bool AllowDivParaStyle
			{
				get { return m_fAllowDivParaStyle; }
			}

			public string Number
			{
				get { return m_sNumber; }
				set { m_sNumber = value; }
			}

			public string NumStyle
			{
				get { return m_sNumStyle; }
				set { m_sNumStyle = value; }
			}

			public bool NumberSingleSense
			{
				get { return m_fNumSingle; }
				set { m_fNumSingle = value; }
			}

			public string NumFont
			{
				get { return m_sNumFont; }
				set { m_sNumFont = value; }
			}

			public bool ShowSingleGramInfoFirst
			{
				get { return m_fSingleGramInfoFirst; }
				set { m_fSingleGramInfoFirst = value; }
			}

			public bool ShowComplexFormPara
			{
				get { return m_fShowComplexFormPara; }
				set { m_fShowComplexFormPara = value; m_fAllowParaStyle = value; }
			}

			public bool ShowWsLabels
			{
				get { return m_fShowWsLabels; }
				set { m_fShowWsLabels = value; }
			}

			public string Param
			{
				get { return m_sParam; }
				set { m_sParam = value; }
			}

			public bool IsDuplicate
			{
				get { return m_fDuplicate; }
				set { m_fDuplicate = value; }
			}

			public string DupString
			{
				get { return m_sDup; }
				set
				{
					m_sDup = value;
					if (Nodes.Count > 0)
					{
						string sDupChild = String.Format("{0}.0", value);
						for (int i = 0; i < Nodes.Count; ++i)
							((LayoutTreeNode)Nodes[i]).DupString = sDupChild;
					}
				}
			}

			internal int OriginalIndex
			{
				set
				{
					if (m_idxOrig == -1)
						m_idxOrig = value;
				}
			}

			/// <summary>
			/// Flag that we're still adding subnodes to this node.
			/// </summary>
			internal bool AddingSubnodes { get; set; }

			internal int OriginalNumberOfSubnodes
			{
				get
				{
					return m_cSubnodes == -1 ? 0 : m_cSubnodes;
				}
				set
				{
					Debug.Assert(value >= m_cSubnodes);
					if (m_cSubnodes == -1 || IsTopLevel || AddingSubnodes)
						m_cSubnodes = value;
					else
					{
						Debug.WriteLine("OriginalNumberOfSubnodes did not update!");
					}
				}
			}

			internal bool IsDirty()
			{
				if (IsNodeDirty() || HasMoved)
					return true;
				for (int i = 0; i < Nodes.Count; ++i)
				{
					LayoutTreeNode ltn = (LayoutTreeNode)Nodes[i];
					if (ltn.IsDirty())
						return true;
				}
				return false;
			}

			internal bool HasMoved
			{
				get { return Index != m_idxOrig; }
			}

			internal bool IsNodeDirty()
			{
				if (Nodes.Count != OriginalNumberOfSubnodes)
					return true;
				if (!IsTopLevel)
				{
					// Now, compare our member variables to the content of m_xnConfig.
					if (m_xnConfig.Name == "part")
					{
						bool fContentVisible = m_sVisibility != "never";
						m_fContentVisible = Checked; // in case (un)checked in treeview, but node never selected.
						if (fContentVisible != m_fContentVisible)
							return true;
						string sBeforeStyleName = XmlUtils.GetOptionalAttributeValue(m_xnConfig, "beforeStyle");
						if (StringsDiffer(sBeforeStyleName, m_sBeforeStyleName))
							return true;
						string sBefore = XmlUtils.GetOptionalAttributeValue(m_xnConfig, "before");
						if (StringsDiffer(sBefore, m_sBefore))
							return true;
						string sAfter = XmlUtils.GetOptionalAttributeValue(m_xnConfig, "after");
						if (StringsDiffer(sAfter, m_sAfter))
						{
							if (sAfter == null)
							{
								if (StringsDiffer(" ", m_sAfter))
									return true;
							}
							else
							{
								return true;
							}
						}
						string sSep = XmlUtils.GetOptionalAttributeValue(m_xnConfig, "sep");
						if (StringsDiffer(sSep, m_sSep))
						{
							if (sSep == null)
							{
								string sSepDefault = null;
								if (!String.IsNullOrEmpty(m_sWsLabel) || !String.IsNullOrEmpty(m_sWsType))
									sSepDefault = " ";
								if (StringsDiffer(sSepDefault, m_sSep))
									return true;
							}
							else
							{
								return true;
							}
						}
						string sWsLabel = StringServices.GetWsSpecWithoutPrefix(m_xnConfig);
						if (StringsDiffer(sWsLabel, m_sWsLabel))
							return true;
						if (m_fStyleFromHiddenChild)
						{
							string sStyleName = XmlUtils.GetOptionalAttributeValue(m_xnHiddenChild, "style");
							m_fHiddenChildDirty = StringsDiffer(sStyleName, m_sStyleName);
							if (m_fHiddenChildDirty)
								return true;
						}
						else
						{
							string sStyleName;
							if (AllowDivParaStyle)
								sStyleName = XmlUtils.GetOptionalAttributeValue(m_xnConfig, "parastyle");
							else
								sStyleName = XmlUtils.GetOptionalAttributeValue(m_xnConfig, "style");
							if (StringsDiffer(sStyleName, m_sStyleName))
								return true;
						}
						string sNumber = XmlUtils.GetOptionalAttributeValue(m_xnConfig, "number");
						if (StringsDiffer(sNumber, m_sNumber))
							return true;
						string sNumStyle = XmlUtils.GetOptionalAttributeValue(m_xnConfig, "numstyle");
						if (StringsDiffer(sNumStyle, m_sNumStyle))
							return true;
						bool fNumSingle = XmlUtils.GetOptionalBooleanAttributeValue(m_xnConfig, "numsingle", false);
						if (fNumSingle != m_fNumSingle)
							return true;
						string sNumFont = XmlUtils.GetOptionalAttributeValue(m_xnConfig, "numfont");
						if (StringsDiffer(sNumFont, m_sNumFont))
							return true;
						bool fSingleGramInfoFirst = XmlUtils.GetOptionalBooleanAttributeValue(m_xnConfig, "singlegraminfofirst", false);
						if (fSingleGramInfoFirst != m_fSingleGramInfoFirst)
							return true;
						bool fShowComplexFormPara = XmlUtils.GetOptionalBooleanAttributeValue(m_xnConfig, "showasindentedpara", false);
						if (fShowComplexFormPara != m_fShowComplexFormPara)
							return true;
						bool fShowWsLabels = XmlUtils.GetOptionalBooleanAttributeValue(m_xnConfig, "showLabels", false);
						if (fShowWsLabels != m_fShowWsLabels)
							return true;
						string sDuplicate = XmlUtils.GetOptionalAttributeValue(m_xnConfig, "dup");
						if (StringsDiffer(sDuplicate, m_sDup))
							return true;
						if (ShowSenseConfig)
						{
							var fSenseIsPara = m_sParam != null && m_sParam.EndsWith("_AsPara");
							if (fSenseIsPara != m_fSenseIsPara)
								return true;
							var sSenseParaStyle = XmlUtils.GetOptionalAttributeValue(m_xnConfig, "parastyle");
							if (sSenseParaStyle != m_sSenseParaStyle)
								return true;
						}
						if (!String.IsNullOrEmpty(LexRelType))
						{
							var sRefTypeSequence = XmlUtils.GetOptionalAttributeValue(m_xnConfig, "reltypeseq");
							if (sRefTypeSequence != LexRelTypeSequence)
								return true;
						}
						if (!String.IsNullOrEmpty(EntryType))
						{
							var sEntryTypeSeq = XmlUtils.GetOptionalAttributeValue(m_xnConfig, "entrytypeseq");
							if (sEntryTypeSeq != EntryTypeSequence)
								return true;
						}
					}
				}
				else
				{
					return IsTopLevel && OverallLayoutVisibilityChanged();
				}
				return false;
			}

			private bool OverallLayoutVisibilityChanged()
			{
				Debug.Assert(Level == 0);
				string sVisible = XmlUtils.GetAttributeValue(m_xnParentLayout, "visibility");
				bool fOldVisible = sVisible != "never";
				return Checked != fOldVisible;
			}

			private static bool StringsDiffer(string s1, string s2)
			{
				return (s1 != s2 && !(String.IsNullOrEmpty(s1) && String.IsNullOrEmpty(s2)));
			}

			internal bool IsRequired
			{
				// LT-10472 says that nothing is really required.
				//get { return m_sVisibility != null && m_sVisibility.ToLowerInvariant() == "required"; }
				get { return false; }
			}

			internal XmlNode ParentLayout
			{
				get { return m_xnParentLayout; }
				set { m_xnParentLayout = value; }
			}

			internal XmlNode HiddenNode
			{
				get { return m_xnHiddenNode; }
				set { m_xnHiddenNode = value; }
			}

			internal XmlNode HiddenNodeLayout
			{
				get { return m_xnHiddenParentLayout; }
				set { m_xnHiddenParentLayout = value; }
			}
			internal XmlNode HiddenChildLayout
			{
				get { return m_xnHiddenChildLayout; }
				set { m_xnHiddenChildLayout = value; }
			}

			internal XmlNode HiddenChild
			{
				get { return m_xnHiddenChild; }
				set
				{
					m_xnHiddenChild = value;
					if (m_sClassName == "StText" && m_fAllowParaStyle && String.IsNullOrEmpty(m_sStyleName))
					{
						m_fStyleFromHiddenChild = true;
						m_sStyleName = XmlUtils.GetOptionalAttributeValue(value, "style");
					}
				}
			}

			internal bool HiddenChildDirty
			{
				get { return m_fHiddenChildDirty; }
			}

			internal bool GetModifiedLayouts(List<XmlNode> rgxn, List<LayoutTreeNode> topNodes)
			{
				List<XmlNode> rgxnDirtyLayouts = new List<XmlNode>();
				for (int i = 0; i < Nodes.Count; ++i)
				{
					LayoutTreeNode ltn = (LayoutTreeNode)Nodes[i];
					if (ltn.GetModifiedLayouts(rgxn, topNodes))
					{
						XmlNode xn = ltn.ParentLayout;
						if (xn != null && !rgxnDirtyLayouts.Contains(xn))
							rgxnDirtyLayouts.Add(xn);
						xn = ltn.HiddenChildLayout;
						if (xn != null && ltn.HiddenChildDirty && !rgxnDirtyLayouts.Contains(xn))
							rgxnDirtyLayouts.Add(xn);
						xn = ltn.m_xnHiddenParentLayout;
						if (xn != null && ltn.HasMoved && !rgxnDirtyLayouts.Contains(xn))
							rgxnDirtyLayouts.Add(xn);
						foreach (LayoutTreeNode ltnMerged in ltn.MergedNodes)
						{
							xn = ltnMerged.ParentLayout;
							if (xn != null && !rgxnDirtyLayouts.Contains(xn))
								rgxnDirtyLayouts.Add(xn);
						}
					}
				}
				var fDirty = IsDirty();
				if (Level == 0 && !rgxnDirtyLayouts.Contains(m_xnParentLayout))
				{
					if (OverallLayoutVisibilityChanged())
					{
						rgxnDirtyLayouts.Add(m_xnParentLayout);
					}
					else if (!IsTopLevel && fDirty)
					{
						rgxnDirtyLayouts.Add(m_xnParentLayout);
					}
				}
				foreach (XmlNode xnDirtyLayout in rgxnDirtyLayouts)
				{
					// Create a new layout node with all its parts in order.  This is needed
					// to handle arbitrary reordering and possible addition or deletion of
					// duplicate nodes.  This is complicated by the presence (or rather absence)
					// of "hidden" nodes, and by "merged" nodes.
					XmlNode xnLayout = xnDirtyLayout.Clone();
					if (xnDirtyLayout == m_xnParentLayout && IsTopLevel && OverallLayoutVisibilityChanged())
						UpdateAttribute(xnLayout, "visibility", Checked ? "always" : "never");
					if (xnLayout.Attributes != null)
					{
						XmlAttribute[] rgxa = new XmlAttribute[xnLayout.Attributes.Count];
						xnLayout.Attributes.CopyTo(rgxa, 0);
						List<XmlNode> rgxnGen = new List<XmlNode>();
						List<int> rgixn = new List<int>();
						for (int i = 0; i < xnLayout.ChildNodes.Count; ++i)
						{
							XmlNode xn = xnLayout.ChildNodes[i];
							if (xn.Name != "part")
							{
								rgxnGen.Add(xn);
								rgixn.Add(i);
							}
						}
						xnLayout.RemoveAll();
						for (int i = 0; i < rgxa.Length; ++i)
						{
							if (xnLayout.Attributes != null)
								xnLayout.Attributes.SetNamedItem(rgxa[i]);
						}
						if (Level == 0 && !IsTopLevel && xnDirtyLayout == m_xnParentLayout)
						{
							foreach (var ltn in topNodes)
							{
								if (ltn.IsTopLevel || ltn.ParentLayout != xnDirtyLayout)
									continue;
								if (fDirty && ltn == this)
									ltn.StoreUpdatedValuesInConfiguration();
								xnLayout.AppendChild(ltn.Configuration.CloneNode(true));
							}
						}
						else
						{
							for (int i = 0; i < Nodes.Count; ++i)
							{
								LayoutTreeNode ltn = (LayoutTreeNode) Nodes[i];
								if (ltn.ParentLayout == xnDirtyLayout)
								{
									xnLayout.AppendChild(ltn.Configuration.CloneNode(true));
								}
								else if (ltn.HiddenNodeLayout == xnDirtyLayout)
								{
									var xpathString = "/" + ltn.HiddenNode.Name + "[" +
													  BuildXPathFromAttributes(ltn.HiddenNode.Attributes) + "]";
									if (xnLayout.SelectSingleNode(xpathString) == null)
										xnLayout.AppendChild(ltn.HiddenNode.CloneNode(true));
								}
								else if (ltn.HiddenChildLayout == xnDirtyLayout)
								{
									var xpathString = "/" + ltn.HiddenNode.Name + "[" +
													  BuildXPathFromAttributes(ltn.HiddenChild.Attributes) + "]";
									if (xnLayout.SelectSingleNode(xpathString) == null)
										xnLayout.AppendChild(ltn.HiddenChild.CloneNode(true));
								}
								else
								{
									for (int itn = 0; itn < ltn.MergedNodes.Count; itn++)
									{
										LayoutTreeNode ltnMerged = ltn.MergedNodes[itn];
										if (ltnMerged.ParentLayout == xnDirtyLayout)
										{
											xnLayout.AppendChild(ltnMerged.Configuration.CloneNode(true));
											break;
										}
									}
								}
							}
						}
						XmlNode xnRef;
						for (int i = 0; i < rgxnGen.Count; ++i)
						{
							if (rgixn[i] <= xnLayout.ChildNodes.Count/2)
							{
								xnRef = xnLayout.ChildNodes[rgixn[i]];
								xnLayout.InsertBefore(rgxnGen[i], xnRef);
							}
							else
							{
								if (rgixn[i] < xnLayout.ChildNodes.Count)
									xnRef = xnLayout.ChildNodes[rgixn[i]];
								else
									xnRef = xnLayout.LastChild;
								xnLayout.InsertAfter(rgxnGen[i], xnRef);
							}
						}
					}
					if (!rgxn.Contains(xnLayout))
						rgxn.Add(xnLayout);
				}
				if (IsTopLevel)
					return UpdateLayoutVisibilityIfChanged();
				if (Level > 0 && fDirty)
					StoreUpdatedValuesInConfiguration();
				return fDirty || HasMoved || IsNew;
			}

			private static string BuildXPathFromAttributes(XmlAttributeCollection attributes)
			{
				if (attributes == null)
					return "";
				string xpath = null;
				foreach(XmlAttribute attr in attributes)
				{
					if(String.IsNullOrEmpty(xpath))
					{
						xpath = "@" + attr.Name + "='" + attr.Value + "'";
					}
					else
					{
						xpath += " and @" + attr.Name + "='" + attr.Value + "'";
					}
				}
				return xpath;
			}

			internal bool IsNew { get; set; }

			private bool UpdateLayoutVisibilityIfChanged()
			{
				if (IsTopLevel && OverallLayoutVisibilityChanged())
				{
					UpdateAttribute(m_xnParentLayout, "visibility", Checked ? "always" : "never");
					return true;
				}
					return false;
				}

			private static void UpdateAttributeIfDirty(XmlNode xn, string sName, string sValue)
			{
				var sOldValue = XmlUtils.GetOptionalAttributeValue(xn, sName);
				if (StringsDiffer(sValue, sOldValue))
					UpdateAttribute(xn, sName, sValue);
			}

			private static void UpdateAttribute(XmlNode xn, string sName, string sValue)
			{
				if (xn.Attributes == null)
					return;
				Debug.Assert(sName != null);
				if (sValue == null)
				{
					// probably can't happen...
					xn.Attributes.RemoveNamedItem(sName);
					//In LayoutTreeNode(XmlNode, StringTable, string) if the flowtype is "div" we can remove "after" and "sep" attributes, so don't assert on them.
					Debug.Assert(sName == "flowType" || xn.Attributes["flowType"] != null && xn.Attributes["flowType"].Value == "div");		// only values we intentionally delete.
				}
				else if (xn.OwnerDocument != null)
				{
					var xa = xn.OwnerDocument.CreateAttribute(sName);
					xa.Value = sValue;
					xn.Attributes.SetNamedItem(xa);
				}
			}

			private void StoreUpdatedValuesInConfiguration()
			{
				if (m_xnConfig.Name != "part")
					return;
				string sDuplicate = XmlUtils.GetOptionalAttributeValue(m_xnConfig, "dup");
				if (StringsDiffer(sDuplicate, m_sDup))
				{
					// Copy Part Node
					m_xnConfig = m_xnConfig.CloneNode(true);
					UpdateAttribute(m_xnConfig, "label", m_sLabel);
					UpdateAttribute(m_xnConfig, "dup", m_sDup);
					if (m_xnHiddenNode != null)
					{
						string sNewName = String.Format("{0}_{1}",
														XmlUtils.GetManditoryAttributeValue(m_xnParentLayout, "name"),
														m_sDup);
						string sNewParam = String.Format("{0}_{1}",
														XmlUtils.GetManditoryAttributeValue(m_xnHiddenNode, "param"),
														m_sDup);
						m_xnHiddenNode = m_xnHiddenNode.CloneNode(true);
						UpdateAttribute(m_xnHiddenNode, "dup", m_sDup);
						UpdateAttribute(m_xnHiddenNode, "param", sNewParam);
						m_xnParentLayout = m_xnParentLayout.CloneNode(true);
						UpdateAttribute(m_xnParentLayout, "name", sNewName);
						//for (var ipc = 1; ipc < m_hiddenPartCallers.Count; ++ipc)
						//{
						//    var xnPartRef = m_hiddenPartCallers[ipc].PartRef.CloneNode(true);
						//    UpdateAttribute(xnPartRef, "dup", m_sDup);
						//    UpdateAttribute(xnPartRef, "param", sNewName);
						//    var xnLayout = m_hiddenPartCallers[ipc].Layout.CloneNode(true);
						//}
					}
					foreach (LayoutTreeNode ltn in m_rgltnMerged)
					{
						ltn.m_xnConfig = ltn.m_xnConfig.CloneNode(true);
						UpdateAttribute(ltn.m_xnConfig, "label", m_sLabel);
						UpdateAttribute(ltn.m_xnConfig, "dup", m_sDup);
					}
				}
				CopyPartAttributes(m_xnConfig);
				foreach (LayoutTreeNode ltn in m_rgltnMerged)
				{
					CopyPartAttributes(ltn.m_xnConfig);
				}
			}

			/// <summary>
			/// xn is a part ref element containing the currently saved version of the part that
			/// this LayoutTreeNode represents. Copy any changed information from yourself to xn.
			/// </summary>
			/// <param name="xn"></param>
			/// <returns></returns>
			private void CopyPartAttributes(XmlNode xn)
			{
				string sVisibility = XmlUtils.GetOptionalAttributeValue(xn, "visibility");
				bool fContentVisible = sVisibility != "never";
				m_fContentVisible = Checked;	// in case (un)checked in treeview, but node never selected.
				if (fContentVisible != m_fContentVisible)
					UpdateAttribute(xn, "visibility", m_fContentVisible ? "ifdata" : "never");
				UpdateAttributeIfDirty(xn, "beforeStyle", m_sBeforeStyleName);
				UpdateAttributeIfDirty(xn, "before", m_sBefore);
				UpdateAttributeIfDirty(xn, "after", m_sAfter);
				UpdateAttributeIfDirty(xn, "sep", m_sSep);
				string sWsLabel = StringServices.GetWsSpecWithoutPrefix(xn);
				if (StringsDiffer(sWsLabel, m_sWsLabel))
					UpdateAttribute(xn, "ws", m_sWsLabel);
				if (m_fStyleFromHiddenChild)
				{
					UpdateAttributeIfDirty(m_xnHiddenChild, "style", m_sStyleName);
				}
				else
				{
					if (AllowDivParaStyle)
						UpdateAttributeIfDirty(xn, "parastyle", m_sStyleName);
					else
					UpdateAttributeIfDirty(xn, "style", m_sStyleName);
				}
				UpdateAttributeIfDirty(xn, "number", m_sNumber);
				UpdateAttributeIfDirty(xn, "numstyle", m_sNumStyle);
				bool fNumSingle = XmlUtils.GetOptionalBooleanAttributeValue(xn, "numsingle", false);
				if (fNumSingle != m_fNumSingle)
					UpdateAttribute(xn, "numsingle", m_fNumSingle.ToString());
				UpdateAttributeIfDirty(xn, "numfont", m_sNumFont);
				bool fSingleGramInfoFirst = XmlUtils.GetOptionalBooleanAttributeValue(m_xnConfig, "singlegraminfofirst", false);
				if (fSingleGramInfoFirst != m_fSingleGramInfoFirst)
				{
					UpdateAttribute(xn, "singlegraminfofirst", m_fSingleGramInfoFirst.ToString());
					LayoutTreeNode ltnOther = null;
					if (ShowSenseConfig)
					{
						foreach (TreeNode n in Nodes)
						{
							LayoutTreeNode ltn = n as LayoutTreeNode;
							if (ltn != null && ltn.ShowGramInfoConfig)
							{
								ltnOther = ltn;
								break;
							}
						}
					}
					else if (ShowGramInfoConfig)
					{
						LayoutTreeNode ltn = Parent as LayoutTreeNode;
						if (ltn != null && ltn.ShowSenseConfig)
							ltnOther = ltn;
					}
					if (ltnOther != null)
						UpdateAttribute(ltnOther.m_xnConfig, "singlegraminfofirst", m_fSingleGramInfoFirst.ToString());
				}
				if (ShowSenseConfig)
				{
					bool fSenseIsPara = m_sParam != null && m_sParam.EndsWith("_AsPara");
					var sSenseParaStyle = XmlUtils.GetOptionalAttributeValue(m_xnConfig, "parastyle");
					if (fSenseIsPara != m_fSenseIsPara || sSenseParaStyle != m_sSenseParaStyle)
						UpdateSenseConfig(xn);
				}
				bool fShowComplexFormPara = XmlUtils.GetOptionalBooleanAttributeValue(m_xnConfig, "showasindentedpara", false);
				if (fShowComplexFormPara != m_fShowComplexFormPara)
					UpdateAttribute(xn, "showasindentedpara", m_fShowComplexFormPara.ToString());
				bool fShowWsLabels = XmlUtils.GetOptionalBooleanAttributeValue(xn, "showLabels", false);
				if (fShowWsLabels != m_fShowWsLabels)
					UpdateAttribute(xn, "showLabels", m_fShowWsLabels.ToString());
				if (!String.IsNullOrEmpty(LexRelType))
				{
					var sOrigRefTypeSeq = XmlUtils.GetOptionalAttributeValue(m_xnConfig, "reltypeseq");
					var sNewRefTypeSeq = LexRelTypeSequence;
					if (sOrigRefTypeSeq != sNewRefTypeSeq)
						UpdateAttribute(xn, "reltypeseq", sNewRefTypeSeq);
				}
				if (!String.IsNullOrEmpty(EntryType))
				{
					var sOrigEntryTypeSeq = XmlUtils.GetOptionalAttributeValue(m_xnConfig, "entrytypeseq");
					var sNewEntryTypeSeq = EntryTypeSequence;
					if (sOrigEntryTypeSeq != sNewEntryTypeSeq)
						UpdateAttribute(xn, "entrytypeseq", sNewEntryTypeSeq);
				}
				return;
			}

			private void UpdateSenseConfig(XmlNode xn)
			{
				if (m_fSenseIsPara)
				{
					var sParam = m_sParam;
					if (!m_sParam.EndsWith("_AsPara"))
						sParam = m_sParam + "_AsPara";
					UpdateAttribute(xn, "param", sParam);
					UpdateAttribute(xn, "flowType", "divInPara");
					UpdateAttribute(xn, "parastyle", m_sSenseParaStyle);
					UpdateAttribute(xn, "before", "");
					UpdateAttribute(xn, "sep", "");
					UpdateAttribute(xn, "after", "");
					//UpdateAttribute(xn, "number", "");
					//UpdateAttribute(xn, "singlegraminfofirst", "no");
				}
				else
				{
					var sParam = m_sParam;
					if (m_sParam.EndsWith("_AsPara"))
						sParam = m_sParam.Substring(0, m_sParam.Length - 7);
					UpdateAttribute(xn, "param", sParam);
					UpdateAttribute(xn, "flowType", null);
					UpdateAttribute(xn, "parastyle", m_sSenseParaStyle);
				}
			}

			/// <summary>
			/// If this node shows sense config information, make sure any changes are consistent with
			/// any child that also shows sense config. In particular if the numbering scheme (1.2.3 vs 1 b iii)
			/// has changed, change in all places.
			/// So far, we never have more than one child that has this property, so we don't try to handle
			/// inconsistent children.
			/// </summary>
			/// <remarks>
			/// pH 2013.09 LT-14749: Before I addressed this report, this code was intentionally not synchronising
			/// punctuation before and after the numerals.  Per a discussion with Steve McConnel, before the dialog
			/// to set these settings, users could set them by editing [project]/ConfigurationSettings/LexEntry.fwlayout
			/// or LexSense.fwlayout.  So my fix may break formatting that users had set up, iff they change settings
			/// through the dialog (Tools > Configure > Dictionary... > Sense).  However, the dialog does not provide the
			/// same granularity as editing the .fwlayout files, and therefore, if users are using the dialog, they
			/// probably expect to update formatting of sense numbers at all levels.
			/// </remarks>
			internal void MakeSenseNumberFormatConsistent()
			{
				foreach (TreeNode tn in Nodes)
				{
					LayoutTreeNode ltn = tn as LayoutTreeNode;
					if (ltn != null)
					{
						ltn.MakeSenseNumberFormatConsistent(); // recurse first, in case it has children needing to be fixed.
						if (!ShowSenseConfig || !ltn.ShowSenseConfig)
							continue;

						// Update numerals and punctuation
						string sNumber = Number;
						string sNumberChild = ltn.Number;
						if (sNumber != sNumberChild)
						{
							string sNumberOld = XmlUtils.GetOptionalAttributeValue(m_xnConfig, "number");
							string sNumberChildOld = XmlUtils.GetOptionalAttributeValue(ltn.m_xnConfig, "number");
							if (sNumber != sNumberOld)
							{
								// parent changed; make child consistent
								ltn.Number = sNumber;
							}
							else if (sNumberChild != sNumberChildOld)
							{
								// child changed; make parent consistent
								Number = sNumberChild;
							}
						}

						// Update style
						string sStyle = NumStyle;
						string sStyleChild = ltn.NumStyle;
						if (sStyle != sStyleChild)
						{
							string sStyleOld = XmlUtils.GetOptionalAttributeValue(m_xnConfig, "numstyle");
							string sStyleChildOld = XmlUtils.GetOptionalAttributeValue(ltn.m_xnConfig, "numstyle");
							if (sStyle != sStyleOld)
								ltn.NumStyle = sStyle;
							else if (sStyleChild != sStyleChildOld)
								NumStyle = sStyleChild;
						}

						// Update font
						string sFont = NumFont;
						string sFontChild = ltn.NumFont;
						if (sFont != sFontChild)
						{
							string sFontOld = XmlUtils.GetOptionalAttributeValue(m_xnConfig, "numfont");
							string sFontChildOld = XmlUtils.GetOptionalAttributeValue(ltn.m_xnConfig, "numfont");
							if (sFont != sFontOld)
								ltn.NumFont = sFont;
							else if (sFontChild != sFontChildOld)
								NumFont = sFontChild;
						}

						// Update whether a single sense is numbered
						bool bNumSingle = NumberSingleSense;
						bool bNumSingleChild = ltn.NumberSingleSense;
						if (bNumSingle != bNumSingleChild)
						{
							bool bNumSingleOld = XmlUtils.GetBooleanAttributeValue(m_xnConfig, "numsingle");
							bool bNumSingleChildOld = XmlUtils.GetBooleanAttributeValue(ltn.m_xnConfig, "numsingle");
							if (bNumSingle != bNumSingleOld)
								ltn.NumberSingleSense = bNumSingle;
							else if (bNumSingleChild != bNumSingleChildOld)
								NumberSingleSense = bNumSingleChild;
						}
					}

					// TODO: before, sep, after, param, parastyle, flowType, others? (these can wait until the refactor)
				}

			}
			internal void SplitNumberFormat(out string sBefore, out string sMark, out string sAfter)
			{
				SplitNumberFormat(Number, out sBefore, out sMark, out sAfter);
			}

			internal void SplitNumberFormat(string sNumber, out string sBefore, out string sMark, out string sAfter)
			{
				sBefore = "";
				sMark = "%O";
				sAfter = ") ";
				if (!String.IsNullOrEmpty(sNumber))
				{
					int ich = sNumber.IndexOf('%');
					if (ich < 0)
						ich = sNumber.Length;
					sBefore = sNumber.Substring(0, ich);
					if (ich < sNumber.Length)
					{
						if (ich == sNumber.Length - 1)
						{
							sMark = "%O";
							ich += 1;
						}
						else
						{
							sMark = sNumber.Substring(ich, 2);
							ich += 2;
						}
						sAfter = sNumber.Substring(ich);
					}
				}
			}

			public List<LayoutTreeNode> MergedNodes
			{
				get { return m_rgltnMerged; }
			}
		}
		#endregion // LayoutTreeNode class

		#region LayoutTypeComboItem class

		public class LayoutTypeComboItem
		{
			private string m_sLabel;
			private readonly string m_sLayout;
			private List<LayoutTreeNode> m_rgltn;

			public LayoutTypeComboItem(XmlNode xnLayoutType, List<LayoutTreeNode> rgltn)
			{
				m_sLabel = XmlUtils.GetManditoryAttributeValue(xnLayoutType, "label");
				m_sLayout = XmlUtils.GetManditoryAttributeValue(xnLayoutType, "layout");
				LayoutTypeNode = xnLayoutType;
				m_rgltn = rgltn;
			}

			public string Label
			{
				get { return m_sLabel; }
				set { m_sLabel = value; }
			}

			public string LayoutName
			{
				get { return m_sLayout; }
			}

			public List<LayoutTreeNode> TreeNodes
			{
				get { return m_rgltn; }
			}

			public override string ToString()
			{
				return m_sLabel;
			}

			internal XmlNode LayoutTypeNode { get; private set; }
		}
		#endregion

		// ReSharper disable InconsistentNaming
		private void XmlDocConfigureDlg_FormClosed(object sender, FormClosedEventArgs e)
		{
			if (DialogResult != DialogResult.OK)
			{
				if (m_fDeleteCustomFiles)
				{
					Inventory.RemoveInventory("layouts", null);
					Inventory.RemoveInventory("parts", null);
				}
			}
		}

		private void m_btnSetAll_Click(object sender, EventArgs e)
		{
			if (m_tvParts == null || m_tvParts.Nodes.Count == 0)
				return;
			foreach (TreeNode node in m_tvParts.Nodes)
				CheckNodeAndChildren(node, m_fValueForSetAll);
			m_fValueForSetAll = !m_fValueForSetAll;
			m_btnSetAll.Text = m_fValueForSetAll ? "DEBUG: Set All" : "DEBUG: Clear All";
		}

		private static void CheckNodeAndChildren(TreeNode node, bool val)
		{
			node.Checked = val;
			foreach (TreeNode tn in node.Nodes)
				CheckNodeAndChildren(tn, val);
		}

		void m_cfgSenses_SensesBtnClicked(object sender, EventArgs e)
		{
			HandleStylesBtn(m_cfgSenses.SenseStyleCombo,
				() => m_cfgSenses.FillStylesCombo(m_rgParaStyles), m_current.StyleName);
		}
		// ReSharper restore InconsistentNaming

		#region Manage Views methods

		// ReSharper disable InconsistentNaming
		/// <summary>
		/// Call the Dictionary Configuration Manager dialog when this "Manage Views" link text is clicked.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void m_linkManageViews_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			var currentItem = m_cbDictType.SelectedItem as LayoutTypeComboItem;
			if (currentItem == null)
				return;
			var current = currentItem.LayoutTypeNode;
			var configViews = (m_cbDictType.Items.OfType<LayoutTypeComboItem>().Select(
				item => item.LayoutTypeNode)).ToList();
			using (var dlg = new DictionaryConfigMgrDlg(m_mediator, m_configObjectName, configViews, current))
			{
				dlg.Text = String.Format(dlg.Text, m_configObjectName);
				var presenter = dlg.Presenter;
				if (dlg.ShowDialog() == DialogResult.OK)
					ProcessXMLConfigChanges(presenter as IDictConfigManager);
			}
		}

		private void ProcessXMLConfigChanges(IDictConfigManager presenter)
		{
			// presenter.NewConfigurationViews will give a list of copied views to create.
			var newViewsToCreate = presenter.NewConfigurationViews;
			if (newViewsToCreate != null)
				CreateConfigurationCopies(newViewsToCreate);

			// presenter.ConfigurationViewsToDelete will give a list of views to delete.
			var viewsToDelete = presenter.ConfigurationViewsToDelete;
			if (viewsToDelete != null)
				DeleteUnwantedConfigurations(viewsToDelete);

			// presenter.RenamedExistingViews will give a list of existing views whose
			//   display name has changed.
			var viewsToRename = presenter.RenamedExistingViews;
			if (viewsToRename != null)
				RenameConfigurations(viewsToRename);

			// presenter.FinalConfigurationView will give the unique code for the view
			//   that should now be active in this dialog.
			var newActiveConfig = presenter.FinalConfigurationView;
			if (newActiveConfig != null)
				SetNewActiveConfiguration(newActiveConfig);
		}
		// ReSharper restore InconsistentNaming

		private void CreateConfigurationCopies(IEnumerable<Tuple<string, string, string>> newViewsToCreate)
		{
			Dictionary<string, XmlNode> mapLayoutToConfigBase = new Dictionary<string, XmlNode>();
			foreach (var xn in(m_cbDictType.Items.OfType<LayoutTypeComboItem>().Select(item => item.LayoutTypeNode)))
			{
				if (xn == null)
					continue;
				var sLayout = XmlUtils.GetManditoryAttributeValue(xn, "layout");
				mapLayoutToConfigBase.Add(sLayout, xn);
			}
			foreach ( var viewSpec in newViewsToCreate)
			{
				var code = viewSpec.Item1;
				var baseLayout = viewSpec.Item2;
				var label = viewSpec.Item3;
				XmlNode xnBaseConfig;
				if (mapLayoutToConfigBase.TryGetValue(baseLayout, out xnBaseConfig))
					CopyConfiguration(xnBaseConfig, code, label);
			}
		}

		//
		// *** Configuration nodes look like this:
		//"<layoutType label=\"Stem-based (complex forms as main entries)\" layout=\"publishStem\">" +
		//    "<configure class=\"LexEntry\" label=\"Main Entry\" layout=\"publishStemEntry\"/>" +
		//    "<configure class=\"LexEntry\" label=\"Minor Entry\" layout=\"publishStemMinorEntry\"/>" +
		//"</layoutType>" +
		//
		private void CopyConfiguration(XmlNode xnBaseConfig, string code, string label)
		{
			var xnNewConfig = xnBaseConfig.CloneNode(true);
			//set the version number on the layoutType node to indicate user configured
// ReSharper disable PossibleNullReferenceException
// With any xml node that can possibly be copied here we have an owner and attributes
			var versionAtt = xnNewConfig.OwnerDocument.CreateAttribute("version");
			versionAtt.Value = LayoutCache.LayoutVersionNumber.ToString();
			xnNewConfig.Attributes.Append(versionAtt);
// ReSharper restore PossibleNullReferenceException
			Debug.Assert(xnNewConfig.Attributes != null);
			var xaLabel = xnNewConfig.Attributes["label"];
			xaLabel.Value = label;
			UpdateLayoutName(xnNewConfig, "layout", code);
			// make sure we copy the top-level node if it isn't directly involved in configuration.
			var className = XmlUtils.GetManditoryAttributeValue(xnBaseConfig.FirstChild, "class");
			var layoutNameChild = XmlUtils.GetManditoryAttributeValue(xnBaseConfig.FirstChild, "layout");
			var layoutName = XmlUtils.GetManditoryAttributeValue(xnBaseConfig, "layout");
			if (layoutName != layoutNameChild)
			{
				var xnBaseLayout = m_layouts.GetElement("layout", new[] {className, "jtview", layoutName, null});
				if (xnBaseLayout != null)
				{
					var xnNewBaseLayout = xnBaseLayout.CloneNode(true);
					Debug.Assert(xnNewBaseLayout.ChildNodes.Count == 1);
					UpdateLayoutName(xnNewBaseLayout, "name", code);
					UpdateLayoutName(xnNewBaseLayout.FirstChild, "param", code);
					m_layouts.AddNodeToInventory(xnNewBaseLayout);
					m_rgxnNewLayoutNodes.Add(xnNewBaseLayout);
				}
			}
			MakeSuffixLayout(code, layoutName, className);
			foreach (XmlNode xn in xnNewConfig.ChildNodes)
			{
				className = XmlUtils.GetManditoryAttributeValue(xn, "class");
				layoutName = XmlUtils.GetManditoryAttributeValue(xn, "layout");
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
			if (!string.IsNullOrEmpty(layoutSuffix))
			{
				// The view also requires a layout made by appending this suffix to the layout name. Make that also.
				var suffixLayoutName = layoutName + layoutSuffix;
				var xnSuffixLayout = m_layouts.GetElement("layout", new[] { className, "jtview", suffixLayoutName, null });
				if (xnSuffixLayout != null)
				{
					var newSuffixLayout = xnSuffixLayout.Clone();
					DuplicateLayout(newSuffixLayout, "#" + code, new List<XmlNode>());
					m_layouts.AddNodeToInventory(newSuffixLayout);
					m_rgxnNewLayoutNodes.Add(newSuffixLayout);
				}
			}
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
			if (xnLayout != null)
			{
				var duplicates = new List<XmlNode>();
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
					var nodes = xn.ChildNodes;
					if (nodes.Count == 1 && nodes[0].Name == "sublayout")
						m_rgxnNewLayoutNodes.Add(xn);
				}
			}
		}

		private static void UpdateLayoutName(XmlNode node, string attrName, string code)
		{
			Debug.Assert(node.Attributes != null);
			var xaLayout = node.Attributes[attrName];
			var oldLayout = xaLayout.Value;
			var idx = oldLayout.IndexOf(Inventory.kcMarkLayoutCopy);
			if (idx > 0)
				oldLayout = oldLayout.Remove(idx);
			xaLayout.Value = String.Format("{0}{1}{2}", oldLayout, Inventory.kcMarkLayoutCopy, code);
		}

		private void DeleteUnwantedConfigurations(IEnumerable<string> viewsToDelete)
		{
			var configDir = FdoFileHelper.GetConfigSettingsDir(m_cache.ProjectId.ProjectFolder);
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

		private static string GetConfigFilePath(XmlNode xnConfig, string configDir)
		{
			var label = XmlUtils.GetManditoryAttributeValue(xnConfig, "label");
			var className = XmlUtils.GetManditoryAttributeValue(xnConfig.FirstChild, "class");
			var name = String.Format("{0}_{1}.fwlayout", label, className);
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
				m_cbDictType.Items.Add(item.Value);
			var newSelItem = FindMatchingLayoutInMap(key, layoutMap);
			if (newSelItem == null)
				m_cbDictType.SelectedIndex = Math.Max(0, idx);
			else
				m_cbDictType.SelectedItem = newSelItem;
			m_cbDictType.EndUpdate();
		}

		private void RenameConfigurations(IEnumerable<Tuple<string, string>> viewsToRename)
		{
			var configDir = FdoFileHelper.GetConfigSettingsDir(m_cache.ProjectId.ProjectFolder);
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
			Debug.Assert(layoutMap.Count != m_cbDictType.Items.Count,
				"Rename shouldn't change number of items!");
			var idx = m_cbDictType.SelectedIndex;
			m_cbDictType.BeginUpdate();
			m_cbDictType.Items.Clear();
			foreach (var item in layoutMap)
				m_cbDictType.Items.Add(item.Value);
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
			var mapKey = layoutMap.Keys.Where(
				key => key.Contains(layoutId)).FirstOrDefault();
			if (mapKey == null)
				return null; // safety feature; shouldn't happen

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
				if (ltn == null)
					continue;
				var sLayout = XmlUtils.GetManditoryAttributeValue(ltn.LayoutTypeNode, "layout");
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
			m_mediator.SendMessage("ConfigureHomographs", this);
		}

		#region ILayoutConverter methods
		public void AddDictionaryTypeItem(XmlNode layoutNode, List<LayoutTreeNode> oldNodes)
		{
			m_cbDictType.Items.Add(new LayoutTypeComboItem(layoutNode, oldNodes));
		}

		public IEnumerable<XmlNode> GetLayoutTypes()
		{
			return m_layouts.GetLayoutTypes();
		}

		public FdoCache Cache { get { return m_cache; } }

		public StringTable StringTable { get { return m_stringTbl; } }

		public LayoutLevels LayoutLevels { get { return m_levels; } }

		public void ExpandWsTaggedNodes(string sWsTag)
		{
			m_layouts.ExpandWsTaggedNodes(sWsTag);
		}

		public void SetOriginalIndexForNode(LayoutTreeNode mainLayoutNode)
		{
			mainLayoutNode.OriginalIndex = m_tvParts.Nodes.Count;
		}

		public XmlNode GetLayoutElement(string className, string layoutName)
		{
			return LegacyConfigurationUtils.GetLayoutElement(m_layouts, className, layoutName);
		}

		public XmlNode GetPartElement(string className, string sRef)
		{
			return LegacyConfigurationUtils.GetPartElement(m_parts, className, sRef);
		}
		#endregion
	}

	/// <summary>
	/// This class provides a stack of nodes that represent a level (possibly hidden) in
	/// displaying the configuration tree.
	/// </summary>
	public class LayoutLevels
	{
		private readonly List<XmlDocConfigureDlg.PartCaller> m_stackCallers = new List<XmlDocConfigureDlg.PartCaller>();

		/// <summary>
		/// Add a set of nodes that represent a level in the tree (possibly hidden).
		/// </summary>
		public void Push(XmlNode partref, XmlNode layout)
		{
			m_stackCallers.Add(new XmlDocConfigureDlg.PartCaller(layout, partref));
		}

		/// <summary>
		/// Remove the most recent set of nodes that represent a (possibly hidden) level.
		/// </summary>
		public void Pop()
		{
			if (m_stackCallers.Count > 0)
				m_stackCallers.RemoveAt(m_stackCallers.Count - 1);
		}

		/// <summary>
		/// Get the most recent part (ref=) node.
		/// </summary>
		public XmlNode PartRef
		{
			get
			{
				return m_stackCallers.Count > 0 ? m_stackCallers[m_stackCallers.Count - 1].PartRef : null;
			}
		}

		/// <summary>
		/// Get the most recent layout node.
		/// </summary>
		public XmlNode Layout
		{
			get
			{
				return m_stackCallers.Count > 0 ? m_stackCallers[m_stackCallers.Count - 1].Layout : null;
			}
		}

		/// <summary>
		/// If the most recent part (ref=) node was "hidden", get the oldest part (ref=)
		/// on the stack that was hidden.  (This allows multiple levels of hiddenness.)
		/// </summary>
		public XmlNode HiddenPartRef
		{
			get
			{
				if (m_stackCallers.Count > 0)
				{
					XmlNode xnHidden = null;
					for (var i = m_stackCallers.Count - 1; i >= 0; --i)
					{
						if (m_stackCallers[i].Hidden)
							xnHidden = m_stackCallers[i].PartRef;
						else
							return xnHidden;
					}
					return xnHidden;
				}
				return null;
			}
		}

		/// <summary>
		/// If the most recent part (ref=) node was "hidden", get the oldest corresponding
		/// layout on the stack that was hidden.  (This allows multiple levels of
		/// hiddenness.)
		/// </summary>
		public XmlNode HiddenLayout
		{
			get
			{
				if (m_stackCallers.Count > 0)
				{
					XmlNode xnHidden = null;
					for (var i = m_stackCallers.Count - 1; i >= 0; --i)
					{
						if (m_stackCallers[i].Hidden)
							xnHidden = m_stackCallers[i].Layout;
						else
							return xnHidden;
					}
					return xnHidden;
				}
				return null;
			}
		}

		/// <summary>
		/// Return the complete list of "hidden" part (ref=)  and layoutnodes, oldest first.
		/// </summary>
		internal List<XmlDocConfigureDlg.PartCaller> HiddenPartCallers
		{
			get
			{
				List<XmlDocConfigureDlg.PartCaller> retval = null;
				for (var i = m_stackCallers.Count - 1; i >= 0; --i)
				{
					if (!m_stackCallers[i].Hidden)
						break;
					if (retval == null)
						retval = new List<XmlDocConfigureDlg.PartCaller>();
					retval.Insert(0, m_stackCallers[i]);
				}
				return retval;
			}
		}
	}
}
