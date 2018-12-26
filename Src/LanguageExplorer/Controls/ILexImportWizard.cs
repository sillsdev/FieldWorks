// Copyright (c) 2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using LanguageExplorer.SfmToXml;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Controls
{
	internal interface ILexImportWizard : IPropertyTableProvider, IPublisherProvider
	{
		bool AddLanguage(string langDesc, string ws, string ec, string wsId);

		ILexImportFields ReadCustomFieldsFromDB(out bool changed);
	}
}