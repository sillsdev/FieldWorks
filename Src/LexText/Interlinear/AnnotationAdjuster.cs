using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.XWorks;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// This class is used to manage changes to annotations resulting from edits,
	/// receiving relevant propChange notifications and also being informed directly
	/// when a key is pressed.
	/// (EricP) This is currently too sensitive, as it does not allow for edits at the beginning
	/// of a sentence or word, for example, inserting "new. " at the beginning of the sentence goes
	/// through the following stages:
	/// 1) "old sentence"
	/// 2) "nold sentence"		// AdjustTextAnnotationsForEdit currently removes the twfic for "old"
	/// 3) "neold sentence"
	/// 4) "new old sentence"
	/// 5) "new. old sentence"  // (the goal in view). AdjustTextAnnotationsForEdit will move the free translation of "old sentence" to "new. "
	/// (JohnT: is the above comment out of date? Moved here from AnnotatedTextEditingHelper, which used to do some of this work.)
	/// </summary>
	public class AnnotationAdjuster : IVwNotifyChange, IFWDisposable
	{
		int m_hvoAnnDefnTwfic = 0;
		int m_hvoSegDefn = 0;
		int m_hvoPunctDefn = 0;
		int m_segDefn_note = 0;
		int m_hvoAnnErrorDefn = 0;

		internal EditingHelper m_parentEditingHelper;
		FdoCache m_cache;

		int m_hvoStText = 0;
		TextSelInfo m_tsiOrig = null;
		int[] m_cbaHvosAffectedByEdit = null;
		int m_cchAdjust = 0;
		int m_hvoParaSelEnd = 0;
		int m_ichSelEnd = -1;
		int m_ihvoParaPrev = -1;
		List<int> m_parasWeHaveAdjusted = new List<int>();

		// Amount we need to add to items in m_annsToMoveFromDeletedParagraph if they survive.
		internal int m_cchAdjustAnnsFromDeletedParagraph = 0;
		internal int m_hvoWhereToMoveAnnsFromDeletedPara;
		internal Set<int> m_annsToMoveFromDeletedParagraph = new Set<int>();
		// As far as possible we want to put annotations we know we will delete into this collection and deal with them
		// all at once. This is much more efficient than doing them one at a time.
		internal List<int> m_annotationsToDelete = new List<int>();
		private Set<int> m_movedAnnsFromAfterInsertBreak = new Set<int>();
		internal Set<int> m_cbasToDel = new Set<int>();

		private int m_BtWs;

		private const int kflidParagraphs = (int) StText.StTextTags.kflidParagraphs;
		private const int kflidContents = (int) StTxtPara.StTxtParaTags.kflidContents;
		private RestoreSegmentsUndoAction m_parseOnUndoAction;

		public AnnotationAdjuster(FdoCache cache, EditingHelper parent)
		{
			m_cache = cache;
			m_parentEditingHelper = parent;
			m_segDefn_note = cache.GetIdFromGuid(LangProject.kguidAnnNote);
			m_hvoSegDefn = cache.GetIdFromGuid(LangProject.kguidAnnTextSegment);
			if (m_segDefn_note == 0 || m_hvoSegDefn == 0)
			{
				m_segDefn_note = 0; // used to subsequently test for in-memory test disabling things.
				return; // In-memory testing, can't do anything useful.
			}
			m_cache.MainCacheAccessor.AddNotification(this);
			m_hvoAnnDefnTwfic = CmAnnotationDefn.Twfic(cache).Hvo;
			m_hvoSegDefn = CmAnnotationDefn.TextSegment(cache).Hvo;
			m_hvoPunctDefn = CmAnnotationDefn.Punctuation(cache).Hvo;
			m_hvoAnnErrorDefn = CmAnnotationDefn.Errors(cache).Hvo;

			m_segDefn_note = cache.GetIdFromGuid(LangProject.kguidAnnNote);
		}

		internal TextSelInfo TextSelInfoBeforeEdit
		{
			get
			{
				// later
				//return m_parentEditingHelper.Callbacks.TextSelInfoBeforeEdit;
				return (m_parentEditingHelper.TextSelInfoBeforeEdit);
			}
		}

		/// <summary>
		/// If non-zero, indicates this adjuster is being used on a view that also displays the free translation.
		/// The adjuster must make sure that PropChanged notifications are sent when the list of segments for
		/// a paragraph changes, and also that segments are real objects and have free translations (unless they are labels).
		/// </summary>
		public int BtWs
		{
			get { return m_BtWs; }
			set { m_BtWs = value; }
		}

		private TextStateInfo m_anchorTextInfo;
		private TextStateInfo m_endpointTextInfo;
		private bool m_fSegSeqChanged; // Set true if something changes the sequence of segment objects on a paragraph.

		/// <summary>
		/// Saves some extra information for use in OnFinishedEdit. Make sure to call that, unless setting up
		/// a DataUpdateMonitor which will do so.
		/// </summary>
		public void OnAboutToEdit()
		{
			m_fSegSeqChanged = false;
			if (m_segDefn_note == 0)
				return; // in-memory testing.
			TextSelInfo info = m_parentEditingHelper.TextSelInfoBeforeEdit;
			m_anchorTextInfo = TextStateInfo.Create(info, false, m_cache, 0);
			if (m_anchorTextInfo != null)
				m_endpointTextInfo = TextStateInfo.Create(info, true, m_cache, m_anchorTextInfo.HvoText);
			else
				m_endpointTextInfo = null;
			CommonInit();
		}

		/// <summary>
		/// Set up TextStateInfo as we would for a selection entirely in the specified object,
		/// if it is a paragraph. Make sure to call OnFinishedEdit.
		/// </summary>
		public void OnAboutToModify(FdoCache cache, int hvoObj)
		{
			if (m_segDefn_note == 0)
				return; // in-memory testing.
			m_endpointTextInfo = null;
			m_anchorTextInfo = TextStateInfo.Create(cache, hvoObj);
			CommonInit();
		}

		void CommonInit()
		{
			if (m_cache.ActionHandlerAccessor == null)
				return;
			m_parseOnUndoAction = RestoreSegmentsUndoAction.CreateForUndo(m_cache, m_anchorTextInfo, m_endpointTextInfo);
			m_cache.ActionHandlerAccessor.AddAction(m_parseOnUndoAction);
		}

		/// <summary>
		/// triggered in Disposing DataUpdateMonitor, or called directly by anything that calls OnAboutToModify
		/// or OnAboutToEdit directly when not using a DUM.
		/// </summary>
		public void OnFinishedEdit()
		{
			if (m_segDefn_note == 0)
				return; // in-memory testing.
			// in case these get called out of the preferred order, make sure we move annotations to a new
			// paragraph before we reparse the text here.
			EndKeyPressed();
			if (m_cbasToDel.Count > 0)
			{
				// delete annotations that will no longer be used.
				DeleteCalculatedAnnotations(m_cbasToDel);
				m_cbasToDel.Clear();
			}
			m_annsToMoveFromDeletedParagraph.Clear();
			m_movedAnnsFromAfterInsertBreak.Clear();
			m_annotationsToDelete.Clear();

			bool fDidTextChange = false;
			if (m_anchorTextInfo != null && m_BtWs != 0)
			{
				fDidTextChange = m_anchorTextInfo.HandleMajorTextChange(m_BtWs);
				if (m_endpointTextInfo != null)
					fDidTextChange |= m_endpointTextInfo.HandleMajorTextChange(m_BtWs);
				if (!fDidTextChange && m_fSegSeqChanged)
					m_anchorTextInfo.HandleMajorParaChange(m_BtWs);
			}
			// If things changed this drastically we'd better refresh (see e.g. TE-7884, TE-7904)
			if (m_cache.ActionHandlerAccessor != null && m_parseOnUndoAction != null && m_parseOnUndoAction.RecordDoneState())
				m_cache.ActionHandlerAccessor.AddAction(RestoreSegmentsUndoAction.CreateForRedo(m_parseOnUndoAction));
			m_anchorTextInfo = null; // disable monitoring changes until we record a new relevant start state.
		}

		#region IVwNotifyChange Members

		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			// we expect undo/redo to make its own changes to the database or virtual properties, when needed.
			if (m_cache.ActionHandlerAccessor == null || m_cache.ActionHandlerAccessor.IsUndoOrRedoInProgress)
				return;
			if (tag == (int)StText.StTextTags.kflidParagraphs && cvDel > 0)
			{
				// TODO: if we've deleted a paragraph, we may be in the process of merging its contents back into
				// a remaining paragraph. So, get the annotation ids for the deleted paragraph, just in case we need to
				// reuse them.

				// This is always called just after deleting the individual paragraphs, but only once, unlike
				// AboutToDelete which is called for each one.
				CmBaseAnnotation.CollectLinkedItemsForDeletionFor(m_cache, m_annotationsToDelete,
					m_cbasToDel, true);
				MoveCbasToNewPara(m_cache,
					m_annsToMoveFromDeletedParagraph.ToArray(),
					m_hvoWhereToMoveAnnsFromDeletedPara,
					m_cchAdjustAnnsFromDeletedParagraph);
				return;
			}

			// we're interested in changing annotations if any edits were made to paragraph contents.
			if (tag == (int)StTxtPara.StTxtParaTags.kflidContents)
			{
				HandleContentChange(hvo, ivMin, cvIns, cvDel);
				return;
			}

			// how do we handle adjusting the deletion of a paragraph break, since by the time
			// we get the PropChange, the real annotations will have their BeginObject changed to the
			// new paragraph, so their offsets will be wrong.
		}

		internal virtual void HandleContentChange(int hvo, int ivMin, int cvIns, int cvDel)
		{
			AdjustTextAnnotationsForEdit(hvo, ivMin, cvIns, cvDel);
		}

		#endregion

		#region IFWDisposable Members

		bool m_fDisposed = false;

		public void CheckDisposed()
		{
			if (m_fDisposed)
				throw new ObjectDisposedException(ToString(),
					"This object is being used after it has been disposed: this is an Error.");
		}

		public bool IsDisposed
		{
			get { return m_fDisposed; }
		}

		protected virtual void Dispose(bool disposing)
		{
			if (IsDisposed)
				return;
			if (disposing)
			{
				if (m_cache != null && !m_cache.IsDisposed && m_cache.MainCacheAccessor != null)
					m_cache.MainCacheAccessor.RemoveNotification(this);
			}
			m_fDisposed = true;
		}


		#endregion

		#region IDisposable Members

		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		#endregion

		#region Enter Key handling

		/// <summary>
		/// If the key is Enter, and the selection is in the contents of an StTxtPara, save information we will need
		/// to adjust things after the keypress executes.
		/// </summary>
		public void StartKeyPressed(KeyPressEventArgs e, Keys modifiers)
		{
			if (m_segDefn_note == 0) //|| !m_parentEditingHelper.Editable
				return; // in-memory testing or we can't edit the view anyways

			m_tsiOrig = new TextSelInfo(m_parentEditingHelper.EditedRootBox);
			// Enhance JohnT: maybe there is something useful we could do if the other end of the selection isn't in the
			// same text, but it's not enough to just get the objects after the end; that produces a crash.
			// A temporary patch is to not attempt this adjustment at all in this case. See TE-8416.
			// This means segmented BT is lost on the trailing part of the paragraph when Enter is used
			// to replace a range, also some precision is lost on scripture notes on that segment. See TE-8419.
			// It should be fixed in the re-architecture, where the delete is handled as a separate UOW.
			if (e.KeyChar == '\r'  && (modifiers & Keys.Shift) == 0 && m_tsiOrig.Selection != null &&
				m_tsiOrig.Tag(true) == kflidContents && m_tsiOrig.Tag(false) == kflidContents &&
				m_cache.GetOwnerOfObject(m_tsiOrig.Hvo(false)) == m_cache.GetOwnerOfObject(m_tsiOrig.Hvo(true)))
			{
				// collect annotations that come after the selection, so we can move those into the new paragraph.
				if (m_tsiOrig.Selection.EndBeforeAnchor)
				{
					// The 'end' of the selection (the boundary after which stuff will be moved)
					// is really the anchor.
					m_hvoParaSelEnd = m_tsiOrig.Hvo(false);
					m_ichSelEnd = m_tsiOrig.IchAnchor;
					m_hvoStText = m_tsiOrig.ContainingObject(1, false);
				}
				else
				{
					m_hvoParaSelEnd = m_tsiOrig.Hvo(true);
					m_ichSelEnd = m_tsiOrig.IchEnd;
					m_hvoStText = m_tsiOrig.ContainingObject(1, true);
				}
				m_ihvoParaPrev = m_cache.GetObjIndex(m_hvoStText, kflidParagraphs, m_hvoParaSelEnd);
				string sql = String.Format("select cba.BeginObject, cba.Id, cba.AnnotationType, cba.BeginOffset, cba.EndOffset from CmBaseAnnotation_ cba " +
					"where cba.BeginObject = {0} and " + "cba.BeginOffset >= {1} " +
					"order by cba.BeginOffset, cba.EndOffset", new object[] { m_hvoParaSelEnd, m_ichSelEnd });
				m_cbaHvosAffectedByEdit = GetCbasPotentiallyAffectedByTextEdit(m_cache, m_hvoParaSelEnd, sql);
				// we will need to subtract the difference from the selection's end.
				m_cchAdjust = -m_ichSelEnd;
				m_parasWeHaveAdjusted.Add(m_hvoParaSelEnd);
			}
			else
			{
				m_hvoStText = 0; // this functionality does not apply.
			}
			if (m_cbaHvosAffectedByEdit != null)
				m_movedAnnsFromAfterInsertBreak.AddRange(m_cbaHvosAffectedByEdit);

		}

		/// <summary>
		/// After calling the base OnKeyPressed, call this to adjust the annotations.
		/// NB: currently may be called directly and also from OnFinishedEdit.
		/// This is because the scope of the DataUpdateManager that calls OnAboutToEdit and
		/// OnFinishedEditing is within the code that calls StartKeyressed and EndKeyPressed,
		/// and the EndKeyPressed MUST do its thing before OnFinishedEditing.
		/// </summary>
		public void EndKeyPressed()
		{
			if (m_cbaHvosAffectedByEdit != null && m_cbaHvosAffectedByEdit.Length > 0)
			{
				// the new paragraph should be one after the old one.
				// move annotations to new paragraph.
				TextSelInfo tsiAfterInsertedNewPara = new TextSelInfo(m_parentEditingHelper.EditedRootBox);
				// the cursor should be at the beginning of the new paragraph
				Debug.Assert(tsiAfterInsertedNewPara.IchAnchor == 0, "cursor should be at beginning of new paragraph");
				int hvoParaNew = tsiAfterInsertedNewPara.HvoAnchor;
				int ihvoParaNew = m_cache.GetObjIndex(m_hvoStText, kflidParagraphs, hvoParaNew);
				// make sure we're inserting the cbas into the expected new paragraph.
				if (ihvoParaNew != m_ihvoParaPrev + 1)
				{
					hvoParaNew = 0;
					throw new ArgumentException("new paragraph should be one after previous location of end of cursor selection");
				}
				if (hvoParaNew > 0)
				{
					MoveCbasToNewPara(m_cache, m_cbaHvosAffectedByEdit, hvoParaNew, m_cchAdjust);
					/// invalidate the old paragraph segments, so they will be recomputed.
					//StTxtPara paraOld = new StTxtPara(m_fdoCache, hvoParaSelEnd);
					//paraOld.ClearInfoAboutSegmentAnnotations();
				}
			}
			m_cbaHvosAffectedByEdit = null;

		}
		#endregion

		/// <summary>
		///
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvoAnnotations">the annotations to move</param>
		/// <param name="hvoNewPara">the new paragraph to move them to</param>
		/// <param name="cchAdjust">the offset to add into BeginOffsets and EndOffsets</param>
		static internal void MoveCbasToNewPara(FdoCache cache, int[] hvoAnnotations, int hvoNewPara, int cchAdjust)
		{
			foreach (ICmBaseAnnotation cba in new FdoObjectSet<ICmBaseAnnotation>(cache, hvoAnnotations, true, typeof(CmBaseAnnotation)))
			{
				if (cba.BeginOffset + cchAdjust < 0 ||
					cba.EndOffset + cchAdjust < 0)
				{
					throw new ArgumentException("cchAdjust (" + cchAdjust + ") will extend cba offsets beyond paragraph limits.");
				}
				cba.BeginObjectRAHvo = hvoNewPara;
				cba.EndObjectRAHvo = hvoNewPara;
				cba.BeginOffset += cchAdjust;
				cba.EndOffset += cchAdjust;
			}
		}

		static internal int[] GetCbasPotentiallyAffectedByTextEdit(FdoCache cache, int hvo, string sql)
		{
			IVwVirtualHandler vh = DummyVirtualHandler.InstallDummyHandler(cache.VwCacheDaAccessor,
																		   "StTxtPara", "CbasAffectedByEdit", (int)FieldType.kcptReferenceSequence);
			LoadAnnotationResults(cache, sql, vh.Tag);
			int[] cbaHvosAffectedByEdit = cache.GetVectorProperty(hvo, vh.Tag, true);
			// after getting the cached cbas, clear the list, so we will load it again
			// next time. Otherwise, LoadAnnotationResults, may (for some unknown reason) keep old data
			// and not load the new.
			(vh as DummyVirtualHandler).Load(hvo, 0, cache.VwCacheDaAccessor);
			return cbaHvosAffectedByEdit;
		}

		/// <summary>
		/// find and delete affected real twfics (let the paragraph parser recompute virtual twfics?)
		/// 1.1) deletions & insertions that occur within or across twfic annotations.
		///	1.1.1) delete affected twfic(s)
		/// 1.1.2) adjust offsets
		/// </summary>
		/// <param name="cba"></param>
		/// <param name="wm"></param>
		/// <param name="ichEditMin">the beginning offset of the selection used for editing.</param>
		/// <param name="cvIns"></param>
		/// <param name="cvDel"></param>
		/// <param name="twficsToDel"></param>
		private static void HandleAnySubstantialTwficChange(ICmBaseAnnotation cba, WordMaker wm, int ichEditMin, int cvIns, int cvDel, ref Set<int> twficsToDel)
		{
			int ichEditLim = ichEditMin + cvDel;

			// Determine whether this could possibly be a substantial change in a twfic
			if (ichEditMin < cba.EndOffset && ichEditLim > cba.BeginOffset)
			{
				// the selection used to edit the text overlaps this twfic.
				//  0123456789
				// "sample sentence"
				// "sample ???tence"
				MarkCbaAndLinkedObjsForDeletion(cba, ref twficsToDel);
				return;
			}
			else if (cvIns > 0 && ichEditMin > cba.BeginOffset && ichEditMin < cba.EndOffset)
			{
				// user changed or broke up this twfic
				//  0123456789
				// "sample sentence"
				// "sample sen?tence"
				MarkCbaAndLinkedObjsForDeletion(cba, ref twficsToDel);
				return;
			}
			else if (cvIns > 0 && ichEditMin == cba.BeginOffset)
			{
				// user has prepended characters.
				// delete twfic if insertion ends with a wordforming character.
				//  0123456789
				// "sample sentence"
				// "sample ?sentence"

				// make sure the last character before this twfic
				// is a wordforming (non-whitespace) character before
				// we consider it to warrant deleting the twfic.
				if (wm.IsWordforming(ichEditMin + cvIns - 1))
				{
					MarkCbaAndLinkedObjsForDeletion(cba, ref twficsToDel);
					return;
				}
			}
			else if (cvIns > 0 && ichEditMin == cba.EndOffset)
			{
				// user has appended characters.
				// if it begins with a wordbreaking character, don't delete the twfic
				//  0123456789
				// "sample sentence"
				// "sample?xx sentence"

				// make sure the character after this twfic
				// is a wordforming (non-whitespace) character before
				// we consider it to warrant deleting the twfic.
				if (wm.IsWordforming(ichEditMin))
				{
					MarkCbaAndLinkedObjsForDeletion(cba, ref twficsToDel);
					return;
				}
			}
		}

		private static void MarkCbaAndLinkedObjsForDeletion(ICmBaseAnnotation cba, ref Set<int> cbasToDel)
		{
			(cba as CmBaseAnnotation).CollectLinkedItemsForDeletion(ref cbasToDel, true);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="cba"></param>
		/// <param name="segBreakBeginOffsetsAfterEdit"></param>
		/// <param name="segmentsAfterEdit"></param>
		/// <param name="closestSegAfterEdit">the dummy segment corresponding to cba in a list of dummy segments marking the bounds after the edit.</param>
		/// <param name="ichMinSegBreakAfterEdit">the ichMin of the segment break marker falling within the offsets of cba.
		/// -1 if cba is null, but will return cba.EndOffset if it couldn't find a segment break in the cba range.</param>
		private static void GetClosestSegmentInfoAfterEdit(ICmBaseAnnotation cba, List<int> segBreakBeginOffsetsAfterEdit, List<TsStringSegment> segmentsAfterEdit, out TsStringSegment closestSegAfterEdit, out int ichMinSegBreakAfterEdit)
		{
			closestSegAfterEdit = GetClosestSegment(cba, segmentsAfterEdit);
			ichMinSegBreakAfterEdit = GetSegmentBreakInCbaRange(closestSegAfterEdit, segBreakBeginOffsetsAfterEdit);
		}

		/// <summary>
		/// create a dictionary for indirect annotations, keyed by annotation type
		/// </summary>
		/// <param name="anns"></param>
		/// <returns></returns>
		public static Dictionary<int, List<ICmIndirectAnnotation>> MakeAnnTypeToAnnDictionary(FdoObjectSet<ICmIndirectAnnotation> anns)
		{
			Dictionary<int, List<ICmIndirectAnnotation>> targetTypeToAnn = new Dictionary<int, List<ICmIndirectAnnotation>>();
			foreach (ICmIndirectAnnotation targetFreeformAnn in anns)
			{
				List<ICmIndirectAnnotation> targetAnns;
				if (targetTypeToAnn.TryGetValue(targetFreeformAnn.AnnotationTypeRAHvo, out targetAnns))
				{
					targetAnns.Add(targetFreeformAnn);
				}
				else
				{
					targetTypeToAnn.Add(targetFreeformAnn.AnnotationTypeRAHvo,
										new List<ICmIndirectAnnotation>(new ICmIndirectAnnotation[] { targetFreeformAnn }));
				}
			}
			return targetTypeToAnn;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="indirectAnnHvos"></param>
		/// <returns></returns>
		public static Dictionary<int, List<ICmIndirectAnnotation>> MakeAnnTypeToAnnDictionary(FdoCache cache, List<int> indirectAnnHvos)
		{
			return MakeAnnTypeToAnnDictionary(new FdoObjectSet<ICmIndirectAnnotation>(cache,
																					  indirectAnnHvos.ToArray(), false, typeof(CmIndirectAnnotation)));
		}

		private static FdoObjectSet<ICmIndirectAnnotation> GetFreeformAnnotations(ICmBaseAnnotation cba)
		{
			List<int> freeformAnnHvos = new List<int>();
			foreach (LinkedObjectInfo loi in cba.LinkedObjects)
			{
				if (loi.RelObjClass == CmIndirectAnnotation.kClassId)
				{
					freeformAnnHvos.Add(loi.RelObjId);
				}
			}
			FdoObjectSet<ICmIndirectAnnotation> freeformAnns = new FdoObjectSet<ICmIndirectAnnotation>(cba.Cache,
																									   freeformAnnHvos.ToArray(), false, typeof(CmIndirectAnnotation));
			return freeformAnns;
		}

		/// <summary>
		/// given the bounds of cba, return the offset of the first character of the first segment break.
		/// If none was found, we return the EndOffset of the given cba.
		/// </summary>
		/// <param name="cba"></param>
		/// <param name="ichMinSegBreaksInPara"></param>
		/// <returns> the ichMin of the segment break marker falling within the offsets of cba.
		/// -1 if cba is null, but will return cba.EndOffset if it couldn't find a segment break in the cba range.</returns>
		private static int GetSegmentBreakInCbaRange(TsStringSegment cba, List<int> ichMinSegBreaksInPara)
		{
			if (cba == null)
				return -1;
			int ichMin = cba.BeginOffset;
			int ichLim = cba.EndOffset;
			return GetSegmentBreakInRange(ichMin, ichLim, ichMinSegBreaksInPara);
		}

		/// <summary>
		/// given the bounds of cba, return the offset of the first character of the first segment break.
		/// If none was found, we return the EndOffset of the given cba.
		/// </summary>
		/// <param name="cba"></param>
		/// <param name="ichMinSegBreaksInPara"></param>
		/// <returns> the ichMin of the segment break marker falling within the offsets of cba.
		/// -1 if cba is null, but will return cba.EndOffset if it couldn't find a segment break in the cba range.</returns>
		private static int GetSegmentBreakInCbaRange(ICmBaseAnnotation cba, List<int> ichMinSegBreaksInPara)
		{
			if (cba == null)
				return -1;
			int ichMin = cba.BeginOffset;
			int ichLim = cba.EndOffset;
			return GetSegmentBreakInRange(ichMin, ichLim, ichMinSegBreaksInPara);
		}

		private static int GetSegmentBreakInRange(int ichMin, int ichLim, List<int> ichSegBreaksInPara)
		{
			int ichMinSegBreak = ichLim;
			foreach (int ich in ichSegBreaksInPara)
			{
				if (ich > ichMin && ich <= ichLim)
				{
					ichMinSegBreak = ich;
					break;
				}
			}
			return ichMinSegBreak;
		}

		/// <summary>
		/// given cbaOrig, find the corresponding dummy segment in a list of dummy segments marking the bounds after the edit.
		/// </summary>
		/// <param name="cbaOrig"></param>
		/// <param name="segsAfterEdit">list of dummy segments marking bounds of segments after edit. we assume this is in order.</param>
		/// <returns>the dummy segment nearest to cbaOrig</returns>
		private static TsStringSegment GetClosestSegment(ICmBaseAnnotation cbaOrig, List<TsStringSegment> segsAfterEdit)
		{
			FdoCache cache = cbaOrig.Cache;
			int ichMin = cbaOrig.BeginOffset;
			foreach (TsStringSegment seg in segsAfterEdit)
			{
				// we stop after we find a segment that equals or is greater than cbaOrig's offset.
				// (this assumes segsAfterEdit list is in order.)
				if (seg.BeginOffset >= ichMin)
					return seg;
			}
			return null;
		}

		private static void AdjustSegmentToNewSegmentBounds(ICmBaseAnnotation cbaToAdjust, TsStringSegment newSeg)
		{
			if (newSeg == null)
				return; // or Assert.Fail? should not happen, but be defensive.
			if (cbaToAdjust.BeginOffset != newSeg.BeginOffset)
				cbaToAdjust.BeginOffset = newSeg.BeginOffset;
			if (cbaToAdjust.EndOffset != newSeg.EndOffset)
				cbaToAdjust.EndOffset = newSeg.EndOffset;
		}

		private static void HandleAnySubstantialPunctuationChange(ICmBaseAnnotation cba, WordMaker wm, int ichEditMin, int cvIns, int cvDel,
																  ref Set<int> punctsToDel)
		{
			int ichEditLim = ichEditMin + cvDel;
			// 1.2) detect deletions that occur across segment annotations
			//	1.2.1) merge adjacent indirect annotations
			//		- TODO: mark indirect annotations as needing verification.
			//	1.2.2) delete first annotation
			if (cvDel > 0 && ichEditMin <= cba.BeginOffset && ichEditLim >= cba.EndOffset)
			{
				// user deleted entire punctuation cba
				//  0123456789
				// "sample. sentence"
				// "sample sentence"
				punctsToDel.Add(cba.Hvo);
				return;
			}
			else if (cvIns > 0 && ichEditMin >= cba.BeginOffset)
			{
				// user inserted something inside the punctuation
				punctsToDel.Add(cba.Hvo);
				return;
			}
		}

		private static void LoadAnnotationResults(FdoCache cache, string sql, int vtag)
		{
			IVwOleDbDa dba = cache.MainCacheAccessor as IVwOleDbDa;
			IDbColSpec dcs = DbColSpecClass.Create();
			dcs.Push((int)DbColType.koctBaseId, 0, 0, 0); // First column contains IDs that are the base of properties
			dcs.Push((int)DbColType.koctObjVec, 1, vtag, 0); // flid used to store the result vector on id of first column.
			dcs.Push((int)DbColType.koctObj, 2, (int)CmAnnotation.CmAnnotationTags.kflidAnnotationType, 0); // type of annotation in column 2.
			dcs.Push((int)DbColType.koctObj, 2, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginOffset, 0);
			dcs.Push((int)DbColType.koctObj, 2, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidEndOffset, 0);
			dba.Load(sql, dcs, 0, 0, null, false);
		}

		/// <summary>
		/// given a real cbaToAdjust, adjust its offsets to the closest matching dummy segment
		/// in a list of segsAfterEdit.
		/// </summary>
		/// <param name="cbaToAdjust"></param>
		/// <param name="segsAfterEdit"></param>
		private void AdjustSegmentToNewSegmentBounds(ICmBaseAnnotation cbaToAdjust, List<TsStringSegment> segsAfterEdit)
		{
			TsStringSegment newSeg = AnnotationAdjuster.GetClosestSegment(cbaToAdjust, segsAfterEdit);
			AnnotationAdjuster.AdjustSegmentToNewSegmentBounds(cbaToAdjust, newSeg);
		}

		/// <summary>
		/// Handle the general case for adjusting the offsets of an annotation based on the
		/// position of the edit and the number of chars inserted and deleted.
		/// </summary>
		/// <param name="cba"></param>
		/// <param name="cvIns"></param>
		/// <param name="cvDel"></param>
		/// <param name="ichBeginEdit"></param>
		/// <param name="fGrowAtBounds">if false, treat the insertion at the boundary as exterior rather than
		/// changing the character count between BeginOffset and EndOffset</param>
		void AdjustOffsets(ICmBaseAnnotation cba, int cvIns, int cvDel, int ichBeginEdit, bool fGrowAtBounds)
		{
			if (m_annsToMoveFromDeletedParagraph.Contains(cba.Hvo) || m_movedAnnsFromAfterInsertBreak.Contains(cba.Hvo))
			{
				// make sure we don't adjust the offsets a second time.
				return;
			}
			int ichEndEdit = ichBeginEdit + cvDel;
			if (ichBeginEdit > cba.EndOffset)
				return;	// no change in offsets needed.
			if (ichBeginEdit <= cba.BeginOffset && ichEndEdit >= cba.EndOffset)
			{
				int hvoType = cba.AnnotationTypeRAHvo;
				if (hvoType == m_hvoAnnDefnTwfic || hvoType == m_hvoPunctDefn || hvoType == m_hvoSegDefn)
					throw new ArgumentException("this cba has been replaced by the edit. can't adjust its offsets");
				// Some other kind of annotation which we don't attempt to delete earlier, for example,
				// a TE check annotation (see TE-7747). The best we can do is make it an empty annotation
				// at the place where the contents were deleted.
				cba.BeginOffset = cba.EndOffset = ichBeginEdit;
				return;
			}
			// the cba needs to change in size, if the edit began within the cba
			int ichMinNew = cba.BeginOffset;
			int ichLimNew = cba.EndOffset;
			// if the entire edit occurred at/before our begin offset, shift both offsets the same amount
			if (ichEndEdit <= cba.BeginOffset && !(fGrowAtBounds && (cvDel == 0 && ichBeginEdit == cba.BeginOffset)))
			{
				// 0123456
				// abc def
				// ----def
				// def
				ichMinNew += (cvIns - cvDel);
				ichLimNew += (cvIns - cvDel);
			}
			else
			{
				// edit occurred within the cba, so change its size
				// 0123456
				// abc def
				// ab---ef
				// abef
				if (ichBeginEdit >= cba.BeginOffset)
				{
					if (ichBeginEdit == cba.EndOffset && !fGrowAtBounds)
					{
						// don't try to change this cba size, if the edit starts at its end offset
					}
					else if (ichEndEdit > cba.EndOffset)
					{
						// the end of the edit crosses the bounds of the cba
						// so we need to truncate the adjustment by its end offset.
						ichLimNew += (cvIns - (cba.EndOffset - ichBeginEdit));
					}
					else
					{
						// edit is contained within the cba
						ichLimNew += (cvIns - cvDel);
					}
				}
				else if (ichEndEdit > cba.BeginOffset)
				{
					// if the end of the edit occurs within the cba
					// then we need to truncate the adjustment by its begin offset.
					// the end of the edit and the begin offset.
					ichMinNew += (cvIns - cvDel + ichEndEdit - cba.BeginOffset);
					ichLimNew += (cvIns - cvDel);
				}
			}
			int max = ((cba.BeginObjectRA) as StTxtPara).Contents.Length;
			// Make sure we don't do anything totally unreasonable in cases we didn't anticipate.
			if (ichMinNew < 0 || ichLimNew > max || ichLimNew < ichMinNew)
				return;
			// Review: would this be better?
			//ichMinNew = Math.Min(Math.Max(ichMinNew, 0), max);
			//ichLimNew = Math.Min(Math.Max(ichLimNew, 0), max);
			//if (ichLimNew < ichMinNew)
			//    ichLimNew = ichMinNew;
			CmBaseAnnotation.SetCbaFields(cba.Cache, cba.Hvo, ichMinNew, ichLimNew, cba.BeginObjectRAHvo, false);
		}

		internal bool AlreadyHandledMovingAnnotations(int hvoPara)
		{
			return m_parasWeHaveAdjusted.Contains(hvoPara);
		}
		/// <summary>
		///
		/// </summary>
		/// <param name="cba"></param>
		/// <param name="segBreakBeginOffsetsBeforeEdit"></param>
		/// <param name="segBreakBeginOffsetsAfterEdit"></param>
		/// <param name="segmentsAfterEdit"></param>
		/// <param name="ichEditMin"></param>
		/// <param name="cvIns"></param>
		/// <param name="cvDel"></param>
		/// <param name="segsToDel"></param>
		/// <param name="segToMerge"> This starts as null on the first segment we handle. It is changed to cba (the current segment) when we
		/// handle the (one and only, if any) segment that has lost the trailing punctuation that separated it from the next segment.
		/// Subsequently, that segment continues as segToMerge until we come to one that is not completely deleted and so can merge with it.
		/// if we cant find a segment in the right place to merge it with, we set its end offset according to what was computed in segmentsAfterEdit.
		/// </param>
		/// <returns>true if we handled it (caller should make no further adjustments to this CBA)</returns>
		private bool HandleAnySubstantialSegmentChange(ICmBaseAnnotation cba,
			List<int> segBreakBeginOffsetsBeforeEdit, List<int> segBreakBeginOffsetsAfterEdit,
			List<TsStringSegment> segmentsAfterEdit, int ichEditMin, int cvIns, int cvDel, ref Set<int> segsToDel, ref ICmBaseAnnotation segToMerge)
		{
			// determine whether we deleted across a segment break character.
			int ichEditLim = ichEditMin + cvDel;
			int ichMinSegBreakBeforeEdit = AnnotationAdjuster.GetSegmentBreakInCbaRange(cba, segBreakBeginOffsetsBeforeEdit);
			// find the endpoint of the paragraph, since it may be the last segments endmarker.
			int ichLimParaBeforeEdit = m_anchorTextInfo.ParaText.Length;

			// If the edit range entirely contains the segment, clobber it. The following cases were originally designed
			// to handle this as a subcase, but don't do so adequately for verse number and similar segments that
			// don't actually have closing punctuation. Since this code is very tricky, it seemed safer to put in a
			// new case rather than try to patch up the old ones.
			if (cvDel > 0 && ichEditMin <= cba.BeginOffset && OnlyWhiteSpaceFollowsEditLimit(cba, ichEditLim, ichEditMin + cvIns))
			{
				AnnotationAdjuster.MarkCbaAndLinkedObjsForDeletion(cba, ref segsToDel);
				return true;
			}

			// 1.2) detect deletions that occur across segment annotations
			//	1.2.1) merge adjacent indirect annotations
			//		- TODO: mark indirect annotations as needing verification.
			//	1.2.2) delete first annotation
			if (segToMerge != null && ichEditLim <= ichMinSegBreakBeforeEdit)
			{
				// There's a previous segment to merge, and at least part of this one will survive
				// (the edit ended before its terminating punctuation). Typically we will merge the two.
				// The decisive thing is whether the re-segmenting of the paragraph requires one segment or
				// two. Since the beginning of segToMerge survived, and the end of this one, we should
				// typically find a new desired segment that matches.
				if (ThereIsAMatchingSegment(segmentsAfterEdit, segToMerge.BeginOffset, cba.EndOffset + cvIns - cvDel))
				//if (ichEditLim > cba.BeginOffset || cba.BeginOffset == segToMerge.EndOffset)
				{
					m_fSegSeqChanged = true;
					// They are (or have become by the deletion) adjacent; merge them.
					// get the Indirect Annotations for SegToMerge
					FdoObjectSet<ICmIndirectAnnotation> targetFreeformAnns = AnnotationAdjuster.GetFreeformAnnotations(segToMerge);
					FdoObjectSet<ICmIndirectAnnotation> srcFreeformAnns = AnnotationAdjuster.GetFreeformAnnotations(cba);
					// make a dictionary for type of freeform annotation to freeform annotation.
					Dictionary<int, List<ICmIndirectAnnotation>> targetTypeToAnn = AnnotationAdjuster.MakeAnnTypeToAnnDictionary(targetFreeformAnns);
					// foreach freeform annotation in the src, merge them into the (first) of same type of annotation on the target.
					// if it's not found on the target, then just move the AppliesToRC item the target.
					foreach (ICmIndirectAnnotation srcFreeformAnn in srcFreeformAnns)
					{
						List<ICmIndirectAnnotation> targetAnns;
						// Exception: don't merge note annotations, just move their instanceOf.
						if (srcFreeformAnn.AnnotationTypeRAHvo != m_segDefn_note &&
							targetTypeToAnn.TryGetValue(srcFreeformAnn.AnnotationTypeRAHvo, out targetAnns))
						{
							targetAnns[0].Comment.MergeAlternatives(srcFreeformAnn.Comment, true);
						}
						else
						{
							// it's a note or not found on the target, just move the AppliesToRC item to the target.
							srcFreeformAnn.AppliesToRS.Remove(cba.Hvo);
							srcFreeformAnn.AppliesToRS.Append(segToMerge.Hvo);
						}
					}
					// finished with the merge, so change the end offset and mark the merged cba for deletion.
					segToMerge.EndOffset = cba.EndOffset;
					AnnotationAdjuster.MarkCbaAndLinkedObjsForDeletion(cba, ref segsToDel);
					AdjustOffsets(segToMerge, cvIns, cvDel, ichEditMin, true);
				}
				else
				{
					// We didn't find an output segment to confirm that we want to merge these two,
					// so try to adjust them for reasonable survival. Eventually re-parsing the paragraph
					// will straighten things out somehow.
					AdjustSegmentToNewSegmentBounds(segToMerge, segmentsAfterEdit);
					// make sure we adjust the current cba.
					AdjustOffsets(cba, cvIns, cvDel, ichEditMin, true);
				}
				segToMerge = null;
				return true;
			}

			// We get here if either there's no previous segment needing merging, or if the edit extends beyond
			// our closing punctuation.
			if (cvDel > 0 && (ichEditLim > ichMinSegBreakBeforeEdit || ichEditLim == ichLimParaBeforeEdit))
			{
				// user deleted(and possibly inserted) across a segment boundary
				//  0123456789
				// "sample. sentence"
				// "sample!? sentence"
				m_fSegSeqChanged = true;
				if (ichEditMin <= cba.BeginOffset)
				{
					// (Case 1) the user deleted across the beginning of the sentence, in addition to the end
					// just delete the segment annotation.
					AnnotationAdjuster.MarkCbaAndLinkedObjsForDeletion(cba, ref segsToDel);
					return true;
				}
				// since the user didn't delete across the begin offset of this cba,
				// we can be fairly certain we can find a dummy segment corresponding to its begin offset.
				TsStringSegment closestSegAfterEdit;
				int ichMinSegBreakAfterEdit;
				AnnotationAdjuster.GetClosestSegmentInfoAfterEdit(cba, segBreakBeginOffsetsAfterEdit, segmentsAfterEdit, out closestSegAfterEdit, out ichMinSegBreakAfterEdit);
				if (ichMinSegBreakAfterEdit < 0)
				{
					// There is no following segment break...possibly we're deleting the final punctuation,
					// or inserted a paragraph break within this segment. Since there is no following segment,
					// this one should extend to the end of the paragraph.
					cba.EndOffset = ((cba.BeginObjectRA) as StTxtPara).Contents.Length;
					return true;
				}
				if ((ichEditMin + cvIns) > ichMinSegBreakAfterEdit)
				{
					// (Case 2) The user inserted a new segment break within this segment,
					// So adjust the end offset accordingly. (Dont merge with subsequent annotation.)

					// Example 1 - simple replacement of segment break char.
					// 0123456789012345678901234567890
					//			 >!<
					// segment one. seg two.
					// segment one! seg two.
					// (ichEditMin(11) + cvIns(1)) > ichMinSegBreakAfterEdit(11)

					// Example 2 - deletion includes twfics but insertion has a seg break char.
					// 0123456789012345678901234567890
					//   > three. -----<
					// segment one. seg two.
					// seg three. two.
					// (ichEditMin(3) + cvIns(8)) > ichMinSegBreakAfterEdit(9)
					AnnotationAdjuster.AdjustSegmentToNewSegmentBounds(cba, closestSegAfterEdit);
				}
				else
				{
					// (Case 3) The user just deleted the segment break marker, merging it with an existing segment.
					//	Rationale:
					//      since we are inside the branch for deleting the old segment boundary (ie. ichEditLim > ichMinSegBreakBeforeEdit)
					//		and the selection deletion didn't delete the whole segment (Case 1)
					//		and the user didn't insert a new segment break character (Case 2)
					//		So the user must have just deleted the segment marker so that it extends into an existing segment.
					segToMerge = cba;
				}
				return true;
			}

			// we didn't delete the old segment boundary, but we still may need to adjust our offsets.
			if (cvIns > 0 && ichEditMin >= cba.BeginOffset && ichEditMin <= cba.EndOffset)
			{
				// There was an insertion in the segment, and there was no deletion (or it didn't delete our segment break character)
				// Adjust the end offset accordingly.
				TsStringSegment closestSegAfterEdit;
				int ichMinSegBreakAfterEdit;
				AnnotationAdjuster.GetClosestSegmentInfoAfterEdit(cba, segBreakBeginOffsetsAfterEdit, segmentsAfterEdit, out closestSegAfterEdit, out ichMinSegBreakAfterEdit);

				// Handle the special case of a complete segment (e.g., a verse number, or pasting a sentence) right before this segment.
				if (cba.BeginOffset == ichEditMin && closestSegAfterEdit != null
					&& closestSegAfterEdit.EndOffset == cba.BeginOffset + cvIns - cvDel)
				{
					// We were about to change this segment's offsets to point at the newly inserted segment!
					// Instead, just adjust it as a segment that follows the edit.
					AdjustOffsets(cba, cvIns, cvDel, ichEditMin, false);
					return true;
				}

				// TODO: Special case: if the user inserted at the beginning of the sentence.
				// just move the whole segment, and depend upon the parser to adjust the rest when
				// the user switches to Interlinear.
				// The impact upon current tests is too drastic
				//if (ichEditMin == cba.BeginOffset)
				//{
				//    AdjustOffsets(cba, cvIns, cvDel, ichEditMin, false);
				//    return true;
				//}
				AnnotationAdjuster.AdjustSegmentToNewSegmentBounds(cba, closestSegAfterEdit);
				return true;
			}
			// otherwise, we expect the change can be handled by the caller in AdjustOffsets.
			// probably just an insertion or deletion contained within the segment.
			return false;
		}

		/// <summary>
		/// Answer true if there is a segment in the list with matching begin and end offsets.
		/// </summary>
		private bool ThereIsAMatchingSegment(List<TsStringSegment> segmentsAfterEdit, int beginOff, int endOff)
		{
			foreach  (TsStringSegment seg in segmentsAfterEdit)
				if (seg.BeginOffset == beginOff && seg.EndOffset == endOff)
					return true;
			return false;
		}

		// We want to be able to test whether all the significant text in cba has been deleted. However,
		// label segments (verse numbers, footnotes, and the like) include following white space.
		// This routine is called only when we know that the start of the range deleted is at or
		// before CBA. ichEditLimOld is the end of the edit (in the original string); if cba ends
		// before that, it is certainly deleted. However, if it ends after that, but all the rest of it
		// is white space, also answer true.
		// That involves testing characters in the NEW version of the paragraph, so we are passed the
		// position in the new contents that corresponds to ichEditLimOld in the old.
		private bool OnlyWhiteSpaceFollowsEditLimit(ICmBaseAnnotation cba, int ichEditLimOld, int ichEditLimNew)
		{
			if (ichEditLimOld >= cba.EndOffset)
				return true;
			StTxtPara para = cba.BeginObjectRA as StTxtPara;
			if (para == null)
				return false; // paranoia
			string contents = para.Contents.Text;
			if (contents == null)
				return false; // paranoia.
			int len = cba.EndOffset - ichEditLimOld; // To succeed, this many characters (or all) must be white.
			for (int i = ichEditLimNew; i < ichEditLimNew + len && i < contents.Length; i++)
				if (!Char.IsWhiteSpace(contents[i]))
					return false;
			return true;
		}

		/// <summary>
		/// Handles the PropChange for relevant paragraph changes in order to adjust annotations effected by the edit.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="ivMin"></param>
		/// <param name="cvIns"></param>
		/// <param name="cvDel"></param>
		internal void AdjustTextAnnotationsForEdit(int hvo, int ivMin, int cvIns, int cvDel)
		{
			if (m_anchorTextInfo == null)
				return;
			StTxtPara para = new StTxtPara(m_cache, hvo);
			// First collect segment break information before before and after the edit was done.
			List<int> segBreakBeginOffsetsBeforeEdit;
			List<int> segBreakBeginOffsetsAfterEdit;
			// collection of dummy segment annotations for edited paragraph.
			List<TsStringSegment> segmentsAfterEdit;
			using (ParagraphParserForEditMonitoring ppfem = new ParagraphParserForEditMonitoring(para as IStTxtPara))
			{
				// collect segment break info for BEFORE the edit
				List<TsStringSegment> segmentsBeforeEdit = ppfem.CollectTempSegmentAnnotations(m_anchorTextInfo.ParaText,
					out segBreakBeginOffsetsBeforeEdit);
				// collect segment break info AFTER the edit.
				segmentsAfterEdit = ppfem.CollectTempSegmentAnnotations(para.Contents.UnderlyingTsString,
																	out segBreakBeginOffsetsAfterEdit);
				if (segmentsBeforeEdit.Count != segmentsAfterEdit.Count)
					m_fSegSeqChanged = true;
			}
			WordMaker wm = new WordMaker(para.Contents.UnderlyingTsString,
										 m_cache.LanguageWritingSystemFactoryAccessor);

			Set<int> cbasToDel = new Set<int>();
			int[] cbaHvosAffectedByEdit = GetCbasPotentiallyAffectedByTextEdit(hvo, ivMin);
			ICmBaseAnnotation segToMerge = null;
			foreach (
				ICmBaseAnnotation cba in
					new FdoObjectSet<ICmBaseAnnotation>(m_cache, cbaHvosAffectedByEdit, false, typeof (CmBaseAnnotation)))
			{
				if (m_annsToMoveFromDeletedParagraph.Contains(cba.Hvo) || m_movedAnnsFromAfterInsertBreak.Contains(cba.Hvo))
				{
					// make sure we don't adjust the offsets a second time.
					continue;
				}
				// 1.0) Determine any substantial changes for CmBaseAnnotations or CmIndirectAnnotations, including
				// whether or not an annotation needs to be deleted as a result of the edit.
				// NOTE: The assumption is that the annotations were in the "correct" state before the edit we're monitoring.
				// We are trying to enforce this state of affairs by ParseText in RawTextPane.SetRoot, but
				// since the annotations may have gotten out of sync before we started monitoring, it's possible
				// the parser would guess wrongly on how to adjust things. oh well, that's the best we can do for now.
				Debug.Assert(cba.EndOffset >= ivMin,
							 "expect to be handling only cbas that come at or after the beginning of an edit");

				// All our patching up is based on comparing the current text of the paragraph with the original paragraph.
				// Don't try to patch up any CBAs that are not currently associated with that paragraph, it just produces
				// crashs. These cases are relatively rare (typically multi-paragraph deletes), and the process of reparsing
				// the paragraphs will fix things pretty well.
				// It is not worth saving the text of the LAST paragraph selected before we start the edit,
				// because all the special code in this class is only intended to optimize matching things up
				// for single-line edits.
				if (cba.BeginObjectRAHvo != m_anchorTextInfo.HvoPara)
					continue;

				if (cba.AnnotationTypeRAHvo == m_hvoAnnDefnTwfic)
				{
					AnnotationAdjuster.HandleAnySubstantialTwficChange(cba, wm, ivMin, cvIns, cvDel, ref cbasToDel);
				}
				else if (cba.AnnotationTypeRAHvo == m_hvoSegDefn)
				{
					if (HandleAnySubstantialSegmentChange(cba, segBreakBeginOffsetsBeforeEdit, segBreakBeginOffsetsAfterEdit,
						segmentsAfterEdit, ivMin, cvIns, cvDel, ref cbasToDel, ref segToMerge))
					{
						continue; // skip default offset adjusting. it's already been handled.
					}
				}
				else if (cba.AnnotationTypeRAHvo == m_hvoPunctDefn)
				{
					AnnotationAdjuster.HandleAnySubstantialPunctuationChange(cba, wm, ivMin, cvIns, cvDel, ref cbasToDel);
				}
				else if (m_cache.IsValidObject(cba.Hvo, ScrScriptureNote.kClassId) &&
					ScrScriptureNote.GetAnnotationType(cba.AnnotationTypeRA) == NoteType.CheckingError)
				{
					// We don't want to adjust for checking error annotations. These will get
					// cleaned up later if the check is run again (TE-8051)
					continue;
				}

				// 2.0 Handle any remaining nonsubstantial offset changes (simply adjust remaining offsets)
				// calculate the total change in annotation offsets after ivMin
				if (!cbasToDel.Contains(cba.Hvo))
				{
					int cbaLengthOrig = cba.EndOffset - cba.BeginOffset;
					// special case: skip zerolengthed annotations at the beginning of the paragraph (e.g. ProcessTime)
					if (cba.BeginOffset == 0 && cba.EndOffset == 0)
						continue;
					AdjustOffsets(cba, cvIns, cvDel, ivMin, cba.AnnotationTypeRAHvo != m_hvoAnnDefnTwfic);
					if (cba.AnnotationTypeRAHvo == m_hvoAnnDefnTwfic && (cba.EndOffset - cba.BeginOffset) != cbaLengthOrig)
					{
						string msg = "We currently don't support changing the size of twfic.";
						Debug.Fail(msg);
						throw new ArgumentException(msg);
					}
				}
			}
			if (segToMerge != null)
			{
				// we didn't find an adjacent real segment to merge with, so
				// we need to adjust endoffset of segToMerge
				AdjustSegmentToNewSegmentBounds(segToMerge, segmentsAfterEdit);
				segToMerge = null;
			}

			// delete any cbas that are no longer valid.
			if (cbasToDel.Count > 0)
			{
				// TODO: possibly need to invalidate virtual properties (Segments/Segforms)?
				DeleteCalculatedAnnotations(cbasToDel);
			}
		}

		private void DeleteCalculatedAnnotations(Set<int> cbasToDel)
		{
			// TE-7973: Filter out any ScrScriptureNotes so that they aren't deleted. These contain
			// data users entered and are not automatically created.
			Set<int> filteredSet = new Set<int>(cbasToDel.Count);
			foreach (int hvo in cbasToDel)
			{
				if (!m_cache.IsValidObject(hvo, ScrScriptureNote.kclsidScrScriptureNote))
					filteredSet.Add(hvo);
			}
			if (filteredSet.Count > 0)
				CmObject.DeleteObjects(filteredSet, m_cache, false);
		}

		private int[] GetCbasPotentiallyAffectedByTextEdit(int hvo, int ivMin)
		{
			string sql = String.Format("select cba.BeginObject, cba.Id, cba.AnnotationType, cba.BeginOffset, cba.EndOffset from CmBaseAnnotation_ cba " +
				"where cba.BeginObject = {0} and cba.EndOffset >= {1} " +
				"order by cba.BeginOffset, cba.EndOffset", new object[] { hvo, ivMin });
			return AnnotationAdjuster.GetCbasPotentiallyAffectedByTextEdit(m_cache, hvo, sql);
		}
		/// <summary>
		/// This is called when the view code is about to delete a paragraph. We need to save the annotations,
		/// by moving them to whatever paragraph the deleted one is merging with. (The next parse will
		/// dispose of any unnecessary ones.)
		/// </summary>
		/// <param name="sel"></param>
		/// <param name="rootb"></param>
		/// <param name="hvoObject"></param>
		/// <param name="hvoOwner"></param>
		/// <param name="tag"></param>
		/// <param name="ihvo"></param>
		/// <param name="fMergeNext"></param>
		public void AboutToDelete(IVwSelection sel, IVwRootBox rootb, int hvoObject, int hvoOwner, int tag, int ihvo, bool fMergeNext)
		{
			CheckDisposed();

			if (m_segDefn_note == 0)
				return; // in-memory testing.
			// Figure which paragraph to move them to.
			int ihvoNew = ihvo - 1;
			int chvo = m_cache.GetVectorSize(hvoOwner, tag);
			if (chvo < 2)
				return; // should not happen, we never delete the only paragraph, if we do just too bad.
			if (ihvoNew < 0)
				ihvoNew = 1; // deleting paragraph 0, attach to paragraph 1, even if fMergeNext is false.
			if (fMergeNext && ihvo < chvo - 1)
				ihvoNew = ihvo + 1;
			m_hvoWhereToMoveAnnsFromDeletedPara = m_cache.GetVectorItem(hvoOwner, tag, ihvoNew);
			// Move any annotations that point at the deleted paragraph to the selected surviving one.
			// Also make sure their old properties aren't cached.
			// Todo: arrange Undo for this.
			string sql = string.Format("select ba.id from CmBaseAnnotation ba where ba.BeginObject = {0}", hvoObject);
			int[] hvoAnnotations = DbOps.ReadIntArrayFromCommand(m_cache, sql, null);
			if (ihvoNew < ihvo)
			{
				// If this is the only or last paragraph we merge, we will merge the cbas of the paragraph
				// we are deleting into the previous paragraph.
				// We will adjust offsets by the length of the previous paragraph. Helps keep things in order.
				m_cchAdjustAnnsFromDeletedParagraph = m_cache.MainCacheAccessor.get_StringProp(
					m_hvoWhereToMoveAnnsFromDeletedPara, (int)StTxtPara.StTxtParaTags.kflidContents).Length;
				// If we previously deleted a paragraph, the annotations from THAT paragraph are now trash;
				// we aren't saving any text from that paragraph.
				m_annotationsToDelete.AddRange(m_annsToMoveFromDeletedParagraph);
				m_annsToMoveFromDeletedParagraph.Clear();
				m_movedAnnsFromAfterInsertBreak.Clear(); // Review EricP(JohnT)
				m_annsToMoveFromDeletedParagraph.AddRange(hvoAnnotations);
			}
			else
			{
				// we are deleting the previous paragraph, so just mark the annotations for deletion.
				CmBaseAnnotation.CollectLinkedItemsForDeletionFor(m_cache, new List<int>(hvoAnnotations), m_cbasToDel, true);
			}
			// Ensure the destination paragraphs is considered 'changed' so it will be parsed next time.
			ITsString tss = m_cache.MainCacheAccessor.get_StringProp(m_hvoWhereToMoveAnnsFromDeletedPara, (int)StTxtPara.StTxtParaTags.kflidContents);
			m_cache.MainCacheAccessor.SetString(m_hvoWhereToMoveAnnsFromDeletedPara, (int)StTxtPara.StTxtParaTags.kflidContents, tss);
			// Todo: we would need to ensure the virtual segment info has been cleared, so it will be parsed next time,
			// but the above code ensures that for us.
			//StTxtPara paraNew = new StTxtPara(m_cache, hvoNew);
			//paraNew.ClearInfoAboutSegmentAnnotations();
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is the drastic answer. The known problem is LT-7833, combining multiple returns in one
		/// buffer which throws off the expectation that the paragraph we are moving annotations to will
		/// be one position beyond where we typed the return. However, it's dangerous for collect typed input
		/// to add a return to ANY previous character, because it means KeyPress won't ever see the \r.
		/// Enhance: if we made another override that allowed us to peek at the NEXT character and decide
		/// whether to combine it with the current buffer contents, we might convince ourselves it was OK
		/// to behave normally except for newlines and maybe delete and backspace.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool KeepCollectingInput(int nextChr)
		{
			return (nextChr != (int)Keys.Back && nextChr != (int)Keys.Enter &&
				nextChr != (int)VwSpecialChars.kscDelForward);
		}
	}

	class RawTextPaneAnnotationAdjuster : AnnotationAdjuster
	{
		internal RawTextPaneAnnotationAdjuster(FdoCache cache, AnnotatedTextEditingHelper parent) : base(cache, parent)
		{
		}

		internal override void HandleContentChange(int hvo, int ivMin, int cvIns, int cvDel)
		{
			RawTextPane rtp = m_parentEditingHelper.Callbacks as RawTextPane;
			if (rtp == null)
				return;
			if (AlreadyHandledMovingAnnotations(hvo))
				return;
			using (RecordClerk.ListUpdateHelper luh = new RecordClerk.ListUpdateHelper(rtp.Clerk))
			{
				luh.SkipShowRecord = true;
				base.HandleContentChange(hvo, ivMin, cvIns, cvDel);
				// in general we don't want to reload the primary clerk
				// while we are editing, since that can be expensive.
				if (rtp.Clerk != null && rtp.Clerk.IsPrimaryClerk)
				{
					luh.TriggerPendingReloadOnDispose = false;
					// in some cases we may also want to do Clerk.RemoveInvalidItems()
					// to help prevent the user from crashing when they click on it.
				}
			}
		}
	}

	/// <summary>
	/// Retains the information we need about the text and paragraphs at one end of a selection.
	/// </summary>
	class TextStateInfo
	{
		private StText m_stText; // The StText containing the selection.
		private int m_hvoPara; // The selected para.
		private List<ParaStateInfo> m_paras = new List<ParaStateInfo>();
		private ITsString m_tssAnchorText;
		private bool m_fCheckOtherParasOfText;

		private const int kflidContents = (int) StTxtPara.StTxtParaTags.kflidContents;
		private const int kflidParagraphs = (int) StText.StTextTags.kflidParagraphs;
		/// <summary>
		///  Create one from the specified end of the selection. If that end is not in a
		/// relevant property return null. Also return null if in the same StText as
		/// hvoOther.
		/// </summary>
		/// <param name="info"></param>
		/// <param name="fEndPoint"></param>
		static public TextStateInfo Create(TextSelInfo info, bool fEndPoint, FdoCache cache, int hvoOther)
		{
			int offset = 0; // in the SelInfoStack
			if (info.Tag(fEndPoint) == kflidContents)
			{
				if (info.Levels(fEndPoint) < 2)
					return null;
			}
			else
			{
				// One other case we need to handle is an embedded picture, because deleting it will modify the string.
				if (!info.IsPicture || info.Levels(false) < 3 || info.ContainingObjectTag(1) != kflidContents)
					return null;
				offset = 1; // one more level for the picture.
			}
			int hvoStText = info.ContainingObject(1 + offset, fEndPoint);
			if (hvoStText == hvoOther)
				return null;
			TextStateInfo result = new TextStateInfo();
			result.m_stText = CmObject.CreateFromDBObject(cache, hvoStText) as StText;;
			result.m_hvoPara = info.ContainingObject(offset, fEndPoint);
			foreach (StTxtPara para in result.m_stText.ParagraphsOS)
				result.m_paras.Add(new ParaStateInfo(para));
			result.m_tssAnchorText = cache.GetTsStringProperty(result.m_hvoPara, kflidContents);
			result.m_fCheckOtherParasOfText = true;
			return result;
		}

		/// <summary>
		/// Create one (or return null) pertaining to a single paragraph.
		/// Returns null if it is not an StTxtPara.
		/// This should be used only for edits that will definitely NOT change the sequence of paragraphs.
		/// </summary>
		static public TextStateInfo Create(FdoCache cache, int hvoObj)
		{
			CmObject obj = CmObject.CreateFromDBObject(cache, hvoObj) as CmObject;
			if (!(obj is StTxtPara))
				return null;
			StText text = obj.Owner as StText;
			if (!(text is StText))
				return null; // paranoia!
			TextStateInfo result = new TextStateInfo();
			result.m_stText = text;
			result.m_hvoPara = hvoObj;
			result.m_paras.Add(new ParaStateInfo(obj as StTxtPara));
			result.m_tssAnchorText = (obj as StTxtPara).Contents.UnderlyingTsString;
			result.m_fCheckOtherParasOfText = false;
			return result;
		}

		/// <summary>
		/// Record the done state for each paragraph and return true if anything changed.
		/// </summary>
		/// <param name="cache"></param>
		/// <returns></returns>
		internal bool RecordDoneState(FdoCache cache)
		{
			bool result = false;
			foreach (ParaStateInfo psi in m_paras)
			{
				result |= psi.RecordDoneState(cache);
			}
			return result;
		}

		/// <summary>
		/// The(original) text of the paragraph at the end of the selection we were created for.
		/// </summary>
		public ITsString ParaText
		{
			get { return m_tssAnchorText; }
		}

		/// <summary>
		/// The Hvo of the paragraph we retain information for.
		/// </summary>
		public int HvoPara
		{
			get { return m_hvoPara; }
		}
		/// <summary>
		/// If the paragraph sequence for the text has changed, reparse it completely
		/// and issue PropChanges for any changed segment sequences. Return true if it
		/// was dealt with (including if it has been deleted).
		/// </summary>
		/// <returns></returns>
		public bool HandleMajorTextChange(int btWs)
		{
			if (!m_stText.IsValidObject())
				return true;
			if (ParaSequenceChanged())
			{
				bool fDidParse;
				ParagraphParserOptions options = new ParagraphParserOptions();
				options.SuppressSubTasks = false; // this is part of an edit, for it to be undoable the side effects need to be
				options.CreateRealSegments = true; // we will attach free translations, which requires them to be real.
				ParagraphParser.ParseText(m_stText, options, new NullProgressState(), out fDidParse);
				StTxtPara.LoadSegmentFreeTranslations(m_stText.ParagraphsOS.HvoArray, m_stText.Cache, btWs);
				Set<int> oldParas = new Set<int>(m_paras.Count);
				foreach (ParaStateInfo info in m_paras)
				{
					info.HandleSegPropChanged(m_stText.Cache);
					oldParas.Add(info.HvoPara);
				}
				int kflidSegments = StTxtPara.SegmentsFlid(m_stText.Cache);
				if (m_fCheckOtherParasOfText)
				{
					foreach (int hvoPara in m_stText.ParagraphsOS.HvoArray)
					{
						if (!oldParas.Contains(hvoPara))
						{
							// A new paragraph. Possibly some segments got created for it after it was
							// displayed.
							int cseg = m_stText.Cache.GetVectorSize(hvoPara, kflidSegments);
							if (cseg > 0)
								m_stText.Cache.PropChanged(hvoPara, kflidSegments, 0, cseg, 0);
						}
					}
				}
				return true;
			}
			return false;
		}

		/// <summary>
		/// The StText it is saving information about.
		/// </summary>
		public int HvoText
		{
			get { return m_stText.Hvo; }
		}

		/// <summary>
		/// We suspect there has been a significant change to the segments of the selected paragraph.
		/// Reparse it and issue a PropChanged if needed.
		/// </summary>
		/// <param name="btWs"></param>
		public void HandleMajorParaChange(int btWs)
		{
			ParagraphParserOptions options = new ParagraphParserOptions();
			options.SuppressSubTasks = false; // this is part of some undoable task, the side effects need to be undoable, too.
			// This is surprisingly important. If LoadSegmentFreeTranslations has to convert dummy segments to real,
			// it issues PropChanged calls; but when new segments are being added, this method gets called before the Views
			// code has been informed of the extra ones. We may end up replacing ones it doesn't know are there, resulting
			// in fall-back behavior that produces strange results when we later send the PropChanged from the old segment
			// list to the new one. It's also more efficient, since we're going to want real segments, to make them right away.
			options.CreateRealSegments = true;
			ParagraphParser.ParseParagraph(CmObject.CreateFromDBObject(m_stText.Cache, m_hvoPara, false) as StTxtPara, options);
			StTxtPara.LoadSegmentFreeTranslations(new int[] {m_hvoPara}, m_stText.Cache, btWs);
			foreach (ParaStateInfo info in m_paras)
				if (info.HvoPara == m_hvoPara)
				{
					info.HandleSegPropChanged(m_stText.Cache);
					return;
				}
		}

		bool ParaSequenceChanged()
		{
			if (m_stText.ParagraphsOS.Count != m_paras.Count)
				return true;
			for (int i = 0; i < m_paras.Count; i++)
				if (m_paras[i].HvoPara != m_stText.ParagraphsOS.HvoArray[i])
					return true;
			return false;
		}


		internal void Redo(FdoCache m_cache)
		{
			foreach (ParaStateInfo info in m_paras)
				info.Redo(m_cache);
		}

		internal void Undo(FdoCache m_cache)
		{
			foreach (ParaStateInfo info in m_paras)
				info.Undo(m_cache);
		}
	}

	/// <summary>
	/// Retains the info we need about one of the paragraphs of the text at one end of a selection.
	/// </summary>
	class ParaStateInfo
	{
		private int m_hvoPara;
		private int[] m_segments;
		private int[] m_doneSegments; // segments when done (or null if not changed).

		public ParaStateInfo(StTxtPara para)
		{
			m_hvoPara = para.Hvo;
			int kflidSegments = StTxtPara.SegmentsFlid(para.Cache);
			m_segments = para.Cache.GetVectorProperty(m_hvoPara, kflidSegments, true);
		}

		internal int HvoPara
		{
			get { return m_hvoPara; }
		}

		/// <summary>
		/// If the segments of your paragraph now are different from when you were created, issue a suitable PropChanged.
		/// Enhance JohnT: we could detect more exactly where the differences lie.
		/// </summary>
		internal void HandleSegPropChanged(FdoCache cache)
		{
			int[] newSegs = GetChangedSegments(cache);
			if (newSegs != null)
			{
				cache.PropChanged(m_hvoPara, StTxtPara.SegmentsFlid(cache), 0, newSegs.Length, m_segments.Length);
			}
		}

		bool SameIntArray(int[] first, int[] second)
		{
			if (first.Length != second.Length)
				return false;
			for (int i = 0; i < first.Length; i++)
				if (first[i] != second[i])
					return false;
			return true;
		}

		// Get the current value of the segments property -- or null, if it isn't cached or hasn't changed.
		int[] GetChangedSegments(FdoCache cache)
		{
			int kflidSegments = StTxtPara.SegmentsFlid(cache);
			if (cache.MainCacheAccessor.get_IsPropInCache(m_hvoPara, kflidSegments, (int)CellarModuleDefns.kcptReferenceSequence, 0))
			{
				int[] result = cache.GetVectorProperty(m_hvoPara, StTxtPara.SegmentsFlid(cache), true);
				if (!SameIntArray(result, m_segments))
					return result;
			}
			return null;
		}

		internal bool RecordDoneState(FdoCache cache)
		{
			m_doneSegments = GetChangedSegments(cache);
			return m_doneSegments != null;
		}

		internal void Redo(FdoCache cache)
		{
			if (m_doneSegments == null)
				return; // no change to this para
			int kflidSegments = StTxtPara.SegmentsFlid(cache);
			cache.VwCacheDaAccessor.CacheVecProp(m_hvoPara, kflidSegments, m_doneSegments, m_doneSegments.Length);
			cache.PropChanged(m_hvoPara, kflidSegments, 0, m_doneSegments.Length, m_segments.Length);
		}

		internal void Undo(FdoCache cache)
		{
			if (m_doneSegments == null)
				return; // no change to this para
			int kflidSegments = StTxtPara.SegmentsFlid(cache);
			cache.VwCacheDaAccessor.CacheVecProp(m_hvoPara, kflidSegments, m_segments, m_segments.Length);
			cache.PropChanged(m_hvoPara, kflidSegments, 0, m_segments.Length, m_doneSegments.Length);
		}
	}

	/// <summary>
	/// This class handles changes which require zero or more paragraphs and/or texts to be reparsed during Undo or Redo.
	/// Normally one is created before any other changes (to handle Undo) and another after (to handle Redo).
	/// In general we don't know before the activity starts whether any texts will need reparsing; so it is
	/// common to have one of these in the sequence with NO texts and hence doing nothing.
	/// </summary>
	class RestoreSegmentsUndoAction : IUndoAction
	{
		private TextStateInfo[] m_textStateInfo;
		private FdoCache m_cache;
		private bool m_fForRedo;

		/// <summary>
		/// Create an empty one to be stuck at the beginning of the task for Undo. Passed zero, one, or two
		/// TextStateInfo objects with the information about the endpoints. Info2 is always null if info1 is.
		/// </summary>
		public static RestoreSegmentsUndoAction CreateForUndo(FdoCache cache, TextStateInfo info1, TextStateInfo info2)
		{
			RestoreSegmentsUndoAction result = new RestoreSegmentsUndoAction();
			result.m_cache = cache;
			result.m_fForRedo = false;
			if (info2 != null)
				result.m_textStateInfo = new TextStateInfo[] {info1, info2};
			else if (info1 != null)
				result.m_textStateInfo = new TextStateInfo[] {info1};
			else
				result.m_textStateInfo = new TextStateInfo[0];
			return result;
		}

		/// <summary>
		/// Create one for Redo, with the same information as the Undo one.
		/// </summary>
		public static RestoreSegmentsUndoAction CreateForRedo(RestoreSegmentsUndoAction undoAction)
		{
			RestoreSegmentsUndoAction result = new RestoreSegmentsUndoAction();
			result.m_textStateInfo = undoAction.m_textStateInfo;
			result.m_cache = undoAction.m_cache;
			result.m_fForRedo = true;
			return result;
		}

		internal bool RecordDoneState()
		{
			bool result = false;
			foreach (TextStateInfo info in m_textStateInfo)
				result |= info.RecordDoneState(m_cache);
			return result;

		}

		#region IUndoAction Members

		public void Commit()
		{
		}

		/// <summary>
		/// It doesn't change the actual database, but it does change cache values.
		/// </summary>
		/// <returns></returns>
		public bool IsDataChange()
		{
			return true;
		}

		public bool IsRedoable()
		{
			return true;
		}

		public bool Redo(bool fRefreshPending)
		{
			if (m_fForRedo && !fRefreshPending)
				foreach(TextStateInfo info in m_textStateInfo)
					info.Redo(m_cache);
			return true;
		}

		public bool RequiresRefresh()
		{
			return false;
		}

		public bool SuppressNotification
		{
			set {  }
		}

		public bool Undo(bool fRefreshPending)
		{
			if (!m_fForRedo && !fRefreshPending)
				foreach (TextStateInfo info in m_textStateInfo)
					info.Undo(m_cache);
			return true;
		}

		#endregion
	}

}
