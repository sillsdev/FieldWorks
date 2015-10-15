// Copyright c 2002, Joe Walnes, Chris Stevenson, Owen Rogers
// See LICENSE.txt for details.

using System;
using System.Collections;
using System.Reflection;
using NMock.Constraints;

namespace NMock
{
	public class Mock : IMock
	{
		private string name;
		private object instance;
		private bool strict;
		private IDictionary methods;

		public Mock(string name)
		{
			this.name = name;
			methods = new Hashtable();
		}

		#region Implementation of IMock
		public virtual string Name
		{
			get { return name; }
			set { name = value; }
		}

		public virtual object MockInstance
		{
			get { return instance; }
			set { instance = value; }
		}

		public virtual bool Strict
		{
			get { return strict; }
			set { strict = value; }
		}

		public virtual void Expect(string methodName, params object[] args)
		{
			ExpectAndReturn(methodName, null, args);
		}

		public virtual void Expect(string methodName, object[] args, string[] argTypes)
		{
			ExpectAndReturn(methodName, null, args, argTypes, null);
		}

		public virtual void Expect(int nCount, string methodName, params object[] args)
		{
			for (int i = 0; i < nCount; i++)
				ExpectAndReturn(methodName, null, args);
		}

		public virtual void Expect(int nCount, string methodName, object[] args, string[] argTypes)
		{
			for (int i = 0; i < nCount; i++)
				ExpectAndReturn(methodName, null, args, argTypes, null);
		}

		public virtual void ExpectNoCall(string methodName, params Type[] argTypes)
		{
			addExpectation(methodName, new MockNoCall(new MethodSignature(Name, methodName, argTypes)));
		}

		public virtual void ExpectNoCall(string methodName, string[] argTypes)
		{
			addExpectation(methodName, new MockNoCall(new MethodSignature(Name, methodName, argTypes)));
		}

		public virtual void ExpectAndReturn(string methodName, object result, params object[] args)
		{
			addExpectation(methodName,
				new MockCall(new MethodSignature(Name, methodName, MockCall.GetArgTypes(args)), result, null, args));
		}

		public virtual void ExpectAndReturn(string methodName, object returnVal, object[] args, string[] argTypes, object[] outParams)
		{
			addExpectation(methodName,
				new MockCall(new MethodSignature(Name, methodName, argTypes), returnVal, null, args, outParams));
		}
		public virtual void ExpectAndReturn(int nCount, string methodName, object result, params object[] args)
		{
			for (int i = 0; i < nCount; i++)
				addExpectation(methodName,
				new MockCall(new MethodSignature(Name, methodName, MockCall.GetArgTypes(args)), result, null, args));
		}

		public virtual void ExpectAndReturn(int nCount, string methodName, object returnVal, object[] args, string[] argTypes, object[] outParams)
		{
			for (int i = 0; i < nCount; i++)
				addExpectation(methodName,
				new MockCall(new MethodSignature(Name, methodName, argTypes), returnVal, null, args, outParams));
		}

