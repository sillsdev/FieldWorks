// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Xml.Linq;
using LanguageExplorer.Controls.XMLViews;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.DomainServices;
using SIL.Xml;

namespace LanguageExplorer.Controls.DetailControls.Slices
{
	/// <summary />
	[Export(typeof(ISliceFactory))]
	internal sealed class SliceFactory : ISliceFactory
	{
		private ISliceFactory AsISliceFactory => this;

		/// <summary></summary>
		ISlice ISliceFactory.Create(LcmCache cache, string editor, int flid, XElement configurationElement, ICmObject obj, IPersistenceProvider persistenceProvider, FlexComponentParameters flexComponentParameters, XElement caller, ObjSeqHashMap reuseMap, ISharedEventHandlers sharedEventHandlers)
		{
			Slice newSlice;
			var sliceWasRecyled = false;
			// Theoretically, 'editor' can be null, as that is one of the switch options, below.
			if (!string.IsNullOrWhiteSpace(editor))
			{
				editor = editor.ToLowerInvariant();
			}
			switch (editor)
			{
				case "multistring": // first, these are the most common slices.
					{
						if (flid == 0)
						{
							throw new ApplicationException("field attribute required for multistring " + configurationElement.GetOuterXml());
						}
						var wsSpec = XmlUtils.GetOptionalAttributeValue(configurationElement, "ws");
						var wsMagic = WritingSystemServices.GetMagicWsIdFromName(wsSpec);
						if (wsMagic == 0)
						{
							throw new ApplicationException($"ws must be 'all vernacular', 'all analysis', 'analysis vernacular', or 'vernacular analysis': it said '{wsSpec}'.");
						}
						var forceIncludeEnglish = XmlUtils.GetOptionalBooleanAttributeValue(configurationElement, "forceIncludeEnglish", false);
						var spellCheck = XmlUtils.GetOptionalBooleanAttributeValue(configurationElement, "spell", true);
						// Either the part or the caller can specify that it isn't editable.
						// (The part may 'know' this, e.g. because it's a virtual attr not capable of editing;
						// more commonly the caller knows there isn't enough context for safe editing.
						var editable = XmlUtils.GetOptionalBooleanAttributeValue(caller, "editable", true) && XmlUtils.GetOptionalBooleanAttributeValue(configurationElement, "editable", true);
						var optionalWsSpec = XmlUtils.GetOptionalAttributeValue(configurationElement, "optionalWs");
						var wsMagicOptional = WritingSystemServices.GetMagicWsIdFromName(optionalWsSpec);
						// Create a new slice everytime for the MultiStringSlice - There are display glitches with height when reused
						newSlice = new MultiStringSlice(obj, flid, wsMagic, wsMagicOptional, forceIncludeEnglish, editable, spellCheck);
						break;
					}
				case "defaultvectorreference": // second most common.
					{
						if (!(reuseMap.GetSliceToReuse("ReferenceVectorSlice") is ReferenceVectorSlice prvSlice))
						{
							newSlice = new ReferenceVectorSlice(cache, obj, flid);
						}
						else
						{
							sliceWasRecyled = true;
							newSlice = prvSlice;
							prvSlice.Reuse(obj, flid);
						}
						break;
					}
				case "possvectorreference":
					{
						if (!(reuseMap.GetSliceToReuse("PossibilityReferenceVectorSlice") is PossibilityReferenceVectorSlice prvSlice))
						{
							newSlice = new PossibilityReferenceVectorSlice(cache, obj, flid);
						}
						else
						{
							sliceWasRecyled = true;
							newSlice = prvSlice;
							prvSlice.Reuse(obj, flid);
						}
						break;
					}
				case "semdomvectorreference":
					{
						if (!(reuseMap.GetSliceToReuse("SemanticDomainReferenceVectorSlice") is SemanticDomainReferenceVectorSlice prvSlice))
						{
							newSlice = new SemanticDomainReferenceVectorSlice(cache, obj, flid);
						}
						else
						{
							sliceWasRecyled = true;
							newSlice = prvSlice;
							prvSlice.Reuse(obj, flid);
						}
						break;
					}
				case "string":
					{
						if (flid == 0)
						{
							throw new ApplicationException("field attribute required for basic properties " + configurationElement.GetOuterXml());
						}
						var ws = GetWs(flexComponentParameters.PropertyTable, configurationElement, cache);
						newSlice = ws != 0 ? new StringSlice(obj, flid, ws) : new StringSlice(obj, flid);
						var fShowWsLabel = XmlUtils.GetOptionalBooleanAttributeValue(configurationElement, "labelws", false);
						if (fShowWsLabel)
						{
							((StringSlice)newSlice).ShowWsLabel = true;
						}
						var wsEmpty = GetWs(flexComponentParameters.PropertyTable, configurationElement, cache, "wsempty");
						if (wsEmpty != 0)
						{
							((StringSlice)newSlice).DefaultWs = wsEmpty;
						}
						break;
					}
				case "jtview":
					{
						var layout = XmlUtils.GetOptionalAttributeValue(caller, "param") ?? XmlUtils.GetMandatoryAttributeValue(configurationElement, "layout");
						// Editable if BOTH the caller (part ref) AND the node itself (the slice) say so...or at least if neither says not.
						var editable = XmlUtils.GetOptionalBooleanAttributeValue(caller, "editable", true) && XmlUtils.GetOptionalBooleanAttributeValue(configurationElement, "editable", true);
						newSlice = new ViewSlice(new XmlView(obj.Hvo, layout, editable));
						break;
					}
				case "summary":
					{
						newSlice = new SummarySlice();
						break;
					}
				case "enumcombobox":
					{
						newSlice = new EnumComboSlice(cache, obj, flid, configurationElement.Element("deParams"));
						break;
					}
				case "referencecombobox":
					{
						newSlice = new ReferenceComboBoxSlice(cache, obj, flid);
						break;
					}
				case "typeaheadrefatomic":
					{
						newSlice = new AtomicRefTypeAheadSlice(obj, flid);
						break;
					}
				case "msareferencecombobox":
					{
						newSlice = new MSAReferenceComboBoxSlice(cache, obj, flid, persistenceProvider);
						break;
					}
				case "lit": // was "message"
					{
						var message = XmlUtils.GetMandatoryAttributeValue(configurationElement, "message");
						var sTranslate = XmlUtils.GetOptionalAttributeValue(configurationElement, "translate", "");
						if (sTranslate.Trim().ToLower() != "do not translate")
						{
							message = StringTable.Table.LocalizeLiteralValue(message);
						}
						newSlice = new LiteralMessageSlice(message);
						break;
					}
				case "picture":
					{
						newSlice = new PictureSlice((ICmPicture)obj);
						break;
					}
				case "image":
					{
						try
						{
							newSlice = new ImageSlice(FwDirectoryFinder.CodeDirectory, XmlUtils.GetMandatoryAttributeValue(configurationElement, "param1"));
						}
						catch (Exception error)
						{
							newSlice = new LiteralMessageSlice(string.Format(DetailControlsStrings.ksImageSliceFailed, error.Message));
						}
						break;
					}
				case "checkbox":
					{
						newSlice = new CheckboxSlice(cache, obj, flid, configurationElement);
						break;
					}
				case "checkboxwithrefresh":
					{
						newSlice = new CheckboxRefreshSlice(cache, obj, flid, configurationElement);
						break;
					}
				case "time":
					{
						newSlice = new DateSlice(cache, obj, flid);
						break;
					}
				case "integer": // produced in the auto-generated parts from the conceptual model
				case "int": // was "integer"
					{
						newSlice = new IntegerSlice(cache, obj, flid);
						break;
					}

				case "gendate":
					{
						newSlice = new GenDateSlice(cache, obj, flid);
						break;
					}

				case "morphtypeatomicreference":
					{
						newSlice = new MorphTypeAtomicReferenceSlice(cache, obj, flid);
						break;
					}

				case "atomicreferencepos":
					{
						newSlice = new AtomicReferencePOSSlice(cache, obj, flid, flexComponentParameters);
						break;
					}
				case "possatomicreference":
					{
						newSlice = new PossibilityAtomicReferenceSlice(cache, obj, flid);
						break;
					}
				case "atomicreferenceposdisabled":
					{
						newSlice = new AtomicReferencePOSDisabledSlice(cache, obj, flid, flexComponentParameters);
						break;
					}

				case "defaultatomicreference":
					{
						newSlice = new AtomicReferenceSlice(cache, obj, flid);
						break;
					}
				case "defaultatomicreferencedisabled":
					{
						newSlice = new AtomicReferenceDisabledSlice(cache, obj, flid);
						break;
					}
				case "derivmsareference":
					{
						newSlice = new DerivMSAReferenceSlice(cache, obj, flid);
						break;
					}
				case "inflmsareference":
					{
						newSlice = new InflMSAReferenceSlice(cache, obj, flid);
						break;
					}
				case "phoneenvreference":
					{
						newSlice = new PhoneEnvReferenceSlice(cache, obj, flid);
						break;
					}
				case "sttext":
					{
						newSlice = new StTextSlice(sharedEventHandlers, obj, flid, GetWs(flexComponentParameters.PropertyTable, configurationElement, cache));
						break;
					}
				case "featuresysteminflectionfeaturelistdlglauncher":
					{
						newSlice = new FeatureSystemInflectionFeatureListDlgLauncherSlice();
						break;
					}
				case "reventrysensescollectionreference":
					{
						newSlice = new RevEntrySensesCollectionReferenceSlice();
						break;
					}
				case "recordreferencevector":
					{
						newSlice = new RecordReferenceVectorSlice();
						break;
					}
				case "roledparticipants":
					{
						newSlice = new RoledParticipantsSlice();
						break;
					}
				case "phonologicalfeaturelistdlglauncher":
					{
						newSlice = new PhonologicalFeatureListDlgLauncherSlice();
						break;
					}
				case "msainflectionfeaturelistdlglauncher":
					{
						newSlice = new MsaInflectionFeatureListDlgLauncherSlice();
						break;
					}
				case "msadlglauncher":
					{
						newSlice = new MSADlgLauncherSlice();
						break;
					}
				case "lexreferencemulti":
					{
						newSlice = new LexReferenceMultiSlice();
						break;
					}
				case "entrysequencereference":
					{
						newSlice = new EntrySequenceReferenceSlice();
						break;
					}
				case "audiovisual":
					{
						newSlice = new AudioVisualSlice();
						break;
					}
				case "interlinear":
					{
						newSlice = new InterlinearSlice();
						break;
					}
				case "metaruleformula":
					{
						newSlice = new MetaRuleFormulaSlice(sharedEventHandlers);
						break;
					}
				case "regruleformula":
					{
						newSlice = new RegRuleFormulaSlice(sharedEventHandlers);
						break;
					}
				case "adhoccoprohibvectorreference":
					{
						newSlice = new AdhocCoProhibVectorReferenceSlice();
						break;
					}
				case "adhoccoorohibvectorreferencedisabled":
					{
						newSlice = new AdhocCoProhibVectorReferenceDisabledSlice();
						break;
					}
				case "adhoccoprohibatomicreference":
					{
						newSlice = new AdhocCoProhibAtomicReferenceSlice();
						break;
					}
				case "adhoccoprohibatomicreferencedisabled":
					{
						newSlice = new AdhocCoProhibAtomicReferenceDisabledSlice();
						break;
					}
				case "inflaffixtemplate":
					{
						newSlice = new InflAffixTemplateSlice();
						break;
					}
				case "affixruleformula":
					{
						newSlice = new AffixRuleFormulaSlice(sharedEventHandlers);
						break;
					}
				case "reversalindexentry":
					{
						newSlice = new ReversalIndexEntrySlice();
						break;
					}
				case "ghostlexref":
					{
						newSlice = new GhostLexRefSlice();
						break;
					}
				case "chorusmessage":
					{
						newSlice = new ChorusMessageSlice();
						break;
					}
				case "reversalindexentryform":
					{
						newSlice = new ReversalIndexEntryFormSlice(flid, obj);
						break;
					}
				case "phenvstrrepresentation":
					{
						newSlice = new PhEnvStrRepresentationSlice(obj, persistenceProvider, sharedEventHandlers);
						break;
					}
				case "lexreferencecollection":
					{
						newSlice = new LexReferenceCollectionSlice();
						break;
					}
				case "lexreferenceunidirectional":
					{
						newSlice = new LexReferenceUnidirectionalSlice();
						break;
					}
				case "lexreferencepair":
					{
						newSlice = new LexReferencePairSlice();
						break;
					}
				case "lexreferencetreebranches":
					{
						newSlice = new LexReferenceTreeBranchesSlice();
						break;
					}
				case "lexreferencetreeroot":
					{
						newSlice = new LexReferenceTreeRootSlice();
						break;
					}
				case "lexreferencesequence":
					{
						newSlice = new LexReferenceSequenceSlice();
						break;
					}
				case "ghostvector":
					{
						newSlice = new GhostReferenceVectorSlice(cache, obj, configurationElement);
						break;
					}
				case null:  //grouping nodes do not necessarily have any editor
					{
						newSlice = new Slice();
						break;
					}
				case "message":
					// case "integer": // added back in to behave as "int" above
					throw new Exception("use of obsolete editor type (message->lit, integer->int)");
				case "autocustom":
					newSlice = MakeAutoCustomSlice(cache, obj, caller, sharedEventHandlers, configurationElement);
					if (newSlice == null)
					{
						return null;
					}
					break;
				case "defaultvectorreferencedisabled": // second most common.
					{
						if (!(reuseMap.GetSliceToReuse("ReferenceVectorDisabledSlice") is ReferenceVectorDisabledSlice referenceVectorDisabledSlice))
						{
							newSlice = new ReferenceVectorDisabledSlice(cache, obj, flid);
						}
						else
						{
							sliceWasRecyled = true;
							newSlice = referenceVectorDisabledSlice;
							referenceVectorDisabledSlice.Reuse(obj, flid);
						}
						break;
					}
				case "basicipasymbol":
				{
					newSlice = new BasicIPASymbolSlice(obj, flid, cache.DefaultPronunciationWs);
					break;
				}
				default:
					{
						//Since the editor has not been implemented yet,
						//is there a bitmap file that we can show for this editor?
						//Such bitmaps belong in the distFiles xde directory
						var fwCodeDir = FwDirectoryFinder.CodeDirectory;
						var editorBitmapRelativePath = $"xde/{editor}.bmp";
						newSlice = File.Exists(Path.Combine(fwCodeDir, editorBitmapRelativePath))
							? (Slice)new ImageSlice(fwCodeDir, editorBitmapRelativePath)
							: new LiteralMessageSlice(string.Format(DetailControlsStrings.ksBadEditorType, editor));
						break;
					}
			}
			if (!sliceWasRecyled)
			{
				// Calling this a second time will throw.
				// So, only call it in slices that are new.
				newSlice.InitializeFlexComponent(flexComponentParameters);
			}
			newSlice.AccessibleName = editor;
			return newSlice;
		}

