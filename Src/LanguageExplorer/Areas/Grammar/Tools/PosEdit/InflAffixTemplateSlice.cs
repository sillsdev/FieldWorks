// Copyright (c) 2003-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.Controls.XMLViews;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.FwCoreDlgs.Controls;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;
using SIL.Xml;

namespace LanguageExplorer.Areas.Grammar.Tools.PosEdit
{
	/// <summary />
	internal class InflAffixTemplateSlice : ViewSlice
	{
		/// <summary>
		/// This method, called once we have a cache and object, is our first chance to
		/// actually create the embedded control.
		/// </summary>
		public override void FinishInit()
		{
			var wsContainer = Cache.ServiceLocator.WritingSystems;
			var vernWsIsRTL = wsContainer.DefaultVernacularWritingSystem.RightToLeftScript;
			var analWsIsRTL = wsContainer.DefaultAnalysisWritingSystem.RightToLeftScript;
			var layoutText = XmlUtils.GetMandatoryAttributeValue(ConfigurationNode, "layout");
			// To properly fix LT-6239, we need to consider all four mixtures of directionality
			// involving the vernacular (table) and analysis (slot name) writing systems.
			// These four possibilities are marked RTL, LTRinRTL, RTLinLTR, and <nothing>.
			if (vernWsIsRTL && analWsIsRTL)
			{
				if (layoutText.EndsWith("RTLinLTR") || layoutText.EndsWith("LTRinRTL"))
				{
					layoutText = layoutText.Substring(0, layoutText.Length - 8);
				}
				if (!layoutText.EndsWith("RTL"))
				{
					layoutText += "RTL"; // both vern and anal are RTL
				}
			}
			else if (vernWsIsRTL)
			{
				if (layoutText.EndsWith("RTLinLTR"))
				{
					layoutText = layoutText.Substring(0, layoutText.Length - 8);
				}
				else if (layoutText.EndsWith("RTL") && !layoutText.EndsWith("LTRinRTL"))
				{
					layoutText = layoutText.Substring(0, layoutText.Length - 3);
				}
				if (!layoutText.EndsWith("LTRinRTL"))
				{
					layoutText += "LTRinRTL"; // LTR anal name in RTL vern table
				}
			}
			else if (analWsIsRTL)
			{
				if (layoutText.EndsWith("LTRinRTL"))
				{
					layoutText = layoutText.Substring(0, layoutText.Length - 8);
				}
				else if (layoutText.EndsWith("RTL"))
				{
					layoutText = layoutText.Substring(0, layoutText.Length - 3);
				}
				if (!layoutText.EndsWith("RTLinLTR"))
				{
					layoutText += "RTLinLTR"; // RTL anal name in LTR vern table
				}
			}
			else
			{
				if (layoutText.EndsWith("RTLinLTR") || layoutText.EndsWith("LTRinRTL"))
				{
					layoutText = layoutText.Substring(0, layoutText.Length - 8);
				}
				else if (layoutText.EndsWith("RTL"))
				{
					layoutText = layoutText.Substring(0, layoutText.Length - 3);
				}
				// both vern and anal are LTR (unmarked case)
			}
			Control = new InflAffixTemplateControl((IMoInflAffixTemplate)MyCmObject, layoutText, new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
			InternalInitialize();
		}

		/// <summary />
		private sealed class InflAffixTemplateControl : XmlView
		{
			/// <summary>
			/// Handles creating the combo list items for the inflectional affix template and funneling commands to the control.
			/// </summary>
			private InflAffixTemplateMenuHandler _menuHandler;
			private ICmObject _clickedCmObject;
			private IMoInflAffixSlot _msaSlot;
			private IMoInflAffixTemplate _template;
			private string _stem;
			private string _slotChooserTitle;
			private string _slotChooserInstructionalText;
			private string _obligatorySlot;
			private string _optionalSlot;
			private string _newSlotName;
			private string _unnamedSlotName;
			private string _inflAffixChooserTitle;
			private string _inflAffixChooserInstructionalTextReq;
			private string _inflAffixChooserInstructionalTextOpt;
			private string _inflAffix;
			private const string ChooseInflectionalAffixHelpTopic = "InflectionalAffixes";
			private const string InflTemplateAddInflAffixMsa = "InflTemplateAddInflAffixMsa";
			private const string InflTemplateInsertSlotBefore = "InflTemplateInsertSlotBefore";
			private const string InflTemplateInsertSlotAfter = "InflTemplateInsertSlotAfter";
			private const string InflTemplateMoveSlotLeft = "InflTemplateMoveSlotLeft";
			private const string InflTemplateMoveSlotRight = "InflTemplateMoveSlotRight";
			private const string InflTemplateToggleSlotOptionality = "InflTemplateToggleSlotOptionality";
			private const string InflTemplateRemoveSlot = "InflTemplateRemoveSlot";
			private const string InflTemplateRemoveInflAffixMsa = "InflTemplateRemoveInflAffixMsa";
			private const string JumpToTool = "JumpToTool";
			private const string InflAffixTemplateHelp = "InflAffixTemplateHelp";

			private event EventHandler ShowContextMenu;

			internal InflAffixTemplateControl(IMoInflAffixTemplate template, string layout, FlexComponentParameters flexComponentParameters)
				: base(template.Hvo, layout, true)
			{
				_template = template;
				InitializeFlexComponent(flexComponentParameters);
			}

			public override void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
			{
				base.InitializeFlexComponent(flexComponentParameters);

				Cache = PropertyTable.GetValue<LcmCache>(FwUtils.cache);
				var handlers = new Dictionary<string, Tuple<EventHandler<EventArgs>, string>>
				{
					{ InflTemplateAddInflAffixMsa, new Tuple<EventHandler<EventArgs>, string>(InflTemplateAddInflAffixMsa_Clicked, GrammarResources.Add_inflectional_affix_es_to_XXX)},
					{ InflTemplateInsertSlotBefore, new Tuple<EventHandler<EventArgs>, string>(InflTemplateInsertSlotBefore_Clicked, GrammarResources.Insert_Slot_before_XXX)},
					{ InflTemplateInsertSlotAfter, new Tuple<EventHandler<EventArgs>, string>(InflTemplateInsertSlotAfter_Clicked, GrammarResources.Insert_Slot_after_XXX)},
					{ InflTemplateMoveSlotLeft, new Tuple<EventHandler<EventArgs>, string>(InflTemplateMoveSlotLeft_Clicked, GrammarResources.Move_XXX_back_one_Slot)},
					{ InflTemplateMoveSlotRight, new Tuple<EventHandler<EventArgs>, string>(InflTemplateMoveSlotRight_Clicked, GrammarResources.Move_XXX_forward_one_Slot)},
					{ InflTemplateToggleSlotOptionality, new Tuple<EventHandler<EventArgs>, string>(InflTemplateToggleSlotOptionality_Clicked, GrammarResources.Change_Optionality_of_XXX_Slot)},
					{ InflTemplateRemoveSlot, new Tuple<EventHandler<EventArgs>, string>(InflTemplateRemoveSlot_Clicked, GrammarResources.Remove_XXX_Slot)},
					{ InflTemplateRemoveInflAffixMsa, new Tuple<EventHandler<EventArgs>, string>(InflTemplateRemoveInflAffixMsa_Clicked, GrammarResources.Remove_YYY_from_XXX)},
					{ JumpToTool, new Tuple<EventHandler<EventArgs>, string>(JumpToTool_Clicked, AreaResources.ksShowEntryInLexicon)},
					{ InflAffixTemplateHelp, new Tuple<EventHandler<EventArgs>, string>(InflAffixTemplateHelp_Clicked, LanguageExplorerResources.ksHelp)}
				};
				_menuHandler = new InflAffixTemplateMenuHandler(PropertyTable, this, handlers, PropertyTable.GetValue<Form>(FwUtils.window));
				SetStringTableValues();
				if (RootBox == null)
				{
					MakeRoot();
				}
			}

			protected override void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
				if (IsDisposed)
				{
					// No need to run it more than once.
					return;
				}

				base.Dispose(disposing);

				if (disposing)
				{
					_menuHandler?.Dispose();
				}
				_template = null;
				_stem = null;
				_slotChooserTitle = null;
				_slotChooserInstructionalText = null;
				_obligatorySlot = null;
				_optionalSlot = null;
				_newSlotName = null;
				_unnamedSlotName = null;
				_menuHandler = null;
			}

			protected override void OnValidating(System.ComponentModel.CancelEventArgs e)
			{
				base.OnValidating(e);
				OnLostFocus(new EventArgs());
			}

			private void SetStringTableValues()
			{
				_stem = StringTable.Table.GetString("Stem", "Linguistics/Morphology/TemplateTable");
				_slotChooserTitle = StringTable.Table.GetString("SlotChooserTitle", "Linguistics/Morphology/TemplateTable");
				_slotChooserInstructionalText = StringTable.Table.GetString("SlotChooserInstructionalText", "Linguistics/Morphology/TemplateTable");
				_obligatorySlot = StringTable.Table.GetString("ObligatorySlot", "Linguistics/Morphology/TemplateTable");
				_optionalSlot = StringTable.Table.GetString("OptionalSlot", "Linguistics/Morphology/TemplateTable");
				_newSlotName = StringTable.Table.GetString("NewSlotName", "Linguistics/Morphology/TemplateTable");
				_unnamedSlotName = StringTable.Table.GetString("UnnamedSlotName", "Linguistics/Morphology/TemplateTable");
				_inflAffixChooserTitle = StringTable.Table.GetString("InflAffixChooserTitle", "Linguistics/Morphology/TemplateTable");
				_inflAffixChooserInstructionalTextReq = StringTable.Table.GetString("InflAffixChooserInstructionalTextReq", "Linguistics/Morphology/TemplateTable");
				_inflAffixChooserInstructionalTextOpt = StringTable.Table.GetString("InflAffixChooserInstructionalTextOpt", "Linguistics/Morphology/TemplateTable");
				_inflAffix = StringTable.Table.GetString("InflAffix", "Linguistics/Morphology/TemplateTable");
			}

			/// <summary>
			/// Intercepts mouse clicks on Command Icons and translates them into right mouse clicks
			/// </summary>
			protected override void OnMouseUp(MouseEventArgs e)
			{
				Rectangle rcSrcRoot;
				Rectangle rcDstRoot;
				Point pt;
				int tag;
				using (new HoldGraphics(this))
				{
					pt = PixelToView(new Point(e.X, e.Y));
					GetCoordRects(out rcSrcRoot, out rcDstRoot);
					var sel = RootBox.MakeSelAt(pt.X, pt.Y, rcSrcRoot, rcDstRoot, false);
					ITsString tss;
					int ichAnchor;
					bool fAssocPrev;
					int hvoObj;
					int ws;
					sel.TextSelInfo(false, out tss, out ichAnchor, out fAssocPrev, out hvoObj, out tag, out ws);
				}
				if (tag == 0) // indicates it is an icon
				{
					OnRightMouseUp(pt, rcSrcRoot, rcDstRoot);
				}
				else
				{
					base.OnMouseUp(e);
				}
			}

			protected override bool OnRightMouseUp(Point pt, Rectangle rcSrcRoot, Rectangle rcDstRoot)
			{
				var slice = FindParentSlice();
				Guard.AgainstNull(slice, nameof(slice));
				if (slice != null)
				{
					// Make sure we are a current slice so we are a colleague so we can enable menu items.
					if (slice != slice.ContainingDataTree.CurrentSlice)
					{
						slice.ContainingDataTree.CurrentSlice = slice;
					}
				}
				if (ShowContextMenu == null)
				{
					return base.OnRightMouseUp(pt, rcSrcRoot, rcDstRoot);
				}
				var sel = RootBox.MakeSelAt(pt.X, pt.Y, new Rect(rcSrcRoot.Left, rcSrcRoot.Top, rcSrcRoot.Right, rcSrcRoot.Bottom), new Rect(rcDstRoot.Left, rcDstRoot.Top, rcDstRoot.Right, rcDstRoot.Bottom), false);
				if (sel == null)
				{
					return base.OnRightMouseUp(pt, rcSrcRoot, rcDstRoot); // no object, so quit and let base handle it
				}
				int index;
				int hvo, tag, prev; // dummies.
									// dummy
				IVwPropertyStore vps;
				// Level 0 would give info about ktagText and the hvo of the dummy line object.
				// Level 1 gives info about which line object it is in the root.
				sel.PropInfo(false, 0, out hvo, out tag, out index, out prev, out vps);  // using level 1 for an msa should return the slot it belongs in
#if MaybeSomeDayToTryAndGetRemoveMsaCorrectForCircumfixes
				int indexSlot;
				int hvoSlot, tagSlot, prevSlot; // dummies.
				IVwPropertyStore vpsSlot; // dummy
				sel.PropInfo(false, 1, out hvoSlot, out tagSlot, out indexSlot, out prevSlot, out vpsSlot);
				int classSlot = Cache.GetClassOfObject(hvoSlot);
				if (classSlot == LCM.Ling.MoInflAffixSlot.kClassId)
				{
					m_hvoSlot = hvoSlot;
				}
#endif
				_clickedCmObject = Cache.ServiceLocator.GetObject(hvo);
				ShowContextMenu(this, new EventArgs());
				return true; // we've handled it
			}

			/// <summary>
			/// The slice is no longer a direct parent, so hunt for it up the Parent chain.
			/// </summary>
			private Slice FindParentSlice()
			{
				var ctl = Parent;
				while (ctl != null)
				{
					var slice = ctl as Slice;
					if (slice != null)
					{
						return slice;
					}
					ctl = ctl.Parent;
				}
				return null;
			}

			private void InflTemplateInsertSlotBefore_Clicked(object sender, EventArgs e)
			{
				HandleInsert(true);
			}

			private void InflTemplateInsertSlotAfter_Clicked(object sender, EventArgs e)
			{
				HandleInsert(false);
			}

			private void InflTemplateMoveSlotLeft_Clicked(object sender, EventArgs e)
			{
				HandleMove();
			}

			private void InflTemplateMoveSlotRight_Clicked(object sender, EventArgs e)
			{
				HandleMove(false);
			}

			private void HandleMove(bool moveLeft = true)
			{
				ILcmReferenceSequence<IMoInflAffixSlot> seq;
				int index;
				var slot = _clickedCmObject as IMoInflAffixSlot;
				GetAffixSequenceContainingSlot(slot, out seq, out index);
				var baseText = DetermineSlotContextMenuItemLabel(moveLeft ? GrammarResources.Move_XXX_back_one_Slot : GrammarResources.Move_XXX_forward_one_Slot).Text;
				UowHelpers.UndoExtension(baseText, Cache.ServiceLocator.GetInstance<IActionHandler>(), () =>
				{
					seq.RemoveAt(index);
					var iOffset = moveLeft ? -1 : 1;
					seq.Insert(index + iOffset, slot);
				});
			}

			private void InflTemplateToggleSlotOptionality_Clicked(object sender, EventArgs e)
			{
				var slot = _clickedCmObject as IMoInflAffixSlot;
				if (slot == null)
				{
					return;
				}
				var slotName = slot.Name.BestAnalysisVernacularAlternative.Text;
				var undoText = string.Format(AreaResources.ksUndoChangeOptionalityOfSlot, slotName);
				var redoText = string.Format(AreaResources.ksRedoChangeOptionalityOfSlot, slotName);
				using (var helper = new UndoableUnitOfWorkHelper(Cache.ActionHandlerAccessor, undoText, redoText))
				{
					slot.Optional = !slot.Optional;
					helper.RollBack = false;
				}
				RootBox.Reconstruct();
			}

			private void InflTemplateRemoveSlot_Clicked(object sender, EventArgs e)
			{
				ILcmReferenceSequence<IMoInflAffixSlot> seq;
				int index;
				GetAffixSequenceContainingSlot(_clickedCmObject as IMoInflAffixSlot, out seq, out index);
				var baseUowText = seq[index].Name.BestAnalysisVernacularAlternative.Text;
				using (var helper = new UndoableUnitOfWorkHelper(m_cache.ActionHandlerAccessor, string.Format(AreaResources.ksUndoRemovingSlot, baseUowText), string.Format(AreaResources.ksRedoRemovingSlot, baseUowText)))
				{
					seq.RemoveAt(index);
					helper.RollBack = false;
				}
			}

			private void JumpToTool_Clicked(object sender, EventArgs e)
			{
				LinkHandler.PublishFollowLinkMessage(Publisher, new FwLinkArgs(AreaServices.LexiconEditMachineName, ((IMoInflAffMsa)_clickedCmObject).Owner.Guid));
			}

			private void InflTemplateRemoveInflAffixMsa_Clicked(object sender, EventArgs e)
			{
				// the user says to remove this affix (msa) from the slot;
				// if there are other infl affix msas in the entry, we delete the MoInflAffMsa completely;
				// otherwise, we remove the slot info.
				var inflMsa = _clickedCmObject as IMoInflAffMsa;
				var lex = inflMsa?.OwnerOfClass<ILexEntry>();
				if (lex == null)
				{
					return; // play it safe
				}
				UndoableUnitOfWorkHelper.Do(AreaResources.ksUndoRemovingAffix, AreaResources.ksRedoRemovingAffix, Cache.ActionHandlerAccessor, () =>
				{
					if (OtherInflAffixMsasExist(lex, inflMsa))
					{
						// remove this msa because there are others
						lex.MorphoSyntaxAnalysesOC.Remove(inflMsa);
					}
					else
					{
						// this is the only one; remove it
						inflMsa.SlotsRC.Clear();
					}
				});
				RootBox.Reconstruct();  // work around because <choice> is not smart enough to remember its dependencies
			}

			private static bool OtherInflAffixMsasExist(ILexEntry lex, IMoInflAffMsa inflMsa)
			{
				return lex.MorphoSyntaxAnalysesOC.Where(msa => msa.ClassID == MoInflAffMsaTags.kClassId).Any(msa => msa != inflMsa);
			}

			private void InflTemplateAddInflAffixMsa_Clicked(object sender, EventArgs e)
			{
				using (var chooser = MakeChooserWithExtantMsas(_msaSlot))
				{
					chooser.ShowDialog();
					if (chooser.DialogResult == DialogResult.OK)
					{
						if (chooser.ChosenObjects != null && chooser.ChosenObjects.Any())
						{
							UndoableUnitOfWorkHelper.Do(AreaResources.ksUndoAddAffixes, AreaResources.ksRedoAddAffixes, Cache.ActionHandlerAccessor, () =>
							{
								foreach (var obj in chooser.ChosenObjects)
								{
									AddInflAffixMsaToSlot(obj, _msaSlot);
								}
							});
						}
					}
				}
			}

			private void AddInflAffixMsaToSlot(ICmObject obj, IMoInflAffixSlot slot)
			{
				var inflMsa = obj as IMoInflAffMsa;
				var lex = inflMsa?.OwnerOfClass<ILexEntry>();
				if (lex == null)
				{
					// play it safe
					return;
				}
				// assume we won't find an existing infl affix msa
				var miamIsSet = false;
				foreach (var msa in lex.MorphoSyntaxAnalysesOC)
				{
					if (msa.ClassID != MoInflAffMsaTags.kClassId)
					{
						// is an inflectional affix msa
						continue;
					}
					var miam = (IMoInflAffMsa)msa;
					var pos = miam.PartOfSpeechRA;
					if (pos == null)
					{
						// use the first unspecified one
						miam.PartOfSpeechRA = slot.OwnerOfClass<IPartOfSpeech>();
						// just in case...
						miam.SlotsRC.Clear();
						miam.SlotsRC.Add(slot);
						miamIsSet = true;
						break;
					}
					if (!pos.AllAffixSlots.Contains(slot))
					{
						continue;
					}
					// if the slot is in this POS
					if (miam.SlotsRC.Count == 0)
					{
						// use the first available
						miam.SlotsRC.Add(slot);
						miamIsSet = true;
						break;
					}
					if (miam.SlotsRC.Contains(slot))
					{
						// it is already set (probably done by the CreateEntry dialog process)
						miamIsSet = true;
						break;
					}
					if (lex.IsCircumfix())
					{
						// only circumfixes can more than one slot
						miam.SlotsRC.Add(slot);
						miamIsSet = true;
						break;
					}
				}
				if (miamIsSet)
				{
					return; // need to create a new infl affix msa
				}
				var newMsa = Cache.ServiceLocator.GetInstance<IMoInflAffMsaFactory>().Create();
				lex.MorphoSyntaxAnalysesOC.Add(newMsa);
				EnsureNewMsaHasSense(lex, newMsa);
				newMsa.SlotsRC.Add(slot);
				newMsa.PartOfSpeechRA = slot.OwnerOfClass<IPartOfSpeech>();
			}

			private void EnsureNewMsaHasSense(ILexEntry lex, IMoInflAffMsa newMsa)
			{
				// if no lexsense has this msa, copy first sense and have it refer to this msa
				var fASenseHasMsa = false;
				foreach (var sense in lex.AllSenses)
				{
					if (sense.MorphoSyntaxAnalysisRA == newMsa)
					{
						fASenseHasMsa = true;
						break;
					}
				}
				if (fASenseHasMsa)
				{
					return;
				}
				var newSense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				lex.SensesOS.Add(newSense);
				var firstSense = lex.SensesOS[0];
				// only copying gloss for now and only copying default analysis ws
				//newSense.Definition.AnalysisDefaultWritingSystem.Text = firstSense.Definition.AnalysisDefaultWritingSystem.Text;
				newSense.Gloss.AnalysisDefaultWritingSystem = firstSense.Gloss.AnalysisDefaultWritingSystem;
				//newSense.GrammarNote.AnalysisDefaultWritingSystem.Text = firstSense.GrammarNote.AnalysisDefaultWritingSystem.Text;
				//newSense.SemanticsNote.AnalysisDefaultWritingSystem.Text = firstSense.SemanticsNote.AnalysisDefaultWritingSystem.Text;
				newSense.MorphoSyntaxAnalysisRA = newMsa;
			}

			private SimpleListChooser MakeChooserWithExtantMsas(IMoInflAffixSlot slot)
			{
				// Want the list of all lex entries which have an infl affix Msa
				// Do not want to list the infl affix Msas that are already assigned to the slot.
				var candidates = new HashSet<ICmObject>();
				var isPrefixSlot = _template.PrefixSlotsRS.Contains(slot);
				foreach (var lex in slot.OtherInflectionalAffixLexEntries)
				{
					var include = EntryHasAffixThatMightBeInSlot(lex, isPrefixSlot);
					if (include)
					{
						foreach (var msa in lex.MorphoSyntaxAnalysesOC)
						{
							if (msa is IMoInflAffMsa)
							{
								candidates.Add(msa);
								break;
							}
						}
					}
				}
				var labels = ObjectLabel.CreateObjectLabels(Cache, candidates.OrderBy(iafmsa => iafmsa.Owner.ShortName), null);
				var persistProvider = PersistenceProviderFactory.CreatePersistenceProvider(PropertyTable);
				var aiForceMultipleChoices = new ICmObject[0];
				var chooser = new SimpleListChooser(persistProvider, labels, ChooseInflectionalAffixHelpTopic, Cache, aiForceMultipleChoices, PropertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider));
				chooser.SetHelpTopic("khtpChoose-Grammar-InflAffixTemplateControl");
				chooser.SetFontForDialog(new[] { Cache.DefaultVernWs, Cache.DefaultAnalWs }, StyleSheet, WritingSystemFactory);
				chooser.Cache = Cache;
				// We don't want the ()'s indicating optionality since the text spells it out.
				chooser.TextParam = slot.Name.AnalysisDefaultWritingSystem.Text;
				chooser.Title = _inflAffixChooserTitle;
				chooser.InstructionalText = slot.Optional ? _inflAffixChooserInstructionalTextOpt : _inflAffixChooserInstructionalTextReq;
				chooser.AddLink(_inflAffix, LinkType.kDialogLink, new MakeInflAffixEntryChooserCommand(Cache, true, _inflAffix, isPrefixSlot, slot, PropertyTable, Publisher, Subscriber));
				chooser.SetObjectAndFlid(slot.Hvo, slot.OwningFlid);
				chooser.ReplaceTreeView(PropertyTable, Publisher, Subscriber, "InflAffixMsaFlatList");
				return chooser;
			}

