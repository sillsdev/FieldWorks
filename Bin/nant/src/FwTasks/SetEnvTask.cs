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
// File: SetEnvTask.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Types;
using System.Text;

namespace SIL.FieldWorks.Build.Tasks
{
	/// <summary>
	/// Summary description for SetEnvTask.
	/// </summary>
	/// <developernote>This task hides the setenv task provided with NAnt</developernote>
	[TaskName("setenv")]
	public class SetEnvTask: Task
	{
		private string m_name;
		private string m_value;
		private bool m_fGlobal;
		private EnvironmentVariableCollection m_environmentVariables =
			new EnvironmentVariableCollection();
		private List<string> m_VariablesToSet = new List<string>();

		/// <summary>The name of the environment variable</summary>
		[TaskAttribute("name")]
		public string VariableName
		{
			get { return m_name; }
			set { m_name = value; }
		}

		/// <summary>The name of the environment variable</summary>
		[TaskAttribute("value")]
		public string Value
		{
			get { return m_value; }
			set { m_value = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the environment variables.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[BuildElementArray("variable", ElementType = typeof(EnvironmentVariable))]
		public EnvironmentVariableCollection EnvironmentVariables
		{
			get { return m_environmentVariables; }
			set { m_environmentVariables = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If <c>true</c> make the changes for the entire system, otherwise only for the
		/// current build. Default is <c>false</c>.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TaskAttribute("global")]
		[BooleanValidator]
		public bool Global
		{
			get { return m_fGlobal; }
			set { m_fGlobal = value; }
		}

		[DllImport("kernel32.dll", CharSet=CharSet.Auto, SetLastError=true)]
		[return:MarshalAs(UnmanagedType.Bool)]
		private static extern bool SetEnvironmentVariable(
			string lpName,
			string lpValue);

		[DllImport("user32.dll", CharSet=CharSet.Auto, SetLastError=true)]
		[return:MarshalAs(UnmanagedType.Bool)]
		private static extern bool SendMessageTimeout(
			IntPtr hWnd,
			int Msg,
			int wParam,
			string lParam,
			int fuFlags,
			int uTimeout,
			out int lpdwResult
			);

		private const int HWND_BROADCAST = 0xffff;
		private const int WM_SETTINGCHANGE = 0x001A;
		private const int SMTO_NORMAL = 0x0000;
		private const int SMTO_BLOCK = 0x0001;
		private const int SMTO_ABORTIFHUNG = 0x0002;
		private const int SMTO_NOTIMEOUTIFNOTHUNG = 0x0008;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Do the job
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void ExecuteTask()
		{
			try
			{
				if (VariableName != null && Value != null)
				{
					// add single environment variable
					EnvironmentVariables.Add(new EnvironmentVariable(VariableName, Value));
				}

				foreach (EnvironmentVariable env in EnvironmentVariables)
				{
					SetSingleEnvironmentVariable(env.VariableName, env.Value);
				}

				if (m_fGlobal)
					SetGlobalEnvironmentVariables();
			}
			catch (Exception e)
			{
				BuildException buildEx = e as BuildException;
				if (buildEx != null)
				{
					throw new BuildException(
						string.Format("Exception setting environment variable {1}: {0}", e.Message,
						buildEx.Data["Name"]), Location, e);
				}
				else
					throw new BuildException(
						string.Format("Exception setting environment variables: {0}", e.Message),
						Location, e);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets a single environment variable.
		/// </summary>
		/// <param name="variableName">Name of the variable.</param>
		/// <param name="value">The value.</param>
		/// ------------------------------------------------------------------------------------
		private void SetSingleEnvironmentVariable(string variableName, string value)
		{
			Log(Level.Verbose, "Setting environment variable {0} to {1}",
				variableName, value);

			// expand any env vars in value
			string expandedValue = Environment.ExpandEnvironmentVariables(value);

			bool fSuccess = SetEnvironmentVariable(variableName, expandedValue);
			if (fSuccess && m_fGlobal)
			{
				m_VariablesToSet.Add(string.Format("\"{0}={1}\"", variableName, expandedValue));
			}

			if (!fSuccess)
			{
				int nError = Marshal.GetLastWin32Error();
				string msg = string.Format("Error {0} setting environment variable {1}",
					nError, variableName);
				if (FailOnError)
				{
					BuildException ex = new BuildException(msg, Location);
					ex.Data.Add("Name", variableName);
					throw ex;
				}

				Log(Level.Info, msg);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the global environment variables.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetGlobalEnvironmentVariables()
		{
			StringBuilder bldr = new StringBuilder();
			foreach (string str in m_VariablesToSet)
			{
				if (bldr.Length > 0)
					bldr.Append(" ");
				bldr.Append(str);
			}

			if (bldr.Length > 0)
			{
				// Set the variable in an external app so that it can display the UAC dialog
				// on Vista
				Process process = new Process();
				process.StartInfo.FileName = Path.Combine(Path.GetDirectoryName(
					System.Reflection.Assembly.GetExecutingAssembly().Location), "SetEnvHelper.exe");
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.Arguments = bldr.ToString();
				process.Start();
				process.WaitForExit();

				int nResult;
				SendMessageTimeout((System.IntPtr)HWND_BROADCAST,
					WM_SETTINGCHANGE, 0, "Environment", SMTO_BLOCK | SMTO_ABORTIFHUNG
					| SMTO_NOTIMEOUTIFNOTHUNG, 5, out nResult);
			}
		}
	}
}
