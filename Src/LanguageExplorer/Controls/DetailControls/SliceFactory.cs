// Copyright (c) 2003-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Xml.Linq;
using LanguageExplorer.Areas;
using LanguageExplorer.Areas.Grammar;
using LanguageExplorer.Areas.Grammar.Tools.AdhocCoprohibEdit;
using LanguageExplorer.Areas.Grammar.Tools.PhonemeEdit;
using LanguageExplorer.Areas.Grammar.Tools.PhonologicalFeaturesAdvancedEdit;
using LanguageExplorer.Areas.Grammar.Tools.PosEdit;
using LanguageExplorer.Areas.Lexicon;
using LanguageExplorer.Areas.Lexicon.Tools.Edit;
using LanguageExplorer.Areas.Lexicon.Tools.ReversalIndexes;
using LanguageExplorer.Areas.Lists.Tools.FeatureTypesAdvancedEdit;
using LanguageExplorer.Areas.Notebook;
using LanguageExplorer.Areas.Notebook.Tools.NotebookEdit;
using LanguageExplorer.Areas.TextsAndWords.Tools.Analyses;
using LanguageExplorer.Controls.DetailControls.Resources;
using LanguageExplorer.Controls.XMLViews;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.DomainServices;
using SIL.Xml;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary></summary>
	internal static class SliceFactory
	{
		private static int GetWs(LcmCache cache, IPropertyTable propertyTable, XElement node, string sAttr = "ws")
		{
			var wsSpec = XmlUtils.GetOptionalAttributeValue(node, sAttr);
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
					var riGuid = ReversalIndexServices.GetObjectGuidIfValid(propertyTable, "ReversalIndexGuid");
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

		/// <summary></summary>
		internal static Slice Create(LcmCache cache, string editor, int flid, XElement node, ICmObject obj, IPersistenceProvider persistenceProvider, FlexComponentParameters flexComponentParameters, XElement caller, ObjSeqHashMap reuseMap, ISharedEventHandlers sharedEventHandlers)
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
							throw new ApplicationException("field attribute required for multistring " + node.GetOuterXml());
						}
						var wsSpec = XmlUtils.GetOptionalAttributeValue(node, "ws");
						var wsMagic = WritingSystemServices.GetMagicWsIdFromName(wsSpec);
						if (wsMagic == 0)
						{
							throw new ApplicationException($"ws must be 'all vernacular', 'all analysis', 'analysis vernacular', or 'vernacular analysis': it said '{wsSpec}'.");
						}
						var forceIncludeEnglish = XmlUtils.GetOptionalBooleanAttributeValue(node, "forceIncludeEnglish", false);
						var spellCheck = XmlUtils.GetOptionalBooleanAttributeValue(node, "spell", true);
						// Either the part or the caller can specify that it isn't editable.
						// (The part may 'know' this, e.g. because it's a virtual attr not capable of editing;
						// more commonly the caller knows there isn't enough context for safe editing.
						var editable = XmlUtils.GetOptionalBooleanAttributeValue(caller, "editable", true) && XmlUtils.GetOptionalBooleanAttributeValue(node, "editable", true);
						var optionalWsSpec = XmlUtils.GetOptionalAttributeValue(node, "optionalWs");
						var wsMagicOptional = WritingSystemServices.GetMagicWsIdFromName(optionalWsSpec);
						// Create a new slice everytime for the MultiStringSlice - There are display glitches with height when reused
						newSlice = new MultiStringSlice(obj, flid, wsMagic, wsMagicOptional, forceIncludeEnglish, editable, spellCheck);
						break;
					}
				case "defaultvectorreference": // second most common.
					{
						var rvSlice = reuseMap.GetSliceToReuse("ReferenceVectorSlice") as ReferenceVectorSlice;
						if (rvSlice == null)
						{
							newSlice = new ReferenceVectorSlice(cache, obj, flid);
						}
						else
						{
							sliceWasRecyled = true;
							newSlice = rvSlice;
							rvSlice.Reuse(obj, flid);
						}
						break;
					}
				case "possvectorreference":
					{
						var prvSlice = reuseMap.GetSliceToReuse("PossibilityReferenceVectorSlice") as PossibilityReferenceVectorSlice;
						if (prvSlice == null)
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
						var prvSlice = reuseMap.GetSliceToReuse("SemanticDomainReferenceVectorSlice") as SemanticDomainReferenceVectorSlice;
						if (prvSlice == null)
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
							throw new ApplicationException("field attribute required for basic properties " + node.GetOuterXml());
						}
						var ws = GetWs(cache, flexComponentParameters.PropertyTable, node);
						newSlice = ws != 0 ? new StringSlice(obj, flid, ws) : new StringSlice(obj, flid);
						var fShowWsLabel = XmlUtils.GetOptionalBooleanAttributeValue(node, "labelws", false);
						if (fShowWsLabel)
						{
							((StringSlice)newSlice).ShowWsLabel = true;
						}
						var wsEmpty = GetWs(cache, flexComponentParameters.PropertyTable, node, "wsempty");
						if (wsEmpty != 0)
						{
							((StringSlice)newSlice).DefaultWs = wsEmpty;
						}
						break;
					}
				case "jtview":
					{
						var layout = XmlUtils.GetOptionalAttributeValue(caller, "param") ?? XmlUtils.GetMandatoryAttributeValue(node, "layout");
						// Editable if BOTH the caller (part ref) AND the node itself (the slice) say so...or at least if neither says not.
						var editable = XmlUtils.GetOptionalBooleanAttributeValue(caller, "editable", true) && XmlUtils.GetOptionalBooleanAttributeValue(node, "editable", true);
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
						newSlice = new EnumComboSlice(cache, obj, flid, node.Element("deParams"));
						break;
					}
				case "referencecombobox":
					{
						newSlice = new ReferenceComboBoxSlice(cache, obj, flid, persistenceProvider);
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
						var message = XmlUtils.GetMandatoryAttributeValue(node, "message");
						var sTranslate = XmlUtils.GetOptionalAttributeValue(node, "translate", "");
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
							newSlice = new ImageSlice(FwDirectoryFinder.CodeDirectory, XmlUtils.GetMandatoryAttributeValue(node, "param1"));
						}
						catch (Exception error)
						{
							newSlice = new LiteralMessageSlice(string.Format(DetailControlsStrings.ksImageSliceFailed, error.Message));
						}
						break;
					}
				case "checkbox":
					{
						newSlice = new CheckboxSlice(cache, obj, flid, node);
						break;
					}
				case "checkboxwithrefresh":
					{
						newSlice = new CheckboxRefreshSlice(cache, obj, flid, node);
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
						newSlice = new AutomicReferencePOSDisabledSlice(cache, obj, flid, flexComponentParameters);
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
						newSlice = new StTextSlice(sharedEventHandlers, obj, flid, GetWs(cache, flexComponentParameters.PropertyTable, node));
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
						newSlice = new InterlinearSlice(sharedEventHandlers);
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
						newSlice = new GhostReferenceVectorSlice(cache, obj, node);
						break;
					}
				case "command":
					{
						newSlice = new CommandSlice(node.Element("deParams"));
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
					newSlice = MakeAutoCustomSlice(sharedEventHandlers, cache, obj, caller, node);
					if (newSlice == null)
					{
						return null;
					}
					break;
				case "defaultvectorreferencedisabled": // second most common.
					{
						var rvSlice = reuseMap.GetSliceToReuse("ReferenceVectorDisabledSlice") as ReferenceVectorDisabledSlice;
						if (rvSlice == null)
						{
							newSlice = new ReferenceVectorDisabledSlice(cache, obj, flid);
						}
						else
						{
							sliceWasRecyled = true;
							newSlice = rvSlice;
							rvSlice.Reuse(obj, flid);
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
						var editorBitmapRelativePath = "xde/" + editor + ".bmp";
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

		/// <summary>
		/// This is invoked when a generated part ref (<part ref="Custom" param="fieldName"/>)
		/// invokes the standard slice (<slice editor="autoCustom" />). It comes up with the
		/// appropriate default slice for the custom field indicated in the param attribute of
		/// the caller.
		/// </summary>
		private static Slice MakeAutoCustomSlice(ISharedEventHandlers sharedEventHandlers, LcmCache cache, ICmObject obj, XElement caller, XElement configurationNode)
		{
			var mdc = cache.DomainDataByFlid.MetaDataCache;
			var flid = GetCustomFieldFlid(caller, mdc, obj);
			if (flid == 0)
			{
				return null;
			}
			Slice slice = null;
			var type = (CellarPropertyType)mdc.GetFieldType(flid);
			switch (type)
			{
				case CellarPropertyType.String:
				case CellarPropertyType.MultiUnicode:
				case CellarPropertyType.MultiString:
					var ws = mdc.GetFieldWs(flid);
					switch (ws)
					{
						case 0: // a desperate default.
						case WritingSystemServices.kwsAnal:
							slice = new StringSlice(obj, flid, cache.DefaultAnalWs);
							break;
						case WritingSystemServices.kwsVern:
							slice = new StringSlice(obj, flid, cache.DefaultVernWs);
							break;
						case WritingSystemServices.kwsAnals:
						case WritingSystemServices.kwsVerns:
						case WritingSystemServices.kwsAnalVerns:
						case WritingSystemServices.kwsVernAnals:
							slice = new MultiStringSlice(obj, flid, ws, 0, false, true, true);
							break;
						default:
							throw new Exception("unhandled ws code in MakeAutoCustomSlice");
					}
					break;
				case CellarPropertyType.Integer:
					slice = new IntegerSlice(cache, obj, flid);
					break;
				case CellarPropertyType.GenDate:
					slice = new GenDateSlice(cache, obj, flid);
					break;
				case CellarPropertyType.OwningAtomic:
					var dstClsid = mdc.GetDstClsId(flid);
					if (dstClsid == StTextTags.kClassId)
					{
						slice = new StTextSlice(sharedEventHandlers, obj, flid, cache.DefaultAnalWs);
					}
					break;
				case CellarPropertyType.ReferenceAtomic:
					slice = new AtomicReferenceSlice(cache, obj, flid);
					break;
				case CellarPropertyType.ReferenceCollection:
				case CellarPropertyType.ReferenceSequence:
					slice = new ReferenceVectorSlice(cache, obj, flid);
					SetConfigurationDisplayPropertyIfNeeded(configurationNode, obj, flid, cache.MainCacheAccessor, cache.LangProject.Services, cache.MetaDataCacheAccessor);
					break;
			}
			if (slice == null)
			{
				throw new Exception("unhandled field type in MakeAutoCustomSlice");
			}
			slice.Label = mdc.GetFieldLabel(flid);
			return slice;
		}

		/// <summary>
		/// Set configuration displayProperty from cmObjectCustomFieldFlid set elements' OwningList DisplayOption.
		///
		/// If cmObjectCustomFieldFlid refers to a set of elements (in cmObject), then examine the setting on the owning list of the
		/// elements to determine which property of each element to use when
		/// displaying each element in a slice, and record that information in configurationNode. This information is used
		/// in DetailControls.VectorReferenceVc.Display().
		/// Addresses LT-15705.
		/// </summary>
		internal static void SetConfigurationDisplayPropertyIfNeeded(XElement configurationNode, ICmObject cmObject, int cmObjectCustomFieldFlid, ISilDataAccess mainCacheAccessor, ILcmServiceLocator lcmServiceLocator, IFwMetaDataCache metadataCache)
		{
			var fieldType = metadataCache.GetFieldType(cmObjectCustomFieldFlid);
			if (!(fieldType == (int)CellarPropertyType.ReferenceCollection || fieldType == (int)CellarPropertyType.OwningCollection || fieldType == (int)CellarPropertyType.ReferenceSequence
			      || fieldType == (int)CellarPropertyType.OwningSequence))
			{
				return;
			}
			var element = FetchFirstElementFromSet(cmObject, cmObjectCustomFieldFlid, mainCacheAccessor, lcmServiceLocator);
			if (element == null)
			{
				return;
			}
			var displayOption = element.OwningList.DisplayOption;
			string propertyNameToGetAndShow = null;
			switch ((PossNameType)displayOption)
			{
				case PossNameType.kpntName:
					propertyNameToGetAndShow = "ShortNameTSS";
					break;
				case PossNameType.kpntNameAndAbbrev:
					propertyNameToGetAndShow = "AbbrAndNameTSS";
					break;
				case PossNameType.kpntAbbreviation:
					propertyNameToGetAndShow = "AbbrevHierarchyString";
					break;
				default:
					break;
			}
			if (propertyNameToGetAndShow == null)
			{
				return;
			}
			SetDisplayPropertyInXMLConfiguration(configurationNode, propertyNameToGetAndShow);
		}

		/// <summary>
		/// Edit or build XML in configurationNode to set displayProperty to displayPropertyValue.
		/// Just update an existing deParams node's displayProperty attribute if there is one already.
		/// </summary>
		private static void SetDisplayPropertyInXMLConfiguration(XElement configurationElement, string displayPropertyValue)
		{
			var displayPropertyAttribute = new XAttribute("displayProperty", displayPropertyValue);
			var deParamsElement = configurationElement.Element("deParams");
			if (deParamsElement == null)
			{
				configurationElement.Add(new XElement("deParams", displayPropertyAttribute));
				return;
			}
			if (deParamsElement.Attribute("displayProperty") == null)
			{
				deParamsElement.Add(displayPropertyAttribute);
				return;
			}
			deParamsElement.Attribute("displayProperty").SetValue(displayPropertyValue);
		}

		/// <summary>
		/// For a set of elements in cmObject that are referred to by setFlid, return the first element, or null.
		/// </summary>
		private static ICmPossibility FetchFirstElementFromSet(ICmObject cmObject, int setFlid, ISilDataAccess mainCacheAccessor, ILcmServiceLocator lcmServiceLocator)
		{
			var elementCount = mainCacheAccessor.get_VecSize(cmObject.Hvo, setFlid);
			if (elementCount == 0)
			{
				return null;
			}
			var firstElementHvo = mainCacheAccessor.get_VecItem(cmObject.Hvo, setFlid, 0);
			return lcmServiceLocator.GetObject(firstElementHvo) as ICmPossibility;
		}

		internal static int GetCustomFieldFlid(XElement caller, IFwMetaDataCache mdc, ICmObject obj)
		{
			var fieldName = XmlUtils.GetMandatoryAttributeValue(caller, "param");
			int flid;
			mdc.GetManagedMetaDataCache().TryGetFieldId(obj.ClassID, fieldName, out flid);
			return flid;
		}
	}
}