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
// File: CopyReferencesTask.cs
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
using NAnt.Core.Util;

namespace SIL.FieldWorks.Build.Tasks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Copy references to todir
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TaskName("copyrefs")]
	public class CopyReferencesTask: CopyTask
	{
		private ReferenceCache m_Cache = new ReferenceCache();
		private DirectoryInfo m_dstBaseInfo;
		private bool m_fResolveAssemblies = true;
		/// <summary>Set to <c>true</c> to force processing all files. Defaults to <c>false</c>.
		/// Used by the <see cref="DelReferencesTask"/>.</summary>
		protected bool m_fForce = false;
		private bool m_fAppend = false;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copying of assemblies that the referenced assemblies reference.
		/// Defaults to <c>true</c>.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TaskAttribute("resolveassemblies")]
		[BooleanValidator]
		public bool ResolveAssemblies
		{
			get { return m_fResolveAssemblies; }
			set { m_fResolveAssemblies = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set to true to append to the existing list of references for this assembly.
		/// Defaults to <c>false</c>.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TaskAttribute("append")]
		[BooleanValidator]
		public bool Append
		{
			get { return m_fAppend; }
			set { m_fAppend = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Executes the Copy task.
		/// </summary>
		/// <exception cref="BuildException">A file that has to be copied does not exist or
		/// could not be copied.</exception>
		/// ------------------------------------------------------------------------------------
		protected override void ExecuteTask()
		{
			if (SourceFile != null)
			{
				// Copy single file.
				base.ExecuteTask();
			}
			else
			{
				// Copy file set contents.

				if (m_fResolveAssemblies)
					LoadReferences();

				// get the complete path of the base directory of the fileset, ie, c:/work/nant/src
				m_dstBaseInfo = ToDirectory;

				XmlAssembly xmlAssembly;
				if (Append)
					xmlAssembly = m_Cache[Project.ProjectName];
				else
				{
					xmlAssembly = new XmlAssembly(Project.ProjectName);
					m_Cache.Add(xmlAssembly);
				}

				// if source file is not specified use fileset
				foreach (string pathname in CopyFileSet.FileNames)
				{
					FileInfo srcInfo = new FileInfo(pathname);
					if (srcInfo.Exists || m_fForce)
					{
						// The full filepath to copy to.
						string dstFilePath = Path.Combine(m_dstBaseInfo.FullName, Path.GetFileName(srcInfo.FullName));

						// do the outdated check
						FileInfo dstInfo = new FileInfo(dstFilePath);
						bool fOutdated = (!dstInfo.Exists) || (srcInfo.LastWriteTime > dstInfo.LastWriteTime);

						if (Overwrite || fOutdated || m_fForce)
						{
							if (!IsInGAC(srcInfo))
							{
								FileCopyMap[dstFilePath] =
									new FileDateInfo(srcInfo.FullName.ToLower(), srcInfo.LastWriteTime);
								if (dstInfo.Exists && dstInfo.Attributes != FileAttributes.Normal)
									File.SetAttributes( dstInfo.FullName, FileAttributes.Normal );
								AddAssemblyAndRelatedFiles(xmlAssembly, srcInfo.FullName);
							}
							else
								Log(Level.Verbose, "Reference file {0} skipped", srcInfo.FullName);

						}
						else
						{
							if (!IsInGAC(srcInfo))
								AddAssemblyAndRelatedFiles(xmlAssembly, srcInfo.FullName);
						}
					}
					else
					{
						if (FailOnError)
						{
							string msg = String.Format(CultureInfo.InvariantCulture, "Could not find file {0} to copy.", srcInfo.FullName);
							throw new BuildException(msg, Location);
						}
						else
							Log(Level.Error, "Reference file {0} does not exist", pathname);
					}
				}

				CreateDirectories(CopyFileSet.BaseDirectory);

				try
				{
					// do all the actual copy operations now...
					DoFileOperations();
				}
				catch
				{
					if (FailOnError)
						throw;
				}
				finally
				{
					// save the references of this project in our settings file
					if (m_fResolveAssemblies)
						SaveReferences();
				}
			}
		}

		private bool IsInGAC(FileInfo srcInfo)
		{
			try
			{
				AssemblyName assemblyName = AssemblyName.GetAssemblyName(srcInfo.FullName);
				Assembly assembly = Assembly.ReflectionOnlyLoad(assemblyName.FullName);
				if (assembly.GlobalAssemblyCache)
				{
					Log(Level.Debug, "Reference file {0} skipped because it is in the GAC", srcInfo.FullName);
					return true;
				}
			}
			catch
			{
				Log(Level.Debug, "Unable to load {0}", srcInfo.FullName);
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copy files and unset read-only attribute
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void DoFileOperations()
		{
			// loop thru our file list
			foreach (string dstPath in FileCopyMap.Keys)
			{
				Log(Level.Debug, "Ready to copy {0}", dstPath);
				try
				{
					FileAttributes fileAttributes = File.GetAttributes(dstPath);
					fileAttributes &= ~FileAttributes.ReadOnly;
					File.SetAttributes(dstPath, fileAttributes);
				}
				catch (FileNotFoundException)
				{
					// just ignore
				}
				catch (Exception e)
				{
					string msg = String.Format(CultureInfo.InvariantCulture,
						"Cannot set file attributes for '{0}'", dstPath);
					throw new BuildException(msg, Location, e);
				}
			}

			BaseDoFileOperations();
		}

		/// <summary>
		/// Actually does the file copies.
		/// </summary>
		protected virtual void BaseDoFileOperations()
		{
			int fileCount = FileCopyMap.Count;
			if (fileCount > 0 || Verbose)
			{
				if (ToFile != null)
				{
					Log(Level.Info, "Copying {0} file{1} to '{2}'.", fileCount, (fileCount != 1) ? "s" : "", ToFile);
				}
				else
				{
					Log(Level.Info, "Copying {0} file{1} to '{2}'.", fileCount, (fileCount != 1) ? "s" : "", ToDirectory);
				}

				// loop thru our file list
				foreach (string destinationFile in FileCopyMap.Keys)
				{
					Log(Level.Debug, "Next file to copy {0}", destinationFile);
					string sourceFile =
						((FileDateInfo)FileCopyMap[destinationFile]).Path;

					if (sourceFile.ToLowerInvariant() == destinationFile.ToLowerInvariant())
					{
						Log(Level.Verbose, "Skipping self-copy of '{0}'.", sourceFile);
						continue;
					}

					try
					{
						Log(Level.Verbose, "Copying '{0}' to '{1}'.", sourceFile, destinationFile);

						// create directory if not present
						string destinationDirectory = Path.GetDirectoryName(destinationFile);
						if (!Directory.Exists(destinationDirectory))
						{
							Directory.CreateDirectory(destinationDirectory);
							Log(Level.Verbose, "Created directory '{0}'.", destinationDirectory);
						}

						// copy the file with filters
						Log(Level.Debug, "Calling File.Copy on {0}", destinationFile);
						File.Copy(sourceFile, destinationFile, true);
					}
					catch
					{
						Log(Level.Error, "Exception on {0}", destinationFile);
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add the referenced assembly to the list of assemblies, resolve any referenced
		/// assemblies, and add the XML and PDB files.
		/// </summary>
		/// <param name="xmlAssembly">The assembly</param>
		/// <param name="referenceName">The name (and path) of the referenced assembly</param>
		/// ------------------------------------------------------------------------------------
		private void AddAssemblyAndRelatedFiles(XmlAssembly xmlAssembly, string referenceName)
		{
			string srcFilePath;
			if (referenceName.StartsWith(ToDirectory.FullName))
				srcFilePath = referenceName.Substring(ToDirectory.FullName.Length+1);
			else
				srcFilePath = referenceName;

			xmlAssembly.Add(srcFilePath);
			if (m_fResolveAssemblies)
				ResolveReferences(referenceName);

			AddFile(referenceName, "xml");
			AddFile(referenceName, "pdb");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create any specified directories that weren't created during the copy (ie: empty
		/// directories).
		/// </summary>
		/// <param name="srcBaseInfo">Base directory</param>
		/// ------------------------------------------------------------------------------------
		private void CreateDirectories(DirectoryInfo srcBaseInfo)
		{
			// Create any specified directories that weren't created during the copy (ie: empty directories)
			foreach (string pathname in CopyFileSet.DirectoryNames)
			{
				DirectoryInfo srcInfo = new DirectoryInfo(pathname);
				string dstRelPath;
				if (srcInfo.FullName.IndexOf(srcBaseInfo.FullName) > 0)
				{
					dstRelPath = srcInfo.FullName.Substring(srcBaseInfo.FullName.Length);
					if(dstRelPath.Length > 0 && dstRelPath[0] == Path.DirectorySeparatorChar )
					{
						dstRelPath = dstRelPath.Substring(1);
					}
				}
				else
					dstRelPath = srcInfo.FullName;

				// The full filepath to copy to.
				string dstPath = Path.Combine(m_dstBaseInfo.FullName, dstRelPath);
				if (!Directory.Exists(dstPath))
				{
					Log(Level.Verbose, "Created directory {0}", dstPath);
					Directory.CreateDirectory(dstPath);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the reference cache file
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private string ReferenceCacheName
		{
			get
			{
				return Path.Combine(Project.BaseDirectory, "references.xml");
			}
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save the reference cache
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SaveReferences()
		{
			XmlSerializer serializer = new XmlSerializer(typeof(ReferenceCache));
			TextWriter writer = new StreamWriter(ReferenceCacheName);
			try
			{
				serializer.Serialize(writer, m_Cache);
			}
			catch(Exception e)
			{
				System.Diagnostics.Debug.WriteLine("Exception: " + e.Message);
			}
			finally
			{
				writer.Close();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load the reference cache
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void LoadReferences()
		{
			XmlSerializer serializer = new XmlSerializer(typeof(ReferenceCache));
			try
			{
				TextReader reader = new StreamReader(ReferenceCacheName);
				try
				{
					m_Cache = (ReferenceCache)serializer.Deserialize(reader);
				}
				catch(Exception e)
				{
					System.Diagnostics.Debug.WriteLine("Exception: " + e.Message);
				}
				finally
				{
					reader.Close();
				}
			}
			catch
			{
				// file doesn't exist
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Resolve references of a reference
		/// </summary>
		/// <param name="referencePath">Path to the referenced assembly</param>
		/// ------------------------------------------------------------------------------------
		private void ResolveReferences(string referencePath)
		{
			string referenceName = Path.GetFileName(referencePath);
			XmlAssembly assembly = m_Cache[referenceName];
			if (assembly != null)
			{	// we have cached references for this assembly
				foreach(Reference reference in assembly.References)
				{
					string srcFilePath = Path.Combine(ToDirectory.FullName, reference.Name);

					string dstFilePath = Path.Combine(m_dstBaseInfo.FullName,
						Path.GetFileName(reference.Name));
					if (srcFilePath.ToLower() != dstFilePath.ToLower())
					{
						FileCopyMap[dstFilePath] =
							new FileDateInfo(srcFilePath.ToLower(), DateTime.MinValue);
						AddFile(srcFilePath, "xml");
						AddFile(srcFilePath, "pdb");
						ResolveReferences(srcFilePath);
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a file to the list of files to copy. The filename is built from the assembly
		/// name and the new extension.
		/// </summary>
		/// <param name="srcName">Path and name of file</param>
		/// <param name="newExtension">New extension</param>
		/// ------------------------------------------------------------------------------------
		private void AddFile(string srcName, string newExtension)
		{
			FileInfo newSrcInfo = new FileInfo(Path.ChangeExtension(srcName, newExtension));
			if (newSrcInfo.Exists)
			{
				string dstFile = Path.Combine(m_dstBaseInfo.FullName,
					Path.GetFileName(newSrcInfo.FullName));
				if (newSrcInfo.FullName.ToLower() != dstFile.ToLower())
					FileCopyMap[dstFile] =
						new FileDateInfo(newSrcInfo.FullName.ToLower(), newSrcInfo.LastWriteTime);
				else
					Log(Level.Debug, "Reference file {0} has same source and destination", newSrcInfo.FullName);
			}
			else
				Log(Level.Debug, "Reference file {0} does not exist", newSrcInfo.FullName);

		}
	}
}
