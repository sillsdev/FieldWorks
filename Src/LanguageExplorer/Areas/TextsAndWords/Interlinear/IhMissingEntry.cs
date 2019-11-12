// Copyright (c) 2006-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.LcmUi;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// Handles the morpheme entry (LexEntry) line when none is known.
	/// </summary>
	internal class IhMissingEntry : InterlinComboHandler
	{
		private IHelpTopicProvider m_helpTopicProvider;
		// form of the morpheme when the combo was initialized.
		ITsString m_tssMorphForm;
		// flag to HideCombo after HandleSelect.
		bool m_fHideCombo = true;
		// int for all classes, except IhMissingEntry, which stuffs MorphItem data into it.
		// So, that ill-behaved class has to make its own m_items data member.

		/// <summary>
		/// Determines if the two MorphItems are based on the same objects, ignoring string values.
		/// </summary>
		private static bool HaveSameObjs(MorphItem x, MorphItem y)
		{
			return x.m_hvoSense == y.m_hvoSense && x.m_hvoMainEntryOfVariant == y.m_hvoMainEntryOfVariant && x.m_hvoMorph == y.m_hvoMorph && x.m_hvoMsa == y.m_hvoMsa
			       && x.m_inflType == y.m_inflType && x.m_entryRef == y.m_entryRef;
		}

		private static int HvoOrZero(ICmObject co)
		{
			return co?.Hvo ?? 0;
		}

		internal MorphItem CreateCoreMorphItemBasedOnSandboxCurrentState()
		{
			var hvoWmb = SelectedMorphHvo;
			var hvoMorphSense = m_caches.DataAccess.get_ObjectProp(hvoWmb, SandboxBase.ktagSbMorphGloss);
			var hvoInflType = m_caches.DataAccess.get_ObjectProp(hvoWmb, SandboxBase.ktagSbNamedObjInflType);
			ILexEntryInflType inflType = null;
			if (hvoInflType != 0)
			{
				inflType = m_caches.MainCache.ServiceLocator.GetInstance<ILexEntryInflTypeRepository>().GetObject(m_caches.RealHvo(hvoInflType));
			}
			var hvoMorphEntry = m_caches.DataAccess.get_ObjectProp(hvoWmb, SandboxBase.ktagSbMorphEntry);
			ILexEntry realEntry = null;
			IMoForm mf = null;
			if (hvoMorphEntry != 0)
			{
				var realHvo = m_caches.RealHvo(hvoMorphEntry);
				if (m_caches.MainCache.ServiceLocator.IsValidObjectId(realHvo))
				{
					realEntry = m_caches.MainCache.ServiceLocator.GetInstance<ILexEntryRepository>().GetObject(realHvo);
					mf = realEntry.LexemeFormOA;
				}
			}
			ILexSense realSense = null;
			ILexEntryRef ler = null;
			if (hvoMorphSense != 0)
			{
				var realHvo = m_caches.RealHvo(hvoMorphSense);
				if (m_caches.MainCache.ServiceLocator.IsValidObjectId(realHvo))
				{
					realSense = m_caches.MainCache.ServiceLocator.GetInstance<ILexSenseRepository>().GetObject(realHvo);
					realEntry?.IsVariantOfSenseOrOwnerEntry(realSense, out ler);
				}
			}
			return GetMorphItem(mf, null, realSense, null, ler, HvoOrZero(realEntry), inflType);
		}

		/// <summary />
		internal IhMissingEntry(IHelpTopicProvider helpTopicProvider)
		{
			m_helpTopicProvider = helpTopicProvider;
		}

		#region IDisposable override

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + " ******");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_tssMorphForm = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		public override List<int> Items
		{
			get
			{
				if (m_items.Count == 0)
				{
					SyncItemsToMorphItems();
				}
				return base.Items;
			}
		}

		internal List<MorphItem> MorphItems { get; } = new List<MorphItem>();

		private void SyncItemsToMorphItems()
		{
			// re-populate the items with the most specific levels of analysis.
			m_items.Clear();
			foreach (var mi in MorphItems)
			{
				if (mi.m_hvoSense > 0)
				{
					m_items.Add(mi.m_hvoSense);
				}
				else if (mi.m_hvoMorph > 0)
				{
					m_items.Add(mi.m_hvoMorph); // should be owned by LexEntry
				}
				else
				{
					throw new ArgumentException("invalid morphItem");
				}
			}
		}

		internal void LoadMorphItems()
		{
			var sda = m_caches.DataAccess;
			var hvoForm = sda.get_ObjectProp(SelectedMorphHvo, SandboxBase.ktagSbMorphForm);
			var tssMorphForm = sda.get_MultiStringAlt(hvoForm, SandboxBase.ktagSbNamedObjName, m_sandbox.RawWordformWs);
			var sPrefix = StrFromTss(sda.get_StringProp(SelectedMorphHvo, SandboxBase.ktagSbMorphPrefix));
			var sPostfix = StrFromTss(sda.get_StringProp(SelectedMorphHvo, SandboxBase.ktagSbMorphPostfix));
			var morphs = MorphServices.GetMatchingMorphs(m_caches.MainCache, sPrefix, tssMorphForm, sPostfix);
			m_tssMorphForm = tssMorphForm;
			MorphItems.Clear();
			foreach (var mf in morphs)
			{
				var parentEntry = mf.Owner as ILexEntry;
				BuildMorphItemsFromEntry(mf, parentEntry, null);

				Debug.Assert(parentEntry != null, "MoForm Owner shouldn't be null.");
				var variantRefs = parentEntry.EntryRefsOS.Where(entryRef => entryRef.VariantEntryTypesRS != null && entryRef.VariantEntryTypesRS.Any());
				// for now, just build morph items for variant EntryRefs having only one component
				// otherwise, it's ambiguous which component to use to build a WfiAnalysis with.
				foreach (var ler in variantRefs.Where(ler => ler.ComponentLexemesRS.Count == 1))
				{
					var mainEntryOfVariant = GetMainEntryOfVariant(ler);
					BuildMorphItemsFromEntry(mf, mainEntryOfVariant, ler);
				}
			}
		}

		internal static ILexEntry GetMainEntryOfVariant(ILexEntryRef ler)
		{
			var component = ler.ComponentLexemesRS[0] as IVariantComponentLexeme;
			ILexEntry mainEntryOfVariant = null;
			switch (component.ClassID)
			{
				case LexEntryTags.kClassId:
					mainEntryOfVariant = component as ILexEntry;
					break;
				case LexSenseTags.kClassId:
					mainEntryOfVariant = (component as ILexSense).Entry;
					break;
			}
			return mainEntryOfVariant;
		}


		/// <summary />
		/// <param name="mf"></param>
		/// <param name="le">the entry used in the morph bundle (for sense info). typically
		/// this is an owner of hvoMorph, but if not, it most likely has hvoMorph linked as its variant.</param>
		/// <param name="ler"></param>
		private void BuildMorphItemsFromEntry(IMoForm mf, ILexEntry le, ILexEntryRef ler)
		{
			var hvoLexEntry = 0;
			if (le != null)
			{
				hvoLexEntry = le.Hvo;
			}
			ITsString tssName;
			if (le != null)
			{
				tssName = LexEntryVc.GetLexEntryTss(m_caches.MainCache, le.Hvo, m_wsVern, ler);
			}
			else
			{
				// looks like we're not in a good state, so just use the form for the name.
				int wsActual;
				tssName = mf.Form.GetAlternativeOrBestTss(m_wsVern, out wsActual);
			}
			var wsAnalysis = m_caches.MainCache.ServiceLocator.WritingSystemManager.Get(m_caches.MainCache.DefaultAnalWs);
			// Populate morphItems with Sense/Msa level specifics
			if (le != null)
			{
				foreach (var sense in le.AllSenses)
				{
					var tssSense = sense.Gloss.get_String(wsAnalysis.Handle);
					if (ler != null)
					{
						var lexEntryInflTypes = ler.VariantEntryTypesRS.OfType<ILexEntryInflType>().ToList();
						if (lexEntryInflTypes.Any())
						{
							foreach (var inflType in lexEntryInflTypes)
							{
								var glossAccessor = tssSense.Length == 0 ? (IMultiStringAccessor)sense.Definition : sense.Gloss;
								tssSense = MorphServices.MakeGlossOptionWithInflVariantTypes(inflType, glossAccessor, wsAnalysis);
								var mi = GetMorphItem(mf, tssName, sense, tssSense, ler, hvoLexEntry, inflType);
								MorphItems.Add(mi);
							}
						}
						else
						{
							AddMorphItemToList(mf, ler, tssSense, sense, wsAnalysis, tssName, hvoLexEntry);
						}
					}
					else
					{
						AddMorphItemToList(mf, null, tssSense, sense, wsAnalysis, tssName, hvoLexEntry);
					}
				}
			}
			// Make a LexEntry level item
			MorphItems.Add(new MorphItem(mf.Hvo, ler != null ? hvoLexEntry : 0, tssName));
		}

		private void AddMorphItemToList(IMoForm mf, ILexEntryRef ler, ITsString tssSense, ILexSense sense, CoreWritingSystemDefinition wsAnalysis, ITsString tssName, int hvoLexEntry)
		{
			if (tssSense.Length == 0)
			{
				// If it doesn't have a gloss (e.g., from Categorised Entry), use the definition.
				tssSense = sense.Definition.get_String(wsAnalysis.Handle);
			}
			MorphItems.Add(GetMorphItem(mf, tssName, sense, tssSense, ler, hvoLexEntry, null));
		}

		private static MorphItem GetMorphItem(IMoForm mf, ITsString tssName, ILexSense sense, ITsString tssSense, ILexEntryRef ler, int hvoLexEntry, ILexEntryInflType inflType)
		{
			IMoMorphSynAnalysis msa = null;
			string msaText = null;
			if (sense != null)
			{
				msa = sense.MorphoSyntaxAnalysisRA;
				if (msa != null)
				{
					msaText = msa.InterlinearName;
				}
			}
			var options = new MorphItemOptions
			{
				HvoMoForm = HvoOrZero(mf),
				HvoEntry = ler != null ? hvoLexEntry : 0,
				TssName = tssName,
				HvoSense = HvoOrZero(sense),
				SenseName = tssSense?.Text,
				HvoMsa = HvoOrZero(msa),
				MsaName = msaText,
				InflType = inflType,
				EntryRef = ler,
			};
			return new MorphItem(options);
		}

		/// <summary />
		public override void SetupCombo()
		{
			base.SetupCombo();

			LoadMorphItems();
			AddMorphItemsToComboList();
			// DON'T ADD ANY MORE MorphItem OBJECTS TO m_morphItems AT THIS POINT!
			// The order of items added to m_comboList.Items below must match exactly the
			// switch statement at the beginning of HandleSelect().
			AddUnknownLexEntryToComboList();
			// If morphemes line is empty then make the Create New Entry
			// appear disabled (cf. LT-6480). If user tries to select this index,
			// we prevent the selection in our HandleSelect override.
			ITsTextProps disabledItemProperties = null;
			if (m_sandbox.IsMorphFormLineEmpty)
			{
				disabledItemProperties = DisabledItemProperties();
			}
			AddItemToComboList(ITextStrings.ksCreateNewEntry_, OnSelectCreateNewEntry, disabledItemProperties, disabledItemProperties == null);
			AddItemToComboList(ITextStrings.ksVariantOf_, OnSelectVariantOf, disabledItemProperties, disabledItemProperties == null);
			// If morphemes line is empty then make the allomorph selection,
			// appear disabled (cf. LT-1621). If user tries to select this index,
			// we prevent the selection in our HandleComboSelChange override.
			AddItemToComboList(ITextStrings.ksAllomorphOf_, OnSelectAllomorphOf, disabledItemProperties, disabledItemProperties == null);
			// If the morpheme line is hidden, give the user the option to edit morph breaks.
			if (m_sandbox.InterlinLineChoices.IndexOf(InterlinLineChoices.kflidMorphemes) < 0)
			{
				AddItemToComboList("-------", null, null, false);
				AddItemToComboList(ITextStrings.ksEditMorphBreaks_, OnSelectEditMorphBreaks, null, true);
			}
			// Set combo selection to current selection.
			ComboList.SelectedIndex = IndexOfCurrentItem;
		}

		private void AddMorphItemsToComboList()
		{
			var coRepository = m_caches.MainCache.ServiceLocator.GetInstance<ICmObjectRepository>();
			var tisb = TsStringUtils.MakeIncStrBldr();
			var miPrev = new MorphItem();
			MorphItems.Sort();
			foreach (var mi in MorphItems)
			{
				ITsString tssToDisplay;
				int hvoPrimary; // the key hvo associated with the combo item.
				tisb.Clear();
				var morph = coRepository.GetObject(mi.m_hvoMorph);
				var le = morph.Owner as ILexEntry;
				if (mi.m_hvoSense > 0)
				{
					tisb.SetIntPropValues((int)FwTextPropType.ktptSuperscript, (int)FwTextPropVar.ktpvEnum, (int)FwSuperscriptVal.kssvOff);
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_wsAnal);
					tisb.Append("  ");
					var tssSense = TsStringUtils.MakeString(mi.m_nameSense, m_caches.MainCache.DefaultAnalWs);
					tisb.AppendTsString(tssSense);
					tisb.Append(", ");
					var sPos = mi.m_nameMsa ?? ITextStrings.ksQuestions;
					tisb.Append(sPos);
					tisb.Append(", ");
					// append lex entry form info
					tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_wsVern);
					tisb.AppendTsString(mi.m_name);
					tssToDisplay = tisb.GetString();
					hvoPrimary = mi.m_hvoSense;
					tisb.Clear();
				}
				else
				{
					hvoPrimary = mi.m_hvoMorph;
					// Make a comboList item for adding a new sense to the LexEntry
					if (miPrev.m_hvoMorph != 0 && mi.m_hvoMorph == miPrev.m_hvoMorph && miPrev.m_hvoSense > 0)
					{
						// "Add New Sense..."
						// the comboList has already added selections for senses and lexEntry form
						// thus establishing the LexEntry the user may wish to "Add New Sense..." to.
						tisb.Clear();
						tisb.SetIntPropValues((int)FwTextPropType.ktptSuperscript, (int)FwTextPropVar.ktpvEnum, (int)FwSuperscriptVal.kssvOff);
						tisb.SetIntPropValues((int)FwTextPropType.ktptBold, (int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvOff);
						tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_wsUser);
						tisb.Append(ITextStrings.ksAddNewSense_);
						tssToDisplay = tisb.GetString();
					}
					else
					{
						// "Add New Sense for {0}"
						// (EricP) This path means the current form matches an entry that (strangely enough)
						// doesn't have any senses so we need to add the LexEntry form into the string,
						// so the user knows what Entry they'll be adding the new sense to.
						Debug.Assert(le.SensesOS.Count == 0, "Expected LexEntry to have no senses.");
						var sFmt = ITextStrings.ksAddNewSenseForX_;
						tisb.Clear();
						tisb.SetIntPropValues((int)FwTextPropType.ktptSuperscript, (int)FwTextPropVar.ktpvEnum, (int)FwSuperscriptVal.kssvOff);
						tisb.SetIntPropValues((int)FwTextPropType.ktptBold, (int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvOff);
						tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_wsUser);
						tisb.Append(sFmt);
						var tss = tisb.GetString();
						var ich = sFmt.IndexOf("{0}");
						if (ich >= 0)
						{
							var tsbT = tss.GetBldr();
							tsbT.ReplaceTsString(ich, ich + "{0}".Length, mi.m_name);
							tss = tsbT.GetString();
						}
						tssToDisplay = tss;
					}
				}
				// keep track of the previous MorphItem to track context.
				ComboList.Items.Add(new MorphComboItem(mi, tssToDisplay, HandleSelectMorphComboItem, hvoPrimary));
				miPrev = mi;
			}
			SyncItemsToMorphItems();
		}

		private void AddUnknownLexEntryToComboList()
		{
			var tisb = TsStringUtils.MakeIncStrBldr();
			tisb.Clear();
			tisb.SetIntPropValues((int)FwTextPropType.ktptSuperscript, (int)FwTextPropVar.ktpvEnum, (int)FwSuperscriptVal.kssvOff);
			tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_wsUser);
			tisb.Append(ITextStrings.ksUnknown);
			ComboList.Items.Add(new InterlinComboHandlerActionComboItem(tisb.GetString(), SetLexEntryToUnknown));
		}

		private void AddItemToComboList(string itemName, EventHandler onSelect, ITsTextProps itemProperties, bool enableItem)
		{
			var tsb = TsStringUtils.MakeStrBldr();
			tsb.Replace(tsb.Length, tsb.Length, itemName, itemProperties);
			tsb.SetIntPropValues(0, tsb.Length, (int)FwTextPropType.ktptWs, 0, m_wsUser);
			ComboList.Items.Add(new InterlinComboHandlerActionComboItem(tsb.GetString(), enableItem ? onSelect : null));
		}

		/// <summary>
		/// Return the index corresponding to the current LexEntry/Sense state of the Sandbox.
		/// </summary>
		public override int IndexOfCurrentItem
		{
			get
			{
				// See if we can find the real hvo corresponding to the LexEntry/Sense currently
				// selected in the Sandbox.
				var sbHvo = m_sandbox.CurrentLexEntriesAnalysis(SelectedMorphHvo);
				var realHvo = m_sandbox.Caches.RealHvo(sbHvo);
				if (realHvo <= 0)
				{
					return base.IndexOfCurrentItem;
				}
				var miCurrentSb = CreateCoreMorphItemBasedOnSandboxCurrentState();
				// Look through our relevant list items to see if we find a match.
				for (var i = 0; i < MorphItems.Count; ++i)
				{
					var mi = MorphItems[i];
					if (HaveSameObjs(mi, miCurrentSb))
					{
						return i;
					}
				}
				// save the class id
				return base.IndexOfCurrentItem;
			}
		}

		/// <summary />
		private int ReturnIndexOfMorphItemMatchingCurrentAnalysisLevel(int realHvo)
		{
			var coRepository = m_caches.MainCache.ServiceLocator.GetInstance<ICmObjectRepository>();
			var co = coRepository.GetObject(realHvo);
			var classid = co.ClassID;
			var msa = co as IMoMorphSynAnalysis;

			// Look through our relevant list items to see if we find a match.
			for (var i = 0; i < MorphItems.Count; ++i)
			{
				var mi = MorphItems[i];
				switch (classid)
				{
					case LexSenseTags.kClassId:
						// See if we match the LexSense
						if (mi.m_hvoSense == realHvo)
						{
							return i;
						}
						break;
					case LexEntryTags.kClassId:
						// Otherwise, see if our LexEntry matches MoForm's owner (also a LexEntry)
						var morph = coRepository.GetObject(mi.m_hvoMorph);
						var entryReal = morph.Owner as ILexEntry;
						if (entryReal == co)
						{
							return i;
						}
						break;
					default:
						// See if we can match on the MSA
						if (msa != null && mi.m_hvoMsa == realHvo)
						{
							// verify the item sense is its owner
							var ls = coRepository.GetObject(mi.m_hvoSense) as ILexSense;
							if (msa == ls.MorphoSyntaxAnalysisRA)
							{
								return i;
							}
						}
						break;
				}
			}
			return base.IndexOfCurrentItem;
		}

		// This indicates there was a previous real LexEntry recorded. The 'real' subclass
		// overrides to answer 1. The value signifies the number of objects stored in the
		// ktagMorphEntry property before the user made a selection in the menu.
		internal virtual int WasReal()
		{
			return 0;
		}

		/// <summary>
		/// Run the dialog that allows the user to create a new LexEntry.
		/// </summary>
		private void RunCreateEntryDlg()
		{
			ILexEntry le;
			IMoForm allomorph;
			ILexSense sense;
			CreateNewEntry(false, out le, out allomorph, out sense);
		}

		internal void CreateNewEntry(bool fCreateNow, out ILexEntry le, out IMoForm allomorph, out ILexSense sense)
		{
			le = null;
			allomorph = null;
			sense = null;
			var mainCache = m_caches.MainCache;
			var hvoMorph = m_caches.DataAccess.get_ObjectProp(SelectedMorphHvo, SandboxBase.ktagSbMorphForm);
			var tssForm = m_caches.DataAccess.get_MultiStringAlt(hvoMorph, SandboxBase.ktagSbNamedObjName, m_sandbox.RawWordformWs);
			// If we don't have a form or it isn't in a current vernacular writing system, give up.
			if (tssForm == null || tssForm.Length == 0 || !WritingSystemServices.GetAllWritingSystems(m_caches.MainCache, "all vernacular", null, 0, 0).Contains(TsStringUtils.GetWsOfRun(tssForm, 0)))
			{
				return;
			}
			var entryComponents = BuildEntryComponents();
			var fCreateAllomorph = false;
			var fCreatedEntry = false;
			if (fCreateNow)
			{
				// just create a new entry based on the given information.
				le = mainCache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(entryComponents);
			}
			else
			{
				using (var dlg = InsertEntryNow.CreateInsertEntryDlg(false))
				{
					dlg.InitializeFlexComponent(new FlexComponentParameters(m_sandbox.PropertyTable, m_sandbox.Publisher, m_sandbox.Subscriber));
					dlg.SetDlgInfo(mainCache, m_sandbox.GetFullMorphForm(SelectedMorphHvo));
					dlg.TssGloss = entryComponents.GlossAlternatives.FirstOrDefault();
					foreach (var tss in entryComponents.GlossAlternatives.Skip(1))
					{
						dlg.SetInitialGloss(TsStringUtils.GetWsAtOffset(tss, 0), tss);
					}
					dlg.ChangeUseSimilarToCreateAllomorph();
					// bring up the dialog so the user can make further decisions.
					var mainWnd = m_sandbox.FindForm();
					// Making the form active fixes LT-2344 & LT-2345.
					// I'm (RandyR) not sure what adverse impact might show up by doing this.
					mainWnd.Activate();
					// The combo should be automatically hidden by activating another window.
					// That works on Windows but not on Mono (reported as https://bugzilla.xamarin.com/show_bug.cgi?id=15848).
					// So to prevent the combo hanging around on Mono, we hide it explicitly here.
					HideCombo();
					dlg.SetHelpTopic("khtpInsertEntryFromInterlinear");
					if (dlg.ShowDialog(mainWnd) == DialogResult.OK)
					{
						fCreateAllomorph = true;
					}
					dlg.GetDialogInfo(out le, out fCreatedEntry);
					if (!fCreatedEntry && !fCreateAllomorph)
					{
						return;
					}
				}
			}
			if (fCreateAllomorph && le.SensesOS.Any())
			{
				sense = le.SensesOS[0];
			}
			allomorph = MorphServices.FindMatchingAllomorph(le, tssForm);
			var fCreatedAllomorph = false;
			if (allomorph == null)
			{
				using (var undoHelper = new UndoableUnitOfWorkHelper(mainCache.ServiceLocator.GetInstance<IActionHandler>(), ITextStrings.ksUndoAddAllomorphToSimilarEntry, ITextStrings.ksRedoAddAllomorphToSimilarEntry))
				{
					allomorph = MorphServices.MakeMorph(le, tssForm);
					fCreatedAllomorph = true;
					Debug.Assert(allomorph != null);
					undoHelper.RollBack = false;
				}
				if (fCreatedEntry)
				{
					// Making the entry and the allomorph should feel like one indivisible action to the end user.
					((IActionHandlerExtensions)mainCache.ActionHandlerAccessor).MergeLastTwoUnitsOfWork();
				}
			}
			var allomorph1 = allomorph;
			var le1 = le;
			var sense1 = sense;
			if (fCreatedEntry || fCreatedAllomorph)
			{
				// If we've created something, then updating the sandbox needs to be undone as a unit with it,
				// so the sandbox isn't left showing something uncreated.
				UndoableUnitOfWorkHelper.Do("join me up", "join me up", mainCache.ActionHandlerAccessor, () => UpdateMorphEntry(allomorph1, le1, sense1));
				((IActionHandlerExtensions)mainCache.ActionHandlerAccessor).MergeLastTwoUnitsOfWork();
			}
			else
			{
				// Updating the sandbox doesn't need to be undoable, no real data changes.
				UpdateMorphEntry(allomorph1, le1, sense1);
			}
		}

		private LexEntryComponents BuildEntryComponents()
		{
			var entryComponents = MorphServices.BuildEntryComponents(m_caches.MainCache, TsStringUtils.GetCleanSingleRunTsString(m_sandbox.GetFullMorphForm(SelectedMorphHvo)));
			var hvoMorph = m_caches.DataAccess.get_ObjectProp(SelectedMorphHvo, SandboxBase.ktagSbMorphForm);
			var intermediateTssForm = m_caches.DataAccess.get_MultiStringAlt(hvoMorph, SandboxBase.ktagSbNamedObjName, m_sandbox.RawWordformWs);
			var tssForm = TsStringUtils.GetCleanSingleRunTsString(intermediateTssForm);
			if (entryComponents.LexemeFormAlternatives.Count > 0 && !entryComponents.LexemeFormAlternatives[0].Equals(tssForm))
			{
				throw new ArgumentException("Expected entryComponents to already have " + tssForm.Text);
			}
			var cMorphs = m_caches.DataAccess.get_VecSize(m_hvoSbWord, SandboxBase.ktagSbWordMorphs);
			if (cMorphs != 1)
			{
				return entryComponents;
			}
			// Make this string the gloss of the dlg.
			var tssGloss = m_sandbox.Caches.DataAccess.get_MultiStringAlt(m_sandbox.RootWordHvo, SandboxBase.ktagSbWordGloss, m_sandbox.Caches.MainCache.DefaultAnalWs);
			var hvoSbPos = m_sandbox.Caches.DataAccess.get_ObjectProp(m_sandbox.RootWordHvo, SandboxBase.ktagSbWordPos);
			var hvoRealPos = m_sandbox.Caches.RealHvo(hvoSbPos);
			IPartOfSpeech realPos = null;
			if (hvoRealPos != 0)
			{
				realPos = m_sandbox.Caches.MainCache.ServiceLocator.GetInstance<IPartOfSpeechRepository>().GetObject(hvoRealPos);
			}
			entryComponents.MSA.MsaType = MsaType.kStem;
			entryComponents.MSA.MainPOS = realPos;
			entryComponents.GlossAlternatives.Add(tssGloss);
			// Also copy any other glosses we have.
			foreach (var ws in m_sandbox.Caches.MainCache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems.Select(wsObj => wsObj.Handle))
			{
				var tss = m_sandbox.Caches.DataAccess.get_MultiStringAlt(m_sandbox.RootWordHvo, SandboxBase.ktagSbWordGloss, ws);
				entryComponents.GlossAlternatives.Add(tss);
			}
			return entryComponents;
		}

		internal void RunAddNewAllomorphDlg()
		{
			ITsString tssForm;
			ITsString tssFullForm;
			IMoMorphType morphType;
			GetMorphInfo(out tssForm, out tssFullForm, out morphType);

			using (var dlg = new AddAllomorphDlg())
			{
				dlg.InitializeFlexComponent(new FlexComponentParameters(m_sandbox.PropertyTable, m_sandbox.Publisher, m_sandbox.Subscriber));
				var mainCache = m_caches.MainCache;
				dlg.SetDlgInfo(mainCache, null, tssForm, morphType.Hvo);
				var mainWnd = m_sandbox.FindForm();
				// Making the form active fixes LT-2619.
				// I'm (RandyR) not sure what adverse impact might show up by doing this.
				mainWnd.Activate();
				if (dlg.ShowDialog(mainWnd) != DialogResult.OK)
				{
					return;
				}
				// OK, they chose an entry, but does it have an appropriate MoForm?
				var le = dlg.SelectedObject as ILexEntry;
				var actionHandler = mainCache.ServiceLocator.GetInstance<IActionHandler>();
				if (dlg.InconsistentType && le.LexemeFormOA != null)
				{
					var morphLe = le.LexemeFormOA;
					var mmtLe = morphLe.MorphTypeRA;
					IMoMorphType mmtNew = null;
					mmtNew = morphType;
					string entryForm = null;
					var tssHeadword = le.HeadWord;
					if (tssHeadword != null)
					{
						entryForm = tssHeadword.Text;
					}
					if (string.IsNullOrEmpty(entryForm))
					{
						entryForm = ITextStrings.ksNoForm;
					}
					var sNoMorphType = StringTable.Table.GetString("NoMorphType", StringTable.DialogStrings);
					var sTypeLe = mmtLe != null ? mmtLe.Name.BestAnalysisAlternative.Text : sNoMorphType;
					var sTypeNew = mmtNew.Name.BestAnalysisAlternative.Text;
					var msg1 = string.Format(ITextStrings.ksSelectedLexEntryXisaY, entryForm, sTypeLe);
					var msg2 = string.Format(ITextStrings.ksAreYouSureAddZtoX, sTypeNew, tssForm.Text);

					using (var warnDlg = new CreateAllomorphTypeMismatchDlg())
					{
						warnDlg.Warning = msg1;
						warnDlg.Question = msg2;
						switch (warnDlg.ShowDialog(mainWnd))
						{
							case DialogResult.No:
								return;
							// cancelled.
							case DialogResult.Yes:
								// Go ahead and create allomorph.
								// But first, we have to ensure an appropriate MSA exists.
								var haveStemMSA = false;
								var haveUnclassifiedMSA = false;
								foreach (var msa in le.MorphoSyntaxAnalysesOC)
								{
									if (msa is IMoStemMsa)
									{
										haveStemMSA = true;
									}
									if (msa is IMoUnclassifiedAffixMsa)
									{
										haveUnclassifiedMSA = true;
									}
								}
								switch (mmtNew.Guid.ToString())
								{
									case MoMorphTypeTags.kMorphBoundRoot:
									case MoMorphTypeTags.kMorphBoundStem:
									case MoMorphTypeTags.kMorphClitic:
									case MoMorphTypeTags.kMorphEnclitic:
									case MoMorphTypeTags.kMorphProclitic:
									case MoMorphTypeTags.kMorphStem:
									case MoMorphTypeTags.kMorphRoot:
									case MoMorphTypeTags.kMorphParticle:
									case MoMorphTypeTags.kMorphPhrase:
									case MoMorphTypeTags.kMorphDiscontiguousPhrase:
										// Add a MoStemMsa, if needed.
										if (!haveStemMSA)
										{
											UndoableUnitOfWorkHelper.Do(ITextStrings.ksUndoAddAllomorph,
											ITextStrings.ksRedoAddAllomorph, actionHandler, () =>
											{
												le.MorphoSyntaxAnalysesOC.Add(mainCache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create());
											});
										}
										break;
									default:
										// Add a MoUnclassifiedAffixMsa, if needed.
										if (!haveUnclassifiedMSA)
										{
											UndoableUnitOfWorkHelper.Do(ITextStrings.ksUndoAddAllomorph, ITextStrings.ksRedoAddAllomorph, actionHandler, () =>
											{
												le.MorphoSyntaxAnalysesOC.Add(mainCache.ServiceLocator.GetInstance<IMoUnclassifiedAffixMsaFactory>().Create());
											});
										}
										break;
								}
								break;
							case DialogResult.Retry:
								// Rather arbitrarily we use this dialog result for the
								// Create New option.
								RunCreateEntryDlg();
								return;
							default:
								// treat as cancelled
								return;
						}
					}
				}
				IMoForm allomorph = null;
				if (dlg.MatchingForm && !dlg.InconsistentType)
				{
					allomorph = MorphServices.FindMatchingAllomorph(le, tssForm);
					if (allomorph == null)
					{
						// We matched on the Lexeme Form, not on an alternate form.
						//allomorph = MoForm.MakeMorph(cache, le, tssFullForm);
						UndoableUnitOfWorkHelper.Do(ITextStrings.ksUndoAddAllomorph, ITextStrings.ksRedoAddAllomorph, actionHandler, () =>
						{
							allomorph = MorphServices.MakeMorph(le, tssFullForm);
							UpdateMorphEntry(allomorph, le, null);
						});
					}
				}
				else
				{
					UndoableUnitOfWorkHelper.Do(ITextStrings.ksUndoAddAllomorph, ITextStrings.ksRedoAddAllomorph, actionHandler, () =>
					{
						allomorph = MorphServices.MakeMorph(le, tssFullForm);
						UpdateMorphEntry(allomorph, le, null);
					});
				}
				Debug.Assert(allomorph != null);
			}
		}

		private void GetMorphInfo(out ITsString tssForm, out ITsString tssFullForm, out IMoMorphType morphType)
		{
			IMoForm morphReal;
			GetMorphInfo(out tssForm, out tssFullForm, out morphReal, out morphType);
		}

		private void GetMorphInfo(out ITsString tssForm, out ITsString tssFullForm, out IMoForm morphReal, out IMoMorphType morphType)
		{
			morphReal = null;
			var hvoMorph = m_caches.DataAccess.get_ObjectProp(SelectedMorphHvo, SandboxBase.ktagSbMorphForm);
			var hvoMorphReal = m_caches.RealHvo(hvoMorph);
			if (hvoMorphReal != 0)
			{
				morphReal = m_caches.MainCache.ServiceLocator.GetInstance<IMoFormRepository>().GetObject(hvoMorphReal);
			}
			tssForm = m_caches.DataAccess.get_MultiStringAlt(hvoMorph, SandboxBase.ktagSbNamedObjName, m_sandbox.RawWordformWs);
			tssFullForm = m_sandbox.GetFullMorphForm(SelectedMorphHvo);
			var fullForm = tssFullForm.Text;
			morphType = null;
			if (morphReal != null)
			{
				morphType = morphReal.MorphTypeRA;
			}
			else
			{
				// if we don't have a form then we can't derive a type. (cf. LT-1621)
				if (!string.IsNullOrEmpty(fullForm))
				{
					// Find the type for this morpheme
					int clsidForm;
					var fullFormTmp = fullForm;
					morphType = MorphServices.FindMorphType(m_caches.MainCache, ref fullFormTmp, out clsidForm);
				}
			}
		}

		internal int RunAddNewSenseDlg(ITsString tssForm, ILexEntry le)
		{
			if (tssForm == null)
			{
				var hvoForm = m_caches.DataAccess.get_ObjectProp(SelectedMorphHvo, SandboxBase.ktagSbMorphForm);
				tssForm = m_caches.DataAccess.get_MultiStringAlt(hvoForm, SandboxBase.ktagSbNamedObjName, m_sandbox.RawWordformWs);
			}
			var newSenseID = 0;
			// This 'using' system is important,
			// because it calls Dispose on the dlg,
			// when it goes out of scope.
			// Otherwise, it gets disposed when the GC gets around to it,
			// and that may not happen until the app closes,
			// which causes bad problems.
			using (var dlg = new AddNewSenseDlg(m_helpTopicProvider))
			{
				dlg.SetDlgInfo(tssForm, le, new FlexComponentParameters(m_sandbox.PropertyTable, m_sandbox.Publisher, m_sandbox.Subscriber));
				var mainWnd = m_sandbox.FindForm();
				// Making the form active fixes problems like LT-2619.
				// I'm (RandyR) not sure what adverse impact might show up by doing this.
				mainWnd.Activate();
				if (dlg.ShowDialog(mainWnd) == DialogResult.OK)
				{
					dlg.GetDlgInfo(out newSenseID);
				}
			}
			return newSenseID;
		}

		// Handles a change in the item selected in the combo box.
		// Some combo items (like "Allomorph of...") can be disabled under
		// certain conditions (cf. LT-1621), but still appear in the dropdown.
		// In those cases, we don't want to hide the combo, so let the HandleSelect
		// set m_fHideCombo (to false) for those items that user tried to select.
		internal override void HandleComboSelChange(object sender, EventArgs ea)
		{
			if (m_fUnderConstruction)
			{
				return;
			}
			HandleSelect(ComboList.SelectedIndex);
			if (m_fHideCombo)
			{
				HideCombo();
			}
			else
			{
				// After skipping HideCombo, reset the flag
				m_fHideCombo = true;
			}
		}

		/// <summary>
		/// Return true if it is necessary to call HandleSelect even though we selected the
		/// current item.
		/// </summary>
		internal bool NeedSelectSame()
		{
			if (ComboList.SelectedIndex >= MorphItems.Count || ComboList.SelectedIndex < 0)
			{
				// This happens, for a reason I (JohnT) don't understand, when we launch a dialog from
				// one of these menu options using Enter, and then the dialog is also closed using enter.
				// If we return true the dialog can be launched twice.
				return false;
			}
			var sbHvo = m_sandbox.CurrentLexEntriesAnalysis(SelectedMorphHvo);
			var realObject = m_sandbox.Caches.RealObject(sbHvo);
			if (realObject == null)
			{
				return true; // nothing currently set, set whatever is current.
			}
			var classid = realObject.ClassID;
			var mi = MorphItems[ComboList.SelectedIndex];
			if (classid != LexSenseTags.kClassId && mi.m_hvoSense != 0)
			{
				return true; // item is a sense, and current value is not!
			}
			if (mi.m_hvoSense == 0)
			{
				return true; // Add New Sense...
			}
			// sense MSA has been set since analysis created, need to update it (LT-14574)
			// Review JohnT: are there any other cases where we should do it anyway?
			return m_sandbox.CurrentPos(SelectedMorphHvo) == 0;
		}

		internal override void HandleComboSelSame(object sender, EventArgs ea)
		{
			// Just close the ComboBox, since nothing changed...unless we selected a sense item and all we
			// had was an entry or msa, or some similar special case.
			if (NeedSelectSame())
			{
				HandleSelect(ComboList.SelectedIndex);
			}
			HideCombo();
		}

		public override void HandleSelect(int index)
		{
			if (index < 0 || index >= ComboList.Items.Count)
			{
				return;
			}
			var morphIndex = GetMorphIndex();
			// NOTE: m_comboList.SelectedItem does not get automatically set in (some) tests.
			// so we use index here.
			InterlinComboHandlerActionComboItem comboItem = ComboList.Items[index] as InterlinComboHandlerActionComboItem;
			if (comboItem != null)
			{
				if (!comboItem.IsEnabled)
				{
					m_fHideCombo = false;
					return;
				}
				comboItem.OnSelectItem();
				if (!(comboItem is MorphComboItem))
				{
					CopyLexEntryInfoToMonomorphemicWordGlossAndPos();
				}
				SelectEntryIcon(morphIndex);
			}
		}

		private int GetMorphIndex()
		{
			var morphIndex = 0;
			var sda = m_caches.DataAccess;
			var cmorphs = sda.get_VecSize(m_hvoSbWord, SandboxBase.ktagSbWordMorphs);
			for (; morphIndex < cmorphs; morphIndex++)
			{
				if (sda.get_VecItem(m_hvoSbWord, SandboxBase.ktagSbWordMorphs, morphIndex) == SelectedMorphHvo)
				{
					break;
				}
			}
			Debug.Assert(morphIndex < cmorphs);
			return morphIndex;
		}

		private void OnSelectCreateNewEntry(object sender, EventArgs args)
		{
			try
			{
				RunCreateEntryDlg();
			}
			catch (Exception exc)
			{
				MessageBox.Show(exc.Message, ITextStrings.ksCannotCreateNewEntry);
			}
		}

		private void OnSelectAllomorphOf(object sender, EventArgs args)
		{
			try
			{
				RunAddNewAllomorphDlg();
			}
			catch (Exception exc)
			{
				MessageBox.Show(exc.Message, ITextStrings.ksCannotAddAllomorph);
			}
		}

		private void OnSelectEditMorphBreaks(object sender, EventArgs args)
		{
			using (var handler = new IhMorphForm(m_sandbox))
			{
				handler.UpdateMorphBreaks(handler.EditMorphBreaks()); // this should launch the dialog.
			}
		}

		/// <summary />
		private void HandleSelectMorphComboItem(object sender, EventArgs args)
		{
			var mci = (MorphComboItem)sender;
			var mi = mci.MorphItem;
			var morphReal = m_caches.MainCache.ServiceLocator.GetInstance<IMoFormRepository>().GetObject(mi.m_hvoMorph);
			var morphEntryReal = mi.GetPrimaryOrOwningEntry(m_caches.MainCache);
			var tss = mi.m_name;
			var fUpdateMorphEntry = true;
			var fCreatedSense = false;
			if (mi.m_hvoSense == 0)
			{
				mi.m_hvoSense = RunAddNewSenseDlg(tss, morphEntryReal);
				if (mi.m_hvoSense == 0)
				{
					// must have canceled from the dlg.
					fUpdateMorphEntry = false;
				}
				else
				{
					fCreatedSense = true;
				}
			}
			ILexSense senseReal = null;
			ILexEntryInflType inflType = null;
			if (mi.m_hvoSense != 0)
			{
				senseReal = m_caches.MainCache.ServiceLocator.GetInstance<ILexSenseRepository>().GetObject(mi.m_hvoSense);
			}
			if (mi.m_inflType != null)
			{
				inflType = mi.m_inflType;
			}
			if (!fUpdateMorphEntry)
			{
				return;
			}
			// If we've created something, then updating the sandbox needs to be undone as a unit with it,
			// so the sandbox isn't left showing something uncreated.
			// If we are already in a UOW, we can just add the focus box to it.
			// If we didn't create a sense, we can just let the focus box Undo be discarded.
			if (m_caches.MainCache.ActionHandlerAccessor.CurrentDepth > 0)
			{
				UpdateMorphEntry(morphReal, morphEntryReal, senseReal, inflType); // already in UOW, join it
			}
			else
			{
				// But if we created something in a separate UOW that is now over, we need to make the
				// focus box action in a new UOW, then merge the two.
				UndoableUnitOfWorkHelper.Do(ITextStrings.ksUndoAddSense, ITextStrings.ksRedoAddSense, m_caches.MainCache.ActionHandlerAccessor,
					() => UpdateMorphEntry(morphReal, morphEntryReal, senseReal, inflType));
				if (fCreatedSense)
				{
					((IActionHandlerExtensions)m_caches.MainCache.ActionHandlerAccessor).MergeLastTwoUnitsOfWork();
				}
			}
		}

		private void SetLexEntryToUnknown(object sender, EventArgs args)
		{
			var sda = m_caches.DataAccess;
			var cda = (IVwCacheDa)m_caches.DataAccess;
			cda.CacheObjProp(SelectedMorphHvo, SandboxBase.ktagSbMorphEntry, 0);
			cda.CacheObjProp(SelectedMorphHvo, SandboxBase.ktagSbMorphGloss, 0);
			cda.CacheObjProp(SelectedMorphHvo, SandboxBase.ktagSbMorphPos, 0);
			// Forget we had an existing wordform; otherwise, the program considers
			// all changes to be editing the wordform, and since it belongs to the
			// old analysis, the old analysis gets resurrected.
			m_sandbox.WordGlossHvo = 0;
			// The current ktagSbMorphForm property is for an SbNamedObject that
			// is associated with an MoForm belonging to the LexEntry that we are
			// trying to dissociate from. If we leave it that way, it will resurrect
			// the LexEntry connection when we update the real cache.
			// Instead make a new named object for the form.
			var tssForm = sda.get_MultiStringAlt(sda.get_ObjectProp(SelectedMorphHvo, SandboxBase.ktagSbMorphForm), SandboxBase.ktagSbNamedObjName, m_sandbox.RawWordformWs);
			var hvoNewForm = sda.MakeNewObject(SandboxBase.kclsidSbNamedObj, SelectedMorphHvo, SandboxBase.ktagSbMorphForm, -2);
			sda.SetMultiStringAlt(hvoNewForm, SandboxBase.ktagSbNamedObjName, m_sandbox.RawWordformWs, tssForm);
			// Send notifiers for each of these deleted items.
			sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll, SelectedMorphHvo, SandboxBase.ktagSbMorphEntry, 0, 0, 1);
			sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll, SelectedMorphHvo, SandboxBase.ktagSbMorphGloss, 0, 0, 1);
			sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll, SelectedMorphHvo, SandboxBase.ktagSbMorphPos, 0, 0, 1);
		}

		private void OnSelectVariantOf(object sender, EventArgs args)
		{
			try
			{
				using (var dlg = new LinkVariantToEntryOrSense())
				{
					dlg.InitializeFlexComponent(new FlexComponentParameters(m_sandbox.PropertyTable, m_sandbox.Publisher, m_sandbox.Subscriber));
					// if no previous variant relationship has been defined,
					// then don't try to fill initial information,
					ITsString tssForm;
					ITsString tssFullForm;
					IMoMorphType morphType;
					IMoForm morphReal;
					GetMorphInfo(out tssForm, out tssFullForm, out morphReal, out morphType);
					if (morphReal != null && morphReal.IsValidObject)
					{
						var variantEntry = morphReal.Owner as ILexEntry;
						dlg.SetDlgInfo(m_sandbox.Cache, variantEntry);
					}
					else
					{
						// since we didn't start with an entry,
						// set up the dialog using the form of the variant
						dlg.SetDlgInfo(m_sandbox.Cache, tssForm);
					}
					dlg.SetHelpTopic("khtpAddVariantFromInterlinear");
					var mainWnd = m_sandbox.FindForm();
					// Making the form active fixes problems like LT-2619.
					// I'm (RandyR) not sure what adverse impact might show up by doing this.
					mainWnd.Activate();
					if (dlg.ShowDialog(mainWnd) != DialogResult.OK)
					{
						return;
					}
					if (dlg.SelectedObject == null)
					{
						return; // odd. nothing more to do.
					}
					var variantEntryRef = dlg.VariantEntryRefResult;
					// if we didn't have a starting entry, create one now.
					var variantResult = variantEntryRef.Owner as ILexEntry;
					var hvoVariantType = dlg.SelectedVariantEntryTypeHvo;
					ILexEntryInflType inflType;
					m_caches.MainCache.ServiceLocator.GetInstance<ILexEntryInflTypeRepository>().TryGetObject(hvoVariantType, out inflType);
					// we need to create a new LexEntryRef.
					var morphBundleEntry = dlg.SelectedObject as ILexEntry;
					var morphBundleSense = dlg.SelectedObject as ILexSense;
					if (morphBundleSense != null)
					{
						morphBundleEntry = morphBundleSense.OwnerOfClass(LexEntryTags.kClassId) as ILexEntry;
					}
					UpdateMorphEntry(variantResult.LexemeFormOA, morphBundleEntry, morphBundleSense, inflType);
				}

			}
			catch (Exception exc)
			{
				MessageBox.Show(exc.Message, ITextStrings.ksCannotAddVariant);
			}
		}

		protected virtual void SelectEntryIcon(int morphIndex)
		{
			m_sandbox.SelectEntryIconOfMorph(morphIndex);
		}

		/// <summary>
		/// Update the sandbox cache to reflect a choice of the real MoForm and the
		/// entry indicated by the LcmCache hvos passed.
		/// </summary>
		internal void UpdateMorphEntry(IMoForm moFormReal, ILexEntry entryReal, ILexSense senseReal, ILexEntryInflType inflType = null)
		{
			var fDirty = m_sandbox.Caches.DataAccess.IsDirty();
			var fApproved = !m_sandbox.UsingGuess;
			var fHasApprovedWordGloss = m_sandbox.HasWordGloss() && (fDirty || fApproved);
			var fHasApprovedWordCat = m_sandbox.HasWordCat && (fDirty || fApproved);
			var undoAction = new UpdateMorphEntryAction(m_sandbox, SelectedMorphHvo); // before changes.
			// Make a new morph, if one does not already exist, corresponding to the
			// selected item.  Its form must match what is already displayed.  Store it as
			// the new value.
			var hvoMorph = m_sandbox.CreateSecondaryAndCopyStrings(InterlinLineChoices.kflidMorphemes, moFormReal.Hvo, MoFormTags.kflidForm);
			m_caches.DataAccess.SetObjProp(SelectedMorphHvo, SandboxBase.ktagSbMorphForm, hvoMorph);
			m_caches.DataAccess.PropChanged(m_rootb, (int)PropChangeType.kpctNotifyAll, SelectedMorphHvo, SandboxBase.ktagSbMorphForm, 0, 1, 1);
			// Try to establish the sense.  Call this before SetSelectedEntry and LoadSecDataForEntry.
			// reset cached gloss, since we should establish the sense according to the real sense or real entry.
			m_caches.DataAccess.SetObjProp(SelectedMorphHvo, SandboxBase.ktagSbMorphGloss, 0);
			var morphEntry = moFormReal.Owner as ILexEntry;
			var realDefaultSense = m_sandbox.EstablishDefaultSense(SelectedMorphHvo, morphEntry, senseReal, inflType);
			// Make and install a secondary object to correspond to the real LexEntry.
			// (The zero says we are not guessing any more, since the user selected this entry.)
			m_sandbox.LoadSecDataForEntry(morphEntry, senseReal ?? realDefaultSense, m_hvoSbWord, m_caches.DataAccess as IVwCacheDa, m_wsVern, SelectedMorphHvo, 0, m_caches.MainCache.MainCacheAccessor);
			m_caches.DataAccess.PropChanged(m_rootb, (int)PropChangeType.kpctNotifyAll, SelectedMorphHvo, SandboxBase.ktagSbMorphEntry, 0, 1, WasReal());
			// Notify any delegates that the selected Entry changed.
			m_sandbox.SetSelectedEntry(entryReal);
			// fHasApprovedWordGloss: if an approved word gloss already exists -- don't replace it
			// fHasApprovedWordCat: if an approved word category already exists -- don't replace it
			CopyLexEntryInfoToMonomorphemicWordGlossAndPos(!fHasApprovedWordGloss, !fHasApprovedWordCat);
			undoAction.GetNewVals();
			// If we're doing this as part of something undoable, and then undo it, we should undo this also,
			// especially so the Sandbox isn't left displaying something whose creation has been undone. (FWR-3547)
			if (m_caches.MainCache.ActionHandlerAccessor.CurrentDepth > 0)
			{
				m_caches.MainCache.ActionHandlerAccessor.AddAction(undoAction);
			}
		}
		protected virtual void CopyLexEntryInfoToMonomorphemicWordGlossAndPos()
		{
			// do nothing in general.
		}

		protected virtual void CopyLexEntryInfoToMonomorphemicWordGlossAndPos(bool fCopyToWordGloss, bool fCopyToWordPos)
		{
			// conditionally set up the word gloss and POS to correspond to monomorphemic lex morph entry info.
			SyncMonomorphemicGlossAndPos(fCopyToWordGloss, fCopyToWordPos);
			// Forget we had an existing wordform; otherwise, the program considers
			// all changes to be editing the wordform, and since it belongs to the
			// old analysis, the old analysis gets resurrected.
			m_sandbox.WordGlossHvo = 0;
		}
	}
}