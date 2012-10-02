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
// File: ISelectionChangeNotifier.cs
// --------------------------------------------------------------------------------------------
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
