// Copyright (c) 2006-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections;
using System.Collections.Generic;

namespace LanguageExplorer.SfmToXml
{
	/// <summary>
	/// This interface exists to serve as a contract between the import process and the object.
	/// </summary>
	public interface ILexImportFields
	{
		Dictionary<string, ILexImportField> GetAutoFields();
		ILexImportField GetAutoField(string className);
		ILexImportField GetField(string className, string fwDest);
		ILexImportField GetField(string fwDest, out string className);  // assumes that the fwDest is only in one class

		bool AddField(string className, string partOf, ILexImportField field);
		bool AddCustomField(int classID, ILexImportCustomField field);
		bool ContainsCustomField(string key);
		ICollection Classes { get; }
		string HierarchyForClass(string className);
		ICollection FieldsForClass(string className);
		string GetUIDestForName(string fieldName);
		bool GetDestinationForName(string name, out string className, out string fieldName);
		bool ReadLexImportFields(string xmlFileName);
		bool AddCustomImportFields(ILexImportFields customFields);
		string GetCustomFieldClassFromClassID(int classID);
	}
}