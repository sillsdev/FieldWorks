using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// This class is intended to be used with a text box (FwTextBox) to handle timing
	/// of user input and then doing some search function after either a pause or a
	/// change in the text box.
	/// </summary>
	public class SearchTimer
	{
		private readonly Timer m_timer;
		private bool m_fTimerTickSet;
		private readonly Control m_owningControl;
		private readonly int m_interval;
		private List<Control> m_controlsToDisable;

		private readonly Searcher m_searcher;
		/// <summary>
		/// Signature for a method to be run when the Timer elapses or the TextChanged event occurs.
		/// </summary>
		public delegate void Searcher();

		/// <summary>
		/// Creates a SearchTimer.
		/// </summary>
		/// <param name="owningControl">This control's cursor will be changed to WaitCursor while searching.</param>
		/// <param name="interval">Number of milliseconds to pause after user input before starting a search.</param>
		/// <param name="searcher">The delegate that will do the searching.</param>
		/// <param name="controlsToDisable">These controls will be disabled while the search is in progress.</param>
		public SearchTimer(Control owningControl, int interval, Searcher searcher, List<Control> controlsToDisable)
			: this(owningControl, interval, searcher)
		{
			m_controlsToDisable = controlsToDisable;
		}

		/// <summary>
		/// Creates a SearchTimer.
		/// </summary>
		/// <param name="owningControl">This control's cursor will be changed to WaitCursor while searching.</param>
		/// <param name="interval">Number of milliseconds to pause after user input before starting a search.</param>
		/// <param name="searcher">The delegate that will do the searching.</param>
		public SearchTimer(Control owningControl, int interval, Searcher searcher)
		{
			if (owningControl == null)
				throw new ArgumentNullException("owningControl");
			if (searcher == null)
				throw new ArgumentNullException("searcher");

			m_timer = new Timer();
			m_owningControl = owningControl;
			m_interval = interval;
			m_searcher = searcher;
		}

		/// <summary>
		/// When the timer interval elapses, this method runs the search function.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		protected virtual void TimerEventProcessor(object sender, EventArgs eventArgs)
		{
			var oldCursor = m_owningControl.Cursor;
			try
			{
				m_owningControl.Cursor = Cursors.WaitCursor;
				m_timer.Tick -= TimerEventProcessor;
				m_fTimerTickSet = false;
				DisableControls();
				m_searcher();
			}
			finally
			{
				EnableControls();
				m_owningControl.Cursor = oldCursor;
			}
		}

		private void EnableControls()
		{
			if (m_controlsToDisable != null)
			{
				foreach (var control in m_controlsToDisable)
					control.Enabled = true;
			}
		}

		private void DisableControls()
		{
			if (m_controlsToDisable != null)
			{
				foreach (var control in m_controlsToDisable)
					control.Enabled = false;
			}
		}

		/// <summary>
		/// This handler should be hooked up to the text box TextChanged event.
		/// When the user types in the text box, this arranges to call the original
		/// delegate method (from the constructor) that does the searching.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void OnSearchTextChanged(object sender, EventArgs e)
		{
			if (m_fTimerTickSet == false)
			{
				// Sets the timer interval
				m_timer.Interval = m_interval;
				m_timer.Start();
				m_fTimerTickSet = true;
				m_timer.Tick += new EventHandler(TimerEventProcessor);
			}
			else
			{
				m_timer.Stop();
				m_timer.Enabled = true;
			}
		}
	}
}
