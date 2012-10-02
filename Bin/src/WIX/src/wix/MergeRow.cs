//-------------------------------------------------------------------------------------------------
// <copyright file="MergeRow.cs" company="Microsoft">
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
// Specialization of a row for tracking merge statements.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;
	using System.Text;
	using System.Xml;

	/// <summary>
	/// Specifies if files in merge module should be compressed
	/// </summary>
	public enum FileCompressionValue : int
	{
		/// <summary>User did not specify compression for files in merge module.</summary>
		NotSpecified = -1,
		/// <summary>User specified to not compress files from a merge module.</summary>
		No = 0,
		/// <summary>User specified to compress files from a merge module.</summary>
		Yes = 1
	}

	/// <summary>
	/// Specialization of a row for tracking merge statements.
	/// </summary>
	public class MergeRow : Row
	{
		/// <summary>
		/// Creates a Merge row that does not belong to a table.
		/// </summary>
		/// <param name="sourceLineNumbers">Original source lines for this row.</param>
		/// <param name="tableDef">TableDefinition this Merge row belongs to and should get its column definitions from.</param>
		public MergeRow(SourceLineNumberCollection sourceLineNumbers, TableDefinition tableDef) :
			base(sourceLineNumbers, tableDef)
		{
		}

		/// <summary>Creates a Merge row that belongs to a table.</summary>
		/// <param name="sourceLineNumbers">Original source lines for this row.</param>
		/// <param name="table">Table this Merge row belongs to and should get its column definitions from.</param>
		public MergeRow(SourceLineNumberCollection sourceLineNumbers, Table table) :
			base(sourceLineNumbers, table)
		{
		}

		/// <summary>
		/// Gets and sets the id for a merge row.
		/// </summary>
		/// <value>Id for the row.</value>
		public string Id
		{
			get { return (string)this.Fields[0].Data; }
			set { this.Fields[0].Data = value; }
		}

		/// <summary>
		/// Gets and sets the language for a merge row.
		/// </summary>
		/// <value>Language for the row.</value>
		public int Language
		{
			get { return Convert.ToInt32(this.Fields[1].Data); }
			set { this.Fields[1].Data = value; }
		}

		/// <summary>
		/// Gets and sets the directory for a merge row.
		/// </summary>
		/// <value>Direcotory for the row.</value>
		public string Directory
		{
			get { return (string)this.Fields[2].Data; }
			set { this.Fields[2].Data = value; }
		}

		/// <summary>
		/// Gets and sets the path to the merge module for a merge row.
		/// </summary>
		/// <value>Source path for the row.</value>
		public string SourceFile
		{
			get { return (string)this.Fields[3].Data; }
			set { this.Fields[3].Data = value; }
		}

		/// <summary>
		/// Gets and sets the disk id the merge module should be placed on for a merge row.
		/// </summary>
		/// <value>Disk identifier for row.</value>
		public int DiskId
		{
			get { return Convert.ToInt32(this.Fields[4].Data); }
			set { this.Fields[4].Data = value; }
		}

		/// <summary>
		/// Gets and sets the compression value for a merge row.
		/// </summary>
		/// <value>Compression for a merge row.</value>
		public FileCompressionValue FileCompression
		{
			get
			{
				if (null == this.Fields[5].Data)
				{
					return FileCompressionValue.NotSpecified;
				}
				else if (Convert.ToBoolean(this.Fields[5].Data))
				{
					return FileCompressionValue.Yes;
				}
				else
				{
					return FileCompressionValue.No;
				}
			}
			set { this.Fields[5].Data = value; }
		}

		/// <summary>
		/// Gets and sets the configuration data for a merge row.
		/// </summary>
		/// <value>Comma delimited string of "name=value" pairs.</value>
		public string ConfigurationData
		{
			get { return (string)this.Fields[7].Data; }
			set { this.Fields[7].Data = value; }
		}

		/// <summary>
		/// Gets and sets if the merge module has files.
		/// </summary>
		/// <value>Flag specifies if merge module contains files.</value>
		/// <remarks>this property is only used by the linker when it figures out if a merge module has files or not</remarks>
		internal bool HasFiles
		{
			get { return Convert.ToBoolean(this.Fields[6].Data); }
			set { this.Fields[6].Data = value; }
		}
	}
}
