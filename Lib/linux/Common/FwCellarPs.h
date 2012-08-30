

/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 6.00.0361 */
/* at Mon May 29 12:01:57 2006
 */
/* Compiler settings for C:\fw\Output\Common\FwCellarPs.idl:
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

#ifndef COM_NO_WINDOWS_H
#include "windows.h"
#include "ole2.h"
#endif /*COM_NO_WINDOWS_H*/

#ifndef __FwCellarPs_h__
#define __FwCellarPs_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */

#ifndef __IFwXmlData_FWD_DEFINED__
#define __IFwXmlData_FWD_DEFINED__
typedef interface IFwXmlData IFwXmlData;
#endif 	/* __IFwXmlData_FWD_DEFINED__ */


#ifndef __IFwXmlData2_FWD_DEFINED__
#define __IFwXmlData2_FWD_DEFINED__
typedef interface IFwXmlData2 IFwXmlData2;
#endif 	/* __IFwXmlData2_FWD_DEFINED__ */


/* header files for imported files */
#include "oaidl.h"
#include "ocidl.h"
#include "FwKernelPs.h"

#ifdef __cplusplus
extern "C"{
#endif

void * __RPC_USER MIDL_user_allocate(size_t);
void __RPC_USER MIDL_user_free( void * );

/* interface __MIDL_itf_FwCellarPs_0000 */
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
	kclidCmDomainQuestion	= 67,
	kflidCmDomainQuestion_Question	= 67001,
	kflidCmDomainQuestion_ExampleWords	= 67002,
	kflidCmDomainQuestion_ExampleSentences	= 67003,
	kclidCmFile	= 47,
	kflidCmFile_Name	= 47001,
	kflidCmFile_Description	= 47002,
	kflidCmFile_OriginalPath	= 47003,
	kflidCmFile_InternalPath	= 47004,
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
	kclidCmOverlay	= 21,
	kflidCmOverlay_Name	= 21001,
	kflidCmOverlay_PossList	= 21002,
	kflidCmOverlay_PossItems	= 21004,
	kclidCmPicture	= 48,
	kflidCmPicture_Caption	= 48001,
	kflidCmPicture_PictureFile	= 48002,
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
	kclidFsFeatureDefn	= 55,
	kflidFsFeatureDefn_Name	= 55001,
	kflidFsFeatureDefn_Abbreviation	= 55002,
	kflidFsFeatureDefn_Description	= 55003,
	kflidFsFeatureDefn_Default	= 55004,
	kflidFsFeatureDefn_GlossAbbreviation	= 55005,
	kflidFsFeatureDefn_RightGlossSeparator	= 55006,
	kflidFsFeatureDefn_ShowInGloss	= 55007,
	kflidFsFeatureDefn_DisplayToRightOfValues	= 55008,
	kclidFsFeatureSpecification	= 56,
	kflidFsFeatureSpecification_RefNumber	= 56001,
	kflidFsFeatureSpecification_ValueState	= 56002,
	kflidFsFeatureSpecification_Feature	= 56003,
	kclidFsFeatureStructureType	= 59,
	kflidFsFeatureStructureType_Name	= 59001,
	kflidFsFeatureStructureType_Abbreviation	= 59002,
	kflidFsFeatureStructureType_Description	= 59003,
	kflidFsFeatureStructureType_Features	= 59004,
	kclidFsFeatureSystem	= 49,
	kflidFsFeatureSystem_Types	= 49002,
	kflidFsFeatureSystem_Features	= 49003,
	kclidFsSymbolicFeatureValue	= 65,
	kflidFsSymbolicFeatureValue_Name	= 65001,
	kflidFsSymbolicFeatureValue_Abbreviation	= 65002,
	kflidFsSymbolicFeatureValue_Description	= 65003,
	kflidFsSymbolicFeatureValue_GlossAbbreviation	= 65004,
	kflidFsSymbolicFeatureValue_RightGlossSeparator	= 65005,
	kflidFsSymbolicFeatureValue_ShowInGloss	= 65006,
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
	kclidPubDivision	= 43,
	kflidPubDivision_DifferentFirstHF	= 43001,
	kflidPubDivision_DifferentEvenHF	= 43002,
	kflidPubDivision_StartAt	= 43003,
	kflidPubDivision_PageLayout	= 43004,
	kflidPubDivision_HeaderFooterSettings	= 43005,
	kclidPubHeader	= 46,
	kflidPubHeader_InsideAlignedText	= 46001,
	kflidPubHeader_CenteredText	= 46002,
	kflidPubHeader_OutsideAlignedText	= 46003,
	kclidPubHeaderFooterSet	= 45,
	kflidPubHeaderFooterSet_Name	= 45001,
	kflidPubHeaderFooterSet_Description	= 45002,
	kflidPubHeaderFooterSet_DefaultHeader	= 45003,
	kflidPubHeaderFooterSet_DefaultFooter	= 45004,
	kflidPubHeaderFooterSet_FirstHeader	= 45005,
	kflidPubHeaderFooterSet_FirstFooter	= 45006,
	kflidPubHeaderFooterSet_EvenHeader	= 45007,
	kflidPubHeaderFooterSet_EvenFooter	= 45008,
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
	kclidPubPageLayout	= 44,
	kflidPubPageLayout_Name	= 44001,
	kflidPubPageLayout_Description	= 44002,
	kflidPubPageLayout_MarginTop	= 44003,
	kflidPubPageLayout_MarginBottom	= 44004,
	kflidPubPageLayout_MarginInside	= 44005,
	kflidPubPageLayout_MarginOutside	= 44006,
	kflidPubPageLayout_PosHeader	= 44007,
	kflidPubPageLayout_PosFooter	= 44008,
	kflidPubPageLayout_MaxPosFootnote	= 44009,
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
	kclidUserAppFeatureActivated	= 41,
	kflidUserAppFeatureActivated_UserConfigAccount	= 41001,
	kflidUserAppFeatureActivated_ApplicationId	= 41002,
	kflidUserAppFeatureActivated_FeatureId	= 41003,
	kflidUserAppFeatureActivated_ActivatedLevel	= 41004,
	kclidUserConfigAccount	= 40,
	kflidUserConfigAccount_Sid	= 40001,
	kflidUserConfigAccount_UserLevel	= 40002,
	kflidUserConfigAccount_HasMaintenance	= 40003,
	kclidUserView	= 18,
	kflidUserView_Name	= 18001,
	kflidUserView_Type	= 18002,
	kflidUserView_App	= 18003,
	kflidUserView_Records	= 18004,
	kflidUserView_Details	= 18005,
	kflidUserView_System	= 18006,
	kflidUserView_SubType	= 18007,
	kclidUserViewField	= 20,
	kflidUserViewField_Label	= 20001,
	kflidUserViewField_HelpString	= 20002,
	kflidUserViewField_Type	= 20003,
	kflidUserViewField_Flid	= 20004,
	kflidUserViewField_Visibility	= 20005,
	kflidUserViewField_Required	= 20006,
	kflidUserViewField_Style	= 20007,
	kflidUserViewField_SubfieldOf	= 20008,
	kflidUserViewField_Details	= 20009,
	kflidUserViewField_IsCustomField	= 20011,
	kflidUserViewField_PossList	= 20012,
	kflidUserViewField_WritingSystem	= 20013,
	kflidUserViewField_WsSelector	= 20014,
	kclidUserViewRec	= 19,
	kflidUserViewRec_Clsid	= 19001,
	kflidUserViewRec_Level	= 19002,
	kflidUserViewRec_Fields	= 19003,
	kflidUserViewRec_Details	= 19004,
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
	kclidFsFeatureStructure	= 57,
	kflidFsFeatureStructure_FeatureDisjunctions	= 57001,
	kflidFsFeatureStructure_FeatureSpecs	= 57002,
	kflidFsFeatureStructure_Type	= 57003,
	kclidFsFeatureStructureDisjunction	= 58,
	kflidFsFeatureStructureDisjunction_Contents	= 58001,
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
	kflidCmObject_Guid	= kflidCmObject_Id + 1,
	kflidCmObject_Class	= kflidCmObject_Guid + 1,
	kflidCmObject_Owner	= kflidCmObject_Class + 1,
	kflidCmObject_OwnFlid	= kflidCmObject_Owner + 1,
	kflidCmObject_OwnOrd	= kflidCmObject_OwnFlid + 1
	} 	CmObjectFields;

