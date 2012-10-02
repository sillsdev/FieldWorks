using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;

namespace PatrParserWrapper
{
	/// <summary>
	/// A straight line by line port of PAtrParser.cpp COM object.
	/// This class could use some refactoring to remove some of the c'isms.
	/// </summary>
	public class PatrParser : IPatrParser, IDisposable
	{
		const string szDefaultWordMarker_g = "\\w";
		const string szDefaultCategoryMarker_g = "\\c";
		const string szDefaultFeatureMarker_g = "\\f";
		const string szDefaultGlossMarker_g = "\\g";
		const string szDefaultRootGlossMarker_g = "\\r";
		const string szWhitespace_g = " \t\r\n\f\v";
		const string szDefaultBarcodes_g = "bdefhijmrsuvyz";

		#region Data Member variables

		private PATRMemory m_memory;
		private PATRData m_data;
		private List<string> m_ppszLexFiles;
		private List<bool> m_pbAnaLexFile;
		private uint m_uiLexFileCount;
		private string m_pszLogFile;
		private string m_pszError;
		private Encoding m_encoding;
		// default to true (in orignal c++ code this variable was potentially uninialized)
		private bool m_bWriteAmpleParses = true;
		private TextControl m_sTextCtl;

		#endregion

		public PatrParser()
		{
			m_memory = new PATRMemory();
			m_data = new PATRData();

			m_sTextCtl = new TextControl();

			m_memory.bPreserve = 1;
			m_data.bUnification = 1;

			m_data.eTreeDisplay = (byte)PATRDataDataTypes.eTreeDisplay.PATR_FULL_TREE;
			m_data.eRootGlossFeature = (byte)PATRDataDataTypes.eRootGlossFeature.PATR_ROOT_GLOSS_NO_FEATURE;
			m_data.bGloss = 1;
			m_data.iFeatureDisplay = (byte)(PATRDataDataTypes.iFeatureDisplay.PATR_FEATURE_ON |
									 PATRDataDataTypes.iFeatureDisplay.PATR_FEATURE_TRIM);

			m_data.bCheckCycles = 1;
			m_data.bTopDownFilter = 1;
			m_data.iMaxAmbiguities = 10;
			m_data.cComment = (byte)PATRDataDataTypes.PATR_DEFAULT_COMMENT;
			m_data.bSilent = 1;
			m_data.bShowWarnings = 1;
			m_data.bPromoteDefAtoms = 1;
			m_data.bPropIsFeature = 0;
			// works here

			m_data.pszRecordMarker = Marshal.StringToHGlobalAnsi(szDefaultWordMarker_g);
			m_data.pszWordMarker = Marshal.StringToHGlobalAnsi(szDefaultWordMarker_g);
			m_data.pszGlossMarker = Marshal.StringToHGlobalAnsi(szDefaultGlossMarker_g);
			m_data.pszRootGlossMarker = Marshal.StringToHGlobalAnsi(szDefaultRootGlossMarker_g);
			m_data.pszCategoryMarker = Marshal.StringToHGlobalAnsi(szDefaultCategoryMarker_g);
			m_data.pszFeatureMarker = Marshal.StringToHGlobalAnsi(szDefaultFeatureMarker_g);
			m_data.pMem = Marshal.AllocHGlobal(Marshal.SizeOf(m_memory));
			Marshal.StructureToPtr(m_memory, m_data.pMem, false);

			initPATRSentenceFinalPunctuation(ref m_data);

			m_sTextCtl.cFormatMark = (byte)'\\';
			m_sTextCtl.cAnaAmbig = (byte)'%';
			m_sTextCtl.cTextAmbig = (byte)'%';
			m_sTextCtl.cDecomp = (byte)'-';
#if __MonoCS__
			unsafe
			{
				fixed ( TextControl * p = &m_sTextCtl)
				{
					p->cBarMark[0] = (byte)'|';
					p->bCapitalize[0] = 1;
				}
			}
#else
			m_sTextCtl.cBarMark = (byte)'|';
			m_sTextCtl.bCapitalize = 1;
#endif
			m_sTextCtl.pszBarCodes = Marshal.StringToHGlobalAnsi(szDefaultBarcodes_g);
			m_sTextCtl.bIndividualCapitalize = 1;
			m_sTextCtl.uiMaxAmbigDecap = 500;

			m_ppszLexFiles = null;
			m_pbAnaLexFile = null;

			m_uiLexFileCount = 0;
			m_pszLogFile = null;

			m_pszError = null;

			m_encoding = Encoding.Default;
		}

		#region Disposable stuff
		#if DEBUG
		/// <summary/>
		~PatrParser()
		{
			Dispose(false);
		}
		#endif

		/// <summary/>
		public bool IsDisposed { get; private set; }

		/// <summary/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects
			Clear();
			}

			if (m_data.pLogFP != IntPtr.Zero)
			{
				wrappedfclose(m_data.pLogFP);
				m_data.pLogFP = IntPtr.Zero;
			}
			m_pszLogFile = null;

			Marshal.FreeHGlobal(m_data.pszRecordMarker);
			Marshal.FreeHGlobal(m_data.pszWordMarker);
			Marshal.FreeHGlobal(m_data.pszGlossMarker);

#if !hab130
			Marshal.FreeHGlobal(m_data.pszRootGlossMarker);
#endif // hab130

			Marshal.FreeHGlobal(m_data.pszCategoryMarker);
			Marshal.FreeHGlobal(m_data.pszFeatureMarker);

