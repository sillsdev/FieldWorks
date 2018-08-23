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
		public string Name = "";

		///<summary />
		public int ClassID = 0;

		///<summary />
		public int FieldID = 0;

		///<summary />
		public string Type = "";

		///<summary/>
		public CustomFields(string name, int classID, int fieldID, string type)
		{
			this.Name = name;
			this.ClassID = classID;
			this.FieldID = fieldID;
			this.Type = type;
		}
	}
}
