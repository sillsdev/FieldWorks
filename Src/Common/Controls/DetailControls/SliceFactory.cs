// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2003' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: SliceFactory.cs
// Responsibility: WordWorks
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Xml;
using System.IO;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework.DetailControls.Resources;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils;
using SIL.FieldWorks.Common.Controls;
using XCore;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary></summary>
	public static class SliceFactory
	{
		/// <summary>
		/// Look for a simple writing system spec as part of a node...currently either 'analysis' or 'vernacular'.
		/// If not found, answer 0.
		/// If found, answer the ID of the appropriate writing system, or throw exception if not valid.
		/// </summary>
		private static int GetWs(Mediator mediator, FdoCache cache, XmlNode node)
		{
			return GetWs(mediator, cache, node, "ws");
		}

		private static int GetWs(Mediator mediator, FdoCache cache, XmlNode node, string sAttr)
		{
			string wsSpec = XmlUtils.GetOptionalAttributeValue(node, sAttr);
			if (wsSpec != null)
			{
				IWritingSystemContainer wsContainer = cache.ServiceLocator.WritingSystems;
				int ws = 0;
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
						string sGuid = (string)mediator.PropertyTable.GetValue("ReversalIndexGuid", null);
						if (sGuid != null)
						{
							try
							{
								Guid riGuid = new Guid(sGuid);
								IReversalIndex ri = cache.ServiceLocator.GetObject(riGuid) as IReversalIndex;
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
						throw new ApplicationException("ws must be 'vernacular', 'analysis', 'pronunciation',  or 'reversal';" + " it said '" + wsSpec + "'.");
				}
				return ws;
			}

			return 0;
		}

		/// <summary></summary>
		public static Slice Create(FdoCache cache, string editor, int flid, XmlNode node, ICmObject obj,
			StringTable stringTbl, IPersistenceProvider persistenceProvider, Mediator mediator, XmlNode caller, ObjSeqHashMap reuseMap)
		{
			Slice slice;
			switch(editor)
			{
				case "multistring": // first, these are the most common slices.
					{
						if (flid == 0)
							throw new ApplicationException("field attribute required for multistring " + node.OuterXml);
						string wsSpec = XmlUtils.GetOptionalAttributeValue(node, "ws");
						int wsMagic = WritingSystemServices.GetMagicWsIdFromName(wsSpec);
						if (wsMagic == 0)
							throw new ApplicationException(
								"ws must be 'all vernacular', 'all analysis', 'analysis vernacular', or 'vernacular analysis'"
								+ " it said '" + wsSpec + "'.");


						bool forceIncludeEnglish = XmlUtils.GetOptionalBooleanAttributeValue(node, "forceIncludeEnglish", false);
						bool spellCheck = XmlUtils.GetOptionalBooleanAttributeValue(node, "spell", true);
						// Either the part or the caller can specify that it isn't editable.
						// (The part may 'know' this, e.g. because it's a virtual attr not capable of editing;
						// more commonly the caller knows there isn't enough context for safe editing.
						bool editable = XmlUtils.GetOptionalBooleanAttributeValue(caller, "editable", true)
							&& XmlUtils.GetOptionalBooleanAttributeValue(node, "editable", true);
						string optionalWsSpec = XmlUtils.GetOptionalAttributeValue(node, "optionalWs");
						int wsMagicOptional = WritingSystemServices.GetMagicWsIdFromName(optionalWsSpec);
						MultiStringSlice msSlice = reuseMap.GetSliceToReuse("MultiStringSlice") as MultiStringSlice;
						if (msSlice == null)
							slice = new MultiStringSlice(obj, flid, wsMagic, wsMagicOptional, forceIncludeEnglish, editable, spellCheck);
						else
						{
							slice = msSlice;
							msSlice.Reuse(obj, flid, wsMagic, wsMagicOptional, forceIncludeEnglish, editable, spellCheck);
						}
						break;
					}
				case "defaultvectorreference": // second most common.
					{
						var rvSlice = reuseMap.GetSliceToReuse("ReferenceVectorSlice") as ReferenceVectorSlice;
						if (rvSlice == null)
							slice = new ReferenceVectorSlice(cache, obj, flid);
						else
						{
							slice = rvSlice;
							rvSlice.Reuse(obj, flid);
						}
						break;
					}
				case "possvectorreference":
					{
						var prvSlice = reuseMap.GetSliceToReuse("PossibilityReferenceVectorSlice") as PossibilityReferenceVectorSlice;
						if (prvSlice == null)
							slice = new PossibilityReferenceVectorSlice(cache, obj, flid);
						else
						{
							slice = prvSlice;
							prvSlice.Reuse(obj, flid);
						}
						break;
					}
				case "string":
				{
					if (flid == 0)
						throw new ApplicationException("field attribute required for basic properties " + node.OuterXml);
					int ws = GetWs(mediator, cache, node);
					if (ws != 0)
						slice = new StringSlice(obj, flid, ws);
					else
						slice = new StringSlice(obj, flid);
					var fShowWsLabel = XmlUtils.GetOptionalBooleanAttributeValue(node, "labelws", false);
					if (fShowWsLabel)
						(slice as StringSlice).ShowWsLabel = true;
					int wsEmpty = GetWs(mediator, cache, node, "wsempty");
					if (wsEmpty != 0)
						(slice as StringSlice).DefaultWs = wsEmpty;
					break;
				}
				case "jtview":
				{
					string layout = XmlUtils.GetOptionalAttributeValue(caller, "param");
					if (layout == null)
						layout = XmlUtils.GetManditoryAttributeValue(node, "layout");
					// Editable if BOTH the caller (part ref) AND the node itself (the slice) say so...or at least if neither says not.
					bool editable = XmlUtils.GetOptionalBooleanAttributeValue(caller, "editable", true)
						&& XmlUtils.GetOptionalBooleanAttributeValue(node, "editable", true);
					slice = new ViewSlice(new XmlView(obj.Hvo, layout, stringTbl, editable));
					break;
				}
				case "summary":
				{
					slice = new SummarySlice();
					break;
				}
				case "enumcombobox":
				{
					slice = new EnumComboSlice(cache, obj, flid, stringTbl, node["deParams"]);
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
					string message = XmlUtils.GetManditoryAttributeValue(node, "message");
					if (stringTbl != null)
					{
						string sTranslate = XmlUtils.GetOptionalAttributeValue(node, "translate", "");
						if (sTranslate.Trim().ToLower() != "do not translate")
							message = stringTbl.LocalizeLiteralValue(message);
					}
					slice = new MessageSlice(message);
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
						slice = new ImageSlice(DirectoryFinder.FWCodeDirectory, XmlUtils.GetManditoryAttributeValue(node, "param1"));
					}
					catch (Exception error)
					{
						slice = new MessageSlice(String.Format(DetailControlsStrings.ksImageSliceFailed,
							error.Message));
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
					slice = new AtomicReferencePOSSlice(cache, obj, flid, persistenceProvider, mediator);
					break;
				}
				case "possatomicreference":
				{
					slice = new PossibilityAtomicReferenceSlice(cache, obj, flid);
					break;
				}
				case "atomicreferenceposdisabled":
				{
					slice = new AutomicReferencePOSDisabledSlice(cache, obj, flid, persistenceProvider, mediator);
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
					slice = new StTextSlice(obj, flid, GetWs(mediator, cache, node));
					break;
				}

				case "custom":
				{
					slice = (Slice)DynamicLoader.CreateObject(node);
					break;
				}

				case "customwithparams":
				{
					slice = (Slice)DynamicLoader.CreateObject(node,
						new object[]{cache, editor, flid, node, obj, stringTbl,
										persistenceProvider, GetWs(mediator, cache, node)});
					break;
				}
				case "ghostvector":
				{
					slice = new GhostReferenceVectorSlice(cache, obj, node);
					break;
				}

				case "command":
				{
					slice = new CommandSlice(node["deParams"]);
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
					slice = MakeAutoCustomSlice(cache, obj, caller);
					if (slice == null)
						return null;
					break;
				case "defaultvectorreferencedisabled": // second most common.
					{
						ReferenceVectorDisabledSlice rvSlice = reuseMap.GetSliceToReuse("ReferenceVectorDisabledSlice") as ReferenceVectorDisabledSlice;
						if (rvSlice == null)
							slice = new ReferenceVectorDisabledSlice(cache, obj, flid);
						else
						{
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
					string fwCodeDir = DirectoryFinder.FWCodeDirectory;
					string editorBitmapRelativePath = "xde/" + editor + ".bmp";
					if(File.Exists(Path.Combine(fwCodeDir, editorBitmapRelativePath)))
						slice = new ImageSlice(fwCodeDir, editorBitmapRelativePath);
					else
						slice = new MessageSlice(String.Format(DetailControlsStrings.ksBadEditorType, editor));
					break;
				}
			}
			slice.AccessibleName = editor;

			return slice;
		}

		/// <summary>
		/// This is invoked when a generated part ref (<part ref="Custom" param="fieldName"/>)
		/// invokes the standard slice (<slice editor="autoCustom".../>). It comes up with the
		/// appropriate default slice for the custom field indicated in the param attribute of
		/// the caller.
		/// </summary>
		static Slice MakeAutoCustomSlice(FdoCache cache, ICmObject obj, XmlNode caller)
		{
			IFwMetaDataCache mdc = cache.DomainDataByFlid.MetaDataCache;
			int flid = GetCustomFieldFlid(caller, mdc, obj);
			if (flid == 0)
				return null;
			Slice slice = null;
			var type = (CellarPropertyType) mdc.GetFieldType(flid);
			switch (type)
			{
				case CellarPropertyType.String:
				case CellarPropertyType.MultiUnicode:
				case CellarPropertyType.MultiString:
					int ws = mdc.GetFieldWs(flid);
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
					break;
			}
			if (slice == null)
				throw new Exception("unhandled field type in MakeAutoCustomSlice");
			slice.Label = mdc.GetFieldLabel(flid);
			return slice;
		}

		static internal int GetCustomFieldFlid(XmlNode caller, IFwMetaDataCache mdc, ICmObject obj)
		{
			string fieldName = XmlUtils.GetManditoryAttributeValue(caller, "param");
			// It would be nice to avoid all the possible throws for invalid fields, but hard
			// to achieve in a static method.
			try
			{
				int flid = mdc.GetFieldId2(obj.ClassID, fieldName, true);
				return flid;
			}
			catch
			{
				return 0;
			}
		}
	}

	/// <summary>
	/// The three 'weights' of objects for detail views:
	/// HeavyWeight objects get a thick rule above them;
	/// Normal objects don't.
	/// (Is there a distinction for lightweight? They're supposed to be almost non-detectable.)
	/// (Since this is an indication of whether an object starts at the top of a field, another
	/// case is that it's just a field.)
	/// (This is not fully utliized or implemented yet. Only the heavy option is distinguished
	/// from normal to produce the heavy rule.)
	/// </summary>
	public enum ObjectWeight
	{
		heavy,
		normal,
		light,
		field
	}
}