GENERIC_DECLARE_SMART_INTERFACE_PTR(
IFwXmlData
,
65BAE1A5-1B75-4127-841E-0228F908727D
);


extern RPC_IF_HANDLE __MIDL_itf_FwCellarPs_0000_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_FwCellarPs_0000_v0_0_s_ifspec;

#ifndef __IFwXmlData_INTERFACE_DEFINED__
#define __IFwXmlData_INTERFACE_DEFINED__

/* interface IFwXmlData */
/* [unique][object][uuid] */


#define IID_IFwXmlData __uuidof(IFwXmlData)

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("65BAE1A5-1B75-4127-841E-0228F908727D")
	IFwXmlData : public IUnknown
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE Open(
			/* [in] */ BSTR bstrServer,
			/* [in] */ BSTR bstrDatabase) = 0;

		virtual HRESULT STDMETHODCALLTYPE Close( void) = 0;

		virtual HRESULT STDMETHODCALLTYPE LoadXml(
			/* [in] */ BSTR bstrFile,
			/* [in] */ IAdvInd *padvi) = 0;

		virtual HRESULT STDMETHODCALLTYPE SaveXml(
			/* [in] */ BSTR bstrFile,
			/* [in] */ ILgWritingSystemFactory *pwsf,
			/* [in] */ IAdvInd *padvi) = 0;

	};

