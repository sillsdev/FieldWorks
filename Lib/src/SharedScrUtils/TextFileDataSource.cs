using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace SILUBS.SharedScrUtils
{
	#region class TextFileDataSource
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// A class representing a file that can be parsed to find characters
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TextFileDataSource : IChecksDataSource
	{
		private string m_scrChecksDllFile;
		private string m_scrCheck;
		private CharacterCategorizer m_characterCategorizer;
		private List<ITextToken> m_tftList;
		private Dictionary<string, string> m_params;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TextFileDataSource"/> class.
		/// </summary>
		/// <param name="scrChecksDllFile">The DLL that contains the CharactersCheck class
		/// </param>
		/// <param name="scrCheck">Name of the scripture check to use</param>
		/// <param name="fileData">An array of strings with the lines of data from the file.
		/// </param>
		/// <param name="scrRefFormatString">Format string used to format scripture references.
		/// </param>
		/// ------------------------------------------------------------------------------------
		public TextFileDataSource(string scrChecksDllFile, string scrCheck, string[] fileData,
			string scrRefFormatString) :
			this(scrChecksDllFile, scrCheck, fileData, scrRefFormatString, null, null)
		{
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TextFileDataSource"/> class.
		/// </summary>
		/// <param name="scrChecksDllFile">The DLL that contains the CharactersCheck class</param>
		/// <param name="scrCheck">Name of the scripture check to use</param>
		/// <param name="fileData">An array of strings with the lines of data from the file.</param>
		/// <param name="scrRefFormatString">Format string used to format scripture references.</param>
		/// <param name="parameters">Checking parameters to send the check.</param>
		/// <param name="categorizer">The character categorizer.</param>
		/// --------------------------------------------------------------------------------
		public TextFileDataSource(string scrChecksDllFile, string scrCheck, string[] fileData,
			string scrRefFormatString, Dictionary<string, string> parameters,
			CharacterCategorizer categorizer)
		{
			m_scrChecksDllFile = scrChecksDllFile;
			m_scrCheck = scrCheck;
			m_characterCategorizer = (categorizer != null) ? categorizer : new CharacterCategorizer();
			m_params = parameters;
			m_tftList = new List<ITextToken>();
			int i = 1;
			foreach (string line in fileData)
				m_tftList.Add(new TextFileToken(line, i++, scrRefFormatString));
		}

		#region IChecksDataSource Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the books present (not supported).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<int> BooksPresent
		{
			get { throw new NotSupportedException(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the character categorizer.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public CharacterCategorizer CharacterCategorizer
		{
			get { return m_characterCategorizer; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the parameter value.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <returns>An empty string</returns>
		/// ------------------------------------------------------------------------------------
		public string GetParameterValue(string key)
		{
			string param;
			if (m_params != null && m_params.TryGetValue(key, out param))
				return param;

			return string.Empty;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the text (not supported).
		/// </summary>
		/// <param name="bookNum">The book num.</param>
		/// <param name="chapterNum">The chapter num.</param>
		/// ------------------------------------------------------------------------------------
		public bool GetText(int bookNum, int chapterNum)
		{
			throw new NotSupportedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Saves this instance (not supported).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Save()
		{
			throw new NotSupportedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the parameter value (not supported).
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="value">The value.</param>
		/// ------------------------------------------------------------------------------------
		public void SetParameterValue(string key, string value)
		{
			throw new NotSupportedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the text tokens.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IEnumerable<ITextToken> TextTokens()
		{
			return m_tftList;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string GetLocalizedString(string strToLocalize)
		{
			return strToLocalize;
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the references.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public List<TextTokenSubstring> GetReferences()
		{
			try
			{
				Assembly asm = Assembly.LoadFile(m_scrChecksDllFile);
				Type type = asm.GetType("SILUBS.ScriptureChecks." + m_scrCheck);
				IScrCheckInventory scrCharInventoryBldr =
					Activator.CreateInstance(type, this) as IScrCheckInventory;

				return scrCharInventoryBldr.GetReferences(m_tftList, string.Empty);
			}
			catch
			{
				return null;
			}
		}
	}

	#endregion

	#region TextFileToken class
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Text token object used for reading any, nondescript text file in order to discover
	/// all the characters therein.
	/// </summary>
	/// ------------------------------------------------------------------------------------
	internal class TextFileToken : ITextToken
	{
		private string m_text;
		private int m_iLine;
		private string m_scrRefFmtString;

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TextFileToken"/> class.
		/// </summary>
		/// --------------------------------------------------------------------------------
		internal TextFileToken(string text, int iLine, string scrRefFormatString)
		{
			m_text = text;
			m_iLine = iLine;
			m_scrRefFmtString = scrRefFormatString;
		}

		#region ITextToken Members
		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Not used.
		/// </summary>
		/// --------------------------------------------------------------------------------
		public bool IsNoteStart
		{
			get { return false; }
		}

		/// --------------------------------------------------------------------------------------------
		/// <summary>
		/// Not used.
		/// </summary>
		/// --------------------------------------------------------------------------------------------
		public bool IsParagraphStart
		{
			get { return true; }
		}

		/// --------------------------------------------------------------------------------------------
		/// <summary>
		/// Not used.
		/// </summary>
		/// --------------------------------------------------------------------------------------------
		public string Locale
		{
			get { return null; }
		}

		/// --------------------------------------------------------------------------------------------
		/// <summary>
		/// Not used.
		/// </summary>
		/// --------------------------------------------------------------------------------------------
		public string ScrRefString
		{
			get { return string.Format(m_scrRefFmtString, m_iLine); }
			set { ; }
		}

		/// --------------------------------------------------------------------------------------------
		/// <summary>
		/// Not used.
		/// </summary>
		/// --------------------------------------------------------------------------------------------
		public string ParaStyleName
		{
			get { return null; }
		}

		/// --------------------------------------------------------------------------------------------
		/// <summary>
		/// Not used.
		/// </summary>
		/// --------------------------------------------------------------------------------------------
		public string CharStyleName
		{
			get { return null; }
		}

		/// --------------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the text.
		/// </summary>
		/// --------------------------------------------------------------------------------------------
		public string Text
		{
			get { return m_text; }
		}

		/// --------------------------------------------------------------------------------------------
		/// <summary>
		/// Force the check to treat the text like verse text.
		/// </summary>
		/// --------------------------------------------------------------------------------------------
		public TextType TextType
		{
			get { return TextType.Verse; }
		}

		/// --------------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// --------------------------------------------------------------------------------------------
		public BCVRef MissingEndRef
		{
			get { return null; }
			set { ; }
		}

		/// --------------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// --------------------------------------------------------------------------------------------
		public BCVRef MissingStartRef
		{
			get { return null; }
			set { ; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes a deep copy of the specified text token.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITextToken Clone()
		{
			return new TextFileToken(m_text, m_iLine, m_scrRefFmtString);
		}
		#endregion
	}

	#endregion
}
