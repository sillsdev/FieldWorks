// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TeKeyTermsTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.Scripture;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test handling of key terms.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class TeKeyTermsTests : ScrInMemoryFdoTestBase
	{
		#region Member variables
		KeyTermsViewWrapper m_ktVwWrapper;
		FdoCache m_cache;
		List<string> m_stylesToRemove;
		#endregion

		#region Setup, Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialization called once.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			CheckDisposed();
			base.FixtureSetup();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialization called for each test.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public void InitTest()
		{
			CheckDisposed();
			base.Initialize();
			m_inMemoryCache.InitializeWritingSystemEncodings();
			m_cache = Cache;
			m_ktVwWrapper = new KeyTermsViewWrapper(null, m_cache, null, null, 0, "Dummy Project");

			m_stylesToRemove = new List<string>();
			m_stylesToRemove.Add("Chapter Number");
			m_stylesToRemove.Add("Verse Number");
		}
		#endregion

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method GetKeyTermFromTSS.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GetKeyTermFromTSS()
		{
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			ITsString tss = tsf.MakeStringWithPropsRgch("angel", "angel".Length,
				StyleUtils.CharStyleTextProps("Emphasis", m_cache.DefaultVernWs));

			Assert.AreEqual("angel", StringUtils.CleanTSS(tss, m_stylesToRemove, true,
				m_cache.LanguageWritingSystemFactoryAccessor));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method GetKeyTermFromTSS when the input string is a ITsString ending with
		/// an object replacement character.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GetKeyTermFromTSS_EndsWithORC()
		{
			ITsStrBldr tsb = TsStrBldrClass.Create();
			tsb.Replace(0, 0, "angel", StyleUtils.CharStyleTextProps("Emphasis", m_cache.DefaultVernWs));
			tsb.ReplaceTsString(5, 5, StringUtils.CreateOrcFromGuid(new Guid(),
				FwObjDataTypes.kodtOwnNameGuidHot, m_cache.DefaultUserWs));
			// We expect the ORC to be removed from the end of the input ITsString.
			Assert.AreEqual("angel", StringUtils.CleanTSS(tsb.GetString(), m_stylesToRemove, true,
				m_cache.LanguageWritingSystemFactoryAccessor));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method GetKeyTermFromTSS when the input string is an ITsString with an
		/// embedded object replacement character.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GetKeyTermFromTSS_EmbeddedORC()
		{
			ITsStrBldr tsb = TsStrBldrClass.Create();
			tsb.Replace(0, 0, "an", StyleUtils.CharStyleTextProps("Emphasis", m_cache.DefaultVernWs));
			tsb.ReplaceTsString(2, 2, StringUtils.CreateOrcFromGuid(new Guid(),
				FwObjDataTypes.kodtOwnNameGuidHot, m_cache.DefaultUserWs));
			tsb.Replace(3, 3, "gel", StyleUtils.CharStyleTextProps("Emphasis", m_cache.DefaultVernWs));
			// We expect the ORC to be removed from the middle of the input ITsString.
			Assert.AreEqual("angel", StringUtils.CleanTSS(tsb.GetString(), m_stylesToRemove, true,
				m_cache.LanguageWritingSystemFactoryAccessor));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method IsValidTSS when the input string is an ITsString with a string that
		/// is too long to be used as a vernacular equivalent.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void IsValidTSS_TooLong()
		{
			StringBuilder bigString = new StringBuilder();
			for (int i = 0; i < 600; i++)
				bigString.Append('A');
			ITsStrBldr tsb = TsStrBldrClass.Create();
			tsb.Replace(0, 0, bigString.ToString(), StyleUtils.CharStyleTextProps("Emphasis", m_cache.DefaultVernWs));
			Assert.IsFalse(ReflectionHelper.GetBoolResult(m_ktVwWrapper, "IsValidTSS", tsb.GetString()));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the KeyTermsViewWrapper.IsRangeInKtRef method when the range is in a single
		/// verse, which matches the reference of the KeyTermRef.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IsRangeInKtRef_InRange_NoVerseBridge()
		{
			ScrReference[] refRange = new ScrReference[2];
			refRange[0] = refRange[1] = new ScrReference(01001001, Paratext.ScrVers.English);
			int hvoKtRef = m_inMemoryCache.NewHvo(ChkRef.kClassId);
			KeyTermRef ktRef = new KeyTermRef(Cache, hvoKtRef);
			ktRef.Ref = 01001001;
			Assert.IsTrue(KeyTermsViewWrapper.IsRangeInKtRef(ktRef, refRange, refRange));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the KeyTermsViewWrapper.IsRangeInKtRef method when the range is in a single
		/// verse, which matches the reference of the KeyTermRef.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IsRangeInKtRef_OutOfRange_NoVerseBridge()
		{
			ScrReference[] refRange = new ScrReference[2];
			refRange[0] = refRange[1] = new ScrReference(01001001, Paratext.ScrVers.English);
			int hvoKtRef = m_inMemoryCache.NewHvo(ChkRef.kClassId);
			KeyTermRef ktRef = new KeyTermRef(Cache, hvoKtRef);
			ktRef.Ref = 01001002;
			Assert.IsFalse(KeyTermsViewWrapper.IsRangeInKtRef(ktRef, refRange, refRange));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the KeyTermsViewWrapper.IsRangeInKtRef method when the range is in a bridged
		/// verse, which matches the reference of the KeyTermRef.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IsRangeInKtRef_InRange_VerseBridge()
		{
			ScrReference[] refRange = new ScrReference[2];
			refRange[0] = new ScrReference(01001001, Paratext.ScrVers.English);
			refRange[1] = new ScrReference(01001003, Paratext.ScrVers.English);
			int hvoKtRef = m_inMemoryCache.NewHvo(ChkRef.kClassId);
			KeyTermRef ktRef = new KeyTermRef(Cache, hvoKtRef);
			ktRef.Ref = 01001002;
			Assert.IsTrue(KeyTermsViewWrapper.IsRangeInKtRef(ktRef, refRange, refRange));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the KeyTermsViewWrapper.IsRangeInKtRef method when the range crosses from one
		/// verse to another, when the KeyTermRef is in one of the two verses.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IsRangeInKtRef_InAnchorButNotInEnd()
		{
			ScrReference[] anchorRefRange = new ScrReference[2];
			anchorRefRange[0] = anchorRefRange[1] = new ScrReference(01001001, Paratext.ScrVers.English);
			ScrReference[] endRefRange = new ScrReference[2];
			endRefRange[0] = endRefRange[1] = new ScrReference(01001002, Paratext.ScrVers.English);
			int hvoKtRef = m_inMemoryCache.NewHvo(ChkRef.kClassId);
			KeyTermRef ktRef = new KeyTermRef(Cache, hvoKtRef);
			ktRef.Ref = 01001001;
			Assert.IsFalse(KeyTermsViewWrapper.IsRangeInKtRef(ktRef, anchorRefRange, endRefRange));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the KeyTermsViewWrapper.IsRangeInKtRef method when the range is in a single
		/// verse, which matches the reference of the KeyTermRef after converting from the
		/// Original to English Versification scheme.
		/// REVIEW (TE-6532): Should we support various versifications for key terms list or
		/// just assume Original?
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IsRangeInKtRef_InRange_ConvertedVersification()
		{
			// Jonah 2:1 in Original == Jonah 1:17 in English
			ScrReference[] refRange = new ScrReference[2];
			refRange[0] = refRange[1] = new ScrReference(32001017, Paratext.ScrVers.English);
			int hvoKtRef = m_inMemoryCache.NewHvo(ChkRef.kClassId);
			KeyTermRef ktRefJonah2_1 = new KeyTermRef(Cache, hvoKtRef);
			ktRefJonah2_1.Ref = 32002001;
			Assert.IsTrue(KeyTermsViewWrapper.IsRangeInKtRef(ktRefJonah2_1, refRange, refRange));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the KeyTermsViewWrapper.IsRangeInKtRef method when the range is in a single
		/// verse, which matches the reference of the KeyTermRef after converting from the
		/// Original to English Versification scheme.
		/// REVIEW (TE-6532): Should we support various versifications for key terms list or
		/// just assume Original?
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IsRangeInKtRef_OutOfRange_ConvertedVersification()
		{
			// Jonah 2:1 in Original != Jonah 2:1 in English
			ScrReference[] refRange = new ScrReference[2];
			refRange[0] = refRange[1] = new ScrReference(32002001, Paratext.ScrVers.English);
			int hvoKtRef = m_inMemoryCache.NewHvo(ChkRef.kClassId);
			KeyTermRef ktRefJonah2_1 = new KeyTermRef(Cache, hvoKtRef);
			ktRefJonah2_1.Ref = 32002001;
			Assert.IsFalse(KeyTermsViewWrapper.IsRangeInKtRef(ktRefJonah2_1, refRange, refRange));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the KeyTermsViewWrapper.IsRangeInKtRef method when the range crosses from one
		/// verse to another, but crosses from a verse into a verse bridge (this is probably
		/// never going to happen in real life).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IsRangeInKtRef_InRangeButAnchorAndEndAreDifferent()
		{
			ScrReference[] anchorRefRange = new ScrReference[2];
			ScrReference[] endRefRange = new ScrReference[2];
			anchorRefRange[0] = endRefRange[0] = new ScrReference(01001001, Paratext.ScrVers.English);
			anchorRefRange[1] = new ScrReference(01001001, Paratext.ScrVers.English);
			endRefRange[1] = new ScrReference(01001003, Paratext.ScrVers.English);
			int hvoKtRef = m_inMemoryCache.NewHvo(ChkRef.kClassId);
			KeyTermRef ktRef = new KeyTermRef(Cache, hvoKtRef);
			ktRef.Ref = 01001001;
			Assert.IsFalse(KeyTermsViewWrapper.IsRangeInKtRef(ktRef, anchorRefRange, endRefRange));
		}
	}
}
