// Copyright (c) 2006-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Windows.Forms;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.ObjectModel;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// This class is mostly responsible for performing various operations on the sequence of morphemes, to do
	/// with altering the morpheme breakdown. It also issues m_sandbox.OnUpdateEdited() when PropChanges
	/// occur in the cache.
	///
	/// 1. Provides an IVwNotifyChange implementation that updates all the rest of the fields when
	/// the morpheme text (or ktagSbMorphPostfix or ktagSbMorphPrefix) is edited.
	/// For example: institutionally -- originally one morpheme, probably no matches, *** below.
	/// institution ally -- breaks into two morphemes, look up both, nothing found
	/// institution -ally -- hyphen breaks out to ktagSbMorphPrefix.
	/// institution -al ly -- make third morpheme
	/// institution -al -ly -- move hyphen to ktagSbMorphPrefix
	/// in-stitution -al -ly -- I think we treat this as an odd morpheme.
	/// in- stitution -al -ly -- break up, make ktabSbMorphSuffix

	/// All these cases are handled by calling the routine that collapses the
	/// morphemes into a single string, then the one that regenerates them (any time a relevant
	/// property changes), while keeping track of how to restore the selection.

	/// When backspace or del forward tries to delete a space, we need to collapse morphemes.
	/// The root site will receive OnProblemDeletion(sel, kdptBsAtStartPara\kdptDelAtEndPara).
	/// Basically we need to be able to collapse the morphemes to a string, keeping track
	/// of the position, make the change, recompute morphemes etc, and restore the selection.
	/// Again, this is basically done by figuring the combined morphemes, deleting the space,
	/// then figuring the resulting morphemes (and restoring the selection).
	/// </summary>
	internal class SandboxEditMonitor : DisposableBase, IVwNotifyChange
	{
		SandboxBase m_sandbox; // The sandbox we're working from.
		string m_morphString; // The representation of the current morphemes as a simple string.
		int m_ichSel = -1; // The index of the selection within that string, or -1 if we don't know it.
		ISilDataAccess m_sda;
		int m_hvoSbWord;
		int m_hvoMorph;

		// The following two variables are used to overcome an infelicity of interacting with TSF
		// on Windows for keyboard input.
		private bool m_needDelayedSelection;
		private SelectionHelper.SelInfo m_infoDelayed;

		private bool m_propChangesOccurredWhileNotMonitoring;

		internal SandboxEditMonitor(SandboxBase sandbox)
		{
			m_sandbox = sandbox;
			m_sda = sandbox.Caches.DataAccess;
			m_hvoSbWord = m_sandbox.RootWordHvo;
			m_sda.AddNotification(this);
		}

		internal bool NeedMorphemeUpdate { get; set; }

		internal string BuildCurrentMorphsString()
		{
			return BuildCurrentMorphsString(null);
		}

		/// <summary>
		/// If we can't get the ws from a selection, we should get it from the choice line.
		///
		/// For now, use the real default vernacular ws for the sandbox.
		/// This only becomes available after the sandbox has been initialized.
		/// </summary>
		private int VernWsForPrimaryMorphemeLine => m_sandbox.RawWordformWs;

		internal string BuildCurrentMorphsString(IVwSelection sel)
		{
			var ichSel = -1;
			var hvoObj = 0;
			var tag = 0;
			m_needDelayedSelection = false;
			if (sel != null)
			{
				var selInfo = new TextSelInfo(sel);
				// Receiving data from TSF on Windows creates a range covering the inserted character instead
				// of an insertion point following the inserted character.  Anchor marks the beginning of that
				// range and End marks the end of the range.  We want an insertion point at the end.
				ichSel = selInfo.IchEnd;
				hvoObj = selInfo.Hvo(true);
				tag = selInfo.Tag(true);
				if (Environment.OSVersion.Platform == PlatformID.Win32NT && ichSel != selInfo.IchAnchor)
				{
					// TSF also replaces our carefully created selection, adjusted carefully to follow the
					// inserted character, with one of its own choosing after we return, so flag that we'll
					// need to recreate the desired selection at idle time when TSF has quit interfering.
					m_needDelayedSelection = true;
				}
			}
			// for now, we'll just configure getting the string for the primary morpheme line.
			var ws = VernWsForPrimaryMorphemeLine;
			m_ichSel = -1;
			var builder = TsStringUtils.MakeStrBldr();
			var space = TsStringUtils.MakeString(" ", ws);
			var sda = m_sandbox.Caches.DataAccess;
			var tssWordform = m_sandbox.SbWordForm(ws);
			// we're dealing with a phrase if there are spaces in the word.
			var fBaseWordIsPhrase = SandboxBase.IsPhrase(tssWordform.Text);
			var cmorphs = m_sda.get_VecSize(m_hvoSbWord, SandboxBase.ktagSbWordMorphs);
			for (var imorph = 0; imorph < cmorphs; ++imorph)
			{
				var hvoMorph = m_sda.get_VecItem(m_hvoSbWord, SandboxBase.ktagSbWordMorphs, imorph);
				if (imorph != 0)
				{
					builder.ReplaceTsString(builder.Length, builder.Length, space);
					// add a second space to separate morphs in a phrase.
					if (fBaseWordIsPhrase)
					{
						builder.ReplaceTsString(builder.Length, builder.Length, space);
					}
				}
				var hvoMorphForm = sda.get_ObjectProp(hvoMorph, SandboxBase.ktagSbMorphForm);
				if (hvoMorph == hvoObj && tag == SandboxBase.ktagSbMorphPrefix)
				{
					m_ichSel = builder.Length + ichSel;
				}
				builder.ReplaceTsString(builder.Length, builder.Length, sda.get_StringProp(hvoMorph, SandboxBase.ktagSbMorphPrefix));
				if (hvoMorphForm == hvoObj && tag == SandboxBase.ktagSbNamedObjName)
				{
					m_ichSel = builder.Length + ichSel;
				}
				builder.ReplaceTsString(builder.Length, builder.Length, sda.get_MultiStringAlt(hvoMorphForm, SandboxBase.ktagSbNamedObjName, ws));
				if (hvoMorph == hvoObj && tag == SandboxBase.ktagSbMorphPostfix)
				{
					m_ichSel = builder.Length + ichSel;
				}
				builder.ReplaceTsString(builder.Length, builder.Length, sda.get_StringProp(hvoMorph, SandboxBase.ktagSbMorphPostfix));
			}
			if (cmorphs == 0)
			{
				if (m_hvoSbWord == hvoObj && tag == SandboxBase.ktagMissingMorphs)
				{
					m_ichSel = ichSel;
				}
				m_morphString = InterlinComboHandler.StrFromTss(tssWordform);
			}
			else
			{
				m_morphString = InterlinComboHandler.StrFromTss(builder.GetString());
			}
			return m_morphString;
		}

		private static bool IsBaseWordPhrase(string baseWord)
		{

			return baseWord.IndexOfAny(Unicode.SpaceChars) != -1;
		}

		/// <summary>
		/// Handle an otherwise-difficult backspace (joining morphemes by deleting a 'space')
		/// Return true if successful.
		/// </summary>
		public bool HandleBackspace()
		{
			var currentMorphemes = BuildCurrentMorphsString(m_sandbox.RootBox.Selection);
			if (m_ichSel <= 0)
			{
				return false;
			}
			// This would be risky if we might be deleting a diacritic or surrogate, but we're certainly
			// deleting a space.
			currentMorphemes = currentMorphemes.Substring(0, m_ichSel - 1) + currentMorphemes.Substring(m_ichSel);
			m_ichSel--;
			SetMorphemes(currentMorphemes);
			return true;
		}

		/// <summary>
		/// Handle an otherwise-difficult delete (joining morphemes by deleting a 'space').
		/// </summary>
		public bool HandleDelete()
		{
			var currentMorphemes = BuildCurrentMorphsString(m_sandbox.RootBox.Selection);
			if (m_ichSel < 0 || m_ichSel >= currentMorphemes.Length)
			{
				return false;
			}
			// This would be risky if we might be deleting a diacritic or surrogate, but we're certainly
			// deleting a space.
			currentMorphemes = currentMorphemes.Substring(0, m_ichSel) + currentMorphemes.Substring(m_ichSel + 1);
			SetMorphemes(currentMorphemes);
			return true;
		}

		#region IVwNotifyChange Members

		/// <summary>
		/// A property changed. Is it one of the ones that requires us to update the morpheme list?
		/// Even if so, we shouldn't do it now, because it's dangerous to issue new PropChanged
		/// messages for the same property during a PropChanged. Instead we wait for a DoUpdates call.
		/// Also don't do it if we're in the middle of processing such an update already.
		/// </summary>
		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			if (!IsMonitoring)
			{
				m_propChangesOccurredWhileNotMonitoring = true;
				return;
			}
			if (IsPropMorphBreak(hvo, tag, ivMin))
			{
				NeedMorphemeUpdate = true;
			}
			// notify the parent sandbox that something has changed its cache.
			m_sandbox.OnUpdateEdited();
		}

		public void DoPendingMorphemeUpdates()
		{
			if (!NeedMorphemeUpdate)
			{
				return; // Nothing we care about has changed.
			}
			// This needs to be set BEFORE we call UpdateMorphemes...otherwise, UpdateMorphemes eventually
			// changes the selection, which triggers another call, making an infinite loop until the
			// stack overflows.
			NeedMorphemeUpdate = false;
			try
			{
				if (m_hvoMorph != 0)
				{
					// The actual form of the morpheme changed. Any current analysis can't be
					// relevant any more. (We might expect the morpheme breaker to fix this, but
					// in fact it thinks the morpheme hasn't changed, because the cache value
					// has already been updated.)
					var cda = (IVwCacheDa)m_sda;
					cda.CacheObjProp(m_hvoMorph, SandboxBase.ktagSbMorphEntry, 0);
					cda.CacheObjProp(m_hvoMorph, SandboxBase.ktagSbMorphGloss, 0);
					cda.CacheObjProp(m_hvoMorph, SandboxBase.ktagSbMorphPos, 0);
				}
				UpdateMorphemes();
			}
			finally
			{
				// We also do this as a way of making quite sure that it doesn't get set again
				// as a side effect of UpdateMorphemes...another way we could get an infinite
				// loop.
				NeedMorphemeUpdate = false;
			}

		}

		/// <summary>
		/// Is the property one of the ones that represents a morpheme breakdown?
		/// </summary>
		public bool IsPropMorphBreak(int hvo, int tag, int ws)
		{
			switch (tag)
			{
				case SandboxBase.ktagSbMorphPostfix:
				case SandboxBase.ktagSbMorphPrefix:
					m_hvoMorph = 0;
					return true;
				case SandboxBase.ktagSbNamedObjName:
					if (ws != VernWsForPrimaryMorphemeLine)
					{
						return false;
					}
					// Name of some object: is it a morph?
					var cmorphs = m_sda.get_VecSize(m_hvoSbWord, SandboxBase.ktagSbWordMorphs);
					for (var imorph = 0; imorph < cmorphs; ++imorph)
					{
						m_hvoMorph = m_sda.get_VecItem(m_hvoSbWord, SandboxBase.ktagSbWordMorphs, imorph);
						if (hvo == m_sda.get_ObjectProp(m_hvoMorph, SandboxBase.ktagSbMorphForm))
						{
							return true;
						}
					}
					m_hvoMorph = 0;
					break;
				case SandboxBase.ktagSbWordMorphs:
					return true;
				default:
					// Some property we don't care about.
					return false;
			}
			return false;
		}

		private void UpdateMorphemes()
		{
			SetMorphemes(BuildCurrentMorphsString(m_sandbox.RootBox.Selection));
		}

		private void SetMorphemes(string currentMorphemes)
		{
			if (currentMorphemes.Length == 0)
			{
				// Reconstructing the sandbox rootbox after deleting all morpheme characters
				// will cause the user to lose the ability to type in the morpheme line (cf. LT-1621).
				// So just return here, since there are no morphemes to process.
				return;
			}
			using (new SandboxEditMonitorHelper(this, true))
			{
				// This code largely duplicates that found in UpdateMorphBreaks() following the call
				// to the EditMorphBreaksDlg, with addition of the m_monitorPropChanges flag and setting
				// the selection to stay in synch with the typing.  Modifying the code to more
				// closely follow that code fixed LT-1023.
				var mb = new MorphemeBreaker(m_sandbox.Caches, currentMorphemes, m_hvoSbWord, VernWsForPrimaryMorphemeLine, m_sandbox)
				{
					IchSel = m_ichSel
				};
				mb.Run();
				NeedMorphemeUpdate = false;
				m_sandbox.RootBox.Reconstruct(); // Everything changed, more or less.
				mb.MakeSel();
				m_infoDelayed = null;
				if (!m_needDelayedSelection)
				{
					return;
				}
				// Gather up the information needed to recreate the current selection at idle time.
				var vwsel = m_sandbox.RootBox.Selection;
				m_infoDelayed = new SelectionHelper.SelInfo();
				var cvsli = vwsel.CLevels(false);
				cvsli--; // CLevels includes the string property itself, but AllTextSelInfo doesn't need it.
				int ichEnd;
				m_infoDelayed.rgvsli = SelLevInfo.AllTextSelInfo(vwsel, cvsli,
					out m_infoDelayed.ihvoRoot,
					out m_infoDelayed.tagTextProp,
					out m_infoDelayed.cpropPrevious,
					out m_infoDelayed.ich,
					out ichEnd,
					out m_infoDelayed.ws,
					out m_infoDelayed.fAssocPrev,
					out m_infoDelayed.ihvoEnd,
					out m_infoDelayed.ttpSelProps);
				Debug.Assert(ichEnd == m_infoDelayed.ich);
				Application.Idle += RecreateDelayedSelection;
			}
		}

		/// <summary>
		/// Recreate a selection that has (almost certainly) been overwritten by TSF since it can't handle a
		/// "selection changed" notification in the middle of it calling into our code to deliver new text.
		/// </summary>
		/// <remarks>
		/// See https://jira.sil.org/browse/LT-16766 "Keyman 9 IPA - insert morpheme breaks can put cursor
		/// in undesirable location, or cause a crash".
		/// </remarks>
		private void RecreateDelayedSelection(object sender, EventArgs e)
		{
#if RANDYTODO_TEST_Application_Idle
// TODO: Remove when finished sorting out idle issues.
Debug.WriteLine($"Start: Application.Idle run at: '{DateTime.Now:HH:mm:ss.ffff}': on '{GetType().Name}'.");
#endif
			Application.Idle -= RecreateDelayedSelection;
			if (m_sandbox != null && m_infoDelayed != null && m_sandbox.RootBox != null)
			{
				m_sandbox.RootBox.MakeTextSelection(
					m_infoDelayed.ihvoRoot,
					m_infoDelayed.rgvsli.Length,
					m_infoDelayed.rgvsli,
					m_infoDelayed.tagTextProp,
					m_infoDelayed.cpropPrevious,
					m_infoDelayed.ich,
					m_infoDelayed.ich,
					m_infoDelayed.ws,
					m_infoDelayed.fAssocPrev,
					m_infoDelayed.ihvoEnd,
					m_infoDelayed.ttpSelProps,
					true);
			}
			m_infoDelayed = null;
			m_needDelayedSelection = false;
#if RANDYTODO_TEST_Application_Idle
// TODO: Remove when finished sorting out idle issues.
Debug.WriteLine($"End: Application.Idle run at: '{DateTime.Now:HH:mm:ss.ffff}': on '{GetType().Name}'.");
#endif
		}

		#endregion

		internal void StartMonitoring()
		{
			if (m_propChangesOccurredWhileNotMonitoring)
			{
				m_sandbox.OnUpdateEdited();
				m_propChangesOccurredWhileNotMonitoring = false;
			}
			IsMonitoring = true;
		}

		internal void StopMonitoring()
		{
			IsMonitoring = false;
		}

		/// <summary>
		/// Don't start monitoring until directed to do so.
		/// </summary>
		internal bool IsMonitoring { get; private set; }

		#region DisposableBase

		protected override void DisposeManagedResources()
		{
			// Dispose managed resources here.
			m_sda?.RemoveNotification(this);
		}

		protected override void DisposeUnmanagedResources()
		{
			m_sda = null;
			m_sandbox = null;
		}

		#endregion
	}
}