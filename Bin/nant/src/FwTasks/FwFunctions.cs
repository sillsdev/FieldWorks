// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2008' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwFunctions.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using NAnt.Core;
using NAnt.Core.Types;
using NAnt.Core.Attributes;
using System.Security.Principal;

namespace SIL.FieldWorks.Build.Tasks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[FunctionSet("fw", "fw")]
	public class FwFunctions: FunctionSetBase
	{
		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FwFunctions"/> class.
		/// </summary>
		/// <param name="project">The project.</param>
		/// <param name="properties">The properties.</param>
		/// ------------------------------------------------------------------------------------
		public FwFunctions(Project project, PropertyDictionary properties)
			: base(project, properties)
		{
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the architecture name of the machine
		/// </summary>
		/// <returns><c>true</c> if current user has administrator privileges, otherwise
		/// <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		[Function("architecture")]
		public string GetArchitecture()
		{
			if (Environment.OSVersion.Platform == PlatformID.Unix)
			{
				// Unix - return output from 'uname -m'
				using (Process process = new Process())
				{
					process.StartInfo.FileName = "uname";
					process.StartInfo.Arguments = "-m";
					process.StartInfo.UseShellExecute = false;
					process.StartInfo.CreateNoWindow = true;
					process.StartInfo.RedirectStandardOutput = true;
					process.Start();

					string architecture = process.StandardOutput.ReadToEnd().Trim();

					process.StandardOutput.Close();
					process.Close();
					return architecture;
				}
			}
			else
			{
				// treat it as Windows
				if (IntPtr.Size == 8)
					return "Win64";
				return "Win32";
			}
		}
	}
}
