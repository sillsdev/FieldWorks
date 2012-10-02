//-------------------------------------------------------------------------------------------------
// <copyright file="IgnoreModularization.cs" company="Microsoft">
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
//    Identifier to ignore when modularizing.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;
	using System.Xml;

	/// <summary>
	/// Identifier to ignore when modularizing.
	/// </summary>
	public class IgnoreModularization
	{
		private string name;
		private string type;

		/// <summary>
		/// Creates a new identifier to ignore.
		/// </summary>
		/// <param name="identiferName">Name of identifier to ignore modularization.</param>
		/// <param name="identifierType">Type of identifier.</param>
		public IgnoreModularization(string identiferName, string identifierType)
		{
			if (null == identiferName || 0 == identiferName.Length)
			{
				throw new ArgumentNullException(identiferName, "IgnoreModularization name cannot be null or empty");
			}
			if (null == identifierType || 0 == identifierType.Length)
			{
				throw new ArgumentNullException(identifierType, "IgnoreModularization type cannot be null or empty");
			}

			this.name = identiferName;
			this.type = identifierType;
		}

		/// <summary>
		/// Gets the name of the identifier to ignore.
		/// </summary>
		/// <value>Name of ignored identifier.</value>
		public string Name
		{
			get { return this.name; }
		}

		/// <summary>
		/// Gets the type of the identifier to ignore.
		/// </summary>
		/// <value>Type of ignored identifier.</value>
		public string Type
		{
			get { return this.type; }
		}

		/// <summary>
		/// Serializes the ignore identifier into Xml.
		/// </summary>
		/// <param name="writer">Writer to serialize into.</param>
		public void Persist(XmlWriter writer)
		{
			if (null == writer)
			{
				throw new ArgumentNullException("writer");
			}

			writer.WriteStartElement("ignoreModularization");
			writer.WriteAttributeString("name", this.name);
			writer.WriteAttributeString("type", this.type);
			writer.WriteEndElement();
		}
	}
}
