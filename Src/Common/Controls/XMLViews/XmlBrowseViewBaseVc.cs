// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.
// <copyright from='2005' to='2005' company='SIL International'>
//		Copyright (c) 2005, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: XmlBrowseViewBaseVc.cs
// Responsibility: Randy Regnier
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Text.RegularExpressions;
using System.Xml;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Reflection; // for check-box icons.
using Palaso.WritingSystems;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;
using SIL.FieldWorks.Filters;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Resources; // for check-box icons.
using SIL.FieldWorks.Common.RootSites;
using SIL.CoreImpl;
using SIL.Utils.ComTypes;
using XCore;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for XmlBrowseViewBaseVc.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class XmlBrowseViewBaseVc : XmlVc
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
		protected List<XmlNode> m_columns = new List<XmlNode>();
		/// <summary>
		/// Specs of columns that COULD be displayed, but which have not been selected.
		/// </summary>
		protected List<XmlNode> m_possibleColumns;
		/// <summary>// Top-level fake property for list of objects.</summary>
		protected int m_fakeFlid = 0;
		/// <summary>// Controls appearance of column for check boxes.</summary>
		protected bool m_fShowSelected;
		/// <summary></summary>
		protected IPicture m_UncheckedCheckPic;
		/// <summary></summary>
		protected IPicture m_CheckedCheckPic;
		/// <summary></summary>
		protected IPicture m_DisabledCheckPic;
		/// <summary>Width in millipoints of the check box.</summary>
		protected int m_dxmpCheckWidth;
		/// <summary>Roughly 1-pixel border.</summary>
		protected int m_dxmpCheckBorderWidth = 72000 / 96;
		/// <summary></summary>
		protected XmlBrowseViewBase m_xbv;
		/// <summary></summary>
		protected ISortItemProvider m_sortItemProvider;
		IPicture m_PreviewArrowPic;
		IPicture m_PreviewRTLArrowPic;
		int m_icolOverrideAllowEdit = -1; // index of column to force allow editing in.
		bool m_fShowEnabled;
		bool m_fInOnPaint;
		// A writing system we wish to force to be used when a <string> element displays a multilingual property.
		// Todo JohnT: Get rid of this!! It is set in a <column> element and used within the display of nested
		// objects. This can produce unexpected results when the object property changes, because the inner
		// Dispay() call is not within the outer one that normally sets m_wsForce.
		// m_wsForce was introduced in place of m_columnNode as part of the process of getting rid of it.
		private int m_wsForce;
		// The writing system calculated by looking at the hvo and flid, as well as the ws attribute.
		private int m_wsBest;
		private int m_tagMe = XMLViewsDataCache.ktagTagMe;

		/// <summary>
		/// Set this variable true for displaying columns in reverse order.  This may also affect
		/// how the TAB key works.
		/// </summary>
		protected bool m_fShowColumnsRTL;

		private static bool s_haveShownDefaultColumnMessage;
		#endregion Data Members

		#region Construction and initialization

		internal string ColListId
		{
			get
			{
				string Id1 = m_xbv.Mediator.PropertyTable.GetStringProperty("currentContentControl", "");
				string Id2 = m_xbv.GetCorrespondingPropertyName("ColumnList");
				return String.Format("{0}_{1}", Id1, Id2);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether [show enabled].
		/// </summary>
		/// <value><c>true</c> if [show enabled]; otherwise, <c>false</c>.</value>
		/// ------------------------------------------------------------------------------------
		public bool ShowEnabled
		{
			get
			{
				return m_fShowEnabled;
			}
			set
			{
				m_fShowEnabled = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether [show columns RTL].
		/// </summary>
		/// <value><c>true</c> if [show columns RTL]; otherwise, <c>false</c>.</value>
		/// ------------------------------------------------------------------------------------
		public bool ShowColumnsRTL
		{
			get
			{
				return m_fShowColumnsRTL;
			}
		}

		/// <summary>
		/// This contructor is used by SortMethodFinder to make a braindead VC.
		/// </summary>
		public XmlBrowseViewBaseVc() : base(null) // We don't have a string table.
		{
			m_fakeFlid = 0;
		}

		/// <summary>
		/// This contructor is used by SortMethodFinder to make a partly braindead VC.
		/// </summary>
		public XmlBrowseViewBaseVc(XmlBrowseViewBase xbv, StringTable table)
			: base(table)
		{

			Debug.Assert(xbv.Mediator.FeedbackInfoProvider != null);

			MApp = xbv.Mediator.FeedbackInfoProvider as IApp;
			XmlBrowseViewBaseVcInit(xbv.Cache, xbv.DataAccess, table);

		}

		/// <summary>
		/// This contructor is used by FilterBar and LayoutCache to make a badly braindead VC for special, temporary use.
		/// It will fail if asked to interpret decorator properties, since it doesn't have the decorator SDA.
		/// Avoid using this constructor if possible.
		/// </summary>
		public XmlBrowseViewBaseVc(FdoCache cache, StringTable table)
			: base(table)
		{
			XmlBrowseViewBaseVcInit(cache, null, table);
		}
		/// <summary>
		/// This contructor is used by FilterBar and LayoutCache to make a partly braindead VC.
		/// </summary>
		public XmlBrowseViewBaseVc(FdoCache cache, ISilDataAccess sda, StringTable table)
			: base(table)
		{
			XmlBrowseViewBaseVcInit(cache, sda, table);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:XmlBrowseViewBaseVc"/> class.
		/// </summary>
		/// <param name="xnSpec">The xn spec.</param>
		/// <param name="fakeFlid">The fake flid.</param>
		/// <param name="stringTable">The string table.</param>
		/// <param name="xbv">The XBV.</param>
		/// ------------------------------------------------------------------------------------
		public XmlBrowseViewBaseVc(XmlNode xnSpec, int fakeFlid, StringTable stringTable, XmlBrowseViewBase xbv)
			: this(xbv, stringTable)
		{
			Debug.Assert(xnSpec != null);
			Debug.Assert(stringTable != null);
			Debug.Assert(xbv != null);
			DataAccess = xbv.SpecialCache;

			m_xbv = xbv;
			m_xnSpec = xnSpec;

			// This column list is saved in BrowseViewer.UpdateColumnList
			string savedCols = m_xbv.Mediator.PropertyTable.GetStringProperty(ColListId, null, XCore.PropertyTable.SettingsGroup.LocalSettings);
			SortItemProvider = xbv.SortItemProvider;
			ComputePossibleColumns();
			XmlDocument doc = null;
			string target = null;
			if (savedCols != null && savedCols != "")
			{
				doc = GetSavedColumns(savedCols, m_xbv.Mediator, ColListId);
			}
			if (doc == null) // nothing saved, or saved info won't parse
			{
				// default: the columns that have 'width' specified.
				foreach(XmlNode node in m_possibleColumns)
				{
					if (XmlUtils.GetOptionalAttributeValue(node, "visibility", "always") == "always")
						m_columns.Add(node);
				}
			}
			else
			{
				foreach (XmlNode node in doc.DocumentElement.SelectNodes("//column"))
				{
					if (IsValidColumnSpec(node))
					m_columns.Add(node);
			}
			}
			m_fakeFlid = fakeFlid;
			SetupSelectColumn();
		}

		internal static XmlDocument GetSavedColumns(string savedCols, Mediator mediator, string colListId)
		{
			XmlDocument doc;
			string target;
			try
			{
				doc = new XmlDocument();
				doc.LoadXml(savedCols);
				int version = XmlUtils.GetOptionalIntegerValue(doc.DocumentElement, "version", 0);
				if (version != BrowseViewer.kBrowseViewVersion)
				{
					// If we can fix problems introduced by a new version, fix them here.
					// Otherwise throw up a dialog telling the user they are losing their settings.
					switch (version)
					{
							// Process changes made in P4 Changelist 29278
						case 12:
							target = "<column label=\"Headword\" sortmethod=\"FullSortKey\" ws=\"$ws=vernacular\" editable=\"false\" width=\"96000\"><span><properties><editable value=\"false\" /></properties><string field=\"MLHeadWord\" ws=\"vernacular\" /></span></column>";
							if (savedCols.IndexOf(target) > -1)
								savedCols = savedCols.Replace(target, "<column label=\"Headword\" sortmethod=\"FullSortKey\" ws=\"$ws=vernacular\" editable=\"false\" width=\"96000\" layout=\"EntryHeadwordForFindEntry\" />");
							target = "<column label=\"Lexeme Form\" visibility=\"menu\" common=\"true\" sortmethod=\"MorphSortKey\" ws=\"$ws=vernacular\" editable=\"false\"><span><properties><editable value=\"false\"/></properties><obj field=\"LexemeForm\" layout=\"empty\"><string field=\"Form\" ws=\"$ws=vernacular\"/></obj></span></column>";
							if (savedCols.IndexOf(target) > -1)
								savedCols = savedCols.Replace(target, "<column label=\"Lexeme Form\" visibility=\"menu\" common=\"true\" sortmethod=\"MorphSortKey\" ws=\"$ws=vernacular\" editable=\"false\" layout=\"LexemeFormForFindEntry\"/>");
							target = "<column label=\"Citation Form\" visibility=\"menu\" sortmethod=\"CitationFormSortKey\" ws=\"$ws=vernacular\" editable=\"false\"><span><properties><editable value=\"false\"/></properties><string field=\"CitationForm\" ws=\"$ws=vernacular\"/></span></column>";
							if (savedCols.IndexOf(target) > -1)
								savedCols = savedCols.Replace(target, "<column label=\"Citation Form\" visibility=\"menu\" sortmethod=\"CitationFormSortKey\" ws=\"$ws=vernacular\" editable=\"false\" layout=\"CitationFormForFindEntry\"/>");
							target = "<column label=\"Allomorphs\" editable=\"false\" width=\"96000\"><span><properties><editable value=\"false\"/></properties><seq field=\"AlternateForms\" layout=\"empty\" sep=\", \"><string field=\"Form\" ws=\"$ws=vernacular\"/></seq></span></column>";
							if (savedCols.IndexOf(target) > -1)
								savedCols = savedCols.Replace(target, "<column label=\"Allomorphs\" editable=\"false\" width=\"96000\" ws=\"$ws=vernacular\" layout=\"AllomorphsForFindEntry\"/>");
							target = "<column label=\"Glosses\" multipara=\"true\" editable=\"false\" width=\"96000\"><seq field=\"Senses\" layout=\"empty\"><para><properties><editable value=\"false\"/></properties><string field=\"Gloss\" ws=\"$ws=analysis\"/></para></seq></column>";
							if (savedCols.IndexOf(target) > -1)
								savedCols = savedCols.Replace(target, "<column label=\"Glosses\" editable=\"false\" width=\"96000\" ws=\"$ws=analysis\" layout=\"GlossesForFindEntry\"/>");
							savedCols = RemoveWeatherColumn(savedCols);
							savedCols = FixVersion15Columns(savedCols);
							savedCols = savedCols.Replace("root version=\"12\"", "root version=\"15\"");
							mediator.PropertyTable.SetProperty(colListId, savedCols);
							doc.LoadXml(savedCols);
							break;
						case 13:
							savedCols = RemoveWeatherColumn(savedCols);
							savedCols = FixVersion15Columns(savedCols);
							savedCols = savedCols.Replace("root version=\"13\"", "root version=\"15\"");
							mediator.PropertyTable.SetProperty(colListId, savedCols);
							doc.LoadXml(savedCols);
							break;
						case 14:
							savedCols = FixVersion15Columns(savedCols);
							mediator.PropertyTable.SetProperty(colListId, savedCols);
							savedCols = savedCols.Replace("root version=\"14\"", "root version=\"15\"");
							doc.LoadXml(savedCols);
							break;
						default:
							if (!s_haveShownDefaultColumnMessage)
							{
								s_haveShownDefaultColumnMessage = true; // FWR-1781 only show this once
								MessageBox.Show(null, XMLViewsStrings.ksInvalidSavedLayout, XMLViewsStrings.ksNote);
							}
							doc = null;
							// Forget the old settings, so we don't keep complaining every time the program runs.
							// There doesn't seem to be any way to remove the property altogether, so at least, make it empty.
							mediator.PropertyTable.SetProperty(colListId, "", XCore.PropertyTable.SettingsGroup.LocalSettings);
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
				int index = match.Groups[1].Index;
				savedCols = savedCols.Substring(0, index) + replaceWith + savedCols.Substring(index + attrValue.Length);
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
			var close = customField ? "" : "\"";
			var pattern = new Regex("<column [^>]*layout *= *\"" + layoutName + close + "[^>]*/>");
			var match = pattern.Match(savedCols);
			if (match.Success)
			{
				int index = match.Index + match.Length - 2; // just before closing />
				savedCols = savedCols.Substring(0, index) + " " + attrName + "=\"" + attrValue + "\"" + savedCols.Substring(index);
			}
			return savedCols;
		}

		/// <summary>
		/// This contructor is used by SortMethodFinder to make a braindead VC.
		/// </summary>
		private void XmlBrowseViewBaseVcInit(FdoCache cache, ISilDataAccess sda, StringTable table)
		{
			Debug.Assert(cache != null);

			m_fakeFlid = 0;
			Cache = cache;	// sets m_mdc and m_layouts as well as m_cache.
			if (sda != null)
				DataAccess = sda;
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
			XmlAttribute xa = m_xnSpec.Attributes["selectColumn"];
			m_fShowSelected = xa != null && xa.Value == "true";
			if (m_fShowSelected)
			{
				// Only need these if showing the selected column.
				m_UncheckedCheckPic = m_app.PictureHolder.GetPicture("UncheckedCheckBox", ResourceHelper.UncheckedCheckBox);
				m_CheckedCheckPic = m_app.PictureHolder.GetPicture("CheckedCheckBox", ResourceHelper.CheckedCheckBox);
				m_DisabledCheckPic = m_app.PictureHolder.GetPicture("DisabledCheckBox", ResourceHelper.DisabledCheckBox);
				// We want a width in millipoints (72000/inch). Value we have is in 100/mm.
				// There are 25.4 mm/inch.
				m_dxmpCheckWidth = m_UncheckedCheckPic.Width * 72000 / 2540;
			}
		}

		/// <summary>
		/// Provide access to the special tag used to implement highlighting the selected row.
		/// </summary>
		internal int TagMe
		{
			get
			{
				return m_tagMe;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// compute and return possible columns. This always recomputes them, so may pick
		/// up generated columns for new custom fields.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public List<XmlNode> ComputePossibleColumns()
		{
			XmlVc vc = null;
			if (ListItemsClass != 0)
				vc = this;
			m_possibleColumns = PartGenerator.GetGeneratedChildren(m_xnSpec.SelectSingleNode("columns"),
						 m_xbv.Cache, vc, (int)ListItemsClass);
			return m_possibleColumns;
		}

		int m_listItemsClass = 0;
		/// <summary>
		/// the class (or base class) of the items in SortItemProvider
		/// </summary>
		internal int ListItemsClass
		{
			get
			{
				if (m_listItemsClass == 0)
				{
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
						string listItemsClass = XmlUtils.GetManditoryAttributeValue(m_xnSpec, "listItemsClass");
						m_listItemsClass = m_cache.MetaDataCacheAccessor.GetClassId(listItemsClass);

					}
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
		/// <param name="node"></param>
		/// <returns></returns>
		internal bool IsValidColumnSpec(XmlNode node)
		{
			List<XmlNode> possibleColumns = this.PossibleColumnSpecs;
			// first, check to see if we can find some part or child node information
			// to process. Eg. Custom field column nodes that refer to parts that no longer exist
			// because the custom field has been removed so the parts cannot be generated
			XmlNode partNode = this.GetPartFromParentNode(node, this.ListItemsClass);
			if (partNode == null)
				return false;	// invalid node, don't add.
			bool badCustomField = CheckForBadCustomField(possibleColumns, node);
			if (badCustomField)
				return false;	// invalid custom field, don't add.
			bool badReversalIndex = CheckForBadReversalIndex(possibleColumns, node);
			if (badReversalIndex)
				return false;
			return true;	// valid as far as we can tell.
		}

		/// <summary>
		/// Check for a nonexistent custom field.  (Custom fields can be deleted.)  As a side-effect,
		/// if the node refers to a valid custom field, the label attribute is adjusted to what we
		/// want the user to see.
		/// </summary>
		/// <param name="possibleColumns"></param>
		/// <param name="node"></param>
		/// <returns>true if this node refers to a nonexistent custom field</returns>
		private bool CheckForBadCustomField(List<XmlNode> possibleColumns, XmlNode node)
		{
			StringTable stringTbl = this.StringTbl;
			// see if this node is based on a layout. If so, get its part
			PropWs propWs;
			XmlNode columnForCustomField = null;
			if (this.TryColumnForCustomField(node, this.ListItemsClass, out columnForCustomField, out propWs))
			{
				if (columnForCustomField != null)
				{
					string fieldName = XmlUtils.GetAttributeValue(columnForCustomField, "field");
					string className = XmlUtils.GetAttributeValue(columnForCustomField, "class");
					if (!String.IsNullOrEmpty(fieldName) && !String.IsNullOrEmpty(className))
					{
						if ((m_mdc as IFwMetaDataCacheManaged).FieldExists(className, fieldName, false))
						{
							ColumnConfigureDialog.GenerateColumnLabel(node, m_cache, stringTbl);
							return false;
						}
					}
					return true;
				}
				else if (propWs != null)
				{
					XmlUtils.AppendAttribute(node, "originalLabel", GetNewLabelFromMatchingCustomField(possibleColumns, propWs.flid));
					ColumnConfigureDialog.GenerateColumnLabel(node, m_cache, stringTbl);
				}
				else
				{
					// it's an invalid custom field.
					return true;
				}
			}
			return false;
		}



		private string GetNewLabelFromMatchingCustomField(List<XmlNode> possibleColumns, int flid)
		{
			StringTable tbl = this.StringTbl;
			foreach (XmlNode possibleColumn in possibleColumns)
			{
				// Desired node may be a child of a child...  (See LT-6447.)
				PropWs propWs;
				XmlNode columnForCustomField = null;
				if (this.TryColumnForCustomField(possibleColumn, this.ListItemsClass, out columnForCustomField, out propWs))
				{
					// the flid of the updated custom field node matches the given flid of the old node.
					if (propWs != null && propWs.flid == flid)
					{
						string label = XmlUtils.GetLocalizedAttributeValue(tbl, possibleColumn,
								"label", null);
						return label;
					}
				}
			}
			return "";
		}

		/// <summary>
		/// Check for an invalid reversal index.  (Reversal indexes can be deleted.)
		/// </summary>
		/// <param name="possibleColumns"></param>
		/// <param name="node"></param>
		/// <returns>true if this node refers to a nonexistent reversal index.</returns>
		private bool CheckForBadReversalIndex(List<XmlNode> possibleColumns, XmlNode node)
		{
			// Look for a child node which is similar to this (value of ws attribute may differ):
			// <string field="ReversalEntriesText" ws="$ws=es"/>
			XmlNode child = XmlUtils.FindNode(node, "string");
			if (child != null &&
				XmlUtils.GetOptionalAttributeValue(child, "field") == "ReversalEntriesText")
			{
				string sWs = StringServices.GetWsSpecWithoutPrefix(child);
				if (sWs != null && sWs != "reversal")
				{
					if (!m_cache.ServiceLocator.WritingSystemManager.Exists(sWs))
						return true;	// invalid writing system
					// Check whether we have a reversal index for the given writing system.
					foreach (var idx in m_cache.LangProject.LexDbOA.ReversalIndexesOC)
					{
						if (idx.WritingSystem == sWs)
							return false;
					}
					return true;
				}
			}
			return false;
		}
		#endregion Construction and initialization

		#region Properties

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the override allow edit column.
		/// </summary>
		/// <value>The override allow edit column.</value>
		/// ------------------------------------------------------------------------------------
		public int OverrideAllowEditColumn
		{
			get
			{
				return m_icolOverrideAllowEdit;
			}
			set
			{
				m_icolOverrideAllowEdit = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance has select column.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance has select column; otherwise, <c>false</c>.
		/// </value>
		/// ------------------------------------------------------------------------------------
		public bool HasSelectColumn
		{
			get
			{
				return m_fShowSelected;
			}
		}

		/// <summary>
		/// Specifies the default state of check boxes
		/// </summary>
		public bool DefaultChecked
		{
			get
			{
				if (m_sda is XMLViewsDataCache)
					return (m_sda as XMLViewsDataCache).DefaultSelected;
				// pretty arbitrary, probably something else will crash if using this without
				// the proper kind of cache, but it makes more sense than the other, since it
				// is our general default.
				return true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the width of the select column.
		/// </summary>
		/// <value>The width of the select column.</value>
		/// ------------------------------------------------------------------------------------
		public int SelectColumnWidth
		{
			get
			{
				return m_dxmpCheckWidth + 4 * m_dxmpCheckBorderWidth;
			}
		}

		internal virtual List<XmlNode> ColumnSpecs
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

		internal List<XmlNode> PossibleColumnSpecs
		{
			get
			{
				return m_possibleColumns;
			}
		}

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
				m_PreviewArrowPic = m_app.PictureHolder.GetPicture("PreviewArrow", value);
				Image x = value;
				x.RotateFlip(RotateFlipType.Rotate180FlipNone);
				m_PreviewRTLArrowPic = m_app.PictureHolder.GetPicture("PreviewRTLArrow", x);
			}
		}

		internal bool HasPreviewArrow
		{
			get { return m_PreviewArrowPic != null; }
		}

		#endregion Properties

		#region Other methods

		/// <summary>
		///  Convert a .NET color to the type understood by Views code and other Win32 stuff.
		/// </summary>
		/// <param name="c"></param>
		/// <returns></returns>
		static public uint RGB(System.Drawing.Color c)
		{
			return RGB(c.R, c.G, c.B);
		}

		/// <summary>
		/// Make a standard Win32 color from three components.
		/// </summary>
		/// <param name="r"></param>
		/// <param name="g"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		static public uint RGB(int r, int g, int b)
		{
			return ((uint)(((byte)(r)|((short)((byte)(g))<<8))|(((short)(byte)(b))<<16)));
		}

		/// <summary>Border color to use</summary>
		private System.Drawing.Color m_BorderColor = SystemColors.Control;	// System.Drawing.Color.LightGray;
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the color of the border.
		/// </summary>
		/// <value>The color of the border.</value>
		/// ------------------------------------------------------------------------------------
		public System.Drawing.Color BorderColor
		{
			get
			{
				return m_BorderColor;
			}
			set
			{
				m_BorderColor = value;
			}
		}

		const int kclrBackgroundSelRow = (int)0xFFE6D7;

		internal virtual int SelectedRowBackgroundColor(int hvo)
		{
			return kclrBackgroundSelRow;
		}
		/// <summary>
		/// Add a new table/row to the view for one existing object.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="hvo"></param>
		/// <param name="frag"></param>
		protected virtual void AddTableRow(IVwEnv vwenv, int hvo, int frag)
		{
			// set the border color
			vwenv.set_IntProperty((int )FwTextPropType.ktptBorderColor,
				(int)FwTextPropVar.ktpvDefault,
				(int)RGB(m_BorderColor));

			// If we're using this special mode where just one column is editable, we need to make
			// sure as far as possible that everything else is not.
			if (OverrideAllowEditColumn >= 0)
				vwenv.set_IntProperty((int)FwTextPropType.ktptEditable, (int) FwTextPropVar.ktpvEnum,
					(int)TptEditable.ktptNotEditable);

			int index, hvoDummy, tagDummy;
			int clev = vwenv.EmbeddingLevel;
			vwenv.GetOuterObject(clev - 2, out hvoDummy, out tagDummy, out index);
			if (index >= m_sda.get_VecSize(hvoDummy, tagDummy))
				return; // something to fix.

			if (index == m_xbv.SelectedIndex && m_xbv.SelectedRowHighlighting != XmlBrowseViewBase.SelectionHighlighting.none)
			{
				vwenv.set_IntProperty((int)FwTextPropType.ktptBackColor,
					(int)FwTextPropVar.ktpvDefault,
					//	(int)RGB(Color.FromKnownColor(KnownColor.Highlight)));
					SelectedRowBackgroundColor(hvo));
				if (m_xbv.SelectedRowHighlighting == XmlBrowseViewBase.SelectionHighlighting.border)
				{
					vwenv.set_IntProperty((int)FwTextPropType.ktptBorderTop,
						(int)FwTextPropVar.ktpvMilliPoint,
						3000);
					vwenv.set_IntProperty((int)FwTextPropType.ktptBorderColor,
						(int)FwTextPropVar.ktpvDefault,
						(int)RGB(Color.FromKnownColor(KnownColor.Highlight)));
				}
			}

			// Make a table.
			VwLength[] rglength = m_xbv.GetColWidthInfo();
			int colCount = m_columns.Count;
			if (m_fShowSelected)
				colCount++;

			// LT-7014, 7058: add the additional columns that are needed
			if (rglength.Length < colCount)
			{
				VwLength[] rglengthNEW = new VwLength[colCount];
				for (int ii = 0; ii < colCount; ii++)
				{
					if (ii < rglength.Length)
						rglengthNEW[ii] = rglength[ii];
					else
						rglengthNEW[ii] = rglength[0];
				}
				rglength = rglengthNEW;
			}

			// If the only columns specified are custom fields which have been deleted,
			// we can't show anything!  (and we don't want to crash -- see LT-6449)
			if (rglength.Length == 0)
				return;

			VwLength vl100; // Length representing 100% of the table width.
			vl100.unit = rglength[0].unit;
			vl100.nVal = 1;
			for (int i = 0; i < colCount; ++i)
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
			for (int i = 0; i < colCount; ++i)
				vwenv.MakeColumns(1, rglength[i]);
			// the table only has a body (no header or footer), and only one row.
			vwenv.OpenTableBody();
			vwenv.OpenTableRow();

			if (m_fShowSelected)
				AddSelectionCell(vwenv, hvo);
			// Make the cells.
			int hvoRoot, tagDummy2, ihvoDummy;
			vwenv.GetOuterObject(0, out hvoRoot, out tagDummy2, out ihvoDummy);
			int icolActive = GetActiveColumn(vwenv, hvoRoot);
			// if m_fShowSelected is true, we get an extra column of checkmarks that gives us
			// a different index into the columns. This allows the one-based indexing to work
			// in the call to ColumnSortedFromEnd. Without this, the one-based indexing has to
			// be adjusted to zero-based indexing.
			int cAdjCol = m_fShowSelected ? 0 : 1;
			if (m_fShowColumnsRTL)
			{
				for (int icol = m_columns.Count; icol > 0; --icol)
					AddTableCell(vwenv, hvo, index, hvoRoot, icolActive, cAdjCol, icol);
			}
			else
			{
				for (int icol = 1; icol <= m_columns.Count; ++icol)
					AddTableCell(vwenv, hvo, index, hvoRoot, icolActive, cAdjCol, icol);
			}
			vwenv.CloseTableRow();
			vwenv.CloseTableBody();
			vwenv.CloseTable();
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="hvoRoot"></param>
		/// <returns></returns>
		protected virtual int GetActiveColumn(IVwEnv vwenv, int hvoRoot)
		{
			int icolActive = -1;
			if (vwenv.DataAccess.get_IsPropInCache(hvoRoot, XMLViewsDataCache.ktagActiveColumn, (int)CellarPropertyType.Integer, 0))
				icolActive = vwenv.DataAccess.get_IntProp(hvoRoot, XMLViewsDataCache.ktagActiveColumn);
			return icolActive;
		}

		/// <summary>
		/// A writing system we wish to force all string elements to use for multilingual properties.
		/// We want to get rid of this; don't use it unless you must.
		/// </summary>
		internal override int WsForce
		{
			get { return m_wsForce; }
			set { m_wsForce = value; }
		}

		private void AddTableCell(IVwEnv vwenv, int hvo, int index, int hvoRoot, int icolActive, int cAdjCol, int icol)
		{
			XmlNode node = m_columns[icol - 1];
			// Figure out the underlying Right-To-Left value.
			bool fRightToLeft = false;
			// Figure out if this column's writing system is audio.
			bool fVoice = false;
			m_wsBest = GetBestWsForNode(node, hvo);
			if (m_wsBest != 0)
			{
				var ws = m_cache.ServiceLocator.WritingSystemManager.Get(m_wsBest);
				if (ws != null)
				{
					fRightToLeft = ws.RightToLeftScript;
					var wsDef = ws as WritingSystemDefinition;
					fVoice = wsDef == null ? false : wsDef.IsVoice;
				}
			}
			bool fSortedFromEnd = m_xbv.ColumnSortedFromEnd(icol - cAdjCol);
			int tal;
			if (fRightToLeft)
			{
				if (fSortedFromEnd)
					tal = (int)FwTextAlign.ktalLeft;
				else
					tal = (int)FwTextAlign.ktalRight;
			}
			else
			{
				if (fSortedFromEnd)
					tal = (int)FwTextAlign.ktalRight;
				else
					tal = (int)FwTextAlign.ktalLeft;
			}
			vwenv.set_IntProperty((int)FwTextPropType.ktptAlign,
				(int)FwTextPropVar.ktpvEnum, tal);

			// If this cell is audio, don't allow editing.
			if (fVoice)
				SetCellEditability(vwenv, false);

			bool fIsCellActive = false;
			if (icolActive != 0 && m_PreviewArrowPic != null)
			{
				// The display depends on which column is active and whether the current row is selected.
				vwenv.NoteDependency(new int[] { hvoRoot, hvo, hvo }, new int[] { XMLViewsDataCache.ktagActiveColumn, XMLViewsDataCache.ktagItemSelected, XMLViewsDataCache.ktagItemEnabled }, 3);
				// We're doing the active column thing.
				if (icol == icolActive)
				{
					fIsCellActive = vwenv.DataAccess.get_IntProp(hvo, XMLViewsDataCache.ktagItemSelected) != 0
						&& vwenv.DataAccess.get_IntProp(hvo, XMLViewsDataCache.ktagItemEnabled) != 0;
				}
			}
			// Make a cell and embed the contents of the column node.
			ProcessProperties(node, vwenv);
			SetCellProperties(index, icol, node, hvo, vwenv, fIsCellActive);
			vwenv.set_IntProperty((int)FwTextPropType.ktptPadTop,
				(int)FwTextPropVar.ktpvMilliPoint, 1607);
			vwenv.OpenTableCell(1, 1);
			vwenv.set_IntProperty((int)FwTextPropType.ktptPadTrailing,
				(int)FwTextPropVar.ktpvMilliPoint, 1607);
			vwenv.set_IntProperty((int)FwTextPropType.ktptPadLeading,
				(int)FwTextPropVar.ktpvMilliPoint, 1607);
			if (node.Name == "column")
			{
				// Paragraph directionality must be set before the paragraph is opened.
				vwenv.set_IntProperty((int)FwTextPropType.ktptRightToLeft,
					(int)FwTextPropVar.ktpvEnum, IsWritingSystemRTL(node) ? -1 : 0);
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

			vwenv.OpenParagraph();			// <Paragraph>
			bool multiPara = XmlUtils.GetOptionalBooleanAttributeValue(node, "multipara", false);
			vwenv.OpenInnerPile();			//		<InnerPile>
			// if the multi-para attribute is not specified, create a paragraph to wrap the cell contents.
			bool fParaOpened = false;
			if (!multiPara)
			{
				vwenv.OpenParagraph();		//			<paragraph>
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
				XmlNode nodeToProcess = GetColumnNode(node, hvo, m_sda, m_layouts);
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
				int level = vwenv.EmbeddingLevel;
				int hvoDum, tag, ihvo;
				vwenv.GetOuterObject(level - 2, out hvoDum, out tag, out ihvo);
				Debug.Assert(tag == m_fakeFlid);
				IManyOnePathSortItem item = m_sortItemProvider.SortItemAt(ihvo);
				if (item != null)
				DisplayCell(item, node, hvo, vwenv);	// (Original) cell contents
			}

			if (fParaOpened)
			{
				vwenv.CloseParagraph();		//			   (Original) cell contents </paragraph>
				fParaOpened = false;
			}
			vwenv.CloseInnerPile();			//		</InnerPile>
			if (fIsCellActive)
				AddPreviewPiles(vwenv, node);
			vwenv.CloseParagraph();			// </Paragraph>
			vwenv.CloseTableCell();
			m_wsBest = 0;
		}

		private int GetBestHvoAndFlid(int hvo, XmlNode xn, out int flidBest)
		{
			// The 'best' ws series needs the hvo and flid in order to work.
			// Some others (e.g., 'reversal' which require the hvo don't need a flid.
			int hvoBest = hvo; // Default assumption, a field of the target.
			flidBest = 0;
			string fieldname = XmlUtils.GetOptionalAttributeValue(xn, "field");
			if (fieldname != null)
			{
				var mdc = m_mdc as IFwMetaDataCacheManaged;
				int objectClid = m_sda.get_IntProp(hvo, CmObjectTags.kflidClass);
				if (xn.ParentNode != null && xn.ParentNode.Name == "obj")
				{
					// Special case: object property, string field is on a derived object.
					string fieldObj = XmlUtils.GetAttributeValue(xn.ParentNode, "field");
					if (mdc.FieldExists(objectClid, fieldObj, true))
					{
						var flidObj = m_mdc.GetFieldId2(objectClid, fieldObj, true);
						hvoBest = m_sda.get_ObjectProp(hvo, flidObj);
						if (hvoBest == 0)
							return 0; // target object does not exist
						objectClid = m_sda.get_IntProp(hvoBest, CmObjectTags.kflidClass); // interpret main fieldname relative to this object.
					}
				}
				if (mdc.FieldExists(objectClid, fieldname, true))
				{
					flidBest = m_mdc.GetFieldId2(objectClid, fieldname, true);
					return hvoBest;
				}
			}
			return 0; // can't find a useful combination.
		}

		/// <summary>
		/// Passed a column node, determines whether it indicates a forced writing system, and if so sets it.
		/// </summary>
		/// <param name="node"></param>
		private void SetForcedWs(XmlNode node)
		{
			string wsSpec = XmlUtils.GetOptionalAttributeValue(node, "ws");
			if (wsSpec != null)
			{
				string realWsSpec = StringServices.GetWsSpecWithoutPrefix(wsSpec);
				// If it's a magic wsId, we must NOT call GetWsFromStr, that is VERY slow
				// when what we pass isn't a valid WS name.
				var magicWsId = WritingSystemServices.GetMagicWsIdFromName(realWsSpec);
				if (magicWsId != 0)
					WsForce = magicWsId;
				else
				{
					WsForce = m_cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr(realWsSpec);
					if (WsForce == 0)
						WsForce = m_wsBest;
				}
			}
		}

		/// <summary>
		/// Set the properties of the table cell we are about to create.
		/// </summary>
		protected virtual void SetCellProperties(int rowIndex, int icol, XmlNode node, int hvo, IVwEnv vwenv, bool fIsCellActive)
		{
			// By default we only need to worry about editability for the active row.
			if (rowIndex == m_xbv.SelectedIndex)
				SetCellEditability(vwenv, AllowEdit(icol, node, fIsCellActive, hvo));
		}

		/// <summary>
		/// Set the actual editability and related appearance of the box (typically table cell) we will next create.
		/// </summary>
		protected void SetCellEditability(IVwEnv vwenv, bool fEditable)
		{
			if (fEditable)
			{
				vwenv.set_IntProperty((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum,
									  (int)TptEditable.ktptIsEditable);
				vwenv.set_IntProperty((int)FwTextPropType.ktptBackColor,
									  (int)FwTextPropVar.ktpvDefault,
									  (int)RGB(Color.FromKnownColor(KnownColor.Window)));
			}
			else
			{
				vwenv.set_IntProperty((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum,
									  (int)TptEditable.ktptNotEditable);
			}
		}

		private void AddPreviewPiles(IVwEnv vwenv, XmlNode node)
			{
			// add padding to separate PreviewArrow from other cell contents.
			vwenv.set_IntProperty((int)FwTextPropType.ktptPadTrailing,
								  (int)FwTextPropVar.ktpvMilliPoint, 2500);
			vwenv.set_IntProperty((int)FwTextPropType.ktptPadLeading,
								  (int)FwTextPropVar.ktpvMilliPoint, 2500);
			vwenv.OpenInnerPile();
			{
				AddPreviewArrow(vwenv, node);
			}
			vwenv.CloseInnerPile();
			vwenv.OpenInnerPile();
			{
				AddAlternateCellContents(vwenv);
			}
			vwenv.CloseInnerPile();
		}

		private void AddPreviewArrow(IVwEnv vwenv, XmlNode node)
		{
			vwenv.OpenParagraph();
			if (IsWritingSystemRTL(node))
				vwenv.AddPicture(m_PreviewRTLArrowPic, 0, 0, 0);
			else
				vwenv.AddPicture(m_PreviewArrowPic, 0, 0, 0);
			vwenv.CloseParagraph();
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="vwenv"></param>
		private void AddAlternateCellContents(IVwEnv vwenv)
		{
			vwenv.OpenParagraph();
			vwenv.AddStringProp(XMLViewsDataCache.ktagAlternateValue, this);
			vwenv.CloseParagraph();
		}

		private void AddSelectionCell(IVwEnv vwenv, int hvo)
		{
			vwenv.OpenTableCell(1, 1);

			// Put a pixel of light grey pad around the check box itself.
			vwenv.set_IntProperty((int)FwTextPropType.ktptBackColor,
				(int)FwTextPropVar.ktpvDefault,
				(int)RGB(System.Drawing.Color.LightGray));
			vwenv.set_IntProperty((int)FwTextPropType.ktptPadLeading,
				(int)FwTextPropVar.ktpvMilliPoint,
				(m_dxmpCheckBorderWidth));
			vwenv.set_IntProperty((int)FwTextPropType.ktptPadTrailing,
				(int)FwTextPropVar.ktpvMilliPoint,
				(m_dxmpCheckBorderWidth));
			vwenv.set_IntProperty((int)FwTextPropType.ktptPadTop,
				(int)FwTextPropVar.ktpvMilliPoint,
				(m_dxmpCheckBorderWidth));
			vwenv.set_IntProperty((int)FwTextPropType.ktptPadBottom,
				(int)FwTextPropVar.ktpvMilliPoint,
				(m_dxmpCheckBorderWidth));
			if (ShowEnabled)
			{
				int enabled = vwenv.DataAccess.get_IntProp(hvo, XMLViewsDataCache.ktagItemEnabled);
				vwenv.NoteDependency(new int[] { hvo }, new int[] { XMLViewsDataCache.ktagItemEnabled }, 1);
				if (enabled != 0)
				{
					// Even if the view as a whole does not allow editing, presumably a check box
					// does!
					vwenv.set_IntProperty((int)FwTextPropType.ktptEditable,
						(int)FwTextPropVar.ktpvEnum,
						(int)TptEditable.ktptIsEditable);
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
				vwenv.set_IntProperty((int)FwTextPropType.ktptEditable,
					(int)FwTextPropVar.ktpvEnum,
					(int)TptEditable.ktptIsEditable);
				vwenv.AddIntPropPic(XMLViewsDataCache.ktagItemSelected, this, kfragCheck, 0, 1);
			}

			vwenv.CloseTableCell();
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is the core routine for XmlVc, which determines how to display the object indicated by hvo,
		/// using the XML 'fragment' given in frag, which may be affected by 'caller', the context (typically
		/// I think a part ref). This is called as part of Display to actually put the data into the vwenv.
		/// NOTE: a parallel routine, DetermineNeededFieldsFor, figures out the data that may be needed
		/// by ProcessFrag, and should be kept in sync with it. This special frag allows a preview to be explicitly inserted into a special-purpose column,
		/// such as one where we want it to be part of a concordance paragraph.
		/// </summary>
		/// <param name="frag"></param>
		/// <param name="vwenv"></param>
		/// <param name="hvo"></param>
		/// <param name="fEditable"></param>
		/// <param name="caller"></param>
		/// ------------------------------------------------------------------------------------
		public override void ProcessFrag(XmlNode frag, IVwEnv vwenv, int hvo, bool fEditable, XmlNode caller)
		{
			if (frag.Name == "preview")
			{
				vwenv.NoteDependency(new int[] { hvo, hvo }, new int[] { XMLViewsDataCache.ktagItemSelected, XMLViewsDataCache.ktagItemEnabled }, 2);
				// Explicitly insert the preview here, if enabled etc.
				if (vwenv.DataAccess.get_IntProp(hvo, XMLViewsDataCache.ktagItemSelected) != 0
										&& vwenv.DataAccess.get_IntProp(hvo, XMLViewsDataCache.ktagItemEnabled) != 0)
				{
					if (!HasPreviewArrow)
						PreviewArrow = BulkEditBar.PreviewArrowStatic;
					AddPreviewPiles(vwenv, frag);
				}
			}
			else
				base.ProcessFrag(frag, vwenv, hvo, fEditable, caller);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given a IManyOnePathSortItem that describes one of the rows in the view, and a node
		/// that configures one of the columns, make the required calls to the VwEnv to display the cell
		/// contents. This is also used by the LayoutFinder.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="node">The node.</param>
		/// <param name="hvo">The hvo.</param>
		/// <param name="vwenv">The vwenv.</param>
		/// ------------------------------------------------------------------------------------
		public void DisplayCell(IManyOnePathSortItem item, XmlNode node, int hvo, IVwEnv vwenv)
		{
			List<XmlNode> outerParts = new List<XmlNode>();
			int hvoToDisplay;
			NodeDisplayCommand dispCommand = XmlViewsUtils.GetDisplayCommandForColumn(item, node, m_mdc,
				m_sda, m_layouts, out hvoToDisplay, outerParts);
			// See if the column has a writing system established for it.
			try
			{
				if (node.Name == "column")
				{
					SetForcedWs(node);
				}
				OpenOuterParts(outerParts, vwenv, hvo);
				if (hvoToDisplay == hvo)
					dispCommand.PerformDisplay(this, 0, hvo, vwenv);
				else
				{
					int fragId = GetId(dispCommand, m_idToDisplayCommand, m_displayCommandToId);
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

		private bool IsWritingSystemRTL(XmlNode node)
		{
			string sWs = XmlViewsUtils.FindWsParam(node);
			if (sWs == "reversal")
				return IsWsRTL(m_wsReversal);
			else if (!String.IsNullOrEmpty(sWs))
				return IsWsRTL(XmlViewsUtils.GetWsFromString(sWs, m_cache));
			else
				return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether [is ws RTL] [the specified ws].
		/// </summary>
		/// <param name="ws">The ws.</param>
		/// <returns>
		/// 	<c>true</c> if [is ws RTL] [the specified ws]; otherwise, <c>false</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		protected bool IsWsRTL(int ws)
		{
			if (ws != 0)
			{
				IWritingSystem wsObj = m_cache.ServiceLocator.WritingSystemManager.Get(ws);
				return wsObj.RightToLeftScript;
			}
			return false;
		}

		internal virtual bool AllowEdit(int icol, XmlNode node, bool fIsCellActive, int hvo)
		{
			// Do we want to allow editing in the cell? Irrelevant if it isn't the current row.
			bool fAllowEdit = false; // a default.
			// If a click copy column is active only that column is editable;
			// otherwise any column marked editable or marked available for transducing is editable,
			// unless it has a commitChanges (in that case, we only allow editing when it is a
			// click copy target...we may eventually change this, but it somewhat complex to catch
			// all the times it might need to be called, and sometimes doing it too often could
			// be a nuisance).
			if (m_icolOverrideAllowEdit > 0)
				fAllowEdit = icol == m_icolOverrideAllowEdit + 1;
			else
			{
				// If commitChanges isn't null, we can't edit
				if (XmlUtils.GetOptionalAttributeValue(node, "commitChanges", null) != null)
					fAllowEdit = false;
				// If we're explicitly marked as being editable, we can edit
				else if (XmlUtils.GetOptionalBooleanAttributeValue(node, "editable", false))
					fAllowEdit = true;
				// If there's a transduce and we're not explicity marked as being uneditable, we can edit
				else if (XmlUtils.GetOptionalAttributeValue(node, "transduce") != null
					&& XmlUtils.GetOptionalBooleanAttributeValue(node, "editable", true))
				{
					fAllowEdit = true;
					string sProperty = XmlUtils.GetOptionalAttributeValue(node, "editif");
					if (!String.IsNullOrEmpty(sProperty))
					{
						var co = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
						bool fNot = false;
						if (sProperty[0] == '!')
						{
							fNot = true;
							sProperty = sProperty.Substring(1);
						}
						MethodInfo mi = co.GetType().GetMethod(sProperty);
						if (mi != null)
						{
							string sWs = StringServices.GetWsSpecWithoutPrefix(node);
							if (sWs != null)
							{
								int ws = (sWs == "reversal") ? m_wsReversal : XmlViewsUtils.GetWsFromString(sWs, m_cache);
								if (ws != 0)
								{
									object o = mi.Invoke(co, new object[] { ws });
									if (o.GetType() == typeof(bool))
										fAllowEdit = fNot ? !(bool)o : (bool)o;
								}
							}
						}
					}
				}
			}

			// But whatever else is going on, the user can't edit in a cell that is displaying a preview.
			if (fIsCellActive)
				fAllowEdit = false;
			// Or has an audio writing system
			if (fAllowEdit && NodeBestWsIsVoice(node, hvo))
				fAllowEdit = false;
			return fAllowEdit;
		}

		private int GetBestWsForNode(XmlNode node, int hvo)
		{
			// look for a writing system ID.
			XmlAttribute xa = node.Attributes["ws"];
			XmlNode xn = node;
			if (xa == null)
				xn = XmlUtils.FindNode(node, "string");
			var wsBest = 0;
			if (xn != null)
			{
				int hvoBest = 0;
				int flidBest = 0;
				bool fNeedObject = XmlViewsUtils.GetWsRequiresObject(xn);
				if (fNeedObject)
				{
					hvoBest = GetBestHvoAndFlid(hvo, xn, out flidBest);
				}
				if (StringServices.GetWsSpecWithoutPrefix(node) == "reversal")
				{
					// This can happen, for instance, in the Merge Entry dialog's browse view.
					// See FWR-2807.
					if (m_wsReversal == 0)
					{
						var repo = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>();
						if (repo.IsValidObjectId(hvo))
						{
							var obj = repo.GetObject(hvo);
							if (obj is IReversalIndexEntry)
							{
								string sWsRev = (obj as IReversalIndexEntry).ReversalIndex.WritingSystem;
								m_wsReversal = m_cache.ServiceLocator.WritingSystemManager.GetWsFromStr(sWsRev);
							}
						}
					}
					wsBest = m_wsReversal;
				}
				else if (fNeedObject && hvoBest == 0)
					wsBest = m_cache.DefaultAnalWs; // we may need the object, but we don't have one!
				else
					wsBest = WritingSystemServices.GetWritingSystem(
						m_cache, m_sda, xn, null, hvoBest, flidBest,
						m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle).Handle;
			}
			return wsBest;
		}

		private bool NodeBestWsIsVoice(XmlNode node, int hvo)
		{
			// Figure out if this column's writing system is audio.
			var fVoice = false;
			// Enhance GJM: Can we just test m_wsBest here?
			var bestWsHandle = GetBestWsForNode(node, hvo);
			if (bestWsHandle != 0)
			{
				var ws = m_cache.ServiceLocator.WritingSystemManager.Get(bestWsHandle);
				if (ws != null)
				{
					var wsDef = ws as WritingSystemDefinition;
					fVoice = wsDef == null ? false : wsDef.IsVoice;
				}
			}
			return fVoice;
		}

		/// <summary>
		/// Given the "column" element that describes a column of a browse view,
		/// come up with the XmlNode whose children will be the actual parts (or part refs)
		/// to use to display the contents.
		/// </summary>
		/// <param name="column"></param>
		/// <param name="hvo"></param>
		/// <param name="sda"></param>
		/// <param name="layouts"></param>
		/// <returns></returns>
		public static XmlNode GetColumnNode(XmlNode column, int hvo, ISilDataAccess sda, LayoutCache layouts)
		{
			XmlNode nodeToProcess = column;
			string layoutName = XmlUtils.GetOptionalAttributeValue(column, "layout");
			if (layoutName != null)
			{
				// new approach: display the object using the specified layout.
				XmlNode nodeInner = GetNodeForPart(hvo, layoutName, true, sda,
					layouts);
				if (nodeInner != null)
					nodeToProcess = nodeInner;
			}
			return nodeToProcess;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is the main interesting method of displaying objects and fragments of them. Most
		/// subclasses should override.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="hvo"></param>
		/// <param name="frag"></param>
		/// ------------------------------------------------------------------------------------
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
							m_wsReversal = m_cache.ServiceLocator.WritingSystemManager.GetWsFromStr((obj as IReversalIndex).WritingSystem);
						vwenv.AddLazyVecItems(m_fakeFlid, this, kfragListItem);
					}
					break;
				case kfragListItemInner:
					AddTableRow(vwenv, hvo, frag);
					break;
				case kfragListItem:
					vwenv.AddObjProp(m_tagMe, this, kfragListItemInner);
					break;
				default:
					base.Display(vwenv, hvo, frag);
					break;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Select an appropriate picture from the list for this fragment.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="val"></param>
		/// <param name="frag"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override IPicture DisplayPicture(IVwEnv vwenv, int hvo, int tag, int val,
			int frag)
		{
			if (frag == kfragCheck)
			{
				if (val == 0)
					return m_UncheckedCheckPic;
				else
					return m_CheckedCheckPic;
			}
			else
				return base.DisplayPicture(vwenv, hvo, tag, val, frag);
		}
		/// <summary>
		/// Called to ensure data is loaded for a lazily-displayed object we are about to use.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="rghvo"></param>
		/// <param name="chvo"></param>
		/// <param name="hvoParent"></param>
		/// <param name="tag"></param>
		/// <param name="frag"></param>
		/// <param name="ihvoMin"></param>
		public override void LoadDataFor(IVwEnv vwenv, int[] rghvo, int chvo, int hvoParent,
			int tag, int frag, int ihvoMin)
		{
			// All data is always in the SDA 'cache'.
		}

		/// <summary>
		/// Allows view browser to let us know we're doing an OnPaint.
		/// That's the only time we really want to request to convert dummy objects to real ones.
		/// </summary>
		public bool InOnPaint
		{
			get
			{
				return m_fInOnPaint;
			}
			set
			{
				m_fInOnPaint = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This routine is used to estimate the height of an item in points. The item will be
		/// one of those you have added to the environment using AddLazyItems. Note that the
		/// calling code does NOT ensure that data for displaying the item in question has been
		/// loaded. The first three arguments are as for Display, that is, you are being asked
		/// to estimate how much vertical space is needed to display this item in the available
		/// width.
		/// </summary>
		/// <param name="hvo">id of object for which estimate is to be done</param>
		/// <param name="frag">fragment of data</param>
		/// <param name="dxpAvailWidth">Width of data layout area in points</param>
		/// ------------------------------------------------------------------------------------
		public override int EstimateHeight(int hvo, int frag, int dxpAvailWidth)
		{
			return 17; // Assume a browse view line is about 17 points high.
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Display nothing when we encounter an ORC.
		/// </summary>
		/// -----------------------------------------------------------------------------------
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
				string url = strData.Substring(1); // may also be just a file name, launches default app.
				try
				{
					if (url.StartsWith(FwLinkArgs.kFwUrlPrefix))
					{
						m_xbv.Mediator.SendMessage("FollowLink", new FwLinkArgs(url));
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

		#region ItemsCollectorEnv
		/// <summary>
		/// Use this class to collect the objects displayed in a cell.
		/// By default, we'll assume the objects being displayed in the cell are of the same
		/// basic class.
		/// </summary>
		public class ItemsCollectorEnv : CollectorEnv
		{
#pragma warning disable 414
			FdoCache m_cache;
#pragma warning restore 414
			Set<int> m_hvosInCell = new Set<int>();

			/// <summary>
			///
			/// </summary>
			/// <param name="env"></param>
			/// <param name="cache"></param>
			/// <param name="hvoRoot"></param>
			public ItemsCollectorEnv(IVwEnv env, FdoCache cache, int hvoRoot)
				: base(env, cache.MainCacheAccessor, hvoRoot)
			{
				m_cache = cache;
			}

			/// <summary>
			/// This constructor should be used if you want to provide a seperate cache decorator.
			/// </summary>
			/// <param name="env"></param>
			/// <param name="cache"></param>
			/// <param name="sda">Data access object, decorator, to use for this ItemsCollectorEnv</param>
			/// <param name="hvoRoot"></param>
			public ItemsCollectorEnv(IVwEnv env, FdoCache cache, ISilDataAccess sda, int hvoRoot)
				: base(env, sda, hvoRoot)
			{
				m_cache = cache;
			}

			/// <summary>
			/// Return the list of hvos used to build the display in DisplayCell.
			/// </summary>
			public Set<int> HvosCollectedInCell
			{
				get
				{
					// Allow a zero in the set only if it's the only element.
					if (m_hvosInCell.Count > 1 && m_hvosInCell.Contains(0))
						m_hvosInCell.Remove(0);
					return m_hvosInCell;
				}
			}

			/// <summary>
			///
			/// </summary>
			/// <param name="s"></param>
			protected override void AddResultString(string s)
			{
				base.AddResultString(s);
				AddOwnerOfBasicPropToCellHvos();
			}

			private void AddOwnerOfBasicPropToCellHvos()
			{
				StackItem top = this.PeekStack;
				if (top != null)
					m_hvosInCell.Add(top.m_hvo);
			}

			/// <summary>
			///
			/// </summary>
			/// <param name="tag"></param>
			public override void AddIntProp(int tag)
			{
				base.AddIntProp(tag);
				AddOwnerOfBasicPropToCellHvos();
			}

			/// <summary>
			///
			/// </summary>
			/// <param name="tag"></param>
			public override void AddGenDateProp(int tag)
			{
				base.AddGenDateProp(tag);
				AddOwnerOfBasicPropToCellHvos();
			}

			/// <summary>
			///
			/// </summary>
			/// <param name="tag"></param>
			/// <param name="flags"></param>
			public override void AddTimeProp(int tag, uint flags)
			{
				base.AddTimeProp(tag, flags);
				AddOwnerOfBasicPropToCellHvos();
			}

			/// <summary>
			///
			/// </summary>
			/// <param name="tag"></param>
			/// <param name="vc"></param>
			/// <param name="frag"></param>
			public override void  AddObjProp(int tag, IVwViewConstructor vc, int frag)
			{
				base.AddObjProp(tag, vc, frag);
				int hvoItem = m_sda.get_ObjectProp(m_hvoCurr, tag);
				if (hvoItem == 0 && m_hvosInCell.Count == 0)
					m_hvosInCell.Add(0);
			}

		}

		#endregion ItemsCollectorEnv

		/// <summary>
		/// Remove any columns that have become invalid, presumably due to deleting a custom
		/// field.  See FWR-1279.
		/// </summary>
		internal bool RemoveInvalidColumns()
		{
			List<XmlNode> invalidColumns = new List<XmlNode>();
			for (int i = 0; i < m_columns.Count; ++i)
			{
				if (!IsValidColumnSpec(m_columns[i]))
					invalidColumns.Add(m_columns[i]);
			}
			for (int i = 0; i < invalidColumns.Count; ++i)
				m_columns.Remove(invalidColumns[i]);
			return invalidColumns.Count > 0;
		}
	}
}