		void ISliceFactory.MakeGhostSlice(DataTree dataTree, LcmCache cache, FlexComponentParameters flexComponentParameters, ArrayList path, XElement node, ObjSeqHashMap reuseMap, ICmObject obj, ISlice parentSlice, int flidEmptyProp, XElement caller, int indent, ref int insertPosition)
		{
			if (parentSlice != null)
			{
				Debug.Assert(!parentSlice.IsDisposed, "AddSimpleNode parameter 'parentSlice' is Disposed!");
			}
			var slice = AsISliceFactory.GetMatchingSlice(path, reuseMap);
			if (slice == null)
			{
				slice = new GhostStringSlice(obj, flidEmptyProp, node, cache);
				slice.InitializeFlexComponent(flexComponentParameters);
				// Set the label and abbreviation (in that order...abbr defaults to label if not given.
				// Note that we don't have a "caller" here, so we pass 'node' as both arguments...
				// means it gets searched twice if not found, but that's fairly harmless.
				slice.Label = dataTree.GetLabel(node, node, obj, "ghostLabel");
				slice.Abbreviation = dataTree.GetLabelAbbr(node, node, obj, slice.Label, "ghostAbbr");
				// Install new item at appropriate position and level.
				slice.Indent = indent;
				slice.MyCmObject = obj;
				slice.Cache = cache;
				// We need a copy since we continue to modify path, so make it as compact as possible.
				slice.Key = path.ToArray();
				slice.ConfigurationNode = node;
				slice.CallerNode = caller;
				AsISliceFactory.SetNodeWeight(node, slice);
				slice.FinishInit();
				dataTree.InsertSlice(insertPosition, slice);
			}
			else
			{
				dataTree.EnsureValidIndexForReusedSlice(slice, insertPosition);
			}
			slice.ParentSlice = parentSlice;
			insertPosition++;
		}

