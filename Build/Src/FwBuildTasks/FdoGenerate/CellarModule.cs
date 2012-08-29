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
