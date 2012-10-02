//-------------------------------------------------------------------------------------------------
// <copyright file="MediaRow.cs" company="Microsoft">
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
// Specialization of a row for the media table.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;
	using System.Text;
	using System.Xml;

	/// <summary>
	/// MediaRow containing data for a table.
	/// </summary>
	public class MediaRow : Row
	{
		/// <summary>
		/// Creates a Media row that does not belong to a table.
		/// </summary>
		/// <param name="sourceLineNumbers">Original source lines for this row.</param>
		/// <param name="tableDef">TableDefinition this Media row belongs to and should get its column definitions from.</param>
		public MediaRow(SourceLineNumberCollection sourceLineNumbers, TableDefinition tableDef) :
			base(sourceLineNumbers, tableDef)
		{
		}

		/// <summary>
		/// Creates a Media row that belongs to a table.
		/// </summary>
		/// <param name="sourceLineNumbers">Original source lines for this row.</param>
		/// <param name="table">Table this Media row belongs to and should get its column definitions from.</param>
		public MediaRow(SourceLineNumberCollection sourceLineNumbers, Table table) :
			base(sourceLineNumbers, table)
		{
		}

		/// <summary>
		/// Gets or sets the disk id for this media row.
		/// </summary>
		/// <value>Disk id.</value>
		public int DiskId
		{
			get { return Convert.ToInt32(this.Fields[0].Data); }
			set { this.Fields[0].Data = value; }
		}

		/// <summary>
		/// Gets or sets the last sequence number for this media row.
		/// </summary>
		/// <value>Last sequence number.</value>
		public int LastSequence
		{
			get { return Convert.ToInt32(this.Fields[1].Data); }
			set { this.Fields[1].Data = value; }
		}

		/// <summary>
		/// Gets or sets the disk prompt for this media row.
		/// </summary>
		/// <value>Disk prompt.</value>
		public string DiskPrompt
		{
			get { return (string)this.Fields[2].Data; }
			set { this.Fields[2].Data = value; }
		}

		/// <summary>
		/// Gets or sets the cabinet name for this media row.
		/// </summary>
		/// <value>Cabinet name.</value>
		public string Cabinet
		{
			get { return (string)this.Fields[3].Data; }
			set { this.Fields[3].Data = value; }
		}

		/// <summary>
		/// Gets or sets the volume label for this media row.
		/// </summary>
		/// <value>Volume label.</value>
		public string VolumeLabel
		{
			get { return (string)this.Fields[4].Data; }
			set { this.Fields[4].Data = value; }
		}

		/// <summary>
		/// Gets or sets the source for this media row.
		/// </summary>
		/// <value>Source.</value>
		public string Source
		{
			get { return (string)this.Fields[5].Data; }
			set { this.Fields[5].Data = value; }
		}

		/// <summary>
		/// Gets or sets the compression level for this media row.
		/// </summary>
		/// <value>Compression level.</value>
		public Cab.CompressionLevel CompressionLevel
		{
			get
			{
				Cab.CompressionLevel level = Cab.CompressionLevel.Mszip;
				switch ((string)this.Fields[6].Data)
				{
					case "low":
						level = Cab.CompressionLevel.Low;
						break;
					case "medium":
						level = Cab.CompressionLevel.Medium;
						break;
					case "high":
						level = Cab.CompressionLevel.High;
						break;
					case "none":
						level = Cab.CompressionLevel.None;
						break;
					case "mszip":
						level = Cab.CompressionLevel.Mszip;
						break;
				}

				return level;
			}
			set
			{
				switch (value)
				{
					case Cab.CompressionLevel.None:
						this.Fields[6].Data = "none";
						break;
					case Cab.CompressionLevel.Low:
						this.Fields[6].Data = "low";
						break;
					case Cab.CompressionLevel.Medium:
						this.Fields[6].Data = "medium";
						break;
					case Cab.CompressionLevel.High:
						this.Fields[6].Data = "high";
						break;
					case Cab.CompressionLevel.Mszip:
						this.Fields[6].Data = "mszip";
						break;
					default:
						throw new ArgumentException(String.Format("Unknown compression level type: {0}", value));
				}
			}
		}

		/// <summary>
		/// Gets or sets the layout location for this media row.
		/// </summary>
		/// <value>Layout location to the root of the media.</value>
		public string Layout
		{
			get { return (string)this.Fields[7].Data; }
			set { this.Fields[7].Data = value; }
		}
	}
}
