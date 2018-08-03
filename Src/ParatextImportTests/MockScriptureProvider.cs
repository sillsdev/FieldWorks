// Copyright (c) 2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.LCModel.Utils;

namespace ParatextImport
{
#if RANDYTODO
	internal class MockScriptureProvider : ScriptureProvider.IScriptureProvider
	{
		public MockScriptureProvider()
		{
			NonEditableTexts = new List<string>();
		}
		public string SettingsDirectory
		{
			get { return MiscUtils.IsUnix ? "~/MyParatextProjects/" : @"c:\My Paratext Projects\"; }
		}
		public IEnumerable<string> NonEditableTexts { get; set; }
		public IEnumerable<string> ScrTextNames { get; set; }
		public void Initialize()
		{
			throw new NotImplementedException();
		}

		public void RefreshScrTexts()
		{
			throw new NotImplementedException();
		}

		public IEnumerable<IScrText> ScrTexts()
		{
			throw new NotImplementedException();
		}

		public IVerseRef MakeVerseRef(int bookNum, int i, int i1)
		{
			throw new NotImplementedException();
		}

		public IScrText Get(string project)
		{
			throw new NotImplementedException();
		}

		public IScrText MakeScrText(string paratextProjectId)
		{
			throw new NotImplementedException();
		}

		public ScriptureProvider.IScriptureProviderParserState GetParserState(IScrText ptProjectText, IVerseRef ptCurrBook)
		{
			throw new NotImplementedException();
		}

		public Version MaximumSupportedVersion { get; set; }
		public bool IsInstalled { get; set; }
	}:
#endif
}
