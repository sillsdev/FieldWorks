

/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 8.00.0603 */
/* at Wed Aug 14 08:55:22 2019
 */
/* Compiler settings for C:\fwrepo\fw\Output\Common\FwKernelTlb.idl:
    Oicf, W1, Zp8, env=Win32 (32b run), target_arch=X86 8.00.0603 
    protocol : dce , ms_ext, c_ext, robust
    error checks: allocation ref bounds_check enum stub_data 
    VC __declspec() decoration level: 
         __declspec(uuid()), __declspec(selectany), __declspec(novtable)
         DECLSPEC_UUID(), MIDL_INTERFACE()
*/
/* @@MIDL_FILE_HEADING(  ) */

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


#ifndef __FwKernelTlb_h__
#define __FwKernelTlb_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */ 

#ifndef __IFwMetaDataCache_FWD_DEFINED__
#define __IFwMetaDataCache_FWD_DEFINED__
typedef interface IFwMetaDataCache IFwMetaDataCache;

#endif 	/* __IFwMetaDataCache_FWD_DEFINED__ */


#ifndef __ILgWritingSystemFactory_FWD_DEFINED__
#define __ILgWritingSystemFactory_FWD_DEFINED__
typedef interface ILgWritingSystemFactory ILgWritingSystemFactory;

#endif 	/* __ILgWritingSystemFactory_FWD_DEFINED__ */


#ifndef __ITsMultiString_FWD_DEFINED__
#define __ITsMultiString_FWD_DEFINED__
typedef interface ITsMultiString ITsMultiString;

#endif 	/* __ITsMultiString_FWD_DEFINED__ */


#ifndef __ITsString_FWD_DEFINED__
#define __ITsString_FWD_DEFINED__
typedef interface ITsString ITsString;

#endif 	/* __ITsString_FWD_DEFINED__ */


#ifndef __ITsTextProps_FWD_DEFINED__
#define __ITsTextProps_FWD_DEFINED__
typedef interface ITsTextProps ITsTextProps;

#endif 	/* __ITsTextProps_FWD_DEFINED__ */


#ifndef __IVwStylesheet_FWD_DEFINED__
#define __IVwStylesheet_FWD_DEFINED__
typedef interface IVwStylesheet IVwStylesheet;

#endif 	/* __IVwStylesheet_FWD_DEFINED__ */


#ifndef __ISimpleInit_FWD_DEFINED__
#define __ISimpleInit_FWD_DEFINED__
typedef interface ISimpleInit ISimpleInit;

#endif 	/* __ISimpleInit_FWD_DEFINED__ */


#ifndef __ICheckWord_FWD_DEFINED__
#define __ICheckWord_FWD_DEFINED__
typedef interface ICheckWord ICheckWord;

#endif 	/* __ICheckWord_FWD_DEFINED__ */


#ifndef __IGetSpellChecker_FWD_DEFINED__
#define __IGetSpellChecker_FWD_DEFINED__
typedef interface IGetSpellChecker IGetSpellChecker;

#endif 	/* __IGetSpellChecker_FWD_DEFINED__ */


#ifndef __IVwNotifyChange_FWD_DEFINED__
#define __IVwNotifyChange_FWD_DEFINED__
typedef interface IVwNotifyChange IVwNotifyChange;

#endif 	/* __IVwNotifyChange_FWD_DEFINED__ */


#ifndef __IUndoAction_FWD_DEFINED__
#define __IUndoAction_FWD_DEFINED__
typedef interface IUndoAction IUndoAction;

#endif 	/* __IUndoAction_FWD_DEFINED__ */


#ifndef __IActionHandler_FWD_DEFINED__
#define __IActionHandler_FWD_DEFINED__
typedef interface IActionHandler IActionHandler;

#endif 	/* __IActionHandler_FWD_DEFINED__ */


#ifndef __ActionHandler_FWD_DEFINED__
#define __ActionHandler_FWD_DEFINED__

#ifdef __cplusplus
typedef class ActionHandler ActionHandler;
#else
typedef struct ActionHandler ActionHandler;
#endif /* __cplusplus */

#endif 	/* __ActionHandler_FWD_DEFINED__ */


#ifndef __ISilDataAccess_FWD_DEFINED__
#define __ISilDataAccess_FWD_DEFINED__
typedef interface ISilDataAccess ISilDataAccess;

#endif 	/* __ISilDataAccess_FWD_DEFINED__ */


#ifndef __IStructuredTextDataAccess_FWD_DEFINED__
#define __IStructuredTextDataAccess_FWD_DEFINED__
typedef interface IStructuredTextDataAccess IStructuredTextDataAccess;

#endif 	/* __IStructuredTextDataAccess_FWD_DEFINED__ */


#ifndef __IDebugReportSink_FWD_DEFINED__
#define __IDebugReportSink_FWD_DEFINED__
typedef interface IDebugReportSink IDebugReportSink;

#endif 	/* __IDebugReportSink_FWD_DEFINED__ */


#ifndef __IDebugReport_FWD_DEFINED__
#define __IDebugReport_FWD_DEFINED__
typedef interface IDebugReport IDebugReport;

#endif 	/* __IDebugReport_FWD_DEFINED__ */


#ifndef __IComDisposable_FWD_DEFINED__
#define __IComDisposable_FWD_DEFINED__
typedef interface IComDisposable IComDisposable;

#endif 	/* __IComDisposable_FWD_DEFINED__ */


#ifndef __ITsStrFactory_FWD_DEFINED__
#define __ITsStrFactory_FWD_DEFINED__
typedef interface ITsStrFactory ITsStrFactory;

#endif 	/* __ITsStrFactory_FWD_DEFINED__ */


#ifndef __ITsPropsFactory_FWD_DEFINED__
#define __ITsPropsFactory_FWD_DEFINED__
typedef interface ITsPropsFactory ITsPropsFactory;

#endif 	/* __ITsPropsFactory_FWD_DEFINED__ */


#ifndef __ITsStrBldr_FWD_DEFINED__
#define __ITsStrBldr_FWD_DEFINED__
typedef interface ITsStrBldr ITsStrBldr;

#endif 	/* __ITsStrBldr_FWD_DEFINED__ */


#ifndef __ITsIncStrBldr_FWD_DEFINED__
#define __ITsIncStrBldr_FWD_DEFINED__
typedef interface ITsIncStrBldr ITsIncStrBldr;

#endif 	/* __ITsIncStrBldr_FWD_DEFINED__ */


#ifndef __ITsPropsBldr_FWD_DEFINED__
#define __ITsPropsBldr_FWD_DEFINED__
typedef interface ITsPropsBldr ITsPropsBldr;

#endif 	/* __ITsPropsBldr_FWD_DEFINED__ */


#ifndef __ILgWritingSystem_FWD_DEFINED__
#define __ILgWritingSystem_FWD_DEFINED__
typedef interface ILgWritingSystem ILgWritingSystem;

#endif 	/* __ILgWritingSystem_FWD_DEFINED__ */


/* header files for imported files */
#include "oaidl.h"
#include "ocidl.h"

#ifdef __cplusplus
extern "C"{
#endif 


/* interface __MIDL_itf_FwKernelTlb_0000_0000 */
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
F1EF76E0-BE04-11d3-8D9A-005004DEFEC4
,
FwKernelLib
);


extern RPC_IF_HANDLE __MIDL_itf_FwKernelTlb_0000_0000_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_FwKernelTlb_0000_0000_v0_0_s_ifspec;


#ifndef __FwKernelLib_LIBRARY_DEFINED__
#define __FwKernelLib_LIBRARY_DEFINED__

/* library FwKernelLib */
/* [helpstring][version][uuid] */ 







typedef int HVO;

typedef int PropTag;

typedef /* [v1_enum] */ 
enum UndoResult
    {
        kuresSuccess	= 0,
        kuresRefresh	= ( kuresSuccess + 1 ) ,
        kuresFailed	= ( kuresRefresh + 1 ) ,
        kuresError	= ( kuresFailed + 1 ) 
    } 	UndoResult;

GENERIC_DECLARE_SMART_INTERFACE_PTR(
ISimpleInit
,
6433D19E-2DA2-4041-B202-DB118EE1694D
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ICheckWord
,
69F4D944-C786-47EC-94F7-15193EED6758
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IGetSpellChecker
,
F0A60670-D280-45EA-A5C5-F0B84C027EFC
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwNotifyChange
,
6C456541-C2B6-11d3-8078-0000C0FB81B5
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IUndoAction
,
B831F535-0D5F-42c8-BF9F-7F5ECA2C4657
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IActionHandler
,
7E8BC421-4CB2-4CF9-8C4C-73A5FD87CA7A
);
ATTACH_GUID_TO_CLASS(class,
CF0F1C0B-0E44-4C1E-9912-2048ED12C2B4
,
ActionHandler
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ISilDataAccess
,
26E6E70E-53EB-4372-96F1-0F4707CCD1EB
);
typedef /* [v1_enum] */ 
enum FieldSource
    {
        kModel	= 0,
        kCustom	= 1,
        kVirtual	= 2
    } 	FieldSource;

GENERIC_DECLARE_SMART_INTERFACE_PTR(
IFwMetaDataCache
,
EDBB1DED-7065-4b56-A262-746453835451
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IStructuredTextDataAccess
,
A2A4F9FA-D4E8-4bfb-B6B7-5F45DAF2DC0C
);
typedef /* [v1_enum] */ 
enum PropChangeType
    {
        kpctNotifyMeThenAll	= 0,
        kpctNotifyAll	= ( kpctNotifyMeThenAll + 1 ) ,
        kpctNotifyAllButMe	= ( kpctNotifyAll + 1 ) 
    } 	PropChangeType;

typedef /* [v1_enum] */ 
enum CrtReportType
    {
        Warn	= 0,
        Error	= 0x1,
        Assert	= 0x2
    } 	CrtReportType;

GENERIC_DECLARE_SMART_INTERFACE_PTR(
IDebugReportSink
,
DD9CE7AD-6ECC-4e0c-BBFC-1DC52E053354
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IDebugReport
,
3D6A0880-D17D-4e4a-9DE9-861A85CA4046
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IComDisposable
,
CA9AAF91-4C34-4c6a-8E07-97C1A7B3486A
);









#if defined(__cplusplus)
const OLECHAR kchObject = 0xFFFC;
#else // !defined(__cplusplus)
#define	kchObject	( 0xfffc )

#endif // !defined(__cplusplus)

typedef /* [v1_enum] */ 
enum FwTextPropVar
    {
        ktpvDefault	= 0,
        ktpvMilliPoint	= 0x1,
        ktpvRelative	= 0x2,
        ktpvEnum	= 0x3,
        ktpvNinch	= 0xf
    } 	FwTextPropVar;

typedef /* [v1_enum] */ 
enum FwNormalizationMode
    {
        knmNone	= 1,
        knmNFD	= 2,
        knmNFKD	= 3,
        knmNFC	= 4,
        knmDefault	= 4,
        knmNFKC	= 5,
        knmFCD	= 6,
        knmNFSC	= 7,
        knmLim	= ( knmNFSC + 1 ) 
    } 	FwNormalizationMode;

typedef /* [v1_enum] */ 
enum FwTextPropType
    {
        ktptWs	= 1,
        ktptItalic	= 2,
        ktptBold	= 3,
        ktptSuperscript	= 4,
        ktptUnderline	= 5,
        ktptFontSize	= 6,
        ktptOffset	= 7,
        ktptForeColor	= 8,
        ktptBackColor	= 9,
        ktptUnderColor	= 10,
        ktptBaseWs	= 16,
        ktptAlign	= 17,
        ktptFirstIndent	= 18,
        ktptLeadingIndent	= 19,
        ktptMarginLeading	= 19,
        ktptTrailingIndent	= 20,
        ktptMarginTrailing	= 20,
        ktptSpaceBefore	= 21,
        ktptMswMarginTop	= 21,
        ktptSpaceAfter	= 22,
        ktptMarginBottom	= 22,
        ktptTabDef	= 23,
        ktptLineHeight	= 24,
        ktptParaColor	= 25,
        ktptSpellCheck	= 26,
        ktptMarginTop	= 50,
        ktptFontFamily	= 1,
        ktptCharStyle	= 2,
        ktptParaStyle	= 3,
        ktptTabList	= 4,
        ktptTags	= 5,
        ktptObjData	= 6,
        ktptCustomBullet	= 7,
        ktptRightToLeft	= 128,
        ktptDirectionDepth	= 129,
        ktptFontVariations	= 130,
        ktptNamedStyle	= 133,
        ktptPadLeading	= 134,
        ktptPadTrailing	= 135,
        ktptPadTop	= 136,
        ktptPadBottom	= 137,
        ktptBorderTop	= 138,
        ktptBorderBottom	= 139,
        ktptBorderLeading	= 140,
        ktptBorderTrailing	= 141,
        ktptBorderColor	= 142,
        ktptBulNumScheme	= 143,
        ktptBulNumStartAt	= 144,
        ktptBulNumTxtBef	= 145,
        ktptBulNumTxtAft	= 146,
        ktptBulNumFontInfo	= 147,
        ktptKeepWithNext	= 148,
        ktptKeepTogether	= 149,
        ktptHyphenate	= 150,
        ktptMaxLines	= 151,
        ktptCellBorderWidth	= 152,
        ktptCellSpacing	= 153,
        ktptCellPadding	= 154,
        ktptEditable	= 155,
        ktptWsStyle	= 156,
        ktptSetRowDefaults	= 159,
        ktptRelLineHeight	= 160,
        ktptTableRule	= 161,
        ktptWidowOrphanControl	= 162,
        ktptFieldName	= 9998,
        ktptMarkItem	= 9999
    } 	FwTextPropType;

typedef /* [v1_enum] */ 
enum TptEditable
    {
        ktptNotEditable	= 0,
        ktptIsEditable	= ( ktptNotEditable + 1 ) ,
        ktptSemiEditable	= ( ktptIsEditable + 1 ) 
    } 	TptEditable;

typedef /* [v1_enum] */ 
enum SpellingModes
    {
        ksmMin	= 0,
        ksmNormalCheck	= 0,
        ksmDoNotCheck	= ( ksmNormalCheck + 1 ) ,
        ksmForceCheck	= ( ksmDoNotCheck + 1 ) ,
        ksmLim	= ( ksmForceCheck + 1 ) 
    } 	SpellingModes;

typedef /* [v1_enum] */ 
enum FwObjDataTypes
    {
        kodtPictEvenHot	= 1,
        kodtPictOddHot	= 2,
        kodtNameGuidHot	= 3,
        kodtExternalPathName	= 4,
        kodtOwnNameGuidHot	= 5,
        kodtEmbeddedObjectData	= 6,
        kodtContextString	= 7,
        kodtGuidMoveableObjDisp	= 8
    } 	FwObjDataTypes;

typedef /* [v1_enum] */ 
enum FwTextScalarProp
    {
        kscpWs	= ( ( ktptWs << 2 )  | 2 ) ,
        kscpWsAndOws	= ( ( ktptWs << 2 )  | 3 ) ,
        kscpItalic	= ( ( ktptItalic << 2 )  | 0 ) ,
        kscpBold	= ( ( ktptBold << 2 )  | 0 ) ,
        kscpSuperscript	= ( ( ktptSuperscript << 2 )  | 0 ) ,
        kscpUnderline	= ( ( ktptUnderline << 2 )  | 0 ) ,
        kscpFontSize	= ( ( ktptFontSize << 2 )  | 2 ) ,
        kscpOffset	= ( ( ktptOffset << 2 )  | 2 ) ,
        kscpForeColor	= ( ( ktptForeColor << 2 )  | 2 ) ,
        kscpBackColor	= ( ( ktptBackColor << 2 )  | 2 ) ,
        kscpUnderColor	= ( ( ktptUnderColor << 2 )  | 2 ) ,
        kscpSpellCheck	= ( ( ktptSpellCheck << 2 )  | 0 ) ,
        kscpBaseWs	= ( ( ktptBaseWs << 2 )  | 2 ) ,
        kscpBaseWsAndOws	= ( ( ktptBaseWs << 2 )  | 3 ) ,
        kscpAlign	= ( ( ktptAlign << 2 )  | 0 ) ,
        kscpFirstIndent	= ( ( ktptFirstIndent << 2 )  | 2 ) ,
        kscpLeadingIndent	= ( ( ktptLeadingIndent << 2 )  | 2 ) ,
        kscpTrailingIndent	= ( ( ktptTrailingIndent << 2 )  | 2 ) ,
        kscpSpaceBefore	= ( ( ktptSpaceBefore << 2 )  | 2 ) ,
        kscpSpaceAfter	= ( ( ktptSpaceAfter << 2 )  | 2 ) ,
        kscpTabDef	= ( ( ktptTabDef << 2 )  | 2 ) ,
        kscpLineHeight	= ( ( ktptLineHeight << 2 )  | 2 ) ,
        kscpParaColor	= ( ( ktptParaColor << 2 )  | 2 ) ,
        kscpKeepWithNext	= ( ( ktptKeepWithNext << 2 )  | 0 ) ,
        kscpKeepTogether	= ( ( ktptKeepTogether << 2 )  | 0 ) ,
        kscpWidowOrphanControl	= ( ( ktptWidowOrphanControl << 2 )  | 0 ) ,
        kscpMarkItem	= ( ( ktptMarkItem << 2 )  | 0 ) 
    } 	FwTextScalarProp;

typedef /* [v1_enum] */ 
enum FwTextStringProp
    {
        kstpFontFamily	= ktptFontFamily,
        kstpCharStyle	= ktptCharStyle,
        kstpParaStyle	= ktptParaStyle,
        kstpTabList	= ktptTabList,
        kstpTags	= ktptTags,
        kstpObjData	= ktptObjData,
        kstpFontVariations	= ktptFontVariations,
        kstpNamedStyle	= ktptNamedStyle,
        kstpBulNumTxtBef	= ktptBulNumTxtBef,
        kstpBulNumTxtAft	= ktptBulNumTxtAft,
        kstpBulNumFontInfo	= ktptBulNumFontInfo,
        kstpWsStyle	= ktptWsStyle,
        kstpFieldName	= ktptFieldName
    } 	FwTextStringProp;

typedef /* [v1_enum] */ 
enum FwTextPropConstants
    {
        kdenTextPropRel	= 10000,
        kcbitTextPropVar	= 4,
        knNinch	= 0x80000000,
        knConflicting	= 0x80000001
    } 	FwTextPropConstants;

typedef /* [v1_enum] */ 
enum FwTextToggleVal
    {
        kttvOff	= 0,
        kttvForceOn	= 1,
        kttvInvert	= 2
    } 	FwTextToggleVal;

typedef /* [v1_enum] */ 
enum FwSuperscriptVal
    {
        kssvOff	= 0,
        kssvSuper	= 1,
        kssvSub	= 2
    } 	FwSuperscriptVal;

typedef /* [v1_enum] */ 
enum FwTextColor
    {
        kclrWhite	= 0xffffff,
        kclrBlack	= 0,
        kclrRed	= 0xff,
        kclrGreen	= 0xff00,
        kclrBlue	= 0xff0000,
        kclrYellow	= 0xffff,
        kclrMagenta	= 0xff00ff,
        kclrCyan	= 0xffff00,
        kclrTransparent	= 0xc0000000
    } 	FwTextColor;

typedef /* [v1_enum] */ 
enum FwUnderlineType
    {
        kuntMin	= 0,
        kuntNone	= kuntMin,
        kuntDotted	= ( kuntNone + 1 ) ,
        kuntDashed	= ( kuntDotted + 1 ) ,
        kuntSingle	= ( kuntDashed + 1 ) ,
        kuntDouble	= ( kuntSingle + 1 ) ,
        kuntStrikethrough	= ( kuntDouble + 1 ) ,
        kuntSquiggle	= ( kuntStrikethrough + 1 ) ,
        kuntLim	= ( kuntSquiggle + 1 ) 
    } 	FwUnderlineType;

typedef /* [v1_enum] */ 
enum FwTextAlign
    {
        ktalMin	= 0,
        ktalLeading	= ktalMin,
        ktalLeft	= ( ktalLeading + 1 ) ,
        ktalCenter	= ( ktalLeft + 1 ) ,
        ktalRight	= ( ktalCenter + 1 ) ,
        ktalTrailing	= ( ktalRight + 1 ) ,
        ktalJustify	= ( ktalTrailing + 1 ) ,
        ktalLim	= ( ktalJustify + 1 ) 
    } 	FwTextAlign;

typedef struct TsRunInfo
    {
    int ichMin;
    int ichLim;
    int irun;
    } 	TsRunInfo;

GENERIC_DECLARE_SMART_INTERFACE_PTR(
ITsString
,
321B7BB3-29AF-41D1-93DE-4A11BF386C82
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ITsTextProps
,
9B804BE2-0F75-4182-AC97-77F477546AB0
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ITsStrFactory
,
721A8D21-9900-4CB0-B4C0-9380A23140E3
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ITsPropsFactory
,
FF3D947F-1D35-487B-A769-5B6C68722054
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ITsStrBldr
,
35C5278D-2A52-4B54-AB13-B6E346B301BA
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ITsIncStrBldr
,
87ECD3CD-6011-485F-8651-DBA0B79245AF
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ITsPropsBldr
,
F1EF76E8-BE04-11d3-8D9A-005004DEFEC4
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ITsMultiString
,
DD409520-C212-11d3-9BB7-00400541F9E9
);
typedef /* [v1_enum] */ 
enum StyleType
    {
        kstParagraph	= 0,
        kstCharacter	= ( kstParagraph + 1 ) ,
        kstLim	= ( kstCharacter + 1 ) 
    } 	StyleType;

