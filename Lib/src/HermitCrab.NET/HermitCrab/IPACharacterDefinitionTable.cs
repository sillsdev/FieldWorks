#if IPA_CHAR_DEF_TABLE
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.IO;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents information about an IPA character.
	/// </summary>
	public class IPACharInfo
	{
		public enum CharType { BASE_CHAR, MODIFIER, TIE_BAR, BOUNDARY };

		[XmlAttribute]
		public int Codepoint = 0;
		[XmlAttribute]
		public string IPAChar = null;
		[XmlAttribute]
		public string HexIPAChar = null;
		public string Name = null;
		public string Description = null;
		public CharType Type = CharType.BASE_CHAR;
		public bool IsStop = false;
		public bool IsFricative = false;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return string.Format("{0}: U+{1:X4}, {2}, {3}", IPAChar, Codepoint, Name, Description);
		}
	}

	/// <summary>
	/// This class represents an International Phonetic Alphabet (IPA) character definition table. It can
	/// support all base IPA characters, diacritics, and affricates.
	/// </summary>
	public class IPACharacterDefinitionTable : CharacterDefinitionTable
	{
		static Dictionary<char, IPACharInfo> s_ipaChars = new Dictionary<char, IPACharInfo>();
		const string IPA_CHAR_FILE = "IPACharInventory.xml";
		static IPACharacterDefinitionTable()
		{
			XmlSerializer charSerializer = new XmlSerializer(typeof(List<IPACharInfo>));
			TextReader reader = new StreamReader(IPA_CHAR_FILE);
			List<IPACharInfo> charInfoList = (List<IPACharInfo>)charSerializer.Deserialize(reader);
			reader.Close();

			foreach (IPACharInfo ipaChar in charInfoList)
				s_ipaChars[ipaChar.IPAChar[0]] = ipaChar;
		}

		List<SegmentDefinition> m_baseChars;
		List<SegmentDefinition> m_modifierChars;
		List<SegmentDefinition> m_tieBars;
		List<SegmentDefinition> m_stops;
		List<SegmentDefinition> m_fricatives;

		/// <summary>
		/// Initializes a new instance of the <see cref="IPACharacterDefinitionTable"/> class.
		/// </summary>
		/// <param name="id">The ID.</param>
		/// <param name="desc">The description.</param>
		/// <param name="morpher">The morpher.</param>
		public IPACharacterDefinitionTable(string id, string desc, Morpher morpher)
			: base(id, desc, morpher)
		{
			m_baseChars = new List<SegmentDefinition>();
			m_modifierChars = new List<SegmentDefinition>();
			m_tieBars = new List<SegmentDefinition>();
			m_stops = new List<SegmentDefinition>();
			m_fricatives = new List<SegmentDefinition>();
		}

		/// <summary>
		/// Adds the segment definition.
		/// </summary>
		/// <param name="segDef">The segment definition.</param>
		public override void AddSegmentDefinition(SegmentDefinition segDef)
		{
			IPACharInfo ipaChar;
			// get information about this IPA character
			if (!s_ipaChars.TryGetValue(segDef.StrRep[0], out ipaChar))
				throw new LoadException(string.Format(HCStrings.kstidInvalidIPAChar, segDef.StrRep));

			switch (ipaChar.Type)
			{
				case IPACharInfo.CharType.BASE_CHAR:
					// add a segment definition for this base character
					base.AddSegmentDefinition(segDef);
					// add segment definitions for each base character and modifier combo
					foreach (SegmentDefinition modifier in m_modifierChars)
						base.AddSegmentDefinition(ComposeModifier(segDef, modifier));

					// add affricate segment definitions if this character is a stop or a fricative
					if (ipaChar.IsFricative || ipaChar.IsStop)
					{
						foreach (SegmentDefinition tiebar in m_tieBars)
						{
							if (ipaChar.IsStop)
							{
								foreach (SegmentDefinition fricative in m_fricatives)
									base.AddSegmentDefinition(ComposeAffricate(segDef, tiebar, fricative));
							}
							else
							{
								foreach (SegmentDefinition stop in m_stops)
									base.AddSegmentDefinition(ComposeAffricate(stop, tiebar, segDef));
							}
						}

						if (ipaChar.IsStop)
							m_stops.Add(segDef);
						else
							m_fricatives.Add(segDef);
					}

					m_baseChars.Add(segDef);
					break;

				case IPACharInfo.CharType.MODIFIER:
					// compose segment definitions for all base character and modifier combos
					foreach (SegmentDefinition baseChar in m_baseChars)
						base.AddSegmentDefinition(ComposeModifier(baseChar, segDef));
					m_modifierChars.Add(segDef);
					break;

				case IPACharInfo.CharType.TIE_BAR:
					// add all affricates using this tie bar
					foreach (SegmentDefinition stop in m_stops)
					{
						foreach (SegmentDefinition fricative in m_fricatives)
							base.AddSegmentDefinition(ComposeAffricate(stop, segDef, fricative));
					}
					m_tieBars.Add(segDef);
					break;
			}
		}

		SegmentDefinition ComposeModifier(SegmentDefinition baseChar, SegmentDefinition diacritic)
		{
			FeatureBundle fb = baseChar.SynthFeatures.Clone();
			fb.Apply(diacritic.SynthFeatures, true);
			fb.Apply(diacritic.AntiFeatures, false);
			return new SegmentDefinition(baseChar.StrRep + diacritic.StrRep, fb);
		}

		SegmentDefinition ComposeAffricate(SegmentDefinition stop, SegmentDefinition tiebar, SegmentDefinition fricative)
		{
			FeatureBundle fb = stop.SynthFeatures.Clone();
			fb.Apply(fricative.SynthFeatures, true);
			fb.Apply(fricative.AntiFeatures, false);
			// the tie bar will probably set [+delayed-release], so do this last
			fb.Apply(tiebar.SynthFeatures, true);
			fb.Apply(tiebar.AntiFeatures, false);
			return new SegmentDefinition(stop.StrRep + tiebar.StrRep + fricative.StrRep, fb);
		}

		/// <summary>
		/// Gets the segment definition for the specified string representation. Strips
		/// all unused IPA modifiers from the string representation before looking for
		/// the segment definition.
		/// </summary>
		/// <param name="strRep">The string representation.</param>
		/// <returns>The segment definition.</returns>
		public override SegmentDefinition GetSegmentDefinition(string strRep)
		{
			string tstrRep;
			if (!StripUnusedChars(strRep, out tstrRep))
				return null;
			return base.GetSegmentDefinition(tstrRep);
		}

		/// <summary>
		/// Determines whether the specified word matches the specified phonetic shape.
		/// All unused IPA modifiers in the word are ignored when attempting to match
		/// the phonetic shape.
		/// </summary>
		/// <param name="word">The word.</param>
		/// <param name="shape">The phonetic shape.</param>
		/// <returns>
		/// 	<c>true</c> if the word matches the shape, otherwise <c>false</c>.
		/// </returns>
		public override bool IsMatch(string word, PhoneticShape shape)
		{
			string tword;
			if (!StripUnusedChars(word, out tword))
				return false;

			return base.IsMatch(tword, shape);
		}

		bool StripUnusedChars(string word, out string output)
		{
			output = null;
			StringBuilder sb = new StringBuilder();
			foreach (char c in word)
			{
				IPACharInfo curChar;
				if (!s_ipaChars.TryGetValue(c, out curChar))
					return false;

				switch (curChar.Type)
				{
					case IPACharInfo.CharType.BASE_CHAR:
						if (m_segDefs.ContainsKey(c.ToString()))
							sb.Append(c);
						else
							return false;
						break;

					case IPACharInfo.CharType.MODIFIER:
						if (m_segDefs.ContainsKey(c.ToString()))
							sb.Append(c);
						break;

					default:
						sb.Append(c);
						break;
				}
			}

			output = sb.ToString();
			return true;
		}
	}
}
#endif