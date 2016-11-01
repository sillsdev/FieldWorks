// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.Utils;
using SIL.FieldWorks.FDO;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwKernelInterfaces;

namespace SIL.FieldWorks.Filters
{
	/// <summary>
	/// A BadSpellingMatcher
	/// </summary>
	public class BadSpellingMatcher : BaseMatcher
	{
		int m_ws;

		/// <summary>
		/// Required constructor for persistence.
		/// </summary>
		public BadSpellingMatcher()
		{
		}

		public BadSpellingMatcher(int ws)
		{
			m_ws = ws;
		}

		/// <summary>
		/// Succeed if some word in the argument is mis-spelled.
		/// </summary>
		/// <param name="arg"></param>
		/// <returns></returns>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="GetDictionary() returns a reference")]
		public override bool Matches(ITsString arg)
		{
			ISpellEngine dict = SpellingHelper.GetSpellChecker(m_ws, WritingSystemFactory);
			return new SpellCheckMethod(arg, dict, WritingSystemFactory.get_EngineOrNull(m_ws)).Run();
		}

		/// <summary>
		/// Same if it checks for the same writing system.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public override bool SameMatcher(IMatcher other)
		{
			if (other is BadSpellingMatcher)
				return m_ws == (other as BadSpellingMatcher).m_ws;
			return false;
		}

		/// ---------------------------------------------------------------------------------------
		/// <summary>
		/// Persists as XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ---------------------------------------------------------------------------------------
		public override void PersistAsXml(XmlNode node)
		{
			base.PersistAsXml(node);
			XmlUtils.AppendAttribute(node, "ws", m_ws.ToString());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inits the XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------
		public override void InitXml(XmlNode node)
		{
			base.InitXml(node);
			m_ws = XmlUtils.GetMandatoryIntegerAttributeValue(node, "ws");
		}

		/// <summary>
		/// I think this usually gets called after InitXml. In either order, we should wind up with
		/// a dictionary.
		/// </summary>
		public override FdoCache Cache
		{
			set
			{
				base.Cache = value;
				Debug.Assert(m_ws != 0); // method should be called after InitXml.
			}
		}
	}

	/// <summary>
	/// Very like the class of the same name in VwTextBoxes.cpp
	/// </summary>
	internal class SpellCheckMethod
	{
		private readonly ITsString m_tss; // main string to check.
		private readonly string m_text; // to check, text of m_tss.
		private readonly int m_cch; // total count of characters in source.
		private readonly ISpellEngine m_dict;
		private readonly ILgWritingSystem m_ws; // only text in this language is checked.

		/// <summary>
		/// Make one
		/// </summary>
		/// <param name="tss"></param>
		/// <param name="dict"></param>
		/// <param name="ws"></param>
		public SpellCheckMethod(ITsString tss, ISpellEngine dict, ILgWritingSystem ws)
		{
			m_tss = tss;
			m_text = tss.Text ?? string.Empty;
			m_cch = m_text.Length;
			m_dict = dict;
			m_ws = ws;
		}

		/// <summary>
		/// Run the method, return true if match (i.e., found a spelling error).
		/// </summary>
		/// <returns></returns>
		public bool Run()
		{
			//if we have no valid dictionary then all the words must be spelled right?
			if (m_dict == null)
				return false;
			int ichMinWord = 0;
			bool fInWord = false;
			for (int ich = 0; ich < m_cch; ich++)
			{
				bool isWordForming = m_ws.get_IsWordForming(m_text[ich]);
				if (isWordForming)
				{
					if (!fInWord)
					{
						fInWord = true;
						ichMinWord = ich;
					}
				}
				else
				{
					if (fInWord)
					{
						if (CheckWord(ichMinWord, ich))
							return true;
						fInWord = false;
					}
				}
			}
			if (fInWord)
				return CheckWord(ichMinWord, m_cch);
			return false;
		}

		// Return true if a spelling error occurs in the word that is the specified substring of m_text.
		bool CheckWord(int ichMinWord, int ichLimWord)
		{
			string word = TsStringUtils.NormalizeToNFC(m_text.Substring(ichMinWord, ichLimWord - ichMinWord));
			TsRunInfo tri;
			ITsTextProps props = m_tss.FetchRunInfoAt(ichMinWord, out tri);
			int var;
			int ws = props.GetIntPropValues((int)FwTextPropType.ktptWs, out var);
			bool fFoundOurWs = ws == m_ws.Handle;
			bool fFoundOtherWs = ws != m_ws.Handle;

			while (tri.ichLim < ichLimWord)
			{
				props = m_tss.FetchRunInfoAt(tri.ichLim, out tri);
				ws = props.GetIntPropValues((int)FwTextPropType.ktptWs, out var);
				fFoundOurWs |= ws == m_ws.Handle;
				fFoundOtherWs |= ws != m_ws.Handle;
			}
			if (!fFoundOurWs)
				return false; // don't check words with nothing in interesting WS.
			if (fFoundOtherWs)
				return true; // mixed writing system in a 'word' always counts as a 'spelling' error.
			return !m_dict.Check(word); // succeed if check fails!
		}
	}
}
