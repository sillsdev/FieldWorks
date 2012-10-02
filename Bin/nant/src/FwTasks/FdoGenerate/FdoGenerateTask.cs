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
using NAnt.Core.Attributes;
using NAnt.Core;

namespace SIL.FieldWorks.FDO.FdoGenerate
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TaskName("fdogenerate")]
	public class FdoGenerateTask: Task
	{
		private string m_outputDir;
		private string m_outputFile;
		private string m_templateFile;
		private string m_backendTemplateFiles;
		private string m_xmlFile;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the XML file.
		/// </summary>
		/// <value>The XML file.</value>
		/// ------------------------------------------------------------------------------------
		[TaskAttribute("xml", Required = true)]
		[StringValidator(AllowEmpty = false)]
		public string XmlFile
		{
			get { return m_xmlFile; }
			set { m_xmlFile = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the base dir for the output.
		/// </summary>
		/// <value>The output directory.</value>
		/// ------------------------------------------------------------------------------------
		[TaskAttribute("outdir", Required = true)]
		[StringValidator(AllowEmpty = false)]
		public string OutputDir
		{
			get { return m_outputDir; }
			set { m_outputDir = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the file for the output.
		/// </summary>
		/// <value>The output file name.</value>
		/// ------------------------------------------------------------------------------------
		[TaskAttribute("outputfile", Required = true)]
		[StringValidator(AllowEmpty = false)]
		public string OutputFile
		{
			get { return m_outputFile; }
			set { m_outputFile = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the template file.
		/// </summary>
		/// <value>The template file.</value>
		/// ------------------------------------------------------------------------------------
		[TaskAttribute("template", Required = true)]
		[StringValidator(AllowEmpty = false)]
		public string TemplateFile
		{
			get { return m_templateFile; }
			set { m_templateFile = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the template file.
		/// </summary>
		/// <value>The template file.</value>
		/// ------------------------------------------------------------------------------------
		[TaskAttribute("backendtemplates", Required = false)]
		[StringValidator(AllowEmpty = true)]
		public string BackendTemplateFiles
		{
			get { return m_backendTemplateFiles; }
			set { m_backendTemplateFiles = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Executes the task.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void ExecuteTask()
		{
			string oldDir = Directory.GetCurrentDirectory();
			try
			{
				var doc = new XmlDocument();
				try
				{
					if (Path.GetDirectoryName(m_templateFile).Length > 0)
						Directory.SetCurrentDirectory(Path.GetDirectoryName(m_templateFile));

					Log(Level.Verbose, "Loading XML file {0}.", XmlFile);
					doc.Load(XmlFile);
				}
				catch (XmlException e)
				{
					throw new BuildException("Error loading XML file", e);
				}

				var config = new XmlDocument();
				var handGeneratedClasses = new Dictionary<string, List<string>>();
				try
				{
					Log(Level.Verbose, "Loading hand generated classes from \"HandGenerated.xml\".");
					config.Load("HandGenerated.xml");
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
					throw new BuildException("Error loading hand generated classes", e);
				}

				// Dictionary<ClassName, Property>
				var intPropTypeOverridesClasses = new Dictionary<string, Dictionary<string, string>>();
				try
				{
					Log(Level.Verbose,
						"Loading hand generated classes from \"IntPropTypeOverrides.xml\".");
					config.Load("IntPropTypeOverrides.xml");
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
					throw new BuildException("Error loading IntPropTypeOverrides classes", e);
				}


				try
				{
					// Remember current directory.
					var originalCurrentDirectory = Directory.GetCurrentDirectory();

					Log(Level.Verbose, "Processing template {0}.", m_templateFile);
					var fdoGenerate = new FdoGenerate(doc, OutputDir)
										{
											Overrides = handGeneratedClasses,
											IntPropTypeOverrides = intPropTypeOverridesClasses
										};

					// Generate the main code.
					fdoGenerate.SetOutput(OutputFile);
					fdoGenerate.Process(Path.GetFileName(m_templateFile));

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
					throw new BuildException("Error processing template", e);
				}
			}
			finally
			{
				Directory.SetCurrentDirectory(oldDir);
			}
		}
	}
}
