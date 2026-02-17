// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Core.Scripture;

namespace ParatextImport
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for DifferenceTests.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class DifferenceTests : ScrInMemoryLcmTestBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests cloning differences - basic.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Clone()
		{
			IScrTxtPara[] paras = DiffTestHelper.CreateDummyParas(2, Cache);
			Difference diff = new Difference(
				new ScrReference(1, 1, 1, ScrVers.English),
				new ScrReference(1, 1, 30, ScrVers.English),
				paras[0], 1, 99, paras[1], 11, 88,
				DifferenceType.PictureDifference,
				null, null, "Whatever", "Whateverelse", "Esperanto", "Latvian",
				null, null);
			//diff.SectionsCurr = new int[] {6, 7, 8};

			Difference clonedDiff = diff.Clone();

			Assert.That((int)clonedDiff.RefStart, Is.EqualTo(1001001));
			Assert.That((int)clonedDiff.RefEnd, Is.EqualTo(1001030));
			Assert.That(clonedDiff.ParaCurr, Is.SameAs(paras[0]));
			Assert.That(clonedDiff.IchMinCurr, Is.EqualTo(1));
			Assert.That(clonedDiff.IchLimCurr, Is.EqualTo(99));
			Assert.That(clonedDiff.ParaRev, Is.SameAs(paras[1]));
			Assert.That(clonedDiff.IchMinRev, Is.EqualTo(11));
			Assert.That(clonedDiff.IchLimRev, Is.EqualTo(88));
			//Assert.That(clonedDiff.hvoAddedSection, Is.EqualTo(987654321));
			Assert.That(clonedDiff.DiffType, Is.EqualTo(DifferenceType.PictureDifference));
			Assert.That(clonedDiff.SubDiffsForParas, Is.Null);
			Assert.That(clonedDiff.SubDiffsForORCs, Is.Null);
			Assert.That(clonedDiff.StyleNameCurr, Is.EqualTo("Whatever"));
			Assert.That(clonedDiff.StyleNameRev, Is.EqualTo("Whateverelse"));
			Assert.That(clonedDiff.WsNameCurr, Is.EqualTo("Esperanto"));
			Assert.That(clonedDiff.WsNameRev, Is.EqualTo("Latvian"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests cloning differences when Difference contains sections.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("Enable when added sections are stored in array")]
		public void Clone_WithSections()
		{
			// wish we had a simpler constructor
			//Difference diffA = new Difference(
			//    new ScrReference(1, 1, 1, ScrVers.English), new ScrReference(1, 1, 30, ScrVers.English),
			//    DifferenceType.SectionAddedToCurrent,
			//    new int[] {6, 7, 8},
			//    4712, 11);
			////diff.SectionsCurr = new int[] {6, 7, 8};

			//Difference clonedDiff = diffA.Clone();

			//Assert.That(clonedDiff.RefStart, Is.EqualTo(1001001));
			//Assert.That(clonedDiff.RefEnd, Is.EqualTo(1001030));
			//Assert.That((DifferenceType)clonedDiff.DiffType, Is.EqualTo(DifferenceType.SectionAddedToCurrent));
			//Assert.That(clonedDiff.SectionsCurr[0], Is.EqualTo(6));
			//Assert.That(clonedDiff.SectionsCurr[1], Is.EqualTo(7));
			//Assert.That(clonedDiff.SectionsCurr[2], Is.EqualTo(8));
			//Assert.That(clonedDiff.ParaCurr, Is.EqualTo(0));
			//Assert.That(clonedDiff.IchMinCurr, Is.EqualTo(0));
			//Assert.That(clonedDiff.IchLimCurr, Is.EqualTo(0));
			//Assert.That(clonedDiff.ParaRev, Is.EqualTo(4712));
			//Assert.That(clonedDiff.IchMinRev, Is.EqualTo(11));
			//Assert.That(clonedDiff.IchLimRev, Is.EqualTo(11));
			//Assert.That(clonedDiff.SubDifferences, Is.Null);
			//Assert.That(clonedDiff.StyleNameCurr, Is.Null);
			//Assert.That(clonedDiff.StyleNameRev, Is.Null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests cloning differences when Difference contains multiple SubDifferences
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Clone_WithSubDiffs()
		{
			IScrTxtPara[] paras = DiffTestHelper.CreateDummyParas(8, Cache);
			Difference subSubDiff = new Difference(
				new ScrReference(1, 1, 3, ScrVers.English), new ScrReference(1, 1, 30, ScrVers.English),
				paras[0], 0, 99, paras[1], 11, 88,
				DifferenceType.PictureDifference, null, null,
				"Whatever", "Whateverelse", "Esperanto", "Latvian",
				null, null);
			Difference subDiff1 = new Difference(
				new ScrReference(1, 1, 2, ScrVers.English), new ScrReference(1, 1, 30, ScrVers.English),
				paras[2], 0, 99, paras[3], 11, 88,
				DifferenceType.PictureDifference, new List<Difference>(new Difference[] { subSubDiff }),
				new List<Difference>(new Difference[] { subSubDiff }), "Whatever", "Whateverelse", "Esperanto", "Latvian",
				null, null);
			Difference subDiff2 = new Difference(
				new ScrReference(1, 1, 4, ScrVers.English), new ScrReference(1, 1, 30, ScrVers.English),
				paras[4], 0, 99, paras[5], 11, 88,
				DifferenceType.PictureDifference, null, null,
				"Whatever", "Whateverelse", "Esperanto", "Latvian",
				null, null);
			Difference diff = new Difference(
				new ScrReference(1, 1, 1, ScrVers.English), new ScrReference(1, 1, 30, ScrVers.English),
				paras[6], 0, 99, paras[7], 11, 88,
				DifferenceType.PictureDifference, new List<Difference>(new Difference[] { subDiff1, subDiff2 }),
				new List<Difference>(new Difference[] { subDiff1, subDiff2 }), "Whatever", "Whateverelse", "Esperanto", "Latvian",
				null, null);

			Difference clonedDiff = diff.Clone();

			Assert.That(clonedDiff.SubDiffsForORCs.Count, Is.EqualTo(2));
			Assert.That(clonedDiff.SubDiffsForORCs[0].SubDiffsForORCs.Count, Is.EqualTo(1));
			Assert.That(clonedDiff.SubDiffsForORCs[1].SubDiffsForORCs, Is.Null);
			Assert.That(clonedDiff.SubDiffsForORCs[0].SubDiffsForORCs[0].SubDiffsForORCs, Is.Null);

			Assert.That(clonedDiff.SubDiffsForParas.Count, Is.EqualTo(2));
			Assert.That(clonedDiff.SubDiffsForParas[0].SubDiffsForParas.Count, Is.EqualTo(1));
			Assert.That(clonedDiff.SubDiffsForParas[1].SubDiffsForParas, Is.Null);
			Assert.That(clonedDiff.SubDiffsForParas[0].SubDiffsForParas[0].SubDiffsForParas, Is.Null);
		}
	}
}
