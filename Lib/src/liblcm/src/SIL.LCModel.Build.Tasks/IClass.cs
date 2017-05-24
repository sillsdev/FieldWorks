// Copyright (c) 2006-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.LCModel.Build.Tasks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IClass
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the abbreviation.
		/// </summary>
		/// <value>The abbreviation.</value>
		/// ------------------------------------------------------------------------------------
		string Abbreviation { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the base class.
		/// </summary>
		/// <value>The base class.</value>
		/// ------------------------------------------------------------------------------------
		Class BaseClass { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the optional class comment.
		/// </summary>
		/// <value>The optional comment, or an empty string.</value>
		/// ------------------------------------------------------------------------------------
		string Comment { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the optional notes.
		/// </summary>
		/// <value>The optional notes, or an empty string.</value>
		/// ------------------------------------------------------------------------------------
		string Notes { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the depth.
		/// </summary>
		/// <value>The depth.</value>
		/// ------------------------------------------------------------------------------------
		int Depth { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance is abstract.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is abstract; otherwise, <c>false</c>.
		/// </value>
		/// ------------------------------------------------------------------------------------
		bool IsAbstract { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets if the class is a singleton or not.
		/// </summary>
		/// <value><c>true</c>, if the class is a singleton, otherwise <c>false</c>.</value>
		/// ------------------------------------------------------------------------------------
		bool IsSingleton { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets if the class' factory should generate the 'braindead' method,
		/// or throw the NotSupportedException.
		/// </summary>
		/// <value>Return <c>true</c>, if the class' factory shoudl generate the braindead impl
		/// of the Create method.
		/// Return <c>false</c> when the NotSupportedExceptin is to be thrown.</value>
		/// ------------------------------------------------------------------------------------
		bool GenerateFullCreateMethod { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the ownership requirements.
		/// </summary>
		/// <value>'required', 'none', 'optional'.</value>
		/// ------------------------------------------------------------------------------------
		string OwnerStatus { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number.
		/// </summary>
		/// <value>The number.</value>
		/// ------------------------------------------------------------------------------------
		int Number { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the properties.
		/// </summary>
		/// <value>The properties.</value>
		/// ------------------------------------------------------------------------------------
		StringKeyCollection<Property> Properties { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the sub classes.
		/// </summary>
		/// <value>The sub classes.</value>
		/// ------------------------------------------------------------------------------------
		StringKeyCollection<Class> SubClasses { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the object properties (owning/reference atomic/col/seq).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		StringKeyCollection<RelationalProperty> ObjectProperties { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the atomic object properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		StringKeyCollection<RelationalProperty> AtomicProperties { get; }
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the atomic reference properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		StringKeyCollection<RelationalProperty> AtomicRefProperties { get; }
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the atomic owning properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		StringKeyCollection<RelationalProperty> AtomicOwnProperties { get; }
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the vector object properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		StringKeyCollection<RelationalProperty> VectorProperties { get; }
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the object owning properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		StringKeyCollection<RelationalProperty> OwningProperties { get; }
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the object owning collection properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		StringKeyCollection<RelationalProperty> CollectionOwnProperties { get; }
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the object owning sequence properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		StringKeyCollection<RelationalProperty> SequenceOwnProperties { get; }
		/// <summary>
		/// Get the object reference collection properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		StringKeyCollection<RelationalProperty> CollectionRefProperties { get; }
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the object reference sequence properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		StringKeyCollection<RelationalProperty> SequenceRefProperties { get; }
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the reference properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		StringKeyCollection<RelationalProperty> ReferenceProperties { get; }
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the non-object properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		StringKeyCollection<Property> BasicProperties { get; }
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the object collection properties (owning and reference)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		StringKeyCollection<RelationalProperty> CollectionProperties { get; }
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the object sequence properties (owning and reference)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		StringKeyCollection<RelationalProperty> SequenceProperties { get; }
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the integer properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		StringKeyCollection<Property> IntegerProperties { get; }
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the boolean properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		StringKeyCollection<Property> BooleanProperties { get; }
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the Guid properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		StringKeyCollection<Property> GuidProperties { get; }
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the DateTime properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		StringKeyCollection<Property> DateTimeProperties { get; }
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the GenDate properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		StringKeyCollection<Property> GenDateProperties { get; }
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the Binary properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		StringKeyCollection<Property> BinaryProperties { get; }
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the TsString properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		StringKeyCollection<Property> StringProperties { get; }
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the Multi (string/Unicode) properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		StringKeyCollection<Property> MultiProperties { get; }
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the Unicode (regular C#) properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		StringKeyCollection<Property> UnicodeProperties { get; }
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the TextPropBinary properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		StringKeyCollection<Property> TextPropBinaryProperties { get; }
	}
}
