// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: MakeMSMFileListsTask.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;

using NAnt.Core.Attributes;
using NAnt.Core;
using NAnt.Core.Tasks;
using NAnt.Core.Types;
using NAnt.Core.Util;
namespace SIL.FieldWorks.Build.Tasks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for MakeMSMFileListsTask.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TaskName("makemsmfilelists")]
	public class MakeMSMFileListsTask : CopyAsmRefsTask
	{
		private int m_currentAssemblyCollection = 0;
		private ArrayList m_assemblyNamesCollection = new ArrayList();
		private Hashtable m_assembliesCount = new Hashtable();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the current collection of assembly names.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override StringCollection AssemblyNames
		{
			get
			{
				return (StringCollection)m_assemblyNamesCollection[m_currentAssemblyCollection];
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void ExecuteTask()
		{
			string oldDir = Directory.GetCurrentDirectory();
			string baseDir = CopyFileSet.BaseDirectory.FullName;
			try
			{
				Directory.SetCurrentDirectory(baseDir);
				foreach (string filename in CopyFileSet.FileNames)
				{
					StringCollection asmNames = new StringCollection();
					m_assemblyNamesCollection.Add(asmNames);
					ProcessAssembly(filename);
					m_currentAssemblyCollection++;
				}

				// Create directory if not present
				if (!Directory.Exists(ToDirectory))
				{
					Directory.CreateDirectory(ToDirectory);
					Log(Level.Verbose, "Created directory: {0}", ToDirectory);
				}

				CountAssemblyReferencesAndReloadFileList();
				BuildAssemblyFileLists();
			}
			finally
			{
				Directory.SetCurrentDirectory(oldDir);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Iterate through each collection of assembly names to gather a count of references
		/// to each assembly.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CountAssemblyReferencesAndReloadFileList()
		{
			CopyFileSet.Reset();
			foreach (StringCollection asmCollection in m_assemblyNamesCollection)
			{
				foreach (string asmName in asmCollection)
				{
					if (m_assembliesCount.Contains(asmName))
						m_assembliesCount[asmName] = (int)m_assembliesCount[asmName] + 1;
					else
						m_assembliesCount.Add(asmName, 1);

					CopyFileSet.Includes.Add(asmName);

					if (CopyRelated)
						CopyFileSet.Includes.Add(Path.ChangeExtension(asmName, ".*"));
				}
			}
			CopyFileSet.Scan();

			if (Verbose)
			{
				Log(Level.Verbose, "List of assemblies with counts:");
				foreach (DictionaryEntry entry in m_assembliesCount)
				{
					Log(Level.Verbose, "{0} ({1})", entry.Key, entry.Value);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates text files containing the assemblies required for each merge module.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void BuildAssemblyFileLists()
		{
			StreamWriter commonFile = null;
			StreamWriter currFile = null;

			try
			{
				// build file lists for individual modules
				foreach (StringCollection asmCollection in m_assemblyNamesCollection)
				{
					if (currFile != null)
					{
						currFile.Close();
						currFile = null;
					}

					foreach (string asmName in asmCollection)
					{
						// If this assembly is only found in one project, then add its
						// name to the list of assemblies for that project.
						if ((int)m_assembliesCount[asmName] == 1)
						{
							if (currFile == null)
							{
								currFile = new StreamWriter(Path.Combine(ToDirectory,
									Path.GetFileName(asmCollection[0]) + "-Modules.txt"), false);
							}

							AddFileName(currFile, asmName);
						}
					}
				}

				// build common file list
				foreach (DictionaryEntry entry in m_assembliesCount)
				{
					// If this assembly was found in more than one project, then add its
					// name to the list of common assemblies.
					if ((int)entry.Value > 1)
					{
						if (commonFile == null)
						{
							commonFile = new StreamWriter(Path.Combine(ToDirectory,
								"Common-Modules.txt"), false);
						}

						AddFileName(commonFile, (string)entry.Key);
					}
				}
			}
			finally
			{
				if (commonFile != null)
					commonFile.Close();

				if (currFile != null)
					currFile.Close();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the assembly name to the file list. If CopyRelated is set to true we also
		/// add the related files.
		/// </summary>
		/// <param name="currFile">File list stream</param>
		/// <param name="asmName">Current assembly name</param>
		/// ------------------------------------------------------------------------------------
		private void AddFileName(StreamWriter currFile, string asmName)
		{
			if (CopyFileSet.FileNames.Contains(asmName))
				currFile.WriteLine(asmName);

			if (CopyRelated)
			{
				//string ext = Path.GetExtension(asmName);
				string[] files = Directory.GetFiles(Path.GetDirectoryName(asmName),
					Path.GetFileNameWithoutExtension(asmName) + ".*");
				foreach (string file in files)
				{
					if (file != asmName && CopyFileSet.FileNames.Contains(file))
						currFile.WriteLine(file);
				}
			}
		}
	}
}
