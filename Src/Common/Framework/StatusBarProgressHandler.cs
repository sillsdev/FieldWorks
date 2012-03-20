// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: StatusBarProgressHandler.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;

using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;

namespace SIL.FieldWorks.Common.Framework
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Handles displaying progress in the status bar.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class StatusBarProgressHandler: IProgress, IFWDisposable
	{
		private readonly ToolStripStatusLabel m_label;
		private readonly ToolStripProgressBar m_progressBar;
		private Control m_control;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="StatusBarProgressHandler"/> class.
		/// </summary>
		/// <param name="label">The label that will display the message.</param>
		/// <param name="progressBar">The progress bar.</param>
		/// ------------------------------------------------------------------------------------
		public StatusBarProgressHandler(ToolStripStatusLabel label,
			ToolStripProgressBar progressBar)
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets a Message
		/// </summary>
		/// <value></value>
		/// <returns>A System.String</returns>
		/// ------------------------------------------------------------------------------------
		public string Message
		{
			get
			{
				if (m_label != null)
				{
					if (m_control.InvokeRequired)
						return (string) m_control.Invoke((Func<string>) (() => m_label.Text));
					return m_label.Text;
				}
				return null;
			}

			set
			{
				if (m_label != null)
				{
					if (m_control.InvokeRequired)
						m_control.BeginInvoke((Action<string>) (s => m_label.Text = s), value);
					else
						m_label.Text = value;
				}
			}
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
			get
			{
				if (m_progressBar != null)
				{
					if (m_control.InvokeRequired)
						return (int) m_control.Invoke((Func<int>) (() => m_progressBar.Value));
					return m_progressBar.Value;
				}
				return -1;
			}

			set
			{
				if (m_progressBar != null)
				{
					if (m_control.InvokeRequired)
						m_control.BeginInvoke((Action<int>) (i => m_progressBar.Value = i), value);
					else
						m_progressBar.Value = value;
				}
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
				if (m_progressBar != null)
				{
					if (m_control.InvokeRequired)
						return (int) m_control.Invoke((Func<int>) (() => m_progressBar.Minimum));
					return m_progressBar.Minimum;
				}
				return -1;
			}

			set
			{
				if (m_progressBar != null)
				{
					if (m_control.InvokeRequired)
						m_control.BeginInvoke((Action<int>) (i => m_progressBar.Minimum = i), value);
					else
						m_progressBar.Minimum = value;
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
				if (m_progressBar != null)
				{
					if (m_control.InvokeRequired)
						return (int)m_control.Invoke((Func<int>)(() => m_progressBar.Maximum));
					return m_progressBar.Maximum;
				}
				return -1;
			}

			set
			{
				if (m_progressBar != null)
				{
					if (m_control.InvokeRequired)
						m_control.BeginInvoke((Action<int>)(i => m_progressBar.Maximum = i), value);
					else
						m_progressBar.Maximum = value;
				}
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether the task has been canceled.
		/// </summary>
		/// <value><c>true</c> if canceled; otherwise, <c>false</c>.</value>
		public bool Canceled
		{
			get { return false; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the progress as a form (used for message box owners, etc).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "FindForm() returns a reference")]
		public Form Form
		{
			get { return m_progressBar.Control.FindForm(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Member Step
		/// </summary>
		/// <param name="nStepAmt">nStepAmt</param>
		/// ------------------------------------------------------------------------------------
		public void Step(int nStepAmt)
		{
			if (m_progressBar == null)
				return;

			if (m_control.InvokeRequired)
			{
				m_control.BeginInvoke((Action<int>) Step, nStepAmt);
			}
			else
			{
				if (nStepAmt == 0)
					m_progressBar.PerformStep();
				else
				{
					if (m_progressBar.Value + nStepAmt > m_progressBar.Maximum)
					{
						m_progressBar.Value = m_progressBar.Minimum +
							(m_progressBar.Value + nStepAmt - m_progressBar.Maximum);
					}
					else
						m_progressBar.Value += nStepAmt;
				}
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
			get
			{
				if (m_progressBar != null)
				{
					if (m_control.InvokeRequired)
						return (int) m_control.Invoke((Func<int>) (() => m_progressBar.Step));
					return m_progressBar.Step;
				}
				return -1;
			}

			set
			{
				if (m_progressBar != null)
				{
					if (m_control.InvokeRequired)
						m_control.BeginInvoke((Action<int>) (i => m_progressBar.Step = i), value);
					else
						m_progressBar.Step = value;
				}
			}
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
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		#endregion

		#region Disposing stuff
		#region IFWDisposable Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method throws an ObjectDisposedException if IsDisposed returns
		/// true.  This is the case where a method or property in an object is being
		/// used but the object itself is no longer valid.
		/// This method should be added to all public properties and methods of this
		/// object and all other objects derived from it (extensive).
		/// </summary>
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
		/// Add the public property for knowing if the object has been disposed of yet
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public bool IsDisposed { get; private set; }

		#endregion

		#region IDisposable Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or
		/// resetting unmanaged resources.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		#endregion

#if DEBUG
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Releases unmanaged resources and performs other cleanup operations before the
		/// <see cref="T:SIL.FieldWorks.Common.Framework.StatusBarProgressHandler"/> is
		/// reclaimed by garbage collection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		~StatusBarProgressHandler()
		{
			Dispose(false);
		}
#endif

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Dispose method
		/// </summary>
		/// <param name="fFromDispose">set to <c>true</c> if it is save to access managed
		/// member variables, <c>false</c> if only unmanaged member variables should be
		/// accessed.</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void Dispose(bool fFromDispose)
		{
			System.Diagnostics.Debug.WriteLineIf(!fFromDispose, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");

			if (fFromDispose)
			{
				if (m_control != null)
					m_control.Dispose();
			}
			m_control = null;

			IsDisposed = true;
		}
		#endregion
	}
}
