// Copyright (c) 2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel.DomainServices;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SIL.AlloGenModel
{
	public class WritingSystem
	{
		public string Name { get; set; } = "";
		public int Handle { get; set; } = -1;

		[XmlIgnore]
		public Font Font { get; set; }

		[XmlIgnore]
		public FontInfo FontInfo { get; set; }

		[XmlIgnore]
		public Color Color { get; set; }

		public WritingSystem Duplicate()
		{
			WritingSystem newWS = new WritingSystem();
			newWS.Name = Name;
			newWS.Handle = Handle;
			newWS.Font = Font;
			newWS.FontInfo = FontInfo;
			newWS.Color = Color;
			return newWS;
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
				WritingSystem ws = (WritingSystem)obj;
				return base.Equals(obj)
					&& (Name == ws.Name)
					&& (Handle == ws.Handle)
					&& (Font == ws.Font)
					&& (FontInfo == ws.FontInfo)
					&& (Color == ws.Color);
			}
		}

		public override int GetHashCode()
		{
			return Tuple.Create(Name, Handle, Font, FontInfo, Color).GetHashCode();
		}

		override public string ToString()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(Name);
			return sb.ToString();
		}
	}
}
