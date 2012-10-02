using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.ComponentModel;            // for description attributes

namespace ECInterfaces
{
	/// <summary>
	/// Definition of the IEncConverterConfig interface (for plug-ins to provide their own
	/// GUI for configuring themselves). With this, the client application can call
	/// EncConverters.AutoConfig and/or AutoSelect to acquire an existing converter
	/// or create a new one.
	/// </summary>
	#region Enum Definitions
	public enum ProcessTypeFlags
	{
		DontKnow = 0x0000,
		UnicodeEncodingConversion = 0x0001,   // don't use for legacy<>legacy encoding conversion
		Transliteration = 0x0002,
		ICUTransliteration = 0x0004,
		ICUConverter = 0x0008,
		CodePageConversion = 0x0010,
		NonUnicodeEncodingConversion = 0x0020,
		SpellingFixerProject = 0x0040,
		ICURegularExpression = 0x0080,
		PythonScript = 0x0100,
		PerlExpression = 0x0200,
		UserDefinedSpare1 = 0x0400,
		UserDefinedSpare2 = 0x0800
		// this can be added to by anyone (the enum is just for convenience), but should
		//  always represent a bit field. If you add to here, than start adding at
		//  UserDefinedSpare1 (i.e. move it down so that it always represents the first
		//  spare), and then be sure to update these two things:
		//      1)  the strProcessType implementation below (for ToString to display ProcessType
		//          details)
		//      2)  the ECAdvConfigDlg.cpp dialog (in project fw\lib\src\EC\CppEncConverterCommon)
		//          which is there to allow user-selection of process type
	};
	#endregion Enum Definitions

	#region IEncConverterConfig Interface Definition
	// It is imperative that if you make any changes to this interface, that you make the
	//  identical changes to the "ECEncConverter.h" file (e.g. in '...\src\EncCnvtrs\lib'),
	//  which has the implementation of this interface for use by C++ servers (and they must match!!)
	[System.Reflection.DefaultMember("ConfiguratorDisplayName")]
	public interface IEncConverterConfig
	{
		string ConfiguratorDisplayName
		{
			[return: MarshalAs(UnmanagedType.BStr)]
			get;
		}

		ProcessTypeFlags DefiningProcessType
		{
			get;
		}

		string ConverterFriendlyName
		{
			[return: MarshalAs(UnmanagedType.BStr)]
			get;
			set;
		}

		string ConverterIdentifier
		{
			[return: MarshalAs(UnmanagedType.BStr)]
			get;
			set;
		}

		string LeftEncodingID
		{
			[return: MarshalAs(UnmanagedType.BStr)]
			get;
			set;
		}

		string RightEncodingID
		{
			[return: MarshalAs(UnmanagedType.BStr)]
			get;
			set;
		}

		ConvType ConversionType
		{
			get;
			set;
		}

		Int32 ProcessType
		{
			[return: MarshalAs(UnmanagedType.I4)]
			get;
			set;
		}

		bool IsInRepository
		{
			[return: MarshalAs(UnmanagedType.VariantBool)]
			get;
			set;
		}

		IEncConverter ParentEncConverter
		{
			[return: MarshalAs(UnmanagedType.Interface)]
			get;
			set;
		}

		[return: MarshalAs(UnmanagedType.VariantBool)]
		bool Configure(
			[In, MarshalAs(UnmanagedType.Interface)] IEncConverters aECs,
			[In, MarshalAs(UnmanagedType.BStr)] string strFriendlyName,
			[In] ConvType eConversionType,
			[In, MarshalAs(UnmanagedType.BStr)] string strLhsEncodingID,
			[In, MarshalAs(UnmanagedType.BStr)] string strRhsEncodingID);

		void DisplayTestPage(
			[In, MarshalAs(UnmanagedType.Interface)] IEncConverters aECs,
			[In, MarshalAs(UnmanagedType.BStr)] string strFriendlyName,
			[In, MarshalAs(UnmanagedType.BStr)] string strConverterIdentifier,
			[In /* , MarshalAs(UnmanagedType.I4) */] ConvType eConversionType,
			[In, Optional, MarshalAs(UnmanagedType.BStr)] string strTestData);

		[return: MarshalAs(UnmanagedType.BStr)]
		string ToString();

		[return: MarshalAs(UnmanagedType.I4)]
		Int32 GetHashCode();

		[return: MarshalAs(UnmanagedType.VariantBool)]
		bool Equals([In, MarshalAs(UnmanagedType.Struct)] object rhs);
	}
	#endregion IEncConverterConfig Interface Definition

	/// <summary>
	/// Definition of IEncConverter(s) interfaces and associated Enums.
	/// </summary>
	#region IEncConverter Enum Definitions
	public enum ConvType
	{
		Unknown = 0,

		// bidirectional conversion types
		Legacy_to_from_Unicode = 1,
		Legacy_to_from_Legacy = 2,
		Unicode_to_from_Legacy = 3,
		Unicode_to_from_Unicode = 4,

		// unidirectional conversion
		Legacy_to_Unicode = 5,
		Legacy_to_Legacy = 6,
		Unicode_to_Legacy = 7,
		Unicode_to_Unicode = 8
	};

	public enum NormConversionType
	{
		eLegacy,
		eUnicode
	};

	public enum EncodingForm
	{
		Unspecified = 0,
		LegacyString = 1,
		UTF8String = 2,
		UTF16BE = 3,
		UTF16 = 4,
		UTF32BE = 5,
		UTF32 = 6,
		LegacyBytes = 20,	// something far enough away that TEC won't use it's value
		UTF8Bytes = 21
	};

	public enum NormalizeFlags
	{
		None = 0,
		FullyComposed = 0x0100,
		FullyDecomposed = 0x0200
	};
	#endregion IEncConverter Enum Definitions

	#region IEncConverter Interface Definition
	// It is imperative that if you make any changes to this interface (or even the Enum
	//  definitions above), that you make the identical changes to the "ECEncConverter.h"
	//  file (e.g. in '...\src\EncCnvtrs\lib'), which has the implementation of
	//  this interface for use by C++ servers (and they must match!!)
	/// <summary>
	/// Definition of the IEncConverter interface.</summary>
	/// <remarks>
	/// This is the definition of the COM interface that all transduction engines must
	/// implement in order to participate in the EncConverters scheme. Such implementations
	/// (known as "add-ins") can then be installed on a system that has the main EncConverters
	/// assembly (i.e. SilEncConverters30.dll) installed and they will automatically be available
	/// to the users of that machine.
	/// See the <see>EncConverter</see> class for a managed .Net base class implementation
	/// of this interface and the <see>CECEncConverter</see> class for an unmanaged C++/ATL
	/// base class implementation.</remarks>
	[System.Reflection.DefaultMember("Name")]
	public interface IEncConverter
	{
		/// <summary>
		/// Name property </summary>
		/// <value>
		/// This property is the user-friendly name of the converter, and also the key of the
		/// converter in the EncConverters collection. <seealso cref="EncConverters.Item"/></value>
		string Name
		{
			[return: MarshalAs(UnmanagedType.BStr)]
			get;
			set;
		}

		/// <summary>
		/// Initialize method </summary>
		/// <remarks>
		/// This method is called by EncConverters collection object to initialize a system converter
		/// when it is instantiated or added to the system repository. Client applications need not
		/// call this method when accessing converters from the EncConverters collection (i.e. via <seealso cref="EncConverters.Item"/>)
		/// But it should be called by any clients that directly instantiates an IEncConverter interface
		/// implementation class.</remarks>
		/// <param name="converterName"> user-friendly name (and unique key for the collection, <seealso cref="EncConverters.Item"/>) of a given converter instance</param>
		/// <seealso cref="String"/>
		/// <param name="ConverterIdentifier"> add-in specific identifier for the converter
		/// <remark>
		/// For file-based converters (e.g. TECkit, CC, etc), the identifier is typically the file specification to the
		/// table. For non-file-based converters, such as ICU, the identifier is a meaningful to the implementation
		/// program (e.g. "Devanagari-Latin" is the internal ICU name for a Devanagari transliterator)</remark></param>
		/// <param name="lhsEncodingID">The technical name of the left-hand side encoding (e.g. SIL-IPA93-2001)
		/// <remark>
		/// This is a reference parameter because some implementations can determine the encoding IDs themselves
		/// (e.g. TECkit reads the encoding ID from the TECkit map)</remark></param>
		/// <param name="rhsEncodingID">The technical name of the right-hand side encoding (e.g. UNICODE)
		/// <remark>
		/// This is a reference parameter because some implementations can determine the encoding IDs themselves
		/// (e.g. TECkit reads the encoding ID from the TECkit map)</remark></param>
		/// <param name="eConversionType">This parameter indicates the encoding type of the left and right-hand
		/// side of the conversion.
		/// <remark>
		/// An encoding conversion transducer is typically a Legacy_to(_from)_Unicode (since
		/// the input data is typically some legacy (i.e. non-Unicode) encoding and the output data is typically
		/// Unicode). A transliteration transducer is typically a Unicode_to(_from)_Unicode or Legacy_to(_from)_Legacy.
		/// These values are important for the IEncConverter interface to be able to correctly interpret the input
		/// and prepare the output data for the <see cref="Convert"/> method.
		/// This is also a reference parameter since some implementations already know what the type is (e.g.
		/// TECkit can determine the value from the TECkit map)</remark></param>
		/// <param name="processTypeFlags">This parameter indicates the process type of the converter.
		/// <remark>
		/// This parameter is a bit field parameter defined in the <see cref="ProcessTypeFlags"/> enumeration.
		/// This field can be used to indicate the type of conversion process the converter does (e.g.
		/// encoding conversion vs. transliteration, etc) and can be used for filtering by client applications.</remark></param>
		/// <param name="CodePageInput">This parameter indicates the code page to be used when converting input legacy
		/// data to/from Unicode. A value of 0 means to use the default system code page.</param>
		/// <param name="CodePageOutput">This parameter indicates the code page to be used when converting output legacy
		/// data to/from Unicode. A value of 0 means to use the default system code page.</param>
		/// <param name="bAdding">This parameter indicates whether the converter is being added for the first time.
		/// <remark>
		/// If true, this parameter causes the transducer engine to open the underlying engine and attempt to
		/// instantiate the converter (to check for syntax, etc). In the normal operation, when an EncConverters collection
		/// object is instantiated, it passes false for each converter in the repository so that such error checking
		/// isn't done (for performance purposes).</remark></param>
		void Initialize([In, MarshalAs(UnmanagedType.BStr)] string converterName,
			[In, MarshalAs(UnmanagedType.BStr)] string ConverterIdentifier,
			[In, Out, MarshalAs(UnmanagedType.BStr)] ref string lhsEncodingID,
			[In, Out, MarshalAs(UnmanagedType.BStr)] ref string rhsEncodingID,
			[In, Out /* ,MarshalAs(UnmanagedType.I4) */] ref ConvType eConversionType,
			[In, Out, MarshalAs(UnmanagedType.I4)] ref Int32 processTypeFlags,
			[In, MarshalAs(UnmanagedType.I4)] Int32 CodePageInput,
			[In, MarshalAs(UnmanagedType.I4)] Int32 CodePageOutput,
			[In, MarshalAs(UnmanagedType.VariantBool)] bool bAdding);

		/// <summary>
		/// ConverterIdentifier property </summary>
		/// <value>
		/// This property is the transducer-specific specification of the converter that is required to instantiate
		/// a converter. For file-based converters (e.g. TECkit, CC, etc), the identifier is typically the file
		/// specification to the table. For non-file-based converters, such as ICU, the identifier is a meaningful
		/// to the implementation program (e.g. "Devanagari-Latin" is the internal ICU name for a Devanagari
		/// transliterator)</value>
		string ConverterIdentifier
		{
			[return: MarshalAs(UnmanagedType.BStr)]
			get;
		}

		/// <summary>
		/// ProgramID property </summary>
		/// <value>
		/// This property is the program id that is used to instantiate the converter via COM. For example, the
		/// TECkit COM wrapper has a ProgramID of "SilEncConverters31.TecEncConverter" </value>
		string ProgramID
		{
			[return: MarshalAs(UnmanagedType.BStr)]
			get;
		}

		/// <summary>
		/// ImplementType property</summary>
		/// <value>
		/// This property is the internal name of a particular implementation of the IEncConverter interface. For example,
		/// the TECkit COM wrapper has the implementation type of "SIL.tec" </value>
		string ImplementType
		{
			[return: MarshalAs(UnmanagedType.BStr)]
			get;
		}

		/// <summary>
		/// ConversionType property </summary>
		/// <value>
		/// This parameter indicates the encoding type of the left and right-hand
		/// side of the conversion.
		/// An encoding conversion transducer is typically a Legacy_to(_from)_Unicode (since
		/// the input data is typically some legacy (i.e. non-Unicode) encoding and the output data is typically
		/// Unicode). A transliteration transducer is typically a Unicode_to(_from)_Unicode or Legacy_to(_from)_Legacy.
		/// These values are important for the IEncConverter interface to be able to correctly interpret the input
		/// and prepare the output data for the <see cref="Convert"/> method.
		/// This is also a reference parameter since some implementations already know what the type is (e.g.
		/// TECkit can determine the value from the TECkit map)</value>
		ConvType ConversionType
		{
			get;
		}

		/// <summary>
		/// ProcessType property </summary>
		/// <value>
		/// This parameter indicates the process type of the converter.
		/// This parameter is a bit field parameter defined in the <see cref="ProcessTypeFlags"/> enumeration.
		/// This field can be used to indicate the type of conversion process the converter does (e.g.
		/// encoding conversion vs. transliteration, etc) and can be used for filtering by client applications.</value>
		Int32 ProcessType
		{
			[return: MarshalAs(UnmanagedType.I4)]
			get;
		}

		/// <summary>
		/// CodePageInput property</summary>
		/// <value>
		/// This parameter indicates the code page to be used when converting input legacy
		/// data to/from Unicode. A value of 0 means to use the default system code page.</value>
		Int32 CodePageInput
		{
			[return: MarshalAs(UnmanagedType.I4)]
			get;
			set;
		}

		/// <summary>
		/// CodePageOutput property</summary>
		/// <value>
		/// This parameter indicates the code page to be used when converting output legacy
		/// data to/from Unicode. A value of 0 means to use the default system code page.</value>
		Int32 CodePageOutput
		{
			[return: MarshalAs(UnmanagedType.I4)]
			get;
			set;
		}

		/// <summary>
		/// LeftEncodingID property </summary>
		/// <value>
		/// The technical name of the left-hand side encoding (e.g. SIL-IPA93-2001) </value>
		string LeftEncodingID
		{
			[return: MarshalAs(UnmanagedType.BStr)]
			get;
		}

		/// <summary>
		/// RightEncodingID property </summary>
		/// <value>
		/// The technical name of the right-hand side encoding (e.g. SIL-IPA93-2001) </value>
		string RightEncodingID
		{
			[return: MarshalAs(UnmanagedType.BStr)]
			get;
			set;
		}

		/// <summary>
		/// Configurator property </summary>
		/// <value>
		/// This property returns an instance of the <see cref="IEncConverterConfig"/> interface that can be used
		/// to configure an instance of the given IEncConverter implementation type. </value>
		IEncConverterConfig Configurator
		{
			get;
		}

		/// <summary>
		/// IsInRepository </summary>
		/// <value>
		/// Indicates whether the given IEncConverter instance is in the static XML repository or whether it is a
		/// temporary converter. </value>
		bool IsInRepository
		{
			[return: MarshalAs(UnmanagedType.VariantBool)]
			get;
			set;
		}

		/// <summary>
		/// ConvertToUnicode method </summary>
		/// <param name="baInput">a byte array <seealso cref="byte []"/> of Legacy encoded bytes to be converted to
		/// Unicode by the converter
		/// <remarks>
		/// This method is useful if you have byte-wide (i.e. narrow) data.</remarks></param>
		/// <returns>
		/// The Unicode-encoded string which is the result of the conversion </returns>
		[return: MarshalAs(UnmanagedType.BStr)]
		string ConvertToUnicode([In, MarshalAs(UnmanagedType.SafeArray,
										SafeArraySubType = VarEnum.VT_UI1)] byte[] baInput);

		/// <summary>
		/// ConvertFromUnicode method </summary>
		/// <param name="sInput">a Unicode-encoded string to be converted to a Legacy-encoding</param>
		/// <remark>
		/// This method is useful if you need a byte-wide (i.e. narrow) array of data (e.g. for File I/O) </remark>
		/// <returns>The legacy-encoded byte array <seealso cref="byte []"/> which is the result of the
		/// conversion </returns>
		[return: MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_UI1)]
		byte[] ConvertFromUnicode([In, MarshalAs(UnmanagedType.BStr)] string sInput);

		/// <summary>
		/// Convert method </summary>
		/// <param name="sInput">a 'wide' string of either Legacy data (widen using some code page) or UTF-16 </param>
		/// <remarks>
		/// This method is the suggested method for text conversion when your data is in a <seealso cref="String"/>
		/// object in .Net or a BSTR (or CStringW) in C++). </remarks>
		/// <example>
		///     EncConverters aECs = new EncConverters();
		///     IEncConverter aEC = aECs[strFriendlyName];
		///
		///     string strInput = "some Input data";
		///     string strOutput = aEC.Convert(strInput);
		///     MessageBox.Show(String.Format("Got '{0}' by converting {1}", strOutput, strInput));
		/// </example>
		/// <returns>The 'wide' string which is the result of the conversion </returns>
		[return: MarshalAs(UnmanagedType.BStr)]
		string Convert([In, MarshalAs(UnmanagedType.BStr)] string sInput);

		/// <summary>
		/// ConvertEx method </summary>
		/// <remarks>This method is for use primarily by non-scripting client applications to specify
		/// the conversion details in a single function (depreciated). </remarks>
		[return: MarshalAs(UnmanagedType.BStr)]
		string ConvertEx([In, MarshalAs(UnmanagedType.BStr)] string sInput,
			[In, Optional, DefaultValue(EncodingForm.Unspecified) /* , MarshalAs(UnmanagedType.I4) */] EncodingForm inEnc,
			[In, Optional, DefaultValue(0), MarshalAs(UnmanagedType.I4)] int ciInput,
			[In, Optional, DefaultValue(EncodingForm.Unspecified) /* , MarshalAs(UnmanagedType.I4) */] EncodingForm outEnc,
			[Out, Optional, DefaultValue(0), MarshalAs(UnmanagedType.I4)] out int ciOutput,
			[In, Optional, DefaultValue(NormalizeFlags.None) /* , MarshalAs(UnmanagedType.I4) */] NormalizeFlags eNormalizeOutput,
			[In, Optional, DefaultValue(true), MarshalAs(UnmanagedType.VariantBool)] bool bForward);

		/// <summary>
		/// DirectionForward property </summary>
		/// <value>
		/// This property indicates the direction of the conversion for bi-directional converters ('true' means reverse)</value>
		bool DirectionForward
		{
			[return: MarshalAs(UnmanagedType.VariantBool)]
			get;
			set;
		}

		/// <summary>
		/// EncodingIn property </summary>
		/// <value>
		/// This property is used to indicate a non-default value for the encoding form of the input data. Normally,
		/// the encoding form is determined by the <see cref="ConversionType"/> field: If the input is a 'Legacy'
		/// encoding, then the EncodingIn value is 'LegacyString' (which implies a 'wide' string of non-Unicode encoded
		/// data, widened by the code page given by the <see cref="CodePageInput"/> property). If the input is a 'Unicode' encoding, then the
		/// EncodingIn value is UTF-16.</value>
		EncodingForm EncodingIn
		{
			get;
			set;
		}

		/// <summary>
		/// EncodingIn property </summary>
		/// <value>
		/// This property is used to indicate a non-default value for the encoding form to be used on the returned data.
		/// Normally, the encoding form is determined by the <see cref="ConversionType"/> field: If the output is a
		/// 'Legacy' encoding, then the EncodingOut value is 'LegacyString' (which implies a 'wide' string of non-Unicode
		/// encoded data, widened by the code page given by the <see cref="CodePageOutput"/> property). If the input is
		/// a 'Unicode' encoding, then the EncodingOut value is UTF-16.</value>
		EncodingForm EncodingOut
		{
			get;
			set;
		}

		/// <summary>
		/// Debug property </summary>
		/// <value>
		/// This property can be set to have the IEncConverter object display debug information during the Convert*
		/// method execution. The debug information shows:
		///   1) the string of data received from the client,
		///   2) the string of data sent to the transducer engine,
		///   3) the string of data received back from the transducer engine, and
		///   4) the string of data returned back to the client. </value>
		bool Debug
		{
			get;
			set;
		}

		/// <summary>
		/// NormalizeOutput property </summary>
		/// <value>
		/// This property can be used to request that the data returned via one of the Convert* methods (if Unicode) be
		/// returned with a particular normalization value as defined in the NormalizeFlags enumerations (i.e.
		/// Composed, Decomposed, or None) </value>
		NormalizeFlags NormalizeOutput
		{
			get;
			set;
		}

		/// <summary>
		/// AttributeKeys property </summary>
		/// <value>
		/// This property returns a string array <seealso cref="string []"/> of the keys for the attributes of the
		/// given IEncConverter instance. Client applications would normally use the <seealso cref="EncConverters.Attributes"/>
		/// method to get a Hashtable of attributes. </value>
		string[] AttributeKeys
		{
			[return: MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)]
			get;
		}

		/// <summary>
		/// AttributeValue method </summary>
		/// <param name="sKey">The key of the attribute to be returned (must be one of the <seealso cref="AttributeKeys"/> strings. </param>
		/// <returns>The string value of the attribute associated with the given key. </returns>
		[return: MarshalAs(UnmanagedType.BStr)]
		string AttributeValue(string sKey);

		/// <summary>
		/// ConverterNameEnum property </summary>
		/// <value>
		/// Certain IEncConverter implementations have the ability to provide an enumeration of the converter IDs
		/// <see cref="ConverterIdentifier"/> that they support (e.g. ICU transliterators, converters). Not all
		/// IEncConverter implementors (need to) implement this property. </value>
		string[] ConverterNameEnum
		{
			[return: MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)]
			get;
		}

		/// <summary>
		/// ToString method </summary>
		/// <remarks>
		/// This method returns a user-displayable string indicating the information about this converter (typically
		/// for ToolTips).</remarks>
		/// <returns></returns>
		[return: MarshalAs(UnmanagedType.BStr)]
		string ToString();

		/// <summary>
		/// Gets the hash code of the current IEncConverter instance </summary>
		/// <returns>The hash code of the current IEncConverter instance </returns>
		[return: MarshalAs(UnmanagedType.I4)]
		Int32 GetHashCode();

		/// <summary>
		/// Equals method </summary>
		/// <remarks>
		/// Determines whether two IEncConverter instances are equal.</remarks>
		/// <param name="rhs">the IEncConverter instance to compare with 'this'</param>
		/// <returns>true if the specified IEncConverter is equal to the current IEncConverter; otherwise, false.</returns>
		[return: MarshalAs(UnmanagedType.VariantBool)]
		bool Equals([In, MarshalAs(UnmanagedType.Struct)] object rhs);
	}
	#endregion IEncConverter Interface Definition

	/// <summary>
	/// Encoding Conversion Repository IEncConverters interface
	/// </summary>
	// Enumeration of repository items that support ECAttributes.
	#region IEncConverters Enum Definitions
	public enum AttributeType
	{
		Converter = 0,    // for a 'map' entry (i.e. an actual converter)
		EncodingID = 1,    // for an 'encodingID' entry
		FontName = 2     // for a 'font' node
	};

	public enum ErrStatus
	{
		NoError = 0,	// this is usually the desired result!

		// positive values are informational status values
		OutputBufferFull = 1,	// ConvertBuffer or Flush: output buffer full, so not all input was processed
		NeedMoreInput = 2,      // ConvertBuffer: processed all input data, ready for next chunk

		// negative values are errors
		InvalidForm = -1,				// inForm or outForm parameter doesn't match mapping (bytes/Unicode mismatch)
		ConverterBusy = -2,				// can't initiate a conversion, as the converter is already in the midst of an operation
		InvalidConverter = -3,			// converter object is corrupted (or not really a TECkit_Converter at all)
		InvalidMapping = -4,			// compiled mapping data is not recognizable
		BadMappingVersion = -5,			// compiled mapping is not a version we can handle
		Exception = -6,					// an internal error has occurred
		NameNotFound = -7,				// couldn't find the requested name: '{0}'
		IncompleteChar = -8,			// bad input data (lone surrogate, incomplete UTF8 sequence)
		CompilationFailed = -9,			// mapping compilation failed (syntax errors, etc)
		OutOfMemory = -10,				// unable to allocate required memory
		CantOpenReadMap = -11,			// unable to open or read the map file '{0}'
		InEncFormNotSupported = -12,	// the input encoding format is not supported
		OutEncFormNotSupported = -13,	// the output encoding format is not supported
		NoAvailableConverters = -14,	// No installed converter supports the given ConverterIdentifier's extension
		SyntaxErrorInTable = -15,		// syntax error in the CC table
		NoErrorCode = -16,				// Oops, this error is not being handled: (%d); contact the vendor
		NotEnoughBuffer = -17,			// Not a large enough buffer was provided; contact the vendor
		RegistryCorrupt = -18,			// The registry appears to be corrupted. You should reinstall or use the repository interface to add converters
		MissingConverter = -19,			// Some converter in the sequence is missing.
		NoConverter = -20,				// No converter exists with the given mappingName
		InvalidConversionType = -21,	// The requested conversion is invalid for this converter
		EncodingConvTypeNotSpecified = -22,	// Either the ConversionType or the EncodingForm must be specified
		ConverterPluginUninstall = -23, // This converter is a plug-in and must be uninstalled rather than removed.
		InvalidCharFound = -24,			// No mapping was found from the source to the target encoding
		TruncatedCharFound = -25,		// All of the source data was read, and a character sequence was incomplete
		IllegalCharFound = -26,			// A character sequence was found in the source which is disallowed in the source encoding scheme
		InvalidTableFormat = -27,		// An error occurred trying to read the backing data for the converter
		NoReturnData = -28,				// The converter DLL returned no data! Is the table/map correct?
		NoReturnDataBadOutForm = -29,	// No data to return! Perhaps bad output encoding form.
		AddFontFirst = -30,             // No font name entry '{0}'! You must call the 'AddFont' method first before calling this method
		InvalidNormalizeForm = -31,     // Invalid NormalizeFlags form
		NoAliasName = -32,              // Alias name does not exist
		ConverterAlreadyExists = -33,   // A converter by this name already exists
		NoImplementDetails = -34,       // Some implementation details are missing
		NoEncodingName = -35,           // No encoding exists with the given encodingName
		NeedSpecTypeInfo = -36,         // There are multiple implementations of this mapping, you must specify which one's properties you want (see BuildConverterSpecName)
		InvalidAliasName = -37,         // Alias name already exists
		FallbackTwoStepsRequired = -38, // Fallback converters require two steps
		FallbackSimilarConvType = -39,  // Fallback converters must have a similar ConvType as the main converters
		InvalidMappingName = -40,       // Invalid character in mapping name (e.g. ';')
		InstallFont = -41               // The font '{0}' doesn't appear to be installed!
	};
	#endregion IEncConverters Enum Definitions

	#region IEncConverters Interface Definition
	[InterfaceTypeAttribute(ComInterfaceType.InterfaceIsDual)]  // for VBA compliance
	public interface IEncConverters
	{
		IEncConverter this[[In, MarshalAs(UnmanagedType.Struct)] object mapName]
		{
			get;
		}

		void Add([In, MarshalAs(UnmanagedType.BStr)] string mappingName,
			[In, MarshalAs(UnmanagedType.BStr)] string converterSpec,
			[In /* ,MarshalAs(UnmanagedType.U4) */] ConvType conversionType,
			[In, Optional, MarshalAs(UnmanagedType.BStr)] string leftEncoding,
			[In, Optional, MarshalAs(UnmanagedType.BStr)] string rightEncoding,
			[In, Optional, DefaultValue(0) /* ,MarshalAs(UnmanagedType.I4) */] ProcessTypeFlags processType);

		void AddConversionMap([In, MarshalAs(UnmanagedType.BStr)] string mappingName,
			[In, MarshalAs(UnmanagedType.BStr)] string converterSpec,
			[In /* ,MarshalAs(UnmanagedType.U4) */] ConvType conversionType,
			[In, MarshalAs(UnmanagedType.BStr)] string implementType,
			[In, Optional, MarshalAs(UnmanagedType.BStr)] string leftEncoding,
			[In, Optional, MarshalAs(UnmanagedType.BStr)] string rightEncoding,
			[In, Optional, DefaultValue(0) /* ,MarshalAs(UnmanagedType.I4) */] ProcessTypeFlags processType);

		void AddImplementation([In, MarshalAs(UnmanagedType.BStr)] string platform,
			[In, MarshalAs(UnmanagedType.BStr)] string type,
			[In, MarshalAs(UnmanagedType.BStr)] string use,
			[In, MarshalAs(UnmanagedType.BStr)] string param,
			[In, MarshalAs(UnmanagedType.I4)] int priority);

		void AddAlias([In, MarshalAs(UnmanagedType.BStr)] string encodingName,
			[In, MarshalAs(UnmanagedType.BStr)] string alias);

		void AddCompoundConverterStep(
			[In, MarshalAs(UnmanagedType.BStr)] string compoundMappingName,
			[In, MarshalAs(UnmanagedType.BStr)] string converterStepMapName,
			[In, Optional, DefaultValue(-1), MarshalAs(UnmanagedType.VariantBool)] bool directionForward,
			[In, Optional, DefaultValue(NormalizeFlags.None) /* ,MarshalAs(UnmanagedType.U4) */] NormalizeFlags normalizeOutput);

		void AddFont(
			[In, MarshalAs(UnmanagedType.BStr)] string fontName,
			[In, MarshalAs(UnmanagedType.I4)] Int32 CodePage,
			[In, Optional, MarshalAs(UnmanagedType.BStr)] string defineEncoding);

		void AddUnicodeFontEncoding([In, MarshalAs(UnmanagedType.BStr)] string fontName,
			[In, MarshalAs(UnmanagedType.BStr)] string unicodeEncoding);

		void AddFontMapping([In, MarshalAs(UnmanagedType.BStr)] string mappingName,
			[In, MarshalAs(UnmanagedType.BStr)] string fontName,
			[In, MarshalAs(UnmanagedType.BStr)] string assocFontName);

		void AddFallbackConverter(
			[In, MarshalAs(UnmanagedType.BStr)] string strMappingName,
			[In, MarshalAs(UnmanagedType.BStr)] string strMappingNamePrimaryStep,
			[In, Optional, DefaultValue(-1), MarshalAs(UnmanagedType.VariantBool)] bool bDirectionForwardPrimary,
			[In, MarshalAs(UnmanagedType.BStr)] string strMappingNameFallbackStep,
			[In, Optional, DefaultValue(-1), MarshalAs(UnmanagedType.VariantBool)] bool bDirectionForwardFallback);

		void Remove([In, MarshalAs(UnmanagedType.Struct)] object mapName);

		void RemoveNonPersist([In, MarshalAs(UnmanagedType.Struct)] object mapName);

		void RemoveAlias([In, MarshalAs(UnmanagedType.BStr)] string alias);

		void RemoveImplementation([In, MarshalAs(UnmanagedType.BStr)] string platform,
			[In, MarshalAs(UnmanagedType.BStr)] string type);

		int Count
		{
			[return: MarshalAs(UnmanagedType.I4)]
			get;
		}

		// the following three methods are the main querying methods for getting one or
		//  more converters based on the process type, encodingID, or font name. They will
		//  always only return:
		//  a)  the highest priority converter of a multi-spec mapping,
		//  b)  a mapping that is supported by an IEncConverter COM wrapper (i.e. if a
		//      mapping's only implementation is a type that *doesn't* have a COM wrapper
		//      which implements the IEncConverter interface, then it won't be:
		//      1)  in the *this* collection (i.e. the hashtable from which EncConverters
		//          derives or
		//      2)  in the EncConverters collection return by the following three methods.
		//  if you want to iterate mappings which are in the repository *regardless* of
		//  whether they are supported by EncConverters, then use the "Mappings",
		//  "Encodings", "Fonts" properties below.
		IEncConverters ByProcessType([In, Optional /* ,MarshalAs(UnmanagedType.I4) */] ProcessTypeFlags processType);

		IEncConverters ByFontName([In, MarshalAs(UnmanagedType.BStr)] string fontName,
			[In, Optional /* ,MarshalAs(UnmanagedType.I4) */] ProcessTypeFlags processType);

		IEncConverters ByEncodingID([In, MarshalAs(UnmanagedType.BStr)] string encoding,
			[In, Optional /*,MarshalAs(UnmanagedType.I4) */] ProcessTypeFlags processType);

		[return: MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)]
		string[] EnumByProcessType([In, Optional, DefaultValue(0) /* ,MarshalAs(UnmanagedType.I4) */ ] ProcessTypeFlags processType);

		ECAttributes Attributes([In, MarshalAs(UnmanagedType.BStr)] string sItem,
			[In, Optional, DefaultValue(AttributeType.Converter) /* ,MarshalAs(UnmanagedType.I4) */] AttributeType repositoryItem);

		void AddAttribute(ECAttributes aECAttributes, object Key, object Value);
		void RemoveAttribute(ECAttributes aECAttributes, object Key);

		// the following properties can be used by clients wishing to know what converters
		//  are in the XML repository even if they aren't supported by EncConverters (if they
		//  are supported by EncConverters (e.g. TECkit, CC, ICU, etc), then it's easier to
		//  use the 'By*' methods (i.e. ByEncodingID, ByFontName, ByProcessType, etc.)
		// If they *aren't* supported by EncConverters, then they won't be in the *this*
		//  collection.
		string[] Encodings
		{
			[return: MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)]
			get;
		}

		string[] Mappings
		{
			[return: MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)]
			get;
		}

		string[] Fonts
		{
			[return: MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)]
			get;
		}

		ICollection Values
		{
			get;
		}

		ICollection Keys
		{
			get;
		}

		void Clear();

		IEncConverters ByImplementationType([In, MarshalAs(UnmanagedType.BStr)] string strImplType,
			[In, Optional /* ,MarshalAs(UnmanagedType.I4) */] ProcessTypeFlags processType);

		[return: MarshalAs(UnmanagedType.BStr)]
		string GetMappingName([In, MarshalAs(UnmanagedType.BStr)] string mappingName,
			[Out, MarshalAs(UnmanagedType.BStr)] out string implementType);

		[return: MarshalAs(UnmanagedType.BStr)]
		string BuildConverterSpecName([In, MarshalAs(UnmanagedType.BStr)] string mappingName,
			[In, MarshalAs(UnmanagedType.BStr)] string implementType);

		[return: MarshalAs(UnmanagedType.BStr)]
		string UnicodeEncodingFormConvert([In, MarshalAs(UnmanagedType.BStr)] string sInput,
			[In /* ,MarshalAs(UnmanagedType.U4) */] EncodingForm eFormInput,
			[In, MarshalAs(UnmanagedType.I4)] int ciInput,
			[In /* ,MarshalAs(UnmanagedType.U4) */] EncodingForm eFormOutput,
			[In /* ,MarshalAs(UnmanagedType.U4) */] NormalizeFlags eNormalizeOutput,
			[Out, MarshalAs(UnmanagedType.U4)] out int nNumItems);

		mappingRegistry RepositoryFile
		{
			get;
		}

		int CodePage([In, MarshalAs(UnmanagedType.BStr)] string fontName);

		string[] GetFontMapping([In, MarshalAs(UnmanagedType.BStr)] string mappingName,
		   [In, MarshalAs(UnmanagedType.BStr)] string fontName);

		string GetMappingNameFromFont([In, MarshalAs(UnmanagedType.BStr)] string fontName);

		void GetImplementationDisplayNames(
			[Out, MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)] out string[] astrImplementationTypes,
			[Out, MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)] out string[] astrDisplayNames);

		string GetImplementationDisplayName([In, MarshalAs(UnmanagedType.BStr)] string strImplementationType);

		[return: MarshalAs(UnmanagedType.VariantBool)]
		bool GetFontMappingFromMapping(
		   [In, MarshalAs(UnmanagedType.BStr)] string strFriendlyName,
		   [Out, MarshalAs(UnmanagedType.BStr)] out string strLhsFontName,
		   [Out, MarshalAs(UnmanagedType.BStr)] out string strRhsFontName);

		[return: MarshalAs(UnmanagedType.VariantBool)]
		bool AutoConfigure(
			[In, Optional, DefaultValue(ConvType.Unknown)] ConvType eConversionTypeFilter,
			[In, Out, Optional, MarshalAs(UnmanagedType.BStr)] ref string strFriendlyName);

		[return: MarshalAs(UnmanagedType.VariantBool)]
		bool AutoConfigureEx(
			[In, MarshalAs(UnmanagedType.Interface)] IEncConverter rIEncConverter,
			[In, Optional, DefaultValue(ConvType.Unknown)] ConvType eConversionTypeFilter,
			[In, Out, Optional, MarshalAs(UnmanagedType.BStr)] ref string strFriendlyName,
			[In, Optional, MarshalAs(UnmanagedType.BStr)] string strLhsEncodingID,
			[In, Optional, MarshalAs(UnmanagedType.BStr)] string strRhsEncodingID
			);

		[return: MarshalAs(UnmanagedType.Interface)]
		IEncConverter AutoSelect(
			[In, Optional, DefaultValue(ConvType.Unknown)] ConvType eConversionTypeFilter);

		[return: MarshalAs(UnmanagedType.Interface)]
		IEncConverter AutoSelectWithTitle(
			[In, Optional, DefaultValue(ConvType.Unknown)] ConvType eConversionTypeFilter,
			[In, MarshalAs(UnmanagedType.BStr)] string strChooseConverterDialogTitle
			);

		[return: MarshalAs(UnmanagedType.Interface)]
		IEncConverter NewEncConverterByImplementationType(
			[In, MarshalAs(UnmanagedType.BStr)] string strImplementationType);

		[return: MarshalAs(UnmanagedType.Interface)]
		IEncConverter AutoSelectWithData(
			[In, MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_UI1)] byte[] abyPreviewData,
			[In, Optional, MarshalAs(UnmanagedType.BStr)] string strFontName,
			[In, Optional, DefaultValue(ConvType.Unknown)] ConvType eConversionTypeFilter,
			[In, Optional, MarshalAs(UnmanagedType.BStr)] string strChooseConverterDialogTitle);

		[return: MarshalAs(UnmanagedType.Interface)]
		IEncConverter AutoSelectWithData(
			[In, MarshalAs(UnmanagedType.BStr)] string strPreviewData,
			[In, Optional, MarshalAs(UnmanagedType.BStr)] string strFontName,
			[In, Optional, DefaultValue(ConvType.Unknown)] ConvType eConversionTypeFilter,
			[In, Optional, MarshalAs(UnmanagedType.BStr)] string strChooseConverterDialogTitle);
	}
	#endregion IEncConverters Interface Definition
}
