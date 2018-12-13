// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;

namespace SIL.FieldWorks.Common.Framework
{
	/// <summary>
	/// Tests the compare methods of SelInfo
	/// </summary>
	[TestFixture]
	public class SelInfo_Compare
	{
		private SelInfo s1;
		private SelInfo s2;

		/// <summary>
		/// Initialize the selection helper objects
		/// </summary>
		[SetUp]
		public void Setup()
		{
			s1 = new SelInfo
			{
				rgvsli = new SelLevInfo[2]
			};
			s2 = new SelInfo
			{
				rgvsli = new SelLevInfo[2]
			};
		}

		/// <summary />
		[TestFixtureTearDown]
		public void FixtureTeardown()
		{
			s1 = null;
			s2 = null;
		}

		/// <summary>
		/// Having different number of levels should throw an exception.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void NumberOfLevels()
		{
			Assert.IsFalse((s1 < s2));

			s2.rgvsli = new SelLevInfo[3];
			Assert.IsFalse((s1 < s2)); // exception
		}

		/// <summary>
		/// Test different parent (top level) objects. If the tags are different we expect
		/// an exception.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void TopLevelParentObjects()
		{
			s1.rgvsli[1].ihvo = 1;
			s2.rgvsli[1].ihvo = 2;
			Assert.IsTrue(s1 < s2);
			Assert.IsTrue(s2 > s1);

			s2.rgvsli[1].ihvo = 1;
			Assert.IsFalse((s1 < s2));
			Assert.IsFalse((s2 < s1));
			Assert.IsFalse((s1 > s2));
			Assert.IsFalse((s2 > s1));

			s1.rgvsli[1].cpropPrevious = 1;
			s2.rgvsli[1].cpropPrevious = 2;
			Assert.IsTrue(s1 < s2);
			Assert.IsTrue(s2 > s1);

			s2.rgvsli[1].cpropPrevious = 1;
			Assert.IsFalse((s1 < s2));
			Assert.IsFalse((s2 < s1));
			Assert.IsFalse((s1 > s2));
			Assert.IsFalse((s2 > s1));

			s1.rgvsli[1].tag = 1;
			s2.rgvsli[1].tag = 1;
			Assert.IsFalse((s1 < s2));
			Assert.IsFalse((s2 < s1));
			Assert.IsFalse((s1 > s2));
			Assert.IsFalse((s2 > s1));

			s2.rgvsli[1].tag = 2;
			Assert.IsFalse((s1 < s2));  // exception
		}

		/// <summary>
		/// Test different immediate parent objects. If the tags are different we expect
		/// an exception.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void ImmediateParentObjects()
		{
			// make the top level parent the same
			s1.rgvsli[1].ihvo = s2.rgvsli[1].ihvo = 1;
			s1.rgvsli[1].cpropPrevious = s2.rgvsli[1].cpropPrevious = 1;
			s1.rgvsli[1].tag = s2.rgvsli[1].tag = 1;

			s1.rgvsli[0].ihvo = 1;
			s2.rgvsli[0].ihvo = 2;
			Assert.IsTrue(s1 < s2);
			Assert.IsTrue(s2 > s1);

			s2.rgvsli[0].ihvo = 1;
			Assert.IsFalse((s1 < s2));
			Assert.IsFalse((s2 < s1));
			Assert.IsFalse((s1 > s2));
			Assert.IsFalse((s2 > s1));

			s1.rgvsli[0].cpropPrevious = 1;
			s2.rgvsli[0].cpropPrevious = 2;
			Assert.IsTrue(s1 < s2);
			Assert.IsTrue(s2 > s1);

			s2.rgvsli[0].cpropPrevious = 1;
			Assert.IsFalse((s1 < s2));
			Assert.IsFalse((s2 < s1));
			Assert.IsFalse((s1 > s2));
			Assert.IsFalse((s2 > s1));

			s1.rgvsli[0].tag = 1;
			s2.rgvsli[0].tag = 1;
			Assert.IsFalse((s1 < s2));
			Assert.IsFalse((s2 < s1));
			Assert.IsFalse((s1 > s2));
			Assert.IsFalse((s2 > s1));

			s2.rgvsli[0].tag = 2;
			Assert.IsFalse((s1 < s2));  // exception
		}

		/// <summary>
		/// Test differing objects.
		/// </summary>
		[Test]
		public void Leafs()
		{
			// make the parents the same
			s1.rgvsli[1].ihvo = s2.rgvsli[1].ihvo = 1;
			s1.rgvsli[1].cpropPrevious = s2.rgvsli[1].cpropPrevious = 1;
			s1.rgvsli[1].tag = s2.rgvsli[1].tag = 1;

			s1.rgvsli[0].ihvo = s2.rgvsli[0].ihvo = 1;
			s1.rgvsli[0].cpropPrevious = s2.rgvsli[0].cpropPrevious = 1;
			s1.rgvsli[0].tag = s2.rgvsli[0].tag = 1;

			s1.ihvoRoot = 1;
			s2.ihvoRoot = 2;
			Assert.IsTrue(s1 < s2);
			Assert.IsTrue(s2 > s1);

			s2.ihvoRoot = 1;
			Assert.IsFalse((s1 < s2));
			Assert.IsFalse((s2 < s1));
			Assert.IsFalse((s1 > s2));
			Assert.IsFalse((s2 > s1));

			s1.cpropPrevious = 1;
			s2.cpropPrevious = 2;
			Assert.IsTrue(s1 < s2);
			Assert.IsTrue(s2 > s1);

			s2.cpropPrevious = 1;
			Assert.IsFalse((s1 < s2));
			Assert.IsFalse((s2 < s1));
			Assert.IsFalse((s1 > s2));
			Assert.IsFalse((s2 > s1));

			s1.ich = 1;
			s2.ich = 2;
			Assert.IsTrue(s1 < s2);
			Assert.IsTrue(s2 > s1);

			s2.ich = 1;
			Assert.IsFalse((s1 < s2));
			Assert.IsFalse((s2 < s1));
			Assert.IsFalse((s1 > s2));
			Assert.IsFalse((s2 > s1));

			// we don't care about the rest of the properties, so we should always get false
			s1.fAssocPrev = true;
			s2.fAssocPrev = true;
			Assert.IsFalse((s1 < s2));
			Assert.IsFalse((s2 < s1));
			Assert.IsFalse((s1 > s2));
			Assert.IsFalse((s2 > s1));

			s2.fAssocPrev = false;
			Assert.IsFalse((s1 < s2));
			Assert.IsFalse((s2 < s1));
			Assert.IsFalse((s1 > s2));
			Assert.IsFalse((s2 > s1));

			s1.ws = 0;
			s2.ws = 1;
			Assert.IsFalse((s1 < s2));
			Assert.IsFalse((s2 < s1));
			Assert.IsFalse((s1 > s2));
			Assert.IsFalse((s2 > s1));

			s1.ihvoEnd = 0;
			s2.ihvoEnd = 1;
			Assert.IsFalse((s1 < s2));
			Assert.IsFalse((s2 < s1));
			Assert.IsFalse((s1 > s2));
			Assert.IsFalse((s2 > s1));
		}
	}
}