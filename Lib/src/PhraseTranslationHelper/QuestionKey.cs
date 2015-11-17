// ---------------------------------------------------------------------------------------------
// Copyright (c) 2012-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: QuestionKey.cs
// ---------------------------------------------------------------------------------------------

using System;
using System.Text;
using System.Xml.Schema;
using System.Xml.Serialization;
using SILUBS.SharedScrUtils;

namespace SILUBS.PhraseTranslationHelper
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Simple base class to encapsulate the information needed to (more-or-less uniquely)
	/// identify a Scripture checking question.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public abstract class QuestionKey
	{
		private string m_text;

		[XmlAttribute("scrref")]
		public abstract string ScriptureReference { get; set; }
		[XmlAttribute("startref")]
		public abstract int StartRef { get; set; }
		[XmlAttribute("endref")]
		public abstract int EndRef { get; set; }
		/// <summary>Text of then question in COMPOSED form</summary>
		[XmlElement("Q", Form = XmlSchemaForm.Unqualified)]
		public virtual string Text
		{
			get { return m_text; }
			set { m_text = value.Normalize(NormalizationForm.FormC); }
		}

		public bool Matches(QuestionKey other)
		{
			return StartRef == other.StartRef && EndRef == other.EndRef && Text == other.Text;
		}

		public bool Matches(string sRef, string questionText)
		{
			return ScriptureReference == sRef && Text == questionText;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a <see cref="System.String"/> that represents this instance.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return ScriptureReference + "-" + Text;
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Trivial implementation of QuestionKey for a question that is not pegged to a particular
	/// Scripture reference
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class SimpleQuestionKey : QuestionKey
	{
		private static readonly int s_startRef;
		private static readonly int s_endRef;

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the <see cref="SimpleQuestionKey"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static SimpleQuestionKey()
		{
			s_startRef = ScrReference.StartOfBible(ScrVers.English).BBCCCVVV;
			s_endRef = ScrReference.EndOfBible(ScrVers.English).BBCCCVVV;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SimpleQuestionKey(string text)
		{
			Text = text;
		}
		#endregion

		#region Overrides of QuestionKey
		public override string ScriptureReference
		{
			get { return string.Empty; }
			set { throw new NotImplementedException(); }
		}

		public override int StartRef
		{
			get { return s_startRef; }
			set { throw new NotImplementedException(); }
		}

		public override int EndRef
		{
			get { return s_endRef;}
			set { throw new NotImplementedException(); }
		}
		#endregion
	}
}
