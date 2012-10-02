// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: XmlLanguageDefinition.cs
// Responsibility:
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using System.Data.SqlClient;
using System.Diagnostics;

namespace SIL.FieldWorks.Common.FwUtils
{
	#region ILanguageDefinition interface
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Language definition
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[ComVisible(true)]
	[Guid("1E5CE044-45A3-4669-B9AD-174276AF5046")]
	public interface ILanguageDefinition
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the writing system
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IWritingSystem WritingSystem
		{
			get;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the ICU base locale
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string BaseLocale
		{
			get;
			set;
		}

		/*
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the new ICU locale
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string NewLocale
		{
			get;
			set;
		}
		*/

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the Ethnologue code
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string EthnoCode
		{
			get;
			set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the ICU locale name
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string LocaleName
		{
			get;
			set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the ICU locale script
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string LocaleScript
		{
			get;
			set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the ICU locale country
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string LocaleCountry
		{
			get;
			set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the ICU locale variant
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string LocaleVariant
		{
			get;
			set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the "Display Name" of the writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string DisplayName
		{
			get;
		}

		/*
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the ICU locale LCID
		/// </summary>
		/// ------------------------------------------------------------------------------------
		int LocaleWinLCID
		{
			get;
			set;
		}
		*/

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets collation elements
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string CollationElements
		{
			get;
			set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets locale resources
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string LocaleResources
		{
			get;
			set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of PUA definitions
		/// </summary>
		/// ------------------------------------------------------------------------------------
		int PuaDefinitionCount
		{
			get;
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a PUA definition
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void GetPuaDefinition(int i, out int code, out string data);
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates a PUA definition
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void UpdatePuaDefinition(int i, int code, string data);
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a PUA definition
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void AddPuaDefinition(int code, string data);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of fonts
		/// </summary>
		/// ------------------------------------------------------------------------------------
		int FontCount
		{
			get;
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a Font
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string GetFont(int i);
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates a font
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void UpdateFont(int i, string filename);
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a font
		/// </summary>
		/// <param name="filename"></param>
		/// ------------------------------------------------------------------------------------
		void AddFont(string filename);
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes a font
		/// </summary>
		/// <param name="i"></param>
		/// ------------------------------------------------------------------------------------
		void RemoveFont(int i);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the keyman keyboard
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string Keyboard
		{
			get;
			set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the encoding converter
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void GetEncodingConverter(out string install, out string file);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the encoding converter
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void SetEncodingConverter(string install, string file);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of collations
		/// </summary>
		/// ------------------------------------------------------------------------------------
		int CollationCount
		{
			get;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a collation
		/// </summary>
		/// ------------------------------------------------------------------------------------
		ICollation GetCollation(int i);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Serializes this LanguageDefinition to an XML file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void Serialize();

		/// <summary>
		/// Save in writing system factory and to database, if any.
		/// </summary>
		/// <param name="oldIcuLocale"></param>
		/// <returns><c>true</c> if writing system changed, otherwise <c>false</c>.</returns>
		bool SaveWritingSystem(string oldIcuLocale);

	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>Enumeration of valid character types</summary>
	/// ----------------------------------------------------------------------------------------
	[Flags]
	public enum ValidCharacterType
	{
		/// <summary>None</summary>
		None = 0,
		/// <summary>Word-forming</summary>
		WordForming = 1,
		/// <summary>Numeric</summary>
		Numeric = 2,
		/// <summary>Punctuation, Symbol, Control, or Whitespace</summary>
		Other = 4,
		/// <summary>Flag to indicate all types of characters (not used for an individual character)</summary>
		All = WordForming | Numeric | Other,
		/// <summary>A character which is defined but whose type has not been determined</summary>
		DefinedUnknown = 8,
	}

	#region LanguageDefinition class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class stores all the information about a writing system from the data base and
	/// from ICU.
	/// JT addition: it also stores some derived information that is useful to dialogs working
	/// with the language definition, and manages some events to do with notifying changes
	/// to the data.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[ComVisible(true)]
	[XmlRootAttribute(ElementName="LanguageDefinition", IsNullable=false)]
	public class LanguageDefinition : ILanguageDefinition, IIcuCleanupCallback, ICloneable
	{
		private XmlWritingSystem m_WritingSystem;
		private string m_baseLocale; // Abbreviation for the 'closest writing system'.
		//		private string m_NewLocale;

		private string m_EthnoCode;
		// These three are the long (display) names. We don't have separate member variables
		// for the abbreviations; the get/set methods for them retrieve and update the parts of
		// the IcuLocale in the writing system object.
		private string m_LocaleName;
		private string m_LocaleScript;
		private string m_LocaleCountry;
		private string m_LocaleVariant;
		//		private int m_LocaleWinLCID;
		private string m_CollationElements;
		private string m_LocaleResources;
		private readonly List<CharDef> m_PuaDefs = new List<CharDef>();
		private FileName[] m_Fonts;
		private FileName m_keyboard;
		private EncodingConverter m_EncodingConverter;

		// Stuff to support UI. Therefore not serialized.
		[XmlIgnoreAttribute]
		private int m_wsUi; // Writing system for UI.
		[XmlIgnoreAttribute]
		private string m_localeUi; // Name of locale corresponding to m_wsUser
		// This is a resource bundle for the root of the FW resources.
		// It is initialized on demand.
		[XmlIgnoreAttribute]
		private ILgIcuResourceBundle m_rbRoot;
		// This is a resource bundle for this language.
		// It is initialized on demand.
		[XmlIgnoreAttribute]
		private ILgIcuResourceBundle m_rbLangDef;

		/// <summary>
		/// Event raised when any language, country, variant or script (name or abbreviation) is changed.
		/// The client may wish to update a field displaying the combination.
		/// </summary>
		public event EventHandler FullCodeChanged;

		bool m_fInitializing = false;
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="LanguageDefinition"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public LanguageDefinition()
		{
			m_fInitializing = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates and initializes a new instance of the LanguageDefinition class.
		/// </summary>
		/// <param name="ws">The writing system </param>
		/// ------------------------------------------------------------------------------------
		public LanguageDefinition(IWritingSystem ws)
			: this()
		{
			m_WritingSystem = new XmlWritingSystem(ws);
			// If this is not a completely new Ws, we can fill the
			// rest of the data structure by its icu defaults.
			if (m_WritingSystem.ICULocale.str != null)
				LoadDefaultICUValues();
			FinishedInitializing();
		}

		private string LoadLocaleName()
		{
			// NOTE: LocaleName will automatically strip variant/region info from name.
			LocaleName = WritingSystem.get_Name(this.WsUi);
			return LocaleName;
		}

		/// <summary>
		/// Compares two locale names.
		/// </summary>
		public static bool SameLocale(string locale1, string locale2)
		{
			// this is probably sufficent, may have to enhance to handle case where
			// hyphen and underscore are used differently in the 2 names.
			return locale1.ToLowerInvariant() == locale2.ToLowerInvariant();
		}

		/// <summary>
		/// This should be called when we need m_localeUi or m_wsUi.
		/// </summary>
		protected void GetUiLocale()
		{
			// Per LT-8574, only support English ("en") versions of icu names for now.
			ILgWritingSystemFactory wsf =
				m_WritingSystem.WritingSystem.WritingSystemFactory;
			IWritingSystem wsUi = wsf.get_Engine("en");
			m_wsUi = wsUi.WritingSystem;
			m_localeUi = "en";
			// Get the name of the ICU locale in which to present UI names.
			//if (m_WritingSystem.WritingSystem == null)
			//{
			//    m_localeUi = "en"; // desperate fallback.
			//    // leave m_wsUi 0; nothing meaningful we can do.
			//}
			//ILgWritingSystemFactory wsf =
			//    m_WritingSystem.WritingSystem.WritingSystemFactory;
			//m_wsUi = wsf.UserWs;
			//IWritingSystem wsUi = wsf.get_EngineOrNull(m_wsUi);
			//if (wsUi == null)
			//    m_localeUi = "en";
			//else
			//    m_localeUi = wsUi.IcuLocale;
		}

		/// <summary>
		/// Return the first cch characters of the input (or as many as are available).
		/// </summary>
		/// <param name="input"></param>
		/// <param name="cch"></param>
		/// <returns></returns>
		protected string LeftSubstring(string input, int cch)
		{
			return input.Substring(0, Math.Min(input.Length, cch));
		}

		/// <summary>
		/// Set the ethnologue code to the specified value, or, if it is null or empty,
		/// to the first three letters of the language name preceded by 'x'.
		/// Also determine the LocaleAbbr.
		/// Return false if the ethnologue code is missing, true if there is one.
		/// </summary>
		/// <param name="ethnologueCode"></param>
		/// <param name="languageName"></param>
		public bool SetEthnologueCode(string ethnologueCode, string languageName)
		{
			bool result = true;
			if (ethnologueCode == null || ethnologueCode == "")
			{
				// Note, this needs to come up with an ASCII name. It will be in the
				// language abbreviation in root.txt as well as a file name for the
				// language definition xml file. We take the first three letters of the
				// language name and convert them to x if they are not alpha-numeric.
				int ich = 0;
				StringBuilder sb = new StringBuilder();
				sb.Append("x");
				while(sb.Length < 4)
				{
					if (languageName.Length > ich)
					{
						char ch = char.ToLower(languageName[ich++]);
						if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9'))
							sb.Append(ch);
					}
					else
						sb.Append("x");
				}
				LocaleAbbr = sb.ToString();
				EthnoCode = "";
				result = false;
			}
			else
			{
				EthnoCode = ethnologueCode;
				SqlConnection dbConnection = null;
				SqlCommand sqlCommand = null;
				string sConnection = string.Format("Server={0}; Database=Ethnologue; User ID=FWDeveloper; " +
					"Password=careful; Pooling=false;", MiscUtils.LocalServerName);

				dbConnection = new SqlConnection(sConnection);
				dbConnection.Open();
				sqlCommand = dbConnection.CreateCommand();
				sqlCommand.CommandText = string.Format("declare @icuCode nchar(4);"
					+ " exec GetIcuCode '{0}', @icuCode output;"
					+ " select @icuCode", ethnologueCode);
				SqlDataReader reader =
					sqlCommand.ExecuteReader(System.Data.CommandBehavior.Default);
				if (reader.HasRows)
				{
					reader.Read();
					LocaleAbbr = reader.GetString(0).Trim().ToLowerInvariant();
				}
				else
				{
					// Should never happen! Pass only valid ethnologue codes.
					Debug.Assert(reader.HasRows);
					// Some sort of recovery.
					LocaleAbbr = "x" + LeftSubstring(languageName, 3).ToLowerInvariant();
					EthnoCode = "";
					result = false;
				}
				reader.Close();
				dbConnection.Close();
			}
			return result;
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fill the LanguageDefinition empty fields with default ICU Values based upon WritingSystem.IcuLocale
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool LoadDefaultICUValues()
		{
			string result;
			Icu.UErrorCode err;
			string icuLocale = WritingSystem.IcuLocale;

			if (LocaleName == null ||
				LocaleName.Length <= 0)
			{
				//string nextTry = WritingSystem.get_Name(
				//    WritingSystem.WritingSystemFactory.UserWs);
				// Try English first.  See LT-8574.
				int wsEn = WritingSystem.WritingSystemFactory.GetWsFromStr("en");
				if (wsEn == 0)
					wsEn = WritingSystem.WritingSystemFactory.UserWs;
				string nextTry = WritingSystem.get_Name(wsEn);
				if (nextTry == null || nextTry.Length == 0)
				{
					// Note this will lock icudt40l\root.res until the next icu.Cleanup.
					int len = Icu.GetDisplayLanguage(icuLocale, "en", out result, out err);
					LocaleName = result;
				}
				else
				{
					LocaleName = nextTry;
				}
			}

			if (LocaleScript == null ||
				LocaleScript.Length <= 0)
			{
				int len = Icu.GetDisplayScript(icuLocale, "en", out result, out err);
				LocaleScript = result;
			}

			if (LocaleCountry == null ||
				LocaleCountry.Length <= 0)
			{
				int len = Icu.GetDisplayCountry(icuLocale, "en", out result, out err);
				LocaleCountry = result;
			}

			if (LocaleVariant == null ||
				LocaleVariant.Length <= 0)
			{
				int len = Icu.GetDisplayVariant(icuLocale, "en", out result, out err);
				LocaleVariant = result;
			}

			if (WritingSystem.Locale <= 0)
			{
				int icuLCID = Icu.GetLCID(icuLocale);
				if (icuLCID <= 0)
					icuLCID = Encoding.Default.CodePage;	// use the ACP (active code page)

				WritingSystem.Locale = icuLCID;
			}

			string defMono = WritingSystem.DefaultMonospace;
			if (defMono == null || defMono.Length == 0)
				WritingSystem.DefaultMonospace = "Courier New";

			string defSerif = WritingSystem.DefaultSerif;
			if (defSerif == null || defSerif.Length == 0)
				WritingSystem.DefaultSerif = "Times New Roman";

			string defSansSerif = WritingSystem.DefaultSansSerif;
			if (defSansSerif == null || defSansSerif.Length == 0)
				WritingSystem.DefaultSansSerif = "Arial";

			string defBodyFont = WritingSystem.DefaultBodyFont;
			if (defBodyFont == null || defBodyFont.Length == 0)
				WritingSystem.DefaultBodyFont = "Charis SIL";

			return true;
		}

		#region Properties

		/// <summary>
		/// The writing system used for user interface. (NOTE: Currently hard-set to English. LT-8628.)
		/// </summary>
		[XmlIgnoreAttribute]
		public int WsUi
		{
			get
			{
				if (m_wsUi == 0)
					GetUiLocale();
				return m_wsUi;
			}
		}

		/// <summary>
		/// Get a resource bundle for the root of all FW resources.
		/// </summary>
		[XmlIgnoreAttribute]
		public ILgIcuResourceBundle RootRb
		{
			get
			{
				if (m_rbRoot == null)
				{
					Icu.InitIcuDataDir();
					m_rbRoot = LgIcuResourceBundleClass.Create();
					m_rbRoot.Init(null, LocaleUi);

					IIcuCleanupManager icuMgr = IcuCleanupManagerClass.Create();
					icuMgr.RegisterCleanupCallback(this);
				}
				return m_rbRoot;
			}
		}

		/// <summary>
		/// Get a resource bundle for this Language Definition.
		/// Returns null if it couldn't load the resource bundle for the LocaleAbbr.
		/// </summary>
		[XmlIgnoreAttribute]
		public ILgIcuResourceBundle LangDefRb
		{
			get
			{
				if (m_rbLangDef == null)
				{
					Icu.InitIcuDataDir();
					m_rbLangDef = LgIcuResourceBundleClass.Create();
					m_rbLangDef.Init(null, LocaleAbbr);

					IIcuCleanupManager icuMgr = IcuCleanupManagerClass.Create();
					icuMgr.RegisterCleanupCallback(this);

					// if the name of the resource bundle doesn't match the LocaleAbbr
					// it loaded something else as a default (e.g. "en").
					// in that case we don't want to use the resource bundle so release it.
					if (m_rbLangDef.Name != LocaleAbbr)
						ReleaseLangDefRb();
				}
				return m_rbLangDef;
			}
		}

		/// <summary>
		/// Answer the full locale ID that will result from the current settings in other fields.
		/// </summary>
		/// <returns></returns>
		public string CurrentFullLocale()
		{
			return m_WritingSystem.ICULocale.str;
		}

		/// <summary>
		/// Handle the FullCodeChanged event; by default just calls delegates.
		/// </summary>
		/// <param name="ea"></param>
		protected virtual void OnFullCodeChanged(EventArgs ea)
		{
			if (FullCodeChanged != null)
				FullCodeChanged(this, ea);
		}

		/// <summary>
		/// Trigger the FullCodeChanged event.
		/// </summary>
		protected void RaiseFullCodeChanged()
		{
			OnFullCodeChanged(new EventArgs());
		}

		/// <summary>
		/// Make sure the root resource bundle is released. This is important to do before
		/// trying to update the resource files.
		/// </summary>
		public void ReleaseRootRb()
		{
			// ENHANCE (EberhardB): This should probably also be called from Dispose just to
			// be safe.

			if (m_rbRoot != null)
			{
				// Allow this to be cleared to unlock memory mapping root.res
				Marshal.FinalReleaseComObject(m_rbRoot);
				m_rbRoot = null;

				IIcuCleanupManager icuMgr = IcuCleanupManagerClass.Create();
				icuMgr.UnregisterCleanupCallback(this);
			}
		}

		/// <summary>
		/// Make sure the root resource bundle is released. This is important to do before
		/// trying to update the resource files.
		/// </summary>
		public void ReleaseLangDefRb()
		{
			// ENHANCE (EberhardB): This should probably also be called from Dispose just to
			// be safe.

			if (m_rbLangDef != null)
			{
				// Allow this to be cleared to unlock memory mapping root.res
				Marshal.FinalReleaseComObject(m_rbLangDef);
				m_rbLangDef = null;

				IIcuCleanupManager icuMgr = IcuCleanupManagerClass.Create();
				icuMgr.UnregisterCleanupCallback(this);
			}
		}

		/// <summary>
		/// The Icu locale, (currently always "en" ) that identifies the language etc. names that should
		/// be presented to the user. LT-8574.
		/// </summary>
		[XmlIgnore]
		private string LocaleUi
		{
			get
			{
				if (m_localeUi == null)
					GetUiLocale();
				return m_localeUi;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the writing system stored in the database
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlElement("LgWritingSystem")]
		public XmlWritingSystem XmlWritingSystem
		{
			get { return m_WritingSystem; }
			set { m_WritingSystem = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the writing system
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public IWritingSystem WritingSystem
		{
			get { return m_WritingSystem.WritingSystem; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the ICU base locale
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string BaseLocale
		{
			get { return m_baseLocale; }
			set { m_baseLocale = value; }
		}

		/*
		 * 		/// ------------------------------------------------------------------------------------
				/// <summary>
				/// Gets/sets the new ICU locale
				/// </summary>
				/// ------------------------------------------------------------------------------------
				public string NewLocale
				{
					get { return m_NewLocale; }
					set { m_NewLocale = value;}
				}
		*/

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the Ethnologue code
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string EthnoCode
		{
			get { return m_EthnoCode; }
			set { m_EthnoCode = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the ICU locale name.  This is really the language name.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string LocaleName
		{
			get { return m_LocaleName; }
			set
			{
				// make sure we only store the language name.
				if (value != null && value.IndexOf("(") != -1)
				{
					string tmpLocaleName = value.Split(new char[] {'('})[0];
					m_LocaleName = tmpLocaleName.Trim();
				}
				else
				{
					if (value == null)
					{
						m_LocaleName = "";
					}
					else if (value != WritingSystem.IcuLocale && value != LocaleAbbr)
					{
						// the name should be different from the code.
						m_LocaleName = value;
					}
					else
					{
						//System.Windows.Forms.MessageBox.Show(String.Format("LocaleName({0}) should not match IcuLocale({1})",
						//    value, WritingSystem.IcuLocale), "");
					}
				}
				SaveWsNameChanges();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the ICU locale country
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string LocaleCountry
		{
			get { return m_LocaleCountry; }
			set { m_LocaleCountry = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the ICU locale script
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string LocaleScript
		{
			get { return m_LocaleScript; }
			set { m_LocaleScript = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the ICU locale variant
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string LocaleVariant
		{
			get { return m_LocaleVariant; }
			set { m_LocaleVariant = value; }
		}

		/*
		 * 		/// ------------------------------------------------------------------------------------
				/// <summary>
				/// Gets/sets the ICU locale LCID
				/// </summary>
				/// ------------------------------------------------------------------------------------
				public int LocaleWinLCID
				{
					get { return m_LocaleWinLCID; }
					set { m_LocaleWinLCID = value; }
				}
		*/
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets collation elements
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string CollationElements
		{
			get { return m_CollationElements; }
			set { m_CollationElements = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets valid characters. Will be serialized as part of the writing system,
		/// so don't do it again here.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public string ValidChars
		{
			get { return m_WritingSystem.WritingSystem.ValidChars; }
			set { m_WritingSystem.WritingSystem.ValidChars = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets matched pairs. Will be serialized as part of the writing system,
		/// so don't do it again here.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public string MatchedPairs
		{
			get { return m_WritingSystem.WritingSystem.MatchedPairs; }
			set { m_WritingSystem.WritingSystem.MatchedPairs = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets punctuation patterns. Will be serialized as part of the writing system,
		/// so don't do it again here.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public string PunctuationPatterns
		{
			get { return m_WritingSystem.WritingSystem.PunctuationPatterns; }
			set { m_WritingSystem.WritingSystem.PunctuationPatterns = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets capitalization info. Will be serialized as part of the writing system,
		/// so don't do it again here.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public string CapitalizationInfo
		{
			get { return m_WritingSystem.WritingSystem.CapitalizationInfo; }
			set { m_WritingSystem.WritingSystem.CapitalizationInfo = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets quotation marks. Will be serialized as part of the writing system,
		/// so don't do it again here.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public string QuotationMarks
		{
			get { return m_WritingSystem.WritingSystem.QuotationMarks; }
			set { m_WritingSystem.WritingSystem.QuotationMarks = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets locale resources
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string LocaleResources
		{
			get { return m_LocaleResources; }
			set { m_LocaleResources = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets PUA definitions
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlArray]
		public CharDef[] PuaDefinitions
		{
			get { return m_PuaDefs.ToArray(); }
			set
			{
				m_PuaDefs.Clear();
				if (value != null)
				{
					// Don't use m_PuaDefs.AddRange() here to keep duplicates from getting in
					// (part of TE-8652)
					foreach (CharDef def in value)
					{
						AddPuaDefinition(def);
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets Fonts
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlArray]
		[XmlArrayItem("Font")]
		public FileName[] Fonts
		{
			get { return m_Fonts; }
			set { m_Fonts = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the keyman keyboard
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FileName Keyboard
		{
			get { return m_keyboard; }
			set { m_keyboard = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the encoding converter
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public EncodingConverter EncodingConverter
		{
			get { return m_EncodingConverter; }
			set { m_EncodingConverter = value; }
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a collation
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ICollation GetCollation(int i)
		{
			if (i >= 0 && i < m_WritingSystem.Collations.Count)
				return m_WritingSystem.Collations[i].Collation;

			return null;
		}

		#region ILanguageDefinition Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the writing system
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IWritingSystem ILanguageDefinition.WritingSystem
		{
			get
			{
				return m_WritingSystem.WritingSystem;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of PUA definitions
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int PuaDefinitionCount
		{
			get { return m_PuaDefs.Count; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a PUA (custom character) definition
		/// </summary>
		/// <param name="i">Zero-based index into the list of custom characters</param>
		/// <param name="code">Character code</param>
		/// <param name="data">Character definition</param>
		/// <exception cref="IndexOutOfRangeException">If <paramref name="i"/> is less than 0 or
		/// greater than or equal to <see cref="PuaDefinitionCount"/></exception>
		/// ------------------------------------------------------------------------------------
		public void GetPuaDefinition(int i, out int code, out string data)
		{
			code = Convert.ToInt32(m_PuaDefs[i].code, 16);
			data = m_PuaDefs[i].data;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets a PUA definition
		/// </summary>
		/// <param name="i">Zero-based index into the list of custom characters</param>
		/// <param name="code">Character code</param>
		/// <param name="data">Character definition</param>
		/// <exception cref="IndexOutOfRangeException">If <paramref name="i"/> is less than 0 or
		/// greater than or equal to <see cref="PuaDefinitionCount"/></exception>
		/// ------------------------------------------------------------------------------------
		public void UpdatePuaDefinition(int i, int code, string data)
		{
			m_PuaDefs[i] = new CharDef(code, data);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a PUA definition
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void AddPuaDefinition(int code, string data)
		{
			AddPuaDefinition(new CharDef(code, data));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a PUA definition
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void AddPuaDefinition(CharDef charDef)
		{
			foreach (CharDef def in m_PuaDefs)
			{
				if (def.code == charDef.code)
					return;
			}
			m_PuaDefs.Add(charDef);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes a PUA definition via matching of codepoints.
		/// </summary>
		/// <param name="charDef">The character definition to find and remove.</param>
		/// ------------------------------------------------------------------------------------
		public void RemovePuaDefinition(CharDef charDef)
		{
			m_PuaDefs.RemoveAll(delegate(CharDef x) { return x.code == charDef.code; });
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of fonts
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int FontCount
		{
			get
			{
				if (m_Fonts == null)
					return 0;

				return m_Fonts.Length;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a font
		/// </summary>
		/// <param name="i">Index</param>
		/// ------------------------------------------------------------------------------------
		public string GetFont(int i)
		{
			if (i < 0 || m_Fonts == null || i >= m_Fonts.Length)
				throw new IndexOutOfRangeException();

			return m_Fonts[i].file;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates a font
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void UpdateFont(int i, string filename)
		{
			if (i < 0 || m_Fonts == null || i >= m_Fonts.Length)
				throw new IndexOutOfRangeException();

			m_Fonts[i] = new FileName(filename);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a font
		/// </summary>
		/// <param name="filename"></param>
		/// ------------------------------------------------------------------------------------
		public void AddFont(string filename)
		{
			int newLength = 1;
			if (m_Fonts != null)
				newLength = m_Fonts.Length + 1;

			FileName[] newFonts = new FileName[newLength];
			if (m_Fonts != null)
				m_Fonts.CopyTo(newFonts, 0);
			m_Fonts = newFonts;
			m_Fonts[newLength - 1] = new FileName(filename);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes a font
		/// </summary>
		/// <param name="i"></param>
		/// ------------------------------------------------------------------------------------
		public void RemoveFont(int i)
		{
			if (i < 0 || m_Fonts == null || i >= m_Fonts.Length)
				throw new IndexOutOfRangeException();

			List<FileName> al = new List<FileName>(m_Fonts);
			al.RemoveAt(i);
			m_Fonts = al.ToArray();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the keyman keyboard
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string ILanguageDefinition.Keyboard
		{
			get { return m_keyboard.file; }
			set { m_keyboard = new FileName(value);}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the encoding converter
		/// </summary>
		/// <param name="install"></param>
		/// <param name="file"></param>
		/// ------------------------------------------------------------------------------------
		public void GetEncodingConverter(out string install, out string file)
		{
			install = m_EncodingConverter.install;
			file = m_EncodingConverter.file;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the encoding converter
		/// </summary>
		/// <param name="install"></param>
		/// <param name="file"></param>
		/// ------------------------------------------------------------------------------------
		public void SetEncodingConverter(string install, string file)
		{
			m_EncodingConverter = new EncodingConverter(install, file);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of collations
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int CollationCount
		{
			get { return m_WritingSystem.Collations.Count; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Serializes this LanguageDefinition to the standard XML file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Serialize()
		{
			string dataDirectory = Path.Combine(DirectoryFinder.FWDataDirectory, "languages");
			string filename = Path.Combine(dataDirectory,
				Path.ChangeExtension(m_WritingSystem.ICULocale.str,
				//				Path.ChangeExtension(m_NewLocale,
				"xml"));

			// Write the language definition xml file.
			Serialize(filename);
		}

		string m_icuLocaleTarget = "";
		///<summary>
		/// Indicates the (original) icu locale that the user desires to merge/overwrite with
		/// any new data currently in this language definition, including a new icu locale.
		/// Returns an empty string if there's nothing to merge/overwrite.
		/// (This will also set the IcuLocaleOrig, for non null/empty values.)
		/// </summary>
		[XmlIgnoreAttribute]
		public string IcuLocaleTarget
		{
			get { return m_icuLocaleTarget; }
			set
			{
				m_icuLocaleTarget = value;
				if (!String.IsNullOrEmpty(value))
					m_icuLocaleOrig = value;
			}
		}

		/// <summary>
		/// Indicates whether this definition is marked for merging this into the WritingSystem indicated by IcuLocaleTarget.
		/// </summary>
		public bool HasPendingMerge()
		{
			return !String.IsNullOrEmpty(m_icuLocaleTarget) && IsWritingSystemInDb();
		}

		/// <summary>
		/// Indicates whether this definition is marked for overwriting the data in the WritingSystem indicated by IcuLocaleTarget
		/// which is a writing system that doesn't exist in our database but does have a language definition file
		/// indicating it belongs to another database on the system.
		/// The existing language definition should be deserialized and used to overwrite the IcuLocaleTarget.
		/// </summary>
		public bool HasPendingOverwrite()
		{
			return !String.IsNullOrEmpty(m_icuLocaleTarget) && !IsWritingSystemInDb() && IsLocaleInLanguagesDir();
		}

		private string m_icuLocaleOrig = "";
		private int m_wsOrig = 0;
		/// <summary>
		/// Captures the icuLocale, so we can compare it against later modifications.
		/// </summary>
		/// <returns></returns>
		private void CaptureIcuLocaleAndWs()
		{
			m_icuLocaleOrig = WritingSystem.IcuLocale;
		}

		/// <summary>
		/// if we've finished initializing, this captures the certain initial state information, and makes sure all the display name
		/// information is synced.
		/// NOTE: this will not set m_fInitializing to false until WritingSystem.ICULocale has been set. Under some circumstances
		/// (e.g. Writing System Wizard) we create a new languageDefinition and use the rest of the dialog to initialize the
		/// rest of the fields. In that case, we won't do FinishedInitializing() until Serialize() and SaveWritingSystem()
		/// </summary>
		internal void FinishedInitializing()
		{
			if (m_fInitializing && m_WritingSystem.ICULocale.str != null)
			{
				m_fInitializing = false;
				CaptureIcuLocaleAndWs();
				// since we're finished with initializing, it's okay to sync display name
				SaveWsNameChanges();
			}
		}

		/// <summary>
		/// The id of the ws for IcuLocaleOriginal.
		/// </summary>
		[XmlIgnoreAttribute]
		public int WsOriginal
		{
			get
			{
				if (m_wsOrig == 0)
				{
					ILgWritingSystemFactory wsf = WritingSystem.WritingSystemFactory;
					if (!String.IsNullOrEmpty(m_icuLocaleOrig))
						m_wsOrig = wsf.GetWsFromStr(m_icuLocaleOrig);
					// it's possible that icuLocaleOrig has gotten overwritten, in which
					// case its id has been associated with the new icuLocale.
					if (m_wsOrig == 0)
						m_wsOrig = wsf.GetWsFromStr(WritingSystem.IcuLocale);
				}
				return m_wsOrig;
			}
		}

		/// <summary>
		/// The IcuLocale of the WritingSystem when LanguageDefinition was first loaded
		/// or set by IcuLocaleTarget.
		/// </summary>
		[XmlIgnoreAttribute]
		public string IcuLocaleOriginal
		{
			get { return m_icuLocaleOrig; }
		}

		/// <summary>
		/// Indicates whether the WrtingSystem.IcuLocale has changed since we set IcuLocaleOriginal.
		/// </summary>
		/// <returns></returns>
		[XmlIgnoreAttribute]
		public bool HasChangedIcuLocale
		{
			get
			{
				return m_icuLocaleOrig != String.Empty &&
					m_icuLocaleOrig != WritingSystem.IcuLocale;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// See SaveWritingSystem(string,bool)
		/// Does not force the writing system to save unless necessary.
		/// </summary>
		/// <param name="oldIcuLocale">
		/// Either the new IcuLocale id, or the old one when we are changing the IcuLocale.
		/// </param>
		/// <returns><c>true</c> if writing system changed, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		public bool SaveWritingSystem(string oldIcuLocale)
		{
			return SaveWritingSystem(oldIcuLocale, false);
		}

		/// <summary>
		/// Individual changes to LocaleName, LocaleScript, LocaleVariant, LocaleCountry
		/// need to get written back to the WritingSystem data before
		/// persisting the information.
		/// </summary>
		/// <returns></returns>
		private void SaveWsNameChanges()
		{
			// don't save while we're in the process of initializing
			if (m_fInitializing)
				return;
			string wsName = m_WritingSystem.WritingSystem.get_Name(this.WsUi);
			// save any pending ws name changes.
			if (wsName != this.DisplayName && this.DisplayName != WritingSystem.IcuLocale)
				this.WritingSystem.set_Name(this.WsUi, this.DisplayName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Saves the information to the writing system in the database which starts out with
		/// oldIcuLocale.
		/// </summary>
		/// <param name="oldIcuLocale">
		/// Either the new IcuLocale id, or the old one when we are changing the IcuLocale.
		/// </param>
		/// <param name="forceSave">
		/// Force the writing system to save, even if it doesn't appear to have changed,
		/// i.e. force the dirty flag to be true (needed when creating/modifying PUA characters)
		/// </param>
		/// <returns><c>true</c> if writing system changed, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		public bool SaveWritingSystem(string oldIcuLocale, bool forceSave)
		{
			FinishedInitializing();
			SaveWsNameChanges();
			// Now copy information from the temporary ws to the real ws and save it in
			// the database.
			ILgWritingSystemFactory wsf =
				m_WritingSystem.WritingSystem.WritingSystemFactory;
			int defWs = this.WsUi;
			int ws = wsf.GetWsFromStr(oldIcuLocale);
			// Try to get the original writing system, if there is one.
			IWritingSystem qws = wsf.get_EngineOrNull(ws);
			if (qws == null)
			{
				// Otherwise create a new writing system.
				qws = wsf.get_Engine(m_WritingSystem.WritingSystem.IcuLocale);
			}

			string str;
			str = m_WritingSystem.WritingSystem.DefaultSerif;
			if (str != qws.DefaultSerif)
				qws.DefaultSerif = str;
			str = m_WritingSystem.WritingSystem.DefaultSansSerif;
			if (str != qws.DefaultSansSerif)
				qws.DefaultSansSerif = str;
			str = m_WritingSystem.WritingSystem.DefaultBodyFont;
			if (str != qws.DefaultBodyFont)
				qws.DefaultBodyFont = str;
			str = m_WritingSystem.WritingSystem.KeymanKbdName;
			if (str != qws.KeymanKbdName)
				qws.KeymanKbdName = str;
			str = m_WritingSystem.WritingSystem.IcuLocale;
			if (str != qws.IcuLocale)
				qws.IcuLocale = str;
			str = m_WritingSystem.WritingSystem.SpellCheckDictionary;
			if (str != qws.SpellCheckDictionary)
				qws.SpellCheckDictionary = str;
			str = m_WritingSystem.WritingSystem.LegacyMapping;
			if (str != qws.LegacyMapping)
				qws.LegacyMapping = str;
			str = m_WritingSystem.WritingSystem.FontVariation;
			if (str != qws.FontVariation)
				qws.FontVariation = str;
			str = m_WritingSystem.WritingSystem.SansFontVariation;
			if (str != qws.SansFontVariation)
				qws.SansFontVariation = str;
			str = m_WritingSystem.WritingSystem.BodyFontFeatures;
			if (str != qws.BodyFontFeatures)
				qws.BodyFontFeatures = str;
			str = m_WritingSystem.WritingSystem.get_Abbr(defWs);
			if (str != qws.get_Abbr(defWs))
				qws.set_Abbr(defWs, str);
			str = m_WritingSystem.WritingSystem.get_Name(defWs);
			if (str != qws.get_Name(defWs))
				qws.set_Name(defWs, str);
			str = m_WritingSystem.WritingSystem.ValidChars;
			if (str != qws.ValidChars)
				qws.ValidChars = str;
			str = m_WritingSystem.WritingSystem.MatchedPairs;
			if (str != qws.MatchedPairs)
				qws.MatchedPairs = str;
			str = m_WritingSystem.WritingSystem.PunctuationPatterns;
			if (str != qws.PunctuationPatterns)
				qws.PunctuationPatterns = str;
			str = m_WritingSystem.WritingSystem.CapitalizationInfo;
			if (str != qws.CapitalizationInfo)
				qws.CapitalizationInfo = str;
			str = m_WritingSystem.WritingSystem.QuotationMarks;
			if (str != qws.QuotationMarks)
				qws.QuotationMarks = str;
			if (m_WritingSystem.WritingSystem.CollationCount > 0 &&
				qws.CollationCount > 0)
			{
				str = m_WritingSystem.WritingSystem.get_Collation(0).IcuRules;
				if (str != qws.get_Collation(0).IcuRules)
					qws.get_Collation(0).IcuRules = str;
			}
			// This can't be enabled until we properly deserialize/serialize
			// descriptions. At the moment we don't load the description, so
			// enabling this would wipe out existing descriptions when we save
			// the writing systems below.
			//ITsString qtss = m_WritingSystem.WritingSystem.get_Description(defWs);
			//if (qtss.Equals(qws.get_Description(defWs))
			//	qws.set_Description(defWs, qtss);
			bool fDir = m_WritingSystem.WritingSystem.RightToLeft;
			if (fDir != qws.RightToLeft)
				qws.RightToLeft = fDir;
			int winLoc = m_WritingSystem.WritingSystem.Locale;
			if (winLoc != qws.Locale)
				qws.Locale = winLoc;

			bool fRet = qws.Dirty;

			// Force the writing system to save even if it doesn't appear to have changed.
			if(forceSave)
				qws.Dirty = true;

			// Note, this currently saves all writing systems, and in doing so, it forces dirty
			// writing systems to update their language definition file. This overwrites
			// the WritingSystem portion we just wrote above, which is a waste of time.
			// Before changing this, though, the Description property needs to be fixed above.
			// Otherwise the descriptions will be wiped out. Also, if we don't use the normal
			// writing system save procedure, then we need to call InstallLanguage directly
			// to make sure changes are reflected in ICU.
			if (qws.Dirty == true)
				wsf.SaveWritingSystems();

			// We've saved the information, so no longer have a merge or overwrite pending.
			m_icuLocaleTarget = "";
			return fRet;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the writing system ID of the new locale in the database, if it exists, returning
		/// zero if it doesn't.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int FindWritingSystemInDb()
		{
			ILgWritingSystemFactory wsf =
				m_WritingSystem.WritingSystem.WritingSystemFactory;
			string strLoc = m_WritingSystem.WritingSystem.IcuLocale;
			return wsf.GetWsFromStr(strLoc);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check to see if the new locale is currently used in the database.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsWritingSystemInDb()
		{
			return (FindWritingSystemInDb() != 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check to see if the new ICULocale is already in an xml file in the languages dir.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsLocaleInLanguagesDir()
		{
			string dataDirectory = Path.Combine(DirectoryFinder.FWDataDirectory, "languages");
			string filename = Path.Combine(dataDirectory,
				Path.ChangeExtension(m_WritingSystem.ICULocale.str,"xml"));
			return File.Exists(filename);
		}

		#endregion // ILanguageDefinition

		#region ICloneable Members

		/// <summary>
		/// creates a clone of the current object.
		/// </summary>
		/// <returns></returns>
		object ICloneable.Clone()
		{
			LanguageDefinition clonedLangDef = null;
			using (MemoryStream stream = new MemoryStream())
			{
				using (XmlTextWriter xtw = new XmlTextWriter(stream, Encoding.UTF8))
				{
					Serialize(xtw);
					xtw.Flush();
					stream.Seek(0, SeekOrigin.Begin);
					using (XmlReader sr = XmlReader.Create(stream))
					{
						LanguageDefinitionFactory ldf = new LanguageDefinitionFactory();
						ldf.Deserialize(sr);
						clonedLangDef = ldf.LanguageDefinition as LanguageDefinition;
						clonedLangDef.m_icuLocaleOrig = this.m_icuLocaleOrig;
					}
				}
			}
			return clonedLangDef;
		}

		/// <summary>
		/// compares the serlialized output for the given two objects.
		/// </summary>
		/// <param name="langDefA"></param>
		/// <param name="langDefB"></param>
		/// <returns>true if the given language definitions have the same values/content.</returns>
		public static bool HaveSameValues(LanguageDefinition langDefA, LanguageDefinition langDefB)
		{
			if (langDefA == null || langDefB == null)
				return false;
			// compare the xml dump of each object to test equality of values.
			MemoryStream streamA;
			XmlTextWriter xtwA;
			StreamReader srA;
			SerializeAndGetReader(langDefA, out streamA, out xtwA, out srA);
			MemoryStream streamB;
			XmlTextWriter xtwB;
			StreamReader srB;
			SerializeAndGetReader(langDefB, out streamB, out xtwB, out srB);
			try
			{
				if (streamA.Length != streamB.Length)
					return false;
				// Compare them.
				string a = srA.ReadToEnd();
				string b = srB.ReadToEnd();
				return a == b;
			}
			finally
			{
				srA.Close();
				xtwA.Close();
				streamA.Dispose();
				srB.Close();
				xtwB.Close();
				streamB.Dispose();
			}
		}

		private static void SerializeAndGetReader(LanguageDefinition langDef, out MemoryStream stream, out XmlTextWriter xtw, out StreamReader sr)
		{
			stream = new MemoryStream();
			xtw = new XmlTextWriter(stream, Encoding.UTF8);
			langDef.Serialize(xtw);
			xtw.Flush();
			stream.Seek(0, SeekOrigin.Begin);
			sr = new StreamReader(stream);
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Serializes this LanguageDefinition to an XML file
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Serialize(string filename)
		{
			// Allow the specific file access error exception to propagate from here if it occurs.
			XmlWriter textWriter = null;
			try
			{
				XmlWriterSettings settings = new XmlWriterSettings();
				settings.Encoding = Encoding.UTF8;
				settings.Indent = true;
				textWriter = XmlWriter.Create(filename, settings);
				Serialize(textWriter);
			}
			finally
			{
				if (textWriter != null)
					textWriter.Close();
			}

		}

		/// <summary>
		///
		/// </summary>
		/// <param name="writer"></param>
		protected void Serialize(XmlWriter writer)
		{
			FinishedInitializing();
			SaveWsNameChanges();
			XmlSerializer xmlSerializer = new XmlSerializer(typeof(LanguageDefinition));
			xmlSerializer.Serialize(writer, this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the parts of the ICU locale. Non-existing parts will get an empty string.
		/// The length of the returned array is always 4.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private string[] LocaleParts
		{
			get
			{
				// The ICU locale is potentially made up of four parts
				// 1) Language,
				// 2) Script    (exactly 4 characters in length,
				// 3) Country, (2 or 3 characters)
				// 4) Variant   (can have multiple underscores in it)
				// eg. en
				// eg. en_US for English (United States)
				// eg. en_Latn_US_IPA
				//
				// How can we parse a locale?
				// The underscore preceeding Script is optional
				// The underscore preceeding Country and Variant is not optional.
				// Country is 2 or 3 characters
				// Script is 4 characters
				//
				// examples
				// en    (Language)
				// en__abcd (Language & Variant since there are two undescores)
				// en_abcd  (Language & Script since there is 1 underscore then 4 characters)
				// en_abc   (Language & Country since there is 1 underscore then 3 characters

				string[] parts = new string[4];
				if (m_WritingSystem.ICULocale.str != null)
				{
					Icu.UErrorCode err = Icu.UErrorCode.U_ZERO_ERROR;
					Icu.GetLanguageCode(m_WritingSystem.ICULocale.str, out parts[0], out err);
					Icu.GetScriptCode(m_WritingSystem.ICULocale.str, out parts[1], out err);
					Icu.GetCountryCode(m_WritingSystem.ICULocale.str, out parts[2], out err);
					Icu.GetVariantCode(m_WritingSystem.ICULocale.str, out parts[3], out err);
				}
				return parts;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Replaces one part of the ICU locale string with a new value.
		/// The four parts of ICU locale are 1) Language 2) Script 3) Country 4) Variant.
		/// To follow ICU rules this method will only add Script and Country that meet the
		/// following criteria.
		/// Script must be exactly 4 characters.
		/// Country must be 2 or 3 characters.
		/// </summary>
		/// <param name="iPart">The index of the part that will be replaced</param>
		/// <param name="newPart">The new value of part[i].</param>
		/// <returns>ICU locale string</returns>
		/// ------------------------------------------------------------------------------------
		private string BuildIcuLocale(int iPart, string newPart)
		{
			// We need to validate the language code is not being modified unintentionally by
			// ICU. For example, if we ask for div, ICU automatically changes this to dv. So
			// we want to make this change immediately so that InstallLanguage will work.
			if (iPart == 0)
			{
				string icuLanguage;
				Icu.UErrorCode err;
				StringUtils.InitIcuDataDir();	// initialize ICU data dir
				Icu.GetLanguageCode(newPart, out icuLanguage, out err);
				newPart = icuLanguage;
			}
			string[] parts = LocaleParts;

			if (iPart == 0
				|| (iPart == 1 && (newPart.Length == 4 || newPart.Length == 0)) // Script must be exactly 4 characters.
				|| (iPart == 2 && (newPart.Length == 2 || newPart.Length == 3 || newPart.Length == 0)) // Country must be 2 or 3 characters.
				|| iPart == 3)
			{
				parts[iPart] = newPart;
			}

			StringBuilder strBldr = new StringBuilder();
			//start with the LanguageCode
			strBldr.Append(parts[0]);

			//now add the ScriptCode if it exists
			if (!String.IsNullOrEmpty(parts[1]))
				strBldr.AppendFormat("_{0}", parts[1]);

			//now add the CountryCode if it exists
			if (!String.IsNullOrEmpty(parts[2]))
				strBldr.AppendFormat("_{0}", parts[2]);

			// if variantCode is notNullofEmpty then add it
			// and if CountryCode is empty add two underscores instead of one.
			if (!String.IsNullOrEmpty(parts[3]))
			{
				if (String.IsNullOrEmpty(parts[2]))
					strBldr.AppendFormat("__{0}", parts[3]);
				else
					strBldr.AppendFormat("_{0}", parts[3]);

			}
			return strBldr.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the abbreviation of the locale
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public string LocaleAbbr
		{
			get { return LocaleParts[0]; }
			set
			{
				m_WritingSystem.WritingSystem.IcuLocale = BuildIcuLocale(0, value);
				RaiseFullCodeChanged();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get/set Scriptname abbreviation of ICULocale. 'Set' only accepts strings of
		/// length 4. 'Set' also makes the first character uppercase and the remaining
		/// characters lowercase to meet the ICU standard.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public string ScriptAbbr
		{
			get { return LocaleParts[1]; }
			set
			{
				if (value.Length != 4 && value.Length != 0)
					return;
				String str = FormatScriptAbbr(value);
				m_WritingSystem.WritingSystem.IcuLocale = BuildIcuLocale(1, str);
				RaiseFullCodeChanged();
			}
		}

		/// <summary>
		/// Makes the first character uppercase and the remaining
		/// characters lowercase to meet the ICU standard.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static String FormatScriptAbbr(string value)
		{
			if (String.IsNullOrEmpty(value))
				return "";
			StringBuilder strBldr = new StringBuilder("");
			char ch = char.ToUpperInvariant(value[0]);
			strBldr.Append(value.ToLowerInvariant());
			strBldr[0] = ch;
			return strBldr.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get/set Country abbreviation of ICULocale. 'Set' only accepts strings of
		/// length 2 or 3. 'Set' converts the string to uppercase to meet the ICU standard.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public string CountryAbbr
		{
			get { return LocaleParts[2]; }
			set
			{
				if (value.Length == 2 || value.Length == 3 || value.Length == 0)
				{
					m_WritingSystem.WritingSystem.IcuLocale = BuildIcuLocale(2, value.ToUpperInvariant());
					RaiseFullCodeChanged();
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the abbreviation of the variant
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public string VariantAbbr
		{
			get { return LocaleParts[3]; }
			set
			{
				m_WritingSystem.WritingSystem.IcuLocale = BuildIcuLocale(3, value);
				RaiseFullCodeChanged();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the displayable name for the ICU code, built from LocaleName, LocaleScript,
		/// LocaleCountry, and LocaleVariant.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public string DisplayName
		{
			get
			{
				StringBuilder sb = new StringBuilder();
				// If the locale name does not yet have a local language variant, then add it
				string localeName = m_LocaleName;
				if (String.IsNullOrEmpty(localeName))
				{
					// use the icuLocale code if we can't find anything else.
					localeName = WritingSystem.IcuLocale;
				}
				else if (m_LocaleName.IndexOf("(") == -1)
				{
					sb.Append(m_LocaleScript);
					if (sb.Length > 0 && m_LocaleCountry != null && m_LocaleCountry.Length > 0)
						sb.Append(", ");
					sb.Append(m_LocaleCountry);
					if (sb.Length > 0 && m_LocaleVariant != null && m_LocaleVariant.Length > 0)
						sb.Append(", ");
					sb.Append(m_LocaleVariant);

					if (sb.Length > 0)
					{
						sb.Insert(0, " (");
						sb.Append(")");
					}
				}
				sb.Insert(0, localeName);	// really the language name.
				return sb.ToString();
			}
		}

		#region IIcuCleanupCallback Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Release everything that holds on to ICU so that we can update ICU files.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void DoneCleanup()
		{
			ReleaseRootRb();
			ReleaseLangDefRb();
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the overridden character type from the language definition if it is not a
		/// diacritic or some other category that is disallowed as a "valid character".
		/// </summary>
		/// <param name="chr">The character.</param>
		/// <returns>The character type if its defined as a PUA character in this language
		/// definition and can be treated as a valid (base) character; otherwise, none</returns>
		/// ------------------------------------------------------------------------------------
		public ValidCharacterType GetOverrideCharType(string chr)
		{
			if (string.IsNullOrEmpty(chr))
				return ValidCharacterType.None;

			switch (GetOverrideCharCategory(chr[0]))
			{
				case "Lu":
				case "Ll":
				case "Lt":
				case "Lm":
				case "Lo":
					return ValidCharacterType.WordForming;
				case "Nd":
				case "No":
				case "Nl":
					return ValidCharacterType.Numeric;
				case "Zs":
				case "Zl":
				case "Pc":
				case "Pd":
				case "Ps":
				case "Pe":
				case "Pi":
				case "Pf":
				case "Po":
				case "Sm":
				case "Sc":
				case "Sk":
				case "So":
				case "Cf":
					return ValidCharacterType.Other;
			}
			return ValidCharacterType.None;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the overridden character category from the language definition.
		/// </summary>
		/// <param name="chr">The character.</param>
		/// <returns>A two-character string representing the Unicode character category of the
		/// given character if it's defined as a PUA character in this language definition;
		/// otherwise, <c>null</c>.</returns>
		/// ------------------------------------------------------------------------------------
		public string GetOverrideCharCategory(char chr)
		{
			string chrCode = ((int)chr).ToString("X");
			foreach (CharDef charDef in PuaDefinitions)
			{
				if (charDef.code == chrCode)
				{
					string[] charDefData = charDef.data.Split(';');
					if (charDefData.Length < 2)
					{
						Debug.Fail("Utterly bogus PUA character data, dude!");
						return null;
					}
					return charDefData[1];
				}
			}
			return null;
		}
	}
	#endregion

	#region XmlWritingSystem
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Encapsulates the information of a writing system for XML serializing purposes
	/// </summary>
	/// ------------------------------------------------------------------------------------
	[ComVisible(false)]
	public struct XmlWritingSystem
	{
		// Hard-code this number because being dependent on FDO produces various Build
		// problems.
		private const string kClsid = "24"; //BaseLgWritingSystem.kclsidLgWritingSystemString;
		private IWritingSystem m_ws;

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new XmlWritingSystem
		/// </summary>
		/// <param name="ws"></param>
		/// --------------------------------------------------------------------------------
		public XmlWritingSystem(IWritingSystem ws)
		{
			m_ws = ws;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the writing system
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public IWritingSystem WritingSystem
		{
			get
			{
				if (m_ws == null)
				{
					m_ws = WritingSystemClass.Create();
					m_ws.WritingSystemFactory = LanguageDefinitionFactory.WritingSystemFactory;
				}
				return m_ws;
			}
			set { m_ws = value; }
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the locale
		/// </summary>
		/// --------------------------------------------------------------------------------
		[XmlElement("Locale" + kClsid)]
		public Integer Locale
		{
			get { return new Integer(m_ws.Locale); }
			set { WritingSystem.Locale = value.integer.val; }
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the right-to-left property
		/// </summary>
		/// --------------------------------------------------------------------------------
		[XmlElement("RightToLeft" + kClsid)]
		public Boolean RightToLeft
		{
			get { return new Boolean(m_ws.RightToLeft); }
			set { WritingSystem.RightToLeft = value.boolean.val; }
		}


		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the default monospace font
		/// </summary>
		/// --------------------------------------------------------------------------------
		[XmlElement("DefaultMonospace" + kClsid)]
		public UniString DefaultMonospace
		{
			get { return new UniString(m_ws.DefaultMonospace); }
			set { WritingSystem.DefaultMonospace = value.str; }
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the default sans serif font
		/// </summary>
		/// --------------------------------------------------------------------------------
		[XmlElement("DefaultSansSerif" + kClsid)]
		public UniString DefaultSansSerif
		{
			get { return new UniString(m_ws.DefaultSansSerif); }
			set { WritingSystem.DefaultSansSerif = value.str; }
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the default body font
		/// </summary>
		/// --------------------------------------------------------------------------------
		[XmlElement("DefaultBodyFont" + kClsid)]
		public UniString DefaultBodyFont
		{
			get { return new UniString(m_ws.DefaultBodyFont); }
			set { WritingSystem.DefaultBodyFont = value.str; }
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the default serif font
		/// </summary>
		/// --------------------------------------------------------------------------------
		[XmlElement("DefaultSerif" + kClsid)]
		public UniString DefaultSerif
		{
			get { return new UniString(m_ws.DefaultSerif); }
			set { WritingSystem.DefaultSerif = value.str; }
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the default serif font variation
		/// </summary>
		/// --------------------------------------------------------------------------------
		[XmlElement("FontVariation" + kClsid)]
		public UniString FontVariation
		{
			get { return new UniString(m_ws.FontVariation); }
			set { WritingSystem.FontVariation = value.str; }
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the default serif font variation
		/// </summary>
		/// --------------------------------------------------------------------------------
		[XmlElement("SansFontVariation" + kClsid)]
		public UniString SansFontVariation
		{
			get { return new UniString(m_ws.SansFontVariation); }
			set { WritingSystem.SansFontVariation = value.str; }
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the default body font features
		/// </summary>
		/// --------------------------------------------------------------------------------
		[XmlElement("BodyFontFeatures" + kClsid)]
		public UniString BodyFontFeatures
		{
			get { return new UniString(m_ws.BodyFontFeatures); }
			set { WritingSystem.BodyFontFeatures = value.str; }
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the ICU locale
		/// </summary>
		/// --------------------------------------------------------------------------------
		[XmlElement("ICULocale" + kClsid)]
		public UniString ICULocale
		{
			get { return new UniString(m_ws.IcuLocale); }
			set { WritingSystem.IcuLocale = value.str; }
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the valid characters
		/// </summary>
		/// --------------------------------------------------------------------------------
		[XmlElement("ValidChars" + kClsid)]
		public UniString ValidChars
		{
			get { return new UniString(m_ws.ValidChars); }
			set { WritingSystem.ValidChars = value.str; }
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the matched pairs
		/// </summary>
		/// --------------------------------------------------------------------------------
		[XmlElement("MatchedPairs" + kClsid)]
		public UniString MatchedPairs
		{
			get { return new UniString(m_ws.MatchedPairs); }
			set { WritingSystem.MatchedPairs = value.str; }
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the punctuation patterns
		/// </summary>
		/// --------------------------------------------------------------------------------
		[XmlElement("PunctuationPatterns" + kClsid)]
		public UniString PunctuationPatterns
		{
			get { return new UniString(m_ws.PunctuationPatterns); }
			set { WritingSystem.PunctuationPatterns = value.str; }
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the capitalization info.
		/// </summary>
		/// --------------------------------------------------------------------------------
		[XmlElement("CapitalizationInfo" + kClsid)]
		public UniString CapitalizationInfo
		{
			get { return new UniString(m_ws.CapitalizationInfo); }
			set { WritingSystem.CapitalizationInfo = value.str; }
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the quotation marks.
		/// </summary>
		/// --------------------------------------------------------------------------------
		[XmlElement("QuotationMarks" + kClsid)]
		public UniString QuotationMarks
		{
			get { return new UniString(m_ws.QuotationMarks); }
			set { WritingSystem.QuotationMarks = value.str; }
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the spelling dictionary name
		/// </summary>
		/// --------------------------------------------------------------------------------
		[XmlElement("SpellCheckDictionary" + kClsid)]
		public UniString SpellCheckDictionary
		{
			get { return new UniString(m_ws.SpellCheckDictionary); }
			set { WritingSystem.SpellCheckDictionary = value.str; }
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the Keyman keyboard, with the appropriate XML element.
		/// </summary>
		/// --------------------------------------------------------------------------------
		[XmlElement("KeymanKeyboard" + kClsid)]
		public UniString KeymanKeyboard
		{
			get { return new UniString(m_ws.KeymanKbdName); }
			set { WritingSystem.KeymanKbdName = value.str; }
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the names
		/// </summary>
		/// --------------------------------------------------------------------------------
		[XmlArray("Name" + kClsid)]
		public NameMultiUnicode Name
		{
			get { return new NameMultiUnicode(WritingSystem); }
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the abbreviations
		/// </summary>
		/// --------------------------------------------------------------------------------
		[XmlArray("Abbr" + kClsid)]
		public AbbrMultiUnicode Abbr
		{
			get { return new AbbrMultiUnicode(WritingSystem); }
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets the collations
		/// </summary>
		/// --------------------------------------------------------------------------------
		[XmlArray("Collations" + kClsid)]
		public XmlCollationArray Collations
		{
			get { return new XmlCollationArray(WritingSystem); }
		}
		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the default monospace font
		/// </summary>
		/// --------------------------------------------------------------------------------
		[XmlElement("LegacyMapping" + kClsid)]
		public UniString LegacyMapping
		{
			get { return new UniString(m_ws.LegacyMapping); }
			set { WritingSystem.LegacyMapping = value.str; }
		}
	}
	#endregion

	#region XmlCollationArray
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Represents all collations for a writing system
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[ComVisible(false)]
	public class XmlCollationArray: ICollection, IEnumerable
	{
		private IWritingSystem m_ws;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Default c'tor
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public XmlCollationArray()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// C'tor
		/// </summary>
		/// <param name="ws"></param>
		/// ------------------------------------------------------------------------------------
		public XmlCollationArray(IWritingSystem ws)
		{
			m_ws = ws;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a string in the given writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public XmlCollation this[int i]
		{
			get { return new XmlCollation(m_ws.get_Collation(i)); }
			set { m_ws.set_Collation(i, value.Collation); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a new string with a ws
		/// </summary>
		/// <param name="value"></param>
		/// ------------------------------------------------------------------------------------
		public virtual void Add(XmlCollation value)
		{
			this[m_ws.CollationCount] = value;
		}

		#region ICollection Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsSynchronized
		{
			get
			{
				// TODO:  Add XmlCollationArray.IsSynchronized getter implementation
				return false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Count
		{
			get
			{
				return m_ws.CollationCount;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="array"></param>
		/// <param name="index"></param>
		/// ------------------------------------------------------------------------------------
		public void CopyTo(Array array, int index)
		{
			// TODO:  Add XmlCollationArray.CopyTo implementation
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public object SyncRoot
		{
			get
			{
				// TODO:  Add XmlCollationArray.SyncRoot getter implementation
				return null;
			}
		}

		#endregion

		#region IEnumerable Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the enumerator
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public IEnumerator GetEnumerator()
		{
			return new XmlCollationEnumerator(this);
		}

		#endregion

		#region Enumerator
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Enumerator for XmlCollationEnumerator
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[ComVisible(false)]
			public class XmlCollationEnumerator : IEnumerator
		{

			private IEnumerator baseEnumerator;

			/// <summary>
			/// Initializes a new instance of SideBarButtonEnumerator class
			/// </summary>
			/// <param name="mappings"></param>
			public XmlCollationEnumerator(XmlCollationArray mappings)
			{
				IEnumerable temp = (IEnumerable)mappings;
				this.baseEnumerator = temp.GetEnumerator();
			}

			/// <summary>
			/// Gets the current element
			/// </summary>
			public XmlCollation Current
			{
				get
				{
					return (XmlCollation)baseEnumerator.Current;
				}
			}

			object IEnumerator.Current
			{
				get
				{
					return baseEnumerator.Current;
				}
			}

			/// <summary>
			/// Moves to the next element in the collection
			/// </summary>
			/// <returns>True if next element exists</returns>
			public bool MoveNext()
			{
				return baseEnumerator.MoveNext();
			}

			/// <summary>
			/// Resets the collection
			/// </summary>
			public void Reset()
			{
				baseEnumerator.Reset();
			}
		}
		#endregion // Enumerator
	}
	#endregion

	#region XmlCollation
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Represents a collation
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[ComVisible(false)]
	[XmlType("LgCollation")]
	public struct XmlCollation
	{
		private ICollation m_collation;
		// This value is hard-coded to avoid needing a dependency on FDO.
		private const string kClsid = "30"; //Collation.kclsidLgCollationString;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// C'tor
		/// </summary>
		/// <param name="coll"></param>
		/// ------------------------------------------------------------------------------------
		public XmlCollation(ICollation coll)
		{
			m_collation = coll;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the underlaying collation object
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public ICollation Collation
		{
			get
			{
				if (m_collation == null)
				{
					m_collation = CollationClass.Create();
					m_collation.WritingSystemFactory = LanguageDefinitionFactory.WritingSystemFactory;
				}
				return m_collation;
			}
			set { m_collation = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The windows LCID
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlElement("WinLCID" + kClsid)]
		public Integer Lcid
		{
			get { return new Integer(m_collation.WinLCID); }
			set { Collation.WinLCID = value.integer.val; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The windows collation
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlElement("WinCollation" + kClsid)]
		public UniString WinCollation
		{
			get { return new UniString(m_collation.WinCollation); }
			set { Collation.WinCollation = value.str; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The name
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlArray("Name" + kClsid)]
		public CollationNameMultiUnicode Name
		{
			get { return new CollationNameMultiUnicode(Collation); }
		}

		/// <summary>
		/// The ICU collation sequence string, if any.
		/// </summary>
		[XmlElement("ICURules" + kClsid)]
		public UniString ICURules
		{
			get { return new UniString(m_collation.IcuRules); }
			set { Collation.IcuRules = value.str; }
		}
	}
	#endregion
}
