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
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.FDO.DomainImpl
{
	internal partial class ScrRefSystem
	{
		internal void Initialize()
		{
			IScrBookRefFactory scrBookRefFactory = Cache.ServiceLocator.GetInstance<IScrBookRefFactory>();
			for (int i = 0; i < 66; i++)
				BooksOS.Add(scrBookRefFactory.Create());
		}
	}

	/// <summary>
	/// Summary description for ScrBookRef.
	/// </summary>
	internal partial class ScrBookRef
	{
		#region Custom Tags and Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the standard abbreviation of the book in the UI writing system. If no abbrev is
		/// available in the UI writing system, try the current analysis languages and English.
		/// If still no abbrev is available, return the UBS 3-letter book code.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string UIBookAbbrev
		{
			get
			{
				int wsHvo = WritingSystemServices.FallbackUserWs(m_cache);
				string sBookAbbrev = BookAbbrev.get_String(wsHvo).Text;

				// Try for the current analysis languages and English.
				if (string.IsNullOrEmpty(sBookAbbrev))
					sBookAbbrev = BookAbbrev.BestAnalysisAlternative.Text;

				// UBS book code
				if (string.IsNullOrEmpty(sBookAbbrev) || sBookAbbrev == BookAbbrev.NotFoundTss.Text)
					sBookAbbrev = ScrReference.NumberToBookCode(IndexInOwner + 1);
				System.Diagnostics.Debug.Assert(sBookAbbrev != null && sBookAbbrev != String.Empty);

				return sBookAbbrev.Trim();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the standard name of the book in the UI writing system. If no name is available
		/// in the UI writing system, try the current analysis languages and English. If still
		/// no name is available, return the UBS 3-letter book code.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string UIBookName
		{
			get
			{
				int wsHvo = WritingSystemServices.FallbackUserWs(m_cache);
				string sBookName = BookName.get_String(wsHvo).Text;

				// Try for the current analysis languages and English.
				if (string.IsNullOrEmpty(sBookName))
					sBookName = BookName.BestAnalysisAlternative.Text;

				// UBS code, if all else fails.
				if (string.IsNullOrEmpty(sBookName) || sBookName == BookName.NotFoundTss.Text)
					sBookName = ScrReference.NumberToBookCode(IndexInOwner + 1);
				System.Diagnostics.Debug.Assert(sBookName != null && sBookName != String.Empty);

				return sBookName.Trim();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"/> that represents this instance.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return UIBookName;
		}

		#endregion
	}
}