#else 	/* C style interface */

	typedef struct IFwXmlDataVtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IFwXmlData * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IFwXmlData * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IFwXmlData * This);

		HRESULT ( STDMETHODCALLTYPE *Open )(
			IFwXmlData * This,
			/* [in] */ BSTR bstrServer,
			/* [in] */ BSTR bstrDatabase);

		HRESULT ( STDMETHODCALLTYPE *Close )(
			IFwXmlData * This);

		HRESULT ( STDMETHODCALLTYPE *LoadXml )(
			IFwXmlData * This,
			/* [in] */ BSTR bstrFile,
			/* [in] */ IAdvInd *padvi);

		HRESULT ( STDMETHODCALLTYPE *SaveXml )(
			IFwXmlData * This,
			/* [in] */ BSTR bstrFile,
			/* [in] */ ILgWritingSystemFactory *pwsf,
			/* [in] */ IAdvInd *padvi);

		END_INTERFACE
	} IFwXmlDataVtbl;

	interface IFwXmlData
	{
		CONST_VTBL struct IFwXmlDataVtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IFwXmlData_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IFwXmlData_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IFwXmlData_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IFwXmlData_Open(This,bstrServer,bstrDatabase)	\
	(This)->lpVtbl -> Open(This,bstrServer,bstrDatabase)

#define IFwXmlData_Close(This)	\
	(This)->lpVtbl -> Close(This)

#define IFwXmlData_LoadXml(This,bstrFile,padvi)	\
	(This)->lpVtbl -> LoadXml(This,bstrFile,padvi)

#define IFwXmlData_SaveXml(This,bstrFile,pwsf,padvi)	\
	(This)->lpVtbl -> SaveXml(This,bstrFile,pwsf,padvi)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IFwXmlData_Open_Proxy(
	IFwXmlData * This,
	/* [in] */ BSTR bstrServer,
	/* [in] */ BSTR bstrDatabase);


void __RPC_STUB IFwXmlData_Open_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwXmlData_Close_Proxy(
	IFwXmlData * This);


void __RPC_STUB IFwXmlData_Close_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwXmlData_LoadXml_Proxy(
	IFwXmlData * This,
	/* [in] */ BSTR bstrFile,
	/* [in] */ IAdvInd *padvi);


void __RPC_STUB IFwXmlData_LoadXml_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);


HRESULT STDMETHODCALLTYPE IFwXmlData_SaveXml_Proxy(
	IFwXmlData * This,
	/* [in] */ BSTR bstrFile,
	/* [in] */ ILgWritingSystemFactory *pwsf,
	/* [in] */ IAdvInd *padvi);


void __RPC_STUB IFwXmlData_SaveXml_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IFwXmlData_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_FwCellarPs_0327 */
/* [local] */

GENERIC_DECLARE_SMART_INTERFACE_PTR(
IFwXmlData2
,
DE12C0CD-5836-4A4A-9E80-D465B69C703E
);


extern RPC_IF_HANDLE __MIDL_itf_FwCellarPs_0327_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_FwCellarPs_0327_v0_0_s_ifspec;

#ifndef __IFwXmlData2_INTERFACE_DEFINED__
#define __IFwXmlData2_INTERFACE_DEFINED__

/* interface IFwXmlData2 */
/* [unique][object][uuid] */


#define IID_IFwXmlData2 __uuidof(IFwXmlData2)

#if defined(__cplusplus) && !defined(CINTERFACE)

	MIDL_INTERFACE("DE12C0CD-5836-4A4A-9E80-D465B69C703E")
	IFwXmlData2 : public IFwXmlData
	{
	public:
		virtual HRESULT STDMETHODCALLTYPE ImportXmlObject(
			/* [in] */ BSTR bstrFile,
			/* [in] */ int hvoOwner,
			/* [in] */ int flid,
			/* [in] */ IAdvInd *padvi) = 0;

	};

