// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: XmlAssembly.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Xml;
using System.Xml.Serialization;

namespace SIL.FieldWorks.Build.Tasks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Represents one reference of an assembly
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class Reference
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="Reference"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Reference()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="Reference"/> class.
		/// </summary>
		/// <param name="name">The name and path of the reference</param>
		/// ------------------------------------------------------------------------------------
		public Reference(string name)
		{
			m_Reference = name;
		}

		private string m_Reference;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the reference
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlAttribute("name")]
		public string Name
		{
			get { return m_Reference; }
			set { m_Reference = value; }
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Represents one assembly
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class XmlAssembly
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="XmlAssembly"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public XmlAssembly()
		{
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="XmlAssembly"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public XmlAssembly(string name)
		{
			m_Name = name;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="XmlAssembly"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public XmlAssembly(string name, Hashtable references)
		{
			m_Name = name;
			foreach (DictionaryEntry reference in references)
			{
				m_References.Add(new Reference((string)reference.Value));
			}
		}

		private string m_Name;
		private ArrayList m_References = new ArrayList();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the name of the assembly
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlAttribute(AttributeName = "name")]
		public string AssemblyName
		{
			get { return m_Name; }
			set { m_Name = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the references for this assembly
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlElement(typeof(Reference), ElementName = "references")]
		public ArrayList References
		{
			get { return m_References; }
			set { m_References = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a new reference to the assembly
		/// </summary>
		/// <param name="reference">Name and path of the reference</param>
		/// ------------------------------------------------------------------------------------
		public void Add(string reference)
		{
			m_References.Add(new Reference(reference));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified assembly is equal to the current assembly
		/// </summary>
		/// <param name="obj">The assembly to compare with the current assembly</param>
		/// <returns><c>true</c> if the assemblies are the same, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		public override bool Equals(object obj)
		{
			XmlAssembly assembly = obj as XmlAssembly;
			if (assembly != null)
				return (assembly.AssemblyName == AssemblyName);

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Serves as hash function
		/// </summary>
		/// <returns>A hash code for the current assembly</returns>
		/// ------------------------------------------------------------------------------------
		public override int GetHashCode()
		{
			return AssemblyName.GetHashCode();
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Contains a map of all the references
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[XmlRoot(ElementName = "referenceCache")]
	public class ReferenceCache
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ReferenceCache"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ReferenceCache()
		{
		}

		private XmlAssemblyCollection m_assemblies = new XmlAssemblyCollection();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The list of assemblies
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlElement(typeof(XmlAssembly), ElementName = "assembly")]
		public XmlAssemblyCollection Assemblies
		{
			get
			{
				return m_assemblies;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add an assembly
		/// </summary>
		/// <param name="assembly">The assembly</param>
		/// ------------------------------------------------------------------------------------
		public void Add(XmlAssembly assembly)
		{
			m_assemblies[assembly.AssemblyName] = assembly;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the assembly
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public XmlAssembly this[string assemblyName]
		{
			get
			{
				return m_assemblies[assemblyName];
			}
		}
	}
}
