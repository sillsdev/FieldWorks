// --------------------------------------------------------------------------------------------
#region // Copyright  2002-2004, SIL International. All Rights Reserved.
// <copyright from='2002' to='2004' company='SIL International'>
//		Copyright  2002-2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwSplashScreen.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.FwCoreDlgs
{
	#region FwSplashScreen implementation
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// FW Splash Screen
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FwSplashScreen : IProgress, IDisposable
	{
		#region Data members
		private delegate void SetStringPropDelegate(string value);
		private delegate void SetAssemblyPropDelegate(Assembly value);
		private delegate string GetStringPropDelegate();
		private delegate void SetIntPropDelegate(int value);
		private delegate int GetIntPropDelegate();

		private Thread m_thread;
		private RealSplashScreen m_splashScreen;
		internal EventWaitHandle m_waitHandle;
		#endregion

		#region Disposable stuff
		#if DEBUG
		/// <summary/>
		~FwSplashScreen()
		{
			Dispose(false);
		}
		#endif

		/// <summary/>
		public bool IsDisposed { get; private set; }

		/// <summary/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		protected virtual void Dispose(bool fDisposing)
		{
			Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType() + ". ****** ");
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects
				Close();
				var disposable = m_waitHandle as IDisposable;
				if (disposable != null)
					disposable.Dispose();
			}
			m_waitHandle = null;
			IsDisposed = true;
		}
		#endregion

		#region Public Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the splash screen
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Show()
		{
			if (m_thread != null)
				return;

			m_waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);

#if __MonoCS__
			// mono winforms can't create items not on main thread.
			StartSplashScreen(); // Create Modeless dialog on Main GUI thread
#else
			m_thread = new Thread(StartSplashScreen);
			m_thread.IsBackground = true;
			m_thread.SetApartmentState(ApartmentState.STA);
			m_thread.Name = "SplashScreen";
			// Copy the UI culture from the main thread to the splash screen thread.
			m_thread.CurrentUICulture = Thread.CurrentThread.CurrentUICulture;
			m_thread.Start();
			m_waitHandle.WaitOne();
