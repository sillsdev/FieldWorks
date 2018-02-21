// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.Common.Widgets
{
	internal interface IHighlightInfo
	{
		bool ShowHighlight { get; set; }
		bool IsHighlighted(int index);
	}
}