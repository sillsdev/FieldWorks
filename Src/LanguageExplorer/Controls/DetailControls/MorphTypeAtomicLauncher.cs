// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Controls.DetailControls.Resources;
using LanguageExplorer.Controls.XMLViews;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using SIL.Xml;

namespace LanguageExplorer.Controls.DetailControls
{
	internal class MorphTypeAtomicLauncher : PossibilityAtomicReferenceLauncher
	{
		private const string m_ksPath = "/group[@id='DialogStrings']/";

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			//Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			if( disposing )
			{
			}
			base.Dispose( disposing );
		}
		/// <summary>
		/// Get the SimpleListChooser.
		/// </summary>
		/// <param name="labels">List of objects to show in the chooser.</param>
		/// <returns>The SimpleListChooser.</returns>
		protected new MorphTypeChooser GetChooser(IEnumerable<ObjectLabel> labels)
		{
			var sShowAllTypes = StringTable.Table.GetStringWithXPath("ChangeLexemeMorphTypeShowAllTypes", m_ksPath);
			var x = new MorphTypeChooser(m_persistProvider, labels, m_fieldName, m_obj, m_displayNameProperty, m_flid, sShowAllTypes, PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"))
			{
				Cache = m_cache
			};
			x.NullLabel.DisplayName  = XmlUtils.GetOptionalAttributeValue(m_configurationNode, "nullLabel", "<EMPTY>");
			return x;
		}

		/// <summary>
		/// Override method to handle launching of a chooser for selecting lexical entries.
		/// </summary>
		protected override void HandleChooser()
		{
			var displayWs = "analysis vernacular";
#pragma warning disable 219
			string postDialogMessageTrigger = null;
#pragma warning restore 219

			var node = m_configurationNode?.Element("deParams");
			if (node != null)
			{
				displayWs = XmlUtils.GetOptionalAttributeValue(node, "ws", "analysis vernacular").ToLower();
				postDialogMessageTrigger = XmlUtils.GetOptionalAttributeValue(node, "postChangeMessageTrigger", null);
			}

			var labels = ObjectLabel.CreateObjectLabels(m_cache, m_obj.ReferenceTargetCandidates(m_flid), m_displayNameProperty, displayWs);

			using (var chooser = GetChooser(labels))
			{
				var fMadeMorphTypeChange = false;
				var entry = (ILexEntry)m_obj.Owner;
				chooser.InitializeExtras(m_configurationNode, PropertyTable);
				chooser.SetObjectAndFlid(m_obj.Hvo, m_flid);
				chooser.SetHelpTopic(Slice.GetChooserHelpTopicID());

				var hvoType = m_cache.DomainDataByFlid.get_ObjectProp(m_obj.Hvo, m_flid);
				var morphTypeRep = m_cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>();
				var type = hvoType != 0 ? morphTypeRep.GetObject(hvoType) : null;
				chooser.MakeSelection(type);

				if (chooser.ShowDialog() != DialogResult.OK)
				{
					return;
				}
				var selected = (IMoMorphType) chooser.ChosenOne.Object;
				var original = Target as IMoMorphType;
				var sUndo = StringTable.Table.GetStringWithXPath("ChangeLexemeMorphTypeUndo", m_ksPath);
				var sRedo = StringTable.Table.GetStringWithXPath("ChangeLexemeMorphTypeRedo", m_ksPath);
				var fRemoveComponents = false;
				if (selected.Guid == MoMorphTypeTags.kguidMorphRoot || selected.Guid == MoMorphTypeTags.kguidMorphBoundRoot)
				{
					// changing to root...not allowed to have complex forms.
					foreach (var ler in entry.EntryRefsOS)
					{
						if (ler.RefType == LexEntryRefTags.krtComplexForm)
						{
							fRemoveComponents = true;
							// If there are no components we will delete without asking...but must then check for more
							// complex forms that DO have components.
							if (ler.ComponentLexemesRS.Count > 0)
							{
								// TODO-Linux: Help is not implemented in Mono
								if (MessageBox.Show(FindForm(), DetailControlsStrings.ksRootNoComponentsMessage,
									    DetailControlsStrings.ksRootNoComponentsCaption, MessageBoxButtons.YesNo,
									    MessageBoxIcon.Question, MessageBoxDefaultButton.Button1, 0, PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider").HelpFile,
									    HelpNavigator.Topic, "/Using_Tools/Lexicon_tools/Lexicon_Edit/change_the_morph_type.htm") != DialogResult.Yes)
								{
									return;
								}
								break;
							}
						}
					}
				}

				UndoableUnitOfWorkHelper.Do(sUndo, sRedo, entry, () =>
				{
					if (fRemoveComponents)
					{
						foreach (var ler in entry.EntryRefsOS.Where(entryRef => entryRef.RefType == LexEntryRefTags.krtComplexForm))
						{
							entry.EntryRefsOS.Remove(ler);
						}
					}

					if (IsStemType(original) || m_obj is IMoStemAllomorph)
					{
						if (IsStemType(selected))
						{
							Target = selected;
						}
						else
						{
							//have to switch from stem to affix
							fMadeMorphTypeChange = ChangeStemToAffix(entry, selected);
						}
					}
					else
					{
						// original is affix variety
						if (IsStemType(selected))
						{
							//have to switch from affix to stem
							fMadeMorphTypeChange = ChangeAffixToStem(entry, selected);
						}
						else
						{
							Target = selected;
						}
					}
					if (selected.Guid == MoMorphTypeTags.kguidMorphPhrase)
					{
						var ler = m_cache.ServiceLocator.GetInstance<ILexEntryRefFactory>().Create();
						entry.EntryRefsOS.Add(ler);
						ler.RefType = LexEntryRefTags.krtComplexForm;
						ler.HideMinorEntry = 1;
					}
				});
			}
		}

		protected override bool AllowEmptyItem => false;

		/// <summary>
		/// Change the affix to a stem (possibly)
		/// </summary>
		private bool ChangeAffixToStem(ILexEntry entry, IMoMorphType type)
		{
			var affix = m_obj as IMoAffixForm;
			if (affix == null)
			{
				throw new ApplicationException("Affix form is not defined");
			}
			var rgmsaOld = new List<IMoMorphSynAnalysis>();
			if (m_obj.OwningFlid == LexEntryTags.kflidLexemeForm)
			{
				foreach (var msa in entry.MorphoSyntaxAnalysesOC)
				{
					if (!(msa is IMoStemMsa))
					{
						rgmsaOld.Add(msa);
					}
				}
			}

			if (CheckForAffixDataLoss(affix, rgmsaOld))
			{
				return false;
			}
			var stem = m_cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			SwapValues(entry, affix, stem, type, rgmsaOld);	// may cause slice/button to be disposed...
			return true;
		}

		private bool CheckForAffixDataLoss(IMoAffixForm affix, List<IMoMorphSynAnalysis> rgmsaAffix)
		{
			var fLoseInflCls = affix.InflectionClassesRC.Count > 0;
			var fLoseInfixLoc = false;
			var fLoseGramInfo = false;
			var fLoseRule = false;
			switch (affix.ClassID)
			{
				case MoAffixProcessTags.kClassId:
					fLoseRule = true;
					break;
				case MoAffixAllomorphTags.kClassId:
					var allo = (IMoAffixAllomorph) affix;
					fLoseInfixLoc = allo.PositionRS.Count > 0;
					fLoseGramInfo = allo.MsEnvPartOfSpeechRA != null || allo.MsEnvFeaturesOA != null;
					break;
			}

			for (var i = 0; !fLoseGramInfo && i < rgmsaAffix.Count; ++i)
			{
				var msaInfl = rgmsaAffix[i] as IMoInflAffMsa;
				if (msaInfl != null)
				{
					if (msaInfl.AffixCategoryRA != null ||
						msaInfl.FromProdRestrictRC.Count > 0 ||
						msaInfl.SlotsRC.Count > 0 ||
						msaInfl.InflFeatsOA != null)
					{
						fLoseGramInfo = true;
					}
					continue;
				}
				var msaDeriv = rgmsaAffix[i] as IMoDerivAffMsa;
				if (msaDeriv != null)
				{
					if (msaDeriv.AffixCategoryRA != null ||
						msaDeriv.FromInflectionClassRA != null ||
						msaDeriv.FromPartOfSpeechRA != null ||
						msaDeriv.FromProdRestrictRC.Count > 0 ||
						msaDeriv.FromStemNameRA != null ||
						msaDeriv.StratumRA != null ||
						msaDeriv.ToInflectionClassRA != null ||
						msaDeriv.ToProdRestrictRC.Count > 0 ||
						msaDeriv.FromMsFeaturesOA != null ||
						msaDeriv.ToMsFeaturesOA != null)
					{
						fLoseGramInfo = true;
					}
					continue;
				}
				var msaStep = rgmsaAffix[i] as IMoDerivStepMsa;
				if (msaStep != null)
				{
					if (msaStep.InflectionClassRA != null ||
						msaStep.ProdRestrictRC.Count > 0 ||
						msaStep.InflFeatsOA != null ||
						msaStep.MsFeaturesOA != null)
					{
						fLoseGramInfo = true;
					}
				}
			}

			if (!fLoseInflCls && !fLoseInfixLoc && !fLoseGramInfo && !fLoseRule)
			{
				return false;
			}
			string sMsg;
			if (fLoseInflCls && fLoseInfixLoc && fLoseGramInfo)
			{
				sMsg = StringTable.Table.GetStringWithXPath("ChangeMorphTypeLoseInflClsInfixLocGramInfo", m_ksPath);
			}
			else if (fLoseRule && fLoseInflCls && fLoseGramInfo)
			{
				sMsg = StringTable.Table.GetStringWithXPath("ChangeMorphTypeLoseRuleInflClsGramInfo", m_ksPath);
			}
			else if (fLoseInflCls && fLoseInfixLoc)
			{
				sMsg = StringTable.Table.GetStringWithXPath("ChangeMorphTypeLoseInflClsInfixLoc", m_ksPath);
			}
			else if (fLoseInflCls && fLoseGramInfo)
			{
				sMsg = StringTable.Table.GetStringWithXPath("ChangeMorphTypeLoseInflClsGramInfo", m_ksPath);
			}
			else if (fLoseInfixLoc && fLoseGramInfo)
			{
				sMsg = StringTable.Table.GetStringWithXPath("ChangeMorphTypeLoseInfixLocGramInfo", m_ksPath);
			}
			else if (fLoseRule && fLoseInflCls)
			{
				sMsg = StringTable.Table.GetStringWithXPath("ChangeMorphTypeLoseRuleInflCls", m_ksPath);
			}
			else if (fLoseRule)
			{
				sMsg = StringTable.Table.GetStringWithXPath("ChangeMorphTypeLoseRule", m_ksPath);
			}
			else if (fLoseInflCls)
			{
				sMsg = StringTable.Table.GetStringWithXPath("ChangeMorphTypeLoseInflCls", m_ksPath);
			}
			else if (fLoseInfixLoc)
			{
				sMsg = StringTable.Table.GetStringWithXPath("ChangeMorphTypeLoseInfixLoc", m_ksPath);
			}
			else
			{
				sMsg = StringTable.Table.GetStringWithXPath("ChangeMorphTypeLoseGramInfo", m_ksPath);
			}
			var sCaption = StringTable.Table.GetStringWithXPath("ChangeLexemeMorphTypeCaption", m_ksPath);
			var result = MessageBox.Show(sMsg, sCaption, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
			return result == DialogResult.No;
		}

		private bool ChangeStemToAffix(ILexEntry entry, IMoMorphType type)
		{
			var stem = m_obj as IMoStemAllomorph;
			if (stem == null)
			{
				throw new ApplicationException("Stem allomorph is not defined");
			}
			var rgmsaOld = new List<IMoMorphSynAnalysis>();
			if (m_obj.OwningFlid == LexEntryTags.kflidLexemeForm)
			{
				foreach (var msa in entry.MorphoSyntaxAnalysesOC)
				{
					if (msa is IMoStemMsa)
					{
						rgmsaOld.Add(msa);
					}
				}
			}

			if (CheckForStemDataLoss(stem, rgmsaOld))
			{
				return false;
			}
			var affix = m_cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create();
			SwapValues(entry, stem, affix, type, rgmsaOld);
			return true;
		}

		private bool CheckForStemDataLoss(IMoStemAllomorph stem, List<IMoMorphSynAnalysis> rgmsaStem)
		{
			var fLoseStemName = stem.StemNameRA != null;
			var fLoseGramInfo = false;
			for (var i = 0; i < rgmsaStem.Count; ++i)
			{
				var msa = rgmsaStem[i] as IMoStemMsa;
				if (msa != null &&
					(msa.FromPartsOfSpeechRC.Count > 0 ||
					msa.InflectionClassRA != null ||
					msa.ProdRestrictRC.Count > 0 ||
					msa.StratumRA != null ||
					msa.MsFeaturesOA != null))
				{
					fLoseGramInfo = true;
					break;
				}
			}

			if (!fLoseStemName && !fLoseGramInfo)
			{
				return false;
			}
			string sMsg;
			if (fLoseStemName && fLoseGramInfo)
			{
				sMsg = StringTable.Table.GetStringWithXPath("ChangeMorphTypeLoseStemNameGramInfo", m_ksPath);
			}
			else if (fLoseStemName)
			{
				sMsg = StringTable.Table.GetStringWithXPath("ChangeMorphTypeLoseStemName", m_ksPath);
			}
			else
			{
				sMsg = StringTable.Table.GetStringWithXPath("ChangeMorphTypeLoseGramInfo", m_ksPath);
			}
			var sCaption = StringTable.Table.GetStringWithXPath("ChangeLexemeMorphTypeCaption", m_ksPath);
			var result = MessageBox.Show(sMsg, sCaption, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
			return result == DialogResult.No;
		}

		private void SwapValues(ILexEntry entry, IMoForm origForm, IMoForm newForm, IMoMorphType type, List<IMoMorphSynAnalysis> rgmsaOld)
		{
			var dtree = Slice.ContainingDataTree;
			var idx = Slice.IndexInContainer;
			dtree.DoNotRefresh = true;	// don't let the datatree repeatedly redraw itself...
			entry.ReplaceMoForm(origForm, newForm);
			newForm.MorphTypeRA = type;
			entry.ReplaceObsoleteMsas(rgmsaOld);
			// Dispose of any obsolete slices: new ones will replace them automatically in a moment
			// when the datatree is redrawn.
			foreach (var slice in Slice.ContainingDataTree.Slices.ToArray())
			{
				if (slice.IsDisposed)
				{
					continue;
				}

				if (slice.Object is IMoMorphSynAnalysis && rgmsaOld.Contains(slice.Object as IMoMorphSynAnalysis))
				{
					slice.Dispose();
				}
				else if (slice is MSAReferenceComboBoxSlice)
				{
					slice.Dispose();
				}
			}
			// now fix the record list, since it may be showing MoForm dependent columns (e.g. MorphType, Homograph, etc...)
			dtree.FixRecordList();
			dtree.DoNotRefresh = false;
			var sliceT = dtree.Slices[idx];
			if (sliceT == null || !(sliceT is MorphTypeAtomicReferenceSlice))
			{
				return;
			}
			// When the new slice is created, the launch button is placed in the middle of
			// the slice rather than at the end.  This fiddling with the slice width seems
			// to fix that.  Then setting the index restores focus to the new slice.
			sliceT.Width += 1;
			sliceT.Width -= 1;
			dtree.GotoNextSliceAfterIndex(idx - 1);
		}

		private static bool IsStemType(IMoMorphType type)
		{
			if (type == null)
			{
				return false;
			}

			return (type.Guid == MoMorphTypeTags.kguidMorphBoundRoot) ||
			       (type.Guid == MoMorphTypeTags.kguidMorphBoundStem) ||
			       (type.Guid == MoMorphTypeTags.kguidMorphEnclitic) ||
			       (type.Guid == MoMorphTypeTags.kguidMorphParticle) ||
			       (type.Guid == MoMorphTypeTags.kguidMorphProclitic) ||
			       (type.Guid == MoMorphTypeTags.kguidMorphRoot) ||
			       (type.Guid == MoMorphTypeTags.kguidMorphStem) ||
			       (type.Guid == MoMorphTypeTags.kguidMorphClitic) ||
			       (type.Guid == MoMorphTypeTags.kguidMorphPhrase) ||
			       (type.Guid == MoMorphTypeTags.kguidMorphDiscontiguousPhrase);
		}
	}
}