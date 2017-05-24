// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.LCModel.Utils;

namespace SIL.LCModel.Application
{
	/// <summary>
	/// Handles services common to DomainDataByFlid and DomainDataByFlidDecoratorBase
	/// to keep dependencies going the right way between SIL.LCModel.Application
	/// and SIL.LCModel.Application.Impl.
	/// </summary>
	internal static class DomainDataByFlidServices
	{
		internal static int ComVecPropFromManagedVecProp(int[] hvos, int hvo, int tag, ArrayPtr rghvo, int chvoMax)
		{
			if (hvos.Length > chvoMax)
				throw new ArgumentException("The count is greater than the parameter 'chvo'.");

			MarshalEx.ArrayToNative(rghvo, chvoMax, hvos);
			return hvos.Length;
		}
	}
}