using System.Collections.Generic;
using System.Text;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents a part of speech category.
	/// </summary>
	public class PartOfSpeech : HCObject
	{
		public PartOfSpeech(string id, string desc, Morpher morpher)
			: base(id, desc, morpher)
		{
		}
	}

	/// <summary>
	/// This class represents a lexical entry.
	/// </summary>
	public class LexEntry : Morpheme
	{
		/// <summary>
		/// This class represents an allomorph in a lexical entry.
		/// </summary>
		public class RootAllomorph : Allomorph
		{
			PhoneticShape m_shape;

			/// <summary>
			/// Initializes a new instance of the <see cref="RootAllomorph"/> class.
			/// </summary>
			/// <param name="id">The id.</param>
			/// <param name="desc">The description.</param>
			/// <param name="morpher">The morpher.</param>
			/// <param name="shape">The shape.</param>
			public RootAllomorph(string id, string desc, Morpher morpher, PhoneticShape shape)
				: base (id, desc, morpher)
			{
				m_shape = shape;
			}

			/// <summary>
			/// Gets the phonetic shape.
			/// </summary>
			/// <value>The phonetic shape.</value>
			public PhoneticShape Shape
			{
				get
				{
					return m_shape;
				}
			}

			public override bool ConstraintsEqual(Allomorph other)
			{
				RootAllomorph otherAllo = (RootAllomorph) other;
				return m_shape.Equals(otherAllo.m_shape) && base.ConstraintsEqual(other);
			}

			public override string ToString()
			{
				return m_shape.ToString();
			}
		}

		List<RootAllomorph> m_allomorphs;
		PartOfSpeech m_pos = null;
		MPRFeatureSet m_mprFeatures = null;
		FeatureValues m_headFeatures = null;
		FeatureValues m_footFeatures = null;
		LexFamily m_family = null;

		/// <summary>
		/// Initializes a new instance of the <see cref="LexEntry"/> class.
		/// </summary>
		/// <param name="id">The ID.</param>
		/// <param name="desc">The description.</param>
		/// <param name="morpher">The morpher.</param>
		public LexEntry(string id, string desc, Morpher morpher)
			: base (id, desc, morpher)
		{
			m_allomorphs = new List<RootAllomorph>();
		}

		/// <summary>
		/// Gets the primary allomorph. This is the first allomorph.
		/// </summary>
		/// <value>The primary allomorph.</value>
		public RootAllomorph PrimaryAllomorph
		{
			get
			{
				if (m_allomorphs.Count == 0)
					return null;
				return m_allomorphs[0];
			}
		}

		/// <summary>
		/// Gets the allomorphs.
		/// </summary>
		/// <value>The allomorphs.</value>
		public IEnumerable<RootAllomorph> Allomorphs
		{
			get
			{
				return m_allomorphs;
			}
		}

		/// <summary>
		/// Gets the allomorph count.
		/// </summary>
		/// <value>The allomorph count.</value>
		public int AllomorphCount
		{
			get
			{
				return m_allomorphs.Count;
			}
		}

		/// <summary>
		/// Gets or sets the part of speech.
		/// </summary>
		/// <value>The part of speech.</value>
		public PartOfSpeech POS
		{
			get
			{
				return m_pos;
			}

			set
			{
				m_pos = value;
			}
		}

		/// <summary>
		/// Gets the MPR features.
		/// </summary>
		/// <value>The MPR features.</value>
		public MPRFeatureSet MPRFeatures
		{
			get
			{
				return m_mprFeatures;
			}

			set
			{
				m_mprFeatures = value;
			}
		}

		/// <summary>
		/// Gets the head features.
		/// </summary>
		/// <value>The head features.</value>
		public FeatureValues HeadFeatures
		{
			get
			{
				return m_headFeatures;
			}

			set
			{
				m_headFeatures = value;
			}
		}

		/// <summary>
		/// Gets the foot features.
		/// </summary>
		/// <value>The foot features.</value>
		public FeatureValues FootFeatures
		{
			get
			{
				return m_footFeatures;
			}

			set
			{
				m_footFeatures = value;
			}
		}

		/// <summary>
		/// Gets or sets the lexical family.
		/// </summary>
		/// <value>The lexical family.</value>
		public LexFamily Family
		{
			get
			{
				return m_family;
			}

			internal set
			{
				m_family = value;
			}
		}

		/// <summary>
		/// Adds the specified allomorph.
		/// </summary>
		/// <param name="allomorph">The allomorph.</param>
		public void AddAllomorph(RootAllomorph allomorph)
		{
			allomorph.Morpheme = this;
			allomorph.Index = m_allomorphs.Count;
			m_allomorphs.Add(allomorph);
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			bool firstItem = true;
			foreach (RootAllomorph allomorph in m_allomorphs)
			{
				if (!firstItem)
					sb.Append(", ");
				sb.Append(m_stratum.CharacterDefinitionTable.ToString(allomorph.Shape, ModeType.SYNTHESIS, true));
				firstItem = false;
			}

			return string.Format(HCStrings.kstidLexEntry, ID, sb, m_gloss == null ? "?" : m_gloss.Description);
		}
	}

	/// <summary>
	/// This class represents a lexical family.
	/// </summary>
	public class LexFamily : HCObject
	{
		HCObjectSet<LexEntry> m_entries;

		public LexFamily(string id, string desc, Morpher morpher)
			: base(id, desc, morpher)
		{
			m_entries = new HCObjectSet<LexEntry>();
		}

		public IEnumerable<LexEntry> Entries
		{
			get
			{
				return m_entries;
			}
		}

		public void AddEntry(LexEntry entry)
		{
			entry.Family = this;
			m_entries.Add(entry);
		}
	}

	/// <summary>
	/// This class represents a lexicon.
	/// </summary>
	public class Lexicon
	{
		HCObjectSet<LexEntry> m_entries;
		HCObjectSet<LexFamily> m_families;

		public Lexicon()
		{
			m_entries = new HCObjectSet<LexEntry>();
			m_families = new HCObjectSet<LexFamily>();
		}

		/// <summary>
		/// Gets the lexical families.
		/// </summary>
		/// <value>The lexical families.</value>
		public IEnumerable<LexFamily> Families
		{
			get
			{
				return m_families;
			}
		}

		/// <summary>
		/// Gets the lexical entries.
		/// </summary>
		/// <value>The lexical entries.</value>
		public IEnumerable<LexEntry> Entries
		{
			get
			{
				return m_entries;
			}
		}

		/// <summary>
		/// Gets the lexical entry associated with the specified ID.
		/// </summary>
		/// <param name="id">The ID.</param>
		/// <returns>The lexical entry.</returns>
		public LexEntry GetEntry(string id)
		{
			LexEntry entry;
			if (m_entries.TryGetValue(id, out entry))
				return entry;
			return null;
		}

		/// <summary>
		/// Gets the lexical family associated with the specified ID.
		/// </summary>
		/// <param name="id">The ID.</param>
		/// <returns>The lexical family.</returns>
		public LexFamily GetFamily(string id)
		{
			LexFamily family;
			if (m_families.TryGetValue(id, out family))
				return family;
			return null;
		}

		/// <summary>
		/// Adds the lexical family.
		/// </summary>
		/// <param name="family">The lexical family.</param>
		public void AddFamily(LexFamily family)
		{
			m_families.Add(family);
		}

		/// <summary>
		/// Adds the lexical entry.
		/// </summary>
		/// <param name="entry">The lexical entry.</param>
		public void AddEntry(LexEntry entry)
		{
			m_entries.Add(entry);
		}

		/// <summary>
		/// Resets this instance.
		/// </summary>
		public void Reset()
		{
			m_families.Clear();
			m_entries.Clear();
		}
	}
}
