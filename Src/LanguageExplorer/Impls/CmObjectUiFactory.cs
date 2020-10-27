// Copyright (c) 2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Controls;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.Reporting;

namespace LanguageExplorer.Impls
{
	[Export(typeof(ICmObjectUiFactory))]
	internal sealed class CmObjectUiFactory : ICmObjectUiFactory
	{
		[Import]
		private IPropertyTable _propertyTable;
		[Import]
		private IPublisher _publisher;
		[Import]
		private ISubscriber _subscriber;
		// Map from uint to uint, specifically, from clsid to clsid.
		// The key is any clsid that we have so far been asked to make a UI object for.
		// The value is the corresponding clsid that actually occurs in the switch.
		private static readonly Dictionary<int, int> Subclasses = new Dictionary<int, int>();

		#region ICmObjectUiFactory implementation

		/// <inheritdoc />
		ICmObjectUi ICmObjectUiFactory.MakeLcmModelUiObject(ICmObject cmObject)
		{
			Guard.AgainstNull(cmObject, nameof(cmObject));

			var retVal = MakeLcmModelUiObject(cmObject, cmObject.ClassID);
			retVal?.Initialize(new FlexComponentParameters(_propertyTable, _publisher, _subscriber));
			return retVal;
		}

		/// <inheritdoc />
		ICmObjectUi ICmObjectUiFactory.MakeLcmModelUiObject(ICmObject cmObject, int newObjectClassId, int flid, int insertionPosition)
		{
			Guard.AgainstNull(cmObject, nameof(cmObject));

			var flexComponentParameters = new FlexComponentParameters(_propertyTable, _publisher, _subscriber);
			CmObjectUi retVal;
			switch (newObjectClassId)
			{
				default:
					retVal = MakeLcmModelUiObject(cmObject, newObjectClassId, flid, insertionPosition);
					break;
				case CmPossibilityTags.kClassId:
					retVal = CmPossibilityUi.MakeLcmModelUiObject(this, cmObject);
					break;
				case PartOfSpeechTags.kClassId:
					retVal = PartOfSpeechUi.MakeLcmModelUiObject(cmObject, _propertyTable, _publisher);
					break;
				case FsFeatDefnTags.kClassId:
					retVal = FsFeatDefnUi.MakeLcmModelUiObject(cmObject, _propertyTable, _publisher, newObjectClassId);
					break;
				case LexSenseTags.kClassId:
					retVal = LexSenseUi.MakeLcmModelUiObject(cmObject, insertionPosition);
					break;
				case LexPronunciationTags.kClassId:
					retVal = LexPronunciationUi.MakeLcmModelUiObject(this, cmObject, newObjectClassId, flid, insertionPosition);
					break;
			}
			retVal?.Initialize(flexComponentParameters);
			return retVal;
		}

		#endregion ICmObjectUiFactory implementation

		private static CmObjectUi MakeLcmModelUiObject(ICmObject cmObject, int clsid)
		{
			var cache = cmObject.Cache;
			var mdc = cache.DomainDataByFlid.MetaDataCache;
			// If we've encountered an object with this Clsid before, and this clsid isn't in
			// the switch below, the dictionary will give us the appropriate clsid that IS in the
			// map, so the loop below will have only one iteration. Otherwise, we start the
			// search with the clsid of the object itself.
			if (!Subclasses.TryGetValue(clsid, out var realClsid))
			{
				realClsid = clsid;
			}
			// Each iteration investigates whether we have a CmObjectUi subclass that
			// corresponds to realClsid. If not, we move on to the base class of realClsid.
			// In this way, the CmObjectUi subclass we return is the one designed for the
			// closest base class of obj that has one.
			CmObjectUi result = null;
			while (result == null)
			{
				switch (realClsid)
				{
					// Todo: lots more useful cases.
					case WfiAnalysisTags.kClassId:
						result = new WfiAnalysisUi(cmObject);
						break;
					case PartOfSpeechTags.kClassId:
						result = new PartOfSpeechUi(cmObject);
						break;
					case CmPossibilityTags.kClassId:
						result = new CmPossibilityUi(cmObject);
						break;
					case LexPronunciationTags.kClassId:
						result = new LexPronunciationUi(cmObject);
						break;
					case LexSenseTags.kClassId:
						result = new LexSenseUi(cmObject);
						break;
					case MoAffixAllomorphTags.kClassId:
					case MoStemAllomorphTags.kClassId:
						result = new MoFormUi(cmObject);
						break;
					case ReversalIndexEntryTags.kClassId:
						result = new ReversalIndexEntryUi(cmObject);
						break;
					case WfiWordformTags.kClassId:
						result = new WfiWordformUi(cmObject);
						break;
					case WfiGlossTags.kClassId:
						result = new WfiGlossUi(cmObject);
						break;
					case CmObjectTags.kClassId:
					case LexEntryTags.kClassId:
					case MoInflAffMsaTags.kClassId:
					case MoDerivAffMsaTags.kClassId:
					case MoMorphSynAnalysisTags.kClassId:
					case MoStemMsaTags.kClassId:
						result = new CmObjectUi(cmObject);
						break;
					default:
						realClsid = mdc.GetBaseClsId(realClsid);
						break;
				}
			}
			if (realClsid != clsid)
			{
				Subclasses[clsid] = realClsid;
			}
			return result;
		}