			/// <summary>
			/// Determine if the lex entry can appear in the prefix/suffix slot
			/// </summary>
			/// <returns>true if the lex entry can appear in the slot</returns>
			private static bool EntryHasAffixThatMightBeInSlot(ILexEntry lex, bool isPrefixSlot)
			{
				var include = false; // be pessimistic
				var morphTypes = lex.MorphTypes;
				foreach (var morphType in morphTypes)
				{
					if (isPrefixSlot)
					{
						if (morphType.IsPrefixishType)
						{
							include = true;
							break;
						}
					}
					else
					{
						// is a suffix slot
						if (morphType.IsSuffixishType)
						{
							include = true;
							break;
						}
					}
				}
				return include;
			}

			private void HandleInsert(bool before)
			{
				var isPrefixSlot = GetIsPrefixSlot(before);
				using (var chooser = MakeChooserWithExtantSlots(isPrefixSlot))
				{
					chooser.ShowDialog(this);
					if (chooser.ChosenOne == null)
					{
						return;
					}
					var chosenSlot = chooser.ChosenOne.Object as IMoInflAffixSlot;
					var flid = 0;
					var ihvo = -1;
					switch (_clickedCmObject.ClassID)
					{
						case MoInflAffixSlotTags.kClassId:
							HandleInsertAroundSlot(before, chosenSlot, out flid, out ihvo);
							break;
						case MoInflAffixTemplateTags.kClassId:
							HandleInsertAroundStem(before, chosenSlot, out flid, out ihvo);
							break;
					}
					RootBox.Reconstruct(); // Ensure that the table gets redrawn
					if (!chooser.LinkExecuted)
					{
						return;
					}
					// Select the header of the newly added slot in case the user wants to edit it.
					// See LT-8209.
					var rgvsli = new SelLevInfo[1];
					rgvsli[0].hvo = chosenSlot.Hvo;
					rgvsli[0].ich = -1;
					rgvsli[0].ihvo = ihvo;
					rgvsli[0].tag = flid;
					RootBox.MakeTextSelInObj(0, 1, rgvsli, 0, null, true, true, true, false, true);
				}
			}