			clearPATRSentenceFinalPunctuation(ref m_data);
			IsDisposed = true;
		}
		#endregion

		#region PInvokes

		[DllImport("Patr100.dll")]
		internal static extern void initPATRSentenceFinalPunctuation(ref PATRData p);

		[DllImport("Patr100.dll")]
		internal static extern void	markPATRParseGarbage (ref PATRData pPATR_io);

		[DllImport("Patr100.dll")]
		internal static extern IntPtr /*PATRWord* */ convertSentenceToPATRWords(
			IntPtr pszSentence_in,
			IntPtr pOutputFP_in,
			IntPtr pfMorphParser_in /*PATRLexItem * (* pfMorphParser_in)(char * pszWord_in)*/,
			int bWarnUnusedFd_in,
			ref PATRData pPATR_in,
			ref int piErrors);

		[DllImport("Patr100.dll")]
		internal static extern void	collectPATRParseGarbage (ref PATRData pPATR_io);

		[DllImport("Patr100.dll")]
		internal static extern void	freeMemory(IntPtr pBlock_io);

		[DllImport("Patr100.dll")]
		internal static extern IntPtr /*PATREdgeList* */ 	parseWithPATR(IntPtr /*PATRWord **/ pSentence_in,
						  ref int /*int * */      piStage_out,
						  ref PATRData /*PATRData **/ pPATR_io);

		[DllImport("Patr100.dll")]
		internal static extern void	writePATRParses(IntPtr /* PATREdgeList **/ pParses_in,
					   IntPtr /*FILE **/         pOutputFP_in,
					   IntPtr /* WordTemplate ***/ ppWords_in,
					   IntPtr /* TextControl **/  pTextControl_in,
					   uint uiSentenceCount_in,
					   ref PATRData  /*PATRData **/     pPATR_in);

		[DllImport("Patr100.dll", CharSet = CharSet.Ansi)]
		internal static extern int stringifyPATRParses (
				  IntPtr /*PATREdgeList **/ pParses_in,
				  ref PATRData /*PATRData **/     pPATR_in,
				  string /*const char **/   pszSentence_in,
				  ref IntPtr /*char ***/ ppszBuffer_out);

		[DllImport("Patr100.dll")]
		internal static extern int	loadPATRGrammar(IntPtr /*const char **/ pszGrammarFile_in,
				   ref PATRData   pPATR_io);

		[DllImport("Patr100.dll")]
		internal static extern void	freePATRGrammar(ref PATRData pPATR_io);

		[DllImport("Patr100.dll")]
		internal static extern void	freePATRLexicon(ref PATRData pPATR_io);

		[DllImport("Patr100.dll")]
		internal static extern int loadPATRLexiconFromAmple(
						  IntPtr /*const char **/  pszAnalysisFile_in,
						  ref TextControl pTextControl_in,
						  ref PATRData pPATR_in);

		[DllImport("Patr100.dll")]
		internal static extern int loadPATRLexicon(IntPtr /*const char **/ pszLexiconFile_in,
				   ref PATRData pPATR_io);

		[DllImport("Patr100.dll")]
		internal static extern void	freePATRInternalMemory(ref PATRData pPATR_in);

		[DllImport("Patr100.dll")]
		internal static extern IntPtr /*WordTemplate ** */ readSentenceOfTemplates(
						 IntPtr /*FILE * */              pInputFP_in,
						 IntPtr /*const char * */        pszAnaFile_in,
						 IntPtr /*const StringList * */  pFinalPunc_in,
						 ref TextControl pTextCtl_in,
						 IntPtr /*FILE * */	         pLogFP_in);

		[DllImport("Patr100.dll")]
		internal static extern int parseAmpleSentenceWithPATR(
					  IntPtr /* WordTemplate ** */ pWords_in,
					  IntPtr /* FILE * */ pOutputFP_in,
					  IntPtr /* char * */          pszOutputFile_in,
					  int             bWarnUnusedFd_in,
					  int             bVerbose_in,
					  int             bWriteAmpleParses_in,
					  uint        uiSentenceCount_in,
					  ref TextControl   pTextControl_in,
					  ref PATRData      pPATR_in);

		[DllImport("Patr100.dll")]
		internal static extern void	clearPATRSentenceFinalPunctuation(ref PATRData pPATR_io);

		[DllImport("Patr100.dll")]
		internal static extern void addPATRSentenceFinalPunctuation(ref PATRData pPATR_io, IntPtr pszChar_in);

		[DllImport("Patr100.dll", CharSet = CharSet.Ansi)]
		internal static extern int	parseWithPATRLexicon(
			string           pszSentence_in,
			IntPtr /*FILE **/           pOutputFP_in,
			IntPtr /* PATRLexItem * (* pfMorphParser_in)(char * pszWord_in) */ pfMorphParser_in,
			int              bWarnUnusedFd_in,
			ref PATRData pPATR_in);

		[DllImport("Patr100.dll", CharSet = CharSet.Ansi)]
		internal static extern void  display_header (IntPtr /* FILE * */ pOutFL_in);

#if !__MonoCS__
		/// If this code is used on Windows we need to ensure that
		/// fopen and fclose use the the verson from the same msvcrt
		/// that Patr100.dll uses.
		/// This could be done by adding wrappedfopen and a wrappedfclose to Patr100.dll

		[DllImport("Patr100.dll")]
		private static extern IntPtr wrappedfopen(string filename, string mode);

		[DllImport("Patr100.dll")]
		private static extern void wrappedfclose(IntPtr fp);

		[DllImport("Patr100.dll")]
		private static extern void wrappedfree(IntPtr mem);
#else

		[DllImport("libc.so.6")]
		private static extern IntPtr fopen(string filename, string mode);

		[DllImport("libc.so.6")]
		private static extern void fclose(IntPtr fp);

		private static IntPtr wrappedfopen(string filename, string mode)
		{
			return fopen(filename, mode);
		}


		private static void wrappedfclose(IntPtr fp)
		{
			fclose(fp);
		}

		private static void wrappedfree(IntPtr mem)
		{
			Marshal.FreeHGlobal(mem);
		}

