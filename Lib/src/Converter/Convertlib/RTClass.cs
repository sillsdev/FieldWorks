using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace ConvertLib
{
	public class RTClass
	{
		public string ClassName = "";
		public int ownFlid = 0;
		public string Guid = "";
		public string owningGuid = "";
		public Dictionary<int, StringCollection> elementList = new Dictionary<int, StringCollection>();
		public List<int> hierarchy = new List<int>();
	}
}
