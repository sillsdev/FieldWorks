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
// File: Base.cs
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
	/// Common base class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class Base<T>
	{
		/// <summary></summary>
		protected XmlElement m_node;
		/// <summary></summary>
		protected T m_Parent;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the class.
		/// </summary>
		/// <param name="node">The node.</param>
		/// <param name="parent">The parent</param>
		/// ------------------------------------------------------------------------------------
		public Base(XmlElement node, T parent)
		{
			m_node = node;
			m_Parent = parent;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the parent.
		/// </summary>
		/// <value>The parent.</value>
		/// ------------------------------------------------------------------------------------
		public T Parent
		{
			get { return m_Parent; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the class.
		/// </summary>
		/// <value>The name.</value>
		/// ------------------------------------------------------------------------------------
		public string Name
		{
			get { return m_node.Attributes["id"].Value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a <see cref="T:System.String"></see> that represents the current
		/// <see cref="T:System.Object"></see>.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"></see> that represents the current
		/// <see cref="T:System.Object"></see>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return Name;
		}

		/// <summary>
		/// Get the &lt;para&gt; child nodes formatted for use in MS C# code.
		/// </summary>
		/// <param name="tabs">The tabs.</param>
		/// <param name="parentNode">The parent node.</param>
		/// <returns></returns>
		protected static string AsMSString(string tabs, XmlNode parentNode)
		{
			if (parentNode == null)
				return string.Empty;

			var retval = string.Empty;
			foreach (XmlNode paraNode in parentNode.SelectNodes("para"))
				retval = retval + tabs+ "/// " + paraNode.OuterXml + Environment.NewLine;
			return retval.TrimEnd();
		}
	}
}
