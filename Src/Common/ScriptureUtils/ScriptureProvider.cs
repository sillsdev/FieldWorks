// Copyright (c) 2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using Paratext;
using Paratext.LexicalClient;

namespace SIL.FieldWorks.Common.ScriptureUtils
{
	/// <summary>
	/// This class is a wrapper for the different supported providers of scripture, either Paratext 7 or Paratext 8.1 at the moment.
	/// </summary>
	public class ScriptureProvider
	{
		private enum ProviderVersion
		{
			Paratext7,
			Paratext8
		}

		private static ProviderVersion Version;

		/// <summary>
		/// Determine if Paratext8 is installed, if it is use it, otherwise fall back to Paratext7
		/// </summary>
		static ScriptureProvider()
		{
			Version = ProviderVersion.Paratext7;
		}

		/// <summary/>
		public static IScrText MakeScrText()
		{
			return new PTScrTextWrapper(new ScrText());
		}

		/// <summary/>
		public static string SettingsDirectory
		{
			get
			{
				switch (Version)
				{
					case ProviderVersion.Paratext7:
						return ScrTextCollection.SettingsDirectory;
				}
				return null;
			}
		}

		/// <summary/>
		public static IEnumerable<string> SLTTexts
		{
			get
			{
				switch (Version)
				{
					case ProviderVersion.Paratext7:
						return ScrTextCollection.SLTTexts;
				}
				return null;
			}
		}

		/// <summary/>
		public static IEnumerable<string> ScrTextNames
		{
			get
			{
				switch (Version)
				{
					case ProviderVersion.Paratext7:
						return ScrTextCollection.ScrTextNames;
				}
				return null;
			}
		}

		/// <summary/>
		public static void Initialize(string paratextSettingsDirectory, bool b)
		{
			switch (Version)
			{
				case ProviderVersion.Paratext7:
					ScrTextCollection.Initialize(paratextSettingsDirectory, b);
					break;
			}
		}

		/// <summary/>
		public static void RefreshScrTexts()
		{
			switch (Version)
			{
				case ProviderVersion.Paratext7:
					ScrTextCollection.RefreshScrTexts();
					break;
			}
		}

		/// <summary/>
		public static IEnumerable<IScrText> ScrTexts()
		{

			switch (Version)
			{
				case ProviderVersion.Paratext7:
					return WrapPt7Collection(ScrTextCollection.ScrTexts(true, true),
						new Func<ScrText, IScrText>(ptText => new PTScrTextWrapper(ptText)));
			}
			return new IScrText[] { };
		}

		internal static IEnumerable<T> WrapPt7Collection<T, T2>(IEnumerable<T2> pt7Collection, Func<T2, T> wrapFunction)
		{
			var returnCollection = new List<T>();
			foreach (var pt7obj in pt7Collection)
			{
				returnCollection.Add(wrapFunction.Invoke(pt7obj));
			}
			return returnCollection;
		}

		/// <summary/>
		public static IScrText Get(string project)
		{
			return new PTScrTextWrapper(ScrTextCollection.Get(project));
		}

		/// <summary/>
		public static IVerseRef MakeVerseRef(int bookNum, int i, int i1)
		{
			return new VerseRefWrapper(new VerseRef(bookNum, i, i1));
		}
	}

	internal class VerseRefWrapper : IVerseRef
	{
		private VerseRef pt7VerseRef;
		public VerseRefWrapper(VerseRef verseRef)
		{
			pt7VerseRef = verseRef;
		}

		public object CoreVerseRef { get { return pt7VerseRef;} }
	}

	internal class PTScrTextWrapper : IScrText
	{
		private ScrText pt7Object;
		public PTScrTextWrapper(ScrText text)
		{
			pt7Object = text;
		}

		public void Reload()
		{
			pt7Object.Reload();
		}

		public IScriptureProviderStyleSheet DefaultStylesheet
		{
			get
			{
				return new PT7StyleSheetWrapper(pt7Object.DefaultStylesheet);
			}
		}

		internal class PT7StyleSheetWrapper : IScriptureProviderStyleSheet
		{
			private ScrStylesheet pt7StyleSheet;
			public PT7StyleSheetWrapper(ScrStylesheet pt7ObjectDefaultStylesheet)
			{
				pt7StyleSheet = pt7ObjectDefaultStylesheet;
			}

			public IEnumerable<ITag> Tags
			{
				get { return ScriptureProvider.WrapPt7Collection(pt7StyleSheet.Tags, new Func<ScrTag, ITag>(tag => new PT7TagWrapper(tag))); }
			}
		}

		public IScriptureProviderParser Parser
		{
			get { return new Pt7ParserWrapper(pt7Object.Parser); }
		}

		public IScriptureProviderBookSet BooksPresentSet
		{
			get
			{
				return new PTBookSetWrapper(pt7Object.BooksPresentSet);
			}
			set { throw new NotImplementedException(); }
		}

		internal class PTBookSetWrapper : IScriptureProviderBookSet
		{
			private BookSet pt7BookSet;
			public PTBookSetWrapper(BookSet getValue)
			{
				pt7BookSet = getValue;
			}

			public IEnumerable<int> SelectedBookNumbers { get { return pt7BookSet.SelectedBookNumbers; } }
		}

