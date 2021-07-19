// Copyright (c) 2016 SIL International
// SilOutlookBar is licensed under the MIT license.

using System;
using System.Reflection;

namespace SIL.SilSidePane
{
	/// <summary>
	/// Utilities to help with unit testing
	/// </summary>
	internal class TestUtilities
	{
		#region Tools
		public static OutlookBarButton GetUnderlyingButtonCorrespondingToTab(Tab tab)
		{
			return GetPrivatePropertyOfType<OutlookBarButton>(tab, "UnderlyingWidget");
		}

		/// <summary>
		/// Use by converting code that doesn't have proper access such as
		///     alpha._bravo.charlie(delta); // _bravo is an inaccessible private field
		/// to
		///     GetPrivateField(alpha,"_bravo").charlie(delta);
		/// </summary>
		/// <param name="obj">
		/// object which has a private field
		/// </param>
		/// <param name="fieldName">
		/// name of private field in object that you want to access
		/// </param>
		/// <returns>
		/// Value of private field. You will need to cast it.
		/// </returns>
		public static object GetPrivateField(object obj, string fieldName)
		{
			return obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(obj);
		}

		/// <see cref="GetPrivateField"/>
		public static object GetPrivateProperty(object obj, string fieldName)
		{
			return obj.GetType().GetProperty(fieldName, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(obj, null);
		}

		public static T GetPrivatePropertyOfType<T>(object obj, string fieldName)
		{
			return (T)obj.GetType().GetProperty(fieldName, BindingFlags.NonPublic | BindingFlags.Instance,
				null, typeof(T), new Type[0], null).GetValue(obj, null);
		}
		#endregion
	}
}
