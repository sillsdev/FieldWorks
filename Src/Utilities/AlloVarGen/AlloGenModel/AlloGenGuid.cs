// Copyright (c) 2022-2023 SIL International
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
	abstract public class AlloGenGuid : AlloGenBase
	{
		[XmlAttribute("guid")]
		public string Guid { get; set; } = "";
		public string Name { get; set; } = "";

		override public string ToString()
		{
			return Name;
		}

		public override bool Equals(Object obj)
		{
			if (!base.Equals(obj))
				return false;

			//Check for null and compare run-time types.
			if ((obj == null) || !this.GetType().Equals(obj.GetType()))
				return false;
			else
			{
				AlloGenGuid agg = (AlloGenGuid)obj;
				return (Guid == agg.Guid) && (Name == agg.Name);
			}
		}

		public override int GetHashCode()
		{
			int result = base.GetHashCode();
			return result + Tuple.Create(Guid, Name).GetHashCode();
		}
	}
}
