// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: IApp.cs
// Responsibility: TE Team
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// Interface for application
	/// </summary>
	public interface IApp
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Return a string from a resource ID.
		/// </summary>
		/// <param name="stid">String resource id</param>
		/// <returns>String</returns>
		/// -----------------------------------------------------------------------------------
		string ResourceString(string stid);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Refreshes all the views in all of the Main Windows of the app.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void RefreshAllViews();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Cycle through the applications main windows and synchronize them with database
		/// changes.
		/// </summary>
		/// <param name="sync">synchronization information record</param>
		/// <param name="cache">database cache</param>
		/// <returns><c>true</c> to continue processing; set to <c>false</c> to prevent
		/// processing of subsequent sync messages. </returns>
		/// ------------------------------------------------------------------------------------
		bool Synchronize(SyncInfo sync, FdoCache cache);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// To participate in automatic synchronization from the database (calling SyncFromDb
		/// in a useful manner) and application must override this, providing a unique Guid.
		/// Typically this is the Guid defined by a static AppGuid method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		Guid SyncGuid { get; }
	}
}
