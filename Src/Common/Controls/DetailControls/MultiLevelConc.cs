// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.Utils;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// A Keyed object is one from which a key string can be retrieved.
	/// It is typically used to sort the objects.
	/// </summary>
	public interface IKeyedObject
	{
		string Key
		{
			get;
		}
	}
	/// <summary>
	/// A MultiLevelConc (concordance) displays a concordance. The concordance can consist
	/// of a mixture of context slices (which are leaf nodes in a tree, and typically display
	/// one occurrence of an interesting item in context) and summary slices (which
	/// typically show a particular interesting item, and can be expanded to show either
	/// further summaries or contexts).
	///
	/// The concordance is initialized with a list (any IList implementation)of IConcSliceInfo
	/// objects. This interface provides information to control how each slice is
	/// displayed. Some default implementations are provided in this component.
	///
	/// </summary>
	public class MultiLevelConc : DataTree
	{
		public interface IConcSliceInfo
		{
			/// <summary>
			/// This gets the object that the slice is mainly about. It may return
			/// zero, if the slice is not 'about' any particular object.
			/// </summary>
			int Hvo {get;}
			/// <summary>
			/// This returns a flid for a string property (of Hvo) that may be displayed as
			/// the contents of the slice. If this of Hvo returns 0, the following method
			/// is used instead to obtain the contents string.
			/// </summary>
			int ContentStringFlid {get;}
			/// <summary>
			/// Alternative context string (used if ContextStringFlid returns 0)
			/// </summary>
			ITsString ContentString {get;}
			/// <summary>
			/// True to allow the content string to be edited. It is assumed that
			/// the content strings are real properties and the cache will handle
			/// persisting the changes. Ignored if Hvo or ContentStringFlid returns 0.
			/// </summary>
			bool AllowContentEditing {get;}
			/// <summary>
			/// True to make this a context slice. In this case, the start offset
			/// and length methods are used to display the contents as a context string.
			/// </summary>
			bool DisplayAsContext {get;}
			/// <summary>
			/// Obtains the offset in the context string where the interesting word
			/// appears, given the HVO and position of the context object.
			/// </summary>
			int ContextStringStartOffset {get;}
			/// <summary>
			/// Obtains the length of the interesting word in the context string,
			/// given the HVO and position of the context object.
			/// </summary>
			int ContextStringLength {get;}
			/// <summary>
			/// This allows the client to supply a view constructor to control what
			/// appears in the slice. If it returns null, one of two standard views
			/// is used, depending on DisplayAsContext.
			/// </summary>
			IVwViewConstructor Vc {get;}
			/// <summary>
			/// Returns the number of children. Typically zero for context slices.
			/// </summary>
			int Count {get;}
			/// <summary>
			/// Returns the indicated child (0-based addressing). The index passed
			/// will be in the range 0..Count-1.
			/// </summary>
			IConcSliceInfo ChildAt(int index);
		}

		/// <summary>
		/// This implementation of ConcSliceInfo is initialized with an Hvo, flid, offset,
		/// and length. It provides information about a context slice (leaf node).
		/// </summary>
		public class ContextInfo : IConcSliceInfo
		{
			int m_hvo;
			int m_flid;
			int m_offset;
			int m_length;
			bool m_fAllowEdit;
			public ContextInfo(int hvo, int flid, int offset, int length, bool fAllowEdit)
			{
				m_hvo = hvo;
				m_flid = flid;
				m_offset = offset;
				m_length = length;
				m_fAllowEdit = fAllowEdit;
			}
			/// <summary>
			/// This gets the object that the slice is mainly about, often an StTxtPara.
			/// </summary>
			public int Hvo {get {return m_hvo;}}
			/// <summary>
			/// This returns a flid for a string property (of Hvo) that may be displayed as
			/// the contents of the slice.
			/// </summary>
			public int ContentStringFlid {get {return m_flid;}}
			/// <summary>
			/// Alternative context string (used if ContextStringFlid returns 0); hence
			/// not implemented for this class.
			/// </summary>
			public ITsString ContentString {get {return null;}}
			/// <summary>
			/// True to allow the content string to be edited. It is assumed that
			/// the content strings are real properties and the cache will handle
			/// persisting the changes.
			/// </summary>
			public bool AllowContentEditing {get {return m_fAllowEdit;}}
			/// <summary>
			/// True to make this a context slice. In this case, the start offset
			/// and length methods are used to display the contents as a context string.
			/// </summary>
			public virtual bool DisplayAsContext {get {return true;}}
			/// <summary>
			/// Obtains the offset in the context string where the interesting word
			/// appears, given the HVO and position of the context object.
			/// </summary>
			public int ContextStringStartOffset {get {return m_offset;}}
			/// <summary>
			/// Obtains the length of the interesting word in the context string,
			/// given the HVO and position of the context object.
			/// </summary>
			public int ContextStringLength {get {return m_length;}}
			/// <summary>
			/// This allows the client to supply a view constructor to control what
			/// appears in the slice. As it returns null, the standard context VC is used
			/// </summary>
			public virtual IVwViewConstructor Vc {get {return null;}}
			/// <summary>
			/// Returns the number of children. A context slice has none
			/// </summary>
			public int Count {get {return 0;}}
			/// <summary>
			/// There are no children.
			/// </summary>
			public IConcSliceInfo ChildAt(int index)
			{
				Debug.Assert(false);
				return null;
			}
		}

		/// <summary>
		/// This is a default summary info suitable for a concordance where the
		/// summary words are just extracted from text, and don't have any
		/// object they represent. Hence, it is initialized simply with a string
		/// and a list of children.
		/// </summary>
		public class SummaryInfo : IConcSliceInfo, IKeyedObject
		{
			ITsString m_content;
			IList m_children;
			public SummaryInfo(ITsString content, IList children)
			{
				m_content = content;
				m_children = children;
			}
			/// <summary>
			/// Don't have an interesting object.
			/// </summary>
			public int Hvo {get {return 0;}}
			/// <summary>
			/// Nor a flid
			/// </summary>
			public int ContentStringFlid {get {return 0;}}
			/// <summary>
			/// So we just provide the string to display.
			/// </summary>
			public ITsString ContentString {get {return m_content;}}
			public string Key
			{
				get
				{
					return m_content.Text;
				}
			}
			/// <summary>
			/// A string that isn't an object field can't be edited.
			/// </summary>
			public bool AllowContentEditing {get {return false;}}
			/// <summary>
			/// It's not a context string.
			/// </summary>
			public virtual bool DisplayAsContext {get {return false;}}
			/// <summary>
			/// Should not be called
			/// </summary>
			public int ContextStringStartOffset {get {Debug.WriteLine("Inappropriate call to ContextStringStartOffset"); return 0;}}
			/// <summary>
			/// Should not be called
			/// </summary>
			public int ContextStringLength {get {Debug.WriteLine("Inappropriate call to ContextStringLength"); return 0;}}
			/// <summary>
			/// This allows the client to supply a view constructor to control what
			/// appears in the slice. As it returns null, the standard summary VC is used.
			/// </summary>
			public virtual IVwViewConstructor Vc {get {return null;}}
			/// <summary>
			/// Returns the number of children.
			/// </summary>
			public int Count {get {return m_children.Count;}}
			/// <summary>
			/// Returns the specified child.
			/// </summary>
			public IConcSliceInfo ChildAt(int index)
			{
				return (IConcSliceInfo)m_children[index];
			}
		}

		class ContextVc : FwBaseVc
		{
			IConcSliceInfo m_info;

			public ContextVc(IConcSliceInfo info)
			{
				m_info = info;
			}

			public override void Display(IVwEnv vwenv, int hvo, int frag)
			{
				// Enhance JohnT: make the alignment position a function of window width.
				// Enhance JohnT: change background if this is the selected context line.
				vwenv.OpenConcPara(m_info.ContextStringStartOffset,
					m_info.ContextStringStartOffset + m_info.ContextStringLength,
					VwConcParaOpts.kcpoDefault,
					72 * 2 * 1000); // 72 pts per inch * 2 inches * 1000 -> 2" in millipoints.
				if (m_info.Hvo == 0 || m_info.ContentStringFlid == 0)
				{
					vwenv.AddString(m_info.ContentString);
				}
				else
				{
					Debug.Assert(hvo == m_info.Hvo);
					vwenv.AddStringProp(m_info.ContentStringFlid, this);
				}
				vwenv.CloseParagraph();
			}
		}
		class SummaryVc : FwBaseVc
		{
			IConcSliceInfo m_info;

			public SummaryVc(IConcSliceInfo info)
			{
				m_info = info;
			}

			public override void Display(IVwEnv vwenv, int hvo, int frag)
			{
				// Enhance JohnT: change background if this is the selected slice.
				vwenv.OpenParagraph();
				if (m_info.Hvo == 0 || m_info.ContentStringFlid == 0)
				{
					vwenv.AddString(m_info.ContentString);
				}
				else
				{
					Debug.Assert(hvo == m_info.Hvo);
					vwenv.AddStringProp(m_info.ContentStringFlid, this);
				}
				vwenv.CloseParagraph();
			}
		}

		public class ConcView : RootSiteControl
		{
			IConcSliceInfo m_info;
			IVwViewConstructor m_vc;
			public ConcView(IConcSliceInfo info)
			{
				m_info = info;
			}
			public IConcSliceInfo SliceInfo
			{
				get { CheckDisposed(); return m_info; }
			}

			/*
						/// <summary>
						/// Override to remove the TAB key from treatment as a normal key.
						/// </summary>
						/// <param name="keyData"></param>
						/// <returns>false, if it is a TAB key, otherwise whatever the superclass returns.</returns>
						protected override bool IsInputKey(Keys keyData)
						{
							if (keyData == Keys.Tab)
								return false;
							return base.IsInputKey(keyData);
						}
			*/

			public override void MakeRoot()
			{
				CheckDisposed();
				base.MakeRoot();

				if (m_fdoCache == null || DesignMode)
					return;

				IVwRootBox rootb = VwRootBoxClass.Create();
				rootb.SetSite(this);

				rootb.DataAccess = m_fdoCache.DomainDataByFlid;

				m_vc = m_info.Vc;
				if (m_vc == null)
				{
					if (m_info.DisplayAsContext)
					{
						m_vc = new ContextVc(m_info);
					}
					else
					{
						m_vc = new SummaryVc(m_info);
					}
				}

				// The root object is the one, if any, that the policy gives us.
				// If it doesn't give us one the vc will obtain a key string from the policy
				// directly. The frag argument is arbitrary. Note that we have to use a non-zero
				// HVO, even when it doesn't mean anything, to avoid triggering an Assert in the Views code.
				rootb.SetRootObject(m_info.Hvo == 0 ? 1 : m_info.Hvo, m_vc, 1, m_styleSheet);
				this.m_rootb = rootb;
			}
		}

		/// <summary>
		/// This is a dummy slice that can create real ones based on the policy.
		/// </summary>
		public class DummyConcSlice : Slice
		{
			ConcSlice m_csParent;

			public DummyConcSlice(ConcSlice csParent)
			{
				m_csParent = csParent;
				m_indent = m_csParent.Indent + 1;
			}

			public override bool IsRealSlice
			{
				get
				{
					CheckDisposed();
					return false;
				}
			}

			public override Slice BecomeReal(int index)
			{
				CheckDisposed();
				// Figure position relative to parent node
				int parentIndex = index - 1;
				while (ContainingDataTree.Slices[parentIndex] != m_csParent)
					parentIndex -= 1;
				int childIndex = index - parentIndex - 1; // relative to parent
				IConcSliceInfo csi = (IConcSliceInfo) m_csParent.SliceInfo.ChildAt(childIndex);
				ViewSlice vs = new ConcSlice(new ConcView(csi));
				vs.Indent = this.Indent;
				if (csi.Count > 0)
					vs.Expansion = DataTree.TreeItemState.ktisCollapsed;

				ContainingDataTree.RawSetSlice(index, vs);
				return vs;
			}
		}

		public class ConcSlice : ViewSlice
		{
			ConcView m_cv;
			public ConcSlice(ConcView cv) : base(cv)
			{
				m_cv = cv;
			}

			public IConcSliceInfo SliceInfo
			{
				get
				{
					CheckDisposed();
					return m_cv.SliceInfo;
				}
			}
			/// <summary>
			/// Expand this node, which is at position iSlice in its parent.
			/// </summary>
			/// <param name="iSlice"></param>
			public override void Expand(int iSlice)
			{
				CheckDisposed();
				((MultiLevelConc)ContainingDataTree).InsertDummies(this, iSlice + 1, SliceInfo.Count);
				Expansion = DataTree.TreeItemState.ktisExpanded;
				this.PerformLayout();
				this.Invalidate(true); // invalidates all children.
			}
		}
		#region member variables
		IList m_items; // of IConcSliceInfo
		#endregion
		public MultiLevelConc(FDO.FdoCache cache, IList items)
		{
			m_items = items;
			InitializeBasic(cache, false);
			InitializeComponentBasic();

			// Can't add null controls to a parent control.
			//m_slices.AddRange(new Slice[items.Count]); // adds appropriate # nulls

			//			// Temporary: until I figure how to be lazy, we have to make slices
			//			// for all nodes.
			//			for (int i = 0; i < items.Count; i++)
			//				FieldAt(i);
		}

		// Must be overridden if nulls will be inserted into items; when real item is needed,
		// this is called to create it.
		// Todo JohnT: can't just use m_items[i] once previous slices may have been
		// expanded. (Doesn't matter if we make them all to start with...)
		public override Slice MakeEditorAt(int i)
		{
			CheckDisposed();
			IConcSliceInfo csi = (IConcSliceInfo)m_items[i];
			ViewSlice vs = new ConcSlice(new ConcView(csi));
			if (csi.Count > 0)
				vs.Expansion = DataTree.TreeItemState.ktisCollapsed;
			Set<Slice> newKids = new Set<Slice>(1);
			newKids.Add(vs);
			InsertSliceRange(i, newKids);
			return vs;
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="DummyConSlice gets added to control collection and disposed there.")]
		public void InsertDummies(ConcSlice concSlice, int index, int count)
		{
			CheckDisposed();
			Set<Slice> dummies = new Set<Slice>(count);
			for (int i = 0; i < dummies.Count; i++)
				dummies.Add(new DummyConcSlice(concSlice));
			InsertSliceRange(index, dummies);
		}
	}
}
