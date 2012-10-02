using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using ECInterfaces;

namespace SilEncConverters40
{
	/// <summary>
	/// TechHindiSiteEncConverter implements the EncConverter interface to provide a
	/// wrapper for the web-page based converter available at the Technical Hindi
	/// google group: http://groups.google.com/group/technical-hindi/files
	///
	/// These web pages have Java script code to convert legacy encodings to Unicode
	/// which I'm "borrowing" to do the conversion.
	///
	/// The identifier for this EncConverter is:
	///     <uri to file>;
	///     <id of input (legacy) textarea>;
	///     <id of output (unicode) textarea>;
	///     <name of function to do conversion>;
	///     (<name of function to do reverse conversion>)
	/// </summary>
	[GuidAttribute("0F218E35-EA40-4d56-9FFF-322094B4F412")]
	public class TechHindiSiteEncConverter : EncConverter
	{
		#region Member Variable Definitions

		public const string CstrImplementationType = "SIL.TechHindiWebPage";
		public const string CstrDisplayName = "Technical Hindi (Google group) Html Converter";
		public const string CstrHtmlFilename = "Technical Hindi (Google group) Html Converter Plug-in About box.mht";

		protected string ConverterPageUri;
		protected string InputHtmlElementId;
		protected string OutputHtmlElementId;
		protected string ConvertFunctionName;
		protected string ConvertReverseFunctionName;

		private WebBrowser _webBrowser;
		private HtmlDocument _docHtml;
		private HtmlElement _elemInput;
		private HtmlElement _elemOutput;

		private bool _bForward;
		#endregion Member Variable Definitions

		#region Initialization
		public TechHindiSiteEncConverter()
			: base(typeof(TechHindiSiteEncConverter).FullName, CstrImplementationType)
		{
		}

		public override void Initialize(string converterName, string converterSpec, ref string lhsEncodingID, ref string rhsEncodingID, ref ECInterfaces.ConvType conversionType, ref int processTypeFlags, int codePageInput, int codePageOutput, bool bAdding)
		{
			base.Initialize(converterName, converterSpec, ref lhsEncodingID, ref rhsEncodingID, ref conversionType, ref processTypeFlags, codePageInput, codePageOutput, bAdding);

			if (!ParseConverterIdentifier(converterSpec, out ConverterPageUri, out InputHtmlElementId,
				out OutputHtmlElementId, out ConvertFunctionName, out ConvertReverseFunctionName))
			{
				throw new ApplicationException(String.Format("{0} not properly configured!", CstrDisplayName));
			}
		}

		internal static bool ParseConverterIdentifier(string converterSpec, out string strConverterPageUri,
			out string strInputHtmlElementId, out string strOutputHtmlElementId,
			out string strConvertFunctionName, out string strConvertReverseFunctionName)
		{
			strConverterPageUri = strInputHtmlElementId = strOutputHtmlElementId =
				strConvertFunctionName = strConvertReverseFunctionName = null;

			string[] astrs = converterSpec.Split(new [] {';'}, StringSplitOptions.RemoveEmptyEntries);
			if ((astrs.Length != 4) && (astrs.Length != 5))
				 return false;

			strConverterPageUri = astrs[0];
			strInputHtmlElementId = astrs[1];
			strOutputHtmlElementId = astrs[2];
			strConvertFunctionName = astrs[3];
			if (astrs.Length == 5)
				strConvertReverseFunctionName = astrs[4];
			return true;
		}
		#endregion Initialization

		#region Abstract Base Class Overrides

		protected override string GetConfigTypeName
		{
			get { return typeof(TechHindiSiteConfig).AssemblyQualifiedName; }
		}

		protected override void PreConvert(EncodingForm eInEncodingForm, ref EncodingForm eInFormEngine, EncodingForm eOutEncodingForm, ref EncodingForm eOutFormEngine, ref NormalizeFlags eNormalizeOutput, bool bForward)
		{
			base.PreConvert(eInEncodingForm, ref eInFormEngine, eOutEncodingForm, ref eOutFormEngine, ref eNormalizeOutput, bForward);

			_bForward = bForward;

			if (!IsLoaded)
				Load();
		}

