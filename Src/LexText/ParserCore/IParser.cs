// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace SIL.FieldWorks.WordWorks.Parser
{
	public interface IParser : IDisposable
	{
		// Warning: This method is not thread-safe.
		//  The parser could end up using stale data.
		bool IsUpToDate();

		void Update();

		void Reset();

		ParseResult ParseWord(string word);

		XDocument ParseWordXml(string word);

		XDocument TraceWordXml(string word, IEnumerable<int> selectTraceMorphs);
	}
}
