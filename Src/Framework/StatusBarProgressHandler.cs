// Copyright (c) 2006-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Windows.Forms;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.Common.Framework
{
	/// <summary>
	/// Handles displaying progress in the status bar.
	/// </summary>
	public class StatusBarProgressHandler : IProgress, IDisposable
	{
		event CancelEventHandler IProgress.Canceling
		{
			add { throw new NotImplementedException(); }
			remove { throw new NotImplementedException(); }
		}

		private readonly ToolStripStatusLabel m_label;
		private readonly ToolStripProgressBar m_progressBar;
		private Control m_control;

		/// <summary />
		public StatusBarProgressHandler(ToolStripStatusLabel label, ToolStripProgressBar progressBar)
		{
			m_label = label;
			m_progressBar = progressBar;

			// Create a (invisible) control for multithreading purposes. We have to do this
			// because ToolStripStatusLabel and ToolStripProgressBar don't derive from Control
			// and so don't provide an implementation of Invoke.
			m_control = new Control();
			m_control.CreateControl();
		}

		#region IProgress Members

		/// <summary>
		/// Sets a Message
		/// </summary>
		public string Message
		{
			get
			{
				return m_label != null ? m_control.InvokeRequired ? (string)m_control.Invoke((Func<string>)(() => m_label.Text)) : m_label.Text : null;
			}

			set
			{
				if (m_label != null)
				{
					if (m_control.InvokeRequired)
					{
						m_control.BeginInvoke((Action<string>)(s => m_label.Text = s), value);
					}
					else
					{
						m_label.Text = value;
					}
				}
			}
		}

		/// <summary>
		/// Sets a Position
		/// </summary>
		public int Position
		{
			get
			{
				return m_progressBar != null ? m_control.InvokeRequired ? (int)m_control.Invoke((Func<int>)(() => m_progressBar.Value)) : m_progressBar.Value : -1;
			}

			set
			{
				if (m_progressBar != null)
				{
					if (m_control.InvokeRequired)
					{
						m_control.BeginInvoke((Action<int>)(i => m_progressBar.Value = i), value);
					}
					else
					{
						m_progressBar.Value = value;
					}
				}
			}
		}

		/// <summary>
		/// Gets or sets the minimum value of the progress bar.
		/// </summary>
		public int Minimum
		{
			get
			{
				return m_progressBar != null ? m_control.InvokeRequired ? (int)m_control.Invoke((Func<int>)(() => m_progressBar.Minimum)) : m_progressBar.Minimum : -1;
			}

			set
			{
				if (m_progressBar != null)
				{
					if (m_control.InvokeRequired)
					{
						m_control.BeginInvoke((Action<int>)(i => m_progressBar.Minimum = i), value);
					}
					else
					{
						m_progressBar.Minimum = value;
					}
				}
			}
		}

		/// <summary>
		/// Gets or sets the maximum value of the progress bar.
		/// </summary>
		public int Maximum
		{
			get
			{
				return m_progressBar != null ? m_control.InvokeRequired ? (int)m_control.Invoke((Func<int>)(() => m_progressBar.Maximum)) : m_progressBar.Maximum : -1;
			}

			set
			{
				if (m_progressBar != null)
				{
					if (m_control.InvokeRequired)
					{
						m_control.BeginInvoke((Action<int>)(i => m_progressBar.Maximum = i), value);
					}
					else
					{
						m_progressBar.Maximum = value;
					}
				}
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether the task has been canceled.
		/// </summary>
		public bool Canceled => false;

		/// <summary>
		/// Gets an object to be used for ensuring that required tasks are invoked on the main
		/// UI thread.
		/// </summary>
		public ISynchronizeInvoke SynchronizeInvoke => m_progressBar.Control;

		/// <summary>
		/// Gets the progress as a form (used for message box owners, etc).
		/// </summary>
		public Form Form => m_progressBar.Control.FindForm();

		/// <summary>
		/// Gets or sets a value indicating whether this progress is indeterminate.
		/// </summary>
		public bool IsIndeterminate
		{
			get { return m_progressBar.Style == ProgressBarStyle.Marquee; }
			set { m_progressBar.Style = value ? ProgressBarStyle.Marquee : ProgressBarStyle.Blocks; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether the operation executing on the separate thread
		/// can be cancelled by a different thread (typically the main UI thread).
		/// </summary>
		public bool AllowCancel
		{
			get { return false; }
			set { throw new NotSupportedException(); }
		}

		/// <summary>
		/// Member Step
		/// </summary>
		public void Step(int nStepAmt)
		{
			if (m_progressBar == null)
			{
				return;
			}
			if (m_control.InvokeRequired)
			{
				m_control.BeginInvoke((Action<int>)Step, nStepAmt);
			}
			else
			{
				if (nStepAmt == 0)
				{
					m_progressBar.PerformStep();
				}
				else
				{
					if (m_progressBar.Value + nStepAmt > m_progressBar.Maximum)
					{
						m_progressBar.Value = m_progressBar.Minimum + (m_progressBar.Value + nStepAmt - m_progressBar.Maximum);
					}
					else
					{
						m_progressBar.Value += nStepAmt;
					}
				}
			}
		}

		/// <summary>
		/// Sets a StepSize
		/// </summary>
		public int StepSize
		{
			get
			{
				return m_progressBar != null ? m_control.InvokeRequired ? (int)m_control.Invoke((Func<int>)(() => m_progressBar.Step)) : m_progressBar.Step : -1;
			}

			set
			{
				if (m_progressBar != null)
				{
					if (m_control.InvokeRequired)
					{
						m_control.BeginInvoke((Action<int>)(i => m_progressBar.Step = i), value);
					}
					else
					{
						m_progressBar.Step = value;
					}
				}
			}
		}

		/// <summary>
		/// Sets a Title
		/// </summary>
		public string Title
		{
			get { throw new NotSupportedException(); }
			set { throw new NotSupportedException(); }
		}

		#endregion

		#region Disposing stuff

		/// <summary>
		/// Add the public property for knowing if the object has been disposed of yet
		/// </summary>
		public bool IsDisposed { get; private set; }

		#region IDisposable Members

		/// <inheritdoc />
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		#endregion

		/// <summary>
		/// Releases unmanaged resources and performs other cleanup operations before the
		/// <see cref="T:SIL.FieldWorks.Common.Framework.StatusBarProgressHandler"/> is
		/// reclaimed by garbage collection.
		/// </summary>
		~StatusBarProgressHandler()
		{
			Dispose(false);
		}

		/// <summary>
		/// Dispose method
		/// </summary>
		protected virtual void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");
			if (IsDisposed)
			{
				// No need to run it more than once
				return;
			}

			if (disposing)
			{
				m_control?.Dispose();
			}
			m_control = null;

			IsDisposed = true;
		}
		#endregion
	}
}