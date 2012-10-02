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
using System.Xml;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.LangProj;
using SIL.Utils;
using SIL.FieldWorks.Filters;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils; // for OleCnvt
using SIL.FieldWorks.Resources; // for check-box icons.
using System.Reflection;
using SIL.FieldWorks.Common.RootSites;

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
		protected const int kfragListItemInner = 100004;

		/// <summary>
		/// This item controls the status of the check box in the bulk edit view, and thus, whether an item is included
		/// in any bulk edit operations. HOWEVER, we don't cache this value for all objects in the list. The VC has a default
		/// state, DefaultChecked, which controls whether an item is selected if it has no value cached for ktagItemSelected.
		/// The value of this property should therefore always be accessed through BrowseViewer.GetCurrentCheckedValue().
		/// The one exception is when the check box is actually displayed by the VC; this uses the simple value in the cache,
		/// which is OK because a line in LoadDataFor makes sure there is a value cached for any item that is visible.
		/// </summary>
		public const int ktagItemSelected = -2000; // enhance: we should install this as a virtual handler so we load the proper default.
		internal const int ktagItemEnabled = -2001;
		// if ktagActiveColumn has a value for m_hvoRoot, then if the check box is on
		// for a given row, the indicated cell is highlighted with a pale blue
		// background, and the ktagAlternateValue property is displayed for those cells.
		// So that no action is required for default behavior, ktagActiveColumn uses
		// 1-based indexing, so zero means no active column.
		internal const int ktagActiveColumn = -2001;
		internal const int ktagAlternateValue = -2002;
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
		/// <summary>// Specifies the default state of check boxes. </summary>
		protected bool m_fDefaultChecked;
		/// <summary></summary>
		protected stdole.IPicture m_UncheckedCheckPic;
		/// <summary></summary>
		protected stdole.IPicture m_CheckedCheckPic;
		/// <summary></summary>
		protected stdole.IPicture m_DisabledCheckPic;
		/// <summary>Width in millipoints of the check box.</summary>
		protected int m_dxmpCheckWidth;
		/// <summary>Roughly 1-pixel border.</summary>
		protected int m_dxmpCheckBorderWidth = 72000 / 96;
		/// <summary></summary>
		protected XmlBrowseViewBase m_xbv;
		/// <summary></summary>
		protected ISortItemProvider m_sortItemProvider;
		stdole.IPicture m_PreviewArrowPic;
		stdole.IPicture m_PreviewRTLArrowPic;
		int m_icolOverrideAllowEdit = -1; // index of column to force allow editing in.
		bool m_fShowEnabled = false;
		bool m_fInOnPaint = false;
		// A writing system we wish to force to be used when a <string> element displays a multilingual property.
		// Todo JohnT: Get rid of this!! It is set in a <column> element and used within the display of nested
		// objects. This can produce unexpected results when the object property changes, because the inner
		// Dispay() call is not within the outer one that normally sets m_wsForce.
		// m_wsForce was introduced in place of m_columnNode as part of the process of getting rid of it.
		private int m_wsForce;
		// The writing system calculated by looking at the hvo and flid, as well as the ws attribute.
		private int m_wsBest;
		private int m_tagMe;

		/// <summary>
		/// Set this variable true for displaying columns in reverse order.  This may also affect
		/// how the TAB key works.
		/// </summary>
		protected bool m_fShowColumnsRTL = false;

		#endregion Data Members

		#region Construction and initialization

		internal string ColListId
		{
			get
			{
				CheckDisposed();

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
				CheckDisposed();
				return m_fShowEnabled;
			}
			set
			{
				CheckDisposed();
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
				CheckDisposed();
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
		/// This contructor is used by SortMethodFinder to make a braindead VC.
		/// </summary>
		public XmlBrowseViewBaseVc(FdoCache cache, StringTable table) : base(table)
		{
			Debug.Assert(cache != null);

			m_fakeFlid = 0;
			Cache = cache;	// sets m_mdc and m_layouts as well as m_cache.
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
			: this(xbv.Cache, stringTable)
		{
			Debug.Assert(xnSpec != null);
			Debug.Assert(stringTable != null);
			Debug.Assert(xbv != null);

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
								savedCols = savedCols.Replace("root version=\"12\"", "root version=\"13\"");
								m_xbv.Mediator.PropertyTable.SetProperty(ColListId, savedCols);
								doc.LoadXml(savedCols);
								break;
							default:
								MessageBox.Show(null, XMLViewsStrings.ksInvalidSavedLayout, XMLViewsStrings.ksNote);
								doc = null;
								// Forget the old settings, so we don't keep complaining every time the program runs.
								// There doesn't seem to be any way to remove the property altogether, so at least, make it empty.
								m_xbv.Mediator.PropertyTable.SetProperty(ColListId, "", XCore.PropertyTable.SettingsGroup.LocalSettings);
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

			// If we have an xbv, this needs to be set now, because the client view may use it
			// before setting the cache, which prevents the compute-on-demand from working.
			m_tagMe = FDO.MeVirtualHandler.InstallMe(xbv.Cache.VwCacheDaAccessor).Tag;
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
				m_UncheckedCheckPic = (stdole.IPicture)
					OLECvt.ToOLE_IPictureDisp(ResourceHelper.UncheckedCheckBox);
				m_CheckedCheckPic =
					(stdole.IPicture)OLECvt.ToOLE_IPictureDisp(ResourceHelper.CheckedCheckBox);
				m_DisabledCheckPic =
					(stdole.IPicture)OLECvt.ToOLE_IPictureDisp(ResourceHelper.DisabledCheckBox);
				// We want a width in millipoints (72000/inch). Value we have is in 100/mm.
				// There are 25.4 mm/inch.
				m_dxmpCheckWidth = m_UncheckedCheckPic.Width * 72000 / 2540;
			}
			// By default, we currently set check boxes
			XmlAttribute xaDefaultCheckedAll = m_xnSpec.Attributes["defaultChecked"];
			m_fDefaultChecked = xaDefaultCheckedAll == null || xaDefaultCheckedAll.Value == "true";
		}

		/// <summary>
		/// Provide access to the special tag used to implement highlighting the selected row.
		/// </summary>
		internal int TagMe
		{
			get
			{
				CheckDisposed();

				// When created by restoring a persisted LayoutFinder, we don't have a browse view,
				// so the constructor can't set this. Set it on demand.
				if (m_tagMe == 0)
					m_tagMe = FDO.MeVirtualHandler.InstallMe(m_cache.VwCacheDaAccessor).Tag;
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
			CheckDisposed();

			XmlVc vc = null;
			if (ListItemsClass != 0)
				vc = this;
			m_possibleColumns = PartGenerator.GetGeneratedChildren(m_xnSpec.SelectSingleNode("columns"),
						 m_xbv.Cache.MetaDataCacheAccessor, vc, (int)ListItemsClass);
			return m_possibleColumns;
		}

		uint m_listItemsClass = 0;
		/// <summary>
		/// the class (or base class) of the items in SortItemProvider
		/// </summary>
		internal uint ListItemsClass
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
						m_listItemsClass = (uint)SortItemProvider.ListItemsClass;

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
				if (propWs != null)
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
					if (propWs.flid == flid)
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
				string sWs = XmlUtils.GetOptionalAttributeValue(child, "ws");
				if (sWs != null)
				{
					if (sWs.StartsWith("$ws="))
						sWs = sWs.Substring(4);
					if (sWs != "reversal")
					{
						int ws = m_cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr(sWs);
						if (ws == 0)
							return true;	// invalid writing system
						// Check whether we have a reversal index for the given writing system.
						IEnumerator<IReversalIndex> ere =
							m_cache.LangProject.LexDbOA.ReversalIndexesOC.GetEnumerator();
						while (ere.MoveNext())
						{
							if (ere.Current.WritingSystemRAHvo == ws)
								return false;
						}
						return true;
					}
				}
			}
			return false;
		}

		private bool Includes(string[] items, string target)
		{
			foreach(string s in items)
				if (s == target)
					return true;
			return false;
		}
		#endregion Construction and initialization

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Executes in two distinct scenarios.
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
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
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_columns != null)
					m_columns.Clear();
				if (m_possibleColumns != null)
					m_possibleColumns.Clear();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_columns = null;
			m_possibleColumns = null;
			m_xbv = null;
			m_sortItemProvider = null;
			if (m_UncheckedCheckPic != null)
			{
				Marshal.ReleaseComObject(m_UncheckedCheckPic);
				m_UncheckedCheckPic = null;
			}
			if (m_CheckedCheckPic != null)
			{
				Marshal.ReleaseComObject(m_CheckedCheckPic);
				m_CheckedCheckPic = null;
			}
			if (m_DisabledCheckPic != null)
			{
				Marshal.ReleaseComObject(m_DisabledCheckPic);
				m_DisabledCheckPic = null;
			}
			if (m_PreviewArrowPic != null)
			{
				Marshal.ReleaseComObject(m_PreviewArrowPic);
				m_PreviewArrowPic = null;
			}
			if (m_PreviewRTLArrowPic != null)
			{
				Marshal.ReleaseComObject(m_PreviewRTLArrowPic);
				m_PreviewRTLArrowPic = null;
			}

			base.Dispose (disposing);
		}

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
				CheckDisposed();
				return m_icolOverrideAllowEdit;
			}
			set
			{
				CheckDisposed();
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
				CheckDisposed();
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
				CheckDisposed();
				return m_fDefaultChecked;
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
				CheckDisposed();
				return m_dxmpCheckWidth + 4 * m_dxmpCheckBorderWidth;
			}
		}

		internal virtual List<XmlNode> ColumnSpecs
		{
			get
			{
				CheckDisposed();
				return m_columns;
			}
			set
			{
				CheckDisposed();
				m_columns = value;
			}
		}

		internal List<XmlNode> PossibleColumnSpecs
		{
			get
			{
				CheckDisposed();
				return m_possibleColumns;
			}
		}

		internal ISortItemProvider SortItemProvider
		{
			get
			{
				CheckDisposed();
				return m_sortItemProvider;
			}
			set
			{
				CheckDisposed();
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
				CheckDisposed();
				m_PreviewArrowPic = (stdole.IPicture)OLECvt.ToOLE_IPictureDisp(value);
				Image x = value;
				x.RotateFlip(RotateFlipType.Rotate180FlipNone);
				m_PreviewRTLArrowPic = (stdole.IPicture)OLECvt.ToOLE_IPictureDisp(x);
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
				CheckDisposed();
				return m_BorderColor;
			}
			set
			{
				CheckDisposed();
				m_BorderColor = value;
			}
		}

		const int kclrBackgroundSelRow = (int)0xFFE6D7;

		internal virtual int SelectedRowBackgroundColor(int hvo)
		{
			CheckDisposed();

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
			if (index >= Cache.MainCacheAccessor.get_VecSize(hvoDummy, tagDummy))
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
			if ((vwenv.DataAccess as VwCacheDa).get_IsPropInCache(hvoRoot, ktagActiveColumn, (int)CellarModuleDefns.kcptInteger, 0))
				icolActive = vwenv.DataAccess.get_IntProp(hvoRoot, ktagActiveColumn);
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
			// look for a writing system ID.
			XmlAttribute xa = node.Attributes["ws"];
			XmlNode xn = node;
			if (xa == null)
				xn = XmlUtils.FindNode(node, "string");
			m_wsBest = 0;
			if (xn != null)
			{
				int hvoBest = 0;
				int flidBest = 0;
				if (LangProject.GetWsRequiresObject(xn))
				{
					// The 'best' ws series needs the hvo and flid in order to work.
					// Some others (e.g., 'reversal' which require the hvo don't need a flid.
					hvoBest = hvo; // Wonder it isn't really the hvoRoot that is to be used.
					string flidname = XmlUtils.GetOptionalAttributeValue(xn, "field");
					if (flidname != null)
					{
						string classname;
						xa = xn.Attributes["class"];
						if (xa == null)
						{
							// Get it from the meta data cache.
							classname = m_cache.GetClassName((uint)m_cache.GetClassOfObject(hvo));
						}
						else
						{
							classname = xa.Value;
						}
						flidBest = (int)m_mdc.GetFieldId(classname, flidname, true);
					}
				}
				m_wsBest = LangProject.GetWritingSystem(xn, m_cache, null, hvoBest, flidBest, m_cache.DefaultAnalWs);
				IWritingSystem lgWs = m_cache.LanguageWritingSystemFactoryAccessor.get_EngineOrNull(m_wsBest);
				if (lgWs != null)
					fRightToLeft = lgWs.RightToLeft;
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

			bool fIsCellActive = false;
			if (icolActive != 0 && m_PreviewArrowPic != null)
			{
				// The display depends on which column is active and whether the current row is selected.
				vwenv.NoteDependency(new int[] { hvoRoot, hvo, hvo }, new int[] { ktagActiveColumn, ktagItemSelected, ktagItemEnabled }, 3);
				// We're doing the active column thing.
				if (icol == icolActive)
				{
					fIsCellActive = vwenv.DataAccess.get_IntProp(hvo, ktagItemSelected) != 0
						&& vwenv.DataAccess.get_IntProp(hvo, ktagItemEnabled) != 0;
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
					XmlNode nodeToProcess = GetColumnNode(node, hvo, m_cache.MainCacheAccessor, m_layouts);
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
				ManyOnePathSortItem item = m_sortItemProvider.SortItemAt(ihvo);
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

		/// <summary>
		/// Passed a column node, determines whether it indicates a forced writing system, and if so sets it.
		/// We want to get rid of this whole forced WS mechanism! Don't add uses of this.
		/// </summary>
		/// <param name="node"></param>
		private void SetForcedWs(XmlNode node)
		{
			string wsSpec = XmlUtils.GetOptionalAttributeValue(node, "ws");
			if (wsSpec != null)
			{
				string realWsSpec = LangProject.GetWsSpecWithoutPrefix(wsSpec);
				// If it's a magic wsId, we must NOT call GetWsFromStr, that is VERY slow
				// when what we pass isn't a valid WS name.
				if (LangProject.GetMagicWsIdFromName(realWsSpec) != 0)
					WsForce = m_wsBest;
				else
				{
					WsForce =
						m_cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr(realWsSpec);
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
			vwenv.AddStringProp(ktagAlternateValue, this);
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
				int enabled = vwenv.DataAccess.get_IntProp(hvo, ktagItemEnabled);
				vwenv.NoteDependency(new int[] { hvo }, new int[] { ktagItemEnabled }, 1);
				if (enabled != 0)
				{
					// Even if the view as a whole does not allow editing, presumably a check box
					// does!
					vwenv.set_IntProperty((int)FwTextPropType.ktptEditable,
						(int)FwTextPropVar.ktpvEnum,
						(int)TptEditable.ktptIsEditable);
					vwenv.AddIntPropPic(ktagItemSelected, this, kfragCheck, 0, 1);
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
				vwenv.AddIntPropPic(ktagItemSelected, this, kfragCheck, 0, 1);
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
				vwenv.NoteDependency(new int[] { hvo, hvo }, new int[] { ktagItemSelected, ktagItemEnabled }, 2);
				// Explicitly insert the preview here, if enabled etc.
				if (vwenv.DataAccess.get_IntProp(hvo, ktagItemSelected) != 0
										&& vwenv.DataAccess.get_IntProp(hvo, ktagItemEnabled) != 0)
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
		/// Given a ManyOnePathSortItem that describes one of the rows in the view, and a node
		/// that configures one of the columns, make the required calls to the VwEnv to display the cell
		/// contents. This is also used by the LayoutFinder.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="node">The node.</param>
		/// <param name="hvo">The hvo.</param>
		/// <param name="vwenv">The vwenv.</param>
		/// ------------------------------------------------------------------------------------
		public void DisplayCell(ManyOnePathSortItem item, XmlNode node, int hvo, IVwEnv vwenv)
		{
			CheckDisposed();

			List<XmlNode> outerParts = new List<XmlNode>();
			int hvoToDisplay;
			NodeDisplayCommand dispCommand = XmlViewsUtils.GetDisplayCommandForColumn(item, node, m_cache.MetaDataCacheAccessor,
				m_cache.MainCacheAccessor, m_layouts, out hvoToDisplay, outerParts);
			// See if the column has a writing system established for it.
			try
			{
				if (node.Name == "column")
				{
					SetForcedWs(node);
				}
				OpenOuterParts(outerParts, vwenv, hvoToDisplay);
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
			if (!String.IsNullOrEmpty(sWs))
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
				IWritingSystem lgws = m_cache.LanguageWritingSystemFactoryAccessor.get_EngineOrNull(ws);
				if (lgws != null)
					return lgws.RightToLeft;
			}
			return false;
		}

		internal virtual bool AllowEdit(int icol, XmlNode node, bool fIsCellActive, int hvo)
		{
			CheckDisposed();

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
						ICmObject co = CmObject.CreateFromDBObject(m_cache, hvo);
						bool fNot = false;
						if (sProperty[0] == '!')
						{
							fNot = true;
							sProperty = sProperty.Substring(1);
						}
						MethodInfo mi = co.GetType().GetMethod(sProperty);
						if (mi != null)
						{
							string sWs = XmlUtils.GetOptionalAttributeValue(node, "ws");
							if (sWs != null)
							{
								if (sWs.StartsWith(LangProject.WsParamLabel))
									sWs = sWs.Substring(LangProject.WsParamLabel.Length);
								int ws = XmlViewsUtils.GetWsFromString(sWs, m_cache);
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
			return fAllowEdit;
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
			CheckDisposed();

			switch (frag)
			{
				case kfragRoot:
					// assume the root object has been loaded.
					if (hvo != 0)
						vwenv.AddLazyVecItems(m_fakeFlid, this, kfragListItem);
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
		public override stdole.IPicture DisplayPicture(IVwEnv vwenv, int hvo, int tag, int val,
			int frag)
		{
			CheckDisposed();

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
			CheckDisposed();

			// Have FDO load the object to make sure its properties are in the cache.
			foreach (int hvo in rghvo)
			{
				try
				{
					LoadDataForObject(hvo);
				}
				catch (Exception)
				{
					if (!m_cache.IsValidObject(hvo))
					{
						MessageBox.Show(m_xbv,
							XMLViewsStrings.ksDeletedObjectMsg,
							XMLViewsStrings.ksDeletedObject,
							MessageBoxButtons.OK, MessageBoxIcon.Warning);
						if (m_xbv != null && m_xbv.Mediator != null)
							m_xbv.Mediator.BroadcastMessage("Refresh", null);
						return;
					}
					throw;
				}
				// It might be useful to make real objects out of any dummy ones we can see. This is controlled by
				// an attribute of the XML file, convertDummiesInView, so it may do nothing at all. Note that this
				// happens in the background (if at all); it isn't guaranteed that objects we can see are real.
				// Enabling it may cause somewhat erratic behavior in the scroll bar as the substitutions are made.
				// For this reason it is currently (Mar 2008) turned off in the XML.
				if (m_cache.IsDummyObject(hvo) && this.InOnPaint && m_xbv.ShouldConvertDummiesInView())
				{
					RequestConversionToRealEventArgs args = new RequestConversionToRealEventArgs(hvo, tag, null, false);
					OnRequestConversionToReal(this, args);
				}

			}
		}

		/// <summary>
		/// Allows view browser to let us know we're doing an OnPaint.
		/// That's the only time we really want to request to convert dummy objects to real ones.
		/// </summary>
		public bool InOnPaint
		{
			get
			{
				CheckDisposed();
				return m_fInOnPaint;
			}
			set
			{
				CheckDisposed();
				m_fInOnPaint = value;
			}
		}

		/// <summary>
		/// Try to convert a dummy object to a real object.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		public void OnRequestConversionToReal(object sender, RequestConversionToRealEventArgs args)
		{
			CheckDisposed();

			IVwVirtualHandler vh = m_cache.VwCacheDaAccessor.GetVirtualHandlerId(args.DataFlid);
			// let the owning list convert this to a real object, if it can and wants to.
			if (vh == null)
			{
				// try the virtual handler of the flid that owns this dummy object.
				int owningFlid = m_cache.GetOwningFlidOfObject(args.DummyHvo);
				args.OwningFlid = owningFlid;
				vh = m_cache.VwCacheDaAccessor.GetVirtualHandlerId(owningFlid);
			}
			if (vh != null && vh is IDummyRequestConversion)
			{
				ISilDataAccess sda = m_cache.MainCacheAccessor;

				// As we do the conversion, it's important to get these three pseudo-properties into the EXACT
				// same state for the new object as for the dummy. I'm not sure about the other two, but especially
				// for wasChecked, just reading it and writing it is not good enough, because we'll get a default
				// of zero (unchecked) if it isn't in the cache at all, but possibly we are depending on code
				// that treats it as CHECKED if it isn't cached at all.
				bool fWasCheckedCached = sda.get_IsPropInCache(args.DummyHvo, XmlBrowseViewVc.ktagItemSelected,
					(int)CellarModuleDefns.kcptInteger, 0);
				int wasChecked = 0;
				if (fWasCheckedCached)
					wasChecked = m_cache.MainCacheAccessor.get_IntProp(args.DummyHvo, XmlBrowseViewVc.ktagItemSelected);

				bool fWasEnabledCached = sda.get_IsPropInCache(args.DummyHvo, XmlBrowseViewVc.ktagItemEnabled,
					(int)CellarModuleDefns.kcptInteger, 0);
				int wasEnabled = 0;
				if (fWasEnabledCached)
					wasEnabled = m_cache.MainCacheAccessor.get_IntProp(args.DummyHvo, XmlBrowseViewVc.ktagItemEnabled);

				bool fWasPreviewCached = sda.get_IsPropInCache(args.DummyHvo, XmlBrowseViewVc.ktagAlternateValue,
					(int)CellarModuleDefns.kcptString, 0);
				ITsString tssPreview = null;
				if (fWasPreviewCached)
					tssPreview = m_cache.MainCacheAccessor.get_StringProp(args.DummyHvo, ktagAlternateValue);

				(vh as IDummyRequestConversion).OnRequestConversionToReal(this, args);

				if (args.RealObject != null)
				{
					if (fWasCheckedCached)
						m_cache.VwCacheDaAccessor.CacheIntProp(args.RealObject.Hvo, XmlBrowseViewVc.ktagItemSelected, wasChecked);
					if (fWasEnabledCached)
						m_cache.VwCacheDaAccessor.CacheIntProp(args.RealObject.Hvo, XmlBrowseViewVc.ktagItemEnabled, wasEnabled);
					if (fWasPreviewCached)
						m_cache.VwCacheDaAccessor.CacheStringProp(args.RealObject.Hvo, ktagAlternateValue, tssPreview);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads the data for object.
		/// </summary>
		/// <param name="hvo">The hvo.</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void LoadDataForObject(int hvo)
		{
			// JohnT: I think we make the CmObject here to load all properties at once. It should only be done
			// for real objects; it won't help for dummy ones. This may have something to do with LT-3121. The
			// following comment was on the code that prevented this method being called at all for dummy objects:
				// lt-3121: only load for hvo values > 0
			// But that was not the right solution, because we definitely need the behavior below of updating the
			// ktagItemSelected value for dummy objects.
			if (!m_cache.IsDummyObject(hvo))
				CmObject.CreateFromDBObject(m_cache, hvo);
			// If we don't already have information about whether it is selected,
			// set it to the default value
			if (!m_cache.MainCacheAccessor.get_IsPropInCache(hvo, ktagItemSelected,
				(int)CellarModuleDefns.kcptInteger, 0))
			{
				(m_cache.MainCacheAccessor as IVwCacheDa).CacheIntProp(hvo,
					ktagItemSelected, this.DefaultChecked ? 1 : 0);
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// This routine is used to estimate the height of an item. The item will be one of
		/// those you have added to the environment using AddLazyItems. Note that the calling
		/// codedoes NOT ensure that data for displaying the item in question has been loaded.
		/// The first three arguments are as for Display, that is, you are being asked to
		/// estimate how much vertical space is needed to display this item in the available
		/// width.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="frag"></param>
		/// <param name="dxAvailWidth"></param>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		public override int EstimateHeight(int hvo, int frag, int dxAvailWidth)
		{
			CheckDisposed();

			return 17; // Assume a browse view line is about 17 points high.
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Display nothing when we encounter an ORC.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public override void DisplayEmbeddedObject(IVwEnv vwenv, int hvo)
		{
			CheckDisposed();
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
			FdoCache m_cache;
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
	}
}
