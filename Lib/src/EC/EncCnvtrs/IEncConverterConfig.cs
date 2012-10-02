using System;
using System.Runtime.InteropServices;   // for the class attributes
using System.Windows.Forms;
using ECInterfaces;                     // for IEncConverterConfig

namespace SilEncConverters31
{
	public abstract class EncConverterConfig : IEncConverterConfig
	{
		#region Member Variable Definitions
		protected string m_strProgramID;                    // eg. "SilEncConverters31.PyScriptEncConverter" rather than the implementation type
		protected string m_strDisplayName;                  // e.g. "Python Script"
		protected string m_strHtmlFilename;                 // filename of the HTML file that becomes the About HTML control (assumed to be in the local dir by installer)
		protected ProcessTypeFlags m_eDefiningProcessType;  // a process type flag that uniquely defines this configurator type (e.g. ProcessTypeFlags_ICUTransliteration)

		protected string        m_strFriendlyName;      // something nice and friendly (e.g. "Annapurna<>Unicode")
		protected string        m_strConverterID;       // file spec to the map (or some plug-in specific identifier, such as "Devanagari-Latin" (for ICU) or "65001" (for code page UTF8)
		protected string        m_strLhsEncodingID;     // something unique/unchangable (at least from TK) (e.g. SIL-UNICODE_DEVANAGRI-2002<>SIL-UNICODE_IPA-2002)
		protected string        m_strRhsEncodingID;     // something unique/unchangable (at least from TK) (e.g. SIL-UNICODE_DEVANAGRI-2002<>SIL-UNICODE_IPA-2002)
		protected ConvType      m_eConversionType;      // conversion type (see .idl)
		protected Int32         m_lProcessType;         // process type (see .idl)
		protected bool          m_bIsInRepository;      // indicates whether this converter is in the static repository (true) or not (false)
		protected IEncConverter m_pIECParent;           // reference to the parent EC of which this is the configurator

		protected string        m_strImplementType;     // needed for "EncConverters::AddConverterMap"; provided by sub-classes
		#endregion Member Variable Definitions

		#region Public Interface
		public EncConverterConfig
			(
			string strProgramID,
			string strDisplayName,
			string strHtmlFilename,
			ProcessTypeFlags eDefiningProcessType
			)
		{
			m_strProgramID = strProgramID;
			m_strDisplayName = strDisplayName;
			m_strHtmlFilename = strHtmlFilename;
			m_eDefiningProcessType = eDefiningProcessType;

			m_lProcessType = (Int32)ProcessTypeFlags.DontKnow;
			m_eConversionType = ConvType.Unknown;
			m_bIsInRepository = false;
		}

		// [DispId(0)]
		public string ConfiguratorDisplayName
		{
			get { return m_strDisplayName; }
		}

		// [DispId(1)]
		public ProcessTypeFlags DefiningProcessType
		{
			get { return m_eDefiningProcessType; }
		}

		// [DispId(5)]
		public string ConverterFriendlyName
		{
			get { return m_strFriendlyName; }
			set { m_strFriendlyName = value; }
		}

		// [DispId(6)]
		public string ConverterIdentifier
		{
			get { return m_strConverterID; }
			set { m_strConverterID = value; }
		}

		// [DispId(7)]
		public string LeftEncodingID
		{
			get { return m_strLhsEncodingID; }
			set { m_strLhsEncodingID = value; }
		}

		// [DispId(8)]
		public string RightEncodingID
		{
			get { return m_strRhsEncodingID; }
			set { m_strRhsEncodingID = value; }
		}

		// [DispId(9)]
		public virtual ConvType ConversionType
		{
			get { return m_eConversionType; }
			set { m_eConversionType = value; }
		}

		// [DispId(10)]
		public Int32 ProcessType
		{
			get { return m_lProcessType; }
			set { m_lProcessType = value; }
		}

		// [DispId(11)]
		public bool IsInRepository
		{
			get { return m_bIsInRepository; }
			set { m_bIsInRepository = value; }
		}

		// [DispId(13)]
		public IEncConverter ParentEncConverter
		{
			get { return m_pIECParent; }
			set { m_pIECParent = value; }
		}

		public abstract bool Configure
			(
			IEncConverters aECs,
			string strFriendlyName,
			ConvType eConversionType,
			string strLhsEncodingID,
			string strRhsEncodingID
			);

