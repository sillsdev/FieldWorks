// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using System;

namespace SIL.FieldWorks.FDO.Application
{
	/// <summary>
	/// Add some .Net friendly methods to ISilDataAccess.
	/// These are not exposed via COM.
	/// </summary>
	public interface ISilDataAccessManaged : ISilDataAccess, IStructuredTextDataAccess
	{
		/// <summary>
		/// Get the Ids of the entire vector property.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <returns>The Ids of entire vector property</returns>
		int[] VecProp(int hvo, int tag);

		/// <summary>
		/// Get the binary data property of an object.
		///</summary>
		/// <param name='hvo'></param>
		/// <param name='tag'></param>
		/// <param name='rgb'>Contains the binary data</param>
		/// <returns>byte count in binary data property</returns>
		int get_Binary(int hvo, int tag, out byte[] rgb);

		/// <summary>
		/// Get the generic date property.
		/// </summary>
		/// <param name="hvo">The hvo.</param>
		/// <param name="tag">The tag.</param>
		/// <returns>The generic date.</returns>
		GenDate get_GenDateProp(int hvo, int tag);

		/// <summary>
		/// Set the generic date property.
		/// </summary>
		/// <param name="hvo">The hvo.</param>
		/// <param name="tag">The tag.</param>
		/// <param name="genDate">The generic date.</param>
		void SetGenDate(int hvo, int tag, GenDate genDate);

		/// <summary>
		/// Get the time property as a DateTime value.
		/// </summary>
		DateTime get_DateTime(int hvo, int tag);

		/// <summary>
		/// Set the time property with a DateTime value.
		/// </summary>
		void SetDateTime(int hvo, int tag, DateTime dt);
	}

	/// <summary>
	/// Interface that provides services to ISilDataAccess impl.
	/// </summary>
	internal interface ISilDataAccessHelperInternal
	{
		/// <summary>
		/// Request notification when properties change. The ${IVwNotifyChange#PropChanged}
		/// method will be called when the property changes (provided the client making the
		/// change properly calls ${#PropChanged}.
		///</summary>
		/// <param name='nchng'> </param>
		void AddNotification(IVwNotifyChange nchng);

		/// <summary> Request removal from the list of objects to notify when properties change. </summary>
		/// <param name='nchng'> </param>
		void RemoveNotification(IVwNotifyChange nchng);

		/// <summary>
		/// The one on which new UOWs will happen (typically set by activating a main window).
		/// </summary>
		IActionHandler CurrentUndoStack { get; }

		/// <summary>
		/// The currently active one (usually the same as current, but it is not allowed to change until
		/// the current one completes).
		/// </summary>
		IActionHandler ActiveUndoStack { get; }
	}
}