#endif

		#endregion

		#region Protected methods
		private void LoadLexicon(string bstrFile, int fAdd, bool bAna)
		{
			bool bAdd = fAdd != 0? true : false;
			// check for valid input
			if (bstrFile == null)
				throw new ArgumentNullException();

			// convert the filename from 16-bit Unicode to 8-bit ANSI
			IntPtr pszFile = Marshal.StringToHGlobalAnsi(bstrFile);

			if (bAdd == false)
			{
				// Erase the old lexicon.
				if (m_data.pLexicon != IntPtr.Zero)
				{
					freePATRLexicon(ref m_data);
				}
				if (m_ppszLexFiles != null)
				{
					m_ppszLexFiles.Clear();
					m_pbAnaLexFile.Clear();
					m_uiLexFileCount = 0;
				}
			}
			else
			{
				if (m_ppszLexFiles != null && m_ppszLexFiles.Contains(bstrFile))
					throw new ApplicationException("Lexicon file already present : " + bstrFile);
			}

			// Load the new lexicon.
			bool bSucceed;
			if (bAna)
				bSucceed = loadPATRLexiconFromAmple(pszFile, ref m_sTextCtl, ref m_data) != 0;
			else
				bSucceed = loadPATRLexicon(pszFile, ref m_data) != 0;
			if (bSucceed)
			{
				if (m_ppszLexFiles == null)
				{
					m_ppszLexFiles = new List<string>();
					m_pbAnaLexFile = new List<bool>();
				}

				m_ppszLexFiles.Add(bstrFile);
				m_pbAnaLexFile.Add(bAna);
				++m_uiLexFileCount;
			}
			else
			{
				Marshal.FreeHGlobal(pszFile);
				throw new ApplicationException("loadPATR failed.");
			}
		}
		#endregion

		#region static helper methods

		private static string PtrToString(IntPtr ptr, Encoding encoding)
		{
			int i = 0;
			byte b;
			var bytes = new List<byte>();
			while ((b = Marshal.ReadByte(ptr, i)) != 0)
			{
				bytes.Add(b);
				i++;
			}
			return encoding.GetString(bytes.ToArray());
		}

		private static void StringToPtr(ref IntPtr ptr, string str, Encoding encoding)
		{
			if (ptr != IntPtr.Zero)
				Marshal.FreeHGlobal(ptr);

			byte[] bytes = encoding.GetBytes(str);
			ptr = Marshal.AllocHGlobal(bytes.Length + 1);
			for (int i = 0; i < bytes.Length; i++)
				Marshal.WriteByte(ptr, i, bytes[i]);
			Marshal.WriteByte(ptr, bytes.Length, 0);
		}

		#endregion

		#region IPatrParser Members

		public string ParseString(string bstrSentence)
		{
			if (bstrSentence == null)
				throw new ArgumentNullException();

			string bstrParse = null;

			if (m_data.pGrammar == IntPtr.Zero)
				throw new ApplicationException("Grammar data not setup correctly");

			if (m_data.pLexicon == IntPtr.Zero)
				throw new ApplicationException("pLexicon data not setup correctly");

			if (bstrSentence.Length == 0)
				return bstrParse;

			bstrSentence.TrimStart(szWhitespace_g.ToCharArray());

			IntPtr psz = IntPtr.Zero;
			StringToPtr(ref psz, bstrSentence, m_encoding);

			if (m_data.pLogFP != IntPtr.Zero)
				m_data.pLogFP = AppendTextToFile(LogFile, m_data.pLogFP, String.Format("{0}{1}", bstrSentence, Environment.NewLine));

			//
			//  convert the flat sentence string into a linked list of words stored
			//  in PATRWord structures
			//
			markPATRParseGarbage(ref m_data);
			int cErrors = 0;
			IntPtr /*PATRWord* */ pSentence = convertSentenceToPATRWords(psz, IntPtr.Zero, IntPtr.Zero, 1,
				ref m_data, ref cErrors);
			if ((cErrors != 0) || (pSentence == IntPtr.Zero))
			{
				FreeMemoryFromSentence(pSentence);
				Marshal.FreeHGlobal(psz);

				throw new ApplicationException("Error looking up words");
			}
			//
			//  if no errors looking up words, try to parse the sentence
			//
			int cParses = 0;
			int iStage = 0;
			string pszXml = null;
			IntPtr /*PATREdgeList**/ parses = parseWithPATR(pSentence, ref iStage, ref m_data);
			string pszMessage = null;
			PATREdgeList pel = new PATREdgeList();
			switch (iStage)
			{
				case 0:

					IntPtr ptr = parses;
					for (; ptr != IntPtr.Zero; ptr = pel.pNext)
					{
						pel = (PATREdgeList) Marshal.PtrToStructure(ptr, typeof (PATREdgeList));
						++cParses;
					}
					break;
				case 1:
					pszMessage = "<!-- Turning off unification -->";
					break;
				case 2:
					pszMessage = "<!-- Turning off top-down filtering -->";
					break;
				case 3:
					pszMessage = "<!-- Building the largest parse \"bush\" -->";
					break;
				case 4:
					pszMessage = "<!-- No output available -->";
					break;
				case 5:
					pszMessage = "<!-- Out of Memory (after %lu edges) -->";
					break;
				case 6:
					pszMessage = "<!-- Out of Time (after %lu edges) -->";
					break;
			}
			if ((m_data.pLogFP != IntPtr.Zero) && (pszMessage != null))
			{
				m_data.pLogFP = AppendTextToFile(LogFile, m_data.pLogFP,
					string.Format("<!-- Cannot parse this sentence -->{1}{0}{1}{1}", pszMessage, Environment.NewLine));
			}
			if (parses != IntPtr.Zero)
			{
				if (m_data.pLogFP != IntPtr.Zero)
				{
					writePATRParses(parses, m_data.pLogFP, IntPtr.Zero, IntPtr.Zero, 0, ref m_data);
					m_data.pLogFP = AppendTextToFile(LogFile, m_data.pLogFP, Environment.NewLine);
				}

				IntPtr temp = IntPtr.Zero;
				if (stringifyPATRParses(parses, ref m_data, bstrSentence /*pszSentence*/, ref temp) != 0)
				{
					FreeMemoryFromSentence(pSentence);
					throw new OutOfMemoryException("stringifyPATRParses failed.");
				}

				pszXml = PtrToString(temp, m_encoding);
				wrappedfree(temp);
			}
			//
			//  erase the temporarily allocated linked list of PATRWords
			//
			FreeMemoryFromSentence(pSentence);
			Marshal.FreeHGlobal(psz);

			if (iStage != 0)
				throw new ApplicationException(pszMessage);

			return pszXml;
		}

		private void FreeMemoryFromSentence(IntPtr pSentence)
		{
			IntPtr pword;
			collectPATRParseGarbage(ref m_data);
			while ((pword = pSentence) != IntPtr.Zero)
			{
				PATRWord temp = (PATRWord)Marshal.PtrToStructure(pword, typeof(PATRWord));
				pSentence = temp.pNext;
				freeMemory(pword);
			}
		}

		public void ParseFile(string bstrInput, string bstrOutput)
		{
			// check for valid input
			if (bstrInput == null || bstrOutput == null)
				throw new ArgumentNullException();

			string psz;
			uint linenum = 1;
			uint cSentences = 0;
			int cAmbiguity;
			int i;
			int[] successes = new int[11];
			using (System.IO.StreamReader pfileIn = new System.IO.StreamReader(bstrInput))
			{
			IntPtr /* FILE * */ pfileOut = wrappedfopen(bstrOutput, "w");
			if (pfileOut == IntPtr.Zero)
			{
				pfileIn.Close();
				throw new ApplicationException("could not open file:" + bstrOutput);
			}

			while((psz = pfileIn.ReadLine()) != null)
			{
				/*
				 *  skip any leading whitespace
				 */
				psz.TrimStart(szWhitespace_g.ToCharArray());

				if (psz == String.Empty)
					continue;
				psz.TrimEnd(szWhitespace_g.ToCharArray());
				if (m_data.eTreeDisplay != (byte)PATRDataDataTypes.eTreeDisplay.PATR_XML_TREE)
				{
					pfileOut = AppendTextToFile(bstrOutput, pfileOut, psz);
				}
				cAmbiguity = parseWithPATRLexicon(psz, pfileOut, IntPtr.Zero,
					1, ref m_data);
				if (cAmbiguity < 10)
					++successes[cAmbiguity];
				else
					++successes[10];
				++cSentences;
			}

			pfileIn.Close();
			wrappedfclose(pfileOut);
			}

			using (var pfileOutManaged = File.Open(bstrOutput, FileMode.OpenOrCreate))
			{
				using (var writer = new StreamWriter(pfileOutManaged))
				{
					writer.WriteLine();
					writer.WriteLine();
					writer.WriteLine("File parsing statistics: {0} sentences read)", cSentences);

			for (i = 0 ; i < 10 ; ++i )
			{
				if (successes[i] != 0)
					writer.WriteLine("    {0} sentence{1} with {2} parses", successes[i], (successes[i] == 1) ? " " : "s", i);
			}
			if (successes[10] != 0)
						writer.WriteLine("    {0} sentence{1} with 10 or more parses", successes[10], (successes[10] == 1) ? " " : "s");

			uint num_parsed;
			uint percent_parsed;
			uint frac_percent;
			num_parsed = (uint) (cSentences - successes[0]);
			if (cSentences == 0)
			{
				percent_parsed = 0;
				frac_percent   = 0;
			}
			else
			{
				percent_parsed = (100 * num_parsed) / cSentences;
				frac_percent = (100 * num_parsed) % cSentences;
				frac_percent = (frac_percent * 10) / cSentences;
			}

					writer.WriteLine("{0} of {1} ({2}.{3} %%) parsed at least once",
				num_parsed, cSentences, percent_parsed, frac_percent);
					writer.WriteLine();
				}
			}
		}

		/// <summary>
		/// Helper function that closes a native FILE* ptr,
		/// appends some text to the file
		/// and reopens it for append.
		/// </summary>
		/// <param name="filename"></param>
		/// <param name="pexistingFilefileHandle"></param>
		/// <param name="text"></param>
		/// <returns></returns>
		private static IntPtr AppendTextToFile(string filename, IntPtr /*FILE */pexistingFilefileHandle, string text)
		{
			wrappedfclose(pexistingFilefileHandle);
			using (var pfileOutManaged = File.Open(filename, FileMode.OpenOrCreate))
			{
				string temp = string.Format("{0}{1}", text, Environment.NewLine);
			new StreamWriter(pfileOutManaged).Write(temp);
			pfileOutManaged.Close();
			}
			pexistingFilefileHandle = wrappedfopen(filename, "a");
			return pexistingFilefileHandle;
		}

		public void LoadGrammarFile(string bstrGrammarFile)
		{
			// check for valid input
			if (bstrGrammarFile == null)
			{
				throw new ArgumentNullException();
			}

			// convert the filename from 16-bit Unicode to 8-bit ANSI
			IntPtr pszFile = Marshal.StringToHGlobalAnsi(bstrGrammarFile);

			try
			{
				// erase the old grammar and filename
				if (m_data.pGrammar != IntPtr.Zero)
				{
					freePATRGrammar(ref m_data);
				}

				// load the new grammar
				if (loadPATRGrammar(pszFile, ref m_data) == 0)
					throw new ApplicationException("loadPATRGrammar call failed");
			}
			finally
			{
				Marshal.FreeHGlobal(pszFile);
			}
		}

		public void LoadLexiconFile(string bstrLexiconFile, int fAdd)
		{
			LoadLexicon(bstrLexiconFile, fAdd, false);
		}

		public void Clear()
		{
			freePATRGrammar(ref m_data);	/* remove existing grammar */
			freePATRLexicon(ref m_data);	/* remove existing lexicon */
			if (m_ppszLexFiles != null)
			{
				m_ppszLexFiles.Clear();
				m_pbAnaLexFile.Clear();
				m_ppszLexFiles = null;
				m_pbAnaLexFile = null;
				m_uiLexFileCount = 0;
			}
			freePATRInternalMemory(ref m_data);
		}

		public void OpenLog(string bstrLogFile)
		{
			// check for valid input
			if (string.IsNullOrEmpty(bstrLogFile))
			{
				throw new ArgumentException();
			}

			// close the old log file (if any) and open the new
			if (m_data.pLogFP != IntPtr.Zero)
			{
				wrappedfclose(m_data.pLogFP);
				m_data.pLogFP = IntPtr.Zero;
			}

			m_pszLogFile = null;

			IntPtr /*FILE* */ pfile = wrappedfopen(bstrLogFile, "w");
			if (pfile == IntPtr.Zero)
			{
				throw new ApplicationException("Could not open file: " + bstrLogFile);
			}
			// save the log filename and FILE pointer
			m_pszLogFile = bstrLogFile;
			m_data.pLogFP = pfile;
		}

		public void CloseLog()
		{
			if ((m_data.pLogFP == IntPtr.Zero) && string.IsNullOrEmpty(m_pszLogFile))
			{
				throw new ApplicationException("Error could not close log file");
			}
			// close the old log file
			if (m_data.pLogFP != IntPtr.Zero)
			{
				wrappedfclose(m_data.pLogFP);
				m_data.pLogFP = IntPtr.Zero;
			}
			if (!string.IsNullOrEmpty(m_pszLogFile))
			{
				m_pszLogFile = null;
			}
		}

		public string GrammarFile
		{
			get
			{
				return Marshal.PtrToStringAnsi(m_data.pszGrammarFile);
			}
		}

		public string get_LexiconFile(long iFile)
		{
			// check for valid input
			return iFile >= m_uiLexFileCount ? null : m_ppszLexFiles[(int)iFile];
		}

		public string LogFile
		{
			get { return m_pszLogFile; }
		}

		public int Unification
		{
			get
			{
				return m_data.bUnification;
			}
			set
			{
				m_data.bUnification = (byte)value;
			}
		}

		public long TreeDisplay
		{
			get
			{
				return (m_data.eTreeDisplay);
			}
			set
			{
				m_data.eTreeDisplay = (byte)value;
			}
		}

		public long RootGlossFeature
		{
			get
			{
				return m_data.eRootGlossFeature;
			}
			set
			{
				m_data.eRootGlossFeature = (byte)value;
			}
		}

		public int Gloss
		{
			get
			{
				return m_data.bGloss;
			}
			set
			{
				m_data.bGloss = (byte)value;
			}
		}

		public long MaxAmbiguity
		{
			get
			{
				return m_data.iMaxAmbiguities;
			}
			set
			{
				m_data.iMaxAmbiguities = (byte)value;
			}
		}

		public int CheckCycles
		{
			get
			{
				return (m_data.bCheckCycles);
			}
			set
			{
				m_data.bCheckCycles = (byte)value;
			}
		}

		public long CommentChar
		{
			get
			{
				return m_data.cComment;
			}
			set
			{
				m_data.cComment = (byte)value;
			}
		}

		public long TimeLimit
		{
			get
			{
				return (long) m_data.iMaxProcTime;
			}
			set
			{
#if __MonoCS__
				m_data.iMaxProcTime = (System.IntPtr)value;
#else
				m_data.iMaxProcTime = (ulong)value;
#endif
			}
		}

		public int TopDownFilter
		{
			get
			{
				return m_data.bTopDownFilter;
			}
			set
			{
				m_data.bTopDownFilter = (byte)value;
			}
		}

		public int TrimEmptyFeatures
		{
			get
			{
				return (m_data.iFeatureDisplay & (byte)PATRDataDataTypes.iFeatureDisplay.PATR_FEATURE_TRIM);
			}
			set
			{
				if (value != 0)
					m_data.iFeatureDisplay |= (byte)PATRDataDataTypes.iFeatureDisplay.PATR_FEATURE_TRIM;
				else
				{
					int temp = (int)~PATRDataDataTypes.iFeatureDisplay.PATR_FEATURE_TRIM;
					m_data.iFeatureDisplay &= (byte)temp;
				}

			}
		}

		public long DebuggingLevel
		{
			get
			{
				return m_data.iDebugLevel;
			}
			set
			{
				m_data.iDebugLevel = (ushort)value;
			}
		}

		public string LexRecordMarker
		{
			get
			{
				return PtrToString(m_data.pszRecordMarker, m_encoding);
			}
			set
			{
				if (m_data.pszRecordMarker != IntPtr.Zero)
					Marshal.FreeHGlobal(m_data.pszRecordMarker);
				StringToPtr(ref m_data.pszRecordMarker, value, m_encoding);
			}
		}

		public string LexWordMarker
		{
			get
			{
				return PtrToString(m_data.pszWordMarker, m_encoding);
			}
			set
			{
				StringToPtr(ref m_data.pszWordMarker, value, m_encoding);
			}
		}

		public string LexCategoryMarker
		{
			get
			{
				return PtrToString(m_data.pszCategoryMarker, m_encoding);
			}
			set
			{
				StringToPtr(ref m_data.pszCategoryMarker, value, m_encoding);
			}
		}

		public string LexFeaturesMarker
		{
			get
			{
				return PtrToString(m_data.pszFeatureMarker, m_encoding);
			}
			set
			{
				StringToPtr(ref m_data.pszFeatureMarker, value, m_encoding);
			}
		}

		public string LexGlossMarker
		{
			get
			{
				return PtrToString(m_data.pszGlossMarker, m_encoding);
			}
			set
			{
				StringToPtr(ref m_data.pszGlossMarker, value, m_encoding);
			}
		}

		public string LexRootGlossMarker
		{
			get
			{
				return PtrToString(m_data.pszRootGlossMarker, m_encoding);
			}
			set
			{
				StringToPtr(ref m_data.pszRootGlossMarker, value, m_encoding);
			}
		}

		public int TopFeatureOnly
		{
			get
			{
				return ~(m_data.iFeatureDisplay & (byte)PATRDataDataTypes.iFeatureDisplay.PATR_FEATURE_ALL);
			}
			set
			{
				if (value != 0)
				{
					int temp = (int)~PATRDataDataTypes.iFeatureDisplay.PATR_FEATURE_ALL;
					m_data.iFeatureDisplay &= (byte) temp;
				}
				else
				{
					m_data.iFeatureDisplay |= (byte) PATRDataDataTypes.iFeatureDisplay.PATR_FEATURE_ALL;
				}
			}
		}

		public int DisplayFeatures
		{
			get
			{
				return (m_data.iFeatureDisplay & (byte)PATRDataDataTypes.iFeatureDisplay.PATR_FEATURE_ON);
			}
			set
			{
				if (value != 0)
					m_data.iFeatureDisplay |= (byte)PATRDataDataTypes.iFeatureDisplay.PATR_FEATURE_ON;
				else
				{
					int temp = (int)~PATRDataDataTypes.iFeatureDisplay.PATR_FEATURE_ON;
					m_data.iFeatureDisplay &= (byte)temp;
				}
			}
		}

		public int FlatFeatureDisplay
		{
			get
			{
				return (m_data.iFeatureDisplay & (byte)PATRDataDataTypes.iFeatureDisplay.PATR_FEATURE_FLAT);
			}
			set
			{
				if (value != 0)
					m_data.iFeatureDisplay |= (byte)PATRDataDataTypes.iFeatureDisplay.PATR_FEATURE_FLAT;
				else
				{
					int temp = (int) ~PATRDataDataTypes.iFeatureDisplay.PATR_FEATURE_FLAT;
					m_data.iFeatureDisplay &= (byte) temp;
				}
			}
		}

		public int Failures
		{
			get
			{
				return m_data.bFailure;
			}
			set
			{
				m_data.bFailure = (byte)value;
			}
		}

		public long CodePage
		{
			get { return m_encoding.CodePage; }
			set { m_encoding = Encoding.GetEncoding((int) value); }
		}

		public void DisambiguateAnaFile(string bstrInput, string bstrOutput)
		{
			// check for valid input
			if ((bstrInput == null) || (bstrOutput == null))
			{
				throw new ArgumentNullException();
			}

			IntPtr pszInput = Marshal.StringToHGlobalAnsi(bstrInput);
			IntPtr pfileIn = wrappedfopen(bstrInput, "r");

			uint cSentences = 0;
			uint cAmbiguity;
			uint numSentences = 0;
			IntPtr /*WordTemplate ** */ wtp;
			int i;
			var successes = new uint[11];

			IntPtr pszOutput = Marshal.StringToHGlobalAnsi(bstrOutput);

#if !hab130l
		if (m_data.pLogFP != IntPtr.Zero)
			{
			display_header(m_data.pLogFP);
				m_data.pLogFP = AppendTextToFile(LogFile, m_data.pLogFP, String.Format("   Grammar file used: {0}{1}", m_data.pszGrammarFile, Environment.NewLine));
			}
#endif // hab130l


			try
			{
				using (var pfileOutManaged = File.Open(bstrOutput, FileMode.OpenOrCreate))
				{
					string temp = String.Format("\\id Grammar file used: {0}{1}", Marshal.PtrToStringAnsi(m_data.pszGrammarFile),
						Environment.NewLine);
				new StreamWriter(pfileOutManaged).Write(temp);
				pfileOutManaged.Close();
			}
			}
			catch (Exception)
			{
				wrappedfclose(pfileIn);
				throw new ApplicationException("Couldn't create file " + bstrOutput);
			}

			IntPtr pfileOut = wrappedfopen(bstrOutput, "a");
			for (;;)
			{

				wtp = readSentenceOfTemplates(pfileIn, pszInput, m_data.pFinalPunc,
					ref m_sTextCtl, m_data.pLogFP);
				if (wtp == IntPtr.Zero)
					break;
				cAmbiguity = (uint)parseAmpleSentenceWithPATR(wtp, pfileOut, pszOutput,
					0, 0, m_bWriteAmpleParses?1:0, numSentences + 1,
					ref m_sTextCtl, ref m_data);

				if (cAmbiguity < 10)
					++successes[cAmbiguity];
				else
					++successes[10];
				++cSentences;

				++numSentences;
			}

			wrappedfclose(pfileIn);
			Marshal.FreeHGlobal(pszInput);

#if !hab1291
		if (m_data.pLogFP != IntPtr.Zero)
		{
			StringBuilder builder = new StringBuilder();
				builder.AppendLine();
				builder.AppendLine();
				builder.AppendLine(String.Format("File parsing statistics: {0} sentences read", cSentences));
			 for ( i = 0 ; i < 10 ; ++i )
			 {
			  if (successes[i] != 0)
						builder.AppendLine(String.Format("    {0} sentence{1} with {2} parse{3}",
								successes[i], (successes[i] == 1) ? " " : "s", i, (i == 1) ? "" : "s"));
			 }
			 if (successes[10] != 0)
					builder.AppendLine(String.Format("    {0} sentence{1} with 10 or more parses",
											 successes[10], (successes[10] == 1) ? " " : "s"));

			 uint num_parsed;
			 uint percent_parsed;
			 uint frac_percent;
			 num_parsed = cSentences - successes[0];
			 if (cSentences == 0)
			 {
				percent_parsed = 0;
				frac_percent   = 0;
			 }
			 else
			 {
				percent_parsed = (100 * num_parsed) / cSentences;
				frac_percent = (100 * num_parsed) % cSentences;
				frac_percent = (frac_percent * 10) / cSentences;
			 }
				builder.AppendLine(String.Format("{0} of {1} ({2}.{3} %%) parsed at least once",
										 num_parsed, cSentences, percent_parsed, frac_percent));

			m_data.pLogFP = AppendTextToFile(LogFile, m_data.pLogFP, builder.ToString());
		}
#else // hab1291
		fprintf(pfileOut, "\n\nFile parsing statistics: %u sentences read\n",
			cSentences);
		for ( i = 0 ; i < 10 ; ++i )
		{
			if (successes[i])
				fprintf(pfileOut, "    %8u sentence%s with %d parse%s\n",
					successes[i], (successes[i] == 1) ? " " : "s",
					i, (i == 1) ? "" : "s");
		}
		if (successes[10])
			fprintf(pfileOut,
				"    %8u sentence%s with 10 or more parses\n",
				successes[10], (successes[10] == 1) ? " " : "s");

		unsigned num_parsed;
		unsigned percent_parsed;
		unsigned frac_percent;
		num_parsed = cSentences - successes[0];
		if (cSentences == 0)
		{
			percent_parsed = 0;
			frac_percent   = 0;
		}
		else
		{
			percent_parsed = (100 * num_parsed) / cSentences;
			frac_percent = (100 * num_parsed) % cSentences;
			frac_percent = (frac_percent * 10) / cSentences;
		}
		fprintf(pfileOut, "%u of %u (%u.%u %%) parsed at least once\n",
			num_parsed, cSentences, percent_parsed, frac_percent);
#endif // hab1291

			wrappedfclose(pfileOut);
			Marshal.FreeHGlobal(pszOutput);
		}

		public int WriteAmpleParses
		{
			get
			{
				return m_bWriteAmpleParses?1:0;
			}

			set
			{
				m_bWriteAmpleParses = value != 0 ? true : false;
			}
		}

		public void LoadAnaFile(string bstrAnaFile, int fAdd)
		{
			LoadLexicon(bstrAnaFile, fAdd, true);
		}

		public void ReloadLexicon()
		{
			if (m_uiLexFileCount == 0)
				return;

			freePATRLexicon(ref m_data);	/* remove existing lexicon */
			int i;
			uint uiSucceed = 0;
			int bSucceed;
			for (i = 0; i < m_uiLexFileCount; ++i)
			{
				IntPtr tempStr = Marshal.StringToHGlobalAnsi(m_ppszLexFiles[i]);
				if (m_pbAnaLexFile[i])
				{
					bSucceed = loadPATRLexiconFromAmple(tempStr, ref m_sTextCtl,
						ref m_data);
				}
				else
				{
					bSucceed = loadPATRLexicon(tempStr, ref m_data);
				}
				Marshal.FreeHGlobal(tempStr);
				if (bSucceed != 0)
				{
					++uiSucceed;
				}
				else
				{
					m_ppszLexFiles[i] = null;
				}
			}
			if (uiSucceed == 0)
			{
				m_uiLexFileCount = 0;
				throw new ApplicationException("Loading Lexicon failed");
			}
			if (uiSucceed < m_uiLexFileCount)
			{
				int j;
				for (j = 0, i = 0; i < m_uiLexFileCount; ++i)
				{
					if (m_ppszLexFiles[i] != null)
					{
						if (j < i)
						{
							m_ppszLexFiles[j] = m_ppszLexFiles[i];
							m_ppszLexFiles[i] = null;
						}
						++j;
					}
				}
				Debug.Assert(j == uiSucceed);
				m_uiLexFileCount = uiSucceed;
			}
		}

		public long LexiconFileCount
		{
			get
			{
				return m_uiLexFileCount;
			}
		}

		public int PromoteDefaultAtoms
		{
			get
			{
				return m_data.bPromoteDefAtoms;
			}
			set
			{
				m_data.bPromoteDefAtoms = (byte)value;
			}
		}

		public string SentenceFinalPunctuation
		{
			get
			{
				// convert the current set of sentence final punctuation characters
				string dstr = String.Empty;
				IntPtr psl;
				for ( psl = m_data.pFinalPunc ; psl != IntPtr.Zero;)
				{
					StringList strList = ((StringList)Marshal.PtrToStructure(psl, typeof(StringList)));

					if (dstr != String.Empty)
						dstr += ' ';

					dstr += Marshal.PtrToStringAnsi(strList.pszString);
					psl = strList.pNext;
				}

				return dstr;
			}
			set
			{
				// check for valid input
				if (value == null)
					throw new ArgumentNullException();

				string[] toks = value.Split(szWhitespace_g.ToCharArray());
				foreach (string tok in toks)
				{
					IntPtr ptr = IntPtr.Zero;
					StringToPtr(ref ptr, tok, m_encoding);
					addPATRSentenceFinalPunctuation(ref m_data, ptr);
				}
			}
		}

		public int AmplePropertyIsFeature
		{
			get
			{
				return (m_data.bPropIsFeature);
			}
			set
			{
				m_data.bPropIsFeature = (byte)value;
			}
		}

		#endregion
	}

	#region Native Data Types

	/// <summary>
	/// data structure for storing low-level PC-PATR operational data.  an
	/// instance of this data structure may be shared by multiple instances of
	/// PATRData.
	/// </summary>
	internal struct PATRMemory
	{
	/* used in unify.c */
	public IntPtr pSavedFeatures;
	public IntPtr pFreeSavedFeatures;
	public IntPtr pSavedComplex;
	public IntPtr pFreeSavedComplex;
	public IntPtr pSavedDisjunct;
	public IntPtr pFreeSavedDisjunct;
	public int bPreserve;
	/* used in patrfunc.c */
	public IntPtr pStoredStringTable;
	public IntPtr pMultTop;
	public int iCurrent;
	/* used in patalloc.c */
	public ulong cFeatureAlloc;
	public ulong cComplexAlloc;
	public ulong cDisjunctAlloc;
	public ulong cRuleAlloc;
	public ulong cRuleAllocList;
	public ulong cNontermAlloc;
	public ulong cNontermAllocList;
	public ulong cPathAlloc;
	public ulong cHashListAlloc;
	public ulong cEdgeAlloc;
	public ulong cEdgeAllocList;
	public ulong cWordAlloc;
	public ulong cCategAlloc;
	public ulong cDispFeatAlloc;
	public ulong cStringAlloc;
	public IntPtr pFeatureFree;
	public IntPtr pComplexFree;
	public IntPtr pDisjunctFree;
	public IntPtr pRuleFree;
	public IntPtr pRuleListFree;
	public IntPtr pNontermFree;
	public IntPtr pNontermListFree;
	public IntPtr pPathFree;
	public IntPtr pHashListFree;
	public IntPtr pEdgeFree;
	public IntPtr pEdgeListFree;
	public IntPtr pWordFree;
	public IntPtr pCategFree;
	public IntPtr pDispFeatFree;
	public IntPtr pStringFree;
	public IntPtr pGarbage;
	public IntPtr pGarbageFree;
	/* used in copyfeat.c */
	public IntPtr pFeatureCopyList;
	/* used in patrlexi.c */
	public IntPtr pHeadPATRLexItemArrays;
	public IntPtr pTailPATRLexItemArrays;
	public uint iPATRLexItemsAvailable;
	public IntPtr pHeadPATRLexCharArrays;
	public IntPtr pTailPATRLexCharArrays;
	public uint iPATRLexCharsAvailable;
	public IntPtr pHeadPATRLexShortArrays;
	public IntPtr pTailPATRLexShortArrays;
	public uint iPATRLexShortsAvailable;
	/* used in patrstrg.c and userpatr.c */
	public DynString dstrOutput;
	/* used in userpatr.c */
	public IntPtr pPrintsFP;
	public int iNextPosition;
	public int bMoreTree;
	public int	iPrintDepth;
	};

	internal struct DynString
	{
			public IntPtr pszBuffer;
			public int cbLen;
			public int cbAlloc;
			public int bError;
	};

	internal class PATRDataDataTypes
	{
		public enum eTreeDisplay
		{	PATR_NO_TREE = 0,
			PATR_FLAT_TREE = 1,
			PATR_FULL_TREE = 2,
			PATR_INDENTED_TREE = 3,
			PATR_XML_TREE = 4
		}

		public enum iFeatureDisplay
		{	PATR_FEATURE_ON = 0x01,
			PATR_FEATURE_FLAT = 0x02, // "flat" or "full" format
			PATR_FEATURE_ALL = 0x10, // all features, or only top one
			PATR_FEATURE_TRIM = 0x20, // show null features explicitly
			PATR_FEATURE_ONCE = 0x40 // xpand coindexed once (first time), or every time
		}

		public const char PATR_DEFAULT_COMMENT = ';';

		public enum eRootGlossFeature
		{
			PATR_ROOT_GLOSS_NO_FEATURE = 0, // no rootgloss feature
			PATR_ROOT_GLOSS_ON = 1, // use rootgloss feature

			// following use rootgloss feature & are
			// special cases for when the input is an
			// ANA file

			PATR_ROOT_GLOSS_LEFT_HEADED = 2, // use leftmost ANA root
			PATR_ROOT_GLOSS_RIGHT_HEADED = 3, // use rightmost ANA root
			PATR_ROOT_GLOSS_USE_ALL = 4, // use all ANA roots

		}
	}

	/// <summary>
	/// data structure for storing PC-PATR control data
	/// </summary>
