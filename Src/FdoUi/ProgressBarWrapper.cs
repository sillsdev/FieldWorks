// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2013, SIL International. All Rights Reserved.
// <copyright from='2013' to='2013' company='SIL International'>
//		Copyright (c) 2013, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ProgressBarWrapper.cs
// ---------------------------------------------------------------------------------------------
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.FdoUi
{
	/// <summary>
	/// Wrapper class to allow a ProgressBar to function as an IProgress
	/// </summary>
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule", Justification="Disposable member is exposed for Dispose call if necessary")]
	public class ProgressBarWrapper : IProgress
	{
		private readonly ProgressBar m_progressBar;
		/// <summary>
		/// Gets the wrapped ProgressBar
		/// </summary>
		public ProgressBar ProgressBar
		{
			get { return m_progressBar; }
		}

		/// <summary>
		/// Constructor which passes in the progressBar to wrap
		/// </summary>
		/// <param name="progressBar"></param>
		public ProgressBarWrapper(ProgressBar progressBar)
		{
			m_progressBar = progressBar;
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
			m_progressBar.PerformStep();
			StepSize = stepSizeHold;

			if (Canceling != null)
			{
				// don't do anything -- this just shuts up the compiler about the
				// event handler never being used.
			}
		}

		/// <summary>
		/// Get the title of the progress display window.
		/// </summary>
		/// <value>The title.</value>
		public string Title { get; set; }  //ProgressBar.Text?

		/// <summary>
		/// Get the message within the progress display window.
		/// </summary>
		/// <value>The message.</value>
		public string Message { get; set; } //ProgressBar.Text?

		/// <summary>
		/// Gets or sets the current position of the progress bar. This should be within the limits set by
		/// SetRange, or returned by GetRange.
		/// </summary>
		/// <value>The position.</value>
		public int Position
		{
			get { return m_progressBar.Value; }
			set { m_progressBar.Value = value; }
		}

		/// <summary>
		/// Gets or sets the size of the step increment used by Step.
		/// </summary>
		/// <value>The size of the step.</value>
		public int StepSize
		{
			get { return m_progressBar.Step; }
			set { m_progressBar.Step = value; }
		}

		/// <summary>
		/// Gets or sets the minimum value of the progress bar.
		/// </summary>
		/// <value>The minimum.</value>
		public int Minimum
		{
			get { return m_progressBar.Minimum; }
			set { m_progressBar.Minimum = value; }
		}
		/// <summary>
		/// Gets or sets the maximum value of the progress bar.
		/// </summary>
		/// <value>The maximum.</value>
		public int Maximum
		{
			get { return m_progressBar.Maximum; }
			set { m_progressBar.Maximum = value; }
		}

		/// <summary>
		/// Gets an object to be used for ensuring that required tasks are invoked on the main
		/// UI thread.
		/// </summary>
		public ISynchronizeInvoke SynchronizeInvoke
		{
			get { return m_progressBar; }
		}

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
			get { return m_progressBar.Style == ProgressBarStyle.Marquee; }
			set { m_progressBar.Style = value ? ProgressBarStyle.Marquee : ProgressBarStyle.Continuous; }
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
