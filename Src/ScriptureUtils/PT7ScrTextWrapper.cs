// Copyright (c) 2017-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;

namespace SIL.FieldWorks.Common.ScriptureUtils
{
	internal class PT7ScrTextWrapper : IScrText
	{
		private Paratext.ScrText pt7Object;
		public PT7ScrTextWrapper(Paratext.ScrText text)
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
			private Paratext.ScrStylesheet pt7StyleSheet;
			public PT7StyleSheetWrapper(Paratext.ScrStylesheet pt7ObjectDefaultStylesheet)
			{
				pt7StyleSheet = pt7ObjectDefaultStylesheet;
			}

			public IEnumerable<ITag> Tags
			{
				get { return ScriptureProvider.WrapPtCollection(pt7StyleSheet.Tags, new Func<Paratext.ScrTag, ITag>(tag => new PT7TagWrapper(tag))); }
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
				return new PT7BookSetWrapper(pt7Object.BooksPresentSet);
			}
			set { throw new NotImplementedException(); }
		}

		internal class PT7BookSetWrapper : IScriptureProviderBookSet
		{
			private Paratext.BookSet pt7BookSet;
			public PT7BookSetWrapper(Paratext.BookSet getValue)
			{
				pt7BookSet = getValue;
			}

			public IEnumerable<int> SelectedBookNumbers { get { return pt7BookSet.SelectedBookNumbers; } }
		}

		public string Name { get { return pt7Object.Name; } set { pt7Object.Name = value; } }

		public ILexicalProject AssociatedLexicalProject
		{
			get { return new PT7LexicalProjectWrapper(pt7Object.AssociatedLexicalProject); }
			set { throw new NotImplementedException(); }
		}

		public ITranslationInfo TranslationInfo
		{
			get { return new PT7TranslationInfoWrapper(pt7Object.TranslationInfo); }
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

		public string FileNamePrePart { get { return pt7Object.FileNamePrePart; } }

		public string FileNameForm { get { return pt7Object.FileNameForm; } }

		public string FileNamePostPart { get { return pt7Object.FileNamePostPart; } }

		public object CoreScrText { get { return pt7Object; } }

		public string GetBookCheckSum(int canonicalNum)
		{
			return pt7Object.GetBookCheckSum(canonicalNum);
		}
	}
}