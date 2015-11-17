// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace SIL.FieldWorks.FixData
{
	/// <summary>
	/// This class handles custom lists with duplicate names.
	/// </summary>
	class CustomListNameFixer : RtFixer
	{
		Dictionary<string, string> m_guidToName = new Dictionary<string, string>();
		Dictionary<string, string> m_nameToGuid = new Dictionary<string, string>();
		/// <summary>
		/// First pass: gather information: remember all the possibility lists
		/// </summary>
		/// <param name="rt"></param>
		internal override void InspectElement(XElement rt)
		{
			var guid = rt.Attribute("guid").Value;
			var xaClass = rt.Attribute("class");
			if (xaClass == null || xaClass.Value != "CmPossibilityList")
				return;
			var nameElt = rt.Element("Name");
			if (nameElt == null)
				return;
			var aUniElt = nameElt.Element("AUni");
			if (aUniElt == null)
				return;
			var name = aUniElt.Value;
			m_guidToName[guid] = name;
			if (!m_nameToGuid.ContainsKey(name))
				m_nameToGuid[name] = guid; // only the first one we see with this name is allowed to keep it.
		}

		/// <summary>
		/// This is the main routine that does actual repair on list names.
		/// </summary>
		/// <param name="rt"></param>
		/// <param name="logger"></param>
		/// <returns>true (always, currently) to indicate that the list should survive.</returns>
		internal override bool FixElement(XElement rt, FwDataFixer.ErrorLogger logger)
		{
			var guid = rt.Attribute("guid").Value;
			string name;
			if (!m_guidToName.TryGetValue(guid, out name))
				return true; // we didn't see a list with this guid; do nothing
			string firstGuid = m_nameToGuid[name];
			if (firstGuid == guid)
				return true; // This one is allowed to keep this name
			int append = 1;
			string fixedName = name + append;
			while (m_nameToGuid.ContainsKey(fixedName))
				fixedName = name + append++;
			m_nameToGuid[fixedName] = guid; // this name is now taken too.
			rt.Element("Name").Element("AUni").SetValue(fixedName);
			logger(guid, DateTime.Now.ToShortDateString(),
				String.Format(Strings.ksRepairingDuplicateListName, name, firstGuid, guid, fixedName));
			return true;
		}
	}
}
