// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: CustomPropertyAttributes.cs
// Responsibility: Randy Regnier
// Last reviewed:
//
// <remarks>
// Custom attributes (subclasses of Attribute) go here.
// </remarks>

using System;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.FDO.Infrastructure
{
	/// <summary>
	/// An attribute that is applied to each model-defined class.
	/// </summary>
	/// <remarks>
	/// Only use "ModelClass" to mark them,
	/// not complete class name of "ModelClassAttribute".
	/// </remarks>
	[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
	internal sealed class ModelClassAttribute : Attribute
	{
		private readonly int m_clsid;
		private readonly string m_type;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="clsid">Class ID for the class this attribute is on.</param>
		/// <param name="type">String representation of the type of object the attribute is on.</param>
		public ModelClassAttribute(int clsid, string type)
		{
			m_clsid = clsid;
			m_type = type;
		}

		/// <summary>
		/// Gets the Class Id for the model Class.
		/// </summary>
		public int Clsid
		{
			get { return m_clsid; }
		}

		/// <summary>
		/// Gets the return object's Type as a string.
		/// </summary>
		public string Type
		{
			get { return m_type; }
		}
	}

	/// <summary>
	/// An attribute that is applied to generated model properties.
	/// </summary>
	/// <remarks>
	/// Only use "ModelProperty" to mark them,
	/// not complete class name of "ModelPropertyAttribute".
	/// </remarks>
	[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
	internal sealed class ModelPropertyAttribute : Attribute
	{
		private readonly CellarPropertyType m_flidType;
		private readonly int m_flid;
		private readonly string m_signature;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="flidType">Type of property.</param>
		/// <param name="flid">Field ID</param>
		/// <param name="signature">return value signature</param>
		public ModelPropertyAttribute(CellarPropertyType flidType, int flid, string signature)
		{
			if (string.IsNullOrEmpty(signature)) throw new ArgumentNullException("signature");

			m_flidType = flidType;
			m_flid = flid;
			m_signature = signature;
		}

		/// <summary>
		/// Get the Flid type.
		/// </summary>
		public CellarPropertyType FlidType
		{
			get { return m_flidType; }
		}

		/// <summary>
		/// Gets the Field Id for the model property.
		/// </summary>
		public int Flid
		{
			get { return m_flid; }
		}

		/// <summary>
		/// Gets the return object's Signature (interface) for the generated virtual property.
		/// This is what is in the back reference collection, not the collection itself.
		/// </summary>
		public string Signature
		{
			get { return m_signature; }
		}
	}

	/// <summary>
	/// An attribute that is applied to manually virtual properties.
	/// Any non-model property that expects to be accessed by the SDA Facade
	/// must add this attribute to the relevant property.
	/// </summary>
	/// <remarks>
	/// Only use "VirtualProperty" to mark them,
	/// not complete class name of "VirtualPropertyAttribute".
	/// </remarks>
	[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
	internal sealed class VirtualPropertyAttribute : Attribute
	{
		private static int s_nextVirtualFlid = 20000000;
		private const int kMaximumVirtualLim = 30000000;

		private static int GetNextFlid()
		{
			if (++s_nextVirtualFlid > kMaximumVirtualLim)
				throw new InvalidOperationException("Virtual flid is greater than the allowed maximum.");
			return s_nextVirtualFlid;
		}

		internal static void ResetFlidCounter()
		{
			s_nextVirtualFlid = 20000000;
		}

		private readonly CellarPropertyType m_flidType;
		private readonly int m_virtualFlid;
		private readonly string m_signature;

		/// <summary>
		/// Constructor for object properties. Signature is the name (unqualified) of the class, e.g., "LexEntry".
		/// </summary>
		public VirtualPropertyAttribute(CellarPropertyType flidType, string signature)
		{
			m_flidType = flidType;
			m_virtualFlid = GetNextFlid();
			m_signature = signature;
		}
		/// <summary>
		/// Constructor for non-object properties.
		/// </summary>
		public VirtualPropertyAttribute(CellarPropertyType flidType)
			:this(flidType, "")
		{
		}

		/// <summary>
		/// Gets the Field Type for the virtual property.
		/// </summary>
		public CellarPropertyType FlidType
		{
			get { return m_flidType; }
		}

		/// <summary>
		/// Gets the Field Id for the virtual property.
		/// </summary>
		public int Flid
		{
			get { return m_virtualFlid; }
		}

		/// <summary>
		/// Gets the return object's Signature (interface for CmObjects) for the virtual property.
		/// </summary>
		public string Signature
		{
			get { return m_signature; }
		}
	}
}