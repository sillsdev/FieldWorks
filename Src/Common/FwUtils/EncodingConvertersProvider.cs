using System;
using System.Collections;
using System.Collections.Generic;
using ECInterfaces;
using SilEncConverters40;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Provides FieldWorks-owned access to the SIL Encoding Converters repository.
	/// </summary>
	public interface IEncodingConvertersProvider
	{
		/// <summary>
		/// Gets the underlying Encoding Converters repository.
		/// </summary>
		IEncConverters Converters { get; }

		/// <summary>
		/// Gets the concrete repository for legacy configuration surfaces that still require it.
		/// </summary>
		EncConverters ConcreteConverters { get; }

		/// <summary>
		/// Gets the converter names currently registered in the repository.
		/// </summary>
		ICollection Keys { get; }

		/// <summary>
		/// Gets the converter names as a typed sequence.
		/// </summary>
		IEnumerable<string> ConverterNames { get; }

		/// <summary>
		/// Gets the named converter from the repository.
		/// </summary>
		IEncConverter GetConverter(string converterName);

		/// <summary>
		/// Determines whether the repository contains the named converter.
		/// </summary>
		bool ContainsKey(string converterName);

		/// <summary>
		/// Clears the cached repository so the next access reloads converter registrations.
		/// </summary>
		void Reset();
	}

	/// <summary>
	/// Default implementation backed by <see cref="SilEncConverters40.EncConverters" />.
	/// </summary>
	public class EncodingConvertersProvider : IEncodingConvertersProvider
	{
		private static readonly IEncodingConvertersProvider s_default
			= new EncodingConvertersProvider();
		private readonly object m_syncRoot = new object();
		private IEncConverters m_encConverters;

		/// <summary>
		/// Gets the shared provider used by product code that does not need a test double.
		/// </summary>
		public static IEncodingConvertersProvider Default
		{
			get { return s_default; }
		}

		/// <summary>
		/// Initializes a provider that lazily creates the default repository.
		/// </summary>
		public EncodingConvertersProvider() { }

		/// <summary>
		/// Initializes a provider around an existing repository.
		/// </summary>
		public EncodingConvertersProvider(IEncConverters encConverters)
		{
			m_encConverters = encConverters;
		}

		/// <summary>
		/// Gets the underlying Encoding Converters repository.
		/// </summary>
		public IEncConverters Converters
		{
			get { return EnsureConvertersInitialized(); }
		}

		/// <summary>
		/// Gets the concrete repository for legacy configuration surfaces that still require it.
		/// </summary>
		public EncConverters ConcreteConverters
		{
			get
			{
				var concreteConverters = Converters as EncConverters;
				if (concreteConverters == null)
					throw new InvalidOperationException(
						"The current Encoding Converters repository is not a SilEncConverters40.EncConverters instance."
					);
				return concreteConverters;
			}
		}

		/// <summary>
		/// Gets the converter names currently registered in the repository.
		/// </summary>
		public ICollection Keys
		{
			get { return Converters.Keys; }
		}

		/// <summary>
		/// Gets the converter names as a typed sequence.
		/// </summary>
		public IEnumerable<string> ConverterNames
		{
			get { return Converters.GetConverterNames(); }
		}

		/// <summary>
		/// Gets the named converter from the repository.
		/// </summary>
		public IEncConverter GetConverter(string converterName)
		{
			return Converters[converterName];
		}

		/// <summary>
		/// Determines whether the repository contains the named converter.
		/// </summary>
		public bool ContainsKey(string converterName)
		{
			return Converters.ContainsKey(converterName);
		}

		/// <summary>
		/// Clears the cached repository so the next access reloads converter registrations.
		/// </summary>
		public void Reset()
		{
			lock (m_syncRoot)
			{
				m_encConverters = null;
			}
		}

		private IEncConverters EnsureConvertersInitialized()
		{
			if (m_encConverters != null)
				return m_encConverters;

			lock (m_syncRoot)
			{
				if (m_encConverters == null)
					m_encConverters = new EncConverters();
				return m_encConverters;
			}
		}
	}

	/// <summary>
	/// Helpers for repository operations that are not exposed directly on <see cref="IEncConverters" />.
	/// </summary>
	public static class EncodingConvertersExtensions
	{
		/// <summary>
		/// Determines whether the repository contains the named converter.
		/// </summary>
		public static bool ContainsKey(this IEncConverters encConverters, string converterName)
		{
			if (encConverters == null || string.IsNullOrEmpty(converterName))
				return false;

			foreach (string name in encConverters.GetConverterNames())
			{
				if (string.Equals(name, converterName, StringComparison.Ordinal))
					return true;
			}

			return false;
		}

		/// <summary>
		/// Gets converter names as strings from the repository keys collection.
		/// </summary>
		public static IEnumerable<string> GetConverterNames(this IEncConverters encConverters)
		{
			if (encConverters == null)
				yield break;

			var keys = encConverters.Keys;
			if (keys == null)
				yield break;

			foreach (object key in keys)
			{
				var name = key as string;
				if (name != null)
					yield return name;
			}
		}
	}
}