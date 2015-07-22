using System;
using System.Xml;
using NUnit.Framework;
using SIL.CoreImpl;

namespace XCore
{
	[TestFixture]
	public class TestTupleComparer
	{
		[Test]
		public void PriorityWins()
		{
			TryCompare(10, 11, new ClassA(), new ClassB());
		}

		[Test]
		public void ClassNameWins_WhenSamePriority()
		{
			TryCompare(10, 10, new ClassA(), new ClassB());
		}

		[Test]
		public void ConsistentAnswer_WhenSamePriorityAndName()
		{
			TryCompare(10, 10, new ClassA(), new ClassA());
		}

		[Test]
		public void SameObjectYieldsZero()
		{
			var sut = new TupleComparer();
			var tuple1 = Tuple.Create (10, (IxCoreColleague)new ClassA());
			Assert.That(sut.Compare(tuple1, tuple1), Is.EqualTo(0));
		}

		public void TryCompare(int p1, int p2, IxCoreColleague c1, IxCoreColleague c2)
		{
			var sut = new TupleComparer();
			var tuple1 = Tuple.Create (p1, c1);
			var tuple2 = Tuple.Create (p2, c2);
			Assert.That(sut.Compare(tuple1, tuple2), Is.LessThan(0));
			Assert.That(sut.Compare(tuple2, tuple1), Is.GreaterThan(0));
		}
	}

	class ClassA : IxCoreColleague
	{
		public int Priority {get; set;}

		public bool ShouldNotCall
		{
			get { return false;}
		}

		public IxCoreColleague[] GetMessageTargets () { return null;}

		public void Init(Mediator mediator, PropertyTable propertyTable, XmlNode configurationParameters)
		{}
	}

	class ClassB: ClassA
	{
	}
}