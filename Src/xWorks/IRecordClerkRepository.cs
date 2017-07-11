// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Interface for a RecordClerk repository that aims to replace all use of IPropertyTable to store RecordClerk instances.
	/// </summary>
	/// <remarks>
	/// 1. For now, the implementation will live in a static property of the RecordClerk class.
	///		Eventually, it may be added to the IFlexComponent InitializeFlexComponent method.
	/// 2. When the implementation is disposed, then so are all of its remaining clerks.
	///
	/// NB: Tools should never use the <see cref="GetRecordClerk"/> method of this interface,
	/// but they should use the similar one from the <see cref="IRecordClerkRepositoryForTools"/>.
	/// </remarks>
	internal interface IRecordClerkRepository : IDisposable
	{
		/// <summary>
		/// Add the <paramref name="recordClerk"/> to the repository.
		/// </summary>
		/// <param name="recordClerk">The clerk to add to the repository.</param>
		void AddRecordClerk(RecordClerk recordClerk);

		/// <summary>
		/// Remove the <paramref name="recordClerk"/> from the repository.
		/// </summary>
		/// <param name="recordClerk">The clerk to remove from the repository.</param>
		/// <remarks>
		/// A side effect of removing a clerk, is that <see cref="ActiveRecordClerk"/> is set to null,
		/// if it is the clerk being removed. The clerk being removed is also disposed.</remarks>
		void RemoveRecordClerk(RecordClerk recordClerk);

		/// <summary>
		/// Get the clerk with the given <paramref name="clerkId" />, or null if not found.
		/// </summary>
		/// <param name="clerkId">The Id of the clerk to return.</param>
		/// <returns>The clerk with the given <paramref name="clerkId"/>, or null if not found.</returns>
		RecordClerk GetRecordClerk(string clerkId);

		/// <summary>
		/// Get/Set the active clerk. Null is an acceptable value for both 'get' and 'set'.
		/// </summary>
		RecordClerk ActiveRecordClerk { get; set; }
	}
}