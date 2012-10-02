// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ProgressDialogWithTask.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	#region Delegates
	/// <summary>Represents a delegate that can start a background task used in the
	/// progress dialog.</summary>
	/// <param name="progressDlg">The progress dialog</param>
	/// <param name="parameters">Parameters</param>
	/// <returns>The return value from the background task</returns>
	public delegate object BackgroundTaskInvoker(IAdvInd4 progressDlg, object[] parameters);

	/// <summary>
	/// Delegate for trapping the cancel event.
	/// </summary>
	public delegate void CancelHandler(object sender);
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// A progress dialog (with an optional cancel button) that displays while a task runs
	/// in the background.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ProgressDialogWithTask : IAdvInd3, IAdvInd4, IFWDisposable
	{
		#region Member variables
		private ProgressDialogImpl m_progressDialog;
		private volatile bool m_fDisposed;
		private BackgroundTaskInvoker m_backgroundTask;
		private object[] m_parameters;
		private object m_RetValue;
		private Exception m_Exception;
		private Form m_owner;
		private bool m_fDisplayUi;

		/// <summary>
		/// Event handler for listening to the cancel button getting pressed.
		/// </summary>
		public event CancelHandler Cancel;
		#endregion

		#region Delegate definitions
		private delegate string GetStringInvoker();
		private delegate void SetStringInvoker(string value);
		private delegate int GetIntInvoker();
		private delegate void SetIntInvoker(int value);
		private delegate bool GetBoolInvoker();
		private delegate void SetBoolInvoker(bool value);
		private delegate void GetIntIntInvoker(out int val1, out int val2);
		private delegate void SetIntIntInvoker(int val1, int val2);
		private delegate void EventInvoker(object sender, EventArgs e);
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:ProgressDialogWithTask"/> class.
		/// </summary>
		/// <param name="owner">The owner.</param>
		/// ------------------------------------------------------------------------------------
		public ProgressDialogWithTask(Form owner)
		{
			// The owner might be the splash screen which runs on a separate thread. In this
			// case we want to just ignore it.
			if (owner != null && !owner.InvokeRequired)
				m_owner = owner;
			m_progressDialog = new ProgressDialogImpl(m_owner);
			m_progressDialog.Shown += new EventHandler(OnProgressDialogShown);
			m_progressDialog.Cancel += new CancelHandler(OnCancel);
			m_progressDialog.VisibleChanged += new EventHandler(OnProgressDialogVisibleChanged);
		}
		#endregion

		#region Disposed stuff

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add the public property for knowing if the object has been disposed of yet
		/// </summary>
		/// <remarks>This property is thread safe.</remarks>
		/// ------------------------------------------------------------------------------------
		public bool IsDisposed
		{
			get { return m_fDisposed; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting
		/// unmanaged resources.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Releases unmanaged resources and performs other cleanup operations before the
		/// <see cref="T:SIL.FieldWorks.Common.Controls.ProgressDialogWithTask"/> is reclaimed by
		/// garbage collection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		~ProgressDialogWithTask()
		{
			Dispose(false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		/// <remarks>This method is thread safe.</remarks>
		/// ------------------------------------------------------------------------------------
		public void CheckDisposed()
		{
			if (IsDisposed)
			{
				throw new ObjectDisposedException(
					string.Format("'{0}' in use after being disposed.", GetType().Name));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="disposing"></param>
		/// ------------------------------------------------------------------------------------
		protected void Dispose(bool disposing)
		{
			if (disposing)
			{
				// TODO: Take this out when we use the multi-threaded progress dialog again
				if (m_owner != null)
					m_owner.Enabled = true;

				if (m_progressDialog != null)
				{
					if (m_progressDialog.InvokeRequired)
						m_progressDialog.BeginInvoke(new MethodInvoker(m_progressDialog.Dispose));
					else
						m_progressDialog.Dispose();
				}
			}

			m_progressDialog = null;
			m_Exception = null;
			m_owner = null;

			m_fDisposed = true;
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a message indicating progress status.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string StatusMessage
		{
			get
			{
				CheckDisposed();

				if (m_progressDialog.InvokeRequired)
				{
					GetStringInvoker method = delegate() { return m_progressDialog.StatusMessage; };
					return (string)m_progressDialog.Invoke(method);
				}
				return m_progressDialog.StatusMessage;
			}
			set
			{
				CheckDisposed();

				if (m_progressDialog.InvokeRequired)
				{
					SetStringInvoker method = delegate(string s) { m_progressDialog.StatusMessage = s; };
					m_progressDialog.Invoke(method, value);
				}
				else
					m_progressDialog.StatusMessage = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the maximum number of steps or increments corresponding to a progress
		/// bar that's 100% filled.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Maximum
		{
			get
			{
				CheckDisposed();

				if (m_progressDialog.InvokeRequired)
				{
					GetIntInvoker method = delegate() { return m_progressDialog.Maximum; };
					return (int)m_progressDialog.Invoke(method);
				}
				return m_progressDialog.Maximum;
			}
			set
			{
				CheckDisposed();

				if (m_progressDialog.InvokeRequired)
				{
					SetIntInvoker method = delegate(int i) { m_progressDialog.Maximum = i; };
					m_progressDialog.Invoke(method, value);
				}
				else
					m_progressDialog.Maximum = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating the number of steps (or increments) having been
		/// completed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Value
		{
			get
			{
				CheckDisposed();

				if (m_progressDialog.InvokeRequired)
				{
					GetIntInvoker method = delegate() { return m_progressDialog.Value; };
					return (int)m_progressDialog.Invoke(method);
				}
				return m_progressDialog.Value;
			}
			set
			{
				CheckDisposed();

				if (m_progressDialog.InvokeRequired)
				{
					SetIntInvoker method = delegate(int i) { m_progressDialog.Value = i; };
					m_progressDialog.Invoke(method, value);
				}
				else
					m_progressDialog.Value = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not the cancel button is visible.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool CancelButtonVisible
		{
			get
			{
				CheckDisposed();

				if (m_progressDialog.InvokeRequired)
				{
					GetBoolInvoker method = delegate() { return m_progressDialog.CancelButtonVisible; };
					return (bool)m_progressDialog.Invoke(method);
				}
				return m_progressDialog.CancelButtonVisible;
			}
			set
			{
				CheckDisposed();

				if (m_progressDialog.InvokeRequired)
				{
					SetBoolInvoker method = delegate(bool f)
					{ m_progressDialog.CancelButtonVisible = f; };
					m_progressDialog.Invoke(method, value);
				}
				else
					m_progressDialog.CancelButtonVisible = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the text on the cancel button.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string CancelButtonText
		{
			get
			{
				CheckDisposed();

				if (m_progressDialog.InvokeRequired)
				{
					GetStringInvoker method = delegate() { return m_progressDialog.CancelButtonText; };
					return (string)m_progressDialog.Invoke(method);
				}
				return m_progressDialog.CancelButtonText;
			}
			set
			{
				CheckDisposed();

				if (m_progressDialog.InvokeRequired)
				{
					SetStringInvoker method = delegate(string s) { m_progressDialog.CancelButtonText = s; };
					m_progressDialog.Invoke(method, value);
				}
				else
					m_progressDialog.CancelButtonText = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the owner form.
		/// </summary>
		/// <value>The owner form.</value>
		/// ------------------------------------------------------------------------------------
		public Form OwnerForm
		{
			get
			{
				// No good to try to access owner form if we're called from background thread
				if (m_progressDialog.InvokeRequired)
					return null;
				return m_owner;
			}
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the style of the ProgressBar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ProgressBarStyle ProgressBarStyle
		{
			get { return m_progressDialog.ProgressBarStyle; }
			set { m_progressDialog.ProgressBarStyle = value; }
		}

		#endregion

		#region Public Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Increment the progress counter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Increment()
		{
			CheckDisposed();

			if (m_progressDialog.InvokeRequired)
			{
				m_progressDialog.Invoke(new MethodInvoker(m_progressDialog.Increment));
			}
			else
				m_progressDialog.Increment();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If <paramref name="progressDlg"/> is not <c>null</c> we run the background task
		/// with that progress dialog (without creating a separate thread). Otherwise we display
		/// a new progress dialog as a modal dialog and start the background task in a separate
		/// thread.
		/// </summary>
		/// <param name="progressDlg">The existing progress dialog, or <c>null</c>.</param>
		/// <param name="fDisplayUi">set to <c>true</c> to display the progress dialog,
		/// <c>false</c> to run without UI.</param>
		/// <param name="backgroundTask">The background task.</param>
		/// <param name="parameters">The paramters that will be passed to the background task</param>
		/// <returns>The return value from the background thread.</returns>
		/// ------------------------------------------------------------------------------------
		public object RunTask(IAdvInd4 progressDlg, bool fDisplayUi,
			BackgroundTaskInvoker backgroundTask, params object[] parameters)
		{
			if (progressDlg != null)
			{
				int nMin;
				int nMax = 0;
				progressDlg.GetRange(out nMin, out nMax);
				progressDlg.Position = nMin;
				object ret = backgroundTask(progressDlg, parameters);
				progressDlg.SetRange(nMin, nMax);
				progressDlg.Position = nMax;
				return ret;
			}
			return RunTask(fDisplayUi, backgroundTask, parameters);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Displays the progress dialog as a modal dialog and starts the background task.
		/// </summary>
		/// <param name="fDisplayUi">set to <c>true</c> to display the progress dialog,
		/// <c>false</c> to run without UI.</param>
		/// <param name="backgroundTask">The background task.</param>
		/// <param name="parameters">The paramters that will be passed to the background task</param>
		/// <returns>The return value from the background thread.</returns>
		/// ------------------------------------------------------------------------------------
		public object RunTask(bool fDisplayUi, BackgroundTaskInvoker backgroundTask,
			params object[] parameters)
		{
			m_fDisplayUi = fDisplayUi;
			return RunTask_DebuggingOnly(fDisplayUi, backgroundTask, parameters);
			//m_backgroundTask = backgroundTask;
			//m_parameters = parameters;

			//if (!fDisplayUi)
			//    m_progressDialog.WindowState = FormWindowState.Minimized;

			//if (m_owner == null)
			//{
			//    if (fDisplayUi)
			//        m_progressDialog.ShowInTaskbar = true;
			//    m_progressDialog.StatusMessage = string.Empty;
			//}

			//m_progressDialog.ShowDialog(m_owner);

			//if (m_Exception != null)
			//{
			//    throw new WorkerThreadException("Background thread threw an exception",
			//        m_Exception);
			//}
			//System.Diagnostics.Debug.WriteLine("End of ProgressDialogWithTask.RunTask");
			//return m_RetValue;
		}

//#if DEBUG
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Displays the progress dialog and starts the background task (not on a background
		/// thread to make debugging easier).
		/// </summary>
		/// <param name="fDisplayUi">set to <c>true</c> to display the progress dialog,
		/// <c>false</c> to run without UI.</param>
		/// <param name="backgroundTask">The background task.</param>
		/// <param name="parameters">The paramters that will be passed to the background task
		/// </param>
		/// <returns>The return value from the background thread.</returns>
		/// <remarks>Use this method only temporary to debug the background task. It will
		/// run the task on the same thread as the progress dialog, thus preventing the progress
		/// dialog from work properly. However, it makes things easier to debug.</remarks>
		/// ------------------------------------------------------------------------------------
		public object RunTask_DebuggingOnly(bool fDisplayUi, BackgroundTaskInvoker backgroundTask,
			params object[] parameters)
		{
			m_progressDialog.Shown -= new EventHandler(OnProgressDialogShown);
			m_backgroundTask = backgroundTask;
			m_parameters = parameters;
			if (fDisplayUi)
			{
				if (m_owner != null)
					m_owner.Enabled = false;
				m_progressDialog.Show(m_owner);
			}

			RunBackgroundTask(null);

			if (m_owner != null)
				m_owner.Activate();

			if (m_Exception != null)
			{
				throw new WorkerThreadException("Background thread threw an exception",
					m_Exception);
			}
			return m_RetValue;
		}
//#endif
		#endregion

		#region Misc protected/privated methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when progress dialog starts to show. We can now start our background task.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the
		/// event data.</param>
		/// ------------------------------------------------------------------------------------
		private void OnProgressDialogShown(object sender, EventArgs e)
		{
			if (!m_fDisplayUi)
				m_progressDialog.Enabled = false;

			// TimS/EberhardB:
			// Our first try was to use a Thread and set its ApartmentState to STA. However
			// this caused an exception (COM object that has been separated from its underlying
			// RCW cannot be used) when the C++ code called FwStyleSheet.NormalFontStyle after
			// the progressDialogWithTask was disposed. The explanation we came up with for this
			// behavior is that we create a RCW on the correct thread if we create the COM
			// object directly (because the generated code in COMInterfaces makes sure the COM
			// objects get created on the main thread). However, if a COM method returns a
			// new COM object (e.g. TsPropsBldr.GetTextProps) the RCW ends up getting created
			// on the background thread. When the background thread exits we are in trouble.
			//
			// Using a thread pool thread instead seems to work. We initially thought it is
			// because the thread doesn't exit when the background task is finished, but our
			// guess now is that it has to do with the fact that thread pool threads run in
			// MTA (we found no way to change this). Interestingly it also worked when we
			// created a Thread and set it to MTA and the thread exited with the end of
			// the background task. So it seems that the fact that we create the COM objects
			// on the STA main thread is sufficient; it creates a RCW that can deal with
			// switching threads for the STA COM object. When we get a COM object from a
			// COM method it creates a normal RCW that doesn't deal with STA thread switches
			// therefore it doesn't matter when the background thread exits.
			//
			// We're not entirely convinced that using MTA on the background thread doesn't
			// cause some problems somewhere. If it turns out to be a problem we might want
			// to create our own ThreadManager class that sets its threads to STA but keeps
			// them around (and reuses them) until the app exits.
			ThreadPool.QueueUserWorkItem(new WaitCallback(RunBackgroundTask));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Executes the background task.
		/// </summary>
		/// <remarks>This method runs in the background thread!</remarks>
		/// ------------------------------------------------------------------------------------
		private void RunBackgroundTask(object obj)
		{
			if (Thread.CurrentThread.Name == null)
				Thread.CurrentThread.Name = "Background thread";

			m_Exception = null;
			m_RetValue = null;

			try
			{
				m_RetValue = m_backgroundTask(this, m_parameters);
			}
			catch (Exception e)
			{
				System.Diagnostics.Debug.WriteLine("Got exception in background thread: "
					+ e.Message);
				m_Exception = e;
			}
			finally
			{
				try
				{
					if (m_progressDialog != null && m_progressDialog.IsHandleCreated)
						m_progressDialog.Invoke(new MethodInvoker(m_progressDialog.Close));
				}
				catch (Exception e)
				{
					System.Diagnostics.Debug.WriteLine(
						"Got exception in background thread while trying to close progress dialog: "
						+ e.Message);
				}
			}

			System.Diagnostics.Debug.WriteLine(string.Format(
				"End of ProgressDialogWithTask.RunBackgroundTask ({0})", Win32.GetCurrentThreadId()));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calls subscribers to the cancel event.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void OnCancel(object sender)
		{
			if (Cancel != null)
				Cancel(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when visible state of the progress dialog changed.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		protected void OnProgressDialogVisibleChanged(object sender, EventArgs e)
		{
			bool fCancelVisible = (Cancel != null);
			if (m_progressDialog.InvokeRequired)
			{
				SetBoolInvoker method = delegate(bool f)
					{ m_progressDialog.CancelButtonVisible = f; };
				m_progressDialog.Invoke(method, fCancelVisible);
			}
			else
				m_progressDialog.CancelButtonVisible = fCancelVisible;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulate pressing the cancel button. Used in tests.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void PressCancelButton()
		{
			if (m_progressDialog.InvokeRequired)
			{
				m_progressDialog.Invoke(new EventInvoker(m_progressDialog.btnCancel_Click),
					null, EventArgs.Empty);
			}
			else
				m_progressDialog.btnCancel_Click(null, EventArgs.Empty);
		}
		#endregion

		#region Implementation of IAdvInd4
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// <param name="cSteps"></param>
		/// ------------------------------------------------------------------------------------
		public virtual void Step(int cSteps)
		{
			CheckDisposed();

			if (m_progressDialog.InvokeRequired)
			{
				m_progressDialog.Invoke(new SetIntInvoker(m_progressDialog.Step),
					cSteps);
			}
			else
				m_progressDialog.Step(cSteps);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual string Title
		{
			get
			{
				CheckDisposed();

				if (m_progressDialog.InvokeRequired)
				{
					GetStringInvoker method = delegate() { return m_progressDialog.Title; };
					return (string)m_progressDialog.Invoke(method);
				}
				else
					return (string)m_progressDialog.Title;
			}
			set
			{
				CheckDisposed();

				if (m_progressDialog.InvokeRequired)
				{
					SetStringInvoker method = delegate(string s) { m_progressDialog.Text = s; };
					m_progressDialog.Invoke(method, value);
				}
				else
					m_progressDialog.Text = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual string Message
		{
			get
			{
				CheckDisposed();
				// Multithreading dealt in StatusMessage
				return StatusMessage;
			}
			set
			{
				CheckDisposed();
				// Multithreading dealt in StatusMessage
				StatusMessage = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual int Position
		{
			get
			{
				CheckDisposed();

				if (m_progressDialog.InvokeRequired)
				{
					GetIntInvoker method = delegate() { return m_progressDialog.Position; };
					return (int)m_progressDialog.Invoke(method);
				}
				else
					return (int)m_progressDialog.Position;
			}
			set
			{
				CheckDisposed();

				if (m_progressDialog.InvokeRequired)
				{
					SetIntInvoker method = delegate(int i) { m_progressDialog.Position = i; };
					m_progressDialog.Invoke(method, value);
				}
				else
					m_progressDialog.Position = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual int StepSize
		{
			get
			{
				CheckDisposed();

				if (m_progressDialog.InvokeRequired)
				{
					GetIntInvoker method = delegate() { return m_progressDialog.StepSize; };
					return (int)m_progressDialog.Invoke(method);
				}
				else
					return (int)m_progressDialog.StepSize;
			}
			set
			{
				CheckDisposed();

				if (m_progressDialog.InvokeRequired)
				{
					SetIntInvoker method = delegate(int i) { m_progressDialog.StepSize = i; };
					m_progressDialog.Invoke(method,	value);
				}
				else
					m_progressDialog.StepSize = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// <param name="nMin"></param>
		/// <param name="nMax"></param>
		/// ------------------------------------------------------------------------------------
		public virtual void SetRange(int nMin, int nMax)
		{
			CheckDisposed();

			if (m_progressDialog.InvokeRequired)
			{
				m_progressDialog.Invoke(new SetIntIntInvoker(m_progressDialog.SetRange),
					nMin, nMax);
			}
			else
				m_progressDialog.SetRange(nMin, nMax);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Member GetRange
		/// </summary>
		/// <param name="nMin"></param>
		/// <param name="nMax"></param>
		/// ------------------------------------------------------------------------------------
		public void GetRange(out int nMin, out int nMax)
		{
			CheckDisposed();

			if (m_progressDialog.InvokeRequired)
			{
				GetIntInvoker methodMin = delegate() { return m_progressDialog.Minimum; };
				nMin = (int)m_progressDialog.Invoke(methodMin);
				GetIntInvoker methodMax = delegate() { return m_progressDialog.Maximum; };
				nMax = (int)m_progressDialog.Invoke(methodMax);
			}
			else
				m_progressDialog.GetRange(out nMin, out nMax);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not the progress indicator can restart
		/// at zero if it goes beyond the end.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool Restartable
		{
			get
			{
				CheckDisposed();

				return m_progressDialog.Restartable;
			}
			set
			{
				CheckDisposed();

				m_progressDialog.Restartable = value;
			}
		}
		#endregion
	}

	#region CancelException Class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Useful little exception class that clients can throw as part of their processing the
	/// Progress Dialog's cancel event.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class CancelException : Exception
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor that allows a message to be set.
		/// </summary>
		/// <param name="msg"></param>
		/// ------------------------------------------------------------------------------------
		public CancelException(string msg)
			: base(msg)
		{
		}
	}
	#endregion

	#region WorkerThreadException
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This exception is thrown by a foreground thread when a background worker thread
	/// had an exception. This allows all exceptions to be handled by the foreground thread.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class WorkerThreadException : Exception
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:WorkerThreadException"/> class.
		/// </summary>
		/// <param name="message">The message.</param>
		/// <param name="innerException">The inner exception.</param>
		/// ------------------------------------------------------------------------------------
		public WorkerThreadException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
	#endregion
}
