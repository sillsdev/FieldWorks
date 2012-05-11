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
		/// Get the name of the program executed by this task.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ExeName
		{
			get
			{
				if (Environment.OSVersion.Platform == PlatformID.Unix)
				{
					if (File.Exists("/usr/local/bin/resgen"))
						return "/usr/local/bin/resgen";
					else
						return "/usr/bin/resgen";
				}
				else
				{
					return "resgen";
				}
			}
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
