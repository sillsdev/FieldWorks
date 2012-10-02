

/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 7.00.0500 */
/* at Tue Aug 30 15:30:01 2011
 */
/* Compiler settings for D:\jenkins\jobs\FieldWorks-CalgaryFW70-build-tlb\workspace\Output\Common\FwCellarTlb.idl:
	Oicf, W1, Zp8, env=Win32 (32b run)
	protocol : dce , ms_ext, c_ext, robust
	error checks: allocation ref bounds_check enum stub_data
	VC __declspec() decoration level:
		 __declspec(uuid()), __declspec(selectany), __declspec(novtable)
		 DECLSPEC_UUID(), MIDL_INTERFACE()
*/
//@@MIDL_FILE_HEADING(  )

#pragma warning( disable: 4049 )  /* more than 64k source lines */


/* verify that the <rpcndr.h> version is high enough to compile this file*/
#ifndef __REQUIRED_RPCNDR_H_VERSION__
#define __REQUIRED_RPCNDR_H_VERSION__ 475
#endif

#include "rpc.h"
#include "rpcndr.h"

#ifndef __RPCNDR_H_VERSION__
#error this stub requires an updated version of <rpcndr.h>
#endif // __RPCNDR_H_VERSION__


#ifndef __FwCellarTlb_h__
#define __FwCellarTlb_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */

/* header files for imported files */
#include "oaidl.h"
#include "ocidl.h"

