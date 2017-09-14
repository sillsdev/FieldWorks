// Copyright (c) 2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using Paratext;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.Common.ScriptureUtils
{
	[Export(typeof(ScriptureProvider.IScriptureProvider))]
	[ExportMetadata("Version", "7")]
	internal class Paratext7Provider : ScriptureProvider.IScriptureProvider
	{
		public string SettingsDirectory { get { return ScrTextCollection.SettingsDirectory; } }

		public IEnumerable<string> NonEditableTexts { get { return ScrTextCollection.SLTTexts; } }
		public IEnumerable<string> ScrTextNames { get { return ScrTextCollection.ScrTextNames; } }
		public void Initialize()
		{
			ScrTextCollection.Initialize();
		}

		public void RefreshScrTexts()
		{
			ScrTextCollection.RefreshScrTexts();
		}
		public IEnumerable<IScrText> ScrTexts()
		{
			return ScriptureProvider.WrapPtCollection(ScrTextCollection.ScrTexts(true, true),
				new Func<ScrText, IScrText>(ptText => new PT7ScrTextWrapper(ptText)));
		}

		public IVerseRef MakeVerseRef(int bookNum, int i, int i1)
		{
			return new PT7VerseRefWrapper(new VerseRef(bookNum, i, i1));
		}

		public IScrText Get(string project)
		{
			return new PT7ScrTextWrapper(ScrTextCollection.Get(project));
		}

		public IScrText MakeScrText(string projectName)
		{
			return string.IsNullOrEmpty(projectName) ? new PT7ScrTextWrapper(new ScrText()) : new PT7ScrTextWrapper(new ScrText(projectName));
		}

		public ScriptureProvider.IScriptureProviderParserState GetParserState(IScrText ptProjectText, IVerseRef ptCurrBook)
		{
			return new PT7ParserStateWrapper(new ScrParserState((ScrText)ptProjectText.CoreScrText, (VerseRef)ptCurrBook.CoreVerseRef));
		}

		public Version MaximumSupportedVersion
		{
			get { return IsInstalled ? new Version(7, 0) : new Version(); }
		}

		/// <see cref="SettingsDirectory">doesn't work when uninitialized, but it is unsafe to initialize if this directory doesn't exist</see>
		private string SettingsDirPreInit
		{
			get
			{
				using (var paratextKey = Registry.LocalMachine.OpenSubKey(@"Software\ScrChecks\1.0\Settings_Directory"))
				{
					if (paratextKey != null)
					{
						return paratextKey.GetValue("") as string;
					}
				}
				return null;
			}
		}

		public bool IsInstalled { get { return FwRegistryHelper.Paratext7Installed() && Directory.Exists(SettingsDirPreInit); } }

		private class PT7ParserStateWrapper : ScriptureProvider.IScriptureProviderParserState
		{
			private readonly ScrParserState pt7ParserState;
			private List<IUsfmToken> wrappedTokenList;

			private List<UsfmToken> rawPtTokenList;

			public PT7ParserStateWrapper(ScrParserState scrParserState)
			{
				pt7ParserState = scrParserState;
			}

			public IVerseRef VerseRef { get { return new PT7VerseRefWrapper(pt7ParserState.VerseRef); } }

			public void UpdateState(List<IUsfmToken> ptBookTokens, int ptCurrentToken)
			{
				if (wrappedTokenList != ptBookTokens)
				{
					wrappedTokenList = ptBookTokens;
					rawPtTokenList = new List<UsfmToken>(ptBookTokens.Select(t => (UsfmToken)t.CoreToken));
				}

				pt7ParserState.UpdateState(rawPtTokenList, ptCurrentToken);
			}
		}
	}
}
