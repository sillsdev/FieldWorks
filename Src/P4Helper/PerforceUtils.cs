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
// File: PerforceUtils.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Windows.Forms;
using System.Diagnostics;


namespace P4Helper
{
	/// <summary>
	/// Summary description for PerforceUtils.
	/// </summary>
	public class PerforceUtils
	{
		protected System.Windows.Forms.TextBox m_console;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="PerforceUtils"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public PerforceUtils(System.Windows.Forms.TextBox console)
		{
			m_console= console;

		}

		public void ConnectSecurePort ()
		{
			Process p = new Process();
			p.StartInfo.CreateNoWindow  = false;
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.Arguments = @" /k %FWROOT%\bin\fwconnect.bat";
			p.StartInfo.RedirectStandardOutput = false;
			p.StartInfo.RedirectStandardError = false;
			p.StartInfo.FileName = System.Environment.ExpandEnvironmentVariables(@"%SystemRoot%\system32\cmd.exe");
			p.Start();
			if(p.WaitForExit(5000)) //did exit this fast
				System.Windows.Forms.MessageBox.Show("Could not connect up SSH.");
		}

		public bool IsP4Connected()
		{
			return (ExecutePerforce("info").IndexOf("Server root")>0);
		}

		protected string ExecutePerforce(string arguments)
		{
			string output = RunProcess("p4",arguments, false );
			return output;
		}

		private string RunProcess(string commandName, string arguments, bool useShell)
		{
			System.Windows.Forms.Cursor oldCursor = System.Windows.Forms.Cursor.Current;
			System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;

			Process p = new Process();
			p.StartInfo.CreateNoWindow  = !useShell;
			p.StartInfo.UseShellExecute = useShell;
			p.StartInfo.Arguments = arguments;
			p.StartInfo.RedirectStandardOutput = !useShell;
			p.StartInfo.RedirectStandardError = !useShell;
			p.StartInfo.FileName = commandName;
			p.Start();
			if(!useShell)
			{
				string output = p.StandardOutput.ReadToEnd();
				string error = p.StandardError.ReadToEnd();
				p.WaitForExit();
				WriteToConsole(error);
				WriteToConsole(output);
				System.Windows.Forms.Cursor.Current =  oldCursor;
				return output;
			}
			return "";
		}

		//does not currently distinguish between open for edit, delete, or add
		public string[] GetP4OpenFiles ()
		{
			string output =ExecutePerforce("opened");

			string[]lines = output.Split(new char[]{'\n'});
			ArrayList files = new ArrayList();
			foreach(string line in lines)
			{
				int i = line.IndexOf("#");//strip off "#4 - edit default change (text)"
				string file;

				if(i>0)
					file = line.Remove(i, line.Length - i);
				else
					file = line;
				file = file.Replace(@"//depot/WW", Environment.ExpandEnvironmentVariables("%fwroot%"));
				file = file.Replace(@"//depot/FW", Environment.ExpandEnvironmentVariables("%fwroot%"));
				file = file.Replace(@"//depot/fw", Environment.ExpandEnvironmentVariables("%fwroot%"));
				file = file.Replace(@"/", @"\");
				files.Add(file);
			}

			return (string []) files.ToArray(typeof(string));
		}

		public void CheckOut(string files)
		{
			string arguments = "edit "+files;

			ExecutePerforce(arguments);
		}

		public void Add(string files)
		{
			string arguments = "add "+files;

			ExecutePerforce(arguments);
		}

		public void SubmitDefault()
		{
			RunProcess(System.  Environment.ExpandEnvironmentVariables(@"%SystemRoot%\system32\cmd.exe"), @"set p4Editor=g:\ww\bin\p4edit.bat", true);

			Process p = new Process();
			//p.StartInfo.CreateNoWindow  = false;
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.Arguments = "submit";
			p.StartInfo.FileName = "p4";
			p.Start();
		}

		public void Diff(string files)
		{
			ExecutePerforce("diff -f		"+files);
		}

		public void Delete(string files)
		{
			ExecutePerforce("delete "+files);
		}

		public void WriteToConsole (string line)
		{
			m_console.Text += "\r\n"+line;
			m_console.Select(100000,1);
			m_console.ScrollToCaret();
		}
	}
}