			private bool GetIsPrefixSlot(bool before)
			{
				var fIsPrefixSlot = false;
				switch (_clickedCmObject.ClassID)
				{
					case MoInflAffixTemplateTags.kClassId:
						fIsPrefixSlot = before;
						break;
					case MoInflAffixSlotTags.kClassId:
						fIsPrefixSlot = _template.PrefixSlotsRS.Contains(_clickedCmObject as IMoInflAffixSlot);
						break;
				}
				return fIsPrefixSlot;
			}

			private SimpleListChooser MakeChooserWithExtantSlots(bool isPrefixSlot)
			{
				var slotFlid = isPrefixSlot ? MoInflAffixTemplateTags.kflidPrefixSlots : MoInflAffixTemplateTags.kflidSuffixSlots;
				var labels = ObjectLabel.CreateObjectLabels(Cache, _template.ReferenceTargetCandidates(slotFlid), null);
				var persistProvider = PersistenceProviderFactory.CreatePersistenceProvider(PropertyTable);
				var chooser = new SimpleListChooser(persistProvider, labels, "Slot", PropertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider));
				chooser.SetHelpTopic("khtpChoose-Grammar-InflAffixTemplateControl");
				chooser.Cache = Cache;
				chooser.TextParamHvo = _template.Owner.Hvo;
				chooser.Title = _slotChooserTitle;
				chooser.InstructionalText = _slotChooserInstructionalText;
				string sTopPOS;
				var pos = GetHighestPOS(_template.OwnerOfClass<IPartOfSpeech>(), out sTopPOS);
				var sLabel = string.Format(_obligatorySlot, sTopPOS);
				chooser.AddLink(sLabel, LinkType.kSimpleLink, new MakeInflAffixSlotChooserCommand(Cache, true, sLabel, pos.Hvo, false, PropertyTable, Publisher, Subscriber));
				sLabel = string.Format(_optionalSlot, sTopPOS);
				chooser.AddLink(sLabel, LinkType.kSimpleLink, new MakeInflAffixSlotChooserCommand(Cache, true, sLabel, pos.Hvo, true, PropertyTable, Publisher, Subscriber));
				chooser.SetObjectAndFlid(pos.Hvo, MoInflAffixTemplateTags.kflidSlots);
				return chooser;
			}

