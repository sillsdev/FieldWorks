// Copyright (c) 2011-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Areas.TextsAndWords.Discourse
{
	internal enum DecoratorMethodTypes
	{
		UnknownMethod,
		AddObj,
		AddObjProp,
		AddObjVec,
		AddObjVecItems,
		AddString,
		AddStringProp,
		CloseInnerPile,
		CloseParagraph,
		CloseSpan,
		CloseTableCell,
		NoteDependency,
		OpenInnerPile,
		OpenParagraph,
		OpenSpan,
		OpenTableCell,
		PropsSetter,
		SetIntProperty
	}
}