// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using SIL.FieldWorks.Common.Controls;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;

namespace LanguageExplorer.Areas.TextsAndWords
{
	/// <summary>
	/// Information about how a paragraph is being changed. Also handles actually making the change.
	/// </summary>
	internal class ParaChangeInfo
	{
		readonly RespellUndoAction m_action;
		readonly int m_hvoTarget; // the one being changed.
		readonly int m_flid; // property being changed (typically paragraph contents, but occasionally picture caption)
		readonly int m_ws; // if m_flid is multilingual, ws of alternative; otherwise, zero.

		public ParaChangeInfo(RespellUndoAction action, int hvoTarget, int flid, int ws)
		{
			m_action = action;
			m_hvoTarget = hvoTarget;
			m_flid = flid;
			m_ws = ws;
		}

		/// <summary>
		/// para contents if change proceeds.
		/// </summary>
		public ITsString NewContents { get; set; }

		/// <summary>
		/// para contents if change does not proceed (or is undone).
		/// </summary>
		public ITsString OldContents { get; set; }

		/// <summary>
		/// Get the list of changes that are to be made to this paragraph, that is, CmBaseAnnotations
		/// which point at it and have been selected to be changed.
		/// </summary>
		public List<int> Changes { get; } = new List<int>();

		/// <summary>
		/// Figure what the new contents needs to be. (Also sets OldContents.) Maybe even set
		/// the contents.
		/// </summary>
		/// <param name="fMakeChangeNow">if set to <c>true</c> make the actual change now;
		/// otherwise, just figure out what the new contents will be.</param>
		/// <param name="progress"></param>
		public void MakeNewContents(bool fMakeChangeNow, ProgressDialogWorkingOn progress)
		{
			var sda = m_action.RespellSda;
			OldContents = RespellUndoAction.AnnotationTargetString(m_hvoTarget, m_flid, m_ws, sda);
			var bldr = OldContents.GetBldr();
			Changes.Sort((left, right) => sda.get_IntProp(left, ConcDecorator.kflidBeginOffset).CompareTo(sda.get_IntProp(right, ConcDecorator.kflidBeginOffset)));

			for (var i = Changes.Count - 1; i >= 0; i--)
			{
				var ichMin = sda.get_IntProp(Changes[i], ConcDecorator.kflidBeginOffset);
				var ichLim = sda.get_IntProp(Changes[i], ConcDecorator.kflidEndOffset);
				var replacement = Replacement(m_action.OldOccurrence(Changes[i]));
				bldr.Replace(ichMin, ichLim, replacement, null);
				if (!fMakeChangeNow)
				{
					continue;
				}
				var tssNew = bldr.GetString();
				if (OldContents.Equals(tssNew))
				{
					continue;
				}

				if (m_ws == 0)
				{
					sda.SetString(m_hvoTarget, m_flid, tssNew);
				}
				else
				{
					sda.SetMultiStringAlt(m_hvoTarget, m_flid, m_ws, tssNew);
				}
			}
			RespellUndoAction.UpdateProgress(progress);
			NewContents = bldr.GetString();
		}

		/// <summary>
		/// Update all the changed wordforms to point at the new Wf.
		/// Must be called before we change the text of the paragraph, as it
		/// depends on offsets into the old string.
		/// This is usually redundant, since updating the text of the paragraph automatically updates the
		/// segment analysis. However, this can be used to force an upper case occurrence to be analyzed
		/// as the lower-case wordform (FWR-3134). The paragraph adjustment does not force this back
		/// (at least not immediately).
		/// </summary>
		private void UpdateInstanceOf(ProgressDialogWorkingOn progress)
		{
#if JASONTODO
			// TODO: Fix it, or get rid of the method.
#endif
			Debug.Fail(@"use of this method was causing very unpleasant data corruption in texts, the bug it fixed needs addressing though.");
			var analysesToChange = new List<Tuple<ISegment, int>>();
			var sda = m_action.RespellSda;
			foreach (var hvoFake in Changes)
			{
				var hvoSeg = sda.get_ObjectProp(hvoFake, ConcDecorator.kflidSegment);
				var beginOffset = sda.get_IntProp(hvoFake, ConcDecorator.kflidBeginOffset);
				if (hvoSeg > 0)
				{
					var seg = m_action.RepoSeg.GetObject(hvoSeg);
					var canal = seg.AnalysesRS.Count;
					for (var i = 0; i < canal; ++i)
					{
						var anal = seg.AnalysesRS[i];
						if (anal.HasWordform && anal.Wordform.Hvo == m_action.OldWordform)
						{
							if (seg.GetAnalysisBeginOffset(i) == beginOffset)
							{
								// Remember that we want to change it, but don't do it yet,
								// because there may be other occurrences in this paragraph,
								// and changing the analysis to something which may have a different
								// length could mess things up.
								analysesToChange.Add(new Tuple<ISegment, int>(seg, i));
							}
						}
					}
				}
			}
			if (analysesToChange.Count > 0)
			{
				var newVal = new[] { m_action.RepoWf.GetObject(m_action.NewWordform) };
				foreach (var change in analysesToChange)
				{
					change.Item1.AnalysesRS.Replace(change.Item2, 1, newVal);
				}
			}
			RespellUndoAction.UpdateProgress(progress);
		}

		private string Replacement(ITsString oldTss)
		{
			var replacement = m_action.NewSpelling;
			if (!m_action.PreserveCase)
			{
				return replacement;
			}
			int var;
			var ws = oldTss.get_Properties(0).GetIntPropValues((int)FwTextPropType.ktptWs, out var);
			var cf = m_action.GetCaseFunctionFor(ws);
			if (cf.StringCase(oldTss.Text) == StringCaseStatus.title)
			{
				replacement = cf.ToTitle(replacement);
			}
			return replacement;
		}
	}
}