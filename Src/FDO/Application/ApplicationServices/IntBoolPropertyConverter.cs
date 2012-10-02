using System;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.FDO.Application.ApplicationServices
{
	/// <summary>
	/// Used by SDA clients who can't tell an int from a bool in the model
	/// to set the right kind of model property.
	/// </summary>
	public static class IntBoolPropertyConverter
	{
		/// <summary>
		/// Set the given boolean to either a boolean or integer model property.
		/// </summary>
		public static void SetValueFromBoolean(ISilDataAccess sda, int hvo, int tag, bool newValue)
		{
			if (sda == null) throw new ArgumentNullException("sda");

			if ((CellarPropertyType)sda.MetaDataCache.GetFieldType(tag) == CellarPropertyType.Boolean)
				sda.SetBoolean(hvo, tag, newValue);
			else
				sda.SetInt(hvo, tag, newValue ? 1 : 0);
		}
		/// <summary>
		/// Get the boolean or integer model property as a boolean.
		/// </summary>
		/// <returns>
		/// The regular boolean value for boolean properties.
		/// For int model properties: 'false' for a '0' int value,
		/// or 'true' for all other int values.
		/// </returns>
		public static bool GetBoolean(ISilDataAccess sda, int hvo, int tag)
		{
			if (sda == null) throw new ArgumentNullException("sda");

			return (CellarPropertyType)sda.MetaDataCache.GetFieldType(tag) == CellarPropertyType.Boolean
					? sda.get_BooleanProp(hvo, tag)
					: sda.get_IntProp(hvo, tag) != 0;
		}
	}
}
