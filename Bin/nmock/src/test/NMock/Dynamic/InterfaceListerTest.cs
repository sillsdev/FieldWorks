using NUnit.Framework;
using System;
using System.Text;

namespace NMock.Dynamic
{

	[TestFixture]
	public class InterfaceListerTest
	{

		InterfaceLister i = new InterfaceLister();
		Type[] result;




		interface StandAloneI {}

		[Test]
		public void StandAloneInterface()
		{
			result = i.List(typeof(StandAloneI));
			AssertResult(typeof(StandAloneI));
		}




		class StandAloneC {}

		[Test]
		public void StandAloneClass()
		{
			result = i.List(typeof(StandAloneC));
			AssertResult(typeof(StandAloneC));
		}




		interface ExtendsI : StandAloneI {}

		[Test]
		public void InterfaceExtendsInterface()
		{
			result = i.List(typeof(ExtendsI));
			AssertResult(typeof(ExtendsI), typeof(StandAloneI));
		}




		class ExtendsC : StandAloneI {}

		[Test]
		public void ClassExtendsInterface()
		{
			result = i.List(typeof(ExtendsC));
			AssertResult(typeof(ExtendsC), typeof(StandAloneI));
		}






		interface I1 {}
		interface I2 {}
		interface I3 {}
		interface ExtendsMultipleI : I1, I2, I3 {}

		[Test]
		public void InterfaceExtendsMultipleInterfaces()
		{
			result = i.List(typeof(ExtendsMultipleI));
			AssertResult(typeof(ExtendsMultipleI), typeof(I1), typeof(I2), typeof(I3));
		}




		interface ExtendsExtendsI : ExtendsI {}

		[Test]
		public void InterfaceExtendsInterfaceExtendsInterface()
		{
			result = i.List(typeof(ExtendsExtendsI));
			AssertResult(typeof(ExtendsExtendsI), typeof(ExtendsI), typeof(StandAloneI));
		}




		class ExtendsClassC : StandAloneC {}

		[Test]
		public void ClassExtendsClass()
		{
			result = i.List(typeof(ExtendsClassC));
			AssertResult(typeof(ExtendsClassC), typeof(StandAloneC));
		}




		class ExtendsClassAndInterfaceC : StandAloneC, StandAloneI {}

		[Test]
		public void ClassExtendsClassAndInterface()
		{
			result = i.List(typeof(ExtendsClassAndInterfaceC));
			AssertResult(typeof(ExtendsClassAndInterfaceC), typeof(StandAloneI), typeof(StandAloneC));
		}




		class ExtendsExtendsC : ExtendsClassC {}

		[Test]
		public void ClassExtendsClassExtendsClass()
		{
			result = i.List(typeof(ExtendsExtendsC));
			AssertResult(typeof(ExtendsExtendsC), typeof(ExtendsClassC), typeof(StandAloneC));
		}





		//   A -      -> D -> X -
		//      -> C -           -> Y -> Z
		//   B -      -> E ------

		interface A {}
		interface B {}
		interface C : A, B {}
		interface D : C {}
		interface E : C {}
		class X : D {}
		class Y : X, E {}
		class Z : Y {}

		[Test]
		public void ReallyEvilClassHierarchy()
		{
			result = i.List(typeof(Z));
			AssertResult(
				typeof(Z),
				typeof(D),
				typeof(C),
				typeof(A),
				typeof(B),
				typeof(E),
				typeof(Y),
				typeof(X)
			);
		}


		private void AssertResult(params Type[] expected)
		{
			StringBuilder expectedString = new StringBuilder();
			StringBuilder actualString = new StringBuilder();
			foreach (Type t in expected)
			{
				expectedString.Append(t.Name);
				expectedString.Append(",");
			}
			foreach (Type t in result)
			{
				actualString.Append(t.Name);
				actualString.Append(",");
			}
			Assertion.AssertEquals(expectedString.ToString(), actualString.ToString());
		}

	}
}