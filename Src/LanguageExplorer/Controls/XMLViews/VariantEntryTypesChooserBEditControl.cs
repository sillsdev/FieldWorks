// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.Controls.XMLViews
{
#if RANDYTODO
	// TODO: Why does this class need to exist? It adds nothing to the superclass.
	// TODO: Jason Naylor's Gerrit comment: "I saw some comments in an earlier file about a hack to handle
	// TODO: Variants separately from ComplexForms, I wonder if this class is involved in that hack?"
#endif
	internal class VariantEntryTypesChooserBEditControl : ComplexListChooserBEditControl
	{
		internal VariantEntryTypesChooserBEditControl(LcmCache cache, IPropertyTable propertyTable, XElement colSpec)
			: base(cache, propertyTable, colSpec)
		{
		}
	}
}