typedef /* [v1_enum] */ 
enum VwBulNum
    {
        kvbnNone	= 0,
        kvbnNumberBase	= 10,
        kvbnArabic	= kvbnNumberBase,
        kvbnRomanUpper	= ( kvbnArabic + 1 ) ,
        kvbnRomanLower	= ( kvbnRomanUpper + 1 ) ,
        kvbnLetterUpper	= ( kvbnRomanLower + 1 ) ,
        kvbnLetterLower	= ( kvbnLetterUpper + 1 ) ,
        kvbnArabic01	= ( kvbnLetterLower + 1 ) ,
        kvbnNumberMax	= ( kvbnArabic01 + 1 ) ,
        kvbnBulletBase	= 100,
        kvbnBullet	= kvbnBulletBase,
        kvbnBulletMax	= ( kvbnBulletBase + 100 ) 
    } 	VwBulNum;

GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwStylesheet
,
D77C0DBC-C7BC-441d-9587-1E3664E1BCD3
);

typedef /* [public][public] */ struct __MIDL___MIDL_itf_FwKernelTlb_0001_0086_0001
    {
    COLORREF clrFore;
    COLORREF clrBack;
    COLORREF clrUnder;
    int dympOffset;
    int ws;
    byte fWsRtl;
    int nDirDepth;
    int ssv;
    int unt;
    int ttvBold;
    int ttvItalic;
    int dympHeight;
    OLECHAR szFaceName[ 32 ];
    OLECHAR szFontVar[ 64 ];
    } 	LgCharRenderProps;

GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgWritingSystem
,
9C0513AB-1AB9-4741-9C49-FA65FA83B7CC
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgWritingSystemFactory
,
CC2BD14F-ACCE-4246-9192-9C29441A5A09
);

#define LIBID_FwKernelLib __uuidof(FwKernelLib)

#ifndef __IFwMetaDataCache_INTERFACE_DEFINED__
#define __IFwMetaDataCache_INTERFACE_DEFINED__

/* interface IFwMetaDataCache */
/* [unique][object][uuid] */ 


