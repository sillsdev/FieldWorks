// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: KeyTermMatchTests.cs
// ---------------------------------------------------------------------------------------------
using System.Linq;
using NUnit.Framework;
using Rhino.Mocks;
using System;

namespace SILUBS.PhraseTranslationHelper
{
	[TestFixture]
	public class KeyTermMatchTests
	{
		#region Add/get Renderings Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ability to get the "externally supplied" renderings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetNormalRenderings()
		{
			IKeyTerm ktFun = KeyTermMatchBuilderTests.AddMockedKeyTerm("diversion");
			ktFun.Stub(kt => kt.Renderings).Return(new [] {"abc", "xyz"});
			KeyTermMatchBuilder bldr = new KeyTermMatchBuilder(ktFun);
			KeyTermMatch matchFun = bldr.Matches.First();
			Assert.IsTrue(matchFun.Renderings.SequenceEqual(ktFun.Renderings));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ability to add and remove additional renderings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddAndRemoveRenderings()
		{
			IKeyTerm ktFun = KeyTermMatchBuilderTests.AddMockedKeyTerm("fun");
			ktFun.Stub(kt => kt.Renderings).Return(new [] { "abc", "xyz" });
			KeyTermMatchBuilder bldr = new KeyTermMatchBuilder(ktFun);
			KeyTermMatch matchFun = bldr.Matches.First();
			matchFun.AddRendering("wunkyboo");
			Assert.AreEqual(3, matchFun.Renderings.Count());
			Assert.IsTrue(matchFun.Renderings.Contains("wunkyboo"));
			Assert.IsTrue(matchFun.CanRenderingBeDeleted("wunkyboo"));
			Assert.IsFalse(matchFun.CanRenderingBeDeleted("abc"));
			matchFun.DeleteRendering("wunkyboo");
			Assert.IsFalse(matchFun.Renderings.Contains("wunkyboo"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the KeyTermMatch.AddRendering method throws an exception if a duplicate
		/// is added.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddRenderingFailsToAddDuplicate()
		{
			IKeyTerm ktFun = KeyTermMatchBuilderTests.AddMockedKeyTerm("good times");
			ktFun.Stub(kt => kt.Renderings).Return(new[] { "abc", "xyz" });
			KeyTermMatchBuilder bldr = new KeyTermMatchBuilder(ktFun);
			KeyTermMatch matchFun = bldr.Matches.First();
			Assert.Throws(typeof(ArgumentException), () => matchFun.AddRendering("abc"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the KeyTermMatch.CanRenderingBeDeleted method returns false for a
		/// rendering that is not in the list (should never happen in real life).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CanRenderingBeDeleted_NonExistentRendering()
		{
			IKeyTerm ktFun = KeyTermMatchBuilderTests.AddMockedKeyTerm("having a blast");
			ktFun.Stub(kt => kt.Renderings).Return(new[] { "abc" });
			KeyTermMatchBuilder bldr = new KeyTermMatchBuilder(ktFun);
			KeyTermMatch matchFun = bldr.Matches.First();
			Assert.IsFalse(matchFun.CanRenderingBeDeleted("xyz"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the KeyTermMatch.CanRenderingBeDeleted method returns false for the
		/// default rendering.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CanRenderingBeDeleted_DefaultRendering()
		{
			IKeyTerm ktFun = KeyTermMatchBuilderTests.AddMockedKeyTerm("time of my life");
			ktFun.Stub(kt => kt.Renderings).Return(new[] { "abc" });
			KeyTermMatchBuilder bldr = new KeyTermMatchBuilder(ktFun);
			KeyTermMatch matchFun = bldr.Matches.First();
			matchFun.AddRendering("bestest");
			matchFun.BestRendering = "bestest";
			Assert.IsFalse(matchFun.CanRenderingBeDeleted("bestest"));
		}
		#endregion
	}
}
