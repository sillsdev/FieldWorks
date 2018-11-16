// Copyright (c) 2009-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Reflection;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Core.Scripture;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// A class representing a file that can be parsed to find characters
	/// </summary>
	public class TextFileDataSource : IChecksDataSource
	{
		private string m_scrChecksDllFile;
		private string m_scrCheck;
		private List<ITextToken> m_tftList;
		private Dictionary<string, string> m_params;

		/// <summary />
		/// <param name="scrChecksDllFile">The DLL that contains the CharactersCheck class</param>
		/// <param name="scrCheck">Name of the scripture check to use</param>
		/// <param name="fileData">An array of strings with the lines of data from the file.</param>
		/// <param name="scrRefFormatString">Format string used to format scripture references.</param>
		/// <param name="parameters">Checking parameters to send the check.</param>
		/// <param name="categorizer">The character categorizer.</param>
		public TextFileDataSource(string scrChecksDllFile, string scrCheck, string[] fileData, string scrRefFormatString, Dictionary<string, string> parameters, CharacterCategorizer categorizer)
		{
			m_scrChecksDllFile = scrChecksDllFile;
			m_scrCheck = scrCheck;
			CharacterCategorizer = (categorizer != null) ? categorizer : new CharacterCategorizer();
			m_params = parameters;
			m_tftList = new List<ITextToken>();
			var i = 1;
			foreach (string line in fileData)
			{
				m_tftList.Add(new TextFileToken(line, i++, scrRefFormatString));
			}
		}

		#region IChecksDataSource Members
		/// <summary>
		/// Gets the books present (not supported).
		/// </summary>
		public List<int> BooksPresent
		{
			get { throw new NotSupportedException(); }
		}

		/// <summary>
		/// Gets the character categorizer.
		/// </summary>
		public CharacterCategorizer CharacterCategorizer { get; }

		/// <summary>
		/// Gets the parameter value.
		/// </summary>
		public string GetParameterValue(string key)
		{
			string param;
			return m_params != null && m_params.TryGetValue(key, out param) ? param : string.Empty;
		}

		/// <summary>
		/// Gets the text (not supported).
		/// </summary>
		public bool GetText(int bookNum, int chapterNum)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Saves this instance (not supported).
		/// </summary>
		public void Save()
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Sets the parameter value (not supported).
		/// </summary>
		public void SetParameterValue(string key, string value)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Gets the text tokens.
		/// </summary>
		public IEnumerable<ITextToken> TextTokens => m_tftList;

		/// <summary />
		public string GetLocalizedString(string strToLocalize)
		{
			return strToLocalize;
		}

		#endregion

		/// <summary>
		/// Gets the references.
		/// </summary>
		public List<TextTokenSubstring> GetReferences()
		{
			try
			{
				var asm = Assembly.LoadFile(m_scrChecksDllFile);
				var type = asm.GetType("SIL.FieldWorks.Common.FwUtils." + m_scrCheck);
				var scrCharInventoryBldr = Activator.CreateInstance(type, this) as IScrCheckInventory;

				return scrCharInventoryBldr.GetReferences(m_tftList, string.Empty);
			}
			catch
			{
				return null;
			}
		}

		/// <summary>
		/// Text token object used for reading any, nondescript text file in order to discover
		/// all the characters therein.
		/// </summary>
		private sealed class TextFileToken : ITextToken
		{
			private int m_iLine;
			private string m_scrRefFmtString;

			/// <summary />
			internal TextFileToken(string text, int iLine, string scrRefFormatString)
			{
				Text = text;
				m_iLine = iLine;
				m_scrRefFmtString = scrRefFormatString;
			}

			#region ITextToken Members

			/// <summary>
			/// Not used.
			/// </summary>
			public bool IsNoteStart => false;

			/// <summary>
			/// Not used.
			/// </summary>
			public bool IsParagraphStart => true;

			/// <summary>
			/// Not used.
			/// </summary>
			public string Locale => null;

			/// <summary>
			/// Not used.
			/// </summary>
			public string ScrRefString
			{
				get { return string.Format(m_scrRefFmtString, m_iLine); }
				set { }
			}

			/// <summary>
			/// Not used.
			/// </summary>
			public string ParaStyleName => null;

			/// <summary>
			/// Not used.
			/// </summary>
			public string CharStyleName => null;

			/// <summary>
			/// Gets the text.
			/// </summary>
			public string Text { get; }

			/// <summary>
			/// Force the check to treat the text like verse text.
			/// </summary>
			public TextType TextType => TextType.Verse;

			/// <summary />
			public BCVRef MissingEndRef
			{
				get { return null; }
				set { }
			}

			/// <summary />
			public BCVRef MissingStartRef
			{
				get { return null; }
				set { }
			}

			/// <summary>
			/// Makes a deep copy of the specified text token.
			/// </summary>
			public ITextToken Clone()
			{
				return new TextFileToken(Text, m_iLine, m_scrRefFmtString);
			}
			#endregion
		}
	}
}