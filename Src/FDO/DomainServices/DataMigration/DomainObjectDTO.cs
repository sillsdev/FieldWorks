// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Text;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class to be used as the Data Transfer Object (DTO) between BEP-land
	/// and Data Migration-land (DM-land). (DTOs move data from point A to point B,
	/// but have no behavior.)
	///
	/// An instance of DomainObjectDTO will represent one ICmObject of some
	/// class, but will be able to 'live' in an older model version and a newer
	/// model version during a data migration, where a real ICmObject could not.
	///
	/// Instances of this object will be available to DM-land via a special Repository.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DomainObjectDTO
	{
		private readonly string m_guid;
		private string m_classname;
		private byte[] m_xml;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="guid">The CmObject's guid as a string.</param>
		/// <param name="classname">The CmObject's class name.</param>
		/// <param name="xml">The CmObject's xml representation.</param>
		/// ------------------------------------------------------------------------------------
		internal DomainObjectDTO(string guid, string classname, byte[] xml)
		{
			if (string.IsNullOrEmpty(guid)) throw new ArgumentNullException("guid");
			if (string.IsNullOrEmpty(classname)) throw new ArgumentNullException("classname");
			if (xml == null || xml.Length == 0) throw new ArgumentNullException("xml");

			m_guid = guid.ToLower();
			m_classname = classname;
			m_xml = xml;
		}

		internal DomainObjectDTO(string guid, string classname, string xml)
			: this(guid, classname, Encoding.UTF8.GetBytes(xml))
		{

		}

		/// <summary>
		/// Get or set the object's xml representation.
		/// </summary>
		/// <remarks>
		/// NB: There is no check to make sure the setter uses valid xml,
		/// or that it is legal for <see cref="Classname"/>.
		///
		/// Unit tests on the data migration results should catch unexpected values
		/// put here.
		/// </remarks>
		public string Xml
		{
			get { return Encoding.UTF8.GetString(m_xml); }
			set { m_xml = Encoding.UTF8.GetBytes(value); }
		}

		/// <summary>
		/// Get the raw form of the XML string, encoded in UTF8.
		/// </summary>
		public byte[] XmlBytes
		{
			get { return m_xml; }
			set { m_xml = value; }
		}

		/// <summary>
		/// Get or set the object's class name.
		/// </summary>
		/// <remarks>
		/// NB: There is no check to make sure the setter uses a valid model class name.
		/// Nor is there a check to make sure <see cref="Xml"/> has the same class name,
		/// although they should.
		///
		/// Unit tests on the data migration results should catch unexpected values
		/// put here.
		/// </remarks>
		public string Classname
		{
			get { return m_classname; }
			set { m_classname = value; }
		}

		/// <summary>
		/// Get a string representation of the object's identifying guid.
		///
		/// Since an object's guid may not be changed,
		/// there is no provisio n made to change it usinng a setter.
		/// </summary>
		public string Guid
		{
			get { return m_guid; }
		}

		/// <summary>
		/// Override to check equality for this type.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			if (!(obj is DomainObjectDTO))
				return false;

			return m_guid.ToLower() == ((DomainObjectDTO)obj).m_guid.ToLower();
		}

		/// <summary>
		/// Override to get better hashcode.
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return m_guid.GetHashCode();
		}

		/// <summary>
		/// Override to return guid.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return m_guid;
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Provide information on class and superclass of a <see cref="DomainObjectDTO"/>.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class ClassStructureInfo
	{
		/// <summary>Superclass</summary>
		internal readonly string m_superClassName;
		/// <summary>Class</summary>
		internal readonly string m_className;

		/// <summary>
		/// Constructor
		/// </summary>
		internal ClassStructureInfo(string superClassName, string className)
		{
			m_superClassName = superClassName;
			m_className = className;
		}
	}
}