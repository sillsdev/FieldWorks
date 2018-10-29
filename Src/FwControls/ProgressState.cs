// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary />
	public class ProgressState : IDisposable
	{
		/// <summary />
		protected IProgressDisplayer m_progressBar;
		private WaitCursor m_wait;

		/// <summary/>
		public ProgressState(IProgressDisplayer progressBar)
		{
			m_wait = new WaitCursor(Form.ActiveForm);

			PercentDone = 0;
			m_progressBar = progressBar;
			m_progressBar?.SetStateProvider(this);
		}

		/// <summary>
		/// factory method for getting a progress state which is already hooked up to the correct progress panel
		/// </summary>
		public static ProgressState CreatePredictiveProgressState(StatusBarProgressPanel panel, string taskLabel)
		{
			return panel == null ? (ProgressState)new NullProgressState() : new PredictiveProgressState(panel, taskLabel);
		}

		#region IDisposable & Co. implementation

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		private bool IsDisposed { get; set; }

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

		/// <inheritdoc />
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
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				// Dispose managed resources here.
				m_progressBar?.ClearStateProvider();
				if (m_wait != null)
				{
					m_wait.Dispose();
					m_wait = null;
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_progressBar = null;
			Status = null;

			IsDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		/// <summary />
		public virtual void SetMilestone(string newStatus)
		{
			Status = newStatus;
		}

		/// <summary />
		public virtual void SetMilestone() { }

		/// <summary>
		/// get some time to update to display or whatever
		/// </summary>
		public virtual void Breath()
		{
			if (IsDisposed)
			{
				return; // harmless, if it's been disposed no longer report progress.
			}
			m_progressBar.Refresh();
		}

		/// <summary>
		/// How much the task is done
		/// </summary>
		public virtual int PercentDone { get; set; }

		/// <summary>
		/// a label which describes what we are busy doing
		/// </summary>
		public string Status { get; private set; }

		/// <summary>
		/// This is good to use when the named task is doing roughly the same amount of work each time,
		/// for example, launching an application.
		/// If the amount of work needed very is vary widely, it is better to use the dumber MilestoneProgressState.
		/// </summary>
		/// <remarks>
		///	This code started from http://www.codeproject.com/csharp/PrettyGoodSplashScreen.asp.
		///	Running the demo provided there is a great way to see what this is supposed to do.
		///
		///	I (JohnH) have separated out the predictive part into this progressState class, so that different processes could have
		///	their own state (the original code was simply for launching an application).
		///	Also, this progress state doesn't know anything about UI, so it could be used with any widget.
		///	I've then written a progress bar widget which uses any progressState class.  -JH
		///
		///	future things to add to this:
		///		It would help the prediction enormously if we added a "size factor" for each milestone; for example, if a list could include
		///		in its side factor the number of items it was going to display, 10 after showing 50 items, then later when
		///		we are showing 500 items, the times could be automatically scaled so that we get a good predictions.
		///
		///		We could add something so that we get a default prediction the first-time the user runs a task. This might include a "percent suggestion" for each milestone.
		/// </remarks>
		private sealed class PredictiveProgressState : ProgressState
		{
			// Status and progress bar
			private double m_currentStepExpectedFractionOfTotal;
			// Progress smoothing
			private double m_acumulatedFractionOfTotal;
			private double m_currentStepStartFraction;
			private int m_expectedTotalMilliSeconds;
			private double m_currentStepExpectedMilliSeconds;
			// Self-calibration support
			private DateTime m_startTime;
			private int m_stepIndex = -1;
			static string m_taskLabel;
			private double m_currentStepStartTime;
			/// <summary>
			/// # of times timer called during total process
			/// </summary>
			private List<double> m_expectedStepDurations;
			private List<double> m_actualStepDurations = new List<double>();
			private const string REGVALUE_PB_TOTAL_TIME = "ExpectedTotalTime";
			private const string REGVALUE_PB_STEP_DURATIONS = "ExpectedStepDurations";

			/// <summary>
			/// This progress state tries to remember how long each milestone took last time, and
			/// thereby give a more smooth and accurate progress progression the next time, to the extent that
			/// the same amount of work is being done. The times taken last time are stored based on
			/// the task label parameter.
			/// </summary>
			public PredictiveProgressState(StatusBarProgressPanel progressBar, string taskLabel)
				: base(progressBar)
			{
				m_taskLabel = taskLabel;
			}

			/// <summary />
			public override int PercentDone
			{
				get
				{
					return (int)(m_acumulatedFractionOfTotal * 100);
				}
				set
				{
					throw new NotSupportedException();
				}
			}

			/// <inheritdoc />
			public override void SetMilestone(string newStatus)
			{
				SetMilestoneInternal();
				base.SetMilestone(newStatus);
			}

			/// <inheritdoc />
			public override void SetMilestone()
			{
				SetMilestoneInternal();
			}

			/// <summary>
			/// Method for setting Milestone points.
			/// </summary>
			private void SetMilestoneInternal()
			{
				if (m_stepIndex < 0)
				{
					m_startTime = DateTime.Now;
					ReadIncrements();
				}
				else
				{
					SaveStepInfo();
				}
				++m_stepIndex;

				//move us up to the fraction of the progress bar according to what fraction of the time
				//this step to last time.
				m_acumulatedFractionOfTotal = m_currentStepExpectedFractionOfTotal;

				//now switch to the next step
				m_currentStepStartFraction += m_currentStepExpectedFractionOfTotal;
				m_currentStepStartTime = ElapsedMilliSeconds();

				//if we have seen the step before
				if ((1 + m_stepIndex) <= m_expectedStepDurations.Count)
				{
					m_currentStepExpectedMilliSeconds = m_expectedStepDurations[m_stepIndex];
					m_currentStepExpectedFractionOfTotal = m_currentStepExpectedMilliSeconds / m_expectedTotalMilliSeconds;
				}
				else
				{
					m_currentStepExpectedFractionOfTotal = (m_stepIndex > 0) ? 1 : 0;
				}
			}

			private void SaveStepInfo()
			{
				//Save off current step
				m_actualStepDurations.Add(ElapsedTimeDoingThisStep());
			}

			// Utility function to return elapsed Milliseconds since the
			// SplashScreen was launched.
			private double ElapsedMilliSeconds()
			{
				var ts = DateTime.Now - m_startTime;
				return ts.TotalMilliseconds;
			}

			private double ElapsedTimeDoingThisStep()
			{
				return ElapsedMilliSeconds() - m_currentStepStartTime;
			}

			// Function to read the checkpoint intervals from the previous invocation of the
			// splashscreen from the registry.
			private void ReadIncrements()
			{
				var s = GetStringRegistryValue(REGVALUE_PB_TOTAL_TIME, m_taskLabel, "2");
				double dblResult;
				if (double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.NumberFormatInfo.InvariantInfo, out dblResult))
				{
					m_expectedTotalMilliSeconds = (int)dblResult;
				}
				else
				{
					m_expectedTotalMilliSeconds = 2;
				}

				var stepTimes = GetStringRegistryValue(REGVALUE_PB_STEP_DURATIONS, m_taskLabel, "");

				m_expectedStepDurations = new List<double>();
				if (stepTimes != "")
				{
					var aTimes = stepTimes.Split(null);
					foreach (var time in aTimes)
					{
						double dblVal;
						m_expectedStepDurations.Add(double.TryParse(time, System.Globalization.NumberStyles.Float, System.Globalization.NumberFormatInfo.InvariantInfo, out dblVal) ? dblVal : 1.0);
					}
				}
			}

			// Method to store the intervals (in percent complete) from the current invocation of
			// the splash screen to the registry.
			private void StoreIncrements()
			{
				var stepTimes = string.Empty;
				var actualElapsedMilliseconds = ElapsedMilliSeconds();
				foreach (var actualStepDuration in m_actualStepDurations)
				{
					stepTimes += actualStepDuration.ToString("0.####", System.Globalization.NumberFormatInfo.InvariantInfo) + " ";
				}
				SetStringRegistryValue(REGVALUE_PB_STEP_DURATIONS, m_taskLabel, stepTimes);
				SetStringRegistryValue(REGVALUE_PB_TOTAL_TIME, m_taskLabel, actualElapsedMilliseconds.ToString("#.000000", System.Globalization.NumberFormatInfo.InvariantInfo));
			}

			/// <inheritdoc />
			protected override void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
				if (IsDisposed)
				{
					// No need to run it more than once.
					return;
				}

				if (disposing)
				{
					SaveStepInfo();
					if (m_acumulatedFractionOfTotal < 1)
					{
						m_acumulatedFractionOfTotal = 1;
						m_progressBar.Refresh();
						System.Threading.Thread.Sleep(50);
					}
					StoreIncrements();
				}
				m_taskLabel = null;
				m_expectedStepDurations = null;
				m_actualStepDurations = null;

				base.Dispose(disposing);
			}

			/// <summary>
			/// give some time to update to display or whatever
			/// </summary>
			public override void Breath()
			{
				if (m_acumulatedFractionOfTotal < (m_acumulatedFractionOfTotal + m_currentStepExpectedFractionOfTotal))
				{
					var fractionOfThisStep = (ElapsedTimeDoingThisStep() / m_currentStepExpectedMilliSeconds);
					if (fractionOfThisStep >= 1.0)
					{
						fractionOfThisStep = 1.0;//it is taking longer this time
					}
					m_acumulatedFractionOfTotal = m_currentStepStartFraction + (fractionOfThisStep * m_currentStepExpectedFractionOfTotal);
				}
				base.Breath();
			}

			/// <summary />
			private static string GetStringRegistryValue(string key, string sub, string defaultValue)
			{
				using (var rkApplication = RegistryHelper.CompanyKey.CreateSubKey(sub))
				{
					if (rkApplication != null)
					{
						return (string)rkApplication.GetValue(key, defaultValue);
					}
					return defaultValue;
				}
			}

			/// <summary />
			private static void SetStringRegistryValue(string key, string sub, string stringValue)
			{
				using (var rkApplication = RegistryHelper.CompanyKey.CreateSubKey(sub))
				{
					rkApplication?.SetValue(key, stringValue);
				}
			}
		}
	}
}