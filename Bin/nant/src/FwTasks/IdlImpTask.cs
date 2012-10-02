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
// File: IdlImpTask.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Tasks;
using NAnt.Core.Types;
using SIL.FieldWorks.Tools;
using NAnt.DotNet.Types;

namespace SIL.FieldWorks.Build.Tasks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Imports the interfaces of an IDL file.
	/// </summary>
	/// <example>
	/// <para>Import the types specified in <c>idlfile</c> and create a C# file <c>output</c>.
	/// The type will be put in <c>namespace</c>, and the <c>using</c> namespaces will be put
	/// at the beginning of the file.</para>
	/// <code><![CDATA[
	/// <idlimp output="${dir.srcProj}\DbAccess.cs" idlfile="${dir.outputBase}\Common\DbAccessTlb.idl"
	///     namespace="SIL.FieldWorks.Common.COMInterfaces">
	///     <using name="FwKernelLib" />
	///     <using name="FwDbAccess" />
	/// </idlimp>
	/// ]]></code>
	/// </example>
	/// ----------------------------------------------------------------------------------------
	[TaskName("idlimp")]
	public class IdlImpTask: FwBaseTask
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="IdlImpTask"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public IdlImpTask()
		{
		}

		#region Class UsingNamespace
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Class used for storing the referenced namespaces - basically a string
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public class UsingNamespace: Element
		{
			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="UsingNamespace"/> class.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public UsingNamespace()
			{
			}

			private string m_Namespace;

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// The namespace
			/// </summary>
			/// --------------------------------------------------------------------------------
			[TaskAttribute("name")]
			public string Namespace
			{
				get { return m_Namespace; }
				set { m_Namespace = value; }
			}
		}
		#endregion

		#region Class IdhFiles
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Class used for storing the source IDH files - basically a string
		/// </summary>
		/// <remarks>It should be possible to get the names of the IDH files directly from
		/// the IDL file - the preprocessor embedds information about line numbers and source
		/// files in it, but for now doing it this way is easier.</remarks>
		/// ------------------------------------------------------------------------------------
		public class SourceIdhFile : Element
		{
			private string m_IdhFile;

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets or sets the idh file.
			/// </summary>
			/// <value>The idh file.</value>
			/// --------------------------------------------------------------------------------
			[TaskAttribute("name")]
			public string IdhFile
			{
				get { return m_IdhFile; }
				set { m_IdhFile = value; }
			}

		}
		#endregion

		private string m_configfile;
		private UsingNamespace[] m_usingNamespaces;
		private FileSet m_idhFiles = new FileSet();
		/// <summary>List of files for resolving referenced types</summary>
		private AssemblyFileSet m_refFiles = new AssemblyFileSet();
		private bool m_fCreateXmlComments = true;
		private string m_namespace;

		/// <summary>Namespace.</summary>
		[TaskAttribute("namespace")]
		public string Namespace
		{
			get { return m_namespace; }
			set { m_namespace = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Configuration file
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TaskAttribute("configfile")]
		public string ConfigFile
		{
			get { return m_configfile; }
			set { m_configfile = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Additional namespaces that are used.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[BuildElementArray("using")]
		public UsingNamespace[] UsingNamespaces
		{
			get { return m_usingNamespaces; }
			set { m_usingNamespaces = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Source IDH files used to retrieve comments from.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[BuildElement("idhfiles")]
		public FileSet IdhFiles
		{
			get { return m_idhFiles; }
			set { m_idhFiles = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reference metadata from the specified assembly files.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		[BuildElement("references")]
		public AssemblyFileSet ReferenceFiles
		{
			get { return m_refFiles; }
			set { m_refFiles = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creation of dummy XML comments. Defaults to <c>true</c>.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TaskAttribute("xmlcomments")]
		[BooleanValidator]
		public bool CreateXmlComments
		{
			get { return m_fCreateXmlComments; }
			set { m_fCreateXmlComments = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The name of the IDL file
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private string IdlFile
		{
			get
			{
				if (Sources.FileNames.Count <= 0 || string.IsNullOrEmpty(Sources.FileNames[0]))
					throw new BuildException("Missing IDL file.", Location);
				return Sources.FileNames[0];
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the file Extension required by the current compiler
		/// </summary>
		/// <returns>Returns the file Extension required by the current compiler</returns>
		/// ------------------------------------------------------------------------------------
		public override string Extension
		{
			get { return "dll"; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines if we have to compile
		/// </summary>
		/// <returns><c>true</c> if compilation is necessary, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		protected override bool NeedsCompiling()
		{
			bool fRet = base.NeedsCompiling();

			if (!fRet)
			{
				// Configfile or IDL file updated?
				StringCollection files = new StringCollection();
				files.Add(m_configfile);
				files.Add(IdlFile);
				fRet = FileUpdated(files);
			}

			return fRet;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Do the import
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void ExecuteTask()
		{
			base.ExecuteTask();

			// Check to see if any of the underlying IDL files have changed
			// Otherwise, it's not necessary to reimport.
			if (NeedsCompiling())
			{
				Log(Level.Info, "Importing {0}", Path.GetFileName(IdlFile));

				try
				{
					IDLImporter importer = new IDLImporter();
					List<string> usingNamespaces = new List<string>();
					foreach(UsingNamespace ns in m_usingNamespaces)
						usingNamespaces.Add(ns.Namespace);

					bool fOk = importer.Import(usingNamespaces, IdlFile, m_configfile,
						OutputFile.FullName, Namespace, m_idhFiles.FileNames,
						m_refFiles.FileNames, m_fCreateXmlComments);
					if (!fOk)
						throw new BuildException("Import failed: data has errors", Location);
				}
				catch (BuildException)
				{
					throw;
				}
				catch(Exception e)
				{
					throw new BuildException("Import failed.", Location, e);
				}
			}
		}
	}
}
