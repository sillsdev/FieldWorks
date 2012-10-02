using System;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;                  // for RegistryKey
using System.Diagnostics;               // for Debug.Assert
using ECInterfaces;                     // for IEncConverter

namespace SilEncConverters30
{
	/// <summary>
	/// Font2IsciiEncConverter implements the EncConverter interface to provide a wrapper
	/// for the font2iscii exe-based converter
	/// </summary>
	[GuidAttribute("1785FCA9-6AB3-46ee-BF89-2BA03B496E70")]
	public class Font2IsciiEncConverter : EncConverter
	{
		#region Member Variable Definitions
		private string m_strF2IFile;
		private string m_strI2FFileCtrl;
		private string m_strI2FFileMap;
		private string m_strWorkingDir;

		public const string strLhsEncoding = "Font2Iscii";
		public const string strRhsEncoding = "ISCII";
		public const string strImplType = "SIL.Font2Iscii";
		public const string strMapDefPath = @"\SIL\Indic\Font2Iscii\"; // (from \pf\cf...)
		public const string strMapPathKey = "PathToMapFiles";

		public string WorkingDir
		{
			get { return m_strWorkingDir; }
			set { m_strWorkingDir = value; }
		}

		public string F2IFile
		{
			get { return m_strF2IFile; }
			set { m_strF2IFile = value; }
		}

		public string I2FFileCtrl
		{
			get { return m_strI2FFileCtrl; }
			set { m_strI2FFileCtrl = value; }
		}
		public string I2FFileMap
		{
			get { return m_strI2FFileMap; }
			set { m_strI2FFileMap = value; }
		}
		#endregion Member Variable Definitions

		#region DLLImport Statements
		[DllImport("SilFont2Iscii", SetLastError = true)]
		static extern unsafe int SilFont2IsciiOpenMapTable(
			byte* lpszMapFileName,
			Int32* pInstanceData);

		[DllImport("SilFont2Iscii", SetLastError = true)]
		static extern unsafe int SilFont2IsciiCloseMapTable(
			Int32 instanceData);

		[DllImport("SilFont2Iscii", SetLastError = true)]
		static extern unsafe int SilFont2IsciiDoConversion(
			Int32 instanceData,
			byte* szInput,
			Int32 nInputLen,
			byte* szOutput,
			Int32* pOutputLen);

		[DllImport("SilFont2Iscii", SetLastError = true)]
		static extern unsafe int SilIscii2FontOpenMapTable(
			byte* lpszMapFilePath,
			byte* lpszMapFileName,
			Int32* pInstanceData);

		[DllImport("SilFont2Iscii", SetLastError = true)]
		static extern unsafe int SilIscii2FontCloseMapTable(
			Int32 instanceData);

		[DllImport("SilFont2Iscii", SetLastError = true)]
		static extern unsafe int SilIscii2FontDoConversion(
			Int32 instanceData,
			byte* szInput,
			Int32 nInputLen,
			byte* szOutput,
			Int32* pOutputLen);
		#endregion DLLImport Statements

		#region Initialization
		private Int32 m_hTableF2I = 0;
		private Int32 m_hTableI2F = 0;
		private DateTime m_timeModifiedForward = DateTime.MinValue;
		private DateTime m_timeModifiedReverseCtrl = DateTime.MinValue;
		private DateTime m_timeModifiedReverseMap = DateTime.MinValue;

		public Font2IsciiEncConverter()
			: base(typeof(Font2IsciiEncConverter).FullName, strImplType)
		{
		}

		~Font2IsciiEncConverter()
		{
			Unload();
		}

