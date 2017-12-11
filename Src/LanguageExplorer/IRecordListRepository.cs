// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace LanguageExplorer
{
	/// <summary>
	/// Interface for a RecordList repository that aims to replace all use of IPropertyTable to store RecordList instances.
	/// </summary>
	/// <remarks>
	/// 1. For now, the implementation will live in a static property of the RecordList class.
	///		Eventually, it may be added to the IFlexComponent InitializeFlexComponent method.
	/// 2. When the implementation is disposed, then so are all of its remaining record lists.
	///
	/// NB: Tools should never use the <see cref="GetRecordList"/> method of this interface,
	/// but they should use the similar one from the <see cref="IRecordListRepositoryForTools"/>.
	/// </remarks>
	internal interface IRecordListRepository : IDisposable
	{
		/// <summary>
		/// Add the <paramref name="recordList"/> to the repository.
		/// </summary>
		/// <param name="recordList">The record list to add to the repository.</param>
		void AddRecordList(IRecordList recordList);

		/// <summary>
		/// Remove the <paramref name="recordList"/> from the repository.
		/// </summary>
		/// <param name="recordList">The record list to remove from the repository.</param>
		/// <remarks>
		/// A side effect of removing a record list, is that <see cref="ActiveRecordList"/> is set to null,
		/// if it is the record list being removed. The record list being removed is also disposed.</remarks>
		void RemoveRecordList(IRecordList recordList);

		/// <summary>
		/// Get the record list with the given <paramref name="recordListId" />, or null if not found.
		/// </summary>
		/// <param name="recordListId">The Id of the record list to return.</param>
		/// <returns>The record list with the given <paramref name="recordListId"/>, or null if not found.</returns>
		IRecordList GetRecordList(string recordListId);

		/// <summary>
		/// Get/Set the active record list. Null is an acceptable value for both 'get' and 'set'.
		/// </summary>
		IRecordList ActiveRecordList { get; set; }
	}
}