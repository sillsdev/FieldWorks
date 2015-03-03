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
