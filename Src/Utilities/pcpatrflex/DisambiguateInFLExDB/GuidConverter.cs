// Copyright (c) 2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIL.DisambiguateInFLExDB
{
	public static class GuidConverter
	{
		public static List<Guid> CreateListFromString(string result)
		{
			List<Guid> guids = new List<Guid>();
			if (!String.IsNullOrEmpty(result))
			{
				var guidStrings = result.Split('\n');
				foreach (string sGuid in guidStrings)
				{
					if (!String.IsNullOrEmpty(sGuid))
					{
						String sGuidToUse = sGuid;
						int i = sGuid.IndexOf("=");
						if (i > 0)
						{
							sGuidToUse = sGuid.Substring(0, i);
						}
						guids.Add(new Guid(sGuidToUse));
					}
				}
			}
			return guids;
		}
	}
}
