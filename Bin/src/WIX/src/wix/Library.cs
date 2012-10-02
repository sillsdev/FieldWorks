//-------------------------------------------------------------------------------------------------
// <copyright file="Library.cs" company="Microsoft">
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
// Object that represents a library file.
// </summary>
//-------------------------------------------------------------------------------------------------
namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;
	using System.Xml;

	/// <summary>
	/// Object that represents a library file.
	/// </summary>
	public class Library
	{
		private string path;
		private IntermediateCollection intermediates;

		/// <summary>
		/// Instantiate a new Library.
		/// </summary>
		public Library()
		{
			this.intermediates = new IntermediateCollection();
		}

		/// <summary>
		/// Gets intermediates in this library.
		/// </summary>
		/// <value>Intermediates in this library.</value>
		public IntermediateCollection Intermediates
		{
			get { return this.intermediates; }
		}

		/// <summary>
		/// Get or set the path to this library on disk.
		/// </summary>
		/// <remarks>The Path may be null if this intermediate was never persisted to disk.</remarks>
		/// <value>Path to this library on disk.</value>
		public string Path
		{
			get { return this.path; }
		}

		/// <summary>
		/// Loads a library from a path on disk.
		/// </summary>
		/// <param name="path">Path to library file saved on disk.</param>
		/// <param name="tableDefinitions">Collection containing TableDefinitions to use when reconstituting the intermediates.</param>
		/// <param name="suppressVersionCheck">Suppresses wix.dll version mismatch check.</param>
		/// <returns>Returns the loaded library.</returns>
		/// <remarks>This method will set the Path and SourcePath properties to the appropriate values on successful load.</remarks>
		public static Library Load(string path, TableDefinitionCollection tableDefinitions, bool suppressVersionCheck)
		{
			Library library = new Library();
			library.path = path;

			XmlTextReader reader = null;
			try
			{
				reader = new XmlTextReader(path);
				ParseLibrary(library, reader, tableDefinitions, suppressVersionCheck);
			}
			finally
			{
				if (null != reader)
				{
					reader.Close();
				}
			}

			return library;
		}

		/// <summary>
		/// Loads a library from a XmlReader in memory.
		/// </summary>
		/// <param name="reader">XmlReader with library persisted as Xml.</param>
		/// <param name="tableDefinitions">Collection containing TableDefinitions to use when reconstituting the intermediates.</param>
		/// <param name="suppressVersionCheck">Suppresses wix.dll version mismatch check.</param>
		/// <returns>Returns the loaded library.</returns>
		/// <remarks>This method will set the SourcePath property to the appropriate values on successful load, but will not update the Path property.</remarks>
		public static Library Load(XmlReader reader, TableDefinitionCollection tableDefinitions, bool suppressVersionCheck)
		{
			Library library = new Library();

			ParseLibrary(library, reader, tableDefinitions, suppressVersionCheck);
			return library;
		}

		/// <summary>
		/// Saves a library to a path on disk.
		/// </summary>
		/// <param name="path">Path to save library file to on disk.</param>
		/// <remarks>This method will set the Path property to the passed in value before saving.</remarks>
		public void Save(string path)
		{
			this.path = path;
			this.Save();
		}

		/// <summary>
		/// Saves a library to a path on disk.
		/// </summary>
		/// <remarks>This method will save the library to the file specified in the Path property.</remarks>
		public void Save()
		{
			XmlWriter writer = null;
			try
			{
				writer = new XmlTextWriter(this.Path, System.Text.Encoding.UTF8);
				this.Persist(writer);
			}
			finally
			{
				if (null != writer)
				{
					writer.Close();
				}
			}
		}

		/// <summary>
		/// Persists a library in an XML format.
		/// </summary>
		/// <param name="writer">XmlWriter where the library should persist itself as XML.</param>
		public void Persist(XmlWriter writer)
		{
			if (null == writer)
			{
				throw new ArgumentNullException("writer");
			}

			writer.WriteStartDocument();
			writer.WriteStartElement("wixLibrary");
			writer.WriteAttributeString("xmlns", "http://schemas.microsoft.com/wix/2003/11/libraries");

			Version currentVersion = Common.LibraryFormatVersion;
			writer.WriteAttributeString("version", currentVersion.ToString());

			foreach (Intermediate intermediate in this.intermediates)
			{
				// save the intermediate
				intermediate.Persist(writer, false);
			}

			writer.WriteEndElement();
			writer.WriteEndDocument();
		}

		/// <summary>
		/// Parse the root library element.
		/// </summary>
		/// <param name="library">Library to read from disk.</param>
		/// <param name="reader">XmlReader with library persisted as Xml.</param>
		/// <param name="tableDefinitions">Collection containing TableDefinitions to use when reconstituting the intermediates.</param>
		/// <param name="suppressVersionCheck">Suppresses check for wix.dll version mismatch.</param>
		private static void ParseLibrary(Library library, XmlReader reader, TableDefinitionCollection tableDefinitions, bool suppressVersionCheck)
		{
			// read the document root
			reader.MoveToContent();
			if ("wixLibrary" != reader.LocalName)
			{
				throw new WixNotLibraryException(SourceLineNumberCollection.FromFileName(library.Path), String.Format("Invalid root element: '{0}', expected 'wixLibrary'", reader.LocalName));
			}

			Version objVersion = null;

			while (reader.MoveToNextAttribute())
			{
				switch (reader.LocalName)
				{
					case "version":
						objVersion = new Version(reader.Value);
						break;
				}
			}

			if (null != objVersion && !suppressVersionCheck)
			{
				Version currentVersion = Common.LibraryFormatVersion;
				if (0 != currentVersion.CompareTo(objVersion))
				{
					throw new WixVersionMismatchException(currentVersion, objVersion, "Library", library.Path);
				}
			}

			// loop through the rest of the xml building up the SectionCollection
			while (reader.Read())
			{
				if (0 == reader.Depth)
				{
					break;
				}
				else if (1 != reader.Depth)
				{
					// throw exception since we should only be processing tables
					throw new WixInvalidIntermediateException(SourceLineNumberCollection.FromFileName(library.Path), "Unexpected depth while processing element: 'wixLibrary'");
				}

				if (XmlNodeType.Element == reader.NodeType && "wixObject" == reader.LocalName)
				{
					Intermediate intermediate = Intermediate.Load(reader, library.Path, tableDefinitions, suppressVersionCheck);

					library.intermediates.Add(intermediate);
				}
			}
		}
	}
}
