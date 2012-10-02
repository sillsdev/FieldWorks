using System;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.FDO.Application
{
	/// <summary>
	/// Handles services common to DomainDataByFlid and DomainDataByFlidDecoratorBase
	/// to keep dependencies going the right way between SIL.FieldWorks.FDO.Application
	/// and SIL.FieldWorks.FDO.Application.Impl.
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