// Copyright (c) 2006-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.FieldWorks.CacheLight;
using SIL.LCModel.Core.KernelInterfaces;

namespace SIL.FieldWorks.CacheLightTests
{
	/// <summary>
	/// Base class for testing the field, class, and virtual methods.
	/// </summary>
	public class MetaDataCacheBase
	{
		/// <summary></summary>
		protected IFwMetaDataCache m_metaDataCache;

		/// <summary>
		/// If a test overrides this, it should call this base implementation.
		/// </summary>
		[TestFixtureSetUp]
		public virtual void FixtureSetup()
		{
			m_metaDataCache = MetaDataCache.CreateMetaDataCache("TestModel.xml");
		}

		/// <summary />
		[TestFixtureTearDown]
		public virtual void FixtureTearDown()
		{
		}
	}
}