// Copyright (c) 2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Paratext.Data;
using PtxUtils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.Scripture;
// ReSharper disable InconsistentNaming

namespace Paratext8Plugin
{
	/// <summary>
	/// Class wrapping the Paratext8 API for intereacting with scripture data.
	/// </summary>
	[Export(typeof(ScriptureProvider.IScriptureProvider))]
	[ExportMetadata("Version", "8")]
	public class Paratext8Provider : ScriptureProvider.IScriptureProvider
	{
		public string SettingsDirectory { get { return ScrTextCollection.SettingsDirectory; } }

		public IEnumerable<string> NonEditableTexts { get { return ScrTextCollection.ScrTexts(IncludeProjects.Resources | IncludeProjects.Inaccessible).Select(scrTxt => scrTxt.Name.ToLowerInvariant()); } }
		public IEnumerable<string> ScrTextNames { get { return ScrTextCollection.ScrTexts(IncludeProjects.AllAccessible).Select(scrText => scrText.Name.ToLowerInvariant()); } }
		public void Initialize()
		{
			ParatextData.Initialize();
			Alert.Implementation = new ParatextAlert();
		}

		public void RefreshScrTexts()
		{
			ScrTextCollection.RefreshScrTexts();
		}

		public IEnumerable<IScrText> ScrTexts()
		{
			return ScriptureProvider.WrapPtCollection(ScrTextCollection.ScrTexts(IncludeProjects.ScriptureOnly),
				new Func<ScrText, IScrText>(ptText => new PT8ScrTextWrapper(ptText)));
		}

		public IVerseRef MakeVerseRef(int bookNum, int i, int i1)
		{
			return new PT8VerseRefWrapper(new VerseRef(bookNum, i, i1));
		}

		public IScrText Get(string project)
		{
			return new PT8ScrTextWrapper(ScrTextCollection.Get(project));
		}

		public IScrText MakeScrText(string projectName)
		{
			return string.IsNullOrEmpty(projectName) ? new PT8ScrTextWrapper(new ScrText()) : new PT8ScrTextWrapper(new ScrText(projectName));
		}

		/// <summary/>
		public ScriptureProvider.IScriptureProviderParserState GetParserState(IScrText ptProjectText, IVerseRef ptCurrBook)
		{
			return new PT8ParserStateWrapper(new ScrParserState((ScrText)ptProjectText.CoreScrText, (VerseRef)ptCurrBook.CoreVerseRef));
		}

		/// <summary/>
		public Version MaximumSupportedVersion
		{
			get { return IsInstalled ? ParatextInfo.ParatextVersion : new Version(); }
		}

		public bool IsInstalled { get { return ParatextInfo.IsParatextInstalled; } }
	}

	public class PT8ParserStateWrapper : ScriptureProvider.IScriptureProviderParserState
	{
		private ScrParserState ptParserState;
		/// <summary>
		/// The paratext code is looking for an identical list when UpdateState is called. We will unwrap the list only if the list
		/// given to UpdateState changes.
		/// </summary>
		private List<IUsfmToken> wrappedTokenList;

		private List<UsfmToken> rawPtTokenList;

		public PT8ParserStateWrapper(ScrParserState scrParserState)
		{
			ptParserState = scrParserState;
		}

		public IVerseRef VerseRef { get { return new PT8VerseRefWrapper(ptParserState.VerseRef); } }

		public void UpdateState(List<IUsfmToken> ptBookTokens, int ptCurrentToken)
		{
			if (wrappedTokenList != ptBookTokens)
			{
				wrappedTokenList = ptBookTokens;
				rawPtTokenList = new List<UsfmToken>(ptBookTokens.Select(t => (UsfmToken)t.CoreToken));
			}
			ptParserState.UpdateState(rawPtTokenList, ptCurrentToken);
		}
	}
}