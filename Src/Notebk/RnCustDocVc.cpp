/*----------------------------------------------------------------------------------------------
Copyright 2000, SIL International. All rights reserved.

File: RnCustDocVc.cpp
Responsibility: John Thomson
Last reviewed: never

Description:
	This class provides a customiseable view constructor for document views.
----------------------------------------------------------------------------------------------*/
#include "Main.h"
#pragma hdrstop
#include "Vector_i.cpp"
#include "GpHashMap_i.cpp"

#undef THIS_FILE
DEFINE_THIS_FILE

static DummyFactory g_fact(_T("Sil.Notebk.RnCustDocVc"));

/*----------------------------------------------------------------------------------------------
	Construct the view constructor. Pass the RecordSpec of the view it is for, which allows it
	to find its block specs.
	ENHANCE JohnT: This is a rather low level object to know about the application and its list
	of views. Should we just pass in the list of block specs?
----------------------------------------------------------------------------------------------*/
RnCustDocVc::RnCustDocVc(UserViewSpec * puvs, AfLpInfo * plpi)
	: VwCustDocVc(puvs, plpi, kflidRnGenericRec_Title, kflidRnGenericRec_SubRecords)
{
	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	StrUni stu;

	stu.Load(kstidSpaces6);
	CheckHr(qtsf->MakeStringRgch(stu.Chars(), stu.Length(), m_wsUser, &m_qtssMissing));

	stu.Load(kstidSpaces1);
	CheckHr(qtsf->MakeStringRgch(stu.Chars(), stu.Length(), m_wsUser, &m_qtssPartEnd));

	StrUni stuAnalysis(kstidAnalysis);
	CheckHr(qtsf->MakeStringRgch(stuAnalysis.Chars(), stuAnalysis.Length(), m_wsUser,
		&m_qtssAnalysis));
	StrUni stuEvent(kstidEvent);
	CheckHr(qtsf->MakeStringRgch(stuEvent.Chars(), stuEvent.Length(), m_wsUser, &m_qtssEvent));

	ITsPropsBldrPtr qtpb;
	qtpb.CreateInstance(CLSID_TsPropsBldr);
	// Border thickness below about 1/96 inch, a single pixel on a typical display
	CheckHr(qtpb->SetIntPropValues(ktptBorderBottom, ktpvMilliPoint, kdzmpInch / 96));
	// About a line (say 12 point) of white space above and below the border
	CheckHr(qtpb->SetIntPropValues(ktptPadBottom, ktpvMilliPoint, 12000));
	CheckHr(qtpb->SetIntPropValues(ktptMarginBottom, ktpvMilliPoint, 12000));
	CheckHr(qtpb->GetTextProps(&m_qttpMain));

	// And make the one for subentries
	qtpb.CreateInstance(CLSID_TsPropsBldr);
	// Border thickness below about 1/96 inch, a single pixel on a typical display
	CheckHr(qtpb->SetIntPropValues(ktptPadLeading, ktpvMilliPoint, kdzmpInch * 3 / 10));
	CheckHr(qtpb->GetTextProps(&m_qttpSub));

	RnRecVc * prrvc = NewObj RnRecVc;
	AssertPtr(plpi);
	AfDbInfo * pdbi = plpi->GetDbInfo();
	prrvc->SetDbInfo(pdbi);
	m_qRecVc.Attach(prrvc);
}

RnCustDocVc::~RnCustDocVc()
{
}