			private static IPartOfSpeech GetHighestPOS(IPartOfSpeech pos, out string topPOS)
			{
				IPartOfSpeech result = null;
				topPOS = AreaResources.ksQuestions;
				ICmObject obj = pos;
				while (obj.ClassID == PartOfSpeechTags.kClassId)
				{
					result = obj as IPartOfSpeech;
					topPOS = obj.ShortName;
					obj = obj.Owner;
				}
				return result;
			}

			private bool IsRTL()
			{
				return Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.RightToLeftScript;
			}

			private void HandleInsertAroundSlot(bool before, IMoInflAffixSlot chosenSlot, out int flid, out int ihvo)
			{
				ILcmReferenceSequence<IMoInflAffixSlot> seq;
				int index;
				flid = GetAffixSequenceContainingSlot(_clickedCmObject as IMoInflAffixSlot, out seq, out index);
				var iOffset = before ? 0 : 1;
				UndoableUnitOfWorkHelper.Do(AreaResources.ksUndoAddSlot, AreaResources.ksRedoAddSlot, Cache.ActionHandlerAccessor,
					() => seq.Insert(index + iOffset, chosenSlot));
				// The views system numbers visually, so adjust index for RTL vernacular writing system.
				ihvo = index + iOffset;
				if (IsRTL())
				{
					ihvo = seq.Count - 1 - ihvo;
				}
			}

