//-------------------------------------------------------------------------------------------------
// <copyright file="ControlRow.cs" company="Microsoft">
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
// Specialization of a row for the Control table.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;
	using System.Text;
	using System.Xml;

	/// <summary>
	/// Specialization of a row for the Control table.
	/// </summary>
	public class ControlRow : Row
	{
		/// <summary>
		/// Creates a Control row that belongs to a table.
		/// </summary>
		/// <param name="sourceLineNumbers">Original source lines for this row.</param>
		/// <param name="table">Table this Control row belongs to and should get its column definitions from.</param>
		public ControlRow(SourceLineNumberCollection sourceLineNumbers, Table table) :
			base(sourceLineNumbers, table)
		{
		}

		/// <summary>Creates a Control row that does not belong to a table.</summary>
		/// <param name="tableDef">TableDefinition this Control row should get its column definitions from.</param>
		public ControlRow(TableDefinition tableDef) :
			base(tableDef)
		{
		}

		/// <summary>
		/// Gets or sets the dialog of the Control row.
		/// </summary>
		/// <value>Primary key of the Control row.</value>
		public string Dialog
		{
			get { return (string)this.Fields[0].Data; }
			set { this.Fields[0].Data = value; }
		}

		/// <summary>
		/// Gets or sets the identifier for this Control row.
		/// </summary>
		/// <value>Identifier for this Control row.</value>
		public string Control
		{
			get { return (string)this.Fields[1].Data; }
			set { this.Fields[1].Data = value; }
		}

		/// <summary>
		/// Gets or sets the type of the control.
		/// </summary>
		/// <value>Name of the control.</value>
		public string Type
		{
			get { return (string)this.Fields[2].Data; }
			set { this.Fields[2].Data = value; }
		}

		/// <summary>
		/// Gets or sets the X location of the control.
		/// </summary>
		/// <value>X location of the control.</value>
		public string X
		{
			get { return (string)this.Fields[3].Data; }
			set { this.Fields[3].Data = value; }
		}

		/// <summary>
		/// Gets or sets the Y location of the control.
		/// </summary>
		/// <value>Y location of the control.</value>
		public string Y
		{
			get { return (string)this.Fields[4].Data; }
			set { this.Fields[4].Data = value; }
		}

		/// <summary>
		/// Gets or sets the width of the control.
		/// </summary>
		/// <value>Width of the control.</value>
		public string Width
		{
			get { return (string)this.Fields[5].Data; }
			set { this.Fields[5].Data = value; }
		}

		/// <summary>
		/// Gets or sets the height of the control.
		/// </summary>
		/// <value>Height of the control.</value>
		public string Height
		{
			get { return (string)this.Fields[6].Data; }
			set { this.Fields[6].Data = value; }
		}

		/// <summary>
		/// Gets or sets the attributes for the control.
		/// </summary>
		/// <value>Attributes for the control.</value>
		public long Attributes
		{
			get { return Convert.ToInt64(this.Fields[7].Data); }
			set { this.Fields[7].Data = value; }
		}

		/// <summary>
		/// Gets or sets the Property associated with the control.
		/// </summary>
		/// <value>Property associated with the control.</value>
		public string Property
		{
			get { return (string)this.Fields[8].Data; }
			set { this.Fields[8].Data = value; }
		}

		/// <summary>
		/// Gets or sets the text of the control.
		/// </summary>
		/// <value>Text of the control.</value>
		public string Text
		{
			get { return (string)this.Fields[9].Data; }
			set { this.Fields[9].Data = value; }
		}

		/// <summary>
		/// Gets or sets the next control.
		/// </summary>
		/// <value>Next control.</value>
		public string Next
		{
			get { return (string)this.Fields[10].Data; }
			set { this.Fields[10].Data = value; }
		}

		/// <summary>
		/// Gets or sets the help for the control.
		/// </summary>
		/// <value>Help for the control.</value>
		public string Help
		{
			get { return (string)this.Fields[11].Data; }
			set { this.Fields[11].Data = value; }
		}

		/// <summary>
		/// Gets or sets the source location to the file to fill in the Text of the control.
		/// </summary>
		/// <value>Source location to the file to fill in the Text of the control.</value>
		public string SourceFile
		{
			get { return (string)this.Fields[12].Data; }
			set { this.Fields[12].Data = value; }
		}
	}
}
