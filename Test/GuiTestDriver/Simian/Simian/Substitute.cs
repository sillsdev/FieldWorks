// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Text;

namespace Simian
{
	public class Substitute
	{
		public readonly string formal;
		public readonly string value;

		public Substitute (string formal, string value)
		{ this.formal = formal; this.value = value; }

		public String image () {return formal + "=\""+value+"\" ";}
		}
}
