// --------------------------------------------------------------------------------------------
#region // Copyright (c) 20045 SIL International. All Rights Reserved.
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
//	This code started from http://www.codeproject.com/csharp/PrettyGoodSplashScreen.asp.
//	Running the demo provided there is a great way to see what this is supposed to do.
//
//	I have separated out the predictive part into this progressState class, so that different processes could have
//	their own state (the original code was simply for launching an application).
//	Also, this progress state doesn't know anything about UI, so it could be used with any widget.
//	I've then written a progress bar widget which uses any progressState class.  -JH
//
//	future things to add to this:
//		It would help the prediction enormously if we added a "size factor" for each milestone; for example, if a list could include
//		in its side factor the number of items it was going to display, 10 after showing 50 items, then later when
//		we are showing 500 items, the times could be automatically scaled so that we get a good predictions.
//
//		We could add something so that we get a default prediction the first-time the user runs a task. This might include a "percent suggestion" for each milestone.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using Microsoft.Win32;
using System.Diagnostics;
using SIL.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// This is good to use when the named task is doing roughly the same amount of work each time,
	/// for example, launching an application.
	/// If the amount of work needed very is vary widely, it is better to use the dumber MilestoneProgressState.
	/// </summary>
	public class PredictiveProgressState : ProgressState
	{
		// Status and progress bar
		private double m_currentStepExpectedFractionOfTotal = 0;

		// Progress smoothing
		private double m_acumulatedFractionOfTotal = 0.0;
		private double m_currentStepStartFraction = 0.0;
		private int m_expectedTotalMilliSeconds = 0;
		private double m_currentStepExpectedMilliSeconds = 0;

		// Self-calibration support
		private DateTime m_startTime;
		private int m_stepIndex = -1;
		static string m_taskLabel;
		private double m_currentStepStartTime =0;

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
		/// <param name="progressBar"></param>
		/// <param name="applicationKey"></param>
		/// <param name="taskLabel"></param>
		public PredictiveProgressState(StatusBarProgressPanel progressBar, RegistryKey applicationKey, string taskLabel)
			: base(progressBar)
		{
			m_taskLabel = taskLabel;
		}

		/// <summary>
		///
		/// </summary>
		public override int PercentDone
		{
			get
			{
				CheckDisposed();

				return (int) (m_acumulatedFractionOfTotal * 100);
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
		/// <param name="newStatus"></param>
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

		// ************ Private methods ************

		// Internal method for setting Milestone points.
		private void SetMilestoneInternal()
		{
			if( m_stepIndex < 0 )
			{
				m_startTime = DateTime.Now;
				ReadIncrements();
			}
			else
				SaveStepInfo();

			++m_stepIndex;

			//move us up to the fraction of the progress bar according to what fraction of the time
			//this step to last time.
			m_acumulatedFractionOfTotal = m_currentStepExpectedFractionOfTotal;

			//now switch to the next step
			m_currentStepStartFraction += m_currentStepExpectedFractionOfTotal;
			m_currentStepStartTime = ElapsedMilliSeconds();

			//if we have seen the step before
			if( (1 + m_stepIndex) <= m_expectedStepDurations.Count )
			{
				m_currentStepExpectedMilliSeconds = m_expectedStepDurations[m_stepIndex];
				m_currentStepExpectedFractionOfTotal = m_currentStepExpectedMilliSeconds / m_expectedTotalMilliSeconds;
			}
			else
				m_currentStepExpectedFractionOfTotal = ( m_stepIndex > 0 )? 1: 0;

			//System.Diagnostics.Debug.WriteLine("*m_acumulatedFractionOfTotal="+m_acumulatedFractionOfTotal.ToString());
			//System.Diagnostics.Debug.WriteLine("m_currentStepStartFraction="+m_currentStepStartFraction.ToString());
			//System.Diagnostics.Debug.WriteLine("m_currentStepExpectedFractionOfTotal="+m_currentStepExpectedFractionOfTotal.ToString());
			//System.Diagnostics.Debug.WriteLine("m_currentStepExpectedMilliSeconds="+m_currentStepExpectedMilliSeconds.ToString());
			//System.Diagnostics.Debug.WriteLine("");

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
			TimeSpan ts = DateTime.Now - m_startTime;
			return ts.TotalMilliseconds;
		}

		private double ElapsedTimeDoingThisStep()
		{
			return ElapsedMilliSeconds()- m_currentStepStartTime;
		}

		// Function to read the checkpoint intervals from the previous invocation of the
		// splashscreen from the registry.
		private void ReadIncrements()
		{
			/*			string sPBIncrementPerTimerInterval = GetStringRegistryValue( REGVALUE_PB_MILISECOND_INCREMENT, m_taskLabel, "0.0015");
						double dblResult;
						if( Double.TryParse(sPBIncrementPerTimerInterval, System.Globalization.NumberStyles.Float, System.Globalization.NumberFormatInfo.InvariantInfo, out dblResult) == true )
							m_incrementPerTimerInterval = dblResult;
						else
							m_incrementPerTimerInterval = .0015;

						if(m_incrementPerTimerInterval > 0.2)//was stuck at a +infinity value
							m_incrementPerTimerInterval = 0.01;
			*/

			string s = GetStringRegistryValue( REGVALUE_PB_TOTAL_TIME, m_taskLabel, "2");
			double dblResult;
			if( Double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.NumberFormatInfo.InvariantInfo, out dblResult) == true )
				m_expectedTotalMilliSeconds  = (int)dblResult;
			else
				m_expectedTotalMilliSeconds = 2;

			string stepTimes = GetStringRegistryValue( REGVALUE_PB_STEP_DURATIONS, m_taskLabel, "" );

			m_expectedStepDurations = new List<double>();
			if( stepTimes != "" )
			{
				string [] aTimes = stepTimes.Split(null);

				for(int i = 0; i < aTimes.Length; i++ )
				{
					double dblVal;
					if( Double.TryParse(aTimes[i], System.Globalization.NumberStyles.Float, System.Globalization.NumberFormatInfo.InvariantInfo, out dblVal) )
						m_expectedStepDurations.Add(dblVal);
					else
						m_expectedStepDurations.Add(1.0);
				}
			}
		}

		// Method to store the intervals (in percent complete) from the current invocation of
		// the splash screen to the registry.
		private void StoreIncrements()
		{
			string stepTimes = "";
			double actualElapsedMilliseconds = ElapsedMilliSeconds();
			for( int i = 0; i < m_actualStepDurations.Count; i++ )
				stepTimes += m_actualStepDurations[i].ToString("0.####", System.Globalization.NumberFormatInfo.InvariantInfo) + " ";

			SetStringRegistryValue( REGVALUE_PB_STEP_DURATIONS, m_taskLabel, stepTimes );

			//m_incrementPerTimerInterval = 1.0/(double)m_actualTickCount;
			//SetStringRegistryValue( REGVALUE_PB_MILISECOND_INCREMENT, m_taskLabel, m_incrementPerTimerInterval.ToString("#.000000", System.Globalization.NumberFormatInfo.InvariantInfo));

			SetStringRegistryValue( REGVALUE_PB_TOTAL_TIME, m_taskLabel, actualElapsedMilliseconds.ToString("#.000000", System.Globalization.NumberFormatInfo.InvariantInfo));
		}

		/// <summary>
		///
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				SaveStepInfo();
				if(m_acumulatedFractionOfTotal < 1)
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
			CheckDisposed();

			//++m_actualTickCount;
			//System.Diagnostics.Debug.Write("/");
			if( /*m_bFirstLaunch == false && */ m_acumulatedFractionOfTotal < (m_acumulatedFractionOfTotal+m_currentStepExpectedFractionOfTotal ))
			{
				//				m_acumulatedFractionOfTotal += m_incrementPerTimerInterval;
				double fractionOfThisStep=(ElapsedTimeDoingThisStep()/m_currentStepExpectedMilliSeconds);
				if (fractionOfThisStep >= 1.0)
					fractionOfThisStep = 1.0;//it is taking longer this time
				//System.Diagnostics.Debug.WriteLine("fractionOfThisStep="+fractionOfThisStep.ToString());
				m_acumulatedFractionOfTotal = m_currentStepStartFraction + (fractionOfThisStep*m_currentStepExpectedFractionOfTotal);
			}
			base.Breath();

			//System.Diagnostics.Debug.WriteLine("**m_acumulatedFractionOfTotal="+m_acumulatedFractionOfTotal.ToString());
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="key"></param>
		/// <param name="sub"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		private static string GetStringRegistryValue(string key, string sub, string defaultValue)
		{
			using (var rkApplication = RegistryHelper.CompanyKey.CreateSubKey(sub))
			{
				if (rkApplication != null)
					return (string)rkApplication.GetValue(key, defaultValue);
				return defaultValue;
			}
		}


		/// <summary>
		///
		/// </summary>
		/// <param name="key"></param>
		/// <param name="sub"></param>
		/// <param name="stringValue"></param>
		private static void SetStringRegistryValue(string key, string sub, string stringValue)
		{
			using (var rkApplication = RegistryHelper.CompanyKey.CreateSubKey(sub))
			{
				if (rkApplication != null)
					rkApplication.SetValue(key, stringValue);
			}
		}
	}
}