#else 	/* C style interface */

	typedef struct IFwXmlData2Vtbl
	{
		BEGIN_INTERFACE

		HRESULT ( STDMETHODCALLTYPE *QueryInterface )(
			IFwXmlData2 * This,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ void **ppvObject);

		ULONG ( STDMETHODCALLTYPE *AddRef )(
			IFwXmlData2 * This);

		ULONG ( STDMETHODCALLTYPE *Release )(
			IFwXmlData2 * This);

		HRESULT ( STDMETHODCALLTYPE *Open )(
			IFwXmlData2 * This,
			/* [in] */ BSTR bstrServer,
			/* [in] */ BSTR bstrDatabase);

		HRESULT ( STDMETHODCALLTYPE *Close )(
			IFwXmlData2 * This);

		HRESULT ( STDMETHODCALLTYPE *LoadXml )(
			IFwXmlData2 * This,
			/* [in] */ BSTR bstrFile,
			/* [in] */ IAdvInd *padvi);

		HRESULT ( STDMETHODCALLTYPE *SaveXml )(
			IFwXmlData2 * This,
			/* [in] */ BSTR bstrFile,
			/* [in] */ ILgWritingSystemFactory *pwsf,
			/* [in] */ IAdvInd *padvi);

		HRESULT ( STDMETHODCALLTYPE *ImportXmlObject )(
			IFwXmlData2 * This,
			/* [in] */ BSTR bstrFile,
			/* [in] */ int hvoOwner,
			/* [in] */ int flid,
			/* [in] */ IAdvInd *padvi);

		END_INTERFACE
	} IFwXmlData2Vtbl;

	interface IFwXmlData2
	{
		CONST_VTBL struct IFwXmlData2Vtbl *lpVtbl;
	};



#ifdef COBJMACROS


#define IFwXmlData2_QueryInterface(This,riid,ppvObject)	\
	(This)->lpVtbl -> QueryInterface(This,riid,ppvObject)

#define IFwXmlData2_AddRef(This)	\
	(This)->lpVtbl -> AddRef(This)

#define IFwXmlData2_Release(This)	\
	(This)->lpVtbl -> Release(This)


#define IFwXmlData2_Open(This,bstrServer,bstrDatabase)	\
	(This)->lpVtbl -> Open(This,bstrServer,bstrDatabase)

#define IFwXmlData2_Close(This)	\
	(This)->lpVtbl -> Close(This)

#define IFwXmlData2_LoadXml(This,bstrFile,padvi)	\
	(This)->lpVtbl -> LoadXml(This,bstrFile,padvi)

#define IFwXmlData2_SaveXml(This,bstrFile,pwsf,padvi)	\
	(This)->lpVtbl -> SaveXml(This,bstrFile,pwsf,padvi)


#define IFwXmlData2_ImportXmlObject(This,bstrFile,hvoOwner,flid,padvi)	\
	(This)->lpVtbl -> ImportXmlObject(This,bstrFile,hvoOwner,flid,padvi)

#endif /* COBJMACROS */


#endif 	/* C style interface */



HRESULT STDMETHODCALLTYPE IFwXmlData2_ImportXmlObject_Proxy(
	IFwXmlData2 * This,
	/* [in] */ BSTR bstrFile,
	/* [in] */ int hvoOwner,
	/* [in] */ int flid,
	/* [in] */ IAdvInd *padvi);


void __RPC_STUB IFwXmlData2_ImportXmlObject_Stub(
	IRpcStubBuffer *This,
	IRpcChannelBuffer *_pRpcChannelBuffer,
	PRPC_MESSAGE _pRpcMessage,
	DWORD *_pdwStubPhase);



#endif 	/* __IFwXmlData2_INTERFACE_DEFINED__ */


/* Additional Prototypes for ALL interfaces */

unsigned long             __RPC_USER  BSTR_UserSize(     unsigned long *, unsigned long            , BSTR * );
unsigned char * __RPC_USER  BSTR_UserMarshal(  unsigned long *, unsigned char *, BSTR * );
unsigned char * __RPC_USER  BSTR_UserUnmarshal(unsigned long *, unsigned char *, BSTR * );
void                      __RPC_USER  BSTR_UserFree(     unsigned long *, BSTR * );

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif
