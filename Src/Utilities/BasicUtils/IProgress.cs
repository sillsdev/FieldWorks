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
// File: IProgress.cs
// ---------------------------------------------------------------------------------------------
using System.ComponentModel;

namespace SIL.Utils
{
	/// --------------------------------------------------------------------------------------------
	/// <summary>
	/// This is a progress reporting interface.
	/// </summary>
	/// --------------------------------------------------------------------------------------------
	public interface IProgress
	{
		/// <summary>
		/// Event handler for listening to whether or the cancel button is pressed.
		/// </summary>
		event CancelEventHandler Canceling;

		/// <summary>
		/// Cause the progress indicator to advance by the specified amount.
		/// </summary>
		/// <param name="amount">Amount of progress.</param>
		void Step(int amount);

		/// <summary>
		/// Get the title of the progress display window.
		/// </summary>
		/// <value>The title.</value>
		string Title
		{
			get;
			set;
		}

		/// <summary>
		/// Get the message within the progress display window.
		/// </summary>
		/// <value>The message.</value>
		string Message
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the current position of the progress bar. This should be within the limits set by
		/// SetRange, or returned by GetRange.
		/// </summary>
		/// <value>The position.</value>
		int Position
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the size of the step increment used by Step.
		/// </summary>
		/// <value>The size of the step.</value>
		int StepSize
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the minimum value of the progress bar.
		/// </summary>
		/// <value>The minimum.</value>
		int Minimum
		{
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the maximum value of the progress bar.
		/// </summary>
		/// <value>The maximum.</value>
		int Maximum
		{
			get;
			set;
		}

		/// <summary>
		/// Gets an object to be used for ensuring that required tasks are invoked on the main
		/// UI thread.
		/// </summary>
		ISynchronizeInvoke SynchronizeInvoke { get; }

		/// <summary>
		/// Gets or sets a value indicating whether this progress is indeterminate.
		/// </summary>
		bool IsIndeterminate { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the opertation executing on the separate thread
		/// can be cancelled by a different thread (typically the main UI thread).
		/// </summary>
		bool AllowCancel { get; set; }
	}
}
