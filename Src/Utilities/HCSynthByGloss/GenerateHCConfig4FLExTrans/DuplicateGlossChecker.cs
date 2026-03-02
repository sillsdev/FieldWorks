// Copyright (c) 2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.Machine.Morphology.HermitCrab;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

namespace SIL.GenerateHCConfigForFLExTrans
{
	public class DuplicateGlossChecker
	{
		Language Language { get; set; }
		XDocument HCConfiguration { get; set; }

		public DuplicateGlossChecker(string HCConfig)
		{
			HCConfiguration = XDocument.Load(HCConfig);
		}

		public void ReportAnyDuplicateGlosses()
		{
			var glosses = new List<DuplicateGlossInfo>();
			var duplicateGlosses = new List<DuplicateGlossInfo>();

			var query =
				from c in HCConfiguration.Root.Descendants("MorphologicalRule").Descendants("Gloss")
				select c;
			foreach (XElement g in query)
			{
				XElement name = g.XPathSelectElement("preceding-sibling::Name");
				string sName = (name != null) ? name.Value : "";
				glosses.Add(new DuplicateGlossInfo(sName, g.Value));
			}
			glosses.Sort();
			DuplicateGlossInfo lastInfo = new DuplicateGlossInfo("", "");
			foreach (DuplicateGlossInfo dupInfo in glosses)
			{
				if (lastInfo.Gloss == dupInfo.Gloss)
				{
					Console.WriteLine(
						"Duplicate gloss found for \""
							+ lastInfo.ToString()
							+ "\" and \""
							+ dupInfo.ToString()
					);
				}
				lastInfo = dupInfo;
			}
		}
	}
}
