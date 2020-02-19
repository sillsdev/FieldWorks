// Copyright (c) 2006-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.TestUtilities.Tests
{
	/// <summary>
	/// Base class for testing the field, class, and virtual methods.
	/// </summary>
	public abstract class MetaDataCacheBase
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