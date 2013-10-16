using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents a segment definition for a character definition table.
	/// </summary>
	public class SegmentDefinition
	{
		string m_strRep;
		CharacterDefinitionTable m_charDefTable;
		FeatureBundle m_analysisFeatures = null;
		FeatureBundle m_synthFeatures = null;
		FeatureBundle m_antiFeatures = null;

		public SegmentDefinition(string strRep, CharacterDefinitionTable charDefTable, FeatureBundle fb)
		{
			m_strRep = strRep;
			m_charDefTable = charDefTable;
			m_synthFeatures = fb;
			m_antiFeatures = new FeatureBundle(m_synthFeatures, true);
			m_analysisFeatures = new FeatureBundle(m_synthFeatures, false);
		}

		public SegmentDefinition(string strRep, CharacterDefinitionTable charDefTable, IEnumerable<FeatureValue> featVals,
			FeatureSystem featSys) : this(strRep, charDefTable, new FeatureBundle(featVals, featSys))
		{
		}

		public string StrRep
		{
			get
			{
				return m_strRep;
			}
		}

		public CharacterDefinitionTable CharacterDefinitionTable
		{
			get
			{
				return m_charDefTable;
			}
		}

		public FeatureBundle AnalysisFeatures
		{
			get
			{
				return m_analysisFeatures;
			}
		}

		public FeatureBundle SynthFeatures
		{
			get
			{
				return m_synthFeatures;
			}
		}

		public FeatureBundle AntiFeatures
		{
			get
			{
				return m_antiFeatures;
			}
		}

		public override int GetHashCode()
		{
			return m_strRep.GetHashCode() ^ m_charDefTable.ID.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			return Equals(obj as SegmentDefinition);
		}

		public bool Equals(SegmentDefinition other)
		{
			if (other == null)
				return false;
			return m_strRep == other.m_strRep && m_charDefTable == other.m_charDefTable;
		}

		public override string ToString()
		{
			return m_strRep;
		}
	}

	public class BoundaryDefinition
	{
		string m_strRep;
		CharacterDefinitionTable m_charDefTable;

		public BoundaryDefinition(string strRep, CharacterDefinitionTable charDefTable)
		{
			m_strRep = strRep;
			m_charDefTable = charDefTable;
		}

		public string StrRep
		{
			get
			{
				return m_strRep;
			}
		}

		public CharacterDefinitionTable CharacterDefinitionTable
		{
			get
			{
				return m_charDefTable;
			}
		}

		public override int GetHashCode()
		{
			return m_strRep.GetHashCode() ^ m_charDefTable.ID.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			return Equals(obj as BoundaryDefinition);
		}

		public bool Equals(BoundaryDefinition other)
		{
			if (other == null)
				return false;
			return m_strRep == other.m_strRep && m_charDefTable == other.m_charDefTable;
		}

		public override string ToString()
		{
			return m_strRep;
		}
	}

	/// <summary>
	/// This class represents a character definition table. It encapsulates the mappings of
	/// characters to phonetic segments.
	/// </summary>
	public class CharacterDefinitionTable : HCObject
	{
		protected Dictionary<string, SegmentDefinition> m_segDefs;
		Dictionary<string, BoundaryDefinition> m_bdryDefs;
		string m_encoding = null;

		/// <summary>
		/// Initializes a new instance of the <see cref="CharacterDefinitionTable"/> class.
		/// </summary>
		/// <param name="featId">The ID.</param>
		/// <param name="desc">The description.</param>
		/// <param name="morpher">The morpher.</param>
		public CharacterDefinitionTable(string id, string desc, Morpher morpher)
			: base(id, desc, morpher)
		{
			m_segDefs = new Dictionary<string, SegmentDefinition>();
			m_bdryDefs = new Dictionary<string, BoundaryDefinition>();
		}

		/// <summary>
		/// Gets or sets the encoding.
		/// </summary>
		/// <value>The encoding.</value>
		public string Encoding
		{
			get
			{
				return m_encoding;
			}

			set
			{
				m_encoding = value;
			}
		}

		/// <summary>
		/// Adds the segment definition.
		/// </summary>
		/// <param name="segDef">The segment definition.</param>
		public virtual void AddSegmentDefinition(string strRep, IEnumerable<FeatureValue> featVals)
		{
			SegmentDefinition segDef = new SegmentDefinition(strRep, this, featVals, Morpher.PhoneticFeatureSystem);
			// what do we do about culture?
			m_segDefs[strRep.ToLowerInvariant()] = segDef;
		}

		/// <summary>
		/// Adds the boundary definition.
		/// </summary>
		/// <param name="strRep">The string representation.</param>
		public void AddBoundaryDefinition(string strRep)
		{
			m_bdryDefs[strRep] = new BoundaryDefinition(strRep, this);
		}

		/// <summary>
		/// Gets the segment definition for the specified string representation.
		/// </summary>
		/// <param name="strRep">The string representation.</param>
		/// <returns>The segment definition.</returns>
		public virtual SegmentDefinition GetSegmentDefinition(string strRep)
		{
			SegmentDefinition segDef;
			// what do we do about culture?
			if (m_segDefs.TryGetValue(strRep.ToLowerInvariant(), out segDef))
				return segDef;
			return null;
		}

		public BoundaryDefinition GetBoundaryDefinition(string strRep)
		{
			BoundaryDefinition bdryDef;
			if (m_bdryDefs.TryGetValue(strRep, out bdryDef))
				return bdryDef;
			return null;
		}

		/// <summary>
		/// Gets all of the string representations that match the specified segment.
		/// </summary>
		/// <param name="seg">The segment.</param>
		/// <param name="mode">The mode.</param>
		/// <returns>The string representations.</returns>
		public IList<SegmentDefinition> GetMatchingSegmentDefinitions(Segment seg, ModeType mode)
		{
			List<SegmentDefinition> results = new List<SegmentDefinition>();

			if (Morpher.PhoneticFeatureSystem.HasFeatures)
			{
				foreach (SegmentDefinition segDef in m_segDefs.Values)
				{
					switch (mode)
					{
						case ModeType.SYNTHESIS:
							if (segDef.AnalysisFeatures.IsUnifiable(seg.FeatureValues))
								results.Add(segDef);
							break;

						case ModeType.ANALYSIS:
							if (seg.FeatureValues.IsUnifiable(segDef.SynthFeatures))
								results.Add(segDef);
							break;
					}
				}
			}
			else
			{
				results.AddRange(seg.InstantiatedSegments);
			}

			return results;
		}

		/// <summary>
		/// Converts the specified string to a phonetic shape. It matches the longest possible segment
		/// first.
		/// </summary>
		/// <param name="str">The string.</param>
		/// <param name="mode">The mode.</param>
		/// <returns>The phonetic shape, <c>null</c> if the string contains invalid segments.</returns>
		public PhoneticShape ToPhoneticShape(string str, ModeType mode)
		{
			PhoneticShape ps = new PhoneticShape();
			int i = 0;
			ps.Add(new Margin(Direction.LEFT));
			while (i < str.Length)
			{
				bool match = false;
				for (int j = str.Length - i; j > 0; j--)
				{
					string s = str.Substring(i, j);
					PhoneticShapeNode node = GetPhoneticShapeNode(s, mode);
					if (node != null)
					{
						try
						{
							ps.Add(node);
						}
						catch (InvalidOperationException)
						{
							return null;
						}
						i += j;
						match = true;
						break;
					}
				}

				if (!match)
				{
					string sPhonemesFoundSoFar = ToRegexString(ps, ModeType.ANALYSIS, true);
					var missing = new MissingPhoneticShapeException(sPhonemesFoundSoFar, i);
					throw missing;
				}
			}
			ps.Add(new Margin(Direction.RIGHT));

			return ps;
		}

		PhoneticShapeNode GetPhoneticShapeNode(string strRep, ModeType mode)
		{
			PhoneticShapeNode node = null;
			SegmentDefinition segDef = GetSegmentDefinition(strRep);
			if (segDef != null)
			{
				Segment seg = new Segment(segDef, mode == ModeType.SYNTHESIS ? segDef.SynthFeatures.Clone() : segDef.AnalysisFeatures.Clone());
				if (!Morpher.PhoneticFeatureSystem.HasFeatures)
					seg.InstantiateSegment(segDef);
				node = seg;
			}
			else
			{
				BoundaryDefinition bdryDef = GetBoundaryDefinition(strRep);
				if (bdryDef != null)
					node = new Boundary(bdryDef);
			}
			return node;
		}

		/// <summary>
		/// Converts the specified phonetic shape to a valid regular expression string. Regular expressions
		/// formatted for display purposes are NOT guaranteed to compile.
		/// </summary>
		/// <param name="shape">The phonetic shape.</param>
		/// <param name="mode">The mode.</param>
		/// <param name="displayFormat">if <c>true</c> the result will be formatted for display, otherwise
		/// it will be formatted for compilation.</param>
		/// <returns>The regular expression string.</returns>
		public string ToRegexString(PhoneticShape shape, ModeType mode, bool displayFormat)
		{
			StringBuilder sb = new StringBuilder();
			foreach (PhoneticShapeNode node in shape)
			{
				if (node.IsDeleted)
					continue;

				switch (node.Type)
				{
					case PhoneticShapeNode.NodeType.SEGMENT:
						Segment seg = node as Segment;
						IList<SegmentDefinition> segDefs = GetMatchingSegmentDefinitions(seg, mode);
						if (segDefs.Count > 0)
						{
							if (segDefs.Count > 1)
								sb.Append(displayFormat ? "[" : "(");
							for (int i = 0; i < segDefs.Count; i++)
							{
								if (segDefs[i].StrRep.Length > 1)
									sb.Append("(");

								if (displayFormat)
									sb.Append(segDefs[i].StrRep);
								else
									sb.Append(Regex.Escape(segDefs[i].StrRep));

								if (segDefs[i].StrRep.Length > 1)
									sb.Append(")");
								if (i < segDefs.Count - 1 && !displayFormat)
									sb.Append("|");
							}
							if (segDefs.Count > 1)
								sb.Append(displayFormat ? "]" : ")");

							if (seg.IsOptional)
								sb.Append("?");
						}
						break;

					case PhoneticShapeNode.NodeType.BOUNDARY:
						Boundary bdry = node as Boundary;
						if (bdry.BoundaryDefinition.StrRep.Length > 1)
							sb.Append("(");

						if (displayFormat)
							sb.Append(bdry.BoundaryDefinition.StrRep);
						else
							sb.Append(Regex.Escape(bdry.BoundaryDefinition.StrRep));

						if (bdry.BoundaryDefinition.StrRep.Length > 1)
							sb.Append(")");
						sb.Append("?");
						break;

					case PhoneticShapeNode.NodeType.MARGIN:
						if (!displayFormat)
						{
							Margin margin = node as Margin;
							sb.Append(margin.MarginType == Direction.LEFT ? "^" : "$");
						}
						break;
				}
			}
			return sb.ToString();
		}

		/// <summary>
		/// Generates a string representation of the specified phonetic shape.
		/// </summary>
		/// <param name="shape">The phonetic shape.</param>
		/// <param name="mode">The mode.</param>
		/// <param name="includeBdry">if <c>true</c> boundary markers will be included in the
		/// string representation.</param>
		/// <returns>The string representation.</returns>
		public string ToString(PhoneticShape shape, ModeType mode, bool includeBdry)
		{
			StringBuilder sb = new StringBuilder();
			foreach (PhoneticShapeNode node in shape)
			{
				if (node.IsDeleted)
					continue;

				switch (node.Type)
				{
					case PhoneticShapeNode.NodeType.SEGMENT:
						Segment seg = node as Segment;
						IList<SegmentDefinition> segDefs = GetMatchingSegmentDefinitions(seg, mode);
						if (segDefs.Count > 0)
							sb.Append(segDefs[0].StrRep);
						break;

					case PhoneticShapeNode.NodeType.BOUNDARY:
						if (includeBdry)
						{
							Boundary bdry = node as Boundary;
							sb.Append(bdry.BoundaryDefinition.StrRep);
						}
						break;
				}
			}
			return sb.ToString();
		}

		/// <summary>
		/// Determines whether the specified word matches the specified phonetic shape.
		/// </summary>
		/// <param name="word">The word.</param>
		/// <param name="shape">The phonetic shape.</param>
		/// <returns>
		/// 	<c>true</c> if the word matches the shape, otherwise <c>false</c>.
		/// </returns>
		public virtual bool IsMatch(string word, PhoneticShape shape)
		{
			string pattern = ToRegexString(shape, ModeType.SYNTHESIS, false);
			return Regex.IsMatch(word, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		}

		public void Reset()
		{
			m_encoding = null;
			m_segDefs.Clear();
			m_bdryDefs.Clear();
		}
	}

	public class MissingPhoneticShapeException : Exception
	{
		//
		// For guidelines regarding the creation of new exception types, see
		//    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
		// and
		//    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
		//

		private string m_phonemesFoundSoFar;
		private int m_position;

		public MissingPhoneticShapeException(string phonemesFoundSoFar, int position)
		{
			m_phonemesFoundSoFar = phonemesFoundSoFar;
			m_position = position;
		}

		public MissingPhoneticShapeException(string phonemesFoundSoFar, int position, string message) : base(message)
		{
			m_phonemesFoundSoFar = phonemesFoundSoFar;
			m_position = position;
		}

		public MissingPhoneticShapeException(string phonemesFoundSoFar, int position, string message, Exception inner) : base(message, inner)
		{
			m_phonemesFoundSoFar = phonemesFoundSoFar;
			m_position = position;
		}

		public string PhonemesFoundSoFar
		{
			get { return m_phonemesFoundSoFar; }
		}

		public int Position
		{
			get { return m_position; }
		}
	}
}
