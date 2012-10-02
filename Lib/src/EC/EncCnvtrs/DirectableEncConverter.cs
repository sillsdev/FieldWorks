using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Runtime.Serialization;                 // for SerializationBinder
using System.Runtime.InteropServices;               // for ClassInterface attribute (for VBA)
using Microsoft.Win32;                              // for Registry
using System.IO;                                    // for FileInfo
using System.Reflection;                            // for Assembly
using ECInterfaces;                                 // for ConvType

namespace SilEncConverters31
{
	// create a wrapper on the IEncConverter interface so we can specify a different direction
	// (among other run-time properties of a converter).
	[Serializable()]
	public class DirectableEncConverter
	{
		protected string m_strEncConverterName = null;
		protected bool m_bDirectionForward;
		protected NormalizeFlags m_eNormalForm;
		protected Font m_fontOutput = null;

		public DirectableEncConverter(string strEncConverterName, bool bDirectionForward, NormalizeFlags eNormalForm)
		{
			m_strEncConverterName = strEncConverterName;
			m_bDirectionForward = bDirectionForward;
			m_eNormalForm = eNormalForm;
		}

		public DirectableEncConverter(IEncConverter aEC)
		{
			InitFromIEncConverter(aEC);
		}

		protected void InitFromIEncConverter(IEncConverter aEC)
		{
			m_strEncConverterName = aEC.Name;
			m_bDirectionForward = aEC.DirectionForward;
			m_eNormalForm = aEC.NormalizeOutput;

			// if the user intends for this to be a temporary converter, it won't be available to
			//  subsequent calls of "GetEncConverter" unless we add it to this particular instance
			//  of the local repository object.
			if (EncConverters[m_strEncConverterName] == null)
				EncConverters.Add(m_strEncConverterName, aEC);
		}

		public DirectableEncConverter() // COM (e.g. Excel) needs a plain vanilla version (w/ no UI)
		{
		}

		public string Name
		{
			get { return m_strEncConverterName; }
		}

		public Font TargetFont
		{
			get { return m_fontOutput; }
			set { m_fontOutput = value; }
		}

		public int CodePageInput
		{
			get
			{
				int nCP = 0;
				IEncConverter aEC = GetEncConverter;
				if (aEC != null)
				{
					if (m_bDirectionForward)
						nCP = aEC.CodePageInput;
					else
						nCP = aEC.CodePageOutput;
				}

				return nCP;
			}
			set
			{
				IEncConverter aEC = GetEncConverter;
				if (aEC != null)
				{
					if (m_bDirectionForward)
						aEC.CodePageInput = value;
					else
						aEC.CodePageOutput = value;
				}
			}
		}

		public int CodePageOutput
		{
			get
			{
				int nCP = 0;
				IEncConverter aEC = GetEncConverter;
				if (aEC != null)
				{
					if (m_bDirectionForward)
						nCP = aEC.CodePageOutput;
					else
						nCP = aEC.CodePageInput;
				}

				return nCP;
			}
			set
			{
				IEncConverter aEC = GetEncConverter;
				if (aEC != null)
				{
					if (m_bDirectionForward)
						aEC.CodePageOutput = value;
					else
						aEC.CodePageInput = value;
				}
			}
		}

		public string Convert(string sInput)
		{
			IEncConverter aEC = GetEncConverter;
			if (aEC != null)
				return aEC.Convert(sInput);
			else
				throw NoConverterException;
		}

		protected ApplicationException NoConverterException
		{
			get
			{
				return new ApplicationException(String.Format("The converter you've requested ('{0}') doesn't exist anymore!", Name));
			}
		}

		// convert in the opposite direction (if it started as "reverse", then this
		//  means forward
		public string ConvertDirectionOpposite(string sInput)
		{
			IEncConverter aEC = EncConverters[m_strEncConverterName];
			if (aEC != null)
			{
				aEC.DirectionForward = !m_bDirectionForward;
				string strOutput = aEC.Convert(sInput);
				aEC.DirectionForward = m_bDirectionForward;
				return strOutput;
			}
			else
				throw NoConverterException;
		}

		public override string ToString()
		{
			IEncConverter aEC = GetEncConverter;
			if (aEC != null)
				return aEC.ToString();
			else
				return Name;
		}

		public IEncConverter GetEncConverter
		{
			get
			{
				if (Name == null)   // if hasn't been initialized yet...
					throw new ApplicationException("You must select a converter first");

				// first set the run-time properties (since it is theoretically possible to have different
				//  values for two different conversions).
				IEncConverter aEC = EncConverters[m_strEncConverterName];
				if (aEC != null)
				{
					aEC.DirectionForward = m_bDirectionForward;
					aEC.NormalizeOutput = m_eNormalForm;
				}
				return aEC;
			}
		}

