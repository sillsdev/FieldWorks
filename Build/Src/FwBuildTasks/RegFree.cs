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
using System.IO;
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
		/// Gets or sets the assemblies that should be processed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITaskItem[] Dlls { get; set; }

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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Executes the task.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool Execute()
		{
			Log.LogMessage(MessageImportance.Low, "Processing {0}", Path.GetFileName(Executable));

			var manifestFile = string.IsNullOrEmpty(Output) ? Executable + ".manifest" : Output;

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
					regHelper.RedirectRegistry();
					var creator = new RegFreeCreator(doc, Log);
					var dllPaths = IdlImp.GetFilesFrom(Dlls);
					foreach (string fileName in dllPaths)
					{
						Log.LogMessage(MessageImportance.Low, "\tRegistering library {0}", Path.GetFileName(fileName));
						try
						{
							regHelper.Register(fileName, true);
						}
						catch(Exception e)
						{
							Log.LogMessage(MessageImportance.High, "Failed to register library {0}", fileName);
							Log.LogMessage(MessageImportance.High, e.StackTrace);
						}
					}

					XmlElement root = creator.CreateExeInfo(Executable);
					foreach (string fileName in dllPaths)
					{
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
					foreach (string fileName in dllPaths)
					{
						Log.LogMessage(MessageImportance.Low, "\tUnregistering library {0}", Path.GetFileName(fileName));
						regHelper.Unregister(fileName, true);
					}
				}
			}
			catch (Exception e)
			{
				Log.LogMessage(MessageImportance.High, "RegFree failed " + e.Message);
				return false;
			}
			return true;
		}
	}
}