		protected override unsafe void DoConvert(byte* lpInBuffer, int nInLen, byte* lpOutBuffer, ref int rnOutLen)
		{
			// we need to put it *back* into a string for the lookup
			// [aside: I should probably override base.InternalConvertEx so I can avoid having the base
			//  class version turn the input string into a byte* for this call just so we can turn around
			//  and put it *back* into a string for our processing... but I like working with a known
			//  quantity and no other EncConverter does it that way. Besides, I'm afraid I'll break smtg ;-]
			byte[] baIn = new byte[nInLen];
			ECNormalizeData.ByteStarToByteArr(lpInBuffer, nInLen, baIn);
			string strOutput;
			Encoding enc;
			bool bInputLegacy = ((_bForward &&
								  (NormalizeLhsConversionType(ConversionType) == NormConversionType.eLegacy))
								 ||
								 (!_bForward &&
								  (NormalizeRhsConversionType(ConversionType) == NormConversionType.eLegacy)));

			if (bInputLegacy)
			{
				try
				{
					enc = Encoding.GetEncoding(CodePageInput);
				}
				catch
				{
					enc = Encoding.GetEncoding(EncConverters.cnIso8859_1CodePage);
				}
			}
			else
				enc = Encoding.Unicode;

			char[] caIn = enc.GetChars(baIn);

			// here's our input string
			string strInput = new string(caIn);

			if (_bForward)
			{
				_elemInput.InnerText = strInput;

				// TODO: catch errors?
				_docHtml.InvokeScript(ConvertFunctionName);

				strOutput = _elemOutput.InnerText;
			}
			else
			{
				_elemOutput.InnerText = strInput;

				// TODO: catch errors?
				_docHtml.InvokeScript(ConvertReverseFunctionName);

				strOutput = _elemInput.InnerText;
			}

			if (!String.IsNullOrEmpty(strOutput))
				StringToProperByteStar(strOutput, lpOutBuffer, ref rnOutLen);
		}

		#endregion Abstract Base Class Overrides

		#region Misc Helpers
		protected bool IsLoaded
		{
			get
			{
				return
					!( (_webBrowser == null)
					|| (_docHtml == null)
					|| (_elemInput == null)
					|| (_elemOutput == null)
					|| (String.IsNullOrEmpty(ConvertFunctionName)));
			}
		}

		public static ManualResetEvent waitForPageLoaded = new ManualResetEvent(false);
		protected void Load()
		{
			System.Diagnostics.Debug.Assert(!String.IsNullOrEmpty(ConverterPageUri));
			if (_webBrowser == null)
			{
				_webBrowser = new WebBrowser();
				_webBrowser.DocumentCompleted += WebBrowserDocumentCompleted;
			}

			_webBrowser.Url = new Uri(ConverterPageUri);
			Application.DoEvents();
			waitForPageLoaded.WaitOne();

			// check that our configuration is correct
			_docHtml = _webBrowser.Document;
			if (_docHtml == null)
				EncConverters.ThrowError(ErrStatus.NameNotFound, ConverterPageUri);

			_elemInput = _docHtml.GetElementById(InputHtmlElementId);
			if (_elemInput == null)
				EncConverters.ThrowError(ErrStatus.NameNotFound, InputHtmlElementId);

			_elemOutput = _docHtml.GetElementById(OutputHtmlElementId);
			if (_elemOutput == null)
				EncConverters.ThrowError(ErrStatus.NameNotFound, OutputHtmlElementId);

			HtmlElementCollection elemScripts = _docHtml.GetElementsByTagName("script");
			if (elemScripts.Count <= 0)
				EncConverters.ThrowError(ErrStatus.NameNotFound, ConvertFunctionName);

			HtmlElement elemScript = elemScripts[0];
			if (elemScript.InnerHtml.IndexOf(ConvertFunctionName) == -1)
				EncConverters.ThrowError(ErrStatus.NameNotFound, ConvertFunctionName);

			if (!String.IsNullOrEmpty(ConvertReverseFunctionName))
				if (elemScript.InnerHtml.IndexOf(ConvertReverseFunctionName) == -1)
					EncConverters.ThrowError(ErrStatus.NameNotFound, ConvertReverseFunctionName);
		}

		static void WebBrowserDocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
		{
			// TODO: check for bad URL
			waitForPageLoaded.Set();
		}

		protected unsafe void StringToProperByteStar(string strOutput, byte* lpOutBuffer, ref int rnOutLen)
		{
			// if the output is legacy, then we need to shrink it from wide to narrow
			if ((_bForward && NormalizeRhsConversionType(ConversionType) == NormConversionType.eLegacy)
				|| (!_bForward && NormalizeLhsConversionType(ConversionType) == NormConversionType.eLegacy))
			{
				byte[] baOut = EncConverters.GetBytesFromEncoding(CodePageOutput, strOutput, true);

				if (baOut.Length > rnOutLen)
					EncConverters.ThrowError(ErrStatus.OutputBufferFull);
				rnOutLen = baOut.Length;
				ECNormalizeData.ByteArrToByteStar(baOut, lpOutBuffer);
			}
			else
			{
				int nLen = strOutput.Length * 2;
				if (nLen > (int)rnOutLen)
					EncConverters.ThrowError(ErrStatus.OutputBufferFull);
				rnOutLen = nLen;
				ECNormalizeData.StringToByteStar(strOutput, lpOutBuffer, rnOutLen);
			}
		}
		#endregion Misc Helpers
	}
}
