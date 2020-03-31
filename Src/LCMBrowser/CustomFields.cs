// Copyright (c) 2010-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LCMBrowser
{
	/// <summary>
	/// Class to hold custom fields in LCMBrowser
	///  </summary>
	///
	internal sealed class CustomFields
	{
		///<summary />
		internal string Name;

		///<summary />
		internal int ClassID;

		///<summary />
		internal int FieldID;

		///<summary />
		internal string Type;

		///<summary/>
		internal CustomFields(string name, int classID, int fieldID, string type)
		{
			Name = name;
			ClassID = classID;
			FieldID = fieldID;
			Type = type;
		}
	}
}