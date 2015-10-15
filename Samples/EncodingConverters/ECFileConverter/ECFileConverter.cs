// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// Responsibility: Bob Eaton

using System;
using System.IO;
using EncCnvtrs;

namespace ECFileConverter
{
	/// <summary>
	/// FCCommandLine: class for processing the command line parameters
	/// </summary>
	class FCCommandLine
	{
		public void WriteUsage()    { Console.WriteLine(sUsage); }
		public bool IsInvalid()   // invalid if no input and output filenames
		{
			return  (   (InputFileName == null)
					||  (OutputFileName == null)
				);
		}
		public FCCommandLine()
		{
			m_eParamState = ParamState.eParamStart;

			InputEncoding = System.Text.Encoding.UTF8;
			OutputEncoding = System.Text.Encoding.Unicode;

			DirectionForward = true;
		}

		public void ProcessCmdLine(string[] args)
		{
			foreach (string arg in args)
			{
				bool bFlag = false;
				string sParam = arg;
				if( (arg[0] == '/') || (arg[0] == '-') )
				{
					bFlag = true;
					sParam = arg.Remove(0,1);
				}
				ProcessArg(sParam,bFlag);
			}
		}

		private void ProcessArg(string arg, bool bFlag)
		{
			if( bFlag )
			{
				if( arg == "r" )
				{
					DirectionForward = false;
				}

				if( arg == "n" )
				{
					m_eParamState = ParamState.eParamConverterName;
				}
				else if( arg == "o16" )
				{
					m_eParamState = ParamState.eParamOutputFileName;
					OutputEncoding = System.Text.Encoding.Unicode;
				}
				else if( arg == "o8" )
				{
					m_eParamState = ParamState.eParamOutputFileName;
					OutputEncoding = System.Text.Encoding.UTF8;
				}
				else if( arg == "o" )
				{
					m_eParamState = ParamState.eParamOutputFileName;
					OutputEncoding = System.Text.Encoding.Default;
				}
				else if( arg == "i16" )
				{
					m_eParamState = ParamState.eParamInputFileName;
					InputEncoding = System.Text.Encoding.Unicode;
				}
				else if( arg == "i8" )
				{
					m_eParamState = ParamState.eParamInputFileName;
					InputEncoding = System.Text.Encoding.UTF8;
				}
				else if( arg == "i" )
				{
					m_eParamState = ParamState.eParamInputFileName;
					InputEncoding = System.Text.Encoding.Default;
				}
			}
			else
			{
				switch( m_eParamState )
				{
					case ParamState.eParamConverterName:
						ConverterName = arg;
						break;
					case ParamState.eParamOutputFileName:
						OutputFileName = arg;
						break;
					case ParamState.eParamInputFileName:
						InputFileName = arg;
						break;
					default:
						break;
				}
			}
		}

		public string ConverterName
		{
			get { return m_strConverterName; }
			set { m_strConverterName = value; }
		}
		public string InputFileName
		{
			get { return m_strInputFileName; }
			set { m_strInputFileName = value; }
		}
		public string OutputFileName
		{
			get { return m_strOutputFileName; }
			set { m_strOutputFileName = value; }
		}
		public System.Text.Encoding InputEncoding
		{
			get { return m_encInputEncoding; }
			set { m_encInputEncoding = value; }
		}
		public System.Text.Encoding OutputEncoding
		{
			get { return m_encOutputEncoding; }
			set { m_encOutputEncoding = value; }
		}
		public bool    DirectionForward
		{
			get { return m_bDirectionForward; }
			set { m_bDirectionForward = value; }
		}


		private const string sUsage = "Usage:\nECDriver (/n <ConverterName> /r(everse)) {/i|i8|i16} <InputFileName> /{o|o8|o16} <OutputFileName>\n\nwhere:\n  no ConverterFriendlyName parameter means no conversion (except the file encoding)\n  r means run the converter in reverse\n  i causes the the input file to be read as an Ansi encoded file (also for narrow, legacy-encoded, non-Ansi files)\n  i8 causes it to be read as a UTF8 encoded file\n  i16 causes it to be read as a UTF16 encoded file\n  (same details for the 'o' forms for encoding the output file)";

		protected enum ParamState
		{
			eParamStart,
			eParamConverterName,
			eParamOutputFileName,
			eParamInputFileName
		};
		protected ParamState            m_eParamState;
		protected string                m_strConverterName;
		protected string                m_strInputFileName;
		protected string                m_strOutputFileName;
		protected System.Text.Encoding  m_encInputEncoding;
		protected System.Text.Encoding  m_encOutputEncoding;
		protected bool                  m_bDirectionForward;
	}
	/// <summary>
	/// CSharp program for converting a file using a given EncConverter.
	/// </summary>
	class ECFileConverter
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			FCCommandLine cl = new FCCommandLine();
			cl.ProcessCmdLine(args);
			if( cl.IsInvalid() )
			{
				cl.WriteUsage();
			}
			else
			{
				try
				{
					// to go from static to (non-static) member function, we need a class to
					// reference from (i.e. 'this').
					ECFileConverter This = new ECFileConverter();

					This.DoFileConvert(
						cl.ConverterName,
						cl.OutputFileName, cl.OutputEncoding,
						cl.InputFileName, cl.InputEncoding,
						cl.DirectionForward);
				}
				catch(NullReferenceException e)
				{
					Console.WriteLine("program error: Caught exception #1.", e);
					throw e;
				}
				catch(ArgumentNullException e)
				{
					Console.WriteLine("File name missing: ", e);
					throw e;
				}
				catch(FileNotFoundException e)
				{
					Console.WriteLine("File not found: ", e);
					throw e;
				}
				catch(IOException e)
				{
					Console.WriteLine("IO error: ", e);
					throw e;
				}
				catch
				{
					Console.WriteLine("program error!");
					throw;
				}
			}
		}

		public void DoFileConvert
			(
			string                  strConverterName,
			string                  strOutputFileName,
			System.Text.Encoding    outEnc,
			string                  strInputFileName,
			System.Text.Encoding    inEnc,
			bool                    bDirectionForward
			)
		{
			// the user *might* not give us a converter name if they simply want to change
			//  the encoding from, say, UTF8 to UTF16.
			bool bIsConverter = !(strConverterName == null);

			IEncConverter aEC = null;
			if( bIsConverter )
			{
				EncConverters aECs = new EncConverters();
				aEC = aECs[strConverterName];
			}

			// open the input and output files using the given encoding formats
			StreamReader srReadLine = new StreamReader(strInputFileName, inEnc, true);
			srReadLine.BaseStream.Seek(0, SeekOrigin.Begin);
			StreamWriter swWriteLine = new StreamWriter(strOutputFileName, false, outEnc);

			// tell the converter to go the other way, if the user selected 'reverse'
			if( !bDirectionForward && bIsConverter )
				aEC.DirectionForward = false;

			// read the lines of the input file, (optionally convert,) and write them out.
			string sOutput, sInput;
			while (srReadLine.Peek() > -1)
			{
				sInput = srReadLine.ReadLine();

				if( bIsConverter )
					sOutput = aEC.Convert(sInput);
				else
					sOutput = sInput;

				swWriteLine.WriteLine(sOutput);
			}

			srReadLine.Close();
			swWriteLine.Close();
		}
	}
}
