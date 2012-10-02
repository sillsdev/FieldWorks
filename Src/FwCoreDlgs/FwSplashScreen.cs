// --------------------------------------------------------------------------------------------
#region // Copyright © 2002-2004, SIL International. All Rights Reserved.
// <copyright from='2002' to='2004' company='SIL International'>
//		Copyright © 2002-2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwSplashScreen.cs
// Responsibility: TE Team
// Last reviewed:
//
// <remarks>
// Splash Screen
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Drawing;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.FwCoreDlgs
{
	#region IFwSplashScreen interface
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Public interface (exported with COM wrapper) for the FW splash screen
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[ComVisible(true)]
	[GuidAttribute("8B6525EE-06A7-4dc7-964E-27E0CDE8D528")]
	public interface IFwSplashScreen
	{
		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the splash screen
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		void Show();

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Activates (brings back to the top) the splash screen (assuming it is already visible
		/// and the application showing it is the active application).
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		void Activate();

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Closes the splash screen
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		void Close();

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Refreshes the display of the splash screen
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		void Refresh();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The product name which appears in the Name label on the splash screen
		/// </summary>
		/// <remarks>
		/// .Net clients should not set this. It will be ignored.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		string ProdName
		{
			set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The product version which appears in the App Version label on the splash screen
		/// </summary>
		/// <remarks>
		/// .Net clients should not set this. It will be ignored.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		string ProdVersion
		{
			set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The integer portion of the "OLE Automation Date" for the product.  This is from the
		/// fourth (and final) field of the product version.
		/// </summary>
		/// <remarks>
		/// .Net clients should not set this. It will be ignored.  C++ clients should set this
		/// before they set ProdVersion.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		int ProdOADate
		{
			set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The product version which appears in the FW Version label on the splash screen
		/// </summary>
		/// <remarks>
		/// .Net clients should not set this. It will be ignored.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		string FieldworksVersion
		{
			set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The message to display to indicate startup activity on the splash screen
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string Message
		{
			set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the progress bar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IAdvInd3 ProgressBar
		{
			get;
		}
	}
	#endregion

	#region FwSplashScreen implementation
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// FW Splash Screen
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[ProgId("FwCoreDlgs.FwSplashScreen")]
	// Key attribute to hide the "clutter" from System.Windows.Forms.Form
	[ClassInterface(ClassInterfaceType.None)]
	[GuidAttribute("2BACF70E-9B19-493f-8E1D-99BC5B8B5170")]
	[ComVisible(true)]
	public class FwSplashScreen : IFwSplashScreen, IAdvInd3, IAdvInd4
	{
		#region Data members
		private delegate void MethodWithFormDelegate(Form value);
		private delegate void SetStringPropDelegate(string value);
		private delegate string GetStringPropDelegate();
		private delegate void SetIntPropDelegate(int value);
		private delegate int GetIntPropDelegate();
		private delegate Icon GetIconPropDelegate();

		private Thread m_thread;
		private RealSplashScreen m_splashScreen;
		private string m_Sync = string.Empty;
		internal EventWaitHandle m_waitHandle;
		/// <summary>Hidden form that gets created on the main thread</summary>
		private Form m_hiddenForm;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Default Constructor for FwSplashScreen
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwSplashScreen()
		{
		}
		#endregion

		#region Public Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the splash screen
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void IFwSplashScreen.Show()
		{
			if (m_thread != null)
				return;

			// Create a hidden form on the main thread so that we have a form in
			// Application.OpenForms on the right thread. We need this so that the COM objects
			// get created on the right thread and so that the main window will become activated
			// when the splash screen closes.
			m_hiddenForm = new Form();
			m_hiddenForm.ShowInTaskbar = false;
			m_hiddenForm.Opacity = 0;
			m_hiddenForm.StartPosition = FormStartPosition.CenterScreen;
			m_hiddenForm.WindowState = FormWindowState.Minimized;
			// Set same icon as splash screen so that we show something useful when the user alt-tabs.
			ComponentResourceManager resources = new ComponentResourceManager(typeof(RealSplashScreen));
			m_hiddenForm.Icon = resources.GetObject("$this.Icon") as Icon;
			m_hiddenForm.Show();

			m_waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);

			m_thread = new Thread(new ThreadStart(StartSplashScreen));
			m_thread.IsBackground = true;
			m_thread.SetApartmentState(ApartmentState.STA);
			m_thread.Name = "SplashScreen";
			// Copy the UI culture from the main thread to the splash screen thread.
			m_thread.CurrentUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
			m_thread.Start();
			m_waitHandle.WaitOne();
			Message = string.Empty;
		}

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Activates (brings back to the top) the splash screen (assuming it is already visible
		/// and the application showing it is the active application).
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		void IFwSplashScreen.Activate()
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
		void IFwSplashScreen.Close()
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
			m_thread.Join();
			lock (m_splashScreen.m_Synchronizer)
			{
				m_splashScreen.Dispose();
				m_splashScreen = null;
			}

			m_hiddenForm.Close();
			m_thread = null;
		}

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Refreshes the display of the splash screen
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		void IFwSplashScreen.Refresh()
		{
			Debug.Assert(m_splashScreen != null);
			lock (m_splashScreen.m_Synchronizer)
			{
				m_splashScreen.Invoke(new MethodInvoker(m_splashScreen.Refresh));
			}
		}

		#endregion

		#region Public Properties needed for all clients
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The message to display to indicate startup activity on the splash screen
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string IFwSplashScreen.Message
		{
			set { ((IAdvInd4)this).Message = value; }
		}
		#endregion

		#region Public properties set automatically in constructor for .Net apps
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The product name which appears in the Name label on the splash screen
		/// </summary>
		/// <remarks>
		/// .Net clients should not set this. It will be ignored. They should set the
		/// AssemblyTitle attribute in AssemblyInfo.cs of the executable.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		string IFwSplashScreen.ProdName
		{
			set
			{
				Debug.Assert(m_splashScreen != null);
				lock (m_splashScreen.m_Synchronizer)
				{
					m_splashScreen.Invoke(new SetStringPropDelegate(m_splashScreen.SetProdName), value);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The product version which appears in the App Version label on the splash screen
		/// </summary>
		/// <remarks>
		/// .Net clients should not set this. It will be ignored. They should set the
		/// AssemblyFileVersion attribute in AssemblyInfo.cs of the executable.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		string IFwSplashScreen.ProdVersion
		{
			set
			{
				Debug.Assert(m_splashScreen != null);
				lock (m_splashScreen.m_Synchronizer)
				{
					m_splashScreen.Invoke(new SetStringPropDelegate(m_splashScreen.SetProdVersion), value);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The integer portion of the "OLE Automation Date" for the product.  This is from the
		/// fourth (and final) field of the product version.
		/// </summary>
		/// <remarks>
		/// .Net clients should not set this. It will be ignored.  C++ clients should set this
		/// before they set ProdVersion.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		int IFwSplashScreen.ProdOADate
		{
			set
			{
				Debug.Assert(m_splashScreen != null);
				lock (m_splashScreen.m_Synchronizer)
				{
					DateTime dt = DateTime.FromOADate(value);
					string sDate = dt.ToString("yyyy/MM/dd");
					m_splashScreen.Invoke(new SetStringPropDelegate(m_splashScreen.SetProdDate), sDate);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The fieldworks version which appears in the FW Version label on the splash screen
		/// </summary>
		/// <remarks>
		/// .Net clients should not set this. It will be ignored. They should set the
		/// AssemblyInformationalVersionAttribute attribute in AssemblyInfo.cs of the executable.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		string IFwSplashScreen.FieldworksVersion
		{
			set
			{
				Debug.Assert(m_splashScreen != null);
				lock (m_splashScreen.m_Synchronizer)
				{
					m_splashScreen.Invoke(new SetStringPropDelegate(m_splashScreen.SetFieldworksVersion), value);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the progress bar.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		IAdvInd3 IFwSplashScreen.ProgressBar
		{
			get { return this; }
		}
		#endregion

		#region IAdvInd3 Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The message to display to indicate startup activity on the splash screen
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public string Message
		{
			set { ((IAdvInd4)this).Message = value; }
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
			set { ((IAdvInd4)this).Position = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Member SetRange
		/// </summary>
		/// <param name="nMin">nMin</param>
		/// <param name="nMax">nMax</param>
		/// ------------------------------------------------------------------------------------
		public void SetRange(int nMin, int nMax)
		{
			lock (m_splashScreen.m_Synchronizer)
			{
				SetIntPropDelegate minMethod = delegate(int min) { m_splashScreen.Min = min; };
				m_splashScreen.Invoke(minMethod, nMin);
				SetIntPropDelegate maxMethod = delegate(int max) { m_splashScreen.Max = max; };
				m_splashScreen.Invoke(maxMethod, nMax);
			}
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
			set { ((IAdvInd4)this).StepSize = value; }
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
			set { ((IAdvInd4)this).Title = value; }
		}

		#endregion

		#region IAdvInd4 Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the minimum and maximum values of the progress bar.
		/// </summary>
		/// <param name="nMin"></param>
		/// <param name="nMax"></param>
		/// ------------------------------------------------------------------------------------
		public void GetRange(out int nMin, out int nMax)
		{
			lock (m_splashScreen.m_Synchronizer)
			{
				GetIntPropDelegate minMethod = delegate() { return m_splashScreen.Min; };
				nMin = (int)m_splashScreen.Invoke(minMethod);
				GetIntPropDelegate maxMethod = delegate() { return m_splashScreen.Max; };
				nMax = (int)m_splashScreen.Invoke(maxMethod);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The message to display to indicate startup activity on the splash screen
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		string IAdvInd4.Message
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
		int IAdvInd4.Position
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
		int IAdvInd4.StepSize
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
		string IAdvInd4.Title
		{
			get { throw new Exception("The property 'Title' is not implemented."); }
			set {  }
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
			m_splashScreen.ShowDialog();
		}
		#endregion
	}
	#endregion
}
