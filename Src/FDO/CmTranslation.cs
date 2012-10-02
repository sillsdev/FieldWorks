// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.
// <copyright from='2005' to='2005' company='SIL International'>
//		Copyright (c) 2005, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: CmTranslation.cs
// Responsibility: Edge
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections;

using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.FDO.Cellar
{
	#region Back Translation Paragraph States
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Status information for back translation paragraphs
	/// </summary>
	/// ------------------------------------------------------------------------------------
	public enum BackTranslationStatus
	{
		/// <summary>
		/// The vernacular paragraph has been edited since the back translation or the
		/// back translation has been edited but the translator has not marked it as
		/// finished yet.
		/// </summary>
		Unfinished,
		/// <summary>
		/// The translator marks each back translation paragraph as finished when
		///	they are done.
		/// </summary>
		Finished,
		/// <summary>
		/// The paragraph is finished and has been consultant checked.
		/// </summary>
		Checked
	}
	#endregion

	/// <summary>
	///
	/// </summary>
	public partial class CmTranslation
	{
		/// <summary>
		/// Overridden to handle ref props of this class.
		/// </summary>
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			switch (flid)
			{
				case (int)CmTranslation.CmTranslationTags.kflidType:
					return m_cache.LangProject.TranslationTagsOA;
				default:
					return base.ReferenceTargetOwner(flid);
			}
		}

		/// <summary>
		/// Set the type of the translation to Free Translation (a reasonable default).
		/// This method is invoked when creating a real CmTranslation from a ghost.
		/// It is called by reflection.
		/// </summary>
		public void SetTypeToFreeTrans()
		{
			this.TypeRAHvo = m_cache.GetIdFromGuid(LangProj.LangProject.kguidTranFreeTranslation);
		}
	}
}
