// Copyright (c) 2006-2028 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections;
using System.Xml.Linq;

namespace LanguageExplorer.SfmToXml
{
	/// <summary>
	/// This interface defines a way of interacting with the import fields that
	/// were read in from the controlled xml file that is used for mapping the
	/// markers in the import data to the fields used in FieldWorks.
	/// </summary>
	public interface ILexImportField
	{
		/// <summary>
		/// the FW field id
		/// </summary>
		string ID { get;}
		/// <summary>
		/// the user readable name
		/// </summary>
		string Property { get;}
		/// <summary>
		/// the UI Name
		/// </summary>
		string UIName { get;}
		/// <summary>
		/// FW data type 'like' property
		/// </summary>
		string Signature { get;}
		/// <summary>
		/// true if it's a list
		/// </summary>
		bool IsList { get;}
		/// <summary>
		/// true if it's a multi
		/// </summary>
		bool IsMulti { get;}
		/// <summary>
		/// true if it's a ref: 'lxrel' and 'cref'
		/// </summary>
		bool IsRef { get;}
		/// <summary>
		/// true if it's an auto
		/// </summary>
		bool IsAutoField { get;}
		/// <summary>
		/// only can have on unique per object(overrides begin marker logic)
		/// </summary>
		bool IsUnique { get;}
		/// <summary>
		/// original XML
		/// </summary>
		XElement Element { get;}
		/// <summary>
		/// mdf markers normally associated with this field
		/// </summary>
		ICollection Markers { get;}
		/// <summary>
		/// type of data [integer, date, string, ...]
		/// </summary>
		string DataType { get; set;}
		/// <summary>
		/// true if this field allows the abbr field to be edited
		/// </summary>
		bool IsAbbrField { get; set;}
		/// <summary>
		/// Read a 'Field' node from the controlled xml file that contains the ImportFields
		/// </summary>
		bool ReadElement(XElement element);
		/// <summary>
		/// return an equivalent Cls object
		/// </summary>
		ClsFieldDescription ClsFieldDescription { get;}
		/// <summary />
		ClsFieldDescription ClsFieldDescriptionWith(ClsFieldDescription fieldIn);
	}
}