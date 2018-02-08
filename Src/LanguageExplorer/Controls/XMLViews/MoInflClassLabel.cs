// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using SIL.LCModel;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// Structure used when returning a list of objects for a UI Widgit that wants to list them.
	/// </summary>
	public class MoInflClassLabel : ObjectLabel
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		public MoInflClassLabel(LcmCache cache, IMoInflClass inflClass, string displayNameProperty, string displayWs)
			: base(cache, inflClass, displayNameProperty, displayWs)
		{
		}
		/// <summary>
		/// Constructor.
		/// </summary>
		public MoInflClassLabel(LcmCache cache, IMoInflClass inflClass, string displayNameProperty)
			: base(cache, inflClass, displayNameProperty)
		{
		}

		/// <summary>
		/// Gets the inflection class.
		/// </summary>
		public IMoInflClass InflectionClass => (IMoInflClass)Object;

		/// <summary>
		/// the sub items of the possibility
		/// </summary>
		public override IEnumerable<ObjectLabel> SubItems => CreateObjectLabels(Cache, InflectionClass.SubclassesOC, m_displayNameProperty, m_displayWs);

		/// <summary>
		/// are there any sub items for this item?
		/// </summary>
		public override bool HaveSubItems => InflectionClass.SubclassesOC.Count > 0;
	}
}