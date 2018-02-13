// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections;
using System.Collections.Generic;
using SIL.LCModel;

namespace LanguageExplorer.Controls.DetailControls
{
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
	internal class MultiLevelConc : DataTree
	{
		#region member variables

		private readonly IList m_items; // of IConcSliceInfo
		#endregion
		internal MultiLevelConc(LcmCache cache, IList items)
		{
			m_items = items;
			InitializeBasic(cache, false);
			InitializeComponentBasic();
		}

		// Must be overridden if nulls will be inserted into items; when real item is needed,
		// this is called to create it.
		// Todo JohnT: can't just use m_items[i] once previous slices may have been
		// expanded. (Doesn't matter if we make them all to start with...)
		public override Slice MakeEditorAt(int i)
		{
			CheckDisposed();
			var csi = (IConcSliceInfo)m_items[i];
			ViewSlice vs = new ConcSlice(new ConcView(csi));
			if (csi.Count > 0)
			{
				vs.Expansion = TreeItemState.ktisCollapsed;
			}
			var newKids = new HashSet<Slice> {vs};
			InsertSliceRange(i, newKids);
			return vs;
		}

		public void InsertDummies(ConcSlice concSlice, int index, int count)
		{
			CheckDisposed();
			var dummies = new HashSet<Slice>();
			for (var i = 0; i < dummies.Count; i++)
			{
				dummies.Add(new DummyConcSlice(concSlice));
			}
			InsertSliceRange(index, dummies);
		}
	}
}