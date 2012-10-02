#define UseXmlFilesForPlugins

using System;
using System.IO;            // for StreamWriter
using Microsoft.Win32;      // for Registry
using System.Diagnostics;   // for ProcessStartInfo
using System.Text;          // for ASCIIEncoding
using ECInterfaces;                     // for IEncConverter

namespace SilEncConverters31
{
	/// <summary>
	/// The ExeEncConverter class implements the EncConverter interface and has common
	/// code for developing an EncConverter plug-in that supports an exe-based converter
	/// (e.g. ITrans.exe, UTrans.exe, etc) that take input data via StandardInput and
	/// return the converted data via StandardOutput/StandardError
	/// </summary>
	public abstract class ExeEncConverter : EncConverter
	{
		#region Member Variable Definitions
		protected string  m_strWorkingDir;
		protected string  m_strImplType;
		protected string  m_strWorkingDirSuffix;
		#endregion Member Variable Definitions

		#region Public Interface
		public string WorkingDir
		{
			get { return m_strWorkingDir; }
			set { m_strWorkingDir = value; }
		}

		public virtual string ExeName
		{
			get { return null; }
		}

		public virtual string Arguments
		{
			get { return null; }
		}
		#endregion Public Interface

		#region Initialization
		public ExeEncConverter
			(
			string  strProgramID,           // e.g. SilEncConverters31.ITrans (usually "typeof(<classname>).FullName")
			string  strImplType,            // e.g. "ITrans" (cf. SIL.tec)
			ConvType conversionType,        // e.g. ConvType.Legacy_to_Unicode
			string  lhsEncodingID,          // e.g. "ITrans" (c.f. "SIL-IPA93-2001")
			string  rhsEncodingID,          // e.g. "UNICODE"
			Int32   lProcessType,           // e.g. ProcessTypeFlags.UnicodeEncodingConversion
			string  strWorkingDirSuffix     // e.g. @"\SIL\Indic\ITrans"
			)
			: base(strProgramID,strImplType)
		{
			m_strImplType = strImplType;
			m_eConversionType = conversionType;
			m_strLhsEncodingID = lhsEncodingID;
			RightEncodingID = rhsEncodingID;
			ProcessType = lProcessType;
			m_strWorkingDirSuffix = strWorkingDirSuffix;
		}

		public override void Initialize(string converterName, string converterSpec,
			ref string lhsEncodingID, ref string rhsEncodingID, ref ConvType conversionType,
			ref Int32 processTypeFlags, Int32 codePageInput, Int32 codePageOutput, bool bAdding)
		{
			base.Initialize
				(
				converterName,
				converterSpec,
				ref m_strLhsEncodingID, // since we may have already set these in the ctor, use our stored value and check for differences later
				ref m_strRhsEncodingID, // ibid
				ref m_eConversionType,  // ibid
				ref m_lProcessType,     // ibid
				codePageInput,
				codePageOutput,
				bAdding
				);

			// normally, the sub-classes can specify the encoding ID, but if it's different
			//  go with what the user gives us (unless it's null)
			if(     !String.IsNullOrEmpty(lhsEncodingID)
				&&  (String.Compare(m_strLhsEncodingID,lhsEncodingID,true) != 0) )
			{
				m_strLhsEncodingID = lhsEncodingID;
			}

			if(     !String.IsNullOrEmpty(rhsEncodingID)
				&&  (String.Compare(m_strRhsEncodingID,rhsEncodingID,true) != 0) )
			{
				m_strRhsEncodingID = rhsEncodingID;
			}

			if( ConversionType != conversionType )
				m_eConversionType = conversionType;

			ProcessType |= processTypeFlags;

#if UseXmlFilesForPlugins
			WorkingDir = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles) + m_strWorkingDirSuffix;
#else
			RegistryKey key = Registry.LocalMachine.OpenSubKey(EncConverters.HKLM_CNVTRS_SUPPORTED);
			if( key != null )
			{
				key = key.OpenSubKey(m_strImplType);
				if( key != null )
					WorkingDir = (string)key.GetValue(strExePathKey);
			}
#endif
		}
		#endregion Initialization

		#region Derived Class overrides
		// these methods can be overridden in the subclasses to modify the way the I/O happens
		public virtual void WriteToExeInputStream(string strInput, StreamWriter input)
		{
			input.WriteLine(strInput);
			input.Close();
		}

		public virtual string ReadFromExeOutputStream(StreamReader srOutput, StreamReader srError)
		{
			string strReturn = srOutput.ReadToEnd();

			if( strReturn == "" )
			{
				strReturn = srError.ReadToEnd();
				srError.Close();
			}

			return strReturn;
		}

