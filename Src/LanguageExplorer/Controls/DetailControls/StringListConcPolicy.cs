// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// This implementation of IConcPolicy is initialized with a list of key strings.
	/// There should be one for each slice.
	/// </summary>
	internal class StringListConcPolicy : IConcPolicy
	{
		ITsString[] m_strings;
		public StringListConcPolicy(ITsString[] strings)
		{
			m_strings = strings;
		}
		/// <summary>
		/// The number of slices in the top-level concordance: one for each string.
		/// </summary>
		public int Count => m_strings.Length;

		/// <summary>
		/// This version does not use HVOs for items.
		/// </summary>
		public int Item(int i) { return 0; }

		/// <summary>
		/// This version does not use FLIDs to get key strings.
		/// </summary>
		public int FlidFor(int islice, int hvo) { return 0; }

		/// <summary>
		/// Just return the islice'th string.
		/// </summary>
		public ITsString KeyFor(int islice, int hvo) { return m_strings[islice]; }
	}
}