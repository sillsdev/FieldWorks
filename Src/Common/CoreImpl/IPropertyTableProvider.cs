// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.CoreImpl
{
	/// <summary>
	/// Implementors can provide a PropertyTable
	/// </summary>
	public interface IPropertyTableProvider
	{
		/// <summary>
		/// Placement in the IPropertyTableProvider interface lets FwApp call PropertyTable.DoStuff.
		/// </summary>
		PropertyTable PropTable
		{
			get;
		}
	}
}