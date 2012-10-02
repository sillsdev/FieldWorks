using System;
using System.Runtime.InteropServices;   // for the class attributes
using System.Collections;
using Microsoft.Win32;                  // for RegistryKey
using ECInterfaces;                     // for ConvType

namespace SilEncConverters40
{
	/// <summary>
	/// Summary description for the FallbackEncConverter </summary>
	/// <remarks>
	/// This class implements the IEncConverter interface for a new kind of "compound"
	/// converter. The Fallback converter is used to all two steps to a converter the
	/// second of which will only be called if the first step fails to make a change to
	/// the input data. That is, the first step's "Convert" method is called and it makes
	/// no change to the data, then the 2nd step's Convert is called. This is useful for
	/// transliteration systems where the first step is the 'lexicon' approach (of special
	/// case transliterations) and the 'fallback' step is a blind approach using CC or
	/// TECkit (or some other transduction engine that works on characters rather than
	/// whole words).
	/// This class is derived from <see cref="CmpdEncConverter"/> since much of the code
	/// there can be reused.</remarks>
	[GuidAttribute("7056F809-22CB-4a9e-AF7A-597F3A9F0FD7")]
	// normally these subclasses are treated as the base class (i.e. the
	//  client can use them orthogonally as IEncConverter interface pointers
	//  so normally these individual subclasses would be invisible), but if
	//  we add 'ComVisible = false', then it doesn't get the registry
	//  'HKEY_CLASSES_ROOT\SilEncConverters40.TecEncConverter' which is the basis of
	//  how it is started (see EncConverters.AddEx).
	// [ComVisible(false)]
	public class FallbackEncConverter : CmpdEncConverter
	{
		#region Member Variable Definitions
		public new const string strDisplayName = "Primary-Fallback Converter";
		public new const string strHtmlFilename = "Fallback Converter Plug-in About box.mht";
		#endregion Member Variable Definitions

		#region Initialization
		public FallbackEncConverter() : base(typeof(FallbackEncConverter).FullName, EncConverters.strTypeSILfallback)
		{
		}
		#endregion Initialization

		#region Abstract Base Class Overrides
		internal virtual string AdjustConverterSpecProperty(string strMappingPri, bool bDirectionForwardPri, string strMappingFallback, bool bDirectionForwardFallback)
		{
			string strConverterSpec = FormatConverterStep(strMappingPri, bDirectionForwardPri, NormalizeFlags.None);
			strConverterSpec += ", with fallback to, ";
			strConverterSpec += FormatConverterStep(strMappingFallback, bDirectionForwardFallback, NormalizeFlags.None);
			return strConverterSpec;
		}

		// we override this method from EncConverter so that we can call all of the step's
		//  convert functions in turn (i.e. for this one, it isn't sufficient to just
		//  provide a "DoConvert" method)
		// and we override this from CmpdEncConverter to we can add our bit of only calling
		//  the 2nd step (i.e. the fallback converter) if the 1st step doesn't change the
		//  string.
		protected override string InternalConvertEx
			(
			EncodingForm    eInEncodingForm,
			string			sInput,
			int             ciInput,
			EncodingForm    eOutEncodingForm,
			NormalizeFlags  eNormalizeOutput,
			out int         rciOutput,
			bool            bForward
			)
		{
			if( CountConverters != 2 )
				EncConverters.ThrowError(ErrStatus.FallbackTwoStepsRequired);

			IEncConverter rConverter = (IEncConverter)m_aEncConverter[0];
			if (rConverter == null)
				EncConverters.ThrowError(ErrStatus.MissingConverter);

			rConverter.Debug = Debug;
			bool bDirectionForward = (bForward) ? (bool)m_aDirectionForward[0] : !(bool)m_aDirectionForward[0];

			string strOutput = rConverter.ConvertEx(
				sInput,
				eInEncodingForm,
				ciInput,
				eOutEncodingForm,
				out rciOutput,
				eNormalizeOutput,
				bDirectionForward);

			// call the fallback if the string wasn't changed
			if( strOutput == sInput )
			{
				IEncConverter rFallbackConverter = (IEncConverter)m_aEncConverter[1];
				if (rFallbackConverter == null)
					EncConverters.ThrowError(ErrStatus.MissingConverter);

				rFallbackConverter.Debug = Debug;
				bDirectionForward = (bForward) ? (bool)m_aDirectionForward[1] : !(bool)m_aDirectionForward[1];

				strOutput = rFallbackConverter.ConvertEx(
					sInput,
					eInEncodingForm,
					ciInput,
					eOutEncodingForm,
					out rciOutput,
					eNormalizeOutput,
					bDirectionForward);
			}

			return strOutput;
		}

		protected override string   GetConfigTypeName
		{
			get { return typeof(FallbackEncConverterConfig).AssemblyQualifiedName; }
		}
		#endregion Abstract Base Class Overrides
	}
}