		public virtual void ExpectAndThrow(string methodName, Exception e, params object[] args)
		{
			addExpectation(methodName,
				new MockCall(new MethodSignature(Name, methodName, MockCall.GetArgTypes(args)), null, e, args));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set a fixed return value. The same value will be returned everytime the method gets
		/// called.
		/// </summary>
		/// <param name="methodName">Name of the method/property</param>
		/// <param name="returnVal">Return value</param>
		/// <param name="argTypes">Types of the arguments. This is used to find the correct
		/// overwrite of the method.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void SetupResult(string methodName, object returnVal, params Type[] argTypes)
		{
			AddMethodWithoutExpecations(new MethodSignature(Name, methodName, argTypes),
				typeof(CallMethodWithoutExpectation), returnVal, null, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set a fixed return value. The same value will be returned everytime the method gets
		/// called.
		/// </summary>
		/// <param name="methodName">Name of the method/property</param>
		/// <param name="returnVal">Return value</param>
		/// <param name="inputTypes">Types of the arguments. This is used to find the correct
		/// overwrite of the method. NOTE: for out/ref parameters you have to add a & to the
		/// typename.</param>
		/// <param name="returnParams"><p>Values of the parameters that are set on return of the
		/// method. Set for out/ref parameters.</p>
		/// <p>NOTE: You have to specify ALL parameters! An <c>in</c> parameter you can
		/// simply set to <c>0/null</c>.</p></param>
		/// ------------------------------------------------------------------------------------
		public virtual void SetupResult(string methodName, object returnVal,
			string[] inputTypes, object[] returnParams)
		{
			AddMethodWithoutExpecations(new MethodSignature(Name, methodName, inputTypes),
				typeof(CallMethodWithoutExpectation), returnVal, returnParams, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a fixed return value. The return values set for this method will be returned
		/// in the order they are set up.
		/// </summary>
		/// <param name="methodName">Name of the method/property</param>
		/// <param name="returnVal">Return value</param>
		/// <param name="argTypes">Types of the arguments. This is used to find the correct
		/// overwrite of the method.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void SetupResultInOrder(string methodName, object returnVal,
			params Type[] argTypes)
		{
			AddMethodWithoutExpecations(new MethodSignature(Name, methodName, argTypes),
				typeof(CallMethodOrder), returnVal, null, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a fixed return value. The return values set for this method will be returned
		/// alternately.
		/// </summary>
		/// <param name="methodName">Name of the method/property</param>
		/// <param name="returnVal">Return value</param>
		/// <param name="inputTypes">Types of the arguments. This is used to find the correct
		/// overwrite of the method. NOTE: for out/ref parameters you have to add a & to the
		/// typename.</param>
		/// <param name="returnParams"><p>Values of the parameters that are set on return of the
		/// method. Set for out/ref parameters.</p>
		/// <p>NOTE: You have to specify ALL parameters! An <c>in</c> parameter you can
		/// simply set to <c>0/null</c>.</p></param>
		/// ------------------------------------------------------------------------------------
		public virtual void SetupResultInOrder(string methodName, object returnVal,
			string[] inputTypes, object[] returnParams)
		{
			AddMethodWithoutExpecations(new MethodSignature(Name, methodName, inputTypes),
				typeof(CallMethodOrder), returnVal, returnParams, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a fixed return value. The return values set for this method will be returned
		/// in the order they are set up.
		/// </summary>
		/// <param name="nCount">Number of times this return value should be returned.</param>
		/// <param name="methodName">Name of the method/property</param>
		/// <param name="returnVal">Return value</param>
		/// <param name="argTypes">Types of the arguments. This is used to find the correct
		/// overwrite of the method.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void SetupResultInOrder(int nCount, string methodName, object returnVal,
			params Type[] argTypes)
		{
			for (int i = 0; i < nCount; i++)
			{
				AddMethodWithoutExpecations(new MethodSignature(Name, methodName, argTypes),
					typeof(CallMethodOrder), returnVal, null, null);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a fixed return value. The return values set for this method will be returned
		/// alternately.
		/// </summary>
		/// <param name="nCount">Number of times this return value should be returned.</param>
		/// <param name="methodName">Name of the method/property</param>
		/// <param name="returnVal">Return value</param>
		/// <param name="inputTypes">Types of the arguments. This is used to find the correct
		/// overwrite of the method. NOTE: for out/ref parameters you have to add a & to the
		/// typename.</param>
		/// <param name="returnParams"><p>Values of the parameters that are set on return of the
		/// method. Set for out/ref parameters.</p>
		/// <p>NOTE: You have to specify ALL parameters! An <c>in</c> parameter you can
		/// simply set to <c>0/null</c>.</p></param>
		/// ------------------------------------------------------------------------------------
		public virtual void SetupResultInOrder(int nCount, string methodName, object returnVal,
			string[] inputTypes, object[] returnParams)
		{
			for (int i = 0; i < nCount; i++)
			{
				AddMethodWithoutExpecations(new MethodSignature(Name, methodName, inputTypes),
					typeof(CallMethodOrder), returnVal, returnParams, null);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a fixed return value for the passed in args.
		/// </summary>
		/// <param name="methodName">Name of the method/property</param>
		/// <param name="returnVal">Return value</param>
		/// <param name="args">Input parameters</param>
		/// ------------------------------------------------------------------------------------
		public virtual void SetupResultForParams(string methodName, object returnVal,
			params object[] args)
		{
			AddMethodWithoutExpecations(
				new MethodSignature(Name, methodName, MockCall.GetArgTypes(args)),
				typeof(CallMethodWithParams), returnVal, null, args);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a fixed return value for the passed in args.
		/// </summary>
		/// <param name="methodName">Name of the method/property</param>
		/// <param name="returnVal">Return value</param>
		/// <param name="args">Input parameters</param>
		/// <param name="argTypes">Types of the arguments. This is used to find the correct
		/// overwrite of the method. NOTE: for out/ref parameters you have to add a & to the
		/// typename.</param>
		/// <param name="returnParams"><p>Values of the parameters that are set on return of the
		/// method. Set for out/ref parameters.</p>
		/// <p>NOTE: You have to specify ALL parameters! An <c>in</c> parameter you can
		/// simply set to <c>0/null</c>.</p></param>
		/// ------------------------------------------------------------------------------------
		public virtual void SetupResultForParams(string methodName, object returnVal,
			object[] args, string[] argTypes, object[] returnParams)
		{
			AddMethodWithoutExpecations(new MethodSignature(Name, methodName, argTypes),
				typeof(CallMethodWithParams), returnVal, returnParams, args);
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a method without expectations
		/// </summary>
		/// <param name="signature">Signature of the method</param>
		/// <param name="newMethod">Method to add if none exists yet</param>
		/// <param name="returnVal">The value that should be returned when the method is called
		/// </param>
		/// <param name="returnParams">The values of the parameters set on return of the method
		/// </param>
		/// <param name="inParams">Input parameters</param>
		/// ------------------------------------------------------------------------------------
		private void AddMethodWithoutExpecations(MethodSignature signature, Type methodType,
			object returnVal, object[] returnParams, object[] inParams)
		{
			IMethod method = getMethod(signature);
			if (method == null || method.GetType() != methodType)
			{
				ConstructorInfo ctor = methodType.GetConstructor(
					new Type[] { typeof(MethodSignature) });
				method = (IMethod)ctor.Invoke(new object[] { signature });
				methods[signature.methodName] = method;
			}
			method.SetExpectation(new MockCall(signature, returnVal, null, inParams,
				returnParams));
		}

		protected void addExpectation(string methodName, MockCall call)
		{
			MethodSignature signature = new MethodSignature(Name, methodName, call.ArgTypes);
			IMethod method = getMethod(signature);
			if (method == null)
			{
				method = new Method(signature);
				methods[methodName] = method;
			}
			method.SetExpectation(call);
		}

		public virtual object Invoke(string methodName)
		{
			return Invoke(methodName, new object[0], new string[0]);
		}

		public virtual object Invoke(string methodName, params object[] args)
		{
			string[] typeNames = new string[args.Length];
			for (int i = 0; i < args.Length; i++)
			{
				if (args[i] == null)
					typeNames[i] = "System.Object";
				else
					typeNames[i] = args[i].GetType().FullName;
			}
			return Invoke(methodName, args, typeNames);
		}

		public virtual object Invoke(string methodName, object[] args, string[] typeNames)
		{
			if ((methodName.StartsWith("get_") && args.Length == 0) ||
				(methodName == "get_Item" && args.Length == 1))
			{
				methodName = methodName.Substring(4); // without get_
			}
			else if (methodName.StartsWith("set_") && args.Length == 1)
				methodName = methodName.Substring(4); // without set_

			MethodSignature signature = new MethodSignature(Name, methodName, typeNames);
			IMethod method = getMethod(signature);
			if (method == null)
			{
				if (strict)
				{
					throw new VerifyException(methodName + "() called too many times", 0, 1);
				}
				return null;
			}
			return method.Call(args);
		}

		public virtual void Verify()
		{
			foreach (IMethod method in methods.Values)
			{
				method.Verify();
			}
		}

		protected virtual IMethod getMethod(MethodSignature signature)
		{
			return (IMethod)methods[signature.methodName];
		}

		public class Assertion
		{
			public static void Assert(string message, bool expression)
			{
				if (!expression)
				{
					throw new VerifyException(message, null, null);
				}
			}

			public static void AssertEquals(string message, object expected, object actual)
			{
				if (!expected.Equals(actual))
				{
					throw new VerifyException(message, expected, actual);
				}
			}
		}
	}
}
