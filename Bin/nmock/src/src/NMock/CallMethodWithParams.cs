// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: CallMethodWithParams.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using NMock.Constraints;

namespace NMock
{
	/// <summary>
	/// Method that returns the value corresponding to the passed in parameters
	/// </summary>
	public class CallMethodWithParams: Method
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="CallMethodWithParams"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public CallMethodWithParams(MethodSignature signature)
			: base(signature)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override the Call method so that we can find the return value for the passed-in
		/// parameters.
		/// </summary>
		/// <param name="parameters"></param>
		/// <returns>The return value set up for the passed-in parameters; otherwise
		/// <c>null</c></returns>
		/// ------------------------------------------------------------------------------------
		public override object Call(params object[] parameters)
		{
			// loop over all the set up method calls
			for (int i = 0; i < expectations.Count; i++)
			{
				MockCall mockCall = expectations[i];
				if (parameters.Length != mockCall.ExpectedArgs.Length)
					continue;

				// Now compare all the parameters
				bool fFoundAllParams = true;
				for (int j = 0; j < parameters.Length; j++)
				{
					IConstraint constraint = mockCall.ExpectedArgs[j];
					fFoundAllParams = (fFoundAllParams && constraint.Eval(parameters[j]));
				}

				if (fFoundAllParams)
					return mockCall.Call(signature.methodName, parameters);
			}
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Since we don't want to check for met expectations we do nothing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void Verify()
		{
		}
	}
}