		private static CmObjectUi MakeLcmModelUiObject(ICmObject cmObject, int classId, int flid, int insertionPosition)
		{
			var cache = cmObject.Cache;
			CmObjectUi newUiObj = null;
			UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoInsert, LanguageExplorerResources.ksRedoInsert, cache.ServiceLocator.GetInstance<IActionHandler>(), () =>
			{
				newUiObj = MakeLcmModelUiObject(cache.ServiceLocator.GetObject(cache.DomainDataByFlid.MakeNewObject(classId, cmObject.Hvo, flid, insertionPosition)), classId);
			});
			return newUiObj;
		}

		private class CmObjectUi : ICmObjectUi
		{
			private ICmObject _cmObject;
			private FlexComponentParameters _flexComponentParameters;
			protected readonly LcmCache _cache;
			protected ICmObjectUi AsICmObjectUi => this;

			#region Construction and initialization

			/// <summary>
			/// If you KNOW for SURE the right subclass of CmObjectUi, you can just make one
			/// directly. Most clients should use MakeLcmModelUiObject.
			/// </summary>
			internal CmObjectUi(ICmObject cmObject)
			{
				Guard.AgainstNull(cmObject, nameof(cmObject));

				_cmObject = cmObject;
				_cache = cmObject.Cache;
			}

			#endregion Construction and initialization

			#region ICmObjectUi implementation

			/// <summary>
			/// Retrieve the CmObject we are providing UI functions for.
			/// </summary>
			ICmObject ICmObjectUi.MyCmObject => _cmObject;

			/// <summary>
			/// Delete the object, after showing a confirmation dialog.
			/// Return true if deleted, false, if cancelled.
			/// </summary>
			bool ICmObjectUi.DeleteUnderlyingObject()
			{
				var cmo = GetCurrentCmObject();
				if (cmo != null && _cmObject != null && cmo.Hvo == _cmObject.Hvo)
				{
					_flexComponentParameters.Publisher.Publish(new PublisherParameterObject("DeleteRecord", this));
				}
				else
				{
					var mainWindow = _flexComponentParameters.PropertyTable.GetValue<Form>(FwUtilsConstants.window);
					using (new WaitCursor(mainWindow))
					{
						using (var dlg = new ConfirmDeleteObjectDlg(_flexComponentParameters.PropertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider)))
						{
							if (CanDelete(out var cannotDeleteMsg))
							{
								dlg.SetDlgInfo(AsICmObjectUi.MyCmObject, _cache, _flexComponentParameters.PropertyTable);
							}
							else
							{
								dlg.SetDlgInfo(AsICmObjectUi.MyCmObject, _cache, _flexComponentParameters.PropertyTable, TsStringUtils.MakeString(cannotDeleteMsg, _cache.DefaultUserWs));
							}
							if (DialogResult.Yes == dlg.ShowDialog(mainWindow))
							{
								ReallyDeleteUnderlyingObject();
								return true; // deleted it
							}
						}
					}
				}
				return false; // didn't delete it.
			}

