#define DontAddVersionNumbersToXMLFilename
#define UseXmlFilesForPlugins
// #define AssemblyChecking

using System;
using System.Runtime.InteropServices;   // for the class attributes
using System.Collections;               // for Hashtable
using System.Collections.Generic;       // for several maps
using System.ComponentModel;            // for description attributes
using Microsoft.Win32;                  // for RegistryKey
using System.Resources;                 // for ResourceManager
using System.Runtime.Remoting;          // for ObjectHandle
using System.Windows.Forms;             // for MessageBox (for showing compiler errors)
using System.Xml;						// for XmlDataDocument
using System.IO;                        // for FileNotFoundException
using System.Diagnostics;               // for Debug.Assert
using System.Reflection;				// for Assembly
using ECInterfaces;                     // for IEncConverters
using System.Text;                      // for Encoding

namespace SilEncConverters31
{
	/// <summary>
	/// Encoding Conversion Repository Class
	/// </summary>
	// use interface type 'none' to force vba to not save disps and to force our explicit
	//  interface above to be the default interface.
	[ClassInterface(ClassInterfaceType.None)]
	[GuidAttribute("3E7D37DE-7EB4-4ad3-8292-CBF1F2FECD5F")]
	public class EncConverters : Hashtable, IEncConverters
	{
		#region Const Definitions
		// private registry access key string constants
		public const string BY_PROCESS_TYPE = "ByProcessType";
#if UseXmlFilesForPlugins
		public const string SEC_ROOT_KEY = @"SOFTWARE\SIL\SilEncConverters31";
		public const string strDefPluginFolder = @"\SIL\EC\Plugins";
		public const string strDefPluginFolderVersionPrefix = @"\IEC";
		public const string strRegKeyPluginFolder = "PluginDir";
#else
		public const string HKLM_CNVTRS_SUPPORTED       = @"SOFTWARE\SIL\SilEncConverters31\ConvertersSupported";
		public const string BY_EXTENSION                = "ByExtension";
		public const string BY_NAME                     = "ByName";
		public const string PRIORITY                    = "Priority";
		public const string strRegKeyForProgId          = "";
		public const string strRegKeyForFriendlyName    = "DisplayName";
		public const string strRegKeyForConfigProgId    = "Configurator";
#endif
		public const string strRegKeyForSelfRegistering = "RegisterSelf";
		internal const string strShowToolTipsStateKey   = "ShowToolTips";

		// implement types define in EncCnvtrs.dll (public so users can use them in .Net
		//  code rather than hard-coding the strings)
		public const string strTypeImplCOM              = "COM";
		public const string strTypeSILcc                = "SIL.cc";
		public const string strTypeSILtec               = "SIL.tec";
		public const string strTypeSILtecForm           = "SIL.tecForm";
		public const string strTypeSILmap               = "SIL.map";
		public const string strTypeSILcp                = "cp";
		public const string strTypeSILcomp              = "SIL.comp";
		public const string strTypeSILicuTrans          = "ICU.trans";
		public const string strTypeSILicuConv           = "ICU.conv";
		public const string strTypeSILicuRegex          = "ICU.regex";
		public const string strTypeSILPyScript          = "SIL.PyScript";
		public const string strTypeSILPerlExpression    = "SIL.PerlExpression";
		public const string strTypeSILfallback          = "SIL.fallback";
		public const string strTypeSILadaptit           = "SIL.AdaptItKB";
		public const string strTypeSILadaptitGuesser    = "SIL.AdaptItKBGuesser";
		public const string cstrTempConverterPrefix     = "Temporary Converter";

		// default values for XML file attributes
		public const string HKLM_PATH_TO_XML_FILE       = @"SOFTWARE\SIL\EncodingConverterRepository";
		public const string strRegKeyForStorePath       = "Registry";
		public const string strRegKeyForMovingRepository = "MoveRepositoryTo";  // should contain the data (e.g. C:\Documents and Settings\All Users\Application Data)
		public const string strDefXmlPath               = @"\SIL\Repository"; // (from \pf\cf...)
		public const string strDefMapsTablesPath        = @"\SIL\MapsTables";

		public  const string strDefXmlFilename          = @"\mappingRegistry.xml";
		private const string strDefXmlNamespace         = @"http://www.sil.org/computing/schemas/SILMappingRegistry.xsd";
		private const string strDefDirection            = "both";
		private const string strForward_only_Direction  = "forward-only";
		private const string strDefTypeBytes            = "bytes";
		private const string strDefTypeUnicode          = "unicode";
		private const string strDefNormalizeFlagNone    = "none";
		private const string strDefNormalizeFlagFC      = "NFC";
		private const string strDefNormalizeFlagFD      = "NFD";
		private const string strDefAttrNameEncName      = "name";
		private const string strDefAttrNameBecomes      = "becomes";
		private const string strDefAttrNameDirection    = "reverse";
		private const string strDefAttrNameCodePage     = "cp";
		private const string strDefProcessTypeName      = "processType";
		private const int cnDefCodePage                 = 0;
		public const int cnSymbolFontCodePage           = 42;
		public const int cnIso8859_1CodePage            = 28591;    // as a substitute for symbol font code page (which isn't supported by .Net or most windows systems)
		private const int cnDefImplPriority = 0;
		public const string strDefUnicodeEncoding       = "UNICODE";
		public const string cstrCaption                 = "EncConverters";

		// The following is the format string used to 'build' the name of a mapping which has
		//  multiple specs (e.g. "Annapurna<>UNICODE (SIL.cc)" and "Annapurna<>UNICODE (SIL.tec)")
		// if you change the following format statement, then you'll probably have to fix the
		//  GetMappingNameEx method (which assumes that the last char of a 'built' name is a
		//  ")" character; when checking for an imbedded implementation type.
		private const string strMapPlusImplFormat       = "{0} ({1})";  // for String.Format
		#endregion Const Definitions

		#region Member Variable Definitions
		// have a hashtable with some additional aliases (basically, if the user
		//  has two different specs for the same mapping, then they will be called, for
		//  example, "Annapurna (SIL.tec)" AND "Annapurna (SIL.cc)". But we also want to
		//  be query-able based on the base name "Annapurna" as well). So the highest
		//  priority conversion implementation (e.g. TECkit is higher than CC) will be
		//  in this map with the base name.
		private Hashtable m_mapAliasNames = new Hashtable();
		private Hashtable m_mapImplementTypesPriority = new Hashtable();

		// prog-id lookup helpers
		// this code was originally written to use Hashtables. I tried making it a Dictionary, but then started
		//  getting some exceptions due to keys not being present (i.e. Hashtables (I think) return null when the
		//  key doesn't exist, whereas Dictionary<>'s throw an exception. So... I'm afraid I'll break something if
		//  I change it... so keeping hashtables for these two.
		private static Hashtable m_mapImplTypeToProgId = new Hashtable();   // prog-ids based on implementTypes
		private static Hashtable m_mapToImplType = new Hashtable();         // implementTypes based on file extensions or ProcessTypeFlags
		private static Hashtable m_mapDisplayNameToProgID = new Hashtable(); // dictionary of <friendly names> = <ProgIDs>
		internal static Dictionary<string, string> m_mapProgIdsToAssemblyName = new Dictionary<string, string>(); // dictionary of prog ids to assembly version full name (so we can load from a specific assembly)
		private Int32               m_dwProcessTypeConverters = 0;

		// trace switch
		private TraceSwitch traceSwitch = new TraceSwitch(typeof(EncConverters).FullName, "General Tracing", "Off");
		#endregion Member Variable Definitions

		#region Initialization
		/// <summary>
		/// Object wrapper for the Encoding Converter's repository. This is the starting point for most
		/// EncConverters-related functions (e.g.
		///
		/// EncConverters aECs = new EncConverters();
		/// string strOutput = aECs["Annapurna<>UNICODE"].Convert(strInput);
		/// </summary>
		public EncConverters()
		{
#if AssemblyChecking
			MessageBox.Show(String.Format("You are listening to:{0}{0}'{1}'{0}{0}implemented in assembly:{0}{0}'{2}'{0}{0}executing from:{0}{0}'{3}'",
				Environment.NewLine,
				typeof(EncConverters).FullName,
				typeof(EncConverters).Assembly,
				typeof(EncConverters).Assembly.CodeBase));
#endif

			// get the names and prog ids of the supported conversion engine (wrappers)
			//  from the registry.
			GetConversionEnginesSupported();

			// load the "actual converters" (i.e. those supported by implementors of the
			//  IEncConverter interface)
			AddActualConverters();
		}

		// this ctor must have a different call spec. It is used to create one of these objects
		//  that *isn't* initialize by reading the XML file (for use by 'ByEncodingId', etc.)
		/// <summary>
		/// Constructor for when you don't need the collection initialized (e.g. if you're just acquiring one to get a
		/// new, blank converter instance of a particular type and don't really care what's in the repository)
		///
		/// EncConverters aECs = new EncConverters(true);
		/// IEncConverter aIcuRegex = aECs.NewEncConverterByImplementationType(EncConverters.strTypeSILicuRegex);
		/// aIcuRegex.Initialize...
		/// </summary>
		/// <param name="bEmptyCollection">dummy parameter to distinguish from other ctor--value is ignored</param>
		public EncConverters(bool bEmptyCollection)
		{
			// get the names and prog ids of the supported conversion engine (wrappers)
			//  from the registry.
			GetConversionEnginesSupported();
		}

		/// <summary>
		/// Reinitialize()
		/// Re-load everything from the repository data store.
		/// </summary>
		public void Reinitialize()
		{
			// first clear everything out
			base.Clear();
			m_mapAliasNames.Clear();
			m_mapImplementTypesPriority.Clear();
			m_mapImplTypeToProgId.Clear();
			m_mapToImplType.Clear();
			m_mapDisplayNameToProgID.Clear();

			// then re-load everything
			GetConversionEnginesSupported();
			AddActualConverters();
		}

		internal   void AddActualConverters()
		{
			// create the 'data set' that goes with our schema and read the file (also get
			// an xmldoc so we can do some xpath queries to retrieve encoding/font info).
			XmlDataDocument xmlDoc;
			XmlNamespaceManager nsmgr;
			mappingRegistry file = RepositoryFile;
			GetXmlDataDocument(file, out xmlDoc, out nsmgr);

			// for every 'mapping'...
			ArrayList aRemList = new ArrayList();   // keep track of mal-formed ones for later removal
			foreach(mappingRegistry.mappingRow aMapRow in file.mapping)
			{
				try
				{
					// Some information is common to all specs (e.g. the 'encoding'
					//  and related 'font' nodes)
					int nCodePageInput = cnDefCodePage, nCodePageOutput = cnDefCodePage;
					string strLeftEncoding, strRightEncoding;
					GetEncodingFontDetails(xmlDoc, nsmgr, aMapRow.name, out strLeftEncoding, out strRightEncoding, out nCodePageInput, out nCodePageOutput);

					// ... for every 'spec' within that mapping...
					InsureSpecsRow(file,aMapRow);
					mappingRegistry.specRow[] aSpecRows = aMapRow.GetspecsRows()[0].GetspecRows();
					bool bMoreThanOne = (aSpecRows.Length > 1);
					string strFavoriteImplementation = null;
					int	nPriorityLastImplementation = -1;
					foreach(mappingRegistry.specRow aSpecRow in aSpecRows)
					{
						// from the 'mapping' node, we knew about left & right type (bytes|unicode)
						// from the 'spec' node(s), we know about direction (both|forward-only)
						string strDirection = ((aSpecRow.IsdirectionNull()) ? strDefDirection : aSpecRow.direction);
						string leftType = ((aMapRow.IsleftTypeNull()) ? strDefTypeBytes : aMapRow.leftType);
						string rightType = ((aMapRow.IsrightTypeNull()) ? strDefTypeUnicode : aMapRow.rightType);
						ConvType eConvType = ToConvType(strDirection,leftType,rightType);
						int processType = GetProcessType(aSpecRow);
						string strProgID = ToProgId(aSpecRow.type);
						string strConverterIdentifier = ((aSpecRow.IspathNull()) ? null : aSpecRow.path);

						// our convention is that if there's more than one spec for the same
						//  mapping, then we concatenate the implementType onto the mapping name
						//  to derive the collection index
						string strConverterKey = aMapRow.name;
						if( bMoreThanOne )
						{
							strConverterKey = BuildConverterSpecNameEx(strConverterKey,aSpecRow.type);
							// this file need not contain only conversion engines that EncConverters
							//	supports:
							//	Debug.Assert(m_mapImplementTypesPriority[aSpecRow.type] != null);
							int nPriorityThis = GetImplPriority(aSpecRow.type);
							if( nPriorityThis > nPriorityLastImplementation )
							{
								nPriorityLastImplementation = nPriorityThis;
								strFavoriteImplementation = strConverterKey;
							}
						}

						// if this a compound converter, we treat it differently.
						if(     (strProgID == typeof(CmpdEncConverter).FullName)
							||  (strProgID == typeof(FallbackEncConverter).FullName) )
						{
							CmpdEncConverter aCmpdEC = null;
							if( strProgID == typeof(CmpdEncConverter).FullName )
								aCmpdEC = new CmpdEncConverter();
							else
								aCmpdEC = new FallbackEncConverter();

							InitializeConverter(aCmpdEC, strConverterKey, strConverterIdentifier,
								ref strLeftEncoding, ref strRightEncoding, ref eConvType,
								ref processType, nCodePageInput, nCodePageOutput, false);

							// it should be the case that all steps must be earlier in the xml
							//	file than the compound converter entry (or the steps couldn't
							//	have been added), so we should be able to find all the steps now.
							//  (ps. if the user subsequently deletes the converter that was one
							//  of the steps, it is cascade deleted from the steps as well...)
							InsureStepsRow(file,aSpecRow);
							foreach(mappingRegistry.stepRow aStep in aSpecRow.GetstepsRows()[0].GetstepRows())
							{
								IEncConverter aECStep = this[aStep.name];
								bool bReverse = ((aStep.IsreverseNull()) ? false : aStep.reverse);
								NormalizeFlags nf = ((aStep.IsnormalizeNull()) ? NormalizeFlags.None : ToNormalizeFlags(aStep.normalize));
								aCmpdEC.AddConverterStep(aECStep, !bReverse, nf);
							}
						}
						else
						{
							// otherwise, initialize and add the converter to the collection.
							try
							{
								AddEx(strProgID, strConverterKey, strConverterIdentifier,
									ref strLeftEncoding, ref strRightEncoding, ref eConvType,
									ref processType, nCodePageInput, nCodePageOutput, false);
							}
							// it might be an implementation EncConverters doesn't support.
							//  In any case, don't ever throw from the ctor or it means the
							//  repository can never be constructed and therefore never be
							//  'clear'd even to start from scratch.
#if AssemblyChecking
							catch (Exception e)
							{
								MessageBox.Show("Key = \"" + strConverterKey +
									"\", Ident = \"" + strConverterIdentifier + "\": " + e.Message, traceSwitch.DisplayName);
							}
#else
#if !DEBUG
							catch {}
#else
							catch(Exception e)
							{
								// catch it in Debug mode, so we can check it
								System.Diagnostics.Debug.WriteLineIf(traceSwitch.TraceError, "Key = \"" + strConverterKey +
									"\", Ident = \"" + strConverterIdentifier + "\": " + e.Message, traceSwitch.DisplayName);
							}
#endif
#endif
						}
					}

					// if there were multiple specs, pick the one we like best and make an alias
					//	entry for it.
					if( bMoreThanOne )
					{
						m_mapAliasNames[aMapRow.name] = this[strFavoriteImplementation];
					}
				}
				catch
				{
					// if there was any mal-formed-ness about that record, then add it to
					//  a different array so we can iterate that and remove it from the file
					aRemList.Add(aMapRow);
				}
			}

			// finally, if we removed any because they were mal-formed, then remove them here
			if( aRemList.Count > 0 )
			{
				foreach(mappingRegistry.mappingRow aRemMapRow in aRemList)
				{
					foreach(mappingRegistry.mappingRow aMapRow in file.mapping)
						if( aRemMapRow == aMapRow )
						{
							aMapRow.Delete();
							break;
						}
				}

				WriteRepositoryFile(file);
			}
		}

#if UseXmlFilesForPlugins
		// Get the prog ids of conversion engines supported from the registry (and update
		//  the implementation details if they've changed in the registry)
		protected void GetConversionEnginesSupported()
		{
			// create the 'data set' that goes with our schema and read the file (so we can
			//  check to see if any of the implementation details have changed.
			mappingRegistry file = RepositoryFile;

			// see if the repository stuff has been moved
			bool bRewriteFile = false;  // means we must re-write the XML file because something changed.
			RegistryKey keyRepositoryMoved = Registry.LocalMachine.OpenSubKey(EncConverters.HKLM_PATH_TO_XML_FILE);
			if (keyRepositoryMoved != null)
			{
				string strNewRepositoryRoot = (string)keyRepositoryMoved.GetValue(strRegKeyForMovingRepository);
				if (!String.IsNullOrEmpty(strNewRepositoryRoot))
				{
					// here we have to fixup the paths that used to be [CommonProgramFiles]\SIL\MapsTables
					// so that they are now [CommonAppData]\SIL\MapsTables
					string strOriginalPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles);
					strOriginalPath += strDefMapsTablesPath;

					string strNewPath = strNewRepositoryRoot + strDefMapsTablesPath;
					if (!Directory.Exists(strNewPath))
						Directory.CreateDirectory(strNewPath);

					foreach (mappingRegistry.specRow aSpecRow in file.spec)
					{
						int nIndex = aSpecRow.path.IndexOf(strOriginalPath, StringComparison.OrdinalIgnoreCase);
						if (nIndex != -1)
						{
							// fixup
							string strNewPathSpec = strNewPath + aSpecRow.path.Substring(strOriginalPath.Length);

							// but before we do, let's make sure it exists
							if (!File.Exists(strNewPathSpec))
							{
								if (File.Exists(aSpecRow.path))
								{
									File.Move(aSpecRow.path, strNewPathSpec);
									aSpecRow.path = strNewPathSpec;
									bRewriteFile = true;
								}
							}
							else
							{
								aSpecRow.path = strNewPathSpec;
								bRewriteFile = true;
							}
						}
					}

					// we should delete this value then so we don't keep doing this
					RegistryKey keyDeletable = Registry.LocalMachine.OpenSubKey(EncConverters.HKLM_PATH_TO_XML_FILE, true);
					if (keyDeletable != null)
						keyDeletable.DeleteValue(strRegKeyForMovingRepository, false);
				}
			}

			// have a map for the XML file implementation details so we can more easily
			//  compare with what's in the registry.
			Hashtable mapImplementTypesUse = new Hashtable();

			// read the XML details
			mappingRegistry.platformRow aPlatformRow = file.platform.FindByname(strTypeImplCOM);
			if (aPlatformRow != null)
				foreach (mappingRegistry.implementRow aImplRow in aPlatformRow.GetimplementRows())
				{
					mapImplementTypesUse[aImplRow.type] = aImplRow.use;
					int priority = ((aImplRow.IspriorityNull()) ? cnDefImplPriority : aImplRow.priority);
					m_mapImplementTypesPriority[aImplRow.type] = priority;
				}

			InsureImplementationsRow(file);
			mappingRegistry.implementationsRow aImplsRow = file.implementations[0];

			// now open and read the information about which plug-ins are installed (based on the
			//  xml files that contain this information). Each COM wrapper will put an xml file in
			//  the strDefPluginFolder folder which gives information about the plug-in indicating,
			//   1) the program id of the COM wrapper (i.e. how we can create one without knowing it's
			//      class details; i.e. since it could be a plug-in from some other app/dll),
			//   2) how it is known (i.e. is it file-based in which case we can do some simplifying
			//      implementation type mapping; e.g. .map/.tec file extensions correspond with the
			//      SIL.tec/.map implementation types (see 'Add' method for where this is used); or
			//      whether it is known by a defining process type; a hack for backwards compatibility,
			//      but useful for other functionality as well--see EnumByProcessType)
			//   3) the priority this conversion engine should be given (e.g. if we have two specs that
			//      do the same mapping, we'll use the highest priority one unless the user asks
			//      specifically for one or the other), and
			//   4) the assembly loader details so that we can distinguish between different versions
			//      of a .Net assembly (e.g. SilEncConverters31, Version=3.0.0.0, Culture=neutral, PublicKeyToken=f1447bae1e63f485
			//      means load it out of that specific assembly)
			// On development machines, the folder for these files is based on the registry key:
			//  HKLM\SOFTWARE\SIL\SilEncConverters31[PluginDir] = <e.g. "C:\src\fw\lib\release\Plugins">
			string strPluginXmlFilesFolder = null;
			RegistryKey keyPluginFolder = Registry.LocalMachine.OpenSubKey(EncConverters.SEC_ROOT_KEY);
			if (keyPluginFolder != null)
				strPluginXmlFilesFolder = (string)keyPluginFolder.GetValue(EncConverters.strRegKeyPluginFolder);

			if (String.IsNullOrEmpty(strPluginXmlFilesFolder))
				strPluginXmlFilesFolder = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + strDefPluginFolder;

			strPluginXmlFilesFolder += strDefPluginFolderVersionPrefix + typeof(IEncConverter).Assembly.GetName().Version.ToString();

			System.Diagnostics.Debug.Assert(Directory.Exists(strPluginXmlFilesFolder), String.Format("Can't find the plug-in folder, '{0}'", strPluginXmlFilesFolder));
			string[] astrPluginXmlFiles = Directory.GetFiles(strPluginXmlFilesFolder, "*.xml");
			System.Diagnostics.Debug.Assert(astrPluginXmlFiles.Length > 0, String.Format(@"You don't have any plug-ins installed (e.g. {0}\SilEncConverters31 Plugin Details.xml)", strPluginXmlFilesFolder));

			// keep track (temporarily) of the version of the implementing assembly from which we get a particular
			//  implementation
			foreach (string strPluginXmlFile in astrPluginXmlFiles)
			{
				EncConverterPlugins aECPluginFile = new EncConverterPlugins();
				try
				{
					aECPluginFile.ReadXml(strPluginXmlFile);
				}
				catch
				{
					continue;   // skip this one if we can't read it
				}

				int priorityReg = 0;
				foreach (EncConverterPlugins.ECPluginDetailsRow aDetailsRow in aECPluginFile.ECPluginDetails)
				{
					string strImplementationType = aDetailsRow.ImplementationName;

					try
					{
						// [note: if this version of the repository is based on IEncConverter v3.0.0.0 (as
						//  defined in the ECInterfaces assembly), then it can only use an identical versioned
						//  implementation of one of the COM wrappers. That is, it cannot use an implementation
						//  of SIL.tec based off of version 3.0.0.1 of IEncConverter (it will throw an
						//  exception if we try to cast such an interface having built this assembly based
						//  on the 3.0.0.0 version of IEncConverter [.Net treats these two versions as
						//  different even if they have the same prototypes)]. This is why the xml plugin
						//  files will be in a version # sub-folder (e.g. 3.0.0.0) of the Plugins sub-folder
						//  where that version # corresponds to the version of IEncConverter.
						//
						// Problem scenario:
						//  However, even for the same version of the IEncConverter interface, it's possible
						//  that there'll be a plug-in definition for two different implementations of the
						//  same implementation type (e.g. SIL.tec from a v3.0.0.0 of this assembly and another
						//  newer implementation (e.g. bugs fixed) in v3.0.0.1 of this assembly)--both of which
						//  will be in the same plug-ins folder, because they both deal with the same version
						//  of the IEncConverter interface (e.g. 3.0.0.0). So the question arises, which of
						//  these two should we use? The answer is, first use the one with the higher priority
						//  value and if they're the same (and are .Net implementations), then use the one
						//  in the higher version number of the implementing assembly.
						//
						// At this point in the code, we may come across another occurrence of an
						//  implementation that we've already done (e.g. we've already read in v3.0.0.0 and
						//  now we're reading in v3.0.0.1 or vise-versa)
						// So, in case this is the same "implementation" as we have already done, we have to
						//  possibly use this newer version rather than the version we had read in earlier
						//  (existing). Using the new version means clearing out the entries in various maps
						//  that we've already added so we can add the new ones. If we'd already done the
						//  newer one, then we just need to 'continue' to skip this (older/lower priority)
						//  version.
						//
						// the map 'm_mapToProgIds' has three entries for each implementation:
						//      <strImplementationType> = <strProgId> e.g. "SIL.tec" = "SilEncConverters31.TecEncConverter"
						//      <ByProcessTypeX> = <strImplementationType>
						//      <ByExtensionX> = <strImplementationType>
						//  This way, during 'Add', we can find the implementation type based on either the
						//  process type or the extension and then use that to find the prog id.
						// The map 'm_mapDisplayNameToProgID' has one entry:
						//      <DisplayName> = <strProgId>
						//  And is used by AutoConfigure to create the correct plug-in wrapper based on the user's
						//  choice of display names,
						// The map 'm_mapImplementTypesPriority' has also one entry:
						//      <strImplementationType> = <priority value>
						// The map 'm_mapProgIdsToAssemblyName' has one entry:
						//      <strProgId> = <AssemblyReference>
						//
						// So... see if we already have a version of this implementation type
						//  and if so, check the relative versions.
						string strProgId = aDetailsRow.ImplementationProgId;
						if (m_mapImplTypeToProgId.ContainsKey(strImplementationType))
						{
							strProgId = (string)m_mapImplTypeToProgId[strImplementationType];

							// see if one or the other has a higher priority.
							System.Diagnostics.Debug.Assert(m_mapImplementTypesPriority.ContainsKey(strImplementationType));
							int nNewPriority = aDetailsRow.Priority;
							int nExistingPriority = (int)m_mapImplementTypesPriority[strImplementationType];
							if (nNewPriority > nExistingPriority)
							{
								// this one has a higher priority, so clear out the one we read in earlier
								RemoveItemsRelatedTo(strImplementationType, strProgId);
							}
							else if((nNewPriority == nExistingPriority)
								&&  (m_mapProgIdsToAssemblyName.ContainsKey(strProgId) && !aDetailsRow.IsAssemblyReferenceNull()))
							{
								// this means that both the existing one and the new one are .Net
								//  implementations with the same priority. In this case, use the
								//  one with the highest version #
								string strAssemblyFullName = m_mapProgIdsToAssemblyName[strProgId];
								string strExistingVersion = ExtractVersion(strAssemblyFullName);
								System.Diagnostics.Debug.Assert(!String.IsNullOrEmpty(strExistingVersion), "Not expecting there to be no version # in the existing Assembly reference!?");
								string strNewVersion = ExtractVersion(aDetailsRow.AssemblyReference);
								System.Diagnostics.Debug.Assert(!String.IsNullOrEmpty(strNewVersion), "Not expecting there to be no version # in the newer Assembly reference!?");
								if (strNewVersion.CompareTo(strExistingVersion) > 0)
									RemoveItemsRelatedTo(strImplementationType, strProgId);
								else
									continue;
							}
							else
								continue;

							strProgId = aDetailsRow.ImplementationProgId;
						}

						m_mapImplTypeToProgId[strImplementationType] = strProgId;

						if (!aDetailsRow.IsDisplayNameNull())
						{
							// add a new map of friendly display names to implementation types (see AutoConfigure)
							string strFriendlyName = aDetailsRow.DisplayName;
							m_mapDisplayNameToProgID[strFriendlyName] = strProgId;
						}

						// then the converters are keyed either based on the process type or the
						//  extension (these are used by the "Add" method where the user doesn't
						//  have to specify the implementation type)
						if (!aDetailsRow.IsDefiningProcessTypeNull())
						{
							int nProcessType = aDetailsRow.DefiningProcessType;

							// keep track of eligible process flags
							m_dwProcessTypeConverters |= nProcessType;
							string strKey = BY_PROCESS_TYPE + nProcessType.ToString();

							// add a second lookup key based on this process type flag
							m_mapToImplType[strKey] = strImplementationType;
						}

						if (!aDetailsRow.IsDefiningExtensionNull())
						{
							string strExtension = aDetailsRow.DefiningExtension;

							// add a second lookup key based on the extension.
							// sExtension.ToLower(); // this does nothing!
							m_mapToImplType[strExtension] = strImplementationType;
						}

						// get the priority so we can check it against the XML file.
						priorityReg = aDetailsRow.Priority;
						string strUse = (string)mapImplementTypesUse[strImplementationType];

						// check to see if the repository has this wrapper's implementation
						if (strUse == null)
						{
							// the repository doesn't have this implementation (which must be a
							//	foreign plug-in). So add it now.
							AddImplementationEx(file, aImplsRow, strTypeImplCOM, strImplementationType,
								strProgId, priorityReg);
							bRewriteFile = true;
						}
						else
						{
							// this means we found it in the XML file, but we still need to compare
							//  that with what's in the registry (which might be newer information)
							int priorityFile = GetImplPriority(strImplementationType);
							if ((strUse != strProgId) || (priorityReg != priorityFile))
							{
								AddImplementationEx(file, aImplsRow, strTypeImplCOM, strImplementationType,
									strProgId, priorityReg);
								bRewriteFile = true;
							}
						}

						// keep track of these priorities for later.
						m_mapImplementTypesPriority[strImplementationType] = priorityReg;

						// we must use only IEncConverter implementations built against this same version
						//  of the core assembly (see InstantiateIEncConverter). This will gather which
						//  versions the different .Net implementations are installed.
						if (!aDetailsRow.IsAssemblyReferenceNull())
						{
							string strAssemblyVersion = aDetailsRow.AssemblyReference;
							m_mapProgIdsToAssemblyName.Add(strProgId, strAssemblyVersion);

							// for the *Config classes, these also need to come from the proper assembly
							if (!aDetailsRow.IsConfiguratorProgIdNull())
							{
								string strConfigProgId = aDetailsRow.ConfiguratorProgId;
								m_mapProgIdsToAssemblyName.Add(strConfigProgId, strAssemblyVersion);
							}
						}
					}
					catch { }
				}

				// therefore, to get the prog id from an extension or processTypeFlag, you
				//  must call the lookup twice (the first time gets you the implementType
				//  (e.g. ".map"->"SIL.map") and the second lookup gets you the prog id
				//  (e.g. "SIL.map"->"SilEncConverters31.TecEncConverter"). This is necessary because
				//  different extensions (e.g. .map and .tec) might go to the same prog id
				//  and otherwise we couldn't figure out the implement type from the extension.
				//  (see "Add" method for more details).
			}

			// if some of the implementation details changed, then re-write it.
			if (bRewriteFile)
				WriteRepositoryFile(file);
		}

		// get the "3.0.0.0" out of "SilEncConverters31, Version=3.0.0.0, Culture=neutral, PublicKeyToken=f1447bae1e63f485"
		protected string ExtractVersion(string strAssemblyFullName)
		{
			string strRetVersion = null;
			try
			{
				AssemblyName an = new AssemblyName(strAssemblyFullName);
				strRetVersion = an.Version.ToString();
			}
			catch { }
			return strRetVersion;
		}

		protected void RemoveItemsRelatedTo(string strImplementationType, string strProgId)
		{
			m_mapImplTypeToProgId.Remove(strImplementationType);

			// also get rid of any ByProcess and/or ByExtension entries there might be
			IDictionaryEnumerator myEnumerator = m_mapToImplType.GetEnumerator();
			List<string> astrKeysToRemove = new List<string>();
			while (myEnumerator.MoveNext())
				if (strImplementationType == (string)myEnumerator.Value)
					astrKeysToRemove.Add((string)myEnumerator.Key);
			foreach (string strKeyToRemove in astrKeysToRemove)
				m_mapToImplType.Remove(strKeyToRemove);

			// some implementations might not have a display name (if they don't have a configurator)
			myEnumerator = m_mapDisplayNameToProgID.GetEnumerator();
			while (myEnumerator.MoveNext())
				if (strProgId == (string)myEnumerator.Value)
				{
					m_mapDisplayNameToProgID.Remove(myEnumerator.Key);
					break;
				}

			// some implementations (e.g. COM implementations) might not have an Assembly Reference
			if (m_mapProgIdsToAssemblyName.ContainsKey(strProgId))
				m_mapProgIdsToAssemblyName.Remove(strProgId);

			// however, all should have a priority value
			System.Diagnostics.Debug.Assert(m_mapImplementTypesPriority.ContainsKey(strImplementationType), "Bad assumption about priority values");
			m_mapImplementTypesPriority.Remove(strImplementationType);

		}
#else   // ! UseXmlFilesForPlugins
		// Get the prog ids of conversion engines supported from the registry (and update
		//  the implementation details if they've changed in the registry)
		protected void GetConversionEnginesSupported()
		{
			// create the 'data set' that goes with our schema and read the file (so we can
			//  check to see if any of the implementation details have changed.
			mappingRegistry file = RepositoryFile;

			// see if the repository stuff has been moved
			bool bRewriteFile = false;  // means we must re-write the XML file because something changed.
			RegistryKey keyRepositoryMoved = Registry.LocalMachine.OpenSubKey(EncConverters.HKLM_PATH_TO_XML_FILE);
			if (keyRepositoryMoved != null)
			{
				string strNewRepositoryRoot = (string)keyRepositoryMoved.GetValue(strRegKeyForMovingRepository);
				if (!String.IsNullOrEmpty(strNewRepositoryRoot))
				{
					// here we have to fixup the paths that used to be [CommonProgramFiles]\SIL\MapsTables
					// so that they are now [CommonAppData]\SIL\MapsTables
					string strOriginalPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles);
					strOriginalPath += strDefMapsTablesPath;

					string strNewPath = strNewRepositoryRoot + strDefMapsTablesPath;
					if (!Directory.Exists(strNewPath))
						Directory.CreateDirectory(strNewPath);

					foreach (mappingRegistry.specRow aSpecRow in file.spec)
					{
						int nIndex = aSpecRow.path.IndexOf(strOriginalPath, StringComparison.OrdinalIgnoreCase);
						if (nIndex != -1)
						{
							// fixup
							string strNewPathSpec = strNewPath + aSpecRow.path.Substring(strOriginalPath.Length);

							// but before we do, let's make sure it exists
							if (!File.Exists(strNewPathSpec))
							{
								if (File.Exists(aSpecRow.path))
								{
									File.Move(aSpecRow.path, strNewPathSpec);
									aSpecRow.path = strNewPathSpec;
									bRewriteFile = true;
								}
							}
							else
							{
								aSpecRow.path = strNewPathSpec;
								bRewriteFile = true;
							}
						}
					}

					// we should delete this value then so we don't keep doing this
					RegistryKey keyDeletable = Registry.LocalMachine.OpenSubKey(EncConverters.HKLM_PATH_TO_XML_FILE, true);
					if (keyDeletable != null)
						keyDeletable.DeleteValue(strRegKeyForMovingRepository, false);
				}
			}

			// have a map for the XML file implementation details so we can more easily
			//  compare with what's in the registry.
			Hashtable mapImplementTypesUse = new Hashtable();

			// read the XML details
			mappingRegistry.platformRow aPlatformRow = file.platform.FindByname(strTypeImplCOM);
			if( aPlatformRow != null )
				foreach(mappingRegistry.implementRow aImplRow in aPlatformRow.GetimplementRows())
				{
					mapImplementTypesUse[aImplRow.type] = aImplRow.use;
					int priority = ((aImplRow.IspriorityNull()) ? cnDefImplPriority : aImplRow.priority);
					m_mapImplementTypesPriority[aImplRow.type] = priority;
				}

			InsureImplementationsRow(file);
			mappingRegistry.implementationsRow aImplsRow = file.implementations[0];

			// now open and read the information out of the registry. Each COM wrapper will
			//  put several of three keys in a sub-key indicating, 1) the program id of the
			//  COM wrapper (i.e. how we can create one without knowing it's class details;
			//  i.e. since it could be a plug-in from some other app/dll), 2) how it is known
			//  (i.e. is it file-based in which case we can do some simplifying implementation
			//  type mapping; e.g. .map/.tec file extensions correspond with the SIL.tec/.map
			//  implementation types (see 'Add' method for where this is used); or whether it
			//  is known by its process type; a hack for backwards compatibility, but useful
			//  for other functionality as well--see EnumByProcessType), and 3) the priority
			//  this conversion engine should be given (e.g. if we have two specs that do
			//  the same mapping, we'll use the highest priority one unless the user asks
			//  specifically for one or the other).
			// Do a check first to see whether the 'code page' implementation detail is present
			//  (which is implemented in this assembly).
			//  if not, it means this is the first time we've been run since being installed.
			//  In that case, register ourself. [this is to work around the great complexity
			//  of getting the equivalent of RegAsm on an assembly in the GAC during installation.
			//  For history: you can register a .Net assembly being installed into the GAC for
			//  "COM interop" by using the MSI property Register = vsdraCOM. But this will not result
			//  in this assembly's ComRegisterFunction attributed methods being called (which we use
			//  to write out information into the registry indicating which converter implementations
			//  are available on the system). To get that to happen, one  must do the equivalent of
			//  RegAsm (or the contents of RegisterSelf, which I've added for convenience).
			//  You also cannot have a custom action in your .msi installer which eventually calls
			//  RegisterSelf because you can't access an assembly that goes in the GAC (as this one
			//  does) during the execution of the .msi which puts it in the GAC until a very late
			//  stage in the installation process). If you can insure that it happens late (which
			//  you can with Wise, but not the VS.Net installer projects), then you can do that
			//  (as we attempted with the file RegEncCnvtrs.exe, which was a simple .Net console
			//  app, which just called SilEncConverters31.RegisterSelf), but that didn't work on
			//  certain peoples' machine which had paranoid security settings on the registry...
			//  So... completely ignore doing the RegisterSelf until we're run subsequent to the
			//  installation. Thanks to Ken Zook for this idea.
			// To detect whether we've registered ourself yet, look for the registry key
			//  SOFTWARE\SIL\SilEncConverters31\ConvertersSupported!RegisterSelf
			RegistryKey keyCnvtrsSupported = Registry.LocalMachine.OpenSubKey(HKLM_CNVTRS_SUPPORTED);
			bool bJustInstalled = false;
			if ((keyCnvtrsSupported == null) || (bJustInstalled = (keyCnvtrsSupported.GetValue(strRegKeyForSelfRegistering) != null)))
			{
				// this means that we haven't been registered yet
				// See if we can help: register ourself
				// THIS WILL FAIL if this code is run outside of the installer on a limited privileges account
				RegisterSelf();

				// if it was just installed, then remove the registry key that triggers the self-registry so we don't
				//  do it every time.
				if (bJustInstalled)
				{
					keyCnvtrsSupported = Registry.LocalMachine.CreateSubKey(HKLM_CNVTRS_SUPPORTED);
					keyCnvtrsSupported.DeleteValue(strRegKeyForSelfRegistering);
				}
			}

			if (keyCnvtrsSupported == null)
				keyCnvtrsSupported = Registry.LocalMachine.OpenSubKey(HKLM_CNVTRS_SUPPORTED);

			// if it's still not there... then we're done.
			if (keyCnvtrsSupported == null)
				return;

			string[] asImplementTypes = keyCnvtrsSupported.GetSubKeyNames();

			int priorityReg = 0;
			string strThisVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
			foreach( string sImplementType in asImplementTypes )
			{
				try
				{
					RegistryKey keyConverter = keyCnvtrsSupported.OpenSubKey(sImplementType);

					// <sImplementType> = <strProgId>
					//  "SIL.tec" = "SilEncConverters31.TecEncConverter"
					string strProgId = (string)keyConverter.GetValue(strRegKeyForProgId);

					m_mapToProgIds[sImplementType] = strProgId;

					try
					{
						// add a new map of friendly display names to implementation types (see AutoConfigure)
						string strFriendlyName = (string)keyConverter.GetValue(strRegKeyForFriendlyName);
						m_mapDisplayNameToProgID[strFriendlyName] = strProgId;
					}
					catch { }

					// then the converters are keyed either based on the process type or the
					//  extension (these are used by the "Add" method where the user doesn't
					//  have to specify the implementation type)
					object dwProcessType = keyConverter.GetValue(BY_PROCESS_TYPE);
					if (dwProcessType != null)
					{
						// keep track of eligible process flags
						m_dwProcessTypeConverters |= (int)dwProcessType;
						string strKey = BY_PROCESS_TYPE + dwProcessType.ToString();

						// add a second lookup key based on this process type flag
						m_mapToProgIds[strKey] = sImplementType;
					}

					string sExtension = (string)keyConverter.GetValue(BY_EXTENSION);
					if (sExtension != null)
					{
						// add a second lookup key based on the extension.
						// sExtension.ToLower(); // this does nothing!
						m_mapToProgIds[sExtension] = sImplementType;
					}

					// get the priority from the registry so we can check it against the XML file.
					priorityReg = (int)keyConverter.GetValue(PRIORITY);
					string strUse = (string)mapImplementTypesUse[sImplementType];

					// check to see if the repository has this wrapper's implementation
					if (strUse == null)
					{
						// the repository doesn't have this implementation (which must be a
						//	foreign plug-in). So add it now.
						AddImplementationEx(file, aImplsRow, strTypeImplCOM, sImplementType,
							(string)strProgId, priorityReg);
						bRewriteFile = true;
					}
					else
					{
						// this means we found it in the XML file, but we still need to compare
						//  that with what's in the registry (which might be newer information)
						int priorityFile = GetImplPriority(sImplementType);
						if ((strUse != (string)strProgId) || (priorityReg != priorityFile))
						{
							AddImplementationEx(file, aImplsRow, strTypeImplCOM, sImplementType,
								(string)strProgId, priorityReg);
							bRewriteFile = true;
						}
					}

					// keep track of these priorities for later.
					m_mapImplementTypesPriority[sImplementType] = priorityReg;

					// we must use only IEncConverter implementations built against this same version
					//  of the core assembly (see InstantiateIEncConverter). This will gather which
					//  versions the different .Net implementations are installed.
					RegistryKey keyAssemVersion = keyConverter.OpenSubKey(typeof(IEncConverter).Assembly.GetName().Version.ToString());
					if (keyAssemVersion != null)
					{
						string strAssemblyVersion = (string)keyAssemVersion.GetValue(null);
						if (!String.IsNullOrEmpty(strAssemblyVersion))
							m_mapProgIdsToAssemblyName.Add(strProgId, strAssemblyVersion);
					}
				}
				catch { }

				// therefore, to get the prog id from an extension or processTypeFlag, you
				//  must call the lookup twice (the first time gets you the implementType
				//  (e.g. ".map"->"SIL.map") and the second lookup gets you the prog id
				//  (e.g. "SIL.map"->"SilEncConverters31.TecEncConverter"). This is necessary because
				//  different extensions (e.g. .map and .tec) might go to the same prog id
				//  and otherwise we couldn't figure out the implement type from the extension.
				//  (see "Add" method for more details).
			}

			// if some of the implementation details changed, then re-write it.
			if( bRewriteFile )
				WriteRepositoryFile(file);
		}