		// allow the sub-classes the ability to override how the process is started.
		protected ProcessStartInfo m_psi = null;
		protected virtual ProcessStartInfo ProcessStarter
		{
			get
			{
				if( m_psi == null )
				{
					m_psi = new ProcessStartInfo(WorkingDir + @"\" + ExeName);
					m_psi.Arguments = Arguments;
					m_psi.WorkingDirectory = WorkingDir;
					m_psi.UseShellExecute = false;
					m_psi.CreateNoWindow = true;
					m_psi.RedirectStandardInput = true;
					m_psi.RedirectStandardOutput = true;
					m_psi.RedirectStandardError = true;
				}
				return m_psi;
			}
		}
		#endregion Derived Class overrides

		#region Implementation
		protected string DoExeCall(string sInput)
		{
			ProcessStartInfo Si = ProcessStarter;
			string strOutput = null;

			try
			{
				Process P = Process.Start(Si);

				// set up the writer to use the correct code page
				StreamWriter sw = null;
				if( P.StandardInput.Encoding.CodePage != this.CodePageInput )
				{
					Encoding enc;
					try
					{
						enc = Encoding.GetEncoding(this.CodePageInput);
					}
					catch
					{
						enc = Encoding.GetEncoding(EncConverters.cnIso8859_1CodePage);
					}
					sw = new StreamWriter(P.StandardInput.BaseStream,enc);
				}
				else
					sw = P.StandardInput;

				// call a virtual to do this in case the sub-classes have special behavior
				WriteToExeInputStream(sInput,sw);

				// set up the reader to use the correct code page
				StreamReader sr = null;
				if( P.StandardOutput.CurrentEncoding.CodePage != this.CodePageOutput )
				{
					Encoding enc;
					try
					{
						enc = Encoding.GetEncoding(this.CodePageOutput);
					}
					catch
					{
						enc = Encoding.GetEncoding(EncConverters.cnIso8859_1CodePage);
					}
					sr = new StreamReader(P.StandardOutput.BaseStream, enc);
				}
				else
					sr = P.StandardOutput;

				// call a virtual to do this in case the sub-classes have special behavior
				strOutput = ReadFromExeOutputStream(sr, P.StandardError);
			}
			catch(Exception e)
			{
				throw e;
			}

			return strOutput;
		}
		#endregion Implementation

		#region Abstract Base Class Overrides
		[CLSCompliant(false)]
		protected override unsafe void DoConvert
			(
			byte*       lpInBuffer,
			int         nInLen,
			byte*       lpOutBuffer,
			ref int     rnOutLen
			)
		{
			rnOutLen = 0;
			if( !String.IsNullOrEmpty(WorkingDir) )
			{
				// we need to put it *back* into a string because the StreamWriter that will
				// ultimately write to the StandardInput uses a string. Use the correct codepg.
				byte [] baDst = new byte [nInLen];
				ECNormalizeData.ByteStarToByteArr(lpInBuffer,nInLen,baDst);
				Encoding enc;
				try
				{
					enc = Encoding.GetEncoding(this.CodePageInput);
				}
				catch
				{
					enc = Encoding.GetEncoding(EncConverters.cnIso8859_1CodePage);
				}
				string strInput = enc.GetString(baDst);

				// call the helper that calls the exe
				string strOutput = DoExeCall(strInput);

				// if there's a response...
				if( !String.IsNullOrEmpty(strOutput) )
				{
					// ... put it in the output buffer
					// if the output is legacy, then we need to shrink it from wide to narrow
					// it'll be legacy either if (the direction is forward and the rhs=eLegacy)
					// or if (the direction is reverse and the rhs=eLegacy)
					bool bLegacyOutput =
						(
							(   (this.DirectionForward == true)
							&&  (EncConverter.NormalizeRhsConversionType(this.ConversionType) == NormConversionType.eLegacy)
							)
						||  (   (this.DirectionForward == false)
							&&  (EncConverter.NormalizeLhsConversionType(this.ConversionType) == NormConversionType.eLegacy)
							)
						);

					if( bLegacyOutput )
					{
						try
						{
							enc = Encoding.GetEncoding(this.CodePageOutput);
						}
						catch
						{
							enc = Encoding.GetEncoding(EncConverters.cnIso8859_1CodePage);
						}
						byte [] baOut = enc.GetBytes(strOutput);
						ECNormalizeData.ByteArrToByteStar(baOut,lpOutBuffer);
						rnOutLen = baOut.Length;
					}
					else
					{
						rnOutLen = strOutput.Length * 2;
						ECNormalizeData.StringToByteStar(strOutput,lpOutBuffer,rnOutLen);
					}
				}
			}
			else
				EncConverters.ThrowError(ErrStatus.RegistryCorrupt);
		}
		#endregion Abstract Base Class Overrides
	}
}
