// Copyright (c) 2008-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: IChangeRootObject.cs

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
