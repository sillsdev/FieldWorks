// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Xml.XPath;
using LanguageExplorer.Areas;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using LanguageExplorer.Filters;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Resources;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.Xml;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// Summary description for XmlBrowseViewBaseVc.
	/// </summary>
	internal class XmlBrowseViewBaseVc : XmlVc
	{
		#region Constants

		internal const int kfragRoot = 100000;
		/// <summary></summary>
		protected const int kfragListItem = 100001;
		/// <summary></summary>
		protected const int kfragCheck = 100002;
		/// <summary></summary>
		protected const int kfragEditRow = 100003;
		/// <summary></summary>
		protected internal const int kfragListItemInner = 100004;

		/// <summary>(int)RGB(200, 255, 255)</summary>
		public const int kclrTentative = 200 + 255 * 256 + 255 * 256*256;

		#endregion Constants

		#region Data Members

		/// <summary>
		/// Specifications of columns to display
		/// </summary>
		protected List<XElement> m_columns = new List<XElement>();

		/// <summary>Top-level fake property for list of objects.</summary>
		protected int m_madeUpFieldIdentifier;

		/// <summary />
		protected IPicture m_UncheckedCheckPic;
		/// <summary />
		protected IPicture m_CheckedCheckPic;
		/// <summary />
		protected IPicture m_DisabledCheckPic;
		/// <summary>Width in millipoints of the check box.</summary>
		protected int m_dxmpCheckWidth;
		/// <summary>Roughly 1-pixel border.</summary>
		protected int m_dxmpCheckBorderWidth = 72000 / 96;
		/// <summary />
		protected XmlBrowseViewBase m_xbv;
		/// <summary />
		protected ISortItemProvider m_sortItemProvider;
		IPicture m_PreviewArrowPic;
		IPicture m_PreviewRTLArrowPic;

		// A writing system we wish to force to be used when a <string> element displays a multilingual property.
		// Todo JohnT: Get rid of this!! It is set in a <column> element and used within the display of nested
		// objects. This can produce unexpected results when the object property changes, because the inner
		// Dispay() call is not within the outer one that normally sets m_wsForce.
		// m_wsForce was introduced in place of m_columnNode as part of the process of getting rid of it.
		// The writing system calculated by looking at the hvo and flid, as well as the ws attribute.
		private int m_wsBest;

		private static bool s_haveShownDefaultColumnMessage;

		private bool m_fMultiColumnPreview = false;
		#endregion Data Members

		#region Construction and initialization

		internal string ColListId => $"{m_xbv.m_bv.PropertyTable.GetValue<string>(AreaServices.ToolChoice)}_{m_xbv.GetCorrespondingPropertyName("ColumnList")}";

		/// <summary>
		/// Gets or sets a value indicating whether [show enabled].
		/// </summary>
		public bool ShowEnabled { get; set; }

		/// <summary>
		/// Gets a value indicating whether [show columns RTL].
		/// </summary>
		public bool ShowColumnsRTL { get; protected set; }

		/// <summary>
		/// This contructor is used by SortMethodFinder to make a braindead VC.
		/// </summary>
		internal XmlBrowseViewBaseVc() // We don't have a string table.
		{
			m_madeUpFieldIdentifier = 0;
		}

		/// <summary>
		/// This contructor is used by SortMethodFinder to make a partly braindead VC.
		/// </summary>
		internal XmlBrowseViewBaseVc(XmlBrowseViewBase xbv)
		{
			TheApp = xbv.m_bv.PropertyTable.GetValue<IApp>("App");
			XmlBrowseViewBaseVcInit(xbv.Cache, xbv.DataAccess);

		}

		/// <summary>
		/// This contructor is used by FilterBar and LayoutCache to make a badly braindead VC for special, temporary use.
		/// It will fail if asked to interpret decorator properties, since it doesn't have the decorator SDA.
		/// Avoid using this constructor if possible.
		/// </summary>
		internal XmlBrowseViewBaseVc(LcmCache cache)
		{
			XmlBrowseViewBaseVcInit(cache, null);
		}
		/// <summary>
		/// This contructor is used by FilterBar and LayoutCache to make a partly braindead VC.
		/// </summary>
		internal XmlBrowseViewBaseVc(LcmCache cache, ISilDataAccess sda)
		{
			XmlBrowseViewBaseVcInit(cache, sda);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:XmlBrowseViewBaseVc"/> class.
		/// </summary>
		internal XmlBrowseViewBaseVc(XElement xnSpec, int madeUpFieldIdentifier, XmlBrowseViewBase xbv)
			: this(xbv)
		{
			Debug.Assert(xnSpec != null);
			Debug.Assert(xbv != null);
			DataAccess = xbv.SpecialCache;

			m_xbv = xbv;
			m_xnSpec = xnSpec;

			// This column list is saved in BrowseViewer.UpdateColumnList
			var savedCols = m_xbv.m_bv.PropertyTable.GetValue<string>(ColListId, SettingsGroup.LocalSettings);
			SortItemProvider = xbv.SortItemProvider;
			ComputePossibleColumns();
			XDocument doc = null;
			string target = null;
			if (!string.IsNullOrEmpty(savedCols))
			{
				doc = MigrateSavedColumnsIfNeeded(savedCols, m_xbv.m_bv.PropertyTable, ColListId);
			}
			if (doc == null) // nothing saved, or saved info won't parse
			{
				// default: the columns that have 'width' specified.
				foreach(var node in PossibleColumnSpecs)
				{
					if (XmlUtils.GetOptionalAttributeValue(node, "visibility", "always") == "always")
					{
						m_columns.Add(node);
					}
				}
			}
			else
			{
				foreach (var node in doc.Root.XPathSelectElements("//column"))
				{
					if (IsValidColumnSpec(node))
					m_columns.Add(node);
				}
			}
			m_madeUpFieldIdentifier = madeUpFieldIdentifier;
			SetupSelectColumn();
		}

		/// <summary>
		/// Don't even 'think' of using this method outside of this class. It would be private, except for some tests that want to call it directly.
		/// </summary>
		internal static XDocument MigrateSavedColumnsIfNeeded(string savedCols, IPropertyTable propertyTable, string colListId)
		{
			XDocument doc;
			try
			{
				doc = XDocument.Parse(savedCols);
				var version = XmlUtils.GetOptionalIntegerValue(doc.Root, "version", 0);
				if (version != BrowseViewer.kBrowseViewVersion)
				{
					// If we can fix problems introduced by a new version, fix them here.
					// Otherwise throw up a dialog telling the user they are losing their settings.
					switch (version)
					{
							// Process changes made in P4 Changelist 29278
						case 12:
							var target = "<column label=\"Headword\" sortmethod=\"FullSortKey\" ws=\"$ws=vernacular\" editable=\"false\" width=\"96000\"><span><properties><editable value=\"false\" /></properties><string field=\"MLHeadWord\" ws=\"vernacular\" /></span></column>";
							if (savedCols.IndexOf(target) > -1)
							{
								savedCols = savedCols.Replace(target, "<column label=\"Headword\" sortmethod=\"FullSortKey\" ws=\"$ws=vernacular\" editable=\"false\" width=\"96000\" layout=\"EntryHeadwordForFindEntry\" />");
							}
							target = "<column label=\"Lexeme Form\" visibility=\"menu\" common=\"true\" sortmethod=\"MorphSortKey\" ws=\"$ws=vernacular\" editable=\"false\"><span><properties><editable value=\"false\"/></properties><obj field=\"LexemeForm\" layout=\"empty\"><string field=\"Form\" ws=\"$ws=vernacular\"/></obj></span></column>";
							if (savedCols.IndexOf(target) > -1)
							{
								savedCols = savedCols.Replace(target, "<column label=\"Lexeme Form\" visibility=\"menu\" common=\"true\" sortmethod=\"MorphSortKey\" ws=\"$ws=vernacular\" editable=\"false\" layout=\"LexemeFormForFindEntry\"/>");
							}
							target = "<column label=\"Citation Form\" visibility=\"menu\" sortmethod=\"CitationFormSortKey\" ws=\"$ws=vernacular\" editable=\"false\"><span><properties><editable value=\"false\"/></properties><string field=\"CitationForm\" ws=\"$ws=vernacular\"/></span></column>";
							if (savedCols.IndexOf(target) > -1)
							{
								savedCols = savedCols.Replace(target, "<column label=\"Citation Form\" visibility=\"menu\" sortmethod=\"CitationFormSortKey\" ws=\"$ws=vernacular\" editable=\"false\" layout=\"CitationFormForFindEntry\"/>");
							}
							target = "<column label=\"Allomorphs\" editable=\"false\" width=\"96000\"><span><properties><editable value=\"false\"/></properties><seq field=\"AlternateForms\" layout=\"empty\" sep=\", \"><string field=\"Form\" ws=\"$ws=vernacular\"/></seq></span></column>";
							if (savedCols.IndexOf(target) > -1)
							{
								savedCols = savedCols.Replace(target, "<column label=\"Allomorphs\" editable=\"false\" width=\"96000\" ws=\"$ws=vernacular\" layout=\"AllomorphsForFindEntry\"/>");
							}
							target = "<column label=\"Glosses\" multipara=\"true\" editable=\"false\" width=\"96000\"><seq field=\"Senses\" layout=\"empty\"><para><properties><editable value=\"false\"/></properties><string field=\"Gloss\" ws=\"$ws=analysis\"/></para></seq></column>";
							if (savedCols.IndexOf(target) > -1)
							{
								savedCols = savedCols.Replace(target, "<column label=\"Glosses\" editable=\"false\" width=\"96000\" ws=\"$ws=analysis\" layout=\"GlossesForFindEntry\"/>");
							}
							savedCols = savedCols.Replace("root version=\"12\"", "root version=\"13\"");
							goto case 13;
						case 13:
							savedCols = RemoveWeatherColumn(savedCols);
							savedCols = savedCols.Replace("root version=\"13\"", "root version=\"14\"");
							goto case 14;
						case 14:
							savedCols = FixVersion15Columns(savedCols);
							savedCols = savedCols.Replace("root version=\"14\"", "root version=\"15\"");
							goto case 15;
						case 15:
							savedCols = FixVersion16Columns(savedCols);
							savedCols = savedCols.Replace("root version=\"15\"", "root version=\"16\"");
							goto case 16;
						case 16:
							savedCols = FixVersion17Columns(savedCols);
							savedCols = savedCols.Replace("root version=\"16\"", "root version=\"17\"");
							goto case 17;
						case 17:
							savedCols = FixVersion18Columns(savedCols);
							savedCols = savedCols.Replace("root version=\"17\"", "root version=\"18\"");
							propertyTable.SetProperty(colListId, savedCols, true, true, SettingsGroup.LocalSettings);
							doc = XDocument.Parse(savedCols);
							break;
						default:
							if (!s_haveShownDefaultColumnMessage)
							{
								s_haveShownDefaultColumnMessage = true; // FWR-1781 only show this once
								MessageBox.Show(null, XMLViewsStrings.ksInvalidSavedLayout, XMLViewsStrings.ksNote);
							}
							doc = null;
							// Forget the old settings, so we don't keep complaining every time the program runs.
							// Do both, since the above code was confused about the property being global or local.
							// The confusion has been resolved in favor of local.
							propertyTable.RemoveProperty(colListId, SettingsGroup.LocalSettings);
							propertyTable.RemoveProperty(colListId, SettingsGroup.GlobalSettings);
							break;
					}
				}
			}
			catch(Exception)
			{
				// If anything is wrong with the saved data (e.g., an old version that doesn't
				// parse as XML), ignore it.
				doc = null;
			}
			return doc;
		}

		/// <summary>
		/// Handles the changes we made to browse columns between 8.3 Alpha and 8.3 Beta 2
		/// 8.3 (version 18, Nov 11, 2016).
		/// </summary>
		internal static string FixVersion18Columns(string savedColsInput)
		{
			var savedCols = savedColsInput;
			savedCols = ChangeAttrValue(savedCols, "ExtNoteType", "ghostListField", "LexDb.AllPossibleExtendedNotes", "LexDb.AllExtendedNoteTargets");
			savedCols = ChangeAttrValue(savedCols, "ExtNoteType", "label", "Ext. Note Type", "Ext. Note - Type");
			savedCols = RemoveAttr(savedCols, "ExtNoteType", "editable");
			savedCols = RemoveAttr(savedCols, "ExtNoteType", "ws");
			savedCols = RemoveAttr(savedCols, "ExtNoteType", "transduce");
			savedCols = AppendAttrValue(savedCols, "ExtNoteType", "list", "LexDb.ExtendedNoteTypes");
			savedCols = AppendAttrValue(savedCols, "ExtNoteType", "field", "LexExtendedNote.ExtendedNoteType");
			savedCols = AppendAttrValue(savedCols, "ExtNoteType", "bulkEdit", "atomicFlatListItem");
			savedCols = AppendAttrValue(savedCols, "ExtNoteType", "displayWs", "best vernoranal");
			savedCols = AppendAttrValue(savedCols, "ExtNoteType", "displayNameProperty", "ShortNameTSS");
			savedCols = ChangeAttrValue(savedCols, "ExtNoteDiscussion", "ghostListField", "LexDb.AllPossibleExtendedNotes", "LexDb.AllExtendedNoteTargets");
			savedCols = ChangeAttrValue(savedCols, "ExtNoteDiscussion", "label", "Ext. Note Discussion", "Ext. Note - Discussion");
			savedCols = ChangeAttrValue(savedCols, "ExtNoteDiscussion", "editable", "false", "true");
			return savedCols;
		}

		/// <summary>
		/// Handles the changes we made to browse columns (other than additions) between roughly 7.3 (March 12, 2013) and
		/// 8.3 (version 17, June 15, 2016).
		/// </summary>
		internal static string FixVersion17Columns(string savedColsInput)
		{
			var savedCols = savedColsInput;
			savedCols = ChangeAttrValue(savedCols, "EtymologyGloss", "transduce", "LexEntry.Etymology.Gloss", "LexEtymology.Gloss");
			savedCols = ChangeAttrValue(savedCols, "EtymologySource", "transduce", "LexEntry.Etymology.Source", "LexEtymology.Source");
			savedCols = ChangeAttrValue(savedCols, "EtymologyForm", "transduce", "LexEntry.Etymology.Form", "LexEtymology.Form");
			savedCols = ChangeAttrValue(savedCols, "EtymologyComment", "transduce", "LexEntry.Etymology.Comment", "LexEtymology.Comment");
			return savedCols;
		}

		/// <summary>
		/// Handles the changes we made to browse columns (other than additions) between roughly 7.2 (April 20, 2011) and
		/// 7.3 (version 16, March 12, 2013).
		/// </summary>
		internal static string FixVersion16Columns(string savedColsInput)
		{
			var savedCols = savedColsInput;
			savedCols = ChangeAttrValue(savedCols, "CVPattern", "ws", "pronunciation", "$ws=pronunciation");
			savedCols = ChangeAttrValue(savedCols, "Tone", "ws", "pronunciation", "$ws=pronunciation");
			savedCols = ChangeAttrValue(savedCols, "ScientificNameForSense", "ws", "analysis", "$ws=analysis");
			savedCols = ChangeAttrValue(savedCols, "SourceForSense", "ws", "analysis", "$ws=analysis");

			savedCols = AppendAttrValue(savedCols, "CustomPossAtomForEntry", true, "displayNameProperty", "ShortNameTSS");
			savedCols = AppendAttrValue(savedCols, "CustomPossAtomForSense", true, "displayNameProperty", "ShortNameTSS");
			savedCols = AppendAttrValue(savedCols, "CustomPossAtomForAllomorph", true, "displayNameProperty", "ShortNameTSS");
			savedCols = AppendAttrValue(savedCols, "CustomPossAtomForExample", true, "displayNameProperty", "ShortNameTSS");

			savedCols = ChangeAttrValue(savedCols, "ComplexEntryTypesBrowse", "ws", "\\$ws=analysis", "$ws=best analysis");
			savedCols = ChangeAttrValue(savedCols, "VariantEntryTypesBrowse", "ws", "\\$ws=analysis", "$ws=best analysis");
			savedCols = ChangeAttrValue(savedCols, "ComplexEntryTypesBrowse", "originalWs", "\\$ws=analysis", "$ws=best analysis");
			savedCols = ChangeAttrValue(savedCols, "VariantEntryTypesBrowse", "originalWs", "\\$ws=analysis", "$ws=best analysis");

			savedCols = AppendAttrValue(savedCols, "EtymologyGloss", "transduce", "LexEntry.Etymology.Gloss");
			savedCols = AppendAttrValue(savedCols, "EtymologySource", "transduce", "LexEntry.Etymology.Source");
			savedCols = AppendAttrValue(savedCols, "EtymologyForm", "transduce", "LexEntry.Etymology.Form");
			savedCols = AppendAttrValue(savedCols, "EtymologyComment", "transduce", "LexEntry.Etymology.Comment");
			return savedCols;
		}

		private static string FixVersion15Columns(string savedColsInput)
		{
			var savedCols = savedColsInput;
			savedCols = new Regex("<column layout=\"IsAHeadwordForEntry\"[^>]*>").Replace(savedCols,
				"<column layout=\"PublishAsHeadword\" multipara=\"true\" label=\"Show As Headword In\" width=\"72000\" " +
				"bulkEdit=\"complexListMultiple\" field=\"LexEntry.ShowMainEntryIn\" list=\"LexDb.PublicationTypes\" displayNameProperty=\"ShortNameTSS\" " +
				"displayWs=\"best analysis\" visibility=\"dialog\"/>");
			savedCols = ChangeAttrValue(savedCols, "IsAbstractFormForEntry", "bulkEdit", "integerOnSubfield", "booleanOnSubfield");
			savedCols = ChangeAttrValue(savedCols, "ExceptionFeatures", "label", "'Exception' Features", "Exception 'Features'");
			savedCols = AppendAttrValue(savedCols, "PictureCaptionForSense", "ws", "$ws=vernacular analysis");
			savedCols = ChangeAttrValue(savedCols, "AcademicDomainsForSense", "displayWs", "analysis", "best analysis");
			savedCols = ChangeAttrValue(savedCols, "StatusForSense", "list", "LexDb.Status", "LangProject.Status");
			savedCols = AppendAttrValue(savedCols, "ComplexEntryTypesBrowse", "ghostListField", "LexDb.AllComplexEntryRefPropertyTargets");
			savedCols = AppendAttrValue(savedCols, "VariantEntryTypesBrowse", "ghostListField", "LexDb.AllVariantEntryRefPropertyTargets");

			savedCols = FixCustomFields(savedCols, "Entry_", "LexEntry");
			savedCols = FixCustomFields(savedCols, "Sense_", "LexSense");
			savedCols = FixCustomFields(savedCols, "Allomorph_", "MoForm");
			savedCols = FixCustomFields(savedCols, "Example_", "LexExampleSentence");
			return savedCols;
		}

		private static string FixCustomFields(string savedCols, string layoutNameFragment, string className)
		{
			savedCols = AppendAttrValue(savedCols, "CustomIntegerFor" + layoutNameFragment, true, "sortType", "integer");
			savedCols = AppendAttrValue(savedCols, "CustomGenDateFor" + layoutNameFragment, true, "sortType", "genDate");
			savedCols = AppendAttrValue(savedCols, "CustomPossVectorFor" + layoutNameFragment, true, "bulkEdit", "complexListMultiple");
			savedCols = AppendAttrValue(savedCols, "CustomPossVectorFor" + layoutNameFragment, true, "field", className + ".$fieldName");
			savedCols = AppendAttrValue(savedCols, "CustomPossVectorFor" + layoutNameFragment, true, "list", "$targetList");
			savedCols = AppendAttrValue(savedCols, "CustomPossVectorFor" + layoutNameFragment, true, "displayNameProperty", "ShortNameTSS");
			savedCols = AppendAttrValue(savedCols, "CustomPossAtomFor" + layoutNameFragment, true, "bulkEdit", "atomicFlatListItem");
			savedCols = AppendAttrValue(savedCols, "CustomPossAtomFor" + layoutNameFragment, true, "field", className + ".$fieldName");
			savedCols = AppendAttrValue(savedCols, "CustomPossAtomFor" + layoutNameFragment, true, "list", "$targetList");
			return savedCols;
		}

		private static string ChangeAttrValue(string savedCols, string layoutName, string attrName, string attrValue, string replaceWith)
		{
			var pattern = new Regex("<column [^>]*layout *= *\"" + layoutName + "\"[^>]*" + attrName + " *= *\"(" + attrValue + ")\"");
			var match = pattern.Match(savedCols);
			if (match.Success)
			{
				var index = match.Groups[1].Index;
				// It is better to use Groups(1).Length here rather than attrValue.Length, because there may be some RE pattern
				// in attrValue (e.g., \\$) which would make a discrepancy.
				savedCols = savedCols.Substring(0, index) + replaceWith + savedCols.Substring(index +match.Groups[1].Length);
			}
			return savedCols;
		}

		private static string RemoveAttr(string savedCols, string layoutName, string attrName)
		{
			var pattern = new Regex("<column [^>]*layout *= *\"" + layoutName + "\"[^>]*(" + attrName + "=\"[^\r\n\t\f ]*\" )");
			var match = pattern.Match(savedCols);
			if (match.Success)
			{
				var index = match.Groups[1].Index;
				// It is better to use Groups(1).Length here rather than attrValue.Length, because there may be some RE pattern
				// in attrValue (e.g., \\$) which would make a discrepancy.
				savedCols = savedCols.Substring(0, index) + savedCols.Substring(index + match.Groups[1].Length);
			}
			return savedCols;
		}

		private static string AppendAttrValue(string savedCols, string layoutName, string attrName, string attrValue)
		{
			return AppendAttrValue(savedCols, layoutName, false, attrName, attrValue);
		}

		private static string AppendAttrValue(string savedCols, string layoutName, bool customField, string attrName, string attrValue)
		{
			// If it's not a custom field we expect to know the layout name exactly, so should match a closing quote.
			// If it is a custom field, any layout name that starts with the expected string...typically generated from something like
			// "CustomIntegerForEntry_$fieldName"...is a match.
			var close = customField ? string.Empty : "\"";
			var pattern = new Regex("<column [^>]*layout *= *\"" + layoutName + close + "[^>]*/>");
			var match = pattern.Match(savedCols);
			if (match.Success)
			{
				var index = match.Index + match.Length - 2; // just before closing />
				var leadIn = savedCols.Substring(0, index);
				var gap = " "; // for neatness don't put an extra space if we already have one.
				if (leadIn.EndsWith(" "))
				{
					gap = string.Empty;
				}
				savedCols = leadIn + gap + attrName + "=\"" + attrValue + "\"" + savedCols.Substring(index);
			}
			return savedCols;
		}

		/// <summary>
		/// This contructor is used by SortMethodFinder to make a braindead VC.
		/// </summary>
		private void XmlBrowseViewBaseVcInit(LcmCache cache, ISilDataAccess sda)
		{
			Debug.Assert(cache != null);

			m_madeUpFieldIdentifier = 0;
			Cache = cache;	// sets m_mdc and m_layouts as well as m_cache.
			if (sda != null)
			{
				DataAccess = sda;
			}
		}

		private static string RemoveWeatherColumn(string savedCols)
		{
			var pattern = new Regex("<column layout=\"Weather\"[^>]*>");
			return pattern.Replace(savedCols, "");
		}

		/// <summary>
		/// Setup the check-mark column, if desired.
		/// </summary>
		protected virtual void SetupSelectColumn()
		{
			var xa = m_xnSpec.Attribute("selectColumn");
			HasSelectColumn = xa != null && xa.Value == "true";
			if (HasSelectColumn)
			{
				// Only need these if showing the selected column.
				m_UncheckedCheckPic = TheApp.PictureHolder.GetPicture("UncheckedCheckBox", ResourceHelper.UncheckedCheckBox);
				m_CheckedCheckPic = TheApp.PictureHolder.GetPicture("CheckedCheckBox", ResourceHelper.CheckedCheckBox);
				m_DisabledCheckPic = TheApp.PictureHolder.GetPicture("DisabledCheckBox", ResourceHelper.DisabledCheckBox);
				// We want a width in millipoints (72000/inch). Value we have is in 100/mm.
				// There are 25.4 mm/inch.
				m_dxmpCheckWidth = m_UncheckedCheckPic.Width * 72000 / 2540;
			}
		}

		/// <summary>
		/// Provide access to the special tag used to implement highlighting the selected row.
		/// </summary>
		internal int TagMe { get; } = XMLViewsDataCache.ktagTagMe;

		/// <summary>
		/// compute and return possible columns. This always recomputes them, so may pick
		/// up generated columns for new custom fields.
		/// </summary>
		public List<XElement> ComputePossibleColumns()
		{
			XmlVc vc = null;
			if (ListItemsClass != 0)
			{
				vc = this;
			}
			PossibleColumnSpecs = PartGenerator.GetGeneratedChildren(m_xnSpec.Element("columns"), m_xbv.Cache, vc, (int)ListItemsClass);
			return PossibleColumnSpecs;
		}

		int m_listItemsClass;
		/// <summary>
		/// the class (or base class) of the items in SortItemProvider
		/// </summary>
		internal int ListItemsClass
		{
			get
			{
				if (m_listItemsClass != 0)
				{
					return m_listItemsClass;
				}
				// try to get the listItemsClass information
				// so we can build our columns (and custom fields) properly.
				if (SortItemProvider != null)
				{
					// Remember, if we have a sortItemProvider, we can use this class info to generate parts for specific custom fields
					m_listItemsClass = SortItemProvider.ListItemsClass;

				}
				else
				{
					// we still need to know what listItemsClass to expect this list to be based on.
					var listItemsClass = XmlUtils.GetMandatoryAttributeValue(m_xnSpec, "listItemsClass");
					m_listItemsClass = m_cache.MetaDataCacheAccessor.GetClassId(listItemsClass);

				}
				return m_listItemsClass;
			}

			set
			{
				m_listItemsClass = value;
			}
		}

		/// <summary>
		/// check to see if column spec is still valid and useable.
		/// </summary>
		internal bool IsValidColumnSpec(XElement node)
		{
			var possibleColumns = PossibleColumnSpecs;
			// first, check to see if we can find some part or child node information
			// to process. Eg. Custom field column nodes that refer to parts that no longer exist
			// because the custom field has been removed so the parts cannot be generated
			var partNode = GetPartFromParentNode(node, ListItemsClass);
			if (partNode == null)
			{
				return false;	// invalid node, don't add.
			}
			var badCustomField = CheckForBadCustomField(possibleColumns, node);
			if (badCustomField)
			{
				return false;	// invalid custom field, don't add.
			}
			var badReversalIndex = CheckForBadReversalIndex(node);
			return !badReversalIndex;
		}

		/// <summary>
		/// Check for a nonexistent custom field.  (Custom fields can be deleted.)  As a side-effect,
		/// if the node refers to a valid custom field, the label attribute is adjusted to what we
		/// want the user to see.
		/// </summary>
		/// <returns>true if this node refers to a nonexistent custom field</returns>
		private bool CheckForBadCustomField(List<XElement> possibleColumns, XElement node)
		{
			// see if this node is based on a layout. If so, get its part
			PropWs propWs;
			XElement columnForCustomField;
			if (!TryColumnForCustomField(node, ListItemsClass, out columnForCustomField, out propWs))
			{
				return false;
			}
			if (columnForCustomField != null)
			{
				var fieldName = XmlUtils.GetOptionalAttributeValue(columnForCustomField, "field");
				var className = XmlUtils.GetOptionalAttributeValue(columnForCustomField, "class");
				if (string.IsNullOrEmpty(fieldName) || String.IsNullOrEmpty(className))
				{
					return true;
				}
				if ((m_mdc as IFwMetaDataCacheManaged).FieldExists(className, fieldName, false))
				{
					ColumnConfigureDialog.GenerateColumnLabel(node, m_cache);
					return false;
				}
				return true;
			}
			if (propWs != null)
			{
				XmlUtils.SetAttribute(node, "originalLabel", GetNewLabelFromMatchingCustomField(possibleColumns, propWs.Flid));
				ColumnConfigureDialog.GenerateColumnLabel(node, m_cache);
			}
			else
			{
				// it's an invalid custom field.
				return true;
			}
			return false;
		}



		private string GetNewLabelFromMatchingCustomField(List<XElement> possibleColumns, int flid)
		{
			foreach (var possibleColumn in possibleColumns)
			{
				// Desired node may be a child of a child...  (See LT-6447.)
				PropWs propWs;
				XElement columnForCustomField;
				if (TryColumnForCustomField(possibleColumn, ListItemsClass, out columnForCustomField, out propWs))
				{
					// the flid of the updated custom field node matches the given flid of the old node.
					if (propWs != null && propWs.Flid == flid)
					{
						var label = StringTable.Table.LocalizeAttributeValue(XmlUtils.GetOptionalAttributeValue(possibleColumn, "label", null));
						return label;
					}
				}
			}
			return string.Empty;
		}

		/// <summary>
		/// Check for an invalid reversal index.  (Reversal indexes can be deleted.)
		/// </summary>
		/// <returns>true if this node refers to a nonexistent reversal index.</returns>
		private bool CheckForBadReversalIndex(XElement node)
		{
			// Look for a child node which is similar to this (value of ws attribute may differ):
			// <string field="ReversalEntriesText" ws="$ws=es"/>
			var child = XmlUtils.FindElement(node, "string");
			if (child == null || XmlUtils.GetOptionalAttributeValue(child, "field") != "ReversalEntriesText")
			{
				return false;
			}
			var sWs = StringServices.GetWsSpecWithoutPrefix(XmlUtils.GetOptionalAttributeValue(child, "ws"));
			if (sWs == null || sWs == "reversal")
			{
				return false;
			}
			if (!m_cache.ServiceLocator.WritingSystemManager.Exists(sWs))
			{
				return true;	// invalid writing system
			}
			// Check whether we have a reversal index for the given writing system.
			foreach (var idx in m_cache.LangProject.LexDbOA.ReversalIndexesOC)
			{
				if (idx.WritingSystem == sWs)
				{
					return false;
				}
			}
			return true;
		}
		#endregion Construction and initialization

		#region Properties

		/// <summary>
		/// Gets or sets the override allow edit column.
		/// </summary>
		public int OverrideAllowEditColumn { get; set; } = -1;

		/// <summary>
		/// Gets a value indicating whether this instance has select column.
		/// </summary>
		public bool HasSelectColumn { get; protected set; }

		/// <summary>
		/// Specifies the default state of check boxes
		/// </summary>
		public bool DefaultChecked => !(m_sda is XMLViewsDataCache) || ((XMLViewsDataCache)m_sda).DefaultSelected;

		/// <summary>
		/// Gets the width of the select column.
		/// </summary>
		public int SelectColumnWidth => m_dxmpCheckWidth + 4 * m_dxmpCheckBorderWidth;

		internal virtual List<XElement> ColumnSpecs
		{
			get
			{
				return m_columns;
			}
			set
			{
				m_columns = value;
			}
		}

		protected internal List<XElement> PossibleColumnSpecs { get; protected set; }

		internal ISortItemProvider SortItemProvider
		{
			get
			{
				return m_sortItemProvider;
			}
			set
			{
				m_sortItemProvider = value;
			}
		}

		/// <summary>
		/// Use this to store a suitable preview arrow if we will be displaying previews.
		/// (See BulkEditBar.)
		/// </summary>
		internal Image PreviewArrow
		{
			set
			{
				m_PreviewArrowPic = TheApp.PictureHolder.GetPicture("PreviewArrow", value);
				var x = value;
				x.RotateFlip(RotateFlipType.Rotate180FlipNone);
				m_PreviewRTLArrowPic = TheApp.PictureHolder.GetPicture("PreviewRTLArrow", x);
			}
		}

		internal bool HasPreviewArrow => m_PreviewArrowPic != null;

		#endregion Properties

		#region Other methods

		/// <summary>
		///  Convert a .NET color to the type understood by Views code and other Win32 stuff.
		/// </summary>
		public static uint RGB(Color c)
		{
			return RGB(c.R, c.G, c.B);
		}

		/// <summary>
		/// Make a standard Win32 color from three components.
		/// </summary>
		public static uint RGB(int r, int g, int b)
		{
			return (uint)((byte)r|((byte)g << 8) | ((byte)b << 16));
		}

		/// <summary>
		/// Gets or sets the color of the border.
		/// </summary>
		public Color BorderColor { get; set; } = SystemColors.Control;

		const int kclrBackgroundSelRow = 0xFFE6D7;

		internal virtual int SelectedRowBackgroundColor(int hvo)
		{
			return kclrBackgroundSelRow;
		}

		/// <summary>
		/// Add a new table/row to the view for one existing object.
		/// </summary>
		protected virtual void AddTableRow(IVwEnv vwenv, int hvo, int frag)
		{
			// set the border color
			vwenv.set_IntProperty((int )FwTextPropType.ktptBorderColor, (int)FwTextPropVar.ktpvDefault, (int)RGB(BorderColor));

			// If we're using this special mode where just one column is editable, we need to make
			// sure as far as possible that everything else is not.
			if (OverrideAllowEditColumn >= 0)
			{
				vwenv.set_IntProperty((int)FwTextPropType.ktptEditable, (int) FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
			}

			int index, hvoDummy, tagDummy;
			var clev = vwenv.EmbeddingLevel;
			vwenv.GetOuterObject(clev - 2, out hvoDummy, out tagDummy, out index);
			if (index >= m_sda.get_VecSize(hvoDummy, tagDummy))
			{
				return; // something to fix.
			}

			if (index == m_xbv.SelectedIndex && m_xbv.SelectedRowHighlighting != SelectionHighlighting.none)
			{
				vwenv.set_IntProperty((int)FwTextPropType.ktptBackColor, (int)FwTextPropVar.ktpvDefault, SelectedRowBackgroundColor(hvo));
				if (m_xbv.SelectedRowHighlighting == SelectionHighlighting.border)
				{
					vwenv.set_IntProperty((int)FwTextPropType.ktptBorderTop, (int)FwTextPropVar.ktpvMilliPoint, 3000);
					vwenv.set_IntProperty((int)FwTextPropType.ktptBorderColor, (int)FwTextPropVar.ktpvDefault, (int)RGB(Color.FromKnownColor(KnownColor.Highlight)));
				}
			}

			// Make a table.
			var rglength = m_xbv.GetColWidthInfo();
			var colCount = m_columns.Count;
			if (HasSelectColumn)
			{
				colCount++;
			}

			// LT-7014, 7058: add the additional columns that are needed
			if (rglength.Length < colCount)
			{
				var rglengthNEW = new VwLength[colCount];
				for (var ii = 0; ii < colCount; ii++)
				{
					if (ii < rglength.Length)
					{
						rglengthNEW[ii] = rglength[ii];
					}
					else
					{
						rglengthNEW[ii] = rglength[0];
					}
				}
				rglength = rglengthNEW;
			}

			// If the only columns specified are custom fields which have been deleted,
			// we can't show anything!  (and we don't want to crash -- see LT-6449)
			if (rglength.Length == 0)
			{
				return;
			}

			VwLength vl100; // Length representing 100% of the table width.
			vl100.unit = rglength[0].unit;
			vl100.nVal = 1;
			for (var i = 0; i < colCount; ++i)
			{
				Debug.Assert(vl100.unit == rglength[i].unit);
				vl100.nVal += rglength[i].nVal;
			}

			vwenv.OpenTable(colCount, // this many columns
				vl100, // using 100% of available space
				72000 / 96, //0, // no border
				VwAlignment.kvaLeft, // cells by default left aligned
				//	VwFramePosition.kvfpBelow, //.kvfpBox, //.kvfpVoid, // no frame
				VwFramePosition.kvfpBelow | VwFramePosition.kvfpRhs,
				VwRule.kvrlCols, // vertical lines between columns
				0, // no space between cells
				0, // no padding within cell.
				false);
			// Set column widths.
			for (var i = 0; i < colCount; ++i)
			{
				vwenv.MakeColumns(1, rglength[i]);
			}
			// the table only has a body (no header or footer), and only one row.
			vwenv.OpenTableBody();
			vwenv.OpenTableRow();

			if (HasSelectColumn)
			{
				AddSelectionCell(vwenv, hvo);
			}
			// Make the cells.
			int hvoRoot, tagDummy2, ihvoDummy;
			vwenv.GetOuterObject(0, out hvoRoot, out tagDummy2, out ihvoDummy);
			var icolActive = GetActiveColumn(vwenv, hvoRoot);
			// if m_fShowSelected is true, we get an extra column of checkmarks that gives us
			// a different index into the columns. This allows the one-based indexing to work
			// in the call to ColumnSortedFromEnd. Without this, the one-based indexing has to
			// be adjusted to zero-based indexing.
			var cAdjCol = HasSelectColumn ? 0 : 1;
			if (ShowColumnsRTL)
			{
				for (var icol = m_columns.Count; icol > 0; --icol)
				{
					AddTableCell(vwenv, hvo, index, hvoRoot, icolActive, cAdjCol, icol);
				}
			}
			else
			{
				for (var icol = 1; icol <= m_columns.Count; ++icol)
				{
					AddTableCell(vwenv, hvo, index, hvoRoot, icolActive, cAdjCol, icol);
				}
			}
			vwenv.CloseTableRow();
			vwenv.CloseTableBody();
			vwenv.CloseTable();
		}

		/// <summary />
		protected virtual int GetActiveColumn(IVwEnv vwenv, int hvoRoot)
		{
			var icolActive = -1;
			if (vwenv.DataAccess.get_IsPropInCache(hvoRoot, XMLViewsDataCache.ktagActiveColumn, (int) CellarPropertyType.Integer, 0))
			{
				icolActive = vwenv.DataAccess.get_IntProp(hvoRoot, XMLViewsDataCache.ktagActiveColumn);
			}
			return icolActive;
		}

		/// <summary>
		/// A writing system we wish to force all string elements to use for multilingual properties.
		/// We want to get rid of this; don't use it unless you must.
		/// </summary>
		internal override int WsForce { get; set; }

		private void AddTableCell(IVwEnv vwenv, int hvo, int index, int hvoRoot, int icolActive, int cAdjCol, int icol)
		{
			var node = m_columns[icol - 1];
			// Figure out the underlying Right-To-Left value.
			var fRightToLeft = false;
			// Figure out if this column's writing system is audio.
			var fVoice = false;
			m_wsBest = GetBestWsForNode(node, hvo);
			if (m_wsBest != 0)
			{
				var ws = m_cache.ServiceLocator.WritingSystemManager.Get(m_wsBest);
				if (ws != null)
				{
					fRightToLeft = ws.RightToLeftScript;
					fVoice = ws.IsVoice;
				}
			}
			var fSortedFromEnd = m_xbv.ColumnSortedFromEnd(icol - cAdjCol);
			int tal;
			if (fRightToLeft)
			{
				tal = fSortedFromEnd ? (int) FwTextAlign.ktalLeft : (int) FwTextAlign.ktalRight;
			}
			else
			{
				tal = fSortedFromEnd ? (int) FwTextAlign.ktalRight : (int) FwTextAlign.ktalLeft;
			}
			vwenv.set_IntProperty((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, tal);

			// If this cell is audio, don't allow editing.
			if (fVoice)
			{
				SetCellEditability(vwenv, false);
			}

			var fIsCellActive = false;
			if (icolActive != 0 && m_PreviewArrowPic != null)
			{
				// The display depends on which column is active and whether the current row is selected.
				vwenv.NoteDependency(new[] { hvoRoot, hvo, hvo }, new[] { XMLViewsDataCache.ktagActiveColumn, XMLViewsDataCache.ktagItemSelected, XMLViewsDataCache.ktagItemEnabled }, 3);
				// We're doing the active column thing.
				if (MultiColumnPreview)
				{
					fIsCellActive = vwenv.DataAccess.get_IntProp(hvo, XMLViewsDataCache.ktagItemSelected) != 0
									&& vwenv.DataAccess.get_StringProp(hvo, XMLViewsDataCache.ktagAlternateValueMultiBase + icol).Length > 0;
				}
				else if (icol == icolActive)
				{
					fIsCellActive = vwenv.DataAccess.get_IntProp(hvo, XMLViewsDataCache.ktagItemSelected) != 0
						&& vwenv.DataAccess.get_IntProp(hvo, XMLViewsDataCache.ktagItemEnabled) != 0;
				}
			}
			// Make a cell and embed the contents of the column node.
			ProcessProperties(node, vwenv);
			SetCellProperties(index, icol, node, hvo, vwenv, fIsCellActive);
			vwenv.set_IntProperty((int)FwTextPropType.ktptPadTop, (int)FwTextPropVar.ktpvMilliPoint, 1607);
			vwenv.OpenTableCell(1, 1);
			vwenv.set_IntProperty((int)FwTextPropType.ktptPadTrailing, (int)FwTextPropVar.ktpvMilliPoint, 1607);
			vwenv.set_IntProperty((int)FwTextPropType.ktptPadLeading, (int)FwTextPropVar.ktpvMilliPoint, 1607);
			if (node.Name == "column")
			{
				// Paragraph directionality must be set before the paragraph is opened.
				vwenv.set_IntProperty((int)FwTextPropType.ktptRightToLeft, (int)FwTextPropVar.ktpvEnum, IsWritingSystemRTL(node) ? -1 : 0);
			}

			// According to LT-8947, in bulk edit preview mode, we want to try to show the
			// original cell contents on the same line with the preview arrow and the new cell contents.
			// to accomplish this, we use 3 InnerPiles in a paragraph
			// to try to keep them on the same line (or wrap otherwise).
			// NOTE: the bottom two inner piles will only be present if fIsCellActive is true.
			//
			// <Paragraph>
			//		<InnerPile>
			//			<paragraph> Original cell contents </paragraph>
			//		</InnerPile>
			//		<InnerPile>
			//			<paragraph> Arrow Picture </paragraph>
			//		</InnerPile>
			//		<InnerPile>
			//			<paragraph> Alternate cell contents </paragraph>
			//		</InnerPile>
			// </Paragraph>
			//

			vwenv.OpenParagraph(); // <Paragraph>
			var multiPara = XmlUtils.GetOptionalBooleanAttributeValue(node, "multipara", false);
			vwenv.OpenInnerPile(); // <InnerPile>
			// if the multi-para attribute is not specified, create a paragraph to wrap the cell contents.
			var fParaOpened = false;
			if (!multiPara)
			{
				vwenv.OpenParagraph(); // <paragraph>
				fParaOpened = true;
			}

			if (m_sortItemProvider == null)
			{
				try
				{
					if (node.Name == "column")
					{
						SetForcedWs(node);
					}
				var nodeToProcess = GetColumnNode(node, hvo, m_sda, LayoutCache);
				ProcessChildren(nodeToProcess, vwenv, hvo, null);
			}
				finally
				{
					// reset the ws for next column in the row.
					WsForce = 0;
				}
			}
			else
			{
				var level = vwenv.EmbeddingLevel;
				int hvoDum, tag, ihvo;
				vwenv.GetOuterObject(level - 2, out hvoDum, out tag, out ihvo);
				Debug.Assert(tag == m_madeUpFieldIdentifier);
				var item = m_sortItemProvider.SortItemAt(ihvo);
				if (item != null)
				{
					DisplayCell(item, node, hvo, vwenv); // (Original) cell contents
				}
			}

			if (fParaOpened)
			{
				vwenv.CloseParagraph(); // (Original) cell contents </paragraph>
				fParaOpened = false;
			}
			vwenv.CloseInnerPile(); // </InnerPile>
			if (fIsCellActive)
			{
				AddPreviewPiles(vwenv, node, icol);
			}
			vwenv.CloseParagraph(); // </Paragraph>
			vwenv.CloseTableCell();
			m_wsBest = 0;
		}

		private int GetBestHvoAndFlid(int hvo, XElement xn, out int flidBest)
		{
			// The 'best' ws series needs the hvo and flid in order to work.
			// Some others (e.g., 'reversal' which require the hvo don't need a flid.
			var hvoBest = hvo; // Default assumption, a field of the target.
			flidBest = 0;
			var fieldname = XmlUtils.GetOptionalAttributeValue(xn, "field");
			if (fieldname == null)
			{
				return 0; // can't find a useful combination.
			}
			var mdc = m_mdc as IFwMetaDataCacheManaged;
			var objectClid = m_sda.get_IntProp(hvo, CmObjectTags.kflidClass);
			if (xn.Parent != null && xn.Parent.Name == "obj")
			{
				// Special case: object property, string field is on a derived object.
				var fieldObj = XmlUtils.GetOptionalAttributeValue(xn.Parent, "field");
				if (mdc.FieldExists(objectClid, fieldObj, true))
				{
					var flidObj = m_mdc.GetFieldId2(objectClid, fieldObj, true);
					hvoBest = m_sda.get_ObjectProp(hvo, flidObj);
					if (hvoBest == 0)
					{
						return 0; // target object does not exist
					}
					objectClid = m_sda.get_IntProp(hvoBest, CmObjectTags.kflidClass); // interpret main fieldname relative to this object.
				}
			}
			if (mdc.FieldExists(objectClid, fieldname, true))
			{
				flidBest = m_mdc.GetFieldId2(objectClid, fieldname, true);
				return hvoBest;
			}
			return 0; // can't find a useful combination.
		}

		/// <summary>
		/// Passed a column node, determines whether it indicates a forced writing system, and if so sets it.
		/// </summary>
		private void SetForcedWs(XElement node)
		{
			var wsSpec = XmlUtils.GetOptionalAttributeValue(node, "ws");
			if (wsSpec == null)
			{
				return;
			}
			var realWsSpec = StringServices.GetWsSpecWithoutPrefix(wsSpec);
			// If it's a magic wsId, we must NOT call GetWsFromStr, that is VERY slow
			// when what we pass isn't a valid WS name.
			var magicWsId = WritingSystemServices.GetMagicWsIdFromName(realWsSpec);
			if (magicWsId != 0)
			{
				WsForce = magicWsId;
			}
			else
			{
				WsForce = m_cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr(realWsSpec);
				if (WsForce == 0)
				{
					WsForce = m_wsBest;
				}
			}
		}

		/// <summary>
		/// Set the properties of the table cell we are about to create.
		/// </summary>
		protected virtual void SetCellProperties(int rowIndex, int icol, XElement node, int hvo, IVwEnv vwenv, bool fIsCellActive)
		{
			// By default we only need to worry about editability for the active row.
			if (rowIndex == m_xbv.SelectedIndex)
			{
				SetCellEditability(vwenv, AllowEdit(icol, node, fIsCellActive, hvo));
			}
		}

		/// <summary>
		/// Set the actual editability and related appearance of the box (typically table cell) we will next create.
		/// </summary>
		protected void SetCellEditability(IVwEnv vwenv, bool fEditable)
		{
			if (fEditable)
			{
				vwenv.set_IntProperty((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptIsEditable);
				vwenv.set_IntProperty((int)FwTextPropType.ktptBackColor, (int)FwTextPropVar.ktpvDefault, (int)RGB(Color.FromKnownColor(KnownColor.Window)));
			}
			else
			{
				vwenv.set_IntProperty((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
			}
		}

		private void AddPreviewPiles(IVwEnv vwenv, XElement node, int icol)
		{
			// add padding to separate PreviewArrow from other cell contents.
			vwenv.set_IntProperty((int)FwTextPropType.ktptPadTrailing, (int)FwTextPropVar.ktpvMilliPoint, 2500);
			vwenv.set_IntProperty((int)FwTextPropType.ktptPadLeading, (int)FwTextPropVar.ktpvMilliPoint, 2500);
			vwenv.OpenInnerPile();
			{
				AddPreviewArrow(vwenv, node);
			}
			vwenv.CloseInnerPile();
			vwenv.OpenInnerPile();
			{
				AddAlternateCellContents(vwenv, icol);
			}
			vwenv.CloseInnerPile();
		}

		private void AddPreviewArrow(IVwEnv vwenv, XElement node)
		{
			vwenv.OpenParagraph();
			vwenv.AddPicture(IsWritingSystemRTL(node) ? m_PreviewRTLArrowPic : m_PreviewArrowPic, 0, 0, 0);
			vwenv.CloseParagraph();
		}

		/// <summary />
		private void AddAlternateCellContents(IVwEnv vwenv, int icol)
		{
			vwenv.OpenParagraph();
			if (MultiColumnPreview)
			{
				vwenv.AddStringProp(XMLViewsDataCache.ktagAlternateValueMultiBase + icol, this);
			}
			else
			{
				vwenv.AddStringProp(XMLViewsDataCache.ktagAlternateValue, this);
			}
			vwenv.CloseParagraph();
		}

		private void AddSelectionCell(IVwEnv vwenv, int hvo)
		{
			vwenv.OpenTableCell(1, 1);

			// Put a pixel of light grey pad around the check box itself.
			vwenv.set_IntProperty((int)FwTextPropType.ktptBackColor, (int)FwTextPropVar.ktpvDefault, (int)RGB(Color.LightGray));
			vwenv.set_IntProperty((int)FwTextPropType.ktptPadLeading, (int)FwTextPropVar.ktpvMilliPoint, (m_dxmpCheckBorderWidth));
			vwenv.set_IntProperty((int)FwTextPropType.ktptPadTrailing, (int)FwTextPropVar.ktpvMilliPoint, (m_dxmpCheckBorderWidth));
			vwenv.set_IntProperty((int)FwTextPropType.ktptPadTop, (int)FwTextPropVar.ktpvMilliPoint, (m_dxmpCheckBorderWidth));
			vwenv.set_IntProperty((int)FwTextPropType.ktptPadBottom, (int)FwTextPropVar.ktpvMilliPoint, m_dxmpCheckBorderWidth);
			if (ShowEnabled)
			{
				var enabled = vwenv.DataAccess.get_IntProp(hvo, XMLViewsDataCache.ktagItemEnabled);
				vwenv.NoteDependency(new[] { hvo }, new[] { XMLViewsDataCache.ktagItemEnabled }, 1);
				if (enabled != 0)
				{
					// Even if the view as a whole does not allow editing, presumably a check box
					// does!
					vwenv.set_IntProperty((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptIsEditable);
					vwenv.AddIntPropPic(XMLViewsDataCache.ktagItemSelected, this, kfragCheck, 0, 1);
				}
				else
				{
					vwenv.AddPicture(m_DisabledCheckPic, 0, 0, 0);
				}
			}
			else
			{
				// Unconditionally show the selected property.
				vwenv.set_IntProperty((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptIsEditable);
				vwenv.AddIntPropPic(XMLViewsDataCache.ktagItemSelected, this, kfragCheck, 0, 1);
			}

			vwenv.CloseTableCell();
		}

		/// <summary>
		/// This is the core routine for XmlVc, which determines how to display the object indicated by hvo,
		/// using the XML 'fragment' given in frag, which may be affected by 'caller', the context (typically
		/// I think a part ref). This is called as part of Display to actually put the data into the vwenv.
		/// NOTE: a parallel routine, DetermineNeededFieldsFor, figures out the data that may be needed
		/// by ProcessFrag, and should be kept in sync with it. This special frag allows a preview to be explicitly inserted into a special-purpose column,
		/// such as one where we want it to be part of a concordance paragraph.
		/// </summary>
		public override void ProcessFrag(XElement frag, IVwEnv vwenv, int hvo, bool fEditable, XElement caller)
		{
			if (frag.Name == "preview")
			{
				vwenv.NoteDependency(new[] {hvo, hvo}, new[] {XMLViewsDataCache.ktagItemSelected, XMLViewsDataCache.ktagItemEnabled}, 2);
				// Explicitly insert the preview here, if enabled etc.
				if (vwenv.DataAccess.get_IntProp(hvo, XMLViewsDataCache.ktagItemSelected) != 0 && vwenv.DataAccess.get_IntProp(hvo, XMLViewsDataCache.ktagItemEnabled) != 0)
				{
					if (!HasPreviewArrow)
					{
						PreviewArrow = BulkEditBar.PreviewArrowStatic;
					}
					Debug.Assert(!MultiColumnPreview, "Expect only single column preview, not multi-column preview");
					AddPreviewPiles(vwenv, frag, 0);
				}
			}
			else
			{
				base.ProcessFrag(frag, vwenv, hvo, fEditable, caller);
			}
		}

		/// <summary>
		/// Given a IManyOnePathSortItem that describes one of the rows in the view, and a node
		/// that configures one of the columns, make the required calls to the VwEnv to display the cell
		/// contents. This is also used by the LayoutFinder.
		/// </summary>
		public void DisplayCell(IManyOnePathSortItem item, XElement node, int hvo, IVwEnv vwenv)
		{
			var outerParts = new List<XElement>();
			int hvoToDisplay;
			var dispCommand = XmlViewsUtils.GetDisplayCommandForColumn(item, node, m_mdc, m_sda, LayoutCache, out hvoToDisplay, outerParts);
			// See if the column has a writing system established for it.
			try
			{
				if (node.Name == "column")
				{
					SetForcedWs(node);
				}
				OpenOuterParts(outerParts, vwenv, hvo);
				if (hvoToDisplay == hvo)
				{
					dispCommand.PerformDisplay(this, 0, hvo, vwenv);
				}
				else
				{
					var fragId = GetId(dispCommand, m_idToDisplayCommand, m_displayCommandToId);
					vwenv.AddObj(hvoToDisplay, this, fragId);
				}
				//ProcessChildren(nodeToProcess, vwenv, hvoToDisplay, null);
				CloseOuterParts(outerParts, vwenv);
			}
			finally
			{
				// reset the ws for next column in the row.
				WsForce = 0;
			}
		}

		private bool IsWritingSystemRTL(XElement node)
		{
			var sWs = XmlViewsUtils.FindWsParam(node);
			if (sWs == "reversal")
			{
				return IsWsRTL(ReversalWs);
			}
			return !string.IsNullOrEmpty(sWs) && IsWsRTL(XmlViewsUtils.GetWsFromString(sWs, m_cache));
		}

		/// <summary>
		/// Determines whether [is ws RTL] [the specified ws].
		/// </summary>
		protected bool IsWsRTL(int ws)
		{
			if (ws != 0)
			{
				var wsObj = m_cache.ServiceLocator.WritingSystemManager.Get(ws);
				return wsObj.RightToLeftScript;
			}
			return false;
		}

		internal virtual bool AllowEdit(int icol, XElement node, bool fIsCellActive, int hvo)
		{
			// Do we want to allow editing in the cell? Irrelevant if it isn't the current row.
			var fAllowEdit = false; // a default.
			// If a click copy column is active only that column is editable;
			// otherwise any column marked editable or marked available for transducing is editable,
			// unless it has a commitChanges (in that case, we only allow editing when it is a
			// click copy target...we may eventually change this, but it somewhat complex to catch
			// all the times it might need to be called, and sometimes doing it too often could
			// be a nuisance).
			if (OverrideAllowEditColumn > 0)
			{
				fAllowEdit = icol == OverrideAllowEditColumn + 1;
			}
			else
			{
				// If commitChanges isn't null, we can't edit
				if (XmlUtils.GetOptionalAttributeValue(node, "commitChanges", null) != null)
				{
					fAllowEdit = false;
				}
				// If we're explicitly marked as being editable, we can edit
				else if (XmlUtils.GetOptionalBooleanAttributeValue(node, "editable", false))
				{
					fAllowEdit = true;
				}
				// If there's a transduce and we're not explicity marked as being uneditable, we can edit
				else if (XmlUtils.GetOptionalAttributeValue(node, "transduce") != null && XmlUtils.GetOptionalBooleanAttributeValue(node, "editable", true))
				{
					fAllowEdit = true;
					var sProperty = XmlUtils.GetOptionalAttributeValue(node, "editif");
					if (!string.IsNullOrEmpty(sProperty))
					{
						var co = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
						var fNot = false;
						if (sProperty[0] == '!')
						{
							fNot = true;
							sProperty = sProperty.Substring(1);
						}
						var mi = co.GetType().GetMethod(sProperty);
						if (mi != null)
						{
							var sWs = StringServices.GetWsSpecWithoutPrefix(XmlUtils.GetOptionalAttributeValue(node, "ws"));
							if (sWs != null)
							{
								var ws = sWs == "reversal" ? ReversalWs : XmlViewsUtils.GetWsFromString(sWs, m_cache);
								if (ws != 0)
								{
									var o = mi.Invoke(co, new object[] { ws });
									if (o.GetType() == typeof(bool))
									{
										fAllowEdit = fNot ? !(bool)o : (bool)o;
									}
								}
							}
						}
					}
				}
			}

			// But whatever else is going on, the user can't edit in a cell that is displaying a preview.
			if (fIsCellActive)
			{
				fAllowEdit = false;
			}
			// Or has an audio writing system
			if (fAllowEdit && NodeBestWsIsVoice(node, hvo))
			{
				fAllowEdit = false;
			}
			return fAllowEdit;
		}

		private int GetBestWsForNode(XElement node, int hvo)
		{
			// look for a writing system ID.
			var xa = node.Attribute("ws");
			var xn = node;
			if (xa == null)
			{
				xn = XmlUtils.FindElement(node, "string");
			}
			var wsBest = 0;
			if (xn == null)
			{
				return wsBest;
			}
			var hvoBest = 0;
			var flidBest = 0;
			var fNeedObject = XmlViewsUtils.GetWsRequiresObject(xn);
			if (fNeedObject)
			{
				hvoBest = GetBestHvoAndFlid(hvo, xn, out flidBest);
			}
			if (StringServices.GetWsSpecWithoutPrefix(XmlUtils.GetOptionalAttributeValue(node, "ws")) == "reversal")
			{
				// This can happen, for instance, in the Merge Entry dialog's browse view.
				// See FWR-2807.
				if (ReversalWs == 0)
				{
					var repo = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>();
					if (repo.IsValidObjectId(hvo))
					{
						var obj = repo.GetObject(hvo);
						if (obj is IReversalIndexEntry)
						{
							var sWsRev = (obj as IReversalIndexEntry).ReversalIndex.WritingSystem;
							ReversalWs = m_cache.ServiceLocator.WritingSystemManager.GetWsFromStr(sWsRev);
						}
					}
				}
				wsBest = ReversalWs;
			}
			else if (fNeedObject && hvoBest == 0)
			{
				wsBest = m_cache.DefaultAnalWs; // we may need the object, but we don't have one!
			}
			else
			{
				wsBest = WritingSystemServices.GetWritingSystem(
					m_cache, m_sda, FwUtils.ConvertElement(xn), null, hvoBest, flidBest,
					m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle).Handle;
			}
			return wsBest;
		}

		private bool NodeBestWsIsVoice(XElement node, int hvo)
		{
			// Figure out if this column's writing system is audio.
			var fVoice = false;
			// Enhance GJM: Can we just test m_wsBest here?
			var bestWsHandle = GetBestWsForNode(node, hvo);
			if (bestWsHandle == 0)
			{
				return false;
			}
			var ws = m_cache.ServiceLocator.WritingSystemManager.Get(bestWsHandle);
			if (ws != null)
			{
				fVoice = ws.IsVoice;
			}
			return fVoice;
		}

		/// <summary>
		/// Given the "column" element that describes a column of a browse view,
		/// come up with the XElement whose children will be the actual parts (or part refs)
		/// to use to display the contents.
		/// </summary>
		public static XElement GetColumnNode(XElement column, int hvo, ISilDataAccess sda, LayoutCache layouts)
		{
			var nodeToProcess = column;
			var layoutName = XmlUtils.GetOptionalAttributeValue(column, "layout");
			if (layoutName != null)
			{
				// new approach: display the object using the specified layout.
				var nodeInner = GetNodeForPart(hvo, layoutName, true, sda, layouts);
				if (nodeInner != null)
				{
					nodeToProcess = nodeInner;
				}
			}
			return nodeToProcess;
		}

		/// <summary>
		/// This is the main interesting method of displaying objects and fragments of them. Most
		/// subclasses should override.
		/// </summary>
		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			switch (frag)
			{
				case kfragRoot:
					// assume the root object has been loaded (FWR-3171: and not deleted).
					if (hvo > 0)
					{
						var obj = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
						if (obj is IReversalIndex)
						{
							ReversalWs = m_cache.ServiceLocator.WritingSystemManager.GetWsFromStr((obj as IReversalIndex).WritingSystem);
						}
						vwenv.AddLazyVecItems(m_madeUpFieldIdentifier, this, kfragListItem);
					}
					break;
				case kfragListItemInner:
					AddTableRow(vwenv, hvo, frag);
					break;
				case kfragListItem:
					vwenv.AddObjProp(TagMe, this, kfragListItemInner);
					break;
				default:
					base.Display(vwenv, hvo, frag);
					break;
			}
		}

		/// <summary>
		/// Select an appropriate picture from the list for this fragment.
		/// </summary>
		public override IPicture DisplayPicture(IVwEnv vwenv, int hvo, int tag, int val,
			int frag)
		{
			if (frag == kfragCheck)
			{
				return val == 0 ? m_UncheckedCheckPic : m_CheckedCheckPic;
			}
			return base.DisplayPicture(vwenv, hvo, tag, val, frag);
		}

		/// <summary>
		/// Called to ensure data is loaded for a lazily-displayed object we are about to use.
		/// </summary>
		public override void LoadDataFor(IVwEnv vwenv, int[] rghvo, int chvo, int hvoParent, int tag, int frag, int ihvoMin)
		{
			// All data is always in the SDA 'cache'.
		}

		/// <summary>
		/// Allows view browser to let us know we're doing an OnPaint.
		/// That's the only time we really want to request to convert dummy objects to real ones.
		/// </summary>
		public bool InOnPaint { get; set; }

		/// <summary>
		/// True if the preview is for multiple columns
		/// </summary>
		public bool MultiColumnPreview { get; set; }

		/// <summary>
		/// This routine is used to estimate the height of an item in points. The item will be
		/// one of those you have added to the environment using AddLazyItems. Note that the
		/// calling code does NOT ensure that data for displaying the item in question has been
		/// loaded. The first three arguments are as for Display, that is, you are being asked
		/// to estimate how much vertical space is needed to display this item in the available
		/// width.
		/// </summary>
		public override int EstimateHeight(int hvo, int frag, int dxpAvailWidth)
		{
			return 17; // Assume a browse view line is about 17 points high.
		}

		/// <summary>
		/// Display nothing when we encounter an ORC.
		/// </summary>
		public override void DisplayEmbeddedObject(IVwEnv vwenv, int hvo)
		{
		}

		/// <summary>
		/// We may have a link embedded in text displayed in a cell.
		/// </summary>
		public override void DoHotLinkAction(string strData, ISilDataAccess sda)
		{
			if (strData.Length > 0 && strData[0] == (int)FwObjDataTypes.kodtExternalPathName)
			{
				var url = strData.Substring(1); // may also be just a file name, launches default app.
				try
				{
					if (url.StartsWith(FwLinkArgs.kFwUrlPrefix))
					{
						var commands = new List<string>
						{
							"AboutToFollowLink",
							"FollowLink"
						};
						var parms = new List<object>
						{
							null,
							new FwLinkArgs(url)
						};
						m_xbv.Publisher.Publish(commands, parms);
						return;
					}
				}
				catch
				{
					// REVIEW: Why are we catching all errors?
				}
			}
			base.DoHotLinkAction(strData, sda);
		}

		#endregion Other methods

		/// <summary>
		/// Remove any columns that have become invalid, presumably due to deleting a custom
		/// field.  See FWR-1279.
		/// </summary>
		internal bool RemoveInvalidColumns()
		{
			var invalidColumns = m_columns.Where(column => !IsValidColumnSpec(column)).ToList();
			foreach (var invalidColumn in invalidColumns)
			{
				m_columns.Remove(invalidColumn);
			}
			return invalidColumns.Any();
		}
	}
}