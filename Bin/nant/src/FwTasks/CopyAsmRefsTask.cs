// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: CopyAsmRefsTask.cs
// Responsibility: Randy Regnier
// Last reviewed:
//
// <remarks>
//	This isn't a subclass of CopyTask, because it allows too much.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml;
using System.Xml.Serialization;

using NAnt.Core.Attributes;
using NAnt.Core;
using NAnt.Core.Tasks;
using NAnt.Core.Types;
using NAnt.Core.Util;

namespace SIL.FieldWorks.Build.Tasks
{
	/// <summary>
	/// Copies an assembly or set of assemblies and all non-GAC referenced assemblies to a directory.
	/// </summary>
	/// <remarks>
	///   <para>
	///   The target directory is emptied out first, and then the Files are copied.
	///   </para>
	///   <para>
	///   A <see cref="FileSet" /> can be used to select several assemblies to copy.
	///   </para>
	/// </remarks>
	/// <example>
	///   <para>Copy a one assembly and all non-GAC referenced assemblies to a directory.</para>
	///   <code>
	///     <![CDATA[
	/// <copyasmrefs todir="${installer.dir}">
	///     <fileset basedir="${fwroot}/Output/${config}">
	///         <includes name="Te.exe" />
	///     </fileset>
	/// </copyasmrefs>
	///     ]]>
	///   </code>
	///   <para>Copy a several assemblies and all non-GAC referenced assemblies to a directory.
	///   Copy any related files, as well.</para>
	///   <code>
	///     <![CDATA[
	/// <copyasmrefs todir="${installer.dir}" copyrelated="true">
	///     <fileset basedir="${fwroot}/Output/${config}">
	///			<!-- Main assembly. -->
	///         <includes name="LexEd.exe" />
	///			<!-- Non-referenced, but required, runtime assemblies. -->
	///         <includes name="AdapterLibrary.dll" />
	///         <includes name="LingCmnDlgs.dll" />
	///     </fileset>
	/// </copyasmrefs>
	///     ]]>
	///   </code>
	/// </example>
	[TaskName("copyasmrefs")]
	public class CopyAsmRefsTask : Task
	{
		#region Private Instance Fields

		/// <summary></summary>
		protected string m_toDirectory = null;
		/// <summary></summary>
		protected FileSet m_fileset = new FileSet();
		/// <summary></summary>
		protected StringCollection m_assemblyNames = new StringCollection();
		/// <summary></summary>
		protected StringCollection m_skipAssemblyNames = new StringCollection();
		/// <summary></summary>
		protected bool m_copyRelated = false;

		#endregion Private Instance Fields

		#region Public Instance Properties

		/// <summary>
		/// The directory to copy to.
		/// </summary>
		[TaskAttribute("todir", Required=true)]
		public virtual string ToDirectory
		{
			get { return (m_toDirectory != null) ? Project.GetFullPath(m_toDirectory) : null; }
			set { m_toDirectory = StringUtils.ConvertEmptyToNull(value); }
		}


		/// <summary>
		/// Used to select the files to copy. To use a <see cref="FileSet" />,
		/// the <see cref="ToDirectory" /> attribute must be set.
		/// </summary>
		[BuildElement("fileset", Required=true)]
		public virtual FileSet CopyFileSet
		{
			get { return m_fileset; }
			set { m_fileset = value; }
		}

		/// <summary>
		/// Copy related files, such as .pdb and .xml. These will use the pattern foo.*.
		/// The default is <see langword="false" />.
		/// </summary>
		[TaskAttribute("copyrelated")]
		[BooleanValidator()]
		public bool CopyRelated
		{
			get { return m_copyRelated; }
			set { m_copyRelated = value; }
		}

		#endregion Public Instance Properties

		#region Protected Instance Properties

		/// <summary>
		/// Get the collection of assembly names.
		/// </summary>
		protected virtual StringCollection AssemblyNames
		{
			get { return m_assemblyNames; }
		}

		#endregion Protected Instance Properties

		#region Override implementation of Task

		/// <summary>
		/// Checks whether the 'file' attribute and the 'fileset' element have both been set.
		/// </summary>
		/// <param name="taskNode">The <see cref="XmlNode" /> used to initialize the task.</param>
		protected override void InitializeTask(XmlNode taskNode)
		{
			if (CopyFileSet.Includes.Count == 0)
			{
				throw new BuildException(string.Format(CultureInfo.InvariantCulture,
					"The <fileset> element has no files included."), Location);
			}
			if (CopyFileSet.BaseDirectory.FullName == Project.BaseDirectory)
			{
				throw new BuildException(string.Format(CultureInfo.InvariantCulture,
					"The <fileset> element does not have its basedir attribute set."), Location);
			}
		}

