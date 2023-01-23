// Copyright (c) 2017-2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Paratext.Data;
using Paratext.Data.ProjectSettingsAccess;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.Scripture;
using ProjectType = SIL.FieldWorks.Common.ScriptureUtils.ProjectType;
using ScrStyleType = SIL.FieldWorks.Common.ScriptureUtils.ScrStyleType;

namespace Paratext8Plugin
{
	/// <summary>
	/// Wrapper for the Paratext8 version of ScrText and all the trimmings
	/// </summary>
	public class PT8ScrTextWrapper : IScrText
	{
		private ScrText pt8Object;
		public PT8ScrTextWrapper(ScrText text)
		{
			pt8Object = text;
		}

		public void Reload()
		{
			pt8Object.Reload();
		}

		public IScriptureProviderStyleSheet DefaultStylesheet => new Pt8StyleSheetWrapper(pt8Object.DefaultStylesheet);

		internal class Pt8StyleSheetWrapper : IScriptureProviderStyleSheet
		{
			private ScrStylesheet ptStyleSheet;
			public Pt8StyleSheetWrapper(ScrStylesheet ptObjectDefaultStylesheet)
			{
				ptStyleSheet = ptObjectDefaultStylesheet;
			}

			public IEnumerable<ITag> Tags
			{
				get { return ScriptureProvider.WrapPtCollection(ptStyleSheet.Tags, new Func<ScrTag, ITag>(tag => new PT8TagWrapper(tag))); }
			}

			internal class PT8TagWrapper : ITag
			{
				private ScrTag ptTag;

				public PT8TagWrapper(ScrTag tag)
				{
					ptTag = tag;
				}

				public string Marker { get => ptTag.Marker; set => ptTag.Marker = value; }
				public string Endmarker { get => ptTag.Endmarker; set => ptTag.Endmarker = value; }

				public ScrStyleType StyleType
				{
					// REVIEW (Hasso) 2022.10: Why can't we simply return ptTag.StyleType? If we want an SIL.FW.C.SU.SST, why are we parsing typeof(P.D.SST)?
					get => (ScrStyleType)Enum.Parse(typeof(Paratext.Data.ScrStyleType), ptTag.StyleType.ToString());
					// REVIEW (Hasso) 2022.10: What?!? This setter converts the existing type to a string, parses it as an Enum, and assigns it back to itself.
					set => ptTag.StyleType = (Paratext.Data.ScrStyleType)Enum.Parse(typeof(Paratext.Data.ScrStyleType), ptTag.StyleType.ToString());
				}

				public bool IsScriptureBook => (ptTag.TextProperties & TextProperties.scBook) != 0;
			}
		}

		public IScriptureProviderParser Parser => new Pt8ParserWrapper(pt8Object.Parser);

		internal class Pt8ParserWrapper : IScriptureProviderParser
		{
			private ScrParser ptParser;
			public Pt8ParserWrapper(ScrParser parser)
			{
				ptParser = parser;
			}

			public IEnumerable<IUsfmToken> GetUsfmTokens(IVerseRef verseRef, bool b, bool b1)
			{
				return ScriptureProvider.WrapPtCollection(ptParser.GetUsfmTokens((VerseRef)verseRef.CoreVerseRef, b, b1),
					new Func<UsfmToken, IUsfmToken>(token => new PT8TokenWrapper(token)));
			}

			internal class PT8TokenWrapper : IUsfmToken
			{
				private UsfmToken ptToken;
				public PT8TokenWrapper(UsfmToken token)
				{
					ptToken = token;
				}

				public string Marker => ptToken.Marker;

				public string EndMarker => ptToken.EndMarker;

				public TokenType Type { get { return Enum.TryParse(ptToken.Type.ToString(), out TokenType outValue) ? outValue : TokenType.Unknown; } }

				public object CoreToken => ptToken;

				public string Text => ptToken.Text;
			}
		}

