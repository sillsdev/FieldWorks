// Copyright (c) 2008-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.Xml.Linq;
using SIL.LCModel;
using SIL.LCModel.Core.SpellChecking;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.Xml;

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
		public override bool Matches(ITsString arg)
		{
			var dict = SpellingHelper.GetSpellChecker(m_ws, WritingSystemFactory);
			return new SpellCheckMethod(arg, dict, WritingSystemFactory.get_EngineOrNull(m_ws)).Run();
		}

		/// <summary>
		/// Same if it checks for the same writing system.
		/// </summary>
		public override bool SameMatcher(IMatcher other)
		{
			return m_ws == (other as BadSpellingMatcher)?.m_ws;
		}

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public override void PersistAsXml(XElement element)
		{
			base.PersistAsXml(element);
			XmlUtils.SetAttribute(element, "ws", m_ws.ToString());
		}

		/// <summary>
		/// Inits the XML.
		/// </summary>
		public override void InitXml(XElement element)
		{
			base.InitXml(element);
			m_ws = XmlUtils.GetMandatoryIntegerAttributeValue(element, "ws");
		}

		/// <summary>
		/// I think this usually gets called after InitXml. In either order, we should wind up with
		/// a dictionary.
		/// </summary>
		public override LcmCache Cache
		{
			set
			{
				base.Cache = value;
				Debug.Assert(m_ws != 0); // method should be called after InitXml.
			}
		}

		/// <summary>
		/// Very like the class of the same name in VwTextBoxes.cpp
		/// </summary>
		private sealed class SpellCheckMethod
		{
			private readonly ITsString m_tss; // main string to check.
			private readonly string m_text; // to check, text of m_tss.
			private readonly int m_cch; // total count of characters in source.
			private readonly ISpellEngine m_dict;
			private readonly ILgWritingSystem m_ws; // only text in this language is checked.

			/// <summary>
			/// Make one
			/// </summary>
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
			public bool Run()
			{
				//if we have no valid dictionary then all the words must be spelled right?
				if (m_dict == null)
				{
					return false;
				}
				var ichMinWord = 0;
				var fInWord = false;
				for (var ich = 0; ich < m_cch; ich++)
				{
					var isWordForming = m_ws.get_IsWordForming(m_text[ich]);
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
							{
								return true;
							}
							fInWord = false;
						}
					}
				}
				return fInWord && CheckWord(ichMinWord, m_cch);
			}

			// Return true if a spelling error occurs in the word that is the specified substring of m_text.
			private bool CheckWord(int ichMinWord, int ichLimWord)
			{
				var word = TsStringUtils.NormalizeToNFC(m_text.Substring(ichMinWord, ichLimWord - ichMinWord));
				TsRunInfo tri;
				var props = m_tss.FetchRunInfoAt(ichMinWord, out tri);
				int var;
				var ws = props.GetIntPropValues((int)FwTextPropType.ktptWs, out var);
				var fFoundOurWs = ws == m_ws.Handle;
				var fFoundOtherWs = ws != m_ws.Handle;

				while (tri.ichLim < ichLimWord)
				{
					props = m_tss.FetchRunInfoAt(tri.ichLim, out tri);
					ws = props.GetIntPropValues((int)FwTextPropType.ktptWs, out var);
					fFoundOurWs |= ws == m_ws.Handle;
					fFoundOtherWs |= ws != m_ws.Handle;
				}
				if (!fFoundOurWs)
				{
					return false; // don't check words with nothing in interesting WS.
				}
				if (fFoundOtherWs)
				{
					return true; // mixed writing system in a 'word' always counts as a 'spelling' error.
				}
				return !m_dict.Check(word); // succeed if check fails!
			}
		}
	}
}
