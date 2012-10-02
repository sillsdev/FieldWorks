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
using System.Text;
using System.Xml;
using NAnt.Core.Attributes;
using NAnt.Core;
using NAnt.Core.Tasks;
using NAnt.Core.Types;
using NVelocity;
using NVelocity.App;
using NVelocity.Context;
using NVelocity.Runtime;
using NVelocity.Runtime.Log;

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
		private string m_templateFile;
		private string m_xmiFile;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the XMI file.
		/// </summary>
		/// <value>The XMI file.</value>
		/// ------------------------------------------------------------------------------------
		[TaskAttribute("xmi", Required = true)]
		[StringValidator(AllowEmpty = false)]
		public string XMIFile
		{
			get { return m_xmiFile; }
			set { m_xmiFile = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the base dir for the output.
		/// </summary>
		/// <value>The output directory.</value>
		/// ------------------------------------------------------------------------------------
		[TaskAttribute("outdir", Required = true)]
		public string OutputDir
		{
			get { return m_outputDir; }
			set { m_outputDir = value; }
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
		/// Executes the task.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void ExecuteTask()
		{
			string oldDir = Directory.GetCurrentDirectory();
			try
			{
				XmlDocument doc = new XmlDocument();
				try
				{
					if (Path.GetDirectoryName(m_templateFile).Length > 0)
						Directory.SetCurrentDirectory(Path.GetDirectoryName(m_templateFile));

					Log(Level.Verbose, "Loading XMI file {0}.", XMIFile);
					doc.Load(XMIFile);
				}
				catch (XmlException e)
				{
					throw new BuildException("Error loading XMI file", e);
				}

				XmlDocument config = new XmlDocument();
				Dictionary<string, List<string>> handGeneratedClasses =
					new Dictionary<string, List<string>>();
				try
				{
					Log(Level.Verbose, "Loading hand generated classes from \"HandGenerated.xml\".");
					config.Load("HandGenerated.xml");
					foreach (XmlElement node in config.GetElementsByTagName("Class"))
					{
						List<string> props = new List<string>();
						foreach (XmlNode propertyNode in node.SelectNodes("property"))
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
				Dictionary<string, Dictionary<string, string>> intPropTypeOverridesClasses =
					new Dictionary<string, Dictionary<string, string>>();
				try
				{
					Log(Level.Verbose,
						"Loading hand generated classes from \"IntPropTypeOverrides.xml\".");
					config.Load("IntPropTypeOverrides.xml");
					foreach (XmlElement node in config.GetElementsByTagName("Class"))
					{
						// Dictionary<PropertyName, PropertyType>
						Dictionary<string, string> props = new Dictionary<string, string>();
						foreach (XmlNode propertyNode in node.SelectNodes("property"))
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

				Dictionary<string, ModuleInfo> moduleLocations = new Dictionary<string, ModuleInfo>();
				try
				{
					Log(Level.Verbose, "Loading module locations from \"ModuleLocations.xml\".");
					config.Load("ModuleLocations.xml");
					foreach (XmlElement moduleNode in config.GetElementsByTagName("module"))
					{
						ModuleInfo moduleInfo = new ModuleInfo();
						moduleInfo.Name = moduleNode.Attributes["name"].Value;
						moduleInfo.Assembly = moduleNode.Attributes["assembly"].Value;
						moduleInfo.Path = moduleNode.Attributes["path"].Value;
						moduleLocations.Add(moduleInfo.Name, moduleInfo);
					}
				}
				catch (XmlException e)
				{
					throw new BuildException("Error loading module locations", e);
				}
				try
				{
					Log(Level.Verbose, "Processing template {0}.", m_templateFile);
					FdoGenerate fdoGenerate = new FdoGenerate(doc, OutputDir);
					fdoGenerate.Overrides = handGeneratedClasses;
					fdoGenerate.IntPropTypeOverrides = intPropTypeOverridesClasses;
					fdoGenerate.ModuleLocations = moduleLocations;

					fdoGenerate.Process(Path.GetFileName(m_templateFile));
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
