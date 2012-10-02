//-------------------------------------------------------------------------------------------------
// <copyright file="FileMediaInformation.cs" company="Microsoft">
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
// Information about files and the media they belong on.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;
	using System.Diagnostics;
	using System.Globalization;
	using System.Text;
	using System.Xml;

	/// <summary>
	/// Information about files and the media they belong on.
	/// </summary>
	public class FileMediaInformation : IComparable
	{
		private string fileId;
		private string directoryId;
		private int mediaId;
		private string source;
		private string modulePath;
		private int rowNumber;
		private bool containedInModule;
		private int sequence;
		private FileCompressionValue fileCompression;
		private int patchGroup;

		/// <summary>
		/// Creates a new file media information from a file row.
		/// </summary>
		/// <param name="fileRow">File row.</param>
		public FileMediaInformation(FileRow fileRow)
		{
			if (null == fileRow)
			{
				throw new ArgumentNullException("fileRow");
			}

			this.containedInModule = false;

			this.fileId = fileRow.File;
			this.directoryId = fileRow.Directory;
			this.mediaId = fileRow.DiskId;
			this.source = fileRow.Source;
			this.modulePath = null;
			this.rowNumber = fileRow.Number;
			this.patchGroup = fileRow.PatchGroup;

			if (0 != (fileRow.Attributes & 0x002000))
			{
				this.fileCompression = FileCompressionValue.No;
			}
			else if (0 != (fileRow.Attributes & 0x004000))
			{
				this.fileCompression = FileCompressionValue.Yes;
			}
			else
			{
				this.fileCompression = FileCompressionValue.NotSpecified;
			}

			this.sequence = -1;
		}

		/// <summary>
		/// Creates a file media information for a module.
		/// </summary>
		/// <param name="fileId">Identifier to file this media information pertains to.</param>
		/// <param name="directoryId">Identifier to directory file belongs to.</param>
		/// <param name="mediaId">Identifier to media file belongs to.</param>
		/// <param name="srcPath">Path file on disk.</param>
		/// <param name="rowNumber">Number to row.</param>
		/// <param name="fileCompression">Compression for file.</param>
		/// <param name="modulePath">The module path where this file came from.</param>
		/// <param name="patchGroup">The patch group for a patch-added file.</param>
		public FileMediaInformation(string fileId, string directoryId, int mediaId, string srcPath, int rowNumber, FileCompressionValue fileCompression, string modulePath, int patchGroup)
		{
			this.containedInModule = true;

			this.fileId = fileId;
			this.directoryId = directoryId;
			this.mediaId = mediaId;
			this.source = srcPath;
			this.modulePath = modulePath;
			this.rowNumber = rowNumber;
			this.patchGroup = patchGroup;

			this.sequence = -1;

			this.fileCompression = fileCompression;
		}

		/// <summary>
		/// Gets the file identifier for this media information.
		/// </summary>
		/// <value>Identifier of file.</value>
		public string File
		{
			get { return this.fileId; }
		}

		/// <summary>
		/// Gets the media identifier for this media information.
		/// </summary>
		/// <value>Media identifier for this file.</value>
		public int Media
		{
			get { return this.mediaId; }
		}

		/// <summary>
		/// Gets if this file media information came from a module.
		/// </summary>
		/// <value>true if file came from module.</value>
		public bool IsInModule
		{
			get { return this.containedInModule; }
		}

		/// <summary>
		/// Gets the file id.
		/// </summary>
		/// <value>The file id.</value>
		public string FileId
		{
			get { return this.fileId; }
		}

		/// <summary>
		/// Gets the source path for the file media information.
		/// </summary>
		/// <value>The source path.</value>
		public string Source
		{
			get { return this.source; }
			set { this.source = value; }
		}

		/// <summary>
		/// Gets the module path where this file came from
		/// </summary>
		/// <value>Module path.</value>
		public string ModulePath
		{
			get { return this.modulePath; }
			set { this.modulePath = value; }
		}

		/// <summary>
		/// Gets and sets the sequence of the file.
		/// </summary>
		/// <value>Sequence of the file media information.</value>
		public int Sequence
		{
			get { return this.sequence; }
			set { this.sequence = value; }
		}

		/// <summary>
		/// Type of file compression to use in th sfile media information.
		/// </summary>
		/// <value>Compression of the file.</value>
		public FileCompressionValue FileCompression
		{
			get { return this.fileCompression; }
		}

		/// <summary>
		/// Gets the patch group of a patch-added file.
		/// </summary>
		/// <value>The patch group of a patch-added file.</value>
		public int PatchGroup
		{
			get { return this.patchGroup; }
		}

		/// <summary>
		/// Modularizes the file media information.
		/// </summary>
		/// <param name="moduleGuid">Guid of module.</param>
		public void Modularize(string moduleGuid)
		{
			this.fileId = String.Concat(this.fileId, ".", moduleGuid);
		}

		/// <summary>
		/// Compares the object to another object.
		/// </summary>
		/// <param name="obj">Object to compare against.</param>
		/// <returns>compared value.</returns>
		public int CompareTo(object obj)
		{
			FileMediaInformation fmi = obj as FileMediaInformation;
			if (null == fmi)
			{
				throw new ArgumentException("object is not a FileMediaInformation");
			}

			int compared = this.mediaId - fmi.mediaId;
			if (0 == compared)
			{
				compared = this.patchGroup - fmi.patchGroup;

				if (0 == compared)
				{
					compared = this.rowNumber - fmi.rowNumber;
				}
			}

			return compared;
		}

		/// <summary>
		/// Processes an XmlReader and builds up the file media information object.
		/// </summary>
		/// <param name="reader">Reader to get data from.</param>
		/// <returns>File media information object.</returns>
		internal static FileMediaInformation Parse(XmlReader reader)
		{
			Debug.Assert("fileMediaInformation" == reader.LocalName);
			string fileId = null;
			string directoryId = null;
			int mediaId = -1;
			string srcPath = null;
			int rowNumber = -1;
			bool containedInModule = false;
			int patchGroup = -1;
			int sequence = -1;
			FileCompressionValue fileCompression = FileCompressionValue.NotSpecified;
			bool empty = reader.IsEmptyElement;

			while (reader.MoveToNextAttribute())
			{
				switch (reader.LocalName)
				{
					case "fileId":
						fileId = reader.Value;
						break;
					case "directoryId":
						directoryId = reader.Value;
						break;
					case "mediaId":
						mediaId = Convert.ToInt32(reader.Value, CultureInfo.InvariantCulture.NumberFormat);
						break;
					case "srcPath":
						srcPath = reader.Value;
						break;
					case "rowNumber":
						rowNumber = Convert.ToInt32(reader.Value, CultureInfo.InvariantCulture.NumberFormat);
						break;
					case "inModule":
						containedInModule = Common.IsYes(reader.Value, null, "fileMediaInformation", "inModule", fileId);
						break;
					case "patchGroup":
						patchGroup = Convert.ToInt32(reader.Value, CultureInfo.InvariantCulture.NumberFormat);
						break;
					case "sequence":
						sequence = Convert.ToInt32(reader.Value, CultureInfo.InvariantCulture.NumberFormat);
						break;
					case "fileCompression":
						switch (reader.Value)
						{
							case "NotSpecified":
								fileCompression = FileCompressionValue.NotSpecified;
								break;
							case "No":
								fileCompression = FileCompressionValue.No;
								break;
							case "Yes":
								fileCompression = FileCompressionValue.Yes;
								break;
							default:
								throw new WixParseException(String.Format("The fileMediaInformation/@fileCompression attribute contains an unexpected value '{0}'.", reader.Value));
						}
						break;
					default:
						throw new WixParseException(String.Format("The fileMediaInformation element contains an unexpected attribute {0}.", reader.Name));
				}
			}
			if (null == fileId)
			{
				throw new WixParseException("The fileMediaInformation/@fileId attribute was not found; it is required.");
			}
			if (null == directoryId)
			{
				throw new WixParseException("The fileMediaInformation/@directoryId attribute was not found; it is required.");
			}

			if (!empty)
			{
				throw new WixParseException("The fileMediaInformation element contains text or other elements; it cannot.");
			}

			FileMediaInformation fmi = new FileMediaInformation(fileId, directoryId, mediaId, srcPath, rowNumber, fileCompression, null, patchGroup);
			fmi.containedInModule = containedInModule;
			fmi.sequence = sequence;
			return fmi;
		}

		/// <summary>
		/// Persists a import stream in an XML format.
		/// </summary>
		/// <param name="writer">XmlWriter where the ImportStream should persist itself as XML.</param>
		internal void Persist(XmlWriter writer)
		{
			writer.WriteStartElement("fileMediaInformation");
			writer.WriteAttributeString("fileId", this.fileId);
			writer.WriteAttributeString("directoryId", this.directoryId);
			writer.WriteAttributeString("mediaId", this.mediaId.ToString());
			writer.WriteAttributeString("srcPath", this.source);
			writer.WriteAttributeString("rowNumber", this.rowNumber.ToString());
			writer.WriteAttributeString("inModule", this.containedInModule ? "yes" : "no");
			writer.WriteAttributeString("patchGroup", this.patchGroup.ToString());
			writer.WriteAttributeString("sequence", this.sequence.ToString());
			writer.WriteAttributeString("fileCompression", this.fileCompression.ToString());
			writer.WriteEndElement();
		}
	}
}
