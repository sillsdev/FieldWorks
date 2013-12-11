// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2007' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ProgressDialogWithTask.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application.ApplicationServices;
using System.IO;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// A progress dialog (with an optional cancel button) that displays while a task runs
	/// in the background.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ProgressDialogWithTask : IThreadedProgress, IFWDisposable
	{
		/// <summary>
		/// Occurs when [canceling].
		/// </summary>
		public event CancelEventHandler Canceling;

		#region Member variables
		/// <summary>The form that actually displays the progress</summary>
		internal ProgressDialogWithTaskDlgImpl m_progressDialog;
		private readonly bool m_fCreatedProgressDlg;
		private volatile bool m_fDisposed;
		private readonly Form m_owner;
		private Func<IThreadedProgress, object[], object> m_backgroundTask;
		private object[] m_parameters;
		private object m_RetValue;
		private Exception m_Exception;
		private bool m_fOwnerWasTopMost;
		private BackgroundWorker m_worker;
		private readonly ISynchronizeInvoke m_synchronizeInvoke;
		#endregion

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ProgressDialogWithTask" /> class that
		/// will create an actual progress dialog box to track the progress.
		/// </summary>
		/// <param name="owner">The form to use as the owner when creating the actual progress
		/// dialog.</param>
		public ProgressDialogWithTask(Form owner)
			: this(owner, owner)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ProgressDialogWithTask"/> class.
		/// </summary>
		/// <param name="synchronizeInvoke"></param>
		public ProgressDialogWithTask(ISynchronizeInvoke synchronizeInvoke)
			: this(null, synchronizeInvoke)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ProgressDialogWithTask" /> class.
		/// </summary>
		/// <param name="owner">The "owning" form (can be null if progressDlg is specified or if
		/// there is no suitable form available to own the progress dialog)</param>
		/// <param name="synchronizeInvoke">The synchronize invoke.</param>
		private ProgressDialogWithTask(Form owner, ISynchronizeInvoke synchronizeInvoke)
		{
			m_owner = owner;
			m_synchronizeInvoke = synchronizeInvoke;
			m_fCreatedProgressDlg = true;
			InitOnOwnerThread();
			if (m_synchronizeInvoke == null)
				m_synchronizeInvoke = m_progressDialog.SynchronizeInvoke;
			m_progressDialog.Canceling += m_progressDialog_Canceling;
			m_progressDialog.FormClosing += m_progressDialog_FormClosing;
			m_worker.DoWork += RunBackgroundTask;
			m_worker.RunWorkerCompleted += m_worker_RunWorkerCompleted;
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="m_worker gets disposed in Dispose()")]
		private void InitOnOwnerThread()
		{
			if (m_synchronizeInvoke != null && m_synchronizeInvoke.InvokeRequired)
			{
				m_synchronizeInvoke.Invoke(InitOnOwnerThread);
				return;
			}

			m_worker = new BackgroundWorker { WorkerSupportsCancellation = true };

			m_progressDialog = new ProgressDialogWithTaskDlgImpl(m_owner);

			// This is the only way to force handle creation for a form that is not yet visible.
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
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (disposing)
			{
				if (m_progressDialog != null)
				{
					m_progressDialog.Canceling -= m_progressDialog_Canceling;
					m_progressDialog.FormClosing -= m_progressDialog_FormClosing;
					RemoveStartListener();
					if (m_fCreatedProgressDlg)
						m_progressDialog.DisposeOnGuiThread();
				}
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

		#region Implementation of IProgress
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Steps the specified number of steps.
		/// </summary>
		/// <param name="cSteps">The count of steps. If it's 0, step the default step size</param>
		/// ------------------------------------------------------------------------------------
		public virtual void Step(int cSteps)
		{
			CheckDisposed();

			if (m_fCreatedProgressDlg && m_synchronizeInvoke.InvokeRequired)
			{
				m_synchronizeInvoke.Invoke((Action<int>)Step, new object[] {cSteps});
				return;
			}
			m_progressDialog.Position += (cSteps > 0) ? cSteps : m_progressDialog.StepSize;
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

				if (m_fCreatedProgressDlg && m_synchronizeInvoke.InvokeRequired)
					return (string)m_synchronizeInvoke.Invoke((Func<string>)(() => m_progressDialog.Title), null);
				return m_progressDialog.Title;
			}
			set
			{
				CheckDisposed();

				if (m_fCreatedProgressDlg && m_synchronizeInvoke.InvokeRequired)
					m_synchronizeInvoke.Invoke((Action<string>)(s => m_progressDialog.Title = s), new [] {value});
				else
					m_progressDialog.Title = value;
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

				if (m_fCreatedProgressDlg && m_synchronizeInvoke.InvokeRequired)
					return (string)m_synchronizeInvoke.Invoke((Func<string>)(() => m_progressDialog.Message), null);
				return m_progressDialog.Message;
			}
			set
			{
				CheckDisposed();

				if (m_fCreatedProgressDlg && m_synchronizeInvoke.InvokeRequired)
					m_synchronizeInvoke.Invoke((Action<string>)(s => m_progressDialog.Message = s), new [] {value});
				else
					m_progressDialog.Message = value;
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

				if (m_fCreatedProgressDlg && m_synchronizeInvoke.InvokeRequired)
					return (int)m_synchronizeInvoke.Invoke((Func<int>)(() => m_progressDialog.Position), null);
				return m_progressDialog.Position;
			}
			set
			{
				CheckDisposed();

				if (m_fCreatedProgressDlg && m_synchronizeInvoke.InvokeRequired)
					m_synchronizeInvoke.Invoke((Action<int>)(i => m_progressDialog.Position = i), new object[] {value});
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

				if (m_fCreatedProgressDlg && m_synchronizeInvoke.InvokeRequired)
					return (int)m_synchronizeInvoke.Invoke((Func<int>)(() => m_progressDialog.StepSize), null);
				return m_progressDialog.StepSize;
			}
			set
			{
				CheckDisposed();

				if (m_fCreatedProgressDlg && m_synchronizeInvoke.InvokeRequired)
					m_synchronizeInvoke.Invoke((Action<int>)(i => m_progressDialog.StepSize = i), new object[] {value});
				else
					m_progressDialog.StepSize = value;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether this progress is indeterminate.
		/// </summary>
		public bool IsIndeterminate
		{
			get
			{
				if (m_fCreatedProgressDlg && m_synchronizeInvoke.InvokeRequired)
					return m_synchronizeInvoke.Invoke(() => m_progressDialog.IsIndeterminate);
				return m_progressDialog.IsIndeterminate;
			}

			set
			{
				if (m_fCreatedProgressDlg && m_synchronizeInvoke.InvokeRequired)
					m_synchronizeInvoke.Invoke(() => m_progressDialog.IsIndeterminate = value);
				else
					m_progressDialog.IsIndeterminate = value;
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
				CheckDisposed();

				if (m_fCreatedProgressDlg && m_synchronizeInvoke.InvokeRequired)
					return (int)m_synchronizeInvoke.Invoke((Func<int>)(() => m_progressDialog.Minimum), null);
				return m_progressDialog.Maximum;
			}
			set
			{
				CheckDisposed();

				if (m_fCreatedProgressDlg && m_synchronizeInvoke.InvokeRequired)
					m_synchronizeInvoke.Invoke((Action<int>)(i => m_progressDialog.Minimum = i), new object[] {value});
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

				if (m_fCreatedProgressDlg && m_synchronizeInvoke.InvokeRequired)
					return (int)m_synchronizeInvoke.Invoke((Func<int>) (() => m_progressDialog.Maximum), null);
				return m_progressDialog.Maximum;
			}
			set
			{
				CheckDisposed();

				if (m_fCreatedProgressDlg && m_synchronizeInvoke.InvokeRequired)
					m_synchronizeInvoke.Invoke((Action<int>) (i => m_progressDialog.Maximum = i), new object[] {value});
				else
					m_progressDialog.Maximum = value;
			}
		}
		#endregion

		#region Other Properties
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

				if (m_synchronizeInvoke.InvokeRequired)
					return (string)m_synchronizeInvoke.Invoke((Func<string>)(() => ((ProgressDialogImpl)m_progressDialog).CancelButtonText), null);
				return ((ProgressDialogImpl)m_progressDialog).CancelButtonText;
			}
			set
			{
				CheckDisposed();

				if (m_synchronizeInvoke.InvokeRequired)
					m_synchronizeInvoke.Invoke((Action<string>)(s => ((ProgressDialogImpl)m_progressDialog).CancelButtonText = s), new object[] {value});
				else
					((ProgressDialogImpl)m_progressDialog).CancelButtonText = value;
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

		#region Implementation of IThreadedProgress
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
		/// Gets or sets a value indicating whether or not the cancel button is visible.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool AllowCancel
		{
			get
			{
				CheckDisposed();

				if (m_fCreatedProgressDlg && m_synchronizeInvoke.InvokeRequired)
					return (bool)m_synchronizeInvoke.Invoke((Func<bool>)(() => m_progressDialog.AllowCancel), null);
				return m_progressDialog.AllowCancel;
			}
			set
			{
				CheckDisposed();

				if (m_fCreatedProgressDlg && m_synchronizeInvoke.InvokeRequired)
					m_synchronizeInvoke.Invoke((Action<bool>)(f => m_progressDialog.AllowCancel = f), new object[] {value});
				else
					m_progressDialog.AllowCancel = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a thread helper used for ensuring that required tasks are invoked on the main
		/// UI thread.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ISynchronizeInvoke SynchronizeInvoke
		{
			get { return m_synchronizeInvoke; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If progress dialog is already showing, we run the background task using it (without
		/// creating a separate thread). Otherwise we display a new progress dialog as a modal
		/// dialog and start the background task in a separate thread.
		/// </summary>
		/// <param name="backgroundTask">The background task.</param>
		/// <param name="parameters">The paramters that will be passed to the background task</param>
		/// <returns>The return value from the background thread.</returns>
		/// <exception cref="WorkerThreadException">Wraps any exception thrown by the background
		/// task</exception>
		/// ------------------------------------------------------------------------------------
		public object RunTask(Func<IThreadedProgress, object[], object> backgroundTask,
			params object[] parameters)
		{
			return RunTask(true, backgroundTask, parameters);
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
		/// <exception cref="WorkerThreadException">Wraps any exception thrown by the background
		/// task</exception>
		/// ------------------------------------------------------------------------------------
		public object RunTask(bool fDisplayUi, Func<IThreadedProgress, object[], object> backgroundTask,
			params object[] parameters)
		{
			if (m_progressDialog.Visible)
			{
				int nMin = Minimum;
				int nMax = Maximum;
				Position = nMin;
				object ret = backgroundTask(this, parameters);
				Minimum = nMin;
				Maximum = nMax;
				Position = nMax;
				return ret;
			}

			Debug.Assert(m_fCreatedProgressDlg);

			m_backgroundTask = backgroundTask;
			m_parameters = parameters;

			if (!fDisplayUi)
			{
				if (m_synchronizeInvoke.InvokeRequired)
					m_synchronizeInvoke.Invoke((Action) (() => m_progressDialog.WindowState = FormWindowState.Minimized), null);
				else
					m_progressDialog.WindowState = FormWindowState.Minimized;
			}

			// On Linux using Xephyr (and possibly other environments that lack a taskbar),
			// m_progressDialog.ShowInTaskBar being true can result in m_progressDialog.ShowDialog(null)
			// acting as though we called m_progressDialog.ShowInTaskBar(m_progressDialog), which of
			// course throws an exception.  This is really a bug in Mono's Form.ShowDialog implementation.
			if (Application.OpenForms.Count == 0 && fDisplayUi && !MiscUtils.IsUnix)
			{
				if (m_synchronizeInvoke.InvokeRequired)
					m_synchronizeInvoke.Invoke((Action)(() => m_progressDialog.ShowInTaskbar = true), null);
				else
					m_progressDialog.ShowInTaskbar = true;
			}

			// Don't let the owner hide the progress dialog.  See FWR-3482.
			Form owner = m_owner;
			if (owner != null && owner.TopMost)
			{
				m_fOwnerWasTopMost = true;
				owner.TopMost = false;
				m_progressDialog.TopMost = true;
			}

			LaunchDialogAndTask(owner);

			if (m_Exception != null)
			{
				throw new WorkerThreadException("Background thread threw an exception",
					m_Exception);
			}
			return m_RetValue;
		}

		private void LaunchDialogAndTask(IWin32Window owner)
		{
			AddStartListener();
			var progressDlg = m_progressDialog as ProgressDialogWithTaskDlgImpl;
			if (progressDlg != null)
			{
				if (m_synchronizeInvoke.InvokeRequired)
				{
					m_synchronizeInvoke.Invoke((Func<IWin32Window, DialogResult>)progressDlg.LaunchDialogAndStartTask,
						new object[] { owner });
				}
				else
				{
					progressDlg.LaunchDialogAndStartTask(owner);
				}
			}
			else
			{
				if (m_synchronizeInvoke.InvokeRequired)
				{
					m_synchronizeInvoke.Invoke((Func<IWin32Window, DialogResult>)m_progressDialog.ShowDialog,
						new object[] { owner });
				}
				else
				{
					m_progressDialog.ShowDialog(owner);
				}
			}
		}

		private void AddStartListener()
		{
			var progressDlg = m_progressDialog as ProgressDialogWithTaskDlgImpl;
			if (progressDlg != null)
			{
				((ProgressDialogWithTaskDlgImpl)m_progressDialog).Start += DialogShown;
			}
			else
			{
				m_progressDialog.Shown += DialogShown;
			}
		}

		private void RemoveStartListener()
		{
			var progressDlg = m_progressDialog as ProgressDialogWithTaskDlgImpl;
			if (progressDlg != null)
			{
				((ProgressDialogWithTaskDlgImpl)m_progressDialog).Start -= DialogShown;
			}
			else
			{
				m_progressDialog.Shown -= DialogShown;
			}
		}

		private void DialogShown(object sender, EventArgs e)
		{
			DialogShown();
		}

		private void DialogShown()
		{
			m_worker.RunWorkerAsync();
			RemoveStartListener();
		}

		#endregion

		// I made this region to hold methods that perform particular tasks involving wrapping this dialog around
		// some work.
		#region static methods to encapsulate usages of the dialog
		/// <summary>
		/// I'd like to put all this logic into XmlTranslatedLists, because it is common to most cases of
		/// calling ImportTranslatedListsForWs, which is the point of this method. Unfortunately FDO
		/// cannot reference the DLL that has ProgressDialogWithTask. I've made it public static so that
		/// anything that references FwControls can use it at least.
		/// </summary>
		/// <param name="cache"> </param>
		/// <param name="ws"></param>
		/// <param name="parentWindow"> </param>
		public static void ImportTranslatedListsForWs(Form parentWindow, FdoCache cache, string ws)
		{
			string path = XmlTranslatedLists.TranslatedListsPathForWs(ws, FwDirectoryFinder.TemplateDirectory);
			if (!File.Exists(path))
				return;
			using (var dlg = new ProgressDialogWithTask(parentWindow))
			{
				dlg.AllowCancel = true;
				dlg.Maximum = 200;
				dlg.Message = Path.GetFileName(path);
				dlg.Title = XmlTranslatedLists.ProgressDialogCaption;
				dlg.RunTask(true, ImportTranslatedListsForWs, ws, cache);
			}
		}

		/// <summary>
		/// Method with required signature for ProgressDialogWithTask.RunTask, to invoke XmlTranslatedLists.ImportTranslatedListsForWs.
		/// Should only be called by the other overload of ImportTranslatedListsForWs.
		/// args must be a writing system identifier string and an FdoCache.
		/// </summary>
		/// <param name="dlg"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		private static object ImportTranslatedListsForWs(IThreadedProgress dlg, object[] args)
		{
			var ws = (string)args[0];
			var cache = (FdoCache) args[1];
			XmlTranslatedLists.ImportTranslatedListsForWs(ws, cache, FwDirectoryFinder.TemplateDirectory, dlg);
			return null;
		}
		#endregion

		#region Misc protected/private methods

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

			if (MiscUtils.RunningTests)
				ManifestHelper.CreateActivationContext();
			m_Exception = null;
			m_RetValue = null;

			try
			{
				m_RetValue = m_backgroundTask(this, m_parameters);
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Got exception in background thread: " + ex.Message);
				m_Exception = ex;
			}
			finally
			{
				if (MiscUtils.RunningTests)
					ManifestHelper.DestroyActivationContext();
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
			if (!m_fOwnerWasTopMost)
				return;
			Form owner = ((Form)sender).Owner;
			if (owner != null)
				owner.TopMost = true;
		}

		private void m_worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (m_fCreatedProgressDlg && m_synchronizeInvoke.InvokeRequired)
				m_synchronizeInvoke.Invoke((Action)m_progressDialog.Close, null);
			else
				m_progressDialog.Close();
		}
		#endregion
	}

	internal class ProgressDialogWithTaskDlgImpl : ProgressDialogImpl
	{
		internal delegate void StartTask();
		/// <summary>
		/// This event will be triggered either by the showing of the dialog or a timer.
		/// </summary>
		public event StartTask Start = delegate {  };
		private System.Windows.Forms.Timer _timer = new System.Windows.Forms.Timer();
		public ProgressDialogWithTaskDlgImpl(Form owner) : base(owner)
		{
			_timer.Interval = 50;
			_timer.Tick += timer_Tick;
		}

		/// <summary>
		/// It used to be that the Shown event of ProgressDialogImpl was used to trigger the start of the task.
		/// This proved unreliable. This timer is a backup for cases where OnShown doesn't happen due to exceptions
		/// being thrown during the dialogs inital message pumping.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void timer_Tick(object sender, EventArgs e)
		{
			Start(); //Alert any listeners that we have started.
			_timer.Stop();
		}

		/// <summary>
		/// Kicks off a timer to send the Start event to any listeners and show the progress dialog
		/// </summary>
		/// <param name="owner"></param>
		/// <returns></returns>
		public DialogResult LaunchDialogAndStartTask(IWin32Window owner)
		{
			_timer.Start();
			return ShowDialog(owner);
		}

		/// <summary>
		/// Override to remove timer in the normal case where it is unnecessary.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);
			_timer.Tick -= timer_Tick;
			_timer.Stop();
			Start();
		}
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
}