#endif

			Debug.Assert(m_splashScreen != null);
			lock (m_splashScreen.m_Synchronizer)
			{
				m_splashScreen.Invoke(new SetAssemblyPropDelegate(m_splashScreen.SetProductExecutableAssembly), ProductExecutableAssembly);
			}
			Message = string.Empty;
		}

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Activates (brings back to the top) the splash screen (assuming it is already visible
		/// and the application showing it is the active application).
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		public void Activate()
		{
			Debug.Assert(m_splashScreen != null);
			lock (m_splashScreen.m_Synchronizer)
			{
				m_splashScreen.Invoke(new MethodInvoker(m_splashScreen.Activate));
			}
		}

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Closes the splash screen
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		public void Close()
		{
			if (m_splashScreen == null)
				return;

			lock (m_splashScreen.m_Synchronizer)
			{
				try
				{
					m_splashScreen.Invoke(new MethodInvoker(m_splashScreen.RealClose));
				}
				catch
				{
					// Something bad happened, but we are closing anyways :)
				}
			}
#if !__MonoCS__
			if (m_thread != null)
				m_thread.Join();
#endif
			lock (m_splashScreen.m_Synchronizer)
			{
				m_splashScreen.Dispose();
				m_splashScreen = null;
			}

#if !__MonoCS__
			m_thread = null;
#endif
		}

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Refreshes the display of the splash screen
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		public void Refresh()
		{
			Debug.Assert(m_splashScreen != null);
			lock (m_splashScreen.m_Synchronizer)
			{
				m_splashScreen.Invoke(new MethodInvoker(m_splashScreen.Refresh));
			}
		}
		#endregion

		#region Public properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The assembly of the product-specific EXE (e.g., TE.exe or FLEx.exe).
		/// .Net callers should set this.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Assembly ProductExecutableAssembly { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the progress bar.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public IProgress ProgressBar
		{
			get { return this; }
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The message to display to indicate startup activity on the splash screen
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public string Message
		{
			set { ((IProgress)this).Message = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets a Position
		/// </summary>
		/// <value></value>
		/// <returns>A System.Int32</returns>
		/// ------------------------------------------------------------------------------------
		public int Position
		{
			set { ((IProgress)this).Position = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Member Step
		/// </summary>
		/// <param name="nStepAmt">nStepAmt</param>
		/// ------------------------------------------------------------------------------------
		public void Step(int nStepAmt)
		{
			lock (m_splashScreen.m_Synchronizer)
			{
				m_splashScreen.Invoke(new SetIntPropDelegate(m_splashScreen.Step),
					nStepAmt);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets a StepSize
		/// </summary>
		/// <value></value>
		/// <returns>A System.Int32</returns>
		/// ------------------------------------------------------------------------------------
		public int StepSize
		{
			set { ((IProgress)this).StepSize = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets a Title
		/// </summary>
		/// <value></value>
		/// <returns>A System.String</returns>
		/// ------------------------------------------------------------------------------------
		public string Title
		{
			set { ((IProgress)this).Title = value; }
		}

		#region IProgress Members

		/// <summary>
		/// Gets or sets the minimum value of the progress bar.
		/// </summary>
		/// <value>The minimum.</value>
		int IProgress.Minimum
		{
			get
			{
				lock (m_splashScreen.m_Synchronizer)
				{
					GetIntPropDelegate minMethod = delegate() { return m_splashScreen.Min; };
					return (int)m_splashScreen.Invoke(minMethod);
				}
			}
			set
			{
				lock (m_splashScreen.m_Synchronizer)
				{
					SetIntPropDelegate minMethod = delegate(int min) { m_splashScreen.Min = min; };
					m_splashScreen.Invoke(minMethod, value);
				}
			}
		}

		/// <summary>
		/// Gets or sets the maximum value of the progress bar.
		/// </summary>
		/// <value>The maximum.</value>
		int IProgress.Maximum
		{
			get
			{
				lock (m_splashScreen.m_Synchronizer)
				{
					GetIntPropDelegate maxMethod = delegate() { return m_splashScreen.Max; };
					return (int) m_splashScreen.Invoke(maxMethod);
				}
			}
			set
			{
				lock (m_splashScreen.m_Synchronizer)
				{
					SetIntPropDelegate maxMethod = delegate(int max) { m_splashScreen.Max = max; };
					m_splashScreen.Invoke(maxMethod, value);
				}
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether the task has been canceled.
		/// </summary>
		/// <value><c>true</c> if canceled; otherwise, <c>false</c>.</value>
		bool IProgress.Canceled
		{
			get { return false; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The message to display to indicate startup activity on the splash screen
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		string IProgress.Message
		{
			get
			{
				lock (m_splashScreen.m_Synchronizer)
				{
					GetStringPropDelegate method = delegate() { return m_splashScreen.Message; };
					return (string)m_splashScreen.Invoke(method);
				}
			}
			set
			{
				lock (m_splashScreen.m_Synchronizer)
				{
					SetStringPropDelegate setMethod = delegate(string val) { m_splashScreen.Message = val; };
					m_splashScreen.Invoke(setMethod, value);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the current position of the progress bar. This should be within the limits set by
		/// SetRange. If it is not, then the value is set to either the minimum or the maximum.
		/// </summary>
		/// <value></value>
		/// <returns>A System.Int32 </returns>
		/// ------------------------------------------------------------------------------------
		int IProgress.Position
		{
			get
			{
				lock (m_splashScreen.m_Synchronizer)
				{
					GetIntPropDelegate method = delegate() { return m_splashScreen.Position; };
					return (int)m_splashScreen.Invoke(method);
				}
			}
			set
			{
				lock (m_splashScreen.m_Synchronizer)
				{
					SetIntPropDelegate method = delegate(int val) { m_splashScreen.Position = val; };
					m_splashScreen.Invoke(method, value);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the size of the step increment used by Step.
		/// </summary>
		/// <value></value>
		/// <returns>A System.Int32 </returns>
		/// ------------------------------------------------------------------------------------
		int IProgress.StepSize
		{
			get
			{
				lock (m_splashScreen.m_Synchronizer)
				{
					GetIntPropDelegate method = delegate() { return m_splashScreen.StepSize; };
					return (int)m_splashScreen.Invoke(method);
				}
			}
			set
			{
				lock (m_splashScreen.m_Synchronizer)
				{
					SetIntPropDelegate method = delegate(int val) { m_splashScreen.StepSize = val; };
					m_splashScreen.Invoke(method, value);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the title of the progress display window.
		/// </summary>
		/// <value></value>
		/// <returns>A System.String </returns>
		/// ------------------------------------------------------------------------------------
		string IProgress.Title
		{
			get { throw new Exception("The property 'Title' is not implemented."); }
			set {  }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the progress as a form (used for message box owners, etc).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		Form IProgress.Form
		{
			get { return m_splashScreen; }
		}
		#endregion

		#region private methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Starts the splash screen.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void StartSplashScreen()
		{
			m_splashScreen = new RealSplashScreen();
			m_splashScreen.RealShow(m_waitHandle);
#if !__MonoCS__
			m_splashScreen.ShowDialog();
#else
			// Mono Winforms can't create Forms that are not on the Main thread.
			m_splashScreen.CreateControl();
			m_splashScreen.Message = string.Empty;
			m_splashScreen.Show();
#endif
		}
		#endregion
	}
	#endregion
}
