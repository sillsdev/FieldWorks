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
using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Types;

namespace SIL.FieldWorks.Build.Tasks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Creates a manifest file for the specified executable that allows the given DLLs to be
	/// used without having to be registered.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TaskName("regfree")]
	public class RegFreeTask: Task
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="RegFreeTask"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public RegFreeTask()
		{
			Dlls = new FileSet();
			Fragments = new FileSet();
			AsIs = new FileSet();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the executable.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TaskAttribute("executable", Required = true)]
		public string Executable { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the assemblies that should be processed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[BuildElement("dlls")]
		public FileSet Dlls { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets manifest fragment files that will be included in the resulting manifest
		/// file. This can be used to pre-process the manifest files for some rarely changing
		/// Dlls.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[BuildElement("fragments")]
		public FileSet Fragments { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets fragment files that will be included "as is".
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[BuildElement("asis")]
		public FileSet AsIs { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Executes the task.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void ExecuteTask()
		{
			Log(Level.Info, "Processing {0}", Path.GetFileName(Executable));

			string manifestFile = Executable + ".manifest";
			try
			{
				var doc = new XmlDocument { PreserveWhitespace = true };

				using (XmlReader reader = new XmlTextReader(manifestFile))
				{
					if (reader.MoveToElement())
						doc.ReadNode(reader);
				}

				// Register all DLLs temporarily
				using (var creator = new RegFreeCreator(doc))
				{
					foreach (string fileName in Dlls.FileNames)
					{
						Log(Level.Verbose, "\tRegistering library {0}", Path.GetFileName(fileName));
						creator.Register(fileName);
					}

					XmlElement root = creator.CreateExeInfo(Executable);
					foreach (string fileName in Dlls.FileNames)
					{
						Log(Level.Verbose, "\tProcessing library {0}", Path.GetFileName(fileName));
						creator.ProcessTypeLibrary(root, fileName);
					}
					foreach (string fragmentName in Fragments.FileNames)
					{
						Log(Level.Verbose, "\tAdding fragment {0}", Path.GetFileName(fragmentName));
						creator.AddFragment(root, fragmentName);
					}

					foreach (string fragmentName in AsIs.FileNames)
					{
						Log(Level.Verbose, "\tAdding as-is fragment {0}", Path.GetFileName(fragmentName));
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
					foreach (string fileName in Dlls.FileNames)
					{
						Log(Level.Verbose, "\tUnregistering library {0}", Path.GetFileName(fileName));
						creator.Unregister(fileName);
					}
				}
			}
			catch (Exception e)
			{
				throw new BuildException("Regfree failed.", Location, e);
			}
		}
	}
}
