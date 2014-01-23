// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: XmlVc.cs
// Responsibility: WordWorks

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Globalization;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.Application.ApplicationServices;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils.ComTypes;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// XmlVc is a view constructor class whose behavior is controlled by an XML document,
	/// described in XmlView.
	/// Note: the current version won't handle field IDs that are zero.
	/// </summary>
	public class XmlVc : FwBaseVc
	{
		// The specification node that contains fragments (in the old approach). Null in new.
		/// <summary></summary>
		protected XmlNode m_xnSpec;

		// Summary of how we use frag IDs.

		// Reserved IDs.
		// The value 1 (kRootFragId) is a reserved fragment ID used for the root of what is generated
		// from XML (there may be a sequence and possibly columns outside this, however).
		// The value 2 is reserved for the top-level sequence of things shown in a document
		// or browse view. When this is the root fragment, each item in m_mainItems is
		// displayed using fragment 1.

		// New approach.
		// When an element does not have a "frag" attribute, we generate a Pair
		// (frag, caller) in m_idToDisplayInfo. This allows us to retrieve from the fragID
		// the two key XmlNodes involved in invoking whatever property it was when we need
		// them to display the resulting thing.

		// Map from fragment ID to an object that will actually be used in Display
		// or some similar method. Most commonly, for Display, it is a DisplayCommand object,
		// but all that matters is that the thing that passes the frag id to the vwenv
		// and the method that is invoked as a result agree.
		internal Dictionary<int, DisplayCommand> m_idToDisplayCommand = new Dictionary<int, DisplayCommand>();
		// Map from DisplayInfo to int: used to avoid adding duplicate items
		// to m_idToDisplayInfo.
		internal Dictionary<DisplayCommand, int> m_displayCommandToId = new Dictionary<DisplayCommand, int>();
		// Cache for results of asking the string table to GetStringsFromListNode on a particular node.
		internal Dictionary<XmlNode, string[]> m_StringsFromListNode = new Dictionary<XmlNode, string[]>();

		/// <summary>
		/// Set this true (currently in XmlDocView and XmlDocItemView) to enable identifying the source
		/// (that is, the XML layout and part)
		/// that most immediately caused particular strings to be put in the display.
		/// </summary>
		public bool IdentifySource { get; set; }

		/// <summary></summary>
		public const int kRootFragId = 1;

		int m_nextID = 1378; // next ID to allocate.
		/// <summary></summary>
		protected internal LayoutCache m_layouts;

		readonly bool m_fEditable = true; // set false to disable editing at top level.

		/// <summary>
		/// If this flag is set, the VC will not create any pictures, and will answer null to routines
		/// that request them. This is done in cases where the VC is needed only for collector environments
		/// which don't need real pictures.
		/// </summary>
		public bool SuppressPictures { get; set; }

		/// <summary></summary>
		protected IFwMetaDataCache m_mdc;
		/// <summary>
		/// Usually the SDA of m_cache, but sometimes decorated.
		/// </summary>
		protected ISilDataAccess m_sda;
		/// <summary></summary>
		protected int m_mainFlid; // Main flid used in XmlSeqView.
		/// <summary></summary>
		protected StringTable m_stringTable;
		/// <summary></summary>
		protected string m_rootLayoutName; // name of part to use for root.
		// Current writing system id being used in multilingual fragment.
		// Some methods that refer to this variable are static, so it must be static also.
		/// <summary></summary>
		static protected IWritingSystem s_qwsCurrent = null;
		/// <summary></summary>
		static protected int s_cwsMulti = 0;	// count of current ws alternatives.
		/// <summary></summary>
		static protected string s_sMultiSep = null;
		/// <summary></summary>
		static protected bool s_fMultiFirst = false;
		// This may be set to perform logging.
		ISimpleLogger m_logStream;
		IPicture m_CommandIcon;

		bool m_fHasFocus; // true to treat the view as having focus.
		int m_cHeightEstimates = 0; // how many times has EstimateHeight been called for this width?
		int m_dxWidthForEstimate = 0;
		int m_heightEstimate; // Our current best height estimate (0 if never estimated)
		// used by <savehvo>.  This was first used in conjunction with displaying minor entries
		// related to an entry or sense, organized by type of minor entry.
		Stack<int> m_stackHvo = new Stack<int>();

		// The next two variables are saved in the constructor to be used in ResetTables(layoutName)
		// when the layout name changes.
		XmlNode m_conditionNode = null;
		SimpleRootSite m_rootSite = null;
		internal IApp m_app;

		/// <summary>
		/// This is used to interpret the "reversal" writing system.  This is needed to ensure
		/// that definitions, parts of speech, etc. are displayed in the proper writing system.
		/// </summary>
		protected int m_wsReversal = 0;

		/// <summary>
		/// This flags that part refs flagged with singlegraminfofirst, typically
		/// grammatical information (MSA) should not be displayed.  See LT-9663 for explanation of what is being done
		/// with this feature.
		/// </summary>
		internal bool ShouldIgnoreGramInfo { get; set; }

		/// <summary>
		/// This stores a delayed number set up because of the "numDelay" attribute.
		/// </summary>
		ITsString m_tssDelayedNumber = null;

		/// <summary>
		/// Get or set a flag that an object number should not be displayed until the first line
		/// (paragraph) of that object is displayed.
		/// </summary>
		internal bool DelayNumFlag { get; set; }

		// This groups senses by placing graminfo before the number, and omitting it if the same as the
		// previous sense in the entry.  This isn't yet supported by the UI, but may well be requested in
		// the future.  (See LT-9663.)
		///// <summary>
		///// The object id of an MSA which was displayed before the previous sense number.  Look for
		///// "graminfobeforenumber"
		///// </summary>
		//int m_hvoGroupedValue = 0;

		/// <summary>
		/// Maintain a stack of &lt;part ref="..."&gt; nodes, so that we can still access a
		/// "caller" even when the chain has been broken in method calls into the views code and
		/// back out.  (part of the implementation of LT-10542)
		/// </summary>
		internal readonly List<XmlNode> m_stackPartRef = new List<XmlNode>();

		/// <summary>
		/// List of guids used to filter/sort in DisplayVec().
		/// </summary>
		Dictionary<Guid, List<LexReferenceInfo>> m_mapGuidToReferenceInfo;
		Dictionary<Guid, ItemTypeInfo> m_mapGuidToComplexRefInfo;
		Dictionary<Guid, ItemTypeInfo> m_mapGuidToVariantRefInfo;

		private readonly Guid m_unspecComplexFormType;
		private readonly Guid m_unspecVariantType;

		// This is the new constructor, where we find parts in the master inventory.
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="XmlVc"/> class.
		/// </summary>
		/// <param name="stringTable">The string table.</param>
		/// <param name="rootLayoutName">Name of the root layout.</param>
		/// <param name="fEditable">if set to <c>true</c> [f editable].</param>
		/// <param name="rootSite">The root site.</param>
		/// <param name="app">The application.</param>
		/// <param name="sda">Data access (possibly a decorator for the rootSite's cache's one)</param>
		/// ------------------------------------------------------------------------------------
		public XmlVc(StringTable stringTable, string rootLayoutName, bool fEditable,
			SimpleRootSite rootSite, IApp app, ISilDataAccess sda)
			: this(stringTable, rootLayoutName, fEditable, rootSite, app, null, sda)
		{
		}

		// This is a variant which can take a condition element to control which items in a list display.
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="XmlVc"/> class.
		/// </summary>
		/// <param name="stringTable">The string table.</param>
		/// <param name="rootLayoutName">Name of the root layout.</param>
		/// <param name="fEditable">if set to <c>true</c> [f editable].</param>
		/// <param name="rootSite">The root site.</param>
		/// <param name="app">The application.</param>
		/// <param name="condition">The condition.</param>
		/// <param name="sda">Data access (possibly a decorator for the rootSite's cache's one)</param>
		/// ------------------------------------------------------------------------------------
		public XmlVc(StringTable stringTable, string rootLayoutName, bool fEditable,
			SimpleRootSite rootSite, IApp app, XmlNode condition, ISilDataAccess sda) : this(stringTable)
		{
			m_rootLayoutName = rootLayoutName;
			m_fEditable = fEditable;
			MApp = app;
			m_sda = sda; // BEFORE we make the root command, which uses it.
			MakeRootCommand(rootSite, condition);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is another new constructor, for using the new approach without a single
		/// top-level layout name, such as browse view.
		/// Initializes a new instance of the <see cref="XmlVc"/> class.
		/// </summary>
		/// <param name="stringTable">The string table.</param>
		/// ------------------------------------------------------------------------------------
		public XmlVc(StringTable stringTable)
		{
			StringTbl = stringTable;

			m_unspecComplexFormType = XmlViewsUtils.GetGuidForUnspecifiedComplexFormType();
			m_unspecVariantType = XmlViewsUtils.GetGuidForUnspecifiedVariantType();
		}

		/// <summary>
		/// Indicates whether the VC should draw the view as if it has focus.
		/// Currently this controls whether command icons are shown.
		/// </summary>
		internal bool HasFocus
		{
			get
			{
				return m_fHasFocus;
			}
			set
			{
				m_fHasFocus = value;
			}
		}

		/// <summary>
		/// These maps support the capability of duplicating certain fields (e.g., Subentries)
		/// in a configuration dialog, and displaying different types of object differently.
		/// When this is done, the part ref is modified by adding a list of guids, and only
		/// the objects linked in a particular way to the selected options are included
		/// in this particular instance of displaying the field.
		/// </summary>
		public bool ShouldFilterByGuid
		{
			get
			{
				return m_mapGuidToReferenceInfo != null || m_mapGuidToComplexRefInfo != null ||
					   m_mapGuidToVariantRefInfo != null;
			}
		}

		/// <summary>
		/// Gets or sets the reversal writing system handle.
		/// </summary>
		/// <value>The reversal writing system handle.</value>
		public int ReversalWs
		{
			get
			{
				return m_wsReversal;
			}

			set
			{
				m_wsReversal = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the log stream.
		/// </summary>
		/// <value>The log stream.</value>
		/// ------------------------------------------------------------------------------------
		public ISimpleLogger LogStream
		{
			get
			{
				return m_logStream;
			}
			set
			{
				m_logStream = value;
			}
		}

		internal void MakeRootCommand(SimpleRootSite rootSite, XmlNode condition)
		{
			DisplayCommand dc;
			if (m_fEditable)
			{
				dc = new RootDisplayCommand(m_rootLayoutName, rootSite);
				Debug.Assert(condition == null, "condition currently only supported for read-only views");
			}
			else
			{
				if (condition == null)
					dc = new ReadOnlyRootDisplayCommand(m_rootLayoutName, rootSite);
				else
					dc = new ReadOnlyConditionalRootDisplayCommand(m_rootLayoutName, rootSite, condition, m_sda);
			}
			m_idToDisplayCommand[kRootFragId] = dc;
			m_rootSite = rootSite;
			m_conditionNode = condition;
		}

		/// <summary>
		/// Reset the tables (typically after changing the layout definitions).
		/// Discard all display commands except the root one.
		/// </summary>
		public void ResetTables()
		{
			ResetTables(m_rootLayoutName);
			m_cFields = 0;	// also force reload of custom field maps.
		}

		/// <summary>
		/// Reset the tables (typically after changing the layout definitions).
		/// Discard all display commands except the root one.
		/// </summary>
		/// <param name="rootLayoutName"></param>
		public void ResetTables(string rootLayoutName)
		{
			DisplayCommand rootCommand;
			if (m_rootLayoutName != rootLayoutName)
			{
				m_rootLayoutName = rootLayoutName;
				MakeRootCommand(m_rootSite, m_conditionNode);
			}
			rootCommand = m_idToDisplayCommand[kRootFragId];
			m_idToDisplayCommand.Clear();
			m_idToDisplayCommand[kRootFragId] = rootCommand;
			m_displayCommandToId.Clear();
			m_layouts = new LayoutCache(m_mdc, m_cache.ProjectId.Name, MApp,
				m_cache.ProjectId.ProjectFolder);
			// We could reset the next id, but that's arbitrary, so why bother?
		}

		/// <summary>
		/// The field of the root object that contains the main, lazy list.
		/// </summary>
		public int MainSeqFlid
		{
			get
			{
				return m_mainFlid;
			}
			set
			{
				m_mainFlid = value;
			}
		}

		Set<int> m_lastLoadRecent = new Set<int>();


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Displays the specified vwenv.
		/// </summary>
		/// <param name="vwenv">The vwenv.</param>
		/// <param name="hvo">The hvo.</param>
		/// <param name="fragId">The frag id.</param>
		/// ------------------------------------------------------------------------------------
		public override void Display(IVwEnv vwenv, int hvo, int fragId)
		{
			// If there isn't a current object, most if not all types will fail.
			// This can happen, for example, if the root of the whole view is nothing,
			// or if we follow an owning atomic property that is empty.
			if (hvo == 0)
				return;

			if (fragId == 2)
			{
				// This number reserved for the main lazy sequence of an XmlSeqView.
				Debug.Assert(m_mainFlid != 0); // XmlSeqView must supply a main flid.
				// For displaying reversal indexes, we need to know the reversal index
				// language (writing system).
				var obj = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
				SetReversalWritingSystemFromRootObject(obj);
				vwenv.AddLazyVecItems(m_mainFlid, this, kRootFragId);
				return;
			}

			DisplayCommand dispCommand = m_idToDisplayCommand[fragId];
			dispCommand.PerformDisplay(this, fragId, hvo, vwenv);
		}

		internal void SetReversalWritingSystemFromRootObject(object obj)
		{
			if (obj is IReversalIndex)
				m_wsReversal = m_cache.ServiceLocator.WritingSystemManager.GetWsFromStr((obj as IReversalIndex).WritingSystem);
		}

		internal bool CanGetMainCallerDisplayCommand(int fragId, out MainCallerDisplayCommand cmd)
		{
			DisplayCommand tmpCmd;
			cmd = null;
			if (!m_idToDisplayCommand.TryGetValue(fragId, out tmpCmd))
				return false;
			cmd = tmpCmd as MainCallerDisplayCommand;
			return cmd != null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given the XmlNode that invoked a display of a related object, and the node that
		/// is looked up as the main way to display the object given the layout name,
		/// come up with the node to actually use to display it.
		/// </summary>
		/// <param name="node">The node.</param>
		/// <param name="callingFrag">The calling frag.</param>
		/// <param name="layouts">The layouts.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static XmlNode GetDisplayNodeForChild(XmlNode node, XmlNode callingFrag, LayoutCache layouts)
		{
			if (node == null)
				node = callingFrag; // didn't get anything, hope the calling node has useful children
			else if (callingFrag != null && callingFrag.ChildNodes.Count > 0)
			{
				// The calling node is attempting to provide info about how to
				// display items, for example, a seq node that specifies how to display
				// items in the sequence.
				// But, we also got a node from the layout.
				// If it's just a part, we don't do unification, just allow the caller to override.
				if (node.Name == "layout")
				{
					node = layouts.LayoutInventory.GetUnified(node, callingFrag);
				}
				else
					node = callingFrag; // treat as complete replacement.
			}
			return node;
		}

		/// <summary>
		/// Get the node that describes how to display the object hvo using the specified
		/// layout name.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="layoutName"></param>
		/// <param name="fIncludeLayouts">if true, include layouts, otherwise just parts.</param>
		/// <returns></returns>
		protected internal XmlNode GetNodeForPart(int hvo, string layoutName, bool fIncludeLayouts)
		{
			return GetNodeForPart(hvo, layoutName, fIncludeLayouts, m_sda,
				m_layouts);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the node for part.
		/// </summary>
		/// <param name="hvo">The hvo.</param>
		/// <param name="layoutName">Name of the layout.</param>
		/// <param name="fIncludeLayouts">if set to <c>true</c> [f include layouts].</param>
		/// <param name="sda">The sda.</param>
		/// <param name="layouts">The layouts.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static XmlNode GetNodeForPart(int hvo, string layoutName, bool fIncludeLayouts,
			ISilDataAccess sda, LayoutCache layouts)
		{
			int clsid = sda.get_IntProp(hvo, CmObjectTags.kflidClass);
			if (clsid == 0)
				throw new Exception("Trying to get part node for invalid object " + hvo);
			return layouts.GetNode(clsid, layoutName, fIncludeLayouts);
		}

		/// <summary>
		/// Get the node that specifies how to lay out an object of the specified class
		/// using the specified layoutName.
		/// </summary>
		/// <param name="clsid"></param>
		/// <param name="layoutName"></param>
		/// <param name="fIncludeLayouts"></param>
		/// <returns></returns>
		public XmlNode GetNodeForPart(string layoutName, bool fIncludeLayouts, int clsid)
		{
			return m_layouts.GetNode(clsid, layoutName, fIncludeLayouts);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the layout cache.
		/// </summary>
		/// <value>The layout cache.</value>
		/// ------------------------------------------------------------------------------------
		public LayoutCache LayoutCache
		{
			get
			{
				return m_layouts;
			}
		}

		// Magic number we set up a dependency on to achieve redrawing of stuff that is visible
		// when we have focus.
		internal const int FocusHvo = -1;

		// Magic tag used similarly
		internal const int IsObjectSelectedTag = -14987;

		// Keeps track of which objects are considered selected. Maintained by the view.
		internal List<int> SelectedObjects = new List<int>(1);

		/// <summary>
		/// Add a command icon (currently the blue circle icon). Visibility is only while the pane has
		/// focus. By default, it is also only visible while the current object is selected.
		/// The other behavior currently possible is always visible while focused. To get that,
		/// set visibility="focused"
		/// </summary>
		/// <param name="frag"></param>
		/// <param name="vwenv"></param>
		/// <param name="hvo"></param>
		private void AddCommandIcon(XmlNode frag, IVwEnv vwenv, int hvo)
		{
			if (m_CommandIcon == null)
				m_CommandIcon = (IPicture)OLECvt.ToOLE_IPictureDisp(ResourceHelper.BlueCircleDownArrowForView);

			string condition = XmlUtils.GetOptionalAttributeValue(frag, "visibility", "objectSelected");
			int hvoDepends = hvo;
			switch (condition)
			{
				case "objectSelected":
					break; // leave as hvo
				case "focused":
					hvoDepends = FocusHvo;
					break;
				default:
					throw new ConfigurationException("visibility of commandIcon must be 'objectSelected' or 'focused' but was " + condition);
			}
			// This phony dependency allows us to regenerate a minimal part of the view when the HasFocus property changes,
			// or when there is a change in the set of selected objects for which icons should be visible.
			vwenv.NoteDependency(new int[] { hvoDepends }, new int[] { IsObjectSelectedTag }, 1);
			if (HasFocus && SelectedObjects.Contains(hvoDepends))
			{
				vwenv.set_IntProperty((int)FwTextPropType.ktptBackColor, (int)FwTextPropVar.ktpvDefault,
					0xffffff);
				vwenv.AddPicture(m_CommandIcon, 0, 0, 0);
			}
		}
		/// <summary>
		/// Select an appropriate picture from the list for this fragment.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="val"></param>
		/// <param name="frag"></param>
		/// <returns></returns>
		public override IPicture DisplayPicture(IVwEnv vwenv, int hvo, int tag, int val, int frag)
		{
			// It has to be obsolete, since the new system puts DisplayCommand objects in m_idToDisplayCommand,
			// not XmlNodes.
			throw new InvalidOperationException("Obsolete system use for 'DisplayPicture' method.");
		}

		// Part of a mechanism by which browse view columns
		// can force a particular WS to be used for all strings. Overridden by XmlBrowseViewBaseVc.
		internal virtual int WsForce
		{
			get { return 0; }
			set { Debug.Fail("WsForce can only be set in browse VCs."); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Display the whole of the specified sequence of collection property.  This allows
		/// the constructor to insert separators, filter or sort the list, and so forth.
		/// Called in response to calling IVwEnv::AddObjVec.
		/// That in turn is called from seq.
		/// Note that seq has some test code that determines whether to use AddObjVec or
		/// AddObjVecItems. If this method is changed to handle additional special attributes,
		/// a change to the seq case of ProcessFrag may be needed.
		/// </summary>
		/// <param name="vwenv">The vwenv.</param>
		/// <param name="hvo">The hvo.</param>
		/// <param name="flid">The flid.</param>
		/// <param name="frag">The frag.</param>
		/// ------------------------------------------------------------------------------------
		public override void DisplayVec(IVwEnv vwenv, int hvo, int flid, int frag)
		{
			if (frag <= 2)
			{
				// Should we do something special here?
				return;
			}
			new XmlVcDisplayVec(this, vwenv, hvo, flid, frag).Display(ref m_tssDelayedNumber);
		}

		/// <summary>
		/// Sort the hvo of an object along with its sort index.
		/// </summary>
		internal class HvoAndIndex
		{
			/// <summary>Hvo of the object</summary>
			internal int RefHvo { get; set; }
			/// <summary>Sort index value for the LexReference owner/subclass</summary>
			internal int Index { get; set; }
			/// <summary>
			/// Original index of the item in the list; used as a tie-breaker to make sort stable.
			/// </summary>
			internal int OriginalIndex { get; set; }
		}

		internal int[] FilterAndSortListByComplexFormType(int[] hvos, int hvoTarget)
		{
			if (hvos.Length == 0)
				return hvos;
			var objs = new List<ICmObject>(hvos.Length);
			var repo = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>();
			objs.AddRange(hvos.Select(hvo => repo.GetObject(hvo)));
			if (objs[0].ClassID == LexReferenceTags.kClassId && m_mapGuidToReferenceInfo != null)
			{
				// First, filter the list of references according to the content of
				// m_mapGuidToReferenceInfo.
				var refs = new List<HvoAndIndex>();
				foreach (var obj in objs)
				{
					var lr = obj as ILexReference;
					if (lr == null || lr.TargetsRS.Count < 2)
						continue;
					List<LexReferenceInfo> lris;
					if (!m_mapGuidToReferenceInfo.TryGetValue(obj.Owner.Guid, out lris))
						continue;
					int originalIndex = 0;
					foreach (var lri in lris)
					{
						switch (lri.SubClass)
						{
							case LexReferenceInfo.TypeSubClass.Forward:
								if (lr.TargetsRS[0].Hvo != hvoTarget)
									continue;
								break;
							case LexReferenceInfo.TypeSubClass.Reverse:
								if (lr.TargetsRS[0].Hvo == hvoTarget)
									continue;
								break;
						}
						refs.Add(new HvoAndIndex {RefHvo = lr.Hvo, Index = lri.Index, OriginalIndex = originalIndex++});
					}
				}
				// Now, sort the list according to the order given by the information stored in
				// m_mapGuidToReferenceInfo, and return the new array of hvos.
				refs.Sort(SortHvoByIndex);
				return refs.Select(t => t.RefHvo).ToArray();
			}
			if (objs[0].ClassID == LexEntryRefTags.kClassId &&
				(m_mapGuidToComplexRefInfo != null || m_mapGuidToVariantRefInfo != null))
			{
				int originalIndex = 0;
				var refs = (from ler in objs.OfType<ILexEntryRef>()
							let info = GetTypeInfoForEntryRef(ler)
							where info != null
							select new HvoAndIndex {RefHvo = ler.Hvo, Index = info.Index, OriginalIndex = originalIndex++}).ToList();
				// Now, sort the list according to the order given by the information stored in
				// m_mapGuidToComplexRefInfo or m_mapGuidToVariantRefInfo, and return the new array of hvos.
				refs.Sort(SortHvoByIndex);
				return refs.Select(t => t.RefHvo).ToArray();
			}
			return hvos;
		}

		private ItemTypeInfo GetTypeInfoForEntryRef(ILexEntryRef ler)
		{
			ItemTypeInfo info = null;
			if (m_mapGuidToComplexRefInfo != null)
				info = GetTypeInfoForComplexEntryRef(ler);

			if (m_mapGuidToVariantRefInfo != null)
				info = GetTypeInfoForVariantEntryRef(ler);

			return info;
		}

		private ItemTypeInfo GetTypeInfoForVariantEntryRef(ILexEntryRef ler)
		{
			ItemTypeInfo info = null;
			if (ler.VariantEntryTypesRS.Count > 0)
			{
				foreach (var type in ler.VariantEntryTypesRS)
				{
					if (m_mapGuidToVariantRefInfo.TryGetValue(type.Guid, out info))
						break;
				}
			}
			else
			{
				m_mapGuidToVariantRefInfo.TryGetValue(m_unspecVariantType, out info);
			}
			return info;
		}

		private ItemTypeInfo GetTypeInfoForComplexEntryRef(ILexEntryRef ler)
		{
			ItemTypeInfo info = null;
			if (ler.ComplexEntryTypesRS.Count > 0)
			{
				foreach (var type in ler.ComplexEntryTypesRS)
				{
					if (m_mapGuidToComplexRefInfo.TryGetValue(type.Guid, out info))
						break;
				}
			}
			else
			{
				m_mapGuidToComplexRefInfo.TryGetValue(m_unspecComplexFormType, out info);
			}
			return info;
		}

		private static int SortHvoByIndex(HvoAndIndex x, HvoAndIndex y)
		{
			if (x.Index == y.Index)
				return x.OriginalIndex - y.OriginalIndex;
			return x.Index - y.Index;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Come up with an estimate of the height of a single item in points.
		/// The current approach is based on collecting the text for an item, and assuming no very unusual
		/// fonts or multi-paragraph layouts, estimating how many lines it will take. This is pretty approximate,
		/// but it's difficult to actually measure without having a VwEnv as a starting point.
		/// It's too expensive to do this for all items (this routine is typically called for every item in the
		/// collection, BEFORE we LoadData for any of the items). So we arrange to sample log(n) items, by
		/// only doing the calculation when the item count is a power of two.
		/// Also, we have to forget our estimtaes if the available width changes.
		/// We return an average of the items we've estimated for all items, to allow the lazy box to take
		/// advantage of a uniform height estimate.
		/// Enhance JohnT: to optimize starting up a window (as opposed to scrolling and searching), we could
		/// check that the frag matches the root property, and do everything on the first call: determine the length
		/// of the list, pick log(n) of them to measure, call the guts of LoadDataFor() to load all the data for
		/// the ones we want to estimate, and then measure all of them.
		/// Enhance SteveMc: to improve the estimate, use the font and fontsizes for the vernacular and analysis
		/// languages (if we can easily obtain that information).  The line height would be the max of the two (or
		/// more?) fonts' line heights.
		/// Enhance JohnT: we could fairly easily make the StringMeasureEnv keep track of any new paragraphs, and
		/// estimate a number of lines for each.
		/// </summary>
		/// <param name="hvo">id of object for which estimate is to be done</param>
		/// <param name="frag">fragment of data</param>
		/// <param name="dxAvailWidth">Width of data layout area in points</param>
		/// ------------------------------------------------------------------------------------
		public override int EstimateHeight(int hvo, int frag, int dxAvailWidth)
		{
			if (dxAvailWidth == m_dxWidthForEstimate)
			{
				m_cHeightEstimates++;
			}
			else
			{
				m_dxWidthForEstimate = dxAvailWidth;
				m_cHeightEstimates = 1;
			}
			// Compute whether number of previous calls is a power of 2.
			int temp = m_cHeightEstimates;
			int cPrev = -1; // count of previous estimates. (loop iterations is one more than prev count.)
			while (temp != 0)
			{
				int temp2 = temp >> 1;
				if ((temp & 1) != 0 && temp2 != 0)
				{
					// not a power of 2; use existing estimate
					return m_heightEstimate;
				}
				temp = temp2;
				cPrev++;
			}
			int newEstimate = ReallyEstimateHeight(hvo, frag, dxAvailWidth);
			// make remembered estimate a mean average
			m_heightEstimate = Math.Max(1, (m_heightEstimate * cPrev + newEstimate) / (cPrev + 1));

			return m_heightEstimate; // typical item height in points.
		}

		/// <summary>
		/// This version of height estimating is only called for a small subset of objects (log n).
		/// It is still an estimate, but we want to make it a fairly good estimate. To do this we run
		/// a display of the object with a CollectorEnv that measures the actual widths of all the strings.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="frag"></param>
		/// <param name="dsAvailWidth"></param>
		/// <returns></returns>
		private int ReallyEstimateHeight(int hvo, int frag, int dsAvailWidth)
		{
			using (System.Windows.Forms.Form form = new System.Windows.Forms.Form())
			{
				using (Graphics g = form.CreateGraphics())
				{
					using (Font font = new Font(MiscUtils.StandardSansSerif, 12.0F))
					{
						StringMeasureEnv env = new StringMeasureEnv(null, m_sda,
							hvo, g, font);
						this.Display(env, hvo, frag);
						int lines = env.Width / dsAvailWidth + 1;
						int lineHeight = Convert.ToInt32(font.GetHeight(g));
						return lines * lineHeight * 72 / Convert.ToInt32(g.DpiY);
					}
				}
			}
		}

		/// <summary>
		/// Get the value of the named property of the specified object.
		/// </summary>
		/// <param name="attrName"></param>
		/// <param name="frag"></param>
		/// <param name="hvo"></param>
		/// <returns></returns>
		private int GetIntFromNamedProp(string attrName, XmlNode frag, int hvo)
		{
			string propName = XmlUtils.GetManditoryAttributeValue(frag, attrName);
			var parts = propName.Split('.');
			int flid;
			if (parts.Length == 2)
			{
				flid = m_sda.MetaDataCache.GetFieldId(parts[0], parts[1], false);
			}
			else
			{
				int clsid = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo).ClassID;
				flid = m_sda.MetaDataCache.GetFieldId2(clsid, propName, true);
			}
			return m_sda.get_IntProp(hvo, flid);
		}

		/// <summary>
		/// Process a fragment's children against multiple writing systems.
		/// </summary>
		/// <param name="frag"></param>
		/// <param name="vwenv"></param>
		/// <param name="hvo"></param>
		internal void ProcessMultiLingualChildren(XmlNode frag, IVwEnv vwenv, int hvo)
		{
			string sWs = XmlUtils.GetOptionalAttributeValue(frag, "ws");
			if (sWs == null)
				return;

			Debug.Assert(s_qwsCurrent == null);
			Debug.Assert(s_cwsMulti == 0);
			try
			{
				HashSet<int> wsIds = WritingSystemServices.GetAllWritingSystems(m_cache, frag, s_qwsCurrent, 0, 0);
				s_cwsMulti = wsIds.Count;
				if (s_cwsMulti > 1)
					s_sMultiSep = XmlUtils.GetOptionalAttributeValue(frag, "sep");
				s_fMultiFirst = true;
				foreach (int WSId in wsIds)
				{
					s_qwsCurrent = m_cache.ServiceLocator.WritingSystemManager.Get(WSId);
					ProcessChildren(frag, vwenv, hvo);
				}
			}
			finally
			{
				// Make sure these are reset, no matter what.
				s_qwsCurrent = null;
				s_cwsMulti = 0;
				s_sMultiSep = null;
				s_fMultiFirst = false;
			}
		}

		internal static void DisplayWsLabel(IWritingSystem qws, IVwEnv vwenv, FdoCache cache)
		{
			if (qws == null)
				return;

			string sLabel = qws.Abbreviation;
			if (sLabel == null)
				sLabel = qws.Id;
			if (sLabel == null)
				sLabel = XMLViewsStrings.ksUNK;
			ITsStrFactory tsf = cache.TsStrFactory;
			ITsIncStrBldr tisb = tsf.GetIncBldr();
			tisb.SetIntPropValues((int)FwTextPropType.ktptWs,
				0, cache.ServiceLocator.WritingSystemManager.UserWs);
			tisb.SetIntPropValues((int)FwTextPropType.ktptEditable,
				(int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
			tisb.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Writing System Abbreviation");
			tisb.Append(sLabel);
			tisb.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, null);
			tisb.Append(" ");
			vwenv.AddString(tisb.GetString());
		}

		internal static void DisplayMultiSep(XmlNode frag, IVwEnv vwenv, FdoCache cache)
		{
			string sWs = XmlUtils.GetOptionalAttributeValue(frag, "ws");
			if (sWs != null && sWs == "current")
			{
				if (!s_fMultiFirst && s_sMultiSep != null && s_sMultiSep != "")
				{
					ITsStrFactory tsf = cache.TsStrFactory;
					int wsUi = cache.WritingSystemFactory.UserWs;
					ITsString tss = tsf.MakeString(s_sMultiSep, wsUi);
					vwenv.AddString(tss);
				}
				else
				{
					s_fMultiFirst = false;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process the nodes in parts, as ProcessFrag treats them before calling
		/// ProcessChildren.
		/// Refactor JohnT: how can we capture what is common here??
		/// </summary>
		/// <param name="parts">The parts.</param>
		/// <param name="vwenv">The vwenv.</param>
		/// <param name="hvo">The hvo.</param>
		/// ------------------------------------------------------------------------------------
		protected void OpenOuterParts(List<XmlNode> parts, IVwEnv vwenv, int hvo)
		{
			foreach (XmlNode frag in parts)
			{
				switch (frag.Name)
				{
					case "para":
						{
							ProcessProperties(frag, vwenv);
							vwenv.OpenParagraph();
							break;
						}
					case "concpara":
						{
							OpenConcordanceParagraph(vwenv, hvo, frag);
							break;
						}
					case "div":
						{
							ProcessProperties(frag, vwenv);
							vwenv.OpenDiv();
							break;
						}
					case "innerpile":
						{
							ProcessProperties(frag, vwenv);
							vwenv.OpenInnerPile();
							break;
						}
					case "span":
						{
							ProcessProperties(frag, vwenv);
							vwenv.OpenSpan();
							break;
						}
					case "table":
					case "header":
					case "footer":
					case "body":
					case "row":
					case "cell":
					case "headercell":
						throw new Exception("table parts not yet supported in OpenOuterParts");
					default:
						throw new Exception("unexpected part in OpenOuterParts");
				}
			}
		}

		private void OpenConcordanceParagraph(IVwEnv vwenv, int hvo, XmlNode frag)
		{
			ProcessProperties(frag, vwenv);
			int dmpAlign = XmlUtils.GetMandatoryIntegerAttributeValue(frag, "align");
			vwenv.OpenConcPara(GetIntFromNamedProp("min", frag, hvo),
								GetIntFromNamedProp("lim", frag, hvo), VwConcParaOpts.kcpoDefault, dmpAlign);
		}
		/// <summary>
		/// Process the nodes in parts, as ProcessFrag treats them after calling
		/// ProcessChildren, in reverse order.
		/// Refactor JohnT: how can we capture what is common here??
		/// </summary>
		/// <param name="parts"></param>
		/// <param name="vwenv"></param>
		protected void CloseOuterParts(List<XmlNode> parts, IVwEnv vwenv)
		{
			for (int i = parts.Count - 1; i >= 0; --i)
			{
				XmlNode frag = parts[i];
				switch (frag.Name)
				{
					case "para":
						{
							vwenv.CloseParagraph();
							break;
						}
					case "concpara":
						{
							vwenv.CloseParagraph();
							break;
						}
					case "div":
						{
							vwenv.CloseDiv();
							break;
						}
					case "innerpile":
						{
							vwenv.CloseInnerPile();
							break;
						}
					case "span":
						{
							vwenv.CloseSpan();
							break;
						}
					case "table":
					case "header":
					case "footer":
					case "body":
					case "row":
					case "cell":
					case "headercell":
						throw new Exception("table parts not yet supported in CloseOuterParts");
					default:
						throw new Exception("unexpected part in CloseOuterParts");
				}
			}
		}

		/// <summary>
		/// Any of these nodes can be skipped in processing, such as that for the ProcessFrag method.
		/// </summary>
		/// <param name="frag"></param>
		/// <returns></returns>
		public static bool CanSkipNode(XmlNode frag)
		{
			return (frag is XmlComment || frag.Name == "properties" || frag.Name == "dynamicloaderinfo");
		}

		private void AddMultipleAlternatives(IEnumerable<int> wsIds, IVwEnv vwenv, int hvo, int flid, XmlNode caller, bool fCurrentHvo)
		{
			string sep = XmlUtils.GetOptionalAttributeValue(caller, "sep", null);
			ITsString tssSep = null;
			if (sep != null)
			{
				tssSep = m_cache.TsStrFactory.MakeString(sep,
					m_cache.ServiceLocator.WritingSystemManager.UserWs);
			}
			bool fLabel = XmlUtils.GetOptionalBooleanAttributeValue(caller, "showLabels", false); // true to 'separate' using multistring labels.
			bool fFirst = true;
			foreach (int ws in wsIds)
			{
				ITsString tss = vwenv.DataAccess.get_MultiStringAlt(hvo, flid, ws);
				// An empty one doesn't even count as 'first'.
				// Enhance JohnT: this behavior could well be configurable.
				// (LT-6224) If the vwenv is a TestCollector and noting empty string dependencies
				// we need to allow the empty string to be added to the collector...don't skip it!
				if (tss.Length == 0 &&
					!((vwenv is TestCollectorEnv) &&
					(vwenv as TestCollectorEnv).NoteEmptyDependencies))
				{
					continue;
				}
				if (fFirst)
				{
					if (tss.Length != 0)
					fFirst = false;
				}
				else if (tssSep != null)
				{
					AddMarkedString(vwenv, caller, tssSep);
				}
				if (vwenv is ConfiguredExport)
					(vwenv as ConfiguredExport).BeginMultilingualAlternative(ws);
				if (fLabel)
				{
					IWritingSystem wsObj = m_cache.ServiceLocator.WritingSystemManager.Get(ws);
					DisplayWsLabel(wsObj, vwenv, m_cache);
				}
				if (fCurrentHvo)
				{
					MarkSource(vwenv, caller);
					vwenv.AddStringAltMember(flid, ws, this);
				}
				else
					AddStringThatCounts(vwenv, tss, caller);
				if (vwenv is ConfiguredExport)
					(vwenv as ConfiguredExport).EndMultilingualAlternative();
			}
		}

		/// <summary>
		/// Add a string that 'counts' for purposes of being treated as a non-empty display.
		/// Normally AddString is used for labels and such that don't count, but sometimes for
		/// important things like name of owner.
		/// </summary>
		private void AddStringThatCounts(IVwEnv vwenv, ITsString tss, XmlNode caller)
		{
			if (vwenv is TestCollectorEnv)
				(vwenv as TestCollectorEnv).AddTsString(tss);
			else
				AddMarkedString(vwenv, caller, tss);
		}

		/// <summary>
		/// This is the core routine for XmlVc, which determines how to display the object indicated by hvo,
		/// using the XML 'fragment' given in frag, which may be affected by 'caller', the context (typically
		/// I think a part ref). This is called as part of Display to actually put the data into the vwenv.
		///
		/// NOTE: a parallel routine, DetermineNeededFieldsFor, figures out the data that may be needed
		/// by ProcessFrag, and should be kept in sync with it.
		/// </summary>
		/// <param name="frag"></param>
		/// <param name="vwenv"></param>
		/// <param name="hvo"></param>
		/// <param name="fEditable"></param>
		/// <param name="caller"></param>
		public virtual void ProcessFrag(XmlNode frag, IVwEnv vwenv, int hvo, bool fEditable,
			XmlNode caller)
		{
			// Some nodes are known to be uninteresting.
			if (CanSkipNode(frag))
				return;

			try
			{
				switch (frag.Name)
				{
					default:
						Debug.Assert(false, "unrecognized XML node.");
						break;
					case "string":
						{
							int hvoTarget = hvo;
							GetActualTarget(frag, ref hvoTarget, m_sda);	// modify the hvo if needed
							if (hvo != hvoTarget)
							{
								AddStringFromOtherObj(frag, hvoTarget, vwenv, caller);
								break;
							}
							int flid = 0;
							if (TryCustomField(m_sda, frag, hvo, out flid))
							{
								// ignore invalid custom fields (LT-6474).
								if (flid == 0)
									break;
							}
							else
							{
								// Could still be an invalid CustomField that TryCustomField couldn't detect.
								if (!TryGetFlid(frag, hvoTarget, out flid))
									break;
							}

							int itype = m_sda.MetaDataCache.GetFieldType(flid);
							itype = itype & (int)CellarPropertyTypeFilter.VirtualMask;
							if (itype == (int)CellarPropertyType.Unicode)
							{
								int wsForUnicode = GetWritingSystemForObject(frag,
									hvo,
									flid,
									m_wsReversal == 0 ? m_cache.DefaultUserWs : m_wsReversal);
								vwenv.AddUnicodeProp(flid, wsForUnicode, this);
							}
							else if (itype == (int)CellarPropertyType.String)
							{
								MarkSource(vwenv, caller);
								vwenv.AddStringProp(flid, this);
							}
							else // multistring of some type
							{
								if (s_cwsMulti > 1)
								{
									string sLabelWs = XmlUtils.GetOptionalAttributeValue(frag, "ws");
									if (sLabelWs != null && sLabelWs == "current")
									{
										MarkSource(vwenv, caller);
										vwenv.OpenSpan();
										DisplayMultiSep(frag, vwenv, m_cache);
										DisplayWsLabel(s_qwsCurrent, vwenv, m_cache);
										if (s_qwsCurrent != null)
										{
											vwenv.AddStringAltMember(flid, s_qwsCurrent.Handle, this);
									}
										vwenv.CloseSpan();
								}
								}
								else
								{
									int wsid = GetWritingSystemForObject(frag,
										hvo,
										flid,
										m_wsReversal == 0 ? m_cache.DefaultUserWs : m_wsReversal);
									MarkSource(vwenv, caller);
									vwenv.AddStringAltMember(flid, wsid, this);
								}
							}
							break;
						}
					case "computedString":
						{
							// For example:
							// <part id="FsFeatStruc-Jt-PhonFeats_$fieldName" type="jtview">
							//   <computedString method="GetFeatureValueTSS" argument="$fieldName"/>
							// </part>
							var obj = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
							string method = XmlUtils.GetAttributeValue(frag, "method");
							if (!String.IsNullOrEmpty(method))
							{
								Type objType = obj.GetType();
								System.Reflection.MethodInfo mi = objType.GetMethod(method);
								if (mi != null)
								{
									string argument = XmlUtils.GetAttributeValue(frag, "argument");
									if (!String.IsNullOrEmpty(argument))
									{
										var value = (ITsString)mi.Invoke(obj, new object[] {argument});
										vwenv.AddString(value);
									}
								}
							}
							break;
						}
					case "configureMlString":
						{
							int hvoTarget = hvo;
							GetActualTarget(frag, ref hvoTarget, m_sda);	// modify the hvo if needed
							int flid = 0;
							if (TryCustomField(m_sda, frag, hvoTarget, out flid))
							{
								// ignore invalid custom fields (LT-6474).
								if (flid == 0)
									break;
							}
							else
							{
								// Could still be an invalid CustomField that TryCustomField couldn't detect.
								if (!TryGetFlid(frag, hvoTarget, out flid))
									break;
							}
							// The Ws info specified in the part ref node
							HashSet<int> wsIds;
							string sWs = XmlUtils.GetOptionalAttributeValue(caller, "ws");
							if (sWs == "reversal")
							{
								wsIds = new HashSet<int> {m_wsReversal};
							}
							else
							{
								wsIds = WritingSystemServices.GetAllWritingSystems(m_cache, caller, null, hvoTarget, flid);
							}
							if (wsIds.Count == 1)
							{
								if (hvoTarget != hvo)
									DisplayOtherObjStringAlt(flid, wsIds.First(), vwenv, hvoTarget, caller);
								else
								{
									MarkSource(vwenv, caller);
									vwenv.AddStringAltMember(flid, wsIds.First(), this);
							}
							}
							else
							{
								AddMultipleAlternatives(wsIds, vwenv, hvoTarget, flid, caller, hvoTarget == hvo);
							}
							break;
						}
					case "para":
						{
							ProcessProperties(frag, vwenv);
							var props = XmlUtils.GetFirstNonCommentChild(frag);
							if ((props == null || props.Name != "properties") && caller != null)
							{
								var style = XmlUtils.GetOptionalAttributeValue(caller, "style");
								if (!String.IsNullOrEmpty(style))
									vwenv.set_StringProperty((int) FwTextPropType.ktptNamedStyle, style);
							}
							// Note: the following assertion is indicative of not setting colSpec attribute multipara="true"
							// VwEnv.cpp: Assertion failed "Expression: dynamic_cast<VwPileBox *>(m_pgboxCurr)"
							vwenv.OpenParagraph();
							ProcessChildren(frag, vwenv, hvo);
							vwenv.CloseParagraph();
							break;
						}
					case "concpara":
						{
							OpenConcordanceParagraph(vwenv, hvo, frag);
							ProcessChildren(frag, vwenv, hvo);
							vwenv.CloseParagraph();
							break;
						}
					case "div":
						{
							ProcessProperties(frag, vwenv);
							vwenv.OpenDiv();
							ProcessChildren(frag, vwenv, hvo, caller);	// caller used for numbering subrecords
							vwenv.CloseDiv();
							break;
						}
					case "innerpile":
						{
							ProcessProperties(frag, vwenv);
							vwenv.OpenInnerPile();
							ProcessChildren(frag, vwenv, hvo);
							vwenv.CloseInnerPile();
							break;
						}
					case "span":
						{
							ProcessProperties(frag, vwenv);
							vwenv.OpenSpan();
							ProcessChildren(frag, vwenv, hvo);
							vwenv.CloseSpan();
							break;
						}
					case "table":
						{
							ProcessProperties(frag, vwenv);
							IWritingSystem ws = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;
							if (ws.RightToLeftScript)
								vwenv.set_IntProperty((int)FwTextPropType.ktptRightToLeft, (int)FwTextPropVar.ktpvEnum, -1);
							// defaults for table settings.
							VwLength vlWidth = new VwLength();
							// For now only support percent of available width; actual unit is
							// 100ths of a percent, so 10000 is 100%, i.e., take up all available
							// width
							vlWidth.nVal = XmlUtils.GetOptionalIntegerValue(frag, "width", 10000);
							vlWidth.unit = VwUnit.kunPercent100;

							XmlAttribute xa = frag.Attributes["columns"];
							if (xa != null && xa.Value == "%ColumnCount")
							{
								// This is a kludge currently used only for MoInflAffixTemplate.
								// Set up the dependencies so the table gets reconstructed if the
								// prefixes or suffixes change.
								vwenv.NoteDependency(new[] { hvo, hvo },
									new[] { MoInflAffixTemplateTags.kflidPrefixSlots,
										MoInflAffixTemplateTags.kflidSuffixSlots },
									2);
							}


							// Open the table
							vwenv.OpenTable(GetColCount(frag, hvo),
								vlWidth,
								XmlUtils.GetOptionalIntegerValue(frag, "border", 0),
								GetAlignment(frag),
								GetFramePositions(frag),
								GetRules(frag),
								XmlUtils.GetOptionalIntegerValue(frag, "spacing", 0),
								XmlUtils.GetOptionalIntegerValue(frag, "padding", 0),
								false);
							ProcessColumnSpecs(frag, vwenv, hvo);
							ProcessChildren(frag, vwenv, hvo);
							vwenv.CloseTable();
							break;
						}
					case "header":
						{
							ProcessProperties(frag, vwenv);
							vwenv.OpenTableHeader();
							ProcessChildren(frag, vwenv, hvo);
							vwenv.CloseTableHeader();
							break;
						}
					case "footer":
						{
							ProcessProperties(frag, vwenv);
							vwenv.OpenTableFooter();
							ProcessChildren(frag, vwenv, hvo);
							vwenv.CloseTableFooter();
							break;
						}
					case "body":
						{
							ProcessProperties(frag, vwenv);
							vwenv.OpenTableBody();
							ProcessChildren(frag, vwenv, hvo);
							vwenv.CloseTableBody();
							break;
						}
					case "row":
						{
							ProcessProperties(frag, vwenv);
							vwenv.OpenTableRow();
							ProcessChildren(frag, vwenv, hvo);
							vwenv.CloseTableRow();
							break;
						}
					case "cell":
						{
							ProcessProperties(frag, vwenv);
							vwenv.OpenTableCell(XmlUtils.GetOptionalIntegerValue(frag, "spanrows", 1),
								XmlUtils.GetOptionalIntegerValue(frag, "spancols", 1));
							ProcessChildren(frag, vwenv, hvo);
							vwenv.CloseTableCell();
							break;
						}
					case "headercell":
						{
							ProcessProperties(frag, vwenv);
							vwenv.OpenTableHeaderCell(XmlUtils.GetOptionalIntegerValue(frag, "spanrows", 1),
								XmlUtils.GetOptionalIntegerValue(frag, "spancols", 1));
							ProcessChildren(frag, vwenv, hvo);
							vwenv.CloseTableHeaderCell();
							break;
						}
					// Deprecated, but still the only way to do some things until
					// we have individual property nodes for everything.
					case "mod":
						{
							int tpt = Convert.ToInt32(frag.Attributes["prop"].Value, 10);
							int var = Convert.ToInt32(frag.Attributes["var"].Value, 10);
							int val = Convert.ToInt32(frag.Attributes["val"].Value, 10);
							vwenv.set_IntProperty(tpt, var, val);
							break;
						}
					case "seq":
						{
							int flid = GetFlid(frag, hvo);
							if (flid == 0)
								return; // can't do anything. Report?

							int fragId = GetSubFragIdSeq(frag, caller);
							if (fragId == 0)
								return; // something badly wrong.
							AddObjectVector(frag, vwenv, flid, fragId, caller);
							break;
						}
					case "objectOfRowUsingViewConstructor": // display the current object using an external VC.
						//notice this assumes that it wants a FdoCache as an argument
						IVwViewConstructor vc =
							(IVwViewConstructor)SIL.Utils.DynamicLoader.CreateObject(frag,
							new Object[] { m_cache });
						int selectorId =
							Convert.ToInt32(XmlUtils.GetManditoryAttributeValue(frag, "selector"));
						// Note this is AddObj, not AddObjProp, and it explicitly adds the current object using the new vc and fragId
						vwenv.AddObj(hvo, vc, selectorId);
						break;

					case "obj":
						{
							int flid = GetFlid(frag, hvo);
							int fragId = GetSubFragId(frag, caller);
							if (flid == 0 || fragId == 0)
								return; // something badly wrong.
							AddObject(frag, vwenv, flid, fragId, caller, hvo);
							break;
						}
					case "objlocal":
						{
							// Display the object in the specified atomic property, using a fragment which causes
							// the children of this node to be processed, using our caller for params.
							// This is like an 'obj' element, but instead of displaying the target object using
							// one of its own views, we effectively display it using parts specified right in this node.
							// This is especially useful for parts which want to pull in writing system specs from the caller.
							int flid = GetFlid(frag, hvo);
							int fragId = GetId(new ObjLocalCommand(frag, caller), m_idToDisplayCommand, m_displayCommandToId);
							vwenv.AddObjProp(flid, this, fragId);
							break;
						}
					case "int":
						{
							int flid = GetFlid(frag, hvo);
							if (flid == 0)
								return; // something badly wrong.
							MarkSource(vwenv, caller);
							vwenv.AddIntProp(flid);
							break;
						}
					case "gendate":
						{
							int flid = GetFlid(frag, hvo);
							if (flid == 0)
								return; // something badly wrong.
							int formatFrag = VwBaseVc.kfragGenDateLong;
							var formatAttr = frag.Attributes["format"];
							if (vwenv is SortCollectorEnv)
							{
								formatFrag = VwBaseVc.kfragGenDateSort;
							}
							else if (formatAttr != null)
							{
								switch (formatAttr.InnerText.ToLowerInvariant())
								{
									case "short":
										formatFrag = VwBaseVc.kfragGenDateShort;
										break;
									case "long":
										formatFrag = VwBaseVc.kfragGenDateLong;
										break;
									case "sort":
										formatFrag = VwBaseVc.kfragGenDateSort;
										break;
									default:
										throw new ConfigurationException("Invalid format attribute value", frag);
								}
							}

							// the actual display of a GenDate property is handled in the VwBaseVc.DisplayVariant method
							MarkSource(vwenv, caller);
							vwenv.AddProp(flid, this, formatFrag);
							break;
						}
					case "datetime":
						{
							int flid = GetFlid(frag, hvo);
							if (flid == 0)
								return; // something badly wrong.

							CellarPropertyType itype = (CellarPropertyType)m_sda.MetaDataCache.GetFieldType(flid);
							if (itype == CellarPropertyType.Time)
							{
								DateTime dt = SilTime.GetTimeProperty(m_sda, hvo, flid);
								XmlNode dtNode = XmlViewsUtils.CopyWithParamDefaults(frag);
								string format;
								if (vwenv is SortCollectorEnv)
									format = System.Globalization.DateTimeFormatInfo.InvariantInfo.SortableDateTimePattern;
								else
									format = XmlUtils.GetOptionalAttributeValue(dtNode, "format");
								string formattedDateTime;
								try
								{
									if (format != null)
									{
										formattedDateTime = dt.ToString(format, System.Globalization.DateTimeFormatInfo.CurrentInfo);
									}
									else
									{
										// "G" format takes user's system ShortDate format appended by system LongTime format.
										formattedDateTime = dt.ToString("G", System.Globalization.DateTimeFormatInfo.CurrentInfo);
									}
								}
								catch (FormatException e)
								{
									string errorMsg = "Invalid datetime format attribute (" + format + ") in " + e.Source;
									formattedDateTime = errorMsg;
									throw new SIL.Utils.ConfigurationException(errorMsg, frag, e);
								}
								ITsStrFactory tsf = m_cache.TsStrFactory;
								int systemWs = m_cache.ServiceLocator.WritingSystemManager.UserWs;
								ITsString tss = tsf.MakeString(formattedDateTime, systemWs);
								if (vwenv is ConfiguredExport)
									vwenv.AddTimeProp(flid, 0);
								AddStringThatCounts(vwenv, tss, caller);
							}
							else
							{
								string stFieldName = XmlUtils.GetManditoryAttributeValue(frag, "field");
								throw new Exception("Bad field type (" + stFieldName + " for hvo " + hvo + " found for " +
									frag.Name + "  property " + flid + " in " + frag.OuterXml);
							}
							break;
						}
					case "iconInt":
						{
							throw new InvalidOperationException("Obsolete system use for 'iconInt'.");
							/*
							int flid = GetFlid(frag, hvo);
							if (flid == 0)
								return; // something badly wrong.
							//temp
							int min = Int32.Parse(XmlUtils.GetOptionalAttributeValue(frag, "min", "0"));
							int max = Int32.Parse(XmlUtils.GetManditoryAttributeValue(frag, "max"));
							// Get an ID that simply identifies the pictureValues child. This is used in DisplayPicture.
							int fragId = GetId(frag.SelectSingleNode("picturevalues"), m_idToDisplayCommand, m_displayCommandToId);
							vwenv.AddIntPropPic(flid, this, fragId, min, max);
							break;
							*/
						}
					case "commandIcon":
						{
							AddCommandIcon(frag, vwenv, hvo);
							break;
						}
					case "lit":
						{
							// Default to UI writing system.
							string literal = frag.InnerText;
							if (m_stringTable != null)
							{
								string sTranslate = XmlUtils.GetOptionalAttributeValue(frag, "translate", "");
								if (sTranslate.Trim().ToLower() != "do not translate")
									literal = m_stringTable.LocalizeLiteralValue(literal);
							}
							string sWs = XmlUtils.GetOptionalAttributeValue(frag, "ws");
							int ws;
							if (sWs != null)
								ws = m_cache.WritingSystemFactory.GetWsFromStr(sWs);
							else
								ws = m_cache.WritingSystemFactory.UserWs;
							vwenv.AddString(m_cache.TsStrFactory.MakeString(literal, ws));
							break;
						}
					case "if":
						{
							if (ConditionPasses(vwenv, frag, hvo, m_cache, m_sda, caller))
								ProcessChildren(frag, vwenv, hvo, caller);
							break;
						}
					case "ifnot":
						{
							if (!ConditionPasses(vwenv, frag, hvo, m_cache, m_sda, caller))
								ProcessChildren(frag, vwenv, hvo, caller);
							break;
						}
					case "choice":
						{
							foreach (XmlNode clause in frag.ChildNodes)
							{
								if (clause.Name == "where")
								{
									if (ConditionPasses(vwenv, clause, hvo, m_cache, m_sda, caller))
									{
										ProcessChildren(clause, vwenv, hvo, caller);
										break;
									}
								}
								else if (clause.Name == "otherwise")
								{
									// enhance: verify last node?
									ProcessChildren(clause, vwenv, hvo, caller);
									break;
								}
								else
								{
									throw new Exception("elements in choice must be where or otherwise");
								}
							}
							break;
						}
					case "stringList":
						{
							string[] labels;
							if (!m_StringsFromListNode.TryGetValue(frag, out labels))
							{
								if (StringTbl == null)
									throw new Exception("The stringList fragment requires a StringTable to be defined by the program");
								labels = StringTbl.GetStringsFromStringListNode(frag);
								m_StringsFromListNode[frag] = labels;
							}
							int flid = GetFlid(frag, hvo);
							int value = m_sda.get_IntProp(hvo, flid);
							if ((value >= 0) &&
								(value < labels.Length))
							{
								int wsUi = m_cache.WritingSystemFactory.UserWs;
								ITsString tss = m_cache.TsStrFactory.MakeString(labels[value], wsUi);
								vwenv.AddString(tss);
								NoteDependency(vwenv, hvo, flid);
							}
							break;
						}
					case "multiling":
						{
							ProcessMultiLingualChildren(frag, vwenv, hvo);
							break;
						}
					case "labelws":
						{
							if (s_cwsMulti > 1)
							{
								DisplayMultiSep(frag, vwenv, m_cache);
								DisplayWsLabel(s_qwsCurrent, vwenv, m_cache);
							}
							break;
						}
					case "part":
						// This occurs when the node we're processing is a child of a layout,
						// and therefore a 'part ref'. It calls the specified part of the current object.

						// Some configuration items, like senses, will not display themselves if their type has already been shown
						// i.e. when displaying a sense any attempt to display sense info under the child of a sense is ignored.
						// in certain circumstances, like when a minor entry is the component of a sense of a main entry you need to display
						// the senses of the subentry. The following code will allow that to be specified in the configuration xml.
						bool wasIgnoring = ShouldIgnoreGramInfo;
						if (frag.Attributes != null && frag.Attributes["forceSubentryDisplay"] != null)
						{
							ShouldIgnoreGramInfo = !Boolean.Parse(frag.Attributes["forceSubentryDisplay"].Value);
						}
						ProcessPartRef(frag, hvo, vwenv);
						ShouldIgnoreGramInfo = wasIgnoring;
						break;
					case "sublayout":
						string layoutName = XmlUtils.GetOptionalAttributeValue(frag, "name", null);
						XmlNode layout;
						if (frag.ChildNodes.Count > 0 && layoutName == null)
						{
							layout = frag;
						}
						else
						{
							// This will potentially also find a part, which we don't want here.
							layout = GetNodeForPart(hvo, layoutName, true);
							if (layout.Name != "layout")
								throw new Exception("sublayout must refer to layout, not part");
						}
						string group = XmlUtils.GetOptionalAttributeValue(frag, "group", "");
						switch (group)
						{
							case "para":
								if (vwenv.IsParagraphOpen())
								{
									group = ""; // suppress CloseParagraph
									break; // don't start our own paragraph if some caller did it for us.
								}
								string style = XmlUtils.GetOptionalAttributeValue(frag, "style", null);
								if (style == null)
								{
									if (caller != null)
									{
										style = GetParaStyle(caller);
									}
									else
									{
										foreach (var parent in m_stackPartRef)
										{
											style = GetParaStyle(parent);
											if (style != null)
												break;
										}
									}
								}
								else if (m_stackPartRef.Count > 0)
								{
									// Check whether we want to replace the given style due to recursive reuse.
									// (See LT-10400.)
									var callingFrag = m_stackPartRef[0];
									if (!XmlUtils.GetOptionalBooleanAttributeValue(callingFrag, "recurseConfig", true) &&
										XmlUtils.GetOptionalAttributeValue(callingFrag, "flowType") == "div" &&
										!String.IsNullOrEmpty(GetParaStyle(callingFrag)))
									{
										style = GetParaStyle(callingFrag);
									}
								}
								GetParagraphStyleIfPara(hvo, ref style);
								if (style != null)
									vwenv.set_StringProperty((int)FwTextPropType.ktptNamedStyle, style);
								vwenv.OpenParagraph();
								break;
							case "innerpile":
								vwenv.OpenInnerPile();
								break;
							default:
								break;
						}
						ProcessChildren(layout, vwenv, hvo, frag);
						switch (group)
						{
							case "para":
								vwenv.CloseParagraph();
								break;
							case "innerpile":
								vwenv.CloseInnerPile();
								break;
							default:
								break;
						}
						break;
					case "picture": // current object is a CmPicture, display the picture.
						{
							var picture = m_cache.ServiceLocator.GetInstance<ICmPictureRepository>().GetObject(hvo);
							if (picture.PictureFileRA == null)
								break; // nothing we can show. // Enhance: should we insert some sort of error?

							string imagePath = picture.PictureFileRA.AbsoluteInternalPath;
							if (String.IsNullOrEmpty(imagePath))
								break;
							var picturePathCollector = vwenv as ICollectPicturePathsOnly;
							if (picturePathCollector != null)
							{
								if (File.Exists(FileUtils.ActualFilePath(imagePath)))
								{
									picturePathCollector.APictureIsBeingAdded();
								}
								// for export, we want the path, but not for these other cases.  (LT-5326)
								// There might be a more efficient way to do this, e.g., by adding to ICollectPicturePathsOnly.
								int fragId = GetSubFragId(frag, caller);
								vwenv.AddObjProp(CmPictureTags.kflidPictureFile, this, fragId);
								break; // whether it exists or not, don't actually make the picture. This can run us out of memory on export (LT-13704)
							}
							IPicture comPicture = GetComPicture(imagePath);
							if (comPicture != null)
							{
								MarkSource(vwenv, caller);
								int height = XmlUtils.GetOptionalIntegerValue(frag, "height", 0);
								int width = XmlUtils.GetOptionalIntegerValue(frag, "width", 0);
								vwenv.AddPicture(comPicture, 1, width, height);
							}

						}
						break;
					// A generate node may occur in a layout element, but doesn't do anything when executed.
					case "generate":
						break;
					case "savehvo":
						m_stackHvo.Push(hvo);
						ProcessChildren(frag, vwenv, hvo, caller);
						m_stackHvo.Pop();
						break;
				}
			}
			catch (SIL.Utils.ConfigurationException)
			{
				throw;
			}
			catch (Exception error)
			{
				throw new SIL.Utils.ConfigurationException(
					"There was an error processing this fragment. " + error.Message, frag, error);
			}
		}

		/// <summary>
		/// Return the paragraph style that should be used for the current item, which is typically one thing
		/// in a sequence invoked by the part ref caller. Usually this is just the parastyle attribute,
		/// but if a singlestyle attribute is present, determine whether to use that style instead.
		/// 19 July 2011 GJM: singlestyle attribute removed per LT-11598
		/// </summary>
		private static string GetParaStyle(XmlNode caller)
		{
			var result = XmlUtils.GetOptionalAttributeValue(caller, "parastyle", null);
			//var singleStyle = XmlUtils.GetOptionalAttributeValue(caller, "singlestyle", null);
			//if (singleStyle == null)
			//    return result;
			//int hvoOuter, tagOuter, ihvo;
			//vwenv.GetOuterObject(vwenv.EmbeddingLevel - 1,
			//    out hvoOuter, out tagOuter, out ihvo);
			//if (vwenv.DataAccess.get_VecSize(hvoOuter, tagOuter) == 1)
			//    return singleStyle;
			return result;
		}

		internal void GetParagraphStyleIfPara(int hvo, ref string style)
		{
			try
			{
				int clid = m_cache.DomainDataByFlid.get_IntProp(hvo, CmObjectTags.kflidClass);
				if (clid == StTxtParaTags.kClassId)
				{
					IStTxtPara para = m_cache.ServiceLocator.GetInstance<IStTxtParaRepository>().GetObject(hvo);
					if (!String.IsNullOrEmpty(para.StyleName) && para.StyleName != "Normal")
						style = para.StyleName;
				}
			}
			catch { }
		}

		private IPicture GetComPicture(string imagePath)
		{
			if (SuppressPictures)
				return null;
			return m_app.PictureHolder.GetComPicture(imagePath);
		}

		/// <summary>
		/// custom field identified during TryColumnForCustomField. Is not currently meaningful
		/// outside TryColumnForCustomField().
		/// </summary>
		XmlNode m_customFieldNode = null;

		/// <summary>
		/// Determine whether or not the given colSpec refers to a custom field, respective of
		/// whether or not it is still valid.
		/// Uses layout/parts to find custom field specifications.
		/// </summary>
		/// <param name="colSpec"></param>
		/// <param name="rootObjClass">the (base)class of items being displayed</param>
		/// <param name="customFieldNode">the xml node of the custom field, if we found one.</param>
		/// <param name="propWs">the first prop/ws info we could find for the custom field,
		/// null if customField is invalid for this database.</param>
		/// <returns></returns>
		internal bool TryColumnForCustomField(XmlNode colSpec, int rootObjClass, out XmlNode customFieldNode, out PropWs propWs)
		{
			propWs = null;
			// now determine the fields that are needed for this node.
			m_customFieldNode = null;
			customFieldNode = null;
			try
			{
				// TryFindFirstAtomicField has the side effect of setting "m_customFieldNode" when we find it.
				TryFindFirstAtomicField(colSpec, rootObjClass, out propWs);
				if (m_customFieldNode != null)
				{
					// we found a custom field spec.
					customFieldNode = m_customFieldNode;
				}
				return customFieldNode != null;
			}
			finally
			{
				m_customFieldNode = null;
			}
		}

		/// <summary>
		/// Find the first part node refered to by the parent node
		/// </summary>
		/// <param name="parentNode"></param>
		/// <param name="rootObjClass">the class of the part we are looking for</param>
		/// <returns></returns>
		internal XmlNode GetPartFromParentNode(XmlNode parentNode, int rootObjClass)
		{
			XmlNode partNode = null;
			string layout = XmlUtils.GetOptionalAttributeValue(parentNode, "layout");
			if (layout != null)
			{
				// get the part from the layout.
				partNode = this.GetNodeForPart(layout, false, rootObjClass);
			}
			else
			{
				partNode = parentNode; // treat column node as containing its parts.
			}
			return partNode;
		}

		/// <summary>
		/// Determine the first atomic field PropWs info we can find for the given parentNode.
		/// NOTE: This uses DetermineNeededFieldsForChildren which has the side effect of
		/// setting "m_customFieldNode" when we find it.
		/// </summary>
		/// <param name="parentNode"></param>
		/// <param name="rootObjClass"></param>
		/// <param name="propWs"></param>
		/// <returns></returns>
		internal bool TryFindFirstAtomicField(XmlNode parentNode, int rootObjClass, out PropWs propWs)
		{
			propWs = null;
//			XmlNode partNode = GetPartFromParentNode(parentNode, rootObjClass);
			NeededPropertyInfo info = DetermineNeededFieldsForSpec(parentNode, rootObjClass);
			if (info != null)
			{
				// if we have a sequence, step down the first branch till we find the first atomic field.
				propWs = FindFirstAtomicPropWs(info);
			}
			return propWs != null;
		}

		internal NeededPropertyInfo DetermineNeededFieldsForSpec(XmlNode spec, int rootObjClass)
		{
			XmlNode partNode = GetPartFromParentNode(spec, rootObjClass);
			if (partNode != null)
			{
				NeededPropertyInfo info = new NeededPropertyInfo(rootObjClass);
				this.DetermineNeededFieldsForChildren(partNode, spec, info);
				return info;
			}
			return null;
		}

		private static PropWs FindFirstAtomicPropWs(NeededPropertyInfo info)
		{
			PropWs propWs = null;
			NeededPropertyInfo nextFieldInfo;
			if (info.SeqFields.Count > 0)
			{
				nextFieldInfo = info.SeqFields[0];
				propWs = FindFirstAtomicPropWs(nextFieldInfo);
			}
			else if (info.AtomicFields.Count > 0)
			{
				propWs = info.AtomicFields[0];
			}
			return propWs;
		}

		/// <summary>
		/// This routine roughly parallels ProcessFrag. It inserts into info (which functions as a Collector)
		/// information about which fields are needed in order to eventually ProcessFrag with the same
		/// frag and caller. This is used to generate queries to efficiently preload this data for a
		/// collection of root objects.
		/// </summary>
		/// <param name="frag"></param>
		/// <param name="caller"></param>
		/// <param name="info"></param>
		internal void DetermineNeededFieldsFor(XmlNode frag, XmlNode caller, NeededPropertyInfo info)
		{
			// Some nodes are known to be uninteresting.
			if (CanSkipNode(frag))
				return;

			try
			{
				switch (frag.Name)
				{
					default:
						// Any cases not handled just do nothing.
						break;
					case "part":
						// This occurs when the node we're processing is a child of a layout,
						// and therefore a 'part ref'. It calls the specified part of the current object.
						DetermineNeededFieldsForPartRef(frag, info);
						break;
					case "elementDisplayCondition":
					case "if":
					case "ifnot":
					case "where":
						{
							// Enhance: get stuff for the condition tests of if, ifnot, and where
							int flid = DetermineNeededFlid(frag, info);
							IFwMetaDataCache mdc = m_sda.MetaDataCache;
							NeededPropertyInfo infoTarget = info;
							if (flid == 0)
							{
								// Deal with the object/object/flid scenario
								string stFieldPath = XmlUtils.GetOptionalAttributeValue(frag, "field");
								if (!String.IsNullOrEmpty(stFieldPath))
								{
									string[] rgstFields = stFieldPath.Split(new char[] { '/' });
									if (rgstFields.Length >= 2) // otherwise DetermineNeededFlid already got it.
									{
										string stClassName = XmlUtils.GetOptionalAttributeValue(frag, "class");
										if (String.IsNullOrEmpty(stClassName))
										{
											int classId = info.TargetClass(this);
											flid = mdc.GetFieldId2(classId, rgstFields[0], true);
										}
										else
										{
											flid = mdc.GetFieldId(stClassName, rgstFields[0], true);
										}
										// on entry to each iteration, flid is the flid resulting from
										// rgstFields[i-1]. On successful exit, it is the flid of the last
										// item, the real field, which is processed normally.
										for (int i = 1; i < rgstFields.Length; i++)
										{
											// We assume this intermediate flid is an object property.
											infoTarget = infoTarget.AddObjField(flid, false);
											string subFieldName = rgstFields[i];
											int outerClassId = mdc.GetDstClsId(flid);
											flid = mdc.GetFieldId2(outerClassId, subFieldName, true);
										}
									}
								}
							}
							if (flid != 0)
							{
								string sWs = XmlUtils.GetOptionalAttributeValue(frag, "ws", null);
								int wsid = 0;
								if (sWs != null)
								{
									if (sWs == "current" && s_qwsCurrent != null)
									{
										wsid = s_qwsCurrent.Handle;
									}
									else
									{
										// If ws is 'configure' then we must have a caller to inherit the configured value from.
										if (sWs == "configure")
										{
											sWs = XmlUtils.GetManditoryAttributeValue(caller, "ws");
										}
										wsid = m_cache.WritingSystemFactory.GetWsFromStr(sWs);
									}
								}
								if (wsid == 0 && sWs != null)
								{
									foreach (int ws in WritingSystemServices.GetWritingSystems(m_cache, frag))
										infoTarget.AddAtomicField(flid, ws);
								}
								else
								{
									infoTarget.AddAtomicField(flid, wsid);
								}
							}
						}
						DetermineNeededFieldsForChildren(frag, caller, info);
						break;
					case "para":
					case "concpara":
					case "div":
					case "innerpile":
					case "span":
					case "table":
					case "header":
					case "footer":
					case "body":
					case "row":
					case "cell":
					case "headercell":
					case "choice":
					case "otherwise":
						DetermineNeededFieldsForChildren(frag, caller, info);
						break;
					case "string":
						{
							//GetActualTarget(frag, ref hvoTarget, m_cache);	// modify the hvo if needed
							// If GetActualTargetInfo would modify the target don't try to be smart.
							string target = XmlUtils.GetOptionalAttributeValue(frag, "target", "").ToLower();
							if (target == "owner")
								return;

							int flid = 0;
							if (!TryCustomField(m_sda, frag, 0, out flid))
							{
								flid = DetermineNeededFlid(frag, info);
							}
							else
							{
								// TryCustomField may not be able to dertermine the flid
								// without the hvo, but might still be able to determine that
								// it's a custom field.
								if (flid == 0)
									flid = DetermineNeededFlid(frag, info);
								m_customFieldNode = frag;
							}

							// If we don't have enough info to determine a flid, give up.
							if (flid == 0)
								return;

							int itype = m_sda.MetaDataCache.GetFieldType(flid);
							itype = itype & (int)CellarPropertyTypeFilter.VirtualMask;
							if ((itype == (int)CellarPropertyType.MultiString) ||
								(itype == (int)CellarPropertyType.MultiUnicode))
							{
								if (s_cwsMulti > 1)
								{
									string sLabelWs = XmlUtils.GetOptionalAttributeValue(frag, "ws");
									if (sLabelWs != null && sLabelWs == "current")
									{
										if (s_qwsCurrent != null)
											info.AddAtomicField(flid, s_qwsCurrent.Handle);
									}
								}
								else
								{
									foreach (int wsid in WritingSystemServices.GetWritingSystems(m_cache, frag))
										info.AddAtomicField(flid, wsid);
								}
							}
							else
							{
								// add info for simple string.
								info.AddAtomicField(flid, 0);
							}
							break;
						}
					case "configureMlString":
						{
							string target = XmlUtils.GetOptionalAttributeValue(frag, "target", "").ToLower();
							if (target == "owner")
								return;

							int flid = 0;
							if (!TryCustomField(m_sda, frag, 0, out flid))
							{
								flid = DetermineNeededFlid(frag, info);
							}
							else
							{
								// TryCustomField may not be able to dertermine the flid
								// without the hvo, but might still be able to determine that
								// it's a custom field.
								if (flid == 0)
									flid = DetermineNeededFlid(frag, info);
								m_customFieldNode = frag;
							}
							// If we don't have enough info to determine a flid, give up.
							if (flid == 0)
								return;

							// The Ws info specified in the part ref node
							string sWs = XmlUtils.GetOptionalAttributeValue(caller, "ws");
							if (sWs == "reversal")
							{
								info.AddAtomicField(flid, m_wsReversal);
							}
							else
							{
								foreach (int wsid in WritingSystemServices.GetWritingSystems(m_cache, caller))
								info.AddAtomicField(flid, wsid);
							}
							break;
						}
					case "gendate":
					case "int":
						{
							int flid = 0;
							if (!TryCustomField(m_sda, frag, 0, out flid))
							{
								flid = DetermineNeededFlid(frag, info);
							}
							else
							{
								// TryCustomField may not be able to dertermine the flid
								// without the hvo, but might still be able to determine that
								// it's a custom field.
							if (flid == 0)
									flid = DetermineNeededFlid(frag, info);
								m_customFieldNode = frag;
							}
							if (flid == 0)
								return;
							info.AddAtomicField(flid, 0);
						}
						break;
					case "seq":
						{
							int flid = 0;
							if (!TryCustomField(m_sda, frag, 0, out flid))
							{
								flid = DetermineNeededFlid(frag, info);
							}
							else
							{
								// TryCustomField may not be able to dertermine the flid
								// without the hvo, but might still be able to determine that
								// it's a custom field.
							if (flid == 0)
									flid = DetermineNeededFlid(frag, info);
								m_customFieldNode = frag;
							}
							if (flid == 0)
								return;
							int fragId = GetSubFragId(frag, caller);
							if (fragId == 0)
								return; // something badly wrong.
							// Checking depth guards against infinite recursion (e.g., subsenses of senses),
							// but also against doing optimization queries for cases which rarely occur in
							// the real data. It may need fine tuning, possibly even by an XML-specified property
							// of the top-level layout.
							if (info.SeqDepth >= 4)
								break;

							NeededPropertyInfo subinfo = info.AddObjField(flid, true);
								DisplayCommand dispCommand = m_idToDisplayCommand[fragId];
								dispCommand.DetermineNeededFields(this, fragId, subinfo);
							break;
						}
					case "sublayout":
						string layoutName = XmlUtils.GetOptionalAttributeValue(frag, "name", null);
						XmlNode layout;
						if (frag.ChildNodes.Count > 0 && layoutName == null)
						{
							layout = frag;
						}
						else
						{
							// This will potentially also find a part, which we don't want here.
							layout = GetNodeForPart(layoutName, true, info.TargetClass(this));
							if (layout.Name != "layout")
								return;
						}
						//ProcessChildren(layout, vwenv, hvo, frag);
						DetermineNeededFieldsForChildren(layout, frag, info);
						break;
					case "obj":
						{
							// NOTE TO JohnT: perhaps "AddAtomicField" is really "AddSimpleField",
							// "AddObjField" is really "AddOwningOrReferenceField" ??
							// or split three ways instead of two? (one for AtomicRef/Own)
							int flid = 0;
							if (!TryCustomField(m_sda, frag, 0, out flid))
							{
								flid = DetermineNeededFlid(frag, info);
							}
							else
							{
								// TryCustomField may not be able to dertermine the flid
								// without the hvo, but might still be able to determine that
								// it's a custom field.
								if (flid == 0)
									flid = DetermineNeededFlid(frag, info);
								m_customFieldNode = frag;
							}
							if (flid == 0)
								return;
							int fragId = GetSubFragId(frag, caller);
							if (fragId == 0)
								return; // something badly wrong.

							if (info.SeqDepth < 3 && info.Depth < 10) // see comments on case seq
							{
								NeededPropertyInfo subinfo = info.AddObjField(flid, false);
								DisplayCommand dispCommand = m_idToDisplayCommand[fragId];
								dispCommand.DetermineNeededFields(this, fragId, subinfo);
							}
						}
						break;
					case "objlocal":
						{
							int flid = DetermineNeededFlid(frag, info);
							if (flid == 0)
								return;
							NeededPropertyInfo subinfo = info.AddObjField(flid, false);
							DisplayCommand dispCommand = new ObjLocalCommand(frag, caller);
							dispCommand.DetermineNeededFields(this, 0, subinfo);
						}
						break;
				}
			}
			catch (Exception)
			{
				// ignore, don't do any preloading.
			}
		}

		/// <summary>
		/// This combines some of the logic of GetFlid(hvo, frag) and FdoCache.GetFlid(hvo, class, field).
		/// Where possible it determines a flid that should be preloaded, adds it to info, and
		/// returns it.
		/// </summary>
		/// <param name="frag"></param>
		/// <param name="info"></param>
		/// <returns></returns>
		private int DetermineNeededFlid(XmlNode frag, NeededPropertyInfo info)
		{
			int flid = 0;
			try
			{
				IFwMetaDataCache mdc = m_sda.MetaDataCache;
				XmlAttribute xa = frag.Attributes["flid"];
				if (xa == null)
				{
					if (mdc == null)
						return 0; // can't do anything else sensible.
					// JohnT: try class, field props and look up in MetaDataCache.

					string stClassName = XmlUtils.GetOptionalAttributeValue(frag, "class");
					string stFieldName = XmlUtils.GetOptionalAttributeValue(frag, "field");
					if (String.IsNullOrEmpty(stFieldName) || stFieldName == "OwningFlid")
					{
						return 0;
					}
					else if (stFieldName == "OwnOrd" ||
						stFieldName == "Guid" ||
						stFieldName == "Owner" ||
						stFieldName == "Self")
					{
						// try a general purpose field that doesn't get treated as a 'base' class.
						flid = mdc.GetFieldId("CmObject", stFieldName, false);
					}
					else if (String.IsNullOrEmpty(stClassName))
					{
						int classId = info.TargetClass(this);
						flid = mdc.GetFieldId2(classId, stFieldName, true);
					}
					else
					{
						flid = mdc.GetFieldId(stClassName, stFieldName, true);
					}
				}
				else
				{
					flid = Convert.ToInt32(xa.Value, 10);
				}
			}
			catch
			{
				// Eat any exception.
			}

			return flid;
		}

		/// <summary>
		/// This gives the column we are displaying a chance to override the writing system for its part specification.
		/// </summary>
		/// <returns></returns>
		private int GetWritingSystemForObject(XmlNode frag, int hvo, int flid, int wsDefault)
		{
			if (WsForce == 0)
				return WritingSystemServices.GetWritingSystem(m_cache, m_sda, frag, s_qwsCurrent, hvo, flid, wsDefault).Handle;
			if (WsForce < 0) // magic.
			{
				// Forced magic ws. Find the corresponding actual WS.
				int wsActual;
				WritingSystemServices.GetMagicStringAlt(m_cache, m_sda, WsForce, hvo, flid, false, out wsActual);
				// If the magic ws doesn't get changed properly, use the default.
				if (wsActual <= 0)
					return wsDefault;
				return wsActual;
			}
			return WsForce;
		}

		private void DisplayOtherObjStringAlt(int flid, int ws, IVwEnv vwenv, int hvoTarget, XmlNode caller)
		{
			var source = IdentifySource ? caller : null;
			int fragId = GetId(new DisplayStringAltCommand(flid, ws, source), m_idToDisplayCommand, m_displayCommandToId);
			vwenv.AddObj(hvoTarget, this, fragId);
		}

		// Process a 'part ref' node (frag) for the specified object an env.
		internal void ProcessPartRef(XmlNode frag, int hvo, IVwEnv vwenv)
		{
			string layoutName = XmlUtils.GetManditoryAttributeValue(frag, "ref");
			XmlNode node;
			if (layoutName == "$child")
			{
				// The contents are right here. This is mainly used for generated part refs.
				node = frag;
			}
			else
			{
				node = GetNodeForPart(hvo, layoutName, false);
			}
			if (node == null)
				return;
			bool fSingleGramInfoFirst = XmlUtils.GetOptionalBooleanAttributeValue(frag, "singlegraminfofirst", false);
			// OTOH, if we're displaying the sequence of senses and suppressing gram info and this is the gram
			// info part, suppress it.
			if (fSingleGramInfoFirst && ShouldIgnoreGramInfo)
				return;
			try
			{
				m_stackPartRef.Insert(0, frag);
				var sVisibility = XmlUtils.GetOptionalAttributeValue(frag, "visibility", "always");
				switch (sVisibility)
				{
					case "never":
						return;
					case "ifdata":
						TestCollectorEnv envT = new TestCollectorEnv(vwenv, m_sda, hvo);
						envT.NoteEmptyDependencies = true;
						envT.MetaDataCache = m_sda.MetaDataCache;

						ProcessChildren(node, envT, hvo, frag);

						if (!envT.Result)
							return;
						break;
					// case "always":
					default: // treat anything we don't know as 'always'
						break;
				}
				// This groups senses by placing graminfo before the number, and omitting it if the same as the
				// previous sense in the entry.  This isn't yet supported by the UI, but may well be requested in
				// the future.  (See LT-9663.)
				//bool fGramInfoBeforeNumber = XmlUtils.GetOptionalBooleanAttributeValue(frag, "graminfobeforenumber", false);
				//if (fGramInfoBeforeNumber && m_tssDelayedNumber != null)
				//{
				//	if (node.FirstChild != null && node.FirstChild.Name == "obj")
				//	{
				//		int flid = GetFlid(node.FirstChild, hvo);
				//		int hvoValue = m_cache.GetObjProperty(hvo, flid);
				//		if (hvoValue == m_hvoGroupedValue)
				//		{
				//			vwenv.AddString(m_tssDelayedNumber);
				//			m_tssDelayedNumber = null;
				//			return;
				//		}
				//		m_hvoGroupedValue = hvoValue;
				//	}
				//}
				// We are going to display the contents of the part.
				string flowType;
				var style = PartRefSetupToProcessChildren(frag, node, hvo, vwenv, fSingleGramInfoFirst, out flowType);
				ProcessChildren(node, vwenv, hvo, frag);
				PartRefWrapupAfterProcessChildren(frag, vwenv, flowType, style);
				// This groups senses by placing graminfo before the number, and omitting it if the same as the
				// previous sense in the entry.  This isn't yet supported by the UI, but may well be requested in
				// the future.  (See LT-9663.)
				//if (fGramInfoBeforeNumber && m_tssDelayedNumber != null)
				//{
				//	vwenv.AddString(m_tssDelayedNumber);
				//	m_tssDelayedNumber = null;
				//}
			}
			finally
			{
				m_stackPartRef.RemoveAt(0);
			}
		}

		internal string PartRefSetupToProcessChildren(XmlNode frag, XmlNode node, int hvo, IVwEnv vwenv,
			bool fSingleGramInfoFirst, out string flowType)
		{
			string style = XmlUtils.GetOptionalAttributeValue(frag, "style", null);
			if (vwenv is ConfiguredExport)
				(vwenv as ConfiguredExport).BeginCssClassIfNeeded(frag);
			flowType = XmlUtils.GetOptionalAttributeValue(frag, "flowType", null);
			if (flowType == "para")
				GetParagraphStyleIfPara(hvo, ref style);
			// The literal string for a sequence should go before the place where we may pull out
			// a common part of speech within a single entry paragraph.
			// By default, though, (unless there is a beforeStyle) we do want it to have the style of this property.
			InsertLiteralString(frag, vwenv, "before", flowType, style);
			// If we pull out a common part of speech for several senses, we want it as part of the main paragraph,
			// not part of (say) the paragraph that belongs to the first sense. So we must insert this
			// before we process the flowType.
			if (fSingleGramInfoFirst)
			{
				// Will we really do gram info first? Only if we are numbering senses and either there is more than one,
				// or we are numbering even single senses.
				// For this to work, node must have just one child, a seq.
				SetupSingleGramInfoFirst(frag, node, hvo, vwenv);
			}
			if (style != null || flowType != null)
			{
				if (style != null && flowType != "divInPara")
					vwenv.set_StringProperty((int) FwTextPropType.ktptNamedStyle, style);
				switch (flowType)
				{
					default:
						vwenv.OpenSpan();
						break;
					case "para":
						vwenv.OpenParagraph();
						break;
					case "div":
						HandleNestedIndentation(frag, vwenv);
						vwenv.OpenDiv();
						break;
					case "none":
						break;
					case "divInPara":
						vwenv.CloseParagraph();
						vwenv.OpenDiv();
						break;
				}
			}
			return style;
		}

		internal void PartRefWrapupAfterProcessChildren(XmlNode frag, IVwEnv vwenv, string flowType, string style)
		{
			InsertLiteralString(frag, vwenv, "after", flowType, null); // don't need default style here, we're inside the main span for the property.
			if (style != null || flowType != null)
			{
					switch (flowType)
					{
						default:
							vwenv.CloseSpan();
							break;
						case "para":
							vwenv.CloseParagraph();
							break;
						case "div":
							vwenv.CloseDiv();
							break;
						case "none":
							break;
						case "divInPara":
							vwenv.CloseDiv();
							vwenv.OpenParagraph();
							// If we end up with an empty paragraph, try to make it disappear.
							vwenv.EmptyParagraphBehavior(1);
							break;
					}
				}
				if (vwenv is ConfiguredExport)
					(vwenv as ConfiguredExport).EndCssClassIfNeeded(frag);
		}

		private void SetupSingleGramInfoFirst(XmlNode frag, XmlNode node, int hvo, IVwEnv vwenv)
		{
			var seq = XmlUtils.GetFirstNonCommentChild(node);
			if (seq == null || seq.Name != "seq")
				return;
			int flid = GetFlid(seq, hvo);
			var rghvo = ((ISilDataAccessManaged)vwenv.DataAccess).VecProp(hvo, flid);

			XmlAttribute xaNum;
			var fNumber = XmlVcDisplayVec.SetNumberFlagIncludingSingleOption(frag, rghvo.Length, out xaNum);
			if (!fNumber)
				return;
			var fAllMsaSame = SetAllMsaSameFlag(rghvo.Length, rghvo);
			int childFrag = GetSubFragIdSeq(seq, frag);
			if (fAllMsaSame)
				DisplayFirstChildPOS(rghvo[0], childFrag, vwenv);

			// Exactly if we put out the grammatical info at the start, we need to NOT put it out
			// as part of each item. Note that we must not set this flag before we put out the one-and-only
			// gram info, or that will be suppressed too!
			ShouldIgnoreGramInfo = fAllMsaSame;
		}

		// Display the first child (item in the vector) in a special mode which suppresses everything except the child
		// marked singlegraminfofirst, to show the POS.
		private void DisplayFirstChildPOS(int firstChildHvo, int childFrag, IVwEnv vwenv)
		{
			var dispCommand = (MainCallerDisplayCommand)m_idToDisplayCommand[childFrag];
			string layoutName;
			var parent = dispCommand.GetNodeForChild(out layoutName, childFrag, this, firstChildHvo);
			if (DisplayFirstChildPos(firstChildHvo, parent, vwenv))
				return;
			// If we didn't find one, we are most likely in the publishRoot_AsPara wrapper layout.
			// See if we can drill down to the one that actually has the POS.
			var sublayout = XmlUtils.GetFirstNonCommentChild(parent);
			if (sublayout.Name != "sublayout")
				return;
			var sublayoutName = XmlUtils.GetAttributeValue(sublayout, "name");
			if (string.IsNullOrEmpty(sublayoutName))
				return;
			parent = GetNodeForPart(firstChildHvo, sublayoutName, true);
			if (parent == null || parent.Name != "layout")
				return;
			DisplayFirstChildPos(firstChildHvo, parent, vwenv);
		}

		// Display the first child (item in the vector) in a special mode which suppresses everything except the child
		// marked singlegraminfofirst, to show the POS.
		private bool DisplayFirstChildPos(int firstChildHvo, XmlNode parent, IVwEnv vwenv)
		{
			foreach (XmlNode gramInfoPartRef in parent.ChildNodes)
			{
				if (XmlUtils.GetOptionalBooleanAttributeValue(gramInfoPartRef, "singlegraminfofirst", false))
				{
					// It really is the gram info part ref we want.
					//m_viewConstructor.ProcessPartRef(gramInfoPartRef, firstChildHvo, m_vwEnv); no! the sense is not on the stack.
					var sVisibility = XmlUtils.GetOptionalAttributeValue(gramInfoPartRef, "visibility", "always");
					if (sVisibility == "never")
						return true; // user has configured gram info first, but turned off gram info.
					string morphLayoutName = XmlUtils.GetManditoryAttributeValue(gramInfoPartRef, "ref");
					var part = GetNodeForPart(firstChildHvo, morphLayoutName, false);
					if (part == null)
						throw new ArgumentException("Attempt to display gram info of first child, but part for " + morphLayoutName +
													" does not exist");
					var objNode = XmlUtils.GetFirstNonCommentChild(part);
					if (objNode == null || objNode.Name != "obj")
						throw new ArgumentException("Attempt to display gram info of first child, but part for " + morphLayoutName +
													" does not hav a single <obj> child");
					int flid = XmlVc.GetFlid(objNode, firstChildHvo, DataAccess);
					int hvoTarget = DataAccess.get_ObjectProp(firstChildHvo, flid);
					if (hvoTarget == 0)
						return true; // first sense has no category.
					int fragId = GetSubFragId(objNode, gramInfoPartRef);
					string flowType;
					try
					{
						m_stackPartRef.Insert(0, gramInfoPartRef);
						// This is the setup that invoking the part ref would normally do: wrap it in a flow and set a style if need be,
						// insert before labels, etc.
						var style = PartRefSetupToProcessChildren(gramInfoPartRef, objNode, hvoTarget, vwenv, true, out flowType);
						vwenv.AddObj(hvoTarget, this, fragId);
						// This is the cleanup that invoking the part ref would normally do after displaying the effect of the part referred to.
						// Closing any opened flow, adding following labels, etc.
						PartRefWrapupAfterProcessChildren(gramInfoPartRef, vwenv, flowType, style);
					}
					finally
					{
						m_stackPartRef.RemoveAt(0);
					}
					return true;
				}
			}
			return false;
		}

		private bool SetAllMsaSameFlag(int hvoCount, int[] msaHvoArray)
		{
			// if there are no Msa's then we can't display any, so we may as well say they aren't the same.
			if (hvoCount == 0)
			{
				return false;
			}
			var hvoMsa = m_sda.get_ObjectProp(msaHvoArray[0], LexSenseTags.kflidMorphoSyntaxAnalysis);
			var fAllMsaSame = SubsenseMsasMatch(msaHvoArray[0], hvoMsa);
			for (var i = 1; fAllMsaSame && i < hvoCount; ++i)
			{
				var hvoMsa2 = m_sda.get_ObjectProp(msaHvoArray[i], LexSenseTags.kflidMorphoSyntaxAnalysis);
				fAllMsaSame = hvoMsa == hvoMsa2 && SubsenseMsasMatch(msaHvoArray[i], hvoMsa);
			}
			return fAllMsaSame;
		}

		/// <summary>
		/// Check whether all the subsenses (if any) use the given MSA.
		/// </summary>
		private bool SubsenseMsasMatch(int hvoSense, int hvoMsa)
		{
			int[] rghvoSubsense = ((ISilDataAccessManaged)m_sda).VecProp(hvoSense, LexSenseTags.kflidSenses);
			for (var i = 0; i < rghvoSubsense.Length; ++i)
			{
				int hvoMsa2 = m_sda.get_ObjectProp(rghvoSubsense[i],
					LexSenseTags.kflidMorphoSyntaxAnalysis);
				if (hvoMsa != hvoMsa2 || !SubsenseMsasMatch(rghvoSubsense[i], hvoMsa))
					return false;
			}
			return true;
		}

		private void HandleNestedIndentation(XmlNode frag, IVwEnv vwenv)
		{
			if (!XmlUtils.GetOptionalBooleanAttributeValue(frag, "indent", false))
				return;
			var style = XmlUtils.GetOptionalAttributeValue(frag, "style");
			if (String.IsNullOrEmpty(style))
				style = XmlUtils.GetOptionalAttributeValue(frag, "parastyle");
			if (String.IsNullOrEmpty(style))
				return;
			var ttp = m_rootSite.StyleSheet.GetStyleRgch(style.Length, style);
			if (ttp == null)
				return;
			int nVar;
			var nVal = ttp.GetIntPropValues((int)FwTextPropType.ktptLeadingIndent, out nVar);
			if (nVal != -1 && nVar != -1)
				vwenv.set_IntProperty((int) FwTextPropType.ktptLeadingIndent, nVar, nVal);
		}

		private void DetermineNeededFieldsForPartRef(XmlNode frag, NeededPropertyInfo info)
		{
			string visibility = XmlUtils.GetOptionalAttributeValue(frag, "visibility", "always");
			if (visibility == "never")
				return;
			string layoutName = XmlUtils.GetManditoryAttributeValue(frag, "ref");
			XmlNode node;
			if (layoutName == "$child")
			{
				// The contents are right here. This is mainly used for generated part refs.
				node = frag;
			}
			else
			{
				int clsid = info.TargetClass(this);
				node = GetNodeForPart(layoutName, false, clsid);
			}
			if (node == null)
				return;
			DetermineNeededFieldsForChildren(node, frag, info);
		}

		private void InsertLiteralString(XmlNode frag, IVwEnv vwenv, string attrName, string flowType, string defaultStyle)
		{
			// When showasindentedpara is set true, the before string is displayed elsewhere, and the after
			// string is not displayed at all.  (Those are the only literal strings that use this method.)
			bool fShowAsParagraphs = XmlUtils.GetOptionalBooleanAttributeValue(frag, "showasindentedpara", false);
			if (fShowAsParagraphs)
				return;
			string item = XmlUtils.GetLocalizedAttributeValue(m_stringTable, frag, attrName, null);
			if (String.IsNullOrEmpty(item))
			{
				if (DelayedNumberExists)
					item = String.Empty;
				else
					return;
			}
			var tss = m_cache.TsStrFactory.MakeString(item,
				m_cache.ServiceLocator.WritingSystemManager.UserWs);
			var tssNumber = GetDelayedNumber(frag, vwenv is TestCollectorEnv);
			var sStyle = XmlUtils.GetAttributeValue(frag, attrName + "Style");
			var fMadePara = false;
			var fMadeDefaultStyleSpan = false;
			if (String.IsNullOrEmpty(sStyle))
			{
				if (!string.IsNullOrEmpty(defaultStyle))
				{
					vwenv.set_StringProperty((int)FwTextPropType.ktptNamedStyle, defaultStyle);
					vwenv.OpenSpan();
					fMadeDefaultStyleSpan = true;
				}
			}
			else // got a beforeStyle or afterStyle (sStyle != null)
			{
				if (flowType == "div")
				{
					fMadePara = true;
					vwenv.set_StringProperty((int) FwTextPropType.ktptNamedStyle, sStyle);
					vwenv.OpenParagraph();
				}
				else
				{
					ITsStrBldr bldr = tss.GetBldr();
					bldr.SetStrPropValue(0, bldr.Length, (int) FwTextPropType.ktptNamedStyle, sStyle);
					tss = bldr.GetString();
				}
			}
			if (tssNumber != null)
			{
				//int dmpx, dmpy;
				//vwenv.get_StringWidth(tss, null, out dmpx, out dmpy);
				ITsStrBldr tsb = tss.GetBldr();
				tsb.Replace(0, 0, tssNumber.Text, null);
				tss = tsb.GetString();
				//int dmpx2, dmpy2;
				//vwenv.get_StringWidth(tss, null, out dmpx2, out dmpy2);
				//int val = (dmpx2 - dmpx) * 1000;
				//tsb.SetIntPropValues(0, tsb.Length, (int)FwTextPropType.ktptFirstIndent,
				//	(int)FwTextPropVar.ktpvMilliPoint, val);
				//tss = tsb.GetString();
			}
			AddMarkedString(vwenv, frag, tss);
			if (fMadePara)
				vwenv.CloseParagraph();
			if (fMadeDefaultStyleSpan)
				vwenv.CloseSpan();
		}

		/// <summary>
		/// Get the identifier we want to use for the given node, which is typically a part ref
		/// </summary>
		/// <param name="whereFrom"></param>
		/// <returns></returns>
		string NodeIdentifier(XmlNode whereFrom)
		{
			var partRef = whereFrom;
			while (partRef != null && partRef.Name != "part")
				partRef = partRef.ParentNode;
			if (partRef == null)
				return "";
			Debug.Assert(m_stackPartRef.Count == 0 || partRef == m_stackPartRef[0], "PartRef " + XmlUtils.GetOptionalAttributeValue(partRef,"ref","NoRefFound") + " is not the first in the stack");
			int i = 1;
			while (XmlUtils.GetOptionalBooleanAttributeValue(partRef, "hideConfig", false)
				   && i < m_stackPartRef.Count)
			{   // No good telling the user about this part ref, he can't select it.
				// See if we have one up the stack that is good.
				partRef = m_stackPartRef[i++];
			}
			//if (XmlUtils.GetOptionalBooleanAttributeValue(partRef, "hideConfig", false))
			//{
			//    // No good telling the user about this part ref, he can't select it.
			//    // See if we have one up the stack that is good.
			//    for (int i =1; i < m_stackPartRef.Count; i++)
			//    {
			//        XmlNode parentPartRef = m_stackPartRef[i];
			//        if (parentPartRef == partRef)
			//            continue;
			//        if (!XmlUtils.GetOptionalBooleanAttributeValue(parentPartRef, "hideConfig", false))
			//        {
			//            partRef = parentPartRef;
			//            break; // got closest parent that is NOT hidden.
			//        }
			//    }
			//}
			var layout = partRef.ParentNode;
			var partRefId = XmlUtils.GetOptionalAttributeValue(partRef, "ref");
			if (partRefId == null)
			{
				// The node is in a part, not a part ref. Not configurable, can't do anything useful..
				return "";
			}
			Debug.Assert(layout.Name == "layout");
			var label = XmlUtils.GetOptionalAttributeValue(partRef, "label", partRefId);
			return XmlUtils.GetManditoryAttributeValue(layout, "class") + ":" +
				XmlUtils.GetManditoryAttributeValue(layout, "name") + ":" + partRefId + ":" + label;
		}


		/// <summary>
		/// Pass the string to the vwenv, but first, set a property that identifies the XmlNode from which it was generated.
		/// Using ktptBulNumTxtBef is a kludge. It saves inventing a new property, since bullet/number stuff is not relevant
		/// to sub-paragraph runs.
		/// </summary>
		internal void AddMarkedString(IVwEnv vwenv, XmlNode whereFrom, ITsString tss)
		{
			if (IdentifySource)
			{
				var bldr = tss.GetBldr();
				bldr.SetStrPropValue(0, tss.Length, (int)FwTextPropType.ktptBulNumTxtBef, NodeIdentifier(whereFrom));
				vwenv.AddString(bldr.GetString());
			}
			else
			vwenv.AddString(tss);
		}

		/// <summary>
		/// Pass the string to the vwenv, but first, set a property that identifies the XmlNode from which it was generated.
		/// Using ktptBulNumTxtBef is a kludge. It saves inventing a new property, since bullet/number stuff is not relevant
		/// to sub-paragraph runs.
		/// </summary>
		internal void AddMarkedString(IVwEnv vwenv, XmlNode whereFrom, string input, int ws)
		{
			var bldr = TsIncStrBldrClass.Create();
			bldr.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, ws);
			if (IdentifySource)
				bldr.SetStrPropValue((int)FwTextPropType.ktptBulNumTxtBef, NodeIdentifier(whereFrom));
			bldr.Append(input);
			vwenv.AddString(bldr.GetString());
		}

		internal void MarkSource(IVwEnv vwenv, XmlNode whereFrom)
		{
			if (!IdentifySource)
				return;
			if (vwenv is CollectorEnv)
				return; // don't need to do this in export or ifdata tests or sorting.
			vwenv.set_StringProperty((int)FwTextPropType.ktptBulNumTxtBef, NodeIdentifier(whereFrom));
		}

		internal void MarkSource(ITsPropsBldr tpb, XmlNode whereFrom)
		{
			if (!IdentifySource)
				return;
			tpb.SetStrPropValue((int)FwTextPropType.ktptBulNumTxtBef, NodeIdentifier(whereFrom));
		}

		internal bool DelayedNumberExists
		{
			get
			{
				return DelayNumFlag && m_tssDelayedNumber != null && m_tssDelayedNumber.Length > 0;
			}
		}

		internal ITsString GetDelayedNumber(XmlNode frag, bool fTest)
		{
			ITsString tssNumber = null;
			if (DelayedNumberExists)
			{
				bool fHide = XmlUtils.GetOptionalBooleanAttributeValue(frag, "hideConfig", false);
				if (!fHide)
				{
					tssNumber = m_tssDelayedNumber;
					if (!fTest)
					{
						DelayNumFlag = false;
						m_tssDelayedNumber = null;
					}
				}
			}
			return tssNumber;
		}

		internal void AddStringFromOtherObj(XmlNode frag, int hvoTarget, IVwEnv vwenv, XmlNode caller)
		{
			int flid = GetFlid(frag, hvoTarget);
			CellarPropertyType itype = (CellarPropertyType)m_sda.MetaDataCache.GetFieldType(flid);
			if (itype == CellarPropertyType.Unicode)
			{
				int fragId = GetId(new DisplayUnicodeCommand(
					flid,
					m_cache.DefaultUserWs),
					m_idToDisplayCommand,
					m_displayCommandToId);
				vwenv.AddObj(hvoTarget, this, fragId);
			}
			else if (itype == CellarPropertyType.String)
			{
				int fragId = GetId(new DisplayStringCommand(flid), m_idToDisplayCommand, m_displayCommandToId);
				vwenv.AddObj(hvoTarget, this, fragId);
			}
			else // multistring of some type
			{
				int wsid = 0;
				if (s_cwsMulti > 1)
				{
					string sLabelWs = XmlUtils.GetOptionalAttributeValue(frag, "ws");
					if (sLabelWs != null && sLabelWs == "current")
					{
						DisplayMultiSep(frag, vwenv, m_cache);
						DisplayWsLabel(s_qwsCurrent, vwenv, m_cache);
						wsid = s_qwsCurrent.Handle;
					}
				}
				if (wsid == 0)
					wsid = WritingSystemServices.GetWritingSystem(m_cache, frag, null, WritingSystemServices.kwsAnal).Handle;
				DisplayOtherObjStringAlt(flid, wsid, vwenv, hvoTarget, caller);
			}
		}

		/// <summary>
		/// We have a pair of dictionaries, one of which maps int to some key value,
		/// and the other which does the reverse. If key is already a key in keyToId,
		/// just return the value stored there. If not, allocate a new integer,
		/// and add a record to both tables.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="idToKey"></param>
		/// <param name="keyToId"></param>
		/// <returns></returns>
		internal int GetId(DisplayCommand key, Dictionary<int, DisplayCommand> idToKey, Dictionary<DisplayCommand, int> keyToId)
		{
			if (!keyToId.ContainsKey(key))
			{
				int id = m_nextID++;
				idToKey[id] = key;
				keyToId[key] = id;
			}
			return keyToId[key];
		}

		internal int GetId(DisplayCommand command)
		{
			return GetId(command, m_idToDisplayCommand, m_displayCommandToId);
		}

		internal void RemoveCommand(DisplayCommand command, int id)
		{
			m_idToDisplayCommand.Remove(id);
			m_displayCommandToId.Remove(command);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add an object to the display.
		/// Set up a call to vwenv.AddObjProp.
		/// This depends on whether the caller has filtering information.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void AddObject(XmlNode frag, IVwEnv vwenv, int flid, int fragId, XmlNode caller, int hvo)
		{
			if (caller != null)
			{
				var guidsFilter = XmlUtils.GetOptionalAttributeValue(caller, "entrytypeseq");
				if (!String.IsNullOrEmpty(guidsFilter))
				{
					// If we have filtering set to show only certain types of minor entries,
					// apply the filter before we try adding the object to the view.  (LT-10953)
					var validTypes = new Set<Guid>();
					var entryType = XmlUtils.GetOptionalAttributeValue(caller, "entrytype");
					Debug.Assert(entryType == "minor");
					var rgsGuidsPlus = guidsFilter.Split(',');
					for (var i = 0; i < rgsGuidsPlus.Length; ++i)
					{
						var s = rgsGuidsPlus[i];
						if (s.StartsWith("-"))
							continue;
						validTypes.Add(new Guid(s.Substring(1)));
					}
					var hvoObj = m_sda.get_ObjectProp(hvo, flid);
					if (hvoObj != 0 && validTypes.Count > 0)
					{
						var repo = m_cache.ServiceLocator.GetInstance<ILexEntryRepository>();
						ILexEntry entry;
						if (repo.TryGetObject(hvoObj, out entry) && entry.EntryRefsOS.Count > 0)
						{
							var fOk = false;	// assume we don't want this entry.
							foreach (var entryref in entry.EntryRefsOS.Where(entryref => entryref.HideMinorEntry == 0))
							{
								switch (entryref.RefType)
								{
									case LexEntryRefTags.krtComplexForm:
										if (!fOk && entryref.ComplexEntryTypesRS.Any(
											type => validTypes.Contains(type.Guid)) ||
											(entryref.ComplexEntryTypesRS.Count == 0 && validTypes.Contains(m_unspecComplexFormType)))
										{
											fOk = true;
										}
										break;
									case LexEntryRefTags.krtVariant:
										if (!fOk && entryref.VariantEntryTypesRS.Any(
											type => validTypes.Contains(type.Guid)) ||
											(entryref.VariantEntryTypesRS.Count == 0 && validTypes.Contains(m_unspecVariantType)))
										{
											fOk = true;
										}
										break;
									default:
										Debug.Fail(String.Format("Unknown LexEntryRef type: {0}", entryref.RefType));
										return;
								}
							}
							if (!fOk)
								return;
						}
					}
				}
			}
			// If the value of the property is null, we used to get an Assert by adding it.
			// Now the Views code is smarter, and generates a useful dependency to regenerate
			// if the value gets set.
			vwenv.AddObjProp(flid, this, fragId);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a vector to the display.
		/// fragId: a fragCaller id
		/// Set up a call to AddObjVec or AddObjVecItems as appropriate.
		/// This depends on whether the target fragment has delimiter info.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void AddObjectVector(XmlNode frag, IVwEnv vwenv, int flid, int fragId, XmlNode caller)
		{
			XmlNode itemDecorationNode;
			XmlNode sequenceOptionNode;
			// seq stuff on the frag node
			itemDecorationNode = sequenceOptionNode = frag;
			if (XmlUtils.GetOptionalBooleanAttributeValue(itemDecorationNode, "inheritSeps", false))
				itemDecorationNode = caller;
			XmlAttribute xaSep = itemDecorationNode == null ? null : itemDecorationNode.Attributes["sep"];
			XmlAttribute xaNum = itemDecorationNode == null ? null : itemDecorationNode.Attributes["number"];
			// Note that we deliberately don't use itemDecorationNode here. "excludeHvo" is not a separator property,
			// nor configurable, so it belongs on the 'seq' element, not the part ref.
			var exclude = XmlUtils.GetOptionalAttributeValue(sequenceOptionNode, "excludeHvo", null);
			var fFirstOnly = XmlUtils.GetOptionalBooleanAttributeValue(sequenceOptionNode, "firstOnly", false);
			var sort = XmlUtils.GetOptionalAttributeValue(sequenceOptionNode, "sort", null);
			var fNumber = xaNum != null && xaNum.Value != null && xaNum.Value != "";
			var fSep = xaSep != null && xaSep.Value != null && xaSep.Value != "";
			bool isDivInPara = XmlUtils.GetOptionalAttributeValue(itemDecorationNode, "flowType", "").Equals("divInPara");
			if (caller != null)
			{
				var guidsFilter = XmlUtils.GetOptionalAttributeValue(caller, "reltypeseq");
				if (!String.IsNullOrEmpty(guidsFilter))
				{
					var rgsGuidsPlus = guidsFilter.Split(',');
					m_mapGuidToReferenceInfo = new Dictionary<Guid, List<LexReferenceInfo>>();
					for (var i = 0; i < rgsGuidsPlus.Length; ++i)
					{
						var s = rgsGuidsPlus[i];
						if (s.StartsWith("-"))
							continue;
						var lri = new LexReferenceInfo(s) {Index = i};
						List<LexReferenceInfo> lris;
						if (!m_mapGuidToReferenceInfo.TryGetValue(lri.ItemGuid, out lris))
						{
							lris = new List<LexReferenceInfo>();
							m_mapGuidToReferenceInfo.Add(lri.ItemGuid, lris);
						}
						lris.Add(lri);
					}
					if (m_mapGuidToReferenceInfo.Count == 0)
						m_mapGuidToReferenceInfo = null; // probably only paranoia...
				}
				else
				{
					var entryType = XmlUtils.GetOptionalAttributeValue(caller, "entrytype");
					guidsFilter = XmlUtils.GetOptionalAttributeValue(caller, "entrytypeseq");
					if (!String.IsNullOrEmpty(guidsFilter))
					{
						switch (entryType)
						{
							case "complex":
								m_mapGuidToComplexRefInfo = BuildGuidToItemTypeInfoMap(guidsFilter);
								break;
							case "variant":
								m_mapGuidToVariantRefInfo = BuildGuidToItemTypeInfoMap(guidsFilter);
								break;
							default:
								Debug.Fail(String.Format("Unknown entryType: {0}", entryType));
								break;
						}
					}
				}
			}
			if (fNumber || fSep || exclude != null || fFirstOnly || sort != null ||
				m_mapGuidToReferenceInfo != null || m_mapGuidToComplexRefInfo != null ||
				m_mapGuidToVariantRefInfo != null || isDivInPara)
			{
				// This results in DisplayVec being called.
				vwenv.AddObjVec(flid, this, fragId);
				m_mapGuidToReferenceInfo = null;
				m_mapGuidToComplexRefInfo = null;
				m_mapGuidToVariantRefInfo = null;
			}
			else
			{
				// We can (and sometimes must, and always should for efficiency) just display
				// each item.  This allows us to use seq, for example, to add cells to a
				// table row.  It also allows the view to insert and delete items without
				// redoing everything.
				bool fReverse = XmlUtils.GetOptionalBooleanAttributeValue(frag, "reverse", false);
				if (fReverse)
				{
					// we need to add these in reverse!
					vwenv.AddReversedObjVecItems(flid, this, fragId);
				}
				else
				{
					vwenv.AddObjVecItems(flid, this, fragId);
				}
			}
		}

		private static Dictionary<Guid, ItemTypeInfo> BuildGuidToItemTypeInfoMap(string guidsFilter)
		{
			var result = new Dictionary<Guid, ItemTypeInfo>();
			var rgsGuidsPlus = guidsFilter.Split(',');
			for (var i = 0; i < rgsGuidsPlus.Length; ++i)
			{
				var s = rgsGuidsPlus[i];
				if (s.StartsWith("-"))
					continue;
				var info = new ItemTypeInfo(s) { Index = i };
				result.Add(info.ItemGuid, info);
			}
			if (result.Count == 0)
				result = null;
			return result;
		}

		/// <summary>
		/// This version allows the IVwEnv to be optional
		/// </summary>
		/// <param name="frag">the 'if' node</param>
		/// <param name="hvo">The hvo.</param>
		/// <param name="cache">The cache.</param>
		/// <param name="sda">The sda.</param>
		/// <param name="caller">The caller.</param>
		/// <returns></returns>
		public static bool ConditionPasses(XmlNode frag, int hvo, FdoCache cache, ISilDataAccess sda, XmlNode caller)
		{
			return ConditionPasses(null, frag, hvo, cache, sda, caller);
		}

		/// <summary>
		/// This version allows the IVwEnv and caller to be omitted
		/// </summary>
		/// <param name="frag">the 'if' node</param>
		/// <param name="hvo">The hvo.</param>
		/// <param name="cache">The cache.</param>
		/// <param name="sda">The sda.</param>
		/// <returns></returns>
		public static bool ConditionPasses(XmlNode frag, int hvo, FdoCache cache, ISilDataAccess sda)
		{
			return ConditionPasses(null, frag, hvo, cache, sda, null);
		}

		/// <summary>
		/// This version allows the IVwEnv, caller, and ISilDataAccess to be omitted
		/// </summary>
		/// <param name="frag">The frag.</param>
		/// <param name="hvo">The hvo.</param>
		/// <param name="cache">The cache.</param>
		/// <returns></returns>
		public static bool ConditionPasses(XmlNode frag, int hvo, FdoCache cache)
		{
			return ConditionPasses(null, frag, hvo, cache, cache.DomainDataByFlid, null);
		}

		/// <summary>
		/// This method looks for the target attribute of an if or where element and if
		/// found switches the hvo value to that of its owner if its value is owner.
		/// </summary>
		/// <param name="frag">xml node</param>
		/// <param name="hvo">object hvo</param>
		/// <param name="sda">The sda.</param>
		public static void GetActualTarget(XmlNode frag, ref int hvo, ISilDataAccess sda)
		{
			var target = XmlUtils.GetOptionalAttributeValue(frag, "target", "this");
			switch (target.ToLower())
			{
				case "owner":
					int flidOwner = sda.MetaDataCache.GetFieldId("CmObject", "Owner", false);
					hvo = sda.get_ObjectProp(hvo, flidOwner);
					break;
				case "this":
					break;
				default:
					if (!String.IsNullOrEmpty(target))
					{
						int clid = sda.get_IntProp(hvo, CmObjectTags.kflidClass);
						int flid = sda.MetaDataCache.GetFieldId2(clid, target, true);
						hvo = sda.get_ObjectProp(hvo, flid);
					}
					break;
			}
		}

		/// <summary>
		/// Evaluate a condition expressed in the frag node.
		/// May be is="classname", requires hvo to be that class or a subclass,
		/// unless excludesubclasses="true" in which case must match exactly.
		/// May be field="name" plus any of lengthatleast="n", lengthatmost="n",
		/// stringequals="xx", stringaltequals="xx", boolequals="true/false", intequals="n",
		/// intgreaterthan="n", or intlessthan="n".
		/// More than one condition may be present, if so, all must pass.
		/// Class of object condition is tested first if present, so property may be a property
		/// possessed only by that class of object.
		/// </summary>
		/// <param name="vwenv">Environment in which evaluated, for NoteDependency. May be null.</param>
		/// <param name="frag">The frag.</param>
		/// <param name="hvo">The hvo.</param>
		/// <param name="cache">The cache.</param>
		/// <param name="sda">The sda.</param>
		/// <param name="caller">the 'part ref' node that invoked the current part. May be null if XML does not use it.</param>
		/// <returns></returns>
		public static bool ConditionPasses(IVwEnv vwenv, XmlNode frag, int hvo, FdoCache cache, ISilDataAccess sda, XmlNode caller)
		{
			GetActualTarget(frag, ref hvo, sda);	// modify the hvo if needed

			if (!IsConditionsPass(frag, hvo, sda))
				return false;
			if (!LengthConditionsPass(vwenv, frag, hvo, sda))
				return false;
			if (!ValueEqualityConditionsPass(vwenv, frag, hvo, cache, sda, caller))
				return false;
			if (!BidiConditionPasses(frag, cache))
				return false;
			return true; // All conditions present passed.
		}

		/// <summary>
		/// Returns true if the vernacular and analysis writing systems have opposite
		/// directionality, ie, one is RTL and the other is LTR, matching against the boolean
		/// argument.  &lt;if bidi="false"&gt; is the same as &lt;ifnot bidi="true"&gt;.
		/// </summary>
		/// <param name="frag"></param>
		/// <param name="cache"></param>
		/// <returns></returns>
		private static bool BidiConditionPasses(XmlNode frag, FdoCache cache)
		{
			string sBidi = XmlUtils.GetOptionalAttributeValue(frag, "bidi");
			if (sBidi != null)
			{
				IWritingSystemContainer wsContainer = cache.ServiceLocator.WritingSystems;
				bool fBidi = XmlUtils.GetBooleanAttributeValue(sBidi);
				bool fRTLVern = wsContainer.DefaultVernacularWritingSystem.RightToLeftScript;
				bool fRTLAnal = wsContainer.DefaultAnalysisWritingSystem.RightToLeftScript;
				if (fRTLVern == fRTLAnal)
					return !fBidi;
				else
					return fBidi;
			}
			return true;
		}
		/// <summary>
		/// Check for ValueEquality conditions
		/// </summary>
		/// <param name="vwenv">The vwenv.</param>
		/// <param name="frag">The frag.</param>
		/// <param name="hvo">The hvo.</param>
		/// <param name="cache">The cache.</param>
		/// <param name="sda">The sda.</param>
		/// <param name="caller">The caller.</param>
		/// <returns></returns>
		static private bool ValueEqualityConditionsPass(IVwEnv vwenv, XmlNode frag, int hvo, FdoCache cache,
			ISilDataAccess sda, XmlNode caller)
		{
			if (!StringEqualsConditionPasses(vwenv, frag, hvo, sda))
				return false;
			if (!StringAltEqualsConditionPasses(vwenv, frag, hvo, cache, sda, caller))
				return false;
			if (!BoolEqualsConditionPasses(vwenv, frag, hvo, sda))
				return false;
			if (!IntEqualsConditionPasses(vwenv, frag, hvo, sda))
				return false;
			if (!IntGreaterConditionPasses(vwenv, frag, hvo, sda))
				return false;
			if (!IntLessConditionPasses(vwenv, frag, hvo, sda))
				return false;
			if (!IntMemberOfConditionPasses(vwenv, frag, hvo, sda))
				return false;
			if (!GuidEqualsConditionPasses(vwenv, frag, hvo, sda))
				return false;
			if (!HvoEqualsConditionPasses(vwenv, frag, hvo, sda))
				return false;
			if (!FlidEqualsConditionPasses(vwenv, frag, hvo, sda))
				return false;
			return true;
		}

		static private void NoteDependency(IVwEnv vwenv, int hvo, int flid)
		{
			if (vwenv == null)
				return;
			vwenv.NoteDependency(new int[] { hvo }, new int[] { flid }, 1);
		}

		static private int GetValueFromCache(IVwEnv vwenv, XmlNode frag, int hvo, ISilDataAccess sda)
		{
			int flid = GetFlidAndHvo(vwenv, frag, ref hvo, sda);
			if (flid == -1 || hvo == 0)
				return 0; // This is rather arbitrary...objects missing, what should each test do?
			NoteDependency(vwenv, hvo, flid);
			return sda.get_IntProp(hvo, flid);
		}

		static private bool GetBoolValueFromCache(IVwEnv vwenv, XmlNode frag, int hvo, ISilDataAccess sda)
		{
			int flid = GetFlidAndHvo(vwenv, frag, ref hvo, sda);
			if (flid == -1 || hvo == 0)
				return false; // This is rather arbitrary...objects missing, what should each test do?
			NoteDependency(vwenv, hvo, flid);
			return IntBoolPropertyConverter.GetBoolean(sda, hvo, flid);
		}

		/// <summary>
		/// Check for "intequals" attribute condition
		/// </summary>
		/// <param name="vwenv">For NoteDependency</param>
		/// <param name="frag">The frag.</param>
		/// <param name="hvo">The hvo.</param>
		/// <param name="sda">The sda.</param>
		/// <returns></returns>
		static private bool IntEqualsConditionPasses(IVwEnv vwenv, XmlNode frag, int hvo, ISilDataAccess sda)
		{
			int intValue = XmlUtils.GetOptionalIntegerValue(frag, "intequals", -2);	// -2 might be valid
			int intValue2 = XmlUtils.GetOptionalIntegerValue(frag, "intequals", -3);	// -3 might be valid
			if (intValue != -2 || intValue2 != -3)
			{
				int value = GetValueFromCache(vwenv, frag, hvo, sda);
				if (value != intValue)
					return false;
			}
			return true;
		}

		/// <summary>
		/// Check for "intgreaterthan" attribute condition
		/// </summary>
		/// <param name="vwenv">For NoteDependency</param>
		/// <param name="frag">The frag.</param>
		/// <param name="hvo">The hvo.</param>
		/// <param name="sda">The sda.</param>
		/// <returns></returns>
		static private bool IntGreaterConditionPasses(IVwEnv vwenv, XmlNode frag, int hvo, ISilDataAccess sda)
		{
			int intValue = XmlUtils.GetOptionalIntegerValue(frag, "intgreaterthan", -2);	// -2 might be valid
			int intValue2 = XmlUtils.GetOptionalIntegerValue(frag, "intgreaterthan", -3);	// -3 might be valid
			if (intValue != -2 || intValue2 != -3)
			{
				int value = GetValueFromCache(vwenv, frag, hvo, sda);
				if (value <= intValue)
					return false;
			}
			return true;
		}

		/// <summary>
		/// Check for "intlessthan" attribute condition
		/// </summary>
		/// <param name="vwenv">For NoteDependency</param>
		/// <param name="frag">The frag.</param>
		/// <param name="hvo">The hvo.</param>
		/// <param name="sda">The sda.</param>
		/// <returns></returns>
		static private bool IntLessConditionPasses(IVwEnv vwenv, XmlNode frag, int hvo, ISilDataAccess sda)
		{
			int intValue = XmlUtils.GetOptionalIntegerValue(frag, "intlessthan", -2);	// -2 might be valid
			int intValue2 = XmlUtils.GetOptionalIntegerValue(frag, "intlessthan", -3);	// -3 might be valid
			if (intValue != -2 || intValue2 != -3)
			{
				int value = GetValueFromCache(vwenv, frag, hvo, sda);
				if (value >= intValue)
					return false;
			}
			return true;
		}

		/// <summary>
		/// Check for "intmemberof" attribute condition
		/// </summary>
		/// <param name="vwenv">For NoteDependency</param>
		/// <param name="frag">The frag.</param>
		/// <param name="hvo">The hvo.</param>
		/// <param name="sda">The sda.</param>
		/// <returns></returns>
		static private bool IntMemberOfConditionPasses(IVwEnv vwenv, XmlNode frag, int hvo, ISilDataAccess sda)
		{
			string stringValue = XmlUtils.GetOptionalAttributeValue(frag, "intmemberof");
			if (stringValue == null)
				return true;
			string[] rgsNum = stringValue.Split(',');
			int val = GetValueFromCache(vwenv, frag, hvo, sda);
			for (int i = 0; i < rgsNum.Length; i++)
			{
				try
				{
					int intVal = Int32.Parse(rgsNum[i], CultureInfo.InvariantCulture);
					if (val == intVal)
						return true;
				}
				catch
				{
					// ignore invalid data.
				}
			}
			return false;
		}

		/// <summary>
		/// Check for "flidequals" attribute condition. True if the given field matches the id in flidequals.
		/// </summary>
		/// <param name="vwenv">The vwenv.</param>
		/// <param name="frag">The frag.</param>
		/// <param name="hvo">The hvo.</param>
		/// <param name="sda">The sda.</param>
		/// <returns></returns>
		static private bool FlidEqualsConditionPasses(IVwEnv vwenv, XmlNode frag, int hvo, ISilDataAccess sda)
		{
			string sFlid = XmlUtils.GetOptionalAttributeValue(frag, "flidequals");
			if (sFlid == null)
				return true;
			int flidVal = 0;
			if (Int32.TryParse(sFlid, out flidVal))
			{
				string fieldName = XmlUtils.GetManditoryAttributeValue(frag, "field");
				int flid = GetFlid(frag, hvo, sda);
				return flid == flidVal;
			}
			return false;
		}
		/// <summary>
		/// Check for "hvoequals" attribute condition
		/// </summary>
		/// <param name="vwenv">For NoteDependency</param>
		/// <param name="frag">The frag.</param>
		/// <param name="hvo">The hvo.</param>
		/// <param name="sda">The sda.</param>
		/// <returns></returns>
		static private bool HvoEqualsConditionPasses(IVwEnv vwenv, XmlNode frag, int hvo, ISilDataAccess sda)
		{
			string sHvo = XmlUtils.GetOptionalAttributeValue(frag, "hvoequals");
			if (sHvo == null)
				return true;
			int val;
			string sIndex = XmlUtils.GetOptionalAttributeValue(frag, "index");
			if (sIndex == null)
			{
				int flid = GetFlid(frag, hvo, sda);
				val = sda.get_ObjectProp(hvo, flid);
			}
			else
			{
				int index;
				try
				{
					index = Convert.ToInt32(sIndex, 10);
					int flid = GetFlid(frag, hvo, sda);
					val = sda.get_VecItem(hvo, flid, index);
				}
				catch
				{
					return false;
				}
			}
			if (sHvo == "$ThisHvo")
			{
				// compare with the object that has the sequence property.
				if (val == hvo)
					return true;
			}
			else if (sHvo == "$ParentHvo")
			{
				// compare with the next outer HVO in the hierarchy.
				int hvoParent, tagDummy, ihvoDummy;
				vwenv.GetOuterObject(vwenv.EmbeddingLevel - 1, out hvoParent, out tagDummy, out ihvoDummy);
				if (val == hvoParent)
					return true;
			}
			return false;
		}

		/// <summary>
		/// Check for the "guidequals" attribute condition.
		/// </summary>
		/// <param name="vwenv">The vwenv.</param>
		/// <param name="frag">The frag.</param>
		/// <param name="hvo">The hvo.</param>
		/// <param name="sda">The sda.</param>
		/// <returns></returns>
		static private bool GuidEqualsConditionPasses(IVwEnv vwenv, XmlNode frag, int hvo, ISilDataAccess sda)
		{
			XmlAttribute xa = frag.Attributes["guidequals"];
			if (xa == null)
				return true;

			int flid = GetFlidAndHvo(vwenv, frag, ref hvo, sda);
			if (flid == -1 || hvo == 0)
				return false; // object is not there, can't have expected value of property.
			NoteDependency(vwenv, hvo, flid);
			int hvoObj = sda.get_ObjectProp(hvo, flid);
			Guid guid = new Guid(xa.Value);
			Guid guidObj = Guid.Empty;
			if (hvoObj != 0)
				guidObj = sda.get_GuidProp(hvoObj, CmObjectTags.kflidGuid);

			return guid == guidObj;
		}

		/// <summary>
		/// Check for "boolequals" attribute condition. Passes if "boolequals" not found.
		/// </summary>
		/// <param name="vwenv">The vwenv.</param>
		/// <param name="frag">The frag.</param>
		/// <param name="hvo">The hvo.</param>
		/// <param name="sda">The sda.</param>
		/// <returns></returns>
		static private bool BoolEqualsConditionPasses(IVwEnv vwenv, XmlNode frag, int hvo, ISilDataAccess sda)
		{
			string boolValue = XmlUtils.GetOptionalAttributeValue(frag, "boolequals", "notFound");	// must be either 'true' or 'false'.
			if (boolValue != "notFound")
			{
				return GetBoolValueFromCache(vwenv, frag, hvo, sda) == (boolValue == "true"?true:false);
			}
			return true;
		}

		static private bool StringAltEquals(string sValue, int hvo, int flid, int ws, ISilDataAccess sda)
		{
			string sAlt = sda.get_MultiStringAlt(hvo, flid, ws).Text;
			if (sAlt == null && sValue == "")
				return true;
			else
				return (sAlt == sValue);
		}

		/// <summary>
		/// Check for "stringaltequals" attribute condition
		/// </summary>
		/// <param name="vwenv">The vwenv.</param>
		/// <param name="frag">The frag.</param>
		/// <param name="hvo">The hvo.</param>
		/// <param name="cache">The cache.</param>
		/// <param name="sda">The sda.</param>
		/// <param name="caller">The caller.</param>
		/// <returns></returns>
		static private bool StringAltEqualsConditionPasses(IVwEnv vwenv, XmlNode frag, int hvo,
			FdoCache cache, ISilDataAccess sda, XmlNode caller)
		{
			string stringAltValue = XmlUtils.GetOptionalAttributeValue(frag, "stringaltequals");
			if (stringAltValue != null)
			{
				// Bad idea, since a ws attr with "all analysis" will return -1,
				// which is a magic ws value, but not the right one for "all analysis".
				// What's even worse than the bogus -1 value, is that a new writing system will be created
				// with an icu locale of "all analysis". Not good.
				// Well, it would have created it, but I fixed that bug, also.
				//int ws = LgWritingSystem.GetWritingSystem(frag, cache).Hvo;
				int flid = GetFlidAndHvo(vwenv, frag, ref hvo, sda);
				if (flid == -1 || hvo == 0)
					return false; // object is not there, can't have expected value of property.
				string wsId = XmlUtils.GetOptionalAttributeValue(frag, "ws");
				// Note: the check for s_qwsCurrent not null here is a desperation move. It prevents
				// a crash if "current" is misused in a context where the thing using "current"
				// can be regenerated without regenerating the thing that has the loop.
				if (wsId == "current" && s_qwsCurrent != null)
				{
					NoteStringValDependency(vwenv, hvo, flid, s_qwsCurrent.Handle, stringAltValue);
					if (!StringAltEquals(stringAltValue, hvo, flid, s_qwsCurrent.Handle, sda))
						return false;
				}
				else
				{
					// If ws is 'configure' then we must have a caller to inherit the configured value from.
					if (wsId == "configure")
					{
						wsId = XmlUtils.GetManditoryAttributeValue(caller, "ws");
					}
					foreach (int ws in WritingSystemServices.GetAllWritingSystems(cache, wsId, null, hvo, flid))
					{
						NoteStringValDependency(vwenv, hvo, flid, ws, stringAltValue);
						if (!StringAltEquals(stringAltValue, hvo, flid, ws, sda))
							return false;
					}
				}
			}
			return true;
		}

		static void NoteStringValDependency(IVwEnv vwenv, int hvo, int flid, int ws, string val)
		{
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			vwenv.NoteStringValDependency(hvo, flid, ws, tsf.MakeString(val, ws));
		}

		/// <summary>
		/// Check for "stringequals" attribute condition
		/// </summary>
		/// <param name="vwenv">For NoteDependency</param>
		/// <param name="frag">The frag.</param>
		/// <param name="hvo">The hvo.</param>
		/// <param name="sda">The sda.</param>
		/// <returns></returns>
		static private bool StringEqualsConditionPasses(IVwEnv vwenv, XmlNode frag, int hvo, ISilDataAccess sda)
		{
			string stringValue = XmlUtils.GetOptionalAttributeValue(frag, "stringequals");
			if (stringValue != null)
			{
				int flid = GetFlidAndHvo(vwenv, frag, ref hvo, sda);
				string value = null;
				if (flid != -1 && hvo != 0)
				{
					ITsString tsString = sda.get_StringProp(hvo, flid);
					int var;
					int realWs = tsString.get_Properties(0).GetIntPropValues((int) FwTextPropType.ktptWs, out var);
					ITsStrFactory tsf = TsStrFactoryClass.Create();
					// Third argument must be 0 to indicate a non-multistring.
					// Fourth argument is the TsString version of the string we are testing against.
					// The display will update for a change in whether it is true that sda.get_StringProp(hvo, flid) is equal to arg4
					vwenv.NoteStringValDependency(hvo, flid, 0, tsf.MakeString(stringValue, realWs));
					value = tsString.Text;
				} // otherwise we don't have an object, and will treat the current value as null.
				if (value == null && stringValue == "")
					return true;
				if (value != stringValue)
					return false;
			}
			return true;
		}

		/// <summary>
		/// Check for "lengthatmost" and/or "lengthatleast" attribute condition
		/// </summary>
		/// <param name="vwenv">For NoteDependency</param>
		/// <param name="frag">The frag.</param>
		/// <param name="hvo">The hvo.</param>
		/// <param name="sda">The sda.</param>
		/// <returns></returns>
		static private bool LengthConditionsPass(IVwEnv vwenv, XmlNode frag, int hvo, ISilDataAccess sda)
		{
			// Class of object condition passes, if any.
			int maxlen = XmlUtils.GetOptionalIntegerValue(frag, "lengthatmost", -1);
			int minlen = XmlUtils.GetOptionalIntegerValue(frag, "lengthatleast", -1);
			if (maxlen >= 0 || minlen >= 0)
			{

				int len = 0;
				if (maxlen == -1)
					maxlen = Int32.MaxValue;
				int flid = GetFlid(frag, hvo, sda);
				NoteDependency(vwenv, hvo, flid);
				int fldType = sda.MetaDataCache.GetFieldType(flid);
				if (fldType == (int)CellarPropertyType.OwningSequence
					|| fldType == (int)CellarPropertyType.ReferenceCollection
					|| fldType == (int)CellarPropertyType.OwningCollection
					|| fldType == (int)CellarPropertyType.ReferenceSequence)
				{
					len = sda.get_VecSize(hvo, flid);
				}
				else if (fldType == (int)CellarPropertyType.OwningAtomic
					|| fldType == (int)CellarPropertyType.ReferenceAtomic)
				{
					int hvoItem = sda.get_ObjectProp(hvo, flid);
					len = hvoItem == 0 ? 0 : 1;
				}
				// a virtual flid; a negative-valued flid indicates that it's a virtual.
				else if ((fldType == 0) && (flid < 0))
				{
					len = sda.get_VecSize(hvo, flid);
				}

				if (len > maxlen || len < minlen)
					return false;
			}
			return true;
		}

		/// <summary>
		/// Check for an "is" attribute condition
		/// </summary>
		/// <param name="frag">The frag.</param>
		/// <param name="hvo">The hvo.</param>
		/// <param name="sda">The sda.</param>
		/// <returns>
		/// 	<c>true</c> if [is condition passes] [the specified frag]; otherwise, <c>false</c>.
		/// </returns>
		static private bool IsConditionPasses(XmlNode frag, int hvo, ISilDataAccess sda)
		{
			string className = XmlUtils.GetOptionalAttributeValue(frag, "is");
			if (className != null)
			{
				var mdc = sda.MetaDataCache;
				// 'is' condition present, evaluate it.
				int uclsidObj = sda.get_IntProp(hvo, CmObjectTags.kflidClass);
				int uclsidArg = mdc.GetClassId(className);
				if (uclsidObj != uclsidArg)
				{
					// Not an exact match, if excluding subclasses, condition fails.
					if (XmlUtils.GetOptionalBooleanAttributeValue(frag, "excludesubclasses", false))
						return false;
					//if the uclsidObj is 0, then the target we are investigating is probably null, this counts as failure in my book.
					int uclsid = uclsidObj != 0 ? mdc.GetBaseClsId(uclsidObj) : 0;
					// Otherwise OK if clsidObj is a subclass of clsidArg
					while (uclsid != 0)
					{
						if (uclsid == uclsidArg)
						{
							break;
						}
						uclsid = mdc.GetBaseClsId(uclsid);
					}
					if (uclsid == 0)
						return false;  // condition failed, not a subclass
				}
			}
			return true;
		}

		/// <summary>
		/// Check for attribute conditions that require specific objects to be of a certain class.
		/// </summary>
		/// <param name="frag">The frag.</param>
		/// <param name="hvo">The hvo.</param>
		/// <param name="sda">The sda.</param>
		/// <returns>
		/// 	<c>true</c> if [is conditions pass] [the specified frag]; otherwise, <c>false</c>.
		/// </returns>
		static private bool IsConditionsPass(XmlNode frag, int hvo, ISilDataAccess sda)
		{
			if (!IsConditionPasses(frag, hvo, sda))
				return false;
			else if (!AtLeastOneIsConditionPasses(frag, hvo, sda))
				return false;
			return true;
		}

		/// <summary>
		/// Check for "atleastoneis" attribute condition. This requires a field attribute that translates to a collection
		/// or sequence property.
		/// </summary>
		/// <param name="frag">The frag.</param>
		/// <param name="hvo">The hvo.</param>
		/// <param name="sda">The sda.</param>
		/// <returns></returns>
		static private bool AtLeastOneIsConditionPasses(XmlNode frag, int hvo, ISilDataAccess sda)
		{
			string className = XmlUtils.GetOptionalAttributeValue(frag, "atleastoneis");
			if (className != null)
			{
				var mdc = sda.MetaDataCache;
				int flid = GetFlid(frag, hvo, sda);
				int uclsidArg = mdc.GetClassId(className);
				bool fExcludeSubClass = XmlUtils.GetOptionalBooleanAttributeValue(frag, "excludesubclasses", false);

				int[] contents;
				int chvoMax = sda.get_VecSize(hvo, flid);
				using (ArrayPtr arrayPtr = MarshalEx.ArrayToNative<int>(chvoMax))
				{
					int chvo;
					sda.VecProp(hvo, flid, chvoMax, out chvo, arrayPtr);
					contents = MarshalEx.NativeToArray<int>(arrayPtr, chvo);
				}
				foreach (int hvoVec in contents)
				{
					int uclsidObj = sda.get_IntProp(hvo, CmObjectTags.kflidClass);
					if (uclsidArg == uclsidObj)
						return true;

					if (!fExcludeSubClass)
					{
						int uclsid = mdc.GetBaseClsId(uclsidObj);
						while (uclsid != 0)
						{
							if (uclsid == uclsidArg)
								return true;
							uclsid = mdc.GetBaseClsId(uclsid);
						}
					}
				}
				return false;
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the rules.
		/// </summary>
		/// <param name="node">The node.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected VwRule GetRules(XmlNode node)
		{
			XmlAttribute xa = node.Attributes["rules"];
			if (xa == null)
				return VwRule.kvrlNone;
			switch (xa.Value)
			{
				case "all":
					return VwRule.kvrlAll;
				case "columns":
					return VwRule.kvrlCols;
				case "groups":
					return VwRule.kvrlGroups;
				case "none":
					return VwRule.kvrlNone;
				case "rows":
					return VwRule.kvrlRows;
			}
			return VwRule.kvrlNone; // or Assert or throw exception?
		}

		/// <summary>
		/// Return an enumeration member indicating the default alignment of cells.
		/// Note JohnT: not sure this feature is implemented yet...full justification certainly isn't.
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		protected VwAlignment GetAlignment(XmlNode node)
		{
			XmlAttribute xa = node.Attributes["alignment"];
			if (xa == null)
				return VwAlignment.kvaLeft;
			switch (xa.Value)
			{
				case "right":
					return VwAlignment.kvaRight;
				case "left":
					return VwAlignment.kvaLeft;
				case "center":
					return VwAlignment.kvaCenter;
				case "justify":
					return VwAlignment.kvaJustified; // not yet supported
			}
			return VwAlignment.kvaLeft; // or Assert or throw exception?
		}

		/// <summary>
		/// Return an enumeration member indicating which sides of the table should
		/// have a frame drawn.
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		protected VwFramePosition GetFramePositions(XmlNode node)
		{
			XmlAttribute xa = node.Attributes["frame"];
			if (xa == null)
				return VwFramePosition.kvfpVoid; // no frame
			switch (xa.Value)
			{
				case "above":
					return VwFramePosition.kvfpAbove;
				case "below":
					return VwFramePosition.kvfpBelow;
				case "box":
					return VwFramePosition.kvfpBox;
				case "hsides":
					return VwFramePosition.kvfpHsides;
				case "left":
					return VwFramePosition.kvfpLhs;
				case "right":
					return VwFramePosition.kvfpRhs;
				case "vsides":
					return VwFramePosition.kvfpVsides;
				case "void":
				case "none":
					return VwFramePosition.kvfpVoid;
			}
			return VwFramePosition.kvfpVoid; // or Assert or throw exception?
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the cache.
		/// </summary>
		/// <value>The cache.</value>
		/// ------------------------------------------------------------------------------------
		public override FdoCache Cache
		{
			set
			{
				if (m_cache == value)
					return;

				base.Cache = value;

				DataAccess = m_cache.DomainDataByFlid;
				m_layouts = new LayoutCache(m_mdc, m_cache.ProjectId.Name, MApp,
					m_cache.ProjectId.ProjectFolder);
			}
		}

		/// <summary>
		/// Test-only way to set cache without setting SDA and layouts.
		/// Test code should set them some other way!
		/// </summary>
		internal void SetCache(FdoCache cache)
		{
			base.Cache = cache;
		}

		/// <summary>
		/// Set the SilDataAccess used within the VC. Setting the Cache property also
		/// sets this, so to get a different result you must call this after setting the Cache.
		/// </summary>
		public ISilDataAccess DataAccess
		{
			get { return m_sda;}
			set
			{
				m_sda = value;
				m_mdc = value.MetaDataCache;
			}
		}

		/// <summary>
		/// a look up table for getting the correct version of strings that the user will see.
		/// </summary>
		public StringTable StringTbl
		{
			get
			{
				return m_stringTable;
			}
			set
			{
				m_stringTable = value;
				m_StringsFromListNode.Clear();
			}
		}

		/// <summary/>
		protected IApp MApp
		{
			get { return m_app; }
			set { m_app = value; }
		}

		/// <summary>
		/// get an integer value, including from an FDO property if specified
		/// </summary>
		/// <remarks> the version of this method without the object ID could go away, but
		/// many callers would have to be changed</remarks>
		/// <param name="node"></param>
		/// <param name="attr"></param>
		/// <param name="defVal"></param>
		/// <param name="hvo"></param>
		/// <returns></returns>
		protected int GetIntVal(XmlNode node, string attr, int defVal, int hvo)
		{
			XmlAttribute xa = node.Attributes[attr];
			if (xa == null)
				return defVal;
			if (xa.Value.IndexOf("%") == 0)
			{
				var obj = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
				string property = xa.Value.Substring(1);//skip the %
				Debug.Assert(property.Length > 0);

				//HACK for a proof of concept.
				//TODO: make  general using reflection
				// Note: if making general, consider supporting a way to hook up notification if underlying properties
				// change. Note that in the handler for <table> there is a also a special case for %ColumnCount.
				Debug.Assert(property == "ColumnCount", "Sorry, only 'ColumnCount' on affix templates are supported at this point.");
				Debug.Assert(obj is IMoInflAffixTemplate);
				var template = (IMoInflAffixTemplate)obj;
				//one column for the STEM
				return 1 + template.PrefixSlotsRS.Count + template.SuffixSlotsRS.Count;
			}
			return Convert.ToInt32(xa.Value, 10);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tries the get flid.
		/// </summary>
		/// <param name="frag">The frag.</param>
		/// <param name="hvo">The hvo.</param>
		/// <param name="flid">The flid.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool TryGetFlid(XmlNode frag, int hvo, out int flid)
		{
			flid = 0;
			try
			{
				string stFieldName = XmlUtils.GetOptionalAttributeValue(frag, "field");
				if (String.IsNullOrEmpty(stFieldName))
					return false;
				flid = GetFlid(frag, hvo, m_sda);
			}
			catch
			{
				// discard exceptions.
				return false;
			}
			return flid != 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the flid.
		/// </summary>
		/// <param name="frag">The frag.</param>
		/// <param name="hvo">The hvo.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected int GetFlid(XmlNode frag, int hvo)
		{
			return GetFlid(frag, hvo, m_sda);
		}

		/// <summary>
		/// Gets the flid.
		/// </summary>
		/// <param name="frag">The frag.</param>
		/// <param name="hvo">The hvo.</param>
		/// <param name="sda">The sda.</param>
		/// <returns></returns>
		public static int GetFlid(XmlNode frag, int hvo, ISilDataAccess sda)
		{
			return GetFlid(frag, hvo, sda, true);
		}

		/// <summary>
		/// Gets the flid.
		/// </summary>
		/// <param name="frag"></param>
		/// <param name="hvo"></param>
		/// <param name="sda"></param>
		/// <param name="fRequired">a flid value is required, so throw if you can't get one</param>
		/// <returns></returns>
		public static int GetFlid(XmlNode frag, int hvo, ISilDataAccess sda, bool fRequired)
		{
			int flid = 0;
			try
			{
				var mdc = sda.MetaDataCache;
				XmlAttribute xa = frag.Attributes["flid"];
				if (xa == null)
				{
					if (mdc == null)
						return 0;
					// can't do anything else sensible.
					// JohnT: try class, field props and look up in MetaDataCache.

					string stClassName = XmlUtils.GetOptionalAttributeValue(frag, "class");
					if (stClassName == null && hvo != 0)
					{
						var clsid = sda.get_IntProp(hvo, CmObjectTags.kflidClass);
						stClassName = mdc.GetClassName(clsid);
					}
					string stFieldName = XmlUtils.GetOptionalAttributeValue(frag, "field");
					if (stFieldName != null)
					{
						string[] rgstFields = stFieldName.Split('/');
						if (rgstFields != null && rgstFields.Length > 1)
						{
							// this kind of field attribute is handled in GetFlidAndHvo()
							return 0;
						}
					}
					if (!String.IsNullOrEmpty(stClassName) && !String.IsNullOrEmpty(stFieldName))
						flid = mdc.GetFieldId(stClassName, stFieldName, true);
				}
				else
				{
					flid = Convert.ToInt32(xa.Value, 10);
				}
			}
			catch (Exception e)
			{
				throw new ConfigurationException("There was a problem figuring out the flid for " + hvo, frag, e);
			}
			if (flid == 0 && fRequired)
				throw new ConfigurationException(string.Format("There was a problem figuring out the flid for {0}"
					+ "{1}This is often caused by saved settings from before a new version was installed."
					+ "{1}Try starting the program while holding down the Shift key to clear all saved settings", hvo, Environment.NewLine), frag);
			return flid;
		}

		int m_cFields = 0;
		/// <summary>
		/// For custom fields, map the field name to a flag: if true, a field of that name is
		/// always custom; if false, a field of that name is sometimes custom.
		/// </summary>
		Dictionary<string, bool> m_mapCustomFields = new Dictionary<string, bool>();
		/// <summary>
		/// Map the field name to a field id if it's a custom field and the name maps only to
		/// one field.
		/// </summary>
		Dictionary<string, int> m_mapCustomFlid = new Dictionary<string, int>();

		private void LoadCustomFieldMapsIfNeeded(IFwMetaDataCacheManaged mdc)
		{
			int cFields = mdc.FieldCount;
			if (cFields != m_cFields)
			{
				m_mapCustomFields.Clear();
				m_mapCustomFlid.Clear();
				// First, get all the flids organized by name.
				Dictionary<string, List<int>> mapNameFlids = new Dictionary<string, List<int>>();
				foreach (int flid in mdc.GetFieldIds())
				{
					string sName = mdc.GetFieldName(flid);
					List<int> rgflids;
					if (!mapNameFlids.TryGetValue(sName, out rgflids))
					{
						rgflids = new List<int>();
						mapNameFlids.Add(sName, rgflids);
					}
					rgflids.Add(flid);
				}
				foreach (string sName in mapNameFlids.Keys)
				{
					int cCustom = 0;
					List<int> rgflids = mapNameFlids[sName];
					foreach (int flid in rgflids)
					{
						if (mdc.IsCustom(flid))
							++cCustom;
					}
					if (cCustom > 0)
					{
						m_mapCustomFields.Add(sName, cCustom == rgflids.Count);
						if (rgflids.Count == 1)
							m_mapCustomFlid.Add(sName, rgflids[0]);
					}
				}
				m_cFields = cFields;
				mapNameFlids.Clear();
			}
		}

		/// <summary>
		/// Try to see if the part field refers to a custom field, whether valid or not.
		/// (Actually if it's not valid, it may not be possible to tell whether it's a custom field.)
		/// </summary>
		/// <param name="sda">The sda.</param>
		/// <param name="node">The node.</param>
		/// <param name="hvo"></param>
		/// <param name="flid">the id of the custom field, 0 if not valid for our database.</param>
		/// <returns>
		/// true, if the node refers to a custom field, false if it does not.
		/// </returns>
		public bool TryCustomField(ISilDataAccess sda, XmlNode node, int hvo, out int flid)
		{
			flid = 0;
			string fieldName = XmlUtils.GetOptionalAttributeValue(node, "field");
			if (!String.IsNullOrEmpty(fieldName))
			{
				IFwMetaDataCacheManaged mdc = sda.MetaDataCache as IFwMetaDataCacheManaged;
				if(mdc != null) // May have just a regular one in testing; in that case, don't handle custom fields.
				LoadCustomFieldMapsIfNeeded(mdc);
				if (m_mapCustomFlid.TryGetValue(fieldName, out flid))
					return true;
				bool fAlways;
				if (m_mapCustomFields.TryGetValue(fieldName, out fAlways))
				{
					try
					{
						flid = XmlVc.GetFlid(node, hvo, sda, false);
						if (fAlways)
							return true;
						else if (flid != 0)
							return mdc.IsCustom(flid);
					}
					catch
					{
					}
				}
				else
				{
					XmlNode xnParent = node.ParentNode;
					if (xnParent != null && xnParent.Name == "part")
					{
						string sId = XmlUtils.GetAttributeValue(xnParent, "id");
						if (sId != null && sId.Contains("-Custom") && sId.Contains("_" + fieldName))
							return true;
			}
				}
			}
			return false;
		}

		/// <summary>
		/// Get the flid and possibly update the hvo for use and processing an 'if' element.
		/// This differs from GetFlid() in that the field attribute may be a slash delimited path
		/// instead of a simple field name. This allows the test to access data referenced by
		/// a field of the current object, and not just data directly referenced by the current
		/// object.
		/// </summary>
		/// <returns>-1 (and hvo 0) if some property along the path is empty.</returns>
		protected static int GetFlidAndHvo(IVwEnv vwenv, XmlNode frag, ref int hvo, ISilDataAccess sda)
		{
			try
			{

				int flid = 0;
				IFwMetaDataCache mdc = sda.MetaDataCache;
				XmlAttribute xa = frag.Attributes["flid"];
				if (xa == null)
				{
					if (mdc == null)
						return 0; // can't do anything else sensible.
					// JohnT: try class, field props and look up in MetaDataCache.

					string stClassName = XmlUtils.GetOptionalAttributeValue(frag, "class");
					string stFieldPath = XmlUtils.GetOptionalAttributeValue(frag, "field");
					string[] rgstFields = stFieldPath.Split('/');

					for (int i = 0; i < rgstFields.Length; i++)
					{
						if (i > 0)
						{
							NoteDependency(vwenv, hvo, flid);
							hvo = sda.get_ObjectProp(hvo, flid);
							if (hvo == 0)
								return -1;
						}
						if (String.IsNullOrEmpty(stClassName))
						{
							int clsid = sda.get_IntProp(hvo, CmObjectTags.kflidClass);
							flid = mdc.GetFieldId2(clsid, rgstFields[i], true);
						}
						else
						{
							flid = mdc.GetFieldId(stClassName, rgstFields[i], true);
							if (flid != 0)
							{
								// And cache it for next time if possible...
								// Can only do this if it doesn't depend on the current object.
								// (Hence we only do this here where there was an explicit "class" attribute,
								// not in the branch where we looked up the class on the object.)
								XmlNode xmldocT = frag;
								while (xmldocT != null && !(xmldocT is XmlDocument))
									xmldocT = xmldocT.ParentNode;
								if (xmldocT != null)
								{
									XmlDocument xmldoc = (XmlDocument)xmldocT;
									XmlAttribute xaT = xmldoc.CreateAttribute("flid");
									xaT.Value = flid.ToString();
									frag.Attributes.Prepend(xaT);
								}
							}
						}
						stClassName = null;
					}
				}
				else
				{
					flid = Convert.ToInt32(xa.Value, 10);
				}
				return flid;
			}
			catch (Exception e)
			{
				throw new ConfigurationException("There was a problem figuring out the flid for " + hvo, frag, e);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the layout name to use, based on both the current node and the one that invoked it.
		/// </summary>
		/// <param name="frag">The frag.</param>
		/// <param name="caller">The caller.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		static public string GetLayoutName(XmlNode frag, XmlNode caller)
		{
			// New approach: find or allocate a unique integer that identifies this layout name.
			// If there is a caller node, its "param" overrides the layout specified on the
			// main frag
			string layoutName = null;
			if (caller != null)
			{
				layoutName = XmlUtils.GetOptionalAttributeValue(caller, "param");
				if (layoutName != null)
				{
					if (layoutName.Contains("{0}"))
					{
						var arg = XmlUtils.GetOptionalAttributeValue(frag, "layoutArg", "");
						return String.Format(layoutName, arg);
					}
					return layoutName;
			}
			}
			return XmlUtils.GetOptionalAttributeValue(frag, "layout", "default");
		}

		// New approach:
		// The ID identifies both the frag and the caller as a pair in m_idToDisplayInfo
		// Note: parallel logic in XmlViewsUtils.GetNodeForRelatedObject must be kept in sync.
		// (Ideas on how to refactor to a single place welcome!!)
		/// <summary>
		/// Gets the sub frag id.
		/// </summary>
		protected internal int GetSubFragId(XmlNode frag, XmlNode caller)
		{
			// New approach: generate an ID that identifies Pair(frag, caller) in m_idToDisplayInfo
			return GetId(new MainCallerDisplayCommand(frag, caller, false, WsForce), m_idToDisplayCommand, m_displayCommandToId);
		}

		/// <summary>
		/// Gets the sub frag id for a sequence. Must retain information about m_stackPartRef in addition to the usual
		/// </summary>
		protected int GetSubFragIdSeq(XmlNode frag, XmlNode caller)
		{
			// New approach: generate an ID that identifies Pair(frag, caller) in m_idToDisplayInfo
			return GetId(new MainCallerDisplayCommandSeq(frag, caller, false, WsForce, m_stackPartRef), m_idToDisplayCommand, m_displayCommandToId);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process all the children of the node as fragments.
		/// </summary>
		/// <param name="frag">The frag.</param>
		/// <param name="vwenv">The vwenv.</param>
		/// <param name="hvo">The hvo.</param>
		/// <param name="caller">The caller.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void ProcessChildren(XmlNode frag, IVwEnv vwenv, int hvo, XmlNode caller)
		{
			ConfiguredExport configuredExport = null;

			string css = null;
			if (frag.Name == "layout")
			{
				string sVisibility = XmlUtils.GetOptionalAttributeValue(frag, "visibility");
				if (sVisibility == "never")
					return;
				if (vwenv is ConfiguredExport)
				{
					css = XmlUtils.GetOptionalAttributeValue(frag, "css");
					if (!String.IsNullOrEmpty(css))
					{
						configuredExport = (vwenv as ConfiguredExport);
						configuredExport.BeginCssClassIfNeeded(frag);
					}
				}
			}

			foreach (XmlNode node in frag.ChildNodes)
			{
				MainCallerDisplayCommand.PrintNodeTreeStep(hvo, node);
				ProcessFrag(node, vwenv, hvo, true, caller);
			}
			if (!String.IsNullOrEmpty(css))
				configuredExport.EndCssClassIfNeeded(frag);
		}
		internal virtual void DetermineNeededFieldsForChildren(XmlNode frag, XmlNode caller, NeededPropertyInfo info)
		{
			foreach (XmlNode node in frag.ChildNodes)
			{
				DetermineNeededFieldsFor(node, caller, info);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the children.
		/// </summary>
		/// <param name="frag">The frag.</param>
		/// <param name="vwenv">The vwenv.</param>
		/// <param name="hvo">The hvo.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void ProcessChildren(XmlNode frag, IVwEnv vwenv, int hvo)
		{
			ProcessChildren(frag, vwenv, hvo, null);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return the number of columns the table should have. An arbitrary default of 2
		/// is currently used if not specified.
		/// </summary>
		/// <param name="frag">The frag.</param>
		/// <param name="hvo">The hvo.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected int GetColCount(XmlNode frag, int hvo)
		{
			return GetIntVal(frag, "columns", 2, hvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the column specs.
		/// </summary>
		/// <param name="frag">The frag.</param>
		/// <param name="vwenv">The vwenv.</param>
		/// <param name="hvo">The hvo.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void ProcessColumnSpecs(XmlNode frag, IVwEnv vwenv, int hvo)
		{
			XmlNode columnsElement = frag["columns"];
			XmlNodeList columns = null;
			if (columnsElement != null)
				columns = columnsElement.ChildNodes;
			int colsSpeced = 0;
			int colsTotal = GetColCount(frag, hvo);
			if (columns != null)
			{
				foreach (XmlNode node in columns)
				{
					colsSpeced += ProcessColumnSpec(node, vwenv, hvo, colsTotal);
				}
			}
			// Distribute remaining width equally to remaining columns, if any, by using
			// the relative width capability.
			int colsLeft = colsTotal - colsSpeced;
			if (colsLeft > 0)
			{
				VwLength vlWidth;
				vlWidth.nVal = 1;
				vlWidth.unit = VwUnit.kunRelative;
				vwenv.MakeColumns(colsLeft, vlWidth);
			}
		}

		/// <summary>
		/// Process a 'column' or 'group' element within the 'columns' element.
		/// By default a column occupies its share of the available space.
		/// Other percentages can be specified.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="vwenv"></param>
		/// <param name="hvo"></param>
		/// <param name="colsTotal"></param>
		/// <returns></returns>
		protected virtual int ProcessColumnSpec(XmlNode node, IVwEnv vwenv, int hvo, int colsTotal)
		{
			VwLength vlWidth;
			vlWidth.nVal = XmlUtils.GetOptionalIntegerValue(node, "width", 1000 / colsTotal);
			vlWidth.unit = VwUnit.kunPercent100;
			int ncols = XmlUtils.GetOptionalIntegerValue(node, "count", 1);
			switch (node.Name)
			{
				case "column":
					vwenv.MakeColumns(ncols, vlWidth);
					break;
				case "group":
					vwenv.MakeColumnGroup(ncols, vlWidth);
					break;
				default:
					ncols = 0;
					if (!(node is XmlComment))
						throw new ApplicationException("Only column, group, or XML Comment elements allowed in columns element.");
					break;
			}
			return ncols;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the properties.
		/// </summary>
		/// <param name="frag">The frag.</param>
		/// <param name="vwenv">The vwenv.</param>
		/// ------------------------------------------------------------------------------------
		public static void ProcessProperties(XmlNode frag, IVwEnv vwenv)
		{
			XmlNode props = XmlUtils.GetFirstNonCommentChild(frag);
			if (props != null && props.Name == "properties")
			{
				foreach (XmlNode node in props.ChildNodes)
				{
					ProcessProperty(node, vwenv);
				}
			}
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Currently all properties are elements, with a 'value' attribute.
		/// This looks it up, given one of the child nodes of the &lt;property&gt; element.
		/// We may switch to making &lt;property&gt; have attributes, in which case,
		/// The nodes will be attribute nodes and we'll just get their values.
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static string GetPropVal(XmlNode node)
		{
			return XmlUtils.GetOptionalAttributeValue(node, "value");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the property.
		/// </summary>
		/// <param name="node">The node.</param>
		/// <param name="vwenv">The vwenv.</param>
		/// ------------------------------------------------------------------------------------
		public static void ProcessProperty(XmlNode node, IVwEnv vwenv)
		{
			switch (node.Name)
			{
				case "fontfamily":
					{
						vwenv.set_StringProperty((int)FwTextPropType.ktptFontFamily, GetPropVal(node));
						break;
					}
				case "italic": // <italic/> || <italic value='on|off|invert'/>
					{
						int val = OnOffInvert(node);
						vwenv.set_IntProperty((int)FwTextPropType.ktptItalic, (int)FwTextPropVar.ktpvEnum, val);
						break;
					}
				case "bold": // <bold/> || <bold value='on|off|invert'/>
					{
						int val = OnOffInvert(node);
						vwenv.set_IntProperty((int)FwTextPropType.ktptBold, (int)FwTextPropVar.ktpvEnum, val);
						break;
					}
				case "style":
					{
						vwenv.set_StringProperty((int)FwTextPropType.ktptNamedStyle, GetPropVal(node));
						break;
					}
				case "superscript": // <superscript/> || <superscript value='super|sub|off'/>
					{
						string strVal = GetPropVal(node);
						int val = (int)FwSuperscriptVal.kssvSuper; // default
						switch (strVal)
						{
							case "super":
							case null:
								val = (int)FwSuperscriptVal.kssvSuper;
								break;
							case "off":
								val = (int)FwSuperscriptVal.kssvOff;
								break;
							case "sub":
								val = (int)FwSuperscriptVal.kssvSub;
								break;
							default:
								Debug.Assert(false, "Expected value super, sub, or off");
								break;
						}
						vwenv.set_IntProperty((int)FwTextPropType.ktptSuperscript, (int)FwTextPropVar.ktpvEnum, val);
						break;
					}
				case "underline": // <underline/> || <underline value='single|none|double|dotted|dashed|squiggle'/>
					{
						string strVal = GetPropVal(node);
						int val = InterpretUnderlineType(strVal);
						vwenv.set_IntProperty((int)FwTextPropType.ktptUnderline, (int)FwTextPropVar.ktpvEnum, val);
						break;
					}
				case "fontsize": // <fontsize value='millipoints'/>
					{
						string sval = GetPropVal(node);
						if (sval == null || sval.Length == 0)
							break;
						sval = sval.Trim();
						if (sval[sval.Length - 1] == '%')
						{
							sval = sval.Substring(0, sval.Length - 1); // strip %
							int percent = Convert.ToInt32(sval);
							vwenv.set_IntProperty((int)FwTextPropType.ktptFontSize, (int)FwTextPropVar.ktpvRelative, percent * 100);
						}
						else
						{
							int val = MillipointVal(node);
							vwenv.set_IntProperty((int)FwTextPropType.ktptFontSize, (int)FwTextPropVar.ktpvMilliPoint, val);
						}
						break;
					}
				case "offset": // <offset value='millipoints'/>
					{
						int val = MillipointVal(node);
						vwenv.set_IntProperty((int)FwTextPropType.ktptOffset, (int)FwTextPropVar.ktpvMilliPoint, val);
						break;
					}
				case "backcolor":
					{
						vwenv.set_IntProperty((int)FwTextPropType.ktptBackColor, (int)FwTextPropVar.ktpvDefault,
							ColorVal(node));
						break;
					}
				case "forecolor":
					{
						vwenv.set_IntProperty((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault,
							ColorVal(node));
						break;
					}
				case "underlinecolor": // underlineColor? But for now I'm sticking to the ktpt names...
					{
						vwenv.set_IntProperty((int)FwTextPropType.ktptUnderColor, (int)FwTextPropVar.ktpvDefault,
							ColorVal(node));
						break;
					}
				case "alignment":
					{
						vwenv.set_IntProperty((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum,
							Alignment(node));
						break;
					}
				case "firstindent": // <firstindent value='millipoints'/>
					{
						int val = MillipointVal(node);
						vwenv.set_IntProperty((int)FwTextPropType.ktptFirstIndent, (int)FwTextPropVar.ktpvMilliPoint, val);
						break;
					}
				case "marginleading":
				case "leadingindent": // <leadingindent value='millipoints'/>
					{
						int val = MillipointVal(node);
						vwenv.set_IntProperty((int)FwTextPropType.ktptLeadingIndent, (int)FwTextPropVar.ktpvMilliPoint, val);
						break;
					}
				case "margintrailing":
				case "trailingindent": // <trailingindent value='millipoints'/>
					{
						int val = MillipointVal(node);
						vwenv.set_IntProperty((int)FwTextPropType.ktptTrailingIndent, (int)FwTextPropVar.ktpvMilliPoint, val);
						break;
					}
				case "spacebefore": // <spacebefore value='millipoints'/>
					{
						int val = MillipointVal(node);
						vwenv.set_IntProperty((int)FwTextPropType.ktptSpaceBefore, (int)FwTextPropVar.ktpvMilliPoint, val);
						break;
					}
				case "marginbottom":
				case "spaceafter": // <spaceafter value='millipoints'/>
					{
						int val = MillipointVal(node);
						vwenv.set_IntProperty((int)FwTextPropType.ktptSpaceAfter, (int)FwTextPropVar.ktpvMilliPoint, val);
						break;
					}
				case "lineheight": // <lineheight value='millipoints'/>
					{
						// Todo JohnT: add support for relative.
						int val = MillipointVal(node);
						vwenv.set_IntProperty((int)FwTextPropType.ktptLineHeight, (int)FwTextPropVar.ktpvMilliPoint, val);
						break;
					}
				case "margintop": // <margintop value='millipoints'/>
					{
						int val = MillipointVal(node);
						vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTop, (int)FwTextPropVar.ktpvMilliPoint, val);
						break;
					}
				case "padleading": // <padleading value='millipoints'/>
					{
						int val = MillipointVal(node);
						vwenv.set_IntProperty((int)FwTextPropType.ktptPadLeading, (int)FwTextPropVar.ktpvMilliPoint, val);
						break;
					}
				case "padtrailing": // <padtrailing value='millipoints'/>
					{
						int val = MillipointVal(node);
						vwenv.set_IntProperty((int)FwTextPropType.ktptPadTrailing, (int)FwTextPropVar.ktpvMilliPoint, val);
						break;
					}
				case "padtop": // <padtop value='millipoints'/>
					{
						int val = MillipointVal(node);
						vwenv.set_IntProperty((int)FwTextPropType.ktptPadTop, (int)FwTextPropVar.ktpvMilliPoint, val);
						break;
					}
				case "padbottom": // <padbottom value='millipoints'/>
					{
						int val = MillipointVal(node);
						vwenv.set_IntProperty((int)FwTextPropType.ktptPadBottom, (int)FwTextPropVar.ktpvMilliPoint, val);
						break;
					}
				case "borderleading": // <borderleading value='millipoints'/>
					{
						int val = MillipointVal(node);
						vwenv.set_IntProperty((int)FwTextPropType.ktptBorderLeading, (int)FwTextPropVar.ktpvMilliPoint, val);
						break;
					}
				case "bordertrailing": // <bordertrailing value='millipoints'/>
					{
						int val = MillipointVal(node);
						vwenv.set_IntProperty((int)FwTextPropType.ktptBorderTrailing, (int)FwTextPropVar.ktpvMilliPoint, val);
						break;
					}
				case "bordertop": // <bordertop value='millipoints'/>
					{
						int val = MillipointVal(node);
						vwenv.set_IntProperty((int)FwTextPropType.ktptBorderTop, (int)FwTextPropVar.ktpvMilliPoint, val);
						break;
					}
				case "borderbottom": // <borderbottom value='millipoints'/>
					{
						int val = MillipointVal(node);
						vwenv.set_IntProperty((int)FwTextPropType.ktptBorderBottom, (int)FwTextPropVar.ktpvMilliPoint, val);
						break;
					}
				case "editable":
					{
						vwenv.set_IntProperty((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum,
							GetEditable(node));
						break;
					}
				case "righttoleft":
					{
						vwenv.set_IntProperty((int)FwTextPropType.ktptRightToLeft, (int)FwTextPropVar.ktpvEnum,
							OnOffInvert(node));
						break;
					}
				case "maxlines":
					{
						var value = GetPropVal(node);
						vwenv.set_IntProperty((int)FwTextPropType.ktptMaxLines, (int)FwTextPropVar.ktpvDefault,
							Convert.ToInt32(value));
						break;
					}
				//Todo JohnT: and a good many more...
			}
		}

		/// <summary>
		/// Interpret an underline type string as an FwUnderlineType.
		/// Note that currently this routine is duplicated in Framework/StylesXmlAccessor (due to avoiding assembly references). Keep in sync.
		/// </summary>
		/// <param name="strVal"></param>
		/// <returns></returns>
		static public int InterpretUnderlineType(string strVal)
		{
			int val = (int)FwUnderlineType.kuntSingle; // default
			switch (strVal)
			{
				case "single":
				case null:
					val = (int)FwUnderlineType.kuntSingle;
					break;
				case "none":
					val = (int)FwUnderlineType.kuntNone;
					break;
				case "double":
					val = (int)FwUnderlineType.kuntDouble;
					break;
				case "dotted":
					val = (int)FwUnderlineType.kuntDotted;
					break;
				case "dashed":
					val = (int)FwUnderlineType.kuntDashed;
					break;
				case "squiggle":
					val = (int)FwUnderlineType.kuntSquiggle;
					break;
				case "strikethrough":
					val = (int)FwUnderlineType.kuntStrikethrough;
					break;
				default:
					Debug.Assert(false, "Expected value single, none, double, dotted, dashed, strikethrough, or squiggle");
					break;
			}
			return val;
		}

		static int MillipointVal(XmlNode node)
		{
			return Convert.ToInt32(GetPropVal(node));
		}
		/// <summary>
		/// Look for an attribute called 'value' and return the appropriate enumeration member for
		/// 'on', 'off', 'invert', or missing.
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		static int OnOffInvert(XmlNode node)
		{
			string strVal = GetPropVal(node);
			int val = (int)FwTextToggleVal.kttvForceOn; // default
			switch (strVal)
			{
				case "on":
				case null:
					val = (int)FwTextToggleVal.kttvForceOn;
					break;
				case "off":
					val = (int)FwTextToggleVal.kttvOff;
					break;
				case "invert":
					val = (int)FwTextToggleVal.kttvInvert;
					break;
				default:
					Debug.Assert(false, "Expected value on, off, or invert");
					break;
			}
			return val;
		}
		/// <summary>
		/// Color value may be (red, green, blue) or one of the KnownColor values.
		/// Note: much of logic is duplicated in StylesXmlAccessor method of same name.
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		static int ColorVal(XmlNode node)
		{
			string val = GetPropVal(node);
			if (val[0] == '(')
			{
				int firstComma = val.IndexOf(',');
				int red = Convert.ToInt32(val.Substring(1, firstComma - 1));
				int secondComma = val.IndexOf(',', firstComma + 1);
				int green = Convert.ToInt32(val.Substring(firstComma + 1, secondComma - firstComma - 1));
				int blue = Convert.ToInt32(val.Substring(secondComma + 1, val.Length - secondComma - 2));
				return red + (blue * 256 + green) * 256;
			}
			Color col = Color.FromName(val);
			return col.R + (col.B * 256 + col.G) * 256;
		}
		static int Alignment(XmlNode node)
		{
			string val = GetPropVal(node);
			switch (val)
			{
				case "left":
					return (int)FwTextAlign.ktalLeft;
				case "right":
					return (int)FwTextAlign.ktalRight;
				case "center":
					return (int)FwTextAlign.ktalCenter;
				case "leading":
					return (int)FwTextAlign.ktalLeading;
				case "trailing":
					return (int)FwTextAlign.ktalTrailing;
				case "justify":
					return (int)FwTextAlign.ktalJustify; // not yet implemented.
			}
			return (int)FwTextAlign.ktalLeading; // default
		}
		static int GetEditable(XmlNode node)
		{
			switch (GetPropVal(node))
			{
				case "noteditable":
				case "no":
				case "false":
					return (int)TptEditable.ktptNotEditable;
				case "editable":
				case "iseditable":
				case "yes":
				case "true":
					return (int)TptEditable.ktptIsEditable;
				case "semi":
					return (int)TptEditable.ktptSemiEditable;
			}
			return (int)TptEditable.ktptIsEditable;
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// this class contains the info used as a value in m_idToDisplayInfo
	/// and as a key in m_displayInfoToId.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public abstract class DisplayCommand
	{
		abstract internal void PerformDisplay(XmlVc vc, int fragId, int hvo, IVwEnv vwenv);

		/// <summary>
		/// Add to info as much useful information as possible about fields and children
		/// that it might be useful to preload in order to handle this command on various
		/// objects that can occur in the property that is the source for info.
		/// </summary>
		/// <param name="vc"></param>
		/// <param name="fragId"></param>
		/// <param name="info"></param>
		internal virtual void DetermineNeededFields(XmlVc vc, int fragId, NeededPropertyInfo info)
		{
			// Default does nothing.
		}

		internal virtual void ProcessChildren(int fragId, XmlVc vc, IVwEnv vwenv, XmlNode node, int hvo)
		{
			++MainCallerDisplayCommand.displayLevel;
			vc.ProcessChildren(node, vwenv, hvo);
			--MainCallerDisplayCommand.displayLevel;
		}

		internal virtual void ProcessChildren(int fragId, XmlVc vc, IVwEnv vwenv, XmlNode node, int hvo, XmlNode caller)
		{
			++MainCallerDisplayCommand.displayLevel;
			vc.ProcessChildren(node, vwenv, hvo, caller);
			--MainCallerDisplayCommand.displayLevel;
		}
		// Gather up info about what fields are needed for the specified node.
		internal void DetermineNeededFieldsForChildren(XmlVc vc, XmlNode node, XmlNode caller,
			NeededPropertyInfo info)
		{
			vc.DetermineNeededFieldsForChildren(node, caller, info);
		}
	}

	/// <summary>
	/// This class implements PerformDisplay by having the VC directly ProcessFrag
	/// on an XmlNode that it stores.
	/// </summary>
	public class NodeDisplayCommand : DisplayCommand
	{
		XmlNode m_node;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="NodeDisplayCommand"/> class.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------
		public NodeDisplayCommand(XmlNode node)
		{
			m_node = node;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the node.
		/// </summary>
		/// <value>The node.</value>
		/// ------------------------------------------------------------------------------------
		public XmlNode Node
		{
			get { return m_node; }
		}

		internal override void PerformDisplay(XmlVc vc, int fragId, int hvo, IVwEnv vwenv)
		{
			ISimpleLogger logStream = vc.LogStream;
			if (logStream != null)
			{

				logStream.WriteLine("Display " + hvo + " using " + m_node.OuterXml);
				logStream.IncreaseIndent();
			}

			vc.ProcessFrag(m_node, vwenv, hvo, true, null);

			if (logStream != null)
				logStream.DecreaseIndent();
		}

		internal override void DetermineNeededFields(XmlVc vc, int fragId, NeededPropertyInfo info)
		{
			vc.DetermineNeededFieldsFor(m_node, null, info);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make it work sensibly as a hash key. Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
		/// </summary>
		/// <param name="obj">The <see cref="T:System.Object"/> to compare with the current <see cref="T:System.Object"/>.</param>
		/// <returns>
		/// true if the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>; otherwise, false.
		/// </returns>
		/// <exception cref="T:System.NullReferenceException">The <paramref name="obj"/> parameter is null.</exception>
		/// ------------------------------------------------------------------------------------
		public override bool Equals(object obj)
		{
			NodeDisplayCommand other = obj as NodeDisplayCommand;
			if (other == null)
				return false;
			return other.m_node == m_node;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make it work sensibly as a hash key. Serves as a hash function for a particular type.
		/// </summary>
		/// <returns>
		/// A hash code for the current <see cref="T:System.Object"/>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override int GetHashCode()
		{
			return m_node.GetHashCode();
		}
	}

	/// <summary>
	/// This one is almost the same but processes the CHILDREN of the stored node.
	/// </summary>
	public class NodeChildrenDisplayCommand : NodeDisplayCommand
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="NodeChildrenDisplayCommand"/> class.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------
		public NodeChildrenDisplayCommand(XmlNode node)
			: base(node)
		{
		}

		internal override void PerformDisplay(XmlVc vc, int fragId, int hvo, IVwEnv vwenv)
		{
			vc.ProcessChildren(Node, vwenv, hvo);
		}

		internal override void DetermineNeededFields(XmlVc vc, int fragId, NeededPropertyInfo info)
		{
			DetermineNeededFieldsForChildren(vc, Node, null, info);
		}

		// Make it work sensibly as a hash key
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make it work sensibly as a hash key. Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
		/// </summary>
		/// <param name="obj">The <see cref="T:System.Object"/> to compare with the current <see cref="T:System.Object"/>.</param>
		/// <returns>
		/// true if the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>; otherwise, false.
		/// </returns>
		/// <exception cref="T:System.NullReferenceException">The <paramref name="obj"/> parameter is null.</exception>
		/// ------------------------------------------------------------------------------------
		public override bool Equals(object obj)
		{
			return base.Equals(obj) && obj is NodeChildrenDisplayCommand;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compiler requires override since Equals is overridden.
		/// Make it work sensibly as a hash key. Serves as a hash function for a particular type.
		/// </summary>
		/// <returns>
		/// A hash code for the current <see cref="T:System.Object"/>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}


	}

	[DebuggerDisplay("main={m_mainNode.Name},caller={m_caller.Name}")]
	internal class MainCallerDisplayCommand : DisplayCommand
	{
		// The main node, usually one that has a "layout" attribute; obj or seq.
		XmlNode m_mainNode;

		// A calling node, which may (if non-null) have a "param" attribute that overrides
		// the "layout" one in m_mainNode; part ref.
		XmlNode m_caller;

		// If true, bypass the normal strategy, and call ProcessFrag
		// using the given hvo and m_mainNode as the fragment.
		bool m_fUseMainAsFrag;

		/// <summary>
		/// The value of wsForce for the vc when the MainCallerDisplayCommand was needed (restored
		/// for the duration of building its parts).
		/// </summary>
		private int m_wsForce;

		internal MainCallerDisplayCommand(XmlNode mainNode, XmlNode caller, bool fUserMainAsFrag, int wsForce)
		{
			m_mainNode = mainNode;
			m_caller = caller;
			m_fUseMainAsFrag = fUserMainAsFrag;
			m_wsForce = wsForce;
		}

		internal XmlNode MainNode
		{
			get { return m_mainNode; }
		}

		internal XmlNode Caller
		{
			get { return m_caller; }
		}

		internal bool UseMainAsFrag
		{
			get { return m_fUseMainAsFrag; }
		}

		// Make it work sensibly as a hash key
		public override bool Equals(object obj)
		{
			MainCallerDisplayCommand other = obj as MainCallerDisplayCommand;
			if (other == null)
				return false;
			return other.m_mainNode == m_mainNode
				&& other.m_caller == m_caller
				&& other.m_fUseMainAsFrag == m_fUseMainAsFrag
				&& other.m_wsForce == m_wsForce;
		}

		// Make it work sensibly as a hash key.
		public override int GetHashCode()
		{
			return m_mainNode.GetHashCode()
				+ (m_fUseMainAsFrag ? 1 : 0)
				+ (m_caller == null ? 0 : m_caller.GetHashCode())
				+ m_wsForce;
		}

		internal static int displayLevel = 0;

		internal override void PerformDisplay(XmlVc vc, int fragId, int hvo, IVwEnv vwenv)
		{
			string layoutName;
			XmlNode node = GetNodeForChild(out layoutName, fragId, vc, hvo);
			var oldWsForce = vc.WsForce;
			ISimpleLogger logStream = vc.LogStream;
			try
			{
				// Force the correct WsForce for the duration of the command.
				// Normally, this is already correct, since the display command is invoked
				// as a result of calling AddObjVecItems or similar in a context where WsForce is already active.
				// However, when it is invoked independently as a result of PropChanged, that setting may need to be restored.
				if (vc.WsForce != m_wsForce) // important because some VCs don't allow setting this.
					vc.WsForce = m_wsForce;

				if (logStream != null)
				{
					logStream.WriteLine("Display " + hvo + " using layout " + layoutName + " which found " +
										node.OuterXml);
					logStream.IncreaseIndent();
				}
				string flowType = null;
				string style = null;
				if (node.Name == "layout")
				{
					// layouts may have flowType and/or style specified.
					flowType = XmlUtils.GetOptionalAttributeValue(node, "flowType", null);
					style = XmlUtils.GetOptionalAttributeValue(node, "style", null);
					if (style != null && flowType == null)
						flowType = "span";
					if (flowType == "para")
						vc.GetParagraphStyleIfPara(hvo, ref style);
				}
				if (flowType != null)
				{
					// Surround the processChildren call with an appropriate flow object, and
					// if requested apply a style to it.
					if (style != null && flowType != "divInPara")
							vwenv.set_StringProperty((int) FwTextPropType.ktptNamedStyle, style);
					switch (flowType)
					{
						case "span":
							vwenv.OpenSpan();
							break;
						case "para":
							vwenv.OpenParagraph();
							break;
						case "div":
							vwenv.OpenDiv();
							break;
						case "none":
							break;
						case "divInPara":
							vwenv.CloseParagraph();
							vwenv.OpenDiv();
							break;
					}
					PrintNodeTreeStep(hvo, node);
					ProcessChildren(fragId, vc, vwenv, node, hvo);
					switch (flowType)
					{
						default:
							vwenv.CloseSpan();
							break;
						case "para":
							vwenv.CloseParagraph();
							break;
						case "div":
							vwenv.CloseDiv();
							break;
						case "none":
							break;
						case "divInPara":
							vwenv.CloseDiv();
							vwenv.OpenParagraph();
							// If we end up with an empty paragraph, try to make it disappear.
							vwenv.EmptyParagraphBehavior(1);
							break;
					}
				}
				else
				{
					// no flow/style specified
					PrintNodeTreeStep(hvo, node);
					ProcessChildren(fragId, vc, vwenv, node, hvo, Caller);
				}
			}
			finally
			{
				if (vc.WsForce != oldWsForce) // important because some VCs don't allow setting this.
					vc.WsForce = oldWsForce; // restore.
			}
			if (logStream != null)
				logStream.DecreaseIndent();
		}

		internal static void PrintNodeTreeStep(int hvo, XmlNode node)
		{
			//string indent = "";
			//for(int i = 0; i < displayLevel; ++i)
			//    indent += "->";
			//Debug.Print("|{0}{1} : {2} # {3}|", indent, node.Attributes != null ?
			//                                                node.Attributes["class"] != null ? node.Attributes["class"].Value :
			//                                                 node.Attributes["ref"] != null ? node.Attributes["ref"].Value : "" : "",
			//            node.Name, hvo);
		}

		internal override void DetermineNeededFields(XmlVc vc, int fragId, NeededPropertyInfo info)
		{
			int clsid = info.TargetClass(vc);
			if (clsid == 0)
				return; // or assert? an object prop should have a dest class.
			string layoutName;
			XmlNode node = GetNodeForChildClass(out layoutName, fragId, vc, clsid);
			DetermineNeededFieldsForChildren(vc, node, null, info);
		}

		internal XmlNode GetNodeForChild(out string layoutName, int fragId, XmlVc vc, int hvo)
		{
			XmlNode node = null;
			XmlNode callingFrag = null;
			layoutName = null;
			layoutName = GetLayoutName(out callingFrag, out node);
			if (node == null)
				node = vc.GetNodeForPart(hvo, layoutName, true);
			node = XmlVc.GetDisplayNodeForChild(node, callingFrag, vc.m_layouts);
			return node;
		}

		/// <summary>
		/// Almost the same as GetDisplayNodeForChild, but depends on knowing the class of child
		/// rather than the actual child instance.
		/// </summary>
		/// <param name="layoutName"></param>
		/// <param name="fragId"></param>
		/// <param name="vc"></param>
		/// <param name="clsid"></param>
		/// <returns></returns>
		internal XmlNode GetNodeForChildClass(out string layoutName, int fragId, XmlVc vc, int clsid)
		{
			XmlNode node = null;
			XmlNode callingFrag = null;
			layoutName = null;
			layoutName = GetLayoutName(out callingFrag, out node);
			if (node == null)
				node = vc.GetNodeForPart(layoutName, true, clsid);
			node = XmlVc.GetDisplayNodeForChild(node, callingFrag, vc.m_layouts);
			return node;
		}

		internal virtual string GetLayoutName(out XmlNode callingFrag, out XmlNode node)
		{
			string layoutName = null;
			node = null;
			callingFrag = (XmlNode)this.MainNode;
			if (this.UseMainAsFrag)
				node = callingFrag;
			else
			{
				XmlNode caller = (XmlNode)this.Caller;
				layoutName = XmlVc.GetLayoutName(callingFrag, caller);
			}
			return layoutName;
		}
	}

	/// <summary>
	/// This is a subclass of MainCallerDisplayCommand, necessary for sequences.
	/// When a display of a sequence is regenerated, we must restore m_stackPartRef to the correct state.
	/// </summary>
	internal class MainCallerDisplayCommandSeq : MainCallerDisplayCommand
	{
		private XmlNode[] m_stackPartRef;
		internal MainCallerDisplayCommandSeq(XmlNode mainNode, XmlNode caller, bool fUserMainAsFrag, int wsForce, List<XmlNode> stackPartRef)
			: base(mainNode, caller, fUserMainAsFrag, wsForce)
		{
			m_stackPartRef = stackPartRef.ToArray();
		}

		/// <summary>
		/// Two of these are equal if everything inherited is equal, and they have the same saved stack items.
		/// </summary>
		public override bool Equals(object obj)
		{
			if (!base.Equals(obj))
				return false;
			var other = obj as MainCallerDisplayCommandSeq;
			if (other == null || other.m_stackPartRef.Length != m_stackPartRef.Length)
				return false;
			for (int i = 0; i < m_stackPartRef.Length; i++)
			{
				if (m_stackPartRef[i] != other.m_stackPartRef[i])
					return false;
			}
			return true;
		}

		/// <summary>
		/// Hash code must incorporate the stack items.
		/// </summary>
		public override int GetHashCode()
		{
			return base.GetHashCode() +
				m_stackPartRef.Aggregate(0, (sum, node) => (sum + node.GetHashCode()) % Int32.MaxValue);
		}

		/// <summary>
		/// Base version wrapped in making the stack what it needs to be.
		/// </summary>
		internal override void PerformDisplay(XmlVc vc, int fragId, int hvo, IVwEnv vwenv)
		{
			var save = vc.m_stackPartRef.ToArray();
			vc.m_stackPartRef.Clear();
			vc.m_stackPartRef.AddRange(m_stackPartRef);
			base.PerformDisplay(vc, fragId, hvo, vwenv);
			vc.m_stackPartRef.Clear();
			vc.m_stackPartRef.AddRange(save);
		}
	}

	/// <summary>
	/// This class is always used to handle fragid 0.
	/// </summary>
	internal class RootDisplayCommand : DisplayCommand
	{
		string m_rootLayoutName;
		internal SimpleRootSite m_rootSite;

		public RootDisplayCommand(string rootLayoutName, SimpleRootSite rootSite)
			: base()
		{
			m_rootLayoutName = rootLayoutName;
			m_rootSite = rootSite;
		}

		internal override void PerformDisplay(XmlVc vc, int fragId, int hvo, IVwEnv vwenv)
		{
			XmlNode node = vc.GetNodeForPart(hvo, m_rootLayoutName, true);
			ProcessChildren(fragId, vc, vwenv, node, hvo);
		}

		internal override void DetermineNeededFields(XmlVc vc, int fragId, NeededPropertyInfo info)
		{
			int clsid = info.TargetClass(vc);
			if (clsid == 0)
				return; // or assert? an object prop should have a dest class.
			DetermineNeededFieldsForClass(vc, fragId, clsid, info);
		}

		internal virtual void DetermineNeededFieldsForClass(XmlVc vc, int fragId, int clsid, NeededPropertyInfo info)
		{
			XmlNode node = vc.GetNodeForPart(m_rootLayoutName, true, clsid);
			DetermineNeededFieldsForChildren(vc, node, null, info);
		}

		public override bool Equals(object obj)
		{
			RootDisplayCommand rdcOther = obj as RootDisplayCommand;
			return rdcOther != null && base.Equals(obj) && m_rootLayoutName == rdcOther.m_rootLayoutName;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode() + m_rootLayoutName.GetHashCode();
		}

		internal override void ProcessChildren(int fragId, XmlVc vc, IVwEnv vwenv, XmlNode node, int hvo)
		{
			// If available, apply defaults from 'Normal' to everything.
			IVwStylesheet styleSheet = m_rootSite.StyleSheet;
			if (styleSheet != null)
			{
				vwenv.Props = styleSheet.NormalFontStyle;
			}
			vwenv.OpenDiv();
			base.ProcessChildren(fragId, vc, vwenv, node, hvo);
			vwenv.CloseDiv();
		}
	}

	/// <summary>
	/// This is used for the root when we want to suppress editing.
	/// </summary>
	internal class ReadOnlyRootDisplayCommand : RootDisplayCommand
	{

		public ReadOnlyRootDisplayCommand(string rootLayoutName, SimpleRootSite rootSite)
			: base(rootLayoutName, rootSite)
		{
		}

		internal override void ProcessChildren(int fragId, XmlVc vc, IVwEnv vwenv, XmlNode node, int hvo)
		{
			// Suppress editing for the whole view. Easiest thing is to insert another div.
			vwenv.set_IntProperty((int)FwTextPropType.ktptEditable,
				(int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
			base.ProcessChildren(fragId, vc, vwenv, node, hvo);
		}

	}

	/// <summary>
	/// This class adds the ability to test a condition and use it to decide whether to display each item.
	/// </summary>
	internal class ReadOnlyConditionalRootDisplayCommand : ReadOnlyRootDisplayCommand
	{
		XmlNode m_condition;
		private ISilDataAccess m_sda;
		public ReadOnlyConditionalRootDisplayCommand(string rootLayoutName, SimpleRootSite rootSite, XmlNode condition, ISilDataAccess sda)
			: base(rootLayoutName, rootSite)
		{
			m_condition = condition;
			Debug.Assert(rootSite is RootSite, "conditional display requires real rootsite with cache");
			m_sda = sda;
		}
		internal override void PerformDisplay(XmlVc vc, int fragId, int hvo, IVwEnv vwenv)
		{
			if (XmlVc.ConditionPasses(m_condition, hvo, (m_rootSite as RootSite).Cache, m_sda))
			{
				base.PerformDisplay(vc, fragId, hvo, vwenv);
			}
		}

		/// <summary>
		/// Overrides to determine the fields needed for evaluating the condition as well as for displaying the
		/// actual objects.
		/// </summary>
		/// <param name="vc"></param>
		/// <param name="fragId"></param>
		/// <param name="info"></param>
		internal override void DetermineNeededFields(XmlVc vc, int fragId, NeededPropertyInfo info)
		{
			base.DetermineNeededFields(vc, fragId, info);
			vc.DetermineNeededFieldsFor(m_condition, null, info);
		}

		internal override void DetermineNeededFieldsForClass(XmlVc vc, int fragId, int clsid, NeededPropertyInfo info)
		{
			base.DetermineNeededFieldsForClass(vc, fragId, clsid, info);
			vc.DetermineNeededFieldsFor(m_condition, null, info);
		}
	}

	/// <summary>
	/// DisplayCommand that displays the current object by displaying a specified ws of a specified
	/// multilingual property.
	/// </summary>
	internal class DisplayStringAltCommand : DisplayCommand
	{
		int m_ws;
		int m_tag;
		private XmlNode m_caller;
		public DisplayStringAltCommand(int tag, int ws, XmlNode caller)
		{
			m_tag = tag;
			m_ws = ws;
			m_caller = caller;
		}

		internal override void PerformDisplay(XmlVc vc, int fragId, int hvo, IVwEnv vwenv)
		{
			if (m_caller != null)
				vc.MarkSource(vwenv, m_caller);
			vwenv.AddStringAltMember(m_tag, m_ws, vc);
		}

		public override bool Equals(object obj)
		{
			DisplayStringAltCommand other = obj as DisplayStringAltCommand;
			if (other == null)
				return false;
			return other.m_tag == m_tag && other.m_ws == m_ws && other.m_caller == m_caller;
		}

		public override int GetHashCode()
		{
			return m_tag + m_ws + (m_caller == null ? 0 : m_caller.GetHashCode());
		}

		internal override void DetermineNeededFields(XmlVc vc, int fragId, NeededPropertyInfo info)
		{
			info.AddAtomicField(m_tag, m_ws);
		}
	}

	/// <summary>
	/// DisplayCommand that displays the current object by displaying a specified string property.
	/// </summary>
	internal class DisplayStringCommand : DisplayCommand
	{
		int m_tag;
		public DisplayStringCommand(int tag)
		{
			m_tag = tag;
		}

		internal override void PerformDisplay(XmlVc vc, int fragId, int hvo, IVwEnv vwenv)
		{
			vwenv.AddStringProp(m_tag, vc);
		}
		internal override void DetermineNeededFields(XmlVc vc, int fragId, NeededPropertyInfo info)
		{
			info.AddAtomicField(m_tag, 0);
		}

		public override bool Equals(object obj)
		{
			DisplayStringCommand other = obj as DisplayStringCommand;
			if (other == null)
				return false;
			return other.m_tag == m_tag;
		}

		public override int GetHashCode()
		{
			return m_tag;
		}
	}
	/// <summary>
	/// DisplayCommand that displays the current object by displaying a specified unicode property
	/// as a string in a specified writing system.
	/// </summary>
	internal class DisplayUnicodeCommand : DisplayCommand
	{
		int m_ws;
		int m_tag;
		public DisplayUnicodeCommand(int tag, int ws)
		{
			m_tag = tag;
			m_ws = ws;
		}

		internal override void PerformDisplay(XmlVc vc, int fragId, int hvo, IVwEnv vwenv)
		{
			vwenv.AddUnicodeProp(m_tag, m_ws, vc);
		}
		internal override void DetermineNeededFields(XmlVc vc, int fragId, NeededPropertyInfo info)
		{
			info.AddAtomicField(m_tag, 0);
		}

		public override bool Equals(object obj)
		{
			DisplayUnicodeCommand other = obj as DisplayUnicodeCommand;
			if (other == null)
				return false;
			return other.m_tag == m_tag && other.m_ws == m_ws;
		}

		public override int GetHashCode()
		{
			return m_tag + m_ws;
		}
	}
	/// <summary>
	/// DisplayCommand that displays the current object by displaying the children of one node, treating another as caller.
	/// Typically at present the node whose children are to be procesed is an "objlocal" node, and the
	/// caller is the "part ref" node that invoked it.
	/// </summary>
	internal class ObjLocalCommand : DisplayCommand
	{
		XmlNode m_objLocal;
		XmlNode m_caller;
		public ObjLocalCommand(XmlNode objLocal, XmlNode caller)
		{
			m_objLocal = objLocal;
			m_caller = caller;
		}

		internal override void PerformDisplay(XmlVc vc, int fragId, int hvo, IVwEnv vwenv)
		{
			vc.ProcessChildren(m_objLocal, vwenv, hvo, m_caller);
		}

		internal override void DetermineNeededFields(XmlVc vc, int fragId, NeededPropertyInfo info)
		{
			int clsid = info.TargetClass(vc);
			if (clsid == 0)
				return; // or assert? an object prop should have a dest class.
			DetermineNeededFieldsForChildren(vc, m_objLocal, m_caller, info);
		}

		public override bool Equals(object obj)
		{
			ObjLocalCommand other = obj as ObjLocalCommand;
			if (other == null)
				return false;
			return other.m_caller == m_caller && other.m_objLocal == m_objLocal;
		}

		int HashOrZero(XmlNode node)
		{
			if (node == null)
				return 0;
			return node.GetHashCode();
		}

		public override int GetHashCode()
		{
			return HashOrZero(m_objLocal) + HashOrZero(m_caller);
		}
	}

	/// <summary/>
	public class PropWs
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="PropWs"/> class.
		/// </summary>
		/// <param name="xflid">The xflid.</param>
		/// <param name="xws">The XWS.</param>
		/// ------------------------------------------------------------------------------------
		public PropWs(int xflid, int xws)
		{
			flid = xflid;
			ws = xws;
		}
		/// <summary></summary>
		public int flid;
		/// <summary>0 if not applicable</summary>
		public int ws;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
		/// </summary>
		/// <param name="obj">The <see cref="T:System.Object"/> to compare with the current <see cref="T:System.Object"/>.</param>
		/// <returns>
		/// true if the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>; otherwise, false.
		/// </returns>
		/// <exception cref="T:System.NullReferenceException">The <paramref name="obj"/> parameter is null.</exception>
		/// ------------------------------------------------------------------------------------
		public override bool Equals(object obj)
		{
			PropWs other = obj as PropWs;
			if (other == null)
				return false;
			return other.flid == this.flid && other.ws == this.ws;
		}

		/// <summary>
		/// Probably not used but should have some overide when Equals overridden.
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return flid * (ws + 11);
		}
	}

	/// <summary>
	/// Compares CmObjects using their SortKey property.
	/// </summary>
	class CmObjectComparer : FwDisposableBase, IComparer<int>
	{
		private IntPtr m_col = IntPtr.Zero;
		private readonly FdoCache m_cache;

		public CmObjectComparer(FdoCache cache)
		{
			m_cache = cache;
		}

		public int Compare(int x, int y)
		{
			if (x == y)
				return 0;

			ICmObject xobj = m_cache.ServiceLocator.ObjectRepository.GetObject(x);
			ICmObject yobj = m_cache.ServiceLocator.ObjectRepository.GetObject(y);
			string xkeyStr = xobj.SortKey;
			string ykeyStr = yobj.SortKey;
			if (string.IsNullOrEmpty(xkeyStr) && string.IsNullOrEmpty(ykeyStr))
				return 0;
			if (string.IsNullOrEmpty(xkeyStr))
				return -1;
			if (string.IsNullOrEmpty(ykeyStr))
				return 1;

			if (m_col == IntPtr.Zero)
			{
				string ws = xobj.SortKeyWs;
				if (string.IsNullOrEmpty(ws))
					ws = yobj.SortKeyWs;
				string icuLocale = Icu.GetName(ws);
				m_col = Icu.OpenCollator(icuLocale);
			}

			byte[] xkey = Icu.GetSortKey(m_col, xkeyStr);
			byte[] ykey = Icu.GetSortKey(m_col, ykeyStr);
			// Simulate strcmp on the two NUL-terminated byte strings.
			// This avoids marshalling back and forth.
			// JohnT: but apparently the strings are not null-terminated if the input was empty.
			int nVal;
			if (xkey.Length == 0)
				nVal = -ykey.Length; // zero if equal, neg if ykey is longer (considered larger)
			else if (ykey.Length == 0)
				nVal = 1; // xkey is longer and considered larger.
			else
			{
				// Normal case, null termination should be present.
				int ib;
				for (ib = 0; xkey[ib] == ykey[ib] && xkey[ib] != 0; ++ib)
				{
					// skip merrily along until strings differ or end.
				}
				nVal = xkey[ib] - ykey[ib];
			}
			if (nVal == 0)
			{
				// Need to get secondary sort keys.
				int xkey2 = xobj.SortKey2;
				int ykey2 = yobj.SortKey2;
				return xkey2 - ykey2;
			}

			return nVal;
		}

		protected override void DisposeUnmanagedResources()
		{
			if (m_col != IntPtr.Zero)
			{
				Icu.CloseCollator(m_col);
				m_col = IntPtr.Zero;
			}
		}
	}

	#region ItemTypeInfo class

	/// <summary>
	/// Store information needed for knowing how to handle a particular type of something.
	/// </summary>
	public class ItemTypeInfo
	{
		/// <summary>Guid of the type object.  (This is probably a CmPossibility or subclass thereof.)</summary>
		public Guid ItemGuid { get; protected set; }
		/// <summary>Flag whether the given LexRefType is enabled for display.</summary>
		public bool Enabled { get; set; }
		/// <summary>Display name of the type object.</summary>
		public string Name { get; set; }
		/// <summary>Index of this item in the list of enabled items.</summary>
		public int Index { get; set; }

		/// <summary>
		/// Override for use in list view.
		/// </summary>
		public override string ToString()
		{
			return Name;
		}

		/// <summary>
		/// Constructor needed for subclass.
		/// </summary>
		public ItemTypeInfo()
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public ItemTypeInfo(bool fEnabled, Guid guid)
		{
			Enabled = fEnabled;
			ItemGuid = guid;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public ItemTypeInfo(string s)
		{
			Enabled = s.StartsWith("+");
			ItemGuid = new Guid(s.Substring(1));
		}

		/// <summary>
		/// Get the string representation used in the part ref attributes.
		/// </summary>
		public virtual string StorageString
		{
			get { return String.Format("{0}{1}", Enabled ? "+" : "-", ItemGuid); }
		}

		/// <summary>
		/// Create a list of these objects from a string representation.
		/// </summary>
		public static List<ItemTypeInfo> CreateListFromStorageString(string sTypeseq)
		{
			var list = new List<ItemTypeInfo>();
			if (!String.IsNullOrEmpty(sTypeseq))
			{
				var rgsTypes = sTypeseq.Split(',');
				list.AddRange(rgsTypes.Select(t => new ItemTypeInfo(t)));
			}
			return list;
		}
	}
	#endregion

	#region LexReferenceInfo class
	/// <summary>
	/// Store information needed for knowing how to handle a particular type of LexReference.
	/// </summary>
	public class LexReferenceInfo : ItemTypeInfo
	{
		/// <summary>Specify particular handling for some types of LexReference.</summary>
		public enum TypeSubClass
		{
			/// <summary>sequence, collection, or simple pair</summary>
			Normal = 0,
			/// <summary>normal name of tree or unequal pair (refers to 2nd and following elements of vector)</summary>
			Forward,
			/// <summary>reverse name of tree or unequal pair (refers back to first element of vector)</summary>
			Reverse
		}
		/// <summary>Flag how the LexRefType is used.</summary>
		public TypeSubClass SubClass { get; set; }

		/// <summary>
		/// Constructor.
		/// </summary>
		public LexReferenceInfo(bool fEnabled, Guid guid)
			: base(fEnabled, guid)
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public LexReferenceInfo(string s)
		{
			Enabled = s.StartsWith("+");
			if (s.EndsWith(":f"))
			{
				SubClass = TypeSubClass.Forward;
				s = s.Remove(s.Length - 2);
			}
			else if (s.EndsWith(":r"))
			{
				SubClass = TypeSubClass.Reverse;
				s = s.Remove(s.Length - 2);
			}
			else
			{
				SubClass = TypeSubClass.Normal;
			}
			ItemGuid = new Guid(s.Substring(1));
		}

		/// <summary>
		/// Get the string representation used in the part ref attributes.
		/// </summary>
		public override string StorageString
		{
			get
			{
				return String.Format("{0}{1}{2}",
					Enabled ? "+" : "-", ItemGuid, SubClassAsString);
			}
		}

		private string SubClassAsString
		{
			get
			{
				switch (SubClass)
				{
					case TypeSubClass.Forward:	return ":f";
					case TypeSubClass.Reverse:	return ":r";
					default:					return "";
				}
			}
		}

		/// <summary>
		/// Create a list of these objects from a string representation.
		/// </summary>
		public new static List<LexReferenceInfo> CreateListFromStorageString(string sTypeseq)
		{
			var list = new List<LexReferenceInfo>();
			if (!String.IsNullOrEmpty(sTypeseq))
			{
				var rgsGuids = sTypeseq.Split(',');
				list.AddRange(rgsGuids.Select(t => new LexReferenceInfo(t)));
			}
			return list;
		}
	}
	#endregion
}
