// Copyright (c) 2017 SIL International
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
	internal class PT8ScrTextWrapper : IScrText
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

		public IScriptureProviderStyleSheet DefaultStylesheet
		{
			get
			{
				return new Pt8StyleSheetWrapper(pt8Object.DefaultStylesheet);
			}
		}

		internal class Pt8StyleSheetWrapper : IScriptureProviderStyleSheet
		{
			private ScrStylesheet pt7StyleSheet;
			public Pt8StyleSheetWrapper(ScrStylesheet pt7ObjectDefaultStylesheet)
			{
				pt7StyleSheet = pt7ObjectDefaultStylesheet;
			}

			public IEnumerable<ITag> Tags
			{
				get { return ScriptureProvider.WrapPtCollection(pt7StyleSheet.Tags, new Func<ScrTag, ITag>(tag => new PT8TagWrapper(tag))); }
			}

			internal class PT8TagWrapper : ITag
			{
				private ScrTag ptTag;

				public PT8TagWrapper(ScrTag tag)
				{
					ptTag = tag;
				}

				public string Marker { get { return ptTag.Marker; } set { ptTag.Marker = value; } }
				public string Endmarker { get { return ptTag.Endmarker; } set { ptTag.Endmarker = value; } }

				public ScrStyleType StyleType
				{
					get { return (ScrStyleType)Enum.Parse(typeof(Paratext.Data.ScrStyleType), ptTag.StyleType.ToString()); }
					set { ptTag.StyleType = (Paratext.Data.ScrStyleType)Enum.Parse(typeof(Paratext.Data.ScrStyleType), ptTag.StyleType.ToString()); }
				}

				public bool IsScriptureBook { get { return (ptTag.TextProperties & TextProperties.scBook) != 0; } }
			}
		}

		public IScriptureProviderParser Parser
		{
			get { return new Pt8ParserWrapper(pt8Object.Parser); }
		}

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

				public string Marker { get { return ptToken.Marker; } }

				public string EndMarker { get { return ptToken.EndMarker; } }

				public TokenType Type { get { return (TokenType)Enum.Parse(typeof(TokenType), ptToken.Type.ToString()); } }

				public object CoreToken { get { return ptToken; } }

				public string Text { get { return ptToken.Text; } }
			}
		}

		public IScriptureProviderBookSet BooksPresentSet
		{
			get
			{
				return new PT8BookSetWrapper(pt8Object.Settings.BooksPresentSet);
			}
			set { throw new NotImplementedException(); }
		}

		internal class PT8BookSetWrapper : IScriptureProviderBookSet
		{
			private BookSet ptBookSet;
			public PT8BookSetWrapper(BookSet getValue)
			{
				ptBookSet = getValue;
			}

			public IEnumerable<int> SelectedBookNumbers { get { return ptBookSet.SelectedBookNumbers; } }
		}

		public string Name { get { return pt8Object.Name; } set { pt8Object.Name = value; } }

		public ILexicalProject AssociatedLexicalProject
		{
			get { return new PT8LexicalProjectWrapper(pt8Object.Settings.AssociatedLexicalProject); }
			set { throw new NotImplementedException(); }
		}

		internal class PT8LexicalProjectWrapper : ILexicalProject
		{
			private AssociatedLexicalProject pt7Object;
			public PT8LexicalProjectWrapper(AssociatedLexicalProject project)
			{
				pt7Object = project;
			}

			public string ProjectType
			{
				get { return pt7Object.ApplicationType; }
			}

			public string ProjectId
			{
				get { return pt7Object.ProjectId; }
			}

			public override string ToString()
			{
				return string.Format("{0}:{1}", ProjectType, ProjectId);
			}
		}

		public ITranslationInfo TranslationInfo
		{
			get { return new PT8TranslationInfoWrapper(pt8Object.Settings.TranslationInfo); }
			set { throw new NotImplementedException(); }
		}

		internal class PT8TranslationInfoWrapper : ITranslationInfo
		{
			private TranslationInformation ptObject;

			public string BaseProjectName { get { return ptObject.BaseProjectName; } }

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
			get { return pt8Object.Settings.Editable; }
			set { pt8Object.Settings.Editable = value; }
		}

		public bool IsResourceText
		{
			get { return pt8Object.Settings.IsZippedResource; }
		}

		public string Directory
		{
			get { return pt8Object.Directory; }
		}

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
			get { return pt8Object.Settings.BooksPresentSet.Books; }
			set
			{
				pt8Object.Settings.BooksPresentSet.Books = value;
			}
		}

		public bool BookPresent(int bookCanonicalNum)
		{
			return pt8Object.BookPresent(bookCanonicalNum);
		}

		public bool IsCheckSumCurrent(int bookCanonicalNum, string checkSum)
		{
			return pt8Object.IsCheckSumCurrent(bookCanonicalNum, checkSum);
		}

		public IScrVerse Versification
		{
			get { return new Pt8VerseWrapper(pt8Object.Settings.Versification); }
		}

		public override string ToString()
		{
			return Name;
		}

		public string JoinedNameAndFullName { get { return pt8Object.JoinedNameAndFullName; } }

		public string FileNamePrePart { get { throw new NotImplementedException("Filename parts changed for PT8. Unnecessary perhaps?"); } }

		public string FileNameForm { get { throw new NotImplementedException("Filename parts changed for PT8. Unnecessary perhaps?"); } }

		public string FileNamePostPart { get { throw new NotImplementedException("Filename parts changed for PT8. Unnecessary perhaps?"); } }

		public object CoreScrText { get { return pt8Object; } }

		public string GetBookCheckSum(int canonicalNum)
		{
			return pt8Object.GetBookCheckSum(canonicalNum);
		}
	}
}