using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents a gloss.
	/// </summary>
	public class Gloss : HCObject
	{
		public Gloss(string id, string desc, Morpher morpher)
			: base(id, desc, morpher)
		{
		}
	}

	/// <summary>
	/// This class represents a morpheme. All morpheme objects should extend this class.
	/// </summary>
	public abstract class Morpheme : HCObject
	{
		protected Stratum m_stratum = null;
		protected Gloss m_gloss = null;
		protected IEnumerable<MorphCoOccurrence> m_requiredMorphCoOccur = null;
		protected IEnumerable<MorphCoOccurrence> m_excludedMorphCoOccur = null;

		/// <summary>
		/// Initializes a new instance of the <see cref="Morpheme"/> class.
		/// </summary>
		/// <param name="id">The id.</param>
		/// <param name="desc">The description.</param>
		/// <param name="morpher">The morpher.</param>
		public Morpheme(string id, string desc, Morpher morpher)
			: base(id, desc, morpher)
		{
		}

		/// <summary>
		/// Gets or sets the stratum.
		/// </summary>
		/// <value>The stratum.</value>
		public Stratum Stratum
		{
			get
			{
				return m_stratum;
			}

			internal set
			{
				m_stratum = value;
			}
		}

		/// <summary>
		/// Gets or sets the morpheme's gloss.
		/// </summary>
		/// <value>The gloss.</value>
		public Gloss Gloss
		{
			get
			{
				return m_gloss;
			}

			set
			{
				m_gloss = value;
			}
		}

		/// <summary>
		/// Gets or sets the required morpheme co-occurrences.
		/// </summary>
		/// <value>The required morpheme co-occurrences.</value>
		public IEnumerable<MorphCoOccurrence> RequiredMorphemeCoOccurrences
		{
			get
			{
				return m_requiredMorphCoOccur;
			}

			set
			{
				m_requiredMorphCoOccur = value;
			}
		}

		/// <summary>
		/// Gets or sets the excluded morpheme co-occurrences.
		/// </summary>
		/// <value>The excluded morpheme co-occurrences.</value>
		public IEnumerable<MorphCoOccurrence> ExcludedMorphemeCoOccurrences
		{
			get
			{
				return m_excludedMorphCoOccur;
			}

			set
			{
				m_excludedMorphCoOccur = value;
			}
		}
	}

	/// <summary>
	/// This class represents an allomorph of a morpheme. Allomorphs can be phonologically
	/// conditioned using environments and are applied disjunctively within a morpheme.
	/// </summary>
	public abstract class Allomorph : HCObject, IComparable<Allomorph>
	{
		private Morpheme m_morpheme;
		private Set<Environment> m_requiredEnvs;
		private Set<Environment> m_excludedEnvs;
		private IEnumerable<MorphCoOccurrence> m_requiredAlloCoOccur;
		private IEnumerable<MorphCoOccurrence> m_excludedAlloCoOccur;
		private readonly Dictionary<string, string> m_properties;
		private int m_index = -1;

		/// <summary>
		/// Initializes a new instance of the <see cref="Allomorph"/> class.
		/// </summary>
		/// <param name="id">The id.</param>
		/// <param name="desc">The description.</param>
		/// <param name="morpher">The morpher.</param>
		protected Allomorph(string id, string desc, Morpher morpher)
			: base (id, desc, morpher)
		{
			m_properties = new Dictionary<string, string>();
		}

		/// <summary>
		/// Gets or sets the morpheme.
		/// </summary>
		/// <value>The morpheme.</value>
		public Morpheme Morpheme
		{
			get
			{
				return m_morpheme;
			}

			internal set
			{
				m_morpheme = value;
			}
		}

		/// <summary>
		/// Gets or sets the index of this allomorph in the morpheme.
		/// </summary>
		/// <value>The index.</value>
		public int Index
		{
			get
			{
				return m_index;
			}

			internal set
			{
				m_index = value;
			}
		}

		/// <summary>
		/// Gets or sets the required environments.
		/// </summary>
		/// <value>The required environments.</value>
		public IEnumerable<Environment> RequiredEnvironments
		{
			get
			{
				return m_requiredEnvs;
			}

			set
			{
				m_requiredEnvs = value == null ? null : new Set<Environment>(value);
			}
		}

		/// <summary>
		/// Gets or sets the excluded environments.
		/// </summary>
		/// <value>The excluded environments.</value>
		public IEnumerable<Environment> ExcludedEnvironments
		{
			get
			{
				return m_excludedEnvs;
			}

			set
			{
				m_excludedEnvs = value == null ? null : new Set<Environment>(value);
			}
		}

		/// <summary>
		/// Gets or sets the required allomorph co-occurrences.
		/// </summary>
		/// <value>The required allomorph co-occurrences.</value>
		public IEnumerable<MorphCoOccurrence> RequiredAllomorphCoOccurrences
		{
			get
			{
				return m_requiredAlloCoOccur;
			}

			set
			{
				m_requiredAlloCoOccur = value;
			}
		}

		/// <summary>
		/// Gets or sets the excluded allomorph co-occurrences.
		/// </summary>
		/// <value>The excluded allomorph co-occurrences.</value>
		public IEnumerable<MorphCoOccurrence> ExcludedAllomorphCoOccurrences
		{
			get
			{
				return m_excludedAlloCoOccur;
			}

			set
			{
				m_excludedAlloCoOccur = value;
			}
		}

		/// <summary>
		/// Gets or sets the properties.
		/// </summary>
		/// <value>The properties.</value>
		public IEnumerable<KeyValuePair<string, string>> Properties
		{
			get
			{
				return m_properties;
			}

			set
			{
				m_properties.Clear();
				if (value != null)
				{
					foreach (KeyValuePair<string, string> kvp in value)
						m_properties[kvp.Key] = kvp.Value;
				}
			}
		}

		/// <summary>
		/// Gets the property value for the specified name.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <returns>The value.</returns>
		public string GetProperty(string name)
		{
			string value;
			if (m_properties.TryGetValue(name, out value))
				return value;
			return null;
		}

		public virtual bool ConstraintsEqual(Allomorph other)
		{
			if (m_requiredEnvs == null)
			{
				if (other.m_requiredEnvs != null)
					return false;
			}
			else
			{
				if (!m_requiredEnvs.Equals(other.m_requiredEnvs))
					return false;
			}

			if (m_excludedEnvs == null)
			{
				if (other.m_excludedEnvs != null)
					return false;
			}
			else
			{
				if (!m_excludedEnvs.Equals(other.m_excludedEnvs))
					return false;
			}

			return true;
		}

		public int CompareTo(Allomorph other)
		{
			if (other.Morpheme != Morpheme)
				throw new ArgumentException("Cannot compare allomorphs from different morphemes.", "other");

			return m_index.CompareTo(other.m_index);
		}
	}

	/// <summary>
	/// This class represents a morph. Morphs are specific phonetic realizations of morphemes in
	/// surface forms.
	/// </summary>
	public class Morph : ICloneable
	{
		int m_partition = -1;
		PhoneticShape m_shape;
		Allomorph m_allomorph;

		/// <summary>
		/// Initializes a new instance of the <see cref="Morph"/> class.
		/// </summary>
		/// <param name="allomorph">The allomorph.</param>
		public Morph(Allomorph allomorph)
		{
			m_allomorph = allomorph;
			m_shape = new PhoneticShape();
		}

		/// <summary>
		/// Copy constructor.
		/// </summary>
		/// <param name="morph">The morph.</param>
		public Morph(Morph morph)
		{
			m_partition = morph.m_partition;
			m_shape = morph.m_shape.Clone();
			m_allomorph = morph.m_allomorph;
		}

		/// <summary>
		/// Gets or sets the partition.
		/// </summary>
		/// <value>The partition.</value>
		public int Partition
		{
			get
			{
				return m_partition;
			}

			internal set
			{
				m_partition = value;
			}
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

		/// <summary>
		/// Gets the allomorph associated with this morph.
		/// </summary>
		/// <value>The allomorph.</value>
		public Allomorph Allomorph
		{
			get
			{
				return m_allomorph;
			}
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			return Equals(obj as Morph);
		}

		public bool Equals(Morph other)
		{
			if (other == null)
				return false;

			return m_allomorph == other.m_allomorph;
		}

		public override int GetHashCode()
		{
			return m_allomorph.GetHashCode();
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		public Morph Clone()
		{
			return new Morph(this);
		}
	}

	/// <summary>
	/// This class represents a left-to-right ordering of morphs in a word.
	/// </summary>
	public class Morphs : KeyedCollection<int, Morph>, ICloneable
	{
		int m_nextPartition;

		/// <summary>
		/// Initializes a new instance of the <see cref="Morphs"/> class.
		/// </summary>
		public Morphs()
		{
		}

		/// <summary>
		/// Copy constructor.
		/// </summary>
		/// <param name="morphs">The morphs.</param>
		public Morphs(Morphs morphs)
		{
			m_nextPartition = morphs.m_nextPartition;
			foreach (Morph mi in morphs)
				base.InsertItem(Count, mi);
		}

		protected override void InsertItem(int index, Morph item)
		{
			if (index < Count)
				throw new NotImplementedException();

			Collection<Morph> morphs = this;
			// check to see if the previous morpheme has the same ID, if so
			// combine the morphemes
			if (index - 1 >= 0 && morphs[index - 1].Allomorph.Morpheme == item.Allomorph.Morpheme
				&& (item.Partition == -1 || Contains(item.Partition) || morphs[index - 1].Partition == item.Partition))
			{
				item.Partition = morphs[index - 1].Partition;
				item.Shape.AddMany(morphs[index - 1].Shape);
				base.SetItem(index - 1, item);
			}
			else
			{

				if (item.Partition == -1 || Contains(item.Partition))
					item.Partition = m_nextPartition++;
				base.InsertItem(index, item);
			}
		}

		protected override void SetItem(int index, Morph item)
		{
			throw new NotImplementedException();
		}

		protected override int GetKeyForItem(Morph morph)
		{
			return morph.Partition;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			return Equals(obj as Morphs);
		}

		public bool Equals(Morphs other)
		{
			if (other == null)
				return false;

			if (Count != other.Count)
				return false;

			Collection<Morph> morphs = this;
			Collection<Morph> otherMorphs = other;
			for (int i = 0; i < Count; i++)
			{
				if (!morphs[i].Equals(otherMorphs[i]))
					return false;
			}
			return true;
		}

		public override int GetHashCode()
		{
			int hashCode = 0;
			foreach (Morph morph in this)
				hashCode ^= morph.GetHashCode();
			return hashCode;
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		public Morphs Clone()
		{
			return new Morphs(this);
		}
	}

	/// <summary>
	/// This class represents a morpheme co-occurrence rule. Morpheme co-occurrence rules are used
	/// to determine if a list of morphemes co-occur with a specific morpheme.
	/// </summary>
	public class MorphCoOccurrence
	{
		/// <summary>
		/// The object type
		/// </summary>
		public enum ObjectType
		{
			/// <summary>
			/// Allomorph
			/// </summary>
			ALLOMORPH,
			/// <summary>
			/// Morpheme
			/// </summary>
			MORPHEME
		}

		/// <summary>
		/// The co-occurrence adjacency type
		/// </summary>
		public enum AdjacencyType
		{
			/// <summary>
			/// Anywhere in the same word
			/// </summary>
			ANYWHERE,
			/// <summary>
			/// Somewhere to the left
			/// </summary>
			SOMEWHERE_TO_LEFT,
			/// <summary>
			/// Somewhere to the right
			/// </summary>
			SOMEWHERE_TO_RIGHT,
			/// <summary>
			/// Adjacent to the left
			/// </summary>
			ADJACENT_TO_LEFT,
			/// <summary>
			/// Adjacent to the right
			/// </summary>
			ADJACENT_TO_RIGHT
		}

		HCObjectSet<HCObject> m_others;
		AdjacencyType m_adjacency;
		ObjectType m_objectType;

		/// <summary>
		/// Initializes a new instance of the <see cref="MorphCoOccurrence"/> class.
		/// </summary>
		/// <param name="others">The other allomorphs or morphemes.</param>
		/// <param name="objectType">Type of the object.</param>
		/// <param name="adjacency">The adjacency.</param>
		public MorphCoOccurrence(IEnumerable<HCObject> others, ObjectType objectType, AdjacencyType adjacency)
		{
			m_others = new HCObjectSet<HCObject>(others);
			m_objectType = objectType;
			m_adjacency = adjacency;
		}

		/// <summary>
		/// Determines if all of the specified morphemes co-occur with the key morpheme.
		/// </summary>
		/// <param name="morphs">The morphs.</param>
		/// <param name="key">The key morpheme.</param>
		/// <returns></returns>
		public bool CoOccurs(Morphs morphs, HCObject key)
		{
			Collection<Morph> morphList = morphs;
			HCObjectSet<HCObject> others = new HCObjectSet<HCObject>(m_others);

			switch (m_adjacency)
			{
				case AdjacencyType.ANYWHERE:
					foreach (Morph morph in morphList)
						others.Remove(GetMorphObject(morph));
					break;

				case AdjacencyType.SOMEWHERE_TO_LEFT:
				case AdjacencyType.ADJACENT_TO_LEFT:
					for (int i = 0; i < morphList.Count; i++)
					{
						HCObject curMorphObj = GetMorphObject(morphList[i]);
						if (key == curMorphObj)
						{
							break;
						}
						else if (others.Count > 0 && others[0] == curMorphObj)
						{
							if (m_adjacency == AdjacencyType.ADJACENT_TO_LEFT)
							{
								if (i == morphList.Count - 1)
									return false;

								HCObject nextMorphObj = GetMorphObject(morphList[i + 1]);
								if (others.Count > 1)
								{
									if (others[1] != nextMorphObj)
										return false;
								}
								else if (key != nextMorphObj)
								{
									return false;
								}
							}
							others.RemoveAt(0);
						}
					}
					break;

				case AdjacencyType.SOMEWHERE_TO_RIGHT:
				case AdjacencyType.ADJACENT_TO_RIGHT:
					for (int i = morphList.Count - 1; i >= 0; i--)
					{
						HCObject curMorphObj = GetMorphObject(morphList[i]);
						if (key == curMorphObj)
						{
							break;
						}
						else if (others.Count > 0 && others[others.Count - 1] == curMorphObj)
						{
							if (m_adjacency == AdjacencyType.ADJACENT_TO_RIGHT)
							{
								if (i == 0)
									return false;

								HCObject prevMorphObj = GetMorphObject(morphList[i - 1]);
								if (others.Count > 1)
								{
									if (others[others.Count - 2] != prevMorphObj)
										return false;
								}
								else if (key != prevMorphObj)
								{
									return false;
								}
							}
							others.RemoveAt(others.Count - 1);
						}
					}
					break;
			}

			return others.Count == 0;
		}

		HCObject GetMorphObject(Morph morph)
		{
			switch (m_objectType)
			{
				case ObjectType.ALLOMORPH:
					return morph.Allomorph;

				case ObjectType.MORPHEME:
					return morph.Allomorph.Morpheme;
			}
			return null;
		}
	}
}
