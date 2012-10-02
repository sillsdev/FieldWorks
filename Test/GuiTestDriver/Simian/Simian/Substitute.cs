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
