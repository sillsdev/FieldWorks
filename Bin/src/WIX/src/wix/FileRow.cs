//-------------------------------------------------------------------------------------------------
// <copyright file="FileRow.cs" company="Microsoft">
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
// Specialization of a row for the file table.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;
	using System.Globalization;
	using System.Text;
	using System.Xml;

	/// <summary>
	/// Every file row has an assembly type.
	/// </summary>
	public enum FileAssemblyType
	{
		/// <summary>File is not an assembly.</summary>
		NotAnAssembly = -1,
		/// <summary>File is a Common Language Runtime Assembly.</summary>
		DotNetAssembly = 0,
		/// <summary>File is Win32 SxS assembly.</summary>
		Win32Assembly  = 1,
	}

	/// <summary>
	/// Specialization of a row for the file table.
	/// </summary>
	public class FileRow : Row
	{
		/// <summary>
		/// Creates a File row that does not belong to a table.
		/// </summary>
		/// <param name="sourceLineNumbers">Original source lines for this row.</param>
		/// <param name="tableDef">TableDefinition this Media row belongs to and should get its column definitions from.</param>
		public FileRow(SourceLineNumberCollection sourceLineNumbers, TableDefinition tableDef) :
			base(sourceLineNumbers, tableDef)
		{
		}

		/// <summary>
		/// Creates a File row that belongs to a table.
		/// </summary>
		/// <param name="sourceLineNumbers">Original source lines for this row.</param>
		/// <param name="table">Table this File row belongs to and should get its column definitions from.</param>
		public FileRow(SourceLineNumberCollection sourceLineNumbers, Table table) :
			base(sourceLineNumbers, table)
		{
		}

		/// <summary>Creates a File row that does not belong to a table.</summary>
		/// <param name="tableDef">TableDefinition this File row should get its column definitions from.</param>
		public FileRow(TableDefinition tableDef) :
			base(tableDef)
		{
		}

		/// <summary>
		/// Gets or sets the primary key of the file row.
		/// </summary>
		/// <value>Primary key of the file row.</value>
		public string File
		{
			get { return (string)this.Fields[0].Data; }
			set { this.Fields[0].Data = value; }
		}

		/// <summary>
		/// Gets or sets the component this file row belongs to.
		/// </summary>
		/// <value>Component this file row belongs to.</value>
		public string Component
		{
			get { return (string)this.Fields[1].Data; }
			set { this.Fields[1].Data = value; }
		}

		/// <summary>
		/// Gets or sets the name of the file.
		/// </summary>
		/// <value>Name of the file.</value>
		public string FileName
		{
			get { return (string)this.Fields[2].Data; }
			set { this.Fields[2].Data = value; }
		}

		/// <summary>
		/// Gets or sets the size of the file.
		/// </summary>
		/// <value>Size of the file.</value>
		public long FileSize
		{
			get { return Convert.ToInt64(this.Fields[3].Data, CultureInfo.InvariantCulture.NumberFormat); }
			set { this.Fields[3].Data = value; }
		}

		/// <summary>
		/// Gets or sets the version of the file.
		/// </summary>
		/// <value>Version of the file.</value>
		public string Version
		{
			get { return (string)this.Fields[4].Data; }
			set { this.Fields[4].Data = value; }
		}

		/// <summary>
		/// Gets or sets the LCID of the file.
		/// </summary>
		/// <value>LCID of the file.</value>
		public string Language
		{
			get { return (string)this.Fields[5].Data; }
			set { this.Fields[5].Data = value; }
		}

		/// <summary>
		/// Gets or sets the attributes on a file.
		/// </summary>
		/// <value>Attributes on a file.</value>
		public int Attributes
		{
			get { return Convert.ToInt32(this.Fields[6].Data, CultureInfo.InvariantCulture.NumberFormat); }
			set { this.Fields[6].Data = value; }
		}

		/// <summary>
		/// Gets or sets the sequence of the file row.
		/// </summary>
		/// <value>Sequence of the file row.</value>
		public int Sequence
		{
			get { return Convert.ToInt32(this.Fields[7].Data, CultureInfo.InvariantCulture.NumberFormat); }
			set { this.Fields[7].Data = value; }
		}

		/// <summary>
		/// Gets or sets the type of assembly of file row.
		/// </summary>
		/// <value>Assembly type for file row.</value>
		public FileAssemblyType AssemblyType
		{
			get
			{
				FileAssemblyType type = FileAssemblyType.NotAnAssembly;
				int data = null == this.Fields[8].Data ? -1 : Convert.ToInt32(this.Fields[8].Data);
				if (0 == data)
				{
					type = FileAssemblyType.DotNetAssembly;
				}
				else if (1 == data)
				{
					type = FileAssemblyType.Win32Assembly;
				}

				return type;
			}
			set
			{
				switch (value)
				{
					case FileAssemblyType.DotNetAssembly:
						this.Fields[8].Data = 0;
						break;
					case FileAssemblyType.Win32Assembly:
						this.Fields[8].Data = 1;
						break;
					default:
						this.Fields[8].Data = null;
						break;
				}
			}
		}

		/// <summary>
		/// Gets or sets the identifier for the assembly manifest.
		/// </summary>
		/// <value>Identifier for the assembly manifest.</value>
		public string AssemblyManifest
		{
			get { return (string)this.Fields[9].Data; }
			set { this.Fields[9].Data = value; }
		}

		/// <summary>
		/// Gets or sets the assembly application.
		/// </summary>
		/// <value>Assembly application.</value>
		public string AssemblyApplication
		{
			get { return (string)this.Fields[10].Data; }
			set { this.Fields[10].Data = value; }
		}

		/// <summary>
		/// Gets or sets the directory of the file.
		/// </summary>
		/// <value>Directory of the file.</value>
		public string Directory
		{
			get { return (string)this.Fields[11].Data; }
			set { this.Fields[11].Data = value; }
		}

		/// <summary>
		/// Gets or sets the disk id for this file.
		/// </summary>
		/// <value>Disk id for the file.</value>
		public int DiskId
		{
			get { return Convert.ToInt32(this.Fields[12].Data, CultureInfo.InvariantCulture.NumberFormat); }
			set { this.Fields[12].Data = value; }
		}

		/// <summary>
		/// Gets or sets the source location to the file.
		/// </summary>
		/// <value>Source location to the file.</value>
		public string Source
		{
			get { return (string)this.Fields[13].Data; }
			set { this.Fields[13].Data = value; }
		}

		/// <summary>
		/// Gets or sets the architecture the file executes on.
		/// </summary>
		/// <value>Architecture the file executes on.</value>
		public string ProcessorArchitecture
		{
			get { return (string)this.Fields[14].Data; }
			set { this.Fields[14].Data = value; }
		}

		/// <summary>
		/// Gets of sets the patch group of a patch-added file.
		/// </summary>
		/// <value>The patch group of a patch-added file.</value>
		public int PatchGroup
		{
			get { return Convert.ToInt32(this.Fields[15].Data, CultureInfo.InvariantCulture.NumberFormat); }
			set { this.Fields[15].Data = value; }
		}
	}
}