#define IID_IFwMetaDataCache __uuidof(IFwMetaDataCache)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("EDBB1DED-7065-4b56-A262-746453835451")
    IFwMetaDataCache : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE InitXml( 
            /* [in] */ BSTR bstrPathname,
            /* [in] */ ComBool fClearPrevCache) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_FieldCount( 
            /* [retval][out] */ int *pcflid) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFieldIds( 
            /* [in] */ int cflid,
            /* [size_is][out] */ int *rgflid) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetOwnClsName( 
            /* [in] */ int luFlid,
            /* [retval][out] */ BSTR *pbstrOwnClsName) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetDstClsName( 
            /* [in] */ int luFlid,
            /* [retval][out] */ BSTR *pbstrDstClsName) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetOwnClsId( 
            /* [in] */ int luFlid,
            /* [retval][out] */ int *pluOwnClsid) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetDstClsId( 
            /* [in] */ int luFlid,
            /* [retval][out] */ int *pluDstClsid) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFieldName( 
            /* [in] */ int luFlid,
            /* [retval][out] */ BSTR *pbstrFieldName) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFieldLabel( 
            /* [in] */ int luFlid,
            /* [retval][out] */ BSTR *pbstrFieldLabel) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFieldHelp( 
            /* [in] */ int luFlid,
            /* [retval][out] */ BSTR *pbstrFieldHelp) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFieldXml( 
            /* [in] */ int luFlid,
            /* [retval][out] */ BSTR *pbstrFieldXml) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFieldWs( 
            /* [in] */ int luFlid,
            /* [retval][out] */ int *piWs) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFieldType( 
            /* [in] */ int luFlid,
            /* [retval][out] */ int *piType) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsValidClass( 
            /* [in] */ int luFlid,
            /* [in] */ int luClid,
            /* [retval][out] */ ComBool *pfValid) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ClassCount( 
            /* [retval][out] */ int *pcclid) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetClassIds( 
            /* [in] */ int cclid,
            /* [size_is][out] */ int *rgclid) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetClassName( 
            /* [in] */ int luClid,
            /* [retval][out] */ BSTR *pbstrClassName) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetAbstract( 
            /* [in] */ int luClid,
            /* [retval][out] */ ComBool *pfAbstract) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetBaseClsId( 
            /* [in] */ int luClid,
            /* [retval][out] */ int *pluClid) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetBaseClsName( 
            /* [in] */ int luClid,
            /* [retval][out] */ BSTR *pbstrBaseClsName) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFields( 
            /* [in] */ int luClid,
            /* [in] */ ComBool fIncludeSuperclasses,
            /* [in] */ int grfcpt,
            /* [in] */ int cflidMax,
            /* [size_is][out] */ int *prgflid,
            /* [retval][out] */ int *pcflid) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetClassId( 
            /* [in] */ BSTR bstrClassName,
            /* [retval][out] */ int *pluClid) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFieldId( 
            /* [in] */ BSTR bstrClassName,
            /* [in] */ BSTR bstrFieldName,
            /* [defaultvalue][in] */ ComBool fIncludeBaseClasses,
            /* [retval][out] */ int *pluFlid) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFieldId2( 
            /* [in] */ int luClid,
            /* [in] */ BSTR bstrFieldName,
            /* [defaultvalue][in] */ ComBool fIncludeBaseClasses,
            /* [retval][out] */ int *pluFlid) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetDirectSubclasses( 
            /* [in] */ int luClid,
            /* [in] */ int cluMax,
            /* [out] */ int *pcluOut,
            /* [size_is][out] */ int *prgluSubclasses) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetAllSubclasses( 
            /* [in] */ int luClid,
            /* [in] */ int cluMax,
            /* [out] */ int *pcluOut,
            /* [size_is][out] */ int *prgluSubclasses) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AddVirtualProp( 
            /* [in] */ BSTR bstrClass,
            /* [in] */ BSTR bstrField,
            /* [in] */ int luFlid,
            /* [in] */ int type) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsVirtual( 
            /* [in] */ int luFlid,
            /* [retval][out] */ ComBool *pfVirtual) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFieldNameOrNull( 
            /* [in] */ int luFlid,
            /* [retval][out] */ BSTR *pbstrFieldName) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IFwMetaDataCacheVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IFwMetaDataCache * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IFwMetaDataCache * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IFwMetaDataCache * This);
        
        HRESULT ( STDMETHODCALLTYPE *InitXml )( 
            IFwMetaDataCache * This,
            /* [in] */ BSTR bstrPathname,
            /* [in] */ ComBool fClearPrevCache);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_FieldCount )( 
            IFwMetaDataCache * This,
            /* [retval][out] */ int *pcflid);
        
        HRESULT ( STDMETHODCALLTYPE *GetFieldIds )( 
            IFwMetaDataCache * This,
            /* [in] */ int cflid,
            /* [size_is][out] */ int *rgflid);
        
        HRESULT ( STDMETHODCALLTYPE *GetOwnClsName )( 
            IFwMetaDataCache * This,
            /* [in] */ int luFlid,
            /* [retval][out] */ BSTR *pbstrOwnClsName);
        
        HRESULT ( STDMETHODCALLTYPE *GetDstClsName )( 
            IFwMetaDataCache * This,
            /* [in] */ int luFlid,
            /* [retval][out] */ BSTR *pbstrDstClsName);
        
        HRESULT ( STDMETHODCALLTYPE *GetOwnClsId )( 
            IFwMetaDataCache * This,
            /* [in] */ int luFlid,
            /* [retval][out] */ int *pluOwnClsid);
        
        HRESULT ( STDMETHODCALLTYPE *GetDstClsId )( 
            IFwMetaDataCache * This,
            /* [in] */ int luFlid,
            /* [retval][out] */ int *pluDstClsid);
        
        HRESULT ( STDMETHODCALLTYPE *GetFieldName )( 
            IFwMetaDataCache * This,
            /* [in] */ int luFlid,
            /* [retval][out] */ BSTR *pbstrFieldName);
        
        HRESULT ( STDMETHODCALLTYPE *GetFieldLabel )( 
            IFwMetaDataCache * This,
            /* [in] */ int luFlid,
            /* [retval][out] */ BSTR *pbstrFieldLabel);
        
        HRESULT ( STDMETHODCALLTYPE *GetFieldHelp )( 
            IFwMetaDataCache * This,
            /* [in] */ int luFlid,
            /* [retval][out] */ BSTR *pbstrFieldHelp);
        
        HRESULT ( STDMETHODCALLTYPE *GetFieldXml )( 
            IFwMetaDataCache * This,
            /* [in] */ int luFlid,
            /* [retval][out] */ BSTR *pbstrFieldXml);
        
        HRESULT ( STDMETHODCALLTYPE *GetFieldWs )( 
            IFwMetaDataCache * This,
            /* [in] */ int luFlid,
            /* [retval][out] */ int *piWs);
        
        HRESULT ( STDMETHODCALLTYPE *GetFieldType )( 
            IFwMetaDataCache * This,
            /* [in] */ int luFlid,
            /* [retval][out] */ int *piType);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsValidClass )( 
            IFwMetaDataCache * This,
            /* [in] */ int luFlid,
            /* [in] */ int luClid,
            /* [retval][out] */ ComBool *pfValid);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ClassCount )( 
            IFwMetaDataCache * This,
            /* [retval][out] */ int *pcclid);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassIds )( 
            IFwMetaDataCache * This,
            /* [in] */ int cclid,
            /* [size_is][out] */ int *rgclid);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassName )( 
            IFwMetaDataCache * This,
            /* [in] */ int luClid,
            /* [retval][out] */ BSTR *pbstrClassName);
        
        HRESULT ( STDMETHODCALLTYPE *GetAbstract )( 
            IFwMetaDataCache * This,
            /* [in] */ int luClid,
            /* [retval][out] */ ComBool *pfAbstract);
        
        HRESULT ( STDMETHODCALLTYPE *GetBaseClsId )( 
            IFwMetaDataCache * This,
            /* [in] */ int luClid,
            /* [retval][out] */ int *pluClid);
        
        HRESULT ( STDMETHODCALLTYPE *GetBaseClsName )( 
            IFwMetaDataCache * This,
            /* [in] */ int luClid,
            /* [retval][out] */ BSTR *pbstrBaseClsName);
        
        HRESULT ( STDMETHODCALLTYPE *GetFields )( 
            IFwMetaDataCache * This,
            /* [in] */ int luClid,
            /* [in] */ ComBool fIncludeSuperclasses,
            /* [in] */ int grfcpt,
            /* [in] */ int cflidMax,
            /* [size_is][out] */ int *prgflid,
            /* [retval][out] */ int *pcflid);
        
        HRESULT ( STDMETHODCALLTYPE *GetClassId )( 
            IFwMetaDataCache * This,
            /* [in] */ BSTR bstrClassName,
            /* [retval][out] */ int *pluClid);
        
        HRESULT ( STDMETHODCALLTYPE *GetFieldId )( 
            IFwMetaDataCache * This,
            /* [in] */ BSTR bstrClassName,
            /* [in] */ BSTR bstrFieldName,
            /* [defaultvalue][in] */ ComBool fIncludeBaseClasses,
            /* [retval][out] */ int *pluFlid);
        
        HRESULT ( STDMETHODCALLTYPE *GetFieldId2 )( 
            IFwMetaDataCache * This,
            /* [in] */ int luClid,
            /* [in] */ BSTR bstrFieldName,
            /* [defaultvalue][in] */ ComBool fIncludeBaseClasses,
            /* [retval][out] */ int *pluFlid);
        
        HRESULT ( STDMETHODCALLTYPE *GetDirectSubclasses )( 
            IFwMetaDataCache * This,
            /* [in] */ int luClid,
            /* [in] */ int cluMax,
            /* [out] */ int *pcluOut,
            /* [size_is][out] */ int *prgluSubclasses);
        
        HRESULT ( STDMETHODCALLTYPE *GetAllSubclasses )( 
            IFwMetaDataCache * This,
            /* [in] */ int luClid,
            /* [in] */ int cluMax,
            /* [out] */ int *pcluOut,
            /* [size_is][out] */ int *prgluSubclasses);
        
        HRESULT ( STDMETHODCALLTYPE *AddVirtualProp )( 
            IFwMetaDataCache * This,
            /* [in] */ BSTR bstrClass,
            /* [in] */ BSTR bstrField,
            /* [in] */ int luFlid,
            /* [in] */ int type);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsVirtual )( 
            IFwMetaDataCache * This,
            /* [in] */ int luFlid,
            /* [retval][out] */ ComBool *pfVirtual);
        
        HRESULT ( STDMETHODCALLTYPE *GetFieldNameOrNull )( 
            IFwMetaDataCache * This,
            /* [in] */ int luFlid,
            /* [retval][out] */ BSTR *pbstrFieldName);
        
        END_INTERFACE
    } IFwMetaDataCacheVtbl;

    interface IFwMetaDataCache
    {
        CONST_VTBL struct IFwMetaDataCacheVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IFwMetaDataCache_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IFwMetaDataCache_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IFwMetaDataCache_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IFwMetaDataCache_InitXml(This,bstrPathname,fClearPrevCache)	\
    ( (This)->lpVtbl -> InitXml(This,bstrPathname,fClearPrevCache) ) 

#define IFwMetaDataCache_get_FieldCount(This,pcflid)	\
    ( (This)->lpVtbl -> get_FieldCount(This,pcflid) ) 

#define IFwMetaDataCache_GetFieldIds(This,cflid,rgflid)	\
    ( (This)->lpVtbl -> GetFieldIds(This,cflid,rgflid) ) 

#define IFwMetaDataCache_GetOwnClsName(This,luFlid,pbstrOwnClsName)	\
    ( (This)->lpVtbl -> GetOwnClsName(This,luFlid,pbstrOwnClsName) ) 

#define IFwMetaDataCache_GetDstClsName(This,luFlid,pbstrDstClsName)	\
    ( (This)->lpVtbl -> GetDstClsName(This,luFlid,pbstrDstClsName) ) 

#define IFwMetaDataCache_GetOwnClsId(This,luFlid,pluOwnClsid)	\
    ( (This)->lpVtbl -> GetOwnClsId(This,luFlid,pluOwnClsid) ) 

#define IFwMetaDataCache_GetDstClsId(This,luFlid,pluDstClsid)	\
    ( (This)->lpVtbl -> GetDstClsId(This,luFlid,pluDstClsid) ) 

#define IFwMetaDataCache_GetFieldName(This,luFlid,pbstrFieldName)	\
    ( (This)->lpVtbl -> GetFieldName(This,luFlid,pbstrFieldName) ) 

#define IFwMetaDataCache_GetFieldLabel(This,luFlid,pbstrFieldLabel)	\
    ( (This)->lpVtbl -> GetFieldLabel(This,luFlid,pbstrFieldLabel) ) 

#define IFwMetaDataCache_GetFieldHelp(This,luFlid,pbstrFieldHelp)	\
    ( (This)->lpVtbl -> GetFieldHelp(This,luFlid,pbstrFieldHelp) ) 

#define IFwMetaDataCache_GetFieldXml(This,luFlid,pbstrFieldXml)	\
    ( (This)->lpVtbl -> GetFieldXml(This,luFlid,pbstrFieldXml) ) 

#define IFwMetaDataCache_GetFieldWs(This,luFlid,piWs)	\
    ( (This)->lpVtbl -> GetFieldWs(This,luFlid,piWs) ) 

#define IFwMetaDataCache_GetFieldType(This,luFlid,piType)	\
    ( (This)->lpVtbl -> GetFieldType(This,luFlid,piType) ) 

#define IFwMetaDataCache_get_IsValidClass(This,luFlid,luClid,pfValid)	\
    ( (This)->lpVtbl -> get_IsValidClass(This,luFlid,luClid,pfValid) ) 

#define IFwMetaDataCache_get_ClassCount(This,pcclid)	\
    ( (This)->lpVtbl -> get_ClassCount(This,pcclid) ) 

#define IFwMetaDataCache_GetClassIds(This,cclid,rgclid)	\
    ( (This)->lpVtbl -> GetClassIds(This,cclid,rgclid) ) 

#define IFwMetaDataCache_GetClassName(This,luClid,pbstrClassName)	\
    ( (This)->lpVtbl -> GetClassName(This,luClid,pbstrClassName) ) 

#define IFwMetaDataCache_GetAbstract(This,luClid,pfAbstract)	\
    ( (This)->lpVtbl -> GetAbstract(This,luClid,pfAbstract) ) 

#define IFwMetaDataCache_GetBaseClsId(This,luClid,pluClid)	\
    ( (This)->lpVtbl -> GetBaseClsId(This,luClid,pluClid) ) 

#define IFwMetaDataCache_GetBaseClsName(This,luClid,pbstrBaseClsName)	\
    ( (This)->lpVtbl -> GetBaseClsName(This,luClid,pbstrBaseClsName) ) 

#define IFwMetaDataCache_GetFields(This,luClid,fIncludeSuperclasses,grfcpt,cflidMax,prgflid,pcflid)	\
    ( (This)->lpVtbl -> GetFields(This,luClid,fIncludeSuperclasses,grfcpt,cflidMax,prgflid,pcflid) ) 

#define IFwMetaDataCache_GetClassId(This,bstrClassName,pluClid)	\
    ( (This)->lpVtbl -> GetClassId(This,bstrClassName,pluClid) ) 

#define IFwMetaDataCache_GetFieldId(This,bstrClassName,bstrFieldName,fIncludeBaseClasses,pluFlid)	\
    ( (This)->lpVtbl -> GetFieldId(This,bstrClassName,bstrFieldName,fIncludeBaseClasses,pluFlid) ) 

#define IFwMetaDataCache_GetFieldId2(This,luClid,bstrFieldName,fIncludeBaseClasses,pluFlid)	\
    ( (This)->lpVtbl -> GetFieldId2(This,luClid,bstrFieldName,fIncludeBaseClasses,pluFlid) ) 

#define IFwMetaDataCache_GetDirectSubclasses(This,luClid,cluMax,pcluOut,prgluSubclasses)	\
    ( (This)->lpVtbl -> GetDirectSubclasses(This,luClid,cluMax,pcluOut,prgluSubclasses) ) 

#define IFwMetaDataCache_GetAllSubclasses(This,luClid,cluMax,pcluOut,prgluSubclasses)	\
    ( (This)->lpVtbl -> GetAllSubclasses(This,luClid,cluMax,pcluOut,prgluSubclasses) ) 

#define IFwMetaDataCache_AddVirtualProp(This,bstrClass,bstrField,luFlid,type)	\
    ( (This)->lpVtbl -> AddVirtualProp(This,bstrClass,bstrField,luFlid,type) ) 

#define IFwMetaDataCache_get_IsVirtual(This,luFlid,pfVirtual)	\
    ( (This)->lpVtbl -> get_IsVirtual(This,luFlid,pfVirtual) ) 

#define IFwMetaDataCache_GetFieldNameOrNull(This,luFlid,pbstrFieldName)	\
    ( (This)->lpVtbl -> GetFieldNameOrNull(This,luFlid,pbstrFieldName) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IFwMetaDataCache_INTERFACE_DEFINED__ */


#ifndef __ILgWritingSystemFactory_INTERFACE_DEFINED__
#define __ILgWritingSystemFactory_INTERFACE_DEFINED__

/* interface ILgWritingSystemFactory */
/* [unique][object][uuid] */ 


#define IID_ILgWritingSystemFactory __uuidof(ILgWritingSystemFactory)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("CC2BD14F-ACCE-4246-9192-9C29441A5A09")
    ILgWritingSystemFactory : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Engine( 
            /* [in] */ BSTR bstrId,
            /* [retval][out] */ ILgWritingSystem **ppwseng) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_EngineOrNull( 
            /* [in] */ int ws,
            /* [retval][out] */ ILgWritingSystem **ppwseng) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetWsFromStr( 
            /* [in] */ BSTR bstr,
            /* [retval][out] */ int *pws) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetStrFromWs( 
            /* [in] */ int ws,
            /* [retval][out] */ BSTR *pbstr) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetIcuLocaleFromWs( 
            /* [in] */ int ws,
            /* [retval][out] */ BSTR *pbstr) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_NumberOfWs( 
            /* [retval][out] */ int *pcws) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetWritingSystems( 
            /* [size_is][out] */ int *rgws,
            /* [in] */ int cws) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_UserWs( 
            /* [retval][out] */ int *pws) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_UserWs( 
            /* [in] */ int ws) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ILgWritingSystemFactoryVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ILgWritingSystemFactory * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ILgWritingSystemFactory * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ILgWritingSystemFactory * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Engine )( 
            ILgWritingSystemFactory * This,
            /* [in] */ BSTR bstrId,
            /* [retval][out] */ ILgWritingSystem **ppwseng);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_EngineOrNull )( 
            ILgWritingSystemFactory * This,
            /* [in] */ int ws,
            /* [retval][out] */ ILgWritingSystem **ppwseng);
        
        HRESULT ( STDMETHODCALLTYPE *GetWsFromStr )( 
            ILgWritingSystemFactory * This,
            /* [in] */ BSTR bstr,
            /* [retval][out] */ int *pws);
        
        HRESULT ( STDMETHODCALLTYPE *GetStrFromWs )( 
            ILgWritingSystemFactory * This,
            /* [in] */ int ws,
            /* [retval][out] */ BSTR *pbstr);
        
        HRESULT ( STDMETHODCALLTYPE *GetIcuLocaleFromWs )( 
            ILgWritingSystemFactory * This,
            /* [in] */ int ws,
            /* [retval][out] */ BSTR *pbstr);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_NumberOfWs )( 
            ILgWritingSystemFactory * This,
            /* [retval][out] */ int *pcws);
        
        HRESULT ( STDMETHODCALLTYPE *GetWritingSystems )( 
            ILgWritingSystemFactory * This,
            /* [size_is][out] */ int *rgws,
            /* [in] */ int cws);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_UserWs )( 
            ILgWritingSystemFactory * This,
            /* [retval][out] */ int *pws);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_UserWs )( 
            ILgWritingSystemFactory * This,
            /* [in] */ int ws);
        
        END_INTERFACE
    } ILgWritingSystemFactoryVtbl;

    interface ILgWritingSystemFactory
    {
        CONST_VTBL struct ILgWritingSystemFactoryVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ILgWritingSystemFactory_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ILgWritingSystemFactory_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ILgWritingSystemFactory_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ILgWritingSystemFactory_get_Engine(This,bstrId,ppwseng)	\
    ( (This)->lpVtbl -> get_Engine(This,bstrId,ppwseng) ) 

#define ILgWritingSystemFactory_get_EngineOrNull(This,ws,ppwseng)	\
    ( (This)->lpVtbl -> get_EngineOrNull(This,ws,ppwseng) ) 

#define ILgWritingSystemFactory_GetWsFromStr(This,bstr,pws)	\
    ( (This)->lpVtbl -> GetWsFromStr(This,bstr,pws) ) 

#define ILgWritingSystemFactory_GetStrFromWs(This,ws,pbstr)	\
    ( (This)->lpVtbl -> GetStrFromWs(This,ws,pbstr) ) 

#define ILgWritingSystemFactory_GetIcuLocaleFromWs(This,ws,pbstr)	\
    ( (This)->lpVtbl -> GetIcuLocaleFromWs(This,ws,pbstr) ) 

#define ILgWritingSystemFactory_get_NumberOfWs(This,pcws)	\
    ( (This)->lpVtbl -> get_NumberOfWs(This,pcws) ) 

#define ILgWritingSystemFactory_GetWritingSystems(This,rgws,cws)	\
    ( (This)->lpVtbl -> GetWritingSystems(This,rgws,cws) ) 

#define ILgWritingSystemFactory_get_UserWs(This,pws)	\
    ( (This)->lpVtbl -> get_UserWs(This,pws) ) 

#define ILgWritingSystemFactory_put_UserWs(This,ws)	\
    ( (This)->lpVtbl -> put_UserWs(This,ws) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ILgWritingSystemFactory_INTERFACE_DEFINED__ */


#ifndef __ITsMultiString_INTERFACE_DEFINED__
#define __ITsMultiString_INTERFACE_DEFINED__

/* interface ITsMultiString */
/* [unique][object][uuid] */ 


#define IID_ITsMultiString __uuidof(ITsMultiString)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("DD409520-C212-11d3-9BB7-00400541F9E9")
    ITsMultiString : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_StringCount( 
            /* [retval][out] */ int *pctss) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetStringFromIndex( 
            /* [in] */ int iws,
            /* [out] */ int *pws,
            /* [retval][out] */ ITsString **pptss) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_String( 
            /* [in] */ int ws,
            /* [retval][out] */ ITsString **pptss) = 0;
        
        virtual /* [propputref] */ HRESULT STDMETHODCALLTYPE putref_String( 
            /* [in] */ int ws,
            /* [in] */ ITsString *ptss) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ITsMultiStringVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ITsMultiString * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ITsMultiString * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ITsMultiString * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_StringCount )( 
            ITsMultiString * This,
            /* [retval][out] */ int *pctss);
        
        HRESULT ( STDMETHODCALLTYPE *GetStringFromIndex )( 
            ITsMultiString * This,
            /* [in] */ int iws,
            /* [out] */ int *pws,
            /* [retval][out] */ ITsString **pptss);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_String )( 
            ITsMultiString * This,
            /* [in] */ int ws,
            /* [retval][out] */ ITsString **pptss);
        
        /* [propputref] */ HRESULT ( STDMETHODCALLTYPE *putref_String )( 
            ITsMultiString * This,
            /* [in] */ int ws,
            /* [in] */ ITsString *ptss);
        
        END_INTERFACE
    } ITsMultiStringVtbl;

    interface ITsMultiString
    {
        CONST_VTBL struct ITsMultiStringVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ITsMultiString_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ITsMultiString_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ITsMultiString_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ITsMultiString_get_StringCount(This,pctss)	\
    ( (This)->lpVtbl -> get_StringCount(This,pctss) ) 

#define ITsMultiString_GetStringFromIndex(This,iws,pws,pptss)	\
    ( (This)->lpVtbl -> GetStringFromIndex(This,iws,pws,pptss) ) 

#define ITsMultiString_get_String(This,ws,pptss)	\
    ( (This)->lpVtbl -> get_String(This,ws,pptss) ) 

#define ITsMultiString_putref_String(This,ws,ptss)	\
    ( (This)->lpVtbl -> putref_String(This,ws,ptss) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ITsMultiString_INTERFACE_DEFINED__ */


#ifndef __ITsString_INTERFACE_DEFINED__
#define __ITsString_INTERFACE_DEFINED__

/* interface ITsString */
/* [unique][object][uuid] */ 


#define IID_ITsString __uuidof(ITsString)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("321B7BB3-29AF-41D1-93DE-4A11BF386C82")
    ITsString : public IUnknown
    {
    public:
        virtual /* [id][propget] */ HRESULT STDMETHODCALLTYPE get_Text( 
            /* [retval][out] */ BSTR *pbstr) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Length( 
            /* [retval][out] */ int *pcch) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_RunCount( 
            /* [retval][out] */ int *pcrun) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_RunAt( 
            /* [in] */ int ich,
            /* [retval][out] */ int *pirun) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_MinOfRun( 
            /* [in] */ int irun,
            /* [retval][out] */ int *pichMin) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_LimOfRun( 
            /* [in] */ int irun,
            /* [retval][out] */ int *pichLim) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetBoundsOfRun( 
            /* [in] */ int irun,
            /* [out] */ int *pichMin,
            /* [out] */ int *pichLim) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE FetchRunInfoAt( 
            /* [in] */ int ich,
            /* [out] */ TsRunInfo *ptri,
            /* [retval][out] */ ITsTextProps **ppttp) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE FetchRunInfo( 
            /* [in] */ int irun,
            /* [out] */ TsRunInfo *ptri,
            /* [retval][out] */ ITsTextProps **ppttp) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_RunText( 
            /* [in] */ int irun,
            /* [retval][out] */ BSTR *pbstr) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetChars( 
            /* [in] */ int ichMin,
            /* [in] */ int ichLim,
            /* [retval][out] */ BSTR *pbstr) = 0;
        
        virtual /* [local][restricted] */ HRESULT STDMETHODCALLTYPE FetchChars( 
            /* [in] */ int ichMin,
            /* [in] */ int ichLim,
            /* [size_is][out][in] */ OLECHAR *prgch) = 0;
        
        virtual /* [local][restricted] */ HRESULT STDMETHODCALLTYPE LockText( 
            /* [string][out] */ const OLECHAR **pprgch,
            /* [out] */ int *pcch) = 0;
        
        virtual /* [local][restricted] */ HRESULT STDMETHODCALLTYPE UnlockText( 
            /* [string][in] */ const OLECHAR *prgch) = 0;
        
        virtual /* [local][restricted] */ HRESULT STDMETHODCALLTYPE LockRun( 
            /* [in] */ int irun,
            /* [string][out] */ const OLECHAR **pprgch,
            /* [out] */ int *pcch) = 0;
        
        virtual /* [local][restricted] */ HRESULT STDMETHODCALLTYPE UnlockRun( 
            /* [in] */ int irun,
            /* [string][in] */ const OLECHAR *prgch) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_PropertiesAt( 
            /* [in] */ int ich,
            /* [retval][out] */ ITsTextProps **ppttp) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Properties( 
            /* [in] */ int irun,
            /* [retval][out] */ ITsTextProps **ppttp) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetBldr( 
            /* [retval][out] */ ITsStrBldr **pptsb) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetIncBldr( 
            /* [retval][out] */ ITsIncStrBldr **pptisb) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Equals( 
            /* [in] */ ITsString *ptss,
            /* [retval][out] */ ComBool *pfEqual) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsNormalizedForm( 
            /* [in] */ FwNormalizationMode nm,
            /* [retval][out] */ ComBool *pfRet) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_NormalizedForm( 
            /* [in] */ FwNormalizationMode nm,
            /* [retval][out] */ ITsString **pptssRet) = 0;
        
        virtual /* [local][restricted] */ HRESULT STDMETHODCALLTYPE NfdAndFixOffsets( 
            /* [out] */ ITsString **pptssRet,
            /* [size_is][out][in] */ int **prgpichOffsetsToFix,
            /* [in] */ int cichOffsetsToFix) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetSubstring( 
            /* [in] */ int ichMin,
            /* [in] */ int ichLim,
            /* [retval][out] */ ITsString **pptssRet) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ITsStringVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ITsString * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ITsString * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ITsString * This);
        
        /* [id][propget] */ HRESULT ( STDMETHODCALLTYPE *get_Text )( 
            ITsString * This,
            /* [retval][out] */ BSTR *pbstr);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Length )( 
            ITsString * This,
            /* [retval][out] */ int *pcch);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_RunCount )( 
            ITsString * This,
            /* [retval][out] */ int *pcrun);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_RunAt )( 
            ITsString * This,
            /* [in] */ int ich,
            /* [retval][out] */ int *pirun);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_MinOfRun )( 
            ITsString * This,
            /* [in] */ int irun,
            /* [retval][out] */ int *pichMin);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_LimOfRun )( 
            ITsString * This,
            /* [in] */ int irun,
            /* [retval][out] */ int *pichLim);
        
        HRESULT ( STDMETHODCALLTYPE *GetBoundsOfRun )( 
            ITsString * This,
            /* [in] */ int irun,
            /* [out] */ int *pichMin,
            /* [out] */ int *pichLim);
        
        HRESULT ( STDMETHODCALLTYPE *FetchRunInfoAt )( 
            ITsString * This,
            /* [in] */ int ich,
            /* [out] */ TsRunInfo *ptri,
            /* [retval][out] */ ITsTextProps **ppttp);
        
        HRESULT ( STDMETHODCALLTYPE *FetchRunInfo )( 
            ITsString * This,
            /* [in] */ int irun,
            /* [out] */ TsRunInfo *ptri,
            /* [retval][out] */ ITsTextProps **ppttp);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_RunText )( 
            ITsString * This,
            /* [in] */ int irun,
            /* [retval][out] */ BSTR *pbstr);
        
        HRESULT ( STDMETHODCALLTYPE *GetChars )( 
            ITsString * This,
            /* [in] */ int ichMin,
            /* [in] */ int ichLim,
            /* [retval][out] */ BSTR *pbstr);
        
        /* [local][restricted] */ HRESULT ( STDMETHODCALLTYPE *FetchChars )( 
            ITsString * This,
            /* [in] */ int ichMin,
            /* [in] */ int ichLim,
            /* [size_is][out][in] */ OLECHAR *prgch);
        
        /* [local][restricted] */ HRESULT ( STDMETHODCALLTYPE *LockText )( 
            ITsString * This,
            /* [string][out] */ const OLECHAR **pprgch,
            /* [out] */ int *pcch);
        
        /* [local][restricted] */ HRESULT ( STDMETHODCALLTYPE *UnlockText )( 
            ITsString * This,
            /* [string][in] */ const OLECHAR *prgch);
        
        /* [local][restricted] */ HRESULT ( STDMETHODCALLTYPE *LockRun )( 
            ITsString * This,
            /* [in] */ int irun,
            /* [string][out] */ const OLECHAR **pprgch,
            /* [out] */ int *pcch);
        
        /* [local][restricted] */ HRESULT ( STDMETHODCALLTYPE *UnlockRun )( 
            ITsString * This,
            /* [in] */ int irun,
            /* [string][in] */ const OLECHAR *prgch);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_PropertiesAt )( 
            ITsString * This,
            /* [in] */ int ich,
            /* [retval][out] */ ITsTextProps **ppttp);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Properties )( 
            ITsString * This,
            /* [in] */ int irun,
            /* [retval][out] */ ITsTextProps **ppttp);
        
        HRESULT ( STDMETHODCALLTYPE *GetBldr )( 
            ITsString * This,
            /* [retval][out] */ ITsStrBldr **pptsb);
        
        HRESULT ( STDMETHODCALLTYPE *GetIncBldr )( 
            ITsString * This,
            /* [retval][out] */ ITsIncStrBldr **pptisb);
        
        HRESULT ( STDMETHODCALLTYPE *Equals )( 
            ITsString * This,
            /* [in] */ ITsString *ptss,
            /* [retval][out] */ ComBool *pfEqual);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsNormalizedForm )( 
            ITsString * This,
            /* [in] */ FwNormalizationMode nm,
            /* [retval][out] */ ComBool *pfRet);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_NormalizedForm )( 
            ITsString * This,
            /* [in] */ FwNormalizationMode nm,
            /* [retval][out] */ ITsString **pptssRet);
        
        /* [local][restricted] */ HRESULT ( STDMETHODCALLTYPE *NfdAndFixOffsets )( 
            ITsString * This,
            /* [out] */ ITsString **pptssRet,
            /* [size_is][out][in] */ int **prgpichOffsetsToFix,
            /* [in] */ int cichOffsetsToFix);
        
        HRESULT ( STDMETHODCALLTYPE *GetSubstring )( 
            ITsString * This,
            /* [in] */ int ichMin,
            /* [in] */ int ichLim,
            /* [retval][out] */ ITsString **pptssRet);
        
        END_INTERFACE
    } ITsStringVtbl;

    interface ITsString
    {
        CONST_VTBL struct ITsStringVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ITsString_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ITsString_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ITsString_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ITsString_get_Text(This,pbstr)	\
    ( (This)->lpVtbl -> get_Text(This,pbstr) ) 

#define ITsString_get_Length(This,pcch)	\
    ( (This)->lpVtbl -> get_Length(This,pcch) ) 

#define ITsString_get_RunCount(This,pcrun)	\
    ( (This)->lpVtbl -> get_RunCount(This,pcrun) ) 

#define ITsString_get_RunAt(This,ich,pirun)	\
    ( (This)->lpVtbl -> get_RunAt(This,ich,pirun) ) 

#define ITsString_get_MinOfRun(This,irun,pichMin)	\
    ( (This)->lpVtbl -> get_MinOfRun(This,irun,pichMin) ) 

#define ITsString_get_LimOfRun(This,irun,pichLim)	\
    ( (This)->lpVtbl -> get_LimOfRun(This,irun,pichLim) ) 

#define ITsString_GetBoundsOfRun(This,irun,pichMin,pichLim)	\
    ( (This)->lpVtbl -> GetBoundsOfRun(This,irun,pichMin,pichLim) ) 

#define ITsString_FetchRunInfoAt(This,ich,ptri,ppttp)	\
    ( (This)->lpVtbl -> FetchRunInfoAt(This,ich,ptri,ppttp) ) 

#define ITsString_FetchRunInfo(This,irun,ptri,ppttp)	\
    ( (This)->lpVtbl -> FetchRunInfo(This,irun,ptri,ppttp) ) 

#define ITsString_get_RunText(This,irun,pbstr)	\
    ( (This)->lpVtbl -> get_RunText(This,irun,pbstr) ) 

#define ITsString_GetChars(This,ichMin,ichLim,pbstr)	\
    ( (This)->lpVtbl -> GetChars(This,ichMin,ichLim,pbstr) ) 

#define ITsString_FetchChars(This,ichMin,ichLim,prgch)	\
    ( (This)->lpVtbl -> FetchChars(This,ichMin,ichLim,prgch) ) 

#define ITsString_LockText(This,pprgch,pcch)	\
    ( (This)->lpVtbl -> LockText(This,pprgch,pcch) ) 

#define ITsString_UnlockText(This,prgch)	\
    ( (This)->lpVtbl -> UnlockText(This,prgch) ) 

#define ITsString_LockRun(This,irun,pprgch,pcch)	\
    ( (This)->lpVtbl -> LockRun(This,irun,pprgch,pcch) ) 

#define ITsString_UnlockRun(This,irun,prgch)	\
    ( (This)->lpVtbl -> UnlockRun(This,irun,prgch) ) 

#define ITsString_get_PropertiesAt(This,ich,ppttp)	\
    ( (This)->lpVtbl -> get_PropertiesAt(This,ich,ppttp) ) 

#define ITsString_get_Properties(This,irun,ppttp)	\
    ( (This)->lpVtbl -> get_Properties(This,irun,ppttp) ) 

#define ITsString_GetBldr(This,pptsb)	\
    ( (This)->lpVtbl -> GetBldr(This,pptsb) ) 

#define ITsString_GetIncBldr(This,pptisb)	\
    ( (This)->lpVtbl -> GetIncBldr(This,pptisb) ) 

#define ITsString_Equals(This,ptss,pfEqual)	\
    ( (This)->lpVtbl -> Equals(This,ptss,pfEqual) ) 

#define ITsString_get_IsNormalizedForm(This,nm,pfRet)	\
    ( (This)->lpVtbl -> get_IsNormalizedForm(This,nm,pfRet) ) 

#define ITsString_get_NormalizedForm(This,nm,pptssRet)	\
    ( (This)->lpVtbl -> get_NormalizedForm(This,nm,pptssRet) ) 

#define ITsString_NfdAndFixOffsets(This,pptssRet,prgpichOffsetsToFix,cichOffsetsToFix)	\
    ( (This)->lpVtbl -> NfdAndFixOffsets(This,pptssRet,prgpichOffsetsToFix,cichOffsetsToFix) ) 

#define ITsString_GetSubstring(This,ichMin,ichLim,pptssRet)	\
    ( (This)->lpVtbl -> GetSubstring(This,ichMin,ichLim,pptssRet) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ITsString_INTERFACE_DEFINED__ */


#ifndef __ITsTextProps_INTERFACE_DEFINED__
#define __ITsTextProps_INTERFACE_DEFINED__

/* interface ITsTextProps */
/* [unique][object][uuid] */ 


#define IID_ITsTextProps __uuidof(ITsTextProps)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("9B804BE2-0F75-4182-AC97-77F477546AB0")
    ITsTextProps : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IntPropCount( 
            /* [retval][out] */ int *pcv) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetIntProp( 
            /* [in] */ int iv,
            /* [out] */ int *ptpt,
            /* [out] */ int *pnVar,
            /* [retval][out] */ int *pnVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetIntPropValues( 
            /* [in] */ int tpt,
            /* [out] */ int *pnVar,
            /* [retval][out] */ int *pnVal) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_StrPropCount( 
            /* [retval][out] */ int *pcv) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetStrProp( 
            /* [in] */ int iv,
            /* [out] */ int *ptpt,
            /* [retval][out] */ BSTR *pbstrVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetStrPropValue( 
            /* [in] */ int tpt,
            /* [retval][out] */ BSTR *pbstrVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetBldr( 
            /* [retval][out] */ ITsPropsBldr **pptpb) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ITsTextPropsVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ITsTextProps * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ITsTextProps * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ITsTextProps * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IntPropCount )( 
            ITsTextProps * This,
            /* [retval][out] */ int *pcv);
        
        HRESULT ( STDMETHODCALLTYPE *GetIntProp )( 
            ITsTextProps * This,
            /* [in] */ int iv,
            /* [out] */ int *ptpt,
            /* [out] */ int *pnVar,
            /* [retval][out] */ int *pnVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetIntPropValues )( 
            ITsTextProps * This,
            /* [in] */ int tpt,
            /* [out] */ int *pnVar,
            /* [retval][out] */ int *pnVal);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_StrPropCount )( 
            ITsTextProps * This,
            /* [retval][out] */ int *pcv);
        
        HRESULT ( STDMETHODCALLTYPE *GetStrProp )( 
            ITsTextProps * This,
            /* [in] */ int iv,
            /* [out] */ int *ptpt,
            /* [retval][out] */ BSTR *pbstrVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetStrPropValue )( 
            ITsTextProps * This,
            /* [in] */ int tpt,
            /* [retval][out] */ BSTR *pbstrVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetBldr )( 
            ITsTextProps * This,
            /* [retval][out] */ ITsPropsBldr **pptpb);
        
        END_INTERFACE
    } ITsTextPropsVtbl;

    interface ITsTextProps
    {
        CONST_VTBL struct ITsTextPropsVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ITsTextProps_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ITsTextProps_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ITsTextProps_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ITsTextProps_get_IntPropCount(This,pcv)	\
    ( (This)->lpVtbl -> get_IntPropCount(This,pcv) ) 

#define ITsTextProps_GetIntProp(This,iv,ptpt,pnVar,pnVal)	\
    ( (This)->lpVtbl -> GetIntProp(This,iv,ptpt,pnVar,pnVal) ) 

#define ITsTextProps_GetIntPropValues(This,tpt,pnVar,pnVal)	\
    ( (This)->lpVtbl -> GetIntPropValues(This,tpt,pnVar,pnVal) ) 

#define ITsTextProps_get_StrPropCount(This,pcv)	\
    ( (This)->lpVtbl -> get_StrPropCount(This,pcv) ) 

#define ITsTextProps_GetStrProp(This,iv,ptpt,pbstrVal)	\
    ( (This)->lpVtbl -> GetStrProp(This,iv,ptpt,pbstrVal) ) 

#define ITsTextProps_GetStrPropValue(This,tpt,pbstrVal)	\
    ( (This)->lpVtbl -> GetStrPropValue(This,tpt,pbstrVal) ) 

#define ITsTextProps_GetBldr(This,pptpb)	\
    ( (This)->lpVtbl -> GetBldr(This,pptpb) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ITsTextProps_INTERFACE_DEFINED__ */


#ifndef __IVwStylesheet_INTERFACE_DEFINED__
#define __IVwStylesheet_INTERFACE_DEFINED__

/* interface IVwStylesheet */
/* [unique][object][uuid] */ 


#define IID_IVwStylesheet __uuidof(IVwStylesheet)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("D77C0DBC-C7BC-441d-9587-1E3664E1BCD3")
    IVwStylesheet : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetDefaultBasedOnStyleName( 
            /* [retval][out] */ BSTR *pbstrNormal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetDefaultStyleForContext( 
            /* [in] */ int nContext,
            /* [in] */ ComBool fCharStyle,
            /* [retval][out] */ BSTR *pbstrStyleName) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE PutStyle( 
            /* [in] */ BSTR bstrName,
            /* [in] */ BSTR bstrUsage,
            /* [in] */ HVO hvoStyle,
            /* [in] */ HVO hvoBasedOn,
            /* [in] */ HVO hvoNext,
            /* [in] */ int nType,
            /* [in] */ ComBool fBuiltIn,
            /* [in] */ ComBool fModified,
            /* [in] */ ITsTextProps *pttp) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetStyleRgch( 
            /* [in] */ int cch,
            /* [size_is][in] */ OLECHAR *prgchName,
            /* [retval][out] */ ITsTextProps **ppttp) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetNextStyle( 
            /* [in] */ BSTR bstrName,
            /* [retval][out] */ BSTR *pbstrNext) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetBasedOn( 
            /* [in] */ BSTR bstrName,
            /* [retval][out] */ BSTR *pbstrBasedOn) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetType( 
            /* [in] */ BSTR bstrName,
            /* [retval][out] */ int *pnType) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetContext( 
            /* [in] */ BSTR bstrName,
            /* [retval][out] */ int *pnContext) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE IsBuiltIn( 
            /* [in] */ BSTR bstrName,
            /* [retval][out] */ ComBool *pfBuiltIn) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE IsModified( 
            /* [in] */ BSTR bstrName,
            /* [retval][out] */ ComBool *pfModified) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_DataAccess( 
            /* [retval][out] */ ISilDataAccess **ppsda) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE MakeNewStyle( 
            /* [retval][out] */ HVO *phvoNewStyle) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Delete( 
            /* [in] */ HVO hvoStyle) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_CStyles( 
            /* [retval][out] */ int *pcttp) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_NthStyle( 
            /* [in] */ int ihvo,
            /* [retval][out] */ HVO *phvo) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_NthStyleName( 
            /* [in] */ int ihvo,
            /* [retval][out] */ BSTR *pbstrStyleName) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_NormalFontStyle( 
            /* [retval][out] */ ITsTextProps **ppttp) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsStyleProtected( 
            /* [in] */ BSTR bstrName,
            /* [retval][out] */ ComBool *pfProtected) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CacheProps( 
            /* [in] */ int cch,
            /* [size_is][in] */ OLECHAR *prgchName,
            /* [in] */ HVO hvoStyle,
            /* [in] */ ITsTextProps *pttp) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IVwStylesheetVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IVwStylesheet * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IVwStylesheet * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IVwStylesheet * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetDefaultBasedOnStyleName )( 
            IVwStylesheet * This,
            /* [retval][out] */ BSTR *pbstrNormal);
        
        HRESULT ( STDMETHODCALLTYPE *GetDefaultStyleForContext )( 
            IVwStylesheet * This,
            /* [in] */ int nContext,
            /* [in] */ ComBool fCharStyle,
            /* [retval][out] */ BSTR *pbstrStyleName);
        
        HRESULT ( STDMETHODCALLTYPE *PutStyle )( 
            IVwStylesheet * This,
            /* [in] */ BSTR bstrName,
            /* [in] */ BSTR bstrUsage,
            /* [in] */ HVO hvoStyle,
            /* [in] */ HVO hvoBasedOn,
            /* [in] */ HVO hvoNext,
            /* [in] */ int nType,
            /* [in] */ ComBool fBuiltIn,
            /* [in] */ ComBool fModified,
            /* [in] */ ITsTextProps *pttp);
        
        HRESULT ( STDMETHODCALLTYPE *GetStyleRgch )( 
            IVwStylesheet * This,
            /* [in] */ int cch,
            /* [size_is][in] */ OLECHAR *prgchName,
            /* [retval][out] */ ITsTextProps **ppttp);
        
        HRESULT ( STDMETHODCALLTYPE *GetNextStyle )( 
            IVwStylesheet * This,
            /* [in] */ BSTR bstrName,
            /* [retval][out] */ BSTR *pbstrNext);
        
        HRESULT ( STDMETHODCALLTYPE *GetBasedOn )( 
            IVwStylesheet * This,
            /* [in] */ BSTR bstrName,
            /* [retval][out] */ BSTR *pbstrBasedOn);
        
        HRESULT ( STDMETHODCALLTYPE *GetType )( 
            IVwStylesheet * This,
            /* [in] */ BSTR bstrName,
            /* [retval][out] */ int *pnType);
        
        HRESULT ( STDMETHODCALLTYPE *GetContext )( 
            IVwStylesheet * This,
            /* [in] */ BSTR bstrName,
            /* [retval][out] */ int *pnContext);
        
        HRESULT ( STDMETHODCALLTYPE *IsBuiltIn )( 
            IVwStylesheet * This,
            /* [in] */ BSTR bstrName,
            /* [retval][out] */ ComBool *pfBuiltIn);
        
        HRESULT ( STDMETHODCALLTYPE *IsModified )( 
            IVwStylesheet * This,
            /* [in] */ BSTR bstrName,
            /* [retval][out] */ ComBool *pfModified);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_DataAccess )( 
            IVwStylesheet * This,
            /* [retval][out] */ ISilDataAccess **ppsda);
        
        HRESULT ( STDMETHODCALLTYPE *MakeNewStyle )( 
            IVwStylesheet * This,
            /* [retval][out] */ HVO *phvoNewStyle);
        
        HRESULT ( STDMETHODCALLTYPE *Delete )( 
            IVwStylesheet * This,
            /* [in] */ HVO hvoStyle);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_CStyles )( 
            IVwStylesheet * This,
            /* [retval][out] */ int *pcttp);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_NthStyle )( 
            IVwStylesheet * This,
            /* [in] */ int ihvo,
            /* [retval][out] */ HVO *phvo);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_NthStyleName )( 
            IVwStylesheet * This,
            /* [in] */ int ihvo,
            /* [retval][out] */ BSTR *pbstrStyleName);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_NormalFontStyle )( 
            IVwStylesheet * This,
            /* [retval][out] */ ITsTextProps **ppttp);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsStyleProtected )( 
            IVwStylesheet * This,
            /* [in] */ BSTR bstrName,
            /* [retval][out] */ ComBool *pfProtected);
        
        HRESULT ( STDMETHODCALLTYPE *CacheProps )( 
            IVwStylesheet * This,
            /* [in] */ int cch,
            /* [size_is][in] */ OLECHAR *prgchName,
            /* [in] */ HVO hvoStyle,
            /* [in] */ ITsTextProps *pttp);
        
        END_INTERFACE
    } IVwStylesheetVtbl;

    interface IVwStylesheet
    {
        CONST_VTBL struct IVwStylesheetVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IVwStylesheet_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IVwStylesheet_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IVwStylesheet_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IVwStylesheet_GetDefaultBasedOnStyleName(This,pbstrNormal)	\
    ( (This)->lpVtbl -> GetDefaultBasedOnStyleName(This,pbstrNormal) ) 

#define IVwStylesheet_GetDefaultStyleForContext(This,nContext,fCharStyle,pbstrStyleName)	\
    ( (This)->lpVtbl -> GetDefaultStyleForContext(This,nContext,fCharStyle,pbstrStyleName) ) 

#define IVwStylesheet_PutStyle(This,bstrName,bstrUsage,hvoStyle,hvoBasedOn,hvoNext,nType,fBuiltIn,fModified,pttp)	\
    ( (This)->lpVtbl -> PutStyle(This,bstrName,bstrUsage,hvoStyle,hvoBasedOn,hvoNext,nType,fBuiltIn,fModified,pttp) ) 

#define IVwStylesheet_GetStyleRgch(This,cch,prgchName,ppttp)	\
    ( (This)->lpVtbl -> GetStyleRgch(This,cch,prgchName,ppttp) ) 

#define IVwStylesheet_GetNextStyle(This,bstrName,pbstrNext)	\
    ( (This)->lpVtbl -> GetNextStyle(This,bstrName,pbstrNext) ) 

#define IVwStylesheet_GetBasedOn(This,bstrName,pbstrBasedOn)	\
    ( (This)->lpVtbl -> GetBasedOn(This,bstrName,pbstrBasedOn) ) 

#define IVwStylesheet_GetType(This,bstrName,pnType)	\
    ( (This)->lpVtbl -> GetType(This,bstrName,pnType) ) 

#define IVwStylesheet_GetContext(This,bstrName,pnContext)	\
    ( (This)->lpVtbl -> GetContext(This,bstrName,pnContext) ) 

#define IVwStylesheet_IsBuiltIn(This,bstrName,pfBuiltIn)	\
    ( (This)->lpVtbl -> IsBuiltIn(This,bstrName,pfBuiltIn) ) 

#define IVwStylesheet_IsModified(This,bstrName,pfModified)	\
    ( (This)->lpVtbl -> IsModified(This,bstrName,pfModified) ) 

#define IVwStylesheet_get_DataAccess(This,ppsda)	\
    ( (This)->lpVtbl -> get_DataAccess(This,ppsda) ) 

#define IVwStylesheet_MakeNewStyle(This,phvoNewStyle)	\
    ( (This)->lpVtbl -> MakeNewStyle(This,phvoNewStyle) ) 

#define IVwStylesheet_Delete(This,hvoStyle)	\
    ( (This)->lpVtbl -> Delete(This,hvoStyle) ) 

#define IVwStylesheet_get_CStyles(This,pcttp)	\
    ( (This)->lpVtbl -> get_CStyles(This,pcttp) ) 

#define IVwStylesheet_get_NthStyle(This,ihvo,phvo)	\
    ( (This)->lpVtbl -> get_NthStyle(This,ihvo,phvo) ) 

#define IVwStylesheet_get_NthStyleName(This,ihvo,pbstrStyleName)	\
    ( (This)->lpVtbl -> get_NthStyleName(This,ihvo,pbstrStyleName) ) 

#define IVwStylesheet_get_NormalFontStyle(This,ppttp)	\
    ( (This)->lpVtbl -> get_NormalFontStyle(This,ppttp) ) 

#define IVwStylesheet_get_IsStyleProtected(This,bstrName,pfProtected)	\
    ( (This)->lpVtbl -> get_IsStyleProtected(This,bstrName,pfProtected) ) 

#define IVwStylesheet_CacheProps(This,cch,prgchName,hvoStyle,pttp)	\
    ( (This)->lpVtbl -> CacheProps(This,cch,prgchName,hvoStyle,pttp) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IVwStylesheet_INTERFACE_DEFINED__ */


#ifndef __ISimpleInit_INTERFACE_DEFINED__
#define __ISimpleInit_INTERFACE_DEFINED__

/* interface ISimpleInit */
/* [unique][object][uuid] */ 


#define IID_ISimpleInit __uuidof(ISimpleInit)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("6433D19E-2DA2-4041-B202-DB118EE1694D")
    ISimpleInit : public IUnknown
    {
    public:
        virtual /* [restricted] */ HRESULT STDMETHODCALLTYPE InitNew( 
            /* [size_is][in] */ const BYTE *prgb,
            /* [in] */ int cb) = 0;
        
        virtual /* [restricted][propget] */ HRESULT STDMETHODCALLTYPE get_InitializationData( 
            /* [retval][out] */ BSTR *pbstr) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISimpleInitVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISimpleInit * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISimpleInit * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISimpleInit * This);
        
        /* [restricted] */ HRESULT ( STDMETHODCALLTYPE *InitNew )( 
            ISimpleInit * This,
            /* [size_is][in] */ const BYTE *prgb,
            /* [in] */ int cb);
        
        /* [restricted][propget] */ HRESULT ( STDMETHODCALLTYPE *get_InitializationData )( 
            ISimpleInit * This,
            /* [retval][out] */ BSTR *pbstr);
        
        END_INTERFACE
    } ISimpleInitVtbl;

    interface ISimpleInit
    {
        CONST_VTBL struct ISimpleInitVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISimpleInit_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISimpleInit_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISimpleInit_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISimpleInit_InitNew(This,prgb,cb)	\
    ( (This)->lpVtbl -> InitNew(This,prgb,cb) ) 

#define ISimpleInit_get_InitializationData(This,pbstr)	\
    ( (This)->lpVtbl -> get_InitializationData(This,pbstr) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISimpleInit_INTERFACE_DEFINED__ */


#ifndef __ICheckWord_INTERFACE_DEFINED__
#define __ICheckWord_INTERFACE_DEFINED__

/* interface ICheckWord */
/* [unique][object][uuid] */ 


#define IID_ICheckWord __uuidof(ICheckWord)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("69F4D944-C786-47EC-94F7-15193EED6758")
    ICheckWord : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Check( 
            /* [in] */ LPCOLESTR pszWord,
            /* [retval][out] */ ComBool *pfCorrect) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ICheckWordVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ICheckWord * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ICheckWord * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ICheckWord * This);
        
        HRESULT ( STDMETHODCALLTYPE *Check )( 
            ICheckWord * This,
            /* [in] */ LPCOLESTR pszWord,
            /* [retval][out] */ ComBool *pfCorrect);
        
        END_INTERFACE
    } ICheckWordVtbl;

    interface ICheckWord
    {
        CONST_VTBL struct ICheckWordVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ICheckWord_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ICheckWord_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ICheckWord_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ICheckWord_Check(This,pszWord,pfCorrect)	\
    ( (This)->lpVtbl -> Check(This,pszWord,pfCorrect) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ICheckWord_INTERFACE_DEFINED__ */


#ifndef __IGetSpellChecker_INTERFACE_DEFINED__
#define __IGetSpellChecker_INTERFACE_DEFINED__

/* interface IGetSpellChecker */
/* [unique][object][uuid] */ 


#define IID_IGetSpellChecker __uuidof(IGetSpellChecker)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("F0A60670-D280-45EA-A5C5-F0B84C027EFC")
    IGetSpellChecker : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetChecker( 
            /* [in] */ LPCOLESTR pszDictId,
            /* [retval][out] */ ICheckWord **pcw) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IGetSpellCheckerVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IGetSpellChecker * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IGetSpellChecker * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IGetSpellChecker * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetChecker )( 
            IGetSpellChecker * This,
            /* [in] */ LPCOLESTR pszDictId,
            /* [retval][out] */ ICheckWord **pcw);
        
        END_INTERFACE
    } IGetSpellCheckerVtbl;

    interface IGetSpellChecker
    {
        CONST_VTBL struct IGetSpellCheckerVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IGetSpellChecker_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IGetSpellChecker_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IGetSpellChecker_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IGetSpellChecker_GetChecker(This,pszDictId,pcw)	\
    ( (This)->lpVtbl -> GetChecker(This,pszDictId,pcw) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IGetSpellChecker_INTERFACE_DEFINED__ */


#ifndef __IVwNotifyChange_INTERFACE_DEFINED__
#define __IVwNotifyChange_INTERFACE_DEFINED__

/* interface IVwNotifyChange */
/* [unique][object][uuid] */ 


#define IID_IVwNotifyChange __uuidof(IVwNotifyChange)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("6C456541-C2B6-11d3-8078-0000C0FB81B5")
    IVwNotifyChange : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE PropChanged( 
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [in] */ int ivMin,
            /* [in] */ int cvIns,
            /* [in] */ int cvDel) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IVwNotifyChangeVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IVwNotifyChange * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IVwNotifyChange * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IVwNotifyChange * This);
        
        HRESULT ( STDMETHODCALLTYPE *PropChanged )( 
            IVwNotifyChange * This,
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [in] */ int ivMin,
            /* [in] */ int cvIns,
            /* [in] */ int cvDel);
        
        END_INTERFACE
    } IVwNotifyChangeVtbl;

    interface IVwNotifyChange
    {
        CONST_VTBL struct IVwNotifyChangeVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IVwNotifyChange_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IVwNotifyChange_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IVwNotifyChange_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IVwNotifyChange_PropChanged(This,hvo,tag,ivMin,cvIns,cvDel)	\
    ( (This)->lpVtbl -> PropChanged(This,hvo,tag,ivMin,cvIns,cvDel) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IVwNotifyChange_INTERFACE_DEFINED__ */


#ifndef __IUndoAction_INTERFACE_DEFINED__
#define __IUndoAction_INTERFACE_DEFINED__

/* interface IUndoAction */
/* [unique][object][uuid] */ 


#define IID_IUndoAction __uuidof(IUndoAction)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("B831F535-0D5F-42c8-BF9F-7F5ECA2C4657")
    IUndoAction : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Undo( 
            /* [retval][out] */ ComBool *pfSuccess) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Redo( 
            /* [retval][out] */ ComBool *pfSuccess) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Commit( void) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsDataChange( 
            /* [retval][out] */ ComBool *pfRet) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsRedoable( 
            /* [retval][out] */ ComBool *pfRet) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_SuppressNotification( 
            /* [in] */ ComBool fSuppress) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IUndoActionVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IUndoAction * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IUndoAction * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IUndoAction * This);
        
        HRESULT ( STDMETHODCALLTYPE *Undo )( 
            IUndoAction * This,
            /* [retval][out] */ ComBool *pfSuccess);
        
        HRESULT ( STDMETHODCALLTYPE *Redo )( 
            IUndoAction * This,
            /* [retval][out] */ ComBool *pfSuccess);
        
        HRESULT ( STDMETHODCALLTYPE *Commit )( 
            IUndoAction * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsDataChange )( 
            IUndoAction * This,
            /* [retval][out] */ ComBool *pfRet);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsRedoable )( 
            IUndoAction * This,
            /* [retval][out] */ ComBool *pfRet);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_SuppressNotification )( 
            IUndoAction * This,
            /* [in] */ ComBool fSuppress);
        
        END_INTERFACE
    } IUndoActionVtbl;

    interface IUndoAction
    {
        CONST_VTBL struct IUndoActionVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IUndoAction_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IUndoAction_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IUndoAction_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IUndoAction_Undo(This,pfSuccess)	\
    ( (This)->lpVtbl -> Undo(This,pfSuccess) ) 

#define IUndoAction_Redo(This,pfSuccess)	\
    ( (This)->lpVtbl -> Redo(This,pfSuccess) ) 

#define IUndoAction_Commit(This)	\
    ( (This)->lpVtbl -> Commit(This) ) 

#define IUndoAction_get_IsDataChange(This,pfRet)	\
    ( (This)->lpVtbl -> get_IsDataChange(This,pfRet) ) 

#define IUndoAction_get_IsRedoable(This,pfRet)	\
    ( (This)->lpVtbl -> get_IsRedoable(This,pfRet) ) 

#define IUndoAction_put_SuppressNotification(This,fSuppress)	\
    ( (This)->lpVtbl -> put_SuppressNotification(This,fSuppress) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IUndoAction_INTERFACE_DEFINED__ */


#ifndef __IActionHandler_INTERFACE_DEFINED__
#define __IActionHandler_INTERFACE_DEFINED__

/* interface IActionHandler */
/* [unique][object][uuid] */ 


#define IID_IActionHandler __uuidof(IActionHandler)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("7E8BC421-4CB2-4CF9-8C4C-73A5FD87CA7A")
    IActionHandler : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE BeginUndoTask( 
            /* [in] */ BSTR bstrUndo,
            /* [in] */ BSTR bstrRedo) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EndUndoTask( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ContinueUndoTask( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EndOuterUndoTask( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE BreakUndoTask( 
            /* [in] */ BSTR bstrUndo,
            /* [in] */ BSTR bstrRedo) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE BeginNonUndoableTask( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EndNonUndoableTask( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CreateMarkIfNeeded( 
            /* [in] */ ComBool fCreateMark) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE StartSeq( 
            /* [in] */ BSTR bstrUndo,
            /* [in] */ BSTR bstrRedo,
            /* [in] */ IUndoAction *puact) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AddAction( 
            /* [in] */ IUndoAction *puact) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetUndoText( 
            /* [retval][out] */ BSTR *pbstrUndoText) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetUndoTextN( 
            /* [in] */ int iAct,
            /* [retval][out] */ BSTR *pbstrUndoText) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetRedoText( 
            /* [retval][out] */ BSTR *pbstrRedoText) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetRedoTextN( 
            /* [in] */ int iAct,
            /* [retval][out] */ BSTR *pbstrRedoText) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CanUndo( 
            /* [retval][out] */ ComBool *pfCanUndo) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CanRedo( 
            /* [retval][out] */ ComBool *pfCanRedo) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Undo( 
            /* [retval][out] */ UndoResult *pures) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Redo( 
            /* [retval][out] */ UndoResult *pures) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Rollback( 
            /* [in] */ int nDepth) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_CurrentDepth( 
            /* [retval][out] */ int *pnDepth) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Commit( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Close( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Mark( 
            /* [retval][out] */ int *phMark) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CollapseToMark( 
            /* [in] */ int hMark,
            /* [in] */ BSTR bstrUndo,
            /* [in] */ BSTR bstrRedo,
            /* [retval][out] */ ComBool *pf) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DiscardToMark( 
            /* [in] */ int hMark) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_TopMarkHandle( 
            /* [retval][out] */ int *phMark) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_TasksSinceMark( 
            /* [in] */ ComBool fUndo,
            /* [retval][out] */ ComBool *pf) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_UndoableActionCount( 
            /* [retval][out] */ int *pcAct) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_UndoableSequenceCount( 
            /* [retval][out] */ int *pcSeq) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_RedoableSequenceCount( 
            /* [retval][out] */ int *pcSeq) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsUndoOrRedoInProgress( 
            /* [retval][out] */ ComBool *pfInProgress) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_SuppressSelections( 
            /* [retval][out] */ ComBool *pfSupressSel) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IActionHandlerVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IActionHandler * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IActionHandler * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IActionHandler * This);
        
        HRESULT ( STDMETHODCALLTYPE *BeginUndoTask )( 
            IActionHandler * This,
            /* [in] */ BSTR bstrUndo,
            /* [in] */ BSTR bstrRedo);
        
        HRESULT ( STDMETHODCALLTYPE *EndUndoTask )( 
            IActionHandler * This);
        
        HRESULT ( STDMETHODCALLTYPE *ContinueUndoTask )( 
            IActionHandler * This);
        
        HRESULT ( STDMETHODCALLTYPE *EndOuterUndoTask )( 
            IActionHandler * This);
        
        HRESULT ( STDMETHODCALLTYPE *BreakUndoTask )( 
            IActionHandler * This,
            /* [in] */ BSTR bstrUndo,
            /* [in] */ BSTR bstrRedo);
        
        HRESULT ( STDMETHODCALLTYPE *BeginNonUndoableTask )( 
            IActionHandler * This);
        
        HRESULT ( STDMETHODCALLTYPE *EndNonUndoableTask )( 
            IActionHandler * This);
        
        HRESULT ( STDMETHODCALLTYPE *CreateMarkIfNeeded )( 
            IActionHandler * This,
            /* [in] */ ComBool fCreateMark);
        
        HRESULT ( STDMETHODCALLTYPE *StartSeq )( 
            IActionHandler * This,
            /* [in] */ BSTR bstrUndo,
            /* [in] */ BSTR bstrRedo,
            /* [in] */ IUndoAction *puact);
        
        HRESULT ( STDMETHODCALLTYPE *AddAction )( 
            IActionHandler * This,
            /* [in] */ IUndoAction *puact);
        
        HRESULT ( STDMETHODCALLTYPE *GetUndoText )( 
            IActionHandler * This,
            /* [retval][out] */ BSTR *pbstrUndoText);
        
        HRESULT ( STDMETHODCALLTYPE *GetUndoTextN )( 
            IActionHandler * This,
            /* [in] */ int iAct,
            /* [retval][out] */ BSTR *pbstrUndoText);
        
        HRESULT ( STDMETHODCALLTYPE *GetRedoText )( 
            IActionHandler * This,
            /* [retval][out] */ BSTR *pbstrRedoText);
        
        HRESULT ( STDMETHODCALLTYPE *GetRedoTextN )( 
            IActionHandler * This,
            /* [in] */ int iAct,
            /* [retval][out] */ BSTR *pbstrRedoText);
        
        HRESULT ( STDMETHODCALLTYPE *CanUndo )( 
            IActionHandler * This,
            /* [retval][out] */ ComBool *pfCanUndo);
        
        HRESULT ( STDMETHODCALLTYPE *CanRedo )( 
            IActionHandler * This,
            /* [retval][out] */ ComBool *pfCanRedo);
        
        HRESULT ( STDMETHODCALLTYPE *Undo )( 
            IActionHandler * This,
            /* [retval][out] */ UndoResult *pures);
        
        HRESULT ( STDMETHODCALLTYPE *Redo )( 
            IActionHandler * This,
            /* [retval][out] */ UndoResult *pures);
        
        HRESULT ( STDMETHODCALLTYPE *Rollback )( 
            IActionHandler * This,
            /* [in] */ int nDepth);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_CurrentDepth )( 
            IActionHandler * This,
            /* [retval][out] */ int *pnDepth);
        
        HRESULT ( STDMETHODCALLTYPE *Commit )( 
            IActionHandler * This);
        
        HRESULT ( STDMETHODCALLTYPE *Close )( 
            IActionHandler * This);
        
        HRESULT ( STDMETHODCALLTYPE *Mark )( 
            IActionHandler * This,
            /* [retval][out] */ int *phMark);
        
        HRESULT ( STDMETHODCALLTYPE *CollapseToMark )( 
            IActionHandler * This,
            /* [in] */ int hMark,
            /* [in] */ BSTR bstrUndo,
            /* [in] */ BSTR bstrRedo,
            /* [retval][out] */ ComBool *pf);
        
        HRESULT ( STDMETHODCALLTYPE *DiscardToMark )( 
            IActionHandler * This,
            /* [in] */ int hMark);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_TopMarkHandle )( 
            IActionHandler * This,
            /* [retval][out] */ int *phMark);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_TasksSinceMark )( 
            IActionHandler * This,
            /* [in] */ ComBool fUndo,
            /* [retval][out] */ ComBool *pf);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_UndoableActionCount )( 
            IActionHandler * This,
            /* [retval][out] */ int *pcAct);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_UndoableSequenceCount )( 
            IActionHandler * This,
            /* [retval][out] */ int *pcSeq);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_RedoableSequenceCount )( 
            IActionHandler * This,
            /* [retval][out] */ int *pcSeq);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsUndoOrRedoInProgress )( 
            IActionHandler * This,
            /* [retval][out] */ ComBool *pfInProgress);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_SuppressSelections )( 
            IActionHandler * This,
            /* [retval][out] */ ComBool *pfSupressSel);
        
        END_INTERFACE
    } IActionHandlerVtbl;

    interface IActionHandler
    {
        CONST_VTBL struct IActionHandlerVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IActionHandler_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IActionHandler_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IActionHandler_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IActionHandler_BeginUndoTask(This,bstrUndo,bstrRedo)	\
    ( (This)->lpVtbl -> BeginUndoTask(This,bstrUndo,bstrRedo) ) 

#define IActionHandler_EndUndoTask(This)	\
    ( (This)->lpVtbl -> EndUndoTask(This) ) 

#define IActionHandler_ContinueUndoTask(This)	\
    ( (This)->lpVtbl -> ContinueUndoTask(This) ) 

#define IActionHandler_EndOuterUndoTask(This)	\
    ( (This)->lpVtbl -> EndOuterUndoTask(This) ) 

#define IActionHandler_BreakUndoTask(This,bstrUndo,bstrRedo)	\
    ( (This)->lpVtbl -> BreakUndoTask(This,bstrUndo,bstrRedo) ) 

#define IActionHandler_BeginNonUndoableTask(This)	\
    ( (This)->lpVtbl -> BeginNonUndoableTask(This) ) 

#define IActionHandler_EndNonUndoableTask(This)	\
    ( (This)->lpVtbl -> EndNonUndoableTask(This) ) 

#define IActionHandler_CreateMarkIfNeeded(This,fCreateMark)	\
    ( (This)->lpVtbl -> CreateMarkIfNeeded(This,fCreateMark) ) 

#define IActionHandler_StartSeq(This,bstrUndo,bstrRedo,puact)	\
    ( (This)->lpVtbl -> StartSeq(This,bstrUndo,bstrRedo,puact) ) 

#define IActionHandler_AddAction(This,puact)	\
    ( (This)->lpVtbl -> AddAction(This,puact) ) 

#define IActionHandler_GetUndoText(This,pbstrUndoText)	\
    ( (This)->lpVtbl -> GetUndoText(This,pbstrUndoText) ) 

#define IActionHandler_GetUndoTextN(This,iAct,pbstrUndoText)	\
    ( (This)->lpVtbl -> GetUndoTextN(This,iAct,pbstrUndoText) ) 

#define IActionHandler_GetRedoText(This,pbstrRedoText)	\
    ( (This)->lpVtbl -> GetRedoText(This,pbstrRedoText) ) 

#define IActionHandler_GetRedoTextN(This,iAct,pbstrRedoText)	\
    ( (This)->lpVtbl -> GetRedoTextN(This,iAct,pbstrRedoText) ) 

#define IActionHandler_CanUndo(This,pfCanUndo)	\
    ( (This)->lpVtbl -> CanUndo(This,pfCanUndo) ) 

#define IActionHandler_CanRedo(This,pfCanRedo)	\
    ( (This)->lpVtbl -> CanRedo(This,pfCanRedo) ) 

#define IActionHandler_Undo(This,pures)	\
    ( (This)->lpVtbl -> Undo(This,pures) ) 

#define IActionHandler_Redo(This,pures)	\
    ( (This)->lpVtbl -> Redo(This,pures) ) 

#define IActionHandler_Rollback(This,nDepth)	\
    ( (This)->lpVtbl -> Rollback(This,nDepth) ) 

#define IActionHandler_get_CurrentDepth(This,pnDepth)	\
    ( (This)->lpVtbl -> get_CurrentDepth(This,pnDepth) ) 

#define IActionHandler_Commit(This)	\
    ( (This)->lpVtbl -> Commit(This) ) 

#define IActionHandler_Close(This)	\
    ( (This)->lpVtbl -> Close(This) ) 

#define IActionHandler_Mark(This,phMark)	\
    ( (This)->lpVtbl -> Mark(This,phMark) ) 

#define IActionHandler_CollapseToMark(This,hMark,bstrUndo,bstrRedo,pf)	\
    ( (This)->lpVtbl -> CollapseToMark(This,hMark,bstrUndo,bstrRedo,pf) ) 

#define IActionHandler_DiscardToMark(This,hMark)	\
    ( (This)->lpVtbl -> DiscardToMark(This,hMark) ) 

#define IActionHandler_get_TopMarkHandle(This,phMark)	\
    ( (This)->lpVtbl -> get_TopMarkHandle(This,phMark) ) 

#define IActionHandler_get_TasksSinceMark(This,fUndo,pf)	\
    ( (This)->lpVtbl -> get_TasksSinceMark(This,fUndo,pf) ) 

#define IActionHandler_get_UndoableActionCount(This,pcAct)	\
    ( (This)->lpVtbl -> get_UndoableActionCount(This,pcAct) ) 

#define IActionHandler_get_UndoableSequenceCount(This,pcSeq)	\
    ( (This)->lpVtbl -> get_UndoableSequenceCount(This,pcSeq) ) 

#define IActionHandler_get_RedoableSequenceCount(This,pcSeq)	\
    ( (This)->lpVtbl -> get_RedoableSequenceCount(This,pcSeq) ) 

#define IActionHandler_get_IsUndoOrRedoInProgress(This,pfInProgress)	\
    ( (This)->lpVtbl -> get_IsUndoOrRedoInProgress(This,pfInProgress) ) 

#define IActionHandler_get_SuppressSelections(This,pfSupressSel)	\
    ( (This)->lpVtbl -> get_SuppressSelections(This,pfSupressSel) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IActionHandler_INTERFACE_DEFINED__ */


#define CLSID_ActionHandler __uuidof(ActionHandler)

#ifdef __cplusplus

class DECLSPEC_UUID("CF0F1C0B-0E44-4C1E-9912-2048ED12C2B4")
ActionHandler;
#endif

#ifndef __ISilDataAccess_INTERFACE_DEFINED__
#define __ISilDataAccess_INTERFACE_DEFINED__

/* interface ISilDataAccess */
/* [unique][object][uuid] */ 


#define IID_ISilDataAccess __uuidof(ISilDataAccess)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("26E6E70E-53EB-4372-96F1-0F4707CCD1EB")
    ISilDataAccess : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ObjectProp( 
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [retval][out] */ HVO *phvo) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_VecItem( 
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [in] */ int index,
            /* [retval][out] */ HVO *phvo) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_VecSize( 
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [retval][out] */ int *pchvo) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_VecSizeAssumeCached( 
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [retval][out] */ int *pchvo) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE VecProp( 
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [in] */ int chvoMax,
            /* [out] */ int *pchvo,
            /* [length_is][size_is][out] */ HVO *prghvo) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE BinaryPropRgb( 
            /* [in] */ HVO obj,
            /* [in] */ PropTag tag,
            /* [size_is][out] */ byte *prgb,
            /* [in] */ int cbMax,
            /* [out] */ int *pcb) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_GuidProp( 
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [retval][out] */ GUID *puid) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ObjFromGuid( 
            /* [in] */ GUID uid,
            /* [retval][out] */ HVO *pHvo) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IntProp( 
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [retval][out] */ int *pn) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Int64Prop( 
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [retval][out] */ __int64 *plln) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_BooleanProp( 
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [retval][out] */ ComBool *pn) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_MultiStringAlt( 
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [in] */ int ws,
            /* [retval][out] */ ITsString **pptss) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_MultiStringProp( 
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [retval][out] */ ITsMultiString **pptms) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Prop( 
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [retval][out] */ VARIANT *pvar) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_StringProp( 
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [retval][out] */ ITsString **pptss) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_TimeProp( 
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [retval][out] */ __int64 *ptim) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_UnicodeProp( 
            /* [in] */ HVO obj,
            /* [in] */ PropTag tag,
            /* [retval][out] */ BSTR *pbstr) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_UnicodeProp( 
            /* [in] */ HVO obj,
            /* [in] */ PropTag tag,
            /* [in] */ BSTR bstr) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE UnicodePropRgch( 
            /* [in] */ HVO obj,
            /* [in] */ PropTag tag,
            /* [size_is][out] */ OLECHAR *prgch,
            /* [in] */ int cchMax,
            /* [out] */ int *pcch) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_UnknownProp( 
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [retval][out] */ IUnknown **ppunk) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE BeginUndoTask( 
            /* [in] */ BSTR bstrUndo,
            /* [in] */ BSTR bstrRedo) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EndUndoTask( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ContinueUndoTask( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EndOuterUndoTask( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Rollback( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE BreakUndoTask( 
            /* [in] */ BSTR bstrUndo,
            /* [in] */ BSTR bstrRedo) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE BeginNonUndoableTask( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EndNonUndoableTask( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetActionHandler( 
            /* [retval][out] */ IActionHandler **ppacth) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetActionHandler( 
            /* [in] */ IActionHandler *pacth) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DeleteObj( 
            /* [in] */ HVO hvoObj) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DeleteObjOwner( 
            /* [in] */ HVO hvoOwner,
            /* [in] */ HVO hvoObj,
            /* [in] */ PropTag tag,
            /* [in] */ int ihvo) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE InsertNew( 
            /* [in] */ HVO hvoObj,
            /* [in] */ PropTag tag,
            /* [in] */ int ihvo,
            /* [in] */ int chvo,
            /* [in] */ IVwStylesheet *pss) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE MakeNewObject( 
            /* [in] */ int clid,
            /* [in] */ HVO hvoOwner,
            /* [in] */ PropTag tag,
            /* [in] */ int ord,
            /* [retval][out] */ HVO *phvoNew) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE MoveOwnSeq( 
            /* [in] */ HVO hvoSrcOwner,
            /* [in] */ PropTag tagSrc,
            /* [in] */ int ihvoStart,
            /* [in] */ int ihvoEnd,
            /* [in] */ HVO hvoDstOwner,
            /* [in] */ PropTag tagDst,
            /* [in] */ int ihvoDstStart) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE MoveOwn( 
            /* [in] */ HVO hvoSrcOwner,
            /* [in] */ PropTag tagSrc,
            /* [in] */ HVO hvo,
            /* [in] */ HVO hvoDstOwner,
            /* [in] */ PropTag tagDst,
            /* [in] */ int ihvoDstStart) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Replace( 
            /* [in] */ HVO hvoObj,
            /* [in] */ PropTag tag,
            /* [in] */ int ihvoMin,
            /* [in] */ int ihvoLim,
            /* [size_is][in] */ HVO *prghvo,
            /* [in] */ int chvo) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetObjProp( 
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [in] */ HVO hvoObj) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE RemoveObjRefs( 
            /* [in] */ HVO hvo) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetBinary( 
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [size_is][in] */ byte *prgb,
            /* [in] */ int cb) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetGuid( 
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [in] */ GUID uid) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetInt( 
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [in] */ int n) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetInt64( 
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [in] */ __int64 lln) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetBoolean( 
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [in] */ ComBool n) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetMultiStringAlt( 
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [in] */ int ws,
            /* [in] */ ITsString *ptss) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetString( 
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [in] */ ITsString *ptss) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetTime( 
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [in] */ __int64 lln) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetUnicode( 
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [size_is][in] */ OLECHAR *prgch,
            /* [in] */ int cch) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetUnknown( 
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [in] */ IUnknown *punk) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AddNotification( 
            /* [in] */ IVwNotifyChange *pnchng) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE PropChanged( 
            /* [in] */ IVwNotifyChange *pnchng,
            /* [in] */ int pct,
            /* [in] */ HVO hvo,
            /* [in] */ int tag,
            /* [in] */ int ivMin,
            /* [in] */ int cvIns,
            /* [in] */ int cvDel) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE RemoveNotification( 
            /* [in] */ IVwNotifyChange *pnchng) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetDisplayIndex( 
            /* [in] */ HVO hvoOwn,
            /* [in] */ int tag,
            /* [in] */ int ihvo,
            /* [retval][out] */ int *ihvoDisp) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_WritingSystemFactory( 
            /* [retval][out] */ ILgWritingSystemFactory **ppwsf) = 0;
        
        virtual /* [propputref] */ HRESULT STDMETHODCALLTYPE putref_WritingSystemFactory( 
            /* [in] */ ILgWritingSystemFactory *pwsf) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_WritingSystemsOfInterest( 
            /* [in] */ int cwsMax,
            /* [size_is][out] */ int *pws,
            /* [retval][out] */ int *pcws) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE InsertRelExtra( 
            /* [in] */ HVO hvoSrc,
            /* [in] */ PropTag tag,
            /* [in] */ int ihvo,
            /* [in] */ HVO hvoDst,
            /* [in] */ BSTR bstrExtra) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE UpdateRelExtra( 
            /* [in] */ HVO hvoSrc,
            /* [in] */ PropTag tag,
            /* [in] */ int ihvo,
            /* [in] */ BSTR bstrExtra) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetRelExtra( 
            /* [in] */ HVO hvoSrc,
            /* [in] */ PropTag tag,
            /* [in] */ int ihvo,
            /* [retval][out] */ BSTR *pbstrExtra) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsPropInCache( 
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [in] */ int cpt,
            /* [in] */ int ws,
            /* [retval][out] */ ComBool *pfCached) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE IsDirty( 
            /* [retval][out] */ ComBool *pf) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ClearDirty( void) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_MetaDataCache( 
            /* [retval][out] */ IFwMetaDataCache **ppmdc) = 0;
        
        virtual /* [propputref] */ HRESULT STDMETHODCALLTYPE putref_MetaDataCache( 
            /* [in] */ IFwMetaDataCache *pmdc) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsValidObject( 
            /* [in] */ HVO hvo,
            /* [retval][out] */ ComBool *pfValid) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsDummyId( 
            /* [in] */ HVO hvo,
            /* [retval][out] */ ComBool *pfDummy) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetObjIndex( 
            /* [in] */ HVO hvoOwn,
            /* [in] */ int flid,
            /* [in] */ HVO hvo,
            /* [retval][out] */ int *ihvo) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetOutlineNumber( 
            /* [in] */ HVO hvo,
            /* [in] */ int flid,
            /* [in] */ ComBool fFinPer,
            /* [retval][out] */ BSTR *pbstr) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE MoveString( 
            /* [in] */ int hvoSource,
            /* [in] */ PropTag flidSrc,
            /* [in] */ int wsSrc,
            /* [in] */ int ichMin,
            /* [in] */ int ichLim,
            /* [in] */ HVO hvoDst,
            /* [in] */ PropTag flidDst,
            /* [in] */ int wsDst,
            /* [in] */ int ichDest,
            /* [in] */ ComBool fDstIsNew) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ISilDataAccessVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ISilDataAccess * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ISilDataAccess * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ISilDataAccess * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ObjectProp )( 
            ISilDataAccess * This,
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [retval][out] */ HVO *phvo);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_VecItem )( 
            ISilDataAccess * This,
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [in] */ int index,
            /* [retval][out] */ HVO *phvo);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_VecSize )( 
            ISilDataAccess * This,
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [retval][out] */ int *pchvo);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_VecSizeAssumeCached )( 
            ISilDataAccess * This,
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [retval][out] */ int *pchvo);
        
        HRESULT ( STDMETHODCALLTYPE *VecProp )( 
            ISilDataAccess * This,
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [in] */ int chvoMax,
            /* [out] */ int *pchvo,
            /* [length_is][size_is][out] */ HVO *prghvo);
        
        HRESULT ( STDMETHODCALLTYPE *BinaryPropRgb )( 
            ISilDataAccess * This,
            /* [in] */ HVO obj,
            /* [in] */ PropTag tag,
            /* [size_is][out] */ byte *prgb,
            /* [in] */ int cbMax,
            /* [out] */ int *pcb);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_GuidProp )( 
            ISilDataAccess * This,
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [retval][out] */ GUID *puid);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ObjFromGuid )( 
            ISilDataAccess * This,
            /* [in] */ GUID uid,
            /* [retval][out] */ HVO *pHvo);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IntProp )( 
            ISilDataAccess * This,
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [retval][out] */ int *pn);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Int64Prop )( 
            ISilDataAccess * This,
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [retval][out] */ __int64 *plln);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_BooleanProp )( 
            ISilDataAccess * This,
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [retval][out] */ ComBool *pn);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_MultiStringAlt )( 
            ISilDataAccess * This,
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [in] */ int ws,
            /* [retval][out] */ ITsString **pptss);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_MultiStringProp )( 
            ISilDataAccess * This,
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [retval][out] */ ITsMultiString **pptms);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Prop )( 
            ISilDataAccess * This,
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [retval][out] */ VARIANT *pvar);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_StringProp )( 
            ISilDataAccess * This,
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [retval][out] */ ITsString **pptss);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_TimeProp )( 
            ISilDataAccess * This,
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [retval][out] */ __int64 *ptim);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_UnicodeProp )( 
            ISilDataAccess * This,
            /* [in] */ HVO obj,
            /* [in] */ PropTag tag,
            /* [retval][out] */ BSTR *pbstr);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_UnicodeProp )( 
            ISilDataAccess * This,
            /* [in] */ HVO obj,
            /* [in] */ PropTag tag,
            /* [in] */ BSTR bstr);
        
        HRESULT ( STDMETHODCALLTYPE *UnicodePropRgch )( 
            ISilDataAccess * This,
            /* [in] */ HVO obj,
            /* [in] */ PropTag tag,
            /* [size_is][out] */ OLECHAR *prgch,
            /* [in] */ int cchMax,
            /* [out] */ int *pcch);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_UnknownProp )( 
            ISilDataAccess * This,
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [retval][out] */ IUnknown **ppunk);
        
        HRESULT ( STDMETHODCALLTYPE *BeginUndoTask )( 
            ISilDataAccess * This,
            /* [in] */ BSTR bstrUndo,
            /* [in] */ BSTR bstrRedo);
        
        HRESULT ( STDMETHODCALLTYPE *EndUndoTask )( 
            ISilDataAccess * This);
        
        HRESULT ( STDMETHODCALLTYPE *ContinueUndoTask )( 
            ISilDataAccess * This);
        
        HRESULT ( STDMETHODCALLTYPE *EndOuterUndoTask )( 
            ISilDataAccess * This);
        
        HRESULT ( STDMETHODCALLTYPE *Rollback )( 
            ISilDataAccess * This);
        
        HRESULT ( STDMETHODCALLTYPE *BreakUndoTask )( 
            ISilDataAccess * This,
            /* [in] */ BSTR bstrUndo,
            /* [in] */ BSTR bstrRedo);
        
        HRESULT ( STDMETHODCALLTYPE *BeginNonUndoableTask )( 
            ISilDataAccess * This);
        
        HRESULT ( STDMETHODCALLTYPE *EndNonUndoableTask )( 
            ISilDataAccess * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetActionHandler )( 
            ISilDataAccess * This,
            /* [retval][out] */ IActionHandler **ppacth);
        
        HRESULT ( STDMETHODCALLTYPE *SetActionHandler )( 
            ISilDataAccess * This,
            /* [in] */ IActionHandler *pacth);
        
        HRESULT ( STDMETHODCALLTYPE *DeleteObj )( 
            ISilDataAccess * This,
            /* [in] */ HVO hvoObj);
        
        HRESULT ( STDMETHODCALLTYPE *DeleteObjOwner )( 
            ISilDataAccess * This,
            /* [in] */ HVO hvoOwner,
            /* [in] */ HVO hvoObj,
            /* [in] */ PropTag tag,
            /* [in] */ int ihvo);
        
        HRESULT ( STDMETHODCALLTYPE *InsertNew )( 
            ISilDataAccess * This,
            /* [in] */ HVO hvoObj,
            /* [in] */ PropTag tag,
            /* [in] */ int ihvo,
            /* [in] */ int chvo,
            /* [in] */ IVwStylesheet *pss);
        
        HRESULT ( STDMETHODCALLTYPE *MakeNewObject )( 
            ISilDataAccess * This,
            /* [in] */ int clid,
            /* [in] */ HVO hvoOwner,
            /* [in] */ PropTag tag,
            /* [in] */ int ord,
            /* [retval][out] */ HVO *phvoNew);
        
        HRESULT ( STDMETHODCALLTYPE *MoveOwnSeq )( 
            ISilDataAccess * This,
            /* [in] */ HVO hvoSrcOwner,
            /* [in] */ PropTag tagSrc,
            /* [in] */ int ihvoStart,
            /* [in] */ int ihvoEnd,
            /* [in] */ HVO hvoDstOwner,
            /* [in] */ PropTag tagDst,
            /* [in] */ int ihvoDstStart);
        
        HRESULT ( STDMETHODCALLTYPE *MoveOwn )( 
            ISilDataAccess * This,
            /* [in] */ HVO hvoSrcOwner,
            /* [in] */ PropTag tagSrc,
            /* [in] */ HVO hvo,
            /* [in] */ HVO hvoDstOwner,
            /* [in] */ PropTag tagDst,
            /* [in] */ int ihvoDstStart);
        
        HRESULT ( STDMETHODCALLTYPE *Replace )( 
            ISilDataAccess * This,
            /* [in] */ HVO hvoObj,
            /* [in] */ PropTag tag,
            /* [in] */ int ihvoMin,
            /* [in] */ int ihvoLim,
            /* [size_is][in] */ HVO *prghvo,
            /* [in] */ int chvo);
        
        HRESULT ( STDMETHODCALLTYPE *SetObjProp )( 
            ISilDataAccess * This,
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [in] */ HVO hvoObj);
        
        HRESULT ( STDMETHODCALLTYPE *RemoveObjRefs )( 
            ISilDataAccess * This,
            /* [in] */ HVO hvo);
        
        HRESULT ( STDMETHODCALLTYPE *SetBinary )( 
            ISilDataAccess * This,
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [size_is][in] */ byte *prgb,
            /* [in] */ int cb);
        
        HRESULT ( STDMETHODCALLTYPE *SetGuid )( 
            ISilDataAccess * This,
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [in] */ GUID uid);
        
        HRESULT ( STDMETHODCALLTYPE *SetInt )( 
            ISilDataAccess * This,
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [in] */ int n);
        
        HRESULT ( STDMETHODCALLTYPE *SetInt64 )( 
            ISilDataAccess * This,
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [in] */ __int64 lln);
        
        HRESULT ( STDMETHODCALLTYPE *SetBoolean )( 
            ISilDataAccess * This,
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [in] */ ComBool n);
        
        HRESULT ( STDMETHODCALLTYPE *SetMultiStringAlt )( 
            ISilDataAccess * This,
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [in] */ int ws,
            /* [in] */ ITsString *ptss);
        
        HRESULT ( STDMETHODCALLTYPE *SetString )( 
            ISilDataAccess * This,
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [in] */ ITsString *ptss);
        
        HRESULT ( STDMETHODCALLTYPE *SetTime )( 
            ISilDataAccess * This,
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [in] */ __int64 lln);
        
        HRESULT ( STDMETHODCALLTYPE *SetUnicode )( 
            ISilDataAccess * This,
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [size_is][in] */ OLECHAR *prgch,
            /* [in] */ int cch);
        
        HRESULT ( STDMETHODCALLTYPE *SetUnknown )( 
            ISilDataAccess * This,
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [in] */ IUnknown *punk);
        
        HRESULT ( STDMETHODCALLTYPE *AddNotification )( 
            ISilDataAccess * This,
            /* [in] */ IVwNotifyChange *pnchng);
        
        HRESULT ( STDMETHODCALLTYPE *PropChanged )( 
            ISilDataAccess * This,
            /* [in] */ IVwNotifyChange *pnchng,
            /* [in] */ int pct,
            /* [in] */ HVO hvo,
            /* [in] */ int tag,
            /* [in] */ int ivMin,
            /* [in] */ int cvIns,
            /* [in] */ int cvDel);
        
        HRESULT ( STDMETHODCALLTYPE *RemoveNotification )( 
            ISilDataAccess * This,
            /* [in] */ IVwNotifyChange *pnchng);
        
        HRESULT ( STDMETHODCALLTYPE *GetDisplayIndex )( 
            ISilDataAccess * This,
            /* [in] */ HVO hvoOwn,
            /* [in] */ int tag,
            /* [in] */ int ihvo,
            /* [retval][out] */ int *ihvoDisp);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_WritingSystemFactory )( 
            ISilDataAccess * This,
            /* [retval][out] */ ILgWritingSystemFactory **ppwsf);
        
        /* [propputref] */ HRESULT ( STDMETHODCALLTYPE *putref_WritingSystemFactory )( 
            ISilDataAccess * This,
            /* [in] */ ILgWritingSystemFactory *pwsf);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_WritingSystemsOfInterest )( 
            ISilDataAccess * This,
            /* [in] */ int cwsMax,
            /* [size_is][out] */ int *pws,
            /* [retval][out] */ int *pcws);
        
        HRESULT ( STDMETHODCALLTYPE *InsertRelExtra )( 
            ISilDataAccess * This,
            /* [in] */ HVO hvoSrc,
            /* [in] */ PropTag tag,
            /* [in] */ int ihvo,
            /* [in] */ HVO hvoDst,
            /* [in] */ BSTR bstrExtra);
        
        HRESULT ( STDMETHODCALLTYPE *UpdateRelExtra )( 
            ISilDataAccess * This,
            /* [in] */ HVO hvoSrc,
            /* [in] */ PropTag tag,
            /* [in] */ int ihvo,
            /* [in] */ BSTR bstrExtra);
        
        HRESULT ( STDMETHODCALLTYPE *GetRelExtra )( 
            ISilDataAccess * This,
            /* [in] */ HVO hvoSrc,
            /* [in] */ PropTag tag,
            /* [in] */ int ihvo,
            /* [retval][out] */ BSTR *pbstrExtra);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsPropInCache )( 
            ISilDataAccess * This,
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [in] */ int cpt,
            /* [in] */ int ws,
            /* [retval][out] */ ComBool *pfCached);
        
        HRESULT ( STDMETHODCALLTYPE *IsDirty )( 
            ISilDataAccess * This,
            /* [retval][out] */ ComBool *pf);
        
        HRESULT ( STDMETHODCALLTYPE *ClearDirty )( 
            ISilDataAccess * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_MetaDataCache )( 
            ISilDataAccess * This,
            /* [retval][out] */ IFwMetaDataCache **ppmdc);
        
        /* [propputref] */ HRESULT ( STDMETHODCALLTYPE *putref_MetaDataCache )( 
            ISilDataAccess * This,
            /* [in] */ IFwMetaDataCache *pmdc);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsValidObject )( 
            ISilDataAccess * This,
            /* [in] */ HVO hvo,
            /* [retval][out] */ ComBool *pfValid);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsDummyId )( 
            ISilDataAccess * This,
            /* [in] */ HVO hvo,
            /* [retval][out] */ ComBool *pfDummy);
        
        HRESULT ( STDMETHODCALLTYPE *GetObjIndex )( 
            ISilDataAccess * This,
            /* [in] */ HVO hvoOwn,
            /* [in] */ int flid,
            /* [in] */ HVO hvo,
            /* [retval][out] */ int *ihvo);
        
        HRESULT ( STDMETHODCALLTYPE *GetOutlineNumber )( 
            ISilDataAccess * This,
            /* [in] */ HVO hvo,
            /* [in] */ int flid,
            /* [in] */ ComBool fFinPer,
            /* [retval][out] */ BSTR *pbstr);
        
        HRESULT ( STDMETHODCALLTYPE *MoveString )( 
            ISilDataAccess * This,
            /* [in] */ int hvoSource,
            /* [in] */ PropTag flidSrc,
            /* [in] */ int wsSrc,
            /* [in] */ int ichMin,
            /* [in] */ int ichLim,
            /* [in] */ HVO hvoDst,
            /* [in] */ PropTag flidDst,
            /* [in] */ int wsDst,
            /* [in] */ int ichDest,
            /* [in] */ ComBool fDstIsNew);
        
        END_INTERFACE
    } ISilDataAccessVtbl;

    interface ISilDataAccess
    {
        CONST_VTBL struct ISilDataAccessVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ISilDataAccess_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ISilDataAccess_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ISilDataAccess_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ISilDataAccess_get_ObjectProp(This,hvo,tag,phvo)	\
    ( (This)->lpVtbl -> get_ObjectProp(This,hvo,tag,phvo) ) 

#define ISilDataAccess_get_VecItem(This,hvo,tag,index,phvo)	\
    ( (This)->lpVtbl -> get_VecItem(This,hvo,tag,index,phvo) ) 

#define ISilDataAccess_get_VecSize(This,hvo,tag,pchvo)	\
    ( (This)->lpVtbl -> get_VecSize(This,hvo,tag,pchvo) ) 

#define ISilDataAccess_get_VecSizeAssumeCached(This,hvo,tag,pchvo)	\
    ( (This)->lpVtbl -> get_VecSizeAssumeCached(This,hvo,tag,pchvo) ) 

#define ISilDataAccess_VecProp(This,hvo,tag,chvoMax,pchvo,prghvo)	\
    ( (This)->lpVtbl -> VecProp(This,hvo,tag,chvoMax,pchvo,prghvo) ) 

#define ISilDataAccess_BinaryPropRgb(This,obj,tag,prgb,cbMax,pcb)	\
    ( (This)->lpVtbl -> BinaryPropRgb(This,obj,tag,prgb,cbMax,pcb) ) 

#define ISilDataAccess_get_GuidProp(This,hvo,tag,puid)	\
    ( (This)->lpVtbl -> get_GuidProp(This,hvo,tag,puid) ) 

#define ISilDataAccess_get_ObjFromGuid(This,uid,pHvo)	\
    ( (This)->lpVtbl -> get_ObjFromGuid(This,uid,pHvo) ) 

#define ISilDataAccess_get_IntProp(This,hvo,tag,pn)	\
    ( (This)->lpVtbl -> get_IntProp(This,hvo,tag,pn) ) 

#define ISilDataAccess_get_Int64Prop(This,hvo,tag,plln)	\
    ( (This)->lpVtbl -> get_Int64Prop(This,hvo,tag,plln) ) 

#define ISilDataAccess_get_BooleanProp(This,hvo,tag,pn)	\
    ( (This)->lpVtbl -> get_BooleanProp(This,hvo,tag,pn) ) 

#define ISilDataAccess_get_MultiStringAlt(This,hvo,tag,ws,pptss)	\
    ( (This)->lpVtbl -> get_MultiStringAlt(This,hvo,tag,ws,pptss) ) 

#define ISilDataAccess_get_MultiStringProp(This,hvo,tag,pptms)	\
    ( (This)->lpVtbl -> get_MultiStringProp(This,hvo,tag,pptms) ) 

#define ISilDataAccess_get_Prop(This,hvo,tag,pvar)	\
    ( (This)->lpVtbl -> get_Prop(This,hvo,tag,pvar) ) 

#define ISilDataAccess_get_StringProp(This,hvo,tag,pptss)	\
    ( (This)->lpVtbl -> get_StringProp(This,hvo,tag,pptss) ) 

#define ISilDataAccess_get_TimeProp(This,hvo,tag,ptim)	\
    ( (This)->lpVtbl -> get_TimeProp(This,hvo,tag,ptim) ) 

#define ISilDataAccess_get_UnicodeProp(This,obj,tag,pbstr)	\
    ( (This)->lpVtbl -> get_UnicodeProp(This,obj,tag,pbstr) ) 

#define ISilDataAccess_put_UnicodeProp(This,obj,tag,bstr)	\
    ( (This)->lpVtbl -> put_UnicodeProp(This,obj,tag,bstr) ) 

#define ISilDataAccess_UnicodePropRgch(This,obj,tag,prgch,cchMax,pcch)	\
    ( (This)->lpVtbl -> UnicodePropRgch(This,obj,tag,prgch,cchMax,pcch) ) 

#define ISilDataAccess_get_UnknownProp(This,hvo,tag,ppunk)	\
    ( (This)->lpVtbl -> get_UnknownProp(This,hvo,tag,ppunk) ) 

#define ISilDataAccess_BeginUndoTask(This,bstrUndo,bstrRedo)	\
    ( (This)->lpVtbl -> BeginUndoTask(This,bstrUndo,bstrRedo) ) 

#define ISilDataAccess_EndUndoTask(This)	\
    ( (This)->lpVtbl -> EndUndoTask(This) ) 

#define ISilDataAccess_ContinueUndoTask(This)	\
    ( (This)->lpVtbl -> ContinueUndoTask(This) ) 

#define ISilDataAccess_EndOuterUndoTask(This)	\
    ( (This)->lpVtbl -> EndOuterUndoTask(This) ) 

#define ISilDataAccess_Rollback(This)	\
    ( (This)->lpVtbl -> Rollback(This) ) 

#define ISilDataAccess_BreakUndoTask(This,bstrUndo,bstrRedo)	\
    ( (This)->lpVtbl -> BreakUndoTask(This,bstrUndo,bstrRedo) ) 

#define ISilDataAccess_BeginNonUndoableTask(This)	\
    ( (This)->lpVtbl -> BeginNonUndoableTask(This) ) 

#define ISilDataAccess_EndNonUndoableTask(This)	\
    ( (This)->lpVtbl -> EndNonUndoableTask(This) ) 

#define ISilDataAccess_GetActionHandler(This,ppacth)	\
    ( (This)->lpVtbl -> GetActionHandler(This,ppacth) ) 

#define ISilDataAccess_SetActionHandler(This,pacth)	\
    ( (This)->lpVtbl -> SetActionHandler(This,pacth) ) 

#define ISilDataAccess_DeleteObj(This,hvoObj)	\
    ( (This)->lpVtbl -> DeleteObj(This,hvoObj) ) 

#define ISilDataAccess_DeleteObjOwner(This,hvoOwner,hvoObj,tag,ihvo)	\
    ( (This)->lpVtbl -> DeleteObjOwner(This,hvoOwner,hvoObj,tag,ihvo) ) 

#define ISilDataAccess_InsertNew(This,hvoObj,tag,ihvo,chvo,pss)	\
    ( (This)->lpVtbl -> InsertNew(This,hvoObj,tag,ihvo,chvo,pss) ) 

#define ISilDataAccess_MakeNewObject(This,clid,hvoOwner,tag,ord,phvoNew)	\
    ( (This)->lpVtbl -> MakeNewObject(This,clid,hvoOwner,tag,ord,phvoNew) ) 

#define ISilDataAccess_MoveOwnSeq(This,hvoSrcOwner,tagSrc,ihvoStart,ihvoEnd,hvoDstOwner,tagDst,ihvoDstStart)	\
    ( (This)->lpVtbl -> MoveOwnSeq(This,hvoSrcOwner,tagSrc,ihvoStart,ihvoEnd,hvoDstOwner,tagDst,ihvoDstStart) ) 

#define ISilDataAccess_MoveOwn(This,hvoSrcOwner,tagSrc,hvo,hvoDstOwner,tagDst,ihvoDstStart)	\
    ( (This)->lpVtbl -> MoveOwn(This,hvoSrcOwner,tagSrc,hvo,hvoDstOwner,tagDst,ihvoDstStart) ) 

#define ISilDataAccess_Replace(This,hvoObj,tag,ihvoMin,ihvoLim,prghvo,chvo)	\
    ( (This)->lpVtbl -> Replace(This,hvoObj,tag,ihvoMin,ihvoLim,prghvo,chvo) ) 

#define ISilDataAccess_SetObjProp(This,hvo,tag,hvoObj)	\
    ( (This)->lpVtbl -> SetObjProp(This,hvo,tag,hvoObj) ) 

#define ISilDataAccess_RemoveObjRefs(This,hvo)	\
    ( (This)->lpVtbl -> RemoveObjRefs(This,hvo) ) 

#define ISilDataAccess_SetBinary(This,hvo,tag,prgb,cb)	\
    ( (This)->lpVtbl -> SetBinary(This,hvo,tag,prgb,cb) ) 

#define ISilDataAccess_SetGuid(This,hvo,tag,uid)	\
    ( (This)->lpVtbl -> SetGuid(This,hvo,tag,uid) ) 

#define ISilDataAccess_SetInt(This,hvo,tag,n)	\
    ( (This)->lpVtbl -> SetInt(This,hvo,tag,n) ) 

#define ISilDataAccess_SetInt64(This,hvo,tag,lln)	\
    ( (This)->lpVtbl -> SetInt64(This,hvo,tag,lln) ) 

#define ISilDataAccess_SetBoolean(This,hvo,tag,n)	\
    ( (This)->lpVtbl -> SetBoolean(This,hvo,tag,n) ) 

#define ISilDataAccess_SetMultiStringAlt(This,hvo,tag,ws,ptss)	\
    ( (This)->lpVtbl -> SetMultiStringAlt(This,hvo,tag,ws,ptss) ) 

#define ISilDataAccess_SetString(This,hvo,tag,ptss)	\
    ( (This)->lpVtbl -> SetString(This,hvo,tag,ptss) ) 

#define ISilDataAccess_SetTime(This,hvo,tag,lln)	\
    ( (This)->lpVtbl -> SetTime(This,hvo,tag,lln) ) 

#define ISilDataAccess_SetUnicode(This,hvo,tag,prgch,cch)	\
    ( (This)->lpVtbl -> SetUnicode(This,hvo,tag,prgch,cch) ) 

#define ISilDataAccess_SetUnknown(This,hvo,tag,punk)	\
    ( (This)->lpVtbl -> SetUnknown(This,hvo,tag,punk) ) 

#define ISilDataAccess_AddNotification(This,pnchng)	\
    ( (This)->lpVtbl -> AddNotification(This,pnchng) ) 

#define ISilDataAccess_PropChanged(This,pnchng,pct,hvo,tag,ivMin,cvIns,cvDel)	\
    ( (This)->lpVtbl -> PropChanged(This,pnchng,pct,hvo,tag,ivMin,cvIns,cvDel) ) 

#define ISilDataAccess_RemoveNotification(This,pnchng)	\
    ( (This)->lpVtbl -> RemoveNotification(This,pnchng) ) 

#define ISilDataAccess_GetDisplayIndex(This,hvoOwn,tag,ihvo,ihvoDisp)	\
    ( (This)->lpVtbl -> GetDisplayIndex(This,hvoOwn,tag,ihvo,ihvoDisp) ) 

#define ISilDataAccess_get_WritingSystemFactory(This,ppwsf)	\
    ( (This)->lpVtbl -> get_WritingSystemFactory(This,ppwsf) ) 

#define ISilDataAccess_putref_WritingSystemFactory(This,pwsf)	\
    ( (This)->lpVtbl -> putref_WritingSystemFactory(This,pwsf) ) 

#define ISilDataAccess_get_WritingSystemsOfInterest(This,cwsMax,pws,pcws)	\
    ( (This)->lpVtbl -> get_WritingSystemsOfInterest(This,cwsMax,pws,pcws) ) 

#define ISilDataAccess_InsertRelExtra(This,hvoSrc,tag,ihvo,hvoDst,bstrExtra)	\
    ( (This)->lpVtbl -> InsertRelExtra(This,hvoSrc,tag,ihvo,hvoDst,bstrExtra) ) 

#define ISilDataAccess_UpdateRelExtra(This,hvoSrc,tag,ihvo,bstrExtra)	\
    ( (This)->lpVtbl -> UpdateRelExtra(This,hvoSrc,tag,ihvo,bstrExtra) ) 

#define ISilDataAccess_GetRelExtra(This,hvoSrc,tag,ihvo,pbstrExtra)	\
    ( (This)->lpVtbl -> GetRelExtra(This,hvoSrc,tag,ihvo,pbstrExtra) ) 

#define ISilDataAccess_get_IsPropInCache(This,hvo,tag,cpt,ws,pfCached)	\
    ( (This)->lpVtbl -> get_IsPropInCache(This,hvo,tag,cpt,ws,pfCached) ) 

#define ISilDataAccess_IsDirty(This,pf)	\
    ( (This)->lpVtbl -> IsDirty(This,pf) ) 

#define ISilDataAccess_ClearDirty(This)	\
    ( (This)->lpVtbl -> ClearDirty(This) ) 

#define ISilDataAccess_get_MetaDataCache(This,ppmdc)	\
    ( (This)->lpVtbl -> get_MetaDataCache(This,ppmdc) ) 

#define ISilDataAccess_putref_MetaDataCache(This,pmdc)	\
    ( (This)->lpVtbl -> putref_MetaDataCache(This,pmdc) ) 

#define ISilDataAccess_get_IsValidObject(This,hvo,pfValid)	\
    ( (This)->lpVtbl -> get_IsValidObject(This,hvo,pfValid) ) 

#define ISilDataAccess_get_IsDummyId(This,hvo,pfDummy)	\
    ( (This)->lpVtbl -> get_IsDummyId(This,hvo,pfDummy) ) 

#define ISilDataAccess_GetObjIndex(This,hvoOwn,flid,hvo,ihvo)	\
    ( (This)->lpVtbl -> GetObjIndex(This,hvoOwn,flid,hvo,ihvo) ) 

#define ISilDataAccess_GetOutlineNumber(This,hvo,flid,fFinPer,pbstr)	\
    ( (This)->lpVtbl -> GetOutlineNumber(This,hvo,flid,fFinPer,pbstr) ) 

#define ISilDataAccess_MoveString(This,hvoSource,flidSrc,wsSrc,ichMin,ichLim,hvoDst,flidDst,wsDst,ichDest,fDstIsNew)	\
    ( (This)->lpVtbl -> MoveString(This,hvoSource,flidSrc,wsSrc,ichMin,ichLim,hvoDst,flidDst,wsDst,ichDest,fDstIsNew) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ISilDataAccess_INTERFACE_DEFINED__ */


#ifndef __IStructuredTextDataAccess_INTERFACE_DEFINED__
#define __IStructuredTextDataAccess_INTERFACE_DEFINED__

/* interface IStructuredTextDataAccess */
/* [unique][object][uuid] */ 


#define IID_IStructuredTextDataAccess __uuidof(IStructuredTextDataAccess)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("A2A4F9FA-D4E8-4bfb-B6B7-5F45DAF2DC0C")
    IStructuredTextDataAccess : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ParaContentsFlid( 
            /* [retval][out] */ PropTag *pflid) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ParaPropertiesFlid( 
            /* [retval][out] */ PropTag *pflid) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_TextParagraphsFlid( 
            /* [retval][out] */ PropTag *pflid) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IStructuredTextDataAccessVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IStructuredTextDataAccess * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IStructuredTextDataAccess * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IStructuredTextDataAccess * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ParaContentsFlid )( 
            IStructuredTextDataAccess * This,
            /* [retval][out] */ PropTag *pflid);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ParaPropertiesFlid )( 
            IStructuredTextDataAccess * This,
            /* [retval][out] */ PropTag *pflid);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_TextParagraphsFlid )( 
            IStructuredTextDataAccess * This,
            /* [retval][out] */ PropTag *pflid);
        
        END_INTERFACE
    } IStructuredTextDataAccessVtbl;

    interface IStructuredTextDataAccess
    {
        CONST_VTBL struct IStructuredTextDataAccessVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IStructuredTextDataAccess_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IStructuredTextDataAccess_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IStructuredTextDataAccess_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IStructuredTextDataAccess_get_ParaContentsFlid(This,pflid)	\
    ( (This)->lpVtbl -> get_ParaContentsFlid(This,pflid) ) 

#define IStructuredTextDataAccess_get_ParaPropertiesFlid(This,pflid)	\
    ( (This)->lpVtbl -> get_ParaPropertiesFlid(This,pflid) ) 

#define IStructuredTextDataAccess_get_TextParagraphsFlid(This,pflid)	\
    ( (This)->lpVtbl -> get_TextParagraphsFlid(This,pflid) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IStructuredTextDataAccess_INTERFACE_DEFINED__ */


#ifndef __IDebugReportSink_INTERFACE_DEFINED__
#define __IDebugReportSink_INTERFACE_DEFINED__

/* interface IDebugReportSink */
/* [unique][object][uuid] */ 


#define IID_IDebugReportSink __uuidof(IDebugReportSink)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("DD9CE7AD-6ECC-4e0c-BBFC-1DC52E053354")
    IDebugReportSink : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Report( 
            /* [in] */ CrtReportType nReportType,
            /* [in] */ BSTR szMsg) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AssertProc( 
            /* [in] */ BSTR pszExp,
            /* [in] */ BSTR pszFile,
            /* [in] */ int nLine) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IDebugReportSinkVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IDebugReportSink * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IDebugReportSink * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IDebugReportSink * This);
        
        HRESULT ( STDMETHODCALLTYPE *Report )( 
            IDebugReportSink * This,
            /* [in] */ CrtReportType nReportType,
            /* [in] */ BSTR szMsg);
        
        HRESULT ( STDMETHODCALLTYPE *AssertProc )( 
            IDebugReportSink * This,
            /* [in] */ BSTR pszExp,
            /* [in] */ BSTR pszFile,
            /* [in] */ int nLine);
        
        END_INTERFACE
    } IDebugReportSinkVtbl;

    interface IDebugReportSink
    {
        CONST_VTBL struct IDebugReportSinkVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IDebugReportSink_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IDebugReportSink_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IDebugReportSink_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IDebugReportSink_Report(This,nReportType,szMsg)	\
    ( (This)->lpVtbl -> Report(This,nReportType,szMsg) ) 

#define IDebugReportSink_AssertProc(This,pszExp,pszFile,nLine)	\
    ( (This)->lpVtbl -> AssertProc(This,pszExp,pszFile,nLine) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IDebugReportSink_INTERFACE_DEFINED__ */


#ifndef __IDebugReport_INTERFACE_DEFINED__
#define __IDebugReport_INTERFACE_DEFINED__

/* interface IDebugReport */
/* [unique][object][uuid] */ 


#define IID_IDebugReport __uuidof(IDebugReport)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("3D6A0880-D17D-4e4a-9DE9-861A85CA4046")
    IDebugReport : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE SetSink( 
            /* [in] */ IDebugReportSink *pSink) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ClearSink( void) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IDebugReportVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IDebugReport * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IDebugReport * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IDebugReport * This);
        
        HRESULT ( STDMETHODCALLTYPE *SetSink )( 
            IDebugReport * This,
            /* [in] */ IDebugReportSink *pSink);
        
        HRESULT ( STDMETHODCALLTYPE *ClearSink )( 
            IDebugReport * This);
        
        END_INTERFACE
    } IDebugReportVtbl;

    interface IDebugReport
    {
        CONST_VTBL struct IDebugReportVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IDebugReport_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IDebugReport_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IDebugReport_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IDebugReport_SetSink(This,pSink)	\
    ( (This)->lpVtbl -> SetSink(This,pSink) ) 

#define IDebugReport_ClearSink(This)	\
    ( (This)->lpVtbl -> ClearSink(This) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IDebugReport_INTERFACE_DEFINED__ */


#ifndef __IComDisposable_INTERFACE_DEFINED__
#define __IComDisposable_INTERFACE_DEFINED__

/* interface IComDisposable */
/* [unique][object][uuid] */ 


#define IID_IComDisposable __uuidof(IComDisposable)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("CA9AAF91-4C34-4c6a-8E07-97C1A7B3486A")
    IComDisposable : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Dispose( void) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IComDisposableVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IComDisposable * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IComDisposable * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IComDisposable * This);
        
        HRESULT ( STDMETHODCALLTYPE *Dispose )( 
            IComDisposable * This);
        
        END_INTERFACE
    } IComDisposableVtbl;

    interface IComDisposable
    {
        CONST_VTBL struct IComDisposableVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IComDisposable_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IComDisposable_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IComDisposable_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IComDisposable_Dispose(This)	\
    ( (This)->lpVtbl -> Dispose(This) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IComDisposable_INTERFACE_DEFINED__ */


#ifndef __ITsStrFactory_INTERFACE_DEFINED__
#define __ITsStrFactory_INTERFACE_DEFINED__

/* interface ITsStrFactory */
/* [unique][object][uuid] */ 


#define IID_ITsStrFactory __uuidof(ITsStrFactory)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("721A8D21-9900-4CB0-B4C0-9380A23140E3")
    ITsStrFactory : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE MakeString( 
            /* [in] */ BSTR bstr,
            /* [in] */ int ws,
            /* [retval][out] */ ITsString **pptss) = 0;
        
        virtual /* [restricted] */ HRESULT STDMETHODCALLTYPE MakeStringRgch( 
            /* [size_is][in] */ const OLECHAR *prgch,
            /* [in] */ int cch,
            /* [in] */ int ws,
            /* [retval][out] */ ITsString **pptss) = 0;
        
        virtual /* [restricted] */ HRESULT STDMETHODCALLTYPE MakeStringWithPropsRgch( 
            /* [size_is][in] */ const OLECHAR *prgch,
            /* [in] */ int cch,
            /* [in] */ ITsTextProps *pttp,
            /* [retval][out] */ ITsString **pptss) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetBldr( 
            /* [retval][out] */ ITsStrBldr **pptsb) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetIncBldr( 
            /* [retval][out] */ ITsIncStrBldr **pptisb) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EmptyString( 
            /* [in] */ int ws,
            /* [retval][out] */ ITsString **pptss) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ITsStrFactoryVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ITsStrFactory * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ITsStrFactory * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ITsStrFactory * This);
        
        HRESULT ( STDMETHODCALLTYPE *MakeString )( 
            ITsStrFactory * This,
            /* [in] */ BSTR bstr,
            /* [in] */ int ws,
            /* [retval][out] */ ITsString **pptss);
        
        /* [restricted] */ HRESULT ( STDMETHODCALLTYPE *MakeStringRgch )( 
            ITsStrFactory * This,
            /* [size_is][in] */ const OLECHAR *prgch,
            /* [in] */ int cch,
            /* [in] */ int ws,
            /* [retval][out] */ ITsString **pptss);
        
        /* [restricted] */ HRESULT ( STDMETHODCALLTYPE *MakeStringWithPropsRgch )( 
            ITsStrFactory * This,
            /* [size_is][in] */ const OLECHAR *prgch,
            /* [in] */ int cch,
            /* [in] */ ITsTextProps *pttp,
            /* [retval][out] */ ITsString **pptss);
        
        HRESULT ( STDMETHODCALLTYPE *GetBldr )( 
            ITsStrFactory * This,
            /* [retval][out] */ ITsStrBldr **pptsb);
        
        HRESULT ( STDMETHODCALLTYPE *GetIncBldr )( 
            ITsStrFactory * This,
            /* [retval][out] */ ITsIncStrBldr **pptisb);
        
        HRESULT ( STDMETHODCALLTYPE *EmptyString )( 
            ITsStrFactory * This,
            /* [in] */ int ws,
            /* [retval][out] */ ITsString **pptss);
        
        END_INTERFACE
    } ITsStrFactoryVtbl;

    interface ITsStrFactory
    {
        CONST_VTBL struct ITsStrFactoryVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ITsStrFactory_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ITsStrFactory_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ITsStrFactory_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ITsStrFactory_MakeString(This,bstr,ws,pptss)	\
    ( (This)->lpVtbl -> MakeString(This,bstr,ws,pptss) ) 

#define ITsStrFactory_MakeStringRgch(This,prgch,cch,ws,pptss)	\
    ( (This)->lpVtbl -> MakeStringRgch(This,prgch,cch,ws,pptss) ) 

#define ITsStrFactory_MakeStringWithPropsRgch(This,prgch,cch,pttp,pptss)	\
    ( (This)->lpVtbl -> MakeStringWithPropsRgch(This,prgch,cch,pttp,pptss) ) 

#define ITsStrFactory_GetBldr(This,pptsb)	\
    ( (This)->lpVtbl -> GetBldr(This,pptsb) ) 

#define ITsStrFactory_GetIncBldr(This,pptisb)	\
    ( (This)->lpVtbl -> GetIncBldr(This,pptisb) ) 

#define ITsStrFactory_EmptyString(This,ws,pptss)	\
    ( (This)->lpVtbl -> EmptyString(This,ws,pptss) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ITsStrFactory_INTERFACE_DEFINED__ */


#ifndef __ITsPropsFactory_INTERFACE_DEFINED__
#define __ITsPropsFactory_INTERFACE_DEFINED__

/* interface ITsPropsFactory */
/* [unique][object][uuid] */ 


#define IID_ITsPropsFactory __uuidof(ITsPropsFactory)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("FF3D947F-1D35-487B-A769-5B6C68722054")
    ITsPropsFactory : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE MakeProps( 
            /* [in] */ BSTR bstrStyle,
            /* [in] */ int ws,
            /* [in] */ int ows,
            /* [retval][out] */ ITsTextProps **ppttp) = 0;
        
        virtual /* [restricted] */ HRESULT STDMETHODCALLTYPE MakePropsRgch( 
            /* [size_is][in] */ const OLECHAR *prgchStyle,
            /* [in] */ int cch,
            /* [in] */ int ws,
            /* [in] */ int ows,
            /* [retval][out] */ ITsTextProps **ppttp) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetPropsBldr( 
            /* [retval][out] */ ITsPropsBldr **pptpb) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ITsPropsFactoryVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ITsPropsFactory * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ITsPropsFactory * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ITsPropsFactory * This);
        
        HRESULT ( STDMETHODCALLTYPE *MakeProps )( 
            ITsPropsFactory * This,
            /* [in] */ BSTR bstrStyle,
            /* [in] */ int ws,
            /* [in] */ int ows,
            /* [retval][out] */ ITsTextProps **ppttp);
        
        /* [restricted] */ HRESULT ( STDMETHODCALLTYPE *MakePropsRgch )( 
            ITsPropsFactory * This,
            /* [size_is][in] */ const OLECHAR *prgchStyle,
            /* [in] */ int cch,
            /* [in] */ int ws,
            /* [in] */ int ows,
            /* [retval][out] */ ITsTextProps **ppttp);
        
        HRESULT ( STDMETHODCALLTYPE *GetPropsBldr )( 
            ITsPropsFactory * This,
            /* [retval][out] */ ITsPropsBldr **pptpb);
        
        END_INTERFACE
    } ITsPropsFactoryVtbl;

    interface ITsPropsFactory
    {
        CONST_VTBL struct ITsPropsFactoryVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ITsPropsFactory_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ITsPropsFactory_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ITsPropsFactory_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ITsPropsFactory_MakeProps(This,bstrStyle,ws,ows,ppttp)	\
    ( (This)->lpVtbl -> MakeProps(This,bstrStyle,ws,ows,ppttp) ) 

#define ITsPropsFactory_MakePropsRgch(This,prgchStyle,cch,ws,ows,ppttp)	\
    ( (This)->lpVtbl -> MakePropsRgch(This,prgchStyle,cch,ws,ows,ppttp) ) 

#define ITsPropsFactory_GetPropsBldr(This,pptpb)	\
    ( (This)->lpVtbl -> GetPropsBldr(This,pptpb) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ITsPropsFactory_INTERFACE_DEFINED__ */


#ifndef __ITsStrBldr_INTERFACE_DEFINED__
#define __ITsStrBldr_INTERFACE_DEFINED__

/* interface ITsStrBldr */
/* [unique][object][uuid] */ 


#define IID_ITsStrBldr __uuidof(ITsStrBldr)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("35C5278D-2A52-4B54-AB13-B6E346B301BA")
    ITsStrBldr : public IUnknown
    {
    public:
        virtual /* [id][propget] */ HRESULT STDMETHODCALLTYPE get_Text( 
            /* [retval][out] */ BSTR *pbstr) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Length( 
            /* [retval][out] */ int *pcch) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_RunCount( 
            /* [retval][out] */ int *pcrun) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_RunAt( 
            /* [in] */ int ich,
            /* [retval][out] */ int *pirun) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetBoundsOfRun( 
            /* [in] */ int irun,
            /* [out] */ int *pichMin,
            /* [out] */ int *pichLim) = 0;
        
        virtual /* [restricted] */ HRESULT STDMETHODCALLTYPE FetchRunInfoAt( 
            /* [in] */ int ich,
            /* [out] */ TsRunInfo *ptri,
            /* [retval][out] */ ITsTextProps **ppttp) = 0;
        
        virtual /* [restricted] */ HRESULT STDMETHODCALLTYPE FetchRunInfo( 
            /* [in] */ int irun,
            /* [out] */ TsRunInfo *ptri,
            /* [retval][out] */ ITsTextProps **ppttp) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_RunText( 
            /* [in] */ int irun,
            /* [retval][out] */ BSTR *pbstr) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetChars( 
            /* [in] */ int ichMin,
            /* [in] */ int ichLim,
            /* [retval][out] */ BSTR *pbstr) = 0;
        
        virtual /* [local][restricted] */ HRESULT STDMETHODCALLTYPE FetchChars( 
            /* [in] */ int ichMin,
            /* [in] */ int ichLim,
            /* [size_is][out][in] */ OLECHAR *prgch) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_PropertiesAt( 
            /* [in] */ int ich,
            /* [retval][out] */ ITsTextProps **pttp) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Properties( 
            /* [in] */ int irun,
            /* [retval][out] */ ITsTextProps **pttp) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Replace( 
            /* [in] */ int ichMin,
            /* [in] */ int ichLim,
            /* [in] */ BSTR bstrIns,
            /* [in] */ ITsTextProps *pttp) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ReplaceTsString( 
            /* [in] */ int ichMin,
            /* [in] */ int ichLim,
            /* [in] */ ITsString *ptssIns) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ReplaceRgch( 
            /* [in] */ int ichMin,
            /* [in] */ int ichLim,
            /* [in] */ const BSTR prgchIns,
            /* [in] */ int cchIns,
            /* [in] */ ITsTextProps *pttp) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetProperties( 
            /* [in] */ int ichMin,
            /* [in] */ int ichLim,
            /* [in] */ ITsTextProps *pttp) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetIntPropValues( 
            /* [in] */ int ichMin,
            /* [in] */ int ichLim,
            /* [in] */ int tpt,
            /* [in] */ int nVar,
            /* [in] */ int nVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetStrPropValue( 
            /* [in] */ int ichMin,
            /* [in] */ int ichLim,
            /* [in] */ int tpt,
            /* [in] */ BSTR bstrVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetString( 
            /* [retval][out] */ ITsString **pptss) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Clear( void) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ITsStrBldrVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ITsStrBldr * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ITsStrBldr * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ITsStrBldr * This);
        
        /* [id][propget] */ HRESULT ( STDMETHODCALLTYPE *get_Text )( 
            ITsStrBldr * This,
            /* [retval][out] */ BSTR *pbstr);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Length )( 
            ITsStrBldr * This,
            /* [retval][out] */ int *pcch);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_RunCount )( 
            ITsStrBldr * This,
            /* [retval][out] */ int *pcrun);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_RunAt )( 
            ITsStrBldr * This,
            /* [in] */ int ich,
            /* [retval][out] */ int *pirun);
        
        HRESULT ( STDMETHODCALLTYPE *GetBoundsOfRun )( 
            ITsStrBldr * This,
            /* [in] */ int irun,
            /* [out] */ int *pichMin,
            /* [out] */ int *pichLim);
        
        /* [restricted] */ HRESULT ( STDMETHODCALLTYPE *FetchRunInfoAt )( 
            ITsStrBldr * This,
            /* [in] */ int ich,
            /* [out] */ TsRunInfo *ptri,
            /* [retval][out] */ ITsTextProps **ppttp);
        
        /* [restricted] */ HRESULT ( STDMETHODCALLTYPE *FetchRunInfo )( 
            ITsStrBldr * This,
            /* [in] */ int irun,
            /* [out] */ TsRunInfo *ptri,
            /* [retval][out] */ ITsTextProps **ppttp);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_RunText )( 
            ITsStrBldr * This,
            /* [in] */ int irun,
            /* [retval][out] */ BSTR *pbstr);
        
        HRESULT ( STDMETHODCALLTYPE *GetChars )( 
            ITsStrBldr * This,
            /* [in] */ int ichMin,
            /* [in] */ int ichLim,
            /* [retval][out] */ BSTR *pbstr);
        
        /* [local][restricted] */ HRESULT ( STDMETHODCALLTYPE *FetchChars )( 
            ITsStrBldr * This,
            /* [in] */ int ichMin,
            /* [in] */ int ichLim,
            /* [size_is][out][in] */ OLECHAR *prgch);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_PropertiesAt )( 
            ITsStrBldr * This,
            /* [in] */ int ich,
            /* [retval][out] */ ITsTextProps **pttp);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Properties )( 
            ITsStrBldr * This,
            /* [in] */ int irun,
            /* [retval][out] */ ITsTextProps **pttp);
        
        HRESULT ( STDMETHODCALLTYPE *Replace )( 
            ITsStrBldr * This,
            /* [in] */ int ichMin,
            /* [in] */ int ichLim,
            /* [in] */ BSTR bstrIns,
            /* [in] */ ITsTextProps *pttp);
        
        HRESULT ( STDMETHODCALLTYPE *ReplaceTsString )( 
            ITsStrBldr * This,
            /* [in] */ int ichMin,
            /* [in] */ int ichLim,
            /* [in] */ ITsString *ptssIns);
        
        HRESULT ( STDMETHODCALLTYPE *ReplaceRgch )( 
            ITsStrBldr * This,
            /* [in] */ int ichMin,
            /* [in] */ int ichLim,
            /* [in] */ const BSTR prgchIns,
            /* [in] */ int cchIns,
            /* [in] */ ITsTextProps *pttp);
        
        HRESULT ( STDMETHODCALLTYPE *SetProperties )( 
            ITsStrBldr * This,
            /* [in] */ int ichMin,
            /* [in] */ int ichLim,
            /* [in] */ ITsTextProps *pttp);
        
        HRESULT ( STDMETHODCALLTYPE *SetIntPropValues )( 
            ITsStrBldr * This,
            /* [in] */ int ichMin,
            /* [in] */ int ichLim,
            /* [in] */ int tpt,
            /* [in] */ int nVar,
            /* [in] */ int nVal);
        
        HRESULT ( STDMETHODCALLTYPE *SetStrPropValue )( 
            ITsStrBldr * This,
            /* [in] */ int ichMin,
            /* [in] */ int ichLim,
            /* [in] */ int tpt,
            /* [in] */ BSTR bstrVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetString )( 
            ITsStrBldr * This,
            /* [retval][out] */ ITsString **pptss);
        
        HRESULT ( STDMETHODCALLTYPE *Clear )( 
            ITsStrBldr * This);
        
        END_INTERFACE
    } ITsStrBldrVtbl;

    interface ITsStrBldr
    {
        CONST_VTBL struct ITsStrBldrVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ITsStrBldr_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ITsStrBldr_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ITsStrBldr_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ITsStrBldr_get_Text(This,pbstr)	\
    ( (This)->lpVtbl -> get_Text(This,pbstr) ) 

#define ITsStrBldr_get_Length(This,pcch)	\
    ( (This)->lpVtbl -> get_Length(This,pcch) ) 

#define ITsStrBldr_get_RunCount(This,pcrun)	\
    ( (This)->lpVtbl -> get_RunCount(This,pcrun) ) 

#define ITsStrBldr_get_RunAt(This,ich,pirun)	\
    ( (This)->lpVtbl -> get_RunAt(This,ich,pirun) ) 

#define ITsStrBldr_GetBoundsOfRun(This,irun,pichMin,pichLim)	\
    ( (This)->lpVtbl -> GetBoundsOfRun(This,irun,pichMin,pichLim) ) 

#define ITsStrBldr_FetchRunInfoAt(This,ich,ptri,ppttp)	\
    ( (This)->lpVtbl -> FetchRunInfoAt(This,ich,ptri,ppttp) ) 

#define ITsStrBldr_FetchRunInfo(This,irun,ptri,ppttp)	\
    ( (This)->lpVtbl -> FetchRunInfo(This,irun,ptri,ppttp) ) 

#define ITsStrBldr_get_RunText(This,irun,pbstr)	\
    ( (This)->lpVtbl -> get_RunText(This,irun,pbstr) ) 

#define ITsStrBldr_GetChars(This,ichMin,ichLim,pbstr)	\
    ( (This)->lpVtbl -> GetChars(This,ichMin,ichLim,pbstr) ) 

#define ITsStrBldr_FetchChars(This,ichMin,ichLim,prgch)	\
    ( (This)->lpVtbl -> FetchChars(This,ichMin,ichLim,prgch) ) 

#define ITsStrBldr_get_PropertiesAt(This,ich,pttp)	\
    ( (This)->lpVtbl -> get_PropertiesAt(This,ich,pttp) ) 

#define ITsStrBldr_get_Properties(This,irun,pttp)	\
    ( (This)->lpVtbl -> get_Properties(This,irun,pttp) ) 

#define ITsStrBldr_Replace(This,ichMin,ichLim,bstrIns,pttp)	\
    ( (This)->lpVtbl -> Replace(This,ichMin,ichLim,bstrIns,pttp) ) 

#define ITsStrBldr_ReplaceTsString(This,ichMin,ichLim,ptssIns)	\
    ( (This)->lpVtbl -> ReplaceTsString(This,ichMin,ichLim,ptssIns) ) 

#define ITsStrBldr_ReplaceRgch(This,ichMin,ichLim,prgchIns,cchIns,pttp)	\
    ( (This)->lpVtbl -> ReplaceRgch(This,ichMin,ichLim,prgchIns,cchIns,pttp) ) 

#define ITsStrBldr_SetProperties(This,ichMin,ichLim,pttp)	\
    ( (This)->lpVtbl -> SetProperties(This,ichMin,ichLim,pttp) ) 

#define ITsStrBldr_SetIntPropValues(This,ichMin,ichLim,tpt,nVar,nVal)	\
    ( (This)->lpVtbl -> SetIntPropValues(This,ichMin,ichLim,tpt,nVar,nVal) ) 

#define ITsStrBldr_SetStrPropValue(This,ichMin,ichLim,tpt,bstrVal)	\
    ( (This)->lpVtbl -> SetStrPropValue(This,ichMin,ichLim,tpt,bstrVal) ) 

#define ITsStrBldr_GetString(This,pptss)	\
    ( (This)->lpVtbl -> GetString(This,pptss) ) 

#define ITsStrBldr_Clear(This)	\
    ( (This)->lpVtbl -> Clear(This) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ITsStrBldr_INTERFACE_DEFINED__ */


#ifndef __ITsIncStrBldr_INTERFACE_DEFINED__
#define __ITsIncStrBldr_INTERFACE_DEFINED__

/* interface ITsIncStrBldr */
/* [unique][object][uuid] */ 


#define IID_ITsIncStrBldr __uuidof(ITsIncStrBldr)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("87ECD3CD-6011-485F-8651-DBA0B79245AF")
    ITsIncStrBldr : public IUnknown
    {
    public:
        virtual /* [id][propget] */ HRESULT STDMETHODCALLTYPE get_Text( 
            /* [retval][out] */ BSTR *pbstr) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Append( 
            /* [in] */ BSTR bstrIns) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AppendTsString( 
            /* [in] */ ITsString *ptssIns) = 0;
        
        virtual /* [restricted] */ HRESULT STDMETHODCALLTYPE AppendRgch( 
            /* [size_is][in] */ const OLECHAR *prgchIns,
            /* [in] */ int cchIns) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetIntPropValues( 
            /* [in] */ int tpt,
            /* [in] */ int nVar,
            /* [in] */ int nVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetStrPropValue( 
            /* [in] */ int tpt,
            /* [in] */ BSTR bstrVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetString( 
            /* [retval][out] */ ITsString **pptss) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Clear( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetStrPropValueRgch( 
            /* [in] */ int tpt,
            /* [size_is][in] */ const byte *rgchVal,
            /* [in] */ int nValLength) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ClearProps( void) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ITsIncStrBldrVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ITsIncStrBldr * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ITsIncStrBldr * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ITsIncStrBldr * This);
        
        /* [id][propget] */ HRESULT ( STDMETHODCALLTYPE *get_Text )( 
            ITsIncStrBldr * This,
            /* [retval][out] */ BSTR *pbstr);
        
        HRESULT ( STDMETHODCALLTYPE *Append )( 
            ITsIncStrBldr * This,
            /* [in] */ BSTR bstrIns);
        
        HRESULT ( STDMETHODCALLTYPE *AppendTsString )( 
            ITsIncStrBldr * This,
            /* [in] */ ITsString *ptssIns);
        
        /* [restricted] */ HRESULT ( STDMETHODCALLTYPE *AppendRgch )( 
            ITsIncStrBldr * This,
            /* [size_is][in] */ const OLECHAR *prgchIns,
            /* [in] */ int cchIns);
        
        HRESULT ( STDMETHODCALLTYPE *SetIntPropValues )( 
            ITsIncStrBldr * This,
            /* [in] */ int tpt,
            /* [in] */ int nVar,
            /* [in] */ int nVal);
        
        HRESULT ( STDMETHODCALLTYPE *SetStrPropValue )( 
            ITsIncStrBldr * This,
            /* [in] */ int tpt,
            /* [in] */ BSTR bstrVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetString )( 
            ITsIncStrBldr * This,
            /* [retval][out] */ ITsString **pptss);
        
        HRESULT ( STDMETHODCALLTYPE *Clear )( 
            ITsIncStrBldr * This);
        
        HRESULT ( STDMETHODCALLTYPE *SetStrPropValueRgch )( 
            ITsIncStrBldr * This,
            /* [in] */ int tpt,
            /* [size_is][in] */ const byte *rgchVal,
            /* [in] */ int nValLength);
        
        HRESULT ( STDMETHODCALLTYPE *ClearProps )( 
            ITsIncStrBldr * This);
        
        END_INTERFACE
    } ITsIncStrBldrVtbl;

    interface ITsIncStrBldr
    {
        CONST_VTBL struct ITsIncStrBldrVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ITsIncStrBldr_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ITsIncStrBldr_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ITsIncStrBldr_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ITsIncStrBldr_get_Text(This,pbstr)	\
    ( (This)->lpVtbl -> get_Text(This,pbstr) ) 

#define ITsIncStrBldr_Append(This,bstrIns)	\
    ( (This)->lpVtbl -> Append(This,bstrIns) ) 

#define ITsIncStrBldr_AppendTsString(This,ptssIns)	\
    ( (This)->lpVtbl -> AppendTsString(This,ptssIns) ) 

#define ITsIncStrBldr_AppendRgch(This,prgchIns,cchIns)	\
    ( (This)->lpVtbl -> AppendRgch(This,prgchIns,cchIns) ) 

#define ITsIncStrBldr_SetIntPropValues(This,tpt,nVar,nVal)	\
    ( (This)->lpVtbl -> SetIntPropValues(This,tpt,nVar,nVal) ) 

#define ITsIncStrBldr_SetStrPropValue(This,tpt,bstrVal)	\
    ( (This)->lpVtbl -> SetStrPropValue(This,tpt,bstrVal) ) 

#define ITsIncStrBldr_GetString(This,pptss)	\
    ( (This)->lpVtbl -> GetString(This,pptss) ) 

#define ITsIncStrBldr_Clear(This)	\
    ( (This)->lpVtbl -> Clear(This) ) 

#define ITsIncStrBldr_SetStrPropValueRgch(This,tpt,rgchVal,nValLength)	\
    ( (This)->lpVtbl -> SetStrPropValueRgch(This,tpt,rgchVal,nValLength) ) 

#define ITsIncStrBldr_ClearProps(This)	\
    ( (This)->lpVtbl -> ClearProps(This) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ITsIncStrBldr_INTERFACE_DEFINED__ */


#ifndef __ITsPropsBldr_INTERFACE_DEFINED__
#define __ITsPropsBldr_INTERFACE_DEFINED__

/* interface ITsPropsBldr */
/* [unique][object][uuid] */ 


#define IID_ITsPropsBldr __uuidof(ITsPropsBldr)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("F1EF76E8-BE04-11d3-8D9A-005004DEFEC4")
    ITsPropsBldr : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IntPropCount( 
            /* [retval][out] */ int *pcv) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetIntProp( 
            /* [in] */ int iv,
            /* [out] */ int *ptpt,
            /* [out] */ int *pnVar,
            /* [retval][out] */ int *pnVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetIntPropValues( 
            /* [in] */ int tpt,
            /* [out] */ int *pnVar,
            /* [retval][out] */ int *pnVal) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_StrPropCount( 
            /* [retval][out] */ int *pcv) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetStrProp( 
            /* [in] */ int iv,
            /* [out] */ int *ptpt,
            /* [retval][out] */ BSTR *pbstrVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetStrPropValue( 
            /* [in] */ int tpt,
            /* [retval][out] */ BSTR *pbstrVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetIntPropValues( 
            /* [in] */ int tpt,
            /* [in] */ int nVar,
            /* [in] */ int nVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetStrPropValue( 
            /* [in] */ int tpt,
            /* [in] */ BSTR bstrVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetStrPropValueRgch( 
            /* [in] */ int tpt,
            /* [size_is][in] */ const byte *rgchVal,
            /* [in] */ int nValLength) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetTextProps( 
            /* [retval][out] */ ITsTextProps **ppttp) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Clear( void) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ITsPropsBldrVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ITsPropsBldr * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ITsPropsBldr * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ITsPropsBldr * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IntPropCount )( 
            ITsPropsBldr * This,
            /* [retval][out] */ int *pcv);
        
        HRESULT ( STDMETHODCALLTYPE *GetIntProp )( 
            ITsPropsBldr * This,
            /* [in] */ int iv,
            /* [out] */ int *ptpt,
            /* [out] */ int *pnVar,
            /* [retval][out] */ int *pnVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetIntPropValues )( 
            ITsPropsBldr * This,
            /* [in] */ int tpt,
            /* [out] */ int *pnVar,
            /* [retval][out] */ int *pnVal);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_StrPropCount )( 
            ITsPropsBldr * This,
            /* [retval][out] */ int *pcv);
        
        HRESULT ( STDMETHODCALLTYPE *GetStrProp )( 
            ITsPropsBldr * This,
            /* [in] */ int iv,
            /* [out] */ int *ptpt,
            /* [retval][out] */ BSTR *pbstrVal);
        
        HRESULT ( STDMETHODCALLTYPE *GetStrPropValue )( 
            ITsPropsBldr * This,
            /* [in] */ int tpt,
            /* [retval][out] */ BSTR *pbstrVal);
        
        HRESULT ( STDMETHODCALLTYPE *SetIntPropValues )( 
            ITsPropsBldr * This,
            /* [in] */ int tpt,
            /* [in] */ int nVar,
            /* [in] */ int nVal);
        
        HRESULT ( STDMETHODCALLTYPE *SetStrPropValue )( 
            ITsPropsBldr * This,
            /* [in] */ int tpt,
            /* [in] */ BSTR bstrVal);
        
        HRESULT ( STDMETHODCALLTYPE *SetStrPropValueRgch )( 
            ITsPropsBldr * This,
            /* [in] */ int tpt,
            /* [size_is][in] */ const byte *rgchVal,
            /* [in] */ int nValLength);
        
        HRESULT ( STDMETHODCALLTYPE *GetTextProps )( 
            ITsPropsBldr * This,
            /* [retval][out] */ ITsTextProps **ppttp);
        
        HRESULT ( STDMETHODCALLTYPE *Clear )( 
            ITsPropsBldr * This);
        
        END_INTERFACE
    } ITsPropsBldrVtbl;

    interface ITsPropsBldr
    {
        CONST_VTBL struct ITsPropsBldrVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ITsPropsBldr_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ITsPropsBldr_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ITsPropsBldr_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ITsPropsBldr_get_IntPropCount(This,pcv)	\
    ( (This)->lpVtbl -> get_IntPropCount(This,pcv) ) 

#define ITsPropsBldr_GetIntProp(This,iv,ptpt,pnVar,pnVal)	\
    ( (This)->lpVtbl -> GetIntProp(This,iv,ptpt,pnVar,pnVal) ) 

#define ITsPropsBldr_GetIntPropValues(This,tpt,pnVar,pnVal)	\
    ( (This)->lpVtbl -> GetIntPropValues(This,tpt,pnVar,pnVal) ) 

#define ITsPropsBldr_get_StrPropCount(This,pcv)	\
    ( (This)->lpVtbl -> get_StrPropCount(This,pcv) ) 

#define ITsPropsBldr_GetStrProp(This,iv,ptpt,pbstrVal)	\
    ( (This)->lpVtbl -> GetStrProp(This,iv,ptpt,pbstrVal) ) 

#define ITsPropsBldr_GetStrPropValue(This,tpt,pbstrVal)	\
    ( (This)->lpVtbl -> GetStrPropValue(This,tpt,pbstrVal) ) 

#define ITsPropsBldr_SetIntPropValues(This,tpt,nVar,nVal)	\
    ( (This)->lpVtbl -> SetIntPropValues(This,tpt,nVar,nVal) ) 

#define ITsPropsBldr_SetStrPropValue(This,tpt,bstrVal)	\
    ( (This)->lpVtbl -> SetStrPropValue(This,tpt,bstrVal) ) 

#define ITsPropsBldr_SetStrPropValueRgch(This,tpt,rgchVal,nValLength)	\
    ( (This)->lpVtbl -> SetStrPropValueRgch(This,tpt,rgchVal,nValLength) ) 

#define ITsPropsBldr_GetTextProps(This,ppttp)	\
    ( (This)->lpVtbl -> GetTextProps(This,ppttp) ) 

#define ITsPropsBldr_Clear(This)	\
    ( (This)->lpVtbl -> Clear(This) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ITsPropsBldr_INTERFACE_DEFINED__ */


#ifndef __ILgWritingSystem_INTERFACE_DEFINED__
#define __ILgWritingSystem_INTERFACE_DEFINED__

/* interface ILgWritingSystem */
/* [unique][object][uuid] */ 


#define IID_ILgWritingSystem __uuidof(ILgWritingSystem)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("9C0513AB-1AB9-4741-9C49-FA65FA83B7CC")
    ILgWritingSystem : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Id( 
            /* [retval][out] */ BSTR *pbstr) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Handle( 
            /* [retval][out] */ int *pws) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_LanguageName( 
            /* [retval][out] */ BSTR *pbstr) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_SpellCheckingId( 
            /* [retval][out] */ BSTR *pbstr) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_RightToLeftScript( 
            /* [retval][out] */ ComBool *pfRightToLeft) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_DefaultFontFeatures( 
            /* [retval][out] */ BSTR *pbstr) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_DefaultFontName( 
            /* [retval][out] */ BSTR *pbstr) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE InterpretChrp( 
            /* [out][in] */ LgCharRenderProps *pchrp) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_UseNfcContext( 
            /* [retval][out] */ ComBool *pUseNfc) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsWordForming( 
            /* [in] */ int ch,
            /* [retval][out] */ ComBool *pfRet) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IcuLocale( 
            /* [retval][out] */ BSTR *pbstr) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsGraphiteEnabled( 
            /* [retval][out] */ ComBool *pfRet) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_WritingSystemFactory( 
            /* [retval][out] */ ILgWritingSystemFactory **ppwsf) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ILgWritingSystemVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ILgWritingSystem * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ILgWritingSystem * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ILgWritingSystem * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Id )( 
            ILgWritingSystem * This,
            /* [retval][out] */ BSTR *pbstr);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Handle )( 
            ILgWritingSystem * This,
            /* [retval][out] */ int *pws);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_LanguageName )( 
            ILgWritingSystem * This,
            /* [retval][out] */ BSTR *pbstr);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_SpellCheckingId )( 
            ILgWritingSystem * This,
            /* [retval][out] */ BSTR *pbstr);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_RightToLeftScript )( 
            ILgWritingSystem * This,
            /* [retval][out] */ ComBool *pfRightToLeft);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_DefaultFontFeatures )( 
            ILgWritingSystem * This,
            /* [retval][out] */ BSTR *pbstr);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_DefaultFontName )( 
            ILgWritingSystem * This,
            /* [retval][out] */ BSTR *pbstr);
        
        HRESULT ( STDMETHODCALLTYPE *InterpretChrp )( 
            ILgWritingSystem * This,
            /* [out][in] */ LgCharRenderProps *pchrp);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_UseNfcContext )( 
            ILgWritingSystem * This,
            /* [retval][out] */ ComBool *pUseNfc);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsWordForming )( 
            ILgWritingSystem * This,
            /* [in] */ int ch,
            /* [retval][out] */ ComBool *pfRet);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IcuLocale )( 
            ILgWritingSystem * This,
            /* [retval][out] */ BSTR *pbstr);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsGraphiteEnabled )( 
            ILgWritingSystem * This,
            /* [retval][out] */ ComBool *pfRet);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_WritingSystemFactory )( 
            ILgWritingSystem * This,
            /* [retval][out] */ ILgWritingSystemFactory **ppwsf);
        
        END_INTERFACE
    } ILgWritingSystemVtbl;

    interface ILgWritingSystem
    {
        CONST_VTBL struct ILgWritingSystemVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ILgWritingSystem_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ILgWritingSystem_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ILgWritingSystem_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ILgWritingSystem_get_Id(This,pbstr)	\
    ( (This)->lpVtbl -> get_Id(This,pbstr) ) 

#define ILgWritingSystem_get_Handle(This,pws)	\
    ( (This)->lpVtbl -> get_Handle(This,pws) ) 

#define ILgWritingSystem_get_LanguageName(This,pbstr)	\
    ( (This)->lpVtbl -> get_LanguageName(This,pbstr) ) 

#define ILgWritingSystem_get_SpellCheckingId(This,pbstr)	\
    ( (This)->lpVtbl -> get_SpellCheckingId(This,pbstr) ) 

#define ILgWritingSystem_get_RightToLeftScript(This,pfRightToLeft)	\
    ( (This)->lpVtbl -> get_RightToLeftScript(This,pfRightToLeft) ) 

#define ILgWritingSystem_get_DefaultFontFeatures(This,pbstr)	\
    ( (This)->lpVtbl -> get_DefaultFontFeatures(This,pbstr) ) 

#define ILgWritingSystem_get_DefaultFontName(This,pbstr)	\
    ( (This)->lpVtbl -> get_DefaultFontName(This,pbstr) ) 

#define ILgWritingSystem_InterpretChrp(This,pchrp)	\
    ( (This)->lpVtbl -> InterpretChrp(This,pchrp) ) 

#define ILgWritingSystem_get_UseNfcContext(This,pUseNfc)	\
    ( (This)->lpVtbl -> get_UseNfcContext(This,pUseNfc) ) 

#define ILgWritingSystem_get_IsWordForming(This,ch,pfRet)	\
    ( (This)->lpVtbl -> get_IsWordForming(This,ch,pfRet) ) 

#define ILgWritingSystem_get_IcuLocale(This,pbstr)	\
    ( (This)->lpVtbl -> get_IcuLocale(This,pbstr) ) 

#define ILgWritingSystem_get_IsGraphiteEnabled(This,pfRet)	\
    ( (This)->lpVtbl -> get_IsGraphiteEnabled(This,pfRet) ) 

#define ILgWritingSystem_get_WritingSystemFactory(This,ppwsf)	\
    ( (This)->lpVtbl -> get_WritingSystemFactory(This,ppwsf) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ILgWritingSystem_INTERFACE_DEFINED__ */

#endif /* __FwKernelLib_LIBRARY_DEFINED__ */

/* Additional Prototypes for ALL interfaces */

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif


