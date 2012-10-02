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
// File: InterlinViewDataCache.cs
// Responsibility: pyle
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.DomainServices;
using HvoFlidKey=SIL.FieldWorks.FDO.HvoFlidKey;

namespace SIL.FieldWorks.IText
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class InterlinViewDataCache : DomainDataByFlidDecoratorBase
	{
		private const int ktagMostApprovedAnalysis = -64; // arbitrary non-valid flid to use for storing Guesses
		private const int ktagOpinionAgent = -66; // arbitrary non-valid flid to use for storing opinion agents

		private readonly IDictionary<HvoFlidKey, int> m_guessCache = new Dictionary<HvoFlidKey, int>();
		private readonly IDictionary<HvoFlidKey, int> m_humanApproved = new Dictionary<HvoFlidKey, int>();

		public InterlinViewDataCache(FdoCache cache) : base(cache.DomainDataByFlid as ISilDataAccessManaged)
		{
		}

		public override bool get_IsPropInCache(int hvo, int tag, int cpt, int ws)
		{
			switch (tag)
			{
				default:
					return base.get_IsPropInCache(hvo, tag, cpt, ws);
				case ktagMostApprovedAnalysis:
					return m_guessCache.ContainsKey(new HvoFlidKey(hvo, tag));
				case ktagOpinionAgent:
					return m_humanApproved.ContainsKey(new HvoFlidKey(hvo, tag));
			}
		}

		public override int get_ObjectProp(int hvo, int tag)
		{
			switch (tag)
			{
				default:
					return base.get_ObjectProp(hvo, tag);
				case ktagMostApprovedAnalysis:
					{
						int result;
						if (m_guessCache.TryGetValue(new HvoFlidKey(hvo, tag), out result))
							return result;
						return 0; // no guess cached.
					}
			}
		}

		public override int get_IntProp(int hvo, int tag)
		{
			switch (tag)
			{
				default:
					return base.get_IntProp(hvo, tag);
				case ktagOpinionAgent:
					{
						int result;
						if (m_humanApproved.TryGetValue(new HvoFlidKey(hvo, tag), out result))
							return result;
						return 0; // not cached.
					}
			}
		}

		public override void SetObjProp(int hvo, int tag, int hvoObj)
		{
			switch (tag)
			{
				default:
					base.SetObjProp(hvo, tag, hvoObj);
					break;
				case ktagMostApprovedAnalysis:
					m_guessCache[new HvoFlidKey(hvo, tag)] = hvoObj;
					break;
			}
		}

		public override void SetInt(int hvo, int tag, int n)
		{
			switch (tag)
			{
				default:
					base.SetInt(hvo, tag, n);
					break;
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
