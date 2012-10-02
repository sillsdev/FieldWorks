//-------------------------------------------------------------------------------------------------
// <copyright file="ImportStream.cs" company="Microsoft">
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
// Stream to be imported into MSI/MSM after it is created.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;
	using System.Diagnostics;
	using System.Xml;

	/// <summary>
	/// Stream to be imported into MSI/MSM after it is created.
	/// </summary>
	public enum ImportStreamType
	{
		/// <summary>Unknown import type.</summary>
		Unknown,
		/// <summary>Binary table import type.</summary>
		Binary,
		/// <summary>Random stream import type.</summary>
		Cabinet,
		/// <summary>Icon table import type.</summary>
		Icon,
		/// <summary>Digital certificate table import type.</summary>
		DigitalCertificate
	}

	/// <summary>
	/// Stream to be imported into MSI/MSM after it is created.
	/// </summary>
	public class ImportStream
	{
		private ImportStreamType type;
		private string streamName;
		private string path;

		/// <summary>
		/// Creates a new import stream.
		/// </summary>
		/// <param name="type">Type of import stream.</param>
		/// <param name="streamName">Name of stream in MSI/MSM.</param>
		/// <param name="path">Path to file to import.</param>
		public ImportStream(ImportStreamType type, string streamName, string path)
		{
			this.type = type;
			this.streamName = streamName;
			this.path = path;
		}

		/// <summary>
		/// Gets the uniquen name of the stream which is "Type:StreamName".
		/// </summary>
		/// <value>Unique name of stream.</value>
		public string Name
		{
			get { return String.Concat(this.type, ":", this.streamName); }
		}

		/// <summary>
		/// Gets the type of the stream.
		/// </summary>
		/// <value>Type of stream.</value>
		public ImportStreamType Type
		{
			get { return this.type; }
		}

		/// <summary>
		/// Gets the name of the stream in the MSI/MSM.
		/// </summary>
		/// <value>Name of actual stream in MSI/MSM.</value>
		public string StreamName
		{
			get { return this.streamName; }
		}

		/// <summary>
		/// Gets the path to the stream to import.
		/// </summary>
		/// <value>Path to stream.</value>
		public string Path
		{
			get { return this.path; }
			set { this.path = value; }
		}

		/// <summary>
		/// Processes an XmlReader and builds up the import stream object.
		/// </summary>
		/// <param name="reader">Reader to get data from.</param>
		/// <returns>Import stream object.</returns>
		internal static ImportStream Parse(XmlReader reader)
		{
			Debug.Assert("importStream" == reader.LocalName);

			string name = null;
			string streamPath = null;
			ImportStreamType type = ImportStreamType.Unknown;
			bool empty = reader.IsEmptyElement;

			while (reader.MoveToNextAttribute())
			{
				switch (reader.LocalName)
				{
					case "name":
						name = reader.Value;
						break;
					case "path":
						streamPath = reader.Value;
						break;
					case "type":
						switch (reader.Value)
						{
							case "Binary":
								type = ImportStreamType.Binary;
								break;
							case "Cabinet":
								type = ImportStreamType.Cabinet;
								break;
							case "Icon":
								type = ImportStreamType.Icon;
								break;
							case "DigitalCertificate":
								type = ImportStreamType.DigitalCertificate;
								break;
							default:
								throw new WixParseException(String.Format("The importStream/@type attribute contains an unexpected value '{0}'.", reader.Value));
						}
						break;
					default:
						throw new WixParseException(String.Format("The importStream element contains an unexpected child element {0}.", reader.Name));
				}
			}
			if (null == name)
			{
				throw new WixParseException("The importStream/@name attribute was not found; it is required.");
			}
			if (null == streamPath)
			{
				throw new WixParseException("The importStream/@path attribute was not found; it is required.");
			}
			if (ImportStreamType.Unknown == type)
			{
				throw new WixParseException("The importStream/@type attribute was not found; it is required.");
			}

			// ensure there are no child elements
			if (!empty)
			{
				throw new WixParseException("The importStream element contains text or other elements; it cannot.");
			}

			return new ImportStream(type, name, streamPath);
		}

		/// <summary>
		/// Persists a import stream in an XML format.
		/// </summary>
		/// <param name="writer">XmlWriter where the ImportStream should persist itself as XML.</param>
		internal void Persist(XmlWriter writer)
		{
			writer.WriteStartElement("importStream");
			writer.WriteAttributeString("type", this.type.ToString());
			writer.WriteAttributeString("name", this.streamName);
			writer.WriteAttributeString("path", this.path);
			writer.WriteEndElement();
		}
	}
}
