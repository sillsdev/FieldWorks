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
// File: FdoGenerateTask.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using SIL.FieldWorks.FDO.FdoGenerate;

namespace FwBuildTasks.FdoGenerate
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FdoGenerate: Task
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the XML file.
		/// </summary>
		/// <value>The XML file.</value>
		/// ------------------------------------------------------------------------------------
		[Required]
		public string XmlFile { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the base dir for the output.
		/// </summary>
		/// <value>The output directory.</value>
		/// ------------------------------------------------------------------------------------
		[Required]
		public string OutputDir { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the file for the output.
		/// </summary>
		/// <value>The output file name.</value>
		/// ------------------------------------------------------------------------------------
		[Required]
		public string OutputFile { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the template file.
		/// </summary>
		/// <value>The template file.</value>
		/// ------------------------------------------------------------------------------------
		[Required]
		public string TemplateFile { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the template file.
		/// </summary>
		/// <value>The template file.</value>
		/// ------------------------------------------------------------------------------------
		public string BackendTemplateFiles { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Executes the task.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool Execute()
		{
			string oldDir = Directory.GetCurrentDirectory();
			try
			{
				var doc = new XmlDocument();
				string xmlPath = XmlFile;
				if (!Path.IsPathRooted(xmlPath))
					xmlPath = Path.Combine(oldDir, XmlFile);
				try
				{
					Log.LogMessage(MessageImportance.Low, "Loading XML file {0}.", xmlPath);
					doc.Load(xmlPath);
				}
				catch (XmlException e)
				{
					Log.LogMessage(MessageImportance.High, "Error loading XML file " + xmlPath + " " + e.Message);
					return false;
				}

				var config = new XmlDocument();
				var handGeneratedClasses = new Dictionary<string, List<string>>();
				try
				{
					Log.LogMessage(MessageImportance.Low, "Loading hand generated classes from \"HandGenerated.xml\".");
					config.Load(Path.Combine(oldDir, Path.Combine("FdoGenerate", "HandGenerated.xml")));
					foreach (XmlElement node in config.GetElementsByTagName("Class"))
					{
						var props = new List<string>();
// ReSharper disable PossibleNullReferenceException
						foreach (XmlNode propertyNode in node.SelectNodes("property"))
// ReSharper restore PossibleNullReferenceException
						{
							props.Add(propertyNode.Attributes["name"].Value);
						}
						if (props.Count > 0)
						{
							handGeneratedClasses.Add(node.Attributes["id"].Value, props);
						}
					}
				}
				catch (XmlException e)
				{
					Log.LogMessage(MessageImportance.High, "Error loading hand generated classes" + " " + e.Message);
					return false;
				}

				// Dictionary<ClassName, Property>
				var intPropTypeOverridesClasses = new Dictionary<string, Dictionary<string, string>>();
				try
				{
					Log.LogMessage(MessageImportance.Low,
						"Loading hand generated classes from \"IntPropTypeOverrides.xml\".");
					config.Load(Path.Combine(oldDir, Path.Combine("FdoGenerate", "IntPropTypeOverrides.xml")));
					foreach (XmlElement node in config.GetElementsByTagName("Class"))
					{
						// Dictionary<PropertyName, PropertyType>
						var props = new Dictionary<string, string>();
// ReSharper disable PossibleNullReferenceException
						foreach (XmlNode propertyNode in node.SelectNodes("property"))
// ReSharper restore PossibleNullReferenceException
						{
							props.Add(propertyNode.Attributes["name"].Value,
								propertyNode.Attributes["type"].Value);
						}
						if (props.Count > 0)
						{
							intPropTypeOverridesClasses.Add(node.Attributes["id"].Value, props);
						}
					}
				}
				catch (XmlException e)
				{
					Log.LogMessage(MessageImportance.High, "Error loading IntPropTypeOverrides classes" + " " + e.Message);
					return false;
				}


				try
				{
					// Remember current directory.
					var originalCurrentDirectory = Directory.GetCurrentDirectory();

					Log.LogMessage(MessageImportance.Low, "Processing template {0}.", TemplateFile);
					string outputDirPath = OutputDir;
					if (!Path.IsPathRooted(OutputDir))
						outputDirPath = Path.Combine(oldDir, OutputDir);
					var fdoGenerate = new FdoGenerateImpl(doc, outputDirPath)
										{
											Overrides = handGeneratedClasses,
											IntPropTypeOverrides = intPropTypeOverridesClasses
										};
					string outputPath = OutputFile;
					if (!Path.IsPathRooted(outputPath))
						outputPath = Path.Combine(outputDirPath, OutputFile);
					// Generate the main code.
					if (Path.GetDirectoryName(TemplateFile).Length > 0)
						Directory.SetCurrentDirectory(Path.GetDirectoryName(TemplateFile));
					fdoGenerate.SetOutput(outputPath);
					fdoGenerate.Process(Path.GetFileName(TemplateFile));

					// Generate flat DB SQL for SqlServer.
					// 'Flat' here means one table per concrete class in the model.
					//fdoGenerate.SetOutput("BootstrapFlatlandSqlServerDB.sql");
					//fdoGenerate.Process("FlatlandSqlServer.vm.sql");

					// Generate flat DB SQL for Firebird.
					// 'Flat' here means one table per concrete class in the model.
					//fdoGenerate.SetOutput("BootstrapFlatlandFirebirdDB.sql");
					//fdoGenerate.Process("FlatlandFirebird.vm.sql");

					// Generate the backend provider(s) code.
					if (!string.IsNullOrEmpty(BackendTemplateFiles))
					{
						foreach (var backendDir in BackendTemplateFiles.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
						{
							var beDir = backendDir.Trim();
							if (beDir == string.Empty) continue;

							var curDir = Path.Combine(Path.Combine(OutputDir, "FDOGenerate"), beDir);
							Directory.SetCurrentDirectory(curDir);
							fdoGenerate.SetOutput(Path.Combine(beDir, beDir + @"Generated.cs"));
							fdoGenerate.Process("Main" + beDir + ".vm.cs");
						}
					}

					// Restore original directory.
					Directory.SetCurrentDirectory(originalCurrentDirectory);
				}
				catch (Exception e)
				{
					Log.LogMessage(MessageImportance.High, "Error processing template" + " " + e.Message);
					return false;
				}
			}
			finally
			{
				Directory.SetCurrentDirectory(oldDir);
			}
			return true;
		}
	}
}
