// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: XmlScrNote.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using SILUBS.SharedScrUtils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using System.Xml;
using System.Diagnostics;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.Utils;
using SIL.FieldWorks.FDO.DomainServices;
using System.Diagnostics.CodeAnalysis;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Stores information about a single Scripture annotation (ScrScriptureNote).
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[XmlType("annotation")]
	public class XmlScrNote
	{
		#region Data members
		private static Dictionary<XmlNoteType, string> s_noteTypes = new Dictionary<XmlNoteType, string>();
		private bool m_serializingForOxes = false;
		private string m_subType;
		private BCVRef m_startRef = null;
		private BCVRef m_endRef = null;
		private Guid m_guidBegObj = Guid.Empty;
		private Guid m_guidEndObj = Guid.Empty;
		private int m_wsDefault;
		private ILgWritingSystemFactory m_lgwsf;
		private DateTime m_createdDate;
		private DateTime m_modifiedDate;
		private DateTime m_resolvedDate;
		private string m_typeName;
		private Guid m_guidType = Guid.Empty;
		private XmlNoteType m_annotationType = XmlNoteType.Unspecified;
		private List<XmlNoteCategory> m_categories;
		private List<XmlNotePara> m_quoteParas;
		private List<XmlNotePara> m_discussionParas;
		private List<XmlNotePara> m_resolutionParas;
		private List<XmlNotePara> m_suggestionParas;
		private List<XmlNoteResponse> m_responsesParas;
		#endregion

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the <see cref="XmlScrNote"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static XmlScrNote()
		{
			s_noteTypes[XmlNoteType.Consultant] = "consultantNote";
			s_noteTypes[XmlNoteType.Translator] = "translatorNote";
			s_noteTypes[XmlNoteType.PreTypesettingCheck] = "pre-typesettingCheck";
			s_noteTypes[XmlNoteType.Unspecified] = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="XmlScrNote"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public XmlScrNote()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="XmlScrNote"/> class based on
		/// the given Scripture note.
		/// </summary>
		/// <param name="ann">The Scripture annotation.</param>
		/// <param name="wsDefault">The default (analysis) writing system.</param>
		/// <param name="lgwsf">The writing system factory.</param>
		/// ------------------------------------------------------------------------------------
		public XmlScrNote(IScrScriptureNote ann, int wsDefault, ILgWritingSystemFactory lgwsf)
			: this()
		{
			m_wsDefault = wsDefault;
			m_lgwsf = lgwsf;
			ResolutionStatus = ann.ResolutionStatus;
			m_typeName = ann.AnnotationTypeRA.Name.get_String(WritingSystemServices.FallbackUserWs(ann.Cache)).Text;
			m_guidType = ann.AnnotationTypeRA.Guid;
			SetNoteType(ann);

			m_startRef = ann.BeginRef;
			if (ann.BeginRef != ann.EndRef)
				m_endRef = ann.EndRef;

			if (ann.BeginObjectRA != null)
				m_guidBegObj = ann.BeginObjectRA.Guid;

			if (ann.BeginObjectRA != ann.EndObjectRA && ann.EndObjectRA != null)
				m_guidEndObj = ann.EndObjectRA.Guid;

			BeginOffset = ann.BeginOffset;
			EndOffset = ann.EndOffset;

			m_createdDate = ann.DateCreated;
			m_modifiedDate = ann.DateModified;
			m_resolvedDate = ann.DateResolved;

			Quote = XmlNotePara.GetParagraphList(ann.QuoteOA, m_wsDefault, m_lgwsf);
			Discussion = XmlNotePara.GetParagraphList(ann.DiscussionOA, m_wsDefault, m_lgwsf);
			Suggestion = XmlNotePara.GetParagraphList(ann.RecommendationOA, m_wsDefault, m_lgwsf);
			Resolution = XmlNotePara.GetParagraphList(ann.ResolutionOA, m_wsDefault, m_lgwsf);
			Categories = XmlNoteCategory.GetCategoryList(ann, m_lgwsf);
			Responses = XmlNoteResponse.GetResponsesList(ann, m_wsDefault, m_lgwsf);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the type (and possibly subtype) of the given Scripture annotation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetNoteType(IScrScriptureNote ann)
		{
			Debug.Assert(ann.AnnotationTypeRA != null, "The annotation type is not set!");
			if (ann.AnnotationTypeRA == null)
				return;

			if (m_guidType == CmAnnotationDefnTags.kguidAnnTranslatorNote)
				AnnotationType = XmlNoteType.Translator;
			else if (m_guidType == CmAnnotationDefnTags.kguidAnnConsultantNote)
				AnnotationType = XmlNoteType.Consultant;
			else if (ann.ResolutionStatus == NoteStatus.Closed) // ignored checking error
			{
				ICmAnnotationDefn defn = ann.AnnotationTypeRA;
				SubType = GetCheckingErrorSubType(defn.Guid);
				AnnotationType = XmlNoteType.PreTypesettingCheck;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the type of the checking error sub.
		/// </summary>
		/// <param name="guid">The GUID for the Scripture check.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private string GetCheckingErrorSubType(Guid guid)
		{
#if !__MonoCS__ // comparing value type to null
			Debug.Assert(guid != null);
#endif
			if (guid == StandardCheckIds.kguidChapterVerse)
				return "chapterVerseCheck";
			else if (guid == StandardCheckIds.kguidCharacters)
				return "characterCheck";
			else if (guid == StandardCheckIds.kguidMatchedPairs)
				return "matchedPairsCheck";
			else if (guid == StandardCheckIds.kguidMixedCapitalization)
				return "mixedCapitalizationCheck";
			else if (guid == StandardCheckIds.kguidPunctuation)
				return "punctuationCheck";
			else if (guid == StandardCheckIds.kguidRepeatedWords)
				return "repeatedWordsCheck";
			else if (guid == StandardCheckIds.kguidCapitalization)
				return "capitalizationCheck";
			else if (guid == StandardCheckIds.kguidQuotations)
				return "quotationCheck";
			else
			{
				Debug.Fail("OXES import does not recognize the editorial check with the guid: "
					+ guid.ToString());
				return "Unknown check";
			}
		}
		#endregion

		#region Serializing/Deserializing methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Serializes the specified annotation and writes it to the specified XML writer.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool Serialize(XmlTextWriter writer, IScrScriptureNote ann)
		{
			return Serialize(writer, ann, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Serializes the specified annotation and writes it to the specified XML writer.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "cache is a reference")]
		public static bool Serialize(XmlTextWriter writer, IScrScriptureNote ann,
			string languageInFocus)
		{
			if (writer == null || ann == null)
				return false;

			try
			{
				FdoCache cache = ann.Cache;
				ILgWritingSystemFactory lgwsf = cache.LanguageWritingSystemFactoryAccessor;
				XmlScrNote xmlNote = new XmlScrNote(ann, cache.DefaultAnalWs, lgwsf);

				if (!string.IsNullOrEmpty(xmlNote.Type))
				{
					if (!string.IsNullOrEmpty(languageInFocus))
						xmlNote.LanguageInFocus = languageInFocus;

					xmlNote.m_serializingForOxes = true;
					return XmlSerializationHelper.SerializeDataAndWriteAsNode(writer, xmlNote);
				}
			}
			catch
			{
				// TODO: Report something useful.
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deserializes the oxes annotation node.
		/// </summary>
		/// <param name="node">The node.</param>
		/// <param name="scr">The scripture.</param>
		/// <param name="styleSheet">The style sheet.</param>
		/// <returns>The deserialized annotation</returns>
		/// ------------------------------------------------------------------------------------
		public static IScrScriptureNote Deserialize(XmlNode node, IScripture scr, FwStyleSheet styleSheet)
		{
			XmlScrNote xmlAnn = XmlSerializationHelper.DeserializeFromString<XmlScrNote>(node.OuterXml, true);
			return xmlAnn.WriteToCache(scr, styleSheet);
		}

		#endregion

		#region XML attribute public variables
		/// <summary>The sub type of note</summary>
		[XmlAttribute("subType")]
		public string SubType
		{
			get { return m_subType; }
			set
			{
				m_subType = value;
				SetGuidType();
			}
		}

		/// <summary>The type of note</summary>
		[XmlAttribute("languageInFocus")]
		public string LanguageInFocus;

		/// <summary>This is used only for serializing and deserializing. To set the status
		/// directly from code, use the ResolutionStatus property.</summary>
		[XmlAttribute("status")]
		public int Status;

		/// <summary>Character offset in the first paragraph where this annotation begins</summary>
		[XmlAttribute("beginOffset")]
		public int BeginOffset;

		/// <summary>Character offset in the last paragraph where this annotation ends</summary>
		[XmlAttribute("endOffset")]
		public int EndOffset;

		#endregion

		#region XML attribute public properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the annotation type as a string. This is used only for serializing
		/// and deserializing. To set the type directly from code, use the AnnotationType
		/// property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlAttribute("type")]
		public string Type
		{
			get { return s_noteTypes[AnnotationType]; }
			set
			{
				if (string.IsNullOrEmpty(value))
					AnnotationType = XmlNoteType.Consultant;
				else
				{
					foreach (KeyValuePair<XmlNoteType, string> kvp in s_noteTypes)
					{
						if (kvp.Value != null &&
							kvp.Value.ToLowerInvariant() == value.ToLowerInvariant())
						{
							AnnotationType = kvp.Key;
							return;
						}
					}

					AnnotationType = XmlNoteType.Consultant;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the English name of the annotation representing the type of note.
		/// This is used only for serializing and deserializing. To set the type directly
		/// from code, use the AnnotationType property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlAttribute("typeName")]
		public string AnnotationTypeName
		{
			get { return (m_serializingForOxes ? null : m_typeName); }
			set { m_typeName = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the GUID of the annotation representing the type of note.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlAttribute("typeGuid")]
		public string AnnotationTypeGuid
		{
			get { return (m_serializingForOxes || m_guidType == Guid.Empty ? null : m_guidType.ToString()); }
			set
			{
				m_guidType = new Guid(value);

				if (m_guidType == CmAnnotationDefnTags.kguidAnnTranslatorNote)
					m_annotationType = XmlNoteType.Translator;
				else if (m_guidType == CmAnnotationDefnTags.kguidAnnConsultantNote)
					m_annotationType = XmlNoteType.Consultant;
				else if (m_guidType != Guid.Empty)
					m_annotationType = XmlNoteType.PreTypesettingCheck;
				else
					m_annotationType = XmlNoteType.Unspecified;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the string representation of the reference in the OXES format.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlAttribute("oxesRef")]
		public string OxesScrRef
		{
			get
			{
				// When not writing pure OXES, this attribute is not used.
				// BeginScrRef and EndScrRef are used instead.
				return (m_serializingForOxes ? GetOxesRef(m_startRef, m_endRef) : null);
			}
			set
			{
				if (!string.IsNullOrEmpty(value))
				{
					string[] refs = value.Split('-');
					m_startRef = SetOxesRef(refs[0]);
					if (refs.Length > 1)
						m_endRef = SetOxesRef(refs[1]);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the string representation of the starting Scripture reference of note
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlAttribute("beginScrRef")]
		public string BeginScrRef
		{
			get { return GetRef(m_startRef); }
			set { m_startRef = SetRef(value); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the string representation of the starting Scripture reference of note.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlAttribute("endScrRef")]
		public string EndScrRef
		{
			get { return GetRef(m_endRef); }
			set { m_endRef = SetRef(value); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the GUID of the first paragraph annotated by this note
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlAttribute("beginObj")]
		public string BeginObj
		{
			get
			{
				return (m_serializingForOxes || m_guidBegObj == Guid.Empty ?
					null : m_guidBegObj.ToString());
			}
			set { m_guidBegObj = new Guid(value); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the GUID of the last paragraph annotated by this note
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlAttribute("endObj")]
		public string EndObj
		{
			get
			{
				if (m_serializingForOxes)
					return null;

				return (m_guidEndObj == Guid.Empty ? BeginObj : m_guidEndObj.ToString());
			}
			set { m_guidEndObj = new Guid(value); }
		}

		#endregion

		#region XML elements
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the date and time note was created. The setter expects a UTC time value
		/// and the getter will always return the time in UTC.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlElement("created")]
		public string DateTimeCreated
		{
			get { return m_createdDate.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.ff"); }
			set { m_createdDate = ScrNoteImportManager.ParseUniversalTime(value); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the date and time note was last modified. The setter expects a UTC
		/// time value and the getter will always return the time in UTC.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlElement("modified")]
		public string DateTimeModified
		{
			get { return m_modifiedDate.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.ff"); }
			set
			{
				DateTime date = ScrNoteImportManager.ParseUniversalTime(value);
				if (date < DateTime.FromFileTimeUtc(0))
					date = DateTime.FromFileTime(0);
				m_modifiedDate = date;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the date and time note was resolved. The setter expects a UTC time value
		/// and the getter will always return the time in UTC.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlElement("resolved")]
		public string DateTimeResolved
		{
			get
			{
				return (ResolutionStatus == NoteStatus.Open ?
					string.Empty : m_resolvedDate.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.ff"));
			}
			set
			{
				DateTime date = ScrNoteImportManager.ParseUniversalTime(value);
				if (date < DateTime.FromFileTimeUtc(0))
					date = (m_modifiedDate > DateTime.FromFileTimeUtc(0)) ? m_modifiedDate : DateTime.FromFileTime(0);

				m_resolvedDate = date;
			}
		}

		#endregion

		#region XML arrays
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the categories of the note.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlArray("notationCategories")]
		public List<XmlNoteCategory> Categories
		{
			get { return m_categories; }
			set { m_categories = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the quote paragraphs (cited text) of the note.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlArray("notationQuote")]
		public List<XmlNotePara> Quote
		{
			get { return m_quoteParas; }
			set { m_quoteParas = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the discussion paragraphs of the note.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlArray("notationDiscussion")]
		public List<XmlNotePara> Discussion
		{
			get { return m_discussionParas; }
			set { m_discussionParas = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the resolution paragraphs of the note.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlArray("notationResolution")]
		public List<XmlNotePara> Resolution
		{
			get { return m_resolutionParas; }
			set { m_resolutionParas = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the recommendation (or suggestion) paragraphs of the note.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlArray("notationRecommendation")]
		public List<XmlNotePara> Suggestion
		{
			get { return m_suggestionParas; }
			set { m_suggestionParas = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the responses of the note
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlElement("notationResponse")]
		public List<XmlNoteResponse> Responses
		{
			get { return m_responsesParas; }
			set { m_responsesParas = value; }
		}

		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the resolution status.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public NoteStatus ResolutionStatus
		{
			get { return (Status == 0 ? NoteStatus.Open : NoteStatus.Closed); }
			set { Status = (int)value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the GUID of the first paragraph annotated by this note.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public Guid BeginObjGuid
		{
			get { return m_guidBegObj; }
			set { m_guidBegObj = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the GUID of the last paragraph annotated by this note.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public Guid EndObjGuid
		{
			get { return m_guidEndObj == Guid.Empty ? BeginObjGuid : m_guidEndObj; }
			set { m_guidEndObj = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the beginning reference as a BCV reference.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public BCVRef BeginScrBCVRef
		{
			get { return m_startRef; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the ending reference as a BCV reference.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public BCVRef EndScrBCVRef
		{
			get { return (m_endRef == null ? BeginScrBCVRef : m_endRef); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The type of annotation
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public XmlNoteType AnnotationType
		{
			get { return m_annotationType; }
			set
			{
				m_annotationType = value;
				SetGuidType();
			}
		}

		#endregion

		#region SetOxesRef/GetOxesRef methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Used to set the value of m_guidType when either the Type or SubType is changed.
		/// This was needed so that SubType did not have to occur before Type in the XML.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetGuidType()
		{
			// Both the Type and SubType fields must be available for the annotations related to
			// Editorial Checks.
			if (!(m_annotationType == XmlNoteType.PreTypesettingCheck && SubType == null))
				m_guidType = ScrNoteImportManager.GetAnnotationTypeGuid(Type, SubType);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a Scripture reference to a string builder.  There may be one or two of these
		/// added to the string builder, with a hyphen inserted between them (if there's two).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static string GetOxesRef(BCVRef startRef, BCVRef endRef)
		{
			StringBuilder bldr = new StringBuilder();

			string sref = startRef.AsString;
			sref = sref.Replace(' ', '.');
			bldr.Append(sref.Replace(':', '.'));

			if (endRef != null && startRef != endRef)
			{
				sref = endRef.AsString;
				sref = sref.Replace(' ', '.');
				bldr.AppendFormat("-{0}", sref.Replace(':', '.'));
			}

			return bldr.ToString();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Parse a reference like "MAT.1.2", returning the integer equivalent.
		/// </summary>
		/// <param name="sRef"></param>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		private static BCVRef SetOxesRef(string sRef)
		{
			if (string.IsNullOrEmpty(sRef))
				return null;

			string[] rgs = sRef.Split('.');
			if (rgs.Length == 0)
				return null;

			int nBook = BCVRef.BookToNumber(rgs[0]);
			if (nBook <= 0)
				return null;

			BCVRef bcvref = new BCVRef(nBook, 0, 0);

			int val;
			if (rgs.Length > 1 && int.TryParse(rgs[1], out val))
			{
				bcvref.Chapter = val;
				if (rgs.Length > 2 && int.TryParse(rgs[2], out val))
					bcvref.Verse = val;
			}

			return bcvref;
		}

		#endregion

		#region GetRef/SetRef methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the string representation of the specified BCVRef for serialization.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private string GetRef(BCVRef bcvref)
		{
			// When writing pure OXES, don't use this attribute, use oxesRef
			return (m_serializingForOxes || bcvref == null || bcvref.IsEmpty ?
				null : bcvref.ToString(BCVRef.RefStringFormat.Exchange));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Parses the specified string reference into a BCVRef.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private BCVRef SetRef(string sRef)
		{
			return (string.IsNullOrEmpty(sRef) ? null : new BCVRef(sRef));
		}

		#endregion

		#region Methods for writing annotation to cache
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Writes this annotation to the specified cache.
		/// </summary>
		/// <param name="scr">The scripture.</param>
		/// <param name="styleSheet">The style sheet.</param>
		/// <returns>The created annotation</returns>
		/// ------------------------------------------------------------------------------------
		internal IScrScriptureNote WriteToCache(IScripture scr, FwStyleSheet styleSheet)
		{
			if (AnnotationTypeGuid == Guid.Empty.ToString())
				return null;

			Debug.Assert(scr.Cache == styleSheet.Cache, "This can't end well");
			int bookNum = BeginScrBCVRef.Book;
			IScrBookAnnotations sba = scr.BookAnnotationsOS[bookNum - 1];

			if (sba == null)
			{
				sba = scr.Cache.ServiceLocator.GetInstance<IScrBookAnnotationsFactory>().Create();
				scr.BookAnnotationsOS.Insert(bookNum - 1, sba);
			}

			IScrScriptureNote scrNote = FindOrCreateAnnotation(styleSheet);

			scrNote.BeginOffset = BeginOffset;
			scrNote.EndOffset = EndOffset;
			scrNote.ResolutionStatus = ResolutionStatus;

			if (m_createdDate > DateTime.MinValue)
				scrNote.DateCreated = m_createdDate;

			if (m_modifiedDate > DateTime.MinValue)
				scrNote.DateModified = m_modifiedDate;

			if (m_resolvedDate > DateTime.MinValue)
				scrNote.DateResolved = m_resolvedDate;

			foreach (XmlNoteCategory category in Categories)
				category.WriteToCache(scrNote);

			foreach (XmlNoteResponse response in Responses)
				response.WriteToCache(scrNote, styleSheet);

			return scrNote;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Prepares the paragraphs for the annotation text fields (e.g. Quote, Recommendation,
		/// etc.) and creates the annotation (or finds an existing one having all the same
		/// information).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "cache is a reference")]
		private IScrScriptureNote FindOrCreateAnnotation(FwStyleSheet styleSheet)
		{
			FdoCache cache = styleSheet.Cache;

			ParagraphCollection parasQuote = new ParagraphCollection(Quote, styleSheet, cache.DefaultVernWs);
			ParagraphCollection parasDiscussion = new ParagraphCollection(Discussion, styleSheet);
			ParagraphCollection parasRecommendation = new ParagraphCollection(Suggestion, styleSheet);
			ParagraphCollection parasResolution = new ParagraphCollection(Resolution, styleSheet);

			if (m_guidType == Guid.Empty)
				m_guidType = CmAnnotationDefnTags.kguidAnnConsultantNote;
			ScrAnnotationInfo info = new ScrAnnotationInfo(m_guidType, parasDiscussion,
				 parasQuote, parasRecommendation, parasResolution, BeginOffset,
				 BeginScrBCVRef, EndScrBCVRef, m_createdDate);

			IScrScriptureNote scrNote =
				ScrNoteImportManager.FindOrCreateAnnotation(info, m_guidBegObj);

			parasQuote.WriteToCache(scrNote.QuoteOA);
			parasDiscussion.WriteToCache(scrNote.DiscussionOA);
			parasRecommendation.WriteToCache(scrNote.RecommendationOA);
			parasResolution.WriteToCache(scrNote.ResolutionOA);

			return scrNote;
		}

		#endregion
	}
}
