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
using ProgressBarStyle = SIL.FieldWorks.Common.FwUtils.ProgressBarStyle;

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
		#region Events
		event CancelEventHandler IProgress.Canceling
		{
			add {  }
			remove {  }
		}
		#endregion

		#region Data members
		private delegate void SetStringPropDelegate(string value);
		private delegate void SetAssemblyPropDelegate(Assembly value);
		private delegate string GetStringPropDelegate();
		private delegate void SetIntPropDelegate(int value);
		private delegate int GetIntPropDelegate();

		private bool m_DisplaySILInfo;
		private bool m_fNoUi;
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
		/// <param name="fDisplaySILInfo">if set to <c>false</c>, any SIL-identifying information
		/// will be hidden.</param>
		/// <param name="fNoUi">if set to <c>true</c> no UI is to be shown (i.e., we aren't
		/// really going to show the splash screen).</param>
		/// ------------------------------------------------------------------------------------
		public void Show(bool fDisplaySILInfo, bool fNoUi)
		{
			if (m_thread != null)
				return;

			m_DisplaySILInfo = fDisplaySILInfo;
			m_fNoUi = fNoUi;
			m_waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);

#if __MonoCS__
			// mono winforms can't create items not on main thread.
			StartSplashScreen(); // Create Modeless dialog on Main GUI thread
#else
			if (fNoUi)
				StartSplashScreen();
			else
			{
				m_thread = new Thread(StartSplashScreen);
				m_thread.IsBackground = true;
				m_thread.SetApartmentState(ApartmentState.STA);
				m_thread.Name = "SplashScreen";
				// Copy the UI culture from the main thread to the splash screen thread.
				m_thread.CurrentUICulture = Thread.CurrentThread.CurrentUICulture;
				m_thread.Start();
				m_waitHandle.WaitOne();
			}
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

		#region IProgress Members
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

		/// <summary>
		/// Gets or sets the minimum value of the progress bar.
		/// </summary>
		/// <value>The minimum.</value>
		public int Minimum
		{
			get
			{
				lock (m_splashScreen.m_Synchronizer)
				{
					GetIntPropDelegate minMethod = () => m_splashScreen.Minimum;
					return (int)m_splashScreen.Invoke(minMethod);
				}
			}
			set
			{
				lock (m_splashScreen.m_Synchronizer)
				{
					SetIntPropDelegate minMethod = delegate(int min) { m_splashScreen.Minimum = min; };
					m_splashScreen.Invoke(minMethod, value);
				}
			}
		}

		/// <summary>
		/// Gets or sets the maximum value of the progress bar.
		/// </summary>
		/// <value>The maximum.</value>
		public int Maximum
		{
			get
			{
				lock (m_splashScreen.m_Synchronizer)
				{
					GetIntPropDelegate maxMethod = () => m_splashScreen.Maximum;
					return (int) m_splashScreen.Invoke(maxMethod);
				}
			}
			set
			{
				lock (m_splashScreen.m_Synchronizer)
				{
					SetIntPropDelegate maxMethod = delegate(int max) { m_splashScreen.Maximum = max; };
					m_splashScreen.Invoke(maxMethod, value);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The message to display to indicate startup activity on the splash screen
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public string Message
		{
			get
			{
				lock (m_splashScreen.m_Synchronizer)
				{
					GetStringPropDelegate method = () => m_splashScreen.Message;
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
		public int Position
		{
			get
			{
				lock (m_splashScreen.m_Synchronizer)
				{
					GetIntPropDelegate method = () => m_splashScreen.Position;
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
		public int StepSize
		{
			get
			{
				lock (m_splashScreen.m_Synchronizer)
				{
					GetIntPropDelegate method = () => m_splashScreen.StepSize;
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
		public string Title
		{
			get { throw new Exception("The property 'Title' is not implemented."); }
			set {  }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the progress as a form (used for message box owners, etc).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Form Form
		{
			get { return m_splashScreen; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the style of the ProgressBar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ProgressBarStyle ProgressBarStyle
		{
			get
			{
				if (m_splashScreen.InvokeRequired)
					return (ProgressBarStyle)m_splashScreen.Invoke((Func<ProgressBarStyle>)(() => m_splashScreen.ProgressBarStyle));
				return m_splashScreen.ProgressBarStyle;
			}
			set
			{
				if (m_splashScreen.InvokeRequired)
					m_splashScreen.Invoke((Action<ProgressBarStyle>)(style => m_splashScreen.ProgressBarStyle = style), value);
				else
					m_splashScreen.ProgressBarStyle = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether the opertation executing on the separate thread
		/// can be cancelled by a different thread (typically the main UI thread).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool AllowCancel
		{
			get { return false; }
			set { throw new NotImplementedException(); }
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
			m_splashScreen = new RealSplashScreen(m_DisplaySILInfo);
			m_splashScreen.WaitHandle = m_waitHandle;
			if (m_fNoUi)
			{
				IntPtr blah = m_splashScreen.Handle; // force handle creation.
			}
			else
			{
#if !__MonoCS__
				m_splashScreen.ShowDialog();
#else
			// Mono Winforms can't create Forms that are not on the Main thread.
			m_splashScreen.CreateControl();
			m_splashScreen.Message = string.Empty;
			m_splashScreen.Show();
#endif
			}
		}
		#endregion
	}
	#endregion
}
