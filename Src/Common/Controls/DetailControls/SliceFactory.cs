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
using System.Diagnostics;
using System.IO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework.DetailControls.Resources;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.Controls;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// Summary description for SliceFactory.
	/// </summary>
	public class SliceFactory
	{
		public SliceFactory()
		{
			//
			// TODO: Add constructor logic here
			//
		}

		/// <summary>
		/// Look for a simple writing system spec as part of a node...currently either 'analysis' or 'vernacular'.
		/// If not found, answer 0.
		/// If found, answer the ID of the appropriate writing system, or throw exception if not valid.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="node"></param>
		/// <returns></returns>
		static int GetWs(Mediator mediator, FdoCache cache, XmlNode node)
		{
			string wsSpec = XmlUtils.GetOptionalAttributeValue(node, "ws");
			if (wsSpec != null)
			{
				int ws = 0;
				switch (wsSpec)
				{
					case "vernacular":
						ws = cache.LangProject.DefaultVernacularWritingSystem;
						break;
					case "analysis":
						ws = cache.LangProject.DefaultAnalysisWritingSystem;
						break;
					case "pronunciation":
						ws = cache.LangProject.DefaultPronunciationWritingSystem;
						break;
					case "reversal":
						int rih = int.Parse((string)mediator.PropertyTable.GetValue("ReversalIndexHvo"));
						if (rih > 0)
						{
							IReversalIndex ri = ReversalIndex.CreateFromDBObject(cache, rih);
							ws = ri.WritingSystemRAHvo;
						}
						else
							throw new ApplicationException("Couldn't find current reversal index.");
						break;
					default:
						throw new ApplicationException("ws must be 'vernacular', 'analysis', 'pronunciation',  or 'reversal';" + " it said '" + wsSpec + "'.");
				}
				return ws;
			}
			else
				return 0;
		}



		public static Slice Create(FdoCache cache, string editor, int flid, XmlNode node, ICmObject obj,
			StringTable stringTbl, IPersistenceProvider persistenceProvider, Mediator mediator, XmlNode caller)
		{
			Slice slice = null;
			switch(editor)
			{
				case "string":
				{
					if (flid == 0)
						throw new ApplicationException("field attribute required for basic properties " + node.OuterXml);
					int ws = GetWs(mediator, cache, node);
					if (ws != 0)
						slice = new StringSlice(obj.Hvo, flid, ws);
					else
						slice = new StringSlice(obj.Hvo, flid);
					break;
				}
				case "multistring":
				{
					if (flid == 0)
						throw new ApplicationException("field attribute required for multistring " + node.OuterXml);
					string wsSpec = XmlUtils.GetOptionalAttributeValue(node, "ws");
					int wsMagic;
					wsMagic = LangProject.GetMagicWsIdFromName(wsSpec);
					if (wsMagic == 0)
						throw new ApplicationException(
							"ws must be 'all vernacular', 'all analysis', 'analysis vernacular', or 'vernacular analysis'"
							+ " it said '" + wsSpec + "'.");

					bool forceIncludeEnglish = XmlUtils.GetOptionalBooleanAttributeValue(node, "forceIncludeEnglish", false);
					bool spellCheck = XmlUtils.GetOptionalBooleanAttributeValue(node, "spell", true);
					bool editable = XmlUtils.GetOptionalBooleanAttributeValue(caller, "editable", true);
					slice = new MultiStringSlice(obj.Hvo, flid, wsMagic, forceIncludeEnglish, editable, spellCheck);
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
					slice = new SummarySlice(obj, caller, node, stringTbl);
					break;
				}
				case "enumcombobox":
				{
					slice = new EnumComboSlice(cache, obj, flid, stringTbl, node["deParams"]);
					break;
				}
				case "referencecombobox":
				{
					slice = new ReferenceComboBoxSlice(cache, obj, flid, persistenceProvider, mediator);
					break;
				}
				case "typeaheadrefatomic":
				{
					slice = new AtomicRefTypeAheadSlice(obj.Hvo, flid);
					break;
				}
				case "msareferencecombobox":
				{
					slice = new MSAReferenceComboBoxSlice(cache, obj, flid, persistenceProvider, mediator);
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
					slice = new PictureSlice((FDO.Cellar.CmPicture) obj);
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

				case "morphtypeatomicreference":
				{
					slice = new MorphTypeAtomicReferenceSlice(cache, obj, flid, node, persistenceProvider, mediator, stringTbl);
					break;
				}

				case "atomicreferencepos":
				{
					slice = new AtomicReferencePOSSlice(cache, obj, flid, persistenceProvider, mediator);
					break;
				}

				case "defaultatomicreference":
				{
					slice = new AtomicReferenceSlice(cache, obj, flid, node, persistenceProvider, mediator, stringTbl);
					break;
				}

				case "derivmsareference":
				{
					slice = new DerivMSAReferenceSlice(cache, obj, flid, node, persistenceProvider, mediator, stringTbl);
					break;
				}

				case "inflmsareference":
				{
					slice = new InflMSAReferenceSlice(cache, obj, flid, node, persistenceProvider, mediator, stringTbl);
					break;
				}

				case "defaultvectorreference":
				{
					slice = new ReferenceVectorSlice(cache, obj, flid, node, persistenceProvider, mediator, stringTbl);
					break;
				}

				case "phoneenvreference":
				{
					slice = new PhoneEnvReferenceSlice(cache, obj, flid, node, persistenceProvider, mediator, stringTbl);
					break;
				}

				case "sttext":
				{
					slice = new StTextSlice(obj.Hvo, flid, GetWs(mediator, cache, node));
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
					slice = MakeAutoCustomSlice(cache, node, obj, caller);
					if (slice == null)
						return null;
					break;
				default:
				{
					//Since the editor has not been implemented yet,
					//is there a bitmap file that we can show for this editor?
					//Such bitmaps belong in the distFiles xde directory
					string fwCodeDir = DirectoryFinder.FWCodeDirectory;
					string editorBitmapRelativePath = @"xde\" + editor + ".bmp";
					if(System.IO.File.Exists(Path.Combine(fwCodeDir, editorBitmapRelativePath)))
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
		/// <param name="cache"></param>
		/// <param name="node"></param>
		/// <param name="obj"></param>
		/// <param name="caller"></param>
		/// <returns></returns>
		static Slice MakeAutoCustomSlice(FdoCache cache, XmlNode node, ICmObject obj, XmlNode caller)
		{
			IFwMetaDataCache mdc;
			mdc = cache.MetaDataCacheAccessor;
			int flid = GetCustomFieldFlid(caller, mdc, obj);
			if (flid == 0)
				return null;
			int ws = mdc.GetFieldWs((uint)flid);
			string label = mdc.GetFieldLabel((uint)flid);

			Slice slice = null;
			switch(ws)
			{
				case LangProject.kwsAnal:
					slice = new StringSlice(obj.Hvo, flid, cache.DefaultAnalWs);
					break;
				case LangProject.kwsVern:
					slice = new StringSlice(obj.Hvo, flid, cache.DefaultVernWs);
					break;
				case LangProject.kwsAnals:
				case LangProject.kwsVerns:
				case LangProject.kwsAnalVerns:
				case LangProject.kwsVernAnals:
					slice = new MultiStringSlice(obj.Hvo, flid, ws, false, true, true);
					break;
				default:
					throw new Exception("unhandled ws code in MakeAutoCustomSlice");
			}
			slice.Label = label;
			return slice;
		}

		static internal int GetCustomFieldFlid(XmlNode caller, IFwMetaDataCache mdc, ICmObject obj)
		{
			string fieldName = XmlUtils.GetManditoryAttributeValue(caller, "param");
			int flid = (int)mdc.GetFieldId2((uint)obj.ClassID, fieldName, true);
			return flid;
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
