/*-----------------------------------------------------------------------------------*//*:Ignore
Copyright 2001, SIL International. All rights reserved.

File: RnImportDlg.cpp
Responsibility: Steve McConnel
Last reviewed: never.

Description:
	Implementation of the File / Import wizard dialog classes for the Data Notebook.

TODO:
	1. automatically copy selected data in list of fields to "replace" box of text replacement
	definition dialog.
-------------------------------------------------------------------------------*//*:End Ignore*/
#include "Main.h"
#pragma hdrstop

#import "FwCoreDlgs.tlb" raw_interfaces_only rename_namespace("FwCoreDlgs")

#include <direct.h>
#include <io.h>

#undef THIS_FILE
DEFINE_THIS_FILE

BEGIN_CMD_MAP(RnImportTextOptionsDlg)
	ON_CID_CHILD(kctidImportTFAddWrtSys, &RnImportTextOptionsDlg::CmdAddWrtSys, NULL)
END_CMD_MAP_NIL()
BEGIN_CMD_MAP(RnImportMultiLingualOptionsDlg)
	ON_CID_CHILD(kctidImportMLAddWrtSys, &RnImportMultiLingualOptionsDlg::CmdAddWrtSys, NULL)
END_CMD_MAP_NIL()
BEGIN_CMD_MAP(RnImportChrMappingDlg)
	ON_CID_CHILD(kctidImportCharMapWrtSystems, &RnImportChrMappingDlg::CmdAddWrtSys, NULL)
END_CMD_MAP_NIL()

//:> This is used by various calls to strpbrk, strspn, and strcspn.
static const char kszSpace[] = " \t\r\n\f";

// Forward declarations of static utility functions.

// return values for AddNewWritingSystem() and OnAddWrtSysClicked()
#define ADDWS_ERROR 0
#define ADDWS_NOTADDED 1
#define ADDWS_ADDED 2

static int AddNewWritingSystem(RnImportWizard * priw, ILgWritingSystemFactory * pwsf, HWND hwnd,
	Vector<WrtSysData> & vwsdAll, Vector<WrtSysData> & vwsdActive);
static void GetAllWrtSys(ILgWritingSystemFactory * pwsf, Vector<WrtSysData> & vwsdAll);
static int OnAddWrtSysClicked(HWND hwnd, int ctidFrom, RnImportWizard * priw,
	Vector<WrtSysData> & vwsdAll);
static void InitializeWrtSysList(HWND hwnd, RnImportWizard * priw, int wsDefault,
	const wchar * pszSel = NULL);
static int GetFirstListViewSelection(HWND hwndList);
#if 99-99
static void ListEncConverters();
#endif

//:>********************************************************************************************
//:>	RnSfMarker methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Default constructor.
----------------------------------------------------------------------------------------------*/
RnSfMarker::RnSfMarker()
{
	m_rt = krtNone;
	m_nLevel = 0;
	m_flidDst = 0;
	m_wsDefault = 0;
	m_fIgnoreEmpty = false;
	m_clo.m_fHaveMulti = false;
	m_clo.m_fHaveSub = false;
	m_clo.m_fHaveBetween = false;
	m_clo.m_fHaveBefore = false;
	m_clo.m_fIgnoreNewStuff = false;
	m_clo.m_pnt = kpntAbbreviation;
	m_txo.m_fStartParaNewLine = false;
	m_txo.m_fStartParaBlankLine = true;
	m_txo.m_fStartParaIndented = true;
	m_txo.m_fStartParaShortLine = false;
	m_txo.m_cchShortLim = 60;
	m_txo.m_ws = 0;
}

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
RnSfMarker::RnSfMarker(const char * pszMkr, const char * pszNam, const char * pszLng,
	const char * pszMkrOverThis, RecordType rt, int nLevel, int flid)
	: m_stabsMkr(pszMkr), m_stabsNam(pszNam), m_stabsLng(pszLng),
	m_stabsMkrOverThis(pszMkrOverThis)
{
	m_rt = rt;
	m_nLevel = nLevel;
	m_flidDst = flid;
	m_wsDefault = 0;
	m_fIgnoreEmpty = false;
	m_clo.m_fHaveMulti = false;
	m_clo.m_fHaveSub = false;
	m_clo.m_fHaveBetween = false;
	m_clo.m_fHaveBefore = false;
	m_clo.m_fIgnoreNewStuff = false;
	m_clo.m_pnt = kpntAbbreviation;
	m_txo.m_fStartParaNewLine = false;
	m_txo.m_fStartParaBlankLine = true;
	m_txo.m_fStartParaIndented = true;
	m_txo.m_fStartParaShortLine = false;
	m_txo.m_cchShortLim = 60;
	m_txo.m_ws = 0;
}

/*----------------------------------------------------------------------------------------------
	Reinitialize all the member variables.
----------------------------------------------------------------------------------------------*/
void RnSfMarker::Clear()
{
	m_stabsMkr.Clear();
	m_stabsNam.Clear();
	m_stabsLng.Clear();
	m_stabsMkrOverThis.Clear();
	m_rt = krtNone;
	m_nLevel = 0;
	m_flidDst = 0;
	m_wsDefault = 0;
	m_fIgnoreEmpty = false;
	m_txo.m_stuStyle.Clear();
	m_txo.m_fStartParaNewLine = false;
	m_txo.m_fStartParaBlankLine = true;
	m_txo.m_fStartParaIndented = true;
	m_txo.m_fStartParaShortLine = false;
	m_txo.m_cchShortLim = 60;
	m_txo.m_ws = 0;
	m_clo.m_fHaveMulti = false;
	m_clo.m_staDelimMulti.Clear();
	m_clo.m_fHaveSub = false;
	m_clo.m_staDelimSub.Clear();
	m_clo.m_fHaveBetween = false;
	m_clo.m_staMarkStart.Clear();
	m_clo.m_staMarkEnd.Clear();
	m_clo.m_fHaveBefore = false;
	m_clo.m_staBefore.Clear();
	m_clo.m_fIgnoreNewStuff = false;
	m_clo.m_vstaMatch.Clear();
	m_clo.m_vstaReplace.Clear();
	m_clo.m_staEmptyDefault.Clear();
	m_clo.m_pnt = kpntAbbreviation;
	m_dto.m_vstaFmt.Clear();
	m_mlo.m_ws = 0;
}


//:>********************************************************************************************
//:>	RnImportCharMapping methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
RnImportCharMapping::RnImportCharMapping()
{
	m_cmt = kcmtNone;
	m_tptDirect = 0;
}

/*----------------------------------------------------------------------------------------------
	Reinitialize all the member variables.
----------------------------------------------------------------------------------------------*/
void RnImportCharMapping::Clear()
{
	m_staBegin.Clear();
	m_staEnd.Clear();
	m_cmt = kcmtNone;
	m_strOldWritingSystem.Clear();
	m_stuCharStyle.Clear();
	m_tptDirect = 0;
}


//:>********************************************************************************************
//:>	RnShoeboxSettings methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
RnShoeboxSettings::RnShoeboxSettings()
{
	m_isfmRecord = -1;
}

/*----------------------------------------------------------------------------------------------
	Clear all the member variables.
----------------------------------------------------------------------------------------------*/
void RnShoeboxSettings::Clear()
{
	m_strSettingsName.Clear();
	m_vsfm.Clear();
	m_isfmRecord = -1;
	m_hmcisfm.Clear();
	m_vrcmp.Clear();
	m_hmsaicmp.Clear();
	m_strDbFile.Clear();
	m_strPrjFile.Clear();
}

/*----------------------------------------------------------------------------------------------
	Set the member variables from the Shoebox Anthropology Guide.
----------------------------------------------------------------------------------------------*/
void RnShoeboxSettings::SetDefaults()
{
	Clear();
	RnSfMarker sfm;
	int isfm = 0;
/*	\date	The date the data was actually observed or elicited. This normally would be set up
			as the record marker (like a diary).  For ease of sorting, the dates should be
			entered as yyyy-mm-dd (e.g. 1998-03-23).  Doing so will keep the events or
			observations in chronological order.  If several notes are written for the same day,
			the date should be followed by the letters a, b, c, etc. (e.g. 1998-03-23b).  More
			than 26 entries for a given day (not common), use za, zb, etc.	*/
	sfm.m_stabsMkr = "date";
	sfm.m_stabsNam = "Date observed or elicited";
	sfm.m_flidDst = kflidRnEvent_DateOfEvent;
	// The next two lines mark this as beginning an Event record.
	sfm.m_rt = RnSfMarker::krtEvent;
	sfm.m_nLevel = 1;
	m_vsfm.Push(sfm);
	m_hmcisfm.Insert(sfm.m_stabsMkr.Chars(), isfm);
	m_isfmRecord = isfm;
/*	\de		Date entered.  It is not always possible to input data the same day we collect it.
			This field is used to indicate the date the data is actually entered into the
			computer (or when it was entered it in a data notebook).  This distinction is
			important since the longer we wait to transcribe our notes or write down an incident
			we participated in or heard about, the more unreliable the facts become.  Later on,
			we may find that we can possibly explain a discrepancy in our data by the fact that
			it was not written up until several days (or even weeks) after the event was
			observed.  Either enter the date using the same format as the \date field, or enter
			the time difference, e.g. d02 (for two days later), d15, w03 (w = weeks),
			y03 (y = years).	*/
	sfm.Clear();
	sfm.m_stabsMkr = "de";
	sfm.m_stabsNam = "Date entered";
	sfm.m_stabsMkrOverThis = "date";
	sfm.m_flidDst = kflidRnGenericRec_DateCreated;
	m_vsfm.Push(sfm);
	m_hmcisfm.Insert(sfm.m_stabsMkr.Chars(), ++isfm);
/*	\wthr	The day’s weather.  This field is useful in that it makes it easy, after several
			years of faithful data collection, to draw conclusions concerning the weather
			patterns of the area, and its relation to behavior and cultural events.  If several
			entries are written for a given day, you only need to fill in this field in the
			first entry.	*/
	sfm.Clear();
	sfm.m_stabsMkr = "wthr";
	sfm.m_stabsNam = "The day’s weather";
	sfm.m_stabsMkrOverThis = "date";
	sfm.m_flidDst = kflidRnEvent_Weather;
	m_vsfm.Push(sfm);
	m_hmcisfm.Insert(sfm.m_stabsMkr.Chars(), ++isfm);
/*	\rscr	Researcher.  Where more than one person is involved in data collection, teams will
			want to use this field to specify which member made the entry.  This allows data
			from multiple researchers to be stored in one database, and yet be viewed, if
			needed, separately.  Generally, initials are adequate and a Shoebox range set should
			be set up for this field.	*/
	sfm.Clear();
	sfm.m_stabsMkr = "rscr";
	sfm.m_stabsNam = "Researcher";
	sfm.m_stabsMkrOverThis = "date";
	sfm.m_flidDst = kflidRnGenericRec_Researchers;
	m_vsfm.Push(sfm);
	m_hmcisfm.Insert(sfm.m_stabsMkr.Chars(), ++isfm);
/*	\type	Type of data.  This field is used to specify if the data for this entry comes from
			an observation, reported information, an impression, an informal conversation you
			had or overheard, a more formal interview, a book or article, or some other data
			type.  This also helps you to evaluate the validity of your data.  Abbreviations
			may be used; set up a Shoebox range set for this field to insure consistency.	*/
	sfm.Clear();
	sfm.m_stabsMkr = "type";
	sfm.m_stabsNam = "Type of data";
	sfm.m_stabsMkrOverThis = "date";
	sfm.m_flidDst = kflidRnEvent_Type;
	m_vsfm.Push(sfm);
	m_hmcisfm.Insert(sfm.m_stabsMkr.Chars(), ++isfm);
/*	\loc	Location.  Where were you when you collected the data?  In your home, office,
			courtyard, a house or garden, the town square?  Location can effect the type of
			information you are getting.  A Shoebox range set might be useful here as well,
			depending on how you enter these locations.	*/
	sfm.Clear();
	sfm.m_stabsMkr = "loc";
	sfm.m_stabsNam = "Location";
	sfm.m_stabsMkrOverThis = "date";
	sfm.m_flidDst = kflidRnEvent_Locations;
	m_vsfm.Push(sfm);
	m_hmcisfm.Insert(sfm.m_stabsMkr.Chars(), ++isfm);
/*	\srce	Source.  This field contains the name of the person(s) from whom the data for this
			entry is obtained.  If the data comes from observation, the source is yourself (or
			spouse or colleague).  For conversations or interviews, the person who gave the
			information is the source.  Ideally, you should maintain a biographical sketch of
			each of these persons, including age, sex, marital status, kinship group, place of
			birth and childhood, social standing, etc. in a biographical Shoebox database.  In
			this biographical database this \srce field marker could be used as the record
			marker.  If the data comes from a book or article, cite the author.  You might want
			to maintain a bibliographical database in Shoebox as well.	*/
	sfm.Clear();
	sfm.m_stabsMkr = "srce";
	sfm.m_stabsNam = "Source";
	sfm.m_stabsMkrOverThis = "date";
	sfm.m_flidDst = kflidRnEvent_Sources;
	m_vsfm.Push(sfm);
	m_hmcisfm.Insert(sfm.m_stabsMkr.Chars(), ++isfm);
/*	\part	Participants in the data gathering.  This refers primarily to whom else was present
			during a conversation or interview, though it may be relevant for some observations
			as well.  The point is that people often adjust their information to fit their
			audience, and certain information may not be shared or shared differently when
			certain people are present.  This is one place where kinship data can be important
			(e.g. certain topics are not discussed before certain relatives).  Record the names
			of those present, were involved in the conversation, or listened to the interview
			in this field.  (See the discussion of Source for comments on biographical data.) */
	sfm.Clear();
	sfm.m_stabsMkr = "part";
	sfm.m_stabsNam = "Participants";
	sfm.m_stabsMkrOverThis = "date";
	sfm.m_flidDst = kflidRnEvent_Participants;
	m_vsfm.Push(sfm);
	m_hmcisfm.Insert(sfm.m_stabsMkr.Chars(), ++isfm);
/*	\data	The actual data in a fully expanded form.  The data may be a single sentence, or a
			description that takes several pages.  Break long descriptions down into multiple
			entries a, b, c, etc. so that the \anth reference field will retrieve only the
			relevant chunk of data -- not the entire description.  Be as complete as memory,
			notes, and time allow.  What did you see, hear, understand, learn, etc.?  Often you
			will only take brief notes at the time, whether at a ceremony or during an
			interview.  Or you may only have time to scribble some notes down after the event or
			when the person giving the data has left.  The full description needs to be written
			down as soon as possible while your memory is fresh.  (See the description of the
			\de field.)	*/
	sfm.Clear();
	sfm.m_stabsMkr = "data";
	sfm.m_stabsNam = "The actual data";
	sfm.m_stabsMkrOverThis = "date";
	sfm.m_flidDst = kflidRnEvent_Description;
	m_vsfm.Push(sfm);
	m_hmcisfm.Insert(sfm.m_stabsMkr.Chars(), ++isfm);
/*	\anth	Anthropology codes.  Here go the key words, categories or cross-reference codes that
			catalog the data in the entry.  Many people use the Outline of Cultural Materials
			(OCM) codes -- often referred to as the HRAF codes -- for this field.  The OCM is a
			compilation of over 625 categories for organizing cultural data and is very useful
			for guiding research and avoiding "holes" in your data.  OCM is recommended;
			certainly worth learning how to use, but each of us will probably find the need to
			add certain categories of our own.  Shoebox can easily handle this.	*/
	sfm.Clear();
	sfm.m_stabsMkr = "anth";
	sfm.m_stabsNam = "Anthropology categories";
	sfm.m_stabsMkrOverThis = "date";
	sfm.m_flidDst = kflidRnGenericRec_AnthroCodes;
	m_vsfm.Push(sfm);
	m_hmcisfm.Insert(sfm.m_stabsMkr.Chars(), ++isfm);
/*	\mtrl	Related materials not on the computer.  The field should be used to catalog
			information such as the location of photographs, drawings, audio and video
			recordings, artifacts, published and unpublished material on the current topic,
			etc.	*/
	sfm.Clear();
	sfm.m_stabsMkr = "mtrl";
	sfm.m_stabsNam = "Related materials";
	sfm.m_stabsMkrOverThis = "date";
	sfm.m_flidDst = kflidRnGenericRec_ExternalMaterials;
	m_vsfm.Push(sfm);
	m_hmcisfm.Insert(sfm.m_stabsMkr.Chars(), ++isfm);
/*	\hypo	Hypothesis by the researcher.  The data may suggest certain hypotheses that need to
			be checked out.  For example, the data may suggest that "Property is owned by men,
			but domestic animals are owned by women" (true among some groups in Brazil); or
			"Only men can have personal fetishes" (true for some groups in Africa).  Once a
			hypothesis is stated, you can be on the lookout for other data that either supports
			or refutes the hypothesis.	*/
	sfm.Clear();
	sfm.m_stabsMkr = "hypo";
	sfm.m_stabsNam = "Hypothesis by the researcher";
	sfm.m_stabsMkrOverThis = "date";
	sfm.m_flidDst = kflidRnAnalysis_Hypothesis;
	m_vsfm.Push(sfm);
	m_hmcisfm.Insert(sfm.m_stabsMkr.Chars(), ++isfm);
/*	\q		Questions for further investigation.  Often as we write up the data we find that
			there is certain other information that we need, e.g. if the entry is discussing the
			market, possible questions might be, "What do the men sell?  What do women sell?
			Do outsiders sell anything not sold by local people?"  This field becomes a list of
			data to be collected later.	*/
	sfm.Clear();
	sfm.m_stabsMkr = "q";
	sfm.m_stabsNam = "Questions to investigate";
	sfm.m_stabsMkrOverThis = "date";
	sfm.m_flidDst = kflidRnGenericRec_FurtherQuestions;
	m_vsfm.Push(sfm);
	m_hmcisfm.Insert(sfm.m_stabsMkr.Chars(), ++isfm);
/*	\cf		Confer, compare.  Other relevant entries can be cross-referenced by placing the Date
			of the other entry in this field.  Using the Jump feature on this reference will
			display the other entry in another window.  Generally used only for entries that you
			want to keep closely related.	*/
	sfm.Clear();
	sfm.m_stabsMkr = "cf";
	sfm.m_stabsNam = "Confer, compare";
	sfm.m_stabsMkrOverThis = "date";
	sfm.m_flidDst = kflidRnAnalysis_SupportingEvidence;
	m_vsfm.Push(sfm);
	m_hmcisfm.Insert(sfm.m_stabsMkr.Chars(), ++isfm);
/*	\nt		Notes.  Place any notes or comments pertaining to the entry here.  This may be used
			to qualify the data.	*/
	sfm.Clear();
	sfm.m_stabsMkr = "nt";
	sfm.m_stabsNam = "Notes";
	sfm.m_stabsMkrOverThis = "date";
	sfm.m_flidDst = kflidRnEvent_PersonalNotes;
	m_vsfm.Push(sfm);
	m_hmcisfm.Insert(sfm.m_stabsMkr.Chars(), ++isfm);
/*	\dt		Date last edited.  This field retains the date the journal entry was last edited.
			Shoebox can date stamp this field automatically for you through the Datestamp
			feature.	*/
	sfm.Clear();
	sfm.m_stabsMkr = "dt";
	sfm.m_stabsNam = "Date last edited";
	sfm.m_stabsMkrOverThis = "date";
	sfm.m_flidDst = kflidRnGenericRec_DateModified;
	m_vsfm.Push(sfm);
	m_hmcisfm.Insert(sfm.m_stabsMkr.Chars(), ++isfm);
}

/*----------------------------------------------------------------------------------------------
	Copy the settings from another instance of this class.

	@param pshbx Pointer to another RnShoeboxSettings instance.
----------------------------------------------------------------------------------------------*/
void RnShoeboxSettings::Copy(RnShoeboxSettings * pshbx)
{
	Assert(pshbx != this);

	m_strSettingsName = pshbx->m_strSettingsName;
	m_vsfm = pshbx->m_vsfm;
	m_isfmRecord = pshbx->m_isfmRecord;
	m_hmcisfm.Clear();
	if (pshbx->m_hmcisfm.Size())
	{
		HashMapChars<int>::iterator it;
		for (it = pshbx->m_hmcisfm.Begin(); it != pshbx->m_hmcisfm.End(); ++it)
			m_hmcisfm.Insert(it.GetKey(), it.GetValue());
	}
	m_vrcmp = pshbx->m_vrcmp;
	m_hmsaicmp.Clear();
	if (pshbx->m_hmsaicmp.Size())
	{
		HashMapChars<int>::iterator it;
		for (it = pshbx->m_hmsaicmp.Begin(); it != pshbx->m_hmsaicmp.End(); ++it)
			m_hmsaicmp.Insert(it.GetKey(), it.GetValue());
	}
	m_strDbFile = pshbx->m_strDbFile;
	m_strPrjFile = pshbx->m_strPrjFile;
}

/*----------------------------------------------------------------------------------------------
	Compare two RnShoeboxSettings objects for having the same content.

	@param pshbx Pointer to another RnShoeboxSettings instance.

	@return True if the two objects are equivalent, otherwise false.
----------------------------------------------------------------------------------------------*/
bool RnShoeboxSettings::Equals(RnShoeboxSettings * pshbx)
{
	if (pshbx == this)
		return true;
	if (m_strSettingsName != pshbx->m_strSettingsName)
		return false;
	if (m_vsfm.Size() != pshbx->m_vsfm.Size())
		return false;
	if (m_isfmRecord != pshbx->m_isfmRecord)
		return false;
	if (m_vrcmp.Size() != pshbx->m_vrcmp.Size())
		return false;
	if (m_strDbFile != pshbx->m_strDbFile)
		return false;
	if (m_strPrjFile != pshbx->m_strPrjFile)
		return false;

	int isfm;
	int ista;
	int istr;
	for (isfm = 0; isfm < m_vsfm.Size(); ++isfm)
	{
		if (m_vsfm[isfm].m_stabsMkr != pshbx->m_vsfm[isfm].m_stabsMkr)
			return false;
		if (m_vsfm[isfm].m_stabsNam != pshbx->m_vsfm[isfm].m_stabsNam)
			return false;
		if (m_vsfm[isfm].m_stabsLng != pshbx->m_vsfm[isfm].m_stabsLng)
			return false;
		if (m_vsfm[isfm].m_stabsMkrOverThis != pshbx->m_vsfm[isfm].m_stabsMkrOverThis)
			return false;
		if (m_vsfm[isfm].m_rt != pshbx->m_vsfm[isfm].m_rt)
			return false;
		if (m_vsfm[isfm].m_nLevel != pshbx->m_vsfm[isfm].m_nLevel)
			return false;
		if (m_vsfm[isfm].m_flidDst != pshbx->m_vsfm[isfm].m_flidDst)
			return false;
		if (m_vsfm[isfm].m_fIgnoreEmpty != pshbx->m_vsfm[isfm].m_fIgnoreEmpty)
			return false;
		if (m_vsfm[isfm].m_txo.m_stuStyle != pshbx->m_vsfm[isfm].m_txo.m_stuStyle)
			return false;
		if (m_vsfm[isfm].m_txo.m_fStartParaNewLine !=
			pshbx->m_vsfm[isfm].m_txo.m_fStartParaNewLine)
		{
			return false;
		}
		if (m_vsfm[isfm].m_txo.m_fStartParaBlankLine !=
			pshbx->m_vsfm[isfm].m_txo.m_fStartParaBlankLine)
		{
			return false;
		}
		if (m_vsfm[isfm].m_txo.m_fStartParaIndented !=
			pshbx->m_vsfm[isfm].m_txo.m_fStartParaIndented)
		{
			return false;
		}
		if (m_vsfm[isfm].m_txo.m_fStartParaShortLine !=
			pshbx->m_vsfm[isfm].m_txo.m_fStartParaShortLine)
		{
			return false;
		}
		if (m_vsfm[isfm].m_txo.m_fStartParaShortLine &&
			m_vsfm[isfm].m_txo.m_cchShortLim != pshbx->m_vsfm[isfm].m_txo.m_cchShortLim)
		{
			return false;
		}
		if (m_vsfm[isfm].m_txo.m_ws != pshbx->m_vsfm[isfm].m_txo.m_ws)
			return false;
		if (m_vsfm[isfm].m_clo.m_fHaveMulti != pshbx->m_vsfm[isfm].m_clo.m_fHaveMulti)
			return false;
		if (m_vsfm[isfm].m_clo.m_staDelimMulti != pshbx->m_vsfm[isfm].m_clo.m_staDelimMulti)
			return false;
		if (m_vsfm[isfm].m_clo.m_fHaveSub != pshbx->m_vsfm[isfm].m_clo.m_fHaveSub)
			return false;
		if (m_vsfm[isfm].m_clo.m_staDelimSub != pshbx->m_vsfm[isfm].m_clo.m_staDelimSub)
			return false;
		if (m_vsfm[isfm].m_clo.m_fHaveBetween != pshbx->m_vsfm[isfm].m_clo.m_fHaveBetween)
			return false;
		if (m_vsfm[isfm].m_clo.m_staMarkStart != pshbx->m_vsfm[isfm].m_clo.m_staMarkStart)
			return false;
		if (m_vsfm[isfm].m_clo.m_staMarkEnd != pshbx->m_vsfm[isfm].m_clo.m_staMarkEnd)
			return false;
		if (m_vsfm[isfm].m_clo.m_fHaveBefore != pshbx->m_vsfm[isfm].m_clo.m_fHaveBefore)
			return false;
		if (m_vsfm[isfm].m_clo.m_staBefore != pshbx->m_vsfm[isfm].m_clo.m_staBefore)
			return false;
		if (m_vsfm[isfm].m_clo.m_fIgnoreNewStuff != pshbx->m_vsfm[isfm].m_clo.m_fIgnoreNewStuff)
			return false;
		if (m_vsfm[isfm].m_clo.m_vstaMatch.Size() !=
			pshbx->m_vsfm[isfm].m_clo.m_vstaMatch.Size())
		{
			return false;
		}
		if (m_vsfm[isfm].m_clo.m_vstaReplace.Size() !=
			pshbx->m_vsfm[isfm].m_clo.m_vstaReplace.Size())
		{
			return false;
		}
		if (m_vsfm[isfm].m_clo.m_staEmptyDefault != pshbx->m_vsfm[isfm].m_clo.m_staEmptyDefault)
			return false;
		if (m_vsfm[isfm].m_clo.m_pnt != pshbx->m_vsfm[isfm].m_clo.m_pnt)
			return false;
		for (ista = 0; ista < m_vsfm[isfm].m_clo.m_vstaMatch.Size(); ++ista)
		{
			if (m_vsfm[isfm].m_clo.m_vstaMatch[ista] !=
				pshbx->m_vsfm[isfm].m_clo.m_vstaMatch[ista])
			{
				return false;
			}
		}
		for (ista = 0; ista < m_vsfm[isfm].m_clo.m_vstaReplace.Size(); ++ista)
		{
			if (m_vsfm[isfm].m_clo.m_vstaReplace[ista] !=
				pshbx->m_vsfm[isfm].m_clo.m_vstaReplace[ista])
			{
				return false;
			}
		}
		if (m_vsfm[isfm].m_dto.m_vstaFmt.Size() != pshbx->m_vsfm[isfm].m_dto.m_vstaFmt.Size())
			return false;
		for (istr = 0; istr < m_vsfm[isfm].m_dto.m_vstaFmt.Size(); ++istr)
		{
			if (m_vsfm[isfm].m_dto.m_vstaFmt[istr] != pshbx->m_vsfm[isfm].m_dto.m_vstaFmt[istr])
				return false;
		}
		if (m_vsfm[isfm].m_mlo.m_ws != pshbx->m_vsfm[isfm].m_mlo.m_ws)
			return false;
	}
	// This is merely paranoid: if the vectors are identical, the hashmaps should be as well!
	if (m_hmcisfm.Size() != pshbx->m_hmcisfm.Size())
		return false;

	int icmp;
	for (icmp = 0; icmp < m_vrcmp.Size(); ++icmp)
	{
		if (m_vrcmp[icmp].m_staBegin != pshbx->m_vrcmp[icmp].m_staBegin)
			return false;
		if (m_vrcmp[icmp].m_staEnd != pshbx->m_vrcmp[icmp].m_staEnd)
			return false;
		if (m_vrcmp[icmp].m_cmt != pshbx->m_vrcmp[icmp].m_cmt)
			return false;
		if (m_vrcmp[icmp].m_strOldWritingSystem != pshbx->m_vrcmp[icmp].m_strOldWritingSystem)
			return false;
		if (m_vrcmp[icmp].m_stuCharStyle != pshbx->m_vrcmp[icmp].m_stuCharStyle)
			return false;
		if (m_vrcmp[icmp].m_tptDirect != pshbx->m_vrcmp[icmp].m_tptDirect)
			return false;
	}
	// This is merely paranoid: if the vectors are identical, the hashmaps should be as well!
	if (m_hmsaicmp.Size() != pshbx->m_hmsaicmp.Size())
		return false;

	return true;
}

/*----------------------------------------------------------------------------------------------
	Write the contents of these settings to the file.

	@param fp C-style FILE pointer
----------------------------------------------------------------------------------------------*/
void RnShoeboxSettings::Serialize(FILE * fp, bool fSavePrjFile, ILgWritingSystemFactory * pwsf)
{
	AssertPtr(fp);
	AssertPtr(pwsf);
	StrAnsi sta;
	sta.Assign(m_strSettingsName.Chars(), m_strSettingsName.Length());
	fprintf(fp, "\\ShoeboxImportSettings %s\n", sta.Chars());
	int isfm;
	int ista;
	int istr;
	SmartBstr sbstr;
	for (isfm = 0; isfm < m_vsfm.Size(); ++isfm)
	{
		fprintf(fp, "\\Marker %s\n", m_vsfm[isfm].m_stabsMkr.Chars());
		if (m_vsfm[isfm].m_stabsNam.Length())
			fprintf(fp, "\\MkrNam %s\n", m_vsfm[isfm].m_stabsNam.Chars());
		if (m_vsfm[isfm].m_stabsLng.Length())
			fprintf(fp, "\\MkrLng %s\n", m_vsfm[isfm].m_stabsLng.Chars());
		if (m_vsfm[isfm].m_stabsMkrOverThis.Length())
			fprintf(fp, "\\MkrOver %s\n", m_vsfm[isfm].m_stabsMkrOverThis.Chars());
		switch (m_vsfm[isfm].m_rt)
		{
		case RnSfMarker::krtEvent:
			fprintf(fp, "\\Event %d\n", m_vsfm[isfm].m_nLevel);
			if (m_vsfm[isfm].m_nLevel == 1)
			{
				Assert(isfm == m_isfmRecord);
			}
			break;
		case RnSfMarker::krtAnalysis:
			fprintf(fp, "\\Analysis %d\n", m_vsfm[isfm].m_nLevel);
			break;
		}
		fprintf(fp, "\\Flid %d\n", m_vsfm[isfm].m_flidDst);
		if (m_vsfm[isfm].m_fIgnoreEmpty)
			fprintf(fp, "\\IgnoreEmptyField\n");
		if (m_vsfm[isfm].m_txo.m_stuStyle.Length())
		{
			sta.Assign(m_vsfm[isfm].m_txo.m_stuStyle.Chars(),
				m_vsfm[isfm].m_txo.m_stuStyle.Length());
			fprintf(fp, "\\TextStyle %s\n", sta.Chars());
		}
		if (m_vsfm[isfm].m_txo.m_fStartParaNewLine)
		{
			fprintf(fp, "\\StartParaForEachLine\n");
		}
		if (m_vsfm[isfm].m_txo.m_fStartParaBlankLine)
		{
			fprintf(fp, "\\StartParaAfterBlankLine\n");
		}
		if (m_vsfm[isfm].m_txo.m_fStartParaIndented)
		{
			fprintf(fp, "\\StartParaForIndentedLine\n");
		}
		if (m_vsfm[isfm].m_txo.m_fStartParaShortLine)
		{
			fprintf(fp, "\\StartParaForShortLine %d\n", m_vsfm[isfm].m_txo.m_cchShortLim);
		}
		if (m_vsfm[isfm].m_txo.m_ws)
		{
			CheckHr(pwsf->GetStrFromWs(m_vsfm[isfm].m_txo.m_ws, &sbstr));
			sta.Assign(sbstr.Chars(), sbstr.Length());
			fprintf(fp, "\\DefaultParaWrtSys %s\n", sta.Chars());
		}
		if (m_vsfm[isfm].m_clo.m_fHaveMulti)
			fprintf(fp, "\\MultiChoice %s\n", m_vsfm[isfm].m_clo.m_staDelimMulti.Chars());
		if (m_vsfm[isfm].m_clo.m_fHaveSub)
			fprintf(fp, "\\SubChoice %s\n", m_vsfm[isfm].m_clo.m_staDelimSub.Chars());
		if (m_vsfm[isfm].m_clo.m_fHaveBetween)
		{
			fprintf(fp, "\\StartChoice %s\n", m_vsfm[isfm].m_clo.m_staMarkStart.Chars());
			fprintf(fp, "\\EndChoice %s\n", m_vsfm[isfm].m_clo.m_staMarkEnd.Chars());
		}
		if (m_vsfm[isfm].m_clo.m_fHaveBefore)
			fprintf(fp, "\\StopChoices %s\n", m_vsfm[isfm].m_clo.m_staBefore.Chars());
		if (m_vsfm[isfm].m_clo.m_fIgnoreNewStuff)
			fprintf(fp, "\\IgnoreNewChoices\n");
		else if (m_vsfm[isfm].m_clo.m_staEmptyDefault.Length())
			fprintf(fp, "\\EmptyChoiceIs %s\n", m_vsfm[isfm].m_clo.m_staEmptyDefault.Chars());
		if (m_vsfm[isfm].m_clo.m_pnt == kpntAbbreviation)
			fprintf(fp, "\\MatchChoice Abbreviation\n");
		else
			fprintf(fp, "\\MatchChoice Name\n");
		Assert(m_vsfm[isfm].m_clo.m_vstaMatch.Size() ==
			m_vsfm[isfm].m_clo.m_vstaReplace.Size());
		for (ista = 0; ista < m_vsfm[isfm].m_clo.m_vstaMatch.Size(); ++ista)
		{
			fprintf(fp, "\\MatchChoice %s\n", m_vsfm[isfm].m_clo.m_vstaMatch[ista].Chars());
			fprintf(fp, "\\ReplaceChoice %s\n", m_vsfm[isfm].m_clo.m_vstaReplace[ista].Chars());
		}
		for (istr = 0; istr < m_vsfm[isfm].m_dto.m_vstaFmt.Size(); ++istr)
			fprintf(fp, "\\DateFormat %s\n", m_vsfm[isfm].m_dto.m_vstaFmt[istr].Chars());
		if (m_vsfm[isfm].m_mlo.m_ws)
		{
			CheckHr(pwsf->GetStrFromWs(m_vsfm[isfm].m_mlo.m_ws, &sbstr));
			sta.Assign(sbstr.Chars(), sbstr.Length());
			fprintf(fp, "\\MultiLingualAltWrtSys %s\n", sta.Chars());
		}
		fprintf(fp, "\\-Marker\n");
	}
	int icmp;
	for (icmp = 0; icmp < m_vrcmp.Size(); ++icmp)
	{
		fprintf(fp, "\\CharMapping\n");
		sta = m_vrcmp[icmp].m_staBegin.Chars();
		fprintf(fp, "\\BeginMap %s\n", sta.Chars());
		sta = m_vrcmp[icmp].m_staEnd.Chars();
		fprintf(fp, "\\EndMap %s\n", sta.Chars());
		switch (m_vrcmp[icmp].m_cmt)
		{
		case RnImportCharMapping::kcmtDirectFormatting:
			fprintf(fp, "\\DirectFmt %s\n",
				m_vrcmp[icmp].m_tptDirect == ktptItalic ? "Italic" : "Bold");
			break;
		case RnImportCharMapping::kcmtCharStyle:
			sta.Assign(m_vrcmp[icmp].m_stuCharStyle.Chars(),
				m_vrcmp[icmp].m_stuCharStyle.Length());
			fprintf(fp, "\\CharStyle %s\n", sta.Chars());
			break;
		case RnImportCharMapping::kcmtOldWritingSystem:
			sta.Assign(m_vrcmp[icmp].m_strOldWritingSystem.Chars(),
				m_vrcmp[icmp].m_strOldWritingSystem.Length());
			fprintf(fp, "\\OldWritingSystem %s\n", sta.Chars());
			break;
		}
		fprintf(fp, "\\-CharMapping\n");
	}
	if (m_strDbFile.Length())
	{
		sta.Assign(m_strDbFile);
		fprintf(fp, "\\DbFile %s\n", sta.Chars());
	}
	if (fSavePrjFile && m_strPrjFile.Length())
	{
		sta.Assign(m_strPrjFile);
		fprintf(fp, "\\PrjFile %s\n", sta.Chars());
	}
	time_t ntime;
	time(&ntime);
	char buf[26];
	ctime_s(buf, SizeOfArray(buf), &ntime);
	fprintf(fp, "\\-ShoeboxImportSettings %s\n", buf);
}

/*----------------------------------------------------------------------------------------------
	Convert a writing system string to its code integer (database id in practice)

	@param pwsf Pointer to a writing system factory.
	@param pszWs Pointer to a string that begins with a writing system's ICU Locale name.

	@return The writing system id number, or zero if it didn't work.
----------------------------------------------------------------------------------------------*/
static int GetWsFromStr(ILgWritingSystemFactory * pwsf, const char * pszWs)
{
	AssertPtr(pwsf);
	AssertPsz(pszWs);

	int cch;
	const char * pszEnd = strpbrk(pszWs, "\r\n");
	if (pszEnd)
		cch = pszEnd - pszWs;
	else
		cch = strlen(pszWs);
	StrUni stu(pszWs, cch);
	int ws;
	pwsf->GetWsFromStr(stu.Bstr(), &ws);
	return ws;
}

/*----------------------------------------------------------------------------------------------
	Initialize these settings from the string (which has presumably been read from a file).

	@param pszSettings Pointer to a NUL-terminated string that contains a set of Shoebox import
			settings at the beginning.
	@param pwsf Pointer to a writing system factory

	@return Pointer to the line following the end of the settings, or NULL if an error occurs.
----------------------------------------------------------------------------------------------*/
char * RnShoeboxSettings::Deserialize(char * pszSettings, ILgWritingSystemFactory * pwsf)
{
	char * pszLine = pszSettings;
	char * pszNext;
	bool fInSettings = false;
	bool fInMarker = false;
	bool fInCharMapping = false;
	RnSfMarker sfm;
	RnImportCharMapping rcmp;
	StrAnsi sta;
	StrAnsi staMatch;
	StrApp str;
	m_isfmRecord = 0;
	char ch;
	char * psz;
	do
	{
		psz = strpbrk(pszLine, "\r\n");
		if (psz)
		{
			ch = *psz;
			pszNext = psz;
			*pszNext++ = 0;
			pszNext += strspn(pszNext, "\r\n");
		}
		else
		{
			ch = 0;
			pszNext = pszLine + strlen(pszLine);
		}
		if (!fInSettings)
		{
			if (strncmp(pszLine, "\\ShoeboxImportSettings ", 23) == 0)
			{
				m_strSettingsName = pszLine + 23;
				fInSettings = true;
			}
			pszLine = pszNext;
			continue;
		}
		if (fInMarker)
		{
			if (strcmp(pszLine, "\\-Marker") == 0)
			{
				int isfm = m_vsfm.Size();
				m_hmcisfm.Insert(sfm.m_stabsMkr.Chars(), isfm);
				m_vsfm.Push(sfm);
				sfm.Clear();
				fInMarker = false;
			}
			else if (strncmp(pszLine, "\\MkrNam ", 8) == 0)
			{
				sfm.m_stabsNam = pszLine + 8;
			}
			else if (strncmp(pszLine, "\\MkrLng ", 8) == 0)
			{
				sfm.m_stabsLng = pszLine + 8;
			}
			else if (strncmp(pszLine, "\\MkrOver ", 9) == 0)
			{
				sfm.m_stabsMkrOverThis = pszLine + 9;
			}
			else if (strncmp(pszLine, "\\Event ", 7) == 0)
			{
				sfm.m_rt = RnSfMarker::krtEvent;
				sfm.m_nLevel = (int)strtol(pszLine + 7, NULL, 10);
				if (sfm.m_nLevel == 1)
					m_isfmRecord = m_vsfm.Size();
			}
			else if (strncmp(pszLine, "\\Analysis ", 10) == 0)
			{
				sfm.m_rt = RnSfMarker::krtAnalysis;
				sfm.m_nLevel = (int)strtol(pszLine + 10, NULL, 10);
			}
			else if (strncmp(pszLine, "\\Flid ", 6) == 0)
			{
				sfm.m_flidDst = (int)strtol(pszLine + 6, NULL, 10);
			}
			else if (strcmp(pszLine, "\\IgnoreEmptyField") == 0)
			{
				sfm.m_fIgnoreEmpty = true;
			}
			else if (strncmp(pszLine, "\\TextStyle ", 11) == 0)
			{
				sfm.m_txo.m_stuStyle = pszLine + 11;
			}
			else if (strcmp(pszLine, "\\StartParaForEachLine") == 0)
			{
				sfm.m_txo.m_fStartParaNewLine = true;
			}
			else if (strcmp(pszLine, "\\StartParaAfterBlankLine") == 0)
			{
				sfm.m_txo.m_fStartParaBlankLine = true;
			}
			else if (strcmp(pszLine, "\\StartParaForIndentedLine") == 0)
			{
				sfm.m_txo.m_fStartParaIndented = true;
			}
			else if (strncmp(pszLine, "\\StartParaForShortLine ", 23) == 0)
			{
				sfm.m_txo.m_fStartParaShortLine = true;
				sfm.m_txo.m_cchShortLim = (int)strtol(pszLine + 23, NULL, 10);
			}
			else if (strncmp(pszLine, "\\DefaultParaWrtSys ", 19) == 0)
			{
				sfm.m_txo.m_ws = GetWsFromStr(pwsf, pszLine + 19);
			}
			else if (strncmp(pszLine, "\\MultiChoice ", 13) == 0)
			{
				sfm.m_clo.m_fHaveMulti = true;
				sfm.m_clo.m_staDelimMulti = pszLine + 13;
			}
			else if (strncmp(pszLine, "\\SubChoice ", 11) == 0)
			{
				sfm.m_clo.m_fHaveSub = true;
				sfm.m_clo.m_staDelimSub = pszLine + 11;
			}
			else if (strncmp(pszLine, "\\StartChoice ", 13) == 0)
			{
				sfm.m_clo.m_fHaveBetween = true;
				sfm.m_clo.m_staMarkStart = pszLine + 13;
			}
			else if (strncmp(pszLine, "\\EndChoice ", 11) == 0)
			{
				if (sfm.m_clo.m_fHaveBetween)
					sfm.m_clo.m_staMarkEnd = pszLine + 11;
			}
			else if (strncmp(pszLine, "\\StopChoices ", 13) == 0)
			{
				sfm.m_clo.m_fHaveBefore = true;
				sfm.m_clo.m_staBefore = pszLine + 13;
			}
			else if (strcmp(pszLine, "\\IgnoreNewChoices") == 0)
			{
				sfm.m_clo.m_fIgnoreNewStuff = true;
			}
			else if (strncmp(pszLine, "\\EmptyChoiceIs ", 15) == 0)
			{
				sfm.m_clo.m_staEmptyDefault = pszLine + 15;
			}
			else if (strcmp(pszLine, "\\MatchChoice Abbreviation") == 0)
			{
				sfm.m_clo.m_pnt = kpntAbbreviation;
			}
			else if (strcmp(pszLine, "\\MatchChoice Name") == 0)
			{
				sfm.m_clo.m_pnt = kpntName;
			}
			else if (strncmp(pszLine, "\\MatchChoice ", 13) == 0)
			{
				staMatch = pszLine + 13;
			}
			else if (staMatch.Length() && strcmp(pszLine, "\\ReplaceChoice") == 0)
			{
				sta = "";
				sfm.m_clo.m_vstaMatch.Push(staMatch);
				sfm.m_clo.m_vstaReplace.Push(sta);
				staMatch.Clear();
			}
			else if (staMatch.Length() && strncmp(pszLine, "\\ReplaceChoice ", 15) == 0)
			{
				sta = pszLine + 15;
				sfm.m_clo.m_vstaMatch.Push(staMatch);
				sfm.m_clo.m_vstaReplace.Push(sta);
				staMatch.Clear();
			}
			else if (strncmp(pszLine, "\\DateFormat ", 12) == 0)
			{
				sta = pszLine + 12;
				sfm.m_dto.m_vstaFmt.Push(sta);
			}
			else if (strncmp(pszLine, "\\MultiLingualAltWrtSys ", 23) == 0)
			{
				sfm.m_mlo.m_ws = GetWsFromStr(pwsf, pszLine + 23);
			}
		}
		else if (fInCharMapping)
		{
			if (strcmp(pszLine, "\\-CharMapping") == 0)
			{
				int icmp = m_vrcmp.Size();
				m_hmsaicmp.Insert(rcmp.m_staBegin, icmp);
				m_vrcmp.Push(rcmp);
				fInCharMapping = false;
			}
			else if (strncmp(pszLine, "\\BeginMap ", 10) == 0)
			{
				rcmp.m_staBegin = pszLine + 10;
			}
			else if (strncmp(pszLine, "\\EndMap ", 8) == 0)
			{
				rcmp.m_staEnd = pszLine + 8;
			}
			else if (strcmp(pszLine, "\\DirectFmt Italic") == 0)
			{
				rcmp.m_cmt = RnImportCharMapping::kcmtDirectFormatting;
				rcmp.m_tptDirect = ktptItalic;
			}
			else if (strcmp(pszLine, "\\DirectFmt Bold") == 0)
			{
				rcmp.m_cmt = RnImportCharMapping::kcmtDirectFormatting;
				rcmp.m_tptDirect = ktptBold;
			}
			else if (strncmp(pszLine, "\\CharStyle ", 11) == 0)
			{
				rcmp.m_cmt = RnImportCharMapping::kcmtCharStyle;
				rcmp.m_stuCharStyle = pszLine + 11;
			}
			else if (strncmp(pszLine, "\\OldWritingSystem ", 15) == 0)
			{
				rcmp.m_cmt = RnImportCharMapping::kcmtOldWritingSystem;
				rcmp.m_strOldWritingSystem = pszLine + 15;
			}
		}
		else
		{
			if (strncmp(pszLine, "\\Marker ", 8) == 0)
			{
				sfm.Clear();
				sfm.m_txo.m_fStartParaBlankLine = false;	// Clear() sets to true.
				sfm.m_txo.m_fStartParaIndented = false;		// Clear() sets to true.
				sfm.m_txo.m_cchShortLim = 0;				// Clear() sets to 60.
				sfm.m_stabsMkr = pszLine + 8;
				fInMarker = true;
			}
			else if (strcmp(pszLine, "\\CharMapping") == 0)
			{
				rcmp.Clear();
				fInCharMapping = true;
			}
			else if (strncmp(pszLine, "\\DbFile ", 8) == 0)
			{
				sta = pszLine + 8;
				m_strDbFile.Assign(sta);
			}
			else if (strncmp(pszLine, "\\PrjFile ", 9) == 0)
			{
				sta = pszLine + 9;
				m_strPrjFile.Assign(sta);
			}
		}
		pszLine = pszNext;
	} while (*pszLine && strncmp(pszLine, "\\-ShoeboxImportSettings", 23) != 0);
	pszNext = strpbrk(pszLine, "\r\n");
	if (pszNext)
		return pszNext + strspn(pszNext, "\r\n");
	else if (m_strSettingsName.Length() && m_vsfm.Size())
		return pszLine + strlen(pszLine);
	else
		return NULL;
}


//:>********************************************************************************************
//:>	RnImportWizard methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
RnImportWizard::RnImportWizard()
	: AfWizardDlg()
{
	m_imdst = kidstAppend;
}

/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
RnImportWizard::~RnImportWizard()
{
	for (int i = 0; i < m_vpshbxStored.Size(); ++i)
		delete m_vpshbxStored[i];
}

/*----------------------------------------------------------------------------------------------
	Initialize the dialog in response to the WM_INITDIALOG message.
	All one-time initialization should be done here (that is, all controls have been created
	and have valid hwnd's, but they need initial values.)

	@param hwndCtrl Not used by this method.
	@param lp Not used by this method.

	@return True.
----------------------------------------------------------------------------------------------*/
bool RnImportWizard::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	StrApp strTitle(kstidImportWizTitle);
	::SetWindowText(m_hwnd, strTitle.Chars());
	AddPage(NewObj RnImportWizOverview());
	AddPage(NewObj RnImportWizSettings());
	AddPage(NewObj RnImportWizSfmDb());
	AddPage(NewObj RnImportWizRecMarker());
	AddPage(NewObj RnImportWizFldMappings());
	AddPage(NewObj RnImportWizChrMappings());
	AddPage(NewObj RnImportWizCodeConverters());
	AddPage(NewObj RnImportWizSaveSet());
	AddPage(NewObj RnImportWizFinal());

	// Subclass the Help button.
	AfButtonPtr qbtn;
	qbtn.Create();
	qbtn->SubclassButton(m_hwnd, kctidHelp, kbtHelp, NULL, 0);

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}

/*----------------------------------------------------------------------------------------------
	Initialize the wizard data values.

	@param pafw Pointer to the application frame window (needed to get various global values).
	@param pwsf Pointer to the application's writing system factory.
----------------------------------------------------------------------------------------------*/
void RnImportWizard::Initialize(AfMainWnd * pafw, ILgWritingSystemFactory * pwsf)
{
	AssertPtr(pafw);
	AssertPtr(pwsf);

	m_pafw = pafw;
	m_qwsf = pwsf;
	CheckHr(pwsf->get_UserWs(&m_wsUser));
	LoadStoredSettings();

	// Initialize the import destinations.
	m_vridst.Clear();
	RnImportDest ridst;

	RnMainWnd * prnmw = dynamic_cast<RnMainWnd *>(m_pafw);
	AssertPtr(prnmw);
	AfDbInfo * pdbi = prnmw->GetLpInfo()->GetDbInfo();
	AssertPtr(pdbi);
	UserViewSpecVec & vuvs = pdbi->GetUserViewSpecs();
	IFwMetaDataCachePtr qmdc;
	pdbi->GetFwMetaDataCache(&qmdc);
	AssertPtr(qmdc);
	int iuvs;
	ClsLevel clevEvent(kclidRnEvent, 0);
	ClsLevel clevAnalysis(kclidRnAnalysis, 0);
	for (iuvs = 0; iuvs < vuvs.Size(); ++iuvs)
	{
		RecordSpecPtr qesp;
		if (vuvs[iuvs]->m_vwt == kvwtDE &&
			(vuvs[iuvs]->m_hmclevrsp.Retrieve(clevEvent, qesp) ||
			 vuvs[iuvs]->m_hmclevrsp.Retrieve(clevAnalysis, qesp)))
		{
			int ibsp;
			for (ibsp = 0; ibsp < qesp->m_vqbsp.Size(); ++ibsp)
			{
				BlockSpecPtr qbsp = qesp->m_vqbsp[ibsp];
				ridst.m_flid = qbsp->m_flid;
				if (ridst.m_flid == kflidRnGenericRec_SubRecords)
					continue;
				SmartBstr sbstr;
				qbsp->m_qtssLabel->get_Text(&sbstr);
				ridst.m_strName.Assign(sbstr.Chars(), sbstr.Length());
				unsigned long clid;
				CheckHr(qmdc->GetOwnClsId(ridst.m_flid, &clid));
				ridst.m_clidRecord = clid;
				CheckHr(qmdc->GetFieldType(ridst.m_flid, &ridst.m_proptype));
				CheckHr(qmdc->GetDstClsId(ridst.m_flid, &clid));
				ridst.m_clidDst = clid;
				if (ridst.m_clidDst == -1)
					ridst.m_clidDst = 0;
				ridst.m_hvoPssl = qbsp->m_hvoPssl;
				ridst.m_wsDefault = qbsp->m_ws;
				bool fAlready = false;
				for (int i = 0; i < m_vridst.Size(); ++i)
				{
					if (m_vridst[i].m_flid == ridst.m_flid)
					{
						Assert(m_vridst[i].m_clidRecord == ridst.m_clidRecord);
						Assert(m_vridst[i].m_clidDst == ridst.m_clidDst);
						Assert(m_vridst[i].m_proptype == ridst.m_proptype);
						Assert(m_vridst[i].m_hvoPssl == ridst.m_hvoPssl);
						Assert(m_vridst[i].m_wsDefault == ridst.m_wsDefault);
						if (m_vridst[i].m_strName.Length() >= ridst.m_strName.Length())
							fAlready = true;
						else
							m_vridst.Delete(i);
						break;
					}
				}
				if (!fAlready)
					m_vridst.Push(ridst);
			}
		}
	}
	LoadStyles();
	LoadWritingSystems();
}

/*----------------------------------------------------------------------------------------------
	Get the full pathname of the settings file, creating any needed directories along the way.
----------------------------------------------------------------------------------------------*/
static void GetSettingsFile(StrApp & strFile)
{
	// Get the FieldWorks root directory, first trying to find it in the registry, then using a
	// dumb default value.
	strFile.Assign(DirectoryFinder::FwRootDataDir());
	_tmkdir(strFile.Chars());
	if (strFile[strFile.Length() - 1] != '\\')
		strFile.Append("\\");
	strFile.Append(_T("UserSettings"));
	_tmkdir(strFile.Chars());
	strFile.Append(_T("\\ImportSettings.sfm"));
}


/*----------------------------------------------------------------------------------------------
	Load the various sets of stored Shoebox settings.
----------------------------------------------------------------------------------------------*/
void RnImportWizard::LoadStoredSettings()
{
	StrApp strFile;
	GetSettingsFile(strFile);
	FILE * fp;
	if (_tfopen_s(&fp, strFile.Chars(), _T("rb")))
		return;
	long cb = _filelength(_fileno(fp));
	Vector<char> vch;
	if (cb != -1)
	{
		vch.Resize(cb + 1);
		if (fread(vch.Begin(), cb, 1, fp) != 1)
			vch.Clear();
		else
			vch[cb] = 0;
	}
	if (vch.Size())
	{
		char * pszSettings = vch.Begin();
		char * pszNext;
		if (*pszSettings == '\\')
			pszSettings = strstr(pszSettings, "\\ShoeboxImportSettings ");
		else
		{
			pszSettings = strstr(pszSettings, "\n\\ShoeboxImportSettings ");
			if (pszSettings)
				++pszSettings;
		}
		while (pszSettings && *pszSettings)
		{
			RnShoeboxSettings * pshbx = NewObj RnShoeboxSettings();
			pszNext = pshbx->Deserialize(pszSettings, m_qwsf);
			if (pszNext)
			{
				if (!m_vpshbxStored.Size())
					m_shbx.Copy(pshbx);
				m_vpshbxStored.Push(pshbx);
				pszSettings = pszNext;
			}
			else
			{
				delete pshbx;
				break;
			}
		}
	}
	fclose(fp);
}

/*----------------------------------------------------------------------------------------------
	Store the various sets of stored Shoebox settings.
----------------------------------------------------------------------------------------------*/
void RnImportWizard::StoreSettings()
{
	int iset;
	bool fDirty = false;
	if (m_ishbxFrom != 0 || m_ishbxTo != 0)
		fDirty = true;
	else
		fDirty = !m_shbx.Equals(m_vpshbxStored[m_ishbxTo]);

	if (fDirty)
	{
		StrApp strFile;
		GetSettingsFile(strFile);
		FILE * fp;
		if (_tfopen_s(&fp, strFile.Chars(), _T("w")))
			return;
		fprintf(fp, "DO NOT EDIT THIS FILE!  YOU HAVE BEEN WARNED!\n"
			"The Fieldworks import process automatically maintains this file.\n");
		m_shbx.Serialize(fp, m_strTypFile.Length(), m_qwsf);
		for (iset = 0; iset < m_vpshbxStored.Size(); ++iset)
		{
			if (iset != m_ishbxTo)
				m_vpshbxStored[iset]->Serialize(fp, true, m_qwsf);
		}
		fclose(fp);
	}
}


/*----------------------------------------------------------------------------------------------
	Get the lists of defined paragraph and characters styles in the current language project.
----------------------------------------------------------------------------------------------*/
void RnImportWizard::LoadStyles()
{
	AssertPtr(m_pafw);
	m_vstuParaStyles.Clear();
	m_stuParaStyleDefault.Clear();
	m_vstuCharStyles.Clear();
	m_stuCharStyleDefault.Clear();
	m_hmsuhvoStyles.Clear();
	m_hmsuqttpStyles.Clear();

	AfStylesheet * pasts = m_pafw->GetStylesheet();
	AssertPtr(pasts);
	HvoClsidVec & vhcStyles = pasts->GetStyles();
	ISilDataAccessPtr qsda; // Pointer to SilDataAccess; used to get and set style properties.
	AfLpInfo * plpi = m_pafw->GetLpInfo();
	AssertPtr(plpi);
	plpi->GetAfStylesheet()->get_DataAccess(&qsda);
	int nType = -1; // Since values for nType begin with 0, initialize nType to -1.
	int iStyle;
	StrUni stu;
	int nStage;
	for (iStyle = 0; iStyle < vhcStyles.Size(); iStyle++)
	{
		nStage = 0;
		CheckHr(qsda->get_IntProp(vhcStyles[iStyle].hvo, kflidStStyle_Type, &nType));
		SmartBstr sbstrName;
		nStage = 1;
		CheckHr(qsda->get_UnicodeProp(vhcStyles[iStyle].hvo, kflidStStyle_Name, &sbstrName));

		nStage = 2;
		// Create a text property consisting only of a named style.
		ITsTextPropsPtr qttp;
		ITsPropsBldrPtr qtpb;
		qtpb.CreateInstance(CLSID_TsPropsBldr);
		CheckHr(qtpb->SetStrPropValue(kspNamedStyle, sbstrName));
		CheckHr(qtpb->GetTextProps(&qttp));

		nStage = 3;
		if (nType == kstParagraph)
		{
			if (!m_stuParaStyleDefault.Length())
				m_stuParaStyleDefault = sbstrName.Chars();
			stu = sbstrName.Chars();
			// Get the position of this style in the sorted list.
			int iv, ivLim;
			for (iv = 0, ivLim = m_vstuParaStyles.Size(); iv < ivLim; )
			{
				int ivMid = (iv + ivLim) / 2;
				if (_wcsicmp(m_vstuParaStyles[ivMid].Chars(), stu.Chars()) < 0)
					iv = ivMid + 1;
				else
					ivLim = ivMid;
			}
			m_vstuParaStyles.Insert(iv, stu);
		}
		else if (nType == kstCharacter)
		{
			if (!m_stuCharStyleDefault.Length())
				m_stuCharStyleDefault = sbstrName.Chars();
			stu = sbstrName.Chars();
			// Get the position of this style in the sorted list.
			int iv, ivLim;
			for (iv = 0, ivLim = m_vstuCharStyles.Size(); iv < ivLim; )
			{
				int ivMid = (iv + ivLim) / 2;
				if (_wcsicmp(m_vstuCharStyles[ivMid].Chars(), stu.Chars()) < 0)
					iv = ivMid + 1;
				else
					ivLim = ivMid;
			}
			m_vstuCharStyles.Insert(iv, stu);
		}
		else
		{
			continue;
		}
		nStage = 4;
		m_hmsuhvoStyles.Insert(stu, vhcStyles[iStyle].hvo);
		nStage = 5;
		m_hmsuqttpStyles.Insert(stu, qttp);
	}
	// Make sure that all stored paragraph styles are still valid.
	int idst;
	StrUni stuNormal(g_pszwStyleNormal);
	for (idst = 0; idst < m_shbx.FieldCodes().Size(); ++idst)
	{
		stu = m_shbx.FieldCodes()[idst].m_txo.m_stuStyle;
		if (!stu.Length())
			continue;
		int iv, ivLim;
		for (iv = 0, ivLim = m_vstuParaStyles.Size(); iv < ivLim; )
		{
			int ivMid = (iv + ivLim) / 2;
			if (_wcsicmp(m_vstuParaStyles[ivMid].Chars(), stu.Chars()) < 0)
				iv = ivMid + 1;
			else
				ivLim = ivMid;
		}
		if (m_vstuParaStyles[iv] != stu)		// Was old style deleted?
			m_shbx.FieldCodes()[idst].m_txo.m_stuStyle = stuNormal;
	}
}

/*----------------------------------------------------------------------------------------------
	Load one of our data structures with the relevant data from an IWritingSystem object.
----------------------------------------------------------------------------------------------*/
void RnImportWizard::LoadWrtSysData(IWritingSystem * pws, WrtSysData & wsd)
{
	AssertPtr(pws);

	SmartBstr sbstr;
	CheckHr(pws->get_WritingSystem(&wsd.m_ws));
	CheckHr(pws->get_Locale(&wsd.m_lcid));
	CheckHr(pws->get_UiName(m_wsUser, &sbstr));
	if (sbstr.Length())
		wsd.m_stuName.Assign(sbstr.Chars(), sbstr.Length());
	CheckHr(pws->get_DefaultSerif(&sbstr));
	if (sbstr)
		wsd.m_stuNormalFont.Assign(sbstr.Chars());
	else
		wsd.m_stuNormalFont.Clear();
	CheckHr(pws->get_DefaultSansSerif(&sbstr));
	if (sbstr)
		wsd.m_stuHeadingFont.Assign(sbstr.Chars());
	else
		wsd.m_stuHeadingFont.Clear();
	CheckHr(pws->get_DefaultBodyFont(&sbstr));
	if (sbstr)
		wsd.m_stuBodyFont.Assign(sbstr.Chars());
	else
		wsd.m_stuBodyFont.Clear();
	CheckHr(pws->get_LegacyMapping(&sbstr));
	if (sbstr)
		wsd.m_stuEncodingConverter.Assign(sbstr.Chars());
	else
		wsd.m_stuEncodingConverter.Clear();
	if (wsd.m_stuEncodingConverter.Length())
	{
		// Get the description from somewhere????
		// The best i can figure out is the file pathname...
		IEncConvertersPtr qencc;
		qencc.CreateInstance(L"SilEncConverters31.EncConverters");
		if (qencc)
		{
			IEncConverterPtr qcon;
			SmartVariant sv;
			sv.SetString(wsd.m_stuEncodingConverter.Chars());
			CheckHr(qencc->get_Item(sv, &qcon));
			Assert(qcon);
			if (qcon)
			{
				SmartBstr sbstr;
				CheckHr(qcon->get_ConverterIdentifier(&sbstr));
				wsd.m_stuEncodingConvDesc.Assign(sbstr.Chars());
			}
		}
	}
	else
	{
		wsd.m_stuEncodingConvDesc.Clear();
	}
}

/*----------------------------------------------------------------------------------------------
	Convert a "magic" ws to an actual wsSelector.
----------------------------------------------------------------------------------------------*/
int RnImportWizard::ActualWs(int wsSelector)
{
	Assert(wsSelector); // An writing system should always be present.
	switch (wsSelector)
	{
	case kwsAnals:
	case kwsAnal:
	case kwsAnalVerns:
		return m_wsAnal;
	case kwsVerns:
	case kwsVern:
	case kwsVernAnals:
		return m_wsVern;
	default:
		return wsSelector;
	}
}

/*----------------------------------------------------------------------------------------------
	Get the list of writing systems in the current language project.
----------------------------------------------------------------------------------------------*/
void RnImportWizard::LoadWritingSystems()
{
	AssertPtr(m_pafw);
	AssertPtr(m_qwsf.Ptr());

	if (!m_vwsd.Size())
	{
		AfLpInfo * plpi = m_pafw->GetLpInfo();
		AssertPtr(plpi);
		Vector<int> vwsProj;
		plpi->ProjectWritingSystems(vwsProj);
		WrtSysData wsd;
		IWritingSystemPtr qws;
		SmartBstr sbstr;
		for (int iws = 0; iws < vwsProj.Size(); ++iws)
		{
			CheckHr(m_qwsf->get_EngineOrNull(vwsProj[iws], &qws));
			if (qws)
			{
				RnImportWizard::LoadWrtSysData(qws, wsd);
				m_vwsd.Push(wsd);
			}
		}
	}
	Assert(m_vwsd.Size());
}

/*----------------------------------------------------------------------------------------------
	This struct stores the data associated with a single structured text field.

	@h3{Hungarian: std}
----------------------------------------------------------------------------------------------*/
struct StTextData
{
	HVO m_hvo;			// Database id of the structured text object.
	HVO m_hvoPara;		// Database id of the current structured text paragraph.
	int m_cPara;		// Count of paragraphs in the structured text object.
};

/*----------------------------------------------------------------------------------------------
	Get the encoding converter, if any, for the given writing system.

	@param wsd The writing system data.
	@param pencc Points to an IEncConverters object.
	@param ppcon Address of a pointer to the encoding converter.
----------------------------------------------------------------------------------------------*/
static void GetEncodingConverter(WrtSysData & wsd, IEncConverters * pencc,
	IEncConverter ** ppcon)
{
	*ppcon = NULL;
	if (wsd.m_stuEncodingConverter.Length())
	{
		SmartVariant sv;
		sv.SetString(wsd.m_stuEncodingConverter.Chars());
		HRESULT hr;
		IgnoreHr(hr = pencc->get_Item(sv, ppcon));
	}
}

/*----------------------------------------------------------------------------------------------
	Import the Shoebox database!

	@param plpi Pointer to the notebook language project information.
----------------------------------------------------------------------------------------------*/
void RnImportWizard::ImportShoeboxDatabase(RnLpInfo * plpi)
{
#if 2
	StrAnsiBuf stab;
	StrAnsiBuf stabTitle;
	stabTitle.Format("DEBUG - RnImportWizard::ImportShoeboxDatabase()");
#endif
	AssertPtr(plpi);
	AfDbInfo * pdbi = plpi->GetDbInfo();
	AssertPtr(pdbi);
	m_wsAnal = plpi->AnalWs();
	m_wsVern = plpi->VernWs();

	// Associate writing system names with writing system values for the character formatting
	// map.
	AssertPtr(m_qwsf.Ptr());
	int cwsObj;
	CheckHr(m_qwsf->get_NumberOfWs(&cwsObj));
	Vector<int> vws;
	vws.Resize(cwsObj);
	CheckHr(m_qwsf->GetWritingSystems(vws.Begin(), vws.Size()));
	SmartBstr sbstr;
	StrUni stu;
	IWritingSystemPtr qws;
	HashMapStrUni<int> hmsuws;
	for (int iws = 0; iws < vws.Size(); ++iws)
	{
		CheckHr(m_qwsf->get_EngineOrNull(vws[iws], &qws));
		Assert(qws.Ptr());
		if (!qws)
			continue;
		CheckHr(qws->get_Name(m_wsUser, &sbstr));
		if (!sbstr.Length())						// Ignore totally unnamed writing system.
			continue;
		stu.Assign(sbstr.Chars());
		hmsuws.Insert(stu, vws[iws], true);
	}
	int ws;
	for (int icmp = 0; icmp < m_shbx.CharMappings().Size(); ++icmp)
	{
		RnImportCharMapping & rcmp = m_shbx.CharMappings()[icmp];
		rcmp.m_ws = 0;
		if (rcmp.m_cmt != RnImportCharMapping::kcmtOldWritingSystem)
			continue;
		stu.Assign(rcmp.m_strOldWritingSystem.Chars());
		if (hmsuws.Retrieve(stu, &ws))
			rcmp.m_ws = ws;
	}

	// Note: prnmw will become invalid after CloseDbAndWindows below, so will need to be
	// reinitialized after that.
	RnMainWnd * prnmw = dynamic_cast<RnMainWnd *>(m_pafw);
	Assert(prnmw);
	prnmw->SaveData();
	// Note: plpi will become invalid after CloseDbAndWindows below, so we need to get
	// the information we want before then.
	HVO hvoRn = plpi->GetRnId();
	const Vector<HVO> & vhvo = plpi->GetPsslIds();
	Vector<HVO> vhvoPssl;
	for (int ihvo = 0; ihvo < vhvo.Size(); ++ihvo)
		vhvoPssl.Push(vhvo[ihvo]);
	int flid;

	// Get the default writing system for each field from the user views.
	Vector<RnSfMarker> & vsfmFields = m_shbx.FieldCodes();
	int wsAnal = plpi->AnalWs();
	int isfm;
	for (isfm = 0; isfm < vsfmFields.Size(); ++isfm)
	{
		FldSpecPtr qfsp = pdbi->FindFldSpec(vsfmFields[isfm].m_flidDst);
		if (qfsp)
			vsfmFields[isfm].m_wsDefault = plpi->ActualWs(qfsp->m_ws);
	}

	StrApp strFmt;
	StrApp strFmt2;
	StrApp strMsg;
	StrApp strCaption(kstidImportMsgCaption);

	StrUni stuServerName(pdbi->ServerName());
	StrUni stuDatabase(pdbi->DbName());
	IOleDbEncapPtr qode; // Declare before qodc.
	IOleDbCommandPtr qodc;
	IStreamPtr qstrm;

	// Get the IStream pointer for logging. NULL returned if no log file.
	AfApp::Papp()->GetLogPointer(&qstrm);

	RnImportProgressDlg rnprg;
	rnprg.DoModeless(NULL);

	// Remember all the main windows for the app which are visible, and hide them.
	HWND hwndActive = MainWindow()->Hwnd();
	Vector<AfMainWndPtr> & vqafw = AfApp::Papp()->GetMainWindows();
	Vector<HWND> vhwnd;
	for (int iwnd = 0; iwnd < vqafw.Size(); ++iwnd)
	{
		HWND hwnd = vqafw[iwnd]->Hwnd();
		if (hwnd != hwndActive && ::IsWindowVisible(hwnd))
		{
			vhwnd.Push(hwnd);
			::ShowWindow(hwnd, SW_HIDE);
		}
	}

	strFmt.Load(kstidImportFromTitleFmt);
	StrApp strFile(m_shbx.GetDbFile());
	int ich = strFile.ReverseFindCh('\\');
	strMsg.Format(strFmt.Chars(), strFile.Chars() + ich + 1);
	::SetWindowText(rnprg.Hwnd(), strMsg.Chars());
	rnprg.SetPercentComplete(0);

	int chvoOrig = prnmw->RawRecordCount();

	try
	{
		qode.CreateInstance(CLSID_OleDbEncap);
		StrUni stuMasterDB(L"master");
		CheckHr(qode->Init(stuServerName.Bstr(), stuMasterDB.Bstr(), qstrm, koltMsgBox,
			koltvForever));
		CheckHr(qode->CreateCommand(&qodc));
	}
	catch (...)
	{
		// Cannot connect to master database:
		strMsg.Load(kstidImportCannotMaster);
		::MessageBox(NULL, strMsg.Chars(), strCaption.Chars(), MB_OK | MB_ICONERROR);
		return;
	}
	// In closing the final window, FwTool is going to call AfApp::DecExportedObjects() which
	// would normally post a quit message to the application if there is only one window open.
	// We don't want the application to quit yet, so we need to temporarily add an extra count
	// until we open our new window below.
	AfApp::Papp()->IncExportedObjects();
	// Now close the current database with all its windows.
	AfApp::Papp()->CloseDbAndWindows(stuDatabase.Chars(), stuServerName.Chars(), false);

	// Give users time to log off, then force them off anyway:
	ComBool fDisconnected;
	try
	{
		IDisconnectDbPtr qdscdb;
		qdscdb.CreateInstance(CLSID_FwDisconnect);

		// Set up strings needed for disconnection:
		StrUni stuReason(kstidReasonDisconnectImport);
		// Get our own name:
		DWORD nBufSize = MAX_COMPUTERNAME_LENGTH + 1;
		achar rgchBuffer[MAX_COMPUTERNAME_LENGTH + 1];
		GetComputerName(rgchBuffer, &nBufSize);
		StrUni stuComputer(rgchBuffer);
		StrUni stuFmt(kstidRemoteReasonImport);
		StrUni stuExternal;
		stuExternal.Format(stuFmt.Chars(), stuComputer.Chars());

		qdscdb->Init(stuDatabase.Bstr(), stuServerName.Bstr(), stuReason.Bstr(),
			stuExternal.Bstr(), (ComBool)false, NULL, 0);
		qdscdb->DisconnectAll(&fDisconnected);
	}
	catch (...)
	{
		fDisconnected = false;
	}
	if (!fDisconnected)
	{
		// User canceled, or it was impossible to disconnect people:
		AfApp::Papp()->ReopenDbAndOneWindow(stuDatabase.Chars(), stuServerName.Chars());
		AfApp::Papp()->DecExportedObjects();
		return;
	}
	qodc.Clear();
	qode.Clear();

	qode.CreateInstance(CLSID_OleDbEncap);
	CheckHr(qode->Init(stuServerName.Bstr(), stuDatabase.Bstr(), qstrm, koltMsgBox,
		koltvForever));
	IFwMetaDataCachePtr qmdc;
	qmdc.CreateInstance(CLSID_FwMetaDataCache);
	CheckHr(qmdc->Init(qode));
	int cErr = 0;
	if (m_imdst == kidstReplace)
	{
		// Delete the Data Notebook records, and any topics list items that have been added,
		// theoretically reinitializing the lists to their factory settings.
		rnprg.SetPercentComplete(0);
		strMsg.Load(kstidImportPhaseOne);
		rnprg.SetActionString(strMsg);
		CheckHr(qode->CreateCommand(&qodc));
		StrUni stuQuery;
		stuQuery.Format(L"exec DeleteAddedNotebookObjects$");
		CheckHr(qode->CreateCommand(&qodc));
		rnprg.SetPercentComplete(4);
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtStoredProcedure));
		rnprg.SetPercentComplete(98);
		qodc.Clear();
		DeleteInvalidFilters(qode);
		rnprg.SetPercentComplete(100);
	}
	else if (m_imdst == kidstMerge)
	{
		// Delete matching entries?
#if 2
		stab.Format("Merging Shoebox data has not been implemented yet.");
		::MessageBoxA(m_hwnd, stab.Chars(), stabTitle.Chars(), MB_OK);
#endif
	}

	RnSfMarker * psfmRecord = &m_shbx.FieldCodes()[m_shbx.RecordIndex()];
	AssertPtr(psfmRecord);

	// Assume Event records if nothing earlier made this decision for us.
	if (psfmRecord->m_rt == RnSfMarker::krtNone)
	{
		psfmRecord->m_rt = RnSfMarker::krtEvent;
		psfmRecord->m_nLevel = 1;
	}

	// Get the writing system data, and hash it on the writing system code.
	// Also get the encoding converter for the default analysis writing system.
	IEncConverterPtr qconAnal;
	IEncConvertersPtr qencc;
	qencc.CreateInstance(L"SilEncConverters31.EncConverters");
	AssertPtr(qencc);
	HashMap<int, WrtSysData> hmwswsd;
	int iwsd;
	for (iwsd = 0; iwsd < m_vwsd.Size(); ++iwsd)
	{
		hmwswsd.Insert(m_vwsd[iwsd].m_ws, m_vwsd[iwsd]);
		if (m_vwsd[iwsd].m_ws == m_wsAnal)
			GetEncodingConverter(m_vwsd[iwsd], qencc, &qconAnal);

	}

	SilTime stimNow = SilTime::CurTime();
	HashMapChars<int> & hmcisfmFields = m_shbx.FieldCodesMap();
	int i;
	int isfmType;
	int irec;
	int idst;
	int clid;
	HVO hvoOwner;
	Vector<HVO> vhvoPrev;
	vhvoPrev.Push(hvoRn);		// Dummy value.
	vhvoPrev.Push(0);			// Signals no previous record.
	int nLevelPrev = 0;
	int nLevel;
	HVO hvoNew;

	HashMap<int, int> hmflididst;
	for (i = 0; i < m_vridst.Size(); ++i)
		hmflididst.Insert(m_vridst[i].m_flid, i);

	// Generate default date formats if necessary (and possible).
	for (isfm = 0; isfm < vsfmFields.Size(); ++isfm)
	{
		if (!hmflididst.Retrieve(vsfmFields[isfm].m_flidDst, &idst))
			continue;
		if (m_vridst[idst].m_proptype == kcptTime || m_vridst[idst].m_proptype == kcptGenDate)
		{
			if (!vsfmFields[isfm].m_dto.m_vstaFmt.Size())
				DeriveDateFormat(vsfmFields[isfm]);
		}
	}
	Vector<char> vchDateFmt;
	int cch = ::GetLocaleInfoA(LOCALE_USER_DEFAULT, LOCALE_SSHORTDATE, vchDateFmt.Begin(), 0);
	if (cch != 0)
	{
		vchDateFmt.Resize(cch);
		::GetLocaleInfoA(LOCALE_USER_DEFAULT, LOCALE_SSHORTDATE, vchDateFmt.Begin(),
			vchDateFmt.Size());
	}

	// Isolate the field markers and store a list of record marker indexes.
	Vector<int> vipszRecords;
	Vector<char *> vpszFieldData;
	StrAnsiBufSmall stabs;
	char * pszField;
	char *psz;
	for (i = 0; i < m_risd.m_vpszFields.Size(); ++i)
	{
		pszField = m_risd.m_vpszFields[i];
		psz = strpbrk(pszField, kszSpace);
		if (psz)
		{
			*psz++ = '\0';
			psz += strspn(psz, kszSpace);
		}
		else
		{
			psz = pszField + strlen(pszField);
		}
		vpszFieldData.Push(psz);
		stabs.Assign(pszField + 1);
		if (stabs == psfmRecord->m_stabsMkr)
			vipszRecords.Push(i);
	}
	int crec = vipszRecords.Size();
	if (crec > 0)
	{
		if (m_imdst == kidstReplace)
			strFmt.Load(kstidImportPhaseTwo);
		else
			strFmt.Load(kstidImportOnlyPhase);
		strMsg.Format(strFmt.Chars(), crec);
		rnprg.SetActionString(strMsg);
		rnprg.SetPercentComplete(0);
	}
	vipszRecords.Push(m_risd.m_vpszFields.Size());

	// Keep track of added possibility list items.
	Vector< Vector<HVO> > vvhvopssNew;
	vvhvopssNew.Resize(vhvoPssl.Size());
	int iPssl;
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	AssertPtr(qtsf);
	ITsStringPtr qtss;
	StrAnsi staField;
	HRESULT hr;

	// Process each of the records in turn.

	time_t timStart = time(0);
	time_t timMid;
	int nDeltaSec;
	cErr = 0;
	StrApp strErrFmt;
	StrApp strErrMsg;
	for (irec = 0; irec < crec; ++irec)
	{
		isfmType = m_shbx.RecordIndex();
		// First determine what kind/level of record this is.
		for (i = vipszRecords[irec] + 1; i < vipszRecords[irec+1]; ++i)
		{
			if (hmcisfmFields.Retrieve(m_risd.m_vpszFields[i] + 1, &isfm))
			{
				pszField = vpszFieldData[i];
				if (vsfmFields[isfm].m_fIgnoreEmpty && !*(pszField + strspn(pszField,kszSpace)))
					continue;			// Ignore empty field.
				if (vsfmFields[isfm].m_rt != RnSfMarker::krtNone)
				{
					isfmType = isfm;
					break;				// Take the first type designating field found.
				}
			}
		}
		nLevel = vsfmFields[isfmType].m_nLevel;
		if (nLevel > nLevelPrev + 1)
			nLevel = nLevelPrev + 1;	// Need Subrecord before SubSubRecord!
		if (vsfmFields[isfmType].m_rt == RnSfMarker::krtEvent)
		{
			// New event record.
			clid = kclidRnEvent;
			if (nLevel == 1)
			{
				hvoOwner = hvoRn;
				flid = kflidRnResearchNbk_Records;
			}
			else
			{
				hvoOwner = vhvoPrev[nLevel - 1];
				flid = kflidRnGenericRec_SubRecords;
			}
		}
		else
		{
			// New analysis record.
			clid = kclidRnAnalysis;
			if (nLevel == 1)
			{
				hvoOwner = hvoRn;
				flid = kflidRnResearchNbk_Records;
			}
			else
			{
				hvoOwner = vhvoPrev[nLevel - 1];
				flid = kflidRnGenericRec_SubRecords;
			}
		}
		////////////////////////////////////////////////////////////////////////////////////////
		try
		{
			CheckHr(qode->BeginTrans());
			strErrMsg.Clear();

			SilTime stim;
			int gdat;
			int ista;
			int ord;
			if (flid == kflidRnGenericRec_SubRecords)
			{
				CheckHr(GetVecSize(qode, qmdc, hvoOwner, flid, &ord));
			}
			else
			{
				ord = -1;
			}
			CheckHr(MakeNewObject(qode, qmdc, clid, hvoOwner, flid, ord, &hvoNew));

			// Set create and modified times to the new time.
			bool fNeedCreateDate = true;
			bool fNeedModDate = true;

			Vector<StTextData> vstd;
			HVO hvoRoledPart = 0;
			HashMap<int, int> hmflidistd;
			// "Error storing %<0>s for input record %<1>d. (%<2>s)"
			StrApp strErrorFmt(kstidImportStoreErrorFmt);

			// Add empty structured texts for all appropriate fields.
			StTextData std;
			std.m_cPara = 0;
			int istd;
			for (i = 0; i < m_vridst.Size(); ++i)
			{
				if (m_vridst[i].m_proptype == kcptOwningAtom &&
					m_vridst[i].m_clidDst == kclidStText &&
					(m_vridst[i].m_clidRecord == clid ||
					 m_vridst[i].m_clidRecord == kclidRnGenericRec))
				{
					CheckHr(MakeNewObject(qode, qmdc, kclidStText, hvoNew, m_vridst[i].m_flid,
						-2, &std.m_hvo));
					CheckHr(MakeNewObject(qode, qmdc, kclidStTxtPara, std.m_hvo,
						kflidStText_Paragraphs, 1, &std.m_hvoPara));
					istd = vstd.Size();
					hmflidistd.Insert(m_vridst[i].m_flid, istd);
					vstd.Push(std);
				}
			}

			for (i = vipszRecords[irec]; i < vipszRecords[irec+1]; ++i)
			{
				if (!hmcisfmFields.Retrieve(m_risd.m_vpszFields[i] + 1, &isfm))
					continue;
				pszField = vpszFieldData[i];
				RnSfMarker & sfm = vsfmFields[isfm];
				if (sfm.m_fIgnoreEmpty && !*(pszField + strspn(pszField, kszSpace)))
					continue;				// Ignore empty field.
				int wsPara = sfm.m_txo.m_ws;
				if (!wsPara)
				{
					wsPara = sfm.m_wsDefault;
					if (!wsPara)
						wsPara = wsAnal;
				}
				if (!hmflididst.Retrieve(sfm.m_flidDst, &idst))
					continue;
				RnImportDest & ridst = m_vridst[idst];
				Assert(ridst.m_flid == sfm.m_flidDst);
				int clidField = sfm.m_flidDst / 1000;
				if (clidField != ridst.m_clidRecord && clidField != kclidRnGenericRec)
					continue;		// Inappropriate record type for this field.  (?)
				switch (ridst.m_proptype)
				{
				case kcptBoolean:
					Assert(ridst.m_proptype != kcptBoolean);
					break;
				case kcptInteger:
					// This will work for bool, tinyint, smallint, and int.
					{
						char * psz;
						long n = strtol(pszField, &psz, 10);
						if (psz != pszField || !sfm.m_fIgnoreEmpty)
						{
							IgnoreHr(hr = SetInt(qode, qmdc, hvoNew, sfm.m_flidDst, n));
							if (FAILED(hr))
							{
								StrApp strCode(AsciiHresult(hr));
								strErrMsg.Format(strErrorFmt.Chars(),
									ridst.m_strName.Chars(), irec + 1, strCode.Chars());
								ThrowHr(hr);
							}
						}
					}
					break;
				case kcptNumeric:
					Assert(ridst.m_proptype != kcptNumeric);
					//? hr = SetInt64(hvo, tag, lln);
					break;
				case kcptFloat:
					Assert(ridst.m_proptype != kcptFloat);
					break;
				case kcptTime:
					// kflidRnGenericRec_DateCreated
					// kflidRnGenericRec_DateModified
					cch = 0;
					if (sfm.m_dto.m_vstaFmt.Size())
					{
						for (ista = 0; ista < sfm.m_dto.m_vstaFmt.Size(); ++ista)
						{
							cch = StrUtil::ParseDateWithFormat(pszField,
								sfm.m_dto.m_vstaFmt[ista].Chars(), &stim);
							if (cch)
								break;
						}
					}
					else if (vchDateFmt.Size())
					{
						cch = StrUtil::ParseDateWithFormat(pszField, vchDateFmt.Begin(), &stim);
					}
					if (cch)
					{
						// Check, and attempt fix, for 2-digit year.
						if (stim.Year() < 100)
						{
							stim.SetYear(stim.Year() + 2000);
							if (stim.Year() > stimNow.Year())
								stim.SetYear(stim.Year() - 100);
						}
						IgnoreHr(hr = SetTime(qode, qmdc, hvoNew, sfm.m_flidDst, stim.AsInt64()));
						if (FAILED(hr))
						{
							StrApp strCode(AsciiHresult(hr));
							strErrMsg.Format(strErrorFmt.Chars(),
								ridst.m_strName.Chars(), irec + 1, strCode.Chars());
							ThrowHr(hr);
						}
						if (sfm.m_flidDst == kflidRnGenericRec_DateCreated)
							fNeedCreateDate = false;
						else if (sfm.m_flidDst == kflidRnGenericRec_DateModified)
							fNeedModDate = false;
					}
					break;
				case kcptGuid:
					// hr = SetGuid(hvo, tag, guid);
					Assert(ridst.m_proptype != kcptGuid);
					break;
				case kcptBinary:
				case kcptImage:
					// hr = SetBinary(hvo, tag, prgb, cb);
					Assert(ridst.m_proptype != kcptBinary && ridst.m_proptype != kcptImage);
					break;

				case kcptGenDate:
					// kflidRnEvent_DateOfEvent
					cch = 0;
					if (sfm.m_dto.m_vstaFmt.Size())
					{
						for (ista = 0; ista < sfm.m_dto.m_vstaFmt.Size(); ++ista)
						{
							cch = StrUtil::ParseDateWithFormat(pszField,
								sfm.m_dto.m_vstaFmt[ista].Chars(), &stim);
							if (cch)
								break;
						}
					}
					else if (vchDateFmt.Size())
					{
						cch = StrUtil::ParseDateWithFormat(pszField, vchDateFmt.Begin(), &stim);
					}
					if (cch)
					{
						// Convert SilTime to GenDate integer, and store the date.
						SilTimeInfo sti;
						stim.GetTimeInfo(&sti);
						gdat = sti.year * 100000 + sti.ymon * 1000 + sti.mday * 10;
						char * psz = pszField + cch;
						psz += strspn(psz, " \t");
						if (*psz == '?')
							gdat += 2;		// Approximate.
						else
							gdat += 1;		// Exact.
						IgnoreHr(hr = SetInt(qode, qmdc, hvoNew, sfm.m_flidDst, gdat));
						if (FAILED(hr))
						{
							StrApp strCode(AsciiHresult(hr));
							strErrMsg.Format(strErrorFmt.Chars(),
								ridst.m_strName.Chars(), irec + 1, strCode.Chars());
							ThrowHr(hr);
						}
					}

					break;
				case kcptString:
				case kcptBigString:
					// kflidRnGenericRec_Title
					{
						FixStringData(pszField, sfm.m_txo, staField);
						int ich;
						while ((ich = staField.FindCh('\n')) >= 0)
							staField.SetAt(ich, ' ');	// No paragraph breaks in title!
						int ws = sfm.m_mlo.m_ws;
						if (!ws)
							ws = sfm.m_wsDefault;
						if (!ws)
							ws = wsAnal;
						MakeString(staField, ws, qtsf, qencc, hmwswsd, &qtss);
						IgnoreHr(hr = SetString(qode, qmdc, hvoNew, sfm.m_flidDst, qtss));
						if (FAILED(hr))
						{
							StrApp strCode(AsciiHresult(hr));
							strErrMsg.Format(strErrorFmt.Chars(),
								ridst.m_strName.Chars(), irec + 1, strCode.Chars());
							ThrowHr(hr);
						}
					}
					break;
				case kcptMultiString:
				case kcptMultiBigString:
					{
						FixStringData(pszField, sfm.m_txo, staField);
						int ich;
						while ((ich = staField.FindCh('\n')) >= 0)
							staField.SetAt(ich, ' ');	// No paragraph breaks.
						int ws = sfm.m_mlo.m_ws;
						if (!ws)
							ws = sfm.m_wsDefault;
						if (!ws)
							ws = wsAnal;
						MakeString(staField, ws, qtsf, qencc, hmwswsd, &qtss);
						IgnoreHr(hr = SetMultiStringAlt(qode, qmdc, hvoNew, sfm.m_flidDst,
							sfm.m_mlo.m_ws, qtss));
						if (FAILED(hr))
						{
							StrApp strCode(AsciiHresult(hr));
							strErrMsg.Format(strErrorFmt.Chars(),
								ridst.m_strName.Chars(), irec + 1, strCode.Chars());
							ThrowHr(hr);
						}
					}
					break;
				case kcptUnicode:
				case kcptBigUnicode:
					// hr = SetUnicode)(hvo, tag, prgch, cch);
					Assert(ridst.m_proptype != kcptUnicode &&
						ridst.m_proptype != kcptBigUnicode);
					break;
				case kcptMultiUnicode:
					Assert(ridst.m_proptype != kcptMultiUnicode);
					break;
				case kcptMultiBigUnicode:
					Assert(ridst.m_proptype != kcptMultiBigUnicode);
					break;
				case kcptOwningAtom:
					if (ridst.m_clidDst == kclidStText)
					{
						// kflidRnGenericRec_VersionHistory
						// kflidRnGenericRec_ExternalMaterials
						// kflidRnEvent_PersonalNotes
						// kflidRnEvent_Description
						// kflidRnAnalysis_ResearchPlan
						// kflidRnAnalysis_Discussion
						// kflidRnGenericRec_FurtherQuestions
						int istd;
						if (!hmflidistd.Retrieve(sfm.m_flidDst, &istd))
						{
							istd = vstd.Size();
							StTextData std;
							std.m_cPara = 1;
							CheckHr(MakeNewObject(qode, qmdc, kclidStText, hvoNew,
								sfm.m_flidDst, -2, &std.m_hvo));
							CheckHr(MakeNewObject(qode, qmdc, kclidStTxtPara, std.m_hvo,
								kflidStText_Paragraphs, std.m_cPara, &std.m_hvoPara));
							hmflidistd.Insert(sfm.m_flidDst, istd);
							vstd.Push(std);
						}
						else if (vstd[istd].m_cPara++)
						{
							// An initial empty paragraph may have been created with ord = 1,
							// but m_cPara = 0.  Don't create a second empty paragraph at the
							// beginning of the field.
							CheckHr(MakeNewObject(qode, qmdc, kclidStTxtPara, vstd[istd].m_hvo,
								kflidStText_Paragraphs, vstd[istd].m_cPara,
								&vstd[istd].m_hvoPara));
						}
						FixStringData(pszField, sfm.m_txo, staField);
						int ich;
						int ich0 = 0;
						while ((ich = staField.FindCh('\n', ich0)) >= 0)
						{
							StrAnsi sta(staField.Chars() + ich0, ich - ich0);
							ich0 = ich + 1;
							MakeString(sta, wsPara, qtsf, qencc, hmwswsd, &qtss);
							IgnoreHr(hr = SetString(qode, qmdc, vstd[istd].m_hvoPara,
								kflidStTxtPara_Contents, qtss));
							if (FAILED(hr))
							{
								StrApp strCode(AsciiHresult(hr));
								strErrMsg.Format(strErrorFmt.Chars(),
									ridst.m_strName.Chars(), irec + 1, strCode.Chars());
								ThrowHr(hr);
							}
							SetParaStyle(qode, qmdc, vstd[istd].m_hvoPara,
								sfm.m_txo.m_stuStyle);
							++vstd[istd].m_cPara;
							IgnoreHr(hr = MakeNewObject(qode, qmdc, kclidStTxtPara, vstd[istd].m_hvo,
								kflidStText_Paragraphs, vstd[istd].m_cPara,
								&vstd[istd].m_hvoPara));
							if (FAILED(hr))
							{
								StrApp strCode(AsciiHresult(hr));
								strErrMsg.Format(strErrorFmt.Chars(),
									ridst.m_strName.Chars(), irec + 1, strCode.Chars());
								ThrowHr(hr);
							}
						}
						if (ich0)
							staField.Replace(0, ich0, L"");
						MakeString(staField, wsPara, qtsf, qencc, hmwswsd, &qtss);
						IgnoreHr(hr = SetString(qode, qmdc, vstd[istd].m_hvoPara,
							kflidStTxtPara_Contents, qtss));
						if (FAILED(hr))
						{
							StrApp strCode(AsciiHresult(hr));
							strErrMsg.Format(strErrorFmt.Chars(),
								ridst.m_strName.Chars(), irec + 1, strCode.Chars());
							ThrowHr(hr);
						}
						SetParaStyle(qode, qmdc, vstd[istd].m_hvoPara, sfm.m_txo.m_stuStyle);
					}
					else
					{
						Assert(ridst.m_proptype != kcptOwningAtom);
					}
					break;
				case kcptReferenceAtom:
					if (ridst.m_clidDst == kclidCmPossibility ||
						ridst.m_clidDst == kclidCmPerson ||
						ridst.m_clidDst == kclidCmLocation ||
						ridst.m_clidDst == kclidCmAnthroItem ||
						ridst.m_clidDst == kclidCmCustomItem)
					{
						// kflidRnGenericRec_Confidence:
						// kflidRnEvent_Type:
						// kflidRnAnalysis_Status:
						// kflidRnEvent_TimeOfEvent:
						int ipssl;
						switch (sfm.m_flidDst)
						{
						case kflidRnGenericRec_Confidence:
							ipssl = RnLpInfo::kpidPsslCon;
							break;
						case  kflidRnEvent_Type:
							ipssl = RnLpInfo::kpidPsslTyp;
							break;
						case kflidRnAnalysis_Status:
							ipssl = RnLpInfo::kpidPsslAna;
							break;
						case kflidRnEvent_TimeOfEvent:
							ipssl = RnLpInfo::kpidPsslTim;
							break;
						default:
							for (ipssl = RnLpInfo::kpidPsslLim;
								ipssl < vhvoPssl.Size();
								++ipssl)
							{
								if (vhvoPssl[ipssl] == ridst.m_hvoPssl)
									break;
							}
							break;
						}
						if (ipssl >= vhvoPssl.Size())
						{
							// ERROR MESSAGE?
							break;
						}
						Assert(ridst.m_hvoPssl == vhvoPssl[ipssl]);
						SetPossibility(qode, qmdc, pszField, sfm, ridst, hvoNew,
							vvhvopssNew[ipssl], qconAnal);
					}
					break;
				case kcptOwningCollection:
					if (ridst.m_clidDst == kclidRnRoledPartic)
					{
						Assert(ridst.m_flid == kflidRnEvent_Participants);
						Vector<PossItemData> vpidParts;
						ParsePossibilities(pszField, sfm, vpidParts);
						if (!vpidParts.Size())
							break;		// The field is actually empty.
						int ipid;
						HVO hvopssl = vhvoPssl[RnLpInfo::kpidPsslPeo];
						for (ipid = 0; ipid < vpidParts.Size(); ++ipid)
						{
							SetListHvoAndConvertToUtf16(vpidParts[ipid], hvopssl, qconAnal);
						}
						if (hvoRoledPart == 0)
						{
							CheckHr(MakeNewObject(qode, qmdc, kclidRnRoledPartic, hvoNew,
								ridst.m_flid, -1, &hvoRoledPart));
						}
						ListInfo * pli = LoadPossibilityList(qode, hvopssl);
						if (!pli)
							break;
						int ipss = pli->m_vlii.Size();
						StrUni stuPoss;
						Vector<HVO> vhvopss;
						while (--ipss >= 0 && vpidParts.Size())
						{
							ListItemInfo & lii = pli->m_vlii[ipss];
							if (sfm.m_clo.m_pnt == kpntName)
								stuPoss = lii.m_stuName;
							else
								stuPoss = lii.m_stuAbbr;
							ipid = PossNameMatchesData(stuPoss, vpidParts);
							if (ipid >= 0)
							{
								int hvopss = lii.m_hvoItem;
								vhvopss.Push(hvopss);
								vpidParts.Delete(ipid);
							}
						}
						if (vhvopss.Size())
						{
							CheckHr(AppendObjRefs(qode, qmdc, hvoRoledPart,
								kflidRnRoledPartic_Participants,
								vhvopss.Begin(), vhvopss.Size()));
						}
						if (!vpidParts.Size())
							break;		// No new names in this field.
						vhvopss.Clear();
						StrApp strCaption(kstidImportMsgCaption);
						StrApp strCannot(kstidCannotInsertListItem);
						StrUni stuAbbr;
						StrUni stuName;
						for (ipid = 0; ipid < vpidParts.Size(); ++ipid)
						{
							// Add a new item at the end of the list.
							// Make the name and the abbreviation the same initially.
							stuAbbr.Assign(vpidParts[ipid].m_stuName.Chars());
							stuName.Assign(stuAbbr);
							int hvopss = InsertPss(qode, qmdc, pli, pli->m_vlii.Size() - 1,
								stuAbbr.Chars(), stuName.Chars(), kpilTop);
							if (!hvopss)
							{
								::MessageBox(m_hwnd, strCannot.Chars(), strCaption.Chars(),
									MB_OK | MB_ICONSTOP);
								continue;
							}
							vhvopss.Push(hvopss);
						}
						if (vhvopss.Size())
						{
							CheckHr(AppendObjRefs(qode, qmdc, hvoRoledPart,
								kflidRnRoledPartic_Participants,
								vhvopss.Begin(), vhvopss.Size()));
						}
					}
					else
					{
						Assert(ridst.m_proptype != kcptOwningCollection);
					}
					break;
				case kcptReferenceCollection:
				case kcptReferenceSequence:
					if (ridst.m_clidDst == kclidCmPossibility ||
						ridst.m_clidDst == kclidCmPerson ||
						ridst.m_clidDst == kclidCmLocation ||
						ridst.m_clidDst == kclidCmAnthroItem ||
						ridst.m_clidDst == kclidCmCustomItem)
					{
						int ipssl;
						switch (sfm.m_flidDst)
						{
						case kflidRnGenericRec_Restrictions:
							ipssl = RnLpInfo::kpidPsslRes;
							break;
						case kflidRnGenericRec_Researchers:
							ipssl = RnLpInfo::kpidPsslPeo;
							break;
						case kflidRnGenericRec_AnthroCodes:
							ipssl = RnLpInfo::kpidPsslAnit;
							break;
						case kflidRnEvent_Locations:
							ipssl = RnLpInfo::kpidPsslLoc;
							break;
						case kflidRnEvent_Sources:
							ipssl = RnLpInfo::kpidPsslPeo;
							break;
						case kflidRnEvent_Weather:
							ipssl = RnLpInfo::kpidPsslWea;
							break;
						default:
							for (ipssl = RnLpInfo::kpidPsslLim;
								ipssl < vhvoPssl.Size();
								++ipssl)
							{
								if (vhvoPssl[ipssl] == ridst.m_hvoPssl)
									break;
							}
							break;
						}
						if (ipssl >= vhvoPssl.Size())
						{
							// ERROR MESSAGE?
							break;
						}
						Assert(ridst.m_hvoPssl == vhvoPssl[ipssl]);
						SetPossibility(qode, qmdc, pszField, sfm, ridst, hvoNew,
							vvhvopssNew[ipssl], qconAnal);
					}
					else if (ridst.m_clidDst == kclidRnGenericRec)
					{
						// kcptReferenceSequence:
						//	kflidRnGenericRec_SeeAlso:
						//	kflidRnAnalysis_CounterEvidence:
						//	kflidRnAnalysis_SupportingEvidence:
						//	kflidRnAnalysis_SupersededBy:
						Assert(ridst.m_proptype != kcptReferenceSequence &&
							ridst.m_proptype != kcptReferenceCollection);
					}
					else
					{
						Assert(ridst.m_proptype != kcptReferenceSequence &&
							ridst.m_proptype != kcptReferenceCollection);
					}
					break;
				case kcptOwningSequence:
					Assert(ridst.m_proptype != kcptOwningSequence);
					break;
				default:
					break;
				}
			}

			// We've created the object, now we need to save its id and fill in its attributes!

			// Set up for the next record.
			if (nLevel < vhvoPrev.Size())
				vhvoPrev[nLevel] = hvoNew;
			else
				vhvoPrev.Push(hvoNew);
			nLevelPrev = nLevel;

			if (fNeedCreateDate)
			{
				IgnoreHr(hr = SetTime(qode, qmdc, hvoNew, kflidRnGenericRec_DateCreated,
					stimNow.AsInt64()));
				if (FAILED(hr))
				{
					strErrFmt.Load(kstidImportDateCreatedError);
					StrApp strCode(AsciiHresult(hr));
					strErrMsg.Format(strErrFmt.Chars(), irec + 1, strCode.Chars());
					ThrowHr(hr);
				}
			}
			if (fNeedModDate)
			{
				IgnoreHr(hr = SetTime(qode, qmdc, hvoNew, kflidRnGenericRec_DateModified,
					stimNow.AsInt64()));
				if (FAILED(hr))
				{
					strErrFmt.Load(kstidImportDateModifiedError);
					StrApp strCode(AsciiHresult(hr));
					strErrMsg.Format(strErrFmt.Chars(), irec + 1, strCode.Chars());
					ThrowHr(hr);
				}
			}
			if (fNeedCreateDate || fNeedModDate)
			{
				// increment stimNow by 4 msec.  (MS SQL Server uses 3.33333333msec increments.)
				stimNow = stimNow + 4;
			}
			CheckHr(qode->CommitTrans());
		}
		catch (Throwable & thr)
		{
			// what do we do about errors?
			CheckHr(qode->RollbackTrans());
			if (!strErrMsg.Length())
			{
				strErrFmt.Load(kstidImportGeneralCOMLoadError);
				StrApp strCode(AsciiHresult(thr.Error()));
				strErrMsg.Format(strErrFmt.Chars(), irec + 1, strCode.Chars());
			}
			::MessageBox(m_hwnd, strErrMsg.Chars(), strCaption.Chars(), MB_OK | MB_ICONWARNING);
			++cErr;
			if (cErr == 1)
			{
				StrApp str(kstidImportEntryError);
				strMsg.FormatAppend(str.Chars());	// string contains %n, which is formatting
			}
			else
			{
				if (strFmt2.Length() == 0)
					strFmt2.Load(kstidImportEntriesError);
				strMsg.Format(strFmt.Chars(), crec);
				strMsg.FormatAppend(strFmt2.Chars(), cErr);
			}
			rnprg.SetActionString(strMsg);
		}
		catch (...)
		{
			// what do we do about errors?
			CheckHr(qode->RollbackTrans());
			if (!strErrMsg.Length())
			{
				strErrFmt.Load(kstidImportGeneralLoadError);
				strErrMsg.Format(strErrFmt.Chars(), irec + 1);
			}
			::MessageBox(m_hwnd, strErrMsg.Chars(), strCaption.Chars(), MB_OK | MB_ICONWARNING);
		}
		timMid = time(0);
		nDeltaSec = static_cast<int>(timMid - timStart);
		rnprg.UpdateStatus(irec+1, crec, nDeltaSec);
	}
	try
	{
		qode->BeginTrans();
		// Set the create and modify times for any newly created possibilities, and the modify times
		// for any modified possibility lists, and for the data notebook itself.
		for (iPssl = 0; iPssl < RnLpInfo::kpidPsslLim; ++iPssl)
		{
			for (int ipss = 0; ipss < vvhvopssNew[iPssl].Size(); ++ipss)
			{
				CheckHr(SetTime(qode, qmdc, vvhvopssNew[iPssl][ipss],
					kflidCmPossibility_DateCreated, stimNow.AsInt64()));
				CheckHr(SetTime(qode, qmdc, vvhvopssNew[iPssl][ipss],
					kflidCmPossibility_DateModified, stimNow.AsInt64()));
				// increment stimNow by 4 msec.  (MS SQL Server uses 3.33333333msec increments.)
				stimNow = stimNow + 4;
			}
			if (vvhvopssNew[iPssl].Size())
			{
				CheckHr(SetTime(qode, qmdc, vhvoPssl[iPssl],
					kflidCmMajorObject_DateModified, stimNow.AsInt64()));
			}
		}
		CheckHr(SetTime(qode, qmdc, hvoRn, kflidCmMajorObject_DateModified, stimNow.AsInt64()));


		// TODO SteveMc: Think about the BeginTrans/CommitTrans strategy.
		CheckHr(qode->CommitTrans());
	}
	catch(...)
	{
		qode->RollbackTrans();
		throw;	// For now we have nothing to add, so pass it on up.
	}
	qmdc.Clear();
	qode.Clear();

	// Open and connect a window to the database:
	AfApp::Papp()->ReopenDbAndOneWindow(stuDatabase.Chars(), stuServerName.Chars());
	// Reopening the window will add a new count to our exported objects,
	// so we can now clear our temporary hold on the application.
	AfApp::Papp()->DecExportedObjects();

	// Show all the main windows for the app that we hid before.
	for (int iwnd = 0; iwnd < vhwnd.Size(); ++iwnd)
		::ShowWindow(vhwnd[iwnd], SW_SHOWNA);

	m_pafw = AfApp::Papp()->GetCurMainWnd();	// MainWindow() retrieves stale data.
	prnmw = dynamic_cast<RnMainWnd *>(m_pafw);
	plpi = dynamic_cast<RnLpInfo *>(prnmw->GetLpInfo());
	AssertPtr(plpi);
	pdbi = plpi->GetDbInfo();
	AssertPtr(pdbi);

	::DestroyWindow(rnprg.Hwnd());

	// Check whether the user wants to edit newly added possibility list items.
	int cpssNew = 0;
	int cpsslChanged = 0;
	StrUni stuChangedLists;
	for (iPssl = 0; iPssl < RnLpInfo::kpidPsslLim; ++iPssl)
	{
		cpssNew += vvhvopssNew[iPssl].Size();
		if (vvhvopssNew[iPssl].Size())
		{
			long hvopssl = plpi->GetPsslIds()[iPssl];

			int wsList = 0;
			int ili;
			if (m_hmhvoListili.Retrieve(hvopssl, &ili))
				wsList = m_vli[ili].m_ws;
			if (!wsList)
				wsList = m_wsUser;
			PossListInfoPtr qpli;
			plpi->LoadPossList(hvopssl, wsList, &qpli);
			Assert(qpli);
			if (stuChangedLists.Length())
				stuChangedLists.Append(L",");
			stuChangedLists.FormatAppend(L" %s", qpli->GetName());
			++cpsslChanged;
		}
	}
	if (cpsslChanged)
	{
		StrApp strCaption(kstidImportMsgCaption);
		StrApp strMsgFmt;
		if (cpsslChanged > 1)
			strMsgFmt.Load(kstidImportEditListsFmt);
		else if (cpssNew == 1)
			strMsgFmt.Load(kstidImportEditList1Fmt);
		else
			strMsgFmt.Load(kstidImportEditList2Fmt);
		StrApp strChangedLists(stuChangedLists);
		StrApp strMsg;
		strMsg.Format(strMsgFmt.Chars(), strChangedLists.Chars());
		int nRet = ::MessageBox(m_hwnd, strMsg.Chars(), strCaption.Chars(),
			MB_YESNO | MB_ICONQUESTION);
		if (nRet == IDYES)
		{
			bool fEditingCancelled = false;
			RnImportEditListProgressDlg rielp(cpsslChanged, m_shbx.GetDbFile(),
				&fEditingCancelled);
			rielp.DoModeless(prnmw->Hwnd());

			// Hide all the main windows for the app, remembering which windows are hidden.
			hwndActive = MainWindow()->Hwnd();
			::ShowWindow(hwndActive, SW_HIDE);
			for (int iwnd = 0; iwnd < vhwnd.Size(); ++iwnd)
				::ShowWindow(vhwnd[iwnd], SW_HIDE);
			int cpsslEdited = 0;
			for (iPssl = 0; iPssl < RnLpInfo::kpidPsslLim; ++iPssl)
			{
				if (fEditingCancelled)
					break;
				if (vvhvopssNew[iPssl].Size())
				{
					// Invoke the list editor on the first changed entry in this list.
					long hvopssl = plpi->GetPsslIds()[iPssl];
					int wsList = 0;
					int ili;
					Assert(sizeof(int) == sizeof(long));
					if (m_hmhvoListili.Retrieve(hvopssl, &ili))
						wsList = m_vli[ili].m_ws;
					if (!wsList)
						wsList = m_wsUser;
					PossListInfoPtr qpli;
					plpi->LoadPossList(hvopssl, wsList, &qpli);
					Assert(qpli);
					StrApp strList(qpli->GetName());
					rielp.UpdateStatus(strList.Chars());
					try
					{
						MSG message;
						if (::PeekMessage(&message, NULL, WM_PAINT, WM_PAINT, PM_REMOVE))
							::DispatchMessage(&message);
						// Open the List Editor on the new item.
						IFwToolPtr qft;
						CLSID clsid;
						StrUni stu(kpszCleProgId);
						CheckHr(::CLSIDFromProgID(stu.Chars(), &clsid));
						qft.CreateInstance(clsid);

						// Always save the database prior to opening the list editor to avoid
						// locks. Locks can happen even when the user doesn't intentionally
						// modify the database (e.g., UserViews are written the first time a
						// ListEditor is opened on a newly created database.)
						prnmw->SaveData();

						long htool;
						int nPid;
						Vector<HVO> vhvo;
						// We don't want to pass in a flid array, but if we try to pass in a
						// null vector, the marshalling process complains. So we need to use
						// this kludge to get it to work.
						int flidKludge;
						int nView = -1; // Default to data entry.
						vhvo.Push(vvhvopssNew[iPssl][0]);
						CheckHr(qft->NewMainWndWithSel((wchar *)pdbi->ServerName(),
							(wchar *)pdbi->DbName(), plpi->GetLpId(), qpli->GetPsslId(),
							qpli->GetWs(), 0, 0, vhvo.Begin(), vhvo.Size(), &flidKludge, 0, 0,
							nView, &nPid, &htool));
						// Disable the Cancel button on the last list, as it can't do any good.
						if (++cpsslEdited == cpsslChanged)
						{
							::EnableWindow(
								::GetDlgItem(rielp.Hwnd(), kctidImportListEditCancel), FALSE);
						}
						// Wait for the new list editor to close.
						HANDLE hproc = ::OpenProcess(PROCESS_QUERY_INFORMATION, FALSE, nPid);
						// THIS NEXT LINE IS CRUCIAL FOR ::GetExitCodeProcess() BELOW TO WORK!
						qft.Clear();
						if (hproc)
						{
							DWORD nCode;
							while (::GetExitCodeProcess(hproc, &nCode))
							{
								if (nCode != STILL_ACTIVE)
									break;
								// Allow windows to repaint themselves.
								if (::PeekMessage(&message, NULL, WM_PAINT,WM_PAINT,PM_REMOVE))
									::DispatchMessage(&message);
								// Allow the progress report dialog to process the Cancel
								// button.
								if (::PeekMessage(&message, rielp.Hwnd(), 0, 0,PM_REMOVE))
									::DispatchMessage(&message);
							}
							::CloseHandle(hproc);
						}
						else
						{
							while (::IsWindow((HWND)htool))
							{
								// Allow windows to repaint themselves.
								if (::PeekMessage(&message, NULL, WM_PAINT,WM_PAINT,PM_REMOVE))
									::DispatchMessage(&message);
								// Allow the progress report dialog to process the Cancel
								// button.
								if (::PeekMessage(&message, rielp.Hwnd(), 0, 0, PM_REMOVE))
									::DispatchMessage(&message);
							}
						}
						// Now we need to reload the possibility list, with the newly created
						// information.
						qpli->FullRefresh();
					}
					catch (...)
					{
						StrApp str(kstidCannotLaunchListEditor);
						::MessageBox(m_hwnd, str.Chars(), strCaption.Chars(),
							MB_OK | MB_ICONSTOP);
						break;
					}
				}
			}
			// Show all the main windows for the app that we hid before, activating our main
			// window.
			::ShowWindow(hwndActive, SW_SHOW);
			for (int iwnd = 0; iwnd < vhwnd.Size(); ++iwnd)
				::ShowWindow(vhwnd[iwnd], SW_SHOWNA);
			rielp.Close();	// This destroys the modeless dialog window if it is still open.
		}
	}

	// Display the first new entry.
	if (m_imdst == kidstAppend && chvoOrig)
		::SendMessage(prnmw->Hwnd(), WM_COMMAND, kcidDataNext, 0);
	else
		::SendMessage(prnmw->Hwnd(), WM_COMMAND, kcidDataFirst, 0);
}


/*----------------------------------------------------------------------------------------------
	Delete any filters which reference a now nonexistent list item.

	@param pode Pointer to the database object.
----------------------------------------------------------------------------------------------*/
void RnImportWizard::DeleteInvalidFilters(IOleDbEncap * pode)
{
	AssertPtr(pode);
	try
	{
		StrUni stuQuery(L"select f.id, c.Contents, c.Contents_Fmt"
			L" from CmFilter f"
			L" left outer join CmFilter_Rows fr on fr.Src = f.id"
			L" left outer join CmRow_Cells rc on rc.Src = fr.Dst"
			L" left outer join CmCell c on c.id = rc.Dst"
			L" where c.Contents_Fmt is not null");
		IOleDbCommandPtr qodc;
		ComBool fMoreRows;
		ComBool fIsNull;
		ULONG cbSpaceTaken;
		CheckHr(pode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		ITsStrFactoryPtr qtsf;
		qtsf.CreateInstance(CLSID_TsStrFactory);
		Vector<HVO> vhvo;
		ComVector<ITsString> vqtss;
		while (fMoreRows)
		{
			HVO hvo;
			wchar rgchContents[4000];
			int cchContents;
			byte rgbFmt[8000];
			ULONG cbFmt;
			ITsStringPtr qtss;
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hvo),
				isizeof(hvo), &cbSpaceTaken, &fIsNull, 0));
			Assert(!fIsNull);
			CheckHr(qodc->GetColValue(2, reinterpret_cast <BYTE *>(rgchContents),
				sizeof(rgchContents), &cbSpaceTaken, &fIsNull, 0));
			cchContents = cbSpaceTaken / sizeof(wchar);
			Assert(!fIsNull);
			CheckHr(qodc->GetColValue(3, reinterpret_cast <BYTE *>(rgbFmt),
				sizeof(rgbFmt), &cbFmt, &fIsNull, 0));
			Assert(!fIsNull);
			CheckHr(qodc->NextRow(&fMoreRows));
			Assert(sizeof(int) == sizeof(ULONG));
			CheckHr(qtsf->DeserializeStringRgch(rgchContents, &cchContents, rgbFmt,
				reinterpret_cast<int *>(&cbFmt), &qtss));
			vhvo.Push(hvo);
			vqtss.Push(qtss);
		}
		qodc.Clear();
		if (!vhvo.Size())
			return;
		int cDelFilters = 0;
		for (int ie = 0; ie < vhvo.Size(); ++ie)
		{
			// Extract any object references from the string, and check whether the objects
			// still exist.
			int crun;
			CheckHr(vqtss[ie]->get_RunCount(&crun));
			for (int irun = 0; irun < crun; ++irun)
			{
				TsRunInfo tri;
				ITsTextPropsPtr qttp;
				CheckHr(vqtss[ie]->FetchRunInfo(irun, &tri, &qttp));
				SmartBstr sbstr;
				CheckHr(qttp->GetStrPropValue(ktptObjData, &sbstr));
				if (sbstr.Length() && sbstr.Chars()[0] == kodtNameGuidHot)
				{
					GUID guidHot;
					memcpy(&guidHot, sbstr.Chars() + 1, sizeof(guidHot));
					stuQuery.Format(
						L"select [Id] from CmObject where Guid$ = '%g'",
						&guidHot);
					CheckHr(pode->CreateCommand(&qodc));
					CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
					CheckHr(qodc->GetRowset(0));
					CheckHr(qodc->NextRow(&fMoreRows));
					qodc.Clear();
					if (!fMoreRows)
					{
						// Target no longer exists: delete this filter.
						stuQuery.Format(L"EXEC DeleteObjects '%d'", vhvo[ie]);
						CheckHr(pode->CreateCommand(&qodc));
						CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtStoredProcedure));
						++cDelFilters;
					}
					break;
				}
			}
		}
		// Message to the user?
		StrApp str;
		if (cDelFilters == 1)
		{
			str.Load(kstidImportDelOneFilter);
		}
		else if (cDelFilters > 1)
		{
			StrApp strFmt(kstidImportDelMultipleFilters);
			str.Format(strFmt.Chars(), cDelFilters);
		}
		if (str.Length())
		{
			StrApp strCaption(kstidImportMsgCaption);
			::MessageBox(NULL, str.Chars(), strCaption.Chars(), MB_OK | MB_ICONINFORMATION);
		}
	}
	catch (...)
	{
	}
}


/*----------------------------------------------------------------------------------------------
	Trim leading and trailing spaces from a NUL terminated character string.

	@param pszIn Pointer to the input string.

	@return Pointer to the first non-space character in the input string.
----------------------------------------------------------------------------------------------*/
static char * TrimSpaces(char * pszIn)
{
	if (!pszIn)
		return NULL;
	char * pszVal = pszIn + strspn(pszIn, kszSpace);
	char * psz = pszVal + strlen(pszVal);
	while (--psz >= pszVal)
	{
		if (!isascii(*psz) || !isspace(*psz))
			break;
		*psz = '\0';
	}
	return pszVal;
}

/*----------------------------------------------------------------------------------------------
	Create a new object of the given type in the database.

	@param pode Pointer to the database object.
	@param pmdc Pointer to the database meta data cache.
	@param clid Type (class) of the desired new object.
	@param hvoOwner Database id of the owner for the new object.
	@param flid Field id (property) of the owner.
	@param ord Relative order in a sequence property, or a negative number to store at end of a
					collection/sequence property.
	@param phvoNew Pointer to HVO for storing the database id of the newly created object upon
					successful return.

	@return S_OK, or an appropriate error code.
----------------------------------------------------------------------------------------------*/
HRESULT RnImportWizard::MakeNewObject(IOleDbEncap * pode, IFwMetaDataCache * pmdc, int clid,
	HVO hvoOwner, int flid, int ord, HVO * phvoNew)
{
	AssertPtr(pode);
	AssertPtr(pmdc);
	Assert(hvoOwner != 0);
	AssertPtr(phvoNew);
	//  Execute the stored procedure CreateOwnedObject$(clid, hvo, guid, hvoOwner, flid, ord, 1)
	//  to create a new object (with no values).
	try
	{
		int chvo = 0;
		GetVecSize(pode, pmdc, hvoOwner, flid, &chvo);
		int nFldType;
		CheckHr(pmdc->GetFieldType(flid, &nFldType));
		IOleDbCommandPtr qodc;
		CheckHr(pode->CreateCommand(&qodc));
		StrUni stuQuery;
		HVO hvoBefore = 0;
		if ((uint)ord < (uint)chvo)
		{
			// get the HVO at position ord from cache. The property is a sequence and we will
			// insert before this object.
			GetVecItem(pode, hvoOwner, flid, ord, &hvoBefore);
			stuQuery.Format(L"set IDENTITY_INSERT CmObject OFF; "
				L"exec CreateOwnedObject$ %d, ? output, null, %d, %d, %d, %d",
				clid, hvoOwner, flid, nFldType, hvoBefore);
		}
		if (hvoBefore == 0)
		{
			stuQuery.Format(L"set IDENTITY_INSERT CmObject OFF; "
				L"exec CreateOwnedObject$ %d, ? output, null, %d, %d, %d, null",
				clid, hvoOwner, flid, nFldType);
		}
		HVO hvoNew = 0;
		qodc->SetParameter(1, DBPARAMFLAGS_ISOUTPUT, NULL, DBTYPE_I4, (ULONG *) &hvoNew,
			sizeof(HVO));
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtStoredProcedure));
		ComBool fIsNull;
		qodc->GetParameter(1, reinterpret_cast<BYTE *>(&hvoNew), sizeof(HVO), &fIsNull);
		if (fIsNull)
			return E_FAIL;

		*phvoNew = hvoNew;
		return S_OK;
	}
	catch (Throwable & thr)
	{
		return thr.Error();
	}
	catch (...)
	{
		return E_FAIL;
	}
}

/*----------------------------------------------------------------------------------------------
	Get the desired item from the given vector property of the given object.

	@param pode Pointer to the database object.
	@param hvoOwner Database id of the object.
	@param flid
	@param ord
	@param phvoItem

	@return S_OK, or an appropriate error code.
----------------------------------------------------------------------------------------------*/
HRESULT RnImportWizard::GetVecItem(IOleDbEncap * pode, HVO hvoOwner, int flid, int ord,
	HVO * phvoItem)
{
	AssertPtr(pode);
	Assert(hvoOwner != 0);
	AssertPtr(phvoItem);
	try
	{
		StrUni stuQuery;
		stuQuery.Format(L"SELECT cm.id FROM CmObject cm "
			L"WHERE cm.Owner$ = %d AND cm.flid = cm.OwnFlid$ = %d AND cm.ord = %d",
			hvoOwner, flid, ord);
		IOleDbCommandPtr qodc;
		CheckHr(pode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		ComBool fMoreRows;
		CheckHr(qodc->NextRow(&fMoreRows));
		if (fMoreRows)
		{
			HVO hvo;
			ComBool fIsNull;
			ULONG cbSpaceTaken;
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&hvo), isizeof(hvo),
				&cbSpaceTaken, &fIsNull, 0));
			Assert(!fIsNull);
			*phvoItem = hvo;
			return S_OK;
		}
		else
		{
			return E_FAIL;
		}
	}
	catch (Throwable & thr)
	{
		return thr.Error();
	}
	catch (...)
	{
		return E_FAIL;
	}
}

/*----------------------------------------------------------------------------------------------
	Get the number of items in the given vector property of the given object.

	@param pode Pointer to the database object.
	@param pmdc Pointer to the database meta data cache.
	@param hvo Database id of the object.
	@param flid Field id of the vector property.
	@param pchvo Pointer to integer for containing the vector size on successful return.

	@return S_OK, or an appropriate error code.
----------------------------------------------------------------------------------------------*/
HRESULT RnImportWizard::GetVecSize(IOleDbEncap * pode, IFwMetaDataCache * pmdc, HVO hvo,
	int flid, int * pchvo)
{
	AssertPtr(pode);
	AssertPtr(pmdc);
	Assert(hvo != 0);
	AssertPtr(pchvo);

	try
	{
		SmartBstr sbstrField;
		CheckHr(pmdc->GetFieldName(flid, &sbstrField));
		SmartBstr sbstrClass;
		CheckHr(pmdc->GetOwnClsName(flid, &sbstrClass));
		StrUni stuQuery;
		stuQuery.Format(L"select COUNT(*) from %s_%s t where t.Src = %d",
			sbstrClass.Chars(), sbstrField.Chars(), hvo);
		IOleDbCommandPtr qodc;
		CheckHr(pode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		ComBool fMoreRows;
		CheckHr(qodc->NextRow(&fMoreRows));
		if (fMoreRows)
		{
			int chvo;
			ComBool fIsNull;
			ULONG cbSpaceTaken;
			CheckHr(qodc->GetColValue(1, reinterpret_cast<BYTE *>(&chvo), isizeof(chvo),
				&cbSpaceTaken, &fIsNull, 0));
			Assert(!fIsNull);
			*pchvo = chvo;
			return S_OK;
		}
		else
		{
			return E_FAIL;
		}
	}
	catch (Throwable & thr)
	{
		return thr.Error();
	}
	catch (...)
	{
		return E_FAIL;
	}
}

/*----------------------------------------------------------------------------------------------
	Set the given integer value for the given object.

	@param pode Pointer to the database object.
	@param pmdc Pointer to the database meta data cache.
	@param hvo Database id of the object.
	@param flid Field id  of the integer property.
	@param nVal Integer value to store.

	@return S_OK, or an appropriate error code.
----------------------------------------------------------------------------------------------*/
HRESULT RnImportWizard::SetInt(IOleDbEncap * pode, IFwMetaDataCache * pmdc, HVO hvo, int flid,
	int nVal)
{
	AssertPtr(pode);
	AssertPtr(pmdc);
	Assert(hvo != 0);
	try
	{
		SmartBstr sbstrField;
		CheckHr(pmdc->GetFieldName(flid, &sbstrField));
		SmartBstr sbstrClass;
		CheckHr(pmdc->GetOwnClsName(flid, &sbstrClass));
		StrUni stuQuery;
		stuQuery.Format(L"update [%s] set [%s]=? where id=%d",
			sbstrClass.Chars(), sbstrField.Chars(), hvo);
		IOleDbCommandPtr qodc;
		CheckHr(pode->CreateCommand(&qodc));
		CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL,
			// Note: DBTYPE_I4 will work for bool, tinyint, smallint, and int. If
			// the user attempts to set a value that is too large for the destination
			// the ExecCommand will return a failure.
			DBTYPE_I4, reinterpret_cast<ULONG *>(&nVal), sizeof(nVal)));
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtNoResults));
		return S_OK;
	}
	catch (Throwable & thr)
	{
		return thr.Error();
	}
	catch (...)
	{
		return E_FAIL;
	}
}

/*----------------------------------------------------------------------------------------------
	Set the given time value for the given object.

	@param pode Pointer to the database object.
	@param pmdc Pointer to the database meta data cache.
	@param hvo Database id of the object.
	@param flid Field id  of the time property.
	@param nTime Time value to store.

	@return S_OK, or an appropriate error code.
----------------------------------------------------------------------------------------------*/
HRESULT RnImportWizard::SetTime(IOleDbEncap * pode, IFwMetaDataCache * pmdc, HVO hvo, int flid,
	int64 nTime)
{
	AssertPtr(pode);
	AssertPtr(pmdc);
	Assert(hvo != 0);
	try
	{
		SmartBstr sbstrField;
		CheckHr(pmdc->GetFieldName(flid, &sbstrField));
		SmartBstr sbstrClass;
		CheckHr(pmdc->GetOwnClsName(flid, &sbstrClass));
		StrUni stuQuery;
		stuQuery.Format(L"update [%s] set [%s]=? where id=%d",
			sbstrClass.Chars(), sbstrField.Chars(), hvo);
		SilTime stim(nTime);
		SilTimeInfo sti;
		stim.GetTimeInfo(&sti);
		DBTIMESTAMP dbts;
		dbts.year = (short)sti.year;
		dbts.month = (short)sti.ymon;
		dbts.day = (short)sti.mday;
		dbts.hour = (short)sti.hour;
		dbts.minute = (short)sti.min;
		dbts.second = (short)sti.sec;
		dbts.fraction = sti.msec * 1000000;
		IOleDbCommandPtr qodc;
		CheckHr(pode->CreateCommand(&qodc));
		CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_DBTIMESTAMP,
			reinterpret_cast<ULONG *>(&dbts), sizeof(DBTIMESTAMP)));
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtNoResults));
		return S_OK;
	}
	catch (Throwable & thr)
	{
		return thr.Error();
	}
	catch (...)
	{
		return E_FAIL;
	}
}

/*----------------------------------------------------------------------------------------------
	Set the given string value for the given object.

	@param pode Pointer to the database object.
	@param pmdc Pointer to the database meta data cache.
	@param hvo Database id of the object.
	@param flid Field id  of the string property.
	@param ptss Pointer to string value to store.

	@return S_OK, or an appropriate error code.
----------------------------------------------------------------------------------------------*/
HRESULT RnImportWizard::SetString(IOleDbEncap * pode, IFwMetaDataCache * pmdc, HVO hvo,
	int flid, ITsString * ptss)
{
	AssertPtr(pode);
	AssertPtr(pmdc);
	Assert(hvo != 0);
	AssertPtr(ptss);
	try
	{
		// First, make sure that the string is normalized.
		ITsStringPtr qtssNFD;
		ComBool fEqual;
		CheckHr(ptss->get_NormalizedForm(knmNFD, &qtssNFD));
		const int kcbFmtBufMax = 1024;
		const int kcchTextMax = 1024;
		Vector<byte> vbFmt;
		vbFmt.Resize(kcbFmtBufMax, true);
		int cbFmtBufSize = kcbFmtBufMax;
		int cbFmtSpaceTaken;
		SmartBstr sbstrText;
		int cchText;
		int nFldType;
		CheckHr(pmdc->GetFieldType(flid, &nFldType));
		if (nFldType != kcptBigString && nFldType != kcptMultiBigString &&
			nFldType != kcptBigUnicode && nFldType != kcptMultiBigUnicode)
		{
			CheckHr(qtssNFD->get_Length(&cchText));
			if (cchText > kcchTextMax)
			{
				// Notify the user and abandon the save.
				StrApp strOverflow(kstidOverflowText);
				StrApp str;
				str.Format(strOverflow.Chars(), kcchTextMax);
				::MessageBox(NULL, str.Chars(), _T("Error"), MB_OK);
				return E_FAIL;
			}
		}
		CheckHr(qtssNFD->get_Text(&sbstrText));
		HRESULT hr = qtssNFD->SerializeFmtRgb(vbFmt.Begin(), cbFmtBufSize, &cbFmtSpaceTaken);
		if (hr != S_OK)
		{
			if (hr == S_FALSE)
			{
				// If the supplied buffer is too small, try it again with the value
				// that cbFmtSpaceTaken was set to. If this fails, throw error.
				vbFmt.Resize(cbFmtSpaceTaken, true);
				cbFmtBufSize = cbFmtSpaceTaken;
				CheckHr(qtssNFD->SerializeFmtRgb(vbFmt.Begin(), cbFmtBufSize,
					&cbFmtSpaceTaken));
			}
			else
			{
				return hr;
			}
		}
		SmartBstr sbstrField;
		CheckHr(pmdc->GetFieldName(flid, &sbstrField));
		SmartBstr sbstrClass;
		CheckHr(pmdc->GetOwnClsName(flid, &sbstrClass));
		StrUni stuQuery;
		stuQuery.Format(L"update [%s] set [%s]=?, %s_Fmt=? where id=%d",
			sbstrClass.Chars(), sbstrField.Chars(), sbstrField.Chars(), hvo);
		IOleDbCommandPtr qodc;
		CheckHr(pode->CreateCommand(&qodc));
		CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
			(ULONG *)sbstrText.Chars(), sbstrText.Length() * 2));
		CheckHr(qodc->SetParameter(2, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_BYTES,
			reinterpret_cast<ULONG *>(vbFmt.Begin()), cbFmtSpaceTaken));
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtNoResults));
		return S_OK;
	}
	catch (Throwable & thr)
	{
		return thr.Error();
	}
	catch (...)
	{
		return E_FAIL;
	}
}

/*----------------------------------------------------------------------------------------------
	Set the given unicode value for the given object.

	@param pode Pointer to the database object.
	@param pmdc Pointer to the database meta data cache.
	@param hvo Database id of the object.
	@param flid Field id  of the unicode property.
	@param prgchText Pointer to Unicode value to store.
	@param cchText Size of the Unicode value to store.

	@return S_OK, or an appropriate error code.
----------------------------------------------------------------------------------------------*/
HRESULT RnImportWizard::SetUnicode(IOleDbEncap * pode, IFwMetaDataCache * pmdc, HVO hvo,
	int flid, OLECHAR * prgchText, int cchText)
{
	AssertPtr(pode);
	AssertPtr(pmdc);
	Assert(hvo != 0);
	AssertArray(prgchText, cchText);
	try
	{
		SmartBstr sbstrField;
		CheckHr(pmdc->GetFieldName(flid, &sbstrField));
		SmartBstr sbstrClass;
		CheckHr(pmdc->GetOwnClsName(flid, &sbstrClass));
		StrUni stuQuery;
		stuQuery.Format(L"update [%s] set [%s]=? where id=%d",
			sbstrClass.Chars(), sbstrField.Chars(), sbstrField.Chars(), hvo);
		IOleDbCommandPtr qodc;
		CheckHr(pode->CreateCommand(&qodc));
		StrUni stu(prgchText, cchText);
		StrUtil::NormalizeStrUni(stu, UNORM_NFD);
		CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
			(ULONG *)stu.Chars(), stu.Length() * isizeof(OLECHAR)));
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtNoResults));
		return S_OK;
	}
	catch (Throwable & thr)
	{
		return thr.Error();
	}
	catch (...)
	{
		return E_FAIL;
	}
}

/*----------------------------------------------------------------------------------------------
	Set the given multilingual string value for the given object.

	@param pode Pointer to the database object.
	@param pmdc Pointer to the database meta data cache.
	@param hvo Database id of the object.
	@param flid Field id  of the string property.
	@param ws Writing system for this alternative of the multilingual string.
	@param ptss Pointer to string value to store.

	@return S_OK, or an appropriate error code.
----------------------------------------------------------------------------------------------*/
HRESULT RnImportWizard::SetMultiStringAlt(IOleDbEncap * pode, IFwMetaDataCache * pmdc, HVO hvo,
	int flid, int ws, ITsString * ptss)
{
	AssertPtr(pode);
	AssertPtr(pmdc);
	Assert(hvo != 0);
	AssertPtr(ptss);
	try
	{
		// First, make sure that the label string is normalized.  This may be paranoid overkill.
		ITsStringPtr qtssNFD;
		CheckHr(ptss->get_NormalizedForm(knmNFD, &qtssNFD));

		const int kcbFmtBufMax = 1024;
		const int kcchTextMax = 1024;
		Vector<byte> vbFmt;
		vbFmt.Resize(kcbFmtBufMax, true);
		int cbFmtBufSize = kcbFmtBufMax;
		int cbFmtSpaceTaken;
		SmartBstr sbstrText;
		int cchText;
		int nFldType;
		CheckHr(pmdc->GetFieldType(flid, &nFldType));
		if (nFldType != kcptMultiBigString && nFldType != kcptMultiBigUnicode)
		{
			CheckHr(qtssNFD->get_Length(&cchText));
			if (cchText > kcchTextMax)
			{
				// Notify the user and abandon the save.
				StrApp strOverflow(kstidOverflowText);
				StrApp str;
				str.Format(strOverflow.Chars(), kcchTextMax);
				::MessageBox(NULL, str.Chars(), _T("Error"), MB_OK);
				return E_FAIL;
			}
		}
		CheckHr(qtssNFD->get_Text(&sbstrText));
		HRESULT hr = qtssNFD->SerializeFmtRgb(vbFmt.Begin(), cbFmtBufSize, &cbFmtSpaceTaken);
		if (hr != S_OK)
		{
			if (hr == S_FALSE)
			{
				// If the supplied buffer is too small, try it again with the value
				// that cbFmtSpaceTaken was set to. If this fails, throw error.
				vbFmt.Resize(cbFmtSpaceTaken, true);
				cbFmtBufSize = cbFmtSpaceTaken;
				CheckHr(qtssNFD->SerializeFmtRgb(vbFmt.Begin(), cbFmtBufSize,
					&cbFmtSpaceTaken));
			}
			else
			{
				return hr;
			}
		}
		StrUni stuQuery;
		SmartBstr sbstrFieldName;
		CheckHr(pmdc->GetFieldName(flid, &sbstrFieldName));
		SmartBstr sbstrClassName;
		CheckHr(pmdc->GetOwnClsName(flid, &sbstrClassName));

		switch (nFldType)
		{
		case kcptMultiString:
			stuQuery.Format(L"DELETE FROM MultiStr$ WHERE Flid=%d AND Obj=%d AND Ws=%d;%n"
				L"INSERT INTO MultiStr$ (Flid,Obj,Ws,Txt,Fmt) VALUES (%d, %d, %d, ?, ?)",
				flid, hvo, ws, flid, hvo, ws);
			break;
		case kcptMultiBigString:
			stuQuery.Format(L"DELETE FROM MultiBigStr$ WHERE Flid=%d AND Obj=%d AND Ws=%d;%n"
				L"INSERT INTO MultiBigStr$ (Flid,Obj,Ws,Txt,Fmt) VALUES (%d, %d, %d, ?, ?)",
				flid, hvo, ws, flid, hvo, ws);
			break;
		case kcptMultiUnicode:
			stuQuery.Format(L"DELETE FROM %s_%s WHERE Flid=%d AND Obj=%d AND Ws=%d;%n"
				L"INSERT INTO %s_%s (Flid, Obj, Ws, Txt) VALUES (%d, %d, %d, ?)",
				sbstrClassName.Chars(), sbstrFieldName.Chars(), flid, hvo, ws,
				sbstrClassName.Chars(), sbstrFieldName.Chars(), flid, hvo, ws);
			break;
		case kcptMultiBigUnicode:
			stuQuery.Format(L"DELETE FROM MultiBigTxt$ WHERE Flid=%d AND Obj=%d AND Ws=%d;%n"
				L"INSERT INTO MultiBigTxt$ (Flid, Obj, Ws, Txt) VALUES (%d, %d, %d, ?)",
				flid, hvo, ws, flid, hvo, ws);
			break;
		default:
			return E_UNEXPECTED;
		}
		IOleDbCommandPtr qodc;
		CheckHr(pode->CreateCommand(&qodc));
		CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
			(ULONG *)sbstrText.Chars(), sbstrText.Length() * 2));
		if (nFldType == kcptMultiString || nFldType == kcptMultiBigString)
		{
			CheckHr(qodc->SetParameter(2, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_BYTES,
				reinterpret_cast<ULONG *>(vbFmt.Begin()), cbFmtSpaceTaken));
		}
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtNoResults));
		return S_OK;
	}
	catch (Throwable & thr)
	{
		return thr.Error();
	}
	catch (...)
	{
		return E_FAIL;
	}
}


/*----------------------------------------------------------------------------------------------
	Set the given object reference value for the given object.

	@param pode Pointer to the database object.
	@param pmdc Pointer to the database meta data cache.
	@param hvo Database id of the object.
	@param flid Field id  of the object reference property.
	@param punk Pointer to object to store.

	@return S_OK, or an appropriate error code.
----------------------------------------------------------------------------------------------*/
HRESULT RnImportWizard::SetUnknown(IOleDbEncap * pode, IFwMetaDataCache * pmdc, HVO hvo,
	int flid, IUnknown * punk)
{
	AssertPtr(pode);
	AssertPtr(pmdc);
	Assert(hvo != 0);
	AssertPtr(punk);
	try
	{
		SmartBstr sbstrField;
		CheckHr(pmdc->GetFieldName(flid, &sbstrField));
		SmartBstr sbstrClass;
		CheckHr(pmdc->GetOwnClsName(flid, &sbstrClass));
		Vector<byte> vbFmt;
		const int kcbFmtBufMax = 1024;
		vbFmt.Resize(kcbFmtBufMax, true);
		int cbFmtBufSize = kcbFmtBufMax;
		int cbFmtSpaceTaken;
		IOleDbCommandPtr qodc;
		CheckHr(pode->CreateCommand(&qodc));
		StrUni stuQuery;
		if (punk)
		{
			ITsTextPropsPtr qttp;
			punk->QueryInterface(IID_ITsTextProps, (void **)&qttp);
			if (qttp)
			{
				CheckHr(qttp->SerializeRgb(vbFmt.Begin(), cbFmtBufSize, &cbFmtSpaceTaken));
				if (cbFmtSpaceTaken > cbFmtBufSize)
				{
					vbFmt.Resize(cbFmtSpaceTaken, true);
					cbFmtBufSize = cbFmtSpaceTaken;
					CheckHr(qttp->SerializeRgb(vbFmt.Begin(), cbFmtBufSize, &cbFmtSpaceTaken));
				}
			}
			//  Add more clauses here if we support more types.
			//  Each should appropriately convert its type into rgbFmt, noting the
			//  length in cbFmtSpaceTaken.
			else
			{
				//  Not a type of object we know how to convert to binary
				return E_NOTIMPL;
			}
			stuQuery.Format(L"update [%s] set [%s]=? where id=%d", sbstrClass.Chars(),
				sbstrField.Chars(), hvo);
			CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_BYTES,
				reinterpret_cast<ULONG *>(vbFmt.Begin()), cbFmtSpaceTaken));
		}
		else
		{
			//  Null object: set prop to null
			stuQuery.Format(L"update [%s] set [%s]=null where id=%d",
				sbstrClass.Chars(), sbstrField.Chars(), hvo);
		}
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtStoredProcedure));
		return S_OK;
	}
	catch (Throwable & thr)
	{
		return thr.Error();
	}
	catch (...)
	{
		return E_FAIL;
	}
}

/*----------------------------------------------------------------------------------------------
	Set the given object reference value for the given object.

	@param pode Pointer to the database object.
	@param pmdc Pointer to the database meta data cache.
	@param hvo Database id of the owner object.
	@param flid Field id  of the object reference property.
	@param hvoObj Database id of the property object.

	@return S_OK, or an appropriate error code.
----------------------------------------------------------------------------------------------*/
HRESULT RnImportWizard::SetObjProp(IOleDbEncap * pode, IFwMetaDataCache * pmdc, HVO hvo,
	int flid, HVO hvoObj)
{
	AssertPtr(pode);
	AssertPtr(pmdc);
	Assert(hvo != 0);

	try
	{
		SmartBstr sbstrField;
		CheckHr(pmdc->GetFieldName(flid, &sbstrField));
		SmartBstr sbstrClass;
		CheckHr(pmdc->GetOwnClsName(flid, &sbstrClass));

		IOleDbCommandPtr qodc;
		CheckHr(pode->CreateCommand(&qodc));
		StrUni stuQuery;
		if (hvoObj)
			stuQuery.Format(L"update [%s] set [%s]=%d where id=%d",
				sbstrClass.Chars(), sbstrField.Chars(), hvoObj, hvo);
		else
			stuQuery.Format(L"update [%s] set [%s]=null where id=%d",
				sbstrClass.Chars(), sbstrField.Chars(), hvo);
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtStoredProcedure));

		return S_OK;
	}
	catch (Throwable & thr)
	{
		return thr.Error();
	}
	catch (...)
	{
		return E_FAIL;
	}
}

/*----------------------------------------------------------------------------------------------
	Set the given range of object reference values for the given object.

	@param pode Pointer to the database object.
	@param pmdc Pointer to the database meta data cache.
	@param hvo Database id of the owner object.
	@param flid Field id  of the object reference property.
	@param prghvo Range of database ids of the property objects.
	@param chvoIns Number of property objects to insert.

	@return S_OK, or an appropriate error code.
----------------------------------------------------------------------------------------------*/
HRESULT RnImportWizard::AppendObjRefs(IOleDbEncap * pode, IFwMetaDataCache * pmdc, HVO hvo,
	int flid, HVO * prghvo, int chvoIns)
{
	AssertPtr(pode);
	AssertPtr(pmdc);
	Assert(hvo != 0);
	AssertArray(prghvo, chvoIns);
	if (!chvoIns)
		return S_OK;

	try
	{
		SmartBstr sbstrField;
		CheckHr(pmdc->GetFieldName(flid, &sbstrField));
		SmartBstr sbstrClass;
		CheckHr(pmdc->GetOwnClsName(flid, &sbstrClass));
		int nFldType;
		CheckHr(pmdc->GetFieldType(flid, &nFldType));
		StrUni stuQuery;
		StrUni stuIds;
		if (nFldType == kcptReferenceSequence)
		{
			// Make the XML document of ids to insert.
			// e.g., <root><Obj Id="4708" Ord="0"/><Obj Id="4708" Ord="1"/></root>
			int nOrd = 0;
			int ihvo;
			stuQuery.Format(L"declare @hdoc int;%n"
				L"exec sp_xml_preparedocument @hdoc output, '<root>");
			for (ihvo = 0; ihvo < chvoIns; ++ihvo, ++nOrd)
				stuQuery.FormatAppend(L"<Obj Id=\"%d\" Ord=\"%d\"/>", prghvo[ihvo], nOrd);
			stuQuery.FormatAppend(L"</root>';%n");
			// Fill in the rest of the query.
			stuQuery.FormatAppend(L"exec ReplaceRefSeq$ %d, %d, NULL, @hdoc;%n", flid, hvo);
			// Note: We do not need sp_xml_removedocument because ReplaceRefSeq$ provides
			// this automatically as long as the ninth parameter is missing.

			// Call the ReplaceRefSeq$ stored procedure.
			// TODO 1724 (PaulP): Check if the time stamp has changed for the sequence and
			// update  it so that other users are blocked from making changes.
			ComBool fIsNull;
			ComBool fMoreRows;
			IOleDbCommandPtr qodc;
			CheckHr(pode->CreateCommand(&qodc)); // Get clean command to get rid of parameters.
			CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtNoResults));
		}
		else if (nFldType == kcptReferenceCollection)
		{
			int ihvo;
			stuIds = L" ";
			for (ihvo = 0; ihvo < chvoIns; ++ihvo)
				stuIds.FormatAppend(L"%d,", prghvo[ihvo]);
			stuQuery.Format(L"exec ReplaceRefColl$ %d, %d, '%s', NULL",
				flid, hvo, stuIds.Chars());

			ComBool fIsNull;
			ComBool fMoreRows;
			// Get clean command to get rid of parameters.
			IOleDbCommandPtr qodc;
			CheckHr(pode->CreateCommand(&qodc));
			CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtStoredProcedure));
		}
		else
		{
			Assert(false);		// Do not use for atomic or owning properties!
		}
		return S_OK;
	}
	catch (Throwable & thr)
	{
		return thr.Error();
	}
	catch (...)
	{
		return E_FAIL;
	}
}

/*----------------------------------------------------------------------------------------------
	Scan the given field data for matching either the name or the abbreviation (as designated
	by the user) against the entries in the given possibility list.

	@param pode Pointer to the database object.
	@param pmdc Pointer to the database meta data cache.
	@param hvopssl Database id of the desired possibility list.
	@param pszFieldData Pointer to the field contents.
	@param sfm Reference to the field import specification.
	@param ridst Reference to the import destination data.
	@param hvoEntry Database id of the newly created entry.
	@param vhvopssNew Output list of database ids of possibilities added to the given list.
	@param pconAnal Pointer to the encoding converter for the default analysis writing system.
----------------------------------------------------------------------------------------------*/
void RnImportWizard::SetPossibility(IOleDbEncap * pode, IFwMetaDataCache * pmdc,
	char * pszFieldData, RnSfMarker & sfm, RnImportDest & ridst, HVO hvoEntry,
	Vector<HVO> & vhvopssNew, IEncConverter * pconAnal)
{
	AssertPtr(pode);
	AssertPtr(pmdc);
	AssertPsz(pszFieldData);
	Assert(hvoEntry != 0);
	Vector<PossItemData> vpidValues;
	ParsePossibilities(pszFieldData, sfm, vpidValues);
	if (!vpidValues.Size())
		return;

	HVO hvopssl = ridst.m_hvoPssl;
	ListInfo * pli = LoadPossibilityList(pode, hvopssl);
	if (!pli)
		return;						// Can't do anything without the possibility list!

	StrApp strCaption(kstidImportMsgCaption);
	StrApp strCannot(kstidCannotInsertListItem);
	int ipid;
	// Check whether we need to match against hierarchical names.  Also set the list HVO, and
	// convert the name to Unicode.
	bool fNeedHier = false;
	for (ipid = 0; ipid < vpidValues.Size(); ++ipid)
	{
		if (vpidValues[ipid].m_fHasHier)
			fNeedHier = true;
		SetListHvoAndConvertToUtf16(vpidValues[ipid], hvopssl, pconAnal);
	}

	int ipss = pli->m_vlii.Size();
	StrUni stu;
	StrUni stuHier;
	Vector<HVO> vhvopss;
	while (--ipss >= 0 && vpidValues.Size())
	{
		ListItemInfo & lii = pli->m_vlii[ipss];
		if (sfm.m_clo.m_pnt == kpntName)
			stu = lii.m_stuName;
		else
			stu = lii.m_stuAbbr;
		ipid = PossNameMatchesData(stu, vpidValues);
		if (ipid < 0 && fNeedHier)
		{
			GetHierName(pli, ipss, stuHier, sfm.m_clo.m_pnt);
			ipid = PossNameMatchesData(stuHier, vpidValues);
		}
		if (ipid >= 0)
		{
			int hvopss = lii.m_hvoItem;
			switch (ridst.m_proptype)
			{
			case kcptReferenceAtom:
				CheckHr(SetObjProp(pode, pmdc, hvoEntry, sfm.m_flidDst, hvopss));
				ipss = 0;	// force exit
				vpidValues.Clear();
				break;
			case kcptReferenceCollection:
			case kcptReferenceSequence:
				vhvopss.Push(hvopss);
				vpidValues.Delete(ipid);
				break;
			default:
				Assert(false);		// THIS SHOULD NEVER HAPPEN!
				break;
			}
		}
		else if (fNeedHier)
		{
			// Find out whether this possibility item partially matches any of the stored
			for (ipid = 0; ipid < vpidValues.Size(); ++ipid)
			{
				if (vpidValues[ipid].m_stuName.Length() <= stuHier.Length())
					continue;
				if (vpidValues[ipid].m_stuName.FindStrCI(stuHier) != 0)
					continue;
				if (vpidValues[ipid].m_stuName.GetAt(stuHier.Length()) != ':')
					continue;
				// we have a partial hierarchical match.
				if (stuHier.Length() > vpidValues[ipid].m_cchMatch)
				{
					vpidValues[ipid].m_hvoOwner = lii.m_hvoItem;
					vpidValues[ipid].m_cchMatch = stuHier.Length() + 1;
				}
			}
		}
	}
	if (vhvopss.Size())
	{
		CheckHr(AppendObjRefs(pode, pmdc, hvoEntry, sfm.m_flidDst, vhvopss.Begin(),
					vhvopss.Size()));
		vhvopss.Clear();
	}
	if (sfm.m_clo.m_fIgnoreNewStuff || pli->m_fIsClosed)
		return;				// Either we don't want to add new items, or we can't.
	if (!vpidValues.Size())
		return;				// We have nothing new to add.

	// Handle new hierarchical items.
	// Add these to the possibility list, and then to the object.
	int ipssIns;
	HVO hvoNew;
	StrUni stuAbbr;
	StrUni stuName;
	int ich;
	wchar szHierDelim[3] = { kchHierDelim, ' ', 0 };		// Normalized separator.
	// Trim off any leading part of a hierarchical name that has been matched.
	for (ipid = 0; ipid < vpidValues.Size(); ++ipid)
	{
		Assert(vpidValues[ipid].m_fHasHier || !vpidValues[ipid].m_cchMatch);
		if (vpidValues[ipid].m_cchMatch)
		{
			vpidValues[ipid].m_stuName.Replace(0, vpidValues[ipid].m_cchMatch, L"");
			ich = vpidValues[ipid].m_stuName.FindStr(szHierDelim);
			vpidValues[ipid].m_fHasHier = (ich >= 0);
		}
	}
	for (ipid = 0; ipid < vpidValues.Size(); ++ipid)
	{
		if (vpidValues[ipid].m_fHasHier)
		{
			ich = vpidValues[ipid].m_stuName.FindStr(szHierDelim);
			if (ich < 0)
				ich = vpidValues[ipid].m_stuName.FindStr(szHierDelim);
			while (vpidValues[ipid].m_fHasHier && vpidValues[ipid].m_stuName.Length())
			{
				Assert(ich >= 0);
				stuAbbr.Assign(vpidValues[ipid].m_stuName.Chars(), ich);
				if (stuAbbr.Length())
				{
					PossItemLocation pil;
					if (vpidValues[ipid].m_hvoOwner == hvopssl)
					{
						ipssIns = pli->m_vlii.Size() - 1;
						pil = kpilTop;
					}
					else
					{
						ipssIns = pli->m_vlii.Size() - 1;
						pli->m_hmhvoilii.Retrieve(vpidValues[ipid].m_hvoOwner, &ipssIns);
						pil = kpilUnder;
					}
					stuName.Assign(stuAbbr);
					hvoNew = InsertPss(pode, pmdc, pli, ipssIns,
						stuAbbr.Chars(), stuName.Chars(), pil);
					if (!hvoNew)
					{
						::MessageBox(m_hwnd, strCannot.Chars(), strCaption.Chars(),
							MB_OK | MB_ICONSTOP);
						break;
					}
					else
					{
						vpidValues[ipid].m_hvoOwner = hvoNew;
						vhvopssNew.Push(vpidValues[ipid].m_hvoOwner);
					}
				}
				int cchName = vpidValues[ipid].m_stuName.Length();
				if (cchName > ich)
					vpidValues[ipid].m_stuName.Replace(0, ich + 1, L"");
				else if (cchName)
					vpidValues[ipid].m_stuName.Replace(0, cchName, L"");
				ich = vpidValues[ipid].m_stuName.FindStr(szHierDelim);
				vpidValues[ipid].m_fHasHier = (ich >= 0);
			}
		}
		if (vpidValues[ipid].m_hvoOwner == hvopssl)
		{
			// Add a new item at the end of the list.
			// Make the name and the abbreviation the same initially.
			ipssIns = pli->m_vlii.Size() - 1;
			stuAbbr.Assign(vpidValues[ipid].m_stuName.Chars());
			stuName.Assign(stuAbbr);
			hvoNew = InsertPss(pode, pmdc, pli, ipssIns,stuAbbr.Chars(), stuName.Chars(),
				kpilTop);
			if (!hvoNew)
			{
				::MessageBox(m_hwnd, strCannot.Chars(), strCaption.Chars(),
					MB_OK | MB_ICONSTOP);
				continue;
			}
			vhvopss.Push(hvoNew);
		}
		else
		{
			ipssIns = pli->m_vlii.Size() - 1;
			pli->m_hmhvoilii.Retrieve(vpidValues[ipid].m_hvoOwner, &ipssIns);
			stuAbbr.Assign(vpidValues[ipid].m_stuName.Chars());
			stuName.Assign(stuAbbr);
			hvoNew = InsertPss(pode, pmdc, pli, ipssIns, stuAbbr.Chars(), stuName.Chars(),
				kpilUnder);
			if (!hvoNew)
			{
				::MessageBox(m_hwnd, strCannot.Chars(), strCaption.Chars(),
					MB_OK | MB_ICONSTOP);
				continue;
			}
			vhvopss.Push(hvoNew);
		}
	}
	if (vhvopss.Size())
	{
		switch (ridst.m_proptype)
		{
		case kcptReferenceAtom:
			CheckHr(SetObjProp(pode, pmdc, hvoEntry, sfm.m_flidDst, vhvopss[0]));
			vhvopssNew.Push(vhvopss[0]);
			break;
		case kcptReferenceCollection:
		case kcptReferenceSequence:
			{
				CheckHr(AppendObjRefs(pode, pmdc, hvoEntry, sfm.m_flidDst, vhvopss.Begin(),
					vhvopss.Size()));
				vhvopssNew.InsertMulti(vhvopssNew.Size(), vhvopss.Size(),
					vhvopss.Begin());
			}
			break;
		default:
			Assert(false);		// THIS SHOULD NEVER HAPPEN!
			break;
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Get the requested part of the name of the list item displaying the full hierarchy.
	The current item must be part of the list described by pli.

	@param pli Pointer to list information data structure.
	@param ilii Index of the list item in pli->m_vlii.
	@param stuHier Output string.
	@param pnt Type of name being matched against (kpntName or kpntAbbreviation).
----------------------------------------------------------------------------------------------*/
void RnImportWizard::GetHierName(ListInfo * pli, int ilii, StrUni & stuHier, PossNameType pnt)
{
	AssertPtr(pli);
	Assert(ilii >= 0);
	Assert(ilii < pli->m_vlii.Size());
	Assert(pnt == kpntName || pnt == kpntAbbreviation);

	stuHier.Clear();
	StrUni stuT;
	int nHier;

	for ( ; ; )
	{
		// Insert the name for the current item.
		if (pnt == kpntName)
			stuT = pli->m_vlii[ilii].m_stuName;
		else
			stuT = pli->m_vlii[ilii].m_stuAbbr;
		stuHier.Replace(0, 0, stuT);

		// Check for higher level names.
		nHier = pli->m_vlii[ilii].m_nHier;
		if (nHier == 1)
			return; // We are done.
		// Back up to the next higher possibility node.
		while (--ilii >= 0)
		{
			if (pli->m_vlii[ilii].m_nHier < nHier)
			{
				// Add a higher level name.
				stuHier.Replace(0, 0, &kchHierDelim, 1);
				break;
			}
		}
		Assert(ilii >= 0); // We should never get in an endless loop.
	}

}

/*----------------------------------------------------------------------------------------------
	Add this possibility to the list of possibilities, both in memory and in the database.

	@param pode Pointer to the database object.
	@param pmdc Pointer to the database meta data cache.
	@param pli Pointer to list information data structure.
	@param ipss
	@param pszAbbr
	@param pszName
	@param pil

	@return Database id of the newly created list item, or 0 if an error occurs.
----------------------------------------------------------------------------------------------*/
HVO RnImportWizard::InsertPss(IOleDbEncap * pode, IFwMetaDataCache * pmdc, ListInfo * pli,
	int ipss, const OLECHAR * pszAbbr, const OLECHAR * pszName, PossItemLocation pil)
{
	AssertPtr(pode);
	AssertPtr(pmdc);
	AssertPtr(pli);
	AssertPszN(pszAbbr);
	AssertPszN(pszName);
	if (!pszAbbr)
		pszAbbr = L"";
	if (!pszName)
		pszName = L"";

	try
	{
		HVO hvoOwner = pli->m_hvo;
		HVO hvoNew = 0;
		HVO hvoBefore = 0;
		int flid = kflidCmPossibilityList_Possibilities;
		int nHierLevel;
		int iliiIns;
		int ilii;
		int clii = pli->m_vlii.Size();
		if (clii == 0)
		{
			Assert(ipss <= 0);
			iliiIns = 0;
			nHierLevel = 1;
		}
		else
		{
			Assert(ipss >= 0);
			Assert(ipss < clii);
			nHierLevel = pli->m_vlii[ipss].m_nHier;
			int nHier;
			// CreateObject2 does an insert before. The interface
			// is currently written to do an insert before or an
			// insert after, depending on the value of pil.
			if (pil == kpilBefore)
			{
				hvoBefore = pli->m_vlii[ipss].m_hvoItem;
				iliiIns = ipss;
				for (ilii = 0; ilii < ipss; ++ilii)
				{
					nHier = pli->m_vlii[ilii].m_nHier;
					if (nHier < nHierLevel)
					{
						hvoOwner = pli->m_vlii[ilii].m_hvoItem;
						flid = kflidCmPossibility_SubPossibilities;
					}
				}
			}
			else if (pil == kpilAfter)
			{
				hvoBefore = 0;
				iliiIns = -1;
				for (ilii = 0; ilii < ipss; ++ilii)
				{
					nHier = pli->m_vlii[ilii].m_nHier;
					if (nHier < nHierLevel)
					{
						hvoOwner = pli->m_vlii[ilii].m_hvoItem;
						flid = kflidCmPossibility_SubPossibilities;
					}
				}

				// This loop in for the vector of possibility items
				for (ilii = ipss + 1; ilii < clii; ++ilii)
				{
					nHier = pli->m_vlii[ilii].m_nHier;
					if (nHier <= nHierLevel)
					{
						iliiIns = ilii;
						// If we're on the same hierarchy level, send it the item
						// clicked. Otherwise send the SQL function a 0 so that
						// it adds the item to the end of the list for the
						// hierarchy level.
						hvoBefore = nHier == nHierLevel ? pli->m_vlii[ilii].m_hvoItem: 0;
						break;
					}
				}
				if (iliiIns == -1)
					iliiIns = clii;
			}
			else if (pil == kpilUnder)
			{
				Assert(ipss < clii);
				hvoOwner = pli->m_vlii[ipss].m_hvoItem;
				flid = kflidCmPossibility_SubPossibilities;
				hvoBefore = 0;
				iliiIns = ipss + 1;
			}
			else // pil == kpilTop
			{
				nHierLevel = 1;
				HVO hvoBefore = 0;
				iliiIns = -1;

				// This loop is for the vector of possibility items
				for (ilii = ipss + 1; ilii < clii; ++ilii)
				{
					nHier = pli->m_vlii[ilii].m_nHier;
					if (nHier <= nHierLevel)
					{
						iliiIns = ilii;
						// If we're on the same hierarchy level, send it the item
						// clicked. Otherwise send the SQL function a 0 so that
						// it adds the item to the end of the list for the
						// hierarchy level.
						hvoBefore = nHier == nHierLevel ? pli->m_vlii[ilii].m_hvoItem: 0;
						break;
					}
				}
				if (iliiIns == -1)
					iliiIns = clii;
			}
		}

		// Add the new item to the database.
		// Execute the stored procedure
		//     CreateOwnedObject$(clid, hvo, guid, hvoOwner, flid, ord, 1)
		// to create a new object (with no values).
		ComBool fIsNull;
		StrUni stuQuery;
		IOleDbCommandPtr qodc;
		CheckHr(pode->CreateCommand(&qodc));
		qodc->SetParameter(1, DBPARAMFLAGS_ISOUTPUT, NULL, DBTYPE_I4, (ULONG *)&hvoNew,
			sizeof(HVO));
		stuQuery.Format(L"exec CreateOwnedObject$ %d, ? output, null, %d, %d, %d, ",
			pli->m_nItemClsid, hvoOwner, flid, kcptOwningSequence);
		if (hvoBefore)
			stuQuery.FormatAppend(L"%d", hvoBefore);
		else
			stuQuery.FormatAppend(L"null");
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtStoredProcedure));
		qodc->GetParameter(1, reinterpret_cast<BYTE *>(&hvoNew), sizeof(HVO), &fIsNull);
		if (!hvoNew)
			return 0;

		// Create the MultiTxt$ strings for Name and Abbreviation.
		ListItemInfo lii;
		lii.m_stuName.Assign(pszName);
		lii.m_stuAbbr.Assign(pszAbbr);
		if (!lii.m_stuName.Length() || !lii.m_stuAbbr.Length())
		{
			StrUni stuNewItem;
			StrUni stuNew;
			if (lii.m_stuName.Length())
			{
				stuNewItem = lii.m_stuName;
			}
			else
			{
				stuNewItem.Load(kstidNewItem);
				lii.m_stuName = stuNewItem;
			}
			if (lii.m_stuAbbr.Length())
			{
				stuNew = lii.m_stuAbbr;
			}
			else
			{
				stuNew.Load(kstidNew);
				lii.m_stuAbbr = stuNew;
			}
			StrUni stu;
			bool fDup;
			int cNewItems = 1;
			do
			{
				fDup = false;
				for (int i = 0; i < pli->m_vlii.Size(); ++i)
				{
					if (pli->m_vlii[i].m_stuName == lii.m_stuName)
					{
						fDup = true;
						++cNewItems;
						break;
					}
					if (pli->m_vlii[i].m_stuAbbr == lii.m_stuAbbr)
					{
						fDup = true;
						++cNewItems;
						break;
					}
				}
				if (fDup)
				{
					lii.m_stuName.Format(L"%s (%d)", stuNewItem.Chars(), cNewItems);
					lii.m_stuAbbr.Format(L"%s (%d)", stuNew.Chars(), cNewItems);
				}
			} while (fDup);
		}
		stuQuery.Format(L"exec SetMultiTxt$ %d, %d, %d, ?",
			kflidCmPossibility_Name, hvoNew, pli->m_ws);
		StrUtil::NormalizeStrUni(lii.m_stuName, UNORM_NFD);
		CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
			(ULONG *)lii.m_stuName.Chars(), lii.m_stuName.Length() * sizeof(OLECHAR)));
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtStoredProcedure));
		stuQuery.Format(L"exec SetMultiTxt$ %d, %d, %d, ?",
			kflidCmPossibility_Abbreviation, hvoNew, pli->m_ws);
		StrUtil::NormalizeStrUni(lii.m_stuAbbr, UNORM_NFD);
		CheckHr(qodc->SetParameter(1, DBPARAMFLAGS_ISINPUT, NULL, DBTYPE_WSTR,
			(ULONG *)lii.m_stuAbbr.Chars(), lii.m_stuAbbr.Length() * sizeof(OLECHAR)));
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtStoredProcedure));
		// Set create/modify times and default colors.
		stuQuery.Format(L"update CmPossibility set DateCreated = getdate(), "
			L"DateModified = getdate(), ForeColor = %d, BackColor = %d, UnderColor = %d "
			L"where id = %d", kclrTransparent, kclrTransparent, kclrTransparent, hvoNew);
		CheckHr(pode->CreateCommand(&qodc));
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtNoResults));

		// Add the new item to our internal vector of possibility items.
		lii.m_hvoItem = hvoNew;
		lii.m_nHier = nHierLevel;
		pli->m_vlii.Insert(iliiIns, lii);
		clii = pli->m_vlii.Size();
		// Add the new item to the hashmaps of possibility items.
		pli->m_hmhvoilii.Insert(lii.m_hvoItem, iliiIns);
		// Update the index stored in the hashmap for all items following the one we just
		// inserted.
		for (ilii = iliiIns + 1; ilii < clii; ++ilii)
			pli->m_hmhvoilii.Insert(pli->m_vlii[ilii].m_hvoItem, ilii, true);

		return hvoNew;
	}
	catch (...)
	{
		return 0;
	}
}


/*----------------------------------------------------------------------------------------------
 *	Store the list items in the vector in correct tree order. ("preorder" in this case).
 *  I.e., 1) visit the root.  2) visit the subtrees of the first tree (in preorder).  3) visit
 *  the remaining trees at the same level (in preorder).  See Knuth, I.334.
 ---------------------------------------------------------------------------------------------*/
void RnImportWizard::StoreListItemVector(Vector<ListItemInfoPlus> & vliix, int ix,
	Vector<ListItemInfo> & vlii)
{
	do
	{
		vlii.Push(vliix[ix].m_lii);
		if (vliix[ix].m_iFirstChild > 0)
			StoreListItemVector(vliix, vliix[ix].m_iFirstChild, vlii);
		ix = vliix[ix].m_iNextSibling;	// think of this loop as tail recursion if you like...
	} while (ix > 0);
}

/*----------------------------------------------------------------------------------------------
	Load the relevant values for the given possibility list into memory (if they aren't already
	loaded), and return a pointer to the filled in RnImportWizard::ListInfo data structure.

	@param pode Pointer to the database object.
	@param hvopssl Database id of the possibility list.

	@return Pointer to the loaded ListInfo structure, or NULL if an error occurs.
----------------------------------------------------------------------------------------------*/
RnImportWizard::ListInfo * RnImportWizard::LoadPossibilityList(IOleDbEncap * pode, HVO hvopssl)
{
	AssertPtr(pode);
	Assert(hvopssl != 0);

	int ili;
	if (m_hmhvoListili.Retrieve(hvopssl, &ili))
		return &m_vli[ili];

	try
	{
		ListInfo li;
		li.m_hvo = hvopssl;
		IOleDbCommandPtr qodc;
		CheckHr(pode->CreateCommand(&qodc));
		StrUni stuQuery;
		stuQuery.Format(L"select Depth, PreventChoiceAboveLevel, IsSorted, IsClosed,\n"
			L" PreventDuplicates, PreventNodeChoices, UseExtendedFields, DisplayOption,\n"
			L" ItemClsid, WritingSystem, WsSelector from CmPossibilityList where id = %d",
			li.m_hvo);
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtSelectWithOneRowset));
		CheckHr(qodc->GetRowset(0));
		ComBool fMoreRows;
		ComBool fIsNull;
		ULONG cbSpaceTaken;
		CheckHr(qodc->NextRow(&fMoreRows));
		if (fMoreRows)
		{
			CheckHr(qodc->GetColValue(1,
				reinterpret_cast<BYTE *>(&li.m_cMaxDepth), isizeof(li.m_cMaxDepth),
				&cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(2,
				reinterpret_cast<BYTE *>(&li.m_nPreventChoiceAboveLevel),
				isizeof(li.m_nPreventChoiceAboveLevel),
				&cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(3,
				reinterpret_cast<BYTE *>(&li.m_fIsSorted), isizeof(li.m_fIsSorted),
				&cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(4,
				reinterpret_cast<BYTE *>(&li.m_fIsClosed), isizeof(li.m_fIsClosed),
				&cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(5,
				reinterpret_cast<BYTE *>(&li.m_fAllowDuplicates),
				isizeof(li.m_fAllowDuplicates),
				&cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(6,
				reinterpret_cast<BYTE *>(&li.m_fPreventNodeChoices),
				isizeof(li.m_fPreventNodeChoices),
				&cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(7,
				reinterpret_cast<BYTE *>(&li.m_fUseExtendedFields),
				isizeof(li.m_fUseExtendedFields),
				&cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(8,
				reinterpret_cast<BYTE *>(&li.m_nDisplayOption), isizeof(li.m_nDisplayOption),
				&cbSpaceTaken, &fIsNull, 0));
			CheckHr(qodc->GetColValue(9,
				reinterpret_cast<BYTE *>(&li.m_nItemClsid), isizeof(li.m_nItemClsid),
				&cbSpaceTaken, &fIsNull, 0));
			li.m_ws = m_wsUser;
			CheckHr(qodc->GetColValue(10, reinterpret_cast<BYTE *>(&li.m_ws), isizeof(li.m_ws),
				&cbSpaceTaken, &fIsNull, 0));
			int wsSelector;
			CheckHr(qodc->GetColValue(11,
				reinterpret_cast<BYTE *>(&wsSelector), isizeof(wsSelector),
				&cbSpaceTaken, &fIsNull, 0));
			if (!fIsNull)
				li.m_ws = ActualWs(wsSelector);
		}
		else
		{
			return NULL;
		}
		// Execute the stored procedure to get all the Possibilities given the
		// PossibilityList and the writing system.
		stuQuery.Format(L"exec GetPossibilities %d, %d", hvopssl, li.m_ws);
		CheckHr(qodc->ExecCommand(stuQuery.Bstr(), knSqlStmtStoredProcedure));
		CheckHr(qodc->GetRowset(0));
		CheckHr(qodc->NextRow(&fMoreRows));
		if (fMoreRows)
		{
			int cpss;
			HVO hvoPss;
			int nHier;
			HVO hvoOwner;
			CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(&cpss),
				sizeof(int), &cbSpaceTaken, &fIsNull, 0));
			// Allocate memory for all the rows.
			Vector<ListItemInfoPlus> vliix;
			vliix.Resize(cpss);
			HashMap<HVO, int> hmhvoidx;

			// The second rowset gives the actual possibility items.
			CheckHr(qodc->GetRowset(0));
			CheckHr(qodc->NextRow(&fMoreRows));
			int ipss = 0;
			int wsNameAbbr =0;
			Vector<OLECHAR> vchNameAbbr;
			vchNameAbbr.Resize(1024, true);
			int cchNameAbbr;
			while (fMoreRows && ipss < cpss)
			{
				CheckHr(qodc->GetColValue(1, reinterpret_cast <BYTE *>(&hvoPss),
					sizeof(HVO), &cbSpaceTaken, &fIsNull, 0));
				CheckHr(qodc->GetColValue(2, reinterpret_cast <BYTE *>(vchNameAbbr.Begin()),
					sizeof(OLECHAR) * vchNameAbbr.Size(), &cbSpaceTaken, &fIsNull, 2));
				//  If buffer was too small, reallocate and try again.
				cchNameAbbr = cbSpaceTaken / isizeof(OLECHAR);
				if (cchNameAbbr > vchNameAbbr.Size() && !fIsNull)
				{
					vchNameAbbr.Resize(cchNameAbbr, true);
					CheckHr(qodc->GetColValue(2,
						reinterpret_cast <BYTE *>(vchNameAbbr.Begin()),
						sizeof(OLECHAR) * vchNameAbbr.Size(), &cbSpaceTaken, &fIsNull, 2));
				}
				CheckHr(qodc->GetColValue(3, reinterpret_cast <BYTE *>(&wsNameAbbr),
					sizeof(wsNameAbbr), &cbSpaceTaken, &fIsNull, 0));
				CheckHr(qodc->GetColValue(4, reinterpret_cast <BYTE *>(&nHier),
					sizeof(nHier), &cbSpaceTaken, &fIsNull, 0));
				// skip columns 5-8 -- they only have to do with presentation (color, etc)
				CheckHr(qodc->GetColValue(9, reinterpret_cast <BYTE *>(&hvoOwner),
					sizeof(hvoOwner), &cbSpaceTaken, &fIsNull, 0));
				CheckHr(qodc->NextRow(&fMoreRows));

				//  Set the item information in the possibility item vector.
				ListItemInfo & lii = vliix[ipss].m_lii;
				lii.m_hvoItem = hvoPss;
				StrUni stuNameAbbr;
				stuNameAbbr.Assign(vchNameAbbr.Begin());
				int ich = stuNameAbbr.FindStr(L" - ");
				lii.m_stuAbbr = stuNameAbbr.Left(ich);
				lii.m_stuName = stuNameAbbr.Right(stuNameAbbr.Length() - ich - 3);
				lii.m_nHier = nHier;
				li.m_hmhvoilii.Insert(lii.m_hvoItem, ipss);
				// Fix the tree index values for this list item.
				vliix[ipss].m_iFirstChild = 0;
				vliix[ipss].m_iNextSibling = 0;
				hmhvoidx.Insert(hvoPss, ipss);
				if (nHier > 1)
				{
					int idx;
					if (hmhvoidx.Retrieve(hvoOwner, &idx))
					{
						if (vliix[idx].m_iFirstChild == 0)
						{
							vliix[idx].m_iFirstChild = ipss;
						}
						else
						{
							idx = vliix[idx].m_iFirstChild;
							while (vliix[idx].m_iNextSibling != 0)
								idx = vliix[idx].m_iNextSibling;
							vliix[idx].m_iNextSibling = ipss;
						}
					}
					else
					{
						// WARN THE PROGRAMMER!
						Assert(hmhvoidx.Retrieve(hvoOwner, &idx));
					}
				}
				else
				{
					if (ipss > 0)
						vliix[ipss - 1].m_iNextSibling = ipss;
				}
				++ipss;
			}
			// The number returned above (cpss) is actually >= the actual number of rows
			// returned. So we resize the vector to the actual number of rows that were
			// returned.
			li.m_vlii.EnsureSpace(ipss, true);
			vliix.Resize(ipss);
			hmhvoidx.Clear();
			// See the comments in PossListInfo::LoadPossList() in AfDbInfo.cpp.
			if (ipss > 0)
				StoreListItemVector(vliix, 0, li.m_vlii);
		}
		ili = m_vli.Size();
		m_hmhvoListili.Insert(hvopssl, ili);
		m_vli.Push(li);
		return m_vli.Top();
	}
	catch (...)
	{
		return NULL;
	}
}

/*----------------------------------------------------------------------------------------------
	Default constructor.
----------------------------------------------------------------------------------------------*/
RnImportWizard::ListInfo::ListInfo()
{
	m_hvo = 0;
	m_cMaxDepth = 0;
	m_nPreventChoiceAboveLevel = 0;
	m_fIsSorted = 0;
	m_fIsClosed = 0;
	m_fAllowDuplicates = 1;
	m_fPreventNodeChoices = 0;
	m_fUseExtendedFields = 0;
	m_nDisplayOption = 0;
	m_nItemClsid = 0;
	m_ws = 0;
	m_vlii.Clear();
	m_hmhvoilii.Clear();
}

/*----------------------------------------------------------------------------------------------
	Copy constructor.

	@param liCopy Reference to a copy of the ListInfo data structure.
----------------------------------------------------------------------------------------------*/
RnImportWizard::ListInfo::ListInfo(const RnImportWizard::ListInfo & li)
{
	m_hvo = li.m_hvo;
	m_cMaxDepth = li.m_cMaxDepth;
	m_nPreventChoiceAboveLevel = li.m_nPreventChoiceAboveLevel;
	m_fIsSorted = li.m_fIsSorted;
	m_fIsClosed = li.m_fIsClosed;
	m_fAllowDuplicates = !li.m_fAllowDuplicates;
	m_fPreventNodeChoices = li.m_fPreventNodeChoices;
	m_fUseExtendedFields = li.m_fUseExtendedFields;
	m_nDisplayOption = li.m_nDisplayOption;
	m_nItemClsid = li.m_nItemClsid;
	m_ws = li.m_ws;

	m_vlii = li.m_vlii;

	const_cast<ListInfo &>(li).m_hmhvoilii.CopyTo(m_hmhvoilii);
}


/*----------------------------------------------------------------------------------------------
	Parse a delimiter field, possibly splitting it into multiple possible delimiters.

	@param staDelim Raw input delimiter specification string.
	@param vstaDelims Refers to output vector of delimiter strings.
----------------------------------------------------------------------------------------------*/
static void ParseDelimField(StrAnsi staDelim, Vector<StrAnsi> & vstaDelims)
{
	const char * pszDelim = staDelim.Chars() + strspn(staDelim.Chars(), kszSpace);
	int cch;
	StrAnsi sta;
	while (*pszDelim)
	{
		cch = strcspn(pszDelim, kszSpace);
		sta.Assign(pszDelim, cch);
		vstaDelims.Push(sta);
		pszDelim += cch;
		pszDelim += strspn(pszDelim, kszSpace);
	}
}

/*----------------------------------------------------------------------------------------------
	Parse the input data to build a list of possibility names (or abbreviations) contained
	therein.

	@param pszFieldData Points to the raw input data field.
	@param sfm Refers to the field import specification.
	@param vpidValues Refers to an output array of possibility names.
----------------------------------------------------------------------------------------------*/
void RnImportWizard::ParsePossibilities(char * pszFieldData, RnSfMarker & sfm,
	Vector<PossItemData> & vpidValues)
{
	Vector<char> vch;					// for working copy of input data.
	if (!pszFieldData || !*pszFieldData)
	{
		if (!sfm.m_fIgnoreEmpty && sfm.m_clo.m_staEmptyDefault.Length())
		{
			int len = sfm.m_clo.m_staEmptyDefault.Length() + 1;
			vch.Resize(len);
			strcpy_s(vch.Begin(), len, sfm.m_clo.m_staEmptyDefault.Chars());
		}
		else
		{
			return;
		}
	}
	else
	{
		int len = strlen(pszFieldData) + 1;
		vch.Resize(len);
		strcpy_s(vch.Begin(), len, pszFieldData);
	}
	char * pszVal = TrimSpaces(vch.Begin());
	if (!*pszVal)
		return;

	// Apply any changes to the input data string.
	Vector<char> vchChanged;			// Used for changes that increase string length.
	if (sfm.m_clo.m_vstaMatch.Size())
	{
		Assert(sfm.m_clo.m_vstaMatch.Size() == sfm.m_clo.m_vstaReplace.Size());
		int ista;
		int istaMatch;
		int cchMatch;
		int cch;
		int ichData = 0;
		int cchData;
		StrAnsi staData(pszVal);
		bool fChanged = false;
		do
		{
			cchMatch = 0;
			istaMatch = -1;
			cchData = staData.Length() - ichData;
			for (ista = 0; ista < sfm.m_clo.m_vstaMatch.Size(); ++ista)
			{
				cch = sfm.m_clo.m_vstaMatch[ista].Length();
				if (cch > cchData)
					continue;
				if (strncmp(staData.Chars() + ichData, sfm.m_clo.m_vstaMatch[ista].Chars(),
						cch) == 0)
				{
					if (cchMatch < cch)
					{
						cchMatch = cch;
						istaMatch = ista;
					}
				}
			}
			if (cchMatch)
			{
				staData.Replace(ichData, ichData + cchMatch,
					sfm.m_clo.m_vstaReplace[istaMatch]);
				ichData += sfm.m_clo.m_vstaReplace[istaMatch].Length();
				fChanged = true;
			}
			else
			{
				++ichData;
			}
		} while (ichData < staData.Length());
		if (fChanged)
		{
			if (staData.Length() > (int)strlen(pszVal))
			{
				vchChanged.Resize(staData.Length() + 1);
				pszVal = vchChanged.Begin();
			}
			strcpy_s(pszVal, staData.Length() + 1, staData.Chars());
		}
	}
	if (!*pszVal)
		return;

	// Split the input data string into a vector of items.
	Vector<char *> vpsz;
	vpsz.Push(pszVal);
	if (sfm.m_clo.m_fHaveMulti && sfm.m_clo.m_staDelimMulti.Length())
	{
		if (!sfm.m_clo.m_vstaDelimMulti.Size())
			ParseDelimField(sfm.m_clo.m_staDelimMulti, sfm.m_clo.m_vstaDelimMulti);
		if (sfm.m_clo.m_vstaDelimMulti.Size())
		{
			int ista;
			int ipsz;
			Vector<char *> vpszT;
			for (ista = 0; ista < sfm.m_clo.m_vstaDelimMulti.Size(); ++ista)
			{
				const char * pszDelim = sfm.m_clo.m_vstaDelimMulti[ista].Chars();
				char * psz;
				char * pszT;
				for (ipsz = 0; ipsz < vpsz.Size(); ++ipsz)
				{
					pszVal = vpsz[ipsz];
					while ((psz = strstr(pszVal, pszDelim)) != NULL)
					{
						*psz++ = '\0';
						pszT = TrimSpaces(pszVal);
						if (*pszT)
							vpszT.Push(pszT);
						pszVal = psz;
					}
					// Store the final (possibly only) chunk.
					pszT = TrimSpaces(pszVal);
					if (*pszT)
						vpszT.Push(pszT);
				}
				vpsz = vpszT;
				vpszT.Clear();
			}
		}
	}

	// Trim each item based on "Before" delimiters, if any.
	if (sfm.m_clo.m_fHaveBefore && sfm.m_clo.m_staBefore.Length())
	{
		if (!sfm.m_clo.m_vstaBefore.Size())
			ParseDelimField(sfm.m_clo.m_staBefore, sfm.m_clo.m_vstaBefore);
		int ista;
		int ipsz;
		const char * pszBefore;
		char * psz;
		for (ista = 0; ista < sfm.m_clo.m_vstaBefore.Size(); ++ista)
		{
			pszBefore = sfm.m_clo.m_vstaBefore[ista].Chars();
			for (ipsz = 0; ipsz < vpsz.Size(); ++ipsz)
			{
				pszVal = vpsz[ipsz];
				psz = strstr(pszVal, pszBefore);
				if (psz)
				{
					*psz = '\0';
					vpsz[ipsz] = TrimSpaces(pszVal);
				}
			}
		}
	}

	// Extract items based on Start and End markers.
	if (sfm.m_clo.m_fHaveBetween && sfm.m_clo.m_staMarkStart.Length() &&
		sfm.m_clo.m_staMarkEnd.Length())
	{
		if (!sfm.m_clo.m_vstaMarkStart.Size())
		{
			ParseDelimField(sfm.m_clo.m_staMarkStart, sfm.m_clo.m_vstaMarkStart);
			ParseDelimField(sfm.m_clo.m_staMarkEnd, sfm.m_clo.m_vstaMarkEnd);
			// Ensure that the two delimiters vectors are the same size.
			if (sfm.m_clo.m_vstaMarkStart.Size() < sfm.m_clo.m_vstaMarkEnd.Size())
			{
				// Complain?!
				sfm.m_clo.m_vstaMarkEnd.Resize(sfm.m_clo.m_vstaMarkStart.Size());
			}
			else if (sfm.m_clo.m_vstaMarkStart.Size() > sfm.m_clo.m_vstaMarkEnd.Size())
			{
				// Complain?!
				sfm.m_clo.m_vstaMarkStart.Resize(sfm.m_clo.m_vstaMarkEnd.Size());
			}
		}
		if (sfm.m_clo.m_vstaMarkStart.Size() && sfm.m_clo.m_vstaMarkEnd.Size())
		{
			int ista;
			int ipsz;
			int i;
			Vector<bool> vfOk;
			vfOk.Resize(vpsz.Size());
			for (i = 0; i < vfOk.Size(); ++i)
				vfOk[i] = false;
			const char * pszStart;
			const char * pszEnd;
			char * psz1;
			char * psz2;
			for (ista = 0; ista < sfm.m_clo.m_vstaMarkStart.Size(); ++ista)
			{
				pszStart = sfm.m_clo.m_vstaMarkStart[ista].Chars();
				pszEnd = sfm.m_clo.m_vstaMarkEnd[ista].Chars();
				for (ipsz = 0; ipsz < vpsz.Size(); ++ipsz)
				{
					pszVal = vpsz[ipsz];
					psz1 = strstr(pszVal, pszStart);
					if (psz1)
						psz2 = strstr(psz1 + 1, pszEnd);
					if (psz1 && psz2)
					{
						pszVal = psz1 + strlen(pszStart);
						*psz2 = '\0';
						vpsz[ipsz] = TrimSpaces(pszVal);
						vfOk[ipsz] = true;
					}
				}
			}
			// Remove any "values" that are not properly enclosed.
			for (ipsz = 0, i = 0; i < vfOk.Size(); ++i)
			{
				if (vfOk[i])
				{
					++ipsz;
				}
				else
				{
					vpsz.Delete(ipsz);
				}
			}
		}
	}

	// Normalize hierarchical item markers.
	Vector<char> vchHier;		// Used to reformat hierarchical names.
	char szHierDelim[3] = { kchHierDelim, ' ', 0 };		// Normalized separator.
	int cchHierDelim = strlen(szHierDelim);
	if (sfm.m_clo.m_fHaveSub && sfm.m_clo.m_staDelimSub.Length())
	{
		if (!sfm.m_clo.m_vstaDelimSub.Size())
			ParseDelimField(sfm.m_clo.m_staDelimSub, sfm.m_clo.m_vstaDelimSub);
		if (sfm.m_clo.m_vstaDelimSub.Size())
		{
			int ipsz;
			Vector<char *> vpszHier;
			Vector<char *> vpszT;
			// Split hierarchical name into its pieces, then store them in canonical form.
			for (ipsz = 0; ipsz < vpsz.Size(); ++ipsz)
			{
				vpszHier.Clear();
				vpszHier.Push(vpsz[ipsz]);
				char * psz;
				char * pszT;
				int isz;
				int cch = 0;
				for (int ista = 0; ista < sfm.m_clo.m_vstaDelimSub.Size(); ++ista)
				{
					vpszT.Clear();
					const char * pszDelim = sfm.m_clo.m_vstaDelimSub[ista].Chars();
					for (isz = 0; isz < vpszHier.Size(); ++isz)
					{
						pszVal = vpszHier[isz];
						while ((psz = strstr(pszVal, pszDelim)) != NULL)
						{
							*psz++ = '\0';
							pszT = TrimSpaces(pszVal);
							if (*pszT)
							{
								vpszT.Push(pszT);
								cch += strlen(pszT) + cchHierDelim;
							}
							pszVal = psz;
						}
						pszT = TrimSpaces(pszVal);
						if (*pszT)
						{
							vpszT.Push(pszT);
							cch += strlen(pszT);
						}
					}
					vpszHier = vpszT;
				}
				if (vpszHier.Size() == 1)
				{
					vpsz[ipsz] = vpszHier[0];
				}
				else
				{
					int ich = vchHier.Size();
					vchHier.Resize(ich + cch + 1);
					pszT = vchHier.Begin() + ich;
					for (isz = 0; isz < vpszHier.Size(); ++isz)
					{
						if (isz)
						{
							strcpy_s(pszT, strlen(szHierDelim) + 1, szHierDelim);
							pszT += cchHierDelim;
						}
						strcpy_s(pszT, strlen(vpszHier[isz]) + 1, vpszHier[isz]);
						pszT += strlen(vpszHier[isz]);
					}
					vpsz[ipsz] = vchHier.Begin() + ich;
				}
			}
		}
	}

	vpidValues.Clear();
	PossItemData pid;
	pid.m_fHasHier = false;
	pid.m_hvoOwner = 0;
	pid.m_cchMatch = 0;
	int ipsz;
	bool fDup;
	int ipid;
	for (ipsz = 0; ipsz < vpsz.Size(); ++ipsz)
	{
		if (!vpsz[ipsz])
			continue;
		vpsz[ipsz] += strspn(vpsz[ipsz], kszSpace);
		if (!*vpsz[ipsz])		// Don't store empty strings.
			continue;
		if (sfm.m_clo.m_vstaDelimSub.Size() && strstr(vpsz[ipsz], szHierDelim) != NULL)
			pid.m_fHasHier = true;
		pid.m_staName.Assign(vpsz[ipsz]);
		fDup = false;
		for (ipid = 0; ipid < vpidValues.Size(); ++ipid)
		{
			if (pid.m_staName == vpidValues[ipid].m_staName)
			{
				fDup = true;
				break;
			}
		}
		if (!fDup)
			vpidValues.Push(pid);
	}
}

/*----------------------------------------------------------------------------------------------
	Call the IEncConverter object to convert the 8-bit characters to UTF-16.

	@param pconAnal Pointer to an Encoding Converter
	@param staIn Reference to the input 8-bit character data string.
	@param pbstrOut Address of the output BSTR.
----------------------------------------------------------------------------------------------*/
static void CallEncConverter(IEncConverter * pconAnal, StrAnsi & staIn, BSTR * pbstrOut)
{
	AssertPtr(pconAnal);
	// We use ConvertToUnicode() with a SAFEARRAY because that's the only reliable way to pass
	// the 8-bit character string to C# code with an accurate character count.  See DN-830.
	SAFEARRAY * psa = ::SafeArrayCreateVector(VT_UI1, 0, staIn.Length());
	void * pBytes;
	CheckHr(::SafeArrayAccessData(psa, &pBytes));
	memcpy(pBytes, staIn.Chars(), staIn.Length());
	CheckHr(::SafeArrayUnaccessData(psa));
	IgnoreHr(pconAnal->ConvertToUnicode(psa, pbstrOut));
	CheckHr(::SafeArrayDestroy(psa));
}


/*----------------------------------------------------------------------------------------------
	Set the possiblity list HVO for the PossItemData, and convert m_staName to Unicode (UTF-16).

	@param pid The possibility item data (loaded from the sfm file)
	@param hvopssl The HVO of the possibility list.
	@param pcon Pointer to the encoding converter for the default analysis writing system.
----------------------------------------------------------------------------------------------*/
void RnImportWizard::SetListHvoAndConvertToUtf16(PossItemData & pid, HVO hvopssl,
	IEncConverter * pconAnal)
{
	pid.m_hvoOwner = hvopssl;
	if (pconAnal)
	{
		SmartBstr sbstrOut;
		CallEncConverter(pconAnal, pid.m_staName, &sbstrOut);
		pid.m_stuName.Assign(sbstrOut.Chars(), sbstrOut.Length());
	}
	else
	{
		StrUtil::StoreUtf16FromUtf8(pid.m_staName.Chars(), pid.m_staName.Length(),
			pid.m_stuName);
	}
}

/*----------------------------------------------------------------------------------------------
	Check the input data for containing the given possibility name.

	@param stuName Refers to a possibility name (or abbreviation).
	@param vpidData Refers to a list of possibility names extracted from the input data field.

	@return Index into vstaData of matching possibility name, or -1 if match not found.
----------------------------------------------------------------------------------------------*/
int RnImportWizard::PossNameMatchesData(StrUni & stuName, Vector<PossItemData> & vpidData)
{
	Assert(stuName.Length());
	int ipid;
	for (ipid = 0; ipid < vpidData.Size(); ++ipid)
	{
		if (stuName.EqualsCI(vpidData[ipid].m_stuName))
			return ipid;
	}
	return -1;
}

/*----------------------------------------------------------------------------------------------
	Fix the field string data by trimming surrounding whitespace, and replacing internal
	linefeeds with spaces.  Paragraphs are separated by a single linefeed characters, as
	specified by the flags in txo.

	@param pszFieldData Pointer to the input data read from the file.
	@param txo Text options for controlling how the raw string is broken into paragraphs.
	@param staFieldData Reference to the output string.
----------------------------------------------------------------------------------------------*/
void RnImportWizard::FixStringData(char * pszFieldData, RnSfMarker::TextOptions & txo,
	StrAnsi & staFieldData)
{
	char * pszField = TrimSpaces(pszFieldData);
	// 1. Split the field into the lines separated by \r\n (or by bare \r).
	Vector<char *> vpszLines;
	char * pszLine;
	char * pszCRLF;
	char * pszCR;
	char * pszNext;
	for (pszLine = pszField; pszLine; pszLine = pszNext)
	{
		pszCRLF = strstr(pszLine, "\r\n");
		if (pszCRLF)
		{
			pszNext = pszCRLF + 2;
			*pszCRLF = '\0';
		}
		else
		{
			pszNext = NULL;
		}
		while ((pszCR = strchr(pszLine, '\r')) != NULL)
		{
			*pszCR++ = '\0';
			vpszLines.Push(pszLine);
			pszLine = pszCR;
		}
		vpszLines.Push(pszLine);
	}
	// 2. Merge the lines into the output string, separating paragraphs by a single \n.
	int ipsz;
	int cchLine;
	int cchSP;
	bool fNewPara;
	bool fBlank = false;
	bool fShort = false;
	staFieldData.Clear();
	for (ipsz = 0; ipsz < vpszLines.Size(); ++ipsz)
	{
		pszLine = vpszLines[ipsz];
		cchLine = strlen(pszLine);
		if (!cchLine)
		{
			fBlank = true;
			continue;
		}
		cchSP = strspn(pszLine, " \t");
		fNewPara = false;
		if (txo.m_fStartParaNewLine)
			fNewPara = true;
		else if (txo.m_fStartParaBlankLine && fBlank)
			fNewPara = true;
		else if (txo.m_fStartParaIndented && cchSP)
			fNewPara = true;
		else if (txo.m_fStartParaShortLine && fShort)
			fNewPara = true;
		if (staFieldData.Length())
		{
			if (fNewPara)
				staFieldData.Append("\n");
			else
				staFieldData.Append(" ");
		}
		staFieldData.Append(TrimSpaces(pszLine));
		fShort = cchLine < txo.m_cchShortLim;
		fBlank = false;
	}
}

#ifdef THIS_CANT_POSSIBLY_EVER_BE_DEFINED
// Look in c:/FW/Obj/Debug/FwNotebook/autopch/SilEncConverters31.tlh for these definitions.

struct __declspec(uuid("def78166-76d9-4e2f-98c2-0dfc5601027d"))
IEncConverters : IDispatch
{
	  HRESULT get_Item (
		/*[in]*/ VARIANT mapName,
		/*[out,retval]*/ struct IEncConverter  ** pRetVal ) = 0;
	  HRESULT Add (
		/*[in]*/ BSTR mappingName,
		/*[in]*/ BSTR converterSpec,
		/*[in]*/ enum ConvType ConversionType,
		/*[in]*/ BSTR leftEncoding,
		/*[in]*/ BSTR rightEncoding,
		/*[in]*/ enum ProcessTypeFlags ProcessType ) = 0;
	  HRESULT AddConversionMap (
		/*[in]*/ BSTR mappingName,
		/*[in]*/ BSTR converterSpec,
		/*[in]*/ enum ConvType ConversionType,
		/*[in]*/ BSTR ImplementType,
		/*[in]*/ BSTR leftEncoding,
		/*[in]*/ BSTR rightEncoding,
		/*[in]*/ enum ProcessTypeFlags ProcessType ) = 0;
	  HRESULT AddImplementation (
		/*[in]*/ BSTR platform,
		/*[in]*/ BSTR Type,
		/*[in]*/ BSTR use,
		/*[in]*/ BSTR param,
		/*[in]*/ long priority ) = 0;
	  HRESULT AddAlias (
		/*[in]*/ BSTR encodingName,
		/*[in]*/ BSTR alias ) = 0;
	  HRESULT AddCompoundConverterStep (
		/*[in]*/ BSTR compoundMappingName,
		/*[in]*/ BSTR converterStepMapName,
		/*[in]*/ VARIANT_BOOL DirectionForward,
		/*[in]*/ enum NormalizeFlags NormalizeOutput ) = 0;
	  HRESULT AddFont (
		/*[in]*/ BSTR fontName,
		/*[in]*/ long CodePage,
		/*[in]*/ BSTR defineEncoding ) = 0;
	  HRESULT AddUnicodeFontEncoding (
		/*[in]*/ BSTR fontName,
		/*[in]*/ BSTR unicodeEncoding ) = 0;
	  HRESULT AddFontMapping (
		/*[in]*/ BSTR mappingName,
		/*[in]*/ BSTR fontName,
		/*[in]*/ BSTR assocFontName ) = 0;
	  HRESULT Remove (
		/*[in]*/ VARIANT mapName ) = 0;
	  HRESULT RemoveNonPersist (
		/*[in]*/ VARIANT mapName ) = 0;
	  HRESULT RemoveAlias (
		/*[in]*/ BSTR alias ) = 0;
	  HRESULT RemoveImplementation (
		/*[in]*/ BSTR platform,
		/*[in]*/ BSTR Type ) = 0;
	  HRESULT get_Count (
		/*[out,retval]*/ long * pRetVal ) = 0;
	  HRESULT ByProcessType (
		/*[in]*/ enum ProcessTypeFlags ProcessType,
		/*[out,retval]*/ struct IEncConverters ** pRetVal ) = 0;
	  HRESULT ByFontName (
		/*[in]*/ BSTR fontName,
		/*[in]*/ enum ProcessTypeFlags ProcessType,
		/*[out,retval]*/ struct IEncConverters ** pRetVal ) = 0;
	  HRESULT ByEncodingID (
		/*[in]*/ BSTR encoding,
		/*[in]*/ enum ProcessTypeFlags ProcessType,
		/*[out,retval]*/ struct IEncConverters ** pRetVal ) = 0;
	  HRESULT EnumByProcessType (
		/*[in]*/ enum ProcessTypeFlags ProcessType,
		/*[out,retval]*/ SAFEARRAY ** pRetVal ) = 0;
	  HRESULT Attributes (
		/*[in]*/ BSTR sItem,
		/*[in]*/ enum AttributeType RepositoryItem,
		/*[out,retval]*/ struct _ECAttributes ** pRetVal ) = 0;
	  HRESULT get_Encodings (
		/*[out,retval]*/ SAFEARRAY ** pRetVal ) = 0;
	  HRESULT get_Mappings (
		/*[out,retval]*/ SAFEARRAY ** pRetVal ) = 0;
	  HRESULT get_Fonts (
		/*[out,retval]*/ SAFEARRAY ** pRetVal ) = 0;
	  HRESULT get_Values (
		/*[out,retval]*/ struct ICollection ** pRetVal ) = 0;
	  HRESULT get_Keys (
		/*[out,retval]*/ struct ICollection ** pRetVal ) = 0;
	  HRESULT Clear ( ) = 0;
	  HRESULT BuildConverterSpecName (
		/*[in]*/ BSTR mappingName,
		/*[in]*/ BSTR ImplementType,
		/*[out,retval]*/ BSTR * pRetVal ) = 0;
	  HRESULT UnicodeEncodingFormConvert (
		/*[in]*/ BSTR sInput,
		/*[in]*/ enum EncodingForm eFormInput,
		/*[in]*/ long ciInput,
		/*[in]*/ enum EncodingForm eFormOutput,
		/*[out]*/ long * nNumItems,
		/*[out,retval]*/ BSTR * pRetVal ) = 0;
	  HRESULT get_RepositoryFile (
		/*[out,retval]*/ struct _mappingRegistry ** pRetVal ) = 0;
	  HRESULT CodePage (
		/*[in]*/ BSTR fontName,
		/*[out,retval]*/ long * pRetVal ) = 0;
	  HRESULT GetFontMapping (
		/*[in]*/ BSTR mappingName,
		/*[in]*/ BSTR fontName,
		/*[out,retval]*/ SAFEARRAY ** pRetVal ) = 0;
};

struct __declspec(uuid("db50f36b-9417-466b-aeb9-92dedc88765a"))
IEncConverter : IDispatch
{
	  HRESULT get_Name (
		/*[out,retval]*/ BSTR * pRetVal ) = 0;
	  HRESULT put_Name (
		/*[in]*/ BSTR pRetVal ) = 0;
	  HRESULT Initialize (
		/*[in]*/ BSTR converterName,
		/*[in]*/ BSTR ConverterIdentifier,
		/*[in,out]*/ BSTR * lhsEncodingID,
		/*[in,out]*/ BSTR * rhsEncodingID,
		/*[in,out]*/ enum ConvType * eConversionType,
		/*[in,out]*/ long * ProcessTypeFlags,
		/*[in]*/ long CodePageInput,
		/*[in]*/ long CodePageOutput,
		/*[in]*/ VARIANT_BOOL bAdding ) = 0;
	  HRESULT get_ConverterIdentifier (
		/*[out,retval]*/ BSTR * pRetVal ) = 0;
	  HRESULT get_ProgramID (
		/*[out,retval]*/ BSTR * pRetVal ) = 0;
	  HRESULT get_ImplementType (
		/*[out,retval]*/ BSTR * pRetVal ) = 0;
	  HRESULT get_ConversionType (
		/*[out,retval]*/ enum ConvType * pRetVal ) = 0;
	  HRESULT get_ProcessType (
		/*[out,retval]*/ long * pRetVal ) = 0;
	  HRESULT get_CodePageInput (
		/*[out,retval]*/ long * pRetVal ) = 0;
	  HRESULT put_CodePageInput (
		/*[in]*/ long pRetVal ) = 0;
	  HRESULT get_CodePageOutput (
		/*[out,retval]*/ long * pRetVal ) = 0;
	  HRESULT put_CodePageOutput (
		/*[in]*/ long pRetVal ) = 0;
	  HRESULT get_LeftEncodingID (
		/*[out,retval]*/ BSTR * pRetVal ) = 0;
	  HRESULT get_RightEncodingID (
		/*[out,retval]*/ BSTR * pRetVal ) = 0;
	  HRESULT put_RightEncodingID (
		/*[in]*/ BSTR pRetVal ) = 0;
	  HRESULT ConvertToUnicode (
		/*[in]*/ SAFEARRAY * baInput,
		/*[out,retval]*/ BSTR * pRetVal ) = 0;
	  HRESULT ConvertFromUnicode (
		/*[in]*/ BSTR sInput,
		/*[out,retval]*/ SAFEARRAY ** pRetVal ) = 0;
	  HRESULT Convert (
		/*[in]*/ BSTR sInput,
		/*[out,retval]*/ BSTR * pRetVal ) = 0;
	  HRESULT ConvertEx (
		/*[in]*/ BSTR sInput,
		/*[in]*/ enum EncodingForm inEnc,
		/*[in]*/ long ciInput,
		/*[in]*/ enum EncodingForm outEnc,
		/*[out]*/ long * ciOutput,
		/*[in]*/ enum NormalizeFlags eNormalizeOutput,
		/*[in]*/ VARIANT_BOOL bForward,
		/*[out,retval]*/ BSTR * pRetVal ) = 0;
	  HRESULT get_DirectionForward (
		/*[out,retval]*/ VARIANT_BOOL * pRetVal ) = 0;
	  HRESULT put_DirectionForward (
		/*[in]*/ VARIANT_BOOL pRetVal ) = 0;
	  HRESULT get_EncodingIn (
		/*[out,retval]*/ enum EncodingForm * pRetVal ) = 0;
	  HRESULT put_EncodingIn (
		/*[in]*/ enum EncodingForm pRetVal ) = 0;
	  HRESULT get_EncodingOut (
		/*[out,retval]*/ enum EncodingForm * pRetVal ) = 0;
	  HRESULT put_EncodingOut (
		/*[in]*/ enum EncodingForm pRetVal ) = 0;
	  HRESULT get_Debug (
		/*[out,retval]*/ VARIANT_BOOL * pRetVal ) = 0;
	  HRESULT put_Debug (
		/*[in]*/ VARIANT_BOOL pRetVal ) = 0;
	  HRESULT get_NormalizeOutput (
		/*[out,retval]*/ enum NormalizeFlags * pRetVal ) = 0;
	  HRESULT put_NormalizeOutput (
		/*[in]*/ enum NormalizeFlags pRetVal ) = 0;
	  HRESULT get_AttributeKeys (
		/*[out,retval]*/ SAFEARRAY ** pRetVal ) = 0;
	  HRESULT AttributeValue (
		/*[in]*/ BSTR sKey,
		/*[out,retval]*/ BSTR * pRetVal ) = 0;
	  HRESULT get_ConverterNameEnum (
		/*[out,retval]*/ SAFEARRAY ** pRetVal ) = 0;
	  HRESULT GetHashCode (
		/*[out,retval]*/ long * pRetVal ) = 0;
	  HRESULT Equals (
		/*[in]*/ VARIANT rhs,
		/*[out,retval]*/ VARIANT_BOOL * pRetVal ) = 0;
};

enum __declspec(uuid("9f4d36d5-fc90-3cb1-abd4-9dd01468d2e2"))
ProcessTypeFlags
{
	ProcessTypeFlags_DontKnow = 0,
	ProcessTypeFlags_UnicodeEncodingConversion = 1,
	ProcessTypeFlags_Transliteration = 2,
	ProcessTypeFlags_ICUTransliteration = 4,
	ProcessTypeFlags_ICUConverter = 8,
	ProcessTypeFlags_CodePageConversion = 16,
	ProcessTypeFlags_NonUnicodeEncodingConversion = 32
};

enum __declspec(uuid("41665dd7-fdf3-3d70-9ae0-c6b2fc5ce5f5"))
ConvType
{
	ConvType_Legacy_to_from_Unicode = 1,
	ConvType_Legacy_to_from_Legacy = 2,
	ConvType_Unicode_to_from_Legacy = 3,
	ConvType_Unicode_to_from_Unicode = 4,
	ConvType_Legacy_to_Unicode = 5,
	ConvType_Legacy_to_Legacy = 6,
	ConvType_Unicode_to_Legacy = 7,
	ConvType_Unicode_to_Unicode = 8
};

enum __declspec(uuid("5a942a2c-dcf3-335d-9fec-2bae3780ecb8"))
EncodingForm
{
	EncodingForm_Unspecified = 0,
	EncodingForm_LegacyString = 1,
	EncodingForm_UTF8String = 2,
	EncodingForm_UTF16BE = 3,
	EncodingForm_UTF16 = 4,
	EncodingForm_UTF32BE = 5,
	EncodingForm_UTF32 = 6,
	EncodingForm_LegacyBytes = 20,
	EncodingForm_UTF8Bytes = 21
};
#endif /*THIS_CANT_POSSIBLY_EVER_BE_DEFINED*/

/*----------------------------------------------------------------------------------------------
	Build a TsString from the Unicode string and writing system, properly handling embedded
	character formatting.

	@param staIn Refers to the input string data.
	@param ws The default writing system for the output string.
	@param ptsf Points to a string factory object.
	@param pencc Points to an IEncConverters object.
	@param hmwswsd Maps from ws to writing system data.
	@param pptss Address of the output string pointer.
----------------------------------------------------------------------------------------------*/
void RnImportWizard::MakeString(StrAnsi & staIn, int ws, ITsStrFactory * ptsf,
	IEncConverters * pencc, HashMap<int, WrtSysData> & hmwswsd, ITsString ** pptss)
{
	AssertPtr(ptsf);
	AssertPtr(pptss);
	ITsIncStrBldrPtr qtisb;
	CheckHr(ptsf->GetIncBldr(&qtisb));
	if (ws)
		CheckHr(qtisb->SetIntPropValues(ktptWs, ktpvDefault, ws));
	Vector<RnImportCharMapping> & vrcmp = m_shbx.CharMappings();
	Vector<RunStart> vrs;
	RunStart rs;
	int ichMin;
	int icmp;
	int iv;
	int ivLim;
	int ivMid;
	for (icmp = 0; icmp < vrcmp.Size(); ++icmp)
	{
		if (!vrcmp[icmp].m_staBegin.Length())
			continue;			// Should never happen, but if it does, don't hang!
		rs.m_icmp = icmp;
		for (ichMin = 0; ; )
		{
			rs.m_ich = staIn.FindStr(vrcmp[icmp].m_staBegin, ichMin);
			if (rs.m_ich == -1)
				break;
			ichMin = rs.m_ich + vrcmp[icmp].m_staBegin.Length();
			// Insert sorted by m_ich.
			for (iv = 0, ivLim = vrs.Size(); iv < ivLim; )
			{
				ivMid = (iv + ivLim) / 2;
				if (vrs[ivMid].m_ich < rs.m_ich)
					iv = ivMid + 1;
				else
					ivLim = ivMid;
			}
			vrs.Insert(iv, rs);
		}
	}
	if (!vrs.Size())
	{
		AppendToString(staIn, qtisb, 0, staIn.Length(), ws, pencc, hmwswsd);
		CheckHr(qtisb->GetString(pptss));
		return;
	}
	int crs = vrs.Size();
	// Add phony run at end to simplify code below.
	rs.m_icmp = -1;
	rs.m_ich = staIn.Length() + 1;
	vrs.Push(rs);
	Vector<int> vicmpOpen;		// Stack of open character formats.
	ichMin = 0;
	int irs;
	for (irs = 0; irs < crs; ++irs)
	{
		// Append any preceding run characters.
		if (ichMin < vrs[irs].m_ich)
		{
			AppendToString(staIn, qtisb, ichMin, vrs[irs].m_ich - ichMin, ws, pencc, hmwswsd);
			ichMin = vrs[irs].m_ich;
		}
		Assert(vrs[irs].m_ich == ichMin);
		// Set any additional properties.
		icmp = vrs[irs].m_icmp;
		RnImportCharMapping & rcmp = vrcmp[icmp];
		int wsSub = ws;
		switch (rcmp.m_cmt)
		{
		case RnImportCharMapping::kcmtNone:
			break;
		case RnImportCharMapping::kcmtDirectFormatting:
			CheckHr(qtisb->SetIntPropValues(rcmp.m_tptDirect, 0, kttvForceOn));
			break;
		case RnImportCharMapping::kcmtCharStyle:
			CheckHr(qtisb->SetStrPropValue(ktptNamedStyle, rcmp.m_stuCharStyle.Bstr()));
			break;
		case RnImportCharMapping::kcmtOldWritingSystem:
			wsSub = rcmp.m_ws;
			break;
		}
		ichMin += rcmp.m_staBegin.Length();
		int ichEnd = staIn.FindStr(rcmp.m_staEnd, ichMin);
		if (ichEnd < 0)
			ichEnd = staIn.Length();	// Missing end marker means end of string, I guess.
		if (ichEnd < vrs[irs+1].m_ich)
		{
			while (ichEnd < vrs[irs+1].m_ich)
			{
				// Append the run characters.
				if (ichEnd > ichMin)
				{
					AppendToString(staIn, qtisb, ichMin, ichEnd - ichMin, wsSub, pencc,
						hmwswsd);
				}
				// Remove the properties added for this run.
				switch (vrcmp[icmp].m_cmt)
				{
				case RnImportCharMapping::kcmtNone:
					break;
				case RnImportCharMapping::kcmtDirectFormatting:
					CheckHr(qtisb->SetIntPropValues(vrcmp[icmp].m_tptDirect, -1, -1));
					break;
				case RnImportCharMapping::kcmtCharStyle:
					CheckHr(qtisb->SetStrPropValue(ktptNamedStyle, NULL));
					break;
				case RnImportCharMapping::kcmtOldWritingSystem:
					break;
				}
				ichMin = ichEnd + vrcmp[icmp].m_staEnd.Length();
				if (ichMin > staIn.Length())
					ichMin = staIn.Length();
				if (vicmpOpen.Size())
				{
					// Check whether the next run of chars closes off any open char formats.
					icmp = *vicmpOpen.Top();
					ichEnd = staIn.FindStr(vrcmp[icmp].m_staEnd, ichMin);
					if (ichEnd < 0)
						ichEnd = staIn.Length();
					if (ichEnd < vrs[irs+1].m_ich)
						vicmpOpen.Pop();
				}
				else
				{
					break;
				}
			}
		}
		else
		{
			vicmpOpen.Push(vrs[irs].m_icmp);
		}
	}
	// Append any trailing run characters and obtain the final string.
	if (ichMin < staIn.Length())
		AppendToString(staIn, qtisb, ichMin, staIn.Length() - ichMin, ws, pencc, hmwswsd);
	CheckHr(qtisb->GetString(pptss));
}

/*----------------------------------------------------------------------------------------------
	Build a TsString from the Unicode string and writing system, properly handling embedded
	character formatting.

	@param staIn Refers to the input string data.
	@param ptisb Points to the incremental string builder.
	@param ichMin Starting offset in staIn of characters to append.
	@param cch Number of characters from staIn to append.
	@param ws The default writing system for the output string.
	@param pencc Points to an IEncConverters object.
	@param hmwswsd Maps from ws to writing system data.
----------------------------------------------------------------------------------------------*/
void RnImportWizard::AppendToString(StrAnsi staIn, ITsIncStrBldr * ptisb, int ichMin, int cch,
	int ws, IEncConverters * pencc, HashMap<int, WrtSysData> & hmwswsd)
{
	AssertPtr(ptisb);
	AssertPtr(pencc);
	Assert(ichMin + cch <= staIn.Length());

	IEncConverterPtr qcon;
	if (ws)
	{
		WrtSysData wsd;
		if (hmwswsd.Retrieve(ws, &wsd))
			GetEncodingConverter(wsd, pencc, &qcon);
		CheckHr(ptisb->SetIntPropValues(ktptWs, ktpvDefault, ws));
	}
	else
	{
		// ENHANCE: remove this possibility.
		CheckHr(ptisb->SetIntPropValues(ktptWs, -1, -1));
	}
	if (qcon)
	{
		SmartBstr sbstrOut;
		StrAnsi staSub(staIn.Chars() + ichMin, cch);
		CallEncConverter(qcon, staSub, &sbstrOut);
		CheckHr(ptisb->AppendRgch(sbstrOut.Chars(), sbstrOut.Length()));
	}
	else
	{
		// Assume UTF-8 if there's no encoding converter defined.
		StrUni stuT;
		StrUtil::StoreUtf16FromUtf8(staIn.Chars() + ichMin, cch, stuT);
		CheckHr(ptisb->AppendRgch(stuT.Chars(), stuT.Length()));
	}
}

/*----------------------------------------------------------------------------------------------
	Set the paragraph style, unless it is the default (Normal) value.

	@param pode Pointer to the database object.
	@param pmdc Pointer to the database meta data cache.
	@param hvoPara Id of the newly created St(Txt)Para object.
	@param stuStyle Name of the desired paragraph style.
----------------------------------------------------------------------------------------------*/
void RnImportWizard::SetParaStyle(IOleDbEncap * pode, IFwMetaDataCache * pmdc, HVO hvoPara,
	StrUni & stuStyle)
{
	if (stuStyle.Length() && stuStyle != m_stuParaStyleDefault)
	{
		CheckHr(SetUnicode(pode, pmdc, hvoPara, kflidStPara_StyleName,
			const_cast<OLECHAR *>(stuStyle.Chars()), stuStyle.Length()));
		ITsTextPropsPtr qttp;
		m_hmsuqttpStyles.Retrieve(stuStyle, qttp);
		IUnknownPtr qunk;
		qttp->QueryInterface(IID_IUnknown, (void **)&qunk);
		CheckHr(SetUnknown(pode, pmdc, hvoPara, kflidStPara_StyleRules, qunk));
	}
}


/*----------------------------------------------------------------------------------------------
	Try to calculate one or more date formats by looking at the data.
----------------------------------------------------------------------------------------------*/
void RnImportWizard::DeriveDateFormat(RnSfMarker & sfm)
{
	StrAnsiBuf stabMkr;
	Vector<const char *> vpszDates;
	const char * psz;
	int ipsz;

	stabMkr.Format("\\%s", sfm.m_stabsMkr.Chars());
	for (ipsz = 0; ipsz < m_risd.m_vpszFields.Size(); ++ipsz)
	{
		psz = m_risd.m_vpszFields[ipsz];
		if (strncmp(psz, stabMkr.Chars(), stabMkr.Length()) == 0)
		{
			if (isascii(psz[stabMkr.Length()]))
			{
				psz += stabMkr.Length();
				psz += strspn(psz, " \t\r\n\f");
				if (*psz)
					vpszDates.Push(psz);
			}
		}
	}
	if (!vpszDates.Size())
		return;
	int n1;
	int n2;
	int n3;
	const char * psz1;
	const char * psz2;
	const char * psz3;
	int cch1;
	int cch2;
	int cch3;
	char * pszNext;
	StrAnsiBufSmall stabsSep1;
	StrAnsiBufSmall stabsSep2;
	bool fMonth1;
	bool fMonth2;

	int cch;
	SilTime stim;
	StrAnsi staFmt;
	bool fFound;
	int ista;
	for (ipsz = 0; ipsz < vpszDates.Size(); ++ipsz)
	{
		fMonth1 = false;
		fMonth2 = false;
		psz1 = vpszDates[ipsz];
		n1 = strtol(psz1, &pszNext, 10);
		cch1 = pszNext - psz1;
		if (!n1 && !cch1)
		{
			// May be something like "August 16, 2002"
			while (*pszNext && (!isascii(*pszNext) || isalpha(*pszNext)))
				++pszNext;
			if (pszNext == psz1 || *pszNext == 0)
				continue;		// give up on this date string.
			cch1 = pszNext - psz1;
			fMonth1 = true;
		}
		if (*pszNext == 0)
			continue;
		psz2 = pszNext;
		while (*psz2 && isascii(*psz2) && !isalnum(*psz2))
			++psz2;
		stabsSep1.Assign(pszNext, psz2 - pszNext);
		n2 = strtol(psz2, &pszNext, 10);
		cch2 = pszNext - psz2;
		if (!n2 && !cch2)
		{
			// May be something like "16-Aug-02"
			while (*pszNext && (!isascii(*pszNext) || isalpha(*pszNext)))
				++pszNext;
			if (pszNext == psz2 || *pszNext == 0)
				continue;		// give up on this date string.
			cch2 = pszNext - psz2;
			fMonth2 = true;
		}
		if (*pszNext == 0)
			continue;
		psz3 = pszNext;
		while (*psz3 && isascii(*psz3) && !isalnum(*psz3))
			++psz3;
		stabsSep2.Assign(pszNext, psz3 - pszNext);
		n3 = strtol(psz3, &pszNext, 10);
		cch3 = pszNext - psz3;
		if (!n3 && !cch3)
			continue;		// Nobody puts the month last!?
		if (*pszNext)
			continue;		// We should have consumed the entire date.

		// Okay we have some data.  Let's try to figure it out.
		staFmt.Clear();
		if (fMonth1)
		{
			// Month Day Year.
			if (cch1 < 3)
				continue;
			if (cch1 == 3)
				staFmt.Format("MMM%s", stabsSep1.Chars());
			else
				staFmt.Format("MMMM%s", stabsSep1.Chars());
			if (n2 >= 1 && n2 <= 31)
			{
				if (cch2 == 1)
					staFmt.FormatAppend("d%s", stabsSep2.Chars());
				else
					staFmt.FormatAppend("dd%s", stabsSep2.Chars());
				if (n3 < 100)
					staFmt.Append("yy");
				else if (n3 < 1000)
					staFmt.Append("yyy");
				else
					staFmt.Append("yyyy");
			}
			else
			{
				continue;		// Nobody puts the year in the middle!
			}
		}
		else if (n1 == 0 || n1 > 31)
		{
			// Year Month Day.
			if (n1 < 100)
				staFmt.Format("yy%s", stabsSep1.Chars());
			else if (n1 < 1000)
				staFmt.Format("yyy%s", stabsSep1.Chars());
			else
				staFmt.Format("yyyy%s", stabsSep1.Chars());
			if (fMonth2)
			{
				if (cch2 < 3)
					continue;
				if (cch2 == 3)
					staFmt.FormatAppend("MMM%s", stabsSep2.Chars());
				else
					staFmt.FormatAppend("MMMM%s", stabsSep2.Chars());
			}
			else if (n2 >= 1 && n2 <= 12)
			{
				if (cch2 == 1)
					staFmt.FormatAppend("M%s", stabsSep2.Chars());
				else
					staFmt.FormatAppend("MM%s", stabsSep2.Chars());
			}
			else
			{
				continue;		// Nobody puts the day in the middle!
			}
			if (cch3 == 1)
				staFmt.Append("d");
			else
				staFmt.Append("dd");
		}
		else if (n1 > 12)
		{
			if (n3 >= 1 && n3 <= 31)
				continue;		// Year or day comes first or last: give up.
			// Day Month Year.
			if (cch1 == 1)
				staFmt.Format("d%s", stabsSep1.Chars());
			else
				staFmt.Format("dd%s", stabsSep1.Chars());
			if (fMonth2)
			{
				if (cch2 < 3)
					continue;
				if (cch2 == 3)
					staFmt.FormatAppend("MMM%s", stabsSep2.Chars());
				else
					staFmt.FormatAppend("MMMM%s", stabsSep2.Chars());
			}
			else if (n2 >= 1 && n2 <= 12)
			{
				if (cch2 == 1)
					staFmt.FormatAppend("M%s", stabsSep2.Chars());
				else
					staFmt.FormatAppend("MM%s", stabsSep2.Chars());
			}
			else
			{
				continue;
			}
			if (n3 < 100)
				staFmt.Append("yy");
			else if (n3 < 1000)
				staFmt.Append("yyy");
			else
				staFmt.Append("yyyy");
		}
		else
		{
			// Year or month or day comes first.
			if (n2 > 12 && n2 <= 31 && (n3 == 0 || n3 > 31))
			{
				// Month Day Year.
				if (cch1 == 1)
					staFmt.Format("M%s", stabsSep1.Chars());
				else
					staFmt.Format("MM%s", stabsSep1.Chars());
				if (cch2 == 1)
					staFmt.FormatAppend("d%s", stabsSep2.Chars());
				else
					staFmt.FormatAppend("dd%s", stabsSep2.Chars());
				if (n3 < 100)
					staFmt.Append("yy");
				else if (n3 < 1000)
					staFmt.Append("yyy");
				else
					staFmt.Append("yyyy");
			}
			else
			{
				continue;
			}
		}
		cch = StrUtil::ParseDateWithFormat(vpszDates[ipsz], staFmt.Chars(), &stim);
		if ((unsigned)cch == strlen(vpszDates[ipsz]))
		{
			fFound = false;
			for (ista = 0; ista < sfm.m_dto.m_vstaFmt.Size(); ++ ista)
			{
				if (staFmt == sfm.m_dto.m_vstaFmt[ista])
				{
					fFound = true;
					break;
				}
			}
			if (!fFound)
				sfm.m_dto.m_vstaFmt.Push(staFmt);
		}
	}
}


//:>********************************************************************************************
//:>	RnImportWizOverview methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
RnImportWizOverview::RnImportWizOverview()
	: AfWizardPage(kridImportWizOverviewDlg)
{
	m_pszHelpUrl = _T("Beginning_Tasks/Import_Standard_Format/Step_1_of_9_Overview.htm");
}

/*----------------------------------------------------------------------------------------------
	Initialize the dialog in response to the WM_INITDIALOG message.
	All one-time initialization should be done here (that is, all controls have been created
	and have valid hwnd's, but they need initial values.)

	@param hwndCtrl Not used by this method.
	@param lp Not used by this method.

	@return True.
----------------------------------------------------------------------------------------------*/
bool RnImportWizOverview::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	// Set the font for the page title.
	m_hfontLarge = AfGdi::CreateFont(14, 0, 0, 0, FW_BOLD, FALSE, FALSE, FALSE, ANSI_CHARSET,
		OUT_CHARACTER_PRECIS, CLIP_DEFAULT_PRECIS, DEFAULT_QUALITY, VARIABLE_PITCH | FF_SWISS,
		_T("MS Sans Serif"));
	if (m_hfontLarge)
		::SendMessage(::GetDlgItem(m_hwnd, kcidImportWizOverviewHdr), WM_SETFONT,
			(WPARAM)m_hfontLarge, false);
	StrApp str(kstidImportWizOverviewStep);
	::SetWindowText(::GetDlgItem(m_hwnd, kcidImportWizOverviewStep), str.Chars());

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}


//:>********************************************************************************************
//:>	RnImportWizSettings methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
RnImportWizSettings::RnImportWizSettings()
	: AfWizardPage(kridImportWizSettingsDlg)
{
	m_pszHelpUrl = _T("Beginning_Tasks/Import_Standard_Format/Step_2_of_9_Save_Settings.htm");
}

/*----------------------------------------------------------------------------------------------
	Initialize the dialog in response to the WM_INITDIALOG message.
	All one-time initialization should be done here (that is, all controls have been created
	and have valid hwnd's, but they need initial values.)

	@param hwndCtrl Not used by this method.
	@param lp Not used by this method.

	@return True.
----------------------------------------------------------------------------------------------*/
bool RnImportWizSettings::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	m_fDirty = true;
	// Set the font for the page title.
	m_hfontLarge = AfGdi::CreateFont(14, 0, 0, 0, FW_BOLD, FALSE, FALSE, FALSE, ANSI_CHARSET,
		OUT_CHARACTER_PRECIS, CLIP_DEFAULT_PRECIS, DEFAULT_QUALITY, VARIABLE_PITCH | FF_SWISS,
		_T("MS Sans Serif"));
	if (m_hfontLarge)
		::SendMessage(::GetDlgItem(m_hwnd, kcidImportWizSettingsHdr), WM_SETFONT,
			(WPARAM)m_hfontLarge, false);
	StrApp str(kstidImportWizSettingsStep);
	::SetWindowText(::GetDlgItem(m_hwnd, kcidImportWizSettingsStep), str.Chars());

	RnImportWizard * priw = dynamic_cast<RnImportWizard *>(m_pwiz);
	AssertPtr(priw);

	// Populate the combo boxes.
	int i;
	StrApp strName;
	StrApp strClean(kstidImportWizSettingsClean);		// "<Clean Slate>"
	StrApp strNewSet(kstidImportWizSettingsNew);	// "(new settings name)"
	StrApp strDefault(kstidImportWizSettingsDefault);	// "Shoebox Anthropology Guide"
	StrApp strDefMod(kstidImportWizSettingsDefMod);	// "Shoebox Anthropology Guide (modified)"
	StrApp strDefModFmt(kstidImportWizSettingsDefModFmt);	// "Shoebox ... (modified %d)"
	int cshbx = priw->StoredSettingsCount();
	bool fMatchFound;
	int nMatch = 1;
	do
	{
		fMatchFound = false;
		for (i = 0; i < cshbx; ++i)
		{
			str = priw->StoredSettings(i)->Name();
			if (str == strDefMod)
			{
				fMatchFound = true;
				break;
			}
		}
		if (fMatchFound)
		{
			++nMatch;
			strDefMod.Format(strDefModFmt.Chars(), nMatch);
		}
	} while (fMatchFound);

	HWND hwndFrom = ::GetDlgItem(m_hwnd, kctidImportWizSettingsFrom);
	Assert(hwndFrom != NULL);
	::SendMessage(hwndFrom, CB_ADDSTRING, 0, (LPARAM)strClean.Chars());
	::SendMessage(hwndFrom, CB_ADDSTRING, 0, (LPARAM)strDefault.Chars());
	HWND hwndTo = ::GetDlgItem(m_hwnd, kctidImportWizSettingsTo);
	Assert(hwndTo != NULL);
	::SendMessage(hwndTo, CB_ADDSTRING, 0, (LPARAM)strNewSet.Chars());
	::SendMessage(hwndTo, CB_ADDSTRING, 0, (LPARAM)strDefMod.Chars());

	for (i = 0; i < cshbx; ++i)
	{
		strName = priw->StoredSettings(i)->Name();
		if (strName != strClean && strName != strDefault)
		{
			::SendMessage(hwndFrom, CB_ADDSTRING, 0, (LPARAM)strName.Chars());
			::SendMessage(hwndTo, CB_ADDSTRING, 0, (LPARAM)strName.Chars());
		}
	}
	if (cshbx)
	{
		m_iselFrom = 2;		// The chosen item from before was written first.
		m_iselTo = 2;
	}
	else
	{
		m_iselFrom = 1;		// we're starting from scratch.
		m_iselTo = 1;
	}
	::SendMessage(hwndFrom, CB_SETCURSEL, m_iselFrom, 0);
	::SendMessage(hwndTo, CB_SETCURSEL, m_iselTo, 0);

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}

/*----------------------------------------------------------------------------------------------
	Handle a WM_NOTIFY message, first letting the superclass method handle it if possible.

	@param ctidFrom Identifies the control sending the message.
	@param pnmh Pointer to the notification message data.
	@param lnRet Reference to a long integer return value used by some messages.

	@return True if the message is handled successfully; otherwise, false.
----------------------------------------------------------------------------------------------*/
bool RnImportWizSettings::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	if (SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet))
		return true;

	achar rgchBuffer[MAX_PATH];
	switch (pnmh->code)
	{
	case CBN_SELCHANGE:
		if (ctidFrom == kctidImportWizSettingsFrom)
		{
			m_fDirty = true;
			int isel = ::SendMessage(::GetDlgItem(m_hwnd, kctidImportWizSettingsFrom),
				CB_GETCURSEL, 0, 0);
			HWND hwndTo = ::GetDlgItem(m_hwnd, kctidImportWizSettingsTo);
			::SendMessage(hwndTo, CB_SETCURSEL, isel, 0);
			if (!::GetWindowText(hwndTo, rgchBuffer, MAX_PATH))
				rgchBuffer[0] = 0;
			StrApp strTo(rgchBuffer);
			if (!strTo.Length())
			{
				m_pwiz->EnableNextButton(false);
			}
			else if (isel == 0)
			{
				StrApp strNew(kstidImportWizSettingsNew);
				m_pwiz->EnableNextButton(strTo != strNew);
			}
			else
			{
				m_pwiz->EnableNextButton(true);
			}
		}
		break;
	case CBN_EDITCHANGE:
		if (ctidFrom == kctidImportWizSettingsTo)
		{
			HWND hwndTo = ::GetDlgItem(m_hwnd, kctidImportWizSettingsTo);
			int isel = ::SendMessage(hwndTo, CB_GETCURSEL, 0, 0);
			if (!::GetWindowText(hwndTo, rgchBuffer, MAX_PATH))
				rgchBuffer[0] = 0;
			StrApp strTo(rgchBuffer);
			if (!strTo.Length())
			{
				m_pwiz->EnableNextButton(false);
			}
			else if (isel == 0)
			{
				StrApp strNew(kstidImportWizSettingsNew);
				m_pwiz->EnableNextButton(strTo != strNew);
			}
			else
			{
				m_pwiz->EnableNextButton(true);
			}
		}
		break;
	}

	return false;
}

/*----------------------------------------------------------------------------------------------
	We're moving on from the settings page, so initialize the settings as indicated.

	@return NULL (advances to the next page).
----------------------------------------------------------------------------------------------*/
HWND RnImportWizSettings::OnWizardNext()
{
	if (!m_fDirty)
		return NULL;
	RnImportWizard * priw = dynamic_cast<RnImportWizard *>(m_pwiz);
	AssertPtr(priw);
	// Get the settings that we want to initialize from, and initialize!
	HWND hwndFrom = ::GetDlgItem(m_hwnd, kctidImportWizSettingsFrom);
	m_iselFrom = ::SendMessage(hwndFrom, CB_GETCURSEL, 0, 0);
	if (m_iselFrom == 0)
	{
		// "<Clean Slate>"
		priw->Settings()->Clear();
	}
	else if (m_iselFrom == 1)
	{
		// "Shoebox Anthropology Guide"
		priw->Settings()->SetDefaults();
	}
	else if (m_iselFrom > 1)
	{
		priw->Settings()->Copy(priw->StoredSettings(m_iselFrom - 2));
	}

	// Store the name of the settings for saving.
	HWND hwndTo = ::GetDlgItem(m_hwnd, kctidImportWizSettingsTo);
	m_iselTo = ::SendMessage(hwndTo, CB_GETCURSEL, 0, 0);
	achar rgchBuffer[MAX_PATH];
	if (!::GetWindowText(hwndTo, rgchBuffer, MAX_PATH))
		rgchBuffer[0] = 0;
	priw->Settings()->SetName(rgchBuffer);

	priw->SetSettingsFromTo(m_iselFrom, m_iselTo);

	m_fDirty = false;
	return NULL;
}


//:>********************************************************************************************
//:>	RnImportWizSfmDb methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
RnImportWizSfmDb::RnImportWizSfmDb()
	: AfWizardPage(kridImportWizSfmDbDlg)
{
	m_pszHelpUrl = _T("Beginning_Tasks/Import_Standard_Format/Step_3_of_9_Choose_Database.htm");
	m_ishbx = -100;
}

/*----------------------------------------------------------------------------------------------
	Initialize the dialog in response to the WM_INITDIALOG message.
	All one-time initialization should be done here (that is, all controls have been created
	and have valid hwnd's, but they need initial values.)

	@param hwndCtrl Not used by this method.
	@param lp Not used by this method.

	@return True.
----------------------------------------------------------------------------------------------*/
bool RnImportWizSfmDb::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	// Set the font for the page title.
	m_hfontLarge = AfGdi::CreateFont(14, 0, 0, 0, FW_BOLD, FALSE, FALSE, FALSE, ANSI_CHARSET,
		OUT_CHARACTER_PRECIS, CLIP_DEFAULT_PRECIS, DEFAULT_QUALITY, VARIABLE_PITCH | FF_SWISS,
		_T("MS Sans Serif"));
	if (m_hfontLarge)
		::SendMessage(::GetDlgItem(m_hwnd, kcidImportWizSfmDbHdr), WM_SETFONT,
			(WPARAM)m_hfontLarge, false);
	StrApp str(kstidImportWizSfmDbStep);
	::SetWindowText(::GetDlgItem(m_hwnd, kcidImportWizSfmDbStep), str.Chars());

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}

/*----------------------------------------------------------------------------------------------
	Handle a WM_NOTIFY message, first letting the superclass method handle it if possible.

	@param ctidFrom Identifies the control sending the message.
	@param pnmh Pointer to the notification message data.
	@param lnRet Reference to a long integer return value used by some messages.

	@return True if the message is handled successfully; otherwise, false.
----------------------------------------------------------------------------------------------*/
bool RnImportWizSfmDb::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	if (SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet))
		return true;

	achar rgchFile[MAX_PATH];
	switch (pnmh->code)
	{
	case BN_CLICKED:
		if (ctidFrom == kctidImportWizSfmDbBrowse)
		{
			// Open file dialog.
			::ZeroMemory(rgchFile, MAX_PATH);
			::GetDlgItemText(m_hwnd, kctidImportWizSfmDbFile, rgchFile, MAX_PATH);

			StrAppBuf strbFile;
			DWORD nFlags = GetFileAttributes(rgchFile);
			strbFile = rgchFile;
			if ((nFlags != -1) &&
				(nFlags != INVALID_FILE_ATTRIBUTES) &&
				(nFlags & FILE_ATTRIBUTE_DIRECTORY))
			{
				int length = strbFile.Length();
				int temp = -2;
				temp = strbFile.FindCh('\\', length - 1);
				if (strbFile.FindCh('\\', length - 1) == (length - 1))
					strbFile.Append("*.db");
				else
					strbFile.Append("\\*.db");
			};

			OPENFILENAME ofn;
			::ZeroMemory(&ofn, isizeof(OPENFILENAME));
			// the constant below is required for compatibility with Windows 95/98 (and maybe
			// NT4)
			ofn.lStructSize = OPENFILENAME_SIZE_VERSION_400;
			ofn.Flags = OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST | OFN_HIDEREADONLY;
			ofn.hwndOwner = m_hwnd;
			ofn.lpstrFilter = _T("Shoebox Database Files (*.db)\0*.db\0")
				_T("All Files (*.*)\0*.*\0\0");
			ofn.lpstrDefExt = _T("db");
			ofn.lpstrInitialDir = rgchFile;
			ofn.lpstrTitle  = _T("Select Shoebox Database File to Import");
			ofn.lpstrFile   = const_cast<achar *>(strbFile.Chars());
			ofn.nMaxFile    = MAX_PATH;
			if (::GetOpenFileName(&ofn) == IDOK)
			{
				::SetDlgItemText(m_hwnd, kctidImportWizSfmDbFile, ofn.lpstrFile);
			}
			return true;
		}
		else if (ctidFrom == kctidImportWizSfmDbProjBrowse)
		{
			// Open file dialog.
			::ZeroMemory(rgchFile, MAX_PATH+1);
			::GetDlgItemText(m_hwnd, kctidImportWizSfmDbProjFile, rgchFile, MAX_PATH);

			StrAppBuf strbFile;
			DWORD nFlags = GetFileAttributes(rgchFile);
			strbFile = rgchFile;
			if ((nFlags != -1) &&
				(nFlags != INVALID_FILE_ATTRIBUTES) &&
				(nFlags & FILE_ATTRIBUTE_DIRECTORY))
			{
				int length = strbFile.Length();
				int temp = -2;
				temp = strbFile.FindCh('\\', length - 1);
				if (strbFile.FindCh('\\', length - 1) == (length - 1))
					strbFile.Append("*.prj");
				else
					strbFile.Append("\\*.prj");
			};

			OPENFILENAME ofn;
			::ZeroMemory(&ofn, isizeof(OPENFILENAME));
			// the constant below is required for compatibility with Windows 95/98 (and maybe
			// NT4)
			ofn.lStructSize = OPENFILENAME_SIZE_VERSION_400;
			ofn.Flags = OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST | OFN_HIDEREADONLY;
			ofn.hwndOwner = m_hwnd;
			ofn.lpstrFilter = _T("Shoebox Project Files (*.prj)\0*.prj\0")
				_T("All Files (*.*)\0*.*\0\0");
			ofn.lpstrDefExt = _T("prj");
			ofn.lpstrInitialDir = rgchFile;
			ofn.lpstrTitle  = _T("Select Shoebox Project File");
			ofn.lpstrFile   = const_cast<achar *>(strbFile.Chars());
			ofn.nMaxFile    = MAX_PATH;
			if (::GetOpenFileName(&ofn) == IDOK)
			{
				::SetDlgItemText(m_hwnd, kctidImportWizSfmDbProjFile, ofn.lpstrFile);
			}
			return true;
		}
		break;
	case EN_CHANGE:
		if (ctidFrom == kctidImportWizSfmDbFile)
		{
			::ZeroMemory(rgchFile, MAX_PATH+1);
			::GetDlgItemText(m_hwnd, kctidImportWizSfmDbFile, rgchFile, MAX_PATH);
			CheckForShoebox(rgchFile, true);
			return true;
		}
		break;
	}

	return false;
}

/*----------------------------------------------------------------------------------------------
	Check whether the given file is a Shoebox database file.  If it is, make the project file
	controls visible, and enable the Next button.  Otherwise, make them invisible and disable
	the Next button.

	@param pszFile Name of file to check for being a Shoebox database file.
	@param fInformUser Flag whether to pop up a message box if it's not a Shoebox file, but
						looks as though it may be a valid SFM file.

	@return True if the given file appears to be a valid Shoebox for Windows database; otherwise
					false.
----------------------------------------------------------------------------------------------*/
bool RnImportWizSfmDb::CheckForShoebox(const achar * pszFile, bool fInformUser)
{
	if (pszFile && *pszFile)
	{
		if (m_strCheckedDbFile == pszFile)
		{	// We've checked it once, why check again?
			m_pwiz->EnableNextButton(m_fCheckedDbFileIsSFM);
			return m_fCheckedDbFileIsSFM;
		}
		m_strCheckedDbFile = pszFile;

		HANDLE hFile = ::CreateFile(pszFile, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING,
			FILE_ATTRIBUTE_NORMAL, NULL);
		if (hFile != INVALID_HANDLE_VALUE)
		{
#define BUFSIZE 4096
			char szLine[BUFSIZE+1];
			::ZeroMemory(szLine, BUFSIZE+1);
			DWORD cb;
			bool fOk = ::ReadFile(hFile, szLine, BUFSIZE, &cb, NULL);
			::CloseHandle(hFile);
			if (fOk)
			{
				szLine[cb] = '\0';
				if (strncmp(szLine, "\\_sh", 4) == 0)
				{
					if (strncmp(szLine, "\\_sh v3.0 ", 10) == 0 ||
						strncmp(szLine, "\\_sh v4.0 ", 10) == 0)
					{
						char * pszTypeName = szLine + 10;
						pszTypeName += strspn(pszTypeName, " \t");
						pszTypeName += strspn(pszTypeName, "0123456789");
						pszTypeName += strspn(pszTypeName, " \t");
						char * psz = strpbrk(pszTypeName, "\r\n");
						if (psz)
							*psz = '\0';
						RnImportWizard * priw = dynamic_cast<RnImportWizard *>(m_pwiz);
						AssertPtr(priw);
						priw->SetTypeName(pszTypeName);
					}
					::ShowWindow(::GetDlgItem(m_hwnd, kctidImportWizSfmDbProjText1), SW_SHOWNA);
					::ShowWindow(::GetDlgItem(m_hwnd, kctidImportWizSfmDbProjText2), SW_SHOWNA);
					::ShowWindow(::GetDlgItem(m_hwnd, kctidImportWizSfmDbProjText3), SW_SHOWNA);
					HWND hwndProj = ::GetDlgItem(m_hwnd, kctidImportWizSfmDbProjFile);
					if (!::ShowWindow(hwndProj, SW_SHOWNA))
						::SetFocus(hwndProj);
					::ShowWindow(::GetDlgItem(m_hwnd, kctidImportWizSfmDbProjBrowse),SW_SHOWNA);
					m_pwiz->EnableNextButton(true);
					m_fCheckedDbFileIsSFM = true;
					return true;
				}
				// Look for superficial signs of at least being a standard format file.
				bool fStdFmt = true;
				StrApp strCaption(kstidImportMsgCaption);
				StrApp strFmt;
				StrApp strMsg;
				int cchSlash = 0;
				int cchLinefeed = 0;
				int cchSlashFirst = 0;
				char * pch;
				for (pch = strchr(szLine, '\\'); pch; pch = strchr(pch + 1, '\\'))
				{
					++cchSlash;
					if (pch == szLine || pch[-1] == '\n')
						++cchSlashFirst;
				}
				for (pch = strchr(szLine, '\n'); pch; pch = strchr(pch + 1, '\n'))
				{
					++cchLinefeed;
				}
				int cchLineAvg;
				if (cchLinefeed)
					cchLineAvg = cb / cchLinefeed;
				else
					cchLineAvg = BUFSIZE * 4;

				UINT gfMsgType = MB_OK;
				if (cchSlashFirst * 4 > cchSlash * 3 &&
					(cchLineAvg <= 100 && cchSlashFirst * 8 >= cchLinefeed) ||
					(cchLineAvg > 100 && cchSlashFirst * 5 >= cchLinefeed * 4))
				{
					// "%<0>s is not a Shoebox database file, but appears to be a Standard ..."
					strFmt.Load(kstidImportWizSfmDbNotShoebox);
					gfMsgType |= MB_ICONINFORMATION;
				}
				else
				{
					// "Warning, File %<0>s does not appear to be a Standard Format database!"
					strFmt.Load(kstidImportWizSfmDbNotStdFmt);
					if (!cchSlashFirst)
					{
						fStdFmt = false;
						gfMsgType |= MB_ICONERROR;
					}
					else
					{
						gfMsgType |= MB_ICONWARNING;
					}
				}
				strMsg.Format(strFmt.Chars(), pszFile);
				if (fInformUser || !fStdFmt)
					::MessageBox(m_hwnd, strMsg.Chars(), strCaption.Chars(), gfMsgType);
				if (fStdFmt)
				{
					// Not a shoebox file -- hide the project file controls.
					::ShowWindow(::GetDlgItem(m_hwnd, kctidImportWizSfmDbProjText1), SW_HIDE);
					::ShowWindow(::GetDlgItem(m_hwnd, kctidImportWizSfmDbProjText2), SW_HIDE);
					::ShowWindow(::GetDlgItem(m_hwnd, kctidImportWizSfmDbProjText3), SW_HIDE);
					::ShowWindow(::GetDlgItem(m_hwnd, kctidImportWizSfmDbProjFile), SW_HIDE);
					::ShowWindow(::GetDlgItem(m_hwnd, kctidImportWizSfmDbProjBrowse), SW_HIDE);
					m_pwiz->EnableNextButton(true);
					m_fCheckedDbFileIsSFM = true;
					return true;
				}
			}
		}
	}
	// Not a valid file -- hide the project file controls.
	::ShowWindow(::GetDlgItem(m_hwnd, kctidImportWizSfmDbProjText1), SW_HIDE);
	::ShowWindow(::GetDlgItem(m_hwnd, kctidImportWizSfmDbProjText2), SW_HIDE);
	::ShowWindow(::GetDlgItem(m_hwnd, kctidImportWizSfmDbProjText3), SW_HIDE);
	::ShowWindow(::GetDlgItem(m_hwnd, kctidImportWizSfmDbProjFile), SW_HIDE);
	::ShowWindow(::GetDlgItem(m_hwnd, kctidImportWizSfmDbProjBrowse), SW_HIDE);
	m_pwiz->EnableNextButton(false);
	m_fCheckedDbFileIsSFM = false;
	return false;
}

/*----------------------------------------------------------------------------------------------
	Disable the Next button if we don't have a valid Shoebox database file.

	@return True if the page was successfully set active; otherwise false.
----------------------------------------------------------------------------------------------*/
bool RnImportWizSfmDb::OnSetActive()
{
	// Set the filenames.
	RnImportWizard * priw = dynamic_cast<RnImportWizard *>(m_pwiz);
	AssertPtr(priw);
	const achar * pszFile;
	if (m_ishbx != priw->GetSettingsIndex())
	{
		m_ishbx = priw->GetSettingsIndex();
		pszFile = priw->GetDbFile();
		CheckForShoebox(pszFile, false);
		if (pszFile)
			::SetDlgItemText(m_hwnd, kctidImportWizSfmDbFile, pszFile);

		pszFile = priw->GetPrjFile();
		if (pszFile)
			::SetDlgItemText(m_hwnd, kctidImportWizSfmDbProjFile, pszFile);
	}
	else
	{
		CheckForShoebox(m_strDbFile.Chars(), false);
		if (m_strDbFile.Chars())
			::SetDlgItemText(m_hwnd, kctidImportWizSfmDbFile, m_strDbFile.Chars());

		if (m_strPrjFile.Chars())
			::SetDlgItemText(m_hwnd, kctidImportWizSfmDbProjFile, m_strPrjFile.Chars());
	}

	return SuperClass::OnSetActive();
}

/*----------------------------------------------------------------------------------------------
	Enable the Next button since we're moving back.

	@return NULL (advances to the previous page).
----------------------------------------------------------------------------------------------*/
HWND RnImportWizSfmDb::OnWizardBack()
{
	m_pwiz->EnableNextButton(true);

	m_strCheckedDbFile.Clear();
	m_fCheckedDbFileIsSFM = false;

	// Save the names of the Shoebox database and project files.
	RnImportWizard * priw = dynamic_cast<RnImportWizard *>(m_pwiz);
	AssertPtr(priw);
	achar rgchFile[MAX_PATH];
	int cch = ::GetDlgItemText(m_hwnd, kctidImportWizSfmDbFile, rgchFile, MAX_PATH);
	priw->SetDbFile(rgchFile);
	m_strDbFile.Assign(rgchFile);
	if (::IsWindowVisible(::GetDlgItem(m_hwnd, kctidImportWizSfmDbProjFile)))
	{
		cch = ::GetDlgItemText(m_hwnd, kctidImportWizSfmDbProjFile, rgchFile, MAX_PATH);
		priw->SetPrjFile(rgchFile);
		m_strPrjFile.Assign(rgchFile);
	}

	return NULL;
}

/*----------------------------------------------------------------------------------------------
	Read the Shoebox input file to build a list of field codes.  Merge these into those already
	established by the settings previously selected.

	@return handle of the next page to advance to.
----------------------------------------------------------------------------------------------*/
HWND RnImportWizSfmDb::OnWizardNext()
{
	RnImportWizard * priw = dynamic_cast<RnImportWizard *>(m_pwiz);
	AssertPtr(priw);
	// Save the name of the Shoebox database file.
	achar rgchFile[MAX_PATH];
	int cch = ::GetDlgItemText(m_hwnd, kctidImportWizSfmDbFile, rgchFile, MAX_PATH);
	if (cch)
		priw->SetDbFile(rgchFile);
	m_strDbFile.Assign(rgchFile);
	// Ensure we have all the field codes actually found in the Shoebox database file.
	ScanDbForCodes();
	// Try to find and read the PRJ file.
	if (::IsWindowVisible(::GetDlgItem(m_hwnd, kctidImportWizSfmDbProjFile)))
	{
		cch = ::GetDlgItemText(m_hwnd, kctidImportWizSfmDbProjFile, rgchFile, MAX_PATH);
		// Save the name of the Shoebox project file.
		priw->SetPrjFile(rgchFile);
		m_strPrjFile.Assign(rgchFile);
		if (!cch)
			return NULL;
		StrAppBufPath strbpFile(rgchFile);
		int ich1 = strbpFile.ReverseFindCh('\\');
		int ich2 = strbpFile.ReverseFindCh('/');
		if (ich2 >= 0 && ich1 < ich2)
			ich1 = ich2;
		++ich1;				// Move past final slash, or to beginning of string if no slashes.
		strbpFile.Replace(ich1, strbpFile.Length(), "*.typ");
		WIN32_FIND_DATA wfd;
		HANDLE hFind;
		hFind = ::FindFirstFile(strbpFile.Chars(), &wfd);
		if (hFind != INVALID_HANDLE_VALUE)
		{
			do
			{
				strbpFile.Replace(ich1, strbpFile.Length(), wfd.cFileName);
				// Load this TYP file if it is the proper one.
				LoadTypFile(strbpFile.Chars());
			} while (::FindNextFile(hFind, &wfd));
			::FindClose(hFind);
		}
	}
	return NULL;
}

/*----------------------------------------------------------------------------------------------
	Check this file to see if it is the desired TYP file.  If it is, load the information from
	it.

	@param pszFilename Full pathname of a candidate TYP file.

	@return True if the TYP file is the right one, and is loaded successfully; otherwise false.
----------------------------------------------------------------------------------------------*/
bool RnImportWizSfmDb::LoadTypFile(const achar * pszFilename)
{
	HANDLE hFile = ::CreateFile(pszFilename, GENERIC_READ, FILE_SHARE_READ,
		NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
	if (hFile == INVALID_HANDLE_VALUE)
		return false;

	DWORD cbFileHigh = 0;
	DWORD cbFile = ::GetFileSize(hFile, &cbFileHigh);
	if (cbFile == -1 || cbFileHigh != 0)
	{
		// Even if it's not an error, the file is way too big to be realistic!
		return false;
	}
	Vector<char> vcb;
	vcb.Resize(cbFile + 1);
	DWORD cb;
	bool fOk = ::ReadFile(hFile, vcb.Begin(), cbFile, &cb, NULL);
	::CloseHandle(hFile);
	if (!fOk)
		return false;
	vcb[cbFile] = '\0';
	RnImportWizard * priw = dynamic_cast<RnImportWizard *>(m_pwiz);
	AssertPtr(priw);
	char * pszLine = vcb.Begin();
	char * pszNext = strpbrk(pszLine, "\r\n");
	if (pszNext)
	{
		*pszNext++ = '\0';
		pszNext += strspn(pszNext, "\r\n");
	}
	char * pszVal = strpbrk(pszLine, " \t");
	if (!pszVal)
		return false;
	*pszVal++ = '\0';
	pszVal += strspn(pszVal, " \t");
	if (strcmp(pszLine, "\\+DatabaseType") != 0 || strcmp(pszVal, priw->GetTypeName()) != 0)
		return false;

	bool fDefiningMarkers = false;
	char * pszDefLang = NULL;
	char * pszRecMkr = NULL;
	int isfmRec = -1;
	char * pszMkr;
	char * pszNam;
	char * pszLng;
	char * pszMkrOverThis;
	Vector<RnSfMarker> vsfm;
	for (pszLine = pszNext; pszLine && *pszLine; pszLine = pszNext)
	{
		pszNext = strpbrk(pszLine, "\r\n");
		if (pszNext)
		{
			*pszNext++ = '\0';
			pszNext += strspn(pszNext, "\r\n");
		}
		pszVal = strpbrk(pszLine, " \t");
		if (pszVal)
		{
			*pszVal++ = '\0';
			pszVal += strspn(pszVal, " \t");
		}
		else
		{
			pszVal = pszLine + strlen(pszLine);
		}
		if (strcmp(pszLine, "\\+mkrset") == 0)
		{
			fDefiningMarkers = true;
		}
		else if (strcmp(pszLine, "\\lngDefault") == 0)
		{
			Assert(fDefiningMarkers);
			if (fDefiningMarkers)
				pszDefLang = pszVal;
		}
		else if (strcmp(pszLine, "\\mkrRecord") == 0)
		{
			Assert(fDefiningMarkers);
			if (fDefiningMarkers)
				pszRecMkr = pszVal;
		}
		else if (strcmp(pszLine, "\\+mkr") == 0)
		{
			Assert(fDefiningMarkers);
			if (fDefiningMarkers)
			{
				pszMkr = pszVal;
				pszNam = NULL;
				pszLng = NULL;
				pszMkrOverThis = NULL;
			}
		}
		else if (strcmp(pszLine, "\\nam") == 0)
		{
			Assert(fDefiningMarkers);
			if (fDefiningMarkers)
				pszNam = pszVal;
		}
		else if (strcmp(pszLine, "\\lng") == 0)
		{
			Assert(fDefiningMarkers);
			if (fDefiningMarkers)
				pszLng = pszVal;
		}
		else if (strcmp(pszLine, "\\mkrOverThis") == 0)
		{
			Assert(fDefiningMarkers);
			if (fDefiningMarkers)
				pszMkrOverThis = pszVal;
		}
		else if (strcmp(pszLine, "\\-mkr") == 0)
		{
			Assert(fDefiningMarkers);
			if (fDefiningMarkers)
			{
				RnSfMarker sfm(pszMkr, pszNam, pszLng, pszMkrOverThis);
				if (pszRecMkr && pszMkr && strcmp(pszRecMkr, pszMkr) == 0)
				{
					isfmRec = vsfm.Size();
					sfm.m_rt = RnSfMarker::krtEvent;
					sfm.m_nLevel = 1;
				}
				vsfm.Push(sfm);
				pszMkr = NULL;
				pszNam = NULL;
				pszLng = NULL;
				pszMkrOverThis = NULL;
			}
		}
		else if (strcmp(pszLine, "\\-mkrset") == 0)
		{
			fDefiningMarkers = false;
			break;
		}
	}
	if (isfmRec == -1)
		return false;

	Vector<RnSfMarker> & vsfmFields = priw->Settings()->FieldCodes();
	HashMapChars<int> & hmcisfm = priw->Settings()->FieldCodesMap();
	int isfm;
	int isfmOld;
	int isfmRecOld = priw->Settings()->RecordIndex();
	int isfmNew;
	// Add the new marker values to the existing list, setting the new record marker along the
	// way.
	for (isfm = 0; isfm < vsfm.Size(); ++isfm)
	{
		RnSfMarker & sfmNew = vsfm[isfm];
		if (hmcisfm.Retrieve(sfmNew.m_stabsMkr.Chars(), &isfmOld))
		{
			RnSfMarker & sfmOld = vsfmFields[isfmOld];
			if (sfmOld.m_stabsNam.Length() < sfmNew.m_stabsNam.Length())
				sfmOld.m_stabsNam = sfmNew.m_stabsNam;
			sfmOld.m_stabsLng = sfmNew.m_stabsLng;
			sfmOld.m_stabsMkrOverThis = sfmNew.m_stabsMkrOverThis;
			if (isfm == isfmRec)
			{
				priw->Settings()->SetRecordIndex(isfmOld);
				sfmOld.m_rt = RnSfMarker::krtEvent;
				sfmOld.m_nLevel = 1;
				if (isfmOld != isfmRecOld)
				{
					vsfmFields[isfmRecOld].m_rt = RnSfMarker::krtNone;
					vsfmFields[isfmRecOld].m_nLevel = 0;
				}
			}
		}
		else
		{
			isfmNew = vsfmFields.Size();
			if (isfm == isfmRec)
			{
				vsfmFields[isfmRecOld].m_rt = RnSfMarker::krtNone;
				vsfmFields[isfmRecOld].m_nLevel = 0;
				sfmNew.m_rt = RnSfMarker::krtEvent;
				sfmNew.m_nLevel = 1;
				priw->Settings()->SetRecordIndex(isfmNew);
			}
			hmcisfm.Insert(sfmNew.m_stabsMkr.Chars(), isfmNew);
			vsfmFields.Push(sfmNew);
		}
	}
	priw->SetTypFile(pszFilename);
	return true;
}

/*----------------------------------------------------------------------------------------------
	Read through the selected Shoebox database file, building the list of field codes.
----------------------------------------------------------------------------------------------*/
void RnImportWizSfmDb::ScanDbForCodes()
{
	RnImportWizard * priw = dynamic_cast<RnImportWizard *>(m_pwiz);
	AssertPtr(priw);
	AssertPsz(priw->GetDbFile());
	HANDLE hFile = ::CreateFile(priw->GetDbFile(), GENERIC_READ, FILE_SHARE_READ,
		NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
	if (hFile == INVALID_HANDLE_VALUE)
		return;
	DWORD cbFileHigh = 0;
	DWORD cbFile = ::GetFileSize(hFile, &cbFileHigh);
	if (cbFile == -1 || cbFileHigh != 0)
	{
		Assert(sizeof(DWORD) >= 4);
		// Even if it's not an error, the file is way too big to be realistic!
		return;
	}
	RnImportShoeboxData * prisd = priw->StoredData();
	prisd->m_vpszFields.Clear();
	prisd->m_vvpszLines.Clear();
	prisd->m_hmcLines.Clear();
	prisd->m_vcb.Resize(cbFile + 1);
	DWORD cb;
	bool fOk = ::ReadFile(hFile, prisd->m_vcb.Begin(), cbFile, &cb, NULL);
	::CloseHandle(hFile);
	if (!fOk)
		return;
	prisd->m_vcb[cbFile] = '\0';
	char * pszLine;
	char * pszNext;
	char * psz;
	Set<StrAnsiBufSmall> setstaCodes;
	StrAnsiBufSmall stabs;
	int ivpsz;
	char ch;
	char * pszBegin = prisd->m_vcb.Begin();
	for (pszLine = pszBegin; pszLine && *pszLine; pszLine = pszNext)
	{
		pszNext = strpbrk(pszLine, "\r\n");
		if (pszNext)
		{
			pszNext += strspn(pszNext, "\r\n");
		}
		if (pszLine[0] == '\\' && pszLine[1] != '_')
		{
			psz = strpbrk(pszLine, kszSpace);
			if (psz)
			{
				ch = *psz;
				*psz = '\0';
			}
			::ZeroMemory(&stabs, isizeof(stabs));	// (needed for HashObj and EqlObj to work.)
			stabs.Assign(pszLine + 1);
			setstaCodes.Insert(stabs);
			if (psz)
				*psz = ch;
			prisd->m_vpszFields.Push(pszLine);
			if (!prisd->m_hmcLines.Retrieve(stabs.Chars(), &ivpsz))
			{
				ivpsz = prisd->m_vvpszLines.Size();
				prisd->m_vvpszLines.Resize(ivpsz + 1);
				prisd->m_hmcLines.Insert(stabs.Chars(), ivpsz);
			}
			prisd->m_vvpszLines[ivpsz].Push(pszLine);
			// Trim the preceding line.
			for (psz = pszLine - 1; psz >= pszBegin && strchr(kszSpace, *psz); --psz)
				*psz = '\0';
		}
	}
	// Trim the last line.
	for (psz = pszBegin + cbFile - 1; psz >= pszBegin && strchr(kszSpace, *psz); --psz)
		*psz = '\0';
	Vector<RnSfMarker> & vsfmFields = priw->Settings()->FieldCodes();
	HashMapChars<int> & hmcisfm = priw->Settings()->FieldCodesMap();
	Assert(vsfmFields.Size() == hmcisfm.Size());
	RnSfMarker sfmDefRec;
	int isfmDefRec = priw->Settings()->RecordIndex();
	Set<StrAnsiBufSmall>::iterator it;
	RnSfMarker sfm;
	const char * pszMkr;
	int isfm;
	for (it = setstaCodes.Begin(); it != setstaCodes.End(); ++it)
	{
		pszMkr = it.GetValue().Chars();
		if (!hmcisfm.Retrieve(pszMkr, &isfm))
		{
			sfm.m_stabsMkr = pszMkr;
			sfm.m_rt = RnSfMarker::krtNone;
			sfm.m_nLevel = 0;
			isfm = vsfmFields.Size();
			hmcisfm.Insert(pszMkr, isfm);
			vsfmFields.Push(sfm);
		}
	}
	if (isfmDefRec < 0 && vsfmFields.Size())
		priw->Settings()->SetRecordIndex(0);
}


//:>********************************************************************************************
//:>	RnImportWizRecMarker methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
RnImportWizRecMarker::RnImportWizRecMarker()
	: AfWizardPage(kridImportWizRecMarkerDlg)
{
	m_pszHelpUrl = _T("Beginning_Tasks/Import_Standard_Format/Step_4_of_9_Record_Marker.htm");
}

/*----------------------------------------------------------------------------------------------
	Initialize the dialog in response to the WM_INITDIALOG message.
	All one-time initialization should be done here (that is, all controls have been created
	and have valid hwnd's, but they need initial values.)

	@param hwndCtrl Not used by this method.
	@param lp Not used by this method.

	@return True.
----------------------------------------------------------------------------------------------*/
bool RnImportWizRecMarker::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	// Set the font for the page title.
	m_hfontLarge = AfGdi::CreateFont(14, 0, 0, 0, FW_BOLD, FALSE, FALSE, FALSE, ANSI_CHARSET,
		OUT_CHARACTER_PRECIS, CLIP_DEFAULT_PRECIS, DEFAULT_QUALITY, VARIABLE_PITCH | FF_SWISS,
		_T("MS Sans Serif"));
	if (m_hfontLarge)
		::SendMessage(::GetDlgItem(m_hwnd, kcidImportWizRecMarkerHdr), WM_SETFONT,
			(WPARAM)m_hfontLarge, false);
	StrApp str(kstidImportWizRecMarkerStep);
	::SetWindowText(::GetDlgItem(m_hwnd, kcidImportWizRecMarkerStep), str.Chars());

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}

/*----------------------------------------------------------------------------------------------
	Handle a WM_NOTIFY message, first letting the superclass method handle it if possible.

	@param ctidFrom Identifies the control sending the message.
	@param pnmh Pointer to the notification message data.
	@param lnRet Reference to a long integer return value used by some messages.

	@return True if the message is handled successfully; otherwise, false.
----------------------------------------------------------------------------------------------*/
bool RnImportWizRecMarker::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	if (SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet))
		return true;
	RnImportWizard * priw = dynamic_cast<RnImportWizard *>(m_pwiz);
	AssertPtr(priw);
	switch (pnmh->code)
	{
	case CBN_SELCHANGE: // Combo box item changed.
		switch (pnmh->idFrom)
		{
		case kctidImportWizRecMarkerCode:
			{
				int isel = ::SendMessage(pnmh->hwndFrom, CB_GETCURSEL, 0, 0);
				Assert((unsigned)isel < (unsigned)m_visfm.Size());
				int isfm = m_visfm[isel];
				RnImportWizard * priw = dynamic_cast<RnImportWizard *>(m_pwiz);
				AssertPtr(priw);
				int isfmRecord = priw->Settings()->RecordIndex();
				Vector<RnSfMarker> & vsfmFields = priw->Settings()->FieldCodes();
				if (isfm != isfmRecord)
				{
					vsfmFields[isfmRecord].m_rt = RnSfMarker::krtNone;
					vsfmFields[isfmRecord].m_nLevel = 0;
				}
				vsfmFields[isfm].m_rt = RnSfMarker::krtEvent;
				vsfmFields[isfm].m_nLevel = 1;
				priw->Settings()->SetRecordIndex(isfm);
				return true;
			}
		}
	case BN_CLICKED:
		if (ctidFrom == kctidImportWizRecMarkerView)
		{
			StrAppBufPath strbpFwPath;
			strbpFwPath.Assign(DirectoryFinder::FwRootCodeDir().Chars());
			if (strbpFwPath[strbpFwPath.Length() - 1] != '\\')
				strbpFwPath.Append("\\");

			strbpFwPath.Append(_T("ZEdit.exe"));

			STARTUPINFO suinfo;
			::ZeroMemory(&suinfo, isizeof(suinfo));
			suinfo.cb = isizeof(suinfo);

			StrAppBuf strbCmd;
			strbCmd.Format(_T(" \"%s\""), priw->GetDbFile());

			PROCESS_INFORMATION procinfo;
			bool fOk = ::CreateProcess(strbpFwPath.Chars(),	// Name of executable module.
				const_cast<achar *>(strbCmd.Chars()),		// Command line string.
				NULL,									// Process Security Attributes.
				NULL,									// Thread Security Attributes.
				false,									// Handle inheritance option.
				CREATE_DEFAULT_ERROR_MODE,		// Creation flags.
				NULL,									// New environment block.
				NULL,									// Current directory name.
				&suinfo,									// Startup information.
				&procinfo);								// Process information.
			return fOk;
		}
		break;
	}
	return false;
}


/*----------------------------------------------------------------------------------------------
	Load the current set of field markers into the combobox, and select the designated record
	marker.

	@return True if the page was successfully set active; otherwise false.
----------------------------------------------------------------------------------------------*/
bool RnImportWizRecMarker::OnSetActive()
{
	HWND hwnd = ::GetDlgItem(m_hwnd, kctidImportWizRecMarkerCode);
	::SendMessage(hwnd, CB_RESETCONTENT, 0, 0);

	// Fill the combobox with the field names, omitting those which do not appear in the data.

	RnImportWizard * priw = dynamic_cast<RnImportWizard *>(m_pwiz);
	AssertPtr(priw);
	RnImportShoeboxData * prisd = priw->StoredData();
	AssertPtr(prisd);
	Vector<RnSfMarker> & vsfmFields = priw->Settings()->FieldCodes();
	int isfm;
	int isfmRecord = priw->Settings()->RecordIndex();
	bool fSel = false;
	int cfld;
	int ivpsz;
	int irec;
	m_visfm.Clear();
	StrAppBufSmall strbs;
	for (irec = 0, isfm = 0; isfm < vsfmFields.Size(); ++isfm)
	{
		RnSfMarker & sfm = vsfmFields[isfm];
		if (prisd->m_hmcLines.Retrieve(sfm.m_stabsMkr.Chars(), &ivpsz))
			cfld = prisd->m_vvpszLines[ivpsz].Size();
		else
			cfld = 0;
		if (!cfld)
			continue;
		m_visfm.Push(isfm);
		strbs = vsfmFields[isfm].m_stabsMkr;
		::SendMessage(hwnd, CB_ADDSTRING, 0, (LPARAM)strbs.Chars());
		if (isfmRecord == isfm)
		{
			::SendMessage(hwnd, CB_SETCURSEL, irec, 0);
			fSel = true;
		}
		++irec;
	}
	if (!fSel)
		::SendMessage(hwnd, CB_SETCURSEL, 0, 0);

	return SuperClass::OnSetActive();
}

#undef USE_REC_TYPES
#ifdef USE_REC_TYPES
//:Ignore
//:>********************************************************************************************
//:>	RnImportWizRecTypes methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
RnImportWizRecTypes::RnImportWizRecTypes()
	: AfWizardPage(kridImportWizRecTypesDlg)
{
	m_pszHelpUrl = "Beginning_Tasks/Import_Standard_Format/Step_5_of_9_Content_Mapping.htm";
}

/*----------------------------------------------------------------------------------------------
	Initialize the dialog in response to the WM_INITDIALOG message.
	All one-time initialization should be done here (that is, all controls have been created
	and have valid hwnd's, but they need initial values.)

	@param hwndCtrl Not used by this method.
	@param lp Not used by this method.

	@return True.
----------------------------------------------------------------------------------------------*/
bool RnImportWizRecTypes::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	// Set the font for the page title.
	m_hfontLarge = AfGdi::CreateFont(14, 0, 0, 0, FW_BOLD, FALSE, FALSE, FALSE, ANSI_CHARSET,
		OUT_CHARACTER_PRECIS, CLIP_DEFAULT_PRECIS, DEFAULT_QUALITY, VARIABLE_PITCH | FF_SWISS,
		"MS Sans Serif");
	if (m_hfontLarge)
		::SendMessage(::GetDlgItem(m_hwnd, kcidImportWizRecTypesHdr), WM_SETFONT,
			(WPARAM)m_hfontLarge, false);

	// Define the two columns for the listbox.
	HWND hwnd = ::GetDlgItem(m_hwnd, kcidImportWizRecTypesList);
	RECT rect;
	int rgnT[2] = { 0, 0 };
	::GetEffectiveClientRect(hwnd, &rect, rgnT);
	int cx = rect.right - rect.left;
	int cxLeft = (cx * 2) / 5;
	int cxRight = cx - cxLeft;
	LVCOLUMN lvc;
	::ZeroMemory(&lvc, isizeof(lvc));
	lvc.mask = LVCF_WIDTH;
	lvc.cx = cxLeft;
	ListView_InsertColumn(hwnd, 0, &lvc);
	lvc.cx = cxRight;
	lvc.mask |= LVCF_SUBITEM;
	lvc.iSubItem = 1;
	ListView_InsertColumn(hwnd, 1, &lvc);
	ListView_SetExtendedListViewStyle(hwnd, LVS_EX_FULLROWSELECT);

	// Load the relevant resource strings.
	m_strEvent.Load(kstidImportWizRecTypesEvent);
	m_strEventLevFmt.Load(kstidImportWizRecTypesEventLev);
	m_strHypo.Load(kstidImportWizRecTypesHypo);
	m_strHypoLevFmt.Load(kstidImportWizRecTypesHypoLev);

	// Disable modify and delete buttons.
	hwnd = ::GetDlgItem(m_hwnd, kctidImportWizRecTypesModify);
	::EnableWindow(hwnd, false);
	hwnd = ::GetDlgItem(m_hwnd, kctidImportWizRecTypesDelete);
	::EnableWindow(hwnd, false);

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}

/*----------------------------------------------------------------------------------------------
	Initialize the listbox with what we know.

	@return True if the page was successfully set active; otherwise false.
----------------------------------------------------------------------------------------------*/
bool RnImportWizRecTypes::OnSetActive()
{
	Assert(m_strEvent.Length());

	InitializeList();
	return SuperClass::OnSetActive();
}


/*----------------------------------------------------------------------------------------------
	Handle a WM_NOTIFY message, first letting the superclass method handle it if possible.

	@param ctidFrom Identifies the control sending the message.
	@param pnmh Pointer to the notification message data.
	@param lnRet Reference to a long integer return value used by some messages.

	@return True if the message is handled successfully; otherwise, false.
----------------------------------------------------------------------------------------------*/
bool RnImportWizRecTypes::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	if (SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet))
		return true;
	RnImportWizard * priw = dynamic_cast<RnImportWizard *>(m_pwiz);
	AssertPtr(priw);
	HWND hwnd = ::GetDlgItem(m_hwnd, kcidImportWizRecTypesList);
	switch (pnmh->code)
	{
	case BN_CLICKED:
		if (ctidFrom == kctidImportWizRecTypesAdd)
		{
			RnImportRecTypeDlgPtr qrirtd;
			qrirtd.Create();
			qrirtd->Initialize(this, "", RnSfMarker::krtNone, 0);
			if (qrirtd->DoModal(m_hwnd) == kctidOk)
			{
				const char * pszMkr = qrirtd->GetMarker();
				int isfm;
				for (isfm = 0; isfm < m_vsfmHier.Size(); ++isfm)
				{
					if (strcmp(pszMkr, m_vsfmHier[isfm].m_stabsMkr.Chars()) == 0)
					{
						StrApp strCaption(kstidImportMsgCaption);
						StrApp strMsgFmt(kstidImportWizRecTypesAlreadyFmt);
						StrApp strMkr = pszMkr;
						StrApp str;
						str.Format(strMsgFmt.Chars(), strMkr.Chars());
						::MessageBox(m_hwnd, str.Chars(), strCaption.Chars(),
							MB_OK | MB_ICONWARNING);
						return true;
					}
				}
				HashMapChars<int> & hmcisfm = priw->Settings()->FieldCodesMap();
				// Store the new field marker record type.
				if (hmcisfm.Retrieve(pszMkr, &isfm))
				{
					Vector<RnSfMarker> & vsfmFields = priw->Settings()->FieldCodes();
					vsfmFields[isfm].m_rt = qrirtd->GetType();
					vsfmFields[isfm].m_nLevel = qrirtd->GetLevel();
					InitializeList();		// Rebuild the display list from scratch.
				}
				return true;
			}
		}
		else
		{
			int isel = GetFirstListViewSelection(hwnd);
			if (isel < 0)
				return false;
			if (ctidFrom == kctidImportWizRecTypesModify)
			{
				ModifyRecType(isel);
				ListView_EnsureVisible(hwnd, isel, FALSE);
				return true;
			}
			else if (ctidFrom == kctidImportWizRecTypesDelete)
			{
				StrApp strCaption(kstidImportMsgCaption);
				int isfm;
				Vector<RnSfMarker> & vsfmFields = priw->Settings()->FieldCodes();
				for (isfm = 0; isfm < vsfmFields.Size(); ++isfm)
				{
					if (vsfmFields[isfm].m_stabsMkr == m_vsfmHier[isel].m_stabsMkr)
					{
						StrApp strMkr = m_vsfmHier[isel].m_stabsMkr.Chars();
						StrApp str;
						if (isfm == priw->Settings()->RecordIndex())
						{
							// Can't delete record marker from the list!
							StrApp strMsgFmt(kstidImportWizRecTypesCannotDelFmt);
							str.Format(strMsgFmt.Chars(), strMkr.Chars());
							::MessageBox(m_hwnd, str.Chars(), strCaption.Chars(),
								MB_OK | MB_ICONWARNING);
							break;
						}
						// Ask the user to confirm the deletion of the selected mapping.
						StrApp strMsgFmt(kstidImportWizRecTypesDelFmt);
						str.Format(strMsgFmt.Chars(), strMkr.Chars());
						int nT = ::MessageBox(m_hwnd, str.Chars(), strCaption.Chars(),
							MB_YESNO | MB_DEFBUTTON2 | MB_ICONQUESTION);
						if (nT == IDYES)
						{
							vsfmFields[isfm].m_rt = RnSfMarker::krtNone;
							vsfmFields[isfm].m_nLevel = 0;
							InitializeList();		// Rebuild the display list from scratch.
						}
						break;
					}
				}
				return true;
			}
		}
		break;

	case NM_DBLCLK:
		if (ctidFrom == kcidImportWizRecTypesList)
		{
			NMITEMACTIVATE * pnmia = reinterpret_cast<NMITEMACTIVATE *>(pnmh);
			ModifyRecType(pnmia->iItem);
			return true;
		}
		break;
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Modify the record type for a standard format field, possibly changing either the type or the
	field marker.

	@param isel Index of the standard format field marker to modify the record type for.
----------------------------------------------------------------------------------------------*/
void RnImportWizRecTypes::ModifyRecType(int isel)
{
	StrApp strCaption(kstidImportMsgCaption);
	RnImportWizard * priw = dynamic_cast<RnImportWizard *>(m_pwiz);
	AssertPtr(priw);
	Vector<RnSfMarker> & vsfmFields = priw->Settings()->FieldCodes();
	int isfmRecord = priw->Settings()->RecordIndex();
	RnImportRecTypeDlgPtr qrirtd;
	qrirtd.Create();
	RnSfMarker & sfm = m_vsfmHier[isel];
	const char * pszMkrOrig = sfm.m_stabsMkr.Chars();
	qrirtd->Initialize(this, pszMkrOrig, sfm.m_rt, sfm.m_nLevel);
	if (qrirtd->DoModal(m_hwnd) == kctidOk)
	{
		StrAnsiBufSmall stabsMkrNew = qrirtd->GetMarker();
		int nLevelNew = qrirtd->GetLevel();
		if (sfm.m_stabsMkr == vsfmFields[isfmRecord].m_stabsMkr)
		{
			StrApp strMkr = stabsMkrNew.Chars();
			StrApp str;
			if (stabsMkrNew != sfm.m_stabsMkr)
			{
				// Can't delete record marker from the list!
				StrApp strMsgFmt(kstidImportWizRecTypesCannotDelFmt);
				str.Format(strMsgFmt.Chars(), strMkr.Chars());
				::MessageBox(m_hwnd, str.Chars(), strCaption.Chars(),
					MB_OK | MB_ICONWARNING);
				return;
			}
			else if (nLevelNew > 1)
			{
				// Record marker must map to top-level record type.
				StrApp strMsgFmt(kstidImportWizRecTypesLevelOneFmt);
				str.Format(strMsgFmt.Chars(), strMkr.Chars());
				::MessageBox(m_hwnd, str.Chars(), strCaption.Chars(),
					MB_OK | MB_ICONWARNING);
				return;
			}
		}
		int isfm;
		for (isfm = 0; isfm < m_vsfmHier.Size(); ++isfm)
		{
			if (isfm == isel)
				continue;
			if (stabsMkrNew == m_vsfmHier[isfm].m_stabsMkr.Chars())
			{
				// Can't duplicate mappings from one SFM field code.
				StrApp strMsgFmt(kstidImportWizRecTypesAlreadyFmt);
				StrApp strMkr = stabsMkrNew.Chars();
				StrApp str;
				str.Format(strMsgFmt.Chars(), strMkr.Chars());
				::MessageBox(m_hwnd, str.Chars(), strCaption.Chars(),
					MB_OK | MB_ICONWARNING);
				return;
			}
		}
		HashMapChars<int> & hmcisfm = priw->Settings()->FieldCodesMap();
		// Reset the original field marker record type.
		if (hmcisfm.Retrieve(pszMkrOrig, &isfm))
		{
			vsfmFields[isfm].m_rt = RnSfMarker::krtNone;
			vsfmFields[isfm].m_nLevel = 0;
		}
		// Store the new field marker record type.
		if (hmcisfm.Retrieve(stabsMkrNew.Chars(), &isfm))
		{
			vsfmFields[isfm].m_rt = qrirtd->GetType();
			vsfmFields[isfm].m_nLevel = qrirtd->GetLevel();
		}
		InitializeList();		// Rebuild the display list from scratch.
	}
}

/*----------------------------------------------------------------------------------------------
	Initialize the contents of the List control, and related member variables.
----------------------------------------------------------------------------------------------*/
void RnImportWizRecTypes::InitializeList()
{
	HWND hwnd = ::GetDlgItem(m_hwnd, kcidImportWizRecTypesList);
	ListView_DeleteAllItems(hwnd);

	RnImportWizard * priw = dynamic_cast<RnImportWizard *>(m_pwiz);
	AssertPtr(priw);
	Vector<RnSfMarker> & vsfmFields = priw->Settings()->FieldCodes();

	LVITEM lvi;
	::ZeroMemory(&lvi, isizeof(lvi));
	lvi.mask = LVIF_TEXT;
	StrAppBuf strb;
	StrAppBufSmall strbsFmt("\\%s");
	StrAppBufSmall strbsT;
	int isfm;
	RnSfMarker::RecordType rt;
	int nLevel;
	m_nLevelMax = 0;
	m_vsfmHier.Clear();
	int isel;
	for (isel = 0, isfm = 0; isfm < vsfmFields.Size(); ++isfm)
	{
		rt = vsfmFields[isfm].m_rt;
		if (rt == RnSfMarker::krtNone)
			continue;
		m_vsfmHier.Push(vsfmFields[isfm]);
		strbsT = vsfmFields[isfm].m_stabsMkr.Chars();
		strb.Format(strbsFmt.Chars(), strbsT.Chars());		// Allow 8-bit or Unicode.
		lvi.pszText = const_cast<achar *>(strb.Chars());
		lvi.iItem = isel;
		ListView_InsertItem(hwnd, &lvi);
		nLevel = vsfmFields[isfm].m_nLevel;
		if (m_nLevelMax < nLevel)
			m_nLevelMax = nLevel;
		if (rt == RnSfMarker::krtEvent)
		{
			if (nLevel == 1)
				strb = m_strEvent.Chars();
			else
				strb.Format(m_strEventLevFmt.Chars(), nLevel);
		}
		else
		{
			Assert(rt == RnSfMarker::krtAnalysis);
			if (nLevel == 1)
				strb = m_strHypo.Chars();
			else
				strb.Format(m_strHypoLevFmt.Chars(), nLevel);
		}
		ListView_SetItemText(hwnd, isel, 1, const_cast<achar *>(strb.Chars()));
		++isel;
	}
}
//:End Ignore
#endif /*USE_REC_TYPES*/

//:>********************************************************************************************
//:>	RnImportWizFldMappings methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
RnImportWizFldMappings::RnImportWizFldMappings()
	: AfWizardPage(kridImportWizFldMappingsDlg)
{
	m_pszHelpUrl = _T("Beginning_Tasks/Import_Standard_Format/Step_5_of_9_Content_Mapping.htm");
}

/*----------------------------------------------------------------------------------------------
	Initialize the dialog in response to the WM_INITDIALOG message.
	All one-time initialization should be done here (that is, all controls have been created
	and have valid hwnd's, but they need initial values.)

	@param hwndCtrl Not used by this method.
	@param lp Not used by this method.

	@return True.
----------------------------------------------------------------------------------------------*/
bool RnImportWizFldMappings::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	// Set the font for the page title.
	m_hfontLarge = AfGdi::CreateFont(14, 0, 0, 0, FW_BOLD, FALSE, FALSE, FALSE, ANSI_CHARSET,
		OUT_CHARACTER_PRECIS, CLIP_DEFAULT_PRECIS, DEFAULT_QUALITY, VARIABLE_PITCH | FF_SWISS,
		_T("MS Sans Serif"));
	if (m_hfontLarge)
		::SendMessage(::GetDlgItem(m_hwnd, kcidImportWizFldMappingsHdr), WM_SETFONT,
			(WPARAM)m_hfontLarge, false);
	StrApp str(kstidImportWizFldMappingsStep);
	::SetWindowText(::GetDlgItem(m_hwnd, kcidImportWizFldMappingsStep), str.Chars());

	// Define the three columns for the listbox.
	HWND hwnd = ::GetDlgItem(m_hwnd, kcidImportWizFldMappingsList);
	RECT rect;
	int rgnT[2] = { 0, 0 };
	::GetEffectiveClientRect(hwnd, &rect, rgnT);
	int cx = rect.right - rect.left;
	int cxLeft = cx / 6;
	int cxMiddle = (cx / 2) - cxLeft;
	int cxRight = cx - (cxLeft + cxMiddle) - 16;		// Allow room for scroll bar.
	LVCOLUMN lvc;
	::ZeroMemory(&lvc, isizeof(lvc));
	lvc.mask = LVCF_WIDTH | LVCF_TEXT;
	lvc.cx = cxLeft;
	str.Load(kstidImportWizFldMapMarker);
	lvc.pszText = const_cast<achar *>(str.Chars());
	ListView_InsertColumn(hwnd, 0, &lvc);
	lvc.mask |= LVCF_SUBITEM;
	lvc.iSubItem = 1;
	lvc.cx = cxMiddle;
	str.Load(kstidImportWizFldMapDescription);
	lvc.pszText = const_cast<achar *>(str.Chars());
	ListView_InsertColumn(hwnd, 1, &lvc);
	lvc.iSubItem = 2;
	lvc.cx = cxRight;
	str.Load(kstidImportWizFldMapDestination);
	lvc.pszText = const_cast<achar *>(str.Chars());
	ListView_InsertColumn(hwnd, 2, &lvc);
	ListView_SetExtendedListViewStyle(hwnd, LVS_EX_FULLROWSELECT);

	// Initialize the mapping between flid and RnImportDest value.
	m_hmflididst.Clear();
	RnImportWizard * priw = dynamic_cast<RnImportWizard *>(m_pwiz);
	AssertPtr(priw);
	Vector<RnImportDest> & vridst = priw->Destinations();
	int i;
	for (i = 0; i < vridst.Size(); ++i)
	{
		Assert(vridst[i].m_flid != 0);
		m_hmflididst.Insert(vridst[i].m_flid, i);
	}

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}

/*----------------------------------------------------------------------------------------------
	Handle a WM_NOTIFY message, first letting the superclass method handle it if possible.

	@param ctidFrom Identifies the control sending the message.
	@param pnmh Pointer to the notification message data.
	@param lnRet Reference to a long integer return value used by some messages.

	@return True if the message is handled successfully; otherwise, false.
----------------------------------------------------------------------------------------------*/
bool RnImportWizFldMappings::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	if (SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet))
		return true;

	switch (pnmh->code)
	{
	case BN_CLICKED:
		{
			HWND hwndList = ::GetDlgItem(m_hwnd, kcidImportWizFldMappingsList);
			int isel = GetFirstListViewSelection(hwndList);
			if (isel < 0)
				return false;
			Assert((uint)isel < (uint)m_visfm.Size());
			if (ctidFrom == kctidImportWizFldMapModify)
			{
				ModifyFieldMapping(isel);
				ListView_EnsureVisible(hwndList, isel, FALSE);
				return true;
			}
			else if (ctidFrom == kctidImportWizFldMapDiscard)
			{
				RnImportWizard * priw = dynamic_cast<RnImportWizard *>(m_pwiz);
				AssertPtr(priw);
				Vector<RnSfMarker> & vsfmFields = priw->Settings()->FieldCodes();
				int isfmSel = m_visfm[isel];
				RnSfMarker & sfmSel = vsfmFields[isfmSel];
				sfmSel.m_flidDst = 0;
				sfmSel.m_wsDefault = 0;
				StrApp strDiscard(kstidImportWizFldMappingDiscard);
				ListView_SetItemText(hwndList, isel, 2,
					const_cast<achar *>(strDiscard.Chars()));
				::EnableWindow(::GetDlgItem(m_hwnd, kctidImportWizFldMapDiscard), FALSE);
				return true;
			}
		}
		break;

	case NM_DBLCLK:
		if (ctidFrom == kcidImportWizFldMappingsList)
		{
			NMITEMACTIVATE * pnmia = reinterpret_cast<NMITEMACTIVATE *>(pnmh);
			ModifyFieldMapping(pnmia->iItem);
			return true;
		}
		break;

	case LVN_ITEMCHANGED:
		if (ctidFrom == kcidImportWizFldMappingsList)
		{
			NMLISTVIEW * pnmv = reinterpret_cast<NMLISTVIEW *>(pnmh);
			AssertPtr(pnmv);
			if ((pnmv->uNewState & LVIS_SELECTED) && !(pnmv->uOldState & LVIS_SELECTED))
			{
				Assert((uint)pnmv->iItem < (uint)m_visfm.Size());
				RnImportWizard * priw = dynamic_cast<RnImportWizard *>(m_pwiz);
				AssertPtr(priw);
				Vector<RnSfMarker> & vsfmFields = priw->Settings()->FieldCodes();
				int isfmSel = m_visfm[pnmv->iItem];
				RnSfMarker & sfmSel = vsfmFields[isfmSel];
				HWND hwnd = ::GetDlgItem(m_hwnd, kctidImportWizFldMapDiscard);
				int idst;
				if (sfmSel.m_flidDst && m_hmflididst.Retrieve(sfmSel.m_flidDst, &idst))
					::EnableWindow(hwnd, TRUE);
				else
					::EnableWindow(hwnd, FALSE);
			}
		}
		break;
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Initialize the listbox with what we know.

	@return True if the page was successfully set active; otherwise false.
----------------------------------------------------------------------------------------------*/
bool RnImportWizFldMappings::OnSetActive()
{
	InitializeList();
	return SuperClass::OnSetActive();
}

/*----------------------------------------------------------------------------------------------
	Scan the appropriate fields from the Shoebox database file to build a list of possible
	character format codes.

	@return handle of the next page to advance to.
----------------------------------------------------------------------------------------------*/
HWND RnImportWizFldMappings::OnWizardNext()
{
	// For each field that maps onto Text (either Simple or Structured), pull out every
	// occurrence of |[a-z][a-z]*{.  Also look for Manuscripter bar codes?
	RnImportWizard * priw = dynamic_cast<RnImportWizard *>(m_pwiz);
	AssertPtr(priw);
	RnShoeboxSettings * pshbx = priw->Settings();
	Vector<RnSfMarker> & vsfmFields = pshbx->FieldCodes();
	Vector<RnImportDest> & vridst = priw->Destinations();
	RnImportShoeboxData * prisd = priw->StoredData();
	Vector<RnImportCharMapping> & vrcmp = pshbx->CharMappings();
	HashMapChars<int> & hmsaicmp = pshbx->CharMappingsMap();

	int isfm;
	int idst;
	int iv;
	int icmp;
	char * pszLine;
	char * pszBar;
	char * pszFlag;
	int cchFlag;
	StrAnsiBuf stabFlag;
	char * pszEnd;
	for (isfm = 0; isfm < vsfmFields.Size(); ++isfm)
	{
		if (!m_hmflididst.Retrieve(vsfmFields[isfm].m_flidDst, &idst))
			continue;
		if (vridst[idst].m_proptype == kcptString ||
			vridst[idst].m_proptype == kcptBigString ||
			vridst[idst].m_proptype == kcptMultiString ||
			vridst[idst].m_proptype == kcptMultiBigString ||
			vridst[idst].m_clidDst == kclidStText)
		{
			// Scan the data for suspicious looking things.
			if (prisd->m_hmcLines.Retrieve(vsfmFields[isfm].m_stabsMkr.Chars(), &iv))
			{
				for (int i = 0; i < prisd->m_vvpszLines[iv].Size(); ++i)
				{
					pszLine = prisd->m_vvpszLines[iv][i];
					do
					{
						stabFlag.Clear();
						pszBar = strchr(pszLine, '|');
						if (!pszBar)
							break;
						pszFlag = pszBar + 1;
						cchFlag = strspn(pszFlag,
							"abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ");
						if (cchFlag == 0)
						{
							pszLine = pszFlag;
							continue;
						}
						pszLine = pszFlag + cchFlag;
						if (pszFlag[cchFlag] == '{')
						{
							stabFlag.Assign(pszBar, cchFlag + 2);
							pszEnd = "}";
						}
						else if (strchr("bi", *pszFlag))
						{
							// Check for subset of Manuscripter bar codes.
							pszEnd = strstr(pszFlag + 1, "|r");
							if (pszEnd)
							{
								stabFlag.Assign(pszBar, 2);
								pszLine = pszEnd + 2;
								pszEnd = "|r";
							}
						}
						if (stabFlag.Length())
						{
							StrAnsi sta(stabFlag.Chars(), stabFlag.Length());
							if (!hmsaicmp.Retrieve(sta, &icmp))
							{
								RnImportCharMapping rcmp;
								rcmp.m_staBegin = sta;
								rcmp.m_staEnd = pszEnd;
								rcmp.m_cmt = RnImportCharMapping::kcmtNone;
								icmp = vrcmp.Size();
								vrcmp.Push(rcmp);
								hmsaicmp.Insert(sta, icmp);
							}
						}
					} while (pszLine && *pszLine);
				}
			}
		}
	}
	return NULL;
}


/*----------------------------------------------------------------------------------------------
	Modify the mapping for a standard format field, possibly changing only the specialized
	options for the chosen import destination.

	@param isel Index of the standard format field marker to modify the mapping for.
----------------------------------------------------------------------------------------------*/
void RnImportWizFldMappings::ModifyFieldMapping(int isel)
{
	RnImportWizard * priw = dynamic_cast<RnImportWizard *>(m_pwiz);
	AssertPtr(priw);
	Vector<RnSfMarker> & vsfmFields = priw->Settings()->FieldCodes();
	Vector<RnImportDest> & vridst = priw->Destinations();
	int isfmSel = m_visfm[isel];
	int idst;
	for (idst = 0; idst < vridst.Size(); ++idst)
	{
		if (vsfmFields[isfmSel].m_flidDst == vridst[idst].m_flid)
			break;
	}
	RnImportFldMappingDlgPtr qrifmd;
	qrifmd.Create();
	qrifmd->Initialize(this, isfmSel, idst);
	if (qrifmd->DoModal(m_hwnd) == kctidOk)
	{
		RnSfMarker & sfmSel = vsfmFields[isfmSel];
		RnSfMarker & sfmOpt = qrifmd->GetDestOptions();
		sfmSel.m_txo = sfmOpt.m_txo;
		sfmSel.m_clo = sfmOpt.m_clo;
		sfmSel.m_dto = sfmOpt.m_dto;
		sfmSel.m_mlo = sfmOpt.m_mlo;
		sfmSel.m_fIgnoreEmpty = sfmOpt.m_fIgnoreEmpty;

		int idstNew = qrifmd->GetDestIndex();
		Assert((uint)idstNew <= (uint)priw->Destinations().Size());
		if (idstNew < priw->Destinations().Size())
		{
			RnImportDest & ridst = priw->Destinations()[idstNew];
			sfmSel.m_flidDst = ridst.m_flid;
			sfmSel.m_wsDefault = 0;
			// Force sanity.
			if (ridst.m_proptype == kcptReferenceAtom)
			{
				sfmSel.m_clo.m_fHaveMulti = false;
				sfmSel.m_clo.m_staDelimMulti.Clear();
			}
		}
		else
		{
			sfmSel.m_flidDst = 0;		// (Discard).
			sfmSel.m_wsDefault = 0;
		}
		InitializeList();		// Rebuild the display list from scratch.
	}
}

/*----------------------------------------------------------------------------------------------
	Initialize the contents of the List control, and related member variables.
----------------------------------------------------------------------------------------------*/
void RnImportWizFldMappings::InitializeList()
{
	HWND hwnd = ::GetDlgItem(m_hwnd, kcidImportWizFldMappingsList);
	int isel = GetFirstListViewSelection(hwnd);
	StrAppBuf strbSel;
	achar rgchText[kcchMaxBufDef];
	if (isel >= 0)
	{
		::ZeroMemory(rgchText, isizeof(rgchText));
		ListView_GetItemText(hwnd, isel, 0, rgchText, kcchMaxBufDef - 1);
		strbSel = rgchText;
	}
	ListView_DeleteAllItems(hwnd);

	RnImportWizard * priw = dynamic_cast<RnImportWizard *>(m_pwiz);
	AssertPtr(priw);
	Vector<RnSfMarker> & vsfmFields = priw->Settings()->FieldCodes();
	Vector<RnImportDest> & vridst = priw->Destinations();

	// Fill in m_visfm with the indexes of fields, sorted by name, and omitting those which do
	// not appear in the data.
	RnImportShoeboxData * prisd = priw->StoredData();
	int ivpsz;
	int cfld;
	m_visfm.Clear();
	int isfm;
	int iv;
	int ivMid;
	int ivLim;
	for (isfm = 0; isfm < vsfmFields.Size(); ++isfm)
	{
		RnSfMarker & sfm = vsfmFields[isfm];
		if (prisd->m_hmcLines.Retrieve(sfm.m_stabsMkr.Chars(), &ivpsz))
			cfld = prisd->m_vvpszLines[ivpsz].Size();
		else
			cfld = 0;
		if (!cfld)
			continue;
		// Insert sorted by m_stabsMkr.
		for (iv = 0, ivLim = m_visfm.Size(); iv < ivLim; )
		{
			ivMid = (iv + ivLim) / 2;
			if (vsfmFields[m_visfm[ivMid]].m_stabsMkr < sfm.m_stabsMkr)
				iv = ivMid + 1;
			else
				ivLim = ivMid;
		}
		m_visfm.Insert(iv, isfm);
	}
	LVITEM lvi;
	::ZeroMemory(&lvi, isizeof(lvi));
	lvi.mask = LVIF_TEXT;
	StrAppBuf strb;
	StrAppBufSmall strbsT;
	StrAppBufSmall strbsMkrFmt("\\%s");
	StrAppBufSmall strbsNamFmt("(%s)");
	StrApp strDiscard(kstidImportWizFldMappingDiscard);
	int idst;
	isel = -1;
	for (iv = 0; iv < m_visfm.Size(); ++iv)
	{
		RnSfMarker & sfm = vsfmFields[m_visfm[iv]];
		strbsT = sfm.m_stabsMkr.Chars();					// Allow 8-bit or Unicode.
		strb.Format(strbsMkrFmt.Chars(), strbsT.Chars());
		lvi.pszText = const_cast<achar *>(strb.Chars());
		if (strb == strbSel)
			isel = iv;
		lvi.iItem = iv;
		ListView_InsertItem(hwnd, &lvi);
		strbsT = sfm.m_stabsNam.Chars();					// Allow 8-bit or Unicode.
		if (strbsT.Length())
			strb.Format(strbsNamFmt.Chars(), strbsT.Chars());
		else
			strb.Clear();
		ListView_SetItemText(hwnd, iv, 1, const_cast<achar *>(strb.Chars()));
		if (!m_hmflididst.Retrieve(sfm.m_flidDst, &idst))
		{
			ListView_SetItemText(hwnd, iv, 2, const_cast<achar *>(strDiscard.Chars()));
		}
		else
		{
			ListView_SetItemText(hwnd, iv, 2,
				const_cast<achar *>(vridst[idst].m_strName.Chars()));
		}
	}

	if (isel == -1)
		isel = 0;
	ListView_SetItemState(hwnd, isel, LVIS_SELECTED, LVIS_SELECTED);
	ListView_EnsureVisible(hwnd, isel, FALSE);
}


//:>********************************************************************************************
//:>	RnImportWizChrMappings methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
RnImportWizChrMappings::RnImportWizChrMappings()
	: AfWizardPage(kridImportWizChrMappingsDlg)
{
	m_pszHelpUrl = _T("Beginning_Tasks/Import_Standard_Format/Step_6_of_9_Character_Mapping.htm");
}

/*----------------------------------------------------------------------------------------------
	Initialize the dialog in response to the WM_INITDIALOG message.
	All one-time initialization should be done here (that is, all controls have been created
	and have valid hwnd's, but they need initial values.)

	@param hwndCtrl Not used by this method.
	@param lp Not used by this method.

	@return True.
----------------------------------------------------------------------------------------------*/
bool RnImportWizChrMappings::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	// Set the font for the page title.
	m_hfontLarge = AfGdi::CreateFont(14, 0, 0, 0, FW_BOLD, FALSE, FALSE, FALSE, ANSI_CHARSET,
		OUT_CHARACTER_PRECIS, CLIP_DEFAULT_PRECIS, DEFAULT_QUALITY, VARIABLE_PITCH | FF_SWISS,
		_T("MS Sans Serif"));
	if (m_hfontLarge)
		::SendMessage(::GetDlgItem(m_hwnd, kcidImportWizChrMappingsHdr), WM_SETFONT,
			(WPARAM)m_hfontLarge, false);
	StrApp str(kstidImportWizChrMappingsStep);
	::SetWindowText(::GetDlgItem(m_hwnd, kcidImportWizChrMappingsStep), str.Chars());

	// Define the three columns for the listbox.
	StrApp strBegin(kstidImportWizChrMapBeginMkr);		// "Beginning Marker"
	StrApp strEnd(kstidImportWizChrMapEndMkr);			// "Ending Marker"
	StrApp strDestiny(kstidImportWizChrMapDestin);		// "Notebook Destination"
	HWND hwnd = ::GetDlgItem(m_hwnd, kcidImportWizChrMappingsList);
	RECT rect;
	int rgnT[2] = { 0, 0 };
	::GetEffectiveClientRect(hwnd, &rect, rgnT);
	int cx = rect.right - rect.left;
	int cxLeft = cx / 4;
	int cxMiddle = (cx / 2) - cxLeft;
	int cxRight = cx - (cxLeft + cxMiddle);
	LVCOLUMN lvc;
	::ZeroMemory(&lvc, isizeof(lvc));
	lvc.mask = LVCF_WIDTH | LVCF_TEXT;
	lvc.cx = cxLeft;
	lvc.pszText = const_cast<achar *>(strBegin.Chars());
	ListView_InsertColumn(hwnd, 0, &lvc);
	lvc.mask |= LVCF_SUBITEM;
	lvc.iSubItem = 1;
	lvc.cx = cxMiddle;
	lvc.pszText = const_cast<achar *>(strEnd.Chars());
	ListView_InsertColumn(hwnd, 1, &lvc);
	lvc.iSubItem = 2;
	lvc.cx = cxRight;
	lvc.pszText = const_cast<achar *>(strDestiny.Chars());
	ListView_InsertColumn(hwnd, 2, &lvc);
	ListView_SetExtendedListViewStyle(hwnd, LVS_EX_FULLROWSELECT);

	// Disable modify and delete buttons.
	hwnd = ::GetDlgItem(m_hwnd, kctidImportWizChrMapModify);
	::EnableWindow(hwnd, false);
	hwnd = ::GetDlgItem(m_hwnd, kctidImportWizChrMapDelete);
	::EnableWindow(hwnd, false);

	// Load the resource strings needed for the list display.
	m_strBold.Load(kstidImportWizChrMapDirFmtBold);			// "Bold"
	m_strItalic.Load(kstidImportWizChrMapDirFmtItalic);		// "Italic"
	m_strWrtSysFmt.Load(kstidImportWizChrMapWrtSystemFmt);	// "Writing System: %s"
	m_strChrStyleFmt.Load(kstidImportWizChrMapStyleFmt);	// "Character Style: %s"
	m_strIgnored.Load(kstidImportWizChrMapIgnored);			// "(Ignored, erased during import)"

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}

/*----------------------------------------------------------------------------------------------
	Handle a WM_NOTIFY message, first letting the superclass method handle it if possible.

	@param ctidFrom Identifies the control sending the message.
	@param pnmh Pointer to the notification message data.
	@param lnRet Reference to a long integer return value used by some messages.

	@return True if the message is handled successfully; otherwise, false.
----------------------------------------------------------------------------------------------*/
bool RnImportWizChrMappings::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	if (SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet))
		return true;

	RnImportWizard * priw = dynamic_cast<RnImportWizard *>(m_pwiz);
	AssertPtr(priw);
	Vector<RnImportCharMapping> & vrcmp = priw->Settings()->CharMappings();
	HWND hwndList = ::GetDlgItem(m_hwnd, kcidImportWizChrMappingsList);
	StrApp strCaption(kstidImportMsgCaption);
	switch (pnmh->code)
	{
	case LVN_ITEMCHANGED:
		{
			// A character mapping was selected or deselected.
			// Enable or disable modify and delete buttons.
			int csel = ListView_GetSelectedCount(
				::GetDlgItem(m_hwnd, kcidImportWizChrMappingsList));
			::EnableWindow(::GetDlgItem(m_hwnd, kctidImportWizChrMapModify), csel > 0);
			::EnableWindow(::GetDlgItem(m_hwnd, kctidImportWizChrMapDelete), csel > 0);
			return true;
		}
	case BN_CLICKED:
		{
			if (ctidFrom == kctidImportWizChrMapAdd)
			{
				RnImportChrMappingDlgPtr qricmd;
				qricmd.Create();
				qricmd->Initialize(this);
				if (qricmd->DoModal(m_hwnd) == kctidOk)
				{
					RnImportCharMapping & rcmp = qricmd->GetCharMapping();
					if (rcmp.m_staBegin.Length() && rcmp.m_staEnd.Length())
					{
						for (int icmp = 0; icmp < vrcmp.Size(); ++icmp)
						{
							if (rcmp.m_staBegin == vrcmp[icmp].m_staBegin)
							{
								// Complain about duplication.
								StrUni strFmt(kstidImportWizChrMapAlreadyFmt);
								StrUni str;
								str.Format(strFmt.Chars(), rcmp.m_staBegin.Chars());
								::MessageBox(m_hwnd, str.Chars(), strCaption.Chars(),
									MB_OK | MB_ICONWARNING);
								return false;
							}
						}
						vrcmp.Push(rcmp);
						InitializeList();			// Rebuild the display list from scratch.
						return true;
					}
					else
					{
						// Complain about need for both begin and end markers.
						StrUni strFmt(kstidImportWizChrMapMissingFmt);
						StrUni str;
						str.Format(strFmt.Chars(), rcmp.m_staBegin.Chars(),
							rcmp.m_staEnd.Chars());
						::MessageBox(m_hwnd, str.Chars(), strCaption.Chars(),
							MB_OK | MB_ICONWARNING);
						return false;
					}
				}
			}
			else
			{
				int isel = GetFirstListViewSelection(hwndList);
				if (isel < 0)
					return false;
				Assert((uint)isel < (uint)vrcmp.Size());
				if (ctidFrom == kctidImportWizChrMapModify)
				{
					ModifyCharMapping(isel);
					ListView_EnsureVisible(hwndList, isel, FALSE);
					return true;
				}
				else if (ctidFrom == kctidImportWizChrMapDelete)
				{
					// Ask the user to confirm the deletion of the selected mapping.
					// Do NOT just use StrApp! String contains %S for ansi strings embedded in
					// Unicode fmt.
					StrUni strMsgFmt(kstidImportWizChrMapDelFmt);
					StrAnsi strBegin(vrcmp[isel].m_staBegin);
					StrAnsi strEnd(vrcmp[isel].m_staEnd);
					StrUni str;
					str.Format(strMsgFmt.Chars(), strBegin.Chars(), strEnd.Chars());
					int nT = ::MessageBox(m_hwnd, str.Chars(), strCaption.Chars(),
						MB_YESNO | MB_DEFBUTTON2 | MB_ICONQUESTION);
					if (nT == IDYES)
					{
						vrcmp.Delete(isel);
						InitializeList();			// Rebuild the display list from scratch.
					}
					return true;
				}
			}
		}
		break;
	case NM_DBLCLK:
		if (ctidFrom == kcidImportWizChrMappingsList)
		{
			NMITEMACTIVATE * pnmia = reinterpret_cast<NMITEMACTIVATE *>(pnmh);
			ModifyCharMapping(pnmia->iItem);
			return true;
		}
		break;
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Initialize the listbox with what we know.

	@return True if the page was successfully set active; otherwise false.
----------------------------------------------------------------------------------------------*/
bool RnImportWizChrMappings::OnSetActive()
{
	InitializeList();
	return SuperClass::OnSetActive();
}

/*----------------------------------------------------------------------------------------------
	Modify the mapping for a character format marker.

	@param isel Index of the character format marker in the list.
----------------------------------------------------------------------------------------------*/
void RnImportWizChrMappings::ModifyCharMapping(int isel)
{
	RnImportWizard * priw = dynamic_cast<RnImportWizard *>(m_pwiz);
	AssertPtr(priw);
	Vector<RnImportCharMapping> & vrcmp = priw->Settings()->CharMappings();
	RnImportChrMappingDlgPtr qricmd;
	qricmd.Create();
	qricmd->Initialize(this, vrcmp.Begin() + isel);
	if (qricmd->DoModal(m_hwnd) == kctidOk)
	{
		StrApp strCaption(kstidImportMsgCaption);
		RnImportCharMapping & rcmp = qricmd->GetCharMapping();
		if (rcmp.m_staBegin.Length() && rcmp.m_staEnd.Length())
		{
			for (int icmp = 0; icmp < vrcmp.Size(); ++icmp)
			{
				if (icmp == isel)
					continue;
				if (rcmp.m_staBegin == vrcmp[icmp].m_staBegin)
				{
					// Complain about duplication.
					StrUni strFmt(kstidImportWizChrMapAlreadyFmt);
					StrUni str;
					str.Format(strFmt.Chars(), rcmp.m_staBegin.Chars());
					::MessageBox(m_hwnd, str.Chars(), strCaption.Chars(),
						MB_OK | MB_ICONWARNING);
					return;
				}
			}
			RnImportCharMapping & rcmpSel = vrcmp[isel];
			rcmpSel.m_staBegin = rcmp.m_staBegin;
			rcmpSel.m_staEnd = rcmp.m_staEnd;
			rcmpSel.m_cmt = rcmp.m_cmt;
			rcmpSel.m_strOldWritingSystem = rcmp.m_strOldWritingSystem;
			rcmpSel.m_stuCharStyle = rcmp.m_stuCharStyle;
			rcmpSel.m_tptDirect = rcmp.m_tptDirect;
			InitializeList();			// Rebuild the display list from scratch.
			return;
		}
		else
		{
			// Complain about need for both begin and end markers.
			StrUni strFmt(kstidImportWizChrMapMissingFmt);
			StrUni str;
			str.Format(strFmt.Chars(), rcmp.m_staBegin.Chars(), rcmp.m_staEnd.Chars());
			::MessageBox(m_hwnd, str.Chars(), strCaption.Chars(), MB_OK | MB_ICONWARNING);
			return;
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Initialize the contents of the List control, and related member variables.
----------------------------------------------------------------------------------------------*/
void RnImportWizChrMappings::InitializeList()
{
	HWND hwnd = ::GetDlgItem(m_hwnd, kcidImportWizChrMappingsList);
	int isel = GetFirstListViewSelection(hwnd);
	StrAppBuf strbSel;
	achar rgchText[kcchMaxBufDef];
	if (isel >= 0)
	{
		::ZeroMemory(rgchText, isizeof(rgchText));
		ListView_GetItemText(hwnd, isel, 0, rgchText, kcchMaxBufDef - 1);
		strbSel = rgchText;
	}
	ListView_DeleteAllItems(hwnd);

	RnImportWizard * priw = dynamic_cast<RnImportWizard *>(m_pwiz);
	AssertPtr(priw);
	Vector<RnImportCharMapping> & vrcmp = priw->Settings()->CharMappings();

	LVITEM lvi;
	::ZeroMemory(&lvi, isizeof(lvi));
	lvi.mask = LVIF_TEXT;
	StrAppBuf strb;
	StrApp str;
	int ircmp;
	isel = -1;
	for (ircmp = 0; ircmp < vrcmp.Size(); ++ircmp)
	{
		RnImportCharMapping & rcmp = vrcmp[ircmp];
		// Review SteveMc (JohnT): what conversion to Unicode should be used?
		strb = rcmp.m_staBegin.Chars();
		lvi.pszText = const_cast<achar *>(strb.Chars());
		if (strb == strbSel)
			isel = ircmp;
		lvi.iItem = ircmp;
		ListView_InsertItem(hwnd, &lvi);
		// Review SteveMc (JohnT); what conversion?
		str.Assign(rcmp.m_staEnd.Chars(), rcmp.m_staEnd.Length());
		ListView_SetItemText(hwnd, ircmp, 1, const_cast<achar *>(str.Chars()));
		switch (rcmp.m_cmt)
		{
		case RnImportCharMapping::kcmtNone:
			strb = m_strIgnored.Chars();
			break;
		case RnImportCharMapping::kcmtCharStyle:
			str.Assign(rcmp.m_stuCharStyle.Chars(), rcmp.m_stuCharStyle.Length());
			strb.Format(m_strChrStyleFmt.Chars(), str.Chars());
			break;
		case RnImportCharMapping::kcmtDirectFormatting:
			strb = (rcmp.m_tptDirect == ktptBold) ? m_strBold.Chars() : m_strItalic.Chars();
			break;
		case RnImportCharMapping::kcmtOldWritingSystem:
			strb.Format(m_strWrtSysFmt.Chars(), rcmp.m_strOldWritingSystem.Chars());
			break;
		}
		ListView_SetItemText(hwnd, ircmp, 2, const_cast<achar *>(strb.Chars()));
	}
	if (isel != -1)
	{
		ListView_SetItemState(hwnd, isel, LVIS_SELECTED, LVIS_SELECTED);
		ListView_EnsureVisible(hwnd, isel, FALSE);
	}
}


//:>********************************************************************************************
//:>	RnImportWizCodeConverters methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
RnImportWizCodeConverters::RnImportWizCodeConverters()
	: AfWizardPage(kridImportWizEncodeConvDlg)
{
	m_pszHelpUrl = _T("Beginning_Tasks/Import_Standard_Format/Step_7_of_9_Encoding_Conversion.htm");
}

/*----------------------------------------------------------------------------------------------
	Initialize the dialog in response to the WM_INITDIALOG message.
	All one-time initialization should be done here (that is, all controls have been created
	and have valid hwnd's, but they need initial values.)

	@param hwndCtrl Not used by this method.
	@param lp Not used by this method.

	@return True.
----------------------------------------------------------------------------------------------*/
bool RnImportWizCodeConverters::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	// Set the font for the page title.
	m_hfontLarge = AfGdi::CreateFont(14, 0, 0, 0, FW_BOLD, FALSE, FALSE, FALSE, ANSI_CHARSET,
		OUT_CHARACTER_PRECIS, CLIP_DEFAULT_PRECIS, DEFAULT_QUALITY, VARIABLE_PITCH | FF_SWISS,
		_T("MS Sans Serif"));
	if (m_hfontLarge)
		::SendMessage(::GetDlgItem(m_hwnd, kcidImportWizEncodeConvHdr), WM_SETFONT,
			(WPARAM)m_hfontLarge, false);
	StrApp str(kstidImportWizEncodeConvStep);
	::SetWindowText(::GetDlgItem(m_hwnd, kcidImportWizEncodeConvStep), str.Chars());

	// Define the four columns for the listbox.
	StrApp strWrtSys(kstidImportEncConvWrtSys);		// "Writing system"
	StrApp strFont(kstidImportEncConvFont);			// "Font"
	StrApp strConverter(kstidImportEncConvConvrtr);	// "Converter"
	StrApp strDesc(kstidImportEncConvDesc);			// "Description"
	HWND hwnd = ::GetDlgItem(m_hwnd, kcidImportWizEncodeConvList);
	RECT rect;
	int rgnT[2] = { 0, 0 };
	::GetEffectiveClientRect(hwnd, &rect, rgnT);
	int cx = rect.right - rect.left;
	int cxFirst = cx / 4;
	int cxSecond = cx / 4;
	int cxThird = cx / 4;
	int cxFourth = cx - (cxFirst + cxSecond + cxThird);
	LVCOLUMN lvc;
	::ZeroMemory(&lvc, isizeof(lvc));
	lvc.mask = LVCF_WIDTH | LVCF_TEXT;
	lvc.cx = cxFirst;
	lvc.pszText = const_cast<achar *>(strWrtSys.Chars());
	ListView_InsertColumn(hwnd, 0, &lvc);
	lvc.mask |= LVCF_SUBITEM;
	lvc.iSubItem = 1;
	lvc.cx = cxSecond;
	lvc.pszText = const_cast<achar *>(strFont.Chars());
	ListView_InsertColumn(hwnd, 1, &lvc);
	lvc.iSubItem = 2;
	lvc.cx = cxThird;
	lvc.pszText = const_cast<achar *>(strConverter.Chars());
	ListView_InsertColumn(hwnd, 2, &lvc);
	lvc.iSubItem = 3;
	lvc.cx = cxFourth;
	lvc.pszText = const_cast<achar *>(strDesc.Chars());
	ListView_InsertColumn(hwnd, 3, &lvc);
	ListView_SetExtendedListViewStyle(hwnd, LVS_EX_FULLROWSELECT);

	// Disable modify and preview buttons.
	hwnd = ::GetDlgItem(m_hwnd, kctidImportWizEncodeConvModify);
	::EnableWindow(hwnd, false);
	// ENHANCE: Implement Preview.
	//hwnd = ::GetDlgItem(m_hwnd, kctidImportWizEncodeConvPreview);
	//::EnableWindow(hwnd, false);

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}

/*----------------------------------------------------------------------------------------------
	Initialize the Encoding Converters listbox with what we know.  Disable the Next button if
	any of the writing systems does not have a converter assigned to it.

	@return True if the page was successfully set active; otherwise false.
----------------------------------------------------------------------------------------------*/
bool RnImportWizCodeConverters::OnSetActive()
{
#if 99-99
	ListEncConverters();
#endif
	InitializeList();
	return SuperClass::OnSetActive();
}

/*----------------------------------------------------------------------------------------------
	Handle a WM_NOTIFY message, first letting the superclass method handle it if possible.

	@param ctidFrom Identifies the control sending the message.
	@param pnmh Pointer to the notification message data.
	@param lnRet Reference to a long integer return value used by some messages.

	@return True if the message is handled successfully; otherwise, false.
----------------------------------------------------------------------------------------------*/
bool RnImportWizCodeConverters::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	if (SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet))
		return true;

	RnImportWizard * priw = dynamic_cast<RnImportWizard *>(m_pwiz);
	AssertPtr(priw);
	Vector<WrtSysData> & vwsdActive = priw->WritingSystems();
	HWND hwndList = ::GetDlgItem(m_hwnd, kcidImportWizEncodeConvList);
	StrApp strCaption(kstidImportMsgCaption);
	switch (pnmh->code)
	{
	case LVN_ITEMCHANGED:
		{
			if (ListView_GetSelectedCount(hwndList) > 0)
			{
				// An encoding conversion was selected - enable modify and preview buttons.
				// ENHANCE: Implement Preview.
				::EnableWindow(::GetDlgItem(m_hwnd, kctidImportWizEncodeConvModify), true);
				//::EnableWindow(::GetDlgItem(m_hwnd, kctidImportWizEncodeConvPreview), true);
			}
			else
			{
				// a blank row was clicked
				// Disable modify and preview buttons.
				// ENHANCE: Implement Preview.
				::EnableWindow(::GetDlgItem(m_hwnd, kctidImportWizEncodeConvModify), false);
				//::EnableWindow(::GetDlgItem(m_hwnd, kctidImportWizEncodeConvPreview), false);
			}

			return true;
		}
	case BN_CLICKED:
		{
			int isel = GetFirstListViewSelection(hwndList);
			if (isel < 0)
				return false;
			Assert((uint)isel < (uint)vwsdActive.Size());
			if (ctidFrom == kctidImportWizEncodeConvModify)
			{
				ModifyCodeConverter(hwndList, isel, vwsdActive);
				ListView_EnsureVisible(hwndList, isel, FALSE);
				return true;
			}
			// ENHANCE: Implement Preview.
			//else if (ctidFrom == kctidImportWizEncodeConvPreview)
			//{
			//}
		}
		break;
	case NM_DBLCLK:
		if (ctidFrom == kcidImportWizEncodeConvList)
		{
			NMITEMACTIVATE * pnmia = reinterpret_cast<NMITEMACTIVATE *>(pnmh);
			ModifyCodeConverter(hwndList, pnmia->iItem, vwsdActive);
			return true;
		}
		break;
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Modify a language definition, presumably to change the encoding converter associated with
	it.
----------------------------------------------------------------------------------------------*/
void RnImportWizCodeConverters::ModifyCodeConverter(HWND hwndList, int iwsd,
	Vector<WrtSysData> & vwsdActive)
{
	// Start of using C# WritingSystemProperties dialog
	// THIS NEEDS WORK!
	RnImportWizard * priw = dynamic_cast<RnImportWizard *>(m_pwiz);
	AssertPtr(priw);
	ILgWritingSystemFactoryPtr qwsf = priw->GetWritingSystemFactory();
	IWritingSystemPtr qws;
	SmartBstr sbstr;
	qwsf->GetStrFromWs(vwsdActive[iwsd].m_ws, &sbstr);
	qwsf->get_Engine(sbstr, &qws);
	FwCoreDlgs::IWritingSystemPropertiesDialogPtr qwspd;
	qwspd.CreateInstance("FwCoreDlgs.WritingSystemPropertiesDialog");
	AfMainWnd * pafw = priw->MainWindow();
	AssertPtr(pafw);
	RnMainWnd * prnmw = dynamic_cast<RnMainWnd *>(pafw);
	AssertPtr(prnmw);
	AfDbInfo * pdbi = prnmw->GetLpInfo()->GetDbInfo();
	AssertPtr(pdbi);
	IOleDbEncapPtr qode;
	pdbi->GetDbAccess(&qode);
	IFwMetaDataCachePtr qmdc;
	pdbi->GetFwMetaDataCache(&qmdc);
	IVwOleDbDaPtr qda;
	AfLpInfo * plpi = pafw->GetLpInfo();
	AssertPtr(plpi);
	CustViewDaPtr qcvd;
	plpi->GetDataAccess(&qcvd);
	qcvd->QueryInterface(IID_IVwOleDbDa, (void**)&qda);

	// We need the style sheet!
	IVwStylesheetPtr qvss;
	AfStylesheet * pasts = pafw->GetStylesheet();
	AssertPtr(pasts);
	CheckHr(pasts->QueryInterface(IID_IVwStylesheet, (void **)&qvss));

	IHelpTopicProviderPtr qhtprov = new HelpTopicProvider(AfApp::Papp()->GetHelpBaseName());
	CheckHr(qwspd->Initialize(qode, qmdc, qda, qhtprov, qvss));
	// Clear all the smart pointers before actually running the dialog.
	qode.Clear();
	qmdc.Clear();
	qda.Clear();
	qhtprov.Clear();
	qvss.Clear();
	long nRet;
	CheckHr(qwspd->ShowDialog(qws, &nRet));
	qwspd.Clear();
	// End of using C# WritingSystemProperties dialog

	if (nRet == kctidOk)
	{
		// Brute force, but this should work.
		vwsdActive.Clear();
		priw->LoadWritingSystems();
		InitializeList();
	}
	else if (nRet == -1)
	{
		DWORD dwError = ::GetLastError();
		achar rgchMsg[MAX_PATH+1];
		DWORD cch = ::FormatMessage(FORMAT_MESSAGE_FROM_SYSTEM, NULL, dwError, 0, rgchMsg,
			MAX_PATH, NULL);
		rgchMsg[cch] = 0;
		StrApp strTitle(kstidImportMsgCaption);
		::MessageBox(m_hwnd, rgchMsg, strTitle.Chars(), MB_OK | MB_ICONWARNING);
		return;
	}
}

/*----------------------------------------------------------------------------------------------
	Enable the Next button since we're moving back.

	@return NULL (advances to the previous page).
----------------------------------------------------------------------------------------------*/
HWND RnImportWizCodeConverters::OnWizardBack()
{
	m_pwiz->EnableNextButton(true);
	return NULL;
}

/*----------------------------------------------------------------------------------------------
	Initialize the contents of the List control, and related member variables.  Disable the Next
	button if any of the writing systems does not have a converter assigned to it.
----------------------------------------------------------------------------------------------*/
void RnImportWizCodeConverters::InitializeList()
{
	HWND hwnd = ::GetDlgItem(m_hwnd, kcidImportWizEncodeConvList);
	int isel = GetFirstListViewSelection(hwnd);
	StrApp strSel;
	achar rgchText[kcchMaxBufDef];
	if (isel >= 0)
	{
		::ZeroMemory(rgchText, isizeof(rgchText));
		ListView_GetItemText(hwnd, isel, 0, rgchText, kcchMaxBufDef - 1);
		strSel = rgchText;
	}
	ListView_DeleteAllItems(hwnd);

	RnImportWizard * priw = dynamic_cast<RnImportWizard *>(m_pwiz);
	AssertPtr(priw);
	Vector<WrtSysData> & vwsdActive = priw->WritingSystems();

	LVITEM lvi;
	::ZeroMemory(&lvi, isizeof(lvi));
	lvi.mask = LVIF_TEXT;
	StrAppBuf strb;
	StrApp str;
	int iwsd;
	isel = -1;
	for (iwsd = 0; iwsd < vwsdActive.Size(); ++iwsd)
	{
		StrApp strName(vwsdActive[iwsd].m_stuName);
		lvi.pszText = const_cast<achar *>(strName.Chars());
		if (strName == strSel)
			isel = iwsd;
		lvi.iItem = iwsd;
		ListView_InsertItem(hwnd, &lvi);

		StrApp strFont(vwsdActive[iwsd].m_stuNormalFont);
		ListView_SetItemText(hwnd, iwsd, 1, const_cast<achar *>(strFont.Chars()));

		StrApp strConverter(vwsdActive[iwsd].m_stuEncodingConverter);
		if (!strConverter.Length())
			strConverter.Load(kstidImportWizUnknownEncodingConv);
		ListView_SetItemText(hwnd, iwsd, 2, const_cast<achar *>(strConverter.Chars()));

		StrApp strDesc(vwsdActive[iwsd].m_stuEncodingConvDesc);
		if (!strDesc.Length())
			strDesc.Load(kstidImportWizNoEncodingConvDesc);
		ListView_SetItemText(hwnd, iwsd, 3, const_cast<achar *>(strDesc.Chars()));
	}
}


//:>********************************************************************************************
//:>	RnImportWizSaveSet methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
RnImportWizSaveSet::RnImportWizSaveSet()
	: AfWizardPage(kridImportWizSaveSetDlg)
{
	m_fNoMerge = false;
	m_pszHelpUrl = _T("Beginning_Tasks/Import_Standard_Format/Step_8_of_9_Add_or_Replace_Entries.htm");
}

/*----------------------------------------------------------------------------------------------
	Initialize the dialog in response to the WM_INITDIALOG message.
	All one-time initialization should be done here (that is, all controls have been created
	and have valid hwnd's, but they need initial values.)

	@param hwndCtrl Not used by this method.
	@param lp Not used by this method.

	@return True.
----------------------------------------------------------------------------------------------*/
bool RnImportWizSaveSet::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	// Set the font for the page title.
	m_hfontLarge = AfGdi::CreateFont(14, 0, 0, 0, FW_BOLD, FALSE, FALSE, FALSE, ANSI_CHARSET,
		OUT_CHARACTER_PRECIS, CLIP_DEFAULT_PRECIS, DEFAULT_QUALITY, VARIABLE_PITCH | FF_SWISS,
		_T("MS Sans Serif"));
	if (m_hfontLarge)
		::SendMessage(::GetDlgItem(m_hwnd, kcidImportWizSaveSetHdr), WM_SETFONT,
			(WPARAM)m_hfontLarge, false);
	StrApp str(kstidImportWizSaveSetStep);
	::SetWindowText(::GetDlgItem(m_hwnd, kcidImportWizSaveSetStep), str.Chars());

	RnMainWnd * prnmw = dynamic_cast<RnMainWnd *>(AfApp::Papp()->GetCurMainWnd());
	RnLpInfo * prlpi = dynamic_cast<RnLpInfo *>(prnmw->GetLpInfo());
	AssertPtr(prlpi);
	StrApp strProjectName(prlpi->PrjName());

	StrApp strMsgFmt(kctidImportWizSaveSetDecision);
	StrApp strMsg;
	strMsg.Format(strMsgFmt.Chars(), strProjectName.Chars());
	::SetWindowText(::GetDlgItem(m_hwnd, kctidImportWizSaveSetFinalDecision), strMsg.Chars());
	return SuperClass::OnInitDlg(hwndCtrl, lp);
}

/*----------------------------------------------------------------------------------------------
	Initialize the radio buttons with what we know.

	@return True if the page was successfully set active; otherwise false.
----------------------------------------------------------------------------------------------*/
bool RnImportWizSaveSet::OnSetActive()
{
	RnImportWizard * priw = dynamic_cast<RnImportWizard *>(m_pwiz);
	AssertPtr(priw);
	HashMapChars<int> & hmcisfm = priw->Settings()->FieldCodesMap();
	int isfm;
	HWND hwndMerge = ::GetDlgItem(m_hwnd, kctidImportWizSaveSetMerge);
	if (!hmcisfm.Retrieve("_guid", &isfm))
	{
		::EnableWindow(hwndMerge, FALSE);
		m_fNoMerge = true;
		if (priw->GetDestination() == kidstMerge)
			priw->SetDestination(kidstAppend);
	}
	else
	{
		::EnableWindow(hwndMerge, TRUE);
		m_fNoMerge = false;
	}
	AfMainWnd * pafw = priw->MainWindow();
	if (pafw)
	{
		RnMainWnd * prnmw = dynamic_cast<RnMainWnd *>(pafw);
		AssertPtr(prnmw);
		if (!prnmw->RawRecordCount())
		{
			priw->SetDestination(kidstAppend);
			::EnableWindow(::GetDlgItem(m_hwnd, kctidImportWizSaveSetOverwrite), FALSE);
			::EnableWindow(hwndMerge, FALSE);
			m_fNoMerge = true;
		}
	}
	switch (priw->GetDestination())
	{
	case kidstAppend:
		::SendMessage(::GetDlgItem(m_hwnd, kctidImportWizSaveSetAppend), BM_SETCHECK,
			BST_CHECKED, 0);
	break;
	case kidstReplace:
		::SendMessage(::GetDlgItem(m_hwnd, kctidImportWizSaveSetOverwrite), BM_SETCHECK,
			BST_CHECKED, 0);
	break;
	case kidstMerge:
		::SendMessage(hwndMerge, BM_SETCHECK, BST_CHECKED, 0);
	break;
	default:
		Assert(false);
		break;
	}
	return SuperClass::OnSetActive();
}

/*----------------------------------------------------------------------------------------------
	Store the current setting of the destination radio button.

	@return NULL (advances to the previous page).
----------------------------------------------------------------------------------------------*/
HWND RnImportWizSaveSet::OnWizardBack()
{
	RnImportWizard * priw = dynamic_cast<RnImportWizard *>(m_pwiz);
	AssertPtr(priw);

	if (::SendMessage(::GetDlgItem(m_hwnd, kctidImportWizSaveSetAppend),
			BM_GETCHECK, 0, 0) == BST_CHECKED)
	{
		priw->SetDestination(kidstAppend);
	}
	else if (::SendMessage(::GetDlgItem(m_hwnd, kctidImportWizSaveSetOverwrite),
				 BM_GETCHECK, 0, 0) == BST_CHECKED)
	{
		priw->SetDestination(kidstReplace);
	}
	else if (!m_fNoMerge)
	{
		priw->SetDestination(kidstMerge);
	}
	return NULL;
}


/*----------------------------------------------------------------------------------------------
	When the framework calls this function, the property sheet is destroyed when the Finish
	button is pressed and OnWizardFinish returns true. The method gives the opportunity to
	handle finish work on the entire wizard.

	Returns: handle of the next page to advance to.  NULL advances to the next page.
----------------------------------------------------------------------------------------------*/
HWND RnImportWizSaveSet::OnWizardNext()
{
	RnImportWizard * priw = dynamic_cast<RnImportWizard *>(m_pwiz);
	AssertPtr(priw);

	if (::SendMessage(::GetDlgItem(m_hwnd, kctidImportWizSaveSetAppend),
			BM_GETCHECK, 0, 0) == BST_CHECKED)
	{
		priw->SetDestination(kidstAppend);
	}
	else if (::SendMessage(::GetDlgItem(m_hwnd, kctidImportWizSaveSetOverwrite),
				 BM_GETCHECK, 0, 0) == BST_CHECKED)
	{
		priw->SetDestination(kidstReplace);
	}
	else if (!m_fNoMerge)
	{
		priw->SetDestination(kidstMerge);
	}
	priw->StoreSettings();
	return NULL;
}

//:>********************************************************************************************
//:>	RnImportWizFinal methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
RnImportWizFinal::RnImportWizFinal()
	: AfWizardPage(kridImportWizFinalDlg)
{
	m_pszHelpUrl = _T("Beginning_Tasks/Import_Standard_Format/Step_9_of_9_Ready_to_Import.htm");
}

/*----------------------------------------------------------------------------------------------
	Initialize the dialog in response to the WM_INITDIALOG message.
	All one-time initialization should be done here (that is, all controls have been created
	and have valid hwnd's, but they need initial values.)

	@param hwndCtrl Not used by this method.
	@param lp Not used by this method.

	@return True.
----------------------------------------------------------------------------------------------*/
bool RnImportWizFinal::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	// Set the font for the page title.
	m_hfontLarge = AfGdi::CreateFont(14, 0, 0, 0, FW_BOLD, FALSE, FALSE, FALSE, ANSI_CHARSET,
		OUT_CHARACTER_PRECIS, CLIP_DEFAULT_PRECIS, DEFAULT_QUALITY, VARIABLE_PITCH | FF_SWISS,
		_T("MS Sans Serif"));
	if (m_hfontLarge)
		::SendMessage(::GetDlgItem(m_hwnd, kcidImportWizFinalHdr), WM_SETFONT,
			(WPARAM)m_hfontLarge, false);
	StrApp str(kstidImportWizFinalStep);
	::SetWindowText(::GetDlgItem(m_hwnd, kcidImportWizFinalStep), str.Chars());

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}

#ifdef USE_REC_TYPES
//:Ignore
//:>********************************************************************************************
//:>	RnImportRecTypeDlg methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
RnImportRecTypeDlg::RnImportRecTypeDlg()
{
	m_rid = kridImportRecTypeDlg;
	m_rt = RnSfMarker::krtNone;
	m_nLevel = 0;
}

/*----------------------------------------------------------------------------------------------
	Initialize the dialog in response to the WM_INITDIALOG message.
	All one-time initialization should be done here (that is, all controls have been created
	and have valid hwnd's, but they need initial values.)

	@param hwndCtrl Not used by this method.
	@param lp Not used by this method.

	@return True if successful.
----------------------------------------------------------------------------------------------*/
bool RnImportRecTypeDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	// Populate the combo boxes.
	HWND hwndMkr = ::GetDlgItem(m_hwnd, kctidImportFldMarker);
	Vector<RnSfMarker> & vsfmFields = m_priwrt->FieldCodes();
	int isfm;
	int isel = 0;
	StrAppBufSmall strbs;
	for (isfm = 0; isfm < vsfmFields.Size(); ++isfm)
	{
		strbs = vsfmFields[isfm].m_stabsMkr;
		::SendMessage(hwndMkr, CB_ADDSTRING, 0, (LPARAM)strbs.Chars());
		if (m_stabsMkr == vsfmFields[isfm].m_stabsMkr)
			isel = isfm;
	}
	::SendMessage(hwndMkr, CB_SETCURSEL, isel, 0);

	HWND hwndType = ::GetDlgItem(m_hwnd, kctidImportRecType);
	int iselDef = 0;
	::SendMessage(hwndType, CB_ADDSTRING, 0, (LPARAM)m_priwrt->GetEventStr().Chars());
	isel = 0;
	if (m_rt == RnSfMarker::krtEvent && m_nLevel == 1)
		iselDef = isel;
	int nLev;
	StrAppBuf strb;
	for (nLev = 1; nLev <= m_priwrt->GetMaxLevel(); ++nLev)
	{
		strb.Format(m_priwrt->GetEventLevelFmt().Chars(), nLev + 1);
		::SendMessage(hwndType, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
		++isel;
		if (m_rt == RnSfMarker::krtEvent && m_nLevel == nLev + 1)
			iselDef = isel;
	}
	::SendMessage(hwndType, CB_ADDSTRING, 0, (LPARAM)m_priwrt->GetHypoStr().Chars());
	++isel;
	if (m_rt == RnSfMarker::krtAnalysis && m_nLevel == 1)
		iselDef = isel;
	for (nLev = 1; nLev <= m_priwrt->GetMaxLevel(); ++nLev)
	{
		strb.Format(m_priwrt->GetHypoLevelFmt().Chars(), nLev + 1);
		::SendMessage(hwndType, CB_ADDSTRING, 0, (LPARAM)strb.Chars());
		++isel;
		if (m_rt == RnSfMarker::krtAnalysis && m_nLevel == nLev + 1)
			iselDef = isel;
	}
	::SendMessage(hwndType, CB_SETCURSEL, iselDef, 0);

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}

/*----------------------------------------------------------------------------------------------
	This method is called by the framework when the user chooses the OK or the Apply Now button.
	When the framework calls this method, changes made in the dialog are accepted.
	The default OnApply closes the dialog.

	@param fClose not used here

	@return True if successful.
----------------------------------------------------------------------------------------------*/
bool RnImportRecTypeDlg::OnApply(bool fClose)
{
	SaveDialogValues();
	return SuperClass::OnApply(fClose);
}

/*----------------------------------------------------------------------------------------------
	Initialize the data for the controls embedded within the dialog box.

	@param priwrt Pointer to the parent wizard page.
----------------------------------------------------------------------------------------------*/
void RnImportRecTypeDlg::Initialize(RnImportWizRecTypes * priwrt, const achar * pszMkr,
	RnSfMarker::RecordType rt, int nLevel)
{
	m_priwrt = priwrt;
	m_stabsMkr = pszMkr;
	m_rt = rt;
	m_nLevel = nLevel;
}

/*----------------------------------------------------------------------------------------------
	Retrieve the values from the dialog, storing them in the member variables.
----------------------------------------------------------------------------------------------*/
void RnImportRecTypeDlg::SaveDialogValues()
{
	HWND hwndMkr = ::GetDlgItem(m_hwnd, kctidImportFldMarker);
	int iselMkr = ::SendMessage(hwndMkr, CB_GETCURSEL, 0, 0);
	if (iselMkr == CB_ERR)
		return;
	HWND hwndType = ::GetDlgItem(m_hwnd, kctidImportRecType);
	int iselType = ::SendMessage(hwndType, CB_GETCURSEL, 0, 0);
	if (iselType == CB_ERR)
		return;

	Vector<RnSfMarker> & vsfmFields = m_priwrt->FieldCodes();
	m_stabsMkr = vsfmFields[iselMkr].m_stabsMkr;

	int nLevelMax = m_priwrt->GetMaxLevel();
	if (iselType == 0)
	{
		m_rt = RnSfMarker::krtEvent;
		m_nLevel = 1;
	}
	else if (iselType <= nLevelMax)
	{
		m_rt = RnSfMarker::krtEvent;
		m_nLevel = iselType + 1;
	}
	else if (iselType == nLevelMax + 1)
	{
		m_rt = RnSfMarker::krtAnalysis;
		m_nLevel = 1;
	}
	else
	{
		m_rt = RnSfMarker::krtAnalysis;
		m_nLevel = iselType - nLevelMax;
	}
}
//:End Ignore
#endif /*USE_REC_TYPES*/


//:>********************************************************************************************
//:>	RnImportTopicsListOptionsDlg methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
RnImportTopicsListOptionsDlg::RnImportTopicsListOptionsDlg()
{
	m_rid = kridImportTopicsListFldOptionSubDlg;
}

/*----------------------------------------------------------------------------------------------
	Initialize the dialog in response to the WM_INITDIALOG message.
	All one-time initialization should be done here (that is, all controls have been created
	and have valid hwnd's, but they need initial values.)

	@param hwndCtrl Not used by this method.
	@param lp Not used by this method.

	@return True if successful.
----------------------------------------------------------------------------------------------*/
bool RnImportTopicsListOptionsDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	HWND hwnd;
	StrApp str;
	// "Delimiters indicating multiple items on a line:" ____
	hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFDelimMultiEnb);
	if (m_clo.m_fHaveMulti)
	{
		::SendMessage(hwnd, BM_SETCHECK, BST_CHECKED, 0);
		hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFDelimMulti);
		str.Assign(m_clo.m_staDelimMulti);		// FIX ME FOR PROPER CODE CONVERSION!
		::SetWindowText(hwnd, str.Chars());
		::SendMessage(hwnd, EM_SETREADONLY, FALSE, 0);
	}
	else
	{
		::SendMessage(hwnd, BM_SETCHECK, BST_UNCHECKED, 0);
		hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFDelimMulti);
		::SendMessage(hwnd, EM_SETREADONLY, TRUE, 0);
	}
	// "Delimiters indicating subitems:" ____
	hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFDelimSubEnb);
	if (m_clo.m_fHaveSub)
	{
		::SendMessage(hwnd, BM_SETCHECK, BST_CHECKED, 0);
		hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFDelimSub);
		str.Assign(m_clo.m_staDelimSub);		// FIX ME FOR PROPER CODE CONVERSION!
		::SetWindowText(hwnd, str.Chars());
		::SendMessage(hwnd, EM_SETREADONLY, FALSE, 0);
	}
	else
	{
		::SendMessage(hwnd, BM_SETCHECK, BST_UNCHECKED, 0);
		hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFDelimSub);
		::SendMessage(hwnd, EM_SETREADONLY, TRUE, 0);
	}
	// "Only consider text between" ____ "and" ____
	hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFBetweenEnb);
	if (m_clo.m_fHaveBetween)
	{
		::SendMessage(hwnd, BM_SETCHECK, BST_CHECKED, 0);
		hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFBetweenAfter);
		str.Assign(m_clo.m_staMarkStart);		// FIX ME FOR PROPER CODE CONVERSION!
		::SetWindowText(hwnd, str.Chars());
		::SendMessage(hwnd, EM_SETREADONLY, FALSE, 0);
		hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFBetweenBefore);
		str.Assign(m_clo.m_staMarkEnd);			// FIX ME FOR PROPER CODE CONVERSION!
		::SetWindowText(hwnd, str.Chars());
		::SendMessage(hwnd, EM_SETREADONLY, FALSE, 0);
	}
	else
	{
		::SendMessage(hwnd, BM_SETCHECK, BST_UNCHECKED, 0);
		hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFBetweenAfter);
		::SendMessage(hwnd, EM_SETREADONLY, TRUE, 0);
		hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFBetweenBefore);
		::SendMessage(hwnd, EM_SETREADONLY, TRUE, 0);
	}
	// "Only consider text before" ____
	hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFOnlyBeforeEnb);
	if (m_clo.m_fHaveBefore)
	{
		::SendMessage(hwnd, BM_SETCHECK, BST_CHECKED, 0);
		hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFOnlyBefore);
		str.Assign(m_clo.m_staBefore);		// FIX ME FOR PROPER CODE CONVERSION!
		::SetWindowText(hwnd, str.Chars());
		::SendMessage(hwnd, EM_SETREADONLY, FALSE, 0);
	}
	else
	{
		::SendMessage(hwnd, BM_SETCHECK, BST_UNCHECKED, 0);
		hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFOnlyBefore);
		::SendMessage(hwnd, EM_SETREADONLY, TRUE, 0);
	}

	// "Throw away everything not already in the topics list."
	hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFDiscardNewEnb);
	if (m_clo.m_fIgnoreNewStuff)
	{
		::SendMessage(hwnd, BM_SETCHECK, BST_CHECKED, 0);
	}
	else
	{
		::SendMessage(hwnd, BM_SETCHECK, BST_UNCHECKED, 0);
	}

	// "Text changes:"
	RECT rect;
	int rgnT[2] = { 0, 0 };
	LVCOLUMN lvc;
	::ZeroMemory(&lvc, isizeof(lvc));
	LVITEM lvi;
	::ZeroMemory(&lvi, isizeof(lvi));
	int ista;
	StrApp strMatchLabel(kstidImportTLFSubstLabelOld);
	StrApp strReplaceLabel(kstidImportTLFSubstLabelNew);
	hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFSubstList);
	// Define the two columns for the "Substitutions:" listbox.
	::GetEffectiveClientRect(hwnd, &rect, rgnT);
	int cx = rect.right - rect.left;
	int cxLeft = cx / 2;
	lvc.mask = LVCF_WIDTH | LVCF_TEXT;
	lvc.cx = cxLeft;
	lvc.pszText = const_cast<achar *>(strMatchLabel.Chars());
	ListView_InsertColumn(hwnd, 0, &lvc);
	lvc.cx = cx - cxLeft;
	lvc.pszText = const_cast<achar *>(strReplaceLabel.Chars());
	lvc.mask |= LVCF_SUBITEM;
	lvc.iSubItem = 1;
	ListView_InsertColumn(hwnd, 1, &lvc);
	ListView_SetExtendedListViewStyle(hwnd, LVS_EX_FULLROWSELECT);

	lvi.mask = LVIF_TEXT;
	Assert(m_clo.m_vstaMatch.Size() == m_clo.m_vstaReplace.Size());
	for (ista = 0; ista < m_clo.m_vstaMatch.Size(); ++ista)
	{
		str.Assign(m_clo.m_vstaMatch[ista]);		// FIX ME FOR PROPER CODE CONVERSION!
		lvi.pszText = const_cast<achar *>(str.Chars());
		lvi.iItem = ista;
		ListView_InsertItem(hwnd, &lvi);
		str.Assign(m_clo.m_vstaReplace[ista]);		// FIX ME FOR PROPER CODE CONVERSION!
		ListView_SetItemText(hwnd, ista, 1, const_cast<achar *>(str.Chars()));
	}

	// "Ignore empty fields"
	hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFDiscardEmptyEnb);
	if (m_fIgnoreEmpty)
	{
		::SendMessage(hwnd, BM_SETCHECK, BST_CHECKED, 0);
		hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFEmptyDefault);
		::SendMessage(hwnd, EM_SETREADONLY, TRUE, 0);
	}
	else
	{
		::SendMessage(hwnd, BM_SETCHECK, BST_UNCHECKED, 0);
		hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFEmptyDefault);
		str.Assign(m_clo.m_staEmptyDefault);		// FIX ME FOR PROPER CODE CONVERSION!
		::SetWindowText(hwnd, str.Chars());
		::SendMessage(hwnd, EM_SETREADONLY, FALSE, 0);
	}

	// "Match topics list items against abbreviations"
	// "Match topics list items against full names"
	switch (m_clo.m_pnt)
	{
	case kpntAbbreviation:
		::SendMessage(::GetDlgItem(m_hwnd, kctidImportTLFMatchAbbr), BM_SETCHECK, BST_CHECKED,
			0);
	break;
	case kpntName:
		::SendMessage(::GetDlgItem(m_hwnd, kctidImportTLFMatchName), BM_SETCHECK, BST_CHECKED,
			0);
	break;
	}

	// Disable modify and delete buttons.
	hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFSubstListModify);
	::EnableWindow(hwnd, false);
	hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFSubstListDelete);
	::EnableWindow(hwnd, false);

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}

/*----------------------------------------------------------------------------------------------
  Handle a WM_NOTIFY message, first letting the superclass method handle it if possible.

  @param ctidFrom Identifies the control sending the message.
  @param pnmh Pointer to the notification message data.
  @param lnRet Reference to a long integer return value used by some messages.

  @return True if the message is handled successfully; otherwise, false.
----------------------------------------------------------------------------------------------*/
bool RnImportTopicsListOptionsDlg::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	if (SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet))
		return true;

	StrApp strCaption(kstidImportMsgCaption);
	switch (pnmh->code)
	{
	case LVN_ITEMCHANGED:
		{
			// A Topics List Option format was selected.
			int csel = ListView_GetSelectedCount(::GetDlgItem(m_hwnd, kctidImportTLFSubstList));
			// Enable or disable modify and delete buttons.
			::EnableWindow(::GetDlgItem(m_hwnd, kctidImportTLFSubstListModify), csel > 0);
			::EnableWindow(::GetDlgItem(m_hwnd, kctidImportTLFSubstListDelete), csel > 0);
			return true;
		}
	case BN_CLICKED:
		if (ctidFrom == kctidImportTLFSubstListAdd)
		{
			RnImportTLFNewSubstitutionDlgPtr qriclsd;
			qriclsd.Create();

			if (qriclsd->DoModal(m_hwnd) == kctidOk)
			{
				StrApp str;
				StrApp strMatch = qriclsd->GetMatch();
				if (!strMatch.Length())
					return true;
				// Check for match with existing item.
				HWND hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFSubstList);
				achar szText[kcchMaxBufDef+1];
				int citems = ListView_GetItemCount(hwnd);
				int i;
				for (i = 0; i < citems; ++i)
				{
					ListView_GetItemText(hwnd, i, 0, szText, kcchMaxBufDef);
					szText[kcchMaxBufDef] = 0;
					str.Assign(szText);
					if (str == strMatch)
					{
						// ERROR, TILT, CRASH, BURN!
						StrApp strFmt(kstidImportTLFSubstAlreadyFmt);
						str.Format(strFmt.Chars(), szText);
						::MessageBox(m_hwnd, str.Chars(), strCaption.Chars(),
							MB_OK | MB_ICONWARNING);
						return true;
					}
				}
				// Add to the list.
				LVITEM lvi;
				::ZeroMemory(&lvi, isizeof(lvi));
				lvi.mask = LVIF_TEXT;
				lvi.pszText = const_cast<achar *>(strMatch.Chars());
				lvi.iItem = citems;
				ListView_InsertItem(hwnd, &lvi);
				ListView_SetItemText(hwnd, citems, 1,
					const_cast<achar *>(qriclsd->GetReplacement()));
			}
		}
		else if (ctidFrom == kctidImportTLFSubstListModify ||
			ctidFrom == kctidImportTLFSubstListDelete)
		{
			// Find the selected item in the list.
			HWND hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFSubstList);
			int isel = GetFirstListViewSelection(hwnd);
			if (isel < 0)
				return false;
			if (ctidFrom == kctidImportTLFSubstListModify)
			{
				ModifySubstitution(isel);
				ListView_EnsureVisible(hwnd, isel, FALSE);
			}
			else
			{
				// Ask the user to confirm the deletion of the selected format.
				StrApp str(kstidImportTLFSubstListDelFmt);
				int nT = ::MessageBox(m_hwnd, str.Chars(), strCaption.Chars(),
					MB_YESNO | MB_DEFBUTTON2 | MB_ICONQUESTION);
				if (nT == IDYES)
				{
					ListView_DeleteItem(hwnd, isel);
				}
				return true;
			}
			return true;
		}
		else if (ctidFrom == kctidImportTLFDelimMultiEnb)
		{
			// Delimiters indicating multiple items on a line: ____
			m_clo.m_fHaveMulti = !m_clo.m_fHaveMulti;
			HWND hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFDelimMultiEnb);
			if (m_clo.m_fHaveMulti)
			{
				::SendMessage(hwnd, BM_SETCHECK, BST_CHECKED, 0);
				hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFDelimMulti);
				StrApp str;
				str.Assign(m_clo.m_staDelimMulti);		// FIX ME FOR PROPER CODE CONVERSION!
				::SetWindowText(hwnd, str.Chars());
				::SendMessage(hwnd, EM_SETREADONLY, FALSE, 0);
			}
			else
			{
				::SendMessage(hwnd, BM_SETCHECK, BST_UNCHECKED, 0);
				hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFDelimMulti);
				::SendMessage(hwnd, EM_SETREADONLY, TRUE, 0);
			}

		}
		else if (ctidFrom == kctidImportTLFDelimSubEnb)
		{
			// Delimiters indicating subitems: ____
			m_clo.m_fHaveSub = !m_clo.m_fHaveSub;
			HWND hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFDelimSubEnb);
			if (m_clo.m_fHaveSub)
			{
				::SendMessage(hwnd, BM_SETCHECK, BST_CHECKED, 0);
				hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFDelimSub);
				StrApp str;
				str.Assign(m_clo.m_staDelimSub);		// FIX ME FOR PROPER CODE CONVERSION!
				::SetWindowText(hwnd, str.Chars());
				::SendMessage(hwnd, EM_SETREADONLY, FALSE, 0);
			}
			else
			{
				::SendMessage(hwnd, BM_SETCHECK, BST_UNCHECKED, 0);
				hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFDelimSub);
				::SendMessage(hwnd, EM_SETREADONLY, TRUE, 0);
			}
		}
		else if (ctidFrom == kctidImportTLFBetweenEnb)
		{
			// Only consider text between ____ and ____
			m_clo.m_fHaveBetween = !m_clo.m_fHaveBetween;
			HWND hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFBetweenEnb);
			if (m_clo.m_fHaveBetween)
			{
				::SendMessage(hwnd, BM_SETCHECK, BST_CHECKED, 0);
				hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFBetweenAfter);
				StrApp str;
				str.Assign(m_clo.m_staMarkStart);		// FIX ME FOR PROPER CODE CONVERSION!
				::SetWindowText(hwnd, str.Chars());
				::SendMessage(hwnd, EM_SETREADONLY, FALSE, 0);
				hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFBetweenBefore);
				str.Assign(m_clo.m_staMarkEnd);		// FIX ME FOR PROPER CODE CONVERSION!
				::SetWindowText(hwnd, str.Chars());
				::SendMessage(hwnd, EM_SETREADONLY, FALSE, 0);
			}
			else
			{
				::SendMessage(hwnd, BM_SETCHECK, BST_UNCHECKED, 0);
				hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFBetweenAfter);
				::SendMessage(hwnd, EM_SETREADONLY, TRUE, 0);
				hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFBetweenBefore);
				::SendMessage(hwnd, EM_SETREADONLY, TRUE, 0);
			}
		}
		else if (ctidFrom == kctidImportTLFOnlyBeforeEnb)
		{
			// Only consider text before ____
			m_clo.m_fHaveBefore = !m_clo.m_fHaveBefore;
			HWND hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFOnlyBeforeEnb);
			if (m_clo.m_fHaveBefore)
			{
				::SendMessage(hwnd, BM_SETCHECK, BST_CHECKED, 0);
				hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFOnlyBefore);
				StrApp str;
				str.Assign(m_clo.m_staBefore);		// FIX ME FOR PROPER CODE CONVERSION!
				::SetWindowText(hwnd, str.Chars());
				::SendMessage(hwnd, EM_SETREADONLY, FALSE, 0);
			}
			else
			{
				::SendMessage(hwnd, BM_SETCHECK, BST_UNCHECKED, 0);
				hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFOnlyBefore);
				::SendMessage(hwnd, EM_SETREADONLY, TRUE, 0);
			}
		}
		else if (ctidFrom == kctidImportTLFDiscardNewEnb)
		{
			// Throw away everything not already in the topics list.
			m_clo.m_fIgnoreNewStuff = !m_clo.m_fIgnoreNewStuff;
			HWND hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFDiscardNewEnb);
			if (m_clo.m_fIgnoreNewStuff)
			{
				::SendMessage(hwnd, BM_SETCHECK, BST_CHECKED, 0);
			}
			else
			{
				::SendMessage(hwnd, BM_SETCHECK, BST_UNCHECKED, 0);
			}
		}
		else if (ctidFrom == kctidImportTLFDiscardEmptyEnb)
		{
			// Ignore empty fields
			HWND hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFDiscardEmptyEnb);
			m_fIgnoreEmpty = !m_fIgnoreEmpty;
			if (m_fIgnoreEmpty)
			{
				::SendMessage(hwnd, BM_SETCHECK, BST_CHECKED, 0);
				hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFEmptyDefault);
				::SendMessage(hwnd, EM_SETREADONLY, TRUE, 0);
			}
			else
			{
				::SendMessage(hwnd, BM_SETCHECK, BST_UNCHECKED, 0);
				hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFEmptyDefault);
				StrApp str;
				str.Assign(m_clo.m_staEmptyDefault);	// FIX ME FOR PROPER CODE CONVERSION!
				::SetWindowText(hwnd, str.Chars());
				::SendMessage(hwnd, EM_SETREADONLY, FALSE, 0);
			}
		}
		break;

	case NM_DBLCLK:
		if (ctidFrom == kctidImportTLFSubstList)
		{
			NMITEMACTIVATE * pnmia = reinterpret_cast<NMITEMACTIVATE *>(pnmh);
			ModifySubstitution(pnmia->iItem);
			return true;
		}
		break;
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Bring up a dialog box to modify the selected text change.

	@param isel Index into the list of text changes.
----------------------------------------------------------------------------------------------*/
void RnImportTopicsListOptionsDlg::ModifySubstitution(int isel)
{
	StrApp strCaption(kstidImportMsgCaption);
	HWND hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFSubstList);
	achar szMatch[kcchMaxBufDef+1];
	ListView_GetItemText(hwnd, isel, 0, szMatch, kcchMaxBufDef);
	szMatch[kcchMaxBufDef] = 0;
	achar szReplace[kcchMaxBufDef+1];
	ListView_GetItemText(hwnd, isel, 1, szReplace, kcchMaxBufDef);
	szReplace[kcchMaxBufDef] = 0;
	RnImportTLFNewSubstitutionDlgPtr qriclsd;
	qriclsd.Create();
	qriclsd->Initialize(szMatch, szReplace);
	if (qriclsd->DoModal(m_hwnd) == kctidOk)
	{
		StrApp strMatch = qriclsd->GetMatch();
		if (!strMatch.Length())
		{
			// Ask the user to confirm the deletion of the selected format.
			StrApp strMsgFmt(kstidImportTLFSubstDelFmt);
			StrApp strMessage;
			strMessage.Format(strMsgFmt.Chars(), szMatch, szReplace);
			int nT = ::MessageBox(m_hwnd, strMessage.Chars(), strCaption.Chars(),
				MB_YESNO | MB_DEFBUTTON2 | MB_ICONQUESTION);
			if (nT == IDYES)
			{
				ListView_DeleteItem(hwnd, isel);
			}
			return;
		}
		// Check for match with existing item.
		achar szText[kcchMaxBufDef+1];
		StrApp str;
		int citems = ListView_GetItemCount(hwnd);
		int i;
		for (i = 0; i < citems; ++i)
		{
			ListView_GetItemText(hwnd, i, 0, szText, kcchMaxBufDef);
			szText[kcchMaxBufDef] = 0;
			str = szText;
			if (str == strMatch && i != isel)
			{
				// ERROR, TILT, CRASH, BURN!
				StrApp strFmt(kstidImportTLFSubstAlreadyFmt);
				str.Format(strFmt.Chars(), szText);
				::MessageBox(m_hwnd, str.Chars(), strCaption.Chars(),
					MB_OK | MB_ICONWARNING);
				return;
			}
		}
		// Put new values in the list.
		ListView_SetItemText(hwnd, isel, 0,
			const_cast<achar *>(qriclsd->GetMatch()));
		ListView_SetItemText(hwnd, isel, 1,
			const_cast<achar *>(qriclsd->GetReplacement()));
	}
}

/*----------------------------------------------------------------------------------------------
	Retrieve the values from the dialog, storing them in the member variables.
----------------------------------------------------------------------------------------------*/
void RnImportTopicsListOptionsDlg::SaveDialogValues()
{
	HWND hwnd;
	int cch;
	Vector<achar> vch;

	// "Delimiters indicating multiple items on a line:" ____
	hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFDelimMultiEnb);
	if (::SendMessage(hwnd, BM_GETCHECK, 0, 0) == BST_CHECKED)
	{
		m_clo.m_fHaveMulti = true;
		hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFDelimMulti);
		cch = ::GetWindowTextLength(hwnd);
		vch.Resize(cch + 1);
		::GetWindowText(hwnd, vch.Begin(), vch.Size());
		vch[cch] = 0;
		m_clo.m_staDelimMulti = vch.Begin();
	}
	else
	{
		m_clo.m_fHaveMulti = false;
	}

	// "Delimiters indicating subitems:" ____
	hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFDelimSubEnb);
	if (::SendMessage(hwnd, BM_GETCHECK, 0, 0) == BST_CHECKED)
	{
		m_clo.m_fHaveSub = true;
		hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFDelimSub);
		cch = ::GetWindowTextLength(hwnd);
		vch.Resize(cch + 1);
		::GetWindowText(hwnd, vch.Begin(), vch.Size());
		vch[cch] = 0;
		m_clo.m_staDelimSub = vch.Begin();
	}
	else
	{
		m_clo.m_fHaveSub = false;
	}

	// "Only consider text between" ____ "and" ____
	hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFBetweenEnb);
	if (::SendMessage(hwnd, BM_GETCHECK, 0, 0) == BST_CHECKED)
	{
		m_clo.m_fHaveBetween = true;
		hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFBetweenAfter);
		cch = ::GetWindowTextLength(hwnd);
		vch.Resize(cch + 1);
		::GetWindowText(hwnd, vch.Begin(), vch.Size());
		vch[cch] = 0;
		m_clo.m_staMarkStart = vch.Begin();
		hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFBetweenBefore);
		cch = ::GetWindowTextLength(hwnd);
		vch.Resize(cch + 1);
		::GetWindowText(hwnd, vch.Begin(), vch.Size());
		vch[cch] = 0;
		m_clo.m_staMarkEnd = vch.Begin();
	}
	else
	{
		m_clo.m_fHaveBetween = false;
	}

	// "Only consider text before" ____
	hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFOnlyBeforeEnb);
	if (::SendMessage(hwnd, BM_GETCHECK, 0, 0) == BST_CHECKED)
	{
		m_clo.m_fHaveBefore = true;
		hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFOnlyBefore);
		cch = ::GetWindowTextLength(hwnd);
		vch.Resize(cch + 1);
		::GetWindowText(hwnd, vch.Begin(), vch.Size());
		vch[cch] = 0;
		m_clo.m_staBefore = vch.Begin();
	}
	else
	{
		m_clo.m_fHaveBefore = false;
	}

	// "Throw away everything not already in the topics list."
	hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFDiscardNewEnb);
	if (::SendMessage(hwnd, BM_GETCHECK, 0, 0) == BST_CHECKED)
		m_clo.m_fIgnoreNewStuff = true;
	else
		m_clo.m_fIgnoreNewStuff = false;

	// "Text changes:"
	achar szText[kcchMaxBufDef+1];
	StrAnsi sta;
	int i;
	m_clo.m_vstaMatch.Clear();
	m_clo.m_vstaReplace.Clear();
	hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFSubstList);
	int citems = ListView_GetItemCount(hwnd);
	for (i = 0; i < citems; ++i)
	{
		ListView_GetItemText(hwnd, i, 0, szText, kcchMaxBufDef);
		szText[kcchMaxBufDef] = 0;
		sta = szText;
		m_clo.m_vstaMatch.Push(sta);
		ListView_GetItemText(hwnd, i, 1, szText, kcchMaxBufDef);
		szText[kcchMaxBufDef] = 0;
		sta = szText;
		m_clo.m_vstaReplace.Push(sta);
	}

	// "Ignore empty fields"
	hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFDiscardEmptyEnb);
	if (::SendMessage(hwnd, BM_GETCHECK, 0, 0) == BST_CHECKED)
	{
		m_fIgnoreEmpty = true;
	}
	else
	{
		m_fIgnoreEmpty = false;
		hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFEmptyDefault);
		cch = ::GetWindowTextLength(hwnd);
		vch.Resize(cch + 1);
		::GetWindowText(hwnd, vch.Begin(), vch.Size());
		vch[cch] = 0;
		m_clo.m_staEmptyDefault = vch.Begin();
	}

	// "Match topics list items against abbreviations"
	// "Match topics list items against full names"
	hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFMatchAbbr);
	if (::SendMessage(hwnd, BM_GETCHECK, 0, 0) == BST_CHECKED)
		m_clo.m_pnt = kpntAbbreviation;
	else
		m_clo.m_pnt = kpntName;
}


//:>********************************************************************************************
//:>	RnImportTextOptionsDlg methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
RnImportTextOptionsDlg::RnImportTextOptionsDlg()
{
	m_rid = kridImportTextFldOptionSubDlg;
}

/*----------------------------------------------------------------------------------------------
	Initialize the dialog in response to the WM_INITDIALOG message.
	All one-time initialization should be done here (that is, all controls have been created
	and have valid hwnd's, but they need initial values.)

	@param hwndCtrl Not used by this method.
	@param lp Not used by this method.

	@return True if successful.
----------------------------------------------------------------------------------------------*/
bool RnImportTextOptionsDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	InitStyleList();
	HWND hwnd;
	// "Start a new paragraph ...for each line"
	hwnd = ::GetDlgItem(m_hwnd, kctidImportTFNewParaEachLineEnb);
	if (m_sfm.m_txo.m_fStartParaNewLine)
		::SendMessage(hwnd, BM_SETCHECK, BST_CHECKED, 0);
	else
		::SendMessage(hwnd, BM_SETCHECK, BST_UNCHECKED, 0);
	// "Start a new paragraph ...after any blank line"
	hwnd = ::GetDlgItem(m_hwnd, kctidImportTFNewParaAfterBlankEnb);
	if (m_sfm.m_txo.m_fStartParaBlankLine)
		::SendMessage(hwnd, BM_SETCHECK, BST_CHECKED, 0);
	else
		::SendMessage(hwnd, BM_SETCHECK, BST_UNCHECKED, 0);
	// "Start a new paragraph ...when a line starts with blank space(s)"
	hwnd = ::GetDlgItem(m_hwnd, kctidImportTFNewParaIndentedEnb);
	if (m_sfm.m_txo.m_fStartParaIndented)
		::SendMessage(hwnd, BM_SETCHECK, BST_CHECKED, 0);
	else
		::SendMessage(hwnd, BM_SETCHECK, BST_UNCHECKED, 0);
	// "Start a new paragraph ...after lines shorter than..."
	hwnd = ::GetDlgItem(m_hwnd, kctidImportTFNewParaShortLineEnb);
	if (m_sfm.m_txo.m_fStartParaShortLine)
	{
		::SendMessage(hwnd, BM_SETCHECK, BST_CHECKED, 0);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidImportTFShortLineLength), TRUE);
		StrAppBufSmall strbsFmt("%d");
		StrAppBufSmall strbs;
		strbs.Format(strbsFmt.Chars(), m_sfm.m_txo.m_cchShortLim);
		::SetDlgItemText(m_hwnd, kctidImportTFShortLineLength, strbs.Chars());
	}
	else
	{
		::SendMessage(hwnd, BM_SETCHECK, BST_UNCHECKED, 0);
		::SetDlgItemTextA(m_hwnd, kctidImportTFShortLineLength, "");
		::EnableWindow(::GetDlgItem(m_hwnd, kctidImportTFShortLineLength), FALSE);
	}
	// "Default Writing System"
	RnImportWizard * priw = m_prifmd->WizardPage()->GetImportWizard();
	AssertPtr(priw);
	int wsDef = m_sfm.m_txo.m_ws;
	if (!wsDef)
		wsDef = m_sfm.m_wsDefault;
	InitializeWrtSysList(::GetDlgItem(m_hwnd, kctidImportTFDefaultWrtSys), priw, wsDef);

	// Subclass the Add button.
	AfButtonPtr qbtn2;
	qbtn2.Create();
	qbtn2->SubclassButton(m_hwnd, kctidImportTFAddWrtSys, kbtPopMenu, NULL, 0);

	// Build the list of all available language writing systems.
	ILgWritingSystemFactoryPtr qwsf = priw->GetWritingSystemFactory();
	AssertPtr(qwsf.Ptr());
	GetAllWrtSys(qwsf, m_vwsdAll);

	// "Ignore empty fields"
	hwnd = ::GetDlgItem(m_hwnd, kctidImportTFDiscardEmptyEnb);
	if (m_fIgnoreEmpty)
		::SendMessage(hwnd, BM_SETCHECK, BST_CHECKED, 0);
	else
		::SendMessage(hwnd, BM_SETCHECK, BST_UNCHECKED, 0);
	return SuperClass::OnInitDlg(hwndCtrl, lp);
}

/*----------------------------------------------------------------------------------------------
	Handle a WM_NOTIFY message, first letting the superclass method handle it if possible.

	@param ctidFrom Identifies the control sending the message.
	@param pnmh Pointer to the notification message data.
	@param lnRet Reference to a long integer return value used by some messages.

	@return True if the message is handled successfully; otherwise, false.
----------------------------------------------------------------------------------------------*/
bool RnImportTextOptionsDlg::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	if (SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet))
		return true;

	switch (pnmh->code)
	{
	case BN_CLICKED:
		switch (ctidFrom)
		{
		case kctidImportTFStyles:
			return OnStylesClicked();
		case kctidImportTFNewParaEachLineEnb:
			return OnNewParaEachLineEnbClicked();
		case kctidImportTFNewParaAfterBlankEnb:
			return OnNewParaAfterBlankEnbClicked();
		case kctidImportTFNewParaIndentedEnb:
			return OnNewParaIndentedEnbClicked();
		case kctidImportTFNewParaShortLineEnb:
			return OnNewParaShortLineEnbClicked();
		case kctidImportTFDiscardEmptyEnb:
			return OnDiscardEmptyEnbClicked();
		case kctidImportTFAddWrtSys:
			{
				RnImportWizard * priw = m_prifmd->WizardPage()->GetImportWizard();
				AssertPtr(priw);
				int nRet = OnAddWrtSysClicked(m_hwnd, ctidFrom, priw, m_vwsdAll);
				if (nRet == ADDWS_ADDED)
				{
					// Reload the writing system combo box.
					Vector<WrtSysData> & vwsdActive = priw->WritingSystems();
					int wsDef = m_sfm.m_txo.m_ws;
					if (!wsDef)
						wsDef = m_sfm.m_wsDefault;
					InitializeWrtSysList(::GetDlgItem(m_hwnd, kctidImportTFDefaultWrtSys),
						priw, wsDef, vwsdActive.Top()->m_stuName.Chars());
				}
				return nRet;
			}
		}
		break;

	default:
		break;
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Handle clicking the Styles pushbutton.  Always return true.
----------------------------------------------------------------------------------------------*/
bool RnImportTextOptionsDlg::OnStylesClicked()
{
	// Fire up the Styles Dialog, and modify the topics list by the result.
	RnImportWizard * priw = m_prifmd->WizardPage()->GetImportWizard();
	AssertPtr(priw);
//	AfMainWnd * pafw = priw->MainWindow();
	RnMainWnd * pafw = dynamic_cast<RnMainWnd *>(priw->MainWindow());
	if (!pafw)
		return false;
	// Save the old selection.
	HWND hwnd = ::GetDlgItem(m_hwnd, kctidImportTFPara);
	int isel = ::SendMessage(hwnd, CB_GETCURSEL, 0, 0);
	Vector<achar> vchSel;
	int cch = 0;
	if (isel != CB_ERR)
	{
		cch = ::SendMessage(hwnd, CB_GETLBTEXTLEN, isel, 0);
		vchSel.Resize(cch + 1);
		cch = ::SendMessage(hwnd, CB_GETLBTEXT, isel, (LPARAM)vchSel.Begin());
	}
	// Variables set by AdjustTsTextProps.
	StrUni stuStyleName(vchSel.Begin(), cch);
	bool fChanged = false;
	bool fCanDoRtl = false;		// ENHANCE (RTL): Fix this when the time comes!
	bool fOuterRtl = false;		// ENHANCE (RTL): Fix this when the time comes!

	ILgWritingSystemFactoryPtr qwsf = priw->GetWritingSystemFactory();
	AssertPtr(qwsf.Ptr());
	if (!AfStylesDlg::EditImportStyles(m_hwnd, fCanDoRtl, fOuterRtl, false, false,
		pafw->GetStylesheet(), qwsf, kstParagraph, stuStyleName, fChanged,
		AfApp::Papp()->GetAppClsid(), pafw->GetRootObj()))
	{
		return false;	// If EditImportStyles returns false, nothing changed.
	}
	// reload combobox with possibly changed list of paragraph styles
	priw->LoadStyles();
	InitStyleList();
	// Set the desired selection.
	StrApp strStyle(stuStyleName);
	isel = ::SendMessage(hwnd, CB_FINDSTRINGEXACT, (WPARAM)-1,
		(LPARAM)strStyle.Chars());
	if (isel != CB_ERR)
	{
		::SendMessage(hwnd, CB_SETCURSEL, isel, 0);
	}
	else
	{
		if (vchSel.Size())
			isel = ::SendMessage(hwnd, CB_FINDSTRINGEXACT, (WPARAM)-1,
				(LPARAM)vchSel.Begin());
		if (isel != CB_ERR)
		{
			::SendMessage(hwnd, CB_SETCURSEL, isel, 0);
		}
		else
		{
			// If all else fails, fall back to the "normal" style.
			StrApp strStyle(g_pszwStyleNormal);
			isel = ::SendMessage(hwnd, CB_FINDSTRINGEXACT, (WPARAM)-1,
				(LPARAM)strStyle.Chars());
			if (isel != CB_ERR)
				::SendMessage(hwnd, CB_SETCURSEL, isel, 0);
		}
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Handle a popup menu command for adding a language writing system.

	@param pcmd Pointer to the command information.

	@return True.
----------------------------------------------------------------------------------------------*/
bool RnImportTextOptionsDlg::CmdAddWrtSys(Cmd * pcmd)
{
	// We don't need to do anything here, since ::TrackPopupMenu() is set to return the code.
	return true;
}

/*----------------------------------------------------------------------------------------------
	Handle clicking the "New Paragraph for Each Line" checkbox.  Always return true.
----------------------------------------------------------------------------------------------*/
bool RnImportTextOptionsDlg::OnNewParaEachLineEnbClicked()
{
	// "Start a new paragraph ...for each line"
	HWND hwnd = ::GetDlgItem(m_hwnd, kctidImportTFNewParaEachLineEnb);
	m_sfm.m_txo.m_fStartParaNewLine = !m_sfm.m_txo.m_fStartParaNewLine;
	if (m_sfm.m_txo.m_fStartParaNewLine)
		::SendMessage(hwnd, BM_SETCHECK, BST_CHECKED, 0);
	else
		::SendMessage(hwnd, BM_SETCHECK, BST_UNCHECKED, 0);
	return true;
}

/*----------------------------------------------------------------------------------------------
	Handle clicking the "New Paragraph for Each Line" checkbox.  Always return true.
----------------------------------------------------------------------------------------------*/
bool RnImportTextOptionsDlg::OnNewParaAfterBlankEnbClicked()
{
	// "Start a new paragraph ...after any blank line"
	HWND hwnd = ::GetDlgItem(m_hwnd, kctidImportTFNewParaAfterBlankEnb);
	m_sfm.m_txo.m_fStartParaBlankLine = !m_sfm.m_txo.m_fStartParaBlankLine;
	if (m_sfm.m_txo.m_fStartParaBlankLine)
		::SendMessage(hwnd, BM_SETCHECK, BST_CHECKED, 0);
	else
		::SendMessage(hwnd, BM_SETCHECK, BST_UNCHECKED, 0);
	return true;
}

/*----------------------------------------------------------------------------------------------
	Handle clicking the "New Paragraph for indented lines" checkbox.  Always return true.
----------------------------------------------------------------------------------------------*/
bool RnImportTextOptionsDlg::OnNewParaIndentedEnbClicked()
{
	// "Start a new paragraph ...when a line starts with blank space(s)"
	HWND hwnd = ::GetDlgItem(m_hwnd, kctidImportTFNewParaIndentedEnb);
	m_sfm.m_txo.m_fStartParaIndented = !m_sfm.m_txo.m_fStartParaIndented;
	if (m_sfm.m_txo.m_fStartParaIndented)
		::SendMessage(hwnd, BM_SETCHECK, BST_CHECKED, 0);
	else
		::SendMessage(hwnd, BM_SETCHECK, BST_UNCHECKED, 0);
	return true;
}

/*----------------------------------------------------------------------------------------------
	Handle clicking the "New Paragraph after short lines" checkbox.  Always return true.
----------------------------------------------------------------------------------------------*/
bool RnImportTextOptionsDlg::OnNewParaShortLineEnbClicked()
{
	// "Start a new paragraph ...after lines shorter than..."
	HWND hwnd = ::GetDlgItem(m_hwnd, kctidImportTFNewParaShortLineEnb);
	m_sfm.m_txo.m_fStartParaShortLine = !m_sfm.m_txo.m_fStartParaShortLine;
	if (m_sfm.m_txo.m_fStartParaShortLine)
	{
		::SendMessage(hwnd, BM_SETCHECK, BST_CHECKED, 0);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidImportTFShortLineLength), TRUE);
		StrAppBufSmall strbsFmt("%d");
		StrAppBufSmall strbs;
		strbs.Format(strbsFmt.Chars(), m_sfm.m_txo.m_cchShortLim);
		::SetDlgItemText(m_hwnd, kctidImportTFShortLineLength, strbs.Chars());
	}
	else
	{
		::SendMessage(hwnd, BM_SETCHECK, BST_UNCHECKED, 0);
		::SetDlgItemTextA(m_hwnd, kctidImportTFShortLineLength, "");
		::EnableWindow(::GetDlgItem(m_hwnd, kctidImportTFShortLineLength), FALSE);
	}
	return true;
}

/*----------------------------------------------------------------------------------------------
	Handle clicking the "Ignore empty entries" checkbox.  Always return true.
----------------------------------------------------------------------------------------------*/
bool RnImportTextOptionsDlg::OnDiscardEmptyEnbClicked()
{
	// Ignore empty fields
	HWND hwnd = ::GetDlgItem(m_hwnd, kctidImportTFDiscardEmptyEnb);
	m_fIgnoreEmpty = !m_fIgnoreEmpty;
	if (m_fIgnoreEmpty)
		::SendMessage(hwnd, BM_SETCHECK, BST_CHECKED, 0);
	else
		::SendMessage(hwnd, BM_SETCHECK, BST_UNCHECKED, 0);
	return true;
}

/*----------------------------------------------------------------------------------------------
	Initialize the paragraph style combo box.
----------------------------------------------------------------------------------------------*/
void RnImportTextOptionsDlg::InitStyleList()
{
	RnImportWizard * priw = m_prifmd->WizardPage()->GetImportWizard();
	AssertPtr(priw);
	StrUni & stuDef = priw->DefaultParaStyle();
	Vector<StrUni> & vstuStyles = priw->ParaStyleNames();
	HWND hwnd = ::GetDlgItem(m_hwnd, kctidImportTFPara);
	::SendMessage(hwnd, CB_RESETCONTENT, 0, 0);

	int iDef = -1;
	int iDef2 = 0;
	int istr;
	StrApp str;
	for (istr = 0; istr < vstuStyles.Size(); ++istr)
	{
		str = vstuStyles[istr];
		::SendMessage(hwnd, CB_ADDSTRING, 0, (LPARAM)str.Chars());
		if (m_sfm.m_txo.m_stuStyle == vstuStyles[istr])
			iDef = istr;
		else if (stuDef == vstuStyles[istr])
			iDef2 = istr;
	}
	if (iDef != -1)
		::SendMessage(hwnd, CB_SETCURSEL, iDef, 0);
	else
		::SendMessage(hwnd, CB_SETCURSEL, iDef2, 0);
}

/*----------------------------------------------------------------------------------------------
	Retrieve the values from the dialog, storing them in the member variables.
----------------------------------------------------------------------------------------------*/
void RnImportTextOptionsDlg::SaveDialogValues()
{
	// "Paragraph Style"
	achar rgchBuf[MAX_PATH];
	int cch = ::GetDlgItemText(m_hwnd, kctidImportTFPara, rgchBuf, MAX_PATH);
	rgchBuf[cch] = 0;
	m_sfm.m_txo.m_stuStyle = rgchBuf;
	// "Start a new paragraph ...for each line"
	m_sfm.m_txo.m_fStartParaNewLine = ::SendMessage(::GetDlgItem(m_hwnd,
		kctidImportTFNewParaEachLineEnb), BM_GETCHECK, 0, 0) == BST_CHECKED;
	// "Start a new paragraph ...after any blank line"
	m_sfm.m_txo.m_fStartParaBlankLine = ::SendMessage(::GetDlgItem(m_hwnd,
		kctidImportTFNewParaAfterBlankEnb), BM_GETCHECK, 0, 0) == BST_CHECKED;
	// "Start a new paragraph ...when a line starts with blank space(s)"
	m_sfm.m_txo.m_fStartParaIndented = ::SendMessage(::GetDlgItem(m_hwnd,
		kctidImportTFNewParaIndentedEnb),BM_GETCHECK, 0, 0) == BST_CHECKED;
	// "Start a new paragraph ...after lines shorter than..."
	m_sfm.m_txo.m_fStartParaShortLine = ::SendMessage(::GetDlgItem(m_hwnd,
		kctidImportTFNewParaShortLineEnb), BM_GETCHECK, 0, 0) == BST_CHECKED;
	if (m_sfm.m_txo.m_fStartParaShortLine)
	{
		cch = ::GetDlgItemText(m_hwnd, kctidImportTFShortLineLength, rgchBuf, MAX_PATH);
		rgchBuf[cch] = 0;
		StrAnsiBuf stab(rgchBuf, cch);
		m_sfm.m_txo.m_cchShortLim = strtol(stab.Chars(), NULL, 10);
	}
	// "Default Old writing system:"
	int iwsd = ::SendMessage(::GetDlgItem(m_hwnd, kctidImportTFDefaultWrtSys), CB_GETCURSEL,
		0, 0);
	if (iwsd != CB_ERR)
	{
		Vector<WrtSysData> & vwsd =
			m_prifmd->WizardPage()->GetImportWizard()->WritingSystems();
		m_sfm.m_txo.m_ws = vwsd[iwsd].m_ws;
	}
	// "Ignore empty fields"
	m_fIgnoreEmpty = ::SendMessage(::GetDlgItem(m_hwnd, kctidImportTFDiscardEmptyEnb),
		BM_GETCHECK, 0, 0) == BST_CHECKED;
}


//:>********************************************************************************************
//:>	RnImportDateOptionsDlg methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
RnImportDateOptionsDlg::RnImportDateOptionsDlg()
{
	m_rid = kridImportDateFldOptionSubDlg;
}

/*----------------------------------------------------------------------------------------------
	Initialize the dialog in response to the WM_INITDIALOG message.
	All one-time initialization should be done here (that is, all controls have been created
	and have valid hwnd's, but they need initial values.)

	@param hwndCtrl Not used by this method.
	@param lp Not used by this method.

	@return True if successful.
----------------------------------------------------------------------------------------------*/
bool RnImportDateOptionsDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	// Get the locale for the analysis writing system.
	AfMainWnd * pafw = m_prifmd->WizardPage()->GetImportWizard()->MainWindow();
	AssertPtr(pafw);
	AfLpInfo * plpi = pafw->GetLpInfo();
	AssertPtr(plpi);
	ILgWritingSystemFactoryPtr qwsf =
		m_prifmd->WizardPage()->GetImportWizard()->GetWritingSystemFactory();
	AssertPtr(qwsf.Ptr());
	IWritingSystemPtr qwseng;
	HRESULT hr;
	IgnoreHr(hr = qwsf->get_EngineOrNull(plpi->AnalWs(), &qwseng));
	int nLocale;
	if (SUCCEEDED(hr) && qwseng.Ptr())
		IgnoreHr(hr = qwseng->get_Locale(&nLocale));
	if (FAILED(hr) || !qwseng.Ptr() || !::IsValidLocale(m_lcid, LCID_INSTALLED))
		m_lcid = ::GetUserDefaultLCID();
	else
		m_lcid = nLocale;

	// "Date formats:"
	StrApp strFormatLabel(kstidImportDFFormatLabel);
	StrApp strLabelFmt(kstidImportDFLongDateFmt);
	HWND hwnd = ::GetDlgItem(m_hwnd, kctidImportDFFormatList);
	// Define the two columns for the "Date formats:" listbox.
	RECT rect;
	int rgnT[2] = { 0, 0 };
	::GetEffectiveClientRect(hwnd, &rect, rgnT);
	int cx = rect.right - rect.left;
	int cxLeft = cx / 2;
	LVCOLUMN lvc;
	::ZeroMemory(&lvc, isizeof(lvc));
	lvc.mask = LVCF_WIDTH | LVCF_TEXT;
	lvc.cx = cxLeft;
	lvc.pszText = const_cast<achar *>(strFormatLabel.Chars());
	ListView_InsertColumn(hwnd, 0, &lvc);
	// "March 2, 2001"
	SYSTEMTIME systim;
	systim.wYear = 2001;
	systim.wMonth = 3;
	systim.wDay = 2;
	achar rgch[128];
	int cch = ::GetDateFormat(m_lcid, 0, &systim, strLabelFmt.Chars(), rgch, 128);
	if (!cch)
	{
		// This format should always work for valid locales.
		strLabelFmt.Assign("MMMM d, yyyy");
		cch = ::GetDateFormat(m_lcid, 0, &systim, strLabelFmt.Chars(), rgch, 128);
	}
	rgch[cch] = '\0';
	lvc.pszText = rgch;
	lvc.cx = cx - cxLeft;
	lvc.mask |= LVCF_SUBITEM;
	lvc.iSubItem = 1;
	ListView_InsertColumn(hwnd, 1, &lvc);
	ListView_SetExtendedListViewStyle(hwnd, LVS_EX_FULLROWSELECT);
	if (!m_sfm.m_dto.m_vstaFmt.Size())
	{
		// Try to figure out possible date format(s) from the data.
		RnImportWizard * priw = m_prifmd->WizardPage()->GetImportWizard();
		AssertPtr(priw);
		priw->DeriveDateFormat(m_sfm);
		if (!m_sfm.m_dto.m_vstaFmt.Size())
		{
			// Can't figure anything out, so start by using the system user short format.
			cch = ::GetLocaleInfo(LOCALE_USER_DEFAULT, LOCALE_SSHORTDATE, rgch, 128);
			if (cch > 0)
			{
				StrAnsi sta(rgch, cch);
				m_sfm.m_dto.m_vstaFmt.Push(sta);
			}
		}
	}
	InitFormatList();

	// Disable modify and delete buttons.
	hwnd = ::GetDlgItem(m_hwnd, kctidImportDFFormatModify);
	::EnableWindow(hwnd, false);
	hwnd = ::GetDlgItem(m_hwnd, kctidImportDFFormatDelete);
	::EnableWindow(hwnd, false);

	// "Ignore empty fields"
	hwnd = ::GetDlgItem(m_hwnd, kctidImportDFDiscardEmptyEnb);
	if (m_sfm.m_fIgnoreEmpty)
		::SendMessage(hwnd, BM_SETCHECK, BST_CHECKED, 0);
	else
		::SendMessage(hwnd, BM_SETCHECK, BST_UNCHECKED, 0);
	return SuperClass::OnInitDlg(hwndCtrl, lp);
}

/*----------------------------------------------------------------------------------------------
	Handle a WM_NOTIFY message, first letting the superclass method handle it if possible.

	@param ctidFrom Identifies the control sending the message.
	@param pnmh Pointer to the notification message data.
	@param lnRet Reference to a long integer return value used by some messages.

	@return True if the message is handled successfully; otherwise, false.
----------------------------------------------------------------------------------------------*/
bool RnImportDateOptionsDlg::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	if (SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet))
		return true;

	StrApp strCaption(kstidImportMsgCaption);
	switch (pnmh->code)
	{
	case LVN_ITEMCHANGED:
		{
			// A Date format was selected or deselected.
			// Enable or disable modify and delete buttons.
			int csel = ListView_GetSelectedCount(::GetDlgItem(m_hwnd, kctidImportDFFormatList));
			::EnableWindow(::GetDlgItem(m_hwnd, kctidImportDFFormatModify), csel > 0);
			::EnableWindow(::GetDlgItem(m_hwnd, kctidImportDFFormatDelete), csel > 0);
			return true;
		}
	case BN_CLICKED:
		{
		if (ctidFrom == kctidImportDFDiscardEmptyEnb)
		{
			// "Ignore empty fields"
			HWND hwnd = ::GetDlgItem(m_hwnd, kctidImportDFDiscardEmptyEnb);
			m_sfm.m_fIgnoreEmpty = !m_sfm.m_fIgnoreEmpty;
			if (m_sfm.m_fIgnoreEmpty)
				::SendMessage(hwnd, BM_SETCHECK, BST_CHECKED, 0);
			else
				::SendMessage(hwnd, BM_SETCHECK, BST_UNCHECKED, 0);
			return true;
		}
		else if (ctidFrom == kctidImportDFFormatList)
		{
			// This does not seem to get called as expected.
			// See LVN_ITEMCHANGED above
		}
		else if (ctidFrom == kctidImportDFFormatAdd)
		{
			// Create a new date format.
			RnImportDateFormatDlgPtr qridfd;
			qridfd.Create();
			qridfd->Initialize(m_lcid);
			if (qridfd->DoModal(m_hwnd) == kctidOk)
			{
				StrAnsi sta = qridfd->GetDateFormat();
				m_sfm.m_dto.m_vstaFmt.Push(sta);
				InitFormatList();
			}
		}
		else
		{
			HWND hwnd = ::GetDlgItem(m_hwnd, kctidImportDFFormatList);
			int isel = GetFirstListViewSelection(hwnd);
			if (isel < 0)
				return false;
			Assert((uint)isel < (uint)m_sfm.m_dto.m_vstaFmt.Size());
			if (ctidFrom == kctidImportDFFormatModify)
			{
				ModifyDateFormat(isel);
				ListView_EnsureVisible(hwnd, isel, FALSE);
			}
			else if (ctidFrom == kctidImportDFFormatDelete)
			{
				// Ask the user to confirm the deletion of the selected format.
				StrApp strMsgFmt(kstidImportDFFormatDelFmt);
				StrApp strDateFmt(m_sfm.m_dto.m_vstaFmt[isel]);
				StrApp str;
				str.Format(strMsgFmt.Chars(), strDateFmt.Chars());
				int nT = ::MessageBox(m_hwnd, str.Chars(), strCaption.Chars(),
					MB_YESNO | MB_DEFBUTTON2 | MB_ICONQUESTION);
				if (nT == IDYES)
				{
					m_sfm.m_dto.m_vstaFmt.Delete(isel);
					InitFormatList();			// Rebuild the display list from scratch.
				}
				return true;
			}
		}
		}
		break;

	case NM_DBLCLK:
		if (ctidFrom == kctidImportDFFormatList)
		{
			NMITEMACTIVATE * pnmia = reinterpret_cast<NMITEMACTIVATE *>(pnmh);
			ModifyDateFormat(pnmia->iItem);
			return true;
		}
		break;
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Retrieve the values from the dialog, storing them in the member variables.
----------------------------------------------------------------------------------------------*/
void RnImportDateOptionsDlg::SaveDialogValues()
{
}

/*----------------------------------------------------------------------------------------------
	Initialize the date format examples list box.
----------------------------------------------------------------------------------------------*/
void RnImportDateOptionsDlg::InitFormatList()
{
	int cch;
	achar rgch[128];
	SYSTEMTIME systim;		// "March 2, 2001"
	systim.wYear = 2001;
	systim.wMonth = 3;
	systim.wDay = 2;
	HWND hwnd = ::GetDlgItem(m_hwnd, kctidImportDFFormatList);
	LVITEM lvi;
	::ZeroMemory(&lvi, isizeof(lvi));
	lvi.mask = LVIF_TEXT;
	ListView_DeleteAllItems(hwnd);
	int i;
	StrApp str;
	for (i = 0; i < m_sfm.m_dto.m_vstaFmt.Size(); ++i)
	{
		str.Assign(m_sfm.m_dto.m_vstaFmt[i]);		// FIX ME FOR PROPER CODE CONVERSION!
		lvi.pszText = const_cast<achar *>(str.Chars());
		lvi.iItem = i;
		ListView_InsertItem(hwnd, &lvi);
		str.Assign(m_sfm.m_dto.m_vstaFmt[i]);		// FIX ME FOR PROPER CODE CONVERSION!
		cch = ::GetDateFormat(m_lcid, 0, &systim, str.Chars(), rgch, 128);
		if (cch > 0)
		{
			rgch[cch] = '\0';
			ListView_SetItemText(hwnd, i, 1, rgch);
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Modify an existing date format.

	@param isel Index into the array of date formats (m_dto.m_vstaFmt).
----------------------------------------------------------------------------------------------*/
void RnImportDateOptionsDlg::ModifyDateFormat(int isel)
{
	Assert((uint)isel < (uint)m_sfm.m_dto.m_vstaFmt.Size());

	RnImportDateFormatDlgPtr qridfd;
	qridfd.Create();
	StrApp str;
	str.Assign(m_sfm.m_dto.m_vstaFmt[isel]);		// FIX ME FOR PROPER CODE CONVERSION!
	qridfd->Initialize(m_lcid, str.Chars());
	if (qridfd->DoModal(m_hwnd) == kctidOk)
	{
		str = qridfd->GetDateFormat();
		m_sfm.m_dto.m_vstaFmt[isel].Assign(str);	// FIX ME FOR PROPER CODE CONVERSION!
		InitFormatList();
	}
}


//:>********************************************************************************************
//:>	RnImportMultiLingualOptionsDlg methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
RnImportMultiLingualOptionsDlg::RnImportMultiLingualOptionsDlg()
{
	m_rid = kridImportMultiLingualFldOptionSubDlg;
}

/*----------------------------------------------------------------------------------------------
	Initialize the dialog in response to the WM_INITDIALOG message.
	All one-time initialization should be done here (that is, all controls have been created
	and have valid hwnd's, but they need initial values.)

	@param hwndCtrl Not used by this method.
	@param lp Not used by this method.

	@return True if successful.
----------------------------------------------------------------------------------------------*/
bool RnImportMultiLingualOptionsDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	RnImportWizard * priw = m_prifmd->WizardPage()->GetImportWizard();
	AssertPtr(priw);

	// "Writing System"
	int wsDef = m_sfm.m_mlo.m_ws;
	if (!wsDef)
		wsDef = m_sfm.m_wsDefault;
	InitializeWrtSysList(::GetDlgItem(m_hwnd, kctidImportMLWrtSys), priw, wsDef);

	// Subclass the Add button.
	AfButtonPtr qbtn2;
	qbtn2.Create();
	qbtn2->SubclassButton(m_hwnd, kctidImportMLAddWrtSys, kbtPopMenu, NULL, 0);

	// Build the list of all available language writing systems.
	ILgWritingSystemFactoryPtr qwsf = priw->GetWritingSystemFactory();
	AssertPtr(qwsf.Ptr());
	GetAllWrtSys(qwsf, m_vwsdAll);

	// "Ignore empty fields"
	HWND hwnd = ::GetDlgItem(m_hwnd, kctidImportMLDiscardEmptyEnb);
	if (m_sfm.m_fIgnoreEmpty)
		::SendMessage(hwnd, BM_SETCHECK, BST_CHECKED, 0);
	else
		::SendMessage(hwnd, BM_SETCHECK, BST_UNCHECKED, 0);

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}

/*----------------------------------------------------------------------------------------------
	Handle a WM_NOTIFY message, first letting the superclass method handle it if possible.

	@param ctidFrom Identifies the control sending the message.
	@param pnmh Pointer to the notification message data.
	@param lnRet Reference to a long integer return value used by some messages.

	@return True if the message is handled successfully; otherwise, false.
----------------------------------------------------------------------------------------------*/
bool RnImportMultiLingualOptionsDlg::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	if (SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet))
		return true;

	switch (pnmh->code)
	{
	case BN_CLICKED:
		if (ctidFrom == kctidImportMLDiscardEmptyEnb)
		{
			// "Ignore empty fields"
			HWND hwnd = ::GetDlgItem(m_hwnd, kctidImportMLDiscardEmptyEnb);
			m_sfm.m_fIgnoreEmpty = !m_sfm.m_fIgnoreEmpty;
			if (m_sfm.m_fIgnoreEmpty)
				::SendMessage(hwnd, BM_SETCHECK, BST_CHECKED, 0);
			else
				::SendMessage(hwnd, BM_SETCHECK, BST_UNCHECKED, 0);
			return true;
		}
		else if (ctidFrom == kctidImportMLAddWrtSys)
		{
			RnImportWizard * priw = m_prifmd->WizardPage()->GetImportWizard();
			AssertPtr(priw);

			int nRet = OnAddWrtSysClicked(m_hwnd, ctidFrom,
				priw, m_vwsdAll);
			if (nRet == ADDWS_ADDED)
			{
				// Reload the writing system combo box, using the newly added "active"
				// writing system as the selected value.
				Vector<WrtSysData> & vwsdActive = priw->WritingSystems();
				int wsDef = m_sfm.m_mlo.m_ws;
				if (!wsDef)
					wsDef = m_sfm.m_wsDefault;
				InitializeWrtSysList(::GetDlgItem(m_hwnd, kctidImportMLWrtSys),
					priw, wsDef, vwsdActive.Top()->m_stuName.Chars());
			}
			return nRet;
		}
		break;
	}

	return false;
}

/*----------------------------------------------------------------------------------------------
	Retrieve the values from the dialog, storing them in the member variables.
----------------------------------------------------------------------------------------------*/
void RnImportMultiLingualOptionsDlg::SaveDialogValues()
{
	// "Old writing system:"
	int iwsd = ::SendMessage(::GetDlgItem(m_hwnd, kctidImportMLWrtSys), CB_GETCURSEL, 0, 0);
	if (iwsd != CB_ERR)
	{
		Vector<WrtSysData> & vwsd =
			m_prifmd->WizardPage()->GetImportWizard()->WritingSystems();
		m_sfm.m_mlo.m_ws = vwsd[iwsd].m_ws;
	}
}

/*----------------------------------------------------------------------------------------------
	Handle a popup menu command for adding a language writing system.

	@param pcmd Pointer to the command information.

	@return True.
----------------------------------------------------------------------------------------------*/
bool RnImportMultiLingualOptionsDlg::CmdAddWrtSys(Cmd * pcmd)
{
	// We don't need to do anything here, since ::TrackPopupMenu() is set to return the code.
	return true;
}



//:>********************************************************************************************
//:>	RnImportNoOptionsDlg methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
RnImportNoOptionsDlg::RnImportNoOptionsDlg()
{
	m_rid = kridImportNoFldOptionSubDlg;
}

/*----------------------------------------------------------------------------------------------
	Initialize the dialog in response to the WM_INITDIALOG message.
	All one-time initialization should be done here (that is, all controls have been created
	and have valid hwnd's, but they need initial values.)

	@param hwndCtrl Not used by this method.
	@param lp Not used by this method.

	@return True if successful.
----------------------------------------------------------------------------------------------*/
bool RnImportNoOptionsDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	// "Ignore empty fields"
	HWND hwnd = ::GetDlgItem(m_hwnd, kctidImportNoOptDiscardEmptyEnb);
	if (m_fIgnoreEmpty)
		::SendMessage(hwnd, BM_SETCHECK, BST_CHECKED, 0);
	else
		::SendMessage(hwnd, BM_SETCHECK, BST_UNCHECKED, 0);
	return SuperClass::OnInitDlg(hwndCtrl, lp);
}

/*----------------------------------------------------------------------------------------------
	Handle a WM_NOTIFY message, first letting the superclass method handle it if possible.

	@param ctidFrom Identifies the control sending the message.
	@param pnmh Pointer to the notification message data.
	@param lnRet Reference to a long integer return value used by some messages.

	@return True if the message is handled successfully; otherwise, false.
----------------------------------------------------------------------------------------------*/
bool RnImportNoOptionsDlg::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	if (SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet))
		return true;
	switch (pnmh->code)
	{
	case BN_CLICKED:
		if (ctidFrom == kctidImportNoOptDiscardEmptyEnb)
		{
			// Ignore empty fields
			HWND hwnd = ::GetDlgItem(m_hwnd, kctidImportNoOptDiscardEmptyEnb);
			m_fIgnoreEmpty = !m_fIgnoreEmpty;
			if (m_fIgnoreEmpty)
				::SendMessage(hwnd, BM_SETCHECK, BST_CHECKED, 0);
			else
				::SendMessage(hwnd, BM_SETCHECK, BST_UNCHECKED, 0);
		}
		break;
	}
	return false;
}


/*----------------------------------------------------------------------------------------------
	Retrieve the values from the dialog, storing them in the member variables.
----------------------------------------------------------------------------------------------*/
void RnImportNoOptionsDlg::SaveDialogValues()
{
	// "Ignore empty fields"
	m_fIgnoreEmpty = ::SendMessage(::GetDlgItem(m_hwnd, kctidImportNoOptDiscardEmptyEnb),
		BM_GETCHECK, 0, 0) == BST_CHECKED;
}


/*----------------------------------------------------------------------------------------------
	Set the string displayed in the embedded subdialog.

	@param stid Resource id of the desired string.
----------------------------------------------------------------------------------------------*/
void RnImportNoOptionsDlg::SetString(int stid)
{
	// Get the string, either from the cache or the resources.
	StrApp str;
	if (!m_hmstidstr.Retrieve(stid, &str))
	{
		str.Load(stid);
		m_hmstidstr.Insert(stid, str);
	}
	::SetWindowText(::GetDlgItem(m_hwnd, kcidImportNoOptionsString), str.Chars());
}


//:>********************************************************************************************
//:>	RnImportFldMappingDlg methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
RnImportFldMappingDlg::RnImportFldMappingDlg()
{
	m_rid = kridImportFldMappingDlg;
	m_pszHelpUrl =
		_T("Beginning_Tasks/Import_Standard_Format/Import_Content_Mapping_Settings.htm");
}

/*----------------------------------------------------------------------------------------------
	Initialize the dialog in response to the WM_INITDIALOG message.
	All one-time initialization should be done here (that is, all controls have been created
	and have valid hwnd's, but they need initial values.)

	@param hwndCtrl Not used by this method.
	@param lp Not used by this method.

	@return True if successful.
----------------------------------------------------------------------------------------------*/
bool RnImportFldMappingDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	RnImportWizard * priw = m_priwfm->GetImportWizard();
	AssertPtr(priw);

	// Load some needed strings.
	m_strDataLabelFmt.Load(kstidImportDataLabelFmt);
	m_strDataSummaryFmt.Load(kstidImportDataSummaryFmt);
	m_strTopicsListLabel.Load(kstidImportListRefFldOptionLabel);
	m_strTextLabel.Load(kstidImportTextFldOptionLabel);
	m_strStringLabel.Load(kstidImportStringFldOptionLabel);
	m_strTimeLabel.Load(kstidImportTimeFldOptionLabel);
	m_strLinkLabel.Load(kstidImportLinkFldOptionLabel);
	m_strGenDateLabel.Load(kstidImportGenDateFldOptionLabel);
	m_strIntegerLabel.Load(kstidImportIntegerFldOptionLabel);
	m_strDiscardLabel.Load(kstidImportDiscardedOptionLabel);

	HWND hwndFldList = ::GetDlgItem(m_hwnd, kctidImportFldContentList);
	RECT rect;
	int rgnT[2] = { 0, 0 };
	::GetEffectiveClientRect(hwndFldList, &rect, rgnT);
	LVCOLUMN lvc;
	::ZeroMemory(&lvc, isizeof(lvc));
	lvc.mask = LVCF_WIDTH;
	lvc.cx = 2 * (rect.right - rect.left);
	ListView_InsertColumn(hwndFldList, 0, &lvc);
	InitializeFldList();

	// Populate the destination combobox list.
	m_vidst.Clear();
	HWND hwndDst = ::GetDlgItem(m_hwnd, kctidImportFldDest);
	Vector<RnImportDest> & vridst = priw->Destinations();
	int idst;
	int iv;
	int ivMid;
	int ivLim;
	for (idst = 0; idst < vridst.Size(); ++idst)
	{
		RnImportDest & ridst = vridst[idst];
		// Insert sorted by m_strName.
		for (iv = 0, ivLim = m_vidst.Size(); iv < ivLim; )
		{
			ivMid = (iv + ivLim) / 2;
			if (vridst[m_vidst[ivMid]].m_strName < ridst.m_strName)
				iv = ivMid + 1;
			else
				ivLim = ivMid;
		}
		m_vidst.Insert(iv, idst);
	}
	int ivSel = m_vidst.Size();
	for (iv = 0; iv < m_vidst.Size(); ++iv)
	{
		idst = m_vidst[iv];
		::SendMessage(hwndDst, CB_ADDSTRING, 0, (LPARAM)vridst[idst].m_strName.Chars());
		if (idst == m_idst)
			ivSel = iv;
	}
	// Finally, the (Discard) entry.
	m_vidst.Push(vridst.Size());
	StrApp strDiscard(kstidImportWizFldMappingDiscard);
	::SendMessage(hwndDst, CB_ADDSTRING, 0, (LPARAM)strDiscard.Chars());
	::SendMessage(hwndDst, CB_SETCURSEL, ivSel, 0);

	// Create the subdialogs.
	HWND hwndLabel = ::GetDlgItem(m_hwnd, kcidImportFldOptionLabel);
	const int kdxpOptionsOffset = 1;
	const int kdypOptionsOffset = 15;
	Rect rc;
	::GetWindowRect(hwndLabel, &rc);
	::MapWindowPoints(NULL, m_hwnd, (POINT *)&rc, 2);

	m_qrinod.Create();
	m_qrinod->Initialize(this, m_sfm.m_fIgnoreEmpty);
	m_qrinod->DoModeless(m_hwnd);
	::SetWindowPos(m_qrinod->Hwnd(), hwndLabel,
		rc.left + kdxpOptionsOffset, rc.top + kdypOptionsOffset,
		0, 0, SWP_NOSIZE | SWP_NOACTIVATE);
	::ShowWindow(m_qrinod->Hwnd(), SW_HIDE);

	m_qridtod.Create();
	m_qridtod->Initialize(this, m_sfm, m_priwfm->GetImportWizard()->StoredData());
	m_qridtod->DoModeless(m_hwnd);
	::SetWindowPos(m_qridtod->Hwnd(), hwndLabel,
		rc.left + kdxpOptionsOffset, rc.top + kdypOptionsOffset,
		0, 0, SWP_NOSIZE | SWP_NOACTIVATE);
	::ShowWindow(m_qridtod->Hwnd(), SW_HIDE);

	m_qrimlod.Create();
	m_qrimlod->Initialize(this, m_sfm, m_priwfm->GetImportWizard()->StoredData());
	m_qrimlod->DoModeless(m_hwnd);
	::SetWindowPos(m_qrimlod->Hwnd(), hwndLabel,
		rc.left + kdxpOptionsOffset, rc.top + kdypOptionsOffset,
		0, 0, SWP_NOSIZE | SWP_NOACTIVATE);
	::ShowWindow(m_qrimlod->Hwnd(), SW_HIDE);

	m_qritxod.Create();
	m_qritxod->Initialize(this, m_sfm, m_sfm.m_fIgnoreEmpty);
	m_qritxod->DoModeless(m_hwnd);
	::SetWindowPos(m_qritxod->Hwnd(), hwndLabel,
		rc.left + kdxpOptionsOffset, rc.top + kdypOptionsOffset,
		0, 0, SWP_NOSIZE | SWP_NOACTIVATE);
	::ShowWindow(m_qritxod->Hwnd(), SW_HIDE);

	m_qriclod.Create();
	m_qriclod->Initialize(this, m_sfm.m_clo, m_sfm.m_fIgnoreEmpty);
	m_qriclod->DoModeless(m_hwnd);
	::SetWindowPos(m_qriclod->Hwnd(), hwndLabel,
		rc.left + kdxpOptionsOffset, rc.top + kdypOptionsOffset,
		0, 0, SWP_NOSIZE | SWP_NOACTIVATE);
	::ShowWindow(m_qriclod->Hwnd(), SW_HIDE);

	InitializeOptions();

	// Subclass the Help button.
	AfButtonPtr qbtn;
	qbtn.Create();
	qbtn->SubclassButton(m_hwnd, kctidHelp, kbtHelp, NULL, 0);

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}

/*----------------------------------------------------------------------------------------------
	This method is called by the framework when the user chooses the OK or the Apply Now button.
	When the framework calls this method, changes made in the dialog are accepted.
	The default OnApply closes the dialog.

	@param fClose not used here

	@return True if successful.
----------------------------------------------------------------------------------------------*/
bool RnImportFldMappingDlg::OnApply(bool fClose)
{
	SaveDialogValues();
	return SuperClass::OnApply(fClose);
}

/*----------------------------------------------------------------------------------------------
	Handle a WM_NOTIFY message, first letting the superclass method handle it if possible.

	@param ctidFrom Identifies the control sending the message.
	@param pnmh Pointer to the notification message data.
	@param lnRet Reference to a long integer return value used by some messages.

	@return True if the message is handled successfully; otherwise, false.
----------------------------------------------------------------------------------------------*/
bool RnImportFldMappingDlg::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	if (SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet))
		return true;

	switch (pnmh->code)
	{
	case CBN_SELCHANGE:
		if (ctidFrom == kctidImportFldDest)
		{
			InitializeOptions();
			return true;
		}
		break;
	case BN_CLICKED:
		break;
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Initialize the data for the controls embedded within the dialog box.

	@param priwfm Pointer to the parent wizard page.
----------------------------------------------------------------------------------------------*/
void RnImportFldMappingDlg::Initialize(RnImportWizFldMappings * priwfm, int isfm, int idst)
{
	m_priwfm = priwfm;
	m_isfm = isfm;
	m_idst = idst;
	m_sfm = priwfm->ContentMapping(isfm);

	// If it is not yet set, set the default writing system for this field.
	if (!m_sfm.m_wsDefault)
	{
		RnImportWizard * priw = priwfm->GetImportWizard();
		AssertPtr(priw);
		Vector<RnImportDest> & vridst = priw->Destinations();
		for (int i = 0; i < vridst.Size(); ++i)
		{
			if (vridst[i].m_flid == m_sfm.m_flidDst)
			{
				AfLpInfo * plpi = priw->MainWindow()->GetLpInfo();
				AssertPtr(plpi);
				m_sfm.m_wsDefault = plpi->ActualWs(vridst[i].m_wsDefault);
				break;
			}
		}
	}
}

/*----------------------------------------------------------------------------------------------
	Retrieve the values from the dialog, storing them in the member variables.
----------------------------------------------------------------------------------------------*/
void RnImportFldMappingDlg::SaveDialogValues()
{
	HWND hwndDst = ::GetDlgItem(m_hwnd, kctidImportFldDest);
	int iv = ::SendMessage(hwndDst, CB_GETCURSEL, 0, 0);
	if (iv != CB_ERR)
		m_idst = m_vidst[iv];
	else
		m_idst = m_vidst.Size() - 1;		// (Discard).
	if (m_qridtod)
	{
		m_qridtod->SaveDialogValues();
		m_sfm.m_dto = m_qridtod->GetOptions();
		if (m_ot == kotDate)
			m_sfm.m_fIgnoreEmpty = m_qridtod->IgnoreEmpty();
	}
	if (m_qritxod)
	{
		m_qritxod->SaveDialogValues();
		m_sfm.m_txo = m_qritxod->GetOptions();
		if (m_ot == kotText)
			m_sfm.m_fIgnoreEmpty = m_qritxod->IgnoreEmpty();
	}
	if (m_qriclod)
	{
		m_qriclod->SaveDialogValues();
		m_sfm.m_clo = m_qriclod->GetOptions();
		if (m_ot == kotTopicsList)
			m_sfm.m_fIgnoreEmpty = m_qriclod->IgnoreEmpty();
	}
	if (m_qrimlod)
	{
		m_qrimlod->SaveDialogValues();
		m_sfm.m_mlo = m_qrimlod->GetOptions();
		if (m_ot == kotMultiLingual)
			m_sfm.m_fIgnoreEmpty = m_qrimlod->IgnoreEmpty();
	}
	if (m_qrinod)
	{
		m_qrinod->SaveDialogValues();
		if (m_ot == kotNone)
			m_sfm.m_fIgnoreEmpty = m_qrinod->IgnoreEmpty();
	}
}

/*----------------------------------------------------------------------------------------------
	Initialize the contents of the Field contents list control, and related items in the dialog.
----------------------------------------------------------------------------------------------*/
void RnImportFldMappingDlg::InitializeFldList()
{
	RnImportWizard * priw = m_priwfm->GetImportWizard();
	AssertPtr(priw);
	Vector<RnSfMarker> & vsfmFields = priw->Settings()->FieldCodes();
	// Get the selected field marker.
	StrAppBuf strbMkr;
	StrAnsiBufSmall stabMkr;
	strbMkr = vsfmFields[m_isfm].m_stabsMkr.Chars();
	stabMkr = vsfmFields[m_isfm].m_stabsMkr.Chars();

	// Set the data side group box label.
	StrAppBuf strb;
	strb.Format(m_strDataLabelFmt.Chars(), strbMkr.Chars());
	::SetWindowText(::GetDlgItem(m_hwnd, kcidImportDataLabel), strb.Chars());

	// Fill the data side list box showing all the unique field contents.
	HWND hwndFldList = ::GetDlgItem(m_hwnd, kctidImportFldContentList);
	ListView_DeleteAllItems(hwndFldList);
	LVITEM lvi;
	::ZeroMemory(&lvi, isizeof(lvi));
	lvi.mask = LVIF_TEXT;
	HashMapChars<int> hmcifld;

	RnImportShoeboxData * prisd = priw->StoredData();
	int ivpsz;
	if (prisd->m_hmcLines.Retrieve(stabMkr.Chars(), &ivpsz))
	{
		char ** prgpszFld = prisd->m_vvpszLines[ivpsz].Begin();
		int cfld = prisd->m_vvpszLines[ivpsz].Size();
		int ihn;
		int ifld;
		int cfldUnique = 0;
		StrAnsiBuf stab;
		for (ifld = 0; ifld < cfld; ++ifld)
		{
			stab = prgpszFld[ifld];						// Truncates to <260 chars.
			if (hmcifld.Retrieve(stab.Chars(), &ihn))
				continue;
			hmcifld.Insert(stab.Chars(), ifld);
			strb.Assign(stab.Chars());			// allow 8-bit or Unicode.
			lvi.pszText = const_cast<achar *>(strb.Chars());
			lvi.iItem = cfldUnique;
			ListView_InsertItem(hwndFldList, &lvi);
			++cfldUnique;
		}
		// Set the data summary statement.
		strb.Format(m_strDataSummaryFmt.Chars(), strbMkr.Chars(), cfld, cfldUnique);
		::SetWindowText(::GetDlgItem(m_hwnd, kcidImportDataSummary), strb.Chars());
	}
	else
	{
		strb.Format(m_strDataSummaryFmt.Chars(), strbMkr.Chars(), 0, 0);
		::SetWindowText(::GetDlgItem(m_hwnd, kcidImportDataSummary), strb.Chars());
	}
}

/*----------------------------------------------------------------------------------------------
	Initialize the contents of the options embedded dialog box.
----------------------------------------------------------------------------------------------*/
void RnImportFldMappingDlg::InitializeOptions()
{
	RnImportWizard * priw = m_priwfm->GetImportWizard();
	AssertPtr(priw);
	Vector<RnImportDest> & vridst = priw->Destinations();

	// Set the option side group box label.
	HWND hwndDst = ::GetDlgItem(m_hwnd, kctidImportFldDest);
	int isel = ::SendMessage(hwndDst, CB_GETCURSEL, 0, 0);
	if (isel == CB_ERR)
		return;
	int iselDst = m_vidst[isel];
	RnImportDest ridstDummy;
	RnImportDest & ridst = ((uint)iselDst < (uint)vridst.Size()) ?
		vridst[iselDst] : ridstDummy;
	HWND hwndLabel = ::GetDlgItem(m_hwnd, kcidImportFldOptionLabel);
#if 2
	bool fHandled = false;
#endif

	switch (ridst.m_proptype)
	{
	case kcptNil:
#if 2
		fHandled = true;
#endif
		if (m_ot == kotTopicsList)
			::ShowWindow(m_qriclod->Hwnd(), SW_HIDE);
		else if (m_ot == kotText)
			::ShowWindow(m_qritxod->Hwnd(), SW_HIDE);
		else if (m_ot == kotDate)
			::ShowWindow(m_qridtod->Hwnd(), SW_HIDE);
		else if (m_ot == kotMultiLingual)
			::ShowWindow(m_qrimlod->Hwnd(), SW_HIDE);

		::SetWindowText(hwndLabel, m_strDiscardLabel.Chars());
		m_ot = kotNone;
		m_qrinod->SetString(kstidImportNoDiscardOptions);
		::ShowWindow(m_qrinod->Hwnd(), SW_SHOW);
		break;

	case kcptInteger:
#if 2
		fHandled = true;
#endif
		if (m_ot == kotTopicsList)
			::ShowWindow(m_qriclod->Hwnd(), SW_HIDE);
		else if (m_ot == kotText)
			::ShowWindow(m_qritxod->Hwnd(), SW_HIDE);
		else if (m_ot == kotDate)
			::ShowWindow(m_qridtod->Hwnd(), SW_HIDE);
		else if (m_ot == kotMultiLingual)
			::ShowWindow(m_qrimlod->Hwnd(), SW_HIDE);

		::SetWindowText(hwndLabel, m_strIntegerLabel.Chars());
		m_ot = kotNone;
		m_qrinod->SetString(kstidImportNoIntegerOptions);
		::ShowWindow(m_qrinod->Hwnd(), SW_SHOW);
		break;

	case kcptTime:
#if 2
		fHandled = true;
#endif
		if (m_ot == kotTopicsList)
			::ShowWindow(m_qriclod->Hwnd(), SW_HIDE);
		else if (m_ot == kotText)
			::ShowWindow(m_qritxod->Hwnd(), SW_HIDE);
		else if (m_ot == kotMultiLingual)
			::ShowWindow(m_qrimlod->Hwnd(), SW_HIDE);

		if (ridst.m_flid == kflidRnGenericRec_DateCreated ||
			ridst.m_flid == kflidRnGenericRec_DateModified)
		{
			if (m_ot == kotNone)
				::ShowWindow(m_qrinod->Hwnd(), SW_HIDE);
			::SetWindowText(hwndLabel, m_strGenDateLabel.Chars());
			m_ot = kotDate;
			::ShowWindow(m_qridtod->Hwnd(), SW_SHOW);
		}
		else
		{
			if (m_ot == kotDate)
				::ShowWindow(m_qridtod->Hwnd(), SW_HIDE);
			::SetWindowText(hwndLabel, m_strTimeLabel.Chars());
			m_ot = kotNone;
			m_qrinod->SetString(kstidImportNoTimeOptions);
			::ShowWindow(m_qrinod->Hwnd(), SW_SHOW);
		}
		break;

	case kcptGenDate:
#if 2
		fHandled = true;
#endif
		if (m_ot == kotTopicsList)
			::ShowWindow(m_qriclod->Hwnd(), SW_HIDE);
		else if (m_ot == kotText)
			::ShowWindow(m_qritxod->Hwnd(), SW_HIDE);
		else if (m_ot == kotNone)
			::ShowWindow(m_qrinod->Hwnd(), SW_HIDE);
		else if (m_ot == kotMultiLingual)
			::ShowWindow(m_qrimlod->Hwnd(), SW_HIDE);

		::SetWindowText(hwndLabel, m_strGenDateLabel.Chars());
		m_ot = kotDate;
		::ShowWindow(m_qridtod->Hwnd(), SW_SHOW);
		break;
	case kcptString:
	case kcptUnicode:
	case kcptBigString:
	case kcptBigUnicode:
		// Allow choosing a language for monolingual string as well as multilingual.
		// This parallels structured text input, which always allows choosing a language.
//#if 2
//		fHandled = true;
//#endif
//		if (m_ot == kotTopicsList)
//			::ShowWindow(m_qriclod->Hwnd(), SW_HIDE);
//		else if (m_ot == kotText)
//			::ShowWindow(m_qritxod->Hwnd(), SW_HIDE);
//		else if (m_ot == kotDate)
//			::ShowWindow(m_qridtod->Hwnd(), SW_HIDE);
//		else if (m_ot == kotMultiLingual)
//			::ShowWindow(m_qrimlod->Hwnd(), SW_HIDE);
//
//		::SetWindowText(hwndLabel, m_strStringLabel.Chars());
//		m_ot = kotNone;
//		m_qrinod->SetString(kstidImportNoStringOptions);
//		::ShowWindow(m_qrinod->Hwnd(), SW_SHOW);
//		break;
	case kcptMultiString:
	case kcptMultiBigString:
	case kcptMultiUnicode:
	case kcptMultiBigUnicode:
#if 2
		fHandled = true;
#endif
		if (m_ot == kotTopicsList)
			::ShowWindow(m_qriclod->Hwnd(), SW_HIDE);
		else if (m_ot == kotText)
			::ShowWindow(m_qritxod->Hwnd(), SW_HIDE);
		else if (m_ot == kotDate)
			::ShowWindow(m_qridtod->Hwnd(), SW_HIDE);
		else if (m_ot == kotNone)
			::ShowWindow(m_qrinod->Hwnd(), SW_HIDE);

		::SetWindowText(hwndLabel, m_strStringLabel.Chars());
		m_ot = kotMultiLingual;

		if (!m_sfm.m_wsDefault)
		{
			m_sfm.m_flidDst = ridst.m_flid;
			AfLpInfo * plpi = priw->MainWindow()->GetLpInfo();
			AssertPtr(plpi);
			m_sfm.m_wsDefault = plpi->ActualWs(ridst.m_wsDefault);
			int wsDef = m_sfm.m_mlo.m_ws;
			if (!wsDef)
				wsDef = m_sfm.m_wsDefault;
			InitializeWrtSysList(::GetDlgItem(m_qrimlod->Hwnd(), kctidImportMLWrtSys),
				priw, wsDef);
		}

		::ShowWindow(m_qrimlod->Hwnd(), SW_SHOW);
		break;
	case kcptOwningAtom:
	case kcptReferenceAtom:
	case kcptOwningCollection:
	case kcptReferenceCollection:
	case kcptOwningSequence:
	case kcptReferenceSequence:
		if (ridst.m_clidDst == kclidStText)
		{
#if 2
			fHandled = true;
#endif
			if (m_ot == kotTopicsList)
				::ShowWindow(m_qriclod->Hwnd(), SW_HIDE);
			else if (m_ot == kotNone)
				::ShowWindow(m_qrinod->Hwnd(), SW_HIDE);
			else if (m_ot == kotDate)
				::ShowWindow(m_qridtod->Hwnd(), SW_HIDE);
			else if (m_ot == kotMultiLingual)
				::ShowWindow(m_qrimlod->Hwnd(), SW_HIDE);

			::SetWindowText(hwndLabel, m_strTextLabel.Chars());
			m_ot = kotText;
			::ShowWindow(m_qritxod->Hwnd(), SW_SHOW);
		}
		else if (ridst.m_clidDst == kclidCmPossibility ||
			ridst.m_clidDst == kclidCmAnthroItem ||
			ridst.m_clidDst == kclidCmCustomItem ||
			ridst.m_clidDst == kclidCmPerson ||
			ridst.m_clidDst == kclidCmLocation)
		{
			// Handle Possibility List items.
#if 2
			fHandled = true;
#endif
			if (m_ot == kotText)
				::ShowWindow(m_qritxod->Hwnd(), SW_HIDE);
			else if (m_ot == kotDate)
				::ShowWindow(m_qridtod->Hwnd(), SW_HIDE);
			else if (m_ot == kotNone)
				::ShowWindow(m_qrinod->Hwnd(), SW_HIDE);
			else if (m_ot == kotMultiLingual)
				::ShowWindow(m_qrimlod->Hwnd(), SW_HIDE);

			::SetWindowText(hwndLabel, m_strTopicsListLabel.Chars());
			m_ot = kotTopicsList;
			// Disable multiple items line for atomic attributes.
			HWND hwndCtl = ::GetDlgItem(m_qriclod->Hwnd(), kctidImportTLFDelimMultiEnb);
			HWND hwndCtl2 = ::GetDlgItem(m_qriclod->Hwnd(), kctidImportTLFDelimMulti);
			if (ridst.m_proptype == kcptReferenceAtom)
			{
				::EnableWindow(hwndCtl, FALSE);
				::EnableWindow(hwndCtl2, FALSE);
			}
			else
			{
				::EnableWindow(hwndCtl, TRUE);
				::EnableWindow(hwndCtl2, TRUE);
			}
			// Disable hierarchical items line for flat lists.
			hwndCtl = ::GetDlgItem(m_qriclod->Hwnd(), kctidImportTLFDelimSubEnb);
			hwndCtl2 = ::GetDlgItem(m_qriclod->Hwnd(), kctidImportTLFDelimSub);
			if (IsListFlat(priw, ridst.m_hvoPssl))
			{
				::EnableWindow(hwndCtl, FALSE);
				::EnableWindow(hwndCtl2, FALSE);
			}
			else
			{
				::EnableWindow(hwndCtl, TRUE);
				::EnableWindow(hwndCtl2, TRUE);
			}
			::ShowWindow(m_qriclod->Hwnd(), SW_SHOW);
		}
		else if (ridst.m_clidDst == kclidRnGenericRec)
		{
#if 2
			fHandled = true;
#endif
			if (m_ot == kotTopicsList)
				::ShowWindow(m_qriclod->Hwnd(), SW_HIDE);
			else if (m_ot == kotText)
				::ShowWindow(m_qritxod->Hwnd(), SW_HIDE);
			else if (m_ot == kotDate)
				::ShowWindow(m_qridtod->Hwnd(), SW_HIDE);
			else if (m_ot == kotMultiLingual)
				::ShowWindow(m_qrimlod->Hwnd(), SW_HIDE);

			::SetWindowText(hwndLabel, m_strLinkLabel.Chars());
			m_ot = kotNone;
			m_qrinod->SetString(kstidImportNoLinkOptions);
			::ShowWindow(m_qrinod->Hwnd(), SW_SHOW);
		}
		else if (ridst.m_clidDst == kclidRnRoledPartic)
		{
#if 2
			fHandled = true;
#endif
			// Handle same as Possibility List items.
			// REVIEW SteveMc: is this adequate?
			if (m_ot == kotText)
				::ShowWindow(m_qritxod->Hwnd(), SW_HIDE);
			else if (m_ot == kotDate)
				::ShowWindow(m_qridtod->Hwnd(), SW_HIDE);
			else if (m_ot == kotNone)
				::ShowWindow(m_qrinod->Hwnd(), SW_HIDE);
			else if (m_ot == kotMultiLingual)
				::ShowWindow(m_qrimlod->Hwnd(), SW_HIDE);

			::SetWindowText(hwndLabel, m_strTopicsListLabel.Chars());
			m_ot = kotTopicsList;
			// Disable multiple items line for atomic attributes.
			HWND hwndCtl = ::GetDlgItem(m_qriclod->Hwnd(), kctidImportTLFDelimMultiEnb);
			HWND hwndCtl2 = ::GetDlgItem(m_qriclod->Hwnd(), kctidImportTLFDelimMulti);
			if (ridst.m_proptype == kcptReferenceAtom)
			{
				::EnableWindow(hwndCtl, FALSE);
				::EnableWindow(hwndCtl2, FALSE);
			}
			else
			{
				::EnableWindow(hwndCtl, TRUE);
				::EnableWindow(hwndCtl2, TRUE);
			}
			// Disable hierarchical items line since People is a flat list.
			hwndCtl = ::GetDlgItem(m_qriclod->Hwnd(), kctidImportTLFDelimSubEnb);
			hwndCtl2 = ::GetDlgItem(m_qriclod->Hwnd(), kctidImportTLFDelimSub);
			::EnableWindow(hwndCtl, FALSE);
			::EnableWindow(hwndCtl2, FALSE);
			::ShowWindow(m_qriclod->Hwnd(), SW_SHOW);
		}
		break;
	}
#if 2
	if (!fHandled)
	{
		switch (ridst.m_proptype)
		{
		case kcptBoolean:
			::MessageBoxA(m_hwnd, "Importing Boolean type fields is not yet supported.",
				"DEBUG", MB_OK);
			break;
		case kcptNumeric:
			::MessageBoxA(m_hwnd, "Importing Numeric type fields is not yet supported.",
				"DEBUG", MB_OK);
			break;
		case kcptFloat:
			::MessageBoxA(m_hwnd, "Importing Float type fields is not yet supported.",
				"DEBUG", MB_OK);
			break;
		case kcptGuid:
			::MessageBoxA(m_hwnd, "Importing Guid type fields is not yet supported.",
				"DEBUG", MB_OK);
			break;
		case kcptImage:
			::MessageBoxA(m_hwnd, "Importing Image type fields is not yet supported.",
				"DEBUG", MB_OK);
			break;
		case kcptBinary:
			::MessageBoxA(m_hwnd, "Importing Binary type fields is not yet supported.",
				"DEBUG", MB_OK);
			break;
		case kcptOwningAtom:
			::MessageBoxA(m_hwnd,
	"Importing OwningAtom type fields is not yet supported for the destination class.",
				"DEBUG", MB_OK);
			break;
		case kcptReferenceAtom:
			::MessageBoxA(m_hwnd,
	"Importing ReferenceAtom type fields is not yet supported for the destination class.",
				"DEBUG", MB_OK);
			break;
		case kcptOwningCollection:
			::MessageBoxA(m_hwnd,
	"Importing OwningCollection type fields is not yet supported for the destination class.",
				"DEBUG", MB_OK);
			break;
		case kcptReferenceCollection:
			::MessageBoxA(m_hwnd,
	"Importing ReferenceCollection type fields is not yet supported for the destination class.",
				"DEBUG", MB_OK);
			break;
		case kcptOwningSequence:
			::MessageBoxA(m_hwnd,
	"Importing OwningSequence type fields is not yet supported for the destination class.",
				"DEBUG", MB_OK);
			break;
		case kcptReferenceSequence:
			::MessageBoxA(m_hwnd,
	"Importing ReferenceSequence type fields is not yet supported for the destination class.",
				"DEBUG", MB_OK);
			break;
		}
		return;
	}
#endif
}

/*----------------------------------------------------------------------------------------------
	Test whether the given possibility list is flat or hierarchical.

	@param priw
	@param hvoPssl

	@return True if the indicated list is flat (or nonexistent).
----------------------------------------------------------------------------------------------*/
bool RnImportFldMappingDlg::IsListFlat(RnImportWizard * priw, HVO hvoPssl)
{
	AssertPtr(priw);
	Assert(hvoPssl);

	AfMainWnd * pafw = priw->MainWindow();
	if (!pafw)
		return true;
	AfLpInfo * plpi = pafw->GetLpInfo();
	AssertPtr(plpi);
	PossListInfoPtr qpli;
	plpi->LoadPossList(hvoPssl, pafw->UserWs(), &qpli);
	Assert(qpli);
	return qpli->GetDepth() <= 1;
}


//:>********************************************************************************************
//:>	RnImportTLFNewDiscardDlg methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
RnImportTLFNewDiscardDlg::RnImportTLFNewDiscardDlg()
{
	m_rid = kridImportTLFNewDiscardDlg;
}

/*----------------------------------------------------------------------------------------------
	Initialize the dialog in response to the WM_INITDIALOG message.
	All one-time initialization should be done here (that is, all controls have been created
	and have valid hwnd's, but they need initial values.)

	@param hwndCtrl Not used by this method.
	@param lp Not used by this method.

	@return True if successful.
----------------------------------------------------------------------------------------------*/
bool RnImportTLFNewDiscardDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	if (m_strDiscard.Length())
		::SetDlgItemText(m_hwnd, kctidImportTLFDiscard, m_strDiscard.Chars());

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}

/*----------------------------------------------------------------------------------------------
	This method is called by the framework when the user chooses the OK or the Apply Now button.
	When the framework calls this method, changes made in the dialog are accepted.
	The default OnApply closes the dialog.

	@param fClose not used here

	@return True if successful.
----------------------------------------------------------------------------------------------*/
bool RnImportTLFNewDiscardDlg::OnApply(bool fClose)
{
	SaveDialogValues();
	return SuperClass::OnApply(fClose);
}

/*----------------------------------------------------------------------------------------------
	Retrieve the values from the dialog, storing them in the member variables.
----------------------------------------------------------------------------------------------*/
void RnImportTLFNewDiscardDlg::SaveDialogValues()
{
	Vector<achar> vch;
	HWND hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFDiscard);
	int cch = ::GetWindowTextLength(hwnd);
	vch.Resize(cch + 1);
	::GetWindowText(hwnd, vch.Begin(), vch.Size());
	vch[cch] = 0;
	m_strDiscard = vch.Begin();
}


//:>********************************************************************************************
//:>	RnImportTLFNewSubstitutionDlg methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
RnImportTLFNewSubstitutionDlg::RnImportTLFNewSubstitutionDlg()
{
	m_rid = kridImportTLFNewSubstitutionDlg;
	m_pszHelpUrl = _T("Beginning_Tasks/Import_Standard_Format/Topics_List_Import_Options.htm");
}

/*----------------------------------------------------------------------------------------------
	Initialize the dialog in response to the WM_INITDIALOG message.
	All one-time initialization should be done here (that is, all controls have been created
	and have valid hwnd's, but they need initial values.)

	@param hwndCtrl Not used by this method.
	@param lp Not used by this method.

	@return True if successful.
----------------------------------------------------------------------------------------------*/
bool RnImportTLFNewSubstitutionDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	if (m_strMatch.Length())
	{
		::SetDlgItemText(m_hwnd, kctidImportTLFSubstMatch, m_strMatch.Chars());
		::SetDlgItemText(m_hwnd, kctidImportTLFSubstReplace, m_strReplace.Chars());
	}
	return SuperClass::OnInitDlg(hwndCtrl, lp);
}

/*----------------------------------------------------------------------------------------------
	This method is called by the framework when the user chooses the OK or the Apply Now button.
	When the framework calls this method, changes made in the dialog are accepted.
	The default OnApply closes the dialog.

	@param fClose not used here

	@return True if successful.
----------------------------------------------------------------------------------------------*/
bool RnImportTLFNewSubstitutionDlg::OnApply(bool fClose)
{
	SaveDialogValues();
	return SuperClass::OnApply(fClose);
}

/*----------------------------------------------------------------------------------------------
	Retrieve the values from the dialog, storing them in the member variables.
----------------------------------------------------------------------------------------------*/
void RnImportTLFNewSubstitutionDlg::SaveDialogValues()
{
	Vector<achar> vch;
	HWND hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFSubstMatch);
	int cch = ::GetWindowTextLength(hwnd);
	vch.Resize(cch + 1);
	::GetWindowText(hwnd, vch.Begin(), vch.Size());
	vch[cch] = 0;
	m_strMatch = vch.Begin();
	hwnd = ::GetDlgItem(m_hwnd, kctidImportTLFSubstReplace);
	cch = ::GetWindowTextLength(hwnd);
	vch.Resize(cch + 1);
	::GetWindowText(hwnd, vch.Begin(), vch.Size());
	vch[cch] = 0;
	m_strReplace = vch.Begin();
}


//:>********************************************************************************************
//:>	RnImportChrMappingDlg methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
RnImportChrMappingDlg::RnImportChrMappingDlg()
{
	m_rid = kridImportCharMappingDlg;
	m_pszHelpUrl = _T("Beginning_Tasks/Import_Standard_Format/Import_Character_Mapping.htm");
}

/*----------------------------------------------------------------------------------------------
	Initialize the dialog in response to the WM_INITDIALOG message.
	All one-time initialization should be done here (that is, all controls have been created
	and have valid hwnd's, but they need initial values.)

	@param hwndCtrl Not used by this method.
	@param lp Not used by this method.

	@return True if successful.
----------------------------------------------------------------------------------------------*/
bool RnImportChrMappingDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	// Fill in the old writing system combo box.
	InitWrtSysList(m_rcmp.m_strOldWritingSystem);
	// Fill in the character style combo box.
	StrApp str(m_rcmp.m_stuCharStyle.Chars(), m_rcmp.m_stuCharStyle.Length());
	InitStyleList(str);
	// Fill in the direct formatting combo box.
	HWND hwnd = ::GetDlgItem(m_hwnd, kctidImportCharMapDirFmtSelect);
	StrApp strBold(kstidImportWizChrMapDirFmtBold);			// "Bold"
	StrApp strItalic(kstidImportWizChrMapDirFmtItalic);		// "Italic"
	::SendMessage(hwnd, CB_ADDSTRING, 0, (LPARAM)strBold.Chars());
	::SendMessage(hwnd, CB_ADDSTRING, 0, (LPARAM)strItalic.Chars());
	::SendMessage(hwnd, CB_SETCURSEL, m_rcmp.m_tptDirect == ktptBold ? 0 : 1, 0);

	// Initialize the radio buttons and related items.
	str.Assign(m_rcmp.m_staBegin.Chars(), m_rcmp.m_staBegin.Length()); // Review SteveMc (JohnT): what conversion?
	::SetDlgItemText(m_hwnd, kctidImportCharMapBeginMkr, str.Chars());
	str.Assign(m_rcmp.m_staEnd.Chars(), m_rcmp.m_staEnd.Length()); // Review SteveMc (JohnT): what conversion?
	::SetDlgItemText(m_hwnd, kctidImportCharMapEndMkr, str.Chars());
	switch (m_rcmp.m_cmt)
	{
	case RnImportCharMapping::kcmtNone:
		::SendMessage(::GetDlgItem(m_hwnd, kctidImportCharMapIgnoreEnb), BM_SETCHECK,
			BST_CHECKED, 0);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidImportCharMapDirFmtSelect), FALSE);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidImportCharMapStyleSelect), FALSE);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidImportCharMapStyles), FALSE);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidImportCharMapWrtSystemSelect), FALSE);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidImportCharMapWrtSystems), FALSE);
		break;
	case RnImportCharMapping::kcmtDirectFormatting:
		::SendMessage(::GetDlgItem(m_hwnd, kctidImportCharMapDirFmtEnb), BM_SETCHECK,
			BST_CHECKED, 0);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidImportCharMapDirFmtSelect), TRUE);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidImportCharMapStyleSelect), FALSE);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidImportCharMapStyles), FALSE);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidImportCharMapWrtSystemSelect), FALSE);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidImportCharMapWrtSystems), FALSE);
		break;
	case RnImportCharMapping::kcmtCharStyle:
		::SendMessage(::GetDlgItem(m_hwnd, kctidImportCharMapStyleEnb), BM_SETCHECK,
			BST_CHECKED, 0);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidImportCharMapDirFmtSelect), FALSE);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidImportCharMapStyleSelect), TRUE);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidImportCharMapStyles), TRUE);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidImportCharMapWrtSystemSelect), FALSE);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidImportCharMapWrtSystems), FALSE);
		break;
	case RnImportCharMapping::kcmtOldWritingSystem:
		::SendMessage(::GetDlgItem(m_hwnd, kctidImportCharMapWrtSystemEnb), BM_SETCHECK,
			BST_CHECKED, 0);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidImportCharMapDirFmtSelect), FALSE);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidImportCharMapStyleSelect), FALSE);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidImportCharMapStyles), FALSE);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidImportCharMapWrtSystemSelect), TRUE);
		::EnableWindow(::GetDlgItem(m_hwnd, kctidImportCharMapWrtSystems), TRUE);
		break;
	}

	// Subclass the Help button.
	AfButtonPtr qbtn;
	qbtn.Create();
	qbtn->SubclassButton(m_hwnd, kctidHelp, kbtHelp, NULL, 0);

	// Subclass the Add button.
	AfButtonPtr qbtn2;
	qbtn2.Create();
	qbtn2->SubclassButton(m_hwnd, kctidImportCharMapWrtSystems, kbtPopMenu, NULL, 0);

	// Build the list of all available language writing systems.
	ILgWritingSystemFactoryPtr qwsf = m_priwcm->GetImportWizard()->GetWritingSystemFactory();
	AssertPtr(qwsf.Ptr());
	GetAllWrtSys(qwsf, m_vwsdAll);

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}

/*----------------------------------------------------------------------------------------------
	This method is called by the framework when the user chooses the OK or the Apply Now button.
	When the framework calls this method, changes made in the dialog are accepted.
	The default OnApply closes the dialog.

	@param fClose not used here

	@return True if successful.
----------------------------------------------------------------------------------------------*/
bool RnImportChrMappingDlg::OnApply(bool fClose)
{
	SaveDialogValues();
	return SuperClass::OnApply(fClose);
}

/*----------------------------------------------------------------------------------------------
	Handle a WM_NOTIFY message, first letting the superclass method handle it if possible.

	@param ctidFrom Identifies the control sending the message.
	@param pnmh Pointer to the notification message data.
	@param lnRet Reference to a long integer return value used by some messages.

	@return True if the message is handled successfully; otherwise, false.
----------------------------------------------------------------------------------------------*/
bool RnImportChrMappingDlg::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	if (SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet))
		return true;

	switch (pnmh->code)
	{
	case BN_CLICKED:
		if (ctidFrom == kctidImportCharMapIgnoreEnb)
		{
			::EnableWindow(::GetDlgItem(m_hwnd, kctidImportCharMapDirFmtSelect), FALSE);
			::EnableWindow(::GetDlgItem(m_hwnd, kctidImportCharMapStyleSelect), FALSE);
			::EnableWindow(::GetDlgItem(m_hwnd, kctidImportCharMapStyles), FALSE);
			::EnableWindow(::GetDlgItem(m_hwnd, kctidImportCharMapWrtSystemSelect), FALSE);
			::EnableWindow(::GetDlgItem(m_hwnd, kctidImportCharMapWrtSystems), FALSE);
		}
		else if (ctidFrom == kctidImportCharMapDirFmtEnb)
		{
			::EnableWindow(::GetDlgItem(m_hwnd, kctidImportCharMapDirFmtSelect), TRUE);
			::EnableWindow(::GetDlgItem(m_hwnd, kctidImportCharMapStyleSelect), FALSE);
			::EnableWindow(::GetDlgItem(m_hwnd, kctidImportCharMapStyles), FALSE);
			::EnableWindow(::GetDlgItem(m_hwnd, kctidImportCharMapWrtSystemSelect), FALSE);
			::EnableWindow(::GetDlgItem(m_hwnd, kctidImportCharMapWrtSystems), FALSE);
		}
		else if (ctidFrom == kctidImportCharMapStyleEnb)
		{
			::EnableWindow(::GetDlgItem(m_hwnd, kctidImportCharMapDirFmtSelect), FALSE);
			::EnableWindow(::GetDlgItem(m_hwnd, kctidImportCharMapStyleSelect), TRUE);
			::EnableWindow(::GetDlgItem(m_hwnd, kctidImportCharMapStyles), TRUE);
			::EnableWindow(::GetDlgItem(m_hwnd, kctidImportCharMapWrtSystemSelect), FALSE);
			::EnableWindow(::GetDlgItem(m_hwnd, kctidImportCharMapWrtSystems), FALSE);
		}
		else if (ctidFrom == kctidImportCharMapWrtSystemEnb)
		{
			::EnableWindow(::GetDlgItem(m_hwnd, kctidImportCharMapDirFmtSelect), FALSE);
			::EnableWindow(::GetDlgItem(m_hwnd, kctidImportCharMapStyleSelect), FALSE);
			::EnableWindow(::GetDlgItem(m_hwnd, kctidImportCharMapStyles), FALSE);
			::EnableWindow(::GetDlgItem(m_hwnd, kctidImportCharMapWrtSystemSelect), TRUE);
			::EnableWindow(::GetDlgItem(m_hwnd, kctidImportCharMapWrtSystems), TRUE);
		}
		else if (ctidFrom == kctidImportCharMapWrtSystems)
		{
			int nRet = OnAddWrtSysClicked(m_hwnd, ctidFrom, m_priwcm->GetImportWizard(),
				m_vwsdAll);
			if (nRet == ADDWS_ADDED)
			{
				// Reload the writing system combo box, using the newly added "active"
				// writing system as the selected value.
				Vector<WrtSysData> & vwsdActive =
					m_priwcm->GetImportWizard()->WritingSystems();
				m_rcmp.m_strOldWritingSystem.Assign(vwsdActive.Top()->m_stuName.Chars(),
					vwsdActive.Top()->m_stuName.Length());
				InitWrtSysList(m_rcmp.m_strOldWritingSystem);
			}
			return nRet;
		}
		else if (ctidFrom == kctidImportCharMapStyles)
		{
			// Fire up the Styles Dialog, and modify the topics list by the result.
			RnImportWizard * priw = m_priwcm->GetImportWizard();
			AssertPtr(priw);
//			AfMainWnd * pafw = priw->MainWindow();
			RnMainWnd * pafw = dynamic_cast<RnMainWnd *>(priw->MainWindow());
			if (!pafw)
				return false;
			// Save the old selection.
			HWND hwnd = ::GetDlgItem(m_hwnd, kctidImportCharMapStyleSelect);
			int isel = ::SendMessage(hwnd, CB_GETCURSEL, 0, 0);
			Vector<achar> vchSel;
			int cch = 0;
			if (isel != CB_ERR)
			{
				cch = ::SendMessage(hwnd, CB_GETLBTEXTLEN, isel, 0);
				vchSel.Resize(cch + 1);
				cch = ::SendMessage(hwnd, CB_GETLBTEXT, isel, (LPARAM)vchSel.Begin());
			}
			// Variables set by AdjustTsTextProps.
			StrUni stuStyleName(vchSel.Begin(), cch);
			bool fChanged = false;
			bool fCanDoRtl = false;		// ENHANCE (RTL): Fix this when the time comes!
			bool fOuterRtl = false;		// ENHANCE (RTL): Fix this when the time comes!
			ILgWritingSystemFactoryPtr qwsf = priw->GetWritingSystemFactory();
			AssertPtr(qwsf.Ptr());
			if (!AfStylesDlg::EditImportStyles(m_hwnd, fCanDoRtl, fOuterRtl, false, false,
				pafw->GetStylesheet(), qwsf, kstCharacter, stuStyleName, fChanged,
				AfApp::Papp()->GetAppClsid(), pafw->GetRootObj()))
			{
				return false;	// If EditImportStyles returns false, nothing changed.
			}
			// Reload combobox with possibly changed list of character styles.
			priw->LoadStyles();
			StrApp strStyle(stuStyleName);
			InitStyleList(strStyle);
			// Set the desired selection.
			isel = ::SendMessage(hwnd, CB_FINDSTRINGEXACT, (WPARAM)-1,
				(LPARAM)strStyle.Chars());
			if (isel != CB_ERR)
			{
				::SendMessage(hwnd, CB_SETCURSEL, isel, 0);
			}
			else
			{
				if (vchSel.Size())
					isel = ::SendMessage(hwnd, CB_FINDSTRINGEXACT, (WPARAM)-1,
						(LPARAM)vchSel.Begin());
				if (isel != CB_ERR)
				{
					::SendMessage(hwnd, CB_SETCURSEL, isel, 0);
				}
				else
				{
					// If all else fails, fall back to the first style shown.
					::SendMessage(hwnd, CB_SETCURSEL, 0, 0);
				}
			}
			return true;
		}
		break;
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Initialize the data for the controls embedded within the dialog box.

	@param priwcm Pointer to the parent wizard page.
	@param pricmp Pointer to the initial values (possibly NULL).
----------------------------------------------------------------------------------------------*/
void RnImportChrMappingDlg::Initialize(RnImportWizChrMappings * priwcm,
	RnImportCharMapping * pricmp)
{
	m_priwcm = priwcm;
	if (pricmp)
	{
		m_rcmp.m_staBegin = pricmp->m_staBegin;
		m_rcmp.m_staEnd = pricmp->m_staEnd;
		m_rcmp.m_cmt = pricmp->m_cmt;
		m_rcmp.m_strOldWritingSystem = pricmp->m_strOldWritingSystem;
		m_rcmp.m_stuCharStyle = pricmp->m_stuCharStyle;
		m_rcmp.m_tptDirect = pricmp->m_tptDirect;
	}
	else
	{
		m_rcmp.m_cmt = RnImportCharMapping::kcmtNone;
		m_rcmp.m_tptDirect = ktptBold;
	}
}

/*----------------------------------------------------------------------------------------------
	Retrieve the values from the dialog, storing them in the member variables.
----------------------------------------------------------------------------------------------*/
void RnImportChrMappingDlg::SaveDialogValues()
{
	Vector<achar> vch;
	HWND hwnd = ::GetDlgItem(m_hwnd, kctidImportCharMapBeginMkr);
	int cch = ::GetWindowTextLength(hwnd);
	vch.Resize(cch + 1);
	::GetWindowText(hwnd, vch.Begin(), vch.Size());
	m_rcmp.m_staBegin.Assign(vch.Begin(), cch); // Review SteveMc (JohnT): what conversion?

	hwnd = ::GetDlgItem(m_hwnd, kctidImportCharMapEndMkr);
	cch = ::GetWindowTextLength(hwnd);
	vch.Resize(cch + 1);
	::GetWindowText(hwnd, vch.Begin(), vch.Size());
	m_rcmp.m_staEnd.Assign(vch.Begin(), cch); // Review SteveMc (JohnT): what conversion?

	if (::SendMessage(::GetDlgItem(m_hwnd, kctidImportCharMapWrtSystemEnb), BM_GETCHECK, 0, 0)
		== BST_CHECKED)
	{
		m_rcmp.m_cmt = RnImportCharMapping::kcmtOldWritingSystem;
	}
	else if (::SendMessage(::GetDlgItem(m_hwnd, kctidImportCharMapStyleEnb), BM_GETCHECK, 0, 0)
		== BST_CHECKED)
	{
		m_rcmp.m_cmt = RnImportCharMapping::kcmtCharStyle;
	}
	else if (::SendMessage(::GetDlgItem(m_hwnd, kctidImportCharMapDirFmtEnb), BM_GETCHECK, 0, 0)
		== BST_CHECKED)
	{
		m_rcmp.m_cmt = RnImportCharMapping::kcmtDirectFormatting;
	}
	else
	{
		m_rcmp.m_cmt = RnImportCharMapping::kcmtNone;
	}

	int isel;
	hwnd = ::GetDlgItem(m_hwnd, kctidImportCharMapWrtSystemSelect);
	isel = ::SendMessage(hwnd, CB_GETCURSEL, 0, 0);
	if (isel == CB_ERR)
		isel = 0;
	cch = ::SendMessage(hwnd, CB_GETLBTEXTLEN, isel, 0);
	vch.Resize(cch + 1);
	cch = ::SendMessage(hwnd, CB_GETLBTEXT, isel, (LPARAM)vch.Begin());
	m_rcmp.m_strOldWritingSystem.Assign(vch.Begin(), cch);

	hwnd = ::GetDlgItem(m_hwnd, kctidImportCharMapStyleSelect);
	isel = ::SendMessage(hwnd, CB_GETCURSEL, 0, 0);
	if (isel == CB_ERR)
		isel = 0;
	cch = ::SendMessage(hwnd, CB_GETLBTEXTLEN, isel, 0);
	vch.Resize(cch + 1);
	cch = ::SendMessage(hwnd, CB_GETLBTEXT, isel, (LPARAM)vch.Begin());
	m_rcmp.m_stuCharStyle.Assign(vch.Begin(), cch);

	isel = ::SendMessage(::GetDlgItem(m_hwnd, kctidImportCharMapDirFmtSelect), CB_GETCURSEL,
		0, 0);
	m_rcmp.m_tptDirect = (isel == 1) ? ktptItalic : ktptBold;
}

/*----------------------------------------------------------------------------------------------
	Initialize the character style combo box.
----------------------------------------------------------------------------------------------*/
void RnImportChrMappingDlg::InitStyleList(StrApp & strSel)
{
	RnImportWizard * priw = m_priwcm->GetImportWizard();
	AssertPtr(priw);
	StrUni & stuDef = priw->DefaultCharStyle();
	Vector<StrUni> & vstuStyles = priw->CharStyleNames();
	HWND hwnd = ::GetDlgItem(m_hwnd, kctidImportCharMapStyleSelect);
	::SendMessage(hwnd, CB_RESETCONTENT, 0, 0);
	int isel = -1;
	int iDef = 0;
	int istu;
	StrApp str;
	for (istu = 0; istu < vstuStyles.Size(); ++istu)
	{
		str = vstuStyles[istu];
		::SendMessage(hwnd, CB_ADDSTRING, 0, (LPARAM)str.Chars());
		if (str == strSel)
			isel = istu;
		if (stuDef == vstuStyles[istu])
			iDef = istu;
	}
	if (isel != -1)
		::SendMessage(hwnd, CB_SETCURSEL, isel, 0);
	else
		::SendMessage(hwnd, CB_SETCURSEL, iDef, 0);
}

/*----------------------------------------------------------------------------------------------
	Initialize the Writing Systems combo box.
----------------------------------------------------------------------------------------------*/
void RnImportChrMappingDlg::InitWrtSysList(StrApp & strSel)
{
	RnImportWizard * priw = m_priwcm->GetImportWizard();
	AssertPtr(priw);
	Vector<WrtSysData> & vwsd = priw->WritingSystems();
	HWND hwnd = ::GetDlgItem(m_hwnd, kctidImportCharMapWrtSystemSelect);
	::SendMessage(hwnd, CB_RESETCONTENT, 0, 0);
	int iows;
	int isel = 0;
	StrApp str;
	for (iows = 0; iows < vwsd.Size(); ++iows)
	{
		str.Assign(vwsd[iows].m_stuName.Chars(), vwsd[iows].m_stuName.Length());
		::SendMessage(hwnd, CB_ADDSTRING, 0, (LPARAM)str.Chars());
		if (str == strSel)
			isel = iows;
	}
	::SendMessage(hwnd, CB_SETCURSEL, isel, 0);
}

/*----------------------------------------------------------------------------------------------
	Handle a popup menu command for adding a language writing system.

	@param pcmd Pointer to the command information.

	@return True.
----------------------------------------------------------------------------------------------*/
bool RnImportChrMappingDlg::CmdAddWrtSys(Cmd * pcmd)
{
	// We don't need to do anything here, since ::TrackPopupMenu() is set to return the code.
	return true;
}


//:>********************************************************************************************
//:>	RnImportDateFormatDlg methods.
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
RnImportDateFormatDlg::RnImportDateFormatDlg()
{
	m_rid = kridImportDateFormatDlg;
	m_pszHelpUrl =
		_T("Beginning_Tasks/Import_Standard_Format/Import_Date_Format_Definition.htm");
}

/*----------------------------------------------------------------------------------------------
	Initialize the dialog in response to the WM_INITDIALOG message.
	All one-time initialization should be done here (that is, all controls have been created
	and have valid hwnd's, but they need initial values.)

	@param hwndCtrl Not used by this method.
	@param lp Not used by this method.

	@return True if successful.
----------------------------------------------------------------------------------------------*/
bool RnImportDateFormatDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	// Fill in the format edit box and the example display (readonly edit) box.
	if (m_strFormat.Length())
	{
		::SetDlgItemText(m_hwnd, kctidImportDateFormat, m_strFormat.Chars());
		SYSTEMTIME systim;		// "March 2, 2001"
		systim.wYear = 2001;
		systim.wMonth = 3;
		systim.wDay = 2;
		achar rgch[128];
		int cch = ::GetDateFormat(m_lcid, 0, &systim, m_strFormat.Chars(), rgch, 128);
		if (cch > 0)
		{
			rgch[cch] = '\0';
			::SetDlgItemText(m_hwnd, kctidImportFormattedDate, rgch);
		}
	}
	return SuperClass::OnInitDlg(hwndCtrl, lp);
}

/*----------------------------------------------------------------------------------------------
	This method is called by the framework when the user chooses the OK or the Apply Now button.
	When the framework calls this method, changes made in the dialog are accepted.
	The default OnApply closes the dialog.

	@param fClose not used here

	@return True if successful.
----------------------------------------------------------------------------------------------*/
bool RnImportDateFormatDlg::OnApply(bool fClose)
{
	if (!SaveDialogValues())
		return false;
	return SuperClass::OnApply(fClose);
}

/*----------------------------------------------------------------------------------------------
	Handle a WM_NOTIFY message, first letting the superclass method handle it if possible.

	@param ctidFrom Identifies the control sending the message.
	@param pnmh Pointer to the notification message data.
	@param lnRet Reference to a long integer return value used by some messages.

	@return True if the message is handled successfully; otherwise, false.
----------------------------------------------------------------------------------------------*/
bool RnImportDateFormatDlg::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	if (SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet))
		return true;

	switch (pnmh->code)
	{
	case BN_CLICKED:
		if (ctidFrom == kctidImportDateFormatApply)
		{
			Vector<achar> vch;
			HWND hwnd = ::GetDlgItem(m_hwnd, kctidImportDateFormat);
			int cch = ::GetWindowTextLength(hwnd);
			vch.Resize(cch + 1);
			::GetWindowText(hwnd, vch.Begin(), vch.Size());
			vch[cch] = '\0';
			// Check for valid characters, ensuring proper case for y, M, and d.
			bool fChanged = false;
			bool fError = false;
			int ich;
			int ch;
			for (ich = 0; ich < vch.Size(); ++ich)
			{
				ch = vch[ich];
				if (!ch)
					break;
				if (ch == 'y' || ch == 'M' || ch == 'd')
					continue;
				if (ch == 'Y')
				{
					vch[ich] = 'y';
					fChanged = true;
				}
				else if (ch == 'm')
				{
					vch[ich] = 'M';
					fChanged = true;
				}
				else if (ch == 'D')
				{
					vch[ich] = 'd';
					fChanged = true;
				}
				else if (!isascii(ch) || isalpha(ch) || isdigit(ch) || ch < ' ')
				{
					// Invalid character.
					fError = true;
					break;
				}
			}
			if (fError)
			{
				StrApp str(kstidImportDateFormatError);
				::SetDlgItemText(m_hwnd, kctidImportFormattedDate, str.Chars());
			}
			else
			{
				SYSTEMTIME systim;		// "March 2, 2001"
				systim.wYear = 2001;
				systim.wMonth = 3;
				systim.wDay = 2;
				achar rgch[128];
				cch = ::GetDateFormat(m_lcid, 0, &systim, vch.Begin(), rgch, 128);
				rgch[cch] = '\0';
				::SetDlgItemText(m_hwnd, kctidImportFormattedDate, rgch);
				if (fChanged)
				{
					::SetWindowText(hwnd, vch.Begin());
				}
			}
			return true;
		}
		break;
	}
	return false;
}

/*----------------------------------------------------------------------------------------------
	Initialize the data for the controls embedded within the dialog box.

	@param lcid Locale identifier for testing date formats.
	@param pszFormat Pointer to the intial data format string.
----------------------------------------------------------------------------------------------*/
void RnImportDateFormatDlg::Initialize(LCID lcid, const achar * pszFormat)
{
	m_lcid = lcid;
	if (pszFormat)
		m_strFormat = pszFormat;
	else
		m_strFormat.Clear();
}

/*----------------------------------------------------------------------------------------------
	Retrieve the values from the dialog, storing them in the member variables.
----------------------------------------------------------------------------------------------*/
bool RnImportDateFormatDlg::SaveDialogValues()
{
	// Get the current content of the edit box.
	Vector<achar> vch;
	HWND hwnd = ::GetDlgItem(m_hwnd, kctidImportDateFormat);
	int cch = ::GetWindowTextLength(hwnd);
	vch.Resize(cch + 1);
	::GetWindowText(hwnd, vch.Begin(), vch.Size());
	vch[cch] = '\0';
	// Test the format string against our standard value.
	SYSTEMTIME systim;		// "March 2, 2001"
	systim.wYear = 2001;
	systim.wMonth = 3;
	systim.wDay = 2;
	achar rgch[128];
	cch = ::GetDateFormat(m_lcid, 0, &systim, vch.Begin(), rgch, 128);
	if (cch < 0)
	{
		// Totally invalid format string!
		return false;
	}
	// Check for valid characters, ensuring proper case for y, M, and d.
	int ich;
	int ch;
	for (ich = 0; ich < vch.Size(); ++ich)
	{
		ch = vch[ich];
		if (!ch)
			break;
		if (ch == 'y' || ch == 'M' || ch == 'd')
			continue;
		if (ch == 'Y')
		{
			vch[ich] = 'y';
		}
		else if (ch == 'm')
		{
			vch[ich] = 'M';
		}
		else if (ch == 'D')
		{
			vch[ich] = 'd';
		}
		else if (!isascii(ch) || isalpha(ch) || isdigit(ch) || ch < ' ')
		{
			// Invalid character.
			StrApp strTitle(kstidImportMsgCaption);
			StrApp str(kstidImportDateFormatErrorMsg);
			::MessageBox(m_hwnd, str.Chars(), strTitle.Chars(), MB_OK | MB_ICONWARNING);
			return false;
		}
	}
	// Valid format string.
	m_strFormat.Assign(vch.Begin(), cch);
	return true;
}


//:>********************************************************************************************
//:>	RnImportProgressDlg Implementation
//:>********************************************************************************************

/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
RnImportProgressDlg::RnImportProgressDlg()
{
	m_rid = kridRnImportProgress;
	m_strStatusFmt.Load(kstidImportStatus);
	m_strShortStatusFmt.Load(kstidImportShortStatus);
	m_nPercent = 0;
}

/*----------------------------------------------------------------------------------------------
	Set the action (description) string.
----------------------------------------------------------------------------------------------*/
void RnImportProgressDlg::SetActionString(StrApp & str)
{
	Assert(m_nPercent >= 0 && m_nPercent <= 100);

	m_strAction = str;
	::SendDlgItemMessage(m_hwnd, kctidImportProgAction, WM_SETTEXT, 0,
		(LPARAM)m_strAction.Chars());
	::SendDlgItemMessage(m_hwnd, kctidImportProgStatus, WM_SETTEXT, 0,
		(LPARAM)m_strStatus.Chars());
	::SendDlgItemMessage(m_hwnd, kctidImportProgProgress, PBM_SETPOS, m_nPercent, 0);
	::BringWindowToTop(m_hwnd); // Needed to stop anomaly whereby dialog disappears
}

/*----------------------------------------------------------------------------------------------
	Set the status string.
----------------------------------------------------------------------------------------------*/
void RnImportProgressDlg::UpdateStatus(int cDone, int cTotal, int nDeltaSeconds)
{
	Assert((uint)cDone <= (uint)cTotal);
	Assert(cTotal >= 0);
	Assert(nDeltaSeconds >= 0);

	if (cTotal == 0)
	{
		m_nPercent = 0;
		m_strStatus.Clear();
	}
	else if (nDeltaSeconds == 0)
	{
		m_nPercent = (cDone * 100) / cTotal;
		m_strStatus.Format(m_strShortStatusFmt.Chars(), cDone, cTotal);
	}
	else
	{
		m_nPercent = (cDone * 100) / cTotal;
		int nSeconds = 1 + ((nDeltaSeconds * cTotal) / cDone) - nDeltaSeconds;
		m_strStatus.Format(m_strStatusFmt.Chars(), cDone, cTotal, nSeconds / 60, nSeconds % 60,
			nDeltaSeconds / 60, nDeltaSeconds % 60);
	}
	::SendDlgItemMessage(m_hwnd, kctidImportProgStatus, WM_SETTEXT, 0,
		(LPARAM)m_strStatus.Chars());
	::SendDlgItemMessage(m_hwnd, kctidImportProgAction, WM_SETTEXT, 0,
		(LPARAM)m_strAction.Chars());
	Assert(m_nPercent >= 0 && m_nPercent <= 100);
	::SendDlgItemMessage(m_hwnd, kctidImportProgProgress, PBM_SETPOS, m_nPercent, 0);
	::BringWindowToTop(m_hwnd); // Needed to stop anomaly whereby dialog disappears
}

/*----------------------------------------------------------------------------------------------
	Set the amount of the progress bar to be filled in.
----------------------------------------------------------------------------------------------*/
void RnImportProgressDlg::SetPercentComplete(int nPercent)
{
	Assert(nPercent >= 0 && nPercent <= 100);
	m_nPercent = nPercent;
	::SendDlgItemMessage(m_hwnd, kctidImportProgProgress, PBM_SETPOS, m_nPercent, 0);
	::SendDlgItemMessage(m_hwnd, kctidImportProgAction, WM_SETTEXT, 0,
		(LPARAM)m_strAction.Chars());
	if (m_nPercent == 0)
		m_strStatus.Clear();
	::SendDlgItemMessage(m_hwnd, kctidImportProgStatus, WM_SETTEXT, 0,
		(LPARAM)m_strStatus.Chars());
	::BringWindowToTop(m_hwnd); // Needed to stop anomaly whereby dialog disappears
}

/*----------------------------------------------------------------------------------------------
	Handle window messages.
----------------------------------------------------------------------------------------------*/
bool RnImportProgressDlg::FWndProc(uint wm, WPARAM wp, LPARAM lp, long & lnRet)
{
	AssertObj(this);

	if (wm == CHGENC_PRG_PERCENT)
	{
		SetPercentComplete(wp);
		return true;
	}
	return SuperClass::FWndProc(wm, wp, lp, lnRet);
}

/*----------------------------------------------------------------------------------------------
	Called by the framework to initialize the dialog. All one-time initialization should be
	done here (that is, all controls have been created and have valid hwnd's, but they need
	initial values.)
----------------------------------------------------------------------------------------------*/
bool RnImportProgressDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
	// Set the range of the progress bar:
	::SendDlgItemMessage(m_hwnd, kctidImportProgProgress, PBM_SETRANGE32, 0, 100);
	// Give the progress bar a white background:
	::SendDlgItemMessage(m_hwnd, kctidImportProgProgress, PBM_SETBKCOLOR, 0, (LPARAM)kclrWhite);

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}


//:>********************************************************************************************
//:>	RnImportEditListProgressDlg Implementation
//:>********************************************************************************************
#if 99
FILE * fp = NULL;
#endif


/*----------------------------------------------------------------------------------------------
	Constructor.
----------------------------------------------------------------------------------------------*/
RnImportEditListProgressDlg::RnImportEditListProgressDlg(int cTotal, const achar * pszFile,
	bool * pfCancelled)
{
	AssertPtr(pfCancelled);

	m_rid = kridRnImportListEditProgress;
	m_cTotal = cTotal;
	m_cDone = 0;
	m_pfCancelled = pfCancelled;
	m_strActionFmt.Load(kstidImportListEditList);
	m_strStatusFmt.Load(kstidImportListEditStatus);
	m_strFile.Assign(pszFile);
#if 99
	fopen_s(&fp, "C:\\FW\\DebugMessages.txt", "w");
#endif
}


/*----------------------------------------------------------------------------------------------
	Destructor.
----------------------------------------------------------------------------------------------*/
RnImportEditListProgressDlg::~RnImportEditListProgressDlg()
{
#if 99
	if (fp)
	{
		fclose(fp);
		fp = NULL;
	}
#endif
}



/*----------------------------------------------------------------------------------------------
	Called by the framework to initialize the dialog. All one-time initialization should be
	done here (that is, all controls have been created and have valid hwnd's, but they need
	initial values.)
----------------------------------------------------------------------------------------------*/
bool RnImportEditListProgressDlg::OnInitDlg(HWND hwndCtrl, LPARAM lp)
{
#if 99
	if (fp)
	{
		fprintf(fp,
"RnImportEditListProgressDlg::OnInitDlg(hwndCtrl = %d (%x), lp = %d (%x): m_hwnd = %d (%x)\n",
			hwndCtrl, hwndCtrl, lp, lp, m_hwnd, m_hwnd);
	}
#endif
	StrApp strMsg;

	// Set the caption string.
	StrApp strCaptionFmt(kstidImportListEditCaption);
	strMsg.Format(strCaptionFmt.Chars(), m_strFile.Chars());
	::SetWindowText(Hwnd(), strMsg.Chars());

	// Initialize the action string (to nothing).
	::SendDlgItemMessage(m_hwnd, kctidImportProgAction, WM_SETTEXT, 0, (LPARAM)_T(""));

	// Initialize the status string.
	strMsg.Format(m_strStatusFmt.Chars(), m_cDone, m_cTotal, m_cTotal - m_cDone);
	::SendDlgItemMessage(m_hwnd, kctidImportProgStatus, WM_SETTEXT, 0,
		(LPARAM)strMsg.Chars());

	// Initialize the progress bar.
	::SendDlgItemMessage(m_hwnd, kctidImportProgProgress, PBM_SETRANGE32, 0, m_cTotal);
	::SendDlgItemMessage(m_hwnd, kctidImportProgProgress, PBM_SETBKCOLOR, 0, (LPARAM)kclrWhite);
	::SendDlgItemMessage(m_hwnd, kctidImportProgProgress, PBM_SETPOS, m_cDone, 0);

	return SuperClass::OnInitDlg(hwndCtrl, lp);
}


/*----------------------------------------------------------------------------------------------
	Handle a WM_NOTIFY message, first letting the superclass method handle it if possible.

	@param ctidFrom Identifies the control sending the message.
	@param pnmh Pointer to the notification message data.
	@param lnRet Reference to a long integer return value used by some messages.

	@return True if the message is handled successfully; otherwise, false.
----------------------------------------------------------------------------------------------*/
bool RnImportEditListProgressDlg::OnNotifyChild(int ctidFrom, NMHDR * pnmh, long & lnRet)
{
	AssertPtr(pnmh);

	if (SuperClass::OnNotifyChild(ctidFrom, pnmh, lnRet))
		return true;

	switch (pnmh->code)
	{
	case BN_CLICKED:
		if (ctidFrom == kctidImportListEditCancel)
		{
			AssertPtr(m_pfCancelled);
			*m_pfCancelled = true;
			// Signal that cancellation is underway by disabling button.
			::EnableWindow(::GetDlgItem(m_hwnd, kctidImportListEditCancel), FALSE);
			return true;
		}
		break;
	default:
		break;
	}
	return false;
}


/*----------------------------------------------------------------------------------------------
	Update the various status displays in the progress dialog for editing another list.
----------------------------------------------------------------------------------------------*/
void RnImportEditListProgressDlg::UpdateStatus(const achar * pszList)
{
	StrApp strMsg;

	strMsg.Format(m_strActionFmt.Chars(), pszList);
	::SendDlgItemMessage(m_hwnd, kctidImportProgAction, WM_SETTEXT, 0, (LPARAM)strMsg.Chars());

	strMsg.Format(m_strStatusFmt.Chars(), m_cDone, m_cTotal, m_cTotal - m_cDone);
	::SendDlgItemMessage(m_hwnd, kctidImportProgStatus, WM_SETTEXT, 0,
		(LPARAM)strMsg.Chars());

	::SendDlgItemMessage(m_hwnd, kctidImportProgProgress, PBM_SETPOS, m_cDone, 0);
	++m_cDone;
}

/*----------------------------------------------------------------------------------------------
	Ignore IDCANCEL messages.
----------------------------------------------------------------------------------------------*/
bool RnImportEditListProgressDlg::OnCancel()
{
	return true;
}

//:>********************************************************************************************
//:>	Local static functions.
//:>********************************************************************************************


/*----------------------------------------------------------------------------------------------
	Static method to add a new writing system.

	@return 0 if error, 1 if nothing added, 2 if writing system added.
----------------------------------------------------------------------------------------------*/
static int AddNewWritingSystem(RnImportWizard * priw, ILgWritingSystemFactory * pwsf, HWND hwnd,
	Vector<WrtSysData> & vwsdAll, Vector<WrtSysData> & vwsdActive)
{
	// Start of using C# WritingSystem wizard
	FwCoreDlgs::IWritingSystemWizardPtr qwsw;
	qwsw.CreateInstance("FwCoreDlgs.WritingSystemWizard");
	IHelpTopicProviderPtr qhtprov = new HelpTopicProvider(AfApp::Papp()->GetHelpBaseName());
	qwsw->Init(pwsf, qhtprov);
	long nRet;
	qwsw->DisplayDialog(&nRet);
	if (nRet == kctidOk)
	{
		// Rebuild vwsdAll and vwsdActive from pwsf.
		IWritingSystemPtr qwsNew;
		IUnknownPtr qunk;
		qwsw->WritingSystem(&qunk);
		if (qunk)
			qunk->QueryInterface(IID_IWritingSystem, (void **)&qwsNew);
		if (qwsNew)
		{
			// Add this new language to both the active list and the full list.
			WrtSysData wsd;
			priw->LoadWrtSysData(qwsNew, wsd);
			vwsdActive.Push(wsd);
			vwsdAll.Push(wsd);
			return ADDWS_ADDED;
		}
	}
	// End of using C# WritingSystem wizard

	return ADDWS_NOTADDED;
}

/*----------------------------------------------------------------------------------------------
	Static method to load all the writing system info from the factory.
----------------------------------------------------------------------------------------------*/
static void GetAllWrtSys(ILgWritingSystemFactory * pwsf, Vector<WrtSysData> & vwsdAll)
{
	AssertPtr(pwsf);

	int wsUser;
	CheckHr(pwsf->get_UserWs(&wsUser));
	int cws;
	CheckHr(pwsf->get_NumberOfWs(&cws));
	Vector<int> vws;
	vws.Resize(cws);
	CheckHr(pwsf->GetWritingSystems(vws.Begin(), vws.Size()));
	SmartBstr sbstr;
	WrtSysData wsd;
	IWritingSystemPtr qws;
	for (int iws = 0; iws < cws; ++iws)
	{
		if (vws[iws] == 0)
			continue;

		CheckHr(pwsf->get_EngineOrNull(vws[iws], &qws));
		if (!qws)
			continue;
		CheckHr(qws->get_UiName(wsUser, &sbstr));
		wsd.m_ws = vws[iws];
		wsd.m_stuName.Assign(sbstr.Chars());
		CheckHr(qws->get_Locale(&wsd.m_lcid));
		CheckHr(qws->get_DefaultSerif(&sbstr));
		if (sbstr)
			wsd.m_stuNormalFont.Assign(sbstr.Chars());
		else
			wsd.m_stuNormalFont.Clear();
		CheckHr(qws->get_DefaultSansSerif(&sbstr));
		if (sbstr)
			wsd.m_stuHeadingFont.Assign(sbstr.Chars());
		else
			wsd.m_stuHeadingFont.Clear();
		CheckHr(qws->get_DefaultBodyFont(&sbstr));
		if (sbstr)
			wsd.m_stuBodyFont.Assign(sbstr.Chars());
		else
			wsd.m_stuBodyFont.Clear();
		vwsdAll.Push(wsd);
	}
}

/*----------------------------------------------------------------------------------------------
	Handle clicking the Add (writing system) pushbutton.  Always return true.

	@return 0 if error, 1 if nothing added, 2 if writing system added.
----------------------------------------------------------------------------------------------*/
static int OnAddWrtSysClicked(HWND hwnd, int ctidFrom, RnImportWizard * priw,
	Vector<WrtSysData> & vwsdAll)
{
	// create a menu consisting of the names of the defined language encodings
	// that are not already on the drop-down list of encodings, plus the command
	// "Define New..." at the end, following a separator.
	HMENU hmenu = ::CreatePopupMenu();
	if (!hmenu)
		ThrowHr(WarnHr(E_FAIL));
	Vector<WrtSysData> & vwsdActive = priw->WritingSystems();
	StrApp str;
	int iwsd;
	bool fIgnore;
	for (iwsd = 0; iwsd < vwsdAll.Size(); ++iwsd)
	{
		fIgnore = !vwsdAll[iwsd].m_ws;
		if (fIgnore)
			continue;
		for (int i = 0; i < vwsdActive.Size(); ++i)
		{
			if (vwsdActive[i].m_ws == vwsdAll[iwsd].m_ws)
			{
				fIgnore = true;
				break;
			}
		}
		if (fIgnore)
			continue;
		str.Assign(vwsdAll[iwsd].m_stuName);
		::AppendMenu(hmenu, MF_STRING, kcidMenuItemDynMin + iwsd, str.Chars());
	}
	::AppendMenu(hmenu, MF_SEPARATOR, 0, 0);
	str.Load(kstidLngPrjNewWrtSys);
	::AppendMenu(hmenu, MF_STRING, kcidMenuItemDynMin + vwsdAll.Size(), str.Chars());

	// Show the popup menu that allows a user to choose the language.
	Rect rc;
	::GetWindowRect(::GetDlgItem(hwnd, ctidFrom), &rc);

	AfApp::GetMenuMgr()->SetMenuHandler(ctidFrom);
	int icmd = ::TrackPopupMenu(hmenu, TPM_LEFTALIGN | TPM_RIGHTBUTTON | TPM_RETURNCMD,
		rc.left, rc.bottom, 0, hwnd, NULL);
	::DestroyMenu(hmenu);
	if (icmd)
	{
		iwsd = icmd - kcidMenuItemDynMin;
		if (iwsd == vwsdAll.Size())
		{
			return AddNewWritingSystem(priw, priw->GetWritingSystemFactory(), hwnd,
				vwsdAll, vwsdActive);
		}
		else
		{
			// Insert an existing language into the active list.
			vwsdActive.Push(vwsdAll[iwsd]);
			return ADDWS_ADDED;
		}
	}
	return ADDWS_NOTADDED;
}

/*----------------------------------------------------------------------------------------------
	Scan through a ListView to get the (first) selected item if any.  Return the 0-based index
	of the selection, or -1 if nothing is selected.
----------------------------------------------------------------------------------------------*/
static int GetFirstListViewSelection(HWND hwndList)
{
	int csel = ListView_GetItemCount(hwndList);
	int isel;
	UINT nselState;
	for (isel = 0; isel < csel; ++isel)
	{
		nselState = ListView_GetItemState(hwndList, isel, LVIS_SELECTED);
		if (nselState & LVIS_SELECTED)
			return isel;
	}
	return -1;
}


/*----------------------------------------------------------------------------------------------
	Initialize the writing systems combo box.
----------------------------------------------------------------------------------------------*/
static void InitializeWrtSysList(HWND hwnd, RnImportWizard * priw, int wsDefault,
	const wchar * pszSel)
{
	Vector<WrtSysData> & vwsd = priw->WritingSystems();
	AssertPtr(priw->MainWindow());
	AfLpInfo * plpi = priw->MainWindow()->GetLpInfo();
	int wsAnal = plpi->AnalWs();
	::SendMessage(hwnd, CB_RESETCONTENT, 0, 0);
	int iDef = -1;
	int iDef2 = 0;
	int iwsd;
	StrApp str;
	for (iwsd = 0; iwsd < vwsd.Size(); ++iwsd)
	{
		str = vwsd[iwsd].m_stuName;
		::SendMessage(hwnd, CB_ADDSTRING, 0, (LPARAM)str.Chars());
		if (pszSel && vwsd[iwsd].m_stuName == pszSel)
			iDef = iwsd;
		else if (iDef == -1 && vwsd[iwsd].m_ws == wsDefault)
			iDef = iwsd;
		else if (vwsd[iwsd].m_ws == wsAnal)
			iDef2 = iwsd;
	}
	if (iDef != -1)
		::SendMessage(hwnd, CB_SETCURSEL, iDef, 0);
	else
		::SendMessage(hwnd, CB_SETCURSEL, iDef2, 0);
}


// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mknb.bat"
// End: (These 4 lines are useful to Steve McConnel.)

// Handle explicit instantiation.
#include "Vector_i.cpp"
#include "HashMap_i.cpp"
#include "Set_i.cpp"
