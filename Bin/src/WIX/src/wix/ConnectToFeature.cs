//-------------------------------------------------------------------------------------------------
// <copyright file="ConnectToFeature.cs" company="Microsoft">
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
// Object that connects things (components/modules) to features.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;
	using System.Diagnostics;
	using System.Collections;
	using System.Collections.Specialized;
	using System.Globalization;
	using System.Xml;

	/// <summary>
	/// Object that connects things (components/modules) to features.
	/// </summary>
	public class ConnectToFeature
	{
		private Section section;
		private string childId;

		private string primaryFeature;
		private bool explicitPrimaryFeature;
		private StringCollection connectFeatures;

		/// <summary>
		/// Creates a new connect to feature.
		/// </summary>
		/// <param name="section">Section this connect belongs to.</param>
		/// <param name="childId">Id of the child.</param>
		public ConnectToFeature(Section section, string childId) :
			this(section, childId, null, false)
		{
		}

		/// <summary>
		/// Creates a new connect to feature.
		/// </summary>
		/// <param name="section">Section this connect belongs to.</param>
		/// <param name="childId">Id of the child.</param>
		/// <param name="primaryFeature">Sets the primary feature for the connection.</param>
		/// <param name="explicitPrimaryFeature">Sets if this is explicit primary.</param>
		public ConnectToFeature(Section section, string childId, string primaryFeature, bool explicitPrimaryFeature)
		{
			this.section = section;
			this.childId = childId;

			this.primaryFeature = primaryFeature;
			this.explicitPrimaryFeature = explicitPrimaryFeature;

			this.connectFeatures = new StringCollection();
		}

		/// <summary>
		/// Gets the section.
		/// </summary>
		/// <value>Section.</value>
		public Section Section
		{
			get { return this.section; }
		}

		/// <summary>
		/// Gets the id of the child.
		/// </summary>
		/// <value>Child identifier.</value>
		public string ChildId
		{
			get { return this.childId; }
		}

		/// <summary>
		/// Gets or sets if the primary feature was set explicitly.
		/// </summary>
		/// <value>Primary feature was set explicitly.</value>
		public bool IsExplicitPrimaryFeature
		{
			get { return this.explicitPrimaryFeature; }
			set { this.explicitPrimaryFeature = value; }
		}

		/// <summary>
		/// Gets or sets the primary feature.
		/// </summary>
		/// <value>Primary feature.</value>
		public string PrimaryFeature
		{
			get { return this.primaryFeature; }
			set { this.primaryFeature = value; }
		}

		/// <summary>
		/// Gets the features connected to.
		/// </summary>
		/// <value>Features connected to.</value>
		public StringCollection ConnectFeatures
		{
			get { return this.connectFeatures; }
		}

		/// <summary>
		/// Processes an XmlReader and builds up the feature connection object.
		/// </summary>
		/// <param name="reader">Reader to get data from.</param>
		/// <returns>ConnectToFeature object.</returns>
		internal static ConnectToFeature Parse(XmlReader reader)
		{
			Debug.Assert("connectToFeature" == reader.LocalName);

			string childId = null;
			string primaryFeature = null;
			bool explicitPrimaryFeature = false;
			bool empty = reader.IsEmptyElement;

			while (reader.MoveToNextAttribute())
			{
				switch (reader.LocalName)
				{
					case "childId":
						childId = reader.Value;
						break;
					case "primaryFeature":
						primaryFeature = reader.Value;
						break;
					case "explicitPrimaryFeature":
						explicitPrimaryFeature = Common.IsYes(reader.Value, null, "connectToFeature", reader.Name, childId);
						break;
					default:
						throw new WixParseException(String.Format("The connectToFeature element contains an unexpected attribute {0}.", reader.Name));
				}
			}
			if (null == childId)
			{
				throw new WixParseException(String.Format("The connectToFeature/@childId attribute was not found; it is required."));
			}
			if (null == primaryFeature)
			{
				throw new WixParseException(String.Format("The connectToFeature/@primaryFeature attribute was not found; it is required."));
			}

			ConnectToFeature ctf = new ConnectToFeature(null, childId, primaryFeature, explicitPrimaryFeature);
			if (!empty)
			{
				bool done = false;

				// loop through all the fields in a row
				while (!done && reader.Read())
				{
					switch (reader.NodeType)
					{
						case XmlNodeType.Element:
							switch (reader.LocalName)
							{
								case "feature":
									ctf.connectFeatures.Add(reader.ReadString());
									break;
								default:
									throw new WixParseException(String.Format("The connectToFeature element contains an unexpected child element {0}.", reader.Name));
							}
							break;
						case XmlNodeType.EndElement:
							done = true;
							break;
					}
				}

				if (!done)
				{
					throw new WixParseException("Missing end element while processing the connectToFeature element.");
				}
			}

			return ctf;
		}

		/// <summary>
		/// Persists a feature connection in an XML format.
		/// </summary>
		/// <param name="writer">XmlWriter where the ConnectToFeature should persist itself as XML.</param>
		internal void Persist(XmlWriter writer)
		{
			writer.WriteStartElement("connectToFeature");
			writer.WriteAttributeString("childId", this.childId);
			writer.WriteAttributeString("primaryFeature", this.primaryFeature);
			writer.WriteAttributeString("explicitPrimaryFeature", this.explicitPrimaryFeature ? "yes" : "no");
			foreach (string feature in this.connectFeatures)
			{
				writer.WriteElementString("feature", feature);
			}
			writer.WriteEndElement();
		}
	}
}
