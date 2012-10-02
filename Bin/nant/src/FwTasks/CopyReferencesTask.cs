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
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Tasks;

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
		private ReferenceCache m_cache = new ReferenceCache();
		private DirectoryInfo m_dstBaseInfo;
		private bool m_fResolveAssemblies = true;
		/// <summary>Set to <c>true</c> to force processing all files. Defaults to <c>false</c>.
		/// Used by the <see cref="DelReferencesTask"/>.</summary>
		protected bool m_force;

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
		public bool Append { get; set; }

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
					xmlAssembly = m_cache[Project.ProjectName];
				else
				{
					xmlAssembly = new XmlAssembly(Project.ProjectName);
					m_cache.Add(xmlAssembly);
				}

				// if source file is not specified use fileset
				foreach (var pathname in CopyFileSet.FileNames)
				{
					var srcInfo = new FileInfo(pathname);
					Log(Level.Debug, "Checking file {0}", pathname);

					if (srcInfo.Exists || m_force)
					{
						// The full filepath to copy to.
						var dstFilePath = Path.Combine(m_dstBaseInfo.FullName, Path.GetFileName(srcInfo.FullName));

						// do the outdated check
						var dstInfo = new FileInfo(dstFilePath);
						Log(Level.Debug, "Comparing with destination file {0}", dstFilePath);
						var fOutdated = (!dstInfo.Exists) || (srcInfo.LastWriteTime > dstInfo.LastWriteTime);

						if (Overwrite || fOutdated || m_force)
						{
							Log(Level.Debug, "Need to process: {0}", Overwrite ? "Overwrite is true" :
								fOutdated ? "src is newer than dst" : "force is true");
							if (!IsInGAC(srcInfo))
							{
								FileCopyMap[dstFilePath] =
									new FileDateInfo(IsUnix ? srcInfo.FullName : srcInfo.FullName.ToLower(), srcInfo.LastWriteTime);
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
							var msg = String.Format(CultureInfo.InvariantCulture,
								"Could not find file {0} to copy.", srcInfo.FullName);
							throw new BuildException(msg, Location);
						}
						Log(Level.Error, "Reference file {0} does not exist (ignored)", pathname);
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

		private bool IsInGAC(FileSystemInfo srcInfo)
		{
			try
			{
				var assemblyName = AssemblyName.GetAssemblyName(srcInfo.FullName);
				var assembly = Assembly.ReflectionOnlyLoad(assemblyName.FullName);
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
		/// Removes the read only attribute of a file.
		/// </summary>
		/// <param name="file">The file name and path of the file</param>
		/// ------------------------------------------------------------------------------------
		private void RemoveReadOnlyAttribute(string file)
		{
			try
			{
				var fileAttributes = File.GetAttributes(file);
				fileAttributes &= ~FileAttributes.ReadOnly;
				File.SetAttributes(file, fileAttributes);
			}
			catch (FileNotFoundException)
			{
				// just ignore
			}
			catch (Exception e)
			{
				var msg = String.Format(CultureInfo.InvariantCulture,
					"Cannot set file attributes for '{0}'", file);
				throw new BuildException(msg, Location, e);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copy files
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void DoFileOperations()
		{
			var fileCount = FileCopyMap.Count;
			if (fileCount <= 0 && !Verbose)
				return;

			Log(Level.Info, "Copying {0} file{1} to '{2}'.", fileCount,
				(fileCount != 1) ? "s" : "",
				ToFile != null ? ToFile.FullName : ToDirectory.FullName);

			// loop thru our file list
			foreach (string destinationFile in FileCopyMap.Keys)
			{
				Log(Level.Debug, "Next file to copy {0}", destinationFile);
				var sourceFile = ((FileDateInfo)FileCopyMap[destinationFile]).Path;

				if ((IsUnix && sourceFile == destinationFile) ||
					(!IsUnix && sourceFile.ToLowerInvariant() == destinationFile.ToLowerInvariant()))
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

					// No. Reset the attrs after the copy.
					//RemoveReadOnlyAttribute(destinationFile);

					// copy the file
					if (!File.Exists(destinationFile))
					{
						Log(Level.Debug, "Calling File.Copy on {0}", destinationFile);
						File.Copy(sourceFile, destinationFile, true);
						RemoveReadOnlyAttribute(destinationFile);
						AdjustDate(sourceFile, destinationFile);

					}
				}
				catch
				{
					Log(Level.Error, "Exception on {0}", destinationFile);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adjusts the date of the copied file.
		/// </summary>
		/// <param name="sourceFile">The source file</param>
		/// <param name="destinationFile">The newly copied file</param>
		/// <remarks>We want to set the dates of the copied files to match the original ones
		/// so that we don't recompile unnecessarily.</remarks>
		/// ------------------------------------------------------------------------------------
		private void AdjustDate(string sourceFile, string destinationFile)
		{
			Log(Level.Debug, "Adjusting date on {0}", destinationFile);
			try
			{
				var srcFile = new FileInfo(sourceFile);
				var dstFile = new FileInfo(destinationFile)
								{
									LastWriteTimeUtc = srcFile.LastWriteTimeUtc,
									LastAccessTimeUtc = srcFile.LastAccessTimeUtc
								};
			}
			catch (FileNotFoundException)
			{
				// just ignore
			}
			catch (Exception e)
			{
				var msg = String.Format(CultureInfo.InvariantCulture,
					"Cannot adjust dates for '{0}'", destinationFile);
				throw new BuildException(msg, Location, e);
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
			string srcFilePath = referenceName.StartsWith(ToDirectory.FullName) ?
				referenceName.Substring(ToDirectory.FullName.Length+1) : referenceName;

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
		private void CreateDirectories(FileSystemInfo srcBaseInfo)
		{
			// Create any specified directories that weren't created during the copy (ie: empty directories)
			foreach (string pathname in CopyFileSet.DirectoryNames)
			{
				var srcInfo = new DirectoryInfo(pathname);
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
			var serializer = new XmlSerializer(typeof(ReferenceCache));
			TextWriter writer = new StreamWriter(ReferenceCacheName);
			try
			{
				serializer.Serialize(writer, m_cache);
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
			var serializer = new XmlSerializer(typeof(ReferenceCache));
			try
			{
				using (TextReader reader = new StreamReader(ReferenceCacheName))
				{
					m_cache = (ReferenceCache) serializer.Deserialize(reader);
				}
			}
			catch(Exception e)
			{
				System.Diagnostics.Debug.WriteLine("Exception: " + e.Message);
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
			XmlAssembly assembly = m_cache[referenceName];
			if (assembly != null)
			{	// we have cached references for this assembly
				foreach(Reference reference in assembly.References)
				{
					string srcFilePath = Path.Combine(ToDirectory.FullName, reference.Name);

					string dstFilePath = Path.Combine(m_dstBaseInfo.FullName,
						Path.GetFileName(reference.Name));
					if ((IsUnix && srcFilePath != dstFilePath) ||
						(!IsUnix && srcFilePath.ToLower() != dstFilePath.ToLower()))
					{
						FileCopyMap[dstFilePath] =
							new FileDateInfo(IsUnix ? srcFilePath : srcFilePath.ToLower(), DateTime.MinValue);
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
			var newSrcInfo = new FileInfo(Path.ChangeExtension(srcName, newExtension));
			if (newSrcInfo.Exists)
			{
				string dstFile = Path.Combine(m_dstBaseInfo.FullName,
					Path.GetFileName(newSrcInfo.FullName));
				string newSrcInfoName = IsUnix ? newSrcInfo.FullName : newSrcInfo.FullName.ToLower();
				string dstFileName = IsUnix ? dstFile : dstFile.ToLower();
				if (newSrcInfoName != dstFileName)
					FileCopyMap[dstFile] =
						new FileDateInfo(newSrcInfoName, newSrcInfo.LastWriteTime);
				else
					Log(Level.Debug, "Reference file {0} has same source and destination", newSrcInfo.FullName);
			}
			else
				Log(Level.Debug, "Reference file {0} does not exist", newSrcInfo.FullName);

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns <c>true</c> if running on Unix, otherwise <c>false</c>.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static bool IsUnix
		{
			get { return Environment.OSVersion.Platform == PlatformID.Unix; }
		}
	}
}
