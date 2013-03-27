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
// File: ResGenTaskEx.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using NAnt.Core.Attributes;
using NAnt.DotNet.Tasks;
using System;
using System.IO;
using System.Diagnostics;

namespace SIL.FieldWorks.Build.Tasks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Extended ResGenTask that allows to set a working/base directory to locate embedded
	/// resources.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TaskName("resgenex")]
	public class ResGenTaskEx : ResGenTask
	{
		private DirectoryInfo m_baseDirectory;
		string m_exepath;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The base/working directory.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TaskAttribute("basedir", Required = false)]
		public string BaseDir
		{
			get { return m_baseDirectory == null ? null : m_baseDirectory.FullName; }
			set { m_baseDirectory = new System.IO.DirectoryInfo(value); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the name of the program executed by this task.  On Linux, only the full pathname
		/// seems to work.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ExeName
		{
			get
			{
				if (Environment.OSVersion.Platform == PlatformID.Unix)
				{
					if (m_exepath == null)
						ComputeResGenPath();
					return m_exepath;
				}
				else
				{
					return "resgen";
				}
			}
		}

		/// <summary>
		/// Get the full path to the resgen program.
		/// </summary>
		private void ComputeResGenPath()
		{
			string path = Environment.GetEnvironmentVariable("PATH");
			if (!String.IsNullOrEmpty(path))
			{
				var dirs = path.Split(new char[] { Path.PathSeparator });
				for (int i = 0; i < dirs.Length; ++i)
				{
					var f = Path.Combine(dirs[i], "resgen");
					if (File.Exists(f))
					{
						// Check for executable?
						m_exepath = f;
						return;
					}
				}
			}
			m_exepath = "resgen";
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the name of this task.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string Name
		{
			get
			{
				if (Environment.OSVersion.Platform == PlatformID.Unix)
					return "resgenex";
				else
					return "resgen";
			}
		}

		/// <summary>
		/// Set the working directory for the process to our very own basedir task attribute.
		/// </summary>
		protected override void PrepareProcess(Process process)
		{
			base.PrepareProcess(process);
			var dir = BaseDir;
			if (dir != null && Directory.Exists(dir))
				process.StartInfo.WorkingDirectory = dir;
		}
	}
}
