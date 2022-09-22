// Copyright (c) 2022 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Moq;
using NUnit.Framework;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Filters;
using SIL.FieldWorks.XWorks;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;

namespace XMLViewsTests
{
	[TestFixture]
	public class OccurrenceInContextFinderTests
	{
		private static int s_nextHvo;

		[Test]
		public void TrimStrings_ZeroLength()
		{
			var occurrenceInContext = new string[0];
			var hvo = ++s_nextHvo;
			var sortItem = new ManyOnePathSortItem(hvo, new int[0], new int[0]);
			var sda = new Mock<ISilDataAccess>();
			sda.Setup(x => x.get_IntProp(hvo, ConcDecorator.kflidBeginOffset)).Returns(0);
			sda.Setup(x => x.get_IntProp(hvo, ConcDecorator.kflidEndOffset)).Returns(0);
			var sut = new OccurrenceInContextFinder(null, null, null, null) { DataAccess = sda.Object };

			// SUT
			var result = sut.TrimSortStrings(occurrenceInContext, sortItem, false);

			Assert.That(result.Length, Is.EqualTo(0));
		}

		[Test]
		public void TrimStrings_NormalCase()
		{
			const string contextBefore = "context before ";
			const string occurrence = "hit";
			const string contextAfter = " after context";
			var occurrenceInContext = new[] { contextBefore + occurrence + contextAfter };
			var hvo = ++s_nextHvo;
			var sortItem = new ManyOnePathSortItem(hvo, new int[0], new int[0]);
			var sda = new Mock<ISilDataAccess>();
			sda.Setup(x => x.get_IntProp(hvo, ConcDecorator.kflidBeginOffset)).Returns(contextBefore.Length);
			sda.Setup(x => x.get_IntProp(hvo, ConcDecorator.kflidEndOffset)).Returns(contextBefore.Length + occurrence.Length);
			var sut = new OccurrenceInContextFinder(null, null, null, null) { DataAccess = sda.Object };

			// SUT
			var result = sut.TrimSortStrings(occurrenceInContext, sortItem, false);

			Assert.That(result.Length, Is.EqualTo(1));
			Assert.That(result[0], Is.EqualTo(occurrence + contextAfter));
		}

		[Test]
		public void TrimStrings_FromEnd()
		{
			const string contextBefore = "context before the ";
			const string occurrence = "hit";
			const string contextAfter = " aft context";
			var occurrenceInContext = new[] { TsStringUtils.ReverseString(contextBefore + occurrence + contextAfter) };
			var hvo = ++s_nextHvo;
			var sortItem = new ManyOnePathSortItem(hvo, new int[0], new int[0]);
			var sda = new Mock<ISilDataAccess>();
			sda.Setup(x => x.get_IntProp(hvo, ConcDecorator.kflidBeginOffset)).Returns(contextBefore.Length);
			sda.Setup(x => x.get_IntProp(hvo, ConcDecorator.kflidEndOffset)).Returns(contextBefore.Length + occurrence.Length);
			var sut = new OccurrenceInContextFinder(null, null, null, null) { DataAccess = sda.Object };

			// SUT
			var result = sut.TrimSortStrings(occurrenceInContext, sortItem, true);

			Assert.That(result.Length, Is.EqualTo(1));
			Assert.That(result[0], Is.EqualTo(TsStringUtils.ReverseString(contextBefore + occurrence)));
		}

		[Test]
		public void TrimStrings_NoContext()
		{
			const string occurrence = "occurrence";
			var occurrenceInContext = new[] { occurrence };
			var hvo = ++s_nextHvo;
			var sortItem = new ManyOnePathSortItem(hvo, new int[0], new int[0]);
			var sda = new Mock<ISilDataAccess>();
			sda.Setup(x => x.get_IntProp(hvo, ConcDecorator.kflidBeginOffset)).Returns(0);
			sda.Setup(x => x.get_IntProp(hvo, ConcDecorator.kflidEndOffset)).Returns(occurrence.Length);
			var sut = new OccurrenceInContextFinder(null, null, null, null) { DataAccess = sda.Object };

			// SUT
			var result = sut.TrimSortStrings(occurrenceInContext, sortItem, false);

			Assert.That(result.Length, Is.EqualTo(1));
			Assert.That(result[0], Is.EqualTo(occurrence));

			// SUT
			result = sut.TrimSortStrings(occurrenceInContext, sortItem, true);

			Assert.That(result.Length, Is.EqualTo(1));
			Assert.That(result[0], Is.EqualTo(occurrence));
		}
	}
}