#if !__MonoCS__ // Windows 32 bit
	[StructLayout(LayoutKind.Explicit, Size=112, CharSet=CharSet.Ansi)]
	internal struct PATRData
	{
		[FieldOffset(0)]
		public byte bFailure;	/* display parser failures */
		[FieldOffset(1)]
		public byte bUnification;	/* enable unification */
		[FieldOffset(2)]
		public byte eTreeDisplay;	/* tree display mode */
		[FieldOffset(3)]
		public byte bGloss;		/* display glosses (if they exist) */
		[FieldOffset(4)]
		public byte bGlossesExist;	/* signal whether glosses exist */
		[FieldOffset(5)]
		public byte iFeatureDisplay; /* feature display mode bits */

		[FieldOffset(6)]
		public byte bCheckCycles;	/* enable checking for parse cycles */
		[FieldOffset(7)]
		public byte bTopDownFilter;	/* enable top down filtering */
		[FieldOffset(8)]
		public ushort iMaxAmbiguities; /* max number of ambiguities to show */
		[FieldOffset(10)]
		public ushort iDebugLevel;	/* degree of debugging output desired */
		[FieldOffset(12)]
		public byte cComment;	/* begins a comment in an input line */
		[FieldOffset(13)]
		public byte bSilent;	/* disable messages to stderr */
		[FieldOffset(14)]
		public byte bShowWarnings;	/* enable warnings as well as error messages */
		[FieldOffset(15)]
		public byte bPromoteDefAtoms; /* promote default atoms before parsing */
		[FieldOffset(16)]
		public byte bPropIsFeature;	/* AMPLE property is feature template name */
		[FieldOffset(17)]
		public byte eRootGlossFeature; /* rootgloss feature mode*/

	/*
	 *  flag that we don't really care about all the parse results,
	 *  just that at least one parse succeeds.
	 */
		[FieldOffset(18)]
		public byte bRecognizeOnly;
		[FieldOffset(24)]
		public ulong /*time_t*/ iMaxProcTime;	/* max number of seconds to process */
		[FieldOffset(32)]
		public IntPtr pLogFP;
		[FieldOffset(36)]
		public IntPtr pFinalPunc;	/* sentence final punctuation chars */
		[FieldOffset(40)]
		public IntPtr /* was const char **/	pszGrammarFile;
		[FieldOffset(44)]
		public IntPtr pGrammar;
		[FieldOffset(48)]
		public int iGrammarSelection;	/* used in garbage collection*/
	/*
	 *  field markers for the lexicon file
	 */
		[FieldOffset(52)]
		public IntPtr /* was const char **/	pszRecordMarker;
		[FieldOffset(56)]
		public IntPtr /* was const char **/	pszWordMarker;
		[FieldOffset(60)]
		public IntPtr /* was const char **/	pszGlossMarker;
		[FieldOffset(64)]
		public IntPtr /* was const char **/	pszCategoryMarker;
		[FieldOffset(68)]
		public IntPtr /* was const char **/ pszFeatureMarker;
		[FieldOffset(72)]
		public IntPtr /* was const char **/ pszRootGlossMarker;
		[FieldOffset(76)]
		public IntPtr pLexicon;
	/*
	 *  values used for internal processing
	 */
		[FieldOffset(80)]
		public int iCurrentIndex;	/* index number of current edge */
		[FieldOffset(84)]
		public int iParseCount;	/* number of parses found */
		[FieldOffset(88)]
		public IntPtr pMem;		/* can be shared by multple PATRData */
		[FieldOffset(92)]
		public ulong uiEdgesAdded;
		[FieldOffset(96)]
		public ulong /*time_t*/ iStartTime;
		[FieldOffset(104)]
		public IntPtr pStartGarbage;
};

