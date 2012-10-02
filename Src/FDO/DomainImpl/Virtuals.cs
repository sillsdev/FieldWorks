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

		internal Virtuals(IFwMetaDataCacheManaged mdc)
		{
			m_mdc = mdc;
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
	}
}
