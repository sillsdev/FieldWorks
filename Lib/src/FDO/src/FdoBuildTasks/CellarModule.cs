// Copyright (c) 2006-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Xml;

namespace SIL.FieldWorks.FDO.Build.Tasks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Represents a module description in the XMI file
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class CellarModule: Base<Model>
	{
		private StringKeyCollection<Class> m_classes;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="CellarModule"/> class.
		/// </summary>
		/// <param name="node">The node.</param>
		/// <param name="parent">The model</param>
		/// ------------------------------------------------------------------------------------
		public CellarModule(XmlElement node, Model parent)
			: base(node, parent)
		{
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
				if (m_classes == null)
				{
					m_classes = new StringKeyCollection<Class>();

					foreach (XmlElement elem in m_node.ChildNodes)
						m_classes.Add(new Class(elem, this));
				}

				return m_classes;
			}
		}
	}
}
