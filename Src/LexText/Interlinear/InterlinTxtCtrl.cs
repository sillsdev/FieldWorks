// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: InterlinTxtCtrl.cs
// Responsibility: John Thomson
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.XWorks;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using XCore;

//using SIL.FieldWorks.WordWorks.Parser;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// Arguments for an event involving selecting an object. Stores the Hvo of the object.
	/// </summary>
	public class AnnotationSelectedArgs : EventArgs
	{
		int m_hvoAnnotation;
		int m_hvoAnalysis;
		public AnnotationSelectedArgs(int hvoAnnotation, int hvoAnalysis) : base()
		{
			m_hvoAnnotation = hvoAnnotation;
			m_hvoAnalysis = hvoAnalysis;
		}

		/// <summary>
		/// Get the hvo of the annotation that was selected.
		/// </summary>
		public int HvoAnnotation
		{
			get { return m_hvoAnnotation; }
		}

		/// <summary>
		/// Get the current 'analysis' (may be WfiWordform, WfiAnalysis, or WfiGloss)
		/// of the selected annotation.
		/// </summary>
		public int HvoAnalysis
		{
			get { return m_hvoAnalysis; }
		}
	}

	public delegate void AnnotationSelectedEventHandler(object sender, AnnotationSelectedArgs e);

	/// <summary>
	/// This can be used to add a freeform annotations to a segment, independent of RootSite.
	/// </summary>
	public class BaseFreeformAdder
	{
		protected FdoCache m_fdoCache;
		protected int ktagSegFF = 0;
		protected int ktagSegFF_literalTranslation = 0;
		protected int ktagSegFF_freeTranslation = 0;
		protected int ktagSegFF_note = 0;

		public BaseFreeformAdder(FdoCache cache)
		{
			m_fdoCache = cache;
			ktagSegFF = StTxtPara.SegmentFreeformAnnotationsFlid(cache);
			ktagSegFF_literalTranslation = cache.GetIdFromGuid(CmAnnotationDefnTags.kguidAnnLiteralTranslation);
			ktagSegFF_freeTranslation = cache.GetIdFromGuid(CmAnnotationDefnTags.kguidAnnFreeTranslation);
			ktagSegFF_note = cache.GetIdFromGuid(CmAnnotationDefnTags.kguidAnnNote);
		}

		/// <summary>
		/// Add the specified type of freeform annotation to the given segment.
		/// Undoable by default.
		/// </summary>
		/// <param name="hvoSeg"></param>
		/// <param name="hvoType">freeform annotation type</param>
		/// <returns></returns>
		public ICmIndirectAnnotation AddFreeformAnnotation(int hvoSeg, int hvoType)
		{
			// (TimS): The SuppressSubTasks class was removed (in favor of Undoable/NonUndoable UOW helpers.
			// I was unsure how SuppressSubTasks was used here so I kept it here so the code's owners can
			// replace it with the correct new class.
			using (SuppressSubTasks suppressor = new SuppressSubTasks(m_fdoCache, true))
			{
				// convert any preceeding dummy segments, so paragraph parser does not push this to the first dummy sentence. (LT-7318)
				int hvoPara = m_fdoCache.GetObjProperty(hvoSeg, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginObject);
				StTxtPara para = StTxtPara.CreateFromDBObject(m_fdoCache, hvoPara) as StTxtPara;
				para.EnsurePreceedingSegmentsAreReal(hvoSeg);

				string undoString = "";
				string redoString = "";
				if (hvoType == ktagSegFF_freeTranslation)
				{
					undoString = ITextStrings.ksUndoAddFreeformTranslation;
					redoString = ITextStrings.ksRedoAddFreeformTranslation;
				}
				else if (hvoType == ktagSegFF_literalTranslation)
				{
					undoString = ITextStrings.ksUndoAddLiteralTranslation;
					redoString = ITextStrings.ksRedoAddLiteralTranslation;
				}
				else if (hvoType == ktagSegFF_note)
				{
					undoString = ITextStrings.ksUndoAddNote;
					redoString = ITextStrings.ksRedoAddNote;
				}
				else
				{
					throw new ArgumentException(String.Format("segment freeform type {0} is not yet supported here.", hvoType));
				}
				ICmIndirectAnnotation ann;
				m_fdoCache.BeginUndoTask(undoString, redoString);
				{
					ann = CmIndirectAnnotation.CreateUnownedIndirectAnnotation(m_fdoCache);
					ann.AppliesToRS.Append(hvoSeg);
					ann.AnnotationTypeRAHvo = hvoType;
					// Add it to the cached collection of freeform annotations. This is a bit clumsy because
					// it isn't a real property so we can't just use the normal methods for modifying a property.
					// Enhance JohnT: put it with the other ones of the same type.
					ISilDataAccess sda = m_fdoCache.MainCacheAccessor;
					IVwCacheDa cda = (IVwCacheDa)sda;
					int cFreeForm = sda.get_VecSize(hvoSeg, ktagSegFF);
					int[] freeForms = new int[cFreeForm + 1];
					for (int i = 0; i < cFreeForm; i++)
						freeForms[i] = sda.get_VecItem(hvoSeg, ktagSegFF, i);
					freeForms[cFreeForm] = ann.Hvo;
					cda.CacheVecProp(hvoSeg, ktagSegFF, freeForms, cFreeForm + 1);
					sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll, hvoSeg,
						ktagSegFF, cFreeForm, 1, 0);
				}
				m_fdoCache.EndUndoTask();
				return ann;
			}
		}

	}

	/// <summary>
	/// This class implements the AddFreeForm method of InterlinTextCtrl (and eventually a whole-document view).
	/// </summary>
	internal class FreeformAdder : BaseFreeformAdder
	{
		int m_hvoType;
		RootSite m_site;
		bool m_fNeedReconstruct; // true if we must reconstruct to show new annotation.
		InterlinVc m_vc;
		InterlinLineChoices m_choices;

		internal FreeformAdder(int hvoType, RootSite site, bool fNeedReconstruct, InterlinVc vc) : base(site.Cache)
		{
			m_hvoType = hvoType;
			m_site = site;
			m_fNeedReconstruct = fNeedReconstruct;
			m_choices = vc.LineChoices;
			m_vc = vc;
		}

		internal void Run(bool fMakeSelectionInNewFreeformAnnotation)
		{
			IVwSelection sel = null;
			if (m_site is InterlinDocChild)
				sel = (m_site as InterlinDocChild).MakeSandboxSel();
			// If there's no sandbox selection, there may be one in the site itself, perhaps in another
			// free translation.
			if (sel == null)
				sel = m_site.RootBox.Selection;
			if (sel == null)
				return; // Enhance JohnT: give an error, or disable the command.
			int cvsli = sel.CLevels(false);
			cvsli--; // CLevels includes the string property itself, but AllTextSelInfo doesn't need it.

			// Out variables for AllTextSelInfo.
			int ihvoRoot;
			int tagTextProp;
			int cpropPrevious;
			int ichAnchor;
			int ichEnd;
			int ws;
			bool fAssocPrev;
			int ihvoEnd;
			ITsTextProps ttpBogus;
			// Main array of information retrived from sel that made combo.
			SelLevInfo[] rgvsli = SelLevInfo.AllTextSelInfo(sel, cvsli,
				out ihvoRoot, out tagTextProp, out cpropPrevious, out ichAnchor, out ichEnd,
				out ws, out fAssocPrev, out ihvoEnd, out ttpBogus);

			// Identify the segment.
			// This is important because although we are currently displaying just an StTxtPara,
			// eventually it might be part of a higher level structure. We want this to work
			// no matter how much higher level structure there is.
			int itagSegments = -1;
			for (int i = rgvsli.Length; --i>=0; )
			{
				if (rgvsli[i].tag == m_vc.ktagParaSegments)
				{
					itagSegments = i;
					break;
				}
			}
			if (itagSegments == -1)
				return; // Enhance JohnT: throw? disable command? Give an error?

			int hvoSeg = rgvsli[itagSegments].hvo;
			if (m_fdoCache.IsDummyObject(hvoSeg))
			{
				// we need to convert this into a real segment before proceeding.
				ICmBaseAnnotation cbaReal = CmBaseAnnotation.ConvertBaseAnnotationToReal(m_fdoCache, hvoSeg);
				hvoSeg = cbaReal != null ? cbaReal.Hvo : 0;
				rgvsli[itagSegments].hvo = hvoSeg;
			}
			ICmIndirectAnnotation ann = AddFreeformAnnotation(hvoSeg, m_hvoType);

			// If necessary (e.g., we just added a previously invisible FF annotation),
			// Reconstruct the root box. Otherwise, a simple PropChanged will do.
			if (m_fNeedReconstruct)
			{
				m_site.RootBox.Reconstruct();
//				m_site.Invalidate();
//				m_site.Update(); // necessary to get a lazy
			}

			if (!fMakeSelectionInNewFreeformAnnotation)
				return;
			// Now try to make a new selection in the FF we just made.
			// The elements of rgvsli from itagSegments onwards form a path to the segment.
			// In the segment we want the freeform propery, specifically the new one we just made.
			// We want to select at the start of it.
			SelLevInfo[] rgvsliNew = new SelLevInfo[rgvsli.Length - itagSegments + 1];
			for (int i = 1; i < rgvsliNew.Length; i++)
				rgvsliNew[i] = rgvsli[i + itagSegments - 1];
			// Work out how many freeforms are DISPLAYED before the (first occurrence of the) one we want to select.
			int ihvo = 0;
			Dictionary<int, List<int>> dict = m_vc.OrganizeFfAnnotations(hvoSeg);
			for (int i = m_choices.FirstFreeformIndex; i < m_choices.Count; )
			{
				int hvoAnnType = m_vc.SegDefnFromFfFlid(m_choices[i].Flid);
				List<int> annotations = null;
				if (dict.ContainsKey(hvoAnnType))
				{
					annotations = dict[hvoAnnType];
				}
				if (hvoAnnType == m_hvoType)
				{
					ihvo += annotations.IndexOf(ann.Hvo);
					break; // And that's where we want our selection!!
				}
				// Adjacent WSS of the same annotation count as only ONE object in the display.
				// So we advance i over as many items in m_choices as there are adjacent Wss
				// of the same flid.
				i += m_choices.AdjacentWssAtIndex(i).Length;
				// But each time we display this flid, we display ALL the objects,
				// so advance ihvo by the number of annotations of the type.
				int chvoAnn = annotations == null ? 0 : annotations.Count;
				ihvo += chvoAnn;
			}
			rgvsliNew[0].ihvo = ihvo;
			rgvsliNew[0].tag = m_vc.ktagSegFF;
			rgvsliNew[0].cpropPrevious = 0;
			m_site.RootBox.MakeTextSelInObj(0, rgvsliNew.Length, rgvsliNew, 0, null, true, true, false, false, true);
		}
	}
}
