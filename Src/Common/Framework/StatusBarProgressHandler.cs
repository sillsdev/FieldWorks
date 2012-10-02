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
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.Common.Framework
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Handles displaying progress in the status bar.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class StatusBarProgressHandler: IAdvInd3, IFWDisposable
	{
		private ToolStripStatusLabel m_label;
		private ToolStripProgressBar m_progressBar;
		private Control m_control;

		private delegate void SetStringInvoke(string value);
		private delegate void SetIntInvoke(int value);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:StatusBarProgressHandler"/> class.
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

		#region IAdvInd3 Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets a Message
		/// </summary>
		/// <value></value>
		/// <returns>A System.String</returns>
		/// ------------------------------------------------------------------------------------
		public string Message
		{
			set
			{
				if (m_label != null)
				{
					if (m_control.InvokeRequired)
					{
						SetStringInvoke method = delegate(string s) { m_label.Text = s; };
						m_control.BeginInvoke(method, value);
					}
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
			set
			{
				if (m_progressBar != null)
				{
					if (m_control.InvokeRequired)
					{
						SetIntInvoke method = delegate(int i) { m_progressBar.Value = i; };
						m_control.BeginInvoke(method, value);
					}
					else
						m_progressBar.Value = value;
				}
			}
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
			if (m_progressBar == null)
				return;

			if (m_control.InvokeRequired)
			{
				SetIntInvoke method = delegate(int i) { m_progressBar.Minimum = i; };
				m_control.Invoke(method, nMin);
				method = delegate(int i) { m_progressBar.Maximum = i; };
				m_control.BeginInvoke(method, nMax);
			}
			else
			{
				m_progressBar.Minimum = nMin;
				m_progressBar.Maximum = nMax;
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
			if (m_progressBar == null)
				return;

			if (m_control.InvokeRequired)
			{
				m_control.BeginInvoke(new SetIntInvoke(Step), nStepAmt);
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
			set
			{
				if (m_progressBar == null)
					return;

				if (m_control.InvokeRequired)
				{
					SetIntInvoke method = delegate(int i) { m_progressBar.Step = i; };
					m_control.BeginInvoke(method, value);
				}
				else
					m_progressBar.Step = value;
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
			set { throw new Exception("StatusBarProgressHandler.Title is not implemented."); }
		}

		#endregion

		#region Disposing stuff
		#region IFWDisposable Members

		private bool m_fDisposed = false;

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
		public bool IsDisposed
		{
			get { return m_fDisposed; }
		}

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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Dispose method
		/// </summary>
		/// <param name="fFromDispose">set to <c>true</c> if it is save to access managed
		/// member variables, <c>false</c> if only unmanaged member variables should be
		/// accessed.</param>
		/// ------------------------------------------------------------------------------------
		protected void Dispose(bool fFromDispose)
		{
			if (fFromDispose)
			{
				if (m_control != null)
					m_control.Dispose();
			}
			m_control = null;

			m_fDisposed = true;
		}
		#endregion
	}
}