/*----------------------------------------------------------------------------------------------
	Load data needed to display the specified objects using the specified fragment.
	This is called before attempting to Display an item that has been listed for lazy display
	using AddLazyItems. It may be used to load the necessary data into the DataAccess object.
	If you are not using AddLazyItems this method may be left unimplemented.
	If you pre-load all the data, it should trivially succeed (i.e., without doing anything).
	(That is the case here, for the moment.)
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RnCustDocVc::LoadDataFor(IVwEnv * pvwenv, HVO * prghvo, int chvo, HVO hvoParent,
	int tag, int frag, int ihvoMin)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvwenv);
	ChkComArrayArg(prghvo, chvo);
	CheckHr(SuperClass::LoadDataFor(pvwenv, prghvo, chvo, hvoParent, tag, frag, ihvoMin));
	FixEmptyStrings(prghvo, chvo, kflidRnGenericRec_Title, m_qlpi->AnalWs());
	END_COM_METHOD(g_fact, IID_IVwViewConstructor);
}

void RnCustDocVc::SetSubItems(IVwEnv * pvwenv, int flid)
{
	switch (flid)
	{
	default:
		Assert(false);
		break;
	case kflidRnEvent_Participants:
		CheckHr(pvwenv->AddObjVec(flid, this, kfrcdEvnParticipants));
		break;
	}
}

/*----------------------------------------------------------------------------------------------
	This is the main interesting method of displaying objects and fragments of them.
	Here an RnResearchNbk is displayed by displaying its Records.
	So far we only handle records that are RnEvents.
	An RnEvent is displayed according to the stored specifications.
----------------------------------------------------------------------------------------------*/
STDMETHODIMP RnCustDocVc::Display(IVwEnv * pvwenv, HVO hvo, int frag)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvwenv);

	// Constant fragments
	switch(frag)
	{
	case kfrcdRpsParticipants:
		CheckHr(pvwenv->AddObjVec(kflidRnRoledPartic_Participants,
			this, kfrcdTagSeq));
		break;
	default:
		return SuperClass::Display(pvwenv, hvo, frag);
	}

	return S_OK;

	END_COM_METHOD(g_fact, IID_IVwViewConstructor);
}


/*----------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------*/
STDMETHODIMP RnCustDocVc::DisplayVec(IVwEnv * pvwenv, HVO hvo, int tag, int frag)
{
	BEGIN_COM_METHOD;
	ChkComArgPtr(pvwenv);

	switch(frag)
	{
	case kfrcdEvnParticipants:
		{
			ISilDataAccessPtr qsda;
			CheckHr(pvwenv->get_DataAccess(&qsda));
			int chvo;
			CheckHr(qsda->get_VecSize(hvo, tag, &chvo));
			if (!chvo)
			{
				CheckHr(pvwenv->AddString(m_qtssMissing));
				return S_OK;
			}
			// Make sure the unspecified item is first.
			Vector<HVO> vhvo;
			int ihvo;
			HVO hvoRpar;
			for (ihvo = 0; ihvo < chvo; ++ihvo)
			{
				CheckHr(qsda->get_VecItem(hvo, tag, ihvo, &hvoRpar));
				int chvoT;
				CheckHr(qsda->get_VecSize(hvoRpar, kflidRnRoledPartic_Participants,
					&chvoT));
				if (!chvoT)
					continue; // Don't put out anything for roles with no participants.
				HVO hvoRole;
				CheckHr(qsda->get_ObjectProp(hvoRpar, kflidRnRoledPartic_Role,
					&hvoRole));
				if (hvoRole)
					vhvo.Push(hvoRpar);
				else
					vhvo.Insert(0, hvoRpar);
			}
			// Now add information from each roled participant.
			chvo = vhvo.Size();
			for (ihvo = 0; ihvo < chvo; ihvo++)
			{
				HVO hvoRole;
				CheckHr(qsda->get_ObjectProp(vhvo[ihvo], kflidRnRoledPartic_Role,
					&hvoRole));
				if (ihvo != 0)
					CheckHr(pvwenv->AddString(m_qtssPartEnd));
				if (hvoRole)
				{
					// Add the role label. (NULL participants was already output.)
					CheckHr(pvwenv->put_IntProperty(ktptBold, ktpvEnum, kttvForceOn));
					CheckHr(pvwenv->put_IntProperty(ktptItalic, ktpvEnum, kttvForceOn));
					CheckHr(pvwenv->AddObj(hvoRole, this, kfrcdPliName));
					CheckHr(pvwenv->put_IntProperty(ktptBold, ktpvEnum, kttvForceOn));
					CheckHr(pvwenv->put_IntProperty(ktptItalic, ktpvEnum, kttvForceOn));
					CheckHr(pvwenv->AddString(m_qtssColon));
				}
				else if (ihvo != 0)
					CheckHr(pvwenv->AddString(m_qtssListSep)); // Shouldn't normally happen.
				// Output the people for this role. We verified above that there are some.
				CheckHr(pvwenv->AddObj(vhvo[ihvo], this, kfrcdRpsParticipants));
			}
			break;
		}
	default:
		// Superclass to handle the others.
		return SuperClass::DisplayVec(pvwenv, hvo, tag, frag);
		break;
	}

	return S_OK;

	END_COM_METHOD(g_fact, IID_IVwViewConstructor);
}


#include "Vector_i.cpp"
template Vector<int>; // IntVec;

#include "Set_i.cpp"
template Set<HVO>; // HvoSet;
