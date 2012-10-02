//-------------------------------------------------------------------------------------------------
// <copyright file="Reference.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//
//    The use and distribution terms for this software are covered by the
//    Common Public License 1.0 (http://opensource.org/licenses/cpl.php)
//    which can be found in the file CPL.TXT at the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by
//    the terms of this license.
//
//    You must not remove this notice, or any other, from this software.
// </copyright>
//
// <summary>
// A reference to another section.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;
	using System.Xml;

	/// <summary>
	/// A reference to another section.
	/// </summary>
	public class Reference : IComparable
	{
		private string tableName;
		private string symbolId;

		/// <summary>
		/// Instantiate a new Reference.
		/// </summary>
		/// <param name="tableName">Table name of the reference.</param>
		/// <param name="symbolId">Symbol Id of the reference.</param>
		public Reference(string tableName, string symbolId)
		{
			if (null == tableName || 0 == tableName.Length)
			{
				throw new ArgumentNullException(tableName, "Reference table name cannot be null or empty");
			}
			if (null == symbolId || 0 == symbolId.Length)
			{
				throw new ArgumentNullException(symbolId, "Reference symbol id cannot be null or empty");
			}

			this.tableName = tableName;
			this.symbolId = symbolId;
		}

		/// <summary>
		/// Gets the table name.
		/// </summary>
		/// <value>The table name.</value>
		public string TableName
		{
			get { return this.tableName; }
		}

		/// <summary>
		/// Gets the symbolic name.
		/// </summary>
		/// <value>Symbolic name.</value>
		public string SymbolicName
		{
			get { return String.Concat(this.tableName, ":", this.symbolId); }
		}

		/// <summary>
		/// Returns a string representation of a Reference.
		/// </summary>
		/// <returns>Returns a string representation of a Reference.</returns>
		public override String ToString()
		{
			throw new NotImplementedException("ToString() should not be called on Reference class");
		}

		/// <summary>
		/// Persist the reference to an XmlWriter.
		/// </summary>
		/// <param name="writer">XmlWriter which reference will be persisted to.</param>
		public void Persist(XmlWriter writer)
		{
			if (null == writer)
			{
				throw new ArgumentNullException("writer");
			}

			writer.WriteStartElement("reference");
			writer.WriteAttributeString("table", this.tableName);
			writer.WriteAttributeString("symbol", this.symbolId);
			writer.WriteEndElement();
		}

		/// <summary>
		/// Test if this reference is equal to another object.
		/// </summary>
		/// <param name="obj">Object to test for equality.</param>
		/// <returns>Returns true if the object is equal to this one, false otherwise.</returns>
		public override bool Equals(object obj)
		{
			Reference otherReference = obj as Reference;

			if (null == otherReference)
			{
				return false;
			}

			return (this.tableName == otherReference.tableName && this.symbolId == otherReference.symbolId);
		}

		/// <summary>
		/// Compares the current instance with another object of the same type.
		/// </summary>
		/// <param name="obj">Other reference to compare this one to.</param>
		/// <returns>Returns less than 0 for less than, 0 for equals, and greater than 0 for greater.</returns>
		public int CompareTo(object obj)
		{
			Reference otherReference = obj as Reference;

			if (null == otherReference)
			{
				throw new ArgumentException("Cannot compare a Reference object against a different object.");
			}

			return this.SymbolicName.CompareTo(otherReference.SymbolicName);
		}

		/// <summary>
		/// Serves as a hash function for a particular type, suitable for use in hashing algorithms and data structures like a hash table.
		/// </summary>
		/// <returns>A hash code for the reference.</returns>
		public override int GetHashCode()
		{
			return this.SymbolicName.GetHashCode();
		}
	}
}
