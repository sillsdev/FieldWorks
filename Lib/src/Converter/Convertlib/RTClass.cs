// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

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
