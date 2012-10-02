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
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;
using System.Collections.Generic;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// An XmlView allows a view to be defined by specifying an XML string.
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

	public class XmlView : RootSiteControl
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		/// <summary></summary>
		protected int m_hvoRoot;
		/// <summary></summary>
		protected string m_layoutName;
		/// <summary></summary>
		protected XmlNode m_xnSpec;
		/// <summary></summary>
		protected XmlVc m_xmlVc;
		/// <summary></summary>
		protected IFwMetaDataCache m_mdc;
		/// <summary></summary>
		protected StringTable m_stringTable;
		/// <summary></summary>
		protected bool m_fEditable = true;
		private List<int> m_selectedObjects = new List<int>(1);
		bool m_fInChangeSelectedObjects = false;
		private ISilDataAccess m_sda;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:XmlView"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public XmlView()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:XmlView"/> class.
		/// </summary>
		/// <param name="hvoRoot">The hvo root.</param>
		/// <param name="xml">The XML.</param>
		/// ------------------------------------------------------------------------------------
		public XmlView(int hvoRoot, string xml)
		{
			var docSpec = new XmlDocument();
			docSpec.LoadXml(xml);
			InitXmlViewRootSpec(hvoRoot, docSpec["XmlView"]);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:XmlView"/> class.
		/// </summary>
		/// <param name="hvoRoot">The hvo root.</param>
		/// <param name="xnSpec">The xn spec.</param>
		/// ------------------------------------------------------------------------------------
		public XmlView(int hvoRoot, XmlNode xnSpec)
		{
			InitXmlViewRootSpec(hvoRoot, xnSpec);
		}

		/// <summary>
		/// This is now the approved constructor for an XmlView.
		/// </summary>
		/// <param name="hvo">Root object to display</param>
		/// <param name="layoutName">Name of standard layout of that object to use to display it</param>
		/// <param name="stringTable">Optional string table for translating literals</param>
		/// <param name="fEditable">True to enable editing at top level.</param>
		public XmlView (int hvo, string layoutName, StringTable stringTable, bool fEditable)
		{
			StringTbl = stringTable;
			m_layoutName = layoutName;
			m_hvoRoot = hvo;
			m_fEditable = fEditable;
		}

		/// <summary>
		/// This is the approved constructor for an XmlView using a decorator.
		/// </summary>
		/// <param name="hvo">Root object to display</param>
		/// <param name="layoutName">Name of standard layout of that object to use to display it</param>
		/// <param name="stringTable">Optional string table for translating literals</param>
		/// <param name="fEditable">True to enable editing at top level.</param>
		/// <param name="sda">typically a decorator; you can omit this to take the default one from the Cache.</param>
		public XmlView(int hvo, string layoutName, StringTable stringTable, bool fEditable, ISilDataAccess sda)
			: this(hvo, layoutName, stringTable, fEditable)
		{
			m_sda = sda;
		}

		private void InitXmlViewRootSpec(int hvoRoot, XmlNode xnSpec)
		{
			m_hvoRoot = hvoRoot;
			Debug.Assert(xnSpec != null, "Creating an XMLView with null spec");
			m_xnSpec = xnSpec;
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:XmlView"/> class.
		/// </summary>
		/// <param name="hvoRoot">The hvo root.</param>
		/// <param name="xnSpec">The xn spec.</param>
		/// <param name="stringTable">The string table.</param>
		/// ------------------------------------------------------------------------------------
		public XmlView(int hvoRoot, XmlNode xnSpec, StringTable stringTable)
		{
			StringTbl = stringTable;
			InitXmlViewRootSpec(hvoRoot, xnSpec);
		}

		/// <summary>
		/// Reset the tables in the VC, typically when the XML your view is based on has changed.
		/// </summary>
		public void ResetTables()
		{
			CheckDisposed();

			// Don't crash if we don't have any view content yet.  See LT-7244.
			if (m_xmlVc != null)
				m_xmlVc.ResetTables();
			if (RootBox != null)
				this.RootBox.Reconstruct();
		}

		/// <summary>
		/// Get the (possibly decorated) SDA used to display data in this view.
		/// </summary>
		public ISilDataAccess DecoratedDataAccess
		{
			get { return m_sda; }
		}

		/// <summary>
		/// Reset the tables in the VC, and set a new layout, typically when the XML your view
		/// is based on has changed.
		/// </summary>
		public void ResetTables(string sNewLayout)
		{
			CheckDisposed();

			// Don't crash if we don't have any view content yet.  See LT-7244.
			if (m_xmlVc != null)
				m_xmlVc.ResetTables(sNewLayout);
			// but save the new layout name just the same.  See FWR-2887.
			else
				m_layoutName = sNewLayout;
			if (RootBox != null)
				this.RootBox.Reconstruct();
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

			base.Dispose( disposing );

			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			m_xmlVc = null;
			m_layoutName = null;
			m_xnSpec = null;
			m_mdc = null;
			m_stringTable = null;
		}

		/// <summary>
		/// Causes XMLViews to be editable by default.
		/// </summary>
		public static new Color DefaultBackColor
		{
			get
			{
				return System.Drawing.SystemColors.Window;
			}
		}

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
		}
		#endregion

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

			if (m_sda == null)
				m_sda = m_fdoCache.DomainDataByFlid;

			Debug.Assert(m_layoutName != null, "No layout name.");
			IApp app = null;
			if (m_mediator != null && m_mediator.PropertyTable != null)
				app = m_mediator.PropertyTable.GetValue("App") as IApp;
			m_xmlVc = new XmlVc(StringTbl, m_layoutName, m_fEditable, this, app);
			m_xmlVc.Cache = m_fdoCache;
			m_xmlVc.DataAccess = m_sda; // let it use the decorator if any.

			rootb.DataAccess = m_sda;
			//if (this.EditingHelper != null)
			//    this.EditingHelper.Editable = m_fEditable;
			m_rootb = rootb;
			RootObjectHvo = m_hvoRoot;
		}

		/// <summary>
		/// the object that has properties that are shown by this view.
		/// </summary>
		public int RootObjectHvo
		{
			set
			{
				CheckDisposed();

				m_hvoRoot = value;
				int frag = 1; // magic number ALWAYS used for root fragment in this type of view.
				m_rootb.SetRootObject(m_hvoRoot, m_xmlVc, frag, m_styleSheet);
			}
		}

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
		/// Selections the changed.
		/// </summary>
		/// <param name="prootb">The prootb.</param>
		/// <param name="sel">The sel.</param>
		/// ------------------------------------------------------------------------------------
		protected override void HandleSelectionChange(IVwRootBox prootb, IVwSelection sel)
		{
			CheckDisposed();

			if (m_fInChangeSelectedObjects)
				return;
			m_fInChangeSelectedObjects = true;
			try
			{
				int cvsli = 0;

				// Out variables for AllTextSelInfo.
				int ihvoRoot = 0;
				int tagTextProp = 0;
				int cpropPrevious = 0;
				int ichAnchor = 0;
				int ichEnd = 0;
				int ws = 0;
				bool fAssocPrev = false;
				int ihvoEnd = 0;
				ITsTextProps ttpBogus = null;
				SelLevInfo[] rgvsli = new SelLevInfo[0];

				List<int> newSelectedObjects = new List<int>(4);
				newSelectedObjects.Add(XmlVc.FocusHvo);
				if (sel != null)
				{
					cvsli = sel.CLevels(false) - 1;
					// Main array of information retrived from sel that made combo.
					rgvsli = SelLevInfo.AllTextSelInfo(sel, cvsli,
						out ihvoRoot, out tagTextProp, out cpropPrevious, out ichAnchor, out ichEnd,
						out ws, out fAssocPrev, out ihvoEnd, out ttpBogus);
					for (int i = 0; i < cvsli; i++)
					{
						newSelectedObjects.Add(rgvsli[i].hvo);
					}
				}
				m_selectedObjects = newSelectedObjects;
				if (sel != null && !sel.IsValid)
				{
					// we wiped it out by regenerating parts of the display in our PropChanged calls! Restore it if we can.
					sel = m_rootb.MakeTextSelection(ihvoRoot, cvsli, rgvsli, tagTextProp,
						cpropPrevious, ichAnchor, ichEnd, ws, fAssocPrev, ihvoEnd, ttpBogus, true);
				}
			}

			finally
			{
				m_fInChangeSelectedObjects = false;
			}
			base.HandleSelectionChange(prootb, sel);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When we get focus, start filtering messages to catch characters
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnGotFocus(EventArgs e)
		{
			m_xmlVc.HasFocus = true;
			if (!m_selectedObjects.Contains(XmlVc.FocusHvo))
				m_selectedObjects.Add(XmlVc.FocusHvo);
			base.OnGotFocus(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.LostFocus"/> event.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnLostFocus(EventArgs e)
		{
			m_xmlVc.HasFocus = false;
			base.OnLostFocus(e);
		}
	}
}
