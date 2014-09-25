// Copyright (c) 2013-2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// <summary>
	/// Tests for SharedBackendServicesTests class
	/// </summary>
	[TestFixture]
	public class SharedBackendServicesTests
	{
		/// <summary>
		///	Test to make sure the AreMultipleApplicationsConnected function
		/// returns false when a null cache is passed in (for LT-15624).
		/// </summary>
		[Test]
		public void AreMultipleApplicationsConnectedWithNullCache()
		{
			Assert.IsFalse(SharedBackendServices.AreMultipleApplicationsConnected(null));
		}
	}
}
