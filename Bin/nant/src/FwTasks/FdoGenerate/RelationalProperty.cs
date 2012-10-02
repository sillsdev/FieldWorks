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
// File: RelationalProperty.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System.Text;
using System.Xml;

namespace SIL.FieldWorks.FDO.FdoGenerate
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Handles the owning and rel tags in the XMI file
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class RelationalProperty : Property
	{
		private readonly int m_backRefNumber;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="RelationalProperty"/> class.
		/// </summary>
		/// <param name="node">The node.</param>
		/// <param name="parent">The parent class</param>
		/// ------------------------------------------------------------------------------------
		public RelationalProperty(XmlElement node, Class parent)
			: base(node, parent)
		{
			m_backRefNumber = Number + Class.kTenMillion;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this property is owning.
		/// </summary>
		/// <value><c>true</c> if this instance is owning; otherwise, <c>false</c>.</value>
		/// ------------------------------------------------------------------------------------
		public bool IsOwning
		{
			get { return m_node.Name == "owning"; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the Back Reference number.
		/// </summary>
		/// <value>0 for non-reference properties.</value>
		/// ------------------------------------------------------------------------------------
		public override int BackRefNumber
		{
			get { return m_backRefNumber; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the cardinality.
		/// </summary>
		/// <value>The cardinality.</value>
		/// ------------------------------------------------------------------------------------
		public override Card Cardinality
		{
			get
			{
				var card = m_node.Attributes["card"].Value;
				switch (card)
				{
					case "atomic":
						return Card.Atomic;
					case "col":
						return Card.Collection;
					case "seq":
						return Card.Sequence;
				}

				System.Diagnostics.Debug.Fail("Unexpected value for card: " + card);
				return Card.Unknown;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the property name with prop type appended.
		/// </summary>
		/// <value>The name of the niuginian prop.</value>
		/// ------------------------------------------------------------------------------------
		public override string NiuginianPropName
		{
			get
			{
				var bldr = new StringBuilder();
				bldr.Append(base.NiuginianPropName);
				if (IsOwning)
					bldr.Append("O");
				else
					bldr.Append("R");
				switch (Cardinality)
				{
					case Card.Atomic:
						bldr.Append("A");
						break;
					case Card.Sequence:
						bldr.Append("S");
						break;
					case Card.Collection:
						bldr.Append("C");
						break;
				}
				return bldr.ToString();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the C# type of this property.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public override string CSharpType
		{
			get
			{
				switch (Cardinality)
				{
					case Card.Collection:
						return IsOwning ? "FdoOwningVector" : "FdoReferenceCollection";
					case Card.Sequence:
						return IsOwning ? "FdoOwningVector" : "FdoReferenceSequence";
				}
				return base.CSharpType;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the type info.
		/// </summary>
		/// <value>The type info.</value>
		/// ------------------------------------------------------------------------------------
		protected override TypeInfo TypeInfo
		{
			get
			{
				if (Cardinality == Card.Atomic)
				{
					return IsOwning ? TypeInfo.TypeInfos["hvoAtomicOwning"] : TypeInfo.TypeInfos["hvoAtomicReference"];
				}

				return base.TypeInfo;
			}
		}
	}
}
