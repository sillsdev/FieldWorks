//-------------------------------------------------------------------------------------------------
// <copyright file="PropertyRow.cs" company="Microsoft">
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
// Specialization of a row for the property table.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;

	/// <summary>
	/// Specialization of a row for the property table.
	/// </summary>
	public class PropertyRow : Row
	{
		/// <summary>Creates a Property row that belongs to a table.</summary>
		/// <param name="sourceLineNumbers">Original source lines for this row.</param>
		/// <param name="table">Table this Property row belongs to and should get its column definitions from.</param>
		public PropertyRow(SourceLineNumberCollection sourceLineNumbers, Table table) :
			base(sourceLineNumbers, table)
		{
		}

		/// <summary>
		/// Gets and sets the id for this property row.
		/// </summary>
		/// <value>Id for the property.</value>
		public string Id
		{
			get { return (string)this.Fields[0].Data; }
			set { this.Fields[0].Data = value; }
		}

		/// <summary>
		/// Gets and sets the value for this property row.
		/// </summary>
		/// <value>Value of the property.</value>
		public string Value
		{
			get { return (string)this.Fields[1].Data; }
			set { this.Fields[1].Data = value; }
		}

		/// <summary>
		/// Gets and sets if this is an admin property row.
		/// </summary>
		/// <value>Flag if this is an admin property.</value>
		public bool Admin
		{
			get { return this.ConvertToBoolean(this.Fields[2].Data); }
			set { this.Fields[2].Data = value; }
		}

		/// <summary>
		/// Gets and sets if this is a secure property row.
		/// </summary>
		/// <value>Flag if this is a secure property.</value>
		public bool Secure
		{
			get { return this.ConvertToBoolean(this.Fields[3].Data); }
			set { this.Fields[3].Data = value; }
		}

		/// <summary>
		/// Gets and sets if this is a hidden property row.
		/// </summary>
		/// <value>Flag if this is a hidden property.</value>
		public bool Hidden
		{
			get { return this.ConvertToBoolean(this.Fields[4].Data); }
			set { this.Fields[4].Data = value; }
		}

		/// <summary>
		/// Simple little function that takes an object and checks if it is true or false.
		/// </summary>
		/// <param name="o">Object to convert to true or false.</param>
		/// <returns>If o equals "1", "true", or "True" then returns true otherwise false.</returns>
		private bool ConvertToBoolean(object o)
		{
			if (null != o && "1" == o.ToString() || "true" == o.ToString() || "True" == o.ToString())
			{
				return true;
			}

			return false;
		}
	}
}
