// Copyright (c) 2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.RootSites;

namespace LanguageExplorer
{
	internal interface IViewSlice : ISlice
	{
		RootSite RootSite { get; }
	}
}