		void ISliceFactory.SetNodeWeight(XElement node, ISlice slice)
		{
			var weightString = XmlUtils.GetOptionalAttributeValue(node, "weight", "field");
			ObjectWeight weight;
			switch (weightString)
			{
				case "heavy":
					weight = ObjectWeight.heavy;
					break;
				case "light":
					weight = ObjectWeight.light;
					break;
				case "normal":
					weight = ObjectWeight.normal;
					break;
				case "field":
					weight = ObjectWeight.field;
					break;
				default:
					throw new FwConfigurationException("Invalid 'weight' value, should be heavy, normal, light, or field");
			}
			slice.Weight = weight;
		}

		/// <summary>
		/// Look for a reusable slice that matches the current path. If found, remove from map and return;
		/// otherwise, return null.
		/// </summary>
		ISlice ISliceFactory.GetMatchingSlice(ArrayList path, ObjSeqHashMap reuseMap)
		{
			// Review JohnT(RandyR): I don't see how this can really work.
			// The original path (the key) used to set this does not, (and cannot) change,
			// but it is very common for slices to come and go, as they are inserted/deleted,
			// or when the Show hidden control is changed.
			// Those kinds of big changes will produce the input 'path' parm,
			// which has little hope of matching that fixed original key, won't it.
			// I can see how it would work when a simple F4 refresh is being done,
			// since the count of slices should remain the same.
			var list = reuseMap[path];
			if (list.Count <= 0)
			{
				return null;
			}
			var slice = (ISlice)list[0];
			reuseMap.Remove(path, slice);
			return slice;
		}