		public override void Initialize(string converterName, string converterSpec,
			ref string lhsEncodingID, ref string rhsEncodingID, ref ConvType conversionType,
			ref Int32 processTypeFlags, Int32 codePageInput, Int32 codePageOutput, bool bAdding)
		{
			conversionType = ConvType.Legacy_to_from_Legacy;
			processTypeFlags |= (Int32)ProcessTypeFlags.NonUnicodeEncodingConversion;

			base.Initialize(converterName, converterSpec, ref lhsEncodingID, ref rhsEncodingID, ref conversionType, ref processTypeFlags, codePageInput, codePageOutput, bAdding);

			// if it's just our value, then make it the same as the font name.
			if (String.Compare(this.LeftEncodingID, strLhsEncoding, true) != 0)
				lhsEncodingID = m_strLhsEncodingID = converterSpec;

			string strSpec = converterSpec.ToLower();
			if ((strSpec == "devpooja")
				|| (strSpec == "langscapedevpooja")
				|| (strSpec == "devpooja1.map")
				|| (strSpec == "dev_pooja.rc")
				|| (strSpec == "dev_pooja1.map")
				)
			{
				this.F2IFile = "devpooja1.map";
				this.I2FFileCtrl = "dev_pooja.rc";
				this.I2FFileMap = "dev_pooja1.map";
			}
			else if ((strSpec == "devpriya")
				|| (strSpec == "devpriya.map.rev")
				|| (strSpec == "devpriya.rc")
				|| (strSpec == "devpriya.map")
				)
			{
				this.F2IFile = "devpriya.map.rev";
				this.I2FFileCtrl = "devpriya.rc";
				this.I2FFileMap = "devpriya.map";
			}
			else if ((strSpec == "dv-ttyogesh")
				|| (strSpec == "f2i.dvngr")
				|| (strSpec == "dvngri.rc")
				|| (strSpec == "dvngri.map")
				)
			{
				this.F2IFile = "f2i.dvngr";
				this.I2FFileCtrl = "Dvngri.rc";
				this.I2FFileMap = "dvngri.map";
			}
			else if ((strSpec == "dvb-ttyogesh")
				|| (strSpec == "yogeshb.f2i")
				|| (strSpec == "yogeshb.rc")
				|| (strSpec == "yogeshb.i2f")
				)
			{
				this.F2IFile = "yogeshb.f2i";
				this.I2FFileCtrl = "yogeshb.rc";
				this.I2FFileMap = "yogeshb.i2f";
			}
			else if ((strSpec == "roman-readable")
				|| (strSpec == "f2i.rmn")
				|| (strSpec == "roman.rc")
				|| (strSpec == "roman.map")
				)
			{
				this.F2IFile = "f2i.rmn";
				this.I2FFileCtrl = "roman.rc";
				this.I2FFileMap = "roman.map";
			}
			else if ((strSpec == "sanskrit-98")
				|| (strSpec == "sansk98.map.rev")
				|| (strSpec == "sansk98.rc")
				|| (strSpec == "sansk98.map")
				)
			{
				this.F2IFile = "sansk98.map.rev";
				this.I2FFileCtrl = "sansk98.rc";
				this.I2FFileMap = "sansk98.map";
			}
			else if ((strSpec == "shusha")
				|| (strSpec == "f2i.shu")
				|| (strSpec == "shusha.rc")
				|| (strSpec == "shusha.map")
				)
			{
				this.F2IFile = "f2i.shu";
				this.I2FFileCtrl = "shusha.rc";
				this.I2FFileMap = "shusha.map";
			}
			else if ((strSpec == "mithi")
				|| (strSpec == "mithi.f2i")
				|| (strSpec == "mithi.rc")
				|| (strSpec == "mithi.map")
				)
			{
				this.F2IFile = "mithi.f2i";
				this.I2FFileCtrl = "mithi.rc";
				this.I2FFileMap = "mithi.map";
			}
			else if ((strSpec == "dvbw-ttyogesh")
				|| (strSpec == "dvbw2i.f2i")
				|| (strSpec == "dvbw.rc")
				|| (strSpec == "DVBWYogesh.i2f")
				)
			{
				this.F2IFile = "DVBW2i.f2i";
				this.I2FFileCtrl = "DVBW.rc";
				this.I2FFileMap = "DVBWYogesh.i2f";
			}
			else if ((strSpec == "akrutidev1")
				|| (strSpec == "f2i.akrutidev1")
				|| (strSpec == "akrutidev1.rc")
				|| (strSpec == "AkrutiDev1.map")
				)
			{
				this.F2IFile = "f2i.akrutidev1";
				this.I2FFileCtrl = "AkrutiDev1.rc";
				this.I2FFileMap = "AkrutiDev1.map";
			}
			else if ((strSpec == "ankit")
				|| (strSpec == "f2i.ankit")
				|| (strSpec == "ankit.rc")
				|| (strSpec == "Ankit.map")
				)
			{
				this.F2IFile = "f2i.ankit";
				this.I2FFileCtrl = "Ankit.rc";
				this.I2FFileMap = "Ankit.map";
			}
			else if ((strSpec == "devlys")
				|| (strSpec == "f2i.devlys")
				|| (strSpec == "devlys.rc")
				|| (strSpec == "Devlys.map")
				)
			{
				this.F2IFile = "f2i.devlys";
				this.I2FFileCtrl = "Devlys.rc";
				this.I2FFileMap = "Devlys.map";
			}
			else if ((strSpec == "kruti46")
				|| (strSpec == "f2i.kruti46")
				|| (strSpec == "kruti.rc")
				|| (strSpec == "Kruti46.map")
				)
			{
				this.F2IFile = "f2i.kruti46";
				this.I2FFileCtrl = "kruti.rc";
				this.I2FFileMap = "Kruti46.map";
			}
			else if ((strSpec == "naidunia")
				|| (strSpec == "f2i.naidunia")
				|| (strSpec == "naidunia.rc")
				|| (strSpec == "naidunia.map")
				)
			{
				this.F2IFile = "f2i.naidunia";
				this.I2FFileCtrl = "naidunia.rc";
				this.I2FFileMap = "naidunia.map";
			}
			else if ((strSpec == "telugu-hemalatha")
				|| (strSpec == "f2i.tlg_pure")
				|| (strSpec == "hemalatha.rc")
				|| (strSpec == "i2f.tlg_pure")
				)
			{
				this.F2IFile = "f2i.tlg_pure";
				this.I2FFileCtrl = "hemalatha.rc";
				this.I2FFileMap = "i2f.tlg_pure";
			}
			else if ((strSpec == "telugu-hemalathab")
				|| (strSpec == "f2i.hemalatha")
				|| (strSpec == "hemalathab.rc")
				|| (strSpec == "hemalatha.i2f")
				)
			{
				this.F2IFile = "f2i.hemalatha";
				this.I2FFileCtrl = "hemalathab.rc";
				this.I2FFileMap = "hemalatha.i2f";
			}
			else
			{
				EncConverters.ThrowError(ErrStatus.NameNotFound, strSpec);
			}

			// we know that our CP input and output are "1252" and "65001" respectively
			// (unless specified)
			if (codePageInput == 0)
				this.CodePageInput = 1252;
			if (codePageOutput == 0)
				this.CodePageOutput = 1252;

			// string strExePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles);
			string strMapPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
			WorkingDir = strMapPath + strMapDefPath;
		}
		#endregion Initialization