		public string Name { get { return pt7Object.Name; } set { pt7Object.Name = value; } }

		public ILexicalProject AssociatedLexicalProject
		{
			get { return new PTLexicalProjectWrapper(pt7Object.AssociatedLexicalProject); }
			set { throw new NotImplementedException(); }
		}

		public ITranslationInfo TranslationInfo
		{
			get { return new PTTranslationInfoWrapper(pt7Object.TranslationInfo); }
			set { throw new NotImplementedException(); }
		}

		public bool Editable
		{
			get { return pt7Object.Editable; }
			set { pt7Object.Editable = value; }
		}

		public bool IsResourceText
		{
			get { return pt7Object.IsResourceText; }
		}

		public string Directory
		{
			get { return pt7Object.Directory; }
			set { pt7Object.Directory = value; }
		}

		/// <summary>
		/// For unit tests only. FLEx is not responsible for disposing of IScrText objects received from ParaText
		/// </summary>
		internal void DisposePTObject()
		{
			if(pt7Object != null)
				pt7Object.Dispose();
			pt7Object = null;
		}

		public void SetParameterValue(string resourcetext, string s)
		{
			pt7Object.SetParameterValue(resourcetext, s);
		}

		public string BooksPresent
		{
			get { return pt7Object.BooksPresent; }
			set
			{
				pt7Object.BooksPresent = value;
			}
		}

		public bool BookPresent(int bookCanonicalNum)
		{
			return pt7Object.BookPresent(bookCanonicalNum);
		}

		public bool IsCheckSumCurrent(int bookCanonicalNum, string checkSum)
		{
			return pt7Object.IsCheckSumCurrent(bookCanonicalNum, checkSum);
		}

		public IScrVerse Versification
		{
			get { return new Pt7VerseWrapper(pt7Object.Versification); }
		}

		public override string ToString()
		{
			return Name;
		}

		public string JoinedNameAndFullName { get { return pt7Object.JoinedNameAndFullName; } }
	}

	internal class Pt7VerseWrapper : IScrVerse
	{
		private ScrVers pt7Versification;
		public Pt7VerseWrapper(ScrVers versification)
		{
			pt7Versification = versification;
		}

		public int LastChapter(int bookNum)
		{
			return pt7Versification.LastChapter(bookNum);
		}

		public int LastVerse(int bookNum, int chapter)
		{
			return pt7Versification.LastVerse(bookNum, chapter);
		}
	}

	/// <summary/>
	public interface IScrVerse
	{
		/// <summary/>
		int LastChapter(int bookNum);

		/// <summary/>
		int LastVerse(int bookNum, int chapter);
	}

	internal class Pt7ParserWrapper : IScriptureProviderParser
	{
		private ScrParser pt7Parser;
		public Pt7ParserWrapper(ScrParser parser)
		{
			pt7Parser = parser;
		}

		public IEnumerable<IUsfmToken> GetUsfmTokens(IVerseRef verseRef, bool b, bool b1)
		{
			return ScriptureProvider.WrapPt7Collection(pt7Parser.GetUsfmTokens((VerseRef)verseRef.CoreVerseRef, b, b1),
				new Func<UsfmToken, IUsfmToken>(token => new PT7TokenWrapper(token)));
		}
	}

	internal class PT7TokenWrapper : IUsfmToken
	{
		private UsfmToken pt7Token;
		public PT7TokenWrapper(UsfmToken token)
		{
			pt7Token = token;
		}

		public string Marker { get {return pt7Token.Marker; } }

		public string EndMarker { get { return pt7Token.EndMarker; } }
	}

	internal class PT7TagWrapper : ITag
	{
		private ScrTag pt7Tag;
		public PT7TagWrapper(ScrTag tag)
		{
			pt7Tag = tag;
		}

		public string Marker { get { return pt7Tag.Marker; } set { pt7Tag.Marker = value; } }
		public string Endmarker { get { return pt7Tag.Endmarker; } set { pt7Tag.Endmarker = value; } }

		public ScrStyleType StyleType
		{
			get { return (ScrStyleType)Enum.Parse(typeof(ScrStyleType), pt7Tag.StyleType.ToString()); }
			set { pt7Tag.StyleType = (Paratext.ScrStyleType)Enum.Parse(typeof(Paratext.ScrStyleType), pt7Tag.StyleType.ToString()); }
		}
	}

	internal class PTLexicalProjectWrapper : ILexicalProject
	{
		private AssociatedLexicalProject pt7Object;

		public PTLexicalProjectWrapper(AssociatedLexicalProject project)
		{
			pt7Object = project;
		}

		public string ProjectType
		{
			get { return pt7Object.ApplicationType.ToString(); }
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


	internal class PTTranslationInfoWrapper : ITranslationInfo
	{
		private TranslationInformation pt7Object;

		public PTTranslationInfoWrapper(TranslationInformation translationInfo)
		{
			pt7Object = translationInfo;
		}

		public string BaseProjectName { get { return pt7Object.BaseProjectName; } }
		public ProjectType Type { get { return (ProjectType)Enum.Parse(typeof(ProjectType), pt7Object.Type.ToString()); } }
	}
}