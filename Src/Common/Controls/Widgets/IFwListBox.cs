// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel.Core.KernelInterfaces;

namespace SIL.FieldWorks.Common.Widgets
{
	/// <summary />
	public interface IFwListBox
	{
		/// <summary>
		/// Obtain the text corresponding to the specified item in your contents list.
		/// </summary>
		ITsString TextOfItem(object item);

		/// <summary>
		/// Gets the data access.
		/// </summary>
		ISilDataAccess DataAccess { get; }

		/// <summary />
		int SelectedIndex { get; set; }

		/// <summary>
		/// Gets a value indicating whether this class is updating.
		/// </summary>
		bool Updating { get; }
	}
}