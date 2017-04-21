

/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 8.00.0603 */
/* at Wed Apr 19 12:46:16 2017
 */
/* Compiler settings for C:\Repositories\fwroot\fw\Output\Common\ViewsTlb.idl:
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


#ifndef __ViewsTlb_h__
#define __ViewsTlb_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */ 

#ifndef __IVwGraphics_FWD_DEFINED__
#define __IVwGraphics_FWD_DEFINED__
typedef interface IVwGraphics IVwGraphics;

#endif 	/* __IVwGraphics_FWD_DEFINED__ */


#ifndef __IJustifyingRenderer_FWD_DEFINED__
#define __IJustifyingRenderer_FWD_DEFINED__
typedef interface IJustifyingRenderer IJustifyingRenderer;

#endif 	/* __IJustifyingRenderer_FWD_DEFINED__ */


#ifndef __IRenderEngineFactory_FWD_DEFINED__
#define __IRenderEngineFactory_FWD_DEFINED__
typedef interface IRenderEngineFactory IRenderEngineFactory;

#endif 	/* __IRenderEngineFactory_FWD_DEFINED__ */


#ifndef __ILgLineBreaker_FWD_DEFINED__
#define __ILgLineBreaker_FWD_DEFINED__
typedef interface ILgLineBreaker ILgLineBreaker;

#endif 	/* __ILgLineBreaker_FWD_DEFINED__ */


#ifndef __IVwGraphicsWin32_FWD_DEFINED__
#define __IVwGraphicsWin32_FWD_DEFINED__
typedef interface IVwGraphicsWin32 IVwGraphicsWin32;

#endif 	/* __IVwGraphicsWin32_FWD_DEFINED__ */


#ifndef __VwGraphicsWin32_FWD_DEFINED__
#define __VwGraphicsWin32_FWD_DEFINED__

#ifdef __cplusplus
typedef class VwGraphicsWin32 VwGraphicsWin32;
#else
typedef struct VwGraphicsWin32 VwGraphicsWin32;
#endif /* __cplusplus */

#endif 	/* __VwGraphicsWin32_FWD_DEFINED__ */


#ifndef __IVwTextSource_FWD_DEFINED__
#define __IVwTextSource_FWD_DEFINED__
typedef interface IVwTextSource IVwTextSource;

#endif 	/* __IVwTextSource_FWD_DEFINED__ */


#ifndef __IVwJustifier_FWD_DEFINED__
#define __IVwJustifier_FWD_DEFINED__
typedef interface IVwJustifier IVwJustifier;

#endif 	/* __IVwJustifier_FWD_DEFINED__ */


#ifndef __ILgSegment_FWD_DEFINED__
#define __ILgSegment_FWD_DEFINED__
typedef interface ILgSegment ILgSegment;

#endif 	/* __ILgSegment_FWD_DEFINED__ */


#ifndef __IRenderEngine_FWD_DEFINED__
#define __IRenderEngine_FWD_DEFINED__
typedef interface IRenderEngine IRenderEngine;

#endif 	/* __IRenderEngine_FWD_DEFINED__ */


#ifndef __LgLineBreaker_FWD_DEFINED__
#define __LgLineBreaker_FWD_DEFINED__

#ifdef __cplusplus
typedef class LgLineBreaker LgLineBreaker;
#else
typedef struct LgLineBreaker LgLineBreaker;
#endif /* __cplusplus */

#endif 	/* __LgLineBreaker_FWD_DEFINED__ */


#ifndef __RomRenderEngine_FWD_DEFINED__
#define __RomRenderEngine_FWD_DEFINED__

#ifdef __cplusplus
typedef class RomRenderEngine RomRenderEngine;
#else
typedef struct RomRenderEngine RomRenderEngine;
#endif /* __cplusplus */

#endif 	/* __RomRenderEngine_FWD_DEFINED__ */


#ifndef __UniscribeEngine_FWD_DEFINED__
#define __UniscribeEngine_FWD_DEFINED__

#ifdef __cplusplus
typedef class UniscribeEngine UniscribeEngine;
#else
typedef struct UniscribeEngine UniscribeEngine;
#endif /* __cplusplus */

#endif 	/* __UniscribeEngine_FWD_DEFINED__ */


#ifndef __GraphiteEngine_FWD_DEFINED__
#define __GraphiteEngine_FWD_DEFINED__

#ifdef __cplusplus
typedef class GraphiteEngine GraphiteEngine;
#else
typedef struct GraphiteEngine GraphiteEngine;
#endif /* __cplusplus */

#endif 	/* __GraphiteEngine_FWD_DEFINED__ */


#ifndef __IRenderingFeatures_FWD_DEFINED__
#define __IRenderingFeatures_FWD_DEFINED__
typedef interface IRenderingFeatures IRenderingFeatures;

#endif 	/* __IRenderingFeatures_FWD_DEFINED__ */


#ifndef __IVwSelection_FWD_DEFINED__
#define __IVwSelection_FWD_DEFINED__
typedef interface IVwSelection IVwSelection;

#endif 	/* __IVwSelection_FWD_DEFINED__ */


#ifndef __IVwEmbeddedWindow_FWD_DEFINED__
#define __IVwEmbeddedWindow_FWD_DEFINED__
typedef interface IVwEmbeddedWindow IVwEmbeddedWindow;

#endif 	/* __IVwEmbeddedWindow_FWD_DEFINED__ */


#ifndef __IVwEnv_FWD_DEFINED__
#define __IVwEnv_FWD_DEFINED__
typedef interface IVwEnv IVwEnv;

#endif 	/* __IVwEnv_FWD_DEFINED__ */


#ifndef __IVwViewConstructor_FWD_DEFINED__
#define __IVwViewConstructor_FWD_DEFINED__
typedef interface IVwViewConstructor IVwViewConstructor;

#endif 	/* __IVwViewConstructor_FWD_DEFINED__ */


#ifndef __IVwRootSite_FWD_DEFINED__
#define __IVwRootSite_FWD_DEFINED__
typedef interface IVwRootSite IVwRootSite;

#endif 	/* __IVwRootSite_FWD_DEFINED__ */


#ifndef __IVwCacheDa_FWD_DEFINED__
#define __IVwCacheDa_FWD_DEFINED__
typedef interface IVwCacheDa IVwCacheDa;

#endif 	/* __IVwCacheDa_FWD_DEFINED__ */


#ifndef __IVwRootBox_FWD_DEFINED__
#define __IVwRootBox_FWD_DEFINED__
typedef interface IVwRootBox IVwRootBox;

#endif 	/* __IVwRootBox_FWD_DEFINED__ */


#ifndef __IVwPropertyStore_FWD_DEFINED__
#define __IVwPropertyStore_FWD_DEFINED__
typedef interface IVwPropertyStore IVwPropertyStore;

#endif 	/* __IVwPropertyStore_FWD_DEFINED__ */


#ifndef __IVwOverlay_FWD_DEFINED__
#define __IVwOverlay_FWD_DEFINED__
typedef interface IVwOverlay IVwOverlay;

#endif 	/* __IVwOverlay_FWD_DEFINED__ */


#ifndef __IVwPrintContext_FWD_DEFINED__
#define __IVwPrintContext_FWD_DEFINED__
typedef interface IVwPrintContext IVwPrintContext;

#endif 	/* __IVwPrintContext_FWD_DEFINED__ */


#ifndef __IVwSearchKiller_FWD_DEFINED__
#define __IVwSearchKiller_FWD_DEFINED__
typedef interface IVwSearchKiller IVwSearchKiller;

#endif 	/* __IVwSearchKiller_FWD_DEFINED__ */


#ifndef __IVwSynchronizer_FWD_DEFINED__
#define __IVwSynchronizer_FWD_DEFINED__
typedef interface IVwSynchronizer IVwSynchronizer;

#endif 	/* __IVwSynchronizer_FWD_DEFINED__ */


#ifndef __IVwVirtualHandler_FWD_DEFINED__
#define __IVwVirtualHandler_FWD_DEFINED__
typedef interface IVwVirtualHandler IVwVirtualHandler;

#endif 	/* __IVwVirtualHandler_FWD_DEFINED__ */


#ifndef __IVwLayoutStream_FWD_DEFINED__
#define __IVwLayoutStream_FWD_DEFINED__
typedef interface IVwLayoutStream IVwLayoutStream;

#endif 	/* __IVwLayoutStream_FWD_DEFINED__ */


#ifndef __IVwLayoutManager_FWD_DEFINED__
#define __IVwLayoutManager_FWD_DEFINED__
typedef interface IVwLayoutManager IVwLayoutManager;

#endif 	/* __IVwLayoutManager_FWD_DEFINED__ */


#ifndef __VwCacheDa_FWD_DEFINED__
#define __VwCacheDa_FWD_DEFINED__

#ifdef __cplusplus
typedef class VwCacheDa VwCacheDa;
#else
typedef struct VwCacheDa VwCacheDa;
#endif /* __cplusplus */

#endif 	/* __VwCacheDa_FWD_DEFINED__ */


#ifndef __VwUndoDa_FWD_DEFINED__
#define __VwUndoDa_FWD_DEFINED__

#ifdef __cplusplus
typedef class VwUndoDa VwUndoDa;
#else
typedef struct VwUndoDa VwUndoDa;
#endif /* __cplusplus */

#endif 	/* __VwUndoDa_FWD_DEFINED__ */


#ifndef __VwRootBox_FWD_DEFINED__
#define __VwRootBox_FWD_DEFINED__

#ifdef __cplusplus
typedef class VwRootBox VwRootBox;
#else
typedef struct VwRootBox VwRootBox;
#endif /* __cplusplus */

#endif 	/* __VwRootBox_FWD_DEFINED__ */


#ifndef __VwInvertedRootBox_FWD_DEFINED__
#define __VwInvertedRootBox_FWD_DEFINED__

#ifdef __cplusplus
typedef class VwInvertedRootBox VwInvertedRootBox;
#else
typedef struct VwInvertedRootBox VwInvertedRootBox;
#endif /* __cplusplus */

#endif 	/* __VwInvertedRootBox_FWD_DEFINED__ */


#ifndef __VwPropertyStore_FWD_DEFINED__
#define __VwPropertyStore_FWD_DEFINED__

#ifdef __cplusplus
typedef class VwPropertyStore VwPropertyStore;
#else
typedef struct VwPropertyStore VwPropertyStore;
#endif /* __cplusplus */

#endif 	/* __VwPropertyStore_FWD_DEFINED__ */


#ifndef __VwOverlay_FWD_DEFINED__
#define __VwOverlay_FWD_DEFINED__

#ifdef __cplusplus
typedef class VwOverlay VwOverlay;
#else
typedef struct VwOverlay VwOverlay;
#endif /* __cplusplus */

#endif 	/* __VwOverlay_FWD_DEFINED__ */


#ifndef __VwPrintContextWin32_FWD_DEFINED__
#define __VwPrintContextWin32_FWD_DEFINED__

#ifdef __cplusplus
typedef class VwPrintContextWin32 VwPrintContextWin32;
#else
typedef struct VwPrintContextWin32 VwPrintContextWin32;
#endif /* __cplusplus */

#endif 	/* __VwPrintContextWin32_FWD_DEFINED__ */


#ifndef __IVwPattern_FWD_DEFINED__
#define __IVwPattern_FWD_DEFINED__
typedef interface IVwPattern IVwPattern;

#endif 	/* __IVwPattern_FWD_DEFINED__ */


#ifndef __VwPattern_FWD_DEFINED__
#define __VwPattern_FWD_DEFINED__

#ifdef __cplusplus
typedef class VwPattern VwPattern;
#else
typedef struct VwPattern VwPattern;
#endif /* __cplusplus */

#endif 	/* __VwPattern_FWD_DEFINED__ */


#ifndef __IVwTxtSrcInit2_FWD_DEFINED__
#define __IVwTxtSrcInit2_FWD_DEFINED__
typedef interface IVwTxtSrcInit2 IVwTxtSrcInit2;

#endif 	/* __IVwTxtSrcInit2_FWD_DEFINED__ */


#ifndef __VwMappedTxtSrc_FWD_DEFINED__
#define __VwMappedTxtSrc_FWD_DEFINED__

#ifdef __cplusplus
typedef class VwMappedTxtSrc VwMappedTxtSrc;
#else
typedef struct VwMappedTxtSrc VwMappedTxtSrc;
#endif /* __cplusplus */

#endif 	/* __VwMappedTxtSrc_FWD_DEFINED__ */


#ifndef __IVwTxtSrcInit_FWD_DEFINED__
#define __IVwTxtSrcInit_FWD_DEFINED__
typedef interface IVwTxtSrcInit IVwTxtSrcInit;

#endif 	/* __IVwTxtSrcInit_FWD_DEFINED__ */


#ifndef __VwStringTextSource_FWD_DEFINED__
#define __VwStringTextSource_FWD_DEFINED__

#ifdef __cplusplus
typedef class VwStringTextSource VwStringTextSource;
#else
typedef struct VwStringTextSource VwStringTextSource;
#endif /* __cplusplus */

#endif 	/* __VwStringTextSource_FWD_DEFINED__ */


#ifndef __VwSearchKiller_FWD_DEFINED__
#define __VwSearchKiller_FWD_DEFINED__

#ifdef __cplusplus
typedef class VwSearchKiller VwSearchKiller;
#else
typedef struct VwSearchKiller VwSearchKiller;
#endif /* __cplusplus */

#endif 	/* __VwSearchKiller_FWD_DEFINED__ */


#ifndef __IVwDrawRootBuffered_FWD_DEFINED__
#define __IVwDrawRootBuffered_FWD_DEFINED__
typedef interface IVwDrawRootBuffered IVwDrawRootBuffered;

#endif 	/* __IVwDrawRootBuffered_FWD_DEFINED__ */


#ifndef __VwDrawRootBuffered_FWD_DEFINED__
#define __VwDrawRootBuffered_FWD_DEFINED__

#ifdef __cplusplus
typedef class VwDrawRootBuffered VwDrawRootBuffered;
#else
typedef struct VwDrawRootBuffered VwDrawRootBuffered;
#endif /* __cplusplus */

#endif 	/* __VwDrawRootBuffered_FWD_DEFINED__ */


#ifndef __VwSynchronizer_FWD_DEFINED__
#define __VwSynchronizer_FWD_DEFINED__

#ifdef __cplusplus
typedef class VwSynchronizer VwSynchronizer;
#else
typedef struct VwSynchronizer VwSynchronizer;
#endif /* __cplusplus */

#endif 	/* __VwSynchronizer_FWD_DEFINED__ */


#ifndef __ILgCollatingEngine_FWD_DEFINED__
#define __ILgCollatingEngine_FWD_DEFINED__
typedef interface ILgCollatingEngine ILgCollatingEngine;

#endif 	/* __ILgCollatingEngine_FWD_DEFINED__ */


#ifndef __LgUnicodeCollater_FWD_DEFINED__
#define __LgUnicodeCollater_FWD_DEFINED__

#ifdef __cplusplus
typedef class LgUnicodeCollater LgUnicodeCollater;
#else
typedef struct LgUnicodeCollater LgUnicodeCollater;
#endif /* __cplusplus */

#endif 	/* __LgUnicodeCollater_FWD_DEFINED__ */


#ifndef __VwLayoutStream_FWD_DEFINED__
#define __VwLayoutStream_FWD_DEFINED__

#ifdef __cplusplus
typedef class VwLayoutStream VwLayoutStream;
#else
typedef struct VwLayoutStream VwLayoutStream;
#endif /* __cplusplus */

#endif 	/* __VwLayoutStream_FWD_DEFINED__ */


#ifndef __IPictureFactory_FWD_DEFINED__
#define __IPictureFactory_FWD_DEFINED__
typedef interface IPictureFactory IPictureFactory;

#endif 	/* __IPictureFactory_FWD_DEFINED__ */


#ifndef __PictureFactory_FWD_DEFINED__
#define __PictureFactory_FWD_DEFINED__

#ifdef __cplusplus
typedef class PictureFactory PictureFactory;
#else
typedef struct PictureFactory PictureFactory;
#endif /* __cplusplus */

#endif 	/* __PictureFactory_FWD_DEFINED__ */


#ifndef __IVwWindow_FWD_DEFINED__
#define __IVwWindow_FWD_DEFINED__
typedef interface IVwWindow IVwWindow;

#endif 	/* __IVwWindow_FWD_DEFINED__ */


#ifndef __VwWindow_FWD_DEFINED__
#define __VwWindow_FWD_DEFINED__

#ifdef __cplusplus
typedef class VwWindow VwWindow;
#else
typedef struct VwWindow VwWindow;
#endif /* __cplusplus */

#endif 	/* __VwWindow_FWD_DEFINED__ */


#ifndef __IViewInputMgr_FWD_DEFINED__
#define __IViewInputMgr_FWD_DEFINED__
typedef interface IViewInputMgr IViewInputMgr;

#endif 	/* __IViewInputMgr_FWD_DEFINED__ */


#ifndef __VwStylesheet_FWD_DEFINED__
#define __VwStylesheet_FWD_DEFINED__

#ifdef __cplusplus
typedef class VwStylesheet VwStylesheet;
#else
typedef struct VwStylesheet VwStylesheet;
#endif /* __cplusplus */

#endif 	/* __VwStylesheet_FWD_DEFINED__ */


/* header files for imported files */
#include "oaidl.h"
#include "ocidl.h"

#ifdef __cplusplus
extern "C"{
#endif 


/* interface __MIDL_itf_ViewsTlb_0000_0000 */
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
f6d10640-c00c-11d2-8078-0000c0fb81b5
,
Views
);


extern RPC_IF_HANDLE __MIDL_itf_ViewsTlb_0000_0000_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_ViewsTlb_0000_0000_v0_0_s_ifspec;


#ifndef __Views_LIBRARY_DEFINED__
#define __Views_LIBRARY_DEFINED__

/* library Views */
/* [helpstring][version][uuid] */ 






typedef /* [v1_enum] */ 
enum LgLBP
    {
        klbpAI	= 0,
        klbpAL	= ( klbpAI + 1 ) ,
        klbpB2	= ( klbpAL + 1 ) ,
        klbpBA	= ( klbpB2 + 1 ) ,
        klbpBB	= ( klbpBA + 1 ) ,
        klbpBK	= ( klbpBB + 1 ) ,
        klbpCB	= ( klbpBK + 1 ) ,
        klbpCL	= ( klbpCB + 1 ) ,
        klbpCM	= ( klbpCL + 1 ) ,
        klbpCR	= ( klbpCM + 1 ) ,
        klbpEX	= ( klbpCR + 1 ) ,
        klbpGL	= ( klbpEX + 1 ) ,
        klbpHY	= ( klbpGL + 1 ) ,
        klbpID	= ( klbpHY + 1 ) ,
        klbpIN	= ( klbpID + 1 ) ,
        klbpIS	= ( klbpIN + 1 ) ,
        klbpLF	= ( klbpIS + 1 ) ,
        klbpNS	= ( klbpLF + 1 ) ,
        klbpNU	= ( klbpNS + 1 ) ,
        klbpOP	= ( klbpNU + 1 ) ,
        klbpPO	= ( klbpOP + 1 ) ,
        klbpPR	= ( klbpPO + 1 ) ,
        klbpQU	= ( klbpPR + 1 ) ,
        klbpSA	= ( klbpQU + 1 ) ,
        klbpSG	= ( klbpSA + 1 ) ,
        klbpSP	= ( klbpSG + 1 ) ,
        klbpSY	= ( klbpSP + 1 ) ,
        klbpXX	= ( klbpSY + 1 ) ,
        klbpZW	= ( klbpXX + 1 ) 
    } 	LgLBP;

typedef /* [v1_enum] */ 
enum LgLineBreak
    {
        klbNoBreak	= 0,
        klbWsBreak	= 10,
        klbWordBreak	= 15,
        klbGoodBreak	= 19,
        klbHyphenBreak	= 20,
        klbLetterBreak	= 30,
        klbClipBreak	= 40
    } 	LgLineBreak;

typedef /* [v1_enum] */ 
enum LgLineBreakStatus
    {
        kflbsBrk	= 0x1,
        kflbsSpace	= 0x2,
        kflbsBrkL	= 0x4
    } 	LgLineBreakStatus;

typedef /* [v1_enum] */ 
enum LgIPDrawMode
    {
        kdmNormal	= 0,
        kdmSplitPrimary	= ( kdmNormal + 1 ) ,
        kdmSplitSecondary	= ( kdmSplitPrimary + 1 ) 
    } 	LgIPDrawMode;

typedef /* [v1_enum] */ 
enum LgIpValidResult
    {
        kipvrOK	= 0,
        kipvrBad	= ( kipvrOK + 1 ) ,
        kipvrUnknown	= ( kipvrBad + 1 ) 
    } 	LgIpValidResult;

typedef /* [v1_enum] */ 
enum LgTrailingWsHandling
    {
        ktwshAll	= 0,
        ktwshNoWs	= ( ktwshAll + 1 ) ,
        ktwshOnlyWs	= ( ktwshNoWs + 1 ) 
    } 	LgTrailingWsHandling;

typedef /* [v1_enum] */ 
enum LgUtfForm
    {
        kutf8	= 0,
        kutf16	= ( kutf8 + 1 ) ,
        kutf32	= ( kutf16 + 1 ) 
    } 	LgUtfForm;

typedef /* [v1_enum] */ 
enum VwGenericFontNames
    {
        kvgfnCustom	= 0,
        kvgfnSerif	= ( kvgfnCustom + 1 ) ,
        kvgfnSansSerif	= ( kvgfnSerif + 1 ) ,
        kvgfnMonospace	= ( kvgfnSansSerif + 1 ) 
    } 	VwGenericFontNames;

typedef /* [v1_enum] */ 
enum VwFontStyle
    {
        kfsNormal	= 0,
        kfsItalic	= ( kfsNormal + 1 ) ,
        kfsOblique	= ( kfsItalic + 1 ) 
    } 	VwFontStyle;

typedef /* [v1_enum] */ 
enum VwTextUnderline
    {
        ktuNoUnderline	= 0,
        ktuSingleUnderline	= ( ktuNoUnderline + 1 ) 
    } 	VwTextUnderline;

typedef 
enum ScriptDirCode
    {
        kfsdcNone	= 0,
        kfsdcHorizLtr	= 1,
        kfsdcHorizRtl	= 2,
        kfsdcVertFromLeft	= 4,
        kfsdcVertFromRight	= 8
    } 	ScriptDirCode;

typedef 
enum JustGlyphAttr
    {
        kjgatStretch	= 1,
        kjgatShrink	= ( kjgatStretch + 1 ) ,
        kjgatWeight	= ( kjgatShrink + 1 ) ,
        kjgatStep	= ( kjgatWeight + 1 ) ,
        kjgatChunk	= ( kjgatStep + 1 ) ,
        kjgatWidth	= ( kjgatChunk + 1 ) ,
        kjgatBreak	= ( kjgatWidth + 1 ) ,
        kjgatStretchInSteps	= ( kjgatBreak + 1 ) ,
        kjgatWidthInSteps	= ( kjgatStretchInSteps + 1 ) ,
        kjgatAdvWidth	= ( kjgatWidthInSteps + 1 ) ,
        kjgatAdvHeight	= ( kjgatAdvWidth + 1 ) ,
        kjgatBbLeft	= ( kjgatAdvHeight + 1 ) ,
        kjgatBbRight	= ( kjgatBbLeft + 1 ) ,
        kjgatBbTop	= ( kjgatBbRight + 1 ) ,
        kjgatBbBottom	= ( kjgatBbTop + 1 ) 
    } 	JustGlyphAttr;

typedef /* [public][public] */ struct __MIDL___MIDL_itf_ViewsTlb_0000_0000_0001
    {
    ScriptDirCode sdcPara;
    ScriptDirCode sdcOuter;
    } 	LgParaRenderProps;

typedef /* [public][public] */ struct __MIDL___MIDL_itf_ViewsTlb_0000_0000_0002
    {
    OLECHAR glyphIndex;
    int x;
    int y;
    } 	GlyphInfo;

GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgLineBreaker
,
F8D5FDE9-9695-4D63-8843-E27FD880BFF0
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwGraphics
,
F7233278-EA87-4FC9-83E2-CB7CC45DEBE7
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwGraphicsWin32
,
C955E295-A259-47D4-8158-4C7A3539D35E
);
ATTACH_GUID_TO_CLASS(class,
D888DB98-83A9-4592-AAD2-F18F6F74AB87
,
VwGraphicsWin32
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwTextSource
,
6C0465AC-17C5-4C9C-8AF3-62221F2F7707
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwJustifier
,
22D5E030-5239-4924-BF1B-6B4F2CBBABA5
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgSegment
,
3818E245-6A0B-45A7-A5D6-52694931279E
);
typedef /* [public][public][v1_enum] */ 
enum __MIDL___MIDL_itf_ViewsTlb_0001_0071_0001
    {
        kestNoMore	= 0,
        kestMoreLines	= ( kestNoMore + 1 ) ,
        kestHardBreak	= ( kestMoreLines + 1 ) ,
        kestBadBreak	= ( kestHardBreak + 1 ) ,
        kestOkayBreak	= ( kestBadBreak + 1 ) ,
        kestWsBreak	= ( kestOkayBreak + 1 ) ,
        kestMoreWhtsp	= ( kestWsBreak + 1 ) ,
        kestNothingFit	= ( kestMoreWhtsp + 1 ) 
    } 	LgEndSegmentType;

GENERIC_DECLARE_SMART_INTERFACE_PTR(
IRenderEngine
,
B8998799-3C5F-47D3-BC14-10AA10AEC691
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IRenderEngineFactory
,
97D3D3E7-042B-4790-AB70-8D6C3A677576
);
ATTACH_GUID_TO_CLASS(class,
94FBFA34-21E5-4A1E-B576-BA5D76CC051A
,
LgLineBreaker
);
ATTACH_GUID_TO_CLASS(class,
6EACAB83-6BDC-49CA-8F66-8C116D3EEBD8
,
RomRenderEngine
);
ATTACH_GUID_TO_CLASS(class,
1287735C-3CAD-41CD-986C-39D7C0DF0314
,
UniscribeEngine
);
ATTACH_GUID_TO_CLASS(class,
62EBEEBF-14EA-43D9-A27A-EF013E14145A
,
GraphiteEngine
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IRenderingFeatures
,
75AFE861-3C17-4F16-851F-A36F5FFABCC6
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IJustifyingRenderer
,
1141174B-923F-4C43-BA43-8A326B76A3F2
);























typedef int HVO;

typedef int PropTag;

typedef /* [v1_enum] */ 
enum VwSpecialChars
    {
        kscBackspace	= 8,
        kscDelForward	= 0x7f
    } 	VwSpecialChars;

typedef /* [v1_enum] */ 
enum VwSelType
    {
        kstText	= 1,
        kstPicture	= 2
    } 	VwSelType;

typedef /* [v1_enum] */ 
enum VwDelProbType
    {
        kdptNone	= 0,
        kdptComplexRange	= ( kdptNone + 1 ) ,
        kdptBsAtStartPara	= ( kdptComplexRange + 1 ) ,
        kdptDelAtEndPara	= ( kdptBsAtStartPara + 1 ) ,
        kdptBsReadOnly	= ( kdptDelAtEndPara + 1 ) ,
        kdptDelReadOnly	= ( kdptBsReadOnly + 1 ) ,
        kdptReadOnly	= ( kdptDelReadOnly + 1 ) 
    } 	VwDelProbType;

typedef /* [v1_enum] */ 
enum VwDelProbResponse
    {
        kdprAbort	= 0,
        kdprFail	= ( kdprAbort + 1 ) ,
        kdprDone	= ( kdprFail + 1 ) 
    } 	VwDelProbResponse;

typedef /* [v1_enum] */ 
enum VwInsertDiffParaResponse
    {
        kidprDefault	= 0,
        kidprFail	= ( kidprDefault + 1 ) ,
        kidprDone	= ( kidprFail + 1 ) 
    } 	VwInsertDiffParaResponse;

typedef /* [v1_enum] */ 
enum DbColType
    {
        koctGuid	= 0,
        koctInt	= 1,
        koctString	= 2,
        koctFmt	= 3,
        koctMlaAlt	= 4,
        koctMlsAlt	= 5,
        koctMltAlt	= 6,
        koctObj	= 7,
        koctObjVec	= 8,
        koctBaseId	= 9,
        koctTtp	= 10,
        koctUnicode	= 11,
        koctInt64	= 12,
        koctTime	= 13,
        koctEnc	= 14,
        koctFlid	= 15,
        koctTimeStamp	= 16,
        koctObjOwn	= 17,
        koctObjVecOwn	= 18,
        koctBinary	= 19,
        koctTimeStampIfMissing	= 20,
        koctLim	= 21,
        koctObjVecExtra	= 22
    } 	DbColType;

typedef /* [v1_enum] */ 
enum AutoloadPolicies
    {
        kalpLoadForThisObject	= 0,
        kalpNoAutoload	= 1,
        kalpLoadForAllOfObjectClass	= 2,
        kalpLoadForAllOfBaseClass	= 3,
        kalpLoadAllOfClassForReadOnly	= 4,
        kalpLoadAllOfClassIncludingAllVirtuals	= 5,
        kalpLim	= 6
    } 	AutoloadPolicies;

typedef /* [v1_enum] */ 
enum FldType
    {
        kftString	= 0,
        kftMsa	= ( kftString + 1 ) ,
        kftMta	= ( kftMsa + 1 ) ,
        kftRefAtomic	= ( kftMta + 1 ) ,
        kftRefCombo	= ( kftRefAtomic + 1 ) ,
        kftRefSeq	= ( kftRefCombo + 1 ) ,
        kftEnum	= ( kftRefSeq + 1 ) ,
        kftUnicode	= ( kftEnum + 1 ) ,
        kftTtp	= ( kftUnicode + 1 ) ,
        kftStText	= ( kftTtp + 1 ) ,
        kftDummy	= ( kftStText + 1 ) ,
        kftLimEmbedLabel	= ( kftDummy + 1 ) ,
        kftGroup	= ( kftLimEmbedLabel + 1 ) ,
        kftGroupOnePerLine	= ( kftGroup + 1 ) ,
        kftTitleGroup	= ( kftGroupOnePerLine + 1 ) ,
        kftDateRO	= ( kftTitleGroup + 1 ) ,
        kftDate	= ( kftDateRO + 1 ) ,
        kftGenDate	= ( kftDate + 1 ) ,
        kftSubItems	= ( kftGenDate + 1 ) ,
        kftObjRefAtomic	= ( kftSubItems + 1 ) ,
        kftObjRefSeq	= ( kftObjRefAtomic + 1 ) ,
        kftInteger	= ( kftObjRefSeq + 1 ) ,
        kftBackRefAtomic	= ( kftInteger + 1 ) ,
        kftExpandable	= ( kftBackRefAtomic + 1 ) ,
        kftObjOwnSeq	= ( kftExpandable + 1 ) ,
        kftObjOwnCol	= ( kftObjOwnSeq + 1 ) ,
        kftGuid	= ( kftObjOwnCol + 1 ) ,
        kftStTextParas	= ( kftGuid + 1 ) ,
        kftLim	= ( kftStTextParas + 1 ) 
    } 	FldType;

typedef /* [v1_enum] */ 
enum VwBoxType
    {
        kvbtUnknown	= 0,
        kvbtGroup	= ( kvbtUnknown + 1 ) ,
        kvbtParagraph	= ( kvbtGroup + 1 ) ,
        kvbtConcPara	= ( kvbtParagraph + 1 ) ,
        kvbtPile	= ( kvbtConcPara + 1 ) ,
        kvbtInnerPile	= ( kvbtPile + 1 ) ,
        kvbtMoveablePile	= ( kvbtInnerPile + 1 ) ,
        kvbtDiv	= ( kvbtMoveablePile + 1 ) ,
        kvbtRoot	= ( kvbtDiv + 1 ) ,
        kvbtTable	= ( kvbtRoot + 1 ) ,
        kvbtTableRow	= ( kvbtTable + 1 ) ,
        kvbtTableCell	= ( kvbtTableRow + 1 ) ,
        kvbtLeaf	= ( kvbtTableCell + 1 ) ,
        kvbtString	= ( kvbtLeaf + 1 ) ,
        kvbtDropCapString	= ( kvbtString + 1 ) ,
        kvbtAnchor	= ( kvbtDropCapString + 1 ) ,
        kvbtSeparator	= ( kvbtAnchor + 1 ) ,
        kvbtBar	= ( kvbtSeparator + 1 ) ,
        kvbtPicture	= ( kvbtBar + 1 ) ,
        kvbtIndepPicture	= ( kvbtPicture + 1 ) ,
        kvbtIntegerPicture	= ( kvbtIndepPicture + 1 ) ,
        kvbtLazy	= ( kvbtIntegerPicture + 1 ) 
    } 	VwBoxType;

typedef /* [v1_enum] */ 
enum VwShiftStatus
    {
        kfssNone	= 0,
        kfssShift	= 1,
        kfssControl	= 2,
        kgrfssShiftControl	= ( kfssShift | kfssControl ) 
    } 	VwShiftStatus;

typedef struct VwSelLevInfo
    {
    PropTag tag;
    int cpropPrevious;
    int ihvo;
    int hvo;
    int ws;
    int ich;
    } 	VwSelLevInfo;

typedef struct VwChangeInfo
    {
    HVO hvo;
    PropTag tag;
    int ivIns;
    int cvIns;
    int cvDel;
    } 	VwChangeInfo;

typedef /* [v1_enum] */ 
enum VwUnit
    {
        kunPoint1000	= 0,
        kunPercent100	= 1,
        kunRelative	= 2
    } 	VwUnit;

typedef /* [public][public][public][public][public] */ struct __MIDL___MIDL_itf_ViewsTlb_0001_0079_0001
    {
    int nVal;
    VwUnit unit;
    } 	VwLength;

typedef /* [v1_enum] */ 
enum VwAlignment
    {
        kvaLeft	= 0,
        kvaCenter	= ( kvaLeft + 1 ) ,
        kvaRight	= ( kvaCenter + 1 ) ,
        kvaJustified	= ( kvaRight + 1 ) 
    } 	VwAlignment;

typedef /* [v1_enum] */ 
enum VwFramePosition
    {
        kvfpVoid	= 0,
        kvfpAbove	= 0x1,
        kvfpBelow	= 0x2,
        kvfpLhs	= 0x4,
        kvfpRhs	= 0x8,
        kvfpHsides	= ( kvfpAbove | kvfpBelow ) ,
        kvfpVsides	= ( kvfpLhs | kvfpRhs ) ,
        kvfpBox	= ( kvfpHsides | kvfpVsides ) 
    } 	VwFramePosition;

typedef /* [v1_enum] */ 
enum VwRule
    {
        kvrlNone	= 0,
        kvrlGroups	= 0x1,
        kvrlRowNoGroups	= 0x2,
        kvrlRows	= ( kvrlGroups | kvrlRowNoGroups ) ,
        kvrlColsNoGroups	= 0x4,
        kvrlCols	= ( kvrlGroups | kvrlColsNoGroups ) ,
        kvrlAll	= ( kvrlRows | kvrlCols ) 
    } 	VwRule;

typedef /* [v1_enum] */ 
enum VwStyleProperty
    {
        kspNamedStyle	= 133,
        kspMarginLeading	= 19,
        kspMarginTrailing	= 20,
        kspMarginTop	= 21,
        kspMarginBottom	= 22,
        kspMaxLines	= 151,
        kspWsStyle	= 156,
        kspRelLineHeight	= 160
    } 	VwStyleProperty;

typedef /* [v1_enum] */ 
enum VwFontAbsoluteSize
    {
        kvfsXXSmall	= 0,
        kvfsXSmall	= ( kvfsXXSmall + 1 ) ,
        kvfsSmall	= ( kvfsXSmall + 1 ) ,
        kvfsNormal	= ( kvfsSmall + 1 ) ,
        kvfsLarge	= ( kvfsNormal + 1 ) ,
        kvfsXLarge	= ( kvfsLarge + 1 ) ,
        kvfsXXLarge	= ( kvfsXLarge + 1 ) ,
        kvfsSmaller	= ( kvfsXXLarge + 1 ) ,
        kvfsLarger	= ( kvfsSmaller + 1 ) 
    } 	VwFontAbsoluteSize;

typedef /* [v1_enum] */ 
enum VwFontWeight
    {
        kvfw100	= 100,
        kvfw200	= 200,
        kvfw300	= 300,
        kvfw400	= 400,
        kvfw500	= 500,
        kvfw600	= 600,
        kvfw700	= 700,
        kvfw800	= 800,
        kvfw900	= 900,
        kvfwNormal	= 400,
        kvfwBold	= 700,
        kvfwBolder	= -1,
        kvfwLighter	= -2
    } 	VwFontWeight;

typedef /* [v1_enum] */ 
enum VwSpecialAttrTags
    {
        ktagNotAnAttr	= -1,
        ktagGapInAttrs	= -2
    } 	VwSpecialAttrTags;

typedef /* [v1_enum] */ 
enum VwClearInfoAction
    {
        kciaRemoveObjectInfoOnly	= 0,
        kciaRemoveObjectAndOwnedInfo	= 1,
        kciaRemoveAllObjectInfo	= 2
    } 	VwClearInfoAction;

typedef /* [v1_enum] */ 
enum VwSelectionState
    {
        vssDisabled	= 0,
        vssOutOfFocus	= ( vssDisabled + 1 ) ,
        vssEnabled	= ( vssOutOfFocus + 1 ) ,
        vssLim	= ( vssEnabled + 1 ) 
    } 	VwSelectionState;

typedef /* [v1_enum] */ 
enum VwPrepDrawResult
    {
        kxpdrNormal	= 0,
        kxpdrAdjust	= ( kxpdrNormal + 1 ) ,
        kxpdrInvalidate	= ( kxpdrAdjust + 1 ) ,
        kxpdrLim	= ( kxpdrInvalidate + 1 ) 
    } 	VwPrepDrawResult;

typedef /* [v1_enum] */ 
enum VwBoundaryMark
    {
        none	= 0,
        endOfParagraph	= ( none + 1 ) ,
        endOfSection	= ( endOfParagraph + 1 ) ,
        endOfParagraphHighlighted	= ( endOfSection + 1 ) ,
        endofSectionHighlighted	= ( endOfParagraphHighlighted + 1 ) 
    } 	VwBoundaryMark;

GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwVirtualHandler
,
581E3FE0-F0C0-42A7-96C7-76B23B8BE580
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwCacheDa
,
FECFCBC9-4CD2-4DB3-ADBE-1F628B2DAA33
);
ATTACH_GUID_TO_CLASS(class,
81EE73B1-BE31-49cf-BC02-6030113AC56F
,
VwCacheDa
);
ATTACH_GUID_TO_CLASS(class,
5BEEFFC6-E88C-4258-A269-D58390A1F2C9
,
VwUndoDa
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwRootBox
,
06DAA10B-F69A-4000-8C8B-3F725C3FC368
);
ATTACH_GUID_TO_CLASS(class,
7C0C6A3C-38B3-4266-AF94-A3A1CBAAD1FC
,
VwRootBox
);
ATTACH_GUID_TO_CLASS(class,
73BCAB14-2537-4b7d-B1C7-7E3DD7A089AD
,
VwInvertedRootBox
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwViewConstructor
,
5b1a08f6-9af9-46f9-9fd7-1011a3039191
);
typedef /* [v1_enum] */ 
enum VwScrollSelOpts
    {
        kssoDefault	= 1,
        kssoNearTop	= 2,
        kssoTop	= 3,
        kssoBoth	= 4
    } 	VwScrollSelOpts;

GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwRootSite
,
C999413C-28C8-481c-9543-B06C92B812D1
);
typedef /* [v1_enum] */ 
enum VwConcParaOpts
    {
        kcpoBold	= 1,
        kcpoAlign	= 2,
        kcpoDefault	= 3
    } 	VwConcParaOpts;

typedef struct DispPropOverride
    {
    /* external definition not present */ LgCharRenderProps chrp;
    int ichMin;
    int ichLim;
    } 	DispPropOverride;

GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwEnv
,
92B462E8-75D5-42c1-8B63-84878E8964C0
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwEmbeddedWindow
,
f6d10646-c00c-11d2-8078-0000c0fb81b5
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwSelection
,
4F8B678D-C5BA-4a2f-B9B3-2780956E3616
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwPropertyStore
,
3D4847FE-EA2D-4255-A496-770059A134CC
);
ATTACH_GUID_TO_CLASS(class,
CB59916A-C532-4a57-8CB4-6E1508B4DEC1
,
VwPropertyStore
);
typedef /* [v1_enum] */ 
enum VwOverlayFlags
    {
        kfofTagsUseAttribs	= 1,
        kfofLeadBracket	= 2,
        kfofLeadTag	= 4,
        kfofTrailBracket	= 8,
        kfofTrailTag	= 16,
        kgrfofTagAbove	= 6,
        kgrfofTagBelow	= 24,
        kgrfofTagAnywhere	= 30,
        kgrfofBracketAnywhere	= 10,
        kgrfofDefault	= 31
    } 	VwOverlayFlags;

typedef /* [v1_enum] */ 
enum VwConst1
    {
        kcchGuidRepLength	= 8
    } 	VwConst1;

typedef /* [v1_enum] */ 
enum FwOverlaySetMask
    {
        kosmAbbr	= 0x1,
        kosmName	= 0x2,
        kosmClrFore	= 0x4,
        kosmClrBack	= 0x8,
        kosmClrUnder	= 0x10,
        kosmUnderType	= 0x20,
        kosmHidden	= 0x40,
        kosmAll	= 0x7f
    } 	FwOverlaySetMask;

GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwOverlay
,
7D9089C1-3BB9-11d4-8078-0000C0FB81B5
);
ATTACH_GUID_TO_CLASS(class,
73F5DB01-3D2A-11d4-8078-0000C0FB81B5
,
VwOverlay
);
typedef /* [v1_enum] */ 
enum VwHeaderPositions
    {
        kvhpLeft	= 1,
        kvhpRight	= 2,
        kvhpOutside	= 4,
        kvhpInside	= 8,
        kvhpCenter	= 16,
        kvhpOdd	= 32,
        kvhpEven	= 64,
        kvhpTop	= 128,
        kvhpBottom	= 256,
        kvhpFirst	= 512,
        kgrfvhpNormal	= 915
    } 	VwHeaderPositions;

GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwPrintContext
,
FF2E1DC2-95A8-41c6-85F4-FFCA3A64216A
);
ATTACH_GUID_TO_CLASS(class,
5E9FB977-66AE-4c16-A036-1D40E7713573
,
VwPrintContextWin32
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwPattern
,
EFEBBD00-D418-4157-A730-C648BFFF3D8D
);
ATTACH_GUID_TO_CLASS(class,
6C659C76-3991-48dd-93F7-DA65847D4863
,
VwPattern
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwTxtSrcInit2
,
8E3EFDB9-4721-4f17-AA50-48DF65078680
);
ATTACH_GUID_TO_CLASS(class,
01D1C8A7-1222-49c9-BFE6-30A84CE76A40
,
VwMappedTxtSrc
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwTxtSrcInit
,
1AB3C970-3EC1-4d97-A7B8-122642AF6333
);
ATTACH_GUID_TO_CLASS(class,
DAF01E81-3026-4480-8783-EEA04CD2EC80
,
VwStringTextSource
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwSearchKiller
,
FF1B39DE-20D3-4cdd-A134-DCBE3BE23F3E
);
ATTACH_GUID_TO_CLASS(class,
4ADA9157-67F8-499b-88CE-D63DF918DF83
,
VwSearchKiller
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwDrawRootBuffered
,
D9E9D65F-E81F-439e-8010-5B22BAEBB92D
);
ATTACH_GUID_TO_CLASS(class,
97199458-10C7-49da-B3AE-EA922EA64859
,
VwDrawRootBuffered
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwSynchronizer
,
C5C1E9DC-5880-4ee3-B3CD-EBDD132A6294
);
ATTACH_GUID_TO_CLASS(class,
5E149A49-CAEE-4823-97F7-BB9DED2A62BC
,
VwSynchronizer
);
typedef /* [v1_enum] */ 
enum LgCollatingOptions
    {
        fcoDefault	= 0,
        fcoIgnoreCase	= 1,
        fcoDontIgnoreVariant	= 2,
        fcoLim	= ( fcoDontIgnoreVariant + 1 ) 
    } 	LgCollatingOptions;

GENERIC_DECLARE_SMART_INTERFACE_PTR(
ILgCollatingEngine
,
D27A3D8C-D3FE-4E25-9097-8F4A1FB30361
);
ATTACH_GUID_TO_CLASS(class,
0D9900D2-1693-481F-AA70-7EA64F264EC4
,
LgUnicodeCollater
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwLayoutStream
,
5DB26616-2741-4688-BC53-24C2A13ACB9A
);
ATTACH_GUID_TO_CLASS(class,
1CD09E06-6978-4969-A1FC-462723587C32
,
VwLayoutStream
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwLayoutManager
,
13F3A421-4915-455b-B57F-AFD4073CFFA0
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IPictureFactory
,
110B7E88-2968-11E0-B493-0019DBF4566E
);
ATTACH_GUID_TO_CLASS(class,
17A2E876-2968-11E0-8046-0019DBF4566E
,
PictureFactory
);
GENERIC_DECLARE_SMART_INTERFACE_PTR(
IVwWindow
,
8856396c-63a9-4bc7-ad47-87ec8b6ef5a4
);
ATTACH_GUID_TO_CLASS(class,
3fb0fcd2-ac55-42a8-b580-73b89a2b6215
,
VwWindow
);
typedef /* [v1_enum] */ 
enum VwMouseEvent
    {
        kmeDown	= 0,
        kmeDblClick	= ( kmeDown + 1 ) ,
        kmeMoveDrag	= ( kmeDblClick + 1 ) ,
        kmeExtend	= ( kmeMoveDrag + 1 ) ,
        kmeUp	= ( kmeExtend + 1 ) 
    } 	VwMouseEvent;

GENERIC_DECLARE_SMART_INTERFACE_PTR(
IViewInputMgr
,
e41668f7-d506-4c8a-a5d7-feae5630797e
);
ATTACH_GUID_TO_CLASS(class,
CCE2A7ED-464C-4ec7-A0B0-E3C1F6B94C5A
,
VwStylesheet
);

#define LIBID_Views __uuidof(Views)

#ifndef __IVwGraphics_INTERFACE_DEFINED__
#define __IVwGraphics_INTERFACE_DEFINED__

/* interface IVwGraphics */
/* [unique][object][uuid] */ 


#define IID_IVwGraphics __uuidof(IVwGraphics)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("F7233278-EA87-4FC9-83E2-CB7CC45DEBE7")
    IVwGraphics : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE InvertRect( 
            /* [in] */ int xLeft,
            /* [in] */ int yTop,
            /* [in] */ int xRight,
            /* [in] */ int yBottom) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_ForeColor( 
            /* [in] */ int clr) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_BackColor( 
            /* [in] */ int clr) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DrawRectangle( 
            /* [in] */ int xLeft,
            /* [in] */ int yTop,
            /* [in] */ int xRight,
            /* [in] */ int yBottom) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DrawHorzLine( 
            /* [in] */ int xLeft,
            /* [in] */ int xRight,
            /* [in] */ int y,
            /* [in] */ int dyHeight,
            /* [in] */ int cdx,
            /* [size_is][in] */ int *prgdx,
            /* [out][in] */ int *pdxStart) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DrawLine( 
            /* [in] */ int xLeft,
            /* [in] */ int yTop,
            /* [in] */ int xRight,
            /* [in] */ int yBottom) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DrawText( 
            /* [in] */ int x,
            /* [in] */ int y,
            /* [in] */ int cch,
            /* [size_is][in] */ const OLECHAR *prgch,
            /* [in] */ int xStretch) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DrawGlyphs( 
            /* [in] */ int x,
            /* [in] */ int y,
            /* [in] */ int cgi,
            /* [size_is][in] */ const GlyphInfo *prggi) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetTextExtent( 
            /* [in] */ int cch,
            /* [size_is][in] */ const OLECHAR *prgch,
            /* [out] */ int *px,
            /* [out] */ int *py) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetTextLeadWidth( 
            /* [in] */ int cch,
            /* [size_is][in] */ const OLECHAR *prgch,
            /* [in] */ int ich,
            /* [in] */ int xStretch,
            /* [retval][out] */ int *px) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetClipRect( 
            /* [out] */ int *pxLeft,
            /* [out] */ int *pyTop,
            /* [out] */ int *pxRight,
            /* [out] */ int *pyBottom) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFontEmSquare( 
            /* [retval][out] */ int *pxyFontEmSquare) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetGlyphMetrics( 
            /* [in] */ int chw,
            /* [out] */ int *psBoundingWidth,
            /* [out] */ int *pyBoundingHeight,
            /* [out] */ int *pxBoundingX,
            /* [out] */ int *pyBoundingY,
            /* [out] */ int *pxAdvanceX,
            /* [out] */ int *pyAdvanceY) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFontData( 
            /* [in] */ int nTableId,
            /* [out][in] */ int *pcbTableSz,
            /* [size_is][out] */ BYTE *prgb) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE XYFromGlyphPoint( 
            /* [in] */ int chw,
            /* [in] */ int nPoint,
            /* [out] */ int *pxRet,
            /* [out] */ int *pyRet) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_FontAscent( 
            /* [retval][out] */ int *py) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_FontDescent( 
            /* [retval][out] */ int *pyRet) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_FontCharProperties( 
            /* [retval][out] */ /* external definition not present */ LgCharRenderProps *pchrp) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ReleaseDC( void) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_XUnitsPerInch( 
            /* [retval][out] */ int *pxInch) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_XUnitsPerInch( 
            /* [in] */ int xInch) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_YUnitsPerInch( 
            /* [retval][out] */ int *pyInch) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_YUnitsPerInch( 
            /* [in] */ int yInch) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetSuperscriptHeightRatio( 
            /* [out] */ int *piNumerator,
            /* [out] */ int *piDenominator) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetSuperscriptYOffsetRatio( 
            /* [out] */ int *piNumerator,
            /* [out] */ int *piDenominator) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetSubscriptHeightRatio( 
            /* [out] */ int *piNumerator,
            /* [out] */ int *piDenominator) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetSubscriptYOffsetRatio( 
            /* [out] */ int *piNumerator,
            /* [out] */ int *piDenominator) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetupGraphics( 
            /* [out][in] */ /* external definition not present */ LgCharRenderProps *pchrp) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE PushClipRect( 
            /* [in] */ RECT rcClip) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE PopClipRect( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DrawPolygon( 
            /* [in] */ int cvpnt,
            /* [size_is][in] */ POINT prgvpnt[  ]) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE RenderPicture( 
            /* [in] */ IPicture *ppic,
            /* [in] */ int x,
            /* [in] */ int y,
            /* [in] */ int cx,
            /* [in] */ int cy,
            /* [in] */ OLE_XPOS_HIMETRIC xSrc,
            /* [in] */ OLE_YPOS_HIMETRIC ySrc,
            /* [in] */ OLE_XSIZE_HIMETRIC cxSrc,
            /* [in] */ OLE_YSIZE_HIMETRIC cySrc,
            /* [in] */ LPCRECT prcWBounds) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE MakePicture( 
            /* [size_is][in] */ byte *pbData,
            /* [in] */ int cbData,
            /* [retval][out] */ IPicture **pppic) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IVwGraphicsVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IVwGraphics * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IVwGraphics * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IVwGraphics * This);
        
        HRESULT ( STDMETHODCALLTYPE *InvertRect )( 
            IVwGraphics * This,
            /* [in] */ int xLeft,
            /* [in] */ int yTop,
            /* [in] */ int xRight,
            /* [in] */ int yBottom);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_ForeColor )( 
            IVwGraphics * This,
            /* [in] */ int clr);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_BackColor )( 
            IVwGraphics * This,
            /* [in] */ int clr);
        
        HRESULT ( STDMETHODCALLTYPE *DrawRectangle )( 
            IVwGraphics * This,
            /* [in] */ int xLeft,
            /* [in] */ int yTop,
            /* [in] */ int xRight,
            /* [in] */ int yBottom);
        
        HRESULT ( STDMETHODCALLTYPE *DrawHorzLine )( 
            IVwGraphics * This,
            /* [in] */ int xLeft,
            /* [in] */ int xRight,
            /* [in] */ int y,
            /* [in] */ int dyHeight,
            /* [in] */ int cdx,
            /* [size_is][in] */ int *prgdx,
            /* [out][in] */ int *pdxStart);
        
        HRESULT ( STDMETHODCALLTYPE *DrawLine )( 
            IVwGraphics * This,
            /* [in] */ int xLeft,
            /* [in] */ int yTop,
            /* [in] */ int xRight,
            /* [in] */ int yBottom);
        
        HRESULT ( STDMETHODCALLTYPE *DrawText )( 
            IVwGraphics * This,
            /* [in] */ int x,
            /* [in] */ int y,
            /* [in] */ int cch,
            /* [size_is][in] */ const OLECHAR *prgch,
            /* [in] */ int xStretch);
        
        HRESULT ( STDMETHODCALLTYPE *DrawGlyphs )( 
            IVwGraphics * This,
            /* [in] */ int x,
            /* [in] */ int y,
            /* [in] */ int cgi,
            /* [size_is][in] */ const GlyphInfo *prggi);
        
        HRESULT ( STDMETHODCALLTYPE *GetTextExtent )( 
            IVwGraphics * This,
            /* [in] */ int cch,
            /* [size_is][in] */ const OLECHAR *prgch,
            /* [out] */ int *px,
            /* [out] */ int *py);
        
        HRESULT ( STDMETHODCALLTYPE *GetTextLeadWidth )( 
            IVwGraphics * This,
            /* [in] */ int cch,
            /* [size_is][in] */ const OLECHAR *prgch,
            /* [in] */ int ich,
            /* [in] */ int xStretch,
            /* [retval][out] */ int *px);
        
        HRESULT ( STDMETHODCALLTYPE *GetClipRect )( 
            IVwGraphics * This,
            /* [out] */ int *pxLeft,
            /* [out] */ int *pyTop,
            /* [out] */ int *pxRight,
            /* [out] */ int *pyBottom);
        
        HRESULT ( STDMETHODCALLTYPE *GetFontEmSquare )( 
            IVwGraphics * This,
            /* [retval][out] */ int *pxyFontEmSquare);
        
        HRESULT ( STDMETHODCALLTYPE *GetGlyphMetrics )( 
            IVwGraphics * This,
            /* [in] */ int chw,
            /* [out] */ int *psBoundingWidth,
            /* [out] */ int *pyBoundingHeight,
            /* [out] */ int *pxBoundingX,
            /* [out] */ int *pyBoundingY,
            /* [out] */ int *pxAdvanceX,
            /* [out] */ int *pyAdvanceY);
        
        HRESULT ( STDMETHODCALLTYPE *GetFontData )( 
            IVwGraphics * This,
            /* [in] */ int nTableId,
            /* [out][in] */ int *pcbTableSz,
            /* [size_is][out] */ BYTE *prgb);
        
        HRESULT ( STDMETHODCALLTYPE *XYFromGlyphPoint )( 
            IVwGraphics * This,
            /* [in] */ int chw,
            /* [in] */ int nPoint,
            /* [out] */ int *pxRet,
            /* [out] */ int *pyRet);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_FontAscent )( 
            IVwGraphics * This,
            /* [retval][out] */ int *py);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_FontDescent )( 
            IVwGraphics * This,
            /* [retval][out] */ int *pyRet);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_FontCharProperties )( 
            IVwGraphics * This,
            /* [retval][out] */ /* external definition not present */ LgCharRenderProps *pchrp);
        
        HRESULT ( STDMETHODCALLTYPE *ReleaseDC )( 
            IVwGraphics * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_XUnitsPerInch )( 
            IVwGraphics * This,
            /* [retval][out] */ int *pxInch);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_XUnitsPerInch )( 
            IVwGraphics * This,
            /* [in] */ int xInch);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_YUnitsPerInch )( 
            IVwGraphics * This,
            /* [retval][out] */ int *pyInch);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_YUnitsPerInch )( 
            IVwGraphics * This,
            /* [in] */ int yInch);
        
        HRESULT ( STDMETHODCALLTYPE *GetSuperscriptHeightRatio )( 
            IVwGraphics * This,
            /* [out] */ int *piNumerator,
            /* [out] */ int *piDenominator);
        
        HRESULT ( STDMETHODCALLTYPE *GetSuperscriptYOffsetRatio )( 
            IVwGraphics * This,
            /* [out] */ int *piNumerator,
            /* [out] */ int *piDenominator);
        
        HRESULT ( STDMETHODCALLTYPE *GetSubscriptHeightRatio )( 
            IVwGraphics * This,
            /* [out] */ int *piNumerator,
            /* [out] */ int *piDenominator);
        
        HRESULT ( STDMETHODCALLTYPE *GetSubscriptYOffsetRatio )( 
            IVwGraphics * This,
            /* [out] */ int *piNumerator,
            /* [out] */ int *piDenominator);
        
        HRESULT ( STDMETHODCALLTYPE *SetupGraphics )( 
            IVwGraphics * This,
            /* [out][in] */ /* external definition not present */ LgCharRenderProps *pchrp);
        
        HRESULT ( STDMETHODCALLTYPE *PushClipRect )( 
            IVwGraphics * This,
            /* [in] */ RECT rcClip);
        
        HRESULT ( STDMETHODCALLTYPE *PopClipRect )( 
            IVwGraphics * This);
        
        HRESULT ( STDMETHODCALLTYPE *DrawPolygon )( 
            IVwGraphics * This,
            /* [in] */ int cvpnt,
            /* [size_is][in] */ POINT prgvpnt[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *RenderPicture )( 
            IVwGraphics * This,
            /* [in] */ IPicture *ppic,
            /* [in] */ int x,
            /* [in] */ int y,
            /* [in] */ int cx,
            /* [in] */ int cy,
            /* [in] */ OLE_XPOS_HIMETRIC xSrc,
            /* [in] */ OLE_YPOS_HIMETRIC ySrc,
            /* [in] */ OLE_XSIZE_HIMETRIC cxSrc,
            /* [in] */ OLE_YSIZE_HIMETRIC cySrc,
            /* [in] */ LPCRECT prcWBounds);
        
        HRESULT ( STDMETHODCALLTYPE *MakePicture )( 
            IVwGraphics * This,
            /* [size_is][in] */ byte *pbData,
            /* [in] */ int cbData,
            /* [retval][out] */ IPicture **pppic);
        
        END_INTERFACE
    } IVwGraphicsVtbl;

    interface IVwGraphics
    {
        CONST_VTBL struct IVwGraphicsVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IVwGraphics_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IVwGraphics_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IVwGraphics_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IVwGraphics_InvertRect(This,xLeft,yTop,xRight,yBottom)	\
    ( (This)->lpVtbl -> InvertRect(This,xLeft,yTop,xRight,yBottom) ) 

#define IVwGraphics_put_ForeColor(This,clr)	\
    ( (This)->lpVtbl -> put_ForeColor(This,clr) ) 

#define IVwGraphics_put_BackColor(This,clr)	\
    ( (This)->lpVtbl -> put_BackColor(This,clr) ) 

#define IVwGraphics_DrawRectangle(This,xLeft,yTop,xRight,yBottom)	\
    ( (This)->lpVtbl -> DrawRectangle(This,xLeft,yTop,xRight,yBottom) ) 

#define IVwGraphics_DrawHorzLine(This,xLeft,xRight,y,dyHeight,cdx,prgdx,pdxStart)	\
    ( (This)->lpVtbl -> DrawHorzLine(This,xLeft,xRight,y,dyHeight,cdx,prgdx,pdxStart) ) 

#define IVwGraphics_DrawLine(This,xLeft,yTop,xRight,yBottom)	\
    ( (This)->lpVtbl -> DrawLine(This,xLeft,yTop,xRight,yBottom) ) 

#define IVwGraphics_DrawText(This,x,y,cch,prgch,xStretch)	\
    ( (This)->lpVtbl -> DrawText(This,x,y,cch,prgch,xStretch) ) 

#define IVwGraphics_DrawGlyphs(This,x,y,cgi,prggi)	\
    ( (This)->lpVtbl -> DrawGlyphs(This,x,y,cgi,prggi) ) 

#define IVwGraphics_GetTextExtent(This,cch,prgch,px,py)	\
    ( (This)->lpVtbl -> GetTextExtent(This,cch,prgch,px,py) ) 

#define IVwGraphics_GetTextLeadWidth(This,cch,prgch,ich,xStretch,px)	\
    ( (This)->lpVtbl -> GetTextLeadWidth(This,cch,prgch,ich,xStretch,px) ) 

#define IVwGraphics_GetClipRect(This,pxLeft,pyTop,pxRight,pyBottom)	\
    ( (This)->lpVtbl -> GetClipRect(This,pxLeft,pyTop,pxRight,pyBottom) ) 

#define IVwGraphics_GetFontEmSquare(This,pxyFontEmSquare)	\
    ( (This)->lpVtbl -> GetFontEmSquare(This,pxyFontEmSquare) ) 

#define IVwGraphics_GetGlyphMetrics(This,chw,psBoundingWidth,pyBoundingHeight,pxBoundingX,pyBoundingY,pxAdvanceX,pyAdvanceY)	\
    ( (This)->lpVtbl -> GetGlyphMetrics(This,chw,psBoundingWidth,pyBoundingHeight,pxBoundingX,pyBoundingY,pxAdvanceX,pyAdvanceY) ) 

#define IVwGraphics_GetFontData(This,nTableId,pcbTableSz,prgb)	\
    ( (This)->lpVtbl -> GetFontData(This,nTableId,pcbTableSz,prgb) ) 

#define IVwGraphics_XYFromGlyphPoint(This,chw,nPoint,pxRet,pyRet)	\
    ( (This)->lpVtbl -> XYFromGlyphPoint(This,chw,nPoint,pxRet,pyRet) ) 

#define IVwGraphics_get_FontAscent(This,py)	\
    ( (This)->lpVtbl -> get_FontAscent(This,py) ) 

#define IVwGraphics_get_FontDescent(This,pyRet)	\
    ( (This)->lpVtbl -> get_FontDescent(This,pyRet) ) 

#define IVwGraphics_get_FontCharProperties(This,pchrp)	\
    ( (This)->lpVtbl -> get_FontCharProperties(This,pchrp) ) 

#define IVwGraphics_ReleaseDC(This)	\
    ( (This)->lpVtbl -> ReleaseDC(This) ) 

#define IVwGraphics_get_XUnitsPerInch(This,pxInch)	\
    ( (This)->lpVtbl -> get_XUnitsPerInch(This,pxInch) ) 

#define IVwGraphics_put_XUnitsPerInch(This,xInch)	\
    ( (This)->lpVtbl -> put_XUnitsPerInch(This,xInch) ) 

#define IVwGraphics_get_YUnitsPerInch(This,pyInch)	\
    ( (This)->lpVtbl -> get_YUnitsPerInch(This,pyInch) ) 

#define IVwGraphics_put_YUnitsPerInch(This,yInch)	\
    ( (This)->lpVtbl -> put_YUnitsPerInch(This,yInch) ) 

#define IVwGraphics_GetSuperscriptHeightRatio(This,piNumerator,piDenominator)	\
    ( (This)->lpVtbl -> GetSuperscriptHeightRatio(This,piNumerator,piDenominator) ) 

#define IVwGraphics_GetSuperscriptYOffsetRatio(This,piNumerator,piDenominator)	\
    ( (This)->lpVtbl -> GetSuperscriptYOffsetRatio(This,piNumerator,piDenominator) ) 

#define IVwGraphics_GetSubscriptHeightRatio(This,piNumerator,piDenominator)	\
    ( (This)->lpVtbl -> GetSubscriptHeightRatio(This,piNumerator,piDenominator) ) 

#define IVwGraphics_GetSubscriptYOffsetRatio(This,piNumerator,piDenominator)	\
    ( (This)->lpVtbl -> GetSubscriptYOffsetRatio(This,piNumerator,piDenominator) ) 

#define IVwGraphics_SetupGraphics(This,pchrp)	\
    ( (This)->lpVtbl -> SetupGraphics(This,pchrp) ) 

#define IVwGraphics_PushClipRect(This,rcClip)	\
    ( (This)->lpVtbl -> PushClipRect(This,rcClip) ) 

#define IVwGraphics_PopClipRect(This)	\
    ( (This)->lpVtbl -> PopClipRect(This) ) 

#define IVwGraphics_DrawPolygon(This,cvpnt,prgvpnt)	\
    ( (This)->lpVtbl -> DrawPolygon(This,cvpnt,prgvpnt) ) 

#define IVwGraphics_RenderPicture(This,ppic,x,y,cx,cy,xSrc,ySrc,cxSrc,cySrc,prcWBounds)	\
    ( (This)->lpVtbl -> RenderPicture(This,ppic,x,y,cx,cy,xSrc,ySrc,cxSrc,cySrc,prcWBounds) ) 

#define IVwGraphics_MakePicture(This,pbData,cbData,pppic)	\
    ( (This)->lpVtbl -> MakePicture(This,pbData,cbData,pppic) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IVwGraphics_INTERFACE_DEFINED__ */


#ifndef __IJustifyingRenderer_INTERFACE_DEFINED__
#define __IJustifyingRenderer_INTERFACE_DEFINED__

/* interface IJustifyingRenderer */
/* [unique][object][uuid] */ 


#define IID_IJustifyingRenderer __uuidof(IJustifyingRenderer)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("1141174B-923F-4C43-BA43-8A326B76A3F2")
    IJustifyingRenderer : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetGlyphAttributeFloat( 
            /* [in] */ int iGlyph,
            /* [in] */ int kjgatId,
            /* [in] */ int nLevel,
            /* [out] */ float *pValueRet) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetGlyphAttributeInt( 
            /* [in] */ int iGlyph,
            /* [in] */ int kjgatId,
            /* [in] */ int nLevel,
            /* [out] */ int *pValueRet) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetGlyphAttributeFloat( 
            /* [in] */ int iGlyph,
            /* [in] */ int kjgatId,
            /* [in] */ int nLevel,
            /* [in] */ float value) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetGlyphAttributeInt( 
            /* [in] */ int iGlyph,
            /* [in] */ int kjgatId,
            /* [in] */ int nLevel,
            /* [in] */ int value) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IJustifyingRendererVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IJustifyingRenderer * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IJustifyingRenderer * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IJustifyingRenderer * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetGlyphAttributeFloat )( 
            IJustifyingRenderer * This,
            /* [in] */ int iGlyph,
            /* [in] */ int kjgatId,
            /* [in] */ int nLevel,
            /* [out] */ float *pValueRet);
        
        HRESULT ( STDMETHODCALLTYPE *GetGlyphAttributeInt )( 
            IJustifyingRenderer * This,
            /* [in] */ int iGlyph,
            /* [in] */ int kjgatId,
            /* [in] */ int nLevel,
            /* [out] */ int *pValueRet);
        
        HRESULT ( STDMETHODCALLTYPE *SetGlyphAttributeFloat )( 
            IJustifyingRenderer * This,
            /* [in] */ int iGlyph,
            /* [in] */ int kjgatId,
            /* [in] */ int nLevel,
            /* [in] */ float value);
        
        HRESULT ( STDMETHODCALLTYPE *SetGlyphAttributeInt )( 
            IJustifyingRenderer * This,
            /* [in] */ int iGlyph,
            /* [in] */ int kjgatId,
            /* [in] */ int nLevel,
            /* [in] */ int value);
        
        END_INTERFACE
    } IJustifyingRendererVtbl;

    interface IJustifyingRenderer
    {
        CONST_VTBL struct IJustifyingRendererVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IJustifyingRenderer_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IJustifyingRenderer_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IJustifyingRenderer_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IJustifyingRenderer_GetGlyphAttributeFloat(This,iGlyph,kjgatId,nLevel,pValueRet)	\
    ( (This)->lpVtbl -> GetGlyphAttributeFloat(This,iGlyph,kjgatId,nLevel,pValueRet) ) 

#define IJustifyingRenderer_GetGlyphAttributeInt(This,iGlyph,kjgatId,nLevel,pValueRet)	\
    ( (This)->lpVtbl -> GetGlyphAttributeInt(This,iGlyph,kjgatId,nLevel,pValueRet) ) 

#define IJustifyingRenderer_SetGlyphAttributeFloat(This,iGlyph,kjgatId,nLevel,value)	\
    ( (This)->lpVtbl -> SetGlyphAttributeFloat(This,iGlyph,kjgatId,nLevel,value) ) 

#define IJustifyingRenderer_SetGlyphAttributeInt(This,iGlyph,kjgatId,nLevel,value)	\
    ( (This)->lpVtbl -> SetGlyphAttributeInt(This,iGlyph,kjgatId,nLevel,value) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IJustifyingRenderer_INTERFACE_DEFINED__ */


#ifndef __IRenderEngineFactory_INTERFACE_DEFINED__
#define __IRenderEngineFactory_INTERFACE_DEFINED__

/* interface IRenderEngineFactory */
/* [unique][object][uuid] */ 


#define IID_IRenderEngineFactory __uuidof(IRenderEngineFactory)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("97D3D3E7-042B-4790-AB70-8D6C3A677576")
    IRenderEngineFactory : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Renderer( 
            /* [in] */ /* external definition not present */ ILgWritingSystem *ws,
            /* [in] */ IVwGraphics *pvg,
            /* [retval][out] */ IRenderEngine **ppreneng) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IRenderEngineFactoryVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IRenderEngineFactory * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IRenderEngineFactory * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IRenderEngineFactory * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Renderer )( 
            IRenderEngineFactory * This,
            /* [in] */ /* external definition not present */ ILgWritingSystem *ws,
            /* [in] */ IVwGraphics *pvg,
            /* [retval][out] */ IRenderEngine **ppreneng);
        
        END_INTERFACE
    } IRenderEngineFactoryVtbl;

    interface IRenderEngineFactory
    {
        CONST_VTBL struct IRenderEngineFactoryVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IRenderEngineFactory_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IRenderEngineFactory_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IRenderEngineFactory_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IRenderEngineFactory_get_Renderer(This,ws,pvg,ppreneng)	\
    ( (This)->lpVtbl -> get_Renderer(This,ws,pvg,ppreneng) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IRenderEngineFactory_INTERFACE_DEFINED__ */


#ifndef __ILgLineBreaker_INTERFACE_DEFINED__
#define __ILgLineBreaker_INTERFACE_DEFINED__

/* interface ILgLineBreaker */
/* [unique][object][uuid] */ 


#define IID_ILgLineBreaker __uuidof(ILgLineBreaker)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("F8D5FDE9-9695-4D63-8843-E27FD880BFF0")
    ILgLineBreaker : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Initialize( 
            /* [in] */ BSTR bstrLocale) = 0;
        
        virtual /* [restricted] */ HRESULT STDMETHODCALLTYPE GetLineBreakProps( 
            /* [size_is][in] */ const OLECHAR *prgchIn,
            /* [in] */ int cchIn,
            /* [size_is][out] */ byte *prglbOut) = 0;
        
        virtual /* [restricted] */ HRESULT STDMETHODCALLTYPE GetLineBreakInfo( 
            /* [size_is][in] */ const OLECHAR *prgchIn,
            /* [in] */ int cchIn,
            /* [in] */ int ichMin,
            /* [in] */ int ichLim,
            /* [size_is][out] */ byte *prglbsOut,
            /* [out] */ int *pichBreak) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetLineBreakText( 
            /* [in] */ int cchMax,
            /* [out] */ OLECHAR *prgchOut,
            /* [out] */ int *pcchOut) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_LineBreakText( 
            /* [size_is][in] */ OLECHAR *prgchIn,
            /* [in] */ int cchMax) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE LineBreakBefore( 
            /* [in] */ int ichIn,
            /* [out] */ int *pichOut,
            /* [out] */ LgLineBreak *plbWeight) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE LineBreakAfter( 
            /* [in] */ int ichIn,
            /* [out] */ int *pichOut,
            /* [out] */ LgLineBreak *plbWeight) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ILgLineBreakerVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ILgLineBreaker * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ILgLineBreaker * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ILgLineBreaker * This);
        
        HRESULT ( STDMETHODCALLTYPE *Initialize )( 
            ILgLineBreaker * This,
            /* [in] */ BSTR bstrLocale);
        
        /* [restricted] */ HRESULT ( STDMETHODCALLTYPE *GetLineBreakProps )( 
            ILgLineBreaker * This,
            /* [size_is][in] */ const OLECHAR *prgchIn,
            /* [in] */ int cchIn,
            /* [size_is][out] */ byte *prglbOut);
        
        /* [restricted] */ HRESULT ( STDMETHODCALLTYPE *GetLineBreakInfo )( 
            ILgLineBreaker * This,
            /* [size_is][in] */ const OLECHAR *prgchIn,
            /* [in] */ int cchIn,
            /* [in] */ int ichMin,
            /* [in] */ int ichLim,
            /* [size_is][out] */ byte *prglbsOut,
            /* [out] */ int *pichBreak);
        
        HRESULT ( STDMETHODCALLTYPE *GetLineBreakText )( 
            ILgLineBreaker * This,
            /* [in] */ int cchMax,
            /* [out] */ OLECHAR *prgchOut,
            /* [out] */ int *pcchOut);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_LineBreakText )( 
            ILgLineBreaker * This,
            /* [size_is][in] */ OLECHAR *prgchIn,
            /* [in] */ int cchMax);
        
        HRESULT ( STDMETHODCALLTYPE *LineBreakBefore )( 
            ILgLineBreaker * This,
            /* [in] */ int ichIn,
            /* [out] */ int *pichOut,
            /* [out] */ LgLineBreak *plbWeight);
        
        HRESULT ( STDMETHODCALLTYPE *LineBreakAfter )( 
            ILgLineBreaker * This,
            /* [in] */ int ichIn,
            /* [out] */ int *pichOut,
            /* [out] */ LgLineBreak *plbWeight);
        
        END_INTERFACE
    } ILgLineBreakerVtbl;

    interface ILgLineBreaker
    {
        CONST_VTBL struct ILgLineBreakerVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ILgLineBreaker_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ILgLineBreaker_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ILgLineBreaker_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ILgLineBreaker_Initialize(This,bstrLocale)	\
    ( (This)->lpVtbl -> Initialize(This,bstrLocale) ) 

#define ILgLineBreaker_GetLineBreakProps(This,prgchIn,cchIn,prglbOut)	\
    ( (This)->lpVtbl -> GetLineBreakProps(This,prgchIn,cchIn,prglbOut) ) 

#define ILgLineBreaker_GetLineBreakInfo(This,prgchIn,cchIn,ichMin,ichLim,prglbsOut,pichBreak)	\
    ( (This)->lpVtbl -> GetLineBreakInfo(This,prgchIn,cchIn,ichMin,ichLim,prglbsOut,pichBreak) ) 

#define ILgLineBreaker_GetLineBreakText(This,cchMax,prgchOut,pcchOut)	\
    ( (This)->lpVtbl -> GetLineBreakText(This,cchMax,prgchOut,pcchOut) ) 

#define ILgLineBreaker_put_LineBreakText(This,prgchIn,cchMax)	\
    ( (This)->lpVtbl -> put_LineBreakText(This,prgchIn,cchMax) ) 

#define ILgLineBreaker_LineBreakBefore(This,ichIn,pichOut,plbWeight)	\
    ( (This)->lpVtbl -> LineBreakBefore(This,ichIn,pichOut,plbWeight) ) 

#define ILgLineBreaker_LineBreakAfter(This,ichIn,pichOut,plbWeight)	\
    ( (This)->lpVtbl -> LineBreakAfter(This,ichIn,pichOut,plbWeight) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ILgLineBreaker_INTERFACE_DEFINED__ */


#ifndef __IVwGraphicsWin32_INTERFACE_DEFINED__
#define __IVwGraphicsWin32_INTERFACE_DEFINED__

/* interface IVwGraphicsWin32 */
/* [unique][object][uuid] */ 


#define IID_IVwGraphicsWin32 __uuidof(IVwGraphicsWin32)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("C955E295-A259-47D4-8158-4C7A3539D35E")
    IVwGraphicsWin32 : public IVwGraphics
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Initialize( 
            /* [in] */ HDC hdc) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetDeviceContext( 
            /* [retval][out] */ HDC *phdc) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetMeasureDc( 
            /* [in] */ HDC hdc) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetClipRect( 
            /* [in] */ RECT *prcClip) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetTextStyleContext( 
            /* [retval][out] */ HDC *ppContext) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IVwGraphicsWin32Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IVwGraphicsWin32 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IVwGraphicsWin32 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IVwGraphicsWin32 * This);
        
        HRESULT ( STDMETHODCALLTYPE *InvertRect )( 
            IVwGraphicsWin32 * This,
            /* [in] */ int xLeft,
            /* [in] */ int yTop,
            /* [in] */ int xRight,
            /* [in] */ int yBottom);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_ForeColor )( 
            IVwGraphicsWin32 * This,
            /* [in] */ int clr);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_BackColor )( 
            IVwGraphicsWin32 * This,
            /* [in] */ int clr);
        
        HRESULT ( STDMETHODCALLTYPE *DrawRectangle )( 
            IVwGraphicsWin32 * This,
            /* [in] */ int xLeft,
            /* [in] */ int yTop,
            /* [in] */ int xRight,
            /* [in] */ int yBottom);
        
        HRESULT ( STDMETHODCALLTYPE *DrawHorzLine )( 
            IVwGraphicsWin32 * This,
            /* [in] */ int xLeft,
            /* [in] */ int xRight,
            /* [in] */ int y,
            /* [in] */ int dyHeight,
            /* [in] */ int cdx,
            /* [size_is][in] */ int *prgdx,
            /* [out][in] */ int *pdxStart);
        
        HRESULT ( STDMETHODCALLTYPE *DrawLine )( 
            IVwGraphicsWin32 * This,
            /* [in] */ int xLeft,
            /* [in] */ int yTop,
            /* [in] */ int xRight,
            /* [in] */ int yBottom);
        
        HRESULT ( STDMETHODCALLTYPE *DrawText )( 
            IVwGraphicsWin32 * This,
            /* [in] */ int x,
            /* [in] */ int y,
            /* [in] */ int cch,
            /* [size_is][in] */ const OLECHAR *prgch,
            /* [in] */ int xStretch);
        
        HRESULT ( STDMETHODCALLTYPE *DrawGlyphs )( 
            IVwGraphicsWin32 * This,
            /* [in] */ int x,
            /* [in] */ int y,
            /* [in] */ int cgi,
            /* [size_is][in] */ const GlyphInfo *prggi);
        
        HRESULT ( STDMETHODCALLTYPE *GetTextExtent )( 
            IVwGraphicsWin32 * This,
            /* [in] */ int cch,
            /* [size_is][in] */ const OLECHAR *prgch,
            /* [out] */ int *px,
            /* [out] */ int *py);
        
        HRESULT ( STDMETHODCALLTYPE *GetTextLeadWidth )( 
            IVwGraphicsWin32 * This,
            /* [in] */ int cch,
            /* [size_is][in] */ const OLECHAR *prgch,
            /* [in] */ int ich,
            /* [in] */ int xStretch,
            /* [retval][out] */ int *px);
        
        HRESULT ( STDMETHODCALLTYPE *GetClipRect )( 
            IVwGraphicsWin32 * This,
            /* [out] */ int *pxLeft,
            /* [out] */ int *pyTop,
            /* [out] */ int *pxRight,
            /* [out] */ int *pyBottom);
        
        HRESULT ( STDMETHODCALLTYPE *GetFontEmSquare )( 
            IVwGraphicsWin32 * This,
            /* [retval][out] */ int *pxyFontEmSquare);
        
        HRESULT ( STDMETHODCALLTYPE *GetGlyphMetrics )( 
            IVwGraphicsWin32 * This,
            /* [in] */ int chw,
            /* [out] */ int *psBoundingWidth,
            /* [out] */ int *pyBoundingHeight,
            /* [out] */ int *pxBoundingX,
            /* [out] */ int *pyBoundingY,
            /* [out] */ int *pxAdvanceX,
            /* [out] */ int *pyAdvanceY);
        
        HRESULT ( STDMETHODCALLTYPE *GetFontData )( 
            IVwGraphicsWin32 * This,
            /* [in] */ int nTableId,
            /* [out][in] */ int *pcbTableSz,
            /* [size_is][out] */ BYTE *prgb);
        
        HRESULT ( STDMETHODCALLTYPE *XYFromGlyphPoint )( 
            IVwGraphicsWin32 * This,
            /* [in] */ int chw,
            /* [in] */ int nPoint,
            /* [out] */ int *pxRet,
            /* [out] */ int *pyRet);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_FontAscent )( 
            IVwGraphicsWin32 * This,
            /* [retval][out] */ int *py);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_FontDescent )( 
            IVwGraphicsWin32 * This,
            /* [retval][out] */ int *pyRet);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_FontCharProperties )( 
            IVwGraphicsWin32 * This,
            /* [retval][out] */ /* external definition not present */ LgCharRenderProps *pchrp);
        
        HRESULT ( STDMETHODCALLTYPE *ReleaseDC )( 
            IVwGraphicsWin32 * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_XUnitsPerInch )( 
            IVwGraphicsWin32 * This,
            /* [retval][out] */ int *pxInch);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_XUnitsPerInch )( 
            IVwGraphicsWin32 * This,
            /* [in] */ int xInch);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_YUnitsPerInch )( 
            IVwGraphicsWin32 * This,
            /* [retval][out] */ int *pyInch);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_YUnitsPerInch )( 
            IVwGraphicsWin32 * This,
            /* [in] */ int yInch);
        
        HRESULT ( STDMETHODCALLTYPE *GetSuperscriptHeightRatio )( 
            IVwGraphicsWin32 * This,
            /* [out] */ int *piNumerator,
            /* [out] */ int *piDenominator);
        
        HRESULT ( STDMETHODCALLTYPE *GetSuperscriptYOffsetRatio )( 
            IVwGraphicsWin32 * This,
            /* [out] */ int *piNumerator,
            /* [out] */ int *piDenominator);
        
        HRESULT ( STDMETHODCALLTYPE *GetSubscriptHeightRatio )( 
            IVwGraphicsWin32 * This,
            /* [out] */ int *piNumerator,
            /* [out] */ int *piDenominator);
        
        HRESULT ( STDMETHODCALLTYPE *GetSubscriptYOffsetRatio )( 
            IVwGraphicsWin32 * This,
            /* [out] */ int *piNumerator,
            /* [out] */ int *piDenominator);
        
        HRESULT ( STDMETHODCALLTYPE *SetupGraphics )( 
            IVwGraphicsWin32 * This,
            /* [out][in] */ /* external definition not present */ LgCharRenderProps *pchrp);
        
        HRESULT ( STDMETHODCALLTYPE *PushClipRect )( 
            IVwGraphicsWin32 * This,
            /* [in] */ RECT rcClip);
        
        HRESULT ( STDMETHODCALLTYPE *PopClipRect )( 
            IVwGraphicsWin32 * This);
        
        HRESULT ( STDMETHODCALLTYPE *DrawPolygon )( 
            IVwGraphicsWin32 * This,
            /* [in] */ int cvpnt,
            /* [size_is][in] */ POINT prgvpnt[  ]);
        
        HRESULT ( STDMETHODCALLTYPE *RenderPicture )( 
            IVwGraphicsWin32 * This,
            /* [in] */ IPicture *ppic,
            /* [in] */ int x,
            /* [in] */ int y,
            /* [in] */ int cx,
            /* [in] */ int cy,
            /* [in] */ OLE_XPOS_HIMETRIC xSrc,
            /* [in] */ OLE_YPOS_HIMETRIC ySrc,
            /* [in] */ OLE_XSIZE_HIMETRIC cxSrc,
            /* [in] */ OLE_YSIZE_HIMETRIC cySrc,
            /* [in] */ LPCRECT prcWBounds);
        
        HRESULT ( STDMETHODCALLTYPE *MakePicture )( 
            IVwGraphicsWin32 * This,
            /* [size_is][in] */ byte *pbData,
            /* [in] */ int cbData,
            /* [retval][out] */ IPicture **pppic);
        
        HRESULT ( STDMETHODCALLTYPE *Initialize )( 
            IVwGraphicsWin32 * This,
            /* [in] */ HDC hdc);
        
        HRESULT ( STDMETHODCALLTYPE *GetDeviceContext )( 
            IVwGraphicsWin32 * This,
            /* [retval][out] */ HDC *phdc);
        
        HRESULT ( STDMETHODCALLTYPE *SetMeasureDc )( 
            IVwGraphicsWin32 * This,
            /* [in] */ HDC hdc);
        
        HRESULT ( STDMETHODCALLTYPE *SetClipRect )( 
            IVwGraphicsWin32 * This,
            /* [in] */ RECT *prcClip);
        
        HRESULT ( STDMETHODCALLTYPE *GetTextStyleContext )( 
            IVwGraphicsWin32 * This,
            /* [retval][out] */ HDC *ppContext);
        
        END_INTERFACE
    } IVwGraphicsWin32Vtbl;

    interface IVwGraphicsWin32
    {
        CONST_VTBL struct IVwGraphicsWin32Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IVwGraphicsWin32_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IVwGraphicsWin32_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IVwGraphicsWin32_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IVwGraphicsWin32_InvertRect(This,xLeft,yTop,xRight,yBottom)	\
    ( (This)->lpVtbl -> InvertRect(This,xLeft,yTop,xRight,yBottom) ) 

#define IVwGraphicsWin32_put_ForeColor(This,clr)	\
    ( (This)->lpVtbl -> put_ForeColor(This,clr) ) 

#define IVwGraphicsWin32_put_BackColor(This,clr)	\
    ( (This)->lpVtbl -> put_BackColor(This,clr) ) 

#define IVwGraphicsWin32_DrawRectangle(This,xLeft,yTop,xRight,yBottom)	\
    ( (This)->lpVtbl -> DrawRectangle(This,xLeft,yTop,xRight,yBottom) ) 

#define IVwGraphicsWin32_DrawHorzLine(This,xLeft,xRight,y,dyHeight,cdx,prgdx,pdxStart)	\
    ( (This)->lpVtbl -> DrawHorzLine(This,xLeft,xRight,y,dyHeight,cdx,prgdx,pdxStart) ) 

#define IVwGraphicsWin32_DrawLine(This,xLeft,yTop,xRight,yBottom)	\
    ( (This)->lpVtbl -> DrawLine(This,xLeft,yTop,xRight,yBottom) ) 

#define IVwGraphicsWin32_DrawText(This,x,y,cch,prgch,xStretch)	\
    ( (This)->lpVtbl -> DrawText(This,x,y,cch,prgch,xStretch) ) 

#define IVwGraphicsWin32_DrawGlyphs(This,x,y,cgi,prggi)	\
    ( (This)->lpVtbl -> DrawGlyphs(This,x,y,cgi,prggi) ) 

#define IVwGraphicsWin32_GetTextExtent(This,cch,prgch,px,py)	\
    ( (This)->lpVtbl -> GetTextExtent(This,cch,prgch,px,py) ) 

#define IVwGraphicsWin32_GetTextLeadWidth(This,cch,prgch,ich,xStretch,px)	\
    ( (This)->lpVtbl -> GetTextLeadWidth(This,cch,prgch,ich,xStretch,px) ) 

#define IVwGraphicsWin32_GetClipRect(This,pxLeft,pyTop,pxRight,pyBottom)	\
    ( (This)->lpVtbl -> GetClipRect(This,pxLeft,pyTop,pxRight,pyBottom) ) 

#define IVwGraphicsWin32_GetFontEmSquare(This,pxyFontEmSquare)	\
    ( (This)->lpVtbl -> GetFontEmSquare(This,pxyFontEmSquare) ) 

#define IVwGraphicsWin32_GetGlyphMetrics(This,chw,psBoundingWidth,pyBoundingHeight,pxBoundingX,pyBoundingY,pxAdvanceX,pyAdvanceY)	\
    ( (This)->lpVtbl -> GetGlyphMetrics(This,chw,psBoundingWidth,pyBoundingHeight,pxBoundingX,pyBoundingY,pxAdvanceX,pyAdvanceY) ) 

#define IVwGraphicsWin32_GetFontData(This,nTableId,pcbTableSz,prgb)	\
    ( (This)->lpVtbl -> GetFontData(This,nTableId,pcbTableSz,prgb) ) 

#define IVwGraphicsWin32_XYFromGlyphPoint(This,chw,nPoint,pxRet,pyRet)	\
    ( (This)->lpVtbl -> XYFromGlyphPoint(This,chw,nPoint,pxRet,pyRet) ) 

#define IVwGraphicsWin32_get_FontAscent(This,py)	\
    ( (This)->lpVtbl -> get_FontAscent(This,py) ) 

#define IVwGraphicsWin32_get_FontDescent(This,pyRet)	\
    ( (This)->lpVtbl -> get_FontDescent(This,pyRet) ) 

#define IVwGraphicsWin32_get_FontCharProperties(This,pchrp)	\
    ( (This)->lpVtbl -> get_FontCharProperties(This,pchrp) ) 

#define IVwGraphicsWin32_ReleaseDC(This)	\
    ( (This)->lpVtbl -> ReleaseDC(This) ) 

#define IVwGraphicsWin32_get_XUnitsPerInch(This,pxInch)	\
    ( (This)->lpVtbl -> get_XUnitsPerInch(This,pxInch) ) 

#define IVwGraphicsWin32_put_XUnitsPerInch(This,xInch)	\
    ( (This)->lpVtbl -> put_XUnitsPerInch(This,xInch) ) 

#define IVwGraphicsWin32_get_YUnitsPerInch(This,pyInch)	\
    ( (This)->lpVtbl -> get_YUnitsPerInch(This,pyInch) ) 

#define IVwGraphicsWin32_put_YUnitsPerInch(This,yInch)	\
    ( (This)->lpVtbl -> put_YUnitsPerInch(This,yInch) ) 

#define IVwGraphicsWin32_GetSuperscriptHeightRatio(This,piNumerator,piDenominator)	\
    ( (This)->lpVtbl -> GetSuperscriptHeightRatio(This,piNumerator,piDenominator) ) 

#define IVwGraphicsWin32_GetSuperscriptYOffsetRatio(This,piNumerator,piDenominator)	\
    ( (This)->lpVtbl -> GetSuperscriptYOffsetRatio(This,piNumerator,piDenominator) ) 

#define IVwGraphicsWin32_GetSubscriptHeightRatio(This,piNumerator,piDenominator)	\
    ( (This)->lpVtbl -> GetSubscriptHeightRatio(This,piNumerator,piDenominator) ) 

#define IVwGraphicsWin32_GetSubscriptYOffsetRatio(This,piNumerator,piDenominator)	\
    ( (This)->lpVtbl -> GetSubscriptYOffsetRatio(This,piNumerator,piDenominator) ) 

#define IVwGraphicsWin32_SetupGraphics(This,pchrp)	\
    ( (This)->lpVtbl -> SetupGraphics(This,pchrp) ) 

#define IVwGraphicsWin32_PushClipRect(This,rcClip)	\
    ( (This)->lpVtbl -> PushClipRect(This,rcClip) ) 

#define IVwGraphicsWin32_PopClipRect(This)	\
    ( (This)->lpVtbl -> PopClipRect(This) ) 

#define IVwGraphicsWin32_DrawPolygon(This,cvpnt,prgvpnt)	\
    ( (This)->lpVtbl -> DrawPolygon(This,cvpnt,prgvpnt) ) 

#define IVwGraphicsWin32_RenderPicture(This,ppic,x,y,cx,cy,xSrc,ySrc,cxSrc,cySrc,prcWBounds)	\
    ( (This)->lpVtbl -> RenderPicture(This,ppic,x,y,cx,cy,xSrc,ySrc,cxSrc,cySrc,prcWBounds) ) 

#define IVwGraphicsWin32_MakePicture(This,pbData,cbData,pppic)	\
    ( (This)->lpVtbl -> MakePicture(This,pbData,cbData,pppic) ) 


#define IVwGraphicsWin32_Initialize(This,hdc)	\
    ( (This)->lpVtbl -> Initialize(This,hdc) ) 

#define IVwGraphicsWin32_GetDeviceContext(This,phdc)	\
    ( (This)->lpVtbl -> GetDeviceContext(This,phdc) ) 

#define IVwGraphicsWin32_SetMeasureDc(This,hdc)	\
    ( (This)->lpVtbl -> SetMeasureDc(This,hdc) ) 

#define IVwGraphicsWin32_SetClipRect(This,prcClip)	\
    ( (This)->lpVtbl -> SetClipRect(This,prcClip) ) 

#define IVwGraphicsWin32_GetTextStyleContext(This,ppContext)	\
    ( (This)->lpVtbl -> GetTextStyleContext(This,ppContext) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IVwGraphicsWin32_INTERFACE_DEFINED__ */


#define CLSID_VwGraphicsWin32 __uuidof(VwGraphicsWin32)

#ifdef __cplusplus

class DECLSPEC_UUID("D888DB98-83A9-4592-AAD2-F18F6F74AB87")
VwGraphicsWin32;
#endif

#ifndef __IVwTextSource_INTERFACE_DEFINED__
#define __IVwTextSource_INTERFACE_DEFINED__

/* interface IVwTextSource */
/* [unique][object][uuid] */ 


#define IID_IVwTextSource __uuidof(IVwTextSource)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("6C0465AC-17C5-4C9C-8AF3-62221F2F7707")
    IVwTextSource : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Fetch( 
            /* [in] */ int ichMin,
            /* [in] */ int ichLim,
            /* [size_is][out] */ OLECHAR *prgchBuf) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Length( 
            /* [retval][out] */ int *pcch) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE FetchSearch( 
            /* [in] */ int ichMin,
            /* [in] */ int ichLim,
            /* [size_is][out] */ OLECHAR *prgchBuf) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_LengthSearch( 
            /* [retval][out] */ int *pcch) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetCharProps( 
            /* [in] */ int ich,
            /* [out] */ /* external definition not present */ LgCharRenderProps *pchrp,
            /* [out] */ int *pichMin,
            /* [out] */ int *pichLim) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetParaProps( 
            /* [in] */ int ich,
            /* [out] */ LgParaRenderProps *pchrp,
            /* [out] */ int *pichMin,
            /* [out] */ int *pichLim) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetCharStringProp( 
            /* [in] */ int ich,
            /* [in] */ int nId,
            /* [out] */ BSTR *pbstr,
            /* [out] */ int *pichMin,
            /* [out] */ int *pichLim) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetParaStringProp( 
            /* [in] */ int ich,
            /* [in] */ int nId,
            /* [out] */ BSTR *pbstr,
            /* [out] */ int *pichMin,
            /* [out] */ int *pichLim) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetSubString( 
            /* [in] */ int ichMin,
            /* [in] */ int ichLim,
            /* [retval][out] */ /* external definition not present */ ITsString **pptss) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetWsFactory( 
            /* [retval][out] */ /* external definition not present */ ILgWritingSystemFactory **ppwsf) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE LogToSearch( 
            /* [in] */ int ichlog,
            /* [retval][out] */ int *pichSearch) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SearchToLog( 
            /* [in] */ int ichSearch,
            /* [in] */ ComBool fAssocPrev,
            /* [retval][out] */ int *pichLog) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE LogToRen( 
            /* [in] */ int ichLog,
            /* [retval][out] */ int *pichRen) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE RenToLog( 
            /* [in] */ int ichRen,
            /* [retval][out] */ int *pichLog) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SearchToRen( 
            /* [in] */ int ichSearch,
            /* [in] */ ComBool fAssocPrev,
            /* [retval][out] */ int *pichRen) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE RenToSearch( 
            /* [in] */ int ichRen,
            /* [retval][out] */ int *pichSearch) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IVwTextSourceVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IVwTextSource * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IVwTextSource * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IVwTextSource * This);
        
        HRESULT ( STDMETHODCALLTYPE *Fetch )( 
            IVwTextSource * This,
            /* [in] */ int ichMin,
            /* [in] */ int ichLim,
            /* [size_is][out] */ OLECHAR *prgchBuf);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Length )( 
            IVwTextSource * This,
            /* [retval][out] */ int *pcch);
        
        HRESULT ( STDMETHODCALLTYPE *FetchSearch )( 
            IVwTextSource * This,
            /* [in] */ int ichMin,
            /* [in] */ int ichLim,
            /* [size_is][out] */ OLECHAR *prgchBuf);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_LengthSearch )( 
            IVwTextSource * This,
            /* [retval][out] */ int *pcch);
        
        HRESULT ( STDMETHODCALLTYPE *GetCharProps )( 
            IVwTextSource * This,
            /* [in] */ int ich,
            /* [out] */ /* external definition not present */ LgCharRenderProps *pchrp,
            /* [out] */ int *pichMin,
            /* [out] */ int *pichLim);
        
        HRESULT ( STDMETHODCALLTYPE *GetParaProps )( 
            IVwTextSource * This,
            /* [in] */ int ich,
            /* [out] */ LgParaRenderProps *pchrp,
            /* [out] */ int *pichMin,
            /* [out] */ int *pichLim);
        
        HRESULT ( STDMETHODCALLTYPE *GetCharStringProp )( 
            IVwTextSource * This,
            /* [in] */ int ich,
            /* [in] */ int nId,
            /* [out] */ BSTR *pbstr,
            /* [out] */ int *pichMin,
            /* [out] */ int *pichLim);
        
        HRESULT ( STDMETHODCALLTYPE *GetParaStringProp )( 
            IVwTextSource * This,
            /* [in] */ int ich,
            /* [in] */ int nId,
            /* [out] */ BSTR *pbstr,
            /* [out] */ int *pichMin,
            /* [out] */ int *pichLim);
        
        HRESULT ( STDMETHODCALLTYPE *GetSubString )( 
            IVwTextSource * This,
            /* [in] */ int ichMin,
            /* [in] */ int ichLim,
            /* [retval][out] */ /* external definition not present */ ITsString **pptss);
        
        HRESULT ( STDMETHODCALLTYPE *GetWsFactory )( 
            IVwTextSource * This,
            /* [retval][out] */ /* external definition not present */ ILgWritingSystemFactory **ppwsf);
        
        HRESULT ( STDMETHODCALLTYPE *LogToSearch )( 
            IVwTextSource * This,
            /* [in] */ int ichlog,
            /* [retval][out] */ int *pichSearch);
        
        HRESULT ( STDMETHODCALLTYPE *SearchToLog )( 
            IVwTextSource * This,
            /* [in] */ int ichSearch,
            /* [in] */ ComBool fAssocPrev,
            /* [retval][out] */ int *pichLog);
        
        HRESULT ( STDMETHODCALLTYPE *LogToRen )( 
            IVwTextSource * This,
            /* [in] */ int ichLog,
            /* [retval][out] */ int *pichRen);
        
        HRESULT ( STDMETHODCALLTYPE *RenToLog )( 
            IVwTextSource * This,
            /* [in] */ int ichRen,
            /* [retval][out] */ int *pichLog);
        
        HRESULT ( STDMETHODCALLTYPE *SearchToRen )( 
            IVwTextSource * This,
            /* [in] */ int ichSearch,
            /* [in] */ ComBool fAssocPrev,
            /* [retval][out] */ int *pichRen);
        
        HRESULT ( STDMETHODCALLTYPE *RenToSearch )( 
            IVwTextSource * This,
            /* [in] */ int ichRen,
            /* [retval][out] */ int *pichSearch);
        
        END_INTERFACE
    } IVwTextSourceVtbl;

    interface IVwTextSource
    {
        CONST_VTBL struct IVwTextSourceVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IVwTextSource_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IVwTextSource_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IVwTextSource_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IVwTextSource_Fetch(This,ichMin,ichLim,prgchBuf)	\
    ( (This)->lpVtbl -> Fetch(This,ichMin,ichLim,prgchBuf) ) 

#define IVwTextSource_get_Length(This,pcch)	\
    ( (This)->lpVtbl -> get_Length(This,pcch) ) 

#define IVwTextSource_FetchSearch(This,ichMin,ichLim,prgchBuf)	\
    ( (This)->lpVtbl -> FetchSearch(This,ichMin,ichLim,prgchBuf) ) 

#define IVwTextSource_get_LengthSearch(This,pcch)	\
    ( (This)->lpVtbl -> get_LengthSearch(This,pcch) ) 

#define IVwTextSource_GetCharProps(This,ich,pchrp,pichMin,pichLim)	\
    ( (This)->lpVtbl -> GetCharProps(This,ich,pchrp,pichMin,pichLim) ) 

#define IVwTextSource_GetParaProps(This,ich,pchrp,pichMin,pichLim)	\
    ( (This)->lpVtbl -> GetParaProps(This,ich,pchrp,pichMin,pichLim) ) 

#define IVwTextSource_GetCharStringProp(This,ich,nId,pbstr,pichMin,pichLim)	\
    ( (This)->lpVtbl -> GetCharStringProp(This,ich,nId,pbstr,pichMin,pichLim) ) 

#define IVwTextSource_GetParaStringProp(This,ich,nId,pbstr,pichMin,pichLim)	\
    ( (This)->lpVtbl -> GetParaStringProp(This,ich,nId,pbstr,pichMin,pichLim) ) 

#define IVwTextSource_GetSubString(This,ichMin,ichLim,pptss)	\
    ( (This)->lpVtbl -> GetSubString(This,ichMin,ichLim,pptss) ) 

#define IVwTextSource_GetWsFactory(This,ppwsf)	\
    ( (This)->lpVtbl -> GetWsFactory(This,ppwsf) ) 

#define IVwTextSource_LogToSearch(This,ichlog,pichSearch)	\
    ( (This)->lpVtbl -> LogToSearch(This,ichlog,pichSearch) ) 

#define IVwTextSource_SearchToLog(This,ichSearch,fAssocPrev,pichLog)	\
    ( (This)->lpVtbl -> SearchToLog(This,ichSearch,fAssocPrev,pichLog) ) 

#define IVwTextSource_LogToRen(This,ichLog,pichRen)	\
    ( (This)->lpVtbl -> LogToRen(This,ichLog,pichRen) ) 

#define IVwTextSource_RenToLog(This,ichRen,pichLog)	\
    ( (This)->lpVtbl -> RenToLog(This,ichRen,pichLog) ) 

#define IVwTextSource_SearchToRen(This,ichSearch,fAssocPrev,pichRen)	\
    ( (This)->lpVtbl -> SearchToRen(This,ichSearch,fAssocPrev,pichRen) ) 

#define IVwTextSource_RenToSearch(This,ichRen,pichSearch)	\
    ( (This)->lpVtbl -> RenToSearch(This,ichRen,pichSearch) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IVwTextSource_INTERFACE_DEFINED__ */


#ifndef __IVwJustifier_INTERFACE_DEFINED__
#define __IVwJustifier_INTERFACE_DEFINED__

/* interface IVwJustifier */
/* [unique][object][uuid] */ 


#define IID_IVwJustifier __uuidof(IVwJustifier)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("22D5E030-5239-4924-BF1B-6B4F2CBBABA5")
    IVwJustifier : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE AdjustGlyphWidths( 
            /* [in] */ IJustifyingRenderer *pjren,
            /* [in] */ int iGlyphMin,
            /* [in] */ int iGlyphLim,
            /* [in] */ float dxCurrentWidth,
            /* [in] */ float dxDesiredWidth) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IVwJustifierVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IVwJustifier * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IVwJustifier * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IVwJustifier * This);
        
        HRESULT ( STDMETHODCALLTYPE *AdjustGlyphWidths )( 
            IVwJustifier * This,
            /* [in] */ IJustifyingRenderer *pjren,
            /* [in] */ int iGlyphMin,
            /* [in] */ int iGlyphLim,
            /* [in] */ float dxCurrentWidth,
            /* [in] */ float dxDesiredWidth);
        
        END_INTERFACE
    } IVwJustifierVtbl;

    interface IVwJustifier
    {
        CONST_VTBL struct IVwJustifierVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IVwJustifier_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IVwJustifier_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IVwJustifier_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IVwJustifier_AdjustGlyphWidths(This,pjren,iGlyphMin,iGlyphLim,dxCurrentWidth,dxDesiredWidth)	\
    ( (This)->lpVtbl -> AdjustGlyphWidths(This,pjren,iGlyphMin,iGlyphLim,dxCurrentWidth,dxDesiredWidth) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IVwJustifier_INTERFACE_DEFINED__ */


#ifndef __ILgSegment_INTERFACE_DEFINED__
#define __ILgSegment_INTERFACE_DEFINED__

/* interface ILgSegment */
/* [unique][object][uuid] */ 


#define IID_ILgSegment __uuidof(ILgSegment)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("3818E245-6A0B-45A7-A5D6-52694931279E")
    ILgSegment : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE DrawText( 
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ RECT rcSrc,
            /* [in] */ RECT rcDst,
            /* [out] */ int *dxdWidth) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Recompute( 
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Width( 
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [retval][out] */ int *px) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_RightOverhang( 
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [retval][out] */ int *px) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_LeftOverhang( 
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [retval][out] */ int *px) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Height( 
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [retval][out] */ int *py) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Ascent( 
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [retval][out] */ int *py) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Extent( 
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [out] */ int *px,
            /* [out] */ int *py) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE BoundingRect( 
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ RECT rcSrc,
            /* [in] */ RECT rcDst,
            /* [retval][out] */ RECT *prcBounds) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetActualWidth( 
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ RECT rcSrc,
            /* [in] */ RECT rcDst,
            /* [out] */ int *dxdWidth) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AscentOverhang( 
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [retval][out] */ int *py) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_DescentOverhang( 
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [retval][out] */ int *py) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_RightToLeft( 
            /* [in] */ int ichBase,
            /* [retval][out] */ ComBool *pfResult) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_DirectionDepth( 
            /* [in] */ int ichBase,
            /* [out] */ int *pnDepth,
            /* [retval][out] */ ComBool *pfWeak) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetDirectionDepth( 
            /* [in] */ int ichwBase,
            /* [in] */ int nNewDepth) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_WritingSystem( 
            /* [in] */ int ichBase,
            /* [retval][out] */ int *pws) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Lim( 
            /* [in] */ int ichBase,
            /* [retval][out] */ int *pdich) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_LimInterest( 
            /* [in] */ int ichBase,
            /* [retval][out] */ int *pdich) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_EndLine( 
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ ComBool fNewVal) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_StartLine( 
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ ComBool fNewVal) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_StartBreakWeight( 
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [retval][out] */ LgLineBreak *plb) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_EndBreakWeight( 
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [retval][out] */ LgLineBreak *plb) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Stretch( 
            /* [in] */ int ichBase,
            /* [retval][out] */ int *pxs) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_Stretch( 
            /* [in] */ int ichBase,
            /* [in] */ int xs) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE IsValidInsertionPoint( 
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ int ich,
            /* [retval][out] */ LgIpValidResult *pipvr) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DoBoundariesCoincide( 
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ ComBool fBoundaryEnd,
            /* [in] */ ComBool fBoundaryRight,
            /* [retval][out] */ ComBool *pfResult) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DrawInsertionPoint( 
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ RECT rcSrc,
            /* [in] */ RECT rcDst,
            /* [in] */ int ich,
            /* [in] */ ComBool fAssocPrev,
            /* [in] */ ComBool fOn,
            /* [in] */ LgIPDrawMode dm) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE PositionsOfIP( 
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ RECT rcSrc,
            /* [in] */ RECT rcDst,
            /* [in] */ int ich,
            /* [in] */ ComBool fAssocPrev,
            /* [in] */ LgIPDrawMode dm,
            /* [out] */ RECT *rectPrimary,
            /* [out] */ RECT *rectSecondary,
            /* [out] */ ComBool *pfPrimaryHere,
            /* [out] */ ComBool *pfSecHere) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DrawRange( 
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ RECT rcSrc,
            /* [in] */ RECT rcDst,
            /* [in] */ int ichMin,
            /* [in] */ int ichLim,
            /* [in] */ int ydTop,
            /* [in] */ int ydBottom,
            /* [in] */ ComBool bOn,
            /* [in] */ ComBool fIsLastLineOfSelection,
            /* [retval][out] */ RECT *rsBounds) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE PositionOfRange( 
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ RECT rcSrc,
            /* [in] */ RECT rcDst,
            /* [in] */ int ichMin,
            /* [in] */ int ichim,
            /* [in] */ int ydTop,
            /* [in] */ int ydBottom,
            /* [in] */ ComBool fIsLastLineOfSelection,
            /* [out] */ RECT *rsBounds,
            /* [retval][out] */ ComBool *pfAnythingToDraw) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE PointToChar( 
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ RECT rcSrc,
            /* [in] */ RECT rcDst,
            /* [in] */ POINT ptdClickPosition,
            /* [out] */ int *pich,
            /* [out] */ ComBool *pfAssocPrev) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ArrowKeyPosition( 
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [out][in] */ int *pich,
            /* [out][in] */ ComBool *pfAssocPrev,
            /* [in] */ ComBool fRight,
            /* [in] */ ComBool fMovingIn,
            /* [out] */ ComBool *pfResult) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ExtendSelectionPosition( 
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [out][in] */ int *pich,
            /* [in] */ ComBool fAssocPrevMatch,
            /* [in] */ ComBool fAssocPrevNeeded,
            /* [in] */ int ichAnchor,
            /* [in] */ ComBool fRight,
            /* [in] */ ComBool fMovingIn,
            /* [out] */ ComBool *pfRet) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetCharPlacement( 
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ int ichMin,
            /* [in] */ int ichLim,
            /* [in] */ RECT rcSrc,
            /* [in] */ RECT rcDst,
            /* [in] */ ComBool fSkipSpace,
            /* [in] */ int cxdMax,
            /* [out] */ int *pcxd,
            /* [size_is][out] */ int *prgxdLefts,
            /* [size_is][out] */ int *prgxdRights,
            /* [size_is][out] */ int *prgydUnderTops) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DrawTextNoBackground( 
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ RECT rcSrc,
            /* [in] */ RECT rcDst,
            /* [out] */ int *dxdWidth) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ILgSegmentVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ILgSegment * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ILgSegment * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ILgSegment * This);
        
        HRESULT ( STDMETHODCALLTYPE *DrawText )( 
            ILgSegment * This,
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ RECT rcSrc,
            /* [in] */ RECT rcDst,
            /* [out] */ int *dxdWidth);
        
        HRESULT ( STDMETHODCALLTYPE *Recompute )( 
            ILgSegment * This,
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Width )( 
            ILgSegment * This,
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [retval][out] */ int *px);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_RightOverhang )( 
            ILgSegment * This,
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [retval][out] */ int *px);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_LeftOverhang )( 
            ILgSegment * This,
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [retval][out] */ int *px);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Height )( 
            ILgSegment * This,
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [retval][out] */ int *py);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Ascent )( 
            ILgSegment * This,
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [retval][out] */ int *py);
        
        HRESULT ( STDMETHODCALLTYPE *Extent )( 
            ILgSegment * This,
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [out] */ int *px,
            /* [out] */ int *py);
        
        HRESULT ( STDMETHODCALLTYPE *BoundingRect )( 
            ILgSegment * This,
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ RECT rcSrc,
            /* [in] */ RECT rcDst,
            /* [retval][out] */ RECT *prcBounds);
        
        HRESULT ( STDMETHODCALLTYPE *GetActualWidth )( 
            ILgSegment * This,
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ RECT rcSrc,
            /* [in] */ RECT rcDst,
            /* [out] */ int *dxdWidth);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AscentOverhang )( 
            ILgSegment * This,
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [retval][out] */ int *py);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_DescentOverhang )( 
            ILgSegment * This,
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [retval][out] */ int *py);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_RightToLeft )( 
            ILgSegment * This,
            /* [in] */ int ichBase,
            /* [retval][out] */ ComBool *pfResult);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_DirectionDepth )( 
            ILgSegment * This,
            /* [in] */ int ichBase,
            /* [out] */ int *pnDepth,
            /* [retval][out] */ ComBool *pfWeak);
        
        HRESULT ( STDMETHODCALLTYPE *SetDirectionDepth )( 
            ILgSegment * This,
            /* [in] */ int ichwBase,
            /* [in] */ int nNewDepth);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_WritingSystem )( 
            ILgSegment * This,
            /* [in] */ int ichBase,
            /* [retval][out] */ int *pws);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Lim )( 
            ILgSegment * This,
            /* [in] */ int ichBase,
            /* [retval][out] */ int *pdich);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_LimInterest )( 
            ILgSegment * This,
            /* [in] */ int ichBase,
            /* [retval][out] */ int *pdich);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_EndLine )( 
            ILgSegment * This,
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ ComBool fNewVal);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_StartLine )( 
            ILgSegment * This,
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ ComBool fNewVal);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_StartBreakWeight )( 
            ILgSegment * This,
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [retval][out] */ LgLineBreak *plb);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_EndBreakWeight )( 
            ILgSegment * This,
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [retval][out] */ LgLineBreak *plb);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Stretch )( 
            ILgSegment * This,
            /* [in] */ int ichBase,
            /* [retval][out] */ int *pxs);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_Stretch )( 
            ILgSegment * This,
            /* [in] */ int ichBase,
            /* [in] */ int xs);
        
        HRESULT ( STDMETHODCALLTYPE *IsValidInsertionPoint )( 
            ILgSegment * This,
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ int ich,
            /* [retval][out] */ LgIpValidResult *pipvr);
        
        HRESULT ( STDMETHODCALLTYPE *DoBoundariesCoincide )( 
            ILgSegment * This,
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ ComBool fBoundaryEnd,
            /* [in] */ ComBool fBoundaryRight,
            /* [retval][out] */ ComBool *pfResult);
        
        HRESULT ( STDMETHODCALLTYPE *DrawInsertionPoint )( 
            ILgSegment * This,
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ RECT rcSrc,
            /* [in] */ RECT rcDst,
            /* [in] */ int ich,
            /* [in] */ ComBool fAssocPrev,
            /* [in] */ ComBool fOn,
            /* [in] */ LgIPDrawMode dm);
        
        HRESULT ( STDMETHODCALLTYPE *PositionsOfIP )( 
            ILgSegment * This,
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ RECT rcSrc,
            /* [in] */ RECT rcDst,
            /* [in] */ int ich,
            /* [in] */ ComBool fAssocPrev,
            /* [in] */ LgIPDrawMode dm,
            /* [out] */ RECT *rectPrimary,
            /* [out] */ RECT *rectSecondary,
            /* [out] */ ComBool *pfPrimaryHere,
            /* [out] */ ComBool *pfSecHere);
        
        HRESULT ( STDMETHODCALLTYPE *DrawRange )( 
            ILgSegment * This,
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ RECT rcSrc,
            /* [in] */ RECT rcDst,
            /* [in] */ int ichMin,
            /* [in] */ int ichLim,
            /* [in] */ int ydTop,
            /* [in] */ int ydBottom,
            /* [in] */ ComBool bOn,
            /* [in] */ ComBool fIsLastLineOfSelection,
            /* [retval][out] */ RECT *rsBounds);
        
        HRESULT ( STDMETHODCALLTYPE *PositionOfRange )( 
            ILgSegment * This,
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ RECT rcSrc,
            /* [in] */ RECT rcDst,
            /* [in] */ int ichMin,
            /* [in] */ int ichim,
            /* [in] */ int ydTop,
            /* [in] */ int ydBottom,
            /* [in] */ ComBool fIsLastLineOfSelection,
            /* [out] */ RECT *rsBounds,
            /* [retval][out] */ ComBool *pfAnythingToDraw);
        
        HRESULT ( STDMETHODCALLTYPE *PointToChar )( 
            ILgSegment * This,
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ RECT rcSrc,
            /* [in] */ RECT rcDst,
            /* [in] */ POINT ptdClickPosition,
            /* [out] */ int *pich,
            /* [out] */ ComBool *pfAssocPrev);
        
        HRESULT ( STDMETHODCALLTYPE *ArrowKeyPosition )( 
            ILgSegment * This,
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [out][in] */ int *pich,
            /* [out][in] */ ComBool *pfAssocPrev,
            /* [in] */ ComBool fRight,
            /* [in] */ ComBool fMovingIn,
            /* [out] */ ComBool *pfResult);
        
        HRESULT ( STDMETHODCALLTYPE *ExtendSelectionPosition )( 
            ILgSegment * This,
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [out][in] */ int *pich,
            /* [in] */ ComBool fAssocPrevMatch,
            /* [in] */ ComBool fAssocPrevNeeded,
            /* [in] */ int ichAnchor,
            /* [in] */ ComBool fRight,
            /* [in] */ ComBool fMovingIn,
            /* [out] */ ComBool *pfRet);
        
        HRESULT ( STDMETHODCALLTYPE *GetCharPlacement )( 
            ILgSegment * This,
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ int ichMin,
            /* [in] */ int ichLim,
            /* [in] */ RECT rcSrc,
            /* [in] */ RECT rcDst,
            /* [in] */ ComBool fSkipSpace,
            /* [in] */ int cxdMax,
            /* [out] */ int *pcxd,
            /* [size_is][out] */ int *prgxdLefts,
            /* [size_is][out] */ int *prgxdRights,
            /* [size_is][out] */ int *prgydUnderTops);
        
        HRESULT ( STDMETHODCALLTYPE *DrawTextNoBackground )( 
            ILgSegment * This,
            /* [in] */ int ichBase,
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ RECT rcSrc,
            /* [in] */ RECT rcDst,
            /* [out] */ int *dxdWidth);
        
        END_INTERFACE
    } ILgSegmentVtbl;

    interface ILgSegment
    {
        CONST_VTBL struct ILgSegmentVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ILgSegment_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ILgSegment_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ILgSegment_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ILgSegment_DrawText(This,ichBase,pvg,rcSrc,rcDst,dxdWidth)	\
    ( (This)->lpVtbl -> DrawText(This,ichBase,pvg,rcSrc,rcDst,dxdWidth) ) 

#define ILgSegment_Recompute(This,ichBase,pvg)	\
    ( (This)->lpVtbl -> Recompute(This,ichBase,pvg) ) 

#define ILgSegment_get_Width(This,ichBase,pvg,px)	\
    ( (This)->lpVtbl -> get_Width(This,ichBase,pvg,px) ) 

#define ILgSegment_get_RightOverhang(This,ichBase,pvg,px)	\
    ( (This)->lpVtbl -> get_RightOverhang(This,ichBase,pvg,px) ) 

#define ILgSegment_get_LeftOverhang(This,ichBase,pvg,px)	\
    ( (This)->lpVtbl -> get_LeftOverhang(This,ichBase,pvg,px) ) 

#define ILgSegment_get_Height(This,ichBase,pvg,py)	\
    ( (This)->lpVtbl -> get_Height(This,ichBase,pvg,py) ) 

#define ILgSegment_get_Ascent(This,ichBase,pvg,py)	\
    ( (This)->lpVtbl -> get_Ascent(This,ichBase,pvg,py) ) 

#define ILgSegment_Extent(This,ichBase,pvg,px,py)	\
    ( (This)->lpVtbl -> Extent(This,ichBase,pvg,px,py) ) 

#define ILgSegment_BoundingRect(This,ichBase,pvg,rcSrc,rcDst,prcBounds)	\
    ( (This)->lpVtbl -> BoundingRect(This,ichBase,pvg,rcSrc,rcDst,prcBounds) ) 

#define ILgSegment_GetActualWidth(This,ichBase,pvg,rcSrc,rcDst,dxdWidth)	\
    ( (This)->lpVtbl -> GetActualWidth(This,ichBase,pvg,rcSrc,rcDst,dxdWidth) ) 

#define ILgSegment_get_AscentOverhang(This,ichBase,pvg,py)	\
    ( (This)->lpVtbl -> get_AscentOverhang(This,ichBase,pvg,py) ) 

#define ILgSegment_get_DescentOverhang(This,ichBase,pvg,py)	\
    ( (This)->lpVtbl -> get_DescentOverhang(This,ichBase,pvg,py) ) 

#define ILgSegment_get_RightToLeft(This,ichBase,pfResult)	\
    ( (This)->lpVtbl -> get_RightToLeft(This,ichBase,pfResult) ) 

#define ILgSegment_get_DirectionDepth(This,ichBase,pnDepth,pfWeak)	\
    ( (This)->lpVtbl -> get_DirectionDepth(This,ichBase,pnDepth,pfWeak) ) 

#define ILgSegment_SetDirectionDepth(This,ichwBase,nNewDepth)	\
    ( (This)->lpVtbl -> SetDirectionDepth(This,ichwBase,nNewDepth) ) 

#define ILgSegment_get_WritingSystem(This,ichBase,pws)	\
    ( (This)->lpVtbl -> get_WritingSystem(This,ichBase,pws) ) 

#define ILgSegment_get_Lim(This,ichBase,pdich)	\
    ( (This)->lpVtbl -> get_Lim(This,ichBase,pdich) ) 

#define ILgSegment_get_LimInterest(This,ichBase,pdich)	\
    ( (This)->lpVtbl -> get_LimInterest(This,ichBase,pdich) ) 

#define ILgSegment_put_EndLine(This,ichBase,pvg,fNewVal)	\
    ( (This)->lpVtbl -> put_EndLine(This,ichBase,pvg,fNewVal) ) 

#define ILgSegment_put_StartLine(This,ichBase,pvg,fNewVal)	\
    ( (This)->lpVtbl -> put_StartLine(This,ichBase,pvg,fNewVal) ) 

#define ILgSegment_get_StartBreakWeight(This,ichBase,pvg,plb)	\
    ( (This)->lpVtbl -> get_StartBreakWeight(This,ichBase,pvg,plb) ) 

#define ILgSegment_get_EndBreakWeight(This,ichBase,pvg,plb)	\
    ( (This)->lpVtbl -> get_EndBreakWeight(This,ichBase,pvg,plb) ) 

#define ILgSegment_get_Stretch(This,ichBase,pxs)	\
    ( (This)->lpVtbl -> get_Stretch(This,ichBase,pxs) ) 

#define ILgSegment_put_Stretch(This,ichBase,xs)	\
    ( (This)->lpVtbl -> put_Stretch(This,ichBase,xs) ) 

#define ILgSegment_IsValidInsertionPoint(This,ichBase,pvg,ich,pipvr)	\
    ( (This)->lpVtbl -> IsValidInsertionPoint(This,ichBase,pvg,ich,pipvr) ) 

#define ILgSegment_DoBoundariesCoincide(This,ichBase,pvg,fBoundaryEnd,fBoundaryRight,pfResult)	\
    ( (This)->lpVtbl -> DoBoundariesCoincide(This,ichBase,pvg,fBoundaryEnd,fBoundaryRight,pfResult) ) 

#define ILgSegment_DrawInsertionPoint(This,ichBase,pvg,rcSrc,rcDst,ich,fAssocPrev,fOn,dm)	\
    ( (This)->lpVtbl -> DrawInsertionPoint(This,ichBase,pvg,rcSrc,rcDst,ich,fAssocPrev,fOn,dm) ) 

#define ILgSegment_PositionsOfIP(This,ichBase,pvg,rcSrc,rcDst,ich,fAssocPrev,dm,rectPrimary,rectSecondary,pfPrimaryHere,pfSecHere)	\
    ( (This)->lpVtbl -> PositionsOfIP(This,ichBase,pvg,rcSrc,rcDst,ich,fAssocPrev,dm,rectPrimary,rectSecondary,pfPrimaryHere,pfSecHere) ) 

#define ILgSegment_DrawRange(This,ichBase,pvg,rcSrc,rcDst,ichMin,ichLim,ydTop,ydBottom,bOn,fIsLastLineOfSelection,rsBounds)	\
    ( (This)->lpVtbl -> DrawRange(This,ichBase,pvg,rcSrc,rcDst,ichMin,ichLim,ydTop,ydBottom,bOn,fIsLastLineOfSelection,rsBounds) ) 

#define ILgSegment_PositionOfRange(This,ichBase,pvg,rcSrc,rcDst,ichMin,ichim,ydTop,ydBottom,fIsLastLineOfSelection,rsBounds,pfAnythingToDraw)	\
    ( (This)->lpVtbl -> PositionOfRange(This,ichBase,pvg,rcSrc,rcDst,ichMin,ichim,ydTop,ydBottom,fIsLastLineOfSelection,rsBounds,pfAnythingToDraw) ) 

#define ILgSegment_PointToChar(This,ichBase,pvg,rcSrc,rcDst,ptdClickPosition,pich,pfAssocPrev)	\
    ( (This)->lpVtbl -> PointToChar(This,ichBase,pvg,rcSrc,rcDst,ptdClickPosition,pich,pfAssocPrev) ) 

#define ILgSegment_ArrowKeyPosition(This,ichBase,pvg,pich,pfAssocPrev,fRight,fMovingIn,pfResult)	\
    ( (This)->lpVtbl -> ArrowKeyPosition(This,ichBase,pvg,pich,pfAssocPrev,fRight,fMovingIn,pfResult) ) 

#define ILgSegment_ExtendSelectionPosition(This,ichBase,pvg,pich,fAssocPrevMatch,fAssocPrevNeeded,ichAnchor,fRight,fMovingIn,pfRet)	\
    ( (This)->lpVtbl -> ExtendSelectionPosition(This,ichBase,pvg,pich,fAssocPrevMatch,fAssocPrevNeeded,ichAnchor,fRight,fMovingIn,pfRet) ) 

#define ILgSegment_GetCharPlacement(This,ichBase,pvg,ichMin,ichLim,rcSrc,rcDst,fSkipSpace,cxdMax,pcxd,prgxdLefts,prgxdRights,prgydUnderTops)	\
    ( (This)->lpVtbl -> GetCharPlacement(This,ichBase,pvg,ichMin,ichLim,rcSrc,rcDst,fSkipSpace,cxdMax,pcxd,prgxdLefts,prgxdRights,prgydUnderTops) ) 

#define ILgSegment_DrawTextNoBackground(This,ichBase,pvg,rcSrc,rcDst,dxdWidth)	\
    ( (This)->lpVtbl -> DrawTextNoBackground(This,ichBase,pvg,rcSrc,rcDst,dxdWidth) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ILgSegment_INTERFACE_DEFINED__ */


#ifndef __IRenderEngine_INTERFACE_DEFINED__
#define __IRenderEngine_INTERFACE_DEFINED__

/* interface IRenderEngine */
/* [unique][object][uuid] */ 


#define IID_IRenderEngine __uuidof(IRenderEngine)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("B8998799-3C5F-47D3-BC14-10AA10AEC691")
    IRenderEngine : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE InitRenderer( 
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ BSTR bstrData) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_RenderEngineFactory( 
            /* [retval][out] */ IRenderEngineFactory **ppref) = 0;
        
        virtual /* [propputref] */ HRESULT STDMETHODCALLTYPE putref_RenderEngineFactory( 
            /* [in] */ IRenderEngineFactory *pref) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_FontIsValid( 
            /* [retval][out] */ ComBool *pfValid) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_SegDatMaxLength( 
            /* [retval][out] */ int *cb) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE FindBreakPoint( 
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ IVwTextSource *pts,
            /* [in] */ IVwJustifier *pvjus,
            /* [in] */ int ichMin,
            /* [in] */ int ichLim,
            /* [in] */ int ichLimBacktrack,
            /* [in] */ ComBool fNeedFinalBreak,
            /* [in] */ ComBool fStartLine,
            /* [in] */ int dxMaxWidth,
            /* [in] */ LgLineBreak lbPref,
            /* [in] */ LgLineBreak lbMax,
            /* [in] */ LgTrailingWsHandling twsh,
            /* [in] */ ComBool fParaRightToLeft,
            /* [out] */ ILgSegment **ppsegRet,
            /* [out] */ int *pdichLimSeg,
            /* [out] */ int *pdxWidth,
            /* [out] */ LgEndSegmentType *pest,
            /* [in] */ ILgSegment *psegPrev) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ScriptDirection( 
            /* [retval][out] */ int *pgrfsdc) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ClassId( 
            /* [retval][out] */ GUID *pguid) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_WritingSystemFactory( 
            /* [retval][out] */ /* external definition not present */ ILgWritingSystemFactory **ppwsf) = 0;
        
        virtual /* [propputref] */ HRESULT STDMETHODCALLTYPE putref_WritingSystemFactory( 
            /* [in] */ /* external definition not present */ ILgWritingSystemFactory *pwsf) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IRenderEngineVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IRenderEngine * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IRenderEngine * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IRenderEngine * This);
        
        HRESULT ( STDMETHODCALLTYPE *InitRenderer )( 
            IRenderEngine * This,
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ BSTR bstrData);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_RenderEngineFactory )( 
            IRenderEngine * This,
            /* [retval][out] */ IRenderEngineFactory **ppref);
        
        /* [propputref] */ HRESULT ( STDMETHODCALLTYPE *putref_RenderEngineFactory )( 
            IRenderEngine * This,
            /* [in] */ IRenderEngineFactory *pref);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_FontIsValid )( 
            IRenderEngine * This,
            /* [retval][out] */ ComBool *pfValid);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_SegDatMaxLength )( 
            IRenderEngine * This,
            /* [retval][out] */ int *cb);
        
        HRESULT ( STDMETHODCALLTYPE *FindBreakPoint )( 
            IRenderEngine * This,
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ IVwTextSource *pts,
            /* [in] */ IVwJustifier *pvjus,
            /* [in] */ int ichMin,
            /* [in] */ int ichLim,
            /* [in] */ int ichLimBacktrack,
            /* [in] */ ComBool fNeedFinalBreak,
            /* [in] */ ComBool fStartLine,
            /* [in] */ int dxMaxWidth,
            /* [in] */ LgLineBreak lbPref,
            /* [in] */ LgLineBreak lbMax,
            /* [in] */ LgTrailingWsHandling twsh,
            /* [in] */ ComBool fParaRightToLeft,
            /* [out] */ ILgSegment **ppsegRet,
            /* [out] */ int *pdichLimSeg,
            /* [out] */ int *pdxWidth,
            /* [out] */ LgEndSegmentType *pest,
            /* [in] */ ILgSegment *psegPrev);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ScriptDirection )( 
            IRenderEngine * This,
            /* [retval][out] */ int *pgrfsdc);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ClassId )( 
            IRenderEngine * This,
            /* [retval][out] */ GUID *pguid);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_WritingSystemFactory )( 
            IRenderEngine * This,
            /* [retval][out] */ /* external definition not present */ ILgWritingSystemFactory **ppwsf);
        
        /* [propputref] */ HRESULT ( STDMETHODCALLTYPE *putref_WritingSystemFactory )( 
            IRenderEngine * This,
            /* [in] */ /* external definition not present */ ILgWritingSystemFactory *pwsf);
        
        END_INTERFACE
    } IRenderEngineVtbl;

    interface IRenderEngine
    {
        CONST_VTBL struct IRenderEngineVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IRenderEngine_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IRenderEngine_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IRenderEngine_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IRenderEngine_InitRenderer(This,pvg,bstrData)	\
    ( (This)->lpVtbl -> InitRenderer(This,pvg,bstrData) ) 

#define IRenderEngine_get_RenderEngineFactory(This,ppref)	\
    ( (This)->lpVtbl -> get_RenderEngineFactory(This,ppref) ) 

#define IRenderEngine_putref_RenderEngineFactory(This,pref)	\
    ( (This)->lpVtbl -> putref_RenderEngineFactory(This,pref) ) 

#define IRenderEngine_get_FontIsValid(This,pfValid)	\
    ( (This)->lpVtbl -> get_FontIsValid(This,pfValid) ) 

#define IRenderEngine_get_SegDatMaxLength(This,cb)	\
    ( (This)->lpVtbl -> get_SegDatMaxLength(This,cb) ) 

#define IRenderEngine_FindBreakPoint(This,pvg,pts,pvjus,ichMin,ichLim,ichLimBacktrack,fNeedFinalBreak,fStartLine,dxMaxWidth,lbPref,lbMax,twsh,fParaRightToLeft,ppsegRet,pdichLimSeg,pdxWidth,pest,psegPrev)	\
    ( (This)->lpVtbl -> FindBreakPoint(This,pvg,pts,pvjus,ichMin,ichLim,ichLimBacktrack,fNeedFinalBreak,fStartLine,dxMaxWidth,lbPref,lbMax,twsh,fParaRightToLeft,ppsegRet,pdichLimSeg,pdxWidth,pest,psegPrev) ) 

#define IRenderEngine_get_ScriptDirection(This,pgrfsdc)	\
    ( (This)->lpVtbl -> get_ScriptDirection(This,pgrfsdc) ) 

#define IRenderEngine_get_ClassId(This,pguid)	\
    ( (This)->lpVtbl -> get_ClassId(This,pguid) ) 

#define IRenderEngine_get_WritingSystemFactory(This,ppwsf)	\
    ( (This)->lpVtbl -> get_WritingSystemFactory(This,ppwsf) ) 

#define IRenderEngine_putref_WritingSystemFactory(This,pwsf)	\
    ( (This)->lpVtbl -> putref_WritingSystemFactory(This,pwsf) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IRenderEngine_INTERFACE_DEFINED__ */


#define CLSID_LgLineBreaker __uuidof(LgLineBreaker)

#ifdef __cplusplus

class DECLSPEC_UUID("94FBFA34-21E5-4A1E-B576-BA5D76CC051A")
LgLineBreaker;
#endif

#define CLSID_RomRenderEngine __uuidof(RomRenderEngine)

#ifdef __cplusplus

class DECLSPEC_UUID("6EACAB83-6BDC-49CA-8F66-8C116D3EEBD8")
RomRenderEngine;
#endif

#define CLSID_UniscribeEngine __uuidof(UniscribeEngine)

#ifdef __cplusplus

class DECLSPEC_UUID("1287735C-3CAD-41CD-986C-39D7C0DF0314")
UniscribeEngine;
#endif

#define CLSID_GraphiteEngine __uuidof(GraphiteEngine)

#ifdef __cplusplus

class DECLSPEC_UUID("62EBEEBF-14EA-43D9-A27A-EF013E14145A")
GraphiteEngine;
#endif

#ifndef __IRenderingFeatures_INTERFACE_DEFINED__
#define __IRenderingFeatures_INTERFACE_DEFINED__

/* interface IRenderingFeatures */
/* [unique][object][uuid] */ 


#define IID_IRenderingFeatures __uuidof(IRenderingFeatures)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("75AFE861-3C17-4F16-851F-A36F5FFABCC6")
    IRenderingFeatures : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE GetFeatureIDs( 
            /* [in] */ int cMax,
            /* [size_is][out] */ int *prgFids,
            /* [out] */ int *pcfid) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFeatureLabel( 
            /* [in] */ int fid,
            /* [in] */ int nLanguage,
            /* [out] */ BSTR *pbstrLabel) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFeatureValues( 
            /* [in] */ int fid,
            /* [in] */ int cfvalMax,
            /* [size_is][out] */ int *prgfval,
            /* [out] */ int *pcfval,
            /* [out] */ int *pfvalDefault) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFeatureValueLabel( 
            /* [in] */ int fid,
            /* [in] */ int fval,
            /* [in] */ int nLanguage,
            /* [out] */ BSTR *pbstrLabel) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IRenderingFeaturesVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IRenderingFeatures * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IRenderingFeatures * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IRenderingFeatures * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetFeatureIDs )( 
            IRenderingFeatures * This,
            /* [in] */ int cMax,
            /* [size_is][out] */ int *prgFids,
            /* [out] */ int *pcfid);
        
        HRESULT ( STDMETHODCALLTYPE *GetFeatureLabel )( 
            IRenderingFeatures * This,
            /* [in] */ int fid,
            /* [in] */ int nLanguage,
            /* [out] */ BSTR *pbstrLabel);
        
        HRESULT ( STDMETHODCALLTYPE *GetFeatureValues )( 
            IRenderingFeatures * This,
            /* [in] */ int fid,
            /* [in] */ int cfvalMax,
            /* [size_is][out] */ int *prgfval,
            /* [out] */ int *pcfval,
            /* [out] */ int *pfvalDefault);
        
        HRESULT ( STDMETHODCALLTYPE *GetFeatureValueLabel )( 
            IRenderingFeatures * This,
            /* [in] */ int fid,
            /* [in] */ int fval,
            /* [in] */ int nLanguage,
            /* [out] */ BSTR *pbstrLabel);
        
        END_INTERFACE
    } IRenderingFeaturesVtbl;

    interface IRenderingFeatures
    {
        CONST_VTBL struct IRenderingFeaturesVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IRenderingFeatures_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IRenderingFeatures_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IRenderingFeatures_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IRenderingFeatures_GetFeatureIDs(This,cMax,prgFids,pcfid)	\
    ( (This)->lpVtbl -> GetFeatureIDs(This,cMax,prgFids,pcfid) ) 

#define IRenderingFeatures_GetFeatureLabel(This,fid,nLanguage,pbstrLabel)	\
    ( (This)->lpVtbl -> GetFeatureLabel(This,fid,nLanguage,pbstrLabel) ) 

#define IRenderingFeatures_GetFeatureValues(This,fid,cfvalMax,prgfval,pcfval,pfvalDefault)	\
    ( (This)->lpVtbl -> GetFeatureValues(This,fid,cfvalMax,prgfval,pcfval,pfvalDefault) ) 

#define IRenderingFeatures_GetFeatureValueLabel(This,fid,fval,nLanguage,pbstrLabel)	\
    ( (This)->lpVtbl -> GetFeatureValueLabel(This,fid,fval,nLanguage,pbstrLabel) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IRenderingFeatures_INTERFACE_DEFINED__ */


#ifndef __IVwSelection_INTERFACE_DEFINED__
#define __IVwSelection_INTERFACE_DEFINED__

/* interface IVwSelection */
/* [unique][object][uuid] */ 


#define IID_IVwSelection __uuidof(IVwSelection)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("4F8B678D-C5BA-4a2f-B9B3-2780956E3616")
    IVwSelection : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsRange( 
            /* [retval][out] */ ComBool *pfRet) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetSelectionProps( 
            /* [in] */ int cttpMax,
            /* [size_is][out][in] */ /* external definition not present */ ITsTextProps **prgpttp,
            /* [size_is][out][in] */ IVwPropertyStore **prgpvps,
            /* [out] */ int *pcttp) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetHardAndSoftCharProps( 
            /* [in] */ int cttpMax,
            /* [size_is][out][in] */ /* external definition not present */ ITsTextProps **prgpttpSel,
            /* [size_is][out][in] */ IVwPropertyStore **prgpvpsSoft,
            /* [out] */ int *pcttp) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetParaProps( 
            /* [in] */ int cttpMax,
            /* [size_is][out][in] */ IVwPropertyStore **prgpvps,
            /* [out] */ int *pcttp) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetHardAndSoftParaProps( 
            /* [in] */ int cttpMax,
            /* [size_is][in] */ /* external definition not present */ ITsTextProps **prgpttpPara,
            /* [size_is][out][in] */ /* external definition not present */ ITsTextProps **prgpttpHard,
            /* [size_is][out][in] */ IVwPropertyStore **prgpvpsSoft,
            /* [out] */ int *pcttp) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetSelectionProps( 
            /* [in] */ int cttp,
            /* [size_is][in] */ /* external definition not present */ ITsTextProps **prgpttp) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE TextSelInfo( 
            /* [in] */ ComBool fEndPoint,
            /* [out] */ /* external definition not present */ ITsString **pptss,
            /* [out] */ int *pich,
            /* [out] */ ComBool *pfAssocPrev,
            /* [out] */ HVO *phvoObj,
            /* [out] */ PropTag *ptag,
            /* [out] */ int *pws) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CLevels( 
            /* [in] */ ComBool fEndPoint,
            /* [retval][out] */ int *pclev) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE PropInfo( 
            /* [in] */ ComBool fEndPoint,
            /* [in] */ int ilev,
            /* [out] */ HVO *phvoObj,
            /* [out] */ PropTag *ptag,
            /* [out] */ int *pihvo,
            /* [out] */ int *pcpropPrevious,
            /* [out] */ IVwPropertyStore **ppvps) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AllTextSelInfo( 
            /* [out] */ int *pihvoRoot,
            /* [in] */ int cvlsi,
            /* [size_is][out] */ VwSelLevInfo *prgvsli,
            /* [out] */ PropTag *ptagTextProp,
            /* [out] */ int *pcpropPrevious,
            /* [out] */ int *pichAnchor,
            /* [out] */ int *pichEnd,
            /* [out] */ int *pws,
            /* [out] */ ComBool *pfAssocPrev,
            /* [out] */ int *pihvoEnd,
            /* [out] */ /* external definition not present */ ITsTextProps **ppttp) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AllSelEndInfo( 
            /* [in] */ ComBool fEndPoint,
            /* [out] */ int *pihvoRoot,
            /* [in] */ int cvlsi,
            /* [size_is][out] */ VwSelLevInfo *prgvsli,
            /* [out] */ PropTag *ptagTextProp,
            /* [out] */ int *pcpropPrevious,
            /* [out] */ int *pich,
            /* [out] */ int *pws,
            /* [out] */ ComBool *pfAssocPrev,
            /* [out] */ /* external definition not present */ ITsTextProps **ppttp) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CompleteEdits( 
            /* [out] */ VwChangeInfo *pci,
            /* [retval][out] */ ComBool *pfOk) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ExtendToStringBoundaries( void) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_EndBeforeAnchor( 
            /* [retval][out] */ ComBool *pfRet) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Location( 
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ RECT rcSrc,
            /* [in] */ RECT rcDst,
            /* [out] */ RECT *prdPrimary,
            /* [out] */ RECT *prdSecondary,
            /* [out] */ ComBool *pfSplit,
            /* [out] */ ComBool *pfEndBeforeAnchor) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetParaLocation( 
            /* [out] */ RECT *prdLoc) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ReplaceWithTsString( 
            /* [in] */ /* external definition not present */ ITsString *ptss) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetSelectionString( 
            /* [out] */ /* external definition not present */ ITsString **pptss,
            /* [in] */ BSTR bstrSep) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetFirstParaString( 
            /* [out] */ /* external definition not present */ ITsString **pptss,
            /* [in] */ BSTR bstrSep,
            /* [out] */ ComBool *pfGotItAll) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetIPLocation( 
            /* [in] */ ComBool fTopLine,
            /* [in] */ int xdPos) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_CanFormatPara( 
            /* [retval][out] */ ComBool *pfRet) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_CanFormatChar( 
            /* [retval][out] */ ComBool *pfRet) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_CanFormatOverlay( 
            /* [retval][out] */ ComBool *pfRet) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Install( void) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Follows( 
            /* [in] */ IVwSelection *psel,
            /* [retval][out] */ ComBool *pfFollows) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsValid( 
            /* [retval][out] */ ComBool *pfValid) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ParagraphOffset( 
            /* [in] */ ComBool fEndPoint,
            /* [retval][out] */ int *pich) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AssocPrev( 
            /* [retval][out] */ ComBool *pfValue) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_AssocPrev( 
            /* [in] */ ComBool fValue) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_SelType( 
            /* [retval][out] */ VwSelType *piType) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_RootBox( 
            /* [retval][out] */ IVwRootBox **pprootb) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GrowToWord( 
            /* [retval][out] */ IVwSelection **ppsel) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EndPoint( 
            /* [in] */ ComBool fEndPoint,
            /* [retval][out] */ IVwSelection **ppsel) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetTypingProps( 
            /* [in] */ /* external definition not present */ ITsTextProps *pttp) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_BoxDepth( 
            /* [in] */ ComBool fEndPoint,
            /* [retval][out] */ int *pcDepth) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_BoxIndex( 
            /* [in] */ ComBool fEndPoint,
            /* [in] */ int iLevel,
            /* [retval][out] */ int *piAtLevel) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_BoxCount( 
            /* [in] */ ComBool fEndPoint,
            /* [in] */ int iLevel,
            /* [retval][out] */ int *pcAtLevel) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_BoxType( 
            /* [in] */ ComBool fEndPoint,
            /* [in] */ int iLevel,
            /* [retval][out] */ VwBoxType *pvbt) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsEditable( 
            /* [retval][out] */ ComBool *pfEditable) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsEnabled( 
            /* [retval][out] */ ComBool *pfEnabled) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IVwSelectionVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IVwSelection * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IVwSelection * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IVwSelection * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsRange )( 
            IVwSelection * This,
            /* [retval][out] */ ComBool *pfRet);
        
        HRESULT ( STDMETHODCALLTYPE *GetSelectionProps )( 
            IVwSelection * This,
            /* [in] */ int cttpMax,
            /* [size_is][out][in] */ /* external definition not present */ ITsTextProps **prgpttp,
            /* [size_is][out][in] */ IVwPropertyStore **prgpvps,
            /* [out] */ int *pcttp);
        
        HRESULT ( STDMETHODCALLTYPE *GetHardAndSoftCharProps )( 
            IVwSelection * This,
            /* [in] */ int cttpMax,
            /* [size_is][out][in] */ /* external definition not present */ ITsTextProps **prgpttpSel,
            /* [size_is][out][in] */ IVwPropertyStore **prgpvpsSoft,
            /* [out] */ int *pcttp);
        
        HRESULT ( STDMETHODCALLTYPE *GetParaProps )( 
            IVwSelection * This,
            /* [in] */ int cttpMax,
            /* [size_is][out][in] */ IVwPropertyStore **prgpvps,
            /* [out] */ int *pcttp);
        
        HRESULT ( STDMETHODCALLTYPE *GetHardAndSoftParaProps )( 
            IVwSelection * This,
            /* [in] */ int cttpMax,
            /* [size_is][in] */ /* external definition not present */ ITsTextProps **prgpttpPara,
            /* [size_is][out][in] */ /* external definition not present */ ITsTextProps **prgpttpHard,
            /* [size_is][out][in] */ IVwPropertyStore **prgpvpsSoft,
            /* [out] */ int *pcttp);
        
        HRESULT ( STDMETHODCALLTYPE *SetSelectionProps )( 
            IVwSelection * This,
            /* [in] */ int cttp,
            /* [size_is][in] */ /* external definition not present */ ITsTextProps **prgpttp);
        
        HRESULT ( STDMETHODCALLTYPE *TextSelInfo )( 
            IVwSelection * This,
            /* [in] */ ComBool fEndPoint,
            /* [out] */ /* external definition not present */ ITsString **pptss,
            /* [out] */ int *pich,
            /* [out] */ ComBool *pfAssocPrev,
            /* [out] */ HVO *phvoObj,
            /* [out] */ PropTag *ptag,
            /* [out] */ int *pws);
        
        HRESULT ( STDMETHODCALLTYPE *CLevels )( 
            IVwSelection * This,
            /* [in] */ ComBool fEndPoint,
            /* [retval][out] */ int *pclev);
        
        HRESULT ( STDMETHODCALLTYPE *PropInfo )( 
            IVwSelection * This,
            /* [in] */ ComBool fEndPoint,
            /* [in] */ int ilev,
            /* [out] */ HVO *phvoObj,
            /* [out] */ PropTag *ptag,
            /* [out] */ int *pihvo,
            /* [out] */ int *pcpropPrevious,
            /* [out] */ IVwPropertyStore **ppvps);
        
        HRESULT ( STDMETHODCALLTYPE *AllTextSelInfo )( 
            IVwSelection * This,
            /* [out] */ int *pihvoRoot,
            /* [in] */ int cvlsi,
            /* [size_is][out] */ VwSelLevInfo *prgvsli,
            /* [out] */ PropTag *ptagTextProp,
            /* [out] */ int *pcpropPrevious,
            /* [out] */ int *pichAnchor,
            /* [out] */ int *pichEnd,
            /* [out] */ int *pws,
            /* [out] */ ComBool *pfAssocPrev,
            /* [out] */ int *pihvoEnd,
            /* [out] */ /* external definition not present */ ITsTextProps **ppttp);
        
        HRESULT ( STDMETHODCALLTYPE *AllSelEndInfo )( 
            IVwSelection * This,
            /* [in] */ ComBool fEndPoint,
            /* [out] */ int *pihvoRoot,
            /* [in] */ int cvlsi,
            /* [size_is][out] */ VwSelLevInfo *prgvsli,
            /* [out] */ PropTag *ptagTextProp,
            /* [out] */ int *pcpropPrevious,
            /* [out] */ int *pich,
            /* [out] */ int *pws,
            /* [out] */ ComBool *pfAssocPrev,
            /* [out] */ /* external definition not present */ ITsTextProps **ppttp);
        
        HRESULT ( STDMETHODCALLTYPE *CompleteEdits )( 
            IVwSelection * This,
            /* [out] */ VwChangeInfo *pci,
            /* [retval][out] */ ComBool *pfOk);
        
        HRESULT ( STDMETHODCALLTYPE *ExtendToStringBoundaries )( 
            IVwSelection * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_EndBeforeAnchor )( 
            IVwSelection * This,
            /* [retval][out] */ ComBool *pfRet);
        
        HRESULT ( STDMETHODCALLTYPE *Location )( 
            IVwSelection * This,
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ RECT rcSrc,
            /* [in] */ RECT rcDst,
            /* [out] */ RECT *prdPrimary,
            /* [out] */ RECT *prdSecondary,
            /* [out] */ ComBool *pfSplit,
            /* [out] */ ComBool *pfEndBeforeAnchor);
        
        HRESULT ( STDMETHODCALLTYPE *GetParaLocation )( 
            IVwSelection * This,
            /* [out] */ RECT *prdLoc);
        
        HRESULT ( STDMETHODCALLTYPE *ReplaceWithTsString )( 
            IVwSelection * This,
            /* [in] */ /* external definition not present */ ITsString *ptss);
        
        HRESULT ( STDMETHODCALLTYPE *GetSelectionString )( 
            IVwSelection * This,
            /* [out] */ /* external definition not present */ ITsString **pptss,
            /* [in] */ BSTR bstrSep);
        
        HRESULT ( STDMETHODCALLTYPE *GetFirstParaString )( 
            IVwSelection * This,
            /* [out] */ /* external definition not present */ ITsString **pptss,
            /* [in] */ BSTR bstrSep,
            /* [out] */ ComBool *pfGotItAll);
        
        HRESULT ( STDMETHODCALLTYPE *SetIPLocation )( 
            IVwSelection * This,
            /* [in] */ ComBool fTopLine,
            /* [in] */ int xdPos);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_CanFormatPara )( 
            IVwSelection * This,
            /* [retval][out] */ ComBool *pfRet);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_CanFormatChar )( 
            IVwSelection * This,
            /* [retval][out] */ ComBool *pfRet);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_CanFormatOverlay )( 
            IVwSelection * This,
            /* [retval][out] */ ComBool *pfRet);
        
        HRESULT ( STDMETHODCALLTYPE *Install )( 
            IVwSelection * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Follows )( 
            IVwSelection * This,
            /* [in] */ IVwSelection *psel,
            /* [retval][out] */ ComBool *pfFollows);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsValid )( 
            IVwSelection * This,
            /* [retval][out] */ ComBool *pfValid);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ParagraphOffset )( 
            IVwSelection * This,
            /* [in] */ ComBool fEndPoint,
            /* [retval][out] */ int *pich);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AssocPrev )( 
            IVwSelection * This,
            /* [retval][out] */ ComBool *pfValue);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_AssocPrev )( 
            IVwSelection * This,
            /* [in] */ ComBool fValue);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_SelType )( 
            IVwSelection * This,
            /* [retval][out] */ VwSelType *piType);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_RootBox )( 
            IVwSelection * This,
            /* [retval][out] */ IVwRootBox **pprootb);
        
        HRESULT ( STDMETHODCALLTYPE *GrowToWord )( 
            IVwSelection * This,
            /* [retval][out] */ IVwSelection **ppsel);
        
        HRESULT ( STDMETHODCALLTYPE *EndPoint )( 
            IVwSelection * This,
            /* [in] */ ComBool fEndPoint,
            /* [retval][out] */ IVwSelection **ppsel);
        
        HRESULT ( STDMETHODCALLTYPE *SetTypingProps )( 
            IVwSelection * This,
            /* [in] */ /* external definition not present */ ITsTextProps *pttp);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_BoxDepth )( 
            IVwSelection * This,
            /* [in] */ ComBool fEndPoint,
            /* [retval][out] */ int *pcDepth);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_BoxIndex )( 
            IVwSelection * This,
            /* [in] */ ComBool fEndPoint,
            /* [in] */ int iLevel,
            /* [retval][out] */ int *piAtLevel);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_BoxCount )( 
            IVwSelection * This,
            /* [in] */ ComBool fEndPoint,
            /* [in] */ int iLevel,
            /* [retval][out] */ int *pcAtLevel);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_BoxType )( 
            IVwSelection * This,
            /* [in] */ ComBool fEndPoint,
            /* [in] */ int iLevel,
            /* [retval][out] */ VwBoxType *pvbt);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsEditable )( 
            IVwSelection * This,
            /* [retval][out] */ ComBool *pfEditable);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsEnabled )( 
            IVwSelection * This,
            /* [retval][out] */ ComBool *pfEnabled);
        
        END_INTERFACE
    } IVwSelectionVtbl;

    interface IVwSelection
    {
        CONST_VTBL struct IVwSelectionVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IVwSelection_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IVwSelection_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IVwSelection_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IVwSelection_get_IsRange(This,pfRet)	\
    ( (This)->lpVtbl -> get_IsRange(This,pfRet) ) 

#define IVwSelection_GetSelectionProps(This,cttpMax,prgpttp,prgpvps,pcttp)	\
    ( (This)->lpVtbl -> GetSelectionProps(This,cttpMax,prgpttp,prgpvps,pcttp) ) 

#define IVwSelection_GetHardAndSoftCharProps(This,cttpMax,prgpttpSel,prgpvpsSoft,pcttp)	\
    ( (This)->lpVtbl -> GetHardAndSoftCharProps(This,cttpMax,prgpttpSel,prgpvpsSoft,pcttp) ) 

#define IVwSelection_GetParaProps(This,cttpMax,prgpvps,pcttp)	\
    ( (This)->lpVtbl -> GetParaProps(This,cttpMax,prgpvps,pcttp) ) 

#define IVwSelection_GetHardAndSoftParaProps(This,cttpMax,prgpttpPara,prgpttpHard,prgpvpsSoft,pcttp)	\
    ( (This)->lpVtbl -> GetHardAndSoftParaProps(This,cttpMax,prgpttpPara,prgpttpHard,prgpvpsSoft,pcttp) ) 

#define IVwSelection_SetSelectionProps(This,cttp,prgpttp)	\
    ( (This)->lpVtbl -> SetSelectionProps(This,cttp,prgpttp) ) 

#define IVwSelection_TextSelInfo(This,fEndPoint,pptss,pich,pfAssocPrev,phvoObj,ptag,pws)	\
    ( (This)->lpVtbl -> TextSelInfo(This,fEndPoint,pptss,pich,pfAssocPrev,phvoObj,ptag,pws) ) 

#define IVwSelection_CLevels(This,fEndPoint,pclev)	\
    ( (This)->lpVtbl -> CLevels(This,fEndPoint,pclev) ) 

#define IVwSelection_PropInfo(This,fEndPoint,ilev,phvoObj,ptag,pihvo,pcpropPrevious,ppvps)	\
    ( (This)->lpVtbl -> PropInfo(This,fEndPoint,ilev,phvoObj,ptag,pihvo,pcpropPrevious,ppvps) ) 

#define IVwSelection_AllTextSelInfo(This,pihvoRoot,cvlsi,prgvsli,ptagTextProp,pcpropPrevious,pichAnchor,pichEnd,pws,pfAssocPrev,pihvoEnd,ppttp)	\
    ( (This)->lpVtbl -> AllTextSelInfo(This,pihvoRoot,cvlsi,prgvsli,ptagTextProp,pcpropPrevious,pichAnchor,pichEnd,pws,pfAssocPrev,pihvoEnd,ppttp) ) 

#define IVwSelection_AllSelEndInfo(This,fEndPoint,pihvoRoot,cvlsi,prgvsli,ptagTextProp,pcpropPrevious,pich,pws,pfAssocPrev,ppttp)	\
    ( (This)->lpVtbl -> AllSelEndInfo(This,fEndPoint,pihvoRoot,cvlsi,prgvsli,ptagTextProp,pcpropPrevious,pich,pws,pfAssocPrev,ppttp) ) 

#define IVwSelection_CompleteEdits(This,pci,pfOk)	\
    ( (This)->lpVtbl -> CompleteEdits(This,pci,pfOk) ) 

#define IVwSelection_ExtendToStringBoundaries(This)	\
    ( (This)->lpVtbl -> ExtendToStringBoundaries(This) ) 

#define IVwSelection_get_EndBeforeAnchor(This,pfRet)	\
    ( (This)->lpVtbl -> get_EndBeforeAnchor(This,pfRet) ) 

#define IVwSelection_Location(This,pvg,rcSrc,rcDst,prdPrimary,prdSecondary,pfSplit,pfEndBeforeAnchor)	\
    ( (This)->lpVtbl -> Location(This,pvg,rcSrc,rcDst,prdPrimary,prdSecondary,pfSplit,pfEndBeforeAnchor) ) 

#define IVwSelection_GetParaLocation(This,prdLoc)	\
    ( (This)->lpVtbl -> GetParaLocation(This,prdLoc) ) 

#define IVwSelection_ReplaceWithTsString(This,ptss)	\
    ( (This)->lpVtbl -> ReplaceWithTsString(This,ptss) ) 

#define IVwSelection_GetSelectionString(This,pptss,bstrSep)	\
    ( (This)->lpVtbl -> GetSelectionString(This,pptss,bstrSep) ) 

#define IVwSelection_GetFirstParaString(This,pptss,bstrSep,pfGotItAll)	\
    ( (This)->lpVtbl -> GetFirstParaString(This,pptss,bstrSep,pfGotItAll) ) 

#define IVwSelection_SetIPLocation(This,fTopLine,xdPos)	\
    ( (This)->lpVtbl -> SetIPLocation(This,fTopLine,xdPos) ) 

#define IVwSelection_get_CanFormatPara(This,pfRet)	\
    ( (This)->lpVtbl -> get_CanFormatPara(This,pfRet) ) 

#define IVwSelection_get_CanFormatChar(This,pfRet)	\
    ( (This)->lpVtbl -> get_CanFormatChar(This,pfRet) ) 

#define IVwSelection_get_CanFormatOverlay(This,pfRet)	\
    ( (This)->lpVtbl -> get_CanFormatOverlay(This,pfRet) ) 

#define IVwSelection_Install(This)	\
    ( (This)->lpVtbl -> Install(This) ) 

#define IVwSelection_get_Follows(This,psel,pfFollows)	\
    ( (This)->lpVtbl -> get_Follows(This,psel,pfFollows) ) 

#define IVwSelection_get_IsValid(This,pfValid)	\
    ( (This)->lpVtbl -> get_IsValid(This,pfValid) ) 

#define IVwSelection_get_ParagraphOffset(This,fEndPoint,pich)	\
    ( (This)->lpVtbl -> get_ParagraphOffset(This,fEndPoint,pich) ) 

#define IVwSelection_get_AssocPrev(This,pfValue)	\
    ( (This)->lpVtbl -> get_AssocPrev(This,pfValue) ) 

#define IVwSelection_put_AssocPrev(This,fValue)	\
    ( (This)->lpVtbl -> put_AssocPrev(This,fValue) ) 

#define IVwSelection_get_SelType(This,piType)	\
    ( (This)->lpVtbl -> get_SelType(This,piType) ) 

#define IVwSelection_get_RootBox(This,pprootb)	\
    ( (This)->lpVtbl -> get_RootBox(This,pprootb) ) 

#define IVwSelection_GrowToWord(This,ppsel)	\
    ( (This)->lpVtbl -> GrowToWord(This,ppsel) ) 

#define IVwSelection_EndPoint(This,fEndPoint,ppsel)	\
    ( (This)->lpVtbl -> EndPoint(This,fEndPoint,ppsel) ) 

#define IVwSelection_SetTypingProps(This,pttp)	\
    ( (This)->lpVtbl -> SetTypingProps(This,pttp) ) 

#define IVwSelection_get_BoxDepth(This,fEndPoint,pcDepth)	\
    ( (This)->lpVtbl -> get_BoxDepth(This,fEndPoint,pcDepth) ) 

#define IVwSelection_get_BoxIndex(This,fEndPoint,iLevel,piAtLevel)	\
    ( (This)->lpVtbl -> get_BoxIndex(This,fEndPoint,iLevel,piAtLevel) ) 

#define IVwSelection_get_BoxCount(This,fEndPoint,iLevel,pcAtLevel)	\
    ( (This)->lpVtbl -> get_BoxCount(This,fEndPoint,iLevel,pcAtLevel) ) 

#define IVwSelection_get_BoxType(This,fEndPoint,iLevel,pvbt)	\
    ( (This)->lpVtbl -> get_BoxType(This,fEndPoint,iLevel,pvbt) ) 

#define IVwSelection_get_IsEditable(This,pfEditable)	\
    ( (This)->lpVtbl -> get_IsEditable(This,pfEditable) ) 

#define IVwSelection_get_IsEnabled(This,pfEnabled)	\
    ( (This)->lpVtbl -> get_IsEnabled(This,pfEnabled) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IVwSelection_INTERFACE_DEFINED__ */


#ifndef __IVwEmbeddedWindow_INTERFACE_DEFINED__
#define __IVwEmbeddedWindow_INTERFACE_DEFINED__

/* interface IVwEmbeddedWindow */
/* [unique][object][uuid] */ 


#define IID_IVwEmbeddedWindow __uuidof(IVwEmbeddedWindow)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("f6d10646-c00c-11d2-8078-0000c0fb81b5")
    IVwEmbeddedWindow : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE MoveWindow( 
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ int xdLeft,
            /* [in] */ int ydTop,
            /* [in] */ int dxdWidth,
            /* [in] */ int dydHeight) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsWindowVisible( 
            /* [retval][out] */ ComBool *pfRet) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ShowWindow( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DrawWindow( 
            /* [in] */ IVwGraphics *pvg) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Width( 
            /* [retval][out] */ int *pnTwips) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Height( 
            /* [retval][out] */ int *pnTwips) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IVwEmbeddedWindowVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IVwEmbeddedWindow * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IVwEmbeddedWindow * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IVwEmbeddedWindow * This);
        
        HRESULT ( STDMETHODCALLTYPE *MoveWindow )( 
            IVwEmbeddedWindow * This,
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ int xdLeft,
            /* [in] */ int ydTop,
            /* [in] */ int dxdWidth,
            /* [in] */ int dydHeight);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsWindowVisible )( 
            IVwEmbeddedWindow * This,
            /* [retval][out] */ ComBool *pfRet);
        
        HRESULT ( STDMETHODCALLTYPE *ShowWindow )( 
            IVwEmbeddedWindow * This);
        
        HRESULT ( STDMETHODCALLTYPE *DrawWindow )( 
            IVwEmbeddedWindow * This,
            /* [in] */ IVwGraphics *pvg);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Width )( 
            IVwEmbeddedWindow * This,
            /* [retval][out] */ int *pnTwips);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Height )( 
            IVwEmbeddedWindow * This,
            /* [retval][out] */ int *pnTwips);
        
        END_INTERFACE
    } IVwEmbeddedWindowVtbl;

    interface IVwEmbeddedWindow
    {
        CONST_VTBL struct IVwEmbeddedWindowVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IVwEmbeddedWindow_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IVwEmbeddedWindow_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IVwEmbeddedWindow_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IVwEmbeddedWindow_MoveWindow(This,pvg,xdLeft,ydTop,dxdWidth,dydHeight)	\
    ( (This)->lpVtbl -> MoveWindow(This,pvg,xdLeft,ydTop,dxdWidth,dydHeight) ) 

#define IVwEmbeddedWindow_get_IsWindowVisible(This,pfRet)	\
    ( (This)->lpVtbl -> get_IsWindowVisible(This,pfRet) ) 

#define IVwEmbeddedWindow_ShowWindow(This)	\
    ( (This)->lpVtbl -> ShowWindow(This) ) 

#define IVwEmbeddedWindow_DrawWindow(This,pvg)	\
    ( (This)->lpVtbl -> DrawWindow(This,pvg) ) 

#define IVwEmbeddedWindow_get_Width(This,pnTwips)	\
    ( (This)->lpVtbl -> get_Width(This,pnTwips) ) 

#define IVwEmbeddedWindow_get_Height(This,pnTwips)	\
    ( (This)->lpVtbl -> get_Height(This,pnTwips) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IVwEmbeddedWindow_INTERFACE_DEFINED__ */


#ifndef __IVwEnv_INTERFACE_DEFINED__
#define __IVwEnv_INTERFACE_DEFINED__

/* interface IVwEnv */
/* [unique][object][uuid] */ 


#define IID_IVwEnv __uuidof(IVwEnv)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("92B462E8-75D5-42c1-8B63-84878E8964C0")
    IVwEnv : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE AddObjProp( 
            /* [in] */ int tag,
            /* [in] */ IVwViewConstructor *pvwvc,
            /* [in] */ int frag) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AddObjVec( 
            /* [in] */ int tag,
            /* [in] */ IVwViewConstructor *pvwvc,
            /* [in] */ int frag) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AddObjVecItems( 
            /* [in] */ int tag,
            /* [in] */ IVwViewConstructor *pvwvc,
            /* [in] */ int frag) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AddReversedObjVecItems( 
            /* [in] */ int tag,
            /* [in] */ IVwViewConstructor *pvwvc,
            /* [in] */ int frag) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AddObj( 
            /* [in] */ HVO hvo,
            /* [in] */ IVwViewConstructor *pvwvc,
            /* [in] */ int frag) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AddLazyVecItems( 
            /* [in] */ int tag,
            /* [in] */ IVwViewConstructor *pvwvc,
            /* [in] */ int frag) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AddLazyItems( 
            /* [size_is][in] */ HVO *prghvo,
            /* [in] */ int chvo,
            /* [in] */ IVwViewConstructor *pvwvc,
            /* [in] */ int frag) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AddProp( 
            /* [in] */ int tag,
            /* [in] */ IVwViewConstructor *pvwvc,
            /* [in] */ int frag) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AddDerivedProp( 
            /* [size_is][in] */ int *prgtag,
            /* [in] */ int ctag,
            /* [in] */ IVwViewConstructor *pvwvc,
            /* [in] */ int frag) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE NoteDependency( 
            /* [size_is][in] */ HVO *prghvo,
            /* [size_is][in] */ PropTag *prgtag,
            /* [in] */ int chvo) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE NoteStringValDependency( 
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [in] */ int ws,
            /* [in] */ /* external definition not present */ ITsString *ptssVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AddStringProp( 
            /* [in] */ int tag,
            /* [in] */ IVwViewConstructor *pvwvc) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AddUnicodeProp( 
            /* [in] */ int tag,
            /* [in] */ int ws,
            /* [in] */ IVwViewConstructor *pvwvc) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AddIntProp( 
            /* [in] */ int tag) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AddIntPropPic( 
            /* [in] */ int tag,
            /* [in] */ IVwViewConstructor *pvc,
            /* [in] */ int frag,
            /* [in] */ int nMin,
            /* [in] */ int nMax) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AddStringAltMember( 
            /* [in] */ int tag,
            /* [in] */ int ws,
            /* [in] */ IVwViewConstructor *pvwvc) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AddStringAlt( 
            /* [in] */ int tag) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AddStringAltSeq( 
            /* [in] */ int tag,
            /* [size_is][in] */ int *prgenc,
            /* [in] */ int cws) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AddString( 
            /* [in] */ /* external definition not present */ ITsString *pss) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AddTimeProp( 
            /* [in] */ int tag,
            /* [in] */ DWORD flags) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CurrentObject( 
            /* [retval][out] */ HVO *phvo) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_OpenObject( 
            /* [retval][out] */ HVO *phvoRet) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_EmbeddingLevel( 
            /* [retval][out] */ int *pchvo) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetOuterObject( 
            /* [in] */ int ichvoLevel,
            /* [out] */ HVO *phvo,
            /* [out] */ int *ptag,
            /* [out] */ int *pihvo) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_DataAccess( 
            /* [retval][out] */ /* external definition not present */ ISilDataAccess **ppsda) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AddWindow( 
            /* [in] */ IVwEmbeddedWindow *pew,
            /* [in] */ int dmpAscent,
            /* [in] */ ComBool fJustifyRight,
            /* [in] */ ComBool fAutoShow) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AddSeparatorBar( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AddSimpleRect( 
            /* [in] */ int rgb,
            /* [in] */ int dmpWidth,
            /* [in] */ int dmpHeight,
            /* [in] */ int dmpBaselineOffset) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE OpenDiv( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CloseDiv( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE OpenParagraph( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE OpenTaggedPara( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE OpenMappedPara( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE OpenMappedTaggedPara( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE OpenConcPara( 
            /* [in] */ int ichMinItem,
            /* [in] */ int ichLimItem,
            /* [in] */ VwConcParaOpts cpoFlags,
            /* [in] */ int dmpAlign) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE OpenOverridePara( 
            /* [in] */ int cOverrideProperties,
            /* [size_is][in] */ DispPropOverride *prgOverrideProperties) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CloseParagraph( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE OpenInnerPile( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CloseInnerPile( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE OpenSpan( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CloseSpan( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE OpenTable( 
            /* [in] */ int cCols,
            /* [in] */ VwLength vlWidth,
            /* [in] */ int mpBorder,
            /* [in] */ VwAlignment vwalign,
            /* [in] */ VwFramePosition frmpos,
            /* [in] */ VwRule vwrule,
            /* [in] */ int mpSpacing,
            /* [in] */ int mpPadding,
            /* [in] */ ComBool fSelectOneCol) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CloseTable( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE OpenTableRow( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CloseTableRow( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE OpenTableCell( 
            /* [in] */ int nRowSpan,
            /* [in] */ int nColSpan) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CloseTableCell( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE OpenTableHeaderCell( 
            /* [in] */ int nRowSpan,
            /* [in] */ int nColSpan) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CloseTableHeaderCell( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE MakeColumns( 
            /* [in] */ int nColSpan,
            /* [in] */ VwLength vlWidth) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE MakeColumnGroup( 
            /* [in] */ int nColSpan,
            /* [in] */ VwLength vlWidth) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE OpenTableHeader( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CloseTableHeader( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE OpenTableFooter( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CloseTableFooter( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE OpenTableBody( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CloseTableBody( void) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_IntProperty( 
            /* [in] */ int tpt,
            /* [in] */ int tpv,
            /* [in] */ int nValue) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_StringProperty( 
            /* [in] */ int sp,
            /* [in] */ BSTR bstrValue) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_Props( 
            /* [in] */ /* external definition not present */ ITsTextProps *pttp) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE get_StringWidth( 
            /* [in] */ /* external definition not present */ ITsString *ptss,
            /* [in] */ /* external definition not present */ ITsTextProps *pttp,
            /* [out] */ int *dmpx,
            /* [out] */ int *dmpy) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AddPictureWithCaption( 
            /* [in] */ IPicture *ppict,
            /* [in] */ PropTag tag,
            /* [in] */ /* external definition not present */ ITsTextProps *pttpCaption,
            /* [in] */ HVO hvoCmFile,
            /* [in] */ int ws,
            /* [in] */ int dxmpWidth,
            /* [in] */ int dympHeight,
            /* [in] */ IVwViewConstructor *pvwvc) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AddPicture( 
            /* [in] */ IPicture *ppict,
            /* [in] */ PropTag tag,
            /* [in] */ int dxmpWidth,
            /* [in] */ int dympHeight) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetParagraphMark( 
            /* [in] */ VwBoundaryMark boundaryMark) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EmptyParagraphBehavior( 
            /* [in] */ int behavior) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE IsParagraphOpen( 
            /* [retval][out] */ ComBool *pfRet) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IVwEnvVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IVwEnv * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IVwEnv * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IVwEnv * This);
        
        HRESULT ( STDMETHODCALLTYPE *AddObjProp )( 
            IVwEnv * This,
            /* [in] */ int tag,
            /* [in] */ IVwViewConstructor *pvwvc,
            /* [in] */ int frag);
        
        HRESULT ( STDMETHODCALLTYPE *AddObjVec )( 
            IVwEnv * This,
            /* [in] */ int tag,
            /* [in] */ IVwViewConstructor *pvwvc,
            /* [in] */ int frag);
        
        HRESULT ( STDMETHODCALLTYPE *AddObjVecItems )( 
            IVwEnv * This,
            /* [in] */ int tag,
            /* [in] */ IVwViewConstructor *pvwvc,
            /* [in] */ int frag);
        
        HRESULT ( STDMETHODCALLTYPE *AddReversedObjVecItems )( 
            IVwEnv * This,
            /* [in] */ int tag,
            /* [in] */ IVwViewConstructor *pvwvc,
            /* [in] */ int frag);
        
        HRESULT ( STDMETHODCALLTYPE *AddObj )( 
            IVwEnv * This,
            /* [in] */ HVO hvo,
            /* [in] */ IVwViewConstructor *pvwvc,
            /* [in] */ int frag);
        
        HRESULT ( STDMETHODCALLTYPE *AddLazyVecItems )( 
            IVwEnv * This,
            /* [in] */ int tag,
            /* [in] */ IVwViewConstructor *pvwvc,
            /* [in] */ int frag);
        
        HRESULT ( STDMETHODCALLTYPE *AddLazyItems )( 
            IVwEnv * This,
            /* [size_is][in] */ HVO *prghvo,
            /* [in] */ int chvo,
            /* [in] */ IVwViewConstructor *pvwvc,
            /* [in] */ int frag);
        
        HRESULT ( STDMETHODCALLTYPE *AddProp )( 
            IVwEnv * This,
            /* [in] */ int tag,
            /* [in] */ IVwViewConstructor *pvwvc,
            /* [in] */ int frag);
        
        HRESULT ( STDMETHODCALLTYPE *AddDerivedProp )( 
            IVwEnv * This,
            /* [size_is][in] */ int *prgtag,
            /* [in] */ int ctag,
            /* [in] */ IVwViewConstructor *pvwvc,
            /* [in] */ int frag);
        
        HRESULT ( STDMETHODCALLTYPE *NoteDependency )( 
            IVwEnv * This,
            /* [size_is][in] */ HVO *prghvo,
            /* [size_is][in] */ PropTag *prgtag,
            /* [in] */ int chvo);
        
        HRESULT ( STDMETHODCALLTYPE *NoteStringValDependency )( 
            IVwEnv * This,
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [in] */ int ws,
            /* [in] */ /* external definition not present */ ITsString *ptssVal);
        
        HRESULT ( STDMETHODCALLTYPE *AddStringProp )( 
            IVwEnv * This,
            /* [in] */ int tag,
            /* [in] */ IVwViewConstructor *pvwvc);
        
        HRESULT ( STDMETHODCALLTYPE *AddUnicodeProp )( 
            IVwEnv * This,
            /* [in] */ int tag,
            /* [in] */ int ws,
            /* [in] */ IVwViewConstructor *pvwvc);
        
        HRESULT ( STDMETHODCALLTYPE *AddIntProp )( 
            IVwEnv * This,
            /* [in] */ int tag);
        
        HRESULT ( STDMETHODCALLTYPE *AddIntPropPic )( 
            IVwEnv * This,
            /* [in] */ int tag,
            /* [in] */ IVwViewConstructor *pvc,
            /* [in] */ int frag,
            /* [in] */ int nMin,
            /* [in] */ int nMax);
        
        HRESULT ( STDMETHODCALLTYPE *AddStringAltMember )( 
            IVwEnv * This,
            /* [in] */ int tag,
            /* [in] */ int ws,
            /* [in] */ IVwViewConstructor *pvwvc);
        
        HRESULT ( STDMETHODCALLTYPE *AddStringAlt )( 
            IVwEnv * This,
            /* [in] */ int tag);
        
        HRESULT ( STDMETHODCALLTYPE *AddStringAltSeq )( 
            IVwEnv * This,
            /* [in] */ int tag,
            /* [size_is][in] */ int *prgenc,
            /* [in] */ int cws);
        
        HRESULT ( STDMETHODCALLTYPE *AddString )( 
            IVwEnv * This,
            /* [in] */ /* external definition not present */ ITsString *pss);
        
        HRESULT ( STDMETHODCALLTYPE *AddTimeProp )( 
            IVwEnv * This,
            /* [in] */ int tag,
            /* [in] */ DWORD flags);
        
        HRESULT ( STDMETHODCALLTYPE *CurrentObject )( 
            IVwEnv * This,
            /* [retval][out] */ HVO *phvo);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_OpenObject )( 
            IVwEnv * This,
            /* [retval][out] */ HVO *phvoRet);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_EmbeddingLevel )( 
            IVwEnv * This,
            /* [retval][out] */ int *pchvo);
        
        HRESULT ( STDMETHODCALLTYPE *GetOuterObject )( 
            IVwEnv * This,
            /* [in] */ int ichvoLevel,
            /* [out] */ HVO *phvo,
            /* [out] */ int *ptag,
            /* [out] */ int *pihvo);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_DataAccess )( 
            IVwEnv * This,
            /* [retval][out] */ /* external definition not present */ ISilDataAccess **ppsda);
        
        HRESULT ( STDMETHODCALLTYPE *AddWindow )( 
            IVwEnv * This,
            /* [in] */ IVwEmbeddedWindow *pew,
            /* [in] */ int dmpAscent,
            /* [in] */ ComBool fJustifyRight,
            /* [in] */ ComBool fAutoShow);
        
        HRESULT ( STDMETHODCALLTYPE *AddSeparatorBar )( 
            IVwEnv * This);
        
        HRESULT ( STDMETHODCALLTYPE *AddSimpleRect )( 
            IVwEnv * This,
            /* [in] */ int rgb,
            /* [in] */ int dmpWidth,
            /* [in] */ int dmpHeight,
            /* [in] */ int dmpBaselineOffset);
        
        HRESULT ( STDMETHODCALLTYPE *OpenDiv )( 
            IVwEnv * This);
        
        HRESULT ( STDMETHODCALLTYPE *CloseDiv )( 
            IVwEnv * This);
        
        HRESULT ( STDMETHODCALLTYPE *OpenParagraph )( 
            IVwEnv * This);
        
        HRESULT ( STDMETHODCALLTYPE *OpenTaggedPara )( 
            IVwEnv * This);
        
        HRESULT ( STDMETHODCALLTYPE *OpenMappedPara )( 
            IVwEnv * This);
        
        HRESULT ( STDMETHODCALLTYPE *OpenMappedTaggedPara )( 
            IVwEnv * This);
        
        HRESULT ( STDMETHODCALLTYPE *OpenConcPara )( 
            IVwEnv * This,
            /* [in] */ int ichMinItem,
            /* [in] */ int ichLimItem,
            /* [in] */ VwConcParaOpts cpoFlags,
            /* [in] */ int dmpAlign);
        
        HRESULT ( STDMETHODCALLTYPE *OpenOverridePara )( 
            IVwEnv * This,
            /* [in] */ int cOverrideProperties,
            /* [size_is][in] */ DispPropOverride *prgOverrideProperties);
        
        HRESULT ( STDMETHODCALLTYPE *CloseParagraph )( 
            IVwEnv * This);
        
        HRESULT ( STDMETHODCALLTYPE *OpenInnerPile )( 
            IVwEnv * This);
        
        HRESULT ( STDMETHODCALLTYPE *CloseInnerPile )( 
            IVwEnv * This);
        
        HRESULT ( STDMETHODCALLTYPE *OpenSpan )( 
            IVwEnv * This);
        
        HRESULT ( STDMETHODCALLTYPE *CloseSpan )( 
            IVwEnv * This);
        
        HRESULT ( STDMETHODCALLTYPE *OpenTable )( 
            IVwEnv * This,
            /* [in] */ int cCols,
            /* [in] */ VwLength vlWidth,
            /* [in] */ int mpBorder,
            /* [in] */ VwAlignment vwalign,
            /* [in] */ VwFramePosition frmpos,
            /* [in] */ VwRule vwrule,
            /* [in] */ int mpSpacing,
            /* [in] */ int mpPadding,
            /* [in] */ ComBool fSelectOneCol);
        
        HRESULT ( STDMETHODCALLTYPE *CloseTable )( 
            IVwEnv * This);
        
        HRESULT ( STDMETHODCALLTYPE *OpenTableRow )( 
            IVwEnv * This);
        
        HRESULT ( STDMETHODCALLTYPE *CloseTableRow )( 
            IVwEnv * This);
        
        HRESULT ( STDMETHODCALLTYPE *OpenTableCell )( 
            IVwEnv * This,
            /* [in] */ int nRowSpan,
            /* [in] */ int nColSpan);
        
        HRESULT ( STDMETHODCALLTYPE *CloseTableCell )( 
            IVwEnv * This);
        
        HRESULT ( STDMETHODCALLTYPE *OpenTableHeaderCell )( 
            IVwEnv * This,
            /* [in] */ int nRowSpan,
            /* [in] */ int nColSpan);
        
        HRESULT ( STDMETHODCALLTYPE *CloseTableHeaderCell )( 
            IVwEnv * This);
        
        HRESULT ( STDMETHODCALLTYPE *MakeColumns )( 
            IVwEnv * This,
            /* [in] */ int nColSpan,
            /* [in] */ VwLength vlWidth);
        
        HRESULT ( STDMETHODCALLTYPE *MakeColumnGroup )( 
            IVwEnv * This,
            /* [in] */ int nColSpan,
            /* [in] */ VwLength vlWidth);
        
        HRESULT ( STDMETHODCALLTYPE *OpenTableHeader )( 
            IVwEnv * This);
        
        HRESULT ( STDMETHODCALLTYPE *CloseTableHeader )( 
            IVwEnv * This);
        
        HRESULT ( STDMETHODCALLTYPE *OpenTableFooter )( 
            IVwEnv * This);
        
        HRESULT ( STDMETHODCALLTYPE *CloseTableFooter )( 
            IVwEnv * This);
        
        HRESULT ( STDMETHODCALLTYPE *OpenTableBody )( 
            IVwEnv * This);
        
        HRESULT ( STDMETHODCALLTYPE *CloseTableBody )( 
            IVwEnv * This);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_IntProperty )( 
            IVwEnv * This,
            /* [in] */ int tpt,
            /* [in] */ int tpv,
            /* [in] */ int nValue);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_StringProperty )( 
            IVwEnv * This,
            /* [in] */ int sp,
            /* [in] */ BSTR bstrValue);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_Props )( 
            IVwEnv * This,
            /* [in] */ /* external definition not present */ ITsTextProps *pttp);
        
        HRESULT ( STDMETHODCALLTYPE *get_StringWidth )( 
            IVwEnv * This,
            /* [in] */ /* external definition not present */ ITsString *ptss,
            /* [in] */ /* external definition not present */ ITsTextProps *pttp,
            /* [out] */ int *dmpx,
            /* [out] */ int *dmpy);
        
        HRESULT ( STDMETHODCALLTYPE *AddPictureWithCaption )( 
            IVwEnv * This,
            /* [in] */ IPicture *ppict,
            /* [in] */ PropTag tag,
            /* [in] */ /* external definition not present */ ITsTextProps *pttpCaption,
            /* [in] */ HVO hvoCmFile,
            /* [in] */ int ws,
            /* [in] */ int dxmpWidth,
            /* [in] */ int dympHeight,
            /* [in] */ IVwViewConstructor *pvwvc);
        
        HRESULT ( STDMETHODCALLTYPE *AddPicture )( 
            IVwEnv * This,
            /* [in] */ IPicture *ppict,
            /* [in] */ PropTag tag,
            /* [in] */ int dxmpWidth,
            /* [in] */ int dympHeight);
        
        HRESULT ( STDMETHODCALLTYPE *SetParagraphMark )( 
            IVwEnv * This,
            /* [in] */ VwBoundaryMark boundaryMark);
        
        HRESULT ( STDMETHODCALLTYPE *EmptyParagraphBehavior )( 
            IVwEnv * This,
            /* [in] */ int behavior);
        
        HRESULT ( STDMETHODCALLTYPE *IsParagraphOpen )( 
            IVwEnv * This,
            /* [retval][out] */ ComBool *pfRet);
        
        END_INTERFACE
    } IVwEnvVtbl;

    interface IVwEnv
    {
        CONST_VTBL struct IVwEnvVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IVwEnv_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IVwEnv_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IVwEnv_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IVwEnv_AddObjProp(This,tag,pvwvc,frag)	\
    ( (This)->lpVtbl -> AddObjProp(This,tag,pvwvc,frag) ) 

#define IVwEnv_AddObjVec(This,tag,pvwvc,frag)	\
    ( (This)->lpVtbl -> AddObjVec(This,tag,pvwvc,frag) ) 

#define IVwEnv_AddObjVecItems(This,tag,pvwvc,frag)	\
    ( (This)->lpVtbl -> AddObjVecItems(This,tag,pvwvc,frag) ) 

#define IVwEnv_AddReversedObjVecItems(This,tag,pvwvc,frag)	\
    ( (This)->lpVtbl -> AddReversedObjVecItems(This,tag,pvwvc,frag) ) 

#define IVwEnv_AddObj(This,hvo,pvwvc,frag)	\
    ( (This)->lpVtbl -> AddObj(This,hvo,pvwvc,frag) ) 

#define IVwEnv_AddLazyVecItems(This,tag,pvwvc,frag)	\
    ( (This)->lpVtbl -> AddLazyVecItems(This,tag,pvwvc,frag) ) 

#define IVwEnv_AddLazyItems(This,prghvo,chvo,pvwvc,frag)	\
    ( (This)->lpVtbl -> AddLazyItems(This,prghvo,chvo,pvwvc,frag) ) 

#define IVwEnv_AddProp(This,tag,pvwvc,frag)	\
    ( (This)->lpVtbl -> AddProp(This,tag,pvwvc,frag) ) 

#define IVwEnv_AddDerivedProp(This,prgtag,ctag,pvwvc,frag)	\
    ( (This)->lpVtbl -> AddDerivedProp(This,prgtag,ctag,pvwvc,frag) ) 

#define IVwEnv_NoteDependency(This,prghvo,prgtag,chvo)	\
    ( (This)->lpVtbl -> NoteDependency(This,prghvo,prgtag,chvo) ) 

#define IVwEnv_NoteStringValDependency(This,hvo,tag,ws,ptssVal)	\
    ( (This)->lpVtbl -> NoteStringValDependency(This,hvo,tag,ws,ptssVal) ) 

#define IVwEnv_AddStringProp(This,tag,pvwvc)	\
    ( (This)->lpVtbl -> AddStringProp(This,tag,pvwvc) ) 

#define IVwEnv_AddUnicodeProp(This,tag,ws,pvwvc)	\
    ( (This)->lpVtbl -> AddUnicodeProp(This,tag,ws,pvwvc) ) 

#define IVwEnv_AddIntProp(This,tag)	\
    ( (This)->lpVtbl -> AddIntProp(This,tag) ) 

#define IVwEnv_AddIntPropPic(This,tag,pvc,frag,nMin,nMax)	\
    ( (This)->lpVtbl -> AddIntPropPic(This,tag,pvc,frag,nMin,nMax) ) 

#define IVwEnv_AddStringAltMember(This,tag,ws,pvwvc)	\
    ( (This)->lpVtbl -> AddStringAltMember(This,tag,ws,pvwvc) ) 

#define IVwEnv_AddStringAlt(This,tag)	\
    ( (This)->lpVtbl -> AddStringAlt(This,tag) ) 

#define IVwEnv_AddStringAltSeq(This,tag,prgenc,cws)	\
    ( (This)->lpVtbl -> AddStringAltSeq(This,tag,prgenc,cws) ) 

#define IVwEnv_AddString(This,pss)	\
    ( (This)->lpVtbl -> AddString(This,pss) ) 

#define IVwEnv_AddTimeProp(This,tag,flags)	\
    ( (This)->lpVtbl -> AddTimeProp(This,tag,flags) ) 

#define IVwEnv_CurrentObject(This,phvo)	\
    ( (This)->lpVtbl -> CurrentObject(This,phvo) ) 

#define IVwEnv_get_OpenObject(This,phvoRet)	\
    ( (This)->lpVtbl -> get_OpenObject(This,phvoRet) ) 

#define IVwEnv_get_EmbeddingLevel(This,pchvo)	\
    ( (This)->lpVtbl -> get_EmbeddingLevel(This,pchvo) ) 

#define IVwEnv_GetOuterObject(This,ichvoLevel,phvo,ptag,pihvo)	\
    ( (This)->lpVtbl -> GetOuterObject(This,ichvoLevel,phvo,ptag,pihvo) ) 

#define IVwEnv_get_DataAccess(This,ppsda)	\
    ( (This)->lpVtbl -> get_DataAccess(This,ppsda) ) 

#define IVwEnv_AddWindow(This,pew,dmpAscent,fJustifyRight,fAutoShow)	\
    ( (This)->lpVtbl -> AddWindow(This,pew,dmpAscent,fJustifyRight,fAutoShow) ) 

#define IVwEnv_AddSeparatorBar(This)	\
    ( (This)->lpVtbl -> AddSeparatorBar(This) ) 

#define IVwEnv_AddSimpleRect(This,rgb,dmpWidth,dmpHeight,dmpBaselineOffset)	\
    ( (This)->lpVtbl -> AddSimpleRect(This,rgb,dmpWidth,dmpHeight,dmpBaselineOffset) ) 

#define IVwEnv_OpenDiv(This)	\
    ( (This)->lpVtbl -> OpenDiv(This) ) 

#define IVwEnv_CloseDiv(This)	\
    ( (This)->lpVtbl -> CloseDiv(This) ) 

#define IVwEnv_OpenParagraph(This)	\
    ( (This)->lpVtbl -> OpenParagraph(This) ) 

#define IVwEnv_OpenTaggedPara(This)	\
    ( (This)->lpVtbl -> OpenTaggedPara(This) ) 

#define IVwEnv_OpenMappedPara(This)	\
    ( (This)->lpVtbl -> OpenMappedPara(This) ) 

#define IVwEnv_OpenMappedTaggedPara(This)	\
    ( (This)->lpVtbl -> OpenMappedTaggedPara(This) ) 

#define IVwEnv_OpenConcPara(This,ichMinItem,ichLimItem,cpoFlags,dmpAlign)	\
    ( (This)->lpVtbl -> OpenConcPara(This,ichMinItem,ichLimItem,cpoFlags,dmpAlign) ) 

#define IVwEnv_OpenOverridePara(This,cOverrideProperties,prgOverrideProperties)	\
    ( (This)->lpVtbl -> OpenOverridePara(This,cOverrideProperties,prgOverrideProperties) ) 

#define IVwEnv_CloseParagraph(This)	\
    ( (This)->lpVtbl -> CloseParagraph(This) ) 

#define IVwEnv_OpenInnerPile(This)	\
    ( (This)->lpVtbl -> OpenInnerPile(This) ) 

#define IVwEnv_CloseInnerPile(This)	\
    ( (This)->lpVtbl -> CloseInnerPile(This) ) 

#define IVwEnv_OpenSpan(This)	\
    ( (This)->lpVtbl -> OpenSpan(This) ) 

#define IVwEnv_CloseSpan(This)	\
    ( (This)->lpVtbl -> CloseSpan(This) ) 

#define IVwEnv_OpenTable(This,cCols,vlWidth,mpBorder,vwalign,frmpos,vwrule,mpSpacing,mpPadding,fSelectOneCol)	\
    ( (This)->lpVtbl -> OpenTable(This,cCols,vlWidth,mpBorder,vwalign,frmpos,vwrule,mpSpacing,mpPadding,fSelectOneCol) ) 

#define IVwEnv_CloseTable(This)	\
    ( (This)->lpVtbl -> CloseTable(This) ) 

#define IVwEnv_OpenTableRow(This)	\
    ( (This)->lpVtbl -> OpenTableRow(This) ) 

#define IVwEnv_CloseTableRow(This)	\
    ( (This)->lpVtbl -> CloseTableRow(This) ) 

#define IVwEnv_OpenTableCell(This,nRowSpan,nColSpan)	\
    ( (This)->lpVtbl -> OpenTableCell(This,nRowSpan,nColSpan) ) 

#define IVwEnv_CloseTableCell(This)	\
    ( (This)->lpVtbl -> CloseTableCell(This) ) 

#define IVwEnv_OpenTableHeaderCell(This,nRowSpan,nColSpan)	\
    ( (This)->lpVtbl -> OpenTableHeaderCell(This,nRowSpan,nColSpan) ) 

#define IVwEnv_CloseTableHeaderCell(This)	\
    ( (This)->lpVtbl -> CloseTableHeaderCell(This) ) 

#define IVwEnv_MakeColumns(This,nColSpan,vlWidth)	\
    ( (This)->lpVtbl -> MakeColumns(This,nColSpan,vlWidth) ) 

#define IVwEnv_MakeColumnGroup(This,nColSpan,vlWidth)	\
    ( (This)->lpVtbl -> MakeColumnGroup(This,nColSpan,vlWidth) ) 

#define IVwEnv_OpenTableHeader(This)	\
    ( (This)->lpVtbl -> OpenTableHeader(This) ) 

#define IVwEnv_CloseTableHeader(This)	\
    ( (This)->lpVtbl -> CloseTableHeader(This) ) 

#define IVwEnv_OpenTableFooter(This)	\
    ( (This)->lpVtbl -> OpenTableFooter(This) ) 

#define IVwEnv_CloseTableFooter(This)	\
    ( (This)->lpVtbl -> CloseTableFooter(This) ) 

#define IVwEnv_OpenTableBody(This)	\
    ( (This)->lpVtbl -> OpenTableBody(This) ) 

#define IVwEnv_CloseTableBody(This)	\
    ( (This)->lpVtbl -> CloseTableBody(This) ) 

#define IVwEnv_put_IntProperty(This,tpt,tpv,nValue)	\
    ( (This)->lpVtbl -> put_IntProperty(This,tpt,tpv,nValue) ) 

#define IVwEnv_put_StringProperty(This,sp,bstrValue)	\
    ( (This)->lpVtbl -> put_StringProperty(This,sp,bstrValue) ) 

#define IVwEnv_put_Props(This,pttp)	\
    ( (This)->lpVtbl -> put_Props(This,pttp) ) 

#define IVwEnv_get_StringWidth(This,ptss,pttp,dmpx,dmpy)	\
    ( (This)->lpVtbl -> get_StringWidth(This,ptss,pttp,dmpx,dmpy) ) 

#define IVwEnv_AddPictureWithCaption(This,ppict,tag,pttpCaption,hvoCmFile,ws,dxmpWidth,dympHeight,pvwvc)	\
    ( (This)->lpVtbl -> AddPictureWithCaption(This,ppict,tag,pttpCaption,hvoCmFile,ws,dxmpWidth,dympHeight,pvwvc) ) 

#define IVwEnv_AddPicture(This,ppict,tag,dxmpWidth,dympHeight)	\
    ( (This)->lpVtbl -> AddPicture(This,ppict,tag,dxmpWidth,dympHeight) ) 

#define IVwEnv_SetParagraphMark(This,boundaryMark)	\
    ( (This)->lpVtbl -> SetParagraphMark(This,boundaryMark) ) 

#define IVwEnv_EmptyParagraphBehavior(This,behavior)	\
    ( (This)->lpVtbl -> EmptyParagraphBehavior(This,behavior) ) 

#define IVwEnv_IsParagraphOpen(This,pfRet)	\
    ( (This)->lpVtbl -> IsParagraphOpen(This,pfRet) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IVwEnv_INTERFACE_DEFINED__ */


#ifndef __IVwViewConstructor_INTERFACE_DEFINED__
#define __IVwViewConstructor_INTERFACE_DEFINED__

/* interface IVwViewConstructor */
/* [unique][object][uuid] */ 


#define IID_IVwViewConstructor __uuidof(IVwViewConstructor)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("5b1a08f6-9af9-46f9-9fd7-1011a3039191")
    IVwViewConstructor : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Display( 
            /* [in] */ IVwEnv *pvwenv,
            /* [in] */ HVO hvo,
            /* [in] */ int frag) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DisplayVec( 
            /* [in] */ IVwEnv *pvwenv,
            /* [in] */ HVO hvo,
            /* [in] */ int tag,
            /* [in] */ int frag) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DisplayVariant( 
            /* [in] */ IVwEnv *pvwenv,
            /* [in] */ int tag,
            /* [in] */ int frag,
            /* [retval][out] */ /* external definition not present */ ITsString **pptss) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DisplayPicture( 
            /* [in] */ IVwEnv *pvwenv,
            /* [in] */ int hvo,
            /* [in] */ int tag,
            /* [in] */ int val,
            /* [in] */ int frag,
            /* [retval][out] */ IPicture **ppPict) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE UpdateProp( 
            /* [in] */ IVwSelection *pvwsel,
            /* [in] */ HVO hvo,
            /* [in] */ int tag,
            /* [in] */ int frag,
            /* [in] */ /* external definition not present */ ITsString *ptssVal,
            /* [retval][out] */ /* external definition not present */ ITsString **pptssRepVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EstimateHeight( 
            /* [in] */ HVO hvo,
            /* [in] */ int frag,
            /* [in] */ int dxAvailWidth,
            /* [retval][out] */ int *pdyHeight) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE LoadDataFor( 
            /* [in] */ IVwEnv *pvwenv,
            /* [size_is][in] */ HVO *prghvo,
            /* [in] */ int chvo,
            /* [in] */ HVO hvoParent,
            /* [in] */ int tag,
            /* [in] */ int frag,
            /* [in] */ int ihvoMin) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetStrForGuid( 
            /* [in] */ BSTR bstrGuid,
            /* [retval][out] */ /* external definition not present */ ITsString **pptss) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DoHotLinkAction( 
            /* [in] */ BSTR bstrData,
            /* [in] */ /* external definition not present */ ISilDataAccess *psda) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetIdFromGuid( 
            /* [in] */ /* external definition not present */ ISilDataAccess *psda,
            /* [in] */ GUID *puid,
            /* [retval][out] */ HVO *phvo) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DisplayEmbeddedObject( 
            /* [in] */ IVwEnv *pvwenv,
            /* [in] */ HVO hvo) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE UpdateRootBoxTextProps( 
            /* [in] */ /* external definition not present */ ITsTextProps *pttp,
            /* [retval][out] */ /* external definition not present */ ITsTextProps **ppttp) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IVwViewConstructorVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IVwViewConstructor * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IVwViewConstructor * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IVwViewConstructor * This);
        
        HRESULT ( STDMETHODCALLTYPE *Display )( 
            IVwViewConstructor * This,
            /* [in] */ IVwEnv *pvwenv,
            /* [in] */ HVO hvo,
            /* [in] */ int frag);
        
        HRESULT ( STDMETHODCALLTYPE *DisplayVec )( 
            IVwViewConstructor * This,
            /* [in] */ IVwEnv *pvwenv,
            /* [in] */ HVO hvo,
            /* [in] */ int tag,
            /* [in] */ int frag);
        
        HRESULT ( STDMETHODCALLTYPE *DisplayVariant )( 
            IVwViewConstructor * This,
            /* [in] */ IVwEnv *pvwenv,
            /* [in] */ int tag,
            /* [in] */ int frag,
            /* [retval][out] */ /* external definition not present */ ITsString **pptss);
        
        HRESULT ( STDMETHODCALLTYPE *DisplayPicture )( 
            IVwViewConstructor * This,
            /* [in] */ IVwEnv *pvwenv,
            /* [in] */ int hvo,
            /* [in] */ int tag,
            /* [in] */ int val,
            /* [in] */ int frag,
            /* [retval][out] */ IPicture **ppPict);
        
        HRESULT ( STDMETHODCALLTYPE *UpdateProp )( 
            IVwViewConstructor * This,
            /* [in] */ IVwSelection *pvwsel,
            /* [in] */ HVO hvo,
            /* [in] */ int tag,
            /* [in] */ int frag,
            /* [in] */ /* external definition not present */ ITsString *ptssVal,
            /* [retval][out] */ /* external definition not present */ ITsString **pptssRepVal);
        
        HRESULT ( STDMETHODCALLTYPE *EstimateHeight )( 
            IVwViewConstructor * This,
            /* [in] */ HVO hvo,
            /* [in] */ int frag,
            /* [in] */ int dxAvailWidth,
            /* [retval][out] */ int *pdyHeight);
        
        HRESULT ( STDMETHODCALLTYPE *LoadDataFor )( 
            IVwViewConstructor * This,
            /* [in] */ IVwEnv *pvwenv,
            /* [size_is][in] */ HVO *prghvo,
            /* [in] */ int chvo,
            /* [in] */ HVO hvoParent,
            /* [in] */ int tag,
            /* [in] */ int frag,
            /* [in] */ int ihvoMin);
        
        HRESULT ( STDMETHODCALLTYPE *GetStrForGuid )( 
            IVwViewConstructor * This,
            /* [in] */ BSTR bstrGuid,
            /* [retval][out] */ /* external definition not present */ ITsString **pptss);
        
        HRESULT ( STDMETHODCALLTYPE *DoHotLinkAction )( 
            IVwViewConstructor * This,
            /* [in] */ BSTR bstrData,
            /* [in] */ /* external definition not present */ ISilDataAccess *psda);
        
        HRESULT ( STDMETHODCALLTYPE *GetIdFromGuid )( 
            IVwViewConstructor * This,
            /* [in] */ /* external definition not present */ ISilDataAccess *psda,
            /* [in] */ GUID *puid,
            /* [retval][out] */ HVO *phvo);
        
        HRESULT ( STDMETHODCALLTYPE *DisplayEmbeddedObject )( 
            IVwViewConstructor * This,
            /* [in] */ IVwEnv *pvwenv,
            /* [in] */ HVO hvo);
        
        HRESULT ( STDMETHODCALLTYPE *UpdateRootBoxTextProps )( 
            IVwViewConstructor * This,
            /* [in] */ /* external definition not present */ ITsTextProps *pttp,
            /* [retval][out] */ /* external definition not present */ ITsTextProps **ppttp);
        
        END_INTERFACE
    } IVwViewConstructorVtbl;

    interface IVwViewConstructor
    {
        CONST_VTBL struct IVwViewConstructorVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IVwViewConstructor_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IVwViewConstructor_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IVwViewConstructor_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IVwViewConstructor_Display(This,pvwenv,hvo,frag)	\
    ( (This)->lpVtbl -> Display(This,pvwenv,hvo,frag) ) 

#define IVwViewConstructor_DisplayVec(This,pvwenv,hvo,tag,frag)	\
    ( (This)->lpVtbl -> DisplayVec(This,pvwenv,hvo,tag,frag) ) 

#define IVwViewConstructor_DisplayVariant(This,pvwenv,tag,frag,pptss)	\
    ( (This)->lpVtbl -> DisplayVariant(This,pvwenv,tag,frag,pptss) ) 

#define IVwViewConstructor_DisplayPicture(This,pvwenv,hvo,tag,val,frag,ppPict)	\
    ( (This)->lpVtbl -> DisplayPicture(This,pvwenv,hvo,tag,val,frag,ppPict) ) 

#define IVwViewConstructor_UpdateProp(This,pvwsel,hvo,tag,frag,ptssVal,pptssRepVal)	\
    ( (This)->lpVtbl -> UpdateProp(This,pvwsel,hvo,tag,frag,ptssVal,pptssRepVal) ) 

#define IVwViewConstructor_EstimateHeight(This,hvo,frag,dxAvailWidth,pdyHeight)	\
    ( (This)->lpVtbl -> EstimateHeight(This,hvo,frag,dxAvailWidth,pdyHeight) ) 

#define IVwViewConstructor_LoadDataFor(This,pvwenv,prghvo,chvo,hvoParent,tag,frag,ihvoMin)	\
    ( (This)->lpVtbl -> LoadDataFor(This,pvwenv,prghvo,chvo,hvoParent,tag,frag,ihvoMin) ) 

#define IVwViewConstructor_GetStrForGuid(This,bstrGuid,pptss)	\
    ( (This)->lpVtbl -> GetStrForGuid(This,bstrGuid,pptss) ) 

#define IVwViewConstructor_DoHotLinkAction(This,bstrData,psda)	\
    ( (This)->lpVtbl -> DoHotLinkAction(This,bstrData,psda) ) 

#define IVwViewConstructor_GetIdFromGuid(This,psda,puid,phvo)	\
    ( (This)->lpVtbl -> GetIdFromGuid(This,psda,puid,phvo) ) 

#define IVwViewConstructor_DisplayEmbeddedObject(This,pvwenv,hvo)	\
    ( (This)->lpVtbl -> DisplayEmbeddedObject(This,pvwenv,hvo) ) 

#define IVwViewConstructor_UpdateRootBoxTextProps(This,pttp,ppttp)	\
    ( (This)->lpVtbl -> UpdateRootBoxTextProps(This,pttp,ppttp) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IVwViewConstructor_INTERFACE_DEFINED__ */


#ifndef __IVwRootSite_INTERFACE_DEFINED__
#define __IVwRootSite_INTERFACE_DEFINED__

/* interface IVwRootSite */
/* [unique][object][uuid] */ 


#define IID_IVwRootSite __uuidof(IVwRootSite)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("C999413C-28C8-481c-9543-B06C92B812D1")
    IVwRootSite : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE InvalidateRect( 
            /* [in] */ IVwRootBox *pRoot,
            /* [in] */ int xsLeft,
            /* [in] */ int ysTop,
            /* [in] */ int dxsWidth,
            /* [in] */ int dysHeight) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetGraphics( 
            /* [in] */ IVwRootBox *pRoot,
            /* [out] */ IVwGraphics **ppvg,
            /* [out] */ RECT *prcSrcRoot,
            /* [out] */ RECT *prcDstRoot) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_LayoutGraphics( 
            /* [in] */ IVwRootBox *pRoot,
            /* [retval][out] */ IVwGraphics **ppvg) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ScreenGraphics( 
            /* [in] */ IVwRootBox *pRoot,
            /* [retval][out] */ IVwGraphics **ppvg) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetTransformAtDst( 
            /* [in] */ IVwRootBox *pRoot,
            /* [in] */ POINT pt,
            /* [out] */ RECT *prcSrcRoot,
            /* [out] */ RECT *prcDstRoot) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetTransformAtSrc( 
            /* [in] */ IVwRootBox *pRoot,
            /* [in] */ POINT pt,
            /* [out] */ RECT *prcSrcRoot,
            /* [out] */ RECT *prcDstRoot) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ReleaseGraphics( 
            /* [in] */ IVwRootBox *pRoot,
            /* [in] */ IVwGraphics *pvg) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetAvailWidth( 
            /* [in] */ IVwRootBox *pRoot,
            /* [retval][out] */ int *ptwWidth) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DoUpdates( 
            /* [in] */ IVwRootBox *pRoot) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE RootBoxSizeChanged( 
            /* [in] */ IVwRootBox *pRoot) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AdjustScrollRange( 
            /* [in] */ IVwRootBox *pRoot,
            /* [in] */ int dxdSize,
            /* [in] */ int dxdPosition,
            /* [in] */ int dydSize,
            /* [in] */ int dydPosition,
            /* [retval][out] */ ComBool *pfForcedScroll) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SelectionChanged( 
            /* [in] */ IVwRootBox *pRoot,
            /* [in] */ IVwSelection *pvwselNew) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE OverlayChanged( 
            /* [in] */ IVwRootBox *pRoot,
            /* [in] */ IVwOverlay *pvo) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_SemiTagging( 
            /* [in] */ IVwRootBox *pRoot,
            /* [retval][out] */ ComBool *pf) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ScreenToClient( 
            /* [in] */ IVwRootBox *pRoot,
            /* [out][in] */ POINT *ppnt) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ClientToScreen( 
            /* [in] */ IVwRootBox *pRoot,
            /* [out][in] */ POINT *ppnt) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetAndClearPendingWs( 
            /* [in] */ IVwRootBox *pRoot,
            /* [retval][out] */ int *pws) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE IsOkToMakeLazy( 
            /* [in] */ IVwRootBox *pRoot,
            /* [in] */ int ydTop,
            /* [in] */ int ydBottom,
            /* [retval][out] */ ComBool *pfOK) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE OnProblemDeletion( 
            /* [in] */ IVwSelection *psel,
            /* [in] */ VwDelProbType dpt,
            /* [retval][out] */ VwDelProbResponse *pdpr) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE OnInsertDiffParas( 
            /* [in] */ IVwRootBox *pRoot,
            /* [in] */ /* external definition not present */ ITsTextProps *pttpDest,
            /* [in] */ int cPara,
            /* [size_is][in] */ /* external definition not present */ ITsTextProps **prgpttpSrc,
            /* [size_is][in] */ /* external definition not present */ ITsString **prgptssSrc,
            /* [in] */ /* external definition not present */ ITsString *ptssTrailing,
            /* [retval][out] */ VwInsertDiffParaResponse *pidpr) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE OnInsertDiffPara( 
            /* [in] */ IVwRootBox *pRoot,
            /* [in] */ /* external definition not present */ ITsTextProps *pttpDest,
            /* [in] */ /* external definition not present */ ITsTextProps *prgpttpSrc,
            /* [in] */ /* external definition not present */ ITsString *prgptssSrc,
            /* [in] */ /* external definition not present */ ITsString *ptssTrailing,
            /* [retval][out] */ VwInsertDiffParaResponse *pidpr) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_TextRepOfObj( 
            /* [in] */ GUID *pguid,
            /* [retval][out] */ BSTR *pbstrRep) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_MakeObjFromText( 
            /* [in] */ BSTR bstrText,
            /* [in] */ IVwSelection *pselDst,
            /* [out] */ int *podt,
            /* [retval][out] */ GUID *pGuid) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ScrollSelectionIntoView( 
            /* [in] */ IVwSelection *psel,
            /* [in] */ VwScrollSelOpts ssoFlag,
            /* [retval][out] */ ComBool *pfRet) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_RootBox( 
            /* [retval][out] */ IVwRootBox **prootb) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Hwnd( 
            /* [retval][out] */ DWORD *phwnd) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE RequestSelectionAtEndOfUow( 
            /* [in] */ IVwRootBox *prootb,
            /* [in] */ int ihvoRoot,
            /* [in] */ int cvlsi,
            /* [size_is][in] */ VwSelLevInfo *prgvsli,
            /* [in] */ int tagTextProp,
            /* [in] */ int cpropPrevious,
            /* [in] */ int ich,
            /* [in] */ int wsAlt,
            /* [in] */ ComBool fAssocPrev,
            /* [in] */ /* external definition not present */ ITsTextProps *selProps) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IVwRootSiteVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IVwRootSite * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IVwRootSite * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IVwRootSite * This);
        
        HRESULT ( STDMETHODCALLTYPE *InvalidateRect )( 
            IVwRootSite * This,
            /* [in] */ IVwRootBox *pRoot,
            /* [in] */ int xsLeft,
            /* [in] */ int ysTop,
            /* [in] */ int dxsWidth,
            /* [in] */ int dysHeight);
        
        HRESULT ( STDMETHODCALLTYPE *GetGraphics )( 
            IVwRootSite * This,
            /* [in] */ IVwRootBox *pRoot,
            /* [out] */ IVwGraphics **ppvg,
            /* [out] */ RECT *prcSrcRoot,
            /* [out] */ RECT *prcDstRoot);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_LayoutGraphics )( 
            IVwRootSite * This,
            /* [in] */ IVwRootBox *pRoot,
            /* [retval][out] */ IVwGraphics **ppvg);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ScreenGraphics )( 
            IVwRootSite * This,
            /* [in] */ IVwRootBox *pRoot,
            /* [retval][out] */ IVwGraphics **ppvg);
        
        HRESULT ( STDMETHODCALLTYPE *GetTransformAtDst )( 
            IVwRootSite * This,
            /* [in] */ IVwRootBox *pRoot,
            /* [in] */ POINT pt,
            /* [out] */ RECT *prcSrcRoot,
            /* [out] */ RECT *prcDstRoot);
        
        HRESULT ( STDMETHODCALLTYPE *GetTransformAtSrc )( 
            IVwRootSite * This,
            /* [in] */ IVwRootBox *pRoot,
            /* [in] */ POINT pt,
            /* [out] */ RECT *prcSrcRoot,
            /* [out] */ RECT *prcDstRoot);
        
        HRESULT ( STDMETHODCALLTYPE *ReleaseGraphics )( 
            IVwRootSite * This,
            /* [in] */ IVwRootBox *pRoot,
            /* [in] */ IVwGraphics *pvg);
        
        HRESULT ( STDMETHODCALLTYPE *GetAvailWidth )( 
            IVwRootSite * This,
            /* [in] */ IVwRootBox *pRoot,
            /* [retval][out] */ int *ptwWidth);
        
        HRESULT ( STDMETHODCALLTYPE *DoUpdates )( 
            IVwRootSite * This,
            /* [in] */ IVwRootBox *pRoot);
        
        HRESULT ( STDMETHODCALLTYPE *RootBoxSizeChanged )( 
            IVwRootSite * This,
            /* [in] */ IVwRootBox *pRoot);
        
        HRESULT ( STDMETHODCALLTYPE *AdjustScrollRange )( 
            IVwRootSite * This,
            /* [in] */ IVwRootBox *pRoot,
            /* [in] */ int dxdSize,
            /* [in] */ int dxdPosition,
            /* [in] */ int dydSize,
            /* [in] */ int dydPosition,
            /* [retval][out] */ ComBool *pfForcedScroll);
        
        HRESULT ( STDMETHODCALLTYPE *SelectionChanged )( 
            IVwRootSite * This,
            /* [in] */ IVwRootBox *pRoot,
            /* [in] */ IVwSelection *pvwselNew);
        
        HRESULT ( STDMETHODCALLTYPE *OverlayChanged )( 
            IVwRootSite * This,
            /* [in] */ IVwRootBox *pRoot,
            /* [in] */ IVwOverlay *pvo);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_SemiTagging )( 
            IVwRootSite * This,
            /* [in] */ IVwRootBox *pRoot,
            /* [retval][out] */ ComBool *pf);
        
        HRESULT ( STDMETHODCALLTYPE *ScreenToClient )( 
            IVwRootSite * This,
            /* [in] */ IVwRootBox *pRoot,
            /* [out][in] */ POINT *ppnt);
        
        HRESULT ( STDMETHODCALLTYPE *ClientToScreen )( 
            IVwRootSite * This,
            /* [in] */ IVwRootBox *pRoot,
            /* [out][in] */ POINT *ppnt);
        
        HRESULT ( STDMETHODCALLTYPE *GetAndClearPendingWs )( 
            IVwRootSite * This,
            /* [in] */ IVwRootBox *pRoot,
            /* [retval][out] */ int *pws);
        
        HRESULT ( STDMETHODCALLTYPE *IsOkToMakeLazy )( 
            IVwRootSite * This,
            /* [in] */ IVwRootBox *pRoot,
            /* [in] */ int ydTop,
            /* [in] */ int ydBottom,
            /* [retval][out] */ ComBool *pfOK);
        
        HRESULT ( STDMETHODCALLTYPE *OnProblemDeletion )( 
            IVwRootSite * This,
            /* [in] */ IVwSelection *psel,
            /* [in] */ VwDelProbType dpt,
            /* [retval][out] */ VwDelProbResponse *pdpr);
        
        HRESULT ( STDMETHODCALLTYPE *OnInsertDiffParas )( 
            IVwRootSite * This,
            /* [in] */ IVwRootBox *pRoot,
            /* [in] */ /* external definition not present */ ITsTextProps *pttpDest,
            /* [in] */ int cPara,
            /* [size_is][in] */ /* external definition not present */ ITsTextProps **prgpttpSrc,
            /* [size_is][in] */ /* external definition not present */ ITsString **prgptssSrc,
            /* [in] */ /* external definition not present */ ITsString *ptssTrailing,
            /* [retval][out] */ VwInsertDiffParaResponse *pidpr);
        
        HRESULT ( STDMETHODCALLTYPE *OnInsertDiffPara )( 
            IVwRootSite * This,
            /* [in] */ IVwRootBox *pRoot,
            /* [in] */ /* external definition not present */ ITsTextProps *pttpDest,
            /* [in] */ /* external definition not present */ ITsTextProps *prgpttpSrc,
            /* [in] */ /* external definition not present */ ITsString *prgptssSrc,
            /* [in] */ /* external definition not present */ ITsString *ptssTrailing,
            /* [retval][out] */ VwInsertDiffParaResponse *pidpr);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_TextRepOfObj )( 
            IVwRootSite * This,
            /* [in] */ GUID *pguid,
            /* [retval][out] */ BSTR *pbstrRep);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_MakeObjFromText )( 
            IVwRootSite * This,
            /* [in] */ BSTR bstrText,
            /* [in] */ IVwSelection *pselDst,
            /* [out] */ int *podt,
            /* [retval][out] */ GUID *pGuid);
        
        HRESULT ( STDMETHODCALLTYPE *ScrollSelectionIntoView )( 
            IVwRootSite * This,
            /* [in] */ IVwSelection *psel,
            /* [in] */ VwScrollSelOpts ssoFlag,
            /* [retval][out] */ ComBool *pfRet);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_RootBox )( 
            IVwRootSite * This,
            /* [retval][out] */ IVwRootBox **prootb);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Hwnd )( 
            IVwRootSite * This,
            /* [retval][out] */ DWORD *phwnd);
        
        HRESULT ( STDMETHODCALLTYPE *RequestSelectionAtEndOfUow )( 
            IVwRootSite * This,
            /* [in] */ IVwRootBox *prootb,
            /* [in] */ int ihvoRoot,
            /* [in] */ int cvlsi,
            /* [size_is][in] */ VwSelLevInfo *prgvsli,
            /* [in] */ int tagTextProp,
            /* [in] */ int cpropPrevious,
            /* [in] */ int ich,
            /* [in] */ int wsAlt,
            /* [in] */ ComBool fAssocPrev,
            /* [in] */ /* external definition not present */ ITsTextProps *selProps);
        
        END_INTERFACE
    } IVwRootSiteVtbl;

    interface IVwRootSite
    {
        CONST_VTBL struct IVwRootSiteVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IVwRootSite_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IVwRootSite_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IVwRootSite_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IVwRootSite_InvalidateRect(This,pRoot,xsLeft,ysTop,dxsWidth,dysHeight)	\
    ( (This)->lpVtbl -> InvalidateRect(This,pRoot,xsLeft,ysTop,dxsWidth,dysHeight) ) 

#define IVwRootSite_GetGraphics(This,pRoot,ppvg,prcSrcRoot,prcDstRoot)	\
    ( (This)->lpVtbl -> GetGraphics(This,pRoot,ppvg,prcSrcRoot,prcDstRoot) ) 

#define IVwRootSite_get_LayoutGraphics(This,pRoot,ppvg)	\
    ( (This)->lpVtbl -> get_LayoutGraphics(This,pRoot,ppvg) ) 

#define IVwRootSite_get_ScreenGraphics(This,pRoot,ppvg)	\
    ( (This)->lpVtbl -> get_ScreenGraphics(This,pRoot,ppvg) ) 

#define IVwRootSite_GetTransformAtDst(This,pRoot,pt,prcSrcRoot,prcDstRoot)	\
    ( (This)->lpVtbl -> GetTransformAtDst(This,pRoot,pt,prcSrcRoot,prcDstRoot) ) 

#define IVwRootSite_GetTransformAtSrc(This,pRoot,pt,prcSrcRoot,prcDstRoot)	\
    ( (This)->lpVtbl -> GetTransformAtSrc(This,pRoot,pt,prcSrcRoot,prcDstRoot) ) 

#define IVwRootSite_ReleaseGraphics(This,pRoot,pvg)	\
    ( (This)->lpVtbl -> ReleaseGraphics(This,pRoot,pvg) ) 

#define IVwRootSite_GetAvailWidth(This,pRoot,ptwWidth)	\
    ( (This)->lpVtbl -> GetAvailWidth(This,pRoot,ptwWidth) ) 

#define IVwRootSite_DoUpdates(This,pRoot)	\
    ( (This)->lpVtbl -> DoUpdates(This,pRoot) ) 

#define IVwRootSite_RootBoxSizeChanged(This,pRoot)	\
    ( (This)->lpVtbl -> RootBoxSizeChanged(This,pRoot) ) 

#define IVwRootSite_AdjustScrollRange(This,pRoot,dxdSize,dxdPosition,dydSize,dydPosition,pfForcedScroll)	\
    ( (This)->lpVtbl -> AdjustScrollRange(This,pRoot,dxdSize,dxdPosition,dydSize,dydPosition,pfForcedScroll) ) 

#define IVwRootSite_SelectionChanged(This,pRoot,pvwselNew)	\
    ( (This)->lpVtbl -> SelectionChanged(This,pRoot,pvwselNew) ) 

#define IVwRootSite_OverlayChanged(This,pRoot,pvo)	\
    ( (This)->lpVtbl -> OverlayChanged(This,pRoot,pvo) ) 

#define IVwRootSite_get_SemiTagging(This,pRoot,pf)	\
    ( (This)->lpVtbl -> get_SemiTagging(This,pRoot,pf) ) 

#define IVwRootSite_ScreenToClient(This,pRoot,ppnt)	\
    ( (This)->lpVtbl -> ScreenToClient(This,pRoot,ppnt) ) 

#define IVwRootSite_ClientToScreen(This,pRoot,ppnt)	\
    ( (This)->lpVtbl -> ClientToScreen(This,pRoot,ppnt) ) 

#define IVwRootSite_GetAndClearPendingWs(This,pRoot,pws)	\
    ( (This)->lpVtbl -> GetAndClearPendingWs(This,pRoot,pws) ) 

#define IVwRootSite_IsOkToMakeLazy(This,pRoot,ydTop,ydBottom,pfOK)	\
    ( (This)->lpVtbl -> IsOkToMakeLazy(This,pRoot,ydTop,ydBottom,pfOK) ) 

#define IVwRootSite_OnProblemDeletion(This,psel,dpt,pdpr)	\
    ( (This)->lpVtbl -> OnProblemDeletion(This,psel,dpt,pdpr) ) 

#define IVwRootSite_OnInsertDiffParas(This,pRoot,pttpDest,cPara,prgpttpSrc,prgptssSrc,ptssTrailing,pidpr)	\
    ( (This)->lpVtbl -> OnInsertDiffParas(This,pRoot,pttpDest,cPara,prgpttpSrc,prgptssSrc,ptssTrailing,pidpr) ) 

#define IVwRootSite_OnInsertDiffPara(This,pRoot,pttpDest,prgpttpSrc,prgptssSrc,ptssTrailing,pidpr)	\
    ( (This)->lpVtbl -> OnInsertDiffPara(This,pRoot,pttpDest,prgpttpSrc,prgptssSrc,ptssTrailing,pidpr) ) 

#define IVwRootSite_get_TextRepOfObj(This,pguid,pbstrRep)	\
    ( (This)->lpVtbl -> get_TextRepOfObj(This,pguid,pbstrRep) ) 

#define IVwRootSite_get_MakeObjFromText(This,bstrText,pselDst,podt,pGuid)	\
    ( (This)->lpVtbl -> get_MakeObjFromText(This,bstrText,pselDst,podt,pGuid) ) 

#define IVwRootSite_ScrollSelectionIntoView(This,psel,ssoFlag,pfRet)	\
    ( (This)->lpVtbl -> ScrollSelectionIntoView(This,psel,ssoFlag,pfRet) ) 

#define IVwRootSite_get_RootBox(This,prootb)	\
    ( (This)->lpVtbl -> get_RootBox(This,prootb) ) 

#define IVwRootSite_get_Hwnd(This,phwnd)	\
    ( (This)->lpVtbl -> get_Hwnd(This,phwnd) ) 

#define IVwRootSite_RequestSelectionAtEndOfUow(This,prootb,ihvoRoot,cvlsi,prgvsli,tagTextProp,cpropPrevious,ich,wsAlt,fAssocPrev,selProps)	\
    ( (This)->lpVtbl -> RequestSelectionAtEndOfUow(This,prootb,ihvoRoot,cvlsi,prgvsli,tagTextProp,cpropPrevious,ich,wsAlt,fAssocPrev,selProps) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IVwRootSite_INTERFACE_DEFINED__ */


#ifndef __IVwCacheDa_INTERFACE_DEFINED__
#define __IVwCacheDa_INTERFACE_DEFINED__

/* interface IVwCacheDa */
/* [unique][object][uuid] */ 


#define IID_IVwCacheDa __uuidof(IVwCacheDa)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("FECFCBC9-4CD2-4DB3-ADBE-1F628B2DAA33")
    IVwCacheDa : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_TsStrFactory( 
            /* [retval][out] */ /* external definition not present */ ITsStrFactory **pptsf) = 0;
        
        virtual /* [propputref] */ HRESULT STDMETHODCALLTYPE putref_TsStrFactory( 
            /* [in] */ /* external definition not present */ ITsStrFactory *ptsf) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CacheObjProp( 
            /* [in] */ HVO obj,
            /* [in] */ PropTag tag,
            /* [in] */ HVO val) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CacheVecProp( 
            /* [in] */ HVO obj,
            /* [in] */ PropTag tag,
            /* [size_is][in] */ HVO rghvo[  ],
            /* [in] */ const int chvo) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CacheReplace( 
            /* [in] */ HVO hvoObj,
            /* [in] */ PropTag tag,
            /* [in] */ int ihvoMin,
            /* [in] */ int ihvoLim,
            /* [size_is][in] */ HVO prghvo[  ],
            /* [in] */ int chvo) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CacheBinaryProp( 
            /* [in] */ HVO obj,
            /* [in] */ PropTag tag,
            /* [size_is][in] */ byte *prgb,
            /* [in] */ int cb) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CacheGuidProp( 
            /* [in] */ HVO obj,
            /* [in] */ PropTag tag,
            /* [in] */ GUID uid) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CacheInt64Prop( 
            /* [in] */ HVO obj,
            /* [in] */ PropTag tag,
            /* [in] */ __int64 val) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CacheIntProp( 
            /* [in] */ HVO obj,
            /* [in] */ PropTag tag,
            /* [in] */ int val) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CacheBooleanProp( 
            /* [in] */ HVO obj,
            /* [in] */ PropTag tag,
            /* [in] */ ComBool val) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CacheStringAlt( 
            /* [in] */ HVO obj,
            /* [in] */ PropTag tag,
            /* [in] */ int ws,
            /* [in] */ /* external definition not present */ ITsString *ptss) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CacheStringProp( 
            /* [in] */ HVO obj,
            /* [in] */ PropTag tag,
            /* [in] */ /* external definition not present */ ITsString *ptss) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CacheTimeProp( 
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [in] */ SilTime val) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CacheUnicodeProp( 
            /* [in] */ HVO obj,
            /* [in] */ PropTag tag,
            /* [size_is][in] */ OLECHAR *prgch,
            /* [in] */ int cch) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CacheUnknown( 
            /* [in] */ HVO obj,
            /* [in] */ PropTag tag,
            /* [in] */ IUnknown *punk) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ClearInfoAbout( 
            /* [in] */ HVO hvo,
            /* [in] */ VwClearInfoAction cia) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ClearInfoAboutAll( 
            /* [size_is][in] */ HVO *prghvo,
            /* [in] */ int chvo,
            /* [in] */ VwClearInfoAction cia) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_CachedIntProp( 
            /* [in] */ HVO obj,
            /* [in] */ PropTag tag,
            /* [out] */ ComBool *pf,
            /* [retval][out] */ int *pn) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ClearAllData( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE InstallVirtual( 
            /* [in] */ IVwVirtualHandler *pvh) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetVirtualHandlerId( 
            /* [in] */ PropTag tag,
            /* [retval][out] */ IVwVirtualHandler **ppvh) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetVirtualHandlerName( 
            /* [in] */ BSTR bstrClass,
            /* [in] */ BSTR bstrField,
            /* [retval][out] */ IVwVirtualHandler **ppvh) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ClearVirtualProperties( void) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IVwCacheDaVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IVwCacheDa * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IVwCacheDa * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IVwCacheDa * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_TsStrFactory )( 
            IVwCacheDa * This,
            /* [retval][out] */ /* external definition not present */ ITsStrFactory **pptsf);
        
        /* [propputref] */ HRESULT ( STDMETHODCALLTYPE *putref_TsStrFactory )( 
            IVwCacheDa * This,
            /* [in] */ /* external definition not present */ ITsStrFactory *ptsf);
        
        HRESULT ( STDMETHODCALLTYPE *CacheObjProp )( 
            IVwCacheDa * This,
            /* [in] */ HVO obj,
            /* [in] */ PropTag tag,
            /* [in] */ HVO val);
        
        HRESULT ( STDMETHODCALLTYPE *CacheVecProp )( 
            IVwCacheDa * This,
            /* [in] */ HVO obj,
            /* [in] */ PropTag tag,
            /* [size_is][in] */ HVO rghvo[  ],
            /* [in] */ const int chvo);
        
        HRESULT ( STDMETHODCALLTYPE *CacheReplace )( 
            IVwCacheDa * This,
            /* [in] */ HVO hvoObj,
            /* [in] */ PropTag tag,
            /* [in] */ int ihvoMin,
            /* [in] */ int ihvoLim,
            /* [size_is][in] */ HVO prghvo[  ],
            /* [in] */ int chvo);
        
        HRESULT ( STDMETHODCALLTYPE *CacheBinaryProp )( 
            IVwCacheDa * This,
            /* [in] */ HVO obj,
            /* [in] */ PropTag tag,
            /* [size_is][in] */ byte *prgb,
            /* [in] */ int cb);
        
        HRESULT ( STDMETHODCALLTYPE *CacheGuidProp )( 
            IVwCacheDa * This,
            /* [in] */ HVO obj,
            /* [in] */ PropTag tag,
            /* [in] */ GUID uid);
        
        HRESULT ( STDMETHODCALLTYPE *CacheInt64Prop )( 
            IVwCacheDa * This,
            /* [in] */ HVO obj,
            /* [in] */ PropTag tag,
            /* [in] */ __int64 val);
        
        HRESULT ( STDMETHODCALLTYPE *CacheIntProp )( 
            IVwCacheDa * This,
            /* [in] */ HVO obj,
            /* [in] */ PropTag tag,
            /* [in] */ int val);
        
        HRESULT ( STDMETHODCALLTYPE *CacheBooleanProp )( 
            IVwCacheDa * This,
            /* [in] */ HVO obj,
            /* [in] */ PropTag tag,
            /* [in] */ ComBool val);
        
        HRESULT ( STDMETHODCALLTYPE *CacheStringAlt )( 
            IVwCacheDa * This,
            /* [in] */ HVO obj,
            /* [in] */ PropTag tag,
            /* [in] */ int ws,
            /* [in] */ /* external definition not present */ ITsString *ptss);
        
        HRESULT ( STDMETHODCALLTYPE *CacheStringProp )( 
            IVwCacheDa * This,
            /* [in] */ HVO obj,
            /* [in] */ PropTag tag,
            /* [in] */ /* external definition not present */ ITsString *ptss);
        
        HRESULT ( STDMETHODCALLTYPE *CacheTimeProp )( 
            IVwCacheDa * This,
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [in] */ SilTime val);
        
        HRESULT ( STDMETHODCALLTYPE *CacheUnicodeProp )( 
            IVwCacheDa * This,
            /* [in] */ HVO obj,
            /* [in] */ PropTag tag,
            /* [size_is][in] */ OLECHAR *prgch,
            /* [in] */ int cch);
        
        HRESULT ( STDMETHODCALLTYPE *CacheUnknown )( 
            IVwCacheDa * This,
            /* [in] */ HVO obj,
            /* [in] */ PropTag tag,
            /* [in] */ IUnknown *punk);
        
        HRESULT ( STDMETHODCALLTYPE *ClearInfoAbout )( 
            IVwCacheDa * This,
            /* [in] */ HVO hvo,
            /* [in] */ VwClearInfoAction cia);
        
        HRESULT ( STDMETHODCALLTYPE *ClearInfoAboutAll )( 
            IVwCacheDa * This,
            /* [size_is][in] */ HVO *prghvo,
            /* [in] */ int chvo,
            /* [in] */ VwClearInfoAction cia);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_CachedIntProp )( 
            IVwCacheDa * This,
            /* [in] */ HVO obj,
            /* [in] */ PropTag tag,
            /* [out] */ ComBool *pf,
            /* [retval][out] */ int *pn);
        
        HRESULT ( STDMETHODCALLTYPE *ClearAllData )( 
            IVwCacheDa * This);
        
        HRESULT ( STDMETHODCALLTYPE *InstallVirtual )( 
            IVwCacheDa * This,
            /* [in] */ IVwVirtualHandler *pvh);
        
        HRESULT ( STDMETHODCALLTYPE *GetVirtualHandlerId )( 
            IVwCacheDa * This,
            /* [in] */ PropTag tag,
            /* [retval][out] */ IVwVirtualHandler **ppvh);
        
        HRESULT ( STDMETHODCALLTYPE *GetVirtualHandlerName )( 
            IVwCacheDa * This,
            /* [in] */ BSTR bstrClass,
            /* [in] */ BSTR bstrField,
            /* [retval][out] */ IVwVirtualHandler **ppvh);
        
        HRESULT ( STDMETHODCALLTYPE *ClearVirtualProperties )( 
            IVwCacheDa * This);
        
        END_INTERFACE
    } IVwCacheDaVtbl;

    interface IVwCacheDa
    {
        CONST_VTBL struct IVwCacheDaVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IVwCacheDa_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IVwCacheDa_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IVwCacheDa_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IVwCacheDa_get_TsStrFactory(This,pptsf)	\
    ( (This)->lpVtbl -> get_TsStrFactory(This,pptsf) ) 

#define IVwCacheDa_putref_TsStrFactory(This,ptsf)	\
    ( (This)->lpVtbl -> putref_TsStrFactory(This,ptsf) ) 

#define IVwCacheDa_CacheObjProp(This,obj,tag,val)	\
    ( (This)->lpVtbl -> CacheObjProp(This,obj,tag,val) ) 

#define IVwCacheDa_CacheVecProp(This,obj,tag,rghvo,chvo)	\
    ( (This)->lpVtbl -> CacheVecProp(This,obj,tag,rghvo,chvo) ) 

#define IVwCacheDa_CacheReplace(This,hvoObj,tag,ihvoMin,ihvoLim,prghvo,chvo)	\
    ( (This)->lpVtbl -> CacheReplace(This,hvoObj,tag,ihvoMin,ihvoLim,prghvo,chvo) ) 

#define IVwCacheDa_CacheBinaryProp(This,obj,tag,prgb,cb)	\
    ( (This)->lpVtbl -> CacheBinaryProp(This,obj,tag,prgb,cb) ) 

#define IVwCacheDa_CacheGuidProp(This,obj,tag,uid)	\
    ( (This)->lpVtbl -> CacheGuidProp(This,obj,tag,uid) ) 

#define IVwCacheDa_CacheInt64Prop(This,obj,tag,val)	\
    ( (This)->lpVtbl -> CacheInt64Prop(This,obj,tag,val) ) 

#define IVwCacheDa_CacheIntProp(This,obj,tag,val)	\
    ( (This)->lpVtbl -> CacheIntProp(This,obj,tag,val) ) 

#define IVwCacheDa_CacheBooleanProp(This,obj,tag,val)	\
    ( (This)->lpVtbl -> CacheBooleanProp(This,obj,tag,val) ) 

#define IVwCacheDa_CacheStringAlt(This,obj,tag,ws,ptss)	\
    ( (This)->lpVtbl -> CacheStringAlt(This,obj,tag,ws,ptss) ) 

#define IVwCacheDa_CacheStringProp(This,obj,tag,ptss)	\
    ( (This)->lpVtbl -> CacheStringProp(This,obj,tag,ptss) ) 

#define IVwCacheDa_CacheTimeProp(This,hvo,tag,val)	\
    ( (This)->lpVtbl -> CacheTimeProp(This,hvo,tag,val) ) 

#define IVwCacheDa_CacheUnicodeProp(This,obj,tag,prgch,cch)	\
    ( (This)->lpVtbl -> CacheUnicodeProp(This,obj,tag,prgch,cch) ) 

#define IVwCacheDa_CacheUnknown(This,obj,tag,punk)	\
    ( (This)->lpVtbl -> CacheUnknown(This,obj,tag,punk) ) 

#define IVwCacheDa_ClearInfoAbout(This,hvo,cia)	\
    ( (This)->lpVtbl -> ClearInfoAbout(This,hvo,cia) ) 

#define IVwCacheDa_ClearInfoAboutAll(This,prghvo,chvo,cia)	\
    ( (This)->lpVtbl -> ClearInfoAboutAll(This,prghvo,chvo,cia) ) 

#define IVwCacheDa_get_CachedIntProp(This,obj,tag,pf,pn)	\
    ( (This)->lpVtbl -> get_CachedIntProp(This,obj,tag,pf,pn) ) 

#define IVwCacheDa_ClearAllData(This)	\
    ( (This)->lpVtbl -> ClearAllData(This) ) 

#define IVwCacheDa_InstallVirtual(This,pvh)	\
    ( (This)->lpVtbl -> InstallVirtual(This,pvh) ) 

#define IVwCacheDa_GetVirtualHandlerId(This,tag,ppvh)	\
    ( (This)->lpVtbl -> GetVirtualHandlerId(This,tag,ppvh) ) 

#define IVwCacheDa_GetVirtualHandlerName(This,bstrClass,bstrField,ppvh)	\
    ( (This)->lpVtbl -> GetVirtualHandlerName(This,bstrClass,bstrField,ppvh) ) 

#define IVwCacheDa_ClearVirtualProperties(This)	\
    ( (This)->lpVtbl -> ClearVirtualProperties(This) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IVwCacheDa_INTERFACE_DEFINED__ */


#ifndef __IVwRootBox_INTERFACE_DEFINED__
#define __IVwRootBox_INTERFACE_DEFINED__

/* interface IVwRootBox */
/* [unique][object][uuid] */ 


#define IID_IVwRootBox __uuidof(IVwRootBox)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("06DAA10B-F69A-4000-8C8B-3F725C3FC368")
    IVwRootBox : public IVwNotifyChange
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE SetSite( 
            /* [in] */ IVwRootSite *pvrs) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_DataAccess( 
            /* [retval][out] */ /* external definition not present */ ISilDataAccess **ppsda) = 0;
        
        virtual /* [propputref] */ HRESULT STDMETHODCALLTYPE putref_DataAccess( 
            /* [in] */ /* external definition not present */ ISilDataAccess *psda) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_RenderEngineFactory( 
            /* [retval][out] */ IRenderEngineFactory **ppref) = 0;
        
        virtual /* [propputref] */ HRESULT STDMETHODCALLTYPE putref_RenderEngineFactory( 
            /* [in] */ IRenderEngineFactory *pref) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_TsStrFactory( 
            /* [retval][out] */ /* external definition not present */ ITsStrFactory **pptsf) = 0;
        
        virtual /* [propputref] */ HRESULT STDMETHODCALLTYPE putref_TsStrFactory( 
            /* [in] */ /* external definition not present */ ITsStrFactory *ptsf) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetRootObjects( 
            /* [size_is][in] */ HVO *prghvo,
            /* [size_is][in] */ IVwViewConstructor **prgpvwvc,
            /* [size_is][in] */ int *prgfrag,
            /* [in] */ /* external definition not present */ IVwStylesheet *pss,
            /* [in] */ int chvo) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetRootObject( 
            /* [in] */ HVO hvo,
            /* [in] */ IVwViewConstructor *pvwvc,
            /* [in] */ int frag,
            /* [in] */ /* external definition not present */ IVwStylesheet *pss) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetRootVariant( 
            /* [in] */ VARIANT v,
            /* [in] */ /* external definition not present */ IVwStylesheet *pss,
            /* [in] */ IVwViewConstructor *pvwvc,
            /* [in] */ int frag) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetRootString( 
            /* [in] */ /* external definition not present */ ITsString *ptss,
            /* [in] */ /* external definition not present */ IVwStylesheet *pss,
            /* [in] */ IVwViewConstructor *pvwvc,
            /* [in] */ int frag) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Overlay( 
            /* [retval][out] */ IVwOverlay **ppvo) = 0;
        
        virtual /* [propputref] */ HRESULT STDMETHODCALLTYPE putref_Overlay( 
            /* [in] */ IVwOverlay *pvo) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetRootVariant( 
            /* [retval][out] */ VARIANT *pv) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Selection( 
            /* [retval][out] */ IVwSelection **ppsel) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DestroySelection( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE MakeTextSelection( 
            /* [in] */ int ihvoRoot,
            /* [in] */ int cvlsi,
            /* [size_is][in] */ VwSelLevInfo *prgvsli,
            /* [in] */ int tagTextProp,
            /* [in] */ int cpropPrevious,
            /* [in] */ int ichAnchor,
            /* [in] */ int ichEnd,
            /* [in] */ int ws,
            /* [in] */ ComBool fAssocPrev,
            /* [in] */ int ihvoEnd,
            /* [in] */ /* external definition not present */ ITsTextProps *pttpIns,
            /* [in] */ ComBool fInstall,
            /* [retval][out] */ IVwSelection **ppsel) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE MakeRangeSelection( 
            /* [in] */ IVwSelection *pselAnchor,
            /* [in] */ IVwSelection *pselEnd,
            /* [in] */ ComBool fInstall,
            /* [retval][out] */ IVwSelection **ppsel) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE MakeSimpleSel( 
            /* [in] */ ComBool fInitial,
            /* [in] */ ComBool fEdit,
            /* [in] */ ComBool fRange,
            /* [in] */ ComBool fInstall,
            /* [retval][out] */ IVwSelection **ppsel) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE MakeTextSelInObj( 
            /* [in] */ int ihvoRoot,
            /* [in] */ int cvsli,
            /* [size_is][in] */ VwSelLevInfo *prgvsli,
            /* [in] */ int cvsliEnd,
            /* [size_is][in] */ VwSelLevInfo *prgvsliEnd,
            /* [in] */ ComBool fInitial,
            /* [in] */ ComBool fEdit,
            /* [in] */ ComBool fRange,
            /* [in] */ ComBool fWholeObj,
            /* [in] */ ComBool fInstall,
            /* [retval][out] */ IVwSelection **ppsel) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE MakeSelInObj( 
            /* [in] */ int ihvoRoot,
            /* [in] */ int cvsli,
            /* [size_is][in] */ VwSelLevInfo *prgvsli,
            /* [in] */ int tag,
            /* [in] */ ComBool fInstall,
            /* [retval][out] */ IVwSelection **ppsel) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE MakeSelAt( 
            /* [in] */ int xd,
            /* [in] */ int yd,
            /* [in] */ RECT rcSrc,
            /* [in] */ RECT rcDst,
            /* [in] */ ComBool fInstall,
            /* [retval][out] */ IVwSelection **ppsel) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE MakeSelInBox( 
            /* [in] */ IVwSelection *pselInit,
            /* [in] */ ComBool fEndPoint,
            /* [in] */ int iLevel,
            /* [in] */ int iBox,
            /* [in] */ ComBool fInitial,
            /* [in] */ ComBool fRange,
            /* [in] */ ComBool fInstall,
            /* [retval][out] */ IVwSelection **ppsel) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsClickInText( 
            /* [in] */ int xd,
            /* [in] */ int yd,
            /* [in] */ RECT rcSrc,
            /* [in] */ RECT rcDst,
            /* [retval][out] */ ComBool *pfInText) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsClickInObject( 
            /* [in] */ int xd,
            /* [in] */ int yd,
            /* [in] */ RECT rcSrc,
            /* [in] */ RECT rcDst,
            /* [out] */ int *podt,
            /* [retval][out] */ ComBool *pfInObject) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsClickInOverlayTag( 
            /* [in] */ int xd,
            /* [in] */ int yd,
            /* [in] */ RECT rcSrc1,
            /* [in] */ RECT rcDst1,
            /* [out] */ int *piGuid,
            /* [out] */ BSTR *pbstrGuids,
            /* [out] */ RECT *prcTag,
            /* [out] */ RECT *prcAllTags,
            /* [out] */ ComBool *pfOpeningTag,
            /* [retval][out] */ ComBool *pfInOverlayTag) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE OnTyping( 
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ BSTR bstrInput,
            /* [in] */ VwShiftStatus ss,
            /* [out][in] */ int *pwsPending) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DeleteRangeIfComplex( 
            /* [in] */ IVwGraphics *pvg,
            /* [out] */ ComBool *pfWasComplex) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE OnChar( 
            /* [in] */ int chw) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE OnSysChar( 
            /* [in] */ int chw) = 0;
        
        virtual /* [custom] */ HRESULT STDMETHODCALLTYPE OnExtendedKey( 
            /* [in] */ int chw,
            /* [in] */ VwShiftStatus ss,
            /* [in] */ int nFlags) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE FlashInsertionPoint( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE MouseDown( 
            /* [in] */ int xd,
            /* [in] */ int yd,
            /* [in] */ RECT rcSrc,
            /* [in] */ RECT rcDst) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE MouseDblClk( 
            /* [in] */ int xd,
            /* [in] */ int yd,
            /* [in] */ RECT rcSrc,
            /* [in] */ RECT rcDst) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE MouseMoveDrag( 
            /* [in] */ int xd,
            /* [in] */ int yd,
            /* [in] */ RECT rcSrc,
            /* [in] */ RECT rcDst) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE MouseDownExtended( 
            /* [in] */ int xd,
            /* [in] */ int yd,
            /* [in] */ RECT rcSrc,
            /* [in] */ RECT rcDst) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE MouseUp( 
            /* [in] */ int xd,
            /* [in] */ int yd,
            /* [in] */ RECT rcSrc,
            /* [in] */ RECT rcDst) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Activate( 
            /* [in] */ VwSelectionState vss) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_SelectionState( 
            /* [retval][out] */ VwSelectionState *pvss) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE PrepareToDraw( 
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ RECT rcSrc,
            /* [in] */ RECT rcDst,
            /* [retval][out] */ VwPrepDrawResult *pxpdr) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DrawRoot( 
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ RECT rcSrc,
            /* [in] */ RECT rcDst,
            /* [in] */ ComBool fDrawSel) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Layout( 
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ int dxsAvailWidth) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Height( 
            /* [retval][out] */ int *pdysHeight) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Width( 
            /* [retval][out] */ int *pdxsWidth) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE InitializePrinting( 
            /* [in] */ IVwPrintContext *pvpc) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetTotalPrintPages( 
            /* [in] */ IVwPrintContext *pvpc,
            /* [retval][out] */ int *pcPageTotal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE PrintSinglePage( 
            /* [in] */ IVwPrintContext *pvpc,
            /* [in] */ int nPageNo) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Site( 
            /* [retval][out] */ IVwRootSite **ppvrs) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE LoseFocus( 
            /* [retval][out] */ ComBool *pfOk) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Close( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Reconstruct( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE OnStylesheetChange( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DrawingErrors( 
            /* [in] */ IVwGraphics *pvg) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Stylesheet( 
            /* [retval][out] */ /* external definition not present */ IVwStylesheet **ppvss) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetTableColWidths( 
            /* [size_is][in] */ VwLength *prgvlen,
            /* [in] */ int cvlen) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE IsDirty( 
            /* [retval][out] */ ComBool *pfDirty) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_XdPos( 
            /* [retval][out] */ int *pxdPos) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Synchronizer( 
            /* [retval][out] */ IVwSynchronizer **ppsync) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetRootObject( 
            /* [out] */ HVO *phvo,
            /* [out] */ IVwViewConstructor **ppvwvc,
            /* [out] */ int *pfrag,
            /* [out] */ /* external definition not present */ IVwStylesheet **ppss) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DrawRoot2( 
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ RECT rcSrc,
            /* [in] */ RECT rcDst,
            /* [in] */ ComBool fDrawSel,
            /* [in] */ int ysTop,
            /* [in] */ int dysHeight) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_MaxParasToScan( 
            /* [retval][out] */ int *pcParas) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_MaxParasToScan( 
            /* [in] */ int cParas) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DoSpellCheckStep( 
            /* [retval][out] */ ComBool *pfComplete) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE IsSpellCheckComplete( 
            /* [retval][out] */ ComBool *pfComplete) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsCompositionInProgress( 
            /* [retval][out] */ ComBool *pfInProgress) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsPropChangedInProgress( 
            /* [retval][out] */ ComBool *pfInProgress) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE RestartSpellChecking( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetSpellingRepository( 
            /* [in] */ /* external definition not present */ IGetSpellChecker *pgsp) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IVwRootBoxVtbl
    {
        BEGIN_INTERFACE
        
        /* [id][restricted][funcdescattr] */ HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IVwRootBox * This,
            /* [in][idldescattr] */ struct GUID *riid,
            /* [out][idldescattr] */ void **ppvObj);
        
        /* [id][restricted][funcdescattr] */ unsigned long ( STDMETHODCALLTYPE *AddRef )( 
            IVwRootBox * This);
        
        /* [id][restricted][funcdescattr] */ unsigned long ( STDMETHODCALLTYPE *Release )( 
            IVwRootBox * This);
        
        /* [id][funcdescattr] */ HRESULT ( STDMETHODCALLTYPE *PropChanged )( 
            IVwRootBox * This,
            /* [in][idldescattr] */ signed INT hvo,
            /* [in][idldescattr] */ signed INT tag,
            /* [in][idldescattr] */ signed INT ivMin,
            /* [in][idldescattr] */ signed INT cvIns,
            /* [in][idldescattr] */ signed INT cvDel);
        
        HRESULT ( STDMETHODCALLTYPE *SetSite )( 
            IVwRootBox * This,
            /* [in] */ IVwRootSite *pvrs);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_DataAccess )( 
            IVwRootBox * This,
            /* [retval][out] */ /* external definition not present */ ISilDataAccess **ppsda);
        
        /* [propputref] */ HRESULT ( STDMETHODCALLTYPE *putref_DataAccess )( 
            IVwRootBox * This,
            /* [in] */ /* external definition not present */ ISilDataAccess *psda);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_RenderEngineFactory )( 
            IVwRootBox * This,
            /* [retval][out] */ IRenderEngineFactory **ppref);
        
        /* [propputref] */ HRESULT ( STDMETHODCALLTYPE *putref_RenderEngineFactory )( 
            IVwRootBox * This,
            /* [in] */ IRenderEngineFactory *pref);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_TsStrFactory )( 
            IVwRootBox * This,
            /* [retval][out] */ /* external definition not present */ ITsStrFactory **pptsf);
        
        /* [propputref] */ HRESULT ( STDMETHODCALLTYPE *putref_TsStrFactory )( 
            IVwRootBox * This,
            /* [in] */ /* external definition not present */ ITsStrFactory *ptsf);
        
        HRESULT ( STDMETHODCALLTYPE *SetRootObjects )( 
            IVwRootBox * This,
            /* [size_is][in] */ HVO *prghvo,
            /* [size_is][in] */ IVwViewConstructor **prgpvwvc,
            /* [size_is][in] */ int *prgfrag,
            /* [in] */ /* external definition not present */ IVwStylesheet *pss,
            /* [in] */ int chvo);
        
        HRESULT ( STDMETHODCALLTYPE *SetRootObject )( 
            IVwRootBox * This,
            /* [in] */ HVO hvo,
            /* [in] */ IVwViewConstructor *pvwvc,
            /* [in] */ int frag,
            /* [in] */ /* external definition not present */ IVwStylesheet *pss);
        
        HRESULT ( STDMETHODCALLTYPE *SetRootVariant )( 
            IVwRootBox * This,
            /* [in] */ VARIANT v,
            /* [in] */ /* external definition not present */ IVwStylesheet *pss,
            /* [in] */ IVwViewConstructor *pvwvc,
            /* [in] */ int frag);
        
        HRESULT ( STDMETHODCALLTYPE *SetRootString )( 
            IVwRootBox * This,
            /* [in] */ /* external definition not present */ ITsString *ptss,
            /* [in] */ /* external definition not present */ IVwStylesheet *pss,
            /* [in] */ IVwViewConstructor *pvwvc,
            /* [in] */ int frag);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Overlay )( 
            IVwRootBox * This,
            /* [retval][out] */ IVwOverlay **ppvo);
        
        /* [propputref] */ HRESULT ( STDMETHODCALLTYPE *putref_Overlay )( 
            IVwRootBox * This,
            /* [in] */ IVwOverlay *pvo);
        
        HRESULT ( STDMETHODCALLTYPE *GetRootVariant )( 
            IVwRootBox * This,
            /* [retval][out] */ VARIANT *pv);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Selection )( 
            IVwRootBox * This,
            /* [retval][out] */ IVwSelection **ppsel);
        
        HRESULT ( STDMETHODCALLTYPE *DestroySelection )( 
            IVwRootBox * This);
        
        HRESULT ( STDMETHODCALLTYPE *MakeTextSelection )( 
            IVwRootBox * This,
            /* [in] */ int ihvoRoot,
            /* [in] */ int cvlsi,
            /* [size_is][in] */ VwSelLevInfo *prgvsli,
            /* [in] */ int tagTextProp,
            /* [in] */ int cpropPrevious,
            /* [in] */ int ichAnchor,
            /* [in] */ int ichEnd,
            /* [in] */ int ws,
            /* [in] */ ComBool fAssocPrev,
            /* [in] */ int ihvoEnd,
            /* [in] */ /* external definition not present */ ITsTextProps *pttpIns,
            /* [in] */ ComBool fInstall,
            /* [retval][out] */ IVwSelection **ppsel);
        
        HRESULT ( STDMETHODCALLTYPE *MakeRangeSelection )( 
            IVwRootBox * This,
            /* [in] */ IVwSelection *pselAnchor,
            /* [in] */ IVwSelection *pselEnd,
            /* [in] */ ComBool fInstall,
            /* [retval][out] */ IVwSelection **ppsel);
        
        HRESULT ( STDMETHODCALLTYPE *MakeSimpleSel )( 
            IVwRootBox * This,
            /* [in] */ ComBool fInitial,
            /* [in] */ ComBool fEdit,
            /* [in] */ ComBool fRange,
            /* [in] */ ComBool fInstall,
            /* [retval][out] */ IVwSelection **ppsel);
        
        HRESULT ( STDMETHODCALLTYPE *MakeTextSelInObj )( 
            IVwRootBox * This,
            /* [in] */ int ihvoRoot,
            /* [in] */ int cvsli,
            /* [size_is][in] */ VwSelLevInfo *prgvsli,
            /* [in] */ int cvsliEnd,
            /* [size_is][in] */ VwSelLevInfo *prgvsliEnd,
            /* [in] */ ComBool fInitial,
            /* [in] */ ComBool fEdit,
            /* [in] */ ComBool fRange,
            /* [in] */ ComBool fWholeObj,
            /* [in] */ ComBool fInstall,
            /* [retval][out] */ IVwSelection **ppsel);
        
        HRESULT ( STDMETHODCALLTYPE *MakeSelInObj )( 
            IVwRootBox * This,
            /* [in] */ int ihvoRoot,
            /* [in] */ int cvsli,
            /* [size_is][in] */ VwSelLevInfo *prgvsli,
            /* [in] */ int tag,
            /* [in] */ ComBool fInstall,
            /* [retval][out] */ IVwSelection **ppsel);
        
        HRESULT ( STDMETHODCALLTYPE *MakeSelAt )( 
            IVwRootBox * This,
            /* [in] */ int xd,
            /* [in] */ int yd,
            /* [in] */ RECT rcSrc,
            /* [in] */ RECT rcDst,
            /* [in] */ ComBool fInstall,
            /* [retval][out] */ IVwSelection **ppsel);
        
        HRESULT ( STDMETHODCALLTYPE *MakeSelInBox )( 
            IVwRootBox * This,
            /* [in] */ IVwSelection *pselInit,
            /* [in] */ ComBool fEndPoint,
            /* [in] */ int iLevel,
            /* [in] */ int iBox,
            /* [in] */ ComBool fInitial,
            /* [in] */ ComBool fRange,
            /* [in] */ ComBool fInstall,
            /* [retval][out] */ IVwSelection **ppsel);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsClickInText )( 
            IVwRootBox * This,
            /* [in] */ int xd,
            /* [in] */ int yd,
            /* [in] */ RECT rcSrc,
            /* [in] */ RECT rcDst,
            /* [retval][out] */ ComBool *pfInText);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsClickInObject )( 
            IVwRootBox * This,
            /* [in] */ int xd,
            /* [in] */ int yd,
            /* [in] */ RECT rcSrc,
            /* [in] */ RECT rcDst,
            /* [out] */ int *podt,
            /* [retval][out] */ ComBool *pfInObject);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsClickInOverlayTag )( 
            IVwRootBox * This,
            /* [in] */ int xd,
            /* [in] */ int yd,
            /* [in] */ RECT rcSrc1,
            /* [in] */ RECT rcDst1,
            /* [out] */ int *piGuid,
            /* [out] */ BSTR *pbstrGuids,
            /* [out] */ RECT *prcTag,
            /* [out] */ RECT *prcAllTags,
            /* [out] */ ComBool *pfOpeningTag,
            /* [retval][out] */ ComBool *pfInOverlayTag);
        
        HRESULT ( STDMETHODCALLTYPE *OnTyping )( 
            IVwRootBox * This,
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ BSTR bstrInput,
            /* [in] */ VwShiftStatus ss,
            /* [out][in] */ int *pwsPending);
        
        HRESULT ( STDMETHODCALLTYPE *DeleteRangeIfComplex )( 
            IVwRootBox * This,
            /* [in] */ IVwGraphics *pvg,
            /* [out] */ ComBool *pfWasComplex);
        
        HRESULT ( STDMETHODCALLTYPE *OnChar )( 
            IVwRootBox * This,
            /* [in] */ int chw);
        
        HRESULT ( STDMETHODCALLTYPE *OnSysChar )( 
            IVwRootBox * This,
            /* [in] */ int chw);
        
        /* [custom] */ HRESULT ( STDMETHODCALLTYPE *OnExtendedKey )( 
            IVwRootBox * This,
            /* [in] */ int chw,
            /* [in] */ VwShiftStatus ss,
            /* [in] */ int nFlags);
        
        HRESULT ( STDMETHODCALLTYPE *FlashInsertionPoint )( 
            IVwRootBox * This);
        
        HRESULT ( STDMETHODCALLTYPE *MouseDown )( 
            IVwRootBox * This,
            /* [in] */ int xd,
            /* [in] */ int yd,
            /* [in] */ RECT rcSrc,
            /* [in] */ RECT rcDst);
        
        HRESULT ( STDMETHODCALLTYPE *MouseDblClk )( 
            IVwRootBox * This,
            /* [in] */ int xd,
            /* [in] */ int yd,
            /* [in] */ RECT rcSrc,
            /* [in] */ RECT rcDst);
        
        HRESULT ( STDMETHODCALLTYPE *MouseMoveDrag )( 
            IVwRootBox * This,
            /* [in] */ int xd,
            /* [in] */ int yd,
            /* [in] */ RECT rcSrc,
            /* [in] */ RECT rcDst);
        
        HRESULT ( STDMETHODCALLTYPE *MouseDownExtended )( 
            IVwRootBox * This,
            /* [in] */ int xd,
            /* [in] */ int yd,
            /* [in] */ RECT rcSrc,
            /* [in] */ RECT rcDst);
        
        HRESULT ( STDMETHODCALLTYPE *MouseUp )( 
            IVwRootBox * This,
            /* [in] */ int xd,
            /* [in] */ int yd,
            /* [in] */ RECT rcSrc,
            /* [in] */ RECT rcDst);
        
        HRESULT ( STDMETHODCALLTYPE *Activate )( 
            IVwRootBox * This,
            /* [in] */ VwSelectionState vss);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_SelectionState )( 
            IVwRootBox * This,
            /* [retval][out] */ VwSelectionState *pvss);
        
        HRESULT ( STDMETHODCALLTYPE *PrepareToDraw )( 
            IVwRootBox * This,
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ RECT rcSrc,
            /* [in] */ RECT rcDst,
            /* [retval][out] */ VwPrepDrawResult *pxpdr);
        
        HRESULT ( STDMETHODCALLTYPE *DrawRoot )( 
            IVwRootBox * This,
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ RECT rcSrc,
            /* [in] */ RECT rcDst,
            /* [in] */ ComBool fDrawSel);
        
        HRESULT ( STDMETHODCALLTYPE *Layout )( 
            IVwRootBox * This,
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ int dxsAvailWidth);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Height )( 
            IVwRootBox * This,
            /* [retval][out] */ int *pdysHeight);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Width )( 
            IVwRootBox * This,
            /* [retval][out] */ int *pdxsWidth);
        
        HRESULT ( STDMETHODCALLTYPE *InitializePrinting )( 
            IVwRootBox * This,
            /* [in] */ IVwPrintContext *pvpc);
        
        HRESULT ( STDMETHODCALLTYPE *GetTotalPrintPages )( 
            IVwRootBox * This,
            /* [in] */ IVwPrintContext *pvpc,
            /* [retval][out] */ int *pcPageTotal);
        
        HRESULT ( STDMETHODCALLTYPE *PrintSinglePage )( 
            IVwRootBox * This,
            /* [in] */ IVwPrintContext *pvpc,
            /* [in] */ int nPageNo);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Site )( 
            IVwRootBox * This,
            /* [retval][out] */ IVwRootSite **ppvrs);
        
        HRESULT ( STDMETHODCALLTYPE *LoseFocus )( 
            IVwRootBox * This,
            /* [retval][out] */ ComBool *pfOk);
        
        HRESULT ( STDMETHODCALLTYPE *Close )( 
            IVwRootBox * This);
        
        HRESULT ( STDMETHODCALLTYPE *Reconstruct )( 
            IVwRootBox * This);
        
        HRESULT ( STDMETHODCALLTYPE *OnStylesheetChange )( 
            IVwRootBox * This);
        
        HRESULT ( STDMETHODCALLTYPE *DrawingErrors )( 
            IVwRootBox * This,
            /* [in] */ IVwGraphics *pvg);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Stylesheet )( 
            IVwRootBox * This,
            /* [retval][out] */ /* external definition not present */ IVwStylesheet **ppvss);
        
        HRESULT ( STDMETHODCALLTYPE *SetTableColWidths )( 
            IVwRootBox * This,
            /* [size_is][in] */ VwLength *prgvlen,
            /* [in] */ int cvlen);
        
        HRESULT ( STDMETHODCALLTYPE *IsDirty )( 
            IVwRootBox * This,
            /* [retval][out] */ ComBool *pfDirty);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_XdPos )( 
            IVwRootBox * This,
            /* [retval][out] */ int *pxdPos);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Synchronizer )( 
            IVwRootBox * This,
            /* [retval][out] */ IVwSynchronizer **ppsync);
        
        HRESULT ( STDMETHODCALLTYPE *GetRootObject )( 
            IVwRootBox * This,
            /* [out] */ HVO *phvo,
            /* [out] */ IVwViewConstructor **ppvwvc,
            /* [out] */ int *pfrag,
            /* [out] */ /* external definition not present */ IVwStylesheet **ppss);
        
        HRESULT ( STDMETHODCALLTYPE *DrawRoot2 )( 
            IVwRootBox * This,
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ RECT rcSrc,
            /* [in] */ RECT rcDst,
            /* [in] */ ComBool fDrawSel,
            /* [in] */ int ysTop,
            /* [in] */ int dysHeight);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_MaxParasToScan )( 
            IVwRootBox * This,
            /* [retval][out] */ int *pcParas);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_MaxParasToScan )( 
            IVwRootBox * This,
            /* [in] */ int cParas);
        
        HRESULT ( STDMETHODCALLTYPE *DoSpellCheckStep )( 
            IVwRootBox * This,
            /* [retval][out] */ ComBool *pfComplete);
        
        HRESULT ( STDMETHODCALLTYPE *IsSpellCheckComplete )( 
            IVwRootBox * This,
            /* [retval][out] */ ComBool *pfComplete);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsCompositionInProgress )( 
            IVwRootBox * This,
            /* [retval][out] */ ComBool *pfInProgress);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsPropChangedInProgress )( 
            IVwRootBox * This,
            /* [retval][out] */ ComBool *pfInProgress);
        
        HRESULT ( STDMETHODCALLTYPE *RestartSpellChecking )( 
            IVwRootBox * This);
        
        HRESULT ( STDMETHODCALLTYPE *SetSpellingRepository )( 
            IVwRootBox * This,
            /* [in] */ /* external definition not present */ IGetSpellChecker *pgsp);
        
        END_INTERFACE
    } IVwRootBoxVtbl;

    interface IVwRootBox
    {
        CONST_VTBL struct IVwRootBoxVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IVwRootBox_QueryInterface(This,riid,ppvObj)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObj) ) 

#define IVwRootBox_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IVwRootBox_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IVwRootBox_PropChanged(This,hvo,tag,ivMin,cvIns,cvDel)	\
    ( (This)->lpVtbl -> PropChanged(This,hvo,tag,ivMin,cvIns,cvDel) ) 


#define IVwRootBox_SetSite(This,pvrs)	\
    ( (This)->lpVtbl -> SetSite(This,pvrs) ) 

#define IVwRootBox_get_DataAccess(This,ppsda)	\
    ( (This)->lpVtbl -> get_DataAccess(This,ppsda) ) 

#define IVwRootBox_putref_DataAccess(This,psda)	\
    ( (This)->lpVtbl -> putref_DataAccess(This,psda) ) 

#define IVwRootBox_get_RenderEngineFactory(This,ppref)	\
    ( (This)->lpVtbl -> get_RenderEngineFactory(This,ppref) ) 

#define IVwRootBox_putref_RenderEngineFactory(This,pref)	\
    ( (This)->lpVtbl -> putref_RenderEngineFactory(This,pref) ) 

#define IVwRootBox_get_TsStrFactory(This,pptsf)	\
    ( (This)->lpVtbl -> get_TsStrFactory(This,pptsf) ) 

#define IVwRootBox_putref_TsStrFactory(This,ptsf)	\
    ( (This)->lpVtbl -> putref_TsStrFactory(This,ptsf) ) 

#define IVwRootBox_SetRootObjects(This,prghvo,prgpvwvc,prgfrag,pss,chvo)	\
    ( (This)->lpVtbl -> SetRootObjects(This,prghvo,prgpvwvc,prgfrag,pss,chvo) ) 

#define IVwRootBox_SetRootObject(This,hvo,pvwvc,frag,pss)	\
    ( (This)->lpVtbl -> SetRootObject(This,hvo,pvwvc,frag,pss) ) 

#define IVwRootBox_SetRootVariant(This,v,pss,pvwvc,frag)	\
    ( (This)->lpVtbl -> SetRootVariant(This,v,pss,pvwvc,frag) ) 

#define IVwRootBox_SetRootString(This,ptss,pss,pvwvc,frag)	\
    ( (This)->lpVtbl -> SetRootString(This,ptss,pss,pvwvc,frag) ) 

#define IVwRootBox_get_Overlay(This,ppvo)	\
    ( (This)->lpVtbl -> get_Overlay(This,ppvo) ) 

#define IVwRootBox_putref_Overlay(This,pvo)	\
    ( (This)->lpVtbl -> putref_Overlay(This,pvo) ) 

#define IVwRootBox_GetRootVariant(This,pv)	\
    ( (This)->lpVtbl -> GetRootVariant(This,pv) ) 

#define IVwRootBox_get_Selection(This,ppsel)	\
    ( (This)->lpVtbl -> get_Selection(This,ppsel) ) 

#define IVwRootBox_DestroySelection(This)	\
    ( (This)->lpVtbl -> DestroySelection(This) ) 

#define IVwRootBox_MakeTextSelection(This,ihvoRoot,cvlsi,prgvsli,tagTextProp,cpropPrevious,ichAnchor,ichEnd,ws,fAssocPrev,ihvoEnd,pttpIns,fInstall,ppsel)	\
    ( (This)->lpVtbl -> MakeTextSelection(This,ihvoRoot,cvlsi,prgvsli,tagTextProp,cpropPrevious,ichAnchor,ichEnd,ws,fAssocPrev,ihvoEnd,pttpIns,fInstall,ppsel) ) 

#define IVwRootBox_MakeRangeSelection(This,pselAnchor,pselEnd,fInstall,ppsel)	\
    ( (This)->lpVtbl -> MakeRangeSelection(This,pselAnchor,pselEnd,fInstall,ppsel) ) 

#define IVwRootBox_MakeSimpleSel(This,fInitial,fEdit,fRange,fInstall,ppsel)	\
    ( (This)->lpVtbl -> MakeSimpleSel(This,fInitial,fEdit,fRange,fInstall,ppsel) ) 

#define IVwRootBox_MakeTextSelInObj(This,ihvoRoot,cvsli,prgvsli,cvsliEnd,prgvsliEnd,fInitial,fEdit,fRange,fWholeObj,fInstall,ppsel)	\
    ( (This)->lpVtbl -> MakeTextSelInObj(This,ihvoRoot,cvsli,prgvsli,cvsliEnd,prgvsliEnd,fInitial,fEdit,fRange,fWholeObj,fInstall,ppsel) ) 

#define IVwRootBox_MakeSelInObj(This,ihvoRoot,cvsli,prgvsli,tag,fInstall,ppsel)	\
    ( (This)->lpVtbl -> MakeSelInObj(This,ihvoRoot,cvsli,prgvsli,tag,fInstall,ppsel) ) 

#define IVwRootBox_MakeSelAt(This,xd,yd,rcSrc,rcDst,fInstall,ppsel)	\
    ( (This)->lpVtbl -> MakeSelAt(This,xd,yd,rcSrc,rcDst,fInstall,ppsel) ) 

#define IVwRootBox_MakeSelInBox(This,pselInit,fEndPoint,iLevel,iBox,fInitial,fRange,fInstall,ppsel)	\
    ( (This)->lpVtbl -> MakeSelInBox(This,pselInit,fEndPoint,iLevel,iBox,fInitial,fRange,fInstall,ppsel) ) 

#define IVwRootBox_get_IsClickInText(This,xd,yd,rcSrc,rcDst,pfInText)	\
    ( (This)->lpVtbl -> get_IsClickInText(This,xd,yd,rcSrc,rcDst,pfInText) ) 

#define IVwRootBox_get_IsClickInObject(This,xd,yd,rcSrc,rcDst,podt,pfInObject)	\
    ( (This)->lpVtbl -> get_IsClickInObject(This,xd,yd,rcSrc,rcDst,podt,pfInObject) ) 

#define IVwRootBox_get_IsClickInOverlayTag(This,xd,yd,rcSrc1,rcDst1,piGuid,pbstrGuids,prcTag,prcAllTags,pfOpeningTag,pfInOverlayTag)	\
    ( (This)->lpVtbl -> get_IsClickInOverlayTag(This,xd,yd,rcSrc1,rcDst1,piGuid,pbstrGuids,prcTag,prcAllTags,pfOpeningTag,pfInOverlayTag) ) 

#define IVwRootBox_OnTyping(This,pvg,bstrInput,ss,pwsPending)	\
    ( (This)->lpVtbl -> OnTyping(This,pvg,bstrInput,ss,pwsPending) ) 

#define IVwRootBox_DeleteRangeIfComplex(This,pvg,pfWasComplex)	\
    ( (This)->lpVtbl -> DeleteRangeIfComplex(This,pvg,pfWasComplex) ) 

#define IVwRootBox_OnChar(This,chw)	\
    ( (This)->lpVtbl -> OnChar(This,chw) ) 

#define IVwRootBox_OnSysChar(This,chw)	\
    ( (This)->lpVtbl -> OnSysChar(This,chw) ) 

#define IVwRootBox_OnExtendedKey(This,chw,ss,nFlags)	\
    ( (This)->lpVtbl -> OnExtendedKey(This,chw,ss,nFlags) ) 

#define IVwRootBox_FlashInsertionPoint(This)	\
    ( (This)->lpVtbl -> FlashInsertionPoint(This) ) 

#define IVwRootBox_MouseDown(This,xd,yd,rcSrc,rcDst)	\
    ( (This)->lpVtbl -> MouseDown(This,xd,yd,rcSrc,rcDst) ) 

#define IVwRootBox_MouseDblClk(This,xd,yd,rcSrc,rcDst)	\
    ( (This)->lpVtbl -> MouseDblClk(This,xd,yd,rcSrc,rcDst) ) 

#define IVwRootBox_MouseMoveDrag(This,xd,yd,rcSrc,rcDst)	\
    ( (This)->lpVtbl -> MouseMoveDrag(This,xd,yd,rcSrc,rcDst) ) 

#define IVwRootBox_MouseDownExtended(This,xd,yd,rcSrc,rcDst)	\
    ( (This)->lpVtbl -> MouseDownExtended(This,xd,yd,rcSrc,rcDst) ) 

#define IVwRootBox_MouseUp(This,xd,yd,rcSrc,rcDst)	\
    ( (This)->lpVtbl -> MouseUp(This,xd,yd,rcSrc,rcDst) ) 

#define IVwRootBox_Activate(This,vss)	\
    ( (This)->lpVtbl -> Activate(This,vss) ) 

#define IVwRootBox_get_SelectionState(This,pvss)	\
    ( (This)->lpVtbl -> get_SelectionState(This,pvss) ) 

#define IVwRootBox_PrepareToDraw(This,pvg,rcSrc,rcDst,pxpdr)	\
    ( (This)->lpVtbl -> PrepareToDraw(This,pvg,rcSrc,rcDst,pxpdr) ) 

#define IVwRootBox_DrawRoot(This,pvg,rcSrc,rcDst,fDrawSel)	\
    ( (This)->lpVtbl -> DrawRoot(This,pvg,rcSrc,rcDst,fDrawSel) ) 

#define IVwRootBox_Layout(This,pvg,dxsAvailWidth)	\
    ( (This)->lpVtbl -> Layout(This,pvg,dxsAvailWidth) ) 

#define IVwRootBox_get_Height(This,pdysHeight)	\
    ( (This)->lpVtbl -> get_Height(This,pdysHeight) ) 

#define IVwRootBox_get_Width(This,pdxsWidth)	\
    ( (This)->lpVtbl -> get_Width(This,pdxsWidth) ) 

#define IVwRootBox_InitializePrinting(This,pvpc)	\
    ( (This)->lpVtbl -> InitializePrinting(This,pvpc) ) 

#define IVwRootBox_GetTotalPrintPages(This,pvpc,pcPageTotal)	\
    ( (This)->lpVtbl -> GetTotalPrintPages(This,pvpc,pcPageTotal) ) 

#define IVwRootBox_PrintSinglePage(This,pvpc,nPageNo)	\
    ( (This)->lpVtbl -> PrintSinglePage(This,pvpc,nPageNo) ) 

#define IVwRootBox_get_Site(This,ppvrs)	\
    ( (This)->lpVtbl -> get_Site(This,ppvrs) ) 

#define IVwRootBox_LoseFocus(This,pfOk)	\
    ( (This)->lpVtbl -> LoseFocus(This,pfOk) ) 

#define IVwRootBox_Close(This)	\
    ( (This)->lpVtbl -> Close(This) ) 

#define IVwRootBox_Reconstruct(This)	\
    ( (This)->lpVtbl -> Reconstruct(This) ) 

#define IVwRootBox_OnStylesheetChange(This)	\
    ( (This)->lpVtbl -> OnStylesheetChange(This) ) 

#define IVwRootBox_DrawingErrors(This,pvg)	\
    ( (This)->lpVtbl -> DrawingErrors(This,pvg) ) 

#define IVwRootBox_get_Stylesheet(This,ppvss)	\
    ( (This)->lpVtbl -> get_Stylesheet(This,ppvss) ) 

#define IVwRootBox_SetTableColWidths(This,prgvlen,cvlen)	\
    ( (This)->lpVtbl -> SetTableColWidths(This,prgvlen,cvlen) ) 

#define IVwRootBox_IsDirty(This,pfDirty)	\
    ( (This)->lpVtbl -> IsDirty(This,pfDirty) ) 

#define IVwRootBox_get_XdPos(This,pxdPos)	\
    ( (This)->lpVtbl -> get_XdPos(This,pxdPos) ) 

#define IVwRootBox_get_Synchronizer(This,ppsync)	\
    ( (This)->lpVtbl -> get_Synchronizer(This,ppsync) ) 

#define IVwRootBox_GetRootObject(This,phvo,ppvwvc,pfrag,ppss)	\
    ( (This)->lpVtbl -> GetRootObject(This,phvo,ppvwvc,pfrag,ppss) ) 

#define IVwRootBox_DrawRoot2(This,pvg,rcSrc,rcDst,fDrawSel,ysTop,dysHeight)	\
    ( (This)->lpVtbl -> DrawRoot2(This,pvg,rcSrc,rcDst,fDrawSel,ysTop,dysHeight) ) 

#define IVwRootBox_get_MaxParasToScan(This,pcParas)	\
    ( (This)->lpVtbl -> get_MaxParasToScan(This,pcParas) ) 

#define IVwRootBox_put_MaxParasToScan(This,cParas)	\
    ( (This)->lpVtbl -> put_MaxParasToScan(This,cParas) ) 

#define IVwRootBox_DoSpellCheckStep(This,pfComplete)	\
    ( (This)->lpVtbl -> DoSpellCheckStep(This,pfComplete) ) 

#define IVwRootBox_IsSpellCheckComplete(This,pfComplete)	\
    ( (This)->lpVtbl -> IsSpellCheckComplete(This,pfComplete) ) 

#define IVwRootBox_get_IsCompositionInProgress(This,pfInProgress)	\
    ( (This)->lpVtbl -> get_IsCompositionInProgress(This,pfInProgress) ) 

#define IVwRootBox_get_IsPropChangedInProgress(This,pfInProgress)	\
    ( (This)->lpVtbl -> get_IsPropChangedInProgress(This,pfInProgress) ) 

#define IVwRootBox_RestartSpellChecking(This)	\
    ( (This)->lpVtbl -> RestartSpellChecking(This) ) 

#define IVwRootBox_SetSpellingRepository(This,pgsp)	\
    ( (This)->lpVtbl -> SetSpellingRepository(This,pgsp) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IVwRootBox_INTERFACE_DEFINED__ */


#ifndef __IVwPropertyStore_INTERFACE_DEFINED__
#define __IVwPropertyStore_INTERFACE_DEFINED__

/* interface IVwPropertyStore */
/* [unique][object][uuid] */ 


#define IID_IVwPropertyStore __uuidof(IVwPropertyStore)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("3D4847FE-EA2D-4255-A496-770059A134CC")
    IVwPropertyStore : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IntProperty( 
            /* [in] */ int nID,
            /* [retval][out] */ int *pnValue) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_StringProperty( 
            /* [in] */ int sp,
            /* [retval][out] */ BSTR *bstrValue) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ChrpFor( 
            /* [in] */ /* external definition not present */ ITsTextProps *pttp,
            /* [retval][out] */ /* external definition not present */ LgCharRenderProps *pchrp) = 0;
        
        virtual /* [propputref] */ HRESULT STDMETHODCALLTYPE putref_Stylesheet( 
            /* [in] */ /* external definition not present */ IVwStylesheet *pvps) = 0;
        
        virtual /* [propputref] */ HRESULT STDMETHODCALLTYPE putref_WritingSystemFactory( 
            /* [in] */ /* external definition not present */ ILgWritingSystemFactory *pwsf) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ParentStore( 
            /* [retval][out] */ IVwPropertyStore **ppvps) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_TextProps( 
            /* [retval][out] */ /* external definition not present */ ITsTextProps **ppttp) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_DerivedPropertiesForTtp( 
            /* [in] */ /* external definition not present */ ITsTextProps *pttp,
            /* [retval][out] */ IVwPropertyStore **ppvps) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IVwPropertyStoreVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IVwPropertyStore * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IVwPropertyStore * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IVwPropertyStore * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IntProperty )( 
            IVwPropertyStore * This,
            /* [in] */ int nID,
            /* [retval][out] */ int *pnValue);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_StringProperty )( 
            IVwPropertyStore * This,
            /* [in] */ int sp,
            /* [retval][out] */ BSTR *bstrValue);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ChrpFor )( 
            IVwPropertyStore * This,
            /* [in] */ /* external definition not present */ ITsTextProps *pttp,
            /* [retval][out] */ /* external definition not present */ LgCharRenderProps *pchrp);
        
        /* [propputref] */ HRESULT ( STDMETHODCALLTYPE *putref_Stylesheet )( 
            IVwPropertyStore * This,
            /* [in] */ /* external definition not present */ IVwStylesheet *pvps);
        
        /* [propputref] */ HRESULT ( STDMETHODCALLTYPE *putref_WritingSystemFactory )( 
            IVwPropertyStore * This,
            /* [in] */ /* external definition not present */ ILgWritingSystemFactory *pwsf);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ParentStore )( 
            IVwPropertyStore * This,
            /* [retval][out] */ IVwPropertyStore **ppvps);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_TextProps )( 
            IVwPropertyStore * This,
            /* [retval][out] */ /* external definition not present */ ITsTextProps **ppttp);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_DerivedPropertiesForTtp )( 
            IVwPropertyStore * This,
            /* [in] */ /* external definition not present */ ITsTextProps *pttp,
            /* [retval][out] */ IVwPropertyStore **ppvps);
        
        END_INTERFACE
    } IVwPropertyStoreVtbl;

    interface IVwPropertyStore
    {
        CONST_VTBL struct IVwPropertyStoreVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IVwPropertyStore_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IVwPropertyStore_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IVwPropertyStore_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IVwPropertyStore_get_IntProperty(This,nID,pnValue)	\
    ( (This)->lpVtbl -> get_IntProperty(This,nID,pnValue) ) 

#define IVwPropertyStore_get_StringProperty(This,sp,bstrValue)	\
    ( (This)->lpVtbl -> get_StringProperty(This,sp,bstrValue) ) 

#define IVwPropertyStore_get_ChrpFor(This,pttp,pchrp)	\
    ( (This)->lpVtbl -> get_ChrpFor(This,pttp,pchrp) ) 

#define IVwPropertyStore_putref_Stylesheet(This,pvps)	\
    ( (This)->lpVtbl -> putref_Stylesheet(This,pvps) ) 

#define IVwPropertyStore_putref_WritingSystemFactory(This,pwsf)	\
    ( (This)->lpVtbl -> putref_WritingSystemFactory(This,pwsf) ) 

#define IVwPropertyStore_get_ParentStore(This,ppvps)	\
    ( (This)->lpVtbl -> get_ParentStore(This,ppvps) ) 

#define IVwPropertyStore_get_TextProps(This,ppttp)	\
    ( (This)->lpVtbl -> get_TextProps(This,ppttp) ) 

#define IVwPropertyStore_get_DerivedPropertiesForTtp(This,pttp,ppvps)	\
    ( (This)->lpVtbl -> get_DerivedPropertiesForTtp(This,pttp,ppvps) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IVwPropertyStore_INTERFACE_DEFINED__ */


#ifndef __IVwOverlay_INTERFACE_DEFINED__
#define __IVwOverlay_INTERFACE_DEFINED__

/* interface IVwOverlay */
/* [unique][object][uuid] */ 


#define IID_IVwOverlay __uuidof(IVwOverlay)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("7D9089C1-3BB9-11d4-8078-0000C0FB81B5")
    IVwOverlay : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Name( 
            /* [retval][out] */ BSTR *pbstr) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_Name( 
            /* [in] */ BSTR bstr) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Guid( 
            /* [retval][size_is][out] */ OLECHAR *prgchGuid) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_Guid( 
            /* [size_is][in] */ OLECHAR *prgchGuid) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_PossListId( 
            /* [retval][out] */ HVO *ppsslId) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_PossListId( 
            /* [in] */ HVO psslId) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Flags( 
            /* [retval][out] */ VwOverlayFlags *pvof) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_Flags( 
            /* [in] */ VwOverlayFlags vof) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_FontName( 
            /* [retval][out] */ BSTR *pbstr) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_FontName( 
            /* [in] */ BSTR bstr) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE FontNameRgch( 
            /* [size_is][out] */ OLECHAR *prgch) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_FontSize( 
            /* [retval][out] */ int *pmp) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_FontSize( 
            /* [in] */ int mp) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_MaxShowTags( 
            /* [retval][out] */ int *pctag) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_MaxShowTags( 
            /* [in] */ int ctag) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_CTags( 
            /* [retval][out] */ int *pctag) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetDbTagInfo( 
            /* [in] */ int itag,
            /* [out] */ HVO *phvo,
            /* [out] */ COLORREF *pclrFore,
            /* [out] */ COLORREF *pclrBack,
            /* [out] */ COLORREF *pclrUnder,
            /* [out] */ int *punt,
            /* [out] */ ComBool *pfHidden,
            /* [size_is][out] */ OLECHAR *prgchGuid) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetTagInfo( 
            /* [size_is][in] */ OLECHAR *prgchGuid,
            /* [in] */ HVO hvo,
            /* [in] */ int grfosm,
            /* [in] */ BSTR bstrAbbr,
            /* [in] */ BSTR bstrName,
            /* [in] */ COLORREF clrFore,
            /* [in] */ COLORREF clrBack,
            /* [in] */ COLORREF clrUnder,
            /* [in] */ int unt,
            /* [in] */ ComBool fHidden) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetTagInfo( 
            /* [size_is][in] */ OLECHAR *prgchGuid,
            /* [out] */ HVO *phvo,
            /* [out] */ BSTR *pbstrAbbr,
            /* [out] */ BSTR *pbstrName,
            /* [out] */ COLORREF *pclrFore,
            /* [out] */ COLORREF *pclrBack,
            /* [out] */ COLORREF *pclrUnder,
            /* [out] */ int *punt,
            /* [out] */ ComBool *pfHidden) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetDlgTagInfo( 
            /* [in] */ int itag,
            /* [out] */ ComBool *pfHidden,
            /* [out] */ COLORREF *pclrFore,
            /* [out] */ COLORREF *pclrBack,
            /* [out] */ COLORREF *pclrUnder,
            /* [out] */ int *punt,
            /* [out] */ BSTR *pbstrAbbr,
            /* [out] */ BSTR *pbstrName) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetDispTagInfo( 
            /* [size_is][in] */ OLECHAR *prgchGuid,
            /* [out] */ ComBool *pfHidden,
            /* [out] */ COLORREF *pclrFore,
            /* [out] */ COLORREF *pclrBack,
            /* [out] */ COLORREF *pclrUnder,
            /* [out] */ int *punt,
            /* [size_is][out] */ OLECHAR *prgchAbbr,
            /* [in] */ int cchMaxAbbr,
            /* [out] */ int *pcchAbbr,
            /* [size_is][out] */ OLECHAR *prgchName,
            /* [in] */ int cchMaxName,
            /* [out] */ int *pcchName) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE RemoveTag( 
            /* [size_is][in] */ OLECHAR *prgchGuid) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Sort( 
            /* [in] */ ComBool fByAbbr) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Merge( 
            /* [in] */ IVwOverlay *pvo,
            /* [retval][out] */ IVwOverlay **ppvoMerged) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IVwOverlayVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IVwOverlay * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IVwOverlay * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IVwOverlay * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Name )( 
            IVwOverlay * This,
            /* [retval][out] */ BSTR *pbstr);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_Name )( 
            IVwOverlay * This,
            /* [in] */ BSTR bstr);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Guid )( 
            IVwOverlay * This,
            /* [retval][size_is][out] */ OLECHAR *prgchGuid);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_Guid )( 
            IVwOverlay * This,
            /* [size_is][in] */ OLECHAR *prgchGuid);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_PossListId )( 
            IVwOverlay * This,
            /* [retval][out] */ HVO *ppsslId);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_PossListId )( 
            IVwOverlay * This,
            /* [in] */ HVO psslId);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Flags )( 
            IVwOverlay * This,
            /* [retval][out] */ VwOverlayFlags *pvof);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_Flags )( 
            IVwOverlay * This,
            /* [in] */ VwOverlayFlags vof);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_FontName )( 
            IVwOverlay * This,
            /* [retval][out] */ BSTR *pbstr);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_FontName )( 
            IVwOverlay * This,
            /* [in] */ BSTR bstr);
        
        HRESULT ( STDMETHODCALLTYPE *FontNameRgch )( 
            IVwOverlay * This,
            /* [size_is][out] */ OLECHAR *prgch);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_FontSize )( 
            IVwOverlay * This,
            /* [retval][out] */ int *pmp);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_FontSize )( 
            IVwOverlay * This,
            /* [in] */ int mp);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_MaxShowTags )( 
            IVwOverlay * This,
            /* [retval][out] */ int *pctag);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_MaxShowTags )( 
            IVwOverlay * This,
            /* [in] */ int ctag);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_CTags )( 
            IVwOverlay * This,
            /* [retval][out] */ int *pctag);
        
        HRESULT ( STDMETHODCALLTYPE *GetDbTagInfo )( 
            IVwOverlay * This,
            /* [in] */ int itag,
            /* [out] */ HVO *phvo,
            /* [out] */ COLORREF *pclrFore,
            /* [out] */ COLORREF *pclrBack,
            /* [out] */ COLORREF *pclrUnder,
            /* [out] */ int *punt,
            /* [out] */ ComBool *pfHidden,
            /* [size_is][out] */ OLECHAR *prgchGuid);
        
        HRESULT ( STDMETHODCALLTYPE *SetTagInfo )( 
            IVwOverlay * This,
            /* [size_is][in] */ OLECHAR *prgchGuid,
            /* [in] */ HVO hvo,
            /* [in] */ int grfosm,
            /* [in] */ BSTR bstrAbbr,
            /* [in] */ BSTR bstrName,
            /* [in] */ COLORREF clrFore,
            /* [in] */ COLORREF clrBack,
            /* [in] */ COLORREF clrUnder,
            /* [in] */ int unt,
            /* [in] */ ComBool fHidden);
        
        HRESULT ( STDMETHODCALLTYPE *GetTagInfo )( 
            IVwOverlay * This,
            /* [size_is][in] */ OLECHAR *prgchGuid,
            /* [out] */ HVO *phvo,
            /* [out] */ BSTR *pbstrAbbr,
            /* [out] */ BSTR *pbstrName,
            /* [out] */ COLORREF *pclrFore,
            /* [out] */ COLORREF *pclrBack,
            /* [out] */ COLORREF *pclrUnder,
            /* [out] */ int *punt,
            /* [out] */ ComBool *pfHidden);
        
        HRESULT ( STDMETHODCALLTYPE *GetDlgTagInfo )( 
            IVwOverlay * This,
            /* [in] */ int itag,
            /* [out] */ ComBool *pfHidden,
            /* [out] */ COLORREF *pclrFore,
            /* [out] */ COLORREF *pclrBack,
            /* [out] */ COLORREF *pclrUnder,
            /* [out] */ int *punt,
            /* [out] */ BSTR *pbstrAbbr,
            /* [out] */ BSTR *pbstrName);
        
        HRESULT ( STDMETHODCALLTYPE *GetDispTagInfo )( 
            IVwOverlay * This,
            /* [size_is][in] */ OLECHAR *prgchGuid,
            /* [out] */ ComBool *pfHidden,
            /* [out] */ COLORREF *pclrFore,
            /* [out] */ COLORREF *pclrBack,
            /* [out] */ COLORREF *pclrUnder,
            /* [out] */ int *punt,
            /* [size_is][out] */ OLECHAR *prgchAbbr,
            /* [in] */ int cchMaxAbbr,
            /* [out] */ int *pcchAbbr,
            /* [size_is][out] */ OLECHAR *prgchName,
            /* [in] */ int cchMaxName,
            /* [out] */ int *pcchName);
        
        HRESULT ( STDMETHODCALLTYPE *RemoveTag )( 
            IVwOverlay * This,
            /* [size_is][in] */ OLECHAR *prgchGuid);
        
        HRESULT ( STDMETHODCALLTYPE *Sort )( 
            IVwOverlay * This,
            /* [in] */ ComBool fByAbbr);
        
        HRESULT ( STDMETHODCALLTYPE *Merge )( 
            IVwOverlay * This,
            /* [in] */ IVwOverlay *pvo,
            /* [retval][out] */ IVwOverlay **ppvoMerged);
        
        END_INTERFACE
    } IVwOverlayVtbl;

    interface IVwOverlay
    {
        CONST_VTBL struct IVwOverlayVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IVwOverlay_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IVwOverlay_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IVwOverlay_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IVwOverlay_get_Name(This,pbstr)	\
    ( (This)->lpVtbl -> get_Name(This,pbstr) ) 

#define IVwOverlay_put_Name(This,bstr)	\
    ( (This)->lpVtbl -> put_Name(This,bstr) ) 

#define IVwOverlay_get_Guid(This,prgchGuid)	\
    ( (This)->lpVtbl -> get_Guid(This,prgchGuid) ) 

#define IVwOverlay_put_Guid(This,prgchGuid)	\
    ( (This)->lpVtbl -> put_Guid(This,prgchGuid) ) 

#define IVwOverlay_get_PossListId(This,ppsslId)	\
    ( (This)->lpVtbl -> get_PossListId(This,ppsslId) ) 

#define IVwOverlay_put_PossListId(This,psslId)	\
    ( (This)->lpVtbl -> put_PossListId(This,psslId) ) 

#define IVwOverlay_get_Flags(This,pvof)	\
    ( (This)->lpVtbl -> get_Flags(This,pvof) ) 

#define IVwOverlay_put_Flags(This,vof)	\
    ( (This)->lpVtbl -> put_Flags(This,vof) ) 

#define IVwOverlay_get_FontName(This,pbstr)	\
    ( (This)->lpVtbl -> get_FontName(This,pbstr) ) 

#define IVwOverlay_put_FontName(This,bstr)	\
    ( (This)->lpVtbl -> put_FontName(This,bstr) ) 

#define IVwOverlay_FontNameRgch(This,prgch)	\
    ( (This)->lpVtbl -> FontNameRgch(This,prgch) ) 

#define IVwOverlay_get_FontSize(This,pmp)	\
    ( (This)->lpVtbl -> get_FontSize(This,pmp) ) 

#define IVwOverlay_put_FontSize(This,mp)	\
    ( (This)->lpVtbl -> put_FontSize(This,mp) ) 

#define IVwOverlay_get_MaxShowTags(This,pctag)	\
    ( (This)->lpVtbl -> get_MaxShowTags(This,pctag) ) 

#define IVwOverlay_put_MaxShowTags(This,ctag)	\
    ( (This)->lpVtbl -> put_MaxShowTags(This,ctag) ) 

#define IVwOverlay_get_CTags(This,pctag)	\
    ( (This)->lpVtbl -> get_CTags(This,pctag) ) 

#define IVwOverlay_GetDbTagInfo(This,itag,phvo,pclrFore,pclrBack,pclrUnder,punt,pfHidden,prgchGuid)	\
    ( (This)->lpVtbl -> GetDbTagInfo(This,itag,phvo,pclrFore,pclrBack,pclrUnder,punt,pfHidden,prgchGuid) ) 

#define IVwOverlay_SetTagInfo(This,prgchGuid,hvo,grfosm,bstrAbbr,bstrName,clrFore,clrBack,clrUnder,unt,fHidden)	\
    ( (This)->lpVtbl -> SetTagInfo(This,prgchGuid,hvo,grfosm,bstrAbbr,bstrName,clrFore,clrBack,clrUnder,unt,fHidden) ) 

#define IVwOverlay_GetTagInfo(This,prgchGuid,phvo,pbstrAbbr,pbstrName,pclrFore,pclrBack,pclrUnder,punt,pfHidden)	\
    ( (This)->lpVtbl -> GetTagInfo(This,prgchGuid,phvo,pbstrAbbr,pbstrName,pclrFore,pclrBack,pclrUnder,punt,pfHidden) ) 

#define IVwOverlay_GetDlgTagInfo(This,itag,pfHidden,pclrFore,pclrBack,pclrUnder,punt,pbstrAbbr,pbstrName)	\
    ( (This)->lpVtbl -> GetDlgTagInfo(This,itag,pfHidden,pclrFore,pclrBack,pclrUnder,punt,pbstrAbbr,pbstrName) ) 

#define IVwOverlay_GetDispTagInfo(This,prgchGuid,pfHidden,pclrFore,pclrBack,pclrUnder,punt,prgchAbbr,cchMaxAbbr,pcchAbbr,prgchName,cchMaxName,pcchName)	\
    ( (This)->lpVtbl -> GetDispTagInfo(This,prgchGuid,pfHidden,pclrFore,pclrBack,pclrUnder,punt,prgchAbbr,cchMaxAbbr,pcchAbbr,prgchName,cchMaxName,pcchName) ) 

#define IVwOverlay_RemoveTag(This,prgchGuid)	\
    ( (This)->lpVtbl -> RemoveTag(This,prgchGuid) ) 

#define IVwOverlay_Sort(This,fByAbbr)	\
    ( (This)->lpVtbl -> Sort(This,fByAbbr) ) 

#define IVwOverlay_Merge(This,pvo,ppvoMerged)	\
    ( (This)->lpVtbl -> Merge(This,pvo,ppvoMerged) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IVwOverlay_INTERFACE_DEFINED__ */


#ifndef __IVwPrintContext_INTERFACE_DEFINED__
#define __IVwPrintContext_INTERFACE_DEFINED__

/* interface IVwPrintContext */
/* [unique][object][uuid] */ 


#define IID_IVwPrintContext __uuidof(IVwPrintContext)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("FF2E1DC2-95A8-41c6-85F4-FFCA3A64216A")
    IVwPrintContext : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Graphics( 
            /* [retval][out] */ IVwGraphics **ppvg) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_FirstPageNumber( 
            /* [retval][out] */ int *pn) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsPageWanted( 
            /* [in] */ int nPageNo,
            /* [retval][out] */ ComBool *pfWanted) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AreMorePagesWanted( 
            /* [in] */ int nPageNo,
            /* [retval][out] */ ComBool *pfWanted) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Aborted( 
            /* [retval][out] */ ComBool *pfAborted) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Copies( 
            /* [retval][out] */ int *pnCopies) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Collate( 
            /* [retval][out] */ ComBool *pfCollate) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_HeaderString( 
            /* [in] */ VwHeaderPositions grfvhp,
            /* [in] */ int pn,
            /* [retval][out] */ /* external definition not present */ ITsString **pptss) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetMargins( 
            /* [out] */ int *pdxpLeft,
            /* [out] */ int *pdxpRight,
            /* [out] */ int *pdypHeader,
            /* [out] */ int *pdypTop,
            /* [out] */ int *pdypBottom,
            /* [out] */ int *pdypFooter) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE OpenPage( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ClosePage( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE OpenDoc( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CloseDoc( void) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_LastPageNo( 
            /* [retval][out] */ int *pnPageNo) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_HeaderMask( 
            /* [in] */ VwHeaderPositions grfvhp) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetHeaderString( 
            /* [in] */ VwHeaderPositions grfvhp,
            /* [in] */ /* external definition not present */ ITsString *ptss) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetMargins( 
            /* [in] */ int dxpLeft,
            /* [in] */ int dxpRight,
            /* [in] */ int dypHeader,
            /* [in] */ int dypTop,
            /* [in] */ int dypBottom,
            /* [in] */ int dypFooter) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetPagePrintInfo( 
            /* [in] */ int nFirstPageNo,
            /* [in] */ int nFirstPrintPage,
            /* [in] */ int nLastPrintPage,
            /* [in] */ int nCopies,
            /* [in] */ ComBool fCollate) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetGraphics( 
            /* [in] */ IVwGraphics *pvg) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE RequestAbort( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AbortDoc( void) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IVwPrintContextVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IVwPrintContext * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IVwPrintContext * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IVwPrintContext * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Graphics )( 
            IVwPrintContext * This,
            /* [retval][out] */ IVwGraphics **ppvg);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_FirstPageNumber )( 
            IVwPrintContext * This,
            /* [retval][out] */ int *pn);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsPageWanted )( 
            IVwPrintContext * This,
            /* [in] */ int nPageNo,
            /* [retval][out] */ ComBool *pfWanted);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AreMorePagesWanted )( 
            IVwPrintContext * This,
            /* [in] */ int nPageNo,
            /* [retval][out] */ ComBool *pfWanted);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Aborted )( 
            IVwPrintContext * This,
            /* [retval][out] */ ComBool *pfAborted);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Copies )( 
            IVwPrintContext * This,
            /* [retval][out] */ int *pnCopies);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Collate )( 
            IVwPrintContext * This,
            /* [retval][out] */ ComBool *pfCollate);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_HeaderString )( 
            IVwPrintContext * This,
            /* [in] */ VwHeaderPositions grfvhp,
            /* [in] */ int pn,
            /* [retval][out] */ /* external definition not present */ ITsString **pptss);
        
        HRESULT ( STDMETHODCALLTYPE *GetMargins )( 
            IVwPrintContext * This,
            /* [out] */ int *pdxpLeft,
            /* [out] */ int *pdxpRight,
            /* [out] */ int *pdypHeader,
            /* [out] */ int *pdypTop,
            /* [out] */ int *pdypBottom,
            /* [out] */ int *pdypFooter);
        
        HRESULT ( STDMETHODCALLTYPE *OpenPage )( 
            IVwPrintContext * This);
        
        HRESULT ( STDMETHODCALLTYPE *ClosePage )( 
            IVwPrintContext * This);
        
        HRESULT ( STDMETHODCALLTYPE *OpenDoc )( 
            IVwPrintContext * This);
        
        HRESULT ( STDMETHODCALLTYPE *CloseDoc )( 
            IVwPrintContext * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_LastPageNo )( 
            IVwPrintContext * This,
            /* [retval][out] */ int *pnPageNo);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_HeaderMask )( 
            IVwPrintContext * This,
            /* [in] */ VwHeaderPositions grfvhp);
        
        HRESULT ( STDMETHODCALLTYPE *SetHeaderString )( 
            IVwPrintContext * This,
            /* [in] */ VwHeaderPositions grfvhp,
            /* [in] */ /* external definition not present */ ITsString *ptss);
        
        HRESULT ( STDMETHODCALLTYPE *SetMargins )( 
            IVwPrintContext * This,
            /* [in] */ int dxpLeft,
            /* [in] */ int dxpRight,
            /* [in] */ int dypHeader,
            /* [in] */ int dypTop,
            /* [in] */ int dypBottom,
            /* [in] */ int dypFooter);
        
        HRESULT ( STDMETHODCALLTYPE *SetPagePrintInfo )( 
            IVwPrintContext * This,
            /* [in] */ int nFirstPageNo,
            /* [in] */ int nFirstPrintPage,
            /* [in] */ int nLastPrintPage,
            /* [in] */ int nCopies,
            /* [in] */ ComBool fCollate);
        
        HRESULT ( STDMETHODCALLTYPE *SetGraphics )( 
            IVwPrintContext * This,
            /* [in] */ IVwGraphics *pvg);
        
        HRESULT ( STDMETHODCALLTYPE *RequestAbort )( 
            IVwPrintContext * This);
        
        HRESULT ( STDMETHODCALLTYPE *AbortDoc )( 
            IVwPrintContext * This);
        
        END_INTERFACE
    } IVwPrintContextVtbl;

    interface IVwPrintContext
    {
        CONST_VTBL struct IVwPrintContextVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IVwPrintContext_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IVwPrintContext_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IVwPrintContext_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IVwPrintContext_get_Graphics(This,ppvg)	\
    ( (This)->lpVtbl -> get_Graphics(This,ppvg) ) 

#define IVwPrintContext_get_FirstPageNumber(This,pn)	\
    ( (This)->lpVtbl -> get_FirstPageNumber(This,pn) ) 

#define IVwPrintContext_get_IsPageWanted(This,nPageNo,pfWanted)	\
    ( (This)->lpVtbl -> get_IsPageWanted(This,nPageNo,pfWanted) ) 

#define IVwPrintContext_get_AreMorePagesWanted(This,nPageNo,pfWanted)	\
    ( (This)->lpVtbl -> get_AreMorePagesWanted(This,nPageNo,pfWanted) ) 

#define IVwPrintContext_get_Aborted(This,pfAborted)	\
    ( (This)->lpVtbl -> get_Aborted(This,pfAborted) ) 

#define IVwPrintContext_get_Copies(This,pnCopies)	\
    ( (This)->lpVtbl -> get_Copies(This,pnCopies) ) 

#define IVwPrintContext_get_Collate(This,pfCollate)	\
    ( (This)->lpVtbl -> get_Collate(This,pfCollate) ) 

#define IVwPrintContext_get_HeaderString(This,grfvhp,pn,pptss)	\
    ( (This)->lpVtbl -> get_HeaderString(This,grfvhp,pn,pptss) ) 

#define IVwPrintContext_GetMargins(This,pdxpLeft,pdxpRight,pdypHeader,pdypTop,pdypBottom,pdypFooter)	\
    ( (This)->lpVtbl -> GetMargins(This,pdxpLeft,pdxpRight,pdypHeader,pdypTop,pdypBottom,pdypFooter) ) 

#define IVwPrintContext_OpenPage(This)	\
    ( (This)->lpVtbl -> OpenPage(This) ) 

#define IVwPrintContext_ClosePage(This)	\
    ( (This)->lpVtbl -> ClosePage(This) ) 

#define IVwPrintContext_OpenDoc(This)	\
    ( (This)->lpVtbl -> OpenDoc(This) ) 

#define IVwPrintContext_CloseDoc(This)	\
    ( (This)->lpVtbl -> CloseDoc(This) ) 

#define IVwPrintContext_get_LastPageNo(This,pnPageNo)	\
    ( (This)->lpVtbl -> get_LastPageNo(This,pnPageNo) ) 

#define IVwPrintContext_put_HeaderMask(This,grfvhp)	\
    ( (This)->lpVtbl -> put_HeaderMask(This,grfvhp) ) 

#define IVwPrintContext_SetHeaderString(This,grfvhp,ptss)	\
    ( (This)->lpVtbl -> SetHeaderString(This,grfvhp,ptss) ) 

#define IVwPrintContext_SetMargins(This,dxpLeft,dxpRight,dypHeader,dypTop,dypBottom,dypFooter)	\
    ( (This)->lpVtbl -> SetMargins(This,dxpLeft,dxpRight,dypHeader,dypTop,dypBottom,dypFooter) ) 

#define IVwPrintContext_SetPagePrintInfo(This,nFirstPageNo,nFirstPrintPage,nLastPrintPage,nCopies,fCollate)	\
    ( (This)->lpVtbl -> SetPagePrintInfo(This,nFirstPageNo,nFirstPrintPage,nLastPrintPage,nCopies,fCollate) ) 

#define IVwPrintContext_SetGraphics(This,pvg)	\
    ( (This)->lpVtbl -> SetGraphics(This,pvg) ) 

#define IVwPrintContext_RequestAbort(This)	\
    ( (This)->lpVtbl -> RequestAbort(This) ) 

#define IVwPrintContext_AbortDoc(This)	\
    ( (This)->lpVtbl -> AbortDoc(This) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IVwPrintContext_INTERFACE_DEFINED__ */


#ifndef __IVwSearchKiller_INTERFACE_DEFINED__
#define __IVwSearchKiller_INTERFACE_DEFINED__

/* interface IVwSearchKiller */
/* [unique][object][uuid] */ 


#define IID_IVwSearchKiller __uuidof(IVwSearchKiller)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("FF1B39DE-20D3-4cdd-A134-DCBE3BE23F3E")
    IVwSearchKiller : public IUnknown
    {
    public:
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_Window( 
            /* [in] */ int hwnd) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE FlushMessages( void) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_AbortRequest( 
            /* [retval][out] */ ComBool *pfAbort) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_AbortRequest( 
            /* [in] */ ComBool fAbort) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IVwSearchKillerVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IVwSearchKiller * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IVwSearchKiller * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IVwSearchKiller * This);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_Window )( 
            IVwSearchKiller * This,
            /* [in] */ int hwnd);
        
        HRESULT ( STDMETHODCALLTYPE *FlushMessages )( 
            IVwSearchKiller * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_AbortRequest )( 
            IVwSearchKiller * This,
            /* [retval][out] */ ComBool *pfAbort);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_AbortRequest )( 
            IVwSearchKiller * This,
            /* [in] */ ComBool fAbort);
        
        END_INTERFACE
    } IVwSearchKillerVtbl;

    interface IVwSearchKiller
    {
        CONST_VTBL struct IVwSearchKillerVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IVwSearchKiller_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IVwSearchKiller_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IVwSearchKiller_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IVwSearchKiller_put_Window(This,hwnd)	\
    ( (This)->lpVtbl -> put_Window(This,hwnd) ) 

#define IVwSearchKiller_FlushMessages(This)	\
    ( (This)->lpVtbl -> FlushMessages(This) ) 

#define IVwSearchKiller_get_AbortRequest(This,pfAbort)	\
    ( (This)->lpVtbl -> get_AbortRequest(This,pfAbort) ) 

#define IVwSearchKiller_put_AbortRequest(This,fAbort)	\
    ( (This)->lpVtbl -> put_AbortRequest(This,fAbort) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IVwSearchKiller_INTERFACE_DEFINED__ */


#ifndef __IVwSynchronizer_INTERFACE_DEFINED__
#define __IVwSynchronizer_INTERFACE_DEFINED__

/* interface IVwSynchronizer */
/* [unique][object][uuid] */ 


#define IID_IVwSynchronizer __uuidof(IVwSynchronizer)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("C5C1E9DC-5880-4ee3-B3CD-EBDD132A6294")
    IVwSynchronizer : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE AddRoot( 
            /* [in] */ IVwRootBox *prootb) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsExpandingLazyItems( 
            /* [retval][out] */ ComBool *fAlreadyExpandingItems) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IVwSynchronizerVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IVwSynchronizer * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IVwSynchronizer * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IVwSynchronizer * This);
        
        HRESULT ( STDMETHODCALLTYPE *AddRoot )( 
            IVwSynchronizer * This,
            /* [in] */ IVwRootBox *prootb);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsExpandingLazyItems )( 
            IVwSynchronizer * This,
            /* [retval][out] */ ComBool *fAlreadyExpandingItems);
        
        END_INTERFACE
    } IVwSynchronizerVtbl;

    interface IVwSynchronizer
    {
        CONST_VTBL struct IVwSynchronizerVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IVwSynchronizer_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IVwSynchronizer_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IVwSynchronizer_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IVwSynchronizer_AddRoot(This,prootb)	\
    ( (This)->lpVtbl -> AddRoot(This,prootb) ) 

#define IVwSynchronizer_get_IsExpandingLazyItems(This,fAlreadyExpandingItems)	\
    ( (This)->lpVtbl -> get_IsExpandingLazyItems(This,fAlreadyExpandingItems) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IVwSynchronizer_INTERFACE_DEFINED__ */


#ifndef __IVwVirtualHandler_INTERFACE_DEFINED__
#define __IVwVirtualHandler_INTERFACE_DEFINED__

/* interface IVwVirtualHandler */
/* [unique][object][uuid] */ 


#define IID_IVwVirtualHandler __uuidof(IVwVirtualHandler)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("581E3FE0-F0C0-42A7-96C7-76B23B8BE580")
    IVwVirtualHandler : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ClassName( 
            /* [retval][out] */ BSTR *pbstr) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_ClassName( 
            /* [in] */ BSTR bstr) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_FieldName( 
            /* [retval][out] */ BSTR *pbstr) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_FieldName( 
            /* [in] */ BSTR bstr) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Tag( 
            /* [retval][out] */ PropTag *ptag) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_Tag( 
            /* [in] */ PropTag tag) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Type( 
            /* [retval][out] */ int *pcpt) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_Type( 
            /* [in] */ int cpt) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Writeable( 
            /* [retval][out] */ ComBool *pf) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_Writeable( 
            /* [in] */ ComBool f) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ComputeEveryTime( 
            /* [retval][out] */ ComBool *pf) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_ComputeEveryTime( 
            /* [in] */ ComBool f) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Load( 
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [in] */ int ws,
            /* [in] */ IVwCacheDa *pcda) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Replace( 
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [in] */ int ihvoMin,
            /* [in] */ int ihvoLim,
            /* [size_is][in] */ HVO *prghvo,
            /* [in] */ int chvo,
            /* [in] */ /* external definition not present */ ISilDataAccess *psda) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE WriteObj( 
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [in] */ int ws,
            /* [in] */ IUnknown *punk,
            /* [in] */ /* external definition not present */ ISilDataAccess *psda) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE WriteInt64( 
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [in] */ __int64 val,
            /* [in] */ /* external definition not present */ ISilDataAccess *psda) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE WriteUnicode( 
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [in] */ BSTR bstr,
            /* [in] */ /* external definition not present */ ISilDataAccess *psda) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE PreLoad( 
            /* [in] */ int chvo,
            /* [size_is][in] */ HVO *prghvo,
            /* [in] */ PropTag tag,
            /* [in] */ int ws,
            /* [in] */ IVwCacheDa *pcda) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Initialize( 
            /* [in] */ BSTR bstrData) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DoesResultDependOnProp( 
            /* [in] */ HVO hvoObj,
            /* [in] */ HVO hvoChange,
            /* [in] */ PropTag tag,
            /* [in] */ int ws,
            /* [retval][out] */ ComBool *pfDepends) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetLoadForAllOfClass( 
            /* [in] */ ComBool fLoadAll) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IVwVirtualHandlerVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IVwVirtualHandler * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IVwVirtualHandler * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IVwVirtualHandler * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ClassName )( 
            IVwVirtualHandler * This,
            /* [retval][out] */ BSTR *pbstr);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_ClassName )( 
            IVwVirtualHandler * This,
            /* [in] */ BSTR bstr);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_FieldName )( 
            IVwVirtualHandler * This,
            /* [retval][out] */ BSTR *pbstr);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_FieldName )( 
            IVwVirtualHandler * This,
            /* [in] */ BSTR bstr);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Tag )( 
            IVwVirtualHandler * This,
            /* [retval][out] */ PropTag *ptag);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_Tag )( 
            IVwVirtualHandler * This,
            /* [in] */ PropTag tag);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Type )( 
            IVwVirtualHandler * This,
            /* [retval][out] */ int *pcpt);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_Type )( 
            IVwVirtualHandler * This,
            /* [in] */ int cpt);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Writeable )( 
            IVwVirtualHandler * This,
            /* [retval][out] */ ComBool *pf);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_Writeable )( 
            IVwVirtualHandler * This,
            /* [in] */ ComBool f);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ComputeEveryTime )( 
            IVwVirtualHandler * This,
            /* [retval][out] */ ComBool *pf);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_ComputeEveryTime )( 
            IVwVirtualHandler * This,
            /* [in] */ ComBool f);
        
        HRESULT ( STDMETHODCALLTYPE *Load )( 
            IVwVirtualHandler * This,
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [in] */ int ws,
            /* [in] */ IVwCacheDa *pcda);
        
        HRESULT ( STDMETHODCALLTYPE *Replace )( 
            IVwVirtualHandler * This,
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [in] */ int ihvoMin,
            /* [in] */ int ihvoLim,
            /* [size_is][in] */ HVO *prghvo,
            /* [in] */ int chvo,
            /* [in] */ /* external definition not present */ ISilDataAccess *psda);
        
        HRESULT ( STDMETHODCALLTYPE *WriteObj )( 
            IVwVirtualHandler * This,
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [in] */ int ws,
            /* [in] */ IUnknown *punk,
            /* [in] */ /* external definition not present */ ISilDataAccess *psda);
        
        HRESULT ( STDMETHODCALLTYPE *WriteInt64 )( 
            IVwVirtualHandler * This,
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [in] */ __int64 val,
            /* [in] */ /* external definition not present */ ISilDataAccess *psda);
        
        HRESULT ( STDMETHODCALLTYPE *WriteUnicode )( 
            IVwVirtualHandler * This,
            /* [in] */ HVO hvo,
            /* [in] */ PropTag tag,
            /* [in] */ BSTR bstr,
            /* [in] */ /* external definition not present */ ISilDataAccess *psda);
        
        HRESULT ( STDMETHODCALLTYPE *PreLoad )( 
            IVwVirtualHandler * This,
            /* [in] */ int chvo,
            /* [size_is][in] */ HVO *prghvo,
            /* [in] */ PropTag tag,
            /* [in] */ int ws,
            /* [in] */ IVwCacheDa *pcda);
        
        HRESULT ( STDMETHODCALLTYPE *Initialize )( 
            IVwVirtualHandler * This,
            /* [in] */ BSTR bstrData);
        
        HRESULT ( STDMETHODCALLTYPE *DoesResultDependOnProp )( 
            IVwVirtualHandler * This,
            /* [in] */ HVO hvoObj,
            /* [in] */ HVO hvoChange,
            /* [in] */ PropTag tag,
            /* [in] */ int ws,
            /* [retval][out] */ ComBool *pfDepends);
        
        HRESULT ( STDMETHODCALLTYPE *SetLoadForAllOfClass )( 
            IVwVirtualHandler * This,
            /* [in] */ ComBool fLoadAll);
        
        END_INTERFACE
    } IVwVirtualHandlerVtbl;

    interface IVwVirtualHandler
    {
        CONST_VTBL struct IVwVirtualHandlerVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IVwVirtualHandler_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IVwVirtualHandler_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IVwVirtualHandler_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IVwVirtualHandler_get_ClassName(This,pbstr)	\
    ( (This)->lpVtbl -> get_ClassName(This,pbstr) ) 

#define IVwVirtualHandler_put_ClassName(This,bstr)	\
    ( (This)->lpVtbl -> put_ClassName(This,bstr) ) 

#define IVwVirtualHandler_get_FieldName(This,pbstr)	\
    ( (This)->lpVtbl -> get_FieldName(This,pbstr) ) 

#define IVwVirtualHandler_put_FieldName(This,bstr)	\
    ( (This)->lpVtbl -> put_FieldName(This,bstr) ) 

#define IVwVirtualHandler_get_Tag(This,ptag)	\
    ( (This)->lpVtbl -> get_Tag(This,ptag) ) 

#define IVwVirtualHandler_put_Tag(This,tag)	\
    ( (This)->lpVtbl -> put_Tag(This,tag) ) 

#define IVwVirtualHandler_get_Type(This,pcpt)	\
    ( (This)->lpVtbl -> get_Type(This,pcpt) ) 

#define IVwVirtualHandler_put_Type(This,cpt)	\
    ( (This)->lpVtbl -> put_Type(This,cpt) ) 

#define IVwVirtualHandler_get_Writeable(This,pf)	\
    ( (This)->lpVtbl -> get_Writeable(This,pf) ) 

#define IVwVirtualHandler_put_Writeable(This,f)	\
    ( (This)->lpVtbl -> put_Writeable(This,f) ) 

#define IVwVirtualHandler_get_ComputeEveryTime(This,pf)	\
    ( (This)->lpVtbl -> get_ComputeEveryTime(This,pf) ) 

#define IVwVirtualHandler_put_ComputeEveryTime(This,f)	\
    ( (This)->lpVtbl -> put_ComputeEveryTime(This,f) ) 

#define IVwVirtualHandler_Load(This,hvo,tag,ws,pcda)	\
    ( (This)->lpVtbl -> Load(This,hvo,tag,ws,pcda) ) 

#define IVwVirtualHandler_Replace(This,hvo,tag,ihvoMin,ihvoLim,prghvo,chvo,psda)	\
    ( (This)->lpVtbl -> Replace(This,hvo,tag,ihvoMin,ihvoLim,prghvo,chvo,psda) ) 

#define IVwVirtualHandler_WriteObj(This,hvo,tag,ws,punk,psda)	\
    ( (This)->lpVtbl -> WriteObj(This,hvo,tag,ws,punk,psda) ) 

#define IVwVirtualHandler_WriteInt64(This,hvo,tag,val,psda)	\
    ( (This)->lpVtbl -> WriteInt64(This,hvo,tag,val,psda) ) 

#define IVwVirtualHandler_WriteUnicode(This,hvo,tag,bstr,psda)	\
    ( (This)->lpVtbl -> WriteUnicode(This,hvo,tag,bstr,psda) ) 

#define IVwVirtualHandler_PreLoad(This,chvo,prghvo,tag,ws,pcda)	\
    ( (This)->lpVtbl -> PreLoad(This,chvo,prghvo,tag,ws,pcda) ) 

#define IVwVirtualHandler_Initialize(This,bstrData)	\
    ( (This)->lpVtbl -> Initialize(This,bstrData) ) 

#define IVwVirtualHandler_DoesResultDependOnProp(This,hvoObj,hvoChange,tag,ws,pfDepends)	\
    ( (This)->lpVtbl -> DoesResultDependOnProp(This,hvoObj,hvoChange,tag,ws,pfDepends) ) 

#define IVwVirtualHandler_SetLoadForAllOfClass(This,fLoadAll)	\
    ( (This)->lpVtbl -> SetLoadForAllOfClass(This,fLoadAll) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IVwVirtualHandler_INTERFACE_DEFINED__ */


#ifndef __IVwLayoutStream_INTERFACE_DEFINED__
#define __IVwLayoutStream_INTERFACE_DEFINED__

/* interface IVwLayoutStream */
/* [unique][object][uuid] */ 


#define IID_IVwLayoutStream __uuidof(IVwLayoutStream)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("5DB26616-2741-4688-BC53-24C2A13ACB9A")
    IVwLayoutStream : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE SetManager( 
            /* [in] */ IVwLayoutManager *plm) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE LayoutObj( 
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ int dxsAvailWidth,
            /* [in] */ int ihvoRoot,
            /* [in] */ int cvsli,
            /* [size_is][in] */ VwSelLevInfo *prgvsli,
            /* [in] */ int hPage) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE LayoutPage( 
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ int dxsAvailWidth,
            /* [in] */ int dysAvailHeight,
            /* [out][in] */ int *pysStartThisPageBoundary,
            /* [in] */ int hPage,
            /* [in] */ int nColumns,
            /* [out] */ int *pdysUsedHeight,
            /* [out] */ int *pysStartNextPageBoundary) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DiscardPage( 
            /* [in] */ int hPage) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE PageBoundary( 
            /* [in] */ int hPage,
            /* [in] */ ComBool fEnd,
            /* [retval][out] */ IVwSelection **ppsel) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE PageHeight( 
            /* [in] */ int hPage,
            /* [retval][out] */ int *pdysHeight) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE PagePostion( 
            /* [in] */ int hPage,
            /* [retval][out] */ int *pysPosition) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE RollbackLayoutObjects( 
            /* [in] */ int hPage) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CommitLayoutObjects( 
            /* [in] */ int hPage) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ColumnHeight( 
            /* [in] */ int iColumn,
            /* [retval][out] */ int *pdysHeight) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ColumnOverlapWithPrevious( 
            /* [in] */ int iColumn,
            /* [retval][out] */ int *pdysHeight) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE IsInPageAbove( 
            /* [in] */ int xs,
            /* [in] */ int ys,
            /* [in] */ int ysBottomOfPage,
            /* [in] */ IVwGraphics *pvg,
            /* [out] */ int *pxsLeft,
            /* [out] */ int *pxsRight,
            /* [retval][out] */ ComBool *pfInLineAbove) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IVwLayoutStreamVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IVwLayoutStream * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IVwLayoutStream * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IVwLayoutStream * This);
        
        HRESULT ( STDMETHODCALLTYPE *SetManager )( 
            IVwLayoutStream * This,
            /* [in] */ IVwLayoutManager *plm);
        
        HRESULT ( STDMETHODCALLTYPE *LayoutObj )( 
            IVwLayoutStream * This,
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ int dxsAvailWidth,
            /* [in] */ int ihvoRoot,
            /* [in] */ int cvsli,
            /* [size_is][in] */ VwSelLevInfo *prgvsli,
            /* [in] */ int hPage);
        
        HRESULT ( STDMETHODCALLTYPE *LayoutPage )( 
            IVwLayoutStream * This,
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ int dxsAvailWidth,
            /* [in] */ int dysAvailHeight,
            /* [out][in] */ int *pysStartThisPageBoundary,
            /* [in] */ int hPage,
            /* [in] */ int nColumns,
            /* [out] */ int *pdysUsedHeight,
            /* [out] */ int *pysStartNextPageBoundary);
        
        HRESULT ( STDMETHODCALLTYPE *DiscardPage )( 
            IVwLayoutStream * This,
            /* [in] */ int hPage);
        
        HRESULT ( STDMETHODCALLTYPE *PageBoundary )( 
            IVwLayoutStream * This,
            /* [in] */ int hPage,
            /* [in] */ ComBool fEnd,
            /* [retval][out] */ IVwSelection **ppsel);
        
        HRESULT ( STDMETHODCALLTYPE *PageHeight )( 
            IVwLayoutStream * This,
            /* [in] */ int hPage,
            /* [retval][out] */ int *pdysHeight);
        
        HRESULT ( STDMETHODCALLTYPE *PagePostion )( 
            IVwLayoutStream * This,
            /* [in] */ int hPage,
            /* [retval][out] */ int *pysPosition);
        
        HRESULT ( STDMETHODCALLTYPE *RollbackLayoutObjects )( 
            IVwLayoutStream * This,
            /* [in] */ int hPage);
        
        HRESULT ( STDMETHODCALLTYPE *CommitLayoutObjects )( 
            IVwLayoutStream * This,
            /* [in] */ int hPage);
        
        HRESULT ( STDMETHODCALLTYPE *ColumnHeight )( 
            IVwLayoutStream * This,
            /* [in] */ int iColumn,
            /* [retval][out] */ int *pdysHeight);
        
        HRESULT ( STDMETHODCALLTYPE *ColumnOverlapWithPrevious )( 
            IVwLayoutStream * This,
            /* [in] */ int iColumn,
            /* [retval][out] */ int *pdysHeight);
        
        HRESULT ( STDMETHODCALLTYPE *IsInPageAbove )( 
            IVwLayoutStream * This,
            /* [in] */ int xs,
            /* [in] */ int ys,
            /* [in] */ int ysBottomOfPage,
            /* [in] */ IVwGraphics *pvg,
            /* [out] */ int *pxsLeft,
            /* [out] */ int *pxsRight,
            /* [retval][out] */ ComBool *pfInLineAbove);
        
        END_INTERFACE
    } IVwLayoutStreamVtbl;

    interface IVwLayoutStream
    {
        CONST_VTBL struct IVwLayoutStreamVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IVwLayoutStream_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IVwLayoutStream_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IVwLayoutStream_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IVwLayoutStream_SetManager(This,plm)	\
    ( (This)->lpVtbl -> SetManager(This,plm) ) 

#define IVwLayoutStream_LayoutObj(This,pvg,dxsAvailWidth,ihvoRoot,cvsli,prgvsli,hPage)	\
    ( (This)->lpVtbl -> LayoutObj(This,pvg,dxsAvailWidth,ihvoRoot,cvsli,prgvsli,hPage) ) 

#define IVwLayoutStream_LayoutPage(This,pvg,dxsAvailWidth,dysAvailHeight,pysStartThisPageBoundary,hPage,nColumns,pdysUsedHeight,pysStartNextPageBoundary)	\
    ( (This)->lpVtbl -> LayoutPage(This,pvg,dxsAvailWidth,dysAvailHeight,pysStartThisPageBoundary,hPage,nColumns,pdysUsedHeight,pysStartNextPageBoundary) ) 

#define IVwLayoutStream_DiscardPage(This,hPage)	\
    ( (This)->lpVtbl -> DiscardPage(This,hPage) ) 

#define IVwLayoutStream_PageBoundary(This,hPage,fEnd,ppsel)	\
    ( (This)->lpVtbl -> PageBoundary(This,hPage,fEnd,ppsel) ) 

#define IVwLayoutStream_PageHeight(This,hPage,pdysHeight)	\
    ( (This)->lpVtbl -> PageHeight(This,hPage,pdysHeight) ) 

#define IVwLayoutStream_PagePostion(This,hPage,pysPosition)	\
    ( (This)->lpVtbl -> PagePostion(This,hPage,pysPosition) ) 

#define IVwLayoutStream_RollbackLayoutObjects(This,hPage)	\
    ( (This)->lpVtbl -> RollbackLayoutObjects(This,hPage) ) 

#define IVwLayoutStream_CommitLayoutObjects(This,hPage)	\
    ( (This)->lpVtbl -> CommitLayoutObjects(This,hPage) ) 

#define IVwLayoutStream_ColumnHeight(This,iColumn,pdysHeight)	\
    ( (This)->lpVtbl -> ColumnHeight(This,iColumn,pdysHeight) ) 

#define IVwLayoutStream_ColumnOverlapWithPrevious(This,iColumn,pdysHeight)	\
    ( (This)->lpVtbl -> ColumnOverlapWithPrevious(This,iColumn,pdysHeight) ) 

#define IVwLayoutStream_IsInPageAbove(This,xs,ys,ysBottomOfPage,pvg,pxsLeft,pxsRight,pfInLineAbove)	\
    ( (This)->lpVtbl -> IsInPageAbove(This,xs,ys,ysBottomOfPage,pvg,pxsLeft,pxsRight,pfInLineAbove) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IVwLayoutStream_INTERFACE_DEFINED__ */


#ifndef __IVwLayoutManager_INTERFACE_DEFINED__
#define __IVwLayoutManager_INTERFACE_DEFINED__

/* interface IVwLayoutManager */
/* [unique][object][uuid] */ 


#define IID_IVwLayoutManager __uuidof(IVwLayoutManager)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("13F3A421-4915-455b-B57F-AFD4073CFFA0")
    IVwLayoutManager : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE AddDependentObjects( 
            /* [in] */ IVwLayoutStream *play,
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ int hPage,
            /* [in] */ int cguid,
            /* [size_is][in] */ GUID *prgguidObj,
            /* [in] */ ComBool fAllowFail,
            /* [out] */ ComBool *pfFailed,
            /* [out][in] */ int *pdysAvailHeight) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE PageBroken( 
            /* [in] */ IVwLayoutStream *play,
            /* [in] */ int hPage) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE PageBoundaryMoved( 
            /* [in] */ IVwLayoutStream *play,
            /* [in] */ int hPage,
            /* [in] */ int ichOld) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE EstimateHeight( 
            /* [in] */ int dxpWidth,
            /* [retval][out] */ int *pdxpHeight) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IVwLayoutManagerVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IVwLayoutManager * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IVwLayoutManager * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IVwLayoutManager * This);
        
        HRESULT ( STDMETHODCALLTYPE *AddDependentObjects )( 
            IVwLayoutManager * This,
            /* [in] */ IVwLayoutStream *play,
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ int hPage,
            /* [in] */ int cguid,
            /* [size_is][in] */ GUID *prgguidObj,
            /* [in] */ ComBool fAllowFail,
            /* [out] */ ComBool *pfFailed,
            /* [out][in] */ int *pdysAvailHeight);
        
        HRESULT ( STDMETHODCALLTYPE *PageBroken )( 
            IVwLayoutManager * This,
            /* [in] */ IVwLayoutStream *play,
            /* [in] */ int hPage);
        
        HRESULT ( STDMETHODCALLTYPE *PageBoundaryMoved )( 
            IVwLayoutManager * This,
            /* [in] */ IVwLayoutStream *play,
            /* [in] */ int hPage,
            /* [in] */ int ichOld);
        
        HRESULT ( STDMETHODCALLTYPE *EstimateHeight )( 
            IVwLayoutManager * This,
            /* [in] */ int dxpWidth,
            /* [retval][out] */ int *pdxpHeight);
        
        END_INTERFACE
    } IVwLayoutManagerVtbl;

    interface IVwLayoutManager
    {
        CONST_VTBL struct IVwLayoutManagerVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IVwLayoutManager_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IVwLayoutManager_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IVwLayoutManager_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IVwLayoutManager_AddDependentObjects(This,play,pvg,hPage,cguid,prgguidObj,fAllowFail,pfFailed,pdysAvailHeight)	\
    ( (This)->lpVtbl -> AddDependentObjects(This,play,pvg,hPage,cguid,prgguidObj,fAllowFail,pfFailed,pdysAvailHeight) ) 

#define IVwLayoutManager_PageBroken(This,play,hPage)	\
    ( (This)->lpVtbl -> PageBroken(This,play,hPage) ) 

#define IVwLayoutManager_PageBoundaryMoved(This,play,hPage,ichOld)	\
    ( (This)->lpVtbl -> PageBoundaryMoved(This,play,hPage,ichOld) ) 

#define IVwLayoutManager_EstimateHeight(This,dxpWidth,pdxpHeight)	\
    ( (This)->lpVtbl -> EstimateHeight(This,dxpWidth,pdxpHeight) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IVwLayoutManager_INTERFACE_DEFINED__ */


#define CLSID_VwCacheDa __uuidof(VwCacheDa)

#ifdef __cplusplus

class DECLSPEC_UUID("81EE73B1-BE31-49cf-BC02-6030113AC56F")
VwCacheDa;
#endif

#define CLSID_VwUndoDa __uuidof(VwUndoDa)

#ifdef __cplusplus

class DECLSPEC_UUID("5BEEFFC6-E88C-4258-A269-D58390A1F2C9")
VwUndoDa;
#endif

#define CLSID_VwRootBox __uuidof(VwRootBox)

#ifdef __cplusplus

class DECLSPEC_UUID("7C0C6A3C-38B3-4266-AF94-A3A1CBAAD1FC")
VwRootBox;
#endif

#define CLSID_VwInvertedRootBox __uuidof(VwInvertedRootBox)

#ifdef __cplusplus

class DECLSPEC_UUID("73BCAB14-2537-4b7d-B1C7-7E3DD7A089AD")
VwInvertedRootBox;
#endif

#define CLSID_VwPropertyStore __uuidof(VwPropertyStore)

#ifdef __cplusplus

class DECLSPEC_UUID("CB59916A-C532-4a57-8CB4-6E1508B4DEC1")
VwPropertyStore;
#endif

#define CLSID_VwOverlay __uuidof(VwOverlay)

#ifdef __cplusplus

class DECLSPEC_UUID("73F5DB01-3D2A-11d4-8078-0000C0FB81B5")
VwOverlay;
#endif

#define CLSID_VwPrintContextWin32 __uuidof(VwPrintContextWin32)

#ifdef __cplusplus

class DECLSPEC_UUID("5E9FB977-66AE-4c16-A036-1D40E7713573")
VwPrintContextWin32;
#endif

#ifndef __IVwPattern_INTERFACE_DEFINED__
#define __IVwPattern_INTERFACE_DEFINED__

/* interface IVwPattern */
/* [unique][object][uuid] */ 


#define IID_IVwPattern __uuidof(IVwPattern)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("EFEBBD00-D418-4157-A730-C648BFFF3D8D")
    IVwPattern : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Pattern( 
            /* [retval][out] */ /* external definition not present */ ITsString **pptssPattern) = 0;
        
        virtual /* [propputref] */ HRESULT STDMETHODCALLTYPE putref_Pattern( 
            /* [in] */ /* external definition not present */ ITsString *ptssPattern) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Overlay( 
            /* [retval][out] */ IVwOverlay **ppvo) = 0;
        
        virtual /* [propputref] */ HRESULT STDMETHODCALLTYPE putref_Overlay( 
            /* [in] */ IVwOverlay *pvo) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_MatchCase( 
            /* [retval][out] */ ComBool *pfMatch) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_MatchCase( 
            /* [in] */ ComBool fMatch) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_MatchDiacritics( 
            /* [retval][out] */ ComBool *pfMatch) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_MatchDiacritics( 
            /* [in] */ ComBool fMatch) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_MatchWholeWord( 
            /* [retval][out] */ ComBool *pfMatch) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_MatchWholeWord( 
            /* [in] */ ComBool fMatch) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_MatchOldWritingSystem( 
            /* [retval][out] */ ComBool *pfMatch) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_MatchOldWritingSystem( 
            /* [in] */ ComBool fMatch) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_MatchExactly( 
            /* [retval][out] */ ComBool *pfMatch) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_MatchExactly( 
            /* [in] */ ComBool fMatch) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_MatchCompatibility( 
            /* [retval][out] */ ComBool *pfMatch) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_MatchCompatibility( 
            /* [in] */ ComBool fMatch) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Find( 
            /* [in] */ IVwRootBox *prootb,
            /* [in] */ ComBool fForward,
            /* [in] */ IVwSearchKiller *pxserkl) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE FindFrom( 
            /* [in] */ IVwSelection *psel,
            /* [in] */ ComBool fForward,
            /* [in] */ IVwSearchKiller *pxserkl) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE FindNext( 
            /* [in] */ ComBool fForward,
            /* [in] */ IVwSearchKiller *pxserkl) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE FindIn( 
            /* [in] */ IVwTextSource *pts,
            /* [in] */ int ichStartLog,
            /* [in] */ int ichEndLog,
            /* [in] */ ComBool fForward,
            /* [out] */ int *pichMinFoundLog,
            /* [out] */ int *pichLimFoundLog,
            /* [in] */ IVwSearchKiller *pxserkl) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Install( void) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Found( 
            /* [retval][out] */ ComBool *pfFound) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetSelection( 
            /* [in] */ ComBool fInstall,
            /* [retval][out] */ IVwSelection **ppsel) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CLevels( 
            /* [out] */ int *pclev) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE AllTextSelInfo( 
            /* [out] */ int *pihvoRoot,
            /* [in] */ int cvlsi,
            /* [size_is][out] */ VwSelLevInfo *prgvsli,
            /* [out] */ PropTag *ptagTextProp,
            /* [out] */ int *pcpropPrevious,
            /* [out] */ int *pichAnchor,
            /* [out] */ int *pichEnd,
            /* [out] */ int *pws) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE MatchWhole( 
            /* [in] */ IVwSelection *psel,
            /* [retval][out] */ ComBool *pfMatch) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Limit( 
            /* [retval][out] */ IVwSelection **ppsel) = 0;
        
        virtual /* [propputref] */ HRESULT STDMETHODCALLTYPE putref_Limit( 
            /* [in] */ IVwSelection *psel) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_StartingPoint( 
            /* [retval][out] */ IVwSelection **ppsel) = 0;
        
        virtual /* [propputref] */ HRESULT STDMETHODCALLTYPE putref_StartingPoint( 
            /* [in] */ IVwSelection *psel) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_SearchWindow( 
            /* [retval][out] */ DWORD *phwnd) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_SearchWindow( 
            /* [in] */ DWORD hwnd) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_StoppedAtLimit( 
            /* [retval][out] */ ComBool *pfAtLimit) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_StoppedAtLimit( 
            /* [in] */ ComBool fAtLimit) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_LastDirection( 
            /* [retval][out] */ ComBool *pfForward) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ReplaceWith( 
            /* [retval][out] */ /* external definition not present */ ITsString **pptssPattern) = 0;
        
        virtual /* [propputref] */ HRESULT STDMETHODCALLTYPE putref_ReplaceWith( 
            /* [in] */ /* external definition not present */ ITsString *ptssPattern) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ShowMore( 
            /* [retval][out] */ ComBool *pfMore) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_ShowMore( 
            /* [in] */ ComBool fMore) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IcuLocale( 
            /* [retval][out] */ BSTR *pbstrLocale) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_IcuLocale( 
            /* [in] */ BSTR bstrLocale) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IcuCollatingRules( 
            /* [retval][out] */ BSTR *pbstrRules) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_IcuCollatingRules( 
            /* [in] */ BSTR bstrRules) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_UseRegularExpressions( 
            /* [retval][out] */ ComBool *pfMatch) = 0;
        
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_UseRegularExpressions( 
            /* [in] */ ComBool fMatch) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ErrorMessage( 
            /* [retval][out] */ BSTR *pbstrMsg) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_ReplacementText( 
            /* [retval][out] */ /* external definition not present */ ITsString **pptssText) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_Group( 
            /* [in] */ int iGroup,
            /* [retval][out] */ /* external definition not present */ ITsString **pptssGroup) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IVwPatternVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IVwPattern * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IVwPattern * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IVwPattern * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Pattern )( 
            IVwPattern * This,
            /* [retval][out] */ /* external definition not present */ ITsString **pptssPattern);
        
        /* [propputref] */ HRESULT ( STDMETHODCALLTYPE *putref_Pattern )( 
            IVwPattern * This,
            /* [in] */ /* external definition not present */ ITsString *ptssPattern);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Overlay )( 
            IVwPattern * This,
            /* [retval][out] */ IVwOverlay **ppvo);
        
        /* [propputref] */ HRESULT ( STDMETHODCALLTYPE *putref_Overlay )( 
            IVwPattern * This,
            /* [in] */ IVwOverlay *pvo);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_MatchCase )( 
            IVwPattern * This,
            /* [retval][out] */ ComBool *pfMatch);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_MatchCase )( 
            IVwPattern * This,
            /* [in] */ ComBool fMatch);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_MatchDiacritics )( 
            IVwPattern * This,
            /* [retval][out] */ ComBool *pfMatch);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_MatchDiacritics )( 
            IVwPattern * This,
            /* [in] */ ComBool fMatch);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_MatchWholeWord )( 
            IVwPattern * This,
            /* [retval][out] */ ComBool *pfMatch);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_MatchWholeWord )( 
            IVwPattern * This,
            /* [in] */ ComBool fMatch);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_MatchOldWritingSystem )( 
            IVwPattern * This,
            /* [retval][out] */ ComBool *pfMatch);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_MatchOldWritingSystem )( 
            IVwPattern * This,
            /* [in] */ ComBool fMatch);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_MatchExactly )( 
            IVwPattern * This,
            /* [retval][out] */ ComBool *pfMatch);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_MatchExactly )( 
            IVwPattern * This,
            /* [in] */ ComBool fMatch);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_MatchCompatibility )( 
            IVwPattern * This,
            /* [retval][out] */ ComBool *pfMatch);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_MatchCompatibility )( 
            IVwPattern * This,
            /* [in] */ ComBool fMatch);
        
        HRESULT ( STDMETHODCALLTYPE *Find )( 
            IVwPattern * This,
            /* [in] */ IVwRootBox *prootb,
            /* [in] */ ComBool fForward,
            /* [in] */ IVwSearchKiller *pxserkl);
        
        HRESULT ( STDMETHODCALLTYPE *FindFrom )( 
            IVwPattern * This,
            /* [in] */ IVwSelection *psel,
            /* [in] */ ComBool fForward,
            /* [in] */ IVwSearchKiller *pxserkl);
        
        HRESULT ( STDMETHODCALLTYPE *FindNext )( 
            IVwPattern * This,
            /* [in] */ ComBool fForward,
            /* [in] */ IVwSearchKiller *pxserkl);
        
        HRESULT ( STDMETHODCALLTYPE *FindIn )( 
            IVwPattern * This,
            /* [in] */ IVwTextSource *pts,
            /* [in] */ int ichStartLog,
            /* [in] */ int ichEndLog,
            /* [in] */ ComBool fForward,
            /* [out] */ int *pichMinFoundLog,
            /* [out] */ int *pichLimFoundLog,
            /* [in] */ IVwSearchKiller *pxserkl);
        
        HRESULT ( STDMETHODCALLTYPE *Install )( 
            IVwPattern * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Found )( 
            IVwPattern * This,
            /* [retval][out] */ ComBool *pfFound);
        
        HRESULT ( STDMETHODCALLTYPE *GetSelection )( 
            IVwPattern * This,
            /* [in] */ ComBool fInstall,
            /* [retval][out] */ IVwSelection **ppsel);
        
        HRESULT ( STDMETHODCALLTYPE *CLevels )( 
            IVwPattern * This,
            /* [out] */ int *pclev);
        
        HRESULT ( STDMETHODCALLTYPE *AllTextSelInfo )( 
            IVwPattern * This,
            /* [out] */ int *pihvoRoot,
            /* [in] */ int cvlsi,
            /* [size_is][out] */ VwSelLevInfo *prgvsli,
            /* [out] */ PropTag *ptagTextProp,
            /* [out] */ int *pcpropPrevious,
            /* [out] */ int *pichAnchor,
            /* [out] */ int *pichEnd,
            /* [out] */ int *pws);
        
        HRESULT ( STDMETHODCALLTYPE *MatchWhole )( 
            IVwPattern * This,
            /* [in] */ IVwSelection *psel,
            /* [retval][out] */ ComBool *pfMatch);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Limit )( 
            IVwPattern * This,
            /* [retval][out] */ IVwSelection **ppsel);
        
        /* [propputref] */ HRESULT ( STDMETHODCALLTYPE *putref_Limit )( 
            IVwPattern * This,
            /* [in] */ IVwSelection *psel);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_StartingPoint )( 
            IVwPattern * This,
            /* [retval][out] */ IVwSelection **ppsel);
        
        /* [propputref] */ HRESULT ( STDMETHODCALLTYPE *putref_StartingPoint )( 
            IVwPattern * This,
            /* [in] */ IVwSelection *psel);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_SearchWindow )( 
            IVwPattern * This,
            /* [retval][out] */ DWORD *phwnd);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_SearchWindow )( 
            IVwPattern * This,
            /* [in] */ DWORD hwnd);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_StoppedAtLimit )( 
            IVwPattern * This,
            /* [retval][out] */ ComBool *pfAtLimit);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_StoppedAtLimit )( 
            IVwPattern * This,
            /* [in] */ ComBool fAtLimit);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_LastDirection )( 
            IVwPattern * This,
            /* [retval][out] */ ComBool *pfForward);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ReplaceWith )( 
            IVwPattern * This,
            /* [retval][out] */ /* external definition not present */ ITsString **pptssPattern);
        
        /* [propputref] */ HRESULT ( STDMETHODCALLTYPE *putref_ReplaceWith )( 
            IVwPattern * This,
            /* [in] */ /* external definition not present */ ITsString *ptssPattern);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ShowMore )( 
            IVwPattern * This,
            /* [retval][out] */ ComBool *pfMore);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_ShowMore )( 
            IVwPattern * This,
            /* [in] */ ComBool fMore);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IcuLocale )( 
            IVwPattern * This,
            /* [retval][out] */ BSTR *pbstrLocale);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_IcuLocale )( 
            IVwPattern * This,
            /* [in] */ BSTR bstrLocale);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IcuCollatingRules )( 
            IVwPattern * This,
            /* [retval][out] */ BSTR *pbstrRules);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_IcuCollatingRules )( 
            IVwPattern * This,
            /* [in] */ BSTR bstrRules);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_UseRegularExpressions )( 
            IVwPattern * This,
            /* [retval][out] */ ComBool *pfMatch);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_UseRegularExpressions )( 
            IVwPattern * This,
            /* [in] */ ComBool fMatch);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ErrorMessage )( 
            IVwPattern * This,
            /* [retval][out] */ BSTR *pbstrMsg);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_ReplacementText )( 
            IVwPattern * This,
            /* [retval][out] */ /* external definition not present */ ITsString **pptssText);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_Group )( 
            IVwPattern * This,
            /* [in] */ int iGroup,
            /* [retval][out] */ /* external definition not present */ ITsString **pptssGroup);
        
        END_INTERFACE
    } IVwPatternVtbl;

    interface IVwPattern
    {
        CONST_VTBL struct IVwPatternVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IVwPattern_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IVwPattern_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IVwPattern_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IVwPattern_get_Pattern(This,pptssPattern)	\
    ( (This)->lpVtbl -> get_Pattern(This,pptssPattern) ) 

#define IVwPattern_putref_Pattern(This,ptssPattern)	\
    ( (This)->lpVtbl -> putref_Pattern(This,ptssPattern) ) 

#define IVwPattern_get_Overlay(This,ppvo)	\
    ( (This)->lpVtbl -> get_Overlay(This,ppvo) ) 

#define IVwPattern_putref_Overlay(This,pvo)	\
    ( (This)->lpVtbl -> putref_Overlay(This,pvo) ) 

#define IVwPattern_get_MatchCase(This,pfMatch)	\
    ( (This)->lpVtbl -> get_MatchCase(This,pfMatch) ) 

#define IVwPattern_put_MatchCase(This,fMatch)	\
    ( (This)->lpVtbl -> put_MatchCase(This,fMatch) ) 

#define IVwPattern_get_MatchDiacritics(This,pfMatch)	\
    ( (This)->lpVtbl -> get_MatchDiacritics(This,pfMatch) ) 

#define IVwPattern_put_MatchDiacritics(This,fMatch)	\
    ( (This)->lpVtbl -> put_MatchDiacritics(This,fMatch) ) 

#define IVwPattern_get_MatchWholeWord(This,pfMatch)	\
    ( (This)->lpVtbl -> get_MatchWholeWord(This,pfMatch) ) 

#define IVwPattern_put_MatchWholeWord(This,fMatch)	\
    ( (This)->lpVtbl -> put_MatchWholeWord(This,fMatch) ) 

#define IVwPattern_get_MatchOldWritingSystem(This,pfMatch)	\
    ( (This)->lpVtbl -> get_MatchOldWritingSystem(This,pfMatch) ) 

#define IVwPattern_put_MatchOldWritingSystem(This,fMatch)	\
    ( (This)->lpVtbl -> put_MatchOldWritingSystem(This,fMatch) ) 

#define IVwPattern_get_MatchExactly(This,pfMatch)	\
    ( (This)->lpVtbl -> get_MatchExactly(This,pfMatch) ) 

#define IVwPattern_put_MatchExactly(This,fMatch)	\
    ( (This)->lpVtbl -> put_MatchExactly(This,fMatch) ) 

#define IVwPattern_get_MatchCompatibility(This,pfMatch)	\
    ( (This)->lpVtbl -> get_MatchCompatibility(This,pfMatch) ) 

#define IVwPattern_put_MatchCompatibility(This,fMatch)	\
    ( (This)->lpVtbl -> put_MatchCompatibility(This,fMatch) ) 

#define IVwPattern_Find(This,prootb,fForward,pxserkl)	\
    ( (This)->lpVtbl -> Find(This,prootb,fForward,pxserkl) ) 

#define IVwPattern_FindFrom(This,psel,fForward,pxserkl)	\
    ( (This)->lpVtbl -> FindFrom(This,psel,fForward,pxserkl) ) 

#define IVwPattern_FindNext(This,fForward,pxserkl)	\
    ( (This)->lpVtbl -> FindNext(This,fForward,pxserkl) ) 

#define IVwPattern_FindIn(This,pts,ichStartLog,ichEndLog,fForward,pichMinFoundLog,pichLimFoundLog,pxserkl)	\
    ( (This)->lpVtbl -> FindIn(This,pts,ichStartLog,ichEndLog,fForward,pichMinFoundLog,pichLimFoundLog,pxserkl) ) 

#define IVwPattern_Install(This)	\
    ( (This)->lpVtbl -> Install(This) ) 

#define IVwPattern_get_Found(This,pfFound)	\
    ( (This)->lpVtbl -> get_Found(This,pfFound) ) 

#define IVwPattern_GetSelection(This,fInstall,ppsel)	\
    ( (This)->lpVtbl -> GetSelection(This,fInstall,ppsel) ) 

#define IVwPattern_CLevels(This,pclev)	\
    ( (This)->lpVtbl -> CLevels(This,pclev) ) 

#define IVwPattern_AllTextSelInfo(This,pihvoRoot,cvlsi,prgvsli,ptagTextProp,pcpropPrevious,pichAnchor,pichEnd,pws)	\
    ( (This)->lpVtbl -> AllTextSelInfo(This,pihvoRoot,cvlsi,prgvsli,ptagTextProp,pcpropPrevious,pichAnchor,pichEnd,pws) ) 

#define IVwPattern_MatchWhole(This,psel,pfMatch)	\
    ( (This)->lpVtbl -> MatchWhole(This,psel,pfMatch) ) 

#define IVwPattern_get_Limit(This,ppsel)	\
    ( (This)->lpVtbl -> get_Limit(This,ppsel) ) 

#define IVwPattern_putref_Limit(This,psel)	\
    ( (This)->lpVtbl -> putref_Limit(This,psel) ) 

#define IVwPattern_get_StartingPoint(This,ppsel)	\
    ( (This)->lpVtbl -> get_StartingPoint(This,ppsel) ) 

#define IVwPattern_putref_StartingPoint(This,psel)	\
    ( (This)->lpVtbl -> putref_StartingPoint(This,psel) ) 

#define IVwPattern_get_SearchWindow(This,phwnd)	\
    ( (This)->lpVtbl -> get_SearchWindow(This,phwnd) ) 

#define IVwPattern_put_SearchWindow(This,hwnd)	\
    ( (This)->lpVtbl -> put_SearchWindow(This,hwnd) ) 

#define IVwPattern_get_StoppedAtLimit(This,pfAtLimit)	\
    ( (This)->lpVtbl -> get_StoppedAtLimit(This,pfAtLimit) ) 

#define IVwPattern_put_StoppedAtLimit(This,fAtLimit)	\
    ( (This)->lpVtbl -> put_StoppedAtLimit(This,fAtLimit) ) 

#define IVwPattern_get_LastDirection(This,pfForward)	\
    ( (This)->lpVtbl -> get_LastDirection(This,pfForward) ) 

#define IVwPattern_get_ReplaceWith(This,pptssPattern)	\
    ( (This)->lpVtbl -> get_ReplaceWith(This,pptssPattern) ) 

#define IVwPattern_putref_ReplaceWith(This,ptssPattern)	\
    ( (This)->lpVtbl -> putref_ReplaceWith(This,ptssPattern) ) 

#define IVwPattern_get_ShowMore(This,pfMore)	\
    ( (This)->lpVtbl -> get_ShowMore(This,pfMore) ) 

#define IVwPattern_put_ShowMore(This,fMore)	\
    ( (This)->lpVtbl -> put_ShowMore(This,fMore) ) 

#define IVwPattern_get_IcuLocale(This,pbstrLocale)	\
    ( (This)->lpVtbl -> get_IcuLocale(This,pbstrLocale) ) 

#define IVwPattern_put_IcuLocale(This,bstrLocale)	\
    ( (This)->lpVtbl -> put_IcuLocale(This,bstrLocale) ) 

#define IVwPattern_get_IcuCollatingRules(This,pbstrRules)	\
    ( (This)->lpVtbl -> get_IcuCollatingRules(This,pbstrRules) ) 

#define IVwPattern_put_IcuCollatingRules(This,bstrRules)	\
    ( (This)->lpVtbl -> put_IcuCollatingRules(This,bstrRules) ) 

#define IVwPattern_get_UseRegularExpressions(This,pfMatch)	\
    ( (This)->lpVtbl -> get_UseRegularExpressions(This,pfMatch) ) 

#define IVwPattern_put_UseRegularExpressions(This,fMatch)	\
    ( (This)->lpVtbl -> put_UseRegularExpressions(This,fMatch) ) 

#define IVwPattern_get_ErrorMessage(This,pbstrMsg)	\
    ( (This)->lpVtbl -> get_ErrorMessage(This,pbstrMsg) ) 

#define IVwPattern_get_ReplacementText(This,pptssText)	\
    ( (This)->lpVtbl -> get_ReplacementText(This,pptssText) ) 

#define IVwPattern_get_Group(This,iGroup,pptssGroup)	\
    ( (This)->lpVtbl -> get_Group(This,iGroup,pptssGroup) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IVwPattern_INTERFACE_DEFINED__ */


#define CLSID_VwPattern __uuidof(VwPattern)

#ifdef __cplusplus

class DECLSPEC_UUID("6C659C76-3991-48dd-93F7-DA65847D4863")
VwPattern;
#endif

#ifndef __IVwTxtSrcInit2_INTERFACE_DEFINED__
#define __IVwTxtSrcInit2_INTERFACE_DEFINED__

/* interface IVwTxtSrcInit2 */
/* [unique][object][uuid] */ 


#define IID_IVwTxtSrcInit2 __uuidof(IVwTxtSrcInit2)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("8E3EFDB9-4721-4f17-AA50-48DF65078680")
    IVwTxtSrcInit2 : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE SetString( 
            /* [in] */ /* external definition not present */ ITsString *ptss,
            /* [in] */ IVwViewConstructor *pvc,
            /* [in] */ /* external definition not present */ ILgWritingSystemFactory *pwsf) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IVwTxtSrcInit2Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IVwTxtSrcInit2 * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IVwTxtSrcInit2 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IVwTxtSrcInit2 * This);
        
        HRESULT ( STDMETHODCALLTYPE *SetString )( 
            IVwTxtSrcInit2 * This,
            /* [in] */ /* external definition not present */ ITsString *ptss,
            /* [in] */ IVwViewConstructor *pvc,
            /* [in] */ /* external definition not present */ ILgWritingSystemFactory *pwsf);
        
        END_INTERFACE
    } IVwTxtSrcInit2Vtbl;

    interface IVwTxtSrcInit2
    {
        CONST_VTBL struct IVwTxtSrcInit2Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IVwTxtSrcInit2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IVwTxtSrcInit2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IVwTxtSrcInit2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IVwTxtSrcInit2_SetString(This,ptss,pvc,pwsf)	\
    ( (This)->lpVtbl -> SetString(This,ptss,pvc,pwsf) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IVwTxtSrcInit2_INTERFACE_DEFINED__ */


#define CLSID_VwMappedTxtSrc __uuidof(VwMappedTxtSrc)

#ifdef __cplusplus

class DECLSPEC_UUID("01D1C8A7-1222-49c9-BFE6-30A84CE76A40")
VwMappedTxtSrc;
#endif

#ifndef __IVwTxtSrcInit_INTERFACE_DEFINED__
#define __IVwTxtSrcInit_INTERFACE_DEFINED__

/* interface IVwTxtSrcInit */
/* [unique][object][uuid] */ 


#define IID_IVwTxtSrcInit __uuidof(IVwTxtSrcInit)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("1AB3C970-3EC1-4d97-A7B8-122642AF6333")
    IVwTxtSrcInit : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE SetString( 
            /* [in] */ /* external definition not present */ ITsString *ptss) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IVwTxtSrcInitVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IVwTxtSrcInit * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IVwTxtSrcInit * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IVwTxtSrcInit * This);
        
        HRESULT ( STDMETHODCALLTYPE *SetString )( 
            IVwTxtSrcInit * This,
            /* [in] */ /* external definition not present */ ITsString *ptss);
        
        END_INTERFACE
    } IVwTxtSrcInitVtbl;

    interface IVwTxtSrcInit
    {
        CONST_VTBL struct IVwTxtSrcInitVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IVwTxtSrcInit_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IVwTxtSrcInit_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IVwTxtSrcInit_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IVwTxtSrcInit_SetString(This,ptss)	\
    ( (This)->lpVtbl -> SetString(This,ptss) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IVwTxtSrcInit_INTERFACE_DEFINED__ */


#define CLSID_VwStringTextSource __uuidof(VwStringTextSource)

#ifdef __cplusplus

class DECLSPEC_UUID("DAF01E81-3026-4480-8783-EEA04CD2EC80")
VwStringTextSource;
#endif

#define CLSID_VwSearchKiller __uuidof(VwSearchKiller)

#ifdef __cplusplus

class DECLSPEC_UUID("4ADA9157-67F8-499b-88CE-D63DF918DF83")
VwSearchKiller;
#endif

#ifndef __IVwDrawRootBuffered_INTERFACE_DEFINED__
#define __IVwDrawRootBuffered_INTERFACE_DEFINED__

/* interface IVwDrawRootBuffered */
/* [unique][object][uuid] */ 


#define IID_IVwDrawRootBuffered __uuidof(IVwDrawRootBuffered)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("D9E9D65F-E81F-439e-8010-5B22BAEBB92D")
    IVwDrawRootBuffered : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE DrawTheRoot( 
            /* [in] */ IVwRootBox *prootb,
            /* [in] */ HDC hdc,
            /* [in] */ RECT rcpDraw,
            /* [in] */ COLORREF bkclr,
            /* [in] */ ComBool fDrawSel,
            /* [in] */ IVwRootSite *pvrs) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE ReDrawLastDraw( 
            /* [in] */ HDC hdc,
            /* [in] */ RECT rcpDraw) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DrawTheRootAt( 
            /* [in] */ IVwRootBox *prootb,
            /* [in] */ HDC hdc,
            /* [in] */ RECT rcpDraw,
            /* [in] */ COLORREF bkclr,
            /* [in] */ ComBool fDrawSel,
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ RECT rcSrc,
            /* [in] */ RECT rcDst,
            /* [in] */ int ysTop,
            /* [in] */ int dysHeight) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE DrawTheRootRotated( 
            /* [in] */ IVwRootBox *prootb,
            /* [in] */ HDC hdc,
            /* [in] */ RECT rcpDraw,
            /* [in] */ COLORREF bkclr,
            /* [in] */ ComBool fDrawSel,
            /* [in] */ IVwRootSite *pvrs,
            /* [in] */ int nHow) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IVwDrawRootBufferedVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IVwDrawRootBuffered * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IVwDrawRootBuffered * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IVwDrawRootBuffered * This);
        
        HRESULT ( STDMETHODCALLTYPE *DrawTheRoot )( 
            IVwDrawRootBuffered * This,
            /* [in] */ IVwRootBox *prootb,
            /* [in] */ HDC hdc,
            /* [in] */ RECT rcpDraw,
            /* [in] */ COLORREF bkclr,
            /* [in] */ ComBool fDrawSel,
            /* [in] */ IVwRootSite *pvrs);
        
        HRESULT ( STDMETHODCALLTYPE *ReDrawLastDraw )( 
            IVwDrawRootBuffered * This,
            /* [in] */ HDC hdc,
            /* [in] */ RECT rcpDraw);
        
        HRESULT ( STDMETHODCALLTYPE *DrawTheRootAt )( 
            IVwDrawRootBuffered * This,
            /* [in] */ IVwRootBox *prootb,
            /* [in] */ HDC hdc,
            /* [in] */ RECT rcpDraw,
            /* [in] */ COLORREF bkclr,
            /* [in] */ ComBool fDrawSel,
            /* [in] */ IVwGraphics *pvg,
            /* [in] */ RECT rcSrc,
            /* [in] */ RECT rcDst,
            /* [in] */ int ysTop,
            /* [in] */ int dysHeight);
        
        HRESULT ( STDMETHODCALLTYPE *DrawTheRootRotated )( 
            IVwDrawRootBuffered * This,
            /* [in] */ IVwRootBox *prootb,
            /* [in] */ HDC hdc,
            /* [in] */ RECT rcpDraw,
            /* [in] */ COLORREF bkclr,
            /* [in] */ ComBool fDrawSel,
            /* [in] */ IVwRootSite *pvrs,
            /* [in] */ int nHow);
        
        END_INTERFACE
    } IVwDrawRootBufferedVtbl;

    interface IVwDrawRootBuffered
    {
        CONST_VTBL struct IVwDrawRootBufferedVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IVwDrawRootBuffered_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IVwDrawRootBuffered_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IVwDrawRootBuffered_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IVwDrawRootBuffered_DrawTheRoot(This,prootb,hdc,rcpDraw,bkclr,fDrawSel,pvrs)	\
    ( (This)->lpVtbl -> DrawTheRoot(This,prootb,hdc,rcpDraw,bkclr,fDrawSel,pvrs) ) 

#define IVwDrawRootBuffered_ReDrawLastDraw(This,hdc,rcpDraw)	\
    ( (This)->lpVtbl -> ReDrawLastDraw(This,hdc,rcpDraw) ) 

#define IVwDrawRootBuffered_DrawTheRootAt(This,prootb,hdc,rcpDraw,bkclr,fDrawSel,pvg,rcSrc,rcDst,ysTop,dysHeight)	\
    ( (This)->lpVtbl -> DrawTheRootAt(This,prootb,hdc,rcpDraw,bkclr,fDrawSel,pvg,rcSrc,rcDst,ysTop,dysHeight) ) 

#define IVwDrawRootBuffered_DrawTheRootRotated(This,prootb,hdc,rcpDraw,bkclr,fDrawSel,pvrs,nHow)	\
    ( (This)->lpVtbl -> DrawTheRootRotated(This,prootb,hdc,rcpDraw,bkclr,fDrawSel,pvrs,nHow) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IVwDrawRootBuffered_INTERFACE_DEFINED__ */


#define CLSID_VwDrawRootBuffered __uuidof(VwDrawRootBuffered)

#ifdef __cplusplus

class DECLSPEC_UUID("97199458-10C7-49da-B3AE-EA922EA64859")
VwDrawRootBuffered;
#endif

#define CLSID_VwSynchronizer __uuidof(VwSynchronizer)

#ifdef __cplusplus

class DECLSPEC_UUID("5E149A49-CAEE-4823-97F7-BB9DED2A62BC")
VwSynchronizer;
#endif

#ifndef __ILgCollatingEngine_INTERFACE_DEFINED__
#define __ILgCollatingEngine_INTERFACE_DEFINED__

/* interface ILgCollatingEngine */
/* [unique][object][uuid] */ 


#define IID_ILgCollatingEngine __uuidof(ILgCollatingEngine)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("D27A3D8C-D3FE-4E25-9097-8F4A1FB30361")
    ILgCollatingEngine : public IUnknown
    {
    public:
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_SortKey( 
            /* [in] */ BSTR bstrValue,
            /* [in] */ LgCollatingOptions colopt,
            /* [retval][out] */ BSTR *pbstrKey) = 0;
        
        virtual /* [restricted] */ HRESULT STDMETHODCALLTYPE SortKeyRgch( 
            /* [size_is][in] */ const OLECHAR *pch,
            /* [in] */ int cchIn,
            /* [in] */ LgCollatingOptions colopt,
            /* [in] */ int cchMaxOut,
            /* [size_is][out] */ OLECHAR *pchKey,
            /* [out] */ int *pcchOut) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Compare( 
            /* [in] */ BSTR bstrValue1,
            /* [in] */ BSTR bstrValue2,
            /* [in] */ LgCollatingOptions colopt,
            /* [retval][out] */ int *pnVal) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_WritingSystemFactory( 
            /* [retval][out] */ /* external definition not present */ ILgWritingSystemFactory **ppwsf) = 0;
        
        virtual /* [propputref] */ HRESULT STDMETHODCALLTYPE putref_WritingSystemFactory( 
            /* [in] */ /* external definition not present */ ILgWritingSystemFactory *pwsf) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_SortKeyVariant( 
            /* [in] */ BSTR bstrValue,
            /* [in] */ LgCollatingOptions colopt,
            /* [retval][out] */ VARIANT *psaKey) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE CompareVariant( 
            /* [in] */ VARIANT saValue1,
            /* [in] */ VARIANT saValue2,
            /* [in] */ LgCollatingOptions colopt,
            /* [retval][out] */ int *pnVal) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Open( 
            /* [in] */ BSTR bstrLocale) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Close( void) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct ILgCollatingEngineVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ILgCollatingEngine * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ILgCollatingEngine * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ILgCollatingEngine * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_SortKey )( 
            ILgCollatingEngine * This,
            /* [in] */ BSTR bstrValue,
            /* [in] */ LgCollatingOptions colopt,
            /* [retval][out] */ BSTR *pbstrKey);
        
        /* [restricted] */ HRESULT ( STDMETHODCALLTYPE *SortKeyRgch )( 
            ILgCollatingEngine * This,
            /* [size_is][in] */ const OLECHAR *pch,
            /* [in] */ int cchIn,
            /* [in] */ LgCollatingOptions colopt,
            /* [in] */ int cchMaxOut,
            /* [size_is][out] */ OLECHAR *pchKey,
            /* [out] */ int *pcchOut);
        
        HRESULT ( STDMETHODCALLTYPE *Compare )( 
            ILgCollatingEngine * This,
            /* [in] */ BSTR bstrValue1,
            /* [in] */ BSTR bstrValue2,
            /* [in] */ LgCollatingOptions colopt,
            /* [retval][out] */ int *pnVal);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_WritingSystemFactory )( 
            ILgCollatingEngine * This,
            /* [retval][out] */ /* external definition not present */ ILgWritingSystemFactory **ppwsf);
        
        /* [propputref] */ HRESULT ( STDMETHODCALLTYPE *putref_WritingSystemFactory )( 
            ILgCollatingEngine * This,
            /* [in] */ /* external definition not present */ ILgWritingSystemFactory *pwsf);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_SortKeyVariant )( 
            ILgCollatingEngine * This,
            /* [in] */ BSTR bstrValue,
            /* [in] */ LgCollatingOptions colopt,
            /* [retval][out] */ VARIANT *psaKey);
        
        HRESULT ( STDMETHODCALLTYPE *CompareVariant )( 
            ILgCollatingEngine * This,
            /* [in] */ VARIANT saValue1,
            /* [in] */ VARIANT saValue2,
            /* [in] */ LgCollatingOptions colopt,
            /* [retval][out] */ int *pnVal);
        
        HRESULT ( STDMETHODCALLTYPE *Open )( 
            ILgCollatingEngine * This,
            /* [in] */ BSTR bstrLocale);
        
        HRESULT ( STDMETHODCALLTYPE *Close )( 
            ILgCollatingEngine * This);
        
        END_INTERFACE
    } ILgCollatingEngineVtbl;

    interface ILgCollatingEngine
    {
        CONST_VTBL struct ILgCollatingEngineVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ILgCollatingEngine_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ILgCollatingEngine_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ILgCollatingEngine_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ILgCollatingEngine_get_SortKey(This,bstrValue,colopt,pbstrKey)	\
    ( (This)->lpVtbl -> get_SortKey(This,bstrValue,colopt,pbstrKey) ) 

#define ILgCollatingEngine_SortKeyRgch(This,pch,cchIn,colopt,cchMaxOut,pchKey,pcchOut)	\
    ( (This)->lpVtbl -> SortKeyRgch(This,pch,cchIn,colopt,cchMaxOut,pchKey,pcchOut) ) 

#define ILgCollatingEngine_Compare(This,bstrValue1,bstrValue2,colopt,pnVal)	\
    ( (This)->lpVtbl -> Compare(This,bstrValue1,bstrValue2,colopt,pnVal) ) 

#define ILgCollatingEngine_get_WritingSystemFactory(This,ppwsf)	\
    ( (This)->lpVtbl -> get_WritingSystemFactory(This,ppwsf) ) 

#define ILgCollatingEngine_putref_WritingSystemFactory(This,pwsf)	\
    ( (This)->lpVtbl -> putref_WritingSystemFactory(This,pwsf) ) 

#define ILgCollatingEngine_get_SortKeyVariant(This,bstrValue,colopt,psaKey)	\
    ( (This)->lpVtbl -> get_SortKeyVariant(This,bstrValue,colopt,psaKey) ) 

#define ILgCollatingEngine_CompareVariant(This,saValue1,saValue2,colopt,pnVal)	\
    ( (This)->lpVtbl -> CompareVariant(This,saValue1,saValue2,colopt,pnVal) ) 

#define ILgCollatingEngine_Open(This,bstrLocale)	\
    ( (This)->lpVtbl -> Open(This,bstrLocale) ) 

#define ILgCollatingEngine_Close(This)	\
    ( (This)->lpVtbl -> Close(This) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ILgCollatingEngine_INTERFACE_DEFINED__ */


#define CLSID_LgUnicodeCollater __uuidof(LgUnicodeCollater)

#ifdef __cplusplus

class DECLSPEC_UUID("0D9900D2-1693-481F-AA70-7EA64F264EC4")
LgUnicodeCollater;
#endif

#define CLSID_VwLayoutStream __uuidof(VwLayoutStream)

#ifdef __cplusplus

class DECLSPEC_UUID("1CD09E06-6978-4969-A1FC-462723587C32")
VwLayoutStream;
#endif

#ifndef __IPictureFactory_INTERFACE_DEFINED__
#define __IPictureFactory_INTERFACE_DEFINED__

/* interface IPictureFactory */
/* [unique][object][uuid] */ 


#define IID_IPictureFactory __uuidof(IPictureFactory)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("110B7E88-2968-11E0-B493-0019DBF4566E")
    IPictureFactory : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE ImageFromBytes( 
            /* [size_is][in] */ byte *pbData,
            /* [in] */ int cbData,
            /* [retval][out] */ IPicture **pppic) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IPictureFactoryVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IPictureFactory * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IPictureFactory * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IPictureFactory * This);
        
        HRESULT ( STDMETHODCALLTYPE *ImageFromBytes )( 
            IPictureFactory * This,
            /* [size_is][in] */ byte *pbData,
            /* [in] */ int cbData,
            /* [retval][out] */ IPicture **pppic);
        
        END_INTERFACE
    } IPictureFactoryVtbl;

    interface IPictureFactory
    {
        CONST_VTBL struct IPictureFactoryVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IPictureFactory_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IPictureFactory_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IPictureFactory_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IPictureFactory_ImageFromBytes(This,pbData,cbData,pppic)	\
    ( (This)->lpVtbl -> ImageFromBytes(This,pbData,cbData,pppic) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IPictureFactory_INTERFACE_DEFINED__ */


#define CLSID_PictureFactory __uuidof(PictureFactory)

#ifdef __cplusplus

class DECLSPEC_UUID("17A2E876-2968-11E0-8046-0019DBF4566E")
PictureFactory;
#endif

#ifndef __IVwWindow_INTERFACE_DEFINED__
#define __IVwWindow_INTERFACE_DEFINED__

/* interface IVwWindow */
/* [unique][object][uuid] */ 


#define IID_IVwWindow __uuidof(IVwWindow)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("8856396c-63a9-4bc7-ad47-87ec8b6ef5a4")
    IVwWindow : public IUnknown
    {
    public:
        virtual /* [propput] */ HRESULT STDMETHODCALLTYPE put_Window( 
            /* [in] */ DWORD *hwnd) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE GetClientRectangle( 
            /* [out] */ RECT *prcClientRectangle) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IVwWindowVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IVwWindow * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IVwWindow * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IVwWindow * This);
        
        /* [propput] */ HRESULT ( STDMETHODCALLTYPE *put_Window )( 
            IVwWindow * This,
            /* [in] */ DWORD *hwnd);
        
        HRESULT ( STDMETHODCALLTYPE *GetClientRectangle )( 
            IVwWindow * This,
            /* [out] */ RECT *prcClientRectangle);
        
        END_INTERFACE
    } IVwWindowVtbl;

    interface IVwWindow
    {
        CONST_VTBL struct IVwWindowVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IVwWindow_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IVwWindow_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IVwWindow_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IVwWindow_put_Window(This,hwnd)	\
    ( (This)->lpVtbl -> put_Window(This,hwnd) ) 

#define IVwWindow_GetClientRectangle(This,prcClientRectangle)	\
    ( (This)->lpVtbl -> GetClientRectangle(This,prcClientRectangle) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IVwWindow_INTERFACE_DEFINED__ */


#define CLSID_VwWindow __uuidof(VwWindow)

#ifdef __cplusplus

class DECLSPEC_UUID("3fb0fcd2-ac55-42a8-b580-73b89a2b6215")
VwWindow;
#endif

#ifndef __IViewInputMgr_INTERFACE_DEFINED__
#define __IViewInputMgr_INTERFACE_DEFINED__

/* interface IViewInputMgr */
/* [unique][object][uuid] */ 


#define IID_IViewInputMgr __uuidof(IViewInputMgr)

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("e41668f7-d506-4c8a-a5d7-feae5630797e")
    IViewInputMgr : public IUnknown
    {
    public:
        virtual HRESULT STDMETHODCALLTYPE Init( 
            /* [in] */ IVwRootBox *prootb) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE Close( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE TerminateAllCompositions( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE SetFocus( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE KillFocus( void) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsCompositionActive( 
            /* [retval][out] */ ComBool *pfCompositionActive) = 0;
        
        virtual /* [propget] */ HRESULT STDMETHODCALLTYPE get_IsEndingComposition( 
            /* [retval][out] */ ComBool *pfEnding) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE OnUpdateProp( 
            /* [retval][out] */ ComBool *pfProcessed) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE OnMouseEvent( 
            /* [in] */ int xd,
            /* [in] */ int yd,
            /* [in] */ RECT rcSrc,
            /* [in] */ RECT rcDst,
            /* [in] */ VwMouseEvent me,
            /* [retval][out] */ ComBool *pfProcessed) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE OnLayoutChange( void) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE OnSelectionChange( 
            /* [in] */ int nHow) = 0;
        
        virtual HRESULT STDMETHODCALLTYPE OnTextChange( void) = 0;
        
    };
    
    
#else 	/* C style interface */

    typedef struct IViewInputMgrVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IViewInputMgr * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IViewInputMgr * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IViewInputMgr * This);
        
        HRESULT ( STDMETHODCALLTYPE *Init )( 
            IViewInputMgr * This,
            /* [in] */ IVwRootBox *prootb);
        
        HRESULT ( STDMETHODCALLTYPE *Close )( 
            IViewInputMgr * This);
        
        HRESULT ( STDMETHODCALLTYPE *TerminateAllCompositions )( 
            IViewInputMgr * This);
        
        HRESULT ( STDMETHODCALLTYPE *SetFocus )( 
            IViewInputMgr * This);
        
        HRESULT ( STDMETHODCALLTYPE *KillFocus )( 
            IViewInputMgr * This);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsCompositionActive )( 
            IViewInputMgr * This,
            /* [retval][out] */ ComBool *pfCompositionActive);
        
        /* [propget] */ HRESULT ( STDMETHODCALLTYPE *get_IsEndingComposition )( 
            IViewInputMgr * This,
            /* [retval][out] */ ComBool *pfEnding);
        
        HRESULT ( STDMETHODCALLTYPE *OnUpdateProp )( 
            IViewInputMgr * This,
            /* [retval][out] */ ComBool *pfProcessed);
        
        HRESULT ( STDMETHODCALLTYPE *OnMouseEvent )( 
            IViewInputMgr * This,
            /* [in] */ int xd,
            /* [in] */ int yd,
            /* [in] */ RECT rcSrc,
            /* [in] */ RECT rcDst,
            /* [in] */ VwMouseEvent me,
            /* [retval][out] */ ComBool *pfProcessed);
        
        HRESULT ( STDMETHODCALLTYPE *OnLayoutChange )( 
            IViewInputMgr * This);
        
        HRESULT ( STDMETHODCALLTYPE *OnSelectionChange )( 
            IViewInputMgr * This,
            /* [in] */ int nHow);
        
        HRESULT ( STDMETHODCALLTYPE *OnTextChange )( 
            IViewInputMgr * This);
        
        END_INTERFACE
    } IViewInputMgrVtbl;

    interface IViewInputMgr
    {
        CONST_VTBL struct IViewInputMgrVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IViewInputMgr_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IViewInputMgr_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IViewInputMgr_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IViewInputMgr_Init(This,prootb)	\
    ( (This)->lpVtbl -> Init(This,prootb) ) 

#define IViewInputMgr_Close(This)	\
    ( (This)->lpVtbl -> Close(This) ) 

#define IViewInputMgr_TerminateAllCompositions(This)	\
    ( (This)->lpVtbl -> TerminateAllCompositions(This) ) 

#define IViewInputMgr_SetFocus(This)	\
    ( (This)->lpVtbl -> SetFocus(This) ) 

#define IViewInputMgr_KillFocus(This)	\
    ( (This)->lpVtbl -> KillFocus(This) ) 

#define IViewInputMgr_get_IsCompositionActive(This,pfCompositionActive)	\
    ( (This)->lpVtbl -> get_IsCompositionActive(This,pfCompositionActive) ) 

#define IViewInputMgr_get_IsEndingComposition(This,pfEnding)	\
    ( (This)->lpVtbl -> get_IsEndingComposition(This,pfEnding) ) 

#define IViewInputMgr_OnUpdateProp(This,pfProcessed)	\
    ( (This)->lpVtbl -> OnUpdateProp(This,pfProcessed) ) 

#define IViewInputMgr_OnMouseEvent(This,xd,yd,rcSrc,rcDst,me,pfProcessed)	\
    ( (This)->lpVtbl -> OnMouseEvent(This,xd,yd,rcSrc,rcDst,me,pfProcessed) ) 

#define IViewInputMgr_OnLayoutChange(This)	\
    ( (This)->lpVtbl -> OnLayoutChange(This) ) 

#define IViewInputMgr_OnSelectionChange(This,nHow)	\
    ( (This)->lpVtbl -> OnSelectionChange(This,nHow) ) 

#define IViewInputMgr_OnTextChange(This)	\
    ( (This)->lpVtbl -> OnTextChange(This) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IViewInputMgr_INTERFACE_DEFINED__ */


#define CLSID_VwStylesheet __uuidof(VwStylesheet)

#ifdef __cplusplus

class DECLSPEC_UUID("CCE2A7ED-464C-4ec7-A0B0-E3C1F6B94C5A")
VwStylesheet;
#endif
#endif /* __Views_LIBRARY_DEFINED__ */

/* Additional Prototypes for ALL interfaces */

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif


