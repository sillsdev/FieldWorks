using System;
using System.Collections.Generic;
using System.Text;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents a morphological/phonological rule feature. It is used to restrict
	/// the application of rules for exception cases.
	/// </summary>
	public class MPRFeature : HCObject
	{
		MPRFeatureGroup m_group = null;

		public MPRFeature(string id, string desc, Morpher morpher)
			: base(id, desc, morpher)
		{
		}

		/// <summary>
		/// Gets or sets the MPR feature group.
		/// </summary>
		/// <value>The group.</value>
		public MPRFeatureGroup Group
		{
			get
			{
				return m_group;
			}

			internal set
			{
				m_group = value;
			}
		}
	}

	/// <summary>
	/// This class represents a group of related MPR features.
	/// </summary>
	public class MPRFeatureGroup : HCObject
	{
		/// <summary>
		/// The matching type
		/// </summary>
		public enum GroupMatchType
		{
			/// <summary>
			/// when any features match within the group
			/// </summary>
			ANY,
			/// <summary>
			/// only if all features match within the group
			/// </summary>
			ALL
		}

		/// <summary>
		/// The outputting type
		/// </summary>
		public enum GroupOutputType
		{
			/// <summary>
			/// overwrites all existing features in the same group
			/// </summary>
			OVERWRITE,
			/// <summary>
			/// appends features
			/// </summary>
			APPEND
		}

		GroupMatchType m_matchType = GroupMatchType.ANY;
		GroupOutputType m_outputType = GroupOutputType.OVERWRITE;
		HCObjectSet<MPRFeature> m_mprFeatures;

		public MPRFeatureGroup(string id, string desc, Morpher morpher)
			: base(id, desc, morpher)
		{
			m_mprFeatures = new HCObjectSet<MPRFeature>();
		}

		/// <summary>
		/// Gets or sets the type of matching that is used for MPR features in this group.
		/// </summary>
		/// <value>The type of matching.</value>
		public GroupMatchType MatchType
		{
			get
			{
				return m_matchType;
			}

			set
			{
				m_matchType = value;
			}
		}

		/// <summary>
		/// Gets or sets the type of outputting that is used for MPR features in this group.
		/// </summary>
		/// <value>The type of outputting.</value>
		public GroupOutputType OutputType
		{
			get
			{
				return m_outputType;
			}

			set
			{
				m_outputType = value;
			}
		}

		/// <summary>
		/// Gets the MPR features.
		/// </summary>
		/// <value>The MPR features.</value>
		public IEnumerable<MPRFeature> Features
		{
			get
			{
				return m_mprFeatures;
			}
		}

		/// <summary>
		/// Adds the MPR feature.
		/// </summary>
		/// <param name="mprFeature">The MPR feature.</param>
		public void Add(MPRFeature mprFeature)
		{
			mprFeature.Group = this;
			m_mprFeatures.Add(mprFeature);
		}

		/// <summary>
		/// Determines whether this group contains the specified MPR feature.
		/// </summary>
		/// <param name="mprFeature">The MPR feature.</param>
		/// <returns>
		/// 	<c>true</c> if this group contains the feature, otherwise <c>false</c>.
		/// </returns>
		public bool Contains(MPRFeature mprFeature)
		{
			return m_mprFeatures.Contains(mprFeature);
		}
	}

	/// <summary>
	/// This represents a set of MPR features.
	/// </summary>
	public class MPRFeatureSet : HCObjectSet<MPRFeature>, ICloneable
	{

		public MPRFeatureSet()
		{
		}

		public MPRFeatureSet(MPRFeatureSet mprFeats)
			: base(mprFeats)
		{
		}

		public IEnumerable<MPRFeatureGroup> Groups
		{
			get
			{
				HCObjectSet<MPRFeatureGroup> groups = new HCObjectSet<MPRFeatureGroup>();
				foreach (MPRFeature feat in this)
				{
					if (feat.Group != null)
						groups.Add(feat.Group);
				}
				return groups;
			}
		}

		public void AddOutput(MPRFeatureSet mprFeats)
		{
			foreach (MPRFeatureGroup group in mprFeats.Groups)
			{
				if (group.OutputType == MPRFeatureGroup.GroupOutputType.OVERWRITE)
				{
					foreach (MPRFeature mprFeat in group.Features)
					{
						if (!mprFeats.Contains(mprFeat))
							Remove(mprFeat);
					}
				}
			}

			AddMany(mprFeats);
		}

		public bool IsMatch(MPRFeatureSet mprFeats)
		{
			foreach (MPRFeatureGroup group in Groups)
			{
				bool match = true;
				foreach (MPRFeature feat in group.Features)
				{
					if (Contains(feat))
					{
						if (group.MatchType == MPRFeatureGroup.GroupMatchType.ALL)
						{
							if (!mprFeats.Contains(feat))
							{
								match = false;
								break;
							}
						}
						else
						{
							if (mprFeats.Contains(feat))
							{
								match = true;
								break;
							}
							else
							{
								match = false;
							}
						}
					}
				}

				if (!match)
					return false;
			}

			foreach (MPRFeature feat in this)
			{
				if (feat.Group == null && !mprFeats.Contains(feat))
					return false;
			}
			return true;
		}

		object ICloneable.Clone()
		{
			return this.Clone();
		}

		public MPRFeatureSet Clone()
		{
			return new MPRFeatureSet(this);
		}
	}
}
