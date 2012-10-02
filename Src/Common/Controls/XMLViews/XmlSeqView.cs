// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: XmlView.cs
// Responsibility: WordWorks
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using System.Xml;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.Utils;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// An XmlSeqView allows a view of a sequence of objects to be defined by specifying
	/// an XML string or node or a layout name.
	///
	/// The XML is as for XmlView.
	///
	/// However, in an XmlSeqView, the top-level is a display of a particular property,
	/// MainSeqFlid.
	///
	/// Old approach:
	///
	/// The top level of the XML document is a node of type XmlView, which contains
	/// a sequence of nodes of type frag.
	/// The most common type of frag node describes how to display one type of
	/// fragment, typically an object of a particular type. A frag node has a
	/// name attribute which indicates when it is to be used. One frag node has
	/// the attribute "root" indicating that it is the fragment to be used
	/// to display the top-level object (supplied as another constructor argument).
	///
	/// Within a frag node may be other kinds of nodes depending on the context
	/// where it is to be used. Most commonly a fragment is meant to be used by
	/// the Display method, to display an object. Such a fragment may contain
	/// structural (flow object) nodes para, div, innerpile, or span. They may also contain
	/// lit nodes (contents inserted literally into the display), and nodes
	/// that insert properties (of the current object) into the display.
	///
	/// Immediately before a flow object node may appear zero or more mod nodes (short for modifier).
	/// A mod node has attributes prop{erty}, var{iation} and val{ue}. These are taken from the enumerations
	/// FwTextPropVar and FwTextPropType in Kernel/TextServ.idh.
	/// Enhance JohnT: build in a mapping so string values can be used for these.
	///
	/// All property-inserting nodes have attributes class and field which indicate
	/// which property is to be inserted. Alternatively the attribute flid may be
	/// used to give an integer-valued field identifier directly. If the class and field
	/// attributes are used, the view must be provided with an IFwMetaDataCache to
	/// enable their interpretation. If both sets of attributes are provided, the flid
	/// is used, and the other two are merely comments, so the metadata cache is not
	/// required.
	///
	/// Property-inserting nodes are as follows:
	///		- string inserts the indicated string property
	///		- stringalt inserts an alternative from a multilingual string.
	///		(the attribute wsid (an integer) or ows (a string name) is required.
	///		- int inserts an integer-valued property
	///		- obj inserts an (atomic) object property. The attribute "frag" is
	///		required. Its value is the name of a fragment node to be used to display
	///		the object.
	///		- objseq inserts an object sequence property. The attribute "frag" is
	///		required. Its value is the name of a fragment node to be used to display
	///		each object in the sequence.
	///
	///		Enhance JohnT: many useful view behaviors are not yet accessible using
	///		this approach. Consider adding lazyseq to implement a lazy view (but
	///		lazy loading of the data is a challenge); also enhancing objseq, or
	///		providing a different node type, to use DisplayVec to display object
	///		sequences with seperators.
	///
	///		Enhance JohnT: allow the user to specify a subclass of XmlVc, which
	///		could supply special behaviors in addition to the XML ones. Overrides
	///		would just call the inherited method if nothing special is required.
	///
	///		New approach:
	///
	///		The configuration node contains a "layout" attribute which is the name of the layout
	///		to use for each object in the sequence.
	/// </summary>
	// Example: // Review JohnT: should this be in the summary? Would the XML here
	// interfere witht the XML used to structure the summary?
	// <?xml version="1.0"?>
	// <!-- A simple interlinear view -->
	// <XmlView>
	//	<frag root="true" name="text">
	//		<para><objseq flid = "97" class = "Text" field = "Words" frag = "word"/></para>
	//	</frag>
	//	<frag name="word">
	//		<mod prop = "20" var = "1" val = "10000"/>
	//		<innerpile>
	//			<string flid = "99" class = "Word" field = "Form"/>
	//			<string flid = "98" class = "Word" field = "Type"/>
	//		</innerpile>
	//	</frag>
	// </XmlView>

	public class XmlSeqView : RootSite
	{
		/// <summary></summary>
		protected string m_sXmlSpec;
		/// <summary></summary>
		protected int m_hvoRoot;
		/// <summary></summary>
		protected int m_mainFlid;
		/// <summary>
		/// The data access that can interpret m_mainFlid of m_hvoRoot, typically a RecordListPublisher.
		/// The SDA mainly used in the view is a further decoration of this.
		/// </summary>
		protected ISilDataAccessManaged m_sdaSource;
		/// <summary></summary>
		protected XmlDocument m_docSpec;
		/// <summary></summary>
		protected XmlNode m_xnSpec;
		/// <summary></summary>
		protected XmlVc m_xmlVc;
		/// <summary></summary>
		protected IFwMetaDataCache m_mdc;
		/// <summary></summary>
		protected StringTable m_stringTable;
		bool m_fShowFailingItems; // display items that fail the condition specified in the view.
		private IApp m_app;

		/// <summary>
		/// This event notifies you that the selected object changed, passing an argument from
		/// which you can directly obtain the new object. This SelectionChangedEvent may fire
		/// even if the selection moves from one occurrence of an object to another occurrence
		/// of the same object.
		/// </summary>
		public event FwSelectionChangedEventHandler SelectionChangedEvent;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="XmlSeqView"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public XmlSeqView() : base(null)
		{
			// TODO: Add any initialization after the InitForm call
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Resets the tables.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ResetTables()
		{
			CheckDisposed();

			// Don't crash if we don't have any view content yet.  See LT-7244.
			if (m_xmlVc != null)
				m_xmlVc.ResetTables();
			if (RootBox != null)
				RootBox.Reconstruct();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Resets the tables.
		/// </summary>
		/// <param name="sLayoutName">Name of the s layout.</param>
		/// ------------------------------------------------------------------------------------
		public void ResetTables(string sLayoutName)
		{
			CheckDisposed();

			// Don't crash if we don't have any view content yet.  See LT-7244.
			if (m_xmlVc != null)
				m_xmlVc.ResetTables(sLayoutName);
			if (RootBox != null)
				RootBox.Reconstruct();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="XmlSeqView"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public XmlSeqView(int hvoRoot, int flid, XmlNode xnSpec, ISilDataAccessManaged sda, IApp app)
			: base(null)
		{
			m_app = app;
			InitXmlViewRootSpec(hvoRoot, flid, xnSpec, sda);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the vc.
		/// </summary>
		/// <value>The vc.</value>
		/// ------------------------------------------------------------------------------------
		public XmlVc Vc
		{
			get
			{
				CheckDisposed();
				return m_xmlVc;
			}
		}

		private void InitXmlViewRootSpec(int hvoRoot, int flid, XmlNode xnSpec, ISilDataAccessManaged sda)
		{
			m_hvoRoot = hvoRoot;
			m_mainFlid = flid;
			m_sdaSource = sda;
			Debug.Assert(xnSpec != null, "Creating an XMLView with null spec");
			m_xnSpec = xnSpec;
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

			base.Dispose(disposing);

			if (disposing)
			{
			}
			m_xmlVc = null;
			m_stringTable = null;
			m_mdc = null;
			m_sXmlSpec = null;
			m_docSpec = null;
			m_xnSpec = null;
		}

		/// <summary>
		/// Causes XMLViews to be editable by default.
		/// </summary>
		public static new Color DefaultBackColor
		{
			get
			{
				return SystemColors.Window;
			}
		}

		/// <summary>
		/// Reset the main object being shown.
		/// </summary>
		/// <param name="hvoRoot"></param>
		public void ResetRoot(int hvoRoot)
		{
			CheckDisposed();

			m_hvoRoot = hvoRoot;
			m_rootb.SetRootObject(m_hvoRoot, m_xmlVc, RootFrag, m_styleSheet);
			m_rootb.Reconstruct();
		}

		/// <summary>
		/// Magic number ALWAYS used for root fragment in this type of view.
		/// </summary>
		public int RootFrag
		{
			get
			{
				CheckDisposed();
				return 2;
			}
		}

		#region overrides
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override this method in your subclass.
		/// It should make a root box and initialize it with appropriate data and
		/// view constructor, etc.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void MakeRoot()
		{
			CheckDisposed();

			base.MakeRoot();

			if (m_fdoCache == null || DesignMode)
				return;

			IVwRootBox rootb = VwRootBoxClass.Create();
			rootb.SetSite(this);

			bool fEditable = XmlUtils.GetOptionalBooleanAttributeValue(m_xnSpec, "editable", true);
			string toolName = m_mediator.PropertyTable.GetStringProperty("currentContentControl", null);
			m_fShowFailingItems = m_mediator.PropertyTable.GetBoolProperty("ShowFailingItems-" + toolName, false);
			//m_xmlVc = new XmlVc(m_xnSpec, StringTbl); // possibly reinstate for old approach?
			string sLayout = null;
			string sProp = XmlUtils.GetOptionalAttributeValue(m_xnSpec, "layoutProperty", null);
			if (!String.IsNullOrEmpty(sProp))
				sLayout = m_mediator.PropertyTable.GetStringProperty(sProp, null);
			if (String.IsNullOrEmpty(sLayout))
				sLayout = XmlUtils.GetManditoryAttributeValue(m_xnSpec, "layout");
			m_xmlVc = new XmlVc(StringTbl, sLayout, fEditable, this, m_app,
				m_fShowFailingItems ? null : ItemDisplayCondition);
			ReadOnlyView = !fEditable;
			if (!fEditable)
				rootb.MaxParasToScan = 0;
			m_xmlVc.Cache = m_fdoCache;
			m_xmlVc.MainSeqFlid = m_mainFlid;

			ISilDataAccess sda = GetSda();
			rootb.DataAccess = sda;
			m_xmlVc.DataAccess = sda;

			rootb.SetRootObject(m_hvoRoot, m_xmlVc, RootFrag, m_styleSheet);
			m_rootb = rootb;
		}

		private ISilDataAccess GetSda()
		{
			XmlNode filterNode = m_xnSpec.SelectSingleNode("filterProps");
			if (filterNode == null || String.IsNullOrEmpty(filterNode.InnerText))
				return m_sdaSource;
			var fsda = new FilterSdaDecorator(m_sdaSource, m_mainFlid, m_hvoRoot);
			fsda.SetFilterFlids(filterNode.InnerText);
			return fsda;
		}

		private XmlNode ItemDisplayCondition
		{
			get { return m_xnSpec.SelectSingleNode("elementDisplayCondition"); }
		}

		/// <summary>
		/// Receives the broadcast message "PropertyChanged"
		/// </summary>
		public override void OnPropertyChanged(string name)
		{
			CheckDisposed();

			base.OnPropertyChanged(name);
			string toolName = m_mediator.PropertyTable.GetStringProperty("currentContentControl", null);
			if(name == "ShowFailingItems-" + toolName)
			{
				bool fShowFailingItems = m_mediator.PropertyTable.GetBoolProperty(name, false);
				if (fShowFailingItems != m_fShowFailingItems)
				{
					m_fShowFailingItems = fShowFailingItems;
					m_xmlVc.MakeRootCommand(this, m_fShowFailingItems ? null : ItemDisplayCondition);
					try
					{
						EditingHelper.DefaultCursor = Cursors.WaitCursor;
						using (new WaitCursor(TopLevelControl))
						{
							m_rootb.Reconstruct();
							Invalidate();
							Update(); // most of the time is the painting, we want the watch cursor till it's done.
						}
					}
					finally
					{
						EditingHelper.DefaultCursor = null;
					}
				}
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Notifies the site that something about the selection has changed.
		/// </summary>
		/// <param name="prootb"></param>
		/// <param name="vwselNew">Selection</param>
		/// <remarks>When overriding you should call the base class first.</remarks>
		/// -----------------------------------------------------------------------------------
		protected override void HandleSelectionChange(IVwRootBox prootb, IVwSelection vwselNew)
		{
			CheckDisposed();

			base.HandleSelectionChange(prootb, vwselNew);
			if (vwselNew == null)
				return;
			int clev = vwselNew.CLevels(false);		// anchor
			int clevEnd = vwselNew.CLevels(true);
			if (clev < 2 || clevEnd < 2)
				return; // paranoia
			int hvoRoot, tag, ihvo, ihvoEnd, cpropPrevious;
			IVwPropertyStore vps;
			vwselNew.PropInfo(true, clevEnd - 1, out hvoRoot, out tag, out ihvoEnd,
				out cpropPrevious, out vps);
			vwselNew.PropInfo(false, clev - 1, out hvoRoot, out tag, out ihvo,
				out cpropPrevious, out vps);
			// Give up if the selection doesn't indicate any top-level object; I think this can happen with pictures.
			// selection larger than a top-level object, maybe select all, side effects are confusing.
			if (ihvo != ihvoEnd || ihvo < 0)
				return;
			Debug.Assert(hvoRoot == m_hvoRoot);
			int hvoObjNewSel = m_sdaSource.get_VecItem(hvoRoot, tag, ihvo);
			if (hvoObjNewSel != 0)
			{
				// Notify any delegates that the selection of the main object in the vector
				// may have changed.
				if (SelectionChangedEvent != null)
					SelectionChangedEvent(this, new FwObjectSelectionEventArgs(hvoObjNewSel, ihvo));
			}
		}
		#endregion

		/// <summary>
		/// a look up table for getting the correct version of strings that the user will see.
		/// </summary>
		public StringTable StringTbl
		{
			get
			{
				CheckDisposed();

				return m_stringTable;
			}
			set
			{
				CheckDisposed();

				m_stringTable = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the object count.
		/// </summary>
		/// <value>The object count.</value>
		/// ------------------------------------------------------------------------------------
		public int ObjectCount
		{
			get
			{
				CheckDisposed();

				int cobj = m_sdaSource.get_VecSize(m_hvoRoot, m_mainFlid);
				return cobj;
			}
		}

		#region printing
		/// <summary>
		/// If help is available for the print dialog, set ShowHelp to true,
		/// and add an event handler that can display some help.
		/// See DraftView in TeDll for an example.
		/// </summary>
		/// <param name="dlg"></param>
		protected override void SetupPrintHelp(PrintDialog dlg)
		{
			base.SetupPrintHelp(dlg);
			dlg.AllowSelection = RootBox.Selection != null;
		}

		///<summary>
		/// Print method for printing one record (e.g. from RecordEditView)
		/// in Document View.
		///</summary>
		///<param name="pd"></param>
		///<param name="mainObjHvo"></param>
		public void PrintFromDetail(PrintDocument pd, int mainObjHvo)
		{
			CheckDisposed();

			var oldSda = RootBox.DataAccess;
			RootBox.DataAccess = CachePrintDecorator(m_sdaSource, m_hvoRoot, m_mainFlid, new[] {mainObjHvo});
			base.Print(pd);
			RootBox.DataAccess = oldSda;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Print method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void Print(PrintDocument pd)
		{
			CheckDisposed();

			ISilDataAccess oldSda = null;
			bool fPrintSelection = (pd.PrinterSettings.PrintRange == PrintRange.Selection);
			if (fPrintSelection)
			{
				oldSda = RootBox.DataAccess;
				IVwSelection sel = RootBox.Selection;
				int clev = sel.CLevels(true);
				int hvoObj, tag, ihvoEnd, ihvoAnchor, cpropPrevious;
				IVwPropertyStore vps;
				sel.PropInfo(true, clev - 1, out hvoObj, out tag, out ihvoEnd, out cpropPrevious, out vps);
				clev = sel.CLevels(false);
				sel.PropInfo(false, clev - 1, out hvoObj, out tag, out ihvoAnchor, out cpropPrevious, out vps);
				int[] originalObjects = m_sdaSource.VecProp(m_hvoRoot, m_mainFlid);
				int ihvoMin = Math.Min(ihvoEnd, ihvoAnchor);
				int ihvoLim = Math.Max(ihvoEnd, ihvoAnchor) + 1;
				var selectedObjects = new int[ihvoLim - ihvoMin];
				for (int i = 0; i < selectedObjects.Length; i++)
					selectedObjects[i] = originalObjects[i + ihvoMin];
				RootBox.DataAccess = CachePrintDecorator(m_sdaSource, m_hvoRoot, m_mainFlid, selectedObjects);
			}
			base.Print(pd);
			if (fPrintSelection)
				RootBox.DataAccess = oldSda;
		}

		///<summary>
		/// Decorate an ISilDataAccessManaged object with another layer limiting objects to print.
		///</summary>
		///<param name="oldSda"></param>
		///<param name="rootHvo"></param>
		///<param name="mainFlid"></param>
		///<param name="selectedObjects"></param>
		///<returns></returns>
		public static ISilDataAccessManaged CachePrintDecorator(ISilDataAccessManaged oldSda,
			int rootHvo, int mainFlid, int[] selectedObjects)
		{
			var printDecorator = new ObjectListPublisher(oldSda, mainFlid);
			printDecorator.CacheVecProp(rootHvo, selectedObjects);
			return printDecorator;
		}

		#endregion

		/// <summary>
		/// Display a wait cursor when drawing a sequence view.  This solves LT-4680, and
		/// probably helps elsewhere without hurting too much anywhere.
		/// </summary>
		/// <param name="e"></param>
		protected override void Draw(PaintEventArgs e)
		{
			using (new WaitCursor(this))
			{
				base.Draw(e);
			}
		}
	}
}
