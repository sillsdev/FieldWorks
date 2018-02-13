// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// This is a dummy slice that can create real ones based on the policy.
	/// </summary>
	internal class DummyConcSlice : Slice
	{
		private readonly ConcSlice m_csParent;

		public DummyConcSlice(ConcSlice csParent)
		{
			m_csParent = csParent;
			Indent = m_csParent.Indent + 1;
		}

		public override bool IsRealSlice => false;

		public override Slice BecomeReal(int index)
		{
			// Figure position relative to parent node
			var parentIndex = index - 1;
			while (ContainingDataTree.Slices[parentIndex] != m_csParent)
			{
				parentIndex -= 1;
			}
			var childIndex = index - parentIndex - 1; // relative to parent
			var csi = m_csParent.SliceInfo.ChildAt(childIndex);
			ViewSlice vs = new ConcSlice(new ConcView(csi));
			vs.Indent = Indent;
			if (csi.Count > 0)
			{
				vs.Expansion = TreeItemState.ktisCollapsed;
			}

			ContainingDataTree.RawSetSlice(index, vs);
			return vs;
		}
	}
}