		/// <summary>
		/// Executes the Copy task.
		/// </summary>
		/// <exception cref="BuildException">A assembly that has to be copied does not exist or could not be copied.</exception>
		protected override void ExecuteTask()
		{
			string oldDir = Directory.GetCurrentDirectory();
			string baseDir = CopyFileSet.BaseDirectory.FullName;
			try
			{
				Directory.SetCurrentDirectory(baseDir);
					foreach (string filename in CopyFileSet.FileNames)
						ProcessAssembly(filename);

				string name = "(unknown)";
				try
				{
					// Create directory if not present
					if (!Directory.Exists(ToDirectory))
					{
						Directory.CreateDirectory(ToDirectory);
						Log(Level.Verbose, "Created directory: {0}", ToDirectory);
					}
					// Retrieve all assemblies and related files; exclude files that are in
					// the exclude list of the file set.
					CopyFileSet.Reset();
					foreach (string file in AssemblyNames)
					{
						CopyFileSet.Includes.Add(file);
						if (CopyRelated)
							CopyFileSet.Includes.Add(Path.ChangeExtension(file, ".*"));
					}
					CopyFileSet.Scan();

					// Copy files
					foreach(string pathname in CopyFileSet.FileNames)
					{
						CopyFile(pathname, ref name);
					}
				}
				catch (Exception ex)
				{
					string msg = string.Format(CultureInfo.InvariantCulture,
							"Cannot copy {0} to {1}.", name, ToDirectory);

					if (FailOnError)
						throw new BuildException(msg, Location, ex);
					else
						Log(Level.Error, msg);
				}
				int fileCount = AssemblyNames.Count;
				if (fileCount == 0)
					Log(Level.Warning, "No files copied.");
				else
					Log(Level.Info, "Copied {0} file{1} to: {2}", fileCount, (fileCount != 1) ? "s" : "", ToDirectory);
			}
			finally
			{
				Directory.SetCurrentDirectory(oldDir);
			}
		}

		#endregion Override implementation of Task

		#region Other methods

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copy a single file
		/// </summary>
		/// <param name="pathname">path name of file to copy from</param>
		/// <param name="name">returns the name of the file copied</param>
		/// ------------------------------------------------------------------------------------
		protected void CopyFile(string pathname, ref string name)
		{
			try
			{
				name = Path.GetFileName(pathname);
				string toFile = Path.Combine(ToDirectory, name);
				Log(Level.Verbose, "Copying {0}", name);

				// before copying, make sure that the destination file is not read-only
				if (File.Exists(toFile))
					File.SetAttributes(toFile, File.GetAttributes(toFile) & ~FileAttributes.ReadOnly);

				// Copy the file and then clear the read-only attribute.
				File.Copy(pathname, toFile, true);
				File.SetAttributes(toFile, File.GetAttributes(toFile) & ~FileAttributes.ReadOnly);
			}
			catch (Exception ex)
			{
				string msg = string.Format(CultureInfo.InvariantCulture,
						"Cannot copy {0} to {1}.", pathname, name);

				if (FailOnError)
					throw new BuildException(msg, Location, ex);
				else
					Log(Level.Error, msg);
			}
		}

		/// <summary>
		/// Process one assembly.
		/// </summary>
		/// <param name="filename">The filename of the assembly to process, without its path.</param>
		protected void ProcessAssembly(string filename)
		{
			Log(Level.Verbose, "Checking assembly: {0}", filename);
			Assembly asm = null;

			FileInfo srcInfo = new FileInfo(filename);
			if (!srcInfo.Exists)
			{
				// Cache files we can't find.
				string name = Path.GetFileName(filename);
				if (Path.GetExtension(name) == ".dll")
					name = name.Substring(0, name.Length - 4);
				try
				{
					asm = Assembly.Load(name);
					if (!m_skipAssemblyNames.Contains(name))
					{
						if (asm.GlobalAssemblyCache || name.IndexOf("mscorlib") != -1)
							Log(Level.Verbose, "Skipping GAC assembly {0}", name);
						else
							Log(Level.Warning, "Skipping assembly {0}, since it could not be found.", name);
						m_skipAssemblyNames.Add(name);
					}
				}
				catch
				{
				}
				return;
			}

			try
			{
				if (filename.LastIndexOfAny(new char[] { '\\', '/' }) > -1)
					asm = Assembly.LoadFrom(filename);
				else
					asm = Assembly.Load(filename);
				if (asm != null)
				{
					if (asm.GlobalAssemblyCache && !m_skipAssemblyNames.Contains(filename))
					{
						Log(Level.Verbose, "Skipping GAC assembly: {0}", filename);
						m_skipAssemblyNames.Add(filename);
						return;
					}
					if (!AssemblyNames.Contains(filename))
					{
						Log(Level.Verbose, "Caching assembly: {0}", filename);
						AssemblyNames.Add(filename);
						foreach (AssemblyName asmName in asm.GetReferencedAssemblies())
						{
							string asmNameName = asmName.Name;
							string fullName = Path.Combine(CopyFileSet.BaseDirectory.FullName, asmNameName + ".dll");
							if (!AssemblyNames.Contains(fullName) && !m_skipAssemblyNames.Contains(asmNameName))
								ProcessAssembly(fullName);
						}
					}
				}
				else
					Log(Level.Verbose, "Failed to load assembly: {0}", filename);
			}
			catch (BuildException be)
			{
				throw be;
			}
			catch (Exception e)
			{
				string msg = string.Format(CultureInfo.InvariantCulture,
						"Could not load assembly: {0}.", filename);

				if (FailOnError)
					throw new BuildException(msg, Location, e);
				else
					Log(Level.Error, msg);
			}
		}

		#endregion Other methods
	}
}
