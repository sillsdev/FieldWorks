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
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Windows.Forms;
using System.Xml;
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
	public class RegFree : Task
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="RegFree"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public RegFree()
		{
			Dlls = new ITaskItem[0];
			ManagedAssemblies = new ITaskItem[0];
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
		/// Gets or sets the Platform win32 or win64/x64.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Platform { get; set; }

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
		/// Gets or sets the managed assemblies that should be processed for [ComVisible] classes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITaskItem[] ManagedAssemblies { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the assemblies that don't have a type lib
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITaskItem[] NoTypeLib { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the CLSIDs to exclude from the manifest.
		/// This is useful when a CLSID is defined in a TypeLib but implemented in a managed assembly
		/// that provides its own manifest entry.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITaskItem[] ExcludedClsids { get; set; }

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
			Log.LogMessage(
				MessageImportance.Normal,
				"RegFree processing {0}",
				Path.GetFileName(Executable)
			);

			var itemsToProcess = new List<ITaskItem>(Dlls);
			if (itemsToProcess.Count == 0)
			{
				string ext = Path.GetExtension(Executable);
				if (
					ext != null
					&& ext.Equals(".dll", StringComparison.InvariantCultureIgnoreCase)
					&& (ManagedAssemblies == null || !ManagedAssemblies.Any(m => m.ItemSpec.Equals(Executable, StringComparison.OrdinalIgnoreCase)))
				)
				{
					itemsToProcess.Add(new TaskItem(Executable));
				}
			}

			string manifestFile = string.IsNullOrEmpty(Output)
				? Executable + ".manifest"
				: Output;

			try
			{
				var doc = new XmlDocument { PreserveWhitespace = true };

				// Try to load existing manifest, or create empty document if it doesn't exist
				if (File.Exists(manifestFile))
				{
					using (XmlReader reader = new XmlTextReader(manifestFile))
					{
						if (reader.MoveToElement())
							doc.ReadNode(reader);
					}
				}
				else
				{
					// Create a minimal valid XML document if manifest doesn't exist
					Log.LogMessage(
						MessageImportance.Low,
						"\tCreating new manifest file {0}",
						manifestFile
					);
				}

				// Process all DLLs using direct type library parsing (no registry redirection needed)
				var creator = new RegFreeCreator(doc, Log);
				if (ExcludedClsids != null)
				{
					creator.AddExcludedClsids(GetFilesFrom(ExcludedClsids));
				}

				// Remove non-existing files from the list
				itemsToProcess.RemoveAll(item => !File.Exists(item.ItemSpec));

				string assemblyName = Path.GetFileNameWithoutExtension(manifestFile);
				if (assemblyName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
				{
					assemblyName = Path.GetFileNameWithoutExtension(assemblyName);
				}
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
				{
					assemblyVersion = "1.0.0.0";
				}
				else
				{
					// Ensure version has exactly 4 numeric parts for manifest compliance (Major.Minor.Build.Revision)
					// Some assemblies might have 3-part versions (e.g. 1.1.0) or non-numeric suffixes
					// (e.g. 9.3.5.local_20260119) which are invalid in SxS manifests.
					// Always sanitize to extract only the leading digits from each part.
					var parts = assemblyVersion.Split('.');
					var newParts = new string[4];
					for (int i = 0; i < 4; i++)
					{
						// Simple parsing to ensure we only get numbers
						string part = "0";
						if (i < parts.Length)
						{
							// Take only the leading digits from each version part
							var digits = new string(parts[i].TakeWhile(char.IsDigit).ToArray());
							if (!string.IsNullOrEmpty(digits))
								part = digits;
						}
						newParts[i] = part;
					}
					assemblyVersion = string.Join(".", newParts);
				}

				XmlElement root = creator.CreateExeInfo(assemblyName, assemblyVersion, Platform);

				foreach (string fileName in GetFilesFrom(ManagedAssemblies))
				{
					Log.LogMessage(
						MessageImportance.Low,
						"\tProcessing managed assembly {0}",
						Path.GetFileName(fileName)
					);
					creator.ProcessManagedAssembly(root, fileName);
				}

				foreach (var item in itemsToProcess)
				{
					string fileName = item.ItemSpec;
					if (NoTypeLib.Count(f => f.ItemSpec == fileName) != 0)
						continue;

					string server = item.GetMetadata("Server");
					if (string.IsNullOrEmpty(server))
						server = null;

					Log.LogMessage(
						MessageImportance.Low,
						"\tProcessing library {0}",
						Path.GetFileName(fileName)
					);

					// Process type library directly (no registry redirection needed)
					creator.ProcessTypeLibrary(root, fileName, server);
				}

				// Process classes and interfaces from HKCR (where COM is already registered)
				creator.ProcessClasses(root);
				creator.ProcessInterfaces(root);

				foreach (string fragmentName in GetFilesFrom(Fragments))
				{
					Log.LogMessage(
						MessageImportance.Low,
						"\tAdding fragment {0}",
						Path.GetFileName(fragmentName)
					);
					creator.AddFragment(root, fragmentName);
				}

				foreach (string fragmentName in GetFilesFrom(AsIs))
				{
					Log.LogMessage(
						MessageImportance.Low,
						"\tAdding as-is fragment {0}",
						Path.GetFileName(fragmentName)
					);
					creator.AddAsIs(root, fragmentName);
				}

				foreach (string assemblyFileName in GetFilesFrom(DependentAssemblies))
				{
					Log.LogMessage(
						MessageImportance.Low,
						"\tAdding dependent assembly {0}",
						Path.GetFileName(assemblyFileName)
					);
					creator.AddDependentAssembly(root, assemblyFileName);
				}

				if (!HasRegFreeContent(doc))
				{
					Log.LogMessage(
						MessageImportance.Low,
						"\tNo registration-free content found for {0}; manifest will not be emitted.",
						Path.GetFileName(manifestFile)
					);
					if (File.Exists(manifestFile))
						File.Delete(manifestFile);
					return true;
				}

				var settings = new XmlWriterSettings
				{
					OmitXmlDeclaration = false,
					NewLineOnAttributes = false,
					NewLineChars = Environment.NewLine,
					Indent = true,
					IndentChars = "\t",
				};
				using (XmlWriter writer = XmlWriter.Create(manifestFile, settings))
				{
					doc.WriteContentTo(writer);
				}

				// Note: No unregistration needed - we never registered anything!
				// Direct type library parsing doesn't touch the registry.
			}
			catch (Exception e)
			{
				Log.LogErrorFromException(e, true, true, null);
				return false;
			}
			return true;
		}

		private static bool HasRegFreeContent(XmlDocument doc)
		{
			if (doc.DocumentElement == null)
				return false;

			var namespaceManager = new XmlNamespaceManager(doc.NameTable);
			namespaceManager.AddNamespace("asmv1", "urn:schemas-microsoft-com:asm.v1");

			bool HasNode(string xpath) => doc.SelectSingleNode(xpath, namespaceManager) != null;

			return HasNode("//asmv1:clrClass")
				|| HasNode("//asmv1:comClass")
				|| HasNode("//asmv1:typelib")
				|| HasNode("//asmv1:dependentAssembly");
		}

		private static List<string> GetFilesFrom(ITaskItem[] source)
		{
			var result = new List<string>();
			if (source == null)
				return result;
			foreach (var item in source)
				result.Add(item.ItemSpec);
			return result;
		}
	}
}
