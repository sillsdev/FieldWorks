// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2008' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: Virtuals.cs
// Responsibility: FW Team
// --------------------------------------------------------------------------------------------
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.FDO.DomainImpl // TODO: Move this to DomainServices
{
	/// <summary>
	/// This class exposes the flids of various virtual properties.
	/// </summary>
	public class Virtuals
	{
		private IFwMetaDataCacheManaged m_mdc;
		private int m_lexDbEntries;
		private int m_stParaIsFinalParaInText;
		private int m_langProjectAllWordforms;
		private int m_lexEntryMLHeadWord;

		internal Virtuals(IFwMetaDataCacheManaged mdc)
		{
			m_mdc = mdc;
		}

		/// <summary>
		/// The Flic for the LexEntry.MLHeadWord property.
		/// </summary>
		public int LexEntryMLHeadWord
		{
			get
			{
				if (m_lexEntryMLHeadWord == 0)
					m_lexEntryMLHeadWord = m_mdc.GetFieldId("LexEntry", "MLHeadWord", false);
				return m_lexEntryMLHeadWord;
			}
		}

		/// <summary>
		/// The Flid for the LexDb.Entries virtual property.
		/// </summary>
		public int LexDbEntries
		{
			get
			{
				if (m_lexDbEntries == 0)
					m_lexDbEntries = m_mdc.GetFieldId2(LexDbTags.kClassId, "Entries", false);
				return m_lexDbEntries;
			}
		}

		/// <summary>
		/// The Flid for the StPara.IsFinalParaInText virtual property.
		/// </summary>
		public int StParaIsFinalParaInText
		{
			get
			{
				if (m_stParaIsFinalParaInText == 0)
					m_stParaIsFinalParaInText = m_mdc.GetFieldId2(StParaTags.kClassId, "IsFinalParaInText", false);
				return m_stParaIsFinalParaInText;
			}
		}

		/// <summary>
		/// The Flid for the LangProject.AllWordforms virtual property.
		/// </summary>
		public int LangProjectAllWordforms
		{
			get
			{
				if (m_langProjectAllWordforms == 0)
					m_langProjectAllWordforms = m_mdc.GetFieldId2(LangProjectTags.kClassId, "AllWordforms", false);
				return m_langProjectAllWordforms;
			}
		}

		private int m_lexEntrySubentries;

		/// <summary>
		/// The Flid for the LexEntry.Subentries virtual property.
		/// </summary>
		public int LexEntrySubentries
		{
			get
			{
				if (m_lexEntrySubentries == 0)
					m_lexEntrySubentries = m_mdc.GetFieldId2(LexEntryTags.kClassId, "Subentries", false);
				return m_lexEntrySubentries;
			}
		}

		private int m_lexEntryHeadWordRef;

		/// <summary>
		/// The Flid for the LexEntry.HeadWordRef virtual property.
		/// </summary>
		public int LexEntryHeadWordRef
		{
			get
			{
				if (m_lexEntryHeadWordRef == 0)
					m_lexEntryHeadWordRef = m_mdc.GetFieldId2(LexEntryTags.kClassId, "HeadWordRef", false);
				return m_lexEntryHeadWordRef;
			}
		}

		private int m_lexEntryHeadWordReversal;

		/// <summary>
		/// The Flid for the LexEntry.HeadWordReversal virtual property.
		/// </summary>
		public int LexEntryHeadWordReversal
		{
			get
			{
				if (m_lexEntryHeadWordReversal == 0)
					m_lexEntryHeadWordReversal = m_mdc.GetFieldId2(LexEntryTags.kClassId, "HeadWordReversal", false);
				return m_lexEntryHeadWordReversal;
			}
		}

		private int m_lexSenseReversalName;

		/// <summary>
		/// The Flid for the LexEntry.HeadWordReversal virtual property.
		/// </summary>
		public int LexSenseReversalName
		{
			get
			{
				if (m_lexEntryHeadWordReversal == 0)
					m_lexEntryHeadWordReversal = m_mdc.GetFieldId2(LexSenseTags.kClassId, "ReversalName", false);
				return m_lexEntryHeadWordReversal;
			}
		}

		private int m_lexEntryComplexFormEntries;

		/// <summary>
		/// The Flid for the LexEntry.ComplexFormEntries virtual property.
		/// </summary>
		public int LexEntryComplexFormEntries
		{
			get
			{
				if (m_lexEntryComplexFormEntries == 0)
					m_lexEntryComplexFormEntries = m_mdc.GetFieldId2(LexEntryTags.kClassId, "ComplexFormEntries", false);
				return m_lexEntryComplexFormEntries;
			}
		}

		private int m_lexEntryVisibleComplexFormEntries;

		/// <summary>
		/// The Flid for the LexEntry.VisibleComplexFormEntries virtual property.
		/// </summary>
		public int LexEntryVisibleComplexFormEntries
		{
			get
			{
				if (m_lexEntryVisibleComplexFormEntries == 0)
					m_lexEntryVisibleComplexFormEntries = m_mdc.GetFieldId2(LexEntryTags.kClassId, "VisibleComplexFormEntries", false);
				return m_lexEntryVisibleComplexFormEntries;
			}
		}
		private int m_lexEntryVisibleComplexFormBackRefs;

		/// <summary>
		/// The Flid for the LexEntry.VisibleComplexFormBackRefs virtual property.
		/// </summary>
		public int LexEntryVisibleComplexFormBackRefs
		{
			get
			{
				if (m_lexEntryVisibleComplexFormBackRefs == 0)
					m_lexEntryVisibleComplexFormBackRefs = m_mdc.GetFieldId2(LexEntryTags.kClassId, "VisibleComplexFormBackRefs", false);
				return m_lexEntryVisibleComplexFormBackRefs;
			}
		}
		private int m_lexSenseVisibleComplexFormBackRefs;

		/// <summary>
		/// The Flid for the LexSense.VisibleComplexFormBackRefs virtual property.
		/// </summary>
		public int LexSenseVisibleComplexFormBackRefs
		{
			get
			{
				if (m_lexSenseVisibleComplexFormBackRefs == 0)
					m_lexSenseVisibleComplexFormBackRefs = m_mdc.GetFieldId2(LexSenseTags.kClassId, "VisibleComplexFormBackRefs", false);
				return m_lexSenseVisibleComplexFormBackRefs;
			}
		}
	}
}