			private void HandleInsertAroundStem(bool before, IMoInflAffixSlot chosenSlot, out int flid, out int ihvo)
			{
				if (before)
				{
					flid = MoInflAffixTemplateTags.kflidPrefixSlots;
					// The views system numbers visually, so adjust index for RTL vernacular writing system.
					ihvo = IsRTL() ? 0 : _template.PrefixSlotsRS.Count;
					UndoableUnitOfWorkHelper.Do(AreaResources.ksUndoAddSlot, AreaResources.ksRedoAddSlot, Cache.ActionHandlerAccessor,
						() => _template.PrefixSlotsRS.Add(chosenSlot));
				}
				else
				{
					flid = MoInflAffixTemplateTags.kflidSuffixSlots;
					// The views system numbers visually, so adjust index for RTL vernacular writing system.
					ihvo = IsRTL() ? _template.SuffixSlotsRS.Count : 0;
					UndoableUnitOfWorkHelper.Do(AreaResources.ksUndoAddSlot, AreaResources.ksRedoAddSlot, Cache.ActionHandlerAccessor,
						() => _template.SuffixSlotsRS.Insert(0, chosenSlot));
				}
			}

			private int GetAffixSequenceContainingSlot(IMoInflAffixSlot slot, out ILcmReferenceSequence<IMoInflAffixSlot> seq, out int index)
			{
				index = _template.PrefixSlotsRS.IndexOf(slot);
				if (index >= 0)
				{
					seq = _template.PrefixSlotsRS;
					return MoInflAffixTemplateTags.kflidPrefixSlots;
				}
				index = _template.SuffixSlotsRS.IndexOf(slot);
				if (index >= 0)
				{
					seq = _template.SuffixSlotsRS;
					return MoInflAffixTemplateTags.kflidSuffixSlots;
				}
				seq = null;
				return 0;
			}

