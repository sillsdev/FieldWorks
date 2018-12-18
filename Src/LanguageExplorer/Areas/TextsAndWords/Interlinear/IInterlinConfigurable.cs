// Copyright (c) 2018-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ViewsInterfaces;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	public interface IInterlinConfigurable : IInterlinearTabControl
	{
		IPropertyTable PropertyTable { get; }
		IVwRootBox Rootb { get; set; }
	}
}
