// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: IcuResourceBundle.cs
// Responsibility: Robin Munn

using System;
using System.Collections.Generic;
using System.Linq;
using SIL.ObjectModel;

namespace SIL.LCModel.Core.Text
{
	/// <summary>
	/// Represents an instance of an ICU ResourceBundle object.
	///
	/// <h3>When to use</h3>
	/// To retrieve information from ICU resources; this is useful for things like enumerating
	/// the countries and locales known to the system.
	///
	/// <h3>About "Null" resource bundles</h3>
	/// If a resource bundle is asked for a subsection that doesn't exist, instead of throwing
	/// an exception or returning null, it will return a "NullResourceBundle": an instance of
	/// IcuResourceBundle that will never throw an exception but always returns an "empty" value
	/// whenever queried. This will make client code much cleaner to write.
	/// </summary>
	public class IcuResourceBundle : DisposableBase
	{
		/// <summary>
		/// The opaque resource bundle pointer from ICU. If this is IntPtr.Zero, then this
		/// </summary>
		private IntPtr m_bundle;

		/// <summary>
		/// Initialize the root resource bundle. The path may be null to use the standard
		/// FieldWorks ICU data directory. The locale may be null to use the default locale.
		/// </summary>
		/// <param name='path'> </param>
		/// <param name='locale'> </param>
		public IcuResourceBundle(string path, string locale)
		{
			m_bundle = Icu.OpenResourceBundle(path, locale);
		}

		// In this version, bundlePtr has already been opened, but we are responsible for disposing it. (Or we are a "null" bundle if bundlePtr = IntPtr.Zero).
		private IcuResourceBundle(IntPtr bundlePtr)
		{
			m_bundle = bundlePtr;
		}

		private static IcuResourceBundle NullResourceBundle()
		{
			return new IcuResourceBundle(IntPtr.Zero);
		}

		/// <summary>
		/// Is this a "null" or "empty" bundle?
		/// </summary>
		public bool IsNullBundle { get { return m_bundle == IntPtr.Zero; } }

		/// <summary> Get the key of the bundle. (Icu getKey.) </summary>
		/// <returns>A System.String, which might be empty if this is an "empty" or "null" bundle, or if this is a root bundle.</returns>
		public string Key
		{
			get
			{
				if (this.IsNullBundle)
					return String.Empty;
				return Icu.GetResourceBundleKey(m_bundle);
			}
		}

		/// <summary>
		/// If this resource bundle is "just" a string, get that string. (Icu getString.)
		/// If you are using this property, consider refactoring to use GetStringByKey on the parent resource bundle instead.
		/// </summary>
		/// <returns>A System.String, which might be empty if this is an "empty" or "null" bundle, or if this is not a string-only bundle.</returns>
		public string String
		{
			get
			{
				if (this.IsNullBundle)
					return String.Empty;
				return Icu.GetResourceBundleString(m_bundle);
			}
		}

		/// <summary>
		/// Get the locale ID of the bundle. (Icu getLocale.)
		/// Note that the Key and String of the bundle are often more useful.
		/// </summary>
		/// <returns>A System.String, which might be empty if this is an "empty" or "null" bundle.</returns>
		public string LocaleId
		{
			get
			{
				if (this.IsNullBundle)
					return String.Empty;
				return Icu.GetResourceBundleLocaleId(m_bundle);
			}
		}

		/// <summary>
		/// Get a subsection of the current resource bundle, identified by a string key.
		/// </summary>
		/// <param name="sectionName">String name of the subsection to get</param>
		/// <returns>A new IcuResourceBundle instance representing the subsection. Disposing of it properly is the responsibility of the caller.</returns>
		public IcuResourceBundle GetSubsection(string sectionName)
		{
			if (this.IsNullBundle)
				return this;
			IntPtr bundlePtr = Icu.GetResourceBundleSubsection(m_bundle, sectionName);
			if (bundlePtr == IntPtr.Zero)
				return NullResourceBundle();  // Technically this isn't required, but let's be explicit about what we're returning here.
			return new IcuResourceBundle(bundlePtr);
		}

		/// <summary>Get a named string from the resource bundle</summary>
		/// <param name='key'>The name of the string to retrieve</param>
		/// <returns>The retrieved string (or throws IcuException if the string is not present)</returns>
		public string GetStringByKey(string key)
		{
			if (this.IsNullBundle)
				return String.Empty;
			return Icu.GetResourceBundleStringByKey(m_bundle, key);
		}

		/// <summary>
		/// Get all the strings this resource bundle contains, in a more C#-friendly way than the default C API provides.
		/// Any resources inside this bundle that were *not* strings (say, other bundles) will be skipped by this function.
		/// It will not recurse into "child" bundles, but only provide the strings that are direct children of this bundle.
		/// </summary>
		/// <returns>An IEnumerable providing all the string contents of this bundle.</returns>
		public IEnumerable<string> GetStringContents()
		{
			if (this.IsNullBundle)
				yield break;
			Icu.BeginResourceBundleIteration(m_bundle);
			string result;
			while ((result = Icu.GetNextStringInResourceBundleIteration(m_bundle)) != null)
			{
				yield return result;
			}
		}

		/// <summary>
		/// Get all the keyed strings this resource bundle contains, in a more C#-friendly way than the default C API provides.
		/// Any resources inside this bundle that were *not* strings (say, other bundles) will be skipped by this function, and
		/// any string contents that do *not* have keys will also be skipped. (If the bundle is a "table" -- a dictionary in C#
		/// terms -- then this function is appropriate. If the bundle is an array, then you want GetStringContents() instead.)
		/// It will not recurse into "child" bundles, but only provide the strings that are direct children of this bundle.
		/// </summary>
		/// <returns>An IEnumerable providing all the string contents of this bundle, along with the keys associated with those strings in the bundle.</returns>
		public IEnumerable<KeyValuePair<string, string>> GetStringContentsWithKeys()
		{
			if (this.IsNullBundle)
				yield break;
			Icu.BeginResourceBundleIteration(m_bundle);
			string key;
			string strValue;
			while ((strValue = Icu.GetNextStringInResourceBundleIteration(m_bundle, out key)) != null)
			{
				if (key != null)
					yield return new KeyValuePair<string, string>(key, strValue);
			}
		}

		/// <summary>
		/// Convert this resource bundle to a dictionary of its string contents with their associated keys.
		/// (This will ignore any nested resource bundles inside this one, and only return its contents that were strings).
		/// Note that the caller is still responsible for disposing the resource bundle even after converting it to a dictionary.
		/// As with GetStringContentsWithKeys(), strings in this bundle that do not have keys will be skipped. This function is
		/// appropriate for "table" bundles, but not for "array" bundles. (For "array" bundles, use GetStringContents()).
		/// </summary>
		/// <returns>
		/// A dictionary representation of this bundle, skipping any nested bundles and/or any non-keyed strings.
		/// This might be an empty dictionary if this was an "empty" or "null" bundle, or if it had no keyed string contents
		/// (i.e., if it was an "array" bundle whose contents had no keys, or if it contained only subsection bundles).
		/// </returns>
		public IDictionary<string, string> ToStringDictionary()
		{
			if (this.IsNullBundle)
				return new Dictionary<string, string>();  // Empty dictionary
			return GetStringContentsWithKeys().ToDictionary(kv => kv.Key, kv => kv.Value);
		}

		/// <summary>
		/// Dispose of the unmanaged ICU resource bundle when this class is being disposed.
		/// </summary>
		protected override void DisposeUnmanagedResources()
		{
			if (!this.IsNullBundle)
				Icu.CloseResourceBundle(m_bundle);
		}
	}
}