#else // Linux
	[StructLayout(LayoutKind.Sequential, Pack = 0)]
	internal struct PATRData
	{
		public byte bFailure;	/* display parser failures */
		public byte bUnification;	/* enable unification */
		public byte eTreeDisplay;	/* tree display mode */
		public byte bGloss;		/* display glosses (if they exist) */
		public byte bGlossesExist;	/* signal whether glosses exist */
		public byte iFeatureDisplay; /* feature display mode bits */

		public byte bCheckCycles;	/* enable checking for parse cycles */
		public byte bTopDownFilter;	/* enable top down filtering */
		public ushort iMaxAmbiguities; /* max number of ambiguities to show */
		public ushort iDebugLevel;	/* degree of debugging output desired */
		public byte cComment;	/* begins a comment in an input line */
		public byte bSilent;	/* disable messages to stderr */
		public byte bShowWarnings;	/* enable warnings as well as error messages */
		public byte bPromoteDefAtoms; /* promote default atoms before parsing */
		public byte bPropIsFeature;	/* AMPLE property is feature template name */
		public byte eRootGlossFeature; /* rootgloss feature mode*/

	/*
	 *  flag that we don't really care about all the parse results,
	 *  just that at least one parse succeeds.
	 */
		public byte bRecognizeOnly;
		public IntPtr /*time_t*/ iMaxProcTime;	/* max number of seconds to process */
		public IntPtr pLogFP;
		public IntPtr pFinalPunc;	/* sentence final punctuation chars */
		public IntPtr /* was const char **/	pszGrammarFile;
		public IntPtr pGrammar;
		public IntPtr iGrammarSelection;	/* used in garbage collection*/
	/*
	 *  field markers for the lexicon file
	 */
		public IntPtr /* was const char **/	pszRecordMarker;
		public IntPtr /* was const char **/	pszWordMarker;
		public IntPtr /* was const char **/	pszGlossMarker;
		public IntPtr /* was const char **/	pszCategoryMarker;
		public IntPtr /* was const char **/ pszFeatureMarker;
		public IntPtr /* was const char **/ pszRootGlossMarker;
		public IntPtr pLexicon;
	/*
	 *  values used for internal processing
	 */
		public int iCurrentIndex;	/* index number of current edge */
		public int iParseCount;	/* number of parses found */
		public IntPtr pMem;		/* can be shared by multple PATRData */
		public IntPtr uiEdgesAdded;
		public IntPtr /*time_t*/ iStartTime;
		public IntPtr pStartGarbage;
	};