		public bool IsLhsLegacy
		{
			get
			{
				IEncConverter aEC = GetEncConverter;
				if (aEC != null)
				{
					if (aEC.DirectionForward)
						return (EncConverter.NormalizeLhsConversionType(aEC.ConversionType) == NormConversionType.eLegacy);
					else
						return (EncConverter.NormalizeRhsConversionType(aEC.ConversionType) == NormConversionType.eLegacy);
				}
				else
					throw NoConverterException;
			}
		}

		public bool IsRhsLegacy
		{
			get
			{
				IEncConverter aEC = GetEncConverter;
				if (aEC != null)
				{
					if (aEC.DirectionForward)
						return (EncConverter.NormalizeRhsConversionType(aEC.ConversionType) == NormConversionType.eLegacy);
					else
						return (EncConverter.NormalizeLhsConversionType(aEC.ConversionType) == NormConversionType.eLegacy);
				}
				else
					throw NoConverterException;
			}
		}

		[NonSerializedAttribute()]
		private static EncConverters m_aECs = null;
		[NonSerializedAttribute()]
		private static DateTime m_timeModified = DateTime.MinValue;

		public static EncConverters EncConverters
		{
			get
			{
				DateTime timeModified = DateTime.MinValue;
				if (    (   (DoesFileExist(EncConverters.GetRepositoryFileName(), ref timeModified))
						&&  (timeModified > m_timeModified)
						)
					||  (m_aECs == null)
					)
				{
					EncConverters aECs = new EncConverters();

					// just in case the last ECs had temporary converters, this will cause them to go away
					//  which is not good for client apps.
					if (m_aECs != null)
						foreach (IEncConverter aEC in m_aECs.Values)
							if (!aEC.IsInRepository)
								aECs.Add(aEC.Name, aEC);    // this flavor of "Add" bypasses the repository store

					// now replace it...
					m_aECs = aECs;

					// keep track of the modified date, so we can detect a new version to reload
					m_timeModified = timeModified;
				}

				return m_aECs;
			}
			set
			{
				m_aECs = value; // allow clients to have us use *their* instance of the repository
				DoesFileExist(EncConverters.GetRepositoryFileName(), ref m_timeModified);
			}
		}

		protected static bool DoesFileExist(string strFileName, ref DateTime TimeModified)
		{
			bool bRet = true;

			try
			{
				FileInfo fi = new FileInfo(strFileName);
				TimeModified = fi.LastWriteTime;
				bRet = fi.Exists;
			}
			catch
			{
				bRet = false;
			}

			return bRet;
		}
	}

	public sealed class DirectableEncConverterDeserializationBinder : SerializationBinder
	{
		public override Type BindToType(string assemblyName, string typeName)
		{
			// If a client app is trying to serialize in an older version, then just redirect it to this
			//  assembly name (we can do this because there's no change to the actual class; just a difference
			//  in the version name)
			// If you have changed the assembly number, then add the old version number to another
			//  OR case in this if statement (and update the Debug.Assert inside to the new version number
			if (    (assemblyName == "SilEncConverters22, Version=2.2.2.0, Culture=neutral, PublicKeyToken=f1447bae1e63f485")
				||  (assemblyName == "SilEncConverters22, Version=2.2.3.0, Culture=neutral, PublicKeyToken=f1447bae1e63f485")
				||  (assemblyName == "SilEncConverters22, Version=2.2.4.0, Culture=neutral, PublicKeyToken=f1447bae1e63f485")
				||  (assemblyName == "SilEncConverters22, Version=2.2.5.0, Culture=neutral, PublicKeyToken=f1447bae1e63f485")
				||  (assemblyName == "SilEncConverters22, Version=2.6.0.0, Culture=neutral, PublicKeyToken=f1447bae1e63f485")
				||  (assemblyName == "SilEncConverters22, Version=2.6.1.0, Culture=neutral, PublicKeyToken=f1447bae1e63f485")
				||  (assemblyName == "SilEncConverters30, Version=3.0.0.0, Culture=neutral, PublicKeyToken=f1447bae1e63f485")
				)
			{
				assemblyName = Assembly.GetExecutingAssembly().FullName;
				System.Diagnostics.Debug.Assert(assemblyName == "SilEncConverters30, Version=3.1.0.0, Culture=neutral, PublicKeyToken=f1447bae1e63f485", "Oops. I forgot to update to the current assembly version in DirectableEncConverterDeserializationBinder. Contact silconverters_support@sil.org with this message");
			}
			return Type.GetType(String.Format("{0}, {1}", typeName, assemblyName));
		}
	}
}