		#region Misc helpers
		protected bool IsForwardFileLoaded()
		{
			return (m_hTableF2I != 0);
		}
		protected bool IsReverseFileLoaded()
		{
			return (m_hTableI2F != 0);
		}

		protected void Unload()
		{
			UnloadForwardFile();
			UnloadReverseFile();
		}

		protected void UnloadForwardFile()
		{
			if (IsForwardFileLoaded())
			{
				SilFont2IsciiCloseMapTable(m_hTableF2I);
				m_hTableF2I = 0;
			}
		}

		protected void UnloadReverseFile()
		{
			if (IsReverseFileLoaded())
			{
				SilIscii2FontCloseMapTable(m_hTableI2F);
				m_hTableI2F = 0;
			}
		}

		protected override EncodingForm DefaultUnicodeEncForm(bool bForward, bool bLHS)
		{
			// if it's unspecified, then we want UTF-16
			return EncodingForm.UTF16;
		}

		protected unsafe void LoadForward()
		{
			string strFilename = WorkingDir + F2IFile;

			// first make sure it's there and get the last time it was modified
			DateTime timeModified = DateTime.MinValue; // don't care really, but have to initialize it.
			if (!DoesFileExist(strFilename, ref timeModified))
				EncConverters.ThrowError(ErrStatus.CantOpenReadMap);

			// if it has been modified or it's not already loaded...
			if (timeModified > m_timeModifiedForward)
			{
				// keep track of the modified date, so we can detect a new version to reload
				m_timeModifiedForward = timeModified;

				if (IsForwardFileLoaded())
					this.UnloadForwardFile();

				byte[] baTablePath = Encoding.ASCII.GetBytes(strFilename);
				fixed (byte* pszTablePath = baTablePath)
				fixed (Int32* phTable = &m_hTableF2I)
				{
					int status = SilFont2IsciiOpenMapTable(pszTablePath, phTable);
					if (status != 0)
					{
						TranslateErrStatus(status, strFilename);
					}
				}
			}
		}

