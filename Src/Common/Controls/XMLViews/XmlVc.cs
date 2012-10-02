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
// File: XmlVc.cs
// Responsibility: WordWorks
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Xml;
using System.Reflection;
using System.IO;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Text;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.Utils;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Resources;


namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// XmlVc is a view constructor class whose behavior is controlled by an XML document,
	/// described in XmlView.
	/// Note: the current version won't handle field IDs that are zero.
	/// </summary>
	public class XmlVc : VwBaseVc
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

		/// <summary></summary>
		public const int kRootFragId = 1;

		int m_nextID = 1378; // next ID to allocate.
		/// <summary></summary>
		protected internal LayoutCache m_layouts;
		bool m_fEditable = true; // set false to disable editing at top level.

		/// <summary></summary>
		protected IFwMetaDataCache m_mdc;
		/// <summary></summary>
		protected FdoCache m_cache;
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
		// REVIEW (EberhardB/TimS): this probably won't work with different databases that have
		// different default ws!
		/// <summary></summary>
		static protected IWritingSystem s_qwsCurrent = null;
		/// <summary></summary>
		static protected int s_cwsMulti = 0;	// count of current ws alternatives.
		/// <summary></summary>
		static protected string s_sMultiSep = null;
		/// <summary></summary>
		static protected bool s_fMultiFirst = false;
		// This may be set to perform logging.
		StreamWriter m_logStream = null;
		int m_logIndent = 0;
		stdole.IPicture m_CommandIcon;
		int m_IsObjectSelectedTag;
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

		/// <summary>
		/// This is used to interpret the "reversal" writing system.  This is needed to ensure
		/// that definitions, parts of speech, etc. are displayed in the proper writing system.
		/// </summary>
		int m_wsReversal = 0;

		/// <summary>
		/// This flags that grammatical information (MSA) should not be displayed unless
		/// m_tssDelayedNumber is set.  See LT-9663 for explanation of what is being done
		/// with this feature.
		/// </summary>
		bool m_fIgnoreGramInfoAfterFirst = false;
		/// <summary>
		/// This stores the sense number string whenever it needs to be delayed to follow the
		/// sense's MSA.  Look for "graminfobeforenumber" to see where it applies.
		/// </summary>
		ITsString m_tssDelayedNumber = null;
		// This groups senses by placing graminfo before the number, and omitting it if the same as the
		// previous sense in the entry.  This isn't yet supported by the UI, but may well be requested in
		// the future.  (See LT-9663.)
		///// <summary>
		///// The object id of an MSA which was displayed before the previous sense number.  Look for
		///// "graminfobeforenumber"
		///// </summary>
		//int m_hvoGroupedValue = 0;

		// This is the new constructor, where we find parts in the master inventory.
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:XmlVc"/> class.
		/// </summary>
		/// <param name="stringTable">The string table.</param>
		/// <param name="rootLayoutName">Name of the root layout.</param>
		/// <param name="fEditable">if set to <c>true</c> [f editable].</param>
		/// <param name="rootSite">The root site.</param>
		/// ------------------------------------------------------------------------------------
		public XmlVc(StringTable stringTable, string rootLayoutName, bool fEditable, SimpleRootSite rootSite)
			: this(stringTable, rootLayoutName, fEditable, rootSite, null)
		{
		}

		// This is a variant which can take a condition element to control which items in a list display.
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:XmlVc"/> class.
		/// </summary>
		/// <param name="stringTable">The string table.</param>
		/// <param name="rootLayoutName">Name of the root layout.</param>
		/// <param name="fEditable">if set to <c>true</c> [f editable].</param>
		/// <param name="rootSite">The root site.</param>
		/// <param name="condition">The condition.</param>
		/// ------------------------------------------------------------------------------------
		public XmlVc(StringTable stringTable, string rootLayoutName, bool fEditable, SimpleRootSite rootSite,
			XmlNode condition)
		{
			StringTbl = stringTable;
			m_rootLayoutName = rootLayoutName;
			m_fEditable = fEditable;
			MakeRootCommand(rootSite, condition);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is another new constructor, for using the new approach without a single
		/// top-level layout name, such as browse view.Initializes a new instance of the <see cref="T:XmlVc"/> class.
		/// </summary>
		/// <param name="stringTable">The string table.</param>
		/// ------------------------------------------------------------------------------------
		public XmlVc(StringTable stringTable)
		{
			StringTbl = stringTable;
		}

		#region IDisposable override
		// Region last reviewed: never

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
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
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_idToDisplayCommand != null)
					m_idToDisplayCommand.Clear();
				if (m_displayCommandToId != null)
					m_displayCommandToId.Clear();
				if (m_layouts != null)
					m_layouts.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_cache = null;
			m_sda = null;
			m_stringTable = null;
			m_xnSpec = null;
			m_idToDisplayCommand = null;
			m_displayCommandToId = null;
			m_layouts = null;
			m_mdc = null;
			m_rootLayoutName = null;
			//s_sMultiSep = null;
			// It may be static, but it is set and cleared in one method.
			if (s_qwsCurrent != null)
				Marshal.ReleaseComObject(s_qwsCurrent);
			s_qwsCurrent = null;
			if (m_logStream != null)
			{
				m_logStream.Flush();
				m_logStream.Close();
			}
			m_logStream = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override
		internal int IsObjectSelectedTag
		{
			get
			{
				CheckDisposed();
				return m_IsObjectSelectedTag;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the caption props.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override ITsTextProps CaptionProps
		{
			get
			{
				CheckDisposed();
				ITsPropsBldr bldr = TsPropsBldrClass.Create();
				bldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, ScrStyleNames.Figure);
				return bldr.GetTextProps();
			}
		}

		/// <summary>
		/// Indicates whether the VC should draw the view as if it has focus.
		/// Currently this controls whether command icons are shown.
		/// </summary>
		internal bool HasFocus
		{
			get
			{
				CheckDisposed();
				return m_fHasFocus;
			}
			set
			{
				CheckDisposed();
				m_fHasFocus = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the log stream.
		/// </summary>
		/// <value>The log stream.</value>
		/// ------------------------------------------------------------------------------------
		public StreamWriter LogStream
		{
			get
			{
				CheckDisposed();
				return m_logStream;
			}
			set
			{
				CheckDisposed();

				m_logStream = value;
				m_logIndent = 0;
			}
		}

		internal int LogIndent
		{
			get
			{
				CheckDisposed();
				return m_logIndent;
			}
			set
			{
				CheckDisposed();
				m_logIndent = value;
			}
		}

		internal void MakeRootCommand(SimpleRootSite rootSite, XmlNode condition)
		{
			CheckDisposed();

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
					dc = new ReadOnlyConditionalRootDisplayCommand(m_rootLayoutName, rootSite, condition);
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
		}

		/// <summary>
		/// Reset the tables (typically after changing the layout definitions).
		/// Discard all display commands except the root one.
		/// </summary>
		/// <param name="rootLayoutName"></param>
		public void ResetTables(string rootLayoutName)
		{
			CheckDisposed();

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
			m_layouts.Dispose();
			m_layouts = new LayoutCache(m_mdc, m_fLoadFlexLayouts, m_cache.DatabaseName);
			m_lastLoadDataInfo = null;
			// We could reset the next id, but that's arbitrary, so why bother?
		}

		/// <summary>
		/// The field of the root object that contains the main, lazy list.
		/// </summary>
		public int MainSeqFlid
		{
			get
			{
				CheckDisposed();
				return m_mainFlid;
			}
			set
			{
				CheckDisposed();
				m_mainFlid = value;
			}
		}

		int m_lastLoadDataClsid;
		int m_lastLoadDataFragId;
		NeededPropertyInfo m_lastLoadDataInfo;
		Set<int> m_lastLoadRecent = new Set<int>();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads the data for.
		/// </summary>
		/// <param name="vwenv">The vwenv.</param>
		/// <param name="rghvo1">The rghvo1.</param>
		/// <param name="chvo">The chvo.</param>
		/// <param name="hvoParent">The hvo parent.</param>
		/// <param name="tag">The tag.</param>
		/// <param name="fragId">The frag id.</param>
		/// <param name="ihvoMin">The ihvo min.</param>
		/// ------------------------------------------------------------------------------------
		public override void LoadDataFor(IVwEnv vwenv, int[] rghvo1, int chvo, int hvoParent, int tag, int fragId, int ihvoMin)
		{
			if (chvo <= 0)
				return;
			bool fNew = false;
			foreach (int hvo in rghvo1)
			{
				if (!m_lastLoadRecent.Contains(hvo))
				{
					fNew = true;
					break;
				}
			}
			if (!fNew)
				return; // already loaded all these hvos.
			// Worth doing a bit wider range.
			int minRange = Math.Max(ihvoMin - 5, 0);
			ISilDataAccess sda = vwenv.DataAccess;
			int chvoProp = sda.get_VecSize(hvoParent, tag);
			int limRange = Math.Min(chvoProp, ihvoMin + rghvo1.Length + 5);
			int[] rghvo = rghvo1;
			if (minRange < ihvoMin || limRange > ihvoMin + rghvo1.Length)
			{
				// Make a new rghvo with the extra objects at either end.
				rghvo = new int[limRange - minRange];
				for (int i = minRange; i < ihvoMin; i++)
					rghvo[i - minRange] = sda.get_VecItem(hvoParent, tag, i);
				rghvo1.CopyTo(rghvo, ihvoMin - minRange);
				int startDest = rghvo1.Length + (ihvoMin - minRange); // first empty slot
				int startSrc = ihvoMin + rghvo1.Length;
				for (int i = startSrc; i < limRange; i++)
					rghvo[i - startSrc + startDest] = sda.get_VecItem(hvoParent, tag, i);
			}
			m_lastLoadRecent.Clear();
			m_lastLoadRecent.AddRange(rghvo);
			DisplayCommand dispCommand = m_idToDisplayCommand[fragId];
			RootDisplayCommand rdisp = dispCommand as RootDisplayCommand;
			NeededPropertyInfo info = null;
			int clsid;
			if (rdisp == null)
			{
				info = new NeededPropertyInfo(Cache.GetDestinationClass((uint)tag));
				dispCommand.DetermineNeededFields(this, fragId, info);
				clsid = info.TargetClass(this);
			}
			else
			{
				// root display command will be displaying the root property, which currently isn't
				// a proper 'property' at all, so not in the MDC, so we can't use it to determine
				// the class of object. For now we will check the actual class of the objects in the list.
				// If they are all the same, we can use that class. If not, for now give up.
				// Enhance JohnT: we could try to determine a common base class and use that,
				// or we could subdivide them into groups that share a class and loop.
				clsid = m_cache.GetClassOfObject(rghvo[0]);
				for (int i = 1; i < rghvo.Length; i++)
				{
					if (m_cache.GetClassOfObject(rghvo[i]) != clsid)
						return; // Enhance: possibly we could figure a common base class, or just go with CmObject?
				}
				if (clsid != m_lastLoadDataClsid || fragId != m_lastLoadDataFragId || m_lastLoadDataInfo == null)
				{
					// TODO: a better approach may be to subclass NeededPropertyInfo with a variant that
					// knows the class already instead of (or in addition to) a flid.
					int flid = FindReasonableFlidFor(hvoParent, clsid);
					if (flid != 0)
					{
						info = new VirtualNeededPropertyInfo(flid, null, true, clsid);
						rdisp.DetermineNeededFieldsForClass(this, fragId, clsid, info);
						m_lastLoadDataFragId = fragId;
						m_lastLoadDataClsid = clsid;
						m_lastLoadDataInfo = info;
					}
				}
				else
				{
					info = m_lastLoadDataInfo;
				}
			}
			if (info == null || (info.AtomicFields.Count == 0 && info.SeqFields.Count == 0))
				return;
			//info.DumpFieldInfo(m_cache.MetaDataCacheAccessor);
			LoadRootFieldInfo(rghvo, info, clsid);
		}

		/// <summary>
		/// rghvos are the base objects (from LoadDataFor) for which we want to load the info
		/// specified by info, the top-level field info object.
		/// </summary>
		/// <param name="rghvo"></param>
		/// <param name="info"></param>
		/// <param name="clsid">of rghvo objects</param>
		private void LoadRootFieldInfo(int[] rghvo, NeededPropertyInfo info, int clsid)
		{
			// We want to generate something like this...
			// select le.id, le.UpdStmp$, le.LexemeForm, mff.Text, from LexEntry le
			//		left outer join MoForm_Form mff on mff.obj = le.LexemeForm and mff.ws = 123456
			// where le.id in (12, 23,34,... from rghvo)

			IFwMetaDataCache mdc = m_cache.MetaDataCacheAccessor;

			// rc = root class, the main table corresponding to clsid
			StringBuilder resultFields = new StringBuilder("select rc.id, rc.UpdStmp");
			StringBuilder whereClause = MakeWhereClause(rghvo);
			StringBuilder mainQuery = new StringBuilder(" from ");
			string baseClass = mdc.GetClassName((uint)clsid);
			mainQuery.AppendFormat("{0}_ rc ", baseClass);
			IDbColSpec dcs = DbColSpecClass.Create();
			dcs.Push((int)DbColType.koctBaseId, 0, 0, 0);
			dcs.Push((int)DbColType.koctTimeStampIfMissing, 1, 0, 0);

			int cFields = AddAtomicFieldInfoToQuerySections(info, mdc, resultFields, mainQuery, dcs, "rc", 1, 0, clsid);
			if (cFields > 0)
			{
				string sql = resultFields.ToString() + mainQuery.ToString() + whereClause.ToString();
				m_cache.VwOleDbDaAccessor.Load(sql, dcs, 0, 0, null, false);
			}

			// After we load the info about this, load it for any sequence children
			foreach (NeededPropertyInfo infoChild in info.SeqFields)
			{
				// Todo: skip atomics.
				LoadChildFieldInfo(rghvo, infoChild, clsid);
			}
		}

		private static StringBuilder MakeWhereClause(int[] rghvo)
		{
			// make the where clause
			StringBuilder whereClause = new StringBuilder(" where rc.id in (");
			foreach (int hvo in rghvo)
				whereClause.Append(hvo.ToString() + ",");
			whereClause.Replace(",", ")", whereClause.Length - 1, 1);
			return whereClause;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// rghvos are objects of type clsid (or a subclass) for which we want to load
		/// the the property specified by info.Source.
		/// Also, for each object that is in the info.Source property of one of the parents,
		/// we want to load the information specified by the properties of info.
		/// </summary>
		/// <param name="parents">The parents.</param>
		/// <param name="info">The info.</param>
		/// <param name="clsid">of parent objects</param>
		/// ------------------------------------------------------------------------------------
		private void LoadChildFieldInfo(int[] parents, NeededPropertyInfo info, int clsid)
		{
			if (parents.Length == 0)
				return;
			IFwMetaDataCache mdc = m_cache.MetaDataCacheAccessor;
			int sourceFieldType = mdc.GetFieldType((uint)info.Source);
			string extraOrderBy = "";
			if (sourceFieldType == (int)CellarModuleDefns.kcptOwningSequence || sourceFieldType == (int)CellarModuleDefns.kcptReferenceSequence)
				extraOrderBy = ", jn.Ord";
			else if (sourceFieldType != (int)CellarModuleDefns.kcptOwningCollection && sourceFieldType != (int)CellarModuleDefns.kcptReferenceCollection)
				return;
			// For each parent set parent.[info.Source] to empty. This ensures that any empty ones are actually
			// recorded in the cache as empty, since we generate zero rows for any empty ones, so Load can't know
			// about them.
			IVwCacheDa cda = m_cache.VwCacheDaAccessor;
			foreach (int hvo in parents)
			{
				cda.CacheVecProp(hvo, info.Source, null, 0);
			}

			// Generate a query and column specs to load the objects in info.Source() and their
			// atomic properties from info.

			// We want to generate something like this...
			// select le.id, le.UpdStmp, les.dst, ls.UpdStmp, gl.Text, de.Text, de.Fmt from LexEntry le
			//		join LexEntry_Senses les on le.id = les.src
			//		join LexSense_ ls on les.dst = ls.id
			//		left outer join LexSense_Gloss gl on ls.dst = gl.obj and gl.ws = 123456
			//		left join LexSense_Definition de on ls.dst = de.obj and de.ws = 123456
			// where le.id in (12, 23,34,... from parents)

			// rc = root class, the main table corresponding to clsid, the parent object class.
			// jn = join for the sequence property
			// tc = target class
			string baseClass = mdc.GetClassName((uint)clsid);
			int clsidTarget = info.TargetClass(this);
			string targetClass = mdc.GetClassName((uint)clsidTarget);
			string sourceFieldName = mdc.GetFieldName((uint)info.Source);
			string sourceFieldBaseClass = mdc.GetOwnClsName((uint)info.Source);
			IVwVirtualHandler handler = m_cache.VwCacheDaAccessor.GetVirtualHandlerId(info.Source);
			if (handler == null)
			{
				// If it's not a virtual property we can do a join on the table name to get the destination objects
				StringBuilder resultFields = new StringBuilder("select rc.id, rc.UpdStmp, jn.dst, tc.UpdStmp");
				StringBuilder whereClause = MakeWhereClause(parents);
				// keep lines for the same parent together, and order sequences appropriately within that
				whereClause.Append(" order by rc.id" + extraOrderBy);
				StringBuilder mainQuery = new StringBuilder(" from ");
				mainQuery.AppendFormat("{0}_ rc ", baseClass);
				mainQuery.Append("join " + sourceFieldBaseClass + "_" + sourceFieldName + " jn on jn.src = rc.id "
					+ "join " + targetClass + "_ tc on tc.id = jn.dst");

				IDbColSpec dcs = DbColSpecClass.Create();
				dcs.Push((int)DbColType.koctBaseId, 0, 0, 0);
				dcs.Push((int)DbColType.koctTimeStampIfMissing, 1, 0, 0);
				dcs.Push((int)DbColType.koctObjVec, 1, info.Source, 0);
				dcs.Push((int)DbColType.koctTimeStampIfMissing, 3, 0, 0);
				// Run the query and cache the results.
				int cFields = AddAtomicFieldInfoToQuerySections(info, mdc, resultFields, mainQuery,
					dcs, "tc", 3, 0, clsidTarget);
				string sql = resultFields.ToString() + mainQuery.ToString() + whereClause.ToString();
				m_cache.VwOleDbDaAccessor.Load(sql, dcs, 0, 0, null, false);
			}

			// Generate a new rghvoChildren by reading [info.Source] from each parent and concatenating
			// the results.

			List<int> children = new List<int>(parents.Length);
			foreach (int hvo in parents)
			{
				int chvo = m_sda.get_VecSize(hvo, info.Source);
				using (ArrayPtr arrayPtr = MarshalEx.ArrayToNative(chvo, typeof(int)))
				{
					m_sda.VecProp(hvo, info.Source, chvo, out chvo, arrayPtr);
					int[] results = (int[])MarshalEx.NativeToArray(arrayPtr, chvo, typeof(int));
					children.AddRange(results);
				}
			}
			int[] rghvoChildren = children.ToArray();
			if (handler != null && info.HasAtomicFields)
			{
				// make use of any atomic field info in 'info', by retrieving it for the objects in
				// rghvoChildren
				// For example:
				// select rc.id, f0.Txt, f0.Fmt from CmPicture rc
				//		join CmPicture_Caption f0 on f0.obj = rc.id and f0.ws = 1234
				// where rc.id in <rghvoChildren>
				StringBuilder resultFields = new StringBuilder("select rc.id ");
				StringBuilder whereClause = MakeWhereClause(rghvoChildren);
				// keep lines for the same parent together, and order sequences appropriately within that
				StringBuilder mainQuery = new StringBuilder(" from " + targetClass + " rc ");

				IDbColSpec dcs = DbColSpecClass.Create();
				dcs.Push((int)DbColType.koctBaseId, 0, 0, 0);
				// Run the query and cache the results.
				int cFields = AddAtomicFieldInfoToQuerySections(info, mdc, resultFields, mainQuery,
					dcs, "rc", 1, 0, clsidTarget);
				string sql = resultFields.ToString() + mainQuery.ToString() + whereClause.ToString();
				m_cache.VwOleDbDaAccessor.Load(sql, dcs, 0, 0, null, false);
			}


			// After we load the info about this, load it for any sequence children
			foreach (NeededPropertyInfo infoChild in info.SeqFields)
			{
				LoadChildFieldInfo(rghvoChildren, infoChild, info.TargetClass(this));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the atomic field info to query sections.
		/// </summary>
		/// <param name="info">The info.</param>
		/// <param name="mdc">The MDC.</param>
		/// <param name="resultFields">The result fields.</param>
		/// <param name="mainQuery">The main query.</param>
		/// <param name="dcs">The DCS.</param>
		/// <param name="baseAlias">alias for the class_ table that atomic fields belong to</param>
		/// <param name="baseCol">The base col.</param>
		/// <param name="cFields">The c fields.</param>
		/// <param name="clsidBaseAlias">The CLSID base alias.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private int AddAtomicFieldInfoToQuerySections(NeededPropertyInfo info, IFwMetaDataCache mdc,
			StringBuilder resultFields, StringBuilder mainQuery, IDbColSpec dcs, string baseAlias,
			int baseCol, int cFields, int clsidBaseAlias)
		{
			resultFields.Append("," + baseAlias + ".owner$," + baseAlias + ".OwnFlid$," + baseAlias + ".Class$");
			dcs.Push((int)DbColType.koctObj, baseCol, (int)CmObjectFields.kflidCmObject_Owner, 0);
			dcs.Push((int)DbColType.koctInt, baseCol, (int)CmObjectFields.kflidCmObject_OwnFlid, 0);
			dcs.Push((int)DbColType.koctInt, baseCol, (int)CmObjectFields.kflidCmObject_Class, 0);

			foreach (PropWs pw in info.AtomicFields)
			{
				if (m_cache.VwCacheDaAccessor.GetVirtualHandlerId(pw.flid) != null)
					continue;
				string fieldName = mdc.GetFieldName((uint)pw.flid);
				string fieldClassName = mdc.GetOwnClsName((uint)pw.flid);
				int clsidField = (int)mdc.GetOwnClsId((uint)pw.flid);
				string alias = "f" + cFields;
				switch (mdc.GetFieldType((uint)pw.flid))
				{
					case (int)CellarModuleDefns.kcptBigString:
					case (int)CellarModuleDefns.kcptString:
						cFields++;
						if (m_cache.IsSameOrSubclassOf(clsidBaseAlias, clsidField))
						{
							resultFields.Append("," + baseAlias + "." + fieldName);
							resultFields.Append("," + baseAlias + "." + fieldName + "_Fmt");
						}
						else
						{
							mainQuery.Append(" left outer join " + fieldClassName + " " + alias + " on " + alias + ".Id = " + baseAlias + ".Id");
							resultFields.Append("," + alias + "." + fieldName);
							resultFields.Append("," + alias + "." + fieldName + "_Fmt");
						}
						dcs.Push((int)DbColType.koctString, baseCol, pw.flid, 0);
						dcs.Push((int)DbColType.koctFmt, baseCol, pw.flid, 0);
						break;
					case (int)CellarModuleDefns.kcptBigUnicode:
					case (int)CellarModuleDefns.kcptUnicode:
						cFields++;
						AddSimpleField(resultFields, mainQuery, dcs, baseAlias, baseCol, clsidBaseAlias,
							pw, fieldName, fieldClassName, clsidField, alias, (int)DbColType.koctUnicode);
						break;
					case (int)CellarModuleDefns.kcptMultiBigString:
					case (int)CellarModuleDefns.kcptMultiString:
						cFields++;
						resultFields.Append("," + alias + ".Txt");
						resultFields.Append("," + alias + ".Fmt");
						mainQuery.Append(" left outer join " + fieldClassName + "_" + fieldName
							+ " " + alias + " on " + alias + ".obj=" + baseAlias + ".id and " + alias + ".ws = " + pw.ws);
						dcs.Push((int)DbColType.koctMlsAlt, baseCol, pw.flid, pw.ws);
						dcs.Push((int)DbColType.koctFmt, baseCol, pw.flid, pw.ws);
						break;
					case (int)CellarModuleDefns.kcptMultiBigUnicode:
					case (int)CellarModuleDefns.kcptMultiUnicode:
						cFields++;
						resultFields.Append("," + alias + ".Txt");
						mainQuery.Append(" left outer join " + fieldClassName + "_" + fieldName
							+ " " + alias + " on " + alias + ".obj=" + baseAlias + ".id and " + alias + ".ws = " + pw.ws);
						dcs.Push((int)DbColType.koctMltAlt, baseCol, pw.flid, pw.ws);
						break;
					case (int)CellarModuleDefns.kcptBoolean:
					case (int)CellarModuleDefns.kcptInteger:
						cFields++;
						AddSimpleField(resultFields, mainQuery, dcs, baseAlias, baseCol, clsidBaseAlias,
							pw, fieldName, fieldClassName, clsidField, alias, (int)DbColType.koctInt);
						break;
				}
			}
			// Add to this query based on any atomic object properties in 'SeqFields'.
			foreach (NeededPropertyInfo infoChild in info.SeqFields)
			{
				if (infoChild.IsSequence)
					continue;
				if (m_cache.VwCacheDaAccessor.GetVirtualHandlerId(infoChild.Source) != null)
					continue;
				string fieldName = mdc.GetFieldName((uint)infoChild.Source);
				string dstClassName = mdc.GetClassName((uint)infoChild.TargetClass(this));
				int clsidField = (int)mdc.GetOwnClsId((uint)infoChild.Source);
				string alias = "f" + cFields;
				bool fAtomicRef = false;
				switch (mdc.GetFieldType((uint)infoChild.Source))
				{
					case (int)CellarModuleDefns.kcptOwningAtom:
						fAtomicRef = true;
						cFields++;
						resultFields.Append("," + alias + ".id");
						mainQuery.Append(" left outer join " + dstClassName + "_ " + alias +
							" on " + alias + ".owner$ = " + baseAlias + ".id and " + alias + ".ownFlid$ = " + infoChild.Source);
						dcs.Push((int)DbColType.koctObjOwn, baseCol, infoChild.Source, 0);
						break;
					case (int)CellarModuleDefns.kcptReferenceAtom:
						fAtomicRef = true;
						cFields++;
						if (m_cache.IsSameOrSubclassOf(clsidBaseAlias, clsidField))
						{
							resultFields.Append("," + baseAlias + "." + fieldName);
							mainQuery.Append(" left outer join " + dstClassName + "_ " + alias +
								" on " + alias + ".id = " + baseAlias + "." + fieldName);
						}
						else
						{
							string alias1 = "g" + cFields;
							string fieldClassName = mdc.GetOwnClsName((uint)infoChild.Source);
							resultFields.Append("," + alias1 + "." + fieldName);
							mainQuery.Append(" left outer join " + fieldClassName + " " + alias1 + " on " +
								alias1 + ".Id = " + baseAlias + ".Id");
							mainQuery.Append(" left outer join " + dstClassName + "_ " + alias + " on " + alias +
								".Id = " + alias1 + "." + fieldName);
						}
						dcs.Push((int)DbColType.koctObj, baseCol, infoChild.Source, 0);
						break;
				}
				// Recursively, we can add any atomic fields of the atomic-related object. These fields will be
				// fields of the object in the column we just added.
				if (fAtomicRef)
				{
					int clsidAlias = infoChild.TargetClass(this);
					int iBaseCol;
					dcs.Size(out iBaseCol);
					cFields = AddAtomicFieldInfoToQuerySections(infoChild, mdc, resultFields, mainQuery, dcs,
						alias, iBaseCol, cFields, clsidAlias);
				}
			}
			return cFields;
		}

		private void AddSimpleField(StringBuilder resultFields, StringBuilder mainQuery, IDbColSpec dcs, string baseAlias, int baseCol, int clsidBaseAlias, PropWs pw, string fieldName, string fieldClassName, int clsidField, string alias, int colType)
		{
			if (m_cache.IsSameOrSubclassOf(clsidBaseAlias, clsidField))
			{
				resultFields.Append("," + baseAlias + "." + fieldName);
			}
			else
			{
				mainQuery.Append(" left outer join " + fieldClassName + " " + alias + " on " + alias + ".Id = " + baseAlias + ".Id");
				resultFields.Append("," + alias + "." + fieldName);
			}
			dcs.Push(colType, baseCol, pw.flid, 0);
		}

		private int FindReasonableFlidFor(int hvoParent, int clsid)
		{
			int clsidOwner = m_cache.GetClassOfObject(hvoParent);
			int flid = FindFlidWithMatchingDstCls(clsid, clsidOwner, (int)CellarModuleDefns.kgrfcptOwning);
			if (flid == 0)
				return FindFlidWithMatchingDstCls(clsid, clsidOwner, (int)CellarModuleDefns.kgrfcptReference);
			else
				return flid;
		}

		private int FindFlidWithMatchingDstCls(int clsidDst, int clsidOwner, int typesFlag)
		{
			int cflid = m_cache.MetaDataCacheAccessor.GetFields((uint)clsidOwner, true, typesFlag, 0, null);
			if (cflid > 0)
			{
				uint[] rgflid;
				using (ArrayPtr flids = MarshalEx.ArrayToNative(cflid, typeof(uint)))
				{
					cflid = m_cache.MetaDataCacheAccessor.GetFields((uint)clsidOwner, true,
						typesFlag, cflid, flids);
					rgflid = (uint[])MarshalEx.NativeToArray(flids, cflid, typeof(uint));
				}
				for (int i = 0; i < cflid; ++i)
				{
					int clsDst = (int)m_cache.MetaDataCacheAccessor.GetDstClsId(rgflid[i]);
					if (clsDst == clsidDst)
						return (int)rgflid[i];
				}
			}
			return 0;
		}

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
			CheckDisposed();

			try
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
					int clid = m_cache.GetClassOfObject(hvo);
					if (clid == SIL.FieldWorks.FDO.Ling.ReversalIndex.kclsidReversalIndex)
						m_wsReversal = m_sda.get_ObjectProp(hvo,
							(int)SIL.FieldWorks.FDO.Ling.ReversalIndex.ReversalIndexTags.kflidWritingSystem);
					vwenv.AddLazyVecItems(m_mainFlid, this, kRootFragId);
					return;
				}

				DisplayCommand dispCommand = m_idToDisplayCommand[fragId];
				dispCommand.PerformDisplay(this, fragId, hvo, vwenv);
			}
			catch (Exception)
			{
				throw;
			}

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
		public static XmlNode GetNodeForChild(XmlNode node, XmlNode callingFrag, LayoutCache layouts)
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
			int clsid = sda.get_IntProp(hvo, (int)CmObjectFields.kflidCmObject_Class);
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
				CheckDisposed();
				return m_layouts;
			}
		}

		// Magic number we set up a dependency on to achieve redrawing of stuff that is visible
		// when we have focus.
		internal const int FocusHvo = -1;
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
				m_CommandIcon = (stdole.IPicture)OLECvt.ToOLE_IPictureDisp(ResourceHelper.BlueCircleDownArrowForView);
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
			vwenv.NoteDependency(new int[] { hvoDepends }, new int[] { m_IsObjectSelectedTag }, 1);
			if (HasFocus && vwenv.DataAccess.get_IntProp(hvoDepends, m_IsObjectSelectedTag) == 1)
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
		public override stdole.IPicture DisplayPicture(IVwEnv vwenv, int hvo, int tag, int val, int frag)
		{
			CheckDisposed();

			// It has to be obsolete, since the new system puts DisplayCommand objects in m_idToDisplayCommand,
			// not XmlNodes.
			throw new InvalidOperationException("Obsolete system use for 'DisplayPicture' method.");
			/*
			XmlNode node = m_idToDisplayCommand[frag] as XmlNode;
			Debug.Assert(node.Name == "picturevalues"); // Review: should we check even in release build?
			// Enhance JohnT: maybe we should give an attribute 'val' to <picture> and search for it?
			XmlNode picNode = node.ChildNodes[val];
			if (picNode.Name=="icon")
			{
				string assemblyPath = XmlUtils.GetManditoryAttributeValue(picNode, "assemblyPath");
				string staticProp = XmlUtils.GetOptionalAttributeValue(picNode, "staticproperty", "");
				Image image = null;
				if (staticProp != "")
				{
					string className = XmlUtils.GetManditoryAttributeValue(picNode, "class");
					string baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase).Substring(6);
					Assembly assembly=null;
					try
					{
						assembly = Assembly.LoadFrom(Path.Combine(baseDir, assemblyPath));
					}
					catch (Exception error)
					{
						throw new RuntimeConfigurationException("XmlVc Could not find the DLL at :"+assemblyPath, error);
					}
					Type type = null;
					try
					{
						type = assembly.GetType(className);
					}
					catch (Exception error)
					{
						throw new RuntimeConfigurationException("XmlVc Could not find the class called: "+className, error);
					}
					PropertyInfo pi = type.GetProperty(staticProp, BindingFlags.Public | BindingFlags.Static | BindingFlags.GetProperty);
					if (pi == null)
						throw new Exception("public static property " + staticProp + " not found");
					object imageObj = pi.GetValue(null, null);
					image = imageObj as Image;
					if (image == null)
						throw new Exception("property " + staticProp + "did not return an image");

					//					image = (Image)type.InvokeMember(staticProp, BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.GetProperty,
					//						null, null, new object[0]);
				}
				else
				{
					// This option is not yet tested and may not work.
					object picMaker = SIL.Utils.DynamicLoader.CreateObject(picNode); // Probably ResourceHelper.
					Type picMakerType = picMaker.GetType();
					string picFunction = XmlUtils.GetManditoryAttributeValue(picNode, "method");
					image = (Image)picMakerType.InvokeMember(picFunction, System.Reflection.BindingFlags.Public,
						null, picMaker, new object[] {});
				}
				return (stdole.IPicture)SIL.FieldWorks.Common.Utils.OLECvt.ToOLE_IPictureDisp(image);
			}
			else
			{
				// whatever...we may do a 'picture' one that has a filename.
				return null;
			}*/
		}

		// Part of a horrible mechanism we are trying to get rid of by which browse view columns
		// can force a particular WS to be used for all strings. Overridden by XmlBrowseViewBaseVc.
		internal virtual int WsForce
		{
			get { return 0; }
			set { Debug.Fail("WsForce can only be set in browse VCs."); }
		}


		private bool IsItemEmpty(int hvo, int fragId)
		{
			// If it's not the kind of vector we know how to deal with, safest to assume the item
			// is not empty.
			if (!m_idToDisplayCommand.ContainsKey(fragId))
				return false;

			MainCallerDisplayCommand command = m_idToDisplayCommand[fragId] as MainCallerDisplayCommand;
			string layoutName;
			XmlNode node = command.GetNodeForChild(out layoutName, fragId, this, hvo);
			string[] keys = XmlViewsUtils.ChildKeys(m_cache, node, hvo, m_layouts, command.Caller, m_stringTable, WsForce);
			return AreAllKeysEmpty(keys);
		}

		/// <summary>
		/// Return true if every key in the array is empty or null. This includes the case of zero keys.
		/// </summary>
		/// <param name="keys"></param>
		/// <returns></returns>
		private bool AreAllKeysEmpty(string[] keys)
		{
			if (keys == null)
				return true;
			foreach (string s in keys)
				if (s != null && s.Length != 0)
					return false;
			return true;
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
			CheckDisposed();

			if (frag <= 2)
			{
				// Should we do something special here?
				return;
			}
			XmlNode listDelimitNode; // has the list seps attrs like 'sep'
			XmlNode specialAttrsNode;  // has the more exotic ones like 'excludeHvo'
			int childFrag;
			MainCallerDisplayCommand dispInfo = (MainCallerDisplayCommand)m_idToDisplayCommand[frag];
			listDelimitNode = specialAttrsNode = dispInfo.MainNode;
			// 'inheritSeps' attr means to use the 'caller' (the part ref node) to get the
			// seperator information.
			if (XmlUtils.GetOptionalBooleanAttributeValue(listDelimitNode, "inheritSeps", false))
			{
				listDelimitNode = dispInfo.Caller;
			}
			childFrag = frag;
			// 1. get number of items in vector
			// 2. for each item in the vector,
			//		a) print leading number if desired.
			//		b) call AddObj
			//		c) print separator if desired and needed
			int chvo = vwenv.DataAccess.get_VecSize(hvo, flid);
			if (chvo == 0)
			{
				// We may want to do something special for empty vectors.  See LT-9687.
				ProcessEmptyVector(vwenv, hvo, flid);
				// Display empty vector indicator?
				return;
			}
			int[] rghvo = GetVector(vwenv.DataAccess, hvo, flid);
			Debug.Assert(chvo == rghvo.Length);
			int ihvo;
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			string strEng = "en";
			int wsEng = m_cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr(strEng);
			XmlAttribute xaSep = listDelimitNode.Attributes["sep"];
			XmlAttribute xaNum = listDelimitNode.Attributes["number"];
			// Note that we deliberately don't use the listDelimitNode here. These three props are not currently configurable,
			// and they belong on the 'seq' element, not the part ref.
			bool fCheckForEmptyItems = XmlUtils.GetOptionalBooleanAttributeValue(specialAttrsNode, "checkForEmptyItems", false);
			string exclude = XmlUtils.GetOptionalAttributeValue(specialAttrsNode, "excludeHvo", null);
			bool fFirstOnly = XmlUtils.GetOptionalBooleanAttributeValue(specialAttrsNode, "firstOnly", false);
			bool fNumber = xaNum != null && xaNum.Value != null && xaNum.Value != "";
			if (fNumber && chvo == 1)
				fNumber = XmlUtils.GetOptionalBooleanAttributeValue(listDelimitNode, "numsingle", false);
			string sort = XmlUtils.GetOptionalAttributeValue(specialAttrsNode, "sort", null);
			if (sort != null)
			{
				// sort the items in this collection, based on the SortKey property
				bool ascending = sort.ToLowerInvariant() == "ascending";
				List<int> hvos = new List<int>(rghvo);
				using (CmObjectComparer comparer = new CmObjectComparer(m_cache))
					hvos.Sort(comparer);
				if (!ascending)
					hvos.Reverse();
				hvos.CopyTo(rghvo);
			}

			// Check whether the user wants the grammatical information to appear only once, preceding any
			// sense numbers, if there is only one set of grammatical information, and all senses refer to
			// it.  See LT-9663.
			bool fSingleGramInfoFirst = false;
			if (flid == (int)SIL.FieldWorks.FDO.Ling.LexEntry.LexEntryTags.kflidSenses)
				fSingleGramInfoFirst = XmlUtils.GetOptionalBooleanAttributeValue(listDelimitNode, "singlegraminfofirst", false);
			if (fSingleGramInfoFirst && chvo > 0 && fNumber)
			{
				int flidSenseMsa = (int)SIL.FieldWorks.FDO.Ling.LexSense.LexSenseTags.kflidMorphoSyntaxAnalysis;
				int hvoMsa = m_sda.get_ObjectProp(rghvo[0], flidSenseMsa);
				bool fAllMsaSame = SubsenseMsasMatch(rghvo[0], hvoMsa);
				for (int i = 1; fAllMsaSame && i < chvo; ++i)
				{
					int hvoMsa2 = m_sda.get_ObjectProp(rghvo[i], flidSenseMsa);
					fAllMsaSame = hvoMsa == hvoMsa2 && SubsenseMsasMatch(rghvo[i], hvoMsa);
				}
				m_fIgnoreGramInfoAfterFirst = fAllMsaSame;
			}
			// This groups senses by placing graminfo before the number, and omitting it if the same as the
			// previous sense in the entry.  This isn't yet supported by the UI, but may well be requested in
			// the future.  (See LT-9663.)
			//bool fGramInfoBeforeNumber = XmlUtils.GetOptionalBooleanAttributeValue(listDelimitNode, "graminfobeforenumber", false);
			ITsTextProps ttpNum = null;
			if (fNumber)
			{
				ITsPropsFactory tpf = TsPropsFactoryClass.Create();
				ITsPropsBldr tpb;
				tpb = tpf.GetPropsBldr();
				// TODO: find more appropriate writing system?
				tpb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, wsEng);
				XmlAttribute xaStyle = listDelimitNode.Attributes["numstyle"];
				if (xaStyle != null && xaStyle.Value != null && xaStyle.Value != "")
				{
					string style = xaStyle.Value.ToLower();
					if (style.IndexOf("-bold") >= 0)
					{
						tpb.SetIntPropValues((int)FwTextPropType.ktptBold,
							(int)FwTextPropVar.ktpvEnum,
							(int)FwTextToggleVal.kttvOff);
					}
					else if (style.IndexOf("bold") >= 0)
					{
						tpb.SetIntPropValues((int)FwTextPropType.ktptBold,
							(int)FwTextPropVar.ktpvEnum,
							(int)FwTextToggleVal.kttvForceOn);
					}
					if (style.IndexOf("-italic") >= 0)
					{
						tpb.SetIntPropValues((int)FwTextPropType.ktptItalic,
							(int)FwTextPropVar.ktpvEnum,
							(int)FwTextToggleVal.kttvOff);
					}
					else if (style.IndexOf("italic") >= 0)
					{
						tpb.SetIntPropValues((int)FwTextPropType.ktptItalic,
							(int)FwTextPropVar.ktpvEnum,
							(int)FwTextToggleVal.kttvForceOn);
					}
				}
				XmlAttribute xaFont = listDelimitNode.Attributes["numfont"];
				if (xaFont != null && xaFont.Value != null && xaFont.Value != "")
				{
					tpb.SetStrPropValue((int)FwTextPropType.ktptFontFamily,
						xaFont.Value);
				}
				ttpNum = tpb.GetTextProps();
			}

			// A vector may be conditionally configured to display its objects as separate paragraphs
			// in dictionary (document) configuration.  See LT-9667.
			bool fShowAsParagraphs = XmlUtils.GetOptionalBooleanAttributeValue(listDelimitNode, "showasindentedpara", false);
			ITsString tssBefore = null;
			if (fShowAsParagraphs && chvo > 0)
			{
				string sBefore = XmlUtils.GetLocalizedAttributeValue(m_stringTable, listDelimitNode, "before", null);
				if (!String.IsNullOrEmpty(sBefore))
					tssBefore = m_cache.MakeUserTss(sBefore);
				// We need a line break here to force the inner pile of paragraphs to begin at
				// the margin, rather than somewhere in the middle of the line.
				vwenv.AddString(m_cache.MakeAnalysisTss("\x2028"));
				vwenv.OpenInnerPile();
			}
			bool fFirst = true; // May actually mean first non-empty.
			for (ihvo = 0; ihvo < chvo; ++ihvo)
			{
				if (exclude != null)
				{
					if (exclude == "this" && rghvo[ihvo] == hvo)
						continue;
					else if (exclude == "parent")
					{
						int hvoParent, tagDummy, ihvoDummy;
						vwenv.GetOuterObject(vwenv.EmbeddingLevel - 1, out hvoParent, out tagDummy, out ihvoDummy);
						if (rghvo[ihvo] == hvoParent)
							continue;
					}
				}

				if (fCheckForEmptyItems && IsItemEmpty(rghvo[ihvo], childFrag))
					continue;

				if (fShowAsParagraphs)
				{
					vwenv.OpenParagraph();
					if (tssBefore != null)
						vwenv.AddString(tssBefore);
				}
				else if (!fFirst && xaSep != null)
				{
					// add the separator.
					string sSep = !string.IsNullOrEmpty(xaSep.Value) ? xaSep.Value : " ";
					ITsString tss = tsf.MakeString(sSep, wsEng);
					vwenv.AddString(tss);
				}
				// add the numbering if needed.
				if (fNumber)
				{
					string sNum = "";
					string sTag = xaNum.Value;
					int ich;
					for (ich = 0; ich < sTag.Length; ++ich)
					{
						if (sTag[ich] == '%' && ich + 1 < sTag.Length)
						{
							++ich;
							if (sTag[ich] == 'd')
							{
								sNum = string.Format("{0}", ihvo + 1);
								break;
							}
							else if (sTag[ich] == 'A')
							{
								sNum = AlphaOutline.NumToAlphaOutline(ihvo + 1);
								break;
							}
							else if (sTag[ich] == 'a')
							{
								sNum = AlphaOutline.NumToAlphaOutline(ihvo + 1).ToLower();
								break;
							}
							else if (sTag[ich] == 'I')
							{
								sNum = RomanNumerals.IntToRoman(ihvo + 1);
								break;
							}
							else if (sTag[ich] == 'i')
							{
								sNum = RomanNumerals.IntToRoman(ihvo + 1).ToLower();
								break;
							}
							else if (sTag[ich] == 'O')
							{
								if (m_mdc.get_IsVirtual((uint)flid))
									sNum = String.Format("{0}", ihvo + 1);
								else
									sNum = m_cache.GetOutlineNumber(rghvo[ihvo], flid, false, true);
								break;
							}
							else if (sTag[ich] == 'z')
							{
								// Top-level: Arabic numeral.
								// Second-level: lowercase letter.
								// Third-level (and lower): lowercase Roman numeral.
								string sOutline = m_cache.GetOutlineNumber(rghvo[ihvo], flid, false, true);
								int cchPeriods = 0;
								int ichPeriod = sOutline.IndexOf('.');
								while (ichPeriod >= 0)
								{
									++cchPeriods;
									ichPeriod = sOutline.IndexOf('.', ichPeriod + 1);
								}
								switch (cchPeriods)
								{
									case 0: sNum = string.Format("{0}", ihvo + 1); break;
									case 1: sNum = AlphaOutline.NumToAlphaOutline(ihvo + 1).ToLower(); break;
									default: sNum = RomanNumerals.IntToRoman(ihvo + 1).ToLower(); break;
								}
								break;
							}
							else if (sTag[ich] == '%')
							{
								// %% displays a single %.
								sTag.Remove(ich, 1);
								--ich;
								continue;
							}
						}
					}
					if (sNum.Length != 0)
						sTag = (sTag.Remove(ich - 1, 2)).Insert(ich - 1, sNum);

					ITsStrBldr tsb = tsf.GetBldr();
					tsb.Replace(0, 0, sTag, ttpNum);
					ITsString tss = tsb.GetString();
					// If so desired, delay the first sense number until after the MSA has been
					// displayed.  See LT-9663.
					if (fSingleGramInfoFirst && m_fIgnoreGramInfoAfterFirst && fFirst)
					{
						m_tssDelayedNumber = tss;
					}
					else
					{
						vwenv.AddString(tss);
						m_tssDelayedNumber = null;
					}
					// This groups senses by placing graminfo before the number, and omitting it if the same as the
					// previous sense in the entry.  This isn't yet supported by the UI, but may well be requested in
					// the future.  (See LT-9663.)
					//if (fGramInfoBeforeNumber)
					//{
					//    m_tssDelayedNumber = tss;
					//    if (fFirst)
					//        m_hvoGroupedValue = 0;
					//}
					//else
					//{
					//    vwenv.AddString(tss);
					//    m_tssDelayedNumber = null;
					//}
				}
				// add the object.
				Debug.Assert(ihvo < rghvo.Length);
				vwenv.AddObj(rghvo[ihvo], this, childFrag);
				fFirst = false;
				if (fShowAsParagraphs)
				{
					vwenv.CloseParagraph();
				}
				if (fFirstOnly)
					break;
			}
			if (fShowAsParagraphs && chvo > 0)
			{
				vwenv.CloseInnerPile();
			}
			// Reset the flag for ignoring grammatical information after the first if it was set
			// earlier in this method.
			if (fSingleGramInfoFirst)
				m_fIgnoreGramInfoAfterFirst = false;
		}

		private void ProcessEmptyVector(IVwEnv vwenv, int hvo, int flid)
		{
			// If we're collecting displayed items, and we could have a list of items but don't,
			// add an hvo of 0 to the collection for use in filtering on missing information.
			// See LT-9687.
			if (vwenv is XmlBrowseViewBaseVc.ItemsCollectorEnv)
			{
				// The complexities of LexEntryRef objects makes the following special case code
				// necessary to achieve satisfactory results for LT-9687.
				if (flid == (int)LexEntryRef.LexEntryRefTags.kflidComplexEntryTypes)
				{
					int type = m_cache.GetIntProperty(hvo, (int)LexEntryRef.LexEntryRefTags.kflidRefType);
					if (type != LexEntryRef.krtComplexForm)
						return;
				}
				else if (flid == (int)LexEntryRef.LexEntryRefTags.kflidVariantEntryTypes)
				{
					int type = m_cache.GetIntProperty(hvo, (int)LexEntryRef.LexEntryRefTags.kflidRefType);
					if (type != LexEntryRef.krtVariant)
						return;
				}
				if ((vwenv as XmlBrowseViewBaseVc.ItemsCollectorEnv).HvosCollectedInCell.Count == 0)
					(vwenv as XmlBrowseViewBaseVc.ItemsCollectorEnv).HvosCollectedInCell.Add(0);
			}
		}

		/// <summary>
		/// Get the items from a vector property.
		/// </summary>
		int[] GetVector(ISilDataAccess sda, int hvo, int tag)
		{
			int chvo = sda.get_VecSize(hvo, tag);
			using (ArrayPtr arrayPtr = MarshalEx.ArrayToNative(chvo, typeof(int)))
			{
				sda.VecProp(hvo, tag, chvo, out chvo, arrayPtr);
				return (int[])MarshalEx.NativeToArray(arrayPtr, chvo, typeof(int));
			}
		}

		/// <summary>
		/// Check whether all the subsenses (if any) use the given MSA.
		/// </summary>
		private bool SubsenseMsasMatch(int hvoSense, int hvoMsa)
		{
			int[] rghvoSubsense = GetVector(m_sda, hvoSense,
				(int)SIL.FieldWorks.FDO.Ling.LexSense.LexSenseTags.kflidSenses);
			for (int i = 0; i < rghvoSubsense.Length; ++i)
			{
				int hvoMsa2 = m_sda.get_ObjectProp(rghvoSubsense[i],
					(int)SIL.FieldWorks.FDO.Ling.LexSense.LexSenseTags.kflidMorphoSyntaxAnalysis);
				if (hvoMsa != hvoMsa2 || !SubsenseMsasMatch(rghvoSubsense[i], hvoMsa))
					return false;
			}
			return true;
		}

		/// <summary>
		/// Come up with an estimate of the height of a single item.
		/// The current approach is based on collecting the text for an item, and assuming no very unusual
		/// fonts or multi-paragraph layouts, estimating how many lines it will take. This is pretty approximate,
		/// but it's difficult to actually measure without having a VwEnv as a starting point.
		/// It's too expensive to do this for all items (this routine is typically called for every item in the
		/// collection, BEFORE we LoadData for any of the items). So we arrange to sample log(n) items, by
		/// only doing the calculation when the item count is a power of two.
		/// Also, we have to forget our estimtaes if the available width changes.
		/// We return an average of the items we've estimated for all items, to allow the lazy box to take
		/// advantage of a uniform height estimate.
		///
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
		/// <param name="hvo"></param>
		/// <param name="frag"></param>
		/// <param name="dxAvailWidth"></param>
		/// <returns></returns>
		public override int EstimateHeight(int hvo, int frag, int dxAvailWidth)
		{
			CheckDisposed();
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
					using (Font font = new Font("Arial", 12.0F))
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
			int clsid = m_cache.GetClassOfObject(hvo);
			int flid = (int)m_cache.MetaDataCacheAccessor.GetFieldId2((uint)clsid, propName, true);
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
			CheckDisposed();

			string sWs = XmlUtils.GetOptionalAttributeValue(frag, "ws");
			if (sWs == null)
				return;

			Debug.Assert(s_qwsCurrent == null);
			Debug.Assert(s_cwsMulti == 0);
			try
			{
				Set<int> wsIds = LangProject.GetAllWritingSystems(frag, m_cache, s_qwsCurrent, 0, 0);
				s_cwsMulti = wsIds.Count;
				if (s_cwsMulti > 1)
					s_sMultiSep = XmlUtils.GetOptionalAttributeValue(frag, "sep");
				s_fMultiFirst = true;
				foreach (int WSId in wsIds)
				{
					s_qwsCurrent = m_cache.LanguageWritingSystemFactoryAccessor.get_EngineOrNull(WSId);
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

			string sLabel = qws.get_Abbr(cache.DefaultUserWs);
			if (sLabel == null)
				sLabel = qws.IcuLocale;
			if (sLabel == null)
				sLabel = XMLViewsStrings.ksUNK;
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			ITsIncStrBldr tisb = tsf.GetIncBldr();
			tisb.SetIntPropValues((int)FwTextPropType.ktptWs,
				0, cache.DefaultUserWs);
			//			tisb.SetStrPropValue((int)FwTextPropType.ktptCharStyle,
			//				"Language Code");
			// This is the formula (red + (blue * 256 + green) * 256) for a
			// FW RGB color, applied to the standard FW color "light blue".
			// This is the default defn of the "Language Code" character
			// style in DN. We could just use this style, except I'm not
			// sure Oyster is yet using style sheets.
			//			tisb.SetIntPropValues((int)FwTextPropType.ktptForeColor,
			//				(int)FwTextPropVar.ktpvDefault, 47 + (255 * 256 + 96) * 256);
			// And this makes it 8 point.
			tisb.SetIntPropValues((int)FwTextPropType.ktptFontSize,
				(int)FwTextPropVar.ktpvMilliPoint, 8000);
			tisb.SetIntPropValues((int)FwTextPropType.ktptEditable,
				(int)FwTextPropVar.ktpvEnum,
				(int)TptEditable.ktptNotEditable);
			tisb.Append(sLabel);
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
					ITsStrFactory tsf = TsStrFactoryClass.Create();
					int wsUi = cache.LanguageWritingSystemFactoryAccessor.UserWs;
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
							ProcessProperties(frag, vwenv);
							int dmpAlign = XmlUtils.GetMandatoryIntegerAttributeValue(frag, "align");
							vwenv.OpenConcPara(GetIntFromNamedProp("min", frag, hvo),
								GetIntFromNamedProp("lim", frag, hvo), VwConcParaOpts.kcpoDefault, dmpAlign);
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

		private void AddMultipleAlternatives(Set<int> wsIds, IVwEnv vwenv, int hvo, int flid, XmlNode frag, bool fCurrentHvo)
		{
			string sep = XmlUtils.GetOptionalAttributeValue(frag, "sep", null);
			ITsString tssSep = null;
			if (sep != null)
				tssSep = m_cache.MakeUserTss(sep);
			bool fLabel = XmlUtils.GetOptionalBooleanAttributeValue(frag, "showLabels", false); // true to 'separate' using multistring labels.
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
					vwenv.AddString(tssSep);
				}
				if (vwenv is ConfiguredExport)
					(vwenv as ConfiguredExport).BeginMultilingualAlternative(ws);
				if (fLabel)
				{
					IWritingSystem wsEngine = m_cache.LanguageWritingSystemFactoryAccessor.get_EngineOrNull(ws);
					DisplayWsLabel(wsEngine, vwenv, m_cache);
				}
				if (fCurrentHvo)
					vwenv.AddStringAltMember(flid, ws, this);
				else
					AddStringThatCounts(vwenv, tss);
				if (vwenv is ConfiguredExport)
					(vwenv as ConfiguredExport).EndMultilingualAlternative();
			}
		}

		/// <summary>
		/// Add a string that 'counts' for purposes of being treated as a non-empty display.
		/// Normally AddString is used for labels and such that don't count, but sometimes for
		/// important things like name of owner.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="tss"></param>
		private void AddStringThatCounts(IVwEnv vwenv, ITsString tss)
		{
			if (vwenv is TestCollectorEnv)
				(vwenv as TestCollectorEnv).AddTsString(tss);
			else
				vwenv.AddString(tss);
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
			CheckDisposed();

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
							GetActualTarget(frag, ref hvoTarget, m_cache);	// modify the hvo if needed
							if (hvo != hvoTarget)
							{
								AddStringFromOtherObj(frag, hvoTarget, vwenv);
								break;
							}
							int flid = 0;
							if (TryCustomField(Cache, frag, out flid))
							{
								// ignore invalid custom fields (LT-6474).
								if (flid == 0)
									break;
							}
							else
							{
								flid = GetFlid(frag, hvo);
							}

							NoteDependencies(vwenv, hvo, flid);
							int itype = m_cache.MetaDataCacheAccessor.GetFieldType((uint)flid);
							itype = itype & 0x1f; // strip virtual bit
							if ((itype == (int)FieldType.kcptUnicode) ||
								(itype == (int)FieldType.kcptBigUnicode))
							{
								int wsForUnicode = GetWritingSystemForObject(frag, hvo, flid, m_cache.DefaultUserWs);
								vwenv.AddUnicodeProp(flid, wsForUnicode, this);
							}
							else if ((itype == (int)FieldType.kcptString) ||
								(itype == (int)FieldType.kcptBigString))
							{
								vwenv.AddStringProp(flid, this);
							}
							else // multistring of some type
							{
								if (s_cwsMulti > 1)
								{
									string sLabelWs = XmlUtils.GetOptionalAttributeValue(frag, "ws");
									if (sLabelWs != null && sLabelWs == "current")
									{
										DisplayMultiSep(frag, vwenv, m_cache);
										DisplayWsLabel(s_qwsCurrent, vwenv, m_cache);
										if (s_qwsCurrent != null)
											vwenv.AddStringAltMember(flid, s_qwsCurrent.WritingSystem, this);
									}
								}
								else
								{
									int wsid = GetWritingSystemForObject(frag, hvo, flid, m_cache.DefaultUserWs);
									vwenv.AddStringAltMember(flid, wsid, this);
								}
							}
							break;
						}
					case "configureMlString":
						{
							int hvoTarget = hvo;
							GetActualTarget(frag, ref hvoTarget, m_cache);	// modify the hvo if needed
							int flid = 0;
							if (TryCustomField(Cache, frag, out flid))
							{
								// ignore invalid custom fields (LT-6474).
								if (flid == 0)
									break;
							}
							else
							{
								flid = GetFlid(frag, hvoTarget);
							}
							// The Ws info specified in the part ref node
							Set<int> wsIds;
							string sWs = XmlUtils.GetOptionalAttributeValue(caller, "ws");
							if (sWs == "reversal")
							{
								wsIds = new Set<int>();
								wsIds.Add(m_wsReversal);
							}
							else
							{
								wsIds = LangProject.GetAllWritingSystems(sWs, m_cache, null, hvoTarget, flid);
							}
							if (wsIds.Count == 1)
							{
								if (hvoTarget != hvo)
									DisplayOtherObjStringAlt(flid, wsIds.ToArray()[0], vwenv, hvoTarget);
								else
									vwenv.AddStringAltMember(flid, wsIds.ToArray()[0], this);
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
							// Note: the following assertion is indicative of not setting colSpec attribute multipara="true"
							// VwEnv.cpp: Assertion failed "Expression: dynamic_cast<VwPileBox *>(m_pgboxCurr)"
							vwenv.OpenParagraph();
							ProcessChildren(frag, vwenv, hvo);
							vwenv.CloseParagraph();
							break;
						}
					case "concpara":
						{
							ProcessProperties(frag, vwenv);
							int dmpAlign = XmlUtils.GetMandatoryIntegerAttributeValue(frag, "align");
							vwenv.OpenConcPara(GetIntFromNamedProp("min", frag, hvo),
								GetIntFromNamedProp("lim", frag, hvo), VwConcParaOpts.kcpoDefault, dmpAlign);
							ProcessChildren(frag, vwenv, hvo);
							vwenv.CloseParagraph();
							break;
						}
					case "div":
						{
							ProcessProperties(frag, vwenv);
							vwenv.OpenDiv();
							ProcessChildren(frag, vwenv, hvo);
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
							IWritingSystem lgws = Cache.LanguageWritingSystemFactoryAccessor.get_EngineOrNull(Cache.DefaultVernWs);
							if (lgws != null && lgws.RightToLeft)
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
								vwenv.NoteDependency(new int[] { hvo, hvo }, new int[] {
																				 (int)FDO.Ling.MoInflAffixTemplate.MoInflAffixTemplateTags.kflidPrefixSlots,
																				 (int)FDO.Ling.MoInflAffixTemplate.MoInflAffixTemplateTags.kflidSuffixSlots
																			 }, 2);
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

							int fragId = GetSubFragId(frag, caller);
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
							// If the value of the property is null, we used to get an Assert by adding it.
							// Now the Views code is smarter, and generates a useful dependency to regenerate
							// if the value gets set.
							// Enhance JohnT: one day we may want to do something smarter here...
							//						if (m_cache.GetObjProperty(hvo, flid)== 0)
							//							return;

							/*						string path =XmlUtils.GetOptionalAttributeValue(frag, "assemblyPath", "") ;
															if(path!= "")
															{
																//notice we don't use the normal "class" argument because that is already used for
																//the CELLAR class in this kind of element
																string dotnetclass = XmlUtils.GetOptionalAttributeValue(frag, "dotnetclass", "");
																//noticed this assumes that it wants a FdoCache as an argument
																IVwViewConstructor vc = (IVwViewConstructor)SIL.Utils.DynamicLoader.CreateObject(path,dotnetclass, new Object[]{m_cache});
																//vwenv.AddObjProp(xflid, vc,  InterlinVc.kfragTwficAnalysis, m_styleSheet);
																vwenv.AddObjProp(flid, vc,  0);
															}
															else
									*/
							{
								int fragId = GetSubFragId(frag, caller);
								if (flid == 0 || fragId == 0)
									return; // something badly wrong.
								// If the value of the property is null, we get an Assert by adding it.
								// So don't.
								// Enhance JohnT: one day we may want to do something smarter here...
								//						if (m_cache.GetObjProperty(hvo, flid) != 0)
								vwenv.AddObjProp(flid, this, fragId);
							}
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
							vwenv.AddIntProp(flid);
							break;
						}
					case "datetime":
						{
							int flid = GetFlid(frag, hvo);
							if (flid == 0)
								return; // something badly wrong.
							FieldType itype = m_cache.GetFieldType(flid);
							if (itype == FieldType.kcptTime)
							{
								DateTime dt = m_cache.GetTimeProperty(hvo, flid);
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
									throw new SIL.Utils.ConfigurationException(errorMsg, frag);
								}
								ITsStrFactory tsf = TsStrFactoryClass.Create();
								int systemWs = m_cache.DefaultUserWs;
								ITsString tss = tsf.MakeString(formattedDateTime, systemWs);
								if (vwenv is ConfiguredExport)
									vwenv.AddTimeProp(flid, 0);
								AddStringThatCounts(vwenv, tss);
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
								ws = m_cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr(sWs);
							else
								ws = m_cache.LangProject.DefaultUserWritingSystem;
							ITsStrFactory tsf = TsStrFactoryClass.Create();
							ITsString tss = tsf.MakeString(literal, ws);
							int flid = 0;
							if (TryGetFlid(frag.ParentNode, hvo, out flid))
							{
								NoteDependencies(vwenv, hvo, flid);
							}
							vwenv.AddString(tss);
							break;
						}
					case "if":
						{
							if (ConditionPasses(vwenv, frag, hvo, m_cache, caller))
								ProcessChildren(frag, vwenv, hvo, caller);
							break;
						}
					case "ifnot":
						{
							if (!ConditionPasses(vwenv, frag, hvo, m_cache, caller))
								ProcessChildren(frag, vwenv, hvo, caller);
							break;
						}
					case "choice":
						{
							foreach (XmlNode clause in frag.ChildNodes)
							{
								if (clause.Name == "where")
								{
									if (ConditionPasses(vwenv, clause, hvo, m_cache, caller))
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
								ITsStrFactory tsf = TsStrFactoryClass.Create();
								int wsUi =
									m_cache.LanguageWritingSystemFactoryAccessor.UserWs;
								ITsString tss = tsf.MakeString(labels[value], wsUi);
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
						ProcessPartRef(frag, hvo, vwenv);
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
								string style = XmlUtils.GetOptionalAttributeValue(frag, "style", null);
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
						ICmPicture picture = CmPicture.CreateFromDBObject(m_cache, hvo);
						if (picture == null || picture.PictureFileRA == null)
							break; // nothing we can show. // Enhance: should we insert some sort of error?
						string imagePath = picture.PictureFileRA.AbsoluteInternalPath;
						if (String.IsNullOrEmpty(imagePath))
							break;
						stdole.IPicture comPicture = null;
						Image image;
						try
						{
							image = Image.FromFile(FileUtils.ActualFilePath(imagePath));
							if (image != null)
								comPicture = (stdole.IPicture)SIL.FieldWorks.Common.Utils.OLECvt.ToOLE_IPictureDisp(image);
						}
						catch (Exception e)
						{
							Debug.WriteLine("Failed to create picture from path " + imagePath + " exception: " + e.Message);
							image = null; // if we can't get the picture too bad.
						}
						if (image != null)
						{
							int height = XmlUtils.GetOptionalIntegerValue(frag, "height", 0);
							int width = XmlUtils.GetOptionalIntegerValue(frag, "width", 0);
							vwenv.AddPicture(comPicture, 1, width, height);
						}
						// for export, we want the path, but not for these other cases.  (LT-5326)
						if (vwenv is ConfiguredExport || vwenv is TestCollectorEnv)
						{
							int fragId = GetSubFragId(frag, caller);
							vwenv.AddObjProp((int)CmPicture.CmPictureTags.kflidPictureFile, this, fragId);
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
					"There was an error processing this fragment. " + error.Message, frag);
			}
		}

		/// <summary>
		/// custom field identified during TryColumnForCustomField. Is not currently meaningful outside TryColumnForCustomField().
		/// </summary>
		XmlNode m_customFieldNode = null;
		/// <summary>
		/// Determine whether or not the given colSpec refers to a custom field, respective of whether or not it is still valid.
		/// Uses layout/parts to find custom field specifications.
		/// </summary>
		/// <param name="colSpec"></param>
		/// <param name="rootObjClass">the (base)class of items being displayed</param>
		/// <param name="customFieldNode">the xml node of the custom field, if we found one.</param>
		/// <param name="propWs">the first prop/ws info we could find for the custom field,
		/// null if customField is invalid for this database.</param>
		/// <returns></returns>
		internal bool TryColumnForCustomField(XmlNode colSpec, uint rootObjClass, out XmlNode customFieldNode, out PropWs propWs)
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
		internal XmlNode GetPartFromParentNode(XmlNode parentNode, uint rootObjClass)
		{
			XmlNode partNode = null;
			string layout = XmlUtils.GetOptionalAttributeValue(parentNode, "layout");
			if (layout != null)
			{
				// get the part from the layout.
				partNode = this.GetNodeForPart(layout, false, (int)rootObjClass);
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
		internal bool TryFindFirstAtomicField(XmlNode parentNode, uint rootObjClass, out PropWs propWs)
		{
			propWs = null;
			XmlNode partNode = GetPartFromParentNode(parentNode, rootObjClass);
			NeededPropertyInfo info = DetermineNeededFieldsForSpec(parentNode, rootObjClass);
			if (info != null)
			{
				// if we have a sequence, step down the first branch till we find the first atomic field.
				propWs = FindFirstAtomicPropWs(info);
			}
			return propWs != null;
		}

		internal NeededPropertyInfo DetermineNeededFieldsForSpec(XmlNode spec, uint rootObjClass)
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
							IFwMetaDataCache mdc = m_cache.MetaDataCacheAccessor;
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
										if (stClassName == null || stClassName.Length == 0)
										{
											int classId = info.TargetClass(this);
											flid = (int)mdc.GetFieldId2((uint)classId, rgstFields[0], true);
										}
										else
										{
											flid = (int)mdc.GetFieldId(stClassName, rgstFields[0], true);
										}
										// on entry to each iteration, flid is the flid resulting from
										// rgstFields[i-1]. On successful exit, it is the flid of the last
										// item, the real field, which is processed normally.
										for (int i = 1; i < rgstFields.Length; i++)
										{
											if (m_cache.VwCacheDaAccessor.GetVirtualHandlerId(flid) != null)
											{
												flid = 0; // can't follow path further
												break;
											}
											// We assume this intermediate flid is an object property.
											infoTarget = infoTarget.AddObjField(flid, false);
											string subFieldName = rgstFields[i];
											int outerClassId = (int)mdc.GetDstClsId((uint)flid);
											flid = (int)mdc.GetFieldId2((uint)outerClassId, subFieldName, true);
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
										wsid = s_qwsCurrent.WritingSystem;
									}
									else
									{
										// If ws is 'configure' then we must have a caller to inherit the configured value from.
										if (sWs == "configure")
										{
											sWs = XmlUtils.GetManditoryAttributeValue(caller, "ws");
										}
										wsid = m_cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr(sWs);
									}
								}
								if (wsid == 0 && sWs != null)
								{
									foreach (int ws in LangProject.GetWritingSystems(frag, m_cache))
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
							if (!TryCustomField(Cache, frag, out flid))
								flid = DetermineNeededFlid(frag, info);
							else
								m_customFieldNode = frag;

							// If we don't have enough info to determine a flid, give up.
							if (flid == 0)
								return;

							int itype = m_cache.MetaDataCacheAccessor.GetFieldType((uint)flid);
							itype = itype & 0x1f; // strip virtual bit
							if ((itype == (int)FieldType.kcptMultiBigString) ||
								(itype == (int)FieldType.kcptMultiBigUnicode) ||
								(itype == (int)FieldType.kcptMultiString) ||
								(itype == (int)FieldType.kcptMultiUnicode))
							{
								if (s_cwsMulti > 1)
								{
									string sLabelWs = XmlUtils.GetOptionalAttributeValue(frag, "ws");
									if (sLabelWs != null && sLabelWs == "current")
									{
										if (s_qwsCurrent != null)
											info.AddAtomicField(flid, s_qwsCurrent.WritingSystem);
									}
								}
								else
								{
									foreach (int wsid in LangProject.GetWritingSystems(frag, m_cache))
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
							if (!TryCustomField(Cache, frag, out flid))
								flid = DetermineNeededFlid(frag, info);
							else
								m_customFieldNode = frag;
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
								foreach (int wsid in LangProject.GetWritingSystems(caller, m_cache))
									info.AddAtomicField(flid, wsid);
							}
							break;
						}
					case "int":
						{
							int flid = DetermineNeededFlid(frag, info);
							if (flid == 0)
								return;
							info.AddAtomicField(flid, 0);
						}
						break;
					case "seq":
						{
							int flid = DetermineNeededFlid(frag, info, true);
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
							IVwVirtualHandler handler = m_cache.VwCacheDaAccessor.GetVirtualHandlerId(flid);
							if (handler == null)
							{
								NeededPropertyInfo subinfo = info.AddObjField(flid, true);
								DisplayCommand dispCommand = m_idToDisplayCommand[fragId];
								dispCommand.DetermineNeededFields(this, fragId, subinfo);
							}
							else
							{
								// Look for hint info on what to load.
								// For example,
								// <seq field="PicturesOfSenses" layout="publishStem" inheritSeps="true" targetclass="CmPicture"
								//		loadHint="CmPicture:LexEntry.Senses/LexSense.Pictures;LexEntry.Senses/LexSense.Senses/LexSense.Pictures"/>
								// (we tried only this one hint, and it didn't seem to help any, so we removed it from the XML.)

								// Enhance JohnT: We also considered creating another kind of hint that would actually specify
								// a bit of SQL that provides the source and destination columns for the query. We'd have to save
								// this in a new subclass of VirtualNeededPropertyInfo for later use in generating the query.
								string hint = XmlUtils.GetOptionalAttributeValue(frag, "loadHint");
								// Don't do virtual seq properties w/o hint. The code blows up because we can't get a valid
								// destination class, and we can't figure out what data is needed to compute the
								// virtual anyway.
								if (String.IsNullOrEmpty(hint))
									break;
								// Sample hint: "CmPicture:LexSense.Pictures;LexSense.Senses/LexSense.Pictures"
								// That is, an (optional) indication of the class of objects found in the virtual property,
								// followed by colon, followed by a semi-colon separated list of paths to load.
								string[] colonSplit = hint.Split(':');
								if (colonSplit.Length > 2)
									throw new Exception("loadHint must not have more than one colon");
								string pathCollection = colonSplit[0];
								string destClass = null;
								if (colonSplit.Length == 2)
								{
									destClass = colonSplit[0];
									pathCollection = colonSplit[1];
								}
								// Do the 'what to load' part first so those items will come first in the load.
								string[] paths = pathCollection.Split(';');
								IFwMetaDataCache mdc = m_cache.MetaDataCacheAccessor;
								foreach (string path in paths)
								{
									string[] items = path.Split('/');
									NeededPropertyInfo curInfo = info;
									foreach (string item in items)
									{
										string[] classField = item.Split('.');
										if (classField.Length != 2)
											throw new Exception("items in loadHint must have Class.FieldName. Found "
												+ item + " in " + frag.OuterXml);
										string className = classField[0].Trim();
										string fieldName = classField[1].Trim();
										int flidPathItem = (int)mdc.GetFieldId(className, fieldName, true);
										if (flidPathItem == 0)
											throw new Exception("Field " + item + " not recognized in " + frag.OuterXml);
										int fieldType = mdc.GetFieldType((uint)flidPathItem);
										switch (fieldType)
										{
											default:
												curInfo.AddAtomicField(flidPathItem, 0);
												break;
											case (int) CellarModuleDefns.kcptReferenceCollection:
											case (int)CellarModuleDefns.kcptReferenceSequence:
											case (int)CellarModuleDefns.kcptOwningCollection:
											case (int)CellarModuleDefns.kcptOwningSequence:
												curInfo = curInfo.AddObjField(flidPathItem, true);
												break;
											case (int)CellarModuleDefns.kcptOwningAtom:
											case (int)CellarModuleDefns.kcptReferenceAtom:
												curInfo = curInfo.AddObjField(flidPathItem, false);
												break;
											case (int)CellarModuleDefns.kcptMultiBigString:
											case (int)CellarModuleDefns.kcptMultiString:
											case (int)CellarModuleDefns.kcptMultiBigUnicode:
											case (int)CellarModuleDefns.kcptMultiUnicode:
												foreach (int wsid in LangProject.GetWritingSystems(frag, m_cache))
													info.AddAtomicField(flidPathItem, wsid);
												break;
										}
									}
								} // foreach string in paths
								// Now if we got a class, we can include the virtual property itself and
								// recurse.
								if (destClass != null)
								{
									int dstClass = (int)mdc.GetClassId(destClass.Trim());
									info.AddVirtualObjField(flid, true, dstClass);
								}
							}
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
							int flid = DetermineNeededFlid(frag, info, true);
							if (flid == 0)
								return;
							int fragId = GetSubFragId(frag, caller);
							if (fragId == 0)
								return; // something badly wrong.
							IVwVirtualHandler handler = m_cache.VwCacheDaAccessor.GetVirtualHandlerId(flid);
							if (handler != null)
							{
								// virtual properties generally don't have useful destination class info,
								// so we can't guess what might be needed. In any case, short of extending
								// the whole virtual property mechanism, we have no idea what data to
								// cache ahead of time for virtual props.
								// But there is one important special case used to switch to another view
								// of the SAME object.
								if (handler.FieldName == "Hvo")
								{
									// Since it's a display of self, we can add the info to the SAME info object.
									DisplayCommand dispCommand = m_idToDisplayCommand[fragId];
									dispCommand.DetermineNeededFields(this, fragId, info);
								}
							}
							else if (info.SeqDepth < 3 && info.Depth < 10) // see comments on case seq
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

		private int DetermineNeededFlid(XmlNode frag, NeededPropertyInfo info)
		{
			return DetermineNeededFlid(frag, info, false);
		}

		/// <summary>
		/// This combines some of the logic of GetFlid(hvo, frag) and FdoCache.GetFlid(hvo, class, field).
		/// Where possible it determines a flid that should be preloaded, adds it to info, and
		/// returns it.
		/// </summary>
		/// <param name="frag"></param>
		/// <param name="info"></param>
		/// <param name="fAllowVirtuals"></param>
		/// <returns></returns>
		private int DetermineNeededFlid(XmlNode frag, NeededPropertyInfo info, bool fAllowVirtuals)
		{
			int flid = 0;
			try
			{
				IFwMetaDataCache mdc = m_cache.MetaDataCacheAccessor;
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
					else if (stClassName == null || stClassName.Length == 0)
					{
						int classId = info.TargetClass(this);
						flid = (int)mdc.GetFieldId2((uint)classId, stFieldName, true);
					}
					else
					{
						flid = (int)mdc.GetFieldId(stClassName, stFieldName, true);
					}
					if (flid == 0)
					{
						// try a general purpose field that doesn't get treated as a 'base' class.
						flid = (int)mdc.GetFieldId("CmObject", stFieldName, false);
					}
				}
				else
				{
					flid = Convert.ToInt32(xa.Value, 10);
				}
			}
			catch (Exception) // optimization only, if it fails, it fails.
			{
			}
			// Don't return virtual properties. We can't do this sort of optimization with them
			// because we have no way to know what data they need.
			if (flid != 0 && !fAllowVirtuals && m_cache.VwCacheDaAccessor.GetVirtualHandlerId(flid) != null)
				flid = 0;
			return flid;
		}

		private void NoteDependencies(IVwEnv vwenv, int hvo, int flid)
		{
			List<int> objIds;
			List<int> flids;
			// if it's a virtual property, see if it has any dependencies we should note.
			if (Cache.TryGetDependencies(hvo, flid, out objIds, out flids))
			{
				vwenv.NoteDependency(objIds.ToArray(), flids.ToArray(), objIds.Count);
			}
		}

		/// <summary>
		/// This gives the column we are displaying a chance to override the writing system for its part specification.
		/// </summary>
		/// <returns></returns>
		private int GetWritingSystemForObject(XmlNode frag, int hvo, int flid, int wsDefault)
		{
			if (WsForce == 0)
				return LangProject.GetWritingSystem(frag, m_cache, s_qwsCurrent, hvo, flid, wsDefault);
			return WsForce;
		}

		private void DisplayOtherObjStringAlt(int flid, int ws, IVwEnv vwenv, int hvoTarget)
		{
			int fragId = GetId(new DisplayStringAltCommand(flid, ws), m_idToDisplayCommand, m_displayCommandToId);
			vwenv.AddObj(hvoTarget, this, fragId);
		}

		// Process a 'part ref' node (frag) for the specified object an env.
		private void ProcessPartRef(XmlNode frag, int hvo, IVwEnv vwenv)
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
			string visibility = XmlUtils.GetOptionalAttributeValue(frag, "visibility", "always");
			switch (visibility)
			{
				case "never":
					return;
				case "ifdata":
					TestCollectorEnv envT = new TestCollectorEnv(vwenv, m_sda, hvo);
					envT.NoteEmptyDependencies = true;
					envT.MetaDataCache = m_cache.MetaDataCacheAccessor;
					envT.CacheDa = m_cache.VwCacheDaAccessor;
					ProcessChildren(node, envT, hvo, frag);
					if (!envT.Result)
						//					string[] contents = XmlViewsUtils.StringsFor(m_cache, frag, hvo, m_layouts, null);
						//					if (AreAllKeysEmpty(contents))
						return;
					break;
				// case "always":
				default: // treat anything we don't know as 'always'
					break;
			}
			// If the flag is set for displaying the single MSA before any sense number, and
			// the delayed sense number is null, we've already displayed the single MSA, so quit.
			// See LT-9663 for an explanation.
			bool fSingleGramInfoFirst = XmlUtils.GetOptionalBooleanAttributeValue(frag, "singlegraminfofirst", false);
			if (fSingleGramInfoFirst && m_fIgnoreGramInfoAfterFirst && m_tssDelayedNumber == null)
				return;
			// This groups senses by placing graminfo before the number, and omitting it if the same as the
			// previous sense in the entry.  This isn't yet supported by the UI, but may well be requested in
			// the future.  (See LT-9663.)
			//bool fGramInfoBeforeNumber = XmlUtils.GetOptionalBooleanAttributeValue(frag, "graminfobeforenumber", false);
			//if (fGramInfoBeforeNumber && m_tssDelayedNumber != null)
			//{
			//    if (node.FirstChild != null && node.FirstChild.Name == "obj")
			//    {
			//        int flid = GetFlid(node.FirstChild, hvo);
			//        int hvoValue = m_cache.GetObjProperty(hvo, flid);
			//        if (hvoValue == m_hvoGroupedValue)
			//        {
			//            vwenv.AddString(m_tssDelayedNumber);
			//            m_tssDelayedNumber = null;
			//            return;
			//        }
			//        m_hvoGroupedValue = hvoValue;
			//    }
			//}
			// We are going to display the contents of the part.
			string style = XmlUtils.GetOptionalAttributeValue(frag, "style", null);
			if (vwenv is ConfiguredExport)
				(vwenv as ConfiguredExport).BeginCssClassIfNeeded(frag);
			if (style != null)
			{
				string flowType = XmlUtils.GetOptionalAttributeValue(frag, "flowType", null);
				vwenv.set_StringProperty((int)FwTextPropType.ktptNamedStyle, style);
				switch (flowType)
				{
					default:
						vwenv.OpenSpan();
						InsertLiteralString(frag, vwenv, "before");
						break;
					case "para":
						vwenv.OpenParagraph();
						break;
					case "div":
						vwenv.OpenDiv();
						break;
				}
				ProcessChildren(node, vwenv, hvo, frag);
				switch (flowType)
				{
					default:
						InsertLiteralString(frag, vwenv, "after");
						vwenv.CloseSpan();
						break;
					case "para":
						vwenv.CloseParagraph();
						break;
					case "div":
						vwenv.CloseDiv();
						break;
				}
			}
			else
			{
				// no style, just process the children.
				InsertLiteralString(frag, vwenv, "before");
				ProcessChildren(node, vwenv, hvo, frag);
				InsertLiteralString(frag, vwenv, "after");
			}
			if (vwenv is ConfiguredExport)
				(vwenv as ConfiguredExport).EndCssClassIfNeeded(frag);
			// Display the delayed sense number now that the MSA has been presumably been displayed.
			// See LT-9663 for an explanation.
			if (fSingleGramInfoFirst && m_fIgnoreGramInfoAfterFirst && m_tssDelayedNumber != null)
			{
				vwenv.AddString(m_tssDelayedNumber);
				m_tssDelayedNumber = null;
			}
			// This groups senses by placing graminfo before the number, and omitting it if the same as the
			// previous sense in the entry.  This isn't yet supported by the UI, but may well be requested in
			// the future.  (See LT-9663.)
			//if (fGramInfoBeforeNumber && m_tssDelayedNumber != null)
			//{
			//    vwenv.AddString(m_tssDelayedNumber);
			//    m_tssDelayedNumber = null;
			//}
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

		private void InsertLiteralString(XmlNode frag, IVwEnv vwenv, string attrName)
		{
			// When showasindentedpara is set true, the before string is displayed elsewhere, and the after
			// string is not displayed at all.  (Those are the only literal strings that use this method.)
			bool fShowAsParagraphs = XmlUtils.GetOptionalBooleanAttributeValue(frag, "showasindentedpara", false);
			if (fShowAsParagraphs)
				return;
			string item = XmlUtils.GetLocalizedAttributeValue(m_stringTable, frag, attrName, null);
			if (item == null || item.Length == 0)
				return;
			ITsString tss = m_cache.MakeUserTss(item);
			vwenv.AddString(tss);
		}

		internal void AddStringFromOtherObj(XmlNode frag, int hvoTarget, IVwEnv vwenv)
		{
			CheckDisposed();

			int flid = GetFlid(frag, hvoTarget);
			FieldType itype = m_cache.GetFieldType(flid);
			if ((itype == FieldType.kcptUnicode) ||
				(itype == FieldType.kcptBigUnicode))
			{
				int fragId = GetId(new DisplayUnicodeCommand(flid, m_cache.DefaultUserWs), m_idToDisplayCommand, m_displayCommandToId);
				vwenv.AddObj(hvoTarget, this, fragId);
			}
			else if ((itype == FieldType.kcptString) ||
				(itype == FieldType.kcptBigString))
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
						wsid = s_qwsCurrent.WritingSystem;
					}
				}
				if (wsid == 0)
					wsid = LangProject.GetWritingSystem(frag, m_cache, null, LangProject.kwsAnal);
				DisplayOtherObjStringAlt(flid, wsid, vwenv, hvoTarget);
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
			CheckDisposed();

			if (!keyToId.ContainsKey(key))
			{
				int id = m_nextID++;
				idToKey[id] = key;
				keyToId[key] = id;
			}
			return keyToId[key];
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a vector to the display.
		/// fragId: a fragCaller id
		/// Set up a call to AddObjVec or AddObjVecItems as appropriate.
		/// This depends on whether the target fragment has delimiter info.
		/// </summary>
		/// <param name="frag">The frag.</param>
		/// <param name="vwenv">The vwenv.</param>
		/// <param name="flid">The flid.</param>
		/// <param name="fragId">The frag id.</param>
		/// <param name="caller">The caller.</param>
		/// ------------------------------------------------------------------------------------
		private void AddObjectVector(XmlNode frag, IVwEnv vwenv, int flid, int fragId, XmlNode caller)
		{
			XmlNode nodeSeqStuff;
			XmlNode specialAttrsStuff;
			// seq stuff on the frag node
			nodeSeqStuff = specialAttrsStuff = frag;
			if (XmlUtils.GetOptionalBooleanAttributeValue(nodeSeqStuff, "inheritSeps", false))
				nodeSeqStuff = caller;
			XmlAttribute xaSep = nodeSeqStuff.Attributes["sep"];
			XmlAttribute xaNum = nodeSeqStuff.Attributes["number"];
			// Note that we deliberately don't use nodeSeqStuff here. "excludeHvo" is not a separator property,
			// nor configurable, so it belongs on the 'seq' element, not the part ref.
			string exclude = XmlUtils.GetOptionalAttributeValue(specialAttrsStuff, "excludeHvo", null);
			bool fFirstOnly = XmlUtils.GetOptionalBooleanAttributeValue(specialAttrsStuff, "firstOnly", false);
			string sort = XmlUtils.GetOptionalAttributeValue(specialAttrsStuff, "sort", null);
			bool fNumber = xaNum != null && xaNum.Value != null && xaNum.Value != "";
			bool fSep = xaSep != null && xaSep.Value != null && xaSep.Value != "";
			if (fNumber || fSep || exclude != null || fFirstOnly || sort != null)
			{
				// This results in DisplayVec being called.
				vwenv.AddObjVec(flid, this, fragId);
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This version allows the IVwEnv to be optional
		/// </summary>
		/// <param name="frag">the 'if' node</param>
		/// <param name="hvo">The hvo.</param>
		/// <param name="cache">The cache.</param>
		/// <param name="caller">The caller.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static bool ConditionPasses(XmlNode frag, int hvo, FdoCache cache, XmlNode caller)
		{
			return ConditionPasses(null, frag, hvo, cache, caller);
		}

		/// <summary>
		/// This version allows the IVwEnv and caller to be omitted
		/// </summary>
		/// <param name="frag">the 'if' node</param>
		/// <param name="hvo"></param>
		/// <param name="cache"></param>
		public static bool ConditionPasses(XmlNode frag, int hvo, FdoCache cache)
		{
			return ConditionPasses(null, frag, hvo, cache, null);
		}

		/// <summary>
		/// This method looks for the target attribute of an if or where element and if
		/// found switches the hvo value to that of it's owner if it's value is owner.
		/// </summary>
		/// <param name="frag">xml node</param>
		/// <param name="hvo">object hvo</param>
		/// <param name="cache"></param>
		public static void GetActualTarget(XmlNode frag, ref int hvo, FdoCache cache)
		{
			string defaultVal = "this";
			string target = XmlUtils.GetOptionalAttributeValue(frag, "target", defaultVal).ToLower();
			switch (target)
			{
				case "owner":
					hvo = cache.GetOwnerOfObject(hvo);
					Debug.Assert(hvo != 0, "object must have an owner", "Error in XML where target=owner and the object has no owner");
					break;
				default:
					break;
			}
		}

		/// <summary>
		/// Evaluate a condition expressed in the frag node.
		/// May be is="classname", requires hvo to be that class or a subclass,
		///	unless excludesubclasses="true" in which case must match exactly.
		///	May be field="name" plus any of lengthatleast="n", lengthatmost="n",
		///		stringequals="xx", stringaltequals="xx", boolequals="0/-1", intequals="n",
		///		intgreaterthan="n", or intlessthan="n".
		///	More than one condition may be present, if so, all must pass.
		///	Class of object condition is tested first if present, so property may be a property
		///	possessed only by that class of object.
		/// </summary>
		/// <param name="vwenv">Environment in which evaluated, for NoteDependency. May be null.</param>
		/// <param name="frag"></param>
		/// <param name="hvo"></param>
		/// <param name="cache"></param>
		/// <param name="caller">the 'part ref' node that invoked the current part. May be null if XML does not use it.</param>
		/// <returns></returns>
		public static bool ConditionPasses(IVwEnv vwenv, XmlNode frag, int hvo, FdoCache cache, XmlNode caller)
		{
			IFwMetaDataCache mdc = cache.MetaDataCacheAccessor;

			GetActualTarget(frag, ref hvo, cache);	// modify the hvo if needed

			if (!IsConditionsPass(frag, cache, hvo, mdc))
				return false;
			if (!LengthConditionsPass(vwenv, frag, hvo, cache, mdc))
				return false;
			if (!ValueEqualityConditionsPass(vwenv, frag, hvo, cache, mdc, caller))
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
				bool fBidi = XmlUtils.GetBooleanAttributeValue(sBidi);
				bool fRTLVern = cache.GetBoolProperty(cache.DefaultVernWs,
					(int)LgWritingSystem.LgWritingSystemTags.kflidRightToLeft);
				bool fRTLAnal = cache.GetBoolProperty(cache.DefaultAnalWs,
					(int)LgWritingSystem.LgWritingSystemTags.kflidRightToLeft);
				if (fRTLVern == fRTLAnal)
					return !fBidi;
				else
					return fBidi;
			}
			return true;
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check for ValueEquality conditions
		/// </summary>
		/// <param name="vwenv">The vwenv.</param>
		/// <param name="frag">The frag.</param>
		/// <param name="hvo">The hvo.</param>
		/// <param name="cache">The cache.</param>
		/// <param name="mdc">The MDC.</param>
		/// <param name="caller">The caller.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		static private bool ValueEqualityConditionsPass(IVwEnv vwenv, XmlNode frag, int hvo, FdoCache cache,
			IFwMetaDataCache mdc, XmlNode caller)
		{
			if (!StringEqualsConditionPasses(vwenv, frag, hvo, cache))
				return false;
			if (!StringAltEqualsConditionPasses(vwenv, frag, hvo, cache, caller))
				return false;
			if (!BoolEqualsConditionPasses(vwenv, frag, hvo, cache))
				return false;
			if (!IntEqualsConditionPasses(vwenv, frag, hvo, cache))
				return false;
			if (!IntGreaterConditionPasses(vwenv, frag, hvo, cache))
				return false;
			if (!IntLessConditionPasses(vwenv, frag, hvo, cache))
				return false;
			if (!IntMemberOfConditionPasses(vwenv, frag, hvo, cache))
				return false;
			if (!GuidEqualsConditionPasses(vwenv, frag, hvo, cache))
				return false;
			if (!HvoEqualsConditionPasses(vwenv, frag, hvo, cache))
				return false;
			if (!FlidEqualsConditionPasses(vwenv, frag, hvo, cache))
				return false;
			return true;
		}

		static private void NoteDependency(IVwEnv vwenv, int hvo, int flid)
		{
			if (vwenv == null)
				return;
			vwenv.NoteDependency(new int[] { hvo }, new int[] { flid }, 1);
		}

		static private int GetValueFromCache(IVwEnv vwenv, XmlNode frag, int hvo, FdoCache cache)
		{
			int flid = GetFlidAndHvo(vwenv, frag, ref hvo, cache);
			if (flid == -1 || hvo == 0)
				return 0; // This is rather arbitrary...objects missing, what should each test do?
			NoteDependency(vwenv, hvo, flid);
			return cache.GetIntProperty(hvo, flid);
		}

		/// <summary>
		/// Check for "intequals" attribute condition
		/// </summary>
		/// <param name="vwenv">For NoteDependency</param>
		/// <param name="frag"></param>
		/// <param name="hvo"></param>
		/// <param name="cache"></param>
		/// <returns></returns>
		static private bool IntEqualsConditionPasses(IVwEnv vwenv, XmlNode frag, int hvo, FdoCache cache)
		{
			int intValue = XmlUtils.GetOptionalIntegerValue(frag, "intequals", -2);	// -2 might be valid
			int intValue2 = XmlUtils.GetOptionalIntegerValue(frag, "intequals", -3);	// -3 might be valid
			if (intValue != -2 || intValue2 != -3)
			{
				int value = GetValueFromCache(vwenv, frag, hvo, cache);
				if (value != intValue)
					return false;
			}
			return true;
		}

		/// <summary>
		/// Check for "intgreaterthan" attribute condition
		/// </summary>
		/// <param name="vwenv">For NoteDependency</param>
		/// <param name="frag"></param>
		/// <param name="hvo"></param>
		/// <param name="cache"></param>
		/// <returns></returns>
		static private bool IntGreaterConditionPasses(IVwEnv vwenv, XmlNode frag, int hvo, FdoCache cache)
		{
			int intValue = XmlUtils.GetOptionalIntegerValue(frag, "intgreaterthan", -2);	// -2 might be valid
			int intValue2 = XmlUtils.GetOptionalIntegerValue(frag, "intgreaterthan", -3);	// -3 might be valid
			if (intValue != -2 || intValue2 != -3)
			{
				int value = GetValueFromCache(vwenv, frag, hvo, cache);
				if (value <= intValue)
					return false;
			}
			return true;
		}

		/// <summary>
		/// Check for "intlessthan" attribute condition
		/// </summary>
		/// <param name="vwenv">For NoteDependency</param>
		/// <param name="frag"></param>
		/// <param name="hvo"></param>
		/// <param name="cache"></param>
		/// <returns></returns>
		static private bool IntLessConditionPasses(IVwEnv vwenv, XmlNode frag, int hvo, FdoCache cache)
		{
			int intValue = XmlUtils.GetOptionalIntegerValue(frag, "intlessthan", -2);	// -2 might be valid
			int intValue2 = XmlUtils.GetOptionalIntegerValue(frag, "intlessthan", -3);	// -3 might be valid
			if (intValue != -2 || intValue2 != -3)
			{
				int value = GetValueFromCache(vwenv, frag, hvo, cache);
				if (value >= intValue)
					return false;
			}
			return true;
		}

		/// <summary>
		/// Check for "intmemberof" attribute condition
		/// </summary>
		/// <param name="vwenv">For NoteDependency</param>
		/// <param name="frag"></param>
		/// <param name="hvo"></param>
		/// <param name="cache"></param>
		/// <returns></returns>
		static private bool IntMemberOfConditionPasses(IVwEnv vwenv, XmlNode frag, int hvo, FdoCache cache)
		{
			string stringValue = XmlUtils.GetOptionalAttributeValue(frag, "intmemberof");
			if (stringValue == null)
				return true;
			string[] rgsNum = stringValue.Split(',');
			int val = GetValueFromCache(vwenv, frag, hvo, cache);
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
		/// <param name="vwenv"></param>
		/// <param name="frag"></param>
		/// <param name="hvo"></param>
		/// <param name="cache"></param>
		/// <returns></returns>
		static private bool FlidEqualsConditionPasses(IVwEnv vwenv, XmlNode frag, int hvo, FdoCache cache)
		{
			string sFlid = XmlUtils.GetOptionalAttributeValue(frag, "flidequals");
			if (sFlid == null)
				return true;
			int flidVal = 0;
			if (Int32.TryParse(sFlid, out flidVal))
			{
				string fieldName = XmlUtils.GetManditoryAttributeValue(frag, "field");
				int flid = GetFlid(frag, hvo, cache);
				if (cache.MetaDataCacheAccessor.get_IsVirtual((uint)flid))
				{
					// currently don't handle virtual flids.
					throw new ArgumentException(String.Format("flidequals condition does not currently support virtual field attributes ({0})", fieldName));
				}
				return flid == flidVal;
			}
			return false;
		}
		/// <summary>
		/// Check for "hvoequals" attribute condition
		/// </summary>
		/// <param name="vwenv">For NoteDependency</param>
		/// <param name="frag"></param>
		/// <param name="hvo"></param>
		/// <param name="cache"></param>
		/// <returns></returns>
		static private bool HvoEqualsConditionPasses(IVwEnv vwenv, XmlNode frag, int hvo, FdoCache cache)
		{
			string sHvo = XmlUtils.GetOptionalAttributeValue(frag, "hvoequals");
			if (sHvo == null)
				return true;
			int val;
			string sIndex = XmlUtils.GetOptionalAttributeValue(frag, "index");
			if (sIndex == null)
			{
				int flid = GetFlid(frag, hvo, cache);
				val = cache.MainCacheAccessor.get_ObjectProp(hvo, flid);
			}
			else
			{
				int index;
				try
				{
					index = Convert.ToInt32(sIndex, 10);
					int flid = GetFlid(frag, hvo, cache);
					val = cache.MainCacheAccessor.get_VecItem(hvo, flid, index);
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
		/// <param name="vwenv"></param>
		/// <param name="frag"></param>
		/// <param name="hvo"></param>
		/// <param name="cache"></param>
		/// <returns></returns>
		static private bool GuidEqualsConditionPasses(IVwEnv vwenv, XmlNode frag, int hvo, FdoCache cache)
		{
			XmlAttribute xa = frag.Attributes["guidequals"];
			if (xa == null)
				return true;

			int flid = GetFlidAndHvo(vwenv, frag, ref hvo, cache);
			if (flid == -1 || hvo == 0)
				return false; // object is not there, can't have expected value of property.
			NoteDependency(vwenv, hvo, flid);
			int hvoObj = cache.GetObjProperty(hvo, flid);
			Guid guid = new Guid(xa.Value);
			Guid guidObj = Guid.Empty;
			if (hvoObj != 0)
				guidObj = cache.GetGuidFromId(hvoObj);

			return guid == guidObj;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check for "boolequals" attribute condition
		/// </summary>
		/// <param name="vwenv">The vwenv.</param>
		/// <param name="frag">The frag.</param>
		/// <param name="hvo">The hvo.</param>
		/// <param name="cache">The cache.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		static private bool BoolEqualsConditionPasses(IVwEnv vwenv, XmlNode frag, int hvo, FdoCache cache)
		{
			int boolValue = XmlUtils.GetOptionalIntegerValue(frag, "boolequals", 2);	// must be either 0 or 1.
			if (boolValue != 2)
			{
				int value = GetValueFromCache(vwenv, frag, hvo, cache);
				// it's possible that 'true' values from the Cache can be -1.
				if (Math.Abs(value) != boolValue)
					return false;
			}
			return true;
		}

		static private bool StringAltEquals(string sValue, int hvo, int flid, int ws,
			FdoCache cache)
		{
			string sAlt =
				cache.MainCacheAccessor.get_MultiStringAlt(hvo, flid, ws).Text;
			if (sAlt == null && sValue == "")
				return true;
			else
				return (sAlt == sValue);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check for "stringaltequals" attribute condition
		/// </summary>
		/// <param name="vwenv">The vwenv.</param>
		/// <param name="frag">The frag.</param>
		/// <param name="hvo">The hvo.</param>
		/// <param name="cache">The cache.</param>
		/// <param name="caller">The caller.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		static private bool StringAltEqualsConditionPasses(IVwEnv vwenv, XmlNode frag, int hvo,
			FdoCache cache, XmlNode caller)
		{
			string stringAltValue = XmlUtils.GetOptionalAttributeValue(frag, "stringaltequals");
			if (stringAltValue != null)
			{
				// Bad idea, since a ws attr with "all analysis" will return -1,
				// which is a magic ws value, but not the right one for "all analysis".
				// What's even worse than the bogus -1 value, is that a new writing system will be created
				// with an icu locale of "all analysis". Not good.
				// Well, it would have created it, but I fixed that bug, also.
				//int ws = GetWritingSystem(frag, cache);
				int flid = GetFlidAndHvo(vwenv, frag, ref hvo, cache);
				if (flid == -1 || hvo == 0)
					return false; // object is not there, can't have expected value of property.
				string wsId = XmlUtils.GetOptionalAttributeValue(frag, "ws");
				// Note: the check for s_qwsCurrent not null here is a desperation move. It prevents
				// a crash if "current" is misused in a context where the thing using "current"
				// can be regenerated without regenerating the thing that has the loop.
				if (wsId == "current" && s_qwsCurrent != null)
				{
					NoteStringValDependency(vwenv, hvo, flid, s_qwsCurrent.WritingSystem, stringAltValue);
					if (!StringAltEquals(stringAltValue, hvo, flid, s_qwsCurrent.WritingSystem, cache))
						return false;
				}
				else
				{
					// If ws is 'configure' then we must have a caller to inherit the configured value from.
					if (wsId == "configure")
					{
						wsId = XmlUtils.GetManditoryAttributeValue(caller, "ws");
					}
					foreach (int ws in LangProject.GetAllWritingSystems(wsId, cache, null, hvo, flid))
					{
						NoteStringValDependency(vwenv, hvo, flid, ws, stringAltValue);
						if (!StringAltEquals(stringAltValue, hvo, flid, ws, cache))
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
		/// <param name="frag"></param>
		/// <param name="hvo"></param>
		/// <param name="cache"></param>
		/// <returns></returns>
		static private bool StringEqualsConditionPasses(IVwEnv vwenv, XmlNode frag, int hvo, FdoCache cache)
		{
			string stringValue = XmlUtils.GetOptionalAttributeValue(frag, "stringequals");
			if (stringValue != null)
			{
				int flid = GetFlidAndHvo(vwenv, frag, ref hvo, cache);
				string value = null;
				if (flid != -1 && hvo != 0)
				{
					ITsString tsString = cache.GetTsStringProperty(hvo, flid);
					int var;
					int realWs = tsString.get_Properties(0).GetIntPropValues((int) FwTextPropType.ktptWs, out var);
					ITsStrFactory tsf = TsStrFactoryClass.Create();
					vwenv.NoteStringValDependency(hvo, flid, realWs, tsf.MakeString(value, realWs));
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
		/// <param name="frag"></param>
		/// <param name="hvo"></param>
		/// <param name="cache"></param>
		/// <param name="mdc"></param>
		/// <returns></returns>
		static private bool LengthConditionsPass(IVwEnv vwenv, XmlNode frag, int hvo, FdoCache cache,
			IFwMetaDataCache mdc)
		{
			// Class of object condition passes, if any.
			int maxlen = XmlUtils.GetOptionalIntegerValue(frag, "lengthatmost", -1);
			int minlen = XmlUtils.GetOptionalIntegerValue(frag, "lengthatleast", -1);
			if (maxlen >= 0 || minlen >= 0)
			{
				int len = 0;
				if (maxlen == -1)
					maxlen = 0x7FFFFFFF; // Int32.MaxVal doesn't compile; // effectively no max.
				int flid = GetFlid(frag, hvo, cache);
				NoteDependency(vwenv, hvo, flid);
				int fldType = mdc.GetFieldType((uint)flid);
				if (fldType == (int)FieldType.kcptOwningSequence
					|| fldType == (int)FieldType.kcptReferenceCollection
					|| fldType == (int)FieldType.kcptOwningCollection
					|| fldType == (int)FieldType.kcptReferenceSequence)
				{
					len = cache.GetVectorSize(hvo, flid);
				}
				else if (fldType == (int)FieldType.kcptOwningAtom
					|| fldType == (int)FieldType.kcptReferenceAtom)
				{
					int hvoItem = cache.GetObjProperty(hvo, flid);
					len = hvoItem == 0 ? 0 : 1;
				}
				// a virtual flid; a negative-valued flid indicates that it's a virtual.
				else if ((fldType == 0) && (flid < 0))
				{
					len = cache.GetVectorSize(hvo, flid);
				}

				if (len > maxlen || len < minlen)
					return false;
			}
			return true;
		}

		/// <summary>
		/// Check for an "is" attribute condition
		/// </summary>
		/// <param name="frag"></param>
		/// <param name="cache"></param>
		/// <param name="hvo"></param>
		/// <param name="mdc"></param>
		/// <returns></returns>
		static private bool IsConditionPasses(XmlNode frag, FdoCache cache, int hvo, IFwMetaDataCache mdc)
		{
			string className = XmlUtils.GetOptionalAttributeValue(frag, "is");
			if (className != null)
			{
				// 'is' condition present, evaluate it.
				uint uclsidObj = (uint)cache.GetClassOfObject(hvo);
				uint uclsidArg = mdc.GetClassId(className);
				if (uclsidObj != uclsidArg)
				{
					// Not an exact match, if excluding subclasses, condition fails.
					if (XmlUtils.GetOptionalBooleanAttributeValue(frag, "excludesubclasses", false))
						return false;
					// Otherwise OK if clsidObj is a subclass of clsidArg
					uint uclsid = mdc.GetBaseClsId(uclsidObj);
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
		/// <param name="frag"></param>
		/// <param name="cache"></param>
		/// <param name="hvo"></param>
		/// <param name="mdc"></param>
		/// <returns></returns>
		static private bool IsConditionsPass(XmlNode frag, FdoCache cache, int hvo, IFwMetaDataCache mdc)
		{
			if (!IsConditionPasses(frag, cache, hvo, mdc))
				return false;
			else if (!AtLeastOneIsConditionPasses(frag, cache, hvo, mdc))
				return false;
			return true;
		}

		/// <summary>
		/// Check for "atleastoneis" attribute condition. This requires a field attribute that translates to a collection
		/// or sequence property.
		/// </summary>
		/// <param name="frag"></param>
		/// <param name="cache"></param>
		/// <param name="hvo"></param>
		/// <param name="mdc"></param>
		/// <returns></returns>
		static private bool AtLeastOneIsConditionPasses(XmlNode frag, FdoCache cache, int hvo, IFwMetaDataCache mdc)
		{
			string className = XmlUtils.GetOptionalAttributeValue(frag, "atleastoneis");
			if (className != null)
			{
				int flid = GetFlid(frag, hvo, cache);
				int cobj = cache.GetVectorSize(hvo, flid);
				uint uclsidArg = mdc.GetClassId(className);
				bool fExcludeSubClass = XmlUtils.GetOptionalBooleanAttributeValue(frag, "excludesubclasses", false);
				for (int iobj = 0; iobj < cobj; ++iobj)
				{
					int hobj = cache.GetVectorItem(hvo, flid, iobj);
					uint uclsidObj = (uint)cache.GetClassOfObject(hobj);
					if (uclsidArg == uclsidObj)
						return true;
					if (!fExcludeSubClass)
					{
						uint uclsid = mdc.GetBaseClsId(uclsidObj);
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
			return VwRule.kvrlNone;		// or Assert or throw exception?
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
		public virtual FdoCache Cache
		{
			get
			{
				CheckDisposed();

				return m_cache;
			}
			set
			{
				CheckDisposed();

				if (m_cache == value)
					return;

				m_cache = value;
				m_IsObjectSelectedTag = ObjectSelectedVirtualHandler.InstallHandler(m_cache.VwCacheDaAccessor).Tag;
				m_mdc = m_cache.MetaDataCacheAccessor;
				if (m_layouts != null)
					m_layouts.Dispose();
				m_layouts = new LayoutCache(m_mdc, m_fLoadFlexLayouts, m_cache.DatabaseName);
				this.LangProjectHvo = m_cache.LangProject.Hvo;
				m_sda = m_cache.MainCacheAccessor;
			}
		}

		/// <summary>
		/// Set the SilDataAccess used within the VC. Setting the Cache property also
		/// sets this, so to get a different result you must call this after setting the Cache.
		/// </summary>
		public ISilDataAccess DataAccess
		{
			get { return m_sda;}
			set { m_sda = value;}
		}

		bool m_fLoadFlexLayouts = false;
		/// <summary>
		/// Cause customized LanguageExplorer layouts to be loaded even for other programs.
		/// </summary>
		public bool LoadFlexLayouts
		{
			set { m_fLoadFlexLayouts = value; }
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
				m_StringsFromListNode.Clear();
			}
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
				ICmObject obj = CmObject.CreateFromDBObject(m_cache, hvo);
				Debug.Assert(null != obj);
				string property = xa.Value.Substring(1);//skip the %
				Debug.Assert(property.Length > 0);

				//HACK for a proof of concept.
				//TODO: make  general using reflection
				// Note: if making general, consider supporting a way to hook up notification if underlying properties
				// change. Note that in the handler for <table> there is a also a special case for %ColumnCount.
				Debug.Assert(property == "ColumnCount", "Sorry, only 'ColumnCount' on affix templates are supported at this point.");
				Debug.Assert(obj is SIL.FieldWorks.FDO.Ling.MoInflAffixTemplate);
				SIL.FieldWorks.FDO.Ling.MoInflAffixTemplate template = (SIL.FieldWorks.FDO.Ling.MoInflAffixTemplate)obj;
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
				flid = GetFlid(frag, hvo, m_cache);
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
			return GetFlid(frag, hvo, m_cache);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the flid.
		/// </summary>
		/// <param name="frag">The frag.</param>
		/// <param name="hvo">The hvo.</param>
		/// <param name="cache">The cache.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static int GetFlid(XmlNode frag, int hvo, FdoCache cache)
		{
			int flid = 0;
			try
			{
				IFwMetaDataCache mdc = cache.MetaDataCacheAccessor;
				XmlAttribute xa = frag.Attributes["flid"];
				if (xa == null)
				{
					if (mdc == null)
						return 0; // can't do anything else sensible.
					// JohnT: try class, field props and look up in MetaDataCache.

					string stClassName = XmlUtils.GetOptionalAttributeValue(frag, "class");
					string stFieldName = XmlUtils.GetOptionalAttributeValue(frag, "field");
					if (stFieldName != null)
					{
						string[] rgstFields = stFieldName.Split(new char[] { '/' });
						if (rgstFields != null && rgstFields.Length > 1)
						{
							// this kind of field attribute is handled in GetFlidAndHvo()
							return 0;
						}
					}
					flid = cache.GetFlid(hvo, stClassName, stFieldName);

				}
				else
				{
					flid = Convert.ToInt32(xa.Value, 10);
				}
			}
			catch (Exception)
			{
				throw new ConfigurationException("There was a problem figuring out the flid for " + hvo, frag);
			}
			if (flid == 0)
				throw new ConfigurationException("There was a problem figuring out the flid for " + hvo
					+ "\nThis is often caused by saved settings from before a new version was installed."
					+ "\nTry starting the program while holding down the Shift key to clear all saved settings", frag);
			return flid;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Try to see if the part field refers to a custom field, whether valid or not.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="node">The node.</param>
		/// <param name="flid">the id of the custom field, 0 if not valid for our database.</param>
		/// <returns>
		/// true, if the node refers to a custom field, false if it does not.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		static public bool TryCustomField(FdoCache cache, XmlNode node, out int flid)
		{
			flid = 0;
			string fieldName = XmlUtils.GetOptionalAttributeValue(node, "field");
			if (!string.IsNullOrEmpty(fieldName) && fieldName.StartsWith("custom"))
			{
				try
				{
					flid = XmlVc.GetFlid(node, 0, cache);
				}
				catch
				{
					//
				}
				return true;
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
		protected static int GetFlidAndHvo(IVwEnv vwenv, XmlNode frag, ref int hvo, FdoCache cache)
		{
			try
			{

				int flid = 0;
				IFwMetaDataCache mdc = cache.MetaDataCacheAccessor;
				XmlAttribute xa = frag.Attributes["flid"];
				if (xa == null)
				{
					if (mdc == null)
						return 0; // can't do anything else sensible.
					// JohnT: try class, field props and look up in MetaDataCache.

					string stClassName = XmlUtils.GetOptionalAttributeValue(frag, "class");
					string stFieldPath = XmlUtils.GetOptionalAttributeValue(frag, "field");
					string[] rgstFields = stFieldPath.Split(new char[] { '/' });

					for (int i = 0; i < rgstFields.Length; i++)
					{
						if (i > 0)
						{
							NoteDependency(vwenv, hvo, flid);
							hvo = cache.GetObjProperty(hvo, flid);
							if (hvo == 0)
								return -1;
						}
						if (stClassName == null || stClassName.Length == 0)
						{
							uint clsid = (uint)cache.GetClassOfObject(hvo);
							flid = (int)mdc.GetFieldId2(clsid, rgstFields[i], true);
						}
						else
						{
							flid = (int)mdc.GetFieldId(stClassName, rgstFields[i], true);
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
			catch (Exception)
			{
				throw new ConfigurationException("There was a problem figuring out the flid for " + hvo, frag);
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
					return layoutName;
			}
			return XmlUtils.GetOptionalAttributeValue(frag, "layout", "default");
		}

		// New approach:
		// The ID identifies both the frag and the caller as a pair in m_idToDisplayInfo
		// Note: parallel logic in XmlViewsUtils.GetNodeForRelatedObject must be kept in sync.
		// (Ideas on how to refactor to a single place welcome!!)
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the sub frag id.
		/// </summary>
		/// <param name="frag">The frag.</param>
		/// <param name="caller">The caller.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected int GetSubFragId(XmlNode frag, XmlNode caller)
		{
			// New approach: generate an ID that identifies Pair(frag, caller) in m_idToDisplayInfo
			return GetId(new MainCallerDisplayCommand(frag, caller, false), m_idToDisplayCommand, m_displayCommandToId);
		}

		// Note: I (RandyR) think this was only for the old system.
		// It returned null, or tried to get the node form the old proeprty.
		// The problem with the old property is it was always null.
		// Since it never caused a null object exception, the mehtod must not have been getitng
		// to that point, which left it with always returning null.
		// Therefore, I replaced the original last line with the exception.
		internal XmlNode GetSubFrag(XmlNode callingFrag)
		{
			CheckDisposed();

			int fragId = GetSubFragId(callingFrag, null);
			if (fragId == 0)
				return null; // something badly wrong.
			throw new InvalidOperationException("The old m_fragments member is gone. Use New XML config file mechanisms.");
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
			CheckDisposed();

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
						(vwenv as ConfiguredExport).BeginCssClassIfNeeded(frag);
				}
			}

			foreach (XmlNode node in frag.ChildNodes)
			{
				ProcessFrag(node, vwenv, hvo, true, caller);
			}
			if (!String.IsNullOrEmpty(css))
				(vwenv as ConfiguredExport).EndCssClassIfNeeded(frag);
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
			CheckDisposed();

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
			CheckDisposed();

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
						vwenv.set_StringProperty((int)FwTextPropType.ktptFontFamily,
							GetPropVal(node));
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
				default:
					Debug.Assert(false, "Expected value single, none, double, dotted, dashed, or squiggle");
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
			vc.ProcessChildren(node, vwenv, hvo);
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
		/// Initializes a new instance of the <see cref="T:NodeDisplayCommand"/> class.
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
			StreamWriter logStream = vc.LogStream;
			if (logStream != null)
			{
				int logIndent = vc.LogIndent;
				for (int i = 0; i < vc.LogIndent; i++)
					logStream.Write("    ");
				logStream.WriteLine("Display " + hvo + " using " + m_node.OuterXml);
				vc.LogIndent = logIndent + 1;
			}

			vc.ProcessFrag(m_node, vwenv, hvo, true, null);

			if (vc.LogStream != null)
				vc.LogIndent--;
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
		/// Initializes a new instance of the <see cref="T:NodeChildrenDisplayCommand"/> class.
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

		internal MainCallerDisplayCommand(XmlNode mainNode, XmlNode caller, bool fUserMainAsFrag)
		{
			m_mainNode = mainNode;
			m_caller = caller;
			m_fUseMainAsFrag = fUserMainAsFrag;
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
				&& other.m_fUseMainAsFrag == m_fUseMainAsFrag;
		}

		// Make it work sensibly as a hash key.
		public override int GetHashCode()
		{
			return m_mainNode.GetHashCode()
				+ (m_fUseMainAsFrag ? 1 : 0)
				+ (m_caller == null ? 0 : m_caller.GetHashCode());
		}

		internal override void PerformDisplay(XmlVc vc, int fragId, int hvo, IVwEnv vwenv)
		{
			string layoutName;
			XmlNode node = GetNodeForChild(out layoutName, fragId, vc, hvo);

			StreamWriter logStream = vc.LogStream;
			if (logStream != null)
			{
				int logIndent = vc.LogIndent;
				for (int i = 0; i < vc.LogIndent; i++)
					logStream.Write("    ");
				logStream.WriteLine("Display " + hvo + " using layout " + layoutName + " which found " + node.OuterXml);
				vc.LogIndent = logIndent + 1;
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
			}
			if (flowType != null)
			{
				// Surround the processChildren call with an appropriate flow object, and
				// if requested apply a style to it.
				if (style != null)
					vwenv.set_StringProperty((int)FwTextPropType.ktptNamedStyle, style);
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
				}
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
				}
			}
			else
			{
				// no flow/style specified
				ProcessChildren(fragId, vc, vwenv, node, hvo);
			}
			if (vc.LogStream != null)
				vc.LogIndent--;
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
			node = XmlVc.GetNodeForChild(node, callingFrag, vc.m_layouts);
			return node;
		}

		/// <summary>
		/// Almost the same as GetNodeForChild, but depends on knowing the class of child
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
			node = XmlVc.GetNodeForChild(node, callingFrag, vc.m_layouts);
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
		public ReadOnlyConditionalRootDisplayCommand(string rootLayoutName, SimpleRootSite rootSite, XmlNode condition)
			: base(rootLayoutName, rootSite)
		{
			m_condition = condition;
			Debug.Assert(rootSite is RootSite, "conditional display requires real rootsite with cache");
		}
		internal override void PerformDisplay(XmlVc vc, int fragId, int hvo, IVwEnv vwenv)
		{
			if (XmlVc.ConditionPasses(m_condition, hvo, (m_rootSite as RootSite).Cache))
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
		public DisplayStringAltCommand(int tag, int ws)
		{
			m_tag = tag;
			m_ws = ws;
		}

		internal override void PerformDisplay(XmlVc vc, int fragId, int hvo, IVwEnv vwenv)
		{
			vwenv.AddStringAltMember(m_tag, m_ws, vc);
		}

		public override bool Equals(object obj)
		{
			DisplayStringAltCommand other = obj as DisplayStringAltCommand;
			if (other == null)
				return false;
			return other.m_tag == m_tag && other.m_ws == m_ws;
		}

		public override int GetHashCode()
		{
			return m_tag + m_ws;
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

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class PropWs
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:PropWs"/> class.
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
	/// This class stores (a) a list of flids of atomic non-object properties that we need
	/// to know about;
	/// (b) a list of object properties, for each of which, we need to know the
	/// contents of the property, and then we need to recursively load info about
	/// the objects in that property. This includes both atomic object properties, where a single
	/// query can be used for both the main and related objects, and sequence properties where
	/// a distinct query is used for each sequence.
	/// We reuse this class for info about the child objects, so it also stores
	/// a flid for obtaining child objects. This is also set for the root
	/// NeededPropertyInfo, since sometimes knowing what to add depends on
	/// the signature of the sequence property.
	/// </summary>
	public class NeededPropertyInfo
	{
		/// <summary>
		/// the class of objects at this property info level (destination of m_flidSource).
		/// </summary>
		protected uint m_targetClass = 0;
		// the property from which the objects whose properties we want come.
		int m_flidSource;
		List<PropWs> m_atomicFlids = new List<PropWs>(); // atomic properties of target objects
		List<NeededPropertyInfo> m_sequenceInfo = new List<NeededPropertyInfo>();
		NeededPropertyInfo m_parent; // if it is in a list of child properties, note of which object.
		bool m_fSeq; // whether m_flidSource is a sequence property.

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:NeededPropertyInfo"/> class.
		/// </summary>
		/// <param name="listItemsClass">the class of objects at the root parent of the NeedPropertyInfo tree.
		/// Typically the destination class of flidSource</param>
		/// ------------------------------------------------------------------------------------
		public NeededPropertyInfo(uint listItemsClass)
		{
			m_targetClass = listItemsClass;
			m_flidSource = 0;	// don't really how we got to the root parent class.
			m_parent = null;
			m_fSeq = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:NeededPropertyInfo"/> class.
		/// </summary>
		/// <param name="flidSource">The flid source.</param>
		/// <param name="parent">The parent.</param>
		/// <param name="fSeq">if set to <c>true</c> [f seq].</param>
		/// ------------------------------------------------------------------------------------
		protected NeededPropertyInfo(int flidSource, NeededPropertyInfo parent, bool fSeq)
		{
			m_flidSource = flidSource;
			m_parent = parent;
			m_fSeq = fSeq;
		}

		/// <summary>
		/// The source property containing the objects about which we want info.
		/// </summary>
		public int Source
		{
			get { return m_flidSource; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance has atomic fields.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance has atomic fields; otherwise, <c>false</c>.
		/// </value>
		/// ------------------------------------------------------------------------------------
		public bool HasAtomicFields
		{
			get
			{
				if (m_atomicFlids.Count > 0)
					return true;
				foreach (NeededPropertyInfo info in m_sequenceInfo)
					if (!info.m_fSeq)
						return true;
				return false;
			}
		}

		/// <summary>
		/// Answer the class of objects for which we are collecting fields.
		/// By default this is the destination class of the field that contains them.
		/// We override this in a subclass for certain virtual and phony properties.
		/// </summary>
		/// <param name="vc"></param>
		/// <returns></returns>
		internal int TargetClass(XmlVc vc)
		{
			return TargetClass(vc.Cache);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Answer the class of objects for which we are collecting fields.
		/// By default this is the destination class of the field that contains them.
		/// If needed, override this in a subclass for certain virtual and phony properties.
		/// </summary>
		/// <param name="cache"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual int TargetClass(FdoCache cache)
		{
			if (m_targetClass == 0 && this.Source != 0)
				m_targetClass = cache.GetDestinationClass((uint)this.Source);
			return (int)m_targetClass;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance is sequence.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is sequence; otherwise, <c>false</c>.
		/// </value>
		/// ------------------------------------------------------------------------------------
		public bool IsSequence
		{
			get { return m_fSeq; }
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the parent.
		/// </summary>
		/// <value>The parent.</value>
		/// ------------------------------------------------------------------------------------
		public NeededPropertyInfo Parent
		{
			get { return m_parent; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the seq depth.
		/// </summary>
		/// <value>The seq depth.</value>
		/// ------------------------------------------------------------------------------------
		public int SeqDepth
		{
			get
			{
				int depth = 0;
				NeededPropertyInfo info = this;
				while (info != null)
				{
					if (info.m_fSeq)
						depth++;
					info = info.Parent;
				}
				return depth;
			}
		}

		/// <summary>
		/// The number of layers of parent (not counting this)
		/// </summary>
		public int Depth
		{
			get
			{
				int depth = 0;
				NeededPropertyInfo info = m_parent;
				while (info != null)
				{
					depth++;
					info = info.Parent;
				}
				return depth;
			}
		}

		/// <summary>
		/// Atomic properties of objects that can occur in the source field.
		/// </summary>
		public List<PropWs> AtomicFields
		{
			get { return m_atomicFlids; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add an atomic flid.
		/// </summary>
		/// <param name="flid">The flid.</param>
		/// <param name="ws">The ws.</param>
		/// ------------------------------------------------------------------------------------
		public void AddAtomicField(int flid, int ws)
		{
			PropWs pw = new PropWs(flid, ws);
			if (m_atomicFlids.Contains(pw))
				return;
			m_atomicFlids.Add(pw);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sequence properties of objects that can occur in the source field.
		/// </summary>
		/// <value>The seq fields.</value>
		/// ------------------------------------------------------------------------------------
		public List<NeededPropertyInfo> SeqFields
		{
			get { return m_sequenceInfo; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add (or retrieve) info about a object (atomic or seq) flid. May include virtuals.
		/// </summary>
		/// <param name="flid">The flid.</param>
		/// <param name="fSeq">if set to <c>true</c> [f seq].</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public NeededPropertyInfo AddObjField(int flid, bool fSeq)
		{
			NeededPropertyInfo info =
				m_sequenceInfo.Find(delegate(NeededPropertyInfo item)
					{ return item.Source == flid; });
			if (info == null)
			{
				info = new NeededPropertyInfo(flid, this, fSeq);
				m_sequenceInfo.Add(info);
			}
			return info;
		}
		/// <summary>
		/// Add (or retrieve) info about a virtual object flid.
		/// </summary>
		/// <param name="flid"></param>
		/// <param name="fSeq"></param>
		/// <param name="dstClass"></param>
		/// <returns></returns>
		public NeededPropertyInfo AddVirtualObjField(int flid, bool fSeq, int dstClass)
		{
			VirtualNeededPropertyInfo info =
				m_sequenceInfo.Find(delegate(NeededPropertyInfo item)
					{ return item.Source == flid; }) as VirtualNeededPropertyInfo;
			if (info == null)
			{
				info = new VirtualNeededPropertyInfo(flid, this, fSeq, dstClass);
				m_sequenceInfo.Add(info);
			}
			return info;
		}

		internal void DumpFieldInfo(IFwMetaDataCache mdc)
		{
			if (this.Depth == 0)
				Debug.WriteLine("");
			for (int i = 0; i < this.Depth; ++i)
				Debug.Write("    ");
			if (Source != 0)
			{
				Debug.WriteLine("[" + this.Depth + "]info.Source = " + this.Source + " = " +
					GetFancyFieldName(this.Source, mdc));
			}
			else
			{
				Debug.WriteLine("Root (target) class: " + mdc.GetClassName(m_targetClass));
			}

			for (int i = 0; i < this.AtomicFields.Count; ++i)
			{
				for (int j = 0; j < this.Depth; ++j)
					Debug.Write("    ");
				Debug.WriteLine("    Atomic[" + i + "] flid = " + this.AtomicFields[i].flid + "(" +
					GetFancyFieldName(this.AtomicFields[i].flid, mdc) + "); ws = " + this.AtomicFields[i].ws);
			}
			for (int i = 0; i < this.SeqFields.Count; ++i)
				this.SeqFields[i].DumpFieldInfo(mdc);
		}
		private string GetFancyFieldName(int flid, IFwMetaDataCache mdc)
		{
			string f = mdc.GetFieldName((uint)flid);
			string c = mdc.GetOwnClsName((uint)flid);
			return c + '_' + f;
		}


	}
	class VirtualNeededPropertyInfo : NeededPropertyInfo
	{
		public VirtualNeededPropertyInfo(int flidSource, NeededPropertyInfo parent, bool fSeq, int dstClsId)
			: base(flidSource, parent, fSeq)
		{
			m_targetClass = (uint)dstClsId;
		}

		/// <summary>
		/// Override: this class knows the appropriate destination class.
		/// </summary>
		public override int TargetClass(FdoCache cache)
		{
			return (int)m_targetClass;
		}
	}

	/// <summary>
	/// Compares CmObjects using their SortKey property.
	/// </summary>
	class CmObjectComparer : FwDisposableBase, IComparer<int>
	{
		private ILgCollatingEngine m_lce;
		private readonly FdoCache m_cache;

		public CmObjectComparer(FdoCache cache)
		{
			m_cache = cache;
		}

		public int Compare(int x, int y)
		{
			if (x == y)
				return 0;

			ICmObject xobj = CmObject.CreateFromDBObject(m_cache, x);
			ICmObject yobj = CmObject.CreateFromDBObject(m_cache, y);
			string xkeyStr = xobj.SortKey;
			string ykeyStr = yobj.SortKey;
			if (string.IsNullOrEmpty(xkeyStr) && string.IsNullOrEmpty(ykeyStr))
				return 0;
			if (string.IsNullOrEmpty(xkeyStr))
				return -1;
			if (string.IsNullOrEmpty(ykeyStr))
				return 1;

			if (m_lce == null)
			{
				string ws = xobj.SortKeyWs;
				if (string.IsNullOrEmpty(ws))
					ws = yobj.SortKeyWs;
				m_lce = LgIcuCollatorClass.Create();
				m_lce.Open(ws);
			}

			byte[] xkey = (byte[])m_lce.get_SortKeyVariant(xkeyStr, LgCollatingOptions.fcoDefault);
			byte[] ykey = (byte[])m_lce.get_SortKeyVariant(ykeyStr, LgCollatingOptions.fcoDefault);
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

		protected override void DisposeManagedResources()
		{
		}

		protected override void DisposeUnmanagedResources()
		{
			if (m_lce != null)
			{
				m_lce.Close();
				m_lce = null;
			}
		}
	}
}
