// Copyright (c) 2022-2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIL.AlloGenModel
{
	public class WritingSystemName
	{
		public string Name { get; set; } = "";

		public WritingSystemName() { }

		public WritingSystemName(string name)
		{
			Name = name;
		}

		public WritingSystemName Duplicate()
		{
			WritingSystemName newWS = new WritingSystemName();
			newWS.Name = Name;
			return newWS;
		}
	}
}
