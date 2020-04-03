// Copyright (c) 2002-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.ViewsInterfaces;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// SelectionHelper helper class that provides public constructors
	/// </summary>
	internal sealed class DummySelectionHelper: SelectionHelper
	{
		/// <summary>
		/// The default constructor must be followed by a call to SetSelection before it will
		/// really be useful
		/// </summary>
		internal DummySelectionHelper()
		{
		}

		/// <summary>
		/// Create a selection helper based on an existing selection
		/// </summary>
		internal DummySelectionHelper(IVwSelection vwSel, SimpleRootSite rootSite)
			: base(vwSel, rootSite)
		{
		}

		/// <summary>
		/// Copy constructor
		/// </summary>
		internal DummySelectionHelper(SelectionHelper src)
			: base(src)
		{
		}

		/// <summary>
		/// Gets the Y-position of the selection relative to the upper left corner of the view.
		/// </summary>
		internal int IPTopY => m_dyIPTop;

		/// <summary>
		/// Creates a new dummy selection helper
		/// </summary>
		internal static DummySelectionHelper Create(SimpleRootSite rootSite)
		{
			return new DummySelectionHelper(SelectionHelper.Create(null, rootSite));
		}

		/// <summary>
		/// Sets the 0-based index of the character for the given limit of the selection.
		/// </summary>
		public override void SetIch(SelLimitType type, int value)
		{
			base.SetIch(type, value);
			SetTextPropId(type, SimpleRootsiteTestsConstants.kflidParaContents);
		}
	}
}