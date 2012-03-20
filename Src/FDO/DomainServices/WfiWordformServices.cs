// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: WfiWordformServices.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.DomainServices
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class WfiWordformServices
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Values for spelling status of WfiWordform.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public enum SpellingStatusStates
		{
			/// <summary>
			/// dunno
			/// </summary>
			undecided,
			/// <summary>
			/// well-spelled
			/// </summary>
			correct,
			/// <summary>
			/// no good
			/// </summary>
			incorrect
		}

		/// <summary>
		/// Find (create, if needed) a wordform for the given ITsString.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="tssContents">The form to find.</param>
		/// <returns>A wordform with the given form.</returns>
		public static IWfiWordform FindOrCreateWordform(FdoCache cache, ITsString tssContents)
		{
			IWfiWordform wf;
			if (!cache.ServiceLocator.GetInstance<IWfiWordformRepository>().TryGetObject(tssContents, out wf))
				wf = cache.ServiceLocator.GetInstance<IWfiWordformFactory>().Create(tssContents);
			return wf;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find a wordform with the given form and writing system, creating a real one, if it
		/// is not found.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="form">The form to look for.</param>
		/// <param name="ws">The writing system to use.</param>
		/// <returns>The wordform, or null if an exception was thrown by the database accessors.</returns>
		/// ------------------------------------------------------------------------------------
		public static IWfiWordform FindOrCreateWordform(FdoCache cache, string form, IWritingSystem ws)
		{
			Debug.Assert(!string.IsNullOrEmpty(form));

			ITsString tssForm = CreateWordformTss(form, ws.Handle);
			IWfiWordform wf;

			if (!cache.ServiceLocator.GetInstance<IWfiWordformRepository>().TryGetObject(tssForm, out wf))
			{
				// Give up looking for one, and just make a new one.
				wf = cache.ServiceLocator.GetInstance<IWfiWordformFactory>().Create(tssForm);
			}
			return wf;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns an ITsString wordform with the given form and ws.
		/// </summary>
		/// <param name="form"></param>
		/// <param name="ws"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private static ITsString CreateWordformTss(string form, int ws)
		{
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			return tsf.MakeString(form, ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make the external spelling dictionary conform as closely as possible to the spelling
		/// status recorded in the Wordforms. We try to keep these in sync, but when we first
		/// create an external spelling dictionary we need to make it match, and even later, on
		/// restoring a backup or when a user on another computer changed the database, we may
		/// need to re-synchronize. The best we can do is to Add all the words we know are
		/// correct and remove all the others we know about at all; it's possible that a
		/// wordform that was previously correct and is now deleted will be thought correct by
		/// the dictionary. In the case of a major language, of course, it's also possible that
		/// words that were never in our inventory at all will be marked correct. This is the
		/// best we know how to do.
		///
		/// We also force there to be an external spelling dictionary for the default vernacular WS;
		/// others are updated only if they already exist.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void ConformSpellingDictToWordforms(FdoCache cache)
		{
			// Force a dictionary to exist for the default vernacular writing system.
			IFdoServiceLocator servloc = cache.ServiceLocator;
			var lgwsFactory = servloc.GetInstance<ILgWritingSystemFactory>();
			EnchantHelper.EnsureDictionary(cache.DefaultVernWs, lgwsFactory);

			// Make all existing spelling dictionaries give as nearly as possible the right answers.
			foreach (IWritingSystem wsObj in cache.ServiceLocator.WritingSystems.VernacularWritingSystems)
			{
				int ws = wsObj.Handle;
				using (var dict = EnchantHelper.GetDictionary(ws, lgwsFactory))
				{
					if (dict == null)
						continue;
					// we only force one to exist for the default, others might not have one.
					foreach (IWfiWordform wf in servloc.GetInstance<IWfiWordformRepository>().AllInstances())
					{
						string wordform = wf.Form.get_String(ws).Text;
						if (!string.IsNullOrEmpty(wordform))
							EnchantHelper.SetSpellingStatus(wordform,
							wf.SpellingStatus == (int)SpellingStatusStates.correct, dict);
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Disable the vernacular spelling dictionary for all vernacular WSs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void DisableVernacularSpellingDictionary(FdoCache cache)
		{
			IFdoServiceLocator servloc = cache.ServiceLocator;
			var factory = servloc.GetInstance<ILgWritingSystemFactory>();
			foreach (IWritingSystem ws in cache.ServiceLocator.WritingSystems.VernacularWritingSystems)
				ws.SpellCheckingId = "<None>";
		}

		/// <summary>
		/// Find or create a punctuation form which has (the same text as) form.
		/// </summary>
		public static IPunctuationForm FindOrCreatePunctuationform(FdoCache cache, ITsString form)
		{
			Debug.Assert(form != null && !string.IsNullOrEmpty(form.Text));

			IPunctuationForm pf;

			if (!cache.ServiceLocator.GetInstance<IPunctuationFormRepository>().TryGetObject(form, out pf))
			{
				// Give up looking for one, and just make a new one.
				pf = cache.ServiceLocator.GetInstance<IPunctuationFormFactory>().Create();
				pf.Form = form;
			}
			return pf;
		}

		/// <summary>
		/// Find and fix duplicate wordforms (any two or more that have the same form for default vernacular and all non-empty writing systems).
		/// All anlyses (and WfiGlosses) are preserved, even if duplicated.
		/// Spelling status is correct if any of the merged items are correct, then false if any is false, otherwise stays unknown.
		/// Note: caller is responsible to create Unit of Work.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="progressBar"></param>
		/// <returns>A string containing a list of wordforms that could not be merged because they have differing values for other WSs</returns>
		public static string FixDuplicates(FdoCache cache, ProgressBar progressBar)
		{
			var failures = new HashSet<string>();
			var wfRepo = cache.ServiceLocator.GetInstance<IWfiWordformRepository>();
			// Note that we may change AllInstances in this loop, so the copy done by ToArray() is essential.
			var wfiWordforms = wfRepo.AllInstances().ToArray();
			progressBar.Minimum = 0;
			progressBar.Maximum = wfiWordforms.Length;
			progressBar.Step = 1;
			foreach (var wf in wfiWordforms)
			{
				progressBar.PerformStep();
				var text = wf.Form.VernacularDefaultWritingSystem.Text;
				if (string.IsNullOrEmpty(text))
					continue;
				var canonicalWf = wfRepo.GetMatchingWordform(cache.DefaultVernWs, text);
				if (canonicalWf == wf)
					continue;
				if (HaveInconsistentAlternatives(wf, canonicalWf))
				{
					failures.Add(text);
					continue; // can't merge.
				}
				// Move all analyses to survivor.
				foreach (var wa in wf.AnalysesOC)
					canonicalWf.AnalysesOC.Add(wa);
				foreach (var source in wf.ReferringObjects)
				{
					var srcSegment = source as ISegment;
					if (srcSegment != null)
					{
						for (;;)
						{
							int index = srcSegment.AnalysesRS.IndexOf(wf);
							if (index == -1)
								break;
							srcSegment.AnalysesRS[index] = canonicalWf;
						}
						continue;
					}
					var wordset = source as IWfiWordSet;
					if (wordset != null)
					{
						if (wordset.CasesRC.Contains(wf))
							wordset.CasesRC.Add(canonicalWf); // does nothing if already present.
						continue;
					}
					var rendering = source as IChkRendering;
					if (rendering != null)
					{
						rendering.SurfaceFormRA = canonicalWf;
						continue;
					}
					var chkRef = source as IChkRef;
					if (chkRef != null)
					{
						chkRef.RenderingRA = canonicalWf;
					}
				}
				if (wf.SpellingStatus == (int)SpellingStatusStates.correct)
					canonicalWf.SpellingStatus = (int)SpellingStatusStates.correct; // may be already, but ensures this wins
				else if (canonicalWf.SpellingStatus == (int)SpellingStatusStates.undecided)
					canonicalWf.SpellingStatus = wf.SpellingStatus; // the only case that does something is undecided => incorrect
				canonicalWf.Checksum = 0; // reset so parser will recheck whole group of analyses.

				// Copy over other alternatives
				foreach (var ws in wf.Form.AvailableWritingSystemIds)
				{
					if(string.IsNullOrEmpty(canonicalWf.Form.get_String(ws).Text))
						canonicalWf.Form.set_String(ws, wf.Form.get_String(ws));
				}
				wf.Delete();
			}
			if (failures.Count == 0)
				return "";
			return failures.OrderBy(x=>x).Aggregate((x,y) => x + " " + y);
		}

		/// <summary>
		/// True if the two wordforms have different forms for some alternative that is non-empty on both.
		/// </summary>
		/// <param name="first"></param>
		/// <param name="second"></param>
		/// <returns></returns>
		static bool HaveInconsistentAlternatives(IWfiWordform first, IWfiWordform second)
		{
			foreach (var ws in first.Form.AvailableWritingSystemIds)
			{
				string firstForm = first.Form.get_String(ws).Text;
				if (string.IsNullOrEmpty(firstForm))
					continue;
				string secondForm = second.Form.get_String(ws).Text;
				if (string.IsNullOrEmpty(secondForm))
					continue;
				if (!firstForm.Equals(secondForm, StringComparison.Ordinal))
					return true;
			}
			return false;
		}
	}
}
