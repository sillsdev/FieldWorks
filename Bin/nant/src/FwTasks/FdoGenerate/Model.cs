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
// File: Model.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.ObjectModel;
using System.Xml;

namespace SIL.FieldWorks.FDO.FdoGenerate
{
	#region StringKeyCollection
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Collection which can also be accessed by Name.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class StringKeyCollection<T> : KeyedCollection<string, T>
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When implemented in a derived class, extracts the key from the specified element.
		/// </summary>
		/// <param name="item">The element from which to extract the key.</param>
		/// <returns>The key for the specified element.</returns>
		/// ------------------------------------------------------------------------------------
		protected override string GetKeyForItem(T item)
		{
			return item.ToString();
		}
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class Model
	{
		private StringKeyCollection<CellarModule> m_modules;
		private readonly XmlElement m_node;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="Model"/> class.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------
		public Model(XmlElement node)
		{
			m_node = node;
		}

		/// <summary>
		/// Get the model's version number.
		/// </summary>
		public int VersionNumber
		{
			get { return Int32.Parse(m_node.GetAttribute("version")); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the modules.
		/// </summary>
		/// <value>The modules.</value>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<CellarModule> Modules
		{
			get
			{
				if (m_modules == null)
				{
					m_modules = new StringKeyCollection<CellarModule>();

					foreach (XmlElement elem in m_node.ChildNodes)
					{
						// Skip FeatSys, since it is empty.
						if (elem.Name == "FeatSys") continue;

						m_modules.Add(new CellarModule(elem, this));
					}
				}
				return m_modules;
			}
		}
	}
}
