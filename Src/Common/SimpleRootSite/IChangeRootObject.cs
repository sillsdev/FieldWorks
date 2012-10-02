// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2008' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: IChangeRootObject.cs
// --------------------------------------------------------------------------------------------
using System;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// This interface is an extra one that must be implemented by subclasses
	/// of IRootSite used in classes like RecordDocView.
	/// The typical implementation of SetRoot is to call SetRootObject on the
	/// RootBox, passing also your standard view constructor and other arguments.
	/// </summary>
	public interface IChangeRootObject
	{
		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		void SetRoot(int hvo);
	}
}