#endif

	/// <summary>
	/// pc-parse/opaclib/textctl.h
	/// </summary>
#if !__MonoCS__ // Windows 32 bit
	[StructLayout(LayoutKind.Explicit, Size=52, CharSet=CharSet.Ansi)]
	internal struct TextControl
	{
		[FieldOffset(0)]
		public IntPtr pszTextControlFile; // name of file the data is loaded from (cstyle string)
		[FieldOffset(4)]
		public IntPtr pLowercaseLetters; /* \luwfc, \luwfcs (input) */
		[FieldOffset(8)]
		public IntPtr pUppercaseLetters; /*    "       "       "    */
		[FieldOffset(12)]
		public IntPtr pCaselessLetters; /* \wfc, \wfcs (input) */
		[FieldOffset(16)]
		public IntPtr pOrthoChanges; /* \ch (input) */
		[FieldOffset(20)]
		public IntPtr pOutputChanges; /* \ch (output) */
		[FieldOffset(24)]
		public IntPtr pIncludeFields; /* \incl (input) */
		[FieldOffset(28)]
		public IntPtr pExcludeFields; /* \excl (input) */
		[FieldOffset(32)]
		public byte cFormatMark; //char		/* \format (input) */
		[FieldOffset(33)]
		public byte cAnaAmbig; /* \ambig (database file) */
		[FieldOffset(34)]
		public byte cTextAmbig; /* \ambig (text file) */
		[FieldOffset(35)]
		public byte cDecomp; /* \dsc (input) */
		[FieldOffset(36)]
		public byte cBarMark; /* \barchar (input) */
		[FieldOffset(40)]
		public IntPtr pszBarCodes; /* \barcodes (input) */
		[FieldOffset(44)]
		public byte bIndividualCapitalize; /* \noincap (input) */
		[FieldOffset(45)]
		public byte bCapitalize; /* \nocap (input) */
		[FieldOffset(48)]
		public uint uiMaxAmbigDecap; /* \maxdecap (input) */
	};
