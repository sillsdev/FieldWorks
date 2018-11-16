// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Implements helper methods for invoking tasks on a specific thread, namely the thread this
	/// class was created on (which should typically be the main UI thread).
	/// </summary>
	/// <remarks>Unfortunately we have to create a Control, so we have to implement IDisposable
	/// to get rid of it again.</remarks>
	public sealed class ThreadHelper : ISynchronizeInvoke, IDisposable
	{
		/// <summary>Control to invoke methods that need to be run on this
		/// thread, but are called from another thread.</summary>
		private Control m_invokeControl;

		/// <summary>
		/// Initializes a new instance of the <see cref="ThreadHelper"/> class. Any calls to
		/// Invoke will be executed on the thread this class is created on, so this should
		/// typically be called on the main UI thread.
		/// </summary>
		public ThreadHelper()
		{
			m_invokeControl = new Control();
			m_invokeControl.CreateControl();
		}

		#region Disposable stuff

		/// <summary/>
		~ThreadHelper()
		{
			Dispose(false);
		}

		/// <summary/>
		private bool IsDisposed { get; set; }

		/// <inheritdoc />
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary />
		private void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + " *******");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				// dispose managed objects
				if (m_invokeControl != null)
				{
					if (m_invokeControl.IsHandleCreated && InvokeRequired)
					{
						// I (RandyR) have seen cases where the handle is destroyed, before this point,
						// which then throws when the Invoke is done.
						Invoke((MethodInvoker)(m_invokeControl.Dispose));
					}
					else
					{
						m_invokeControl.Dispose();
					}
				}
			}
			m_invokeControl = null;

			IsDisposed = true;
		}
		#endregion

		/// <inheritdoc />
		public IAsyncResult BeginInvoke(Delegate method, params object[] args)
		{
			return m_invokeControl.BeginInvoke(method, args);
		}

		/// <inheritdoc />
		public object EndInvoke(IAsyncResult result)
		{
			return m_invokeControl.EndInvoke(result);
		}

		/// <inheritdoc />
		public object Invoke(Delegate method, params object[] args)
		{
			return m_invokeControl.Invoke(method, args);
		}

		/// <inheritdoc />
		public bool InvokeRequired => m_invokeControl.InvokeRequired;

		/// <summary>
		/// Displays a message box with the specified owner. If owner is null, then the currently
		/// active form is used. Any invoking that is required is handled.
		/// </summary>
		public static DialogResult ShowMessageBox(Form owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
		{
			// ENHANCE (TimS): From what I understand of Mono, it needs all forms to be shown on the
			// main UI thread. Using the owner or the active form should always be safe since they
			// were theoretically created on the main UI thread since Mono requires it.
			// However, there may be a problem when showing the message box without an owner
			// (i.e. realOwner == null) as it may attempt to show the message box on the
			// current thread. If this is a problem, we might have to make this method non-static
			// and invoke the showing of the message box using Invoke() on this ThreadHelper.
			var realOwner = owner ?? Form.ActiveForm;
			if (realOwner == null)
			{
				return MessageBox.Show(text, caption, buttons, icon);
			}
			return (DialogResult)realOwner.Invoke((Func<DialogResult>)(() => MessageBox.Show(owner, text, caption, buttons, icon)));
		}
	}
}