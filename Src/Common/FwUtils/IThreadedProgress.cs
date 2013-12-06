// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: IThreadedProgress.cs
// ---------------------------------------------------------------------------------------------
using System;
using SIL.Utils;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// --------------------------------------------------------------------------------------------
	/// <summary>
	/// This interface is for classes that can record progress on cancellable tasks that can be run
	/// in a separate thread.
	/// </summary>
	/// --------------------------------------------------------------------------------------------
	public interface IThreadedProgress : IProgress
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the task has been canceled.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool Canceled { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If progress dialog is already showing, we run the background task using it (without
		/// creating a separate thread). Otherwise we display a new progress dialog as a modal
		/// dialog and start the background task in a separate thread.
		/// </summary>
		/// <param name="backgroundTask">The background task.</param>
		/// <param name="parameters">The paramters that will be passed to the background task</param>
		/// <returns>The return value from the background thread.</returns>
		/// <exception cref="WorkerThreadException">Wraps any exception thrown by the background
		/// task</exception>
		/// ------------------------------------------------------------------------------------
		object RunTask(Func<IThreadedProgress, object[], object> backgroundTask,
			params object[] parameters);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Displays the progress dialog as a modal dialog and starts the background task.
		/// </summary>
		/// <param name="fDisplayUi">set to <c>true</c> to display the progress dialog,
		/// <c>false</c> to run without UI.</param>
		/// <param name="backgroundTask">The background task.</param>
		/// <param name="parameters">The paramters that will be passed to the background task</param>
		/// <returns>The return value from the background thread.</returns>
		/// <exception cref="WorkerThreadException">Wraps any exception thrown by the background
		/// task</exception>
		/// ------------------------------------------------------------------------------------
		object RunTask(bool fDisplayUi, Func<IThreadedProgress, object[], object> backgroundTask,
			params object[] parameters);
	}

	#region WorkerThreadException
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This exception is thrown by a foreground thread when a background worker thread
	/// had an exception. This allows all exceptions to be handled by the foreground thread.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class WorkerThreadException : Exception
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="WorkerThreadException"/> class.
		/// </summary>
		/// <param name="message">The message.</param>
		/// <param name="innerException">The inner exception.</param>
		/// ------------------------------------------------------------------------------------
		public WorkerThreadException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
	#endregion
}
