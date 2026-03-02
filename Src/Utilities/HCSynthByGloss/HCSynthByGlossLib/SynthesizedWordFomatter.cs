// Copyright (c) 2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HCSynthByGloss
{
	public class SynthesizedWordFomatter
	{
		private static readonly SynthesizedWordFomatter instance = new SynthesizedWordFomatter();
		const string separator = "%";
		const string failure = "%0%";

		public static SynthesizedWordFomatter Instance
		{
			get { return instance; }
		}

		public string Format(IEnumerable<string> forms, string analysis)
		{
			StringBuilder sb = new StringBuilder();
			int count = forms.Count();
			switch (count)
			{
				case 0:
					sb.Append(failure);
					sb.Append(analysis);
					sb.Append(separator);
					break;
				case 1:
					sb.Append(forms.ElementAt(0));
					break;
				default:
					sb.Append(separator);
					sb.Append(count);
					sb.Append(separator);
					for (int i = 0; i < count; i++)
					{
						sb.Append(forms.ElementAt(i));
						sb.Append(separator);
					}
					break;
			}
			return sb.ToString();
		}
	}
}
