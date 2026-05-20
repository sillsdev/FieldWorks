using ECInterfaces;
using SilEncConverters40;

namespace ParatextImport
{
	/// <summary>
	/// Provides encoding converters for Paratext import without exposing call sites to
	/// direct Encoding Converters repository construction.
	/// </summary>
	public interface IEncodingConverterProvider
	{
		/// <summary>
		/// Gets the named converter from the underlying converter repository.
		/// </summary>
		IEncConverter GetConverter(string converterName);
	}

	internal class EncodingConverterProvider : IEncodingConverterProvider
	{
		private IEncConverters m_encConverters;

		internal EncodingConverterProvider()
		{
		}

		internal EncodingConverterProvider(IEncConverters encConverters)
		{
			m_encConverters = encConverters;
		}

		public IEncConverter GetConverter(string converterName)
		{
			EnsureEncConvertersInitialized();
			return m_encConverters[converterName];
		}

		private void EnsureEncConvertersInitialized()
		{
			if (m_encConverters != null)
				return;

			m_encConverters = new EncConverters();
		}
	}
}