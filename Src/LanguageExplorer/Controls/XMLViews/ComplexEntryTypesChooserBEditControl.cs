// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Xml.Linq;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.Controls.XMLViews
{
	internal class ComplexEntryTypesChooserBEditControl : ComplexListChooserBEditControl
	{
		HashSet<int> m_complexEntryRefs;

		internal ComplexEntryTypesChooserBEditControl(LcmCache cache, IPropertyTable propertyTable, XElement colSpec)
			: base(cache, propertyTable, colSpec)
		{
		}

		/// <summary>
		/// kludge: filter to allow only complex entry references.
		/// </summary>
		protected override bool DisableItem(int hvoItem)
		{
			if (m_complexEntryRefs != null)
			{
				return !m_complexEntryRefs.Contains(hvoItem);
			}
			m_complexEntryRefs = new HashSet<int>();
#if RANDYTODO
			// TODO: Why create 'dict', not fill it with anything, but then loop over it to no apparent purpose?
#endif
			var dict = new Dictionary<int,List<int>>();
			// go through each list and add the values to our set.
			foreach (var refs in dict.Values)
			{
				m_complexEntryRefs.UnionWith(refs);
			}
			return !m_complexEntryRefs.Contains(hvoItem);
		}

		public override void FakeDoit(IEnumerable<int> itemsToChange, int tagMadeUpFieldIdentifier, int tagEnabled, ProgressState state)
		{
			m_complexEntryRefs = null;	// reset the filtered entry refs cache.
			base.FakeDoit(itemsToChange, tagMadeUpFieldIdentifier, tagEnabled, state);
		}

		public override void DoIt(IEnumerable<int> itemsToChange, ProgressState state)
		{
			m_complexEntryRefs = null; // reset the filtered entry refs cache.
			base.DoIt(itemsToChange, state);
		}
	}
}