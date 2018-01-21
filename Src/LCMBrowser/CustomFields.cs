// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LCMBrowser
{
	/// <summary>
	/// Class to hold custom fields in LCMBrowser
	///  </summary>
	///
	public class CustomFields
	{
		///<summary />
		public string Name;

		///<summary />
		public int ClassID;

		///<summary />
		public int FieldID;

		///<summary />
		public string Type;

		///<summary>
		/// Initialize class with passed in parameters.
		///</summary>
		public CustomFields(string name, int classID, int fieldID, string type)
		{
			Name = name;
			ClassID = classID;
			FieldID = fieldID;
			Type = type;
		}
	}
}