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
// File: CallMethodOrder.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;

namespace NMock
{
	/// <summary>
	/// Method that returns the results alternately without checking expecations.
	/// </summary>
	public class CallMethodOrder: Method
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructs a new object of type <see cref="CallMethodOrder"/>
		/// </summary>
		/// <param name="signature"></param>
		/// ------------------------------------------------------------------------------------
		public CallMethodOrder(MethodSignature signature)
			: base(signature)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override the Call method so that we can wrap around if called to often
		/// </summary>
		/// <param name="parameters"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override object Call(params object[] parameters)
		{
			MockCall mockCall = expectations[timesCalled];
			timesCalled++;
			if (timesCalled >= expectations.Count)
				timesCalled = 0;
			return mockCall.Call(signature.methodName, parameters);
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