		ISlice ISliceFactory.CreateDummyObject(int indent, XElement node, ArrayList path, ICmObject obj, int flid, int cnt, string layoutOverride, string layoutChoiceField, XElement caller)
		{
			return new DummyObjectSlice(indent, node, path, obj, flid, cnt, layoutOverride, layoutChoiceField, caller);
		}

		private static Slice MakeAutoCustomSlice(LcmCache cache, ICmObject obj, XElement caller, ISharedEventHandlers sharedEventHandlers, XElement configurationElement)
		{
			var mdc = cache.DomainDataByFlid.MetaDataCache;
			if (!mdc.GetManagedMetaDataCache().TryGetFieldId(obj.ClassID, XmlUtils.GetMandatoryAttributeValue(caller, "param"), out var innerFlid))
			{
				return null;
			}

			Slice slice = null;
			var type = (CellarPropertyType)mdc.GetFieldType(innerFlid);
			switch (type)
			{
				case CellarPropertyType.String:
				case CellarPropertyType.MultiUnicode:
				case CellarPropertyType.MultiString:
					var ws = mdc.GetFieldWs(innerFlid);
					switch (ws)
					{
						case 0: // a desperate default.
						case WritingSystemServices.kwsAnal:
							slice = new StringSlice(obj, innerFlid, cache.DefaultAnalWs);
							break;
						case WritingSystemServices.kwsVern:
							slice = new StringSlice(obj, innerFlid, cache.DefaultVernWs);
							break;
						case WritingSystemServices.kwsAnals:
						case WritingSystemServices.kwsVerns:
						case WritingSystemServices.kwsAnalVerns:
						case WritingSystemServices.kwsVernAnals:
							slice = new MultiStringSlice(obj, innerFlid, ws, 0, false, true, true);
							break;
						default:
							throw new Exception("unhandled ws code in MakeAutoCustomSlice");
					}

					break;
				case CellarPropertyType.Integer:
					slice = new IntegerSlice(cache, obj, innerFlid);
					break;
				case CellarPropertyType.GenDate:
					slice = new GenDateSlice(cache, obj, innerFlid);
					break;
				case CellarPropertyType.OwningAtomic:
					var dstClsid = mdc.GetDstClsId(innerFlid);
					if (dstClsid == StTextTags.kClassId)
					{
						slice = new StTextSlice(sharedEventHandlers, obj, innerFlid, cache.DefaultAnalWs);
					}

					break;
				case CellarPropertyType.ReferenceAtomic:
					slice = new AtomicReferenceSlice(cache, obj, innerFlid);
					break;
				case CellarPropertyType.ReferenceCollection:
				case CellarPropertyType.ReferenceSequence:
					slice = new ReferenceVectorSlice(cache, obj, innerFlid);
					configurationElement.SetConfigurationDisplayPropertyIfNeeded(obj, innerFlid, cache.MainCacheAccessor, cache.LangProject.Services, cache.MetaDataCacheAccessor);
					break;
			}

			if (slice == null)
			{
				throw new Exception("unhandled field type in MakeAutoCustomSlice");
			}

			slice.Label = mdc.GetFieldLabel(innerFlid);
			return slice;
		}

		private static int GetWs(IPropertyTable propertyTable, XElement configurationElement, LcmCache cache, string sAttr = "ws")
		{
			var wsSpec = XmlUtils.GetOptionalAttributeValue(configurationElement, sAttr);
			if (wsSpec == null)
			{
				return 0;
			}

			var wsContainer = cache.ServiceLocator.WritingSystems;
			int ws;
			switch (wsSpec)
			{
				case "vernacular":
					ws = wsContainer.DefaultVernacularWritingSystem.Handle;
					break;
				case "analysis":
					ws = wsContainer.DefaultAnalysisWritingSystem.Handle;
					break;
				case "pronunciation":
					ws = wsContainer.DefaultPronunciationWritingSystem.Handle;
					break;
				case "reversal":
					var riGuid = FwUtils.GetObjectGuidIfValid(propertyTable, LanguageExplorerConstants.ReversalIndexGuid);
					if (!riGuid.Equals(Guid.Empty))
					{
						IReversalIndex ri;
						if (cache.ServiceLocator.GetInstance<IReversalIndexRepository>().TryGetObject(riGuid, out ri))
						{
							ws = cache.ServiceLocator.WritingSystemManager.GetWsFromStr(ri.WritingSystem);
						}
						else
						{
							throw new ApplicationException("Couldn't find current reversal index.");
						}
					}
					else
					{
						throw new ApplicationException("Couldn't find current reversal index.");
					}

					break;
				default:
					throw new ApplicationException($"ws must be 'vernacular', 'analysis', 'pronunciation',  or 'reversal': it said '{wsSpec}'.");
			}

			return ws;
		}
	}
}