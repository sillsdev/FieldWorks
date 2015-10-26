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
	public class Classes
	{
		public int ClassNum = 0;
		public int ModNum = 0;
		public string ClassName = "";
		public Dictionary<string, int> fields = new Dictionary<string, int>();
		public string BaseClassName = "";
	}
}
