// Copyright (c) 2005-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: CmTranslation.cs
// Responsibility: FW Team

using System;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Common.COMInterfaces;
using System.Collections.Generic;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.DomainImpl
{
	/// <summary>
	///
	/// </summary>
	internal partial class CmTranslation
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Validates the type RA.
		/// </summary>
		/// <param name="newObjValue">The new obj value.</param>
		/// ------------------------------------------------------------------------------------
		partial void ValidateTypeRA(ref ICmPossibility newObjValue)
		{
			// For Flex Example Sentences, it's quite legal for the user to mark the type as
			// unknown/empty/null.  See FWR-549.
			if (newObjValue == null && OwningFlid != LexExampleSentenceTags.kflidTranslations)
				throw new ArgumentException("New value must not be null.");

			// This check only applies if the translation belongs to a paragraph.
			if (Cache.FullyInitializedAndReadyToRock && Owner is IStTxtPara &&
				newObjValue.Guid != LangProjectTags.kguidTranBackTranslation)
			{
				throw new ArgumentException("Back translations are the only type of translation allowed for paragraphs");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overridden to handle ref props of this class.
		/// </summary>
		/// <param name="flid"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			switch (flid)
			{
				case CmTranslationTags.kflidType:
					return m_cache.LangProject.TranslationTagsOA;
				default:
					return base.ReferenceTargetOwner(flid);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Subclasses should override this, if they need to have side effects.
		/// </summary>
		/// <param name="multiAltFlid"></param>
		/// <param name="alternativeWs"></param>
		/// <param name="originalValue"></param>
		/// <param name="newValue"></param>
		/// ------------------------------------------------------------------------------------
		protected override void ITsStringAltChangedSideEffectsInternal(int multiAltFlid,
			IWritingSystem alternativeWs, ITsString originalValue, ITsString newValue)
		{
			base.ITsStringAltChangedSideEffectsInternal(multiAltFlid, alternativeWs, originalValue, newValue);

			// Make sure the translation belongs to Scripture.
			ScrTxtPara para = Owner as ScrTxtPara;
			if (para == null || TypeRA == null)
				return;

			if (multiAltFlid == CmTranslationTags.kflidTranslation &&
				TypeRA.Guid == CmPossibilityTags.kguidTranBackTranslation &&
				((originalValue == null && newValue != null) ||
				(originalValue != null && newValue == null) ||
				(originalValue != null && originalValue.Text != newValue.Text)))
			{
				BtConverter.ConvertCmTransToInterlin(para, alternativeWs.Handle);
				MarkAsUnfinished(alternativeWs.Handle);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Marks all the translations as unfinished.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void MarkAsUnfinished()
		{
			foreach (int ws in Status.AvailableWritingSystemIds)
				MarkAsUnfinished(ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Marks the translation for the specified writing system as unfinished.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void MarkAsUnfinished(int ws)
		{
			// set the status to unfinished
			ITsString state = Status.get_String(ws);
			if (state != null && state.Text != null)
				Status.set_String(ws, BackTranslationStatus.Unfinished.ToString());
		}

		/// <summary>
		/// Set the type of the translation to Free Translation (a reasonable default).
		/// This method is invoked when creating a real CmTranslation from a ghost.
		/// It is called by reflection.
		/// </summary>
		public void SetTypeToFreeTrans()
		{
			TypeRA = m_cache.ServiceLocator.GetInstance<ICmPossibilityRepository>().GetObject(CmPossibilityTags.kguidTranFreeTranslation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a set of all writing systems used for this translation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public HashSet<IWritingSystem> AvailableWritingSystems
		{
			get
			{
				var wses = new HashSet<IWritingSystem>();
				foreach (int hvoWs in Translation.AvailableWritingSystemIds)
					wses.Add(Services.WritingSystemManager.Get(hvoWs));
				return wses;
			}
		}

		#region ICloneableCmObject Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clones this instance.
		/// </summary>
		/// <param name="clone"></param>
		/// ------------------------------------------------------------------------------------
		public void SetCloneProperties(ICmObject clone)
		{
			ICmTranslation clonedTrans = (ICmTranslation)clone;
			clonedTrans.TypeRA = TypeRA; // Must be set before copying the alternatives
			clonedTrans.Translation.CopyAlternatives(Translation);
			clonedTrans.Status.CopyAlternatives(Status);
		}
		#endregion
	}
}