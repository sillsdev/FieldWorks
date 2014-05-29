// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: RegFreeTask.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Xml;
using FwBuildTasks;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace SIL.FieldWorks.Build.Tasks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Creates a manifest file for the specified executable that allows the given DLLs to be
	/// used without having to be registered.
	/// Adapted from Nant RegFreeTask. Some properties have not been tested.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class RegFree: Task
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="RegFree"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public RegFree()
		{
			Dlls = new ITaskItem[0];
			Fragments = new ITaskItem[0];
			AsIs = new ITaskItem[0];
			NoTypeLib = new ITaskItem[0];
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the executable.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Required]
		public string Executable { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the output file name and path. This attribute is optional. If not
		/// specified, the path and name of the executable with ".manifest" appended will be
		/// used.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Output { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Whether to unregister the processed assemblies after creating the manifest file.
		/// Defaults to <c>false</c>.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool Unregister { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the assemblies that should be processed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITaskItem[] Dlls { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the assemblies that don't have a type lib
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITaskItem[] NoTypeLib { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets manifest fragment files that will be included in the resulting manifest
		/// file. This can be used to pre-process the manifest files for some rarely changing
		/// Dlls.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITaskItem[] Fragments { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets fragment files that will be included "as is".
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITaskItem[] AsIs { get; set; }

		/// <summary>
		/// Gets or sets the dependent assemblies. Currently, this only accepts paths to assembly
		/// manifests.
		/// </summary>
		public ITaskItem[] DependentAssemblies { get; set; }

		private bool? m_IsAdmin;
		private bool UserIsAdmin
		{
			get
			{
				if (!m_IsAdmin.HasValue)
				{
					var id = WindowsIdentity.GetCurrent();
					var p = new WindowsPrincipal(id);
					m_IsAdmin = p.IsInRole(WindowsBuiltInRole.Administrator);
				}
				return m_IsAdmin.Value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Executes the task.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool Execute()
		{
			Log.LogMessage(MessageImportance.Normal, "RegFree processing {0}",
				Path.GetFileName(Executable));

			StringCollection dllPaths = IdlImp.GetFilesFrom(Dlls);
			if (dllPaths.Count == 0)
			{
				string ext = Path.GetExtension(Executable);
				if (ext != null && ext.Equals(".dll", StringComparison.InvariantCultureIgnoreCase))
					dllPaths.Add(Executable);
			}
			string manifestFile = string.IsNullOrEmpty(Output) ? Executable + ".manifest" : Output;

			try
			{
				var doc = new XmlDocument { PreserveWhitespace = true };

				using (XmlReader reader = new XmlTextReader(manifestFile))
				{
					if (reader.MoveToElement())
						doc.ReadNode(reader);
				}

				// Register all DLLs temporarily
				using (var regHelper = new RegHelper(Log))
				{
					regHelper.RedirectRegistry(!UserIsAdmin);
					var creator = new RegFreeCreator(doc, Log);
					var filesToRemove = dllPaths.Cast<string>().Where(fileName => !File.Exists(fileName)).ToList();
					foreach (var file in filesToRemove)
						dllPaths.Remove(file);

					foreach (string fileName in dllPaths)
					{
						Log.LogMessage(MessageImportance.Low, "\tRegistering library {0}", Path.GetFileName(fileName));
						try
						{
							regHelper.Register(fileName, true, false);
						}
						catch (Exception e)
						{
							Log.LogMessage(MessageImportance.High, "Failed to register library {0}", fileName);
							Log.LogMessage(MessageImportance.High, e.StackTrace);
						}
					}

					string assemblyName = Path.GetFileNameWithoutExtension(manifestFile);
					Debug.Assert(assemblyName != null);
					// The C++ test programs won't run if an assemblyIdentity element exists.
					//if (assemblyName.StartsWith("test"))
					//	assemblyName = null;
					string assemblyVersion = null;
					try
					{
						assemblyVersion = FileVersionInfo.GetVersionInfo(Executable).FileVersion;
					}
					catch
					{
						// just ignore
					}
					if (string.IsNullOrEmpty(assemblyVersion))
						assemblyVersion = "1.0.0.0";
					XmlElement root = creator.CreateExeInfo(assemblyName, assemblyVersion);
					foreach (string fileName in dllPaths)
					{
						if (NoTypeLib.Count(f => f.ItemSpec == fileName) != 0)
							continue;

						Log.LogMessage(MessageImportance.Low, "\tProcessing library {0}", Path.GetFileName(fileName));
						creator.ProcessTypeLibrary(root, fileName);
					}
					creator.ProcessClasses(root);
					creator.ProcessInterfaces(root);
					foreach (string fragmentName in IdlImp.GetFilesFrom(Fragments))
					{
						Log.LogMessage(MessageImportance.Low, "\tAdding fragment {0}", Path.GetFileName(fragmentName));
						creator.AddFragment(root, fragmentName);
					}

					foreach (string fragmentName in IdlImp.GetFilesFrom(AsIs))
					{
						Log.LogMessage(MessageImportance.Low, "\tAdding as-is fragment {0}", Path.GetFileName(fragmentName));
						creator.AddAsIs(root, fragmentName);
					}

					foreach (string assemblyFileName in IdlImp.GetFilesFrom(DependentAssemblies))
					{
						Log.LogMessage(MessageImportance.Low, "\tAdding dependent assembly {0}", Path.GetFileName(assemblyFileName));
						creator.AddDependentAssembly(root, assemblyFileName);
					}

					var settings = new XmlWriterSettings
									{
										OmitXmlDeclaration = false,
										NewLineOnAttributes = false,
										NewLineChars = Environment.NewLine,
										Indent = true,
										IndentChars = "\t"
									};
					using (XmlWriter writer = XmlWriter.Create(manifestFile, settings))
					{
						doc.WriteContentTo(writer);
					}

					// Unregister DLLs
					if (Unregister)
					{
						foreach (string fileName in dllPaths)
						{
							Log.LogMessage(MessageImportance.Low, "\tUnregistering library {0}",
								Path.GetFileName(fileName));
							regHelper.Unregister(fileName, true);
						}
					}
				}
			}
			catch (Exception e)
			{
				Log.LogErrorFromException(e, true, true, null);
				return false;
			}
			return true;
		}
	}
}
