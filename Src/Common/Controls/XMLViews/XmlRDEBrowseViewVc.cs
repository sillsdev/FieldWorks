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
// File: XmlRDEBrowseViewVc.cs
// Responsibility: Randy Regnier
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System.Xml;
using System.Drawing;
using System.Diagnostics;
using System.Collections.Generic;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// Summary description for XmlRDEBrowseViewVc.
	/// </summary>
	public class XmlRDEBrowseViewVc : XmlBrowseViewBaseVc
	{
		#region Constants

		/// <summary></summary>
		public const int khvoNewItem = -1234567890;
		/// <summary></summary>
		public const string ksEditColumnBaseName = "FakeEditColumn";

		#endregion Constants

		#region Data members

		// The following variables/constants are for implementing an editable row at the bottom
		// of the browse view table.
		string m_sEditRowModelClass = null;
		string m_sEditRowSaveMethod = null;
		string m_sEditRowMergeMethod = null;
		string m_sEditRowAssembly = null;
		string m_sEditRowClass = null;
		// A Set of Hvos. If an HVO is in the set,
		// it is the HVO of a 'new' row that is allowed to be edited.
		Set<int> m_editableHvos = new Set<int>();

		#endregion Data members

		#region Constructiona and initialization

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="xnSpec"></param>
		/// <param name="fakeFlid"></param>
		/// <param name="stringTable"></param>
		/// <param name="xbv"></param>
		public XmlRDEBrowseViewVc(XmlNode xnSpec, int fakeFlid, StringTable stringTable, XmlBrowseViewBase xbv)
			: base(xnSpec, fakeFlid, stringTable, xbv)
		{
			// set the border color
			BorderColor = SystemColors.ControlDark;

			// Check for the special editable row attributes.
			// this one is independently optional so we can handle it separately.
			m_sEditRowMergeMethod = XmlUtils.GetAttributeValue(xnSpec, "editRowMergeMethod", null);
			XmlAttribute xa = xnSpec.Attributes["editRowModelClass"];
			if (xa != null && xa.Value.Length != 0)
			{
				m_sEditRowModelClass = xa.Value;
				xa = xnSpec.Attributes["editRowSaveMethod"];
				if (xa != null && xa.Value.Length != 0)
				{
					m_sEditRowSaveMethod = xa.Value;
					xa = xnSpec.Attributes["editRowAssembly"];
					if (xa != null && xa.Value.Length != 0)
					{
						m_sEditRowAssembly = xa.Value;
						xa = xnSpec.Attributes["editRowClass"];
						if (xa != null && xa.Value.Length != 0)
						{
							m_sEditRowClass = xa.Value;
						}
						else
						{
							// Should we complain to the user?  Die horribly? ...
							m_sEditRowModelClass = null;
							m_sEditRowSaveMethod = null;
							m_sEditRowAssembly = null;
							Debug.WriteLine("editRowModelClass, editRowSaveMethod, and " +
								"editRowAssembly are set, but editRowClass is not!?");
						}
					}
					else
					{
						// Should we complain to the user?  Die horribly? ...
						m_sEditRowModelClass = null;
						m_sEditRowSaveMethod = null;
						Debug.WriteLine("editRowModelClass and editRowSaveMethod are set, " +
							"but editRowAssembly is not!?");
					}
				}
				else
				{
					// Should we complain to the user?  Die horribly? ...
					m_sEditRowModelClass = null;
					Debug.WriteLine("editRowModelClass is set, but editRowSaveMethod is not!?");
				}
			}

			// For RDE use, we want total RTL user experience (see LT-5127).
			Cache = m_xbv.Cache;
			m_fShowColumnsRTL = IsWsRTL(m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle);
		}

		#endregion Construction and initialization

		#region Properties

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the edit row model class.
		/// </summary>
		/// <value>The edit row model class.</value>
		/// ------------------------------------------------------------------------------------
		public string EditRowModelClass
		{
			get
			{
				return m_sEditRowModelClass;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the edit row assembly.
		/// </summary>
		/// <value>The edit row assembly.</value>
		/// ------------------------------------------------------------------------------------
		public string EditRowAssembly
		{
			get
			{
				return m_sEditRowAssembly;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the edit row class.
		/// </summary>
		/// <value>The edit row class.</value>
		/// ------------------------------------------------------------------------------------
		public string EditRowClass
		{
			get
			{
				return m_sEditRowClass;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the edit row save method.
		/// </summary>
		/// <value>The edit row save method.</value>
		/// ------------------------------------------------------------------------------------
		public string EditRowSaveMethod
		{
			get
			{
				return m_sEditRowSaveMethod;
			}
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the edit row merge method.
		/// </summary>
		/// <value>The edit row merge method.</value>
		/// ------------------------------------------------------------------------------------
		public string EditRowMergeMethod
		{
			get
			{
				return m_sEditRowMergeMethod;
			}
		}

		/// <summary>
		/// Return a Set of HVOs that are editable...
		/// typically new objects added this session.
		/// </summary>
		public Set<int> EditableObjectsClone()
		{

			return new Set<int>(m_editableHvos);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Editables the objects contains.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool EditableObjectsContains(int key)
		{
			return m_editableHvos.Contains(key);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Editables the objects add.
		/// </summary>
		/// <param name="key">The key.</param>
		/// ------------------------------------------------------------------------------------
		public void EditableObjectsAdd(int key)
		{
			lock (this)
			{
				m_editableHvos.Add(key);
			}
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Editables the objects clear.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void EditableObjectsClear()
		{
			lock (this)
			{
				m_editableHvos.Clear();
			}
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Editables the objects remove.
		/// </summary>
		/// <param name="data">The data.</param>
		/// ------------------------------------------------------------------------------------
		public void EditableObjectsRemove(int data)
		{
			lock (this)
			{
				m_editableHvos.Remove(data);
			}
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Editables the objects ids.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public ICollection<int> EditableObjectsIds()
		{
			return m_editableHvos;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Editables the objects remove invalid objects.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void EditableObjectsRemoveInvalidObjects()
		{
			if (m_editableHvos.Count == 0)
				return;		// no processing needed on empty list

			bool done = false;
			Set<int> validSenses = new Set<int>();
			lock (this)
			{
				while (!done)
				{
					bool restartEnumerator = false;
					foreach (int hvoSense in EditableObjectsIds())
					{
						if (validSenses.Contains(hvoSense))	// already processed
						{
							done = false;	// just for something to do...
						}
						else
						{
							if (m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoSense).ClassID <= 0)
							{
								EditableObjectsRemove(hvoSense);
								restartEnumerator = true;
								break;
							}
							else
							{
								validSenses.Add(hvoSense);
							}
						}
					}
					if (!restartEnumerator)
						done = true;
				}
				// at this point we have a list of only valid objects
			}
		}

		#endregion Properties

		#region Other methods

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
					// assume the root object has been loaded.
					base.Display(vwenv, hvo, frag);
					vwenv.AddObj(khvoNewItem, this, kfragEditRow);
					break;
				case kfragEditRow:
					AddEditRow(vwenv, hvo);
					break;
				case kfragListItem:
					if (hvo != khvoNewItem)
						base.Display(vwenv, hvo, frag);
					break;
				default:
					base.Display(vwenv, hvo, frag);
					break;
			}
		}

		/// <summary>
		/// Change the background color of the non-editable items
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="hvo"></param>
		/// <param name="frag"></param>
		protected override void AddTableRow(IVwEnv vwenv, int hvo, int frag)
		{
			// change the background color for all non-editable items
			if (!m_editableHvos.Contains(hvo))
				vwenv.set_IntProperty((int )FwTextPropType.ktptBackColor,
					(int)FwTextPropVar.ktpvDefault,
					(int)RGB(SystemColors.ControlLight));

			// use the base functionality, just needed to set the colors
			base.AddTableRow(vwenv, hvo, frag);
		}

		/// <summary>
		/// In this subclass all cells need a specific editability, but it depends on whether it's a new
		/// item or a pre-existing one.
		/// </summary>
		protected override void SetCellProperties(int rowIndex, int icol, XmlNode node, int hvo, IVwEnv vwenv, bool fIsCellActive)
		{
			SetCellEditability(vwenv, m_editableHvos.Contains(hvo));
		}


		internal override int SelectedRowBackgroundColor(int hvo)
		{
			if (m_editableHvos.Contains(hvo))
			{
				return (int)RGB(Color.FromKnownColor(KnownColor.Window));
			}
			return NoEditBackgroundColor;
		}

		private static int NoEditBackgroundColor
		{
			get { return (int)RGB(SystemColors.ControlLight); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a table/row for editing a new object.
		/// </summary>
		/// <param name="vwenv">The vwenv.</param>
		/// <param name="hvo">The hvo.</param>
		/// ------------------------------------------------------------------------------------
		private void AddEditRow(IVwEnv vwenv, int hvo)
		{
			// set the border color to gray
			vwenv.set_IntProperty((int )FwTextPropType.ktptBorderColor,
				(int)FwTextPropVar.ktpvDefault,
				(int)RGB(BorderColor));	//SystemColors.ControlDark));

			// Make a table
			VwLength[] rglength = m_xbv.GetColWidthInfo();
			int colCount = m_columns.Count;
			if (m_fShowSelected)
				colCount++;

			VwLength vl100; // Length representing 100%.
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
			for (int i = 0; i < colCount; ++i)
				vwenv.MakeColumns(1, rglength[i]);
			// the table only has a body (no header or footer), and only one row.
			vwenv.OpenTableBody();
			vwenv.OpenTableRow();
			IVwCacheDa cda = m_cache.DomainDataByFlid as IVwCacheDa;
			// Make the cells.
			if (m_fShowColumnsRTL)
			{
				for (int i = m_columns.Count; i > 0; --i)
					AddEditCell(vwenv, cda, i);
			}
			else
			{
				for (int i = 1; i <= m_columns.Count; ++i)
					AddEditCell(vwenv, cda, i);
			}
			vwenv.CloseTableRow();
			vwenv.CloseTableBody();
			vwenv.CloseTable();
		}

		private void AddEditCell(IVwEnv vwenv, IVwCacheDa cda, int i)
		{
			XmlNode node = m_columns[i - 1];
			// Make a cell and embed an editable virtual string for the column.
			var editable = XmlUtils.GetOptionalBooleanAttributeValue(node, "editable", true);
			if (!editable)
				vwenv.set_IntProperty((int)FwTextPropType.ktptBackColor, (int)FwTextPropVar.ktpvDefault, NoEditBackgroundColor);
			vwenv.OpenTableCell(1, 1);
			int flid = XMLViewsDataCache.ktagEditColumnBase + i;
			int ws = WritingSystemServices.GetWritingSystem(m_cache, node, null,
				m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle).Handle;

			// Paragraph directionality must be set before the paragraph is opened.
			bool fRTL = IsWsRTL(ws);
			vwenv.set_IntProperty((int)FwTextPropType.ktptRightToLeft,
				(int)FwTextPropVar.ktpvEnum, fRTL ? -1 : 0);
			vwenv.set_IntProperty((int)FwTextPropType.ktptAlign,
				(int)FwTextPropVar.ktpvEnum,
				fRTL ? (int)FwTextAlign.ktalRight : (int)FwTextAlign.ktalLeft);

			// Fill in the cell with the virtual property.
			vwenv.OpenParagraph();
			vwenv.set_IntProperty((int)FwTextPropType.ktptEditable,
				(int)FwTextPropVar.ktpvEnum,
				editable ? (int)TptEditable.ktptIsEditable : (int)TptEditable.ktptNotEditable);
			vwenv.AddStringAltMember(flid, ws, this);
			vwenv.CloseParagraph();
			vwenv.CloseTableCell();
		}

		#endregion Other methods
	}
}
