using System.Windows.Forms;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// This is a progress reporting interface.
	/// </summary>
	public interface IProgress
	{
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
		/// Gets or sets a value indicating whether the task has been canceled.
		/// </summary>
		/// <value><c>true</c> if canceled; otherwise, <c>false</c>.</value>
		bool Canceled
		{
			get;
		}

		/// <summary>
		/// Gets the progress as a form (used for message box owners, etc).
		/// </summary>
		Form Form
		{
			get;
		}
	}
}
