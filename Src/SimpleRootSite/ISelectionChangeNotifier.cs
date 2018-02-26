// Copyright (c) 2008-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ISelectionChangeNotifier.cs

using System;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// This interface should be implemented by rootsites and rootsite "proxies" that need
	/// to be able to notify interested parties of changes to the VwSelection owned by their
	/// RootBoxes.
	/// </summary>
	public interface ISelectionChangeNotifier
	{
		/// <summary>
		/// Event handler for when the rootbox's selection changes.
		/// </summary>
		event EventHandler<VwSelectionArgs> VwSelectionChanged;
	}
}
