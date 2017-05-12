// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.FDO.DomainServices.SemanticDomainSearch
{
	/// <summary>
	/// Class that walks through all the semantic domains and collects search results
	/// via an injected strategy.
	/// </summary>
	internal class SemDomSearchEngine
	{
		private readonly ICmSemanticDomainRepository m_semdomRepo;

		public SemDomSearchEngine(FdoCache cache)
		{
			if (cache == null)
				throw new ApplicationException("Can't search domains without a cache.");

			m_semdomRepo = cache.ServiceLocator.GetInstance<ICmSemanticDomainRepository>();
		}

		public void WalkDomains(SemDomSearchStrategy strat)
		{
			foreach (var domain in m_semdomRepo.AllInstances())
			{
				strat.PutDomainInDesiredBucket(domain);
			}
		}
	}
}
