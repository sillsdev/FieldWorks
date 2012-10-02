using System;
using System.Runtime.InteropServices;   // for the class attributes
using System.Collections.Generic;       // for List<>
using Microsoft.Win32;                  // for RegistryKey
using ECInterfaces;                     // for IEncConverter

namespace SilEncConverters31
{
	/// <summary>
	/// Summary description for CmpdEncConverter.
	/// </summary>
	[GuidAttribute("4EFFB786-51C8-4651-9CB9-73748D891860")]
	// normally these subclasses are treated as the base class (i.e. the
	//  client can use them orthogonally as IEncConverter interface pointers
	//  so normally these individual subclasses would be invisible), but if
	//  we add 'ComVisible = false', then it doesn't get the registry
	//  'HKEY_CLASSES_ROOT\SilEncConverters31.TecEncConverter' which is the basis of
	//  how it is started (see EncConverters.AddEx).
	// [ComVisible(false)]
	public class CmpdEncConverter : EncConverter
	{
		#region Member Variable Definitions
		protected List<IEncConverter>   m_aEncConverter;      // ordered array of EncConverter objects to call.
		protected List<bool>            m_aDirectionForward;  // and of the forward direction flag
		protected List<NormalizeFlags>  m_aNormalizeOutput;   // and of the normalize output flag

		public const string strDisplayName = "Compound (daisy-chained) Converter";
		public const string strHtmlFilename = "Compound Converter Plug-in About box.mht";

		#endregion Member Variable Definitions

		#region Initialization
		public CmpdEncConverter() : base(typeof(CmpdEncConverter).FullName,EncConverters.strTypeSILcomp)
		{
			Init();
		}

		protected CmpdEncConverter(string strProgramID, string strImplType)
			: base(strProgramID,strImplType)
		{
			Init();
		}

		protected void Init()
		{
			m_aEncConverter = new List<IEncConverter>();
			m_aDirectionForward = new List<bool>();
			m_aNormalizeOutput = new List<NormalizeFlags>();

			// compound converters are, by definition, "in the repository"
			this.IsInRepository = true;
		}
		#endregion Initialization

		#region Implementation
		public  int CountConverters
		{
			get { return m_aEncConverter.Count; }
		}

		public virtual void AddConverterStep(IEncConverter rConvert, bool bDirectionForward, NormalizeFlags eNormalizeOutput)
		{
			m_aEncConverter.Add(rConvert);
			m_aDirectionForward.Add(bDirectionForward);
			m_aNormalizeOutput.Add(eNormalizeOutput);
		}

		protected override void GetConverterNameEnum(out string [] rSa)
		{
			rSa = new string[CountConverters];
			for(int i = 0; i < CountConverters; i++ )
			{
				IEncConverter rConvert = (IEncConverter)m_aEncConverter[i];
				if (rConvert == null)
					EncConverters.ThrowError(ErrStatus.MissingConverter);
				else
					rSa[i] = FormatConverterStep(rConvert.Name, (bool)m_aDirectionForward[i], (NormalizeFlags)m_aNormalizeOutput[i]);
			}
		}

		// this routine is just to create a converter spec for
		internal virtual string AdjustConverterSpecProperty(string strMapping, bool bDirectionForward, NormalizeFlags normalizeOutput)
		{
			string strPast = ConverterIdentifier;
			if( !String.IsNullOrEmpty(strPast) )
				strPast += " + \n";

			// put the mapping name for this step
			strPast += FormatConverterStep(strMapping, bDirectionForward, normalizeOutput);

			return strPast;
		}

		internal const string cstrDirectionReversed = " (reversed)";
		internal const string cstrNormalizationFullyComposed = " (Fully Composed)";
		internal const string cstrNormalizationFullyDecomposed = " (Fully Decomposed)";

		// this routine is just to create a converter spec for
		protected virtual string FormatConverterStep(string strMapping, bool bDirectionForward, NormalizeFlags normalizeOutput)
		{
			string str = strMapping;

			// indicate if it's 'reversed'
			if( !bDirectionForward )
				str += cstrDirectionReversed;

			// indicate if there's any special normalization
			switch(normalizeOutput)
			{
				case NormalizeFlags.FullyComposed:
				{
					str += cstrNormalizationFullyComposed;
					break;
				}
				case NormalizeFlags.FullyDecomposed:
				{
					str += cstrNormalizationFullyDecomposed;
					break;
				}
			}

			return str;
		}
		#endregion Implementation

		#region Abstract Base Class Overrides
		protected override string InternalConvert
			(
			EncodingForm    eInEncodingForm,
			string			sInput,
			EncodingForm    eOutEncodingForm,
			NormalizeFlags  eNormalizeOutput,
			bool            bForward
			)
		{
			// this routine is only called by one of the 'implicit' methods (e.g.
			//  ConvertToUnicode). For these "COM" standard methods, the length of the
			//  string is specified by the BSTR itself and always/only supports UTF-16-like
			//  (i.e. wide) data. So, pass 0 so that the function will determine the length
			//  from the BSTR itself (just in case the user happens to have a value of 0 in
			//  the data (i.e. it won't necessarily be null terminated... don't ask...
			Int32 iOutput = 0;
			return InternalConvertEx
				(
				eInEncodingForm,
				sInput,
				0,
				eOutEncodingForm,
				eNormalizeOutput,
				out iOutput,
				bForward
				);
		}

		// we override this method from EncConverter so that we can call all of the step's
		//  convert functions in turn (i.e. for this one, it isn't sufficient to just
		//  provide a "DoConvert" method)
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
			// setup common items for both directions.
			int nSize = m_aEncConverter.Count;
			string strOutput = null;
			EncodingForm inForm = eInEncodingForm;
			Int32 ciOutput = 0;
			IEncConverter rConverter;
			NormalizeFlags eNormalizeFlags;
			ConvType eConversionType;
			EncodingForm outForm;
			NormConversionType eType;
			bool bDirectionForward;
			int i;

			try
			{
				if( bForward )
				{
					for(i = 0; i < nSize; i++ )
					{
						rConverter = (IEncConverter)m_aEncConverter[i];
						if (rConverter == null)
							EncConverters.ThrowError(ErrStatus.MissingConverter);

						rConverter.Debug = Debug;
						eNormalizeFlags = (NormalizeFlags)m_aNormalizeOutput[i];
						if( i == (nSize-1) )
							eNormalizeFlags = eNormalizeOutput;

						eConversionType = rConverter.ConversionType;
						bDirectionForward = (bool)m_aDirectionForward[i];

						// if this is the last one, then use the user's requested output format
						if( i == (nSize-1) )
						{
							outForm = eOutEncodingForm;
						}
						else
						{
							if( bDirectionForward )
								eType = NormalizeRhsConversionType(eConversionType);
							else
								eType = NormalizeLhsConversionType(eConversionType);

							if( eType == NormConversionType.eLegacy)
								outForm = EncodingForm.LegacyBytes;
							else
								outForm = EncodingForm.Unspecified;
						}

						strOutput = rConverter.ConvertEx(
							sInput,
							inForm,
							ciInput,
							outForm,
							out ciOutput,
							eNormalizeFlags,
							bDirectionForward);

						// setup input for the next step
						sInput = strOutput;
						inForm = outForm;
						ciInput = ciOutput;

						// it's possible the user cancelled the debug mode so get it back
						Debug = rConverter.Debug;
					}
				}
				else    // reverse
				{
					for(i = nSize-1; i >= 0; i-- )
					{
						rConverter = (IEncConverter)m_aEncConverter[i];
						if (rConverter == null)
							EncConverters.ThrowError(ErrStatus.MissingConverter);

						rConverter.Debug = Debug;

						eNormalizeFlags = (NormalizeFlags)m_aNormalizeOutput[i];
						if( i == 0 )
							eNormalizeFlags = eNormalizeOutput;

						eConversionType = rConverter.ConversionType;

						// the direction is the opposite of what the user said in
						//  reverse mode.
						bDirectionForward = !(bool)m_aDirectionForward[i];

						// if this is the last one, then use the user's requested output format
						if( i == 0 )
							outForm = eOutEncodingForm;
						else
						{
							if( bDirectionForward )
								eType = NormalizeRhsConversionType(eConversionType);
							else
								eType = NormalizeRhsConversionType(eConversionType);

							if( eType == NormConversionType.eLegacy )
								outForm = EncodingForm.LegacyBytes;
							else
								outForm = EncodingForm.Unspecified;
						}

						strOutput = rConverter.ConvertEx(
							sInput,
							inForm,
							ciInput,
							outForm,
							out ciOutput,
							eNormalizeFlags,
							bDirectionForward);

						// setup input for the next step
						sInput = strOutput;
						inForm = outForm;
						ciInput = ciOutput;

						// it's possible the user cancelled the debug mode so get it back
						Debug = rConverter.Debug;
					}
				}
			}
			catch(ApplicationException e)
			{
				throw e;
			}

			rciOutput = ciOutput;
			return strOutput;
		}

		[CLSCompliant(false)]
		protected override unsafe void DoConvert
			(
			byte*       lpInBuffer,
			int         nInLen,
			byte*       lpOutBuffer,
			ref int     rnOutLen
			)
		{
			// this shouldn't be called since we override the InternalConvert(Ex) methods,
			//  but we've have to override this abstract base class member nevertheless.
			EncConverters.ThrowError(ErrStatus.Exception);
		}

		protected override string   GetConfigTypeName
		{
			get { return typeof(CmpdEncConverterConfig).AssemblyQualifiedName; }
		}

		#endregion Abstract Base Class Overrides
	}
}