		protected unsafe void LoadReverse()
		{
			string strFilenameCtrl = WorkingDir + this.I2FFileCtrl;
			string strFilenameMap = WorkingDir + this.I2FFileMap;

			// first make sure it's there and get the last time it was modified
			DateTime timeModifiedCtrl = DateTime.MinValue;
			DateTime timeModifiedMap = DateTime.MinValue;
			if (!DoesFileExist(strFilenameCtrl, ref timeModifiedCtrl)
				|| !DoesFileExist(strFilenameMap, ref timeModifiedMap)
				)
			{
				EncConverters.ThrowError(ErrStatus.CantOpenReadMap);
			}

			// if it has been modified or it's not already loaded...
			if ((timeModifiedCtrl > m_timeModifiedReverseCtrl)
				|| (timeModifiedMap > m_timeModifiedReverseMap))
			{
				// keep track of the modified date, so we can detect a new version to reload
				m_timeModifiedReverseCtrl = timeModifiedCtrl;
				m_timeModifiedReverseMap = timeModifiedMap;

				if (IsReverseFileLoaded())
					this.UnloadReverseFile();

				byte[] baTablePath = Encoding.ASCII.GetBytes(this.WorkingDir);
				byte[] baTableName = Encoding.ASCII.GetBytes(this.I2FFileCtrl);
				fixed (byte* pszTablePath = baTablePath, pszTableName = baTableName)
				fixed (Int32* phTable = &m_hTableI2F)
				{
					int status = SilIscii2FontOpenMapTable(pszTablePath, pszTableName, phTable);
					if (status != 0)
					{
						TranslateErrStatus(status, WorkingDir + I2FFileCtrl);
					}
				}
			}
		}

		protected void TranslateErrStatus(int status, string strName)
		{
			if (status == -7)
				EncConverters.ThrowError(ErrStatus.NameNotFound, strName);
			else if (status != 0)
				throw new ECException(String.Format("Unknown error returned from Font2Iscii converter: '{0}'", status), ErrStatus.Exception);
		}
		#endregion Misc helpers

		#region Abstract Base Class Overrides
		protected bool m_bForward;

		protected override void PreConvert
			(
			EncodingForm eInEncodingForm,
			ref EncodingForm eInFormEngine,
			EncodingForm eOutEncodingForm,
			ref EncodingForm eOutFormEngine,
			ref NormalizeFlags eNormalizeOutput,
			bool bForward
			)
		{
			// let the base class do it's thing first
			base.PreConvert(eInEncodingForm, ref eInFormEngine,
				eOutEncodingForm, ref eOutFormEngine,
				ref eNormalizeOutput, bForward);

			eInFormEngine = EncodingForm.LegacyBytes;
			eOutFormEngine = EncodingForm.LegacyBytes;

			// do the load at this point.
			m_bForward = bForward;  // keep track so we can see during DoConvert
			if (m_bForward)
				LoadForward();
			else
				LoadReverse();
		}

		protected override unsafe void DoConvert
			(
			byte* lpInBuffer,
			int nInLen,
			byte* lpOutBuffer,
			ref int rnOutLen
			)
		{
			int status = 0;
			fixed (int* pnOut = &rnOutLen)
			{
				if (m_bForward)
					status = SilFont2IsciiDoConversion(m_hTableF2I, lpInBuffer, nInLen, lpOutBuffer, pnOut);
				else
					status = SilIscii2FontDoConversion(m_hTableI2F, lpInBuffer, nInLen, lpOutBuffer, pnOut);
			}

			if (status != 0)
			{
				TranslateErrStatus(status, null);
			}
		}

		protected override string GetConfigTypeName
		{
			get { return null; }
		}

		#endregion Abstract Base Class Overrides
	}
}
