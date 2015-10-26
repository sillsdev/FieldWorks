// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.FieldWorks.FDO.Application.ApplicationServices;

namespace SIL.FieldWorks.FDO.Application
{
	/// <summary>
	/// This is a helper class for XmlImportData. It tracks links (references from one object to another)
	/// that have not yet been resolved.
	/// </summary>
	class ReferenceTracker
	{
		private Dictionary<ICmObject, List<XmlImportData.PendingLink>> m_lookupLinks =
			new Dictionary<ICmObject, List<XmlImportData.PendingLink>>();

		public void Add(XmlImportData.PendingLink link)
		{
			var owner = link.FieldInformation.Owner;
			List<XmlImportData.PendingLink> linksForOwner;
			if (!m_lookupLinks.TryGetValue(owner, out linksForOwner))
			{
				linksForOwner = new List<XmlImportData.PendingLink>();
				m_lookupLinks[owner] = linksForOwner;
			}
			linksForOwner.Add(link);
		}

		/// <summary>
		/// Return all the links.
		/// </summary>
		public IEnumerable<XmlImportData.PendingLink> Links
		{
			get {return m_lookupLinks.Values.SelectMany(list => list);}
		}

		public List<XmlImportData.PendingLink> LinksForField(ICmObject owner, int flid)
		{
			List<XmlImportData.PendingLink> linksForOwner;
			if (!m_lookupLinks.TryGetValue(owner, out linksForOwner))
				return new List<XmlImportData.PendingLink>();  // nothing is linked from this owner.
			return (from link in linksForOwner where link.FieldInformation.FieldId == flid select link).ToList();
		}

		public void UpdateOwner(ICmObject oldOwner, ICmObject newOwner, bool fUpdateParents)
		{
			List<XmlImportData.PendingLink> linksForOwner;
			if (!m_lookupLinks.TryGetValue(oldOwner, out linksForOwner))
				return;  // nothing is linked from this owner.

			foreach (XmlImportData.PendingLink pend in linksForOwner)
			{
				XmlImportData.FieldInfo fi2 = pend.FieldInformation;
				if (fi2.Owner == oldOwner)
					fi2.Owner = newOwner;
				if (!fUpdateParents)
					continue;
				while (fi2.ParentOfOwner != null)
				{
					fi2 = fi2.ParentOfOwner;
					if (fi2.Owner == oldOwner)
						fi2.Owner = newOwner;
				}
			}
		}
	}
}
