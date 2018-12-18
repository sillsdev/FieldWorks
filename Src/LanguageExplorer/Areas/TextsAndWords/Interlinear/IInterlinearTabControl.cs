// Copyright (c) 2009-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// This interface helps to identify a control that can be used in InterlinMaster tab pages.
	/// In the future, we may not want to force any such control to implement all of these
	/// interfaces, but for now, this works.
	/// </summary>
	public interface IInterlinearTabControl : IChangeRootObject
	{
		LcmCache Cache { get; set; }
	}
}