#ifdef __cplusplus
extern "C"{
#endif


/* interface __MIDL_itf_FwCellarTlb_0000_0000 */
/* [local] */


#undef ATTACH_GUID_TO_CLASS
#if defined(__cplusplus)
#define ATTACH_GUID_TO_CLASS(type, guid, cls) \
	type __declspec(uuid(#guid)) cls;
#else // !defined(__cplusplus)
#define ATTACH_GUID_TO_CLASS(type, guid, cls)
#endif // !defined(__cplusplus)

#ifndef DEFINE_COM_PTR
#define DEFINE_COM_PTR(cls)
#endif

#undef GENERIC_DECLARE_SMART_INTERFACE_PTR
#define GENERIC_DECLARE_SMART_INTERFACE_PTR(cls, iid) \
	ATTACH_GUID_TO_CLASS(interface, iid, cls); \
	DEFINE_COM_PTR(cls);


#ifndef CUSTOM_COM_BOOL
typedef VARIANT_BOOL ComBool;

#endif

#if 0
// This is so there is an equivalent VB type.
typedef CY SilTime;

#elif defined(SILTIME_IS_STRUCT)
// This is for code that compiles UtilTime.*.
struct SilTime;
#else
// This is for code that uses a 64-bit integer for SilTime.
typedef __int64 SilTime;
#endif

ATTACH_GUID_TO_CLASS(class,
2F0FCCC0-C160-11d3-8DA2-005004DEFEC4
,
FwCellarLib
);


extern RPC_IF_HANDLE __MIDL_itf_FwCellarTlb_0000_0000_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_FwCellarTlb_0000_0000_v0_0_s_ifspec;


#ifndef __FwCellarLib_LIBRARY_DEFINED__
#define __FwCellarLib_LIBRARY_DEFINED__

/* library FwCellarLib */
/* [helpstring][version][uuid] */

typedef /* [v1_enum] */
enum CellarModuleDefns
	{	kcptNil	= 0,
	kcptMin	= 1,
	kcptBoolean	= 1,
	kcptInteger	= 2,
	kcptNumeric	= 3,
	kcptFloat	= 4,
	kcptTime	= 5,
	kcptGuid	= 6,
	kcptImage	= 7,
	kcptGenDate	= 8,
	kcptBinary	= 9,
	kcptString	= 13,
	kcptMultiString	= 14,
	kcptUnicode	= 15,
	kcptMultiUnicode	= 16,
	kcptBigString	= 17,
	kcptMultiBigString	= 18,
	kcptBigUnicode	= 19,
	kcptMultiBigUnicode	= 20,
	kcptMinObj	= 23,
	kcptOwningAtom	= 23,
	kcptReferenceAtom	= 24,
	kcptOwningCollection	= 25,
	kcptReferenceCollection	= 26,
	kcptOwningSequence	= 27,
	kcptReferenceSequence	= 28,
	kcptLim	= 29,
	kcptVirtual	= 32,
	kfcptOwningAtom	= 8388608,
	kfcptReferenceAtom	= 16777216,
	kfcptOwningCollection	= 33554432,
	kfcptReferenceCollection	= 67108864,
	kfcptOwningSequence	= 134217728,
	kfcptReferenceSequence	= 268435456,
	kgrfcptOwning	= 176160768,
	kgrfcptReference	= 352321536,
	kgrfcptAll	= 528482304,
	kwsAnal	= 0xffffffff,
	kwsVern	= 0xfffffffe,
	kwsAnals	= 0xfffffffd,
	kwsVerns	= 0xfffffffc,
	kwsAnalVerns	= 0xfffffffb,
	kwsVernAnals	= 0xfffffffa,
	kwsLim	= 0xfffffff9,
	kmidCellar	= 0,
	kclidCmAgent	= 23,
	kflidCmAgent_Name	= 23001,
	kflidCmAgent_StateInformation	= 23002,
	kflidCmAgent_Human	= 23003,
	kflidCmAgent_Notes	= 23004,
	kflidCmAgent_Version	= 23005,
	kflidCmAgent_Evaluations	= 23006,
	kclidCmAgentEvaluation	= 32,
	kflidCmAgentEvaluation_Target	= 32001,
	kflidCmAgentEvaluation_DateCreated	= 32002,
	kflidCmAgentEvaluation_Accepted	= 32003,
	kflidCmAgentEvaluation_Details	= 32004,
	kclidCmAnnotation	= 34,
	kflidCmAnnotation_CompDetails	= 34001,
	kflidCmAnnotation_Comment	= 34002,
	kflidCmAnnotation_AnnotationType	= 34003,
	kflidCmAnnotation_Source	= 34004,
	kflidCmAnnotation_InstanceOf	= 34006,
	kflidCmAnnotation_Text	= 34007,
	kflidCmAnnotation_Features	= 34008,
	kflidCmAnnotation_DateCreated	= 34009,
	kflidCmAnnotation_DateModified	= 34010,
	kclidCmCell	= 11,
	kflidCmCell_Contents	= 11001,
	kclidCmDomainQ	= 67,
	kflidCmDomainQ_Question	= 67001,
	kflidCmDomainQ_ExampleWords	= 67002,
	kflidCmDomainQ_ExampleSentences	= 67003,
	kclidCmFile	= 47,
	kflidCmFile_Name	= 47001,
	kflidCmFile_Description	= 47002,
	kflidCmFile_OriginalPath	= 47003,
	kflidCmFile_InternalPath	= 47004,
	kflidCmFile_Copyright	= 47005,
	kclidCmFilter	= 9,
	kflidCmFilter_Name	= 9001,
	kflidCmFilter_ClassId	= 9002,
	kflidCmFilter_FieldId	= 9003,
	kflidCmFilter_FieldInfo	= 9004,
	kflidCmFilter_App	= 9005,
	kflidCmFilter_Type	= 9006,
	kflidCmFilter_Rows	= 9007,
	kflidCmFilter_ColumnInfo	= 9008,
	kflidCmFilter_ShowPrompt	= 9009,
	kflidCmFilter_PromptText	= 9010,
	kclidCmFolder	= 2,
	kflidCmFolder_Name	= 2001,
	kflidCmFolder_SubFolders	= 2003,
	kflidCmFolder_Description	= 2005,
	kflidCmFolder_Files	= 2006,
	kclidCmMajorObject	= 5,
	kflidCmMajorObject_Name	= 5001,
	kflidCmMajorObject_DateCreated	= 5002,
	kflidCmMajorObject_DateModified	= 5003,
	kflidCmMajorObject_Description	= 5004,
	kflidCmMajorObject_Publications	= 5005,
	kflidCmMajorObject_HeaderFooterSets	= 5006,
	kclidCmMedia	= 69,
	kflidCmMedia_Label	= 69001,
	kflidCmMedia_MediaFile	= 69002,
	kclidCmOverlay	= 21,
	kflidCmOverlay_Name	= 21001,
	kflidCmOverlay_PossList	= 21002,
	kflidCmOverlay_PossItems	= 21004,
	kclidCmPicture	= 48,
	kflidCmPicture_PictureFile	= 48002,
	kflidCmPicture_Caption	= 48003,
	kflidCmPicture_Description	= 48004,
	kflidCmPicture_LayoutPos	= 48005,
	kflidCmPicture_ScaleFactor	= 48006,
	kflidCmPicture_LocationRangeType	= 48007,
	kflidCmPicture_LocationMin	= 48008,
	kflidCmPicture_LocationMax	= 48009,
	kclidCmPossibility	= 7,
	kflidCmPossibility_Name	= 7001,
	kflidCmPossibility_Abbreviation	= 7002,
	kflidCmPossibility_Description	= 7003,
	kflidCmPossibility_SubPossibilities	= 7004,
	kflidCmPossibility_SortSpec	= 7006,
	kflidCmPossibility_Restrictions	= 7007,
	kflidCmPossibility_Confidence	= 7008,
	kflidCmPossibility_Status	= 7009,
	kflidCmPossibility_DateCreated	= 7010,
	kflidCmPossibility_DateModified	= 7011,
	kflidCmPossibility_Discussion	= 7012,
	kflidCmPossibility_Researchers	= 7013,
	kflidCmPossibility_HelpId	= 7014,
	kflidCmPossibility_ForeColor	= 7015,
	kflidCmPossibility_BackColor	= 7016,
	kflidCmPossibility_UnderColor	= 7017,
	kflidCmPossibility_UnderStyle	= 7018,
	kflidCmPossibility_Hidden	= 7019,
	kflidCmPossibility_IsProtected	= 7020,
	kclidCmProject	= 1,
	kflidCmProject_Name	= 1001,
	kflidCmProject_DateCreated	= 1002,
	kflidCmProject_DateModified	= 1004,
	kflidCmProject_Description	= 1005,
	kclidCmResource	= 70,
	kflidCmResource_Name	= 70001,
	kflidCmResource_Version	= 70002,
	kclidCmRow	= 10,
	kflidCmRow_Cells	= 10001,
	kclidCmSortSpec	= 31,
	kflidCmSortSpec_Name	= 31001,
	kflidCmSortSpec_App	= 31002,
	kflidCmSortSpec_ClassId	= 31003,
	kflidCmSortSpec_PrimaryField	= 31004,
	kflidCmSortSpec_PrimaryCollType	= 31007,
	kflidCmSortSpec_PrimaryReverse	= 31009,
	kflidCmSortSpec_SecondaryField	= 31010,
	kflidCmSortSpec_SecondaryCollType	= 31013,
	kflidCmSortSpec_SecondaryReverse	= 31015,
	kflidCmSortSpec_TertiaryField	= 31016,
	kflidCmSortSpec_TertiaryCollType	= 31019,
	kflidCmSortSpec_TertiaryReverse	= 31021,
	kflidCmSortSpec_IncludeSubentries	= 31022,
	kflidCmSortSpec_PrimaryWs	= 31023,
	kflidCmSortSpec_SecondaryWs	= 31024,
	kflidCmSortSpec_TertiaryWs	= 31025,
	kflidCmSortSpec_PrimaryCollation	= 31026,
	kflidCmSortSpec_SecondaryCollation	= 31027,
	kflidCmSortSpec_TertiaryCollation	= 31028,
	kclidCmTranslation	= 29,
	kflidCmTranslation_Translation	= 29001,
	kflidCmTranslation_Type	= 29002,
	kflidCmTranslation_Status	= 29003,
	kclidCrossReference	= 28,
	kflidCrossReference_Comment	= 28001,
	kclidFsAbstractStructure	= 60,
	kclidFsFeatDefn	= 55,
	kflidFsFeatDefn_Name	= 55001,
	kflidFsFeatDefn_Abbreviation	= 55002,
	kflidFsFeatDefn_Description	= 55003,
	kflidFsFeatDefn_Default	= 55004,
	kflidFsFeatDefn_GlossAbbreviation	= 55005,
	kflidFsFeatDefn_RightGlossSep	= 55006,
	kflidFsFeatDefn_ShowInGloss	= 55007,
	kflidFsFeatDefn_DisplayToRightOfValues	= 55008,
	kflidFsFeatDefn_CatalogSourceId	= 55009,
	kclidFsFeatStrucType	= 59,
	kflidFsFeatStrucType_Name	= 59001,
	kflidFsFeatStrucType_Abbreviation	= 59002,
	kflidFsFeatStrucType_Description	= 59003,
	kflidFsFeatStrucType_Features	= 59004,
	kflidFsFeatStrucType_CatalogSourceId	= 59005,
	kclidFsFeatureSpecification	= 56,
	kflidFsFeatureSpecification_RefNumber	= 56001,
	kflidFsFeatureSpecification_ValueState	= 56002,
	kflidFsFeatureSpecification_Feature	= 56003,
	kclidFsFeatureSystem	= 49,
	kflidFsFeatureSystem_Types	= 49002,
	kflidFsFeatureSystem_Features	= 49003,
	kclidFsSymFeatVal	= 65,
	kflidFsSymFeatVal_Name	= 65001,
	kflidFsSymFeatVal_Abbreviation	= 65002,
	kflidFsSymFeatVal_Description	= 65003,
	kflidFsSymFeatVal_GlossAbbreviation	= 65004,
	kflidFsSymFeatVal_RightGlossSep	= 65005,
	kflidFsSymFeatVal_ShowInGloss	= 65006,
	kflidFsSymFeatVal_CatalogSourceId	= 65007,
	kclidLgCollation	= 30,
	kflidLgCollation_Name	= 30001,
	kflidLgCollation_WinLCID	= 30002,
	kflidLgCollation_WinCollation	= 30003,
	kflidLgCollation_IcuResourceName	= 30004,
	kflidLgCollation_IcuResourceText	= 30005,
	kflidLgCollation_ICURules	= 30007,
	kclidLgWritingSystem	= 24,
	kflidLgWritingSystem_Name	= 24001,
	kflidLgWritingSystem_Locale	= 24003,
	kflidLgWritingSystem_Abbr	= 24006,
	kflidLgWritingSystem_DefaultMonospace	= 24009,
	kflidLgWritingSystem_DefaultSansSerif	= 24010,
	kflidLgWritingSystem_DefaultSerif	= 24011,
	kflidLgWritingSystem_FontVariation	= 24012,
	kflidLgWritingSystem_KeyboardType	= 24013,
	kflidLgWritingSystem_RightToLeft	= 24015,
	kflidLgWritingSystem_Collations	= 24018,
	kflidLgWritingSystem_Description	= 24020,
	kflidLgWritingSystem_ICULocale	= 24021,
	kflidLgWritingSystem_KeymanKeyboard	= 24022,
	kflidLgWritingSystem_LegacyMapping	= 24023,
	kflidLgWritingSystem_SansFontVariation	= 24024,
	kflidLgWritingSystem_LastModified	= 24025,
	kflidLgWritingSystem_DefaultBodyFont	= 24026,
	kflidLgWritingSystem_BodyFontFeatures	= 24027,
	kflidLgWritingSystem_ValidChars	= 24028,
	kflidLgWritingSystem_SpellCheckDictionary	= 24029,
	kflidLgWritingSystem_MatchedPairs	= 24030,
	kflidLgWritingSystem_PunctuationPatterns	= 24031,
	kflidLgWritingSystem_CapitalizationInfo	= 24032,
	kflidLgWritingSystem_QuotationMarks	= 24033,
	kclidPubDivision	= 43,
	kflidPubDivision_DifferentFirstHF	= 43001,
	kflidPubDivision_DifferentEvenHF	= 43002,
	kflidPubDivision_StartAt	= 43003,
	kflidPubDivision_PageLayout	= 43004,
	kflidPubDivision_HFSet	= 43005,
	kflidPubDivision_NumColumns	= 43006,
	kclidPubHeader	= 46,
	kflidPubHeader_InsideAlignedText	= 46001,
	kflidPubHeader_CenteredText	= 46002,
	kflidPubHeader_OutsideAlignedText	= 46003,
	kclidPubHFSet	= 45,
	kflidPubHFSet_Name	= 45001,
	kflidPubHFSet_Description	= 45002,
	kflidPubHFSet_DefaultHeader	= 45003,
	kflidPubHFSet_DefaultFooter	= 45004,
	kflidPubHFSet_FirstHeader	= 45005,
	kflidPubHFSet_FirstFooter	= 45006,
	kflidPubHFSet_EvenHeader	= 45007,
	kflidPubHFSet_EvenFooter	= 45008,
	kclidPublication	= 42,
	kflidPublication_Name	= 42001,
	kflidPublication_Description	= 42002,
	kflidPublication_PageHeight	= 42003,
	kflidPublication_PageWidth	= 42004,
	kflidPublication_IsLandscape	= 42005,
	kflidPublication_GutterMargin	= 42006,
	kflidPublication_GutterLoc	= 42007,
	kflidPublication_Divisions	= 42008,
	kflidPublication_FootnoteSepWidth	= 42009,
	kflidPublication_PaperHeight	= 42010,
	kflidPublication_PaperWidth	= 42011,
	kflidPublication_BindingEdge	= 42012,
	kflidPublication_SheetLayout	= 42013,
	kflidPublication_SheetsPerSig	= 42014,
	kflidPublication_BaseFontSize	= 42015,
	kflidPublication_BaseLineSpacing	= 42016,
	kclidPubPageLayout	= 44,
	kflidPubPageLayout_Name	= 44001,
	kflidPubPageLayout_Description	= 44002,
	kflidPubPageLayout_MarginTop	= 44003,
	kflidPubPageLayout_MarginBottom	= 44004,
	kflidPubPageLayout_MarginInside	= 44005,
	kflidPubPageLayout_MarginOutside	= 44006,
	kflidPubPageLayout_PosHeader	= 44007,
	kflidPubPageLayout_PosFooter	= 44008,
	kflidPubPageLayout_IsBuiltIn	= 44010,
	kflidPubPageLayout_IsModified	= 44011,
	kclidStPara	= 15,
	kflidStPara_StyleName	= 15001,
	kflidStPara_StyleRules	= 15002,
	kclidStStyle	= 17,
	kflidStStyle_Name	= 17001,
	kflidStStyle_BasedOn	= 17002,
	kflidStStyle_Next	= 17003,
	kflidStStyle_Type	= 17004,
	kflidStStyle_Rules	= 17005,
	kflidStStyle_IsPublishedTextStyle	= 17006,
	kflidStStyle_IsBuiltIn	= 17007,
	kflidStStyle_IsModified	= 17008,
	kflidStStyle_UserLevel	= 17009,
	kflidStStyle_Context	= 17011,
	kflidStStyle_Structure	= 17012,
	kflidStStyle_Function	= 17013,
	kflidStStyle_Usage	= 17014,
	kclidStText	= 14,
	kflidStText_Paragraphs	= 14001,
	kflidStText_RightToLeft	= 14002,
	kclidUserAppFeatAct	= 41,
	kflidUserAppFeatAct_UserConfigAcct	= 41001,
	kflidUserAppFeatAct_ApplicationId	= 41002,
	kflidUserAppFeatAct_FeatureId	= 41003,
	kflidUserAppFeatAct_ActivatedLevel	= 41004,
	kclidUserConfigAcct	= 40,
	kflidUserConfigAcct_Sid	= 40001,
	kflidUserConfigAcct_UserLevel	= 40002,
	kflidUserConfigAcct_HasMaintenance	= 40003,
	kclidCmAnnotationDefn	= 35,
	kflidCmAnnotationDefn_AllowsComment	= 35003,
	kflidCmAnnotationDefn_AllowsFeatureStructure	= 35004,
	kflidCmAnnotationDefn_AllowsInstanceOf	= 35005,
	kflidCmAnnotationDefn_InstanceOfSignature	= 35006,
	kflidCmAnnotationDefn_UserCanCreate	= 35007,
	kflidCmAnnotationDefn_CanCreateOrphan	= 35008,
	kflidCmAnnotationDefn_PromptUser	= 35009,
	kflidCmAnnotationDefn_CopyCutPastable	= 35010,
	kflidCmAnnotationDefn_ZeroWidth	= 35011,
	kflidCmAnnotationDefn_Multi	= 35012,
	kflidCmAnnotationDefn_Severity	= 35013,
	kflidCmAnnotationDefn_MaxDupOccur	= 35014,
	kclidCmAnthroItem	= 26,
	kclidCmBaseAnnotation	= 37,
	kflidCmBaseAnnotation_BeginOffset	= 37001,
	kflidCmBaseAnnotation_Flid	= 37003,
	kflidCmBaseAnnotation_EndOffset	= 37004,
	kflidCmBaseAnnotation_BeginObject	= 37005,
	kflidCmBaseAnnotation_EndObject	= 37006,
	kflidCmBaseAnnotation_OtherObjects	= 37007,
	kflidCmBaseAnnotation_WritingSystem	= 37008,
	kflidCmBaseAnnotation_WsSelector	= 37009,
	kflidCmBaseAnnotation_BeginRef	= 37010,
	kflidCmBaseAnnotation_EndRef	= 37011,
	kclidCmCustomItem	= 27,
	kclidCmIndirectAnnotation	= 36,
	kflidCmIndirectAnnotation_AppliesTo	= 36001,
	kclidCmLocation	= 12,
	kflidCmLocation_Alias	= 12001,
	kclidCmMediaAnnotation	= 38,
	kclidCmPerson	= 13,
	kflidCmPerson_Alias	= 13001,
	kflidCmPerson_Gender	= 13003,
	kflidCmPerson_DateOfBirth	= 13004,
	kflidCmPerson_PlaceOfBirth	= 13006,
	kflidCmPerson_IsResearcher	= 13008,
	kflidCmPerson_PlacesOfResidence	= 13009,
	kflidCmPerson_Education	= 13010,
	kflidCmPerson_DateOfDeath	= 13011,
	kflidCmPerson_Positions	= 13013,
	kclidCmPossibilityList	= 8,
	kflidCmPossibilityList_Depth	= 8002,
	kflidCmPossibilityList_PreventChoiceAboveLevel	= 8003,
	kflidCmPossibilityList_IsSorted	= 8004,
	kflidCmPossibilityList_IsClosed	= 8005,
	kflidCmPossibilityList_PreventDuplicates	= 8006,
	kflidCmPossibilityList_PreventNodeChoices	= 8007,
	kflidCmPossibilityList_Possibilities	= 8008,
	kflidCmPossibilityList_Abbreviation	= 8010,
	kflidCmPossibilityList_HelpFile	= 8011,
	kflidCmPossibilityList_UseExtendedFields	= 8012,
	kflidCmPossibilityList_DisplayOption	= 8013,
	kflidCmPossibilityList_ItemClsid	= 8014,
	kflidCmPossibilityList_IsVernacular	= 8015,
	kflidCmPossibilityList_WritingSystem	= 8017,
	kflidCmPossibilityList_WsSelector	= 8018,
	kflidCmPossibilityList_ListVersion	= 8021,
	kclidCmSemanticDomain	= 66,
	kflidCmSemanticDomain_LouwNidaCodes	= 66001,
	kflidCmSemanticDomain_OcmCodes	= 66002,
	kflidCmSemanticDomain_OcmRefs	= 66003,
	kflidCmSemanticDomain_RelatedDomains	= 66004,
	kflidCmSemanticDomain_Questions	= 66005,
	kclidFsClosedFeature	= 50,
	kflidFsClosedFeature_Values	= 50001,
	kclidFsClosedValue	= 51,
	kflidFsClosedValue_Value	= 51001,
	kclidFsComplexFeature	= 4,
	kflidFsComplexFeature_Type	= 4001,
	kclidFsComplexValue	= 53,
	kflidFsComplexValue_Value	= 53001,
	kclidFsDisjunctiveValue	= 54,
	kflidFsDisjunctiveValue_Value	= 54001,
	kclidFsFeatStruc	= 57,
	kflidFsFeatStruc_FeatureDisjunctions	= 57001,
	kflidFsFeatStruc_FeatureSpecs	= 57002,
	kflidFsFeatStruc_Type	= 57003,
	kclidFsFeatStrucDisj	= 58,
	kflidFsFeatStrucDisj_Contents	= 58001,
	kclidFsNegatedValue	= 61,
	kflidFsNegatedValue_Value	= 61001,
	kclidFsOpenFeature	= 62,
	kflidFsOpenFeature_WritingSystem	= 62002,
	kflidFsOpenFeature_WsSelector	= 62003,
	kclidFsOpenValue	= 63,
	kflidFsOpenValue_Value	= 63001,
	kclidFsSharedValue	= 64,
	kflidFsSharedValue_Value	= 64001,
	kclidStFootnote	= 39,
	kflidStFootnote_FootnoteMarker	= 39001,
	kflidStFootnote_DisplayFootnoteReference	= 39002,
	kflidStFootnote_DisplayFootnoteMarker	= 39003,
	kclidStJournalText	= 68,
	kflidStJournalText_DateCreated	= 68001,
	kflidStJournalText_DateModified	= 68002,
	kflidStJournalText_CreatedBy	= 68003,
	kflidStJournalText_ModifiedBy	= 68004,
	kclidStTxtPara	= 16,
	kflidStTxtPara_Label	= 16001,
	kflidStTxtPara_Contents	= 16002,
	kflidStTxtPara_TextObjects	= 16004,
	kflidStTxtPara_AnalyzedTextObjects	= 16005,
	kflidStTxtPara_ObjRefs	= 16006,
	kflidStTxtPara_Translations	= 16008,
	kflidStartDummyFlids	= 1000000000
	} 	CellarModuleDefns;

typedef
enum CmObjectFields
	{	kflidCmObject_Id	= 100,
	kflidCmObject_Guid	= ( kflidCmObject_Id + 1 ) ,
	kflidCmObject_Class	= ( kflidCmObject_Guid + 1 ) ,
	kflidCmObject_Owner	= ( kflidCmObject_Class + 1 ) ,
	kflidCmObject_OwnFlid	= ( kflidCmObject_Owner + 1 ) ,
	kflidCmObject_OwnOrd	= ( kflidCmObject_OwnFlid + 1 )
	} 	CmObjectFields;


#define LIBID_FwCellarLib __uuidof(FwCellarLib)
#endif /* __FwCellarLib_LIBRARY_DEFINED__ */

/* Additional Prototypes for ALL interfaces */

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif
