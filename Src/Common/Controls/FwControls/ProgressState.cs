// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2005' to='2005' company='SIL International'>
//		Copyright (c) 2005, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: PredictiveProgressState.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
//	This class may not actually get used directly, although it should work. The primary use will be
//	through the subclass PredictiveProgressState. Anyways, this is used with StatusBarProgressPanel.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Windows.Forms;

using SIL.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary/>
	public class ProgressState : IFWDisposable
	{
		/// <summary/>
		protected int m_percentDone;

		private string m_status;

		/// <summary/>
		protected IProgressDisplayer m_progressBar;

		private Cursor m_previousCursor;

		/// <summary/>
		public ProgressState(IProgressDisplayer progressBar)
		{
			m_previousCursor = Cursor.Current;
			Cursor.Current = Cursors.WaitCursor;

			m_percentDone = 0;
			m_progressBar = progressBar;
			if (m_progressBar != null)
				m_progressBar.SetStateProvider(this);
		}

		#region IDisposable & Co. implementation
		// Region last reviewed: Oct. 16, 2005: RandyR.

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~ProgressState()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected virtual void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			Cursor.Current = m_previousCursor;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_progressBar != null)
				{
					m_progressBar.ClearStateProvider();
					//m_progressBar.Dispose(); // We don't own this!! (JohnT)
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_progressBar = null;
			m_status = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		/// <summary/>
		public virtual void SetMilestone(string newStatus)
		{
			CheckDisposed();

			m_status = newStatus;
		}

		/// <summary/>
		public virtual void SetMilestone(){}

		/// <summary>
		/// get some time to update to display or whatever
		/// </summary>
		public virtual void Breath()
		{
			if (IsDisposed)
				return; // harmless, if it's been disposed no longer report progress.

			m_progressBar.Refresh();
		}

		/// <summary>
		/// How much the task is done
		/// </summary>
		public virtual int PercentDone
		{
			get
			{
				return m_percentDone;
			}
			set
			{
				m_percentDone= value;
			}
		}

		/// <summary>
		/// a label which describes what we are busy doing
		/// </summary>
		public string Status
		{
			get
			{
				CheckDisposed();

				return m_status;
			}
		}
	}

	/// <summary>
	/// A fairly dumb progress state which knows how to divide progress into a set of milestones.
	/// </summary>
	public class MilestoneProgressState: ProgressState
	{
		// Progress smoothing
		private double m_currentStepExpectedFractionOfTotal = 0;
		private double m_accumulatedFractionOfTotal = 0.0;
		private double m_currentStepStartFraction = 0.0;
		private int m_stepIndex = -1;
		private DateTime m_startTime;
		private int m_stepsCount=0;
		private double m_currentStepStartTime = 0;

		/// <summary>
		/// This progress state just knows enough to divide the task into a set of milestones
		/// </summary>
		public MilestoneProgressState(IProgressDisplayer progressBar):base(progressBar)
		{
		}

		/// <param name="relativeLength">the relative length of this milestone, in whatever units you want.</param>
		public void AddMilestone (float relativeLength)
		{
			CheckDisposed();

			m_stepsCount++;
		}

		/// <summary/>
		public override int PercentDone
		{
			get
			{
				CheckDisposed();

				return (int) (m_accumulatedFractionOfTotal * 100);
			}
			set
			{
				CheckDisposed();

				throw new NotImplementedException();
			}
		}

		/// <summary>
		/// mark a known point in the works that we're doing. kept these are used to get the predictive progress bar
		/// to both learn how long part of the work takes, as well draw correctly (hard to explain).
		/// </summary>
		public override void SetMilestone(string newStatus)
		{
			CheckDisposed();

			SetMilestoneInternal();
			base.SetMilestone(newStatus);
		}

		/// <summary>
		/// mark a known point in the works that we're doing. kept these are used to get the predictive progress bar
		/// to both learn how long part of the work takes, as well draw correctly (hard to explain).
		/// </summary>
		public override void SetMilestone()
		{
			CheckDisposed();

			SetMilestoneInternal();
		}

		private void SetMilestoneInternal()
		{
			if( m_stepIndex < 0 )
			{
				m_startTime = DateTime.Now;
			}

			//move us up to the fraction of the progress bar according to what fraction of the time
			//this step to last time.
			m_accumulatedFractionOfTotal = m_currentStepExpectedFractionOfTotal;

			//now switch to the next step
			m_stepIndex++;
			m_currentStepStartFraction += m_currentStepExpectedFractionOfTotal;
			m_currentStepStartTime = ElapsedMilliSeconds();

			m_currentStepExpectedFractionOfTotal =(1+ m_stepIndex) /(float)m_stepsCount;

			if (m_currentStepExpectedFractionOfTotal>1)//there were more steps than expected
				m_currentStepExpectedFractionOfTotal= 1;
		}


		// Utility function to return elapsed Milliseconds since the
		// SplashScreen was launched.
		private double ElapsedMilliSeconds()
		{
			TimeSpan ts = DateTime.Now - m_startTime;
			return ts.TotalMilliseconds;
		}

		/// <summary/>
		protected double ElapsedTimeDoingThisStep()
		{
			return ElapsedMilliSeconds()- m_currentStepStartTime;
		}

		/// <summary/>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				if (m_accumulatedFractionOfTotal < 1)
				{
					m_accumulatedFractionOfTotal = 1;
					m_progressBar.Refresh();
					System.Threading.Thread.Sleep(50);
				}
			}

			base.Dispose(disposing);
		}

		/// <summary>
		/// give some time to update to display or whatever
		/// </summary>
		public override void Breath()
		{
			CheckDisposed();

			//System.Diagnostics.Debug.Write("/");
			if(m_accumulatedFractionOfTotal < (m_accumulatedFractionOfTotal+m_currentStepExpectedFractionOfTotal ))
			{
				//just add 1 percent since we have no idea how many times Breathe()
				//will be called during this milestone
				m_accumulatedFractionOfTotal += 0.01; //TODO: this might be good to be based on how long it has been since we last breathed.

				//Review: this will currently allow us to go past what had been allocated for this milestone
			}
			base.Breath();

			//System.Diagnostics.Debug.WriteLine("**m_accumulatedFractionOfTotal="+m_accumulatedFractionOfTotal.ToString());
		}
	}

	/// <summary>
	/// use this when a colleague is expecting you to pass a progress state
	/// but you aren't in a position to create a real one.
	/// </summary>
	public class NullProgressState:ProgressState
	{
		/// <summary>
		/// just initializes the base class
		/// </summary>
		public NullProgressState():base(null)
		{
		}
		/// <summary>
		/// does nothing
		/// </summary>
		/// <param name="newStatus"></param>
		public override void SetMilestone(string newStatus){}
		/// <summary>
		/// does nothing
		/// </summary>
		public override void SetMilestone(){}

		/// <summary>
		/// does nothing
		/// </summary>
		public override void Breath(){}
	}

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
		/// <param name="state"></param>
		void SetStateProvider(ProgressState state);
		/// <summary>
		/// Inform the control that the PercentDone can no longer be obtained.
		/// </summary>
		void ClearStateProvider();
	}

	/// <summary>
	/// Wrapper class to allow a ProgressBar to function as the progress displayer of a ProgressState.
	/// The progress Bar's minimum and maximum will be set (to 0 and 100)
	/// </summary>
	public class ProgressBarWrapper : IProgressDisplayer
	{
		private ProgressBar m_progressBar;
		private ProgressState m_state;

		/// <summary>
		/// Make one.
		/// </summary>
		/// <param name="progressBar"></param>
		public ProgressBarWrapper(System.Windows.Forms.ProgressBar progressBar)
		{
			m_progressBar = progressBar;
			m_progressBar.Maximum = 100;
			m_progressBar.Minimum = 0;
		}

		#region IProgressDisplayer Members

		/// <summary>
		/// Make it display.
		/// </summary>
		public void Refresh()
		{
			int percentDone = 100;
			if (m_state != null)
				percentDone = m_state.PercentDone;
			m_progressBar.Value = Math.Min(percentDone, 100);
			m_progressBar.Update();
		}

		/// <summary>
		/// Remember where to get the state
		/// </summary>
		/// <param name="state"></param>
		public void SetStateProvider(ProgressState state)
		{
			m_state = state;
		}

		/// <summary>
		/// Stop retrieving state.
		/// </summary>
		public void ClearStateProvider()
		{
			m_state = null;
		}

		#endregion
	}
}
