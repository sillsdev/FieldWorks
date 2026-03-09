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
	public class Replace : AlloGenGuid
	{
		// mode: false = plain; true = regular expression
		[XmlAttribute("mode")]
		public bool Mode { get; set; } = false;
		public string From { get; set; } = "";
		public string To { get; set; } = "";
		public List<string> WritingSystemRefs { get; set; } = new List<string>();
		public string Description { get; set; } = "";

		public Replace()
		{
			if (Guid == "")
			{ // make sure it has a value
				Guid = System.Guid.NewGuid().ToString();
			}
			;
		}

		public Replace Duplicate()
		{
			Replace newReplace = new Replace();
			newReplace.From = From;
			newReplace.Mode = Mode;
			newReplace.To = To;
			newReplace.Active = Active;
			newReplace.Description = Description;
			newReplace.Name = Name;
			List<string> newWritingSystems = new List<string>();
			foreach (string ws in WritingSystemRefs)
			{
				newWritingSystems.Add(ws);
				Console.WriteLine("name='" + ws + "'");
			}
			newReplace.WritingSystemRefs = newWritingSystems;
			return newReplace;
		}

		public bool IsEmpty()
		{
			if (!String.IsNullOrEmpty(From))
				return false;
			if (!String.IsNullOrEmpty(Name))
				return false;
			if (!String.IsNullOrEmpty(Description))
				return false;
			return true;
		}

		override public string ToString()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(Name);
			sb.Append(": ");
			sb.Append("Replace '");
			sb.Append(From);
			sb.Append("' with '");
			sb.Append(To);
			sb.Append("' for");
			foreach (string ws in WritingSystemRefs)
			{
				sb.Append(" ");
				sb.Append(ws);
			}
			sb.Append(".");
			return sb.ToString();
		}

		public override bool Equals(Object obj)
		{
			//Check for null and compare run-time types.
			if ((obj == null) || !this.GetType().Equals(obj.GetType()))
			{
				return false;
			}
			else
			{
				Replace replace = (Replace)obj;
				return base.Equals(obj)
					&& (Description == replace.Description)
					&& (Mode == replace.Mode)
					&& (From == replace.From)
					&& (To == replace.To)
					&& (WritingSystemRefs.SequenceEqual(replace.WritingSystemRefs));
			}
		}

		public override int GetHashCode()
		{
			int result =
				base.GetHashCode()
				+ Tuple.Create(Description, Mode, WritingSystemRefs).GetHashCode();
			return result + Tuple.Create(From, To).GetHashCode();
		}
	}
}
