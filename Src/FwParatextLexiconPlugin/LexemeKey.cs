// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using Paratext.LexicalContracts;
using SIL.LCModel;

namespace SIL.FieldWorks.ParatextLexiconPlugin
{
	internal class LexemeKey
	{
		private static readonly Regex s_idRegex = new Regex("^(\\w+):(.*?)(:\\d+)?$", RegexOptions.Compiled);

		private readonly LexemeType m_type;
		private readonly string m_lexicalForm;
		private readonly int m_homograph;
		private Guid m_guid;

		public bool IsGuidKey => m_guid != Guid.Empty;

		public LexemeKey(LexemeType type, string lexicalForm) : this(type, lexicalForm, 1)
		{
		}

		public LexemeKey(LexemeType type, Guid id, LcmCache cache)
		{
			m_type = type;
			m_guid = id;
			m_lexicalForm = GetLexicalFormFromObject(type, id, cache);
			m_homograph = GetHomographFromObject(id, cache);
		}

		public LexemeKey(LexemeType type, string lexicalForm, int homograph)
		{
			Debug.Assert(lexicalForm.IsNormalized(), "Key lexical forms should always be in composed form");

			m_type = type;
			m_lexicalForm = lexicalForm;
			m_homograph = homograph;
		}

		public LexemeKey(string id)
		{
			Match match = s_idRegex.Match(id);
			Debug.Assert(match.Groups[2].Value.IsNormalized(), "Key lexical forms should always be in composed form");
			m_type = (LexemeType)Enum.Parse(typeof(LexemeType), match.Groups[1].Value);
			m_lexicalForm = match.Groups[2].Value;
			m_homograph = match.Groups[3].Length > 0 ? Int32.Parse(match.Groups[3].Value.Substring(1), CultureInfo.InvariantCulture) : 1;
		}

		/// <summary>
		/// Unique string identifier of the lexeme string. Unique within this lexicon type.
		/// </summary>
		public string Id
		{
			get
			{
				// Version 2 interface
				if (m_guid != Guid.Empty)
				{
					return m_guid.ToString();
				}
				// Version 1 interface
				if (m_homograph == 1)
					return string.Format("{0}:{1}", Type, LexicalForm);

				return string.Format("{0}:{1}:{2}", Type, LexicalForm, m_homograph);
			}
		}

		public LexemeType Type
		{
			get { return m_type; }
		}

		public string LexicalForm => m_lexicalForm;

		// get the undecorated lexical form for the object type from the actual object
		private static string GetLexicalFormFromObject(LexemeType type, Guid guid, LcmCache cache)
		{
			if (cache.ServiceLocator.ObjectRepository.TryGetObject(guid, out var wordform))
			{
				if (type == LexemeType.Word)
				{
					return ((IWfiAnalysis)wordform).GetForm(cache.DefaultVernWs).Text;
				}
				return ((ILexEntry)wordform).LexemeFormOA.Form.get_String(cache.DefaultVernWs).Text;
			}
			return string.Empty;
		}

		/// <summary>
		/// If the object is an ILexEntry then return its adjusted Homograph number, otherwise return 0
		/// </summary>
		private int GetHomographFromObject(Guid guid, LcmCache cache)
		{
			return cache.ServiceLocator.GetInstance<ILexEntryRepository>().TryGetObject(guid, out var entry) ? FdoLexicon.GetParatextHomographNumFromEntry(entry) : 0;
		}

		public int Homograph
		{
			get { return m_homograph; }
		}

		public override bool Equals(object obj)
		{
			var other = obj as LexemeKey;

			return other != null
				&& m_type == other.m_type
				&& m_lexicalForm == other.m_lexicalForm
				&& m_homograph == other.m_homograph;
		}

		public override int GetHashCode()
		{
			int code = 23;
			code = code * 31 + m_type.GetHashCode();
			code = code * 31 + m_lexicalForm.GetHashCode();
			code = code * 31 + m_homograph.GetHashCode();
			return code;
		}

		public override string ToString()
		{
			return Id;
		}
	}

}
