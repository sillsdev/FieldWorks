// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ThreadHelper.cs
// Responsibility: FW TEam
// --------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;

namespace SIL.Utils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Implements helper methods for invoking tasks on a specific thread, namely the thread this
	/// class was created on (which should typically be the main UI thread).
	/// </summary>
	/// <remarks>Unfortunately we have to create a Control, so we have to implement IDisposable
	/// to get rid of it again.</remarks>
	/// ----------------------------------------------------------------------------------------
	public sealed class ThreadHelper: IDisposable
	{
		/// <summary>Control to invoke methods that need to be run on this
		/// thread, but are called from another thread.</summary>
		private readonly Control m_invokeControl;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ThreadHelper"/> class. Any calls to
		/// Invoke will be executed on the thread this class is created on, so this should
		/// typically be called on the main UI thread.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ThreadHelper()
		{
			m_invokeControl = new Control();
			m_invokeControl.CreateControl();
		}

		#region Disposable stuff
		#if DEBUG
		/// <summary/>
		~ThreadHelper()
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
		private void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType() + " *******");
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects
				Invoke(() => m_invokeControl.Dispose());
			}
			IsDisposed = true;
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether an invoke is required from the calling thread. You
		/// don't normally need to check this, since Invoke takes care of doing it if needed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool InvokeRequired
		{
			get { return m_invokeControl.InvokeRequired; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Invokes the specified action synchronously on the thread on which this ThreadHelper
		/// was created. If the calling thread is the thread on which this ThreadHelper was
		/// created, no invoke is performed.
		/// </summary>
		/// <param name="action">The action.</param>
		/// ------------------------------------------------------------------------------------
		public void Invoke(Action action)
		{
			if (m_invokeControl.InvokeRequired)
			{
				m_invokeControl.Invoke(action);
				return;
			}
			action();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Invokes the specified function synchronously on the thread on which this
		/// ThreadHelper was created. If the calling thread is the thread on which this
		/// ThreadHelper was created, no invoke is performed.
		/// </summary>
		/// <param name="func">The function.</param>
		/// ------------------------------------------------------------------------------------
		public TResult Invoke<TResult>(Func<TResult> func)
		{
			if (m_invokeControl.InvokeRequired)
				return (TResult)m_invokeControl.Invoke(func);
			return func();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Invokes the specified action, asynchronously or not on the thread on which this
		/// ThreadHelper was created
		/// </summary>
		/// <param name="invokeAsync">if set to <c>true</c> invoke asynchronously.</param>
		/// <param name="action">The action to perform.</param>
		/// ------------------------------------------------------------------------------------
		public void Invoke(bool invokeAsync, Action action)
		{
			if (invokeAsync)
				InvokeAsync(action);
			else
				Invoke(action);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Executes the specified method asynchronously. The action will typically be called
		/// when the the Application.Run() loop regains control or the next call to
		/// Application.DoEvents() at some unspecified time in the future.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void InvokeAsync(Action action)
		{
			if (m_invokeControl.IsHandleCreated)
				m_invokeControl.BeginInvoke(action);
			else
			{
				// not ideal, but better than crashing
				action();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Executes the specified method asynchronously. The action will typically be called
		/// when the the Application.Run() loop regains control or the next call to
		/// Application.DoEvents() at some unspecified time in the future.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void InvokeAsync<T>(Action<T> action, T param1)
		{
			if (m_invokeControl.IsHandleCreated)
				m_invokeControl.BeginInvoke(action, param1);
			else
			{
				// not ideal, but better than crashing
				action(param1);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Executes the specified method asynchronously. The action will typically be called
		/// when the the Application.Run() loop regains control or the next call to
		/// Application.DoEvents() at some unspecified time in the future.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void InvokeAsync<T1, T2>(Action<T1, T2> action, T1 param1, T2 param2)
		{
			m_invokeControl.BeginInvoke(action, param1, param2);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Executes the specified method asynchronously. The action will typically be called
		/// when the the Application.Run() loop regains control or the next call to
		/// Application.DoEvents() at some unspecified time in the future.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void InvokeAsync<T1, T2, T3>(Action<T1, T2, T3> action, T1 param1, T2 param2, T3 param3)
		{
			m_invokeControl.BeginInvoke(action, param1, param2, param3);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Displays a message box with the specified owner. If owner is null, then the currently
		/// active form is used. Any invoking that is required is handled.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static DialogResult ShowMessageBox(Form owner, string text, string caption,
			MessageBoxButtons buttons, MessageBoxIcon icon)
		{
			// ENHANCE (TimS): From what I understand of Mono, it needs all forms to be shown on the
			// main UI thread. Using the owner or the active form should always be safe since they
			// were theoretically created on the main UI thread since Mono requires it.
			// However, there may be a problem when showing the message box without an owner
			// (i.e. realOwner == null) as it may attempt to show the message box on the
			// current thread. If this is a problem, we might have to make this method non-static
			// and invoke the showing of the message box using Invoke() on this ThreadHelper.
			Form realOwner = owner ?? Form.ActiveForm;
			if (realOwner == null)
				return MessageBox.Show(text, caption, buttons, icon);
			return (DialogResult)realOwner.Invoke((Func<DialogResult>)(() =>
				 MessageBox.Show(owner, text, caption, buttons, icon)));
		}
	}
}
