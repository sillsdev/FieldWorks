// Copyright (c) 2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIL.GenerateHCConfigForFLExTrans
{
	public class DuplicateGlossInfo : IComparable
	{
		public DuplicateGlossInfo(string name, string gloss)
		{
			Name = name;
			Gloss = gloss;
		}

		public string Name { get; set; }
		public string Gloss { get; set; }

		public int CompareTo(object obj)
		{
			DuplicateGlossInfo dup2 = (DuplicateGlossInfo)obj;
			int compare = Gloss.CompareTo(dup2.Gloss);
			if (compare == 0)
			{
				// the gloss is the same; sort by name
				compare = Name.CompareTo(dup2.Name);
			}
			return compare;
		}

		public override string ToString() => $"({Gloss}, {Name})";
	}
}
