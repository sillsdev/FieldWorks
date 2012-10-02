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
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace SIL.FieldWorks.FDO.FdoGenerate
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Handles the owning and rel tags in the XMI file
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class RelationalProperty: Property
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:Property"/> class.
		/// </summary>
		/// <param name="node">The node.</param>
		/// <param name="parent">The parent class</param>
		/// ------------------------------------------------------------------------------------
		public RelationalProperty(XmlElement node, Class parent)
			: base(node, parent)
		{
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
		/// Gets the cardinality.
		/// </summary>
		/// <value>The cardinality.</value>
		/// ------------------------------------------------------------------------------------
		public override Card Cardinality
		{
			get
			{
				string card = m_node.Attributes["card"].Value;
				if (card == "atomic")
					return Card.Atomic;
				else if (card == "col")
					return Card.Collection;
				else if (card == "seq")
					return Card.Sequence;

				System.Diagnostics.Debug.Fail("Unexpected value for card: " + card);
				return Card.Unknown;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Outputs the column spec. Generate the ColumnSpec Push commands for each atomic REL .
		/// </summary>
		/// <param name="prefix"></param>
		/// <returns></returns>
		/// <remarks>ONLY DIFF between this and basic is THE SIG VARIABLE</remarks>
		/// ------------------------------------------------------------------------------------
		public override string OutputColumnSpec(string prefix)
		{
			if (Cardinality != Card.Atomic || IsOwning)
				return string.Empty;

			StringBuilder bldr = new StringBuilder();

			if (TypeInfo.RetrievalType != "special")
			{
				bldr.AppendLine(string.Format("// {0}: {1}", Name, Signature));
				bldr.Append(prefix);

				bldr.AppendLine(string.Format("cs.Push((int)DbColType.{0}, 1, {1}, 0);",
					TypeInfo.ColumnType, FlidLine));
			}
			return bldr.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the owning atomic flid.
		/// </summary>
		/// <param name="prefix">The prefix.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override string ProcessOwningAtomicFlid(string prefix)
		{
			if (!IsOwning || Cardinality != Card.Atomic)
				return string.Empty;

			return string.Format("{0}{1}", prefix, FlidLine);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the vector of flids.
		/// </summary>
		/// <param name="prefix">The prefix.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override string ProcessVectorFlids(string prefix)
		{
			if (Cardinality != Card.Sequence && Cardinality != Card.Collection)
				return string.Empty;

			return string.Format("{0}{1}", prefix, FlidLine);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Generates a list of  sequel view names needed for caching vector properties
		/// </summary>
		/// <param name="prefix">The prefix.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override string ProcessVectorViewNames(string prefix)
		{
			if (Cardinality != Card.Sequence && Cardinality != Card.Collection)
				return string.Empty;

			return string.Format("{0}{1}", prefix, ViewName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Generates a list of isSequence bools needed for caching vector properties
		/// </summary>
		/// <param name="prefix">The prefix.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override string ProcessVectorIsSequence(string prefix)
		{
			if (Cardinality != Card.Sequence && Cardinality != Card.Collection)
				return string.Empty;
			bool fIsSequence = Cardinality == Card.Sequence;
			return string.Format("{0}{1}", prefix, fIsSequence.ToString().ToLower());
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
				StringBuilder bldr = new StringBuilder();
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
				if (Cardinality == Card.Collection)
				{
					if (IsOwning)
						return "FdoOwningCollection";
					else
						return "FdoReferenceCollection";
				}
				else if (Cardinality == Card.Sequence)
				{
					if (IsOwning)
						return "FdoOwningSequence";
					else
						return "FdoReferenceSequence";
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
					if (IsOwning)
						return TypeInfo.TypeInfos["hvoAtomicOwning"];
					else
						return TypeInfo.TypeInfos["hvoAtomicReference"];
				}

				return base.TypeInfo;
			}
		}
	}
}
