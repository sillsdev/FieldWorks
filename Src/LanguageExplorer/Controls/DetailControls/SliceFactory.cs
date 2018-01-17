// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Xml.Linq;
using LanguageExplorer.Controls.DetailControls.Resources;
using LanguageExplorer.Controls.XMLViews;
using LanguageExplorer.LcmUi;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
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
					var riGuid = ReversalIndexEntryUi.GetObjectGuidIfValid(propertyTable, "ReversalIndexGuid");
					if (!riGuid.Equals(Guid.Empty))
					{
						try
						{
							var ri = cache.ServiceLocator.GetObject(riGuid) as IReversalIndex;
							ws = cache.ServiceLocator.WritingSystemManager.GetWsFromStr(ri.WritingSystem);
						}
						catch
						{
							throw new ApplicationException("Couldn't find current reversal index.");
						}
					}
					else
						throw new ApplicationException("Couldn't find current reversal index.");
					break;
				default:
					throw new ApplicationException($"ws must be 'vernacular', 'analysis', 'pronunciation',  or 'reversal': it said '{wsSpec}'.");
			}
			return ws;

		}

		/// <summary></summary>
		internal static Slice Create(LcmCache cache, string editor, int flid, XElement node, ICmObject obj, IPersistenceProvider persistenceProvider, FlexComponentParameters flexComponentParameters, XElement caller, ObjSeqHashMap reuseMap)
		{
			var sliceWasRecyled = false;
			Slice slice;
			switch(editor)
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
						var msSlice = reuseMap.GetSliceToReuse("MultiStringSlice") as MultiStringSlice;
						if (msSlice == null)
						{
							slice = new MultiStringSlice(obj, flid, wsMagic, wsMagicOptional, forceIncludeEnglish, editable, spellCheck);
						}
						else
						{
							sliceWasRecyled = true;
							slice = msSlice;
							msSlice.Reuse(obj, flid, wsMagic, wsMagicOptional, forceIncludeEnglish, editable, spellCheck);
						}
						break;
					}
				case "defaultvectorreference": // second most common.
					{
						var rvSlice = reuseMap.GetSliceToReuse("ReferenceVectorSlice") as ReferenceVectorSlice;
						if (rvSlice == null)
						{
							slice = new ReferenceVectorSlice(cache, obj, flid);
						}
						else
						{
							sliceWasRecyled = true;
							slice = rvSlice;
							rvSlice.Reuse(obj, flid);
						}
						break;
					}
				case "possvectorreference":
					{
						var prvSlice = reuseMap.GetSliceToReuse("PossibilityReferenceVectorSlice") as PossibilityReferenceVectorSlice;
						if (prvSlice == null)
						{
							slice = new PossibilityReferenceVectorSlice(cache, obj, flid);
						}
						else
						{
							sliceWasRecyled = true;
							slice = prvSlice;
							prvSlice.Reuse(obj, flid);
						}
						break;
					}
				case "semdomvectorreference":
					{
						var prvSlice = reuseMap.GetSliceToReuse("SemanticDomainReferenceVectorSlice") as SemanticDomainReferenceVectorSlice;
						if (prvSlice == null)
						{
							slice = new SemanticDomainReferenceVectorSlice(cache, obj, flid);
						}
						else
						{
							sliceWasRecyled = true;
							slice = prvSlice;
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
					slice = ws != 0 ? new StringSlice(obj, flid, ws) : new StringSlice(obj, flid);
					var fShowWsLabel = XmlUtils.GetOptionalBooleanAttributeValue(node, "labelws", false);
					if (fShowWsLabel)
					{
						(slice as StringSlice).ShowWsLabel = true;
					}
					var wsEmpty = GetWs(cache, flexComponentParameters.PropertyTable, node, "wsempty");
					if (wsEmpty != 0)
					{
						(slice as StringSlice).DefaultWs = wsEmpty;
					}
					break;
				}
				case "jtview":
				{
					var layout = XmlUtils.GetOptionalAttributeValue(caller, "param") ?? XmlUtils.GetMandatoryAttributeValue(node, "layout");
					// Editable if BOTH the caller (part ref) AND the node itself (the slice) say so...or at least if neither says not.
					var editable = XmlUtils.GetOptionalBooleanAttributeValue(caller, "editable", true) && XmlUtils.GetOptionalBooleanAttributeValue(node, "editable", true);
					slice = new ViewSlice(new XmlView(obj.Hvo, layout, editable));
					break;
				}
				case "summary":
				{
					slice = new SummarySlice();
					break;
				}
				case "enumcombobox":
				{
					slice = new EnumComboSlice(cache, obj, flid, node.Element("deParams"));
					break;
				}
				case "referencecombobox":
				{
					slice = new ReferenceComboBoxSlice(cache, obj, flid, persistenceProvider);
					break;
				}
				case "typeaheadrefatomic":
				{
					slice = new AtomicRefTypeAheadSlice(obj, flid);
					break;
				}
				case "msareferencecombobox":
				{
					slice = new MSAReferenceComboBoxSlice(cache, obj, flid, persistenceProvider);
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
					slice = new LiteralMessageSlice(message);
					break;
				}
				case "picture":
				{
					slice = new PictureSlice((ICmPicture)obj);
					break;
				}
				case "image":
				{
					try
					{
						slice = new ImageSlice(FwDirectoryFinder.CodeDirectory, XmlUtils.GetMandatoryAttributeValue(node, "param1"));
					}
					catch (Exception error)
					{
						slice = new LiteralMessageSlice(String.Format(DetailControlsStrings.ksImageSliceFailed, error.Message));
					}
					break;
				}
				case "checkbox":
				{
					slice = new CheckboxSlice(cache, obj, flid, node);
					break;
				}
				case "checkboxwithrefresh":
				{
					slice = new CheckboxRefreshSlice(cache, obj, flid, node);
					break;
				}
				case "time":
				{
					slice = new DateSlice(cache, obj, flid);
					break;
				}
				case "integer": // produced in the auto-generated parts from the conceptual model
				case "int": // was "integer"
				{
					slice = new IntegerSlice(cache, obj, flid);
					break;
				}

				case "gendate":
				{
					slice = new GenDateSlice(cache, obj, flid);
					break;
				}

				case "morphtypeatomicreference":
				{
					slice = new MorphTypeAtomicReferenceSlice(cache, obj, flid);
					break;
				}

				case "atomicreferencepos":
				{
					slice = new AtomicReferencePOSSlice(cache, obj, flid, flexComponentParameters.PropertyTable, flexComponentParameters.Publisher);
					break;
				}
				case "possatomicreference":
				{
					slice = new PossibilityAtomicReferenceSlice(cache, obj, flid);
					break;
				}
				case "atomicreferenceposdisabled":
				{
					slice = new AutomicReferencePOSDisabledSlice(cache, obj, flid, flexComponentParameters.PropertyTable, flexComponentParameters.Publisher);
					break;
				}

				case "defaultatomicreference":
				{
					slice = new AtomicReferenceSlice(cache, obj, flid);
					break;
				}
				case "defaultatomicreferencedisabled":
				{
					slice = new AtomicReferenceDisabledSlice(cache, obj, flid);
					break;
				}

				case "derivmsareference":
				{
					slice = new DerivMSAReferenceSlice(cache, obj, flid);
					break;
				}

				case "inflmsareference":
				{
					slice = new InflMSAReferenceSlice(cache, obj, flid);
					break;
				}

				case "phoneenvreference":
				{
					slice = new PhoneEnvReferenceSlice(cache, obj, flid);
					break;
				}

				case "sttext":
				{
					slice = new StTextSlice(obj, flid, GetWs(cache, flexComponentParameters.PropertyTable, node));
					break;
				}

				case "custom":
				{
					slice = (Slice)DynamicLoader.CreateObject(node);
					break;
				}

				case "customwithparams":
				{
					slice = (Slice)DynamicLoader.CreateObject(node, cache, editor, flid, node, obj, persistenceProvider, GetWs(cache, flexComponentParameters.PropertyTable, node));
					break;
				}
				case "ghostvector":
				{
					slice = new GhostReferenceVectorSlice(cache, obj, node);
					break;
				}

				case "command":
				{
					slice = new CommandSlice(node.Element("deParams"));
					break;
				}

				case null:	//grouping nodes do not necessarily have any editor
				{
					slice = new Slice();
					break;
				}
				case "message":
					// case "integer": // added back in to behave as "int" above
					throw new Exception("use of obsolete editor type (message->lit, integer->int)");
				case "autocustom":
					slice = MakeAutoCustomSlice(cache, obj, caller, node);
					if (slice == null)
					{
						return null;
					}
					break;
				case "defaultvectorreferencedisabled": // second most common.
					{
						var rvSlice = reuseMap.GetSliceToReuse("ReferenceVectorDisabledSlice") as ReferenceVectorDisabledSlice;
						if (rvSlice == null)
						{
							slice = new ReferenceVectorDisabledSlice(cache, obj, flid);
						}
						else
						{
							sliceWasRecyled = true;
							slice = rvSlice;
							rvSlice.Reuse(obj, flid);
						}
						break;
					}
				default:
				{
					//Since the editor has not been implemented yet,
					//is there a bitmap file that we can show for this editor?
					//Such bitmaps belong in the distFiles xde directory
					var fwCodeDir = FwDirectoryFinder.CodeDirectory;
					var editorBitmapRelativePath = "xde/" + editor + ".bmp";
					slice = File.Exists(Path.Combine(fwCodeDir, editorBitmapRelativePath))
						? (Slice) new ImageSlice(fwCodeDir, editorBitmapRelativePath)
						: new LiteralMessageSlice(String.Format(DetailControlsStrings.ksBadEditorType, editor));
					break;
				}
			}
			if (!sliceWasRecyled)
			{
				// Calling this a second time will throw.
				// So, only call it in slices that are new.
				slice.InitializeFlexComponent(flexComponentParameters);
			}
			slice.AccessibleName = editor;

			return slice;
		}

		/// <summary>
		/// This is invoked when a generated part ref (<part ref="Custom" param="fieldName"/>)
		/// invokes the standard slice (<slice editor="autoCustom" />). It comes up with the
		/// appropriate default slice for the custom field indicated in the param attribute of
		/// the caller.
		/// </summary>
		private static Slice MakeAutoCustomSlice(LcmCache cache, ICmObject obj, XElement caller, XElement configurationNode)
		{
			var mdc = cache.DomainDataByFlid.MetaDataCache;
			var flid = GetCustomFieldFlid(caller, mdc, obj);
			if (flid == 0)
			{
				return null;
			}
			Slice slice = null;
			var type = (CellarPropertyType) mdc.GetFieldType(flid);
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
					int dstClsid = mdc.GetDstClsId(flid);
					if (dstClsid == StTextTags.kClassId)
						slice = new StTextSlice(obj, flid, cache.DefaultAnalWs);
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

			if (!(fieldType == (int)CellarPropertyType.ReferenceCollection ||
				fieldType == (int)CellarPropertyType.OwningCollection ||
				fieldType == (int)CellarPropertyType.ReferenceSequence ||
				fieldType == (int)CellarPropertyType.OwningSequence))
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
		private static ICmPossibility FetchFirstElementFromSet(ICmObject cmObject, int setFlid, ISilDataAccess mainCacheAccessor, ILcmServiceLocator fdoServiceLocator)
		{
			var elementCount = mainCacheAccessor.get_VecSize(cmObject.Hvo, setFlid);
			if (elementCount == 0)
			{
				return null;
			}

			var firstElementHvo = mainCacheAccessor.get_VecItem(cmObject.Hvo, setFlid, 0);
			return fdoServiceLocator.GetObject(firstElementHvo) as ICmPossibility;
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