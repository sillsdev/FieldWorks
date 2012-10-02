// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2002, SIL International. All Rights Reserved.
// <copyright from='2002' to='2002' company='SIL International'>
//		Copyright (c) 2002, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ScrBookRef.cs
// Responsibility: TomB
// Last reviewed:
//
// <remarks>
//
// </remarks>
// --------------------------------------------------------------------------------------------
using System;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.ScriptureUtils;

namespace SIL.FieldWorks.FDO.Scripture
{
	/// <summary>
	/// Summary description for ScrBookRef.
	/// </summary>
	public partial class ScrBookRef : CmObject
	{
		#region Custom Tags and Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the standard abbreviation of the book in the UI writing system. If no abbrev is
		/// available in the UI writing system, try the current analysis languages and English.
		/// If still no abbrev is available, return the UBS 3-letter book code.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string UIBookAbbrev
		{
			get
			{
				ILgWritingSystemFactory wsf = m_cache.LanguageWritingSystemFactoryAccessor;;
				string sBookAbbrev = BookAbbrev.GetAlternative(wsf.UserWs);

				// Try for the current analysis languages and English.
				if (sBookAbbrev == null || sBookAbbrev == String.Empty)
					sBookAbbrev = BookAbbrev.BestAnalysisAlternative.Text;

				// UBS book code
				if (sBookAbbrev == null || sBookAbbrev == String.Empty || sBookAbbrev == "***")
					sBookAbbrev = ScrReference.NumberToBookCode(IndexInOwner + 1);
				System.Diagnostics.Debug.Assert(sBookAbbrev != null && sBookAbbrev != String.Empty);

				return sBookAbbrev.Trim();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the standard name of the book in the UI writing system. If no name is available
		/// in the UI writing system, try the current analysis languages and English. If still
		/// no name is available, return the UBS 3-letter book code.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string UIBookName
		{
			get
			{
				ILgWritingSystemFactory wsf = m_cache.LanguageWritingSystemFactoryAccessor;;
				string sBookName = BookName.GetAlternative(wsf.UserWs);

				// Try for the current analysis languages and English.
				if (sBookName == null || sBookName == String.Empty)
					sBookName = BookName.BestAnalysisAlternative.Text;

				// UBS code, if all else fails.
				if (sBookName == null || sBookName == String.Empty || sBookName == "***")
					sBookName = ScrReference.NumberToBookCode(IndexInOwner + 1);
				System.Diagnostics.Debug.Assert(sBookName != null && sBookName != String.Empty);

				return sBookName.Trim();
			}
		}
		#endregion
	}
}
