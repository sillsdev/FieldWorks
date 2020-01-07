// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;
using SIL.LCModel;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary />
	public interface IGhostable
	{
		/// <summary>
		/// Initialize the class for handling ghost items and parents of ghost items.
		/// </summary>
		void InitForGhostItems(LcmCache cache, XElement colSpec);
	}
}