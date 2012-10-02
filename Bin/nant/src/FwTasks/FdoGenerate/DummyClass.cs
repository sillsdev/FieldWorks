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
// File: DummyClass.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;

namespace SIL.FieldWorks.FDO.FdoGenerate
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Used for non-Cellar classes
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyClass : IClass
	{
		#region IClass Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the abbreviation.
		/// </summary>
		/// <value>The abbreviation.</value>
		/// ------------------------------------------------------------------------------------
		public string Abbreviation
		{
			get { return string.Empty; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the base class.
		/// </summary>
		/// <value>The base class.</value>
		/// ------------------------------------------------------------------------------------
		public Class BaseClass
		{
			get { return null; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the optional class comment.
		/// </summary>
		/// <value>The optional comment, or an empty string.</value>
		/// ------------------------------------------------------------------------------------
		public string Comment
		{
			get { return string.Empty; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the optional notes.
		/// </summary>
		/// <value>The optional notes, or an empty string.</value>
		/// ------------------------------------------------------------------------------------
		public string Notes
		{
			get { return string.Empty; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the depth.
		/// </summary>
		/// <value>The depth.</value>
		/// ------------------------------------------------------------------------------------
		public int Depth
		{
			get { return 0; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance is abstract.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is abstract; otherwise, <c>false</c>.
		/// </value>
		/// ------------------------------------------------------------------------------------
		public bool IsAbstract
		{
			get { return false; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance is hand generated.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is hand generated; otherwise, <c>false</c>.
		/// </value>
		/// ------------------------------------------------------------------------------------
		public bool IsHandGenerated
		{
			get { return false; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets if the class is a singleton or not.
		/// </summary>
		/// <value><c>true</c>, if the class is a singleton, otherwise <c>false</c>.</value>
		/// ------------------------------------------------------------------------------------
		public bool IsSingleton
		{
			get { return false; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets if the class' factory should generate the 'braindead' method,
		/// or throw the NotSupportedException.
		/// </summary>
		/// <value>Return <c>true</c>, if the class' factory shoudl generate the braindead impl
		/// of the Create method.
		/// Return <c>false</c> when the NotSupportedExceptin is to be thrown.</value>
		/// ------------------------------------------------------------------------------------
		public bool GenerateFullCreateMethod
		{
			get { return true; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the ownership requirements.
		/// </summary>
		/// <value>'required', 'none', 'optional'.</value>
		/// ------------------------------------------------------------------------------------
		public string OwnerStatus
		{
			get { return "required"; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number.
		/// </summary>
		/// <value>The number.</value>
		/// ------------------------------------------------------------------------------------
		public int Number
		{
			get { return 0; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the properties.
		/// </summary>
		/// <value>The properties.</value>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<Property> Properties
		{
			get { return new StringKeyCollection<Property>(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the sub classes.
		/// </summary>
		/// <value>The sub classes.</value>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<Class> SubClasses
		{
			get { return new StringKeyCollection<Class>(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the object properties (owning/reference atomic/col/seq).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<RelationalProperty> ObjectProperties
		{
			get { return new StringKeyCollection<RelationalProperty>(); }
		}

		/// <summary>
		/// Get the atomic reference properties
		/// </summary>
		public StringKeyCollection<RelationalProperty> AtomicRefProperties
		{
			get { return new StringKeyCollection<RelationalProperty>(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the atomic owning properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<RelationalProperty> AtomicOwnProperties
		{
			get { return new StringKeyCollection<RelationalProperty>(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the vector object properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<RelationalProperty> VectorProperties
		{
			get { return new StringKeyCollection<RelationalProperty>(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the object owning properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<RelationalProperty> OwningProperties
		{
			get { return new StringKeyCollection<RelationalProperty>(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the object owning collection properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<RelationalProperty> CollectionOwnProperties
		{
			get { return new StringKeyCollection<RelationalProperty>(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the object owning sequence properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<RelationalProperty> SequenceOwnProperties
		{
			get { return new StringKeyCollection<RelationalProperty>(); }
		}

		/// <summary>
		/// Get the object reference collection properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<RelationalProperty> CollectionRefProperties
		{
			get { return new StringKeyCollection<RelationalProperty>(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the object reference sequence properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<RelationalProperty> SequenceRefProperties
		{
			get { return new StringKeyCollection<RelationalProperty>(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the reference properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<RelationalProperty> ReferenceProperties
		{
			get { return new StringKeyCollection<RelationalProperty>(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the non-object properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<Property> BasicProperties
		{
			get { return new StringKeyCollection<Property>(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the object collection properties (owning and reference)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<RelationalProperty> CollectionProperties
		{
			get { return new StringKeyCollection<RelationalProperty>(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the object sequence properties (owning and reference)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<RelationalProperty> SequenceProperties
		{
			get { return new StringKeyCollection<RelationalProperty>(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the integer properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<Property> IntegerProperties
		{
			get { return new StringKeyCollection<Property>(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the boolean properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<Property> BooleanProperties
		{
			get { return new StringKeyCollection<Property>(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the Guid properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<Property> GuidProperties
		{
			get { return new StringKeyCollection<Property>(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the DateTime properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<Property> DateTimeProperties
		{
			get { return new StringKeyCollection<Property>(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the GenDate properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<Property> GenDateProperties
		{
			get { return new StringKeyCollection<Property>(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the Binary properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<Property> BinaryProperties
		{
			get { return new StringKeyCollection<Property>(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the TsString properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<Property> StringProperties
		{
			get { return new StringKeyCollection<Property>(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the Multi (string/Unicode) properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<Property> MultiProperties
		{
			get { return new StringKeyCollection<Property>(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the Unicode (regular C#) properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<Property> UnicodeProperties
		{
			get { return new StringKeyCollection<Property>(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the TextPropBinary properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<Property> TextPropBinaryProperties
		{
			get { return new StringKeyCollection<Property>(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the atomic object properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<RelationalProperty> AtomicProperties
		{
			get { return new StringKeyCollection<RelationalProperty>(); }
		}

		#endregion
	}
}
