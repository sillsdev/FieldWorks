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
// File: Class.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace SIL.FieldWorks.FDO.FdoGenerate
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Represents a Class description in the XMI file
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class Class: Base<CellarModule>, IClass
	{
		/// <summary>
		/// Counter for incrementing virtual generated flids for back references.
		/// </summary>
// ReSharper disable InconsistentNaming
		public const int kTenMillion = 10000000;
// ReSharper restore InconsistentNaming
		private StringKeyCollection<Property> m_properties;
		private StringKeyCollection<RelationalProperty> m_objectProperties;
		private StringKeyCollection<RelationalProperty> m_atomicProperties;
		private StringKeyCollection<RelationalProperty> m_atomicRefProperties;
		private StringKeyCollection<RelationalProperty> m_atomicOwnProperties;
		private StringKeyCollection<RelationalProperty> m_vectorProperties;
		private StringKeyCollection<RelationalProperty> m_owningProperties;
		private StringKeyCollection<RelationalProperty> m_referenceProperties;
		private StringKeyCollection<RelationalProperty> m_collectionOwnProperties;
		private StringKeyCollection<RelationalProperty> m_sequenceOwnProperties;
		private StringKeyCollection<RelationalProperty> m_collectionRefProperties;
		private StringKeyCollection<RelationalProperty> m_sequenceRefProperties;
		private StringKeyCollection<RelationalProperty> m_collectionProperties;
		private StringKeyCollection<RelationalProperty> m_sequenceProperties;

		private StringKeyCollection<Property> m_basicProperties;
		private StringKeyCollection<Property> m_integerProperties;
		private StringKeyCollection<Property> m_booleanProperties;
		private StringKeyCollection<Property> m_guidProperties;
		private StringKeyCollection<Property> m_dateTimeProperties;
		private StringKeyCollection<Property> m_genDateProperties;
		private StringKeyCollection<Property> m_binaryProperties;
		private StringKeyCollection<Property> m_tsStringProperties;
		private StringKeyCollection<Property> m_multiProperties;
		private StringKeyCollection<Property> m_unicodeProperties;
		private StringKeyCollection<Property> m_textPropBinaryProperties;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="Class"/> class.
		/// </summary>
		/// <param name="node">The node.</param>
		/// <param name="parent">The model</param>
		/// ------------------------------------------------------------------------------------
		public Class(XmlElement node, CellarModule parent)
			: base(node, parent)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets if the class is a singleton or not.
		/// </summary>
		/// <value><c>true</c>, if the class is a singleton, otherwise <c>false</c>.</value>
		/// ------------------------------------------------------------------------------------
		public bool IsSingleton
		{
			get
			{
				var isSingleton = m_node.GetAttribute("singleton");
				return (string.IsNullOrEmpty(isSingleton)) ? false : bool.Parse(isSingleton);
			}
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
			get
			{
				var generateFullCreate = m_node.GetAttribute("generateBasicCreateMethod");
				return (string.IsNullOrEmpty(generateFullCreate)) ? true : bool.Parse(generateFullCreate);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the ownership requirements.
		/// </summary>
		/// <value>'required', 'none', 'optional'.</value>
		/// ------------------------------------------------------------------------------------
		public string OwnerStatus
		{
			get
			{
				var ownerStatus = m_node.GetAttribute("owner");
				return (string.IsNullOrEmpty(ownerStatus)) ? "required" : ownerStatus;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of the class.
		/// </summary>
		/// <value>The number.</value>
		/// ------------------------------------------------------------------------------------
		public int Number
		{
			get
			{
				return Parent.Number * 1000 + Convert.ToInt32(m_node.Attributes["num"].Value);
			}
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
			get
			{
				return Name == "CmObject" || Convert.ToBoolean(m_node.Attributes["abstract"].Value);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the abbreviation.
		/// </summary>
		/// <value>The abbreviation.</value>
		/// ------------------------------------------------------------------------------------
		public string Abbreviation
		{
			get { return m_node.Attributes["abbr"].Value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the base class.
		/// </summary>
		/// <value>The name of the base class.</value>
		/// ------------------------------------------------------------------------------------
		private string InternalBaseClassName
		{
			get
			{
				var attribute = m_node.Attributes["base"];
				return attribute == null ? string.Empty : attribute.Value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the base class.
		/// </summary>
		/// <value>The base class.</value>
		/// ------------------------------------------------------------------------------------
		public Class BaseClass
		{
			get
			{
				var baseClassName = InternalBaseClassName;
				if (baseClassName == string.Empty)
					return null;

				foreach (var module in Parent.Parent.Modules)
				{
					if (module.Classes.Contains(baseClassName))
						return module.Classes[baseClassName];
				}
				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the optional class comment.
		/// </summary>
		/// <value>The optional comment, or an empty string.</value>
		/// ------------------------------------------------------------------------------------
		public string Comment
		{
			get
			{
				return AsMSString("\t", m_node.SelectSingleNode("comment"));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the optional notes.
		/// </summary>
		/// <value>The optional notes, or an empty string.</value>
		/// ------------------------------------------------------------------------------------
		public string Notes
		{
			get
			{
				return AsMSString("\t", m_node.SelectSingleNode("notes"));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the depth.
		/// </summary>
		/// <value>The depth.</value>
		/// ------------------------------------------------------------------------------------
		public int Depth
		{
			get
			{
				var attribute = m_node.Attributes["depth"];
				return attribute == null ? 0 : Convert.ToInt32(attribute.Value);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the properties.
		/// </summary>
		/// <value>The properties.</value>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<Property> Properties
		{
			get
			{
				if (m_properties == null)
				{
					m_properties = new StringKeyCollection<Property>();
					foreach (XmlElement elem in m_node.SelectSingleNode("props").ChildNodes)
					{
						switch (elem.Name)
						{
							case "basic":
								m_properties.Add(new Property(elem, this));
								break;
							case "owning":
								m_properties.Add(new RelationalProperty(elem, this));
								break;
							case "rel":
								m_properties.Add(new RelationalProperty(elem, this));
								break;
						}
					}
/*
					// Add back reference props.
					// <rel num="12" id="CompoundRuleApps" card="seq" sig="MoCompoundRule"></rel>
					var query =
						String.Format("//EntireModel/CellarModule/class/props/rel[@sig='{0}']",
							Name);
// ReSharper disable PossibleNullReferenceException
					foreach (XmlElement backRefNode in m_node.OwnerDocument.SelectNodes(query))
// ReSharper restore PossibleNullReferenceException
						m_properties.Add(new BackReferenceProperty(backRefNode, this));
*/
				}

				return m_properties;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the sub classes.
		/// </summary>
		/// <value>The sub classes.</value>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<Class> SubClasses
		{
			get
			{
				var subClasses = new StringKeyCollection<Class>();
				foreach (var module in Parent.Parent.Modules)
				{
					foreach (var cls in module.Classes)
					{
						if (cls.InternalBaseClassName == Name)
							subClasses.Add(cls);
					}
				}
				return subClasses;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the object properties (owning/reference atomic/col/seq).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<RelationalProperty> ObjectProperties
		{
			get
			{
				return GatherObjectProperties(ref m_objectProperties,
					from property in Properties
					where property is RelationalProperty
					select (RelationalProperty)property);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the atomic reference properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<RelationalProperty> AtomicProperties
		{
			get
			{
				return GatherObjectProperties(ref m_atomicProperties,
					from property in ObjectProperties
					where property.Cardinality == Property.Card.Atomic
					select property);
			}
		}

		/// <summary>
		/// Get the atomic reference properties
		/// </summary>
		public StringKeyCollection<RelationalProperty> AtomicRefProperties
		{
			get
			{
				return GatherObjectProperties(ref m_atomicRefProperties,
					from property in AtomicProperties
					where !property.IsOwning
					select property);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the atomic reference properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<RelationalProperty> AtomicOwnProperties
		{
			get
			{
				return GatherObjectProperties(ref m_atomicOwnProperties,
					from property in AtomicProperties
					where property.IsOwning
					select property);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the vector object properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<RelationalProperty> VectorProperties
		{
			get
			{
				return GatherObjectProperties(ref m_vectorProperties,
					from property in ObjectProperties
					where property.Cardinality != Property.Card.Atomic
					select property);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the object owning properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<RelationalProperty> OwningProperties
		{
			get
			{
				return GatherObjectProperties(ref m_owningProperties,
					from property in ObjectProperties
					where property.IsOwning
					select property);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the object owning collection properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<RelationalProperty> CollectionOwnProperties
		{
			get
			{
				return GatherObjectProperties(ref m_collectionOwnProperties,
					from property in OwningProperties
					where property.Cardinality == Property.Card.Collection
					select property);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the object owning sequence properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<RelationalProperty> SequenceOwnProperties
		{
			get
			{
				return GatherObjectProperties(ref m_sequenceOwnProperties,
					from property in OwningProperties
					where property.Cardinality == Property.Card.Sequence
					select property);
			}
		}

		/// <summary>
		/// Get the object reference collection properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<RelationalProperty> CollectionRefProperties
		{
			get
			{
				return GatherObjectProperties(ref m_collectionRefProperties,
					from property in ReferenceProperties
					where property.Cardinality == Property.Card.Collection
					select property);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the object reference sequence properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<RelationalProperty> SequenceRefProperties
		{
			get
			{
				return GatherObjectProperties(ref m_sequenceRefProperties,
					from property in ReferenceProperties
					where property.Cardinality == Property.Card.Sequence
					select property);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the reference properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<RelationalProperty> ReferenceProperties
		{
			get
			{
				return GatherObjectProperties(ref m_referenceProperties,
					from property in ObjectProperties
						where !property.IsOwning
						select property);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the non-object properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<Property> BasicProperties
		{
			get
			{
				return GatherNonObjectProperties(ref m_basicProperties,
					from property in Properties
					where property.Cardinality == Property.Card.Basic
					select property);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the object collection properties (owning and reference)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<RelationalProperty> CollectionProperties
		{
			get
			{
				return GatherObjectProperties(ref m_collectionProperties,
					from property in VectorProperties
					where property.Cardinality == Property.Card.Collection
					select property);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the object sequence properties (owning and reference)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<RelationalProperty> SequenceProperties
		{
			get
			{
				return GatherObjectProperties(ref m_sequenceProperties,
					from property in VectorProperties
					where property.Cardinality == Property.Card.Sequence
					select property);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the integer properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<Property> IntegerProperties
		{
			get
			{
				return GatherNonObjectProperties(ref m_integerProperties,
					from property in BasicProperties
					where property.Signature == "Integer"
					select property);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the boolean properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<Property> BooleanProperties
		{
			get
			{
				return GatherNonObjectProperties(ref m_booleanProperties,
					from property in BasicProperties
					where property.Signature == "Boolean"
					select property);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the boolean properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<Property> GuidProperties
		{
			get
			{
				return GatherNonObjectProperties(ref m_guidProperties,
					from property in BasicProperties
					where property.Signature == "Guid"
					select property);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the DateTime properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<Property> DateTimeProperties
		{
			get
			{
				return GatherNonObjectProperties(ref m_dateTimeProperties,
					from property in BasicProperties
					where property.Signature == "Time"
					select property);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the GenDate properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<Property> GenDateProperties
		{
			get
			{
				return GatherNonObjectProperties(ref m_genDateProperties,
					from property in BasicProperties
					where property.Signature == "GenDate"
					select property);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the Binary properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<Property> BinaryProperties
		{
			get
			{
				return GatherNonObjectProperties(ref m_binaryProperties,
					from property in BasicProperties
					where property.Signature == "Binary"
					select property);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the TsString properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<Property> StringProperties
		{
			get
			{
				return GatherNonObjectProperties(ref m_tsStringProperties,
					from property in BasicProperties
					where property.Signature == "String"
					select property);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the Multi (string/Unicode) properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<Property> MultiProperties
		{
			get
			{
				return GatherNonObjectProperties(ref m_multiProperties,
					from property in BasicProperties
					where property.Signature == "MultiString" || property.Signature == "MultiUnicode"
					select property);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the Unicode (regular C#) properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<Property> UnicodeProperties
		{
			get
			{
				return GatherNonObjectProperties(ref m_unicodeProperties,
					from property in BasicProperties
					where property.Signature == "Unicode"
					select property);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the TextPropBinary properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<Property> TextPropBinaryProperties
		{
			get
			{
				return GatherNonObjectProperties(ref m_textPropBinaryProperties,
					from property in BasicProperties
					where property.Signature == "TextPropBinary"
					select property);
			}
		}

		private static StringKeyCollection<Property> GatherNonObjectProperties(ref StringKeyCollection<Property> propertyCollection, IEnumerable<Property> query)
		{
			if (propertyCollection == null)
			{
				propertyCollection = new StringKeyCollection<Property>();
				foreach (var relationalProperty in query)
					propertyCollection.Add(relationalProperty);
			}

			return propertyCollection;
		}

		private static StringKeyCollection<RelationalProperty> GatherObjectProperties(ref StringKeyCollection<RelationalProperty> propertyCollection, IEnumerable<RelationalProperty> query)
		{
			if (propertyCollection == null)
			{
				propertyCollection = new StringKeyCollection<RelationalProperty>();
				foreach (var relationalProperty in query)
					propertyCollection.Add(relationalProperty);
			}

			return propertyCollection;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets all of the class' superclasses (includes this class).
		/// </summary>
		/// <value>The superclasses.</value>
		/// <remarks>
		/// CmObject should be first in the list, and this class should be last.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public StringKeyCollection<Class> Superclasses
		{
			get
			{
				var superclasses = new StringKeyCollection<Class>();
				if (BaseClass != null)
					superclasses = BaseClass.Superclasses;
				superclasses.Add(this);

				return superclasses;
			}
		}
	}
}
