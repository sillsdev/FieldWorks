// Copyright (c) 2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIL.AlloGenModel
{
	public class ApplyTo
	{
		public string Name { get; set; } = "";
		public int Id { get; set; } = -1;

		public ApplyTo(string name, int id)
		{
			Name = name;
			Id = id;
		}

		override public string ToString()
		{
			return Name;
		}
	}
}