		public abstract void DisplayTestPage
			(
			IEncConverters aECs,
			string strFriendlyName,
			string strConverterIdentifier,
			ConvType eConversionType,
			string strTestData
			);

		protected bool Configure(AutoConfigDialog form)
		{
			m_strFriendlyName = form.FriendlyName;
			m_eConversionType = form.ConversionType;
			m_strLhsEncodingID = form.LhsEncodingId;
			m_strRhsEncodingID = form.RhsEncodingId;

			DialogResult res = form.ShowDialog();
			// update our internal values from the config tab (but only if the user clicked OK or
			//  if it is already in the repository--either editing or MetaCmpd type)
			// NOTE: I've taken out the OR case with IsInRepository, because even in that case
			//  we have to require the OK result. If the user is editing and cancels, then we *don't*
			//  want to update the results. This might mean that I've broken the MetaCmpd cases.
			//  So we'll probably have to have special handling in that class to gracefully remove
			//  the added converter if the user then cancels the dialog
			// NO: I figured out why that was there: if the converter "IsInRepository", it may mean that
			//  the user has made changes and clicked the "Save In Repository" button. In that case, it
			//  is in the repository, so this method needs to return 'true' (so the SelectConverter dialog
			//  will update the list of converters (in case this one was added). However, if the user clicks
			//  Cancel after saving in the repository and making some changes, then those changes should be
			//  ignored. So we *do* want to remove the OR IsInRepository case for the following IF statement
			//  and only update the EncConverter properties in the case where the user actually clicks OK.
			//  But make the 'else' case return 'false', only if it isn't "in the repository".
			// if ((res == DialogResult.OK) || form.IsInRepository)
			if (res == DialogResult.OK)
			{
				if (!String.IsNullOrEmpty(form.FriendlyName))
					m_strFriendlyName = form.FriendlyName;
				if (!String.IsNullOrEmpty(form.ConverterIdentifier))
					m_strConverterID = form.ConverterIdentifier;
				if (!String.IsNullOrEmpty(form.LhsEncodingId))
					m_strLhsEncodingID = form.LhsEncodingId;
				if (!String.IsNullOrEmpty(form.RhsEncodingId))
					m_strRhsEncodingID = form.RhsEncodingId;
				if (form.ConversionType != ConvType.Unknown)
					m_eConversionType = form.ConversionType;
				m_lProcessType = (int)form.ProcessType;
				m_bIsInRepository = form.IsInRepository;

				// and... if we have the pointer to the parent EC, then go ahead and update that also
				//  (to save *some* clients a step)
				if (m_pIECParent != null)
				{
					// initialize it with the details we have.
					m_pIECParent.Initialize(m_strFriendlyName, m_strConverterID, ref m_strLhsEncodingID,
						ref m_strRhsEncodingID, ref m_eConversionType, ref m_lProcessType,
						m_pIECParent.CodePageInput, m_pIECParent.CodePageOutput, true);

					// and update it's temporariness status
					m_pIECParent.IsInRepository = m_bIsInRepository;
				}
			}
			// it might either have already been in the repository (e.g. editing) or added while there (e.g. compound converters)
			//  however, in any case, if we don't have a ConverterFriendlyName, all bets are off (and the caller will choke)
			else if (form.IsInRepository && !String.IsNullOrEmpty(ConverterFriendlyName))
			{
				// and update it's temporariness status
				if (m_pIECParent != null)
					m_pIECParent.IsInRepository = m_bIsInRepository;

				// fall thru to return true (so the SelectConverter dialog will refresh)
			}
			else
				return false;

			return true;
		}

		protected void InitializeFromThis
			(
			ref string strFriendlyName,
			ref string strConverterIdentifier,
			ref ConvType eConversionType,
			ref string strTestData
			)
		{
			if (String.IsNullOrEmpty(strFriendlyName))
				strFriendlyName = m_strFriendlyName;
			if (String.IsNullOrEmpty(strConverterIdentifier))
				strConverterIdentifier = m_strConverterID;
			if (String.IsNullOrEmpty(strTestData))
				strTestData = "Test Data";
			if (eConversionType == ConvType.Unknown)
				eConversionType = m_eConversionType;
		}

		protected void DisplayTestPage(AutoConfigDialog form)
		{
			form.Text = String.Format("{0} (converter: {1})", m_strDisplayName, form.FriendlyName);
			form.ShowDialog();
		}

		[DispId(28)]
		public override string ToString()
		{
			return this.ConverterFriendlyName;
		}
		#endregion Public Interface
	}
}
