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
using System.IO;

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
			get { return m_baseDirectory.FullName; }
			set { m_baseDirectory = new System.IO.DirectoryInfo(value); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the working directory for the application.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override DirectoryInfo BaseDirectory
		{
			get
			{
				if (m_baseDirectory == null)
				{
					return base.BaseDirectory;
				}
				return m_baseDirectory;
			}
			set { m_baseDirectory = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The name of the task.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string Name
		{
			get { return "resgen"; }
		}
	}
}
