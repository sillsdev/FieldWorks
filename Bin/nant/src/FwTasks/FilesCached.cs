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
// File: FilesCached.cs
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
	/// Determines if files are stored in FileCache.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TaskName("filescached")]
	public class FilesCached: Task
	{
		private string m_PropertyName;
		private string m_HandlePropertyName;
		private string m_Parameters;
		private FileSet m_Files = new FileSet();

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the name of the property.
		/// </summary>
		/// <value>The property.</value>
		/// ------------------------------------------------------------------------------------------
		[TaskAttribute("property", Required=true)]
		[StringValidator(AllowEmpty=false)]
		public string PropertyName
		{
			get { return m_PropertyName; }
			set { m_PropertyName = value; }
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the name of the property that holds the handle to the file cache.
		/// </summary>
		/// <value>The property.</value>
		/// ------------------------------------------------------------------------------------------
		[TaskAttribute("handleproperty", Required = true)]
		[StringValidator(AllowEmpty = false)]
		public string HandlePropertyName
		{
			get { return m_HandlePropertyName; }
			set { m_HandlePropertyName = value; }
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the name of the optional parameters.
		/// </summary>
		/// <value>The parameters.</value>
		/// ------------------------------------------------------------------------------------------
		[TaskAttribute("parameters", Required = false)]
		public string Parameters
		{
			get { return m_Parameters; }
			set { m_Parameters = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the files.
		/// </summary>
		/// <value>The files.</value>
		/// ------------------------------------------------------------------------------------
		[BuildElement("files", Required=true)]
		public FileSet Files
		{
			get { return m_Files; }
			set { m_Files = value; }
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
				string[] includedFiles = new String[m_Files.FileNames.Count];
				m_Files.FileNames.CopyTo(includedFiles, 0);

				string handle = cacheManager.GetHash(m_Parameters, includedFiles);
				if (Properties.Contains(HandlePropertyName))
					Properties[HandlePropertyName] = handle;
				else
					Properties.Add(HandlePropertyName, handle);
				bool fCached = cacheManager.IsCached(handle);
				if (Properties.Contains(PropertyName))
					Properties[PropertyName] = fCached.ToString();
				else
					Properties.Add(PropertyName, fCached.ToString());
			}
		}
	}
}
