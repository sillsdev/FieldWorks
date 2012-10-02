using System;
using System.Collections.Generic;
using System.Text;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents a feature value.
	/// </summary>
	public class FeatureValue : HCObject
	{
		int m_index = -1;
		Feature m_feature = null;

		/// <summary>
		/// Initializes a new instance of the <see cref="FeatureValue"/> class.
		/// </summary>
		/// <param name="id">The id.</param>
		/// <param name="desc">The description.</param>
		/// <param name="morpher">The morpher.</param>
		public FeatureValue(string id, string desc, Morpher morpher)
			: base(id, desc, morpher)
		{
		}

		/// <summary>
		/// Gets or sets the feature.
		/// </summary>
		/// <value>The feature.</value>
		public Feature Feature
		{
			get
			{
				return m_feature;
			}

			internal set
			{
				m_feature = value;
			}
		}

		/// <summary>
		/// Gets or sets the index of this value in feature bundles.
		/// </summary>
		/// <value>The index in feature bundles.</value>
		internal int FeatureBundleIndex
		{
			get
			{
				return m_index;
			}

			set
			{
				m_index = value;
			}
		}
	}

	/// <summary>
	/// This class represents a feature.
	/// </summary>
	public class Feature : HCObject
	{
		HCObjectSet<FeatureValue> m_possibleValues;
		ValueInstance m_defaultValue = null;

		Feature m_parent = null;
		HCObjectSet<Feature> m_subFeatures;

		/// <summary>
		/// Initializes a new instance of the <see cref="Feature"/> class.
		/// </summary>
		/// <param name="id">The id.</param>
		/// <param name="desc">The description.</param>
		/// <param name="morpher">The morpher.</param>
		public Feature(string id, string desc, Morpher morpher)
			: base(id, desc, morpher)
		{
			m_possibleValues = new HCObjectSet<FeatureValue>();
			m_subFeatures = new HCObjectSet<Feature>();
		}

		/// <summary>
		/// Gets all possible values.
		/// </summary>
		/// <value>All possible values.</value>
		public IEnumerable<FeatureValue> PossibleValues
		{
			get
			{
				return m_possibleValues;
			}
		}

		/// <summary>
		/// Gets all default values.
		/// </summary>
		/// <value>The default values.</value>
		public ValueInstance DefaultValue
		{
			get
			{
				return m_defaultValue;
			}

			set
			{
				m_defaultValue = value;
			}
		}

		/// <summary>
		/// Gets the parent feature.
		/// </summary>
		/// <value>The parent feature.</value>
		public Feature Parent
		{
			get
			{
				return m_parent;
			}
		}

		/// <summary>
		/// Gets the subfeatures.
		/// </summary>
		/// <value>The subfeatures.</value>
		public IEnumerable<Feature> SubFeatures
		{
			get
			{
				return m_subFeatures;
			}
		}

		/// <summary>
		/// Gets the value associated with the specified ID.
		/// </summary>
		/// <param name="id">The ID.</param>
		/// <returns>The feature value.</returns>
		public FeatureValue GetPossibleValue(string id)
		{
			FeatureValue value;
			if (m_possibleValues.TryGetValue(id, out value))
				return value;
			return null;
		}

		/// <summary>
		/// Adds the value.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <param name="isDefault">if <c>true</c> this value is a default value.</param>
		public void AddPossibleValue(FeatureValue value)
		{
			value.Feature = this;
			m_possibleValues.Add(value);
		}

		/// <summary>
		/// Removes the value associated with the specified ID.
		/// </summary>
		/// <param name="id">The ID.</param>
		public void RemovePossibleValue(string id)
		{
			m_possibleValues.Remove(id);
		}

		/// <summary>
		/// Adds the subfeature.
		/// </summary>
		/// <param name="feature">The feature.</param>
		public void AddSubFeature(Feature feature)
		{
			feature.m_parent = this;
			m_subFeatures.Add(feature);
		}
	}

	/// <summary>
	/// This represents a set of feature values. Each possible feature value in the feature system is encoded in
	/// a single bit in a unsigned long integer. This allows fast manipulation of the set of feature values using
	/// bitwise operators. Each bit indicates whether the corresponding value has been set. The feature bundle allows
	/// multiple values for the same feature to be set. This is used to uninstantiate features during the analysis
	/// phase. It is primarily used for phonetic features.
	/// </summary>
	public class FeatureBundle : ICloneable
	{
		const int ULONG_BITS = sizeof(ulong) * 8;
		public const int MAX_NUM_VALUES = ULONG_BITS * 2;

		ulong m_flags1;
		ulong m_flags2;
		FeatureSystem m_featSys;

		/// <summary>
		/// Initializes a new instance of the <see cref="FeatureBundle"/> class.
		/// </summary>
		/// <param name="initialVal">The initial value for all values in the feature bundle.</param>
		/// <param name="featSys">The feature system.</param>
		public FeatureBundle(bool initialVal, FeatureSystem featSys)
		{
			SetAll(initialVal);
			m_featSys = featSys;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="FeatureBundle"/> class.
		/// </summary>
		/// <param name="featVals">The feature values.</param>
		/// <param name="set">if <c>true</c> the specified values will be set, otherwise they will be unset.</param>
		/// <param name="featSys">The feat system.</param>
		public FeatureBundle(IEnumerable<FeatureValue> featVals, FeatureSystem featSys)
			: this(false, featSys)
		{
			foreach (FeatureValue value in featVals)
				Set(value, true);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="FeatureBundle"/> class. This constructor
		/// creates an anti feature bundle from the specified feature bundle.
		/// </summary>
		/// <param name="fb">The feature bundle.</param>
		/// <param name="set">if <c>true</c> the specified values will be set, otherwise they will be unset.</param>
		public FeatureBundle(FeatureBundle fb, bool set)
			: this(!set, fb.m_featSys)
		{
			foreach (Feature feature in fb.Features)
			{
				foreach (FeatureValue value in feature.PossibleValues)
				{
					if (!fb.Get(value))
					{
						Set(value, set);
					}
				}
			}
		}

		/// <summary>
		/// Copy constructor.
		/// </summary>
		/// <param name="fb">The feature bundle.</param>
		public FeatureBundle(FeatureBundle fb)
		{
			m_flags1 = fb.m_flags1;
			m_flags2 = fb.m_flags2;
			m_featSys = fb.m_featSys;
		}

		/// <summary>
		/// Gets all that have instantiated values in this feature bundle.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<Feature> Features
		{
			get
			{
				HCObjectSet<Feature> feats = new HCObjectSet<Feature>();
				foreach (FeatureValue value in m_featSys.Values)
				{
					if (Get(value))
						feats.Add(value.Feature);
				}
				return feats;
			}
		}

		/// <summary>
		/// Gets the feature system.
		/// </summary>
		/// <value>The feature system.</value>
		public FeatureSystem FeatureSystem
		{
			get
			{
				return m_featSys;

			}
		}

		/// <summary>
		/// Gets the values associated with the specified feature.
		/// </summary>
		/// <param name="feature">The feature.</param>
		/// <returns>The values.</returns>
		public IEnumerable<FeatureValue> GetValues(Feature feature)
		{
			foreach (FeatureValue value in feature.PossibleValues)
			{
				if (Get(value))
					yield return value;
			}
		}

		/// <summary>
		/// Determines whether this set of feature values contains the specified feature.
		/// </summary>
		/// <param name="feature">The feature.</param>
		/// <returns>
		/// 	<c>true</c> if this set contains the specified feature, otherwise <c>false</c>.
		/// </returns>
		public bool ContainsFeature(Feature feature)
		{
			foreach (FeatureValue value in m_featSys.Values)
			{
				if (Get(value) && value.Feature == feature)
					return true;
			}
			return false;
		}

		/// <summary>
		/// Determines whether this feature bundle is unifiable with the specified feature bundle.
		/// </summary>
		/// <param name="fb">The feature bundle.</param>
		/// <returns>
		/// 	<c>true</c> if the feature bundles are unifiable, otherwise <c>false</c>.
		/// </returns>
		public bool IsUnifiable(FeatureBundle fb)
		{
			return ((fb.m_flags1 & ~m_flags1) == 0) && ((fb.m_flags2 & ~m_flags2) == 0);
		}

		/// <summary>
		/// Determines whether this feature bundle and the specified feature bundle are disjoint sets.
		/// </summary>
		/// <param name="fb">The feature bundle.</param>
		/// <returns>
		/// 	<c>true</c> if the feature bundles are disjoint, otherwise <c>false</c>.
		/// </returns>
		public bool IsDisjoint(FeatureBundle fb)
		{
			return ((m_flags1 & fb.m_flags1) == 0) && ((m_flags2 & fb.m_flags2) == 0);
		}

		/// <summary>
		/// Determines if the value at the specified index is set.
		/// </summary>
		/// <param name="index">The index.</param>
		/// <returns><c>true</c> if the value is set, otherwise <c>false</c></returns>
		public bool Get(FeatureValue value)
		{
			if (value.FeatureBundleIndex < ULONG_BITS)
				return (m_flags1 & (1UL << value.FeatureBundleIndex)) != 0;
			else
				return (m_flags2 & (1UL << (value.FeatureBundleIndex - ULONG_BITS))) != 0;
		}

		/// <summary>
		/// Sets or unsets the value at the specified index.
		/// </summary>
		/// <param name="index">The index.</param>
		/// <param name="v">if <c>true</c> the value is set, otherwise it is unset.</param>
		public void Set(FeatureValue value, bool v)
		{
			if (value.FeatureBundleIndex < ULONG_BITS)
			{
				ulong mask = 1UL << value.FeatureBundleIndex;
				if (v)
					m_flags1 |= mask;
				else
					m_flags1 &= ~mask;
			}
			else
			{
				ulong mask = 1UL << value.FeatureBundleIndex - ULONG_BITS;
				if (v)
					m_flags2 |= mask;
				else
					m_flags2 &= ~mask;
			}
		}

		/// <summary>
		/// Sets or unsets all of the values.
		/// </summary>
		/// <param name="v">if <c>true</c> all values will be set, otherwise they will be unset.</param>
		public void SetAll(bool v)
		{
			if (v)
			{
				m_flags1 = 0xffffffffffffffffUL;
				m_flags2 = 0xffffffffffffffffUL;
			}
			else
			{
				m_flags1 = 0;
				m_flags2 = 0;
			}
		}

		/// <summary>
		/// Applies the specified feature bundle to this feature bundle.
		/// </summary>
		/// <param name="fb">The feature bundle.</param>
		/// <param name="polarity">The polarity.</param>
		public void Apply(FeatureBundle fb, bool polarity)
		{
			if (polarity)
			{
				AddValues(fb);
			}
			else
			{
				RemoveValues(fb);
			}
		}

		/// <summary>
		/// Adds all of the feature values set in the specified feature bundle by performing a
		/// bitwise-or operation.
		/// </summary>
		/// <param name="fb">The feature bundle.</param>
		public void AddValues(FeatureBundle fb)
		{
			m_flags1 |= fb.m_flags1;
			m_flags2 |= fb.m_flags2;
		}

		/// <summary>
		/// Adds all of the anti feature values set in the specified feature bundle by performing
		/// a bitwise-nor operation.
		/// </summary>
		/// <param name="fb">The feature bundle.</param>
		public void AddAntiValues(FeatureBundle fb)
		{
			m_flags1 |= ~fb.m_flags1;
			m_flags2 |= ~fb.m_flags2;
		}

		/// <summary>
		/// Removes all of the feature values set in the specified feature bundle by performing
		/// a bitwise-nand operation.
		/// </summary>
		/// <param name="fb">The feature bundle.</param>
		public void RemoveValues(FeatureBundle fb)
		{
			m_flags1 &= ~fb.m_flags1;
			m_flags2 &= ~fb.m_flags2;
		}

		public void Intersection(FeatureBundle fb)
		{
			m_flags1 &= fb.m_flags1;
			m_flags2 &= fb.m_flags2;
		}

		public FeatureBundle Clone()
		{
			return new FeatureBundle(this);
		}

		object ICloneable.Clone()
		{
			return this.Clone();
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			return Equals(obj as FeatureBundle);
		}

		public bool Equals(FeatureBundle other)
		{
			if (other == null)
				return false;
			return m_flags1 == other.m_flags1 && m_flags2 == other.m_flags2;
		}

		public override int GetHashCode()
		{
			return m_flags1.GetHashCode() ^ m_flags2.GetHashCode();
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			bool firstFeature = true;
			foreach (Feature feat in Features)
			{
				bool firstValue = true;
				StringBuilder valuesSb = new StringBuilder();
				foreach (FeatureValue value in GetValues(feat))
				{
					if (!firstValue)
						valuesSb.Append(", ");
					valuesSb.Append(value.Description);
					firstValue = false;
				}

				if (!firstValue)
				{
					if (!firstFeature)
						sb.Append(", ");
					sb.Append(feat.Description);
					sb.Append("->(");
					sb.Append(valuesSb);
					sb.Append(")");
					firstFeature = false;
				}
			}
			return sb.ToString();
		}
	}

	public abstract class ValueInstance
	{
		public enum ValueType { CLOSED, COMPLEX };

		public abstract ValueType Type
		{
			get;
		}

		public abstract bool Unify(ValueInstance other, out ValueInstance output);
		public abstract bool UnifyDefaults(ValueInstance other, out ValueInstance output);
		public abstract bool IsMatch(ValueInstance other);
		public abstract bool IsCompatible(ValueInstance other);
	}

	public class ClosedValueInstance : ValueInstance
	{
		HCObjectSet<FeatureValue> m_values;

		public ClosedValueInstance(IEnumerable<FeatureValue> values)
		{
			m_values = new HCObjectSet<FeatureValue>(values);
		}

		public ClosedValueInstance(FeatureValue value)
		{
			m_values = new HCObjectSet<FeatureValue>();
			m_values.Add(value);
		}

		public override ValueType Type
		{
			get
			{
				return ValueType.CLOSED;
			}
		}

		public IEnumerable<FeatureValue> Values
		{
			get
			{
				return m_values;
			}
		}

		public override bool Unify(ValueInstance other, out ValueInstance output)
		{
			ClosedValueInstance vi = other as ClosedValueInstance;

			HCObjectSet<FeatureValue> intersection = m_values.Intersection(vi.m_values);
			if (intersection.Count > 0)
			{
				output = new ClosedValueInstance(intersection);
				return true;
			}
			else
			{
				output = null;
				return false;
			}
		}

		public override bool UnifyDefaults(ValueInstance other, out ValueInstance output)
		{
			return Unify(other, out output);
		}

		public override bool IsMatch(ValueInstance other)
		{
			ClosedValueInstance vi = other as ClosedValueInstance;
			HCObjectSet<FeatureValue> intersection = m_values.Intersection(vi.m_values);
			return intersection.Count > 0;
		}

		public override bool IsCompatible(ValueInstance other)
		{
			return IsMatch(other);
		}

		public override int GetHashCode()
		{
			return m_values.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			return Equals(obj as ClosedValueInstance);
		}

		public bool Equals(ClosedValueInstance other)
		{
			if (other == null)
				return false;
			return m_values.Equals(other.m_values);
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			bool firstValue = true;
			foreach (FeatureValue value in m_values)
			{
				if (!firstValue)
					sb.Append(", ");
				sb.Append(value.Description);
				firstValue = false;
			}
			return sb.ToString();
		}
	}

	/// <summary>
	/// This class represents a set of feature values. It differs from <see cref="FeatureBundle"/> in that
	/// it does not represent the values as bits in unsigned long integers. It is primarily used for
	/// morphosyntatic features.
	/// </summary>
	public class FeatureValues : ValueInstance, ICloneable
	{
		SortedDictionary<Feature, ValueInstance> m_values;

		/// <summary>
		/// Initializes a new instance of the <see cref="FeatureValues"/> class.
		/// </summary>
		public FeatureValues()
		{
			m_values = new SortedDictionary<Feature, ValueInstance>();
		}

		/// <summary>
		/// Copy constructor.
		/// </summary>
		/// <param name="fv">The fv.</param>
		public FeatureValues(FeatureValues fv)
		{
			m_values = new SortedDictionary<Feature, ValueInstance>(fv.m_values);
		}

		/// <summary>
		/// Gets the features.
		/// </summary>
		/// <value>The features.</value>
		public IEnumerable<Feature> Features
		{
			get
			{
				return m_values.Keys;
			}
		}

		/// <summary>
		/// Gets the number of features.
		/// </summary>
		/// <value>The number of features.</value>
		public int NumFeatures
		{
			get
			{
				return m_values.Count;
			}
		}

		public override ValueType Type
		{
			get
			{
				return ValueType.COMPLEX;
			}
		}

		/// <summary>
		/// Adds the specified feature-value pair.
		/// </summary>
		/// <param name="feature">The feature.</param>
		/// <param name="value">The value.</param>
		public void Add(Feature feature, ValueInstance value)
		{
			m_values[feature] = value;
		}

		/// <summary>
		/// Adds the specified feature.
		/// </summary>
		/// <param name="feature">The feature.</param>
		public void Add(Feature feature)
		{
			Add(feature, null);
		}

		/// <summary>
		/// Adds features from the specified set of feature values. If a feature that is in the specified
		/// set of feature values already exists in this set of feature values, it will not be added.
		/// </summary>
		/// <param name="fv">The feature values.</param>
		public void Add(FeatureValues fv)
		{
			foreach (KeyValuePair<Feature, ValueInstance> kvp in fv.m_values)
			{
				ValueInstance vi;
				if (kvp.Value != null && m_values.TryGetValue(kvp.Key, out vi))
				{
					if (kvp.Value.Type == ValueType.COMPLEX)
						(vi as FeatureValues).Add(kvp.Value as FeatureValues);
				}
				else
				{
					m_values[kvp.Key] = kvp.Value;
				}
			}
		}

		/// <summary>
		/// Gets the values for the specified feature.
		/// </summary>
		/// <param name="feature">The feature.</param>
		/// <returns>All values.</returns>
		public ValueInstance GetValues(Feature feature)
		{
			ValueInstance value;
			if (m_values.TryGetValue(feature, out value))
				return value;
			return null;
		}

		/// <summary>
		/// Determines whether this set of feature values contains the specified feature.
		/// </summary>
		/// <param name="feature">The feature.</param>
		/// <returns>
		/// 	<c>true</c> if this set contains the specified feature, otherwise <c>false</c>.
		/// </returns>
		public bool ContainsFeature(Feature feature)
		{
			ValueInstance value;
			if (!m_values.TryGetValue(feature, out value))
				return false;

			FeatureValues fvs = value as FeatureValues;
			foreach (Feature sf in feature.SubFeatures)
			{
				if (fvs == null || !fvs.ContainsFeature(sf))
					return false;
			}
			return true;
		}

		/// <summary>
		/// Unifies this set of feature values with the specified set of feature values.
		/// </summary>
		/// <param name="fv">The feature values.</param>
		/// <param name="output">The unification.</param>
		/// <returns><c>true</c> if the feature values could be unified, otherwise <c>false</c>.</returns>
		public bool Unify(FeatureValues fv, out FeatureValues output)
		{
			output = fv.Clone();
			foreach (KeyValuePair<Feature, ValueInstance> kvp in m_values)
			{
				ValueInstance value;
				if (fv.m_values.TryGetValue(kvp.Key, out value))
				{
					ValueInstance vi = null;
					if (kvp.Value != null && !kvp.Value.Unify(value, out vi))
					{
						output = null;
						return false;
					}
					output.m_values[kvp.Key] = vi;
				}
				else
				{
					output.m_values[kvp.Key] = kvp.Value;
				}
			}

			return true;
		}

		public override bool Unify(ValueInstance other, out ValueInstance output)
		{
			FeatureValues fvs;
			if (Unify(other as FeatureValues, out fvs))
			{
				output = fvs;
				return true;
			}
			else
			{
				output = null;
				return false;
			}
		}

		/// <summary>
		/// Unifies this set of feature values with the specified set of feature values. If the specified
		/// set of feature values does not contain a feature specified in this set of feature values, then the
		/// default value will be used for that feature.
		/// </summary>
		/// <param name="fv">The feature values.</param>
		/// <param name="output">The unification.</param>
		/// <returns><c>true</c> if the feature values could be unified, otherwise <c>false</c>.</returns>
		public bool UnifyDefaults(FeatureValues fv, out FeatureValues output)
		{
			output = fv.Clone();
			foreach (KeyValuePair<Feature, ValueInstance> kvp in m_values)
			{
				ValueInstance value;
				if (fv.m_values.TryGetValue(kvp.Key, out value))
				{
					ValueInstance vi = null;
					if (kvp.Value != null && !kvp.Value.UnifyDefaults(value, out vi))
					{
						output = null;
						return false;
					}
					output.m_values[kvp.Key] = vi;
				}
				else if (kvp.Key.DefaultValue != null)
				{
					ValueInstance vi = null;
					if (kvp.Value != null && !kvp.Value.UnifyDefaults(kvp.Key.DefaultValue, out vi))
					{
						output = null;
						return false;
					}
					output.m_values[kvp.Key] = vi;
				}
				else
				{
					output.m_values[kvp.Key] = kvp.Value;
				}
			}

			return true;
		}

		public override bool UnifyDefaults(ValueInstance other, out ValueInstance output)
		{
			FeatureValues fvs;
			if (UnifyDefaults(other as FeatureValues, out fvs))
			{
				output = fvs;
				return true;
			}
			else
			{
				output = null;
				return false;
			}
		}

		/// <summary>
		/// Determines whether the specified feature values set matches this set of feature
		/// values. For each feature in this set, there must be a value which belongs to
		/// the list of values in the specified set.
		/// </summary>
		/// <param name="fv">The feature values.</param>
		/// <returns>
		/// 	<c>true</c> if the sets match, otherwise <c>false</c>.
		/// </returns>
		public bool IsMatch(FeatureValues fv)
		{
			foreach (KeyValuePair<Feature, ValueInstance> kvp in m_values)
			{
				ValueInstance value;
				if (!fv.m_values.TryGetValue(kvp.Key, out value))
					return false;

				if (kvp.Value != null && !kvp.Value.IsMatch(value))
					return false;
			}
			return true;
		}

		public override bool IsMatch(ValueInstance other)
		{
			return IsMatch(other as FeatureValues);
		}

		/// <summary>
		/// Determines whether the specified set of feature values is compatible with this
		/// set of feature values. It is much like <c>IsMatch</c> except that if a the
		/// specified set does not contain a feature in this set, it is still a match.
		/// It basically checks to make sure that there is no contradictory features.
		/// </summary>
		/// <param name="fv">The feature values.</param>
		/// <returns>
		/// 	<c>true</c> the sets are compatible, otherwise <c>false</c>.
		/// </returns>
		public bool IsCompatible(FeatureValues fv)
		{
			foreach (KeyValuePair<Feature, ValueInstance> kvp in m_values)
			{
				ValueInstance value;
				if (!fv.m_values.TryGetValue(kvp.Key, out value))
					continue;

				if (kvp.Value != null && !kvp.Value.IsCompatible(value))
					return false;
			}
			return true;
		}

		public override bool IsCompatible(ValueInstance other)
		{
			return IsCompatible(other as FeatureValues);
		}

		/// <summary>
		/// Gets the difference between this subset and the specified superset. If this set is
		/// not a subset of the specified superset, it will return <c>false</c>.
		/// </summary>
		/// <param name="superset">The superset feature values.</param>
		/// <param name="remainder">The remainder.</param>
		/// <returns><c>true</c> if this is a subset, otherwise <c>false</c>.</returns>
		public bool GetSupersetRemainder(FeatureValues superset, out FeatureValues remainder)
		{
			FeatureValues result = superset.Clone();
			foreach (KeyValuePair<Feature, ValueInstance> kvp in m_values)
			{
				ValueInstance value;
				if (kvp.Value != null && (!result.m_values.TryGetValue(kvp.Key, out value) || !value.Equals(kvp.Value)))
				{
					remainder = null;
					return false;
				}

				result.m_values.Remove(kvp.Key);
			}

			remainder = result;
			return true;
		}

		public FeatureValues Clone()
		{
			return new FeatureValues(this);
		}

		object ICloneable.Clone()
		{
			return this.Clone();
		}

		public override int GetHashCode()
		{
			int hashCode = 0;
			foreach (KeyValuePair<Feature, ValueInstance> kvp in m_values)
			{
				hashCode ^= kvp.Key.GetHashCode() ^ (kvp.Value != null ? kvp.Value.GetHashCode() : 0);
			}
			return hashCode;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			return Equals(obj as FeatureValues);
		}

		public bool Equals(FeatureValues other)
		{
			if (other == null)
				return false;

			if (m_values.Count != other.m_values.Count)
				return false;

			foreach (KeyValuePair<Feature, ValueInstance> kvp in m_values)
			{
				ValueInstance value;
				if (!other.m_values.TryGetValue(kvp.Key, out value))
					return false;

				if (kvp.Value != null && !kvp.Value.Equals(value))
					return false;
			}

			return true;
		}

		public override string ToString()
		{
			bool firstFeature = true;
			StringBuilder sb = new StringBuilder();
			foreach (KeyValuePair<Feature, ValueInstance> kvp in m_values)
			{
				if (!firstFeature)
					sb.Append(", ");
				sb.Append(kvp.Key.Description);
				if (kvp.Value != null)
				{
					sb.Append("->(");
					sb.Append(kvp.Value.ToString());
					sb.Append(")");
				}
				firstFeature = false;
			}

			return sb.ToString();
		}
	}

	/// <summary>
	/// This class represents a feature system. It encapsulates all of the valid features and values.
	/// </summary>
	public class FeatureSystem
	{
		HCObjectSet<Feature> m_features;
		HCObjectSet<FeatureValue> m_values;

		/// <summary>
		/// Initializes a new instance of the <see cref="FeatureSystem"/> class.
		/// </summary>
		public FeatureSystem()
		{
			m_features = new HCObjectSet<Feature>();
			m_values = new HCObjectSet<FeatureValue>();
		}

		/// <summary>
		/// Gets the features.
		/// </summary>
		/// <value>The features.</value>
		public IEnumerable<Feature> Features
		{
			get
			{
				return m_features;
			}
		}

		public int NumFeatures
		{
			get
			{
				return m_features.Count;
			}
		}

		/// <summary>
		/// Gets the feature values.
		/// </summary>
		/// <value>The values.</value>
		public IEnumerable<FeatureValue> Values
		{
			get
			{
				return m_values;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this feature system has features.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance has features, otherwise <c>false</c>.
		/// </value>
		public bool HasFeatures
		{
			get
			{
				return m_features.Count > 0;
			}
		}

		/// <summary>
		/// Gets the feature associated with the specified ID.
		/// </summary>
		/// <param name="id">The ID.</param>
		/// <returns>The feature.</returns>
		public Feature GetFeature(string id)
		{
			Feature feature;
			if (m_features.TryGetValue(id, out feature))
				return feature;
			return null;
		}

		/// <summary>
		/// Gets the feature value associated with the specified ID.
		/// </summary>
		/// <param name="id">The ID.</param>
		/// <returns>The feature value.</returns>
		public FeatureValue GetValue(string id)
		{
			FeatureValue value;
			if (m_values.TryGetValue(id, out value))
				return value;
			return null;
		}

		/// <summary>
		/// Adds the feature.
		/// </summary>
		/// <param name="feature">The feature.</param>
		public void AddFeature(Feature feature)
		{
			m_features.Add(feature);
		}

		/// <summary>
		/// Adds the feature value.
		/// </summary>
		/// <param name="value">The feature value.</param>
		/// <exception cref="System.InvalidOperationException">Thrown when the current number of feature values is equal to the maximum
		/// and this feature system does not contain the specified value.</exception>
		public void AddValue(FeatureValue value)
		{
			if (!m_values.Contains(value) && m_values.Count == FeatureBundle.MAX_NUM_VALUES)
				throw new InvalidOperationException(HCStrings.kstidTooManyFeatValues);

			value.FeatureBundleIndex = m_values.Count;
			m_values.Add(value);
		}

		/// <summary>
		/// Resets this instance.
		/// </summary>
		public void Reset()
		{
			m_features.Clear();
			m_values.Clear();
		}
	}
}