		public IScriptureProviderBookSet BooksPresentSet
		{
			get => new PT8BookSetWrapper(pt8Object.Settings.BooksPresentSet);
			set => throw new NotImplementedException();
		}

		internal class PT8BookSetWrapper : IScriptureProviderBookSet
		{
			private BookSet ptBookSet;
			public PT8BookSetWrapper(BookSet getValue)
			{
				ptBookSet = getValue;
			}

			public IEnumerable<int> SelectedBookNumbers => ptBookSet.SelectedBookNumbers;
		}

		public string Name
		{
			get => pt8Object.Name;
			set => pt8Object.Name = value;
		}

		public ILexicalProject AssociatedLexicalProject
		{
			get => new PT8LexicalProjectWrapper(pt8Object.Settings.AssociatedLexicalProject);
			set => throw new NotImplementedException();
		}

		internal class PT8LexicalProjectWrapper : LexicalProject
		{
			private AssociatedLexicalProject ptObject;
			public PT8LexicalProjectWrapper(AssociatedLexicalProject project)
			{
				ptObject = project;
			}

			public override string ProjectType => ptObject.ApplicationType;

			public override string ProjectId => ptObject.ProjectId;
		}

		public ITranslationInfo TranslationInfo
		{
			get => new PT8TranslationInfoWrapper(pt8Object.Settings.TranslationInfo);
			set => throw new NotImplementedException();
		}

		internal class PT8TranslationInfoWrapper : ITranslationInfo
		{
			private TranslationInformation ptObject;

			public string BaseProjectName => ptObject.BaseProjectName;

			public ProjectType Type
			{
				get
				{
					try
					{
						return (ProjectType) Enum.Parse(typeof(ProjectType),
							ptObject.Type.ToString());
					}
					catch (ArgumentException)
					{
						// The enum type is unknown - Assert for developers, and ignore.
						Debug.Fail($"Unknown Enum value {ptObject.Type.ToString()}");
						return ProjectType.Unknown;
					}
				}
			}
			public PT8TranslationInfoWrapper(TranslationInformation translationInfo)
			{
				ptObject = translationInfo;
			}
		}

		public bool Editable
		{
			get => pt8Object.Settings.Editable;
			set => pt8Object.Settings.Editable = value;
		}

		public bool IsResourceText => pt8Object.IsResourceProject;

		public string Directory => pt8Object.Directory;

		/// <summary>
		/// For unit tests only. FLEx is not responsible for disposing of IScrText objects received from ParaText
		/// </summary>
		internal void DisposePTObject()
		{
			if (pt8Object != null)
				pt8Object.Dispose();
			pt8Object = null;
		}

		public void SetParameterValue(string resourcetext, string s)
		{
			pt8Object.Settings.SetSetting(resourcetext, s);
		}

		public string BooksPresent
		{
			get => pt8Object.Settings.BooksPresentSet.Books;
			set => pt8Object.Settings.BooksPresentSet.Books = value;
		}

		public bool BookPresent(int bookCanonicalNum)
		{
			return pt8Object.BookPresent(bookCanonicalNum);
		}

		public bool IsCheckSumCurrent(int bookCanonicalNum, string checkSum)
		{
			return pt8Object.IsCheckSumCurrent(bookCanonicalNum, checkSum);
		}

		public IScrVerse Versification => new Pt8VerseWrapper(pt8Object.Settings.Versification);

		public override string ToString()
		{
			return Name;
		}

		public string JoinedNameAndFullName { get { return pt8Object.FullName; } }

		public string FileNamePrePart => throw new NotImplementedException("Filename parts changed for PT8. Unnecessary perhaps?");

		public string FileNameForm => throw new NotImplementedException("Filename parts changed for PT8. Unnecessary perhaps?");

		public string FileNamePostPart => throw new NotImplementedException("Filename parts changed for PT8. Unnecessary perhaps?");

		public object CoreScrText => pt8Object;

		public string GetBookCheckSum(int canonicalNum)
		{
			return pt8Object.GetBookCheckSum(canonicalNum);
		}
	}
}