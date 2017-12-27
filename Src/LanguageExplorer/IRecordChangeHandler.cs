// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.LCModel;

namespace LanguageExplorer
{
	/// <summary>
	/// This interface is implemented to handle side-effects of changing the contents of an
	/// object that may be stored in a list.  If it is stored in a list, then the Fixup() method
	/// must be called before refreshing the list in order to ensure that those side-effects
	/// have occurred properly before redisplaying.
	/// </summary>
	internal interface IRecordChangeHandler : IDisposable
	{
		/// <summary>Initialize the object with the record and the list to which it belongs.
		/// </summary>
		void Setup(object /*"record"*/ o, IRecordListUpdater rlu, LcmCache cache);
		/// <summary>Fix the record for any changes, possibly refreshing the list to which it
		/// belongs.</summary>
		void Fixup(bool fRefreshList);

		/// <summary>
		/// True, if the updater was not null in the Setup call, otherwise false.
		/// </summary>
		bool HasRecordListUpdater
		{
			get;
		}

		/// <summary>
		/// Let users know it is beiong dispsoed
		/// </summary>
		event EventHandler Disposed;
	}
}