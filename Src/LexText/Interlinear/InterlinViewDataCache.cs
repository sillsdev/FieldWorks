// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using HvoFlidKey=SIL.LCModel.HvoFlidKey;

namespace SIL.FieldWorks.IText
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// A data cache for guesses
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class InterlinViewDataCache
	{
		private const int ktagMostApprovedAnalysis = -64; // arbitrary non-valid flid to use for storing Guesses
		private const int ktagOpinionAgent = -66; // arbitrary non-valid flid to use for storing opinion agents

		private readonly IDictionary<AnalysisOccurrence, int> m_guessCache = new Dictionary<AnalysisOccurrence, int>();
		private readonly IDictionary<HvoFlidKey, int> m_humanApproved = new Dictionary<HvoFlidKey, int>();

		public InterlinViewDataCache(LcmCache cache)
		{
		}

		public bool get_IsPropInCache(AnalysisOccurrence occurrence, int tag, int cpt, int ws)
		{
			switch (tag)
			{
				default:
					throw new ArgumentException(string.Format("Unhandled property id: {0}", tag.ToString()), nameof(tag));
				case ktagMostApprovedAnalysis:
					return m_guessCache.ContainsKey(occurrence);
			}
		}

		public int get_ObjectProp(AnalysisOccurrence occurrence, int tag)
		{
			switch (tag)
			{
				default:
					throw new ArgumentException(string.Format("Unhandled property id: {0}", tag.ToString()), nameof(tag));
				case ktagMostApprovedAnalysis:
					{
						int result;
						if (m_guessCache.TryGetValue(occurrence, out result))
							return result;
						return 0; // no guess cached.
					}
			}
		}

		public int get_IntProp(int hvo, int tag)
		{
			switch (tag)
			{
				default:
					throw new ArgumentException(string.Format("Unhandled property id: {0}", tag.ToString()), nameof(tag));
				case ktagOpinionAgent:
					{
						int result;
						if (m_humanApproved.TryGetValue(new HvoFlidKey(hvo, tag), out result))
							return result;
						return 0; // not cached.
					}
			}
		}

		public void SetObjProp(AnalysisOccurrence occurrence, int tag, int hvoObj)
		{
			switch (tag)
			{
				default:
					throw new ArgumentException(string.Format("Unhandled property id: {0}", tag.ToString()), nameof(tag));
				case ktagMostApprovedAnalysis:
					if (hvoObj == 0)
						m_guessCache.Remove(occurrence);
					else
						m_guessCache[occurrence] = hvoObj;
					break;
			}
		}

		public void SetInt(int hvo, int tag, int n)
		{
			switch (tag)
			{
				default:
					throw new ArgumentException(string.Format("Unhandled property id: {0}", tag.ToString()), nameof(tag));
				case ktagOpinionAgent:
					m_humanApproved[new HvoFlidKey(hvo, tag)] = n;
					break;
			}
		}

		private bool m_fSuppressResettingGuesses;

		/// <summary>
		/// Perform this action, without clearing your guesses if it modifies a property that would normally cause that.
		/// </summary>
		/// <param name="task"></param>
		internal void SuppressResettingGuesses(Action task)
		{
			try
			{
				m_fSuppressResettingGuesses = true;
				task();
			}
			finally
			{
				m_fSuppressResettingGuesses = false;
			}
		}


		public void ClearPropFromCache(int tag)
		{
			switch (tag)
			{
				default:
					break;
				case ktagOpinionAgent:
				case ktagMostApprovedAnalysis:
					if (m_fSuppressResettingGuesses)
						break;
					m_guessCache.Clear();
					m_humanApproved.Clear();
					break;
			}
		}

		/// <summary>
		/// For a given WfiWordform or WfiAnalysis, this field will store/retrieve the WfiAnalysis or WfiGloss
		/// prioritized in this order:
		/// 1) has been most approved by a user in texts. If never approved in texts, then
		/// 2) has a ICmAgentEvaluation assigned as Approved.
		/// </summary>
		internal static int AnalysisMostApprovedFlid
		{
			get { return ktagMostApprovedAnalysis; }
		}

		/// <summary>
		/// indicate whether the given (guess) analysis is human approved.
		/// </summary>
		internal static int OpinionAgentFlid
		{
			get { return ktagOpinionAgent; }
		}
	}
}
