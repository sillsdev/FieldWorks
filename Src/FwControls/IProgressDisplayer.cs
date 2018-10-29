// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// This class wraps the functionality that a ProgressState expects of the thing
	/// that actually displays the progress. Originally this was typically a StatusBarProgressPanel,
	/// but the need developed to support an ordinary ProgressBar as well.
	/// </summary>
	public interface IProgressDisplayer
	{
		/// <summary>
		/// Update the display of the control so that in indicates the current amount of
		/// progress, as determined by the state passed to SetStateProvider.
		/// </summary>
		void Refresh();

		/// <summary>
		/// Provide the object from which the PercentDone can be obtained.
		/// </summary>
		void SetStateProvider(ProgressState state);

		/// <summary>
		/// Inform the control that the PercentDone can no longer be obtained.
		/// </summary>
		void ClearStateProvider();
	}
}