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
// File: CacheNewFiles.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using NAnt.Core.Attributes;
using NAnt.Core;
using NAnt.Core.Tasks;
using NAnt.Core.Types;

using SIL.FieldWorks.Tools;

namespace SIL.FieldWorks.Build.Tasks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TaskName("cachenewfiles")]
	public class CacheNewFiles: Task
	{
		private FileSet m_OutputFiles = new FileSet();
		private string m_HandlePropertyName;

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the name of the property that holds the handle to the file cache.
		/// </summary>
		/// <value>The property.</value>
		/// ------------------------------------------------------------------------------------------
		[TaskAttribute("handle", Required = true)]
		[StringValidator(AllowEmpty = false)]
		public string HandlePropertyName
		{
			get { return m_HandlePropertyName; }
			set { m_HandlePropertyName = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the output files.
		/// </summary>
		/// <value>The output files.</value>
		/// ------------------------------------------------------------------------------------
		[BuildElement("output", Required = true)]
		public FileSet Output
		{
			get { return m_OutputFiles; }
			set { m_OutputFiles = value; }
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Executes the task.
		/// </summary>
		/// ------------------------------------------------------------------------------------------
		protected override void ExecuteTask()
		{
			using (CacheManager cacheManager = new CacheManager())
			{
				string handle = Properties[HandlePropertyName];

				string[] outputFiles = new string[m_OutputFiles.FileNames.Count];
				m_OutputFiles.FileNames.CopyTo(outputFiles, 0);
				cacheManager.CacheFile(handle, outputFiles);
				cacheManager.AddCacheMiss();
			}
		}
	}
}
