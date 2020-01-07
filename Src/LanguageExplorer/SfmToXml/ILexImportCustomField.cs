// Copyright (c) 2006-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace LanguageExplorer.SfmToXml
{
	public interface ILexImportCustomField : ILexImportField
	{
		int WsSelector { get; }
		bool Big { get; }
		int FLID { get; }
		string Class { get; }
		string UIClass { get; set; }
		string CustomKey { get; }
		uint CRC { get; }
		Guid ListRootId { get; }
	}
}