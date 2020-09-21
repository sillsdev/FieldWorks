// Copyright c 2002, Joe Walnes, Chris Stevenson, Owen Rogers
// See LICENSE.txt for details.

using System;
using NMock.Constraints;

namespace NMock
{
	public class MockCall
	{
		protected MethodSignature signature;
		private IConstraint[] expectedArgs;
		private object returnValue;
		private Exception e;
		private object[] returnArgs;

		public MockCall(MethodSignature signature, object returnValue, Exception e, object[] expectedArgs)
			: this(signature, returnValue, e, expectedArgs, null)
		{
		}

		public MockCall(MethodSignature signature, object returnValue, Exception e, object[] expectedArgs,
			object[] returnArgs)
		{
			this.signature = signature;
			this.returnValue = returnValue;
			this.e = e;
			this.expectedArgs = argsAsConstraints(expectedArgs);
			this.returnArgs = returnArgs;
		}

		public virtual bool HasExpectations
		{
			get { return expectedArgs.Length > 0; }
		}

		public string[] ArgTypes { get {return signature.argumentTypes; } }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the expected arguments
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IConstraint[] ExpectedArgs
		{
			get { return expectedArgs; }
		}

		private IConstraint[] argsAsConstraints(object[] args)
		{
			if (null == args)
			{
				return new IConstraint[0];
			}

			IConstraint[] result = new IConstraint[args.Length];
			for (int i = 0; i < result.Length; ++i)
			{
				result[i] = argAsConstraint(args[i]);
			}
			return result;
		}

		private IConstraint argAsConstraint(object arg)
		{
			IConstraint constraint = arg as IConstraint;
			if (constraint != null)
			{
				return constraint;
			}
			return arg == null ? (IConstraint)new IsAnything() : (IConstraint)new IsEqual(arg);
		}

		public virtual object Call(string methodName, object[] actualArgs)
		{
			checkArguments(methodName, actualArgs);

			if (e != null)
			{
				throw e;
			}

			if (returnArgs != null)
				returnArgs.CopyTo(actualArgs, 0);

			return returnValue;
		}

		private void checkArguments(string methodName, object[] actualArgs)
		{
			if ( HasExpectations )
			{
				Mock.Assertion.AssertEquals(methodName + "() called with incorrect number of parameters",
					expectedArgs.Length, actualArgs.Length);

				for (int i = 0; i < expectedArgs.Length; i++)
				{
					checkConstraint(methodName, expectedArgs[i], actualArgs[i], i);
				}
			}
		}

		private void checkConstraint(string methodName, IConstraint expected, object actual, int index)
		{
			if (!expected.Eval(actual))
			{
				String messageFormat = "{0}() called with incorrect parameter ({1})";
				String message = String.Format(messageFormat, methodName, index + 1);
				throw new VerifyException(message, expected.Message, actual);
			}
		}

		public static Type[] GetArgTypes(object[] args)
		{
			if (null == args)
			{
				return new Type[0];
			}

			Type[] result = new Type[args.Length];
			for (int i = 0; i < result.Length; ++i)
			{
				if (args[i] == null)
				{
					result[i] = typeof(object);
				}
				else
				{
					result[i] = args[i].GetType();
				}
			}

			return result;
		}
	}

	public class MockNoCall : MockCall
	{
		public MockNoCall(MethodSignature signature) : base(signature, null, null, null)
		{
		}

		public override object Call(string methodName, object[] actualArgs)
		{
			throw new VerifyException(signature.ToString() + " called", 0, 1);
		}
	}
}
