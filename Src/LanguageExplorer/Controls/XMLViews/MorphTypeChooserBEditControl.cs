// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Widgets;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// This class implements setting the MorphType of the MoForm belonging to the LexemeForm
	/// field of a LexEntry.
	/// </summary>
	internal class MorphTypeChooserBEditControl : FlatListChooserBEditControl
	{
		protected int m_flidParent;
		BrowseViewer m_containingViewer;

		//int flidAtomicProp, int hvoList, int ws, bool useAbbr
		public MorphTypeChooserBEditControl(int flid, int subflid, int hvoList, int ws, BrowseViewer viewer)
			: base(subflid, hvoList, ws, false)
		{
			m_flidParent = flid;
			m_containingViewer = viewer;
		}

		#region IBulkEditSpecControl Members (overrides)

		public override List<int> FieldPath
		{
			get
			{
				var fieldPath = base.FieldPath;
				fieldPath.Insert(0, m_flidParent);
				return fieldPath;
			}
		}

		public override void DoIt(IEnumerable<int> itemsToChange, ProgressState state)
		{
			UndoableUnitOfWorkHelper.Do(XMLViewsStrings.ksUndoBulkEdit, XMLViewsStrings.ksRedoBulkEdit, m_cache.ActionHandlerAccessor,
				() =>
				{
					var sda = m_cache.DomainDataByFlid;
					var item = m_combo.SelectedItem as HvoTssComboItem;
					if (item == null)
					{
						return;
					}
					var hvoSelMorphType = item.Hvo;
					var fSelAffix = false;
					if (hvoSelMorphType != 0)
					{
						fSelAffix = MorphServices.IsAffixType(m_cache, hvoSelMorphType);
					}
					var fAnyFundamentalChanges = false;
					// Preliminary check and warning if changing fundamental type.
					foreach (int hvoLexEntry in itemsToChange)
					{
						var hvoLexemeForm = sda.get_ObjectProp(hvoLexEntry, m_flidParent);
						if (hvoLexemeForm == 0)
						{
							continue;
						}
						var hvoMorphType = sda.get_ObjectProp(hvoLexemeForm, m_flidAtomicProp);
						if (hvoMorphType == 0)
						{
							continue;
						}
						var fAffix = MorphServices.IsAffixType(m_cache, hvoMorphType);
						if (fAffix == fSelAffix || hvoSelMorphType == 0)
						{
							continue;
						}
						var msg = string.Format(XMLViewsStrings.ksMorphTypeChangesSlow,
							(fAffix ? XMLViewsStrings.ksAffixes : XMLViewsStrings.ksStems),
							(fAffix ? XMLViewsStrings.ksStems : XMLViewsStrings.ksAffixes));
						if (MessageBox.Show(m_combo, msg, XMLViewsStrings.ksChangingMorphType,
							    MessageBoxButtons.OKCancel,
							    MessageBoxIcon.Warning) != DialogResult.OK)
						{
							return;
						}
						fAnyFundamentalChanges = true;
						break; // user OKd it, no need to check further.
					}
					if (fAnyFundamentalChanges)
					{
						m_containingViewer.SetListModificationInProgress(true);
					}
					try
					{
						// Report progress 50 times or every 100 items, whichever is more
						// (but no more than once per item!)
						var idsToDel = new HashSet<int>();
						var newForms = new Dictionary<IMoForm, ILexEntry>();
						var interval = Math.Min(80, Math.Max(itemsToChange.Count()/50, 1));
						var i = 0;
						var rgmsaOld = new List<IMoMorphSynAnalysis>();
						foreach (var hvoLexEntry in itemsToChange)
						{
							// Guess we're 80% done when through all but deleting leftover objects and moving
							// new MoForms to LexemeForm slot.
							if ((i + 1)%interval == 0)
							{
								state.PercentDone = i*80/itemsToChange.Count();
								state.Breath();
							}
							i++;
							var hvoLexemeForm = sda.get_ObjectProp(hvoLexEntry, m_flidParent);
							if (hvoLexemeForm == 0)
							{
								continue;
							}
							var hvoMorphType = sda.get_ObjectProp(hvoLexemeForm, m_flidAtomicProp);
							if (hvoMorphType == 0)
							{
								continue;
							}
							var fAffix = MorphServices.IsAffixType(m_cache, hvoMorphType);
							var stemAlloFactory = m_cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>();
							var afxAlloFactory = m_cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>();
							if (fAffix == fSelAffix)
							{
								// Not changing C# type of allomorph object, just set the morph type.
								if (hvoMorphType != hvoSelMorphType)
								{
									sda.SetObjProp(hvoLexemeForm, m_flidAtomicProp, hvoSelMorphType);
								}
							}
							else if (fAffix)
							{
								// Changing from affix to stem, need a new allomorph object.
								var entry = m_cache.ServiceLocator.GetInstance<ILexEntryRepository>().GetObject(hvoLexEntry);
								var affix = m_cache.ServiceLocator.GetInstance<IMoAffixAllomorphRepository>().GetObject(hvoLexemeForm);
								var stem = stemAlloFactory.Create();
								rgmsaOld.Clear();
								foreach (var msa in entry.MorphoSyntaxAnalysesOC)
								{
									if (!(msa is IMoStemMsa))
									{
										rgmsaOld.Add(msa);
									}
								}
								entry.ReplaceObsoleteMsas(rgmsaOld);
								SwapFormValues(entry, affix, stem, hvoSelMorphType, idsToDel);
								foreach (var env in affix.PhoneEnvRC)
								{
									stem.PhoneEnvRC.Add(env);
								}
								newForms[stem] = entry;
							}
							else
							{
								// Changing from stem to affix, need a new allomorph object.
								var entry = m_cache.ServiceLocator.GetInstance<ILexEntryRepository>().GetObject(hvoLexEntry);
								var stem = m_cache.ServiceLocator.GetInstance<IMoStemAllomorphRepository>().GetObject(hvoLexemeForm);
								var affix = afxAlloFactory.Create();
								rgmsaOld.Clear();
								foreach (var msa in entry.MorphoSyntaxAnalysesOC)
								{
									if (msa is IMoStemMsa)
									{
										rgmsaOld.Add(msa);
									}
								}
								entry.ReplaceObsoleteMsas(rgmsaOld);
								SwapFormValues(entry, stem, affix, hvoSelMorphType, idsToDel);
								foreach (var env in stem.PhoneEnvRC)
								{
									affix.PhoneEnvRC.Add(env);
								}
								newForms[affix] = entry;
							}
						}
						if (fAnyFundamentalChanges)
						{
							foreach (int hvo in idsToDel)
							{
								sda.DeleteObj(hvo);
							}
							state.PercentDone = 90;
							state.Breath();
							foreach (var pair in newForms)
							{
								pair.Value.LexemeFormOA = pair.Key;
							}
							state.PercentDone = 100;
							state.Breath();
						}
					}
					finally
					{
						if (fAnyFundamentalChanges)
						{
							m_containingViewer.SetListModificationInProgress(false);
						}
					}
				});
		}
		// Swap values of various attributes between an existing form that is a LexemeForm and
		// a newly created one. Includes adding the new one to the alternate forms of the entry, and
		// the id of the old one to a map of things to delete.
		private void SwapFormValues(ILexEntry entry, IMoForm origForm, IMoForm newForm, int typeHvo, HashSet<int> idsToDel)
		{
			entry.AlternateFormsOS.Add(newForm);
			origForm.SwapReferences(newForm);
			var muaOrigForm = origForm.Form;
			var muaNewForm = newForm.Form;
			muaNewForm.MergeAlternatives(muaOrigForm);
			newForm.MorphTypeRA = m_cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(typeHvo);
			idsToDel.Add(origForm.Hvo);
		}

		public override void FakeDoit(IEnumerable<int> itemsToChange, int tagMadeUpFieldIdentifier, int tagEnabled, ProgressState state)
		{
			var sda = m_cache.DomainDataByFlid;
			var item = m_combo.SelectedItem as HvoTssComboItem;
			if (item == null)
			{
				return;
			}
			var hvoSelMorphType = item.Hvo;

			// Report progress 50 times or every 100 items, whichever is more
			// (but no more than once per item!)
			var interval = Math.Min(100, Math.Max(itemsToChange.Count() / 50, 1));
			var i = 0;
			foreach (int hvoLexEntry in itemsToChange)
			{
				if ((i + 1) % interval == 0)
				{
					state.PercentDone = i * 100 / itemsToChange.Count();
					state.Breath();
				}
				var hvoLexemeForm = sda.get_ObjectProp(hvoLexEntry, m_flidParent);
				if (hvoLexemeForm == 0)
				{
					continue;
				}
				var hvoMorphType = sda.get_ObjectProp(hvoLexemeForm, m_flidAtomicProp);
				if (hvoMorphType == 0)
				{
					continue;
				}
				// Per LT-5305, OK to switch types.
				//bool fEnable = fAffix == fSelAffix && hvoMorphType != hvoSelMorphType;
				var fEnable = hvoMorphType != hvoSelMorphType;
				if (fEnable)
				{
					m_sda.SetString(hvoLexEntry, tagMadeUpFieldIdentifier, item.AsTss);
				}
				m_sda.SetInt(hvoLexEntry, tagEnabled, (fEnable ? 1 : 0));
				i++;
			}
		}

		#endregion
	}
}