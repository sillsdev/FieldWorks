// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using SIL.LCModel;

namespace LanguageExplorer.Controls.DetailControls
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
	internal class TwoLevelConc : DataTree
	{
		#region member variables

		IConcPolicy m_cp;
		IGetNodeInfo m_gni;

		#endregion

		internal TwoLevelConc(LcmCache cache, IConcPolicy cp, IGetNodeInfo gni)
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
			for (var i = 0; i < cp.Count; i++)
			{
				FieldAt(i);
			}
		}

		// Must be overridden if nulls will be inserted into items; when real item is needed,
		// this is called to create it.
		public override Slice MakeEditorAt(int i)
		{
			CheckDisposed();

			var vs = new ViewSlice(new TwoLevelConcView(m_cp, m_gni, i));
			var newKids = new HashSet<Slice> {vs};
			InsertSliceRange(i, newKids);
			return vs;
		}
	}
}