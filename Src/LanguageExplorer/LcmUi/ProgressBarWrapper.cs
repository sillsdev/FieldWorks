// Copyright (c) 2013-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.ComponentModel;
using System.Windows.Forms;
using SIL.LCModel.Utils;

namespace LanguageExplorer.LcmUi
{
#if RANDYTODO
// TODO: Why are there two ProgressBarWrapper classes in the Flex repo?
// TODO: SIL.FieldWorks.Common.Controls.ProgressBarWrapper : IProgressDisplayer (FwControls)
// TODO: LanguageExplorer.LcmUi.ProgressBarWrapper : IProgress (LanguageExplorer)
#endif
	/// <summary>
	/// Wrapper class to allow a ProgressBar to function as an IProgress
	/// </summary>
	public class ProgressBarWrapper : IProgress
	{
		/// <summary>
		/// Gets the wrapped ProgressBar
		/// </summary>
		public ProgressBar ProgressBar { get; }

		/// <summary>
		/// Constructor which passes in the progressBar to wrap
		/// </summary>
		/// <param name="progressBar"></param>
		public ProgressBarWrapper(ProgressBar progressBar)
		{
			ProgressBar = progressBar;
		}

		#region IProgress implementation
		/// <summary>
		/// Event handler for listening to whether or the cancel button is pressed.
		/// </summary>
		public event CancelEventHandler Canceling;

		/// <summary>
		/// Cause the progress indicator to advance by the specified amount.
		/// </summary>
		/// <param name="amount">Amount of progress.</param>
		public void Step(int amount)
		{
			int stepSizeHold = StepSize;
			StepSize = amount;
			ProgressBar.PerformStep();
			StepSize = stepSizeHold;

			if (Canceling != null)
			{
				// don't do anything -- this just shuts up the compiler about the
				// event handler never being used.
			}
		}

		/// <summary>
		/// Get/Set the title of the progress display window.
		/// </summary>
		public string Title { get; set; }

		/// <summary>
		/// Get/Set the message within the progress display window.
		/// </summary>
		public string Message { get; set; }

		/// <summary>
		/// Gets or sets the current position of the progress bar. This should be within the limits set by
		/// SetRange, or returned by GetRange.
		/// </summary>
		public int Position
		{
			get { return ProgressBar.Value; }
			set { ProgressBar.Value = value; }
		}

		/// <summary>
		/// Gets or sets the size of the step increment used by Step.
		/// </summary>
		public int StepSize
		{
			get { return ProgressBar.Step; }
			set { ProgressBar.Step = value; }
		}

		/// <summary>
		/// Gets or sets the minimum value of the progress bar.
		/// </summary>
		public int Minimum
		{
			get { return ProgressBar.Minimum; }
			set { ProgressBar.Minimum = value; }
		}
		/// <summary>
		/// Gets or sets the maximum value of the progress bar.
		/// </summary>
		public int Maximum
		{
			get { return ProgressBar.Maximum; }
			set { ProgressBar.Maximum = value; }
		}

		/// <summary>
		/// Gets an object to be used for ensuring that required tasks are invoked on the main
		/// UI thread.
		/// </summary>
		public ISynchronizeInvoke SynchronizeInvoke => ProgressBar;

		/// <summary>
		/// Gets the form displaying the progress (used for message box owners, etc). If the progress
		/// is not associated with a visible Form, then this returns its owning form, if any.
		/// </summary>
		public Form Form { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this progress is indeterminate.
		/// </summary>
		public bool IsIndeterminate
		{
			get { return ProgressBar.Style == ProgressBarStyle.Marquee; }
			set { ProgressBar.Style = value ? ProgressBarStyle.Marquee : ProgressBarStyle.Continuous; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether the opertation executing on the separate thread
		/// can be cancelled by a different thread (typically the main UI thread).
		/// </summary>
		public bool AllowCancel
		{
			get { return false; }
			set { }
		}
		#endregion
	}
}
