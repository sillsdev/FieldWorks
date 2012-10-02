// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: AppInteract.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Windows.Forms;
using SIL.Utils;
//using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.AcceptanceTests.Framework
{
	/// <summary>
	/// Methods for interaction with an application
	/// </summary>
	public class AppInteract
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="AppInteract"/> class.
		/// </summary>
		/// <param name="exe">Path to the executable</param>
		/// -----------------------------------------------------------------------------------
		public AppInteract(string exe)
		{
			m_ExePath = exe;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Starts the application
		/// </summary>
		/// <returns><c>true</c> if the executable was successfully started, otherwise
		/// <c>false</c></returns>.
		/// ------------------------------------------------------------------------------------
		public virtual bool Start()
		{
			m_proc = Process.Start(m_ExePath);
			if (m_proc == null)
				return false;
			m_proc.WaitForInputIdle();
			string name = m_proc.ProcessName;

			// Find the created process so the main window handle will be set
			Process[] activeProcesses = Process.GetProcessesByName(name);
			if (activeProcesses.Length == 0)
				return false;
			m_proc = activeProcesses[0];

			m_proc.WaitForInputIdle();
			Win32.SetForegroundWindow(m_proc.MainWindowHandle);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exits the application
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Exit()
		{
			Exit(false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exits the application
		/// </summary>
		/// <param name="fOnCurrentWindow">If <c>true</c> send Alt-F4 regardless of what window
		/// is active. Otherwise switch to the main window first.</param>
		/// ------------------------------------------------------------------------------------
		public void Exit(bool fOnCurrentWindow)
		{
			try
			{
				m_proc.WaitForInputIdle();
				if (!fOnCurrentWindow)
					Win32.SetForegroundWindow(m_proc.MainWindowHandle);

				// It works better if we send an ESC first
				m_proc.Close();
				m_proc.WaitForExit();
			}
			catch
			{
			}

			m_proc = null;
			m_AccHelper = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Send key strokes to the application.
		/// </summary>
		/// <param name="keys">Key strokes to send</param>
		/// <seealso cref="SendKeys"/>
		/// ------------------------------------------------------------------------------------
		public void SendKeys(string keys)
		{
			if (m_proc.HasExited)
				return;
			System.Windows.Forms.SendKeys.SendWait(keys);

			if (m_proc.HasExited)
				return;
			m_proc.WaitForInputIdle(30000);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the process associated with the application
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Process Process
		{
			get { return m_proc; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the <see cref="AccessibilityHelper"/> object for the main window.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public AccessibilityHelper MainAccessibilityHelper
		{
			get
			{
				if (m_AccHelper == null)
				{
					m_AccHelper = new AccessibilityHelper(m_proc.MainWindowHandle);
				}

				return m_AccHelper;
			}
			set
			{
				m_AccHelper = value;
			}
		}

		private string m_ExePath;
		private Process m_proc;
		private AccessibilityHelper m_AccHelper;
	}
}
