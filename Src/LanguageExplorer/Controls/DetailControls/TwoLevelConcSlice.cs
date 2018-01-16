// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Controls.DetailControls
{
	internal class TwoLevelConcSlice : ViewSlice
	{
		readonly TwoLevelConcView m_cv;

		internal TwoLevelConcSlice(TwoLevelConcView cv) : base(cv)
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

		private void ToggleExpansion()
		{
			m_cv.m_cvc.Expanded = !m_cv.m_cvc.Expanded;
			m_cv.RootBox.Reconstruct();
		}
	}
}