#else // Linux
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	internal unsafe struct TextControl
	{
		public IntPtr pszTextControlFile; // name of file the data is loaded from (cstyle string)
		public IntPtr pLowercaseLetters; /* \luwfc, \luwfcs (input) */
		public IntPtr pUppercaseLetters; /*    "       "       "    */
		public IntPtr pCaselessLetters; /* \wfc, \wfcs (input) */
		public IntPtr pOrthoChanges; /* \ch (input) */
		public IntPtr pOutputChanges; /* \ch (output) */
		public IntPtr pIncludeFields; /* \incl (input) */
		public IntPtr pExcludeFields; /* \excl (input) */
		public byte cFormatMark; //char		/* \format (input) */
		public byte cAnaAmbig; /* \ambig (database file) */
		public byte cTextAmbig; /* \ambig (text file) */
		public byte cDecomp; /* \dsc (input) */
		public fixed byte cBarMark[4]; /* \barchar (input) */
		public IntPtr pszBarCodes; /* \barcodes (input) */
		public byte bIndividualCapitalize; /* \noincap (input) */
		public fixed byte bCapitalize[3]; /* \nocap (input) */
		public uint uiMaxAmbigDecap; /* \maxdecap (input) */
	}
#endif

	/// <summary>
	/// from patr.h
	/// </summary>
	internal struct PATRWord
	{
		public int iWordNumber;	/* Number of word in sentence */
		public IntPtr /*char **/		pszWordName;	/* the spelling of the word */
		public IntPtr /*PATRWordCategory **/	pCategories;	/* Pointer to category list */
		public IntPtr /*struct patr_word **/	pNext;		/* Link to next word in sentence */
	};

	/// <summary>
	///  data structure for list of pointers to parse chart edges
	/// </summary>
	[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi)]
	internal struct PATREdgeList
	{
		public IntPtr /* PATREdge * */ pEdge;
		public IntPtr /* struct patr_edge_list * */	pNext;
	};

	/// <summary>
	/// structure for a linked list of NUL-terminated character strings
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	internal struct StringList
	{
		public IntPtr /*char **/		pszString;	/* stored string */
		public IntPtr pNext;		/* next node in linked list */
	};
	#endregion
}