			/// <summary>
			/// Merge the underling objects. This method handles the confirm dialog, then delegates
			/// the actual merge to ReallyMergeUnderlyingObject. If the flag is true, we merge
			/// strings and owned atomic objects; otherwise, we don't change any that aren't null
			/// to begin with.
			/// </summary>
			void ICmObjectUi.MergeUnderlyingObject(bool fLoseNoTextData)
			{
				var mainWindow = _flexComponentParameters.PropertyTable.GetValue<Form>(FwUtilsConstants.window);
				using (new WaitCursor(mainWindow))
				using (var dlg = new MergeObjectDlg(_flexComponentParameters.PropertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider)))
				{
					dlg.InitializeFlexComponent(_flexComponentParameters);
					var wp = new WindowParams();
					var mergeCandidates = new List<DummyCmObject>();
					var dObj = GetMergeinfo(wp, mergeCandidates, out var guiControlParameters, out var helpTopic);
					mergeCandidates.Sort();
					dlg.SetDlgInfo(_cache, wp, dObj, mergeCandidates, guiControlParameters, helpTopic);
					if (DialogResult.OK == dlg.ShowDialog(mainWindow))
					{
						ReallyMergeUnderlyingObject(dlg.Hvo, fLoseNoTextData);
					}
				}
			}

			/// <summary />
			public virtual void MoveUnderlyingObjectToCopyOfOwner()
			{
				MessageBox.Show(_flexComponentParameters.PropertyTable.GetValue<Form>(FwUtilsConstants.window), LanguageExplorerControls.ksCannotMoveObjectToCopy, LanguageExplorerControls.ksBUG);
			}

			#endregion ICmObjectUi implementation

			#region Jumping

			private ICmObject GetCurrentCmObject()
			{
				return _flexComponentParameters.PropertyTable.GetValue<ICmObject>(LanguageExplorerConstants.ActiveListSelectedObject, null);
			}

			#endregion

			#region Other methods
			protected virtual bool CanDelete(out string cannotDeleteMsg)
			{
				if (AsICmObjectUi.MyCmObject.CanDelete)
				{
					cannotDeleteMsg = null;
					return true;
				}
				cannotDeleteMsg = LanguageExplorerResources.ksCannotDeleteItem;
				return false;
			}

			/// <summary>
			/// Do any cleanup that involves interacting with the user, after the user has confirmed that our object should be
			/// deleted.
			/// </summary>
			protected virtual void DoRelatedCleanupForDeleteObject()
			{
				// For media and pictures: should we delete the file also?
				// arguably this should be on a subclass, but it's easier to share behavior for both here.
				ICmFile file = null;
				if (_cmObject is ICmPicture pict)
				{
					file = pict.PictureFileRA;
				}
				else if (_cmObject is ICmMedia media)
				{
					file = media.MediaFileRA;
				}
				else if (_cmObject != null)
				{
					// No cleanup needed
					return;
				}
				file.ConsiderDeletingRelatedFile(_flexComponentParameters.PropertyTable);
			}

			protected virtual void ReallyDeleteUnderlyingObject()
			{
				Logger.WriteEvent("Deleting '" + AsICmObjectUi.MyCmObject.ShortName + "'...");
				UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(LanguageExplorerControls.ksUndoDelete, LanguageExplorerControls.ksRedoDelete, _cache.ActionHandlerAccessor, () =>
				{
					DoRelatedCleanupForDeleteObject();
					AsICmObjectUi.MyCmObject.Cache.DomainDataByFlid.DeleteObj(AsICmObjectUi.MyCmObject.Hvo);
				});
				Logger.WriteEvent("Done Deleting.");
				_cmObject = null;
			}

			/// <summary>
			/// Merge the underling objects. This method handles the transaction, then delegates
			/// the actual merge to MergeObject. If the flag is true, we merge
			/// strings and owned atomic objects; otherwise, we don't change any that aren't null
			/// to begin with.
			/// </summary>
			protected virtual void ReallyMergeUnderlyingObject(int survivorHvo, bool fLoseNoTextData)
			{
				var survivor = _cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(survivorHvo);
				Logger.WriteEvent("Merging '" + AsICmObjectUi.MyCmObject.ShortName + "' into '" + survivor.ShortName + "'.");
				var ah = _cache.ServiceLocator.GetInstance<IActionHandler>();
				UndoableUnitOfWorkHelper.Do(LanguageExplorerControls.ksUndoMerge, LanguageExplorerControls.ksRedoMerge, ah, () => survivor.MergeObject(AsICmObjectUi.MyCmObject, fLoseNoTextData));
				Logger.WriteEvent("Done Merging.");
				_cmObject = null;
			}

			protected virtual DummyCmObject GetMergeinfo(WindowParams wp, List<DummyCmObject> mergeCandidates, out XElement guiControlParameters, out string helpTopic)
			{
				guiControlParameters = null;
				helpTopic = null;
				Debug.Assert(false, "Subclasses must override this method.");
				return null;
			}

			#endregion Other methods

			/// <summary />
			internal void Initialize(FlexComponentParameters flexComponentParameters)
			{
				Guard.AgainstNull(flexComponentParameters, nameof(flexComponentParameters));

				_flexComponentParameters = flexComponentParameters;
			}
		}

		/// <summary>
		/// FsFeatDefnUi provides UI-specific methods for the PartOfSpeech class.
		/// </summary>
		private sealed class FsFeatDefnUi : CmObjectUi
		{
			/// <summary>
			/// Create one. Argument must be a FsFeatDefn.
			/// Note that declaring it to be forces us to just do a cast in every case of MakeLcmModelUiObject, which is
			/// passed an obj anyway.
			/// </summary>
			private FsFeatDefnUi(ICmObject cmObject) : base(cmObject)
			{
				Require.That(cmObject is IFsFeatDefn);
			}

			/// <summary>
			/// Handle the context menu for inserting an FsFeatDefn.
			/// </summary>
			internal static FsFeatDefnUi MakeLcmModelUiObject(ICmObject cmObject, IPropertyTable propertyTable, IPublisher publisher, int classId)
			{
				FsFeatDefnUi ffdUi = null;
				var className = "FsClosedFeature";
				if (classId == FsComplexFeatureTags.kClassId)
				{
					className = "FsComplexFeature";
				}
				using (var dlg = new MasterInflectionFeatureListDlg(className))
				{
					dlg.SetDlginfo(cmObject.Cache.LanguageProject.MsFeatureSystemOA, propertyTable);
					switch (dlg.ShowDialog(propertyTable.GetValue<Form>(FwUtilsConstants.window)))
					{
						case DialogResult.OK: // Fall through.
						case DialogResult.Yes:
							ffdUi = new FsFeatDefnUi(dlg.SelectedFeatDefn);
							publisher.Publish(new PublisherParameterObject(LanguageExplorerConstants.JumpToRecord, dlg.SelectedFeatDefn.Hvo));
							break;
					}
				}
				return ffdUi;
			}
		}

		/// <summary>
		/// UI for LexPronunciation.
		/// </summary>
		private sealed class LexPronunciationUi : CmObjectUi
		{
			internal LexPronunciationUi(ICmObject cmObject)
				: base(cmObject)
			{
				Require.That(cmObject is ILexPronunciation);
			}

			/// <summary>
			/// Handle the context menu for inserting a LexPronunciation.
			/// </summary>
			internal static LexPronunciationUi MakeLcmModelUiObject(ICmObjectUiFactory factory, ICmObject cmObject, int classId, int flid, int insertionPosition)
			{
				var cache = cmObject.Cache;
				LexPronunciationUi result = null;
				UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoInsert, LanguageExplorerResources.ksRedoInsert, cache.ActionHandlerAccessor, () =>
				{
					result = (LexPronunciationUi)factory.MakeLcmModelUiObject(cache.ServiceLocator.GetObject(cache.DomainDataByFlid.MakeNewObject(classId, cmObject.Hvo, flid, insertionPosition)));
					// Forces them to be created (lest it try to happen while displaying the new object in PropChanged).
					var dummy = cache.LangProject.DefaultPronunciationWritingSystem;
				});
				return result;
			}
		}

		/// <summary>
		/// UI functions for MoMorphSynAnalysis.
		/// </summary>
		private sealed class LexSenseUi : CmObjectUi
		{
			/// <summary />
			internal LexSenseUi(ICmObject cmObject)
				: base(cmObject)
			{
				Require.That(cmObject is ILexSense);
			}

			protected override DummyCmObject GetMergeinfo(WindowParams wp, List<DummyCmObject> mergeCandidates, out XElement guiControlParameters, out string helpTopic)
			{
				wp.m_title = LanguageExplorerControls.ksMergeSense;
				wp.m_label = LanguageExplorerControls.ksSelectSense;
				var sense = (ILexSense)AsICmObjectUi.MyCmObject;
				var le = sense.Entry;
				// Exclude subsenses of the chosen sense.  See LT-6107.
				var rghvoExclude = new List<int>();
				foreach (var ls in sense.AllSenses)
				{
					rghvoExclude.Add(ls.Hvo);
				}
				foreach (var senseInner in le.AllSenses)
				{
					if (senseInner == AsICmObjectUi.MyCmObject || rghvoExclude.Contains(senseInner.Hvo))
					{
						continue;
					}
					// Make sure we get the actual WS used (best analysis would be the
					// descriptive term) for the ShortName.  See FWR-2812.
					var tssName = senseInner.ShortNameTSS;
					mergeCandidates.Add(new DummyCmObject(senseInner, tssName.Text, TsStringUtils.GetWsAtOffset(tssName, 0)));
				}
				guiControlParameters = XElement.Parse(LanguageExplorerControls.MergeSenseListParameters);
				helpTopic = "khtpMergeSense";
				var tss = AsICmObjectUi.MyCmObject.ShortNameTSS;
				return new DummyCmObject(AsICmObjectUi.MyCmObject, tss.Text, TsStringUtils.GetWsAtOffset(tss, 0));
			}

			public override void MoveUnderlyingObjectToCopyOfOwner()
			{
				var obj = AsICmObjectUi.MyCmObject.Owner;
				var clid = obj.ClassID;
				while (clid != LexEntryTags.kClassId)
				{
					obj = obj.Owner;
					clid = obj.ClassID;
				}
				var le = (ILexEntry)obj;
				le.MoveSenseToCopy((ILexSense)AsICmObjectUi.MyCmObject);
			}

			/// <summary>
			/// When inserting a LexSense, copy the MSA from the one we are inserting after, or the
			/// first one.  If this is the first one, we may need to create an MSA if the owning entry
			/// does not have an appropriate one.
			/// </summary>
			internal static LexSenseUi MakeLcmModelUiObject(ICmObject cmObject, int insertionPosition = int.MaxValue)
			{
				switch (cmObject)
				{
					case ILexEntry entry:
						return new LexSenseUi(entry.CreateNewLexSense(insertionPosition));
					case ILexSense sense:
						return new LexSenseUi(sense.CreateNewLexSense(insertionPosition));
					default:
						throw new ArgumentOutOfRangeException(nameof(cmObject), $"Owner must be an ILexEntry or an ILexSense, but it was: '{cmObject.ClassName}'.");
				}
			}
		}

		/// <summary>
		/// UI functions for MoMorphSynAnalysis.
		/// </summary>
		private sealed class MoFormUi : CmObjectUi
		{
			internal MoFormUi(ICmObject cmObject) :base(cmObject)
			{}

			protected override DummyCmObject GetMergeinfo(WindowParams wp, List<DummyCmObject> mergeCandidates, out XElement guiControlParameters, out string helpTopic)
			{
				wp.m_title = LanguageExplorerControls.ksMergeAllomorph;
				wp.m_label = LanguageExplorerControls.ksSelectAlternateForm;
				var defVernWs = _cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle;
				var le = (ILexEntry)AsICmObjectUi.MyCmObject.Owner;
				foreach (var allo in le.AlternateFormsOS)
				{
					if (allo.Hvo != AsICmObjectUi.MyCmObject.Hvo && allo.ClassID == AsICmObjectUi.MyCmObject.ClassID)
					{
						mergeCandidates.Add(new DummyCmObject(allo, allo.Form.VernacularDefaultWritingSystem.Text, defVernWs));
					}
				}
				if (le.LexemeFormOA.ClassID == AsICmObjectUi.MyCmObject.ClassID)
				{
					// Add the lexeme form.
					mergeCandidates.Add(new DummyCmObject(le.LexemeFormOA, le.LexemeFormOA.Form.VernacularDefaultWritingSystem.Text, defVernWs));
				}
				guiControlParameters = XElement.Parse(LanguageExplorerControls.MergeAllomorphListParameters);
				helpTopic = "khtpMergeAllomorph";
				return new DummyCmObject(AsICmObjectUi.MyCmObject, ((IMoForm)AsICmObjectUi.MyCmObject).Form.VernacularDefaultWritingSystem.Text, defVernWs);
			}
		}

		/// <summary>
		/// ReversalIndexEntryUi provides UI-specific methods for the ReversalIndexEntryUi class.
		/// </summary>
		private sealed class ReversalIndexEntryUi : CmObjectUi
		{
			/// <summary>
			/// Create one. Argument must be a PartOfSpeech.
			/// Note that declaring it to be forces us to just do a cast in every case of MakeLcmModelUiObject, which is
			/// passed an obj anyway.
			/// </summary>
			internal ReversalIndexEntryUi(ICmObject obj) : base(obj)
			{
				Debug.Assert(obj is IReversalIndexEntry);
			}

			protected override DummyCmObject GetMergeinfo(WindowParams wp, List<DummyCmObject> mergeCandidates, out XElement guiControlParameters, out string helpTopic)
			{
				wp.m_title = LanguageExplorerControls.ksMergeReversalEntry;
				wp.m_label = LanguageExplorerControls.ksEntries;
				var rie = (IReversalIndexEntry)AsICmObjectUi.MyCmObject;
				var filteredHvos = new HashSet<int>(rie.AllOwnedObjects.Select(obj => obj.Hvo)) { rie.Hvo }; // exclude `rie` and all of its subentries
				var wsIndex = _cache.ServiceLocator.WritingSystemManager.GetWsFromStr(rie.ReversalIndex.WritingSystem);
				mergeCandidates.AddRange(rie.ReversalIndex.AllEntries.Where(rieInner => !filteredHvos.Contains(rieInner.Hvo)).Select(rieInner => new DummyCmObject(rieInner, rieInner.ShortName, wsIndex)));
				guiControlParameters = XElement.Parse(LanguageExplorerControls.MergeReversalEntryListParameters);
				helpTopic = "khtpMergeReversalEntry";
				return new DummyCmObject(AsICmObjectUi.MyCmObject, rie.ShortName, wsIndex);
			}
		}

		/// <summary>
		/// UI functions for WfiAnalysis.
		/// </summary>
		private sealed class WfiAnalysisUi : CmObjectUi
		{
			internal WfiAnalysisUi(ICmObject cmObject)
				: base(cmObject)
			{
			}

			protected override void ReallyDeleteUnderlyingObject()
			{
				using (var helper = new UndoableUnitOfWorkHelper(_cache.ActionHandlerAccessor, LanguageExplorerControls.ksUndoDelete, LanguageExplorerControls.ksRedoDelete))
				{
					// we need to include resetting the wordform's checksum as part of the undo action for deleting this analysis.
					base.ReallyDeleteUnderlyingObject();
					// Make sure it gets parsed the next time.
					((IWfiWordform)AsICmObjectUi.MyCmObject.Owner).Checksum = 0;
					helper.RollBack = false;
				}
			}
		}

		/// <summary>
		/// UI functions for WfiGloss.
		/// </summary>
		private sealed class WfiGlossUi : CmObjectUi
		{
			internal WfiGlossUi(ICmObject cmObject)
				: base(cmObject)
			{
			}

			protected override DummyCmObject GetMergeinfo(WindowParams wp, List<DummyCmObject> mergeCandidates, out XElement guiControlParameters, out string helpTopic)
			{
				wp.m_title = LanguageExplorerControls.ksMergeWordGloss;
				wp.m_label = LanguageExplorerControls.ksSelectGloss;
				var anal = (IWfiAnalysis)AsICmObjectUi.MyCmObject.Owner;
				ITsString tss;
				int ws;
				foreach (var gloss in anal.MeaningsOC)
				{
					if (gloss.Hvo == AsICmObjectUi.MyCmObject.Hvo)
					{
						continue;
					}
					tss = gloss.ShortNameTSS;
					ws = tss.get_PropertiesAt(0).GetIntPropValues((int)FwTextPropType.ktptWs, out _);
					mergeCandidates.Add(new DummyCmObject(gloss, tss.Text, ws));
				}
				guiControlParameters = XElement.Parse(LanguageExplorerControls.MergeWordGlossListParameters);
				helpTopic = "khtpMergeWordGloss";
				var me = (IWfiGloss)AsICmObjectUi.MyCmObject;
				tss = me.ShortNameTSS;
				ws = tss.get_PropertiesAt(0).GetIntPropValues((int)FwTextPropType.ktptWs, out _);
				return new DummyCmObject(AsICmObjectUi.MyCmObject, tss.Text, ws);
			}
		}

		/// <summary>
		/// WfiWordformUi provides UI-specific methods for the WfiWordformUi class.
		/// </summary>
		private sealed class WfiWordformUi : CmObjectUi
		{
			internal WfiWordformUi(ICmObject cmObject)
				: base(cmObject)
			{
			}

			protected override bool CanDelete(out string cannotDeleteMsg)
			{
				if (base.CanDelete(out cannotDeleteMsg))
				{
					return true;
				}
				cannotDeleteMsg = LanguageExplorerControls.ksCannotDeleteWordform;
				return false;
			}
		}

		/// <summary>
		/// Special UI behaviors for the CmPossibility class.
		/// </summary>
		private class CmPossibilityUi : CmObjectUi
		{
			/// <summary>
			/// Create one. Argument must be a CmPossibility.
			/// Review JohnH (JohnT): should we declare the argument to be CmPossibility?
			/// Note that declaring it to be forces us to just do a cast in every case of MakeLcmModelUiObject, which is
			/// passed an obj anyway.
			/// </summary>
			protected CmPossibilityUi(ICmPossibility obj) : base(obj)
			{}

			internal CmPossibilityUi(ICmObject cmObject) : this((ICmPossibility)cmObject)
			{}


			internal static CmObjectUi MakeLcmModelUiObject(ICmObjectUiFactory factory, ICmObject cmObject)
			{
				Guard.AgainstNull(cmObject, nameof(cmObject));
				Require.That(cmObject is ICmPossibility);

				return CheckAndReportProblemAddingSubitem((ICmPossibility)cmObject) ? null : (CmObjectUi)factory.MakeLcmModelUiObject(cmObject);
			}

			/// <summary>
			/// Check whether it is OK to add a possibility to the specified item. If not, report the
			/// problem to the user and return true.
			/// </summary>
			private static bool CheckAndReportProblemAddingSubitem(ICmPossibility possItem)
			{
				var possItemMainPossibility = possItem.MainPossibility;
				var owningList = possItemMainPossibility.OwningList;
				// If we get here owningList is a possibility list and hvoRootItem is a top level item in that list
				// and possItem is, or is a subpossibility of, that top level item.
				// 1. Check to see if possItemMainPossibility is a chart template containing our target.
				// If so, owningList is owned in the chart templates property.
				if (CheckAndReportBadDiscourseTemplateAdd(possItem, possItemMainPossibility, owningList))
				{
					return true;
				}
				// 2. Check to see if hvoRootItem is a TextMarkup TagList containing our target (i.e. a Tag type).
				// If so, hvoPossList is owned in the text markup tags property.
				return CheckAndReportBadTagListAdd(possItem, possItemMainPossibility, owningList);
			}

			private static bool CheckAndReportBadDiscourseTemplateAdd(ICmPossibility possItem, ICmPossibility possItemMainPossibility, ICmPossibilityList owningList)
			{
				if (owningList.OwningFlid != DsDiscourseDataTags.kflidConstChartTempl)
				{
					return false; // some other list we don't care about.
				}
				// We can't turn a column into a group if it's in use.
				// If the item doesn't already have children, we can only add them if it isn't already in use
				// as a column: we don't want to change a column into a group. Thus, if there are no
				// children, we generally call the same routine as when deleting.
				// However, that routine has a special case to prevent deletion of the default template even
				// if NOT in use...and we must not prevent adding to that when it is empty! Indeed any
				// empty CHART can always be added to, so only if col's owner is a CmPossibility (it's not a root
				// item in the templates list) do we need to check for it being in use.
				if (possItem.SubPossibilitiesOS.Count == 0 && possItem.Owner is ICmPossibility && possItem.CheckAndReportProtectedChartColumn())
				{
					return true;
				}
				// Finally, we have to confirm the three-level rule.
				var owner = possItem.Owner;
				if (possItem.Hvo != possItemMainPossibility.Hvo && owner != null && owner.Hvo != possItemMainPossibility.Hvo)
				{
					MessageBox.Show(LanguageExplorerControls.ksTemplateTooDeep, LanguageExplorerControls.ksHierarchyLimit, MessageBoxButtons.OK, MessageBoxIcon.Warning);
					return true;
				}
				return false;
			}

			private static bool CheckAndReportBadTagListAdd(ICmPossibility possItem, ICmPossibility possItemMainPossibility, ICmPossibilityList owningList)
			{
				if (owningList.OwningFlid != LangProjectTags.kflidTextMarkupTags)
				{
					return false; // some other list we don't care about.
				}
				// Confirm the two-level rule.
				if (possItem.Hvo != possItemMainPossibility.Hvo)
				{
					MessageBox.Show(LanguageExplorerControls.ksMarkupTagsTooDeep, LanguageExplorerControls.ksHierarchyLimit, MessageBoxButtons.OK, MessageBoxIcon.Warning);
					return true;
				}
				return false;
			}

			protected override bool CanDelete(out string cannotDeleteMsg)
			{
				var poss = (ICmPossibility)AsICmObjectUi.MyCmObject;
				if (!poss.CanModifyChartColumn(out cannotDeleteMsg))
				{
					return false;
				}
				return CanDeleteTextMarkupTag(out cannotDeleteMsg) && base.CanDelete(out cannotDeleteMsg);
			}

			private bool CanDeleteTextMarkupTag(out string msg)
			{
				var poss = (ICmPossibility)AsICmObjectUi.MyCmObject;
				if (poss.IsOnlyTextMarkupTag)
				{
					msg = LanguageExplorerControls.ksCantDeleteLastTagList;
					return false;
				}
				var usedTag = poss.Services.GetInstance<ITextTagRepository>().GetByTextMarkupTag(poss).FirstOrDefault();
				if (usedTag != null)
				{
					string textName = null;
					if (usedTag.BeginSegmentRA != null)
					{
						var ws = usedTag.Cache.LangProject.DefaultWsForMagicWs(WritingSystemServices.kwsFirstAnalOrVern);
						var text = (IStText)usedTag.BeginSegmentRA.Owner.Owner;
						textName = text.Title.get_String(ws).Text;
						if (string.IsNullOrEmpty(textName))
						{
							textName = text.ShortName;
						}
					}
					msg = string.Format(poss.SubPossibilitiesOS.Count == 0 ? LanguageExplorerControls.ksCantDeleteMarkupTagInUse : LanguageExplorerControls.ksCantDeleteMarkupTypeInUse, textName);
					return false;
				}
				msg = null;
				return true;
			}
		}

		/// <summary>
		/// PartOfSpeechUi provides UI-specific methods for the PartOfSpeech class.
		/// </summary>
		private sealed class PartOfSpeechUi : CmPossibilityUi
		{
			/// <summary>
			/// Create one. Argument must be a PartOfSpeech.
			/// Note that declaring it to be forces us to just do a cast in every case of MakeLcmModelUiObject, which is
			/// passed an obj anyway.
			/// </summary>
			private PartOfSpeechUi(IPartOfSpeech obj) : base(obj)
			{
			}

			internal PartOfSpeechUi(ICmObject cmObject)
				: this((IPartOfSpeech)cmObject)
			{
			}

			/// <summary>
			/// Handle the context menu for inserting a POS.
			/// </summary>
			internal static CmObjectUi MakeLcmModelUiObject(ICmObject cmObject, IPropertyTable propertyTable, IPublisher publisher)
			{
				CmObjectUi posUi = null;
				using (var dlg = new MasterCategoryListDlg())
				{
					var newOwner = (IPartOfSpeech)cmObject;
					dlg.SetDlginfo(newOwner.OwningList, propertyTable, true, newOwner);
					switch (dlg.ShowDialog(propertyTable.GetValue<Form>(FwUtilsConstants.window)))
					{
						case DialogResult.OK: // Fall through.
						case DialogResult.Yes:
							posUi = new PartOfSpeechUi(dlg.SelectedPOS);
							publisher.Publish(new PublisherParameterObject(LanguageExplorerConstants.JumpToRecord, dlg.SelectedPOS.Hvo));
							break;
					}
				}
				return posUi;
			}
		}
	}
}