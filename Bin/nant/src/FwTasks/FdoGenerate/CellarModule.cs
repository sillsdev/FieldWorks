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
// File: Module.cs
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

namespace SIL.FieldWorks.FDO.FdoGenerate
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Represents a module description in the XMI file
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class CellarModule: Base<Model>
	{
		private string m_Assembly;
		private string m_RelativePath;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:Module"/> class.
		/// </summary>
		/// <param name="node">The node.</param>
		/// <param name="parent">The model</param>
		/// ------------------------------------------------------------------------------------
		public CellarModule(XmlElement node, Model parent)
			: base(node, parent)
		{
			if (FdoGenerate.Generator.ModuleLocations.ContainsKey(Name))
			{
				ModuleInfo moduleInfo = FdoGenerate.Generator.ModuleLocations[Name];
				m_Assembly = moduleInfo.Assembly;
				m_RelativePath = moduleInfo.Path;
			}
			else
			{
				m_Assembly = "FDO";
				m_RelativePath = ".";
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of the module.
		/// </summary>
		/// <value>The number.</value>
		/// ------------------------------------------------------------------------------------
		public int Number
		{
			get { return Convert.ToInt32(m_node.Attributes["num"].Value); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the classes defined in this module.
		/// </summary>
		/// <value>The classes.</value>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<Class> Classes
		{
			get
			{
				StringKeyCollection<Class> classes = new StringKeyCollection<Class>();

				foreach (XmlElement elem in m_node.ChildNodes)
				{
					classes.Add(new Class(elem, this));
				}

				return classes;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the name of the assembly.
		/// </summary>
		/// <value>The assembly.</value>
		/// ------------------------------------------------------------------------------------
		public string Assembly
		{
			get { return m_Assembly; }
			set { m_Assembly = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the relative path to the assembly without trailing backslash.
		/// </summary>
		/// <value>The relative path.</value>
		/// ------------------------------------------------------------------------------------
		public string RelativePath
		{
			get { return m_RelativePath; }
			set { m_RelativePath = value; }
		}
	}
}
