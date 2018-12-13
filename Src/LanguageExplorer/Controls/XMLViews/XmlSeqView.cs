// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Areas;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Xml;

namespace LanguageExplorer.Controls.XMLViews
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

	internal class XmlSeqView : RootSite
	{
		/// <summary />
		protected string m_sXmlSpec;
		/// <summary />
		protected int m_hvoRoot;
		/// <summary />
		protected int m_mainFlid;
		/// <summary>
		/// The data access that can interpret m_mainFlid of m_hvoRoot, typically a RecordListPublisher.
		/// The SDA mainly used in the view is a further decoration of this.
		/// </summary>
		protected ISilDataAccessManaged m_sdaSource;
		/// <summary />
		protected XElement m_specElement;

		/// <summary />
		protected IFwMetaDataCache m_mdc;
		bool m_fShowFailingItems; // display items that fail the condition specified in the view.
		private IFlexApp m_app;

		/// <summary>
		/// This event notifies you that the selected object changed, passing an argument from
		/// which you can directly obtain the new object. This SelectionChangedEvent may fire
		/// even if the selection moves from one occurrence of an object to another occurrence
		/// of the same object.
		/// </summary>
		public event FwSelectionChangedEventHandler SelectionChangedEvent;

		/// <summary>
		/// Initializes a new instance of the <see cref="XmlSeqView"/> class.
		/// </summary>
		public XmlSeqView() : base(null)
		{
			// TODO: Add any initialization after the InitForm call
		}

		/// <summary>
		/// Resets the tables.
		/// </summary>
		public void ResetTables()
		{
			// Don't crash if we don't have any view content yet.  See LT-7244.
			Vc?.ResetTables();
			RootBox?.Reconstruct();
		}

		/// <summary>
		/// Resets the tables.
		/// </summary>
		public void ResetTables(string sLayoutName)
		{
			// Don't crash if we don't have any view content yet.  See LT-7244.
			Vc?.ResetTables(sLayoutName);
			RootBox?.Reconstruct();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="XmlSeqView"/> class.
		/// </summary>
		public XmlSeqView(LcmCache cache, int hvoRoot, int flid, XElement configurationParametersElement, ISilDataAccessManaged sda, IFlexApp app, ICmPossibility publication)
			: base(null)
		{
			m_app = app;
			var useSda = sda;
			var decoratorSpec = XmlUtils.FindElement(configurationParametersElement, "decoratorClass");
			if (decoratorSpec != null)
			{
				// For example, this may create a DictionaryPublicationDecorator.
				useSda = (ISilDataAccessManaged)DynamicLoader.CreateObject(decoratorSpec, cache, sda, flid, publication);
			}
			InitXmlViewRootSpec(hvoRoot, flid, configurationParametersElement, useSda);
		}

		/// <summary>
		/// We need a smarter selection restorere here, to try to keep the selection on the same object.
		/// </summary>
		protected override SelectionRestorer CreateSelectionRestorer()
		{
			return new XmlSeqSelectionRestorer(this, Cache);
		}

		/// <summary>
		/// Gets the vc.
		/// </summary>
		protected internal XmlVc Vc { get; protected set; }

		private void InitXmlViewRootSpec(int hvoRoot, int flid, XElement element, ISilDataAccessManaged sda)
		{
			m_hvoRoot = hvoRoot;
			m_mainFlid = flid;
			m_sdaSource = sda;
			Debug.Assert(element != null, "Creating an XMLView with null spec");
			m_specElement = element;
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			if (disposing)
			{
				if (!string.IsNullOrEmpty(m_currentSubscriptionString))
				{
					Subscriber.Unsubscribe(m_currentSubscriptionString, ShowFailingItemsForTool_Changed);
				}
			}

			base.Dispose(disposing);

			Vc = null;
			m_mdc = null;
			m_sXmlSpec = null;
			m_specElement = null;
		}

		/// <summary>
		/// Causes XMLViews to be editable by default.
		/// </summary>
		public new static Color DefaultBackColor => SystemColors.Window;

		/// <summary>
		/// Reset the main object being shown.
		/// </summary>
		public void ResetRoot(int hvoRoot)
		{
			m_hvoRoot = hvoRoot;
			RootBox.SetRootObject(m_hvoRoot, Vc, RootFrag, m_styleSheet);
			RootBox.Reconstruct();
		}

		/// <summary>
		/// Magic number ALWAYS used for root fragment in this type of view.
		/// </summary>
		public int RootFrag => 2;

		#region overrides
		/// <summary>
		/// Override this method in your subclass.
		/// It should make a root box and initialize it with appropriate data and
		/// view constructor, etc.
		/// </summary>
		public override void MakeRoot()
		{
			if (m_cache == null || DesignMode)
			{
				return;
			}

			base.MakeRoot();

			var fEditable = XmlUtils.GetOptionalBooleanAttributeValue(m_specElement, "editable", true);
			var toolChoice = PropertyTable.GetValue<string>(AreaServices.ToolChoice);
			m_fShowFailingItems = PropertyTable.GetValue("ShowFailingItems-" + toolChoice, false);
			//m_xmlVc = new XmlVc(m_xnSpec, Table); // possibly reinstate for old approach?
			// Note: we want to keep this logic similar to RecordDocView.GetLayoutName(), except that here
			// we do NOT want to use the layoutSuffix, though it may be specified so that it can be
			// used when the configure dialog handles a shared view.
			string sLayout = null;
			var sProp = XmlUtils.GetOptionalAttributeValue(m_specElement, "layoutProperty", null);
			if (!string.IsNullOrEmpty(sProp))
			{
				sLayout = PropertyTable.GetValue<string>(sProp);
			}
			if (string.IsNullOrEmpty(sLayout))
			{
				sLayout = XmlUtils.GetMandatoryAttributeValue(m_specElement, "layout");
			}
			var sda = GetSda();
			Vc = new XmlVc(sLayout, fEditable, this, m_app, m_fShowFailingItems ? null : ItemDisplayCondition, sda) {IdentifySource = true};
			ReadOnlyView = !fEditable;
			if (!fEditable)
			{
				RootBox.MaxParasToScan = 0;
			}
			Vc.Cache = m_cache;
			Vc.MainSeqFlid = m_mainFlid;

			RootBox.DataAccess = sda;
			Vc.DataAccess = sda;

			RootBox.SetRootObject(m_hvoRoot, Vc, RootFrag, m_styleSheet);
		}

		private ISilDataAccess GetSda()
		{
			return m_sdaSource;
		}

		private XElement ItemDisplayCondition => m_specElement.Element("elementDisplayCondition");

		/// <summary>
		/// Receives the broadcast message "JumpToRecord" before RecordList
		/// (because this is the active Control?), so we can see if we
		/// need to display a failure message.
		/// </summary>
		public bool OnJumpToRecord(object argument)
		{
			Publisher.Publish("CheckJump", argument);
			return false; // I don't want to be seen as handling this!
		}

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public override void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			base.InitializeFlexComponent(flexComponentParameters);

			m_currentSubscriptionString = "ShowFailingItems-" + PropertyTable.GetValue<string>(AreaServices.ToolChoice);
			Subscriber.Subscribe(m_currentSubscriptionString, ShowFailingItemsForTool_Changed);
		}

		private string m_currentSubscriptionString = string.Empty;
		private void ShowFailingItemsForTool_Changed(object newValue)
		{
			var fShowFailingItems = PropertyTable.GetValue(m_currentSubscriptionString, false);
			if (fShowFailingItems == m_fShowFailingItems)
			{
				return;
			}
			m_fShowFailingItems = fShowFailingItems;
			Vc.MakeRootCommand(this, m_fShowFailingItems ? null : ItemDisplayCondition);
			try
			{
				EditingHelper.DefaultCursor = Cursors.WaitCursor;
				using (new WaitCursor(TopLevelControl))
				{
					RootBox.Reconstruct();
					Invalidate();
					Update(); // most of the time is the painting, we want the watch cursor till it's done.
				}
			}
			finally
			{
				EditingHelper.DefaultCursor = null;
			}
		}

		/// <summary>
		/// Notifies the site that something about the selection has changed.
		/// </summary>
		protected override void HandleSelectionChange(IVwRootBox prootb, IVwSelection vwselNew)
		{
			base.HandleSelectionChange(prootb, vwselNew);
			if (vwselNew == null)
			{
				return;
			}
			var clev = vwselNew.CLevels(false);		// anchor
			var clevEnd = vwselNew.CLevels(true);
			if (clev < 2 || clevEnd < 2)
			{
				return; // paranoia
			}
			int hvoRoot, tag, ihvo, ihvoEnd, cpropPrevious;
			IVwPropertyStore vps;
			vwselNew.PropInfo(true, clevEnd - 1, out hvoRoot, out tag, out ihvoEnd, out cpropPrevious, out vps);
			vwselNew.PropInfo(false, clev - 1, out hvoRoot, out tag, out ihvo, out cpropPrevious, out vps);
			// Give up if the selection doesn't indicate any top-level object; I think this can happen with pictures.
			// selection larger than a top-level object, maybe select all, side effects are confusing.
			if (ihvo != ihvoEnd || ihvo < 0)
			{
				return;
			}
			if (hvoRoot == 0)
			{
				return;
			}
			Debug.Assert(hvoRoot == m_hvoRoot);
			var hvoObjNewSel = m_sdaSource.get_VecItem(hvoRoot, tag, ihvo);
			if (hvoObjNewSel != 0)
			{
				// Notify any delegates that the selection of the main object in the vector
				// may have changed.
				SelectionChangedEvent?.Invoke(this, new FwObjectSelectionEventArgs(hvoObjNewSel, ihvo));
			}
		}
		#endregion

		/// <summary>
		/// Gets the object count.
		/// </summary>
		public int ObjectCount => m_sdaSource.get_VecSize(m_hvoRoot, m_mainFlid);

		#region printing
		/// <summary>
		/// If help is available for the print dialog, set ShowHelp to true,
		/// and add an event handler that can display some help.
		/// </summary>
		protected override void SetupPrintHelp(PrintDialog dlg)
		{
			base.SetupPrintHelp(dlg);
			dlg.AllowSelection = RootBox.Selection != null;
		}

		///<summary>
		/// Print method for printing one record (e.g. from RecordEditView)
		/// in Document View.
		///</summary>
		public void PrintFromDetail(PrintDocument printDoc, int mainObjHvo)
		{
			var oldSda = RootBox.DataAccess;
			RootBox.DataAccess = CachePrintDecorator(m_sdaSource, m_hvoRoot, m_mainFlid, new[] {mainObjHvo});
			base.Print(printDoc);
			RootBox.DataAccess = oldSda;
		}

		/// <summary>
		/// Print method
		/// </summary>
		public override void Print(PrintDocument printDoc)
		{
			ISilDataAccess oldSda = null;
			var fPrintSelection = (printDoc.PrinterSettings.PrintRange == PrintRange.Selection);
			if (fPrintSelection)
			{
				oldSda = RootBox.DataAccess;
				var sel = RootBox.Selection;
				var clev = sel.CLevels(true);
				int hvoObj, tag, ihvoEnd, ihvoAnchor, cpropPrevious;
				IVwPropertyStore vps;
				sel.PropInfo(true, clev - 1, out hvoObj, out tag, out ihvoEnd, out cpropPrevious, out vps);
				clev = sel.CLevels(false);
				sel.PropInfo(false, clev - 1, out hvoObj, out tag, out ihvoAnchor, out cpropPrevious, out vps);
				var originalObjects = m_sdaSource.VecProp(m_hvoRoot, m_mainFlid);
				var ihvoMin = Math.Min(ihvoEnd, ihvoAnchor);
				var ihvoLim = Math.Max(ihvoEnd, ihvoAnchor) + 1;
				var selectedObjects = new int[ihvoLim - ihvoMin];
				for (var i = 0; i < selectedObjects.Length; i++)
				{
					selectedObjects[i] = originalObjects[i + ihvoMin];
				}
				RootBox.DataAccess = CachePrintDecorator(m_sdaSource, m_hvoRoot, m_mainFlid, selectedObjects);
			}
			base.Print(printDoc);
			if (fPrintSelection)
			{
				RootBox.DataAccess = oldSda;
			}
		}

		///<summary>
		/// Decorate an ISilDataAccessManaged object with another layer limiting objects to print.
		///</summary>
		public static ISilDataAccessManaged CachePrintDecorator(ISilDataAccessManaged oldSda, int rootHvo, int mainFlid, int[] selectedObjects)
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