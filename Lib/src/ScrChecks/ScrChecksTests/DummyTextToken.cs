using System;
using System.Collections.Generic;
using System.Text;
using SILUBS.SharedScrUtils;

namespace SILUBS.ScriptureChecks
{
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// See ITextToken for field definitions comments.
	/// This is a dummy class used for testing the checks.
	/// </summary>
	/// ------------------------------------------------------------------------------------
	public class DummyTextToken : ITextToken
	{
		private string m_text;
		private TextType m_textType;
		private bool m_isParagraphStart;
		private bool m_isNoteStart;
		private string m_paraStyleName;
		private string m_charStyleName;
		private string m_iculocale = null;
		private BCVRef m_missingStartRef;
		private BCVRef m_missingEndRef;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DummyTextToken"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DummyTextToken(string text)
		{
			m_text = text;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DummyTextToken"/> class.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <param name="textType">Type of the text.</param>
		/// <param name="isParagraphStart">if set to <c>true</c> text token starts a paragraph.
		/// </param>
		/// <param name="isNoteStart">if set to <c>true</c> text token starts a note.</param>
		/// <param name="paraStyleName">Name of the paragraph style.</param>
		/// ------------------------------------------------------------------------------------
		public DummyTextToken(string text, TextType textType, bool isParagraphStart,
			bool isNoteStart, string paraStyleName) : this(text, textType, isParagraphStart,
			isNoteStart, paraStyleName, string.Empty, null)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DummyTextToken"/> class.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <param name="textType">Type of the text.</param>
		/// <param name="isParagraphStart">if set to <c>true</c> text token starts a paragraph.
		/// </param>
		/// <param name="isNoteStart">if set to <c>true</c> text token starts a note.</param>
		/// <param name="paraStyleName">Name of the paragraph style.</param>
		/// <param name="charStyleName">Name of the character style.</param>
		/// ------------------------------------------------------------------------------------
		public DummyTextToken(string text, TextType textType, bool isParagraphStart,
			bool isNoteStart, string paraStyleName, string charStyleName)
			: this(text, textType, isParagraphStart,
				isNoteStart, paraStyleName, charStyleName, null)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DummyTextToken"/> class.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <param name="textType">Type of the text.</param>
		/// <param name="isParagraphStart">if set to <c>true</c> text token starts a paragraph.
		/// </param>
		/// <param name="isNoteStart">if set to <c>true</c> text token starts a note.</param>
		/// <param name="paraStyleName">Name of the paragraph style.</param>
		/// <param name="charStyleName">Name of the character style.</param>
		/// <param name="icuLocale">The icu locale.</param>
		/// ------------------------------------------------------------------------------------
		public DummyTextToken(string text, TextType textType, bool isParagraphStart,
			bool isNoteStart, string paraStyleName, string charStyleName, string icuLocale)
		{
			m_text = text;
			m_textType = textType;
			m_isParagraphStart = isParagraphStart;
			m_isNoteStart = isNoteStart;
			m_paraStyleName = paraStyleName;
			m_charStyleName = charStyleName;
			m_iculocale = icuLocale;
		}

		#region ITextToken Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the locale.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Locale
		{
			get { return m_iculocale; }
			set { m_iculocale = value;  }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the type of the text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public TextType TextType
		{
			get { return m_textType; }
			set { m_textType = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether this instance is note start.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsNoteStart
		{
			get { return m_isNoteStart; }
			set { m_isNoteStart = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether this instance is paragraph start.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsParagraphStart
		{
			get { return m_isParagraphStart; }
			set { m_isParagraphStart = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the name of the paragraph style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ParaStyleName
		{
			get { return m_paraStyleName; }
			set { m_paraStyleName = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the name of the character style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string CharStyleName
		{
			get { return m_charStyleName; }
			set { m_charStyleName = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Text
		{
			get { return m_text; }
			set { m_text = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the Scripture reference as a string, suitable for displaying in the UI
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ScrRefString
		{
			get { return string.Empty; }
			set { ; }
		}

		public BCVRef MissingEndRef
		{
			get { return m_missingEndRef; }
			set { m_missingEndRef = value; }
		}

		public BCVRef MissingStartRef
		{
			get { return m_missingStartRef; }
			set { m_missingStartRef = value; }
		}

		public override string ToString()
		{
			return Text;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes a deep copy of this text token.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITextToken Clone()
		{
			DummyTextToken copy = new DummyTextToken(Text, TextType, IsParagraphStart,
				IsNoteStart, ParaStyleName, CharStyleName, Locale);
			copy.m_missingStartRef = m_missingStartRef;
			copy.m_missingEndRef = m_missingEndRef;
			return copy;
		}
		#endregion
	}
}
