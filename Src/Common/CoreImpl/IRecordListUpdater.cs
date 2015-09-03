// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.Utils;

namespace SIL.CoreImpl
{
	// The following three interfaces are used in DetailControls and XWorks.  This seems like a
	// suitably general namespace for them.  Whether they belong in this particular file or
	// not, they need to go somewhere.
	#region IRecordListUpdater interface
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This interface is implemented to help handle side-effects of changing the contents of an
	/// object that may be stored in a list by providing access to that list.
	/// </summary>
	/// <remarks>Hungarian: rlu</remarks>
	/// ----------------------------------------------------------------------------------------
	public interface IRecordListUpdater
	{
		/// <summary>Set the IRecordChangeHandler object for this list.</summary>
		IRecordChangeHandler RecordChangeHandler { set; }
		/// <summary>Update the list, possibly calling IRecordChangeHandler.Fixup() first.
		/// </summary>
		void UpdateList(bool fRefreshRecord);

		/// <summary>
		/// just update the current record
		/// </summary>
		void RefreshCurrentRecord();
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This interface is implemented to help handle side-effects of changing the contents of an
	/// object that may be stored in a list.  Its single method returns either null or the
	/// list object stored under the given name.
	/// </summary>
	/// <remarks>Hungarian: rlo</remarks>
	/// ----------------------------------------------------------------------------------------
	public interface IRecordListOwner
	{
		/// <summary>Find the IRecordListUpdater object with the given name.</summary>
		IRecordListUpdater FindRecordListUpdater(string name);
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This interface is implemented to handle side-effects of changing the contents of an
	/// object that may be stored in a list.  If it is stored in a list, then the Fixup() method
	/// must be called before refreshing the list in order to ensure that those side-effects
	/// have occurred properly before redisplaying.
	/// </summary>
	/// <remarks>Hungarian: rch</remarks>
	/// ----------------------------------------------------------------------------------------
	public interface IRecordChangeHandler : IFWDisposable
	{
		/// <summary>Initialize the object with the record and the list to which it belongs.
		/// </summary>
		void Setup(object /*"record"*/ o, IRecordListUpdater rlu);
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
	#endregion

	#region Enumerations
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// The different window tiling options
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public enum WindowTiling
	{
		/// <summary>Top to bottom (horizontal)</summary>
		Stacked,
		/// <summary>Side by side (vertical)</summary>
		SideBySide,
	};

	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// This is used for menu items and toolbar buttons. Multiple strings for each command
	/// are stored together in the same string resource. See AfApp::GetResourceStr for more
	/// information.
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	public enum ResourceStringType
	{
		/// <summary></summary>
		krstHoverEnabled,
		/// <summary></summary>
		krstHoverDisabled,
		/// <summary></summary>
		krstStatusEnabled,
		/// <summary></summary>
		krstStatusDisabled,
		/// <summary></summary>
		krstItem,
	};
	#endregion

}
