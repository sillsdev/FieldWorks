// Copyright (c) 2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIL.ToneParsFLEx
{
	public struct OrthoChangeMapping
	{
		public OrthoChangeMapping(string from, string to)
		{
			From = from;
			To = to;
		}

		public string From { get; set; }
		public string To { get; set; }

		public override string ToString() => $"({From}, {To})";
	}
}
