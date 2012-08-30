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
using SIL.Utils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.CoreImpl;
using SILUBS.SharedScrUtils;

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
		List<string> m_stylesToRemove;
		#endregion

		#region Setup, Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialization called for each test.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();
			m_ktVwWrapper = new KeyTermsViewWrapper(null, Cache, null, null, 0, "Dummy Project", null, null);

			m_stylesToRemove = new List<string>();
			m_stylesToRemove.Add("Chapter Number");
			m_stylesToRemove.Add("Verse Number");
		}

		/// <summary/>
		public override void TestTearDown()
		{
			if (m_ktVwWrapper != null)
			{
				m_ktVwWrapper.Dispose();
				m_ktVwWrapper = null;
			}

			base.TestTearDown();
		}

		private KeyTermRef CreateCheckRef(int scrRef)
		{
			IChkTerm chkTerm = Cache.ServiceLocator.GetInstance<IChkTermFactory>().Create();
			Cache.LanguageProject.KeyTermsList.PossibilitiesOS.Add(chkTerm);

			IChkRef chkRef = Cache.ServiceLocator.GetInstance<IChkRefFactory>().Create();
			chkTerm.OccurrencesOS.Add(chkRef);
			chkRef.Ref = scrRef;

			return new KeyTermRef(chkRef);
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
			ITsString tss = TsStringUtils.MakeTss("angel", Cache.DefaultVernWs, "Emphasis");
			Assert.AreEqual("angel", TsStringUtils.GetCleanTextFromTsString(tss,
				m_stylesToRemove, true, Cache.LanguageWritingSystemFactoryAccessor));
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
			tsb.Replace(0, 0, "angel", StyleUtils.CharStyleTextProps("Emphasis", Cache.DefaultVernWs));
			tsb.ReplaceTsString(5, 5, TsStringUtils.CreateOrcFromGuid(new Guid(),
				FwObjDataTypes.kodtOwnNameGuidHot, Cache.DefaultUserWs));
			// We expect the ORC to be removed from the end of the input ITsString.
			Assert.AreEqual("angel", TsStringUtils.GetCleanTextFromTsString(tsb.GetString(),
				m_stylesToRemove, true, Cache.LanguageWritingSystemFactoryAccessor));
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
			tsb.Replace(0, 0, "an", StyleUtils.CharStyleTextProps("Emphasis", Cache.DefaultVernWs));
			tsb.ReplaceTsString(2, 2, TsStringUtils.CreateOrcFromGuid(new Guid(),
				FwObjDataTypes.kodtOwnNameGuidHot, Cache.DefaultUserWs));
			tsb.Replace(3, 3, "gel", StyleUtils.CharStyleTextProps("Emphasis", Cache.DefaultVernWs));
			// We expect the ORC to be removed from the middle of the input ITsString.
			Assert.AreEqual("angel", TsStringUtils.GetCleanTextFromTsString(tsb.GetString(),
				m_stylesToRemove, true, Cache.LanguageWritingSystemFactoryAccessor));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method IsValidTSS when the input string is an ITsString with a string that
		/// is too long to be used as a vernacular equivalent.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: Disabled due to mono bug: https://bugzilla.novell.com/show_bug.cgi?id=517855")]
		public void IsValidTSS_TooLong()
		{
			StringBuilder str = new StringBuilder();
			for (int i = 0; i < 600; i++)
				str.Append('A');
			ITsStrBldr tsb = TsStrBldrClass.Create();
			tsb.Replace(0, 0, str.ToString(), StyleUtils.CharStyleTextProps("Emphasis", Cache.DefaultVernWs));
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
			refRange[0] = refRange[1] = new ScrReference(01001001, ScrVers.English);
			Assert.IsTrue(KeyTermsViewWrapper.IsRangeInKtRef(CreateCheckRef(01001001),
				refRange, refRange));
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
			refRange[0] = refRange[1] = new ScrReference(01001001, ScrVers.English);
			Assert.IsFalse(KeyTermsViewWrapper.IsRangeInKtRef(CreateCheckRef(01001002),
				refRange, refRange));
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
			refRange[0] = new ScrReference(01001001, ScrVers.English);
			refRange[1] = new ScrReference(01001003, ScrVers.English);
			Assert.IsTrue(KeyTermsViewWrapper.IsRangeInKtRef(CreateCheckRef(01001002),
				refRange, refRange));
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
			anchorRefRange[0] = anchorRefRange[1] = new ScrReference(01001001, ScrVers.English);
			ScrReference[] endRefRange = new ScrReference[2];
			endRefRange[0] = endRefRange[1] = new ScrReference(01001002, ScrVers.English);
			Assert.IsFalse(KeyTermsViewWrapper.IsRangeInKtRef(CreateCheckRef(01001001),
				anchorRefRange, endRefRange));
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
			refRange[0] = refRange[1] = new ScrReference(32001017, ScrVers.English);
			Assert.IsTrue(KeyTermsViewWrapper.IsRangeInKtRef(CreateCheckRef(32002001),
				refRange, refRange));
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
			refRange[0] = refRange[1] = new ScrReference(32002001, ScrVers.English);
			Assert.IsFalse(KeyTermsViewWrapper.IsRangeInKtRef(CreateCheckRef(32002001), refRange, refRange));
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
			anchorRefRange[0] = endRefRange[0] = new ScrReference(01001001, ScrVers.English);
			anchorRefRange[1] = new ScrReference(01001001, ScrVers.English);
			endRefRange[1] = new ScrReference(01001003, ScrVers.English);
			Assert.IsFalse(KeyTermsViewWrapper.IsRangeInKtRef(CreateCheckRef(01001001), anchorRefRange, endRefRange));
		}
	}
}