#endif  // UseXmlFilesForPlugins
		#endregion Initialization

		#region Public Methods
		// The following ought to return a "EncConverter" object rather than the
		//  interface but that won't work for plug-ins (which can't be casted as the managed
		//  class object--known as the "Interface Identity problem"). So we always collect
		//  and return just the pointers to the interface.
		// P.S. by convention, if the public interface parameter is called "mapName", then
		//  it could equally likely be the mappingName or the combination of mappingName and
		//  implementation type (see GetMappingName for details)
		/// <summary>
		/// Retrieve an item from the collection (e.g. 'aECs.Item(\"Annapurna<>UNICODE\")', or 'aECs[\"Annapurna<>UNICODE\"]')
		/// </summary>
		/// <param name="mapName">named item in the collection. Note: you can also use integer indices (0 to Count) as well.</param>
		/// <returns></returns>
		[Description("Retrieve an item from the collection (e.g. 'aECs.Item(\"Annapurna<>UNICODE\")', or 'aECs(\"Annapurna<>UNICODE\")')"),Category("Data")]
		public new IEncConverter this[object mapName]
		{
			get
			{
				string strMapName = StringMapNameFromObject(mapName);   // supports int indexing
				IEncConverter aEC = (IEncConverter) base[strMapName];

				// if we didn't find it by that name, then check the alias list
				if( aEC == null )
					aEC = (IEncConverter) m_mapAliasNames[strMapName];

				// if it's still unfound, see if the user requested it *with* the implemType
				if( aEC == null )
				{
					string strImplementType;
					strMapName = GetMappingName(strMapName, out strImplementType);
					aEC = (IEncConverter) base[strMapName];

					// but make sure it really is this implementation type
					if( (aEC != null) && (aEC.ImplementType != strImplementType) )
						aEC = null;
				}

				return aEC;
			}
		}

		/// <summary>
		/// Indicates whether the given mapping name is in the collection (may be an alias as well)
		/// </summary>
		/// <param name="mapName">name of the item</param>
		/// <returns></returns>
		[Description("Indicates whether the given mapping name is in the repository"),Category("Data")]
		public override bool ContainsKey(object mapName)
		{
			bool bContained = base.ContainsKey(mapName);

			// check the alias list also.
			if( !bContained )
				bContained = m_mapAliasNames.ContainsKey(mapName);

			return bContained;
		}

		// Simplified 'Add' method (backwards compatibility): maximal assumptions
		/// <summary>
		/// Add a converter to the repository (e.g. .Add \"Annapurna<>UNICODE\", \"C:\\Program Files\\Common Files\\SIL\\MapsTables\\Annapurna.tec\", Legacy_to_from_Unicode, \"SIL-ANNAPURNA_05-2002\", \"UNICODE\", ProcessEncodingConversion)
		/// </summary>
		/// <param name="mappingName">friendly name key that the converter is to be accessed with</param>
		/// <param name="converterSpec">technical spec of the converter (e.g. TECkit & CC = filespec to map)</param>
		/// <param name="conversionType">ConvType parameter indicating the type of conversion (e.g. "Legacy_to_from_Unicode")</param>
		/// <param name="leftEncoding">optional technical name of the left-hand side encoding (e.g. SIL-ANNAPURNA-05)</param>
		/// <param name="rightEncoding">optional technical name of the right-hand side encoding (e.g. UNICODE)</param>
		/// <param name="processType">flag to indicate teh implementation/transduction type (e.g. UnicodeEncodingConversion) from which you can do later filtering (e.g. ByEncodingID)</param>
		[Description("Add a converter to the repository (e.g. .Add \"Annapurna<>UNICODE\", \"C:\\Program Files\\Common Files\\SIL\\MapsTables\\Annapurna.tec\", Legacy_to_from_Unicode, \"SIL-ANNAPURNA_05-2002\", \"UNICODE\", ProcessEncodingConversion)"),Category("Data")]
		public void Add(string mappingName, string converterSpec, ConvType conversionType, string leftEncoding, string rightEncoding, ProcessTypeFlags processType)
		{
			// previously, the process type was dual-use in defining (possibly) the program
			//  id of the converter (used by ICU). So, see if that's the case and then we
			//  know what the program id of the com wrapper is for this converter.
			// first see if it's a 'by process type' converter. (but filter out extraneous
			//  process type flags (i.e. those that *aren't* processed by their process type
			//  flag)).
			int processTypeFlag = ((int)processType & (int)m_dwProcessTypeConverters);
			string strKey = BY_PROCESS_TYPE + processTypeFlag.ToString();
			string strImplementType = (string)m_mapToImplType[strKey];

			// if we didn't find it above, then look up the implement type based on the
			//  extension
			if( strImplementType == null )
			{
				if( converterSpec == null )     // this could be used by a one-shot plug-in
				{                               // where the name *is* the implement type
					strImplementType = mappingName; //  (e.g. utf-8<>utf-16 plug-in used to be this way)
				}
				else
				{
					string strFilename = null;
					GetFileExtn(converterSpec, out strFilename, out strKey);
					if( strKey != null )
						strImplementType = (string)m_mapToImplType[strKey];
				}
			}

			// it might *still* be null
			if( strImplementType == null )
				ThrowError(ErrStatus.NoAvailableConverters);

			// now call the main add method with these default values
			AddConversionMap(mappingName, converterSpec, conversionType, strImplementType,
				leftEncoding, rightEncoding, processType);
		}

		[Description("Add a conversion map to the repository"),Category("Data")]
		public void AddConversionMap(string mappingName, string converterSpec,
			ConvType conversionType, string implementType, string leftEncoding,
			string rightEncoding, ProcessTypeFlags processType)
		{
			// first, disallow a semi-colon in the mapping name (too many plug-ins use
			//  those to delimit stuff and we don't want to run into problems.
			if( mappingName.IndexOf(';') != -1 )
				ThrowError(ErrStatus.InvalidMappingName);

			// next, make sure any encodingIDs we're given are parent encodingIDs rather
			//  than aliases.
			mappingRegistry file = RepositoryFile;
			leftEncoding = GetEncodingName(file, leftEncoding);
			rightEncoding = GetEncodingName(file, rightEncoding);

			// a mapping name by itself will either refer to the single spec of a mapping
			//	with only a single spec, or it will refer to the highest priority spec of
			//	a multi-spec mapping. First see if this same mapping name already exists.
			string strConverterName = mappingName;
			IEncConverter aNewEC = null, aExistEC = this[mappingName];
			if( aExistEC != null )
			{
				// if we're adding a second spec to the same mapping, then the original
				//  spec's name must be adjusted and this new one will also be adjusted
				strConverterName = AdjustMappingNames(mappingName, implementType, ref aExistEC);
			}

			// in either case, now add this new spec to the collection (by calling AddEx
			//  here). By doing this here before adding the details to the XML file below,
			//  we allow the actual COM wrapper to modify the given parameters (e.g. the
			//  TECkit wrapper will read the encodingNames from the map in case they weren't
			//  given by the user at this point). (but use 'try' as this might be an
			//  implementation we don't know about) (also, use default code page now and
			//  update them later).
			try
			{
				int pt = (int)processType;
				aNewEC = AddEx(ToProgId(implementType), strConverterName, converterSpec,
					ref leftEncoding, ref rightEncoding, ref conversionType, ref pt,
					cnDefCodePage, cnDefCodePage, true);
			}
			catch(COMException e)
			{
				// but some exceptions we *should* re-throw
				if( e.ErrorCode != (int)ErrStatus.NoAvailableConverters )
					throw e;
			}
			catch {}

			// if this is a second converter for the same mapping, then pick the highest
			//	priority one and make that also go by the name of the mappingName (so it'll
			//  get picked by the default mapping name).
			if( (aNewEC != null) && (aExistEC != null) )
			{
				int nNewPri = GetImplPriority(aNewEC.ImplementType);
				int nExistPri = GetImplPriority(aExistEC.ImplementType);
				if( nNewPri > nExistPri )
					m_mapAliasNames[mappingName] = aNewEC;
				else
					m_mapAliasNames[mappingName] = aExistEC;
			}

			// now add this information to the XML file.
			mappingRegistry.mappingRow aMapRow;
			mappingRegistry.specRow aSpecRow;
			AddConversionMapEx(file, mappingName, conversionType, converterSpec,
				implementType, leftEncoding, rightEncoding, processType, false, out aMapRow,
				out aSpecRow);

			// now that everything is in the file, let's see if we now have new values for the
			//  font's code page. First get an xmlDoc so we can do the XPath querying.
			XmlDataDocument xmlDoc;
			XmlNamespaceManager nsmgr;
			GetXmlDataDocument(file, out xmlDoc, out nsmgr);
			int nCodePageInput, nCodePageOutput;
			GetFontDetails(xmlDoc, nsmgr, leftEncoding, out nCodePageInput);
			GetFontDetails(xmlDoc, nsmgr, rightEncoding, out nCodePageOutput);

			// notice that what we're saying here is that the lhsEncoding corresponds to the
			//  *input* code page (which means for the string we'll be converting). This will
			//  only be true if the converter is being run in the forward direction. So the
			//  COM wrappers must make sure to use the 'output code page' for the 'input data'
			//  when running in reverse. [We could have called these members 'CodePageLeft',
			//  etc., but that's more confusing to the user.]
			if( nCodePageInput != cnDefCodePage )
				aNewEC.CodePageInput = nCodePageInput;
			if( nCodePageOutput != cnDefCodePage )
				aNewEC.CodePageOutput = nCodePageOutput;

			// finally, just in case this is a tec, icu (or some new plug-in) that has
			//  attributes, let's put them in the xml file as well, so we can make them
			//  available to other repository file readers.
			try
			{
				string [] asAttributeKeys = aNewEC.AttributeKeys;
				if (asAttributeKeys != null && asAttributeKeys.Length > 0)
				{
					InsureSpecPropertiesRow(file,aSpecRow);
					Hashtable mapProperties = new Hashtable();
					mappingRegistry.specPropertiesRow aAttrsRow = aSpecRow.GetspecPropertiesRows()[0];

					// if the properties were already present, however, make sure we don't
					//  get duplicates). Put the current property names in a map for easier
					//  lookup.
					foreach(mappingRegistry.specPropertyRow aPropRow in aAttrsRow.GetspecPropertyRows())
						mapProperties.Add(aPropRow.name,aPropRow);

					// for each property the converter knows about...
					mappingRegistry.specPropertyDataTable aAttrDT = file.specProperty;
					foreach( string sAttributeKey in asAttributeKeys )
					{
						string propValue = aNewEC.AttributeValue(sAttributeKey);

						// but don't bother if there's no value.
						if( !String.IsNullOrEmpty(propValue) )
						{
							// ... see if we already had a property by this name
							mappingRegistry.specPropertyRow aPropRow =
								(mappingRegistry.specPropertyRow)mapProperties[sAttributeKey];

							// if so...
							if( aPropRow != null )
							{
								// ... then update the text value
								aPropRow.specProperty_Text = propValue;
							}
							else    // otherwise add a new one.
							{
								aAttrDT.AddspecPropertyRow(sAttributeKey, propValue, aAttrsRow);
							}
						}
					}
				}
			}
			catch {}	// ke sirah sirah

			WriteRepositoryFile(file);	// save changes.
		}

		// Add for an 'implementation' (e.g. "Perl" or "COM")
		/// <summary>
		/// Allows special client applications to add implementations that EncConverters otherwise doesn't
		/// support.
		/// </summary>
		/// <param name="platform">indication of the technology implementing access to the transduction (e.g. COM)</param>
		/// <param name="type">indication of the implementation (e.g. SIL.tec)</param>
		/// <param name="use">indication of the prog ID of the implementing service (e.g. SilEncConverters31.TecEncConverter)</param>
		/// <param name="param">additional parameter used by some implementation engines</param>
		/// <param name="priority">indication of the desirability of using this implementation over others</param>
		public void AddImplementation(string platform, string type, string use, string param, int priority)
		{
			// load the XML file.
			mappingRegistry file = RepositoryFile;

			// see if we have the outer most 'implementation' node
			InsureImplementationsRow(file);
			mappingRegistry.implementationsRow aImplsRow = file.implementations[0];

			// call internal helper to do the rest (also called when creating the XML file from
			//  scratch)
			AddImplementationEx(file,aImplsRow,platform,type,use,priority);

			// save changes
			WriteRepositoryFile(file);
		}

		// Remove for an 'implementation' (e.g. "Perl" or "COM")
		/// <summary>
		/// Allows special client applications to remove implementations that EncConverters otherwise doesn't
		/// support.
		/// </summary>
		/// <param name="platform">indication of the technology implementing access to the transduction (e.g. COM)</param>
		/// <param name="type">indication of the implementation (e.g. SIL.tec)</param>
		public void RemoveImplementation(string platform, string type)
		{
			// load the XML file.
			mappingRegistry file = RepositoryFile;

			// see if we have the outer most 'implementation' node
			mappingRegistry.implementationsDataTable aImplsDT = file.implementations;
			if( aImplsDT.Count == 0 )
				ThrowError(ErrStatus.NoImplementDetails);

			// make sure the platform row exists
			mappingRegistry.platformRow aPlatRowNew = null;
			foreach(mappingRegistry.platformRow aPlatRow in aImplsDT[0].GetplatformRows())
			{
				if(aPlatRow.name == platform)
				{
					aPlatRowNew = aPlatRow;
					break;
				}
			}

			if( aPlatRowNew == null )
				ThrowError(ErrStatus.NoImplementDetails);

			// do the same double-checking for the implement rows
			foreach(mappingRegistry.implementRow aImplRow in aPlatRowNew.GetimplementRows())
			{
				if( aImplRow.type == type )
				{
					file.implement.RemoveimplementRow(aImplRow);
					WriteRepositoryFile(file);  // save changes
					return; // then we're done.
				}
			}

			// fall thru means we didn't find the details
			ThrowError(ErrStatus.NoImplementDetails);
		}

		// Add an alias entry to the given encoding name
		/// <summary>
		/// Add an alternate name (e.g. a version independant name) by which a given encoding ID may be accessed
		/// </summary>
		/// <param name="encodingName">name of the encoding being aliased</param>
		/// <param name="alias">alias name</param>
		[Description("Add an alternate name (e.g. a version independant name) by which a given encoding ID may be accessed"),Category("Data")]
		public void AddAlias(string encodingName, string alias)
		{
			// load XML file
			mappingRegistry file = RepositoryFile;

			// the given encodingName could be an alias itself!
			encodingName = GetEncodingName(file,encodingName);

			// find the encodingRow for the given encodingName
			mappingRegistry.encodingDataTable aEncodingDT = file.encoding;
			mappingRegistry.encodingRow aEncodingRow = aEncodingDT.FindByname(encodingName);
			if( aEncodingRow == null )
				throw new ArgumentOutOfRangeException();

			// make sure this alias doesn't already exist.
			if( GetEncodingName(file,alias) != alias )
				ThrowError(ErrStatus.InvalidAliasName);

			InsureAliasesRow(file,aEncodingRow);
			file.alias.AddaliasRow(alias,aEncodingRow.GetaliasesRows()[0]);

			// save changes
			WriteRepositoryFile(file);
		}

		// Add a compound (daisy-chained) converter step
		// Technically, the 'AddConversionMap' for the compound converter *doesn't* need to
		//  be called first (this one will add it if it doesn't exist), but if you want the
		//  values (e.g. encoding IDs, processTypes, etc) to be defined by you (rather than
		//  them being determined by this method from the left values of the first step),
		//  then you can call that method first (and we won't overwrite them here).
		//  Otherwise, this routine will update the details of the map with each step added.
		//  Also note that currently, the converterSpec will be an encoding ID representation
		//  of the actual conversion being done by these steps (e.g. if three steps are
		//  added:
		//      1) Annapurna<>Unicode (Devanagari)
		//      2) Unicode Devanagari<>Unicode IPA (transliteration), and
		//      3) IPA<>Unicode (in reverse)
		//  to go from Annapurna to IPA, then the converterSpec will be created as:
		//  SIL-ANNAPURNA_05-2002<>UNICODE:DEVANAGARI + UNICODE:DEVANAGARI<>UNICODE:IPA +
		//  UNICODE:IPA<>SIL-IPA93-2002
		/// <summary>
		/// Add a conversion step to a 'compound' (daisy-chained) converter to allow conversion across multiple, consecutive EncConverter objects
		/// </summary>
		/// <param name="compoundMappingName">name of compound converter</param>
		/// <param name="converterStepMapName">name of the next step in the compound converter</param>
		/// <param name="directionForward">indication of the direction to go with this step</param>
		/// <param name="normalizeOutput">indication of any output normalization desired for this step</param>
		[Description("Add a conversion step to a 'compound' (daisy-chained) converter to allow conversion across multiple, consecutive EncConverter objects"),Category("Data")]
		public void AddCompoundConverterStep(string compoundMappingName, string converterStepMapName, bool directionForward, NormalizeFlags normalizeOutput)
		{
			// first make sure we have the step converter already and get it's information
			//  (which becomes the compound converter's lhs information if we're adding it
			//  here).
			IEncConverter rStepConverter = this[converterStepMapName];
			if( rStepConverter == null )
				ThrowError(ErrStatus.NoConverter,converterStepMapName);

			Int32 nCodePageInput = rStepConverter.CodePageInput;
			string strLhsEncoding = rStepConverter.LeftEncodingID;
			string strRhsEncoding = rStepConverter.RightEncodingID;
			ConvType eConversionType = rStepConverter.ConversionType;
			bool bBidirectional = !IsUnidirectional(eConversionType);

			// if the step is forward-only and the user asks to go backwards, then that's bad.
			if( !bBidirectional && !directionForward )
				ThrowError(ErrStatus.InvalidConversionType);

			// if this new step is reverse direction, then the ConvType reverses
			//  (only relevant for Legacy<>Unicode or vise versa conversion)
			if( !directionForward )
			{
				if( eConversionType == ConvType.Legacy_to_from_Unicode )
					eConversionType = ConvType.Unicode_to_from_Legacy;
				else if( eConversionType == ConvType.Unicode_to_from_Legacy )
					eConversionType = ConvType.Legacy_to_from_Unicode;
			}

			bool bClearOldEncodingInfo = true;
			CmpdEncConverter rCmpdConverter = null;
			string strConverterIdentifier = null;
			try
			{
				// casting the Converter returned as a compound converter might fail, but then we just
				//  do the catch case which is what we want anyway
				rCmpdConverter = (CmpdEncConverter)this[compoundMappingName];

				if( rCmpdConverter == null )
					throw new SystemException();    // just go to 'catch'

				// if this is the first one, then 'borrow' it's lhs if ours is empty
				// the compound converter is bidi if both the prev was bidi and this step is bidi.
				bool bCmpdBidi = !IsUnidirectional(rCmpdConverter.ConversionType) && bBidirectional;

				// adjust the converter spec item
				string strPresentLhsEncoding = rCmpdConverter.LeftEncodingID;
				string strPresentRhsEncoding = ((directionForward) ? strRhsEncoding : strLhsEncoding);

				// if this is the first converter and it wasn't already set, then use the steps lhs
				if( rCmpdConverter.CountConverters == 0 )
				{
					if( String.IsNullOrEmpty(strPresentLhsEncoding) )
						strPresentLhsEncoding = rStepConverter.LeftEncodingID;
				}

				// get a human-readable string (based on the encoding IDs and how this step
				//  goes (e.g. reverse direction, normalize flags, etc.)
				strConverterIdentifier = rCmpdConverter.AdjustConverterSpecProperty(converterStepMapName,
					directionForward, normalizeOutput);

				// depending on the conversion type of this new step, we might have to adjust the
				//  conversion type of the compound converter (e.g. if the step is unidirectional,
				//  then the compound converter can't be bi-directional).
				ConvType eConversionTypeNew;
				NormConversionType
					eLhsConversionType =
						EncConverter.NormalizeLhsConversionType(rCmpdConverter.ConversionType),
					eRhsConversionType =
						EncConverter.NormalizeRhsConversionType(eConversionType);

				if( bCmpdBidi )
				{
					if( eLhsConversionType == NormConversionType.eLegacy )
					{
						if( eRhsConversionType == NormConversionType.eLegacy )
							eConversionTypeNew = ConvType.Legacy_to_from_Legacy;
						else    // eUnicode
							eConversionTypeNew = ConvType.Legacy_to_from_Unicode;
					}
					else    // lhs == eUnicode
					{
						if( eRhsConversionType == NormConversionType.eLegacy )
							eConversionTypeNew = ConvType.Unicode_to_from_Legacy;
						else    // eUnicode
							eConversionTypeNew = ConvType.Unicode_to_from_Unicode;
					}
				}
				else    // m_bBidirectional == false
				{
					if( eLhsConversionType == NormConversionType.eLegacy )
					{
						if( eRhsConversionType == NormConversionType.eLegacy )
							eConversionTypeNew = ConvType.Legacy_to_Legacy;
						else    // eUnicode
							eConversionTypeNew = ConvType.Legacy_to_Unicode;
					}
					else    // lhs == eUnicode
					{
						if( eRhsConversionType == NormConversionType.eLegacy )
							eConversionTypeNew = ConvType.Unicode_to_Legacy;
						else    // eUnicode
							eConversionTypeNew = ConvType.Unicode_to_Unicode;
					}
				}

				// update the compound converter with all this new information
				RemoveNonPersist(rCmpdConverter.Name);  // remove it cause we're going to add it again.
				int pt = rCmpdConverter.ProcessType;
				InitializeConverter(rCmpdConverter, compoundMappingName, strConverterIdentifier,
					ref strPresentLhsEncoding, ref strPresentRhsEncoding,
					ref eConversionTypeNew, ref pt, nCodePageInput,
					rCmpdConverter.CodePageOutput, true);
			}
			catch
			{
				bClearOldEncodingInfo = false;  // don't want to clear it out in this case

				// if the compound converter didn't exist, then just add it ourselves (backwards
				//  compatibility) but make it look just like the step we're adding.
				rCmpdConverter = new CmpdEncConverter();

				// get a human-readable string (based on the encoding IDs and how this step
				//  goes (e.g. reverse direction, normalize flags, etc.)
				strConverterIdentifier = rCmpdConverter.AdjustConverterSpecProperty(converterStepMapName,
					directionForward, normalizeOutput);

				if( ContainsKey(compoundMappingName) )
					RemoveNonPersist(compoundMappingName);

				// initialize the new one (don't know what the process type is if the user
				//  doesn't add the base converter first).
				int lProcessType = (int)ProcessTypeFlags.DontKnow;
				InitializeConverter(rCmpdConverter, compoundMappingName, strConverterIdentifier,
					ref strLhsEncoding, ref strRhsEncoding, ref eConversionType,
					ref lProcessType, nCodePageInput, rStepConverter.CodePageOutput, true);
			}

			// for either case, add the step to the queue in the virt converter
			rCmpdConverter.AddConverterStep(rStepConverter, directionForward, normalizeOutput);

			// update the XML file to add the compound converter to the mapping node
			mappingRegistry.mappingRow aMapRow;
			mappingRegistry.specRow aSpecRow;
			mappingRegistry file = RepositoryFile;
			AddConversionMapEx(file, rCmpdConverter.Name, rCmpdConverter.ConversionType,
				rCmpdConverter.ConverterIdentifier, rCmpdConverter.ImplementType,
				rCmpdConverter.LeftEncodingID, rCmpdConverter.RightEncodingID,
				(ProcessTypeFlags)rCmpdConverter.ProcessType, bClearOldEncodingInfo,
				out aMapRow, out aSpecRow);

			AddStepRow(file, aMapRow, aSpecRow, converterStepMapName, directionForward, normalizeOutput);

			// finally, save changes
			WriteRepositoryFile(file);
		}

		internal void AddStepRow(mappingRegistry file, mappingRegistry.mappingRow aMapRow, mappingRegistry.specRow aSpecRow,
			string converterStepMapName, bool directionForward, NormalizeFlags normalizeOutput)
		{
			Debug.Assert(aSpecRow != null);
			InsureStepsRow(file,aSpecRow);
			mappingRegistry.stepsRow aStepsRow = aSpecRow.GetstepsRows()[0];

			// but the step that we're adding is from the stepname...
			string str, mapName = GetMappingName(converterStepMapName,out str);
			aMapRow = file.mapping.FindByname(mapName);
			Debug.Assert(aMapRow != null);
			mappingRegistry.stepRow aStepRow = file.step.AddstepRow(aMapRow,!directionForward,ToNormalizeStrings(normalizeOutput),aStepsRow);

			// default these values
			if( normalizeOutput == NormalizeFlags.None )
				aStepRow.SetnormalizeNull();
			if( directionForward )
				aStepRow.SetreverseNull();
		}

		/// <summary>
		/// Add font information to the repository. Especially for a) legacy fonts (which have a code page) and b) which uniquely identify a particular encoding (e.g. SILDoulos IPA93 *is* SIL-IPA93-2002)
		/// </summary>
		/// <param name="fontName">name of the font this information is associated with</param>
		/// <param name="codePage">code page that the font is based on (e.g. 1252). Use SIL ViewGlype to determine this if you don't know.</param>
		/// <param name="defineEncoding">name of the encoding that this font uniquely identifies</param>
		[Description("Add font information to the repository. Especially for a) legacy fonts (which have a code page) and b) which uniquely identify a particular encoding (e.g. SILDoulos IPA93 *is* SIL-IPA93-2002)"),Category("Data")]
		public void AddFont(string fontName, int codePage, string defineEncoding)
		{
			// load XML file and see if we have an entry already for this font
			mappingRegistry file = RepositoryFile;

			// next make sure the 'fonts' node is present (it should be but just make sure)
			InsureFontsRow(file);
			mappingRegistry.fontsRow aFontsRow = file.fonts[0];

			// now get the 'font' node that corresponds to this fontName
			mappingRegistry.fontDataTable aFontDT = file.font;
			mappingRegistry.fontRow aFontRow = aFontDT.FindByname(fontName);
			if( aFontRow == null )
			{
				// this font doesn't already exist, so add it now.
				aFontRow = aFontDT.AddfontRow(fontName,codePage,aFontsRow);
			}

			Debug.Assert(aFontRow != null);
			if( codePage == cnDefCodePage )
				aFontRow.SetcpNull();
			else
				aFontRow.cp = codePage;

			// add a mapping to the encoding if it's not empty.
			if( !String.IsNullOrEmpty(defineEncoding) )
			{
				// the given encodingName could be an alias itself!
				defineEncoding = GetEncodingName(file,defineEncoding);

				// first make sure the related encoding exists (unless not given)
				mappingRegistry.encodingDataTable aEncodingDT = file.encoding;
				mappingRegistry.encodingRow aEncodingRow = aEncodingDT.FindByname(defineEncoding);
				if( aEncodingRow == null )
					ThrowError(ErrStatus.NoEncodingName,defineEncoding);

				InsureFontEncodingsRow(file,aFontRow);
				bool bFound = false;
				foreach(mappingRegistry.fontEncodingRow aFontEncodingRow in aFontRow.GetfontEncodingsRows()[0].GetfontEncodingRows())
				{
					if( aFontEncodingRow.name == defineEncoding )
					{
						aFontEncodingRow.unique = true;
						bFound = true;
						break;  // prego
					}
				}

				if( !bFound )
					file.fontEncoding.AddfontEncodingRow(aEncodingRow,true,
						aFontRow.GetfontEncodingsRows()[0]);
			}

			// save changes
			WriteRepositoryFile(file);
		}

		/// <summary>
		/// Add unicode font encoding information to the repository (use 'AddFont' (with zero for the code page)
		/// instead if the Unicode font uniquely identifies an encoding)
		/// </summary>
		/// <param name="fontName">name of the font this information is associated with</param>
		/// <param name="unicodeEncoding">name of the encoding that this font uniquely identifies (e.g. UNICODE_DEVANAGARI)</param>
		[Description("Add unicode font encoding information to the repository (use 'AddFont' (with zero for the code page) instead if the Unicode font uniquely identifies an encoding)"),Category("Data")]
		public void AddUnicodeFontEncoding(string fontName, string unicodeEncoding)
		{
			// load XML file and see if we have an entry already for this font
			mappingRegistry file = RepositoryFile;

			// next make sure the 'fonts' node is present (it should be but just make sure)
			InsureFontsRow(file);
			mappingRegistry.fontsRow aFontsRow = file.fonts[0];

			// now get the 'font' node that corresponds to this fontName
			mappingRegistry.fontDataTable aFontDT = file.font;
			mappingRegistry.fontRow aFontRow = aFontDT.FindByname(fontName);
			if( aFontRow == null )
			{
				// this font doesn't already exist, so add it now.
				aFontRow = aFontDT.AddfontRow(fontName,cnDefCodePage,aFontsRow);
			}

			Debug.Assert(aFontRow != null);
			aFontRow.SetcpNull();   // unicode fonts don't have codepages

			// more to do if the encoding name isn't empty
			if( !String.IsNullOrEmpty(unicodeEncoding) )
			{
				// the given encodingName could be an alias itself!
				unicodeEncoding = GetEncodingName(file,unicodeEncoding);

				// first make sure the related encoding exists (unless not given)
				mappingRegistry.encodingRow aEncodingRow = null;
				if( !String.IsNullOrEmpty(unicodeEncoding) )
				{
					mappingRegistry.encodingDataTable aEncodingDT = file.encoding;
					aEncodingRow = aEncodingDT.FindByname(unicodeEncoding);
					if( aEncodingRow == null )
						ThrowError(ErrStatus.NoEncodingName,unicodeEncoding);
				}

				// now add a mapping record for this encoding (but check first for existance)
				InsureFontEncodingsRow(file,aFontRow);
				bool bFound = false;
				foreach(mappingRegistry.fontEncodingRow aFontEncodingRow in aFontRow.GetfontEncodingsRows()[0].GetfontEncodingRows())
				{
					if( aFontEncodingRow.name == unicodeEncoding )
					{
						aFontEncodingRow.SetuniqueNull();
						bFound = true;
						break;  // prego
					}
				}

				if( !bFound )
					file.fontEncoding.AddfontEncodingRow(aEncodingRow,false,
						aFontRow.GetfontEncodingsRows()[0]).SetuniqueNull();
			}

			// save changes
			WriteRepositoryFile(file);
		}

		/// <summary>
		/// Add a mapping between fonts to a converter mapping entry. Can be used by clients to automatically
		/// pick the font to suggest when a particular map is used.
		/// </summary>
		/// <param name="mappingName">friendly name (i.e. key) of the mapping being used</param>
		/// <param name="fontName">name of the left-hand side (i.e. pre-conversion) font</param>
		/// <param name="assocFontName">name of the font to associate with the right-hand side (i.e. post-conversion)</param>
		[Description("Add a mapping between fonts to a converter mapping entry."),Category("Data")]
		public void AddFontMapping(string mappingName, string fontName, string assocFontName)
		{
			// load XML file and see if we have an entry already for this font
			mappingRegistry file = RepositoryFile;

			// make sure the mapping exists
			string sImplementType;
			mappingName = GetMappingName(mappingName, out sImplementType);
			mappingRegistry.mappingRow aMapRow = file.mapping.FindByname(mappingName);
			if( aMapRow == null )
				ThrowError(ErrStatus.NoConverter,mappingName);

			// finally, just add the mapping for this (unless it already exists)
			InsureFontMappingsRow(file,aMapRow);
			mappingRegistry.fontMappingsRow aFontMappingsRow = aMapRow.GetfontMappingsRows()[0];
			foreach(mappingRegistry.fontMappingRow aFontMappingRow in aFontMappingsRow.GetfontMappingRows())
			{
				if(     (aFontMappingRow.name == fontName)
					&&  (aFontMappingRow.assocFont == assocFontName) )
					return; // prego...
			}

			// falling thru means we have to add it.
			file.fontMapping.AddfontMappingRow(fontName,assocFontName,aFontMappingsRow);

			// save changes
			WriteRepositoryFile(file);
		}

		/// <summary>
		/// Add a primary/fallback mapping converter. The fallback converter is only called if the primary doesn't change the input data
		/// </summary>
		/// <param name="strMappingName">friendly name of the fallback converter mapping by which it will be accessed</param>
		/// <param name="strMappingNamePrimaryStep">friendly name of the primary converter step</param>
		/// <param name="bDirectionForwardPrimary">indication of the direction to be used for the primary converter</param>
		/// <param name="strMappingNameFallbackStep">friendly name of the fallback converter step</param>
		/// <param name="bDirectionForwardFallback">indication of the direction to be used for the fallback converter</param>
		[Description("Add a primary/fallback mapping converter. The fallback converter is only called if the primary doesn't change the input data."),Category("Data")]
		public void AddFallbackConverter(string strMappingName,
			string strMappingNamePrimaryStep, bool bDirectionForwardPrimary,
			string strMappingNameFallbackStep, bool bDirectionForwardFallback)
		{
			// first test whether these parameters are existing converters
			IEncConverter rPriEC = this[strMappingNamePrimaryStep];
			if( rPriEC == null )
				ThrowError(ErrStatus.NoConverter,strMappingNamePrimaryStep);
			IEncConverter rFallbackEC = this[strMappingNameFallbackStep];
			if( rFallbackEC == null )
				ThrowError(ErrStatus.NoConverter,strMappingNameFallbackStep);

			// in case there was a converter by this same name, clobber it
			this.Remove(strMappingName);

			// next check if the two converters have a comparable input and output encoding types (unicode vs.
			//  bytes... otherwise, they wouldn't make very good partners)
			if( !IsConvTypeCompariable(rPriEC.ConversionType, bDirectionForwardPrimary, rFallbackEC.ConversionType, bDirectionForwardFallback) )
				ThrowError(ErrStatus.FallbackSimilarConvType);

			// create the combined converter type (i.e. the one that contains both the other converters)
			FallbackEncConverter rCombinedFallbackEC = new FallbackEncConverter();

			// initialize its details (most of it comes from the primary converter)
			string strConverterIdentifier = rCombinedFallbackEC.AdjustConverterSpecProperty(strMappingNamePrimaryStep, bDirectionForwardPrimary, strMappingNameFallbackStep, bDirectionForwardFallback);
			string strPresentLhsEncoding = (String.IsNullOrEmpty(rPriEC.LeftEncodingID)) ? rFallbackEC.LeftEncodingID : rPriEC.LeftEncodingID;
			string strPresentRhsEncoding = (String.IsNullOrEmpty(rPriEC.RightEncodingID)) ? rFallbackEC.RightEncodingID : rPriEC.RightEncodingID;
			ConvType eConversionType = (bDirectionForwardPrimary) ? rPriEC.ConversionType : ReverseConvType(rPriEC.ConversionType);
			int nProcessType = rPriEC.ProcessType;

			// Initialize the combined converter and add it to the (Hashtable) collection
			InitializeConverter(rCombinedFallbackEC, strMappingName, strConverterIdentifier,
				ref strPresentLhsEncoding, ref strPresentRhsEncoding, ref eConversionType, ref nProcessType,
				rPriEC.CodePageInput, rPriEC.CodePageOutput, true);

			// add the two sub-converters as "steps"... first to the combined converter...
			rCombinedFallbackEC.AddConverterStep(rPriEC,bDirectionForwardPrimary,NormalizeFlags.None);
			rCombinedFallbackEC.AddConverterStep(rFallbackEC,bDirectionForwardFallback,NormalizeFlags.None);

			// ... and then add the whole thing (converter and it's two steps) to the repository XML file
			mappingRegistry.mappingRow aMapRow;
			mappingRegistry.specRow aSpecRow;
			mappingRegistry file = RepositoryFile;
			AddConversionMapEx(file, strMappingName, eConversionType, strConverterIdentifier,
				rCombinedFallbackEC.ImplementType, strPresentLhsEncoding, strPresentRhsEncoding,
				(ProcessTypeFlags)nProcessType, true, out aMapRow, out aSpecRow);

			// ... the step rows...
			AddStepRow(file, aMapRow, aSpecRow, strMappingNamePrimaryStep, bDirectionForwardPrimary, NormalizeFlags.None);
			AddStepRow(file, aMapRow, aSpecRow, strMappingNameFallbackStep, bDirectionForwardFallback, NormalizeFlags.None);

			// ... and finally, save changes
			WriteRepositoryFile(file);
		}

		// this method can be used by application to get the code page used to widen a narrow
		//  byte string of data with a certain encoding (based on a font)
		/// <summary>
		/// Get the code page associated with this font name (based on the information in the repository; not necessarily accurate).
		/// </summary>
		/// <param name="fontName">The name of the font to get the code page of</param>
		/// <returns>code page value</returns>
		[Description("Get the code page associated with this font name"),Category("Data")]
		public int CodePage(string strFontName)
		{
			// load the XML file and see if we already have the requisite font name row
			mappingRegistry file = RepositoryFile;
			mappingRegistry.fontRow aFontRow = file.font.FindByname(strFontName);

#if !rde300 // add better smarts to the determination of code page (thanks to Bob Hallissy for these tips)
			int ncp = cnDefCodePage;
			if( aFontRow == null )
				ncp = TryToGetCodePageFromFont(strFontName);
			else if (aFontRow.IscpNull())
				ncp = cnDefCodePage;
			else
				ncp = aFontRow.cp;
#else
			if( aFontRow == null )
				ThrowError(ErrStatus.AddFontFirst,fontName);

			int ncp = ((aFontRow.IscpNull()) ? cnDefCodePage : aFontRow.cp);
#endif
			return ncp;
		}

		/// <summary>
		/// Get the one or more font names associated with the given mapping and font name. Can be used to
		/// suggest a font for the output of a conversion.
		/// </summary>
		/// <param name="mappingName">friendly name of the mapping being used</param>
		/// <param name="fontName">name of the left-hand side (pre-conversion) font</param>
		/// <returns>an array of font names associated with the other parameters</returns>
		[Description("Get the one or more font names associated with the given mapping and font name (cf. AddFontMapping)"),Category("Data")]
		public string [] GetFontMapping(string mappingName, string fontName)
		{
			// load XML file and see if we have an entry already for this font
			mappingRegistry file = RepositoryFile;

			// make sure the mapping exists
			string sImplementType;
			mappingName = GetMappingName(mappingName, out sImplementType);
			mappingRegistry.mappingRow aMapRow = file.mapping.FindByname(mappingName);
			if( aMapRow == null )
				ThrowError(ErrStatus.NoConverter,mappingName);

			// finally, just add the mapping for this (unless it already exists)
			InsureFontMappingsRow(file,aMapRow);
			mappingRegistry.fontMappingsRow aFontMappingsRow = aMapRow.GetfontMappingsRows()[0];
			ArrayList ar = new ArrayList();
			foreach(mappingRegistry.fontMappingRow aFontMappingRow in aFontMappingsRow.GetfontMappingRows())
			{
				if( aFontMappingRow.name == fontName )
					ar.Add(aFontMappingRow.assocFont);
				else if( aFontMappingRow.assocFont == fontName )
					ar.Add(aFontMappingRow.name);
			}

			int i = 0;
			string [] aStr = new string[ar.Count];
			foreach(string st in ar)
				aStr[i++] = st;
			return aStr;
		}

		/// <summary>
		/// Return the mapping name that uniquely works for the given font name (can be used as a suggestion for the
		/// converter based on a particular font. This corresponds to the defining encoding mapping.
		/// </summary>
		/// <param name="strFontName">name of the font to check for the assocated defining encoding mapping</param>
		/// <returns>the friendly name associated with this font name</returns>
		public string GetMappingNameFromFont(string strFontName)
		{
			XmlDataDocument xmlDoc;
			XmlNamespaceManager nsmgr;
			GetXmlDataDocument(RepositoryFile, out xmlDoc, out nsmgr);

			// do xpath query to find the font entry corresponding to the given encoding
			string strXPath = String.Format("//ec:encodings/ec:encoding[@name = //ec:fonts/ec:font[@name = '{0}']/ec:fontEncodings/ec:fontEncoding[@unique = 'true']/@name]/ec:defineMapping/@name", strFontName);
			XmlNode nMapping = xmlDoc.SelectSingleNode(strXPath, nsmgr);
			string strMappingName = null;
			if (nMapping != null)
			{
				strMappingName = nMapping.Value;
			}

			return strMappingName;
		}

		/// <summary>
		/// Shortcut routine for grabbing the /mappingRegistry/mappings/mapping/fontMappings information
		/// (i.e. the left-hand and right-hand side font associated with the given converter mapping
		/// </summary>
		/// <param name="strFriendlyName">friendly name of the converter mapping to check</param>
		/// <param name="strLhsFontName">output parameter for the left-hand side value</param>
		/// <param name="strRhsFontName">output parameter for the right-hand side value</param>
		/// <returns>true if the output parameters are filled in; otherwise false</returns>
		public bool GetFontMappingFromMapping(string strFriendlyName, out string strLhsFontName, out string strRhsFontName)
		{
			strLhsFontName = strRhsFontName = null;
			XmlDataDocument xmlDoc;
			XmlNamespaceManager nsmgr;
			GetXmlDataDocument(RepositoryFile, out xmlDoc, out nsmgr);

			// do xpath query to find the font entry corresponding to the given encoding
			try
			{
				string sDummyImplementType;
				strFriendlyName = GetMappingName(strFriendlyName, out sDummyImplementType);  // get name without ImplType
				string strXPath = String.Format("//ec:mappings/ec:mapping[@name = '{0}']/ec:fontMappings/ec:fontMapping", strFriendlyName);
				XmlNode nFontMapping = xmlDoc.SelectSingleNode(strXPath, nsmgr);
				if (nFontMapping != null)
				{
					GetXmlAttributeValue(nFontMapping.Attributes, "name", ref strLhsFontName);
					GetXmlAttributeValue(nFontMapping.Attributes, "assocFont", ref strRhsFontName);
					return true;
				}
			}
			catch { }   // could get an XPathException... (e.g. if temporary converter), so just treat it as 'didn't find' and return false

			return false;
		}

		/// <summary>
		/// Remove an item from the collection and the persistent store
		/// </summary>
		/// <param name="mapName">friendly name of the map to remove</param>
		[Description("Remove an item from the collection and the persistent store (e.g. '.Remove(\"Annapurna<>UNICODE\")')"),Category("Data")]
		public override void Remove(object mapName)
		{
			string strMapName = StringMapNameFromObject(mapName);   // allow int indexing

			// triangulate the given mapping name which might be a combination
			//	of mapping name + implementation type so we can remove only the one spec
			//  if it is combined
			string sImplementType;
			string strConverterName = GetMappingName(strMapName, out sImplementType);

			RemoveFromStore(strConverterName,sImplementType);    // from XML file
			RemoveNonPersist(strMapName);  // from collection
		}

		/// <summary>
		/// Remove an item from the collection, but not the persistent store (e.g. to do your own filtering)
		/// </summary>
		/// <param name="mapName">friendly name of the converter to remove</param>
		[Description("Remove an item from the collection, but not the persistent store (e.g. '.RemoveNonPersist(\"Annapurna<>UNICODE\")')--for programmatic filtering"),Category("Data")]
		public void RemoveNonPersist(object mapName)
		{
			// first see if this name is a concatenation of mappingName+implementation
			string sImplementType;
			string strMapName = StringMapNameFromObject(mapName);
			string strConverterName = GetMappingName(strMapName, out sImplementType);

			if( sImplementType != null )
			{
				// this means that it already was concatenated (so there must be more than 1
				// spec) go ahead and remove this one now and we'll adjust the priority next
				base.Remove(strMapName);

				// Find the next highest priority one and make that the new alias.
				int nLastPriority = -10, nCount = 0;
				IEncConverter aECHighest = null;
				foreach(IEncConverter aEC in this.Values)
				{
					string strThisMappingName = GetMappingName(aEC.Name, out sImplementType);
					if( strThisMappingName == strConverterName )
					{
						nCount++;
						int nThisPriority = GetImplPriority(aEC.ImplementType);
						if( nThisPriority > nLastPriority )
						{
							aECHighest = aEC;
							nLastPriority = nThisPriority;
						}
					}
				}

				// get rid of the alias first since we're going to change it
				if( m_mapAliasNames.ContainsKey(strConverterName) )
					m_mapAliasNames.Remove(strConverterName);

				// if there's only going to be one left, then readjust its name.
				if( nCount == 1 )
				{
					Debug.Assert(base.ContainsKey(aECHighest.Name));
					base.Remove(aECHighest.Name);
					base[strConverterName] = aECHighest;
					aECHighest.Name = strConverterName;
				}
				else if( aECHighest != null )
				{
					// otherwise, at least adjust the alias name to point to this new
					//  highest priority one
					m_mapAliasNames[strConverterName] = aECHighest;
				}
			}
			else
			{
				int i = 0;
				ArrayList saNames = new ArrayList(i);

				// otherwise, the user gave us the un-concatenated name, so remove *all*
				//	converters that go by this name (I don't know what else to do)
				foreach(IEncConverter aEC in this.Values)
				{
					string strThisMappingName = GetMappingName(aEC.Name, out sImplementType);
					if( strThisMappingName == strConverterName )
						saNames.Add(aEC.Name);
				}

				foreach(string strName in saNames)
				{
//					IEncConverter ecTmp = (IEncConverter)base[strName];
					base.Remove(strName);
//					Marshal.ReleaseComObject(ecTmp);
				}

				// get rid of the alias if there's one also
				if( m_mapAliasNames.ContainsKey(strConverterName) )
					m_mapAliasNames.Remove(strConverterName);
			}
		}

		/// <summary>
		/// Remove an encoding name alias from the persistent store.
		/// </summary>
		/// <param name="alias">alias to remove</param>
		[Description("Remove an encoding name alias from the persistent store"),Category("Data")]
		public void RemoveAlias(string alias)
		{
			// load the XML file...
			mappingRegistry file = RepositoryFile;
			mappingRegistry.aliasDataTable aAliasDT = file.alias;

			// find it first and then remove it.
			foreach(mappingRegistry.aliasRow aAliasRow in aAliasDT)
			{
				if( aAliasRow.name == alias )
				{
					aAliasDT.RemovealiasRow(aAliasRow);
					WriteRepositoryFile(file);	// save changes.
					return;
				}
			}

			// fall thru means we didn't find it.
			ThrowError(ErrStatus.NoAliasName);
		}

		/// <summary>
		/// Remove all items from the collection and the persistent store
		/// </summary>
		[Description("Remove all items from the collection and the persistent store"),Category("Data")]
		public override void Clear()
		{
			// we should be careful, however, not to clobber the implementation section since
			//  that will probably only be populated (via AddImplementation) during
			//  installation. So be careful to only get rid of the *other* sections.
			mappingRegistry file = RepositoryFile;
			mappingRegistry.mappingsDataTable amappingsDT = file.mappings;
			int nCount = amappingsDT.Count;
			for(int i = 0; i < nCount; i++)
			{
				// normally, there should only be one, but with other possible XML file writers
				//  out there, we can never really know. If the table becomes corrupted, then
				//  'clear' is going to be the suggested solution, so here we'll try to
				//  anticipate other contingencies.
				mappingRegistry.mappingsRow aRow = amappingsDT[i];
				amappingsDT.RemovemappingsRow(aRow);
			}

			// now add one back for next time.
			amappingsDT.AddmappingsRow();

			// same thing for the encodings row(s)
			mappingRegistry.encodingsDataTable aencodingsDT = file.encodings;
			nCount = aencodingsDT.Count;
			for(int i = 0; i < nCount; i++)
			{
				mappingRegistry.encodingsRow aRow = aencodingsDT[i];
				aencodingsDT.RemoveencodingsRow(aRow);
			}

			// now add one back for next time.
			aencodingsDT.AddencodingsRow();

			// same thing for the fonts row(s)
			mappingRegistry.fontsDataTable afontsDT = file.fonts;
			nCount = afontsDT.Count;
			for(int i = 0; i < nCount; i++)
			{
				mappingRegistry.fontsRow aRow = afontsDT[i];
				afontsDT.RemovefontsRow(aRow);
			}

			// now add one back for next time.
			afontsDT.AddfontsRow();

			WriteRepositoryFile(file);	// save changes.

			// remove them from the collection as well
			base.Clear();
			m_mapAliasNames.Clear();
		}

		/// <summary>
		/// Returns a collection of converters relevant to the given 'encoding name', optionally filtered by the process type (see ProcessTypeFlags enum)
		/// </summary>
		/// <param name="encoding">name of the encoding on which to filter the collection</param>
		/// <param name="processType">indication of the process type(s) to filter on as well (e.g. only UnicodeEncodingConversion)</param>
		/// <returns>the filtered collection of converters</returns>
		[Description("Returns a collection of converters relevant to the given 'encoding', optionally filtered by the process type (see ProcessTypeFlags enum)"),Category("Data")]
		public EncConverters FilterByEncodingID(string encoding, ProcessTypeFlags processType)
		{
			// make a new one of us first (but use a special ctor that doesn't attempt to add
			//  any converters itself)
			EncConverters aECs = new EncConverters(true);

			// load the XML file and find the encoding ID entry corresponding to the given
			//  name
			mappingRegistry file = RepositoryFile;
			encoding = GetEncodingName(file,encoding);  // handle aliases
			mappingRegistry.encodingRow aRow = file.encoding.FindByname(encoding);
			if( aRow == null )
				ThrowError(ErrStatus.NoEncodingName,encoding);

			// first get any mappings associated with the defineMapping element
			int pt = (int)processType;
			mappingRegistry.defineMappingRow [] aDefMapRows = aRow.GetdefineMappingRows();
			if( aDefMapRows.Length > 0 )
			{
				// doing it this way (i.e. using the mapping name to query the converter)
				//  will always get us only the highest priority spec for this mapping name
				//  (which I think is what we want for these cases)
				IEncConverter aEC = this[aDefMapRows[0].name];
				if( aEC != null )   // might be one we don't support (i.e. not in collection)
				{
					// first check for the Unicode encoding converters case (in this case, we only
					//  want to return a single converter normally)
					if( processType == ProcessTypeFlags.UnicodeEncodingConversion )
					{
						aECs.Add(aDefMapRows[0].name,aEC);
						return aECs;
					}
					// otherwise, possibly filter based on the processType
					else if(    (pt == -1)  // all bits on means get all
							||  (processType == ProcessTypeFlags.DontKnow)  // same here
							||  (((int)aEC.ProcessType & pt) != 0)
					)
					{
						aECs.Add(aDefMapRows[0].name,aEC);
					}
				}
			}

			// for fall-thru, it means there either was no define-mapping or it was something
			//  besides a UnicodeEncodingConversion.
			// iterate the encodingMapping rows also and put them in the new collection to be
			//  returned (if we aren't excluding it on the basis of the process type flag)
			InsureEncodingMappingsRow(file,aRow);
			mappingRegistry.encodingMappingRow[] aToMapRows =
				aRow.GetencodingMappingsRows()[0].GetencodingMappingRows();

			foreach(mappingRegistry.encodingMappingRow aToMapRow in aToMapRows)
			{
				IEncConverter aEC = this[aToMapRow.name];

				// filter if requested by user
				// either don't care or all gets all
				if(     (aEC != null)
					&&  (   (pt == -1)
						||  (processType == ProcessTypeFlags.DontKnow)
						||  (((int)aEC.ProcessType & pt) != 0)
						)
				)
				{
					// some converters (e.g. 'null' transliterator) may be in the list twice, but only
					//  include it once (or 'Add' throws).
					if( !aECs.ContainsKey(aToMapRow.name) )
						aECs.Add(aToMapRow.name,aEC);
				}
			}

			return aECs;
		}

		public IEncConverters ByEncodingID(string encoding, ProcessTypeFlags processType)
		{
			return (IEncConverters)FilterByEncodingID(encoding, processType);
		}

		/// <summary>
		/// Returns a collection of converters that implement the same processType
		/// </summary>
		/// <param name="processType">indication of the process type(s) to filter on (e.g. only UnicodeEncodingConversion)</param>
		/// <returns>the filtered collection of converters</returns>
		[Description("Returns a collection of converters that implement the same processType (e.g. UnicodeEncodingConversion)"),Category("Data")]
		public EncConverters FilterByProcessType(ProcessTypeFlags processType)
		{
			// load the XML file and find the font entry corresponding to the given name
			mappingRegistry file = RepositoryFile;
			InsureMappingsRow(file);

			int pt = (int)processType;
			EncConverters aECs = new EncConverters(true);
			foreach(mappingRegistry.mappingRow aMapRow in file.mapping)
			{
				// doing it this way (i.e. using the mapping name to query the converter)
				//  will always get us only the highest priority spec for this mapping name
				//  (which I think is what we want for these cases)
				IEncConverter aEC = this[aMapRow.name];

				if(     (aEC != null)   // might be one we don't support (i.e. not in collection)
					&&  (   (pt == -1)  // all bits on means get all
						||  (processType == ProcessTypeFlags.DontKnow)  // same here
						||  (((int)aEC.ProcessType & pt) != 0)
						)
				)
				{
					aECs.Add(aMapRow.name,aEC);
				}
			}

			return aECs;
		}

		public IEncConverters ByProcessType(ProcessTypeFlags processType)
		{
			return (IEncConverters)FilterByProcessType(processType);
		}

		/// <summary>
		/// Returns a collection of converters that are implemented by the same processor (e.g. 'SIL.tec'), optionally filtered by a process type (e.g. 'Transliterators')
		/// </summary>
		/// <param name="strImplType">name of the implementation to filter on (see EncConverters.strType* string members)</param>
		/// <param name="processType">indication of the process type(s) to filter on (e.g. only UnicodeEncodingConversion)</param>
		/// <returns>the filtered collection of converters</returns>
		[Description("Returns a collection of converters that are implemented by the same processor (e.g. 'SIL.tec'), optionally filtered by a process type (e.g. 'Transliterators')"), Category("Data")]
		public EncConverters FilterByImplementationType(string strImplType, ProcessTypeFlags processType)
		{
			int pt = (int)processType;
			EncConverters aECs = new EncConverters(true);
			foreach(IEncConverter aEC in Values)
			{
				if( aEC.ImplementType == strImplType )
				{
					if(     (pt == -1)  // all bits on means get all
						||  (processType == ProcessTypeFlags.DontKnow)  // same here
						||  (((int)aEC.ProcessType & pt) != 0) )
					{
						aECs.Add(aEC.Name,aEC);
					}
				}
			}

			return aECs;
		}

		public IEncConverters ByImplementationType(string strImplType, ProcessTypeFlags processType)
		{
			return (IEncConverters)FilterByImplementationType(strImplType, processType);
		}

		/// <summary>
		/// Returns a collection of converters relevant to the given 'fontName', optionally filtered by the process type (see ProcessTypeFlags enum)
		/// </summary>
		/// <param name="fontName">name of the font to find associated converters for</param>
		/// <param name="processType">indication of the process type(s) to filter on (e.g. only UnicodeEncodingConversion)</param>
		/// <returns>the filtered collection of converters</returns>
		[Description("Returns a collection of converters relevant to the given 'fontName', optionally filtered by the process type (see ProcessTypeFlags enum)"), Category("Data")]
		public EncConverters FilterByFontName(string fontName, ProcessTypeFlags processType)
		{
			// load the XML file and find the font entry corresponding to the given name
			mappingRegistry file = RepositoryFile;
			mappingRegistry.fontRow aRow = file.font.FindByname(fontName);
			if( aRow == null )
				throw new ArgumentOutOfRangeException();

			EncConverters aECs = null;
			if( processType != ProcessTypeFlags.UnicodeEncodingConversion )
				aECs = new EncConverters(true);

			// as with ByEncodingID, we'll treat the UnicodeEncodingConversion processType
			//  as special: if UEC, then get the fontEncoding with @unique=true and find
			//  the define mapping from that encodingName.
			InsureFontEncodingsRow(file,aRow);
			foreach(mappingRegistry.fontEncodingRow aFontEncodingRow in aRow.GetfontEncodingsRows()[0].GetfontEncodingRows())
			{
				if(     (processType == ProcessTypeFlags.UnicodeEncodingConversion)
					&&  (aFontEncodingRow.unique == true) )
				{
					return FilterByEncodingID(aFontEncodingRow.name,processType);
				}
				else
				{
					// otherwise, get all mappings according to the encodingID for *all*
					//  the fontEncoding rows
					foreach(IEncConverter aEC in ByEncodingID(aFontEncodingRow.name,processType).Values)
					{
						aECs.Add(aEC.Name,aEC);
					}
				}
			}

			return aECs;
		}

		public IEncConverters ByFontName(string fontName, ProcessTypeFlags processType)
		{
			return (IEncConverters)FilterByFontName(fontName, processType);
		}

		/// <summary>
		/// Returns a collection of converter or transliterator names supported by the converter type associated with the 'ProcessTypeFlag' parameter
		/// </summary>
		/// <param name="processType">parameter type associated with the converter (e.g. ICUTransliteration)</param>
		/// <returns>array of converter specs supported by the transduction engine associated with the given process type</returns>
		[Description("Returns a collection of converter or transliterator names supported by the converter type associated with the 'ProcessTypeFlag' parameter"),Category("Data")]
		public string [] EnumByProcessType(ProcessTypeFlags processType)
		{
			// first find the converter corresponding to this process type flag
			int processTypeFlag = (int)processType;
			string strKey = BY_PROCESS_TYPE + processTypeFlag.ToString();
			string strImplementationType = (string)m_mapToImplType[strKey];

			if( strImplementationType == null )
				throw new ArgumentOutOfRangeException();

			// now ask that converter to give back the array of strings for it's converter
			//  names list
			string strProgID = (string)m_mapImplTypeToProgId[strImplementationType];

#if !DontCheckAssemVersion
			IEncConverter rConverter = InstantiateIEncConverter(strProgID);

			if (rConverter == null)
				throw new ApplicationException(String.Format("Unable to create an object of type '{0}'", strProgID));
#else
			Type typeConverter = Type.GetTypeFromProgID(strProgID);
			IEncConverter rConverter = (IEncConverter) Activator.CreateInstance(typeConverter);
#endif

			return rConverter.ConverterNameEnum;
		}

		public IEncConverter InstantiateIEncConverter(string strProgID)
		{
			System.Diagnostics.Debug.Assert(!String.IsNullOrEmpty(strProgID));

			// Initially, we created the IEncConverter objects based on the prog id from the registry.
			//  (see comments near "Item" for why we're dealing with the interface rather
			//  than the coclass; i.e. IEncConverter rather than EncConverter).
			// But this doesn't work if we try to get, say, the TecEncConverter out of another version of this
			//  same assembly--which will happen if a newer version of it is installed, because the ProgId way
			//  of instantiation is equivalent to a COM instantiation, which always gets the object out of the
			//  newest version of the assembly. So we had situations where 2.2.2 was trying to get the
			//  TecEncConverter object out of 2.2.5 and the cast to 2.2.2 version of IEncConverter in that case
			//  fails, because that different version of the assembly will have different IEncConverter definitions.
			// So... get it out of the assembly (typically, the local assembly) that was built against the
			//  same version as this assembly.
			IEncConverter rConverter = null;
			string strAssemblySpec = null;
			if (m_mapProgIdsToAssemblyName.TryGetValue(strProgID, out strAssemblySpec))
			{
				ObjectHandle ohndl = Activator.CreateInstance(strAssemblySpec, strProgID);
				rConverter = (IEncConverter)ohndl.Unwrap();
			}
			else
			{
				// the fall back is just to give it a stab via its progid
				// see if we have some other assembly with the same version number we can pull it out of
				Type typeEncConverter = Type.GetTypeFromProgID(strProgID);
				if (typeEncConverter != null)
					rConverter = (IEncConverter)Activator.CreateInstance(typeEncConverter);
			}

			return rConverter;
		}

		// there are four nodes in the XML file that contain properties: in the individual
		//  mapping elements (which we don't support), the spec elements, the encoding
		//  elements, and the font elements. They can either be queries from the converters
		//  themselves (as we do in AddConversionMap, as an array of strings), or if they
		//  want to be able to add/remove from the list, then they must get the object
		//  returned by this method. They give a string entry name and a type indicating
		//  which of the three sets of attributes they want to acquire.
		/// <summary>
		/// Retrieve the ECAttributes collection for a particular repository item
		/// </summary>
		/// <param name="sItem">name of the item</param>
		/// <param name="repositoryItem">type of the item</param>
		/// <returns>Attribute collection for the requested repository item</returns>
		[Description("Retrieve the ECAttributes collection for a particular repository item"),Category("Data")]
		public ECAttributes Attributes(string sItem, AttributeType repositoryItem)
		{
			// load the XML file and create and initialize the object we'll be returning.
			mappingRegistry file = RepositoryFile;
			ECAttributes aECProps = new ECAttributes(this, sItem, repositoryItem);

			// here we always just get what's in the XML file
			switch(repositoryItem)
			{
				case AttributeType.Converter:   // now: i.e. specProperties
				{
					string strImplementType = null;
					string strMappingName = EncConverters.GetMappingNameEx(sItem, out strImplementType);
					mappingRegistry.mappingRow aRow = file.mapping.FindByname(strMappingName);
					if( aRow == null )
						ThrowError(ErrStatus.NoConverter,sItem);

					EncConverters.InsureSpecsRow(file,aRow);
					mappingRegistry.specRow aSpecRow = null;
					if( strImplementType == null )
					{
						// in this case, there can only be one spec (or we won't know which
						//  to get)
						int nLen = aRow.GetspecsRows()[0].GetspecRows().Length;
						if( nLen > 1 )
							EncConverters.ThrowError(ErrStatus.NeedSpecTypeInfo,
								EncConverters.BuildConverterSpecNameEx(strMappingName,
								aRow.GetspecsRows()[0].GetspecRows()[0].type));
						else if( nLen == 1 )
							// just use the one's values
							aSpecRow = aRow.GetspecsRows()[0].GetspecRows()[0];
					}
					else
					{
						foreach(mappingRegistry.specRow aSpecRow2 in aRow.GetspecsRows()[0].GetspecRows())
						{
							if( aSpecRow2.type == strImplementType )
							{
								aSpecRow = aSpecRow2;
								break;
							}
						}
					}

					if( aSpecRow == null )
						throw new ArgumentOutOfRangeException();

					// see if there are any properties now (there might not be, but we still want
					//  to return the object so the user can add some)
					mappingRegistry.specPropertiesRow[] aAttrsRows = aSpecRow.GetspecPropertiesRows();
					if( aAttrsRows.Length > 0 )
					{
						mappingRegistry.specPropertiesRow aAttrsRow = aAttrsRows[0];
						foreach(mappingRegistry.specPropertyRow aAttrRow in aAttrsRow.GetspecPropertyRows())
							aECProps.AddNonPersist(aAttrRow.name,(aAttrRow.IsspecProperty_TextNull())
								? null : aAttrRow.specProperty_Text);
					}
					break;
				}
				case AttributeType.FontName:
				{
					mappingRegistry.fontRow aRow = file.font.FindByname(sItem);
					if( aRow == null )
						ThrowError(ErrStatus.NameNotFound, sItem);

					// see if there are any properties now (there might not be, but we still want
					//  to return the object so the user can add some)
					mappingRegistry.fontPropertiesRow[] aAttrsRows = aRow.GetfontPropertiesRows();
					if( aAttrsRows.Length > 0 )
					{
						mappingRegistry.fontPropertiesRow aAttrsRow = aAttrsRows[0];
						foreach(mappingRegistry.fontPropertyRow aAttrRow in aAttrsRow.GetfontPropertyRows())
							aECProps.AddNonPersist(aAttrRow.name, (aAttrRow.IsfontProperty_TextNull())
								? null : aAttrRow.fontProperty_Text);
					}
					break;
				}

				case AttributeType.EncodingID:
				{
					mappingRegistry.encodingRow aRow = file.encoding.FindByname(sItem);
					if( aRow == null )
						ThrowError(ErrStatus.NoEncodingName,sItem);

					// see if there are any properties now (there might not be, but we still want
					//  to return the object so the user can add some)
					mappingRegistry.encodingPropertiesRow[] aAttrsRows = aRow.GetencodingPropertiesRows();
					if( aAttrsRows.Length > 0 )
					{
						mappingRegistry.encodingPropertiesRow aAttrsRow = aAttrsRows[0];
						foreach(mappingRegistry.encodingPropertyRow aAttrRow in aAttrsRow.GetencodingPropertyRows())
							aECProps.AddNonPersist(aAttrRow.name,(aAttrRow.IsencodingProperty_TextNull())
								? null : aAttrRow.encodingProperty_Text);
					}
					break;
				}

				default:
					throw new ArgumentOutOfRangeException();
			};

			return aECProps;
		}

		public void AddAttribute(ECAttributes aECAttributes, object Key, object Value)
		{
			string sKey = (string)Key.ToString();
			string sValue = (string)Value.ToString();

			mappingRegistry file = GetRepositoryFile();
			switch (aECAttributes.Type)
			{
				case AttributeType.Converter:
					{
						string strImplementType = null;
						string strMappingName = GetMappingNameEx(aECAttributes.RepositoryItem, out strImplementType);
						mappingRegistry.mappingRow aRow = file.mapping.FindByname(strMappingName);
						if (aRow == null)
						{
							throw new ArgumentOutOfRangeException();
						}

						InsureSpecsRow(file, aRow);
						mappingRegistry.specRow aSpecRow = null;
						if (strImplementType == null)
						{
							// in this case, there can only be one spec (or we won't know which
							//  to get)
							int nLen = aRow.GetspecsRows()[0].GetspecRows().Length;
							if (nLen > 1)
								ThrowError(ErrStatus.NeedSpecTypeInfo,
									BuildConverterSpecNameEx(strMappingName,
									aRow.GetspecsRows()[0].GetspecRows()[0].type));
							else if (nLen == 1)
								// just use the one's values
								aSpecRow = aRow.GetspecsRows()[0].GetspecRows()[0];
						}
						else
						{
							foreach (mappingRegistry.specRow aSpecRow2 in aRow.GetspecsRows()[0].GetspecRows())
							{
								if (aSpecRow2.type == strImplementType)
								{
									aSpecRow = aSpecRow2;
									break;
								}
							}
						}

						if (aSpecRow == null)
							throw new ArgumentOutOfRangeException();

						mappingRegistry.specPropertiesRow[] aAttrsRows = aSpecRow.GetspecPropertiesRows();
						mappingRegistry.specPropertiesRow aAttrsRow = null;
						if (aAttrsRows.Length == 0)
							aAttrsRow = file.specProperties.AddspecPropertiesRow(aSpecRow);
						else
							aAttrsRow = aSpecRow.GetspecPropertiesRows()[0];

						foreach (mappingRegistry.specPropertyRow aAttrRow in aAttrsRow.GetspecPropertyRows())
						{
							// see if we find it already existing.
							if (aAttrRow.name == sKey)
							{
								aAttrRow.specProperty_Text = sValue;
								WriteRepositoryFile(file);
								return;
							}
						}

						// if we fall thru, then it means we have to add it.
						file.specProperty.AddspecPropertyRow(sKey, sValue, aAttrsRow);
						break;
					}
				case AttributeType.FontName:
					{
						mappingRegistry.fontRow aRow = file.font.FindByname(aECAttributes.RepositoryItem);
						if (aRow == null)
						{
							throw new ArgumentOutOfRangeException();
						}

						mappingRegistry.fontPropertiesRow[] aAttrsRows = aRow.GetfontPropertiesRows();
						mappingRegistry.fontPropertiesRow aAttrsRow = null;
						if (aAttrsRows.Length == 0)
							aAttrsRow = file.fontProperties.AddfontPropertiesRow(aRow);
						else
							aAttrsRow = aRow.GetfontPropertiesRows()[0];

						foreach (mappingRegistry.fontPropertyRow aAttrRow in aAttrsRow.GetfontPropertyRows())
						{
							// see if we find it already existing.
							if (aAttrRow.name == sKey)
							{
								aAttrRow.fontProperty_Text = sValue;
								WriteRepositoryFile(file);
								return;
							}
						}

						// if we fall thru, then it means we have to add it.
						file.fontProperty.AddfontPropertyRow(sKey, sValue, aAttrsRow);
						break;
					}

				case AttributeType.EncodingID:
					{
						mappingRegistry.encodingRow aRow = file.encoding.FindByname(aECAttributes.RepositoryItem);
						if (aRow == null)
						{
							throw new ArgumentOutOfRangeException();
						}

						mappingRegistry.encodingPropertiesRow[] aAttrsRows = aRow.GetencodingPropertiesRows();
						mappingRegistry.encodingPropertiesRow aAttrsRow = null;
						if (aAttrsRows.Length == 0)
							aAttrsRow = file.encodingProperties.AddencodingPropertiesRow(aRow);
						else
							aAttrsRow = aRow.GetencodingPropertiesRows()[0];

						foreach (mappingRegistry.encodingPropertyRow aAttrRow in aAttrsRow.GetencodingPropertyRows())
						{
							// see if we find it already existing.
							if (aAttrRow.name == sKey)
							{
								aAttrRow.encodingProperty_Text = sValue;
								WriteRepositoryFile(file);
								return;
							}
						}

						// if we fall thru, then it means we have to add it.
						file.encodingProperty.AddencodingPropertyRow(sKey, sValue, aAttrsRow);
						break;
					}

				default:
					throw new ArgumentOutOfRangeException();
			};

			WriteRepositoryFile(file);	// save changes.
		}

		public void RemoveAttribute(ECAttributes aECAttributes, object Key)
		{
			string sKey = (string)Key;
			RemoveNonPersist(sKey);
			mappingRegistry file = EncConverters.GetRepositoryFile();
			switch (aECAttributes.Type)
			{
				case AttributeType.Converter:
					{
						string strImplementType = null;
						string strMappingName = GetMappingNameEx(aECAttributes.RepositoryItem, out strImplementType);
						mappingRegistry.mappingRow aRow = file.mapping.FindByname(strMappingName);
						if (aRow == null)
						{
							throw new ArgumentOutOfRangeException();
						}

						InsureSpecsRow(file, aRow);
						mappingRegistry.specRow aSpecRow = null;
						if (strImplementType == null)
						{
							// in this case, there can only be one spec (or we won't know which
							//  to get)
							int nLen = aRow.GetspecsRows()[0].GetspecRows().Length;
							if (nLen > 1)
								ThrowError(ErrStatus.NeedSpecTypeInfo,
									BuildConverterSpecNameEx(strMappingName,
									aRow.GetspecsRows()[0].GetspecRows()[0].type));
							else if (nLen == 1)
								// just use the one's values
								aSpecRow = aRow.GetspecsRows()[0].GetspecRows()[0];
						}
						else
						{
							foreach (mappingRegistry.specRow aSpecRow2 in aRow.GetspecsRows()[0].GetspecRows())
							{
								if (aSpecRow2.type == strImplementType)
								{
									aSpecRow = aSpecRow2;
									break;
								}
							}
						}

						if (aSpecRow == null)
							throw new ArgumentOutOfRangeException();

						mappingRegistry.specPropertiesRow aAttrsRow = aSpecRow.GetspecPropertiesRows()[0];
						foreach (mappingRegistry.specPropertyRow aAttrRow in aAttrsRow.GetspecPropertyRows())
						{
							// see if we find it already existing.
							if (aAttrRow.name == sKey)
							{
								file.specProperty.RemovespecPropertyRow(aAttrRow);
								WriteRepositoryFile(file);
								return;
							}
						}
						break;
					}
				case AttributeType.FontName:
					{
						mappingRegistry.fontRow aRow = file.font.FindByname(aECAttributes.RepositoryItem);
						if (aRow == null)
						{
							throw new ArgumentOutOfRangeException();
						}

						mappingRegistry.fontPropertiesRow aAttrsRow = aRow.GetfontPropertiesRows()[0];
						foreach (mappingRegistry.fontPropertyRow aAttrRow in aAttrsRow.GetfontPropertyRows())
						{
							// see if we find it already existing.
							if (aAttrRow.name == sKey)
							{
								file.fontProperty.RemovefontPropertyRow(aAttrRow);
								WriteRepositoryFile(file);
								return;
							}
						}
						break;
					}

				case AttributeType.EncodingID:
					{
						mappingRegistry.encodingRow aRow = file.encoding.FindByname(aECAttributes.RepositoryItem);
						if (aRow == null)
						{
							throw new ArgumentOutOfRangeException();
						}

						mappingRegistry.encodingPropertiesRow aAttrsRow = aRow.GetencodingPropertiesRows()[0];
						foreach (mappingRegistry.encodingPropertyRow aAttrRow in aAttrsRow.GetencodingPropertyRows())
						{
							// see if we find it already existing.
							if (aAttrRow.name == sKey)
							{
								file.encodingProperty.RemoveencodingPropertyRow(aAttrRow);
								WriteRepositoryFile(file);
								return;
							}
						}
						break;
					}
			};

			// if we fall thru, then it means the property doesn't exist.
			throw new ArgumentOutOfRangeException();
		}

		// used to initialize the path to the XML file in the registry
		/// <summary>
		/// Initialize the path to the XML file in the registry.
		/// </summary>
		/// <param name="strRepositoryFile"></param>
		public static void WriteStorePath(string strRepositoryFile)
		{
			try
			{
				// see if a user-based key is present
				RegistryKey key = Registry.CurrentUser.OpenSubKey(EncConverters.HKLM_PATH_TO_XML_FILE, true);
				if (key == null)
					// otherwise, check whether this user has permission to write the key in HKLM
					key = Registry.LocalMachine.CreateSubKey(EncConverters.HKLM_PATH_TO_XML_FILE);

				key.SetValue(strRegKeyForStorePath, strRepositoryFile);
			}
			catch (UnauthorizedAccessException ex)
			{
				throw new ApplicationException(String.Format("You don't have sufficient privilege to write to the registry.{0}Re-install SILConverters in order to get the proper registry key or manually add the following string key to the registry:{0}{0}{1}{0}\"{2}\" (REG_SZ) = \"{3}\"",
					Environment.NewLine, @"HKEY_LOCAL_MACHINE\" + EncConverters.HKLM_PATH_TO_XML_FILE, strRegKeyForStorePath, strRepositoryFile), ex);
			}
		}

		// provide an object accessible access method (for unmanaged clients that can't use
		//  the corresponding static version).
		/// <summary>
		/// or converting between common Unicode encodings (e.g. UTF16<>UTF32BE)
		/// </summary>
		/// <param name="sInput">input string to convert</param>
		/// <param name="eFormInput">indication of the transfer format of the input string</param>
		/// <param name="ciInput">number of characters in the input string (depends on the transfer format)</param>
		/// <param name="eFormOutput">indication of the transfer format requested for the output string</param>
		/// <param name="eNormalizeOutput">indication of the normalization requested for the output string</param>
		/// <param name="nNumItems">out parameter indicating the number of units in the output string (depends on the transfer format)</param>
		/// <returns>converted string</returns>
		[Description("For converting between common Unicode encodings (e.g. UTF16<>UTF32BE)"),Category("Data")]
		public string UnicodeEncodingFormConvert(string sInput, EncodingForm eFormInput, int ciInput, EncodingForm eFormOutput, NormalizeFlags eNormalizeOutput, out int nNumItems)
		{
			return UnicodeEncodingFormConvertEx(sInput, eFormInput, ciInput, eFormOutput, eNormalizeOutput, out nNumItems);
		}

		/// <summary>
		/// Retrieve the DataSet for the XML Repository file (for specialized clients who want access to
		/// something not otherwise available in the interface)
		/// </summary>
		[Description("Retrieve the DataSet for the XML Repository file"),Category("Data")]
		public mappingRegistry RepositoryFile
		{
			get { return GetRepositoryFile(); }
		}

		/// <summary>
		/// Return the name of the encodings defined in the repository
		/// </summary>
		[Description("Return the name of the encodings defined in the repository"),Category("Data")]
		public string [] Encodings
		{
			get
			{
				mappingRegistry file = RepositoryFile;
				InsureEncodingsRow(file);
				mappingRegistry.encodingRow[] aRows = file.encodings[0].GetencodingRows();
				string[] aStr = new string[aRows.Length];
				for(int i = 0; i < aRows.Length; i++ )
				{
					aStr[i] = aRows[i].name;
				}

				return aStr;
			}
		}

		/// <summary>
		/// Return the name of the mappings defined in the repository
		/// </summary>
		[Description("Return the name of the mappings defined in the repository"),Category("Data")]
		public string [] Mappings
		{
			get
			{
				mappingRegistry file = RepositoryFile;
				InsureMappingsRow(file);
				mappingRegistry.mappingRow[] aRows = file.mappings[0].GetmappingRows();
				string[] aStr = new string[aRows.Length];
				for(int i = 0; i < aRows.Length; i++ )
				{
					aStr[i] = aRows[i].name;
				}

				return aStr;
			}
		}

		/// <summary>
		/// Return the name of the fonts defined in the repository
		/// </summary>
		[Description("Return the name of the fonts defined in the repository"),Category("Data")]
		public string [] Fonts
		{
			get
			{
				mappingRegistry file = RepositoryFile;
				InsureFontsRow(file);
				mappingRegistry.fontRow[] aRows = file.fonts[0].GetfontRows();
				string[] aStr = new string[aRows.Length];
				for(int i = 0; i < aRows.Length; i++ )
				{
					aStr[i] = aRows[i].name;
				}

				return aStr;
			}
		}

		public string GetImplementationDisplayName(string strImplementationType)
		{
			string strDisplayName = null;
			string strProgId = (string)m_mapImplTypeToProgId[strImplementationType];
			if (!String.IsNullOrEmpty(strProgId) && m_mapDisplayNameToProgID.ContainsValue(strProgId))
			{
				IDictionaryEnumerator myEnumerator = m_mapDisplayNameToProgID.GetEnumerator();
				while (myEnumerator.MoveNext())
					if (strProgId == (string)myEnumerator.Value)
					{
						strDisplayName = (string)myEnumerator.Key;
						break;
					}
			}
			return strDisplayName;
		}

		public void GetImplementationDisplayNames
			(
			out string[] astrImplementationTypes,
			out string[] astrDisplayNames
			)
		{
			int nNumImplementations = m_mapImplTypeToProgId.Count;
			astrImplementationTypes = new string[nNumImplementations];
			astrDisplayNames = new string[nNumImplementations];

			IDictionaryEnumerator myImplementationTypeEnumerator = m_mapImplTypeToProgId.GetEnumerator();
			while (myImplementationTypeEnumerator.MoveNext())
			{
				string strImplementationType = (string)myImplementationTypeEnumerator.Key;
				string strDisplayName = strImplementationType;  // in case there is no display name
				string strProgId = (string)myImplementationTypeEnumerator.Value;
				IDictionaryEnumerator myEnumerator = m_mapDisplayNameToProgID.GetEnumerator();
				while (myEnumerator.MoveNext())
					if (strProgId == (string)myEnumerator.Value)
					{
						strDisplayName = (string)myEnumerator.Key;
						break;
					}

				astrImplementationTypes[--nNumImplementations] = strImplementationType;
				astrDisplayNames[nNumImplementations] = strDisplayName;
			}
		}

/*      this seems of questionable usefulness. Perhaps it should have 3 out parameters which
 *      return arrays of the spec attributes (i.e. type, path, and direction)
		[Description("Return the implementation type of the spec(s) of the given mappingName"),Category("Data")]
		public string [] Specs(string mappingName)
		{
			mappingRegistry file = RepositoryFile;
			mappingRegistry.mappingRow aMapRow = file.mapping.FindByname(mappingName);
			InsureSpecsRow(file,aMapRow);
			mappingRegistry.specRow[] aRows = aMapRow.GetspecsRows()[0].GetspecRows();
			string[] aStr = new string[aRows.Length];
			for(int i = 0; i < aRows.Length; i++ )
			{
				aStr[i] = aRows[i].type;
			}
			return aStr;
		}
*/
		/// <summary>
		/// If you want to get a converter for a specific implementation of a mapping use this method to build the key.
		/// This is useful in the case there are multiple implementations of the same converter and you want a specific
		/// one, particularly the one with the lower priority, since the higher priority one will be automatically
		/// return when you use just the mapping name as the key.
		/// </summary>
		/// <param name="mappingName">friendly name of the mapping to get</param>
		/// <param name="implementType">the implementation string (see EncConverters.strType* string members)</param>
		/// <returns>the modified friendly key to the specific implementation converter</returns>
		[Description("If you want to get a converter for a specific implementation of a mapping use this string as the key"),Category("Data")]
		public string BuildConverterSpecName(string mappingName, string implementType)
		{
			return BuildConverterSpecNameEx(mappingName, implementType);
		}

		// wherever we have 'mappingName' at the interface, it might either be the name of
		//	the 'mapping' entry in the XML file (ie. /mappings/mapping/@name) or it might
		//	be the combination of that mapping name, plus the implementType (e.g.
		//	</mappings/mapping/@name>:<../specs/spec/@type> -- in case the user wants a
		//	particular spec within a mapping). The name we 'publish' (i.e. via the
		//	EncConverter.Name property and the 'key' on which the collection is key'd) is
		//	the former when there's only one spec in the specs node and always the latter
		//	when there are multiple.
		//	i.e. either:
		//		"Annapurna<>Unicode"
		//	or
		//		"Annapurna<>Unicode (SIL.cc)"
		//		"Annapurna<>Unicode (SIL.tec)"
		/// <summary>
		/// Give a mapName and it is returned stripped of any implementation type names (which is returned in the implementType out parameter).
		/// e.g. "Annapurna<>Unicode (SIL.tec)" returns "Annapurna<>Unicode" and implementType = "(SIL.tec)"
		/// </summary>
		/// <param name="mapName">friendly name of the mapping with possible implementation type added</param>
		/// <param name="implementType">output parameter with the </param>
		/// <returns></returns>
		[Description("Give a mapName and it is returned stripped of any implementation type names (which is returned in the implementType out parameter)"),Category("Data")]
		public string GetMappingName(string mapName, out string implementType)
		{
			// houston, we have a problem... what if the user gives us a mapName that *has* an implmentation
			//  type in it, but it *isn't* meant to represent one of multiple specs for the same mapping (which
			//  is what having an implementType as part of the name normally means). If so, then we'll mistakenly
			//  think that the implementation type is *not* part of the name.
			// (this is primarily an issue for the compound and fallback type converters which use the step
			//  names (e.g. (SIL IPA93<>UNICODE (SIL.tec)") as part of the full mapName.
			string str = GetMappingNameEx(mapName, out implementType);

			// so, first--if we have a non-null implementType--before we agree that this mapName is really in two
			//  parts, see if it exists in the keys collection as is, and that it has this implementation type
			//  that matches as well. Then and only then do we want to make the split.
			if( implementType != null )
			{
				// rde 17-Apr-06: this didn't work totally as anticipated. The above discussion was regarding
				//  the problem of the new Compound Converter UI sending us a friendly name which *includes* the
				//  steps name which may have an implementation name as part of it even though the compound
				//  converter isn't one of multiple specs that do the same things (but it will look like it).
				//  However, there's still a problem with the SILConvertersInstaller\SetupSC program which might
				//  try to access a converter by it's full name (including the impl spec) even though it might
				//  not be in the collection by that name (e.g. because it isn't even *in* the collection yet).
				//  In that case, it may show up in the collection as mapName (if there is another spec of the
				//  same mapping) or 'str' (if there's not another spec of the same mapping). So check both.
				// TODO: if we ever change the interface again, then add another "Item" method which takes both
				//  the friendly name AND the implementation type so we can satisfy this properly. It was stupid
				//  to put both pieces of information into the same data item (i.e. the Name can contain both the
				//  FriendlyName as well as the appended implementation type)...
				if( this.ContainsKey(mapName) ) // use ContainsKey so we check the alias' as well
				{
					IEncConverter aEC = (IEncConverter)base[mapName];
					if( aEC.ImplementType == implementType )
						return str; // it really looks like what it looks like...
				}
				else if( this.ContainsKey(str) )
				{
					IEncConverter aEC = (IEncConverter)base[str];
					if( aEC.ImplementType == implementType )
						return str; // it really looks like what it looks like...
				}
			}

			// otherwise, this means it was not what we thought, so just ignore that there was an implementType
			implementType = null;
			return mapName;
		}

		/// <summary>
		/// Launch the dialog to Configure a converter. First the "Choose Implementation" dialog is displayed.
		/// </summary>
		/// <param name="eConversionTypeFilter">if you want to specify the conversion type so it doesn't need to be dealt with by the user (e.g. if you know you only deal with Legacy_to_(from_)Unicode, then you can tell that here). Of course, if the user selects a Legacy_to_Legacy converter, then it'll fail to work, but...</param>
		/// <param name="strFriendlyName">if you want to specify the name of the converter so the user doesn't need to.</param>
		/// <returns>indicates whether the converter was configured or not</returns>
		[Description("Launch a dialog to Configure a converter"),Category("Data")]
		public bool AutoConfigure(ConvType eConversionTypeFilter, ref string strFriendlyName)
		{
			ImplTypeList dlg = new ImplTypeList(m_mapDisplayNameToProgID.Keys);
			if( dlg.ShowDialog() == DialogResult.OK )
			{
				string strProgID = (string)m_mapDisplayNameToProgID[dlg.SelectedDisplayName];

				try
				{
					// we create the EncConverter objects based on the prog id from the registry.
					//  (see comments near "Item" for why we're dealing with the interface rather
					//  than the coclass; i.e. IEncConverter rather than EncConverter).
#if !DontCheckAssemVersion
					IEncConverter rConverter = InstantiateIEncConverter(strProgID);

					if (rConverter == null)
						throw new ApplicationException(String.Format("Unable to create an object of type '{0}'", strProgID));
#else
					Type typeEncConverter = Type.GetTypeFromProgID(strProgID);
					IEncConverter rIEncConverter = (IEncConverter) Activator.CreateInstance(typeEncConverter);
#endif

					// call an 'internal' helper to do the rest (which also works for the SelectConverter.Edit)
					return  AutoConfigureEx(rConverter, eConversionTypeFilter, ref strFriendlyName, null, null);
				}
				catch (Exception e)
				{
					MessageBox.Show("AutoConfigure failed: " + e.Message, traceSwitch.DisplayName);
				}
			}

			return false;
		}

		public bool AutoConfigureEx
			(
			IEncConverter   rIEncConverter,
			ConvType        eConversionTypeFilter,
			ref string      strFriendlyName,
			string          strLhsEncodingID,
			string          strRhsEncodingID
			)
		{
			try
			{
				// get the configuration interface for this type
				IEncConverterConfig rConfigurator = rIEncConverter.Configurator;

				// call its Configure method to do the UI
				if( rConfigurator.Configure(this, strFriendlyName, eConversionTypeFilter, strLhsEncodingID, strRhsEncodingID) )
				{
					// if this is just a temporary converter (i.e. it isn't being added permanentally to the
					//  repository), then just make up a name so the caller can use it.
					if( !rConfigurator.IsInRepository )
					{
						DateTime dt = DateTime.Now;
						strFriendlyName = String.Format(cstrTempConverterPrefix + ": id: '{0}', created on '{1}' at '{2}'", rConfigurator.ConverterIdentifier, dt.ToLongDateString(), dt.ToLongTimeString());

						// in this case, the Configurator didn't update the name
						rIEncConverter.Name = strFriendlyName;

						// one final thing missing: for this 'client', we have to put it into the 'this' collection
						AddToCollection(rIEncConverter,strFriendlyName);
					}
					else
					{
						// else, if it was in the repository, then it should also be (have been) updated in
						//  the collection already, so just get its name so we can return it.
						strFriendlyName = rConfigurator.ConverterFriendlyName;
					}

					return true;
				}
				else if( rConfigurator.IsInRepository && !String.IsNullOrEmpty(rConfigurator.ConverterFriendlyName) )
				{
					// if the user added it to the repository and then *cancelled* it (i.e. so Configure
					//  returns false), then it *still* is in the repository and we should therefore return
					//  true.
					strFriendlyName = rConfigurator.ConverterFriendlyName;
					return true;
				}
			}
			catch
			{
#if DEBUG
				throw;
#endif
			}

			return false;
		}

		/// <summary>
		/// Launch a dialog to select one of the existing converters and/or add a new converter
		/// </summary>
		/// <param name="eConversionTypeFilter">if you want to specify the conversion type so it doesn't need to be dealt with by the user (e.g. if you know you only deal with Legacy_to_(from_)Unicode, then you can tell that here). Of course, if the user selects a Legacy_to_Legacy converter, then it'll fail to work, but...</param>
		/// <returns>returns the converter selected by the user</returns>
		[Description("Launch a dialog to select one of the existing converters and/or add a new converter"),Category("Data")]
		public IEncConverter AutoSelect(ConvType eConversionTypeFilter)
		{
			return AutoSelectWithTitle(eConversionTypeFilter, null);
		}

		/// <summary>
		/// Launch a dialog to select one of the existing converters and/or add a new converter, but allow the client to pass a string for the title of the dialog box
		/// </summary>
		/// <param name="eConversionTypeFilter">if you want to specify the conversion type so it doesn't need to be dealt with by the user (e.g. if you know you only deal with Legacy_to_(from_)Unicode, then you can tell that here). Of course, if the user selects a Legacy_to_Legacy converter, then it'll fail to work, but...</param>
		/// <param name="strChooseConverterDialogTitle">string to use in the Choose Converter dialog frame (e.g. "Choose Glossing Converter")</param>
		/// <returns>returns the converter selected by the user</returns>
		[Description("Launch a dialog to select one of the existing converters and/or add a new converter, but allow the client to pass a string for the title of the dialog box"), Category("Data")]
		public IEncConverter AutoSelectWithTitle(ConvType eConversionTypeFilter, string strChooseConverterDialogTitle)
		{
			SelectConverter dlg = new SelectConverter(this, eConversionTypeFilter, strChooseConverterDialogTitle, (string)null, (string)null);
			if (dlg.ShowDialog() == DialogResult.OK)
				return dlg.IEncConverter;   // return it, whatever it is (may be 0)
			else
				return null;
		}

		/// <summary>
		/// Return an empty, blank IEncConverter based on the implementation type. You need to call Initialize
		/// afterwards.
		/// </summary>
		/// <param name="strImplementationType">implementation type of the converter to return</param>
		/// <returns>the blank, uninitialized EncConverter based on the requested implementation type</returns>
		[Description("Return an IEncConverter based on the implementation type"),Category("Data")]
		public IEncConverter NewEncConverterByImplementationType(string strImplementationType)
		{
			if (String.IsNullOrEmpty(strImplementationType))
				ThrowError(ErrStatus.NameNotFound, strImplementationType);

			// we create the EncConverter objects based on the prog id from the registry.
			//  (see comments near "Item" for why we're dealing with the interface rather
			//  than the coclass; i.e. IEncConverter rather than EncConverter).
			string strProgID = (string)m_mapImplTypeToProgId[strImplementationType];

			if( String.IsNullOrEmpty(strProgID) )
				ThrowError(ErrStatus.NameNotFound, strProgID);

#if !DontCheckAssemVersion
			return InstantiateIEncConverter(strProgID);
#else
			Type typeEncConverter = Type.GetTypeFromProgID(strProgID);
			return (IEncConverter) Activator.CreateInstance(typeEncConverter);
#endif
		}

		public IEncConverter AutoSelectWithData(byte[] abyPreviewData, string strFontName, ConvType eConversionTypeFilter, string strChooseConverterDialogTitle)
		{
			SelectConverter dlg = new SelectConverter(this, eConversionTypeFilter, strChooseConverterDialogTitle, abyPreviewData, strFontName);
			if (dlg.ShowDialog() == DialogResult.OK)
				return dlg.IEncConverter;   // return it, whatever it is (may be 0)
			else
				return null;
		}

		public IEncConverter AutoSelectWithData(string strPreviewData, string strFontName, ConvType eConversionTypeFilter, string strChooseConverterDialogTitle)
		{
			SelectConverter dlg = new SelectConverter(this, eConversionTypeFilter, strChooseConverterDialogTitle, strPreviewData, strFontName);
			if (dlg.ShowDialog() == DialogResult.OK)
				return dlg.IEncConverter;   // return it, whatever it is (may be 0)
			else
				return null;
		}

		#endregion Public Methods

		#region Misc helpers
		// COM create and initialize a converter.
		internal IEncConverter AddEx(string strProgID, string mappingName,
			string converterSpec, ref string rLhsEncoding, ref string rRhsEncoding,
			ref ConvType eConversionType, ref int processTypeFlags, Int32 codePageInput,
			Int32 codePageOutput, bool bAddingPersist)
		{
			if( strProgID == null )
				ThrowError(ErrStatus.NoAvailableConverters);

			// we create the EncConverter objects based on the prog id from the registry.
			//  (see comments near "Item" for why we're dealing with the interface rather
			//  than the coclass; i.e. IEncConverter rather than EncConverter).
			// This doesn't work if we try to get, say, the TecEncConverter out of another version of this assembly,
			//  because that different version of the assembly will have a different IEncConverter definition.
			// So... try to get it out of the local assembly first
#if !DontCheckAssemVersion
			IEncConverter rConverter = InstantiateIEncConverter(strProgID);
#else
			Type typeEncConverter = Type.GetTypeFromProgID(strProgID);
			rIEncConverter = (IEncConverter)Activator.CreateInstance(typeEncConverter);
#endif

			if (rConverter == null)
				throw new ApplicationException(String.Format("Unable to create an object of type '{0}'", strProgID));

			// initialize and add to collection
			InitializeConverter(rConverter, mappingName, converterSpec,
				ref rLhsEncoding, ref rRhsEncoding, ref eConversionType,
				ref processTypeFlags, codePageInput, codePageOutput, bAddingPersist);

			// the 'bAddingPersist' flag is used to cause the Initialize routine to do some
			//  error checking (reading of maps, checking of parameters, etc), which only
			//  needs to be done the first time a converter is added to the collection.
			//  If everything is good after that, then during subsequently initializations,
			//  we can pass false and it shortens processing time during instantiation.
			// However, another piece of information is useful for converters and that's whether
			//  the converter is temporary or not (i.e. whether it is in the persistent store
			//  or not). We can't use bAddingPersist to determine this, however, because that
			//  may be false even though the converter is in the store (e.g. if we're 'AddEx'ing
			//  from the persistent store during AddActualConverters).
			// So, when this method is called (currently during AddActualConverters and during
			//  AddConversionMap), we know that the converter *is* or is *about* to be in the
			//  persistent store, so from here we can make the IsInRepository flag true.
			rConverter.IsInRepository = true;

			return rConverter;
		}

		internal IEncConverter InitializeConverter(IEncConverter rConverter, string converterName, string converterSpec, ref string rLhsEncoding, ref string rRhsEncoding, ref ConvType eConversionType, ref Int32 processType, Int32 codePageInput, Int32 codePageOutput, bool bAddingPersist)
		{
			rConverter.Initialize(converterName,converterSpec,ref rLhsEncoding,ref rRhsEncoding, ref eConversionType,ref processType,codePageInput,codePageOutput,bAddingPersist);

			AddToCollection(rConverter,converterName);

			return rConverter;
		}

		protected void AddToCollection(IEncConverter rConverter, string converterName)
		{
			// now add it to the 'this' collection
			// converterName.ToLower(); // this does nothing anyway, so get rid of it

			// no sense in allowing this to be added if it already exists because it'll always
			//  be hidden.
			if (ContainsKey(converterName))
			{
//				IEncConverter ecTmp = (IEncConverter)base[converterName];
				base.Remove(converterName); // always overwrite existing ones.
//				Marshal.ReleaseComObject(ecTmp);
			}

			base[converterName] = rConverter;
		}

		protected string AdjustMappingNames(string mappingNameNew, string implementTypeNew,
			ref IEncConverter aExistEC)
		{
			// this means that we already have (at least) one other spec with this same
			//	mapping name.
			Debug.Assert( aExistEC != null );

			// first, see if the existing one is *already* concatenated.
			string strImplementType = null;
			string strExistName = GetMappingName(aExistEC.Name, out strImplementType);

			// If it has the same implementation, then this is *replacing* the existing one.
			if( aExistEC.ImplementType == implementTypeNew )
			{
				RemoveNonPersist(aExistEC.Name);
				aExistEC = null;        // in this case, it doesn't really exist anymore...
				return mappingNameNew;  // so don't add the implement type (as in the last line below)
			}
			else if( strImplementType == null )
			{
				// otherwise, if it wasn't already concatenated... then do it now.
				string strNewNameExist = BuildConverterSpecNameEx(strExistName, aExistEC.ImplementType);

				// replace the existing one with the new combined name
				base.Remove(strExistName);
				aExistEC.Name = strNewNameExist;
				base[strNewNameExist] = aExistEC;
			}

			// if it was already concatenated, remove it from the alias map as well
			//  (it'll get re-set after we make the new converter if it's priority
			//	is high enough).
			if( m_mapAliasNames.ContainsKey(strExistName) )
				m_mapAliasNames.Remove(strExistName);

			// finally, the new map must also be concatenated
			return String.Format(strMapPlusImplFormat, mappingNameNew, implementTypeNew);
		}

		// allow for integer indexing of the collection as well
		protected string StringMapNameFromObject(object mapName)
		{
			string strMapName = null;
			if( mapName.GetType() != typeof(string) )
			{
				// it might be numeric, so try casting it as an int.
				int nIndex = -1;
				try
				{
					nIndex = (int)Convert.ToInt32(mapName);
				}
				catch{} // just it case it didn't like that either

				// I'm making this kind of numeric indexing is zero-based rather than
				//  1-based... I don't know if this is the right decision or not.
				if( (nIndex >= 0) && (nIndex < base.Count) )
				{
					IDictionaryEnumerator entry = base.GetEnumerator();
					while(nIndex-- >= 0)
						entry.MoveNext();

					strMapName = (string)entry.Key;
				}
			}

			// otherwise, try forcing the string cast in case that might work...
			return ((strMapName == null) ? (string)mapName : strMapName);
		}

		/// <summary>
		/// Shortcut to determine if the given conversion type is not bi-directional
		/// </summary>
		/// <param name="eConversionType">the conversion type to check</param>
		/// <returns>true if the conversion type is *not* bi-directional</returns>
		public static bool IsUnidirectional(ConvType eConversionType)
		{
			return ((eConversionType >= ConvType.Legacy_to_Unicode)
				&&  (eConversionType <= ConvType.Unicode_to_Unicode)
				);
		}

		// the following allows clients to have quick access to unicode encoding form conversions
		internal static EncConverter m_ecTec = null;
		public static string UnicodeEncodingFormConvertEx(string sInput, EncodingForm eFormInput, int ciInput, EncodingForm eFormOutput, NormalizeFlags eNormalizeOutput, out int nNumItems)
		{
			nNumItems = 0;
			string sOutput = null;

			// get the TECkit COM interface (if we don't have it already)
			if( m_ecTec == null )
				m_ecTec = new TecFormEncConverter();    // no need to initialize this one.

			if( m_ecTec != null )
				sOutput = m_ecTec.ConvertEx(sInput,eFormInput,ciInput,eFormOutput,out nNumItems,eNormalizeOutput,true);

			return sOutput;
		}

		public static string BuildConverterSpecNameEx(string mappingName, string implementType)
		{
			// must take into account users that don't pass us an implementType here.
			if( String.IsNullOrEmpty(implementType) )
				return mappingName;
			else
				return String.Format(strMapPlusImplFormat, mappingName, implementType);
		}

		// strip out the implementation portion of a mapName (e.g. the 'SIL.cc' out of
		//  "Annapurna<>UNICODE (SIL.cc)")
		public static string GetMappingNameEx(string mapName, out string implementType)
		{
			// if we have such a string, then it must have both a left and right paran
			//  AND the right paran must be the last char in the string.
			implementType = null;
			int nStartIndex = mapName.LastIndexOf(" (");
			int nEndIndex = mapName.LastIndexOf(')');
			if(		(nStartIndex != -1)
				&&	(nEndIndex != -1)
				&&	(nEndIndex > nStartIndex)
				&&  (nEndIndex == (mapName.Length - 1)) // must be the last char
				)
			{
				// this substring will be the implementation type.
				string strImplementType = mapName.Substring(nStartIndex+2,nEndIndex-nStartIndex-2);

				// the final huristic is that the sub-string ought to be in the hashtable of
				//  implementation names (this might cause a bug because not all
				//  implementation types are actually supported by EncConverters (in which
				//  case, they wouldn't be in this hashtable), however, the alternative of
				//  mistakenly thinking that the 'Dev' out of "ISCII (Dev)" is an implementation
				//  is a greater risk...
				if (!String.IsNullOrEmpty(strImplementType) && !String.IsNullOrEmpty((string)m_mapImplTypeToProgId[strImplementType]))
				{
					// found everything as it should be.
					implementType = strImplementType;
					return mapName.Substring(0,nStartIndex);
				}
			}

			return mapName;
		}

		// most client programs can't deal with byte[] when the cp is Symbol (since 42 throws an error in GetEncoding)
		public static byte[] GetBytesFromEncoding(int cp, string strInput, bool bUse8859_1AsFallback)
		{
			byte[] abyValues;
			if (cp == cnSymbolFontCodePage)
			{
				// for symbol encoded data, the bytes are just the sequence of the low bytes
				abyValues = new byte[strInput.Length];
				for (int i = 0; i < strInput.Length; i++)
					abyValues[i] = (byte)(strInput[i] & 0xFF);
			}
			else
			{
				try
				{
					abyValues = Encoding.GetEncoding(cp).GetBytes(strInput);
				}
				catch
				{
					if (bUse8859_1AsFallback)
						abyValues = Encoding.GetEncoding(EncConverters.cnIso8859_1CodePage).GetBytes(strInput);
					else
						throw;
				}
			}

			return abyValues;
		}

		// given an encoding name, return either that value or the parent encodingID if it
		//  is an alias
		internal string GetEncodingName(mappingRegistry file, string strEncoding)
		{
			if( strEncoding != null )
				foreach(mappingRegistry.aliasRow aAliasRow in file.alias)
				{
					if( aAliasRow.name == strEncoding )
					{
						// return the parent's parent's @name
						strEncoding = aAliasRow.aliasesRow.encodingRow.name;
						break;
					}
				}
			return strEncoding;
		}

		// helpers to convert between enum values and the strings we use in the XML files
		protected NormalizeFlags ToNormalizeFlags(string normalize)
		{
			NormalizeFlags eNormalize = NormalizeFlags.None;
			if( normalize == strDefNormalizeFlagFC )
				return NormalizeFlags.FullyComposed;
			else if( normalize == strDefNormalizeFlagFD )
				return NormalizeFlags.FullyDecomposed;
			return eNormalize;
		}

		protected string ToNormalizeStrings(NormalizeFlags normalize)
		{
			string str = null;
			switch(normalize)
			{
				case NormalizeFlags.None:
					str = strDefNormalizeFlagNone;
					break;
				case NormalizeFlags.FullyComposed:
					str = strDefNormalizeFlagFC;
					break;
				case NormalizeFlags.FullyDecomposed:
					str = strDefNormalizeFlagFD;
					break;
				default:
					ThrowError(ErrStatus.InvalidNormalizeForm);
					break;
			}
			return str;
		}

		protected ConvType ToConvType(string direction, string leftType, string rightType)
		{
			if( direction == strDefDirection )
			{
				if( leftType == strDefTypeBytes )
				{
					if( rightType == strDefTypeBytes )
						return ConvType.Legacy_to_from_Legacy;
					else
						return ConvType.Legacy_to_from_Unicode;
				}
				else
				{
					if( rightType == strDefTypeBytes )
						return ConvType.Unicode_to_from_Legacy;
					else
						return ConvType.Unicode_to_from_Unicode;
				}
			}
			else
			{
				if( leftType == strDefTypeBytes )
				{
					if( rightType == strDefTypeBytes )
						return ConvType.Legacy_to_Legacy;
					else
						return ConvType.Legacy_to_Unicode;
				}
				else
				{
					if( rightType == strDefTypeBytes )
						return ConvType.Unicode_to_Legacy;
					else
						return ConvType.Unicode_to_Unicode;
				}
			}
		}

		protected string ToProgId(string implementType)
		{
			return (string)m_mapImplTypeToProgId[implementType];
		}

		internal int GetImplPriority(string implementType)
		{
			int nPriority = cnDefImplPriority;
			try
			{
				nPriority = (int)m_mapImplementTypesPriority[implementType];
			}
			catch {}    // might throw if the implementType is screwed up.
			return nPriority;
		}

		// if str is "C:\file.doc", then strFilename will be "C:\file" and strExt will be ".doc"
		public static bool GetFileExtn(string str, out string strFilename, out string strExt)
		{
			int nPeriodIndex = str.LastIndexOf('.');
			if( nPeriodIndex == -1 )
			{
				strFilename = strExt = null;
				return false;
			}

			strFilename = str.Substring(0,nPeriodIndex);

			// some converters might have optional parameters in addition to the filename,
			//  so search forward for a space or a semi-colon (the two known delimiters)
			//  and if found, only grab that part for the extension
			int nLen = str.Length;
			int nSpaceIndex = str.IndexOfAny(new char [] {' ',';',':'}, nPeriodIndex);
			if( nSpaceIndex != -1 )
				nLen = nSpaceIndex;

			strExt = str.Substring(nPeriodIndex,nLen - nPeriodIndex);
			return true;
		}

		// basically, return if both sides are the same (irregardless of directionality)
		/// <summary>
		/// return true if both sides are the same (irregardless of directionality). e.g. returns 'true'
		/// if lhs = Legacy_to_from_Unicode and rhs = Legacy_to_Unicode
		/// </summary>
		/// <param name="eConvTypeLhs">conversion type of the left-hand side</param>
		/// <param name="eConvTypeRhs">conversion type of the right-hand side</param>
		/// <returns></returns>
		public static bool  IsConvTypeCompariable(ConvType eConvTypeLhs, ConvType eConvTypeRhs)
		{
			if( eConvTypeLhs == eConvTypeRhs )
				return true;    // same

			// otherwise, if they at least have the same lhs and rhs
			bool bCompariable = false;
			switch(eConvTypeLhs)
			{
				case ConvType.Legacy_to_from_Legacy:
					bCompariable = (eConvTypeRhs == ConvType.Legacy_to_Legacy);
					break;
				case ConvType.Legacy_to_from_Unicode:
					bCompariable = (eConvTypeRhs == ConvType.Legacy_to_Unicode);
					break;
				case ConvType.Legacy_to_Legacy:
					bCompariable = (eConvTypeRhs == ConvType.Legacy_to_from_Legacy);
					break;
				case ConvType.Legacy_to_Unicode:
					bCompariable = (eConvTypeRhs == ConvType.Legacy_to_from_Unicode);
					break;
				case ConvType.Unicode_to_from_Legacy:
					bCompariable = (eConvTypeRhs == ConvType.Unicode_to_Legacy);
					break;
				case ConvType.Unicode_to_from_Unicode:
					bCompariable = (eConvTypeRhs == ConvType.Unicode_to_Unicode);
					break;
				case ConvType.Unicode_to_Legacy:
					bCompariable = (eConvTypeRhs == ConvType.Unicode_to_from_Legacy);
					break;
				case ConvType.Unicode_to_Unicode:
					bCompariable = (eConvTypeRhs == ConvType.Unicode_to_from_Unicode);
					break;
			}
			return bCompariable;
		}

		// another version that can take direction into consideration
		/// <summary>
		/// return true if both sides are the same (this version take direction into consideration).
		/// e.g. returns 'true' if lhs = Legacy_to_from_Unicode, forward, and rhs = Unicode_to_Legacy, reverse
		/// </summary>
		/// <param name="eConvTypeLhs">conversion type of the left-hand side</param>
		/// <param name="bForwardLhs">direction of the left-hand side conversion type</param>
		/// <param name="eConvTypeRhs">conversion type of the right-hand side</param>
		/// <param name="bForwardRhs">direction of the right-hand side conversion type</param>
		/// <returns></returns>
		public static bool  IsConvTypeCompariable(ConvType eConvTypeLhs, bool bForwardLhs, ConvType eConvTypeRhs, bool bForwardRhs)
		{
			// if they're both going forward, then we can use the other routine.
			if( bForwardLhs != bForwardRhs )
				// ... otherwise, reverse one of them and then check.
				eConvTypeLhs = ReverseConvType(eConvTypeLhs);

			return IsConvTypeCompariable( eConvTypeLhs, eConvTypeRhs );
		}

		internal static ConvType ReverseConvType(ConvType eConvType)
		{
			ConvType eConvTypeRet = ConvType.Unknown;
			switch(eConvType)
			{
				case ConvType.Legacy_to_from_Legacy:
				case ConvType.Unicode_to_from_Unicode:
				case ConvType.Legacy_to_Legacy:
				case ConvType.Unicode_to_Unicode:
					eConvTypeRet = eConvType;
					// these are palendromes
					break;
				case ConvType.Legacy_to_from_Unicode:
					eConvTypeRet = ConvType.Unicode_to_from_Legacy;
					break;
				case ConvType.Legacy_to_Unicode:
					eConvTypeRet = ConvType.Unicode_to_Legacy;
					break;
				case ConvType.Unicode_to_from_Legacy:
					eConvTypeRet = ConvType.Legacy_to_from_Unicode;
					break;
				case ConvType.Unicode_to_Legacy:
					eConvTypeRet = ConvType.Legacy_to_Unicode;
					break;
			}
			return eConvTypeRet;
		}

		// shamelessly taken from: http://www.koders.com/csharp/fid8CDA7A212ED2D159102529D89D77BDCD2F14F3DC.aspx
		protected struct FONTSIGNATURE
		{
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public int[] fsUsb;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
			public int[] fsCsb;
		}
		protected struct CHARSETINFO
		{
			public int ciCharset;
			public int ciACP;
			public FONTSIGNATURE fs;
		}

		protected const int SYMBOL_CHARSET = 2;     // wingdi.h
		protected const int CP_SYMBOL = 42;         // winnls.h
		protected const int DEFAULT_CHARSET = 1;    // wingdi.h
		protected const int TCI_SRCFONTSIG = 3;     // wingdi.h
		protected const int TCI_SRCCHARSET = 1;     // wingdi.h
		protected IntPtr HGDI_ERROR = new IntPtr(-1);

		[DllImport("gdi32")]
		protected static extern int GetTextCharsetInfo(IntPtr hdc, ref FONTSIGNATURE lpSig, int dwFlags);
		[DllImport("gdi32")]
		protected static extern bool TranslateCharsetInfo(int lpSrc, ref CHARSETINFO lpcs, int dwFlags);
		[DllImport("gdi32")]
		protected static extern IntPtr SelectObject(IntPtr hdc, IntPtr hObject);

		protected unsafe int TryToGetCodePageFromFont(string strFontName)
		{
			// if we don't have the information in the repository, maybe we can get it from Windows.
			//  first get a Font object with the given font name (size doesn't matter :-)
			System.Drawing.Font font = new System.Drawing.Font(strFontName, 12);
			if (font.Name != strFontName)
				ThrowError(ErrStatus.InstallFont, strFontName);

			// next we have to select it into a device context
			System.Drawing.Graphics g = null;
			IntPtr hdc = IntPtr.Zero;
			IntPtr hOrigFont = HGDI_ERROR;
			try
			{
				// Get a GDI+ drawing surface (any will do)
				g = System.Drawing.Graphics.FromHwnd(IntPtr.Zero);

				// Get handle to device context for the graphics object
				hdc = g.GetHdc();

				// Get a handle to our font
				IntPtr hFont = font.ToHfont();

				// Replace currently selected font with our font
				hOrigFont = SelectObject(hdc, hFont);
				if (hOrigFont == HGDI_ERROR)
					throw new Exception("Unexpected failure of SelectObject.");

				FONTSIGNATURE fs = new FONTSIGNATURE();
				CHARSETINFO CSI = new CHARSETINFO();
				int nRet = GetTextCharsetInfo(hdc, ref fs, 0);
				if (nRet == SYMBOL_CHARSET)
					return CP_SYMBOL;
				else if (nRet != DEFAULT_CHARSET)
				{
					fixed (int* lpfsCsb = fs.fsCsb)
					{
						if (TranslateCharsetInfo((int)lpfsCsb, ref CSI, TCI_SRCFONTSIG))
							return CSI.ciACP;
					}
				}
				else
				{
					int nCharSet = (int)font.GdiCharSet;
					if (TranslateCharsetInfo((int)&nCharSet, ref CSI, TCI_SRCCHARSET))
						return CSI.ciACP;
				}
			}
			catch
			{
#if DEBUG
				throw;
#endif
			}
			finally
			{
				// release what we got
				if (hOrigFont != HGDI_ERROR)
					SelectObject(hdc, hOrigFont);
				if (g != null)
				{
					if (hdc != IntPtr.Zero)
						g.ReleaseHdc(hdc);
					g.Dispose();
				}
			}

			// if we fall thru, it means we couldn't figure it out.
			ThrowError(ErrStatus.AddFontFirst, strFontName);
			return cnDefCodePage;   // unreachable
		}

		#endregion Misc helpers

		#region XML file Get/Set
		// return the name of the latest version of the XML file.
		/// <summary>
		/// return the name of the XML file storying the information in the repository
		/// </summary>
		/// <returns>file spec to the most recent XML file</returns>
		public static string GetRepositoryFileName()
		{
			// try the current user key first
			string strRepositoryFile = null;
			RegistryKey aStoreKey = Registry.CurrentUser.OpenSubKey(EncConverters.HKLM_PATH_TO_XML_FILE);

			if (aStoreKey == null)
				aStoreKey = Registry.LocalMachine.OpenSubKey(EncConverters.HKLM_PATH_TO_XML_FILE);

			if (aStoreKey != null)
				strRepositoryFile = (string)aStoreKey.GetValue(strRegKeyForStorePath);

			if( String.IsNullOrEmpty(strRepositoryFile) )
			{
				// by default, put it in the C:\Program Files\Common Files\Enc... folder
				strRepositoryFile = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
				strRepositoryFile += strDefXmlPath;
				VerifyDirectoryExists(strRepositoryFile);
				strRepositoryFile += strDefXmlFilename;
				WriteStorePath(strRepositoryFile);
			}
			else
			{
				string strPath = strRepositoryFile.Substring(0,strRepositoryFile.LastIndexOf('\\'));
				VerifyDirectoryExists(strPath);
			}

			return strRepositoryFile;
		}

		protected static void VerifyDirectoryExists(string strPath)
		{
			try
			{
				if (!Directory.Exists(strPath))
					Directory.CreateDirectory(strPath);
			}
			catch (UnauthorizedAccessException ex)
			{
				throw new ApplicationException(String.Format("You don't have sufficient privilege to create the folder where the repository files are to be stored.{0}Re-install SILConverters in order to get the proper permissions or manually create the following folder:{0}{0}{1}",
					Environment.NewLine, strPath), ex);
			}
		}

		// Load and return the DataSet class that corresponds with our XSD file
		/// <summary>
		/// Load and return the DataSet class that corresponds with our XSD file. For special clients
		/// that want to access the information in the repository not otherwise available from this interface
		/// </summary>
		/// <returns></returns>
		public static mappingRegistry GetRepositoryFile()
		{
			string strRepositoryFile = GetRepositoryFileName();

			// create the 'data set' that goes with our schema and read the file
			mappingRegistry file = new mappingRegistry();

			try
			{
				file.ReadXml(strRepositoryFile);
			}
			catch
			{
				// if we didn't find it, then create one from scratch (add the main nodes only)
				file.mappings.AddmappingsRow();
				file.encodings.AddencodingsRow();
				file.fonts.AddfontsRow();
				file.implementations.AddimplementationsRow();

				// save these new values
				file.WriteXml(strRepositoryFile);
			}

			return file;
		}

		// save the XML file changes
		/// <summary>
		/// save the DataSet to the XML file storage
		/// </summary>
		/// <param name="file">DataSet of the repository information</param>
		public static void WriteRepositoryFile(mappingRegistry file)
		{
			string strRepositoryFile = GetRepositoryFileName();
#if !DontAddVersionNumbersToXMLFilename
			const int cnVersionDigits = 1;
			const string cstrExtension = ".xml";
			const int cnXmlExtension = 4;
			// the following conditionally compiled code will add an X digit version number to
			//  the repository file so we also create a new XML file with each change (e.g.
			//  mappingRegistry.xml will become SILRepository0000.xml and then
			//  SILRepository0001.xml, etc.)
			// if people decide they don't like this feature, just define the define and it'll
			//  stop behaving that way. (though it's definitely useful for Debug mode anyway)
			string strVersionNum = strRepositoryFile.Substring(strRepositoryFile.Length-(cnXmlExtension+cnVersionDigits),cnVersionDigits);
			string strNewFileName = strRepositoryFile.Substring(0,strRepositoryFile.Length-cnXmlExtension);

			Int32 nVersion = 0;
			try
			{
				nVersion = Convert.ToInt32(strVersionNum) + 1;
				strNewFileName = strNewFileName.Substring(0,strNewFileName.Length-cnVersionDigits);
			}
			catch(System.FormatException) {}

			string strVersion = nVersion.ToString();
			strVersion = strVersion.Substring(strVersion.Length - cnVersionDigits,cnVersionDigits);
			strRepositoryFile = strNewFileName + strVersion + cstrExtension;
#endif
			// make a backup copy if we can
			try
			{
				string strBackupFilename = strRepositoryFile + ".bak";
				File.Copy(strRepositoryFile, strBackupFilename, true);
			}
			catch { }

			// finally save the XML file
			try
			{
				file.WriteXml(strRepositoryFile);
			}
			catch (UnauthorizedAccessException ex)
			{
				throw new ApplicationException(String.Format("You don't have sufficient privilege to save the changes in the repository file:{0}{0}{1}{0}{0}Re-install SILConverters in order to get the proper permissions or manually add the Modify permission to that file.",
					Environment.NewLine, strRepositoryFile), ex);
			}

			// too quickly calling this again (e.g. during install when we go thru all the
			//  installation vbscripts in the MapsTables folder) can cause exceptions, so
			//  make sure it is all freed here.
			file.Dispose();
		}
		#endregion XML file Get/Set

		#region Data Set Helpers
		internal mappingRegistry AddConversionMapEx(mappingRegistry file, string mappingName,
			ConvType conversionType, string converterSpec, string implementType,
			string leftEncoding, string rightEncoding, ProcessTypeFlags processType,
			bool bClearOldEncodingInfo, out mappingRegistry.mappingRow aMapRow,
			out mappingRegistry.specRow aSpecRow)
		{
			// some items are common for all cases below
			bool bBidi = !IsUnidirectional(conversionType);
			string direction = ((bBidi) ? strDefDirection : strForward_only_Direction);
			bool bLhsUnicode = (EncConverter.NormalizeLhsConversionType(conversionType) == NormConversionType.eUnicode);
			bool bRhsUnicode = (EncConverter.NormalizeRhsConversionType(conversionType) == NormConversionType.eUnicode);
			string leftType = (bLhsUnicode ? strDefTypeUnicode : strDefTypeBytes);
			string rightType = (bRhsUnicode ? strDefTypeUnicode : strDefTypeBytes);

			// load the XML file and see if this mappingName already exists
			mappingRegistry.mappingDataTable aMapDT = file.mapping;
			aMapRow = aMapDT.FindByname(mappingName);

			// it's either there already (in which case, we want to overwrite it or remove it
			//  first) or it isn't (in which case we want to add it).
			aSpecRow = null;
			if( aMapRow != null )
			{
				// see if the spec for this already exists (if the map exists, then the specs
				//  must also)
				InsureSpecsRow(file,aMapRow);
				foreach(mappingRegistry.specRow aSpecRow2 in aMapRow.GetspecsRows()[0].GetspecRows())
				{
					if( aSpecRow2.type == implementType )
					{
						aSpecRow = aSpecRow2;

						// this is the same as an existing one, so just update the rest of
						//  the information.
						aMapRow.name = mappingName;
						aMapRow.leftType = leftType;
						aMapRow.rightType = rightType;
						aSpecRow.path = converterSpec;
						aSpecRow.direction = direction;
						aSpecRow.type = implementType;

						if(     (processType != ProcessTypeFlags.DontKnow)
							||  (GetProcessType(aSpecRow) != (int)ProcessTypeFlags.DontKnow)
						)
						{
							SetProcessType(file,aSpecRow,processType);
						}
						break;
					}
				}
			}
			else
			{
				InsureMappingsRow(file);
				aMapRow = aMapDT.AddmappingRow(mappingName,leftType,
					rightType,file.mappings[0]);

				// always add the specs row here (so below we'll know it's there.
				file.specs.AddspecsRow(aMapRow);
			}

			if( aSpecRow == null )   // if it's (still) null
			{
				Debug.Assert(aMapRow != null);
				InsureSpecsRow(file,aMapRow);
				aSpecRow = file.spec.AddspecRow(implementType,converterSpec,
					direction,aMapRow.GetspecsRows()[0]);

				if( processType != ProcessTypeFlags.DontKnow )
				{
					SetProcessType(file,aSpecRow,processType);
				}
			}

			// Some callers want us to actually remove the original encoding mappings (e.g.
			//  if doing a compound converter between each step)
			if( bClearOldEncodingInfo )
				RemoveEncodingInfo(file, false, aMapRow, aMapDT);

			// otherwise, at least update the encoding id information.
			AdjustEncodingIDs(file, processType, aMapRow, bBidi, bLhsUnicode, leftEncoding,
				bRhsUnicode, rightEncoding);

			return file;
		}

		internal void AdjustEncodingIDs(mappingRegistry file, ProcessTypeFlags processType,
			mappingRegistry.mappingRow aMapRow, bool bBidi, bool bLhsUnicode,
			string leftEncoding, bool bRhsUnicode, string rightEncoding)
		{
			// now add the encodings entries for the left and right encodings.
			// make sure the outer node (Encodings) is there.
			InsureEncodingsRow(file);

			// if the rhs is unicode and *still* empty (after giving the engines a chance
			//  at changing it), then make it "Unicode (" + lhsEncoding + ")".
			bool bUseDefEncForRhs = false;
			if( bRhsUnicode && String.IsNullOrEmpty(rightEncoding) && !String.IsNullOrEmpty(leftEncoding) )
			{
				// seems like a useful helper (e.g. "UNICODE (SIL-EEG_SINDHI)")
				rightEncoding = BuildConverterSpecName(strDefUnicodeEncoding, leftEncoding);

				// if we're going to build an encoding specifically out of the legacy
				//  encodings name (like this), then go ahead and make the legacy encoding
				//  the 'defineMapping' of the rhs (i.e. Unicode) encoding record.
				bUseDefEncForRhs = true;
			}

			// do something different if this is a unicode encoding conversion (defineMapping)
			if( processType == ProcessTypeFlags.UnicodeEncodingConversion )
			{
				AddDefineEncodingEntry(file,aMapRow,leftEncoding,bLhsUnicode,true,rightEncoding);
			}
			else
			{
				AddEncodingMapEntry(file,aMapRow,leftEncoding,bLhsUnicode,true,rightEncoding);
			}

			// if the map is reversable, then we have an outbound conversion from the rhs also.
			if( bBidi )
			{
				if( bUseDefEncForRhs )
					AddDefineEncodingEntry(file,aMapRow,rightEncoding,bRhsUnicode,false,leftEncoding);
				else
					AddEncodingMapEntry(file,aMapRow,rightEncoding,bRhsUnicode,false,leftEncoding);
			}
			else
				// at least put in the encoding entry corresponding to the rhs (if no map)
				AddEncodingEntry(file,rightEncoding,bRhsUnicode);
		}

		internal void AddDefineEncodingEntry(mappingRegistry file,
			mappingRegistry.mappingRow aMapRow, string strEncoding, bool bUnicode,
			bool directionForward, string strBecomesEncoding)
		{
			mappingRegistry.encodingRow aEncodingRow = AddEncodingEntry(file, strEncoding,
				bUnicode);
			if( aEncodingRow == null )
				return;

			// the 'define-mapping' is (single) child of aEncodingRow
			if( aEncodingRow.GetdefineMappingRows().Length == 0 )
				file.defineMapping.AdddefineMappingRow(aMapRow,strBecomesEncoding,!directionForward,
					aEncodingRow);

			// now update the record
			mappingRegistry.defineMappingRow aDefineMappingRow = aEncodingRow.GetdefineMappingRows()[0];
			aDefineMappingRow.name = aMapRow.name;
			aDefineMappingRow.becomes = strBecomesEncoding;
			aDefineMappingRow.reverse = !directionForward;

			// these optional values can be left null/empty in certain cases.
			if( directionForward )
				aDefineMappingRow.SetreverseNull();
			if( String.IsNullOrEmpty(strBecomesEncoding) )
				aDefineMappingRow.SetbecomesNull();
		}

		internal mappingRegistry.encodingRow AddEncodingEntry(mappingRegistry file, string strEncoding, bool bUnicode)
		{
			if( String.IsNullOrEmpty(strEncoding) )
			{
				return null; // if empty, then we're done
			}

			mappingRegistry.encodingDataTable aEncodingDT = file.encoding;
			mappingRegistry.encodingRow aEncodingRow = aEncodingDT.FindByname(strEncoding);
			if( aEncodingRow == null )
			{
				// this encoding ID's sub-node isn't there, so add it.
				InsureEncodingsRow(file);
				aEncodingRow = aEncodingDT.AddencodingRow(strEncoding,bUnicode,file.encodings[0]);
			}

			// these optional values can be left null/empty in certain cases.
			if( !bUnicode )
				aEncodingRow.SetisUnicodeNull();

			return aEncodingRow;
		}

		internal void AddEncodingMapEntry(mappingRegistry file, mappingRegistry.mappingRow aMapRow,
			string strEncoding, bool bUnicode, bool directionForward, string strBecomesEncoding)
		{
			mappingRegistry.encodingRow aEncodingRow = AddEncodingEntry(file, strEncoding,
				bUnicode);
			if( aEncodingRow == null )
				return;

			// make sure the outer sub-node (EncodingMappings) is there
			InsureEncodingMappingsRow(file,aEncodingRow);
			mappingRegistry.encodingMappingsRow aEncodingMappingsRow = aEncodingRow.GetencodingMappingsRows()[0];

			// make sure it isn't already present (if so, then just replace it)
			foreach(mappingRegistry.encodingMappingRow aEncodingMappingRow in aEncodingMappingsRow.GetencodingMappingRows())
			{
				bool bReverse = ((aEncodingMappingRow.IsreverseNull()) ? false : aEncodingMappingRow.reverse);

				// for a match, it must have the same mapping name *AND* the same direction
				if(     (aEncodingMappingRow.name == aMapRow.name)
					&&  (!bReverse == directionForward) )
				{
					// prego... just update the 'becomes' entry
					if( String.IsNullOrEmpty(strBecomesEncoding) )
						aEncodingMappingRow.SetbecomesNull();
					else
						aEncodingMappingRow.becomes = strBecomesEncoding;
					return;
				}
			}

			// falling thru means we have to add it.
			mappingRegistry.encodingMappingRow aRow = file.encodingMapping.AddencodingMappingRow(
				aMapRow, strBecomesEncoding, !directionForward, aEncodingMappingsRow);

			if( directionForward )
				aRow.SetreverseNull();
			if( String.IsNullOrEmpty(strBecomesEncoding) )
				aRow.SetbecomesNull();
		}

		// internally callable
		internal static void AddImplementationEx(mappingRegistry file,
			mappingRegistry.implementationsRow aImplsRow, string platform, string type,
			string use, int priority)
		{
			// see if this platform row exists already
			mappingRegistry.platformRow aPlatRowNew = null;
			foreach(mappingRegistry.platformRow aPlatRow in aImplsRow.GetplatformRows())
			{
				if(aPlatRow.name == platform)
				{
					aPlatRowNew = aPlatRow;
					break;
				}
			}

			// if we didn't already find it, then add a new one
			if( aPlatRowNew == null )
				aPlatRowNew = file.platform.AddplatformRow(platform,aImplsRow);

			// do the same double-checking for the implement rows
			foreach(mappingRegistry.implementRow aImplRow in aPlatRowNew.GetimplementRows())
			{
				if( aImplRow.type == type )
				{
					aImplRow.use = use;
					aImplRow.priority = priority;
					return; // then we're done.
				}
			}

			// this means we didn't find it, so add it.
			file.implement.AddimplementRow(type,use,priority,aPlatRowNew);
		}

		protected void RemoveFromStore(string mappingName, string sImplementType)
		{
			// load XML file and find the given mappingName's entry
			mappingRegistry file = RepositoryFile;
			mappingRegistry.mappingDataTable aMapDT = file.mapping;
			mappingRegistry.mappingRow aMapRow = aMapDT.FindByname(mappingName);
			if( aMapRow == null )
				// apparently, we're done!
				return;

			if( sImplementType != null )
			{
				// this means we're only removing the spec node that goes with this mapping
				Debug.Assert(aMapRow.GetspecsRows().Length == 1);
				mappingRegistry.specRow[] aSpecRows = aMapRow.GetspecsRows()[0].GetspecRows();
				if( aSpecRows.Length == 1 )
				{
					// this is the last one, so the user falsely (or at least needlessly)
					//  used the implementation type in the name. Just ignore it and remove
					//  the whole map.
					sImplementType = null;
				}
				else
				{
					foreach(mappingRegistry.specRow aSpecRow in aSpecRows)
					{
						if( aSpecRow.type == sImplementType )
						{
							file.spec.RemovespecRow(aSpecRow);
							break;
						}
					}
				}

				// if this is the last one, then we're removing the entire map (and the user
				//  falsely
			}

			// the above if case, might have nulled out an unnecessary implement type.
			if( sImplementType == null )
			{
				// otherwise, we're removing the entire map entry.
				//  first get references to the encodingids (which we'll need to refer
				//  to later), before removing the parent (which will cascade delete them).
				RemoveEncodingInfo(file, true, aMapRow, aMapDT);
			}

			WriteRepositoryFile(file);	// save changes.
		}

		internal void RemoveEncodingInfo(mappingRegistry file, bool bRemoveMap,
			mappingRegistry.mappingRow aMapRow, mappingRegistry.mappingDataTable aMapDT)
		{
			// otherwise, we're removing the entire map entry.
			//  first get references to the encodingids (which we'll need to refer
			//  to later), before removing the parent (which will cascade delete them).
			int ndontcare = 0;
			XmlDataDocument xmlDoc;
			XmlNamespaceManager nsmgr;
			GetXmlDataDocument(file, out xmlDoc, out nsmgr);
			string strLeftEncoding, strRightEncoding, mappingName = aMapRow.name;
			GetEncodingFontDetails(xmlDoc, nsmgr, mappingName, out strLeftEncoding, out strRightEncoding, out ndontcare, out ndontcare);

			mappingRegistry.encodingDataTable aEncodingDT = file.encoding;
			mappingRegistry.encodingRow aLEncodingRow = aEncodingDT.FindByname(strLeftEncoding);
			mappingRegistry.encodingRow aREncodingRow = aEncodingDT.FindByname(strRightEncoding);

			// now remove the mapping entry... if requested
			if( bRemoveMap )
				aMapDT.RemovemappingRow(aMapRow);

			// ... and then remove the stranded Encoding rows if they are now empty (they
			//  don't hurt anything, but it looks less cluttered)
			RemoveObsoleteEncodingRows(file,mappingName,aLEncodingRow);
			if( string.Compare( strLeftEncoding, strRightEncoding, true ) != 0 )
				RemoveObsoleteEncodingRows(file,mappingName,aREncodingRow);
		}

		internal void SetProcessType(mappingRegistry file, mappingRegistry.specRow aSpecRow,
			ProcessTypeFlags processType)
		{
			InsureSpecPropertiesRow(file,aSpecRow);
			foreach(mappingRegistry.specPropertyRow aPropRow in aSpecRow.GetspecPropertiesRows()[0].GetspecPropertyRows())
			{
				if( aPropRow.name == strDefProcessTypeName )
				{
					aPropRow.specProperty_Text = processType.ToString();
					return;
				}
			}

			// fall thru means we have to add it afresh
			file.specProperty.AddspecPropertyRow(strDefProcessTypeName,
				processType.ToString(),aSpecRow.GetspecPropertiesRows()[0]);
		}

		internal void RemoveObsoleteEncodingRows(mappingRegistry file, string mappingName,
			mappingRegistry.encodingRow aEncodingRow)
		{
			// might be empty... just return if so.
			if( aEncodingRow == null )
				return;

			// if this is the last one refering to the left or rightEncoding, then remove that
			//  as well.
			mappingRegistry.encodingMappingsRow[] aEncodingMappingsRows = aEncodingRow.GetencodingMappingsRows();
			if( aEncodingMappingsRows.Length > 0 )
			{
				foreach(mappingRegistry.encodingMappingRow aEncodingMappingRow in aEncodingMappingsRows[0].GetencodingMappingRows())
				{
					if( aEncodingMappingRow.name == mappingName )
						file.encodingMapping.RemoveencodingMappingRow(aEncodingMappingRow);
				}

				if( aEncodingMappingsRows[0].GetencodingMappingRows().Length == 0 )
				{
					file.encodingMappings.RemoveencodingMappingsRow(aEncodingMappingsRows[0]);
				}
			}

			// now check about the define mapping (if they're both empty, then get rid of the
			// whole encoding).
			mappingRegistry.defineMappingRow[] aDefineMappingRows = aEncodingRow.GetdefineMappingRows();
			if(		(aDefineMappingRows.Length == 0)
				&&	(aEncodingRow.GetencodingMappingsRows().Length == 0) )
			{
				// there's nothing left so get rid of the entire encoding row
				file.encoding.RemoveencodingRow(aEncodingRow);
			}
		}
		#endregion Data Set Helpers

		#region Insure DataSet Row Helpers
		internal void InsureEncodingMappingsRow(mappingRegistry file,mappingRegistry.encodingRow aEncodingRow)
		{
			if( aEncodingRow.GetencodingMappingsRows().Length == 0 )
				file.encodingMappings.AddencodingMappingsRow(aEncodingRow);
		}

		internal void InsureFontEncodingsRow(mappingRegistry file,mappingRegistry.fontRow aRow)
		{
			if( aRow.GetfontEncodingsRows().Length == 0 )
				file.fontEncodings.AddfontEncodingsRow(aRow);
		}

		internal void InsureFontMappingsRow(mappingRegistry file,mappingRegistry.mappingRow aRow)
		{
			if( aRow.GetfontMappingsRows().Length == 0 )
				file.fontMappings.AddfontMappingsRow(aRow);
		}

		internal void InsureAliasesRow(mappingRegistry file,mappingRegistry.encodingRow aEncodingRow)
		{
			if( aEncodingRow.GetaliasesRows().Length == 0 )
				file.aliases.AddaliasesRow(aEncodingRow);
		}

		internal void InsureSpecPropertiesRow(mappingRegistry file,mappingRegistry.specRow aRow)
		{
			if( aRow.GetspecPropertiesRows().Length == 0 )
				file.specProperties.AddspecPropertiesRow(aRow);
		}

		internal static mappingRegistry.specsRow InsureSpecsRow(mappingRegistry file,mappingRegistry.mappingRow aRow)
		{
			if( aRow.GetspecsRows().Length == 0 )
				file.specs.AddspecsRow(aRow);
			return aRow.GetspecsRows()[0];
		}

		internal void InsureStepsRow(mappingRegistry file,mappingRegistry.specRow aRow)
		{
			if( aRow.GetstepsRows().Length == 0 )
				file.steps.AddstepsRow(aRow);
		}

		internal void InsureMappingsRow(mappingRegistry file)
		{
			mappingRegistry.mappingsDataTable aDT = file.mappings;
			if( aDT.Count == 0 )
				aDT.AddmappingsRow();
		}

		internal void InsureEncodingsRow(mappingRegistry file)
		{
			mappingRegistry.encodingsDataTable aDT = file.encodings;
			if( aDT.Count == 0 )
				aDT.AddencodingsRow();
		}

		internal void InsureFontsRow(mappingRegistry file)
		{
			mappingRegistry.fontsDataTable aDT = file.fonts;
			if( aDT.Count == 0 )
				aDT.AddfontsRow();
		}

		internal void InsureImplementationsRow(mappingRegistry file)
		{
			mappingRegistry.implementationsDataTable aDT = file.implementations;
			if( aDT.Count == 0 )
				aDT.AddimplementationsRow();
		}
		#endregion Insure DataSet Row Helpers

		#region XPath Helpers

		private bool GetXmlAttributeValue(System.Xml.XmlAttributeCollection xac, string stName,
			ref string stValue)
		{
			System.Xml.XmlAttribute xa = xac[stName];
			if (xa != null)
			{
				stValue = xa.Value;
				return true;
			}
			return false;
		}

		internal void GetEncodingFontDetails(XmlDataDocument doc, XmlNamespaceManager nsmgr,
			string mappingName, out string lhsEncodingID, out string rhsEncodingID,
			out int lhsCodePage, out int rhsCodePage)
		{
			// initialize out params
			lhsEncodingID = rhsEncodingID = null;
			lhsCodePage = rhsCodePage = cnDefCodePage;

			// do xpath query to find the encodings which have references to the
			//  given mapping name.
			// first check the define-mappings
			string strXPath = String.Format("//ec:defineMapping[@name=\"{0}\"]", mappingName);
			XmlNode  nodeDM = doc.SelectSingleNode(strXPath, nsmgr);
			if (nodeDM != null )
			{
				if (IsRhsEncoding(nodeDM))
				{
					GetXmlAttributeValue(nodeDM.ParentNode.Attributes, strDefAttrNameEncName,
						ref rhsEncodingID);
					GetXmlAttributeValue(nodeDM.Attributes, strDefAttrNameBecomes,
						ref lhsEncodingID);
				}
				else
				{
					GetXmlAttributeValue(nodeDM.ParentNode.Attributes, strDefAttrNameEncName,
						ref lhsEncodingID);
					GetXmlAttributeValue(nodeDM.Attributes, strDefAttrNameBecomes,
						ref rhsEncodingID);
				}
			}
			else
			{
				// otherwise, check the encodingMapping rows
				strXPath = String.Format("//ec:encodingMapping[@name=\"{0}\"]", mappingName);
				XmlNodeList  nlEncodingMappings = doc.SelectNodes(strXPath, nsmgr);

				foreach (XmlNode xnEncodingMapping in nlEncodingMappings)
				{
					if (IsRhsEncoding(xnEncodingMapping))
					{
						GetXmlAttributeValue(xnEncodingMapping.ParentNode.ParentNode.Attributes,
							strDefAttrNameEncName, ref rhsEncodingID);
						GetXmlAttributeValue(xnEncodingMapping.Attributes, strDefAttrNameBecomes,
							ref lhsEncodingID);
					}
					else    // lhs
					{
						GetXmlAttributeValue(xnEncodingMapping.ParentNode.ParentNode.Attributes,
							strDefAttrNameEncName, ref lhsEncodingID);
						GetXmlAttributeValue(xnEncodingMapping.Attributes, strDefAttrNameBecomes,
							ref rhsEncodingID);
					}
				}
			}

			if (lhsEncodingID != null)
			{
				GetFontDetails(doc, nsmgr, lhsEncodingID, out lhsCodePage);
			}
			if (rhsEncodingID != null)
			{
				GetFontDetails(doc, nsmgr, rhsEncodingID, out rhsCodePage);
			}
		}

		internal int GetProcessType(mappingRegistry.specRow aSpecRow)
		{
			int processType = (int)ProcessTypeFlags.DontKnow;
			if( aSpecRow.GetspecPropertiesRows().Length > 0 )
			{
				foreach(mappingRegistry.specPropertyRow aSpecPropRow in aSpecRow.GetspecPropertiesRows()[0].GetspecPropertyRows())
				{
					if( aSpecPropRow.name == strDefProcessTypeName )
					{
						bool bFound = false;
						foreach(string asName in Enum.GetNames(typeof(ProcessTypeFlags)))
						{
							if( asName == aSpecPropRow.specProperty_Text )
							{
								processType = (int)Enum.Parse(typeof(ProcessTypeFlags),aSpecPropRow.specProperty_Text);
								bFound = true;
								break;
							}
						}

						// if we didn't find it above, then it must be some concatenation of
						//  more than one value, so just parse it as an int.
						if( !bFound )
						{
							try
							{
								processType = Convert.ToInt32(aSpecPropRow.specProperty_Text);
							}
							catch {}
						}

						break;
					}
				}
			}
			return processType;
		}

		internal bool IsRhsEncoding(XmlNode xnEncodingMapping)
		{
			string stRhs = "false";
			GetXmlAttributeValue(xnEncodingMapping.Attributes, strDefAttrNameDirection,
				ref stRhs);
			return stRhs.ToLower() == "true";
		}

		internal void GetFontDetails(XmlDataDocument doc, XmlNamespaceManager nsmgr,
			string encodingID, out int codePage)
		{
			// initialize out params
			codePage = cnDefCodePage;

			// do xpath query to find the font entry corresponding to the given encoding
			string strXPath = String.Format("//ec:fontEncoding[@name=\"{0}\"]", encodingID);
			XmlNode  nFont = doc.SelectSingleNode(strXPath, nsmgr);
			if( nFont != null )
			{
				string stName = "";
				GetXmlAttributeValue(nFont.ParentNode.ParentNode.Attributes,
					strDefAttrNameCodePage, ref stName);
				try
				{
					codePage = Convert.ToInt32(stName);
				}
				catch
				{
				}
			}
		}

		internal void GetXmlDataDocument(mappingRegistry file, out XmlDataDocument xmlDoc,
			out XmlNamespaceManager nsmgr)
		{
			xmlDoc = new XmlDataDocument(file);
			nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
			nsmgr.AddNamespace("ec",strDefXmlNamespace);
		}
		#endregion XPath Helpers

		#region StringBytes Helpers
		/// <summary>
		/// Turn a .Net 'byte []' into a string for use with the LegacyBytes and UTF8Bytes encoding forms
		/// </summary>
		/// <param name="abytes"></param>
		/// <returns></returns>
		[Description("Turn a .Net 'byte []' into a string for use with the LegacyBytes and UTF8Bytes encoding forms"),Category("Data")]
		public static string ByteArrToBytesString(byte [] abytes)
		{
			return ECNormalizeData.ByteArrToString(abytes);
		}

		/// <summary>
		/// Turn a .Net 'string' used with the LegacyBytes or UTF8Bytes encoding form into a 'byte []'
		/// </summary>
		/// <param name="strBytesString"></param>
		/// <returns></returns>
		[Description("Turn a .Net 'string' used with the LegacyBytes or UTF8Bytes encoding form into a 'byte []'"),Category("Data")]
		public static byte [] BytesStringToByteArr(string strBytesString)
		{
			return ECNormalizeData.StringToByteArr(strBytesString);
		}
		#endregion StringBytes Helpers

		#region Error Handling
		// error handling
		public static void ThrowError(long status)
		{
			ThrowError((ErrStatus)status, null);
		}

		public static void ThrowError(ErrStatus status)
		{
			ThrowError(status,null);
		}

		public static void ThrowError(ErrStatus status, string strExtra)
		{
			bool bFormatExtra = false;
			string strRes = null;
			switch( status )
			{
				case ErrStatus.InEncFormNotSupported:
					strRes = "IDS_InEncodingFormNotSupported";
					break;
				case ErrStatus.OutEncFormNotSupported:
					strRes = "IDS_OutEncodingFormNotSupported";
					break;
				case ErrStatus.EncodingConvTypeNotSpecified:
					strRes = "IDS_EncodingConvTypeNotSpecified";
					break;
				case ErrStatus.InvalidConversionType:
					strRes = "IDS_InvalidConversionType";
					break;
				case ErrStatus.OutOfMemory:
					strRes = "IDS_kStatus_OutOfMemory";
					break;
				case ErrStatus.NoReturnData:
					strRes = "IDS_NoReturnData";
					break;
				case ErrStatus.NoReturnDataBadOutForm:
					strRes = "IDS_NoReturnDataBadOutForm";
					break;
				case ErrStatus.IncompleteChar:
					strRes = "IDS_IncompleteChar";
					break;
				case ErrStatus.NoAvailableConverters:
					strRes = "IDS_NoAvailableConverters";
					break;
				case ErrStatus.InvalidForm:
					strRes = "IDS_kStatus_InvalidForm";
					break;
				case ErrStatus.InvalidMapping:
					strRes = "IDS_kStatus_InvalidMapping";
					break;
				case ErrStatus.CompilationFailed:
					strRes = "IDS_kStatus_CompilationFailed";
					break;
				case ErrStatus.NameNotFound:
					strRes = "IDS_kStatus_NameNotFound";
					bFormatExtra = true;
					break;
				case ErrStatus.Exception:
					strRes = "IDS_kStatus_Exception";
					break;
				case ErrStatus.BadMappingVersion:
					strRes = "IDS_kStatus_BadMappingVersion";
					break;
				case ErrStatus.InvalidConverter:
					strRes = "IDS_kStatus_InvalidConverter";
					break;
				case ErrStatus.ConverterBusy:
					strRes = "IDS_kStatus_ConverterBusy";
					break;
				case ErrStatus.NotEnoughBuffer:
					strRes = "IDS_NotEnoughBuffer";
					break;
				case ErrStatus.SyntaxErrorInTable:
					strRes = "IDS_SyntaxErrorInTable";
					break;
				case ErrStatus.CantOpenReadMap:
					strRes = "IDS_CantOpenReadMap";
					bFormatExtra = true;
					break;
				case ErrStatus.NoConverter:
					strRes = "IDS_NoConverter";
					bFormatExtra = true;
					break;
				case ErrStatus.RegistryCorrupt:
					strRes = "IDS_RegistryCorrupt";
					break;
				case ErrStatus.MissingConverter:
					strRes = "IDS_MissingConverter";
					break;
				case ErrStatus.ConverterPluginUninstall:
					strRes = "IDS_ConverterPluginUninstall";
					break;
				case ErrStatus.InvalidCharFound:
					strRes = "IDS_InvalidCharFound";
					break;
				case ErrStatus.TruncatedCharFound:
					strRes = "IDS_TruncatedCharFound";
					break;
				case ErrStatus.IllegalCharFound:
					strRes = "IDS_IllegalCharFound";
					break;
				case ErrStatus.InvalidTableFormat:
					strRes = "IDS_InvalidTableFormat";
					break;
				case ErrStatus.AddFontFirst:
					strRes = "IDS_AddFontFirst";
					bFormatExtra = true;
					break;
				case ErrStatus.InstallFont:
					strRes = "IDS_InstallFont";
					bFormatExtra = true;
					break;
				case ErrStatus.InvalidNormalizeForm:
					strRes = "IDS_InvalidNormalizeForm";
					break;
				case ErrStatus.NoAliasName:
					strRes = "IDS_NoAliasName";
					break;
				case ErrStatus.ConverterAlreadyExists:
					strRes = "IDS_ConverterAlreadyExists";
					break;
				case ErrStatus.NoImplementDetails:
					strRes = "IDS_NoImplementDetails";
					break;
				case ErrStatus.NoEncodingName:
					strRes = "IDS_NoEncodingName";
					bFormatExtra = true;
					break;
				case ErrStatus.NeedSpecTypeInfo:
					strRes = "IDS_NeedSpecTypeInfo";
					bFormatExtra = true;
					break;
				case ErrStatus.InvalidAliasName:
					strRes = "IDS_InvalidAliasName";
					break;
				case ErrStatus.FallbackTwoStepsRequired:
					strRes = "IDS_FallbackTwoStepsRequired";
					break;
				case ErrStatus.FallbackSimilarConvType:
					strRes = "IDS_FallbackSimilarConvType";
					break;
				case ErrStatus.InvalidMappingName:
					strRes = "IDS_InvalidMappingName";
					break;

				case ErrStatus.NoError:
				default:
					strRes = "IDS_NoErrorCode";
					break;
			};

			if( strRes != null )
			{
				// Declare a Resource Manager instance
				ResourceManager LocRM = new ResourceManager("SilEncConverters31.EncCnvtrs",typeof(EncConverters).Assembly);
				string strMsg = LocRM.GetString(strRes);

				if( bFormatExtra )
					strMsg = String.Format(strMsg,strExtra);

				ECException x = new ECException(strMsg,status);
				throw x;
			}
		}
		#endregion Error Handling

		#region Registration Methods
		/* now obsolete
		/// <summary>
		/// Call this method to register all the .Net/COM classes in this assembly. This does not work
		/// during installation because you can call methods on assemblies installed in the GAC *during installation*
		/// </summary>
		public static void RegisterSelf()
		{
			RegistrationServices aRS = new RegistrationServices();
			Assembly thisAssembly = Assembly.GetExecutingAssembly();
			aRS.RegisterAssembly(thisAssembly, AssemblyRegistrationFlags.None);
		}

		/// <summary>
		/// Call this method to unregister all the .Net/COM classes in this assembly.
		/// </summary>
		public static void UnregisterSelf()
		{
			RegistrationServices aRS = new RegistrationServices();
			Assembly thisAssembly = Assembly.GetExecutingAssembly();
			if( thisAssembly != null )
				aRS.UnregisterAssembly(thisAssembly);
		}
		*/
		#endregion Registration Methods
	}

	#region Exception Class
	public class ECException : COMException
	{
		public ECException(string strMessage,ErrStatus status)
			: base(strMessage,(int)status)
		{
		}
	}
	#endregion Exception Class
}
