// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FDOBrowser
{
	/// <summary>
	/// Class to hold custom fields in FDOBrowser
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

		///<summary>
		/// Initialize class with passed in parameters.
		///</summary>
		///<param name="name"></param>
		///<param name="classID"></param>
		///<param name="fieldID"></param>
		///<param name="type"></param>
		///
		public CustomFields(string name, int classID, int fieldID, string type)
		{
			this.Name = name;
			this.ClassID = classID;
			this.FieldID = fieldID;
			this.Type = type;
		}
	}
}