			private void InflAffixTemplateHelp_Clicked(object sender, EventArgs e)
			{
				ShowHelp.ShowHelpTopic(PropertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider), ChooseInflectionalAffixHelpTopic);
			}

			/// <summary>
			/// Fix the name of any slot that is still "Type slot name here".
			/// </summary>
			private void FixSlotName(IMoInflAffixSlot slot)
			{
				if (slot.Name.AnalysisDefaultWritingSystem.Text == _newSlotName)
				{
					slot.Name.SetAnalysisDefaultWritingSystem(GetNextUnnamedSlotName());
				}
			}

			/// <summary>
			/// Return true if no slot name needs to be adjusted by FixSlotName.
			/// </summary>
			private bool AllSlotNamesOk => _template.PrefixSlotsRS.All(slot => slot.Name.AnalysisDefaultWritingSystem.Text != _newSlotName)
										   && _template.SuffixSlotsRS.All(slot => slot.Name.AnalysisDefaultWritingSystem.Text != _newSlotName);

			private string GetNextUnnamedSlotName()
			{
				// get count of how many unnamed slots there are in this pos and its parent
				// append that number plus one to the string table name
				var aiUnnamedSlotValues = GetAllUnnamedSlotValues();
				aiUnnamedSlotValues.Sort();
				var iMax = aiUnnamedSlotValues.Count;
				// default to the next one
				var iValueToUse = iMax + 1;
				// find any "holes" in the numbered sequence (in case the user has renamed
				//   one or more of them since the last time we did this)
				for (var i = 0; i < iMax; i++)
				{
					var iValue = i + 1;
					if (aiUnnamedSlotValues[i] != iValue)
					{
						// use the value in the "hole"
						iValueToUse = iValue;
						break;
					}
				}
				return _unnamedSlotName + iValueToUse;
			}

			private List<int> GetAllUnnamedSlotValues()
			{
				var aiUnnamedSlotValues = new List<int>();
				var pos = _template.OwnerOfClass<IPartOfSpeech>();
				while (pos != null)
				{
					foreach (var slot in pos.AffixSlotsOC)
					{
						if (slot.Name.AnalysisDefaultWritingSystem == null || slot.Name.BestAnalysisAlternative.Text == null || slot.Name.BestAnalysisAlternative.Text.StartsWith(_unnamedSlotName))
						{
							var sValue = _unnamedSlotName;
							int i;
							try
							{
								i = Convert.ToInt32(sValue);
							}
							catch (Exception)
							{
								// default to 9999 if what's after is not a number
								i = 9999; // use something very unlikely to happen normally
							}
							aiUnnamedSlotValues.Add(i);
						}
					}
					pos = pos.OwnerOfClass<IPartOfSpeech>();
				}
				return aiUnnamedSlotValues;
			}

			/// <summary>
			/// When focus is lost, stop filtering messages to catch characters
			/// </summary>
			protected override void OnLostFocus(EventArgs e)
			{
				// During deletion of a Grammar Category, Windows/.Net can pass through here after
				// the template has been deleted, resulting a crash trying to verify the slot names
				// of the template.  See LT-13932.
				if (_template.IsValidObject && !AllSlotNamesOk)
				{
					UndoableUnitOfWorkHelper.Do(AreaResources.ksUndoChangeSlotName, AreaResources.ksRedoChangeSlotName, Cache.ActionHandlerAccessor, () =>
					{
						foreach (var slot in _template.PrefixSlotsRS)
						{
							FixSlotName(slot);
						}
						foreach (var slot in _template.SuffixSlotsRS)
						{
							FixSlotName(slot);
						}
					});
				}
				base.OnLostFocus(e);
			}

			private ITsString MenuLabelForInflTemplateAddInflAffixMsa(string label)
			{
				switch (_clickedCmObject.ClassID)
				{
					case MoInflAffMsaTags.kClassId:
						return DetermineMsaContextMenuItemLabel(label);
					case MoInflAffixSlotTags.kClassId:
						_msaSlot = _clickedCmObject as IMoInflAffixSlot;
						return DoXXXReplace(label, TsSlotName(_msaSlot));
					default:
						return null;
				}
			}

			private ITsString DetermineSlotContextMenuItemLabel(string label)
			{
				switch (_clickedCmObject.ClassID)
				{
					case MoInflAffixSlotTags.kClassId:
						return DoXXXReplace(label, TsSlotName(_clickedCmObject as IMoInflAffixSlot));
					case MoInflAffixTemplateTags.kClassId:
						var tssStem = TsStringUtils.MakeString(_stem, Cache.DefaultUserWs);
						return DoXXXReplace(label, tssStem);
					default:
						return null;
				}
			}

			private ITsString MenuLabelForInflTemplateMoveSlot(string label, bool moveLeft, out bool enabled)
			{
				var tssLabel = DetermineSlotContextMenuItemLabel(label);
				if (_clickedCmObject.ClassID != MoInflAffixSlotTags.kClassId)
				{
					enabled = false;
				}
				else if (!SetEnabledIfFindSlotInSequence(_template.PrefixSlotsRS, out enabled, moveLeft))
				{
					SetEnabledIfFindSlotInSequence(_template.SuffixSlotsRS, out enabled, moveLeft);
				}
				return tssLabel;
			}

			private ITsString MenuLabelForInflTemplateAffixSlotOperation(string label, out bool enabled)
			{
				var tssLabel = DetermineSlotContextMenuItemLabel(label);
				enabled = _clickedCmObject.ClassID == MoInflAffixSlotTags.kClassId;
				return tssLabel;
			}

			private ITsString MenuLabelForInflTemplateRemoveInflAffixMsa(string label)
			{
				return _clickedCmObject.ClassID == MoInflAffMsaTags.kClassId ? DetermineMsaContextMenuItemLabel(label) : null;
			}

			private ITsString MenuLabelForJumpToTool(string label)
			{
				return _clickedCmObject.ClassID == MoInflAffMsaTags.kClassId ? TsStringUtils.MakeString(label, Cache.DefaultUserWs) : null;
			}

			private ITsString MenuLabelForInflAffixTemplateHelp(string label)
			{
				var helpTopic = PropertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider).GetHelpString(ChooseInflectionalAffixHelpTopic);
				if (string.IsNullOrWhiteSpace(helpTopic))
				{
					return null;
				}
				if (_clickedCmObject.ClassID != MoInflAffMsaTags.kClassId && _clickedCmObject.ClassID != MoInflAffixSlotTags.kClassId && _clickedCmObject.ClassID != MoInflAffixTemplateTags.kClassId)
				{
					return null;
				}
				// Display help only if there's a topic linked to the generated ID in the resource file.
				return TsStringUtils.MakeString(label, Cache.DefaultUserWs);
			}

			private bool SetEnabledIfFindSlotInSequence(ILcmReferenceSequence<IMoInflAffixSlot> slots, out bool enabled, bool isLeft)
			{
				var index = slots.IndexOf(_clickedCmObject as IMoInflAffixSlot);
				if (index >= 0)
				{   // it was found
					bool bAtEdge;
					if (isLeft)
					{
						bAtEdge = (index == 0);
					}
					else
					{
						bAtEdge = (index == slots.Count - 1);
					}
					if (bAtEdge || slots.Count == 1)
					{
						enabled = false;  // Cannot move it left when it's at the left edge or there's only one
					}
					else
					{
						enabled = true;
					}
					return true;
				}
				enabled = false;
				return false;
			}

			private ITsString DetermineMsaContextMenuItemLabel(string sLabel)
			{
				var msa = _clickedCmObject as IMoInflAffMsa;
				var tss = DoYYYReplace(sLabel, msa.ShortNameTSS);
				var tssSlotName = TsSlotNameOfMsa(msa);
				return DoXXXReplace(tss, tssSlotName);
			}

			private ITsString TsSlotNameOfMsa(IMoInflAffMsa msa)
			{
				IMoInflAffixSlot slot = null;
#if !MaybeSomeDayToTryAndGetRemoveMsaCorrectForCircumfixes
				if (msa.SlotsRC.Count > 0)
				{
					slot = msa.SlotsRC.First();
				}
				var tssResult = TsSlotName(slot);
				_msaSlot = slot;
#else
			slot = (LCM.Ling.MoInflAffixSlot)CmObject.CreateFromDBObject(this.Cache, m_hvoSlot);
			sResult = TsSlotName(slot);
#endif
				return tssResult;
			}

			private ITsString TsSlotName(IMoInflAffixSlot slot)
			{
				if (slot == null)
				{
					return TsStringUtils.MakeString(AreaResources.ksQuestions, Cache.DefaultUserWs);
				}
				if (slot.Name.AnalysisDefaultWritingSystem.Text == _newSlotName)
				{
					NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(Cache.ActionHandlerAccessor, () => slot.Name.SetAnalysisDefaultWritingSystem(GetNextUnnamedSlotName()));
				}
				return slot.Name.AnalysisDefaultWritingSystem;
			}

			private ITsString DoXXXReplace(string source, ITsString tssReplace)
			{
				return DoReplaceToken(source, tssReplace, "XXX");
			}

			private ITsString DoYYYReplace(string source, ITsString tssReplace)
			{
				return DoReplaceToken(source, tssReplace, "YYY");
			}

			private ITsString DoReplaceToken(string source, ITsString tssReplace, string token)
			{
				var tss = TsStringUtils.MakeString(source, Cache.DefaultUserWs);
				return DoReplaceToken(tss, tssReplace, token);
			}

			private static ITsString DoXXXReplace(ITsString tssSource, ITsString tssReplace)
			{
				return DoReplaceToken(tssSource, tssReplace, "XXX");
			}

			private static ITsString DoReplaceToken(ITsString tssSource, ITsString tssReplace, string token)
			{
				var tsb = tssSource.GetBldr();
				var ich = tsb.Text.IndexOf(token);
				while (ich >= 0)
				{
					if (ich > 0)
					{
						tsb.ReplaceTsString(ich, ich + token.Length, tssReplace);
					}
					if (ich + tssReplace.Length >= tsb.Length)
					{
						break;
					}
					ich = tsb.Text.IndexOf(token, ich + tssReplace.Length);
				}
				return tsb.GetString();
			}

			/// <summary>
			/// InflAffixTemplateMenuHandler provides context menus to the Inflectional Affix Template control.
			/// When the user (or test code) issues commands, this class also invokes the corresponding methods on the
			/// Inflectional Affix Template control.
			/// </summary>
			private sealed class InflAffixTemplateMenuHandler : IDisposable
			{
				private ComboListBox _comboListBox;
				private List<FwMenuItem> _fwMenuItems;
				private InflAffixTemplateControl _control;
				private readonly Dictionary<string, Tuple<EventHandler<EventArgs>, string>> _eventHandlers;
				private Form _mainFlexForm;
				private IPropertyTable _propertyTable;

				internal InflAffixTemplateMenuHandler(IPropertyTable propertyTable, InflAffixTemplateControl inflAffixTemplateControl, Dictionary<string, Tuple<EventHandler<EventArgs>, string>> eventHandlers, Form mainFlexForm)
				{
					_control = inflAffixTemplateControl;
					_eventHandlers = eventHandlers;
					_mainFlexForm = mainFlexForm;
					_propertyTable = propertyTable;
					// Note the = instead of += we do not want more than 1 handler trying to open the context menu!
					// you could try changing this if we wanted to have a fall back handler, and if there
					// was some way to get the first handler to be able to say "don't pass on this message"
					// when it handled the menu display itself.
					_control.ShowContextMenu = ShowSliceContextMenu;
				}

				#region IDisposable & Co. implementation

				private bool IsDisposed { get; set; }

				/// <summary>
				/// Finalizer, in case client doesn't dispose it.
				/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
				/// </summary>
				/// <remarks>
				/// In case some clients forget to dispose it directly.
				/// </remarks>
				~InflAffixTemplateMenuHandler()
				{
					Dispose(false);
					// The base class finalizer is called automatically.
				}

				/// <summary />
				public void Dispose()
				{
					Dispose(true);
					// This object will be cleaned up by the Dispose method.
					// Therefore, you should call GC.SuppressFinalize to
					// take this object off the finalization queue
					// and prevent finalization code for this object
					// from executing a second time.
					GC.SuppressFinalize(this);
				}

				/// <summary>
				/// Executes in two distinct scenarios.
				///
				/// 1. If disposing is true, the method has been called directly
				/// or indirectly by a user's code via the Dispose method.
				/// Both managed and unmanaged resources can be disposed.
				///
				/// 2. If disposing is false, the method has been called by the
				/// runtime from inside the finalizer and you should not reference (access)
				/// other managed objects, as they already have been garbage collected.
				/// Only unmanaged resources can be disposed.
				/// </summary>
				private void Dispose(bool disposing)
				{
					Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
					if (IsDisposed)
					{
						// No need to run it more than once.
						return;
					}

					if (disposing)
					{
						// Dispose managed resources here.
						_comboListBox?.Dispose();
					}
					_control = null;
					_propertyTable = null;
					_comboListBox = null;
					_fwMenuItems = null;
					_mainFlexForm = null;

					// Dispose unmanaged resources here, whether disposing is true or false.

					IsDisposed = true;
				}

				#endregion IDisposable & Co. implementation

				/// <summary>
				/// Invoked by a DataTree (which is in turn invoked by the slice)
				/// when the user does something to bring up a context menu
				/// </summary>
				private void ShowSliceContextMenu(object sender, EventArgs e)
				{
					_fwMenuItems = CreateListItems();
					if (!_fwMenuItems.Any())
					{
						return;
					}
					if (_comboListBox == null)
					{
						_comboListBox = new ComboListBox(_mainFlexForm)
						{
							// Since we may initialize with TsStrings, need to set WSF.
							WritingSystemFactory = _propertyTable.GetValue<LcmCache>(FwUtils.cache).LanguageWritingSystemFactoryAccessor,
							DropDownStyle = ComboBoxStyle.DropDownList,
							StyleSheet = FwUtils.StyleSheetFromPropertyTable(_propertyTable)
						};
					}
					else
					{
						_comboListBox.Items.Clear();
					}
					// Prevents direct editing.
					foreach (var menuItem in _fwMenuItems)
					{
						_comboListBox.Items.Add(menuItem.Label);
					}
					using (var g = _control.CreateGraphics())
					{
						var maxWidth = 0;
						var height = 0;
						var ie = _comboListBox.Items.GetEnumerator();
						while (ie.MoveNext())
						{
							string s = null;
							if (ie.Current is ITsString)
							{
								var tss = (ITsString)ie.Current;
								s = tss.Text;
							}
							else if (ie.Current is string)
							{
								s = (string)ie.Current;
							}
							if (s != null)
							{
								var szf = g.MeasureString(s, _comboListBox.Font);
								var nWidth = (int)szf.Width + 2;
								if (maxWidth < nWidth)
								{
									// 2 is not quite enough for height if you have homograph
									// subscripts.
									maxWidth = nWidth;
								}
								height += (int)szf.Height + 3;
							}
						}
						_comboListBox.Form.Width = Math.Max(_comboListBox.Form.Width, maxWidth);
						_comboListBox.Form.Height = Math.Max(_comboListBox.Form.Height, height);
					}
					_comboListBox.AdjustSize(500, 400); // these are maximums!
					_comboListBox.SelectedIndex = 0;
					// Set handlers after the initial selection to avoid triggering the handler
					_comboListBox.SelectedIndexChanged += HandleFwMenuSelection;
					_comboListBox.SameItemSelected += HandleFwMenuSelection;
					_comboListBox.Launch(new Rectangle(new Point(Cursor.Position.X, Cursor.Position.Y), new Size(10, 10)), Screen.GetWorkingArea(_control));
				}

				private void HandleFwMenuSelection(object sender, EventArgs e)
				{
					var selectedIndex = _comboListBox.SelectedIndex;
					if (selectedIndex == -1)
					{
						// Nothing selected, so bail out.
						return;
					}
					_comboListBox.HideForm();
					var selectedFwMenuItem = _fwMenuItems[selectedIndex];
					selectedFwMenuItem.Handler.Invoke(sender, e);
				}

				/// <summary>
				/// We may need to display vernacular data within the menu: if so, the standard menu display
				/// won't work, and we must do something else...  Actually the analysis and user writing systems
				/// (or fonts) may differ as well.
				/// </summary>
				private List<FwMenuItem> CreateListItems()
				{
					var fwMenuItems = new List<FwMenuItem>();
					foreach (var kvp in _eventHandlers)
					{
						ITsString tssLabel;
						var enabled = true;
						switch (kvp.Key)
						{
							case InflTemplateAddInflAffixMsa:
								tssLabel = _control.MenuLabelForInflTemplateAddInflAffixMsa(kvp.Value.Item2);
								break;
							case InflTemplateInsertSlotAfter:
							case InflTemplateInsertSlotBefore:
								tssLabel = _control.DetermineSlotContextMenuItemLabel(kvp.Value.Item2);
								break;
							case InflTemplateMoveSlotLeft:
								tssLabel = _control.MenuLabelForInflTemplateMoveSlot(kvp.Value.Item2, true, out enabled);
								break;
							case InflTemplateMoveSlotRight:
								tssLabel = _control.MenuLabelForInflTemplateMoveSlot(kvp.Value.Item2, false, out enabled);
								break;
							case InflTemplateToggleSlotOptionality:
							case InflTemplateRemoveSlot:
								tssLabel = _control.MenuLabelForInflTemplateAffixSlotOperation(kvp.Value.Item2, out enabled);
								break;
							case InflTemplateRemoveInflAffixMsa:
								tssLabel = _control.MenuLabelForInflTemplateRemoveInflAffixMsa(kvp.Value.Item2);
								break;
							case JumpToTool:
								tssLabel = _control.MenuLabelForJumpToTool(kvp.Value.Item2);
								break;
							case InflAffixTemplateHelp:
								tssLabel = _control.MenuLabelForInflAffixTemplateHelp(kvp.Value.Item2);
								break;
							default:
								throw new InvalidOperationException("Unrecognized message");
						}
						// If "tssLabel" is null or "enabled" is false, then don't bother creating it at all.
						if (tssLabel != null && enabled)
						{
							fwMenuItems.Add(new FwMenuItem(tssLabel, kvp.Value.Item1));
						}
					}
					return fwMenuItems;
				}

				/// <summary>
				/// This class stores the information needed for one menu item in a menu that must be displayed
				/// using views code (in order to handle multiple writing systems/fonts within each menu item).
				/// </summary>
				private sealed class FwMenuItem
				{
					internal FwMenuItem(ITsString tssItem, EventHandler<EventArgs> eventHandler)
					{
						Handler = eventHandler;
						Label = tssItem;
					}

					internal ITsString Label { get; }

					internal EventHandler<EventArgs> Handler { get; }
				}
			}
		}
	}
}