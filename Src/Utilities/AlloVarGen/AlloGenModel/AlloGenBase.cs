// Copyright (c) 2022 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SIL.AlloGenModel
{
	abstract public class AlloGenBase
	{
		[XmlAttribute("active")]
		public bool Active { get; set; } = true;

		public override bool Equals(Object obj)
		{
			//Check for null and compare run-time types.
			if ((obj == null) || !this.GetType().Equals(obj.GetType()))
				return false;
			else
			{
				AlloGenBase agb = (AlloGenBase)obj;
				return (Active == agb.Active);
			}
		}

		public override int GetHashCode()
		{
			return Active.GetHashCode();
		}
	}
}
