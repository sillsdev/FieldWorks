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
using System.Collections.Generic;
using System.Diagnostics;

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
	}
}
