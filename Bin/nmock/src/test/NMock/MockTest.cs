// Copyright c 2002, Joe Walnes, Chris Stevenson, Owen Rogers
// See LICENSE.txt for details.

using System;
using NUnit.Framework;
using NMock.Constraints;

namespace NMock
{

	[TestFixture] public class MockTest : Assertion
	{

		private Mock mock;

		[SetUp] public void SetUp()
		{
			mock = new Mock("mymock");
		}

		[Test] public void HasNameSetDuringConstruction()
		{
			Assertion.AssertEquals("mymock", mock.Name);
		}

		[Test] public void VerifyiDoesNothingWithANewMock()
		{
			mock.Verify();
		}

		[Test] public void ExpectAndCallAVoidMethod()
		{
			mock.Expect("myMethod");
			mock.Invoke("myMethod");
			mock.Verify();
		}

		[Test] [ExpectedException(typeof(VerifyException))] public void VerifyFailsWhenAnExpectedVoidMethodIsNotCalled()
		{
			mock.Expect("myMethod");
			mock.Verify();
		}

		[Test] [ExpectedException(typeof(VerifyException))] public void VerifyFailsWhenTooManyCallsToAnExpectedVoidMethod()
		{
			mock.Expect("myMethod");
			mock.Invoke("myMethod");
			mock.Invoke("myMethod");
		}

		[Test] public void IgnoresUnexpectedCalls()
		{
			mock.Invoke("myMethod");
			mock.Verify();
		}

		[Test] public void VerifyMultipleCallsToSameExpectedVoidMethod()
		{
			mock.Expect("myMethod");
			mock.Expect("myMethod");
			mock.Expect("myMethod");
			mock.Invoke("myMethod");
			mock.Invoke("myMethod");
			mock.Invoke("myMethod");
			mock.Verify();
		}

		[Test] [ExpectedException(typeof(VerifyException))] public void VerifyFailsForDifferentNumberOfCallsToExpectedVoidMethod()
		{
			mock.Expect("myMethod");
			mock.Expect("myMethod");
			mock.Expect("myMethod");
			mock.Invoke("myMethod");
			mock.Invoke("myMethod");
			mock.Verify();
		}

		[Test] public void CallToMethodWithParameterConstraints()
		{
			mock.Expect("myMethod", new IsEqual("hello"), new IsAnything());
			mock.Invoke("myMethod", "hello", null);
			mock.Verify();
		}

		[Test] [ExpectedException(typeof(VerifyException))] public void CallToMethodWithInvalidParams()
		{
			mock.Expect("myMethod", new IsEqual("hello"), new IsAnything());
			mock.Invoke("myMethod", "world", null);
		}

		[Test] public void CallToMethodWithParamsButNotCheckingValues()
		{
			mock.Expect("myMethod");
			mock.Invoke("myMethod", "world", null);
			mock.Verify();
		}

		[Test] public void CallMultipleMethods()
		{
			mock.Expect("myMethod1");
			mock.Expect("myMethod2");
			mock.Expect("myMethod3");
			mock.Invoke("myMethod1");
			mock.Invoke("myMethod2");
			mock.Invoke("myMethod3");
			mock.Verify();
		}

		[Test] public void CallMultipleMethodsInDifferentOrder()
		{
			mock.Expect("myMethod1");
			mock.Expect("myMethod2");
			mock.Expect("myMethod3");
			mock.Invoke("myMethod3");
			mock.Invoke("myMethod1");
			mock.Invoke("myMethod2");
			mock.Verify();
		}

		[Test] public void CallMultipleMethodsSomeWithoutExpectations()
		{
			mock.Expect("myMethod1");
			mock.Expect("myMethod3");
			mock.Expect("myMethod3");

			mock.Invoke("myMethod2");
			mock.Invoke("myMethod3");
			mock.Invoke("myMethod1");
			mock.Invoke("myMethod3");
			mock.Verify();
		}

		[Test] public void CallToNonVoidMethod()
		{
			object something = new object();
			mock.ExpectAndReturn("myMethod", something);
			object result = mock.Invoke("myMethod");
			mock.Verify();
			Assertion.AssertSame(something, result);
		}

		[Test] public void CallToNonVoidMethodWithParams()
		{
			object something = new object();
			mock.ExpectAndReturn("myMethod", something, new IsEqual("hello"));
			object result = mock.Invoke("myMethod", "hello");
			mock.Verify();
			Assertion.AssertSame(something, result);
		}

		[Test] [ExpectedException(typeof(VerifyException))] public void CallToNonVoidMethodWithWrongParams()
		{
			object something = new object();
			mock.ExpectAndReturn("myMethod", something, new IsEqual("hello"));
			object result = mock.Invoke("myMethod", "bye");
			mock.Verify();
			Assertion.AssertSame(something, result);
		}

		[Test] public void MultipleCallToNonVoidMethod()
		{
			object something = new object();
			object anotherthing = new object();
			int x = 3;
			mock.ExpectAndReturn("myMethod", something);
			mock.ExpectAndReturn("myMethod", anotherthing);
			mock.ExpectAndReturn("myMethod", x);
			Assertion.AssertSame(something, mock.Invoke("myMethod"));
			Assertion.AssertSame(anotherthing, mock.Invoke("myMethod"));
			Assertion.AssertEquals(x, mock.Invoke("myMethod"));
			mock.Verify();
		}

		[Test] public void MultipleCallToNonVoidMethodWithCount()
		{
			object something = new object();
			mock.ExpectAndReturn(3, "myMethod", something);
			Assertion.AssertSame(something, mock.Invoke("myMethod"));
			Assertion.AssertSame(something, mock.Invoke("myMethod"));
			Assertion.AssertSame(something, mock.Invoke("myMethod"));
			mock.Verify();
		}

		[Test] public void CallToNonVoidMethodReturningNull()
		{
			mock.ExpectAndReturn("myMethod", null);
			Assertion.AssertNull(mock.Invoke("myMethod"));
			mock.Verify();
		}

		[Test] public void DefaultEqualsConstraint()
		{
			object o = new object();
			mock.Expect("myMethod", o);
			mock.Invoke("myMethod", o);
		}

		[Test] [ExpectedException(typeof(VerifyException))] public void DefaultEqualsConstraintFailure()
		{
			mock.Expect("myMethod", new object());
			mock.Invoke("myMethod", new object());
		}

		[Test] public void DefaultAnythingConstraint()
		{
			mock.Expect("myMethod", null, null);
			mock.Expect("myMethod", null, "zzz");
			mock.Expect("myMethod", "zzz", null);
			mock.Invoke("myMethod", "???", "???");
			mock.Invoke("myMethod", "???", "zzz");
			mock.Invoke("myMethod", "zzz", "???");
		}

		[Test] public void FixedValue()
		{
			mock.SetupResult("myMethod", "hello");
			mock.SetupResult("anotherMethod", "world");
			Assertion.AssertEquals("hello", mock.Invoke("myMethod"));
			Assertion.AssertEquals("hello", mock.Invoke("myMethod"));
			Assertion.AssertEquals("hello", mock.Invoke("myMethod"));
			Assertion.AssertEquals("hello", mock.Invoke("myMethod"));
			Assertion.AssertEquals("world", mock.Invoke("anotherMethod"));
			Assertion.AssertEquals("world", mock.Invoke("anotherMethod"));
			mock.SetupResult("myMethod", "bye");
			Assertion.AssertEquals("bye", mock.Invoke("myMethod"));
			Assertion.AssertEquals("bye", mock.Invoke("myMethod"));
			Assertion.AssertEquals("world", mock.Invoke("anotherMethod"));
			mock.SetupResult("myMethod", null);
			Assertion.AssertNull(mock.Invoke("myMethod"));
			Assertion.AssertNull(mock.Invoke("myMethod"));
			mock.Verify();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that we can set different fixed return values that are returned alternately.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixedValueOrder()
		{
			mock.SetupResultInOrder("myMethod", "hello");
			mock.SetupResultInOrder("myMethod", "world");
			Assertion.AssertEquals("hello", mock.Invoke("myMethod"));
			Assertion.AssertEquals("world", mock.Invoke("myMethod"));
			Assertion.AssertEquals("hello", mock.Invoke("myMethod"));
			Assertion.AssertEquals("world", mock.Invoke("myMethod"));
			Assertion.AssertEquals("hello", mock.Invoke("myMethod"));
			mock.SetupResultInOrder("myMethod", "bye");
			Assertion.AssertEquals("world", mock.Invoke("myMethod"));
			Assertion.AssertEquals("bye", mock.Invoke("myMethod"));
			Assertion.AssertEquals("hello", mock.Invoke("myMethod"));

			// calling SetupResult resets return value
			mock.SetupResult("myMethod", null);
			Assertion.AssertNull(mock.Invoke("myMethod"));
			Assertion.AssertNull(mock.Invoke("myMethod"));
			mock.Verify();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that we can set different fixed return values that are returned alternately.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixedValueOrderWithSpecifiedNumber()
		{
			mock.SetupResultInOrder(2, "myMethod", "hello");
			mock.SetupResultInOrder(2, "myMethod", "world");
			Assertion.AssertEquals("hello", mock.Invoke("myMethod"));
			Assertion.AssertEquals("hello", mock.Invoke("myMethod"));
			Assertion.AssertEquals("world", mock.Invoke("myMethod"));
			Assertion.AssertEquals("world", mock.Invoke("myMethod"));
			Assertion.AssertEquals("hello", mock.Invoke("myMethod"));
			Assertion.AssertEquals("hello", mock.Invoke("myMethod"));
			Assertion.AssertEquals("world", mock.Invoke("myMethod"));
			Assertion.AssertEquals("world", mock.Invoke("myMethod"));
			Assertion.AssertEquals("hello", mock.Invoke("myMethod"));
			mock.SetupResultInOrder("myMethod", "bye");
			Assertion.AssertEquals("hello", mock.Invoke("myMethod"));
			Assertion.AssertEquals("world", mock.Invoke("myMethod"));
			Assertion.AssertEquals("world", mock.Invoke("myMethod"));
			Assertion.AssertEquals("bye", mock.Invoke("myMethod"));
			Assertion.AssertEquals("hello", mock.Invoke("myMethod"));

			// calling SetupResult resets return value
			mock.SetupResult("myMethod", null);
			Assertion.AssertNull(mock.Invoke("myMethod"));
			Assertion.AssertNull(mock.Invoke("myMethod"));
			mock.Verify();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that we can set different fixed return values that are returned based on
		/// input parameter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixedValueForParams()
		{
			mock.SetupResultForParams("myMethod", "hello", "abc");
			mock.SetupResultForParams("myMethod", "world", "xyz");
			Assertion.AssertEquals("hello", mock.Invoke("myMethod", "abc"));
			Assertion.AssertEquals("hello", mock.Invoke("myMethod", "abc"));
			Assertion.AssertEquals("world", mock.Invoke("myMethod", "xyz"));
			Assertion.AssertEquals("hello", mock.Invoke("myMethod", "abc"));
			mock.SetupResultForParams("myMethod", "bye", "jkl");
			Assertion.AssertEquals("world", mock.Invoke("myMethod", "xyz"));
			Assertion.AssertEquals("bye", mock.Invoke("myMethod", "jkl"));
			Assertion.AssertEquals("bye", mock.Invoke("myMethod", "jkl"));
			Assertion.AssertEquals("hello", mock.Invoke("myMethod", "abc"));

			Assertion.AssertNull(mock.Invoke("myMethod", "def"));
			Assertion.AssertNull(mock.Invoke("myMethod", "hij"));

			// calling SetupResult resets return value
			mock.SetupResult("myMethod", null);
			Assertion.AssertNull(mock.Invoke("myMethod", "xyz"));
			Assertion.AssertNull(mock.Invoke("myMethod", "abc"));
			mock.Verify();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that we can set different fixed return values that are returned based on
		/// input parameters if we have two methods with the same name but different number
		/// or kind of parameters
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixedValueForParamWithDifferentNumberOfParams()
		{
			mock.SetupResultForParams("myMethod", "hello", "abc");
			mock.SetupResultForParams("myMethod", "world", "abc", "xyz");
			mock.SetupResultForParams("myMethod", 99, 88);
			Assertion.AssertEquals("hello", mock.Invoke("myMethod", "abc"));
			Assertion.AssertEquals("world", mock.Invoke("myMethod", "abc", "xyz"));
			Assertion.AssertEquals(99, mock.Invoke("myMethod", 88));
			mock.Verify();
		}

		[Test] [ExpectedException(typeof(System.IO.IOException))] public void CallThrowingException()
		{
			mock.ExpectAndThrow("myMethod", new System.IO.IOException());
			mock.Invoke("myMethod");
		}

		[Test] [ExpectedException(typeof(VerifyException))] public void ExpectNoCall()
		{
			mock.ExpectNoCall("myMethod");
			mock.Invoke("myMethod");
		}

		// , Ignore("NoCall does not currently work, but requires a biggish overhaul to fix.  No time right now to do it")]
		[Test] public void ExpectNoCall_WithNoCall()
		{
			mock.ExpectNoCall("myMethod");
			mock.Verify();
		}

		[Test] [ExpectedException(typeof(VerifyException))] public void Strict()
		{
			mock.Strict = true;
			mock.Expect("x");
			mock.Expect("y");
			mock.Invoke("x");
			mock.Invoke("y");
			mock.Invoke("z");
		}

		[Test] public void UnexpectedMethodCallVerifyExceptionMessage()
		{
			try
			{
				mock.ExpectNoCall("x");
				mock.Invoke("x");
				Fail("Expected VerifyException");
			}
			catch (VerifyException e)
			{
				Assertion.AssertEquals("mymock.x() called", e.Reason);
				Assertion.AssertEquals(0, e.Expected);
				Assertion.AssertEquals(1, e.Actual);
			}
		}

		[Test] public void MethodNotCalledVerifyExpectionMessage()
		{
			try
			{
				mock.Expect("x");
				mock.Verify();
				Assertion.Fail("Expected VerifyException");
			}
			catch (VerifyException e)
			{
				Assertion.AssertEquals("mymock.x() never called", e.Reason);
				Assertion.AssertEquals(1, e.Expected);
				Assertion.AssertEquals(0, e.Actual);
			}
		}

		[Test] public void ExpectedOneCallGotTooManyMessage()
		{
			try
			{
				mock.Expect("x");
				mock.Invoke("x");
				mock.Invoke("x");
				mock.Verify();
				Assertion.Fail("Expected VerifyException");
			}
			catch (VerifyException e)
			{
				Assertion.AssertEquals("x() called too many times", e.Reason);
				Assertion.AssertEquals(1, e.Expected);
				Assertion.AssertEquals(2, e.Actual);
			}
		}

		[Test] public void ExpectedManyCallsGotNoneMessage()
		{
			try
			{
				mock.Expect("x");
				mock.Expect("x");
				mock.Verify();
				Assertion.Fail("Expected VerifyException");
			}
			catch (VerifyException e)
			{
				AssertEquals("mymock.x() never called", e.Reason);
				AssertEquals(2, e.Expected);
				AssertEquals(0, e.Actual);
			}
		}

		[Test] public void ExpectedManyCallsGotOneMessage()
		{
			try
			{
				mock.Expect("x");
				mock.Expect("x");
				mock.Invoke("x");
				mock.Verify();
				Assertion.Fail("Expected VerifyException");
			}
			catch (VerifyException e)
			{
				Assertion.AssertEquals("mymock.x() not called enough times", e.Reason);
				Assertion.AssertEquals(2, e.Expected);
				Assertion.AssertEquals(1, e.Actual);
			}
		}

		[Test] public void ExpectedManyCallsGotTooManyMessage()
		{
			try
			{
				mock.Expect("x");
				mock.Expect("x");
				mock.Invoke("x");
				mock.Invoke("x");
				mock.Invoke("x");
				mock.Verify();
				Assertion.Fail("Expected VerifyException");
			}
			catch (VerifyException e)
			{
				Assertion.AssertEquals("x() called too many times", e.Reason);
				Assertion.AssertEquals(2, e.Expected);
				Assertion.AssertEquals(3, e.Actual);
			}
		}

		[Test] public void ExpectedManyCallsGotTooManyMessageWithCount()
		{
			try
			{
				mock.Expect(2, "x");
				mock.Invoke("x");
				mock.Invoke("x");
				mock.Invoke("x");
				mock.Verify();
				Assertion.Fail("Expected VerifyException");
			}
			catch (VerifyException e)
			{
				Assertion.AssertEquals("x() called too many times", e.Reason);
				Assertion.AssertEquals(2, e.Expected);
				Assertion.AssertEquals(3, e.Actual);
			}
		}

		[Test] public void ExpectedManyCallsGotNotEnoughMessage()
		{
			try
			{
				mock.Expect("x");
				mock.Expect("x");
				mock.Expect("x");
				mock.Invoke("x");
				mock.Invoke("x");
				mock.Verify();
				Assertion.Fail("Expected VerifyException");
			}
			catch (VerifyException e)
			{
				Assertion.AssertEquals(3, e.Expected);
				Assertion.AssertEquals(2, e.Actual);
				Assertion.AssertEquals("mymock.x() not called enough times", e.Reason);
			}
		}

		[Test] public void ExpectedManyCallsGotNotEnoughMessageWithCount()
		{
			try
			{
				mock.Expect(3, "x");
				mock.Invoke("x");
				mock.Invoke("x");
				mock.Verify();
				Assertion.Fail("Expected VerifyException");
			}
			catch (VerifyException e)
			{
				Assertion.AssertEquals(3, e.Expected);
				Assertion.AssertEquals(2, e.Actual);
				Assertion.AssertEquals("mymock.x() not called enough times", e.Reason);
			}
		}
		[Test] public void IncorrectNumberOfParametersMessage()
		{
			try
			{
				mock.Expect("x", 1, 2, 3 );
				mock.Invoke("x", 2, 3 );
				Assertion.Fail("Expected VerifyException");
			}
			catch (VerifyException e)
			{
				Assertion.AssertEquals("x() called with incorrect number of parameters", e.Reason);
				Assertion.AssertEquals(3, e.Expected);
				Assertion.AssertEquals(2, e.Actual);
			}
		}

		[Test] public void IncorrectParameterConstraintMessage()
		{
			try
			{
				IMock Constraint = new DynamicMock(typeof(IConstraint));
				Constraint.SetupResult("Message", "wee woo");
				Constraint.SetupResult("Eval", false, typeof(object));

				mock.Expect("x", new IsAnything(), (IConstraint)Constraint.MockInstance);
				mock.Invoke("x", "hello", "world");
				Assertion.Fail("Expected VerifyException");
			}
			catch (VerifyException e)
			{
				Assertion.AssertEquals("x() called with incorrect parameter (2)", e.Reason);
				Assertion.AssertEquals("wee woo", e.Expected);
				Assertion.AssertEquals("world", e.Actual);
			}
		}
	}

	[TestFixture] public class MockCallTest
	{
		[Test] public void ArgTypes()
		{
			MockCall call = new MockCall(
				new MethodSignature("mymock", "call", new Type[] {typeof(int), typeof(string), typeof(bool[]) }),
				null, null,
				new object[] {1, "string",	new bool[] {true, false}});
			Assertion.AssertEquals("Parameter one", typeof(int).FullName, call.ArgTypes[0]);
			Assertion.AssertEquals("Parameter two", typeof(string).FullName, call.ArgTypes[1]);
			Assertion.AssertEquals("Parameter three", typeof(bool[]).FullName, call.ArgTypes[2]);
		}
	}
}
