// Copyright (c) 2011-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Areas.TextsAndWords.Discourse
{
	internal class StoredMethod
	{
		internal StoredMethod(DecoratorMethodTypes mtype, object[] paramArray)
		{
			MethodType = mtype;
			ParamArray = paramArray;
		}

		internal DecoratorMethodTypes MethodType { get; }

		internal object[] ParamArray { get; }

		internal int ParamCount => ParamArray.Length;
	}
}