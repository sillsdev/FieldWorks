// Copyright (c) 2014-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Linq;
using LanguageExplorer.Controls.DetailControls;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	/// Subclass VectorReferenceView to support deleting from the (virtual) Complex Forms property and similar.
	/// </summary>
	internal class EntrySequenceVectorReferenceView: VectorReferenceView
	{
		/// <summary />
		protected override void RemoveObjectFromList(int[] hvosOld, int ihvo, string undoText, string redoText)
		{
			if (!Cache.MetaDataCacheAccessor.get_IsVirtual(m_rootFlid))
			{
				base.RemoveObjectFromList(hvosOld, ihvo, undoText, redoText);
				return;
			}

			if (Cache.MetaDataCacheAccessor.GetFieldName(m_rootFlid) != "ComplexFormEntries")
			{
				return;
			}
			var startHeight = m_rootb.Height;
			UndoableUnitOfWorkHelper.Do(undoText, redoText, m_rootObj,
				() =>
				{
					var complex = m_rootObj.Services.GetInstance<ILexEntryRepository>().GetObject(hvosOld[ihvo]);
					// the selected object in the list is a complex entry which has this as one of its components.
					// We want to remove this from its components.
					var ler = complex.EntryRefsOS.First(item => item.RefType == LexEntryRefTags.krtComplexForm);
					ler.PrimaryLexemesRS.Remove(m_rootObj);
					ler.ShowComplexFormsInRS.Remove(m_rootObj);
					ler.ComponentLexemesRS.Remove(m_rootObj);
				});
			if (m_rootb != null)
			{
				CheckViewSizeChanged(startHeight, m_rootb.Height);
				// Redisplay (?) the vector property.
				m_rootb.SetRootObject(m_rootObj.Hvo, m_VectorReferenceVc, kfragTargetVector, m_rootb.Stylesheet);
			}
		}

		/// <summary>
		/// The user has attempted to delete something which the system does not inherently
		/// know how to delete.  If the problem is a "complex range", then try to delete one
		/// object from the vector displayed in the entry sequence.
		/// </summary>
		public override VwDelProbResponse OnProblemDeletion(IVwSelection sel, VwDelProbType dpt)
		{
			if (dpt != VwDelProbType.kdptComplexRange)
			{
				return base.OnProblemDeletion(sel, dpt);
			}
			var helper = SelectionHelper.GetSelectionInfo(sel, this);
			var clev = helper.NumberOfLevels;
			var rginfo = helper.LevelInfo;
			var info = rginfo[clev - 1];
			ICmObject cmo;
			if (info.tag != m_rootFlid || !m_cache.ServiceLocator.ObjectRepository.TryGetObject(info.hvo, out cmo))
			{
				return base.OnProblemDeletion(sel, dpt);
			}
			var sda = m_cache.DomainDataByFlid as ISilDataAccessManaged;
			Debug.Assert(sda != null);
			var rghvos = sda.VecProp(m_rootObj.Hvo, m_rootFlid);
			var ihvo = -1;
			for (var i = 0; i < rghvos.Length; ++i)
			{
				if (rghvos[i] == cmo.Hvo)
				{
					ihvo = i;
					break;
				}
			}

			if (ihvo < 0)
			{
				return base.OnProblemDeletion(sel, dpt);
			}
			var startHeight = m_rootb.Height;
			if (Cache.MetaDataCacheAccessor.get_IsVirtual(m_rootFlid))
			{
				var obj = m_cache.ServiceLocator.GetObject(rghvos[ihvo]);
				ILexEntryRef ler = null;
				if (obj is ILexEntry)
				{
					var complex = (ILexEntry)obj;
					// the selected object in the list is a complex entry which has this as one of
					// its components.  We want to remove this from its components.
					foreach (var item in complex.EntryRefsOS)
					{
						switch (item.RefType)
						{
							case LexEntryRefTags.krtComplexForm:
							case LexEntryRefTags.krtVariant:
								ler = item;
								break;
							default:
								throw new Exception("Unexpected LexEntryRef type in EntrySequenceVectorReferenceView.OnProblemDeletion");
						}
					}
				}
				else if (obj is ILexEntryRef)
				{
					ler = (ILexEntryRef)obj;
				}
				else
				{
					return VwDelProbResponse.kdprAbort; // we don't know how to delete it.
				}
				var fieldName = m_cache.MetaDataCacheAccessor.GetFieldName(m_rootFlid);
				switch (fieldName)
				{
					case "Subentries":
						ler.PrimaryLexemesRS.Remove(m_rootObj);
						break;
					case "VisibleComplexFormEntries":
					case "VisibleComplexFormBackRefs":
						ler.ShowComplexFormsInRS.Remove(m_rootObj);
						break;
					case "VariantFormEntries":
						ler.ComponentLexemesRS.Remove(m_rootObj);
						break;
				}
			}
			else
			{
				sda.Replace(m_rootObj.Hvo, m_rootFlid, ihvo, ihvo + 1, new int[0], 0);
			}
			if (m_rootb != null)
			{
				CheckViewSizeChanged(startHeight, m_rootb.Height);
				// Redisplay (?) the vector property.
				m_rootb.SetRootObject(m_rootObj.Hvo, m_VectorReferenceVc, kfragTargetVector, m_rootb.Stylesheet);
			}
			return VwDelProbResponse.kdprDone;
		}
	}
}