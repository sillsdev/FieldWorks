using SIL.FieldWorks.FDO;
using SIL.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// A TwoLevelConc (concordance) displays a concordance that is initially a
	/// list of words, each of which can be expanded to show a context list.
	///
	/// The most basic version is initialized with an Array of HVOs, one
	/// for each top-level object. A flid is specified which can be used to obtain
	/// a string that is the keyword for that object (displayed in the top-level
	/// node).
	///
	/// Enhance: plan to allow an interface to be supplied that allows
	/// a different string property to be displayed as the name of each hvo.
	/// TwoLevelConc itself will implement the interface, by supplying the flid from
	/// its member variable.
	///
	/// Enhance: plan to allow an interface to be supplied that handles
	/// consequences of the user editing the keyword string (e.g., correcting
	/// spelling in all the occurrences). It will also have a flag indicating
	/// whether such editing is allowed. TwoLevelConc itself will implement this
	/// trivially to say that editing is not allowed.
	///
	/// A call-back interface, IGetNodeInfo, is specified to allow the client to supply the
	/// information needed when a particular node is to be expanded. The information
	/// is supplied in the form of a INodeInfo object. A default implementation of
	/// INodeInfo, SimpleNodeInfo, is provided in this package.
	/// </summary>
	public class TwoLevelConc : DataTree
	{
		public interface INodeInfo
		{
			/// <summary>
			/// A flid that can be used to obtain from the cache an object sequence
			/// property of the HVO for the slice, giving the list of objects
			/// each of which produces one line in the cache.
			/// </summary>
			int ListFlid {get;}
			/// <summary>
			/// For each context object, this returns a flid that may be displayed as
			/// the context for that object. If this returns 0, the following method
			/// is used instead to obtain the context string. It is passed both the
			/// position and actual hvo of the context object.
			/// </summary>
			int ContextStringFlid(int ihvoContext, int hvoContext);
			/// <summary>
			/// Alternative context string (used if ContextStringFlid returns 0)
			/// </summary>
			ITsString ContextString(int ihvoContext, int hvoContext);
			/// <summary>
			/// True to allow the context string to be edited. It is assumed that
			/// the context strings are real properties and the cache will handle
			/// persisting the changes. Ignored if ContextStringFlid returns 0.
			/// </summary>
			bool AllowContextEditing {get;}
			/// <summary>
			/// Obtains the offset in the context string where the interesting word
			/// appears, given the HVO and position of the context object.
			/// </summary>
			int ContextStringStartOffset(int ihvoContext, int hvoContext);
			/// <summary>
			/// Obtains the length of the interesting word in the context string,
			/// given the HVO and position of the context object.
			/// </summary>
			int ContextStringLength(int ihvoContext, int hvoContext);
		}
		/// <summary>
		/// The is the basic interface that must be implemented to configure a TwoLevelConc.
		/// Review JohnT: very possibly this method could just be part of IConcPolicy.
		/// However, clients are much less likely to use one of the default implementations
		/// for this method.
		/// </summary>
		public interface IGetNodeInfo
		{
			/// <summary>
			/// Obtain an implementation of INodeInfo for the specified object at the specified
			/// position in the overall list of top-level objects.
			/// </summary>
			INodeInfo InfoFor(int ihvoRoot, int hvoRoot);
		}

		/// <summary>
		/// This interface is used to configure a TwoLevelConc. It supplies the basic
		/// information: the list of top-level objects that are displayed, and the
		/// flid used to obtain the key to display for each.
		///
		/// Enhance JohnT: add methods to handle spelling change, etc.
		/// </summary>
		public interface IConcPolicy
		{
			/// <summary>
			/// The number of slices in the top-level concordance.
			/// </summary>
			int Count {get;}
			/// <summary>
			/// Get the ith item (HVO) to display. If this returns 0, the key to display
			/// is obtained from KeyFor without calling FlidFor.
			/// </summary>
			/// <param name="i"></param>
			/// <returns></returns>
			int Item(int i);
			/// <summary>
			/// Get the flid to use to obtain a key for the ith item. If it answers 0,
			/// Use the KeyFor instead.
			/// </summary>
			/// <param name="islice"></param>
			/// <param name="hvo"></param>
			/// <returns></returns>
			int FlidFor(int islice, int hvo);
			/// <summary>
			/// Get the key to display for the ith slice, given its index and hvo.
			/// This method is used if Item(islice) returns 0, or if FlidFor(islice, hvo)
			/// returns 0. If it is used, the key will definitely not be editable.
			/// </summary>
			ITsString KeyFor(int islice, int hvo);
		}

		/// <summary>
		/// This implementation of IConcPolicy is initialized with a list of key strings.
		/// There should be one for each slice.
		/// </summary>
		public class StringListConcPolicy : IConcPolicy
		{
			ITsString[] m_strings;
			public StringListConcPolicy(ITsString[] strings)
			{
				m_strings = strings;
			}
			/// <summary>
			/// The number of slices in the top-level concordance: one for each string.
			/// </summary>
			public int Count {get {return m_strings.Length;}}
			/// <summary>
			/// This version does not use HVOs for items.
			/// </summary>
			/// <param name="i"></param>
			/// <returns></returns>
			public int Item(int i) {return 0;}
			/// <summary>
			/// This version does not use FLIDs to get key strings.
			/// </summary>
			/// <param name="islice"></param>
			/// <param name="hvo"></param>
			/// <returns></returns>
			public int FlidFor(int islice, int hvo) {return 0;}
			/// <summary>
			/// Just return the islice'th string.
			/// </summary>
			public ITsString KeyFor(int islice, int hvo) {return m_strings[islice];}
		}

		/// <summary>
		/// A ParaNodeinfo assumes that all the context strings are the contents of
		/// StTxtParas. It assumes the length of all key strings is constant, typically
		/// the length of the header node string. The offsets are supplied as a
		/// parameter to the constructor. Context editing is off by default, but
		/// may be turned on.
		/// </summary>
		public class ParaNodeInfo : INodeInfo
		{
			int[] m_startOffsets;
			int m_keyLength;
			int m_flidList;
			bool m_fAllowContextEditing = false;
			public ParaNodeInfo(int flidList, int[] startOffsets, int keyLength)
			{
				m_startOffsets = startOffsets;
				m_keyLength = keyLength;
				m_flidList = flidList;
			}
			/// <summary>
			/// This constructor looks up the headword of the slice to get the key length.
			/// </summary>
			public ParaNodeInfo(int flidList, int[] startOffsets, ISilDataAccess sda, int hvoHeadObj, int flid)
			{
				m_flidList = flidList;
				ITsString str = sda.get_StringProp(hvoHeadObj, flid);
				m_keyLength = str.Length;
				m_startOffsets = startOffsets;
			}
			/// <summary>
			/// A flid that can be used to obtain from the cache an object sequence
			/// property of the HVO for the slice, giving the list of objects
			/// each of which produces one line in the cache.
			/// </summary>
			public int ListFlid
			{
				get
				{
					return m_flidList;
				}
			}
			/// <summary>
			/// This implementation assumes it is dealing with contents of paragraphs.
			/// </summary>
			public int ContextStringFlid(int ihvoContext, int hvoContext)
			{
				return StTxtParaTags.kflidContents;
			}
			/// <summary>
			/// Alternative context string (not used in this impl)
			/// </summary>
			public ITsString ContextString(int ihvoContext, int hvoContext)
			{
					return null;
			}
			/// <summary>
			/// True to allow the context string to be edited. It is assumed that
			/// the context strings are real properties and the cache will handle
			/// persisting the changes. Ignored if ContextStringFlid returns 0.
			/// </summary>
			public bool AllowContextEditing
			{
				get
				{
					return m_fAllowContextEditing;
				}
				set
				{
					m_fAllowContextEditing = value;
				}
			}
			/// <summary>
			/// Obtains the offset in the context string where the interesting word
			/// appears, given the HVO and position of the context object.
			/// </summary>
			public int ContextStringStartOffset(int ihvoContext, int hvoContext)
			{
				return m_startOffsets[ihvoContext];
			}
			/// <summary>
			/// Obtains the length of the interesting word in the context string,
			/// given the HVO and position of the context object.
			/// </summary>
			public int ContextStringLength(int ihvoContext, int hvoContext)
			{
				return m_keyLength;
			}
		}
		class ConcVc : FwBaseVc
		{
			IConcPolicy m_cp;
			IGetNodeInfo m_gni;
			int m_index; // slice number
			bool m_fExpanded = false;
			INodeInfo m_ni = null;
			public ConcVc(IConcPolicy cp, IGetNodeInfo gni, int index)
			{
				m_cp = cp;
				m_gni = gni;
				m_index = index;
			}
			public bool Expanded
			{
				get
				{
					return m_fExpanded;
				}
				set
				{
					// Caller should arrange to regenerate the view.
					m_fExpanded = value;
					// Review JohnT: should we refresh this even if already obtianed when expanding?
					if (m_fExpanded && m_ni == null)
						m_ni = m_gni.InfoFor(m_index, m_cp.Item(m_index));

				}
			}
			public override void Display(IVwEnv vwenv, int hvo, int frag)
			{
				switch(frag)
				{
					case 1:
					{
						// The top-level.
						// Enhance JohnT: add a property setting to make the key bold
						// Roughly, vwenv.set_IntProperty(ktptBold, ktpvEnum, kttvForceOn);
						// If we can get an hvo and flid, display that property of that object.
						int flid = 0;
						if (hvo != 0)
						{
							flid = m_cp.FlidFor(m_index, hvo);
						}
						if (flid != 0)
						{
							// Warning (JohnT): this option not yet tested...
							vwenv.AddStringProp(flid, this);
							return;
						}
						else
						{
							// Otherwise display a literal string straight from the policy object.
							vwenv.AddString(m_cp.KeyFor(m_index, hvo));
						}

						if (m_fExpanded)
						{
							vwenv.AddLazyVecItems(m_ni.ListFlid, this, 2);
						}
						break;
					}
					case 2:
					{
						// One line of context.

						// Figure the index of this object in the next object out (the root).
						int hvoOuter, tagOuter, ihvo;
						vwenv.GetOuterObject(vwenv.EmbeddingLevel - 1,
							out hvoOuter, out tagOuter, out ihvo);
						int ichKey = m_ni.ContextStringStartOffset(ihvo, hvo);
						int cchKey = m_ni.ContextStringLength(ihvo, hvo);
						// Enhance JohnT: make the alignment position a function of window width.
						// Enhance JohnT: change background if this is the selected context line.
						vwenv.OpenConcPara(ichKey, ichKey + cchKey,
							VwConcParaOpts.kcpoDefault,
							72 * 2 * 1000); // 72 pts per inch * 2 inches * 1000 -> 2" in millipoints.
						int flidKey = m_ni.ContextStringFlid(ihvo, hvo);
						if (flidKey == 0)
						{
							// Not tested yet.
							vwenv.AddString(m_ni.ContextString(ihvo, hvo));
						}
						else
						{
							vwenv.AddStringProp(flidKey, this);
						}
						vwenv.CloseParagraph();
						break;
					}
				}
			}
		}

		class ConcView : RootSiteControl
		{
			internal ConcVc m_cvc;
			IConcPolicy m_cp;
			IGetNodeInfo m_gni;
			int m_index; // slice number
			public ConcView(IConcPolicy cp, IGetNodeInfo gni, int index)
			{
				m_cp = cp;
				m_gni = gni;
				m_index = index;
			}

			public override void MakeRoot()
			{
				CheckDisposed();
				base.MakeRoot();

				if (m_fdoCache == null || DesignMode)
					return;

				IVwRootBox rootb = VwRootBoxClass.Create();
				rootb.SetSite(this);

				rootb.DataAccess = m_fdoCache.DomainDataByFlid;

				m_cvc = new ConcVc(m_cp, m_gni, m_index);

				// The root object is the one, if any, that the policy gives us for this slice.
				// If it doesn't give us one the vc will obtain a key string from the policy
				// directly. The frag argument is arbitrary.
				rootb.SetRootObject(m_cp.Item(m_index), m_cvc, 1, m_styleSheet);
				m_rootb = rootb;
			}
		}

		public class ConcSlice : ViewSlice
		{
			ConcView m_cv;

			ConcSlice(ConcView cv) : base(cv)
			{
				m_cv = cv;
			}
			/// <summary>
			/// Expand this node, which is at position iSlice in its parent.
			/// </summary>
			/// <param name="iSlice"></param>
			public override void Expand(int iSlice)
			{
				CheckDisposed();
				ToggleExpansion();
			}
			/// <summary>
			/// Collapse this node, which is at position iSlice in its parent.
			/// </summary>
			/// <param name="iSlice"></param>
			public override void Collapse(int iSlice)
			{
				CheckDisposed();
				ToggleExpansion();
			}
			void ToggleExpansion()
			{
				m_cv.m_cvc.Expanded = ! m_cv.m_cvc.Expanded;
				m_cv.RootBox.Reconstruct();
			}
		}
		#region member variables

		IConcPolicy m_cp;
		IGetNodeInfo m_gni;

		#endregion

		public TwoLevelConc(FdoCache cache, IConcPolicy cp, IGetNodeInfo gni)
		{
			m_cp = cp;
			m_gni = gni;
			InitializeBasic(cache, false); // JT: was Initialize, that now has more args; not retested.
			InitializeComponentBasic();

			// Can't add null values to Controls,
			// now that there isn't a SliceCollection, which could hold nulls.
			//m_slices.AddRange(new Slice[cp.Count]); // adds appropriate # nulls

			// Temporary: until I figure how to be lazy, we have to make slices
			// for all nodes.
			for (int i = 0; i < cp.Count; i++)
				FieldAt(i);
		}

		// Must be overridden if nulls will be inserted into items; when real item is needed,
		// this is called to create it.
		public override Slice MakeEditorAt(int i)
		{
			CheckDisposed();

			ViewSlice vs = new ViewSlice(new ConcView(m_cp, m_gni, i));
			Set<Slice> newKids = new Set<Slice>(1);
			newKids.Add(vs);
			InsertSliceRange(i, newKids);
			return vs;
		}
	}
}
