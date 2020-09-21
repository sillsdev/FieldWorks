// Copyright (c) 2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.FwUtils;

namespace XCore
{
	public interface IPropertyRetriever
	{
		string GetStringProperty(string propertyName, string defaultValue);

		/// <summary>
		/// Test whether a property exists, tries local first and then global.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		bool PropertyExists(string name);

		/// <summary>
		/// Test whether a property exists in the specified group. Gives any value found.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="propertyValue">null, if it didn't find the property.</param>
		/// <returns></returns>
		bool TryGetValue<T>(string name, out T propertyValue);

		/// <summary>
		/// get the value of the best property (i.e. tries local first, then global).
		/// </summary>
		/// <param name="name"></param>
		/// <returns>returns null if the property is not found</returns>
		T GetValue<T>(string name);

		/// <summary>
		/// get the value of the best property (tries local then global),
		/// set the defaultValue if it doesn't exist. (creates global property)
		/// </summary>
		/// <param name="name"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		T GetValue<T>(string name, T defaultValue);

		/// <summary>
		/// Gets the boolean value of property in the best settings group (trying local first then global)
		/// and creates the (global) property with the default value if it doesn't exist yet.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		bool GetBoolProperty(string name, bool defaultValue);

		/// <summary>
		/// Gets the int value of property in the best settings group (trying local first then global)
		/// and creates the (global) property with the default value if it doesn't exist yet.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		int GetIntProperty(string name, int defaultValue);
	}
}