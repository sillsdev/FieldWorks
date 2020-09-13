// Copyright (c) 2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace LanguageExplorer.DictionaryConfiguration
{
	/// <summary>
	/// A disposable writer for generating a fragment of a larger document.
	/// </summary>
	public interface IFragmentWriter : IDisposable
	{
		void Flush();
	}
}