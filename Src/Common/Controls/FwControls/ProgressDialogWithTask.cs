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
using System.ComponentModel;
using System.Threading;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// A progress dialog (with an optional cancel button) that displays while a task runs
	/// in the background.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ProgressDialogWithTask : IProgress, IFWDisposable
	{
		/// <summary>
		/// Occurs when [canceling].
		/// </summary>
		public event CancelEventHandler Canceling;

		#region Member variables
		private ProgressDialogImpl m_progressDialog;
		private volatile bool m_fDisposed;
		private Func<IProgress, object[], object> m_backgroundTask;
		private object[] m_parameters;
		private object m_RetValue;
		private Exception m_Exception;
		private readonly Form m_owner;
		private bool m_fOwnerWasTopMost;
		private BackgroundWorker m_worker;
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
			m_owner = owner;
			InitOnOwnerThread();
			m_progressDialog.Shown += m_progressDialog_Shown;
			m_progressDialog.Canceling += m_progressDialog_Canceling;
			m_progressDialog.FormClosing += m_progressDialog_FormClosing;
			m_worker.DoWork += RunBackgroundTask;
			m_worker.RunWorkerCompleted += m_worker_RunWorkerCompleted;
		}

		private void InitOnOwnerThread()
		{
			if (m_owner != null && m_owner.InvokeRequired)
			{
				m_owner.Invoke((Action) InitOnOwnerThread);
				return;
			}

			m_worker = new BackgroundWorker { WorkerSupportsCancellation = true };
			m_progressDialog = new ProgressDialogImpl(m_owner);
			// Don't let the owner hide the progress dialog.  See FWR-3482.
			if (m_owner != null && m_owner.TopMost)
			{
				m_fOwnerWasTopMost = true;
				m_owner.TopMost = false;
				m_progressDialog.TopMost = true;
			}
			IntPtr handle = m_progressDialog.Handle;
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
		protected virtual void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (disposing)
			{
				if (m_progressDialog != null)
					m_progressDialog.DisposeOnGuiThread();
				if (m_worker != null)
				{
					m_worker.CancelAsync();
					m_worker.Dispose();
				}
			}

			m_progressDialog = null;
			m_Exception = null;
			m_worker = null;

			m_fDisposed = true;
		}
		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the minimum value of the progress bar.
		/// </summary>
		/// <value>The minimum.</value>
		public int Minimum
		{
			get
			{
				CheckDisposed();

				if (m_progressDialog.InvokeRequired)
					return (int)m_progressDialog.Invoke((Func<int>)(() => m_progressDialog.Minimum));
				return m_progressDialog.Maximum;
			}
			set
			{
				CheckDisposed();

				if (m_progressDialog.InvokeRequired)
					m_progressDialog.Invoke((Action<int>)(i => m_progressDialog.Minimum = i), value);
				else
					m_progressDialog.Maximum = value;
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
					return (int)m_progressDialog.Invoke((Func<int>) (() => m_progressDialog.Maximum));
				return m_progressDialog.Maximum;
			}
			set
			{
				CheckDisposed();

				if (m_progressDialog.InvokeRequired)
					m_progressDialog.Invoke((Action<int>) (i => m_progressDialog.Maximum = i), value);
				else
					m_progressDialog.Maximum = value;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether the task has been canceled.
		/// </summary>
		/// <value><c>true</c> if canceled; otherwise, <c>false</c>.</value>
		public bool Canceled
		{
			get
			{
				return m_worker.CancellationPending;
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
					return (int)m_progressDialog.Invoke((Func<int>) (() => m_progressDialog.Value));
				return m_progressDialog.Value;
			}
			set
			{
				CheckDisposed();

				if (m_progressDialog.InvokeRequired)
					m_progressDialog.Invoke((Action<int>) (i => m_progressDialog.Value = i), value);
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
					return (bool)m_progressDialog.Invoke((Func<bool>) (() => m_progressDialog.CancelButtonVisible));
				return m_progressDialog.CancelButtonVisible;
			}
			set
			{
				CheckDisposed();

				if (m_progressDialog.InvokeRequired)
					m_progressDialog.Invoke((Action<bool>) (f => m_progressDialog.CancelButtonVisible = f), value);
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
					return (string)m_progressDialog.Invoke((Func<string>) (() => m_progressDialog.CancelButtonText));
				return m_progressDialog.CancelButtonText;
			}
			set
			{
				CheckDisposed();

				if (m_progressDialog.InvokeRequired)
					m_progressDialog.Invoke((Action<string>) (s => m_progressDialog.CancelButtonText = s), value);
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
			get
			{
				if (m_progressDialog.InvokeRequired)
					return (ProgressBarStyle) m_progressDialog.Invoke((Func<ProgressBarStyle>) (() => m_progressDialog.ProgressBarStyle));
				return m_progressDialog.ProgressBarStyle;
			}
			set
			{
				if (m_progressDialog.InvokeRequired)
					m_progressDialog.Invoke((Action<ProgressBarStyle>) (style => m_progressDialog.ProgressBarStyle = style), value);
				else
					m_progressDialog.ProgressBarStyle = value;
			}
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

		#region Public Methods

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
		public object RunTask(IProgress progressDlg, bool fDisplayUi,
			Func<IProgress, object[], object> backgroundTask, params object[] parameters)
		{
			if (progressDlg != null)
			{
				int nMin = progressDlg.Minimum;
				int nMax = progressDlg.Maximum;
				progressDlg.Position = nMin;
				object ret = backgroundTask(progressDlg, parameters);
				progressDlg.Minimum = nMin;
				progressDlg.Maximum = nMax;
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
		public object RunTask(bool fDisplayUi, Func<IProgress, object[], object> backgroundTask,
			params object[] parameters)
		{
			m_backgroundTask = backgroundTask;
			m_parameters = parameters;

			if (!fDisplayUi)
			{
				if (m_progressDialog.InvokeRequired)
					m_progressDialog.Invoke((Action) (() => m_progressDialog.WindowState = FormWindowState.Minimized));
				else
					m_progressDialog.WindowState = FormWindowState.Minimized;
			}

			if (m_owner == null)
			{
				// On Linux using Xephyr (and possibly other environments that lack a taskbar),
				// m_progressDialog.ShowInTaskBar being true can result in m_progressDialog.ShowDialog(null)
				// acting as though we called m_progressDialog.ShowInTaskBar(m_progressDialog), which of
				// course throws an exception.  This is really a bug in Mono's Form.ShowDialog implementation.
				if (fDisplayUi && !MiscUtils.IsUnix)
				{
					if (m_progressDialog.InvokeRequired)
						m_progressDialog.Invoke((Action) (() => m_progressDialog.ShowInTaskbar = true));
					else
						m_progressDialog.ShowInTaskbar = true;
				}
				Message = string.Empty;
			}

			if (m_progressDialog.InvokeRequired)
				m_progressDialog.Invoke((Func<IWin32Window, DialogResult>) m_progressDialog.ShowDialog, m_owner);
			else
				m_progressDialog.ShowDialog(m_owner);

			if (m_Exception != null)
			{
				throw new WorkerThreadException("Background thread threw an exception",
					m_Exception);
			}
			return m_RetValue;
		}

#if DEBUG
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
		public object RunTask_DebuggingOnly(bool fDisplayUi, Func<IProgress, object[], object> backgroundTask,
			params object[] parameters)
		{
			m_progressDialog.Shown -= m_progressDialog_Shown;
			m_backgroundTask = backgroundTask;
			m_parameters = parameters;
			if (fDisplayUi)
			{
				if (m_owner != null)
				{
					if (m_owner.InvokeRequired)
						m_owner.Invoke((Action) (() => m_owner.Enabled = false));
					else
						m_owner.Enabled = false;
				}

				if (m_progressDialog.InvokeRequired)
					m_progressDialog.Invoke((Action<IWin32Window>)m_progressDialog.Show, m_owner);
				else
					m_progressDialog.Show(m_owner);
			}

			RunBackgroundTask(null, null);

			if (m_owner != null)
			{
				if (m_owner.InvokeRequired)
				{
					m_owner.Invoke((Action)(() =>
					{
						m_owner.Enabled = true;
						m_owner.Activate();
					}));
				}
				else
				{
					m_owner.Enabled = true;
					m_owner.Activate();
				}
			}

			if (m_Exception != null)
			{
				throw new WorkerThreadException("Background thread threw an exception",
					m_Exception);
			}
			return m_RetValue;
		}
#endif
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
		private void m_progressDialog_Shown(object sender, EventArgs e)
		{
			m_worker.RunWorkerAsync();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Executes the background task.
		/// </summary>
		/// <remarks>This method runs in the background thread!</remarks>
		/// ------------------------------------------------------------------------------------
		private void RunBackgroundTask(object sender, DoWorkEventArgs e)
		{
			if (string.IsNullOrEmpty(Thread.CurrentThread.Name))
				Thread.CurrentThread.Name = "Background thread";

			m_Exception = null;
			m_RetValue = null;

			try
			{
				m_RetValue = m_backgroundTask(this, m_parameters);
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine("Got exception in background thread: "
					+ ex.Message);
				m_Exception = ex;
			}
		}

		/// <summary>
		/// Calls subscribers to the cancel event.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		protected virtual void m_progressDialog_Canceling(object sender, CancelEventArgs e)
		{
			bool cancel = true;
			if (Canceling != null)
			{
				var cea = new CancelEventArgs();
				Canceling(this, cea);
				e.Cancel = cea.Cancel;
				cancel = !cea.Cancel;
			}
			if (cancel)
				m_worker.CancelAsync();
		}

		private void m_progressDialog_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (m_worker.IsBusy)
				e.Cancel = true;
			if (m_fOwnerWasTopMost && m_owner != null)
				m_owner.TopMost = true;
		}

		private void m_worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (m_progressDialog.InvokeRequired)
				m_progressDialog.Invoke((Action)m_progressDialog.Close, m_owner);
			else
				m_progressDialog.Close();

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
				m_progressDialog.Invoke((Action<object, EventArgs>) m_progressDialog.btnCancel_Click,
					null, EventArgs.Empty);
			}
			else
				m_progressDialog.btnCancel_Click(null, EventArgs.Empty);
		}
		#endregion

		#region Implementation of IProgress
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
				m_progressDialog.Invoke((Action<int>) Step, cSteps);
				return;
			}
			m_progressDialog.Value += (cSteps > 0) ? cSteps : m_progressDialog.Step;
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
					return (string)m_progressDialog.Invoke((Func<string>) (() => m_progressDialog.Text));
				return m_progressDialog.Text;
			}
			set
			{
				CheckDisposed();

				if (m_progressDialog.InvokeRequired)
					m_progressDialog.Invoke((Action<string>)(s => m_progressDialog.Text = s), value);
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

				if (m_progressDialog.InvokeRequired)
					return (string)m_progressDialog.Invoke((Func<string>)(() => m_progressDialog.StatusMessage));
				return m_progressDialog.StatusMessage;
			}
			set
			{
				CheckDisposed();

				if (m_progressDialog.InvokeRequired)
					m_progressDialog.Invoke((Action<string>)(s => m_progressDialog.StatusMessage = s), value);
				else
					m_progressDialog.StatusMessage = value;
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
					return (int) m_progressDialog.Invoke((Func<int>) (() => m_progressDialog.Value));
				return m_progressDialog.Value;
			}
			set
			{
				CheckDisposed();

				if (m_progressDialog.InvokeRequired)
					m_progressDialog.Invoke((Action<int>) (i => m_progressDialog.Value = i), value);
				else
					m_progressDialog.Value = value;
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
					return (int)m_progressDialog.Invoke((Func<int>) (() => m_progressDialog.Step));
				return m_progressDialog.Step;
			}
			set
			{
				CheckDisposed();

				if (m_progressDialog.InvokeRequired)
					m_progressDialog.Invoke((Action<int>) (i => m_progressDialog.Step = i), value);
				else
					m_progressDialog.Step = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the progress as a form (used for message box owners, etc).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual Form Form
		{
			get { return m_progressDialog; }
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
