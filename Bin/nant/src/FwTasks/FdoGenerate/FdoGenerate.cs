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
// File: FdoGenerate.cs
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
using System.Xml.XPath;
using NVelocity;
using NVelocity.App;
using NVelocity.Runtime;

namespace SIL.FieldWorks.FDO.FdoGenerate
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// FDO code generator
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FdoGenerate
	{
		/// <summary></summary>
		public static FdoGenerate Generator;

		private string m_OutputDir;
		private string m_OutputFileName;
		private VelocityEngine m_Engine;
		private VelocityContext m_Context;
		private Dictionary<string, List<string>> m_OverrideList;
		private Dictionary<string, Dictionary<string, string>> m_IntPropTypeOverrideList;
		private Dictionary<string, ModuleInfo> m_ModuleLocations;
		private Model m_Model;
		private XmlDocument m_Document;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FdoGenerate"/> class.
		/// </summary>
		/// <param name="doc">The XMI document.</param>
		/// <param name="outputDir">The output dir.</param>
		/// ------------------------------------------------------------------------------------
		public FdoGenerate(XmlDocument doc, string outputDir)
			: this(doc, outputDir, null)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FdoGenerate"/> class.
		/// </summary>
		/// <param name="doc">The XMI document.</param>
		/// <param name="outputDir">The output dir.</param>
		/// <param name="outputFile">The output file name.</param>
		/// ------------------------------------------------------------------------------------
		public FdoGenerate(XmlDocument doc, string outputDir, string outputFile)
		{
			FdoGenerate.Generator = this;
			m_Document = doc;
			m_OutputDir = outputDir;
			m_OutputFileName = outputFile;
			XmlElement entireModel = (XmlElement)doc.GetElementsByTagName("EntireModel")[0];
			m_Model = new Model(entireModel);

			m_Engine = new VelocityEngine();
			m_Engine.Init();

			m_Context = new VelocityContext();
			m_Context.Put("fdogenerate", this);
			m_Context.Put("model", m_Model);

			RuntimeSingleton.RuntimeServices.SetApplicationAttribute("FdoGenerate.Engine", m_Engine);
			RuntimeSingleton.RuntimeServices.SetApplicationAttribute("FdoGenerate.Context", m_Context);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the list with the class names that we want to override.
		/// </summary>
		/// <value>The list of override class names.</value>
		/// ------------------------------------------------------------------------------------
		public Dictionary<string, List<string>> Overrides
		{
			get { return m_OverrideList; }
			set { m_OverrideList = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the list with the names and types of integer properties we want to
		/// override.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Dictionary<string, Dictionary<string, string>> IntPropTypeOverrides
		{
			get { return m_IntPropTypeOverrideList; }
			set { m_IntPropTypeOverrideList = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the overriden locations of modules.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Dictionary<string, ModuleInfo> ModuleLocations
		{
			get { return m_ModuleLocations; }
			set { m_ModuleLocations = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the model.
		/// </summary>
		/// <value>The model.</value>
		/// ------------------------------------------------------------------------------------
		public Model Model
		{
			get { return (Model)m_Context.Get("model"); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the modules.
		/// </summary>
		/// <value>The modules.</value>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<CellarModule> Modules
		{
			get { return Model.Modules; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the output file name.
		/// </summary>
		/// <param name="outputFile">The output file name.</param>
		/// ------------------------------------------------------------------------------------
		public void SetOutput(string outputFile)
		{
			m_OutputFileName = outputFile;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the specified template.
		/// </summary>
		/// <param name="templateName">Name of the template.</param>
		/// ------------------------------------------------------------------------------------
		public void Process(string templateName)
		{
			Stream stream = null;
			try
			{
				if (m_OutputFileName == null || m_OutputFileName == string.Empty)
					stream = new MemoryStream(); // we don't care about the output
				else
				{
					stream = new FileStream(Path.Combine(m_OutputDir, m_OutputFileName),
						FileMode.Create, FileAccess.Write);
				}

				using (StreamWriter writer = new StreamWriter(stream))
				{
					m_Engine.MergeTemplate(templateName, "UTF-8", m_Context, writer);
				}
			}
			finally
			{
				if (stream != null)
					stream.Dispose();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the module.
		/// </summary>
		/// <param name="moduleName">Name of the module.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public CellarModule GetModule(string moduleName)
		{
			string query = string.Format("//CellarModule[@id='{0}']", moduleName);
			XPathNodeIterator iterator = m_Document.CreateNavigator().Select(query);
			if (iterator.MoveNext())
			{
				XmlElement module = (XmlElement)iterator.Current.UnderlyingObject;
				return new CellarModule(module, m_Model);
			}
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the class.
		/// </summary>
		/// <param name="className">Name of the class.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public IClass GetClass(string className)
		{
			string query = string.Format("//CellarModule[class/@id='{0}']", className);
			XPathNodeIterator iterator = m_Document.CreateNavigator().Select(query);
			if (iterator.MoveNext())
			{
				XmlElement module = (XmlElement)iterator.Current.UnderlyingObject;
				CellarModule cellarModule = new CellarModule(module, m_Model);
				return cellarModule.Classes[className];
			}

			return new DummyClass();
		}
	}
}
