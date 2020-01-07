// Copyright (c) 2011-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using SIL.LCModel.Utils;

namespace LanguageExplorer
{
	/// <summary>
	/// Utility class to provide progress reporting via console output instead of a dialog box.
	/// This may be useful for command line utility programs that access DLLs that use IProgress
	/// arguments for progress reporting.
	/// </summary>
	internal sealed class ConsoleProgress : IProgress
	{
		private readonly SingleThreadedSynchronizeInvoke m_sychronizeInvoke = new SingleThreadedSynchronizeInvoke();
		private int m_min;
		private int m_max = 100;
		private string m_message;
		private int m_dots;
		private int m_grain = 1;

		/// <summary>
		/// Let the caller know whether we've written any dots out for progress reporting.
		/// </summary>
		public bool DotsWritten => m_dots > 0;

		#region IProgress Members

		/// <summary>
		/// Gets an object to be used for ensuring that required tasks are invoked on the main
		/// UI thread.
		/// </summary>
		public ISynchronizeInvoke SynchronizeInvoke => m_sychronizeInvoke;

		/// <summary>
		/// Gets the form displaying the progress (used for message box owners, etc). If the progress
		/// is not associated with a visible Form, then this returns its owning form, if any.
		/// </summary>
		public System.Windows.Forms.Form Form => null;

		/// <summary>
		/// Gets or sets a value indicating whether this progress is indeterminate.
		/// </summary>
		public bool IsIndeterminate
		{
			get { return false; }
			set { }
		}

		/// <summary>
		/// Gets or sets the maximum value of the progress bar.
		/// </summary>
		public int Maximum
		{
			get { return m_max; }
			set
			{
				m_max = value;
				ComputeGranularity();
			}
		}

		private void ComputeGranularity()
		{
			m_grain = ((m_max - m_min) + 79) / 80;
			if (m_grain <= 0)
			{
				m_grain = 1;
			}
		}

		/// <summary>
		/// Get the message within the progress display window.
		/// </summary>
		public string Message
		{
			get { return m_message; }
			set
			{
				m_message = value;
				if (DotsWritten)
				{
					Console.WriteLine();
				}
				Console.WriteLine(m_message);
				m_dots = 0;
			}
		}

		/// <summary>
		/// Gets or sets the minimum value of the progress bar.
		/// </summary>
		public int Minimum
		{
			get { return m_min; }
			set
			{
				m_min = value;
				ComputeGranularity();
			}
		}

		/// <summary>
		/// Gets or sets the current position of the progress bar. This should be within the limits set by
		/// SetRange, or returned by GetRange.
		/// </summary>
		public int Position { get; set; }

		/// <summary>
		/// Cause the progress indicator to advance by the specified amount.
		/// </summary>
		/// <param name="amount">Amount of progress.</param>
		public void Step(int amount)
		{
			++Position;
			if ((Position % m_grain) == 0)
			{
				Console.Write('.');
				++m_dots;
			}
			if (Canceling != null)
			{
				// don't do anything -- this just shuts up the compiler about the
				// event handler never being used.
			}
		}

		/// <summary>
		/// Gets or sets the size of the step increment used by Step.
		/// </summary>
		public int StepSize
		{
			get { return 1; }
			set { }
		}

		/// <summary>
		/// Get the title of the progress display window.
		/// </summary>
		public string Title
		{
			get { return string.Empty; }
			set { }
		}

		/// <summary>
		/// Gets or sets a value indicating whether the operation executing on the separate thread
		/// can be cancelled by a different thread (typically the main UI thread).
		/// </summary>
		public bool AllowCancel
		{
			get { return false; }
			set { }
		}

		/// <summary>
		/// Event handler for listening to whether or the cancel button is pressed.
		/// </summary>
		public event CancelEventHandler Canceling;

		#endregion
	}
}