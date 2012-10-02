using System;

namespace NMock
{
	/// <summary>
	/// Interface for setting up and invoking a Mock object. The default implementation of
	/// this is <c>Mock</c> but users may choose to implement their own with custom needs.
	/// </summary>
	/// <see cref="Mock"/>
	public interface IMock : IInvocationHandler, IVerifiable
	{
		/// <summary>
		/// Name of this Mock - used for test failure readability only.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Get mocked version of object.
		/// </summary>
		object MockInstance { get; }

		/// <summary>
		/// If strict, any method called that doesn't have an expectation set
		/// will fail. (Defaults false)
		/// </summary>
		bool Strict { get; set; }

		/// <summary>
		/// Expect a method to be called with the supplied parameters.
		/// </summary>
		void Expect(string methodName, params object[] args);
		void Expect(string methodName, object[] args, string[] argTypes);
		void Expect(int nCount, string methodName, params object[] args);
		void Expect(int nCount, string methodName, object[] args, string[] argTypes);

		/// <summary>
		/// Expect no call to this method.
		/// </summary>
		void ExpectNoCall(string methodName, params Type[] argTypes);
		void ExpectNoCall(string methodName, string[] argTypes);

		/// <summary>
		/// Expect a method to be called with the supplied parameters and setup a
		/// value to be returned.
		/// </summary>
		void ExpectAndReturn(string methodName, object returnVal, params object[] args);
		void ExpectAndReturn(string methodName, object returnVal, object[] args, string[] argTypes, object[] outParams) ;
		void ExpectAndReturn(int nCount, string methodName, object returnVal, params object[] args);
		void ExpectAndReturn(int nCount, string methodName, object returnVal, object[] args, string[] argTypes, object[] outParams) ;

		/// <summary>
		/// Expect a method to be called with the supplied parameters and setup an
		/// exception to be thrown.
		/// </summary>
		void ExpectAndThrow(string methodName, Exception exceptionVal, params object[] args);

		/// <summary>
		/// Set a fixed return value for a method/property. This allows the method to be
		/// called multiple times in no particular sequence and have the same value returned
		/// each time. Useful for getter style methods.
		/// </summary>
		void SetupResult(string methodName, object returnVal, params Type[] argTypes);
		void SetupResult(string methodName, object returnVal, string[] inputTypes, object[] returnParams);

		/// <summary>
		/// Set a fixed return value for a method/property. Multiple return values can be set
		/// and will be returned alternately.
		/// </summary>
		void SetupResultInOrder(string methodName, object returnVal, params Type[] argTypes);
		void SetupResultInOrder(string methodName, object returnVal, string[] inputTypes, object[] returnParams);
		void SetupResultInOrder(int nCount, string methodName, object returnVal, params Type[] argTypes);
		void SetupResultInOrder(int nCount, string methodName, object returnVal, string[] inputTypes, object[] returnParams);

		/// <summary>
		/// Set a fixed return value depending on the input params.
		/// </summary>
		void SetupResultForParams(string methodName, object returnVal, params object[] args);
		void SetupResultForParams(string methodName, object returnVal, object[] args, string[] argTypes, object[] outParams) ;
	}
}
