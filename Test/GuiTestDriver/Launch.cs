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
// File: Launch.cs
// Responsibility: LastufkaM
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;
using SIL.FieldWorks.Common.Utils;
using System.Windows.Forms;

namespace GuiTestDriver
{
	/// <summary>
	/// Summary description for Launch.
	/// </summary>
	public class Launch : ActionBase
	{
		string m_name;
		string m_path;
		string m_exeName;
		Process m_proc;

		public Launch()
		{
			m_name = null;
			m_path = null;
			m_exeName = null;
			m_proc = null;
		}
		public string Name
		{
			get {return m_name;}
			set {m_name = value;}
		}
		public string Path
		{
			get {return m_path;}
			set {m_path = value;}
		}
		public string Exe
		{
			get {return m_exeName;}
			set {m_exeName = value;}
		}
		public Process Process
		{
			get {return m_proc;}
		}
		public override void Execute(TestState ts)
		{
			bool fStarted = true;
			string fullPath = m_path + @"\" + m_exeName;
			// if the app is already running, close it first
			AppHandle app = Application;
			if (app != null && app.ExePath == fullPath)
			{
				if (app.Process != null)
				{
					app.Process.Kill();
					app.Process.WaitForExit(3000);
				}
			}

			m_proc = Process.Start(fullPath);
			if (m_proc == null)
				fStarted = false;

			m_proc.WaitForInputIdle();
			while (Process.GetProcessById(m_proc.Id).MainWindowHandle == IntPtr.Zero)
				Thread.Sleep(100);
			if (m_proc.HasExited)
				fStarted = false;

			m_proc.WaitForInputIdle();
			Win32.SetForegroundWindow(m_proc.MainWindowHandle);

			Assertion.AssertEquals(true, fStarted);
			Assertion.AssertNotNull("Null process", m_proc);
			Assertion.AssertNotNull("Null window handle", m_proc.MainWindowHandle);
		}
	}
}
