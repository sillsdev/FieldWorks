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
// File: DelReferencesTask.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using NAnt.Core.Attributes;
using NAnt.Core;
using NAnt.Core.Tasks;

namespace SIL.FieldWorks.Build.Tasks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Delete references
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TaskName("delrefs")]
	public class DelReferencesTask: CopyReferencesTask
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new instance of the <see cref="DelReferencesTask"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DelReferencesTask()
		{
			m_fForce = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Executes the Copy task.
		/// </summary>
		/// <exception cref="BuildException">A file that has to be deleted could not be
		/// deleted.</exception>
		/// ------------------------------------------------------------------------------------
		protected override void ExecuteTask()
		{
			if (SourceFile != null)
			{
				throw new BuildException("Deleting of a single file not supported. Use " +
					"the file set instead of SourceFile", Location);
			}
			else
			{
				base.ExecuteTask();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Does the actual copying/deleting of files
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void DoFileOperations()
		{
			int nFiles = FileCopyMap.Values.Count;
			if (nFiles > 0 || Verbose)
			{
				Log(Level.Info, "Deleting {0} file{1}", nFiles,
					( nFiles != 1 ) ? "s" : "");

				foreach (string fileName in FileCopyMap.Values)
				{
					try
					{
						Log(Level.Verbose, "Deleting file {0}", fileName);
						FileAttributes attr = File.GetAttributes(fileName);
						File.SetAttributes(fileName, attr & ~FileAttributes.ReadOnly);
						File.Delete(fileName);
					}
					catch(Exception e)
					{
						Log(Level.Verbose, "Can't delete file {0}", fileName);
						if (FailOnError)
							throw new BuildException(string.Format("Can't delete file {0}",
								fileName), Location, e);
					}
				}
			}
		}

	}
}
